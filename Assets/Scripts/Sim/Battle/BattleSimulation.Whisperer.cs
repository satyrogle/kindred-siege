using System;

namespace KindredSiege.Sim.Battle
{
    /// The Whisperer in the fight (Jacob, 31 Oct 2026: "Go, build her"; DESIGN_LOCKS, "Habits come with the game, the Whisperer
    /// upgrades them", "The Whisperer's core" and her sections of 28 Oct; FightRules.Whisper, campaign rules 24). Your side brings
    /// each class's habit, upgraded or not, and the squad's pair (PalKit), and each fighter his rank (UnitSpec.Rank). It rides
    /// on the floor under the three Pals and draws no random number of its own.
    /// - Habits. A spell's target order is Hook drag, Focus Fire, the combo's target, the habit, the pair, then plain; in the
    ///   Guard order a Warden's habit, then Mend and Hold, take only the "Guard the Medic" step's place.
    ///   Bodyguard Guards the Marksman while he aims; upgraded, while one of your back row aims his Guard covers the whole
    ///   back row (only the aim takes time in this build). Rescuer Guards whoever has the least health; upgraded, the Guard
    ///   jumps the moment someone else drops lower. Breaker strikes whoever winds up a big move; upgraded, her strike sends
    ///   the wind up back to its start. Avenger strikes whoever last hit one of yours; upgraded, she goes at once even if her
    ///   spell is not ready, once a cycle of it. Counter Shot shoots whoever last hit your back row; upgraded, that shot needs
    ///   no aim. Spotter shoots whoever winds up a big move; upgraded, his Opening stays until someone cashes it. Ambusher's
    ///   trap ignores light hits and springs on the first heavy swing as it lands; upgraded, that swing is stopped. Triage
    ///   waits until someone is under a quarter health; upgraded, that treatment lands twice as big. Front First treats the
    ///   front row first; upgraded, her treatment reaches the whole front row. Gatekeeper is out of the slice.
    /// - Pairs bond their two classes (BattleSimulation.Chains.cs): Pin and Shoot (the Trapper's snare, the Marksman), Open
    ///   and Cut (the Marksman's Opening, the Skirmisher), Snare and Rip (the Trapper's bleed, the Skirmisher). The answer is
    ///   free, spending nothing of his own spell, once a cycle of it and at least ChainRules.BondGapTicks apart. Mend and Hold:
    ///   right after the Medic treats someone, the Warden Guards them, and her too.
    /// - Rank 2: his class spell happens one more time. The Warden Guards two allies, the Skirmisher strikes twice, the
    ///   Marksman fires two shots per aim, the Trapper's trap springs twice before she rearms it, and the Medic keeps one
    ///   treatment in hand (banked on a turn with nobody to treat) so she treats twice in a row. Rank 3 is the second slot
    ///   (BattleSimulation.Shade.cs).
    public sealed partial class BattleSimulation
    {
        private readonly bool _whisperOn;

        /// Whether this fight plays the Whisperer: campaign rules 24.
        public bool WhisperOn => _whisperOn;

        // His rank, 1 outside rules 24.
        private int FighterRank(BattleUnit unit)
        {
            return _whisperOn && unit != null ? SpecOf(unit).Rank : 1;
        }

        private bool RankTwo(BattleUnit unit)
        {
            return FighterRank(unit) >= WhisperRules.RankTwo;
        }

        // His class's habit on his side, None outside rules 24.
        private WhispererHabit HabitOf(BattleUnit unit)
        {
            return _whisperOn && unit != null ? KitOf(unit.Side).HabitOf(unit.Class) : WhispererHabit.None;
        }

        // Whether his class's habit is this one, upgraded.
        private bool HabitUp(BattleUnit unit, WhispererHabit habit)
        {
            return HabitOf(unit) == habit && KitOf(unit.Side).Upgraded(habit);
        }

        private SquadPair PairOf(int side)
        {
            return _whisperOn ? KitOf(side).Pair : SquadPair.None;
        }

        // Whether he winds up a big move: his side's big spell, its heavy swing, his kit's Cleave as its chief, or a chief's move
        // he carries (the moves a Stasis burst restarts, BattleSimulation.Marks.cs).
        private bool WindingUp(BattleUnit e)
        {
            SideState state = _sides[e.Side];
            return (state.Windup > 0 && state.CasterId == e.Id) || (state.HeavyWindup > 0 && state.HeavySwingerId == e.Id) ||
                   (state.CleaveWindup > 0 && ReferenceEquals(Leader(e.Side), e)) || e.CarryTell > 0;
        }

