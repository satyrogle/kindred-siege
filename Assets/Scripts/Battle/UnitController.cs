using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KindredSiege.AI.BehaviourTree;
using KindredSiege.Core;
using KindredSiege.Units;

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
        public int MaxHP          { get; internal set; }
        public int CurrentHP      { get; internal set; }
        public int AttackDamage   { get; internal set; }
        public float AttackRange  { get; internal set; }
        public float MoveSpeed    { get; internal set; }
        public int Armour         { get; internal set; }

        public bool DirectiveOverrideActive { get; set; } = false;

        // ─── Sanity ───
        public int MaxSanity      { get; internal set; }
        public int CurrentSanity  { get; internal set; }

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
        public string    UnitType  => unitData != null ? unitData.UnitType : "warden";
        public string    UnitName  => unitData != null ? unitData.UnitName : "Unit";
        public UnitClass UnitClass => unitData != null ? unitData.UnitClass : UnitClass.None;
        public UnitData  Data      => unitData;

        public bool IsTargetable  => IsAlive && _shadowVanishedTimer <= 0f;
        public bool IsOnShrine    => _currentHazard == HazardType.Shrine;

        public int TeamId  { get; private set; }
        public int UnitId  { get; private set; }

        // ─── Comprehension (GDD §5.3) ───
        // Applied as a multiplier to all eldritch-source sanity damage.
        public float Comprehension => unitData != null ? Mathf.Max(0.1f, unitData.Comprehension + TalentComprehensionMod) : 1f;

        // ─── Phobia (GDD §5.5) ───
        public PhobiaType ActivePhobia => unitData != null ? unitData.ActivePhobia : PhobiaType.None;

        // SolitudePhobia + DarkPhobia: interval timers tracked per unit at runtime
        private float _solitudePhobiaTimer = 0f;
        private float _darkPhobiaTimer     = 0f;
        private const float SolitudePhobiaInterval = 5f;
        private const float DarkPhobiaInterval     = 10f;

        // ─── Fatigue (GDD §11.4) ───
        // Extra hesitation chance applied on top of sanity hesitation when unit is exhausted.
        public float ExtraHesitationFromFatigue { get; set; } = 0f;

        // ─── Reactive Core Skills (GDD §8.1) ───
        private int   _bloodRageStacks        = 0;
        private float _shadowVanishedTimer    = 0f;
        private bool  _shadowVanishUsed       = false;
        private bool  _heraldPulseUsed        = false;
        private bool  _vesselDeathDeniedUsed  = false;

        // ─── Environmental Hazards (GDD §12) ───
        private float     _baseMoveSpeed  = 3f;
        private HazardType _currentHazard = HazardType.None;
        private float     _hazardTimer    = 0f;

        // ─── Gambit / Directive state ───
        // Gambits and Directives can set these to modify combat behaviour.
        public float GambitDamageMultiplier { get; set; } = 1f;
        public bool  GambitIgnoreRetreat    { get; set; } = false;

        // Spawn position — recorded in Initialise. Used by HoldTheLine gambit.
        public Vector3 SpawnPosition { get; set; }

        // Set by DirectiveSystem when FocusFire is active; injected into BT blackboard each tick.
        public UnitController ForcedTarget { get; set; }

        // ─── Prolonged combat drain (GDD §5.2: -3 per round after round 5) ───
        private float _combatTimer    = 0f;
        private int   _drainRound     = 0;
        private const float SecondsPerRound    = 5f;
        private const int   RoundsBeforeDrain  = 5;

        // ─── Dread Contest (GDD §6.2) ───
        private float _hesitationLockTimer = 0f;   // > 0 = stunned, cannot act

        // ─── Talent flags (GDD §9) — set by TalentSystem.ApplyTalents() ───
        // Psychological branch
        public bool  TalentImmuneFirstAffliction { get; set; } = false;
        public int   TalentAllyDeathSanityAmount { get; set; } = -15;   // default −15
        public bool  TalentArmourSanityAbsorb    { get; set; } = false;
        public bool  TalentHorrorAura            { get; set; } = false;
        public bool  TalentMentalFortress        { get; set; } = false;
        public float TalentDreadDamageReduction  { get; set; } = 0f;
        public bool  TalentSolitudeImmune        { get; set; } = false;
        public bool  TalentPhobiaRollResist      { get; set; } = false;
        public bool  TalentImmuneEldritchHit     { get; set; } = false;
        public bool  TalentPassiveDrainHalved    { get; set; } = false;
        public bool  TalentDrainStartsLate       { get; set; } = false;
        // Combat branch
        public bool  TalentHeroicSacrifice       { get; set; } = false;
        public bool  TalentDeathBlessing         { get; set; } = false;
        public bool  TalentTauntOnStart          { get; set; } = false;
        public bool  TalentFrenzy                { get; set; } = false;
        public bool  TalentUndyingRage           { get; set; } = false;
        public bool  _undyingRageUsed                          = false;
        public bool  TalentDeathBurst            { get; set; } = false;
        public bool  TalentDeathDeniedAllyBoost  { get; set; } = false;
        public bool  TalentTwoDeathDenied        { get; set; } = false;
        public float TalentDeathDeniedChance     { get; set; } = 0.20f;
        public bool  TalentSecondVanish          { get; set; } = false;
        public float TalentVanishThreshold       { get; set; } = 0.40f;
        public bool  TalentVanishRecharge        { get; set; } = false;
        public float _vanishRechargeTimer                      = 0f;
        public bool  TalentSilentStrike          { get; set; } = false;
        public float TalentBackstabBonus         { get; set; } = 0f;
        public int   TalentBloodRageMaxStacks    { get; set; } = 5;
        public int   TalentBloodRageStartStacks  { get; set; } = 0;
        public bool  TalentBloodRagePersists     { get; set; } = false;
        public float TalentHitIgnoreChance       { get; set; } = 0f;
        public int   TalentSanityOnKillBonus     { get; set; } = 0;
        public float TalentInsightFlashChance    { get; set; } = 0.20f;
        public bool  TalentFKRateHalved          { get; set; } = false;
        public bool  TalentFKRecoveryFree        { get; set; } = false;
        public int   TalentFKCapBonus            { get; set; } = 0;
        public bool  TalentTeamRevealBonus       { get; set; } = false;
        public bool  TalentBattleStartAnalyse    { get; set; } = false;
        public float TalentIgnoreArmourChance    { get; set; } = 0f;
        public bool  TalentExecute               { get; set; } = false;
        public bool  _executeUsed                              = false;
        public bool  TalentReducedFatigue        { get; set; } = false;
        public float TalentPsychicRecoilChance   { get; set; } = 0.25f;
        public int   TalentPsychicRecoilDamage   { get; set; } = 5;
        public float TalentPsychicRecoilStun     { get; set; } = 0f;
        public bool  TalentRecoilChain           { get; set; } = false;
        public bool  TalentMaddeningAura         { get; set; } = false;
        public bool  TalentFocusedDamageBonus    { get; set; } = false;
        public int   TalentHeraldPulseBonus      { get; set; } = 0;
        public bool  TalentSecondHeraldPulse     { get; set; } = false;
        public float TalentHealAmplify           { get; set; } = 1f;
        public bool  TalentPulseGlobal           { get; set; } = false;
        public bool  TalentSustainingAura        { get; set; } = false;
        public bool  TalentEldritchAnchor        { get; set; } = false;
        public float TalentComprehensionMod      { get; set; } = 0f;   // negative = less horror
        public float AttackCooldownMod           { get; set; } = 1f;   // applied to attackCooldown
        public float _sustainingAuraTimer                      = 0f;

        // ─── Bond (GDD §Unit Bonds) ───────────────────────────────────────────
        /// <summary>Runtime display names of all active bonded partners in this battle.</summary>
        public HashSet<string> ActiveBondPartners { get; private set; } = new();
        /// <summary>Total damage bonus from all live bonds. Decreased when a partner dies.</summary>
        public float  ActiveBondDamageBonus { get; set; } = 0f;

        // ─── Mutations ───
        private float _fearIsPowerBonusTimer = 0f;

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

            // Sanity init — MaxSanityPenalty reduces the ceiling permanently (GDD §5.4)
            MaxSanity     = Mathf.Max(1, data.BaseSanity - data.MaxSanityPenalty);
            CurrentSanity = MaxSanity;
            ActiveAffliction = AfflictionType.None;
            ActiveVirtue     = VirtueType.None;
            _stressTraitRolled   = false;
            _stalwartActive      = false;
            _resoluteTimer       = 0f;
            _courageousAuraCooldown = 0f;
            _combatTimer = 0f;
            _drainRound  = 0;

            // Auto-derive UnitClass from UnitType string if not set in Inspector
            if (data.UnitClass == UnitClass.None && !string.IsNullOrEmpty(data.UnitType))
                data.UnitClass = UnitTypeToClass(data.UnitType);

            // Build default behaviour tree for this class
            behaviourTree = BTPresets.GetPreset(data.UnitClass);

            // Record spawn position for HoldTheLine gambit
            SpawnPosition = transform.position;

            // Reset gambit / directive modifiers
            GambitDamageMultiplier = 1f;
            GambitIgnoreRetreat    = false;
            ForcedTarget           = null;

            // Reset phobia timers
            _solitudePhobiaTimer  = 0f;
            _darkPhobiaTimer      = 0f;
            ExtraHesitationFromFatigue = 0f;

            // Reactive skills reset
            _bloodRageStacks = 0;
            _shadowVanishedTimer = 0f;
            _shadowVanishUsed = false;
            _heraldPulseUsed = false;
            _hesitationLockTimer = 0f;
            _vesselDeathDeniedUsed = false;

            // Hazard state reset
            _baseMoveSpeed = data.MoveSpeed;
            _currentHazard = HazardType.None;
            _hazardTimer   = 0f;

            // Talent runtime flags reset
            TalentImmuneFirstAffliction = false;
            TalentAllyDeathSanityAmount = -15;
            TalentArmourSanityAbsorb    = false;
            TalentHorrorAura            = false;
            TalentMentalFortress        = false;
            TalentDreadDamageReduction  = 0f;
            TalentSolitudeImmune        = false;
            TalentPhobiaRollResist      = false;
            TalentImmuneEldritchHit     = false;
            TalentPassiveDrainHalved    = false;
            TalentDrainStartsLate       = false;
            TalentHeroicSacrifice       = false;
            TalentDeathBlessing         = false;
            TalentTauntOnStart          = false;
            TalentFrenzy                = false;
            TalentUndyingRage           = false;
            _undyingRageUsed            = false;
            TalentDeathBurst            = false;
            TalentDeathDeniedAllyBoost  = false;
            TalentTwoDeathDenied        = false;
            TalentDeathDeniedChance     = 0.20f;
            TalentSecondVanish          = false;
            TalentVanishThreshold       = 0.40f;
            TalentVanishRecharge        = false;
            _vanishRechargeTimer        = 0f;
            TalentSilentStrike          = false;
            TalentBackstabBonus         = 0f;
            TalentBloodRageMaxStacks    = 5;
            TalentBloodRageStartStacks  = 0;
            TalentBloodRagePersists     = false;
            TalentHitIgnoreChance       = 0f;
            TalentSanityOnKillBonus     = 0;
            TalentInsightFlashChance    = 0.20f;
            TalentFKRateHalved          = false;
            TalentFKRecoveryFree        = false;
            TalentFKCapBonus            = 0;
            TalentTeamRevealBonus       = false;
            TalentBattleStartAnalyse    = false;
            TalentIgnoreArmourChance    = 0f;
            TalentExecute               = false;
            _executeUsed                = false;
            TalentReducedFatigue        = false;
            TalentPsychicRecoilChance   = 0.25f;
            TalentPsychicRecoilDamage   = 5;
            TalentPsychicRecoilStun     = 0f;
            TalentRecoilChain           = false;
            TalentMaddeningAura         = false;
            TalentFocusedDamageBonus    = false;
            TalentHeraldPulseBonus      = 0;
            TalentSecondHeraldPulse     = false;
            TalentHealAmplify           = 1f;
            TalentPulseGlobal           = false;
            TalentSustainingAura        = false;
            TalentEldritchAnchor        = false;
            TalentComprehensionMod      = 0f;
            AttackCooldownMod           = 1f;
            _sustainingAuraTimer        = 0f;

            // Bond fields reset each battle (ApplyBondEffects re-sets them)
            BondedPartnerName     = null;
            ActiveBondDamageBonus = 0f;

            // Apply talent stat boosts and set runtime flags
            TalentSystem.ApplyTalents(this);

            // Post-talent: apply cooldown modifier and blood rage start stacks
            attackCooldown  = data.AttackCooldown * AttackCooldownMod;
            _bloodRageStacks = TalentBloodRagePersists
                ? Mathf.Max(_bloodRageStacks, TalentBloodRageStartStacks)
                : TalentBloodRageStartStacks;

            transform.localScale = Vector3.one * data.ModelScale;
            ActionHistory.Clear();
        }

        /// <summary>Apply building/upgrade stat multipliers before battle starts.</summary>
        public void ApplyModifiers(float hpMult = 1f, float dmgMult = 1f, float speedMult = 1f)
        {
            MaxHP         = Mathf.RoundToInt(unitData.MaxHP * hpMult);
            CurrentHP     = MaxHP;
            AttackDamage  = Mathf.RoundToInt(unitData.AttackDamage * dmgMult);
            MoveSpeed     = unitData.MoveSpeed * speedMult;
            _baseMoveSpeed = MoveSpeed; // Update reference so hazard multiplier stays correct
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

            // ── Hesitation lock from Dread Contest (GDD §6.2) ──
            if (_hesitationLockTimer > 0f)
            {
                _hesitationLockTimer -= dt;
                return; // Cannot act while stunned
            }
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

            // ── Phobia tick effects (GDD §5.5) ──
            TickPhobiaEffects(dt);

            // ── Virtue timers ──
            if (_resoluteTimer > 0f)
                _resoluteTimer -= dt;

            // ── Shadow Vanish timer ──
            if (_shadowVanishedTimer > 0f)
            {
                _shadowVanishedTimer -= dt;
                // Talent: Vanish Recharge — resets vanish after 8s cooldown
                if (_shadowVanishedTimer <= 0f && TalentVanishRecharge && _shadowVanishUsed)
                    _vanishRechargeTimer = 8f;
            }
            if (_vanishRechargeTimer > 0f)
            {
                _vanishRechargeTimer -= dt;
                if (_vanishRechargeTimer <= 0f)
                    _shadowVanishUsed = false; // Vanish recharged
            }

            // ── Talent: Sustaining Aura (Vessel) — allies within 2 units regen 1 HP per 2s ──
            if (TalentSustainingAura)
            {
                _sustainingAuraTimer -= dt;
                if (_sustainingAuraTimer <= 0f)
                {
                    _sustainingAuraTimer = 2f;
                    var allies = battleContext?.Allies;
                    if (allies != null)
                    {
                        foreach (var ally in allies)
                        {
                            if (ally != null && ally.IsAlive && ally != this &&
                                Vector3.Distance(transform.position, ally.transform.position) <= 2f)
                                ally.Heal(1);
                        }
                    }
                }
            }

            // ── Mutation: Fear Is Power timer ──
            if (_fearIsPowerBonusTimer > 0f)
                _fearIsPowerBonusTimer -= dt;

            // ── Mutation: The Deep Calls (Tide) ──
            if (KindredSiege.Modifiers.MutationEngine.Instance != null &&
                KindredSiege.Modifiers.MutationEngine.Instance.HasMutation(KindredSiege.Modifiers.MutationType.TheDeepCalls))
            {
                Vector3 toCenter = (Vector3.zero - transform.position).normalized;
                transform.position += toCenter * 0.5f * dt;
            }

            // ── Environmental Hazard effects (GDD §12) ──
            if (battleContext.Grid != null)
            {
                HazardType hazard = battleContext.Grid.GetHazardAt(transform.position);

                // On hazard change — update movement speed multiplier
                if (hazard != _currentHazard)
                {
                    _currentHazard = hazard;
                    MoveSpeed = hazard == HazardType.DeepWater
                        ? _baseMoveSpeed * 0.5f
                        : _baseMoveSpeed;
                }

                // Per-second sanity tick
                _hazardTimer += dt;
                if (_hazardTimer >= 1f)
                {
                    _hazardTimer = 0f;
                    switch (_currentHazard)
                    {
                        case HazardType.DeepWater:
                            ModifySanity(-3, "DeepWater");
                            break;
                        case HazardType.Shrine:
                            ModifySanity(2, "Shrine");
                            break;
                        case HazardType.EldritchGround:
                            ModifySanity(-2, "EldritchGround");
                            break;
                    }
                }
            }

            // ── Marksman Snap Shot ──
            if (UnitClass == UnitClass.Marksman && CanAttack() && battleContext.Enemies != null)
            {
                var snapTarget = battleContext.Enemies.FirstOrDefault(e => e != null && e.IsTargetable && e != this &&
                    Vector3.Distance(transform.position, e.transform.position) <= AttackRange);
                if (snapTarget != null)
                {
                    PerformAttack(snapTarget);
                }
            }

            if (ActiveVirtue == VirtueType.Courageous)
            {
                _courageousAuraCooldown -= dt;
                if (_courageousAuraCooldown <= 0f)
                {
                    _courageousAuraCooldown = CourageousAuraInterval;
                    BoostNearbyAllySanity(amount: 10, range: 5f);
                }
            }

            // ── Sanity hesitation (GDD §5.1) + fatigue hesitation (GDD §11.4) ──
            float hesitation = SanitySystem.GetHesitationChance(SanityState) + ExtraHesitationFromFatigue;

            // Mutation: Clarity In Pain (Mind)
            if (KindredSiege.Modifiers.MutationEngine.Instance != null &&
                KindredSiege.Modifiers.MutationEngine.Instance.HasMutation(KindredSiege.Modifiers.MutationType.ClarityInPain) &&
                CurrentSanity < 25)
            {
                hesitation = 0f;
            }

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

            // ── Directive override: skip BT when a directive coroutine is controlling this unit ──
            if (DirectiveOverrideActive) return;

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
            // Talent: Armour Sanity Absorb (Warden F3) — first 5 sanity damage blocked by armour, once
            if (delta < 0 && TalentArmourSanityAbsorb && Armour > 0)
            {
                TalentArmourSanityAbsorb = false; // consume once per battle
                delta = Mathf.Min(0, delta + 5);  // absorb up to 5
                if (delta == 0) return;
            }

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
            {
                float comp = Comprehension;
                // EldritchPhobia: +0.5 to effective Comprehension for all eldritch hits
                if (ActivePhobia == PhobiaType.EldritchPhobia)
                    comp += 0.5f;
                // EldritchGround (GDD §12): standing on corrupted earth doubles comp multiplier
                if (_currentHazard == HazardType.EldritchGround)
                    comp *= 2f;
                delta = Mathf.RoundToInt(delta * comp);
            }

            int oldSanity = CurrentSanity;
            CurrentSanity = Mathf.Clamp(CurrentSanity + delta, 0, MaxSanity);

            // Mutation: Fear Is Power (Mind)
            if (delta < 0 && KindredSiege.Modifiers.MutationEngine.Instance != null &&
                KindredSiege.Modifiers.MutationEngine.Instance.HasMutation(KindredSiege.Modifiers.MutationType.FearIsPower))
            {
                _fearIsPowerBonusTimer = 5f;
            }

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
            // Talent: Immune to first Affliction roll per battle
            if (TalentImmuneFirstAffliction)
            {
                TalentImmuneFirstAffliction = false; // consume once
                Debug.Log($"[Talent] {UnitName} resisted Affliction roll (Immune First Affliction talent).");
                return;
            }

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
            // Bond: "For them" — if this was the bonded partner, gain sanity instead of losing it
            string deadName = deadAlly?.UnitName;
            if (!string.IsNullOrEmpty(deadName) &&
                KindredSiege.Units.BondSystem.NotifyPartnerDied(this, deadName))
                return; // Bond reaction fired — skip normal ally-death loss

            // Talent: AllyDeathSanityAmount — default -15, reduced by Warden/Berserker/Herald talents
            ModifySanity(TalentAllyDeathSanityAmount, "AllyDied");

            // BloodPhobia: witnessing any death deals an extra -8 sanity
            if (ActivePhobia == PhobiaType.BloodPhobia)
                ModifySanity(-8, "BloodPhobia");
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

            int effectiveArmour = target.Armour;
            float rivalDamageMult = 1f;

            // --- Rival Memory Traits (Adaptive Counter-play) ---
            var rival = KindredSiege.Battle.BattleManager.Instance?.GetActiveRival();
            if (rival != null && UnitName == rival.FullName)
            {
                if (target.UnitClass == UnitClass.Warden && rival.Traits.Contains(KindredSiege.Rivalry.RivalTraitType.VanguardSlayer))
                    rivalDamageMult += 0.20f;
                if (target.UnitClass == UnitClass.Warden && rival.Traits.Contains(KindredSiege.Rivalry.RivalTraitType.WardenBreaker))
                    effectiveArmour = 0; // Breaker ignores Warden armour entirely
            }

            // Talent: Ignore Armour chance
            if (TalentIgnoreArmourChance > 0f && Random.value < TalentIgnoreArmourChance)
                effectiveArmour = 0;

            int damage = Mathf.Max(1, AttackDamage - effectiveArmour);

            // Berserker (Blood Rage): +5% damage per stack
            if (UnitClass == UnitClass.Berserker && _bloodRageStacks > 0)
                damage = Mathf.RoundToInt(damage * (1f + 0.05f * _bloodRageStacks));

            // Talent: Frenzy (+25% damage below 30% HP)
            if (TalentFrenzy && (float)CurrentHP / MaxHP < 0.30f)
                damage = Mathf.RoundToInt(damage * 1.25f);

            // Talent: Focused Damage Bonus (Occultist, sanity > 70)
            if (TalentFocusedDamageBonus && CurrentSanity > 70)
                damage = Mathf.RoundToInt(damage * 1.10f);

            // Bond: +8% damage while bonded partner is alive
            if (ActiveBondDamageBonus > 0f)
                damage = Mathf.RoundToInt(damage * (1f + ActiveBondDamageBonus));

            // Focused virtue: +25% damage
            if (ActiveVirtue == VirtueType.Focused)
                damage = Mathf.RoundToInt(damage * 1.25f);

            // Gambit / Directive damage multiplier (Unleash, Reckless Abandon, etc.)
            if (GambitDamageMultiplier != 1f)
                damage = Mathf.RoundToInt(damage * GambitDamageMultiplier);

            // Rival Adaptive Trait modifier
            if (rivalDamageMult != 1f)
                damage = Mathf.RoundToInt(damage * rivalDamageMult);

            // Mutation: Fear Is Power physical damage bonus (+40%)
            if (_fearIsPowerBonusTimer > 0f)
                damage = Mathf.RoundToInt(damage * 1.40f);

            // Talent: Execute — auto-kill targets below 15% HP (once per battle)
            if (TalentExecute && !_executeUsed && (float)target.CurrentHP / target.MaxHP < 0.15f)
            {
                _executeUsed = true;
                damage = target.CurrentHP; // lethal
            }

            target.TakeDamage(damage, this);
            attackTimer = attackCooldown;

            // ViolencePhobia: every attack costs -2 sanity
            if (ActivePhobia == PhobiaType.ViolencePhobia)
                ModifySanity(-2, "ViolencePhobia");

            RecordAction("Attack", target.UnitId);

            EventBus.Publish(new UnitActionEvent
            {
                UnitId     = UnitId,
                ActionName = "Attack",
                Position   = transform.position,
                TargetId   = target.UnitId
            });

            // Berserker: gains sanity from kills
            if (!target.IsAlive)
            {
                // Base sanity-on-kill
                if (unitData != null && unitData.SanityOnKill > 0)
                    ModifySanity(unitData.SanityOnKill, "KilledEnemy");
                // Talent: extra sanity on kill bonus
                if (TalentSanityOnKillBonus > 0)
                    ModifySanity(TalentSanityOnKillBonus, "KilledEnemyTalent");
            }
        }

        public void ResetAttackCooldown() => attackTimer = attackCooldown;

        public void TakeDamage(int damage, UnitController attacker, bool isCounter = false)
        {
            if (!IsAlive) return;

            // Talent: Hit Ignore — 15% chance to negate the hit entirely
            if (TalentHitIgnoreChance > 0f && Random.value < TalentHitIgnoreChance)
                return;

            // Vessel (Death Denied): survive lethal hit (chance from talent or default 20%)
            bool canDeathDeny = UnitClass == UnitClass.Vessel && damage >= CurrentHP &&
                                (!_vesselDeathDeniedUsed || TalentTwoDeathDenied);
            if (canDeathDeny)
            {
                if (Random.value < TalentDeathDeniedChance)
                {
                    damage = CurrentHP - 1;
                    if (_vesselDeathDeniedUsed)
                        TalentTwoDeathDenied = false; // consume second use
                    _vesselDeathDeniedUsed = true;

                    // Talent: Death Denied ally boost
                    if (TalentDeathDeniedAllyBoost)
                        BoostNearbyAllySanity(8, 4f);
                }
            }

            // Talent: Undying Rage (Berserker)
            if (TalentUndyingRage && !_undyingRageUsed && damage >= CurrentHP)
            {
                if (Random.value < 0.50f) // 50% proc — not guaranteed like Vessel
                {
                    damage = CurrentHP - 1;
                    _undyingRageUsed = true;
                }
            }

            // Resolute virtue: cannot drop below 1 HP while shield is active
            if (ActiveVirtue == VirtueType.Resolute && _resoluteTimer > 0f)
                damage = Mathf.Min(damage, CurrentHP - 1);

            CurrentHP = Mathf.Max(0, CurrentHP - damage);

            // Mutation: Pain Is Shared (Flesh)
            if (damage > 0 && KindredSiege.Modifiers.MutationEngine.Instance != null &&
                KindredSiege.Modifiers.MutationEngine.Instance.HasMutation(KindredSiege.Modifiers.MutationType.PainIsShared))
            {
                var nearestAlly = battleContext?.Allies
                    .Where(a => a != null && a.IsAlive && a != this)
                    .OrderBy(a => Vector3.Distance(transform.position, a.transform.position))
                    .FirstOrDefault();

                if (nearestAlly != null)
                {
                    int sharedDmg = Mathf.RoundToInt(damage * 0.30f);
                    if (sharedDmg > 0)
                    {
                        nearestAlly.CurrentHP = Mathf.Max(0, nearestAlly.CurrentHP - sharedDmg);
                        if (!nearestAlly.IsAlive) nearestAlly.OnDeath(attacker);
                    }
                }
            }

            // Berserker (Blood Rage): +5% damage per hit taken
            if (UnitClass == UnitClass.Berserker && _bloodRageStacks < TalentBloodRageMaxStacks)
                _bloodRageStacks++;

            // Investigator (Insight Flash): 20% auto-analyse
            if (UnitClass == UnitClass.Investigator && CurrentSanity >= 2 && attacker != null && attacker.IsTargetable)
            {
                if (Random.value < 0.20f && battleContext != null)
                {
                    ModifySanity(-2, "InsightFlash");
                    battleContext.Set($"Analysed_{attacker.UnitId}", true);
                }
            }

            // Shadow (Vanish): threshold from talent (default 40%, raised to 50% with Hunter_3)
            if (UnitClass == UnitClass.Shadow && !_shadowVanishUsed && (float)CurrentHP / MaxHP < TalentVanishThreshold &&
                !(KindredSiege.Battle.BattleManager.Instance?.GetActiveRival()?.Traits.Contains(KindredSiege.Rivalry.RivalTraitType.ShadowCatcher) == true))
            {
                _shadowVanishUsed = true;
                _shadowVanishedTimer = 3f;
                // Talent: Second Vanish — resets after a cooldown via VanishRecharge, handled in TickAI
            }

            // Herald (Martyrdom Pulse): sanity boost to allies (talent adds +5)
            if (UnitClass == UnitClass.Herald && !_heraldPulseUsed && (float)CurrentHP / MaxHP < 0.50f)
            {
                _heraldPulseUsed = true;
                float range = TalentPulseGlobal ? 999f : 4f;
                BoostNearbyAllySanity(8 + TalentHeraldPulseBonus, range);
            }
            else if (UnitClass == UnitClass.Herald && _heraldPulseUsed && TalentSecondHeraldPulse && (float)CurrentHP / MaxHP < 0.25f)
            {
                // Second pulse at 25% HP threshold (consumed once)
                TalentSecondHeraldPulse = false;
                float range = TalentPulseGlobal ? 999f : 4f;
                BoostNearbyAllySanity(8 + TalentHeraldPulseBonus, range);
            }

            // Warden (Brace): 30% counter-attack
            if (!isCounter && UnitClass == UnitClass.Warden && attacker != null && attacker.IsTargetable)
            {
                float dist = Vector3.Distance(transform.position, attacker.transform.position);
                if (dist <= AttackRange && Random.value < 0.30f)
                    attacker.TakeDamage(Mathf.Max(1, AttackDamage - attacker.Armour), this, true);
            }

            // Eldritch Hit + Occultist (Psychic Recoil)
            if (attacker != null && attacker.TeamId != TeamId)
            {
                if (!TalentImmuneEldritchHit)
                    ModifySanity(-5, "EldritchHit");

                if (UnitClass == UnitClass.Occultist && Random.value < TalentPsychicRecoilChance)
                {
                    attacker.ModifySanity(-TalentPsychicRecoilDamage, "PsychicRecoil");

                    // Talent: Recoil Stun
                    if (TalentPsychicRecoilStun > 0f && Random.value < TalentPsychicRecoilStun)
                        attacker._hesitationLockTimer = Mathf.Max(attacker._hesitationLockTimer, 1f);

                    // Talent: Recoil Chain
                    if (TalentRecoilChain)
                    {
                        var chainTarget = battleContext?.Enemies
                            .Where(e => e != null && e.IsAlive && e != attacker)
                            .OrderBy(e => Vector3.Distance(attacker.transform.position, e.transform.position))
                            .FirstOrDefault();
                        chainTarget?.ModifySanity(-(TalentPsychicRecoilDamage / 2), "PsychicRecoilChain");
                    }
                }
                // Dread Contest is now periodic via DreadContestSystem, not on every hit
            }

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

            // Talent: Heroic Sacrifice — allies gain +10 sanity instead of losing −15
            // (Normal ally-death loss is handled in OnWitnessAllyDeath; this overrides the sign)
            if (TalentHeroicSacrifice && battleContext?.Allies != null)
            {
                foreach (var ally in battleContext.Allies)
                    if (ally != null && ally.IsAlive && ally != this)
                        ally.ModifySanity(10, "HeroicSacrifice");
            }

            // Talent: Death Blessing (Herald) — entire team +20 sanity
            if (TalentDeathBlessing && battleContext?.Allies != null)
            {
                foreach (var ally in battleContext.Allies)
                    if (ally != null && ally.IsAlive)
                        ally.ModifySanity(20, "DeathBlessing");
            }

            // Talent: Death Burst (Vessel) — deal 30 damage to nearest enemy
            if (TalentDeathBurst && battleContext?.Enemies != null)
            {
                var nearest = battleContext.Enemies
                    .Where(e => e != null && e.IsAlive)
                    .OrderBy(e => Vector3.Distance(transform.position, e.transform.position))
                    .FirstOrDefault();
                nearest?.TakeDamage(30, this);
            }

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
            BTNode defaultTree = BTPresets.GetPreset(unitData != null ? unitData.UnitClass : UnitClass.None);

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
        // DREAD CONTEST (GDD §9)
        // ════════════════════════════════════════════

        /// <summary>
        /// Triggered when struck by a high-ranking rival.
        /// </summary>
        /// <summary>
        /// GDD §6.2 Dread Contest formula:
        ///   Resistance = (CurrentSanity / 10) + Comprehension
        ///   Sanity Damage = max(0, DreadPower - Resistance) × 2
        ///   Damage > 20 → 3-second Hesitation Lock
        ///   Damage > 35 → phobia check (activates existing phobia or rolls new one)
        ///   Resist (damage = 0) → +2 sanity
        /// </summary>
        public void RollDreadContest(int dreadPower, string rivalName)
        {
            if (!IsAlive) return;

            // GDD formula
            float resistance = (CurrentSanity / 10f) + Comprehension;
            int sanityDamage = Mathf.Max(0, Mathf.RoundToInt((dreadPower - resistance) * 2f));

            // Talent: Psychic Shield (Investigator) — reduce dread damage by 30%
            if (TalentDreadDamageReduction > 0f && sanityDamage > 0)
                sanityDamage = Mathf.Max(1, Mathf.RoundToInt(sanityDamage * (1f - TalentDreadDamageReduction)));

            bool hesitationLock  = false;
            bool phobiaTriggered = false;

            if (sanityDamage > 0)
            {
                ModifySanity(-sanityDamage, "DreadContest");

                // Damage > 20: 3-second Hesitation Lock (unless Mental Fortress talent)
                if (sanityDamage > 20 && !TalentMentalFortress)
                {
                    hesitationLock = true;
                    _hesitationLockTimer = 3f;
                    var rend = GetComponent<Renderer>();
                    if (rend != null) StartCoroutine(StunVisualCoroutine(rend));
                }

                // Damage > 35: phobia check
                if (sanityDamage > 35)
                {
                    phobiaTriggered = true;
                    if (ActivePhobia == PhobiaType.None)
                        TraumaPhobiaSystem.ForceRollPhobia(this);
                    // Existing phobia is already ticking — no extra action needed
                }

                Debug.Log($"[Dread] {UnitName} FAILED vs {rivalName} — Res:{resistance:F1} Pwr:{dreadPower} Dmg:{sanityDamage}" +
                          (hesitationLock ? " [STUNNED]" : "") + (phobiaTriggered ? " [PHOBIA]" : ""));
            }
            else
            {
                // Resisted — small sanity boost
                ModifySanity(2, "DreadContestResisted");
                Debug.Log($"[Dread] {UnitName} RESISTED {rivalName} — Res:{resistance:F1} vs Pwr:{dreadPower}");
            }

            EventBus.Publish(new DreadContestEvent
            {
                UnitId          = UnitId,
                UnitName        = UnitName,
                RivalName       = rivalName,
                DreadPower      = dreadPower,
                Resistance      = Mathf.RoundToInt(resistance),
                SanityDamage    = sanityDamage,
                HesitationLock  = hesitationLock,
                PhobiaTriggered = phobiaTriggered
            });
        }

        private System.Collections.IEnumerator StunVisualCoroutine(Renderer renderer)
        {
            var originalColor = renderer.material.color;
            renderer.material.color = Color.magenta;
            yield return new WaitForSeconds(3f);
            if (this != null && IsAlive)
                renderer.material.color = originalColor;
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
            // Shrine protects — the rival's presence cannot reach those who stand on holy ground
            if (IsOnShrine) return;
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
        // FORBIDDEN KNOWLEDGE (GDD §5.4)
        // ════════════════════════════════════════════

        /// <summary>
        /// Permanently lower this unit's MaxSanity ceiling by <paramref name="amount"/>.
        /// Called by the ForbiddenScan gambit each time a new enemy is analysed.
        /// The penalty persists on UnitData between battles. Recovery requires Full Rest
        /// at the city Apothecary (FatigueSystem.Rest with recoverForbiddenKnowledge = true).
        /// </summary>
        public void ApplyForbiddenKnowledge(int amount)
        {
            if (amount <= 0 || unitData == null) return;

            unitData.MaxSanityPenalty = Mathf.Min(unitData.MaxSanityPenalty + amount,
                unitData.BaseSanity - 1); // Always leave at least 1 MaxSanity

            int newMax = Mathf.Max(1, unitData.BaseSanity - unitData.MaxSanityPenalty);
            MaxSanity = newMax;

            // Clamp current sanity to the new ceiling
            if (CurrentSanity > MaxSanity)
                CurrentSanity = MaxSanity;

            EventBus.Publish(new ForbiddenKnowledgeEvent
            {
                UnitId        = UnitId,
                UnitName      = UnitName,
                MaxSanityLost = amount,
                NewMaxSanity  = MaxSanity,
                TotalPenalty  = unitData.MaxSanityPenalty
            });

            Debug.Log($"[ForbiddenKnowledge] {UnitName}: MaxSanity reduced by {amount} → {MaxSanity} " +
                      $"(total penalty: {unitData.MaxSanityPenalty})");
        }

        // ════════════════════════════════════════════
        // PHOBIA PROCESSING (GDD §5.5)
        // ════════════════════════════════════════════

        /// <summary>
        /// Tick interval-based phobia effects (SolitudePhobia, DarkPhobia).
        /// Called each TickAI() frame.
        /// </summary>
        private void TickPhobiaEffects(float dt)
        {
            switch (ActivePhobia)
            {
                case PhobiaType.SolitudePhobia:
                    _solitudePhobiaTimer += dt;
                    if (_solitudePhobiaTimer >= SolitudePhobiaInterval)
                    {
                        _solitudePhobiaTimer = 0f;
                        // Check if any ally is within 4 units
                        bool allyNearby = battleContext != null && battleContext.Allies != null &&
                            battleContext.Allies.Exists(a =>
                                a != null && a.IsAlive && a != this &&
                                Vector3.Distance(transform.position, a.transform.position) <= 4f);

                        if (!allyNearby)
                            ModifySanity(-3, "SolitudePhobia");
                    }
                    break;

                case PhobiaType.DarkPhobia:
                    _darkPhobiaTimer += dt;
                    if (_darkPhobiaTimer >= DarkPhobiaInterval)
                    {
                        _darkPhobiaTimer = 0f;
                        ModifySanity(-4, "DarkPhobia");
                    }
                    break;
            }
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
                         or "PassiveDrain"
                         or "RitualGambit";
        }

        /// <summary>
        /// Derive UnitClass from a UnitType string.
        /// Used during Initialise() when UnitData.UnitClass hasn't been set in the Inspector.
        /// </summary>
        private static UnitClass UnitTypeToClass(string unitType) => unitType?.ToLower() switch
        {
            "warden"       => UnitClass.Warden,
            "marksman"     => UnitClass.Marksman,
            "occultist"    => UnitClass.Occultist,
            "berserker"    => UnitClass.Berserker,
            "investigator" => UnitClass.Investigator,
            "shadow"       => UnitClass.Shadow,
            "herald"       => UnitClass.Herald,
            "vessel"       => UnitClass.Vessel,
            // Legacy aliases
            "guardian"     => UnitClass.Warden,
            "ranger"       => UnitClass.Marksman,
            "healer"       => UnitClass.Occultist,
            "scout"        => UnitClass.Shadow,
            "emissary"     => UnitClass.Vessel,
            _              => UnitClass.None
        };
    }
}
