using System;

namespace KindredSiege.Sim.Battle
{
    /// Road combat's defence moves (Jacob, 25 Oct 2026: every unit gets a kit of 3, "attacks/defensekit/magic boost"; HANDOFF_LOG
    /// lines 11263 and 11358; FightRules.RoadCombat). The kit's second move, each class's own, fired only by a real trigger and
    /// spending DefenceCooldownTicks only when it fires:
    /// - Warden, Brace: a band's heavy swing reaching him, his Guard's included, lands BraceCutPercent lighter, and the swinger
    ///   is left with an Opening, which his Skirmisher reacts to (BattleSimulation.Chains.cs).
    /// - Skirmisher, Sidestep: a band's heavy swing at her misses.
    /// - Marksman, Step Back: a melee blow at him misses; he hopped back.
    /// - Trapper, Snap Trap: a melee enemy whose blow lands on her is snared, a mark his Marksman reacts to.
    /// - Medic, Self Patch: dropping below SelfPatchBelowPermille of her health, she heals SelfPatchPercent of it.
    /// Brace and Sidestep answer only a band's heavy swing (a small beat), and never while their side's Hold is up: a Hold, and a
    /// parry, come first. Every cut multiplies with the others as before. Every number is a starting value.
    public static class DefenceRules
    {
        public const int DefenceCooldownTicks = 10 * SimClock.TicksPerSecond;
        public const int BraceCutPercent = 50;
        public const int SelfPatchBelowPermille = 500;
        public const int SelfPatchPercent = 25;

        /// The Defence event's detail for each move.
        public const string Brace = "brace";
        public const string Sidestep = "sidestep";
        public const string StepBack = "stepback";
        public const string SnapTrap = "snaptrap";
        public const string SelfPatch = "selfpatch";
    }

    public sealed partial class BattleSimulation
    {
        private void TickDefences()
        {
            for (int i = 0; i < _units.Count; i++)
            {
                if (_units[i].DefenceTicks > 0) _units[i].DefenceTicks--;
                // And the road kit's boosts (BattleSimulation.Boosts.cs).
                if (_roadKitOn && _units[i].BoostTicks > 0) _units[i].BoostTicks--;
            }
        }

        private bool DefenceReady(BattleUnit u, UnitClass unitClass)
        {
            return _roadCombatOn && DefencesAvailable && u != null && !SimpleEnemy(u) && u.Alive && u.Class == unitClass && u.DefenceTicks <= 0 && u.PanicTicks <= 0 &&
                u.SnareTicks <= 0 && !u.Frenzied;
        }

        private void Defended(BattleUnit defender, BattleUnit against, int amount, string move)
        {
            defender.DefenceTicks = DefenceRules.DefenceCooldownTicks;
            Emit(EventFor(BattleEventKind.Defence, defender, against, amount, move));
        }

        // A band's heavy swing reaching this fighter, with its side's Hold down: his Brace or her Sidestep. What still lands.
        private int HeavyDefence(BattleUnit swinger, BattleUnit taker, int damage)
        {
            if (!_roadCombatOn || _sides[taker.Side].HoldTicks > 0) return damage;
            if (DefenceReady(taker, UnitClass.Warden))
            {
                int braced = damage * (100 - DefenceRules.BraceCutPercent) / 100;
                Defended(taker, swinger, damage - braced, DefenceRules.Brace);
                if (swinger.Alive) Open(taker, swinger);
                return braced;
            }
            if (DefenceReady(taker, UnitClass.Skirmisher))
            {
                Defended(taker, swinger, damage, DefenceRules.Sidestep);
                return 0;
            }
            return damage;
        }

        // A melee blow at a Marksman whose Step Back is ready misses: he hopped back.
        private bool SteppedBack(BattleUnit attacker, BattleUnit target)
        {
            if (attacker.Stats.Ranged || attacker.Side == target.Side || !DefenceReady(target, UnitClass.Marksman)) return false;
            // Gravity Mesh (rules 29): too heavy to step back while snared.
            if (Meshed(attacker, target)) return false;
            Defended(target, attacker, 0, DefenceRules.StepBack);
            return true;
        }

        // A melee blow landed on a Trapper whose Snap Trap is ready: the attacker is snared.
        private void SnapTrapped(BattleUnit attacker, BattleUnit target)
        {
            if (_finished || attacker.Stats.Ranged || !attacker.Alive || attacker.Side == target.Side || !DefenceReady(target, UnitClass.Trapper))
                return;
            Defended(target, attacker, 0, DefenceRules.SnapTrap);
            Snare(target, target.Side, attacker);
        }

        // A Medic dropped below SelfPatchBelowPermille of her health: she heals SelfPatchPercent of it.
        private void SelfPatched(BattleUnit victim)
        {
            if (!DefenceReady(victim, UnitClass.Medic) || victim.HpPermille >= DefenceRules.SelfPatchBelowPermille) return;
            int healed = Math.Min(victim.MaxHp - victim.Hp, Math.Max(1, victim.MaxHp * DefenceRules.SelfPatchPercent / 100));
            victim.Hp += healed;
            Defended(victim, null, healed, DefenceRules.SelfPatch);
        }
    }
}
