using System;
using System.Collections.Generic;

namespace KindredSiege.Sim.Battle
{
    /// What a unit is to its side (Jacob, 4 Oct 2026). A Pal leads the player's side and a Chief
    /// leads a tribe's: if either drops, its side breaks on the spot, and neither can be
    /// sacrificed. Everyone else is a Fighter, which is all the older fights ever had.
    public enum UnitRole
    {
        Fighter = 0,
        Pal = 1,
        Chief = 2
    }

    /// One fighter to place in a battle. Row defaults to the class's usual row. Power and health
    /// are in percent of the class's numbers (tiers, chiefs and gear raise them); 100 is plain.
    public readonly struct UnitSpec
    {
        public const int PlainPercent = 100;

        public UnitSpec(string id, UnitClass unitClass)
            : this(id, unitClass, SliceClasses.Stats(unitClass).DefaultRow)
        {
        }

        public UnitSpec(string id, UnitClass unitClass, Row row)
            : this(id, unitClass, row, UnitRole.Fighter, PlainPercent, PlainPercent)
        {
        }

        public UnitSpec(string id, UnitClass unitClass, Row row, UnitRole role, int powerPercent, int healthPercent)
            : this(id, unitClass, row, role, powerPercent, healthPercent, 0)
        {
        }

        /// The same, with fear: a percent added to the courage this unit's hits drain (gear that
        /// frightens, like the raider chief's Lava Sun Tabard). 0 is plain.
        public UnitSpec(string id, UnitClass unitClass, Row row, UnitRole role, int powerPercent, int healthPercent, int fearPercent)
            : this(id, unitClass, row, role, powerPercent, healthPercent, fearPercent, 0, 0)
        {
        }

        /// The same, with a tribe's trait (6 Oct 2026, see Learning.TribeTraits): a percent added
        /// to this unit's hits on anyone below half health, and a percent taken off every hit it
        /// takes. 0 is plain.
        public UnitSpec(string id, UnitClass unitClass, Row row, UnitRole role, int powerPercent, int healthPercent,
            int fearPercent, int woundedBonusPercent, int armorPercent)
            : this(id, unitClass, row, role, powerPercent, healthPercent, fearPercent, woundedBonusPercent, armorPercent, 0, 0)
        {
        }

        /// The same, with a tribe's aim (13 Oct 2026, see FightRules.Misses): points added to this
        /// unit's Hit and to its Avoid. 0 is plain; only fights with misses use them.
        public UnitSpec(string id, UnitClass unitClass, Row row, UnitRole role, int powerPercent, int healthPercent,
            int fearPercent, int woundedBonusPercent, int armorPercent, int hitBonus, int avoidBonus)
            : this(id, unitClass, row, role, powerPercent, healthPercent, fearPercent, woundedBonusPercent, armorPercent,
                hitBonus, avoidBonus, 0, 0, 0)
        {
        }

        /// The same, with a fighter's quirk and gear (Sprint 2, 13 Oct 2026, see Learning.CampaignGear): points
        /// added to the chance this unit rises rather than panics when its side's nerve snaps, nerve its kills
        /// take from the victim's side on top of the usual cost, and a percent taken off a big hit (an Eclipse)
        /// on it. 0 is plain. A class piece's bend (ClassBend, 20 Oct 2026) only counts in fights with class spells. Road scars
        /// (BeastKits, 21 Oct 2026) only count in a beast fight.
        public UnitSpec(string id, UnitClass unitClass, Row row, UnitRole role, int powerPercent, int healthPercent,
            int fearPercent, int woundedBonusPercent, int armorPercent, int hitBonus, int avoidBonus,
            int riseBonus, int killNerve, int bigHitArmorPercent, int bleedPercent = 0, ClassBend bend = ClassBend.None,
            bool reserve = false, int beastScars = 0, int burnPercent = 0, ChiefMove carries = ChiefMove.None,
            int comboRank = 1, ClassBend bend2 = ClassBend.None, int comboRank2 = 1, int clockPercent = 0, int rank = 1)
        {
            Id = id ?? string.Empty;
            Class = unitClass;
            Row = row;
            Role = role;
            PowerPercent = Math.Max(1, powerPercent);
            HealthPercent = Math.Max(1, healthPercent);
            FearPercent = Math.Max(0, fearPercent);
            WoundedBonusPercent = Math.Max(0, woundedBonusPercent);
            ArmorPercent = Math.Max(0, Math.Min(MaxArmorPercent, armorPercent));
            HitBonus = Math.Max(-MaxAimBonus, Math.Min(MaxAimBonus, hitBonus));
            AvoidBonus = Math.Max(-MaxAimBonus, Math.Min(MaxAimBonus, avoidBonus));
            RiseBonus = Math.Max(0, Math.Min(100, riseBonus));
            KillNerve = Math.Max(0, Math.Min(NerveRules.Full, killNerve));
            BigHitArmorPercent = Math.Max(0, Math.Min(MaxArmorPercent, bigHitArmorPercent));
            BleedPercent = Math.Max(0, Math.Min(BleedRules.MaxPercent, bleedPercent));
            Bend = bend;
            Reserve = reserve;
            BeastScars = Math.Max(0, Math.Min(BeastKits.PartCount, beastScars));
            BurnPercent = Math.Max(0, Math.Min(BurnRules.MaxPercent, burnPercent));
            Carries = carries & CarryRules.Carried;
            ComboRank = Math.Max(1, Math.Min(MarkRules.MaxStacks, comboRank));
            Bend2 = bend2;
            ComboRank2 = Math.Max(1, Math.Min(MarkRules.MaxStacks, comboRank2));
            ClockPercent = Math.Max(MinClockPercent, Math.Min(MaxClockPercent, clockPercent));
            Rank = Math.Max(1, Math.Min(MaxRank, rank));
        }

        /// The same fighter wearing a class piece with this bend.
        public UnitSpec WithBend(ClassBend bend)
        {
            return new UnitSpec(Id, Class, Row, Role, PowerPercent, HealthPercent, FearPercent, WoundedBonusPercent, ArmorPercent,
                HitBonus, AvoidBonus, RiseBonus, KillNerve, BigHitArmorPercent, BleedPercent, bend, Reserve, BeastScars, BurnPercent, Carries, ComboRank, Bend2, ComboRank2, ClockPercent, Rank);
        }

        /// The same fighter held back as one of a chief's reserves (ChiefFight), or not.
        public UnitSpec AsReserve(bool reserve = true)
        {
            return new UnitSpec(Id, Class, Row, Role, PowerPercent, HealthPercent, FearPercent, WoundedBonusPercent, ArmorPercent,
                HitBonus, AvoidBonus, RiseBonus, KillNerve, BigHitArmorPercent, BleedPercent, Bend, reserve, BeastScars, BurnPercent, Carries, ComboRank, Bend2, ComboRank2, ClockPercent, Rank);
        }

        /// The same fighter carrying this many road scars from beasts (BeastKits).
        public UnitSpec WithScars(int beastScars)
        {
            return new UnitSpec(Id, Class, Row, Role, PowerPercent, HealthPercent, FearPercent, WoundedBonusPercent, ArmorPercent,
                HitBonus, AvoidBonus, RiseBonus, KillNerve, BigHitArmorPercent, BleedPercent, Bend, Reserve, beastScars, BurnPercent, Carries, ComboRank, Bend2, ComboRank2, ClockPercent, Rank);
        }

        /// The road kit (rules 12, 25 Oct 2026): this fighter's blows burn (BurnRules), and the chief's moves he carries on the
        /// road (CarryRules), from his tribe.
        public UnitSpec WithRoadKit(int burnPercent, ChiefMove carries)
        {
            return new UnitSpec(Id, Class, Row, Role, PowerPercent, HealthPercent, FearPercent, WoundedBonusPercent, ArmorPercent,
                HitBonus, AvoidBonus, RiseBonus, KillNerve, BigHitArmorPercent, BleedPercent, Bend, Reserve, BeastScars, burnPercent, carries, ComboRank, Bend2, ComboRank2, ClockPercent, Rank);
        }

        /// The same fighter with his combo at this rank (rules 18, DESIGN_LOCKS "The Butcher's theme"): a rank 2 combo lays 2
        /// of its mark's stacks at once, a rank 3 combo 3.
        public UnitSpec WithComboRank(int comboRank)
        {
            return new UnitSpec(Id, Class, Row, Role, PowerPercent, HealthPercent, FearPercent, WoundedBonusPercent, ArmorPercent,
                HitBonus, AvoidBonus, RiseBonus, KillNerve, BigHitArmorPercent, BleedPercent, Bend, Reserve, BeastScars, BurnPercent, Carries, comboRank, Bend2, ComboRank2, ClockPercent, Rank);
        }

        /// The same fighter with a second piece, his rank 3 second slot (rules 23, DESIGN_LOCKS "The floor under the three
        /// Pals"): its bend and, for a combo, its rank.
        public UnitSpec WithSecondPiece(ClassBend bend2, int comboRank2)
        {
            return new UnitSpec(Id, Class, Row, Role, PowerPercent, HealthPercent, FearPercent, WoundedBonusPercent, ArmorPercent,
                HitBonus, AvoidBonus, RiseBonus, KillNerve, BigHitArmorPercent, BleedPercent, Bend, Reserve, BeastScars, BurnPercent, Carries,
                ComboRank, bend2, comboRank2, ClockPercent, Rank);
        }

        /// The same fighter with the faster hook on his clocks (rules 23): a percent on his spell's turn, his aim and his
        /// cooldown, below 0 for faster.
        public UnitSpec WithClockPercent(int clockPercent)
        {
            return new UnitSpec(Id, Class, Row, Role, PowerPercent, HealthPercent, FearPercent, WoundedBonusPercent, ArmorPercent,
                HitBonus, AvoidBonus, RiseBonus, KillNerve, BigHitArmorPercent, BleedPercent, Bend, Reserve, BeastScars, BurnPercent, Carries,
                ComboRank, Bend2, ComboRank2, clockPercent, Rank);
        }

        /// His second piece's bend (rules 23), None without one; only fights with the floor under the three Pals use it.
        public ClassBend Bend2 { get; }

        /// His second piece's combo rank, as ComboRank is his first's.
        public int ComboRank2 { get; }

        /// The faster hook: a percent on his clocks, between MinClockPercent and MaxClockPercent. 0 unless a talent sets it.
        public int ClockPercent { get; }

        /// The same fighter at this rank (rules 24, DESIGN_LOCKS "The Whisperer's core"): from rank 2 his class spell happens one
        /// more time (BattleSimulation.Whisperer.cs).
        public UnitSpec WithRank(int rank)
        {
            return new UnitSpec(Id, Class, Row, Role, PowerPercent, HealthPercent, FearPercent, WoundedBonusPercent, ArmorPercent,
                HitBonus, AvoidBonus, RiseBonus, KillNerve, BigHitArmorPercent, BleedPercent, Bend, Reserve, BeastScars, BurnPercent, Carries,
                ComboRank, Bend2, ComboRank2, ClockPercent, rank);
        }

        /// The Whisperer's rank on him, 1 to MaxRank; only fights with FightRules.Whisper use it.
        public int Rank { get; }

        public const int MaxRank = 3;

        public const int MinClockPercent = -75;
        public const int MaxClockPercent = 300;

        /// His combo's rank, 1 to MarkRules.MaxStacks: how many of its mark's stacks it lays at once. 1 unless ranked up; only
        /// fights with the Butcher's marks use it.
        public int ComboRank { get; }

        /// Whether this fighter's blows burn (BurnRules), above 0. 0 for none.
        public int BurnPercent { get; }

        /// The chief's moves this fighter carries on the road (CarryRules.Carried). None unless set.
        public ChiefMove Carries { get; }

        /// A unit never shrugs off more of a hit than this.
        public const int MaxArmorPercent = 90;

        public string Id { get; }
        public UnitClass Class { get; }
        public Row Row { get; }
        public UnitRole Role { get; }
        public int PowerPercent { get; }
        public int HealthPercent { get; }

        /// Percent added to the courage this unit's hits drain. 0 unless its gear or tribe frightens.
        public int FearPercent { get; }

        /// Percent added to this unit's hits on an enemy below half health. 0 unless its tribe hunts.
        public int WoundedBonusPercent { get; }

        /// Percent taken off every hit this unit takes. 0 unless its tribe is armoured.
        public int ArmorPercent { get; }

        /// A tribe's aim never moves Hit or Avoid by more than this.
        public const int MaxAimBonus = 50;

        /// Points added to this unit's Hit and to its Avoid in fights with misses. 0 unless its tribe
        /// aims well, swings wild or is hard to hit.
        public int HitBonus { get; }
        public int AvoidBonus { get; }

        /// Points added to the chance this unit rises rather than panics when its side's nerve snaps. 0 unless
        /// its quirk steadies it.
        public int RiseBonus { get; }

        /// Nerve its kills take from the victim's side on top of the usual cost. 0 unless its gear breaks nerve.
        public int KillNerve { get; }

        /// Percent taken off a big hit (an Eclipse) on this unit. 0 unless its gear guards against big hits.
        public int BigHitArmorPercent { get; }

        /// Percent of their health a fighter its blows hit bleeds away each second (BleedRules, 18 Oct 2026). 0 unless its
        /// tribe or gear bleeds them.
        public int BleedPercent { get; }

        /// What its class piece does to its own class's spell (ClassBend). None without one; only fights with class
        /// spells use it.
        public ClassBend Bend { get; }

        /// One of a chief's reserves: in a fight with the chief rules it stays out of the fight until his second phase
        /// (ChiefFight). Only fights with the chief rules use it.
        public bool Reserve { get; }

        /// Road scars this fighter earned breaking a beast's parts (BeastKits, 21 Oct 2026): with any, its own spell works
        /// BeastKits.ScarPercent as well against a beast. 0 without; only beast fights use it.
        public int BeastScars { get; }
    }

