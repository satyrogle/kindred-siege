using System.Collections.Generic;
using System.Linq;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using NUnit.Framework;

namespace KindredSiege.Sim.Tests
{
    /// Jacob, 18 Oct 2026: "three squads per fight shown on the node". Each tribe has three squads for each kind of road
    /// fight and for its chief's band; a road node sends the one its own dice pick and shows it before you go, and the
    /// chief sends the run's. From 11 Oct each tribe sent the same lineup every time ("fixed per fight"); before that the
    /// lineup turned with the number of fights played, and the same fight swung from about 1% to 98% for a sensible bot.
    public class LineupTests
    {
        // Tier 2 enemies at half strength, so the reference pilot still reaches Skullpeak's chief under the class spells
        // (20 Oct 2026). These tests check line ups, not balance.
        // Historical mixed-node road. A reload takes the same tuning, or it would play today's rules (the cap is not saved): from
        // rules 43 the road's last row is the champion's, whose band is one fighter smaller.
        private static CampaignTuning Pinned() => new CampaignTuning { EnemyScaleTier2 = 50, RulesCap = 12 };

        private static Campaign Ready(ulong seed)
        {
            Campaign c = Campaign.New(seed, Pinned());
            Assert.IsTrue(CampaignPilot.PlayTutorial(c, PilotStyle.Reference, seed, 10), "the tutorial was never won");
            return c;
        }

        private static List<UnitClass> Fighters(IReadOnlyList<UnitSpec> side)
        {
            var classes = new List<UnitClass>();
            // A chief's reserves (20 Oct 2026) only come in at his second phase: not the squad the node shows.
            for (int i = 0; i < side.Count; i++) if (side[i].Role == UnitRole.Fighter && !side[i].Reserve) classes.Add(side[i].Class);
            return classes;
        }

        private static List<UnitClass> Expected(CampaignTribe tribe, EncounterKind kind, int count, int squad = 0)
        {
            IReadOnlyList<UnitClass> lineup = tribe.Lineup(kind, squad);
            var classes = new List<UnitClass>();
            // A raid chief brings no band (rules 10).
            if (Campaign.IsRaidChief(tribe, kind, Campaign.RulesVersion)) return classes;
            for (int i = 0; i < count; i++) classes.Add(lineup[i % lineup.Count]);
            return classes;
        }

        [Test]
        public void EveryFightSendsTheSquadItsNodeShowsWhateverCameBefore()
        {
            var seen = new HashSet<string>();
            var squads = new HashSet<string>();
            for (ulong seed = 700; seed < 760; seed++)
            {
                foreach (string tribeId in new[] { CampaignTribes.BloodridgeId, CampaignTribes.SkullpeakId })
                {
                    // A different number of fights before it each time: none, or a few on the road, up to its chief.
                    Campaign c = Ready(seed);
                    c.ChooseTribe(tribeId);
                    // The run's chief band, known from the start: which of the tribe's three it is.
                    CampaignTribe tribe = CampaignTribes.Find(tribeId);
                    for (int squad = 0; squad < 3; squad++)
                        if (Enumerable.SequenceEqual(c.ChiefSquad, Expected(tribe, EncounterKind.Chief, c.ChiefSquad.Count, squad)))
                            squads.Add(tribeId + ".Chief." + squad);
                    var picker = new CampaignPilot(PilotStyle.Reference, seed);
                    int before = (int)(seed % (ulong)(c.PathLength + 1));
                    for (int n = 0; n < before && c.Stage == CampaignStage.InRun && c.Offers[0].Kind != EncounterKind.Chief; n++)
                        CampaignPilot.PlayFight(c, c.StartEncounter(picker.PickOffer(c)), new CampaignPilot(PilotStyle.Reference, seed + (ulong)n));
                    if (c.Stage != CampaignStage.InRun) continue;
                    for (int i = 0; i < c.Offers.Count; i++)
                    {
                        Encounter e = c.Offers[i];
                        if (e.Kind == EncounterKind.Duel) continue;
                        // What the node (or the chief's band) shows is what comes, after a save too.
                        IReadOnlyList<UnitClass> shown = e.Kind == EncounterKind.Chief ? c.ChiefSquad : c.SquadOn(e.Node);
                        CollectionAssert.AreEqual(e.EnemyClasses, shown, "the node shows the squad that comes");
                        Campaign copy = CampaignSave.Read(CampaignSave.Write(c), Pinned());
                        CampaignFight fight = copy.StartEncounter(i);
                        CollectionAssert.AreEqual(Expected(e.Tribe, e.Kind, e.EnemyFighters, e.Squad), Fighters(fight.Setup.Side(1)),
                            e.Tribe.Name + " " + e.Kind + " after " + before + " fights");
                        CollectionAssert.AreEqual(shown, Fighters(fight.Setup.Side(1)));
                        seen.Add(e.Tribe.Id + "." + e.Kind);
                        squads.Add(e.Tribe.Id + "." + e.Kind + "." + e.Squad);
                    }
                }
            }
            foreach (string key in new[] { "raiders.WarParty", "raiders.HoldTheLine", "raiders.Trial", "raiders.Chief",
                "skullpeak.WarParty", "skullpeak.HoldTheLine", "skullpeak.Trial", "skullpeak.Chief" })
            {
                Assert.IsTrue(seen.Contains(key), key + " was never offered");
                for (int squad = 0; squad < 3; squad++) Assert.IsTrue(squads.Contains(key + "." + squad), key + " never sent squad " + squad);
            }
        }

        [Test]
        public void TheTutorialAndTheHuntSendTheirLineupsToo()
        {
            Campaign fresh = Campaign.New(761);
            CampaignFight tutorial = fresh.StartTutorial();
            CollectionAssert.AreEqual(Expected(CampaignTribes.Bloodridge, EncounterKind.Tutorial, tutorial.Encounter.EnemyFighters),
                Fighters(tutorial.Setup.Side(1)));

            for (ulong seed = 762; seed < 822; seed++)
            {
                Campaign c = Campaign.New(seed, new CampaignTuning { EnemyScaleTier1 = 50, EnemyScaleTier2 = 50 });
                if (!CampaignPilot.PlayTutorial(c, PilotStyle.Reference, seed, 10)) continue;
                c.StartRun();
                var picker = new CampaignPilot(PilotStyle.Reference, seed);
                for (ulong n = 0; c.Stage == CampaignStage.InRun && c.Target != AlphaHunt.Tribe; n++)
                    CampaignPilot.PlayFight(c, c.StartEncounter(picker.PickOffer(c)), new CampaignPilot(PilotStyle.Reference, seed * 31UL + n));
                if (c.Target != AlphaHunt.Tribe) continue;
                CampaignFight hunt = c.StartEncounter(0);
                CollectionAssert.AreEqual(Expected(AlphaHunt.Tribe, EncounterKind.AlphaHunt, hunt.Encounter.EnemyFighters),
                    Fighters(hunt.Setup.Side(1)));
                return;
            }
            Assert.Fail("the reference pilot never reached the hunt in sixty campaigns");
        }
    }
}
