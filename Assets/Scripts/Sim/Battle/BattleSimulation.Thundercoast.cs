using System;
using System.Collections.Generic;

namespace KindredSiege.Sim.Battle
{
    /// The Thundercoast chief's kit, the tide's clock (Jacob, 22 Oct 2026, later: "yes, lock both kits as written", then "go,
    /// build the Stoneveil and Thundercoast chief kits in the sim and hand the API to Codex"; DESIGN_LOCKS "The Stoneveil and
    /// Thundercoast chiefs"). The sea tribe's water (FloodRules) already rises in their fights; with him it rises and falls on
    /// a clock you can see, like a tide table: low water, 1, 2, high water, 2, 1 and round again (ChiefKits.TideSteps), each
    /// step FloodRules.StepTicks, from low water at the start. At high water his fishmen are at their strongest, as the flood
    /// always made them; at low water they are stranded, slower and lighter. His Tide, which hits harder the higher the water;
    /// his Sluice Gate, Riptide, Tidal Bore and Storm Surge; his Spring Tide at half health; Turn of the Tide, his hidden chain.
    /// His tactical piece, moves that hit places, not people, is his moves themselves. ChiefKits holds the numbers. Only fights
    /// where a side's kit is his come here. His Tidal Bore picks which way it runs from the fight's seeded stream; nothing else
    /// here draws.
    ///
    /// Claude's calls while building, Jacob's to change: your answers bend the clock back by stopping his moves (a jammed or
    /// slammed gate never raises the water, a broken horn never brings the surge), and only Turn of the Tide drains it; his
    /// Sluice Gate's extra level lasts until the next low water, and the clock holds 10 s, so that low water comes 10 s late;
    /// stranded at low water, his side waits a fifth longer between actions and its blows land a fifth lighter; his Tide hits a
    /// quarter as hard at low water, half at 1, three quarters at 2 and in full at high water, nerve and all; his engineer is
    /// his band's first free Trapper and does nothing else while he cranks; the Riptide pulls everyone of yours a move can fall
    /// on (never your leader while another of yours stands) the first moment you fall back with no Hold up, hurts each a tenth
    /// and holds them 2 s, and is spent; a Hold at any time during it answers it as a Brace; the Tidal Bore runs from one end
    /// of your rows to the other, reaching both rows at each lane position at once, front row first, so it reaches the near
    /// end on its first tick, and the first Warden it reaches breaks it, taking it less his Guard, which answers it only when
    /// nobody was hit in full before him; a fighter it reaches while you fall back is clear, one it reaches while you hold
    /// takes half and stays up; how it went is judged when it ends: any full hit and you ate it; a Hold at any time of the
    /// high water answers the Storm Surge as a Brace, and halves the nerve the water takes only while it runs; his horn and
    /// his Tide never wind up together; Spring Tide's clock turns twice as fast; Turn of the Tide counts answers, of any kind,
    /// given while the water stands at its highest, starts over on an answer below it, a move you eat, or a Sluice Gate that
    /// raises the water, and a Storm Surge still standing ends with it.
    public sealed partial class BattleSimulation
    {
        // A side's kit is the Thundercoast chief's, and that side, -1 without (set in the constructor).
        private readonly bool _tideOn;
        private readonly int _tideSide = -1;

        // ------------------------------------------------------------ questions the screen asks

        /// True when this fight runs the Thundercoast chief's tide, and his side, or -1. FloodLevel follows the tide.
        public bool TideOn => _tideOn;

        public int TideSide => _tideSide;

        /// The tide table: ticks until the clock next stands at high water, and at low water, or 0 while it does. A hold of the
        /// clock counts; a Storm Surge or a drain over the top of it does not.
        public int TideTicksToHigh => _tideOn ? TicksToStep(ChiefKits.HighStep) : 0;

        public int TideTicksToLow => _tideOn ? TicksToStep(0) : 0;

        /// Ticks the tide's clock still holds after his Sluice Gates, and the levels they added until the next low water.
        public int TideHoldTicks => _tideOn ? _sides[_tideSide].TideHold : 0;

        public int TideGateLevels => _tideOn ? _sides[_tideSide].TideGate : 0;

        /// Ticks the water still stands at its highest in his Storm Surge, and ticks it still stands drained by your Turn of the
        /// Tide. 0 otherwise.
        public int SurgeTicksLeft => _tideOn ? _sides[_tideSide].SurgeTicks : 0;

        public int DrainTicksLeft => _tideOn ? _sides[_tideSide].DrainTicks : 0;

