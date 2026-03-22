using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KindredSiege.AI.BehaviourTree;
using KindredSiege.Core;

namespace KindredSiege.Battle
{
    /// <summary>
    /// Runtime controller for a unit in battle.
    /// Owns a behaviour tree AND a sanity state that directly degrades AI quality.
    ///
    /// PILLAR 1 — AI Behaviour: behaviour tree ticked every frame during combat.
    /// PILLAR 2 — Psychological Simulation: sanity drains under stress, causing
    ///            hesitation, afflictions, and eventually permanent loss.
    ///
    /// Attach to unit prefabs.
    /// </summary>
    public class UnitController : MonoBehaviour
    {
        [Header("Unit Config")]
        [SerializeField] private UnitData unitData;

        // ─── Combat Stats ───
        public int MaxHP          { get; private set; }
        public int CurrentHP      { get; private set; }
        public int AttackDamage   { get; private set; }
        public float AttackRange  { get; private set; }
        public float MoveSpeed    { get; private set; }
        public int Armour         { get; private set; }

        // ─── Sanity ───
        public int MaxSanity      { get; private set; }
        public int CurrentSanity  { get; private set; }

        public SanityState SanityState => SanitySystem.GetState(CurrentSanity);

        // Active affliction / virtue — set when sanity first drops below StressedMin
        public AfflictionType ActiveAffliction { get; private set; } = AfflictionType.None;
        public VirtueType     ActiveVirtue     { get; private set; } = VirtueType.None;

        private bool  _stressTraitRolled   = false;
        private bool  _stalwartActive      = false;  // Immune to further sanity loss
        private float _resoluteTimer       = 0f;     // Invincibility window (Resolute virtue)
        private float _courageousAuraCooldown = 0f;

        private const float ResoluteShieldDuration  = 10f;
        private const float CourageousAuraInterval  = 8f;

        // ─── Identity ───
        public bool   IsAlive   => CurrentHP > 0;
        public bool   IsVeteran => unitData != null && unitData.ExpeditionCount >= 5;
        public string UnitType  => unitData != null ? unitData.UnitType : "warden";
        public string UnitName  => unitData != null ? unitData.UnitName : "Unit";
        public UnitData Data    => unitData;

        public int TeamId  { get; private set; }
        public int UnitId  { get; private set; }

        // ─── Comprehension (GDD §5.3) ───
        // Applied as a multiplier to all eldritch-source sanity damage.
        public float Comprehension => unitData != null ? unitData.Comprehension : 1f;

        // ─── Gambit / Directive state ───
        // Gambits and Directives can set these to modify combat behaviour.
        public float GambitDamageMultiplier { get; set; } = 1f;
        public bool  GambitIgnoreRetreat    { get; set; } = false;

        // Spawn position — recorded in Initialise. Used by HoldTheLine gambit.
        public Vector3 SpawnPosition { get; private set; }

        // Set by DirectiveSystem when FocusFire is active; injected into BT blackboard each tick.
        public UnitController ForcedTarget { get; set; }

        // ─── Prolonged combat drain (GDD §5.2: -3 per round after round 5) ───
        private float _combatTimer    = 0f;
        private int   _drainRound     = 0;
        private const float SecondsPerRound    = 5f;
        private const int   RoundsBeforeDrain  = 5;

        // ─── Behaviour Tree ───
        private BTNode       behaviourTree;
        private BattleContext battleContext;
        private float         attackTimer   = 0f;
        private float         attackCooldown = 1f;

        // ─── Battle Recording (replay system) ───
        public List<UnitActionRecord> ActionHistory { get; private set; } = new();

        [System.Serializable]
        public struct UnitActionRecord
        {
            public float   Timestamp;
            public string  Action;
            public Vector3 Position;
            public int     TargetId;
        }

        // ════════════════════════════════════════════
        // INITIALISATION
        // ════════════════════════════════════════════

