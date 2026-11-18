using System;

namespace KindredSiege.Sim.Battle
{
    /// What a band of hunters brings to a fight (Jacob, 18 Oct 2026: the hunters are the rival, built like the
    /// mercenaries of Assassin's Creed Odyssey, with 16 traits that each change which button answers them). The
    /// campaign turns a named hunter's traits and weakness into these flags on their side's rules
    /// (Learning.HunterTraits). The traits that work through what a side already has are not flags: Quickstrike is a
    /// faster HeavyEveryTicks, and Big Hit, Firebomb, War Cry and Iron Wall are the side's chief spell. A fight where
    /// neither side has a flag plays exactly as before.
    [Flags]
    public enum BandTrait
    {
        None = 0,

        /// Fall Back neither dodges their heavy swings nor keeps their blades off you. Answer: Hold, the parry.
        LongReach = 1 << 0,

        /// Their blows shake your nerve twice as hard, unless you are holding. Answer: Hold.
        Terror = 1 << 1,

        /// Their heavy swings cannot be parried; a Hold only soaks them. Answer: Fall Back. Under the parry at the center
        /// (FightRules.ParryCenter, 24 Oct 2026) this is Delay instead: every beat of theirs waits out a fresh Hold, once per
        /// beat, and lands late. Fake a Hold, then parry the late one.
        Unstoppable = 1 << 2,

        /// A heavy swing that lands on your Hold breaks it, lands in full and costs you nerve. A parry still parries.
        /// Answer: Fall Back.
        Shieldbreaker = 1 << 3,

        /// The first time their leader drops below half health, the band goes berserk for BandRules.BerserkTicks:
        /// their blows hit twice as hard and their heavy swings cannot be parried (under the parry at the center they can).
        /// Answer: Fall Back until it passes.
        Berserk = 1 << 4,

        /// Your Unleash never shakes their nerve, and a Riposte only cuts. Answer: Focus Fire.
        Unshakable = 1 << 5,

        /// Their leader patches up the most hurt of the band every BandRules.MendEveryTicks. Answer: Focus Fire on
        /// the leader.
        Mender = 1 << 6,

        /// Once a fight, while their leader stands, a break is rallied back to BandRules.SecondWindNerve. Answer:
        /// Focus Fire on the leader, whose fall still breaks them.
        SecondWind = 1 << 7,

        /// Plain blows often miss them; unleashed blows never do. Answer: Unleash.
        Evasive = 1 << 8,

        /// Plain blows on them do half; unleashed blows and a Riposte hit in full. Answer: Unleash.
        ThickHide = 1 << 9,

        /// Their nerve cannot fall below BandRules.BannerFloor while their banner flies. Your Unleash tears it down.
        /// Answer: Unleash.
        Banner = 1 << 10,

        /// Weakness: a parried heavy swing or cast cracks twice the nerve.
        WeakToParry = 1 << 11,

        /// Weakness: a dodged heavy swing staggers the swinger like a parry and costs their side nerve.
        WeakToDodge = 1 << 12,

        /// Weakness: focused blows on them hit half as hard again.
        WeakToFocus = 1 << 13,

        /// Weakness: your Unleash shakes them twice as hard, even through their Hold.
        WeakToScare = 1 << 14,

        /// Double swing, under the parry at the center (24 Oct 2026; what Quickstrike became): each heavy swing comes as two
        /// beats, a light one (ParryRules.DoubleLightPercent) and the heavy one ParryRules.DoubleGapTicks later. Parry both
        /// and the second cracks twice; Fall Back clears only the first.
        Double = 1 << 15,

        /// Feint, under the parry at the center (24 Oct 2026, new): a fake beat before each real one. The tell flashes and
        /// nothing comes, and the faker is open meanwhile: hit him then.
        Feint = 1 << 16
    }

    /// The bands' numbers. Every one is a starting value: build first, tune in the tuning weeks.
    public static class BandRules
    {
        /// Every flag at once (Double and Feint since 24 Oct 2026).
        public const int All = (1 << 17) - 1;

