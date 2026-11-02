using System;
using System.Collections.Generic;
using System.Globalization;

namespace KindredSiege.Sim.Battle
{
    /// The beasts as raid bosses, campaign rules 27 (Jacob, 1 Nov 2026: "can we finish the bear and shark just like a wow boss",
    /// then "Full plan" and "Go as written"; FightRules.BeastRaid; DESIGN_LOCKS, "The beasts as raid bosses (rules 27)"). Every
    /// number is a starting value (BeastKits).
    ///
    /// The sand shark's Feeding frenzy: at its second broken part it dives for BeastKits.FeedingTicks. Its moves stop and its
    /// parts cannot be hit; for the last ChurnTicks the sand churns under all of you; then it bursts up through all of you for
    /// BurstPercent each. Fall Back dodges it; an armed trap snares it on top instead, and its parts take ExposedPercent for
    /// ExposedTicks; a covering Warden halves a share; the Medic heals through. Then its Blood frenzy, and its sand pups burst
    /// up and latch on your back row: units after its parts on its side, benched until then, each biting the one it holds every
    /// PupBiteTicks for PupBitePercent with a bleed until you kill it. They die with the shark.
    ///
    /// The cage bear's Overload: when its ribcage breaks it roars (its Roar's nerve, no Hold and no cut off) and frost cracks
    /// race across the floor for FrostTicks; anyone of yours not falling back as they land is frozen for FrozenTicks and does
    /// nothing. In its Last stand each Pulse hits PulseRampPercent harder than the last.
    public sealed partial class BattleSimulation
    {
        private readonly bool _beastRaidOn;
        private long _feedAt = -1;
        private bool _feedDone;
        private long _exposedUntil = -1;
        private long _overloadAt = -1;
        private bool _overloadDone;
        private int _rampPulses;
        private long[] _frozenUntil = Array.Empty<long>();
        private readonly string[] _pupTarget = new string[BeastKits.PupCount];
        private readonly long[] _pupNext = new long[BeastKits.PupCount];

        /// Whether this beast fight plays the beasts as raid bosses: campaign rules 27.
        public bool BeastRaidOn => _beastRaidOn;

        /// The intermission running now, BeastKits.FeedingName or BeastKits.OverloadName, else empty.
        public string BeastIntermission => Feeding ? BeastKits.FeedingName : Overloading ? BeastKits.OverloadName : string.Empty;

        /// Ticks until the intermission's blow lands (the shark's burst, the bear's frost), or 0.
        public int BeastIntermissionTicks => Feeding ? (int)Math.Max(0L, _feedAt + BeastKits.FeedingTicks - _tick)
            : Overloading ? (int)Math.Max(0L, _overloadAt + BeastKits.FrostTicks - _tick) : 0;

        /// The warning line while the intermission's blow tells (from the churn's Tell, its last ChurnTicks; the frost's whole
        /// run), else empty.
        public string BeastWarning => Feeding && BeastIntermissionTicks < BeastKits.ChurnTicks ? BeastKits.ChurnWarning
            : Overloading ? BeastKits.FrostWarning : string.Empty;

        /// Ticks the shark's parts still take BeastKits.ExposedPercent after a trap caught its burst, or 0.
        public int BeastExposedTicks => _beastRaidOn ? (int)Math.Max(0L, _exposedUntil - _tick) : 0;

        /// The shark's sand pups in the fight now, in order.
        public IReadOnlyList<string> BeastAddIds
        {
            get
            {
                var ids = new List<string>();
                for (int n = 0; n < BeastKits.PupCount; n++)
                {
                    BattleUnit pup = PupIn(n);
                    if (pup != null && pup.Alive) ids.Add(pup.Id);
                }
                return ids;
            }
        }

        /// Whom this sand pup holds, or empty.
        public string BeastAddTargetId(string pupId)
        {
            for (int n = 0; n < BeastKits.PupCount; n++)
            {
                BattleUnit pup = PupIn(n);
                if (pup != null && pup.Alive && string.Equals(pup.Id, pupId, StringComparison.Ordinal)) return _pupTarget[n] ?? string.Empty;
            }
            return string.Empty;
        }

        /// Ticks this fighter stays frozen by the cage bear's frost, or 0.
        public int FrozenTicks(BattleUnit unit)
        {
            return unit != null && unit.Index < _frozenUntil.Length ? (int)Math.Max(0L, _frozenUntil[unit.Index] - _tick) : 0;
        }

        private bool Feeding => _feedAt >= 0 && !_feedDone;

