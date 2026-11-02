using System;
using System.Collections.Generic;

namespace KindredSiege.Sim.Battle
{
    /// The Skullpeak chief's kit (Jacob, 22 Oct 2026: "go, build the Skullpeak chief and the Bloodridge extras in the sim";
    /// DESIGN_LOCKS "The Bloodridge chief finished and the Skullpeak chief's kit"): a slow grind behind walls. Her Steel Storm
    /// on its own clock, which your Trapper can leave half built; her Hook, Rifle Line, Iron Advance and Overclock; her
    /// Lockdown at half health; Breach, her hidden chain; and, with FightRules.ChiefTactics, her band's jobs. ChiefKits holds
    /// the numbers. Only fights where a side's kit is hers come here. Her Hook picks its mark from the fight's seeded stream;
    /// nothing else here draws.
    ///
    /// Claude's calls while building, Jacob's to change: she throws the Hook herself (she is a Trapper), so a parry staggers
    /// her; her Wardens keep fighting while they lock shields, so one of them can spring your trap and be snared; her Steel
    /// Storm and her Lockdown wall need a Warden of hers standing; a Rifle Line's shooters are her ranged fighters but her
    /// Medic, her band first, then herself; Iron Advance marches only while her front row stands; Overclock needs her
    /// Medic; her Shieldbearer is her band's first Warden and her Spotter its first Marksman.
    public sealed partial class BattleSimulation
    {
        // ------------------------------------------------------------ questions the screen asks

        /// Ticks left before her chain bites, or 0, and on whom it glints.
        public int HookTellTicks(int side)
        {
            return Kit(side) == ChiefKit.Skullpeak ? _sides[side].HookTell : 0;
        }

        public string HookTargetId(int side)
        {
            return HookTellTicks(side) > 0 ? _sides[side].HookTargetId : string.Empty;
        }

        /// True while her chain glints and a Hold timed to its last FightRules.PerfectHoldTicks would snap it.
        public bool HookParryable(int side)
        {
            return HookTellTicks(side) > 0 && _fight.PerfectHold;
        }

        /// True once your Breach took out her steel leg: she cannot Hook again this fight.
        public bool HookGone(int side)
        {
            return Kit(side) == ChiefKit.Skullpeak && _sides[side].HookGone;
        }

        /// Ticks left before her Rifle Line's volley, or 0, and its mark. Her shooters are her units with RifleTicks above 0.
        public int RifleLineTicks(int side)
        {
            return Kit(side) == ChiefKit.Skullpeak ? _sides[side].RifleTicks : 0;
        }

        public string RifleTargetId(int side)
        {
            return RifleLineTicks(side) > 0 ? _sides[side].RifleTargetId : string.Empty;
        }

        /// Ticks her Iron Advance still marches, or 0. It crushes the other side's front row each second.
        public int AdvanceTicksLeft(int side)
        {
            return Kit(side) == ChiefKit.Skullpeak ? _sides[side].AdvanceTicks : 0;
        }

        /// Ticks left of her Medic's Overclock jab, or 0, and which Medic jabs.
        public int JabTicksLeft(int side)
        {
            return Kit(side) == ChiefKit.Skullpeak ? _sides[side].JabTicks : 0;
        }

        public string JabMedicId(int side)
        {
            return JabTicksLeft(side) > 0 ? _sides[side].JabMedicId : string.Empty;
        }

        /// Ticks her band still acts twice as fast, or 0.
        public int OverclockTicksLeft(int side)
        {
            return Kit(side) == ChiefKit.Skullpeak ? _sides[side].OverclockTicks : 0;
        }

        /// True from her Lockdown, her second phase, on.
        public bool InLockdown(int side)
        {
            return Kit(side) == ChiefKit.Skullpeak && _sides[side].Lockdown;
        }

        /// In her Lockdown: ticks the other side's blows still land in full after a smash, and ticks until her Wardens have
        /// the wall whole again. Until it is whole an Unleash finds nothing to smash. 0 otherwise.
        public int WallOpenTicks(int side)
        {
            return InLockdown(side) ? _sides[side].OpenTicks : 0;
        }

        public int WallRebuildTicks(int side)
        {
            return InLockdown(side) ? _sides[side].RebuildTicks : 0;
        }

        /// True when an Unleash from the other side would smash a wall on this side now: a standing Steel Storm wall, or her
        /// Lockdown wall while it is whole.
        public bool WallSmashable(int side)
        {
            if (side < 0 || side > 1) return false;
            return InLockdown(side) ? LockdownWhole(side) : _sides[side].WallTicks > 0;
        }

        /// What the other side's blows do on this side right now, in percent: 100 with no wall up, a tenth through a Steel
        /// Storm wall or her Lockdown wall, half through her half built one.
        public int WallPercentNow(int side)
        {
            if (side < 0 || side > 1) return 100;
            if (_spellsOn && _sides[side].WallTicks > 0) return WallPercentOf(side);
            if (_kitsTwoOn && LockdownHolds(side)) return ChiefSpells.WallDamagePercent;
            return 100;
        }

