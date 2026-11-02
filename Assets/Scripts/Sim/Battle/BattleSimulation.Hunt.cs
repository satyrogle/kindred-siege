using System;

namespace KindredSiege.Sim.Battle
{
    /// The hunt (Jacob, 22 Oct 2026: "yea go i like it", then "yes go" to building it; DESIGN_LOCKS "The hunters' boss, the
    /// hunt"). The fight at the end of a run is the hunt the hunters run on one of your fighters, a bar of four steps over the
    /// prey (HuntStep): Track, Drive, Corner, the Kill. Each step runs on unless you break it; break any step and the hunt
    /// starts again at Track on a new prey. From the captain's half health two hunts run at once (the Pack). Break each of
    /// the four steps once with four different answers and the hunt turns (Turned Hunt). ChiefKits holds the numbers. Only
    /// fights where a side's kit is ChiefKit.Hunt come here, and nothing here draws from the random stream.
    ///
    /// Claude's calls while building, Jacob's to change: a prey is your most hurt fighter, never your leader while another of
    /// yours stands, and a new hunt takes someone other than the last prey when it can; your Warden breaks a Track only by
    /// covering the prey as he already would (no new cover rule there), and your Medic only by a treatment that heals them
    /// from half or under to over half; a Warden does step in for an ally netted for the Kill, as he covers a branded ally;
    /// Hold the line means your Hold up at any moment of the Drive; the Drive's harder blows are all of his side's on
    /// the prey's row; his first Trapper throws the net, the captain if none stands, and that thrower falling during the line
    /// breaks the Corner; the net is a snare (they do nothing and cannot dodge, and his Marksmen's aimed shots crit them),
    /// and your Medic or your Unleash freeing them before the Kill lands breaks the Corner; only a perfect Hold parries the
    /// Kill, a late one does not soak it, and the hunters wait out a fresh Hold once as they always do; the captain does
    /// nothing else while his Kill comes; a hunt whose prey falls to something else simply starts again; Turned Hunt counts
    /// the steps you broke in a row, each with an answer not used yet, starts over on a repeat and ends with any Kill that
    /// lands; his own Big Hit (call J2) stays on its clock.
    public sealed partial class BattleSimulation
    {
        private const int HuntSlots = 2;

        // Every step's bit, for Turned Hunt.
        private const int AllHuntSteps = (1 << (int)HuntStep.Track) | (1 << (int)HuntStep.Drive) | (1 << (int)HuntStep.Corner) |
            (1 << (int)HuntStep.Kill);

        // The hunt is on, and the side that hunts (-1 without it). Set in the constructor.
        private readonly bool _huntOn;
        private readonly int _huntSide = -1;

        // Each hunt's slot, 0 from the start and 1 from the Pack: its step, ticks left in it, its prey, its last prey, who
        // throws its net, and whether its Kill already waited out a fresh Hold.
        private readonly int[] _huntStep = new int[HuntSlots];
        private readonly int[] _huntTicks = new int[HuntSlots];
        private readonly string[] _huntPrey = { string.Empty, string.Empty };
        private readonly string[] _huntLastPrey = { string.Empty, string.Empty };
        private readonly string[] _huntNetter = { string.Empty, string.Empty };
        private readonly bool[] _killWaited = new bool[HuntSlots];
        private bool _packOn;

        // Under the parry at the center each step ends on a beat (BattleSimulation.Parry.cs): the Track's mark, the Drive's
        // shove, the Corner's net throw and the Kill. What each hunt's beat has done so far.
        private readonly int[] _huntBeat = new int[HuntSlots];

        // Turned Hunt: the steps broken toward it and the answers that broke them (bits), and its ticks left.
        private int _chainSteps;
        private int _chainAnswers;
        private int _turnedTicks;

        // ------------------------------------------------------------ questions the screen asks

        /// True when this fight is the hunt (ChiefKit.Hunt), and the side that hunts, or -1.
        public bool HuntOn => _huntOn;

