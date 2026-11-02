using System;
using System.Collections.Generic;

namespace KindredSiege.Sim.Battle
{
    /// The Butcher's 15 combos (Jacob, 29 Oct 2026: "Go, Black Hole Round", then "Yes, go" to the other 14; DESIGN_LOCKS, "The
    /// four Pals' verbs and costs", "The four Pals audited and fixed" and "Four Pal fixes from the model"; ClassSpells holds
    /// the rules and numbers). A combo is a fighter's own move made bigger by one augment, worn as his piece's bend: the
    /// Gravity Well's pulls (a Pull event), the Fold Door's portals (Portal) and the Time Stop's freezes (Freeze), each with its
    /// augment's pact. Only a fight where someone wears one plays any of this (_comboOn), so every other fight plays and
    /// checksums exactly as before. No random numbers are drawn here beyond a blow's usual roll.
    public sealed partial class BattleSimulation
    {
        private bool _comboOn;

        // How many Aimed Shots the blow being dealt carries: Stopped Bullets' third carries all three. While it is dealt, time
        // stopped around it: it cannot miss or be guarded.
        private int _shotsAtOnce = 1;
        private bool _timeShot;

        // True when someone in this fight wears a combo of his own class.
        private bool AnyCombo()
        {
            for (int i = 0; i < _units.Count; i++)
                if (ClassSpells.IsCombo(BendOf(_units[i])) || ClassSpells.IsCombo(Bend2Of(_units[i]))) return true;
            return false;
        }

        private bool Wears(BattleUnit unit, ClassBend combo)
        {
            return _comboOn && unit != null && HasBend(unit, combo);
        }

        // Whether one of this fighter's combos is made with this augment, so he keeps its pact (from rules 23 he can carry two).
        private bool HasAugment(BattleUnit unit, Augment augment)
        {
            return _comboOn && (ClassSpells.AugmentOf(BendOf(unit)) == augment || ClassSpells.AugmentOf(Bend2Of(unit)) == augment);
        }

        // ------------------------------------------------------------ the pacts

        // Heavy, the Gravity Well's pact: his spell's own clock runs a quarter longer.
        private int Heavy(BattleUnit unit, int ticks)
        {
            return HasAugment(unit, Augment.GravityWell) ? ticks * HeavyPactPercent(unit) / 100 : ticks;
        }

        // The Warden's Guard and the Medic's treatment come every turn, so for them Heavy slows the turn.
        private int HeavyTurn(BattleUnit unit, int ticks)
        {
            return unit.Class == UnitClass.Warden || unit.Class == UnitClass.Medic ? Heavy(unit, ticks) : ticks;
        }

        // How long between a Marksman's aims.
        private int AimEvery(BattleUnit unit)
        {
            // The faster hook (rules 23, BattleSimulation.Shade.cs).
            return Clock(unit, Heavy(unit, ClassSpells.AimEveryTicks));
        }

        // How many of her actions a Trapper's trap takes to rearm: Heavy rounds a quarter more up.
        private int RearmActions(BattleUnit unit)
        {
            int actions = SliceClasses.TrapRearmActions;
            return HasAugment(unit, Augment.GravityWell) ? (actions * HeavyPactPercent(unit) + 99) / 100 : actions;
        }

        // Borrowed time, the Time Stop's pact: an eighth less health, before the sides count their health.
        private void BorrowedTime()
        {
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (!HasAugment(u, Augment.TimeStop)) continue;
                u.MaxHp = Math.Max(1, u.MaxHp * BorrowedPermille(u) / 1000);
                u.Hp = Math.Min(u.Hp, u.MaxHp);
            }
        }

