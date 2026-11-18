using System;
using System.Collections.Generic;

namespace KindredSiege.Sim.Battle
{
    /// The Stoneveil chief's kit, the blood rite (Jacob, 22 Oct 2026, later: "yes, lock both kits as written", then "go, build
    /// the Stoneveil and Thundercoast chief kits in the sim and hand the API to Codex"; DESIGN_LOCKS "The Stoneveil and
    /// Thundercoast chiefs"). His Blood Call on its own clock, which your Medic answers by stitching wounds shut during the
    /// chant; his Basin, Leeches, Open Veins and Transfusion; his Red Rite at half health; Clean Hands, his hidden chain; and,
    /// with FightRules.ChiefTactics, Blood Price. ChiefKits holds the numbers. Only fights where a side's kit is his come here.
    /// His Leeches pick their mark from the fight's seeded stream; nothing else here draws.
    ///
    /// Claude's calls while building, Jacob's to change: his acolyte is his band's first free Medic, else its first free
    /// fighter, and does nothing else while it carries the Basin; the Basin fills a drop a second for each bleeding fighter of
    /// yours and is full at four; a Basin that never filled answers him as a Starve, a new answer; his Leeches land on your back
    /// row a move can fall on (never your leader while another of yours stands), wait while it has nobody, and feed his side's
    /// most hurt, him included; your Medic's turn goes first to the fighter his Leeches drink from, and while his chant or his
    /// Open Veins run to your most hurt bleeding fighter at any health, and that treatment plucks the leeches off or stitches
    /// the wound shut before it heals; Open Veins makes each second of your bleeding take twice as much, a Hold brings it back
    /// to as much, and with anyone bled under it a Hold answers it as a Brace; his Transfusion waits while he chants, draws at
    /// each second of its channel into his band's most hurt fighter, not him, and the first draw your Warden blocks ends it,
    /// your Warden covering the one it draws from as he covers a hooked ally; leeches drinking and blood drawn are no blows, so
    /// they break no aim and lose no shot, like bleeding; his band's harder blows after the drink are their ordinary blows, not
    /// their heavy swings, as with the flood; Blood Price counts the orders you give, not your side's reflexes, and brings on
    /// his soonest due move that is not running (ties: Blood Call, Basin, Leeches, Open Veins, Transfusion); the Red Rite makes
    /// every wound left on your side from then on bleed twice as long, and his Blood Call heals him for all it rips; Clean Hands
    /// counts each of his moves landing, whatever answered it, starts over when one lands with anyone of yours bleeding, stuns
    /// him, stops his band's barbed snares bleeding as well as their blades, and goes off once a fight.
    public sealed partial class BattleSimulation
    {
        // A side's kit is the Stoneveil chief's (set in the constructor).
        private readonly bool _riteOn;

        // His Blood Price's pool order, for its ties.
        private const int RiteMoves = 5;

        // ------------------------------------------------------------ questions the screen asks

        /// True from the Stoneveil chief's half health, his Red Rite: wounds left on your side bleed twice as long, and his
        /// Blood Call heals him for what it rips out.
        public bool RedRite(int side)
        {
            BattleUnit chief = KitChief(side);
            return Kit(side) == ChiefKit.Stoneveil && chief != null && chief.PhaseDone;
        }

        /// Ticks his acolyte still carries the Basin, or 0, who carries it, and the drops in it so far (ChiefKits.BasinFull
        /// fills it).
        public int BasinTicksLeft(int side)
        {
            return Kit(side) == ChiefKit.Stoneveil ? _sides[side].BasinTicks : 0;
        }

        public string BasinCarrierId(int side)
        {
            return BasinTicksLeft(side) > 0 ? _sides[side].BasinCarrierId : string.Empty;
        }

        public int BasinFill(int side)
        {
            return BasinTicksLeft(side) > 0 ? _sides[side].BasinFill : 0;
        }

        /// Ticks his band still hits harder after he drank his Basin, or 0.
        public int DrinkTicksLeft(int side)
        {
            return Kit(side) == ChiefKit.Stoneveil ? _sides[side].DrinkTicks : 0;
        }

        /// Ticks his Leeches still drink, or 0, and from whom.
        public int LeechTicksLeft(int side)
        {
            return Kit(side) == ChiefKit.Stoneveil ? _sides[side].LeechTicks : 0;
        }

        public string LeechTargetId(int side)
        {
            return LeechTicksLeft(side) > 0 ? _sides[side].LeechTargetId : string.Empty;
        }

        /// Ticks his Open Veins still run, or 0.
        public int VeinsTicksLeft(int side)
        {
            return Kit(side) == ChiefKit.Stoneveil ? _sides[side].VeinsTicks : 0;
        }

        /// Ticks his Transfusion's channel still runs, or 0, and whom he draws from.
        public int TransfusionTicksLeft(int side)
        {
            return Kit(side) == ChiefKit.Stoneveil ? _sides[side].DrawTicks : 0;
        }

        public string TransfusionTargetId(int side)
        {
            return TransfusionTicksLeft(side) > 0 ? _sides[side].DrawTargetId : string.Empty;
        }

        /// True when his tactical piece, Blood Price, is on (FightRules.ChiefTactics), and the drops your orders have fed it
        /// since it last came due, 0 to ChiefKits.PriceOrders - 1: his card shows ChiefKits.PriceOrders.
        public bool PriceOn(int side)
        {
            return _tacticsOn && Kit(side) == ChiefKit.Stoneveil;
        }

        public int PriceDrops(int side)
        {
            return PriceOn(side) ? _sides[side].PriceDrops : 0;
        }

        /// True once your Clean Hands broke his rite: his band's blades leave no more wounds this fight.
        public bool RiteBroken(int side)
        {
            return Kit(side) == ChiefKit.Stoneveil && _sides[side].CleanHands;
        }

