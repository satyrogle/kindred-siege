using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KindredSiege.Core;
using KindredSiege.Battle;

namespace KindredSiege.Rivalry
{
    /// <summary>
    /// PILLAR 3 — Rival Encounter Scheduler (GDD §6)
    ///
    /// Bridges RivalryEngine and BattleManager so rivals actually appear in battles.
    /// Decides WHICH rival appears, WHEN they appear, and RECORDS the outcome.
    ///
    /// Encounter rules:
    ///   - First 2 battles are a grace period (no rival, player finds their feet)
    ///   - Base encounter chance: 40% per battle
    ///   - +25% if the player lost the previous battle (rival is emboldened)
    ///   - +15% per active rival who has a grudge against the player
    ///   - Rivals that have beaten the player are preferred (escalation feel)
    ///   - Season boss: final battle of each season always has an Overlord (if one exists)
    ///
    /// After each encountered battle the rival's memory is updated via RivalryEngine,
    /// which may promote, scar, or evolve them for the next encounter.
    ///
    /// Attach to the persistent Manager GameObject alongside RivalryEngine.
    /// </summary>
    public class RivalEncounterSystem : MonoBehaviour
    {
        public static RivalEncounterSystem Instance { get; private set; }

        // ─── Config ───
        [SerializeField] private int   gracePeriodBattles    = 2;
        [SerializeField] private float baseEncounterChance   = 0.40f;
        [SerializeField] private float defeatEncounterBonus  = 0.25f;
        [SerializeField] private float grudgeEncounterBonus  = 0.15f;

        // ─── State ───
        private RivalData _pendingRival;     // Rival queued for the NEXT battle
        private RivalData _battleRival;      // Rival active in the CURRENT/last battle
        private bool      _lastBattleWasDefeat;
        private List<string> _lastPlayerUnitTypes = new();

        // ─── Public read ───
        /// <summary>The rival that will appear in the next battle (null = no encounter).</summary>
        public RivalData PendingRival => _pendingRival;

        // ════════════════════════════════════════════
        // LIFECYCLE
        // ════════════════════════════════════════════

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<BattleStartEvent>(OnBattleStart);
            EventBus.Subscribe<BattleEndEvent>(OnBattleEnd);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<BattleStartEvent>(OnBattleStart);
            EventBus.Unsubscribe<BattleEndEvent>(OnBattleEnd);
        }

        // ════════════════════════════════════════════
        // EVENT HANDLERS
        // ════════════════════════════════════════════

        private void OnBattleStart(BattleStartEvent evt)
        {
            // Apply the queued rival to BattleManager
            _battleRival   = _pendingRival;
            _pendingRival  = null;

            if (BattleManager.Instance != null)
                BattleManager.Instance.SetActiveRival(_battleRival);

            if (_battleRival != null)
            {
                Debug.Log($"[Rivals] {_battleRival.FullName} [{_battleRival.Rank}] enters the battle. " +
                          $"Horror Rating: {_battleRival.HorrorRating}");

                EventBus.Publish(new RivalEncounteredEvent
                {
                    RivalId   = _battleRival.RivalId,
                    RivalName = _battleRival.FullName,
                    Rank      = _battleRival.Rank.ToString()
                });
            }

            // Snapshot player unit types for post-battle adaptation
            _lastPlayerUnitTypes.Clear();
            if (BattleManager.Instance != null)
            {
                foreach (var unit in BattleManager.Instance.GetTeam1Controllers())
                {
                    if (unit != null && !string.IsNullOrEmpty(unit.UnitType))
                        _lastPlayerUnitTypes.Add(unit.UnitType);
                }
            }
        }

        private void OnBattleEnd(BattleEndEvent evt)
        {
            bool playerWon = evt.BattleResult == BattleEndEvent.Result.Victory;
            _lastBattleWasDefeat = !playerWon;

            // Record outcome for the rival that was present
            if (_battleRival != null && RivalryEngine.Instance != null)
            {
                RivalryEngine.Instance.RecordBattleOutcome(
                    _battleRival.RivalId,
                    playerWon,
                    _lastPlayerUnitTypes);

                if (playerWon)
                    RivalryEngine.Instance.MarkRivalDefeated(_battleRival.RivalId);
            }

            _battleRival = null;

            // Refresh the rival pool (generate new rivals, maybe resurrect Undying)
            RivalryEngine.Instance?.RefreshRivalPool();

            // Roll for who appears in the next battle
            RollForNextEncounter();
        }