        // Once the nerve has started. Unnerving, the Fold Door's pact: an eighth less of the wearer's own nerve, or, where
        // fighters have no nerve of their own, his side's bar loses an eighth of his share of it. And Rewind's first clock.
        private void StartCombos()
        {
            for (int s = 0; s < 2 && _nerveOn; s++)
            {
                int fighters = 0;
                for (int i = 0; i < _units.Count; i++)
                    if (_units[i].Side == s && !_units[i].Benched && _units[i].Role == UnitRole.Fighter) fighters++;
                int share = fighters > 0 ? _sides[s].Nerve / fighters : 0;
                for (int i = 0; i < _units.Count; i++)
                {
                    BattleUnit u = _units[i];
                    if (u.Side != s || !HasAugment(u, Augment.FoldDoor)) continue;
                    // Synthesis Discount, Fold Shield and Entropy Purge (rules 26) cut it.
                    int percent = UnnervingPercent(u);
                    if (_unitNerveOn) u.UnitNerve -= u.UnitNerve * percent / (100 * ClassSpells.UnnervingEighths);
                    else _sides[s].Nerve = Math.Max(1, _sides[s].Nerve - share * percent / (100 * ClassSpells.UnnervingEighths));
                }
            }
            for (int i = 0; i < _units.Count; i++)
                if (Wears(_units[i], ClassBend.Rewind)) _units[i].ComboTicks = ClassSpells.RewindEveryTicks;
        }

        // ------------------------------------------------------------ what rides on a landed spell

        // After a spell's blow lands on its mark, the combos that ride on it; the mark may have fallen to it.
        private void SpellCombos(BattleUnit attacker, BattleUnit mark, BlowKind kind, string detail)
        {
            // From rules 23 a rank 3 fighter can carry two, so each rides on its own.
            if (kind == BlowKind.AimedShot)
            {
                if (Wears(attacker, ClassBend.BlackHoleRound)) BlackHole(attacker, mark, detail == ClassSpells.AimedPinDetail);
                if (Wears(attacker, ClassBend.AroundTheCorner)) Lay(attacker, mark, ClassBend.AroundTheCorner);
                return;
            }
            if (Wears(attacker, ClassBend.Collapse)) CollapseStruck(attacker, mark);
            if (Wears(attacker, ClassBend.ExecutionCorridor) && detail == ClassSpells.StrikeOpenDetail) Corridor(attacker, mark);
            if (Wears(attacker, ClassBend.FrozenFlurry)) FlurryStruck(attacker, mark);
        }

        // ------------------------------------------------------------ Gravity Well: pulls

        // The enemies beside this one in its row (the audit's "nearby": the same row and the next lane, as the Brand's
        // BrandedAndBeside), standing and not spared.
        private List<BattleUnit> PulledIn(BattleUnit by, BattleUnit mark, int reach = 1)
        {
            // Neutron Mass and Stasis Wind Up (rules 26) reach further.
            // The Butcher's flavours (rules 29): Gravity pulls further.
            if (_flavoursOn) reach += FlavourReachOf(by);
            List<BattleUnit> beside = Beside(mark, reach);
            var pulled = new List<BattleUnit>();
            for (int k = 1; k < beside.Count; k++)
                if (beside[k].Alive && !Spared(by, beside[k]) && beside[k].EvacTicks <= 0) pulled.Add(beside[k]);
            return pulled;
        }

        // A black hole's pin or a freeze: he does nothing and cannot dodge for this long, but carries no Snare mark (the
        // 28 Oct audit). A chief whose band still stands shrugs it off, as he does a snare.
        private void Pin(BattleUnit unit, int ticks)
        {
            if (!unit.Alive || ShrugsMarks(unit)) return;
            unit.Statuses.Apply(StatusKind.Pin, ticks);
        }

        // ------------------------------------------------------------ the hooks (29 Oct 2026)

        // Each combo leaves a mark or cashes one (DESIGN_LOCKS, "The combos' hooks"), so the chains carry them: a mark makes
        // its casher hit at once, pays its point and nerve once, and counts toward a bond.

        // A combo's bleeding wound, as a snare's: ClassSpells.SnareBleedPercent a second for as long as a snare's wound bleeds
        // (twice as long under a Stoneveil chief's Red Rite, none once Clean Hands broke the rite on that side). Bleeds are no
        // mark a chief's band makes him shrug.
        private void Wound(BattleUnit by, BattleUnit victim)
        {
            if (_finished || !victim.Alive || (_riteOn && !RiteWounds(by.Side))) return;
            int ticks = _riteOn ? WoundTicks(victim) : BleedRules.Ticks;
            victim.Statuses.Apply(StatusKind.Bleed, ticks, ClassSpells.SnareBleedPercent, keepLonger: true);
            MarkEvent(by.Side, by, victim, ticks, ClassSpells.BleedMark);
        }

