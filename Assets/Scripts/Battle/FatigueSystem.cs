using System.Collections.Generic;
using UnityEngine;
using KindredSiege.Core;

namespace KindredSiege.Battle
{
    /// <summary>
    /// PILLAR 2 — Fatigue System (GDD §11.4)
    ///
    /// Every battle wears units down. After each expedition, surviving player units
    /// gain Fatigue based on how badly they were hurt and how long the battle lasted.
    /// High Fatigue penalises HP and damage in the NEXT battle. The city's Safehouse
    /// and Apothecary buildings can reduce Fatigue between battles.
    ///
    /// Fatigue thresholds:
    ///   0–49  — Rested:   no penalty
    ///   50–79 — Weary:   −15% max HP, −10% damage
    ///   80–99 — Exhausted: −25% max HP, −20% damage, +15% hesitation chance
    ///   100   — Broken:  unit refuses to deploy (BattleManager skips them)
    ///
    /// Fatigue accrual per battle (applied at BattleEndEvent):
    ///   Base:  HP lost % × 40 (e.g., lost 50% HP → +20 fatigue)
    ///   Time:  +5 per 30 s of battle (capped at +20)
    ///   Death: unit that needed Mercy Token → +15 bonus fatigue
    ///
    /// Attach to the BattleArena alongside BattleManager.
    /// </summary>
    public class FatigueSystem : MonoBehaviour
    {
        public static FatigueSystem Instance { get; private set; }

        // Cached snapshot of unit HP at battle start for damage-taken calculation
        private readonly Dictionary<int, int> _startingHP = new();
        private float _battleDuration;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<BattleStartEvent>(OnBattleStart);
            EventBus.Subscribe<BattleEndEvent>(OnBattleEnd);
            EventBus.Subscribe<MercyDecisionResolvedEvent>(OnMercyResolved);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<BattleStartEvent>(OnBattleStart);
            EventBus.Unsubscribe<BattleEndEvent>(OnBattleEnd);
            EventBus.Unsubscribe<MercyDecisionResolvedEvent>(OnMercyResolved);
        }

        // ════════════════════════════════════════════
        // EVENT HANDLERS
        // ════════════════════════════════════════════

        private void OnBattleStart(BattleStartEvent evt)
        {
            _startingHP.Clear();
            _battleDuration = 0f;

            // Snapshot starting HP for all living player units
            var team1 = BattleManager.Instance?.GetTeam1Controllers();
            if (team1 == null) return;

            foreach (var unit in team1)
            {
                if (unit != null && unit.IsAlive)
                    _startingHP[unit.UnitId] = unit.CurrentHP;
            }
        }

        private void OnBattleEnd(BattleEndEvent evt)
        {
            _battleDuration = evt.Duration;

            var team1 = BattleManager.Instance?.GetTeam1Controllers();
            if (team1 == null) return;

            foreach (var unit in team1)
            {
                if (unit == null || unit.Data == null) continue;
                ApplyPostBattleFatigue(unit);
            }
        }

        /// <summary>Units that needed a Mercy Token to survive get extra fatigue.</summary>
        private readonly HashSet<int> _mercyRecipients = new();

        private void OnMercyResolved(MercyDecisionResolvedEvent evt)
        {
            if (evt.TokenSpent)
                _mercyRecipients.Add(evt.UnitId);
        }

        // ════════════════════════════════════════════
        // FATIGUE CALCULATION
        // ════════════════════════════════════════════

        private void ApplyPostBattleFatigue(UnitController unit)
        {
            int startHP = _startingHP.TryGetValue(unit.UnitId, out int v) ? v : unit.MaxHP;
            int hpLost  = Mathf.Max(0, startHP - unit.CurrentHP);

            // HP damage component: 0–40 fatigue based on % HP lost
            float hpRatio   = unit.MaxHP > 0 ? (float)hpLost / unit.MaxHP : 0f;
            int hpFatigue   = Mathf.RoundToInt(hpRatio * 40f);

            // Time component: +5 per 30s, capped at 20
            int timeFatigue = Mathf.Min(Mathf.FloorToInt(_battleDuration / 30f) * 5, 20);

            // Mercy bonus: surviving 0 HP is harrowing
            int mercyFatigue = _mercyRecipients.Contains(unit.UnitId) ? 15 : 0;

            int totalGained = hpFatigue + timeFatigue + mercyFatigue;

            // Fatigue builds, capped at 100
            unit.Data.FatigueLevel = Mathf.Min(100, unit.Data.FatigueLevel + totalGained);

            EventBus.Publish(new FatigueAppliedEvent
            {
                UnitId       = unit.UnitId,
                UnitName     = unit.UnitName,
                FatigueGained = totalGained,
                TotalFatigue  = unit.Data.FatigueLevel
            });

            if (totalGained > 0)
                Debug.Log($"[Fatigue] {unit.UnitName}: +{totalGained} fatigue (HP:{hpFatigue} + Time:{timeFatigue} + Mercy:{mercyFatigue}) → total {unit.Data.FatigueLevel}");

            _mercyRecipients.Remove(unit.UnitId);
        }

        // ════════════════════════════════════════════
        // PUBLIC API (called by BattleManager at spawn)
        // ════════════════════════════════════════════

        /// <summary>
        /// Returns the stat multipliers for a unit based on its current fatigue level.
        /// BattleManager calls this in SpawnTeam() via UnitController.ApplyModifiers().
        /// </summary>
        public static (float hpMult, float dmgMult, float extraHesitation) GetFatigueModifiers(int fatigueLevel)
        {
            if (fatigueLevel >= 80)
                return (0.75f, 0.80f, 0.15f);   // Exhausted
            if (fatigueLevel >= 50)
                return (0.85f, 0.90f, 0f);       // Weary
            return (1f, 1f, 0f);                  // Rested
        }

        /// <summary>Returns true if a unit is too fatigued to deploy (fatigue == 100).</summary>
        public static bool IsUndeployable(UnitData data) => data != null && data.FatigueLevel >= 100;

        /// <summary>
        /// Reduce fatigue for a unit (called by city rest actions / Safehouse / Apothecary).
        /// </summary>
        public static void Rest(UnitData data, int amount)
        {
            if (data == null) return;
            data.FatigueLevel = Mathf.Max(0, data.FatigueLevel - amount);
            Debug.Log($"[Fatigue] {data.UnitName} rested: -{amount} fatigue → {data.FatigueLevel}");
        }

        /// <summary>Describe fatigue state for tooltips / UI.</summary>
        public static string DescribeFatigue(int level)
        {
            if (level >= 100) return "Broken — cannot deploy.";
            if (level >= 80)  return $"Exhausted ({level}/100) — −25% HP, −20% damage, +15% hesitation.";
            if (level >= 50)  return $"Weary ({level}/100) — −15% HP, −10% damage.";
            return $"Rested ({level}/100).";
        }
    }
}