        public int HuntSide => _huntSide;

        /// True from the hunt captain's half health: two hunts run at once.
        public bool PackOn => _huntOn && _packOn;

        /// How many hunts run now: 1, 2 with the Pack, 0 without the hunt.
        public int HuntCount => !_huntOn ? 0 : _packOn ? 2 : 1;

        /// A hunt's step (hunt 0, or 1 with the Pack), the ticks left in it and its prey. HuntStep.None between hunts.
        public HuntStep HuntStepOf(int hunt)
        {
            return hunt >= 0 && hunt < HuntCount ? (HuntStep)_huntStep[hunt] : HuntStep.None;
        }

        public int HuntTicksLeft(int hunt)
        {
            return HuntStepOf(hunt) != HuntStep.None ? _huntTicks[hunt] : 0;
        }

        public string HuntPreyId(int hunt)
        {
            return HuntStepOf(hunt) != HuntStep.None ? _huntPrey[hunt] : string.Empty;
        }

        /// Who throws a hunt's net while its Corner line glows: his first Trapper, else the captain. Empty otherwise.
        public string HuntNetterId(int hunt)
        {
            return HuntStepOf(hunt) == HuntStep.Corner ? _huntNetter[hunt] : string.Empty;
        }

        /// True while a hunt's Kill comes and a Hold timed to its last FightRules.PerfectHoldTicks would parry it.
        public bool KillParryable(int hunt)
        {
            return HuntStepOf(hunt) == HuntStep.Kill && _fight.PerfectHold;
        }

        /// Ticks of Turned Hunt left, or 0: his band cannot cover him, your blows on him land double, and his hunts wait.
        public int TurnedTicksLeft => _huntOn ? _turnedTicks : 0;

        // ------------------------------------------------------------ the hunts

        // The hunts run on, after the tick's blows. While Turned Hunt runs they wait.
        private void AdvanceHunt(int side)
        {
            BattleUnit captain = KitChief(side);
            if (captain == null || _turnedTicks > 0) return;
            for (int k = 0; k < HuntCount && !Stopped; k++) AdvanceOneHunt(side, captain, k);
        }