        private bool Overloading => _overloadAt >= 0 && !_overloadDone;

        private bool Frozen(BattleUnit unit)
        {
            return unit.Index < _frozenUntil.Length && _frozenUntil[unit.Index] > _tick;
        }

        // The n-th sand pup: the units after the shark's parts on its side, or null.
        private BattleUnit PupIn(int n)
        {
            if (!_beastRaidOn || n < 0 || n >= BeastKits.PupCount) return null;
            int i = FirstPartIndex + BeastKits.PartCount + n;
            return i < _units.Count && _units[i].Side == _beastSide ? _units[i] : null;
        }

        // At the start: its pups wait out of the fight, counted as not alive, until its Feeding frenzy ends.
        private void StartBeastRaid()
        {
            _frozenUntil = new long[_units.Count];
            for (int n = 0; n < BeastKits.PupCount; n++)
            {
                _pupTarget[n] = string.Empty;
                BattleUnit pup = PupIn(n);
                if (pup == null) continue;
                pup.Benched = true;
                pup.Alive = false;
            }
        }

        // A part broke: true when the raid rules took over its phases. The shark's second broken part starts its Feeding frenzy,
        // which brings its Blood frenzy when it ends, and its last takes its pups with it; the cage bear's ribcage starts its
        // Overload.
        private bool RaidSevered(BeastPart which, int broken, BattleUnit source, int sourceSide)
        {
            if (_beastKit == BeastKit.SandShark)
            {
                if (broken >= BeastKits.PartCount)
                {
                    PupsGo(source, sourceSide);
                    return false;
                }
                if (broken < 2 || _feedAt >= 0) return false;
                StartFeeding();
                if (broken >= 3) EnterPhase(3);
                return true;
            }
            if (_beastKit == BeastKit.CageBear && which == BeastPart.Ribcage && _overloadAt < 0) StartOverload();
            return false;
        }

        // Every tick before the tells land: the Feeding frenzy's churn and burst, the frost, and the pups' bites.
        private void TickBeastRaid()
        {
            if (Feeding)
            {
                long t = _tick - _feedAt;
                BattleUnit actor = FeedingActor();
                if (t == BeastKits.FeedingTicks - BeastKits.ChurnTicks && actor != null)
                    Emit(EventFor(BattleEventKind.Tell, actor, null, BeastKits.ChurnTicks, BeastKits.FeedingName));
                if (t >= BeastKits.FeedingTicks) Burst();
            }
            if (!Stopped && Overloading && _tick - _overloadAt >= BeastKits.FrostTicks) Frost();
            if (!Stopped) TickPups();
        }

        // ------------------------------------------------------------ the shark's Feeding frenzy and its pups

        private BattleUnit FeedingActor()
        {
            BattleUnit fin = Part(BeastPart.Fin);
            return fin != null && fin.Alive ? fin : FirstLivingPart();
        }

        // It dives: a Phase event (amount the ticks it stays under), and every move it was telling is gone.
        private void StartFeeding()
        {
            _feedAt = _tick;
            for (int p = 0; p < BeastKits.PartCount; p++) ClearTell(p);
            BattleUnit actor = FeedingActor();
            if (actor == null)
            {
                _feedDone = true;
                return;
            }
            Emit(EventFor(BattleEventKind.Phase, actor, null, BeastKits.FeedingTicks, BeastKits.FeedingName));
        }

        // It bursts up through all of them, then its Blood frenzy and its pups.
        private void Burst()
        {
            _feedDone = true;
            BattleUnit fin = FeedingActor();
            if (fin == null) return;
            int foe = 1 - fin.Side;
            SideState theirs = _sides[foe];
            string name = BeastKits.FeedingName + ".";
            if (theirs.TrapArmed)
            {
                BattleUnit trapper = FindById(theirs.TrapOwnerId);
                theirs.TrapArmed = false;
                theirs.TrapOwnerId = string.Empty;
                Emit(new BattleEvent(_tick, BattleEventKind.TrapSprung, foe, trapper == null ? string.Empty : trapper.Id,
                    trapper == null ? -1 : (int)trapper.Class, fin.Id, (int)fin.Class, SliceClasses.TrapDamage, string.Empty));
                Emit(EventFor(BattleEventKind.Land, fin, trapper, BeastKits.ExposedTicks, name + BeastKits.Snared));
                _exposedUntil = _tick + BeastKits.ExposedTicks;
                ApplyDamage(trapper, foe, fin, SliceClasses.TrapDamage, TrapDetail);
                if (!Stopped && fin.Alive && _classOn) Snare(trapper, foe, fin);
                if (!Stopped) Earn(foe, FightRules.DodgePoints, FightRules.DodgeReason);
            }
            else if (theirs.FallBackTicks > 0)
            {
                Emit(EventFor(BattleEventKind.Land, fin, null, 0, name + BeastKits.Dodged));
                BeastDodged(foe, fin);
            }
            else
            {
                List<BattleUnit> all = AllMarks(foe);
                if (all.Count > 0)
                    RowHit(fin, all, BeastKits.BurstPercent, BeastKits.FeedingName, BeastKits.BurstDetail, true, BeatMet.Open, out _);
            }
            if (Stopped) return;
            EnterPhase(PartsBroken() >= 3 ? 3 : 2);
            BattleUnit actor = FeedingActor();
            if (actor != null) PupsJoin(actor);
        }

