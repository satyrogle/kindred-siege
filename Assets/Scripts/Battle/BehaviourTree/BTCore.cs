using System.Collections.Generic;
using UnityEngine;
using KindredSiege.Battle;

namespace KindredSiege.AI.BehaviourTree
{
    /// <summary>
    /// Result of a behaviour tree node tick.
    /// </summary>
    public enum NodeState
    {
        Running,  // Still executing
        Success,  // Completed successfully
        Failure   // Failed to complete
    }

    /// <summary>
    /// Base class for all behaviour tree nodes.
    /// Subclass this to create custom actions, conditions, composites, and decorators.
    /// </summary>
    [System.Serializable]
    public abstract class BTNode
    {
        public string Name { get; set; }

        protected BTNode(string name = "Node")
        {
            Name = name;
        }

        /// <summary>Evaluate this node and return its state.</summary>
        public abstract NodeState Tick(BattleContext context);

        /// <summary>Called when the tree is reset between ticks (optional override).</summary>
        public virtual void Reset() { }
    }

    /// <summary>
    /// Shared context passed to all nodes during evaluation.
    /// Contains everything a node needs to make decisions.
    /// </summary>
    public class BattleContext
    {
        public UnitController Owner;        // The unit running this tree
        public List<UnitController> Allies;
        public List<UnitController> Enemies;
        public BattleGrid Grid;
        public float DeltaTime;

        // Blackboard for sharing data between nodes
        public Dictionary<string, object> Blackboard = new();

        public void Set<T>(string key, T value) => Blackboard[key] = value;

        public T Get<T>(string key, T defaultValue = default)
        {
            if (Blackboard.TryGetValue(key, out object val) && val is T typed)
                return typed;
            return defaultValue;
        }

        public bool Has(string key) => Blackboard.ContainsKey(key);
    }

    // ═══════════════════════════════════════════════
    // COMPOSITE NODES
    // ═══════════════════════════════════════════════

    /// <summary>
    /// Sequence: Runs children left-to-right. Fails on first failure.
    /// All children must succeed for the sequence to succeed.
    /// Think of it as "AND" logic.
    /// </summary>
    public class Sequence : BTNode
    {
        private readonly List<BTNode> children;
        private int currentChild = 0;

        public Sequence(string name, params BTNode[] children) : base(name)
        {
            this.children = new List<BTNode>(children);
        }

        public override NodeState Tick(BattleContext context)
        {
            while (currentChild < children.Count)
            {
                var state = children[currentChild].Tick(context);

                if (state == NodeState.Running) return NodeState.Running;
                if (state == NodeState.Failure)
                {
                    currentChild = 0;
                    return NodeState.Failure;
                }

                currentChild++;
            }

            currentChild = 0;
            return NodeState.Success;
        }

        public override void Reset()
        {
            currentChild = 0;
            foreach (var child in children) child.Reset();
        }
    }

    /// <summary>
    /// Selector: Runs children left-to-right. Succeeds on first success.
    /// Think of it as "OR" logic — tries alternatives until one works.
    /// </summary>
    public class Selector : BTNode
    {
        private readonly List<BTNode> children;
        private int currentChild = 0;

        public Selector(string name, params BTNode[] children) : base(name)
        {
            this.children = new List<BTNode>(children);
        }

        public override NodeState Tick(BattleContext context)
        {
            while (currentChild < children.Count)
            {
                var state = children[currentChild].Tick(context);

                if (state == NodeState.Running) return NodeState.Running;
                if (state == NodeState.Success)
                {
                    currentChild = 0;
                    return NodeState.Success;
                }

                currentChild++;
            }

            currentChild = 0;
            return NodeState.Failure;
        }

        public override void Reset()
        {
            currentChild = 0;
            foreach (var child in children) child.Reset();
        }
    }

    // ═══════════════════════════════════════════════
    // DECORATOR NODES
    // ═══════════════════════════════════════════════

    /// <summary>
    /// Inverter: Flips Success to Failure and vice versa. Running stays Running.
    /// </summary>
    public class Inverter : BTNode
    {
        private readonly BTNode child;

        public Inverter(string name, BTNode child) : base(name)
        {
            this.child = child;
        }

        public override NodeState Tick(BattleContext context)
        {
            var state = child.Tick(context);
            return state switch
            {
                NodeState.Success => NodeState.Failure,
                NodeState.Failure => NodeState.Success,
                _ => NodeState.Running
            };
        }

        public override void Reset() => child.Reset();
    }

    /// <summary>
    /// Repeater: Runs child N times (or forever if count is -1).
    /// </summary>
    public class Repeater : BTNode
    {
        private readonly BTNode child;
        private readonly int maxRepeats;
        private int currentRepeat = 0;

        public Repeater(string name, BTNode child, int maxRepeats = -1) : base(name)
        {
            this.child = child;
            this.maxRepeats = maxRepeats;
        }

        public override NodeState Tick(BattleContext context)
        {
            var state = child.Tick(context);

            if (state == NodeState.Running) return NodeState.Running;

            currentRepeat++;
            if (maxRepeats > 0 && currentRepeat >= maxRepeats)
            {
                currentRepeat = 0;
                return NodeState.Success;
            }

            child.Reset();
            return NodeState.Running;
        }

        public override void Reset()
        {
            currentRepeat = 0;
            child.Reset();
        }
    }

    /// <summary>
    /// Succeeder: Always returns Success regardless of child result.
    /// Useful for optional actions in sequences.
    /// </summary>
    public class Succeeder : BTNode
    {
        private readonly BTNode child;

        public Succeeder(string name, BTNode child) : base(name)
        {
            this.child = child;
        }

        public override NodeState Tick(BattleContext context)
        {
            child.Tick(context);
            return NodeState.Success;
        }

        public override void Reset() => child.Reset();
    }

    // ═══════════════════════════════════════════════
    // CONDITION NODES (Leaf nodes that check state)
    // ═══════════════════════════════════════════════

    /// <summary>
    /// Generic condition: wraps a lambda for quick prototyping.
    /// For production, create named condition classes.
    /// </summary>
    public class Condition : BTNode
    {
        private readonly System.Func<BattleContext, bool> predicate;

        public Condition(string name, System.Func<BattleContext, bool> predicate) : base(name)
        {
            this.predicate = predicate;
        }

        public override NodeState Tick(BattleContext context)
        {
            return predicate(context) ? NodeState.Success : NodeState.Failure;
        }
    }

    /// <summary>
    /// Generic action: wraps a lambda for quick prototyping.
    /// For production, create named action classes.
    /// </summary>
    public class ActionNode : BTNode
    {
        private readonly System.Func<BattleContext, NodeState> action;

        public ActionNode(string name, System.Func<BattleContext, NodeState> action) : base(name)
        {
            this.action = action;
        }

        public override NodeState Tick(BattleContext context)
        {
            return action(context);
        }
    }
}
