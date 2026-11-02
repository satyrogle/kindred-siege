using System;
using System.Collections.Generic;
using System.Globalization;

namespace KindredSiege.Sim.Battle
{
    /// The beasts (Jacob, 20 Oct 2026, DESIGN_LOCKS "Beasts"; 21 Oct: "go, build the shark API with your six calls"; 22 Oct:
    /// "go, fix the audit findings and add both phases", then "go, build the Cage Bear as planned"). BeastKits holds the rules
    /// and numbers. Only fights with FightRules.Beasts and a side whose rules carry a beast come here, so every other fight
    /// plays exactly as before. The beast's side is its parts, in its order (BeastKits.Parts): they never act on their own and
    /// never lose nerve; each moves only by its own clock. Its moves never fall on the other side's leader while any of their
    /// fighters stands, and on the leader once nobody else does. The shark's Dive picks who it comes up under from the fight's
    /// seeded stream; nothing else here draws.
    public sealed partial class BattleSimulation
    {
        // A beast fight, the side that is the beast (-1 without one) and which beast it is. Set in the constructor.
        private readonly bool _beastOn;

        // Rules 25 (FightRules.BeastScenes; Jacob, 1 Nov 2026: "Sim follows scenes", then "Go as written"): the shark's Bite takes
        // the marked one's whole row, its tail's Sweep all of them, and the cage bear's Grab is a Slam and Grab.
        private readonly bool _beastScenesOn;

        /// Whether this beast fight plays its moves as the approved scenes show them: campaign rules 25.
        public bool BeastScenesOn => _beastScenesOn;
        private readonly int _beastSide = -1;
        private readonly BeastKit _beastKit;

        // Each part's clock, by its place among the beast's parts: the tick its next move may start, the ticks its move still
        // tells, and the Bite's or the Grab's mark.
        private readonly long[] _partNext = new long[BeastKits.PartCount];
        private readonly int[] _partTell = new int[BeastKits.PartCount];
        private readonly string[] _partTarget = { string.Empty, string.Empty, string.Empty, string.Empty };

        // Its phase (1, then 2 at Blood frenzy or Overdrive, 3 at Last stand) and, in the shark's Last stand, the tick it digs
        // away on.
        private int _beastPhase = 1;
        private long _escapeAt;

        // The cage bear's claw: whoever it holds (empty for nobody) and the tick it grabbed them.
        private string _heldId = string.Empty;
        private long _heldAt;

        /// True when this fight is against a beast.
        public bool BeastOn => _beastOn;

        /// The side that is the beast, or -1.
        public int BeastSide => _beastSide;

        /// The beast a side is, or BeastKit.None.
        public BeastKit Beast(int side)
        {
            return _beastOn && side == _beastSide ? _beastKit : BeastKit.None;
        }

        /// The unit that is this part of the beast, alive or broken, or null outside a beast fight and for another beast's
        /// part. Its id ends with a dot and BeastKits.Name(part); Focus Fire takes that id, and a broken part cannot be focused.
        public BattleUnit Part(BeastPart part)
        {
            if (!_beastOn || BeastKits.KitOf(part) != _beastKit) return null;
            int i = FirstPartIndex + BeastKits.Slot(part);
            return i >= 0 && i < _units.Count ? _units[i] : null;
        }

        /// Which part of the beast this unit is. False for anyone else.
        public bool TryPartOf(BattleUnit unit, out BeastPart part)
        {
            part = BeastPart.Jaw;
            if (!_beastOn || unit == null || unit.Side != _beastSide) return false;
            int k = unit.Index - FirstPartIndex;
            if (k < 0 || k >= BeastKits.PartCount) return false;
            part = BeastKits.PartAt(_beastKit, k);
            return true;
        }

        /// True once this part is broken: its move is gone, and it cannot be targeted. After the cage bear's heart breaks
        /// every part of it is down, though only the ones broken before (and the heart) drop trophies.
        public bool PartBroken(BeastPart part)
        {
            BattleUnit u = Part(part);
            return u != null && !u.Alive;
        }

        /// Ticks until this part's move lands, or 0 while it is not telling one. For the Fin it is the time the shark stays
        /// under the sand.
        public int BeastTellTicks(BeastPart part)
        {
            return Part(part) != null ? _partTell[BeastKits.Slot(part)] : 0;
        }

        /// Who the Bite or the Grab falls on while it tells (the glow under them), else empty. The other moves hit a whole
        /// row or side, and the Dive picks its mark as it comes up.
        public string BeastTellTargetId(BeastPart part)
        {
            return BeastTellTicks(part) > 0 ? _partTarget[BeastKits.Slot(part)] : string.Empty;
        }

        /// Ticks before this part's next move can start, or 0 while it tells, once it is due, once it is broken, and for the
        /// ribcage, which has no move.
        public int BeastNextTicks(BeastPart part)
        {
            if (Part(part) == null || PartBroken(part) || BeastKits.EveryTicks(part) <= 0) return 0;
            int slot = BeastKits.Slot(part);
            if (_partTell[slot] > 0) return 0;
            return (int)Math.Max(0L, _partNext[slot] - _tick);
        }

        /// True while the shark is under the sand (its Dive): blows on it mostly miss.
        public bool BeastUnder => _beastKit == BeastKit.SandShark && (_partTell[BeastKits.Slot(BeastPart.Fin)] > 0 || Feeding);

        /// True while a live Hold timed to the Bite's last FightRules.PerfectHoldTicks would parry it.
        public bool BiteParryable => BeastTellTicks(BeastPart.Jaw) > 0 && _fight.PerfectHold;

        /// The beast's phase: 1, then 2 once two parts are broken (Blood frenzy, Overdrive), 3 once three are (Last stand). 0
        /// without a beast.
        public int BeastPhase => _beastOn ? _beastPhase : 0;

        /// In the shark's Last stand, the ticks before it digs away for good, or 0. It digs away LastStandTicks after the tick
        /// its third part broke on. The cage bear never runs.
        public int BeastEscapeTicks =>
            _beastKit == BeastKit.SandShark && _beastPhase >= 3 ? (int)Math.Max(0L, _escapeAt - _tick) : 0;

        /// The cage bear: the one of yours its claw holds, out of the fight, or empty. They do nothing, nothing of the bear's
        /// reaches them but the crush, and nobody heals or covers them until the claw breaks or they fall.
        public string BeastHeldId => _heldId;

