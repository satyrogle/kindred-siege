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

        // ─── Horror Rating (GDD §6.3) ───────────────────────────────────────
        // Passive sanity drain applied to ALL player units every 5 seconds.
        // Multiplied by each unit's Comprehension stat.
        // Grunt: 0 | Lieutenant: 1 | Captain: 2 | Overlord: 4 | Undying: +2
        public int HorrorRating
        {
            get
            {
                int baseRating = Rank switch
                {
                    RivalRank.Grunt       => 0,
                    RivalRank.Lieutenant  => 1,
                    RivalRank.Captain     => 2,
                    RivalRank.Overlord    => 4,
                    _                     => 0
                };
                return IsUndying ? baseRating + 2 : baseRating;
            }
        }

        // Sanity drain per 5-second tick (GDD §6.3)
        public int HorrorRatingDrainPerTick
        {
            get
            {
                return HorrorRating switch
                {
                    0 => 0,
                    1 => 2,
                    2 => 4,
                    _ => HorrorRating * 2   // Rating 4 → 8, Rating 6 → 12, etc.
                };
            }
        }

        // ─── Dread Power (GDD §6.2) ─────────────────────────────────────────
        // Base value for taunt contests: Rival Dread Power vs Unit Resistance.
        // Grunt: 5 | Lieutenant: 10 | Captain: 18 | Overlord: 28 | Grudge: +3 | Undying: +10
        public int DreadPower
        {
            get
            {
                int baseDread = Rank switch
                {
                    RivalRank.Grunt       => 5,
                    RivalRank.Lieutenant  => 10,
                    RivalRank.Captain     => 18,
                    RivalRank.Overlord    => 28,
                    _                     => 5
                };
                if (Memory.HasGrudge) baseDread += 3;
                if (IsUndying)        baseDread += 10;
                return baseDread;
            }
        }

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

        // Adaptive traits — specific class counters (Memory Adaptation)
        VanguardSlayer,    // Gained after fighting multiple Vanguards. +20% dmg to them.
        WardenBreaker,     // Gained after fighting multiple Wardens. Ignores Warden armor.
        ShadowCatcher      // Gained after fighting multiple Shadows. Prevents Shadow vanish.
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
