using System;

namespace KindredSiege.Sim.Battle
{
    /// The sizing pass (campaign rules 28; FightRules.Sized; Jacob in chat, 1 Nov 2026: "okay go"). The Pals' picks made big
    /// enough to decide fights, sized with the builds test (tools/sim_checks/sweep/PalSweep.cs): a run leaning into one Pal should
    /// win about 10 more runs in 100 than the same number of random picks. Leaning pays most: a route's later forms, a limb's
    /// later talents, a star's higher rank and a flavour's later combos each give more than the one before. Every number is a
    /// starting value, for Jacob's play to change.
    public static class SizeRules
    {
        /// The Shade: how many percent bigger her trick is at its route's form: its damage, Sanctuary's shields, Backstab's
        /// crit and Fear's nerve and panic.
        public const int ShadeForm1Percent = 50;
        public const int ShadeForm2Percent = 200;
        public const int ShadeForm3Percent = 400;

        /// What her route prices keep, in percent.
        public const int ShadePricePercent = 50;

        /// The Glutton: every fight talent's bonus this many percent bigger, and this many more for each other talent held in
        /// its limb. A bonus under GluttonMinBonus is a count (a place of reach, a spring, a second) and stays as it is.
        public const int GluttonPercent = 100;
        public const int GluttonClimbPercent = 300;
        public const int GluttonMinBonus = 10;

        /// The Whisperer: a fighter's blows and heals this many percent bigger at rank 2 and at rank 3.
        public const int Rank2Percent = 0;
        public const int Rank3Percent = 600;

        /// The Butcher: every burst this many percent bigger (Crush's hit, Exposed's crit, Stasis's freeze), and this many more for
        /// each rank past the first of combos of its flavour the side wears.
        public const int BurstPercent = 0;
        public const int FlavourPercent = 500;

        /// And each mark a combo lays, this many more stacks for each rank past the first of combos of its flavour the side
        /// wears, so a run leaning into one flavour bursts sooner.
        public const int FlavourStacks = 1;

        /// What his pacts cost keeps, in percent (Heavy's slower turns, Unnerving's nerve, Borrowed time's health).
        public const int PactPercent = 50;

        /// The duos, sized (DuoRules has their numbers before rules 28). Crowned Marks: the star lays this many times his stacks.
        /// Marked Tricks: each trick hit lays this many stacks. Gorged Bursts and Gorged Tricks: percent a talent level. Gorged
        /// Tricks: what her prices keep. Shadow Star: the star casts from her tricks at most once in this long. Fattened Star:
        /// his talents' bonuses this many percent bigger again on the star.
        public const int CrownStacks = 3;
        public const int MarkedStacks = 2;
        public const int GorgedBurstPercent = 100;
        public const int GorgedTrickPercent = 60;
        public const int GorgedPricePercent = 25;
        public const int ShadowStarTicks = 3 * SimClock.TicksPerSecond / 2;
        public const int FattenedPercent = 200;

        /// The Butcher's flavours (campaign rules 29, BattleSimulation.Flavours.cs). For each rank past the first of combos of a
        /// combo's flavour the side wears: its hits (Black Hole Round's crush, Event Horizon's cleave, Execution Corridor's blow,
        /// a Frozen Flurry's thaw) this many percent bigger, its pins, freezes and slows this many percent longer, and its pulls
        /// this many places further.
        public const int FlavourComboPercent = 300;
        public const int FlavourHoldPercent = 200;
        public const int FlavourReach = 1;

        /// At this many ranks of one flavour, its final form: a Crush burst crushes the whole row of the one it burst on; an
        /// Exposed burst's wide open jumps on to the next enemy beside him when it is spent, ExposedJumps times; a Stasis burst
        /// freezes every enemy for StasisAllTicks.
        public const int FinalRanks = 3;

        /// From rules 30, the stacks of his combo's mark every blow of a fighter wearing one lays.
        public const int HitStacks = 1;
        public const int ExposedJumps = 1;
        public const int StasisAllTicks = SimClock.TicksPerSecond;

        /// The final forms' Burst event details.
        public const string CrushRowDetail = "crush.row";
        public const string ExposedJumpDetail = "exposed.jump";
        public const string StasisAllDetail = "stasis.all";
    }

    public sealed partial class BattleSimulation
    {
        private readonly bool _sizedOn;

