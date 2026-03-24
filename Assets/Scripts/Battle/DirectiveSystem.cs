using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KindredSiege.Core;

namespace KindredSiege.Battle
{
    /// <summary>
    /// PILLAR 1 + 2: Tactical Directive System (GDD §4.2)
    ///
    /// The player starts each battle with 5 Directive Points (upgradeable via War Table).
    /// During auto-battle, clicking a unit and selecting a Directive forces an immediate
    /// AI override. Points don't regenerate — once spent, the rest of the battle is pure AI.
    ///
    /// This is the during-battle strategy layer: 5 points forces brutal prioritisation.
    ///
    /// Attach to the BattleArena GameObject alongside BattleManager.
    /// </summary>
    public class DirectiveSystem : MonoBehaviour
    {
        public static DirectiveSystem Instance { get; private set; }

        [Header("Config (GDD §4.2)")]
        [SerializeField] private int startingDirectivePoints = 5;   // Upgradeable via War Table
        [SerializeField] private int startingMercyTokens     = 1;   // Upgradeable via Shrine

        // ─── State ───
        public int DirectivePoints     { get; private set; }
        public int MercyTokens         { get; private set; }

        // FocusFire target — shared with BattleManager to override all units' targeting
        public UnitController FocusFireTarget    { get; private set; }
        public float          FocusFireTimer     { get; private set; }
        private const float   FocusFireDuration  = 8f;

        // Mercy pause state — raised when a unit hits 0 HP
        public bool          MercyPauseActive   { get; private set; }
        public UnitController MercyPauseUnit    { get; private set; }

        // Unleash active units (for UI feedback)
        private readonly HashSet<int> _unleashActiveIds = new();

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
            EventBus.Subscribe<UnitDefeatedEvent>(OnUnitDefeated);
            EventBus.Subscribe<MercyDecisionResolvedEvent>(OnMercyResolved);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<BattleStartEvent>(OnBattleStart);
            EventBus.Unsubscribe<UnitDefeatedEvent>(OnUnitDefeated);
            EventBus.Unsubscribe<MercyDecisionResolvedEvent>(OnMercyResolved);
        }

        private void OnBattleStart(BattleStartEvent evt)
        {
            DirectivePoints   = startingDirectivePoints;
            MercyTokens       = startingMercyTokens;
            FocusFireTarget   = null;
            FocusFireTimer    = 0f;
            MercyPauseActive  = false;
            MercyPauseUnit    = null;
            _unleashActiveIds.Clear();

            // City building bonuses: War Table → +Directive Points, Shrine → +Mercy Tokens
            var bridge = KindredSiege.City.CityBattleBridge.Instance;
            if (bridge != null)
            {
                DirectivePoints += bridge.ExtraDirectivePoints;
                MercyTokens     += bridge.ExtraMercyTokens;
            }
        }

        private void Update()
        {
            // Tick down FocusFire duration
            if (FocusFireTarget != null)
            {
                FocusFireTimer -= Time.deltaTime;
                if (FocusFireTimer <= 0f || !FocusFireTarget.IsAlive)
                    FocusFireTarget = null;
            }
        }

        // ════════════════════════════════════════════
        // DIRECTIVE SPENDING
        // ════════════════════════════════════════════

        /// <summary>
        /// Attempt to spend a directive. Returns true if the directive was applied.
        /// Directives that require a target unit should pass it as targetUnit.
        /// FocusFire requires targetUnit = the enemy to focus.
        /// InvokeMercy requires targetUnit = the fallen unit to revive.
        /// </summary>
        public bool SpendDirective(DirectiveType type, UnitController targetUnit = null)
        {
            int cost = GetDirectiveCost(type);

            if (type == DirectiveType.InvokeMercy)
            {
                if (MercyTokens <= 0)
                {
                    Debug.LogWarning("[Directives] No Mercy Tokens remaining.");
                    return false;
                }
                return ApplyInvokeMercy(targetUnit);
            }

            if (DirectivePoints < cost)
            {
                Debug.LogWarning($"[Directives] Not enough points for {type} (need {cost}, have {DirectivePoints}).");
                return false;
            }

            bool applied = type switch
            {
                DirectiveType.FocusFire    => ApplyFocusFire(targetUnit),
                DirectiveType.HoldPosition => ApplyHoldPosition(targetUnit),
                DirectiveType.FallBack     => ApplyFallBack(targetUnit),
                DirectiveType.Unleash      => ApplyUnleash(targetUnit),
                DirectiveType.Sacrifice    => ApplySacrifice(targetUnit),
                _                          => false
            };

            if (applied)
            {
                DirectivePoints -= cost;
                Debug.Log($"[Directives] {type} applied. Points remaining: {DirectivePoints}");

                EventBus.Publish(new DirectiveUsedEvent
                {
                    DirectiveName  = type.ToString(),
                    UnitId         = targetUnit?.UnitId ?? -1,
                    PointsRemaining = DirectivePoints
                });
            }

            return applied;
        }

