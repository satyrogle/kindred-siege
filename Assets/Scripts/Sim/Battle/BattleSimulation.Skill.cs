using System;
using System.Collections.Generic;

namespace KindredSiege.Sim.Battle
{
    // The 13 Oct 2026 fight rules (FightRules: misses, earned points, Perfect Hold, nerve snaps) and the
    // tribes' doctrines (TacticsProfile: Rally, GuardCaster, HuntWounded, HoldFormation, Kite, PickOffBack).
    // A fight without FightRules and without doctrines never reaches any of it: no extra roll, no event,
    // no change to its snapshot, so older fights replay exactly.
    public sealed partial class BattleSimulation
    {
        private readonly FightRules _fight;
        private readonly bool _skillOn;

        /// Rally: when one of a side's fighters falls, the rest hit this percent as hard for RallyTicks.
        public const int RallyTicks = 90;
        public const int RallyDamagePercent = 125;

        /// PickOffBack: a ranged fighter's score for an enemy in the back row, per point of doctrine.
        public const int PickOffBackScore = 120;

        /// Reaction details, one per doctrine that acts (Rally, GuardCaster, HoldFormation, Kite). The
        /// two that only change who gets hit (HuntWounded, PickOffBack) show in Doctrines instead.
        public const string RallyDetail = "rally";
        public const string GuardCasterDetail = "guardcaster";
        public const string HoldFormationDetail = "holdformation";
        public const string KiteDetail = "kite";
        public const string HuntWoundedDetail = "huntwounded";
        public const string PickOffBackDetail = "pickoffback";

        /// This fight's 13 Oct rules. FightRules.Off unless the setup turned them on.
        public FightRules Fight => _fight;

        /// The doctrines a side fights with, as reaction details, for the screen. Empty for none.
        public IReadOnlyList<string> Doctrines(int side)
        {
            var list = new List<string>();
            TacticsProfile t = _tactics[side];
            if (t.Rally >= HabitThreshold) list.Add(RallyDetail);
            if (t.GuardCaster >= HabitThreshold) list.Add(GuardCasterDetail);
            if (t.HuntWounded >= HabitThreshold) list.Add(HuntWoundedDetail);
            if (t.HoldFormation >= HabitThreshold) list.Add(HoldFormationDetail);
            if (t.Kite >= HabitThreshold) list.Add(KiteDetail);
            if (t.PickOffBack >= HabitThreshold) list.Add(PickOffBackDetail);
            return list;
        }

        /// The chance, in percent, that this fighter's next blow on that enemy lands; 100 while misses
        /// are off. Read only. Never shown as a number (Jacob, 13 Oct 2026); for tests and bots.
        public int HitChance(BattleUnit attacker, BattleUnit target)
        {
            // From rules 42 a blow lands unless the attack table dodges or parries it (BattleSimulation.Table.cs).
            if (_fight.Table && attacker != null && target != null) return 100 - DodgeChance(attacker, target) - ParryChance(attacker, target);
            if (!_fight.Misses || attacker == null || target == null) return 100;
            int chance = SliceClasses.Hit(attacker.Class) + SpecOf(attacker).HitBonus -
                SliceClasses.Avoid(target.Class) - SpecOf(target).AvoidBonus;
            // The Whisperer's and the Glutton's favours add to these (SideFavours, 18 Oct 2026).
            if (IsFocusing(attacker.Side, target)) chance += FightRules.FocusHitBonus + _rules[attacker.Side].Favours.FocusHit;
            if (_sides[target.Side].HoldTicks > 0 && target.Row == Row.Front)
                chance -= FightRules.HoldAvoidBonus + _rules[target.Side].Favours.HoldAvoid;
            // Red sand whirls round a side whose Dust Shark is winding up: blows on it mostly miss (Jacob, 18 Oct 2026).
            if (_sides[target.Side].Windup > 0 && _rules[target.Side].Spell == ChiefSpell.DustShark) chance -= ChiefSpells.DustAvoid;
            // So do blows on a beast while it is under the sand (BeastKits, 21 Oct 2026).
            if (_beastOn && target.Side == _beastSide && BeastUnder) chance -= BeastKits.DiveAvoid;
            // An evasive band slips plain blows, but never an unleashed one (BandTrait.Evasive).
            if (HasBand(target.Side, BandTrait.Evasive))
            {
                if (_sides[attacker.Side].UnleashTicks > 0) return 100;
                chance -= BandRules.EvasiveAvoid;
            }
            return Math.Max(FightRules.MinHitPercent, Math.Min(FightRules.MaxHitPercent, chance));
        }

