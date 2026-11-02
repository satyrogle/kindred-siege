using System;

namespace KindredSiege.Sim.Battle
{
    /// The damage layers (DESIGN_LOCKS, "Builds: one theme per Pal", step 2; Jacob, 30 Oct 2026: "Yes, start step 2"). From
    /// campaign rules 17 (FightRules.Layers) every hit and heal goes through one pipeline: a base, then each layer's bonuses
    /// added together and the layers multiplied, rounded once at the end, with what is left over kept for the next one on the
    /// same fighter (BattleSimulation.Layers.cs). A bonus is percent points on its layer's 100 and a cut is negative points,
    /// and no layer goes below 0, so a fifth copy of one bonus adds its points while a bonus on a new layer multiplies
    /// everything. In the order a hit takes them.
    public enum HitLayer
    {
        /// Gear, quirks and traits: the increased pile.
        Increased,

        /// The orders' push: Unleash, Frenzy, Rally and a Rise.
        Orders,

        /// A band's, a tribe's or a chief's own: berserk, a flood, a Drive, his Basin, Open Veins, an Eclipse that wore him
        /// out, a Turned Hunt.
        Kit,

        /// A class spell's own weight, and the Medic's Covered Treatment.
        Spell,

        /// Status payoffs: a band weak to Focus Fire.
        Status,

        /// The Butcher's combos.
        Combo,

        /// A mark cashed: a snare's crit, an Opening.
        Cash,

        /// Pacts. Nothing pays into it yet.
        Pact,

        /// The held parry bars (DESIGN_LOCKS, 28 Oct 2026). Nothing pays into it until they are built.
        Parry,

        /// The Pals' sized picks (campaign rules 31, BattleSimulation.Sized.cs): a Shade trick's form, a star's rank.
        Pal,

        /// Cuts: a Hold's soak, a Warden's Guard, a wall, the cage bear's ribcage.
        Guard,

        /// Cuts: armour, Thick Hide, gear against big hits.
        Armor
    }

    /// One hit or heal on its way through the pipeline. Worked in millionths of a point, so only the end rounds to whole points.
    public sealed class Hit
    {
        /// Millionths of a point in one point.
        public const long Unit = 1000000;

        private readonly int[] _percent = new int[(int)HitLayer.Armor + 1];

        /// A hit worth this many points before its layers.
        public Hit(int points)
        {
            Base = points * Unit;
        }

        /// A hit worth this percent of a number (a share of health, a percent of power), exactly, nothing rounded off.
        public static Hit Percent(long of, int percent)
        {
            var hit = new Hit(0);
            hit.Base = of * Unit * percent / 100;
            return hit;
        }

        /// What it is worth before its layers, in millionths of a point.
        public long Base { get; private set; }

        /// The percent points a layer holds.
        public int On(HitLayer layer) => _percent[(int)layer];

        /// A bonus on a layer, in percent points; a cut is negative.
        public Hit Add(HitLayer layer, int percent)
        {
            _percent[(int)layer] += percent;
            return this;
        }

        /// The base taken this many times: Stopped Bullets' shots in one.
        public Hit Times(int count)
        {
            Base *= count;
            return this;
        }

        /// The same hit, for another target.
        public Hit Copy()
        {
            var copy = new Hit(0) { Base = Base };
            Array.Copy(_percent, copy._percent, _percent.Length);
            return copy;
        }

        /// What it is worth now, in millionths of a point: every layer taken, in order.
        public long Value
        {
            get
            {
                long value = Base;
                for (int i = 0; i < _percent.Length; i++)
                    if (_percent[i] != 0) value = value * Math.Max(0, 100 + _percent[i]) / 100;
                return value;
            }
        }

        /// Whole points it is worth now, with no leftover: what a cash or a Guard event shows.
        public int Points => (int)Math.Min(int.MaxValue, Value / Unit);

        /// A hit rounds once: its value and the fighter's leftover in whole points, the rest left over for the next. One worth
        /// anything lands for at least 1, as armour always promised, and that point takes the leftover with it.
        public int Resolve(ref long leftover)
        {
            long total = Value + leftover;
            long whole = total / Unit;
            if (whole < 1)
            {
                leftover = 0;
                return Base > 0 ? 1 : 0;
            }
            leftover = total - whole * Unit;
            return (int)Math.Min(int.MaxValue, whole);
        }

        /// A heal rounds the same way, but one worth less than a point heals nothing yet: it waits in the leftover.
        public int ResolveHeal(ref long leftover)
        {
            long total = Value + leftover;
            long whole = total / Unit;
            leftover = total - whole * Unit;
            return (int)Math.Min(int.MaxValue, whole);
        }
    }
}
