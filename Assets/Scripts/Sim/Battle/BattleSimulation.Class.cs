using System;

namespace KindredSiege.Sim.Battle
{
    /// The class spells and their marks (Jacob, 20 Oct 2026; DESIGN_LOCKS, "The squad's spells, marks and class
    /// pieces"; ClassSpells holds the rules and numbers). Only fights with FightRules.ClassSpells come here, so every
    /// other fight plays exactly as before. No random numbers are drawn here beyond a blow's usual roll.
    public sealed partial class BattleSimulation
    {
        private readonly bool _classOn;

        /// What a blow is: an auto attack, or a class spell's.
        private enum BlowKind
        {
            Auto = 0,
            AimedShot = 1,
            Strike = 2
        }

        /// True when this fight plays the class spells.
        public bool ClassSpellsOn => _classOn;

        // Each Marksman and Skirmisher starts halfway to their first spell.
        private void StartSpellClocks()
        {
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (u.Class == UnitClass.Marksman) u.SpellTicks = AimEvery(u) / 2;
                else if (u.Class == UnitClass.Skirmisher) u.SpellTicks = StrikeEvery(u) / 2;
            }
        }

        // A fighter's clocks for this tick. Held by a snare, or aiming, it does nothing else and its clocks wait; otherwise
        // its spell comes a tick closer. False when it does nothing this tick.
        private bool SpellClock(BattleUnit unit)
        {
            // A black hole's pin, a freeze and an Evac hold him too (BattleSimulation.Combos.cs).
            if (unit.SnareTicks > 0 || unit.AimTicks > 0 || unit.PinTicks > 0 || unit.EvacTicks > 0) return false;
            if (unit.SpellTicks > 0) unit.SpellTicks--;
            return true;
        }

        // The fighter's piece's bend, when it is one of its own class's.
        private ClassBend BendOf(BattleUnit unit)
        {
            ClassBend bend = SpecOf(unit).Bend;
            return ClassSpells.IsFor(bend, unit.Class) ? bend : ClassBend.None;
        }

        // His second piece's bend, the same way: a rank 3 fighter's second slot (rules 23). None without one.
        private ClassBend Bend2Of(BattleUnit unit)
        {
            if (!_floorOn) return ClassBend.None;
            ClassBend bend = SpecOf(unit).Bend2;
            return ClassSpells.IsFor(bend, unit.Class) ? bend : ClassBend.None;
        }

        // Whether either of his pieces bends his spell this way.
        private bool HasBend(BattleUnit unit, ClassBend bend)
        {
            return bend != ClassBend.None && (BendOf(unit) == bend || Bend2Of(unit) == bend);
        }

        // The rank of the piece carrying this combo: his second's when only the second carries it.
        private int RankOf(BattleUnit unit, ClassBend combo)
        {
            UnitSpec spec = SpecOf(unit);
            return BendOf(unit) != combo && Bend2Of(unit) == combo ? spec.ComboRank2 : spec.ComboRank;
        }

        // What a fighter does when its turn comes, with class spells: its spell when it is ready, else its auto attack.
        private void ActWithSpell(BattleUnit unit)
        {
            if (SimpleEnemy(unit)) { Attack(unit, unit.Power); return; }
            // Under the road kit his boost comes first when it is ready and has a real target (BattleSimulation.Boosts.cs).
            if (_roadKitOn && TryBoost(unit)) return;
            switch (unit.Class)
            {
                case UnitClass.Warden:
                    // Guard: he covers an ally (as before), then swings. Every fourth swing of Event Horizon is its bash.
                    UpdateGuard(unit);
                    // At rank 2, or under Mend and Hold, a second ward (rules 24, BattleSimulation.Whisperer.cs).
                    if (_whisperOn) SecondGuard(unit);
                    if (_comboOn && EventHorizon(unit)) return;
                    Attack(unit, unit.Power);
                    return;
                case UnitClass.Medic:
                    // Treatment. She never attacks: with nobody to treat she waits. A chief's band's Medic treats him first.
                    // From rules 24 her habit and rank 2 (BattleSimulation.Whisperer.cs).
                    if (_whisperOn)
                    {
                        WhisperTreat(unit);
                        return;
                    }
                    BattleUnit patient = PatientFor(unit);
                    if (patient != null) Treat(unit, patient);
                    return;
                case UnitClass.Trapper:
                    // Barbed Snare: she arms the side's trap in her turn (as before), otherwise shoots.
                    if (ArmTrap(unit)) return;
                    Attack(unit, unit.Power);
                    return;
                case UnitClass.Marksman:
                    if (unit.SpellTicks <= 0 && StartAim(unit)) return;
                    Attack(unit, unit.Power);
                    return;
                case UnitClass.Skirmisher:
                    if (unit.SpellTicks <= 0 && TryStrike(unit)) return;
                    Attack(unit, unit.Power);
                    return;
                default:
                    Attack(unit, unit.Power);
                    return;
            }
        }