        // An enemy his spell can reach right now.
        private bool Reachable(BattleUnit unit, BattleUnit e, bool reachBack)
        {
            return e != null && e.Alive && e.Side != unit.Side && !LeaderProtected(e) && (e.Row == Row.Front || reachBack) &&
                   !Spared(unit, e) && e.EvacTicks <= 0;
        }

        // ------------------------------------------------------------ the habits' targets

        // The enemy his habit picks for his spell, or null: Breaker and Spotter go for whoever winds up a big move, Avenger for
        // whoever last hit one of yours, Counter Shot for whoever last hit your back row.
        private BattleUnit HabitTarget(BattleUnit unit)
        {
            WhispererHabit habit = HabitOf(unit);
            if (habit == WhispererHabit.None) return null;
            bool reach = CanReachBack(unit);
            switch (habit)
            {
                case WhispererHabit.Breaker:
                case WhispererHabit.Spotter:
                    for (int i = 0; i < _units.Count; i++)
                        if (Reachable(unit, _units[i], reach) && WindingUp(_units[i])) return _units[i];
                    return null;
                case WhispererHabit.Avenger:
                {
                    BattleUnit e = FindById(_sides[unit.Side].LastHitterId);
                    return Reachable(unit, e, reach) ? e : null;
                }
                case WhispererHabit.CounterShot:
                {
                    BattleUnit e = FindById(_sides[unit.Side].LastBackHitterId);
                    return Reachable(unit, e, reach) ? e : null;
                }
                default:
                    return null;
            }
        }

        // A blow from the other side landed on one of this side: who did it, for Avenger and Counter Shot.
        private void Struck(BattleUnit source, BattleUnit victim)
        {
            SideState mine = _sides[victim.Side];
            mine.LastHitterId = source.Id;
            if (victim.Row == Row.Back) mine.LastBackHitterId = source.Id;
            if (!KitOf(victim.Side).Upgraded(WhispererHabit.Avenger)) return;
            mine.AvengeId = source.Id;
            mine.AvengeTick = _tick;
        }

        // ------------------------------------------------------------ spells, once more at rank 2

        // Her strike's blows: one, and at rank 2 a second, at the same enemy while he stands, else at her next target. An
        // upgraded Breaker's strike sends a wind up back to its start.
        private void Strikes(BattleUnit unit, BattleUnit target)
        {
            bool alive = target.Alive;
            Blow(unit, target, unit.Power, BlowKind.Strike);
            Broke(unit, target);
            // Reaver (rules 30, BattleSimulation.Vows.cs).
            if (_vowsOn) Reaved(unit, target, alive);
            if (!RankTwo(unit) || _finished || !unit.Alive) return;
            BattleUnit next = target.Alive ? target : SpellTarget(unit, false);
            if (next == null) return;
            alive = next.Alive;
            Blow(unit, next, unit.Power, BlowKind.Strike);
            Broke(unit, next);
            if (_vowsOn) Reaved(unit, next, alive);
        }

        private void Broke(BattleUnit unit, BattleUnit target)
        {
            if (!_finished && target.Alive && HabitUp(unit, WhispererHabit.Breaker) && WindingUp(target)) RestartWindUp(target);
        }

        // His shot's blows: one, and at rank 2 a second, the same way.
        private void Shots(BattleUnit unit, BattleUnit target)
        {
            Blow(unit, target, unit.Power, BlowKind.AimedShot);
            // Sniper and Ranger (rules 30, BattleSimulation.Vows.cs) on the first shot.
            if (_vowsOn) ShotVows(unit, target);
            if (!RankTwo(unit) || _finished || !unit.Alive) return;
            BattleUnit next = target.Alive ? target : SpellTarget(unit, true);
            if (next != null) Blow(unit, next, unit.Power, BlowKind.AimedShot);
        }

        // An upgraded Counter Shot at whoever last hit your back row needs no aim: he fires at once. True when he did.
        private bool CounterAtOnce(BattleUnit unit, BattleUnit target)
        {
            if (!HabitUp(unit, WhispererHabit.CounterShot) || target.Id != _sides[unit.Side].LastBackHitterId) return false;
            unit.SpellTicks = AimEvery(unit);
            unit.FreeSpent = false;
            Emit(EventFor(BattleEventKind.Whisper, unit, target, 0, WhisperRules.CounterDetail));
            Shots(unit, target);
            CheckWipe();
            return true;
        }

