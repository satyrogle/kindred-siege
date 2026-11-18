using System;
using System.Collections.Generic;

namespace KindredSiege.Sim.Battle
{
    /// Rules 40's numbers (FightRules.Stage; Jacob, 6 Nov 2026, after playing The Pit: "i cant understand shit ... they are just
    /// lunging forward", then "Yes, build it"). Claude's starting values, Jacob's to change.
    public static class StageRules
    {
        /// A fighter's turn comes this many times as seldom, and his auto attack and the heals he gives on his turn are this many
        /// times as big: the same damage and healing a second, a third as many blows, so the screen can give each one the stage
        /// and the fight still lasts as long, with every chief's and beast's move coming as often as before.
        public const int TurnTimes = 3;
    }

    /// One blow at a time (FightRules.Stage, campaign rules 40). A fighter's turn comes StageRules.TurnTimes times as seldom and
    /// his auto attack and his turn's heals are that many times as big, so a fight has a third of the blows. A turn that comes due
    /// joins a line, and the line lets one turn land per tick, first come first served; an Aimed Shot that comes due on a tick a
    /// turn landed waits for the next. So no two turns or shots land on the same tick, and the screen can hold the fight while each
    /// one plays (a battle clock that waits while a blow plays) and show them one after another.
    ///
    /// The sim never waits for the screen: holding the fight is the screen's, between Tick() calls, so every clock (big moves,
    /// bleeding, orders, nerve, spells) keeps its pace and the fight lasts as long as under rules 39.
    ///
    /// Claude's calls while building, Jacob's to change: fighters due on the same tick join the line in the tick loop's scan order,
    /// which alternates sides; a fighter in line who cannot act this tick (snared, panicking, frozen) lets the next one through and
    /// keeps his place; any turn counts, a Medic's treatment, a Warden's guard and a Marksman starting to aim as well as a hit;
    /// the class spells (Aimed Shot, Strike) keep their own clocks and their size; whatever a turn's hit or heal sets off at once
    /// (a cash in, a counter, a splash heal) is as big as that turn; big moves, bleeding, burning, counters after a parry,
    /// carried moves and the Pals' tricks are not in the line, so one may land on the same tick as a turn.
    public sealed partial class BattleSimulation
    {
        private readonly bool _stageOn;
        // The first tick a turn or an Aimed Shot may land again: the tick after the last one landed.
        private long _stageFreeAt;
        // The fighters whose turn is due, in the order it came due, waiting to land (unit indexes).
        private readonly List<int> _stageLine = new List<int>();
        // The tick each fighter last stood in line able to act (his turn due and every check of the tick loop passed).
        private readonly long[] _stageReadyAt;
        // True while a fighter takes his turn from the line, so his auto attack and his heals are StageRules.TurnTimes as big; and
        // whether this turn's were, so his next turn comes that many times as late. A turn that only cast his spell, armed his trap
        // or started his aim comes back as soon as before, since those keep their own clocks and their own size.
        private bool _stageTurn;
        private bool _stageScaled;

        /// True when this fight plays one blow at a time (campaign rules 40).
        public bool StageOn => _stageOn;

        /// The fighters whose turn is due and who wait to land it, first in line first. Read only.
        public IReadOnlyList<string> StageLine
        {
            get
            {
                var ids = new List<string>();
                for (int i = 0; i < _stageLine.Count; i++) ids.Add(_units[_stageLine[i]].Id);
                return ids;
            }
        }

        private static long[] NewStageClocks(int count)
        {
            var clocks = new long[count];
            for (int i = 0; i < count; i++) clocks[i] = -1;
            return clocks;
        }

        // True when a turn or an Aimed Shot already landed this tick.
        private bool StageTaken() => _tick < _stageFreeAt;

        // His turn is due: he joins the line (once) and stands ready this tick.
        private void StageReady(BattleUnit unit)
        {
            unit.Cooldown = 0;
            if (!_stageLine.Contains(unit.Index)) _stageLine.Add(unit.Index);
            _stageReadyAt[unit.Index] = _tick;
        }

