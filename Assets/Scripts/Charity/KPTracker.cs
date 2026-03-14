using UnityEngine;
using System;
using System.Collections.Generic;
using KindredSiege.Core;

namespace KindredSiege.Charity
{
    /// <summary>
    /// Tracks Kindness Points (KP) and manages the charity donation system.
    /// Listens to battle results and city production to accumulate KP.
    /// 
    /// For the prototype, this tracks locally. Production version would
    /// aggregate across all players via a server.
    /// </summary>
    public class KPTracker : MonoBehaviour
    {
        public static KPTracker Instance { get; private set; }

        [Header("Season Tracking")]
        [SerializeField] private int currentSeasonKP = 0;
        [SerializeField] private int lifetimeKP = 0;

        [Header("Donation Config")]
        [SerializeField] private float donationRatePerKP = 0.01f; // £0.01 per KP (placeholder)

        public int CurrentSeasonKP => currentSeasonKP;
        public int LifetimeKP => lifetimeKP;
        public float EstimatedDonation => currentSeasonKP * donationRatePerKP;
        public float LifetimeDonation => lifetimeKP * donationRatePerKP;

        // History for transparency dashboard
        public List<KPRecord> History { get; private set; } = new();

        [Serializable]
        public struct KPRecord
        {
            public int Amount;
            public string Source; // "Battle Victory", "Charity Building", "Emissary Survival"
            public float Timestamp;
            public int Season;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<BattleEndEvent>(OnBattleEnd);
            EventBus.Subscribe<KindnessPointsEarnedEvent>(OnKPEarned);

            if (GameManager.Instance != null)
                GameManager.Instance.OnSeasonEnd += OnSeasonEnd;
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<BattleEndEvent>(OnBattleEnd);
            EventBus.Unsubscribe<KindnessPointsEarnedEvent>(OnKPEarned);

            if (GameManager.Instance != null)
                GameManager.Instance.OnSeasonEnd -= OnSeasonEnd;
        }

        private void OnBattleEnd(BattleEndEvent e)
        {
            AddKP(e.KPEarned, $"Battle {e.BattleResult}");
        }

        private void OnKPEarned(KindnessPointsEarnedEvent e)
        {
            AddKP(e.Amount, e.Source);
        }

        /// <summary>Add KP from any source.</summary>
        public void AddKP(int amount, string source)
        {
            if (amount <= 0) return;

            currentSeasonKP += amount;
            lifetimeKP += amount;

            History.Add(new KPRecord
            {
                Amount = amount,
                Source = source,
                Timestamp = Time.time,
                Season = GameManager.Instance?.CurrentSeason ?? 1
            });

            Debug.Log($"[KP] +{amount} from {source} | Season: {currentSeasonKP} | Lifetime: {lifetimeKP}");
        }

        private void OnSeasonEnd()
        {
            int seasonTotal = currentSeasonKP;
            float donation = seasonTotal * donationRatePerKP;

            Debug.Log($"[KP] Season ended! Total KP: {seasonTotal} | Estimated donation: £{donation:F2}");

            EventBus.Publish(new SeasonDonationEvent
            {
                TotalKP = seasonTotal,
                Season = GameManager.Instance?.CurrentSeason ?? 1
            });

            currentSeasonKP = 0; // Reset for new season
        }

        /// <summary>Get KP breakdown by source for the transparency dashboard.</summary>
        public Dictionary<string, int> GetSeasonBreakdown(int season)
        {
            var breakdown = new Dictionary<string, int>();
            foreach (var record in History)
            {
                if (record.Season != season) continue;
                if (!breakdown.ContainsKey(record.Source))
                    breakdown[record.Source] = 0;
                breakdown[record.Source] += record.Amount;
            }
            return breakdown;
        }
    }
}