        /// True while this side is his and the water stands at 0: his fishmen are stranded.
        public bool Stranded(int side)
        {
            return _tideOn && side == _tideSide && FloodLevel == 0;
        }

        /// True from his half health, his Spring Tide: the clock turns faster, the water never drops below the first level, and
        /// his Tide drowns both your rows.
        public bool SpringTide(int side)
        {
            BattleUnit chief = KitChief(side);
            return Kit(side) == ChiefKit.Thundercoast && chief != null && chief.PhaseDone;
        }

        /// Ticks his engineer still cranks his Sluice Gate, or 0, and who.
        public int SluiceTicksLeft(int side)
        {
            return Kit(side) == ChiefKit.Thundercoast ? _sides[side].CrankTicks : 0;
        }

        public string SluiceEngineerId(int side)
        {
            return SluiceTicksLeft(side) > 0 ? _sides[side].EngineerId : string.Empty;
        }

        /// Ticks his Riptide still runs, or 0.
        public int RiptideTicksLeft(int side)
        {
            return Kit(side) == ChiefKit.Thundercoast ? _sides[side].RipTicks : 0;
        }

        /// His Tidal Bore: ticks it still runs, or 0; true when it runs from the right end of your rows (lane 1000) to the left;
        /// and how far across it has come, 0 to 1000 from where it started.
        public int BoreTicksLeft(int side)
        {
            return Kit(side) == ChiefKit.Thundercoast ? _sides[side].BoreTicks : 0;
        }

        public bool BoreFromRight(int side)
        {
            return BoreTicksLeft(side) > 0 && _sides[side].BoreFromRight;
        }

        public int BoreFront(int side)
        {
            return BoreTicksLeft(side) > 0 ? Math.Max(0, _sides[side].BoreFront) : 0;
        }

        /// Ticks his Storm Surge's horn still sounds, or 0.
        public int HornTicksLeft(int side)
        {
            return Kit(side) == ChiefKit.Thundercoast ? _sides[side].HornTicks : 0;
        }

        // ------------------------------------------------------------ the tide

        // The water the clock alone gives at this point of its cycle: 0, 1, 2, 3, 2, 1.
        private static int ClockLevel(int phase)
        {
            int step = phase / FloodRules.StepTicks;
            return step <= ChiefKits.HighStep ? step : ChiefKits.TideSteps - step;
        }

        // The water now: drained by your Turn of the Tide, at its highest in his Storm Surge, else the clock and his Sluice
        // Gates, never under Spring Tide's floor.
        private int TideLevel()
        {
            SideState state = _sides[_tideSide];
            if (state.DrainTicks > 0) return 0;
            if (state.SurgeTicks > 0) return FloodRules.MaxLevel;
            int level = Math.Min(FloodRules.MaxLevel, ClockLevel(state.TidePhase) + state.TideGate);
            return SpringTide(_tideSide) ? Math.Max(ChiefKits.SpringFloor, level) : level;
        }

        // How hard his Tide hits at the water's level now, in percent of its full hit.
        private int TideScale()
        {
            return (FloodLevel + 1) * ChiefKits.TidePercentPerLevel;
        }

        // Ticks until the clock next starts this step, at its speed, after any hold; 0 while it stands in that step.
        private int TicksToStep(int step)
        {
            SideState state = _sides[_tideSide];
            if (state.TidePhase / FloodRules.StepTicks == step) return 0;
            int ahead = step * FloodRules.StepTicks - state.TidePhase;
            if (ahead <= 0) ahead += ChiefKits.TideCycleTicks;
            int speed = SpringTide(_tideSide) ? ChiefKits.SpringSpeed : 1;
            return state.TideHold + (ahead + speed - 1) / speed;
        }

        // The water's level changed: a Tide event says why.
        private void TideChanged(string why)
        {
            SideState state = _sides[_tideSide];
            int level = TideLevel();
            if (level == state.TideShown) return;
            state.TideShown = level;
            BattleUnit chief = KitChief(_tideSide);
            Emit(new BattleEvent(_tick, BattleEventKind.Tide, _tideSide, chief == null ? string.Empty : chief.Id,
                chief == null ? -1 : (int)chief.Class, string.Empty, -1, level, why));
        }

