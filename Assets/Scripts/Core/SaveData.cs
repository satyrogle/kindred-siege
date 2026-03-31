using System;
using System.Collections.Generic;
using KindredSiege.Rivalry;

namespace KindredSiege.Core
{
    /// <summary>
    /// Pure serializable POCO representing the full campaign save state.
    ///
    /// All fields use primitive types or serializable classes so JsonUtility
    /// can round-trip them cleanly. ScriptableObjects are referenced by asset
    /// name so they can be looked up at load time from the live catalog.
    ///
    /// RivalData and RivalMemory are already [Serializable] — they serialize
    /// directly without wrappers.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        // ─── Save Version ───
        public int SaveVersion = 1;

        // ─── GameManager state ───
        public int CurrentSeason;
        public int BattlesCompleted;

        // ─── Resources ───
        public List<ResourceEntry> Resources = new();

        // ─── Unit states — covers every unit in the recruit catalog ───
        // Keyed by ScriptableObject asset name (unit.name in Unity).
        public List<UnitSaveEntry> UnitStates = new();

        // ─── Roster — which asset names are currently in the active expedition ───
        public List<string> RosterAssetNames = new();

        // ─── Rivalry Engine ───
        public List<RivalData> ActiveRivals   = new();
        public List<RivalData> DefeatedRivals = new();

        // ─── City buildings ───
        public List<BuildingSaveEntry> PlacedBuildings = new();

        // ─── Districts — unlocked district IDs (DistrictType cast to int) ───
        public List<int> UnlockedDistricts = new();

        // ─── Mythos Exposure (0–100) ───
        public int MythosExposure = 0;
    }

    // ─────────────────────────────────────────────────────────────────────────

    [Serializable]
    public class ResourceEntry
    {
        public string TypeName; // ResourceType enum as string
        public int    Amount;
    }

    [Serializable]
    public class UnitSaveEntry
    {
        public string       AssetName;          // UnitData.name (Unity asset file name)
        public int          FatigueLevel;
        public int          ActivePhobia;       // PhobiaType cast to int
        public int          MaxSanityPenalty;
        public int          ExpeditionCount;
        public List<int>    UnlockedTalentIds;  // TalentNodeId cast to int (JsonUtility-safe)
        public List<string> CoSurvivedWith;     // Bond co-survival tracking (duplicates = count)
        public List<string> BondedWith;         // Fully bonded unit asset names
    }

    [Serializable]
    public class BuildingSaveEntry
    {
        public string BuildingName; // Matches BuildingData.BuildingName
        public int    Level;
    }
}