        // Singularity Net's hook: one it drags in is snared as long as whoever sprang her trap, the Snare mark and all, so a
        // Marksman can cash it. Only the barbs on the one who sprang it cut. A chief whose band still stands shrugs it off.
        private void NetSnare(BattleUnit trapper, BattleUnit victim, int hold)
        {
            if (!victim.Alive || ShrugsMarks(victim)) return;
            victim.Statuses.Apply(StatusKind.Snare, hold);
            MarkEvent(trapper.Side, trapper, victim, hold, ClassSpells.SnareMark);
        }

        // Black Hole Round: the hole drags in the enemies beside its mark, and each one it drags in is crushed for
        // BlackHoleCrushPercent of the shot's own weight, less its armour; its hook, a shot that cashed a Snare crushes them with
        // the crit. The Pull says where it opened and how many it took, so the screen can draw the hole with nobody beside it.
        private void BlackHole(BattleUnit marksman, BattleUnit mark, bool snared)
        {
            if (_finished) return;
            List<BattleUnit> pulled = PulledIn(marksman, mark);
            Emit(EventFor(BattleEventKind.Pull, marksman, mark, pulled.Count, ClassSpells.Name(ClassBend.BlackHoleRound)));
            // Through the damage layers the crush is his shot's weight with the combo's and a snare's crit as layers
            // (BattleSimulation.Layers.cs).
            if (_layersOn)
            {
                var crushed = new Hit(marksman.Power);
                SpellHit(marksman, BlowKind.AimedShot, crushed);
                crushed.Add(HitLayer.Combo, StakePercent(marksman) - 100);
                if (snared) crushed.Add(HitLayer.Cash, ClassSpells.PinCritPercent - 100);
                for (int k = 0; k < pulled.Count && !_finished; k++)
                    ApplyHit(marksman, marksman.Side, pulled[k], Armor(pulled[k], crushed.Copy()), ClassSpells.BlackHoleDetail);
            }
            else
            {
                int crush = Math.Max(1, SpellWeight(marksman, BlowKind.AimedShot, marksman.Power) * StakePercent(marksman) / 100);
                if (snared) crush = crush * ClassSpells.PinCritPercent / 100;
                for (int k = 0; k < pulled.Count && !_finished; k++)
                    ApplyDamage(marksman, marksman.Side, pulled[k], Armored(pulled[k], crush), ClassSpells.BlackHoleDetail);
            }
            // Rules 18: everyone it dragged in carries Crush (BattleSimulation.Marks.cs).
            for (int k = 0; k < pulled.Count && !_finished; k++) Lay(marksman, pulled[k], ClassBend.BlackHoleRound);
        }

        // Collapse: every second strike of hers that lands drops a black hole that pins her mark flat; its hook, so does a
        // strike that rips a bleeding wound open (AfterSpellBlow rips it next).
        private void CollapseStruck(BattleUnit skirmisher, BattleUnit mark)
        {
            skirmisher.ComboCount++;
            bool rips = mark.BleedTicks > 0;
            if ((!rips && skirmisher.ComboCount % ClassSpells.CollapseEvery != 0) || _finished || !mark.Alive || ShrugsMarks(mark)) return;
            Emit(EventFor(BattleEventKind.Pull, skirmisher, mark, 0, ClassSpells.Name(ClassBend.Collapse)));
            Pin(mark, FlavourHold(skirmisher, ClassBend.Collapse, ClassSpells.CollapseTicks));
            Lay(skirmisher, mark, ClassBend.Collapse);
        }

