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
        public bool IsCharityUnit = false; // Vessel type — generates Mercy Tokens by surviving
        public int BonusKPPerSurvival = 0;

        [Header("Sanity")]
        [Range(0, 100)]
        public int BaseSanity = 100;
        // Berserker: feeds on violence — gains sanity from kills
        public int SanityOnKill = 0;
        // Shadow: loner — unaffected by ally deaths
        public bool ImmuneToAllyDeathSanityLoss = false;
        // Herald: empathic — double sanity loss from ally-related events
        [Range(1f, 3f)]
        public float AllySanityLossMultiplier = 1f;
        // Vessel: cannot be healed, slowly loses sanity
        public bool CannotBeHealed = false;
        public int PassiveSanityDrainPerSecond = 0;

        [Header("Progression")]
        // Incremented by the campaign manager after each survived expedition.
        // At 5+, unit is a Veteran — higher Virtue chance under stress.
        // NOTE: ScriptableObject assets are shared; this field is runtime-modified
        //       and should be persisted via a separate save system in production.
        public int ExpeditionCount = 0;

        [Header("Recruitment Cost")]
        public int GoldCost = 50;
        public int FoodCost = 20;
        public int MaterialCost = 0;

        [Header("Visual")]
        public Color TeamTint = Color.white;
        public float ModelScale = 1f;
    }
}
