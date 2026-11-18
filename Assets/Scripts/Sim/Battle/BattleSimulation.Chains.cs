using System;
using System.Collections.Generic;
using System.Globalization;

namespace KindredSiege.Sim.Battle
{
    /// Road combat's chains and bonds (Jacob, 25 Oct 2026; HANDOFF_LOG lines 11263 and 11358; FightRules.RoadCombat):
    /// - A setup is one mark on one enemy (MarkEvent): who left it, and the chain it belongs to. A mark a reaction leaves
    ///   continues that reaction's chain; any other starts a new one.
    /// - The first of the side's cashers for that mark (a Marksman for a snare, a Skirmisher for an Opening or a bleeding
    ///   wound) who is eligible reacts at once, out of turn: alive, not snared, frenzied, panicking or aiming, able to reach
    ///   it, not held back by Fall Back, not following a Focus Fire on someone else, not already in this chain, and with his
    ///   last cast at least half his spell's clock ago. His reaction is his cast: his clock starts again after it. Nobody
    ///   eligible, and the setup is never claimed (a Chain event with RoadRules.Unready says so).
    /// - A mark pays its point and its nerve once, whoever cashes it. A target that dies, flees or loses the mark cancels it;
    ///   a reaction never moves to another enemy.
    /// - Three cashes by one casher of one fighter's marks in one fight, on any enemies, bond them for the run. Bonded, he
    ///   answers that fighter's setups at once, with no half clock wait and no aim, at least BondGapTicks apart; inside that
    ///   gap the pair acts unbonded.
    public static class ChainRules
    {
        public const int BondCombos = 3;
        public const int BondGapTicks = 3 * SimClock.TicksPerSecond / 2;

        /// The Chain event's detail suffixes: a bonded answer, and a setup nobody was ready to claim.
        public const string Bonded = "bonded";
        public const string Unready = "unready";
    }

    public sealed partial class BattleSimulation
    {
        // A mark waiting for its casher's reaction this tick.
        private sealed class PendingMark
        {
            internal PendingMark(int side, string targetId, string mark, int chain, string originId)
            {
                Side = side;
                TargetId = targetId;
                Mark = mark;
                Chain = chain;
                OriginId = originId;
            }

            internal int Side { get; }
            internal string TargetId { get; }
            internal string Mark { get; }
            internal int Chain { get; }
            internal string OriginId { get; }
        }

        // How many of one fighter's marks one casher cashed this fight.
        private sealed class PairCredit
        {
            internal PairCredit(string originId, string casherId)
            {
                OriginId = originId;
                CasherId = casherId;
            }

            internal string OriginId { get; }
            internal string CasherId { get; }
            internal int Count { get; set; }
        }

        private int _chainCounter;
        private int _currentChain;
        private readonly List<PendingMark> _pending = new List<PendingMark>();
        private readonly List<PairCredit> _credits = new List<PairCredit>();
        private readonly List<BondPair> _bondsNow = new List<BondPair>();

        /// The bonded pairs in this fight: the setup's, then any formed in it, in order.
        public IReadOnlyList<BondPair> BondsNow => _bondsNow;

        private void StartRoadCombat()
        {
            for (int i = 0; i < _setup.Bonds.Count; i++) _bondsNow.Add(_setup.Bonds[i]);
        }

        // A mark was set (MarkEvent): it remembers who left it and its chain, and waits for its casher's reaction.
        private void MarkSet(int side, BattleUnit by, BattleUnit on, string mark)
        {
            int chain = _currentChain > 0 ? _currentChain : ++_chainCounter;
            string origin = by == null ? string.Empty : by.Id;
            if (mark == ClassSpells.SnareMark) on.Statuses.Mark(StatusKind.Snare, origin, chain);
            else if (mark == ClassSpells.OpeningMark) on.Statuses.Mark(StatusKind.Opening, origin, chain);
            else if (mark == ClassSpells.BleedMark) on.Statuses.Mark(StatusKind.Bleed, origin, chain);
            else return;
            _pending.Add(new PendingMark(side, on.Id, mark, chain, origin));
        }

        // Every mark set this tick gets its reaction, oldest first; a reaction's own marks join the queue.
        private void ResolveReactions()
        {
            for (int guard = 0; _pending.Count > 0 && !_finished && guard < 64; guard++)
            {
                PendingMark p = _pending[0];
                _pending.RemoveAt(0);
                React(p);
            }
        }

