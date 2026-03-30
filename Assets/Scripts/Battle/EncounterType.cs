namespace KindredSiege.Battle
{
    /// <summary>
    /// GDD §Encounter Types — 6 distinct battle scenarios.
    ///
    /// Each type modifies enemy spawn composition, win condition, and
    /// any pre-battle setup (flanking positions, ritual timers, etc).
    ///
    /// Set on BattleManager via SetActiveEncounterType() before StartBattle().
    /// LighthouseMapPanel assigns the type when the player confirms a path.
    /// </summary>
    public enum EncounterType
    {
        /// <summary>Standard — destroy all enemies. Default fallback.</summary>
        Annihilation    = 0,

        /// <summary>Survive for 90 seconds. Win if any player unit still lives.</summary>
        Survival        = 1,

        /// <summary>The rival is guaranteed present and must be killed specifically.</summary>
        RivalHunt       = 2,

        /// <summary>Enemies spawn in a flanking formation (some behind player spawn zone).</summary>
        Ambush          = 3,

        /// <summary>
        /// A Ritual Keeper stands at the back of the enemy line.
        /// Players must kill the Keeper within 75 seconds or lose.
        /// </summary>
        Ritual          = 4,

        /// <summary>
        /// A friendly Vessel unit starts near the enemy zone.
        /// Players win only if the Vessel survives to the end.
        /// </summary>
        Rescue          = 5,
    }

    public static class EncounterTypeInfo
    {
        public static string GetName(EncounterType t) => t switch
        {
            EncounterType.Annihilation => "Annihilation",
            EncounterType.Survival     => "Survival",
            EncounterType.RivalHunt    => "Rival Hunt",
            EncounterType.Ambush       => "Ambush",
            EncounterType.Ritual       => "Ritual",
            EncounterType.Rescue       => "Rescue",
            _                          => t.ToString()
        };

        public static string GetDescription(EncounterType t) => t switch
        {
            EncounterType.Annihilation => "Destroy every enemy. Standard engagement.",
            EncounterType.Survival     => "Hold the line for 90 seconds. Outlast the tide.",
            EncounterType.RivalHunt    => "Your rival is here. Destroy them before they destroy you.",
            EncounterType.Ambush       => "You've walked into a trap. Enemies encircle the position.",
            EncounterType.Ritual       => "A Keeper performs a dark rite. Kill them within 75 seconds or all is lost.",
            EncounterType.Rescue       => "A survivor is stranded behind enemy lines. Keep them alive.",
            _                          => ""
        };
    }
}