        /// <summary>Called by BattleManager when spawning units.</summary>
        public void Initialise(UnitData data, int teamId, int unitId)
        {
            unitData = data;
            TeamId   = teamId;
            UnitId   = unitId;

            // Combat stats
            MaxHP         = data.MaxHP;
            CurrentHP     = data.MaxHP;
            AttackDamage  = data.AttackDamage;
            AttackRange   = data.AttackRange;
            MoveSpeed     = data.MoveSpeed;
            Armour        = data.Armour;
            attackCooldown = data.AttackCooldown;
            attackTimer   = 0f;

            // Sanity init
            MaxSanity        = data.BaseSanity;
            CurrentSanity    = MaxSanity;
            ActiveAffliction = AfflictionType.None;
            ActiveVirtue     = VirtueType.None;
            _stressTraitRolled   = false;
            _stalwartActive      = false;
            _resoluteTimer       = 0f;
            _courageousAuraCooldown = 0f;
            _combatTimer = 0f;
            _drainRound  = 0;

            // Build default behaviour tree for this class
            behaviourTree = BTPresets.GetPreset(data.UnitType);

            // Record spawn position for HoldTheLine gambit
            SpawnPosition = transform.position;

            // Reset gambit / directive modifiers
            GambitDamageMultiplier = 1f;
            GambitIgnoreRetreat    = false;
            ForcedTarget           = null;

            transform.localScale = Vector3.one * data.ModelScale;
            ActionHistory.Clear();
        }

        /// <summary>Apply building/upgrade stat multipliers before battle starts.</summary>
        public void ApplyModifiers(float hpMult = 1f, float dmgMult = 1f, float speedMult = 1f)
        {
            MaxHP        = Mathf.RoundToInt(unitData.MaxHP * hpMult);
            CurrentHP    = MaxHP;
            AttackDamage = Mathf.RoundToInt(unitData.AttackDamage * dmgMult);
            MoveSpeed    = unitData.MoveSpeed * speedMult;
        }

        /// <summary>Set the shared battle context. Called by BattleManager each frame.</summary>
        public void SetContext(BattleContext context) => battleContext = context;

        // ════════════════════════════════════════════
        // AI TICK — runs every frame during battle
        // ════════════════════════════════════════════

        /// <summary>
        /// Tick the behaviour tree with sanity-based degradation applied first.
        /// High sanity = full effectiveness. Low sanity = hesitation, affliction effects,
        /// and eventually complete breakdown.
        /// </summary>
        public void TickAI()
        {
            if (!IsAlive || battleContext == null) return;

            float dt = Time.deltaTime;
            attackTimer  -= dt;
            _combatTimer += dt;

            battleContext.DeltaTime = dt;
            battleContext.Owner     = this;

            // Focus Fire directive: override target for all BT nodes this tick
            if (ForcedTarget != null && ForcedTarget.IsAlive)
                battleContext.Set("Target", ForcedTarget);

            // ── Passive sanity drain (Vessel class) ──
            if (unitData != null && unitData.PassiveSanityDrainPerSecond > 0)
            {
                // Spread drain probabilistically over frames to avoid float precision issues
                if (Random.value < unitData.PassiveSanityDrainPerSecond * dt)
                    ModifySanity(-1, "PassiveDrain");
            }

            // ── Prolonged combat drain (GDD §5.2: -3 per round after round 5) ──
            int currentRound = Mathf.FloorToInt(_combatTimer / SecondsPerRound);
            if (currentRound > RoundsBeforeDrain && currentRound > _drainRound)
            {
                _drainRound = currentRound;
                ModifySanity(-3, "ProlongedCombat");
            }

            // ── Virtue timers ──
            if (_resoluteTimer > 0f)
                _resoluteTimer -= dt;

            if (ActiveVirtue == VirtueType.Courageous)
            {
                _courageousAuraCooldown -= dt;
                if (_courageousAuraCooldown <= 0f)
                {
                    _courageousAuraCooldown = CourageousAuraInterval;
                    BoostNearbyAllySanity(amount: 10, range: 5f);
                }
            }

            // ── Sanity hesitation (GDD §5.1) ──
            float hesitation = SanitySystem.GetHesitationChance(SanityState);
            if (hesitation > 0f && Random.value < hesitation)
                return; // Unit hesitates — skip this tick

            // ── Broken: cower and retreat ──
            if (SanityState == SanityState.Broken && Random.value < 0.40f)
            {
                var nearestEnemy = battleContext.Enemies
                    .Where(e => e != null && e.IsAlive)
                    .OrderBy(e => Vector3.Distance(transform.position, e.transform.position))
                    .FirstOrDefault();

                if (nearestEnemy != null)
                {
                    Vector3 awayDir = (transform.position - nearestEnemy.transform.position).normalized;
                    transform.position += awayDir * MoveSpeed * dt;
                }
                return;
            }

            // ── Affliction: Paranoid — 15% chance to attack a random ally ──
            if (ActiveAffliction == AfflictionType.Paranoid && Random.value < 0.15f)
            {
                var randomAlly = battleContext.Allies
                    .Where(a => a != null && a.IsAlive && a != this)
                    .OrderBy(_ => Random.value)
                    .FirstOrDefault();

                if (randomAlly != null)
                {
                    battleContext.Set("Target", randomAlly);
                    if (CanAttack()) PerformAttack(randomAlly);
                    return;
                }
            }

            // ── Affliction: Irrational — override targeting with a random enemy ──
            if (ActiveAffliction == AfflictionType.Irrational)
            {
                var randomEnemy = battleContext.Enemies
                    .Where(e => e != null && e.IsAlive)
                    .OrderBy(_ => Random.value)
                    .FirstOrDefault();

                if (randomEnemy != null)
                    battleContext.Set("Target", randomEnemy);
            }

            // ── Affliction: Selfish — strip heal targets so HealAlly can't fire ──
            if (ActiveAffliction == AfflictionType.Selfish)
                battleContext.Blackboard.Remove("HealTarget");

            // ── Affliction: Hopeless — raise retreat threshold (BT nodes read this flag) ──
            battleContext.Set("Hopeless", ActiveAffliction == AfflictionType.Hopeless);

            // ── Normal BT execution ──
            behaviourTree.Tick(battleContext);
        }

