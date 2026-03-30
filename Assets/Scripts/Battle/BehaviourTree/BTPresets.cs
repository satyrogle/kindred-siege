using KindredSiege.Battle;

namespace KindredSiege.AI.BehaviourTree
{
    /// <summary>
    /// Factory for the default behaviour tree of each unit class.
    /// These are the "instinct" layer — the baseline AI before any strategy cards are applied.
    ///
    /// ARCHITECTURE NOTE:
    /// Each tree follows the same top-level pattern:
    ///   Priority selector → specialised role behaviours → fallback basic combat
    ///
    /// The eight classes match GDD §7.1 exactly.
    /// Old names (Guardian/Ranger/Healer/Scout/Emissary) are kept as aliases for
    /// backwards compatibility with any existing ScriptableObject assets.
    /// </summary>
    public static class BTPresets
    {
        // ══════════════════════════════════════════════════════
        // PILLAR 1 CLASSES — core combat roles
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Warden (Tank/Frontline): Engage nearest, shield allies.
        /// High base sanity. Hard to break.
        /// Priority: low-HP retreat → engage nearest → move and attack.
        /// </summary>
        public static BTNode CreateWarden()
        {
            return new Selector("Warden_Root",
                // 1. Retreat if critically wounded (unless Hopeless affliction raises threshold)
                new Sequence("CriticalRetreat",
                    new Condition("NeedRetreat", ctx =>
                    {
                        float threshold = ctx.Get<bool>("Hopeless") ? 0.50f : 0.15f;
                        return (float)ctx.Owner.CurrentHP / ctx.Owner.MaxHP < threshold;
                    }),
                    new Retreat()
                ),
                // 2. Standard combat — engage the nearest threat
                new Sequence("EngageNearest",
                    new HasEnemies(),
                    new FindNearestEnemy(),
                    new Selector("ApproachOrAttack",
                        new Sequence("AttackIfInRange",
                            new IsTargetInRange(),
                            new AttackTarget()
                        ),
                        new MoveToTarget()
                    )
                )
            );
        }

        /// <summary>
        /// Marksman (Ranged DPS): Keep distance, focus weakest target.
        /// Medium sanity. Panics if enemies close.
        /// Priority: kite back if too close → focus weakest → move and shoot.
        /// </summary>
        public static BTNode CreateMarksman()
        {
            return new Selector("Marksman_Root",
                // 1. Kite if an enemy gets too close
                new Sequence("KiteBack",
                    new Condition("EnemyClose", ctx =>
                    {
                        var target = ctx.Get<UnitController>("Target");
                        if (target == null) return false;
                        float dist = UnityEngine.Vector3.Distance(
                            ctx.Owner.transform.position, target.transform.position);
                        return dist < ctx.Owner.AttackRange * 0.6f;
                    }),
                    new Retreat()
                ),
                // 2. Focus fire on the weakest enemy
                new Sequence("FocusWeakest",
                    new HasEnemies(),
                    new FindWeakestEnemy(),
                    new Selector("RangedAttack",
                        new Sequence("ShootIfInRange",
                            new IsTargetInRange(),
                            new AttackTarget()
                        ),
                        new MoveToTarget()
                    )
                )
            );
        }

        /// <summary>
        /// Occultist (Healer/Support): Prioritise healing, avoid direct combat.
        /// Low sanity — sees too much.
        /// Priority: heal wounded ally → retreat if threatened → basic combat fallback.
        /// </summary>
        public static BTNode CreateOccultist()
        {
            return new Selector("Occultist_Root",
                // 1. Heal most damaged ally
                new Sequence("HealDuty",
                    new HasWoundedAlly(0.6f),
                    new HealAlly(15)
                ),
                // 2. Self-preservation
                new Sequence("SelfPreservation",
                    new IsHealthBelow(0.3f),
                    new Retreat()
                ),
                // 3. Basic attack as fallback
                new Sequence("BasicCombat",
                    new HasEnemies(),
                    new FindNearestEnemy(),
                    new Selector("ApproachOrAttack",
                        new Sequence("AttackIfInRange",
                            new IsTargetInRange(),
                            new AttackTarget()
                        ),
                        new MoveToTarget()
                    )
                )
            );
        }

        /// <summary>
        /// Berserker (Melee DPS): Charge highest-HP target, never retreat.
        /// Feeds on violence — gains sanity from kills (set on UnitData.SanityOnKill).
        /// Pure aggression — no retreat logic.
        /// </summary>
        public static BTNode CreateBerserker()
        {
            return new Selector("Berserker_Root",
                // Single priority: hunt the biggest target and never stop
                new Sequence("RecklessCharge",
                    new HasEnemies(),
                    new FindHighestHPEnemy(),   // Targets the strongest, not the weakest
                    new Selector("BruteAttack",
                        new Sequence("AttackIfInRange",
                            new IsTargetInRange(),
                            new AttackTarget()
                        ),
                        new MoveToTarget()
                    )
                )
            );
        }