        // Its pups burst up and latch on: a Join event each (actor the part, target the pup, detail its name) and a Mark on whom
        // it holds.
        private void PupsJoin(BattleUnit actor)
        {
            int foe = 1 - actor.Side;
            var taken = new List<string>();
            for (int n = 0; n < BeastKits.PupCount; n++)
            {
                BattleUnit pup = PupIn(n);
                if (pup == null || !pup.Benched) continue;
                BattleUnit target = PupMark(foe, taken);
                if (target == null) return;
                pup.Benched = false;
                pup.Alive = true;
                _pupTarget[n] = target.Id;
                _pupNext[n] = _tick + BeastKits.PupBiteTicks;
                taken.Add(target.Id);
                Emit(EventFor(BattleEventKind.Join, actor, pup, 0, BeastKits.PupName));
                Emit(EventFor(BattleEventKind.Mark, pup, target, 0, BeastKits.PupDetail));
            }
        }

        // Whom a pup latches on: their back row first, one pup each while there are enough of them, then their front row.
        private BattleUnit PupMark(int foe, List<string> taken)
        {
            BattleUnit spare = null;
            for (int r = 0; r < 2; r++)
            {
                Row row = r == 0 ? Row.Back : Row.Front;
                for (int i = 0; i < _units.Count; i++)
                {
                    BattleUnit u = _units[i];
                    if (u.Row != row || !Markable(u, foe)) continue;
                    if (!taken.Contains(u.Id)) return u;
                    if (spare == null) spare = u;
                }
            }
            return spare;
        }

        // Each standing pup bites whom it holds every PupBiteTicks and leaves them bleeding; if they are gone it latches on
        // another.
        private void TickPups()
        {
            for (int n = 0; n < BeastKits.PupCount && !Stopped; n++)
            {
                BattleUnit pup = PupIn(n);
                if (pup == null || !pup.Alive || _tick < _pupNext[n]) continue;
                _pupNext[n] = _tick + BeastKits.PupBiteTicks;
                int foe = 1 - pup.Side;
                BattleUnit target = FindById(_pupTarget[n]);
                if (target == null || !Markable(target, foe))
                {
                    var taken = new List<string>();
                    for (int k = 0; k < BeastKits.PupCount; k++)
                    {
                        BattleUnit other = PupIn(k);
                        if (k != n && other != null && other.Alive) taken.Add(_pupTarget[k]);
                    }
                    target = PupMark(foe, taken);
                    if (target == null) continue;
                    _pupTarget[n] = target.Id;
                    Emit(EventFor(BattleEventKind.Mark, pup, target, 0, BeastKits.PupDetail));
                }
                ApplyDamage(pup, pup.Side, target, Armored(target, Share(target, BeastKits.PupBitePercent, pup)), BeastKits.PupDetail);
                if (Stopped || !target.Alive) continue;
                target.Statuses.Apply(StatusKind.Bleed, BleedRules.Ticks, BeastKits.PupBleedPercent);
                Emit(EventFor(BattleEventKind.Mark, pup, target, BleedRules.Ticks, ClassSpells.BleedMark));
            }
        }

        // The shark is dead: its pups go with it (a Kill each, detail "pup", from whoever broke its last part).
        private void PupsGo(BattleUnit source, int sourceSide)
        {
            bool breaker = source != null && source.Side != _beastSide;
            for (int n = 0; n < BeastKits.PupCount; n++)
            {
                BattleUnit pup = PupIn(n);
                if (pup == null || !pup.Alive) continue;
                pup.Hp = 0;
                pup.Alive = false;
                Emit(new BattleEvent(_tick, BattleEventKind.Kill, sourceSide, breaker ? source.Id : string.Empty,
                    breaker ? (int)source.Class : -1, pup.Id, (int)pup.Class, 0, BeastKits.PupDetail));
            }
        }

