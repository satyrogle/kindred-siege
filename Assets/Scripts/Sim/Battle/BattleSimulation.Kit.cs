using System;
using System.Collections.Generic;
using System.Globalization;

namespace KindredSiege.Sim.Battle
{
    /// The chief kits and the kneel (Jacob, 20 Oct 2026, later, DESIGN_LOCKS "Chief fights"; 21 Oct: "go do the chief build").
    /// ChiefKits holds the rules and numbers. Only fights with FightRules.ChiefKits (and the chief rules) come here, so every
    /// other fight plays exactly as before. A Brand picks its mark from the fight's seeded stream; nothing else here draws.
    ///
    /// The Bloodridge chief finished (22 Oct 2026, "go, build the Skullpeak chief and the Bloodridge extras in the sim"):
    /// his Burning Ring and Sun Mantle, and his forecast (FightRules.ChiefTactics). A Burning Ring picks its lane from the
    /// same stream, and with his forecast a Brand's marks and a ring's lane are picked when they show, 6 s before they are due.
    /// The Skullpeak chief's kit is in BattleSimulation.Skullpeak.cs. A fight with neither, and without the tactics, never
    /// meets any of the 22 Oct additions. The Stoneveil and Thundercoast chiefs' kits, added later that day, are in
    /// BattleSimulation.Stoneveil.cs and BattleSimulation.Thundercoast.cs, and only their fights meet them.
    public sealed partial class BattleSimulation
    {
        // The kneel and the chief kits are on (FightRules.ChiefKits, with the chief rules).
        private readonly bool _kitsOn;

        // The 22 Oct 2026 kit additions are on: a side's kit or moves go past the 21 Oct ones, or the tactical pieces are.
        private readonly bool _kitsTwoOn;

        // The kits' tactical pieces are on (FightRules.ChiefTactics): Bloodridge's forecast, Skullpeak's band jobs.
        private readonly bool _tacticsOn;

        // His forecast keeps one slot for each move he can show, in this order (also the order ties are listed in).
        private const int ForecastSlots = 5;
        private static readonly ChiefMove[] ForecastOrder =
            { ChiefMove.Cleave, ChiefMove.Brand, ChiefMove.Eclipse, ChiefMove.BurningRing, ChiefMove.SunMantle };

        // A chief kneels: nothing moves until the other side spares or executes him.
        private bool _halted;

        // The fight is over, or stopped for a kneeling chief's verdict.
        private bool Stopped => _finished || _halted;

        /// True when this fight plays the chief kits and the kneel.
        public bool ChiefKitsOn => _kitsOn;

        /// A side's chief kit, or ChiefKit.None. Read only, for the screen.
        public ChiefKit Kit(int side)
        {
            return _kitsOn && side >= 0 && side <= 1 ? _rules[side].Kit : ChiefKit.None;
        }

        /// The moves a side's chief took into this fight.
        public ChiefMove KitMoves(int side)
        {
            return Kit(side) == ChiefKit.None ? ChiefMove.None : _rules[side].KitMoves;
        }

        /// True while a chief kneels and the other side has to choose: Hold spares him, Focus Fire on him executes him, and
        /// no other order goes. Nothing moves meanwhile, so the screen holds the fight's clock here (no timer, Jacob's lock);
        /// if the clock runs out anyway, he is spared.
        public bool AwaitingVerdict => _halted && !_finished;

        /// The chief kneeling for that verdict, or null.
        public BattleUnit KneelingChief
        {
            get
            {
                if (!AwaitingVerdict) return null;
                for (int i = 0; i < _units.Count; i++)
                {
                    BattleUnit u = _units[i];
                    if (u.Alive && u.Yielded && u.Role == UnitRole.Chief) return u;
                }
                return null;
            }
        }

        /// Ticks left before a side's chief's Cleave falls, or 0, and on whom.
        public int CleaveTicks(int side)
        {
            return Kit(side) != ChiefKit.None ? _sides[side].CleaveWindup : 0;
        }

        public string CleaveTargetId(int side)
        {
            return CleaveTicks(side) > 0 ? _sides[side].CleaveTargetId : string.Empty;
        }

        /// True while a side's Cleave winds up and a Hold timed to its last FightRules.PerfectHoldTicks would parry it.
        public bool CleaveParryable(int side)
        {
            return CleaveTicks(side) > 0 && _fight.PerfectHold && !Unparryable(side);
        }

        /// True once a side's chief has reached his second phase in a kit that has one (Bloodridge's Blood Sun).
        public bool BloodSun(int side)
        {
            BattleUnit chief = KitChief(side);
            return Kit(side) == ChiefKit.Bloodridge && chief != null && chief.PhaseDone;
        }

        /// True when this side faces a chief with a kit, so its answers earn Glory.
        public bool GloryOn(int side)
        {
            return _kitsOn && side >= 0 && side <= 1 && _rules[1 - side].Kit != ChiefKit.None;
        }

        /// A side's Glory, 0 to ChiefKits.GloryMax, and its rank: the grade it holds when the fight ends.
        public int Glory(int side)
        {
            return GloryOn(side) ? _sides[side].Glory : 0;
        }

        public GloryRank GloryRankOf(int side)
        {
            return ChiefKits.Rank(Glory(side));
        }

        /// True when this fight plays the kits' tactical pieces (FightRules.ChiefTactics).
        public bool ChiefTacticsOn => _tacticsOn;

        /// Ticks left before a side's chief throws his spinning Burning Ring, or 0, and whose lane it goes down.
        public int RingTicks(int side)
        {
            return Kit(side) != ChiefKit.None ? _sides[side].RingSpin : 0;
        }

        public string RingTargetId(int side)
        {
            return RingTicks(side) > 0 ? _sides[side].RingTargetId : string.Empty;
        }

        /// Ticks a side's chief's Sun Mantle still burns, or 0. Every blow that lands on him meanwhile burns its striker.
        public int MantleTicksLeft(int side)
        {
            return Kit(side) != ChiefKit.None ? _sides[side].MantleTicks : 0;
        }

        /// True when a side's chief shows his moves before they are due (Bloodridge's tactical piece).
        public bool ForecastOn(int side)
        {
            return _tacticsOn && Kit(side) == ChiefKit.Bloodridge;
        }

        /// The moves a side's chief shows now, soonest first, ties in the order Cleave, Brand, Eclipse, Burning Ring, Sun
        /// Mantle. Each shows ChiefKits.ForecastTicks before it is due and until it starts. Empty without his forecast.
        public IReadOnlyList<ChiefForecast> Forecasts(int side)
        {
            var list = new List<ChiefForecast>();
            if (!ForecastOn(side)) return list;
            SideState state = _sides[side];
            for (int k = 0; k < ForecastSlots; k++)
            {
                if (!state.ForeShown[k]) continue;
                ChiefMove move = ForecastOrder[k];
                var shown = new ChiefForecast(move, (int)Math.Max(0L, DueTick(side, move) - _tick), state.ForeTarget[k],
                    state.ForeTarget2[k], state.ForeAmount[k]);
                int at = list.Count;
                while (at > 0 && list[at - 1].TicksLeft > shown.TicksLeft) at--;
                list.Insert(at, shown);
            }
            return list;
        }

        // ------------------------------------------------------------ setup and clocks

        private void StartKitClocks()
        {
            for (int s = 0; s < 2; s++)
            {
                if (HasMove(s, ChiefMove.Cleave)) _sides[s].CleaveNext = ChiefKits.CleaveEveryTicks;
                if (HasMove(s, ChiefMove.Brand)) _sides[s].BrandNext = ChiefKits.BrandEveryTicks;
                if (!_kitsTwoOn) continue;
                if (HasMove(s, ChiefMove.BurningRing)) _sides[s].RingNext = ChiefKits.RingEveryTicks;
                if (HasMove(s, ChiefMove.SunMantle)) _sides[s].MantleNext = ChiefKits.MantleEveryTicks;
                StartSkullpeakClocks(s);
                if (_riteOn) StartRiteClocks(s);
                if (_tideOn) StartTideClocks(s);
            }
        }

