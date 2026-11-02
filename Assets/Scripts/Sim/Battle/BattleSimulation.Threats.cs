using System;
using System.Collections.Generic;

namespace KindredSiege.Sim.Battle
{
    /// What meets a threat (Threat.Answers). The orders are always listed when the fight's rules let them meet it; the
    /// squad's answers only while a fighter of that class of yours still stands, since they act on their own.
    [Flags]
    public enum ThreatAnswer
    {
        None = 0,

        /// A Hold up when it lands meets it: soaks, braces, blocks or stands fast against it.
        Hold = 1 << 0,

        /// A Hold started in its last FightRules.PerfectHoldTicks parries it.
        Parry = 1 << 1,

        /// Fall Back gets clear of it, or shakes it off.
        FallBack = 1 << 2,

        /// Focus Fire on Threat.FocusId breaks or stops it.
        FocusFire = 1 << 3,

        /// Unleash breaks or stops it.
        Unleash = 1 << 4,

        /// A Warden of yours meets it by covering its mark (or, for the Tidal Bore, by standing in its path).
        Warden = 1 << 5,

        /// A Medic of yours meets it with her treatment.
        Medic = 1 << 6,

        /// A Trapper of yours meets it with her snare or her trap.
        Trapper = 1 << 7,

        /// A Marksman of yours meets it with a shot while you Focus Fire Threat.FocusId.
        Marksman = 1 << 8
    }

    /// How big a threat is on the screen, after the fight feel's three effect tiers: a fighter's heavy swing; a chief's or a
    /// beast's move, or a step of the hunt; a chief spell, cast through the 5th button's slot.
    public enum ThreatTier
    {
        Basic = 0,
        Move = 1,
        Spell = 2
    }

    /// One thing coming at a side that an order or its squad can still meet (BattleSimulation.Threats, Jacob, 22 Oct 2026,
    /// later: "go, build the biggest threat read in the sim for the screen and hand it to Codex"). Read only: the sim works it
    /// out from the rules it plays by, so the screen never ranks or answers anything itself.
    public readonly struct Threat
    {
        /// The name of the cage bear's claw squeezing whoever it holds, which has no Tell of its own.
        public const string Crush = "Crush";

        public static readonly Threat None = new Threat(string.Empty, ThreatTier.Basic, string.Empty, string.Empty, 0, 0,
            ThreatAnswer.None, string.Empty);

        public Threat(string name, ThreatTier tier, string actorId, string targetId, int ticksLeft, int amount, ThreatAnswer answers,
            string focusId, int beatTicks = -1, bool fakeFirst = false, bool waitsOutHold = false, bool duel = false,
            bool castBar = false, int windowTicks = FightRules.PerfectHoldTicks)
        {
            CastBar = castBar;
            WindowTicks = windowTicks;
            Name = name ?? string.Empty;
            Tier = tier;
            ActorId = actorId ?? string.Empty;
            TargetId = targetId ?? string.Empty;
            TicksLeft = Math.Max(0, ticksLeft);
            Amount = Math.Max(0, amount);
            Answers = answers;
            FocusId = focusId ?? string.Empty;
            BeatTicks = beatTicks < 0 ? -1 : beatTicks;
            FakeFirst = BeatTicks >= 0 && fakeFirst;
            WaitsOutHold = BeatTicks >= 0 && waitsOutHold;
            Duel = BeatTicks >= 0 && duel;
        }

        /// What is coming, by the name on its Tell, Windup or Land: "Eclipse", "Heavy", "Cleave", "Track", "Bite" and so on;
        /// Crush for the cage bear's squeeze.
        public string Name { get; }

        public ThreatTier Tier { get; }

        /// Who does it: the caster, the swinger, the chief, his acolyte or engineer, the shooter, a beast's part.
        public string ActorId { get; }

        /// The one of yours it falls on, or empty when it falls on a row, a lane or your whole side, or picks its mark as it
        /// lands (the shark's Dive).
        public string TargetId { get; }

        /// Ticks until it lands. For one already running (a march, leeches, a channel, open veins, a bore, a horn's surge), until
        /// it runs out; for the claw's squeeze, which never runs out, until its next squeeze.
        public int TicksLeft { get; }

        /// The health it would take from your side if nothing met it: before armour, misses and answers, at the water and the
        /// rite as they stand now. 0 for one that takes no health by itself: a wall, a buff for his side, a roar on your nerve,
        /// the water rising, a drag, a trap that only bites if you do the wrong thing.
        public int Amount { get; }

        public ThreatAnswer Answers { get; }

        /// Whom to Focus Fire when FocusFire is among its answers, else empty.
        public string FocusId { get; }

        /// Under the parry at the center (FightRules.ParryCenter, 24 Oct 2026): ticks until its next beat, counted as TicksLeft
        /// is, so the beat lands on the tick this many ticks on, less one. -1 when it has none: outside the parry at the center,
        /// or a move already running past its beat (leeches drinking, a channel, the flurry, the high water).
        public int BeatTicks { get; }

        /// Under the parry at the center: true when its next beat is a fake (he feints on this move, BattleSimulation.FeintsOn),
        /// so the real one comes RealBeatTicks on. A Hold for the fake is stale by the real one, and he stands open between.
        public bool FakeFirst { get; }

        /// Ticks until its real beat, counted as BeatTicks is: BeatTicks, plus ParryRules.FeintTicks when FakeFirst; -1 with no
        /// beat.
        public int RealBeatTicks => BeatTicks < 0 ? -1 : FakeFirst ? BeatTicks + ParryRules.FeintTicks : BeatTicks;

        /// Under the parry at the center: true when its next beat waits out a Hold that is fresh when it comes (he learned to
        /// wait out your parry on this move, or his band has Delay, BattleSimulation.WaitsOutOn). It holds until that Hold goes
        /// stale, at least a second under the squad first, and a second Hold timed to the late beat still parries it.
        public bool WaitsOutHold { get; }

        /// Under the parry at the center: true when its beat is a duel for nerve. False for a band's heavy swing under the squad
        /// first (FightRules.SquadFirst), a small beat: a parry still staggers and opens the swinger, but cracks no nerve.
        public bool Duel { get; }

        /// True only while a Hold given now would really parry its next beat: inside the parry window, and neither a fake nor a
        /// beat that waits out a fresh Hold. The Hold button's beat ring is full. From rules 26 (the tight parry) this is the
        /// 0.2 s window, and ActorId is the one whose body flashes gold.
        public bool ParryWindow => BeatTicks >= 1 && BeatTicks <= WindowTicks + 1 && !FakeFirst && !WaitsOutHold;

        /// The parry window this threat was read under, in ticks: FightRules.PerfectHoldTicks, or TightRules.WindowTicks from
        /// rules 26.
        public int WindowTicks { get; }

        internal Threat WithBeat(bool fakeFirst, bool waitsOutHold, bool duel, int windowTicks)
        {
            return new Threat(Name, Tier, ActorId, TargetId, TicksLeft, Amount, Answers, FocusId, BeatTicks, fakeFirst, waitsOutHold, duel,
                CastBar, windowTicks);
        }

        /// Under the raid chief (FightRules.RaidChief, 25 Oct 2026): true for a cast the screen shows as a cast bar, which Focus
        /// Fire on its caster breaks (Jacob: "interruptible gas are shown as a cash bar"). A move to parry has no bar: you read
        /// his body, and his ultimate gets the cut in and the flash on its beat.
        public bool CastBar { get; }

        public bool IsNone => Name.Length == 0;
    }

