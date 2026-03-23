using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KindredSiege.Core;

namespace KindredSiege.City
{
    /// <summary>
    /// Manages the abstract city state: building catalog, purchases, upgrades,
    /// and per-phase resource production ticks.
    ///
    /// Does NOT handle 3D city layout — that's a visual layer for later.
    /// This is the game-logic side: what's built, what it costs, what it produces.
    ///
    /// On entering CityPhase, TickProduction() fires automatically, granting
    /// resources from all placed buildings. Buildings must be purchased with
    /// Gold + Materials before they contribute.
    ///
    /// Attach to the persistent Manager GameObject alongside GameManager.
    /// </summary>
    public class CityManager : MonoBehaviour
    {
        public static CityManager Instance { get; private set; }

        // ─── Building catalog (runtime-created since no assets exist yet) ───
        private List<BuildingData> _catalog = new();

        // ─── Placed buildings and their current levels ───────────────────────
        private readonly List<PlacedBuilding> _placed = new();

        public IReadOnlyList<PlacedBuilding> PlacedBuildings => _placed;
        public IReadOnlyList<BuildingData>   Catalog         => _catalog;

        [System.Serializable]
        public class PlacedBuilding
        {
            public BuildingData Data;
            public int          Level = 1;

            public int UpgradeCost =>
                Mathf.RoundToInt(Data.GoldCost * Mathf.Pow(Data.UpgradeCostMultiplier, Level));

            public float ProductionMultiplier =>
                Mathf.Pow(Data.UpgradeProductionMultiplier, Level - 1);
        }

        // ════════════════════════════════════════════
        // LIFECYCLE
        // ════════════════════════════════════════════

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            BuildCatalog();
        }

