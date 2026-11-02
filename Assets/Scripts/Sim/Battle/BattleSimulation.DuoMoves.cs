using System;
using System.Collections.Generic;

namespace KindredSiege.Sim.Battle
{
    /// The duos that change a move (campaign rules 34; FightRules.DuoMoves; Jacob in chat, 2 Nov 2026: "merge first, then build
    /// the duos on top. Go", after "take some pointers from returnal and nex machina"). Each side brings its duos and its streak
    /// in its PalKit. The streak: each big move of theirs your side answers (a parry, a Fall Back that dodges it, a Focus Fire
    /// that breaks it) raises it by one, up to DuoRules.MaxLevel; one that lands in full on you, or one of yours falling, drops it
    /// to 1. Every duo grows with it, and every duo has its catch. A side without one of these duos keeps no streak. Every
    /// number is a starting value.
    public sealed partial class BattleSimulation
    {
        private readonly bool _duoMovesOn;

        // How deep the chain of bursts this tick has gone (Chain Reaction). Never set between ticks.
        private int _chainDepth;

        /// Whether this fight plays the rules 34 duos.
        public bool DuoMovesOn => _duoMovesOn;

        /// This side's streak: 1 to DuoRules.MaxLevel while it holds one of the rules 34 duos, else 0.
        public int DuoLevelOf(int side) => _duoMovesOn && side >= 0 && side <= 1 ? _sides[side].DuoLevel : 0;

        /// Endless Unleash: the stacks this Unleash has gathered, and how long this side is still tired after one.
        public int UnleashStacksOf(int side) => _duoMovesOn && side >= 0 && side <= 1 ? _sides[side].UnleashStacks : 0;
        public int TiredTicksLeft(int side) => _duoMovesOn && side >= 0 && side <= 1 ? _sides[side].TiredTicks : 0;

        /// Gorge: how many percent this fighter has grown.
        public int GorgedOf(BattleUnit unit) => _duoMovesOn && unit != null ? unit.GorgePercent : 0;

        /// Shadow Twin: whether this fighter's shadow fights on while his side falls back.
        public bool ShadowFighting(BattleUnit unit) => ShadowFights(unit) && FallingBack(unit.Side);

        private bool HasMove(int side, PalDuo duo) => _duoMovesOn && side >= 0 && side <= 1 && KitOf(side).HasDuo(duo);

        private bool AnyMove(int side)
        {
            for (int d = (int)DuoRules.FirstMove; d < DuoRules.Count; d++) if (HasMove(side, (PalDuo)d)) return true;
            return false;
        }

        // At the start: each side with one of these duos starts at its kit's streak, at least 1; Shadow Twin halves its star.
        private void StartDuoMoves()
        {
            for (int side = 0; side < 2; side++)
            {
                if (!AnyMove(side)) continue;
                _sides[side].DuoLevel = Math.Max(1, Math.Min(DuoRules.MaxLevel, KitOf(side).DuoLevel));
                if (!HasMove(side, PalDuo.ShadowTwin)) continue;
                BattleUnit star = StarOf(side);
                if (star == null) continue;
                star.MaxHp = Math.Max(1, star.MaxHp / 2);
                star.Hp = Math.Min(star.Hp, star.MaxHp);
            }
        }

        // ------------------------------------------------------------ the streak

        // Every event passes here (Emit): an answered big move raises the streak, one that landed in full or a fallen fighter
        // drops it.
        private void DuoStreak(BattleEvent e)
        {
            switch (e.Kind)
            {
                case BattleEventKind.Parry:
                case BattleEventKind.Interrupt:
                    Answered(e.ActorSide);
                    return;
                case BattleEventKind.Release:
                    if (e.ActorSide < 0 || e.ActorSide > 1) return;
                    if (e.Detail == Moves.Dodged) Answered(1 - e.ActorSide);
                    else if (e.Detail == Moves.Full) Missed(1 - e.ActorSide);
                    return;
                case BattleEventKind.Land:
                    if (e.ActorSide < 0 || e.ActorSide > 1 || e.Detail == null) return;
                    if (e.Detail.EndsWith(".dodged", StringComparison.Ordinal)) Answered(1 - e.ActorSide);
                    else if (e.Detail.EndsWith(".full", StringComparison.Ordinal)) Missed(1 - e.ActorSide);
                    return;
                case BattleEventKind.Kill:
                    if (e.ActorSide >= 0 && e.ActorSide <= 1) Missed(1 - e.ActorSide);
                    return;
            }
        }