    /// The biggest threat read (Jacob, 22 Oct 2026, later), for Gate 1 of Codex's plan: everything coming at a side, what it
    /// is, whom it hits, how long it has, how hard it is and what meets it, with the one to answer first on top, so the screen
    /// can show that one big and the rest small without keeping its own copy of a combat rule. It reads the fight and changes
    /// nothing: no random number is drawn, no state moves, and a fight that never asks plays exactly as one that asks every
    /// frame.
    ///
    /// Claude's calls while building, Jacob's to change: the one to answer first is the next hit on your side, the soonest to
    /// land and, on a tie, the heavier; everything that takes no health by itself (a wall, a buff, a roar, the water rising, a
    /// drag, a trap) comes after every hit, soonest first; a move already running counts until it runs out, since an answer
    /// any time before then still saves the rest; a forecast move (Bloodridge's tactical piece) is not here until its tell
    /// starts, since the forecast already shows it; nothing is here while a chief kneels; Amount is the health it would take,
    /// so a Brand counts everyone beside its mark, a row move the whole row, a hunt step the Kill at its end, and the shark's
    /// Dive the worst of where it can come up.
    public sealed partial class BattleSimulation
    {
        /// Everything coming at this side now that an order or its squad can still meet, the one to answer first at the top
        /// (see the calls above). Empty when nothing is coming, while a chief kneels, and once the fight is over.
        public IReadOnlyList<Threat> Threats(int side)
        {
            var list = new List<Threat>();
            if (side < 0 || side > 1 || Stopped) return list;
            int foe = 1 - side;
            ThreatsOfSpell(list, side, foe);
            if (_heavyOn) ThreatsOfHeavy(list, side, foe);
            if (_kitsOn && KitOn(foe)) ThreatsOfKit(list, side, foe);
            if (_roadKitOn) ThreatsOfCarriers(list, side, foe);
            if (_beastOn && foe == _beastSide) ThreatsOfBeast(list, side);
            if (_parryOn) ReadBeats(list, foe);
            Rank(list);
            return list;
        }

        // Under the parry at the center: whether each threat's next beat is a fake, whether it waits out a fresh Hold, and
        // whether it is a duel (BattleSimulation.Parry.cs), from what its beat has done so far. A double swing's heavy second
        // swing neither feints nor waits; under the squad first a beat that feinted never waits.
        private void ReadBeats(List<Threat> list, int foe)
        {
            for (int i = 0; i < list.Count; i++)
            {
                Threat t = list[i];
                if (t.BeatTicks < 0) continue;
                int flags = BeatFlagsOf(foe, t);
                bool second = (flags & BeatSecond) != 0;
                bool fake = !second && (flags & BeatFeinted) == 0 && FeintsOn(foe, t.Name);
                bool waits = !second && !fake && (flags & BeatWaited) == 0 && !(_squadOn && (flags & BeatFeinted) != 0) &&
                             WaitsOutOn(foe, t.Name);
                list[i] = t.WithBeat(fake, waits, BigHeavies || t.Name != HeavyRules.Heavy, ParryWindowTicks);
            }
        }

