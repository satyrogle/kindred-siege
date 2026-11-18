using System;
using System.Linq;
using KindredSiege.Sim;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;

// Plays campaign runs at the current rules (41) with the reference pilot and dumps the first road fight where both
// sides field all five fighter classes, so the art mock can stage a real five against five. Scratch tool, not in the repo.
public static class FightDump2
{
    static readonly UnitClass[] Five = { UnitClass.Warden, UnitClass.Skirmisher, UnitClass.Trapper, UnitClass.Marksman, UnitClass.Medic };

    static bool AllFive(BattleSimulation s, int side)
    {
        var cls = s.Units.Where(u => u.Side == side && u.Role == UnitRole.Fighter).Select(u => u.Class).ToList();
        return Five.All(cls.Contains);
    }

    public static void Main(string[] a)
    {
        for (ulong seed = 5100; seed < 5140; seed++)
        {
            var c = Campaign.New(seed, new CampaignTuning { EnemyScaleTier1 = 40 });
            if (!CampaignPilot.PlayTutorial(c, PilotStyle.Reference, seed, 10)) continue;
            c.StartRun();
            var picker = new CampaignPilot(PilotStyle.Reference, seed);
            for (int n = 0; c.Stage == CampaignStage.InRun && n < 30; n++)
            {
                CampaignFight f = c.StartEncounter(picker.PickOffer(c));
                CampaignPilot.PlayFight(c, f, new CampaignPilot(PilotStyle.Reference, seed * 31UL + (ulong)n));
                if (c.FavourOffer != null) c.TryTakeFavour(0, out _);
                else if (c.SpoilsChoices.Count > 0) c.TryTakeSpoils(0, out _);
                var sim = f.Sim;
                if (!AllFive(sim, 0) || !AllFive(sim, 1)) continue;
                Console.WriteLine("seed " + seed + " fight " + n + " " + f.Setup.FightId + " rules " + f.RulesVersion + " ticks " + sim.CurrentTick + " tps " + SimClock.TicksPerSecond);
                foreach (var u in sim.Units)
                    Console.WriteLine("U " + u.Side + " " + u.Id + " " + u.Class + " " + u.Role + " alive " + u.Alive + " hp " + u.MaxHp);
                foreach (var e in sim.Events)
                {
                    if (e.Kind == BattleEventKind.Directive || e.Kind == BattleEventKind.Earn) continue;
                    Console.WriteLine("E " + e.Tick + " " + e.Kind + " " + e.ActorSide + " " + e.ActorId + " > " + e.TargetId + " " + e.Amount + " " + e.Detail);
                }
                return;
            }
        }
        Console.WriteLine("none found");
    }
}