        /// The cage bear: true while its ribcage stands. Every hit on it lands at BeastKits.CagePercent, and its heart cannot
        /// be hit or focused (CanStrike says StrikeBlock.Caged).
        public bool BeastCaged => _beastKit == BeastKit.CageBear && !PartBroken(BeastPart.Ribcage);

        // ------------------------------------------------------------ setup

        // The beast's side holds its parts in its order, side 0's units first.
        private int FirstPartIndex => _beastSide == 0 ? 0 : _setup.Side(0).Count;

        private BattleUnit PartIn(int slot)
        {
            return _units[FirstPartIndex + slot];
        }

        private void StartBeast()
        {
            if (!BeastKits.SideFits(_beastKit, _setup.Side(_beastSide).Count, _beastRaidOn))
                throw new ArgumentException("A beast fight needs the beast's " + BeastKits.PartCount.ToString(CultureInfo.InvariantCulture) +
                    " parts, and nobody else, on its side (from rules 27 the shark's pups after them).");
            for (int p = 0; p < BeastKits.PartCount; p++) _partNext[p] = BeastKits.EveryTicks(BeastKits.PartAt(_beastKit, p));
            if (_beastRaidOn) StartBeastRaid();
        }

        private bool IsPart(BattleUnit unit)
        {
            return _beastOn && unit != null && unit.Side == _beastSide;
        }

        // In the cage bear's claw: out of the fight.
        private bool IsHeld(BattleUnit unit)
        {
            return _heldId.Length > 0 && string.Equals(unit.Id, _heldId, StringComparison.Ordinal);
        }

        // The cage bear's heart while its ribcage stands: nobody can hit it, so nobody tries (Spared, CanStrike, Focus Fire).
        private bool Shielded(BattleUnit unit)
        {
            return _beastKit == BeastKit.CageBear && ReferenceEquals(unit, Part(BeastPart.Heart)) && BeastCaged;
        }

        private bool AnyTell()
        {
            for (int p = 0; p < BeastKits.PartCount; p++) if (_partTell[p] > 0) return true;
            return false;
        }

        private void ClearTell(int slot)
        {
            _partTell[slot] = 0;
            _partTarget[slot] = string.Empty;
        }

        private BattleUnit FirstLivingPart()
        {
            for (int p = 0; p < BeastKits.PartCount; p++)
            {
                BattleUnit u = PartIn(p);
                if (u.Alive) return u;
            }
            return null;
        }

        private int PartsBroken()
        {
            int n = 0;
            for (int p = 0; p < BeastKits.PartCount; p++) if (!PartIn(p).Alive) n++;
            return n;
        }

        // Ticks from one of a part's moves to its next, by phase: a third faster from the second phase; in the shark's Last
        // stand twice as fast, and in the cage bear's its heart pulses every LastStandPulseTicks.
        private int EveryNow(BeastPart part)
        {
            int every = BeastKits.EveryTicks(part);
            if (_beastPhase >= 3 && part == BeastPart.Heart) return BeastKits.LastStandPulseTicks;
            if (_beastPhase >= 3) return Math.Max(1, every * BeastKits.LastStandEveryPercent / 100);
            if (_beastPhase >= 2) return Math.Max(1, every * BeastKits.FrenzyEveryPercent / 100);
            return every;
        }

        // ------------------------------------------------------------ who it can hit

        // A fighter of theirs still stands in the fight, not counting their leader or the one in the claw.
        private bool AnyFighterLeft(int side)
        {
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (u.Alive && u.Side == side && !u.IsLeader && !IsHeld(u)) return true;
            }
            return false;
        }

        // Who the beast's moves can fall on: their fighters, never their leader, until the leader is all that stands. Then him,
        // so his fall can lose the fight (the audit, 22 Oct 2026: a lone leader could not be hurt, and the clock gave the win).
        // Never the one in the cage bear's claw: only the crush reaches them.
        private bool Markable(BattleUnit u, int side)
        {
            return u.Alive && u.Side == side && !IsHeld(u) && (!u.IsLeader || !AnyFighterLeft(side));
        }

        private bool AnyMarkIn(int side, Row row)
        {
            for (int i = 0; i < _units.Count; i++)
                if (_units[i].Row == row && Markable(_units[i], side)) return true;
            return false;
        }

        private List<BattleUnit> MarksIn(int side, Row row)
        {
            var list = new List<BattleUnit>();
            for (int i = 0; i < _units.Count; i++)
                if (_units[i].Row == row && Markable(_units[i], side)) list.Add(_units[i]);
            return list;
        }

        // The Warden covering this one from the beast's moves: never a leader, even one who is a Warden.
        private BattleUnit BeastGuard(BattleUnit target)
        {
            BattleUnit warden = GuardingWarden(target);
            return warden != null && !warden.IsLeader ? warden : null;
        }

