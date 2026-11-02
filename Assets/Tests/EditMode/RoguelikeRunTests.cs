using System.Collections.Generic;
using System.Linq;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using NUnit.Framework;

namespace KindredSiege.Sim.Tests
{
    /// Jacob, 18 Oct 2026, to make runs differ: "go, tackle all four with your picks: favours from the Pals on road
    /// nodes, three squads per fight shown on the node, random tribe per tier". One quick test each, only to prove they
    /// run (Jacob, 12 Oct 2026: build first). The three squads are tested in LineupTests.
    public class RoguelikeRunTests
    {
        private static Campaign Ready(ulong seed, CampaignTuning tuning)
        {
            Campaign c = Campaign.New(seed, tuning);
            Assert.IsTrue(CampaignPilot.PlayTutorial(c, PilotStyle.Reference, seed, 10), "the tutorial was never won");
            return c;
        }

        [Test]
        public void AFavourFightOffersItsPalsFavoursAndTheTakenOneBendsYourNextFight()
        {
            // From rules 28 the Shade and from rules 29 the Glutton offer their road cards too (ShadeFormsTests, GluttonTests), so
            // the old button favours play only below them.
            var easy = new CampaignTuning { EnemyScaleTier1 = 40, RulesCap = Campaign.WhispererRules };
            for (ulong seed = 5100; seed < 5140; seed++)
            {
                Campaign c = Ready(seed, easy);
                c.StartRun();
                // Every row of the road has one favour fight: never the one with the bonus goal, never an event. From rules 19
                // every fight in a row carries a Pal (fix 5, PalRoadTests).
                for (int row = 0; row < c.Road.Rows; row++)
                {
                    int favourFights = 0, fights = 0;
                    for (int lane = 0; lane < CampaignRoad.Lanes; lane++)
                    {
                        RoadNode node = c.Road.Node(row, lane);
                        if (node.Kind == RoadNodeKind.Fight) fights++;
                        if (node.Pal == Pal.None) continue;
                        favourFights++;
                        Assert.AreEqual(RoadNodeKind.Fight, node.Kind);
                        if (!c.PalRoadRun) Assert.AreEqual(FightObjective.None, node.Objective);
                    }
                    Assert.AreEqual(c.PalRoadRun ? fights : 1, favourFights, c.PalRoadRun ? "a Pal on every fight" : "one favour fight a row");
                }

                // From rules 15 the Butcher's fights offer his combos instead (ButcherComboTests), and from rules 24 the Whisperer's
                // her road card (WhispererTests), so this is another Pal's.
                int index = c.Offers.ToList().FindIndex(o => o.Kind != EncounterKind.Ambush && o.Node != null && o.Node.Pal != Pal.None &&
                    o.Node.Pal != Pal.Butcher && o.Node.Pal != Pal.Whisperer);
                if (index < 0) continue;
                Pal pal = c.Offers[index].Node.Pal;
                int before = c.Materials;
                EncounterResult r = CampaignPilot.PlayFight(c, c.StartEncounter(index), new CampaignPilot(PilotStyle.Reference, seed));
                if (!r.Won) continue;
                // From rules 19 the tribe's gear is on the same card (fix 5); before it a favour fight drops none.
                if (c.PalRoadRun) Assert.IsNotNull(r.SpoilsOffered, "the gear beside the favours");
                else Assert.IsNull(r.SpoilsOffered, "a favour fight drops no gear");
                Assert.AreSame(r.FavourOffered, c.FavourOffer);
                Assert.AreEqual(pal, c.FavourOffer.Pal);
                CollectionAssert.AreEqual(FavourRules.Of(pal), c.FavourOffer.Choices, "that Pal's three favours");
                Assert.AreEqual(before + r.MaterialsEarned, c.Materials, "the materials are banked until a favour is taken");

                // Saved with the offer standing, it comes back the same.
                string saved = CampaignSave.Write(c);
                Assert.AreEqual(saved, CampaignSave.Write(CampaignSave.Read(saved, easy)));

                Favour taken = c.FavourOffer.Choices[0];
                Assert.IsTrue(c.TryTakeFavour(0, out string why), why);
                Assert.AreEqual(before, c.Materials, "taking a favour gives those materials back, as a piece does");
                Assert.AreEqual(1, c.FavourRank(taken));
                Assert.IsNull(c.FavourOffer);
                Assert.IsFalse(c.TryTakeFavour(0, out _), "only once");

                // It bends your side's orders in the next fight, and only yours, and a save keeps it.
                CampaignFight next = c.StartEncounter(0);
                CollectionAssert.AreEqual(FavourRules.ToSide(c.Favours).ToArray(), next.Setup.Rules(0).Favours.ToArray());
                Assert.IsFalse(next.Setup.Rules(0).Favours.IsNone);
                Assert.IsTrue(next.Setup.Rules(1).Favours.IsNone);
                Campaign read = CampaignSave.Read(CampaignSave.Write(c), easy);
                Assert.IsEmpty(read.LoadNote, "the fight with the favour plays back the same");
                Assert.AreEqual(1, read.FavourRank(taken));
                return;
            }
            Assert.Fail("no favour fight was won in forty campaigns");
        }

