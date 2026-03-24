using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KindredSiege.Core;

namespace KindredSiege.Rivalry
{
    /// <summary>
    /// PILLAR 3: The Rivalry Engine.
    ///
    /// Generates, tracks, and evolves enemy leaders across encounters.
    /// Rivals remember the player's tactics, adapt their behaviour, develop grudges,
    /// and can return from death as Undying horrors.
    ///
    /// This creates emergent narrative without hand-authored story — the GDD's core
    /// differentiator from other auto-battlers.
    ///
    /// Attach to a persistent manager GameObject (DontDestroyOnLoad).
    /// </summary>
    public class RivalryEngine : MonoBehaviour
    {
        public static RivalryEngine Instance { get; private set; }

        [Header("Config")]
        [SerializeField] private int maxActiveRivals = 6;
        [SerializeField] [Range(0f, 1f)] private float undyingReturnChance = 0.12f;

        // ─── Rival pools ───
        public IReadOnlyList<RivalData> ActiveRivals => _activeRivals;
        private readonly List<RivalData> _activeRivals  = new();
        private readonly List<RivalData> _defeatedRivals = new();

        // ════════════════════════════════════════════
        // LIFECYCLE
        // ════════════════════════════════════════════

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<BattleEndEvent>(OnBattleEnded);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<BattleEndEvent>(OnBattleEnded);
        }

        // ════════════════════════════════════════════
        // NAME GENERATION (Lovecraftian two-part names)
        // ════════════════════════════════════════════

        private static readonly string[] FirstNames =
        {
            "Vhaal", "Skrix", "Yr'oth", "Nyx", "Cthol", "Azrath",
            "Shub", "Nyar", "Zoth", "Gla'aki", "Mhog", "Rlyeh",
            "Dagon", "Ithaqua", "Yig", "Tsath", "Vorvadoss", "Mnomquah"
        };

        private static readonly string[] Epithets =
        {
            "the Drowned",       "Hollow-Eye",        "the Unborn",
            "of the Deep",       "the Flayed",        "the Whispering",
            "Tide-Touched",      "the Forgotten",     "of Black Waters",
            "the Relentless",    "Scar-Bearer",       "the Hungering",
            "of Sunken Halls",   "the Pale",          "the Undying"
        };

        private string GenerateFirstName() => FirstNames[Random.Range(0, FirstNames.Length)];
        private string GenerateEpithet()   => Epithets[Random.Range(0, Epithets.Length)];

        // ════════════════════════════════════════════
        // RIVAL GENERATION
        // ════════════════════════════════════════════

        private static readonly RivalTraitType[] BaseTraitPool =
        {
            RivalTraitType.Rage,
            RivalTraitType.Fearful,
            RivalTraitType.Tactical,
            RivalTraitType.Resilient,
            RivalTraitType.Ambusher
        };

        private static readonly RivalWeaknessType[] WeaknessPool =
        {
            RivalWeaknessType.Flanking,
            RivalWeaknessType.Fire,
            RivalWeaknessType.Light,
            RivalWeaknessType.Isolation,
            RivalWeaknessType.Disruption
        };

        /// <summary>
        /// Procedurally generate a new Grunt-rank rival and add it to the active pool.
        /// </summary>
        public RivalData GenerateRival()
        {
            var rival = new RivalData
            {
                RivalId        = System.Guid.NewGuid().ToString(),
                FirstName      = GenerateFirstName(),
                Epithet        = GenerateEpithet(),
                Rank           = RivalRank.Grunt,
                Weakness       = WeaknessPool[Random.Range(0, WeaknessPool.Length)],
                BaseHP         = Random.Range(80, 130),
                BaseDamage     = Random.Range(8, 16),
                SizeMultiplier = 1f
            };

            // Assign 2–4 random starting traits
            int traitCount = Random.Range(2, 5);
            var selected = BaseTraitPool
                .OrderBy(_ => Random.value)
                .Take(traitCount);
            rival.Traits.AddRange(selected);

            _activeRivals.Add(rival);

            rival.Memory.EncounterLog.Add($"Spawned as Grunt with traits: {string.Join(", ", rival.Traits)}.");
            Debug.Log($"[Rivalry] Spawned: {rival.FullName} | Traits: {string.Join(", ", rival.Traits)} | Weakness: {rival.Weakness}");

            return rival;
        }

        /// <summary>
        /// Ensure the active rival pool is topped up. Called after each battle.
        /// May also resurrect a defeated rival as Undying.
        /// </summary>
        public void RefreshRivalPool()
        {
            int alive = _activeRivals.Count(r => !r.IsDefeated);

            while (alive < maxActiveRivals)
            {
                GenerateRival();
                alive++;
            }

            // Chance to resurrect a fallen rival as an Undying horror
            if (_defeatedRivals.Count > 0 && Random.value < undyingReturnChance)
            {
                var candidate = _defeatedRivals[Random.Range(0, _defeatedRivals.Count)];
                ReturnAsUndying(candidate);
            }
        }