        /// Whether this fight plays the sized picks: campaign rules 28.
        public bool SizedOn => _sizedOn;

        // ------------------------------------------------------------ the Shade

        // How many percent bigger this side's trick on this route is, at its form.
        private int ShadeSized(int side, ShadeRoute route)
        {
            if (!_sizedOn || side < 0 || side > 1) return 0;
            switch (FormOf(side, route))
            {
                case 1: return SizeRules.ShadeForm1Percent;
                case 2: return SizeRules.ShadeForm2Percent;
                case 3: return SizeRules.ShadeForm3Percent;
                default: return 0;
            }
        }

        // The route whose trick dealt damage with this detail.
        private static ShadeRoute RouteOfTrick(string detail)
        {
            if (detail == ShadeRules.MirrorDetail) return ShadeRoute.Mirror;
            if (detail == ShadeRules.MineDetail) return ShadeRoute.RiftMine;
            if (detail == ShadeRules.RuptureDetail) return ShadeRoute.Rupture;
            if (detail == ShadeRules.BreachDetail || detail == ShadeRules.RamDetail) return ShadeRoute.Breach;
            if (detail == ShadeRules.EchoDetail) return ShadeRoute.Echo;
            if (detail == ShadeRules.TurncoatDetail) return ShadeRoute.Fear;
            return ShadeRoute.None;
        }

        // A number of her trick on this route, sized.
        private int ShadeSized(int side, ShadeRoute route, int value)
        {
            int more = ShadeSized(side, route);
            return more == 0 ? value : value * (100 + more) / 100;
        }

        // What is left of her route prices, sized.
        private int PriceSized(int left)
        {
            return _sizedOn ? left * SizeRules.ShadePricePercent / 100 : left;
        }

        // ------------------------------------------------------------ the Glutton

        // A talent's bonus on this side, sized: bigger, and bigger again for each other talent held in its limb.
        private int GluttonSized(int side, GluttonTalent talent, int bonus)
        {
            if (!_sizedOn || bonus < SizeRules.GluttonMinBonus || side < 0 || side > 1) return bonus;
            int limb = GluttonRules.LimbOf(talent), others = 0;
            for (int slot = 0; slot < GluttonRules.PerLimb; slot++)
            {
                GluttonTalent t = GluttonRules.At(limb, slot);
                if (t != talent && KitOf(side).TalentLevel(t) > 0) others++;
            }
            return bonus * (100 + SizeRules.GluttonPercent + SizeRules.GluttonClimbPercent * others) / 100;
        }

        // ------------------------------------------------------------ the Whisperer

        // How many percent bigger this fighter's blows and heals are at his rank.
        private int RankSized(BattleUnit unit)
        {
            if (!_sizedOn || unit == null) return 0;
            int rank = SpecOf(unit).Rank;
            return rank >= 3 ? SizeRules.Rank3Percent : rank == 2 ? SizeRules.Rank2Percent : 0;
        }

        // ------------------------------------------------------------ the Butcher

        // How many percent bigger this one's burst of this mark is: BurstPercent, and FlavourPercent for each rank past the first
        // of combos of its flavour his side wears.
        private int BurstSized(BattleUnit by, StatusKind mark)
        {
            if (!_sizedOn || by == null) return 0;
            return SizeRules.BurstPercent + SizeRules.FlavourPercent * Math.Max(0, FlavourRanks(by.Side, mark) - 1);
        }

        // The stacks a combo lays, sized: FlavourStacks more for each rank past the first of its flavour's combos.
        private int StacksSized(BattleUnit wearer, StatusKind mark, int stacks)
        {
            if (!_sizedOn || SizeRules.FlavourStacks == 0) return stacks;
            return stacks + SizeRules.FlavourStacks * Math.Max(0, FlavourRanks(wearer.Side, mark) - 1);
        }

        // The ranks of the combos this side wears that lay this mark.
        private int FlavourRanks(int side, StatusKind mark)
        {
            int ranks = 0;
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (u.Side != side || u.Benched) continue;
                UnitSpec spec = SpecOf(u);
                if (ClassSpells.IsCombo(BendOf(u)) && MarkRules.Of(BendOf(u)) == mark) ranks += spec.ComboRank;
                if (ClassSpells.IsCombo(Bend2Of(u)) && MarkRules.Of(Bend2Of(u)) == mark) ranks += spec.ComboRank2;
            }
            return ranks;
        }
    }
}