    /// How a side fights: which enemy classes it goes for first, and two learned habits.
    /// Tribes learn these numbers. A fight copies them at the start and never changes them.
    public sealed class TacticsProfile
    {
        public const int MinValue = 0;
        public const int MaxValue = 10;
        public const int StartWeight = 3;

        private readonly int[] _targetWeights = new int[SliceClasses.Count];

        public TacticsProfile()
        {
            for (int i = 0; i < _targetWeights.Length; i++) _targetWeights[i] = StartWeight;
        }

        /// 0 to 10. Higher means strike this enemy class first.
        public int TargetWeight(UnitClass unitClass)
        {
            return _targetWeights[(int)unitClass];
        }

        /// 0 to 10. At 3 or more, our ranged fighters stay off whoever an enemy Warden is covering.
        /// Melee ignore it: measured on 1 Oct 2026, melee that dodge the guarded lose more fights.
        public int AvoidGuarded { get; private set; }

        /// 0 to 10. At 3 or more, melee fighters other than the Warden wait while an enemy trap
        /// is armed, and let their own Warden spring it. Not learned: measured on 1 Oct 2026, the
        /// raiders with it won 64% of fights against the Clarth instead of 98%, and hunting the
        /// Trapper instead was worse still.
        public int TrapCaution { get; private set; }

        /// 0 to 10. At 3 or more, when the enemy focuses fire on one of ours, our Warden steps
        /// in to cover that unit for as long as the focus lasts.
        public int CoverFocused { get; private set; }

        /// 0 to 10. At 3 or more, the moment the enemy uses Unleash we answer with Hold, if we
        /// have a directive point left.
        public int HoldOnUnleash { get; private set; }

        /// 0 to 10. At 3 or more, when the enemy falls back we answer with Unleash, so our
        /// ranged fighters hit harder while nobody is in melee. Needs two directive points.
        public int PressOnFallBack { get; private set; }

        /// Enemy class to focus fire on at the start of a fight, or -1 for none. Costs a point.
        /// Not learned: measured on 1 Oct 2026, opening on a fixed class made the raiders worse.
        public int FocusOpener { get; private set; } = -1;