        private void AdvanceOneHunt(int side, BattleUnit captain, int k)
        {
            HuntStep step = (HuntStep)_huntStep[k];
            if (step == HuntStep.None)
            {
                if (k == 0 && _tick < ChiefKits.HuntStartTicks) return;
                StartTrack(side, captain, k);
                return;
            }
            int foe = 1 - side;
            BattleUnit prey = FindById(_huntPrey[k]);
            if (prey == null || !prey.Alive)
            {
                EndHunt(k);
                return;
            }
            switch (step)
            {
                case HuntStep.Track:
                    // Your Warden covering the prey loses him the scent.
                    if (GuardingWarden(prey) != null)
                    {
                        Broke(side, captain, k, HuntStep.Track, ChiefAnswer.Guard, ChiefKits.Guarded, ChiefKits.GuardPoints,
                            ChiefKits.Name(ChiefAnswer.Guard));
                        return;
                    }
                    if (--_huntTicks[k] > 0) return;
                    if (_parryOn && HuntBeat(side, captain, k, prey, HuntStep.Track, captain)) return;
                    StartDrive(side, captain, k, prey);
                    return;
                case HuntStep.Drive:
                    // Hold the line, or Focus Fire one of his front row. Under the parry at the center a Hold no longer breaks it:
                    // the shove at its end is its beat.
                    if (!_parryOn && _sides[foe].HoldTicks > 0)
                    {
                        Broke(side, captain, k, HuntStep.Drive, ChiefAnswer.Brace, ChiefKits.Braced, 0, string.Empty);
                        return;
                    }
                    if (FocusOnRow(foe, side, Row.Front))
                    {
                        Broke(side, captain, k, HuntStep.Drive, ChiefAnswer.Break, ChiefKits.Broken, FightRules.InterruptPoints,
                            FightRules.InterruptReason);
                        return;
                    }
                    if (!AnyAlive(side, Row.Front))
                    {
                        StartCorner(side, captain, k, prey);
                        return;
                    }
                    if (--_huntTicks[k] > 0) return;
                    if (_parryOn && HuntBeat(side, captain, k, prey, HuntStep.Drive, captain)) return;
                    StartCorner(side, captain, k, prey);
                    return;
                case HuntStep.Corner:
                {
                    // Focus Fire the one who throws the net while the line glows, or bring him down, and the net never comes.
                    BattleUnit netter = FindById(_huntNetter[k]);
                    if (netter == null || !netter.Alive || IsFocusing(foe, netter))
                    {
                        Broke(side, captain, k, HuntStep.Corner, ChiefAnswer.Break, ChiefKits.Broken, FightRules.InterruptPoints,
                            FightRules.InterruptReason);
                        return;
                    }
                    if (--_huntTicks[k] > 0) return;
                    if (_parryOn && HuntBeat(side, captain, k, prey, HuntStep.Corner, netter)) return;
                    Net(side, captain, k, prey, netter);
                    return;
                }
                case HuntStep.Kill:
                    // The glaive waits while the captain cannot move; then it falls, unless his side waits out a fresh Hold
                    // once.
                    if (!CanMove(captain)) return;
                    if (--_huntTicks[k] > 0) return;
                    // Under the parry at the center the Kill is a beat like the rest (BattleSimulation.Parry.cs).
                    if (_parryOn)
                    {
                        int hold = HoldBeat(side, captain, ref _huntBeat[k], ChiefKits.Name(HuntStep.Kill));
                        if (hold > 0)
                        {
                            _huntTicks[k] = hold;
                            return;
                        }
                        _huntBeat[k] = 0;
                        Strike(side, captain, k, prey);
                        return;
                    }
                    if (!_killWaited[k] && WaitsOutParry(side))
                    {
                        int wait = FreshHoldWait(foe);
                        if (wait > 0)
                        {
                            _killWaited[k] = true;
                            _huntTicks[k] = wait;
                            Emit(EventFor(BattleEventKind.Reaction, captain, null, wait, WaitOutDetail));
                            return;
                        }
                    }
                    Strike(side, captain, k, prey);
                    return;
            }
        }

        // A step's beat under the parry at the center: a wait or a feint holds it; a parry breaks the hunt there, as a broken
        // step does (true, it has ended); a stale Hold cracks your nerve and the step goes on; with no Hold your parry chain
        // starts over and it goes on. False to go on.
        private bool HuntBeat(int side, BattleUnit captain, int k, BattleUnit prey, HuntStep step, BattleUnit doer)
        {
            int hold = HoldBeat(side, doer, ref _huntBeat[k], ChiefKits.Name(step));
            if (hold > 0)
            {
                _huntTicks[k] = hold;
                return true;
            }
            _huntBeat[k] = 0;
            BeatMet met = MeetBeat(1 - side);
            if (met == BeatMet.Parry)
            {
                Emit(EventFor(BattleEventKind.Land, captain, prey, 0, ChiefKits.Name(step) + "." + ChiefKits.Parried));
                EndHunt(k);
                int cracked = ParryLands(side, doer, ChiefKits.Name(step), ChiefKits.ParryNerve, FightRules.ParryPoints);
                ChainBreak(side, captain, step, ChiefAnswer.Parry);
                ParryCracks(side, captain, cracked, FightRules.ParryPoints, true);
                return true;
            }
            if (met == BeatMet.Block) BlockedBeat(side, doer, ChiefKits.ParryNerve);
            else LandedBeat(side);
            return Stopped;
        }

