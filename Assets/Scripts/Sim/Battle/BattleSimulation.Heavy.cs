using System;

namespace KindredSiege.Sim.Battle
{
    /// Heavy swings (Jacob, 18 Oct 2026: "parry always wins thats the whole point but look at sekiro you dont
    /// have to attack at all just riposte"). A side whose rules give HeavyEveryTicks has its fighters take turns
    /// winding up a heavy blow, one at a time, that the screen can show coming. A Hold started in the last half
    /// second before it lands (FightRules.PerfectHoldTicks) parries it: nothing lands, the swinger staggers, the
    /// parrying side gets its point back, the swinger's side loses ParryNerve nerve, and an Unleash straight
    /// after is the Riposte on the swinger. Parry alone can break a side. Fall Back dodges a heavy, for no points.
    ///
    /// A fight with heavy swings on either side also plays the other 18 Oct changes: a parried Eclipse costs its
    /// side EclipseParryNerve nerve; a side that learned to wait out the parry swings late once a blow or a cast,
    /// so a second Hold timed to the late swing still parries; and a Hold given while the fight was paused still
    /// soaks but never parries. A fight without heavy swings plays exactly as before. Every number here is a
    /// starting value: build first, tune in the tuning weeks.
    public static class HeavyRules
    {
        /// How often a side starts a heavy swing, in ticks (2.5 seconds), counted from the last one's start.
        public const int EveryTicks = 75;

        /// How long a heavy swing winds up before it lands, in ticks (one second): the tell.
        public const int WindupTicks = 30;

        /// A heavy blow hits this percent as hard as the swinger's ordinary blow.
        public const int DamagePercent = 200;

        /// A champion duel's champion's heavy blow hits this percent as hard as his ordinary blow instead (FightRules
        /// ChampionDuel): unparried it hurts enough that whoever you send has to parry to win (DESIGN_LOCKS, "Duels"; Jacob,
        /// 30 Oct 2026: "Go, write 1500"). His parried heavy still cracks ParryNerve.
        public const int DuelDamagePercent = 1500;

        /// Nerve a side loses when one of its heavy swings is parried, out of NerveRules.Full.
        public const int ParryNerve = 40;

        /// Nerve a side loses when its Eclipse is parried, in a fight with heavy swings.
        public const int EclipseParryNerve = 100;

        /// Points a side gets back for parrying a heavy swing: the Hold's own cost.
        public const int ParryPoints = 1;

        /// The Windup event's detail for a heavy swing (an Eclipse's is "Eclipse" or "Pyre").
        public const string Heavy = "Heavy";

        /// The Hit event's detail for a heavy blow.
        public const string HeavyDetail = "heavy";

        /// Arg0 of a directive command given while the fight was paused (CampaignFight.TryOrder, live false).
        public const int PausedOrderArg = 1;
    }

    public sealed partial class BattleSimulation
    {
        /// True when either side swings heavies, which also turns on the other 18 Oct changes (HeavyRules).
        private readonly bool _heavyOn;

        /// True when this fight plays the 18 Oct heavy swing rules. Read only, for the screen.
        public bool HeavyOn => _heavyOn;

        /// Ticks left before a side's heavy swing lands, or 0 when none is winding up. Read only, for the screen.
        public int HeavyTicks(int side)
        {
            return side >= 0 && side <= 1 ? _sides[side].HeavyWindup : 0;
        }

        /// The fighter winding up a side's heavy swing, or empty.
        public string HeavySwingerId(int side)
        {
            return side >= 0 && side <= 1 && _sides[side].HeavyWindup > 0 ? _sides[side].HeavySwingerId : string.Empty;
        }

        /// The fighter a side's heavy swing is aimed at, or empty. If that fighter falls first, the swing takes
        /// whoever the swinger would hit instead.
        public string HeavyTargetId(int side)
        {
            return side >= 0 && side <= 1 && _sides[side].HeavyWindup > 0 ? _sides[side].HeavyTargetId : string.Empty;
        }

