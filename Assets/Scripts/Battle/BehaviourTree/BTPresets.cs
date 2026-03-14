using KindredSiege.Battle;

namespace KindredSiege.AI.BehaviourTree
{
    /// <summary>
    /// Factory for creating default behaviour trees for each unit type.
    /// These are the "instinct" layer — players can modify them via the visual editor.
    /// 
    /// ARCHITECTURE NOTE:
    /// Each tree follows the same pattern:
    ///   1. Check if battle is still active (enemies exist)
    ///   2. Priority selector chooses between specialised behaviours
    ///   3. Fallback to basic combat if specialised behaviours don't apply
    /// </summary>
    public static class BTPresets
    {
        /// <summary>
        /// Guardian: Engage nearest enemy, shield allies.
        /// Priority: Low HP retreat > Find nearest > Move & Attack
        /// </summary>
        public static BTNode CreateGuardian()
        {
            return new Selector("Guardian_Root",
                // Priority 1: Retreat if critically wounded
                new Sequence("CriticalRetreat",
                    new IsHealthBelow(0.15f),
                    new Retreat()
                ),
                // Priority 2: Standard combat — engage nearest
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
        /// Ranger: Maintain distance, focus lowest-HP target.
        /// Priority: Too close = retreat > Find weakest > Attack from range
        /// </summary>
        public static BTNode CreateRanger()
        {
            return new Selector("Ranger_Root",
                // Priority 1: Retreat if enemy is too close
                new Sequence("KiteBack",
                    new IsHealthBelow(0.2f),
                    new Condition("EnemyClose", ctx =>
                    {
                        var target = ctx.Get<UnitController>("Target");
                        if (target == null) return false;
                        float dist = UnityEngine.Vector3.Distance(
                            ctx.Owner.transform.position, target.transform.position);
                        return dist < ctx.Owner.AttackRange * 0.5f;
                    }),
                    new Retreat()
                ),
                // Priority 2: Focus fire on weakest
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
        /// Healer: Prioritise healing allies, basic combat as fallback.
        /// Priority: Heal wounded ally > Retreat if threatened > Basic attack
        /// </summary>
        public static BTNode CreateHealer()
        {
            return new Selector("Healer_Root",
                // Priority 1: Heal wounded allies
                new Sequence("HealDuty",
                    new HasWoundedAlly(0.6f),
                    new HealAlly(15)
                ),
                // Priority 2: Retreat if low health
                new Sequence("SelfPreservation",
                    new IsHealthBelow(0.3f),
                    new Retreat()
                ),
                // Priority 3: Basic combat fallback
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
        /// Berserker: Charge highest-value target, no retreat.
        /// Pure aggression — never retreats, always seeks the strongest enemy.
        /// </summary>
        public static BTNode CreateBerserker()
        {
            return new Selector("Berserker_Root",
                new Sequence("ChargeStrongest",
                    new HasEnemies(),
                    // Berserker targets nearest (could swap to highest-value later)
                    new FindNearestEnemy(),
                    new Selector("RecklessAttack",
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
        /// Scout: Flanker — circles to target ranged/support units.
        /// Priority: Find weakest (likely ranged) > Engage
        /// </summary>
        public static BTNode CreateScout()
        {
            return new Selector("Scout_Root",
                // Priority 1: Retreat if critically hurt
                new Sequence("ScoutRetreat",
                    new IsHealthBelow(0.2f),
                    new Retreat()
                ),
                // Priority 2: Hunt weakest targets (squishy backline)
                new Sequence("HuntSquishy",
                    new HasEnemies(),
                    new FindWeakestEnemy(),
                    new Selector("FlankAttack",
                        new Sequence("StrikeIfClose",
                            new IsTargetInRange(),
                            new AttackTarget()
                        ),
                        new MoveToTarget()
                    )
                )
            );
        }

        /// <summary>
        /// Emissary: Non-combat charity specialist.
        /// Avoids enemies, generates bonus KP by surviving.
        /// </summary>
        public static BTNode CreateEmissary()
        {
            return new Selector("Emissary_Root",
                // Always try to flee from danger
                new Sequence("AvoidCombat",
                    new HasEnemies(),
                    new Retreat()
                )
            );
        }

        /// <summary>
        /// Get the default tree for a unit type string.
        /// </summary>
        public static BTNode GetPreset(string unitType)
        {
            return unitType.ToLower() switch
            {
                "guardian" => CreateGuardian(),
                "ranger" => CreateRanger(),
                "healer" => CreateHealer(),
                "berserker" => CreateBerserker(),
                "scout" => CreateScout(),
                "emissary" => CreateEmissary(),
                _ => CreateGuardian() // Default fallback
            };
        }
    }
}