        // The clock turns, unless a Sluice Gate holds it, and at low water his Sluice Gates' levels drain away. Your drain
        // counts down here too.
        private void TurnClock(int side)
        {
            SideState state = _sides[side];
            if (state.DrainTicks > 0 && --state.DrainTicks == 0) TideChanged(ChiefKits.TideDrain);
            if (state.TideHold > 0) state.TideHold--;
            else
            {
                int before = ClockLevel(state.TidePhase);
                state.TidePhase = (state.TidePhase + (SpringTide(side) ? ChiefKits.SpringSpeed : 1)) % ChiefKits.TideCycleTicks;
                if (ClockLevel(state.TidePhase) == 0 && before != 0) state.TideGate = 0;
            }
            TideChanged(ChiefKits.TideClock);
        }

        // ------------------------------------------------------------ setup and clocks

        // His moves' first clocks. His Tide is his spell, on SideRules.AutoEclipseTicks.
        private void StartTideClocks(int side)
        {
            if (side != _tideSide) return;
            SideState state = _sides[side];
            if (HasMove(side, ChiefMove.SluiceGate)) state.SluiceNext = ChiefKits.SluiceEveryTicks;
            if (HasMove(side, ChiefMove.Riptide)) state.RipNext = ChiefKits.RiptideEveryTicks;
            if (HasMove(side, ChiefMove.TidalBore)) state.BoreNext = ChiefKits.BoreEveryTicks;
            if (HasMove(side, ChiefMove.StormSurge)) state.SurgeNext = ChiefKits.SurgeEveryTicks;
        }

        // His moves start on their own clocks.
        private void StartTideMoves(int side)
        {
            if (side != _tideSide) return;
            TryStartSluice(side);
            if (!Stopped) TryStartRiptide(side);
            if (!Stopped) TryStartBore(side);
            if (!Stopped) TryStartSurge(side);
        }

        // And run on after the tick's blows; then the clock turns.
        private void AdvanceTideMoves(int side)
        {
            if (side != _tideSide) return;
            AdvanceSluice(side);
            if (!Stopped) AdvanceRiptide(side);
            if (!Stopped) AdvanceBore(side);
            if (!Stopped) AdvanceHorn(side);
            if (!Stopped) AdvanceSurge(side);
            if (!Stopped) TurnClock(side);
        }

        // Busy with his tide, so it does nothing else this tick: his engineer cranking, and he sounding his horn.
        private bool TideBusy(BattleUnit unit)
        {
            if (unit.Side != _tideSide) return false;
            SideState state = _sides[unit.Side];
            if (state.CrankTicks > 0 && string.Equals(unit.Id, state.EngineerId, StringComparison.Ordinal)) return true;
            return state.HornTicks > 0 && unit.Role == UnitRole.Chief;
        }

        // ------------------------------------------------------------ Sluice Gate

        // Every SluiceEveryTicks his engineer cranks a gate, while he can move and an engineer of his is free.
        private void TryStartSluice(int side)
        {
            SideState state = _sides[side];
            if (!HasMove(side, ChiefMove.SluiceGate) || state.CrankTicks > 0 || _tick < state.SluiceNext) return;
            BattleUnit chief = KitChief(side);
            BattleUnit engineer = chief != null && CanMove(chief) ? Engineer(side) : null;
            if (engineer == null)
            {
                state.SluiceNext = _tick + 1;
                return;
            }
            state.CrankTicks = ChiefKits.CrankTicks;
            state.EngineerId = engineer.Id;
            state.SluiceNext = _tick + ChiefKits.SluiceEveryTicks;
            Emit(EventFor(BattleEventKind.Tell, engineer, null, ChiefKits.CrankTicks, ChiefKits.SluiceName));
        }

        // His engineer: his band's first free Trapper.
        private BattleUnit Engineer(int side)
        {
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (u.Alive && u.Side == side && u.Role == UnitRole.Fighter && u.Class == UnitClass.Trapper && u.SnareTicks <= 0 &&
                    u.PanicTicks <= 0 && u.StunTicks <= 0 && !(_fight.FreezeContracts && ControlBlocked(u)))
                    return u;
            }
            return null;
        }

