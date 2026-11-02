using System;
using System.Collections.Generic;
using System.Globalization;

namespace KindredSiege.Sim.Battle
{
    /// The floor under the three Pals (Jacob, 31 Oct 2026: "Go, sprint it"; DESIGN_LOCKS, "The floor under the three Pals";
    /// FightRules.Floor, campaign rules 23), the fight's half: a fighter's second piece (UnitSpec.Bend2, read through
    /// HasBend in BattleSimulation.Class.cs), the faster hook on a fighter's clocks (UnitSpec.ClockPercent), the cheaper hook on
    /// a side's order costs (PalKit.OrderCostPercent), and the Shade's three tricks built so far, each route's first form:
    /// Sanctuary's shield, Backstab and Fear (ShadeRules). From campaign rules 28 (FightRules.ShadeForms; Jacob in chat, 31 Oct
    /// 2026: "go" on Claude's Shade design) their second and third forms and each route's price: Sanctuary's Relay and Last
    /// Refuge (its parries do not stagger), Backstab's Twin Knives and Shadow Strike (Fall Back waits longer), and Fear's
    /// Spreading Dread and Turncoat (Focus Fire is shorter). Every number is a starting value.
    public sealed partial class BattleSimulation
    {
        private readonly bool _floorOn;
        private readonly bool _shadeOn;

        /// Whether this fight plays the floor under the three Pals: campaign rules 23.
        public bool FloorOn => _floorOn;

        /// Whether this fight plays the Shade's forms: campaign rules 28.
        public bool ShadeFormsOn => _shadeOn;

        /// The ticks Sanctuary's field still runs on this side, 0 when it is down (rules 28).
        public int FieldTicksLeft(int side) => _shadeOn && side >= 0 && side <= 1 ? _sides[side].FieldTicks : 0;

        /// What Relay's pool can still soak on this side (rules 28).
        public int RelayLeft(int side) => _shadeOn && side >= 0 && side <= 1 ? _sides[side].RelaySize : 0;

        /// Whether this side spent its Last Refuge this fight (rules 28).
        public bool RefugeSpent(int side) => _shadeOn && side >= 0 && side <= 1 && _sides[side].RefugeUsed;

        /// The ticks until this side can Fall Back again, under Backstab's price (rules 28).
        public int FallBackLockLeft(int side) => _shadeOn && side >= 0 && side <= 1 ? _sides[side].FallBackLockTicks : 0;

        /// How many of his next hits still land double from behind (rules 28; before it, 1 while his Backstab is on).
        public int BackstabHitsLeft(BattleUnit unit)
        {
            if (unit == null || !unit.Statuses[StatusKind.Backstab].On) return 0;
            return _shadeOn ? Math.Max(1, unit.Statuses[StatusKind.Backstab].Store) : 1;
        }

        // A route's form on this side: below rules 28 only first forms play, whatever the kit holds.
        private int FormOf(int side, ShadeRoute route)
        {
            int form = KitOf(side).FormOf(route);
            return form > 0 && !_shadeOn ? 1 : form;
        }

        // What this side brings from the Pals' stores; nothing before rules 23.
        private PalKit KitOf(int side)
        {
            return _floorOn ? _rules[side].Pals : PalKit.None;
        }

        // The faster hook: a fighter's clocks (his spell's turn, his aim, his cooldown) pass his ClockPercent, never below a tick.
        private int Clock(BattleUnit unit, int ticks)
        {
            if (!_floorOn || unit == null) return ticks;
            // Entropy Sink (rules 29, BattleSimulation.Glutton.cs) on the same percent.
            int percent = SpecOf(unit).ClockPercent - SinkPercent(unit);
            return percent == 0 ? ticks : Math.Max(1, ticks * (100 + percent) / 100);
        }

        // The cheaper hook: a side's order costs pass its kit's OrderCostPercent.
        private int Cheaper(int side, int cost)
        {
            if (!_floorOn || cost <= 0) return cost;
            int percent = KitOf(side).OrderCostPercent;
            return percent == 0 ? cost : Math.Max(0, cost * (100 + percent) / 100);
        }

        // ------------------------------------------------------------ Sanctuary (Hold)