        private void Answered(int side)
        {
            if (side < 0 || side > 1 || _sides[side].DuoLevel <= 0) return;
            SideState state = _sides[side];
            state.DuoLevel = Math.Min(DuoRules.MaxLevel, state.DuoLevel + 1);
            // Endless Unleash: the answer feeds an Unleash that runs.
            if (HasMove(side, PalDuo.EndlessUnleash) && state.UnleashTicks > 0)
            {
                state.UnleashTicks += DuoRules.EndlessTicks * state.DuoLevel;
                state.UnleashStacks++;
                Emit(new BattleEvent(_tick, BattleEventKind.Whisper, side, string.Empty, -1, string.Empty, -1, state.UnleashStacks,
                    DuoRules.EndlessDetail));
            }
        }

        private void Missed(int side)
        {
            if (side < 0 || side > 1 || _sides[side].DuoLevel <= 0) return;
            _sides[side].DuoLevel = 1;
        }

        // ------------------------------------------------------------ Blood Parry (Butcher and Shade)

        // A parry by this side: every mark on the one parried bursts, at level 2 on those beside him too, at level 3 on his row.
        private void BloodParry(int side, BattleUnit doer)
        {
            if (!HasMove(side, PalDuo.BloodParry) || doer == null || _finished) return;
            int level = _sides[side].DuoLevel;
            var targets = new List<BattleUnit> { doer };
            if (level >= 3)
            {
                for (int i = 0; i < _units.Count; i++)
                    if (_units[i].Alive && _units[i].Side == doer.Side && _units[i].Row == doer.Row && !ReferenceEquals(_units[i], doer)) targets.Add(_units[i]);
            }
            else if (level >= 2)
            {
                List<BattleUnit> beside = BrandedAndBeside(doer);
                for (int k = 1; k < beside.Count; k++) if (beside[k].Side == doer.Side) targets.Add(beside[k]);
            }
            int burst = 0;
            for (int k = 0; k < targets.Count && !_finished; k++) burst += Detonate(side, targets[k]);
            if (burst > 0) Emit(new BattleEvent(_tick, BattleEventKind.Burst, side, string.Empty, -1, doer.Id, (int)doer.Class, burst, DuoRules.BloodParryDetail));
        }

        // Bursts every mark on him now, whatever its stacks, for the side's wearer of that flavour. How many burst.
        private int Detonate(int side, BattleUnit target)
        {
            int n = 0;
            for (StatusKind mark = StatusKind.Crush; mark <= StatusKind.Stasis && !_finished; mark++)
            {
                if (!target.Alive || target.Statuses[mark].Stacks <= 0) continue;
                BattleUnit by = MarkLayer(side, mark);
                if (by == null) continue;
                Burst(by, target, mark);
                n++;
            }
            return n;
        }

        // Who a burst of this mark is for on this side: its first standing wearer of a combo of that flavour, else its first
        // standing fighter.
        private BattleUnit MarkLayer(int side, StatusKind mark)
        {
            BattleUnit any = null;
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (!u.Alive || u.Side != side || u.Benched) continue;
                if ((ClassSpells.IsCombo(BendOf(u)) && MarkRules.Of(BendOf(u)) == mark) ||
                    (ClassSpells.IsCombo(Bend2Of(u)) && MarkRules.Of(Bend2Of(u)) == mark)) return u;
                if (any == null) any = u;
            }
            return any;
        }

        // Blood Parry's catch: Fall Back costs double.
        private int BloodParryCost(int side, Directive directive, int cost)
        {
            return directive == Directive.FallBack && HasMove(side, PalDuo.BloodParry) ? cost * 2 : cost;
        }

