using System;
using System.Linq;
using System.Reflection;
using KindredSiege.Sim;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;

// Replays the 6 Nov all in fight (seed 5100, road fight 9, dump55.txt) with the run held at rules 41 and only that fight
// played at the rules given (41 or 42), so the looks mock stands on the same five against five with the attack table on.
// Mode 41 must print the same fight as dump55.txt. Directive lines are kept this time. Scratch tool, not in the repo.
public static class FightDump3
{
    static readonly UnitClass[] Five = { UnitClass.Warden, UnitClass.Skirmisher, UnitClass.Trapper, UnitClass.Marksman, UnitClass.Medic };

    static bool AllFive(BattleSimulation s, int side)
    {
        var cls = s.Units.Where(u => u.Side == side && u.Role == UnitRole.Fighter).Select(u => u.Class).ToList();
        return Five.All(cls.Contains);
    }

    public static void Main(string[] a)
    {
        int last = int.Parse(a[0]);
        ulong seed = 5100;
        var c = Campaign.New(seed, new CampaignTuning { EnemyScaleTier1 = 40, RulesCap = 41 });
        var tuning = (CampaignTuning)typeof(Campaign).GetField("_tuning", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(c);
        if (!CampaignPilot.PlayTutorial(c, PilotStyle.Reference, seed, 10)) { Console.WriteLine("tutorial lost"); return; }
        c.StartRun();
        var picker = new CampaignPilot(PilotStyle.Reference, seed);
        for (int n = 0; c.Stage == CampaignStage.InRun && n < 30; n++)
        {
            if (n == 9) tuning.RulesCap = last;
            CampaignFight f = c.StartEncounter(picker.PickOffer(c));
            CampaignPilot.PlayFight(c, f, new CampaignPilot(PilotStyle.Reference, seed * 31UL + (ulong)n));
            if (c.FavourOffer != null) c.TryTakeFavour(0, out _);
            else if (c.SpoilsChoices.Count > 0) c.TryTakeSpoils(0, out _);
            var sim = f.Sim;
            if (!AllFive(sim, 0) || !AllFive(sim, 1)) continue;
            Console.WriteLine("seed " + seed + " fight " + n + " " + f.Setup.FightId + " rules " + f.RulesVersion + " ticks " + sim.CurrentTick + " tps " + SimClock.TicksPerSecond + " table " + f.Setup.Fight.Table);
            foreach (var u in sim.Units)
                Console.WriteLine("U " + u.Side + " " + u.Id + " " + u.Class + " " + u.Role + " alive " + u.Alive + " hp " + u.MaxHp);
            foreach (var e in sim.Events)
            {
                if (e.Kind == BattleEventKind.Earn) continue;
                if (e.Kind == BattleEventKind.Directive) { Console.WriteLine("D " + e.Tick + " " + e.ActorSide + " " + e.ActorId + " > " + e.TargetId + " " + e.Amount + " " + e.Detail); continue; }
                Console.WriteLine("E " + e.Tick + " " + e.Kind + " " + e.ActorSide + " " + e.ActorId + " > " + e.TargetId + " " + e.Amount + " " + e.Detail);
            }
            // Every tick of the archived replay: each living unit's guard links, aim and statuses, for the marks layer.
            if (!FightPlayback.TryOpen(c.Archive, f.Setup.FightId, out FightPlayback play, out string why)) { Console.WriteLine("no playback " + why); return; }
            while (!play.AtEnd && play.Step())
                foreach (var u in play.Sim.Units)
                {
                    if (!u.Alive) continue;
                    string g = (string.IsNullOrEmpty(u.GuardTargetId) ? "-" : u.GuardTargetId) + "/" + (string.IsNullOrEmpty(u.GuardTargetId2) ? "-" : u.GuardTargetId2);
                    string aim = u.AimTicks > 0 && !string.IsNullOrEmpty(u.AimTargetId) ? u.AimTargetId + ":" + u.AimTicks : "-";
                    string m = string.Join(",", u.Statuses.All.Where(s => s != null && s.On).Select(s => s.Kind + "=" + s.Ticks));
                    if (g == "-/-" && aim == "-" && m.Length == 0) continue;
                    Console.WriteLine("S " + play.Tick + " " + u.Id + " g:" + g + " a:" + aim + " m:" + (m.Length == 0 ? "-" : m));
                }
            Console.WriteLine("playback " + play.Tick + " matched " + play.Matched);
            return;
        }
        Console.WriteLine("none found");
    }
}
