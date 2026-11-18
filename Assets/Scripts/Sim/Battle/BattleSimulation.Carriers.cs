using System;
using System.Collections.Generic;

namespace KindredSiege.Sim.Battle
{
    /// The road kit's carried moves (Jacob, 25 Oct 2026: "the trash before the boss is divided in such a way that one of the boss
    /// ability is given to one of a mob in a trash pack", then "1. We can do that"; FightRules.RoadKit, campaign rules version
    /// 12). A road fighter carrying his chief's move (UnitSpec.Carries) uses a small version of it on his own clock, so you
    /// learn the chief's moves before his fight. The answers are the chief's:
    /// - Cleave: his blade glows CleaveTellTicks, then a hit of CleavePercent of his power on whoever he was going for. A Hold
    ///   timed to it parries it (a small beat: he staggers and is left open), a stale Hold soaks it, a Warden's Guard takes it,
    ///   and a Brace or Sidestep meets it.
    /// - Brand: a sigil over the most hurt of your fighters for BrandTicks, then it bursts for BrandPercent of the health of that
    ///   fighter and of each one beside them in their row. Fall Back gets clear; a Warden covering the branded fighter halves it
    ///   on them.
    /// - Burning Ring: he spins it RingTellTicks, a cast bar (Threat.CastBar), then throws it down one lane through both rows,
    ///   RingPercent of each one's health. Focus Fire on him while it spins knocks it out of his hand; Fall Back gets clear.
    /// A fighter carrying two moves uses them in turn. Every number is a starting value.
    public static class CarryRules
    {
        /// The moves a road fighter can carry.
        public const ChiefMove Carried = ChiefMove.Cleave | ChiefMove.Brand | ChiefMove.BurningRing;

        public const int FirstTicks = 4 * SimClock.TicksPerSecond;
        public const int StaggerTicks = 2 * SimClock.TicksPerSecond;

        public const int CleaveEveryTicks = 6 * SimClock.TicksPerSecond;
        public const int CleaveTellTicks = SimClock.TicksPerSecond;
        public const int CleavePercent = 200;

        public const int BrandEveryTicks = 15 * SimClock.TicksPerSecond;
        public const int BrandTicks = 4 * SimClock.TicksPerSecond;
        public const int BrandPercent = 10;

        public const int RingEveryTicks = 12 * SimClock.TicksPerSecond;
        public const int RingTellTicks = 3 * SimClock.TicksPerSecond / 2;
        public const int RingPercent = 15;

        /// What a carried move's Land event says after its name and a ".": parried, dodged or broken.
        public const string Parried = "parried";
        public const string Dodged = "dodged";
        public const string Broken = "broken";
    }

    public sealed partial class BattleSimulation
    {
        private static ChiefMove FirstCarried(ChiefMove carries)
        {
            if ((carries & ChiefMove.Cleave) != 0) return ChiefMove.Cleave;
            if ((carries & ChiefMove.Brand) != 0) return ChiefMove.Brand;
            return (carries & ChiefMove.BurningRing) != 0 ? ChiefMove.BurningRing : ChiefMove.None;
        }

        // The carried move after this one, in the order Cleave, Brand, Ring and round again.
        private static ChiefMove NextCarried(ChiefMove carries, ChiefMove after)
        {
            ChiefMove[] order = { ChiefMove.Cleave, ChiefMove.Brand, ChiefMove.BurningRing };
            int at = Array.IndexOf(order, after);
            for (int k = 1; k <= order.Length; k++)
            {
                ChiefMove m = order[(at + k) % order.Length];
                if ((carries & m) != 0) return m;
            }
            return after;
        }

        private static string CarriedName(ChiefMove move)
        {
            switch (move)
            {
                case ChiefMove.Cleave: return ChiefKits.CleaveName;
                case ChiefMove.Brand: return ChiefKits.BrandName;
                case ChiefMove.BurningRing: return ChiefKits.RingName;
                default: return string.Empty;
            }
        }

        private static int CarriedEvery(ChiefMove move)
        {
            switch (move)
            {
                case ChiefMove.Cleave: return CarryRules.CleaveEveryTicks;
                case ChiefMove.Brand: return CarryRules.BrandEveryTicks;
                default: return CarryRules.RingEveryTicks;
            }
        }