        // The gate grinds on. Focus Fire on the engineer jams it, as does his falling (a Break). Cranked all the way, the water
        // stands a level higher until the next low water, and the clock holds, so that low water comes late. It rose
        // unanswered, so Turn of the Tide's count starts over.
        private void AdvanceSluice(int side)
        {
            SideState state = _sides[side];
            if (state.CrankTicks <= 0) return;
            int enemy = 1 - side;
            BattleUnit chief = KitChief(side);
            BattleUnit engineer = FindById(state.EngineerId);
            if (chief == null || engineer == null)
            {
                ClearSluice(state);
                return;
            }
            string name = ChiefKits.SluiceName + ".";
            if (!engineer.Alive || IsFocusing(enemy, engineer))
            {
                ClearSluice(state);
                Emit(EventFor(BattleEventKind.Land, engineer, null, 0, name + ChiefKits.Broken));
                Answered(enemy, ChiefAnswer.Break, chief, FightRules.InterruptPoints, FightRules.InterruptReason);
                return;
            }
            if (_fight.FreezeContracts && ControlBlocked(engineer)) return;
            if (--state.CrankTicks > 0) return;
            // Under the parry at the center the slam is its beat (BattleSimulation.Parry.cs): parried, the gate jams. It hits
            // nobody of yours, so a stale Hold costs nothing here.
            if (_parryOn)
            {
                int hold = HoldBeat(side, engineer, BeatSlot.Sluice);
                if (hold > 0)
                {
                    state.CrankTicks = hold;
                    return;
                }
                if (MeetBeat(enemy) == BeatMet.Parry)
                {
                    ClearSluice(state);
                    Emit(EventFor(BattleEventKind.Land, engineer, null, 0, name + ChiefKits.Parried));
                    ParriedBeat(side, engineer, ChiefKits.SluiceName, ChiefKits.ParryNerve, FightRules.ParryPoints, true, chief);
                    return;
                }
            }
            ClearSluice(state);
            state.TideGate = Math.Min(FloodRules.MaxLevel, state.TideGate + 1);
            state.TideHold += ChiefKits.SluiceHoldTicks;
            Emit(EventFor(BattleEventKind.Land, engineer, null, TideLevel(), name + ChiefKits.Full));
            TideChanged(ChiefKits.TideGate);
            _sides[enemy].Chain = 0;
        }

        // An Unleash from the other side slams his gate shut while it grinds (a Smash).
        private void SlamSluice(int side)
        {
            int enemy = 1 - side;
            if (Stopped || enemy != _tideSide) return;
            SideState his = _sides[enemy];
            if (his.CrankTicks <= 0) return;
            BattleUnit chief = KitChief(enemy);
            BattleUnit engineer = FindById(his.EngineerId);
            ClearSluice(his);
            if (chief == null || engineer == null) return;
            Emit(EventFor(BattleEventKind.Land, engineer, null, 0, ChiefKits.SluiceName + "." + ChiefKits.Smashed));
            Answered(side, ChiefAnswer.Smash, chief, ChiefSpells.SmashPoints, ChiefSpells.SmashReason);
        }

        private static void ClearSluice(SideState state)
        {
            state.CrankTicks = 0;
            state.EngineerId = string.Empty;
            state.Beats[(int)BeatSlot.Sluice] = 0;
        }

        // ------------------------------------------------------------ Riptide

        // Every RiptideEveryTicks the current runs out, while he can move.
        private void TryStartRiptide(int side)
        {
            SideState state = _sides[side];
            if (!HasMove(side, ChiefMove.Riptide) || state.RipTicks > 0 || _tick < state.RipNext) return;
            BattleUnit chief = KitChief(side);
            if (chief == null || !CanMove(chief))
            {
                state.RipNext = _tick + 1;
                return;
            }
            state.RipTicks = ChiefKits.RiptideTicks;
            state.RipHeld = false;
            state.RipNext = _tick + ChiefKits.RiptideEveryTicks;
            Emit(EventFor(BattleEventKind.Tell, chief, null, ChiefKits.RiptideTicks, ChiefKits.RiptideName));
        }

        // The current runs. With a Hold up the other side stands fast; the first moment it falls back with none, the current
        // pulls it into the deep and is spent. When it has run, a Hold meanwhile answered it (a Brace); nobody fell back and
        // nobody held, nothing came of it.
        private void AdvanceRiptide(int side)
        {
            SideState state = _sides[side];
            if (state.RipTicks <= 0) return;
            int enemy = 1 - side;
            SideState theirs = _sides[enemy];
            BattleUnit chief = KitChief(side);
            if (chief == null)
            {
                ClearRiptide(state);
                return;
            }
            if (theirs.HoldTicks > 0) state.RipHeld = true;
            else if (theirs.FallBackTicks > 0)
            {
                ClearRiptide(state);
                Pull(side, chief);
                return;
            }
            // Under the parry at the center the yank, ParryRules.YankTicks in, is its beat (BattleSimulation.Parry.cs).
            if (state.RipHold > 0)
            {
                if (--state.RipHold == 0) Yank(side, chief);
                return;
            }
            if (--state.RipTicks > 0)
            {
                if (_parryOn && ChiefKits.RiptideTicks - state.RipTicks == ParryRules.YankTicks) Yank(side, chief);
                return;
            }
            bool held = state.RipHeld;
            ClearRiptide(state);
            Emit(EventFor(BattleEventKind.Land, chief, null, 0, ChiefKits.RiptideName + "." + (held ? ChiefKits.Braced : ChiefKits.Clear)));
            // Under the parry at the center a brace is a block, paid at the yank, not an answer.
            if (held && !_parryOn) Answer(enemy, ChiefAnswer.Brace, chief, 0);
        }

