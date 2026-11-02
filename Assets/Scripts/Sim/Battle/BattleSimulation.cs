using System;
using System.Collections.Generic;
using System.Globalization;

namespace KindredSiege.Sim.Battle
{
    /// A deterministic auto battle between two sides. Units act on their own, steered by their
    /// side's tactics; the only outside input is a directive command. Learned reflexes (answer
    /// Unleash with Hold, cover whoever the enemy focuses, press with Unleash when the enemy falls
    /// back) fire inside the sim from the tactics, so they replay exactly. Integers only, one
    /// seeded random stream, units always processed in a fixed order. Numbers are provisional.
    ///
    /// Under the nerve rules (BattleSetup.Nerve, Jacob 2 Oct 2026) each side also has a nerve
    /// bar: hits and deaths drain it, the enemy's Unleash and Sacrifice drain it, and Hold drains
    /// the holder's own. A side at zero breaks and the fight ends there with its survivors alive.
    /// The nerve rules use no randomness, so with them off every fight is exactly as before.
    ///
    /// The slice's newer rules (Jacob, 2 and 3 Oct 2026) add two more switches on the setup:
    /// orders refill during the fight (a point back every RefillTicks ticks, up to the starting
    /// points) so the player stays busy, and HealthPercent makes units tougher so a fight lasts
    /// longer. Both are off by default and use no randomness either.
    ///
    /// The slice's big moves (Jacob, 4 Oct 2026, see Moves) come from the setup's side rules and
    /// unit roles: Eclipse winds up and can be soaked, dodged or interrupted, a Pal can Frenzy
    /// once a fight, orders fuse into combos, and a side whose Pal or Chief drops breaks on the
    /// spot. A unit whose gear frightens (UnitSpec.FearPercent) drains more courage with its hits.
    /// None of it draws on the random stream, and a fight without side rules, roles or fear
    /// never meets any of it.
    ///
    /// The 13 Oct 2026 rules (FightRules, see BattleSimulation.Skill.cs) and the tribes' doctrines
    /// (TacticsProfile) come from the setup too. Misses and nerve snaps draw on the random stream; a
    /// fight without fight rules or doctrines never meets any of it.
    public sealed partial class BattleSimulation : ISimulation
    {
        public const int HoldTicks = 150;
        public const int FallBackTicks = 75;
        public const int FocusTicks = 150;
        public const int UnleashTicks = 120;
        public const int HoldDamagePercent = 70;
        public const int UnleashDamagePercent = 150;
        public const int SacrificeHealPercent = 25;
        public const int MedicHealsBelowPermille = 900;
        public const int HabitThreshold = 3;
        public const int AvoidGuardedScore = 400;
        public const int OpenerTick = 15;
        public const string SacrificeDetail = "sacrifice";
        public const string TrapDetail = "trap";
        public const string GuardedDetail = "guarded";
        public const string CoverDetail = "Cover";
        public const string AutoActorPrefix = "auto";

        /// Nerve rules, placeholders. Losing a side's whole starting health to hits costs this
        /// percent of a full bar; each death costs NerveDeathCost on top.
        public const int NerveHitPercent = 70;
        public const int NerveDeathCost = 200;

        /// Scary orders: what the enemy's nerve loses when you Unleash or Sacrifice. A fight's
        /// nerve rules can price Unleash differently (NerveRules.UnleashCost).
        public const int UnleashNerveCost = 100;
        public const int SacrificeNerveCost = 150;

        /// Holding costs the holder this much nerve every tick it runs (150 for a full Hold), or
        /// every NerveRules.HoldDrainEveryTicks ticks when the fight's nerve rules say so.
        public const int HoldNervePerTick = 1;

        public const string BreakDetail = "break";

        private const ulong FnvOffset = 14695981039346656037UL;
        private const ulong FnvPrime = 1099511628211UL;

        private readonly BattleSetup _setup;
        private readonly TacticsProfile[] _tactics;
        private readonly SideRules[] _rules;
        private readonly DeterministicRng _rng;
        private readonly List<BattleUnit> _units = new List<BattleUnit>();
        private readonly List<BattleEvent> _events = new List<BattleEvent>();
        private readonly SideState[] _sides = { new SideState(), new SideState() };
        private readonly int[] _trapReadyIn;
        private readonly int[] _fear;
        private readonly bool _nerveOn;
        private readonly bool _movesOn;
        private readonly int _refillTicks;
        private readonly int _pointCap;
        private long _tick;
        private long _endTick = -1;
        private bool _finished;
        private int _winner = -1;
        private int _brokenSide = -1;
        private ulong _eventHash = FnvOffset;

        private sealed class SideState
        {
            public int Points;
            public int HoldTicks;
            public int FallBackTicks;
            public int FocusTicks;
            public int UnleashTicks;
            public string FocusTargetId = string.Empty;
            public bool TrapArmed;
            public string TrapOwnerId = string.Empty;
            // From rules 24 (BattleSimulation.Whisperer.cs): springs the armed trap has left (a rank 2 Trapper's springs twice), the
            // enemy who last hit one of this side and one of its back row, the one an upgraded Avenger owes a strike, and whom
            // its Medic last treated.
            public int TrapSprings;
            public string LastHitterId = string.Empty;
            public string LastBackHitterId = string.Empty;
            public string AvengeId = string.Empty;
            public string LastPatientId = string.Empty;
            public long AvengeTick = -1;
            public int ForcedWardTicks;
            public string ForcedWardId = string.Empty;
            // From rules 25 (BattleSimulation.Shade.cs): Sanctuary's field and its Relay pool, whether its Last Refuge is spent,
            // and how long Backstab's price keeps Fall Back waiting.
            public int FieldTicks;
            public int RelaySize;
            public bool RefugeUsed;
            public int FallBackLockTicks;
            // And the other five routes: Rupture's target and its steps, Echo recording, Rift Mine's mine and its return, and
            // Breach's exposure.
            public string RuptureTargetId = string.Empty;
            public int RuptureSteps;
            public bool EchoRecording;
            public int MineTicks;
            public int ReturnTicks;
            public int ExposedTicks;
            public int BreachTicks;
            // The duos (rules 27, BattleSimulation.Duos.cs): a free cast of the star's spell called this tick (1 Shadow Star,
            // 2 Crowned Marks, 0 none), and when Shadow Star can call the next.
            public int StarCastDue;
            public long ShadowStarAt;
            public int Nerve;
            public int StartHp;
            public int NerveRemainder;

            // Big moves (4 Oct 2026).
            public int Windup;
            public string CasterId = string.Empty;
            public int WindupDamage;
            public int WindupPercent;
            public bool WindupPyre;
            public string PyreTargetId = string.Empty;
            public long NextAutoEclipse;
            public bool Empowered;
            public int FrenzyTicks;
            public bool FrenzyUsed;
            public bool AmbushUsed;
            public bool PyreUsed;
            public long LastFallBackTick = -1;
            public long LastFocusTick = -1;
            public string LastFocusId = string.Empty;
            public long LastUnleashTick = -1;

            // The 13 Oct 2026 rules and doctrines (BattleSimulation.Skill.cs).
            public long LastHoldTick = -1;
            // The raid chief (BattleSimulation.Raid.cs, 25 Oct 2026): your last Hold's press, and the part of a point of posture
            // still to refill.
            public long LastHoldPress = -1;
            public int PostureCarry;
            public long LastParryTick = -1;
            public string ParriedCasterId = string.Empty;
            public int SnapsTaken;
            public int RallyTicks;
            public bool FormationHeld;

            // The 18 Oct 2026 heavy swings (BattleSimulation.Heavy.cs).
            public int HeavyWindup;
            public string HeavySwingerId = string.Empty;
            public string HeavyTargetId = string.Empty;
            public long HeavyNextTick;
            public int HeavyTurn;
            public bool HeavyWaited;
            public bool WindupWaited;

            // Chief spells (BattleSimulation.Spells.cs): ticks a Steel Storm wall still stands.
            public int WallTicks;

            // A band of hunters (BattleSimulation.Band.cs, 18 Oct 2026).
            public bool BannerUp;
            public int BerserkTicks;
            public bool BerserkUsed;
            public bool SecondWindUsed;
            public long MendNext;

            // A chief's kit (BattleSimulation.Kit.cs, 21 Oct 2026): on his side, his Cleave and Brand clocks, the Eclipses
            // since his Blood Sun and whether he has said his parry line; on the side facing him, its Glory, its last answer
            // and the Cleaves it parried in a row since his last Eclipse (the Sunbreaker chain).
            public int CleaveWindup;
            public string CleaveTargetId = string.Empty;
            public long CleaveNext;
            public bool CleaveWaited;
            // From rules 21 (BattleSimulation.Danger.cs): his last big move was a Cleave, so the next Cleave waits for a blast.
            public bool SwungLast;
            // From rules 22 (BattleSimulation.Meter.cs): the bars your parries have filled.
            public int ParryBars;
            public long BrandNext;
            public int SunEclipses;
            public bool SaidParry;
            public int Glory;
            public int LastAnswer;
            public int Chain;

            // The 22 Oct 2026 kit additions (BattleSimulation.Kit.cs, BattleSimulation.Skullpeak.cs). On a Bloodridge chief's
            // side: his Burning Ring (the spin left, the one whose lane it goes down, its clock), his Sun Mantle (the burn left,
            // its clock, what it burned, and whether the side facing him fell back or switched its Focus Fire to his band while
            // it burned) and his forecast, one slot per move in ForecastOrder (shown yet, its marks and how hard).
            public int RingSpin;
            public string RingTargetId = string.Empty;
            public long RingNext;
            public int MantleTicks;
            public long MantleNext;
            public int MantleBurned;
            public bool MantleFellBack;
            public bool MantleSwitched;
            public readonly bool[] ForeShown = new bool[ForecastSlots];
            public readonly string[] ForeTarget = { string.Empty, string.Empty, string.Empty, string.Empty, string.Empty };
            public readonly string[] ForeTarget2 = { string.Empty, string.Empty, string.Empty, string.Empty, string.Empty };
            public readonly int[] ForeAmount = new int[ForecastSlots];

            // On a Skullpeak chief's side: her wall (what your blows do on a half built one, the tick it last went up or was
            // whole again, whether one of her Wardens was snared while they locked), her Hook, her Rifle Line, her Iron
            // Advance, her Overclock, her Lockdown and her band's jobs. On the side facing her, Chain counts the walls it
            // smashed in their first second in a row (Breach).
            public int WallPercent;
            public long WallUpTick;
            public bool LockSnared;
            public int HookTell;
            public string HookTargetId = string.Empty;
            public long HookNext;
            public bool HookWaited;
            public bool HookGone;
            public int RifleTicks;
            public string RifleTargetId = string.Empty;
            public long RifleNext;
            public int RifleShooters;
            public int AdvanceTicks;
            public long AdvanceNext;
            public int AdvanceFull;
            public int AdvanceBraced;
            public int AdvanceGave;
            public int JabTicks;
            public string JabMedicId = string.Empty;
            public int OverclockTicks;
            public long OverclockNext;
            public bool OverclockSnared;
            public bool OverclockHeld;
            public bool Lockdown;
            public int OpenTicks;
            public int RebuildTicks;
            public string ShieldbearerId = string.Empty;
            public string SpotterId = string.Empty;

            // On a Stoneveil chief's side (BattleSimulation.Stoneveil.cs, 22 Oct 2026, later): the wounds your Medic stitched
            // during his chant; his Basin (the carry left, its clock, who carries it, the drops in it) and his drink; his Leeches
            // (left, clock, whom they drink from, what they drank); his Open Veins (left, clock, and whether a wound was stitched,
            // anyone bled or the other side held meanwhile); his Transfusion (left, clock, whom from, what it drew); his Blood
            // Price's drops; and whether your Clean Hands broke his rite. On the side facing him, Chain counts his moves that
            // landed in a row with nobody of yours bleeding.
            public int ChantStitches;
            public int BasinTicks;
            public long BasinNext;
            public string BasinCarrierId = string.Empty;
            public int BasinFill;
            public int DrinkTicks;
            public int LeechTicks;
            public long LeechNext;
            public string LeechTargetId = string.Empty;
            public int LeechDrunk;
            public int VeinsTicks;
            public long VeinsNext;
            public bool VeinsStitched;
            public bool VeinsBled;
            public bool VeinsHeld;
            public int DrawTicks;
            public long DrawNext;
            public string DrawTargetId = string.Empty;
            public int DrawDrawn;
            public int PriceDrops;
            public bool CleanHands;

            // On a Thundercoast chief's side (BattleSimulation.Thundercoast.cs): the tide's clock (ticks into its cycle, ticks it
            // holds, the levels his Sluice Gates added, the Storm Surge's and your drain's ticks left, the level last told); his
            // Sluice Gate (crank left, clock, engineer); his Riptide (left, clock, whether the other side held); his Tidal Bore
            // (left, clock, which way, how far, and its full, soaked and dodged reaches); his Storm Surge (horn left, clock, and
            // whether the other side held through the high water). On the side facing him, Chain counts his moves answered in a
            // row at high water.
            public int TidePhase;
            public int TideHold;
            public int TideGate;
            public int SurgeTicks;
            public int DrainTicks;
            public int TideShown;
            public int CrankTicks;
            public long SluiceNext;
            public string EngineerId = string.Empty;
            public int RipTicks;
            public long RipNext;
            public bool RipHeld;
            public int BoreTicks;
            public long BoreNext;
            public bool BoreFromRight;
            public int BoreFront;
            public int BoreFull;
            public int BoreSoaked;
            public int BoreDodged;
            public int HornTicks;
            public long SurgeNext;
            public bool SurgeHeld;

            // The parry at the center (BattleSimulation.Parry.cs, 24 Oct 2026): this side's parries in a row since a move last
            // landed on it in full, and what each of its moves' beats has done so far (BeatSlot).
            public int ParryChain;
            public readonly int[] Beats = new int[BeatSlots];

            // The beats the 22 Oct moves did not have (ParryRules.TellTicks after their tells): ticks until his Sun Mantle flares.
            // And how long her Iron Advance holds a crush that waits or feints.
            public int MantleFlare;
            public int AdvanceHold;

            // The Stoneveil chief's: how long his drink waits or feints, and ticks until his Leeches leap, his cut opens your
            // veins and his Transfusion latches.
            public int BasinHold;
            public int LeechLeap;
            public int VeinsCut;
            public int DrawLatch;

            // The Thundercoast chief's: how long his Riptide's yank and his Tidal Bore's first lane wait or feint.
            public int RipHold;
            public int BoreHold;
        }

