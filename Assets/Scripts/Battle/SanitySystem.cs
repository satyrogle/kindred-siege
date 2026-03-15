using UnityEngine;

namespace KindredSiege.Battle
{
    // ═══════════════════════════════════════════════════════
    // SANITY ENUMS  (GDD §5.1 and §5.3)
    // ═══════════════════════════════════════════════════════

    public enum SanityState
    {
        Resolute,    // 80-100: Full effectiveness. May gain Virtues under pressure.
        Stressed,    // 50-79:  Slight accuracy reduction. Occasional hesitation.
        Afflicted,   // 25-49:  AI degrades. Cards may partially fail.
        Broken,      // 1-24:   Severe disruption. May refuse to fight or cower.
        Lost         // 0:      Consumed by madness. Permanently removed from roster.
    }

    public enum AfflictionType
    {
        None,
        Paranoid,    // AI targets allies as threats 15% of the time
        Hopeless,    // AI retreat threshold raised to 50% HP
        Selfish,     // Ignores heal/support orders, self-preservation only
        Irrational   // Random targeting — ignores priority systems
    }

    public enum VirtueType
    {
        None,
        Stalwart,    // Immune to further sanity loss this battle
        Focused,     // +25% damage, AI operates at peak efficiency
        Courageous,  // Inspires allies: +10 sanity to all nearby units every 8 seconds
        Resolute     // Cannot drop below 1 HP for 10 seconds
    }

    /// <summary>
    /// Static helper for sanity state logic, hesitation chances, and stress trait rolls.
    /// This is Pillar 2: Psychological Simulation — directly implementing the MSc thesis
    /// finding that resilience emerges from iterative stress exposure, not its absence.
    /// </summary>
    public static class SanitySystem
    {
        // ─── Thresholds (from GDD §5.1) ───

        public const int ResoluteMin = 80;   // 80-100: Resolute
        public const int StressedMin = 50;   // 50-79: Stressed
        public const int AfflictedMin = 25;  // 25-49: Afflicted
        public const int BrokenMin = 1;      // 1-24:  Broken
        // 0 = Lost (permanent)

        /// <summary>Derive SanityState from a raw sanity int.</summary>
        public static SanityState GetState(int sanity)
        {
            if (sanity >= ResoluteMin) return SanityState.Resolute;
            if (sanity >= StressedMin) return SanityState.Stressed;
            if (sanity >= AfflictedMin) return SanityState.Afflicted;
            if (sanity > 0)            return SanityState.Broken;
            return SanityState.Lost;
        }

        /// <summary>
        /// Probability that a unit's AI tick is skipped entirely due to sanity state.
        /// Resolute = never hesitates. Broken = hesitates half the time.
        /// </summary>
        public static float GetHesitationChance(SanityState state)
        {
            return state switch
            {
                SanityState.Resolute  => 0f,
                SanityState.Stressed  => 0.10f,
                SanityState.Afflicted => 0.25f,
                SanityState.Broken    => 0.50f,
                _                     => 1f    // Lost: always hesitates (unit is gone)
            };
        }

        // ─── Affliction pool (GDD §5.3) ───

        private static readonly AfflictionType[] AfflictionPool =
        {
            AfflictionType.Paranoid,
            AfflictionType.Hopeless,
            AfflictionType.Selfish,
            AfflictionType.Irrational
        };

        // ─── Virtue pool (GDD §5.3) ───

        private static readonly VirtueType[] VirtuePool =
        {
            VirtueType.Stalwart,
            VirtueType.Focused,
            VirtueType.Courageous,
            VirtueType.Resolute
        };

        /// <summary>
        /// Roll for a stress trait when a unit's sanity first crosses below 50.
        /// Veterans (5+ expeditions) have a significantly higher Virtue chance —
        /// modelling the thesis finding: resilience grows from repeated stress exposure.
        /// </summary>
        public static (AfflictionType affliction, VirtueType virtue) RollStressTrait(bool isVeteran)
        {
            // Veteran units are more likely to draw strength from adversity
            float virtueChance     = isVeteran ? 0.30f : 0.10f;
            float afflictionChance = 0.60f;   // 60% chance of affliction regardless

            float roll = Random.value;

            if (roll < virtueChance)
            {
                var v = VirtuePool[Random.Range(0, VirtuePool.Length)];
                return (AfflictionType.None, v);
            }

            if (roll < virtueChance + afflictionChance)
            {
                var a = AfflictionPool[Random.Range(0, AfflictionPool.Length)];
                return (a, VirtueType.None);
            }

            // ~30% chance: neither — unit endures silently
            return (AfflictionType.None, VirtueType.None);
        }

        /// <summary>
        /// Describe a sanity state in plain language for UI tooltips.
        /// </summary>
        public static string Describe(SanityState state)
        {
            return state switch
            {
                SanityState.Resolute  => "Resolute — Fighting at full effectiveness.",
                SanityState.Stressed  => "Stressed — Slight hesitation. Accuracy reduced.",
                SanityState.Afflicted => "Afflicted — AI degraded. Cards may partially fail.",
                SanityState.Broken    => "Broken — Severe disruption. Unit may refuse orders.",
                SanityState.Lost      => "Lost — Consumed by madness.",
                _                     => "Unknown"
            };
        }
    }
}