        /// True while a side's heavy swing winds up and a Hold timed to its last FightRules.PerfectHoldTicks would
        /// parry it right now: the fight plays Perfect Hold, and the swinging band is neither Unstoppable nor berserk
        /// (BandTrait). Berserk can start in the middle of a windup, so ask every frame. Read only, for the tell
        /// (Codex's request, 18 Oct 2026), so the screen never keeps its own copy of the parry rule.
        public bool HeavyParryable(int side)
        {
            return side >= 0 && side <= 1 && _sides[side].HeavyWindup > 0 && _fight.PerfectHold && !Unparryable(side);
        }

        /// True while a side's heavy swing winds up after it already waited out a fresh Hold once: this is the late
        /// swing, and it will not wait again (WaitsOutParry). Read only; it holds through a save, since a reopened
        /// fight plays its orders back.
        public bool HeavyWaited(int side)
        {
            return side >= 0 && side <= 1 && _sides[side].HeavyWindup > 0 && _sides[side].HeavyWaited;
        }

        /// The same for a side's Eclipse slot cast: true while it winds up after waiting out a Hold once, in a fight
        /// with heavy swings (where a cast waits only once).
        public bool WindupWaited(int side)
        {
            return side >= 0 && side <= 1 && _sides[side].Windup > 0 && _sides[side].WindupWaited;
        }

        /// True while this fighter winds up its side's heavy swing: it does nothing else until the swing lands.
        private bool IsSwingingHeavy(BattleUnit unit)
        {
            SideState state = _sides[unit.Side];
            return state.HeavyWindup > 0 && string.Equals(state.HeavySwingerId, unit.Id, StringComparison.Ordinal);
        }

        /// A side due a heavy swing picks its next fighter in turn that can reach someone, and it starts winding
        /// up. One tell at a time: never while the side's own Eclipse winds up.
        private void TryStartHeavy(int side)
        {
            SideRules rules = _rules[side];
            SideState state = _sides[side];
            if (rules.HeavyEveryTicks <= 0 || _finished || state.HeavyWindup > 0 || state.Windup > 0 || _tick < state.HeavyNextTick)
                return;
            // The Pit: it waits while another parryable move of this side winds up (BattleSimulation.Pit.cs, rule 3).
            if (PitWaits(side)) return;
            int count = _units.Count;
            for (int k = 0; k < count; k++)
            {
                int i = (state.HeavyTurn + k) % count;
                BattleUnit u = _units[i];
                if (!u.Alive || u.Side != side || u.PanicTicks > 0 || !CanSwingHeavy(u) || (_chiefOn && u.Yielded)) continue;
                if (_parryOn && Braced(u)) continue; // a Thick Hide hunter you focus braces and cannot swing
                // A chief with his own kit swings his Cleave instead (BattleSimulation.Kit.cs); his band keeps its turns.
                if (_kitsOn && u.Role == UnitRole.Chief && _rules[side].Kit != ChiefKit.None) continue;
                BattleUnit target = ChooseTarget(u);
                if (target == null) continue;
                state.HeavyTurn = i + 1;
                // A Time Snare slows the swinger's next big move (BattleSimulation.Combos.cs).
                state.HeavyWindup = Slowed(u, HeavyRules.WindupTicks);
                state.HeavySwingerId = u.Id;
                state.HeavyTargetId = target.Id;
                state.HeavyWaited = false;
                state.HeavyNextTick = _tick + rules.HeavyEveryTicks;
                Emit(new BattleEvent(_tick, BattleEventKind.Windup, side, u.Id, (int)u.Class, target.Id, (int)target.Class,
                    state.HeavyWindup, HeavyRules.Heavy));
                return;
            }
            // Nobody can reach anyone this tick (a Fall Back, say): try again on the next.
            state.HeavyNextTick = _tick + 1;
        }