        // The carriers' clocks run on, and road brands count down to their burst.
        private void TickCarriers()
        {
            if (!CarriersAvailable) return;
            for (int i = 0; i < _units.Count && !_finished; i++)
            {
                BattleUnit u = _units[i];
                if (u.BrandTicks > 0 && u.BrandById.Length > 0)
                {
                    u.BrandTicks--;
                    if (u.BrandTicks == 0) BurstBrand(u);
                }
            }
            for (int i = 0; i < _units.Count && !_finished; i++)
            {
                BattleUnit u = _units[i];
                if (!u.Alive || SpecOf(u).Carries == ChiefMove.None) continue;
                // Stasis restarts this windup. Keep it suspended while pinned, then resume its tell.
                if (_fight.FreezeContracts && u.PinTicks > 0) continue;
                if (u.SnareTicks > 0 || u.PanicTicks > 0 || u.StunTicks > 0 || u.ReelTicks > 0 ||
                    (_fight.FreezeContracts && ControlBlocked(u)))
                {
                    // Held, he cannot start a move, and one he was telling is lost.
                    if (u.CarryTell > 0) EndCarry(u);
                    continue;
                }
                if (u.CarryTell > 0)
                {
                    u.CarryTell--;
                    if (u.CarryTell == 0) LandCarry(u);
                    continue;
                }
                if (_tick >= u.CarryNext) StartCarry(u);
            }
        }

        private void StartCarry(BattleUnit u)
        {
            ChiefMove move = u.CarryMove;
            // The Pit: a carried Cleave, the one a Hold parries, waits while another parryable move of his side winds up
            // (BattleSimulation.Pit.cs, rule 3); his clock tries again next tick.
            if (move == ChiefMove.Cleave && PitWaits(u.Side)) return;
            if (move == ChiefMove.Brand)
            {
                BattleUnit mark = MostHurtFoe(u.Side);
                if (mark == null)
                {
                    u.CarryNext = _tick + 1;
                    return;
                }
                mark.BrandTicks = CarryRules.BrandTicks;
                mark.BrandById = u.Id;
                Emit(EventFor(BattleEventKind.Tell, u, mark, CarryRules.BrandTicks, ChiefKits.BrandName));
                EndCarry(u);
                return;
            }
            BattleUnit target = ChooseTarget(u);
            if (target == null)
            {
                u.CarryNext = _tick + 1;
                return;
            }
            u.CarryTargetId = target.Id;
            // A Time Snare slows his next big move (BattleSimulation.Combos.cs).
            u.CarryTell = Slowed(u, move == ChiefMove.Cleave ? CarryRules.CleaveTellTicks : CarryRules.RingTellTicks);
            Emit(EventFor(BattleEventKind.Tell, u, target, u.CarryTell, CarriedName(move)));
        }

        // His move is done, landed or lost: his clock starts again for the next one he carries.
        private void EndCarry(BattleUnit u)
        {
            u.CarryTell = 0;
            u.CarryTargetId = string.Empty;
            u.CarryNext = _tick + CarriedEvery(u.CarryMove);
            u.CarryMove = NextCarried(SpecOf(u).Carries, u.CarryMove);
        }

        private void LandCarry(BattleUnit u)
        {
            ChiefMove move = u.CarryMove;
            BattleUnit target = FindById(u.CarryTargetId);
            EndCarry(u);
            if (target == null || !target.Alive) return;
            int facing = target.Side;
            if (move == ChiefMove.Cleave)
            {
                BeatMet met = MeetBeat(facing);
                if (met == BeatMet.Parry)
                {
                    Emit(EventFor(BattleEventKind.Land, u, target, 0, ChiefKits.CleaveName + "." + CarryRules.Parried));
                    SmallParried(u.Side, u, ChiefKits.CleaveName);
                    return;
                }
                int damage = CarriedCleave(u, target);
                if (met == BeatMet.Block && target.Row == Row.Front) damage = Math.Max(1, damage * HoldSoak(facing) / 100);
                Emit(EventFor(BattleEventKind.Land, u, target, damage, ChiefKits.CleaveName));
                BattleUnit warden = GuardingWarden(target);
                if (warden != null)
                {
                    int guarded = damage * (100 - SliceClasses.GuardReductionPercent) / 100;
                    Emit(EventFor(BattleEventKind.Guard, warden, target, damage - guarded, string.Empty));
                    guarded = HeavyDefence(u, warden, guarded);
                    if (guarded > 0) ApplyDamage(u, u.Side, warden, Armored(warden, guarded), GuardedDetail);
                }
                else
                {
                    damage = HeavyDefence(u, target, damage);
                    if (damage > 0) ApplyDamage(u, u.Side, target, Armored(target, damage), ChiefKits.CleaveName);
                }
                CheckWipe();
                return;
            }
            // The Burning Ring.
            if (IsFocusing(facing, u))
            {
                Emit(EventFor(BattleEventKind.Land, u, target, 0, ChiefKits.RingName + "." + CarryRules.Broken));
                Earn(facing, FightRules.InterruptPoints, FightRules.InterruptReason);
                return;
            }
            if (_sides[facing].FallBackTicks > 0)
            {
                Emit(EventFor(BattleEventKind.Land, u, target, 0, ChiefKits.RingName + "." + CarryRules.Dodged));
                Earn(facing, FightRules.DodgePoints, FightRules.DodgeReason);
                return;
            }
            int lane = LanePosition(target);
            var hit = new List<BattleUnit>();
            BattleUnit front = NearestInLane(facing, Row.Front, lane), back = NearestInLane(facing, Row.Back, lane);
            if (front != null) hit.Add(front);
            if (back != null && !ReferenceEquals(back, front)) hit.Add(back);
            Emit(EventFor(BattleEventKind.Land, u, target, hit.Count, ChiefKits.RingName));
            for (int k = 0; k < hit.Count && !_finished; k++)
                ApplyDamage(u, u.Side, hit[k], Math.Max(1, hit[k].MaxHp * CarriedRingPercent / 100), ChiefKits.RingName);
            CheckWipe();
        }

