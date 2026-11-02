using System;
using KindredSiege.Sim.Battle;

namespace KindredSiege.Sim.Learning
{
    // GluttonTalent lives in Battle (PalKit.cs) from rules 29, with the same names and numbers, where the fight reads it.
    // WhispererHabit and SquadPair live in Battle (PalKit.cs), where the fight reads them (rules 24).

    /// A talent the Glutton lent this run, and whether Feed doubled it.
    public sealed class HeldTalent
    {
        internal HeldTalent(GluttonTalent talent, bool fed)
        {
            Talent = talent;
            Fed = fed;
        }

        public GluttonTalent Talent { get; }
        public bool Fed { get; internal set; }
    }

    /// One of the Shade's routes this run, at its form.
    public sealed class HeldRoute
    {
        internal HeldRoute(ShadeRoute route, int form)
        {
            Route = route;
            Form = form;
        }

        public ShadeRoute Route { get; }
        public int Form { get; internal set; }
    }

    /// One class's habit. From rules 24 it is set at camp and kept, and the Whisperer's road card can upgrade it for the run.
    public sealed class HeldHabit
    {
        internal HeldHabit(WhispererHabit habit, bool upgraded = false)
        {
            Habit = habit;
            Upgraded = upgraded;
        }

        public WhispererHabit Habit { get; }
        public UnitClass Class => PalStoreRules.ClassOf(Habit);

        /// Upgraded by the Whisperer this run (rules 24).
        public bool Upgraded { get; internal set; }
    }

    /// The rules of the Pals' stores (campaign rules 23, DESIGN_LOCKS "The floor under the three Pals").
    public static class PalStoreRules
    {
        public const int TalentCount = 35;
        public const int HabitCount = WhisperRules.HabitCount;
        public const int PairCount = WhisperRules.PairCount;

        /// A fighter's ranks, and the rank that opens his second slot.
        public const int MaxRank = 3;
        public const int SecondSlotRank = 3;

        /// The class a habit belongs to.
        public static UnitClass ClassOf(WhispererHabit habit) => WhisperRules.ClassOf(habit);
    }
}