        // ════════════════════════════════════════════
        // DIRECTIVE IMPLEMENTATIONS
        // ════════════════════════════════════════════

        /// <summary>
        /// Focus Fire (1pt): All player units target the clicked enemy for 8 seconds.
        /// BattleManager injects the focus target into every unit's BattleContext each tick.
        /// </summary>
        private bool ApplyFocusFire(UnitController targetEnemy)
        {
            if (targetEnemy == null || !targetEnemy.IsAlive)
            {
                Debug.LogWarning("[Directives] Focus Fire requires a living enemy target.");
                return false;
            }

            FocusFireTarget = targetEnemy;
            FocusFireTimer  = FocusFireDuration;
            Debug.Log($"[Directives] Focus Fire → {targetEnemy.UnitName} for {FocusFireDuration}s.");
            return true;
        }

        /// <summary>
        /// Hold Position (1pt): Unit stops moving, attacks anything in range for 10 seconds.
        /// </summary>
        private bool ApplyHoldPosition(UnitController targetUnit)
        {
            if (targetUnit == null || !targetUnit.IsAlive)
            {
                Debug.LogWarning("[Directives] Hold Position requires a living friendly unit.");
                return false;
            }

            StartCoroutine(HoldPositionCoroutine(targetUnit, 10f));
            Debug.Log($"[Directives] Hold Position → {targetUnit.UnitName} for 10s.");
            return true;
        }

        private IEnumerator HoldPositionCoroutine(UnitController unit, float duration)
        {
            unit.DirectiveOverrideActive = true;
            Vector3 holdPos = unit.transform.position;
            float elapsed   = 0f;

            while (elapsed < duration && unit != null && unit.IsAlive)
            {
                // Force unit back to hold position each frame
                unit.transform.position = holdPos;
                elapsed += Time.deltaTime;
                yield return null;
            }
            if (unit != null) unit.DirectiveOverrideActive = false;
        }

        /// <summary>
        /// Fall Back (1pt): Unit retreats to the rear of its spawn zone immediately.
        /// Activates Retreat behaviour for 3 seconds.
        /// </summary>
        private bool ApplyFallBack(UnitController targetUnit)
        {
            if (targetUnit == null || !targetUnit.IsAlive)
            {
                Debug.LogWarning("[Directives] Fall Back requires a living friendly unit.");
                return false;
            }

            StartCoroutine(FallBackCoroutine(targetUnit, 3f));
            Debug.Log($"[Directives] Fall Back → {targetUnit.UnitName}.");
            return true;
        }

