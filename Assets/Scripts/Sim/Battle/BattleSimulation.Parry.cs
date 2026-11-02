using System;

namespace KindredSiege.Sim.Battle
{
    /// The parry at the center (Jacob, 24 Oct 2026: "go, build the parry at the center in the sim behind rules version 8";
    /// DESIGN_LOCKS "The parry at the center"; FightRules.ParryCenter). Every move with a tell has a beat, the moment it lands,
    /// and a Hold started in the last half second before it (FightRules.PerfectHoldTicks) parries it. Every beat is a duel
    /// for nerve:
    /// - Won, with a parry: the move does nothing, whoever did it staggers for FightRules.StaggerTicks and reels (nobody can
    ///   cover him meanwhile), he is left with an Opening, and his side's nerve cracks by the move's crack, harder with each
    ///   parry in a row (ChainStepPercent more each, up to ChainCapPercent). Glory no longer halves a second parry in a row.
    /// - Lost too early, with a Hold that is up but not in the window: it only soaks, and the holding side's nerve cracks by
    ///   the move's crack.
    /// - Lost too late, or with no Hold: the move lands in full, against a chief your Glory drops a rank, and your parry
    ///   chain starts over.
    /// - Skipped, with a safe answer (every answer each kit had before, Fall Back from a heavy swing or a cast): you are safe,
    ///   but his nerve cracks only SafePercent of a parry's.
    /// Nothing is unparryable any more, a Hold given while paused never parries, and a rival who learned to wait out the
    /// parry, or to feint, does it once per beat. Every number is a starting value: build first, tune in the tuning weeks.
    ///
    /// The squad first (FightRules.SquadFirst, campaign rules version 9; Jacob, 24 Oct 2026, later: "go, build rules 9 with
    /// your three picks as written"; Claude's proposal at HANDOFF_LOG line 10945) changes the parry at the center so the
    /// squad matters as much as the parry:
    /// - Big beats and small beats. A chief's kit moves and spell, every cast, every beast move and the hunt's steps are big
    ///   beats, duels as above. A band's heavy swings are small beats: parried they still stagger, reel and open the swinger
    ///   and give the Riposte its window, but they crack no nerve, step no chain, and landing they drop nothing.
    /// - A Hold is a stance again: a stale one only soaks, for its point and its drain. No block cost.
    /// - Points come from the squad: a parry gives back the Hold's point and no more, cashing a mark earns one, and the
    ///   trickle runs faster.
    /// - The squad breaks nerve too: cashing a mark cracks it, the chain stops at SquadChainCapPercent, a safe answer to a big
    ///   beat cracks SquadSafePercent of a parry, and Glory halves a second parry in a row again.
    /// - A learned wait out and feint sit on one move each (TacticsProfile.WaitOutMove, FeintMove), never on the same beat,
    ///   and a wait out comes at least WaitOutMinTicks late.
    public static class ParryRules
    {
        /// A safe answer cracks his nerve this percent of what a parry of the same move would.
        public const int SafePercent = 25;

        /// Each parry in a row cracks this many percent harder than a single parry, up to ChainCapPercent of one.
        public const int ChainStepPercent = 50;
        public const int ChainCapPercent = 300;

        /// A feint: the fake beat, and how long the real one comes after it. The faker is open meanwhile.
        public const int FeintTicks = 30;

        /// A double swing (BandTrait.Double): the light swing's weight against a heavy one, and the gap before the heavy one.
        public const int DoubleLightPercent = 50;
        public const int DoubleGapTicks = 15;

        /// A beat the 22 Oct moves did not have (the Sun Mantle's flare, the Leeches' leap and the rest marked "new" in the
        /// lock) comes this long after its tell.
        public const int TellTicks = 30;

        /// The Riptide's yank comes this long into it.
        public const int YankTicks = 2 * SimClock.TicksPerSecond;

