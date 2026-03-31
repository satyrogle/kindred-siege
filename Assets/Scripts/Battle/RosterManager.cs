using System.Collections.Generic;
using UnityEngine;
using KindredSiege.Core;
using KindredSiege.City;
using KindredSiege.Units;

namespace KindredSiege.Battle
{
    /// <summary>
    /// Owns the player's active expedition roster between battles (GDD §Campaign Loop).
    ///
    /// The roster is the set of UnitData that will be deployed in the next battle.
    /// Maximum size is governed by CityBattleBridge.MaxUnitSlots (starts at 4,
    /// grows with city upgrades).
    ///
    /// The recruit catalog is a list of UnitData assets the player can hire from.
    /// Drag all hirable UnitData assets into the Inspector array.
    ///
    /// Attach to the persistent Manager GameObject alongside BattleManager.
    /// </summary>
    public class RosterManager : MonoBehaviour
    {
        public static RosterManager Instance { get; private set; }

        // ─── Inspector ───
        [Header("Recruit Catalog")]
        [Tooltip("All UnitData assets available for hire. Drag from Project window.")]
        [SerializeField] private UnitData[] recruitCatalog = new UnitData[0];

        // ─── Runtime state ───
        private readonly List<UnitData> _activeRoster = new();

        // ─── Public read ───
        public IReadOnlyList<UnitData> ActiveRoster  => _activeRoster;
        public UnitData[]              RecruitCatalog => recruitCatalog;
        public int                     RosterCount    => _activeRoster.Count;

        /// <summary>Max deployable units — from city building upgrades.</summary>
        public int MaxSlots => CityBattleBridge.Instance?.MaxUnitSlots ?? 4;

        // ════════════════════════════════════════════
        // LIFECYCLE
        // ════════════════════════════════════════════

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // ════════════════════════════════════════════
        // QUERIES
        // ════════════════════════════════════════════

        /// <summary>True if this exact UnitData asset is already in the roster.</summary>
        public bool IsInRoster(UnitData unit) => _activeRoster.Contains(unit);

        /// <summary>True if the player has enough Gold / Food / Materials to hire this unit.</summary>
        public bool CanAffordRecruit(UnitData unit)
        {
            var res = ResourceManager.Instance;
            if (res == null) return false;
            if (!res.CanAfford(ResourceType.Gold, unit.GoldCost)) return false;
            if (unit.FoodCost > 0 && !res.CanAfford(ResourceType.Food, unit.FoodCost)) return false;
            if (unit.MaterialCost > 0 && !res.CanAfford(ResourceType.Materials, unit.MaterialCost)) return false;
            return true;
        }

        /// <summary>True if this unit can join the roster right now.</summary>
        public bool CanRecruit(UnitData unit)
        {
            if (unit == null) return false;
            if (IsInRoster(unit)) return false;               // Already present
            if (_activeRoster.Count >= MaxSlots) return false; // Roster full
            if (FatigueSystem.IsUndeployable(unit)) return false; // Fatigue 100 blocks deploy
            return CanAffordRecruit(unit);
        }

        // ════════════════════════════════════════════
        // COMMANDS
        // ════════════════════════════════════════════

        /// <summary>
        /// Hire a unit into the roster. Spends Gold + Food + Materials.
        /// Returns true if successful.
        /// </summary>
        public bool Recruit(UnitData unit)
        {
            if (!CanRecruit(unit)) return false;

            var res = ResourceManager.Instance;
            res.Spend(ResourceType.Gold, unit.GoldCost);
            if (unit.FoodCost > 0)     res.Spend(ResourceType.Food,      unit.FoodCost);
            if (unit.MaterialCost > 0) res.Spend(ResourceType.Materials, unit.MaterialCost);

            _activeRoster.Add(unit);
            Debug.Log($"[Roster] Hired: {unit.UnitName} ({unit.UnitType}). " +
                      $"Roster: {_activeRoster.Count}/{MaxSlots}");
            return true;
        }