        /// Tribe doctrines (Jacob, 13 Oct 2026: tribes react differently under threat), set by
        /// Learning.TribeDoctrines and never learned. 0 to 10; each acts at 3 or more (see
        /// BattleSimulation.Skill.cs). Rally: when one of ours falls, the rest hit harder for a while.
        public int Rally { get; private set; }

        /// Doctrine: our Wardens cover whoever is winding up a big move.
        public int GuardCaster { get; private set; }

        /// Doctrine: our fighters go for the most hurt enemy even harder.
        public int HuntWounded { get; private set; }

        /// Doctrine: the first time our nerve falls below half, we Hold.
        public int HoldFormation { get; private set; }

        /// Doctrine: when the enemy Unleashes, we Fall Back out of its way.
        public int Kite { get; private set; }

        /// Doctrine: our ranged fighters go for the enemy's back row.
        public int PickOffBack { get; private set; }

        /// True when any doctrine is set.
        public bool AnyDoctrine => Rally > 0 || GuardCaster > 0 || HuntWounded > 0 || HoldFormation > 0 || Kite > 0 || PickOffBack > 0;

        public void SetRally(int value)
        {
            Rally = Clamp(value);
        }

        public void SetGuardCaster(int value)
        {
            GuardCaster = Clamp(value);
        }

        public void SetHuntWounded(int value)
        {
            HuntWounded = Clamp(value);
        }

        public void SetHoldFormation(int value)
        {
            HoldFormation = Clamp(value);
        }

        public void SetKite(int value)
        {
            Kite = Clamp(value);
        }

        public void SetPickOffBack(int value)
        {
            PickOffBack = Clamp(value);
        }

        public void SetPressOnFallBack(int value)
        {
            PressOnFallBack = Clamp(value);
        }

        public void SetTargetWeight(UnitClass unitClass, int value)
        {
            _targetWeights[(int)unitClass] = Clamp(value);
        }

        public void SetAvoidGuarded(int value)
        {
            AvoidGuarded = Clamp(value);
        }

        public void SetTrapCaution(int value)
        {
            TrapCaution = Clamp(value);
        }

        public void SetCoverFocused(int value)
        {
            CoverFocused = Clamp(value);
        }

        public void SetHoldOnUnleash(int value)
        {
            HoldOnUnleash = Clamp(value);
        }

        /// Pass -1 to clear.
        public void SetFocusOpener(int unitClass)
        {
            FocusOpener = unitClass < 0 || unitClass >= SliceClasses.Count ? -1 : unitClass;
        }

        /// 0 to 10. At 3 or more, our caster will not let a big move go off into a Hold the enemy started
        /// within the parry window: it waits until that Hold is older, so the move lands soaked instead of
        /// parried (Jacob, 14 Oct 2026: "yes go build the parry learning").
        public int WaitOutParry { get; private set; }

        public void SetWaitOutParry(int value)
        {
            WaitOutParry = Clamp(value);
        }

        /// 0 to 10. At 3 or more, under the parry at the center (FightRules.ParryCenter), our fighters throw a fake beat before
        /// each real one: the tell flashes, nothing comes, and the faker is open meanwhile (Jacob, 24 Oct 2026: "Feints (new;
        /// learned when you parried 3 of his moves in one fight)").
        public int Feint { get; private set; }

        public void SetFeint(int value)
        {
            Feint = Clamp(value);
        }

        /// Under the squad first (FightRules.SquadFirst, 24 Oct 2026): the one move the learned wait out of the parry and the
        /// learned feint sit on, by the name the Parry event gives it ("Cleave", "Heavy", "Eclipse"). Empty for none: then the
        /// habit acts on no move under the squad first, and on every move under the parry at the center alone.
        public string WaitOutMove { get; private set; } = string.Empty;
        public string FeintMove { get; private set; } = string.Empty;

        public void SetWaitOutMove(string move)
        {
            WaitOutMove = move ?? string.Empty;
        }

        public void SetFeintMove(string move)
        {
            FeintMove = move ?? string.Empty;
        }

        public TacticsProfile Clone()
        {
            var clone = new TacticsProfile();
            Array.Copy(_targetWeights, clone._targetWeights, _targetWeights.Length);
            clone.AvoidGuarded = AvoidGuarded;
            clone.TrapCaution = TrapCaution;
            clone.CoverFocused = CoverFocused;
            clone.HoldOnUnleash = HoldOnUnleash;
            clone.PressOnFallBack = PressOnFallBack;
            clone.WaitOutParry = WaitOutParry;
            clone.Feint = Feint;
            clone.WaitOutMove = WaitOutMove;
            clone.FeintMove = FeintMove;
            clone.FocusOpener = FocusOpener;
            clone.Rally = Rally;
            clone.GuardCaster = GuardCaster;
            clone.HuntWounded = HuntWounded;
            clone.HoldFormation = HoldFormation;
            clone.Kite = Kite;
            clone.PickOffBack = PickOffBack;
            return clone;
        }

        public void AppendTo(SnapshotBuilder b, string prefix)
        {
            for (int i = 0; i < SliceClasses.All.Length; i++)
                b.Field(prefix + "w." + SliceClasses.Name(SliceClasses.All[i]), _targetWeights[i]);
            b.Field(prefix + "avoidGuarded", AvoidGuarded);
            b.Field(prefix + "trapCaution", TrapCaution);
            b.Field(prefix + "coverFocused", CoverFocused);
            b.Field(prefix + "holdOnUnleash", HoldOnUnleash);
            b.Field(prefix + "pressOnFallBack", PressOnFallBack);
            b.Field(prefix + "focusOpener", FocusOpener);
            // Doctrines only when set, so a side without them keeps its old snapshot.
            if (Rally > 0) b.Field(prefix + "rally", Rally);
            if (GuardCaster > 0) b.Field(prefix + "guardCaster", GuardCaster);
            if (HuntWounded > 0) b.Field(prefix + "huntWounded", HuntWounded);
            if (HoldFormation > 0) b.Field(prefix + "holdFormation", HoldFormation);
            if (Kite > 0) b.Field(prefix + "kite", Kite);
            if (PickOffBack > 0) b.Field(prefix + "pickOffBack", PickOffBack);
            // Learned on 14 Oct 2026, also only when set, for the same reason.
            if (WaitOutParry > 0) b.Field(prefix + "waitOutParry", WaitOutParry);
            // Learned from 24 Oct 2026, the same way.
            if (Feint > 0) b.Field(prefix + "feint", Feint);
            if (WaitOutMove.Length > 0) b.Field(prefix + "waitOutMove", WaitOutMove);
            if (FeintMove.Length > 0) b.Field(prefix + "feintMove", FeintMove);
        }

        private static int Clamp(int value)
        {
            return Math.Max(MinValue, Math.Min(MaxValue, value));
        }
    }

    /// The nerve bar (Jacob, 2 Oct 2026). Each side starts a fight with some nerve. Hits, deaths
    /// and scary orders drain it, Hold drains your own, and a side at zero breaks: the fight ends
    /// there and its survivors live. Off by default, so older fights and their tests keep their
    /// numbers. The numbers are placeholders, not balance.
    public sealed class NerveRules
    {
        /// Full nerve. Nerve is counted in thousandths of a full bar.
        public const int Full = 1000;

        /// The old rules: no nerve, fights end in a wipe or on time.
        public static readonly NerveRules Off = new NerveRules(false, Full, Full, 0, 0,
            BattleSimulation.UnleashNerveCost, 1);

        private readonly int _start0;
        private readonly int _start1;
        private readonly int _fear0;
        private readonly int _fear1;

        private NerveRules(bool enabled, int start0, int start1, int fear0, int fear1, int unleashCost, int holdDrainEveryTicks)
        {
            Enabled = enabled;
            _start0 = ClampStart(start0);
            _start1 = ClampStart(start1);
            _fear0 = Math.Max(0, fear0);
            _fear1 = Math.Max(0, fear1);
            UnleashCost = Math.Max(0, unleashCost);
            HoldDrainEveryTicks = Math.Max(1, holdDrainEveryTicks);
        }