        // An upgraded Spotter's Opening stays until someone cashes it: the whole fight's clock.
        private int OpeningTicksBy(BattleUnit by)
        {
            return HabitUp(by, WhispererHabit.Spotter) ? Math.Max(ClassSpells.OpeningTicks, _setup.TickLimit) : ClassSpells.OpeningTicks;
        }

        // ------------------------------------------------------------ the pairs' free answer

        // Whether a pair bonds this fighter's marks to this casher: the pair's first class to its answering one, on one side.
        private bool PairBonded(string originId, BattleUnit casher)
        {
            SquadPair pair = PairOf(casher.Side);
            if (pair == SquadPair.None || pair == SquadPair.MendAndHold || string.IsNullOrEmpty(originId)) return false;
            BattleUnit origin = FindById(originId);
            return origin != null && origin.Side == casher.Side && origin.Class == WhisperRules.FirstOf(pair) &&
                   casher.Class == WhisperRules.AnswerOf(pair);
        }

        // A bonded answer is free when a pair bonds them and he has not had his free one this cycle of his spell. It spends it.
        private bool FreeAnswer(string originId, BattleUnit casher, BattleUnit target)
        {
            if (!_whisperOn || casher.FreeSpent || !PairBonded(originId, casher)) return false;
            casher.FreeSpent = true;
            Emit(EventFor(BattleEventKind.Whisper, casher, target, 0, WhisperRules.FreeDetail));
            return true;
        }

        // ------------------------------------------------------------ the Warden

        // The Guard order's "Guard the Medic" step from rules 24: his habit's ally, then Mend and Hold's patient, then the Medic
        // only with no habit. Null leaves it to the next step.
        private BattleUnit WhisperGuard(BattleUnit warden)
        {
            WhispererHabit habit = HabitOf(warden);
            if (habit == WhispererHabit.Bodyguard)
            {
                BattleUnit aiming = AimingAlly(warden);
                if (aiming != null) return aiming;
            }
            else if (habit == WhispererHabit.Rescuer) return MostHurtAlly(warden, int.MaxValue, false);
            if (PairOf(warden.Side) == SquadPair.MendAndHold)
            {
                BattleUnit patient = FindById(_sides[warden.Side].LastPatientId);
                if (patient != null && patient.Alive && patient.Side == warden.Side && !ReferenceEquals(patient, warden) &&
                    !(_beastOn && IsHeld(patient)))
                    return patient;
            }
            return habit == WhispererHabit.None ? CoveredMedic(warden) : null;
        }

        // A Marksman of his side aiming right now, the first in setup order.
        private BattleUnit AimingAlly(BattleUnit warden)
        {
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (u.Alive && u.Side == warden.Side && u.Class == UnitClass.Marksman && u.AimTicks > 0) return u;
            }
            return null;
        }

        // His second ward, after UpdateGuard picked his first: under Mend and Hold the Medic beside the patient he Guards, else
        // at rank 2 the next in his order (the Medic with no habit, else the most hurt ally left).
        private void SecondGuard(BattleUnit warden)
        {
            warden.GuardTargetId2 = string.Empty;
            BattleUnit first = FindById(warden.GuardTargetId);
            BattleUnit second = null;
            if (first != null && PairOf(warden.Side) == SquadPair.MendAndHold && first.Id == _sides[warden.Side].LastPatientId)
                second = CoveredMedic(warden);
            if ((second == null || ReferenceEquals(second, first)) && RankTwo(warden)) second = NextWard(warden, first);
            if (second != null && !ReferenceEquals(second, first) && !ReferenceEquals(second, warden)) warden.GuardTargetId2 = second.Id;
        }

