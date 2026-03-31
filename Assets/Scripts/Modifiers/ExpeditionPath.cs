using System.Collections.Generic;
using KindredSiege.Battle;
using KindredSiege.Rivalry;

namespace KindredSiege.Modifiers
{
    public struct ExpeditionPath
    {
        public List<MutationType> Mutations;
        public RivalData          Rival;
        public EncounterType      Encounter;
        public string             Reward;
        public bool               IsDomainExpansion;
        public KindredSiege.City.DistrictType? TargetDistrict;
    }
}