        // A road brand's ring ran out: it bursts on its fighter and those beside them, unless their side fell back.
        private void BurstBrand(BattleUnit marked)
        {
            BattleUnit brander = FindById(marked.BrandById);
            marked.BrandById = string.Empty;
            if (!marked.Alive) return;
            int side = marked.Side;
            if (_sides[side].FallBackTicks > 0)
            {
                Emit(new BattleEvent(_tick, BattleEventKind.Land, 1 - side, brander == null ? string.Empty : brander.Id,
                    brander == null ? -1 : (int)brander.Class, marked.Id, (int)marked.Class, 0, ChiefKits.BrandName + "." + CarryRules.Dodged));
                Earn(side, FightRules.DodgePoints, FightRules.DodgeReason);
                return;
            }
            int lane = LanePosition(marked);
            var hit = new List<BattleUnit>();
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (!u.Alive || u.Side != side || u.Row != marked.Row) continue;
                if (Math.Abs(LanePosition(u) - lane) <= 1) hit.Add(u);
            }
            Emit(new BattleEvent(_tick, BattleEventKind.Land, 1 - side, brander == null ? string.Empty : brander.Id,
                brander == null ? -1 : (int)brander.Class, marked.Id, (int)marked.Class, hit.Count, ChiefKits.BrandName));
            bool covered = GuardingWarden(marked) != null;
            for (int k = 0; k < hit.Count && !_finished; k++)
            {
                int damage = Math.Max(1, hit[k].MaxHp * CarriedBrandPercent / 100);
                if (covered && ReferenceEquals(hit[k], marked)) damage = Math.Max(1, damage / 2);
                ApplyDamage(brander, 1 - side, hit[k], damage, ChiefKits.BrandName);
            }
            CheckWipe();
        }

        private BattleUnit MostHurtFoe(int side)
        {
            BattleUnit best = null;
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (!u.Alive || u.Side == side || u.BrandTicks > 0) continue;
                if (best == null || u.HpPermille < best.HpPermille) best = u;
            }
            return best;
        }

        // What the carriers of side foe have coming at side: a Cleave's beat, a Burning Ring's cast bar, and each road brand.
        private void ThreatsOfCarriers(List<Threat> list, int side, int foe)
        {
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (u.Alive && u.Side == foe && u.CarryTell > 0 && !(_fight.FreezeContracts && ControlBlocked(u)))
                {
                    if (u.CarryMove == ChiefMove.Cleave)
                        list.Add(new Threat(ChiefKits.CleaveName, ThreatTier.Move, u.Id, u.CarryTargetId, u.CarryTell,
                            CarriedCleaveShown(u), AtBeat | ThreatAnswer.Hold, string.Empty, BeatIn(u.CarryTell)));
                    else if (u.CarryMove == ChiefMove.BurningRing)
                        list.Add(new Threat(ChiefKits.RingName, ThreatTier.Move, u.Id, u.CarryTargetId, u.CarryTell, 0,
                            ThreatAnswer.FallBack | ThreatAnswer.FocusFire, u.Id, castBar: true));
                }
                if (u.Alive && u.Side == side && u.BrandTicks > 0 && u.BrandById.Length > 0)
                    list.Add(new Threat(ChiefKits.BrandName, ThreatTier.Move, u.BrandById, u.Id, u.BrandTicks,
                        Math.Max(1, u.MaxHp * CarriedBrandPercent / 100), ThreatAnswer.FallBack, string.Empty));
            }
        }
    }
}