        public BattleSimulation(BattleSetup setup)
        {
            _setup = setup ?? throw new ArgumentNullException(nameof(setup));
            _tactics = new[] { setup.CopyTactics(0), setup.CopyTactics(1) };
            _rules = new[] { setup.Rules(0), setup.Rules(1) };
            _rng = DeterministicRng.ForStream(setup.Seed, SimStream.Battle);

            int index = 0;
            bool special = false;
            var fear = new List<int>();
            for (int side = 0; side < 2; side++)
            {
                IReadOnlyList<UnitSpec> specs = setup.Side(side);
                for (int i = 0; i < specs.Count; i++)
                {
                    UnitSpec spec = specs[i];
                    if (spec.Id.Length == 0) throw new ArgumentException("Every unit needs an id.");
                    if (FindById(spec.Id) != null) throw new ArgumentException("Duplicate unit id " + spec.Id + ".");
                    _units.Add(new BattleUnit(spec, side, index++, setup.HealthPercent));
                    fear.Add(spec.FearPercent);
                    if (spec.Role != UnitRole.Fighter || spec.PowerPercent != UnitSpec.PlainPercent ||
                        spec.HealthPercent != UnitSpec.PlainPercent || spec.FearPercent != 0)
                        special = true;
                }
            }
            _trapReadyIn = new int[_units.Count];
            _fear = fear.ToArray();
            _movesOn = special || !_rules[0].IsNone || !_rules[1].IsNone;
            _fight = setup.Fight;
            _skillOn = _fight.AnySkill || _tactics[0].AnyDoctrine || _tactics[1].AnyDoctrine;
            // The parry at the center (BattleSimulation.Parry.cs, 24 Oct 2026): every move with a tell has a beat to parry.
            _parryOn = _fight.ParryCenter;
            // The squad first on top of it (BattleSimulation.Parry.cs, campaign rules version 9).
            _squadOn = _parryOn && _fight.SquadFirst;
            // A champion duel on top of that (DESIGN_LOCKS "Duels", campaign rules version 16).
            _duelOn = _squadOn && _fight.ChampionDuel;
            // The raid chief on top of that (BattleSimulation.Raid.cs, campaign rules version 10).
            _raidOn = _squadOn && _fight.RaidChief;
            // Road combat on top of that (BattleSimulation.Road.cs, campaign rules version 11).
            _roadCombatOn = _raidOn && _fight.RoadCombat;
            // The road kit on top of that (BattleSimulation.Boosts.cs, campaign rules version 12).
            _roadKitOn = _roadCombatOn && _fight.RoadKit;
            // The class spells (BattleSimulation.Class.cs, 20 Oct 2026). The Trapper's snare bleeds, so the bleeding runs too.
            _classOn = _fight.ClassSpells;
            // The floor under the three Pals (BattleSimulation.Shade.cs, campaign rules 23), before the combos look for a second piece.
            _floorOn = _fight.Floor;
            // The Whisperer (BattleSimulation.Whisperer.cs, campaign rules 24) rides on the floor.
            _whisperOn = _floorOn && _fight.Whisper;
            // The Shade's forms (BattleSimulation.Shade.cs, campaign rules 25) ride on the floor too.
            _shadeOn = _floorOn && _fight.ShadeForms;
            // The Glutton's talents (BattleSimulation.Glutton.cs, campaign rules 26) ride on the floor too.
            _gluttonOn = _floorOn && _fight.Glutton;
            // The Pals' duos (BattleSimulation.Duos.cs, campaign rules 27) too.
            _duosOn = _floorOn && _fight.Duos;
            // The sizing pass (BattleSimulation.Sized.cs, campaign rules 28) too.
            _sizedOn = _floorOn && _fight.Sized;
            // The Butcher's flavours (BattleSimulation.Flavours.cs, campaign rules 29) too.
            _flavoursOn = _floorOn && _fight.Flavours;
            // The Butcher's combos (BattleSimulation.Combos.cs, 29 Oct 2026): only a fight where someone wears one plays them.
            _comboOn = _classOn && AnyCombo();
            // The damage layers (BattleSimulation.Layers.cs, campaign rules 17): every hit and heal through one pipeline.
            _layersOn = _fight.Layers;
            // The Butcher's marks (BattleSimulation.Marks.cs, campaign rules 18): only where someone wears a combo to lay them.
            _marksOn = _comboOn && _fight.Marks;
            // The Whisperer's vows (BattleSimulation.Vows.cs) and the Butcher's marks on every hit (BattleSimulation.Flavours.cs),
            // campaign rules 30.
            _vowsOn = _floorOn && _fight.Whisper && _fight.Vows;
            _marksOnHitsOn = _marksOn && _floorOn && _fight.MarksOnHits;
            // Payoffs that spread (BattleSimulation.Payoffs.cs, campaign rules 20).
            _payoffsOn = _fight.Payoffs;
            // Danger (BattleSimulation.Danger.cs, campaign rules 21).
            _dangerOn = _fight.Danger;
            // The parry meter (BattleSimulation.Meter.cs, campaign rules 22).
            _meterOn = _fight.Meter;
            // The chief rules (BattleSimulation.Chief.cs, 20 Oct 2026): his reserves wait out of the fight.
            _chiefOn = _fight.ChiefRules;
            if (_chiefOn) BenchReserves();
            // The chief kits and the kneel (BattleSimulation.Kit.cs, 21 Oct 2026), only with the chief rules.
            _kitsOn = _chiefOn && _fight.ChiefKits;
            // Their tactical pieces, and the 22 Oct 2026 kit additions: Bloodridge's Burning Ring and Sun Mantle, and the
            // Skullpeak chief's kit (BattleSimulation.Skullpeak.cs). A fight with none of them plays as a 21 Oct kit fight did.
            _tacticsOn = _kitsOn && _fight.ChiefTactics;
            _kitsTwoOn = _kitsOn && (_tacticsOn || KitAfterFirst(0) || KitAfterFirst(1));
            // The hunt (BattleSimulation.Hunt.cs, 22 Oct 2026): a side whose kit is the hunters' captain's.
            _huntSide = !_kitsOn ? -1 : _rules[1].Kit == ChiefKit.Hunt ? 1 : _rules[0].Kit == ChiefKit.Hunt ? 0 : -1;
            _huntOn = _huntSide >= 0;
            // The Stoneveil chief's blood rite and the Thundercoast chief's tide (BattleSimulation.Stoneveil.cs and
            // BattleSimulation.Thundercoast.cs, 22 Oct 2026, later): a side whose kit is one of theirs.
            _riteOn = _kitsOn && (_rules[0].Kit == ChiefKit.Stoneveil || _rules[1].Kit == ChiefKit.Stoneveil);
            _tideSide = !_kitsOn ? -1 : _rules[1].Kit == ChiefKit.Thundercoast ? 1 : _rules[0].Kit == ChiefKit.Thundercoast ? 0 : -1;
            _tideOn = _tideSide >= 0;
            // A beast fight (BattleSimulation.Beast.cs, 21 Oct 2026): one side is a beast's parts.
            _beastSide = !_fight.Beasts ? -1 : _rules[1].Beast != BeastKit.None ? 1 : _rules[0].Beast != BeastKit.None ? 0 : -1;
            _beastOn = _beastSide >= 0;
            _beastKit = _beastOn ? _rules[_beastSide].Beast : BeastKit.None;

            _sides[0].Points = setup.StartingDirectivePoints;
            _sides[1].Points = setup.StartingDirectivePoints;
            _refillTicks = setup.RefillTicks;
            _pointCap = setup.StartingDirectivePoints;

            _nerveOn = setup.Nerve.Enabled;
            // In a road combat road fight every fighter has his own nerve (BattleSimulation.Road.cs).
            _unitNerveOn = _roadCombatOn && _nerveOn && !_chiefOn && !_beastOn;
            if (_unitNerveOn) StartUnitNerve();
            if (_roadCombatOn) StartRoadCombat();
            if (_roadKitOn) StartRoadKit();
            // Borrowed time, the Time Stop combos' pact, before the sides count their health.
            if (_comboOn) BorrowedTime();
            for (int i = 0; i < _units.Count; i++) if (!_units[i].Benched) _sides[_units[i].Side].StartHp += _units[i].MaxHp;
            _sides[0].Nerve = setup.Nerve.StartNerve(0);
            _sides[1].Nerve = setup.Nerve.StartNerve(1);
            // Unnerving, the Fold Door combos' pact, once the nerve has started, and the combos' own clocks.
            if (_comboOn) StartCombos();
            _sides[0].NextAutoEclipse = _rules[0].AutoEclipseTicks;
            _sides[1].NextAutoEclipse = _rules[1].AutoEclipseTicks;
            _heavyOn = _rules[0].HeavyEveryTicks > 0 || _rules[1].HeavyEveryTicks > 0;
            _spellsOn = _rules[0].Spell != ChiefSpell.Eclipse || _rules[1].Spell != ChiefSpell.Eclipse;
            _sides[0].HeavyNextTick = _rules[0].HeavyEveryTicks;
            _sides[1].HeavyNextTick = _rules[1].HeavyEveryTicks;
            _bandOn = _rules[0].Band != BandTrait.None || _rules[1].Band != BandTrait.None;
            // The new tribes (18 Oct 2026): bleeding blades or a blood call, and a side that floods the fight. The Thundercoast
            // chief's tide floods it whatever his rules say.
            _floodOn = _rules[0].Flood || _rules[1].Flood || _tideOn;
            // A beast's Blood frenzy leaves its victims bleeding (BattleSimulation.Beast.cs).
            _bleedOn = _rules[0].Spell == ChiefSpell.BloodCall || _rules[1].Spell == ChiefSpell.BloodCall || _classOn || _beastOn;
            for (int i = 0; i < _units.Count; i++) if (SpecOf(_units[i]).BleedPercent > 0) _bleedOn = true;
            for (int s = 0; s < 2; s++)
            {
                _sides[s].BannerUp = (_rules[s].Band & BandTrait.Banner) != 0;
                _sides[s].MendNext = (_rules[s].Band & BandTrait.Mender) != 0 ? BandRules.MendEveryTicks : 0;
            }

            // Stagger first actions so both sides do not open in lockstep.
            for (int i = 0; i < _units.Count; i++)
                _units[i].Cooldown = 1 + _rng.Next(_units[i].Stats.CooldownTicks);
            if (_classOn) StartSpellClocks();
            if (_kitsOn) StartKitClocks();
            if (_beastOn) StartBeast();
        }

        public BattleSetup Setup => _setup;
        public long CurrentTick => _tick;
        public bool IsFinished => _finished;

        /// 0 or 1 once finished, -1 for a draw or while running.
        public int Winner => _winner;

        /// Tick on which the fight ended, or -1 while running.
        public long EndTick => _endTick;

        public IReadOnlyList<BattleUnit> Units => _units;
        public IReadOnlyList<BattleEvent> Events => _events;
        public ulong EventHash => _eventHash;

        /// True when this fight runs under the nerve rules.
        public bool NerveOn => _nerveOn;

        /// The side whose nerve broke, or -1. Only a fight under the nerve rules can end in a break,
        /// unless a side's Pal or Chief drops.
        public int BrokenSide => _brokenSide;

        /// A side's nerve in thousandths of a full bar. Stays full when the nerve rules are off.
        public int Nerve(int side)
        {
            // In a road combat road fight there is no shared bar: this is the sum of the nerve still standing on that side.
            if (_unitNerveOn)
            {
                int sum = 0;
                for (int i = 0; i < _units.Count; i++) if (_units[i].Alive && _units[i].Side == side) sum += _units[i].UnitNerve;
                return sum;
            }
            return _sides[side].Nerve;
        }

        public int DirectivePoints(int side)
        {
            return _sides[side].Points;
        }

        /// Percent added to the courage this unit's hits drain. 0 unless its gear frightens.
        public int FearPercent(BattleUnit unit)
        {
            return _fear[unit.Index];
        }

        /// A side's big moves for this fight.
        public SideRules Rules(int side)
        {
            return _rules[side];
        }

        /// Ticks left before a side's wound up move goes off, or 0 when nothing is winding up.
        public int WindupTicks(int side)
        {
            return _sides[side].Windup;
        }

        /// The unit winding up a side's move, or empty.
        public string WindupCasterId(int side)
        {
            return _sides[side].CasterId;
        }

        /// True when the move winding up finishes a Pyre.
        public bool WindupIsPyre(int side)
        {
            return _sides[side].WindupPyre;
        }

        /// The Pyre's target while one is winding up, or empty.
        public string PyreTargetId(int side)
        {
            return _sides[side].PyreTargetId;
        }

        /// The side interrupted a move and its next order is doubled.
        public bool IsEmpowered(int side)
        {
            return _sides[side].Empowered;
        }

        /// Ticks of Frenzy left, or 0.
        public int FrenzyTicksLeft(int side)
        {
            return _sides[side].FrenzyTicks;
        }

        public bool FrenzyUsed(int side)
        {
            return _sides[side].FrenzyUsed;
        }

        /// True once the side has made this combo (Moves.Ambush or Moves.Pyre) this fight.
        public bool ComboUsed(int side, string combo)
        {
            if (string.Equals(combo, Moves.Ambush, StringComparison.Ordinal)) return _sides[side].AmbushUsed;
            if (string.Equals(combo, Moves.Pyre, StringComparison.Ordinal)) return _sides[side].PyreUsed;
            return false;
        }

        /// The side's living Pal or Chief, or null. Eclipse is cast by this unit.
        public BattleUnit Leader(int side)
        {
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (u.Alive && u.Side == side && u.IsLeader) return u;
            }
            return null;
        }

        public BattleUnit FindById(string id)
        {
            for (int i = 0; i < _units.Count; i++)
            {
                if (string.Equals(_units[i].Id, id, StringComparison.Ordinal)) return _units[i];
            }
            return null;
        }

        public int AliveCount(int side)
        {
            int count = 0;
            for (int i = 0; i < _units.Count; i++)
            {
                if (_units[i].Side == side && _units[i].Alive) count++;
            }
            return count;
        }

        /// True while a Warden on the unit's own side is covering it.
        public bool IsGuarded(BattleUnit unit)
        {
            return GuardingWarden(unit) != null;
        }

        /// Whether a side could give this order now, targets aside. The same checks TryApply makes.
        public bool CanUse(int side, Directive directive, out string failure)
        {
            failure = string.Empty;
            if (_finished)
            {
                failure = "The battle is over.";
                return false;
            }
            // While a chief kneels only the verdict goes, and it is free: Hold spares him, Focus Fire on him executes him.
            if (_halted)
            {
                BattleUnit chief = KneelingChief;
                if (chief != null && side != chief.Side && (directive == Directive.Hold || directive == Directive.FocusFire)) return true;
                failure = ChiefKits.KneelFailure;
                return false;
            }
            // What the order needs comes before what it costs, so the reason given is the real one.
            SideState state = _sides[side];
            if (directive == Directive.Eclipse)
            {
                if (_rules[side].EclipseGrade <= 0)
                {
                    failure = "Eclipse is not yours yet.";
                    return false;
                }
                if (Leader(side) == null)
                {
                    failure = "Eclipse needs a living leader to cast it.";
                    return false;
                }
                if (state.Windup > 0)
                {
                    failure = "Eclipse is already winding up.";
                    return false;
                }
                // From rules 22 your chief spell is cast with a full parry meter (BattleSimulation.Meter.cs).
                if (MeterFor(side) && state.ParryBars < MeterRules.Bars)
                {
                    failure = "Needs " + MeterRules.Bars + " parry bars, has " + state.ParryBars + ".";
                    return false;
                }
            }
            else if (directive == Directive.FallBack && _shadeOn && state.FallBackLockTicks > 0)
            {
                // Backstab's price (rules 25, BattleSimulation.Shade.cs).
                failure = "Fall Back is back in " + SecondsLeft(state.FallBackLockTicks) + ".";
                return false;
            }
            else if (directive == Directive.Frenzy)
            {
                if (LivingPal(side) == null)
                {
                    failure = "Frenzy needs a living Pal.";
                    return false;
                }
                if (state.FrenzyUsed)
                {
                    failure = "Frenzy is used up this fight.";
                    return false;
                }
            }
            int cost = CostOf(side, directive);
            if (state.Points < cost)
            {
                failure = "Not enough directive points: need " + cost + ", have " + state.Points + ".";
                return false;
            }
            return true;
        }

        /// Runs ticks until the fight ends or the tick limit passes. Returns the tick count reached.
        public long RunUntilFinished()
        {
            while (!_finished && _tick < _setup.TickLimit) Tick();
            return _tick;
        }