        // ------------------------------------------------------------ Chain Reaction (Butcher and Glutton)

        // A burst went off on him: a stack of its mark splashes onto one beside him, both at level 2, his whole row at 3, and
        // any it brings to the cap bursts too, a few deep.
        private void Chain(BattleUnit by, BattleUnit target, StatusKind mark)
        {
            if (!HasMove(by.Side, PalDuo.ChainReaction) || _chainDepth >= DuoRules.ChainDepth || _finished) return;
            int level = _sides[by.Side].DuoLevel;
            var near = new List<BattleUnit>();
            if (level >= 3)
            {
                for (int i = 0; i < _units.Count; i++)
                    if (_units[i].Alive && _units[i].Side == target.Side && _units[i].Row == target.Row && !ReferenceEquals(_units[i], target)) near.Add(_units[i]);
            }
            else
            {
                List<BattleUnit> beside = BrandedAndBeside(target);
                for (int k = 1; k < beside.Count && near.Count < level; k++) if (beside[k].Side == target.Side) near.Add(beside[k]);
            }
            if (near.Count == 0) return;
            Emit(new BattleEvent(_tick, BattleEventKind.Burst, by.Side, by.Id, (int)by.Class, target.Id, (int)target.Class, near.Count, DuoRules.ChainDetail));
            _chainDepth++;
            for (int k = 0; k < near.Count && !_finished; k++)
            {
                BattleUnit u = near[k];
                if (!u.Alive || ShrugsMarks(u)) continue;
                u.Statuses.Apply(mark, MarkRules.Ticks, sourceId: by.Id, stacks: 1);
                if (u.Statuses[mark].Stacks >= MarkRules.MaxStacks) Burst(by, u, mark);
            }
            _chainDepth--;
        }

        // ------------------------------------------------------------ Executioner (Butcher and Whisperer)

        // His side's Focus Fire target fell: his marks jump to one other, three at level 2, all of them at 3.
        private void Executed(int killerSide, BattleUnit victim)
        {
            if (!HasMove(killerSide, PalDuo.Executioner) || _finished) return;
            SideState state = _sides[killerSide];
            if (state.FocusTicks <= 0 || !string.Equals(state.FocusTargetId, victim.Id, StringComparison.Ordinal)) return;
            int level = state.DuoLevel;
            int count = level >= 3 ? int.MaxValue : level >= 2 ? 3 : 1;
            List<BattleUnit> next = Nearest(victim, count == int.MaxValue ? _units.Count : count);
            int jumped = 0;
            for (StatusKind mark = StatusKind.Crush; mark <= StatusKind.Stasis && !_finished; mark++)
            {
                int stacks = victim.Statuses[mark].Stacks;
                if (stacks <= 0) continue;
                BattleUnit by = MarkLayer(killerSide, mark);
                if (by == null) continue;
                for (int k = 0; k < next.Count && !_finished; k++)
                {
                    BattleUnit u = next[k];
                    if (!u.Alive || ShrugsMarks(u)) continue;
                    u.Statuses.Apply(mark, MarkRules.Ticks, sourceId: by.Id, stacks: stacks);
                    jumped++;
                    if (u.Statuses[mark].Stacks >= MarkRules.MaxStacks) Burst(by, u, mark);
                }
            }
            if (jumped > 0) Emit(new BattleEvent(_tick, BattleEventKind.Burst, killerSide, string.Empty, -1, victim.Id, (int)victim.Class, jumped,
                DuoRules.ExecutionerDetail));
        }

        // Executioner's catch: the star takes more.
        private int ExecutionerTaken(BattleUnit victim)
        {
            return HasMove(victim.Side, PalDuo.Executioner) && IsStar(victim) ? DuoRules.ExecutionerTakenPercent : 0;
        }

        // ------------------------------------------------------------ Endless Unleash (Shade and Glutton)

        // How many percent bigger this side's blows are from this Unleash's stacks.
        private int EndlessPercent(int side)
        {
            if (!HasMove(side, PalDuo.EndlessUnleash) || _sides[side].UnleashTicks <= 0) return 0;
            return _sides[side].UnleashStacks * DuoRules.EndlessPercent;
        }