        // ════════════════════════════════════════════
        // POST-BATTLE MEMORY UPDATE
        // ════════════════════════════════════════════

        /// <summary>
        /// Record the outcome of an encounter with a specific rival.
        /// This is the heart of the memory system — drives all adaptation.
        /// </summary>
        /// <param name="rivalId">The rival encountered.</param>
        /// <param name="playerWon">True if player defeated the rival.</param>
        /// <param name="playerUnitTypes">Unit types used by the player (for adaptation).</param>
        public void RecordBattleOutcome(string rivalId, bool playerWon, List<string> playerUnitTypes = null)
        {
            var rival = GetRival(rivalId);
            if (rival == null) return;

            if (playerWon)
            {
                rival.Memory.LossesAgainstPlayer++;
                rival.HasScar = true;
                AdaptToDefeat(rival, playerUnitTypes);
                rival.Memory.EncounterLog.Add($"Defeated by player. Scarred. [{rival.Memory.LossesAgainstPlayer} total defeats]");

                EventBus.Publish(new RivalDefeatedEvent
                {
                    RivalId   = rival.RivalId,
                    RivalName = rival.FullName
                });
            }
            else
            {
                rival.Memory.WinsAgainstPlayer++;
                PromoteRival(rival);
                rival.Memory.EncounterLog.Add($"Defeated player. Promoted to {rival.Rank}. [{rival.Memory.WinsAgainstPlayer} total wins]");

                EventBus.Publish(new RivalEncounteredEvent
                {
                    RivalId   = rival.RivalId,
                    RivalName = rival.FullName,
                    Rank      = rival.Rank.ToString()
                });
            }
        }

        /// <summary>
        /// Record that a rival personally killed one of the player's named units.
        /// This forms the Grudge — the rival will hunt that unit type in future encounters.
        /// </summary>
        public void RecordRivalKilledUnit(string rivalId, string playerUnitName, string playerUnitType)
        {
            var rival = GetRival(rivalId);
            if (rival == null) return;

            rival.Memory.KilledPlayerUnits.Add(playerUnitName);

            // First kill becomes the grudge target
            if (!rival.Memory.HasGrudge)
            {
                rival.Memory.GrudgeTargetUnitName = playerUnitName;

                if (!rival.Traits.Contains(RivalTraitType.Grudge))
                    rival.Traits.Add(RivalTraitType.Grudge);

                rival.Memory.EncounterLog.Add($"Formed grudge against \"{playerUnitName}\" ({playerUnitType}).");
                Debug.Log($"[Rivalry] {rival.FullName} formed a grudge against {playerUnitName}!");
            }
        }

        /// <summary>Track when the player avoids a rival entirely.</summary>
        public void RecordRivalAvoided(string rivalId)
        {
            var rival = GetRival(rivalId);
            if (rival == null) return;

            rival.Memory.TimesAvoided++;

            // After being avoided 3+ times, the rival grows bolder and starts ambushing
            if (rival.Memory.TimesAvoided >= 3 && !rival.Traits.Contains(RivalTraitType.Bold))
            {
                rival.Traits.Add(RivalTraitType.Bold);
                rival.Memory.EncounterLog.Add("Grown bold from being avoided. Now initiates ambushes.");
                Debug.Log($"[Rivalry] {rival.FullName} has grown Bold from being avoided!");
            }
        }

        // ════════════════════════════════════════════
        // RIVAL LIFECYCLE
        // ════════════════════════════════════════════

        private void PromoteRival(RivalData rival)
        {
            if (rival.Rank == RivalRank.Overlord) return;

            rival.Rank           = (RivalRank)((int)rival.Rank + 1);
            rival.PromotionCount++;
            rival.SizeMultiplier = 1f + rival.PromotionCount * 0.15f;  // Gets visually larger
            rival.BaseHP         = Mathf.RoundToInt(rival.BaseHP   * 1.20f);
            rival.BaseDamage     = Mathf.RoundToInt(rival.BaseDamage * 1.10f);

            // Gain a new trait on promotion
            var availableTraits = BaseTraitPool
                .Where(t => !rival.Traits.Contains(t))
                .ToArray();

            if (availableTraits.Length > 0)
            {
                var newTrait = availableTraits[Random.Range(0, availableTraits.Length)];
                rival.Traits.Add(newTrait);
                rival.Memory.EncounterLog.Add($"Promoted to {rival.Rank}. Gained trait: {newTrait}.");
            }

            Debug.Log($"[Rivalry] {rival.FullName} PROMOTED to {rival.Rank}! Size: x{rival.SizeMultiplier:F2}");
        }

