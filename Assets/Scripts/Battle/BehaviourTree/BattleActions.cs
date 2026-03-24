using UnityEngine;
using System.Linq;
using KindredSiege.Battle;

namespace KindredSiege.AI.BehaviourTree
{
    // ═══════════════════════════════════════════════
    // CONDITIONS — checks that don't change state
    // ═══════════════════════════════════════════════

    /// <summary>Check if any enemies are alive.</summary>
    public class HasEnemies : BTNode
    {
        public HasEnemies() : base("HasEnemies") { }

        public override NodeState Tick(BattleContext context)
        {
            bool hasLiving = context.Enemies.Any(e => e != null && e.IsTargetable);
            return hasLiving ? NodeState.Success : NodeState.Failure;
        }
    }

    /// <summary>Check if unit health is below a threshold (0-1 normalised).</summary>
    public class IsHealthBelow : BTNode
    {
        private readonly float threshold;

        public IsHealthBelow(float threshold) : base($"HP<{threshold:P0}")
        {
            this.threshold = threshold;
        }

        public override NodeState Tick(BattleContext context)
        {
            float ratio = (float)context.Owner.CurrentHP / context.Owner.MaxHP;
            return ratio < threshold ? NodeState.Success : NodeState.Failure;
        }
    }

    /// <summary>Check if a target is within attack range.</summary>
    public class IsTargetInRange : BTNode
    {
        public IsTargetInRange() : base("InRange?") { }

        public override NodeState Tick(BattleContext context)
        {
            var target = context.Get<UnitController>("Target");
            if (target == null || !target.IsTargetable) return NodeState.Failure;

            float dist = Vector3.Distance(
                context.Owner.transform.position,
                target.transform.position
            );

            return dist <= context.Owner.AttackRange ? NodeState.Success : NodeState.Failure;
        }
    }

    /// <summary>Check if any ally needs healing (below threshold).</summary>
    public class HasWoundedAlly : BTNode
    {
        private readonly float threshold;

        public HasWoundedAlly(float threshold = 0.6f) : base("WoundedAlly?")
        {
            this.threshold = threshold;
        }

        public override NodeState Tick(BattleContext context)
        {
            var wounded = context.Allies
                .Where(a => a != null && a.IsAlive && a != context.Owner)
                .FirstOrDefault(a => (float)a.CurrentHP / a.MaxHP < threshold);

            if (wounded != null)
            {
                context.Set("HealTarget", wounded);
                return NodeState.Success;
            }
            return NodeState.Failure;
        }
    }

    // ═══════════════════════════════════════════════
    // ACTIONS — things units do
    // ═══════════════════════════════════════════════

    /// <summary>Find the nearest enemy and store as "Target" on the blackboard.</summary>
    public class FindNearestEnemy : BTNode
    {
        public FindNearestEnemy() : base("FindNearest") { }

        public override NodeState Tick(BattleContext context)
        {
            UnitController nearest = null;
            float bestDist = float.MaxValue;

            foreach (var enemy in context.Enemies)
            {
                if (enemy == null || !enemy.IsTargetable) continue;

                float dist = Vector3.Distance(
                    context.Owner.transform.position,
                    enemy.transform.position
                );

                if (dist < bestDist)
                {
                    bestDist = dist;
                    nearest = enemy;
                }
            }

            if (nearest != null)
            {
                context.Set("Target", nearest);
                return NodeState.Success;
            }
            return NodeState.Failure;
        }
    }

    /// <summary>Find the weakest (lowest HP) enemy.</summary>
    public class FindWeakestEnemy : BTNode
    {
        public FindWeakestEnemy() : base("FindWeakest") { }

        public override NodeState Tick(BattleContext context)
        {
            var weakest = context.Enemies
                .Where(e => e != null && e.IsTargetable)
                .OrderBy(e => e.CurrentHP)
                .FirstOrDefault();

            if (weakest != null)
            {
                context.Set("Target", weakest);
                return NodeState.Success;
            }
            return NodeState.Failure;
        }
    }

    /// <summary>Move toward the current target.</summary>
    public class MoveToTarget : BTNode
    {
        public MoveToTarget() : base("MoveToTarget") { }

        public override NodeState Tick(BattleContext context)
        {
            var target = context.Get<UnitController>("Target");
            if (target == null || !target.IsTargetable) return NodeState.Failure;

            Vector3 direction = (target.transform.position - context.Owner.transform.position).normalized;
            float moveAmount = context.Owner.MoveSpeed * context.DeltaTime;

            context.Owner.transform.position += direction * moveAmount;

            // Check if close enough to attack
            float dist = Vector3.Distance(
                context.Owner.transform.position,
                target.transform.position
            );

            return dist <= context.Owner.AttackRange ? NodeState.Success : NodeState.Running;
        }
    }

