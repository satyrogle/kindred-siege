using System.Collections.Generic;
using UnityEngine;
using KindredSiege.Battle;

namespace KindredSiege.Units
{
    /// <summary>
    /// GDD §Unit Bonds.
    ///
    /// Units that survive expeditions together accumulate co-survival counts.
    /// At BondThreshold (2) shared expeditions they form a Bond, which grants:
    ///   • +8% damage while the partner is alive
    ///   • +5 sanity on battle start
    ///   • "For them" reaction: when a bonded partner dies the survivor gains
    ///     +15 sanity instead of losing the normal ally-death amount
    ///
    /// Usage:
    ///   RecordCoSurvival()  — call from BattleManager.EndBattle() for all surviving team1 units
    ///   ApplyBondEffects()  — call from BattleManager.StartBattle() after all units are spawned
    ///   NotifyPartnerDied() — call from UnitController.OnWitnessAllyDeath() when the dead unit
    ///                         is a bonded partner
    /// </summary>
    public static class BondSystem
    {
        public const int    BondThreshold         = 2;     // Shared expeditions needed
        public const float  BondDamageBonus       = 0.08f; // +8% damage
        public const int    BondStartingSanity    = 5;     // +5 sanity at battle start
        public const int    ForThemSanityAmount   = 15;    // Sanity gained when partner dies

        // ════════════════════════════════════════════
        // RECORD CO-SURVIVAL (call after battle ends)
        // ════════════════════════════════════════════

        /// <summary>
        /// For every pair in <paramref name="survivors"/>, record one shared expedition.
        /// Bonds are formed automatically once the threshold is crossed.
        /// </summary>
        public static void RecordCoSurvival(IReadOnlyList<UnitController> survivors)
        {
            if (survivors == null || survivors.Count < 2) return;

            for (int i = 0; i < survivors.Count; i++)
            {
                for (int j = i + 1; j < survivors.Count; j++)
                {
                    var a = survivors[i];
                    var b = survivors[j];
                    if (a?.Data == null || b?.Data == null) continue;

                    // Add each other's asset name to the co-survival list
                    a.Data.CoSurvivedWith.Add(b.Data.name);
                    b.Data.CoSurvivedWith.Add(a.Data.name);

                    // Count occurrences to determine bond
                    int countAB = CountOccurrences(a.Data.CoSurvivedWith, b.Data.name);
                    int countBA = CountOccurrences(b.Data.CoSurvivedWith, a.Data.name);

                    if (countAB >= BondThreshold && !a.Data.BondedWith.Contains(b.Data.name))
                    {
                        a.Data.BondedWith.Add(b.Data.name);
                        Debug.Log($"[Bond] {a.Data.UnitName} and {b.Data.UnitName} have formed a Bond!");
                    }
                    if (countBA >= BondThreshold && !b.Data.BondedWith.Contains(a.Data.name))
                    {
                        b.Data.BondedWith.Add(a.Data.name);
                    }
                }
            }
        }

        // ════════════════════════════════════════════
        // APPLY BOND EFFECTS (call after spawning team1)
        // ════════════════════════════════════════════

        /// <summary>
        /// Scan <paramref name="team"/> for bonded pairs both present in this battle.
        /// For each active bond: apply damage bonus and starting sanity bonus.
        /// Sets BondedPartnerName on each controller so partner-death reactions work.
        /// </summary>
        public static void ApplyBondEffects(List<UnitController> team)
        {
            if (team == null || team.Count < 2) return;

            // Build asset-name → controller map
            var nameMap = new Dictionary<string, UnitController>();
            foreach (var uc in team)
                if (uc?.Data != null) nameMap[uc.Data.name] = uc;

            foreach (var uc in team)
            {
                if (uc?.Data?.BondedWith == null) continue;

                foreach (var partnerName in uc.Data.BondedWith)
                {
                    if (!nameMap.TryGetValue(partnerName, out var partner)) continue;

                    // Both are present — activate bond
                    uc.ActiveBondDamageBonus      += BondDamageBonus;
                    uc.BondedPartnerName           = partner.UnitName; // Runtime display name
                    uc.ModifySanity(BondStartingSanity, "BondStart");

                    Debug.Log($"[Bond] Active: {uc.Data.UnitName} ↔ {partner.Data.UnitName} " +
                              $"(+{BondDamageBonus:P0} dmg, +{BondStartingSanity} sanity)");
                }
            }
        }

        // ════════════════════════════════════════════
        // PARTNER DIED REACTION
        // ════════════════════════════════════════════

        /// <summary>
        /// Call from UnitController.OnWitnessAllyDeath() when the dead unit is a bonded partner.
        /// Returns true if this was a bond reaction (caller should skip the normal sanity loss).
        /// </summary>
        public static bool NotifyPartnerDied(UnitController survivor, string deadUnitName)
        {
            if (survivor?.Data == null) return false;
            if (string.IsNullOrEmpty(survivor.BondedPartnerName)) return false;
            if (survivor.BondedPartnerName != deadUnitName) return false;

            survivor.ModifySanity(ForThemSanityAmount, "ForThem");
            survivor.ActiveBondDamageBonus = 0f; // Bond is severed on partner death
            survivor.BondedPartnerName     = null;
            Debug.Log($"[Bond] {survivor.UnitName}: \"For them.\" (+{ForThemSanityAmount} sanity)");
            return true;
        }

        // ════════════════════════════════════════════
        // HELPERS
        // ════════════════════════════════════════════

        private static int CountOccurrences(List<string> list, string value)
        {
            int count = 0;
            foreach (var s in list) if (s == value) count++;
            return count;
        }

        /// <summary>Returns all active bond pairs for a given unit (asset names).</summary>
        public static List<string> GetBondedNames(UnitData unit)
        {
            return unit?.BondedWith ?? new List<string>();
        }

        /// <summary>How many shared expeditions two units have (before bonding).</summary>
        public static int GetCoSurvivalCount(UnitData a, UnitData b)
        {
            if (a?.CoSurvivedWith == null) return 0;
            return CountOccurrences(a.CoSurvivedWith, b.name);
        }
    }
}