        /// Nerve on. Pass each side's starting nerve (a tribe brings what it has left) and, for
        /// gear that frightens, a percent added to the nerve damage that side deals.
        public static NerveRules On(int startNerve0 = Full, int startNerve1 = Full, int fearBonusPercent0 = 0, int fearBonusPercent1 = 0)
        {
            return new NerveRules(true, startNerve0, startNerve1, fearBonusPercent0, fearBonusPercent1,
                BattleSimulation.UnleashNerveCost, 1);
        }

        /// The same rules with other prices on the two orders that cost nerve: what the enemy loses
        /// when you Unleash, and how often a running Hold takes one nerve from the holder (every
        /// tick by default, 150 for a full Hold). For balance work on the slice (Jacob, 4 Oct 2026).
        public NerveRules WithOrderCosts(int unleashCost, int holdDrainEveryTicks)
        {
            return new NerveRules(Enabled, _start0, _start1, _fear0, _fear1, unleashCost, holdDrainEveryTicks);
        }

        public bool Enabled { get; }

        /// Nerve the enemy loses when a side Unleashes. BattleSimulation.UnleashNerveCost unless set.
        public int UnleashCost { get; }

        /// A running Hold takes one nerve from the holder every this many ticks. 1 unless set.
        public int HoldDrainEveryTicks { get; }

        /// 1 to Full. A side cannot start a fight already broken.
        public int StartNerve(int side)
        {
            return side == 0 ? _start0 : _start1;
        }

        /// Percent added to the nerve damage this side deals to the enemy. 0 without gear.
        public int FearBonusPercent(int side)
        {
            return side == 0 ? _fear0 : _fear1;
        }

        private static int ClampStart(int nerve)
        {
            return Math.Max(1, Math.Min(Full, nerve));
        }
    }

    /// Everything needed to start the same fight again. Tactics are copied in, so a replay
    /// uses what each side knew at the time, not what it learned afterwards.
    public sealed class BattleSetup
    {
        public const int DefaultTickLimit = 3600;
        public const int DefaultDirectivePoints = 5;

        /// Unit health at the old toughness, in percent of each class's health.
        public const int NormalHealthPercent = 100;

        private readonly UnitSpec[] _side0;
        private readonly UnitSpec[] _side1;
        private readonly TacticsProfile _tactics0;
        private readonly TacticsProfile _tactics1;
        private readonly SideRules _rules0;
        private readonly SideRules _rules1;

        public BattleSetup(
            string fightId,
            ulong seed,
            IReadOnlyList<UnitSpec> side0,
            IReadOnlyList<UnitSpec> side1,
            TacticsProfile side0Tactics,
            TacticsProfile side1Tactics,
            int tickLimit = DefaultTickLimit,
            int startingDirectivePoints = DefaultDirectivePoints)
            : this(fightId, seed, side0, side1, side0Tactics, side1Tactics, tickLimit, startingDirectivePoints, NerveRules.Off)
        {
        }

        /// The same, with nerve rules. Pass NerveRules.Off for the old rules. With refillTicks above
        /// 0, each side gets one directive point back every that many ticks, up to its starting
        /// points; 0 never refills, as before. healthPercent scales every unit's health; 100 is the
        /// old toughness. Refilling orders and a slower pace are the slice's newer rules (Jacob,
        /// 2 and 3 Oct 2026). Side rules give a side its big moves (Eclipse, combos); null is none.
        /// Fight rules are the 13 Oct 2026 rules (FightRules); null is none.
        public BattleSetup(
            string fightId,
            ulong seed,
            IReadOnlyList<UnitSpec> side0,
            IReadOnlyList<UnitSpec> side1,
            TacticsProfile side0Tactics,
            TacticsProfile side1Tactics,
            int tickLimit,
            int startingDirectivePoints,
            NerveRules nerve,
            int refillTicks = 0,
            int healthPercent = NormalHealthPercent,
            SideRules side0Rules = null,
            SideRules side1Rules = null,
            FightRules fightRules = null,
            IReadOnlyList<BondPair> bonds = null)
        {
            if (side0 == null || side0.Count == 0) throw new ArgumentException("Side 0 needs at least one unit.");
            if (side1 == null || side1.Count == 0) throw new ArgumentException("Side 1 needs at least one unit.");
            if (tickLimit <= 0) throw new ArgumentOutOfRangeException(nameof(tickLimit));

            FightId = fightId ?? string.Empty;
            Seed = seed;
            TickLimit = tickLimit;
            StartingDirectivePoints = Math.Max(0, startingDirectivePoints);
            Nerve = nerve ?? NerveRules.Off;
            RefillTicks = Math.Max(0, refillTicks);
            HealthPercent = Math.Max(1, healthPercent);
            _side0 = Copy(side0);
            _side1 = Copy(side1);
            _tactics0 = side0Tactics == null ? new TacticsProfile() : side0Tactics.Clone();
            _tactics1 = side1Tactics == null ? new TacticsProfile() : side1Tactics.Clone();
            _rules0 = side0Rules ?? SideRules.None;
            _rules1 = side1Rules ?? SideRules.None;
            Fight = fightRules ?? FightRules.Off;
            var pairs = new List<BondPair>();
            if (bonds != null) for (int i = 0; i < bonds.Count; i++) if (bonds[i] != null) pairs.Add(bonds[i]);
            Bonds = pairs;
        }

        /// Your side's bonded pairs coming into this fight, in order (road combat, BattleSimulation.Road.cs). Empty unless set.
        public IReadOnlyList<BondPair> Bonds { get; }

        public string FightId { get; }
        public ulong Seed { get; }
        public int TickLimit { get; }
        public int StartingDirectivePoints { get; }

        /// The nerve rules for this fight. NerveRules.Off unless set.
        public NerveRules Nerve { get; }

        /// Ticks between refilled directive points, or 0 when orders never refill.
        public int RefillTicks { get; }

        /// Every unit's health in percent of its class's health. NormalHealthPercent unless set.
        public int HealthPercent { get; }

        /// The 13 Oct 2026 fight rules (misses, earned points, Perfect Hold, nerve snaps). FightRules.Off unless set.
        public FightRules Fight { get; }

        public IReadOnlyList<UnitSpec> Side(int side)
        {
            return side == 0 ? (IReadOnlyList<UnitSpec>)_side0 : _side1;
        }

        /// A side's big moves. SideRules.None unless set.
        public SideRules Rules(int side)
        {
            return side == 0 ? _rules0 : _rules1;
        }

        public int TargetWeight(int side, UnitClass unitClass)
        {
            return Tactics(side).TargetWeight(unitClass);
        }

        public int AvoidGuarded(int side)
        {
            return Tactics(side).AvoidGuarded;
        }

        public int TrapCaution(int side)
        {
            return Tactics(side).TrapCaution;
        }

        /// A copy of a side's tactics for this fight.
        public TacticsProfile CopyTactics(int side)
        {
            return Tactics(side).Clone();
        }

        private TacticsProfile Tactics(int side)
        {
            return side == 0 ? _tactics0 : _tactics1;
        }

        private static UnitSpec[] Copy(IReadOnlyList<UnitSpec> specs)
        {
            var copy = new UnitSpec[specs.Count];
            for (int i = 0; i < specs.Count; i++) copy[i] = specs[i];
            return copy;
        }
    }

    /// A fighter during a battle. Only BattleSimulation changes it.
    /// Two of your fighters bonded for the run (road combat, Jacob 25 Oct 2026: three combos by one pair in one fight, on any
    /// enemies): the one whose mark starts it and the one who cashes it.
    public sealed class BondPair
    {
        public BondPair(string originId, string casherId)
        {
            OriginId = originId ?? string.Empty;
            CasherId = casherId ?? string.Empty;
        }

        public string OriginId { get; }
        public string CasherId { get; }
    }

    public sealed class BattleUnit
    {
        internal BattleUnit(string id, int side, int index, UnitClass unitClass, Row row,
            int healthPercent = BattleSetup.NormalHealthPercent)
            : this(new UnitSpec(id, unitClass, row), side, index, healthPercent)
        {
        }

