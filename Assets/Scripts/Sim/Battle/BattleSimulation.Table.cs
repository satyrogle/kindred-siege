using System;

namespace KindredSiege.Sim.Battle
{
    /// The attack table, campaign rules 42 (Jacob, 7 Nov 2026: "they should be like pokemon stats bro", "nothing bro" to the
    /// six stats and to the five spreads, then "Go, build it"). Every class has six stats on one ladder of five steps. A blow
    /// rolls once on a table, World of Warcraft style, and each slice is a different result the screen can show: a dodge,
    /// a parry, a block, a crit or a plain hit. Every number here is a starting value: build first, tune after he plays.
    public static class AttackTable
    {
        /// The five steps of the ladder, as Jacob agreed them: very low, low, mid, high, very high.
        public const int VeryLow = 20;
        public const int Low = 40;
        public const int Mid = 60;
        public const int High = 80;
        public const int VeryHigh = 100;

        /// Dodge: the target's Speed against the attacker's. Even speeds dodge DodgeBase percent, and every DodgeSpeedStep
        /// points of Speed the target has over the attacker one more.
        public const int DodgeBase = 5;
        public const int DodgeSpeedStep = 4;
        public const int DodgeMin = 2;
        public const int DodgeMax = 30;

        /// Parry: only a melee fighter parries, and only a melee blow. The target's Attack against the attacker's.
        public const int ParryBase = 5;
        public const int ParryAttackStep = 5;
        public const int ParryMin = 3;
        public const int ParryMax = 20;

        /// Block: only a fighter with a shield, the Warden. One Defence point in BlockDefenceStep blocks a percent, and a
        /// blocked blow lands for BlockLeavesPercent of itself.
        public const int BlockDefenceStep = 4;
        public const int BlockLeavesPercent = 50;

        /// Crit: CritBase percent, and one more for every CritSpeedStep points of the attacker's Speed. A crit lands for
        /// CritPercent of itself.
        public const int CritBase = 2;
        public const int CritSpeedStep = 20;
        public const int CritPercent = 150;

        /// Defence: every blow that lands loses a percent for each DefenceStep points of the target's Defence.
        public const int DefenceStep = 10;

        /// The details: a dodge and a parry are Miss events, a block and a crit are Struck events.
        public const string DodgeDetail = "dodge";
        public const string ParryDetail = "parry";
        public const string BlockDetail = "block";
        public const string CritDetail = "crit";

        /// A class's six stats (Jacob, 7 Nov 2026). Health and Attack say where MaxHp and Power already put a class; the
        /// table reads Attack for a parry. Spirit and Will are for spells, traps and heals, and nothing reads them yet.
        public static ClassSix Of(UnitClass unitClass)
        {
            switch (unitClass)
            {
                case UnitClass.Warden: return new ClassSix(VeryHigh, Low, VeryHigh, VeryLow, Low, High);
                case UnitClass.Skirmisher: return new ClassSix(Mid, High, VeryLow, VeryHigh, Low, Low);
                case UnitClass.Trapper: return new ClassSix(Mid, Mid, Low, High, High, Mid);
                case UnitClass.Marksman: return new ClassSix(Low, VeryHigh, VeryLow, Mid, Low, Mid);
                case UnitClass.Medic: return new ClassSix(Low, VeryLow, Low, Mid, VeryHigh, VeryHigh);
                default: return new ClassSix(Mid, Mid, Mid, Mid, Mid, Mid);
            }
        }

        /// The dodge from Speed alone, before orders, tribes and chiefs.
        public static int Dodge(UnitClass attacker, UnitClass target)
        {
            int chance = DodgeBase + (Of(target).Speed - Of(attacker).Speed) / DodgeSpeedStep;
            return Math.Max(DodgeMin, Math.Min(DodgeMax, chance));
        }

        /// The parry from Attack alone; 0 unless both fight in melee.
        public static int Parry(UnitClass attacker, bool attackerRanged, UnitClass target, bool targetRanged)
        {
            if (attackerRanged || targetRanged) return 0;
            int chance = ParryBase + (Of(target).Attack - Of(attacker).Attack) / ParryAttackStep;
            return Math.Max(ParryMin, Math.Min(ParryMax, chance));
        }

        /// The block; 0 for a fighter with no shield.
        public static int Block(UnitClass target) => target == UnitClass.Warden ? Of(target).Defence / BlockDefenceStep : 0;

        public static int Crit(UnitClass attacker) => CritBase + Of(attacker).Speed / CritSpeedStep;

        /// The percent a landed blow loses to the target's Defence.
        public static int DefenceCut(UnitClass target) => Of(target).Defence / DefenceStep;
    }