        private void OnEnable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnStateChanged += OnStateChanged;
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnStateChanged -= OnStateChanged;
        }

        private void OnStateChanged(GameManager.GameState from, GameManager.GameState to)
        {
            if (to == GameManager.GameState.CityPhase)
                TickProduction();
        }

        // ════════════════════════════════════════════
        // BUILDING CATALOG
        // ════════════════════════════════════════════

        /// <summary>
        /// Create runtime BuildingData instances for the starting catalog.
        /// In production these would be ScriptableObject assets loaded from Resources.
        /// </summary>
        private void BuildCatalog()
        {
            _catalog = new List<BuildingData>
            {
                MakeBuilding("Watchtower",
                    category:    BuildingCategory.Economy,
                    desc:        "A watchtower overlooking the flood. Scouts recover salvage after each battle.",
                    goldCost: 30, matCost: 0,
                    produces:    ResourceType.Gold, productionAmt: 10),

                MakeBuilding("Market",
                    category:    BuildingCategory.Economy,
                    desc:        "Traders brave the drowned streets. Generates Gold and Materials each phase.",
                    goldCost: 50, matCost: 20,
                    produces:    ResourceType.Gold, productionAmt: 20,
                    matProduction: 5),

                MakeBuilding("Barracks",
                    category:    BuildingCategory.Military,
                    desc:        "Drilled soldiers stand readier. All units enter battle with +10% max HP.",
                    goldCost: 50, matCost: 25,
                    hpMult: 1.10f),

                MakeBuilding("Armory",
                    category:    BuildingCategory.Military,
                    desc:        "Better weapons, sharper edges. All units deal +10% damage.",
                    goldCost: 75, matCost: 50,
                    dmgMult: 1.10f),

                MakeBuilding("Shrine",
                    category:    BuildingCategory.Charity,
                    desc:        "A shrine to those lost to the flood. Grants +1 Mercy Token per battle.",
                    goldCost: 60, matCost: 0),

                MakeBuilding("War Table",
                    category:    BuildingCategory.Military,
                    desc:        "Tactical planning room. Commanders enter battle with +1 Directive Point.",
                    goldCost: 80, matCost: 30),
            };
        }

        private BuildingData MakeBuilding(
            string name, BuildingCategory category, string desc,
            int goldCost, int matCost,
            ResourceType produces       = ResourceType.Gold,
            int productionAmt           = 0,
            int matProduction           = 0,
            float hpMult                = 1f,
            float dmgMult               = 1f)
        {
            var b = ScriptableObject.CreateInstance<BuildingData>();
            b.name                  = name;
            b.BuildingName          = name;
            b.Category              = category;
            b.Description           = desc;
            b.GoldCost              = goldCost;
            b.MaterialCost          = matCost;
            b.ProducesResource      = produces;
            b.ProductionAmount      = productionAmt;
            b.UnitHPMultiplier      = hpMult;
            b.UnitDamageMultiplier  = dmgMult;
            b.MaxLevel              = 3;
            b.UpgradeCostMultiplier = 1.5f;
            b.UpgradeProductionMultiplier = 1.3f;

            // Store mat production in a tag for Market — we'll read it in TickProduction
            if (matProduction > 0)
                b.name += $"|mat:{matProduction}";

            return b;
        }

        // ════════════════════════════════════════════
        // PURCHASING + UPGRADING
        // ════════════════════════════════════════════

        /// <summary>Purchase a building if the player can afford it. Returns true on success.</summary>
        public bool PurchaseBuilding(BuildingData building)
        {
            if (building == null) return false;
            if (IsBuilt(building))
            {
                Debug.LogWarning($"[City] {building.BuildingName} is already built.");
                return false;
            }

            var rm = ResourceManager.Instance;
            if (rm == null) return false;

            if (!rm.CanAfford(ResourceType.Gold, building.GoldCost) ||
                !rm.CanAfford(ResourceType.Materials, building.MaterialCost))
            {
                Debug.Log($"[City] Cannot afford {building.BuildingName}.");
                return false;
            }

            rm.Spend(ResourceType.Gold,      building.GoldCost);
            rm.Spend(ResourceType.Materials, building.MaterialCost);

            _placed.Add(new PlacedBuilding { Data = building, Level = 1 });
            RecalculateBonuses();

            EventBus.Publish(new BuildingPlacedEvent
            {
                BuildingType  = building.BuildingName,
                GridPosition  = Vector2Int.zero  // Visual placement handled separately
            });

            Debug.Log($"[City] Built: {building.BuildingName}");
            return true;
        }

        /// <summary>Upgrade a placed building to the next level. Returns true on success.</summary>
        public bool UpgradeBuilding(BuildingData building)
        {
            var placed = _placed.FirstOrDefault(p => p.Data == building);
            if (placed == null) return false;
            if (placed.Level >= placed.Data.MaxLevel)
            {
                Debug.Log($"[City] {building.BuildingName} is already at max level.");
                return false;
            }

            int cost = placed.UpgradeCost;
            var rm   = ResourceManager.Instance;
            if (rm == null || !rm.CanAfford(ResourceType.Gold, cost)) return false;

            rm.Spend(ResourceType.Gold, cost);
            placed.Level++;
            RecalculateBonuses();

            Debug.Log($"[City] Upgraded {building.BuildingName} to Level {placed.Level}.");
            return true;
        }

        // ════════════════════════════════════════════
        // PRODUCTION TICK
        // ════════════════════════════════════════════

        /// <summary>
        /// Called on entering CityPhase. All placed buildings produce their resources.
        /// </summary>
        public void TickProduction()
        {
            var rm = ResourceManager.Instance;
            if (rm == null) return;

            int totalGold = 0, totalMat = 0, totalFood = 0, totalKP = 0;

            foreach (var pb in _placed)
            {
                float mult = pb.ProductionMultiplier;
                int   amt  = Mathf.RoundToInt(pb.Data.ProductionAmount * mult);

                if (amt > 0)
                    rm.Add(pb.Data.ProducesResource, amt);

                // Market extra Materials — stored in name tag
                if (pb.Data.name.Contains("|mat:"))
                {
                    string tag     = pb.Data.name.Split("|mat:")[1];
                    int    matAmt  = Mathf.RoundToInt(int.Parse(tag) * mult);
                    rm.Add(ResourceType.Materials, matAmt);
                    totalMat += matAmt;
                }

                // KP from charity buildings
                if (pb.Data.GeneratesKP && pb.Data.KPPerTick > 0)
                {
                    int kp = Mathf.RoundToInt(pb.Data.KPPerTick * mult);
                    rm.Add(ResourceType.KindnessPoints, kp);
                    totalKP += kp;
                }

                if (pb.Data.ProducesResource == ResourceType.Gold)  totalGold += amt;
                if (pb.Data.ProducesResource == ResourceType.Materials) totalMat += amt;
                if (pb.Data.ProducesResource == ResourceType.Food) totalFood += amt;
            }

            Debug.Log($"[City] Production tick: +{totalGold}G, +{totalMat}M, +{totalFood}F, +{totalKP}KP");
        }

        // ════════════════════════════════════════════
        // QUERIES
        // ════════════════════════════════════════════

        public bool IsBuilt(BuildingData b)    => _placed.Any(p => p.Data == b);
        public int  GetLevel(BuildingData b)   => _placed.FirstOrDefault(p => p.Data == b)?.Level ?? 0;

        public bool CanAfford(BuildingData b)
        {
            var rm = ResourceManager.Instance;
            return rm != null
                && rm.CanAfford(ResourceType.Gold,      b.GoldCost)
                && rm.CanAfford(ResourceType.Materials,  b.MaterialCost);
        }

        // ════════════════════════════════════════════
        // BONUS RECALCULATION
        // ════════════════════════════════════════════

        private void RecalculateBonuses()
        {
            var bridge = CityBattleBridge.Instance;
            if (bridge == null) return;

            // Military stat bonuses
            bridge.RecalculateBonuses(_placed.Select(p => p.Data).ToList());

            // Special bonuses: Shrine → +1 MercyToken, War Table → +1 DirectivePoint
            int shrineCount   = _placed.Count(p => p.Data.BuildingName == "Shrine");
            int warTableCount = _placed.Count(p => p.Data.BuildingName == "War Table");
            bridge.ExtraMercyTokens     = shrineCount;
            bridge.ExtraDirectivePoints = warTableCount;
        }
    }
}
