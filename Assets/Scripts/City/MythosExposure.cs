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
    ///   0–24   (Calm)      — no effect
    ///   25–49  (Unsettled) — all units start battles with -5 sanity
    ///   50–74  (Haunted)   — -10 sanity, Dread Contest power +2 for all rivals
    ///   75–99  (Corrupted) — -15 sanity, passive sanity drain ticks in city phase
    ///   100    (Consumed)  — city falls; campaign ends in defeat
    ///
    /// Exposure decreases by spending Kindness Points at the Shrine or Apothecary
    /// (handled via ReduceExposure calls from CityRestPanel).
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

        // ─── Thresholds ──────────────────────────────────────────────────────
        public const int ThresholdUnsettled = 25;
        public const int ThresholdHaunted   = 50;
        public const int ThresholdCorrupted = 75;
        public const int ThresholdConsumed  = 100;

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
            // Corrupted-tier: tick sanity drain on all roster units while in city phase
            if (_exposure < ThresholdCorrupted) return;
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
            Debug.Log($"[Mythos] Corrupted city tick — roster BaseSanity reduced by 1.");
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
                // Victory reduces exposure slightly — hope is a ward against horror
                case BattleEndEvent.Result.Victory: Reduce(2, "Victory");         break;
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
            if (amount <= 0 || _exposure >= ThresholdConsumed) return;
            int old = _exposure;
            _exposure = Mathf.Min(_exposure + amount, ThresholdConsumed);
            Debug.Log($"[Mythos] Exposure +{amount} ({reason}) → {_exposure}  [{GetTierName()}]");
            OnExposureChanged?.Invoke(old, _exposure);

            if (_exposure >= ThresholdConsumed)
            {
                Debug.LogWarning("[Mythos] Exposure 100 — THE CITY HAS FALLEN.");
                OnCityFallen?.Invoke();
            }
        }

        public void Reduce(int amount, string reason = "")
        {
            if (amount <= 0 || _exposure <= 0) return;
            int old = _exposure;
            _exposure = Mathf.Max(0, _exposure - amount);
            Debug.Log($"[Mythos] Exposure -{amount} ({reason}) → {_exposure}  [{GetTierName()}]");
            OnExposureChanged?.Invoke(old, _exposure);
        }

        // ════════════════════════════════════════════
        // QUERIES — used by BattleManager & DreadContestSystem
        // ════════════════════════════════════════════

        public ExposureTier GetTier() => _exposure switch
        {
            >= ThresholdConsumed  => ExposureTier.Consumed,
            >= ThresholdCorrupted => ExposureTier.Corrupted,
            >= ThresholdHaunted   => ExposureTier.Haunted,
            >= ThresholdUnsettled => ExposureTier.Unsettled,
            _                     => ExposureTier.Calm
        };

        public string GetTierName() => GetTier() switch
        {
            ExposureTier.Calm      => "Calm",
            ExposureTier.Unsettled => "Unsettled",
            ExposureTier.Haunted   => "Haunted",
            ExposureTier.Corrupted => "Corrupted",
            ExposureTier.Consumed  => "Consumed",
            _                      => "Unknown"
        };

        /// <summary>
        /// Sanity penalty applied to all player units at battle start.
        /// 0 at Calm, -5 at Unsettled, -10 at Haunted, -15 at Corrupted.
        /// </summary>
        public int BattleStartSanityPenalty => GetTier() switch
        {
            ExposureTier.Unsettled => -5,
            ExposureTier.Haunted   => -10,
            ExposureTier.Corrupted => -15,
            _                      => 0
        };

        /// <summary>
        /// Bonus Dread Power added to ALL rival Dread Contests at Haunted+.
        /// Makes rivals feel more terrifying as the city decays.
        /// </summary>
        public int DreadContestBonus => _exposure >= ThresholdHaunted ? 2 : 0;

        // ════════════════════════════════════════════
        // SAVE / LOAD
        // ════════════════════════════════════════════

        public int  GetExposureForSave()           => _exposure;
        public void LoadFromSave(int savedExposure) => _exposure = Mathf.Clamp(savedExposure, 0, 100);
    }

    public enum ExposureTier { Calm, Unsettled, Haunted, Corrupted, Consumed }
}