        /// A Reaction event's detail: a stale Hold met a beat (actor side the side that held, target the doer, amount the nerve
        /// it cost); a fake beat (actor the faker, amount the ticks until the real one); the light first swing of a double
        /// swing (actor the swinger, amount the ticks until the heavy one).
        public const string BlockDetail = "block";
        public const string FeintDetail = "feint";
        public const string DoubleDetail = "double";

        /// The Parry event's detail for a band's heavy swing (a kit move's is the move's name).
        public const string HeavyName = "Heavy";

        // ------------------------------------------------------------ the squad first (FightRules.SquadFirst)

        /// A band's heavy swings come this often (5 seconds), not every HeavyRules.EveryTicks: the campaign sets it.
        public const int SquadSwingTicks = 150;

        /// A parry gives back this many points: the Hold's own.
        public const int SquadParryPoints = 1;

        /// A safe answer to a big beat cracks his nerve this percent of what a parry of it would.
        public const int SquadSafePercent = 50;

        /// The parry chain stops at this percent of a single parry.
        public const int SquadChainCapPercent = 200;

        /// Cashing a mark (an Aimed Shot on a snare, a strike on an Opening, a rip on a bleed) earns this many points and
        /// cracks the marked side's nerve this much.
        public const int MarkPoints = 1;
        public const int MarkNerve = 40;

        /// The Earn event's detail for a cashed mark.
        public const string MarkReason = "mark";

        /// The trickle: a point back every this many refill periods (every 8 seconds with the 4 second refill).
        public const int SquadTrickleFactor = 2;

        /// A wait out holds its beat at least this long (one second): long enough for a Focus Fire or a Fall Back.
        public const int WaitOutMinTicks = SimClock.TicksPerSecond;
    }

    public sealed partial class BattleSimulation
    {
        /// True when this fight plays the parry at the center (FightRules.ParryCenter).
        private readonly bool _parryOn;

        /// True when this fight plays the squad first on top of it (FightRules.SquadFirst).
        private readonly bool _squadOn;

        /// True when this fight is a champion duel on top of it (FightRules.ChampionDuel, campaign rules version 16).
        private readonly bool _duelOn;

        // A heavy swing is a big beat, a duel for nerve: before the squad first, or in a champion duel, where the champion is
        // no band (DESIGN_LOCKS, "Duels"). Otherwise it is a small beat.
        private bool BigHeavies => !_squadOn || _duelOn;

        // What a beat has done so far: waited out a fresh Hold, feinted, swung the first swing of a double swing, and had
        // that first swing parried.
        private const int BeatWaited = 1;
        private const int BeatFeinted = 2;
        private const int BeatSecond = 4;
        private const int BeatFirstParried = 8;

        // One slot per move a side can have in flight, for what its beat has done (SideState.Beats). A Brand's and a beast
        // part's beats live on the fighter instead (BattleUnit.BeatFlags).
        private enum BeatSlot
        {
            Windup = 0,
            Heavy = 1,
            Cleave = 2,
            Ring = 3,
            Mantle = 4,
            Hook = 5,
            Rifle = 6,
            Advance = 7,
            Jab = 8,
            Basin = 9,
            Leech = 10,
            Veins = 11,
            Draw = 12,
            Sluice = 13,
            Rip = 14,
            Bore = 15,
            Surge = 16,
            Hunt = 17,
            Count = 18
        }

        private const int BeatSlots = (int)BeatSlot.Count;

        // How the side facing a beat meets it: no Hold up, a Hold in the parry window, or a stale one.
        private enum BeatMet
        {
            Open = 0,
            Parry = 1,
            Block = 2
        }

        /// True when this fight plays the parry at the center. Read only, for the screen.
        public bool ParryCenterOn => _parryOn;

        /// True when this fight plays the squad first (FightRules.SquadFirst): a band's heavy swings are small beats, a stale
        /// Hold only soaks, and marks earn and crack. Read only, for the screen.
        public bool SquadFirstOn => _squadOn;

