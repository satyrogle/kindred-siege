namespace KindredSiege.Battle
{
    /// <summary>
    /// Terrain hazard types for the battlefield grid (GDD §12).
    ///
    /// All tiles remain walkable — hazards impose movement and sanity costs
    /// without blocking pathfinding, keeping combat fluid.
    ///
    ///   Deep Water     — Harbour district flooded tiles
    ///   Shrine         — Ancient worship sites, sanity refuge
    ///   EldritchGround — Corrupted earth, amplifies cosmic horror
    /// </summary>
    public enum HazardType
    {
        None           = 0,
        DeepWater      = 1,   // Half MoveSpeed + -3 sanity per second
        Shrine         = 2,   // +2 sanity per second + blocks Horror Rating drain
        EldritchGround = 3,   // Doubles Comprehension multiplier on all sanity damage
    }
}