        public void Tick()
        {
            // A chief kneels (BattleSimulation.Kit.cs, 21 Oct 2026): nothing moves until the other side spares or executes
            // him. The screen holds the clock here; if it runs out anyway, he is spared.
            if (_halted && !_finished)
            {
                if (_tick + 1 >= _setup.TickLimit) TimeSpare();
                _tick++;
                return;
            }
            if (!_finished)
            {
                if (_tick == OpenerTick)
                {
                    for (int s = 0; s < 2; s++) TryOpener(s);
                }

                // A chief casts his Eclipse on his own schedule, once he has the points.
                for (int s = 0; s < 2 && !Stopped; s++) TryAutoEclipse(s);

                // Heavy swings start on their own schedule too, one at a time (HeavyRules, 18 Oct 2026).
                if (_heavyOn)
                    for (int s = 0; s < 2 && !Stopped; s++) TryStartHeavy(s);

                // So do a chief's own moves (BattleSimulation.Kit.cs, 21 Oct 2026), and from 22 Oct his newer ones and her kit
                // (BattleSimulation.Skullpeak.cs); then whatever is due within 6 s shows (Bloodridge's forecast).
                if (_kitsOn)
                    for (int s = 0; s < 2 && !Stopped; s++)
                    {
                        TryStartCleave(s);
                        TryStartBrand(s);
                        if (!_kitsTwoOn) continue;
                        StartKitMoves(s);
                        if (_tacticsOn && !Stopped) Forecast(s);
                    }

                // A beast's parts start their moves on their own clocks (BattleSimulation.Beast.cs, 21 Oct 2026).
                if (_beastOn && !Stopped) StartBeastMoves();

                // Alternate the scan direction each tick so neither side always acts first. A kneeling chief stops it
                // (Stopped is the end of the fight, or that).
                bool forward = (_tick & 1L) == 0L;
                for (int n = 0; n < _units.Count && !Stopped; n++)
                {
                    BattleUnit unit = _units[forward ? n : _units.Count - 1 - n];
                    if (!unit.Alive) continue;
                    if (_beastOn && unit.Side == _beastSide) continue; // a beast's parts only move by their clocks
                    if (_beastOn && IsHeld(unit)) continue; // in the cage bear's claw, out of the fight
                    if (unit.PanicTicks > 0) continue; // a panicking fighter does nothing (FightRules.NerveSnaps)
                    if (_heavyOn && IsSwingingHeavy(unit)) continue; // winding up a heavy swing, nothing else
                    if (_chiefOn && unit.Yielded) continue; // a chief who yielded does nothing more (BattleSimulation.Chief.cs)
                    if (_kitsOn && KitBusy(unit)) continue; // stunned, or busy with a move of his kit (BattleSimulation.Kit.cs)
                    if (_classOn && !SpellClock(unit)) continue; // held by a snare, or aiming (BattleSimulation.Class.cs)
                    if (_parryOn && Braced(unit)) continue; // a Thick Hide hunter you focus braces and cannot swing (BattleSimulation.Band.cs)
                    if (_kitsTwoOn && _sides[unit.Side].OverclockTicks > 0) unit.Cooldown--; // her Overclock: twice as fast
                    if (HalfSpeed(unit)) continue; // a Time Snare's half speed (BattleSimulation.Combos.cs)
                    unit.Cooldown--;
                    if (unit.Cooldown > 0) continue;
                    Act(unit);
                    // Heavy slows a Gravity Well Warden's and Medic's turns (BattleSimulation.Combos.cs).
                    if (unit.Alive) unit.Cooldown += HeavyTurn(unit, NextCooldown(unit));
                    CheckWipe();
                }

                // The Whisperer's upgraded Rescuers and Avengers (BattleSimulation.Whisperer.cs, rules 24).
                if (_whisperOn && !Stopped) TickWhisper();
                // A star's spell a duo called (BattleSimulation.Duos.cs, rules 27).
                if (_duosOn && !Stopped) TickDuos();

                // Class spells (BattleSimulation.Class.cs, 20 Oct 2026): aims run on, and a finished one fires.
                if (_classOn && !Stopped) AdvanceAims();

                // The new tribes (BattleSimulation.Tribes.cs, 18 Oct 2026): wounds that bleed, and the water rising.
                if (_bleedOn && !Stopped) TickBleeding();
                if (_floodOn && _nerveOn && !Stopped) TickFlood();
                // A chief who yielded at a quarter health costs his side heart (BattleSimulation.Chief.cs); the chief kits'
                // kneel replaces that yield.
                if (_chiefOn && !_kitsOn && _nerveOn && !Stopped) TickYield();

                // Holding costs nerve: the longer a side digs in, the shakier it gets. Unleashing costs half
                // that while it runs (Jacob, 11 Oct 2026: call J3, then "halve the cost"): spamming it wears
                // your own side down in a long fight, while a timed Unleash still pays. A starting value. Under road combat a road
                // fight has no such drain (BattleSimulation.Road.cs).
                if (_nerveOn && !_unitNerveOn)
                {
                    for (int s = 0; s < 2 && !Stopped; s++)
                    {
                        int every = _setup.Nerve.HoldDrainEveryTicks;
                        if (_tick % every != 0) continue;
                        // The Glutton's and the Butcher's favours skip quarters of these drains (SideFavours, 18 Oct 2026).
                        if (_sides[s].HoldTicks > 0 && !Skipped(_tick / every, _rules[s].Favours.HoldDrainCut))
                            DrainNerve(s, HoldNervePerTick, s);
                        if (_sides[s].UnleashTicks > 0 && !Stopped && _tick % (2 * every) == 0 &&
                            !Skipped(_tick / (2 * every), _rules[s].Favours.UnleashDrainCut))
                            DrainNerve(s, HoldNervePerTick, s);
                    }
                }

                // A raid chief's bar refills like posture (BattleSimulation.Raid.cs).
                if (_raidOn && _nerveOn && !Stopped) TickPosture();
                // A frenzy that has run its time ends in collapse (BattleSimulation.Road.cs).
                if (_unitNerveOn && !Stopped) TickRoad();
                // Every mark set this tick gets its casher's reaction (BattleSimulation.Chains.cs), and the defence moves come
                // a tick closer (BattleSimulation.Defence.cs).
                // Burn and the carried moves run on (the road kit, BattleSimulation.Burn.cs and Carriers.cs).
                if (_roadKitOn && !Stopped)
                {
                    TickBurn();
                    TickCarriers();
                }
                if (_roadCombatOn && !Stopped) ResolveReactions();
                if (_roadCombatOn) TickDefences();

                // Refilling orders: a point back every RefillTicks ticks, up to the starting points. Once
                // points are earned (FightRules.EarnedPoints) this is only a slow trickle.
                if (_refillTicks > 0 && !Stopped && (_tick + 1) % RefillEvery == 0)
                {
                    for (int s = 0; s < 2; s++)
                    {
                        if (_sides[s].Points < _pointCap) _sides[s].Points++;
                    }
                }

                // Wound up moves go off once their windup has run, before this tick's effects
                // count down, so an order in force on this tick still counts against them.
                for (int s = 0; s < 2 && !Stopped; s++) AdvanceWindup(s);
                if (_heavyOn)
                    for (int s = 0; s < 2 && !Stopped; s++) AdvanceHeavy(s);
                if (_kitsOn)
                    for (int s = 0; s < 2 && !Stopped; s++)
                    {
                        AdvanceCleave(s);
                        if (!Stopped) AdvanceBrands(s);
                        if (_kitsTwoOn && !Stopped) AdvanceKitMoves(s);
                    }
                if (_beastOn && !Stopped) AdvanceBeast();

                // A hunter who mends patches up the band on their own schedule (BandTrait.Mender).
                if (_bandOn)
                    for (int s = 0; s < 2 && !Stopped; s++) TryMend(s);

                // Counted down after the tick's actions, so an order given on tick t is in force
                // for exactly its length: ticks t to t + length - 1.
                for (int s = 0; s < 2; s++) CountDownEffects(_sides[s]);
                if (_parryOn) CountDownReel();
                if (_skillOn) CountDownSnaps();
                if (_spellsOn) CountDownWalls();
                if (_bandOn) CountDownBand();
                if (_classOn) CountDownMarks();
                if (_comboOn) CountDownCombos();
                if (_marksOn) CountDownTheme();
                if (_floorOn) CountDownFloor();
                if (_shadeOn) CountDownShade();
                if (_kitsOn) CountDownKit();

                if (!_finished && _tick + 1 >= _setup.TickLimit)
                {
                    if (_halted) TimeSpare();
                    else EndOnTime();
                }
            }
            _tick++;
        }

        public bool TryApply(SimCommand command, out string failure)
        {
            failure = string.Empty;
            if (command == null)
            {
                failure = "Command is null.";
                return false;
            }
            if (!string.Equals(command.Kind, Directives.CommandKind, StringComparison.Ordinal))
            {
                failure = "Unknown command kind " + command.Kind + ".";
                return false;
            }
            if (_finished)
            {
                failure = "The battle is over.";
                return false;
            }

            int side;
            if (string.Equals(command.ActorId, "0", StringComparison.Ordinal)) side = 0;
            else if (string.Equals(command.ActorId, "1", StringComparison.Ordinal)) side = 1;
            else
            {
                failure = "Directive actor must be side 0 or 1.";
                return false;
            }

            if (!Directives.TryParse(command.TextArg, out Directive directive))
            {
                failure = "Unknown directive " + command.TextArg + ".";
                return false;
            }
            if (TryApplyOrder(side, directive, command, out failure)) return true;
            // Kept for the screen (LastRejection), never part of the fight.
            _rejected[side] = new OrderRejection(directive, _tick, command.TargetId, failure);
            return false;
        }

        // A parsed order from a side: the verdict while a chief kneels, else the order if it can go.
        private bool TryApplyOrder(int side, Directive directive, SimCommand command, out string failure)
        {
            // A kneeling chief waits for the verdict, and nothing else goes (BattleSimulation.Kit.cs).
            if (_halted)
                return TryVerdict(side, directive, command.TargetId, "side" + side.ToString(CultureInfo.InvariantCulture), out failure);

            // The Shade's follow up presses (rules 25, BattleSimulation.Shade.cs): free, and never the plain order.
            if (_shadeOn)
            {
                int followed = FollowUp(side, directive, command.TargetId, out failure);
                if (followed > 0) return true;
                if (followed < 0) return false;
            }

            if (!CanUse(side, directive, out failure)) return false;

            BattleUnit target = null;
            if (directive == Directive.FocusFire)
            {
                target = FindById(command.TargetId);
                if (target == null || !target.Alive || target.Side == side)
                {
                    failure = "Focus fire needs a living enemy target.";
                    return false;
                }
                // The cage bear's heart cannot be hit while its ribcage stands (BattleSimulation.Beast.cs).
                if (_beastOn && Shielded(target))
                {
                    failure = "Its heart is behind its ribcage.";
                    return false;
                }
            }
            else if (directive == Directive.Sacrifice)
            {
                target = FindById(command.TargetId);
                if (target == null || !target.Alive || target.Side != side)
                {
                    failure = "Sacrifice needs a living unit on your side.";
                    return false;
                }
                if (target.IsLeader)
                {
                    failure = "A leader cannot be sacrificed.";
                    return false;
                }
                if (AliveCount(side) < 2)
                {
                    failure = "Sacrifice needs another living ally to save.";
                    return false;
                }
            }

            int at = _events.Count;
            ApplyDirective(side, directive, target, "side" + side.ToString(CultureInfo.InvariantCulture));
            // A Hold given while the fight was paused still soaks, but it can never parry: the parry is a live
            // press (Jacob, 18 Oct 2026). Only in fights with heavy swings, or a beast's Bite to parry (the audit, 22 Oct
            // 2026), so older fights replay as they were; under the parry at the center in every fight (24 Oct 2026).
            if ((_heavyOn || _beastOn || _parryOn) && directive == Directive.Hold && command.Arg0 == HeavyRules.PausedOrderArg)
                _sides[side].LastHoldTick = -1;
            // Which orders were given while paused, for their results (BattleSimulation.Orders.cs). Not in the snapshot.
            if (command.Arg0 == HeavyRules.PausedOrderArg && at < _events.Count) _pausedOrders.Add(at);
            return true;
        }