        /// The job this fighter holds in a chief's band (FightRules.ChiefTactics), or BandJob.None.
        public BandJob JobOf(BattleUnit unit)
        {
            if (!_tacticsOn || unit == null) return BandJob.None;
            SideState state = _sides[unit.Side];
            if (string.Equals(unit.Id, state.ShieldbearerId, StringComparison.Ordinal)) return BandJob.Shieldbearer;
            if (string.Equals(unit.Id, state.SpotterId, StringComparison.Ordinal)) return BandJob.Spotter;
            return BandJob.None;
        }

        /// Who holds this job in a side's band, or empty.
        public string JobHolderId(int side, BandJob job)
        {
            if (!_tacticsOn || side < 0 || side > 1) return string.Empty;
            switch (job)
            {
                case BandJob.Shieldbearer: return _sides[side].ShieldbearerId;
                case BandJob.Spotter: return _sides[side].SpotterId;
                default: return string.Empty;
            }
        }

        /// True once whoever held this job in a side's band is dead: its move is worse from then on.
        public bool JobDown(int side, BandJob job)
        {
            string id = JobHolderId(side, job);
            if (id.Length == 0) return false;
            BattleUnit holder = FindById(id);
            return holder != null && !holder.Alive;
        }

        // ------------------------------------------------------------ setup

        // Her moves' first clocks, and, with the tactical pieces, her band's jobs: its first Warden is her Shieldbearer and its
        // first Marksman her Spotter, among those in the fight from the start.
        private void StartSkullpeakClocks(int side)
        {
            if (Kit(side) != ChiefKit.Skullpeak) return;
            SideState state = _sides[side];
            if (HasMove(side, ChiefMove.Hook)) state.HookNext = ChiefKits.HookEveryTicks;
            if (HasMove(side, ChiefMove.RifleLine)) state.RifleNext = ChiefKits.RifleEveryTicks;
            if (HasMove(side, ChiefMove.IronAdvance)) state.AdvanceNext = ChiefKits.AdvanceEveryTicks;
            if (HasMove(side, ChiefMove.Overclock)) state.OverclockNext = ChiefKits.OverclockEveryTicks;
            if (!_tacticsOn) return;
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (!u.Alive || u.Side != side || u.Role != UnitRole.Fighter) continue;
                if (u.Class == UnitClass.Warden && state.ShieldbearerId.Length == 0) state.ShieldbearerId = u.Id;
                if (u.Class == UnitClass.Marksman && state.SpotterId.Length == 0) state.SpotterId = u.Id;
            }
        }