        /// A side's parries in a row since a move last landed on it in full. 0 outside the parry at the center.
        public int ParryChain(int side)
        {
            return _parryOn && side >= 0 && side <= 1 ? _sides[side].ParryChain : 0;
        }

        /// How hard this side's next parry cracks, in percent of a single parry: the chain's step. Read only, for the screen.
        public int NextParryPercent(int side)
        {
            int cap = _squadOn ? ParryRules.SquadChainCapPercent : ParryRules.ChainCapPercent;
            return Math.Min(cap, 100 + ParryRules.ChainStepPercent * ParryChain(side));
        }

        /// Ticks this fighter still reels from a parry or stands open after a feint, or 0: nobody can cover him meanwhile.
        public int ReelTicksLeft(BattleUnit unit)
        {
            return _parryOn && unit != null ? unit.ReelTicks : 0;
        }

        /// True when a side throws a fake beat before each real one (TacticsProfile.Feint, or a band's BandTrait.Feint). Under
        /// the squad first a learned feint is only on its own move (FeintsOn): then this is true when it is on any.
        public bool Feints(int side)
        {
            return _parryOn && side >= 0 && side <= 1 &&
                   ((_tactics[side].Feint >= HabitThreshold && (!_squadOn || _tactics[side].FeintMove.Length > 0)) ||
                    HasBand(side, BandTrait.Feint));
        }

        /// True when a side throws a fake beat before this move's real one (the Parry event's name for it: "Cleave", "Heavy").
        /// A band's feint is on all its moves; a learned one on every move under the parry at the center alone, and only on
        /// its own move under the squad first (TacticsProfile.FeintMove).
        public bool FeintsOn(int side, string move)
        {
            if (!_parryOn || side < 0 || side > 1) return false;
            return HasBand(side, BandTrait.Feint) || (_tactics[side].Feint >= HabitThreshold && LearnedOn(_tactics[side].FeintMove, move));
        }

        /// True when a side holds this move's beat until a fresh Hold goes stale: a learned wait out (only on its own move under
        /// the squad first, TacticsProfile.WaitOutMove), or a band with Delay (all its moves).
        public bool WaitsOutOn(int side, string move)
        {
            return _parryOn && side >= 0 && side <= 1 && (LearnedWaitOut(side, move) || Delays(side));
        }

        // A learned wait out acting on this move.
        private bool LearnedWaitOut(int side, string move)
        {
            return WaitsOutParry(side) && LearnedOn(_tactics[side].WaitOutMove, move);
        }

        // Under the parry at the center alone a learned habit acts on every beat; under the squad first only on its own move.
        private bool LearnedOn(string habitMove, string move)
        {
            return !_squadOn || (habitMove.Length > 0 && string.Equals(habitMove, move, StringComparison.Ordinal));
        }

        // The name a beat's move goes by on its Parry event, which is what a learned feint or wait out sits on.
        private string SlotMove(int side, BeatSlot slot)
        {
            switch (slot)
            {
                case BeatSlot.Windup: return CastName(side);
                case BeatSlot.Heavy: return ParryRules.HeavyName;
                case BeatSlot.Cleave: return ChiefKits.CleaveName;
                case BeatSlot.Ring: return ChiefKits.RingName;
                case BeatSlot.Mantle: return ChiefKits.MantleName;
                case BeatSlot.Hook: return ChiefKits.HookName;
                case BeatSlot.Rifle: return ChiefKits.RifleName;
                case BeatSlot.Advance: return ChiefKits.AdvanceName;
                case BeatSlot.Jab: return ChiefKits.OverclockName;
                case BeatSlot.Basin: return ChiefKits.BasinName;
                case BeatSlot.Leech: return ChiefKits.LeechName;
                case BeatSlot.Veins: return ChiefKits.VeinsName;
                case BeatSlot.Draw: return ChiefKits.TransfusionName;
                case BeatSlot.Sluice: return ChiefKits.SluiceName;
                case BeatSlot.Rip: return ChiefKits.RiptideName;
                case BeatSlot.Bore: return ChiefKits.BoreName;
                case BeatSlot.Surge: return ChiefKits.SurgeName;
                default: return string.Empty;
            }
        }