    /// <summary>Attack the current target.</summary>
    public class AttackTarget : BTNode
    {
        public AttackTarget() : base("Attack") { }

        public override NodeState Tick(BattleContext context)
        {
            var target = context.Get<UnitController>("Target");
            if (target == null || !target.IsTargetable) return NodeState.Failure;

            if (context.Owner.CanAttack())
            {
                context.Owner.PerformAttack(target);
                return NodeState.Success;
            }

            return NodeState.Running; // Waiting for cooldown
        }
    }

    /// <summary>Retreat away from the nearest enemy.</summary>
    public class Retreat : BTNode
    {
        public Retreat() : base("Retreat") { }

        public override NodeState Tick(BattleContext context)
        {
            var nearest = context.Enemies
                .Where(e => e != null && e.IsTargetable)
                .OrderBy(e => Vector3.Distance(context.Owner.transform.position, e.transform.position))
                .FirstOrDefault();

            if (nearest == null) return NodeState.Failure;

            Vector3 away = (context.Owner.transform.position - nearest.transform.position).normalized;
            context.Owner.transform.position += away * context.Owner.MoveSpeed * context.DeltaTime;

            return NodeState.Running;
        }
    }

    /// <summary>Check if the owner's sanity is below a 0-100 threshold.</summary>
    public class IsSanityBelow : BTNode
    {
        private readonly int threshold;

        public IsSanityBelow(int threshold) : base($"Sanity<{threshold}") { this.threshold = threshold; }

        public override NodeState Tick(BattleContext context)
        {
            return context.Owner.CurrentSanity < threshold ? NodeState.Success : NodeState.Failure;
        }
    }

    /// <summary>
    /// Check if any ally's sanity is below a threshold.
    /// Stores the most stressed ally as "SanityTarget" on the blackboard.
    /// </summary>
    public class HasStressedAlly : BTNode
    {
        private readonly int threshold;

        public HasStressedAlly(int threshold = 60) : base("StressedAlly?") { this.threshold = threshold; }

        public override NodeState Tick(BattleContext context)
        {
            var stressed = context.Allies
                .Where(a => a != null && a.IsAlive && a != context.Owner)
                .OrderBy(a => a.CurrentSanity)
                .FirstOrDefault(a => a.CurrentSanity < threshold);

            if (stressed != null)
            {
                context.Set("SanityTarget", stressed);
                return NodeState.Success;
            }
            return NodeState.Failure;
        }
    }

    /// <summary>
    /// Herald action: restore sanity to the most stressed nearby ally.
    /// Requires "SanityTarget" on the blackboard (set by HasStressedAlly).
    /// </summary>
    public class BoostStressedAlly : BTNode
    {
        private readonly int sanityAmount;
        private readonly float range;

        public BoostStressedAlly(int sanityAmount = 15, float range = 6f) : base("BoostAlly")
        {
            this.sanityAmount = sanityAmount;
            this.range = range;
        }

        public override NodeState Tick(BattleContext context)
        {
            var target = context.Get<UnitController>("SanityTarget");
            if (target == null || !target.IsAlive) return NodeState.Failure;

            float dist = Vector3.Distance(context.Owner.transform.position, target.transform.position);

            if (dist > range)
            {
                // Move closer first
                Vector3 dir = (target.transform.position - context.Owner.transform.position).normalized;
                context.Owner.transform.position += dir * context.Owner.MoveSpeed * context.DeltaTime;
                return NodeState.Running;
            }

            if (context.Owner.CanAttack())
            {
                target.ModifySanity(sanityAmount, "HeraldBoost");
                context.Owner.ResetAttackCooldown();
                return NodeState.Success;
            }
            return NodeState.Running;
        }
    }

    /// <summary>
    /// Herald action: position the unit at mid-range — far enough to avoid melee,
    /// close enough to project its aura and support allies.
    /// </summary>
    public class MaintainMidRange : BTNode
    {
        private readonly float minRange;
        private readonly float maxRange;

        public MaintainMidRange(float minRange = 3f, float maxRange = 6f) : base("MidRange")
        {
            this.minRange = minRange;
            this.maxRange = maxRange;
        }