        // ════════════════════════════════════════════
        // ENCOUNTER ROLLING
        // ════════════════════════════════════════════

        private void RollForNextEncounter()
        {
            int battlesCompleted = GameManager.Instance?.BattlesCompleted ?? 0;
            int battlesPerSeason = 8; // Matches GameManager default

            // Grace period — no rivals at the start
            if (battlesCompleted < gracePeriodBattles)
            {
                _pendingRival = null;
                Debug.Log($"[Rivals] Grace period ({battlesCompleted}/{gracePeriodBattles}). No encounter next battle.");
                return;
            }

            // Season boss: last battle of each season always has the strongest active rival
            bool isSeasonFinalBattle = (battlesCompleted + 1) >= battlesPerSeason;
            if (isSeasonFinalBattle)
            {
                _pendingRival = GetSeasonBoss();
                if (_pendingRival != null)
                    Debug.Log($"[Rivals] Season final battle — boss: {_pendingRival.FullName} [{_pendingRival.Rank}]");
                return;
            }

            // Standard roll
            float chance = baseEncounterChance;
            if (_lastBattleWasDefeat) chance += defeatEncounterBonus;

            // Grudge bonus — rivals with grudges push for encounters
            int grudgeRivals = RivalryEngine.Instance?.ActiveRivals
                .Count(r => !r.IsDefeated && r.Memory.HasGrudge) ?? 0;
            chance += grudgeRivals * grudgeEncounterBonus;

            chance = Mathf.Clamp01(chance);

            if (Random.value <= chance)
            {
                _pendingRival = PickRival();
                if (_pendingRival != null)
                    Debug.Log($"[Rivals] Encounter scheduled: {_pendingRival.FullName} [{_pendingRival.Rank}]. Roll: {chance:P0}");
            }
            else
            {
                _pendingRival = null;
                Debug.Log($"[Rivals] No encounter next battle. Roll: {chance:P0}");
            }
        }

        /// <summary>
        /// Pick which rival appears next.
        /// Prefers rivals that have beaten the player (escalation), then highest rank.
        /// </summary>
        private RivalData PickRival()
        {
            if (RivalryEngine.Instance == null) return null;

            var candidates = RivalryEngine.Instance.ActiveRivals
                .Where(r => !r.IsDefeated)
                .ToList();

            if (candidates.Count == 0) return null;

            // Prefer rivals with wins against the player
            var emboldened = candidates
                .Where(r => r.Memory.WinsAgainstPlayer > 0)
                .OrderByDescending(r => r.Memory.WinsAgainstPlayer)
                .ThenByDescending(r => r.Rank)
                .FirstOrDefault();

            if (emboldened != null && Random.value < 0.65f)
                return emboldened;

            // Otherwise pick by rank weight (higher rank = more likely)
            return candidates
                .OrderByDescending(r => (int)r.Rank + Random.value)
                .FirstOrDefault();
        }

        /// <summary>
        /// Returns the strongest active rival for the season boss battle.
        /// Generates a Captain if no suitable rival exists.
        /// </summary>
        private RivalData GetSeasonBoss()
        {
            var engine = RivalryEngine.Instance;
            if (engine == null) return null;

            // Prefer highest-ranked active rival
            var boss = engine.ActiveRivals
                .Where(r => !r.IsDefeated)
                .OrderByDescending(r => r.Rank)
                .FirstOrDefault();

            // If only Grunts exist, promote one on the spot for the season climax
            if (boss != null && boss.Rank == RivalRank.Grunt)
            {
                // Generate a fresh Captain-level rival for dramatic effect
                var freshBoss = engine.GenerateRival();
                freshBoss.Memory.EncounterLog.Add("Elevated to season boss.");
                return freshBoss;
            }

            return boss;
        }

        // ════════════════════════════════════════════
        // PUBLIC API
        // ════════════════════════════════════════════

        /// <summary>
        /// Force a specific rival into the next battle (for scripted events / season bosses).
        /// </summary>
        public void ForceEncounterNext(RivalData rival) => _pendingRival = rival;

        /// <summary>
        /// Force-clear the pending encounter (player chose to avoid).
        /// Records the avoidance in the rival's memory.
        /// </summary>
        public void AvoidPendingEncounter()
        {
            if (_pendingRival == null) return;
            RivalryEngine.Instance?.RecordRivalAvoided(_pendingRival.RivalId);
            Debug.Log($"[Rivals] {_pendingRival.FullName} avoided. They grow bolder.");
            _pendingRival = null;
        }
    }
}