        // Their Medic still stands in the fight, out of the claw.
        private bool MedicStands(int side)
        {
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (u.Alive && u.Side == side && u.Class == UnitClass.Medic && !IsHeld(u)) return true;
            }
            return false;
        }

        // ------------------------------------------------------------ the moves

        // Each standing part with a move starts it when its clock comes round. A snared part waits; the shark's others wait
        // while it is under the sand, and it only dives when no other move is telling; the cage bear's claw waits while it
        // holds someone.
        private void StartBeastMoves()
        {
            // Rules 27: in its Feeding frenzy its clocks stand still.
            if (Feeding)
            {
                for (int p = 0; p < BeastKits.PartCount; p++) _partNext[p]++;
                return;
            }
            for (int p = 0; p < BeastKits.PartCount && !Stopped; p++)
            {
                BeastPart part = BeastKits.PartAt(_beastKit, p);
                BattleUnit unit = PartIn(p);
                if (!unit.Alive || BeastKits.EveryTicks(part) <= 0 || _partTell[p] > 0 || _tick < _partNext[p]) continue;
                bool waits = unit.SnareTicks > 0 || BeastUnder || (part == BeastPart.Fin && AnyTell()) ||
                    (part == BeastPart.Claw && _heldId.Length > 0);
                bool marks = part == BeastPart.Jaw || part == BeastPart.Claw;
                BattleUnit mark = marks && !waits ? BeastMark(unit) : null;
                if (waits || (marks && mark == null))
                {
                    _partNext[p] = _tick + 1;
                    continue;
                }
                int tell = BeastKits.TellTicks(part);
                _partTell[p] = tell;
                _partTarget[p] = mark == null ? string.Empty : mark.Id;
                _partNext[p] = _tick + EveryNow(part);
                Emit(EventFor(BattleEventKind.Tell, unit, mark, tell, BeastKits.MoveName(part)));
            }
        }

        // The shark's Last stand runs down, and the cage bear's claw crushes whoever it holds; then the tells: at zero each
        // move lands. A snared part's tell waits; a broken part's is gone; Focus Fire on the roaring maw cuts its Roar off.
        private void AdvanceBeast()
        {
            if (_beastRaidOn)
            {
                TickBeastRaid();
                if (Stopped) return;
            }
            if (_beastKit == BeastKit.SandShark && _beastPhase >= 3 && _tick >= _escapeAt)
            {
                Flee(BeastKits.EscapeDetail);
                return;
            }
            if (_heldId.Length > 0) Crush();
            for (int p = 0; p < BeastKits.PartCount && !Stopped; p++)
            {
                if (_partTell[p] <= 0) continue;
                BattleUnit unit = PartIn(p);
                if (!unit.Alive)
                {
                    ClearTell(p);
                    continue;
                }
                if (unit.SnareTicks > 0) continue;
                BeastPart part = BeastKits.PartAt(_beastKit, p);
                if (part == BeastPart.Maw && IsFocusing(1 - unit.Side, unit))
                {
                    CutRoar(unit);
                    continue;
                }
                _partTell[p]--;
                if (_partTell[p] > 0) continue;
                // Under the parry at the center the landing is its beat (BattleSimulation.Parry.cs).
                if (_parryOn)
                {
                    int hold = HoldBeat(unit.Side, unit, unit, BeastKits.MoveName(part));
                    if (hold > 0)
                    {
                        _partTell[p] = hold;
                        continue;
                    }
                    unit.BeatFlags = 0;
                }
                switch (part)
                {
                    case BeastPart.Jaw: LandBite(unit); break;
                    case BeastPart.Fin: LandDive(unit); break;
                    case BeastPart.Spine: LandRow(unit, Row.Back, BeastKits.VolleyPercent, BeastKits.VolleyName, BeastKits.VolleyDetail, false); break;
                    case BeastPart.Tail: LandRow(unit, Row.Front, BeastKits.SweepPercent, BeastKits.SweepName, BeastKits.SweepDetail, true,
                        _beastScenesOn); break;
                    case BeastPart.Claw: LandGrab(unit); break;
                    case BeastPart.Maw: LandRoar(unit); break;
                    case BeastPart.Heart: LandPulse(unit); break;
                }
                _partTarget[p] = string.Empty;
                CheckWipe();
            }
        }

        // Whoever stands across from the jaw or the claw: the nearest to its lane in their front row, their back row once it
        // has fallen; never their leader while anyone else stands, never the one already in the claw.
        private BattleUnit BeastMark(BattleUnit part)
        {
            int foe = 1 - part.Side;
            Row row = AnyMarkIn(foe, Row.Front) ? Row.Front : Row.Back;
            int lane = LanePosition(part);
            BattleUnit best = null;
            int bestDistance = int.MaxValue;
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (u.Row != row || !Markable(u, foe)) continue;
                int distance = Math.Abs(LanePosition(u) - lane);
                if (distance < bestDistance)
                {
                    best = u;
                    bestDistance = distance;
                }
            }
            return best;
        }

        // A move's hit on one fighter: this percent of their max health, at the part's power.
        private static int Share(BattleUnit target, int percent, BattleUnit part)
        {
            return Math.Max(1, target.MaxHp * percent / 100 * part.PowerPercent / 100);
        }

        // What a Warden takes of a beast's move: less with a road scar.
        private int Scarred(BattleUnit warden, int taken)
        {
            return SpecOf(warden).BeastScars > 0 ? Math.Max(1, taken * 100 / BeastKits.ScarPercent) : taken;
        }

        // A Warden's cut on one of the beast's blows: his Guard, Bastion's when his piece has it.
        private int BeastGuardCut(BattleUnit warden)
        {
            return _classOn ? GuardCut(warden) : SliceClasses.GuardReductionPercent;
        }

        // What a hit of this size will take off a victim, as ApplyDamage will take it: their armour, then a standing Steel Storm
        // wall on their side.
        private int Lands(BattleUnit victim, int share, int sourceSide)
        {
            int amount = Armored(victim, share);
            if (_spellsOn && _sides[victim.Side].WallTicks > 0 && sourceSide == 1 - victim.Side)
                amount = Math.Max(1, amount * ChiefSpells.WallDamagePercent / 100);
            return amount;
        }

        // In its Blood frenzy every hit the shark lands leaves its victim bleeding.
        private void FrenzyWound(BattleUnit part, BattleUnit victim)
        {
            if (_beastPhase < 2 || victim == null || !victim.Alive || _finished) return;
            victim.Statuses.Apply(StatusKind.Bleed, BleedRules.Ticks, BeastKits.FrenzyBleedPercent);
            Emit(EventFor(BattleEventKind.Mark, part, victim, BleedRules.Ticks, ClassSpells.BleedMark));
        }

        // A side dodged one of its moves with Fall Back: the Shade's favours pay as for any dodge, and Wrongfoot holds back that
        // part's next move (its turns run on its own clock, not a cooldown).
        private void BeastDodged(int dodger, BattleUnit part)
        {
            Earn(dodger, FightRules.DodgePoints, FightRules.DodgeReason);
            SideFavours f = _rules[dodger].Favours;
            if (f.DodgePoints > 0) Earn(dodger, f.DodgePoints, FightRules.DodgeReason);
            if (f.WrongfootTicks <= 0 || !part.Alive || !TryPartOf(part, out BeastPart which)) return;
            int slot = BeastKits.Slot(which);
            _partNext[slot] = Math.Max(_partNext[slot], _tick) + f.WrongfootTicks;
            Emit(EventFor(BattleEventKind.Reaction, part, null, f.WrongfootTicks, SideFavours.WrongfootDetail));
        }

        // The Bite: parried by a live Hold started in the parry window (the jaw staggers); taken by a Warden on his shield,
        // himself or the one he covers, and then the jaw takes BiteBackPercent of it; soaked by any other Hold on the front row;
        // or full.
        private void LandBite(BattleUnit jaw)
        {
            int foe = 1 - jaw.Side;
            SideState theirs = _sides[foe];
            int slot = BeastKits.Slot(BeastPart.Jaw);
            BattleUnit target = FindById(_partTarget[slot]);
            if (target == null || !Markable(target, foe)) target = BeastMark(jaw);
            if (target == null) return;
            string name = BeastKits.BiteName + ".";

            BeatMet met = _parryOn ? MeetBeat(foe) : BeatMet.Open;
            if (met == BeatMet.Parry)
            {
                Emit(EventFor(BattleEventKind.Land, jaw, target, 0, name + BeastKits.Parried));
                BeastParried(jaw, BeastPart.Jaw, BeastKits.BiteName);
                return;
            }
            if (!_parryOn && _fight.PerfectHold && theirs.HoldTicks > 0 && theirs.LastHoldTick >= 0 &&
                _tick - theirs.LastHoldTick <= ParryWindowTicks)
            {
                Emit(EventFor(BattleEventKind.Land, jaw, target, 0, name + BeastKits.Parried));
                Emit(new BattleEvent(_tick, BattleEventKind.Parry, foe, string.Empty, -1, jaw.Id, (int)jaw.Class,
                    FightRules.StaggerTicks, BeastKits.BiteName));
                _partNext[slot] += FightRules.StaggerTicks;
                theirs.HoldTicks = 0;
                theirs.LastParryTick = _tick;
                theirs.ParriedCasterId = jaw.Id;
                Earn(foe, FightRules.ParryPoints, FightRules.ParryReason);
                return;
            }

            if (_beastScenesOn)
            {
                BiteRow(jaw, target, met);
                return;
            }

            int damage = Share(target, BeastKits.BitePercent, jaw);
            BattleUnit warden = target.Class == UnitClass.Warden ? target : BeastGuard(target);
            if (warden != null)
            {
                int taken = Scarred(warden, damage * (100 - BeastGuardCut(warden)) / 100);
                Emit(EventFor(BattleEventKind.Land, jaw, target, taken, name + BeastKits.Guarded));
                Emit(EventFor(BattleEventKind.Guard, warden, target, damage - taken, string.Empty));
                ApplyDamage(jaw, jaw.Side, warden, Armored(warden, taken), BeastKits.BiteDetail);
                FrenzyWound(jaw, warden);
                if (!Stopped && warden.Alive && jaw.Alive)
                    ApplyDamage(warden, foe, jaw, Math.Max(1, damage * BeastKits.BiteBackPercent / 100), BeastKits.BiteBackDetail);
                if (!Stopped) Earn(foe, ChiefKits.GuardPoints, ChiefKits.Name(ChiefAnswer.Guard));
                return;
            }

            if (_parryOn && !BeastBeatLands(jaw, met)) return;
            bool soaked = theirs.HoldTicks > 0 && target.Row == Row.Front;
            if (soaked) damage = Math.Max(1, damage * HoldSoak(foe) / 100);
            Emit(EventFor(BattleEventKind.Land, jaw, target, damage, name + (soaked ? BeastKits.Soaked : BeastKits.Full)));
            ApplyDamage(jaw, jaw.Side, target, Armored(target, damage), BeastKits.BiteDetail);
            FrenzyWound(jaw, target);
        }

        // Rules 25: the Bite takes everyone in the marked one's row, each answered as the one bite is. A Warden takes the share of
        // whoever he covers on his shield (his own as well), less his Guard, and the jaw takes BiteBackPercent of each share back
        // once every share has landed; any other Hold soaks the front row; or full. Each share is its own Land event on its
        // fighter.
        private void BiteRow(BattleUnit jaw, BattleUnit marked, BeatMet met)
        {
            int foe = 1 - jaw.Side;
            SideState theirs = _sides[foe];
            string name = BeastKits.BiteName + ".";
            List<BattleUnit> hit = MarksIn(foe, marked.Row);
            var open = new List<BattleUnit>();
            var backs = new List<(BattleUnit warden, int amount)>();
            for (int k = 0; k < hit.Count && !Stopped; k++)
            {
                BattleUnit target = hit[k];
                if (!target.Alive) continue;
                int damage = Share(target, BeastKits.BitePercent, jaw);
                BattleUnit warden = target.Class == UnitClass.Warden ? target : BeastGuard(target);
                if (warden == null || !warden.Alive)
                {
                    open.Add(target);
                    continue;
                }
                int taken = Scarred(warden, damage * (100 - BeastGuardCut(warden)) / 100);
                Emit(EventFor(BattleEventKind.Land, jaw, target, taken, name + BeastKits.Guarded));
                Emit(EventFor(BattleEventKind.Guard, warden, target, damage - taken, string.Empty));
                ApplyDamage(jaw, jaw.Side, warden, Armored(warden, taken), BeastKits.BiteDetail);
                FrenzyWound(jaw, warden);
                backs.Add((warden, Math.Max(1, damage * BeastKits.BiteBackPercent / 100)));
            }
            if (Stopped) return;
            if (open.Count > 0 && (!_parryOn || BeastBeatLands(jaw, met)))
            {
                bool holding = theirs.HoldTicks > 0;
                for (int k = 0; k < open.Count && !Stopped; k++)
                {
                    BattleUnit target = open[k];
                    if (!target.Alive) continue;
                    int damage = Share(target, BeastKits.BitePercent, jaw);
                    bool soaked = holding && target.Row == Row.Front;
                    if (soaked) damage = Math.Max(1, damage * HoldSoak(foe) / 100);
                    Emit(EventFor(BattleEventKind.Land, jaw, target, damage, name + (soaked ? BeastKits.Soaked : BeastKits.Full)));
                    ApplyDamage(jaw, jaw.Side, target, Armored(target, damage), BeastKits.BiteDetail);
                    FrenzyWound(jaw, target);
                }
            }
            for (int k = 0; k < backs.Count && !Stopped; k++)
                if (backs[k].warden.Alive && jaw.Alive)
                    ApplyDamage(backs[k].warden, foe, jaw, backs[k].amount, BeastKits.BiteBackDetail);
            if (!Stopped && backs.Count > 0) Earn(foe, ChiefKits.GuardPoints, ChiefKits.Name(ChiefAnswer.Guard));
        }

        // Who a move on all of them falls on: every one of their fighters it can mark, both rows.
        private List<BattleUnit> AllMarks(int side)
        {
            var list = new List<BattleUnit>();
            for (int i = 0; i < _units.Count; i++)
                if (Markable(_units[i], side)) list.Add(_units[i]);
            return list;
        }

        // A part's move parried under the parry at the center, after its Land: it staggers (its next move comes
        // FightRules.StaggerTicks later) and reels, and it is left with an Opening. A beast has no nerve to crack.
        private void BeastParried(BattleUnit part, BeastPart which, string move)
        {
            _partNext[BeastKits.Slot(which)] += FightRules.StaggerTicks;
            ParriedBeat(part.Side, part, move, ChiefKits.ParryNerve, FightRules.ParryPoints, false);
        }

        // A part's move that is neither parried nor safely answered lands: a stale Hold costs your nerve (a beast has none to
        // crack, so a parry's crack of a chief's move stands in), and with no Hold your parry chain starts over. False when the
        // block ended the fight.
        private bool BeastBeatLands(BattleUnit part, BeatMet met)
        {
            if (met == BeatMet.Block) BlockedBeat(part.Side, part, ChiefKits.ParryNerve);
            else LandedBeat(part.Side);
            return !Stopped;
        }

        // The shark bursts up: into an armed snare, which springs on the fin and holds it on top (the bite never lands); clear
        // of a side falling back; or under one of the back row, picked from the fight's stream (the front row once the back has
        // fallen), a Warden covering them taking it.
        private void LandDive(BattleUnit fin)
        {
            int foe = 1 - fin.Side;
            SideState theirs = _sides[foe];
            string name = BeastKits.DiveName + ".";

            if (theirs.TrapArmed)
            {
                BattleUnit trapper = FindById(theirs.TrapOwnerId);
                theirs.TrapArmed = false;
                theirs.TrapOwnerId = string.Empty;
                Emit(new BattleEvent(_tick, BattleEventKind.TrapSprung, foe, trapper == null ? string.Empty : trapper.Id,
                    trapper == null ? -1 : (int)trapper.Class, fin.Id, (int)fin.Class, SliceClasses.TrapDamage, string.Empty));
                Emit(EventFor(BattleEventKind.Land, fin, trapper, 0, name + BeastKits.Snared));
                ApplyDamage(trapper, foe, fin, SliceClasses.TrapDamage, TrapDetail);
                if (!Stopped && fin.Alive && _classOn) Snare(trapper, foe, fin);
                if (!Stopped) Earn(foe, FightRules.DodgePoints, FightRules.DodgeReason);
                return;
            }

            if (theirs.FallBackTicks > 0)
            {
                Emit(EventFor(BattleEventKind.Land, fin, null, 0, name + BeastKits.Dodged));
                BeastDodged(foe, fin);
                return;
            }

            List<BattleUnit> pool = MarksIn(foe, AnyMarkIn(foe, Row.Back) ? Row.Back : Row.Front);
            if (pool.Count == 0) return;
            BattleUnit target = pool[_rng.Next(pool.Count)];
            BeatMet met = _parryOn ? MeetBeat(foe) : BeatMet.Open;
            if (met == BeatMet.Parry)
            {
                Emit(EventFor(BattleEventKind.Land, fin, target, 0, name + BeastKits.Parried));
                BeastParried(fin, BeastPart.Fin, BeastKits.DiveName);
                return;
            }
            int damage = Share(target, BeastKits.DivePercent, fin);
            BattleUnit warden = BeastGuard(target);
            if (warden != null)
            {
                int taken = Scarred(warden, damage * (100 - BeastGuardCut(warden)) / 100);
                Emit(EventFor(BattleEventKind.Land, fin, target, taken, name + BeastKits.Guarded));
                Emit(EventFor(BattleEventKind.Guard, warden, target, damage - taken, string.Empty));
                ApplyDamage(fin, fin.Side, warden, Armored(warden, taken), BeastKits.DiveDetail);
                FrenzyWound(fin, warden);
                if (!Stopped) Earn(foe, ChiefKits.GuardPoints, ChiefKits.Name(ChiefAnswer.Guard));
                return;
            }
            if (_parryOn && !BeastBeatLands(fin, met)) return;
            bool soaked = met == BeatMet.Block && target.Row == Row.Front;
            if (soaked) damage = Math.Max(1, damage * HoldSoak(foe) / 100);
            Emit(EventFor(BattleEventKind.Land, fin, target, damage, name + (soaked ? BeastKits.Soaked : BeastKits.Full)));
            ApplyDamage(fin, fin.Side, target, Armored(target, damage), BeastKits.DiveDetail);
            FrenzyWound(fin, target);
        }

        // The Volley on their back row, the Sweep on their front row (each the other row once its own has fallen). Fall Back gets
        // everyone clear; a Warden covering one of them takes their share at WardPercent while he still stands; and against the
        // Sweep, with their Medic standing and nobody falling to it, she heals through it. Planned in full before it lands, so
        // a Warden who would fall partway leaves the later shares to whoever he covered, and the Land event says what landed.
        // Rules 25 (everyone): the Sweep falls on all of them, both rows.
        private void LandRow(BattleUnit part, Row first, int percent, string move, string detail, bool heals, bool everyone = false)
        {
            int foe = 1 - part.Side;
            SideState theirs = _sides[foe];
            string name = move + ".";
            if (theirs.FallBackTicks > 0)
            {
                Emit(EventFor(BattleEventKind.Land, part, null, 0, name + BeastKits.Dodged));
                BeastDodged(foe, part);
                return;
            }

            Row row = AnyMarkIn(foe, first) ? first : first == Row.Front ? Row.Back : Row.Front;
            List<BattleUnit> hit = everyone ? AllMarks(foe) : MarksIn(foe, row);
            if (hit.Count == 0) return;
            BeatMet met = _parryOn ? MeetBeat(foe) : BeatMet.Open;
            if (met == BeatMet.Parry && TryPartOf(part, out BeastPart which))
            {
                Emit(EventFor(BattleEventKind.Land, part, null, 0, name + BeastKits.Parried));
                BeastParried(part, which, move);
                return;
            }
            RowHit(part, hit, percent, move, detail, heals, met, out _);
        }

        // The hit of a move on many (LandRow's, and rules 25's Slam): each share at percent, a Warden covering one of them taking
        // it at WardPercent while he still stands, the Medic healing through when heals. beat says whether its beat was settled
        // here (it landed full under the parry at the center). False when the fight stopped.
        private bool RowHit(BattleUnit part, List<BattleUnit> hit, int percent, string move, string detail, bool heals, BeatMet met,
            out bool beat)
        {
            beat = false;
            int foe = 1 - part.Side;
            string name = move + ".";
            var left = new int[_units.Count];
            for (int i = 0; i < _units.Count; i++) left[i] = _units[i].Hp;
            var takers = new BattleUnit[hit.Count];
            var shares = new int[hit.Count];
            var prevented = new int[hit.Count];
            bool guarded = false, falls = false;
            int total = 0;
            for (int k = 0; k < hit.Count; k++)
            {
                if (left[hit[k].Index] <= 0) continue; // fell to an earlier share of this move
                int share = Share(hit[k], percent, part);
                if (met == BeatMet.Block && hit[k].Row == Row.Front) share = Math.Max(1, share * HoldSoak(foe) / 100);
                BattleUnit warden = BeastGuard(hit[k]);
                takers[k] = hit[k];
                if (warden != null && left[warden.Index] > 0)
                {
                    int halved = Scarred(warden, Math.Max(1, share * BeastKits.WardPercent / 100));
                    prevented[k] = share - halved;
                    share = halved;
                    takers[k] = warden;
                    guarded = true;
                }
                shares[k] = share;
                total += share;
                left[takers[k].Index] -= Lands(takers[k], share, part.Side);
                if (left[takers[k].Index] <= 0) falls = true;
            }
            bool medic = heals && MedicStands(foe);
            string how = guarded ? BeastKits.Guarded : heals && medic && !falls ? BeastKits.Healed : BeastKits.Full;
            if (_parryOn && how == BeastKits.Full)
            {
                beat = true;
                if (!BeastBeatLands(part, met)) return false;
            }
            Emit(EventFor(BattleEventKind.Land, part, null, total, name + how));
            for (int k = 0; k < hit.Count && !Stopped; k++)
            {
                if (takers[k] == null) continue;
                if (!ReferenceEquals(takers[k], hit[k])) Emit(EventFor(BattleEventKind.Guard, takers[k], hit[k], prevented[k], string.Empty));
                ApplyDamage(part, part.Side, takers[k], Armored(takers[k], shares[k]), detail);
                FrenzyWound(part, takers[k]);
            }
            if (Stopped) return false;
            if (how == BeastKits.Guarded) Earn(foe, ChiefKits.GuardPoints, ChiefKits.Name(ChiefAnswer.Guard));
            else if (how == BeastKits.Healed) Earn(foe, ChiefKits.HealPoints, ChiefKits.Name(ChiefAnswer.Heal));
            return !Stopped;
        }

        // The cage bear's Grab on whoever it reached for (or whoever stands across from the claw now): Fall Back gets clear; a
        // Warden covering them blocks it on his shield and takes GrabPercent less his Guard; or the claw closes on them for
        // GrabPercent and holds them out of the fight: their aim is lost and they cover nobody.
        private void LandGrab(BattleUnit claw)
        {
            int foe = 1 - claw.Side;
            string name = BeastKits.GrabName + ".";
            if (_sides[foe].FallBackTicks > 0)
            {
                Emit(EventFor(BattleEventKind.Land, claw, null, 0, name + BeastKits.Dodged));
                BeastDodged(foe, claw);
                return;
            }

            BattleUnit target = FindById(_partTarget[BeastKits.Slot(BeastPart.Claw)]);
            if (target == null || !Markable(target, foe)) target = BeastMark(claw);
            if (target == null) return;
            BeatMet met = _parryOn ? MeetBeat(foe) : BeatMet.Open;
            if (met == BeatMet.Parry)
            {
                Emit(EventFor(BattleEventKind.Land, claw, target, 0, name + BeastKits.Parried));
                BeastParried(claw, BeastPart.Claw, BeastKits.GrabName);
                return;
            }
            // Rules 25, the Slam and Grab: first the ice wave on all of them, then the claw on the one it marked.
            bool beatDone = false;
            if (_beastScenesOn)
            {
                List<BattleUnit> all = AllMarks(foe);
                if (all.Count > 0 && !RowHit(claw, all, BeastKits.SlamPercent, BeastKits.SlamName, BeastKits.SlamDetail, false, met,
                    out beatDone)) return;
                if (!target.Alive || !Markable(target, foe)) return;
            }
            int damage = Share(target, BeastKits.GrabPercent, claw);
            BattleUnit warden = BeastGuard(target);
            if (warden != null)
            {
                int taken = Scarred(warden, damage * (100 - BeastGuardCut(warden)) / 100);
                Emit(EventFor(BattleEventKind.Land, claw, target, taken, name + BeastKits.Guarded));
                Emit(EventFor(BattleEventKind.Guard, warden, target, damage - taken, string.Empty));
                ApplyDamage(claw, claw.Side, warden, Armored(warden, taken), BeastKits.GrabDetail);
                if (!Stopped) Earn(foe, ChiefKits.GuardPoints, ChiefKits.Name(ChiefAnswer.Guard));
                return;
            }

            if (_parryOn && !beatDone && !BeastBeatLands(claw, met)) return;
            if (met == BeatMet.Block && target.Row == Row.Front) damage = Math.Max(1, damage * HoldSoak(foe) / 100);
            Emit(EventFor(BattleEventKind.Land, claw, target, damage, name + (met == BeatMet.Block ? BeastKits.Soaked : BeastKits.Full)));
            ApplyDamage(claw, claw.Side, target, Armored(target, damage), BeastKits.GrabDetail);
            if (Stopped || !target.Alive) return;
            _heldId = target.Id;
            _heldAt = _tick;
            target.GuardTargetId = string.Empty;
            target.AimTicks = 0;
            target.AimTargetId = string.Empty;
        }

        // Each second it holds them the claw crushes whoever it holds, at CrushPercent, twice as hard in its Overdrive. Once they
        // fall the claw is free again.
        private void Crush()
        {
            BattleUnit claw = Part(BeastPart.Claw);
            BattleUnit held = FindById(_heldId);
            if (held == null || !held.Alive)
            {
                LetGo(claw, false);
                return;
            }
            long since = _tick - _heldAt;
            if (since <= 0 || since % SimClock.TicksPerSecond != 0) return;
            int percent = BeastKits.CrushPercent;
            if (_beastPhase >= 2) percent = percent * BeastKits.OverdriveCrushPercent / 100;
            int squeeze = Share(held, percent, claw);
            // Under the parry at the center every squeeze is a beat (BattleSimulation.Parry.cs): one parry breaks the grip, a
            // stale Hold soaks it and cracks your nerve.
            if (_parryOn)
            {
                BeatMet met = MeetBeat(held.Side);
                if (met == BeatMet.Parry)
                {
                    Emit(EventFor(BattleEventKind.Land, claw, held, 0, BeastKits.CrushName + "." + BeastKits.Parried));
                    LetGo(claw, true);
                    BeastParried(claw, BeastPart.Claw, BeastKits.CrushName);
                    return;
                }
                if (!BeastBeatLands(claw, met)) return;
                if (met == BeatMet.Block) squeeze = Math.Max(1, squeeze * HoldSoak(held.Side) / 100);
            }
            ApplyDamage(claw, claw.Side, held, Armored(held, squeeze), BeastKits.CrushDetail);
            if (!held.Alive) LetGo(claw, false);
            CheckWipe();
        }

        // The claw lets go. When it broke, whoever it held is free (a Reaction "released"); when they fell, its next Grab comes
        // a full turn later.
        private void LetGo(BattleUnit claw, bool broke)
        {
            BattleUnit held = FindById(_heldId);
            _heldId = string.Empty;
            _heldAt = 0;
            if (broke)
            {
                if (held != null && held.Alive && !_finished) Emit(EventFor(BattleEventKind.Reaction, claw, held, 0, BeastKits.ReleasedDetail));
                return;
            }
            int slot = BeastKits.Slot(BeastPart.Claw);
            _partNext[slot] = Math.Max(_partNext[slot], _tick + EveryNow(BeastPart.Claw));
        }

        // The cage bear's Roar: a Hold up blocks it, a paused one too; or it takes RoarNerve of their nerve. The Land amount is
        // the nerve it took.
        private void LandRoar(BattleUnit maw)
        {
            int foe = 1 - maw.Side;
            string name = BeastKits.RoarName + ".";
            // Under the parry at the center its peak is its beat (BattleSimulation.Parry.cs): parried, the maw staggers; a stale
            // Hold still blocks the roar but cracks your nerve.
            BeatMet met = _parryOn ? MeetBeat(foe) : BeatMet.Open;
            if (met == BeatMet.Parry)
            {
                Emit(EventFor(BattleEventKind.Land, maw, null, 0, name + BeastKits.Parried));
                BeastParried(maw, BeastPart.Maw, BeastKits.RoarName);
                return;
            }
            if (_sides[foe].HoldTicks > 0)
            {
                Emit(EventFor(BattleEventKind.Land, maw, null, 0, name + BeastKits.Blocked));
                if (_parryOn) BlockedBeat(maw.Side, maw, ChiefKits.ParryNerve);
                return;
            }
            if (_parryOn) LandedBeat(maw.Side);
            Emit(EventFor(BattleEventKind.Land, maw, null, BeastKits.RoarNerve, name + BeastKits.Full));
            DrainNerve(foe, BeastKits.RoarNerve, maw.Side);
        }

        // Focus Fire on the maw while it roars cuts the Roar off before it lands, and pays as breaking a cast does.
        private void CutRoar(BattleUnit maw)
        {
            int foe = 1 - maw.Side;
            ClearTell(BeastKits.Slot(BeastPart.Maw));
            Emit(EventFor(BattleEventKind.Land, maw, null, 0, BeastKits.RoarName + "." + BeastKits.Cut));
            Earn(foe, FightRules.InterruptPoints, FightRules.InterruptReason);
        }

        // The cage bear's Pulse. While the ribcage stands it mends each of the bear's other standing parts by PulseMendPercent
        // (the Land amount is all it mended). Once the cage is broken it hits their whole side, both rows, for PulsePercent
        // each: a Hold soaks it, a paused one too; with their Medic standing and nobody falling to it she heals through it; or
        // it lands in full.
        private void LandPulse(BattleUnit heart)
        {
            string name = BeastKits.PulseName + ".";
            // Under the parry at the center the beat is its beat (BattleSimulation.Parry.cs): parried, the heart staggers and
            // nothing comes of it, neither the mending nor the hit.
            int foeSide = 1 - heart.Side;
            BeatMet met = _parryOn ? MeetBeat(foeSide) : BeatMet.Open;
            if (met == BeatMet.Parry)
            {
                Emit(EventFor(BattleEventKind.Land, heart, null, 0, name + BeastKits.Parried));
                BeastParried(heart, BeastPart.Heart, BeastKits.PulseName);
                return;
            }
            if (BeastCaged)
            {
                var mended = new int[BeastKits.PartCount];
                int healed = 0;
                for (int p = 0; p < BeastKits.PartCount; p++)
                {
                    BattleUnit u = PartIn(p);
                    if (!u.Alive || ReferenceEquals(u, heart)) continue;
                    mended[p] = Math.Min(Math.Max(1, u.MaxHp * BeastKits.PulseMendPercent / 100), u.MaxHp - u.Hp);
                    healed += mended[p];
                }
                Emit(EventFor(BattleEventKind.Land, heart, null, healed, name + BeastKits.Mended));
                for (int p = 0; p < BeastKits.PartCount; p++)
                    if (mended[p] > 0) Heal(heart, PartIn(p), mended[p], BeastKits.PulseDetail);
                return;
            }

            int foe = 1 - heart.Side;
            var hit = new List<BattleUnit>();
            for (int i = 0; i < _units.Count; i++) if (Markable(_units[i], foe)) hit.Add(_units[i]);
            if (hit.Count == 0) return;
            int percent = PulseNow();
            bool soaked = _sides[foe].HoldTicks > 0;
            var shares = new int[hit.Count];
            bool falls = false;
            int total = 0;
            for (int k = 0; k < hit.Count; k++)
            {
                int share = Share(hit[k], percent, heart);
                if (soaked) share = Math.Max(1, share * HoldSoak(foe) / 100);
                shares[k] = share;
                total += share;
                if (hit[k].Hp - Lands(hit[k], share, heart.Side) <= 0) falls = true;
            }
            string how = soaked ? BeastKits.Soaked : MedicStands(foe) && !falls ? BeastKits.Healed : BeastKits.Full;
            if (_parryOn && how != BeastKits.Healed && !BeastBeatLands(heart, met)) return;
            Emit(EventFor(BattleEventKind.Land, heart, null, total, name + how));
            for (int k = 0; k < hit.Count && !Stopped; k++)
                ApplyDamage(heart, heart.Side, hit[k], Armored(hit[k], shares[k]), BeastKits.PulseDetail);
            if (!Stopped && how == BeastKits.Healed) Earn(foe, ChiefKits.HealPoints, ChiefKits.Name(ChiefAnswer.Heal));
        }

        // ------------------------------------------------------------ the cage

        // A blow from the other side on the cage bear while its ribcage stands: its heart takes none of it, every other part
        // CagePercent, and the ribcage itself CageUnleashPercent of that while the striker's side Unleashes.
        private int BeastTakes(BattleUnit part, int sourceSide, int amount)
        {
            amount = RaidTakes(part, amount);
            if (amount <= 0 || !BeastCaged) return amount;
            if (ReferenceEquals(part, Part(BeastPart.Heart))) return 0;
            int percent = BeastKits.CagePercent;
            if (ReferenceEquals(part, Part(BeastPart.Ribcage)) && _sides[sourceSide].UnleashTicks > 0)
                percent = percent * BeastKits.CageUnleashPercent / 100;
            return Math.Max(1, amount * percent / 100);
        }

        // The same through the damage layers (BattleSimulation.Layers.cs): the ribcage's cut in the guard layer, an Unleash
        // through it in the orders layer. False for a blow on its heart, which takes none.
        private bool CageHit(BattleUnit part, int sourceSide, Hit hit)
        {
            if (!RaidHit(part, hit)) return false;
            if (!BeastCaged) return true;
            if (ReferenceEquals(part, Part(BeastPart.Heart))) return false;
            hit.Add(HitLayer.Guard, BeastKits.CagePercent - 100);
            if (ReferenceEquals(part, Part(BeastPart.Ribcage)) && _sides[sourceSide].UnleashTicks > 0)
                hit.Add(HitLayer.Orders, BeastKits.CageUnleashPercent - 100);
            return true;
        }

        // ------------------------------------------------------------ breaking parts, phases and scars

        // A part fell: it is broken, its move is gone, and whoever broke it is named on the Sever event (their road scar). The
        // cage bear's claw lets go of whoever it held, and its heart drops it dead. Otherwise its second broken part brings its
        // second phase, its third its Last stand.
        private void Severed(BattleUnit source, int sourceSide, BattleUnit part)
        {
            if (!TryPartOf(part, out BeastPart which)) return;
            bool breaker = source != null && source.Side != part.Side;
            Emit(new BattleEvent(_tick, BattleEventKind.Sever, sourceSide, breaker ? source.Id : string.Empty,
                breaker ? (int)source.Class : -1, part.Id, (int)part.Class, 0, BeastKits.Name(which)));
            ClearTell(BeastKits.Slot(which));
            if (which == BeastPart.Claw && _heldId.Length > 0) LetGo(part, true);
            if (which == BeastPart.Heart)
            {
                HeartStops(source, sourceSide);
                return;
            }
            int broken = PartsBroken();
            if (_beastRaidOn && RaidSevered(which, broken, source, sourceSide)) return;
            if (broken >= 2) EnterPhase(2);
            if (broken >= 3) EnterPhase(3);
        }

        // The cage bear's heart broke: it drops dead on the spot. Its claw lets go, and its standing parts go down with it (a
        // Kill each, detail "heart", from whoever broke the heart), but none of them is broken, so only the parts broken before
        // come home as trophies with the heart.
        private void HeartStops(BattleUnit source, int sourceSide)
        {
            if (_heldId.Length > 0) LetGo(Part(BeastPart.Claw), true);
            bool breaker = source != null && source.Side != _beastSide;
            for (int p = 0; p < BeastKits.PartCount; p++)
            {
                BattleUnit u = PartIn(p);
                ClearTell(p);
                if (!u.Alive) continue;
                u.Hp = 0;
                u.Alive = false;
                Emit(new BattleEvent(_tick, BattleEventKind.Kill, sourceSide, breaker ? source.Id : string.Empty,
                    breaker ? (int)source.Class : -1, u.Id, (int)u.Class, 0, BeastKits.HeartDetail));
            }
            CheckWipe();
        }

        // A new phase: one Phase event (actor a standing part, amount 0, detail its name), and its waiting moves come sooner at
        // once. The shark's Last stand starts its clock. Nothing once every part is broken: then it dies.
        private void EnterPhase(int phase)
        {
            if (phase <= _beastPhase) return;
            BattleUnit actor = FirstLivingPart();
            if (actor == null) return;
            _beastPhase = phase;
            if (phase >= 3 && _beastKit == BeastKit.SandShark) _escapeAt = _tick + BeastKits.LastStandTicks;
            Emit(EventFor(BattleEventKind.Phase, actor, null, 0, BeastKits.PhaseName(_beastKit, phase)));
            for (int p = 0; p < BeastKits.PartCount; p++)
            {
                BeastPart part = BeastKits.PartAt(_beastKit, p);
                BattleUnit u = PartIn(p);
                if (!u.Alive || BeastKits.EveryTicks(part) <= 0 || _partTell[p] > 0) continue;
                _partNext[p] = Math.Min(_partNext[p], _tick + EveryNow(part));
            }
        }

        // The shark dives away: when its Last stand runs out ("escape") or the fight's clock does ("time"). The side facing it
        // holds the field and keeps what it broke.
        private void Flee(string detail)
        {
            if (_finished) return;
            BattleUnit actor = FirstLivingPart();
            if (actor != null) Emit(EventFor(BattleEventKind.Flee, actor, null, 0, detail));
            Finish(1 - _beastSide, detail == BeastKits.EscapeDetail ? EscapedReason : "time");
        }

        // The fight's clock ran out on a beast: the shark dives away and leaves you the field; the cage bear never runs, so it
        // holds its ground and you are driven off.
        private void BeastTimeUp()
        {
            if (_beastKit == BeastKit.SandShark)
            {
                Flee(BeastKits.TimeDetail);
                return;
            }
            Finish(_beastSide, "time");
        }

        /// The finish reason when a beast dug away in its Last stand (FightEnd.Escaped).
        public const string EscapedReason = "escaped";

        // A scarred fighter's spell blow on the beast (an Aimed Shot or a strike) hits ScarPercent as hard.
        private int ScarSpell(BattleUnit attacker, BattleUnit target, int damage)
        {
            return IsPart(target) && SpecOf(attacker).BeastScars > 0 ? damage * BeastKits.ScarPercent / 100 : damage;
        }

        private void AppendBeast(SnapshotBuilder b)
        {
            b.Field("beastSide", _beastSide);
            b.Field("beastPhase", _beastPhase);
            b.Field("beastEscapeAt", _escapeAt);
            for (int p = 0; p < BeastKits.PartCount; p++)
            {
                string key = "beast." + BeastKits.Name(BeastKits.PartAt(_beastKit, p)) + ".";
                b.Field(key + "next", _partNext[p]);
                b.Field(key + "tell", _partTell[p]);
                b.Field(key + "target", _partTarget[p]);
            }
            // Written only under rules 27, so every older beast fight keeps its checksum.
            if (_beastRaidOn) AppendBeastRaid(b);
            // Written only for the cage bear, so every shark fight keeps its checksum.
            if (_beastKit != BeastKit.CageBear) return;
            b.Field("beast.held", _heldId);
            b.Field("beast.heldAt", _heldAt);
        }
    }
}