        // ════════════════════════════════════════════
        // SANITY SYSTEM
        // ════════════════════════════════════════════

        /// <summary>
        /// Modify sanity, apply class-specific multipliers, clamp, and handle state transitions.
        /// Positive delta = restore. Negative delta = stress.
        /// </summary>
        public void ModifySanity(int delta, string reason)
        {
            // Stalwart virtue: immune to sanity loss this battle
            if (delta < 0 && _stalwartActive) return;

            // Shadow: unaffected by ally deaths (loner trait)
            if (delta < 0 && reason == "AllyDied"
                && unitData != null && unitData.ImmuneToAllyDeathSanityLoss)
                return;

            // Herald: double sanity loss from ally-related events (empathic)
            if (delta < 0 && unitData != null && unitData.AllySanityLossMultiplier > 1f)
            {
                if (reason == "AllyDied" || reason == "WitnessLost" || reason == "ProlongedCombat")
                    delta = Mathf.RoundToInt(delta * unitData.AllySanityLossMultiplier);
            }

            // Comprehension (GDD §5.3): eldritch-source damage scaled by class multiplier.
            // High-Comprehension units understand the horror and suffer more.
            if (delta < 0 && IsEldritchSource(reason))
                delta = Mathf.RoundToInt(delta * Comprehension);

            int oldSanity = CurrentSanity;
            CurrentSanity = Mathf.Clamp(CurrentSanity + delta, 0, MaxSanity);

            EventBus.Publish(new SanityChangedEvent
            {
                UnitId     = UnitId,
                UnitName   = UnitName,
                OldSanity  = oldSanity,
                NewSanity  = CurrentSanity,
                Reason     = reason
            });

            // Roll a stress trait the first time sanity crosses below StressedMin (50)
            if (!_stressTraitRolled
                && oldSanity >= SanitySystem.StressedMin
                && CurrentSanity < SanitySystem.StressedMin)
            {
                _stressTraitRolled = true;
                RollStressTrait();
            }

            // Check for Lost (sanity = 0)
            if (CurrentSanity == 0 && oldSanity > 0)
                OnLost();
        }

