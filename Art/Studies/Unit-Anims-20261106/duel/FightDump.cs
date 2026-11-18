using System;
using System.Linq;
using KindredSiege.Sim;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;

// Plays one campaign fight at the current rules (41) with the reference pilot and prints its units and events,
// so the art mock can play the real fight's beats. Scratch tool, not in the repo.
public static class FightDump
{
    public static void Main(string[] a)
    {
        ulong seed = a.Length > 0 ? ulong.Parse(a[0]) : 5;
        var c = Campaign.New(seed, new CampaignTuning());
        var f = c.StartTutorial();
        CampaignPilot.PlayFight(c, f, new CampaignPilot(PilotStyle.Reference, seed));
        var sim = f.Sim;
        Console.WriteLine("rules " + Campaign.RulesVersion +" ticks " + sim.CurrentTick + " tps " + SimClock.TicksPerSecond);
        foreach (var u in sim.Units)
            Console.WriteLine("U " + u.Side + " " + u.Id + " " + u.Class + " " + u.Role + " alive " + u.Alive + " hp " + u.MaxHp);
        foreach (var e in sim.Events)
        {
            if (e.Kind == BattleEventKind.Directive || e.Kind == BattleEventKind.Earn) continue;
            Console.WriteLine("E " + e.Tick + " " + e.Kind + " " + e.ActorSide + " " + e.ActorId + " > " + e.TargetId + " " + e.Amount + " " + e.Detail);
        }
    }
}