        private BattleUnit NextWard(BattleUnit warden, BattleUnit first)
        {
            if (_classOn && HabitOf(warden) == WhispererHabit.None)
            {
                BattleUnit medic = CoveredMedic(warden);
                if (medic != null && !ReferenceEquals(medic, first)) return medic;
            }
            BattleUnit best = null;
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (!u.Alive || u.Side != warden.Side || ReferenceEquals(u, warden) || ReferenceEquals(u, first)) continue;
                if (_beastOn && IsHeld(u)) continue;
                if (best == null || u.HpPermille < best.HpPermille) best = u;
            }
            return best;
        }

        // Whether his Guard covers this ally beside his first ward: his second, or, an upgraded Bodyguard, the whole back row
        // while one of them aims.
        private bool AlsoGuards(BattleUnit warden, BattleUnit target)
        {
            if (warden.GuardTargetId2.Length > 0 && string.Equals(warden.GuardTargetId2, target.Id, StringComparison.Ordinal)) return true;
            // Bastion (rules 30, BattleSimulation.Vows.cs).
            if (_vowsOn && BastionCovers(warden, target)) return true;
            return target.Row == Row.Back && HabitUp(warden, WhispererHabit.Bodyguard) && AimingAlly(warden) != null;
        }

        // ------------------------------------------------------------ the Medic

        // Whom her habit treats: Triage only someone under a quarter health, else she waits; Front First the most hurt of the
        // front row, else as plain.
        private BattleUnit HabitPatient(BattleUnit medic)
        {
            switch (HabitOf(medic))
            {
                case WhispererHabit.Triage:
                    return MostHurtAlly(medic, WhisperRules.TriageBelowPermille, true);
                case WhispererHabit.FrontFirst:
                {
                    BattleUnit best = null;
                    for (int i = 0; i < _units.Count; i++)
                    {
                        BattleUnit u = _units[i];
                        if (!u.Alive || u.Side != medic.Side || u.Row != Row.Front || u.HpPermille >= MedicHealsBelowPermille) continue;
                        if (_beastOn && IsHeld(u)) continue;
                        if (best == null || u.HpPermille < best.HpPermille) best = u;
                    }
                    return best ?? MostHurtAlly(medic, MedicHealsBelowPermille, true);
                }
                default:
                    return MostHurtAlly(medic, MedicHealsBelowPermille, true);
            }
        }

        // Her turn from rules 24: she treats, and at rank 2 she keeps a treatment in hand on a turn with nobody to treat, and
        // spends it at once on the next one so she treats twice in a row.
        private void WhisperTreat(BattleUnit medic)
        {
            BattleUnit patient = PatientFor(medic);
            if (patient == null)
            {
                if (!RankTwo(medic) || medic.TreatmentInHand) return;
                medic.TreatmentInHand = true;
                Emit(EventFor(BattleEventKind.Whisper, medic, null, 0, WhisperRules.InHandDetail));
                return;
            }
            TreatOnce(medic, patient);
            if (Stopped || !medic.Alive || !medic.TreatmentInHand) return;
            BattleUnit next = PatientFor(medic);
            if (next == null) return;
            medic.TreatmentInHand = false;
            TreatOnce(medic, next);
        }

        // One treatment, then an upgraded Front First's reach along the front row, then Mend and Hold's Guard.
        private void TreatOnce(BattleUnit medic, BattleUnit patient)
        {
            int amount = Treat(medic, patient);
            _sides[medic.Side].LastPatientId = patient.Id;
            // Hierophant (rules 30, BattleSimulation.Vows.cs).
            if (_vowsOn) Splashed(medic, patient, amount);
            if (HabitUp(medic, WhispererHabit.FrontFirst) && patient.Row == Row.Front && amount > 0)
                for (int i = 0; i < _units.Count && !Stopped; i++)
                {
                    BattleUnit u = _units[i];
                    if (!u.Alive || u.Side != medic.Side || u.Row != Row.Front || ReferenceEquals(u, patient) || u.Hp >= u.MaxHp) continue;
                    if (_beastOn && IsHeld(u)) continue;
                    Heal(medic, u, amount, ClassSpells.TreatDetail);
                }
            if (PairOf(medic.Side) != SquadPair.MendAndHold) return;
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit w = _units[i];
                if (!w.Alive || w.Side != medic.Side || w.Class != UnitClass.Warden) continue;
                UpdateGuard(w);
                SecondGuard(w);
            }
        }

        // ------------------------------------------------------------ the Trapper

        // An Ambusher's trap waits for a heavy blow: light hits never spring it.
        private bool AmbushWaits(int side)
        {
            return _whisperOn && KitOf(side).HabitOf(UnitClass.Trapper) == WhispererHabit.Ambusher;
        }

        // A trap that sprang: true while it has a spring left (a rank 2 Trapper's springs twice), else it is spent.
        private bool SpringLeft(SideState state)
        {
            if (!_whisperOn) return false;
            state.TrapSprings--;
            return state.TrapSprings > 0;
        }

        // The springs a trap she arms has: two at rank 2.
        private int SpringsOf(BattleUnit trapper)
        {
            return RankTwo(trapper) ? WhisperRules.RankTwo : 1;
        }

        // A heavy swing lands on this side: an Ambusher's armed trap springs on it, snaring the swinger and making him bleed.
        // True when the swing is stopped: an upgraded Ambusher's, or the trap killed him.
        private bool Ambush(int side, BattleUnit swinger)
        {
            SideState state = _sides[side];
            if (!state.TrapArmed || !AmbushWaits(side) || swinger == null || !swinger.Alive) return false;
            BattleUnit trapper = FindById(state.TrapOwnerId);
            bool stopped = KitOf(side).Upgraded(WhispererHabit.Ambusher);
            if (!SpringLeft(state))
            {
                state.TrapArmed = false;
                state.TrapOwnerId = string.Empty;
            }
            Emit(new BattleEvent(_tick, BattleEventKind.TrapSprung, side, trapper == null ? string.Empty : trapper.Id,
                trapper == null ? -1 : (int)trapper.Class, swinger.Id, (int)swinger.Class, SliceClasses.TrapDamage, string.Empty));
            Emit(new BattleEvent(_tick, BattleEventKind.Whisper, side, trapper == null ? string.Empty : trapper.Id,
                trapper == null ? -1 : (int)trapper.Class, swinger.Id, (int)swinger.Class, 0,
                stopped ? WhisperRules.StoppedDetail : WhisperRules.AmbushDetail));
            ApplyDamage(trapper, side, swinger, SliceClasses.TrapDamage, TrapDetail);
            if (!swinger.Alive || _finished) return true;
            if (_classOn) Snare(trapper, side, swinger);
            return stopped;
        }

        // ------------------------------------------------------------ each tick

        // After the fighters act: an upgraded Rescuer's Guard jumps to whoever now has the least health, and an upgraded Avenger
        // strikes whoever just hit one of yours.
        private void TickWhisper()
        {
            for (int i = 0; i < _units.Count && !Stopped; i++)
            {
                BattleUnit u = _units[i];
                if (!u.Alive) continue;
                if (u.Class == UnitClass.Warden && HabitUp(u, WhispererHabit.Rescuer))
                {
                    BattleUnit lowest = MostHurtAlly(u, int.MaxValue, false);
                    if (lowest != null && lowest.Id != u.GuardTargetId)
                    {
                        UpdateGuard(u);
                        SecondGuard(u);
                    }
                }
                else if (u.Class == UnitClass.Skirmisher && HabitUp(u, WhispererHabit.Avenger)) Avenge(u);
            }
        }

        // An upgraded Avenger's free strike at whoever hit one of yours this tick or the last, once a cycle of her strike. Focus
        // Fire on someone else still comes first.
        private void Avenge(BattleUnit u)
        {
            SideState mine = _sides[u.Side];
            if (mine.AvengeId.Length == 0 || _tick - mine.AvengeTick > 1 || u.FreeSpent) return;
            if (u.SnareTicks > 0 || u.AimTicks > 0 || u.PinTicks > 0 || u.EvacTicks > 0 || u.PanicTicks > 0 || u.Frenzied) return;
            if (MeleeHeldBack(u) != StrikeBlock.None) return;
            BattleUnit foe = FindById(mine.AvengeId);
            if (!Reachable(u, foe, CanReachBack(u))) return;
            if (mine.FocusTicks > 0 && mine.FocusTargetId.Length > 0 && mine.FocusTargetId != foe.Id)
            {
                BattleUnit focus = FindById(mine.FocusTargetId);
                if (focus != null && focus.Alive) return;
            }
            mine.AvengeId = string.Empty;
            u.FreeSpent = true;
            Emit(EventFor(BattleEventKind.Whisper, u, foe, 0, WhisperRules.FreeDetail));
            Strikes(u, foe);
            CheckWipe();
        }

        // ------------------------------------------------------------ the snapshot

        // What the Whisperer keeps on each fighter and side, written only where she plays.
        private void AppendWhisperUnit(SnapshotBuilder b, BattleUnit u)
        {
            b.Field(u.Id + ".rank", SpecOf(u).Rank);
            b.Field(u.Id + ".guard2", u.GuardTargetId2);
            b.Field(u.Id + ".freeSpent", u.FreeSpent);
            b.Field(u.Id + ".inHand", u.TreatmentInHand);
        }

        private void AppendWhisperSide(SnapshotBuilder b, SideState state)
        {
            b.Field("trapSprings", state.TrapSprings);
            b.Field("lastHitter", state.LastHitterId);
            b.Field("lastBackHitter", state.LastBackHitterId);
            b.Field("avenge", state.AvengeId);
            b.Field("avengeTick", state.AvengeTick);
            b.Field("lastPatient", state.LastPatientId);
        }
    }
}