        private void React(PendingMark p)
        {
            if (ClearRoadOn && _unitNerveOn && p.Side != 0) return;
            BattleUnit target = FindById(p.TargetId);
            if (target == null || !target.Alive || target.Side == p.Side) return;
            // From rules 43 nobody chains at a leader no blow can reach (BattleSimulation.Leaders.cs).
            if (LeadersOn && LeaderProtected(target)) return;
            // The mark must still be on him, and still be this one.
            if (p.Mark == ClassSpells.SnareMark && (target.SnareTicks <= 0 || target.SnareChain != p.Chain)) return;
            if (p.Mark == ClassSpells.OpeningMark && (target.OpeningTicks <= 0 || target.OpeningChain != p.Chain)) return;
            if (p.Mark == ClassSpells.BleedMark && (target.BleedTicks <= 0 || target.BleedChain != p.Chain)) return;
            UnitClass cashes = p.Mark == ClassSpells.SnareMark ? UnitClass.Marksman : UnitClass.Skirmisher;
            BattleUnit bonded = null, ready = null;
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                // Nobody reacts to his own mark.
                if (u.Side != p.Side || u.Class != cashes || u.Id == p.OriginId || !CanReact(u, target, p.Chain)) continue;
                // From rules 24 the squad's pair bonds its two classes too (BattleSimulation.Whisperer.cs).
                if (bonded == null && (IsBonded(p.OriginId, u.Id) || PairBonded(p.OriginId, u)) &&
                    (u.LastBondedTick < 0 || _tick - u.LastBondedTick >= BondGap(u.Side)))
                    bonded = u;
                if (ready == null && HalfReady(u)) ready = u;
            }
            BattleUnit casher = bonded ?? ready;
            if (casher == null)
            {
                Emit(new BattleEvent(_tick, BattleEventKind.Chain, p.Side, string.Empty, -1, target.Id, (int)target.Class, p.Chain,
                    p.Mark + "." + ChainRules.Unready));
                return;
            }
            casher.LastChainId = p.Chain;
            Emit(EventFor(BattleEventKind.Chain, casher, target, p.Chain, bonded != null ? p.Mark + "." + ChainRules.Bonded : p.Mark));
            int saved = _currentChain;
            _currentChain = p.Chain;
            if (bonded != null) casher.LastBondedTick = _tick;
            // A pair's answer is free once a cycle of his own spell: his clock runs on (rules 24, BattleSimulation.Whisperer.cs).
            bool free = bonded != null && FreeAnswer(p.OriginId, casher, target);
            if (cashes == UnitClass.Marksman)
            {
                if (bonded != null)
                {
                    // A bonded shot needs no aim.
                    if (!free)
                    {
                        casher.SpellTicks = AimEvery(casher);
                        casher.FreeSpent = false;
                    }
                    Shots(casher, target);
                }
                else
                {
                    StartAimAt(casher, target);
                    casher.AimChainId = p.Chain;
                }
            }
            else
            {
                if (!free)
                {
                    casher.SpellTicks = StrikeEvery(casher);
                    casher.FreeSpent = false;
                }
                Strikes(casher, target);
            }
            _currentChain = saved;
            CheckWipe();
        }

        // Able to react to a setup on this target, in this chain, right now (the clock aside).
        private bool CanReact(BattleUnit u, BattleUnit target, int chain)
        {
            if (_fight.Contracts && ActionBlocked(u)) return false;
            if (!u.Alive || u.SnareTicks > 0 || u.Frenzied || u.PanicTicks > 0 || u.AimTicks > 0 || u.LastChainId == chain) return false;
            if (MeleeHeldBack(u) != StrikeBlock.None || (target.Row == Row.Back && !CanReachBack(u)) || Spared(u, target)) return false;
            // From rules 44 only a fighter whose class shape reaches the mark answers it (BattleSimulation.Grid.cs).
            if (_gridOn && u.GridCol >= 0 && !ReferenceEquals(GridTarget(u), target)) return false;
            SideState mine = _sides[u.Side];
            if (mine.FocusTicks > 0 && mine.FocusTargetId.Length > 0 && mine.FocusTargetId != target.Id)
            {
                BattleUnit focus = FindById(mine.FocusTargetId);
                if (focus != null && focus.Alive) return false;
            }
            return true;
        }