        /// The heavy swing winds on; once its windup has run it lands, unless the side learned to wait out a
        /// fresh Hold and has not waited yet this swing.
        private void AdvanceHeavy(int side)
        {
            SideState state = _sides[side];
            if (state.HeavyWindup <= 0) return;
            BattleUnit swinger = FindById(state.HeavySwingerId);
            // A swinger held by a barbed snare loses the swing too (ClassSpells), and so does one a black hole pins or a freeze
            // holds (BattleSimulation.Combos.cs), and a Thick Hide hunter you focus.
            if (swinger == null || !swinger.Alive || swinger.PanicTicks > 0 || (_classOn && (swinger.SnareTicks > 0 || swinger.PinTicks > 0)) ||
                (_parryOn && Braced(swinger)) || (_fight.Contracts && ActionBlocked(swinger, completingHeavy: true)))
            {
                ClearHeavy(state);
                return;
            }
            state.HeavyWindup--;
            if (state.HeavyWindup > 0) return;
            // Under the parry at the center the landing is its beat (BattleSimulation.Parry.cs). A double swing's heavy second
            // swing never waits or feints: the first one did.
            if (_parryOn)
            {
                int hold = (state.Beats[(int)BeatSlot.Heavy] & BeatSecond) != 0 ? 0 : HoldBeat(side, swinger, BeatSlot.Heavy);
                if (hold > 0) state.HeavyWindup = hold;
                else ReleaseSwing(side, swinger);
                return;
            }
            // A swing nobody can parry has no reason to wait out a Hold.
            if (!state.HeavyWaited && !Unparryable(side) && WaitsOutParry(side))
            {
                int wait = FreshHoldWait(1 - side);
                if (wait > 0)
                {
                    state.HeavyWaited = true;
                    state.HeavyWindup = wait;
                    Emit(new BattleEvent(_tick, BattleEventKind.Reaction, side, swinger.Id, (int)swinger.Class, string.Empty, -1,
                        wait, WaitOutDetail));
                    return;
                }
            }
            ReleaseHeavy(side, swinger);
        }