        public string OrderedStateSnapshot
        {
            get
            {
                var b = new SnapshotBuilder();
                b.Section("battle");
                b.Field("fight", _setup.FightId);
                b.Field("tick", _tick);
                b.Field("finished", _finished);
                b.Field("winner", _winner);
                b.Field("endTick", _endTick);
                b.Field("rng", _rng.State);
                // Written only under road combat, so every older fight keeps its checksum.
                if (_roadCombatOn)
                {
                    b.Field("unitNerve", _unitNerveOn);
                    b.Field("bonds", _setup.Bonds.Count);
                    for (int i = 0; i < _setup.Bonds.Count; i++) b.Field("bond" + i.ToString(CultureInfo.InvariantCulture),
                        _setup.Bonds[i].OriginId + ">" + _setup.Bonds[i].CasherId);
                }
                if (_nerveOn)
                {
                    b.Field("nerveOn", true);
                    b.Field("brokenSide", _brokenSide);
                    if (_setup.Nerve.UnleashCost != UnleashNerveCost || _setup.Nerve.HoldDrainEveryTicks != 1)
                    {
                        b.Field("unleashNerve", _setup.Nerve.UnleashCost);
                        b.Field("holdDrainEvery", _setup.Nerve.HoldDrainEveryTicks);
                    }
                }
                if (_refillTicks > 0) b.Field("refill", _refillTicks);
                if (_setup.HealthPercent != BattleSetup.NormalHealthPercent) b.Field("health", _setup.HealthPercent);
                if (!_fight.IsNone) _fight.AppendTo(b);
                // Written only in fights with the chief kits, so every older fight keeps its checksum.
                if (_kitsOn) b.Field("halted", _halted);
                // And the beast's clocks only in beast fights, the hunts only in the hunt.
                if (_beastOn) AppendBeast(b);
                if (_huntOn) AppendHunt(b);
                if (_roadCombatOn) AppendChains(b);
                for (int s = 0; s < 2; s++)
                {
                    SideState state = _sides[s];
                    string name = "side" + s.ToString(CultureInfo.InvariantCulture);
                    b.Section(name);
                    b.Field("points", state.Points);
                    b.Field("hold", state.HoldTicks);
                    b.Field("fallBack", state.FallBackTicks);
                    b.Field("focus", state.FocusTicks);
                    b.Field("focusTarget", state.FocusTargetId);
                    b.Field("unleash", state.UnleashTicks);
                    b.Field("trapArmed", state.TrapArmed);
                    b.Field("trapOwner", state.TrapOwnerId);
                    b.Field("ward", state.ForcedWardTicks);
                    b.Field("wardId", state.ForcedWardId);
                    if (_whisperOn) AppendWhisperSide(b, state);
                    if (_shadeOn) AppendShadeSide(b, state);
                    if (_duosOn) AppendDuoSide(b, state);
                    if (_nerveOn)
                    {
                        b.Field("nerve", state.Nerve);
                        b.Field("nerveRem", state.NerveRemainder);
                    }
                    // Written only under the raid chief, so every older fight keeps its checksum.
                    if (_raidOn)
                    {
                        b.Field("holdPress", state.LastHoldPress);
                        b.Field("postureCarry", state.PostureCarry);
                    }
                    if (_movesOn)
                    {
                        _rules[s].AppendTo(b, name + ".");
                        b.Field("windup", state.Windup);
                        b.Field("caster", state.CasterId);
                        b.Field("windupDamage", state.WindupDamage);
                        b.Field("windupPercent", state.WindupPercent);
                        b.Field("windupPyre", state.WindupPyre);
                        b.Field("pyreTarget", state.PyreTargetId);
                        b.Field("nextAutoEclipse", state.NextAutoEclipse);
                        b.Field("empowered", state.Empowered);
                        b.Field("frenzy", state.FrenzyTicks);
                        b.Field("frenzyUsed", state.FrenzyUsed);
                        b.Field("ambushUsed", state.AmbushUsed);
                        b.Field("pyreUsed", state.PyreUsed);
                        b.Field("lastFallBack", state.LastFallBackTick);
                        b.Field("lastFocus", state.LastFocusTick);
                        b.Field("lastFocusId", state.LastFocusId);
                        b.Field("lastUnleash", state.LastUnleashTick);
                    }
                    _tactics[s].AppendTo(b, name + ".");
                    if (_skillOn)
                    {
                        b.Field("lastHold", state.LastHoldTick);
                        b.Field("lastParry", state.LastParryTick);
                        b.Field("parriedCaster", state.ParriedCasterId);
                        b.Field("snaps", state.SnapsTaken);
                        b.Field("rally", state.RallyTicks);
                        b.Field("formationHeld", state.FormationHeld);
                    }
                    // Written only in fights with heavy swings, so every older fight keeps its checksum.
                    if (_heavyOn)
                    {
                        b.Field("heavy", state.HeavyWindup);
                        b.Field("heavySwinger", state.HeavySwingerId);
                        b.Field("heavyTarget", state.HeavyTargetId);
                        b.Field("heavyNext", state.HeavyNextTick);
                        b.Field("heavyTurn", state.HeavyTurn);
                        b.Field("heavyWaited", state.HeavyWaited);
                        b.Field("windupWaited", state.WindupWaited);
                    }
                    if (_spellsOn) b.Field("wall", state.WallTicks);
                    // Written only in fights with a band, so every older fight keeps its checksum.
                    if (_bandOn)
                    {
                        b.Field("bannerUp", state.BannerUp);
                        b.Field("berserk", state.BerserkTicks);
                        b.Field("berserkUsed", state.BerserkUsed);
                        b.Field("secondWind", state.SecondWindUsed);
                        b.Field("mendNext", state.MendNext);
                    }
                    // Written only under the parry meter (rules 22), so every older fight keeps its checksum.
                    if (_meterOn) b.Field("parryBars", state.ParryBars);
                    // Written only in fights with the chief kits, so every older fight keeps its checksum.
                    if (_kitsOn)
                    {
                        b.Field("cleave", state.CleaveWindup);
                        b.Field("cleaveTarget", state.CleaveTargetId);
                        b.Field("cleaveNext", state.CleaveNext);
                        b.Field("cleaveWaited", state.CleaveWaited);
                        // Written only under danger (rules 21), so every older fight keeps its checksum.
                        if (_dangerOn) b.Field("swungLast", state.SwungLast);
                        b.Field("brandNext", state.BrandNext);
                        b.Field("sunEclipses", state.SunEclipses);
                        b.Field("saidParry", state.SaidParry);
                        b.Field("glory", state.Glory);
                        b.Field("lastAnswer", state.LastAnswer);
                        b.Field("chain", state.Chain);
                    }
                    // Written only in fights with the 22 Oct kit additions, so every 21 Oct kit fight keeps its checksum.
                    if (_kitsTwoOn) AppendKitTwo(b, state);
                    // And the Stoneveil and Thundercoast chiefs' only in their fights.
                    if (_riteOn) AppendRite(b, state);
                    if (_tideOn) AppendTide(b, state);
                    // And the parry at the center's only in its fights (rules version 8).
                    if (_parryOn) AppendParry(b, state);
                }
                b.Section("units");
                for (int i = 0; i < _units.Count; i++)
                {
                    BattleUnit u = _units[i];
                    // Who each unit is, not only how it is doing, so two different line ups never
                    // share a checksum (Codex audit, 2 Oct 2026, finding 8).
                    b.Field(u.Id + ".class", SliceClasses.Name(u.Class));
                    b.Field(u.Id + ".row", (int)u.Row);
                    b.Field(u.Id + ".role", (int)u.Role);
                    b.Field(u.Id + ".power", u.PowerPercent);
                    b.Field(u.Id + ".fear", _fear[i]);
                    b.Field(u.Id + ".maxHp", u.MaxHp);
                    b.Field(u.Id + ".hp", u.Hp);
                    b.Field(u.Id + ".alive", u.Alive);
                    b.Field(u.Id + ".cd", u.Cooldown);
                    b.Field(u.Id + ".guard", u.GuardTargetId);
                    b.Field(u.Id + ".trapIn", _trapReadyIn[i]);
                    if (_skillOn)
                    {
                        b.Field(u.Id + ".panic", u.PanicTicks);
                        b.Field(u.Id + ".rise", u.RiseTicks);
                    }
                    if (_fight.Misses)
                    {
                        UnitSpec spec = SpecOf(u);
                        b.Field(u.Id + ".hitBonus", spec.HitBonus);
                        b.Field(u.Id + ".avoidBonus", spec.AvoidBonus);
                    }
                    // Written only in fights where someone bleeds, so every older fight keeps its checksum.
                    if (_bleedOn)
                    {
                        b.Field(u.Id + ".bleed", u.BleedTicks);
                        b.Field(u.Id + ".bleedPct", u.BleedPercent);
                    }
                    // Written only in fights with class spells, so every older fight keeps its checksum.
                    if (_classOn)
                    {
                        b.Field(u.Id + ".snare", u.SnareTicks);
                        b.Field(u.Id + ".opening", u.OpeningTicks);
                        b.Field(u.Id + ".spell", u.SpellTicks);
                        b.Field(u.Id + ".aim", u.AimTicks);
                        b.Field(u.Id + ".aimAt", u.AimTargetId);
                        b.Field(u.Id + ".bend", (int)SpecOf(u).Bend);
                    }
                    // Written only under the road kit, so every older fight keeps its checksum.
                    if (_roadKitOn)
                    {
                        b.Field(u.Id + ".boost", u.BoostTicks);
                        b.Field(u.Id + ".steady", u.Steady);
                        b.Field(u.Id + ".burn", u.BurnStacks);
                        b.Field(u.Id + ".burnTicks", u.BurnTicks);
                        b.Field(u.Id + ".burner", u.BurnerId);
                        b.Field(u.Id + ".carryNext", u.CarryNext);
                        b.Field(u.Id + ".carryMove", (int)u.CarryMove);
                        b.Field(u.Id + ".carryTell", u.CarryTell);
                        b.Field(u.Id + ".carryAt", u.CarryTargetId);
                        b.Field(u.Id + ".brandBy", u.BrandById);
                        b.Field(u.Id + ".brandTicks", u.BrandTicks);
                    }
                    // Written only under road combat, so every older fight keeps its checksum.
                    if (_roadCombatOn)
                    {
                        b.Field(u.Id + ".nerve", u.UnitNerve);
                        b.Field(u.Id + ".nerveRem", u.UnitNerveRem);
                        b.Field(u.Id + ".snapped", u.NerveSnapped);
                        b.Field(u.Id + ".frenzied", u.Frenzied);
                        b.Field(u.Id + ".fled", u.Fled);
                        b.Field(u.Id + ".snareBy", u.SnareOriginId);
                        b.Field(u.Id + ".snareChain", u.SnareChain);
                        b.Field(u.Id + ".openingBy", u.OpeningOriginId);
                        b.Field(u.Id + ".openingChain", u.OpeningChain);
                        b.Field(u.Id + ".bleedBy", u.BleedOriginId);
                        b.Field(u.Id + ".bleedChain", u.BleedChain);
                        b.Field(u.Id + ".lastChain", u.LastChainId);
                        b.Field(u.Id + ".aimChain", u.AimChainId);
                        b.Field(u.Id + ".lastBonded", u.LastBondedTick);
                        b.Field(u.Id + ".defence", u.DefenceTicks);
                        b.Field(u.Id + ".stepBack", u.StepBackTicks);
                    }
                    // Written only in fights with the chief rules, so every older fight keeps its checksum.
                    if (_chiefOn)
                    {
                        b.Field(u.Id + ".yielded", u.Yielded);
                        b.Field(u.Id + ".phase", u.PhaseDone);
                        b.Field(u.Id + ".benched", u.Benched);
                    }
                    // Written only in fights with the chief kits, so every older fight keeps its checksum.
                    if (_kitsOn)
                    {
                        b.Field(u.Id + ".brand", u.BrandTicks);
                        b.Field(u.Id + ".stun", u.StunTicks);
                    }
                    if (_kitsTwoOn)
                    {
                        b.Field(u.Id + ".hook", u.HookTicks);
                        b.Field(u.Id + ".rifle", u.RifleTicks);
                    }
                    // Written only in beast fights, so every older fight keeps its checksum.
                    if (_beastOn) b.Field(u.Id + ".scars", SpecOf(u).BeastScars);
                    // And only under the parry at the center.
                    if (_parryOn)
                    {
                        b.Field(u.Id + ".reel", u.ReelTicks);
                        b.Field(u.Id + ".beat", u.BeatFlags);
                    }
                    // And only in fights where someone wears a Butcher combo (BattleSimulation.Combos.cs).
                    if (_comboOn)
                    {
                        b.Field(u.Id + ".pin", u.PinTicks);
                        b.Field(u.Id + ".evac", u.EvacTicks);
                        b.Field(u.Id + ".evacSpent", u.EvacSpent);
                        b.Field(u.Id + ".flurry", u.FlurryTicks);
                        b.Field(u.Id + ".flurryStore", u.FlurryStore);
                        b.Field(u.Id + ".flurryBy", u.FlurryById);
                        b.Field(u.Id + ".slow", u.SlowTicks);
                        b.Field(u.Id + ".comboCount", u.ComboCount);
                        b.Field(u.Id + ".comboTicks", u.ComboTicks);
                        for (int k = 0; k < u.HurtLost.Length; k++)
                        {
                            string slot = k.ToString(CultureInfo.InvariantCulture);
                            b.Field(u.Id + ".hurt" + slot, u.HurtLost[k]);
                            b.Field(u.Id + ".hurtAt" + slot, u.HurtSecond[k]);
                        }
                        b.Field(u.Id + ".hurtBy", u.HurtById);
                    }
                    // And only in a champion duel (DESIGN_LOCKS, "Duels").
                    if (_duelOn) b.Field(u.Id + ".sprangTrap", u.SprangTrap);
                    // And the damage layers' leftovers, only under them (BattleSimulation.Layers.cs).
                    if (_layersOn)
                    {
                        b.Field(u.Id + ".hitLeft", u.HitLeftover);
                        b.Field(u.Id + ".healLeft", u.HealLeftover);
                    }
                    // And the Butcher's marks, only where they play (BattleSimulation.Marks.cs).
                    if (_marksOn)
                    {
                        b.Field(u.Id + ".comboRank", SpecOf(u).ComboRank);
                        for (StatusKind k = StatusKind.Crush; k <= StatusKind.WideOpen; k++)
                        {
                            Status mark = u.Statuses[k];
                            string name = u.Id + "." + k.ToString().ToLowerInvariant();
                            b.Field(name, mark.Ticks);
                            b.Field(name + "Stacks", mark.Stacks);
                            b.Field(name + "By", mark.SourceId);
                        }
                    }
                    // And the floor under the three Pals, only where it plays (BattleSimulation.Shade.cs).
                    if (_floorOn)
                    {
                        b.Field(u.Id + ".bend2", (int)SpecOf(u).Bend2);
                        b.Field(u.Id + ".comboRank2", SpecOf(u).ComboRank2);
                        b.Field(u.Id + ".clock", SpecOf(u).ClockPercent);
                        Status shield = u.Statuses[StatusKind.Shield];
                        b.Field(u.Id + ".shield", shield.Ticks);
                        b.Field(u.Id + ".shieldSize", shield.Size);
                        b.Field(u.Id + ".backstab", u.Statuses[StatusKind.Backstab].Ticks);
                        // And the Whisperer's, only where she plays (BattleSimulation.Whisperer.cs).
                        if (_whisperOn) AppendWhisperUnit(b, u);
                        // And the Shade's forms, only where they play (rules 25).
                        if (_shadeOn) AppendShadeUnit(b, u);
                        if (_gluttonOn) AppendGluttonUnit(b, u);
                    }
                }
                b.Section("events");
                b.Field("count", _events.Count);
                b.Field("hash", _eventHash);
                return b.ToString();
            }
        }

        // ------------------------------------------------------------ directives and reflexes

        private void ApplyDirective(int side, Directive directive, BattleUnit target, string actorId)
        {
            if (_finished) return;
            SideState state = _sides[side];
            int cost = CostOf(side, directive);
            state.Points -= cost;
            // From rules 22 casting your chief spell spends the whole parry meter (BattleSimulation.Meter.cs).
            if (directive == Directive.Eclipse && MeterFor(side)) state.ParryBars = 0;
            Emit(new BattleEvent(_tick, BattleEventKind.Directive, side, actorId, -1,
                target == null ? string.Empty : target.Id, target == null ? -1 : (int)target.Class, cost, Directives.Name(directive)));

            // Only orders the player gives count toward combos; reflexes and the chief's own casts do not.
            bool ordered = !actorId.StartsWith(AutoActorPrefix, StringComparison.Ordinal);

            // An interrupt doubled this order.
            int percent = 100;
            if (state.Empowered)
            {
                state.Empowered = false;
                percent = Moves.EmpoweredPercent;
                Emit(new BattleEvent(_tick, BattleEventKind.Empowered, side, actorId, -1,
                    target == null ? string.Empty : target.Id, target == null ? -1 : (int)target.Class, percent, Directives.Name(directive)));
            }

            switch (directive)
            {
                case Directive.Hold:
                    state.HoldTicks = HoldTicks * percent / 100;
                    StampHold(side, state);
                    break;
                case Directive.FallBack:
                    state.FallBackTicks = Longer(FallBackTicks * percent / 100, _rules[side].Favours.FallBackLongerPercent);
                    // Backstab's price (rules 25): the next Fall Back waits until a while after this one runs out.
                    if (_shadeOn && KitOf(side).Has(ShadeRoute.Backstab))
                        state.FallBackLockTicks = state.FallBackTicks + ShadeRules.BackstabLockTicks * PriceLeft(side) / 100;
                    if (ordered) state.LastFallBackTick = _tick;
                    ReflexPressOnFallBack(1 - side);
                    // Backstab, the Shade's Fall Back trick (rules 23, BattleSimulation.Shade.cs).
                    if (_floorOn) Backstab(side);
                    // Rift Mine, her other Fall Back route (rules 25).
                    if (_shadeOn) RiftMine(side, state);
                    break;
                case Directive.FocusFire:
                    // A Thick Hide hunter braces the moment you focus him, under the parry at the center (BattleSimulation.Band.cs).
                    if (_parryOn && HasBand(target.Side, BandTrait.ThickHide)) Fired(target.Side, LessonFiring.ThickHide, target, null);
                    state.FocusTargetId = target.Id;
                    state.FocusTicks = FearPriced(side, Longer(FocusTicks * percent / 100, _rules[side].Favours.FocusLongerPercent));
                    // Rupture (rules 25): a new Focus Fire starts its weak point over (BattleSimulation.Shade.cs).
                    if (_shadeOn) RuptureOn(side, state, target);
                    if (ordered)
                    {
                        state.LastFocusTick = _tick;
                        state.LastFocusId = target.Id;
                    }
                    ReflexCoverFocused(1 - side, target);
                    break;
                case Directive.Unleash:
                    state.UnleashTicks = Longer(UnleashTicks * percent / 100, _rules[side].Favours.UnleashLongerPercent);
                    // An Unleash smashes the enemy's standing Steel Storm wall (ChiefSpells, 18 Oct 2026) and tears
                    // down a hunter band's banner (BandTrait.Banner). Into her Iron Advance it staggers her line (ChiefKits).
                    if (_spellsOn) TrySmashWall(side);
                    if (_kitsTwoOn) StaggerAdvance(side);
                    // And tears the hunters' net off a prey waiting for the Kill (BattleSimulation.Hunt.cs), kicks the Stoneveil
                    // chief's Basin over and slams the Thundercoast chief's Sluice Gate shut.
                    if (_huntOn) TearNets(side);
                    if (_riteOn) KickBasin(side);
                    if (_tideOn) SlamSluice(side);
                    if (_bandOn) TryTearBanner(side);
                    if (ordered)
                    {
                        TryAmbush(side);
                        TryRiposte(side);
                        state.LastUnleashTick = _tick;
                    }
                    ReflexHoldOnUnleash(1 - side);
                    ReflexKite(1 - side);
                    if (_nerveOn) UnleashScare(side);
                    // Echo and Breach, the Shade's Unleash routes (rules 25, BattleSimulation.Shade.cs).
                    if (_shadeOn) ShadeUnleash(side, state);
                    break;
                case Directive.Sacrifice:
                    Sacrifice(target, percent);
                    if (_nerveOn) DrainNerve(1 - side, SacrificeNerveCost, side);
                    break;
                case Directive.Eclipse:
                    // At Glory S your 5th button goes off twice as hard (ChiefKits, 21 Oct 2026).
                    int glory = GloryCast(side);
                    if (glory != 100)
                        Emit(new BattleEvent(_tick, BattleEventKind.Empowered, side, actorId, -1, string.Empty, -1, glory,
                            Directives.Name(directive)));
                    StartEclipse(side, percent * glory / 100, ordered);
                    break;
                case Directive.Frenzy:
                    BattleUnit pal = LivingPal(side);
                    state.FrenzyTicks = Moves.FrenzyTicks * percent / 100;
                    state.FrenzyUsed = true;
                    Emit(new BattleEvent(_tick, BattleEventKind.Transform, side, pal.Id, (int)pal.Class,
                        string.Empty, -1, state.FrenzyTicks, Directives.Name(Directive.Frenzy)));
                    break;
            }

            // Every order you give feeds the Stoneveil chief's Blood Price (BattleSimulation.Stoneveil.cs).
            if (_riteOn && ordered) FeedPrice(1 - side);
        }

        /// Detail of the event a side gets when its raised shields block an Unleash: actor side
        /// is the side that held, amount the courage it kept.
        public const string BlockedDetail = "blocked";

        /// Ticks of Hold left on a side, or 0. Its shields are up while this runs.
        public int HoldTicksLeft(int side)
        {
            return _sides[side].HoldTicks;
        }

        /// Ticks of Unleash left on a side, or 0.
        public int UnleashTicksLeft(int side)
        {
            return _sides[side].UnleashTicks;
        }

        /// Ticks of Fall Back left on a side, or 0. No melee lands while it runs. Read only, for
        /// screens that show each order while it lasts (10 Oct 2026).
        public int FallBackTicksLeft(int side)
        {
            return _sides[side].FallBackTicks;
        }

        /// Ticks of Focus Fire left on a side, or 0.
        public int FocusTicksLeft(int side)
        {
            return _sides[side].FocusTicks;
        }

        /// The unit a side's Focus Fire is on, or empty while it has none.
        public string FocusedId(int side)
        {
            return _sides[side].FocusTicks > 0 ? _sides[side].FocusTargetId : string.Empty;
        }

        /// Unleash's scare (Jacob, 6 Oct 2026). It drains the enemy's courage, unless the enemy is
        /// holding: an Unleash into raised shields scares nobody, and against the held front row
        /// its extra damage is all but cancelled by the Hold. So it pays to wait for the shields
        /// to drop, and a chief who learned to Hold the moment you Unleash blocks you while he
        /// has a point to spend. Measured against Codex's audit of the same day, where pressing
        /// Unleash whenever it was affordable won every run.
        private void UnleashScare(int side)
        {
            // Under road combat a Riposte that ended the fight scares nobody after it (a raid chief can fall to one).
            if (_finished && _roadCombatOn) return;
            int enemy = 1 - side;
            // A band that is Unshakable never scares (BandTrait.Unshakable); a skittish one scares through its Hold.
            if (HasBand(enemy, BandTrait.Unshakable))
            {
                Emit(new BattleEvent(_tick, BattleEventKind.Reaction, enemy, string.Empty, -1, string.Empty, -1,
                    _setup.Nerve.UnleashCost, BandRules.UnshakableDetail));
                Fired(enemy, LessonFiring.Unshakable, null, null);
                return;
            }
            bool skittish = HasBand(enemy, BandTrait.WeakToScare);
            if (_sides[enemy].HoldTicks > 0 && !skittish)
            {
                Emit(new BattleEvent(_tick, BattleEventKind.Directive, enemy, AutoActor(enemy), -1,
                    string.Empty, -1, _setup.Nerve.UnleashCost, BlockedDetail));
                return;
            }
            int scare = skittish ? _setup.Nerve.UnleashCost * BandRules.WeakScarePercent / 100 : _setup.Nerve.UnleashCost;
            // The Butcher's favour breaks more of their nerve (SideFavours, 18 Oct 2026).
            scare = scare * (100 + _rules[side].Favours.UnleashScarePercent) / 100;
            // In a road combat road fight the scare lands on their front row (BattleSimulation.Road.cs).
            if (_unitNerveOn) ScareFront(enemy, scare, side);
            else DrainNerve(enemy, scare, side);
        }