        public const int TerrorPercent = 200;
        public const int ShieldbreakNerve = 60;
        public const int BerserkBelowPermille = 500;
        public const int BerserkTicks = 150;
        public const int BerserkPercent = 200;
        public const int MendEveryTicks = 60;
        public const int MendPercent = 6;
        public const int SecondWindNerve = 333;
        public const int EvasiveAvoid = 25;
        public const int ThickHidePercent = 50;
        public const int BannerFloor = 250;
        public const int BannerTornNerve = 100;
        public const int BannerPoints = 1;
        public const int WeakParryPercent = 200;
        public const int WeakFocusPercent = 150;
        public const int WeakScarePercent = 200;

        /// Details of the Reaction events a band's traits make (actor side is the band's side), the Heal event of a
        /// Mender, and the reason of the Earn event for tearing a banner.
        public const string ShieldbreakDetail = "shieldbreak";
        public const string BerserkDetail = "berserk";
        public const string MendDetail = "mender";
        public const string SecondWindDetail = "secondwind";
        public const string UnshakableDetail = "unshakable";
        public const string BannerTornDetail = "bannertorn";
        public const string OffBalanceDetail = "offbalance";
        public const string BannerReason = "banner";
    }

    public sealed partial class BattleSimulation
    {
        /// True when either side has a band trait (BandTrait).
        private readonly bool _bandOn;

        /// What a side's band brings to this fight. Read only, for the screen.
        public BandTrait Band(int side)
        {
            return side >= 0 && side <= 1 ? _rules[side].Band : BandTrait.None;
        }

        /// True when a side's band has this trait.
        public bool HasBand(int side, BandTrait trait)
        {
            return _bandOn && side >= 0 && side <= 1 && (_rules[side].Band & trait) != 0;
        }

        /// True while a side's banner flies (BandTrait.Banner).
        public bool BannerUp(int side)
        {
            return _bandOn && side >= 0 && side <= 1 && _sides[side].BannerUp;
        }

        /// Ticks a side's band is still berserk, or 0 (BandTrait.Berserk).
        public int BerserkTicksLeft(int side)
        {
            return _bandOn && side >= 0 && side <= 1 ? _sides[side].BerserkTicks : 0;
        }

        /// True once a side's second wind is spent (BandTrait.SecondWind).
        public bool SecondWindUsed(int side)
        {
            return _bandOn && side >= 0 && side <= 1 && _sides[side].SecondWindUsed;
        }

        /// A side's heavy swings cannot be parried: it is Unstoppable, or berserk right now. Under the parry at the center
        /// nothing is unparryable (Unstoppable is Delay there, BattleSimulation.Parry.cs).
        private bool Unparryable(int side)
        {
            if (_parryOn) return false;
            return HasBand(side, BandTrait.Unstoppable) || (_bandOn && _sides[side].BerserkTicks > 0);
        }

        /// What the band traits do to a blow from attacker on target before cover and armour: a berserk band hits
        /// harder, and Thick Hide halves a blow that is not unleashed. Under the parry at the center Thick Hide is a brace
        /// instead: only the one you focus takes half, unleashed or not, and he cannot swing meanwhile (Braced).
        private int BandBlow(BattleUnit attacker, BattleUnit target, int damage)
        {
            if (!_bandOn) return damage;
            if (_sides[attacker.Side].BerserkTicks > 0) damage = damage * BandRules.BerserkPercent / 100;
            if (Hides(attacker, target)) damage = damage * BandRules.ThickHidePercent / 100;
            return Math.Max(1, damage);
        }

        // The same through the damage layers (BattleSimulation.Layers.cs): berserk in the kit layer, Thick Hide in armour.
        private void BandHit(BattleUnit attacker, BattleUnit target, Hit hit)
        {
            if (_sides[attacker.Side].BerserkTicks > 0) hit.Add(HitLayer.Kit, BandRules.BerserkPercent - 100);
            if (Hides(attacker, target)) hit.Add(HitLayer.Armor, BandRules.ThickHidePercent - 100);
        }

