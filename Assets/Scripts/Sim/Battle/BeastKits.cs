using System;
using System.Collections.Generic;

namespace KindredSiege.Sim.Battle
{
    /// A beast (Jacob, 20 Oct 2026, DESIGN_LOCKS "Beasts": each region's road gets a beast boss node you choose to take). The
    /// Broken Wastes' sand shark and Skullpeak's cage bear are written.
    public enum BeastKit
    {
        None = 0,

        /// The sand shark: its jaw bites, its fin dives, its spine fires spines and its tail sweeps.
        SandShark = 1,

        /// The cage bear (Jacob, 22 Oct 2026: "cage bear bro looks and feels awesome", then "go, build the Cage Bear as
        /// planned"): Skullpeak's beast, a cave bear with a steel ribcage grown through its chest and one giant bionic claw
        /// arm. Its claw grabs, its maw roars, its ribcage shields it and its heart pulses.
        CageBear = 2
    }

    /// A beast's parts. Each beast has BeastKits.PartCount of them, and they stand on its side of a beast fight in its order
    /// (BeastKits.Parts): the sand shark's jaw, fin, spine and tail; the cage bear's claw, maw, ribcage and heart. Each is a
    /// target with its own health, and breaking it ends its move.
    public enum BeastPart
    {
        Jaw = 0,
        Fin = 1,
        Spine = 2,
        Tail = 3,
        Claw = 4,
        Maw = 5,
        Ribcage = 6,
        Heart = 7
    }

    /// The beasts' rules and numbers. Every number is a starting value that waits for Jacob's play.
    ///
    /// The sand shark (Jacob's six calls, 21 Oct 2026: "go, build the shark API with your six calls"). No nerve: nothing drains
    /// the shark's, so Unleash's scare and every other nerve hit do nothing to it, and you beat it by breaking its parts. Break
    /// all four and it dies. When the fight's clock runs out it dives away and the side facing it wins, keeping whatever it
    /// broke.
    ///
    /// Each part has one move on its own timer, with more than one answer:
    /// Jaw, Bite: a heavy bite on whoever stands across from the jaw. A Hold as it lands parries it; a Warden takes it on his
    /// shield (himself, or the one he covers) and the jaw takes part of it back, so he can break the jaw on his shield; any other
    /// Hold soaks it; or it lands in full.
    /// Fin, Dive: the shark sinks, and while it is under, blows on it mostly miss; then it bursts up under the back row. An armed
    /// snare springs on it and holds it on top, Fall Back dodges it, or a Warden covering the one it comes up under takes it.
    /// Spine, Volley: spines rain on the back row. Fall Back dodges, a covering Warden takes his ward's share halved.
    /// Tail, Sweep: the tail sweeps the front row. Fall Back dodges, a covering Warden takes his ward's share halved, and with
    /// the Medic standing and nobody falling to it she heals through it (as Jacob locked it; the 21 Oct build had a Hold soak).
    /// Its moves never fall on the leader while any other fighter stands, and on the leader once nobody else does.
    ///
    /// Phases (Jacob, 22 Oct 2026: "go, fix the audit findings and add both phases"): its second broken part brings its Blood
    /// frenzy (its moves come a third faster and every hit it lands leaves its victim bleeding); its third its Last stand (its
    /// last part moves twice as fast, and after LastStandTicks it digs away for good: break that part first and it dies).
    ///
    /// The cage bear (Jacob, 22 Oct 2026: "go, build the Cage Bear as planned"). No nerve either. Its ribcage has no move: while
    /// it stands, every hit on the bear lands at CagePercent and its heart cannot be hit at all, and while your Unleash runs your
    /// hits on the ribcage land CageUnleashPercent as hard as that. Break the heart and the bear drops dead on the spot, and
    /// only the parts you broke come home as trophies: crack the cage and go straight for the heart, or break the claw and the
    /// maw first for all four. It never runs: when the fight's clock runs out it holds its ground and you are driven off.
    /// Claw, Grab: it reaches for whoever stands across from it. Fall Back gets clear; a Warden covering them blocks it on his
    /// shield; or it grabs them and holds them out of the fight (they do nothing, nothing reaches them, nobody heals or covers
    /// them), crushing them every second, until you break the claw or they fall. It holds one at a time.
    /// Maw, Roar: a big hit to your nerve. A Hold blocks it, or Focus Fire on the maw while it roars cuts it off.
    /// Heart, Pulse: while the ribcage stands it mends the bear's other parts; once the cage is broken it hits your whole side
    /// instead. A Hold soaks it, and the Medic heals through it.
    /// Phases: its second broken part brings its Overdrive (its moves come a third faster and its grip crushes twice as hard);
    /// once only its heart is left, its Last stand: it pulses every LastStandPulseTicks until you break the heart or fall.
    ///
    /// Road scars: whoever breaks a part earns its scar for the run, and a scarred fighter's own spell works ScarPercent as well
    /// against any beast. Jawbreaker is Jacob's (DESIGN_LOCKS, point 7); the other seven names are Claude's placeholders.
    public static class BeastKits
    {
        /// Each beast's parts.
        public const int PartCount = 4;