        private void AdaptToDefeat(RivalData rival, List<string> playerUnitTypes)
        {
            if (playerUnitTypes == null) return;

            // Scarred by Marksmen — develops a counter-hunting trait
            if (playerUnitTypes.Contains("marksman") && !rival.Traits.Contains(RivalTraitType.MarksmanHunter))
            {
                rival.Traits.Add(RivalTraitType.MarksmanHunter);
                rival.Memory.ScarredByMarksman = true;
                rival.Memory.EncounterLog.Add("Scarred by Marksmen. Will now hunt them specifically.");
                Debug.Log($"[Rivalry] {rival.FullName} developed MarksmanHunter after being scarred.");
            }

            // Defeated by Ritual cards — develops eldritch resistance
            if (playerUnitTypes.Contains("ritual") && !rival.Traits.Contains(RivalTraitType.EldritchResistant))
            {
                rival.Traits.Add(RivalTraitType.EldritchResistant);
                rival.Memory.DefeatedByRitualCards = true;
                rival.Memory.EncounterLog.Add("Adapted to eldritch attacks. Now resistant.");
            }
        }

        /// <summary>
        /// Mark a rival as defeated (drops loot, removed from active pool).
        /// May trigger a Vendetta from a subordinate rival.
        /// </summary>
        public void MarkRivalDefeated(string rivalId)
        {
            var rival = GetRival(rivalId);
            if (rival == null) return;

            rival.IsDefeated = true;
            _activeRivals.Remove(rival);
            _defeatedRivals.Add(rival);

            // Chance that a subordinate (another active rival) develops a Vendetta
            var subordinate = _activeRivals
                .Where(r => !r.IsDefeated && !r.Traits.Contains(RivalTraitType.Vendetta))
                .OrderBy(_ => Random.value)
                .FirstOrDefault();

            if (subordinate != null && Random.value < 0.5f)
            {
                subordinate.Traits.Add(RivalTraitType.Vendetta);
                subordinate.Memory.EncounterLog.Add($"Developed Vendetta after {rival.FullName} was slain.");
                Debug.Log($"[Rivalry] {subordinate.FullName} swears Vendetta for {rival.FullName}!");
            }

            Debug.Log($"[Rivalry] {rival.FullName} defeated and archived.");
        }

        private void ReturnAsUndying(RivalData rival)
        {
            rival.IsDefeated    = false;
            rival.IsUndying     = true;
            rival.Epithet       = "the Undying";
            rival.Rank          = rival.Rank < RivalRank.Captain ? RivalRank.Captain : rival.Rank;
            rival.BaseHP        = Mathf.RoundToInt(rival.BaseHP * 1.5f);
            rival.SizeMultiplier *= 1.2f;

            _defeatedRivals.Remove(rival);
            _activeRivals.Add(rival);

            rival.Memory.EncounterLog.Add("Returned from death as Undying.");
            Debug.Log($"[Rivalry] {rival.FullName} has returned from death as UNDYING!");
        }

        // ════════════════════════════════════════════
        // QUERIES
        // ════════════════════════════════════════════

        public RivalData GetRival(string id)
        {
            return _activeRivals.FirstOrDefault(r => r.RivalId == id)
                ?? _defeatedRivals.FirstOrDefault(r => r.RivalId == id);
        }

        /// <summary>Active rivals not yet defeated, sorted by rank (highest first).</summary>
        public List<RivalData> GetActiveRivals() =>
            _activeRivals
                .Where(r => !r.IsDefeated)
                .OrderByDescending(r => r.Rank)
                .ToList();

        /// <summary>The highest-rank active rival — used for the Arch-Rival boss encounter.</summary>
        public RivalData GetArchRival() =>
            GetActiveRivals().FirstOrDefault();

        /// <summary>
        /// Check if a rival has a grudge against a specific unit name.
        /// Used in-battle to trigger taunt sanity damage.
        /// </summary>
        public bool HasGrudgeAgainst(string rivalId, string unitName) =>
            GetRival(rivalId)?.Memory.GrudgeTargetUnitName == unitName;

        // ════════════════════════════════════════════
        // SAVE / LOAD
        // ════════════════════════════════════════════

        public List<RivalData> GetActivesForSave()   => new List<RivalData>(_activeRivals);
        public List<RivalData> GetDefeatedForSave()  => new List<RivalData>(_defeatedRivals);

        /// <summary>Replace the rival pools with data restored from a save file.</summary>
        public void LoadFromSave(List<RivalData> active, List<RivalData> defeated)
        {
            if (active   == null) active   = new List<RivalData>();
            if (defeated == null) defeated = new List<RivalData>();

            _activeRivals.Clear();
            _defeatedRivals.Clear();
            _activeRivals.AddRange(active);
            _defeatedRivals.AddRange(defeated);

            Debug.Log($"[Rivalry] Loaded: {_activeRivals.Count} active, {_defeatedRivals.Count} defeated rivals.");
        }

        // ════════════════════════════════════════════
        // EVENT HANDLERS
        // ════════════════════════════════════════════

        private void OnBattleEnded(BattleEndEvent evt)
        {
            RefreshRivalPool();
        }
    }
}