        /// The heavy blow lands: dodged by a Fall Back, parried by a Hold started in the parry window, soaked
        /// by any other Hold on the front row, or in full.
        private void ReleaseHeavy(int side, BattleUnit swinger)
        {
            SideState state = _sides[side];
            int enemy = 1 - side;
            SideState theirs = _sides[enemy];
            BattleUnit target = FindById(state.HeavyTargetId);
            ClearHeavy(state);

            // Fall Back dodges it, unless the band has Long Reach; a band weak to Fall Back is left off balance.
            if (theirs.FallBackTicks > 0 && !HasBand(side, BandTrait.LongReach))
            {
                Emit(new BattleEvent(_tick, BattleEventKind.Release, side, swinger.Id, (int)swinger.Class,
                    target == null ? string.Empty : target.Id, target == null ? -1 : (int)target.Class, 0, Moves.Dodged));
                if (HasBand(side, BandTrait.WeakToDodge)) OffBalance(side, swinger);
                FavourDodge(enemy, swinger);
                return;
            }
            // The Pit: its mark was locked at its windup, so with the mark fallen it lands on nobody (BattleSimulation.Pit.cs, rule 2).
            if (_pitOn && (target == null || !target.Alive))
            {
                LostSwing(side, swinger, target);
                return;
            }
            if (target == null || !target.Alive) target = ChooseTarget(swinger);
            if (target == null) return;

            if (_fight.PerfectHold && !Unparryable(side) && theirs.HoldTicks > 0 && theirs.LastHoldTick >= 0 &&
                _tick - theirs.LastHoldTick <= ParryWindow(1 - side))
            {
                Emit(new BattleEvent(_tick, BattleEventKind.Release, side, swinger.Id, (int)swinger.Class, target.Id,
                    (int)target.Class, 0, FightRules.Parried));
                Emit(new BattleEvent(_tick, BattleEventKind.Parry, enemy, string.Empty, -1, swinger.Id, (int)swinger.Class,
                    ParryStagger(enemy), string.Empty));
                swinger.Cooldown += ParryStagger(enemy);
                StaggerTimer(swinger, ParryStagger(enemy));
                theirs.HoldTicks = 0;
                theirs.LastParryTick = _tick;
                theirs.ParriedCasterId = swinger.Id;
                Earn(enemy, HeavyRules.ParryPoints, FightRules.ParryReason);
                DrainNerve(side, HasBand(side, BandTrait.WeakToParry)
                    ? HeavyRules.ParryNerve * BandRules.WeakParryPercent / 100
                    : HeavyRules.ParryNerve, enemy);
                return;
            }

            // A Shieldbreaker's swing on a Hold breaks it and lands in full (BandTrait.Shieldbreaker).
            if (theirs.HoldTicks > 0 && HasBand(side, BandTrait.Shieldbreaker))
            {
                theirs.HoldTicks = 0;
                Emit(new BattleEvent(_tick, BattleEventKind.Reaction, side, swinger.Id, (int)swinger.Class, target.Id,
                    (int)target.Class, BandRules.ShieldbreakNerve, BandRules.ShieldbreakDetail));
                DrainNerve(enemy, BandRules.ShieldbreakNerve, side);
                if (_finished) return;
            }

            // An Ambusher's trap springs on it as it lands; upgraded, it stops it (rules 24, BattleSimulation.Whisperer.cs).
            if (_whisperOn && Ambush(enemy, swinger))
            {
                CheckWipe();
                return;
            }
            bool soaked = theirs.HoldTicks > 0 && target.Row == Row.Front;
            int damage = HeavyDamage(swinger, target);
            Emit(new BattleEvent(_tick, BattleEventKind.Release, side, swinger.Id, (int)swinger.Class, target.Id,
                (int)target.Class, damage, soaked ? Moves.Soaked : Moves.Full));
            // A Warden covering the target takes the blow instead, lighter, as with any blow.
            BattleUnit warden = GuardingWarden(target);
            if (warden != null)
            {
                int guarded = damage * (100 - SliceClasses.GuardReductionPercent) / 100;
                Emit(EventFor(BattleEventKind.Guard, warden, target, damage - guarded, string.Empty));
                ApplyDamage(swinger, side, warden, Armored(warden, guarded), GuardedDetail);
            }
            else ApplyDamage(swinger, side, target, Armored(target, damage), HeavyRules.HeavyDetail);
            CheckWipe();
        }

        /// The heavy blow before any cover or armour: the swinger's ordinary blow, doubled (HeavyPercent), with every order,
        /// rally, rise and tribe trait that touches an ordinary blow.
        private int HeavyDamage(BattleUnit swinger, BattleUnit target)
        {
            int power = swinger.Class == UnitClass.Medic
                ? Math.Max(1, SliceClasses.MedicFallbackAttack * swinger.PowerPercent / 100)
                : swinger.Power;
            int damage = power * (85 + _rng.Next(31)) / 100 * HeavyPercent(swinger) / 100;
            SideState mine = _sides[swinger.Side];
            SideState theirs = _sides[target.Side];
            if (mine.UnleashTicks > 0) damage = damage * UnleashPercent(swinger.Side) / 100;
            if (mine.FrenzyTicks > 0)
                damage = damage * (swinger.Role == UnitRole.Pal ? Moves.FrenzyPalPowerPercent : Moves.FrenzyAllyPowerPercent) / 100;
            if (mine.RallyTicks > 0) damage = damage * RallyDamagePercent / 100;
            if (swinger.RiseTicks > 0) damage = damage * FightRules.RiseDamagePercent / 100;
            if (theirs.HoldTicks > 0 && target.Row == Row.Front) damage = damage * HoldSoak(target.Side) / 100;
            if (_bandOn) damage = BandBlow(swinger, target, damage);
            int wounded = SpecOf(swinger).WoundedBonusPercent;
            if (wounded > 0 && target.HpPermille < WoundedBelowPermille) damage = damage * (100 + wounded) / 100;
            return Math.Max(1, damage);
        }

