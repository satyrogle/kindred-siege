using System;

namespace KindredSiege.Sim.Battle
{
    public sealed partial class BattleSimulation
    {
        // The damage layers (Hits.cs; DESIGN_LOCKS, "Builds: one theme per Pal", step 2; FightRules.Layers, campaign rules 17):
        // every hit and heal goes through one pipeline, rounded once, the leftover kept on the fighter for the next.
        private readonly bool _layersOn;

        /// Whether this fight plays the damage layers (campaign rules 17).
        public bool LayersOn => _layersOn;

        // A hit through the pipeline, as ApplyDamage takes one: the victim's side of it (a wall, the cage bear's ribcage, an
        // Eclipse that wore a chief out, a Turned Hunt, gear against big hits) as layers, then rounded once with his leftover.
        private void ApplyHit(BattleUnit source, int sourceSide, BattleUnit victim, Hit hit, string detail, bool leaveOne = false)
        {
            if (_finished || !victim.Alive || hit.Base <= 0) return;
            if (LeaderProtected(victim)) return;
            if (_halted) return;
            if (_comboOn && (victim.EvacTicks > 0 || FlurryKeeps(victim, sourceSide, hit.Points, detail))) return;

            bool foe = sourceSide == 1 - victim.Side;
            // From rules 22 held parry bars make your payoffs stronger (BattleSimulation.Meter.cs).
            if (foe) MeterBoost(sourceSide, hit);
            // From rules 26 a side that whiffed a parry takes more for a second (BattleSimulation.Tight.cs).
            if (_tightOn) WhiffHit(victim, foe, hit);
            // From rules 29 Dark Matter Plating and Deep Node (BattleSimulation.Glutton.cs).
            if (foe && _gluttonOn) GluttonHit(sourceSide, victim, hit, detail);
            // From rules 31 the sized picks (BattleSimulation.Sized.cs): her trick at its route's form, a ranked fighter's hit.
            if (foe && _sizedOn)
            {
                int form = ShadeRules.IsTrickDetail(detail) ? ShadeSized(sourceSide, RouteOfTrick(detail)) : 0;
                if (form > 0) hit.Add(HitLayer.Pal, form);
                int rank = source != null && source.Side == sourceSide ? RankSized(source) : 0;
                if (rank > 0) hit.Add(HitLayer.Pal, rank);
            }
            // From rules 32 a combo's hit grows with its flavour (BattleSimulation.Flavours.cs).
            if (foe && _flavoursOn)
            {
                int flavour = FlavourHitPercent(source, detail);
                if (flavour > 0) hit.Add(HitLayer.Pal, flavour);
            }
            // From rules 34 Endless Unleash's stacks, and Executioner's catch on the star (BattleSimulation.DuoMoves.cs).
            if (foe && _duoMovesOn)
            {
                int endless = EndlessPercent(sourceSide);
                if (endless > 0) hit.Add(HitLayer.Pal, endless);
                int taken = ExecutionerTaken(victim);
                if (taken > 0) hit.Add(HitLayer.Guard, taken);
            }
            // From rules 30 Gorged Tricks on one of her tricks (BattleSimulation.Duos.cs).
            if (foe && _duosOn && ShadeRules.IsTrickDetail(detail))
            {
                int gorged = GorgedTrickPercent(sourceSide);
                if (gorged > 0) hit.Add(HitLayer.Kit, gorged);
            }
            if (_spellsOn && _sides[victim.Side].WallTicks > 0 && foe) hit.Add(HitLayer.Guard, WallPercentOf(victim.Side) - 100);
            // Breach's price (rules 28): a side that breaches takes more while it is exposed (BattleSimulation.Shade.cs).
            if (_shadeOn && foe && _sides[victim.Side].ExposedTicks > 0) hit.Add(HitLayer.Guard, ShadeRules.ExposedPercent * PriceLeft(victim.Side) / 100);
            else if (_kitsTwoOn && foe && LockdownHolds(victim.Side)) hit.Add(HitLayer.Guard, ChiefSpells.WallDamagePercent - 100);
            if (_beastOn && victim.Side == _beastSide && foe && !CageHit(victim, sourceSide, hit)) return;
            if (victim.Role == UnitRole.Chief && WornOutTicksLeft(victim.Side) > 0) hit.Add(HitLayer.Kit, Moves.WornOutDamagePercent - 100);
            if (_huntOn && Turned(victim) && foe) hit.Add(HitLayer.Kit, ChiefKits.TurnedPercent - 100);
            int guard = SpecOf(victim).BigHitArmorPercent;
            if (guard > 0 && detail == Moves.EclipseDetail) hit.Add(HitLayer.Armor, -guard);

            long left = victim.HitLeftover;
            int amount = hit.Resolve(ref left);
            victim.HitLeftover = left;
            if (leaveOne) amount = Math.Min(amount, victim.Hp - 1);
            // A shield soaks what it can first (rules 23, BattleSimulation.Shade.cs).
            int unshielded = amount;
            amount = Shielded(victim, amount);
            if (amount <= 0 && unshielded > 0) return;
            if (amount <= 0) return;
            // Last Refuge, Sanctuary's last form (rules 28): once a fight a killing blow under the field leaves him at 1 health.
            if (_shadeOn) amount = LastRefuge(victim, amount);
            if (amount <= 0) return;
            // Who hit one of yours, for Avenger and Counter Shot (rules 24, BattleSimulation.Whisperer.cs).
            if (_whisperOn && foe && source != null && source.Side == sourceSide) Struck(source, victim);
            Land(source, sourceSide, victim, amount, detail);
        }

