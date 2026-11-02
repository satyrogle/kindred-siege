using System;

namespace KindredSiege.Sim.Battle
{
    /// The raid chief (Jacob, 25 Oct 2026: "lets imagine our whole run like a raid bosses from World of Warcraft", "only chief and
    /// his mechanics where some you have to parry, some you have to mitigate", then "show me in game those boss fights ... do 1
    /// pass"; FightRules.RaidChief, campaign rules version 10). On top of the squad first:
    /// - Your Hold is free, and a parry pays no points: it pays in his bar instead ("hold is universal mechanic tht doesnt require
    ///   points"). A Hold given less than MashTicks after your last one does not restart the parry window, the way mashing block
    ///   does in Sekiro.
    /// - A raid chief fights alone (the campaign sends no band and no reserves), with RaidHealthPercent of a chief's health, and
    ///   his nerve works like Sekiro's posture: only your answers crack it, never plain hits, and it refills all the time, faster
    ///   the more health he has. So parries alone cannot break a healthy chief; your squad has to bring his health down too.
    /// - Below DesperationPermille of his health his spell comes twice as often: his desperation phase.
    /// - His Sun Mantle and Burning Ring are casts with a bar that Focus Fire on him breaks; his Eclipse, his ultimate, cannot be
    ///   broken, only parried or dodged.
    /// Only Bloodridge's chief is a raid chief so far; every number is a starting value.
    public static class RaidRules
    {
        /// How much of his bar refills a second at full health; at half health half that, and so on.
        public const int PostureRegenPerSecond = 30;

        /// A raid chief's health, percent of a chief's: alone, he stands in for his whole band.
        public const int RaidHealthPercent = 400;

        /// A Hold given sooner than this after your last one keeps the last one's window: mashing never refreshes it.
        public const int MashTicks = SimClock.TicksPerSecond;

        /// Below this much of his health, per mille, he is desperate: his spell comes twice as often.
        public const int DesperationPermille = 300;

        /// The detail on his Sun Mantle's Land when Focus Fire broke it.
        public const string Broken = "broken";
    }

    public sealed partial class BattleSimulation
    {
        /// True when this fight plays the raid chief (FightRules.RaidChief).
        private readonly bool _raidOn;

        /// True when this fight plays the raid chief (FightRules.RaidChief): the screen shows a raid chief's cast bars and his bar
        /// as his posture.
        public bool RaidChiefOn => _raidOn;

        /// True when this side's chief is a raid chief: so far only Bloodridge's.
        private bool RaidChiefSide(int side)
        {
            return _raidOn && side >= 0 && side <= 1 && Kit(side) == ChiefKit.Bloodridge;
        }

        /// What an order costs this side: under the raid chief your Hold is free.
        private int CostOf(int side, Directive directive)
        {
            // From rules 22 your chief spell costs no points: the parry meter pays for it (BattleSimulation.Meter.cs).
            if (directive == Directive.Eclipse && MeterFor(side)) return 0;
            // From rules 23 the cheaper hook (BattleSimulation.Shade.cs).
            return Cheaper(side, _raidOn && side == 0 && directive == Directive.Hold ? 0 : Directives.Cost(directive));
        }

        /// What this order costs this side right now, in points, after every rule that changes it: the raid chief's free Hold,
        /// the meter's free chief spell and the cheaper hook (PalKit.OrderCostPercent). Read only, for the screen, so it never
        /// works the cost out itself.
        public int OrderCost(int side, Directive directive)
        {
            if (side < 0 || side >= _sides.Length) throw new ArgumentOutOfRangeException(nameof(side));
            return CostOf(side, directive);
        }

        /// Ticks until a new Hold press can refresh its parry timestamp. Hold may still be accepted as a guard while
        /// this is positive. Read only: the HUD must not advertise a fresh parry based on CanUse alone.
        public int HoldRefreshTicksLeft(int side)
        {
            if (side < 0 || side >= _sides.Length) throw new ArgumentOutOfRangeException(nameof(side));
            SideState state = _sides[side];
            if (!_raidOn || side != 0 || state.LastHoldPress < 0 || state.LastHoldTick < 0) return 0;
            long elapsed = _tick - state.LastHoldPress;
            return elapsed < RaidRules.MashTicks ? (int)(RaidRules.MashTicks - elapsed) : 0;
        }

        // A Hold is given. Under the raid chief one given sooner than MashTicks after your last keeps the last one's window.
        private void StampHold(int side, SideState state)
        {
            bool mashed = HoldRefreshTicksLeft(side) > 0;
            if (_raidOn && side == 0) state.LastHoldPress = _tick;
            if (!mashed) state.LastHoldTick = _tick;
        }

        // How long until his spell comes again: half as long while a raid chief is desperate.
        private int SpellGap(int side)
        {
            int gap = _rules[side].AutoEclipseTicks;
            if (!RaidChiefSide(side)) return gap;
            BattleUnit chief = KitChief(side);
            return chief != null && chief.HpPermille <= RaidRules.DesperationPermille ? Math.Max(1, gap / 2) : gap;
        }

        // A raid chief's bar refills like posture, by PostureRegenPerSecond at full health and less the more he is hurt. Whole
        // points only: the rest carries to the next tick.
        private void TickPosture()
        {
            for (int s = 0; s < 2 && !Stopped; s++)
            {
                if (!RaidChiefSide(s)) continue;
                BattleUnit chief = KitChief(s);
                SideState state = _sides[s];
                int full = _setup.Nerve.StartNerve(s);
                if (chief == null || !chief.Alive || chief.Yielded || state.Nerve <= 0 || state.Nerve >= full) continue;
                state.PostureCarry += RaidRules.PostureRegenPerSecond * chief.HpPermille;
                const int perPoint = SimClock.TicksPerSecond * 1000;
                while (state.PostureCarry >= perPoint && state.Nerve < full)
                {
                    state.PostureCarry -= perPoint;
                    state.Nerve++;
                }
                if (state.Nerve >= full) state.PostureCarry = 0;
            }
        }

        // His Sun Mantle's flare under the raid chief: a Focus Fire on him while it lit breaks it, like an interrupted cast.
        private bool MantleBroken(int side, BattleUnit chief)
        {
            int enemy = 1 - side;
            if (!RaidChiefSide(side) || !IsFocusing(enemy, chief)) return false;
            Emit(EventFor(BattleEventKind.Land, chief, null, 0, ChiefKits.MantleName + "." + RaidRules.Broken));
            Answered(enemy, ChiefAnswer.Break, chief, FightRules.InterruptPoints, FightRules.InterruptReason);
            return true;
        }
    }
}