        // Endless Unleash's catch: once an Unleash ends, the side is tired, at half speed for TiredTicks.
        private bool Tired(BattleUnit unit)
        {
            return _duoMovesOn && unit.Side >= 0 && unit.Side <= 1 && _sides[unit.Side].TiredTicks > 0 && (_tick & 1L) == 1L;
        }

        // ------------------------------------------------------------ Shadow Twin (Whisperer and Shade)

        // Whether his shadow fights on while his side falls back: the star of a side with Shadow Twin.
        private bool ShadowFights(BattleUnit unit)
        {
            return unit != null && HasMove(unit.Side, PalDuo.ShadowTwin) && IsStar(unit);
        }

        // His shadow's blows while his side falls back, a percent cut by level.
        private int ShadowCut(BattleUnit attacker)
        {
            if (!ShadowFights(attacker) || !FallingBack(attacker.Side)) return 0;
            int level = _sides[attacker.Side].DuoLevel;
            int percent = level >= 3 ? DuoRules.ShadowPercent3 : level >= 2 ? DuoRules.ShadowPercent2 : DuoRules.ShadowPercent1;
            return percent - 100;
        }

        // ------------------------------------------------------------ Gorge (Whisperer and Glutton)

        // A kill: the star of a side with Gorge grows, GorgePercent a level, health and blows, healed by what he grew. His
        // fall afterwards shakes his side (Gorged).
        private void Gorged(BattleUnit source, int sourceSide, BattleUnit victim)
        {
            if (_finished) return;
            if (source != null && source.Side == sourceSide && source.Alive && HasMove(sourceSide, PalDuo.Gorge) && IsStar(source))
            {
                int grow = DuoRules.GorgePercent * _sides[sourceSide].DuoLevel;
                source.GorgePercent += grow;
                int more = Math.Max(1, source.MaxHp * grow / 100);
                source.MaxHp += more;
                source.Hp = Math.Min(source.MaxHp, source.Hp + more);
                Emit(EventFor(BattleEventKind.Whisper, source, victim, source.GorgePercent, DuoRules.GorgeDetail));
            }
            // The catch: a gorged star falling shakes every fighter of his side.
            if (victim.GorgePercent > 0 && HasMove(victim.Side, PalDuo.Gorge))
            {
                int kills = Math.Max(1, victim.GorgePercent / DuoRules.GorgePercent);
                int drain = DuoRules.GorgeNervePerKill * kills;
                if (_unitNerveOn)
                {
                    for (int i = 0; i < _units.Count && !_finished; i++)
                        if (_units[i].Alive && _units[i].Side == victim.Side) UnitDrain(_units[i], drain, sourceSide);
                }
                else if (_nerveOn) DrainNerve(victim.Side, drain, sourceSide);
            }
        }

        // ------------------------------------------------------------ each tick and the snapshot

        // After the fighters act: an Unleash that ended tires a side with Endless Unleash; the tiredness runs down.
        private void TickDuoMoves()
        {
            for (int side = 0; side < 2; side++)
            {
                SideState state = _sides[side];
                if (state.TiredTicks > 0) state.TiredTicks--;
                if (!HasMove(side, PalDuo.EndlessUnleash)) continue;
                if (state.UnleashTicks > 0)
                {
                    state.UnleashRan = true;
                    continue;
                }
                if (!state.UnleashRan) continue;
                state.UnleashRan = false;
                state.UnleashStacks = 0;
                state.TiredTicks = DuoRules.TiredTicks;
            }
        }

        private void AppendDuoMovesSide(SnapshotBuilder b, SideState state)
        {
            b.Field("duoLevel", state.DuoLevel);
            b.Field("unleashStacks", state.UnleashStacks);
            b.Field("unleashRan", state.UnleashRan);
            b.Field("tired", state.TiredTicks);
        }

        private void AppendDuoMovesUnit(SnapshotBuilder b, BattleUnit u)
        {
            b.Field(u.Id + ".gorged", u.GorgePercent);
        }
    }
}
