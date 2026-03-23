namespace KindredSiege.Battle
{
    // ═══════════════════════════════════════════════════════════════════════
    // GAMBIT TYPES  (GDD §4.1)
    // Each value maps to a factory method in GambitLibrary.
    // Players start with Default-tier gambits; Archive upgrades unlock higher tiers.
    // ═══════════════════════════════════════════════════════════════════════

    public enum GambitType
    {
        None = 0,

        // ── Warden (Tank) ──────────────────────────────────
        Warden_HoldTheLine,        // Default: stay at spawn, attack in range
        Warden_ProtectTheWeak,     // Default: rush any ally below 30% HP, draw aggro
        Warden_HuntTheLeader,      // Archive L1: target highest-HP enemy only
        Warden_RecklessAbandon,    // Archive L2: charge nearest, +15% dmg, -5 sanity/tick

        // ── Marksman (Ranged DPS) ──────────────────────────
        Marksman_KeepDistance,     // Default: always maintain max range
        Marksman_FocusFire,        // Default: lock a single target until dead
        Marksman_SuppressiveShots, // Archive L1: target nearest, never advance
        Marksman_PrecisionTarget,  // Archive L2: target the lowest-sanity enemy (break the broken)

        // ── Occultist (Healer) ─────────────────────────────
        Occultist_TendTheWounded,  // Default: only heal, never attack unless surrounded
        Occultist_PsychicBarrier,  // Default: prioritise sanity restoration over HP
        Occultist_DespairWave,     // Archive L1: attack to deal eldritch sanity damage
        Occultist_SacredRite,      // Archive L2: channel in place for +30 self-sanity, no movement

        // ── Berserker (Melee DPS) ──────────────────────────
        Berserker_BloodFrenzy,     // Default: attack nearest regardless — ally or enemy
        Berserker_DeathCharge,     // Default: ignore all retreats, charge strongest
        Berserker_BerserkRage,     // Archive L1: +5% dmg per HP% lost (stacks)
        Berserker_HuntThePack,     // Archive L2: target whichever enemy cluster is largest

        // ── Investigator (Debuffer) ────────────────────────
        Investigator_ObserveReport,  // Default: only analyse — never engage directly
        Investigator_WeaknessExploit,// Default: bonus damage to analysed targets
        Investigator_ForbiddenScan,  // Archive L1: analyse every enemy hit (loses MaxSanity)
        Investigator_TacticalWithdraw,// Archive L2: retreat and analyse from maximum range

        // ── Shadow (Assassin) ──────────────────────────────
        Shadow_AssassinProtocol,   // Default: wait for enemy to be engaged, then strike flank
        Shadow_VanishAndStrike,    // Default: retreat 3 units after each attack
        Shadow_LeaderAssassination,// Archive L1: only target highest-HP enemy, ignore all others
        Shadow_GhostStep,          // Archive L2: only move when not targeted by any enemy

        // ── Herald (Buffer) ────────────────────────────────
        Herald_SanityBeacon,       // Default: stand still, emit continuous sanity aura
        Herald_BattleCry,          // Default: boost ally damage instead of sanity for 10 sec
        Herald_LastRites,          // Archive L1: immediately boost survivors when any ally dies
        Herald_MartyrSignal,       // Archive L2: allow self to take damage; convert pain to ally resolve

        // ── Vessel (Mercy Specialist) ──────────────────────
        Vessel_GhostProtocol,      // Default: flee to furthest point from any enemy
        Vessel_SacredWard,         // Default: always stay directly behind a living Warden
        Vessel_DesperatePlea,      // Archive L1: when near death, generates +1 extra Mercy Token
    }

    // ═══════════════════════════════════════════════════════════════════════
    // PHOBIA TYPES  (GDD §5.5)
    // Gained when a unit hits 0 sanity but is saved by a Mercy Token.
    // The phobia persists on the UnitData ScriptableObject between battles.
    // ═══════════════════════════════════════════════════════════════════════

    public enum PhobiaType
    {
        None = 0,
        BloodPhobia,      // Witnessing any death deals extra -8 sanity (stacks with AllyDied penalty)
        EldritchPhobia,   // Comprehension multiplier on all eldritch hits raised by 0.5
        SolitudePhobia,   // Takes -3 sanity per 5 seconds when no ally is within 4 units
        ViolencePhobia,   // Takes -2 sanity each time this unit deals damage
        DarkPhobia,       // Takes -4 sanity per 10 seconds in prolonged combat (stacks with ProlongedCombat)
    }

    // ═══════════════════════════════════════════════════════════════════════
    // DIRECTIVE TYPES  (GDD §4.2)
    // The player's limited during-battle interventions.
    // ═══════════════════════════════════════════════════════════════════════

    public enum DirectiveType
    {
        None        = 0,
        FocusFire   = 1,   // 1pt — all units target the clicked enemy for 8 seconds
        HoldPosition= 2,   // 1pt — unit stops moving, attacks in range for 10 seconds
        FallBack    = 3,   // 1pt — unit retreats to rear immediately
        Unleash     = 4,   // 2pt — +20% damage, ignore retreat thresholds, -5 sanity per tick
        Sacrifice   = 5,   // 3pt — 2x damage charge, unit dies at end
        InvokeMercy = 6,   // Token — revive a unit at 30% HP + 15 sanity
    }
}