        private void RollStressTrait()
        {
            var (affliction, virtue) = SanitySystem.RollStressTrait(IsVeteran);

            if (virtue != VirtueType.None)
            {
                ActiveVirtue = virtue;

                if (virtue == VirtueType.Stalwart)
                    _stalwartActive = true;

                if (virtue == VirtueType.Courageous)
                    _courageousAuraCooldown = 0f; // Trigger aura immediately

                if (virtue == VirtueType.Resolute)
                    _resoluteTimer = ResoluteShieldDuration;

                EventBus.Publish(new VirtueGainedEvent
                {
                    UnitId     = UnitId,
                    UnitName   = UnitName,
                    VirtueName = virtue.ToString()
                });

                Debug.Log($"[Sanity] {UnitName} gained Virtue: {virtue}");
            }
            else if (affliction != AfflictionType.None)
            {
                ActiveAffliction = affliction;

                EventBus.Publish(new AfflictionGainedEvent
                {
                    UnitId          = UnitId,
                    UnitName        = UnitName,
                    AfflictionName  = affliction.ToString()
                });

                Debug.Log($"[Sanity] {UnitName} gained Affliction: {affliction}");
            }
        }

        /// <summary>Called by BattleManager when an ally on this unit's team is defeated.</summary>
        public void OnWitnessAllyDeath(UnitController deadAlly)
        {
            ModifySanity(-15, "AllyDied");
        }

        /// <summary>Called by BattleManager when ANY unit is Lost (sanity = 0). Hits everyone.</summary>
        public void OnWitnessUnitLost()
        {
            ModifySanity(-20, "WitnessLost");
        }

        /// <summary>Called by BattleManager at end of a won battle.</summary>
        public void OnBattleVictory()
        {
            ModifySanity(10, "Victory");
        }

        /// <summary>Called when this unit kills the rival who previously killed one of its allies.</summary>
        public void OnKilledRival()
        {
            ModifySanity(25, "KilledRival");
        }

        /// <summary>Called after being saved by a Mercy Token.</summary>
        public void OnSavedByMercy()
        {
            ModifySanity(15, "MercySaved");
            CurrentHP = Mathf.RoundToInt(MaxHP * 0.30f);
        }

        private void OnLost()
        {
            EventBus.Publish(new UnitLostEvent
            {
                UnitId   = UnitId,
                UnitName = UnitName,
                UnitType = UnitType
            });

            Debug.Log($"[Sanity] {UnitName} is LOST — consumed by madness.");
            OnDeath(null); // Treat as permanent death
        }

        private void BoostNearbyAllySanity(int amount, float range)
        {
            if (battleContext == null) return;
            foreach (var ally in battleContext.Allies)
            {
                if (ally == null || !ally.IsAlive || ally == this) continue;
                if (Vector3.Distance(transform.position, ally.transform.position) <= range)
                    ally.ModifySanity(amount, "CourageousAura");
            }
        }

        // ════════════════════════════════════════════
        // COMBAT METHODS (called by BT action nodes)
        // ════════════════════════════════════════════

        public bool CanAttack() => attackTimer <= 0f;

        public void PerformAttack(UnitController target)
        {
            if (!CanAttack() || target == null || !target.IsAlive) return;

            int damage = Mathf.Max(1, AttackDamage - target.Armour);

            // Focused virtue: +25% damage
            if (ActiveVirtue == VirtueType.Focused)
                damage = Mathf.RoundToInt(damage * 1.25f);

            // Gambit / Directive damage multiplier (Unleash, Reckless Abandon, etc.)
            if (GambitDamageMultiplier != 1f)
                damage = Mathf.RoundToInt(damage * GambitDamageMultiplier);

            target.TakeDamage(damage, this);
            attackTimer = attackCooldown;

            RecordAction("Attack", target.UnitId);

            EventBus.Publish(new UnitActionEvent
            {
                UnitId     = UnitId,
                ActionName = "Attack",
                Position   = transform.position,
                TargetId   = target.UnitId
            });

            // Berserker: gains sanity from kills
            if (!target.IsAlive && unitData != null && unitData.SanityOnKill > 0)
                ModifySanity(unitData.SanityOnKill, "KilledEnemy");
        }

