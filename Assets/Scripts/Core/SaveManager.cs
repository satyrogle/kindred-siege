using System.IO;
using System.Collections.Generic;
using UnityEngine;
using KindredSiege.Battle;
using KindredSiege.City;
using KindredSiege.Rivalry;

namespace KindredSiege.Core
{
    /// <summary>
    /// Save / Load manager — persists campaign state to JSON between sessions.
    ///
    /// SAVE PATH: Application.persistentDataPath/campaign.json
    ///
    /// Autosave triggers when the game enters CityPhase (after every battle).
    /// Manual save: SaveManager.Instance.SaveGame()
    /// Manual load: SaveManager.Instance.LoadGame()
    ///
    /// What is saved:
    ///   GameManager    — season, battles completed
    ///   ResourceManager — all resource amounts
    ///   RosterManager  — unit mutable state + active roster membership
    ///   RivalryEngine  — full rival pool (active + defeated) including memory
    ///   CityManager    — placed buildings + levels
    ///
    /// What is NOT saved:
    ///   Mid-battle state (battle is always restarted fresh)
    ///   Unity scene objects (purely runtime)
    ///
    /// Attach to the persistent Manager GameObject.
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        private string SavePath => Path.Combine(Application.persistentDataPath, "campaign.json");

        public bool HasSave => File.Exists(SavePath);

        // ════════════════════════════════════════════
        // LIFECYCLE
        // ════════════════════════════════════════════

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnStateChanged += OnStateChanged;
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnStateChanged -= OnStateChanged;
        }

        // Autosave on every CityPhase entry
        private void OnStateChanged(GameManager.GameState from, GameManager.GameState to)
        {
            if (to == GameManager.GameState.CityPhase)
                SaveGame();
        }

        // ════════════════════════════════════════════
        // SAVE
        // ════════════════════════════════════════════

        /// <summary>
        /// Snapshot all campaign state and write to campaign.json.
        /// Safe to call at any time outside of active battle.
        /// </summary>
        public void SaveGame()
        {
            var data = new SaveData();

            // ── GameManager ──
            var gm = GameManager.Instance;
            if (gm != null)
            {
                data.CurrentSeason    = gm.CurrentSeason;
                data.BattlesCompleted = gm.BattlesCompleted;
            }

            // ── Resources ──
            var res = ResourceManager.Instance;
            if (res != null)
                data.Resources = res.GetResourcesForSave();

            // ── Unit states + roster ──
            var roster = RosterManager.Instance;
            if (roster != null)
            {
                data.UnitStates      = roster.GetUnitStatesForSave();
                data.RosterAssetNames = roster.GetRosterNamesForSave();
            }

            // ── Rivals ──
            var rivalry = RivalryEngine.Instance;
            if (rivalry != null)
            {
                data.ActiveRivals   = rivalry.GetActivesForSave();
                data.DefeatedRivals = rivalry.GetDefeatedForSave();
            }

            // ── City buildings ──
            var city = CityManager.Instance;
            if (city != null)
                data.PlacedBuildings = city.GetBuildingsForSave();

            // ── Write ──
            string json = JsonUtility.ToJson(data, prettyPrint: true);
            File.WriteAllText(SavePath, json);
            Debug.Log($"[Save] Campaign saved → {SavePath}");
        }

        // ════════════════════════════════════════════
        // LOAD
        // ════════════════════════════════════════════

        /// <summary>
        /// Read campaign.json and restore all singletons to the saved state.
        /// Call from the main menu "Continue" button.
        /// Returns true if a save file was found and loaded.
        /// </summary>
        public bool LoadGame()
        {
            if (!HasSave)
            {
                Debug.LogWarning("[Save] No save file found.");
                return false;
            }

            string json = File.ReadAllText(SavePath);
            var data = JsonUtility.FromJson<SaveData>(json);
            if (data == null)
            {
                Debug.LogError("[Save] Failed to parse save file.");
                return false;
            }

            // ── GameManager ──
            GameManager.Instance?.LoadState(data.CurrentSeason, data.BattlesCompleted);

            // ── Resources ──
            ResourceManager.Instance?.LoadResources(data.Resources);

            // ── Unit states + roster ──
            RosterManager.Instance?.LoadRoster(data.UnitStates, data.RosterAssetNames);

            // ── Rivals ──
            RivalryEngine.Instance?.LoadFromSave(data.ActiveRivals, data.DefeatedRivals);

            // ── City buildings ──
            CityManager.Instance?.LoadFromSave(data.PlacedBuildings);

            Debug.Log($"[Save] Campaign loaded. Season {data.CurrentSeason}, " +
                      $"Battles {data.BattlesCompleted}, Rivals {data.ActiveRivals.Count}");
            return true;
        }

        /// <summary>Delete the save file (new game / reset).</summary>
        public void DeleteSave()
        {
            if (File.Exists(SavePath))
            {
                File.Delete(SavePath);
                Debug.Log("[Save] Save file deleted.");
            }
        }
    }
}