        /// Every beast's parts, for saves: the BeastPart numbers run 0 to this less one.
        public const int AllPartCount = 8;

        /// The ids a beast's parts carry after their fight's id: "camp.5.bloodridge.beast.shark.jaw".
        public const string SharkId = "shark";
        public const string BearId = "bear";

        private static readonly BeastPart[] NoParts = new BeastPart[0];
        private static readonly BeastPart[] SharkParts = { BeastPart.Jaw, BeastPart.Fin, BeastPart.Spine, BeastPart.Tail };
        private static readonly BeastPart[] BearParts = { BeastPart.Claw, BeastPart.Maw, BeastPart.Ribcage, BeastPart.Heart };

        // ------------------------------------------------------------ phases

        /// Blood frenzy and Overdrive, from its second broken part: its moves come this percent as often apart. The shark's
        /// Blood frenzy also leaves every victim bleeding this percent a second for BleedRules.Ticks.
        public const int FrenzyEveryPercent = 67;
        public const int FrenzyBleedPercent = 3;

        /// The shark's Last stand, from its third broken part: its last part moves this percent as often apart, and it digs away
        /// after this.
        public const int LastStandEveryPercent = 50;
        public const int LastStandTicks = 15 * SimClock.TicksPerSecond;

        /// The Phase events' details.
        public const string FrenzyName = "Blood frenzy";
        public const string OverdriveName = "Overdrive";
        public const string LastStandName = "Last stand";

        /// The Flee event's detail: the shark dug away in its Last stand, or the fight's clock ran out.
        public const string EscapeDetail = "escape";
        public const string TimeDetail = "time";

        // ------------------------------------------------------------ health

        /// Each part's health in percent of a Warden's, at the fight's toughness and strength.
        public const int JawHealthPercent = 150;
        public const int FinHealthPercent = 120;
        public const int SpineHealthPercent = 120;
        public const int TailHealthPercent = 150;
        public const int ClawHealthPercent = 150;
        public const int MawHealthPercent = 130;
        public const int RibcageHealthPercent = 150;
        public const int HeartHealthPercent = 120;

        // ------------------------------------------------------------ timers

        public const int BiteEveryTicks = 6 * SimClock.TicksPerSecond;
        public const int BiteTellTicks = SimClock.TicksPerSecond;
        public const int DiveEveryTicks = 20 * SimClock.TicksPerSecond;

        /// How long the shark stays under the sand before it bursts up: the Dive's tell.
        public const int DiveTicks = 3 * SimClock.TicksPerSecond;
        public const int VolleyEveryTicks = 14 * SimClock.TicksPerSecond;
        public const int VolleyTellTicks = 3 * SimClock.TicksPerSecond / 2;
        public const int SweepEveryTicks = 10 * SimClock.TicksPerSecond;
        public const int SweepTellTicks = 3 * SimClock.TicksPerSecond / 2;

        public const int GrabEveryTicks = 9 * SimClock.TicksPerSecond;
        public const int GrabTellTicks = 3 * SimClock.TicksPerSecond / 2;
        public const int RoarEveryTicks = 15 * SimClock.TicksPerSecond;

        /// The Roar's wind up: Focus Fire on the maw in it cuts the roar off.
        public const int RoarTellTicks = 2 * SimClock.TicksPerSecond;
        public const int PulseEveryTicks = 12 * SimClock.TicksPerSecond;
        public const int PulseTellTicks = 3 * SimClock.TicksPerSecond / 2;

        /// In its Last stand the cage bear's heart pulses this often.
        public const int LastStandPulseTicks = 4 * SimClock.TicksPerSecond;

        // ------------------------------------------------------------ damage

        /// Percent of each one's max health a move hits for.
        public const int BitePercent = 18;
        public const int DivePercent = 25;
        public const int VolleyPercent = 10;
        public const int SweepPercent = 10;

        /// The claw closing on whoever it grabs, or on the shield of a Warden who blocks it.
        public const int GrabPercent = 10;