        // What a threat's beat has done so far: a Brand's on its mark, a hunt step's on its hunt, a beast part's on the part,
        // and every other move's in its slot (SideState.Beats).
        private int BeatFlagsOf(int foe, Threat t)
        {
            if (t.Name == ChiefKits.BrandName)
            {
                BattleUnit mark = FindById(t.TargetId);
                return mark == null ? 0 : mark.BeatFlags;
            }
            if (_huntOn && foe == _huntSide)
            {
                for (int k = 0; k < HuntCount; k++)
                    if (_huntStep[k] != (int)HuntStep.None && ChiefKits.Name((HuntStep)_huntStep[k]) == t.Name &&
                        string.Equals(_huntPrey[k], t.TargetId, StringComparison.Ordinal))
                        return _huntBeat[k];
                return 0;
            }
            if (_beastOn && foe == _beastSide)
            {
                BattleUnit part = FindById(t.ActorId);
                return part == null ? 0 : part.BeatFlags;
            }
            for (int k = 0; k < BeatSlots; k++)
                if (string.Equals(SlotMove(foe, (BeatSlot)k), t.Name, StringComparison.Ordinal)) return _sides[foe].Beats[k];
            return 0;
        }

        /// The one threat to answer first, or Threat.None.
        public Threat TopThreat(int side)
        {
            IReadOnlyList<Threat> list = Threats(side);
            return list.Count > 0 ? list[0] : Threat.None;
        }

        /// The fighters of this side who can answer a threat on their own, for the glow beside the Hold button's beat ring
        /// (Jacob, 24 Oct 2026, DESIGN_LOCKS "The parry at the center", part 4: "the fighter who can answer it glows"): every
        /// standing Warden, Medic, Trapper or Marksman its answers name, in unit order. Empty when only orders answer it.
        public IReadOnlyList<string> ThreatAnswerers(int side, Threat threat)
        {
            var ids = new List<string>();
            if (side < 0 || side > 1 || threat.IsNone) return ids;
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (!u.Alive || u.Side != side) continue;
                bool answers = (u.Class == UnitClass.Warden && (threat.Answers & ThreatAnswer.Warden) != 0) ||
                               (u.Class == UnitClass.Medic && (threat.Answers & ThreatAnswer.Medic) != 0) ||
                               (u.Class == UnitClass.Trapper && (threat.Answers & ThreatAnswer.Trapper) != 0) ||
                               (u.Class == UnitClass.Marksman && (threat.Answers & ThreatAnswer.Marksman) != 0);
                if (answers) ids.Add(u.Id);
            }
            return ids;
        }

        // The next hit first (soonest, then the heavier), then everything that takes no health by itself, soonest first. Ties
        // keep the order they were found in.
        private static void Rank(List<Threat> list)
        {
            for (int i = 1; i < list.Count; i++)
            {
                Threat t = list[i];
                int j = i;
                while (j > 0 && Before(t, list[j - 1]))
                {
                    list[j] = list[j - 1];
                    j--;
                }
                list[j] = t;
            }
        }

        private static bool Before(Threat a, Threat b)
        {
            bool hurtsA = a.Amount > 0, hurtsB = b.Amount > 0;
            if (hurtsA != hurtsB) return hurtsA;
            if (a.TicksLeft != b.TicksLeft) return a.TicksLeft < b.TicksLeft;
            return a.Amount > b.Amount;
        }

        // ------------------------------------------------------------ what this side's squad can do

        // The squad's answers that this side can give now: only those whose class still stands.
        private ThreatAnswer Squad(int side, ThreatAnswer wanted)
        {
            ThreatAnswer have = ThreatAnswer.None;
            if ((wanted & ThreatAnswer.Warden) != 0 && FirstLivingWarden(side) != null) have |= ThreatAnswer.Warden;
            if ((wanted & ThreatAnswer.Medic) != 0 && MedicStands(side)) have |= ThreatAnswer.Medic;
            if ((wanted & ThreatAnswer.Trapper) != 0 && FirstLiving(side, UnitClass.Trapper) != null) have |= ThreatAnswer.Trapper;
            if ((wanted & ThreatAnswer.Marksman) != 0 && FirstLiving(side, UnitClass.Marksman) != null) have |= ThreatAnswer.Marksman;
            return (wanted & ~(ThreatAnswer.Warden | ThreatAnswer.Medic | ThreatAnswer.Trapper | ThreatAnswer.Marksman)) | have;
        }

        // A Hold soaks what falls on this one only when they stand in the front row.
        private static ThreatAnswer HoldOn(BattleUnit target)
        {
            return target != null && target.Row == Row.Front ? ThreatAnswer.Hold : ThreatAnswer.None;
        }

        private ThreatAnswer ParryOf(int foe)
        {
            return _fight.PerfectHold && !Unparryable(foe) ? ThreatAnswer.Parry : ThreatAnswer.None;
        }

        // Under the parry at the center every beat can be parried; outside it, no beat ticks are given.
        private ThreatAnswer AtBeat => _parryOn && _fight.PerfectHold ? ThreatAnswer.Parry : ThreatAnswer.None;

        private int BeatIn(int ticks)
        {
            return _parryOn ? ticks : -1;
        }

        // Ticks until the next second boundary of a move that acts once a second, counted from its own clock.
        private static int NextSecond(int total, int left)
        {
            int done = total - left;
            return SimClock.TicksPerSecond - done % SimClock.TicksPerSecond;
        }

        private static int Percent(BattleUnit u, int percent)
        {
            return Math.Max(1, u.MaxHp * percent / 100);
        }

        // Whom a row move lands on: this row of the side, the other once this one has fallen.
        private Row RowHit(int side, Row first)
        {
            return AnyAlive(side, first) ? first : first == Row.Front ? Row.Back : Row.Front;
        }