        // A Pal's favour lengthens an order (SideFavours, 18 Oct 2026).
        private static int Longer(int ticks, int percent)
        {
            return percent <= 0 ? ticks : ticks * (100 + percent) / 100;
        }

        // A favour that cuts quarters of a drain skips these steps of it: quarters 0 to 3 of every four.
        private static bool Skipped(long step, int quarters)
        {
            return quarters > 0 && step % 4 < quarters;
        }

        // What a holding front row takes from a blow or a heavy swing, in percent; the Glutton's favour lowers it.
        private int HoldSoak(int side)
        {
            // Glancing Fold (rules 26, BattleSimulation.Glutton.cs) soaks more.
            return Glanced(side, HoldDamagePercent - _rules[side].Favours.HoldSoakCut);
        }

        // The Shade's favours when a side dodges a cast or a heavy swing with Fall Back (SideFavours, 18 Oct 2026): the
        // dodge earns more, and whoever threw it can be left staggering.
        private void FavourDodge(int dodger, BattleUnit thrower)
        {
            SideFavours f = _rules[dodger].Favours;
            if (f.DodgePoints > 0) Earn(dodger, f.DodgePoints, FightRules.DodgeReason);
            if (f.WrongfootTicks <= 0 || thrower == null || !thrower.Alive) return;
            thrower.Cooldown += f.WrongfootTicks;
            Emit(EventFor(BattleEventKind.Reaction, thrower, null, f.WrongfootTicks, SideFavours.WrongfootDetail));
        }