        // A new hunt marks the other side's most hurt fighter a move can fall on: not the other hunt's prey, and not this
        // hunt's last one while someone else will do. None to mark, it waits.
        private void StartTrack(int side, BattleUnit captain, int k)
        {
            BattleUnit prey = PickPrey(side, k);
            if (prey == null) return;
            _huntStep[k] = (int)HuntStep.Track;
            _huntTicks[k] = ChiefKits.TrackTicks;
            _huntPrey[k] = prey.Id;
            _huntNetter[k] = string.Empty;
            _killWaited[k] = false;
            Emit(EventFor(BattleEventKind.Tell, captain, prey, ChiefKits.TrackTicks, ChiefKits.Name(HuntStep.Track)));
        }

        private BattleUnit PickPrey(int side, int k)
        {
            int foe = 1 - side;
            string other = HuntCount > 1 ? _huntPrey[1 - k] : string.Empty;
            BattleUnit best = null, last = null;
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (!Markable(u, foe) || string.Equals(u.Id, other, StringComparison.Ordinal)) continue;
                if (string.Equals(u.Id, _huntLastPrey[k], StringComparison.Ordinal)) last = u;
                else if (best == null || u.HpPermille < best.HpPermille) best = u;
            }
            return best ?? last;
        }

        // The Drive: his front row pushes in. With none of it standing the hunt goes straight to the Corner.
        private void StartDrive(int side, BattleUnit captain, int k, BattleUnit prey)
        {
            if (!AnyAlive(side, Row.Front))
            {
                StartCorner(side, captain, k, prey);
                return;
            }
            _huntStep[k] = (int)HuntStep.Drive;
            _huntTicks[k] = ChiefKits.DriveTicks;
            Emit(EventFor(BattleEventKind.Tell, captain, prey, ChiefKits.DriveTicks, ChiefKits.Name(HuntStep.Drive)));
        }

        // The Corner: the purple line glows under the prey, from his first Trapper, or the captain if none stands.
        private void StartCorner(int side, BattleUnit captain, int k, BattleUnit prey)
        {
            BattleUnit netter = null;
            for (int i = 0; i < _units.Count && netter == null; i++)
            {
                BattleUnit u = _units[i];
                if (u.Alive && u.Side == side && u.Role == UnitRole.Fighter && u.Class == UnitClass.Trapper) netter = u;
            }
            netter = netter ?? captain;
            _huntStep[k] = (int)HuntStep.Corner;
            _huntTicks[k] = ChiefKits.LineTicks;
            _huntNetter[k] = netter.Id;
            Emit(EventFor(BattleEventKind.Tell, captain, prey, ChiefKits.LineTicks, ChiefKits.Name(HuntStep.Corner)));
        }

        // The net lands: the prey is held as a snare holds, and the Kill comes.
        private void Net(int side, BattleUnit captain, int k, BattleUnit prey, BattleUnit netter)
        {
            prey.Statuses.Apply(StatusKind.Snare, ChiefKits.NetTicks);
            MarkEvent(side, netter, prey, ChiefKits.NetTicks, ChiefKits.NetMark);
            Emit(EventFor(BattleEventKind.Land, captain, prey, 0, ChiefKits.Name(HuntStep.Corner) + "." + ChiefKits.Full));
            _huntStep[k] = (int)HuntStep.Kill;
            _huntTicks[k] = ChiefKits.KillTicks;
            _huntNetter[k] = string.Empty;
            Emit(EventFor(BattleEventKind.Tell, captain, prey, ChiefKits.KillTicks, ChiefKits.Name(HuntStep.Kill)));
        }