        // His last cast was at least half his spell's clock ago.
        private bool HalfReady(BattleUnit u)
        {
            int clock = u.Class == UnitClass.Marksman ? AimEvery(u) : StrikeEvery(u);
            return u.SpellTicks <= clock / 2;
        }

        private bool IsBonded(string originId, string casherId)
        {
            if (string.IsNullOrEmpty(originId)) return false;
            for (int i = 0; i < _bondsNow.Count; i++)
                if (_bondsNow[i].OriginId == originId && _bondsNow[i].CasherId == casherId) return true;
            return false;
        }

        // A spell's blow cashed this mark on this target: it pays once, and the pair's combo counts toward their bond.
        private void RoadCash(BattleUnit by, BattleUnit on, string mark)
        {
            Status status = on.Statuses[mark == ClassSpells.SnareMark ? StatusKind.Snare
                : mark == ClassSpells.OpeningMark ? StatusKind.Opening : StatusKind.Bleed];
            int chain = status.Chain;
            string origin = status.SourceId;
            status.Chain = 0;
            if (chain == 0) return;
            MarkCashed(by, on);
            Credit(by, origin);
        }

        // Called after real HP loss but before Kill/End. A finishing combo must not append past End.
        private void ConfirmTeamwork(BattleUnit by, BattleUnit on, string detail)
        {
            if (!SquadScreenOn || by == null) return;
            string mark, origin; int chain;
            if (detail == ClassSpells.AimedPinDetail)
            { mark = ClassSpells.SnareMark; origin = on.SnareOriginId; chain = on.SnareChain; }
            else if (detail == ClassSpells.StrikeOpenDetail)
            { mark = ClassSpells.OpeningMark; origin = on.OpeningOriginId; chain = on.OpeningChain; }
            else if (detail == ClassSpells.RipDetail)
            { mark = ClassSpells.BleedMark; origin = on.BleedOriginId; chain = on.BleedChain; }
            else return;
            if (chain == 0 || string.IsNullOrEmpty(origin) || origin == by.Id) return;
            var setter = FindById(origin);
            if (setter == null || setter.Side != by.Side) return;
            Emit(EventFor(BattleEventKind.ComboSetup, setter, on, chain, mark));
            Emit(EventFor(BattleEventKind.ComboFinish, by, on, chain, mark));
        }

        private void Credit(BattleUnit casher, string originId)
        {
            if (string.IsNullOrEmpty(originId) || originId == casher.Id) return;
            BattleUnit origin = FindById(originId);
            if (origin == null || origin.Side != casher.Side) return;
            PairCredit credit = null;
            for (int i = 0; i < _credits.Count; i++)
                if (_credits[i].OriginId == originId && _credits[i].CasherId == casher.Id) credit = _credits[i];
            if (credit == null)
            {
                credit = new PairCredit(originId, casher.Id);
                _credits.Add(credit);
            }
            credit.Count++;
            if (credit.Count < ChainRules.BondCombos || IsBonded(originId, casher.Id)) return;
            _bondsNow.Add(new BondPair(originId, casher.Id));
            Emit(EventFor(BattleEventKind.Bond, casher, origin, credit.Count, string.Empty));
        }

        // Chains and bonds in the snapshot, written only under road combat.
        private void AppendChains(SnapshotBuilder b)
        {
            b.Section("chains");
            b.Field("counter", _chainCounter);
            b.Field("pending", _pending.Count);
            for (int i = 0; i < _pending.Count; i++)
                b.Field("pending" + i.ToString(CultureInfo.InvariantCulture), _pending[i].Side + ">" + _pending[i].TargetId + ">" +
                    _pending[i].Mark + ">" + _pending[i].Chain + ">" + _pending[i].OriginId);
            b.Field("credits", _credits.Count);
            for (int i = 0; i < _credits.Count; i++)
                b.Field("credit" + i.ToString(CultureInfo.InvariantCulture), _credits[i].OriginId + ">" + _credits[i].CasherId + ">" +
                    _credits[i].Count);
            b.Field("bondsNow", _bondsNow.Count);
            for (int i = 0; i < _bondsNow.Count; i++)
                b.Field("bondNow" + i.ToString(CultureInfo.InvariantCulture), _bondsNow[i].OriginId + ">" + _bondsNow[i].CasherId);
        }
    }
}
