using System;

namespace KindredSiege.Sim.Battle
{
    /// Road combat (Jacob, 25 Oct 2026: "go, build rules 11 in the sim as written in 11263, 11358 and 11406"; HANDOFF_LOG
    /// lines 11263, 11358 and 11406; FightRules.RoadCombat, campaign rules version 11). In a road fight (no chief, no beast):
    /// - Every fighter has his own nerve, UnitNerve to start. It drains from the health he loses (NerveHitPercent of it over his
    ///   whole health), a comrade falling (ComradeFallNerve), your Unleash's scare on their front row (ScarePercent of the
    ///   scare each), a mark cashed on him, and anything else that shakes his side, shared out. There is no shared bar: the
    ///   fight ends when one side has nobody left fighting (dead, fled or collapsed); a frozen, snared or staggered fighter
    ///   still stands, and a side's leader falling no longer breaks it.
    /// - At zero he snaps, once a fight. A melee fighter makes his last stand if he is below LastStandBelowPermille of his
    ///   health: he frenzies, hits harder (FightRules.RiseDamagePercent) and ignores Fall Back and a Focus Fire target for
    ///   FrenzyTicks, then collapses. Above it his nerve stays at 1 until he drops below it. Anyone else: one of theirs flees
    ///   the field; one of yours, who never run ("there's no running only death"), freezes FreezeTicks and comes back with
    ///   FreezeRecoverNerve, never below 1 again that fight. Leaders never snap.
    /// - Your own Hold and Unleash no longer drain your nerve.
    /// Every number is a starting value.
    public static class RoadRules
    {
        public const int UnitNerve = 200;
        public const int ComradeFallNerve = 40;
        public const int ScarePercent = 50;
        public const int LastStandBelowPermille = 500;
        public const int FrenzyTicks = 30 * SimClock.TicksPerSecond;
        public const int FreezeTicks = 3 * SimClock.TicksPerSecond;
        public const int FreezeRecoverNerve = 50;

        /// The Snap event's detail for each way a fighter snaps, and for a frenzy's end.
        public const string Frenzy = "frenzy";
        public const string Freeze = "freeze";
        public const string Fled = "fled";
        public const string Collapse = "collapse";
    }

    public sealed partial class BattleSimulation
    {
        /// True when this fight plays road combat (FightRules.RoadCombat).
        private readonly bool _roadCombatOn;

        /// True when this road combat fight is a road fight, with no chief and no beast: every fighter has his own nerve.
        private readonly bool _unitNerveOn;

        /// True when this fight plays road combat (FightRules.RoadCombat).
        public bool RoadCombatOn => _roadCombatOn;

        /// True when every fighter has his own nerve and there is no shared bar (a road combat road fight).
        public bool UnitNerveOn => _unitNerveOn;

        /// How many of a side's fighters are still fighting: alive, not fled, not collapsed. The enemies left counter.
        public int StillFighting(int side)
        {
            return AliveCount(side);
        }

        private void StartUnitNerve()
        {
            for (int i = 0; i < _units.Count; i++) _units[i].UnitNerve = RoadRules.UnitNerve;
        }

        // A blow cost him this much health: his nerve drains by NerveHitPercent of it over his whole health, and a melee fighter
        // whose nerve already ran out makes his last stand once he drops below half.
        private void UnitShaken(BattleUnit victim, int hpLost, int causedBySide)
        {
            if (!victim.Alive || hpLost <= 0 || victim.MaxHp <= 0) return;
            int divisor = 100 * victim.MaxHp;
            int scaled = hpLost * NerveHitPercent * RoadRules.UnitNerve + victim.UnitNerveRem;
            victim.UnitNerveRem = scaled % divisor;
            UnitDrain(victim, scaled / divisor, causedBySide);
            if (victim.Alive && !victim.NerveSnapped && victim.UnitNerve <= 1 && CanLastStand(victim)) Frenzy(victim);
        }

        // One of his side fell: every comrade still fighting is shaken.
        private void ComradeFell(BattleUnit fallen, int causedBySide)
        {
            for (int i = 0; i < _units.Count && !_finished; i++)
            {
                BattleUnit u = _units[i];
                if (u.Alive && u.Side == fallen.Side && !ReferenceEquals(u, fallen)) UnitDrain(u, RoadRules.ComradeFallNerve, causedBySide);
            }
        }