        // The cast winding up on a side, by the name its release and its Parry event give it.
        private string CastName(int side)
        {
            ChiefSpell spell = _rules[side].Spell;
            return _sides[side].WindupPyre ? Moves.Pyre
                : spell == ChiefSpell.Eclipse ? Directives.Name(Directive.Eclipse) : ChiefSpells.Name(spell);
        }

        /// True when a side's band has Delay (BandTrait.Unstoppable under the parry at the center): every beat of theirs waits
        /// out a fresh Hold once.
        public bool Delays(int side)
        {
            return _parryOn && HasBand(side, BandTrait.Unstoppable);
        }

        // ------------------------------------------------------------ the beat

        // A beat is due: does it land now? A side that feints throws a fake beat first and is open meanwhile; a side that
        // learned to wait out the parry, or a band with Delay, holds it until a fresh Hold goes stale. Each once per beat.
        // Returns the ticks to hold it, 0 to land now. move is the beat's move by its Parry event's name: under the squad first
        // a learned feint or wait out acts only on its own move, a beat that feinted never also waits out, and a wait out holds
        // the beat at least ParryRules.WaitOutMinTicks.
        private int HoldBeat(int side, BattleUnit doer, ref int flags, string move)
        {
            if (!_parryOn || doer == null || !doer.Alive) return 0;
            if ((flags & BeatFeinted) == 0 && FeintsOn(side, move))
            {
                flags |= BeatFeinted;
                doer.ReelTicks = Math.Max(doer.ReelTicks, ParryRules.FeintTicks);
                Emit(EventFor(BattleEventKind.Reaction, doer, null, ParryRules.FeintTicks, ParryRules.FeintDetail));
                Fired(side, LessonFiring.Feint, doer, null);
                return ParryRules.FeintTicks;
            }
            if ((flags & BeatWaited) == 0 && !(_squadOn && (flags & BeatFeinted) != 0) && WaitsOutOn(side, move))
            {
                int wait = FreshHoldWait(1 - side);
                if (wait > 0)
                {
                    if (_squadOn) wait = Math.Max(wait, ParryRules.WaitOutMinTicks);
                    flags |= BeatWaited;
                    Emit(EventFor(BattleEventKind.Reaction, doer, null, wait, WaitOutDetail));
                    Fired(side, LearnedWaitOut(side, move) ? LessonFiring.WaitOut : LessonFiring.Delay, doer, null);
                    return wait;
                }
            }
            return 0;
        }

        // The same for a beat whose memory lives on a fighter: the one a Brand is on, or a beast's part.
        private int HoldBeat(int side, BattleUnit doer, BattleUnit carrier, string move)
        {
            int flags = carrier.BeatFlags;
            int hold = HoldBeat(side, doer, ref flags, move);
            carrier.BeatFlags = flags;
            return hold;
        }

        private int HoldBeat(int side, BattleUnit doer, BeatSlot slot)
        {
            return HoldBeat(side, doer, ref _sides[side].Beats[(int)slot], SlotMove(side, slot));
        }

        private void ClearBeat(int side, BeatSlot slot)
        {
            _sides[side].Beats[(int)slot] = 0;
        }

        private BeatMet MeetBeat(int facing)
        {
            SideState s = _sides[facing];
            if (s.HoldTicks <= 0) return BeatMet.Open;
            // Precognition (rules 29) widens the window, the tight one from rules 26.
            bool fresh = _fight.PerfectHold && s.LastHoldTick >= 0 && _tick - s.LastHoldTick <= ParryWindow(facing);
            if (fresh) return BeatMet.Parry;
            // From rules 26 a Hold out of the window is a whiff: it ends, soaks nothing, and the blow lands in full
            // (BattleSimulation.Tight.cs).
            if (_tightOn)
            {
                Whiff(facing);
                return BeatMet.Open;
            }
            return BeatMet.Block;
        }

