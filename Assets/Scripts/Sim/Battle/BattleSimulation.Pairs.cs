using System;
using System.Collections.Generic;
using System.Globalization;

namespace KindredSiege.Sim.Battle
{
    /// Rules 45: the locked exchanges approved 9 Nov, implementation assigned 13 Nov.
    /// These clocks are authoritative. Animation reads them; it never decides a hit.
    public static class PairRules
    {
        public const string Formed = "paired";
        public const string Ended = "unpaired";
        public const string Swing = "trade";
        public static int Windup(UnitClass c) => Math.Max(6, 18 - AttackTable.Of(c).Speed / 10);
        public static int Answer(UnitClass c) => Math.Max(6, 18 - AttackTable.Of(c).Speed / 10);
    }

    public sealed partial class BattleSimulation
    {
        private sealed class Exchange
        {
            public int Id, A, B, Turn, Pending = -1;
            public long Ready, Lands;
            public bool Aim;
        }
        private readonly bool _pairsOn;
        private readonly List<Exchange> _exchanges = new List<Exchange>();
        private int _nextExchange;
        private readonly List<BattleUnit> _pairInitiative = new List<BattleUnit>();
        private void PrepareExchanges()
        {
            _pairInitiative.AddRange(_units);
            _pairInitiative.Sort((a,b)=>{int order=AttackTable.Of(b.Class).Speed.CompareTo(AttackTable.Of(a.Class).Speed);return order!=0?order:a.Index.CompareTo(b.Index);});
            FormExchanges();
        }
        public bool LockedPairsOn => _pairsOn;

        private Exchange ExchangeOf(BattleUnit u)
        {
            if (!_pairsOn || u == null) return null;
            for (int i = 0; i < _exchanges.Count; i++)
                if (_exchanges[i].A == u.Index || _exchanges[i].B == u.Index) return _exchanges[i];
            return null;
        }
        public string PairedOpponentId(BattleUnit u)
        {
            Exchange p = ExchangeOf(u);
            return p == null ? string.Empty : _units[p.A == u.Index ? p.B : p.A].Id;
        }
        public bool PairHasTurn(BattleUnit u)
        {
            Exchange p = ExchangeOf(u);
            return p != null && p.Turn == u.Index;
        }
        public int PairWindupTicks(BattleUnit u)
        {
            Exchange p = ExchangeOf(u);
            return p == null || p.Pending != u.Index || p.Aim ? 0 : (int)Math.Max(1, p.Lands - _tick + 1);
        }
        private bool PairFighter(BattleUnit u) => OnField(u) && !u.IsLeader &&
            u.Class != UnitClass.Warden && u.Class != UnitClass.Medic;