        // ------------------------------------------------------------ setup and clocks

        // His moves' first clocks. His Blood Call is his spell, on SideRules.AutoEclipseTicks.
        private void StartRiteClocks(int side)
        {
            if (Kit(side) != ChiefKit.Stoneveil) return;
            SideState state = _sides[side];
            if (HasMove(side, ChiefMove.Basin)) state.BasinNext = ChiefKits.BasinEveryTicks;
            if (HasMove(side, ChiefMove.Leeches)) state.LeechNext = ChiefKits.LeechEveryTicks;
            if (HasMove(side, ChiefMove.OpenVeins)) state.VeinsNext = ChiefKits.VeinsEveryTicks;
            if (HasMove(side, ChiefMove.Transfusion)) state.DrawNext = ChiefKits.TransfusionEveryTicks;
        }

        // His moves start on their own clocks.
        private void StartRiteMoves(int side)
        {
            if (Kit(side) != ChiefKit.Stoneveil) return;
            TryStartBasin(side);
            if (!Stopped) TryStartLeeches(side);
            if (!Stopped) TryStartVeins(side);
            if (!Stopped) TryStartTransfusion(side);
        }

        // And run on after the tick's blows, landing when their time comes; his drink wears off.
        private void AdvanceRiteMoves(int side)
        {
            if (Kit(side) != ChiefKit.Stoneveil) return;
            if (_sides[side].DrinkTicks > 0) _sides[side].DrinkTicks--;
            AdvanceBasin(side);
            if (!Stopped) AdvanceLeeches(side);
            if (!Stopped) AdvanceVeins(side);
            if (!Stopped) AdvanceTransfusion(side);
        }

        // Busy with his rite, so it does nothing else this tick: his acolyte carrying the Basin, and he channelling.
        private bool RiteBusy(BattleUnit unit)
        {
            SideState state = _sides[unit.Side];
            if (Kit(unit.Side) != ChiefKit.Stoneveil) return false;
            if (state.BasinTicks > 0 && string.Equals(unit.Id, state.BasinCarrierId, StringComparison.Ordinal)) return true;
            return (state.DrawTicks > 0 || state.DrawLatch > 0) && unit.Role == UnitRole.Chief;
        }

        // How many of a side's fighters bleed now.
        private int BleedingCount(int side)
        {
            int n = 0;
            for (int i = 0; i < _units.Count; i++)
                if (_units[i].Alive && _units[i].Side == side && _units[i].BleedTicks > 0) n++;
            return n;
        }

        // A side's most hurt standing unit, or of its band only; ties go to the lower index.
        private BattleUnit MostHurtOf(int side, bool bandOnly)
        {
            BattleUnit best = null;
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (!u.Alive || u.Side != side || (bandOnly && u.Role != UnitRole.Fighter)) continue;
                if (best == null || u.HpPermille < best.HpPermille) best = u;
            }
            return best;
        }

        // His rite draws this percent of a fighter's health and feeds what it took to his side's most hurt, or his band's.
        // Returns what it took.
        private int Drain(BattleUnit chief, int side, BattleUnit victim, int percent, string detail, bool bandOnly)
        {
            int before = victim.Hp;
            ApplyDamage(chief, side, victim, Armored(victim, Math.Max(1, victim.MaxHp * percent / 100)), detail);
            int taken = before - victim.Hp;
            if (taken <= 0 || Stopped) return taken;
            BattleUnit fed = MostHurtOf(side, bandOnly);
            if (fed != null) Heal(chief, fed, taken, detail);
            return taken;
        }

        // ------------------------------------------------------------ Blood Call

