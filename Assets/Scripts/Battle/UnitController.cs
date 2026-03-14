using UnityEngine;
using KindredSiege.AI.BehaviourTree;
using KindredSiege.Core;

namespace KindredSiege.Battle
{
    /// <summary>
    /// Runtime controller for a unit in battle.
    /// Owns a behaviour tree and ticks it each frame during combat.
    /// Attach this to unit prefabs.
    /// </summary>
    public class UnitController : MonoBehaviour
    {
        [Header("Unit Config")]
        [SerializeField] private UnitData unitData;

        // Runtime stats (can be modified by city bonuses)
        public int MaxHP { get; private set; }
        public int CurrentHP { get; private set; }
        public int AttackDamage { get; private set; }
        public float AttackRange { get; private set; }
        public float MoveSpeed { get; private set; }
        public int Armour { get; private set; }

        public bool IsAlive => CurrentHP > 0;
        public string UnitType => unitData != null ? unitData.UnitType : "guardian";
        public string UnitName => unitData != null ? unitData.UnitName : "Unit";
        public UnitData Data => unitData;

        public int TeamId { get; private set; }
        public int UnitId { get; private set; }

        // Behaviour tree
        private BTNode behaviourTree;
        private BattleContext battleContext;
        private float attackTimer = 0f;
        private float attackCooldown = 1f;

        // Battle recording — for replay system
        public System.Collections.Generic.List<UnitActionRecord> ActionHistory { get; private set; } = new();

        [System.Serializable]
        public struct UnitActionRecord
        {
            public float Timestamp;
            public string Action;
            public Vector3 Position;
            public int TargetId;
        }

        /// <summary>
        /// Initialise the unit with data and team assignment.
        /// Called by BattleManager when spawning units.
        /// </summary>
        public void Initialise(UnitData data, int teamId, int unitId)
        {
            unitData = data;
            TeamId = teamId;
            UnitId = unitId;

            // Set runtime stats from data
            MaxHP = data.MaxHP;
            CurrentHP = data.MaxHP;
            AttackDamage = data.AttackDamage;
            AttackRange = data.AttackRange;
            MoveSpeed = data.MoveSpeed;
            Armour = data.Armour;
            attackCooldown = data.AttackCooldown;
            attackTimer = 0f;

            // Create default behaviour tree for this unit type
            behaviourTree = BTPresets.GetPreset(data.UnitType);

            // Visual setup
            transform.localScale = Vector3.one * data.ModelScale;

            ActionHistory.Clear();
        }

        /// <summary>
        /// Apply stat modifiers from city buildings.
        /// Called before battle starts.
        /// </summary>
        public void ApplyModifiers(float hpMult = 1f, float dmgMult = 1f, float speedMult = 1f)
        {
            MaxHP = Mathf.RoundToInt(unitData.MaxHP * hpMult);
            CurrentHP = MaxHP;
            AttackDamage = Mathf.RoundToInt(unitData.AttackDamage * dmgMult);
            MoveSpeed = unitData.MoveSpeed * speedMult;
        }

        /// <summary>Set the battle context for this tick. Called by BattleManager each frame.</summary>
        public void SetContext(BattleContext context)
        {
            battleContext = context;
        }

        /// <summary>Tick the behaviour tree. Called by BattleManager during battle phase.</summary>
        public void TickAI()
        {
            if (!IsAlive || battleContext == null) return;

            attackTimer -= Time.deltaTime;
            battleContext.DeltaTime = Time.deltaTime;
            battleContext.Owner = this;

            behaviourTree.Tick(battleContext);
        }

        // ─── Combat Methods (called by BT action nodes) ───

        public bool CanAttack() => attackTimer <= 0f;

        public void PerformAttack(UnitController target)
        {
            if (!CanAttack() || target == null || !target.IsAlive) return;

            int damage = Mathf.Max(1, AttackDamage - target.Armour);
            target.TakeDamage(damage, this);
            attackTimer = attackCooldown;

            // Record action
            RecordAction("Attack", target.UnitId);

            EventBus.Publish(new UnitActionEvent
            {
                UnitId = UnitId,
                ActionName = "Attack",
                Position = transform.position,
                TargetId = target.UnitId
            });
        }

        public void ResetAttackCooldown()
        {
            attackTimer = attackCooldown;
        }

        public void TakeDamage(int damage, UnitController attacker)
        {
            if (!IsAlive) return;

            CurrentHP = Mathf.Max(0, CurrentHP - damage);

            if (!IsAlive)
            {
                OnDeath(attacker);
            }
        }

        public void Heal(int amount)
        {
            if (!IsAlive) return;
            CurrentHP = Mathf.Min(MaxHP, CurrentHP + amount);
        }

        private void OnDeath(UnitController killedBy)
        {
            RecordAction("Defeated", killedBy?.UnitId ?? -1);

            EventBus.Publish(new UnitDefeatedEvent
            {
                UnitId = UnitId,
                UnitType = UnitType,
                DefeatedByUnitId = killedBy?.UnitId ?? -1
            });

            // Visual death — disable but don't destroy (needed for replay)
            gameObject.SetActive(false);
        }

        private void RecordAction(string action, int targetId)
        {
            ActionHistory.Add(new UnitActionRecord
            {
                Timestamp = Time.time,
                Action = action,
                Position = transform.position,
                TargetId = targetId
            });
        }

        /// <summary>Override the default behaviour tree (for player customisation).</summary>
        public void SetBehaviourTree(BTNode tree)
        {
            behaviourTree = tree;
        }
    }
}