        /// healthPercent is the fight's toughness; the spec's own health percent goes on top.
        internal BattleUnit(UnitSpec spec, int side, int index, int healthPercent)
        {
            Id = spec.Id;
            Side = side;
            Index = index;
            Class = spec.Class;
            Row = spec.Row;
            Role = spec.Role;
            PowerPercent = spec.PowerPercent;
            Stats = SliceClasses.Stats(spec.Class);
            MaxHp = Math.Max(1, Stats.MaxHp * healthPercent / 100 * spec.HealthPercent / 100);
            Hp = MaxHp;
            Alive = true;
            GuardTargetId = string.Empty;
            AimTargetId = string.Empty;
        }

        public string Id { get; }
        public int Side { get; }

        /// Position in the battle's fixed unit order. Ties always break on the lower index.
        public int Index { get; }

        public UnitClass Class { get; }
        public Row Row { get; }
        public UnitRole Role { get; }

        /// Power in percent of the class's power. UnitSpec.PlainPercent unless raised.
        public int PowerPercent { get; }

        public ClassStats Stats { get; }

        /// Set once when the fight starts: Borrowed time, a Time Stop combo's pact, takes an eighth off (BattleSimulation.Combos.cs).
        public int MaxHp { get; internal set; }
        public int Hp { get; internal set; }
        public bool Alive { get; internal set; }
        public int Cooldown { get; internal set; }

        /// Warden only: the ally it is currently covering. Empty when none.
        public string GuardTargetId { get; internal set; }

        /// Warden only, from rules 24 (BattleSimulation.Whisperer.cs): the second ally he covers at rank 2, or the Medic under
        /// Mend and Hold. Empty when none.
        public string GuardTargetId2 { get; internal set; } = string.Empty;

        /// From rules 24: his one free answer this cycle of his own spell is spent (a pair's, or an upgraded Avenger's), and,
        /// for a rank 2 Medic, a treatment kept in hand.
        public bool FreeSpent { get; internal set; }
        public bool TreatmentInHand { get; internal set; }

        /// From rules 29 (BattleSimulation.Glutton.cs): when, and by which side, he was last parried (Answer the Parry), and
        /// Rewind Wound's longer memory of what he lost each second.
        public long ParriedAtTick { get; internal set; } = -1;
        public int ParriedBySide { get; internal set; } = -1;
        internal int[] HurtLongLost { get; } = new int[ClassSpells.RewindSeconds + 2 * GluttonRules.RewindMoreSeconds];
        internal long[] HurtLongSecond { get; } = LongSeconds();

        private static long[] LongSeconds()
        {
            var seconds = new long[ClassSpells.RewindSeconds + 2 * GluttonRules.RewindMoreSeconds];
            for (int i = 0; i < seconds.Length; i++) seconds[i] = -1;
            return seconds;
        }

        /// From rules 28 (BattleSimulation.Shade.cs), Echo: what his echo holds this Unleash, and the first one he recorded.
        public int EchoStored { get; internal set; }
        public string EchoTargetId { get; internal set; } = string.Empty;

        /// Every status on this fighter (Statuses.cs; DESIGN_LOCKS, "Builds: one theme per Pal", step 1, 30 Oct 2026): panic,
        /// bleed, snare, Opening, stun, burn, pin, flurry and slow, with who left each mark and its chain. The old names below
        /// read it.
        public StatusList Statuses { get; } = new StatusList();

        /// FightRules.NerveSnaps: ticks this fighter is still panicking (it does not act) or rising
        /// (its blows hit harder). 0 otherwise. The private set is only for SquadScreenTests, which freezes an ally by reflection.
        public int PanicTicks { get => Statuses[StatusKind.Panic].Ticks; private set => Statuses[StatusKind.Panic].Ticks = value; }
        public int RiseTicks { get; internal set; }

        /// BleedRules: ticks this fighter keeps bleeding, and the percent of its health it loses each second. 0 otherwise.
        public int BleedTicks => Statuses[StatusKind.Bleed].Ticks;
        public int BleedPercent => Statuses[StatusKind.Bleed].Size;

        /// FightRules.ClassSpells (ClassSpells, 20 Oct 2026): ticks this fighter is still held by a snare (it does nothing
        /// and cannot dodge) and still open, ticks until its spell is ready, and a Marksman's aim: ticks left and whom at.
        /// 0 or empty otherwise.
        public int SnareTicks => Statuses[StatusKind.Snare].Ticks;
        public int OpeningTicks => Statuses[StatusKind.Opening].Ticks;
        public int SpellTicks { get; internal set; }
        public int AimTicks { get; internal set; }
        public string AimTargetId { get; internal set; }

        /// FightRules.ChiefRules (ChiefFight, 20 Oct 2026): a chief who has yielded (he kneels and does nothing more) and
        /// one whose second phase has come; a reserve still out of the fight (it counts as not alive until it joins).
        public bool Yielded { get; internal set; }
        public bool PhaseDone { get; internal set; }
        public bool Benched { get; internal set; }

        /// FightRules.ChiefKits (ChiefKits, 21 Oct 2026): ticks until a chief's Brand bursts on this fighter, and ticks a chief
        /// stays stunned after your Sunbreaker (he does nothing). 0 otherwise. In a kit fight Yielded is the kneel.
        public int BrandTicks { get; internal set; }
        public int StunTicks => Statuses[StatusKind.Stun].Ticks;

        /// The Skullpeak chief's kit (ChiefKits, 22 Oct 2026): ticks this fighter of yours stays dragged in front of her shields
        /// by her Hook (every blow of hers lands on them, and her side goes for them), and ticks until this shooter of hers
        /// fires his Rifle Line shot (he does nothing else meanwhile). 0 otherwise.
        public int HookTicks { get; internal set; }
        public int RifleTicks { get; internal set; }

        /// The parry at the center (FightRules.ParryCenter, 24 Oct 2026): ticks this fighter still reels from a parry or
        /// stands open after a feint (nobody can cover him meanwhile), and what the beat of a move on or by this fighter has
        /// done so far (a Brand on them, a beast part's move: ParryRules beat flags). 0 otherwise.
        public int ReelTicks { get; internal set; }
        public int BeatFlags { get; internal set; }

        // Road combat (BattleSimulation.Road.cs, 25 Oct 2026). His own nerve and the part of a point still to drain; whether he
        // has snapped this fight (once only), and how: frenzied, his last stand, or fled off the field.
        public int UnitNerve { get; internal set; }
        public int UnitNerveRem { get; internal set; }
        public bool NerveSnapped { get; internal set; }
        public bool Frenzied { get; internal set; }
        public bool Fled { get; internal set; }

        // Each mark on him: who left it, and the chain it belongs to (0 for none). A chain is the id of the setup that started
        // it; a fighter reacts to a chain once.
        public string SnareOriginId => Statuses[StatusKind.Snare].SourceId;
        public int SnareChain => Statuses[StatusKind.Snare].Chain;
        public string OpeningOriginId => Statuses[StatusKind.Opening].SourceId;
        public int OpeningChain => Statuses[StatusKind.Opening].Chain;
        public string BleedOriginId => Statuses[StatusKind.Bleed].SourceId;
        public int BleedChain => Statuses[StatusKind.Bleed].Chain;

        // As a casher: the last chain he reacted to, the chain of the shot he is aiming (0 for a plain one), and when his last
        // bonded answer came (-1 for never).
        public int LastChainId { get; internal set; }
        public int AimChainId { get; internal set; }
        public long LastBondedTick { get; internal set; } = -1;

        // His defence move's cooldown, and, for a Marksman, the ticks left in which the next melee blow on him misses.
        public int DefenceTicks { get; internal set; }
        public int StepBackTicks { get; internal set; }