        public override NodeState Tick(BattleContext context)
        {
            var nearest = context.Enemies
                .Where(e => e != null && e.IsTargetable)
                .OrderBy(e => Vector3.Distance(context.Owner.transform.position, e.transform.position))
                .FirstOrDefault();

            if (nearest == null) return NodeState.Failure;

            float dist = Vector3.Distance(context.Owner.transform.position, nearest.transform.position);
            Vector3 dir = (context.Owner.transform.position - nearest.transform.position).normalized;

            if (dist < minRange)
            {
                // Too close — back away
                context.Owner.transform.position += dir * context.Owner.MoveSpeed * context.DeltaTime;
                return NodeState.Running;
            }

            if (dist > maxRange)
            {
                // Too far — move in
                context.Owner.transform.position -= dir * context.Owner.MoveSpeed * context.DeltaTime;
                return NodeState.Running;
            }

            return NodeState.Success; // Already in sweet spot
        }
    }

    /// <summary>
    /// Shadow action: find the highest-HP enemy (proxy for "rival leader" / priority target).
    /// Stores as "Target" on the blackboard.
    /// </summary>
    public class FindHighestHPEnemy : BTNode
    {
        public FindHighestHPEnemy() : base("FindLeader") { }

        public override NodeState Tick(BattleContext context)
        {
            var leader = context.Enemies
                .Where(e => e != null && e.IsTargetable)
                .OrderByDescending(e => e.CurrentHP)
                .FirstOrDefault();

            if (leader != null)
            {
                context.Set("Target", leader);
                return NodeState.Success;
            }
            return NodeState.Failure;
        }
    }

    /// <summary>
    /// Shadow action: circle wide around the target to approach from the rear.
    /// Moves perpendicular to the target direction, then closes in.
    /// </summary>
    public class CircleToFlank : BTNode
    {
        private bool _circlingRight;

        public CircleToFlank() : base("CircleFlank") { }

        public override NodeState Tick(BattleContext context)
        {
            var target = context.Get<UnitController>("Target");
            if (target == null || !target.IsTargetable) return NodeState.Failure;

            Vector3 toTarget = target.transform.position - context.Owner.transform.position;
            float dist = toTarget.magnitude;

            // Once close enough, stop flanking and attack
            if (dist <= context.Owner.AttackRange * 1.5f)
                return NodeState.Success;

            // Pick a consistent side to circle (decided once per approach)
            if (!context.Has("FlankDir"))
            {
                _circlingRight = Random.value > 0.5f;
                context.Set("FlankDir", _circlingRight ? 1 : -1);
            }

            // Move in an arc: forward + perpendicular
            Vector3 forward = toTarget.normalized;
            Vector3 perp    = Vector3.Cross(forward, Vector3.up) * context.Get<int>("FlankDir");
            Vector3 arc     = (forward * 0.6f + perp * 0.4f).normalized;

            context.Owner.transform.position += arc * context.Owner.MoveSpeed * context.DeltaTime;
            return NodeState.Running;
        }

        public override void Reset()
        {
            _circlingRight = false;
        }
    }

    /// <summary>
    /// Investigator action: spend a full attack cooldown to "analyse" the current target,
    /// writing its weakness tag to the blackboard for other units to exploit.
    /// In future, this will hook into the Rivalry Engine to reveal rival weaknesses.
    /// </summary>
    public class AnalyseTarget : BTNode
    {
        public AnalyseTarget() : base("Analyse") { }

        public override NodeState Tick(BattleContext context)
        {
            var target = context.Get<UnitController>("Target");
            if (target == null || !target.IsTargetable) return NodeState.Failure;

            if (!context.Owner.CanAttack()) return NodeState.Running;

            // Mark target as analysed — all allies can read this to deal bonus damage
            string key = $"Analysed_{target.UnitId}";
            context.Set(key, true);
            context.Owner.ResetAttackCooldown();

            Debug.Log($"[Investigator] {context.Owner.UnitName} analysed {target.UnitName}.");
            return NodeState.Success;
        }
    }

    /// <summary>Heal the wounded ally stored on the blackboard.</summary>
    public class HealAlly : BTNode
    {
        private readonly int healAmount;

        public HealAlly(int healAmount = 15) : base("Heal")
        {
            this.healAmount = healAmount;
        }

        public override NodeState Tick(BattleContext context)
        {
            var target = context.Get<UnitController>("HealTarget");
            if (target == null || !target.IsAlive) return NodeState.Failure;

            float dist = Vector3.Distance(
                context.Owner.transform.position,
                target.transform.position
            );

            if (dist > context.Owner.AttackRange)
            {
                // Move closer first
                Vector3 direction = (target.transform.position - context.Owner.transform.position).normalized;
                context.Owner.transform.position += direction * context.Owner.MoveSpeed * context.DeltaTime;
                return NodeState.Running;
            }

            if (context.Owner.CanAttack()) // Reuse attack cooldown for heal cooldown
            {
                target.Heal(healAmount);
                context.Owner.ResetAttackCooldown();
                return NodeState.Success;
            }

            return NodeState.Running;
        }
    }
}