        // ------------------------------------------------------------ the cage bear's Overload and its ramp

        private BattleUnit OverloadActor()
        {
            BattleUnit maw = Part(BeastPart.Maw);
            return maw != null && maw.Alive ? maw : FirstLivingPart();
        }

        // The skull's bolt: a Phase event (amount the frost's ticks), its roar's nerve on them (no Hold blocks it, nothing cuts
        // it off), and the frost's Tell.
        private void StartOverload()
        {
            _overloadAt = _tick;
            BattleUnit actor = OverloadActor();
            if (actor == null)
            {
                _overloadDone = true;
                return;
            }
            int foe = 1 - actor.Side;
            Emit(EventFor(BattleEventKind.Phase, actor, null, BeastKits.FrostTicks, BeastKits.OverloadName));
            Emit(EventFor(BattleEventKind.Land, actor, null, BeastKits.RoarNerve, BeastKits.RoarName + "." + BeastKits.Full));
            DrainNerve(foe, BeastKits.RoarNerve, actor.Side);
            if (Stopped) return;
            Emit(EventFor(BattleEventKind.Tell, actor, null, BeastKits.FrostTicks, BeastKits.FrostName));
        }

        // The frost lands: Fall Back gets them all clear; otherwise every one of theirs it can mark is frozen (a Mark each).
        private void Frost()
        {
            _overloadDone = true;
            BattleUnit actor = OverloadActor();
            if (actor == null) return;
            int foe = 1 - actor.Side;
            string name = BeastKits.FrostName + ".";
            if (_sides[foe].FallBackTicks > 0)
            {
                Emit(EventFor(BattleEventKind.Land, actor, null, 0, name + BeastKits.Dodged));
                BeastDodged(foe, actor);
                return;
            }
            List<BattleUnit> all = AllMarks(foe);
            Emit(EventFor(BattleEventKind.Land, actor, null, all.Count, name + BeastKits.Full));
            for (int k = 0; k < all.Count; k++)
            {
                _frozenUntil[all[k].Index] = _tick + BeastKits.FrozenTicks;
                Emit(EventFor(BattleEventKind.Mark, actor, all[k], BeastKits.FrozenTicks, BeastKits.FrozenDetail));
            }
        }

        // The Pulse's percent: in the raid rules' Last stand each one PulseRampPercent harder than the last.
        private int PulseNow()
        {
            if (!_beastRaidOn || _beastPhase < 3) return BeastKits.PulsePercent;
            return BeastKits.PulsePercent * (100 + BeastKits.PulseRampPercent * _rampPulses++) / 100;
        }

        // ------------------------------------------------------------ damage and the snapshot

        // A blow on one of the beast's parts under the raid rules: none while the shark is under in its Feeding frenzy, and
        // ExposedPercent while a trap's catch has it exposed. Pups are not parts.
        private int RaidTakes(BattleUnit unit, int amount)
        {
            if (!_beastRaidOn || !TryPartOf(unit, out _)) return amount;
            if (Feeding) return 0;
            return _tick < _exposedUntil ? amount * BeastKits.ExposedPercent / 100 : amount;
        }

        // The same through the damage layers. False for a blow that does nothing.
        private bool RaidHit(BattleUnit unit, Hit hit)
        {
            if (!_beastRaidOn || !TryPartOf(unit, out _)) return true;
            if (Feeding) return false;
            if (_tick < _exposedUntil) hit.Add(HitLayer.Status, BeastKits.ExposedPercent - 100);
            return true;
        }

        private void AppendBeastRaid(SnapshotBuilder b)
        {
            b.Field("beast.feedAt", _feedAt);
            b.Field("beast.feedDone", _feedDone);
            b.Field("beast.exposedUntil", _exposedUntil);
            b.Field("beast.overloadAt", _overloadAt);
            b.Field("beast.overloadDone", _overloadDone);
            b.Field("beast.rampPulses", _rampPulses);
            for (int n = 0; n < BeastKits.PupCount; n++)
            {
                string key = "beast.pup" + n.ToString(CultureInfo.InvariantCulture) + ".";
                b.Field(key + "target", _pupTarget[n] ?? string.Empty);
                b.Field(key + "next", _pupNext[n]);
            }
            for (int i = 0; i < _frozenUntil.Length; i++)
                if (_frozenUntil[i] > _tick) b.Field("beast.frozen." + _units[i].Id, _frozenUntil[i]);
        }
    }
}
