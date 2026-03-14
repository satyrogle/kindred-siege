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
            bool hasLiving = context.Enemies.Any(e => e != null && e.IsAlive);
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
            if (target == null || !target.IsAlive) return NodeState.Failure;

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
                if (enemy == null || !enemy.IsAlive) continue;

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
                .Where(e => e != null && e.IsAlive)
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
            if (target == null || !target.IsAlive) return NodeState.Failure;

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
            if (target == null || !target.IsAlive) return NodeState.Failure;

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
                .Where(e => e != null && e.IsAlive)
                .OrderBy(e => Vector3.Distance(context.Owner.transform.position, e.transform.position))
                .FirstOrDefault();

            if (nearest == null) return NodeState.Failure;

            Vector3 away = (context.Owner.transform.position - nearest.transform.position).normalized;
            context.Owner.transform.position += away * context.Owner.MoveSpeed * context.DeltaTime;

            return NodeState.Running;
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
