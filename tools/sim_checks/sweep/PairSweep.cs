using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KindredSiege.Sim;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;

// The duo check, 1 Nov 2026. When a run leaning into two Pals first holds picks from both, how many of their cards stand after
// that, how often their duo shows and when it is taken; and the runs won by a themed pilot taking the duo first, a random one,
// the themed one never taking a duo, and the themed one capped at rules 29, before the duos. Routes to either Pal's fight and takes every pick of both. One CSV
// line a run: seed, both Pals, scale, style, mode, run won, fights won, fights, picks of each, the fight both were first held after (-1 never), their cards
// after, cards showing the duo, the fight the duo was taken after (-1 never), and the star's casts from a duo.
//   args: palA palB seedStart seedCount enemyScale style
public static class PairSweep
{
    public static int Main(string[] args)
    {
        Pal a = (Pal)Enum.Parse(typeof(Pal), args[0]), b = (Pal)Enum.Parse(typeof(Pal), args[1]);
        ulong seed0 = ulong.Parse(args[2]);
        int count = int.Parse(args[3]), scale = int.Parse(args[4]);
        PilotStyle style = (PilotStyle)Enum.Parse(typeof(PilotStyle), args[5]);
        // The modes: themed and random at the current rules, and themed capped at rules 29, before the duos.
        var modes = new[] { ("themed", true, 0), ("random", false, 0), ("noduo", true, 0), ("themed26", true, Campaign.GluttonTreeRules) };
        for (ulong seed = seed0; seed < seed0 + (ulong)count; seed++)
            foreach (var (mode, themed, cap) in modes)
            {
                var t = new CampaignTuning { EnemyScaleTier1 = scale, EnemyScaleTier2 = scale * 86 / 95 };
                if (cap > 0) t.RulesCap = cap;
                Campaign c = Campaign.New(seed, t);
                if (!CampaignPilot.PlayTutorial(c, PilotStyle.Reference, seed, 10)) continue;
                c.StartRun();
                var picker = new CampaignPilot(PilotStyle.Reference, seed);
                int fights = 0, won = 0, unlockAt = -1, cardsAfter = 0, picksA = 0, picksB = 0, duoShown = 0, duoAt = -1, stars = 0;
                bool runWon = false;
                for (int n = 0; c.Stage == CampaignStage.InRun && n < 80; n++)
                {
                    int offer = -1;
                    for (int i = 0; i < c.Offers.Count && offer < 0; i++)
                    {
                        Encounter e = c.Offers[i];
                        if (e.Node != null && (e.Node.Pal == a || e.Node.Pal == b) && c.PalPickOffered(e.Node)) offer = i;
                    }
                    if (offer < 0) offer = picker.PickOffer(c);
                    CampaignFight f = c.StartEncounter(offer);
                    EncounterResult r = CampaignPilot.PlayFight(c, f, new CampaignPilot(style, seed * 31UL + (ulong)n));
                    fights++;
                    if (r.Won) won++;
                    stars += f.Sim.Events.Count(e => e.Kind == BattleEventKind.Trick && e.ActorSide == 0 && e.Detail == DuoRules.StarDetail);
                    if (r.RunOver)
                    {
                        runWon = r.RunWon;
                        break;
                    }
                    FavourOffer card = c.FavourOffer;
                    if (card == null || (card.Pal != a && card.Pal != b)) continue;
                    if (unlockAt >= 0) cardsAfter++;
                    if (card.Choices.Any(FavourRules.IsDuo)) duoShown++;
                    int pick = picker.PickFavour(c, themed, mode != "noduo");
                    bool duo = pick >= 0 && FavourRules.IsDuo(card.Choices[pick]);
                    if (pick >= 0 && c.TryTakeFavour(pick, out _))
                    {
                        if (duo) duoAt = fights;
                        else if (card.Pal == a) picksA++;
                        else picksB++;
                    }
                    if (unlockAt < 0 && picksA > 0 && picksB > 0) unlockAt = fights;
                }
                Console.WriteLine(string.Join(",", seed, a, b, scale, style, mode, runWon ? 1 : 0, won, fights, picksA, picksB, unlockAt,
                    cardsAfter, duoShown, duoAt, stars));
            }
        return 0;
    }
}