        /// <summary>
        /// Remove a unit from the roster by index.
        /// No resource refund — dismissal is permanent for this session.
        /// </summary>
        public void Dismiss(int index)
        {
            if (index < 0 || index >= _activeRoster.Count) return;
            string name = _activeRoster[index].UnitName;
            _activeRoster.RemoveAt(index);
            Debug.Log($"[Roster] Dismissed: {name}. Roster: {_activeRoster.Count}/{MaxSlots}");
        }

        /// <summary>Snapshot the roster as a plain array for BattleManager.</summary>
        public UnitData[] GetRosterAsArray() => _activeRoster.ToArray();

        /// <summary>Remove all units from the active roster (used on NewGame).</summary>
        public void ClearRoster()
        {
            _activeRoster.Clear();
            Debug.Log("[Roster] Cleared.");
        }

        // ════════════════════════════════════════════
        // SAVE / LOAD
        // ════════════════════════════════════════════

        /// <summary>
        /// Snapshot mutable state for every unit in the catalog (not just the roster).
        /// Fatigue, phobias, FK penalty, and expedition count all persist between sessions.
        /// </summary>
        public List<UnitSaveEntry> GetUnitStatesForSave()
        {
            var list = new List<UnitSaveEntry>();
            foreach (var unit in recruitCatalog)
            {
                if (unit == null) continue;
                var talentIds = new List<int>();
                if (unit.UnlockedTalents != null)
                    foreach (var t in unit.UnlockedTalents) talentIds.Add((int)t);

                list.Add(new UnitSaveEntry
                {
                    AssetName         = unit.name,
                    FatigueLevel      = unit.FatigueLevel,
                    ActivePhobia      = (int)unit.ActivePhobia,
                    MaxSanityPenalty  = unit.MaxSanityPenalty,
                    ExpeditionCount   = unit.ExpeditionCount,
                    UnlockedTalentIds = talentIds,
                    CoSurvivedWith    = new List<string>(unit.CoSurvivedWith ?? new List<string>()),
                    BondedWith        = new List<string>(unit.BondedWith     ?? new List<string>()),
                });
            }
            return list;
        }

        /// <summary>Returns the asset names of units currently in the active roster.</summary>
        public List<string> GetRosterNamesForSave()
        {
            var list = new List<string>();
            foreach (var unit in _activeRoster)
                if (unit != null) list.Add(unit.name);
            return list;
        }

        /// <summary>
        /// Restore unit mutable state from a save file and rebuild the active roster.
        /// Units are matched by ScriptableObject asset name.
        /// </summary>
        public void LoadRoster(List<UnitSaveEntry> unitStates, List<string> rosterNames)
        {
            // Restore mutable fields on every catalog unit
            if (unitStates != null)
            {
                foreach (var entry in unitStates)
                {
                    var unit = System.Array.Find(recruitCatalog, u => u != null && u.name == entry.AssetName);
                    if (unit == null) continue;
                    unit.FatigueLevel     = entry.FatigueLevel;
                    unit.ActivePhobia     = (PhobiaType)entry.ActivePhobia;
                    unit.MaxSanityPenalty = entry.MaxSanityPenalty;
                    unit.ExpeditionCount  = entry.ExpeditionCount;

                    unit.UnlockedTalents.Clear();
                    if (entry.UnlockedTalentIds != null)
                        foreach (int id in entry.UnlockedTalentIds)
                            unit.UnlockedTalents.Add((TalentNodeId)id);

                    unit.CoSurvivedWith = entry.CoSurvivedWith != null
                        ? new List<string>(entry.CoSurvivedWith) : new List<string>();
                    unit.BondedWith     = entry.BondedWith != null
                        ? new List<string>(entry.BondedWith)     : new List<string>();
                }
            }

            // Rebuild the active roster
            _activeRoster.Clear();
            if (rosterNames != null)
            {
                foreach (var assetName in rosterNames)
                {
                    var unit = System.Array.Find(recruitCatalog, u => u != null && u.name == assetName);
                    if (unit != null && !_activeRoster.Contains(unit))
                        _activeRoster.Add(unit);
                }
            }

            Debug.Log($"[Roster] Loaded: {_activeRoster.Count} units in expedition.");
        }
    }
}