        // His Blood Call goes off after its chant. Fall Back got clear (a Dodge). Otherwise it rips the blood out of every
        // bleeding fighter of the other side, as the cave tribe's call always has, and under his Red Rite heals him for what it
        // ripped. A wound your Medic stitched shut during the chant answers it (a Heal); with none stitched, if it ripped anyone
        // you ate it; nobody bleeding, nothing came of it.
        private void ReleaseRite(int side, BattleUnit caster, int percent, bool dodged)
        {
            SideState state = _sides[side];
            int enemy = 1 - side;
            int stitched = state.ChantStitches;
            state.ChantStitches = 0;
            Emit(new BattleEvent(_tick, BattleEventKind.Release, side, caster.Id, (int)caster.Class, string.Empty, -1, percent,
                dodged ? Moves.Dodged : Moves.Full));
            string name = ChiefSpells.Name(ChiefSpell.BloodCall) + ".";
            if (dodged)
            {
                Emit(EventFor(BattleEventKind.Land, caster, null, 0, name + ChiefKits.Dodged));
                Earn(enemy, FightRules.DodgePoints, FightRules.DodgeReason);
                FavourDodge(enemy, caster);
                RiteLanded(side, caster);
                if (!Stopped) Answer(enemy, ChiefAnswer.Dodge, caster, FightRules.DodgePoints);
                return;
            }
            int total = 0;
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (u.Alive && u.Side == enemy && u.BleedTicks > 0) total += Math.Max(1, u.MaxHp * percent / 100);
            }
            string how = stitched > 0 ? ChiefKits.Healed : total > 0 ? ChiefKits.Full : ChiefKits.Clear;
            Emit(EventFor(BattleEventKind.Land, caster, null, total, name + how));
            RiteLanded(side, caster);
            int ripped = CallBlood(side, caster, percent);
            if (Stopped) return;
            if (ripped > 0 && caster.Alive && RedRite(side)) Heal(caster, caster, ripped, ChiefSpells.BloodDetail);
            if (stitched > 0) Answered(enemy, ChiefAnswer.Heal, caster, ChiefKits.HealPoints, ChiefKits.Name(ChiefAnswer.Heal));
            else if (total > 0)
            {
                Say(caster, ChiefKits.EclipseLine);
                Eaten(enemy, caster);
            }
        }

        // ------------------------------------------------------------ your Medic against his rite

        // His chant or his Open Veins run: a treatment now stitches a wound shut.
        private bool Stitching(int side)
        {
            SideState his = _sides[side];
            return his.VeinsTicks > 0 || (his.Windup > 0 && _rules[side].Spell == ChiefSpell.BloodCall);
        }

        // Who a Medic facing him treats first: the one his Leeches drink from, then, while his chant or his Open Veins run, her
        // side's most hurt bleeding fighter at any health. Null to treat as she always does.
        private BattleUnit RitePatient(BattleUnit medic)
        {
            int foe = 1 - medic.Side;
            if (Kit(foe) != ChiefKit.Stoneveil) return null;
            SideState his = _sides[foe];
            if (his.LeechTicks > 0)
            {
                BattleUnit leeched = FindById(his.LeechTargetId);
                if (leeched != null && leeched.Alive && leeched.Side == medic.Side) return leeched;
            }
            if (!Stitching(foe)) return null;
            BattleUnit best = null;
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (!u.Alive || u.Side != medic.Side || u.BleedTicks <= 0) continue;
                if (best == null || u.HpPermille < best.HpPermille) best = u;
            }
            return best;
        }

        // A treatment from a Medic facing him, before it heals, so a patient at full health counts too: it plucks his Leeches
        // off her patient (a Heal), or, while his chant or his Open Veins run, stitches her patient's bleeding wound shut.
        private void RiteTreated(BattleUnit healer, BattleUnit patient)
        {
            if (Stopped || healer == null || !healer.Alive || healer.Class != UnitClass.Medic || healer.Side != patient.Side) return;
            int foe = 1 - patient.Side;
            if (Kit(foe) != ChiefKit.Stoneveil) return;
            BattleUnit chief = KitChief(foe);
            if (chief == null) return;
            SideState his = _sides[foe];
            if (his.LeechTicks > 0 && string.Equals(his.LeechTargetId, patient.Id, StringComparison.Ordinal))
            {
                ClearLeeches(his);
                Emit(EventFor(BattleEventKind.Land, chief, patient, 0, ChiefKits.LeechName + "." + ChiefKits.Healed));
                RiteLanded(foe, chief);
                if (!Stopped) Answered(patient.Side, ChiefAnswer.Heal, chief, ChiefKits.HealPoints, ChiefKits.Name(ChiefAnswer.Heal));
                return;
            }
            if (patient.BleedTicks <= 0 || !Stitching(foe)) return;
            int left = patient.BleedTicks;
            patient.Statuses.Clear(StatusKind.Bleed);
            Emit(EventFor(BattleEventKind.Cash, healer, patient, left, ChiefKits.StitchDetail));
            if (his.VeinsTicks > 0) his.VeinsStitched = true;
            if (his.Windup > 0) his.ChantStitches++;
        }

        // ------------------------------------------------------------ the Basin

        // Every BasinEveryTicks his acolyte carries the Basin forward, while he can move and an acolyte of his is free.
        private void TryStartBasin(int side)
        {
            SideState state = _sides[side];
            if (!HasMove(side, ChiefMove.Basin) || state.BasinTicks > 0 || _tick < state.BasinNext) return;
            BattleUnit chief = KitChief(side);
            BattleUnit acolyte = chief != null && CanMove(chief) ? Acolyte(side) : null;
            if (acolyte == null)
            {
                state.BasinNext = _tick + 1;
                return;
            }
            state.BasinTicks = ChiefKits.BasinTicks;
            state.BasinCarrierId = acolyte.Id;
            state.BasinFill = 0;
            state.BasinNext = _tick + ChiefKits.BasinEveryTicks;
            Emit(EventFor(BattleEventKind.Tell, acolyte, null, ChiefKits.BasinTicks, ChiefKits.BasinName));
        }

        // His acolyte: his band's first free Medic, else its first free fighter.
        private BattleUnit Acolyte(int side)
        {
            BattleUnit first = null;
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (!u.Alive || u.Side != side || u.Role != UnitRole.Fighter || u.SnareTicks > 0 || u.PanicTicks > 0 || u.StunTicks > 0 || (_fight.FreezeContracts && ControlBlocked(u)))
                    continue;
                if (u.Class == UnitClass.Medic) return u;
                if (first == null) first = u;
            }
            return first;
        }

        // The Basin comes forward. Focus Fire on the acolyte spills it, as does the acolyte falling (a Break). Each second it
        // fills a drop for each bleeding fighter of the other side; full, he drinks it. Carried all the way without filling,
        // it comes back empty (a Starve).
        private void AdvanceBasin(int side)
        {
            SideState state = _sides[side];
            if (state.BasinTicks <= 0 && state.BasinHold <= 0) return;
            int enemy = 1 - side;
            BattleUnit chief = KitChief(side);
            BattleUnit acolyte = FindById(state.BasinCarrierId);
            if (chief == null || acolyte == null)
            {
                ClearBasin(state);
                return;
            }
            string name = ChiefKits.BasinName + ".";
            if (!acolyte.Alive || IsFocusing(enemy, acolyte))
            {
                ClearBasin(state);
                Emit(EventFor(BattleEventKind.Land, acolyte, null, 0, name + ChiefKits.Broken));
                RiteLanded(side, chief);
                if (!Stopped) Answered(enemy, ChiefAnswer.Break, chief, FightRules.InterruptPoints, FightRules.InterruptReason);
                return;
            }
            if (_fight.FreezeContracts && (ControlBlocked(acolyte) || ControlBlocked(chief))) return;
            // Under the parry at the center the drink is its beat (BattleSimulation.Parry.cs).
            if (state.BasinHold > 0)
            {
                if (--state.BasinHold > 0) return;
                DrinkBeat(side, chief, acolyte);
                return;
            }
            state.BasinTicks--;
            if ((ChiefKits.BasinTicks - state.BasinTicks) % SimClock.TicksPerSecond == 0) state.BasinFill += BleedingCount(enemy);
            if (state.BasinFill >= ChiefKits.BasinFull)
            {
                if (_parryOn) DrinkBeat(side, chief, acolyte);
                else DrinkBasin(side, chief, acolyte);
                return;
            }
            if (state.BasinTicks > 0) return;
            ClearBasin(state);
            Emit(EventFor(BattleEventKind.Land, acolyte, null, 0, name + ChiefKits.Empty));
            RiteLanded(side, chief);
            if (!Stopped) Answered(enemy, ChiefAnswer.Starve, chief, ChiefKits.StarvePoints, ChiefKits.Name(ChiefAnswer.Starve));
        }

        // The drink's beat: a wait or a feint holds it; parried, the Basin spills. It hits nobody of yours, so a stale Hold costs
        // nothing here.
        private void DrinkBeat(int side, BattleUnit chief, BattleUnit acolyte)
        {
            SideState state = _sides[side];
            int hold = HoldBeat(side, chief, BeatSlot.Basin);
            if (hold > 0)
            {
                state.BasinHold = hold;
                return;
            }
            if (MeetBeat(1 - side) != BeatMet.Parry)
            {
                DrinkBasin(side, chief, acolyte);
                return;
            }
            ClearBasin(state);
            Emit(EventFor(BattleEventKind.Land, acolyte, null, 0, ChiefKits.BasinName + "." + ChiefKits.Parried));
            RiteLanded(side, chief);
            if (!Stopped) ParriedBeat(side, chief, ChiefKits.BasinName, ChiefKits.ParryNerve, FightRules.ParryPoints, true);
        }

        // Full, he drinks it: he heals ChiefKits.DrinkHealPercent of his health, and his band's blows land harder for a while.
        private void DrinkBasin(int side, BattleUnit chief, BattleUnit acolyte)
        {
            SideState state = _sides[side];
            ClearBasin(state);
            state.DrinkTicks = ChiefKits.DrinkTicks;
            int heal = Math.Min(chief.MaxHp - chief.Hp, Math.Max(1, chief.MaxHp * ChiefKits.DrinkHealPercent / 100));
            Emit(EventFor(BattleEventKind.Land, acolyte, null, heal, ChiefKits.BasinName + "." + ChiefKits.Full));
            RiteLanded(side, chief);
            if (!Stopped) Heal(chief, chief, heal, ChiefKits.BasinDetail);
        }

        // An Unleash from the other side kicks his Basin over (a Smash).
        private void KickBasin(int side)
        {
            int enemy = 1 - side;
            SideState his = _sides[enemy];
            if (Stopped || (his.BasinTicks <= 0 && his.BasinHold <= 0) || Kit(enemy) != ChiefKit.Stoneveil) return;
            BattleUnit chief = KitChief(enemy);
            BattleUnit acolyte = FindById(his.BasinCarrierId);
            ClearBasin(his);
            if (chief == null || acolyte == null) return;
            Emit(EventFor(BattleEventKind.Land, acolyte, null, 0, ChiefKits.BasinName + "." + ChiefKits.Smashed));
            RiteLanded(enemy, chief);
            if (!Stopped) Answered(side, ChiefAnswer.Smash, chief, ChiefSpells.SmashPoints, ChiefSpells.SmashReason);
        }

        // After he drank his Basin his band's blows land harder.
        private int DrinkBlow(BattleUnit attacker, int damage)
        {
            if (attacker.Role != UnitRole.Fighter || Kit(attacker.Side) != ChiefKit.Stoneveil || _sides[attacker.Side].DrinkTicks <= 0)
                return damage;
            return damage * ChiefKits.DrinkPercent / 100;
        }

        // The same through the damage layers (BattleSimulation.Layers.cs): his Basin in the kit layer.
        private void DrinkHit(BattleUnit attacker, Hit hit)
        {
            if (attacker.Role != UnitRole.Fighter || Kit(attacker.Side) != ChiefKit.Stoneveil || _sides[attacker.Side].DrinkTicks <= 0) return;
            hit.Add(HitLayer.Kit, ChiefKits.DrinkPercent - 100);
        }

        private static void ClearBasin(SideState state)
        {
            state.BasinHold = 0;
            state.Beats[(int)BeatSlot.Basin] = 0;
            state.BasinTicks = 0;
            state.BasinCarrierId = string.Empty;
            state.BasinFill = 0;
        }

        // ------------------------------------------------------------ Leeches

        // Every LeechEveryTicks his leeches land on one of the other side's back row a move can fall on, picked from the
        // fight's seeded stream, while he can move. None there, they wait.
        private void TryStartLeeches(int side)
        {
            SideState state = _sides[side];
            if (!HasMove(side, ChiefMove.Leeches) || state.LeechTicks > 0 || state.LeechLeap > 0 || _tick < state.LeechNext) return;
            BattleUnit chief = KitChief(side);
            List<BattleUnit> pool = chief != null && CanMove(chief) ? MarksIn(1 - side, Row.Back) : new List<BattleUnit>();
            if (pool.Count == 0)
            {
                state.LeechNext = _tick + 1;
                return;
            }
            BattleUnit target = pool[_rng.Next(pool.Count)];
            state.LeechTargetId = target.Id;
            state.LeechDrunk = 0;
            state.LeechNext = _tick + ChiefKits.LeechEveryTicks;
            // Under the parry at the center they leap ParryRules.TellTicks after the tell, and the leap is its beat: the Tell's
            // amount is the ticks until it (BattleSimulation.Parry.cs).
            if (_parryOn)
            {
                state.LeechLeap = ParryRules.TellTicks;
                Emit(EventFor(BattleEventKind.Tell, chief, target, ParryRules.TellTicks, ChiefKits.LeechName));
                return;
            }
            state.LeechTicks = ChiefKits.LeechTicks;
            Emit(EventFor(BattleEventKind.Tell, chief, target, ChiefKits.LeechTicks, ChiefKits.LeechName));
        }

        // The leap: Fall Back gets clear (a Dodge, as shaking them off always was); a parry knocks them away; a stale Hold costs
        // your nerve and they latch on anyway; otherwise they latch on and your parry chain starts over.
        private void LeapLeeches(int side, BattleUnit chief, BattleUnit target)
        {
            SideState state = _sides[side];
            int enemy = 1 - side;
            string name = ChiefKits.LeechName + ".";
            ClearBeat(side, BeatSlot.Leech);
            if (_sides[enemy].FallBackTicks > 0)
            {
                ClearLeeches(state);
                Emit(EventFor(BattleEventKind.Land, chief, target, 0, name + ChiefKits.Dodged));
                RiteLanded(side, chief);
                if (!Stopped) Answered(enemy, ChiefAnswer.Dodge, chief, FightRules.DodgePoints, FightRules.DodgeReason);
                return;
            }
            BeatMet met = MeetBeat(enemy);
            if (met == BeatMet.Parry)
            {
                ClearLeeches(state);
                Emit(EventFor(BattleEventKind.Land, chief, target, 0, name + ChiefKits.Parried));
                RiteLanded(side, chief);
                if (!Stopped) ParriedBeat(side, chief, ChiefKits.LeechName, ChiefKits.ParryNerve, FightRules.ParryPoints, true);
                return;
            }
            if (met == BeatMet.Block) BlockedBeat(side, chief, ChiefKits.ParryNerve);
            else LandedBeat(side);
            if (Stopped) return;
            state.LeechTicks = ChiefKits.LeechTicks;
        }

        // They drink each second and feed his most hurt. Fall Back shakes them loose (a Dodge); your Medic plucks them off
        // (RiteTreated). Drunk all the way, or their mark fallen to them, you ate it; their mark falling to anything else ends
        // them with nothing to show.
        private void AdvanceLeeches(int side)
        {
            SideState state = _sides[side];
            if (state.LeechLeap > 0)
            {
                BattleUnit leaper = KitChief(side);
                BattleUnit mark = FindById(state.LeechTargetId);
                if (leaper == null || !CanMove(leaper) || mark == null || !mark.Alive)
                {
                    ClearLeeches(state);
                    return;
                }
                if (--state.LeechLeap > 0) return;
                int hold = HoldBeat(side, leaper, BeatSlot.Leech);
                if (hold > 0) state.LeechLeap = hold;
                else LeapLeeches(side, leaper, mark);
                return;
            }
            if (state.LeechTicks <= 0) return;
            int enemy = 1 - side;
            BattleUnit chief = KitChief(side);
            BattleUnit target = FindById(state.LeechTargetId);
            if (chief == null || target == null || !target.Alive)
            {
                ClearLeeches(state);
                return;
            }
            string name = ChiefKits.LeechName + ".";
            if (_sides[enemy].FallBackTicks > 0)
            {
                ClearLeeches(state);
                Emit(EventFor(BattleEventKind.Land, chief, target, 0, name + ChiefKits.Dodged));
                RiteLanded(side, chief);
                if (!Stopped) Answered(enemy, ChiefAnswer.Dodge, chief, FightRules.DodgePoints, FightRules.DodgeReason);
                return;
            }
            state.LeechTicks--;
            if ((ChiefKits.LeechTicks - state.LeechTicks) % SimClock.TicksPerSecond == 0)
            {
                state.LeechDrunk += Drain(chief, side, target, ChiefKits.LeechPercent, ChiefKits.LeechDetail, false);
                if (Stopped) return;
            }
            if (state.LeechTicks > 0 && target.Alive) return;
            int drunk = state.LeechDrunk;
            ClearLeeches(state);
            Emit(EventFor(BattleEventKind.Land, chief, target, drunk, name + ChiefKits.Full));
            RiteLanded(side, chief);
            if (!Stopped) Eaten(enemy, chief);
            CheckWipe();
        }

        private static void ClearLeeches(SideState state)
        {
            state.LeechLeap = 0;
            state.Beats[(int)BeatSlot.Leech] = 0;
            state.LeechTicks = 0;
            state.LeechTargetId = string.Empty;
            state.LeechDrunk = 0;
        }

        // ------------------------------------------------------------ Open Veins

        // Every VeinsEveryTicks he opens your veins, while he can move.
        private void TryStartVeins(int side)
        {
            SideState state = _sides[side];
            if (!HasMove(side, ChiefMove.OpenVeins) || state.VeinsTicks > 0 || state.VeinsCut > 0 || _tick < state.VeinsNext) return;
            BattleUnit chief = KitChief(side);
            if (chief == null || !CanMove(chief))
            {
                state.VeinsNext = _tick + 1;
                return;
            }
            ClearVeins(state);
            state.VeinsNext = _tick + ChiefKits.VeinsEveryTicks;
            // Under the parry at the center the cut comes ParryRules.TellTicks after the tell, and it is its beat: the Tell's
            // amount is the ticks until it (BattleSimulation.Parry.cs).
            if (_parryOn)
            {
                state.VeinsCut = ParryRules.TellTicks;
                Emit(EventFor(BattleEventKind.Tell, chief, null, ParryRules.TellTicks, ChiefKits.VeinsName));
                return;
            }
            state.VeinsTicks = ChiefKits.VeinsTicks;
            Emit(EventFor(BattleEventKind.Tell, chief, null, ChiefKits.VeinsTicks, ChiefKits.VeinsName));
        }

        // The cut: a parry turns it aside and your veins stay shut; a stale Hold costs your nerve and they open anyway;
        // otherwise they open and your parry chain starts over.
        private void CutVeins(int side, BattleUnit chief)
        {
            SideState state = _sides[side];
            ClearBeat(side, BeatSlot.Veins);
            BeatMet met = MeetBeat(1 - side);
            if (met == BeatMet.Parry)
            {
                ClearVeins(state);
                Emit(EventFor(BattleEventKind.Land, chief, null, 0, ChiefKits.VeinsName + "." + ChiefKits.Parried));
                RiteLanded(side, chief);
                if (!Stopped) ParriedBeat(side, chief, ChiefKits.VeinsName, ChiefKits.ParryNerve, FightRules.ParryPoints, true);
                return;
            }
            if (met == BeatMet.Block) BlockedBeat(side, chief, ChiefKits.ParryNerve);
            else LandedBeat(side);
            if (Stopped) return;
            state.VeinsTicks = ChiefKits.VeinsTicks;
        }

        // A second of bleeding on this fighter while the Stoneveil chief facing them has their veins open takes twice as much,
        // and as much again while their side holds.
        private int VeinsBleed(BattleUnit bleeder, int amount)
        {
            int foe = 1 - bleeder.Side;
            if (Kit(foe) != ChiefKit.Stoneveil || _sides[foe].VeinsTicks <= 0) return amount;
            _sides[foe].VeinsBled = true;
            int doubled = amount * ChiefKits.VeinsPercent / 100;
            return _sides[bleeder.Side].HoldTicks > 0 ? Math.Max(1, doubled * ChiefKits.PressPercent / 100) : doubled;
        }

        // The same through the damage layers (BattleSimulation.Layers.cs): Open Veins in the kit layer, and holding through
        // them adds to it.
        private void VeinsHit(BattleUnit bleeder, Hit hit)
        {
            int foe = 1 - bleeder.Side;
            if (Kit(foe) != ChiefKit.Stoneveil || _sides[foe].VeinsTicks <= 0) return;
            _sides[foe].VeinsBled = true;
            hit.Add(HitLayer.Kit, ChiefKits.VeinsPercent - 100);
            if (_sides[bleeder.Side].HoldTicks > 0) hit.Add(HitLayer.Kit, ChiefKits.PressPercent - 100);
        }

        // Open Veins runs on, noting a Hold from the other side. When it ends, how it was met: a wound stitched shut during it
        // (a Heal); else, if anyone bled under it, a Hold that pressed (a Brace) or nothing (you ate it); nobody bled, nothing
        // came of it.
        private void AdvanceVeins(int side)
        {
            SideState state = _sides[side];
            if (state.VeinsCut > 0)
            {
                BattleUnit cutter = KitChief(side);
                if (cutter == null || !CanMove(cutter))
                {
                    ClearVeins(state);
                    return;
                }
                if (--state.VeinsCut > 0) return;
                int hold = HoldBeat(side, cutter, BeatSlot.Veins);
                if (hold > 0) state.VeinsCut = hold;
                else CutVeins(side, cutter);
                return;
            }
            if (state.VeinsTicks <= 0) return;
            BattleUnit chief = KitChief(side);
            if (chief == null)
            {
                ClearVeins(state);
                return;
            }
            int enemy = 1 - side;
            if (_sides[enemy].HoldTicks > 0) state.VeinsHeld = true;
            if (--state.VeinsTicks > 0) return;
            bool stitched = state.VeinsStitched, bled = state.VeinsBled, held = state.VeinsHeld;
            ClearVeins(state);
            string how = stitched ? ChiefKits.Healed : !bled ? ChiefKits.Clear : held ? ChiefKits.Braced : ChiefKits.Full;
            Emit(EventFor(BattleEventKind.Land, chief, null, 0, ChiefKits.VeinsName + "." + how));
            RiteLanded(side, chief);
            if (Stopped) return;
            if (stitched) Answered(enemy, ChiefAnswer.Heal, chief, ChiefKits.HealPoints, ChiefKits.Name(ChiefAnswer.Heal));
            else if (bled && held) Answer(enemy, ChiefAnswer.Brace, chief, 0);
            else if (bled) Eaten(enemy, chief);
        }

        private static void ClearVeins(SideState state)
        {
            state.VeinsCut = 0;
            state.Beats[(int)BeatSlot.Veins] = 0;
            state.VeinsTicks = 0;
            state.VeinsStitched = false;
            state.VeinsBled = false;
            state.VeinsHeld = false;
        }

        // ------------------------------------------------------------ Transfusion

        // Every TransfusionEveryTicks he channels into the other side's most hurt fighter a move can fall on, while he can move
        // and is not chanting his Blood Call.
        private void TryStartTransfusion(int side)
        {
            SideState state = _sides[side];
            if (!HasMove(side, ChiefMove.Transfusion) || state.DrawTicks > 0 || state.DrawLatch > 0 || _tick < state.DrawNext) return;
            BattleUnit chief = KitChief(side);
            BattleUnit target = chief != null && CanMove(chief) && state.Windup <= 0 ? MostHurtMark(1 - side) : null;
            if (target == null)
            {
                state.DrawNext = _tick + 1;
                return;
            }
            state.DrawTargetId = target.Id;
            state.DrawDrawn = 0;
            state.DrawNext = _tick + ChiefKits.TransfusionEveryTicks;
            // Under the parry at the center the channel latches ParryRules.TellTicks after the tell, and the latch is its beat:
            // the Tell's amount is the ticks until it (BattleSimulation.Parry.cs).
            if (_parryOn)
            {
                state.DrawLatch = ParryRules.TellTicks;
                Emit(EventFor(BattleEventKind.Tell, chief, target, ParryRules.TellTicks, ChiefKits.TransfusionName));
                return;
            }
            state.DrawTicks = ChiefKits.TransfusionTicks;
            Emit(EventFor(BattleEventKind.Tell, chief, target, ChiefKits.TransfusionTicks, ChiefKits.TransfusionName));
        }

        // The latch: a parry tears it loose; a stale Hold costs your nerve and it latches anyway; otherwise it latches and your
        // parry chain starts over.
        private void LatchTransfusion(int side, BattleUnit chief, BattleUnit target)
        {
            SideState state = _sides[side];
            ClearBeat(side, BeatSlot.Draw);
            BeatMet met = MeetBeat(1 - side);
            if (met == BeatMet.Parry)
            {
                ClearTransfusion(state);
                Emit(EventFor(BattleEventKind.Land, chief, target, 0, ChiefKits.TransfusionName + "." + ChiefKits.Parried));
                RiteLanded(side, chief);
                if (!Stopped) ParriedBeat(side, chief, ChiefKits.TransfusionName, ChiefKits.ParryNerve, FightRules.ParryPoints, true);
                return;
            }
            if (met == BeatMet.Block) BlockedBeat(side, chief, ChiefKits.ParryNerve);
            else LandedBeat(side);
            if (Stopped) return;
            state.DrawTicks = ChiefKits.TransfusionTicks;
        }

        // The channel runs on. Focus Fire on him breaks it (a Break). At each second of it he draws, unless your Warden covers
        // the one he draws from, which blocks it and ends it (a Guard). All of it drawn, or the one he draws from fallen once he
        // has drawn, you ate it. Gone with nothing to show if he is stunned, held or kneels, or his mark falls before a draw.
        private void AdvanceTransfusion(int side)
        {
            SideState state = _sides[side];
            if (state.DrawLatch > 0)
            {
                BattleUnit drawer = KitChief(side);
                BattleUnit mark = FindById(state.DrawTargetId);
                if (drawer == null || !CanMove(drawer) || mark == null || !mark.Alive)
                {
                    ClearTransfusion(state);
                    return;
                }
                if (--state.DrawLatch > 0) return;
                int hold = HoldBeat(side, drawer, BeatSlot.Draw);
                if (hold > 0) state.DrawLatch = hold;
                else LatchTransfusion(side, drawer, mark);
                return;
            }
            if (state.DrawTicks <= 0) return;
            int enemy = 1 - side;
            BattleUnit chief = KitChief(side);
            BattleUnit target = FindById(state.DrawTargetId);
            if (chief == null || !CanMove(chief) || target == null || (!target.Alive && state.DrawDrawn == 0))
            {
                ClearTransfusion(state);
                return;
            }
            string name = ChiefKits.TransfusionName + ".";
            if (IsFocusing(enemy, chief))
            {
                ClearTransfusion(state);
                Emit(EventFor(BattleEventKind.Land, chief, target, 0, name + ChiefKits.Broken));
                RiteLanded(side, chief);
                if (!Stopped) Answered(enemy, ChiefAnswer.Break, chief, FightRules.InterruptPoints, FightRules.InterruptReason);
                return;
            }
            if (target.Alive)
            {
                state.DrawTicks--;
                if ((ChiefKits.TransfusionTicks - state.DrawTicks) % SimClock.TicksPerSecond == 0)
                {
                    BattleUnit warden = GuardingWarden(target);
                    if (warden != null)
                    {
                        ClearTransfusion(state);
                        Emit(EventFor(BattleEventKind.Land, chief, target, 0, name + ChiefKits.Guarded));
                        Emit(EventFor(BattleEventKind.Guard, warden, target, 0, string.Empty));
                        RiteLanded(side, chief);
                        if (!Stopped) Answered(enemy, ChiefAnswer.Guard, chief, ChiefKits.GuardPoints, ChiefKits.Name(ChiefAnswer.Guard));
                        return;
                    }
                    state.DrawDrawn += Drain(chief, side, target, ChiefKits.TransfusionPercent, ChiefKits.TransfusionDetail, true);
                    if (Stopped) return;
                }
                if (state.DrawTicks > 0 && target.Alive) return;
            }
            int drawn = state.DrawDrawn;
            ClearTransfusion(state);
            Emit(EventFor(BattleEventKind.Land, chief, target, drawn, name + ChiefKits.Full));
            RiteLanded(side, chief);
            if (!Stopped) Eaten(enemy, chief);
            CheckWipe();
        }

        // The ally of this Warden the Stoneveil chief's Transfusion draws from, not himself, or null.
        private BattleUnit DrawnAlly(BattleUnit warden)
        {
            int foe = 1 - warden.Side;
            SideState his = _sides[foe];
            if (Kit(foe) != ChiefKit.Stoneveil || (his.DrawTicks <= 0 && his.DrawLatch <= 0)) return null;
            BattleUnit drawn = FindById(his.DrawTargetId);
            return drawn != null && drawn.Alive && drawn.Side == warden.Side && !ReferenceEquals(drawn, warden) ? drawn : null;
        }

        private static void ClearTransfusion(SideState state)
        {
            state.DrawLatch = 0;
            state.Beats[(int)BeatSlot.Draw] = 0;
            state.DrawTicks = 0;
            state.DrawTargetId = string.Empty;
            state.DrawDrawn = 0;
        }

        // ------------------------------------------------------------ Blood Price

        // An order the other side gave feeds his rite a drop. At ChiefKits.PriceOrders his next move comes at once.
        private void FeedPrice(int side)
        {
            if (Stopped || !PriceOn(side)) return;
            BattleUnit chief = KitChief(side);
            if (chief == null) return;
            SideState state = _sides[side];
            if (++state.PriceDrops < ChiefKits.PriceOrders) return;
            state.PriceDrops = 0;
            Emit(EventFor(BattleEventKind.Land, chief, null, ChiefKits.PriceOrders, ChiefKits.BloodPriceName + "." + ChiefKits.Full));
            long best = long.MaxValue;
            int pick = -1;
            for (int k = 0; k < RiteMoves; k++)
            {
                long due = RiteDue(side, k);
                if (due >= best) continue;
                best = due;
                pick = k;
            }
            if (pick < 0 || best <= _tick) return;
            switch (pick)
            {
                case 0: state.NextAutoEclipse = _tick; break;
                case 1: state.BasinNext = _tick; break;
                case 2: state.LeechNext = _tick; break;
                case 3: state.VeinsNext = _tick; break;
                default: state.DrawNext = _tick; break;
            }
        }

        // When each of his moves is next due, in his pool's order (Blood Call, Basin, Leeches, Open Veins, Transfusion), or
        // never for one he did not take or that runs now.
        private long RiteDue(int side, int k)
        {
            SideState state = _sides[side];
            switch (k)
            {
                case 0:
                    return HasMove(side, ChiefMove.BloodCall) && _rules[side].AutoEclipseTicks > 0 && state.Windup <= 0
                        ? state.NextAutoEclipse : long.MaxValue;
                case 1: return HasMove(side, ChiefMove.Basin) && state.BasinTicks <= 0 ? state.BasinNext : long.MaxValue;
                case 2:
                    return HasMove(side, ChiefMove.Leeches) && state.LeechTicks <= 0 && state.LeechLeap <= 0 ? state.LeechNext : long.MaxValue;
                case 3:
                    return HasMove(side, ChiefMove.OpenVeins) && state.VeinsTicks <= 0 && state.VeinsCut <= 0 ? state.VeinsNext : long.MaxValue;
                default:
                    return HasMove(side, ChiefMove.Transfusion) && state.DrawTicks <= 0 && state.DrawLatch <= 0 ? state.DrawNext : long.MaxValue;
            }
        }

        // ------------------------------------------------------------ the Red Rite and Clean Hands

        // How long a wound left on this fighter bleeds: twice as long under the Red Rite of a Stoneveil chief facing them.
        private int WoundTicks(BattleUnit victim)
        {
            return RedRite(1 - victim.Side) ? BleedRules.Ticks * ChiefKits.RedRitePercent / 100 : BleedRules.Ticks;
        }

        // Whether a side's blades and barbs still leave wounds: not the Stoneveil chief's band once your Clean Hands broke his
        // rite.
        private bool RiteWounds(int side)
        {
            return side < 0 || side > 1 || Kit(side) != ChiefKit.Stoneveil || !_sides[side].CleanHands;
        }

        // One of his moves landed. With nobody of the other side bleeding it counts toward Clean Hands; with anyone bleeding
        // that count starts over. At ChiefKits.CleanHandsLands the rite fails: he is stunned, and his band's blades leave no
        // more wounds this fight. It comes before the answer's chunk of his nerve, so it shows even when that chunk breaks him.
        private void RiteLanded(int side, BattleUnit chief)
        {
            SideState state = _sides[side];
            if (Stopped || state.CleanHands) return;
            int enemy = 1 - side;
            SideState theirs = _sides[enemy];
            if (BleedingCount(enemy) > 0)
            {
                theirs.Chain = 0;
                return;
            }
            if (++theirs.Chain < ChiefKits.CleanHandsLands) return;
            theirs.Chain = 0;
            state.CleanHands = true;
            Emit(new BattleEvent(_tick, BattleEventKind.Combo, enemy, string.Empty, -1, chief.Id, (int)chief.Class,
                ChiefKits.CleanHandsStunTicks, ChiefKits.CleanHands));
            Earn(enemy, FightRules.ComboPoints, FightRules.ComboReason);
            if (!chief.Alive) return;
            chief.Statuses.Apply(StatusKind.Stun, ChiefKits.CleanHandsStunTicks);
            chief.AimTicks = 0;
            chief.AimTargetId = string.Empty;
            ClearTransfusion(state);
        }

        // ------------------------------------------------------------ the kneel and the checksum

        private void ClearRite(int side)
        {
            SideState state = _sides[side];
            ClearBasin(state);
            ClearLeeches(state);
            ClearVeins(state);
            ClearTransfusion(state);
            state.DrinkTicks = 0;
            state.ChantStitches = 0;
        }

        // His kit's state for the checksum, written only in his fights.
        private static void AppendRite(SnapshotBuilder b, SideState state)
        {
            b.Field("chantStitches", state.ChantStitches);
            b.Field("basin", state.BasinTicks);
            b.Field("basinNext", state.BasinNext);
            b.Field("basinCarrier", state.BasinCarrierId);
            b.Field("basinFill", state.BasinFill);
            b.Field("drink", state.DrinkTicks);
            b.Field("leech", state.LeechTicks);
            b.Field("leechNext", state.LeechNext);
            b.Field("leechTarget", state.LeechTargetId);
            b.Field("leechDrunk", state.LeechDrunk);
            b.Field("veins", state.VeinsTicks);
            b.Field("veinsNext", state.VeinsNext);
            b.Field("veinsStitched", state.VeinsStitched);
            b.Field("veinsBled", state.VeinsBled);
            b.Field("veinsHeld", state.VeinsHeld);
            b.Field("draw", state.DrawTicks);
            b.Field("drawNext", state.DrawNext);
            b.Field("drawTarget", state.DrawTargetId);
            b.Field("drawDrawn", state.DrawDrawn);
            b.Field("price", state.PriceDrops);
            b.Field("cleanHands", state.CleanHands);
        }
    }
}