        // A clean parry by this side: with Sanctuary each of its standing fighters gets a shield.
        private void Sanctuary(int side)
        {
            if (!KitOf(side).Has(ShadeRoute.Sanctuary)) return;
            int shielded = 0;
            long given = 0;
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (!u.Alive || u.Side != side || u.Benched) continue;
                // Branch Anchor (rules 29) thickens it on the front row.
                // Sized (rules 31) at Sanctuary's form.
                int size = Anchored(u, Math.Max(1, ShadeSized(side, ShadeRoute.Sanctuary, u.MaxHp * ShadeRules.ShieldPercent / 100)));
                u.Statuses.Apply(StatusKind.Shield, ShadeRules.ShieldTicks, size);
                shielded++;
                given += size;
            }
            if (shielded > 0) Emit(new BattleEvent(_tick, BattleEventKind.Trick, side, string.Empty, -1, string.Empty, -1, shielded, ShadeRules.SanctuaryDetail));
            // From rules 28 the shields are one field for their length, and from Relay it also makes a pool of a share of them.
            if (!_shadeOn || shielded == 0) return;
            SideState s = _sides[side];
            s.FieldTicks = ShadeRules.ShieldTicks;
            s.RelaySize = FormOf(side, ShadeRoute.Sanctuary) >= 2 ? (int)Math.Max(1, given * ShadeRules.RelayPercent / 100) : 0;
        }

        // Sanctuary's price (rules 28): while it is equipped this side's parries do not stagger.
        private bool SanctuaryPriced(int side)
        {
            return _shadeOn && side >= 0 && side <= 1 && KitOf(side).Has(ShadeRoute.Sanctuary);
        }

        // Relay (rules 28): a hit broke his shield, and the field's pool soaks what it can of the rest. What it soaked.
        private int Relayed(BattleUnit victim, int rest)
        {
            SideState s = _sides[victim.Side];
            if (rest <= 0 || s.RelaySize <= 0 || s.FieldTicks <= 0) return 0;
            int soaked = Math.Min(s.RelaySize, rest);
            s.RelaySize -= soaked;
            Emit(new BattleEvent(_tick, BattleEventKind.Shield, victim.Side, victim.Id, (int)victim.Class, victim.Id, (int)victim.Class, soaked,
                ShadeRules.RelayDetail));
            return soaked;
        }

        // Last Refuge (rules 28): once a fight, a hit that would kill one of the side while its field is up leaves him at 1
        // health, and the field shatters, every shield of it with it. What lands of the hit.
        private int LastRefuge(BattleUnit victim, int amount)
        {
            if (amount < victim.Hp) return amount;
            int side = victim.Side;
            if (side < 0 || side > 1) return amount;
            SideState s = _sides[side];
            if (s.FieldTicks <= 0 || s.RefugeUsed || FormOf(side, ShadeRoute.Sanctuary) < 3) return amount;
            s.RefugeUsed = true;
            s.FieldTicks = 0;
            s.RelaySize = 0;
            for (int i = 0; i < _units.Count; i++) if (_units[i].Side == side) _units[i].Statuses.Clear(StatusKind.Shield);
            int lands = Math.Max(0, victim.Hp - ShadeRules.RefugeHealth);
            Emit(new BattleEvent(_tick, BattleEventKind.Trick, side, string.Empty, -1, victim.Id, (int)victim.Class, amount - lands,
                ShadeRules.RefugeDetail));
            return lands;
        }

        // A shield soaks what it can of a hit before the fighter's health, then breaks. What is left lands.
        private int Shielded(BattleUnit victim, int amount)
        {
            if (!_floorOn || amount <= 0) return amount;
            Status shield = victim.Statuses[StatusKind.Shield];
            if (!shield.On || shield.Size <= 0) return amount;
            int soaked = Math.Min(shield.Size, amount);
            shield.Size -= soaked;
            Emit(new BattleEvent(_tick, BattleEventKind.Shield, victim.Side, victim.Id, (int)victim.Class, victim.Id, (int)victim.Class, soaked,
                ShadeRules.SanctuaryDetail));
            if (shield.Size <= 0)
            {
                victim.Statuses.Clear(StatusKind.Shield);
                // Relay (rules 28): the field's pool soaks what is left.
                if (_shadeOn) soaked += Relayed(victim, amount - soaked);
            }
            return amount - soaked;
        }

        // ------------------------------------------------------------ Backstab (Fall Back)