        // Event Horizon: every fourth swing his shield bash pulls in the enemies beside his mark, then the cleave hits them
        // all: the mark through his blow, the rest crushed for the same; its hook, everyone it hits is left bleeding. False
        // when it is not that swing, or there is nobody to swing at.
        private bool EventHorizon(BattleUnit warden)
        {
            if (!Wears(warden, ClassBend.EventHorizon)) return false;
            warden.ComboCount++;
            if (warden.ComboCount % ClassSpells.EventHorizonEvery != 0) return false;
            BattleUnit mark = ChooseTarget(warden);
            if (mark == null) return false;
            List<BattleUnit> pulled = PulledIn(warden, mark, Reach(warden, ClassBend.EventHorizon));
            Emit(EventFor(BattleEventKind.Pull, warden, mark, pulled.Count, ClassSpells.Name(ClassBend.EventHorizon)));
            int power = Math.Max(1, warden.Power * ClassSpells.EventHorizonPercent / 100);
            int markHp = mark.Hp;
            // Through the damage layers the cleave's weight is a combo layer on his power (BattleSimulation.Layers.cs).
            if (_layersOn) Blow(warden, mark, warden.Power, BlowKind.Auto, ClassSpells.EventHorizonPercent - 100);
            else Blow(warden, mark, power, BlowKind.Auto);
            if (mark.Hp < markHp) Wound(warden, mark);
            for (int k = 0; k < pulled.Count && !_finished; k++)
            {
                if (_layersOn)
                    ApplyHit(warden, warden.Side, pulled[k], Armor(pulled[k], new Hit(warden.Power).Add(HitLayer.Combo, ClassSpells.EventHorizonPercent - 100)),
                        ClassSpells.EventHorizonDetail);
                else ApplyDamage(warden, warden.Side, pulled[k], Armored(pulled[k], power), ClassSpells.EventHorizonDetail);
                Wound(warden, pulled[k]);
                Lay(warden, pulled[k], ClassBend.EventHorizon);
            }
            return true;
        }

        // A Trapper's combo on the one her trap caught and holds. Singularity Net: the enemies beside it are dragged in and
        // held as long, and its hook snares them too (NetSnare). Time Snare (rethought 29 Oct 2026): for TimeSnareTicks after
        // the hold it moves at half speed and every big move it starts winds up twice as slowly; its hook, the snare's wound
        // bleeds as long (Snare, BattleSimulation.Class.cs).
        private void SnareCombos(BattleUnit trapper, BattleUnit caught, int hold)
        {
            if (Wears(trapper, ClassBend.SingularityNet))
            {
                List<BattleUnit> pulled = PulledIn(trapper, caught, Reach(trapper, ClassBend.SingularityNet));
                Emit(EventFor(BattleEventKind.Pull, trapper, caught, pulled.Count, ClassSpells.Name(ClassBend.SingularityNet)));
                for (int k = 0; k < pulled.Count; k++)
                {
                    NetSnare(trapper, pulled[k], hold);
                    Lay(trapper, pulled[k], ClassBend.SingularityNet);
                }
            }
            else if (Wears(trapper, ClassBend.TimeSnare))
            {
                caught.Statuses.Apply(StatusKind.Slow, hold + FlavourHold(trapper, ClassBend.TimeSnare, ClassSpells.TimeSnareTicks));
                Emit(EventFor(BattleEventKind.Freeze, trapper, caught, caught.SlowTicks, ClassSpells.Name(ClassBend.TimeSnare)));
                Lay(trapper, caught, ClassBend.TimeSnare);
            }
        }

        // Mercy Well: her treatment pulls spilt blood back into the allies beside her patient, each healed for half of it, and
        // what they do not need flows back into the patient (rethought 29 Oct 2026).
        private void MercyWell(BattleUnit medic, BattleUnit patient, int amount)
        {
            List<BattleUnit> beside = BrandedAndBeside(patient);
            var allies = new List<BattleUnit>();
            for (int k = 1; k < beside.Count; k++)
                if (beside[k].Alive && beside[k].EvacTicks <= 0 && !(_beastOn && IsHeld(beside[k]))) allies.Add(beside[k]);
            Emit(EventFor(BattleEventKind.Pull, medic, patient, allies.Count, ClassSpells.Name(ClassBend.MercyWell)));
            int heal = Math.Max(1, amount * ClassSpells.MercyWellPercent / 100);
            int spare = 0;
            for (int k = 0; k < allies.Count && !Stopped; k++)
            {
                spare += heal - Math.Min(heal, Math.Max(0, allies[k].MaxHp - allies[k].Hp));
                Heal(medic, allies[k], heal, ClassSpells.MercyDetail);
            }
            if (spare > 0 && !Stopped && patient.Alive) Heal(medic, patient, spare, ClassSpells.MercyDetail);
        }

        // Mercy Well's hook: every bleeding enemy's spilt blood feeds her treatment, MercyBleedPercent more each, up to
        // MercyBleedingMost of them. She only reads the wounds: none closes, so a strike can still rip it, and she deals no
        // damage. The treatment with it.
        private int MercyBlood(BattleUnit medic, BattleUnit patient, int amount)
        {
            int more = amount * ClassSpells.MercyBleedPercent * BleedingFoes(medic) / 100;
            ReadWounds(medic);
            if (more <= 0) return amount;
            Emit(EventFor(BattleEventKind.Cash, medic, patient, more, ClassSpells.MercyDetail));
            return amount + more;
        }