        // A heal through the pipeline, rounded once with the patient's own leftover.
        private void HealHit(BattleUnit healer, BattleUnit patient, Hit heal, string detail)
        {
            // Facing the Stoneveil chief your Medic's treatment plucks his Leeches off or stitches a wound shut, healed or not
            // (BattleSimulation.Stoneveil.cs).
            if (_riteOn)
            {
                RiteTreated(healer, patient);
                if (Stopped) return;
            }
            // From rules 29 Matter Printer and Recompile (BattleSimulation.Glutton.cs).
            if (_gluttonOn) GluttonHeal(healer, patient, heal);
            // From rules 33 a Martyr heals more the less health she has (BattleSimulation.Vows.cs).
            if (_vowsOn && healer != null)
            {
                int martyr = MartyrPercent(healer);
                if (martyr > 0) heal.Add(HitLayer.Pal, martyr);
            }
            // From rules 31 a ranked healer heals more (BattleSimulation.Sized.cs).
            if (_sizedOn && healer != null)
            {
                int rank = RankSized(healer);
                if (rank > 0) heal.Add(HitLayer.Pal, rank);
            }
            long left = patient.HealLeftover;
            int amount = heal.ResolveHeal(ref left);
            patient.HealLeftover = left;
            Restore(healer, patient, amount, detail);
        }

        // A blow's own bonuses as layers, as Bonuses takes them one rounding after another before rules 17. Its base is the
        // attacker's power and the roll, never under a point.
        private Hit BlowHit(BattleUnit attacker, BattleUnit target, int power, int roll, BlowKind kind)
        {
            Hit hit = Hit.Percent(power, roll);
            if (hit.Base < Hit.Unit) hit = new Hit(1);
            SideState mine = _sides[attacker.Side];
            SideState theirs = _sides[target.Side];
            if (mine.UnleashTicks > 0) hit.Add(HitLayer.Orders, UnleashPercent(attacker.Side) - 100);
            if (mine.FrenzyTicks > 0)
                hit.Add(HitLayer.Orders, (attacker.Role == UnitRole.Pal ? Moves.FrenzyPalPowerPercent : Moves.FrenzyAllyPowerPercent) - 100);
            if (mine.RallyTicks > 0) hit.Add(HitLayer.Orders, RallyDamagePercent - 100);
            if (attacker.RiseTicks > 0) hit.Add(HitLayer.Orders, FightRules.RiseDamagePercent - 100);
            if (theirs.HoldTicks > 0 && target.Row == Row.Front) hit.Add(HitLayer.Guard, HoldSoak(target.Side) - 100);
            if (_bandOn) BandHit(attacker, target, hit);
            if (_floodOn) FloodHit(attacker.Side, hit);
            if (_huntOn) DriveHit(attacker, target, hit);
            if (_riteOn) DrinkHit(attacker, hit);
            int wounded = SpecOf(attacker).WoundedBonusPercent;
            if (wounded > 0 && target.HpPermille < WoundedBelowPermille) hit.Add(HitLayer.Increased, wounded);
            if (kind != BlowKind.Auto) SpellHit(attacker, kind, hit);
            if (kind == BlowKind.AimedShot && _shotsAtOnce > 1) hit.Times(_shotsAtOnce);
            if (_beastOn && kind != BlowKind.Auto && IsPart(target) && SpecOf(attacker).BeastScars > 0)
                hit.Add(HitLayer.Increased, BeastKits.ScarPercent - 100);
            // From rules 29 the strikers' and shooters' talents (BattleSimulation.Glutton.cs).
            if (_gluttonOn) GluttonBlow(attacker, target, hit);
            // From rules 34 (BattleSimulation.DuoMoves.cs): Chain Reaction's smaller plain hits, Shadow Twin's shadow, a gorged star.
            if (_duoMovesOn && attacker.Side >= 0 && attacker.Side <= 1 && target.Side != attacker.Side)
            {
                if (HasMove(attacker.Side, PalDuo.ChainReaction)) hit.Add(HitLayer.Pal, -DuoRules.ChainCutPercent);
                int shadow = ShadowCut(attacker);
                if (shadow != 0) hit.Add(HitLayer.Pal, shadow);
                if (attacker.GorgePercent > 0) hit.Add(HitLayer.Pal, attacker.GorgePercent);
            }
            return hit;
        }

        // A Warden takes the blow for his ward: his Guard's cut and his armour on the hit, the Guard event showing in whole
        // points what the cut kept off.
        private void GuardedHit(BattleUnit attacker, BattleUnit target, BattleUnit warden, Hit hit, int cut)
        {
            int before = hit.Points;
            hit.Add(HitLayer.Guard, -cut);
            Emit(EventFor(BattleEventKind.Guard, warden, target, before - hit.Points, string.Empty));
            ApplyHit(attacker, attacker.Side, warden, Armor(warden, hit), GuardedDetail);
        }

        // A unit's armour as a cut on the hit (Armored, before rules 17): thinner when Cracked (rules 28), and against a Marksman
        // with Dark Matter Rounds (rules 29).
        private Hit Armor(BattleUnit victim, Hit hit, BattleUnit attacker = null)
        {
            int armor = ArmorAgainst(attacker, Cracked(victim, SpecOf(victim).ArmorPercent));
            return armor > 0 ? hit.Add(HitLayer.Armor, -armor) : hit;
        }

        // One rounding step before rules 17 or one layer from them: the damage taken this percent, or the percent on its layer.
        private static int More(int damage, Hit hit, HitLayer layer, int percent)
        {
            if (hit == null) return damage * percent / 100;
            hit.Add(layer, percent - 100);
            return damage;
        }
    }
}
