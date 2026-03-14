using UnityEngine;

namespace KindredSiege.Battle
{
    /// <summary>
    /// ScriptableObject defining a unit type's base stats.
    /// Create instances via Assets > Create > KindredSiege > Unit Data
    /// 
    /// These are the BASE stats — city buildings and upgrades modify them at runtime.
    /// </summary>
    [CreateAssetMenu(fileName = "NewUnit", menuName = "KindredSiege/Unit Data")]
    public class UnitData : ScriptableObject
    {
        [Header("Identity")]
        public string UnitName = "New Unit";
        public string UnitType = "guardian"; // Matches BTPresets key
        [TextArea(2, 4)]
        public string Description = "";
        public Sprite Portrait;
        public GameObject Prefab;

        [Header("Combat Stats")]
        public int MaxHP = 100;
        public int AttackDamage = 10;
        public float AttackRange = 2f;
        public float AttackCooldown = 1f; // Seconds between attacks
        public float MoveSpeed = 3f;
        public int Armour = 0;

        [Header("Special")]
        public bool IsRanged = false;
        public bool CanHeal = false;
        public int HealAmount = 0;
        public bool IsCharityUnit = false; // Emissary type — generates bonus KP
        public int BonusKPPerSurvival = 0;

        [Header("Recruitment Cost")]
        public int GoldCost = 50;
        public int FoodCost = 20;
        public int MaterialCost = 0;

        [Header("Visual")]
        public Color TeamTint = Color.white;
        public float ModelScale = 1f;
    }
}
