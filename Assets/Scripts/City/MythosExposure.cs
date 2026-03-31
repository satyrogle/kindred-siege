using System;
using UnityEngine;
using KindredSiege.Core;
using KindredSiege.Battle;

namespace KindredSiege.City
{
    /// <summary>
    /// GDD §Mythos Exposure — city-level creeping horror.
    ///
    /// Mythos Exposure (0–100) tracks how deeply the drowned city's influence
    /// has seeped into the player's stronghold. It rises through battle defeats,
    /// unit losses, and time. At thresholds it applies escalating penalties:
    ///
    ///   0–24   (Initiate)  — no effect
    ///   25–49  (Acolyte)   — all units start battles with -5 sanity
    ///   50–74  (Scholar)   — -10 sanity, Dread Contest power +2 for all rivals
    ///   75–99  (Adept)     — -15 sanity, passive sanity drain ticks in city phase
    ///   100    (Seer)      — city falls; campaign ends in defeat
    ///
    ///
    /// Mythos Exposure is a ONE-WAY ticking clock. It persists across runs via PlayerPrefs.
    ///
    /// Attach to the persistent Manager GameObject.
    /// </summary>
    public class MythosExposure : MonoBehaviour
    {
        public static MythosExposure Instance { get; private set; }

        [Header("Exposure State")]
        [SerializeField, Range(0, 100)]
        private int _exposure = 0;

        public int  Exposure => _exposure;
        public bool CityFallen => _exposure >= 100;

        public event Action<int, int> OnExposureChanged; // old, new
        public event Action           OnCityFallen;

        // ─── Thresholds (One-Way Escalation) ─────────────────────────────────
        public const int ThresholdAcolyte = 25;
        public const int ThresholdScholar = 50;
        public const int ThresholdAdept   = 75;
        public const int ThresholdSeer    = 100;

        // ─── Gain rates ──────────────────────────────────────────────────────
        private const int GainOnDefeat      = 8;   // Battle lost
        private const int GainOnDraw        = 3;   // Battle timed out
        private const int GainOnUnitLost    = 4;   // Unit sanity → 0 (lost to madness)
        private const int GainPerSeason     = 2;   // Passive rise each new season

        // City-phase passive drain for Corrupted tier
        private const float CorruptedDrainInterval = 30f; // seconds (real-time in city)
        private float _drainTimer = 0f;

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
            LoadFromPlayerPrefs();

            EventBus.Subscribe<BattleEndEvent>(OnBattleEnd);
            EventBus.Subscribe<UnitLostEvent>(OnUnitLost);

            if (GameManager.Instance != null)
                GameManager.Instance.OnSeasonEnd += OnSeasonEnd;
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<BattleEndEvent>(OnBattleEnd);
            EventBus.Unsubscribe<UnitLostEvent>(OnUnitLost);

            if (GameManager.Instance != null)
                GameManager.Instance.OnSeasonEnd -= OnSeasonEnd;
        }

        private void Update()
        {
            // Adept-tier: tick sanity drain on all roster units while in city phase
            if (_exposure < ThresholdAdept) return;
            if (GameManager.Instance?.CurrentState != GameManager.GameState.CityPhase) return;

            _drainTimer += Time.deltaTime;
            if (_drainTimer < CorruptedDrainInterval) return;
            _drainTimer = 0f;

            var roster = RosterManager.Instance;
            if (roster == null) return;
            foreach (var unit in roster.ActiveRoster)
            {
                if (unit == null) continue;
                // Reduce BaseSanity by 1 (permanent passive drain from corruption)
                unit.BaseSanity = Mathf.Max(10, unit.BaseSanity - 1);
            }
            Debug.Log($"[Mythos] Adept city tick — roster BaseSanity reduced by 1.");
        }

        // ════════════════════════════════════════════
        // EVENT HANDLERS
        // ════════════════════════════════════════════

