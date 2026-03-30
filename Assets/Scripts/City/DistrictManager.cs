using System;
using System.Collections.Generic;
using UnityEngine;
using KindredSiege.Core;
using KindredSiege.Rivalry;

namespace KindredSiege.City
{
    public enum DistrictType
    {
        Harbor          = 0,  // Default — always unlocked
        MilitaryWard    = 1,  // Unlock: 2+ battles completed
        CharityQuarter  = 2,  // Unlock: 1+ rival defeated
        ScholarsQuarter = 3,  // Unlock: Season 2+ (8+ battles total)
        TheAbyss        = 4,  // Unlock: 1+ Overlord rival defeated
    }

    /// <summary>
    /// GDD §City — District System.
    ///
    /// The city is divided into 5 districts, each gating a set of buildings.
    /// Districts unlock based on campaign progress, giving the player a sense
    /// of the city expanding as they push deeper into the drowned city.
    ///
    /// Unlock conditions:
    ///   Harbor          — always unlocked
    ///   Military Ward   — 2 battles completed (this run)
    ///   Charity Quarter — 1 rival defeated (ever)
    ///   Scholars Quarter— Season 2 or later
    ///   The Abyss       — 1 Overlord-rank rival defeated
    ///
    /// Call CheckUnlocks() after each battle and after loading a save.
    /// Attach to the persistent Manager GameObject.
    /// </summary>
    public class DistrictManager : MonoBehaviour
    {
        public static DistrictManager Instance { get; private set; }

        private readonly HashSet<DistrictType> _unlocked = new();

        public event Action<DistrictType> OnDistrictUnlocked;

        // ─── Display data ────────────────────────────────────────────────────────

        public static string GetName(DistrictType d) => d switch
        {
            DistrictType.Harbor          => "Harbor District",
            DistrictType.MilitaryWard    => "Military Ward",
            DistrictType.CharityQuarter  => "Charity Quarter",
            DistrictType.ScholarsQuarter => "Scholars' Quarter",
            DistrictType.TheAbyss        => "The Abyss",
            _                            => d.ToString()
        };

        public static string GetDescription(DistrictType d) => d switch
        {
            DistrictType.Harbor          => "The flooded docks. Salvagers operate here, keeping the city supplied.",
            DistrictType.MilitaryWard    => "Battle-hardened veterans drill and forge here. Strength through discipline.",
            DistrictType.CharityQuarter  => "Mercy and faith endure where coin cannot reach. The desperate gather here.",
            DistrictType.ScholarsQuarter => "Ancient texts, forbidden or otherwise, are studied beneath candlelight.",
            DistrictType.TheAbyss        => "A rent in the city's foundations. What lies below cannot be explained.",
            _                            => ""
        };

        public static string GetUnlockHint(DistrictType d) => d switch
        {
            DistrictType.Harbor          => "Available from the start.",
            DistrictType.MilitaryWard    => "Unlocks after 2 battles.",
            DistrictType.CharityQuarter  => "Unlocks after defeating your first rival.",
            DistrictType.ScholarsQuarter => "Unlocks in Season 2.",
            DistrictType.TheAbyss        => "Unlocks after defeating an Overlord-rank rival.",
            _                            => ""
        };

        // ════════════════════════════════════════════
        // LIFECYCLE
        // ════════════════════════════════════════════

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            // Harbor always unlocked
            _unlocked.Add(DistrictType.Harbor);
        }

        private void Start()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnStateChanged += OnStateChanged;

            CheckUnlocks();
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnStateChanged -= OnStateChanged;
        }

        private void OnStateChanged(GameManager.GameState from, GameManager.GameState to)
        {
            if (to == GameManager.GameState.CityPhase)
                CheckUnlocks();
        }

        // ════════════════════════════════════════════
        // UNLOCK LOGIC
        // ════════════════════════════════════════════

        /// <summary>
        /// Evaluate all unlock conditions against current campaign state.
        /// Safe to call multiple times — already-unlocked districts are skipped.
        /// </summary>
        public void CheckUnlocks()
        {
            var gm      = GameManager.Instance;
            var rivalry = RivalryEngine.Instance;

            int battlesCompleted  = gm?.BattlesCompleted ?? 0;
            int currentSeason     = gm?.CurrentSeason    ?? 1;
            int rivalsDefeated    = rivalry?.GetDefeatedForSave()?.Count ?? 0;
            bool overlordDefeated = HasDefeatedOverlord(rivalry);

            TryUnlock(DistrictType.Harbor,          condition: true);
            TryUnlock(DistrictType.MilitaryWard,    condition: battlesCompleted >= 2);
            TryUnlock(DistrictType.CharityQuarter,  condition: rivalsDefeated >= 1);
            TryUnlock(DistrictType.ScholarsQuarter, condition: currentSeason   >= 2);
            TryUnlock(DistrictType.TheAbyss,        condition: overlordDefeated);
        }

        private void TryUnlock(DistrictType district, bool condition)
        {
            if (_unlocked.Contains(district) || !condition) return;
            _unlocked.Add(district);
            Debug.Log($"[District] Unlocked: {GetName(district)}");
            OnDistrictUnlocked?.Invoke(district);
        }

        private static bool HasDefeatedOverlord(RivalryEngine rivalry)
        {
            if (rivalry == null) return false;
            var defeated = rivalry.GetDefeatedForSave();
            if (defeated == null) return false;
            foreach (var r in defeated)
                if (r.Rank == RivalRank.Overlord) return true;
            return false;
        }

        // ════════════════════════════════════════════
        // QUERIES
        // ════════════════════════════════════════════

        public bool IsUnlocked(DistrictType district) => _unlocked.Contains(district);

        public IEnumerable<DistrictType> AllDistricts()
        {
            yield return DistrictType.Harbor;
            yield return DistrictType.MilitaryWard;
            yield return DistrictType.CharityQuarter;
            yield return DistrictType.ScholarsQuarter;
            yield return DistrictType.TheAbyss;
        }

        // ════════════════════════════════════════════
        // SAVE / LOAD
        // ════════════════════════════════════════════

        public List<int> GetUnlockedForSave()
        {
            var list = new List<int>();
            foreach (var d in _unlocked) list.Add((int)d);
            return list;
        }

        public void LoadFromSave(List<int> ids)
        {
            if (ids == null) return;
            _unlocked.Clear();
            _unlocked.Add(DistrictType.Harbor); // Always unlocked
            foreach (int id in ids)
                _unlocked.Add((DistrictType)id);
            Debug.Log($"[District] Loaded {_unlocked.Count} unlocked districts.");
        }
    }
}