        /// The next order that finishes a combo this side has started, or ComboHint.None. Read only, so
        /// the screen can light the button (13 Oct 2026). Riposte first, then Ambush, then Pyre.
        public ComboHint NextCombo(int side)
        {
            if (_finished || side < 0 || side > 1) return ComboHint.None;
            SideState s = _sides[side];
            if (!_rules[side].Combos) return ComboHint.None;
            if (_fight.PerfectHold && s.LastParryTick >= 0)
            {
                long left = FightRules.RiposteWindowTicks - (_tick - s.LastParryTick);
                BattleUnit caster = FindById(s.ParriedCasterId);
                if (left >= 0 && caster != null && caster.Alive)
                    return new ComboHint(FightRules.Riposte, Directive.Unleash, (int)left, 1, 2);
            }
            if (!s.AmbushUsed && s.LastFallBackTick >= 0)
            {
                long left = Moves.AmbushWindowTicks - (_tick - s.LastFallBackTick);
                if (left >= 0) return new ComboHint(Moves.Ambush, Directive.Unleash, (int)left, 1, 2);
            }
            if (!s.PyreUsed && s.LastFocusTick >= 0 && _rules[side].EclipseGrade > 0 && Leader(side) != null)
            {
                long left = Moves.PyreWindowTicks - (_tick - s.LastFocusTick);
                BattleUnit focused = FindById(s.LastFocusId);
                if (left >= 0 && focused != null && focused.Alive)
                {
                    bool unleashed = s.LastUnleashTick >= s.LastFocusTick;
                    return new ComboHint(Moves.Pyre, unleashed ? Directive.Eclipse : Directive.Unleash, (int)left,
                        unleashed ? 2 : 1, 3);
                }
            }
            return ComboHint.None;
        }

        // ------------------------------------------------------------ misses

        /// Rolls the blow. True, with a Miss event, when it misses.
        private bool Missed(BattleUnit attacker, BattleUnit target)
        {
            int chance = HitChance(attacker, target);
            if (_rng.Next(100) < chance) return false;
            Emit(new BattleEvent(_tick, BattleEventKind.Miss, attacker.Side, attacker.Id, (int)attacker.Class,
                target.Id, (int)target.Class, chance, FightRules.MissDetail));
            return true;
        }

        // ------------------------------------------------------------ earned points

        /// Gives a side earned points, never above its starting points, with an Earn event when any came.
        private void Earn(int side, int points, string why)
        {
            if (!_fight.EarnedPoints || _finished || side < 0 || side > 1 || points <= 0) return;
            // Under the raid chief your parry pays in his bar, not in points (BattleSimulation.Raid.cs).
            if (_raidOn && side == 0 && string.Equals(why, FightRules.ParryReason, StringComparison.Ordinal)) return;
            SideState state = _sides[side];
            int before = state.Points;
            state.Points = Math.Min(_pointCap, state.Points + points);
            if (state.Points > before)
                Emit(new BattleEvent(_tick, BattleEventKind.Earn, side, string.Empty, -1, string.Empty, -1,
                    state.Points - before, why));
        }

        /// Ticks between trickled points: slower once points are earned, a little less slow under the squad first.
        private int RefillEvery => !_fight.EarnedPoints ? _refillTicks
            : _refillTicks * (_squadOn ? ParryRules.SquadTrickleFactor : FightRules.TrickleFactor);

        // ------------------------------------------------------------ Perfect Hold

        /// The big hit lands on a side that started its Hold within PerfectHoldTicks: parried. Nothing
        /// lands, the Hold ends there (so it costs no more nerve), the caster staggers, and the side earns.
        private bool TryParry(int side, BattleUnit caster, int percent)
        {
            int enemy = 1 - side;
            SideState theirs = _sides[enemy];
            if (!_fight.PerfectHold || theirs.HoldTicks <= 0 || theirs.LastHoldTick < 0 ||
                _tick - theirs.LastHoldTick > FightRules.PerfectHoldTicks)
                return false;
            Emit(new BattleEvent(_tick, BattleEventKind.Release, side, caster.Id, (int)caster.Class, string.Empty, -1,
                percent, FightRules.Parried));
            Emit(new BattleEvent(_tick, BattleEventKind.Parry, enemy, string.Empty, -1, caster.Id, (int)caster.Class,
                ParryStagger(enemy), string.Empty));
            caster.Cooldown += ParryStagger(enemy);
            StaggerTimer(caster, ParryStagger(enemy));
            theirs.HoldTicks = 0;
            theirs.LastParryTick = _tick;
            theirs.ParriedCasterId = caster.Id;
            Earn(enemy, FightRules.ParryPoints, FightRules.ParryReason);
            // In a fight with heavy swings a parry also cracks the caster's side (Jacob, 18 Oct 2026: parry wins), twice
            // as hard for a band weak to the parry.
            if (_heavyOn)
                DrainNerve(side, HasBand(side, BandTrait.WeakToParry)
                    ? HeavyRules.EclipseParryNerve * BandRules.WeakParryPercent / 100
                    : HeavyRules.EclipseParryNerve, enemy);
            return true;
        }