        /// Rules 25: the ice wave of the Slam and Grab on each of your fighters, before the claw.
        public const int SlamPercent = 10;

        /// Each second in the claw; in its Overdrive this percent of that.
        public const int CrushPercent = 4;
        public const int OverdriveCrushPercent = 200;

        /// The Pulse on each of your fighters once the cage is broken.
        public const int PulsePercent = 10;

        /// What the Pulse mends of each of the bear's other parts while the cage stands, in percent of its max health.
        public const int PulseMendPercent = 10;

        /// The nerve the Roar takes, in thousandths of a full bar (NerveRules.Full).
        public const int RoarNerve = 250;

        /// A Warden who takes the Bite on his shield sends this percent of it back into the jaw.
        public const int BiteBackPercent = 50;

        /// A Warden covering one of a Volley's or a Sweep's marks takes this percent of their share.
        public const int WardPercent = 50;

        /// While the shark is under the sand, blows on it land this many percent less often (never below
        /// FightRules.MinHitPercent).
        public const int DiveAvoid = 40;

        /// While the cage bear's ribcage stands every hit on it lands at this percent, and its heart takes none; while the
        /// striker's side Unleashes, hits on the ribcage itself land at this percent of that.
        public const int CagePercent = 50;
        public const int CageUnleashPercent = 200;

        /// A scarred fighter's own spell works this percent as well against a beast.
        public const int ScarPercent = 125;

        // ------------------------------------------------------------ rules 27, the beasts as raid bosses

        /// The shark's Feeding frenzy: at its second broken part it is under the sand this long, its moves stopped and its parts
        /// beyond reach; for the last ChurnTicks the sand churns under all of you; then it bursts up through all of you for
        /// BurstPercent of each one's health.
        public const int FeedingTicks = 4 * SimClock.TicksPerSecond;
        public const int ChurnTicks = 3 * SimClock.TicksPerSecond;
        public const int BurstPercent = 20;

        /// An armed trap snares the burst on top instead: its parts take this percent for this long.
        public const int ExposedPercent = 200;
        public const int ExposedTicks = 4 * SimClock.TicksPerSecond;

        /// Its sand pups: how many, each this percent of its parts' average health, biting whom they hold every PupBiteTicks
        /// for PupBitePercent of their health with a PupBleedPercent bleed.
        public const int PupCount = 2;
        public const int PupHealthPercent = 25;
        public const int PupBitePercent = 2;
        public const int PupBiteTicks = SimClock.TicksPerSecond;
        public const int PupBleedPercent = 2;

        /// The cage bear's Overload: when its ribcage breaks, frost cracks race across the floor this long; anyone of yours not
        /// falling back as they land is frozen this long.
        public const int FrostTicks = 4 * SimClock.TicksPerSecond;
        public const int FrozenTicks = 3 * SimClock.TicksPerSecond;

        /// In its Last stand each Pulse hits this many percent harder than the last.
        public const int PulseRampPercent = 20;

        /// Whether a beast's side of this many units fits it: its parts, and from rules 27 the shark's parts and its pups.
        public static bool SideFits(BeastKit kit, int count, bool raid)
        {
            return count == PartCount || (raid && kit == BeastKit.SandShark && count == PartCount + PupCount);
        }