        // Thick Hide takes this blow: braced under the parry at the center, else on any blow not unleashed.
        private bool Hides(BattleUnit attacker, BattleUnit target)
        {
            return _parryOn
                ? Braced(target)
                : HasBand(target.Side, BandTrait.ThickHide) && _sides[attacker.Side].UnleashTicks == 0;
        }

        /// Thick Hide under the parry at the center (Jacob, 24 Oct 2026: "The one you focus braces: your blows do half, but
        /// braced he cannot swing"): true while the other side focuses this fighter of a Thick Hide band.
        public bool Braced(BattleUnit unit)
        {
            return _parryOn && unit != null && HasBand(unit.Side, BandTrait.ThickHide) && IsFocusing(1 - unit.Side, unit);
        }

        /// Berserk: the band's leader just dropped below half health for the first time.
        private void CheckBerserk(BattleUnit victim)
        {
            SideState state = _sides[victim.Side];
            if (!HasBand(victim.Side, BandTrait.Berserk) || state.BerserkUsed || !victim.Alive ||
                victim.HpPermille >= BandRules.BerserkBelowPermille)
                return;
            state.BerserkUsed = true;
            state.BerserkTicks = BandRules.BerserkTicks;
            Emit(EventFor(BattleEventKind.Reaction, victim, null, BandRules.BerserkTicks, BandRules.BerserkDetail));
        }

        /// Mender: every MendEveryTicks the band's living leader heals the most hurt of them.
        private void TryMend(int side)
        {
            SideState state = _sides[side];
            if (!HasBand(side, BandTrait.Mender) || _tick < state.MendNext) return;
            BattleUnit leader = Leader(side);
            if (_fight.FreezeContracts && ControlBlocked(leader)) return;
            state.MendNext = _tick + BandRules.MendEveryTicks;
            if (leader == null) return;
            BattleUnit patient = MostHurtAlly(leader, 1000, true);
            if (patient != null) Heal(leader, patient, Math.Max(1, patient.MaxHp * BandRules.MendPercent / 100), BandRules.MendDetail);
        }

        /// Second Wind: the side's nerve just ran out while its leader stands. True, with a Reaction event, when it
        /// rallies instead of breaking.
        private bool TrySecondWind(int side)
        {
            SideState state = _sides[side];
            BattleUnit leader = Leader(side);
            if (!HasBand(side, BandTrait.SecondWind) || state.SecondWindUsed || leader == null) return false;
            state.SecondWindUsed = true;
            state.Nerve = BandRules.SecondWindNerve;
            Emit(EventFor(BattleEventKind.Reaction, leader, null, BandRules.SecondWindNerve, BandRules.SecondWindDetail));
            return true;
        }

        /// A side unleashed: the enemy's banner comes down, the enemy loses nerve and the side gets a point back.
        private void TryTearBanner(int side)
        {
            int enemy = 1 - side;
            if (_finished || !_bandOn || !_sides[enemy].BannerUp) return;
            _sides[enemy].BannerUp = false;
            Emit(new BattleEvent(_tick, BattleEventKind.Reaction, enemy, string.Empty, -1, string.Empty, -1, BandRules.BannerTornNerve,
                BandRules.BannerTornDetail));
            Earn(side, BandRules.BannerPoints, BandRules.BannerReason);
            DrainNerve(enemy, BandRules.BannerTornNerve, side);
        }

        /// Weak to Fall Back: a dodged heavy swing leaves the swinger off balance.
        private void OffBalance(int side, BattleUnit swinger)
        {
            swinger.Cooldown += FightRules.StaggerTicks;
            Emit(EventFor(BattleEventKind.Reaction, swinger, null, FightRules.StaggerTicks, BandRules.OffBalanceDetail));
            DrainNerve(side, HeavyRules.ParryNerve, 1 - side);
        }

        private void CountDownBand()
        {
            for (int s = 0; s < 2; s++)
                if (_sides[s].BerserkTicks > 0) _sides[s].BerserkTicks--;
        }
    }
}