        // A Fall Back by this side: with Backstab each of its standing fighters steps out behind them, his next hit doubling.
        private void Backstab(int side)
        {
            if (!KitOf(side).Has(ShadeRoute.Backstab)) return;
            // From rules 28 his hits from behind are counted: Twin Knives gives two.
            int hits = FormOf(side, ShadeRoute.Backstab) >= 2 ? ShadeRules.TwinKnivesHits : 1;
            int stepped = 0;
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (!u.Alive || u.Side != side || u.Benched) continue;
                u.Statuses.Apply(StatusKind.Backstab, ShadeRules.BackstabTicks);
                if (_shadeOn) u.Statuses[StatusKind.Backstab].Store = hits;
                stepped++;
            }
            if (stepped > 0) Emit(new BattleEvent(_tick, BattleEventKind.Trick, side, string.Empty, -1, string.Empty, -1, stepped, ShadeRules.BackstabDetail));
        }

        // Whether this blow is a backstab, from behind them. The blow spends it.
        private bool SpendBackstab(BattleUnit attacker, BattleUnit target)
        {
            if (!_floorOn || attacker.Side == target.Side || !attacker.Statuses[StatusKind.Backstab].On) return false;
            Status stab = attacker.Statuses[StatusKind.Backstab];
            // Twin Knives (rules 28): a hit from behind left over stays for his next blow.
            if (_shadeOn && stab.Store > 1)
            {
                stab.Store--;
                return true;
            }
            attacker.Statuses.Clear(StatusKind.Backstab);
            if (_shadeOn) stab.Store = 0;
            return true;
        }

        // Shadow Strike, Backstab's last form (rules 28): this side keeps attacking while it falls back.
        private bool ShadowStrike(int side)
        {
            return _shadeOn && side >= 0 && side <= 1 && FormOf(side, ShadeRoute.Backstab) >= 3;
        }

        // Fear's price (rules 28): with Fear equipped Focus Fire runs a quarter shorter.
        private int FearPriced(int side, int ticks)
        {
            if (!_shadeOn || side < 0 || side > 1 || !KitOf(side).Has(ShadeRoute.Fear)) return ticks;
            // Command Relay (rules 29) shrinks the price.
            return Math.Max(1, ticks * (100 - (100 - ShadeRules.FearFocusPercent) * PriceLeft(side) / 100) / 100);
        }

        // "2.0 s": what a rejection shows for a clock, rounded up to a tenth.
        private static string SecondsLeft(int ticks)
        {
            int tenths = (ticks * 10 + SimClock.TicksPerSecond - 1) / SimClock.TicksPerSecond;
            return (tenths / 10).ToString(CultureInfo.InvariantCulture) + "." + (tenths % 10).ToString(CultureInfo.InvariantCulture) + " s";
        }

        // ------------------------------------------------------------ Fear (Focus Fire)

        // An enemy fell to this side, killed or broken and fleeing (Flee): with Fear, if he was under its Focus Fire, the
        // enemies beside him lose nerve and the one of them left with the least panics.
        private void Fear(int side, BattleUnit fallen)
        {
            if (!KitOf(side).Has(ShadeRoute.Fear)) return;
            SideState mine = _sides[side];
            if (mine.FocusTicks <= 0 || !string.Equals(mine.FocusTargetId, fallen.Id, StringComparison.Ordinal)) return;
            int form = FormOf(side, ShadeRoute.Fear);
            int spreads = form >= 2 ? ShadeRules.DreadSpreads : 0;
            FearAround(side, fallen, form, ShadeRules.FearDetail, ref spreads);
        }

        // Fear around one who fell: those beside him lose nerve and the one of them left with the least panics. From rules 28,
        // Spreading Dread: each of them it breaks (he flees or makes his last stand) sets it off again around him, while spreads
        // last; and Turncoat: the one it panics hits the one beside him once.
        private void FearAround(int side, BattleUnit center, int form, string detail, ref int spreads)
        {
            List<BattleUnit> beside = BrandedAndBeside(center);
            List<BattleUnit> broke = form >= 2 ? new List<BattleUnit>() : null;
            BattleUnit weakest = null;
            int shaken = 0;
            for (int k = 1; k < beside.Count && !_finished; k++)
            {
                BattleUnit u = beside[k];
                if (!u.Alive || u.Side == side) continue;
                shaken++;
                bool snapped = u.NerveSnapped;
                UnitDrain(u, ShadeSized(side, ShadeRoute.Fear, ShadeRules.FearNerve), side);
                if (broke != null && !snapped && u.NerveSnapped) broke.Add(u);
                if (u.Alive && (weakest == null || u.UnitNerve < weakest.UnitNerve)) weakest = u;
            }
            Emit(new BattleEvent(_tick, BattleEventKind.Trick, side, string.Empty, -1, center.Id, (int)center.Class, shaken, detail));
            if (!_finished && weakest != null && weakest.Alive)
            {
                weakest.Statuses.Apply(StatusKind.Panic, ShadeSized(side, ShadeRoute.Fear, ShadeRules.FearPanicTicks));
                if (form >= 3) Turncoat(side, weakest);
            }
            for (int i = 0; broke != null && i < broke.Count && spreads > 0 && !_finished; i++)
            {
                spreads--;
                FearAround(side, broke[i], form, ShadeRules.DreadDetail, ref spreads);
            }
        }

        // Turncoat (rules 28): the one Fear panicked hits the first of his own side beside him once, for his power. The blow is
        // the trick's, so it counts for the side whose Fear it was.
        private void Turncoat(int side, BattleUnit turned)
        {
            List<BattleUnit> beside = BrandedAndBeside(turned);
            BattleUnit victim = null;
            for (int k = 1; k < beside.Count && victim == null; k++)
                if (beside[k].Alive && beside[k].Side == turned.Side) victim = beside[k];
            if (victim == null) return;
            int power = Math.Max(1, turned.Power);
            Emit(new BattleEvent(_tick, BattleEventKind.Trick, side, turned.Id, (int)turned.Class, victim.Id, (int)victim.Class, power,
                ShadeRules.TurncoatDetail));
            ApplyDamage(turned, side, victim, power, ShadeRules.TurncoatDetail);
        }

        // ------------------------------------------------------------ the clocks

        // Shields and backstabs run out.
        private void CountDownFloor()
        {
            for (int i = 0; i < _units.Count; i++)
            {
                StatusList statuses = _units[i].Statuses;
                if (statuses.CountDown(StatusKind.Shield)) statuses.Clear(StatusKind.Shield);
                if (statuses.CountDown(StatusKind.Backstab)) statuses.Clear(StatusKind.Backstab);
            }
        }

        // From rules 28 the field, Backstab's lock, the mine, its return and Breach's exposure run out; a backstab that ran
        // out keeps no hits; Rupture's steps go with the Focus; Crack and Gap run out; and an Unleash that ran out lets its
        // echoes go.
        private void CountDownShade()
        {
            for (int s = 0; s < 2; s++)
            {
                SideState state = _sides[s];
                if (state.FieldTicks > 0 && --state.FieldTicks == 0) state.RelaySize = 0;
                if (state.FallBackLockTicks > 0) state.FallBackLockTicks--;
                if (state.MineTicks > 0) state.MineTicks--;
                if (state.ReturnTicks > 0) state.ReturnTicks--;
                if (state.ExposedTicks > 0) state.ExposedTicks--;
                if (state.RuptureTargetId.Length > 0 &&
                    (state.FocusTicks <= 0 || !string.Equals(state.FocusTargetId, state.RuptureTargetId, StringComparison.Ordinal)))
                {
                    state.RuptureTargetId = string.Empty;
                    state.RuptureSteps = 0;
                }
            }
            for (int i = 0; i < _units.Count; i++)
            {
                StatusList statuses = _units[i].Statuses;
                Status stab = statuses[StatusKind.Backstab];
                if (!stab.On && stab.Store != 0) stab.Store = 0;
                if (statuses.CountDown(StatusKind.Crack)) statuses.Clear(StatusKind.Crack);
                if (statuses.CountDown(StatusKind.NoCover)) statuses.Clear(StatusKind.NoCover);
            }
            for (int s = 0; s < 2 && !_finished; s++)
                if (_sides[s].EchoRecording && _sides[s].UnleashTicks <= 0) ReleaseEchoes(s, _sides[s], false);
        }

        private void AppendShadeSide(SnapshotBuilder b, SideState state)
        {
            b.Field("field", state.FieldTicks);
            b.Field("relay", state.RelaySize);
            b.Field("refugeUsed", state.RefugeUsed);
            b.Field("fallBackLock", state.FallBackLockTicks);
            b.Field("ruptureTarget", state.RuptureTargetId);
            b.Field("ruptureSteps", state.RuptureSteps);
            b.Field("echoRecording", state.EchoRecording);
            b.Field("mine", state.MineTicks);
            b.Field("return", state.ReturnTicks);
            b.Field("exposed", state.ExposedTicks);
            b.Field("breach", state.BreachTicks);
        }

        private void AppendShadeUnit(SnapshotBuilder b, BattleUnit u)
        {
            b.Field(u.Id + ".backstabHits", u.Statuses[StatusKind.Backstab].Store);
            b.Field(u.Id + ".crack", u.Statuses[StatusKind.Crack].Ticks);
            b.Field(u.Id + ".noCover", u.Statuses[StatusKind.NoCover].Ticks);
            b.Field(u.Id + ".echo", u.EchoStored);
            b.Field(u.Id + ".echoTarget", u.EchoTargetId);
        }

        // ------------------------------------------------------------ the other five routes: reads (rules 28)

        /// The ticks this side's Rift Mine is still armed, and its return still keeps its attacks waiting.
        public int MineTicksLeft(int side) => _shadeOn && side >= 0 && side <= 1 ? _sides[side].MineTicks : 0;
        public int ReturnTicksLeft(int side) => _shadeOn && side >= 0 && side <= 1 ? _sides[side].ReturnTicks : 0;

        /// The ticks this side still takes Breach's extra damage.
        public int ExposedTicksLeft(int side) => _shadeOn && side >= 0 && side <= 1 ? _sides[side].ExposedTicks : 0;

        /// Rupture: whom this side's weak point is on (empty for nobody), and its steps.
        public string RuptureTargetOf(int side) => _shadeOn && side >= 0 && side <= 1 ? _sides[side].RuptureTargetId : string.Empty;
        public int RuptureStepsOf(int side) => _shadeOn && side >= 0 && side <= 1 ? _sides[side].RuptureSteps : 0;

        /// Echo: whether this side's Unleash is recording, and what his echo holds.
        public bool EchoRecordingOf(int side) => _shadeOn && side >= 0 && side <= 1 && _sides[side].EchoRecording;
        public int EchoStoredOf(BattleUnit unit) => _shadeOn && unit != null ? unit.EchoStored : 0;

        // ------------------------------------------------------------ the follow up presses (rules 28)

        /// What pressing this order now would do instead of the plain order, for the button: "Rupture", "Ram" or "Release"
        /// (with Converge, still "Release"), or empty for the plain order. A follow up costs nothing.
        public string FollowUpOf(int side, Directive directive, string targetId = "")
        {
            if (!_shadeOn || side < 0 || side > 1 || _finished) return string.Empty;
            SideState state = _sides[side];
            if (directive == Directive.FocusFire && KitOf(side).Has(ShadeRoute.Rupture) && state.FocusTicks > 0 &&
                string.Equals(state.FocusTargetId, targetId ?? string.Empty, StringComparison.Ordinal))
                return ShadeRules.FormName(ShadeRoute.Rupture, 1);
            if (directive != Directive.Unleash || state.UnleashTicks <= 0) return string.Empty;
            if (FormOf(side, ShadeRoute.Breach) >= 3) return ShadeRules.FormName(ShadeRoute.Breach, 3);
            if (FormOf(side, ShadeRoute.Echo) >= 2) return ShadeRules.FormName(ShadeRoute.Echo, 2);
            return string.Empty;
        }

        // A press that follows up an order still running: Rupture's second Focus Fire on its target, and while Unleash runs
        // Breach's Ram or Echo's Release. Each is free and never fires the plain order (finding F3). 1 when it went, -1 when it
        // is refused (failure says why, nothing spent), 0 when it is no follow up and the plain order goes.
        private int FollowUp(int side, Directive directive, string targetId, out string failure)
        {
            failure = string.Empty;
            SideState state = _sides[side];
            if (directive == Directive.FocusFire && KitOf(side).Has(ShadeRoute.Rupture) && state.FocusTicks > 0 &&
                string.Equals(state.FocusTargetId, targetId, StringComparison.Ordinal))
            {
                BattleUnit target = FindById(targetId);
                if (target == null || !target.Alive) return 0;
                if (state.RuptureSteps <= 0)
                {
                    failure = "Nothing to rupture yet.";
                    return -1;
                }
                Rupture(side, state, target);
                return 1;
            }
            if (directive != Directive.Unleash) return 0;
            // One Impact a Breach: no new one while it runs or its exposure lasts, except its Ram.
            if (KitOf(side).Has(ShadeRoute.Breach) && state.ExposedTicks > 0 &&
                (state.UnleashTicks <= 0 || FormOf(side, ShadeRoute.Breach) < 3))
            {
                failure = "Breach is still open: " + SecondsLeft(state.ExposedTicks) + ".";
                return -1;
            }
            if (state.UnleashTicks <= 0) return 0;
            if (FormOf(side, ShadeRoute.Breach) >= 3)
            {
                if (state.UnleashTicks < ShadeRules.RamMinTicks)
                {
                    failure = "Too little Unleash left to ram.";
                    return -1;
                }
                Ram(side, state);
                return 1;
            }
            if (FormOf(side, ShadeRoute.Echo) >= 2)
            {
                if (!AnyEcho(side))
                {
                    failure = "No echoes to let go yet.";
                    return -1;
                }
                state.UnleashTicks = 0;
                ReleaseEchoes(side, state, FormOf(side, ShadeRoute.Echo) >= 3);
                return 1;
            }
            return 0;
        }

        // A blow of this side landed (Land): on its Rupture target it opens a step, and while its Unleash records it goes into
        // the striker's echo. Her tricks' own damage, traps, the Riposte and damage over time are no blows.
        private void ShadeLanded(BattleUnit source, int sourceSide, BattleUnit victim, int hpLost, string detail)
        {
            if (source == null || sourceSide < 0 || sourceSide > 1 || source.Side != sourceSide || victim.Side != 1 - sourceSide) return;
            if (hpLost <= 0 || NoBlow(detail) || ShadeRules.IsTrickDetail(detail) || detail == TrapDetail || detail == FightRules.RiposteDetail)
                return;
            SideState state = _sides[sourceSide];
            if (state.RuptureSteps < ShadeRules.RuptureSteps && state.FocusTicks > 0 &&
                string.Equals(state.RuptureTargetId, victim.Id, StringComparison.Ordinal) && KitOf(sourceSide).Has(ShadeRoute.Rupture))
            {
                state.RuptureSteps++;
                Emit(new BattleEvent(_tick, BattleEventKind.Trick, sourceSide, source.Id, (int)source.Class, victim.Id, (int)victim.Class,
                    state.RuptureSteps, ShadeRules.WeakPointDetail));
            }
            if (state.EchoRecording && state.UnleashTicks > 0)
            {
                // Echo Strike (rules 29) keeps more of a Skirmisher's.
                int kept = hpLost * EchoShare(source) / 100;
                if (kept <= 0) return;
                source.EchoStored += kept;
                if (source.EchoTargetId.Length == 0) source.EchoTargetId = victim.Id;
            }
        }

        // ------------------------------------------------------------ Mirror (Hold)

        // A clean parry by this side of doer's beat: Mirror sends part of his health back at him, more with the parries in a
        // row at Catch, and at Pierce half of that through him into the first of his side behind him.
        private void Mirror(int side, BattleUnit doer, int chain)
        {
            if (!KitOf(side).Has(ShadeRoute.Mirror) || doer == null || !doer.Alive || _finished) return;
            int form = FormOf(side, ShadeRoute.Mirror);
            int times = form >= 2 ? Math.Min(ShadeRules.MirrorCatchMax, Math.Max(1, chain)) : 1;
            int damage = Math.Max(1, doer.MaxHp * ShadeRules.MirrorPercent * times / 100);
            BattleUnit behind = form >= 3 ? Behind(doer) : null;
            Emit(new BattleEvent(_tick, BattleEventKind.Trick, side, string.Empty, -1, doer.Id, (int)doer.Class, damage, ShadeRules.MirrorDetail));
            ApplyDamage(null, side, doer, damage, ShadeRules.MirrorDetail);
            if (behind != null && behind.Alive && !_finished)
                ApplyDamage(null, side, behind, Math.Max(1, damage * ShadeRules.PiercePercent / 100), ShadeRules.MirrorDetail);
        }

        // The first standing one of his side in the row behind his: none for one in the back row.
        private BattleUnit Behind(BattleUnit unit)
        {
            if (unit.Row != Row.Front) return null;
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (u.Alive && u.Side == unit.Side && u.Row == Row.Back && !u.Benched) return u;
            }
            return null;
        }

        // ------------------------------------------------------------ Rift Mine (Fall Back)

        // A Fall Back by this side: Rift Mine arms its mine (a new one replaces the old) and its attacks wait out the return.
        private void RiftMine(int side, SideState state)
        {
            if (!KitOf(side).Has(ShadeRoute.RiftMine)) return;
            state.MineTicks = ShadeRules.MineTicks;
            state.ReturnTicks = state.FallBackTicks * (100 + (ShadeRules.ReturnPercent - 100) * PriceLeft(side) / 100) / 100;
        }

        // attacker is about to strike target: if target's side has a mine armed, it blasts him (and at Wide Blast the ones beside
        // him), and at Shock his next turn waits. True when the blast dropped him or ended the fight, so the blow never comes.
        private bool MineGoesOff(BattleUnit attacker, BattleUnit target)
        {
            int side = target.Side;
            if (side < 0 || side > 1 || attacker.Side == side) return false;
            SideState state = _sides[side];
            if (state.MineTicks <= 0 || !KitOf(side).Has(ShadeRoute.RiftMine)) return false;
            state.MineTicks = 0;
            int form = FormOf(side, ShadeRoute.RiftMine);
            var blasted = new List<BattleUnit> { attacker };
            if (form >= 3)
            {
                List<BattleUnit> beside = BrandedAndBeside(attacker);
                for (int k = 1; k < beside.Count; k++) if (beside[k].Alive) blasted.Add(beside[k]);
            }
            Emit(new BattleEvent(_tick, BattleEventKind.Trick, side, string.Empty, -1, attacker.Id, (int)attacker.Class, blasted.Count,
                ShadeRules.MineDetail));
            for (int k = 0; k < blasted.Count && !_finished; k++)
            {
                BattleUnit u = blasted[k];
                if (!u.Alive) continue;
                ApplyDamage(null, side, u, Math.Max(1, u.MaxHp * ShadeRules.MinePercent / 100), ShadeRules.MineDetail);
                if (form >= 2 && u.Alive) u.Cooldown += ShadeRules.ShockTicks;
            }
            return _finished || !attacker.Alive;
        }

        // ------------------------------------------------------------ Rupture (Focus Fire)

        // A new Focus Fire of this side: with Rupture its weak point starts over on the new target.
        private void RuptureOn(int side, SideState state, BattleUnit target)
        {
            if (!KitOf(side).Has(ShadeRoute.Rupture)) return;
            state.RuptureTargetId = target.Id;
            state.RuptureSteps = 0;
        }

        // The follow up press: the weak point ruptures for its steps and the Focus ends. A full one cracks him at Crack, and at
        // Finish pulls your fighters' next turns forward.
        private void Rupture(int side, SideState state, BattleUnit target)
        {
            int steps = state.RuptureSteps;
            int form = FormOf(side, ShadeRoute.Rupture);
            int damage = Math.Max(1, target.MaxHp * ShadeRules.RupturePercent * steps / 100);
            state.RuptureSteps = 0;
            state.RuptureTargetId = string.Empty;
            state.FocusTicks = 0;
            state.FocusTargetId = string.Empty;
            Emit(new BattleEvent(_tick, BattleEventKind.Trick, side, string.Empty, -1, target.Id, (int)target.Class, steps, ShadeRules.RuptureDetail));
            ApplyDamage(null, side, target, damage, ShadeRules.RuptureDetail);
            if (steps < ShadeRules.RuptureSteps || _finished) return;
            if (form >= 2 && target.Alive) target.Statuses.Apply(StatusKind.Crack, ShadeRules.CrackTicks);
            if (form >= 3) Finish(side);
        }

        // Finish: each of this side's fighters but a Medic whose turn comes within FinishTicks takes it now, and the time he
        // took waits on after his next one (finding F2: they borrow it).
        private void Finish(int side)
        {
            int pulled = 0;
            for (int i = 0; i < _units.Count && !Stopped; i++)
            {
                BattleUnit u = _units[i];
                if (!u.Alive || u.Side != side || u.Benched || u.Class == UnitClass.Medic) continue;
                if (u.Cooldown <= 0 || u.Cooldown > ShadeRules.FinishTicks || u.PanicTicks > 0) continue;
                if (_beastOn && IsHeld(u)) continue;
                if (_heavyOn && IsSwingingHeavy(u)) continue;
                if (_kitsOn && KitBusy(u)) continue;
                if (_classOn && (u.SnareTicks > 0 || u.AimTicks > 0 || u.PinTicks > 0 || u.EvacTicks > 0)) continue;
                int borrowed = u.Cooldown;
                pulled++;
                Act(u);
                if (u.Alive) u.Cooldown = borrowed + HeavyTurn(u, NextCooldown(u));
            }
            Emit(new BattleEvent(_tick, BattleEventKind.Trick, side, string.Empty, -1, string.Empty, -1, pulled, ShadeRules.FinishDetail));
            if (!Stopped) CheckWipe();
        }

        // Crack: his armour this much thinner while it holds.
        private int Cracked(BattleUnit victim, int armor)
        {
            if (!_shadeOn || armor <= 0 || !victim.Statuses[StatusKind.Crack].On) return armor;
            return armor * (100 - ShadeRules.CrackPercent) / 100;
        }

        // ------------------------------------------------------------ Breach and Echo (Unleash)

        // An Unleash by this side: Echo starts recording, Breach hits their front line and exposes yours.
        private void ShadeUnleash(int side, SideState state)
        {
            // A plain Unleash again while it records keeps what it recorded.
            if (KitOf(side).Has(ShadeRoute.Echo) && !state.EchoRecording)
            {
                state.EchoRecording = true;
                for (int i = 0; i < _units.Count; i++)
                    if (_units[i].Side == side)
                    {
                        _units[i].EchoStored = 0;
                        _units[i].EchoTargetId = string.Empty;
                    }
            }
            if (!KitOf(side).Has(ShadeRoute.Breach)) return;
            state.ExposedTicks = state.UnleashTicks + ShadeRules.ExposedAfterTicks;
            state.BreachTicks = state.UnleashTicks;
            int form = FormOf(side, ShadeRoute.Breach);
            List<BattleUnit> front = FrontLine(1 - side);
            Emit(new BattleEvent(_tick, BattleEventKind.Trick, side, string.Empty, -1, string.Empty, -1, front.Count, ShadeRules.BreachDetail));
            for (int k = 0; k < front.Count && !_finished; k++)
            {
                BattleUnit u = front[k];
                if (!u.Alive) continue;
                ApplyDamage(null, side, u, Math.Max(1, u.MaxHp * ShadeRules.ImpactPercent / 100), ShadeRules.BreachDetail);
                if (!u.Alive) continue;
                u.Cooldown += ShadeRules.ShockTicks;
                if (form >= 2) u.Statuses.Apply(StatusKind.NoCover, ShadeRules.GapTicks);
            }
        }

        // Ram, the follow up press: the Unleash ends and rams their front line, harder the more of it was left.
        private void Ram(int side, SideState state)
        {
            int left = state.UnleashTicks;
            int length = Math.Max(left, state.BreachTicks);
            state.UnleashTicks = 0;
            List<BattleUnit> front = FrontLine(1 - side);
            Emit(new BattleEvent(_tick, BattleEventKind.Trick, side, string.Empty, -1, string.Empty, -1, left, ShadeRules.RamDetail));
            for (int k = 0; k < front.Count && !_finished; k++)
            {
                BattleUnit u = front[k];
                if (!u.Alive) continue;
                int damage = (int)Math.Max(1, (long)u.MaxHp * ShadeRules.RamPercent / 100 * left / Math.Max(1, length));
                ApplyDamage(null, side, u, damage, ShadeRules.RamDetail);
            }
        }

        // Up to BreachTargets standing ones of this side's front row, its back row if the front is empty, in setup order.
        private List<BattleUnit> FrontLine(int side)
        {
            var line = new List<BattleUnit>();
            for (int pass = 0; pass < 2 && line.Count == 0; pass++)
            {
                Row row = pass == 0 ? Row.Front : Row.Back;
                for (int i = 0; i < _units.Count && line.Count < ShadeRules.BreachTargets; i++)
                {
                    BattleUnit u = _units[i];
                    if (u.Alive && u.Side == side && u.Row == row && !u.Benched) line.Add(u);
                }
            }
            return line;
        }

        // Echo's price: with Echo equipped this side's Unleash bonus is halved.
        private int UnleashPercent(int side)
        {
            if (!_shadeOn || side < 0 || side > 1 || !KitOf(side).Has(ShadeRoute.Echo)) return UnleashDamagePercent;
            // Half the bonus, less of a cut with Command Relay (rules 29).
            return 100 + (UnleashDamagePercent - 100) * (100 - 50 * PriceLeft(side) / 100) / 100;
        }

        private bool AnyEcho(int side)
        {
            for (int i = 0; i < _units.Count; i++) if (_units[i].Side == side && _units[i].EchoStored > 0) return true;
            return false;
        }

        // The echoes go: each hits the first one it recorded, or, converging, this side's Focus Fire target if he stands. An
        // echo whose target is gone is wasted. It may go after its fighter fell.
        private void ReleaseEchoes(int side, SideState state, bool converge)
        {
            state.EchoRecording = false;
            BattleUnit focus = converge && state.FocusTicks > 0 ? FindById(state.FocusTargetId) : null;
            if (focus != null && (!focus.Alive || focus.Side == side)) focus = null;
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (u.Side != side || u.EchoStored <= 0) continue;
                int stored = u.EchoStored;
                BattleUnit target = focus ?? FindById(u.EchoTargetId);
                u.EchoStored = 0;
                u.EchoTargetId = string.Empty;
                if (_finished || target == null || !target.Alive || target.Side == side) continue;
                Emit(new BattleEvent(_tick, BattleEventKind.Trick, side, u.Id, (int)u.Class, target.Id, (int)target.Class, stored, ShadeRules.EchoDetail));
                ApplyDamage(u, side, target, stored, ShadeRules.EchoDetail);
            }
        }
    }
}
