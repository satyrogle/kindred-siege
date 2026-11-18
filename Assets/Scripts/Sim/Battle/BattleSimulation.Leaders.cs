using System;

namespace KindredSiege.Sim.Battle
{
    /// The leaders, campaign rules 43 (FightRules.Leaders and FightRules.ChampionFight). Jacob, 8 Nov 2026, on the rules 42
    /// fight: "the leader of the other tribe shouldn't be attacked, we should always go for the units. Once the units are done,
    /// the leader automatically retreats", "Warden should always be in the middle, guarding the party", "The leaders cannot be
    /// touched until he is alone in the last stage"; then "the sim side is actually fine", "in stage 4, instead of leader, the
    /// champion steps in and takes control of all the units and we have to kill the champion to go to the tribe's leader", and
    /// "our warden holds the middle and his defense decides how many enemies blows get past him. Yes".
    /// - Their leader (a chief, the hunt captain, a trial's shaman) can't be hit while any of his fighters stand, as yours
    ///   can't (LeaderProtected). When the last of them falls or flees he leaves the field and the fight is won. In his own
    ///   chief fight he stays and fights on alone instead, so his half health phase, his kneel and his worn out turn after his
    ///   spell all come once he is alone.
    /// - The champion's fight, the last fight of a tribe's road: their chief is the tribe's champion. He can be hit from the
    ///   start, he winds up his big swing for your leader to parry, and when he falls his band breaks.
    /// - A Warden holds the middle: a melee blow at anyone else of his side may meet his shield first. His Defence decides how
    ///   often (LeaderRules.MiddlePercent of it), and the blow lands on him as on a Guard.
    /// - Turned Hunt turns on the hunters: for its seconds nobody covers them and your blows on them land double, since their
    ///   captain can't be hit while they stand.
    /// Claude's calls, Jacob's to change: both sides' Wardens hold the middle; only melee blows meet him; a Warden who is
    /// snared, stunned, panicking or kneeling holds nothing; the champion is his tribe's duel champion (its first Warden or
    /// Skirmisher) with a chief's power and a lieutenant's health, standing in the front row; a trial on that row trades its
    /// shaman for him. Every number is a starting value.
    public static class LeaderRules
    {
        /// Their leader leaves the field: the detail on his side's Break.
        public const string RetreatDetail = "retreat";

        /// Their champion fell: the detail on his side's Break.
        public const string ChampionDetail = "champion";

        /// A Warden holding the middle steps in front of a blow: the detail on his Defence event, whose amount is the chance.
        public const string MiddleDetail = "holds";

        /// The percent of his Defence (AttackTable) that is a Warden's chance to step in front of a melee blow on another of his
        /// side: a Warden's very high Defence of 100 makes it 50.
        public const int MiddlePercent = 50;
    }

    public sealed partial class BattleSimulation
    {
        private bool LeadersOn => _fight.Leaders;
        private bool ChampionOn => _fight.ChampionFight;

        /// True when this fight plays the leaders (campaign rules 43). For the screen.
        public bool LeadersRulesOn => _fight.Leaders;

        /// True when this fight is a tribe's champion's fight (campaign rules 43): their chief is the champion. For the screen.
        public bool ChampionFightOn => _fight.ChampionFight;

        // Their leader stays and fights on alone in his own chief fight; anywhere else he leaves when his last fighter falls.
        private bool LeaderStays => _chiefOn && !_huntOn;

        // The far side's leader whom the rules 43 shield covers while his fighters stand: never the champion.
        private bool TheirLeaderShielded(BattleUnit unit)
        {
            return LeadersOn && !ChampionOn && unit.Side == 1;
        }

        /// The chance, in percent, that this Warden steps in front of a melee blow on another of his side; 0 without the leaders.
        /// Read only, for tests and bots.
        public int MiddleChance(BattleUnit warden)
        {
            if (!LeadersOn || warden == null || warden.Class != UnitClass.Warden) return 0;
            return AttackTable.Of(warden.Class).Defence * LeaderRules.MiddlePercent / 100;
        }

        // A fighter of theirs fell or fled: with none of his fighters left and their leader standing, he leaves the field and
        // the fight is won.
        private void FighterGone(BattleUnit gone)
        {
            if (!LeadersOn || ChampionOn || _finished || gone.Side != 1 || gone.IsLeader || LeaderStays) return;
            bool leaderStands = false;
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (u.Side != 1 || !u.Alive) continue;
                if (!u.IsLeader && !u.Fled && !u.Benched) return;
                if (u.IsLeader) leaderStands = true;
            }
            if (!leaderStands) return;
            _sides[1].Nerve = 0;
            BreakSide(1, LeaderRules.RetreatDetail);
        }

        // Their champion fell: his band breaks, unless nobody of it is left (then it is a wipe). True when it ended the fight.
        private bool ChampionFell(int side)
        {
            if (!ChampionOn || side != 1 || _finished) return false;
            if (AliveCount(side) == 0)
            {
                CheckWipe();
                return true;
            }
            _sides[side].Nerve = 0;
            BreakSide(side, LeaderRules.ChampionDetail);
            return true;
        }

        // A Warden holding the middle: the first Warden of the target's side who stands free, when the target is someone else,
        // may step in front of this melee blow by his Defence. One roll on the fight's stream, so replays stay exact. Null when
        // the blow goes past him.
        private BattleUnit MiddleWarden(BattleUnit attacker, BattleUnit target)
        {
            if (!LeadersOn || attacker == null || attacker.Stats.Ranged || target.Class == UnitClass.Warden) return null;
            // Turned Hunt: nobody covers the hunters while it lasts (BattleSimulation.Hunt.cs).
            if (_huntOn && Turned(target)) return null;
            BattleUnit warden = null;
            for (int i = 0; i < _units.Count && warden == null; i++)
            {
                BattleUnit u = _units[i];
                // Only a fighter holds the middle: a leader or a champion who is a Warden by class never does.
                if (u.Alive && u.Side == target.Side && u.Class == UnitClass.Warden && u.Role == UnitRole.Fighter && !ReferenceEquals(u, target) &&
                    !u.Benched && !u.Fled && !u.Yielded && u.SnareTicks <= 0 && u.StunTicks <= 0 && u.PanicTicks <= 0)
                    warden = u;
            }
            if (warden == null) return null;
            int chance = MiddleChance(warden);
            if (_rng.Next(100) >= chance) return null;
            Emit(EventFor(BattleEventKind.Defence, warden, target, chance, LeaderRules.MiddleDetail));
            return warden;
        }
    }
}