        private int RowPercent(int side, Row row, int percent, bool bothRows)
        {
            int total = 0;
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (u.Alive && u.Side == side && (bothRows || u.Row == row)) total += Percent(u, percent);
            }
            return total;
        }

        // The first standing fighter of a side's band, of its front row only if asked, or null.
        private BattleUnit FirstOfBand(int side, bool frontOnly)
        {
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (u.Alive && u.Side == side && u.Role == UnitRole.Fighter && (!frontOnly || u.Row == Row.Front)) return u;
            }
            return null;
        }

        private static string IdOf(BattleUnit u)
        {
            return u == null ? string.Empty : u.Id;
        }

        // ------------------------------------------------------------ spells and heavy swings

        // The other side's spell winding up in the 5th button's slot.
        private void ThreatsOfSpell(List<Threat> list, int side, int foe)
        {
            SideState his = _sides[foe];
            if (his.Windup <= 0) return;
            BattleUnit caster = FindById(his.CasterId);
            if (caster == null || !caster.Alive) return;
            ChiefSpell spell = _rules[foe].Spell;
            string name = his.WindupPyre ? Moves.Pyre : spell == ChiefSpell.Eclipse ? Directives.Name(Directive.Eclipse) : ChiefSpells.Name(spell);
            int percent = his.WindupPercent;
            int amount = 0;
            ThreatAnswer answers;
            string focus = string.Empty;
            switch (spell)
            {
                case ChiefSpell.SteelStorm:
                    // Your Trapper's snare on one of her Wardens while they lock leaves the Skullpeak chief's wall half built.
                    answers = _kitsTwoOn && Kit(foe) == ChiefKit.Skullpeak ? Squad(side, ThreatAnswer.Trapper) : ThreatAnswer.None;
                    answers |= AtBeat;
                    break;
                case ChiefSpell.WarCry:
                    answers = ThreatAnswer.FocusFire | AtBeat;
                    focus = caster.Id;
                    break;
                case ChiefSpell.BloodCall:
                    for (int i = 0; i < _units.Count; i++)
                    {
                        BattleUnit u = _units[i];
                        if (u.Alive && u.Side == side && u.BleedTicks > 0) amount += Percent(u, percent);
                    }
                    answers = ThreatAnswer.FallBack;
                    if (_riteOn && Kit(foe) == ChiefKit.Stoneveil) answers |= Squad(side, ThreatAnswer.Medic);
                    if (_parryOn) answers |= AtBeat | ThreatAnswer.Hold;
                    break;
                case ChiefSpell.Firebomb:
                    amount = RowPercent(side, RowHit(side, Row.Front), percent, false);
                    answers = ThreatAnswer.FallBack;
                    if (_parryOn) answers |= AtBeat | ThreatAnswer.Hold;
                    break;
                case ChiefSpell.DustShark:
                    amount = RowPercent(side, RowHit(side, Row.Back), percent, false);
                    answers = ThreatAnswer.FallBack;
                    if (_parryOn) answers |= AtBeat | ThreatAnswer.Hold;
                    break;
                case ChiefSpell.Tide:
                    bool tideKit = _tideOn && foe == _tideSide;
                    if (tideKit) percent = percent * TideScale() / 100;
                    amount = RowPercent(side, RowHit(side, Row.Back), percent, tideKit && SpringTide(foe));
                    answers = ThreatAnswer.FallBack;
                    if (_parryOn) answers |= AtBeat | ThreatAnswer.Hold;
                    break;
                default:
                    amount = RowPercent(side, RowHit(side, Row.Front), percent, false);
                    answers = ThreatAnswer.Hold | ThreatAnswer.FallBack | ThreatAnswer.FocusFire | AtBeat;
                    // A raid chief's ultimate cannot be broken, only parried or dodged (BattleSimulation.Raid.cs).
                    if (RaidChiefSide(foe)) answers &= ~ThreatAnswer.FocusFire;
                    if (spell == ChiefSpell.Eclipse) answers |= ParryOf(foe);
                    // From rules 21 the Eclipse is a blast: a Hold that is not a parry soaks nothing (BattleSimulation.Danger.cs).
                    if (_dangerOn && spell == ChiefSpell.Eclipse) answers &= ~ThreatAnswer.Hold;
                    focus = caster.Id;
                    break;
            }
            list.Add(new Threat(name, ThreatTier.Spell, caster.Id, his.WindupPyre ? his.PyreTargetId : string.Empty, his.Windup, amount,
                answers, focus, BeatIn(his.Windup)));
        }

        // The other side's heavy swing winding up: a parry, Fall Back unless the band reaches through it, a Hold when its mark
        // stands in the front row, or a Warden covering them.
        private void ThreatsOfHeavy(List<Threat> list, int side, int foe)
        {
            SideState his = _sides[foe];
            if (his.HeavyWindup <= 0) return;
            BattleUnit swinger = FindById(his.HeavySwingerId);
            if (swinger == null || !swinger.Alive) return;
            BattleUnit target = FindById(his.HeavyTargetId);
            int power = swinger.Class == UnitClass.Medic
                ? Math.Max(1, SliceClasses.MedicFallbackAttack * swinger.PowerPercent / 100)
                : swinger.Power;
            ThreatAnswer answers = ParryOf(foe) | HoldOn(target) | Squad(side, ThreatAnswer.Warden);
            if (!HasBand(foe, BandTrait.LongReach)) answers |= ThreatAnswer.FallBack;
            list.Add(new Threat(HeavyRules.Heavy, ThreatTier.Basic, swinger.Id, IdOf(target), his.HeavyWindup,
                Math.Max(1, power * HeavyPercent(swinger) / 100), answers, string.Empty, BeatIn(his.HeavyWindup)));
        }

        // ------------------------------------------------------------ the chiefs' kits

        private void ThreatsOfKit(List<Threat> list, int side, int foe)
        {
            if (Kit(foe) == ChiefKit.Hunt)
            {
                ThreatsOfHunt(list, side, foe);
                return;
            }
            BattleUnit chief = KitChief(foe);
            if (chief == null) return;
            SideState his = _sides[foe];

            // Bloodridge's Cleave and Brands (21 Oct 2026).
            if (his.CleaveWindup > 0)
            {
                BattleUnit target = FindById(his.CleaveTargetId);
                list.Add(new Threat(ChiefKits.CleaveName, ThreatTier.Move, chief.Id, IdOf(target), his.CleaveWindup,
                    Math.Max(1, chief.Power * HeavyRules.DamagePercent / 100), ParryOf(foe) | HoldOn(target) | Squad(side, ThreatAnswer.Warden),
                    string.Empty, BeatIn(his.CleaveWindup)));
            }
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (!u.Alive || u.Side != side || u.BrandTicks <= 0) continue;
                List<BattleUnit> hit = BrandedAndBeside(u);
                int amount = 0;
                for (int k = 0; k < hit.Count; k++) amount += Percent(hit[k], ChiefBrandPercent);
                list.Add(new Threat(ChiefKits.BrandName, ThreatTier.Move, chief.Id, u.Id, u.BrandTicks, amount,
                    ThreatAnswer.FallBack | AtBeat | Squad(side, ThreatAnswer.Warden | ThreatAnswer.Medic), string.Empty, BeatIn(u.BrandTicks)));
            }
            if (!_kitsTwoOn) return;

            // Bloodridge's Burning Ring and Sun Mantle (22 Oct 2026).
            if (his.RingSpin > 0)
            {
                BattleUnit mark = FindById(his.RingTargetId);
                int lane = mark != null ? LanePosition(mark) : LanePosition(chief);
                int amount = 0;
                BattleUnit front = NearestInLane(side, Row.Front, lane), back = NearestInLane(side, Row.Back, lane);
                if (front != null) amount += Percent(front, ChiefRingPercent);
                if (back != null) amount += Percent(back, ChiefRingPercent);
                list.Add(new Threat(ChiefKits.RingName, ThreatTier.Move, chief.Id, IdOf(mark), his.RingSpin, amount,
                    ThreatAnswer.FallBack | ThreatAnswer.FocusFire | AtBeat | Squad(side, ThreatAnswer.Marksman), chief.Id, BeatIn(his.RingSpin),
                    castBar: RaidChiefSide(foe)));
            }
            // Under the parry at the center his Sun Mantle flares after its tell: the flare is its beat.
            if (his.MantleFlare > 0)
            {
                BattleUnit band = FirstOfBand(foe, false);
                // A raid chief stands alone: Focus Fire on him breaks his Mantle as it lights (BattleSimulation.Raid.cs).
                bool raid = RaidChiefSide(foe);
                list.Add(new Threat(ChiefKits.MantleName, ThreatTier.Move, chief.Id, string.Empty, his.MantleFlare, 0,
                    AtBeat | ThreatAnswer.FallBack | (band != null || raid ? ThreatAnswer.FocusFire : ThreatAnswer.None),
                    raid ? chief.Id : IdOf(band), BeatIn(his.MantleFlare), castBar: raid));
            }
            if (his.MantleTicks > 0)
            {
                BattleUnit band = FirstOfBand(foe, false);
                list.Add(new Threat(ChiefKits.MantleName, ThreatTier.Move, chief.Id, string.Empty, his.MantleTicks, 0,
                    ThreatAnswer.FallBack | (band != null ? ThreatAnswer.FocusFire : ThreatAnswer.None), IdOf(band)));
            }

            // Skullpeak's Hook, Rifle Line, Iron Advance and Overclock (her Steel Storm is her spell).
            if (his.HookTell > 0)
                list.Add(new Threat(ChiefKits.HookName, ThreatTier.Move, chief.Id, his.HookTargetId, his.HookTell, 0,
                    (_fight.PerfectHold ? ThreatAnswer.Parry : ThreatAnswer.None) | Squad(side, ThreatAnswer.Warden), string.Empty,
                    BeatIn(his.HookTell)));
            if (his.RifleTicks > 0)
            {
                BattleUnit target = FindById(his.RifleTargetId);
                if (target == null || !target.Alive) target = MostHurtMark(side);
                int shots = 0;
                BattleUnit shooter = null;
                for (int i = 0; i < _units.Count; i++)
                {
                    BattleUnit u = _units[i];
                    if (!u.Alive || u.Side != foe || u.RifleTicks <= 0) continue;
                    shots++;
                    if (shooter == null || (shooter.Role != UnitRole.Fighter && u.Role == UnitRole.Fighter)) shooter = u;
                }
                list.Add(new Threat(ChiefKits.RifleName, ThreatTier.Move, chief.Id, IdOf(target), his.RifleTicks,
                    target == null ? 0 : shots * Percent(target, ChiefKits.RifleShotPercent),
                    ThreatAnswer.FallBack | AtBeat | (shooter != null ? ThreatAnswer.FocusFire : ThreatAnswer.None) | Squad(side, ThreatAnswer.Medic),
                    IdOf(shooter), BeatIn(his.RifleTicks)));
            }
            if (his.AdvanceTicks > 0 || his.AdvanceHold > 0)
            {
                Row row = AnyMarkIn(side, Row.Front) ? Row.Front : Row.Back;
                List<BattleUnit> crushed = MarksIn(side, row);
                int amount = 0;
                for (int k = 0; k < crushed.Count; k++) amount += Percent(crushed[k], ChiefKits.AdvanceCrushPercent);
                // Every crush is a beat under the parry at the center: one parry breaks the march.
                int beat = his.AdvanceHold > 0 ? his.AdvanceHold : NextSecond(ChiefKits.AdvanceTicks, his.AdvanceTicks);
                list.Add(new Threat(ChiefKits.AdvanceName, ThreatTier.Move, chief.Id, string.Empty, Math.Max(his.AdvanceTicks, his.AdvanceHold),
                    amount * ((his.AdvanceTicks + SimClock.TicksPerSecond - 1) / SimClock.TicksPerSecond + (his.AdvanceHold > 0 ? 1 : 0)),
                    ThreatAnswer.Hold | ThreatAnswer.Unleash | ThreatAnswer.FallBack | AtBeat, string.Empty, BeatIn(beat)));
            }
            if (his.JabTicks > 0)
            {
                BattleUnit medic = FindById(his.JabMedicId);
                list.Add(new Threat(ChiefKits.OverclockName, ThreatTier.Move, IdOf(medic), string.Empty, his.JabTicks, 0,
                    ThreatAnswer.FocusFire | AtBeat | Squad(side, ThreatAnswer.Trapper), IdOf(medic), BeatIn(his.JabTicks)));
            }
            if (his.OverclockTicks > 0)
                list.Add(new Threat(ChiefKits.OverclockName, ThreatTier.Move, chief.Id, string.Empty, his.OverclockTicks, 0,
                    ThreatAnswer.Hold | Squad(side, ThreatAnswer.Trapper), string.Empty));

            if (_riteOn && Kit(foe) == ChiefKit.Stoneveil) ThreatsOfRite(list, side, foe, chief);
            if (_tideOn && foe == _tideSide) ThreatsOfTide(list, side, foe, chief);
        }

        // The Stoneveil chief's Basin, Leeches, Open Veins and Transfusion (his Blood Call is his spell).
        private void ThreatsOfRite(List<Threat> list, int side, int foe, BattleUnit chief)
        {
            SideState his = _sides[foe];
            if (his.BasinTicks > 0 || his.BasinHold > 0)
            {
                // Under the parry at the center his drink is its beat: due at the next second if your bleeding fills the Basin.
                int drink = his.BasinHold > 0 ? his.BasinHold
                    : his.BasinFill + BleedingCount(side) >= ChiefKits.BasinFull ? NextSecond(ChiefKits.BasinTicks, his.BasinTicks) : -1;
                list.Add(new Threat(ChiefKits.BasinName, ThreatTier.Move, his.BasinCarrierId, string.Empty, Math.Max(his.BasinTicks, his.BasinHold), 0,
                    ThreatAnswer.FocusFire | ThreatAnswer.Unleash | AtBeat, his.BasinCarrierId, drink < 0 ? -1 : BeatIn(drink)));
            }
            // Under the parry at the center his Leeches leap, his cut opens your veins and his Transfusion latches after their
            // tells: each of those moments is a beat.
            if (his.LeechLeap > 0)
            {
                BattleUnit target = FindById(his.LeechTargetId);
                int drinks = ChiefKits.LeechTicks / SimClock.TicksPerSecond;
                list.Add(new Threat(ChiefKits.LeechName, ThreatTier.Move, chief.Id, IdOf(target), his.LeechLeap,
                    target == null ? 0 : drinks * Percent(target, ChiefKits.LeechPercent),
                    ThreatAnswer.FallBack | AtBeat | ThreatAnswer.Hold, string.Empty, BeatIn(his.LeechLeap)));
            }
            if (his.VeinsCut > 0)
                list.Add(new Threat(ChiefKits.VeinsName, ThreatTier.Move, chief.Id, string.Empty, his.VeinsCut, 0,
                    AtBeat | ThreatAnswer.Hold, string.Empty, BeatIn(his.VeinsCut)));
            if (his.DrawLatch > 0)
            {
                BattleUnit target = FindById(his.DrawTargetId);
                int draws = ChiefKits.TransfusionTicks / SimClock.TicksPerSecond;
                list.Add(new Threat(ChiefKits.TransfusionName, ThreatTier.Move, chief.Id, IdOf(target), his.DrawLatch,
                    target == null || !target.Alive ? 0 : draws * Percent(target, ChiefKits.TransfusionPercent),
                    ThreatAnswer.FocusFire | AtBeat | ThreatAnswer.Hold | Squad(side, ThreatAnswer.Warden), chief.Id, BeatIn(his.DrawLatch)));
            }
            if (his.LeechTicks > 0)
            {
                BattleUnit target = FindById(his.LeechTargetId);
                int drinks = (his.LeechTicks + SimClock.TicksPerSecond - 1) / SimClock.TicksPerSecond;
                list.Add(new Threat(ChiefKits.LeechName, ThreatTier.Move, chief.Id, IdOf(target), his.LeechTicks,
                    target == null ? 0 : drinks * Percent(target, ChiefKits.LeechPercent),
                    ThreatAnswer.FallBack | Squad(side, ThreatAnswer.Medic), string.Empty));
            }
            if (his.VeinsTicks > 0)
            {
                // What it adds: as much again as your bleeding takes while it runs.
                int amount = 0;
                for (int i = 0; i < _units.Count; i++)
                {
                    BattleUnit u = _units[i];
                    if (!u.Alive || u.Side != side || u.BleedTicks <= 0) continue;
                    amount += Math.Min(u.BleedTicks, his.VeinsTicks) / SimClock.TicksPerSecond * Percent(u, u.BleedPercent);
                }
                list.Add(new Threat(ChiefKits.VeinsName, ThreatTier.Move, chief.Id, string.Empty, his.VeinsTicks, amount,
                    ThreatAnswer.Hold | Squad(side, ThreatAnswer.Medic), string.Empty));
            }
            if (his.DrawTicks > 0)
            {
                BattleUnit target = FindById(his.DrawTargetId);
                int draws = (his.DrawTicks + SimClock.TicksPerSecond - 1) / SimClock.TicksPerSecond;
                list.Add(new Threat(ChiefKits.TransfusionName, ThreatTier.Move, chief.Id, IdOf(target), his.DrawTicks,
                    target == null || !target.Alive ? 0 : draws * Percent(target, ChiefKits.TransfusionPercent),
                    ThreatAnswer.FocusFire | Squad(side, ThreatAnswer.Warden), chief.Id));
            }
        }

        // The Thundercoast chief's Sluice Gate, Riptide, Tidal Bore and Storm Surge (his Tide is his spell).
        private void ThreatsOfTide(List<Threat> list, int side, int foe, BattleUnit chief)
        {
            SideState his = _sides[foe];
            if (his.CrankTicks > 0)
                list.Add(new Threat(ChiefKits.SluiceName, ThreatTier.Move, his.EngineerId, string.Empty, his.CrankTicks, 0,
                    ThreatAnswer.FocusFire | ThreatAnswer.Unleash | AtBeat, his.EngineerId, BeatIn(his.CrankTicks)));
            if (his.RipTicks > 0)
            {
                // Under the parry at the center the yank, ParryRules.YankTicks in, is its beat.
                int yank = his.RipHold > 0 ? his.RipHold : his.RipTicks - (ChiefKits.RiptideTicks - ParryRules.YankTicks);
                list.Add(new Threat(ChiefKits.RiptideName, ThreatTier.Move, chief.Id, string.Empty, his.RipTicks, 0,
                    ThreatAnswer.Hold | (yank >= 1 ? AtBeat : ThreatAnswer.None), string.Empty, yank >= 1 ? BeatIn(yank) : -1));
            }
            if (his.BoreTicks > 0)
            {
                // Whoever it has still to reach, nearest first: the first is its next mark.
                List<BattleUnit> ahead = Reached(side, his.BoreFromRight, his.BoreFront, 1000);
                int amount = 0;
                bool warden = false;
                for (int k = 0; k < ahead.Count; k++)
                {
                    amount += Percent(ahead[k], ChiefKits.BorePercent);
                    if (ahead[k].Class == UnitClass.Warden) warden = true;
                }
                // Under the parry at the center the first lane it hits is its beat: when its front reaches the nearest of yours.
                int beat = -1;
                if (_parryOn && (his.Beats[(int)BeatSlot.Bore] & BeatSecond) == 0 && ahead.Count > 0)
                {
                    int d = his.BoreFromRight ? 1000 - LanePosition(ahead[0]) : LanePosition(ahead[0]);
                    int need = (d * ChiefKits.BoreTicks + 999) / 1000;
                    beat = his.BoreHold > 0 ? his.BoreHold : Math.Max(1, need - (ChiefKits.BoreTicks - his.BoreTicks));
                }
                list.Add(new Threat(ChiefKits.BoreName, ThreatTier.Move, chief.Id, ahead.Count > 0 ? ahead[0].Id : string.Empty,
                    his.BoreTicks, amount, ThreatAnswer.FallBack | ThreatAnswer.Hold | (warden ? ThreatAnswer.Warden : ThreatAnswer.None) |
                    (beat >= 1 ? AtBeat : ThreatAnswer.None), string.Empty, beat));
            }
            if (his.HornTicks > 0)
                list.Add(new Threat(ChiefKits.SurgeName, ThreatTier.Move, chief.Id, string.Empty, his.HornTicks, 0,
                    ThreatAnswer.FocusFire | AtBeat, chief.Id, BeatIn(his.HornTicks)));
            if (his.SurgeTicks > 0)
                list.Add(new Threat(ChiefKits.SurgeName, ThreatTier.Move, chief.Id, string.Empty, his.SurgeTicks, 0, ThreatAnswer.Hold,
                    string.Empty));
        }

        // The hunt's steps, each to be broken before it runs out; its Amount is the Kill at the end.
        private void ThreatsOfHunt(List<Threat> list, int side, int foe)
        {
            BattleUnit captain = KitChief(foe);
            if (captain == null) return;
            for (int k = 0; k < HuntCount; k++)
            {
                HuntStep step = (HuntStep)_huntStep[k];
                BattleUnit prey = FindById(_huntPrey[k]);
                if (step == HuntStep.None || prey == null || !prey.Alive) continue;
                ThreatAnswer answers;
                string focus = string.Empty;
                switch (step)
                {
                    case HuntStep.Track:
                        answers = Squad(side, ThreatAnswer.Warden | ThreatAnswer.Medic);
                        break;
                    case HuntStep.Drive:
                        BattleUnit front = FirstOfBand(foe, true);
                        // Under the parry at the center a Hold no longer breaks the Drive: the shove at its end is its beat.
                        answers = (_parryOn ? ThreatAnswer.None : ThreatAnswer.Hold) | (front != null ? ThreatAnswer.FocusFire : ThreatAnswer.None);
                        focus = IdOf(front);
                        break;
                    case HuntStep.Corner:
                        answers = ThreatAnswer.FocusFire;
                        focus = _huntNetter[k];
                        break;
                    default:
                        // The Kill: a parry or a Warden stepping in; while the prey is netted, an Unleash or a treatment frees them.
                        answers = (_fight.PerfectHold ? ThreatAnswer.Parry : ThreatAnswer.None) | Squad(side, ThreatAnswer.Warden);
                        if (prey.SnareTicks > 0) answers |= ThreatAnswer.Unleash | Squad(side, ThreatAnswer.Medic);
                        break;
                }
                list.Add(new Threat(ChiefKits.Name(step), ThreatTier.Move, captain.Id, prey.Id, _huntTicks[k],
                    Percent(prey, ChiefKits.KillPercent), answers | AtBeat, focus, BeatIn(_huntTicks[k])));
            }
        }

        // ------------------------------------------------------------ the beasts

        // Each part's move while it tells, and the cage bear's claw squeezing whoever it holds. Its Pulse while the ribcage
        // stands mends the bear and touches nobody of yours, so it is not here.
        private void ThreatsOfBeast(List<Threat> list, int side)
        {
            for (int p = 0; p < BeastKits.PartCount; p++)
            {
                if (_partTell[p] <= 0) continue;
                BattleUnit part = PartIn(p);
                if (!part.Alive) continue;
                BeastPart which = BeastKits.PartAt(_beastKit, p);
                BattleUnit target = FindById(_partTarget[p]);
                if (target != null && !Markable(target, side)) target = BeastMark(part);
                int amount = 0;
                ThreatAnswer answers;
                string focus = string.Empty;
                switch (which)
                {
                    case BeastPart.Jaw:
                        amount = target == null ? 0 : Share(target, BeastKits.BitePercent, part);
                        answers = (_fight.PerfectHold ? ThreatAnswer.Parry : ThreatAnswer.None) | HoldOn(target) | Squad(side, ThreatAnswer.Warden);
                        break;
                    case BeastPart.Fin:
                        List<BattleUnit> pool = MarksIn(side, AnyMarkIn(side, Row.Back) ? Row.Back : Row.Front);
                        for (int k = 0; k < pool.Count; k++) amount = Math.Max(amount, Share(pool[k], BeastKits.DivePercent, part));
                        answers = ThreatAnswer.FallBack | Squad(side, ThreatAnswer.Warden | ThreatAnswer.Trapper);
                        target = null;
                        break;
                    case BeastPart.Spine:
                    case BeastPart.Tail:
                        bool sweep = which == BeastPart.Tail;
                        Row first = sweep ? Row.Front : Row.Back;
                        List<BattleUnit> hit = MarksIn(side, AnyMarkIn(side, first) ? first : first == Row.Front ? Row.Back : Row.Front);
                        for (int k = 0; k < hit.Count; k++)
                            amount += Share(hit[k], sweep ? BeastKits.SweepPercent : BeastKits.VolleyPercent, part);
                        answers = ThreatAnswer.FallBack | Squad(side, sweep ? ThreatAnswer.Warden | ThreatAnswer.Medic : ThreatAnswer.Warden);
                        target = null;
                        break;
                    case BeastPart.Claw:
                        amount = target == null ? 0 : Share(target, BeastKits.GrabPercent, part);
                        answers = ThreatAnswer.FallBack | Squad(side, ThreatAnswer.Warden);
                        break;
                    case BeastPart.Maw:
                        answers = ThreatAnswer.Hold | ThreatAnswer.FocusFire;
                        focus = part.Id;
                        target = null;
                        break;
                    case BeastPart.Heart:
                        // While the ribcage stands its Pulse mends the bear and touches nobody of yours; under the parry at the
                        // center a parry still snuffs the mending, so it is here then, with nothing to take.
                        if (BeastCaged)
                        {
                            if (!_parryOn) continue;
                            answers = ThreatAnswer.None;
                            target = null;
                            break;
                        }
                        for (int i = 0; i < _units.Count; i++)
                            if (Markable(_units[i], side)) amount += Share(_units[i], BeastKits.PulsePercent, part);
                        answers = ThreatAnswer.Hold | Squad(side, ThreatAnswer.Medic);
                        target = null;
                        break;
                    default:
                        continue;
                }
                list.Add(new Threat(BeastKits.MoveName(which), ThreatTier.Move, part.Id, IdOf(target), _partTell[p], amount, answers | AtBeat, focus,
                    BeatIn(_partTell[p])));
            }

            // The claw squeezes whoever it holds each second until it breaks or they fall: Focus Fire on the claw breaks it.
            BattleUnit held = _heldId.Length > 0 ? FindById(_heldId) : null;
            BattleUnit claw = Part(BeastPart.Claw);
            if (held == null || !held.Alive || claw == null || !claw.Alive) return;
            int percent = BeastKits.CrushPercent;
            if (_beastPhase >= 2) percent = percent * BeastKits.OverdriveCrushPercent / 100;
            int since = (int)Math.Max(0L, _tick - _heldAt);
            int squeeze = SimClock.TicksPerSecond - since % SimClock.TicksPerSecond;
            list.Add(new Threat(Threat.Crush, ThreatTier.Move, claw.Id, held.Id, squeeze,
                Share(held, percent, claw), ThreatAnswer.FocusFire | AtBeat, claw.Id, BeatIn(squeeze)));
        }
    }
}