        // The enemy a spell goes for: the side's Focus Fire target when it can reach it; else the most hurt enemy with
        // the mark it cashes in (a snare for the Aimed Shot; an Opening, then a bleeding wound, for the strike); else
        // whoever its auto attack would hit. Null when it can strike nobody right now.
        private BattleUnit SpellTarget(BattleUnit unit, bool aimed)
        {
            if (MeleeHeldBack(unit) != StrikeBlock.None) return null;
            // Whoever her Hook dragged in front of her shields comes first for her side too (ChiefKits, 22 Oct 2026).
            BattleUnit dragged = _kitsTwoOn ? DraggedFoe(unit.Side) : null;
            if (dragged != null && !LeaderProtected(dragged)) return dragged;
            // Around the Corner's shot goes for their healer once Focus Fire and snared enemies are served (BattleSimulation.Combos.cs).
            BattleUnit healer = aimed && _comboOn ? AroundTheCorner(unit) : null;
            SideState mine = _sides[unit.Side];
            bool reach = CanReachBack(unit);
            if (mine.FocusTicks > 0 && mine.FocusTargetId.Length > 0)
            {
                BattleUnit focus = FindById(mine.FocusTargetId);
                if (focus != null && focus.Alive && !LeaderProtected(focus) && focus.Side != unit.Side && (focus.Row == Row.Front || reach) && !Spared(unit, focus) &&
                    focus.EvacTicks <= 0)
                    return focus;
            }
            // The habit comes after the combo's own target (rules 24, BattleSimulation.Whisperer.cs): Around the Corner's
            // snared enemies and healer first for one who wears it.
            BattleUnit habit = _whisperOn ? HabitTarget(unit) : null;
            if (habit != null && !(aimed && _comboOn && Wears(unit, ClassBend.AroundTheCorner))) return habit;
            BattleUnit best = null;
            int bestScore = 0;
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit e = _units[i];
                if (!e.Alive || e.Side == unit.Side || LeaderProtected(e) || (e.Row == Row.Back && !reach) || Spared(unit, e)) continue;
                if (e.EvacTicks > 0) continue; // out of reach after an Evac (BattleSimulation.Combos.cs)
                int mark = aimed ? (e.SnareTicks > 0 ? 1 : 0) : (e.OpeningTicks > 0 ? 2 : e.BleedTicks > 0 ? 1 : 0);
                if (mark == 0) continue;
                int score = mark * 1000 + (1000 - e.HpPermille);
                if (score > bestScore)
                {
                    best = e;
                    bestScore = score;
                }
            }
            return best ?? healer ?? habit ?? ChooseTarget(unit);
        }

        // ------------------------------------------------------------ Marksman: Aimed Shot

        // He stops and aims at one enemy: twice as fast at a snared one. False when there is nobody to aim at.
        private bool StartAim(BattleUnit unit)
        {
            BattleUnit target = SpellTarget(unit, true);
            if (target == null) return false;
            // An upgraded Counter Shot needs no aim (rules 24, BattleSimulation.Whisperer.cs).
            if (_whisperOn && CounterAtOnce(unit, target)) return true;
            StartAimAt(unit, target);
            // The portal Around the Corner's shot comes through, from behind its mark (BattleSimulation.Combos.cs).
            if (Wears(unit, ClassBend.AroundTheCorner))
                Emit(EventFor(BattleEventKind.Portal, unit, target, unit.AimTicks, ClassSpells.Name(ClassBend.AroundTheCorner)));
            return true;
        }

        // He stops and aims at this enemy.
        private void StartAimAt(BattleUnit unit, BattleUnit target)
        {
            // Under the road kit his Steady, when ready, goes on this shot (BattleSimulation.Boosts.cs).
            if (_roadKitOn) SteadyAim(unit, target);
            bool pin = target.SnareTicks > 0;
            int ticks = ClassSpells.AimTicks;
            if (HasBend(unit, ClassBend.Deadeye)) ticks = ticks * ClassSpells.DeadeyePercent / 100;
            else if (HasBend(unit, ClassBend.Quickdraw)) ticks = ticks * ClassSpells.QuickdrawAimPercent / 100;
            if (pin) ticks = ticks * ClassSpells.PinAimPercent / 100;
            // The faster hook (rules 23, BattleSimulation.Shade.cs).
            unit.AimTicks = Math.Max(1, Clock(unit, ticks));
            unit.AimTargetId = target.Id;
            Emit(EventFor(BattleEventKind.Aim, unit, target, unit.AimTicks, pin ? ClassSpells.PinDetail : ClassSpells.AimDetail));
        }

        // Aims run on; one that has run its time fires at its mark, or, if the mark fell, at the best shot left. A
        // panicking Marksman's aim waits.
        private void AdvanceAims()
        {
            for (int i = 0; i < _units.Count && !_finished; i++)
            {
                BattleUnit u = _units[i];
                if (!u.Alive || u.AimTicks <= 0 || u.PanicTicks > 0) continue;
                u.AimTicks--;
                if (u.AimTicks > 0) continue;
                BattleUnit target = FindById(u.AimTargetId);
                u.AimTargetId = string.Empty;
                u.SpellTicks = AimEvery(u);
                // His spell went off: a new cycle for his free answer (rules 24).
                u.FreeSpent = false;
                // A shot aimed in a road combat chain stays in it; one that has to find a new mark starts afresh.
                int chain = u.AimChainId;
                u.AimChainId = 0;
                if (target == null || !target.Alive || Spared(u, target))
                {
                    target = SpellTarget(u, true);
                    chain = 0;
                }
                int saved = _currentChain;
                _currentChain = chain;
                // Stopped Bullets: a shot that hangs frozen fires nothing yet, and the third fires all three (Combos.cs).
                int shots = target == null || !_comboOn ? 1 : StoppedBullet(u, target);
                _shotsAtOnce = shots;
                _timeShot = shots > 1;
                // Two shots per aim at rank 2 (rules 24, BattleSimulation.Whisperer.cs).
                if (target != null && shots > 0) Shots(u, target);
                _shotsAtOnce = 1;
                _timeShot = false;
                _currentChain = saved;
                // A Steady shot goes through to the enemy behind its mark (the road kit).
                if (u.Steady && shots > 0) Pierce(u, target);
                CheckWipe();
            }
        }

        // A blow on an aiming Marksman loses his shot, unless his piece keeps his aim (Quickdraw). He can aim again
        // AimRetryTicks later (Jacob, 20 Oct 2026: "2 seconds after a break").
        private void BreakAim(BattleUnit marksman, BattleUnit source)
        {
            if (HasBend(marksman, ClassBend.Quickdraw)) return;
            // A Steady shot can't be broken (the road kit, BattleSimulation.Boosts.cs).
            if (marksman.Steady) return;
            marksman.AimTicks = 0;
            marksman.AimTargetId = string.Empty;
            marksman.SpellTicks = ClassSpells.AimRetryTicks;
            Emit(new BattleEvent(_tick, BattleEventKind.Broken, 1 - marksman.Side, source == null ? string.Empty : source.Id,
                source == null ? -1 : (int)source.Class, marksman.Id, (int)marksman.Class, 0, ClassSpells.AimDetail));
        }

        // ------------------------------------------------------------ Skirmisher: Strike

        // How long until her next strike: half as long below half health with Bloodrush.
        private int StrikeEvery(BattleUnit unit)
        {
            // Heavy, a Gravity Well combo's pact (BattleSimulation.Combos.cs).
            int ticks = Heavy(unit, ClassSpells.StrikeEveryTicks);
            if (HasBend(unit, ClassBend.Bloodrush) && unit.HpPermille < ClassSpells.BloodrushBelowPermille)
                ticks = ticks * ClassSpells.BloodrushEveryPercent / 100;
            // The faster hook (rules 23, BattleSimulation.Shade.cs).
            return Clock(unit, ticks);
        }

        // Her strike, at an Opening or a bleeding wound first. False when she can strike nobody right now.
        private bool TryStrike(BattleUnit unit)
        {
            BattleUnit target = SpellTarget(unit, false);
            if (target == null) return false;
            unit.SpellTicks = StrikeEvery(unit);
            unit.FreeSpent = false;
            // Twice at rank 2, and an upgraded Breaker's restart (rules 24, BattleSimulation.Whisperer.cs).
            Strikes(unit, target);
            return true;
        }

        // ------------------------------------------------------------ Trapper: Barbed Snare

        // Whoever sprang a barbed snare is held and bleeds. side is the trap's side: its Trapper may have fallen.
        private void Snare(BattleUnit trapper, int side, BattleUnit victim)
        {
            // Time Lock (rules 26, BattleSimulation.Glutton.cs) holds it longer.
            int hold = Locked(side, ClassSpells.SnareTicks);
            if (trapper != null && HasBend(trapper, ClassBend.LongSnare)) hold = hold * ClassSpells.LongSnarePercent / 100;
            // A Trapper with a road scar holds a beast longer (BeastKits, 21 Oct 2026).
            if (trapper != null && IsPart(victim) && SpecOf(trapper).BeastScars > 0) hold = hold * BeastKits.ScarPercent / 100;
            int bleed = ClassSpells.SnareBleedPercent;
            if (trapper != null && HasBend(trapper, ClassBend.Barbs)) bleed = bleed * ClassSpells.BarbsPercent / 100;
            // A chief whose band still stands shrugs the hold off; the barbs still cut (ChiefFight).
            if (!ShrugsMarks(victim))
            {
                victim.Statuses.Apply(StatusKind.Snare, hold);
                MarkEvent(side, trapper, victim, hold, ClassSpells.SnareMark);
                // Singularity Net and Time Snare ride on the hold (BattleSimulation.Combos.cs).
                if (_comboOn && trapper != null) SnareCombos(trapper, victim, hold);
            }
            // Netter (rules 30, BattleSimulation.Vows.cs): her trap catches more.
            if (_vowsOn) Netted(trapper, side, victim);
            // The Stoneveil chief's band's barbs cut no more once your Clean Hands broke his rite, and his Red Rite makes your
            // wounds bleed twice as long (BattleSimulation.Stoneveil.cs).
            if (_riteOn && !RiteWounds(side)) return;
            int wound = _riteOn ? WoundTicks(victim) : BleedRules.Ticks;
            // Time Snare's hook: the wound bleeds as long as the slow (BattleSimulation.Combos.cs).
            if (Wears(trapper, ClassBend.TimeSnare)) wound = Math.Max(wound, victim.SlowTicks);
            victim.Statuses.Apply(StatusKind.Bleed, wound, bleed);
            MarkEvent(side, trapper, victim, wound, ClassSpells.BleedMark);
        }

        // ------------------------------------------------------------ Medic: Treatment

        // She treats one ally: half again as much while a Warden's Guard covers her (Covered Treatment), and Field Kit
        // treats the next most hurt too. How much her patient got.
        private int Treat(BattleUnit medic, BattleUnit patient)
        {
            ClassBend bend = BendOf(medic);
            int amount = _layersOn ? TreatmentHit(medic, patient, bend) : Treatment(medic, patient, bend);
            // Mercy Well pulls spilt blood back into the allies beside her patient (BattleSimulation.Combos.cs).
            if (HasBend(medic, ClassBend.MercyWell) && !Stopped) MercyWell(medic, patient, amount);
            if (!HasBend(medic, ClassBend.FieldKit)) return amount;
            BattleUnit next = null;
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (!u.Alive || u.Side != medic.Side || ReferenceEquals(u, patient) || u.HpPermille >= MedicHealsBelowPermille) continue;
                if (_beastOn && IsHeld(u)) continue; // in the cage bear's claw (BattleSimulation.Beast.cs)
                if (next == null || u.HpPermille < next.HpPermille) next = u;
            }
            if (next != null) Heal(medic, next, Math.Max(1, medic.Power * ClassSpells.FieldKitPercent / 100), ClassSpells.TreatDetail);
            return amount;
        }

        // Her treatment of the one she treats before rules 17, each step rounding on its own; how much it was.
        private int Treatment(BattleUnit medic, BattleUnit patient, ClassBend bend)
        {
            int amount = medic.Power;
            if (HasBend(medic, ClassBend.Surgeon)) amount = amount * ClassSpells.SurgeonPercent / 100;
            // A Medic with a road scar treats more in a beast fight (BeastKits, 21 Oct 2026).
            if (_beastOn && SpecOf(medic).BeastScars > 0) amount = amount * BeastKits.ScarPercent / 100;
            // An upgraded Triage's saved treatment lands twice as big (rules 24, BattleSimulation.Whisperer.cs).
            if (HabitUp(medic, WhispererHabit.Triage)) amount = amount * WhisperRules.TriageUpPercent / 100;
            if (GuardingWarden(medic) != null)
            {
                int more = amount * (ClassSpells.GuardedTreatPercent + SynthBonus(medic)) / 100 - amount;
                Emit(EventFor(BattleEventKind.Cash, medic, patient, more, ClassSpells.CoveredDetail));
                amount += more;
            }
            // Mercy Well's hook: every bleeding enemy feeds her treatment, the half heals beside with it (BattleSimulation.Combos.cs).
            if (HasBend(medic, ClassBend.MercyWell)) amount = MercyBlood(medic, patient, amount);
            Heal(medic, patient, amount, ClassSpells.TreatDetail);
            return amount;
        }

        // The same treatment through the damage layers (BattleSimulation.Layers.cs): Surgeon and a road scar in the increased
        // pile, Covered Treatment in the spell's own layer, Mercy Well's bleeding enemies in the combo layer; how much it was.
        private int TreatmentHit(BattleUnit medic, BattleUnit patient, ClassBend bend)
        {
            var heal = new Hit(medic.Power);
            if (HasBend(medic, ClassBend.Surgeon)) heal.Add(HitLayer.Increased, ClassSpells.SurgeonPercent - 100);
            if (_beastOn && SpecOf(medic).BeastScars > 0) heal.Add(HitLayer.Increased, BeastKits.ScarPercent - 100);
            // An upgraded Triage's, in the status layer, which no other heal pays into, so it doubles the whole treatment (rules 24).
            if (HabitUp(medic, WhispererHabit.Triage)) heal.Add(HitLayer.Status, WhisperRules.TriageUpPercent - 100);
            if (GuardingWarden(medic) != null)
            {
                int before = heal.Points;
                // Synth Armour (rules 26) makes it bigger.
                heal.Add(HitLayer.Spell, ClassSpells.GuardedTreatPercent - 100 + SynthBonus(medic));
                Emit(EventFor(BattleEventKind.Cash, medic, patient, heal.Points - before, ClassSpells.CoveredDetail));
            }
            if (HasBend(medic, ClassBend.MercyWell)) MercyBloodHit(medic, patient, heal);
            int amount = heal.Points;
            HealHit(medic, patient, heal, ClassSpells.TreatDetail);
            return amount;
        }

        // ------------------------------------------------------------ Warden: Guard

        // The Medic a Warden covers so she treats under his Guard: his side's first one standing, or null. Not one in the cage
        // bear's claw (BattleSimulation.Beast.cs).
        private BattleUnit CoveredMedic(BattleUnit warden)
        {
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (u.Alive && u.Side == warden.Side && u.Class == UnitClass.Medic && !(_beastOn && IsHeld(u))) return u;
            }
            return null;
        }

        // How much his Guard takes off a blow: Bastion takes more.
        private int GuardCut(BattleUnit warden)
        {
            return HasBend(warden, ClassBend.Bastion) ? ClassSpells.BastionGuardPercent : SliceClasses.GuardReductionPercent;
        }

        // Retaliator: an enemy whose blow lands on his Guard is left open.
        private void Retaliate(BattleUnit warden, BattleUnit attacker)
        {
            if (_finished || !attacker.Alive || !HasBend(warden, ClassBend.Retaliator)) return;
            Open(warden, attacker);
        }

        // ------------------------------------------------------------ spell blows and marks

        // A spell's own weight on its blow.
        private int SpellWeight(BattleUnit attacker, BlowKind kind, int damage)
        {
            if (kind == BlowKind.Strike) return damage * ClassSpells.StrikePercent / 100;
            int percent = ClassSpells.AimedShotPercent;
            if (HasBend(attacker, ClassBend.Deadeye)) percent = percent * ClassSpells.DeadeyePercent / 100;
            else if (HasBend(attacker, ClassBend.Quickdraw)) percent = percent * ClassSpells.QuickdrawShotPercent / 100;
            return damage * percent / 100;
        }

        // The same weight through the damage layers: the spell's in its own layer, Deadeye's and Quickdraw's (class pieces) in
        // the increased pile.
        private void SpellHit(BattleUnit attacker, BlowKind kind, Hit hit)
        {
            if (kind == BlowKind.Strike)
            {
                hit.Add(HitLayer.Spell, ClassSpells.StrikePercent - 100);
                return;
            }
            hit.Add(HitLayer.Spell, ClassSpells.AimedShotPercent - 100);
            if (HasBend(attacker, ClassBend.Deadeye)) hit.Add(HitLayer.Increased, ClassSpells.DeadeyePercent - 100);
            else if (HasBend(attacker, ClassBend.Quickdraw)) hit.Add(HitLayer.Increased, ClassSpells.QuickdrawShotPercent - 100);
        }

        // The mark a spell's blow cashes in on whoever it lands on: a snare makes the Aimed Shot crit; an Opening makes
        // the strike hit twice as hard (three times with Opportunist) and closes. detail names the hit.
        private int CashIn(BattleUnit attacker, BattleUnit target, BlowKind kind, int damage, out string detail)
        {
            if (kind == BlowKind.AimedShot)
            {
                detail = ClassSpells.AimedDetail;
                if (target.SnareTicks <= 0) return damage;
                detail = ClassSpells.AimedPinDetail;
                // Around the Corner's hook: from behind a snared enemy takes triple (BattleSimulation.Combos.cs).
                int crit = damage * (ThroughPortal(attacker, kind) ? ClassSpells.CornerSnarePercent : ClassSpells.PinCritPercent) / 100;
                Emit(EventFor(BattleEventKind.Cash, attacker, target, crit - damage, ClassSpells.PinDetail));
                return crit;
            }
            detail = ClassSpells.StrikeDetail;
            if (target.OpeningTicks <= 0) return damage;
            detail = ClassSpells.StrikeOpenDetail;
            int percent = HasBend(attacker, ClassBend.Opportunist) ? ClassSpells.OpportunistPercent : ClassSpells.OpeningPercent;
            int opened = damage * percent / 100;
            target.Statuses.Clear(StatusKind.Opening);
            Emit(EventFor(BattleEventKind.Cash, attacker, target, opened - damage, ClassSpells.OpeningMark));
            return opened;
        }

        // The same cash through the damage layers: the snare's crit or the Opening in the cash layer, each Cash event showing in
        // whole points what it added.
        private void CashInHit(BattleUnit attacker, BattleUnit target, BlowKind kind, Hit hit, out string detail)
        {
            int before = hit.Points;
            if (kind == BlowKind.AimedShot)
            {
                detail = ClassSpells.AimedDetail;
                if (target.SnareTicks <= 0) return;
                detail = ClassSpells.AimedPinDetail;
                // Around the Corner's hook: from behind a snared enemy takes triple (BattleSimulation.Combos.cs).
                hit.Add(HitLayer.Cash, (ThroughPortal(attacker, kind) ? ClassSpells.CornerSnarePercent : ClassSpells.PinCritPercent) - 100);
                Emit(EventFor(BattleEventKind.Cash, attacker, target, hit.Points - before, ClassSpells.PinDetail));
                return;
            }
            detail = ClassSpells.StrikeDetail;
            if (target.OpeningTicks <= 0) return;
            detail = ClassSpells.StrikeOpenDetail;
            hit.Add(HitLayer.Cash, (HasBend(attacker, ClassBend.Opportunist) ? ClassSpells.OpportunistPercent : ClassSpells.OpeningPercent) - 100);
            target.Statuses.Clear(StatusKind.Opening);
            Emit(EventFor(BattleEventKind.Cash, attacker, target, hit.Points - before, ClassSpells.OpeningMark));
        }

        // After a spell's blow lands: the Aimed Shot leaves an Opening; the strike rips a bleeding wound open, so all the
        // bleeding it had left lands at once. Under the squad first the mark it cashed in (detail, from CashIn) and the rip
        // each earn and crack (MarkCashed).
        private void AfterSpellBlow(BattleUnit attacker, BattleUnit target, BlowKind kind, string detail)
        {
            // Under road combat each mark pays once and counts toward a bond (BattleSimulation.Chains.cs).
            if (_roadCombatOn)
            {
                if (detail == ClassSpells.AimedPinDetail) RoadCash(attacker, target, ClassSpells.SnareMark);
                else if (detail == ClassSpells.StrikeOpenDetail) RoadCash(attacker, target, ClassSpells.OpeningMark);
            }
            else if (_squadOn && (detail == ClassSpells.AimedPinDetail || detail == ClassSpells.StrikeOpenDetail)) MarkCashed(attacker, target);
            // The Butcher's combos that ride on a landed spell, even one that killed its mark (BattleSimulation.Combos.cs).
            if (_comboOn) SpellCombos(attacker, target, kind, detail);
            if (_finished || !target.Alive) return;
            if (kind == BlowKind.AimedShot)
            {
                // Under the road kit an Aimed Shot flares a burning enemy's burn (BattleSimulation.Burn.cs).
                if (_roadKitOn) Flare(attacker, target);
                if (_finished || !target.Alive) return;
                Open(attacker, target);
                return;
            }
            if (target.BleedTicks <= 0) return;
            int left = (target.BleedTicks + SimClock.TicksPerSecond - 1) / SimClock.TicksPerSecond;
            // Through the damage layers all the bleeding left, exactly (BattleSimulation.Layers.cs).
            // From rules 20 a big target's share counts at most PayoffRules.PercentHealthCap (BattleSimulation.Payoffs.cs).
            Hit ripped = _layersOn ? Hit.Percent((long)left * ShareHealth(target), target.BleedPercent) : null;
            int rip = ripped != null ? ripped.Points : left * Math.Max(1, ShareHealth(target) * target.BleedPercent / 100);
            target.Statuses.Clear(StatusKind.Bleed);
            Emit(EventFor(BattleEventKind.Cash, attacker, target, rip, ClassSpells.RipDetail));
            if (ripped != null) ApplyHit(attacker, attacker.Side, target, ripped, ClassSpells.RipDetail);
            else ApplyDamage(attacker, attacker.Side, target, rip, ClassSpells.RipDetail);
            if (_roadCombatOn) RoadCash(attacker, target, ClassSpells.BleedMark);
            else MarkCashed(attacker, target);
        }

        private void Open(BattleUnit by, BattleUnit victim)
        {
            // A chief whose band still stands shrugs it off (ChiefFight).
            if (ShrugsMarks(victim)) return;
            // An upgraded Spotter's stays until it is cashed (rules 24, BattleSimulation.Whisperer.cs).
            // Strip (rules 26) makes it last longer.
            int ticks = Stripped(by.Side, _whisperOn ? OpeningTicksBy(by) : ClassSpells.OpeningTicks);
            victim.Statuses.Apply(StatusKind.Opening, ticks);
            MarkEvent(by.Side, by, victim, ticks, ClassSpells.OpeningMark);
        }

        private void MarkEvent(int side, BattleUnit by, BattleUnit on, int ticks, string mark)
        {
            Emit(new BattleEvent(_tick, BattleEventKind.Mark, side, by == null ? string.Empty : by.Id, by == null ? -1 : (int)by.Class,
                on.Id, (int)on.Class, ticks, mark));
            // Under road combat the mark waits for its casher's reaction (BattleSimulation.Chains.cs).
            if (_roadCombatOn) MarkSet(side, by, on, mark);
        }

        // Snares and Openings wear off.
        private void CountDownMarks()
        {
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                u.Statuses.CountDown(StatusKind.Snare);
                u.Statuses.CountDown(StatusKind.Opening);
            }
        }

        // With class spells a fighter held by a snare or aiming takes no heavy swing, and the Medic never does: she never
        // attacks.
        private bool CanSwingHeavy(BattleUnit u)
        {
            return !_classOn || (u.SnareTicks <= 0 && u.AimTicks <= 0 && u.PinTicks <= 0 && u.Class != UnitClass.Medic);
        }
    }
}
