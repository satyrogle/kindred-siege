using System.Collections.Generic;
using UnityEngine;

namespace KindredSiege.Rivalry
{
    /// <summary>
    /// All data for a procedurally generated rival leader.
    /// Rivals are the core of Pillar 3: the Rivalry Engine.
    /// They persist across encounters, remember the player, and grow stronger.
    /// </summary>
    [System.Serializable]
    public class RivalData
    {
        public string RivalId;      // Unique GUID — survives save/load
        public string FirstName;
        public string Epithet;      // e.g. "the Drowned", "Hollow-Eye"
        public string FullName => $"{FirstName} {Epithet}";

        public RivalRank Rank = RivalRank.Grunt;
        public List<RivalTraitType> Traits = new();
        public RivalWeaknessType Weakness;
        public RivalMemory Memory = new();

        // ─── Visual state ───
        public bool HasScar;
        public int PromotionCount;
        public bool IsUndying;
        public float SizeMultiplier = 1f;   // +0.15 per promotion

        // ─── Combat stats (scale with rank) ───
        public int BaseHP     = 100;
        public int BaseDamage = 10;

        // ─── Lifecycle ───
        public bool IsDefeated;
    }

    // ─────────────────────────────────────────────────
    // RIVAL MEMORY — what the rival has logged about you
    // ─────────────────────────────────────────────────

    /// <summary>
    /// Everything a rival remembers about encounters with the player.
    /// This drives trait adaptation and taunt targeting in the Rivalry Engine.
    /// </summary>
    [System.Serializable]
    public class RivalMemory
    {
        public int WinsAgainstPlayer;
        public int LossesAgainstPlayer;

        // Units this rival has personally killed
        public List<string> KilledPlayerUnits = new();

        // What hurt this rival — used for counter-trait generation
        public bool ScarredByMarksman;
        public bool DefeatedByRitualCards;

        // If player consistently avoids this rival
        public int TimesAvoided;

        // Grudge: first unit this rival killed becomes the grudge target
        // (Rival will specifically taunt / hunt this unit type in future encounters)
        public string GrudgeTargetUnitName = "";

        // Human-readable log for the Rivalry Board UI
        public List<string> EncounterLog = new();

        public bool HasGrudge => !string.IsNullOrEmpty(GrudgeTargetUnitName);
    }

    // ─────────────────────────────────────────────────
    // ENUMS
    // ─────────────────────────────────────────────────

    public enum RivalRank
    {
        Grunt,
        Lieutenant,
        Captain,
        Overlord
    }

    public enum RivalTraitType
    {
        // Default pool — assigned on generation
        Rage,              // Charges when wounded (< 40% HP)
        Fearful,           // Flees at 30% HP
        Tactical,          // Switches targets mid-battle
        Resilient,         // Slower to retreat — higher threshold
        Ambusher,          // Initiates combat with a burst of aggression

        // Adaptive traits — earned through memory
        MarksmanHunter,    // Gained after being scarred by a Marksman — hunts Marksmen first
        EldritchResistant, // Gained after being defeated by Ritual cards
        Vendetta,          // Gained when one of their allies is killed — more aggressive
        Grudge,            // Gained when they kill a player unit — taunts that unit type
        Bold,              // Gained when player avoids them repeatedly — starts ambushing
    }

    public enum RivalWeaknessType
    {
        Flanking,          // Takes bonus damage from flank attacks (Shadow units)
        Fire,              // Vulnerable to fire-type abilities
        Light,             // Vulnerable to light-based attacks
        Isolation,         // Weaker when no allies are nearby
        Disruption,        // Vulnerable to debuffs and status effects
    }
}