        // How hard a heavy blow hits, in percent of the swinger's ordinary blow: a champion duel's champion hits
        // HeavyRules.DuelDamagePercent.
        private int HeavyPercent(BattleUnit swinger)
        {
            return _duelOn && swinger.Side == 1 ? HeavyRules.DuelDamagePercent : HeavyRules.DamagePercent;
        }

        /// How long a side that learned the parry holds its blow or cast so the enemy's fresh Hold goes stale:
        /// 0 when the enemy has no Hold inside the parry window, or is falling back.
        private int FreshHoldWait(int holdingSide)
        {
            SideState theirs = _sides[holdingSide];
            if (theirs.FallBackTicks > 0 || theirs.HoldTicks <= 0 || theirs.LastHoldTick < 0 ||
                _tick - theirs.LastHoldTick > ParryWindow(holdingSide))
                return 0;
            return (int)(ParryWindow(holdingSide) + 1 - (_tick - theirs.LastHoldTick));
        }

        private static void ClearHeavy(SideState state)
        {
            state.HeavyWindup = 0;
            state.HeavySwingerId = string.Empty;
            state.HeavyTargetId = string.Empty;
            state.HeavyWaited = false;
            state.Beats[(int)BeatSlot.Heavy] = 0;
        }

        /// The heavy swing's beat under the parry at the center (BattleSimulation.Parry.cs): dodged by Fall Back (a safe
        /// answer, a quarter of a parry's crack off the band's nerve), parried, blocked by a stale Hold (it soaks and the
        /// holder's nerve cracks), or in full. A band with Double swing (BandTrait.Double) swings light first and the heavy one
        /// ParryRules.DoubleGapTicks later: Fall Back clears only the first, and parrying both cracks the second twice. Under
        /// the squad first it is a small beat: no crack whether dodged or parried, and landing in full it leaves the chain be.
        /// A champion duel's is a big beat again (BigHeavies; DESIGN_LOCKS, "Duels").
        private void ReleaseSwing(int side, BattleUnit swinger)
        {
            SideState state = _sides[side];
            int enemy = 1 - side;
            SideState theirs = _sides[enemy];
            BattleUnit target = FindById(state.HeavyTargetId);
            string targetId = state.HeavyTargetId;
            int flags = state.Beats[(int)BeatSlot.Heavy];
            bool doubled = HasBand(side, BandTrait.Double);
            bool first = doubled && (flags & BeatSecond) == 0;
            bool second = doubled && !first;
            ClearHeavy(state);
            int crack = HeavyRules.ParryNerve;

            if (theirs.FallBackTicks > 0 && !HasBand(side, BandTrait.LongReach) && !second)
            {
                Emit(new BattleEvent(_tick, BattleEventKind.Release, side, swinger.Id, (int)swinger.Class,
                    target == null ? string.Empty : target.Id, target == null ? -1 : (int)target.Class, 0, Moves.Dodged));
                if (HasBand(side, BandTrait.WeakToDodge)) OffBalance(side, swinger);
                FavourDodge(enemy, swinger);
                if (!_finished && BigHeavies) SafeBeat(side, crack);
                if (first && !_finished) NextSwing(side, swinger, targetId, flags, false);
                return;
            }
            // The Pit: its mark was locked at its windup, so with the mark fallen it lands on nobody, and a double swing's second
            // never comes (BattleSimulation.Pit.cs, rule 2).
            if (_pitOn && (target == null || !target.Alive))
            {
                LostSwing(side, swinger, target);
                return;
            }
            if (target == null || !target.Alive) target = ChooseTarget(swinger);
            if (target == null) return;
            if (theirs.FallBackTicks > 0 && HasBand(side, BandTrait.LongReach)) Fired(side, LessonFiring.LongReach, swinger, target);

            BeatMet met = MeetBeat(enemy);
            if (met == BeatMet.Parry)
            {
                Emit(new BattleEvent(_tick, BattleEventKind.Release, side, swinger.Id, (int)swinger.Class, target.Id,
                    (int)target.Class, 0, FightRules.Parried));
                int parryCrack = second && (flags & BeatFirstParried) != 0 ? crack * 2 : crack;
                if (!BigHeavies) SmallParried(side, swinger, ParryRules.HeavyName);
                else ParriedBeat(side, swinger, ParryRules.HeavyName, parryCrack, HeavyRules.ParryPoints, false);
                if (first && !_finished) NextSwing(side, swinger, target.Id, flags, true);
                return;
            }

            // A Shieldbreaker's swing on a Hold breaks it and lands in full (BandTrait.Shieldbreaker): its own nerve cost
            // stands in for the block's.
            if (theirs.HoldTicks > 0 && HasBand(side, BandTrait.Shieldbreaker))
            {
                theirs.HoldTicks = 0;
                Emit(new BattleEvent(_tick, BattleEventKind.Reaction, side, swinger.Id, (int)swinger.Class, target.Id,
                    (int)target.Class, BandRules.ShieldbreakNerve, BandRules.ShieldbreakDetail));
                DrainNerve(enemy, BandRules.ShieldbreakNerve, side);
                if (_finished) return;
            }
            else if (met == BeatMet.Block)
            {
                BlockedBeat(side, swinger, crack);
                if (_finished) return;
            }
            if (met == BeatMet.Open && BigHeavies) LandedBeat(side);

            // An Ambusher's trap springs on it as it lands; upgraded, it stops it (rules 24, BattleSimulation.Whisperer.cs).
            if (_whisperOn && Ambush(enemy, swinger))
            {
                CheckWipe();
                return;
            }
            bool soaked = theirs.HoldTicks > 0 && target.Row == Row.Front;
            int damage = HeavyDamage(swinger, target);
            if (first) damage = Math.Max(1, damage * ParryRules.DoubleLightPercent / 100);
            Emit(new BattleEvent(_tick, BattleEventKind.Release, side, swinger.Id, (int)swinger.Class, target.Id,
                (int)target.Class, damage, soaked ? Moves.Soaked : Moves.Full));
            BattleUnit warden = GuardingWarden(target);
            if (warden != null)
            {
                int guarded = damage * (100 - SliceClasses.GuardReductionPercent) / 100;
                Emit(EventFor(BattleEventKind.Guard, warden, target, damage - guarded, string.Empty));
                // Under road combat a Warden's Brace takes its share off what his Guard pulled onto him
                // (BattleSimulation.Defence.cs).
                guarded = HeavyDefence(swinger, warden, guarded);
                if (guarded > 0) ApplyDamage(swinger, side, warden, Armored(warden, guarded), GuardedDetail);
            }
            else
            {
                // Under road combat its target's Brace or Sidestep (BattleSimulation.Defence.cs).
                damage = HeavyDefence(swinger, target, damage);
                if (damage > 0) ApplyDamage(swinger, side, target, Armored(target, damage), HeavyRules.HeavyDetail);
            }
            if (first && !_finished) NextSwing(side, swinger, target.Id, flags, false);
            CheckWipe();
        }

        // A double swing's heavy second swing comes DoubleGapTicks after the light first, at the same mark.
        private void NextSwing(int side, BattleUnit swinger, string targetId, int flags, bool parried)
        {
            if (!swinger.Alive || _halted) return;
            SideState state = _sides[side];
            state.HeavyWindup = ParryRules.DoubleGapTicks;
            state.HeavySwingerId = swinger.Id;
            state.HeavyTargetId = targetId;
            state.Beats[(int)BeatSlot.Heavy] = flags | BeatSecond | (parried ? BeatFirstParried : 0);
            BattleUnit target = FindById(targetId);
            Emit(EventFor(BattleEventKind.Reaction, swinger, target, ParryRules.DoubleGapTicks, ParryRules.DoubleDetail));
            Fired(side, LessonFiring.Double, swinger, target);
        }
    }
}