        // The road kit (rules 12). His boost's cooldown, and a Marksman's Steady on the shot he is aiming. The Burn stacks on him,
        // the ticks left on them and whose blows lit them. A carrier's clock for his next carried move, the move he is telling,
        // its tell's ticks and its mark; and on a branded fighter, who branded him.
        public int BoostTicks { get; internal set; }
        public bool Steady { get; internal set; }
        public int BurnStacks => Statuses[StatusKind.Burn].Stacks;
        public int BurnTicks => Statuses[StatusKind.Burn].Ticks;
        public string BurnerId => Statuses[StatusKind.Burn].SourceId;
        public long CarryNext { get; internal set; }
        public ChiefMove CarryMove { get; internal set; }
        public int CarryTell { get; internal set; }
        public string CarryTargetId { get; internal set; } = string.Empty;
        public string BrandById { get; internal set; } = string.Empty;

        // The Butcher's combos (BattleSimulation.Combos.cs, 29 Oct 2026). Ticks this fighter is still held by a black hole's
        // pin or a freeze (he does nothing and cannot dodge, but carries no Snare mark); out of reach after an Evac, and
        // whether a Medic's Evac is spent; frozen in a Frozen Flurry, the hits waiting for the thaw and whose flurry it is;
        // and ticks he still moves at half speed after a Time Snare, every big move he starts winding up slowly. As a wearer: how often his combo's move has
        // come round (Collapse, Event Horizon, Stopped Bullets) and its own clock (Still Wall's wait, Rewind's). What he lost
        // in each of the last ClassSpells.RewindSeconds seconds, and which second each slot holds, for a Rewind, and the enemy
        // whose blow last struck him, for Rewind's hook (29 Oct 2026).
        public int PinTicks => Statuses[StatusKind.Pin].Ticks;
        public int EvacTicks { get; internal set; }
        public bool EvacSpent { get; internal set; }
        public int FlurryTicks => Statuses[StatusKind.Flurry].Ticks;
        public int FlurryStore => Statuses[StatusKind.Flurry].Store;
        public string FlurryById => Statuses[StatusKind.Flurry].SourceId;
        public int SlowTicks => Statuses[StatusKind.Slow].Ticks;
        public int ComboCount { get; internal set; }
        public int ComboTicks { get; internal set; }
        internal int[] HurtLost { get; } = new int[ClassSpells.RewindSeconds];
        internal long[] HurtSecond { get; } = NoSeconds();
        public string HurtById { get; internal set; } = string.Empty;

        // A duel's champion who already sprang the trap once this fight, and steps around it now (DESIGN_LOCKS, "Duels").
        public bool SprangTrap { get; internal set; }

        // The damage layers (BattleSimulation.Layers.cs, campaign rules 17): what is left over of the hits on him and of his
        // heals, in millionths of a point, for the next one.
        public long HitLeftover { get; internal set; }
        public long HealLeftover { get; internal set; }

        private static long[] NoSeconds()
        {
            var seconds = new long[ClassSpells.RewindSeconds];
            for (int i = 0; i < seconds.Length; i++) seconds[i] = -1;
            return seconds;
        }

        /// Health in thousandths, integer only.
        public int HpPermille => MaxHp <= 0 ? 0 : Hp * 1000 / MaxHp;

        /// Damage per attack (or healing per action for the Medic), with this unit's power percent.
        public int Power => Math.Max(1, Stats.Power * PowerPercent / 100);

        /// True for a Pal or a Chief: if it drops, its side breaks.
        public bool IsLeader => Role != UnitRole.Fighter;
    }

    public enum BattleEventKind
    {
        Hit = 0,
        Heal = 1,
        Guard = 2,
        TrapArmed = 3,
        TrapSprung = 4,
        Kill = 5,
        Directive = 6,
        End = 7,

        /// Nerve rules only: a side's nerve ran out. Actor side is the side that broke, amount is
        /// how many of its fighters were still standing. Detail is "break", or "leader" when its
        /// Pal or Chief dropped.
        Break = 8,

        /// A big move started winding up. Actor is the caster, amount the windup in ticks, detail
        /// the move ("Eclipse", or "Pyre" when it finishes that combo), target the Pyre's target.
        Windup = 9,

        /// A caster was hit hard enough to lose its wound up move. Actor side is the side that
        /// interrupted (its next order is doubled), target is the caster, amount the damage taken.
        Interrupt = 10,

        /// Orders fused into a combo. Actor side is the side that made it, detail the combo.
        Combo = 11,

        /// A Pal transformed (Frenzy). Actor is the Pal, amount the ticks it lasts.
        Transform = 12,

        /// An empowered order was used: it runs twice as long or hits twice as hard. Detail is the order.
        Empowered = 13,

        /// A wound up move went off. Actor is the caster, amount the percent of max health it
        /// hits for, detail "full", "soaked" (the enemy held), "dodged" (the enemy fell back) or
        /// "parried" (FightRules.PerfectHold: the enemy held at the last moment).
        Release = 14,

        /// FightRules.Misses: a blow missed. Actor is the attacker, target the enemy it missed, amount
        /// the chance it had in percent (for the history, never shown).
        Miss = 15,

        /// FightRules.PerfectHold: a Hold parried a big hit. Actor side is the side that parried,
        /// target the caster, amount the ticks the caster staggers.
        Parry = 16,

        /// FightRules.NerveSnaps: a fighter's nerve snapped. Actor is the fighter, amount the ticks it
        /// lasts, detail "panic" or "rise".
        Snap = 17,

        /// FightRules.EarnedPoints: a side earned order points. Actor side is the side, amount the
        /// points, detail why ("parry", "dodge", "interrupt", "kill", "panic", "combo").
        Earn = 18,

        /// A tribe's doctrine acted. Actor side is the side, target the unit it was about when there is
        /// one, detail the doctrine ("rally", "guardcaster", "holdformation", "kite").
        Reaction = 19,

        /// A Steel Storm wall (ChiefSpells, 18 Oct 2026). Actor side is the side whose wall it is. Detail "up" (actor
        /// is the caster, amount the ticks it stands), "smashed" (an Unleash broke it; amount is the nerve that side
        /// lost) or "fell" (it ran out). The Skullpeak chief's kit (22 Oct 2026) adds "half" (it went up half built: your
        /// Trapper snared one of her Wardens while they locked; amount the ticks it stands), "lockdown" (her Lockdown wall
        /// went up for good; actor the chief), "rebuilt" (her Wardens have it whole again after a smash) and, in Lockdown,
        /// "fell" when her last Warden falls and it is down for good.
        Wall = 20,

        /// Class spells (ClassSpells, 20 Oct 2026): a Marksman started an Aimed Shot. Actor is the Marksman, target the
        /// enemy he aims at (the aim line), amount the ticks it takes, detail "aim", or "pin" at a snared enemy.
        Aim = 21,

        /// Class spells: a mark landed. Actor is who left it, target who carries it, amount the ticks it lasts, detail
        /// "snare", "bleed" or "opening".
        Mark = 22,

        /// Class spells: a spell cashed in a mark. Actor is the caster, target the marked fighter, amount what it added
        /// (damage, or healing), detail "pin" (a snare), "opening", "rip" (a bleed) or "covered" (a guard).
        Cash = 23,

        /// Class spells: a spell was broken before it went off. Actor is whoever broke it, target the caster, detail
        /// "aim".
        Broken = 24,

        /// Chief rules (ChiefFight, 20 Oct 2026): a chief yielded. Actor is the chief; from now on Hold spares him. With the
        /// chief kits (21 Oct 2026) this is the kneel, detail "kneel": his nerve ran out, everything stops, and the other side
        /// chooses with Hold (Spare) or Focus Fire on him (Execute).
        Yield = 25,

        /// Chief rules: a chief's second phase. Actor is the chief, amount how many of his reserves join, detail his kit's
        /// name for it ("Blood Sun") or empty.
        Phase = 26,

        /// Chief rules: one of his reserves joined the fight. Actor is the chief, target the one who joined.
        Join = 27,