        // A side's kit or moves go past the 21 Oct ones: a kit after Bloodridge's, or one of his newer moves.
        private bool KitAfterFirst(int side)
        {
            return _rules[side].Kit > ChiefKit.Bloodridge || (_rules[side].KitMoves & ~ChiefKits.FirstWritten) != 0;
        }

        private bool KitOn(int side)
        {
            return Kit(side) != ChiefKit.None;
        }

        private bool HasMove(int side, ChiefMove move)
        {
            return KitOn(side) && (_rules[side].KitMoves & move) != 0;
        }

        // The side's living chief, or null.
        private BattleUnit KitChief(int side)
        {
            BattleUnit leader = side >= 0 && side <= 1 ? Leader(side) : null;
            return leader != null && leader.Role == UnitRole.Chief ? leader : null;
        }

        // He can start a move: standing, not kneeling, not stunned, not held by a snare.
        private bool CanMove(BattleUnit chief)
        {
            return chief.Alive && !chief.Yielded && chief.StunTicks <= 0 && chief.PanicTicks <= 0 && !(_classOn && chief.SnareTicks > 0);
        }

        // A chief winding up his Cleave does nothing else.
        private bool IsCleaving(BattleUnit unit)
        {
            return unit.Role == UnitRole.Chief && _sides[unit.Side].CleaveWindup > 0;
        }

        // Busy with his kit, so he does nothing else this tick: stunned, or his blade glows; from 22 Oct also a chief spinning
        // his ring or throwing her chain, a shooter aiming her Rifle Line, and her Medic jabbing for her Overclock; later that
        // day the Stoneveil chief channelling and his acolyte carrying the Basin, the Thundercoast chief sounding his horn and
        // his engineer cranking.
        private bool KitBusy(BattleUnit unit)
        {
            if (unit.StunTicks > 0 || IsCleaving(unit)) return true;
            if (!_kitsTwoOn) return false;
            if (unit.RifleTicks > 0 || (_huntOn && KillComing(unit))) return true;
            if ((_riteOn && RiteBusy(unit)) || (_tideOn && TideBusy(unit))) return true;
            SideState state = _sides[unit.Side];
            if (unit.Role == UnitRole.Chief && (state.RingSpin > 0 || state.HookTell > 0)) return true;
            return state.JabTicks > 0 && string.Equals(unit.Id, state.JabMedicId, StringComparison.Ordinal);
        }

        // Stuns, and from 22 Oct her Hook's drag and her Lockdown's clocks, count down after the tick's actions.
        private void CountDownKit()
        {
            for (int i = 0; i < _units.Count; i++)
            {
                _units[i].Statuses.CountDown(StatusKind.Stun);
                if (_units[i].HookTicks > 0) _units[i].HookTicks--;
            }
            if (_kitsTwoOn) CountDownLockdown();
            if (_turnedTicks > 0) _turnedTicks--;
        }

        // The 22 Oct moves start on their own clocks: his Burning Ring and Sun Mantle, her Hook, Rifle Line, Iron Advance and
        // Overclock (BattleSimulation.Skullpeak.cs). Each starts only if he took it this run.
        private void StartKitMoves(int side)
        {
            if (!KitOn(side)) return;
            TryStartRing(side);
            if (!Stopped) TryStartMantle(side);
            if (!Stopped) TryStartHook(side);
            if (!Stopped) TryStartRifle(side);
            if (!Stopped) TryStartAdvance(side);
            if (!Stopped) TryStartOverclock(side);
            // The Stoneveil and Thundercoast chiefs' (BattleSimulation.Stoneveil.cs, BattleSimulation.Thundercoast.cs).
            if (_riteOn && !Stopped) StartRiteMoves(side);
            if (_tideOn && !Stopped) StartTideMoves(side);
        }

        // And run on, after the tick's blows, landing when their time comes. The hunt runs here too (BattleSimulation.Hunt.cs).
        private void AdvanceKitMoves(int side)
        {
            if (!KitOn(side)) return;
            if (Kit(side) == ChiefKit.Hunt)
            {
                AdvanceHunt(side);
                return;
            }
            AdvanceRing(side);
            if (!Stopped) AdvanceMantle(side);
            if (!Stopped) AdvanceHook(side);
            if (!Stopped) AdvanceRifle(side);
            if (!Stopped) MarchOn(side);
            if (!Stopped) AdvanceJab(side);
            if (!Stopped) AdvanceOverclock(side);
            if (_riteOn && !Stopped) AdvanceRiteMoves(side);
            if (_tideOn && !Stopped) AdvanceTideMoves(side);
        }

        // The wound up spell's windup: the Skullpeak chief's Wardens lock shields for her Steel Storm in ChiefKits.LockTicks,
        // twice that once her Shieldbearer is dead; every other spell as before.
        private int KitWindupTicks(int side)
        {
            if (Kit(side) != ChiefKit.Skullpeak) return Moves.EclipseWindupTicks;
            return ChiefKits.LockTicks * (JobDown(side, BandJob.Shieldbearer) ? ChiefKits.JobSlowPercent : 100) / 100;
        }

        // A kit's spell started winding up: his forecast of it is spent, her lock starts with none of her Wardens snared, and
        // the Stoneveil chief's chant with no wound stitched.
        private void StartedSpell(int side, SideState state)
        {
            if (ForecastOn(side)) ClearForecast(side, ChiefMove.Eclipse);
            state.LockSnared = false;
            state.ChantStitches = 0;
        }

        // Whoever of the other side's fighters a move can fall on, in setup order (BattleSimulation.Beast.cs, Markable): never
        // their leader while another of theirs stands.
        private List<BattleUnit> MarksOf(int side)
        {
            var list = new List<BattleUnit>();
            for (int i = 0; i < _units.Count; i++)
                if (Markable(_units[i], side)) list.Add(_units[i]);
            return list;
        }

        // The one a move can fall on nearest this lane position in a row of theirs, or null.
        private BattleUnit NearestInLane(int side, Row row, int lane)
        {
            BattleUnit best = null;
            int bestDistance = int.MaxValue;
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (u.Row != row || !Markable(u, side)) continue;
                int distance = Math.Abs(LanePosition(u) - lane);
                if (distance < bestDistance)
                {
                    best = u;
                    bestDistance = distance;
                }
            }
            return best;
        }

        // ------------------------------------------------------------ Cleave