        private void OnBattleEnd(BattleEndEvent evt)
        {
            switch (evt.BattleResult)
            {
                case BattleEndEvent.Result.Defeat: Gain(GainOnDefeat, "Defeat"); break;
                case BattleEndEvent.Result.Draw:   Gain(GainOnDraw,   "Draw");   break;
                // Victory no longer reduces exposure — it only delays the inevitable.
            }
        }

        private void OnUnitLost(UnitLostEvent evt)
        {
            Gain(GainOnUnitLost, "UnitLost");
        }

        private void OnSeasonEnd()
        {
            Gain(GainPerSeason, "SeasonTurn");
        }

        // ════════════════════════════════════════════
        // GAIN / REDUCE
        // ════════════════════════════════════════════

        public void Gain(int amount, string reason = "")
        {
            if (amount <= 0 || _exposure >= ThresholdSeer) return;
            int old = _exposure;
            _exposure = Mathf.Min(_exposure + amount, ThresholdSeer);
            Debug.Log($"[Mythos] Exposure +{amount} ({reason}) → {_exposure}  [{GetTierName()}]");
            
            // Auto-save exposure instantly for cross-run persistence
            SaveToPlayerPrefs();
            
            OnExposureChanged?.Invoke(old, _exposure);

            if (_exposure >= ThresholdSeer)
            {
                Debug.LogWarning("[Mythos] Exposure 100 — THE CITY HAS FALLEN.");
                OnCityFallen?.Invoke();
            }
        }

        // ════════════════════════════════════════════
        // QUERIES — used by BattleManager & DreadContestSystem
        // ════════════════════════════════════════════

        public ExposureTier GetTier() => _exposure switch
        {
            >= ThresholdSeer    => ExposureTier.Seer,
            >= ThresholdAdept   => ExposureTier.Adept,
            >= ThresholdScholar => ExposureTier.Scholar,
            >= ThresholdAcolyte => ExposureTier.Acolyte,
            _                   => ExposureTier.Initiate
        };

        public string GetTierName() => GetTier() switch
        {
            ExposureTier.Initiate => "Initiate",
            ExposureTier.Acolyte  => "Acolyte",
            ExposureTier.Scholar  => "Scholar",
            ExposureTier.Adept    => "Adept",
            ExposureTier.Seer     => "Seer",
            _                     => "Unknown"
        };

        /// <summary>
        /// Sanity penalty applied to all player units at battle start.
        /// </summary>
        public int BattleStartSanityPenalty => GetTier() switch
        {
            ExposureTier.Acolyte => -5,
            ExposureTier.Scholar => -10,
            ExposureTier.Adept   => -15,
            _                    => 0
        };

        /// <summary>
        /// Bonus Dread Power added to ALL rival Dread Contests at Scholar+.
        /// </summary>
        public int DreadContestBonus => _exposure >= ThresholdScholar ? 2 : 0;

        /// <summary>
        /// Danger Scaling: Extra traits awarded to rivals at Adept+ tier.
        /// </summary>
        public int ExtraRivalTraits => _exposure >= ThresholdAdept ? 1 : 0;

        /// <summary>
        /// Power Scaling: Investigation analyses are free at Scholar+ tier.
        /// </summary>
        public bool FreeAnalyses => _exposure >= ThresholdScholar;

        // ════════════════════════════════════════════
        // CROSS-RUN PERSISTENCE
        // ════════════════════════════════════════════

        public void SaveToPlayerPrefs()
        {
            PlayerPrefs.SetInt("KS_MythosExposure", _exposure);
            PlayerPrefs.Save();
        }

        public void LoadFromPlayerPrefs()
        {
            _exposure = PlayerPrefs.GetInt("KS_MythosExposure", 0);
        }

        // Keep stub for backwards compatibility with SaveManager until removed
        public int  GetExposureForSave()           => _exposure;
        public void LoadFromSave(int savedExposure) { /* Ignored, handled by PlayerPrefs */ }
    }

    public enum ExposureTier { Initiate, Acolyte, Scholar, Adept, Seer }
}