        private bool PairReach(BattleUnit a, BattleUnit b)
        {
            if (a == null || b == null || !OnField(b) || a.Side == b.Side || LeaderProtected(b) || Spared(a, b) || b.EvacTicks > 0) return false;
            if (_gridOn && a.GridCol >= 0) return ReferenceEquals(GridReachFrom(a, a.GridCol, a.GridLane), b);
            return b.Row == Row.Front || CanReachBack(a);
        }
        // An ordinary step must not displace or screen an ally's committed opponent.
        // Fall Back explicitly releases pairs before moving anyone.
        private bool PairPanelAvailable(BattleUnit mover, int col, int lane)
        {
            if (!_pairsOn) return true;
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit ally = _units[i];
                if (ReferenceEquals(ally, mover) || ally.Side != mover.Side || !OnField(ally) || ExchangeOf(ally) == null) continue;
                if (ally.GridLane == lane && col >= ally.GridCol) return false;
            }
            return true;
        }
        private BattleUnit PairFocus(BattleUnit u)
        {
            SideState side = _sides[u.Side];
            if (side.FocusTicks <= 0 || u.Frenzied) return null;
            BattleUnit target = FindById(side.FocusTargetId);
            return PairReach(u, target) ? target : null;
        }
        private BattleUnit PairTarget(BattleUnit u)
        {
            BattleUnit focus = PairFocus(u);
            if (focus != null) return focus;
            Exchange p = ExchangeOf(u);
            if (p == null) return null;
            BattleUnit target = _units[p.A == u.Index ? p.B : p.A];
            return PairReach(u, target) ? target : null;
        }
        private BattleUnit SupportPartner(BattleUnit u)
        {
            if (!PairFighter(u) || !u.Stats.Ranged || ExchangeOf(u) != null || PairFocus(u) != null) return null;
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit ally = _units[i];
                if (ally.Side != u.Side || !PairFighter(ally) || ally.Stats.Ranged || ExchangeOf(ally) == null) continue;
                if (_gridOn && u.GridLane != ally.GridLane) continue;
                if (PairReach(u, PairTarget(ally))) return ally;
            }
            return null;
        }
        private void EndExchange(int index)
        {
            Exchange p = _exchanges[index];
            // A cancelled committed shot must not silently acquire another victim.
            if (p.Pending >= 0 && p.Aim)
            {
                BattleUnit u = _units[p.Pending];
                u.AimTicks = 0; u.AimTargetId = string.Empty; u.AimChainId = 0;
            }
            _exchanges.RemoveAt(index);
            Emit(EventFor(BattleEventKind.Pair, _units[p.A], _units[p.B], p.Id, PairRules.Ended));
        }
        private void PairOrder(int side, BattleUnit focus = null)
        {
            if (!_pairsOn) return;
            for (int i = _exchanges.Count - 1; i >= 0; i--)
            {
                Exchange p = _exchanges[i];
                BattleUnit u = _units[_units[p.A].Side == side ? p.A : p.B];
                if (focus == null || PairReach(u, focus)) EndExchange(i);
            }
        }
        private void FormExchanges()
        {
            for (int i = _exchanges.Count - 1; i >= 0; i--)
            {
                Exchange p = _exchanges[i];
                if (!PairFighter(_units[p.A]) || !PairFighter(_units[p.B])) EndExchange(i);
            }
            // Fastest chooses first, stable unit index breaks ties. No RNG or unordered iteration.
            for (int i = 0; i < _pairInitiative.Count; i++)
                {
                    BattleUnit a = _pairInitiative[i];
                    if (!PairFighter(a) || ExchangeOf(a) != null ||
                        _sides[a.Side].FallBackTicks > 0 || FallingBack(a.Side) || PairFocus(a) != null || SupportPartner(a) != null) continue;
                    BattleUnit best = null; int nearest = int.MaxValue;
                    for (int j = 0; j < _units.Count; j++)
                    {
                        BattleUnit b = _units[j];
                        if (!PairFighter(b) || b.Side == a.Side || ExchangeOf(b) != null || _sides[b.Side].FallBackTicks > 0 || FallingBack(b.Side) ||
                            PairFocus(b) != null || SupportPartner(b) != null || !PairReach(a, b) || !PairReach(b, a)) continue;
                        int distance = _gridOn ? Math.Abs(a.GridLane - b.GridLane) * 10 + 5 - a.GridCol - b.GridCol : Math.Abs(a.Index - b.Index);
                        if (distance < nearest) { best = b; nearest = distance; }
                    }
                    if (best == null) continue;
                    var pair = new Exchange { Id = _nextExchange++, A = a.Index, B = best.Index, Turn = a.Index, Ready = _tick };
                    _exchanges.Add(pair);
                    Emit(EventFor(BattleEventKind.Pair, a, best, pair.Id, PairRules.Formed));
                }
        }
        private void FinishExchange(Exchange p, BattleUnit actor)
        {
            p.Pending = -1; p.Aim = false;
            p.Turn = p.A == actor.Index ? p.B : p.A;
            p.Ready = _tick + PairRules.Answer(_units[p.Turn].Class);
        }
        private void TickExchanges()
        {
            FormExchanges();
            // All pairs run independently; no global presentation queue.
            // Gold heavy attacks retain their own existing schedule and parry window.
            // TurnWaits holds that actor's ordinary exchange until its special resolves.
            for (int i = 0; i < _exchanges.Count && !Stopped; i++)
            {
                Exchange p = _exchanges[i];
                if (p.Pending < 0) continue;
                BattleUnit actor = _units[p.Pending];
                if (p.Aim)
                {
                    if (actor.AimTicks <= 0) FinishExchange(p, actor);
                    continue;
                }
                if (_tick < p.Lands || TurnWaits(actor) || MeleeHeldBack(actor) != StrikeBlock.None) continue;
                if (PairTarget(actor) == null) continue;
                Act(actor);
                actor.Cooldown = HeavyTurn(actor, NextCooldown(actor));
                if (_duoMovesOn && actor.GorgePercent > 0) actor.Cooldown += NextCooldown(actor) * actor.GorgePercent / 200;
                p.Aim = actor.AimTicks > 0;
                if (!p.Aim) { FirePairSupport(actor); FinishExchange(p, actor); }
                CheckWipe();
            }
        }
        private void FirePairSupport(BattleUnit actor)
        {
            for (int i = 0; i < _units.Count && !Stopped; i++)
            {
                BattleUnit u = _units[i];
                if (u.Cooldown > 0 || u.AimTicks > 0 || TurnWaits(u) || !ReferenceEquals(SupportPartner(u), actor)) continue;
                Act(u);
                u.Cooldown = HeavyTurn(u, NextCooldown(u));
            }
        }
        // True when the exchange owns this ready unit's action, including waiting for its partner.
        private bool PairAction(BattleUnit u)
        {
            if (!_pairsOn || !PairFighter(u) || PairFocus(u) != null) return false;
            Exchange p = ExchangeOf(u);
            if (p == null)
            {
                if (SupportPartner(u) != null) { u.Cooldown = 0; return true; }
                BattleUnit reached = _gridOn ? GridTarget(u) : null;
                // Wait for an occupied eligible enemy; still step when nothing is reached, and
                // fight non-pairing guards/leaders normally rather than making them invulnerable.
                if (reached != null && PairFighter(reached) && ExchangeOf(reached) != null) { u.Cooldown = 0; return true; }
                return false;
            }
            u.Cooldown = 0;
            if (p.Pending >= 0 || p.Turn != u.Index || _tick < p.Ready || PairTarget(u) == null) return true;
            p.Pending = u.Index; p.Lands = _tick + PairRules.Windup(u.Class);
            Emit(EventFor(BattleEventKind.PairWindup, u, PairTarget(u), PairRules.Windup(u.Class), p.Id.ToString(CultureInfo.InvariantCulture)));
            return true;
        }
        private void AppendExchanges(SnapshotBuilder b)
        {
            b.Section("pairs"); b.Field("next", _nextExchange); b.Field("count", _exchanges.Count);
            for (int i = 0; i < _exchanges.Count; i++)
            {
                Exchange p = _exchanges[i]; b.Section("pair" + i.ToString(CultureInfo.InvariantCulture));
                b.Field("id", p.Id); b.Field("a", p.A); b.Field("b", p.B); b.Field("turn", p.Turn);
                b.Field("ready", p.Ready); b.Field("pending", p.Pending); b.Field("lands", p.Lands);
                b.Field("aim", p.Aim);
            }
        }
    }
}
