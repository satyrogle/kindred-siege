using System;
using System.Collections.Generic;

namespace KindredSiege.Sim.Battle
{
    /// The chief rules (Jacob, 20 Oct 2026, later: chiefs harder "by adding more rules and gameplay"; he called these three
    /// the basics that should already be in the game). Only in fights with FightRules.ChiefRules, which the campaign turns
    /// on for its chief fights:
    /// - Second phase: at SecondPhasePercent of his health a chief gets back up angrier. Up to Reserves of his reserves
    ///   (UnitSpec.Reserve) join the fight, and he starts his chief spell again at once.
    /// - Yield: at YieldPercent of his health he kneels. He does nothing more, and his side loses YieldNervePerSecond nerve
    ///   a second. While your side holds, your fighters spare him and strike someone else, so his nerve breaks and you
    ///   capture him. Keep hitting him and he dies.
    /// - Bodyguards: while any of his band stands, his Wardens guard him, his Medic treats him first, and snares and
    ///   Openings do not take on him.
    /// Starting values. With FightRules.ChiefKits (21 Oct 2026) the yield is gone: he kneels when his nerve runs out
    /// (BattleSimulation.Kit.cs). The second phase and the bodyguards stay under every chief's kit.
    public static class ChiefFight
    {
        public const int SecondPhasePercent = 50;
        public const int YieldPercent = 25;
        public const int YieldNervePerSecond = 50;
        public const int Reserves = 2;
    }

    public sealed partial class BattleSimulation
    {
        private readonly bool _chiefOn;

        /// True when this fight plays the chief rules.
        public bool ChiefRulesOn => _chiefOn;

        // A chief's reserves wait out of the fight until his second phase; till then they count as not alive.
        private void BenchReserves()
        {
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (!SpecOf(u).Reserve) continue;
                u.Benched = true;
                u.Alive = false;
            }
        }

        // A chief took a blow: half health brings his second phase, a quarter his yield. With the chief kits he kneels when his
        // nerve runs out instead (BattleSimulation.Kit.cs), so there is no yield.
        private void ChiefStruck(BattleUnit chief)
        {
            if (!chief.PhaseDone && chief.Hp * 100 <= chief.MaxHp * ChiefFight.SecondPhasePercent) SecondPhase(chief);
            if (_kitsOn) return;
            if (!_finished && chief.Alive && !chief.Yielded && chief.Hp * 100 <= chief.MaxHp * ChiefFight.YieldPercent) Yield(chief);
        }

        // His second phase: his reserves join, and he starts his chief spell again at once unless his side already winds
        // something up or his wall still stands. In a kit that has one it is his named phase (Bloodridge's Blood Sun, which
        // its own rules read from PhaseDone); he says his line and drops a glowing Cleave to cast, unless he is stunned or
        // his kit left the Eclipse out this run. The Skullpeak chief's is her Lockdown instead: a wall that never drops
        // (BattleSimulation.Skullpeak.cs), and no Steel Storm again. The hunt captain's is the Pack: two hunts at once. The
        // Stoneveil chief's is his Red Rite and the Thundercoast chief's his Spring Tide (their own files read both from
        // PhaseDone); each casts his spell again if he took it, once he is free of a channel or a horn.
        private void SecondPhase(BattleUnit chief)
        {
            chief.PhaseDone = true;
            int side = chief.Side;
            var joining = new List<BattleUnit>();
            for (int i = 0; i < _units.Count && joining.Count < ChiefFight.Reserves; i++)
            {
                BattleUnit u = _units[i];
                if (u.Side == side && u.Benched) joining.Add(u);
            }
            Emit(EventFor(BattleEventKind.Phase, chief, null, joining.Count, ChiefKits.PhaseName(Kit(side))));
            for (int i = 0; i < joining.Count; i++)
            {
                BattleUnit u = joining[i];
                u.Benched = false;
                u.Alive = true;
                u.Cooldown = 1 + u.Stats.CooldownTicks / 2;
                // From rules 44 he comes onto a free panel (BattleSimulation.Grid.cs).
                if (_gridOn) GridJoin(u);
                Emit(EventFor(BattleEventKind.Join, chief, u, 0, string.Empty));
            }
            SideState state = _sides[side];
            if (KitOn(side))
            {
                Say(chief, ChiefKits.PhaseLine(Kit(side)));
                if (Kit(side) == ChiefKit.Skullpeak)
                {
                    EnterLockdown(side, chief);
                    return;
                }
                // The hunt captain's is the Pack: a second hunt runs from now on (BattleSimulation.Hunt.cs), and he casts his
                // own spell again as every chief does.
                if (Kit(side) == ChiefKit.Hunt) _packOn = true;
                else
                {
                    // Spring Tide lifts the water off its floor at once (BattleSimulation.Thundercoast.cs).
                    if (_tideOn && side == _tideSide) TideChanged(ChiefKits.TideSpring);
                    if (chief.StunTicks > 0 || !HasMove(side, ChiefKits.SpellMove(Kit(side)))) return;
                    ClearCleave(state);
                    if (Kit(side) > ChiefKit.Hunt && KitBusy(chief))
                    {
                        state.NextAutoEclipse = _tick;
                        return;
                    }
                }
            }
            if (_rules[side].EclipseGrade <= 0 || state.Windup > 0 || (_heavyOn && state.HeavyWindup > 0) ||
                (_spellsOn && state.WallTicks > 0) || !ReferenceEquals(Leader(side), chief)) return;
            StartEclipse(side, 100, false);
            state.NextAutoEclipse = _tick + SpellGap(side);
        }