        /// The n-th sand pup's id (1 and 2) in a fight: "camp.5.bloodridge.beast.shark.pup1".
        public static string PupId(string fightId, int n)
        {
            return fightId + "." + Id(BeastKit.SandShark) + ".pup" + n.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        // ------------------------------------------------------------ names and details

        public const string BiteName = "Bite";
        public const string DiveName = "Dive";
        public const string VolleyName = "Volley";
        public const string SweepName = "Sweep";
        public const string GrabName = "Grab";

        /// Rules 25: the ice wave of the cage bear's Slam and Grab, landed as its own move ("Slam.full" and the rest) just before
        /// the Grab's.
        public const string SlamName = "Slam";

        /// Rules 27: the shark's intermission (its Phase, Tell and Land events, "Feeding frenzy.full" and the rest), its sand pups
        /// (their Join events), the cage bear's intermission (its Phase event) and its frost (Tell and Land, "Frost cracks.full").
        public const string FeedingName = "Feeding frenzy";
        public const string PupName = "Sand pup";
        public const string OverloadName = "Overload";
        public const string FrostName = "Frost cracks";

        /// Rules 27: the warning lines while the burst and the frost tell.
        public const string ChurnWarning = "THE SAND CHURNS: FALL BACK";
        public const string FrostWarning = "FROST CRACKS: FALL BACK";
        public const string RoarName = "Roar";
        public const string PulseName = "Pulse";

        /// The claw's squeeze on whoever it holds, named only under the parry at the center (24 Oct 2026), where each squeeze is
        /// a beat: "Crush.parried" when a parry breaks the grip.
        public const string CrushName = "Crush";

        /// Hit details.
        public const string BiteDetail = "bite";
        public const string BiteBackDetail = "biteback";
        public const string DiveDetail = "dive";
        public const string VolleyDetail = "volley";
        public const string SweepDetail = "sweep";
        public const string GrabDetail = "grab";
        public const string SlamDetail = "slam";
        public const string BurstDetail = "burst";
        public const string PupDetail = "pup";
        public const string FrozenDetail = "frozen";
        public const string CrushDetail = "crush";
        public const string PulseDetail = "pulse";

        /// The Kill detail of the cage bear's parts that drop with its heart. None of them is broken (no Sever).
        public const string HeartDetail = "heart";

        /// The Reaction detail when the cage bear's claw breaks and lets go of whoever it held: actor the claw, target them.
        public const string ReleasedDetail = "released";

        /// How a move landed, after its name and a dot on a Land event: "Bite.parried", "Bite.guarded", "Bite.soaked",
        /// "Bite.full", "Dive.snared", "Dive.dodged", "Dive.guarded", "Dive.full", "Volley.dodged", "Volley.guarded",
        /// "Volley.full", "Sweep.dodged", "Sweep.guarded", "Sweep.healed", "Sweep.full", "Grab.dodged", "Grab.guarded",
        /// "Grab.full" (it holds them), "Roar.cut", "Roar.blocked", "Roar.full", "Pulse.mended" (the cage stands),
        /// "Pulse.soaked", "Pulse.healed" or "Pulse.full".
        public const string Parried = "parried";
        public const string Guarded = "guarded";
        public const string Soaked = "soaked";
        public const string Dodged = "dodged";
        public const string Snared = "snared";
        public const string Healed = "healed";
        public const string Blocked = "blocked";
        public const string Cut = "cut";
        public const string Mended = "mended";
        public const string Full = "full";

        /// The beast's name, for a card.
        public static string Name(BeastKit kit)
        {
            switch (kit)
            {
                case BeastKit.SandShark: return "Sand shark";
                case BeastKit.CageBear: return "Cage bear";
                default: return string.Empty;
            }
        }

        /// The id its parts carry after their fight's id, "shark" or "bear".
        public static string Id(BeastKit kit)
        {
            switch (kit)
            {
                case BeastKit.SandShark: return SharkId;
                case BeastKit.CageBear: return BearId;
                default: return string.Empty;
            }
        }

        /// A beast's parts in the order they stand on its side.
        public static IReadOnlyList<BeastPart> Parts(BeastKit kit)
        {
            switch (kit)
            {
                case BeastKit.SandShark: return SharkParts;
                case BeastKit.CageBear: return BearParts;
                default: return NoParts;
            }
        }

        /// The beast a part belongs to.
        public static BeastKit KitOf(BeastPart part)
        {
            return (int)part >= (int)BeastPart.Claw ? BeastKit.CageBear : BeastKit.SandShark;
        }

        /// Where a part stands among its beast's parts, 0 to PartCount less one.
        public static int Slot(BeastPart part)
        {
            return (int)part % PartCount;
        }

        /// The part standing in this place among a beast's parts.
        public static BeastPart PartAt(BeastKit kit, int slot)
        {
            return Parts(kit)[slot];
        }

        /// True when breaking these parts killed the beast: all four of the shark's, or the cage bear's heart.
        public static bool Kills(BeastKit kit, IReadOnlyList<BeastPart> broken)
        {
            if (kit == BeastKit.CageBear)
            {
                for (int i = 0; i < broken.Count; i++) if (broken[i] == BeastPart.Heart) return true;
                return false;
            }
            return broken.Count == PartCount;
        }

        /// The Phase event's detail when the beast enters this phase.
        public static string PhaseName(BeastKit kit, int phase)
        {
            if (phase >= 3) return LastStandName;
            return kit == BeastKit.CageBear ? OverdriveName : FrenzyName;
        }

        /// A part's name: its unit id ends with a dot and this, and it is the detail of its Sever event.
        public static string Name(BeastPart part)
        {
            switch (part)
            {
                case BeastPart.Jaw: return "jaw";
                case BeastPart.Fin: return "fin";
                case BeastPart.Spine: return "spine";
                case BeastPart.Tail: return "tail";
                case BeastPart.Claw: return "claw";
                case BeastPart.Maw: return "maw";
                case BeastPart.Ribcage: return "ribcage";
                default: return "heart";
            }
        }

        public static bool TryParse(string name, out BeastPart part)
        {
            for (int p = 0; p < AllPartCount; p++)
            {
                if (!string.Equals(Name((BeastPart)p), name, StringComparison.Ordinal)) continue;
                part = (BeastPart)p;
                return true;
            }
            part = BeastPart.Jaw;
            return false;
        }

        /// The part's move, as on its Tell event, or empty for the ribcage, which has none.
        public static string MoveName(BeastPart part)
        {
            switch (part)
            {
                case BeastPart.Jaw: return BiteName;
                case BeastPart.Fin: return DiveName;
                case BeastPart.Spine: return VolleyName;
                case BeastPart.Tail: return SweepName;
                case BeastPart.Claw: return GrabName;
                case BeastPart.Maw: return RoarName;
                case BeastPart.Heart: return PulseName;
                default: return string.Empty;
            }
        }

        /// The row a part stands in. The shark's jaw and fin in front, its spine and tail behind; the cage bear's claw, maw and
        /// ribcage in front and its heart behind them. Only ranged fighters and Skirmishers reach the back row until the front
        /// row is broken.
        public static Row RowOf(BeastPart part)
        {
            switch (part)
            {
                case BeastPart.Jaw:
                case BeastPart.Fin:
                case BeastPart.Claw:
                case BeastPart.Maw:
                case BeastPart.Ribcage:
                    return Row.Front;
                default:
                    return Row.Back;
            }
        }

        public static int HealthPercent(BeastPart part)
        {
            switch (part)
            {
                case BeastPart.Jaw: return JawHealthPercent;
                case BeastPart.Fin: return FinHealthPercent;
                case BeastPart.Spine: return SpineHealthPercent;
                case BeastPart.Tail: return TailHealthPercent;
                case BeastPart.Claw: return ClawHealthPercent;
                case BeastPart.Maw: return MawHealthPercent;
                case BeastPart.Ribcage: return RibcageHealthPercent;
                default: return HeartHealthPercent;
            }
        }

        /// Ticks from one of a part's moves to its next, or 0 for the ribcage, which has none.
        public static int EveryTicks(BeastPart part)
        {
            switch (part)
            {
                case BeastPart.Jaw: return BiteEveryTicks;
                case BeastPart.Fin: return DiveEveryTicks;
                case BeastPart.Spine: return VolleyEveryTicks;
                case BeastPart.Tail: return SweepEveryTicks;
                case BeastPart.Claw: return GrabEveryTicks;
                case BeastPart.Maw: return RoarEveryTicks;
                case BeastPart.Heart: return PulseEveryTicks;
                default: return 0;
            }
        }

        /// Ticks a part's move tells before it lands (for the Fin, how long the shark stays under).
        public static int TellTicks(BeastPart part)
        {
            switch (part)
            {
                case BeastPart.Jaw: return BiteTellTicks;
                case BeastPart.Fin: return DiveTicks;
                case BeastPart.Spine: return VolleyTellTicks;
                case BeastPart.Tail: return SweepTellTicks;
                case BeastPart.Claw: return GrabTellTicks;
                case BeastPart.Maw: return RoarTellTicks;
                case BeastPart.Heart: return PulseTellTicks;
                default: return 0;
            }
        }

        /// The road scar for breaking this part. Jawbreaker is Jacob's; the rest are Claude's placeholders.
        public static string ScarName(BeastPart part)
        {
            switch (part)
            {
                case BeastPart.Jaw: return "Jawbreaker";
                case BeastPart.Fin: return "Finripper";
                case BeastPart.Spine: return "Spinesnapper";
                case BeastPart.Tail: return "Tailcutter";
                case BeastPart.Claw: return "Clawcutter";
                case BeastPart.Maw: return "Roarbreaker";
                case BeastPart.Ribcage: return "Cagecracker";
                default: return "Heartstopper";
            }
        }

        /// What a scar does for a fighter of this class, for their card.
        public static string ScarText(UnitClass unitClass)
        {
            switch (unitClass)
            {
                case UnitClass.Warden: return "Their Guard holds better against beasts.";
                case UnitClass.Marksman: return "Their Aimed Shot hits beasts harder.";
                case UnitClass.Skirmisher: return "Their strike hits beasts harder.";
                case UnitClass.Trapper: return "Their snare holds beasts longer.";
                default: return "Their Treatment heals more against beasts.";
            }
        }
    }
}