        // The yank: a parry breaks the current; a stale Hold stands fast but cracks your nerve, and the current runs on; with no
        // Hold it pulls your side into the deep.
        private void Yank(int side, BattleUnit chief)
        {
            SideState state = _sides[side];
            int hold = HoldBeat(side, chief, BeatSlot.Rip);
            if (hold > 0)
            {
                state.RipHold = hold;
                return;
            }
            ClearBeat(side, BeatSlot.Rip);
            BeatMet met = MeetBeat(1 - side);
            if (met == BeatMet.Parry)
            {
                ClearRiptide(state);
                Emit(EventFor(BattleEventKind.Land, chief, null, 0, ChiefKits.RiptideName + "." + ChiefKits.Parried));
                ParriedBeat(side, chief, ChiefKits.RiptideName, ChiefKits.ParryNerve, FightRules.ParryPoints, true);
                return;
            }
            if (met == BeatMet.Block)
            {
                state.RipHeld = true;
                BlockedBeat(side, chief, ChiefKits.ParryNerve);
                return;
            }
            ClearRiptide(state);
            LandedBeat(side);
            Pull(side, chief);
        }

        // Everyone of the other side a move can fall on is pulled into the deep: hurt, and doing nothing for a while. They ate it.
        private void Pull(int side, BattleUnit chief)
        {
            int enemy = 1 - side;
            List<BattleUnit> hit = MarksOf(enemy);
            var shares = new int[hit.Count];
            int total = 0;
            for (int k = 0; k < hit.Count; k++)
            {
                shares[k] = Math.Max(1, hit[k].MaxHp * ChiefKits.RiptidePercent / 100);
                total += shares[k];
            }
            Emit(EventFor(BattleEventKind.Land, chief, null, total, ChiefKits.RiptideName + "." + ChiefKits.Full));
            for (int k = 0; k < hit.Count && !Stopped; k++)
            {
                hit[k].Statuses.Apply(StatusKind.Stun, ChiefKits.PulledTicks, keepLonger: true);
                ApplyDamage(chief, side, hit[k], Armored(hit[k], shares[k]), ChiefKits.RiptideDetail);
            }
            if (!Stopped) Eaten(enemy, chief);
            CheckWipe();
        }

        private static void ClearRiptide(SideState state)
        {
            state.RipTicks = 0;
            state.RipHeld = false;
            state.RipHold = 0;
            state.Beats[(int)BeatSlot.Rip] = 0;
        }

        // ------------------------------------------------------------ Tidal Bore

        // Every BoreEveryTicks a wall of water starts across the other side's rows, from an end picked from the fight's seeded
        // stream, while he can move and someone of theirs stands in its way.
        private void TryStartBore(int side)
        {
            SideState state = _sides[side];
            if (!HasMove(side, ChiefMove.TidalBore) || state.BoreTicks > 0 || _tick < state.BoreNext) return;
            BattleUnit chief = KitChief(side);
            if (chief == null || !CanMove(chief) || MarksOf(1 - side).Count == 0)
            {
                state.BoreNext = _tick + 1;
                return;
            }
            ClearBore(state);
            state.BoreTicks = ChiefKits.BoreTicks;
            state.BoreFromRight = _rng.Next(2) == 1;
            state.BoreNext = _tick + ChiefKits.BoreEveryTicks;
            Emit(EventFor(BattleEventKind.Tell, chief, null, ChiefKits.BoreTicks, ChiefKits.BoreName));
        }

