using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using KindredSiege.Core;

namespace KindredSiege.City
{
    /// <summary>
    /// Bridge between City and Battle systems.
    /// Translates city state into battle modifiers and battle results into city rewards.
    /// 
    /// This is the SINGLE interface between the two loops.
    /// Neither CityManager nor BattleManager should reference each other directly.
    /// </summary>
    public class CityBattleBridge : MonoBehaviour
    {
        public static CityBattleBridge Instance { get; private set; }

        // ─── City -> Battle modifiers ───

        [Header("Cumulative City Bonuses")]
        [SerializeField] private float unitHPBonus = 1f;     // Multiplier from military buildings
        [SerializeField] private float unitDamageBonus = 1f;
        [SerializeField] private float unitSpeedBonus = 1f;
        [SerializeField] private int maxUnitSlots = 4;        // Increases with city level

        public float UnitHPBonus     => unitHPBonus;
        public float UnitDamageBonus => unitDamageBonus;
        public float UnitSpeedBonus  => unitSpeedBonus;
        public int   MaxUnitSlots    => maxUnitSlots;

        // ─── Special building bonuses (set by CityManager.RecalculateBonuses) ───
        /// <summary>Extra Mercy Tokens per battle (from Shrine buildings).</summary>
        public int   ExtraMercyTokens           { get; set; } = 0;
        /// <summary>Extra Directive Points per battle (from War Table buildings).</summary>
        public int   ExtraDirectivePoints        { get; set; } = 0;
        /// <summary>
        /// Comprehension reduction applied to all player units at battle start.
        /// Each Void Gate contributes -0.1 (less cosmic horror damage taken).
        /// </summary>
        public float VoidGateComprehensionBonus { get; set; } = 0f;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<BattleEndEvent>(OnBattleEnd);
            EventBus.Subscribe<BuildingPlacedEvent>(OnBuildingPlaced);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<BattleEndEvent>(OnBattleEnd);
            EventBus.Unsubscribe<BuildingPlacedEvent>(OnBuildingPlaced);
        }

        // ─── City -> Battle: Calculate bonuses from buildings ───

        /// <summary>
        /// Recalculate unit bonuses based on current city buildings.
        /// Call this before entering battle.
        /// 
        /// TODO: Replace with actual building query once CityManager is built.
        /// For now, bonuses are set manually or via debug.
        /// </summary>
        public void RecalculateBonuses(List<BuildingData> activeBuildings)
        {
            unitHPBonus = 1f;
            unitDamageBonus = 1f;
            unitSpeedBonus = 1f;

            foreach (var building in activeBuildings)
            {
                unitHPBonus *= building.UnitHPMultiplier;
                unitDamageBonus *= building.UnitDamageMultiplier;
                unitSpeedBonus *= building.UnitSpeedMultiplier;
            }

            Debug.Log($"[Bridge] Bonuses recalculated: HP x{unitHPBonus:F2} | DMG x{unitDamageBonus:F2} | SPD x{unitSpeedBonus:F2}");
        }

        // ─── Battle -> City: Process battle rewards ───

        private void OnBattleEnd(BattleEndEvent e)
        {
            // Reward resources based on battle result
            int goldReward = e.BattleResult switch
            {
                BattleEndEvent.Result.Victory => 100,
                BattleEndEvent.Result.Draw => 30,
                BattleEndEvent.Result.Defeat => 10,
                _ => 0
            };

            int materialReward = e.BattleResult switch
            {
                BattleEndEvent.Result.Victory => 50,
                BattleEndEvent.Result.Draw => 15,
                BattleEndEvent.Result.Defeat => 5,
                _ => 0
            };

            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.Add(ResourceType.Gold, goldReward);
                ResourceManager.Instance.Add(ResourceType.Materials, materialReward);
                ResourceManager.Instance.Add(ResourceType.TechPoints, 5);
            }

            Debug.Log($"[Bridge] Battle rewards: +{goldReward} Gold, +{materialReward} Materials, +5 Tech");
        }

        private void OnBuildingPlaced(BuildingPlacedEvent e)
        {
            // When a military building is placed, we might want to recalculate
            // For now, just log
            Debug.Log($"[Bridge] Building placed: {e.BuildingType} at {e.GridPosition}");
        }

        // ─── Convenience ───

        /// <summary>
        /// Apply city bonuses to a unit before battle.
        /// Called by BattleManager during unit spawning.
        /// </summary>
        public void ApplyBonusesToUnit(Battle.UnitController unit)
        {
            unit.ApplyModifiers(unitHPBonus, unitDamageBonus, unitSpeedBonus);
        }

        /// <summary>Increase max unit slots (from city upgrades).</summary>
        public void IncreaseUnitSlots(int amount)
        {
            maxUnitSlots += amount;
            Debug.Log($"[Bridge] Max unit slots: {maxUnitSlots}");
        }
    }
}