        // Whoever stands across from him: the nearest to his lane in their front row, their back row once it has fallen.
        private BattleUnit AcrossFrom(BattleUnit chief)
        {
            int enemy = 1 - chief.Side;
            Row row = AnyAlive(enemy, Row.Front) ? Row.Front : Row.Back;
            int lane = LanePosition(chief);
            BattleUnit best = null;
            int bestDistance = int.MaxValue;
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (!u.Alive || u.Side != enemy || u.Row != row) continue;
                int distance = Math.Abs(LanePosition(u) - lane);
                if (distance < bestDistance)
                {
                    best = u;
                    bestDistance = distance;
                }
            }
            return best;
        }

        // Every CleaveEveryTicks from the last one's start his blade glows. Never while he winds up his own Eclipse, aims a
        // shot or spins his ring: it waits. With his forecast it falls on whoever it showed, while they stand.
        private void TryStartCleave(int side)
        {
            SideState state = _sides[side];
            if (!HasMove(side, ChiefMove.Cleave) || state.CleaveWindup > 0 || _tick < state.CleaveNext) return;
            // From rules 21 his swings and blasts take turns (BattleSimulation.Danger.cs).
            if (CleaveWaitsForBlast(side)) return;
            BattleUnit chief = KitChief(side);
            BattleUnit target = chief != null && CanMove(chief) && state.Windup <= 0 && chief.AimTicks <= 0 && state.RingSpin <= 0
                ? CleaveMark(side, chief) : null;
            if (target == null)
            {
                state.CleaveNext = _tick + 1;
                return;
            }
            state.CleaveWindup = ChiefKits.CleaveWindupTicks;
            state.CleaveTargetId = target.Id;
            state.CleaveWaited = false;
            state.SwungLast = _dangerOn;
            state.CleaveNext = _tick + ChiefKits.CleaveEveryTicks;
            if (ForecastOn(side)) ClearForecast(side, ChiefMove.Cleave);
            Emit(EventFor(BattleEventKind.Tell, chief, target, ChiefKits.CleaveWindupTicks, ChiefKits.CleaveName));
        }

        private BattleUnit CleaveMark(int side, BattleUnit chief)
        {
            BattleUnit shown = ForecastMark(side, ChiefMove.Cleave);
            return shown != null && shown.Alive && shown.Side != side ? shown : AcrossFrom(chief);
        }

        // The glow runs on; once it has run the Cleave falls, unless his tribe learned to wait out a fresh Hold and has not
        // waited yet this swing (the memory of your last fight: he fakes the windup).
        private void AdvanceCleave(int side)
        {
            SideState state = _sides[side];
            if (state.CleaveWindup <= 0) return;
            BattleUnit chief = KitChief(side);
            if (chief == null || !CanMove(chief))
            {
                ClearCleave(state);
                return;
            }
            state.CleaveWindup--;
            if (state.CleaveWindup > 0) return;
            // Under the parry at the center the fall is its beat (BattleSimulation.Parry.cs).
            if (_parryOn)
            {
                int hold = HoldBeat(side, chief, BeatSlot.Cleave);
                if (hold > 0) state.CleaveWindup = hold;
                else ReleaseCleave(side, chief);
                return;
            }
            if (!state.CleaveWaited && !Unparryable(side) && WaitsOutParry(side))
            {
                int wait = FreshHoldWait(1 - side);
                if (wait > 0)
                {
                    state.CleaveWaited = true;
                    state.CleaveWindup = wait;
                    Emit(EventFor(BattleEventKind.Reaction, chief, null, wait, WaitOutDetail));
                    return;
                }
            }
            ReleaseCleave(side, chief);
        }

        // The Cleave falls: parried by a Hold started in the parry window, taken by a Warden covering its target, soaked by
        // any other Hold on the front row, or in full. Under Blood Sun a Cleave that lands leaves its victim bleeding.
        private void ReleaseCleave(int side, BattleUnit chief)
        {
            SideState state = _sides[side];
            int enemy = 1 - side;
            SideState theirs = _sides[enemy];
            BattleUnit target = FindById(state.CleaveTargetId);
            ClearCleave(state);
            if (target == null || !target.Alive) target = AcrossFrom(chief);
            if (target == null) return;
            string name = ChiefKits.CleaveName + ".";

            // Under the parry at the center the fall is a duel for nerve (BattleSimulation.Parry.cs).
            BeatMet met = _parryOn ? MeetBeat(enemy) : BeatMet.Open;
            if (_parryOn && met == BeatMet.Parry)
            {
                Emit(EventFor(BattleEventKind.Land, chief, target, 0, name + ChiefKits.Parried));
                theirs.Chain++;
                ParriedBeat(side, chief, ChiefKits.CleaveName, ChiefKits.ParryNerve, FightRules.ParryPoints, true);
                return;
            }
            if (!_parryOn && _fight.PerfectHold && !Unparryable(side) && theirs.HoldTicks > 0 && theirs.LastHoldTick >= 0 &&
                _tick - theirs.LastHoldTick <= ParryWindowTicks)
            {
                Emit(EventFor(BattleEventKind.Land, chief, target, 0, name + ChiefKits.Parried));
                Emit(new BattleEvent(_tick, BattleEventKind.Parry, enemy, string.Empty, -1, chief.Id, (int)chief.Class,
                    FightRules.StaggerTicks, ChiefKits.CleaveName));
                chief.Cooldown += FightRules.StaggerTicks;
                theirs.HoldTicks = 0;
                theirs.LastParryTick = _tick;
                theirs.ParriedCasterId = chief.Id;
                theirs.Chain++;
                Earn(enemy, FightRules.ParryPoints, FightRules.ParryReason);
                SayOnParry(chief);
                Answer(enemy, ChiefAnswer.Parry, chief, FightRules.ParryPoints);
                return;
            }

            theirs.Chain = 0;
            int damage = HeavyDamage(chief, target);
            BattleUnit warden = GuardingWarden(target);
            if (warden != null)
            {
                int cut = _classOn ? GuardCut(warden) : SliceClasses.GuardReductionPercent;
                int guarded = damage * (100 - cut) / 100;
                Emit(EventFor(BattleEventKind.Land, chief, target, guarded, name + ChiefKits.Guarded));
                Emit(EventFor(BattleEventKind.Guard, warden, target, damage - guarded, string.Empty));
                ApplyDamage(chief, side, warden, Armored(warden, guarded), ChiefKits.CleaveDetail);
                if (_classOn && !Stopped) Retaliate(warden, chief);
                if (!Stopped)
                {
                    Earn(enemy, ChiefKits.GuardPoints, ChiefKits.Name(ChiefAnswer.Guard));
                    Answer(enemy, ChiefAnswer.Guard, chief, ChiefKits.GuardPoints);
                }
                CheckWipe();
                return;
            }

            if (_parryOn)
            {
                if (met == BeatMet.Block) BlockedBeat(side, chief, ChiefKits.ParryNerve);
                else LandedBeat(side);
                if (Stopped) return;
            }
            bool soaked = theirs.HoldTicks > 0 && target.Row == Row.Front;
            Emit(EventFor(BattleEventKind.Land, chief, target, damage, name + (soaked ? ChiefKits.Soaked : ChiefKits.Full)));
            ApplyDamage(chief, side, target, Armored(target, damage), ChiefKits.CleaveDetail);
            // Too late, or no Hold: under the parry at the center your Glory drops a rank.
            if (_parryOn && met == BeatMet.Open && !Stopped) Eaten(enemy, chief);
            if (!Stopped) Bleed(chief, target);
            if (!Stopped && target.Alive && BloodSun(side))
            {
                target.Statuses.Apply(StatusKind.Bleed, BleedRules.Ticks, ChiefKits.BloodSunBleedPercent);
                Emit(EventFor(BattleEventKind.Mark, chief, target, BleedRules.Ticks, ClassSpells.BleedMark));
            }
            CheckWipe();
        }

        private static void ClearCleave(SideState state)
        {
            state.CleaveWindup = 0;
            state.CleaveTargetId = string.Empty;
            state.CleaveWaited = false;
            state.Beats[(int)BeatSlot.Cleave] = 0;
        }

        // ------------------------------------------------------------ Brand

        // Every BrandEveryTicks a burning sigil goes over one of the other side's fighters (two under Blood Sun), never their
        // leader, picked from the fight's seeded stream. With his forecast it goes over whoever it showed, while they can
        // take it, and only a mark it could not keep is picked now.
        private void TryStartBrand(int side)
        {
            SideState state = _sides[side];
            if (!HasMove(side, ChiefMove.Brand) || _tick < state.BrandNext) return;
            BattleUnit chief = KitChief(side);
            List<BattleUnit> pool = chief != null && CanMove(chief) ? Brandable(side) : new List<BattleUnit>();
            if (pool.Count == 0)
            {
                state.BrandNext = _tick + 1;
                return;
            }
            state.BrandNext = _tick + ChiefKits.BrandEveryTicks;
            int marks = BloodSun(side) ? ChiefKits.BloodSunBrands : 1;
            int k = ForecastOn(side) ? BrandShown(side, chief, pool, marks) : 0;
            for (; k < marks && pool.Count > 0; k++)
            {
                int pick = _rng.Next(pool.Count);
                BattleUnit marked = pool[pick];
                pool.RemoveAt(pick);
                marked.BrandTicks = ChiefKits.BrandRingTicks;
                Emit(EventFor(BattleEventKind.Tell, chief, marked, ChiefKits.BrandRingTicks, ChiefKits.BrandName));
            }
        }

        // Whom a Brand can go over: the other side's standing fighters, never their leader, none already branded.
        private List<BattleUnit> Brandable(int side)
        {
            var pool = new List<BattleUnit>();
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (u.Alive && u.Side == 1 - side && !u.IsLeader && u.BrandTicks <= 0) pool.Add(u);
            }
            return pool;
        }

        // His forecast's Brand marks that can still take it are branded first, and leave the pool. Returns how many.
        private int BrandShown(int side, BattleUnit chief, List<BattleUnit> pool, int marks)
        {
            SideState state = _sides[side];
            int slot = ForecastSlot(ChiefMove.Brand);
            int done = 0;
            for (int n = 0; n < 2 && done < marks && state.ForeShown[slot]; n++)
            {
                BattleUnit shown = FindById(n == 0 ? state.ForeTarget[slot] : state.ForeTarget2[slot]);
                if (shown == null || !pool.Remove(shown)) continue;
                shown.BrandTicks = ChiefKits.BrandRingTicks;
                Emit(EventFor(BattleEventKind.Tell, chief, shown, ChiefKits.BrandRingTicks, ChiefKits.BrandName));
                done++;
            }
            ClearForecast(side, ChiefMove.Brand);
            return done;
        }

        // The rings run down; at zero each Brand bursts. They fizzle if he kneels or falls first.
        private void AdvanceBrands(int side)
        {
            if (!HasMove(side, ChiefMove.Brand)) return;
            BattleUnit chief = KitChief(side);
            for (int i = 0; i < _units.Count && !Stopped; i++)
            {
                BattleUnit u = _units[i];
                if (u.Side != 1 - side || u.BrandTicks <= 0) continue;
                if (!u.Alive || chief == null)
                {
                    u.BrandTicks = 0;
                    if (_parryOn) u.BeatFlags = 0;
                    continue;
                }
                u.BrandTicks--;
                if (u.BrandTicks != 0) continue;
                // Under the parry at the center the burst is its beat (BattleSimulation.Parry.cs).
                int hold = HoldBeat(side, chief, u, ChiefKits.BrandName);
                if (hold > 0) u.BrandTicks = hold;
                else BurstBrand(side, chief, u);
            }
        }

        // The Brand bursts on the branded fighter and those beside them. Fall Back gets everyone clear; a Warden covering
        // the branded one takes their share halved; with their Medic standing and nobody falling to it, she heals through
        // it. Otherwise they ate it.
        private void BurstBrand(int side, BattleUnit chief, BattleUnit branded)
        {
            int enemy = 1 - side;
            string name = ChiefKits.BrandName + ".";
            if (_parryOn) branded.BeatFlags = 0;
            if (_sides[enemy].FallBackTicks > 0)
            {
                Emit(EventFor(BattleEventKind.Land, chief, branded, 0, name + ChiefKits.Dodged));
                Earn(enemy, FightRules.DodgePoints, FightRules.DodgeReason);
                Answer(enemy, ChiefAnswer.Dodge, chief, FightRules.DodgePoints);
                return;
            }
            // Under the parry at the center a parry snuffs the burst, and a stale Hold soaks it on the front row
            // (BattleSimulation.Parry.cs).
            // From rules 21 the Brand is a blast: a Hold that is not a parry soaks nothing (BattleSimulation.Danger.cs).
            BeatMet met = AsBlast(_parryOn ? MeetBeat(enemy) : BeatMet.Open);
            BlastWent(side);
            if (met == BeatMet.Parry)
            {
                Emit(EventFor(BattleEventKind.Land, chief, branded, 0, name + ChiefKits.Parried));
                ParriedBeat(side, chief, ChiefKits.BrandName, ChiefKits.ParryNerve, FightRules.ParryPoints, true);
                return;
            }

            List<BattleUnit> hit = BrandedAndBeside(branded);
            BattleUnit warden = GuardingWarden(branded);
            bool medic = false;
            for (int i = 0; i < _units.Count; i++)
                if (_units[i].Alive && _units[i].Side == enemy && _units[i].Class == UnitClass.Medic) medic = true;
            var shares = new int[hit.Count];
            int total = 0;
            int prevented = 0;
            bool falls = false;
            for (int k = 0; k < hit.Count; k++)
            {
                int share = Math.Max(1, hit[k].MaxHp * ChiefBrandPercent / 100);
                if (met == BeatMet.Block && hit[k].Row == Row.Front) share = Math.Max(1, share * HoldSoak(enemy) / 100);
                BattleUnit takes = hit[k];
                if (k == 0 && warden != null)
                {
                    int halved = Math.Max(1, share * ChiefKits.BrandGuardPercent / 100);
                    prevented = share - halved;
                    share = halved;
                    takes = warden;
                }
                shares[k] = share;
                total += share;
                if (takes.Hp <= Armored(takes, share)) falls = true;
            }
            string how = warden != null ? ChiefKits.Guarded : medic && !falls ? ChiefKits.Healed : ChiefKits.Full;
            if (_parryOn && how == ChiefKits.Full)
            {
                if (met == BeatMet.Block) BlockedBeat(side, chief, ChiefKits.ParryNerve);
                else LandedBeat(side);
                if (Stopped) return;
            }
            Emit(EventFor(BattleEventKind.Land, chief, branded, total, name + how));
            for (int k = 0; k < hit.Count && !Stopped; k++)
            {
                if (k == 0 && warden != null)
                {
                    Emit(EventFor(BattleEventKind.Guard, warden, branded, prevented, string.Empty));
                    ApplyDamage(chief, side, warden, Armored(warden, shares[0]), ChiefKits.BrandDetail);
                }
                else ApplyDamage(chief, side, hit[k], Armored(hit[k], shares[k]), ChiefKits.BrandDetail);
            }
            if (Stopped) return;
            if (how == ChiefKits.Guarded)
            {
                Earn(enemy, ChiefKits.GuardPoints, ChiefKits.Name(ChiefAnswer.Guard));
                Answer(enemy, ChiefAnswer.Guard, chief, ChiefKits.GuardPoints);
            }
            else if (how == ChiefKits.Healed)
            {
                Earn(enemy, ChiefKits.HealPoints, ChiefKits.Name(ChiefAnswer.Heal));
                Answer(enemy, ChiefAnswer.Heal, chief, ChiefKits.HealPoints);
            }
            else Eaten(enemy, chief);
            CheckWipe();
        }

        // The branded fighter first, then whoever stands next to them in their row, by setup order, if they stand.
        private List<BattleUnit> BrandedAndBeside(BattleUnit branded)
        {
            var row = new List<BattleUnit>();
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (u.Side == branded.Side && u.Row == branded.Row && !u.Benched) row.Add(u);
            }
            int at = row.IndexOf(branded);
            var hit = new List<BattleUnit> { branded };
            if (at > 0 && row[at - 1].Alive) hit.Add(row[at - 1]);
            if (at >= 0 && at + 1 < row.Count && row[at + 1].Alive) hit.Add(row[at + 1]);
            return hit;
        }

        // A Warden covers an ally a chief has branded: the first of his side under a Brand, not himself.
        private BattleUnit BrandedAlly(BattleUnit warden)
        {
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (u.Alive && u.Side == warden.Side && u.BrandTicks > 0 && !ReferenceEquals(u, warden)) return u;
            }
            return null;
        }

        // ------------------------------------------------------------ Burning Ring (22 Oct 2026)

        // Every RingEveryTicks he spins his ring, when he is free: not winding up his Eclipse, aiming, or cleaving. Its lane is
        // the one its mark stands in: his forecast's, else one of yours picked from the fight's seeded stream.
        private void TryStartRing(int side)
        {
            SideState state = _sides[side];
            if (!HasMove(side, ChiefMove.BurningRing) || state.RingSpin > 0 || _tick < state.RingNext) return;
            BattleUnit chief = KitChief(side);
            BattleUnit target = chief != null && CanMove(chief) && state.Windup <= 0 && chief.AimTicks <= 0 && state.CleaveWindup <= 0
                ? RingMark(side) : null;
            if (target == null)
            {
                state.RingNext = _tick + 1;
                return;
            }
            state.RingSpin = ChiefKits.RingSpinTicks;
            state.RingTargetId = target.Id;
            state.RingNext = _tick + ChiefKits.RingEveryTicks;
            if (ForecastOn(side)) ClearForecast(side, ChiefMove.BurningRing);
            Emit(EventFor(BattleEventKind.Tell, chief, target, ChiefKits.RingSpinTicks, ChiefKits.RingName));
        }

        private BattleUnit RingMark(int side)
        {
            BattleUnit shown = ForecastMark(side, ChiefMove.BurningRing);
            if (shown != null && Markable(shown, 1 - side)) return shown;
            List<BattleUnit> pool = MarksOf(1 - side);
            return pool.Count == 0 ? null : pool[_rng.Next(pool.Count)];
        }

        // The ring spins on; once it has spun he throws it. It is gone if he is stunned, held or kneels first.
        private void AdvanceRing(int side)
        {
            SideState state = _sides[side];
            if (state.RingSpin <= 0) return;
            BattleUnit chief = KitChief(side);
            if (chief == null || !CanMove(chief))
            {
                ClearRing(state);
                return;
            }
            state.RingSpin--;
            if (state.RingSpin != 0) return;
            // Under the parry at the center the landing is its beat (BattleSimulation.Parry.cs).
            int hold = HoldBeat(side, chief, BeatSlot.Ring);
            if (hold > 0) state.RingSpin = hold;
            else ThrowRing(side, chief);
        }

        // The ring goes down its lane, even if its mark fell meanwhile (the dead keep their place). Fall Back gets everyone
        // clear; otherwise it hits whoever stands nearest that lane in each of your rows, never your leader while another of
        // yours stands, and you ate it. Nothing soaks it.
        private void ThrowRing(int side, BattleUnit chief)
        {
            SideState state = _sides[side];
            int enemy = 1 - side;
            BattleUnit mark = FindById(state.RingTargetId);
            ClearRing(state);
            string name = ChiefKits.RingName + ".";
            if (_sides[enemy].FallBackTicks > 0)
            {
                Emit(EventFor(BattleEventKind.Land, chief, mark, 0, name + ChiefKits.Dodged));
                Earn(enemy, FightRules.DodgePoints, FightRules.DodgeReason);
                FavourDodge(enemy, chief);
                Answer(enemy, ChiefAnswer.Dodge, chief, FightRules.DodgePoints);
                return;
            }
            // Under the parry at the center a parry knocks it down, and a stale Hold soaks it on the front row, which nothing
            // did before (BattleSimulation.Parry.cs).
            // From rules 21 the Burning Ring is a blast: a Hold that is not a parry soaks nothing (BattleSimulation.Danger.cs).
            BeatMet met = AsBlast(_parryOn ? MeetBeat(enemy) : BeatMet.Open);
            BlastWent(side);
            if (met == BeatMet.Parry)
            {
                Emit(EventFor(BattleEventKind.Land, chief, mark, 0, name + ChiefKits.Parried));
                ParriedBeat(side, chief, ChiefKits.RingName, ChiefKits.ParryNerve, FightRules.ParryPoints, true);
                return;
            }
            int lane = mark != null ? LanePosition(mark) : LanePosition(chief);
            var hit = new List<BattleUnit>();
            BattleUnit front = NearestInLane(enemy, Row.Front, lane);
            BattleUnit back = NearestInLane(enemy, Row.Back, lane);
            if (front != null) hit.Add(front);
            if (back != null) hit.Add(back);
            if (hit.Count == 0) return;
            var shares = new int[hit.Count];
            int total = 0;
            for (int k = 0; k < hit.Count; k++)
            {
                shares[k] = Math.Max(1, hit[k].MaxHp * ChiefRingPercent / 100);
                if (met == BeatMet.Block && hit[k].Row == Row.Front) shares[k] = Math.Max(1, shares[k] * HoldSoak(enemy) / 100);
                total += shares[k];
            }
            if (_parryOn)
            {
                if (met == BeatMet.Block) BlockedBeat(side, chief, ChiefKits.ParryNerve);
                else LandedBeat(side);
                if (Stopped) return;
            }
            Emit(EventFor(BattleEventKind.Land, chief, mark, total, name + (met == BeatMet.Block ? ChiefKits.Soaked : ChiefKits.Full)));
            for (int k = 0; k < hit.Count && !Stopped; k++)
                ApplyDamage(chief, side, hit[k], Armored(hit[k], shares[k]), ChiefKits.RingDetail);
            if (!Stopped && met != BeatMet.Block) Eaten(enemy, chief);
            CheckWipe();
        }

        // A Marksman of the side focusing him landed a shot while his ring spun: it is knocked out of his hand, and that
        // answers it as breaking a cast does.
        private void KnockRing(int side, BattleUnit chief, BattleUnit marksman)
        {
            int enemy = 1 - side;
            SideState state = _sides[side];
            BattleUnit mark = FindById(state.RingTargetId);
            ClearRing(state);
            Emit(new BattleEvent(_tick, BattleEventKind.Broken, enemy, marksman.Id, (int)marksman.Class, chief.Id, (int)chief.Class, 0,
                ChiefKits.RingDetail));
            Emit(EventFor(BattleEventKind.Land, chief, mark, 0, ChiefKits.RingName + "." + ChiefKits.Broken));
            Answered(enemy, ChiefAnswer.Break, chief, FightRules.InterruptPoints, FightRules.InterruptReason);
        }

        private static void ClearRing(SideState state)
        {
            state.RingSpin = 0;
            state.RingTargetId = string.Empty;
            state.Beats[(int)BeatSlot.Ring] = 0;
        }

        // ------------------------------------------------------------ Sun Mantle (22 Oct 2026)

        // Every MantleEveryTicks fire wraps him while he can move. It needs nothing of his hands, so it comes over his other
        // moves.
        private void TryStartMantle(int side)
        {
            SideState state = _sides[side];
            if (!HasMove(side, ChiefMove.SunMantle) || state.MantleTicks > 0 || state.MantleFlare > 0 || _tick < state.MantleNext) return;
            BattleUnit chief = KitChief(side);
            if (chief == null || !CanMove(chief))
            {
                state.MantleNext = _tick + 1;
                return;
            }
            ClearMantle(state);
            state.MantleNext = _tick + ChiefKits.MantleEveryTicks;
            if (ForecastOn(side)) ClearForecast(side, ChiefMove.SunMantle);
            // Under the parry at the center the fire flares ParryRules.TellTicks after its tell, and the flare is its beat: the
            // Tell's amount is the ticks until it, and a parry snuffs it (BattleSimulation.Parry.cs).
            if (_parryOn)
            {
                state.MantleFlare = ParryRules.TellTicks;
                Emit(EventFor(BattleEventKind.Tell, chief, null, ParryRules.TellTicks, ChiefKits.MantleName));
                return;
            }
            state.MantleTicks = ChiefKits.MantleTicks;
            Emit(EventFor(BattleEventKind.Tell, chief, null, ChiefKits.MantleTicks, ChiefKits.MantleName));
        }

        // The flare's beat: parried, the fire is snuffed; otherwise it lights and burns as it always has. Nothing is hit at the
        // flare, so a stale Hold costs nothing here.
        private void FlareMantle(int side, BattleUnit chief)
        {
            SideState state = _sides[side];
            int enemy = 1 - side;
            ClearBeat(side, BeatSlot.Mantle);
            if (MeetBeat(enemy) == BeatMet.Parry)
            {
                Emit(EventFor(BattleEventKind.Land, chief, null, 0, ChiefKits.MantleName + "." + ChiefKits.Parried));
                ParriedBeat(side, chief, ChiefKits.MantleName, ChiefKits.ParryNerve, FightRules.ParryPoints, true);
                return;
            }
            if (MantleBroken(side, chief)) return;
            state.MantleTicks = ChiefKits.MantleTicks;
        }

        // The mantle burns on, noting whether the side facing him falls back, or focuses one of his band, while it burns; when
        // it ends, how they answered it.
        private void AdvanceMantle(int side)
        {
            SideState state = _sides[side];
            if (state.MantleFlare > 0)
            {
                BattleUnit flaring = KitChief(side);
                if (flaring == null || !CanMove(flaring))
                {
                    ClearMantle(state);
                    return;
                }
                state.MantleFlare--;
                if (state.MantleFlare > 0) return;
                int hold = HoldBeat(side, flaring, BeatSlot.Mantle);
                if (hold > 0) state.MantleFlare = hold;
                else FlareMantle(side, flaring);
                return;
            }
            if (state.MantleTicks <= 0) return;
            BattleUnit chief = KitChief(side);
            if (chief == null)
            {
                ClearMantle(state);
                return;
            }
            SideState theirs = _sides[1 - side];
            if (theirs.FallBackTicks > 0) state.MantleFellBack = true;
            BattleUnit focus = theirs.FocusTicks > 0 ? FindById(theirs.FocusTargetId) : null;
            if (focus != null && focus.Alive && focus.Side == side && !ReferenceEquals(focus, chief)) state.MantleSwitched = true;
            state.MantleTicks--;
            if (state.MantleTicks == 0) EndMantle(side, chief);
        }

        // It burned someone: you ate it. Nobody: Fall Back while it burned dodged it, else Focus Fire on his band switched
        // away from it; with neither, nothing came of it.
        private void EndMantle(int side, BattleUnit chief)
        {
            SideState state = _sides[side];
            int enemy = 1 - side;
            int burned = state.MantleBurned;
            bool fellBack = state.MantleFellBack, switched = state.MantleSwitched;
            ClearMantle(state);
            string name = ChiefKits.MantleName + ".";
            if (burned > 0)
            {
                Emit(EventFor(BattleEventKind.Land, chief, null, burned, name + ChiefKits.Full));
                Eaten(enemy, chief);
            }
            else if (fellBack)
            {
                Emit(EventFor(BattleEventKind.Land, chief, null, 0, name + ChiefKits.Dodged));
                Answered(enemy, ChiefAnswer.Dodge, chief, FightRules.DodgePoints, FightRules.DodgeReason);
            }
            else if (switched)
            {
                Emit(EventFor(BattleEventKind.Land, chief, null, 0, name + ChiefKits.Switched));
                Answered(enemy, ChiefAnswer.Switch, chief, ChiefKits.SwitchPoints, ChiefKits.Name(ChiefAnswer.Switch));
            }
            else Emit(EventFor(BattleEventKind.Land, chief, null, 0, name + ChiefKits.Clear));
        }

        private static void ClearMantle(SideState state)
        {
            state.MantleFlare = 0;
            state.Beats[(int)BeatSlot.Mantle] = 0;
            state.MantleTicks = 0;
            state.MantleBurned = 0;
            state.MantleFellBack = false;
            state.MantleSwitched = false;
        }

        // A blow from the other side landed on a chief of a kit: while his ring spins, a Marksman's from a side focusing him
        // knocks it out of his hand; while his Sun Mantle burns, it burns whoever struck it.
        private void KitStruck(BattleUnit attacker, BattleUnit target)
        {
            if (Stopped || !target.Alive || target.Role != UnitRole.Chief || attacker.Side == target.Side || !KitOn(target.Side)) return;
            int side = target.Side;
            SideState state = _sides[side];
            if (state.RingSpin > 0 && attacker.Class == UnitClass.Marksman && IsFocusing(attacker.Side, target))
                KnockRing(side, target, attacker);
            if (Stopped || state.MantleTicks <= 0 || !attacker.Alive) return;
            int burn = Math.Max(1, attacker.MaxHp * ChiefKits.MantleBurnPercent / 100);
            state.MantleBurned += burn;
            ApplyDamage(target, side, attacker, Armored(attacker, burn), ChiefKits.MantleDetail);
            CheckWipe();
        }

        // ------------------------------------------------------------ his forecast (22 Oct 2026)

        // When each of his moves is next due.
        private long DueTick(int side, ChiefMove move)
        {
            SideState state = _sides[side];
            switch (move)
            {
                case ChiefMove.Cleave: return state.CleaveNext;
                case ChiefMove.Brand: return state.BrandNext;
                case ChiefMove.Eclipse: return state.NextAutoEclipse;
                case ChiefMove.BurningRing: return state.RingNext;
                case ChiefMove.SunMantle: return state.MantleNext;
                default: return long.MaxValue;
            }
        }

        private static int ForecastSlot(ChiefMove move)
        {
            for (int k = 0; k < ForecastSlots; k++)
                if (ForecastOrder[k] == move) return k;
            return -1;
        }

        // Whom his forecast showed a move falling on, or null.
        private BattleUnit ForecastMark(int side, ChiefMove move)
        {
            if (!ForecastOn(side)) return null;
            int slot = ForecastSlot(move);
            SideState state = _sides[side];
            return slot >= 0 && state.ForeShown[slot] ? FindById(state.ForeTarget[slot]) : null;
        }

        private void ClearForecast(int side, ChiefMove move)
        {
            int slot = ForecastSlot(move);
            if (slot < 0) return;
            SideState state = _sides[side];
            state.ForeShown[slot] = false;
            state.ForeTarget[slot] = string.Empty;
            state.ForeTarget2[slot] = string.Empty;
            state.ForeAmount[slot] = 0;
        }

        // Each move he took shows once it is due within ForecastTicks, with its mark locked in and how hard it comes (see
        // ChiefForecast): a Cleave on whoever stands across from him; a Brand's marks, two under Blood Sun, and a ring's lane
        // picked from the fight's seeded stream, in ForecastOrder; the Eclipse at its percent, a fifth more under Blood Sun;
        // the Sun Mantle's burn. One with nobody to mark waits to show.
        private void Forecast(int side)
        {
            if (!ForecastOn(side)) return;
            BattleUnit chief = KitChief(side);
            if (chief == null || chief.Yielded) return;
            SideState state = _sides[side];
            int enemy = 1 - side;
            for (int k = 0; k < ForecastSlots; k++)
            {
                ChiefMove move = ForecastOrder[k];
                if (state.ForeShown[k] || !HasMove(side, move) || DueTick(side, move) > _tick + ChiefKits.ForecastTicks) continue;
                if (move == ChiefMove.Eclipse && (_rules[side].AutoEclipseTicks <= 0 || _rules[side].EclipseGrade <= 0)) continue;
                BattleUnit first = null, second = null;
                int amount;
                switch (move)
                {
                    case ChiefMove.Cleave:
                        first = AcrossFrom(chief);
                        amount = Math.Max(1, chief.Power * HeavyRules.DamagePercent / 100);
                        break;
                    case ChiefMove.Brand:
                        List<BattleUnit> pool = Brandable(side);
                        if (pool.Count == 0) continue;
                        int pick = _rng.Next(pool.Count);
                        first = pool[pick];
                        pool.RemoveAt(pick);
                        if (BloodSun(side) && pool.Count > 0) second = pool[_rng.Next(pool.Count)];
                        amount = Math.Max(1, first.MaxHp * ChiefBrandPercent / 100);
                        break;
                    case ChiefMove.Eclipse:
                        amount = Moves.EclipsePercent(_rules[side].EclipseGrade);
                        if (BloodSun(side))
                            for (int i = 0; i <= state.SunEclipses; i++) amount = amount * ChiefKits.BloodSunEclipsePercent / 100;
                        break;
                    case ChiefMove.BurningRing:
                        List<BattleUnit> lanes = MarksOf(enemy);
                        if (lanes.Count == 0) continue;
                        first = lanes[_rng.Next(lanes.Count)];
                        amount = Math.Max(1, first.MaxHp * ChiefRingPercent / 100);
                        break;
                    default:
                        amount = ChiefKits.MantleBurnPercent;
                        break;
                }
                if (move == ChiefMove.Cleave && first == null) continue;
                state.ForeShown[k] = true;
                state.ForeTarget[k] = first == null ? string.Empty : first.Id;
                state.ForeTarget2[k] = second == null ? string.Empty : second.Id;
                state.ForeAmount[k] = amount;
                Emit(EventFor(BattleEventKind.Forecast, chief, first, amount, ChiefKits.Name(move)));
                if (second != null)
                    Emit(EventFor(BattleEventKind.Forecast, chief, second, Math.Max(1, second.MaxHp * ChiefBrandPercent / 100),
                        ChiefKits.Name(move)));
            }
        }

        // ------------------------------------------------------------ Eclipse

        // Each Eclipse after Blood Sun hits BloodSunEclipsePercent as hard as the one before.
        private void RampEclipse(int side, SideState state)
        {
            if (!BloodSun(side)) return;
            state.SunEclipses++;
            for (int i = 0; i < state.SunEclipses; i++) state.WindupPercent = state.WindupPercent * ChiefKits.BloodSunEclipsePercent / 100;
        }

        // Your 5th button goes off twice as hard while your Glory is S.
        private int GloryCast(int side)
        {
            return GloryOn(side) && GloryRankOf(side) == GloryRank.S ? Moves.EmpoweredPercent : 100;
        }

        // His Eclipse was parried. Two Cleaves parried in a row before it make it Sunbreaker: your Warden throws it back at him.
        // The throw comes before the parry's chunk of his nerve, so the chain shows even when that chunk breaks him; if his
        // nerve already broke on the parry itself, the throw shows and lands on a kneeling man, which does nothing.
        private void KitEclipseParried(int side, BattleUnit caster, int percent, int nerve = -1)
        {
            int enemy = 1 - side;
            SideState theirs = _sides[enemy];
            bool chain = theirs.Chain >= ChiefKits.ChainCleaves;
            theirs.Chain = 0;
            SayOnParry(caster);
            BattleUnit warden = FirstLivingWarden(enemy);
            if (chain && caster.Alive && warden != null)
            {
                int damage = Math.Max(1, caster.MaxHp * percent / 100);
                Emit(new BattleEvent(_tick, BattleEventKind.Combo, enemy, warden.Id, (int)warden.Class, caster.Id, (int)caster.Class,
                    damage, ChiefKits.Sunbreaker));
                Earn(enemy, FightRules.ComboPoints, FightRules.ComboReason);
                // Stunned before it lands, so a Blood Sun the throw brings on does not start his Eclipse again, and a shot he
                // was aiming is gone.
                if (!Stopped)
                {
                    caster.Statuses.Apply(StatusKind.Stun, ChiefKits.SunbreakerStunTicks);
                    caster.AimTicks = 0;
                    caster.AimTargetId = string.Empty;
                    ClearCleave(_sides[side]);
                }
                ApplyDamage(warden, enemy, caster, damage, ChiefKits.SunbreakerDetail, true);
                if (!Stopped) DrainNerve(side, ChiefKits.SunbreakerNerve, enemy);
            }
            Answer(enemy, ChiefAnswer.Parry, caster, _squadOn ? ParryRules.SquadParryPoints : FightRules.ParryPoints, nerve);
        }

        // His Eclipse went off unparried, before its hits: dodged, soaked, or eaten in full. The Thundercoast chief's Tide goes
        // the same way, but leaves the count toward Turn of the Tide to the answer (BattleSimulation.Thundercoast.cs).
        private void KitEclipseLanded(int side, BattleUnit caster, bool dodged, bool soaked)
        {
            int enemy = 1 - side;
            if (!_tideOn || side != _tideSide) _sides[enemy].Chain = 0;
            if (dodged)
            {
                Answer(enemy, ChiefAnswer.Dodge, caster, FightRules.DodgePoints);
                return;
            }
            if (soaked) return;
            Say(caster, ChiefKits.EclipseLine);
            Eaten(enemy, caster);
        }

        // Focus Fire broke his Eclipse.
        private void KitEclipseBroken(BattleUnit caster, int interrupter)
        {
            _sides[interrupter].Chain = 0;
            Answer(interrupter, ChiefAnswer.Break, caster, FightRules.InterruptPoints);
        }

        // ------------------------------------------------------------ Glory and his nerve

        // A side answered one of his moves: its Glory climbs (a different answer from the last by half again, the same one
        // twice running by half as much), Glory adds to the points it earned (half again at A, double at S), and the answer
        // takes its chunk of his nerve, which can bring the kneel. Under the parry at the center a parry in a row is not
        // halved, the parry's chunk comes chained (nerve, from BattleSimulation.Parry.cs) and every other answer takes a
        // quarter of a parry's (AnswerNerve). Under the squad first a parry in a row is halved again, and every other answer
        // takes half a parry's.
        private void Answer(int side, ChiefAnswer answer, BattleUnit chief, int earned, int nerve = -1)
        {
            if (!GloryOn(side) || _finished) return;
            SideState state = _sides[side];
            // Under the raid chief your parry earns no points, so its Glory adds none either (BattleSimulation.Raid.cs).
            if (_raidOn && side == 0 && answer == ChiefAnswer.Parry) earned = 0;
            int bonus = ChiefKits.GloryBonus(ChiefKits.Rank(state.Glory), earned);
            if (bonus > 0) Earn(side, bonus, ChiefKits.GloryReason);
            int percent = state.LastAnswer == (int)ChiefAnswer.None ? 100
                : state.LastAnswer == (int)answer ? (_parryOn && !_squadOn && answer == ChiefAnswer.Parry ? 100 : ChiefKits.RepeatPercent)
                : ChiefKits.MixedPercent;
            state.LastAnswer = (int)answer;
            state.Glory = Math.Min(ChiefKits.GloryMax, state.Glory + ChiefKits.Glory(answer) * percent / 100);
            Emit(new BattleEvent(_tick, BattleEventKind.Glory, side, string.Empty, -1, chief.Id, (int)chief.Class, state.Glory,
                ChiefKits.Name(answer)));
            DrainNerve(chief.Side, nerve >= 0 ? nerve : AnswerNerve(answer), side);
            // An answer to the Thundercoast chief at high water counts toward Turn of the Tide (BattleSimulation.Thundercoast.cs).
            if (_tideOn && !Stopped && chief.Side == _tideSide) TideAnswered(side, chief);
        }

        // A side answered one of his moves and earned for it: the points first, then its Glory and his nerve.
        private void Answered(int side, ChiefAnswer answer, BattleUnit chief, int points, string reason)
        {
            Earn(side, points, reason);
            Answer(side, answer, chief, points);
        }

        // A side ate one of his big moves in full: its Glory drops a rank, and against the Thundercoast chief its count toward
        // Turn of the Tide starts over.
        private void Eaten(int side, BattleUnit chief)
        {
            // Under the parry at the center your parry chain starts over too (BattleSimulation.Parry.cs).
            if (_parryOn) _sides[side].ParryChain = 0;
            if (!GloryOn(side) || _finished) return;
            SideState state = _sides[side];
            if (_tideOn && chief.Side == _tideSide) state.Chain = 0;
            state.Glory = Math.Max(0, state.Glory - ChiefKits.GloryPerRank);
            Emit(new BattleEvent(_tick, BattleEventKind.Glory, side, string.Empty, -1, chief.Id, (int)chief.Class, state.Glory,
                ChiefKits.EatenDetail));
        }

        // ------------------------------------------------------------ his lines

        // He speaks at a big moment. The event comes even while the line has no words yet (Skullpeak's are Jacob's to write),
        // so the screen can hold the moment and the words can come later without changing a fight.
        private void Say(BattleUnit chief, string key)
        {
            if (KitOn(chief.Side) && key.Length > 0) Emit(EventFor(BattleEventKind.Line, chief, null, 0, key));
        }

        // "Lucky.", the first time a fight parries one of his moves.
        private void SayOnParry(BattleUnit chief)
        {
            SideState state = _sides[chief.Side];
            if (state.SaidParry) return;
            state.SaidParry = true;
            Say(chief, ChiefKits.ParryLine);
        }

        // ------------------------------------------------------------ the kneel and the verdict

        // His side's nerve ran out: a chief who still stands kneels instead of his side breaking. True when he kneels (or
        // already knelt). The hunt captain never kneels: the hunters never join, so they break (BattleSimulation.Hunt.cs).
        private bool TryKneel(int side)
        {
            if (!_kitsOn || Kit(side) == ChiefKit.Hunt) return false;
            BattleUnit chief = KitChief(side);
            if (chief == null) return false;
            if (!chief.Yielded) Kneel(chief);
            return true;
        }

        // He kneels and stays down: his cast, his band's swing, his Cleave and his Brands are gone, his Wardens step back, and
        // nothing moves until the verdict.
        private void Kneel(BattleUnit chief)
        {
            chief.Yielded = true;
            chief.AimTicks = 0;
            chief.AimTargetId = string.Empty;
            _halted = true;
            SideState state = _sides[chief.Side];
            ClearWindup(state);
            ClearHeavy(state);
            ClearCleave(state);
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                u.BrandTicks = 0;
                if (_parryOn && u.Side != chief.Side) u.BeatFlags = 0;
                if (u.Side == chief.Side && string.Equals(u.GuardTargetId, chief.Id, StringComparison.Ordinal)) u.GuardTargetId = string.Empty;
            }
            // And from 22 Oct his ring, his mantle and his forecast, her chain, her aims, her march and her Medic's jab.
            if (_kitsTwoOn) ClearKitTwo(chief.Side);
            Emit(EventFor(BattleEventKind.Yield, chief, null, 0, ChiefKits.KneelDetail));
            Say(chief, ChiefKits.KneelLine);
        }

        private void ClearKitTwo(int side)
        {
            SideState state = _sides[side];
            ClearRing(state);
            ClearMantle(state);
            for (int k = 0; k < ForecastSlots; k++) ClearForecast(side, ForecastOrder[k]);
            ClearHook(state);
            state.RifleTicks = 0;
            state.RifleTargetId = string.Empty;
            state.RifleShooters = 0;
            ClearAdvance(state);
            state.JabTicks = 0;
            state.JabMedicId = string.Empty;
            state.OverclockTicks = 0;
            for (int i = 0; i < _units.Count; i++)
            {
                _units[i].HookTicks = 0;
                _units[i].RifleTicks = 0;
            }
            // And later on 22 Oct the Stoneveil chief's rite and the Thundercoast chief's moves.
            if (_riteOn) ClearRite(side);
            if (_tideOn) ClearTide(side);
        }

        // The 22 Oct additions' state for the checksum, written only in fights that have them.
        private static void AppendKitTwo(SnapshotBuilder b, SideState state)
        {
            b.Field("ring", state.RingSpin);
            b.Field("ringTarget", state.RingTargetId);
            b.Field("ringNext", state.RingNext);
            b.Field("mantle", state.MantleTicks);
            b.Field("mantleNext", state.MantleNext);
            b.Field("mantleBurned", state.MantleBurned);
            b.Field("mantleFellBack", state.MantleFellBack);
            b.Field("mantleSwitched", state.MantleSwitched);
            for (int k = 0; k < ForecastSlots; k++)
            {
                string key = "fore" + k.ToString(CultureInfo.InvariantCulture) + ".";
                b.Field(key + "shown", state.ForeShown[k]);
                b.Field(key + "target", state.ForeTarget[k]);
                b.Field(key + "target2", state.ForeTarget2[k]);
                b.Field(key + "amount", state.ForeAmount[k]);
            }
            b.Field("wallPercent", state.WallPercent);
            b.Field("wallUp", state.WallUpTick);
            b.Field("lockSnared", state.LockSnared);
            b.Field("hookTell", state.HookTell);
            b.Field("hookTarget", state.HookTargetId);
            b.Field("hookNext", state.HookNext);
            b.Field("hookWaited", state.HookWaited);
            b.Field("hookGone", state.HookGone);
            b.Field("rifle", state.RifleTicks);
            b.Field("rifleTarget", state.RifleTargetId);
            b.Field("rifleNext", state.RifleNext);
            b.Field("rifleShooters", state.RifleShooters);
            b.Field("advance", state.AdvanceTicks);
            b.Field("advanceNext", state.AdvanceNext);
            b.Field("advanceFull", state.AdvanceFull);
            b.Field("advanceBraced", state.AdvanceBraced);
            b.Field("advanceGave", state.AdvanceGave);
            b.Field("jab", state.JabTicks);
            b.Field("jabMedic", state.JabMedicId);
            b.Field("overclock", state.OverclockTicks);
            b.Field("overclockNext", state.OverclockNext);
            b.Field("overclockSnared", state.OverclockSnared);
            b.Field("overclockHeld", state.OverclockHeld);
            b.Field("lockdown", state.Lockdown);
            b.Field("open", state.OpenTicks);
            b.Field("rebuild", state.RebuildTicks);
            b.Field("shieldbearer", state.ShieldbearerId);
            b.Field("spotter", state.SpotterId);
        }

        // The other side's verdict on a kneeling chief: Hold spares him, Focus Fire on him executes him. Both are free.
        private bool TryVerdict(int side, Directive directive, string targetId, string actorId, out string failure)
        {
            failure = string.Empty;
            BattleUnit chief = KneelingChief;
            if (chief == null || side == chief.Side)
            {
                failure = ChiefKits.KneelFailure;
                return false;
            }
            if (directive == Directive.Hold)
            {
                Spare(side, chief, actorId);
                return true;
            }
            if (directive == Directive.FocusFire && string.Equals(targetId, chief.Id, StringComparison.Ordinal))
            {
                Execute(side, chief, actorId);
                return true;
            }
            failure = ChiefKits.KneelFailure;
            return false;
        }

        // Spared: his side breaks and he lives, so he and his tribe join the side that spared him.
        private void Spare(int side, BattleUnit chief, string actorId)
        {
            Emit(new BattleEvent(_tick, BattleEventKind.Spare, side, actorId, -1, chief.Id, (int)chief.Class, 0, string.Empty));
            _halted = false;
            BreakSide(chief.Side, BreakDetail);
        }

        // Executed: he falls where he kneels, and his side breaks with him.
        private void Execute(int side, BattleUnit chief, string actorId)
        {
            int health = chief.Hp;
            Emit(new BattleEvent(_tick, BattleEventKind.Execute, side, actorId, -1, chief.Id, (int)chief.Class, health, string.Empty));
            _halted = false;
            chief.Hp = 0;
            chief.Alive = false;
            chief.GuardTargetId = string.Empty;
            Emit(new BattleEvent(_tick, BattleEventKind.Hit, side, string.Empty, -1, chief.Id, (int)chief.Class, health,
                ChiefKits.ExecuteDetail));
            Emit(new BattleEvent(_tick, BattleEventKind.Kill, side, string.Empty, -1, chief.Id, (int)chief.Class, 0,
                ChiefKits.ExecuteDetail));
            LeaderDown(chief.Side);
        }

        // The fight's clock ran out while he knelt: he is spared.
        private void TimeSpare()
        {
            BattleUnit chief = KneelingChief;
            if (chief != null) Spare(1 - chief.Side, chief, ChiefKits.TimeActor);
        }
    }
}
