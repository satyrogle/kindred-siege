using System;

namespace KindredSiege.Sim.Battle
{
    /// Read-only projection for the floor trap marker. Springs is the stored count, not a new calculation.
    public readonly struct TrapRead
    {
        public TrapRead(bool armed,string ownerId,int springs)
        {Armed=armed;OwnerId=ownerId;Springs=springs;}
        public bool Armed {get;}
        public string OwnerId {get;}
        public int Springs {get;}
    }
    public sealed partial class BattleSimulation
    {
        public TrapRead ReadTrap(int side)
        {
            if(side<0||side>=_sides.Length)throw new ArgumentOutOfRangeException(nameof(side));
            var state=_sides[side];
            return new TrapRead(state.TrapArmed,state.TrapOwnerId,state.TrapSprings);
        }
    }
}