        private BattleUnit FirstLiving(int side, UnitClass unitClass)
        {
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (u.Alive && u.Side == side && u.Class == unitClass) return u;
            }
            return null;
        }

        // A snare holds one of her Wardens.
        private bool WardenSnared(int side)
        {
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (u.Alive && u.Side == side && u.Class == UnitClass.Warden && u.SnareTicks > 0) return true;
            }
            return false;
        }

        // ------------------------------------------------------------ her walls

        // An Unleash on her wall: a standing Steel Storm wall, or her Lockdown wall while it is whole. It is smashed (a big
        // chunk of her nerve, a point back, and a Smash for your Glory); it counts toward her Breach when it came in the wall's
        // first second, else that count starts over; and in Lockdown her wall is open for LockdownOpenTicks (twice that once
        // her Shieldbearer is dead) and whole again LockdownRebuildTicks after the smash.
        private void SmashKitWall(int side)
        {
            int enemy = 1 - side;
            SideState her = _sides[enemy];
            if (her.Lockdown)
            {
                if (!LockdownWhole(enemy)) return;
                her.OpenTicks = ChiefKits.LockdownOpenTicks * (JobDown(enemy, BandJob.Shieldbearer) ? ChiefKits.JobSlowPercent : 100) / 100;
                her.RebuildTicks = ChiefKits.LockdownRebuildTicks;
            }
            else
            {
                if (her.WallTicks <= 0) return;
                her.WallTicks = 0;
                her.WallPercent = 0;
            }
            SideState mine = _sides[side];
            mine.Chain = _tick - her.WallUpTick <= ChiefKits.QuickSmashTicks ? mine.Chain + 1 : 0;
            Emit(new BattleEvent(_tick, BattleEventKind.Wall, enemy, string.Empty, -1, string.Empty, -1,
                ChiefSpells.WallSmashNerve, ChiefSpells.WallSmashed));
            BattleUnit chief = KitChief(enemy);
            if (chief != null) Answered(side, ChiefAnswer.Smash, chief, ChiefSpells.SmashPoints, ChiefSpells.SmashReason);
            else Earn(side, ChiefSpells.SmashPoints, ChiefSpells.SmashReason);
            if (!_finished) DrainNerve(enemy, ChiefSpells.WallSmashNerve, side);
        }

        // Her second phase: she steps behind a wall that never drops. A Steel Storm wall, or her Wardens' lock for one, gives
        // way to it. With no Warden of hers standing it is down from the start.
        private void EnterLockdown(int side, BattleUnit chief)
        {
            SideState state = _sides[side];
            state.Lockdown = true;
            state.OpenTicks = 0;
            state.RebuildTicks = 0;
            state.WallTicks = 0;
            state.WallPercent = 0;
            state.WallUpTick = _tick;
            if (state.Windup > 0 && string.Equals(state.CasterId, chief.Id, StringComparison.Ordinal)) ClearWindup(state);
            Emit(new BattleEvent(_tick, BattleEventKind.Wall, side, chief.Id, (int)chief.Class, string.Empty, -1, 0, ChiefKits.WallLockdown));
            if (FirstLivingWarden(side) == null)
                Emit(new BattleEvent(_tick, BattleEventKind.Wall, side, string.Empty, -1, string.Empty, -1, 0, ChiefSpells.WallFell));
        }

        // Her Lockdown wall holds the other side's blows to a tenth: no window open after a smash, and a Warden of hers stands.
        private bool LockdownHolds(int side)
        {
            SideState state = _sides[side];
            return state.Lockdown && state.OpenTicks <= 0 && FirstLivingWarden(side) != null;
        }

        // Her Lockdown wall is whole, so an Unleash can smash it.
        private bool LockdownWhole(int side)
        {
            return LockdownHolds(side) && _sides[side].RebuildTicks <= 0;
        }

        // Her Lockdown's clocks: the window after a smash closes, and her Wardens have the wall whole again, while one stands.
        private void CountDownLockdown()
        {
            for (int s = 0; s < 2; s++)
            {
                SideState state = _sides[s];
                if (!state.Lockdown) continue;
                if (state.OpenTicks > 0) state.OpenTicks--;
                if (state.RebuildTicks <= 0) continue;
                state.RebuildTicks--;
                if (state.RebuildTicks > 0 || FirstLivingWarden(s) == null) continue;
                state.WallUpTick = _tick;
                Emit(new BattleEvent(_tick, BattleEventKind.Wall, s, string.Empty, -1, string.Empty, -1, 0, ChiefKits.WallRebuilt));
            }
        }

        // A Warden fell: if he was her last, her Lockdown wall is down for good.
        private void WardenFell(BattleUnit victim)
        {
            if (!_sides[victim.Side].Lockdown || FirstLivingWarden(victim.Side) != null) return;
            Emit(new BattleEvent(_tick, BattleEventKind.Wall, victim.Side, string.Empty, -1, string.Empty, -1, 0, ChiefSpells.WallFell));
        }

        // ------------------------------------------------------------ Hook

        // Every HookEveryTicks her chain glints at one of the other side's back row, when she is free: not winding up her Steel
        // Storm, not aiming. Never once your Breach took her steel leg.
        private void TryStartHook(int side)
        {
            SideState state = _sides[side];
            if (!HasMove(side, ChiefMove.Hook) || state.HookGone || state.HookTell > 0 || _tick < state.HookNext) return;
            BattleUnit chief = KitChief(side);
            BattleUnit target = chief != null && CanMove(chief) && state.Windup <= 0 && chief.RifleTicks <= 0 ? HookMark(side) : null;
            if (target == null)
            {
                state.HookNext = _tick + 1;
                return;
            }
            state.HookTell = ChiefKits.HookTellTicks;
            state.HookTargetId = target.Id;
            state.HookWaited = false;
            state.HookNext = _tick + ChiefKits.HookEveryTicks;
            Emit(EventFor(BattleEventKind.Tell, chief, target, ChiefKits.HookTellTicks, ChiefKits.HookName));
        }

        // One of their back row a move can fall on and not dragged already, picked from the fight's seeded stream; null when
        // their back row has none.
        private BattleUnit HookMark(int side)
        {
            int enemy = 1 - side;
            var pool = new List<BattleUnit>();
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (u.Row == Row.Back && u.HookTicks <= 0 && Markable(u, enemy)) pool.Add(u);
            }
            return pool.Count == 0 ? null : pool[_rng.Next(pool.Count)];
        }

        // The chain glints on; then it bites, unless her tribe learned to wait out a fresh Hold and has not waited yet this
        // throw (as his Cleave does). Gone if she is stunned, held or kneels first.
        private void AdvanceHook(int side)
        {
            SideState state = _sides[side];
            if (state.HookTell <= 0) return;
            BattleUnit chief = KitChief(side);
            if (chief == null || !CanMove(chief))
            {
                ClearHook(state);
                return;
            }
            state.HookTell--;
            if (state.HookTell > 0) return;
            // Under the parry at the center the bite is its beat (BattleSimulation.Parry.cs).
            if (_parryOn)
            {
                int hold = HoldBeat(side, chief, BeatSlot.Hook);
                if (hold > 0) state.HookTell = hold;
                else BiteHook(side, chief);
                return;
            }
            if (!state.HookWaited && WaitsOutParry(side))
            {
                int wait = FreshHoldWait(1 - side);
                if (wait > 0)
                {
                    state.HookWaited = true;
                    state.HookTell = wait;
                    Emit(EventFor(BattleEventKind.Reaction, chief, null, wait, WaitOutDetail));
                    return;
                }
            }
            BiteHook(side, chief);
        }

        // The chain bites: a Hold started in the parry window snaps it (she staggers, and two quick smashes before make it her
        // Breach); a Warden covering its mark anchors them; otherwise it drags them in front of her shields for HookTicks.
        private void BiteHook(int side, BattleUnit chief)
        {
            SideState state = _sides[side];
            int enemy = 1 - side;
            SideState theirs = _sides[enemy];
            BattleUnit target = FindById(state.HookTargetId);
            ClearHook(state);
            if (target == null || !target.Alive) return;
            string name = ChiefKits.HookName + ".";

            // Under the parry at the center the bite is a duel for nerve (BattleSimulation.Parry.cs).
            BeatMet met = _parryOn ? MeetBeat(enemy) : BeatMet.Open;
            if (met == BeatMet.Parry)
            {
                Emit(EventFor(BattleEventKind.Land, chief, target, 0, name + ChiefKits.Parried));
                state.HookNext += FightRules.StaggerTicks;
                int cracked = ParryLands(side, chief, ChiefKits.HookName, ChiefKits.ParryNerve, FightRules.ParryPoints);
                TryBreach(side, chief);
                ParryCracks(side, chief, cracked, FightRules.ParryPoints, true);
                return;
            }
            if (!_parryOn && _fight.PerfectHold && theirs.HoldTicks > 0 && theirs.LastHoldTick >= 0 &&
                _tick - theirs.LastHoldTick <= ParryWindowTicks)
            {
                Emit(EventFor(BattleEventKind.Land, chief, target, 0, name + ChiefKits.Parried));
                Emit(new BattleEvent(_tick, BattleEventKind.Parry, enemy, string.Empty, -1, chief.Id, (int)chief.Class,
                    FightRules.StaggerTicks, ChiefKits.HookName));
                chief.Cooldown += FightRules.StaggerTicks;
                state.HookNext += FightRules.StaggerTicks;
                theirs.HoldTicks = 0;
                theirs.LastParryTick = _tick;
                theirs.ParriedCasterId = chief.Id;
                Earn(enemy, FightRules.ParryPoints, FightRules.ParryReason);
                SayOnParry(chief);
                TryBreach(side, chief);
                Answer(enemy, ChiefAnswer.Parry, chief, FightRules.ParryPoints);
                return;
            }

            BattleUnit warden = GuardingWarden(target);
            if (warden != null)
            {
                Emit(EventFor(BattleEventKind.Land, chief, target, 0, name + ChiefKits.Guarded));
                Emit(EventFor(BattleEventKind.Guard, warden, target, 0, string.Empty));
                Answered(enemy, ChiefAnswer.Guard, chief, ChiefKits.GuardPoints, ChiefKits.Name(ChiefAnswer.Guard));
                return;
            }

            // A drag soaks nothing: a stale Hold only costs your nerve, and with no Hold your Glory drops and your chain starts over.
            if (_parryOn)
            {
                if (met == BeatMet.Block) BlockedBeat(side, chief, ChiefKits.ParryNerve);
                else LandedBeat(side);
                if (Stopped) return;
            }
            target.HookTicks = ChiefKits.HookTicks;
            Emit(EventFor(BattleEventKind.Land, chief, target, 0, name + ChiefKits.Full));
            if (_parryOn && met == BeatMet.Open) Eaten(enemy, chief);
        }

        // Breach, her hidden chain: BreachSmashes of her walls smashed in their first second in a row, then this parry, with a
        // Skirmisher of yours standing. Your Skirmisher runs the chain back and takes out her steel leg: she is down for
        // BreachStunTicks and cannot Hook again this fight.
        private void TryBreach(int side, BattleUnit chief)
        {
            int enemy = 1 - side;
            SideState theirs = _sides[enemy];
            if (theirs.Chain < ChiefKits.BreachSmashes) return;
            BattleUnit runner = FirstLiving(enemy, UnitClass.Skirmisher);
            if (runner == null) return;
            theirs.Chain = 0;
            _sides[side].HookGone = true;
            Emit(new BattleEvent(_tick, BattleEventKind.Combo, enemy, runner.Id, (int)runner.Class, chief.Id, (int)chief.Class,
                ChiefKits.BreachStunTicks, ChiefKits.Breach));
            Earn(enemy, FightRules.ComboPoints, FightRules.ComboReason);
            chief.Statuses.Apply(StatusKind.Stun, ChiefKits.BreachStunTicks);
            chief.AimTicks = 0;
            chief.AimTargetId = string.Empty;
        }

        private static void ClearHook(SideState state)
        {
            state.HookTell = 0;
            state.HookTargetId = string.Empty;
            state.HookWaited = false;
            state.Beats[(int)BeatSlot.Hook] = 0;
        }

        // The ally of this Warden her chain glints at, or one it dragged: the first such, not himself.
        private BattleUnit HookedAlly(BattleUnit warden)
        {
            SideState her = _sides[1 - warden.Side];
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (!u.Alive || u.Side != warden.Side || ReferenceEquals(u, warden)) continue;
                if (u.HookTicks > 0 || (her.HookTell > 0 && string.Equals(her.HookTargetId, u.Id, StringComparison.Ordinal))) return u;
            }
            return null;
        }

        // For her side's blows: one of the other side her Hook dragged in front of her shields, or null. Null for any other side.
        private BattleUnit DraggedFoe(int side)
        {
            if (Kit(side) != ChiefKit.Skullpeak) return null;
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (u.Alive && u.Side != side && u.HookTicks > 0) return u;
            }
            return null;
        }

        // ------------------------------------------------------------ Rifle Line

        // Every RifleEveryTicks up to RifleShots of her shooters aim at the other side's most hurt fighter, for RifleAimTicks
        // (twice that once her Spotter is dead). They do nothing else meanwhile.
        private void TryStartRifle(int side)
        {
            SideState state = _sides[side];
            if (!HasMove(side, ChiefMove.RifleLine) || state.RifleTicks > 0 || _tick < state.RifleNext) return;
            BattleUnit chief = KitChief(side);
            List<BattleUnit> shooters = chief != null && CanMove(chief) ? Shooters(side, chief) : new List<BattleUnit>();
            BattleUnit target = shooters.Count > 0 ? MostHurtMark(1 - side) : null;
            if (target == null)
            {
                state.RifleNext = _tick + 1;
                return;
            }
            int aim = ChiefKits.RifleAimTicks * (JobDown(side, BandJob.Spotter) ? ChiefKits.JobSlowPercent : 100) / 100;
            state.RifleTicks = aim;
            state.RifleTargetId = target.Id;
            state.RifleShooters = shooters.Count;
            state.RifleNext = _tick + ChiefKits.RifleEveryTicks;
            Emit(EventFor(BattleEventKind.Tell, chief, target, aim, ChiefKits.RifleName));
            for (int i = 0; i < shooters.Count; i++)
            {
                shooters[i].RifleTicks = aim;
                Emit(EventFor(BattleEventKind.Aim, shooters[i], target, aim, ChiefKits.RifleDetail));
            }
        }

        // Up to RifleShots of her free ranged fighters, her Medic aside: her band first in setup order, then herself if she is
        // free of her chain and her Steel Storm.
        private List<BattleUnit> Shooters(int side, BattleUnit chief)
        {
            var list = new List<BattleUnit>();
            for (int i = 0; i < _units.Count && list.Count < ChiefKits.RifleShots; i++)
            {
                BattleUnit u = _units[i];
                if (u.Side == side && u.Role == UnitRole.Fighter && FreeShooter(u)) list.Add(u);
            }
            SideState state = _sides[side];
            if (list.Count < ChiefKits.RifleShots && FreeShooter(chief) && state.HookTell <= 0 && state.Windup <= 0) list.Add(chief);
            return list;
        }

        private bool FreeShooter(BattleUnit u)
        {
            return u.Alive && u.Stats.Ranged && u.Class != UnitClass.Medic && u.SnareTicks <= 0 && u.PanicTicks <= 0 && u.AimTicks <= 0 &&
                u.RifleTicks <= 0 && u.StunTicks <= 0 && !(_heavyOn && IsSwingingHeavy(u));
        }

        // The other side's most hurt fighter a move can fall on; ties go to the lower index.
        private BattleUnit MostHurtMark(int side)
        {
            BattleUnit best = null;
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (Markable(u, side) && (best == null || u.HpPermille < best.HpPermille)) best = u;
            }
            return best;
        }

        // The aims run on, each shooter's with the line's; when they have run, every shooter who kept his shot fires.
        private void AdvanceRifle(int side)
        {
            SideState state = _sides[side];
            if (state.RifleTicks <= 0) return;
            state.RifleTicks--;
            bool fire = state.RifleTicks == 0;
            // Under the parry at the center the volley is its beat (BattleSimulation.Parry.cs): a wait or a feint holds every
            // shooter who kept his shot.
            if (fire && _parryOn)
            {
                BattleUnit lead = KitChief(side);
                int hold = lead != null ? HoldBeat(side, lead, BeatSlot.Rifle) : 0;
                if (hold > 0)
                {
                    state.RifleTicks = hold;
                    for (int i = 0; i < _units.Count; i++)
                        if (_units[i].Side == side && _units[i].RifleTicks > 0) _units[i].RifleTicks = hold;
                    return;
                }
                ClearBeat(side, BeatSlot.Rifle);
            }
            var shots = new List<BattleUnit>();
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (u.Side != side || u.RifleTicks <= 0) continue;
                if (!fire)
                {
                    u.RifleTicks--;
                    continue;
                }
                u.RifleTicks = 0;
                if (u.Alive) shots.Add(u);
            }
            if (fire) Volley(side, shots);
        }

        // A shooter of her Rifle Line struck by a side focusing him loses his shot.
        private void LoseShot(BattleUnit shooter, BattleUnit source)
        {
            shooter.RifleTicks = 0;
            Emit(new BattleEvent(_tick, BattleEventKind.Broken, 1 - shooter.Side, source == null ? string.Empty : source.Id,
                source == null ? -1 : (int)source.Class, shooter.Id, (int)shooter.Class, 0, ChiefKits.RifleDetail));
        }

        // The volley on its mark, or on their most hurt now if the mark fell. Fall Back takes everyone out of sight; with every
        // shot lost nothing lands; a mark who falls to it was eaten; one who stands with shots lost was answered by them, and
        // otherwise with your Medic standing was topped up through it; with neither, you ate it. Nothing covers it.
        private void Volley(int side, List<BattleUnit> shots)
        {
            SideState state = _sides[side];
            int enemy = 1 - side;
            int started = state.RifleShooters;
            BattleUnit target = FindById(state.RifleTargetId);
            state.RifleTargetId = string.Empty;
            state.RifleShooters = 0;
            BattleUnit chief = KitChief(side);
            if (chief == null) return;
            if (target == null || !Markable(target, enemy)) target = MostHurtMark(enemy);
            if (target == null) return;
            string name = ChiefKits.RifleName + ".";
            if (_sides[enemy].FallBackTicks > 0)
            {
                Emit(EventFor(BattleEventKind.Land, chief, target, 0, name + ChiefKits.Dodged));
                Earn(enemy, FightRules.DodgePoints, FightRules.DodgeReason);
                FavourDodge(enemy, chief);
                Answer(enemy, ChiefAnswer.Dodge, chief, FightRules.DodgePoints);
                return;
            }
            if (shots.Count == 0)
            {
                Emit(EventFor(BattleEventKind.Land, chief, target, 0, name + ChiefKits.Broken));
                Answered(enemy, ChiefAnswer.Break, chief, FightRules.InterruptPoints, FightRules.InterruptReason);
                return;
            }
            // Under the parry at the center a parry turns the volley away and every shooter staggers; a stale Hold soaks it on
            // the front row (BattleSimulation.Parry.cs).
            BeatMet met = _parryOn ? MeetBeat(enemy) : BeatMet.Open;
            if (met == BeatMet.Parry)
            {
                Emit(EventFor(BattleEventKind.Land, chief, target, 0, name + ChiefKits.Parried));
                for (int k = 0; k < shots.Count; k++)
                {
                    if (ReferenceEquals(shots[k], chief) || !shots[k].Alive) continue;
                    shots[k].Cooldown += FightRules.StaggerTicks;
                    shots[k].ReelTicks = FightRules.StaggerTicks;
                }
                ParriedBeat(side, chief, ChiefKits.RifleName, ChiefKits.ParryNerve, FightRules.ParryPoints, true);
                return;
            }
            int each = Math.Max(1, target.MaxHp * ChiefKits.RifleShotPercent / 100);
            if (met == BeatMet.Block && target.Row == Row.Front) each = Math.Max(1, each * HoldSoak(enemy) / 100);
            int left = target.Hp;
            for (int k = 0; k < shots.Count; k++) left -= Lands(target, each, side);
            string how = left <= 0 ? ChiefKits.Full : shots.Count < started ? ChiefKits.Broken
                : MedicStands(enemy) ? ChiefKits.Healed : ChiefKits.Full;
            if (_parryOn && how == ChiefKits.Full)
            {
                if (met == BeatMet.Block) BlockedBeat(side, chief, ChiefKits.ParryNerve);
                else LandedBeat(side);
                if (Stopped) return;
            }
            Emit(EventFor(BattleEventKind.Land, chief, target, each * shots.Count, name + how));
            for (int k = 0; k < shots.Count && !Stopped && target.Alive; k++)
                ApplyDamage(shots[k], side, target, Armored(target, each), ChiefKits.RifleDetail);
            if (Stopped) return;
            if (how == ChiefKits.Broken) Answered(enemy, ChiefAnswer.Break, chief, FightRules.InterruptPoints, FightRules.InterruptReason);
            else if (how == ChiefKits.Healed) Answered(enemy, ChiefAnswer.Heal, chief, ChiefKits.HealPoints, ChiefKits.Name(ChiefAnswer.Heal));
            else if (met != BeatMet.Block) Eaten(enemy, chief);
            CheckWipe();
        }

        // ------------------------------------------------------------ Iron Advance

        // Every AdvanceEveryTicks her shield line marches, while she can move, her front row stands and there is someone of
        // theirs to crush.
        private void TryStartAdvance(int side)
        {
            SideState state = _sides[side];
            if (!HasMove(side, ChiefMove.IronAdvance) || state.AdvanceTicks > 0 || _tick < state.AdvanceNext) return;
            BattleUnit chief = KitChief(side);
            if (chief == null || !CanMove(chief) || !AnyAlive(side, Row.Front) || MarksOf(1 - side).Count == 0)
            {
                state.AdvanceNext = _tick + 1;
                return;
            }
            ClearAdvance(state);
            state.AdvanceTicks = ChiefKits.AdvanceTicks;
            state.AdvanceNext = _tick + ChiefKits.AdvanceEveryTicks;
            Emit(EventFor(BattleEventKind.Tell, chief, null, ChiefKits.AdvanceTicks, ChiefKits.AdvanceName));
        }

        // The march runs on and crushes once a second; it stops if her front row has fallen. When it has run, how it was met.
        private void MarchOn(int side)
        {
            SideState state = _sides[side];
            if (state.AdvanceTicks <= 0 && state.AdvanceHold <= 0) return;
            BattleUnit chief = KitChief(side);
            if (chief == null || !AnyAlive(side, Row.Front))
            {
                ClearAdvance(state);
                return;
            }
            // Under the parry at the center every crush is a beat (BattleSimulation.Parry.cs): one that waits or feints holds
            // the march until it lands.
            if (state.AdvanceHold > 0)
            {
                state.AdvanceHold--;
                if (state.AdvanceHold > 0) return;
                MarchCrush(side, chief);
                ClearBeat(side, BeatSlot.Advance);
                if (state.AdvanceTicks == 0 && !Stopped) EndAdvance(side, chief);
                return;
            }
            state.AdvanceTicks--;
            if ((ChiefKits.AdvanceTicks - state.AdvanceTicks) % SimClock.TicksPerSecond == 0)
            {
                int hold = _parryOn ? HoldBeat(side, chief, BeatSlot.Advance) : 0;
                if (hold > 0)
                {
                    state.AdvanceHold = hold;
                    return;
                }
                MarchCrush(side, chief);
                if (_parryOn) ClearBeat(side, BeatSlot.Advance);
            }
            if (state.AdvanceTicks == 0 && !Stopped) EndAdvance(side, chief);
        }

        // One second of the march. Fall Back gives ground: no damage, and her side gets nerve back (the Land amount). A Hold
        // braces: no damage, and the holding side's own nerve pays. Otherwise it crushes their front row (the back row once the
        // front has fallen), and nothing covers it.
        private void MarchCrush(int side, BattleUnit chief)
        {
            SideState state = _sides[side];
            int enemy = 1 - side;
            SideState theirs = _sides[enemy];
            string name = ChiefKits.AdvanceName + ".";
            if (theirs.FallBackTicks > 0)
            {
                state.AdvanceGave++;
                int before = state.Nerve;
                if (_nerveOn) state.Nerve = Math.Min(NerveRules.Full, state.Nerve + ChiefKits.AdvanceGroundNerve);
                Emit(EventFor(BattleEventKind.Land, chief, null, state.Nerve - before, name + ChiefKits.Dodged));
                return;
            }
            // Under the parry at the center a parry breaks the march and staggers her line; a stale Hold still braces (no
            // damage), but it cracks your nerve as hard as the parry would have cracked hers (BattleSimulation.Parry.cs).
            BeatMet met = _parryOn ? MeetBeat(enemy) : BeatMet.Open;
            if (met == BeatMet.Parry)
            {
                ClearAdvance(state);
                Emit(EventFor(BattleEventKind.Land, chief, null, 0, name + ChiefKits.Parried));
                for (int i = 0; i < _units.Count; i++)
                {
                    BattleUnit u = _units[i];
                    if (u.Alive && u.Side == side && u.Row == Row.Front && !ReferenceEquals(u, chief)) u.Cooldown += FightRules.StaggerTicks;
                }
                ParriedBeat(side, chief, ChiefKits.AdvanceName, ChiefKits.ParryNerve, FightRules.ParryPoints, true);
                return;
            }
            if (theirs.HoldTicks > 0)
            {
                state.AdvanceBraced++;
                Emit(EventFor(BattleEventKind.Land, chief, null, 0, name + ChiefKits.Braced));
                if (_parryOn) BlockedBeat(side, chief, ChiefKits.ParryNerve);
                else DrainNerve(enemy, ChiefKits.AdvanceBraceNerve, enemy);
                return;
            }
            Row row = AnyMarkIn(enemy, Row.Front) ? Row.Front : Row.Back;
            List<BattleUnit> hit = MarksIn(enemy, row);
            if (hit.Count == 0) return;
            state.AdvanceFull++;
            if (_parryOn) LandedBeat(side);
            var shares = new int[hit.Count];
            int total = 0;
            for (int k = 0; k < hit.Count; k++)
            {
                shares[k] = Math.Max(1, hit[k].MaxHp * ChiefKits.AdvanceCrushPercent / 100);
                total += shares[k];
            }
            Emit(EventFor(BattleEventKind.Land, chief, null, total, name + ChiefKits.Full));
            for (int k = 0; k < hit.Count && !Stopped; k++)
                ApplyDamage(chief, side, hit[k], Armored(hit[k], shares[k]), ChiefKits.AdvanceDetail);
            CheckWipe();
        }

        // None of it landed: a Hold that braced answered it (a Brace, which earns no points), else giving ground did (a Dodge).
        // All of it landed with neither: they ate it.
        private void EndAdvance(int side, BattleUnit chief)
        {
            SideState state = _sides[side];
            int enemy = 1 - side;
            int full = state.AdvanceFull, braced = state.AdvanceBraced, gave = state.AdvanceGave;
            ClearAdvance(state);
            // Under the parry at the center a brace is a block, not an answer (BattleSimulation.Parry.cs).
            if (full == 0 && braced > 0) { if (!_parryOn) Answer(enemy, ChiefAnswer.Brace, chief, 0); }
            else if (full == 0 && gave > 0) Answered(enemy, ChiefAnswer.Dodge, chief, FightRules.DodgePoints, FightRules.DodgeReason);
            else if (full > 0 && braced == 0 && gave == 0) Eaten(enemy, chief);
        }

        // An Unleash into her march staggers her line: the march stops, her front row loses FightRules.StaggerTicks, and it
        // answers her with a Smash.
        private void StaggerAdvance(int side)
        {
            int enemy = 1 - side;
            SideState her = _sides[enemy];
            if (_finished || (her.AdvanceTicks <= 0 && her.AdvanceHold <= 0)) return;
            BattleUnit chief = KitChief(enemy);
            ClearAdvance(her);
            if (chief == null) return;
            Emit(EventFor(BattleEventKind.Land, chief, null, 0, ChiefKits.AdvanceName + "." + ChiefKits.Staggered));
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (u.Alive && u.Side == enemy && u.Row == Row.Front) u.Cooldown += FightRules.StaggerTicks;
            }
            Answered(side, ChiefAnswer.Smash, chief, ChiefSpells.SmashPoints, ChiefSpells.SmashReason);
        }

        private static void ClearAdvance(SideState state)
        {
            state.AdvanceHold = 0;
            state.Beats[(int)BeatSlot.Advance] = 0;
            state.AdvanceTicks = 0;
            state.AdvanceFull = 0;
            state.AdvanceBraced = 0;
            state.AdvanceGave = 0;
        }

        // ------------------------------------------------------------ Overclock

        // Every OverclockEveryTicks her Medic jabs her band, while she can move and a Medic of her band is free.
        private void TryStartOverclock(int side)
        {
            SideState state = _sides[side];
            if (!HasMove(side, ChiefMove.Overclock) || state.JabTicks > 0 || state.OverclockTicks > 0 || _tick < state.OverclockNext) return;
            BattleUnit chief = KitChief(side);
            BattleUnit medic = chief != null && CanMove(chief) ? BandMedic(side) : null;
            if (medic == null)
            {
                state.OverclockNext = _tick + 1;
                return;
            }
            state.JabTicks = ChiefKits.JabTicks;
            state.JabMedicId = medic.Id;
            state.OverclockNext = _tick + ChiefKits.OverclockEveryTicks;
            Emit(EventFor(BattleEventKind.Tell, medic, null, ChiefKits.JabTicks, ChiefKits.OverclockName));
        }

        // Her band's first free Medic: standing, not held by a snare, not panicking.
        private BattleUnit BandMedic(int side)
        {
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (u.Alive && u.Side == side && u.Role == UnitRole.Fighter && u.Class == UnitClass.Medic && u.SnareTicks <= 0 &&
                    u.PanicTicks <= 0)
                    return u;
            }
            return null;
        }

        // The jab runs on. Focus Fire on her Medic breaks it, as does her falling (a Break), or a snare holding her (a Snare);
        // once it has run, her band acts twice as fast for OverclockTicks.
        private void AdvanceJab(int side)
        {
            SideState state = _sides[side];
            if (state.JabTicks <= 0) return;
            int enemy = 1 - side;
            BattleUnit medic = FindById(state.JabMedicId);
            BattleUnit chief = KitChief(side);
            string name = ChiefKits.OverclockName + ".";
            bool standing = medic != null && medic.Alive;
            bool focused = standing && IsFocusing(enemy, medic);
            bool snared = standing && medic.SnareTicks > 0;
            if (!standing || focused || snared)
            {
                state.JabTicks = 0;
                state.JabMedicId = string.Empty;
                if (_parryOn) ClearBeat(side, BeatSlot.Jab);
                if (medic != null) Emit(EventFor(BattleEventKind.Land, medic, null, 0, name + ChiefKits.Broken));
                if (chief == null) return;
                if (snared && !focused) Answered(enemy, ChiefAnswer.Snare, chief, ChiefKits.SnarePoints, ChiefKits.Name(ChiefAnswer.Snare));
                else Answered(enemy, ChiefAnswer.Break, chief, FightRules.InterruptPoints, FightRules.InterruptReason);
                return;
            }
            state.JabTicks--;
            if (state.JabTicks > 0) return;
            // Under the parry at the center the needle is its beat (BattleSimulation.Parry.cs): parried, no flurry comes. It hits
            // nobody of yours, so a stale Hold costs nothing here.
            if (_parryOn)
            {
                int hold = HoldBeat(side, medic, BeatSlot.Jab);
                if (hold > 0)
                {
                    state.JabTicks = hold;
                    return;
                }
                ClearBeat(side, BeatSlot.Jab);
                if (MeetBeat(enemy) == BeatMet.Parry && chief != null)
                {
                    state.JabMedicId = string.Empty;
                    Emit(EventFor(BattleEventKind.Land, medic, null, 0, name + ChiefKits.Parried));
                    ParriedBeat(side, medic, ChiefKits.OverclockName, ChiefKits.ParryNerve, FightRules.ParryPoints, true, chief);
                    return;
                }
            }
            state.JabMedicId = string.Empty;
            state.OverclockTicks = ChiefKits.OverclockTicks;
            state.OverclockSnared = false;
            state.OverclockHeld = false;
            Emit(EventFor(BattleEventKind.Land, medic, null, ChiefKits.OverclockTicks, name + ChiefKits.Full));
        }

        // The flurry runs on (the unit loop counts her band's cooldowns down twice as fast), noting whether the other side
        // holds or a snare holds one of her band meanwhile; when it ends, how it was answered: a snare first, else a Hold.
        private void AdvanceOverclock(int side)
        {
            SideState state = _sides[side];
            if (state.OverclockTicks <= 0) return;
            if (_sides[1 - side].HoldTicks > 0) state.OverclockHeld = true;
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (u.Alive && u.Side == side && u.SnareTicks > 0) state.OverclockSnared = true;
            }
            state.OverclockTicks--;
            if (state.OverclockTicks > 0) return;
            bool snared = state.OverclockSnared, held = state.OverclockHeld;
            state.OverclockSnared = false;
            state.OverclockHeld = false;
            BattleUnit chief = KitChief(side);
            if (chief == null) return;
            if (snared) Answered(1 - side, ChiefAnswer.Snare, chief, ChiefKits.SnarePoints, ChiefKits.Name(ChiefAnswer.Snare));
            else if (held) Answer(1 - side, ChiefAnswer.Brace, chief, 0);
        }
    }
}