        // The side facing the doer parried his beat, after the move's own "parried" event. He staggers and reels, he is left
        // with an Opening (even a chief whose band stands), the parrying side's Hold ends, it earns, and his side's nerve cracks
        // by the move's crack times the chain, twice for a band weak to the parry. A chief's move cracks it through his Answer,
        // so the parry also climbs Glory and counts in his hidden chains. chief is whose move it was when someone else did it
        // (her Medic's needle, his acolyte's Basin, his engineer's Sluice Gate); null when the doer is the chief.
        private void ParriedBeat(int side, BattleUnit doer, string move, int crack, int points, bool kit, BattleUnit chief = null)
        {
            int cracked = ParryLands(side, doer, move, crack, points);
            ParryCracks(side, chief ?? doer, cracked, points, kit);
        }

        // The parry's first half: everything but the crack, which it returns (chained, and doubled for a weak band). A small
        // beat's parry (duel false) steps no chain and returns no crack. Under the squad first every parry gives back only
        // the Hold's point.
        private int ParryLands(int side, BattleUnit doer, string move, int crack, int points, bool duel = true, int counter = 0)
        {
            if (_squadOn) points = ParryRules.SquadParryPoints;
            int facing = 1 - side;
            SideState theirs = _sides[facing];
            // Sanctuary's price (rules 28): the parrying side's parries do not stagger (BattleSimulation.Shade.cs).
            // Command Relay (rules 29) gives part of it back.
            int stagger = SanctuaryPriced(facing) ? FightRules.StaggerTicks * (100 - PriceLeft(facing)) / 100 : FightRules.StaggerTicks;
            // Answer the Parry (rules 29) remembers who was parried, and by whom.
            if (_gluttonOn)
            {
                doer.ParriedAtTick = _tick;
                doer.ParriedBySide = facing;
            }
            Emit(new BattleEvent(_tick, BattleEventKind.Parry, facing, string.Empty, -1, doer.Id, (int)doer.Class,
                stagger, move));
            if (stagger > 0)
            {
                doer.Cooldown += stagger;
                doer.ReelTicks = stagger;
            }
            // From rules 26 every parry hits him back for the move's damage, once the tick's moves are done; counter is it
            // when the caller knows it, else his heavy blow (BattleSimulation.Tight.cs).
            if (_tightOn) OweCounter(doer, counter);
            // Blood Parry (rules 34, BattleSimulation.DuoMoves.cs): the marks on him burst.
            if (_duoMovesOn) BloodParry(facing, doer);
            theirs.HoldTicks = 0;
            theirs.LastParryTick = _tick;
            theirs.ParriedCasterId = doer.Id;
            // Sanctuary, the Shade's Hold trick (rules 23): a clean parry shields the parrying side (BattleSimulation.Shade.cs).
            if (_floorOn) Sanctuary(facing);
            if (_classOn && doer.Alive)
            {
                // Strip (rules 29) makes it last longer.
                int opening = Stripped(facing, ClassSpells.OpeningTicks);
                doer.Statuses.Apply(StatusKind.Opening, opening);
                MarkEvent(facing, null, doer, opening, ClassSpells.OpeningMark);
            }
            if (!duel)
            {
                Earn(facing, points, FightRules.ParryReason);
                // Mirror, the Shade's other Hold route (rules 28, BattleSimulation.Shade.cs): a small beat counts one.
                if (_shadeOn) Mirror(facing, doer, 1);
                return 0;
            }
            int percent = NextParryPercent(facing);
            int weak = HasBand(side, BandTrait.WeakToParry) ? BandRules.WeakParryPercent : 100;
            theirs.ParryChain++;
            Earn(facing, points, FightRules.ParryReason);
            if (_shadeOn) Mirror(facing, doer, theirs.ParryChain);
            return crack * percent / 100 * weak / 100;
        }