    /// Six stats on the ladder of AttackTable.
    public readonly struct ClassSix
    {
        public ClassSix(int health, int attack, int defence, int speed, int spirit, int will)
        {
            Health = health;
            Attack = attack;
            Defence = defence;
            Speed = speed;
            Spirit = spirit;
            Will = will;
        }

        public int Health { get; }
        public int Attack { get; }
        public int Defence { get; }
        public int Speed { get; }
        public int Spirit { get; }
        public int Will { get; }
    }

    public sealed partial class BattleSimulation
    {
        /// The chance, in percent, that the target dodges this fighter's next blow; 0 without the table. The orders, tribes,
        /// chiefs and bands that moved a miss before move a dodge now. Read only, for tests and bots.
        public int DodgeChance(BattleUnit attacker, BattleUnit target)
        {
            if (!_fight.Table || attacker == null || target == null) return 0;
            int dodge = AttackTable.Dodge(attacker.Class, target.Class) - SpecOf(attacker).HitBonus + SpecOf(target).AvoidBonus;
            if (IsFocusing(attacker.Side, target)) dodge -= FightRules.FocusHitBonus + _rules[attacker.Side].Favours.FocusHit;
            if (_sides[target.Side].HoldTicks > 0 && target.Row == Row.Front)
                dodge += FightRules.HoldAvoidBonus + _rules[target.Side].Favours.HoldAvoid;
            if (_sides[target.Side].Windup > 0 && _rules[target.Side].Spell == ChiefSpell.DustShark) dodge += ChiefSpells.DustAvoid;
            if (_beastOn && target.Side == _beastSide && BeastUnder) dodge += BeastKits.DiveAvoid;
            if (HasBand(target.Side, BandTrait.Evasive))
            {
                if (_sides[attacker.Side].UnleashTicks > 0) return 0;
                dodge += BandRules.EvasiveAvoid;
            }
            return Math.Max(0, Math.Min(100 - FightRules.MinHitPercent, dodge));
        }

        /// The chance, in percent, that the target parries this fighter's next blow; 0 without the table. Read only.
        public int ParryChance(BattleUnit attacker, BattleUnit target)
        {
            if (!_fight.Table || attacker == null || target == null) return 0;
            return AttackTable.Parry(attacker.Class, attacker.Stats.Ranged, target.Class, target.Stats.Ranged);
        }

        /// Rolls a blow on the table: one number from the fight's stream, as a miss rolled before, so replays stay exact.
        /// True, with a Miss event whose detail is "dodge" or "parry", when it never lands. Otherwise a block cuts it or a crit
        /// raises it, each with a Struck event, and the blow goes on.
        private bool Tabled(BattleUnit attacker, BattleUnit target, Hit hit, ref int damage)
        {
            int dodge = DodgeChance(attacker, target);
            int parry = Math.Min(100 - dodge, ParryChance(attacker, target));
            int block = Math.Min(100 - dodge - parry, AttackTable.Block(target.Class));
            int crit = Math.Min(100 - dodge - parry - block, AttackTable.Crit(attacker.Class));
            int lands = 100 - dodge - parry;
            int roll = _rng.Next(100);
            if (roll < dodge + parry)
            {
                Emit(new BattleEvent(_tick, BattleEventKind.Miss, attacker.Side, attacker.Id, (int)attacker.Class,
                    target.Id, (int)target.Class, lands, roll < dodge ? AttackTable.DodgeDetail : AttackTable.ParryDetail));
                return true;
            }
            roll -= dodge + parry;
            if (roll < block)
            {
                int before = hit != null ? hit.Points : damage;
                damage = More(damage, hit, HitLayer.Guard, AttackTable.BlockLeavesPercent);
                Emit(EventFor(BattleEventKind.Struck, attacker, target, before - (hit != null ? hit.Points : damage), AttackTable.BlockDetail));
            }
            else if (roll < block + crit)
            {
                int before = hit != null ? hit.Points : damage;
                damage = More(damage, hit, HitLayer.Cash, AttackTable.CritPercent);
                Emit(EventFor(BattleEventKind.Struck, attacker, target, (hit != null ? hit.Points : damage) - before, AttackTable.CritDetail));
            }
            return false;
        }

        /// What the victim's Defence leaves of a blow that lands on it; the blow as it was without the table.
        private int Defended(BattleUnit victim, Hit hit, int damage)
        {
            int cut = _fight.Table ? AttackTable.DefenceCut(victim.Class) : 0;
            if (cut <= 0) return damage;
            int left = More(damage, hit, HitLayer.Armor, 100 - cut);
            return hit == null ? Math.Max(1, left) : left;
        }
    }
}