        // Your Unleash's scare lands on their front row, ScarePercent of it on each fighter there.
        private void ScareFront(int side, int scare, int causedBySide)
        {
            int each = scare * RoadRules.ScarePercent / 100;
            for (int i = 0; i < _units.Count && !_finished; i++)
            {
                BattleUnit u = _units[i];
                if (u.Alive && u.Side == side && u.Row == Row.Front) UnitDrain(u, each, causedBySide);
            }
        }

        // Anything else that shakes a side is shared out over its fighters, the first in setup order taking what is left over.
        // The fear bonus was already added.
        private void SpreadNerve(int side, int amount)
        {
            int n = AliveCount(side);
            if (n <= 0) return;
            int each = amount / n, rest = amount % n;
            for (int i = 0; i < _units.Count && !_finished; i++)
            {
                BattleUnit u = _units[i];
                if (!u.Alive || u.Side != side) continue;
                UnitDrain(u, each + rest, -1);
                rest = 0;
            }
        }

        // Takes nerve from one fighter: what the enemy causes is raised by the enemy's fear bonus. At zero he snaps, once.
        private void UnitDrain(BattleUnit u, int amount, int causedBySide)
        {
            if (!_unitNerveOn || _finished || u == null || !u.Alive || amount <= 0) return;
            if (causedBySide == 1 - u.Side) amount = amount * (100 + _setup.Nerve.FearBonusPercent(causedBySide)) / 100;
            u.UnitNerve -= amount;
            if (u.UnitNerve > 0) return;
            // A duel's champion whose nerve runs out yields: he leaves the field and the duel is won (DESIGN_LOCKS, "Duels").
            // Yours never run.
            if (_duelOn && u.Side == 1)
            {
                u.NerveSnapped = true;
                Flee(u);
                return;
            }
            if (u.NerveSnapped || u.IsLeader)
            {
                u.UnitNerve = 1;
                return;
            }
            if (!u.Stats.Ranged)
            {
                if (CanLastStand(u)) Frenzy(u);
                else u.UnitNerve = 1;
                return;
            }
            u.NerveSnapped = true;
            if (u.Side == 1) Flee(u);
            else Freeze(u);
        }

        private bool CanLastStand(BattleUnit u)
        {
            return !u.Stats.Ranged && !u.IsLeader && u.HpPermille < (ClearRoadOn && u.Side == 0 ? 250 : RoadRules.LastStandBelowPermille);
        }

        private void Frenzy(BattleUnit u)
        {
            u.NerveSnapped = true;
            u.Frenzied = true;
            u.UnitNerve = 1;
            u.Statuses.Clear(StatusKind.Panic);
            u.RiseTicks = RoadRules.FrenzyTicks;
            Emit(EventFor(BattleEventKind.Snap, u, null, RoadRules.FrenzyTicks, RoadRules.Frenzy));
        }

        private void Freeze(BattleUnit u)
        {
            u.Statuses.Apply(StatusKind.Panic, RoadRules.FreezeTicks);
            u.UnitNerve = RoadRules.FreezeRecoverNerve;
            Emit(EventFor(BattleEventKind.Snap, u, null, RoadRules.FreezeTicks, RoadRules.Freeze));
        }

        // He runs off the field: out of the fight, not dead. The side that scared him off earns as for a panic.
        private void Flee(BattleUnit u)
        {
            // Fear, the Shade's Focus Fire trick (rules 23, BattleSimulation.Shade.cs): an enemy under your Focus Fire who breaks
            // and flees counts as fallen (Jacob, 31 Oct 2026: "Yes, on break too"). Before he leaves, so those beside him are found.
            if (_floorOn) Fear(1 - u.Side, u);
            u.Fled = true;
            u.Alive = false;
            u.GuardTargetId = string.Empty;
            u.AimTicks = 0;
            u.AimTargetId = string.Empty;
            Emit(EventFor(BattleEventKind.Snap, u, null, 0, RoadRules.Fled));
            Earn(1 - u.Side, FightRules.PanicPoints, FightRules.PanicReason);
            CheckWipe();
            // From rules 43 their leader leaves when his last fighter flees too (BattleSimulation.Leaders.cs).
            if (LeadersOn) FighterGone(u);
        }

        // A frenzy that has run its time ends in collapse: he falls, as dead for this fight.
        private void TickRoad()
        {
            for (int i = 0; i < _units.Count && !_finished; i++)
            {
                BattleUnit u = _units[i];
                if (!u.Alive || !u.Frenzied || u.RiseTicks > 0) continue;
                Emit(EventFor(BattleEventKind.Snap, u, null, 0, RoadRules.Collapse));
                ApplyDamage(null, -1, u, u.Hp, RoadRules.Collapse);
            }
        }
    }
}