        // And its second: the crack, through his Answer for a chief's move.
        private void ParryCracks(int side, BattleUnit doer, int cracked, int points, bool kit)
        {
            if (Stopped) return;
            if (_squadOn) points = ParryRules.SquadParryPoints;
            if (kit)
            {
                SayOnParry(doer);
                Answer(1 - side, ChiefAnswer.Parry, doer, points, cracked);
            }
            else DrainNerve(side, cracked, 1 - side);
        }

        // A stale Hold met a beat that hits: the move only soaks (the caller lands it soaked), and the holding side's nerve
        // cracks as hard as a parry of it would have cracked the doer's, before the chain. Under the squad first a Hold is a
        // stance again: it only soaks, and costs nothing more, except in a champion duel, where the 24 Oct rule stands.
        private void BlockedBeat(int side, BattleUnit doer, int crack)
        {
            if (_squadOn && !_duelOn) return;
            int facing = 1 - side;
            Emit(new BattleEvent(_tick, BattleEventKind.Reaction, facing, string.Empty, -1, doer == null ? string.Empty : doer.Id,
                doer == null ? -1 : (int)doer.Class, crack, ParryRules.BlockDetail));
            DrainNerve(facing, crack, side);
        }

        // A beat landed in full on the side facing it, with no safe answer: its parry chain starts over. Against a chief its
        // Glory drops a rank too (Eaten), which the caller gives.
        private void LandedBeat(int side)
        {
            _sides[1 - side].ParryChain = 0;
        }

        // A small beat, under the squad first: a band's heavy swing parried. It staggers, reels and opens him, the Riposte
        // gets its window and the Hold's point comes back, but there is no duel: no crack, no step on the chain.
        private void SmallParried(int side, BattleUnit doer, string move)
        {
            ParryLands(side, doer, move, 0, ParryRules.SquadParryPoints, false);
        }

        // A safe answer's share of a parry's crack: a quarter, half under the squad first.
        private int SafePercent => _squadOn ? ParryRules.SquadSafePercent : ParryRules.SafePercent;

        // A safe answer to a beat of a band's or a plain cast's: his side's nerve cracks a quarter of a parry's. A chief's moves
        // get theirs through his Answer instead.
        private void SafeBeat(int side, int crack)
        {
            DrainNerve(side, crack * SafePercent / 100, 1 - side);
        }

        // What an answer to a chief's move cracks off his nerve: under the parry at the center a parry cracks the most and every
        // other answer a quarter of that (half under the squad first).
        private int AnswerNerve(ChiefAnswer answer)
        {
            if (!_parryOn) return ChiefKits.Nerve(answer);
            return answer == ChiefAnswer.Parry ? ChiefKits.ParryNerve : ChiefKits.ParryNerve * SafePercent / 100;
        }

        // Under the squad first a mark cashed in (an Aimed Shot on a snare, a strike on an Opening, a rip on a bleed) earns
        // its side a point and cracks the marked side's nerve.
        private void MarkCashed(BattleUnit by, BattleUnit on)
        {
            if (!_squadOn || _finished) return;
            Earn(by.Side, ParryRules.MarkPoints, ParryRules.MarkReason);
            // In a road combat road fight a cashed mark shakes the one it was on (BattleSimulation.Road.cs).
            if (!Stopped && _unitNerveOn) UnitDrain(on, ParryRules.MarkNerve, by.Side);
            else if (!Stopped) DrainNerve(on.Side, ParryRules.MarkNerve, by.Side);
        }

        // ------------------------------------------------------------ the cast's beat