        private IEnumerator FallBackCoroutine(UnitController unit, float duration)
        {
            float elapsed = 0f;

            while (elapsed < duration && unit != null && unit.IsAlive)
            {
                // Move toward spawn position at full speed
                Vector3 home = unit.SpawnPosition;
                float dist   = Vector3.Distance(unit.transform.position, home);

                if (dist > 0.5f)
                {
                    Vector3 dir = (home - unit.transform.position).normalized;
                    unit.transform.position += dir * unit.MoveSpeed * 1.5f * Time.deltaTime;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (unit != null) unit.DirectiveOverrideActive = false;

        }

        /// <summary>
        /// Unleash (2pt): +20% damage, ignore retreat thresholds for 10 seconds, -5 sanity cost upfront.
        /// </summary>
        private bool ApplyUnleash(UnitController targetUnit)
        {
            if (targetUnit == null || !targetUnit.IsAlive)
            {
                Debug.LogWarning("[Directives] Unleash requires a living friendly unit.");
                return false;
            }

            targetUnit.ModifySanity(-5, "EldritchHit"); // Sanity cost of unleashing
            _unleashActiveIds.Add(targetUnit.UnitId);
            // Visual feedback — turn the unit yellow during Unleash
            var renderer = targetUnit.GetComponent<Renderer>();
            if (renderer != null)
            {
                var mat = renderer.material;
                mat.color = Color.yellow;
            }
            StartCoroutine(UnleashCoroutine(targetUnit, 10f));
            Debug.Log($"[Directives] Unleash → {targetUnit.UnitName} for 10s (+20% dmg, no retreat).");
            return true;
        }

        private IEnumerator UnleashCoroutine(UnitController unit, float duration)
        {
            unit.GambitDamageMultiplier = 1.20f;
            unit.GambitIgnoreRetreat    = true;

            yield return new WaitForSeconds(duration);

            if (unit != null)
            {
                unit.GambitDamageMultiplier = 1f;
                unit.GambitIgnoreRetreat = false;
                _unleashActiveIds.Remove(unit.UnitId);

                // Restore team colour
                var renderer = unit.GetComponent<Renderer>();
                if (renderer != null)
                    renderer.material.color = new Color(0.2f, 0.5f, 0.9f); // Blue team colour

                Debug.Log($"[Directives] Unleash expired on {unit.UnitName}.");
            }
        }

        /// <summary>
        /// Sacrifice (3pt): Unit charges the strongest enemy with 2x damage, then dies at the end of the charge.
        /// </summary>
        private bool ApplySacrifice(UnitController targetUnit)
        {
            if (targetUnit == null || !targetUnit.IsAlive)
            {
                Debug.LogWarning("[Directives] Sacrifice requires a living friendly unit.");
                return false;
            }

            StartCoroutine(SacrificeCoroutine(targetUnit));
            Debug.Log($"[Directives] Sacrifice → {targetUnit.UnitName} charges the strongest enemy.");
            return true;
        }

        private IEnumerator SacrificeCoroutine(UnitController unit)
        {
            unit.DirectiveOverrideActive = true;
            unit.GambitDamageMultiplier = 2.0f;
            unit.GambitIgnoreRetreat    = true;


            float elapsed = 0f;
            float maxDuration = 8f; // Safety cap — sacrifice charge lasts up to 8 seconds

            while (elapsed < maxDuration && unit != null && unit.IsAlive)
            {
                // Find the strongest enemy each frame (in case it changes)
                var strongest = FindStrongestEnemy(unit);
                if (strongest == null) break;

                float dist = Vector3.Distance(unit.transform.position, strongest.transform.position);

                if (dist <= unit.AttackRange)
                {
                    // In range — deliver the killing blow
                    unit.PerformAttack(strongest);
                    break;
                }

                // Charge at full speed
                Vector3 dir = (strongest.transform.position - unit.transform.position).normalized;
                unit.transform.position += dir * unit.MoveSpeed * 2f * Time.deltaTime;

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Unit dies at the end of the charge
            if (unit != null && unit.IsAlive)
            {
                unit.DirectiveOverrideActive = false;
                unit.TakeDamage(unit.CurrentHP, null); // Instant death
                Debug.Log($"[Directives] {unit.UnitName} died in the Sacrifice charge.");
            }
        }

        /// <summary>
        /// Invoke Mercy (Token): Revive a fallen unit at 30% HP + 15 sanity.
        /// Can only be used during the Mercy Pause or immediately after a unit falls.
        /// </summary>
        private bool ApplyInvokeMercy(UnitController targetUnit)
        {
            if (targetUnit == null)
            {
                Debug.LogWarning("[Directives] Invoke Mercy requires a target unit.");
                return false;
            }

            MercyTokens--;
            targetUnit.OnSavedByMercy();

            // Re-activate the unit's GameObject if it was deactivated on death
            if (!targetUnit.gameObject.activeSelf)
                targetUnit.gameObject.SetActive(true);

            // Clear mercy pause
            if (MercyPauseActive && MercyPauseUnit == targetUnit)
                ResolveMercyPause(tokenSpent: true);

            EventBus.Publish(new MercyDecisionResolvedEvent
            {
                UnitId     = targetUnit.UnitId,
                TokenSpent = true
            });

            Debug.Log($"[Directives] Mercy Token used on {targetUnit.UnitName}. Tokens remaining: {MercyTokens}");
            return true;
        }

        // ════════════════════════════════════════════
        // MERCY DECISION PAUSE
        // ════════════════════════════════════════════

        /// <summary>
        /// Called by BattleManager when a unit hits 0 HP.
        /// Raises MercyDecisionRequiredEvent for the HUD to pause and prompt the player.
        /// </summary>
        public void RaiseMercyDecision(UnitController unit)
        {
            if (unit == null) return;

            MercyPauseActive = true;
            MercyPauseUnit   = unit;

            if (BattleManager.Instance != null)
                BattleManager.Instance.PauseBattle();

            EventBus.Publish(new MercyDecisionRequiredEvent
            {
                UnitId                 = unit.UnitId,
                UnitName               = unit.UnitName,
                UnitType               = unit.UnitType,
                ExpeditionCount        = unit.Data != null ? unit.Data.ExpeditionCount : 0,
                MercyTokensAvailable   = MercyTokens
            });

            Debug.Log($"[Mercy] Decision required for {unit.UnitName}. Tokens: {MercyTokens}");
        }

        /// <summary>Let the unit die permanently and resume battle.</summary>
        public void LetUnitDie()
        {
            if (!MercyPauseActive || MercyPauseUnit == null) return;

            EventBus.Publish(new MercyDecisionResolvedEvent
            {
                UnitId     = MercyPauseUnit.UnitId,
                TokenSpent = false
            });

            ResolveMercyPause(tokenSpent: false);
        }

        private void ResolveMercyPause(bool tokenSpent)
        {
            MercyPauseActive = false;
            MercyPauseUnit   = null;

            if (BattleManager.Instance != null)
                BattleManager.Instance.ResumeBattle();
        }

        // ════════════════════════════════════════════
        // FOCUS FIRE — called by BattleManager per tick
        // ════════════════════════════════════════════

        /// <summary>
        /// If FocusFire is active, set ForcedTarget on all player units.
        /// BattleManager calls this every Update() before ticking unit AI.
        /// </summary>
        public void InjectFocusFireTarget(List<UnitController> playerTeam)
        {
            UnitController activeTarget = (FocusFireTarget != null && FocusFireTarget.IsAlive)
                ? FocusFireTarget : null;

            foreach (var unit in playerTeam)
            {
                if (unit == null || !unit.IsAlive) continue;
                unit.ForcedTarget = activeTarget;
            }
        }

        // ════════════════════════════════════════════
        // TOKEN MANAGEMENT
        // ════════════════════════════════════════════

        /// <summary>Add Mercy Tokens (from Vessel survival, Shrine upgrades, etc.).</summary>
        public void AddMercyTokens(int amount, string source = "")
        {
            MercyTokens += amount;
            EventBus.Publish(new MercyTokenEarnedEvent { Amount = amount, Source = source });
            Debug.Log($"[Mercy] +{amount} token(s) from {source}. Total: {MercyTokens}");
        }

        // ════════════════════════════════════════════
        // EVENT HANDLERS
        // ════════════════════════════════════════════

        private void OnUnitDefeated(UnitDefeatedEvent evt)
        {
            // Only pause for player team (Team 1)
            if (evt.TeamId != 1) return;

            var unit = FindUnitById(evt.UnitId);
            if (unit != null)
                RaiseMercyDecision(unit);
        }

        private void OnMercyResolved(MercyDecisionResolvedEvent evt)
        {
            // If resolved externally, clear pause state
            if (MercyPauseActive)
                ResolveMercyPause(evt.TokenSpent);
        }

        // ════════════════════════════════════════════
        // HELPERS
        // ════════════════════════════════════════════

        public static int GetDirectiveCost(DirectiveType type)
        {
            return type switch
            {
                DirectiveType.FocusFire    => 1,
                DirectiveType.HoldPosition => 1,
                DirectiveType.FallBack     => 1,
                DirectiveType.Unleash      => 2,
                DirectiveType.Sacrifice    => 3,
                DirectiveType.InvokeMercy  => 0, // Token cost, not point cost
                _                          => 0
            };
        }

        public bool IsUnleashActive(int unitId) => _unleashActiveIds.Contains(unitId);

        private UnitController FindUnitById(int unitId)
        {
            return BattleManager.Instance?.GetUnitById(unitId);
        }

        private UnitController FindStrongestEnemy(UnitController fromUnit)
        {
            var enemies = fromUnit.TeamId == 1 ? BattleManager.Instance?.GetTeam2Controllers() : BattleManager.Instance?.GetTeam1Controllers();
            if (enemies == null) return null;

            return enemies
                .Where(u => u != null && u.IsAlive)
                .OrderByDescending(u => u.CurrentHP)
                .FirstOrDefault();
        }
    }
}