        // Two plain squads of three; only your side's favours differ.
        private static BattleSimulation Fight(SideFavours favours)
        {
            var mine = new List<UnitSpec>
            {
                new UnitSpec("c.warden", UnitClass.Warden), new UnitSpec("c.skirm", UnitClass.Skirmisher), new UnitSpec("c.marks", UnitClass.Marksman)
            };
            var them = new List<UnitSpec>
            {
                new UnitSpec("t.warden", UnitClass.Warden), new UnitSpec("t.skirm", UnitClass.Skirmisher), new UnitSpec("t.marks", UnitClass.Marksman)
            };
            return new BattleSimulation(new BattleSetup("favours", 21, mine, them, new TacticsProfile(), new TacticsProfile(),
                BattleSetup.DefaultTickLimit, BattleSetup.DefaultDirectivePoints, NerveRules.On(), SliceRules.RefillTicks, 400,
                new SideRules(0, 0, true, favours: favours), new SideRules(0, 0, false), FightRules.Skill));
        }

        [Test]
        public void AFavourBendsTheOrderItNames()
        {
            // The Butcher's Long Rage at its last rank: Unleash lasts 60% longer. Nothing else changes before the order.
            var rage = new SideFavours(unleashLongerPercent: FavourRules.LongerPercent * FavourRules.MaxRank);
            BattleSimulation bent = Fight(rage), plain = Fight(SideFavours.None);
            var a = new SimSession(bent);
            var b = new SimSession(plain);
            while (!bent.IsFinished && bent.CurrentTick < 900 && bent.DirectivePoints(0) < Directives.Cost(Directive.Unleash))
            {
                a.Tick();
                b.Tick();
            }
            Assert.AreEqual(bent.DirectivePoints(0), plain.DirectivePoints(0), "the same fight until the order");
            Assert.IsTrue(a.TryIssue(Directives.CommandKind, "0", string.Empty, 0, 0, "Unleash", out string why), why);
            Assert.IsTrue(b.TryIssue(Directives.CommandKind, "0", string.Empty, 0, 0, "Unleash", out why), why);
            a.Tick();
            b.Tick();
            Assert.AreEqual(BattleSimulation.UnleashTicks * 60 / 100, bent.UnleashTicksLeft(0) - plain.UnleashTicksLeft(0),
                "Unleash lasts 60% longer");
            Assert.AreEqual(bent.HoldTicksLeft(0), plain.HoldTicksLeft(0), "and nothing else moved");
        }

        [Test]
        public void ARunMeetsOneTribeOfEachTierPickedAtRandom()
        {
            // Each of the slice's two tiers holds two tribes since the new tribes (18 Oct 2026): a run meets Bloodridge or
            // Stoneveil, then Skullpeak or Thundercoast, and goes where its route says.
            var seenTribes = new HashSet<string>();
            for (ulong seed = 5300; seed < 5316; seed++)
            {
                Campaign c = Ready(seed, new CampaignTuning());
                IReadOnlyList<CampaignTribe> route = c.RunRoute;
                Assert.AreEqual(2, route.Count);
                Assert.AreEqual(1, route[0].Tier);
                Assert.AreEqual(2, route[1].Tier);
                Assert.AreSame(route[0], c.NextRunTribe);
                c.StartRun();
                Assert.AreSame(route[0], c.Target);
                CollectionAssert.AreEqual(route, c.RunRoute, "the same run, the same route");
                foreach (CampaignTribe t in route) seenTribes.Add(t.Id);
            }
            CollectionAssert.AreEquivalent(new[] { CampaignTribes.BloodridgeId, CampaignTribes.StoneveilId, CampaignTribes.SkullpeakId,
                CampaignTribes.ThundercoastId }, seenTribes, "every tribe comes up");

            // With more tribes in a tier, a run picks one by the campaign's seed and the run: the same run always the same
            // one, and over the runs every tribe of the tier comes up.
            var seen = new HashSet<int>();
            for (int run = 1; run <= 40; run++)
            {
                int pick = Campaign.TierPick(3, 77UL, run, 1);
                Assert.AreEqual(pick, Campaign.TierPick(3, 77UL, run, 1));
                Assert.That(pick, Is.InRange(0, 2));
                seen.Add(pick);
            }
            Assert.AreEqual(3, seen.Count, "every tribe of the tier came up");
            Assert.AreEqual(0, Campaign.TierPick(1, 77UL, 5, 1), "a tier of one always sends it");
        }
    }
}