        // The cast's release under the parry at the center: every chief spell can be parried there, fire and the tide too, and
        // a Hold that is not a parry soaks every one of them. Her Steel Storm and a War Cry go up on the caster's own side, so
        // they hit nobody: parried they never go up, otherwise they do, and a stale Hold costs nothing. A blast is dodged by Fall
        // Back (a safe answer), parried, blocked (it soaks and the holder's nerve cracks) or eaten in full.
        private void ReleaseCast(int side, BattleUnit caster)
        {
            SideState state = _sides[side];
            int enemy = 1 - side;
            SideState theirs = _sides[enemy];
            int percent = state.WindupPercent;
            bool pyre = state.WindupPyre;
            string pyreTargetId = state.PyreTargetId;
            ClearWindup(state);

            ChiefSpell spell = _rules[side].Spell;
            bool kit = KitOn(side);
            int crack = kit ? ChiefKits.ParryNerve : HeavyRules.EclipseParryNerve;
            string name = pyre ? Moves.Pyre : spell == ChiefSpell.Eclipse ? Directives.Name(Directive.Eclipse) : ChiefSpells.Name(spell);
            BeatMet met = MeetBeat(enemy);
            if (spell == ChiefSpell.Eclipse) BlastWent(side);

            if (spell == ChiefSpell.SteelStorm || spell == ChiefSpell.WarCry)
            {
                if (met == BeatMet.Parry)
                {
                    CastParried(side, caster, percent, name, crack, kit);
                    return;
                }
                if (spell == ChiefSpell.SteelStorm) RaiseWall(side, caster);
                else CryOut(side, caster);
                return;
            }

            bool bloodCall = spell == ChiefSpell.BloodCall;
            bool shark = spell == ChiefSpell.DustShark || spell == ChiefSpell.Tide;
            bool tideKit = spell == ChiefSpell.Tide && _tideOn && side == _tideSide;
            int tide = tideKit ? TideScale() : 100;
            percent = percent * tide / 100;
            bool dodged = theirs.FallBackTicks > 0;
            if (!dodged && met == BeatMet.Parry)
            {
                if (bloodCall && _riteOn && Kit(side) == ChiefKit.Stoneveil) state.ChantStitches = 0;
                CastParried(side, caster, percent, name, crack, kit);
                return;
            }
            // From rules 21 the Eclipse is a blast: a Hold that is not a parry soaks nothing (BattleSimulation.Danger.cs).
            bool soaked = !dodged && met == BeatMet.Block && !(_dangerOn && spell == ChiefSpell.Eclipse);
            if (soaked)
            {
                BlockedBeat(side, caster, crack);
                if (Stopped) return;
            }
            // The Stoneveil chief's rip is answered his own way (BattleSimulation.Stoneveil.cs), soaked like any blast.
            if (bloodCall && _riteOn && Kit(side) == ChiefKit.Stoneveil)
            {
                if (!dodged && !soaked) LandedBeat(side);
                ReleaseRite(side, caster, soaked ? percent * Moves.EclipseHoldPercent / 100 : percent, dodged);
                return;
            }
            Emit(new BattleEvent(_tick, BattleEventKind.Release, side, caster.Id, (int)caster.Class, string.Empty, -1,
                percent, dodged ? Moves.Dodged : soaked ? Moves.Soaked : Moves.Full));
            if (dodged)
            {
                Earn(enemy, FightRules.DodgePoints, FightRules.DodgeReason);
                FavourDodge(enemy, caster);
                if (kit) KitEclipseLanded(side, caster, true, false);
                else SafeBeat(side, crack);
                return;
            }
            if (!soaked) LandedBeat(side);
            if (kit) KitEclipseLanded(side, caster, false, soaked);
            if (Stopped) return;
            if (bloodCall)
            {
                CallBlood(side, caster, soaked ? percent * Moves.EclipseHoldPercent / 100 : percent);
                return;
            }

            Row first = shark ? Row.Back : Row.Front;
            Row row = AnyAlive(enemy, first) ? first : first == Row.Front ? Row.Back : Row.Front;
            bool bothRows = tideKit && SpringTide(side);
            for (int i = 0; i < _units.Count && !_finished; i++)
            {
                BattleUnit u = _units[i];
                if (!u.Alive || u.Side != enemy || (u.Row != row && !bothRows)) continue;
                int damage = u.MaxHp * percent / 100;
                if (soaked) damage = damage * Moves.EclipseHoldPercent / 100;
                ApplyDamage(caster, side, u, Math.Max(1, damage),
                    !shark ? Moves.EclipseDetail : spell == ChiefSpell.Tide ? ChiefSpells.TideDetail : ChiefSpells.BiteDetail);
            }
            if (!_finished)
                DrainNerve(enemy, (soaked ? Moves.EclipseNerveCost * Moves.EclipseHoldPercent / 100 : Moves.EclipseNerveCost) * tide / 100,
                    side);

            if (pyre && !_finished)
            {
                BattleUnit target = FindById(pyreTargetId);
                if (target != null && target.Alive)
                {
                    int burn = Math.Max(1, target.MaxHp * Moves.PyrePercent / 100);
                    Emit(new BattleEvent(_tick, BattleEventKind.Combo, side, caster.Id, (int)caster.Class,
                        target.Id, (int)target.Class, burn, Moves.Pyre));
                    Earn(side, FightRules.ComboPoints, FightRules.ComboReason);
                    ApplyDamage(caster, side, target, burn, Moves.PyreDetail);
                    if (!_finished) DrainNerve(enemy, Moves.PyreNerveCost, side);
                }
            }
            CheckWipe();
        }

