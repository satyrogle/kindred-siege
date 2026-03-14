using UnityEngine;
using System;
using System.Collections.Generic;

namespace KindredSiege.Core
{
    public enum ResourceType
    {
        Gold,
        Materials,
        Food,
        TechPoints,
        KindnessPoints
    }

    /// <summary>
    /// Manages all game resources. Central authority for resource transactions.
    /// Other systems request changes through this — never modify resources directly.
    /// </summary>
    public class ResourceManager : MonoBehaviour
    {
        public static ResourceManager Instance { get; private set; }

        [Serializable]
        public class ResourceConfig
        {
            public ResourceType Type;
            public int StartingAmount;
            public int MaxAmount = 9999;
        }

        [Header("Starting Resources")]
        [SerializeField] private ResourceConfig[] resourceConfigs = new ResourceConfig[]
        {
            new() { Type = ResourceType.Gold, StartingAmount = 200, MaxAmount = 9999 },
            new() { Type = ResourceType.Materials, StartingAmount = 100, MaxAmount = 9999 },
            new() { Type = ResourceType.Food, StartingAmount = 150, MaxAmount = 9999 },
            new() { Type = ResourceType.TechPoints, StartingAmount = 0, MaxAmount = 999 },
            new() { Type = ResourceType.KindnessPoints, StartingAmount = 0, MaxAmount = 99999 },
        };

        private Dictionary<ResourceType, int> resources = new();

        public event Action<ResourceType, int, int> OnResourceChanged; // type, oldVal, newVal

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            InitialiseResources();
        }

        private void InitialiseResources()
        {
            resources.Clear();
            foreach (var config in resourceConfigs)
            {
                resources[config.Type] = config.StartingAmount;
            }
        }

        /// <summary>Get current amount of a resource.</summary>
        public int GetAmount(ResourceType type)
        {
            return resources.TryGetValue(type, out int amount) ? amount : 0;
        }

        /// <summary>Add resources. Returns actual amount added (may be capped).</summary>
        public int Add(ResourceType type, int amount)
        {
            if (amount <= 0) return 0;

            int max = GetMaxForType(type);
            int current = GetAmount(type);
            int actualAdd = Mathf.Min(amount, max - current);

            if (actualAdd <= 0) return 0;

            int oldAmount = current;
            resources[type] = current + actualAdd;

            OnResourceChanged?.Invoke(type, oldAmount, resources[type]);
            EventBus.Publish(new ResourceChangedEvent
            {
                Type = type,
                OldAmount = oldAmount,
                NewAmount = resources[type],
                Delta = actualAdd
            });

            return actualAdd;
        }

        /// <summary>Spend resources. Returns true if successful, false if insufficient.</summary>
        public bool Spend(ResourceType type, int amount)
        {
            if (amount <= 0) return true;
            if (GetAmount(type) < amount) return false;

            int oldAmount = resources[type];
            resources[type] -= amount;

            OnResourceChanged?.Invoke(type, oldAmount, resources[type]);
            EventBus.Publish(new ResourceChangedEvent
            {
                Type = type,
                OldAmount = oldAmount,
                NewAmount = resources[type],
                Delta = -amount
            });

            return true;
        }

        /// <summary>Check if player can afford a cost.</summary>
        public bool CanAfford(ResourceType type, int amount)
        {
            return GetAmount(type) >= amount;
        }

        /// <summary>Check if player can afford multiple costs at once.</summary>
        public bool CanAfford(Dictionary<ResourceType, int> costs)
        {
            foreach (var cost in costs)
            {
                if (!CanAfford(cost.Key, cost.Value)) return false;
            }
            return true;
        }

        /// <summary>Spend multiple resources at once. All-or-nothing.</summary>
        public bool SpendMultiple(Dictionary<ResourceType, int> costs)
        {
            if (!CanAfford(costs)) return false;

            foreach (var cost in costs)
            {
                Spend(cost.Key, cost.Value);
            }
            return true;
        }

        public void ResetResources() => InitialiseResources();

        private int GetMaxForType(ResourceType type)
        {
            foreach (var config in resourceConfigs)
            {
                if (config.Type == type) return config.MaxAmount;
            }
            return 9999;
        }
    }
}