        // The wall runs on: each tick it reaches whoever of the other side a move can fall on stands between where it was and
        // where it is now, nearest first. A Warden it reaches breaks it (a Guard), and nobody past him is touched. A fighter it
        // reaches while their side falls back is clear; while it holds, the wall soaks and knocks nobody down; otherwise it
        // hits and knocks them down. When it has run: any full hit and they ate it; else soaked (a Brace); else dodged.
        private void AdvanceBore(int side)
        {
            SideState state = _sides[side];
            if (state.BoreTicks <= 0) return;
            int enemy = 1 - side;
            SideState theirs = _sides[enemy];
            BattleUnit chief = KitChief(side);
            if (chief == null)
            {
                ClearBore(state);
                return;
            }
            if (state.BoreHold > 0)
            {
                state.BoreHold--;
                return;
            }
            state.BoreTicks--;
            int front = 1000 * (ChiefKits.BoreTicks - state.BoreTicks) / ChiefKits.BoreTicks;
            List<BattleUnit> reached = Reached(enemy, state.BoreFromRight, state.BoreFront, front);
            // Under the parry at the center the first lane it hits is its beat (BattleSimulation.Parry.cs): a wait or a feint
            // holds the wall; a parry breaks it there; a stale Hold soaks as it always did but cracks your nerve once. A Warden
            // in that first lane breaks it as he always did, a safe answer.
            if (_parryOn && reached.Count > 0 && theirs.FallBackTicks <= 0 && (state.Beats[(int)BeatSlot.Bore] & BeatSecond) == 0)
            {
                int hold = HoldBeat(side, chief, BeatSlot.Bore);
                if (hold > 0)
                {
                    state.BoreTicks++;
                    state.BoreHold = hold;
                    return;
                }
                state.Beats[(int)BeatSlot.Bore] |= BeatSecond;
                BeatMet met = MeetBeat(enemy);
                if (met == BeatMet.Parry)
                {
                    ClearBore(state);
                    Emit(EventFor(BattleEventKind.Land, chief, reached[0], 0, ChiefKits.BoreName + "." + ChiefKits.Parried));
                    ParriedBeat(side, chief, ChiefKits.BoreName, ChiefKits.ParryNerve, FightRules.ParryPoints, true);
                    return;
                }
                if (reached[0].Class != UnitClass.Warden)
                {
                    if (met == BeatMet.Block) BlockedBeat(side, chief, ChiefKits.ParryNerve);
                    else LandedBeat(side);
                    if (Stopped) return;
                }
            }
            state.BoreFront = front;
            for (int k = 0; k < reached.Count && !Stopped; k++)
            {
                BattleUnit u = reached[k];
                if (theirs.FallBackTicks > 0)
                {
                    state.BoreDodged++;
                    continue;
                }
                int share = Math.Max(1, u.MaxHp * ChiefKits.BorePercent / 100);
                if (u.Class == UnitClass.Warden)
                {
                    BreakBore(side, chief, u, share);
                    return;
                }
                if (theirs.HoldTicks > 0)
                {
                    state.BoreSoaked++;
                    share = Math.Max(1, share * ChiefKits.BoreSoakPercent / 100);
                }
                else
                {
                    state.BoreFull++;
                    u.Statuses.Apply(StatusKind.Stun, ChiefKits.KnockTicks, keepLonger: true);
                }
                ApplyDamage(chief, side, u, Armored(u, share), ChiefKits.BoreDetail);
            }
            if (Stopped || state.BoreTicks > 0) return;
            int full = state.BoreFull, soaked = state.BoreSoaked, dodged = state.BoreDodged;
            ClearBore(state);
            string how = full > 0 ? ChiefKits.Full : soaked > 0 ? ChiefKits.Soaked : dodged > 0 ? ChiefKits.Dodged : ChiefKits.Clear;
            Emit(EventFor(BattleEventKind.Land, chief, null, 0, ChiefKits.BoreName + "." + how));
            if (full > 0) Eaten(enemy, chief);
            else if (soaked > 0) { if (!_parryOn) Answer(enemy, ChiefAnswer.Brace, chief, 0); }
            else if (dodged > 0)
            {
                Earn(enemy, FightRules.DodgePoints, FightRules.DodgeReason);
                FavourDodge(enemy, chief);
                Answer(enemy, ChiefAnswer.Dodge, chief, FightRules.DodgePoints);
            }
            CheckWipe();
        }