        // The glaive comes down: a Hold started in the parry window parries it (he staggers); a Warden covering the prey takes
        // it less his Guard; otherwise it lands in full, you ate it, and Turned Hunt starts over.
        private void Strike(int side, BattleUnit captain, int k, BattleUnit prey)
        {
            int foe = 1 - side;
            SideState theirs = _sides[foe];
            string name = ChiefKits.Name(HuntStep.Kill) + ".";
            BeatMet met = _parryOn ? MeetBeat(foe) : BeatMet.Open;
            if (met == BeatMet.Parry)
            {
                Emit(EventFor(BattleEventKind.Land, captain, prey, 0, name + ChiefKits.Parried));
                EndHunt(k);
                int cracked = ParryLands(side, captain, ChiefKits.Name(HuntStep.Kill), ChiefKits.ParryNerve, FightRules.ParryPoints);
                ChainBreak(side, captain, HuntStep.Kill, ChiefAnswer.Parry);
                ParryCracks(side, captain, cracked, FightRules.ParryPoints, true);
                return;
            }
            if (!_parryOn && _fight.PerfectHold && theirs.HoldTicks > 0 && theirs.LastHoldTick >= 0 &&
                _tick - theirs.LastHoldTick <= ParryWindowTicks)
            {
                Emit(EventFor(BattleEventKind.Land, captain, prey, 0, name + ChiefKits.Parried));
                Emit(new BattleEvent(_tick, BattleEventKind.Parry, foe, string.Empty, -1, captain.Id, (int)captain.Class,
                    FightRules.StaggerTicks, ChiefKits.Name(HuntStep.Kill)));
                captain.Cooldown += FightRules.StaggerTicks;
                theirs.HoldTicks = 0;
                theirs.LastParryTick = _tick;
                theirs.ParriedCasterId = captain.Id;
                Earn(foe, FightRules.ParryPoints, FightRules.ParryReason);
                SayOnParry(captain);
                EndHunt(k);
                ChainBreak(side, captain, HuntStep.Kill, ChiefAnswer.Parry);
                Answer(foe, ChiefAnswer.Parry, captain, FightRules.ParryPoints);
                return;
            }

            int damage = Math.Max(1, prey.MaxHp * ChiefKits.KillPercent / 100);
            BattleUnit warden = GuardingWarden(prey);
            if (warden != null)
            {
                int cut = _classOn ? GuardCut(warden) : SliceClasses.GuardReductionPercent;
                int taken = damage * (100 - cut) / 100;
                Emit(EventFor(BattleEventKind.Land, captain, prey, taken, name + ChiefKits.Guarded));
                Emit(EventFor(BattleEventKind.Guard, warden, prey, damage - taken, string.Empty));
                ApplyDamage(captain, side, warden, Armored(warden, taken), ChiefKits.KillDetail);
                EndHunt(k);
                if (Stopped) return;
                Earn(foe, ChiefKits.GuardPoints, ChiefKits.Name(ChiefAnswer.Guard));
                ChainBreak(side, captain, HuntStep.Kill, ChiefAnswer.Guard);
                Answer(foe, ChiefAnswer.Guard, captain, ChiefKits.GuardPoints);
                CheckWipe();
                return;
            }

            // Under the parry at the center a stale Hold soaks the Kill on the front row and cracks your nerve.
            if (met == BeatMet.Block)
            {
                BlockedBeat(side, captain, ChiefKits.ParryNerve);
                if (Stopped) return;
                if (prey.Row == Row.Front) damage = Math.Max(1, damage * HoldSoak(foe) / 100);
            }
            else if (_parryOn) LandedBeat(side);
            Emit(EventFor(BattleEventKind.Land, captain, prey, damage, name + (met == BeatMet.Block ? ChiefKits.Soaked : ChiefKits.Full)));
            ApplyDamage(captain, side, prey, Armored(prey, damage), ChiefKits.KillDetail);
            EndHunt(k);
            _chainSteps = 0;
            _chainAnswers = 0;
            if (Stopped) return;
            if (met == BeatMet.Block)
            {
                CheckWipe();
                return;
            }
            Say(captain, ChiefKits.EclipseLine);
            Eaten(foe, captain);
            CheckWipe();
        }

        // A step was broken: its Land, the points, Turned Hunt's count, then the answer (Glory and a chunk of his nerve), and
        // the hunt starts again at Track on a new prey.
        private void Broke(int side, BattleUnit captain, int k, HuntStep step, ChiefAnswer answer, string how, int points, string reason)
        {
            int foe = 1 - side;
            Emit(EventFor(BattleEventKind.Land, captain, FindById(_huntPrey[k]), 0, ChiefKits.Name(step) + "." + how));
            Earn(foe, points, reason);
            EndHunt(k);
            ChainBreak(side, captain, step, answer);
            Answer(foe, answer, captain, points);
        }