        // He kneels: he does nothing more, and any cast or swing of his is gone.
        private void Yield(BattleUnit chief)
        {
            chief.Yielded = true;
            chief.AimTicks = 0;
            chief.AimTargetId = string.Empty;
            chief.GuardTargetId = string.Empty; // a kneeling Warden covers nobody

            SideState state = _sides[chief.Side];
            if (state.Windup > 0 && string.Equals(state.CasterId, chief.Id, StringComparison.Ordinal)) ClearWindup(state);
            if (state.HeavyWindup > 0 && string.Equals(state.HeavySwingerId, chief.Id, StringComparison.Ordinal)) ClearHeavy(state);
            Emit(EventFor(BattleEventKind.Yield, chief, null, 0, string.Empty));
        }

        // A kneeling chief's side loses heart every second.
        private void TickYield()
        {
            if (_tick % SimClock.TicksPerSecond != 0) return;
            for (int i = 0; i < _units.Count && !_finished; i++)
            {
                BattleUnit u = _units[i];
                if (u.Alive && u.Yielded) DrainNerve(u.Side, ChiefFight.YieldNervePerSecond, 1 - u.Side);
            }
        }

        // While your side holds, your fighters spare a kneeling chief and strike someone else. They leave the cage bear's
        // heart alone too while its ribcage stands, since no blow reaches it (BattleSimulation.Beast.cs).
        private bool Spared(BattleUnit attacker, BattleUnit target)
        {
            return (_chiefOn && target.Yielded && _sides[attacker.Side].HoldTicks > 0) || (_beastOn && Shielded(target));
        }

        // Any of his band still standing.
        private bool BandStands(int side)
        {
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (u.Alive && u.Side == side && u.Role == UnitRole.Fighter) return true;
            }
            return false;
        }

        // The chief a Warden of his band guards, or null.
        private BattleUnit GuardedChief(BattleUnit warden)
        {
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (u.Alive && u.Side == warden.Side && u.Role == UnitRole.Chief && !ReferenceEquals(u, warden)) return u;
            }
            return null;
        }

        // Who a Medic treats: her hurt chief first while his band stands, else the most hurt. Facing the Stoneveil chief, one
        // his Leeches drink from comes first, and while his chant or his Open Veins run a bleeding wound (BattleSimulation.Stoneveil.cs).
        private BattleUnit PatientFor(BattleUnit medic)
        {
            // From rules 44 only an ally on her own panel or one touching it (BattleSimulation.Grid.cs).
            if (_gridOn && medic.GridCol >= 0) return GridPatient(medic);
            if (_riteOn)
            {
                BattleUnit rite = RitePatient(medic);
                if (rite != null) return rite;
            }
            if (_chiefOn && BandStands(medic.Side))
            {
                for (int i = 0; i < _units.Count; i++)
                {
                    BattleUnit u = _units[i];
                    if (u.Alive && u.Side == medic.Side && u.Role == UnitRole.Chief && u.HpPermille < MedicHealsBelowPermille) return u;
                }
            }
            // From rules 24 her habit picks (BattleSimulation.Whisperer.cs).
            if (_whisperOn && HabitOf(medic) != WhispererHabit.None) return HabitPatient(medic);
            return MostHurtAlly(medic, MedicHealsBelowPermille, true);
        }

        // A chief shrugs off snares and Openings while his band stands.
        private bool ShrugsMarks(BattleUnit unit)
        {
            return _chiefOn && unit.Role == UnitRole.Chief && BandStands(unit.Side);
        }
    }
}