        // The same through the damage layers (BattleSimulation.Layers.cs): the bleeding enemies in the combo layer.
        private void MercyBloodHit(BattleUnit medic, BattleUnit patient, Hit heal)
        {
            int bleeding = BleedingFoes(medic);
            if (bleeding <= 0) return;
            ReadWounds(medic);
            int before = heal.Points;
            heal.Add(HitLayer.Combo, ClassSpells.MercyBleedPercent * bleeding);
            if (heal.Points > before) Emit(EventFor(BattleEventKind.Cash, medic, patient, heal.Points - before, ClassSpells.MercyDetail));
        }

        // Rules 18: each bleeding enemy her treatment reads, the same ones BleedingFoes counts, carries Crush
        // (BattleSimulation.Marks.cs).
        private void ReadWounds(BattleUnit medic)
        {
            if (!_marksOn) return;
            int read = 0;
            for (int i = 0; i < _units.Count && read < ClassSpells.MercyBleedingMost && !_finished; i++)
            {
                BattleUnit e = _units[i];
                if (!e.Alive || e.Side == medic.Side || e.BleedTicks <= 0) continue;
                read++;
                Lay(medic, e, ClassBend.MercyWell);
            }
        }

        // How many of her enemies bleed, up to MercyBleedingMost.
        private int BleedingFoes(BattleUnit medic)
        {
            int bleeding = 0;
            for (int i = 0; i < _units.Count && bleeding < ClassSpells.MercyBleedingMost; i++)
            {
                BattleUnit e = _units[i];
                if (e.Alive && e.Side != medic.Side && e.BleedTicks > 0) bleeding++;
            }
            return bleeding;
        }

        // ------------------------------------------------------------ Fold Door: portals

        // Around the Corner (rethought 29 Oct 2026): his Aimed Shot comes through a portal from behind, so no Guard can take it.
        private bool ThroughPortal(BattleUnit attacker, BlowKind kind)
        {
            return kind == BlowKind.AimedShot && Wears(attacker, ClassBend.AroundTheCorner);
        }