        private void EndHunt(int k)
        {
            _huntLastPrey[k] = _huntPrey[k];
            _huntStep[k] = (int)HuntStep.None;
            _huntTicks[k] = 0;
            _huntPrey[k] = string.Empty;
            _huntNetter[k] = string.Empty;
            _killWaited[k] = false;
            _huntBeat[k] = 0;
        }

        // Turned Hunt's count: a step not yet broken in this run of breaks, with an answer not yet used, adds to it; anything
        // else starts it over from this break. All four steps and it turns: for TurnedTicks his band cannot cover him and
        // your blows on him land double.
        private void ChainBreak(int side, BattleUnit captain, HuntStep step, ChiefAnswer answer)
        {
            int s = 1 << (int)step, a = 1 << (int)answer;
            if ((_chainSteps & s) != 0 || (_chainAnswers & a) != 0)
            {
                _chainSteps = s;
                _chainAnswers = a;
            }
            else
            {
                _chainSteps |= s;
                _chainAnswers |= a;
            }
            if (_chainSteps != AllHuntSteps || Stopped) return;
            _chainSteps = 0;
            _chainAnswers = 0;
            int foe = 1 - side;
            _turnedTicks = ChiefKits.TurnedTicks;
            Emit(new BattleEvent(_tick, BattleEventKind.Combo, foe, string.Empty, -1, captain.Id, (int)captain.Class,
                ChiefKits.TurnedTicks, ChiefKits.TurnedHunt));
            Earn(foe, FightRules.ComboPoints, FightRules.ComboReason);
        }

        // ------------------------------------------------------------ what the rest of the fight asks of the hunt

        // A side's Focus Fire is on a standing fighter of that row of the other side.
        private bool FocusOnRow(int focusing, int side, Row row)
        {
            SideState state = _sides[focusing];
            if (state.FocusTicks <= 0) return false;
            BattleUnit target = FindById(state.FocusTargetId);
            return target != null && target.Alive && target.Side == side && target.Row == row;
        }

        // While a Drive runs, the hunting side's blows on its prey's row land harder.
        private int DriveBlow(BattleUnit attacker, BattleUnit target, int damage)
        {
            return Drives(attacker, target) ? damage * ChiefKits.DrivePercent / 100 : damage;
        }

        // The same through the damage layers (BattleSimulation.Layers.cs): a Drive in the kit layer.
        private void DriveHit(BattleUnit attacker, BattleUnit target, Hit hit)
        {
            if (Drives(attacker, target)) hit.Add(HitLayer.Kit, ChiefKits.DrivePercent - 100);
        }

        // A hunter's blow on the row of a prey his side is driving.
        private bool Drives(BattleUnit attacker, BattleUnit target)
        {
            if (attacker.Side != _huntSide || target.Side == _huntSide) return false;
            for (int k = 0; k < HuntCount; k++)
            {
                if ((HuntStep)_huntStep[k] != HuntStep.Drive) continue;
                BattleUnit prey = FindById(_huntPrey[k]);
                if (prey != null && prey.Row == target.Row) return true;
            }
            return false;
        }

