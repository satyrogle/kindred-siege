namespace KindredSiege.Sim.Battle
{
    public sealed partial class BattleSimulation
    {
        public bool ClearRoadOn => _fight.ClearRoadStage > 0;
        public int RoadStage => _fight.ClearRoadStage;
        public bool SquadScreenOn => _fight.SquadScreen;
        // Only the player's leader. Enemy chiefs keep their authored vulnerabilities.
        // Fled and reserve fighters cannot protect anyone; a frozen living ally still stands.
        public bool LeaderProtected(BattleUnit unit)
        {
            if (unit == null || !unit.Alive || !unit.IsLeader) return false;
            // From rules 43 theirs too, never their champion (BattleSimulation.Leaders.cs).
            if (!(SquadScreenOn && unit.Side == 0) && !TheirLeaderShielded(unit)) return false;
            for (int i = 0; i < _units.Count; i++)
            {
                var ally = _units[i];
                if (ally.Side == unit.Side && ally.Alive && !ally.Fled && !ally.IsLeader && !ally.Benched) return true;
            }
            return false;
        }
        public bool DefencesAvailable => !ClearRoadOn || RoadStage >= 2;
        public bool BoostsAvailable => !ClearRoadOn || RoadStage >= 3;
        // From rules 21 Bloodridge's road bands carry their chief's moves from the first fight (BattleSimulation.Danger.cs).
        public bool CarriersAvailable => !ClearRoadOn || RoadStage >= 4 || _dangerOn;
        private bool SimpleEnemy(BattleUnit u) => ClearRoadOn && _unitNerveOn && u.Side == 1;
    }
}