        // A cast parried at its release never goes off. The Bloodridge chief's Eclipse can finish his hidden chain (Sunbreaker);
        // the Stoneveil chief's Blood Call still counts toward your Clean Hands.
        private void CastParried(int side, BattleUnit caster, int percent, string name, int crack, bool kit)
        {
            Emit(new BattleEvent(_tick, BattleEventKind.Release, side, caster.Id, (int)caster.Class, string.Empty, -1, percent,
                FightRules.Parried));
            ChiefSpell spell = _rules[side].Spell;
            if (kit && spell == ChiefSpell.BloodCall && _riteOn && Kit(side) == ChiefKit.Stoneveil)
            {
                Emit(EventFor(BattleEventKind.Land, caster, null, 0, ChiefSpells.Name(ChiefSpell.BloodCall) + "." + ChiefKits.Parried));
                RiteLanded(side, caster);
            }
            // Her Steel Storm parried as her Wardens lock shields: the wall never stands, which counts toward your Breach like a
            // smash in its first second.
            if (kit && spell == ChiefSpell.SteelStorm)
            {
                _sides[side].LockSnared = false;
                if (Kit(side) == ChiefKit.Skullpeak) _sides[1 - side].Chain++;
            }
            int cracked = ParryLands(side, caster, name, crack, FightRules.ParryPoints, true, _tightOn ? CastCounter(side, percent) : 0);
            if (kit && Kit(side) == ChiefKit.Bloodridge) KitEclipseParried(side, caster, percent, cracked);
            else ParryCracks(side, caster, cracked, FightRules.ParryPoints, kit);
        }

        // Reels and feints wear off.
        private void CountDownReel()
        {
            for (int i = 0; i < _units.Count; i++)
                if (_units[i].ReelTicks > 0) _units[i].ReelTicks--;
        }

        private void AppendParry(SnapshotBuilder b, SideState state)
        {
            b.Field("parryChain", state.ParryChain);
            b.Field("mantleFlare", state.MantleFlare);
            b.Field("advanceHold", state.AdvanceHold);
            b.Field("basinHold", state.BasinHold);
            b.Field("leechLeap", state.LeechLeap);
            b.Field("veinsCut", state.VeinsCut);
            b.Field("drawLatch", state.DrawLatch);
            b.Field("ripHold", state.RipHold);
            b.Field("boreHold", state.BoreHold);
            for (int k = 0; k < BeatSlots; k++)
                b.Field("beat" + k.ToString(System.Globalization.CultureInfo.InvariantCulture), state.Beats[k]);
        }
    }
}