        // A treatment from the prey's own Medic: in the Track, one that heals them from half or under to over half turns the
        // scent cold; in the Kill, while they are netted, she cuts them free and the Corner is broken.
        private void HuntHealed(BattleUnit healer, BattleUnit patient, int healed)
        {
            if (healer == null || healer.Class != UnitClass.Medic || healer.Side != patient.Side || patient.Side == _huntSide || Stopped) return;
            int before = (patient.Hp - healed) * 1000 / patient.MaxHp;
            BattleUnit captain = KitChief(_huntSide);
            if (captain == null) return;
            for (int k = 0; k < HuntCount && !Stopped; k++)
            {
                if (!string.Equals(_huntPrey[k], patient.Id, StringComparison.Ordinal)) continue;
                HuntStep step = (HuntStep)_huntStep[k];
                if (step == HuntStep.Track && before <= ChiefKits.ColdScentPermille && patient.HpPermille > ChiefKits.ColdScentPermille)
                    Broke(_huntSide, captain, k, HuntStep.Track, ChiefAnswer.Heal, ChiefKits.Healed, ChiefKits.HealPoints,
                        ChiefKits.Name(ChiefAnswer.Heal));
                else if (step == HuntStep.Kill && patient.SnareTicks > 0)
                {
                    patient.Statuses.Clear(StatusKind.Snare);
                    Broke(_huntSide, captain, k, HuntStep.Corner, ChiefAnswer.Heal, ChiefKits.Healed, ChiefKits.HealPoints,
                        ChiefKits.Name(ChiefAnswer.Heal));
                }
            }
        }

        // An Unleash from the hunted side tears the net off every prey waiting for the Kill.
        private void TearNets(int side)
        {
            if (side == _huntSide || Stopped) return;
            BattleUnit captain = KitChief(_huntSide);
            if (captain == null) return;
            for (int k = 0; k < HuntCount && !Stopped; k++)
            {
                if ((HuntStep)_huntStep[k] != HuntStep.Kill) continue;
                BattleUnit prey = FindById(_huntPrey[k]);
                if (prey == null || !prey.Alive || prey.SnareTicks <= 0) continue;
                prey.Statuses.Clear(StatusKind.Snare);
                Broke(_huntSide, captain, k, HuntStep.Corner, ChiefAnswer.Smash, ChiefKits.Torn, ChiefSpells.SmashPoints,
                    ChiefSpells.SmashReason);
            }
        }

        // A Warden steps in for an ally of his netted for the Kill: the first such, not himself.
        private BattleUnit NettedPrey(BattleUnit warden)
        {
            if (warden.Side == _huntSide) return null;
            for (int k = 0; k < HuntCount; k++)
            {
                if ((HuntStep)_huntStep[k] != HuntStep.Kill) continue;
                BattleUnit prey = FindById(_huntPrey[k]);
                if (prey != null && prey.Alive && prey.Side == warden.Side && !ReferenceEquals(prey, warden)) return prey;
            }
            return null;
        }

        // The captain winds up a Kill: he does nothing else meanwhile.
        private bool KillComing(BattleUnit unit)
        {
            if (unit.Side != _huntSide || unit.Role != UnitRole.Chief) return false;
            for (int k = 0; k < HuntCount; k++)
                if ((HuntStep)_huntStep[k] == HuntStep.Kill) return true;
            return false;
        }

        // While Turned Hunt runs, nobody covers the captain and your blows on him land double.
        private bool Turned(BattleUnit unit)
        {
            return _turnedTicks > 0 && unit.Side == _huntSide && unit.Role == UnitRole.Chief;
        }

        private void AppendHunt(SnapshotBuilder b)
        {
            b.Field("huntSide", _huntSide);
            b.Field("pack", _packOn);
            b.Field("huntChainSteps", _chainSteps);
            b.Field("huntChainAnswers", _chainAnswers);
            b.Field("turned", _turnedTicks);
            for (int k = 0; k < HuntSlots; k++)
            {
                string key = "hunt" + (k == 0 ? "0" : "1") + ".";
                b.Field(key + "step", _huntStep[k]);
                b.Field(key + "ticks", _huntTicks[k]);
                b.Field(key + "prey", _huntPrey[k]);
                b.Field(key + "lastPrey", _huntLastPrey[k]);
                b.Field(key + "netter", _huntNetter[k]);
                b.Field(key + "waited", _killWaited[k]);
                if (_parryOn) b.Field(key + "beat", _huntBeat[k]);
            }
        }
    }
}