        /// <summary>
        /// Investigator (Debuffer/Intel): Analyse rivals to reveal weaknesses, then debuff.
        /// Curious — extra sanity loss from eldritch enemies (set on UnitData).
        /// Priority: analyse unanalysed high-threat → apply knowledge via attack → retreat if threatened.
        /// </summary>
        public static BTNode CreateInvestigator()
        {
            return new Selector("Investigator_Root",
                // 1. Retreat if threatened
                new Sequence("SafetyFirst",
                    new IsHealthBelow(0.25f),
                    new Retreat()
                ),
                // 2. Analyse the highest-threat enemy if not yet studied
                new Sequence("GatherIntel",
                    new HasEnemies(),
                    new FindHighestHPEnemy(),
                    new Condition("NotYetAnalysed", ctx =>
                    {
                        var t = ctx.Get<UnitController>("Target");
                        return t != null && !ctx.Get<bool>($"Analysed_{t.UnitId}");
                    }),
                    new AnalyseTarget()
                ),
                // 3. Attack the analysed target
                new Sequence("ExploitWeakness",
                    new HasEnemies(),
                    new FindWeakestEnemy(),
                    new Selector("PrecisionStrike",
                        new Sequence("StrikeIfInRange",
                            new IsTargetInRange(),
                            new AttackTarget()
                        ),
                        new MoveToTarget()
                    )
                )
            );
        }

        /// <summary>
        /// Shadow (Flanker/Assassin): Circle to rear, target rival leaders.
        /// Loner — unaffected by ally deaths (set on UnitData.ImmuneToAllyDeathSanityLoss).
        /// Priority: retreat if critical → find leader → flank to rear → strike.
        /// </summary>
        public static BTNode CreateShadow()
        {
            return new Selector("Shadow_Root",
                // 1. Retreat only if critical (loner — not spooked by allied deaths)
                new Sequence("ShadowRetreat",
                    new IsHealthBelow(0.15f),
                    new Retreat()
                ),
                // 2. Hunt the highest-HP enemy (rival leader proxy)
                new Sequence("HuntLeader",
                    new HasEnemies(),
                    new FindHighestHPEnemy(),
                    new Selector("FlankAndStrike",
                        // If already in range, attack
                        new Sequence("StrikeFromShadows",
                            new IsTargetInRange(),
                            new AttackTarget()
                        ),
                        // Otherwise, circle to a flank position first
                        new Sequence("CircleApproach",
                            new CircleToFlank(),
                            new AttackTarget()
                        ),
                        // Fallback: direct approach
                        new MoveToTarget()
                    )
                )
            );
        }

        /// <summary>
        /// Herald (Buffer/Aura): Maintain mid-range, boost ally sanity.
        /// Empathic — double sanity loss from ally stress (set on UnitData.AllySanityLossMultiplier = 2).
        /// Priority: boost stressed ally → maintain mid-range → basic attack.
        /// </summary>
        public static BTNode CreateHerald()
        {
            return new Selector("Herald_Root",
                // 1. Actively boost the most stressed ally nearby
                new Sequence("SanitySupport",
                    new HasStressedAlly(65),      // Triggers if any ally below 65 sanity
                    new BoostStressedAlly(15, 6f)  // Restore 15 sanity, range 6 units
                ),
                // 2. Position in the mid-range sweet spot (3-6 units from nearest enemy)
                new Sequence("HoldPosition",
                    new HasEnemies(),
                    new FindNearestEnemy(),
                    new MaintainMidRange(3f, 6f)
                ),
                // 3. Basic attack as a last resort
                new Sequence("BasicAttack",
                    new HasEnemies(),
                    new FindNearestEnemy(),
                    new Selector("AttackOrApproach",
                        new Sequence("AttackIfClose",
                            new IsTargetInRange(),
                            new AttackTarget()
                        ),
                        new MoveToTarget()
                    )
                )
            );
        }

        /// <summary>
        /// Vessel (Mercy Specialist): Non-combat. Generates Mercy Tokens by surviving.
        /// Cannot be healed (set on UnitData.CannotBeHealed). Slowly loses sanity.
        /// Priority: always flee — surviving is the mission.
        /// </summary>
        public static BTNode CreateVessel()
        {
            return new Selector("Vessel_Root",
                // The Vessel's only instruction: avoid death at all costs
                new Sequence("AvoidCombat",
                    new HasEnemies(),
                    new Retreat()
                )
            );
        }

        // ══════════════════════════════════════════════════════
        // LOOKUP
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Get the default behaviour tree for a UnitClass enum value.
        /// Preferred over the string overload — use this when UnitData.UnitClass is set.
        /// </summary>
        public static BTNode GetPreset(UnitClass unitClass) => unitClass switch
        {
            UnitClass.Warden       => CreateWarden(),
            UnitClass.Marksman     => CreateMarksman(),
            UnitClass.Occultist    => CreateOccultist(),
            UnitClass.Berserker    => CreateBerserker(),
            UnitClass.Investigator => CreateInvestigator(),
            UnitClass.Shadow       => CreateShadow(),
            UnitClass.Herald       => CreateHerald(),
            UnitClass.Vessel       => CreateVessel(),
            _                      => CreateWarden() // None / unknown → safe default
        };

        /// <summary>
        /// Get the default behaviour tree for a unit type string.
        /// Kept for backward compatibility with legacy assets that haven't set UnitClass.
        /// Old names (guardian/ranger/healer/scout/emissary) are kept as aliases.
        /// </summary>
        public static BTNode GetPreset(string unitType)
        {
            return unitType?.ToLower() switch
            {
                "warden"       => CreateWarden(),
                "marksman"     => CreateMarksman(),
                "occultist"    => CreateOccultist(),
                "berserker"    => CreateBerserker(),
                "investigator" => CreateInvestigator(),
                "shadow"       => CreateShadow(),
                "herald"       => CreateHerald(),
                "vessel"       => CreateVessel(),

                // Legacy aliases
                "guardian"  => CreateWarden(),
                "ranger"    => CreateMarksman(),
                "healer"    => CreateOccultist(),
                "scout"     => CreateShadow(),
                "emissary"  => CreateVessel(),

                _ => CreateWarden()
            };
        }
    }
}
