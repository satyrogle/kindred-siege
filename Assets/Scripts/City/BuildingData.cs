using UnityEngine;
using System.Collections.Generic;
using KindredSiege.Core;

namespace KindredSiege.City
{
    public enum BuildingCategory
    {
        Economy,    // Gold, Food, Materials generators
        Military,   // Unit training and upgrades
        Charity,    // KP generators
        Tech,       // Tech point generators, AI upgrades
        Utility     // Storage, roads, decorative
    }

    /// <summary>
    /// ScriptableObject defining a building type.
    /// Create instances via Assets > Create > KindredSiege > Building Data
    /// </summary>
    [CreateAssetMenu(fileName = "NewBuilding", menuName = "KindredSiege/Building Data")]
    public class BuildingData : ScriptableObject
    {
        [Header("Identity")]
        public string BuildingName = "New Building";
        public BuildingCategory Category = BuildingCategory.Economy;
        [TextArea(2, 4)]
        public string Description = "";
        public Sprite Icon;
        public GameObject Prefab;

        [Header("Grid")]
        public Vector2Int Size = Vector2Int.one; // 1x1, 2x2, etc.

        [Header("Cost")]
        public int GoldCost = 100;
        public int MaterialCost = 50;
        public int FoodCost = 0;

        [Header("Production (per city tick)")]
        public ResourceType ProducesResource = ResourceType.Gold;
        public int ProductionAmount = 10;

        [Header("Adjacency Bonuses")]
        [Tooltip("Building types that boost this building when placed next to it")]
        public string[] BonusFromAdjacent = new string[0]; // Building names
        public float AdjacencyBonusMultiplier = 1.25f;     // 25% boost per adjacent match

        [Header("Military Bonuses")]
        [Tooltip("Stat modifiers applied to units when this building exists")]
        public float UnitHPMultiplier = 1f;
        public float UnitDamageMultiplier = 1f;
        public float UnitSpeedMultiplier = 1f;

        [Header("Charity")]
        public bool GeneratesKP = false;
        public int KPPerTick = 0;

        [Header("Upgrade")]
        public int MaxLevel = 3;
        public float UpgradeCostMultiplier = 1.5f; // Each level costs 1.5x more
        public float UpgradeProductionMultiplier = 1.3f; // Each level produces 1.3x more
    }
}
