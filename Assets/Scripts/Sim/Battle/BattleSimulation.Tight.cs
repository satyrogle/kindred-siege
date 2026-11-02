using System;
using System.Collections.Generic;

namespace KindredSiege.Sim.Battle
{
    /// The tight parry (Jacob, 1 Nov 2026: "Go, lock it", then "Go, build it"; DESIGN_LOCKS, "The parry: tight window, big
    /// payoff, a gold flash on the attacker"; FightRules.Tight, campaign rules 26). On top of the parry at the center:
    /// - The window is WindowTicks (0.2 s), not FightRules.PerfectHoldTicks.
    /// - A whiff: a Hold that is up when a beat lands but was not given in the window parries nothing and soaks nothing. The
    ///   blow lands in full, the Hold ends, and for WhiffTicks after it every blow on that side lands WhiffPercent harder.
    /// - A counter: every parry hits whoever did the move back for the move's damage, one tick later. A cast's is what it
    ///   would have done to the biggest fighter of the row it would hit; every other move's is the doer's heavy blow, at its
    ///   middle roll.
    /// The stagger (FightRules.StaggerTicks, 2 s), the cancel and the meter's bar were already there. The screen reads the
    /// window on Threat.ParryWindow and Threat.ActorId, which follow WindowTicks under these rules. Every number is a starting
    /// value.
    public static class TightRules
    {
        /// A Hold given this many ticks or fewer before a beat lands parries it: 0.2 s.
        public const int WindowTicks = 6;

        /// After a whiff, every blow on the side that whiffed lands this percent harder for this long.
        public const int WhiffPercent = 50;
        public const int WhiffTicks = SimClock.TicksPerSecond;

        /// A Reaction event's detail for a whiff (actor side the side that whiffed, amount WhiffTicks), and the damage detail
        /// of a counter.
        public const string WhiffDetail = "whiff";
        public const string CounterDetail = "counter";
    }

    public sealed partial class BattleSimulation
    {
        private readonly bool _tightOn;

        // Counters owed by a parry this tick, dealt once the tick's moves are done: the doer, his side, how hard.
        private readonly List<PendingCounter> _counters = new List<PendingCounter>();

        private readonly struct PendingCounter
        {
            public PendingCounter(string doerId, int side, int amount)
            {
                DoerId = doerId;
                Side = side;
                Amount = amount;
            }

            public string DoerId { get; }
            public int Side { get; }
            public int Amount { get; }
        }

        /// Whether this fight plays the tight parry: campaign rules 26.
        public bool TightParryOn => _tightOn;

        /// How many ticks before a beat a Hold still parries it: TightRules.WindowTicks under the tight parry, else
        /// FightRules.PerfectHoldTicks. Read only, for the screen.
        public int ParryWindowTicks => _tightOn ? TightRules.WindowTicks : FightRules.PerfectHoldTicks;

        /// Ticks this side still takes TightRules.WhiffPercent more from every blow after a whiff, or 0. Read only, for the screen.
        public int WhiffTicksLeft(int side)
        {
            if (side < 0 || side >= _sides.Length) throw new ArgumentOutOfRangeException(nameof(side));
            return _tightOn ? _sides[side].WhiffTicks : 0;
        }

        // A Hold was up when a beat landed but not given in the window: it ends, and the side is open for a second. The blow
        // itself lands in full (MeetBeat says Open); the extra starts on the next tick.
        private void Whiff(int facing)
        {
            SideState s = _sides[facing];
            s.HoldTicks = 0;
            s.WhiffTicks = TightRules.WhiffTicks;
            s.WhiffTick = _tick;
            Emit(new BattleEvent(_tick, BattleEventKind.Reaction, facing, string.Empty, -1, string.Empty, -1, TightRules.WhiffTicks,
                TightRules.WhiffDetail));
        }

        // Blows on a side that whiffed land harder, from the tick after the whiff, in the Status layer.
        private void WhiffHit(BattleUnit victim, bool foe, Hit hit)
        {
            SideState s = _sides[victim.Side];
            if (foe && s.WhiffTicks > 0 && s.WhiffTick != _tick) hit.Add(HitLayer.Status, TightRules.WhiffPercent);
        }

        // The same for a fight without the damage layers.
        private int WhiffDamage(BattleUnit victim, int sourceSide, int amount)
        {
            SideState s = _sides[victim.Side];
            if (sourceSide != 1 - victim.Side || s.WhiffTicks <= 0 || s.WhiffTick == _tick) return amount;
            return amount * (100 + TightRules.WhiffPercent) / 100;
        }

        private void CountDownWhiff()
        {
            for (int s = 0; s < 2; s++)
                if (_sides[s].WhiffTicks > 0) _sides[s].WhiffTicks--;
        }

        // A parry owes the doer a counter for the move's damage: counter when the caller knows it, else his heavy blow.
        private void OweCounter(BattleUnit doer, int counter)
        {
            if (doer == null) return;
            int amount = counter > 0 ? counter : HeavyBlow(doer);
            _counters.Add(new PendingCounter(doer.Id, doer.Side, amount));
        }

        // His heavy blow at its middle roll, with no number drawn.
        private int HeavyBlow(BattleUnit doer)
        {
            int power = doer.Class == UnitClass.Medic
                ? Math.Max(1, SliceClasses.MedicFallbackAttack * doer.PowerPercent / 100)
                : doer.Power;
            return Math.Max(1, power * HeavyPercent(doer) / 100);
        }

        // What a cast would have done to the biggest fighter of the row it would hit: the front row, or the back when the
        // front is gone. 0 when it hits nobody.
        private int CastCounter(int side, int percent)
        {
            int enemy = 1 - side;
            Row row = AnyAlive(enemy, Row.Front) ? Row.Front : Row.Back;
            int most = 0;
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (u.Alive && u.Side == enemy && u.Row == row) most = Math.Max(most, u.MaxHp * percent / 100);
            }
            return most;
        }

        // The tick's counters land, in the order the parries came.
        private void DealCounters()
        {
            for (int i = 0; i < _counters.Count && !_finished; i++)
            {
                PendingCounter c = _counters[i];
                BattleUnit doer = FindById(c.DoerId);
                if (doer == null || !doer.Alive) continue;
                ApplyDamage(null, 1 - c.Side, doer, c.Amount, TightRules.CounterDetail);
            }
            _counters.Clear();
            if (!_finished) CheckWipe();
        }

        private void AppendTight(SnapshotBuilder b, SideState state)
        {
            b.Field("whiff", state.WhiffTicks);
            b.Field("whiffTick", state.WhiffTick);
        }
    }
}