        /// Parry, then an ordered Unleash within the window: the staggered caster is struck.
        private void TryRiposte(int side)
        {
            SideState state = _sides[side];
            if (_finished || !_fight.PerfectHold || !_rules[side].Combos || state.LastParryTick < 0 ||
                _tick - state.LastParryTick > FightRules.RiposteWindowTicks)
                return;
            BattleUnit caster = FindById(state.ParriedCasterId);
            state.LastParryTick = -1;
            state.ParriedCasterId = string.Empty;
            if (caster == null || !caster.Alive) return;
            int damage = Math.Max(1, caster.MaxHp * FightRules.RipostePercent / 100);
            Emit(new BattleEvent(_tick, BattleEventKind.Combo, side, string.Empty, -1, caster.Id, (int)caster.Class,
                damage, FightRules.Riposte));
            Earn(side, FightRules.ComboPoints, FightRules.ComboReason);
            ApplyDamage(null, side, caster, damage, FightRules.RiposteDetail);
            // An Unshakable band only takes the cut (BandTrait.Unshakable).
            if (!_finished && !HasBand(caster.Side, BandTrait.Unshakable)) DrainNerve(caster.Side, FightRules.RiposteNerveCost, side);
            CheckWipe();
        }

        /// Reaction detail when a caster waits out a fresh Hold (14 Oct 2026). Amount is how many ticks it waits.
        public const string WaitOutDetail = "waitout";

        /// True when this side's casters have learned to wait out a parry: a perfect Hold against them only
        /// soaks. Read only, for the screen (Jacob, 14 Oct 2026: "yes go build the parry learning").
        public bool WaitsOutParry(int side)
        {
            return _fight.PerfectHold && side >= 0 && side <= 1 && _tactics[side].WaitOutParry >= HabitThreshold;
        }

        /// The cast is due, but the enemy started a Hold within the parry window. A side that learned the parry
        /// holds the release until that Hold is older than PerfectHoldTicks, so it lands soaked. True, with a
        /// Reaction event, when it waits. A fresh Hold given during the wait makes it wait again, at a point each.
        /// In a fight with heavy swings it waits only once a cast (18 Oct 2026): it swings late, and a second Hold
        /// timed to the late release still parries.
        private bool WaitOutHold(int side, BattleUnit caster)
        {
            SideState theirs = _sides[1 - side];
            if (_rules[side].Spell != ChiefSpell.Eclipse) return false; // only the Eclipse can be parried
            if (_heavyOn && _sides[side].WindupWaited) return false;
            if (!WaitsOutParry(side) || theirs.FallBackTicks > 0 || theirs.HoldTicks <= 0 || theirs.LastHoldTick < 0 ||
                _tick - theirs.LastHoldTick > FightRules.PerfectHoldTicks)
                return false;
            int wait = (int)(FightRules.PerfectHoldTicks + 1 - (_tick - theirs.LastHoldTick));
            if (_heavyOn) _sides[side].WindupWaited = true;
            _sides[side].Windup = wait;
            Emit(new BattleEvent(_tick, BattleEventKind.Reaction, side, caster.Id, (int)caster.Class, string.Empty, -1,
                wait, WaitOutDetail));
            return true;
        }

        // ------------------------------------------------------------ kills, nerve and snaps

        /// A fighter fell: its killer's side earns, and a rallying side hits harder for a while.
        private void OnKill(int killerSide, BattleUnit victim)
        {
            if (killerSide == 1 - victim.Side) Earn(killerSide, FightRules.KillPoints, FightRules.KillReason);
            int side = victim.Side;
            if (_finished || _tactics[side].Rally < HabitThreshold || AliveCount(side) == 0) return;
            _sides[side].RallyTicks = RallyTicks;
            Emit(new BattleEvent(_tick, BattleEventKind.Reaction, side, string.Empty, -1, victim.Id, (int)victim.Class,
                RallyTicks, RallyDetail));
        }