        public void ResetAttackCooldown() => attackTimer = attackCooldown;

        public void TakeDamage(int damage, UnitController attacker)
        {
            if (!IsAlive) return;

            // Resolute virtue: cannot drop below 1 HP while shield is active
            if (ActiveVirtue == VirtueType.Resolute && _resoluteTimer > 0f)
                damage = Mathf.Min(damage, CurrentHP - 1);

            CurrentHP = Mathf.Max(0, CurrentHP - damage);

            if (!IsAlive)
                OnDeath(attacker);
        }

        public void Heal(int amount)
        {
            if (!IsAlive) return;
            // Vessel cannot be healed — it is sustained by something else
            if (unitData != null && unitData.CannotBeHealed) return;
            CurrentHP = Mathf.Min(MaxHP, CurrentHP + amount);
        }

        private void OnDeath(UnitController killedBy)
        {
            RecordAction("Defeated", killedBy?.UnitId ?? -1);

            EventBus.Publish(new UnitDefeatedEvent
            {
                UnitId           = UnitId,
                UnitName         = UnitName,
                UnitType         = UnitType,
                TeamId           = TeamId,
                DefeatedByUnitId = killedBy?.UnitId ?? -1
            });

            gameObject.SetActive(false);
        }

        private void RecordAction(string action, int targetId)
        {
            ActionHistory.Add(new UnitActionRecord
            {
                Timestamp = Time.time,
                Action    = action,
                Position  = transform.position,
                TargetId  = targetId
            });
        }

        /// <summary>Override the default behaviour tree (used by the Card system).</summary>
        public void SetBehaviourTree(BTNode tree) => behaviourTree = tree;

        // ════════════════════════════════════════════
        // GAMBIT INJECTION (GDD §4.1)
        // ════════════════════════════════════════════

        /// <summary>
        /// Inject up to two Pre-Built Gambits at the top of this unit's behaviour tree.
        /// Gambit 1 has the highest priority; Gambit 2 is tried if Gambit 1 fails.
        /// The unit's default class behaviour remains as a fallback below both gambits.
        /// </summary>
        public void SetGambits(BTNode gambit1, BTNode gambit2 = null)
        {
            // Rebuild: [Gambit1] → [Gambit2] → [Default class BT]
            BTNode defaultTree = BTPresets.GetPreset(unitData != null ? unitData.UnitType : "warden");

            if (gambit1 == null && gambit2 == null)
            {
                behaviourTree = defaultTree;
                return;
            }

            var children = new System.Collections.Generic.List<BTNode>();
            if (gambit1 != null) children.Add(gambit1);
            if (gambit2 != null) children.Add(gambit2);
            children.Add(defaultTree);

            behaviourTree = new KindredSiege.AI.BehaviourTree.Selector("GambitRoot", children.ToArray());
        }

        // ════════════════════════════════════════════
        // HORROR RATING (GDD §6.3)
        // ════════════════════════════════════════════

        /// <summary>
        /// Called by BattleManager every 5 seconds when a rival with Horror Rating is present.
        /// Drain is multiplied by this unit's Comprehension stat.
        /// </summary>
        public void ApplyHorrorRatingDrain(int baseDrain, string rivalName)
        {
            if (baseDrain <= 0) return;
            int actual = Mathf.RoundToInt(baseDrain * Comprehension);
            ModifySanity(-actual, "HorrorRatingAura");

            EventBus.Publish(new HorrorRatingDrainEvent
            {
                UnitId     = UnitId,
                SanityLost = actual,
                RivalName  = rivalName
            });
        }

        // ════════════════════════════════════════════
        // HELPERS
        // ════════════════════════════════════════════

        /// <summary>
        /// Returns true if the sanity damage source is eldritch in nature.
        /// These sources are scaled by the unit's Comprehension stat (GDD §5.3).
        /// </summary>
        private static bool IsEldritchSource(string reason)
        {
            return reason is "EldritchHit"
                         or "HorrorRatingAura"
                         or "DreadContest"
                         or "ForbiddenKnowledge"
                         or "PassiveDrain"    // Vessel — already doomed by eldritch means
                         or "RitualGambit";
        }
    }
}