        /// Chief kits (ChiefKits, 21 Oct 2026): a chief's move started its tell. Actor is the chief, target its mark (who a
        /// Cleave falls on, or a branded fighter, one event each), amount the ticks until it lands, detail the move ("Cleave",
        /// "Brand"). From 22 Oct also "Burning Ring" (target the one whose lane it goes down, amount the spin), "Sun Mantle"
        /// (no target, amount the ticks it burns), "Hook" (target the one the chain glints at), "Rifle Line" (target its mark,
        /// amount the aim; each shooter also gets an Aim event, detail "rifle"), "Iron Advance" (no target, amount the march)
        /// and "Overclock" (actor her Medic, who jabs; no target, amount the jab). The hunt (ChiefKit.Hunt): "Track", "Drive",
        /// "Corner" (its purple line) and "Kill", each as its step starts, actor the hunt captain, target the prey, amount the
        /// step's ticks. The Stoneveil chief (22 Oct 2026, later; his Blood Call is a Windup as before): "Basin" (actor his
        /// acolyte, who carries it; amount the carry), "Leeches" (target the one they land on, amount how long they drink),
        /// "Open Veins" (no target) and "Transfusion" (target the one he draws from, amount the channel). The Thundercoast chief
        /// (his Tide is a Windup as before): "Sluice Gate" (actor his engineer; amount the crank), "Riptide" (no target),
        /// "Tidal Bore" (no target, amount its run; BattleSimulation.BoreFromRight says which way) and "Storm Surge" (no target,
        /// amount the horn).
        Tell = 28,

        /// Chief kits: a chief's move landed, after the hits it made on the same tick. Actor the chief (her Medic for
        /// Overclock), target who it fell on (empty for a move that marks nobody), amount the damage it dealt before armour
        /// (0 when parried or dodged), detail the move, a dot and how it went: "Cleave.parried", "Cleave.guarded",
        /// "Cleave.soaked", "Cleave.full", "Brand.dodged", "Brand.guarded", "Brand.healed" or "Brand.full". From 22 Oct:
        /// "Burning Ring.broken" (knocked out of his hand), ".dodged" or ".full"; "Sun Mantle.full" (it burned someone; amount
        /// all it burned), ".dodged", ".switched" or ".clear" (nobody struck him and you did neither), when it ends; "Hook.parried",
        /// ".guarded" (anchored) or ".full" (dragged); "Rifle Line.dodged", ".broken" (shots lost; amount what still landed),
        /// ".healed" or ".full"; "Iron Advance.braced", ".dodged" (you gave ground; amount the nerve she got back) or ".full",
        /// one each second of the march, and ".staggered" when an Unleash stops it; "Overclock.broken" (focused, snared or her
        /// Medic fell during the jab) or ".full" (her band speeds up; amount the ticks). The hunt, target the prey: a broken
        /// step, "Track.guarded" or ".healed", "Drive.braced" or ".broken", "Corner.broken", ".healed" (your Medic cut the net)
        /// or ".torn" (your Unleash did); "Corner.full" when the net lands (a Mark "net" first); "Kill.parried", ".guarded" or
        /// ".full" (amount the hit). The Stoneveil chief: "Blood Call.dodged", ".healed" (your Medic stitched a wound during
        /// the chant), ".full" (it ripped someone; amount all it ripped) or ".clear" (nobody bled), after its Release;
        /// "Basin.full" (he drank it; amount what it healed him), ".broken" (Focus Fire spilled it, or the acolyte fell),
        /// ".smashed" (your Unleash kicked it over) or ".empty" (it never filled); "Leeches.healed" (your Medic plucked them
        /// off), ".dodged" (Fall Back shook them loose) or ".full" (amount all they drank); "Open Veins.healed", ".braced",
        /// ".full" or ".clear", when it ends; "Transfusion.broken", ".guarded" (a Guard event names your Warden) or ".full"
        /// (amount all he drew); "Blood Price.full" (no target, amount the orders it took) when your orders bring his next move
        /// at once. The
        /// Thundercoast chief: "Sluice Gate.broken" (jammed: Focus Fire on the engineer, or he fell), ".smashed" (your Unleash
        /// slammed it shut) or ".full" (amount the water's level after); "Riptide.full" (it pulled your side; amount the hits),
        /// ".braced" or ".clear", when it ends; "Tidal Bore.guarded" (target the Warden it broke on, nobody hit in full before
        /// him), ".full" (target that Warden when one broke it late), ".soaked", ".dodged" or ".clear", when it ends or breaks;
        /// "Storm Surge.broken" during the horn, else ".braced" or ".full" when the high water ends.
        Land = 29,

        /// Chief kits: a side's Glory moved. Actor side the side, target the chief, amount its Glory after (0 to
        /// ChiefKits.GloryMax; ChiefKits.Rank gives the letter), detail why: an answer ("parry", "dodge", "break", "guard",
        /// "heal", "smash", "snare", "brace", "switch", "starve") or "eaten" (it ate a big move in full and dropped a rank).
        Glory = 30,

        /// Chief kits: the chief speaks. Actor the chief, detail the line's key (ChiefKits.Line gives the words).
        Line = 31,

        /// Chief kits: Hold spared the kneeling chief. Actor side the side that spared him (actor id "time" when the fight's
        /// clock ran out first), target the chief. His side breaks next, and he lives.
        Spare = 32,

        /// Chief kits: Focus Fire executed the kneeling chief. Actor side the side that did it, target the chief, amount the
        /// health he had. He falls next.
        Execute = 33,

        /// Beasts (BeastKits, 21 Oct 2026): a beast's part broke, right after its Kill. Actor is whoever broke it (empty when a
        /// bleed or an order did), target the part, detail the part's name ("jaw", "fin", "spine", "tail"; the cage bear's
        /// "claw", "maw", "ribcage", "heart"). Its move is gone for the rest of the fight, it drops its trophy if the fight is
        /// won, and the breaker earns its road scar. The cage bear's other parts go down with its heart with a Kill (detail
        /// "heart") and no Sever: they are not broken.
        Sever = 34,

        /// Beasts (22 Oct 2026): the sand shark dived away, right before the fight's End. Actor a part still standing, detail
        /// "escape" (its Last stand ran out) or "time" (the fight's clock did). The side facing it wins and keeps what it broke.
        /// The cage bear never flees.
        Flee = 35,

        /// Chief tactics (FightRules.ChiefTactics, 22 Oct 2026): you can see one of the Bloodridge chief's moves coming, 6 s
        /// before it is due. Actor the chief, target its mark (empty for the Eclipse and the Sun Mantle; a Brand under Blood
        /// Sun gives one event per mark), amount how hard (ChiefForecast.Amount), detail the move. BattleSimulation.Forecasts
        /// says how long each still has.
        Forecast = 36,

        /// The Thundercoast chief's tide (ChiefKits, 22 Oct 2026, later): the water's level changed. Actor side his side,
        /// amount the level now (0 to FloodRules.MaxLevel; at 0 his side is stranded), detail why (ChiefKits.TideClock "clock",
        /// TideGate "gate", TideSurge "surge", TideDrain "drain", TideSpring "spring").
        Tide = 37,

        /// Road combat (BattleSimulation.Road.cs, 25 Oct 2026): two of a side's fighters bonded. Actor the casher, target the
        /// fighter whose marks he cashes, amount the combos they landed together this fight.
        Bond = 38,

        /// Road combat: a fighter's defence move fired. Actor the defender, target whoever it answered (empty for Self Patch),
        /// amount what it did (damage taken off, health healed, 0 for a dodge or a snare), detail the move (RoadRules.Brace,
        /// Sidestep, StepBack, SnapTrap, SelfPatch).
        Defence = 39,

        /// Road combat: a fighter reacted to a mark, out of turn. Actor the casher, target the enemy, amount the chain's id,
        /// detail the mark (ClassSpells.SnareDetail, OpeningDetail, BleedDetail), with RoadRules.Bonded added after a "." for a
        /// bonded answer.
        Chain = 40,

        /// The road kit (BattleSimulation.Boosts.cs, rules 12): a fighter's automatic boost fired. Actor the fighter, target whom
        /// it went for (empty for Rearm), amount what it did (the bash's damage, the Stim's ticks saved, else 0), detail the move
        /// (BoostRules.ShieldBash, Steady, TwinCut, Rearm, Stim).
        Boost = 41,
        // Paired confirmed teamwork events: same chain (Amount), target and mark (Detail).
        // Setup actor is the origin; Finish actor is the casher. Rules 14 only.
        ComboSetup = 42,
        ComboFinish = 43,