        // After the tick loop: the first fighter in line who stood ready this tick takes his turn.
        private void StageNext()
        {
            if (StageTaken()) return;
            for (int i = 0; i < _stageLine.Count; i++)
            {
                BattleUnit unit = _units[_stageLine[i]];
                if (!unit.Alive)
                {
                    _stageLine.RemoveAt(i);
                    i--;
                    continue;
                }
                if (_stageReadyAt[unit.Index] != _tick) continue;
                _stageLine.RemoveAt(i);
                _stageTurn = true;
                _stageScaled = false;
                Act(unit);
                _stageTurn = false;
                // As the tick loop, his clock StageRules.TurnTimes as long after a scaled turn: Heavy slows a Gravity Well
                // Warden's and Medic's turns, a gorged star's turns slow.
                int next = NextCooldown(unit) * (_stageScaled ? StageRules.TurnTimes : 1);
                if (unit.Alive) unit.Cooldown += HeavyTurn(unit, next);
                if (_duoMovesOn && unit.Alive && unit.GorgePercent > 0) unit.Cooldown += next * unit.GorgePercent / 200;
                _stageFreeAt = _tick + 1;
                CheckWipe();
                return;
            }
        }

        // An Aimed Shot fired: nothing else lands from the line this tick.
        private void StageShot() => _stageFreeAt = _tick + 1;

        // At the start of the fight every first turn comes StageRules.TurnTimes as late, from the same staggered roll.
        private void StageStagger()
        {
            for (int i = 0; i < _units.Count; i++) _units[i].Cooldown *= StageRules.TurnTimes;
        }

        // A turn's auto attack, hit or miss, and a heal given on a turn, are StageRules.TurnTimes as big, and that turn's wait after.
        private void StageBlow(Hit hit)
        {
            if (!_stageTurn) return;
            hit.Times(StageRules.TurnTimes);
            _stageScaled = true;
        }

        private void AppendStage(SnapshotBuilder b)
        {
            b.Section("stage");
            b.Field("freeAt", _stageFreeAt);
            var line = new System.Text.StringBuilder();
            for (int i = 0; i < _stageLine.Count; i++)
            {
                if (i > 0) line.Append(',');
                line.Append(_units[_stageLine[i]].Id);
            }
            b.Field("line", line.ToString());
        }

        // Under rules 40, how many Tick() calls until each fighter's next turn or Aimed Shot lands (0 for none), playing the tick
        // loop's line forward on copies: the turn clocks fill in the scan order, the first in line lands, and the Aimed Shots count
        // down after the turns. A read for Blows: it ignores an Overclock, a Time Snare and a fighter's checks changing on the
        // way, so the event that lands is the truth.
        private int[] StageLands()
        {
            int n = _units.Count;
            var lands = new int[n];
            if (_halted) return lands;
            var left = new int[n];
            var aim = new int[n];
            int pending = 0;
            for (int i = 0; i < n; i++)
            {
                BattleUnit u = _units[i];
                left[i] = -1;
                if (!u.Alive || u.Benched) continue;
                if (_beastOn && u.Side == _beastSide) continue;
                if (u.AimTicks > 0)
                {
                    if (u.PanicTicks <= 0 && !(_fight.Contracts && ActionBlocked(u, completingAim: true)))
                    {
                        aim[i] = u.AimTicks;
                        pending++;
                    }
                    continue;
                }
                if (TurnWaits(u) || (_classOn && u.Class == UnitClass.Medic)) continue;
                left[i] = Math.Max(0, u.Cooldown);
                pending++;
            }
            var line = new List<int>();
            for (int i = 0; i < _stageLine.Count; i++) if (left[_stageLine[i]] >= 0) line.Add(_stageLine[i]);
            for (long t = _tick; pending > 0 && t < _tick + 3600; t++)
            {
                bool forward = (t & 1L) == 0L;
                for (int k = 0; k < n; k++)
                {
                    int i = forward ? k : n - 1 - k;
                    if (left[i] < 0 || lands[i] > 0) continue;
                    left[i]--;
                    if (left[i] > 0) continue;
                    left[i] = 0;
                    if (!line.Contains(i)) line.Add(i);
                }
                bool taken = t < _stageFreeAt;
                if (!taken && line.Count > 0)
                {
                    int who = line[0];
                    line.RemoveAt(0);
                    lands[who] = (int)(t - _tick) + 1;
                    pending--;
                    taken = true;
                }
                for (int i = 0; i < n; i++)
                {
                    if (aim[i] <= 0 || lands[i] > 0) continue;
                    aim[i]--;
                    if (aim[i] > 0) continue;
                    if (taken)
                    {
                        aim[i] = 1;
                        continue;
                    }
                    lands[i] = (int)(t - _tick) + 1;
                    pending--;
                    taken = true;
                }
            }
            return lands;
        }
    }
}
