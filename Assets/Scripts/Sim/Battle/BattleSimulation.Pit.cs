using System;
using System.Collections.Generic;

namespace KindredSiege.Sim.Battle
{
    /// The Pit's words (FightRules.Pit, campaign rules 38; Jacob, 4 Nov 2026: "i wanna see it in action then only i can judge write
    /// the code and ask codex to wire it up go"; Claude's design "The Pit: how units fight, with the parry at the core", not locked).
    public static class PitRules
    {
        /// The detail of a parryable move whose mark fell while it wound up. Its mark is locked from the start of its windup, so it
        /// lands on nobody (rule 2): a heavy swing's Release and a chief's Cleave's Land ("Cleave.lost") carry it, with 0.
        public const string Lost = "lost";
    }

    /// What kind of blow a Blow is.
    public enum BlowType
    {
        /// A fighter's turn: his auto attack, unless his turn goes to his spell, his boost or his trap instead (Blow.MaySpell).
        Turn = 0,

        /// A Marksman's Aimed Shot already aiming: it fires at its mark when it runs out.
        Aim = 1,

        /// A big move winding up: a heavy swing, a carried move, a chief's move or spell, a beast's move. Blow.Move is its threat.
        Move = 2
    }

    /// One blow coming from a fighter, for the screen to start his swing before it lands (The Pit, rule 1). Read only.
    public readonly struct Blow
    {
        public Blow(BlowType kind, string attackerId, string targetId, int ticksLeft, bool ranged, bool maySpell, Threat move)
        {
            Kind = kind;
            AttackerId = attackerId ?? string.Empty;
            TargetId = targetId ?? string.Empty;
            TicksLeft = Math.Max(1, ticksLeft);
            Ranged = ranged;
            MaySpell = kind == BlowType.Turn && maySpell;
            Move = move;
        }

        public BlowType Kind { get; }

        /// Who strikes: the fighter, the swinger, the caster, the chief or his acolyte, a beast's part.
        public string AttackerId { get; }

        /// Whom it lands on. For a Move and an Aim this is its mark, locked (rule 2), or empty when it falls on a row, a lane or a
        /// whole side. For a Turn it is whom his auto attack would hit as things stand now: Focus Fire, a fall or a Fall Back
        /// before it lands can change it, and the Hit, Miss or spell event that lands says whom it really hit.
        public string TargetId { get; }

        /// It lands during the Tick() this many calls from now: 1 is the next Tick(), and its events carry the tick
        /// CurrentTick + TicksLeft - 1. For a Turn this is exact unless a Time Snare's half speed or an Overclock ends first; for
        /// a Move it is its threat's TicksLeft. Under rules 40 a Turn's and an Aim's come from the stage's line played forward
        /// (BattleSimulation.Stage.cs): exact unless a big move starts, a fighter's checks change or a fighter falls on the way.
        public int TicksLeft { get; }

        /// True for a shot: an archer's, a Marksman's or a Trapper's. The screen sends a shot along the lane; else he steps in.
        public bool Ranged { get; }

        /// For a Turn only: true when his spell, his boost or his trap is ready by then, so his turn may go to that instead of his
        /// auto attack. The event that lands says which.
        public bool MaySpell { get; }

        /// For a Move, its threat read (name, tier, beat, parry window, answers); Threat.None for a Turn or an Aim.
        public Threat Move { get; }

        /// True for a Move a Hold can parry at its beat: the fuse under its mark (The Pit, part 6).
        public bool Parryable => Kind == BlowType.Move && Move.BeatTicks >= 0 && (Move.Answers & ThreatAnswer.Parry) != 0;
    }

    /// The Pit (FightRules.Pit, campaign rules 38): every blow is announced ahead, so the screen can play each swing at its own
    /// speed with its strike frame on the tick the sim lands it (rule 1, a read that changes nothing); a parryable move's mark
    /// is locked from the start of its windup, so a heavy swing or a chief's Cleave whose mark falls lands on nobody instead of
    /// switching to someone with no warning (rule 2); and a side winds up one parryable move at a time, the next waiting until
    /// that one lands, is parried or is lost (rule 3), so there is one thing to parry on screen at once.
    ///
    /// Claude's calls while building, Jacob's to change: a parryable move is any threat a Hold can parry at its beat
    /// (Blow.Parryable), read through Threats so the rule and the screen agree; while one winds up, a chief's other kit moves
    /// wait too, even those nobody parries, so he does one thing at a time; a forecast move that has to wait comes late; the
    /// hunt's steps and the beasts' parts are not held back yet, and a chief's second phase spell is never held back.
    public sealed partial class BattleSimulation
    {
        private readonly bool _pitOn;

        /// True when this fight plays The Pit (campaign rules 38).
        public bool PitOn => _pitOn;