        // Around the Corner: past your Focus Fire and snared enemies (SpellTarget), his shot goes for their healer, wherever
        // they stand. Null when he wears no such combo or they have no healer he could shoot.
        private BattleUnit AroundTheCorner(BattleUnit marksman)
        {
            if (!Wears(marksman, ClassBend.AroundTheCorner)) return null;
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit e = _units[i];
                if (e.Alive && e.Side != marksman.Side && e.Class == UnitClass.Medic && !LeaderProtected(e) && !Spared(marksman, e) &&
                    e.EvacTicks <= 0) return e;
            }
            return null;
        }

        // Execution Corridor: when her strike cashes an Opening she steps through a portal and hits that enemy again from
        // behind, for her strike's own weight.
        private void Corridor(BattleUnit skirmisher, BattleUnit mark)
        {
            if (_finished || !mark.Alive) return;
            Emit(EventFor(BattleEventKind.Portal, skirmisher, mark, 0, ClassSpells.Name(ClassBend.ExecutionCorridor)));
            // Through the damage layers her strike's weight and the corridor's own as layers (BattleSimulation.Layers.cs).
            if (_layersOn)
            {
                var again = new Hit(skirmisher.Power);
                SpellHit(skirmisher, BlowKind.Strike, again);
                again.Add(HitLayer.Combo, CorridorPercent(skirmisher) - 100);
                ApplyHit(skirmisher, skirmisher.Side, mark, Armor(mark, again), ClassSpells.CorridorDetail);
            }
            else
            {
                int hit = Math.Max(1, SpellWeight(skirmisher, BlowKind.Strike, skirmisher.Power) * CorridorPercent(skirmisher) / 100);
                ApplyDamage(skirmisher, skirmisher.Side, mark, Armored(mark, hit), ClassSpells.CorridorDetail);
            }
            Lay(skirmisher, mark, ClassBend.ExecutionCorridor);
        }

        // Wrong Door (the 28 Oct audit: only hits on the allies next to the one he Guards): the Warden of this fighter's side
        // whose ward stands beside him in his row, so the blow comes through a portal onto his shield; null for none. Nobody
        // covers one the parry at the center leaves uncovered, as GuardingWarden.
        private BattleUnit WrongDoor(BattleUnit target)
        {
            if (_huntOn && Turned(target)) return null;
            if (_parryOn && (target.ReelTicks > 0 || (HasBand(target.Side, BandTrait.LongReach) && _sides[1 - target.Side].FallBackTicks > 0)))
                return null;
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit warden = _units[i];
                if (!warden.Alive || warden.Side != target.Side || ReferenceEquals(warden, target) || warden.EvacTicks > 0 ||
                    !Wears(warden, ClassBend.WrongDoor)) continue;
                BattleUnit ward = FindById(warden.GuardTargetId);
                if (ward == null || !ward.Alive || ReferenceEquals(ward, target)) continue;
                List<BattleUnit> beside = BrandedAndBeside(ward);
                for (int k = 1; k < beside.Count; k++)
                    if (ReferenceEquals(beside[k], target)) return warden;
            }
            return null;
        }

        // Doorway (the 28 Oct audit): a shooter falls through a portal into her armed trap too.
        private bool Doorway(SideState theirs)
        {
            if (!_comboOn || !theirs.TrapArmed) return false;
            BattleUnit trapper = FindById(theirs.TrapOwnerId);
            return trapper != null && Wears(trapper, ClassBend.Doorway);
        }

        // Evac: once a fight a Medic's serpent pulls an ally who would fall through a portal to the back, alive with
        // EvacHealthPermille of his health and out of reach for EvacTicks (the 28 Oct audit). His aim and his Guard are lost.
        // Its hook: the enemy whose blow would have killed him is left with an Opening (a bleed or a burn is no blow). True
        // when it did.
        private bool Evac(BattleUnit victim, BattleUnit source, int sourceSide, string detail)
        {
            if (victim.Role != UnitRole.Fighter) return false;
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit medic = _units[i];
                if (!medic.Alive || medic.Side != victim.Side || ReferenceEquals(medic, victim) || medic.EvacSpent ||
                    !Wears(medic, ClassBend.Evac)) continue;
                medic.EvacSpent = true;
                victim.Hp = Math.Max(1, victim.MaxHp * ClassSpells.EvacHealthPermille / 1000);
                victim.EvacTicks = ClassSpells.EvacTicks;
                victim.AimTicks = 0;
                victim.AimTargetId = string.Empty;
                victim.GuardTargetId = string.Empty;
                Emit(EventFor(BattleEventKind.Portal, medic, victim, ClassSpells.EvacTicks, ClassSpells.Name(ClassBend.Evac)));
                if (source != null && source.Alive && sourceSide == 1 - victim.Side && !NoBlow(detail))
                {
                    Open(medic, source);
                    Lay(medic, source, ClassBend.Evac);
                }
                return true;
            }
            return false;
        }

        // ------------------------------------------------------------ Time Stop: freezes, slows and rewinds

        // Stopped Bullets: his first two Aimed Shots hang frozen in their marks and the third fires all three at once
        // (_timeShot: it cannot miss or be guarded). A bullet that hangs freezes its mark and leaves its Opening (rethought
        // 29 Oct 2026). How many shots this one carries: 0 while it hangs frozen, 1 without the combo, StoppedShots for the one
        // that fires them.
        private int StoppedBullet(BattleUnit marksman, BattleUnit mark)
        {
            if (!Wears(marksman, ClassBend.StoppedBullets)) return 1;
            marksman.ComboCount++;
            if (marksman.ComboCount < ClassSpells.StoppedShots)
            {
                Emit(EventFor(BattleEventKind.Freeze, marksman, mark, marksman.ComboCount, ClassSpells.Name(ClassBend.StoppedBullets)));
                if (!_finished && mark.Alive)
                {
                    Open(marksman, mark);
                    Pin(mark, FlavourHold(marksman, ClassBend.StoppedBullets, FreezeTicks(marksman)));
                    Lay(marksman, mark, ClassBend.StoppedBullets);
                }
                return 0;
            }
            marksman.ComboCount = 0;
            return ClassSpells.StoppedShots;
        }

        // Frozen Flurry: a strike of hers that lands freezes its mark, and every hit on it from her side waits until it
        // thaws, then lands at once. One flurry at a time on a fighter.
        private void FlurryStruck(BattleUnit skirmisher, BattleUnit mark)
        {
            if (_finished || !mark.Alive || mark.FlurryTicks > 0 || ShrugsMarks(mark)) return;
            int flurry = FlavourHold(skirmisher, ClassBend.FrozenFlurry, ClassSpells.FlurryTicks);
            mark.Statuses.Apply(StatusKind.Flurry, flurry, sourceId: skirmisher.Id);
            Pin(mark, flurry);
            Emit(EventFor(BattleEventKind.Freeze, skirmisher, mark, flurry, ClassSpells.Name(ClassBend.FrozenFlurry)));
            Lay(skirmisher, mark, ClassBend.FrozenFlurry);
        }

        // A hit from the other side on a fighter frozen in a flurry waits. True when it was kept.
        private bool FlurryKeeps(BattleUnit victim, int sourceSide, int amount, string detail)
        {
            if (victim.FlurryTicks <= 0 || sourceSide != 1 - victim.Side || detail == ClassSpells.FlurryDetail) return false;
            victim.Statuses[StatusKind.Flurry].Store += amount;
            return true;
        }

        // A flurry thaws: everything it kept lands at once, as hers. Its hook: an Opening on it is cashed by the thaw, and the
        // whole kept hit lands FlurryOpenPercent. Her own strike cannot come again inside the freeze, so the thaw cashes it,
        // paying the Opening's point and nerve and counting toward her bond with whoever left it.
        private void Thaw(BattleUnit victim)
        {
            Status flurry = victim.Statuses[StatusKind.Flurry];
            int store = flurry.Store;
            BattleUnit by = FindById(flurry.SourceId);
            flurry.Store = 0;
            flurry.SourceId = string.Empty;
            if (store <= 0 || !victim.Alive) return;
            // Through the damage layers the kept hits are its base and the Opening's cash a layer (BattleSimulation.Layers.cs).
            Hit kept = _layersOn ? new Hit(store) : null;
            if (by != null && by.Alive && victim.OpeningTicks > 0)
            {
                int opened = kept != null ? kept.Add(HitLayer.Cash, ClassSpells.FlurryOpenPercent - 100).Points : store * ClassSpells.FlurryOpenPercent / 100;
                victim.Statuses.Clear(StatusKind.Opening);
                Emit(EventFor(BattleEventKind.Cash, by, victim, opened - store, ClassSpells.OpeningMark));
                store = opened;
                if (_roadCombatOn) RoadCash(by, victim, ClassSpells.OpeningMark);
                else if (_squadOn) MarkCashed(by, victim);
            }
            if (kept != null) ApplyHit(by, by == null ? 1 - victim.Side : by.Side, victim, kept, ClassSpells.FlurryDetail);
            else ApplyDamage(by, by == null ? 1 - victim.Side : by.Side, victim, store, ClassSpells.FlurryDetail);
        }

        // Still Wall: whoever lands a blow on his shield freezes, at most once every StillWallEveryTicks; its hook, and is
        // left with an Opening.
        private void StillWall(BattleUnit warden, BattleUnit striker)
        {
            if (warden.ComboTicks > 0 || !warden.Alive || !striker.Alive || ShrugsMarks(striker) || !Wears(warden, ClassBend.StillWall))
                return;
            warden.ComboTicks = ClassSpells.StillWallEveryTicks;
            int wall = FlavourHold(warden, ClassBend.StillWall, ClassSpells.StillWallTicks);
            Pin(striker, wall);
            Emit(EventFor(BattleEventKind.Freeze, warden, striker, wall, ClassSpells.Name(ClassBend.StillWall)));
            Open(warden, striker);
            Lay(warden, striker, ClassBend.StillWall);
        }

        // A big move's wind up for this fighter: twice as long while a Time Snare slows him.
        private int Slowed(BattleUnit unit, int ticks)
        {
            if (!_comboOn || unit == null || unit.SlowTicks <= 0) return ticks;
            return ticks * CrawlPercent(unit) / 100;
        }

        // A Time Snare's half speed: on every other tick his turn does not come closer.
        private bool HalfSpeed(BattleUnit unit)
        {
            return _comboOn && unit.SlowTicks > 0 && (_tick & 1L) == 1L;
        }

        // He lost this much health this second: a Rewind can give back the last RewindSeconds of it. An enemy's blow also
        // records who struck him, for Rewind's hook; a bleed or a burn is no blow.
        private void Hurt(BattleUnit victim, int hpLost, BattleUnit source, int sourceSide, string detail)
        {
            if (hpLost <= 0) return;
            if (source != null && sourceSide == 1 - victim.Side && !NoBlow(detail)) victim.HurtById = source.Id;
            long second = _tick / SimClock.TicksPerSecond;
            int slot = (int)(second % ClassSpells.RewindSeconds);
            if (victim.HurtSecond[slot] != second)
            {
                victim.HurtSecond[slot] = second;
                victim.HurtLost[slot] = 0;
            }
            victim.HurtLost[slot] += hpLost;
            // Rewind Wound's longer memory (rules 26, BattleSimulation.Glutton.cs).
            if (_gluttonOn) HurtLonger(victim, hpLost);
        }

        // What a Medic's Rewind gives back: the last RewindSeconds, or with Rewind Wound (rules 26) the longer memory.
        private int RewindLost(BattleUnit medic, BattleUnit unit)
        {
            int seconds = RewindSecondsOf(medic);
            return seconds <= ClassSpells.RewindSeconds ? Lost(unit) : LostOver(unit, seconds);
        }

        // What he lost in the last RewindSeconds seconds, this one counted.
        private int Lost(BattleUnit unit)
        {
            long second = _tick / SimClock.TicksPerSecond;
            int lost = 0;
            for (int k = 0; k < ClassSpells.RewindSeconds; k++)
                if (unit.HurtSecond[k] >= 0 && second - unit.HurtSecond[k] < ClassSpells.RewindSeconds) lost += unit.HurtLost[k];
            return lost;
        }

        // Rewind: her clock came round. The most hurt ally who lost health in the last RewindSeconds gets it back, and her
        // clock starts again. Its hook: whoever last struck that ally is rewound mid swing and left with an Opening.
        private void Rewind(BattleUnit medic)
        {
            medic.ComboTicks = ClassSpells.RewindEveryTicks;
            BattleUnit ally = null;
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (!u.Alive || u.Side != medic.Side || u.Hp >= u.MaxHp || u.EvacTicks > 0 || RewindLost(medic, u) <= 0) continue;
                if (_beastOn && IsHeld(u)) continue; // in the cage bear's claw (BattleSimulation.Beast.cs)
                if (ally == null || u.HpPermille < ally.HpPermille) ally = u;
            }
            if (ally == null) return;
            int back = Math.Min(RewindLost(medic, ally), ally.MaxHp - ally.Hp);
            Emit(EventFor(BattleEventKind.Freeze, medic, ally, back, ClassSpells.Name(ClassBend.Rewind)));
            for (int k = 0; k < ClassSpells.RewindSeconds; k++) ally.HurtLost[k] = 0;
            if (_gluttonOn) for (int k = 0; k < ally.HurtLongLost.Length; k++) ally.HurtLongLost[k] = 0;
            Heal(medic, ally, back, ClassSpells.RewindDetail);
            BattleUnit foe = FindById(ally.HurtById);
            if (!_finished && foe != null && foe.Alive && foe.Side != ally.Side)
            {
                Open(medic, foe);
                Lay(medic, foe, ClassBend.Rewind);
            }
        }

        // ------------------------------------------------------------ the clocks

        // Pins, Evacs and slows wear off, a flurry thaws, Still Wall's wait runs down and a Rewind comes round.
        private void CountDownCombos()
        {
            for (int i = 0; i < _units.Count && !_finished; i++)
            {
                BattleUnit u = _units[i];
                u.Statuses.CountDown(StatusKind.Pin);
                if (u.EvacTicks > 0) u.EvacTicks--;
                u.Statuses.CountDown(StatusKind.Slow);
                if (u.Statuses.CountDown(StatusKind.Flurry)) Thaw(u);
                if (!u.Alive || u.ComboTicks <= 0) continue;
                if (Wears(u, ClassBend.StillWall)) u.ComboTicks--;
                else if (Wears(u, ClassBend.Rewind) && --u.ComboTicks == 0) Rewind(u);
            }
        }
    }
}
