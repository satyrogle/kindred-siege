using System;

namespace KindredSiege.Sim.Battle
{
    /// The parry meter (Jacob asked for it on 26 Oct 2026, HANDOFF_LOG 11823; 31 Oct: "Go, lock and build"; DESIGN_LOCKS, "The
    /// parry meter"; FightRules.Meter, campaign rules 22). Every parry your side lands fills one of Bars bars: a big beat, a
    /// small beat or a cast, each beat of a move with several. Your chief spell (Directive.Eclipse, whatever the spell is) is
    /// cast with all the bars and costs no order points. Each held bar makes your hits with a Combo or a Cash bonus
    /// PercentPerBar stronger, in the Parry damage layer. A miss takes none away; Fall Back earns none. Only your side has a
    /// meter. Every number is a starting value.
    public static class MeterRules
    {
        public const int Bars = 3;
        public const int PercentPerBar = 10;

        /// The side with the meter: yours.
        public const int Side = 0;
    }

    public sealed partial class BattleSimulation
    {
        private readonly bool _meterOn;

        /// Whether this fight plays the parry meter: campaign rules 22.
        public bool MeterOn => _meterOn;

        /// How many bars this side's meter holds, 0 to MeterRules.Bars; 0 for a side with no meter. Read only, for the screen.
        public int ParryBars(int side)
        {
            if (side < 0 || side >= _sides.Length) throw new ArgumentOutOfRangeException(nameof(side));
            return MeterFor(side) ? _sides[side].ParryBars : 0;
        }

        private bool MeterFor(int side) => _meterOn && side == MeterRules.Side;

        // A parry this side landed fills a bar. Emit sees every Parry event, from every kind of beat.
        private void MeterParried(int side)
        {
            if (MeterFor(side) && _sides[side].ParryBars < MeterRules.Bars) _sides[side].ParryBars++;
        }

        // Held bars make this side's payoffs stronger: any hit of theirs that carries a Combo or a Cash bonus.
        private void MeterBoost(int side, Hit hit)
        {
            if (!MeterFor(side) || _sides[side].ParryBars <= 0) return;
            if (hit.On(HitLayer.Combo) == 0 && hit.On(HitLayer.Cash) == 0) return;
            // Charged Branches (rules 29) adds to each bar.
            hit.Add(HitLayer.Parry, _sides[side].ParryBars * PerBar(side));
        }
    }
}