        /// Learned: our Warden covers whoever the enemy just focused.
        private void ReflexCoverFocused(int side, BattleUnit focused)
        {
            if (_finished) return;
            if (_tactics[side].CoverFocused < HabitThreshold || !focused.Alive || focused.Class == UnitClass.Warden) return;
            BattleUnit warden = FirstLivingWarden(side);
            if (warden == null) return;
            SideState state = _sides[side];
            // Under the parry at the center his Wardens stay on the first one for the whole FocusTicks: a second Focus Fire
            // meanwhile finds them stuck (Jacob, 24 Oct 2026).
            if (_parryOn && state.ForcedWardTicks > 0) return;
            state.ForcedWardId = focused.Id;
            state.ForcedWardTicks = FocusTicks;
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (u.Alive && u.Side == side && u.Class == UnitClass.Warden) u.GuardTargetId = focused.Id;
            }
            Emit(new BattleEvent(_tick, BattleEventKind.Directive, side, AutoActor(side), -1,
                focused.Id, (int)focused.Class, 0, CoverDetail));
            Fired(side, LessonFiring.Cover, warden, focused);
        }

        /// Learned: answer the enemy's Unleash with Hold straight away.
        private void ReflexHoldOnUnleash(int side)
        {
            if (_finished || _tactics[side].HoldOnUnleash < HabitThreshold) return;
            // An Unshakable band stops Holding against your Unleash under the parry at the center (BandTrait.Unshakable).
            if (_parryOn && HasBand(side, BandTrait.Unshakable)) return;
            if (_sides[side].Points < CostOf(side, Directive.Hold) || _sides[side].HoldTicks > 0) return;
            Fired(side, LessonFiring.HoldOnUnleash, null, null);
            ApplyDirective(side, Directive.Hold, null, AutoActor(side));
        }

        /// Learned: when the enemy falls back, nobody is in melee, so our ranged fighters unleash.
        private void ReflexPressOnFallBack(int side)
        {
            if (_finished || _tactics[side].PressOnFallBack < HabitThreshold) return;
            if (_sides[side].Points < Directives.Cost(Directive.Unleash) || _sides[side].UnleashTicks > 0) return;
            Fired(side, LessonFiring.PressOnFallBack, null, null);
            ApplyDirective(side, Directive.Unleash, null, AutoActor(side));
        }

        /// Open the fight by focusing a set enemy class. Not learned (see TacticsProfile.FocusOpener).
        private void TryOpener(int side)
        {
            int opener = _tactics[side].FocusOpener;
            if (opener < 0 || _sides[side].Points < Directives.Cost(Directive.FocusFire)) return;
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (u.Alive && u.Side != side && (int)u.Class == opener)
                {
                    ApplyDirective(side, Directive.FocusFire, u, AutoActor(side));
                    return;
                }
            }
        }

        private static string AutoActor(int side)
        {
            return AutoActorPrefix + side.ToString(CultureInfo.InvariantCulture);
        }

        private static void CountDownEffects(SideState state)
        {
            if (state.HoldTicks > 0) state.HoldTicks--;
            if (state.FallBackTicks > 0) state.FallBackTicks--;
            if (state.UnleashTicks > 0) state.UnleashTicks--;
            if (state.FrenzyTicks > 0) state.FrenzyTicks--;
            if (state.RallyTicks > 0) state.RallyTicks--;
            if (state.FocusTicks > 0)
            {
                state.FocusTicks--;
                if (state.FocusTicks == 0) state.FocusTargetId = string.Empty;
            }
            if (state.ForcedWardTicks > 0)
            {
                state.ForcedWardTicks--;
                if (state.ForcedWardTicks == 0) state.ForcedWardId = string.Empty;
            }
        }

        // ------------------------------------------------------------ big moves

        /// The chief's own Eclipse: due every AutoEclipseTicks, cast as soon as it can be.
        private void TryAutoEclipse(int side)
        {
            SideRules rules = _rules[side];
            SideState state = _sides[side];
            if (rules.AutoEclipseTicks <= 0 || rules.EclipseGrade <= 0 || _tick < state.NextAutoEclipse) return;
            // One tell at a time: the Eclipse waits for the side's heavy swing to land (18 Oct 2026).
            if (_heavyOn && state.HeavyWindup > 0) return;
            // A chief never casts Steel Storm again while his wall still stands, so it cannot be kept up for good.
            if (_spellsOn && state.WallTicks > 0) return;
            // A chief who yielded casts nothing more (ChiefFight).
            if (_chiefOn && Leader(side) != null && Leader(side).Yielded) return;
            // Nor while his blade glows for a Cleave, he spins his ring, she throws her chain or aims, or your Sunbreaker or
            // Breach has him stunned (ChiefKits): it waits. Her Steel Storm needs a Warden of hers standing, and stops at her
            // Lockdown, whose wall never drops.
            if (_kitsOn && Leader(side) != null && KitBusy(Leader(side))) return;
            if (_kitsTwoOn && Kit(side) == ChiefKit.Skullpeak && (state.Lockdown || FirstLivingWarden(side) == null)) return;
            if (!CanUse(side, Directive.Eclipse, out _)) return;
            ApplyDirective(side, Directive.Eclipse, null, AutoActor(side));
            state.NextAutoEclipse = _tick + SpellGap(side);
        }

        /// The side's leader starts winding up. If the player's last orders were Focus Fire then
        /// Unleash, close enough together, this Eclipse finishes a Pyre on the focused target.
        private void StartEclipse(int side, int percent, bool ordered)
        {
            SideState state = _sides[side];
            BattleUnit caster = Leader(side);
            // The Skullpeak chief's Wardens lock shields for her Steel Storm in their own time (ChiefKits).
            // A Time Snare slows the caster's next big move (BattleSimulation.Combos.cs).
            state.Windup = Slowed(caster, _kitsTwoOn ? KitWindupTicks(side) : Moves.EclipseWindupTicks);
            state.CasterId = caster.Id;
            state.WindupDamage = 0;
            state.WindupPercent = Moves.EclipsePercent(_rules[side].EclipseGrade) * percent / 100;
            // Under Blood Sun each Eclipse hits harder than the last (ChiefKits); his forecast of it is spent, and her lock
            // starts unsnared.
            if (KitOn(side)) RampEclipse(side, state);
            if (_kitsTwoOn && KitOn(side)) StartedSpell(side, state);
            state.WindupPyre = false;
            state.PyreTargetId = string.Empty;
            // Only an Eclipse finishes a Pyre; a Steel Storm is a wall, not a blast.
            if (_rules[side].Spell == ChiefSpell.Eclipse &&
                ordered && _rules[side].Combos && !state.PyreUsed && state.LastFocusTick >= 0 &&
                state.LastUnleashTick >= state.LastFocusTick && _tick - state.LastFocusTick <= Moves.PyreWindowTicks)
            {
                BattleUnit focused = FindById(state.LastFocusId);
                if (focused != null && focused.Alive)
                {
                    state.WindupPyre = true;
                    state.PyreTargetId = focused.Id;
                    state.PyreUsed = true;
                }
            }
            BattleUnit pyreTarget = state.WindupPyre ? FindById(state.PyreTargetId) : null;
            Emit(new BattleEvent(_tick, BattleEventKind.Windup, side, caster.Id, (int)caster.Class,
                pyreTarget == null ? string.Empty : pyreTarget.Id, pyreTarget == null ? -1 : (int)pyreTarget.Class,
                state.Windup, state.WindupPyre ? Moves.Pyre
                    : _rules[side].Spell == ChiefSpell.Eclipse ? Directives.Name(Directive.Eclipse) : ChiefSpells.Name(_rules[side].Spell)));
            ReactGuardCaster(side, caster);
        }

        private void AdvanceWindup(int side)
        {
            SideState state = _sides[side];
            if (state.Windup <= 0) return;
            BattleUnit caster = FindById(state.CasterId);
            if (caster == null || !caster.Alive)
            {
                ClearWindup(state);
                return;
            }
            // While her Wardens lock shields, a snare on any of them leaves the wall half built (ChiefKits).
            if (_kitsTwoOn && Kit(side) == ChiefKit.Skullpeak && WardenSnared(side)) state.LockSnared = true;
            state.Windup--;
            // Under the parry at the center the cast's release is its beat (BattleSimulation.Parry.cs).
            if (_parryOn && state.Windup == 0)
            {
                int hold = HoldBeat(side, caster, BeatSlot.Windup);
                if (hold > 0) state.Windup = hold;
                else ReleaseCast(side, caster);
                return;
            }
            if (state.Windup == 0 && WaitOutHold(side, caster)) return;
            if (state.Windup == 0) ReleaseEclipse(side, caster);
        }

        /// The Eclipse goes off on the enemy front row (the back row once the front has fallen).
        /// Fall Back dodges it, Hold soaks half of it and half of its nerve cost.
        private void ReleaseEclipse(int side, BattleUnit caster)
        {
            SideState state = _sides[side];
            int enemy = 1 - side;
            SideState theirs = _sides[enemy];
            int percent = state.WindupPercent;
            bool pyre = state.WindupPyre;
            string pyreTargetId = state.PyreTargetId;
            ClearWindup(state);

            // A Steel Storm goes up as a wall on the caster's own side: nothing to parry or dodge. A War Cry steadies the
            // caster's own side.
            ChiefSpell spell = _rules[side].Spell;
            if (spell == ChiefSpell.SteelStorm)
            {
                RaiseWall(side, caster);
                return;
            }
            if (spell == ChiefSpell.WarCry)
            {
                CryOut(side, caster);
                return;
            }

            // A Firebomb is fire: a Hold neither parries nor soaks it, only Fall Back gets clear. A Dust Shark's bite comes
            // up out of the sand the same way, at the back row first (Jacob, 18 Oct 2026: "2 Dust Shark").
            // The cave tribe's blood call and the sea tribe's tide (18 Oct 2026) cannot be parried or soaked either; the tide
            // comes up at the back row like the shark.
            bool bloodCall = spell == ChiefSpell.BloodCall;
            bool shark = spell == ChiefSpell.DustShark || spell == ChiefSpell.Tide;
            bool fire = spell == ChiefSpell.Firebomb || shark || bloodCall;
            // The Thundercoast chief's Tide hits harder the higher the water stands, and from his Spring Tide drowns both rows
            // (BattleSimulation.Thundercoast.cs).
            bool tideKit = spell == ChiefSpell.Tide && _tideOn && side == _tideSide;
            int tide = tideKit ? TideScale() : 100;
            percent = percent * tide / 100;
            bool dodged = theirs.FallBackTicks > 0;
            // The Stoneveil chief's Blood Call is answered his own way (BattleSimulation.Stoneveil.cs).
            if (bloodCall && _riteOn && Kit(side) == ChiefKit.Stoneveil)
            {
                ReleaseRite(side, caster, percent, dodged);
                return;
            }
            if (!dodged && !fire && TryParry(side, caster, percent))
            {
                // A chief's kit (BattleSimulation.Kit.cs): the parry answers his Eclipse and can finish his hidden chain.
                if (KitOn(side)) KitEclipseParried(side, caster, percent);
                return;
            }
            bool soaked = !dodged && !fire && theirs.HoldTicks > 0;
            Emit(new BattleEvent(_tick, BattleEventKind.Release, side, caster.Id, (int)caster.Class, string.Empty, -1,
                percent, dodged ? Moves.Dodged : soaked ? Moves.Soaked : Moves.Full));
            if (dodged)
            {
                Earn(enemy, FightRules.DodgePoints, FightRules.DodgeReason);
                FavourDodge(enemy, caster);
                if (KitOn(side)) KitEclipseLanded(side, caster, true, false);
                return;
            }
            if (KitOn(side)) KitEclipseLanded(side, caster, false, soaked);
            if (bloodCall)
            {
                CallBlood(side, caster, percent);
                return;
            }

            Row first = shark ? Row.Back : Row.Front;
            Row row = AnyAlive(enemy, first) ? first : first == Row.Front ? Row.Back : Row.Front;
            bool bothRows = tideKit && SpringTide(side);
            for (int i = 0; i < _units.Count && !_finished; i++)
            {
                BattleUnit u = _units[i];
                if (!u.Alive || u.Side != enemy || (u.Row != row && !bothRows)) continue;
                int damage = u.MaxHp * percent / 100;
                if (soaked) damage = damage * Moves.EclipseHoldPercent / 100;
                ApplyDamage(caster, side, u, Math.Max(1, damage),
                    !shark ? Moves.EclipseDetail : spell == ChiefSpell.Tide ? ChiefSpells.TideDetail : ChiefSpells.BiteDetail);
            }
            if (!_finished)
                DrainNerve(enemy, (soaked ? Moves.EclipseNerveCost * Moves.EclipseHoldPercent / 100 : Moves.EclipseNerveCost) * tide / 100,
                    side);

            if (pyre && !_finished)
            {
                BattleUnit target = FindById(pyreTargetId);
                if (target != null && target.Alive)
                {
                    int burn = Math.Max(1, target.MaxHp * Moves.PyrePercent / 100);
                    Emit(new BattleEvent(_tick, BattleEventKind.Combo, side, caster.Id, (int)caster.Class,
                        target.Id, (int)target.Class, burn, Moves.Pyre));
                    Earn(side, FightRules.ComboPoints, FightRules.ComboReason);
                    ApplyDamage(caster, side, target, burn, Moves.PyreDetail);
                    if (!_finished) DrainNerve(enemy, Moves.PyreNerveCost, side);
                }
            }
            CheckWipe();
        }

        /// Fall Back then Unleash, close together: the enemy back row (the front row once the back
        /// has fallen) is caught in the open.
        private void TryAmbush(int side)
        {
            SideState state = _sides[side];
            if (!_rules[side].Combos || state.AmbushUsed || state.LastFallBackTick < 0 ||
                _tick - state.LastFallBackTick > Moves.AmbushWindowTicks)
                return;
            state.AmbushUsed = true;
            int enemy = 1 - side;
            Row row = AnyAlive(enemy, Row.Back) ? Row.Back : Row.Front;
            Emit(new BattleEvent(_tick, BattleEventKind.Combo, side, string.Empty, -1, string.Empty, -1,
                Moves.AmbushPercent, Moves.Ambush));
            Earn(side, FightRules.ComboPoints, FightRules.ComboReason);
            for (int i = 0; i < _units.Count && !_finished; i++)
            {
                BattleUnit u = _units[i];
                if (!u.Alive || u.Side != enemy || u.Row != row) continue;
                ApplyDamage(null, side, u, Math.Max(1, u.MaxHp * Moves.AmbushPercent / 100), Moves.AmbushDetail);
            }
            if (!_finished) DrainNerve(enemy, Moves.AmbushNerveCost, side);
            CheckWipe();
        }

        /// The caster was hit hard enough: the move is lost and the other side's next order is doubled.
        private void Interrupt(BattleUnit caster, BattleUnit source)
        {
            SideState state = _sides[caster.Side];
            int taken = state.WindupDamage;
            bool pyre = state.WindupPyre;
            ClearWindup(state);
            int interrupter = 1 - caster.Side;
            _sides[interrupter].Empowered = true;
            ChiefSpell spell = _rules[caster.Side].Spell;
            Emit(new BattleEvent(_tick, BattleEventKind.Interrupt, interrupter,
                source == null ? string.Empty : source.Id, source == null ? -1 : (int)source.Class,
                caster.Id, (int)caster.Class, taken,
                pyre ? Moves.Pyre : spell == ChiefSpell.Eclipse ? Directives.Name(Directive.Eclipse) : ChiefSpells.Name(spell)));
            Earn(interrupter, FightRules.InterruptPoints, FightRules.InterruptReason);
            // A chief's kit (BattleSimulation.Kit.cs): breaking his Eclipse answers it.
            if (KitOn(caster.Side)) KitEclipseBroken(caster, interrupter);
        }

        private static void ClearWindup(SideState state)
        {
            state.Windup = 0;
            state.CasterId = string.Empty;
            state.WindupDamage = 0;
            state.WindupPercent = 0;
            state.WindupPyre = false;
            state.PyreTargetId = string.Empty;
            state.WindupWaited = false;
            state.Beats[(int)BeatSlot.Windup] = 0;
        }

        private BattleUnit LivingPal(int side)
        {
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (u.Alive && u.Side == side && u.Role == UnitRole.Pal) return u;
            }
            return null;
        }

        private bool AnyAlive(int side, Row row)
        {
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (u.Alive && u.Side == side && u.Row == row) return true;
            }
            return false;
        }

        // ------------------------------------------------------------ actions

        private void Act(BattleUnit unit)
        {
            // With class spells each class casts its own spell (BattleSimulation.Class.cs).
            if (_classOn)
            {
                ActWithSpell(unit);
                return;
            }

            if (unit.Class == UnitClass.Warden) UpdateGuard(unit);

            if (unit.Class == UnitClass.Medic)
            {
                BattleUnit patient = PatientFor(unit);
                if (patient != null)
                {
                    Heal(unit, patient, unit.Power, string.Empty);
                    return;
                }
                Attack(unit, Math.Max(1, SliceClasses.MedicFallbackAttack * unit.PowerPercent / 100));
                return;
            }

            if (unit.Class == UnitClass.Trapper && ArmTrap(unit)) return;

            Attack(unit, unit.Power);
        }

        /// A Trapper arms its side's trap when none is armed and its turn has come round again, and then does nothing
        /// else this action; otherwise its turn comes one action closer. True when it armed one.
        private bool ArmTrap(BattleUnit unit)
        {
            SideState own = _sides[unit.Side];
            if (!own.TrapArmed && _trapReadyIn[unit.Index] <= 0)
            {
                own.TrapArmed = true;
                own.TrapOwnerId = unit.Id;
                // A rank 2 Trapper's trap springs twice before she rearms it (rules 24, BattleSimulation.Whisperer.cs).
                if (_whisperOn) own.TrapSprings = SpringsOf(unit) + FoldTrapSprings(unit);
                _trapReadyIn[unit.Index] = RearmActions(unit);
                Emit(EventFor(BattleEventKind.TrapArmed, unit, null, 0, string.Empty));
                return true;
            }
            if (_trapReadyIn[unit.Index] > 0) _trapReadyIn[unit.Index]--;
            return false;
        }

        private void UpdateGuard(BattleUnit warden)
        {
            SideState own = _sides[warden.Side];
            if (own.ForcedWardTicks > 0)
            {
                BattleUnit forced = FindById(own.ForcedWardId);
                if (forced != null && forced.Alive && !ReferenceEquals(forced, warden))
                {
                    warden.GuardTargetId = forced.Id;
                    return;
                }
            }
            BattleUnit caster = GuardedCaster(warden);
            if (caster != null)
            {
                warden.GuardTargetId = caster.Id;
                return;
            }
            // Bodyguards (ChiefFight): with the chief rules a Warden of a chief's band guards his chief.
            BattleUnit chief = _chiefOn ? GuardedChief(warden) : null;
            if (chief != null)
            {
                warden.GuardTargetId = chief.Id;
                return;
            }
            // A chief's Brand (ChiefKits): he covers the branded ally to take it halved, even if that leaves his Medic.
            BattleUnit branded = _kitsOn ? BrandedAlly(warden) : null;
            if (branded != null)
            {
                warden.GuardTargetId = branded.Id;
                return;
            }
            // Her Hook (ChiefKits, 22 Oct 2026): he covers the ally her chain glints at, to anchor them, or one it dragged.
            BattleUnit hooked = _kitsTwoOn ? HookedAlly(warden) : null;
            if (hooked != null)
            {
                warden.GuardTargetId = hooked.Id;
                return;
            }
            // The Stoneveil chief's Transfusion (BattleSimulation.Stoneveil.cs): he covers the ally it draws from, to block it.
            BattleUnit drawn = _riteOn ? DrawnAlly(warden) : null;
            if (drawn != null)
            {
                warden.GuardTargetId = drawn.Id;
                return;
            }
            // The hunt (BattleSimulation.Hunt.cs): he steps in for an ally netted for the captain's Kill.
            BattleUnit netted = _huntOn ? NettedPrey(warden) : null;
            if (netted != null)
            {
                warden.GuardTargetId = netted.Id;
                return;
            }
            // Covered Treatment (ClassSpells): with class spells he covers his side's Medic, who treats under his Guard. From rules
            // 24 his habit, then Mend and Hold, take this step's place (BattleSimulation.Whisperer.cs).
            BattleUnit medic = !_classOn ? null : _whisperOn ? WhisperGuard(warden) : CoveredMedic(warden);
            if (medic != null)
            {
                warden.GuardTargetId = medic.Id;
                return;
            }
            BattleUnit ward = MostHurtAlly(warden, int.MaxValue, false);
            warden.GuardTargetId = ward == null ? string.Empty : ward.Id;
        }

        /// Living ally below the given health with the lowest health. Ties go to the lower index.
        private BattleUnit MostHurtAlly(BattleUnit unit, int belowPermille, bool includeSelf)
        {
            BattleUnit best = null;
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (!u.Alive || u.Side != unit.Side) continue;
                if (!includeSelf && ReferenceEquals(u, unit)) continue;
                if (u.HpPermille >= belowPermille) continue;
                if (_beastOn && IsHeld(u)) continue; // in the cage bear's claw: nobody heals or covers them

                if (best == null || u.HpPermille < best.HpPermille) best = u;
            }
            return best;
        }

        private void Attack(BattleUnit attacker, int power)
        {
            BattleUnit target = ChooseTarget(attacker);
            if (target == null) return; // nothing it can or will strike this action; it waits
            Blow(attacker, target, power, BlowKind.Auto);
        }

        /// One blow on a chosen enemy: an auto attack, or a class spell's (BattleSimulation.Class.cs). A fight without
        /// class spells only ever deals auto attacks, exactly as before.
        private void Blow(BattleUnit attacker, BattleUnit target, int power, BlowKind kind, int comboPercent = 0)
        {
            // Under the road kit Fall Back is a pure dodge: nobody of the falling side attacks (BattleSimulation.Boosts.cs).
            if (FallingBack(attacker.Side)) return;
            // Rift Mine (rules 25): striking one of a side with a mine sets it off, and a blast that drops him stops the blow.
            if (_shadeOn && MineGoesOff(attacker, target)) return;
            int roll = 85 + _rng.Next(31);
            int damage = power * roll / 100;
            if (damage < 1) damage = 1;

            SideState theirs = _sides[target.Side];
            // From rules 17 the same bonuses go into the damage layers and the hit rounds once (BattleSimulation.Layers.cs). A
            // combo whose weight rides on the blow (Event Horizon) passes it as comboPercent there.
            Hit hit = _layersOn ? BlowHit(attacker, target, power, roll, kind) : null;
            if (hit == null) damage = Bonuses(attacker, target, damage, kind);
            else if (comboPercent != 0) hit.Add(HitLayer.Combo, comboPercent);

            // A melee attacker springs the defenders' armed trap before its blow lands. Through Doorway's portal a shooter does
            // too (BattleSimulation.Combos.cs). A duel's champion springs it once, then steps around it (DESIGN_LOCKS, "Duels").
            // An Ambusher's trap ignores these blows and waits for a heavy swing (rules 24, BattleSimulation.Whisperer.cs).
            if ((!attacker.Stats.Ranged || Doorway(theirs)) && theirs.TrapArmed && !(_duelOn && attacker.SprangTrap) && !AmbushWaits(target.Side))
            {
                if (_duelOn) attacker.SprangTrap = true;
                BattleUnit trapper = FindById(theirs.TrapOwnerId);
                // A rank 2 Trapper's trap springs twice (rules 24).
                if (!SpringLeft(theirs))
                {
                    theirs.TrapArmed = false;
                    theirs.TrapOwnerId = string.Empty;
                }
                Emit(new BattleEvent(_tick, BattleEventKind.TrapSprung, target.Side,
                    trapper == null ? string.Empty : trapper.Id, trapper == null ? -1 : (int)trapper.Class,
                    attacker.Id, (int)attacker.Class, SliceClasses.TrapDamage, string.Empty));
                if (attacker.Stats.Ranged) Emit(EventFor(BattleEventKind.Portal, trapper, attacker, 0, ClassSpells.Name(ClassBend.Doorway)));
                if (!_classOn) attacker.Cooldown += SliceClasses.TrapSlowTicks;
                ApplyDamage(trapper, target.Side, attacker, SliceClasses.TrapDamage, TrapDetail);
                // Under the nerve rules the trap can break the attacker's side; then the blow never lands.
                if (!attacker.Alive || _finished) return;
                // A barbed snare holds whoever sprang it instead of slowing it, and its blow never lands (ClassSpells).
                if (_classOn)
                {
                    Snare(trapper, target.Side, attacker);
                    // Rules 18: a shooter Doorway caught carries Exposed (BattleSimulation.Marks.cs).
                    if (attacker.Stats.Ranged) Lay(trapper, attacker, ClassBend.Doorway);
                    return;
                }
            }

            // FightRules.Misses: the blow can miss (Jacob, 13 Oct 2026). Then nobody is hit, not even a
            // covering Warden. A snared enemy cannot dodge (ClassSpells), nor can one her Hook dragged (ChiefKits).
            // Nor can one a black hole pins or a freeze holds, and Stopped Bullets' time stopped shot never misses
            // (BattleSimulation.Combos.cs).
            // An Exposed burst left him wide open: this blow from the other side spends it, can't miss and can't be Guarded,
            // and its crit doubles, in the Cash layer (BattleSimulation.Marks.cs).
            // Gorged Bursts (rules 27) left the burst's own percent more on it.
            int wideMore = _duosOn || _sizedOn ? target.Statuses[StatusKind.WideOpen].Size : 0;
            bool wideOpen = SpendWideOpen(attacker, target);
            if (wideOpen)
            {
                int before = hit != null ? hit.Points : damage;
                damage = More(damage, hit, HitLayer.Cash, MarkRules.WideOpenCashPercent * (100 + wideMore) / 100);
                Emit(EventFor(BattleEventKind.Cash, attacker, target, (hit != null ? hit.Points : damage) - before, MarkRules.WideOpenDetail));
            }
            // Backstab, the Shade's Fall Back trick (rules 23): stepping out behind them, this blow doubles, in the Cash layer
            // (BattleSimulation.Shade.cs).
            if (SpendBackstab(attacker, target))
            {
                int before = hit != null ? hit.Points : damage;
                // Sized (rules 28) at Backstab's form.
                damage = More(damage, hit, HitLayer.Cash, ShadeSized(attacker.Side, ShadeRoute.Backstab, ShadeRules.BackstabCashPercent));
                Emit(EventFor(BattleEventKind.Cash, attacker, target, (hit != null ? hit.Points : damage) - before, ShadeRules.BackstabDetail));
            }
            // A Duelist's strike on her Focus Fire target never misses and crits (rules 30, BattleSimulation.Vows.cs).
            bool duel = _vowsOn && DuelistSure(attacker, target, kind);
            if (duel)
            {
                int before = hit != null ? hit.Points : damage;
                damage = More(damage, hit, HitLayer.Cash, DuelistPercent(attacker));
                Emit(EventFor(BattleEventKind.Cash, attacker, target, (hit != null ? hit.Points : damage) - before, VowRules.DuelistDetail));
            }
            if (_fight.Misses && !wideOpen && !duel && !(_classOn && (target.SnareTicks > 0 || target.PinTicks > 0)) && target.HookTicks <= 0 && !_timeShot &&
                Missed(attacker, target)) return;
            // Under road combat a Marksman's Step Back makes a melee blow at him miss (BattleSimulation.Defence.cs).
            if (_roadCombatOn && !wideOpen && SteppedBack(attacker, target)) return;

            // A Warden covering this target takes the blow instead, lighter. A class spell's blow cashes in no mark on
            // him and leaves none.
            // No Guard takes a time stopped shot, nor Around the Corner's through a portal from behind, which hits a healer
            // harder (BattleSimulation.Combos.cs), nor a blow on one left wide open.
            bool portal = ThroughPortal(attacker, kind);
            BattleUnit warden = _timeShot || portal || wideOpen ? null : GuardingWarden(target);
            if (portal && target.Class == UnitClass.Medic) damage = More(damage, hit, HitLayer.Combo, ClassSpells.CornerCritPercent);
            // Wrong Door: a blow on an ally beside his ward comes through a portal onto his shield, and its hook, the door's
            // edge leaves whoever struck it bleeding (BattleSimulation.Combos.cs).
            bool door = false;
            if (warden == null && _comboOn && !_timeShot && !portal && !wideOpen && (warden = WrongDoor(target)) != null)
            {
                door = true;
                Emit(EventFor(BattleEventKind.Portal, warden, target, 0, ClassSpells.Name(ClassBend.WrongDoor)));
            }
            if (warden != null)
            {
                int cut = _classOn ? GuardCut(warden) : SliceClasses.GuardReductionPercent;
                if (hit != null) GuardedHit(attacker, target, warden, hit, cut);
                else
                {
                    int guarded = damage * (100 - cut) / 100;
                    int prevented = damage - guarded;
                    Emit(EventFor(BattleEventKind.Guard, warden, target, prevented, string.Empty));
                    ApplyDamage(attacker, attacker.Side, warden, Armored(warden, guarded), GuardedDetail);
                }
                Bleed(attacker, warden);
                // A burning fighter's blow burns whoever it lands on (BattleSimulation.Burn.cs).
                if (_roadKitOn) Burned(attacker, warden);
                if (_classOn) Retaliate(warden, attacker);
                // Reprisal (rules 30, BattleSimulation.Vows.cs).
                if (_vowsOn) Reprisal(warden, attacker);
                if (door)
                {
                    Wound(warden, attacker);
                    Lay(warden, attacker, ClassBend.WrongDoor);
                }
                return;
            }

            // A band weak to Focus Fire takes more from a focused blow that lands (BandTrait.WeakToFocus).
            if (HasBand(target.Side, BandTrait.WeakToFocus) && IsFocusing(attacker.Side, target))
                damage = More(damage, hit, HitLayer.Status, BandRules.WeakFocusPercent);
            // The marks a class spell cashes in on whoever it lands on (BattleSimulation.Class.cs).
            string detail = string.Empty;
            if (kind != BlowKind.Auto)
            {
                if (hit != null) CashInHit(attacker, target, kind, hit, out detail);
                else damage = CashIn(attacker, target, kind, damage, out detail);
            }
            if (hit != null) ApplyHit(attacker, attacker.Side, target, Armor(target, hit, attacker), detail);
            else ApplyDamage(attacker, attacker.Side, target, Armored(target, damage), detail);
            if (_roadKitOn) Burned(attacker, target);
            // Under road combat a Trapper's Snap Trap snares the melee enemy who hit her (BattleSimulation.Defence.cs).
            if (_roadCombatOn && target.Alive) SnapTrapped(attacker, target);
            // A blow on a chief whose ring spins or whose Sun Mantle burns (ChiefKits, 22 Oct 2026).
            if (_kitsTwoOn) KitStruck(attacker, target);
            if (kind != BlowKind.Auto) AfterSpellBlow(attacker, target, kind, detail);
            // From rules 30 a combo's wearer marks with every blow (BattleSimulation.Flavours.cs).
            if (_marksOnHitsOn && !_finished && target.Alive) HitMark(attacker, target);
            SnareSpent(target, detail);
            Bleed(attacker, target);
        }

        // A blow's bonuses before rules 17, each rounding down on its own (BlowHit takes the same as layers).
        private int Bonuses(BattleUnit attacker, BattleUnit target, int damage, BlowKind kind)
        {
            SideState mine = _sides[attacker.Side];
            SideState theirs = _sides[target.Side];
            if (mine.UnleashTicks > 0) damage = damage * UnleashPercent(attacker.Side) / 100;
            if (mine.FrenzyTicks > 0)
                damage = damage * (attacker.Role == UnitRole.Pal ? Moves.FrenzyPalPowerPercent : Moves.FrenzyAllyPowerPercent) / 100;
            if (mine.RallyTicks > 0) damage = damage * RallyDamagePercent / 100;
            if (attacker.RiseTicks > 0) damage = damage * FightRules.RiseDamagePercent / 100;
            if (theirs.HoldTicks > 0 && target.Row == Row.Front) damage = damage * HoldSoak(target.Side) / 100;
            // A hunter band's traits (BattleSimulation.Band.cs): berserk, and Thick Hide on plain blows.
            if (_bandOn) damage = BandBlow(attacker, target, damage);
            // The flooding side's blows land harder as the water rises (FloodRules).
            if (_floodOn) damage = FloodBlow(attacker.Side, damage);
            // So do the hunters' blows on their prey's row while they drive it (BattleSimulation.Hunt.cs), and the Stoneveil
            // chief's band's after he drank his Basin (BattleSimulation.Stoneveil.cs).
            if (_huntOn) damage = DriveBlow(attacker, target, damage);
            if (_riteOn) damage = DrinkBlow(attacker, damage);

            // Tribe traits (6 Oct 2026, see Learning.TribeTraits): hunters hit the wounded harder.
            int wounded = SpecOf(attacker).WoundedBonusPercent;
            if (wounded > 0 && target.HpPermille < WoundedBelowPermille) damage = damage * (100 + wounded) / 100;

            // A class spell's own weight (BattleSimulation.Class.cs), and a road scar's against a beast (BattleSimulation.Beast.cs).
            if (kind != BlowKind.Auto) damage = SpellWeight(attacker, kind, damage);
            // Stopped Bullets' third shot carries all three (BattleSimulation.Combos.cs).
            if (kind == BlowKind.AimedShot && _shotsAtOnce > 1) damage *= _shotsAtOnce;
            if (_beastOn && kind != BlowKind.Auto) damage = ScarSpell(attacker, target, damage);
            return damage;
        }

        /// Tribe traits (6 Oct 2026): hunters hit an enemy below this share of its health harder.
        public const int WoundedBelowPermille = 500;

        /// In the slice's fights most blows go to whoever stands across from the attacker (see
        /// LaneScore): the fighter straight across scores this much extra, the far end of a row 0.
        public const int LaneWeight = 400;

        /// What a unit's armour (Learning.TribeTraits) leaves of a hit, never below 1.
        private int Armored(BattleUnit victim, int damage)
        {
            int armor = Cracked(victim, SpecOf(victim).ArmorPercent);
            return armor > 0 ? Math.Max(1, damage * (100 - armor) / 100) : damage;
        }

        /// The spec a unit was made from. Units are numbered side 0 first, in setup order.
        private UnitSpec SpecOf(BattleUnit unit)
        {
            IReadOnlyList<UnitSpec> side0 = _setup.Side(0);
            return unit.Index < side0.Count ? side0[unit.Index] : _setup.Side(1)[unit.Index - side0.Count];
        }

        /// Jacob's playtest, 6 Oct 2026: every enemy piled onto the Butcher, the first fighter in
        /// your line, and he took 40% of all their damage. Now each attacker prefers whoever
        /// stands across from it, so a line spreads its blows; the most hurt still draw the rest.
        private int LaneScore(BattleUnit attacker, BattleUnit target)
        {
            int distance = Math.Abs(LanePosition(attacker) - LanePosition(target));
            return LaneWeight - distance * LaneWeight / 1000;
        }

        /// Where a unit stands across its row, 0 to 1000, by setup order among its side's units in
        /// that row. The dead keep their place. A row of one stands in the middle.
        private int LanePosition(BattleUnit unit)
        {
            int slot = 0;
            int count = 0;
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (u.Side != unit.Side || u.Row != unit.Row) continue;
                if (u.Index < unit.Index) slot++;
                count++;
            }
            return count <= 1 ? 500 : slot * 1000 / (count - 1);
        }

        // ------------------------------------------------------------ questions the screen asks (11 Oct 2026)
        // Read only: none of these draws a random number or changes anything, so the frontend may ask every
        // frame and a replay stays the same. They answer from the same helpers ChooseTarget uses, so the
        // screen never keeps its own copy of a combat rule (AGENTS.md).

        /// Why this fighter may not strike that enemy right now, or StrikeBlock.None when it may. The checks
        /// ChooseTarget makes, in its order.
        public StrikeBlock CanStrike(BattleUnit attacker, BattleUnit target)
        {
            if (attacker == null || !attacker.Alive) return StrikeBlock.AttackerDown;
            if (target == null || !target.Alive) return StrikeBlock.TargetDown;
            if (target.Side == attacker.Side) return StrikeBlock.SameSide;
            if (LeaderProtected(target)) return StrikeBlock.SquadProtectsLeader;
            StrikeBlock held = MeleeHeldBack(attacker);
            if (held != StrikeBlock.None) return held;
            // One her Hook dragged stands in front of her shields, in anyone's reach (ChiefKits).
            if (target.Row == Row.Back && !CanReachBack(attacker) && target.HookTicks <= 0) return StrikeBlock.FrontStands;
            if (_beastOn && Shielded(target)) return StrikeBlock.Caged;
            return StrikeBlock.None;
        }

        /// How many fighters on the other side may strike this target right now; living is how many of
        /// that side still stand.
        public int StrikersOn(BattleUnit target, out int living)
        {
            living = 0;
            if (target == null) return 0;
            int able = 0;
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (!u.Alive || u.Side == target.Side) continue;
                living++;
                if (CanStrike(u, target) == StrikeBlock.None) able++;
            }
            return able;
        }

        /// The Warden covering this fighter right now, or null: hits on it land on that Warden instead.
        public BattleUnit CoveredBy(BattleUnit unit)
        {
            return unit == null ? null : GuardingWarden(unit);
        }

        /// How the fight ended, read from its own events.
        public FightEnding Ending
        {
            get
            {
                if (_finished)
                {
                    for (int i = _events.Count - 1; i >= 0; i--)
                    {
                        BattleEvent e = _events[i];
                        if (e.Kind == BattleEventKind.Break)
                            return new FightEnding(e.Detail == Moves.LeaderDetail ? FightEnd.LeaderFell : FightEnd.NerveBreak,
                                e.ActorSide, e.Amount, e.Tick);
                        if (e.Kind != BattleEventKind.End) continue;
                        // The end event's side is the winner: a wipe happened to the other side, or to both.
                        if (e.Detail == "wipe") return new FightEnding(FightEnd.Wipe, e.ActorSide < 0 ? -1 : 1 - e.ActorSide, 0, e.Tick);
                        if (e.Detail == "time") return new FightEnding(FightEnd.TimeUp, -1, 0, e.Tick);
                        // A beast dug away in its Last stand (BattleSimulation.Beast.cs).
                        if (e.Detail == EscapedReason) return new FightEnding(FightEnd.Escaped, 1 - e.ActorSide, AliveCount(1 - e.ActorSide), e.Tick);
                    }
                }
                return new FightEnding(FightEnd.None, -1, 0, -1);
            }
        }

        /// Why a melee fighter strikes nobody right now, or None: either side's Fall Back keeps the lines
        /// apart, or a learned habit waits for its own Warden to spring the enemy's trap.
        private StrikeBlock MeleeHeldBack(BattleUnit attacker)
        {
            // A frenzied fighter ignores Fall Back (BattleSimulation.Road.cs).
            if (attacker.Stats.Ranged || attacker.Frenzied) return StrikeBlock.None;
            SideState mine = _sides[attacker.Side];
            SideState theirs = _sides[1 - attacker.Side];
            // A band with Long Reach still reaches you through your Fall Back (BandTrait.LongReach), not through its own.
            // Shadow Strike, Backstab's last form (rules 25): your side keeps striking while it falls back, and they still can't
            // reach you (BattleSimulation.Shade.cs).
            // Rift Mine's price (rules 25): your attacks wait out its return too.
            if ((mine.FallBackTicks > 0 && !ShadowStrike(attacker.Side)) || (_shadeOn && mine.ReturnTicks > 0) ||
                (theirs.FallBackTicks > 0 && !HasBand(attacker.Side, BandTrait.LongReach)))
                return StrikeBlock.FallBack;
            // Never on an Ambusher's trap, which no light blow springs (rules 24).
            if (attacker.Class != UnitClass.Warden && theirs.TrapArmed && !AmbushWaits(1 - attacker.Side) &&
                _tactics[attacker.Side].TrapCaution >= HabitThreshold && FirstLivingWarden(attacker.Side) != null)
                return StrikeBlock.WaitsOutTrap;
            return StrikeBlock.None;
        }

        /// Ranged fighters and Skirmishers reach the back row; other melee only once the enemy front is down.
        private bool CanReachBack(BattleUnit attacker)
        {
            if (attacker.Stats.Ranged || attacker.Class == UnitClass.Skirmisher) return true;
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (u.Alive && u.Side != attacker.Side && u.Row == Row.Front) return false;
            }
            return true;
        }

        private BattleUnit ChooseTarget(BattleUnit attacker)
        {
            SideState mine = _sides[attacker.Side];
            TacticsProfile tactics = _tactics[attacker.Side];
            bool melee = !attacker.Stats.Ranged;

            // Either side has fallen back (no melee lands for now), or a learned habit lets our own
            // Warden spring their trap first.
            if (MeleeHeldBack(attacker) != StrikeBlock.None) return null;

            // Her side goes for whoever her Hook dragged in front of her shields (ChiefKits, 22 Oct 2026).
            BattleUnit dragged = _kitsTwoOn ? DraggedFoe(attacker.Side) : null;
            if (dragged != null && !LeaderProtected(dragged)) return dragged;

            bool canReachBack = CanReachBack(attacker);

            // A frenzied fighter ignores his side's Focus Fire target (BattleSimulation.Road.cs).
            if (mine.FocusTicks > 0 && mine.FocusTargetId.Length > 0 && !attacker.Frenzied)
            {
                BattleUnit focus = FindById(mine.FocusTargetId);
                if (focus != null && focus.Alive && !LeaderProtected(focus) && focus.Side != attacker.Side &&
                    (focus.Row == Row.Front || canReachBack) && !Spared(attacker, focus) && focus.EvacTicks <= 0)
                    return focus;
            }

            // Priorities first, then the most hurt, then (in the slice's fights) whoever stands
            // across from the attacker, then the lower index.
            BattleUnit best = null;
            int bestScore = int.MinValue;
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit e = _units[i];
                if (!e.Alive || e.Side == attacker.Side || LeaderProtected(e)) continue;
                if (Spared(attacker, e)) continue; // a kneeling chief your side spares (ChiefFight)
                if (e.EvacTicks > 0) continue; // out of reach after an Evac (BattleSimulation.Combos.cs)
                if (e.Row == Row.Back && !canReachBack) continue;
                int score = tactics.TargetWeight(e.Class) * 1000 + (1000 - e.HpPermille);
                if (_movesOn) score += LaneScore(attacker, e);
                if (attacker.Class == UnitClass.Skirmisher && e.Row == Row.Back) score += 500;
                score += DoctrineScore(attacker, e, tactics);
                // Ranged only: melee that dodge the guarded waste their blows (measured 1 Oct 2026). Cut under the parry at the
                // center (Jacob, 24 Oct 2026: "Shoots past your Warden: cut").
                if (!melee && !_parryOn && tactics.AvoidGuarded >= HabitThreshold && GuardingWarden(e) != null)
                    score -= tactics.AvoidGuarded * AvoidGuardedScore;
                if (score > bestScore)
                {
                    best = e;
                    bestScore = score;
                }
            }
            return best;
        }

        private BattleUnit FirstLivingWarden(int side)
        {
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (u.Alive && u.Side == side && u.Class == UnitClass.Warden) return u;
            }
            return null;
        }

        private BattleUnit GuardingWarden(BattleUnit target)
        {
            // Turned Hunt: nobody covers the hunt captain (BattleSimulation.Hunt.cs).
            if (_huntOn && Turned(target)) return null;
            // Under the parry at the center nobody covers a fighter who reels from a parry or stands open after a feint, nor a
            // Long Reach hunter while he reaches through your Fall Back (BattleSimulation.Parry.cs).
            if (_parryOn && (target.ReelTicks > 0 ||
                             (HasBand(target.Side, BandTrait.LongReach) && _sides[1 - target.Side].FallBackTicks > 0)))
                return null;
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                // From rules 24 his second ward too, and an upgraded Bodyguard's back row (BattleSimulation.Whisperer.cs).
                if (u.Alive && u.Side == target.Side && u.Class == UnitClass.Warden && !ReferenceEquals(u, target) &&
                    (string.Equals(u.GuardTargetId, target.Id, StringComparison.Ordinal) || (_whisperOn && AlsoGuards(u, target))) &&
                    !(_shadeOn && u.Statuses[StatusKind.NoCover].On))
                    return u;
            }
            return null;
        }

        /// leaveOne: the blow never takes the victim's last health (ChiefKits: your Sunbreaker breaks a chief, it never kills him).
        private void ApplyDamage(BattleUnit source, int sourceSide, BattleUnit victim, int amount, string detail, bool leaveOne = false)
        {
            // From rules 17 every hit goes through the damage layers, this one with its own number as the base
            // (BattleSimulation.Layers.cs).
            if (_layersOn)
            {
                ApplyHit(source, sourceSide, victim, new Hit(amount), detail, leaveOne);
                return;
            }
            if (_finished || !victim.Alive || amount <= 0) return;
            // Covers splash, periodic damage and already committed attacks as well as targeting.
            // Re-evaluate every hit: the leader becomes vulnerable as soon as the last ally falls.
            if (LeaderProtected(victim)) return;
            // A kneeling chief stops the fight: nobody is hurt any more until the verdict (BattleSimulation.Kit.cs).
            if (_halted) return;
            // An Evac takes the fighter out of reach, and a Frozen Flurry keeps every hit until its mark thaws
            // (BattleSimulation.Combos.cs).
            if (_comboOn && (victim.EvacTicks > 0 || FlurryKeeps(victim, sourceSide, amount, detail))) return;

            // A standing Steel Storm wall: the enemy's blows barely land (ChiefSpells, 18 Oct 2026); through a half built one
            // they land at half, and behind the Skullpeak chief's Lockdown wall they barely land (ChiefKits, 22 Oct 2026).
            if (_spellsOn && _sides[victim.Side].WallTicks > 0 && sourceSide == 1 - victim.Side)
                amount = Math.Max(1, amount * WallPercentOf(victim.Side) / 100);
            else if (_kitsTwoOn && sourceSide == 1 - victim.Side && LockdownHolds(victim.Side))
                amount = Math.Max(1, amount * ChiefSpells.WallDamagePercent / 100);

            // The cage bear's ribcage: every blow on it lands lighter while it stands, and none reaches its heart
            // (BattleSimulation.Beast.cs).
            if (_beastOn && victim.Side == _beastSide && sourceSide == 1 - victim.Side)
            {
                amount = BeastTakes(victim, sourceSide, amount);
                if (amount <= 0) return;
            }

            // A chief worn out by his own Eclipse takes double from every blow.
            if (victim.Role == UnitRole.Chief && WornOutTicksLeft(victim.Side) > 0)
                amount = amount * Moves.WornOutDamagePercent / 100;

            // Turned Hunt: your blows on the hunt captain land double (BattleSimulation.Hunt.cs).
            if (_huntOn && Turned(victim) && sourceSide == 1 - victim.Side) amount = amount * ChiefKits.TurnedPercent / 100;

            // Gear that guards against big hits takes a share off an Eclipse (Learning.CampaignGear).
            int guard = SpecOf(victim).BigHitArmorPercent;
            if (guard > 0 && detail == Moves.EclipseDetail) amount = Math.Max(1, amount * (100 - guard) / 100);

            if (leaveOne) amount = Math.Min(amount, victim.Hp - 1);
            if (amount <= 0) return;
            Land(source, sourceSide, victim, amount, detail);
        }

        // A hit lands, from ApplyDamage or the damage layers' ApplyHit: the health it takes, a kill, and all a landed hit sets off.
        private void Land(BattleUnit source, int sourceSide, BattleUnit victim, int amount, string detail)
        {
            int hpLost = Math.Min(amount, victim.Hp);
            victim.Hp -= amount;
            Emit(new BattleEvent(_tick, BattleEventKind.Hit, sourceSide,
                source == null ? string.Empty : source.Id, source == null ? -1 : (int)source.Class,
                victim.Id, (int)victim.Class, amount, detail));
            ConfirmTeamwork(source, victim, detail);
            // Rupture's steps and Echo's record (rules 25, BattleSimulation.Shade.cs).
            if (_shadeOn) ShadeLanded(source, sourceSide, victim, hpLost, detail);
            // What he lost and who struck him, for a Rewind; and a Medic's Evac pulls a falling fighter out alive
            // (BattleSimulation.Combos.cs).
            if (_comboOn)
            {
                Hurt(victim, hpLost, source, sourceSide, detail);
                if (victim.Hp <= 0) Evac(victim, source, sourceSide, detail);
            }
            bool killed = false;
            if (victim.Hp <= 0)
            {
                victim.Hp = 0;
                victim.Alive = false;
                victim.GuardTargetId = string.Empty;
                victim.AimTicks = 0;
                victim.AimTargetId = string.Empty;
                victim.HookTicks = 0;
                victim.RifleTicks = 0;
                Emit(new BattleEvent(_tick, BattleEventKind.Kill, sourceSide,
                    source == null ? string.Empty : source.Id, source == null ? -1 : (int)source.Class,
                    victim.Id, (int)victim.Class, 0, detail));
                killed = true;
                // Fear, the Shade's Focus Fire trick (rules 23, BattleSimulation.Shade.cs).
                if (_floorOn && (sourceSide == 0 || sourceSide == 1) && sourceSide != victim.Side) Fear(sourceSide, victim);
                // Bleeder (rules 30, BattleSimulation.Vows.cs): his wound jumps on.
                if (_vowsOn) BledOn(sourceSide, victim);
                // A beast's part broke (BattleSimulation.Beast.cs).
                if (_beastOn && victim.Side == _beastSide) Severed(source, sourceSide, victim);
                // Her last Warden fell: her Lockdown wall is down for good (BattleSimulation.Skullpeak.cs).
                if (_kitsTwoOn && victim.Class == UnitClass.Warden) WardenFell(victim);
            }

            // A caster loses the move to blows from a side that is focusing it: only a Focus Fire on
            // the caster breaks a cast (Jacob's playtest, 6 Oct 2026: stray hits on the Butcher broke
            // his Eclipse in 371 of the Smart autopilot's 600 casts).
            SideState victimState = _sides[victim.Side];
            // A Steel Storm cannot be interrupted: Unleash is its one answer (18 Oct 2026). Nor can a Firebomb.
            if (!killed && victimState.Windup > 0 && string.Equals(victimState.CasterId, victim.Id, StringComparison.Ordinal) &&
                ChiefSpells.CanBreak(_rules[victim.Side].Spell) && !RaidChiefSide(victim.Side) && IsFocusing(sourceSide, victim))
            {
                victimState.WindupDamage += hpLost;
                if (victimState.WindupDamage * 100 >= Moves.InterruptPercent * victim.MaxHp) Interrupt(victim, source);
            }

            // A blow from the enemy on an aiming Marksman loses his shot; a wound bleeding is no blow (ClassSpells), nor is
            // the Stoneveil chief's rite drinking or drawing blood (BattleSimulation.Stoneveil.cs).
            if (!killed && _classOn && victim.AimTicks > 0 && sourceSide == 1 - victim.Side && !NoBlow(detail))
                BreakAim(victim, source);

            // A blow on a Still Wall Warden freezes whoever struck it (BattleSimulation.Combos.cs).
            if (!killed && _comboOn && source != null && sourceSide == 1 - victim.Side && !NoBlow(detail)) StillWall(victim, source);

            // A blow from a side focusing one of her Rifle Line shooters loses him his shot (ChiefKits, 22 Oct 2026).
            if (!killed && _kitsTwoOn && victim.RifleTicks > 0 && sourceSide == 1 - victim.Side && !NoBlow(detail) &&
                IsFocusing(sourceSide, victim))
                LoseShot(victim, source);

            // Under road combat a Medic's Self Patch (BattleSimulation.Defence.cs).
            if (!killed && _roadCombatOn) SelfPatched(victim);

            // A chief at half health gets his second phase, at a quarter he yields (ChiefFight).
            if (!killed && _chiefOn && victim.Role == UnitRole.Chief) ChiefStruck(victim);

            // A berserker band's leader just fell below half (BandTrait.Berserk).
            if (!killed && _bandOn && victim.Role == UnitRole.Chief) CheckBerserk(victim);

            if (_nerveOn)
            {
                // A frightening attacker's hits shake the victim's side harder.
                int shaken = source == null ? hpLost : hpLost * (100 + _fear[source.Index]) / 100;
                // A terrifying band shakes twice as hard whoever is not holding (BandTrait.Terror).
                if (sourceSide == 1 - victim.Side && HasBand(sourceSide, BandTrait.Terror) && victimState.HoldTicks == 0)
                    shaken = shaken * BandRules.TerrorPercent / 100;
                // In a road combat road fight the blow shakes the one it hit, and a fall shakes his comrades
                // (BattleSimulation.Road.cs).
                if (_unitNerveOn)
                {
                    UnitShaken(victim, shaken, sourceSide);
                    if (killed) ComradeFell(victim, sourceSide);
                }
                else
                {
                    DrainForHit(victim.Side, shaken, sourceSide);
                    if (killed) DrainNerve(victim.Side, NerveDeathCost, sourceSide);
                }
                // The Whisperer's favour: the one you focused falling breaks more of their nerve (SideFavours, 18 Oct 2026).
                int foe = 1 - victim.Side;
                if (killed && !_finished && _rules[foe].Favours.FocusFallNervePercent > 0 && IsFocusing(foe, victim))
                    DrainNerve(victim.Side, NerveDeathCost * _rules[foe].Favours.FocusFallNervePercent / 100, foe);
                // Gear whose wearer's kills break more of their nerve (Learning.CampaignGear).
                if (killed && source != null && SpecOf(source).KillNerve > 0) DrainNerve(victim.Side, SpecOf(source).KillNerve, sourceSide);
            }

            // Earned points for the kill, and a rallying side's answer (BattleSimulation.Skill.cs).
            if (killed && !_finished && _skillOn) OnKill(sourceSide, victim);

            // A side whose Pal or Chief drops breaks on the spot.
            if (killed && victim.IsLeader) LeaderDown(victim.Side);

            // Her tricks under the duos: Marked Tricks and Shadow Star (rules 27, BattleSimulation.Duos.cs).
            if (_duosOn && !_finished) DuoLanded(sourceSide, victim, detail);
        }

        // Damage that is no blow: a wound bleeding, and the Stoneveil chief's Leeches drinking or Transfusion drawing.
        private static bool NoBlow(string detail)
        {
            return detail == BleedRules.Detail || detail == ChiefKits.LeechDetail || detail == ChiefKits.TransfusionDetail ||
                detail == BurnRules.Detail;
        }

        /// True while this side's Focus Fire is on this unit.
        private bool IsFocusing(int side, BattleUnit unit)
        {
            if (side != 0 && side != 1) return false;
            SideState state = _sides[side];
            return side != unit.Side && state.FocusTicks > 0 &&
                string.Equals(state.FocusTargetId, unit.Id, StringComparison.Ordinal);
        }

        /// Ticks a side's chief is still worn out by his own Eclipse, or 0 (Jacob's playtest, 6 Oct
        /// 2026: the rematch chief was a wall). For Moves.WornOutTicks after his Eclipse goes off,
        /// dodged or not, he takes Moves.WornOutDamagePercent of every blow, so a dodge can be
        /// punished. Read from the fight's own events, so it replays exactly.
        public int WornOutTicksLeft(int side)
        {
            for (int i = _events.Count - 1; i >= 0; i--)
            {
                BattleEvent e = _events[i];
                long age = _tick - e.Tick;
                if (age >= Moves.WornOutTicks) return 0;
                if (e.Kind != BattleEventKind.Release || e.ActorSide != side) continue;
                BattleUnit caster = FindById(e.ActorId);
                if (caster == null || caster.Role != UnitRole.Chief) return 0;
                return (int)(Moves.WornOutTicks - age);
            }
            return 0;
        }

        private void Heal(BattleUnit healer, BattleUnit patient, int amount, string detail)
        {
            // From rules 17 every heal goes through the damage layers too (BattleSimulation.Layers.cs).
            if (_layersOn)
            {
                HealHit(healer, patient, new Hit(amount), detail);
                return;
            }
            // Facing the Stoneveil chief your Medic's treatment plucks his Leeches off or stitches a wound shut, healed or not
            // (BattleSimulation.Stoneveil.cs).
            if (_riteOn)
            {
                RiteTreated(healer, patient);
                if (Stopped) return;
            }
            Restore(healer, patient, amount, detail);
        }

        // A heal lands, from Heal or the damage layers' HealHit: what the patient was missing of it, and what it sets off.
        private void Restore(BattleUnit healer, BattleUnit patient, int amount, string detail)
        {
            int healed = Math.Min(amount, patient.MaxHp - patient.Hp);
            if (healed <= 0) return;
            patient.Hp += healed;
            Emit(EventFor(BattleEventKind.Heal, healer, patient, healed, detail));
            // Your Medic treating the hunters' prey can break their hunt (BattleSimulation.Hunt.cs).
            if (_huntOn) HuntHealed(healer, patient, healed);
        }

        /// A chosen death: it heals the allies and costs your own side no nerve. An empowered
        /// Sacrifice heals twice as much.
        private void Sacrifice(BattleUnit victim, int percent)
        {
            victim.Hp = 0;
            victim.Alive = false;
            victim.GuardTargetId = string.Empty;
            Emit(EventFor(BattleEventKind.Kill, victim, victim, 0, SacrificeDetail));
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit ally = _units[i];
                if (!ally.Alive || ally.Side != victim.Side) continue;
                Heal(victim, ally, ally.MaxHp * SacrificeHealPercent / 100 * percent / 100, SacrificeDetail);
            }
            CheckWipe();
        }

        // ------------------------------------------------------------ nerve

        /// Hits drain nerve in proportion to the side's starting health: losing all of it costs
        /// NerveHitPercent of a full bar. The remainder carries, so small hits add up exactly.
        private void DrainForHit(int side, int hpLost, int causedBySide)
        {
            SideState state = _sides[side];
            if (hpLost <= 0 || state.StartHp <= 0) return;
            // A raid chief's bar is his posture: only your answers crack it, never plain hits (BattleSimulation.Raid.cs).
            if (RaidChiefSide(side)) return;
            int divisor = 100 * state.StartHp;
            int scaled = hpLost * NerveHitPercent * NerveRules.Full + state.NerveRemainder;
            state.NerveRemainder = scaled % divisor;
            DrainNerve(side, scaled / divisor, causedBySide);
        }

        /// Takes nerve from a side. What the enemy causes is raised by the enemy's fear bonus.
        /// At zero the side breaks, unless nobody is left standing: that is a wipe, not a break.
        private void DrainNerve(int side, int amount, int causedBySide)
        {
            if (!_nerveOn || _finished || amount <= 0) return;
            // A beast has no nerve: you beat it by breaking its parts (BeastKits, 21 Oct 2026).
            if (_beastOn && side == _beastSide) return;
            if (causedBySide == 1 - side)
                amount = amount * (100 + _setup.Nerve.FearBonusPercent(causedBySide)) / 100;
            // In a road combat road fight there is no shared bar: it is shared out over the side's fighters
            // (BattleSimulation.Road.cs).
            if (_unitNerveOn)
            {
                SpreadNerve(side, amount);
                return;
            }
            SideState state = _sides[side];
            int before = state.Nerve;
            state.Nerve -= amount;
            // A band's banner holds its nerve up while it flies (BandTrait.Banner).
            if (_bandOn && state.BannerUp && state.Nerve < BandRules.BannerFloor) state.Nerve = Math.Min(before, BandRules.BannerFloor);
            if (state.Nerve > 0)
            {
                if (_skillOn) AfterNerveLoss(side);
                return;
            }
            state.Nerve = 0;
            if (AliveCount(side) == 0) return;
            // A band with a second wind rallies once while its leader stands (BandTrait.SecondWind).
            if (_bandOn && TrySecondWind(side)) return;
            // A chief whose nerve runs out kneels for the other side's verdict instead (BattleSimulation.Kit.cs).
            if (_kitsOn && TryKneel(side)) return;
            BreakSide(side, BreakDetail);
        }

        /// The side's Pal or Chief dropped: its nerve is gone and it breaks, unless nobody is left.
        private void LeaderDown(int side)
        {
            if (_finished) return;
            // In a road combat road fight a leader falling does not break his side: it fights on until nobody is left
            // (BattleSimulation.Road.cs).
            if (_unitNerveOn)
            {
                CheckWipe();
                return;
            }
            if (AliveCount(side) == 0)
            {
                CheckWipe();
                return;
            }
            _sides[side].Nerve = 0;
            BreakSide(side, Moves.LeaderDetail);
        }

        private void BreakSide(int side, string detail)
        {
            _brokenSide = side;
            Emit(new BattleEvent(_tick, BattleEventKind.Break, side, string.Empty, -1, string.Empty, -1,
                AliveCount(side), detail));
            Finish(1 - side, detail);
        }

        // ------------------------------------------------------------ endings

        private void CheckWipe()
        {
            if (_finished) return;
            int alive0 = AliveCount(0);
            int alive1 = AliveCount(1);
            if (alive0 > 0 && alive1 > 0) return;
            Finish(alive0 > 0 ? 0 : (alive1 > 0 ? 1 : -1), "wipe");
        }

        private void EndOnTime()
        {
            // The shark dives away when the clock runs out and leaves you the field; the cage bear holds its ground
            // (BeastKits, 21 and 22 Oct 2026).
            if (_beastOn)
            {
                BeastTimeUp();
                return;
            }
            int total0 = 0;
            int total1 = 0;
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (!u.Alive) continue;
                if (u.Side == 0) total0 += u.HpPermille;
                else total1 += u.HpPermille;
            }
            Finish(total0 > total1 ? 0 : (total1 > total0 ? 1 : -1), "time");
        }

        private void Finish(int winner, string reason)
        {
            _finished = true;
            _winner = winner;
            _endTick = _tick;
            Emit(new BattleEvent(_tick, BattleEventKind.End, winner, string.Empty, -1, string.Empty, -1, 0, reason));
        }

        private BattleEvent EventFor(BattleEventKind kind, BattleUnit actor, BattleUnit target, int amount, string detail)
        {
            return new BattleEvent(_tick, kind, actor.Side, actor.Id, (int)actor.Class,
                target == null ? string.Empty : target.Id, target == null ? -1 : (int)target.Class, amount, detail);
        }

        private void Emit(BattleEvent e)
        {
            // From rules 22 every parry a side lands fills its parry meter (BattleSimulation.Meter.cs).
            if (_meterOn && e.Kind == BattleEventKind.Parry) MeterParried(e.ActorSide);
            // A killing combo can earn its bond after damage ended the fight. Keep that award,
            // but End must remain the terminal event. Historical event order is frozen.
            if (SquadScreenOn && _events.Count > 0 && _events[_events.Count - 1].Kind == BattleEventKind.End)
            {
                _events.Insert(_events.Count - 1, e);
                _eventHash = FnvOffset;
                for (int i = 0; i < _events.Count; i++) HashEvent(_events[i]);
                return;
            }
            _events.Add(e);
            HashEvent(e);
        }

        private void HashEvent(BattleEvent e)
        {
            Hash(e.Tick.ToString(CultureInfo.InvariantCulture));
            Hash(((int)e.Kind).ToString(CultureInfo.InvariantCulture));
            Hash(e.ActorSide.ToString(CultureInfo.InvariantCulture));
            Hash(e.ActorId);
            Hash(e.ActorClass.ToString(CultureInfo.InvariantCulture));
            Hash(e.TargetId);
            Hash(e.TargetClass.ToString(CultureInfo.InvariantCulture));
            Hash(e.Amount.ToString(CultureInfo.InvariantCulture));
            Hash(e.Detail);
        }

        private void Hash(string s)
        {
            for (int i = 0; i < s.Length; i++)
            {
                _eventHash ^= s[i];
                _eventHash *= FnvPrime;
            }
            _eventHash ^= '|';
            _eventHash *= FnvPrime;
        }
    }
}