        // Whoever of a side a move can fall on stands past where the wall was and up to where it is now, measured from the end
        // it started at; nearest first, then the front row, then setup order. The dead keep their place in the lanes.
        private List<BattleUnit> Reached(int side, bool fromRight, int was, int now)
        {
            var list = new List<BattleUnit>();
            var distance = new List<int>();
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (!Markable(u, side)) continue;
                int d = fromRight ? 1000 - LanePosition(u) : LanePosition(u);
                if (d <= was || d > now) continue;
                int at = list.Count;
                while (at > 0 && (distance[at - 1] > d || (distance[at - 1] == d && list[at - 1].Row == Row.Back && u.Row == Row.Front))) at--;
                list.Insert(at, u);
                distance.Insert(at, d);
            }
            return list;
        }

        // A Warden of the other side broke the wall: he takes it less his Guard, and it is over. With nobody hit in full before
        // him that answers it (a Guard); a Warden at the far end who breaks it after it hit the rest leaves it eaten all the same.
        private void BreakBore(int side, BattleUnit chief, BattleUnit warden, int share)
        {
            int enemy = 1 - side;
            SideState state = _sides[side];
            bool full = state.BoreFull > 0;
            ClearBore(state);
            int cut = _classOn ? GuardCut(warden) : SliceClasses.GuardReductionPercent;
            int taken = Math.Max(1, share * (100 - cut) / 100);
            Emit(EventFor(BattleEventKind.Land, chief, warden, taken, ChiefKits.BoreName + "." + (full ? ChiefKits.Full : ChiefKits.Guarded)));
            ApplyDamage(chief, side, warden, Armored(warden, taken), ChiefKits.BoreDetail);
            if (Stopped) return;
            if (full) Eaten(enemy, chief);
            else Answered(enemy, ChiefAnswer.Guard, chief, ChiefKits.GuardPoints, ChiefKits.Name(ChiefAnswer.Guard));
            CheckWipe();
        }

        private static void ClearBore(SideState state)
        {
            state.BoreHold = 0;
            state.Beats[(int)BeatSlot.Bore] = 0;
            state.BoreTicks = 0;
            state.BoreFront = -1;
            state.BoreFull = 0;
            state.BoreSoaked = 0;
            state.BoreDodged = 0;
        }

        // ------------------------------------------------------------ Storm Surge

        // Every SurgeEveryTicks he sounds his horn, while he can move, no surge stands and his Tide is not winding up.
        private void TryStartSurge(int side)
        {
            SideState state = _sides[side];
            if (!HasMove(side, ChiefMove.StormSurge) || state.HornTicks > 0 || state.SurgeTicks > 0 || _tick < state.SurgeNext) return;
            BattleUnit chief = KitChief(side);
            if (chief == null || !CanMove(chief) || state.Windup > 0)
            {
                state.SurgeNext = _tick + 1;
                return;
            }
            state.HornTicks = ChiefKits.HornTicks;
            state.SurgeHeld = false;
            state.SurgeNext = _tick + ChiefKits.SurgeEveryTicks;
            Emit(EventFor(BattleEventKind.Tell, chief, null, ChiefKits.HornTicks, ChiefKits.SurgeName));
        }

        // The horn sounds on. Focus Fire on him breaks the call (a Break); gone with nothing to show if he is stunned, held or
        // kneels. Sounded all the way, the water stands at its highest for ChiefKits.SurgeTicks.
        private void AdvanceHorn(int side)
        {
            SideState state = _sides[side];
            if (state.HornTicks <= 0) return;
            int enemy = 1 - side;
            BattleUnit chief = KitChief(side);
            if (chief == null || !CanMove(chief))
            {
                state.HornTicks = 0;
                ClearBeat(side, BeatSlot.Surge);
                return;
            }
            if (IsFocusing(enemy, chief))
            {
                state.HornTicks = 0;
                ClearBeat(side, BeatSlot.Surge);
                Emit(EventFor(BattleEventKind.Land, chief, null, 0, ChiefKits.SurgeName + "." + ChiefKits.Broken));
                Answered(enemy, ChiefAnswer.Break, chief, FightRules.InterruptPoints, FightRules.InterruptReason);
                return;
            }
            if (--state.HornTicks > 0) return;
            // Under the parry at the center the horn's last blast is its beat (BattleSimulation.Parry.cs): parried, the surge
            // never comes. It hits nobody of yours, so a stale Hold costs nothing here.
            if (_parryOn)
            {
                int hold = HoldBeat(side, chief, BeatSlot.Surge);
                if (hold > 0)
                {
                    state.HornTicks = hold;
                    return;
                }
                ClearBeat(side, BeatSlot.Surge);
                if (MeetBeat(enemy) == BeatMet.Parry)
                {
                    Emit(EventFor(BattleEventKind.Land, chief, null, 0, ChiefKits.SurgeName + "." + ChiefKits.Parried));
                    ParriedBeat(side, chief, ChiefKits.SurgeName, ChiefKits.ParryNerve, FightRules.ParryPoints, true);
                    return;
                }
            }
            state.SurgeTicks = ChiefKits.SurgeTicks;
            state.SurgeHeld = false;
            TideChanged(ChiefKits.TideSurge);
        }

        // The high water stands, noting a Hold from the other side. On its last tick, how it was met, while the water still
        // stands high: held through (a Brace), or eaten; then the water falls back to the clock.
        private void AdvanceSurge(int side)
        {
            SideState state = _sides[side];
            if (state.SurgeTicks <= 0) return;
            int enemy = 1 - side;
            if (_sides[enemy].HoldTicks > 0) state.SurgeHeld = true;
            if (state.SurgeTicks > 1)
            {
                state.SurgeTicks--;
                return;
            }
            bool held = state.SurgeHeld;
            BattleUnit chief = KitChief(side);
            if (chief != null)
            {
                Emit(EventFor(BattleEventKind.Land, chief, null, 0, ChiefKits.SurgeName + "." + (held ? ChiefKits.Braced : ChiefKits.Full)));
                if (held) Answer(enemy, ChiefAnswer.Brace, chief, 0);
                else Eaten(enemy, chief);
            }
            state.SurgeTicks = 0;
            state.SurgeHeld = false;
            if (!Stopped) TideChanged(ChiefKits.TideSurge);
        }

        // The nerve the water takes from a side each second: half of it while that side holds through his Storm Surge.
        private int SurgeNerve(int side, int nerve)
        {
            return _sides[_tideSide].SurgeTicks > 0 && _sides[side].HoldTicks > 0 ? nerve * ChiefKits.SurgeHoldPercent / 100 : nerve;
        }

        // ------------------------------------------------------------ Turn of the Tide

        // A side answered one of his moves. At high water it counts toward Turn of the Tide; below it the count starts over. At
        // ChiefKits.TurnAnswers the tide turns: the water drains to nothing for ChiefKits.DrainTicks, so his fishmen are
        // stranded, and a Storm Surge still standing ends with it.
        private void TideAnswered(int side, BattleUnit chief)
        {
            SideState theirs = _sides[side];
            if (FloodLevel < FloodRules.MaxLevel)
            {
                theirs.Chain = 0;
                return;
            }
            if (++theirs.Chain < ChiefKits.TurnAnswers) return;
            theirs.Chain = 0;
            SideState his = _sides[chief.Side];
            his.DrainTicks = ChiefKits.DrainTicks;
            his.SurgeTicks = 0;
            his.SurgeHeld = false;
            Emit(new BattleEvent(_tick, BattleEventKind.Combo, side, string.Empty, -1, chief.Id, (int)chief.Class, ChiefKits.DrainTicks,
                ChiefKits.TurnOfTheTide));
            Earn(side, FightRules.ComboPoints, FightRules.ComboReason);
            TideChanged(ChiefKits.TideDrain);
        }

        // ------------------------------------------------------------ the kneel and the checksum

        private void ClearTide(int side)
        {
            SideState state = _sides[side];
            ClearSluice(state);
            ClearRiptide(state);
            ClearBore(state);
            state.HornTicks = 0;
            state.Beats[(int)BeatSlot.Surge] = 0;
        }

        // His kit's state for the checksum, written only in his fights.
        private static void AppendTide(SnapshotBuilder b, SideState state)
        {
            b.Field("tidePhase", state.TidePhase);
            b.Field("tideHold", state.TideHold);
            b.Field("tideGate", state.TideGate);
            b.Field("surge", state.SurgeTicks);
            b.Field("drain", state.DrainTicks);
            b.Field("tideShown", state.TideShown);
            b.Field("crank", state.CrankTicks);
            b.Field("sluiceNext", state.SluiceNext);
            b.Field("engineer", state.EngineerId);
            b.Field("rip", state.RipTicks);
            b.Field("ripNext", state.RipNext);
            b.Field("ripHeld", state.RipHeld);
            b.Field("bore", state.BoreTicks);
            b.Field("boreNext", state.BoreNext);
            b.Field("boreFromRight", state.BoreFromRight);
            b.Field("boreFront", state.BoreFront);
            b.Field("boreFull", state.BoreFull);
            b.Field("boreSoaked", state.BoreSoaked);
            b.Field("boreDodged", state.BoreDodged);
            b.Field("horn", state.HornTicks);
            b.Field("surgeNext", state.SurgeNext);
            b.Field("surgeHeld", state.SurgeHeld);
        }
    }
}