        /// After a side loses nerve without breaking: a side that holds formation Holds the first time
        /// it falls below half, and each quarter of its starting nerve lost snaps one of its fighters.
        private void AfterNerveLoss(int side)
        {
            SideState state = _sides[side];
            if (_tactics[side].HoldFormation >= HabitThreshold && !state.FormationHeld && state.Nerve * 2 < NerveRules.Full)
            {
                state.FormationHeld = true;
                if (state.HoldTicks == 0 && state.Points >= CostOf(side, Directive.Hold))
                {
                    Emit(new BattleEvent(_tick, BattleEventKind.Reaction, side, string.Empty, -1, string.Empty, -1, 0,
                        HoldFormationDetail));
                    ApplyDirective(side, Directive.Hold, null, AutoActor(side));
                }
            }
            if (!_fight.NerveSnaps) return;
            int start = _setup.Nerve.StartNerve(side);
            while (!_finished && state.SnapsTaken < FightRules.SnapsPerFight &&
                   state.Nerve < start - (state.SnapsTaken + 1) * start / (FightRules.SnapsPerFight + 1))
            {
                state.SnapsTaken++;
                Snap(side);
            }
        }

        /// The side's most hurt fighter (not its leader, not one already snapped) panics or rises.
        private void Snap(int side)
        {
            BattleUnit unit = null;
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (!u.Alive || u.Side != side || u.IsLeader || u.PanicTicks > 0 || u.RiseTicks > 0) continue;
                if (unit == null || u.HpPermille < unit.HpPermille) unit = u;
            }
            if (unit == null) return;
            int rise = FightRules.BaseRisePercent;
            if (_sides[side].HoldTicks > 0) rise += FightRules.SteadyRiseBonus;
            if (_sides[1 - side].UnleashTicks > 0) rise -= FightRules.ShakenRisePenalty;
            // A steady fighter (Learning.Quirks) rises far more often.
            rise += SpecOf(unit).RiseBonus;
            bool rises = _rng.Next(100) < rise;
            if (rises) unit.RiseTicks = FightRules.SnapTicks;
            else unit.Statuses.Apply(StatusKind.Panic, FightRules.SnapTicks);
            Emit(EventFor(BattleEventKind.Snap, unit, null, FightRules.SnapTicks, rises ? FightRules.Rise : FightRules.Panic));
            if (!rises) Earn(1 - side, FightRules.PanicPoints, FightRules.PanicReason);
        }

        private void CountDownSnaps()
        {
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                u.Statuses.CountDown(StatusKind.Panic);
                if (u.RiseTicks > 0) u.RiseTicks--;
            }
        }

        // ------------------------------------------------------------ doctrines

        /// HuntWounded and PickOffBack: how much more this fighter wants that enemy. 0 without them.
        private int DoctrineScore(BattleUnit attacker, BattleUnit enemy, TacticsProfile tactics)
        {
            int score = 0;
            if (tactics.HuntWounded >= HabitThreshold) score += (1000 - enemy.HpPermille) * tactics.HuntWounded / 10;
            if (tactics.PickOffBack >= HabitThreshold && attacker.Stats.Ranged && enemy.Row == Row.Back)
                score += tactics.PickOffBack * PickOffBackScore;
            return score;
        }

        /// GuardCaster: while this Warden's side winds up a big move, it covers the caster. Null otherwise.
        private BattleUnit GuardedCaster(BattleUnit warden)
        {
            SideState own = _sides[warden.Side];
            if (_tactics[warden.Side].GuardCaster < HabitThreshold || own.Windup <= 0) return null;
            BattleUnit caster = FindById(own.CasterId);
            return caster != null && caster.Alive && !ReferenceEquals(caster, warden) ? caster : null;
        }

        /// GuardCaster, the moment a cast starts: every Warden on the side turns to cover the caster.
        private void ReactGuardCaster(int side, BattleUnit caster)
        {
            if (_tactics[side].GuardCaster < HabitThreshold) return;
            bool any = false;
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (!u.Alive || u.Side != side || u.Class != UnitClass.Warden || ReferenceEquals(u, caster)) continue;
                u.GuardTargetId = caster.Id;
                any = true;
            }
            if (any)
                Emit(new BattleEvent(_tick, BattleEventKind.Reaction, side, string.Empty, -1, caster.Id, (int)caster.Class,
                    0, GuardCasterDetail));
        }

        /// Kite: the enemy unleashed, so this side falls back out of its way, unless it already holds.
        private void ReflexKite(int side)
        {
            if (_finished || _tactics[side].Kite < HabitThreshold) return;
            SideState state = _sides[side];
            if (state.HoldTicks > 0 || state.FallBackTicks > 0 || state.Points < Directives.Cost(Directive.FallBack)) return;
            Emit(new BattleEvent(_tick, BattleEventKind.Reaction, side, string.Empty, -1, string.Empty, -1, 0, KiteDetail));
            ApplyDirective(side, Directive.FallBack, null, AutoActor(side));
        }
    }
}