        /// Every blow this side's fighters have coming, soonest first: each big move winding up (from Threats, so the two reads
        /// agree), each Aimed Shot aiming, and each fighter's next turn. Empty once the fight is over or while a chief kneels.
        /// Read only: it draws no random number and changes nothing, so the screen may ask every frame. Outside The Pit it
        /// answers the same way; only the fight's rules differ.
        public IReadOnlyList<Blow> Blows(int side)
        {
            var list = new List<Blow>();
            if (side < 0 || side > 1 || Stopped) return list;
            IReadOnlyList<Threat> moves = Threats(1 - side);
            for (int i = 0; i < moves.Count; i++)
            {
                Threat t = moves[i];
                if (t.ActorId.Length == 0) continue;
                BattleUnit actor = FindById(t.ActorId);
                list.Add(new Blow(BlowType.Move, t.ActorId, t.TargetId, t.TicksLeft, actor != null && actor.Stats.Ranged, false, t));
            }
            // From rules 40 turns and shots land one at a time, so their ticks come from the stage's line (BattleSimulation.Stage.cs).
            int[] lands = _stageOn ? StageLands() : null;
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (!u.Alive || u.Benched || u.Side != side) continue;
                if (_beastOn && u.Side == _beastSide) continue; // a beast's parts only move by their clocks: their moves are threats
                if (u.AimTicks > 0)
                {
                    if (u.PanicTicks <= 0 && !(_fight.Contracts && ActionBlocked(u, completingAim: true)) && (lands == null || lands[i] > 0))
                        list.Add(new Blow(BlowType.Aim, u.Id, u.AimTargetId, lands == null ? u.AimTicks : lands[i], true, false, Threat.None));
                    continue;
                }
                if (TurnWaits(u)) continue;
                if (_pairsOn && ExchangeOf(u) != null && PairWindupTicks(u) <= 0) continue;
                if (_pairsOn && SupportPartner(u) != null) continue;
                // With class spells a Medic treats and never attacks: her Treatment event shows it.
                if (_classOn && u.Class == UnitClass.Medic) continue;
                BattleUnit target = ChooseTarget(u);
                if (target == null) continue;
                int ticks = _pairsOn && ExchangeOf(u) != null ? PairWindupTicks(u) : lands == null ? TurnTicks(u) : lands[i];
                if (ticks <= 0) continue;
                list.Add(new Blow(BlowType.Turn, u.Id, target.Id, ticks, u.Stats.Ranged, MaySpell(u, ticks), Threat.None));
            }
            // Soonest first; ties keep the order they were found in (big moves before turns).
            for (int i = 1; i < list.Count; i++)
            {
                Blow b = list[i];
                int j = i;
                while (j > 0 && b.TicksLeft < list[j - 1].TicksLeft)
                {
                    list[j] = list[j - 1];
                    j--;
                }
                list[j] = b;
            }
            return list;
        }

        // The tick loop's reasons a fighter does nothing this tick (BattleSimulation.Tick), read without changing anything.
        private bool TurnWaits(BattleUnit u)
        {
            if (_fight.Contracts && ActionBlocked(u)) return true;
            if (_beastOn && IsHeld(u)) return true;
            if (_beastRaidOn && Frozen(u)) return true;
            if (u.PanicTicks > 0) return true;
            if (_heavyOn && IsSwingingHeavy(u)) return true;
            if (_chiefOn && u.Yielded) return true;
            if (_kitsOn && KitBusy(u)) return true;
            if (_classOn && (u.SnareTicks > 0 || u.PinTicks > 0 || u.EvacTicks > 0)) return true;
            return _parryOn && Braced(u);
        }

        // How many Tick() calls until his cooldown runs out, counting an Overclock's double speed and a Time Snare's half speed
        // as they stand now (BattleSimulation.Tick).
        private int TurnTicks(BattleUnit u)
        {
            int left = u.Cooldown;
            if (left <= 0) return 1;
            bool fast = _kitsTwoOn && _sides[u.Side].OverclockTicks > 0;
            bool slow = _comboOn && u.SlowTicks > 0;
            long tick = _tick;
            int ticks = 0;
            while (left > 0 && ticks < 100000)
            {
                ticks++;
                if (fast) left--;
                if (!(slow && (tick & 1L) == 1L)) left--;
                tick++;
            }
            return ticks;
        }

        // Whether his spell, his boost or his trap is ready by his turn, so the turn may go to it (ActWithSpell).
        private bool MaySpell(BattleUnit u, int ticks)
        {
            if (!_classOn || SimpleEnemy(u)) return false;
            switch (u.Class)
            {
                case UnitClass.Marksman:
                case UnitClass.Skirmisher:
                    return u.SpellTicks <= ticks;
                case UnitClass.Trapper:
                    return !_sides[u.Side].TrapArmed && _trapReadyIn[u.Index] <= 0;
                default:
                    return false;
            }
        }

        // Rule 2: a heavy swing whose locked mark fell lands on nobody. The Release says so, with 0 and PitRules.Lost.
        private void LostSwing(int side, BattleUnit swinger, BattleUnit mark)
        {
            Emit(new BattleEvent(_tick, BattleEventKind.Release, side, swinger.Id, (int)swinger.Class,
                mark == null ? string.Empty : mark.Id, mark == null ? -1 : (int)mark.Class, 0, PitRules.Lost));
        }

        // Rule 3: true while this side already winds up a parryable move, so its next one waits until that one lands, is parried
        // or is lost. Always false outside The Pit.
        private bool PitWaits(int side)
        {
            if (!_pitOn) return false;
            // A heavy swing, a spell, a chief's Cleave or a carried Cleave still winding up counts even while a freeze or a pin
            // holds it, when the threat read leaves it out (rules 37): it resumes later, and two would then wind up at once.
            SideState state = _sides[side];
            if (state.HeavyWindup > 0 || state.Windup > 0 || state.CleaveWindup > 0) return true;
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (u.Alive && u.Side == side && u.CarryTell > 0 && u.CarryMove == ChiefMove.Cleave) return true;
            }
            IReadOnlyList<Threat> coming = Threats(1 - side);
            for (int i = 0; i < coming.Count; i++)
                if (coming[i].BeatTicks >= 0 && (coming[i].Answers & ThreatAnswer.Parry) != 0) return true;
            return false;
        }
    }
}
