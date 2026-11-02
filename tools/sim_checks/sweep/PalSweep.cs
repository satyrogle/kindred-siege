using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KindredSiege.Sim;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;

// The builds test (DESIGN_LOCKS "Builds" and "The Butcher's theme"), for one Pal, 1 Nov 2026. On the same seeds: a run that takes
// none of his picks, a random pilot, a themed one (CampaignPilot.PickFavour), and a themed one leaning into each of his lines
// (CampaignPilot.ThemeLine). Every run routes to his fight whenever one is on offer and takes his picks and nothing else (no
// spoils, no other Pal); the themed Glutton also feeds. The fight pilot is the same for all and presses the Shade's follow ups.
// One CSV line a run: seed, lean, mode, scale, style, run won, fights won, fights, picks, his fights, chief fights, chief wins,
// follow up tricks, fed, depth, error, health margin, bursts. Build it against Assets/Scripts/Sim as Sweep.cs is.
//   args: lean seedStart seedCount enemyScale style [lines: 0 for none]
public static class PalSweep
{
    private static CampaignTuning Tuning(int scale) => new CampaignTuning { EnemyScaleTier1 = scale, EnemyScaleTier2 = scale * 86 / 95 };

    private static readonly string[] Tricks = { "rupture", "ram", "echo" };

    // A side's health left, in thousandths of its whole.
    private static int Share(BattleSimulation sim, int side)
    {
        long hp = 0, max = 0;
        foreach (BattleUnit u in sim.Units)
        {
            if (u.Side != side || u.Benched) continue;
            hp += Math.Max(0, u.Hp);
            max += u.MaxHp;
        }
        return max <= 0 ? 0 : (int)(hp * 1000 / max);
    }

    // How deep the run leans: the Butcher's most combos of one flavour, the Whisperer's top rank, the Shade's top form,
    // the Glutton's most talents in one limb.
    private static int Depth(Campaign c, Pal lean)
    {
        switch (lean)
        {
            case Pal.Butcher:
                return c.Gear.Where(g => ClassSpells.IsCombo(g.Bend)).GroupBy(g => ClassSpells.AugmentOf(g.Bend))
                    .Select(g => g.Sum(x => x.Rank)).DefaultIfEmpty(0).Max();
            case Pal.Whisperer: return c.Reserve.Select(x => x.Rank).DefaultIfEmpty(0).Max();
            case Pal.Shade: return c.ShadeRoutes.Select(x => x.Form).DefaultIfEmpty(0).Max();
            default:
                return Enumerable.Range(0, GluttonRules.LimbCount)
                    .Max(l => Enumerable.Range(0, GluttonRules.PerLimb).Count(k => c.HoldsTalent(GluttonRules.At(l, k))));
        }
    }

    // Each line a themed run can lean into: the Butcher's flavours, the Whisperer's classes, the Shade's routes, the Glutton's limbs.
    private static IEnumerable<int> Lines(Pal lean)
    {
        switch (lean)
        {
            case Pal.Butcher: return Enumerable.Range(1, 3);
            case Pal.Whisperer: return Enumerable.Range(0, 5);
            case Pal.Shade: return Enumerable.Range(1, 8);
            default: return Enumerable.Range(0, GluttonRules.LimbCount);
        }
    }

    public static int Main(string[] args)
    {
        Pal lean = (Pal)Enum.Parse(typeof(Pal), args[0]);
        ulong seed0 = ulong.Parse(args[1]);
        int count = int.Parse(args[2]);
        int scale = int.Parse(args[3]);
        PilotStyle style = (PilotStyle)Enum.Parse(typeof(PilotStyle), args[4]);
        // A 6th arg of 0 plays only none, random and themed, leaving out the lines.
        bool lines = args.Length <= 5 || args[5] != "0";
        CampaignTuning t = Tuning(scale);
        TextWriter o = Console.Out;
        for (ulong seed = seed0; seed < seed0 + (ulong)count; seed++)
        {
            Campaign start = Campaign.New(seed, t);
            if (!CampaignPilot.PlayTutorial(start, PilotStyle.Reference, seed, 10)) continue;
            start.StartRun();
            string save = CampaignSave.Write(start);
            var modes = new List<int> { -3, -2, -1 };
            if (lines) modes.AddRange(Lines(lean));
            foreach (int line in modes)
            {
                bool themed = line >= -1;
                Campaign c = CampaignSave.Read(save, t);
                var picker = new CampaignPilot(PilotStyle.Reference, seed) { ThemeLine = line };
                long share = 0; int bursts = 0, depth = 0, won = 0, fights = 0, picks = 0, chiefs = 0, chiefWins = 0, presses = 0, fed = 0, palFights = 0;
                bool runWon = false;
                string error = "";
                try
                {
                    for (int n = 0; c.Stage == CampaignStage.InRun && n < 80; n++)
                    {
                        int offer = picker.PickOffer(c, lean, themed);
                        Encounter e = c.Offers[offer];
                        if (e.Node != null && e.Node.Pal == lean) palFights++;
                        CampaignFight f = c.StartEncounter(offer);
                        EncounterResult r = CampaignPilot.PlayFight(c, f, new CampaignPilot(style, seed * 31UL + (ulong)n));
                        fights++;
                        if (r.Won) won++;
                        share += Share(f.Sim, 0) - Share(f.Sim, 1);
                        bursts += f.Sim.Events.Count(ev => ev.Kind == BattleEventKind.Burst && ev.ActorSide == 0);
                        if (e.Kind == EncounterKind.Chief)
                        {
                            chiefs++;
                            if (r.Won) chiefWins++;
                        }
                        foreach (BattleEvent ev in f.Sim.Events)
                            if (ev.Kind == BattleEventKind.Trick && ev.ActorSide == 0 && Array.IndexOf(Tricks, ev.Detail) >= 0) presses++;
                        if (r.RunOver)
                        {
                            runWon = r.RunWon;
                            break;
                        }
                        if (line != -3 && c.FavourOffer != null && c.FavourOffer.Pal == lean)
                        {
                            int pick = picker.PickFavour(c, themed);
                            if (pick >= 0 && c.TryTakeFavour(pick, out _)) picks++;
                            depth = Math.Max(depth, Depth(c, lean));
                        }
                        if (themed && lean == Pal.Glutton && CampaignPilot.TryFeed(c)) fed = 1;
                    }
                }
                catch (Exception ex)
                {
                    error = ex.GetType().Name + ":" + ex.Message.Replace(',', ';').Replace('\n', ' ');
                }
                o.WriteLine(string.Join(",", seed, lean, line == -3 ? "none" : themed ? "themed" + line : "random", scale, style, runWon ? 1 : 0, won, fights,
                    picks, palFights, chiefs, chiefWins, presses, fed, depth, error, share, bursts));
            }
            o.Flush();
        }
        return 0;
    }
}
