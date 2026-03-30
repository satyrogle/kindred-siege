using System.Collections.Generic;
using UnityEngine;
using KindredSiege.City;
using KindredSiege.Core;
using KindredSiege.Rivalry;

namespace KindredSiege.Battle
{
    /// <summary>
    /// GDD §6.2 — Dread Contest System.
    ///
    /// High-ranking rivals periodically issue taunts at all player units.
    /// Each taunt triggers RollDreadContest() on every living team-1 unit.
    ///
    /// Taunt intervals (seconds between taunts):
    ///   Lieutenant : 20s
    ///   Captain    : 14s
    ///   Overlord   : 9s
    ///   Undying    : –3s bonus (shorter interval)
    ///
    /// Attach to the same GameObject as BattleManager.
    /// </summary>
    public class DreadContestSystem : MonoBehaviour
    {
        public static DreadContestSystem Instance { get; private set; }

        private float _tauntTimer   = 0f;
        private float _tauntInterval = 15f;
        private bool  _active        = false;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<BattleStartEvent>(OnBattleStart);
            EventBus.Subscribe<BattleEndEvent>(OnBattleEnd);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<BattleStartEvent>(OnBattleStart);
            EventBus.Unsubscribe<BattleEndEvent>(OnBattleEnd);
        }

        private void OnBattleStart(BattleStartEvent evt)
        {
            var rival = BattleManager.Instance?.GetActiveRival();
            if (rival == null || rival.Rank == RivalRank.Grunt)
            {
                _active = false;
                return;
            }

            _tauntInterval = GetTauntInterval(rival);
            _tauntTimer    = _tauntInterval; // First taunt after one full interval
            _active        = true;

            Debug.Log($"[DreadContest] {rival.FullName} [{rival.Rank}] will taunt every {_tauntInterval:F0}s.");
        }

        private void OnBattleEnd(BattleEndEvent evt)
        {
            _active = false;
        }

        private void Update()
        {
            if (!_active) return;

            _tauntTimer -= Time.deltaTime;
            if (_tauntTimer > 0f) return;

            var rival = BattleManager.Instance?.GetActiveRival();
            if (rival == null) { _active = false; return; }

            _tauntTimer = _tauntInterval;
            IssueTaunt(rival);
        }

        private void IssueTaunt(RivalData rival)
        {
            var team1 = BattleManager.Instance?.GetTeam1Controllers();
            if (team1 == null || team1.Count == 0) return;

            Debug.Log($"[DreadContest] {rival.FullName} issues a taunt! (DreadPower: {rival.DreadPower})");

            int dreadPower = rival.DreadPower + (MythosExposure.Instance?.DreadContestBonus ?? 0);

            foreach (var unit in team1)
            {
                if (unit != null && unit.IsAlive)
                    unit.RollDreadContest(dreadPower, rival.FullName);
            }
        }

        // ────────────────────────────────────────────
        // Interval table (GDD §6.2 — shorter = more pressure)
        // ────────────────────────────────────────────

        private static float GetTauntInterval(RivalData rival)
        {
            float interval = rival.Rank switch
            {
                RivalRank.Lieutenant => 20f,
                RivalRank.Captain    => 14f,
                RivalRank.Overlord   => 9f,
                _                    => 20f
            };

            // Undying rivals taunt 3 seconds more frequently
            if (rival.IsUndying) interval = Mathf.Max(5f, interval - 3f);

            return interval;
        }
    }
}