        /// The Butcher's combos (29 Oct 2026, campaign rules 15; BattleSimulation.Combos.cs), one event kind per augment, detail
        /// the combo's name (ClassSpells.Name). Pull, a Gravity Well hole or pull: actor the wearer, target its mark (Mercy Well:
        /// her patient), amount how many it dragged in. Black Hole Round's are each crushed right after, a Hit with detail
        /// ClassSpells.BlackHoleDetail; Event Horizon's mark takes his blow and the rest a Hit with EventHorizonDetail;
        /// Collapse pins its mark (amount 0); Singularity Net holds the ones beside whoever sprang her trap; Mercy Well heals
        /// the allies beside her patient (Heal, MercyDetail).
        Pull = 44,

        /// Portal, a Fold Door portal: actor the wearer, target whom it is about. Around the Corner: each of his aims, whose shot
        /// comes through it from behind (amount the aim's ticks; past Focus Fire and snares the mark is their healer). Execution Corridor: her hit from behind follows (Hit, CorridorDetail). Wrong Door: target
        /// the ally whose blow comes onto his shield (his Guard and Hit follow). Doorway: target the shooter who fell into her
        /// trap. Evac: target the fighter pulled out alive, amount the ticks out of reach.
        Portal = 45,

        /// Freeze, a Time Stop freeze or slow: actor the wearer, target whom it froze. Stopped Bullets: a shot hangs frozen in
        /// his mark, which freezes and opens (amount how many hang now; the next shot fires them all). Frozen Flurry: her mark
        /// freezes (amount the ticks; the hits wait, then land as one Hit with FlurryDetail). Still Wall: whoever struck his
        /// shield freezes (amount the ticks). Time Snare: whoever sprang her trap is slowed (amount the ticks it moves at half
        /// speed, the hold included).
        /// Rewind: the ally she rewinds (amount what it gives back, a Heal with RewindDetail after).
        Freeze = 46,

        /// Burst, one of the Butcher's marks reaching its cap (rules 18, BattleSimulation.Marks.cs): actor whoever laid the last
        /// stack, target the marked enemy, detail the mark (MarkRules.Crush, Exposed or Stasis). Crush: amount the hit's
        /// points before the victim's layers (its Hit follows, CrushedDetail) and he is pinned. Exposed: amount the ticks he
        /// stays wide open for your side's next blow. Stasis: amount the ticks he is frozen.
        Burst = 47,

        /// Shield, a shield soaking a hit (rules 23, BattleSimulation.Shade.cs): actor and target the shielded fighter, amount
        /// what it soaked, detail ShadeRules.SanctuaryDetail.
        Shield = 48,

        /// Trick, one of the Shade's tricks going off (rules 23): side whose it is, detail the trick (ShadeRules.SanctuaryDetail,
        /// BackstabDetail or FearDetail). Sanctuary and Backstab: amount how many fighters it reached. Fear: target the fallen
        /// enemy, amount how many beside him lost nerve.
        Trick = 49,

        /// Whisper, one of the Whisperer's pieces going off (rules 24, BattleSimulation.Whisperer.cs): actor the fighter, target
        /// whom it was on, detail which (WhisperRules.FreeDetail for a pair's free answer or an upgraded Avenger's free strike,
        /// InHandDetail for a Medic keeping a treatment in hand, AmbushDetail or StoppedDetail for an Ambusher's trap on a heavy
        /// swing, CounterDetail for an upgraded Counter Shot fired with no aim).
        Whisper = 50
    }

    /// Why a fighter may not strike an enemy right now (BattleSimulation.CanStrike, 11 Oct 2026).
    /// None means it may: it still picks its own target unless a Focus Fire names this one.
    public enum StrikeBlock
    {
        None = 0,
        AttackerDown = 1,
        TargetDown = 2,
        SameSide = 3,

        /// Either side's Fall Back keeps the lines apart: no melee lands.
        FallBack = 4,

        /// A learned habit: this melee fighter waits for its own Warden to spring the enemy's trap.
        WaitsOutTrap = 5,

        /// Their front row still stands, and only ranged fighters and Skirmishers reach the back row.
        FrontStands = 6,

        /// The cage bear's heart, while its ribcage stands (BeastKits, 22 Oct 2026): no blow reaches it.
        Caged = 7,
        SquadProtectsLeader = 8
    }

    /// How a fight ended (BattleSimulation.Ending, 11 Oct 2026).
    public enum FightEnd
    {
        None = 0,
        NerveBreak = 1,
        LeaderFell = 2,
        Wipe = 3,
        TimeUp = 4,

        /// A beast dug away in its Last stand before its last part broke (BeastKits, 22 Oct 2026). Side is the beast's.
        Escaped = 5
    }

    /// How a fight ended: what happened, to which side (the side that broke or was wiped out; -1 when
    /// time ran out or both sides went down), how many of a broken side still stood, and on which tick.
    /// How is FightEnd.None, Side -1 and Tick -1 while the fight runs.
    public readonly struct FightEnding
    {
        public readonly FightEnd How;
        public readonly int Side;
        public readonly int Standing;
        public readonly long Tick;

        public FightEnding(FightEnd how, int side, int standing, long tick)
        {
            How = how;
            Side = side;
            Standing = standing;
            Tick = tick;
        }
    }

    /// Player orders during an auto battle. Costs come from Jacob's UI mockup; Eclipse and
    /// Frenzy came with the slice's big moves (Jacob, 4 Oct 2026).
    public enum Directive
    {
        Hold = 0,
        FallBack = 1,
        FocusFire = 2,
        Unleash = 3,
        Sacrifice = 4,
        Eclipse = 5,
        Frenzy = 6
    }

    public static class Directives
    {
        public const string CommandKind = "directive";

        public static int Cost(Directive directive)
        {
            switch (directive)
            {
                case Directive.Hold: return 1;
                case Directive.FallBack: return 1;
                case Directive.FocusFire: return 1;
                case Directive.Unleash: return 2;
                case Directive.Sacrifice: return 3;
                case Directive.Eclipse: return 3;
                case Directive.Frenzy: return 0;
                default: return 99;
            }
        }

        public static string Name(Directive directive)
        {
            switch (directive)
            {
                case Directive.Hold: return "Hold";
                case Directive.FallBack: return "FallBack";
                case Directive.FocusFire: return "FocusFire";
                case Directive.Unleash: return "Unleash";
                case Directive.Sacrifice: return "Sacrifice";
                case Directive.Eclipse: return "Eclipse";
                case Directive.Frenzy: return "Frenzy";
                default: return "Unknown";
            }
        }

        public static bool TryParse(string text, out Directive directive)
        {
            for (int i = 0; i <= (int)Directive.Frenzy; i++)
            {
                var candidate = (Directive)i;
                if (string.Equals(Name(candidate), text, StringComparison.Ordinal))
                {
                    directive = candidate;
                    return true;
                }
            }
            directive = Directive.Hold;
            return false;
        }
    }

    /// Something that happened in a fight. The full list is the fight's history, and lessons
    /// cite it, so it must be complete and in the order things happened.
    public readonly struct BattleEvent
    {
        public BattleEvent(
            long tick,
            BattleEventKind kind,
            int actorSide,
            string actorId,
            int actorClass,
            string targetId,
            int targetClass,
            int amount,
            string detail)
        {
            Tick = tick;
            Kind = kind;
            ActorSide = actorSide;
            ActorId = actorId ?? string.Empty;
            ActorClass = actorClass;
            TargetId = targetId ?? string.Empty;
            TargetClass = targetClass;
            Amount = amount;
            Detail = detail ?? string.Empty;
        }

        public long Tick { get; }
        public BattleEventKind Kind { get; }

        /// Side of the unit or player that caused the event.
        public int ActorSide { get; }

        public string ActorId { get; }

        /// Class of the actor as an int, or -1 when the actor is a player order.
        public int ActorClass { get; }

        public string TargetId { get; }

        /// Class of the target as an int, or -1 when there is no unit target.
        public int TargetClass { get; }

        public int Amount { get; }
        public string Detail { get; }
    }
}
