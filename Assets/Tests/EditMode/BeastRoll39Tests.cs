using System;
using System.Collections.Generic;
using System.Linq;
using KindredSiege.Sim;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using NUnit.Framework;

namespace KindredSiege.Sim.Tests
{
    /// Beasts as rolls, campaign rules 39 (Jacob, 2 Nov 2026, DESIGN_LOCKS "Beasts as rolls, tier 1 to 50"; CampaignBeast.cs):
    /// a run rolls 1 in 5 for a beast on a fight node of its first road, each beast keeps its tier at camp, beating it raises the
    /// tier, losing to it costs a life and keeps the tier. One quick test per piece, only to prove it runs, and the version
    /// boundary. Nothing here is tuning.
    public class BeastRoll39Tests
    {
        // A campaign at camp after the tutorial, written as a save, so a run of any number can be read from it without fights.
        private static string CampAfterTutorial(ulong seed, CampaignTuning t)
        {
            Campaign c = Campaign.New(seed, t);
            Assert.IsTrue(CampaignPilot.PlayTutorial(c, PilotStyle.Reference, seed, 10), "the tutorial was never won");
            Assert.AreEqual(CampaignStage.ChooseTribe, c.Stage);
            return CampaignSave.Write(c);
        }

        // The camp save with this many runs already started (the roll's dice are the run's number), and a beast tier if given.
        private static Campaign Camp(string camp, int runsBefore, CampaignTuning t, BeastKit tierKit = BeastKit.None, int tier = 0)
        {
            string[] lines = camp.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                if (!lines[i].StartsWith("counters\t", StringComparison.Ordinal)) continue;
                string[] f = lines[i].Split('\t');
                f[3] = runsBefore.ToString(System.Globalization.CultureInfo.InvariantCulture);
                lines[i] = string.Join("\t", f);
                if (tierKit != BeastKit.None) lines[i] += "\nbeasttier\t" + (int)tierKit + "\t" + tier;
            }
            Campaign c = CampaignSave.Read(string.Join("\n", lines), t);
            Assert.IsEmpty(c.LoadNote);
            Assert.AreEqual(runsBefore, c.RunsStarted);
            return c;
        }

        private static RoadNode BeastNode(Campaign c)
        {
            return c.Road == null ? null : c.Road.All.FirstOrDefault(n => n.Beast != BeastKit.None);
        }

        // The first run number from this one on whose roll brings a beast, with its node in the first row (so it is on offer
        // at once) when firstRow is set.
        private static int RolledRun(string camp, CampaignTuning t, bool firstRow, BeastKit kit = BeastKit.None)
        {
            for (int run = 0; run < 400; run++)
            {
                Campaign c = Camp(camp, run, t);
                c.StartRun();
                RoadNode node = BeastNode(c);
                if (node == null || (firstRow && node.Row != 0) || (kit != BeastKit.None && node.Beast != kit)) continue;
                return run;
            }
            Assert.Fail("no run in four hundred rolled that beast");
            return -1;
        }

        [Test]
        public void ARunRollsItsBeastOneTimeInFiveOntoAFightNodeOfItsFirstRoad()
        {
            var t = new CampaignTuning();
            string camp = CampAfterTutorial(3900, t);
            int rolled = 0;
            const int Runs = 200;
            for (int run = 0; run < Runs; run++)
            {
                Campaign c = Camp(camp, run, t);
                c.StartRun();
                Assert.IsTrue(c.BeastRollRun);
                Assert.IsNull(c.BeastOffer, "no beast waits beside the road any more");
                Assert.AreEqual(BeastKit.None, c.RoadBeast);
                List<RoadNode> beasts = c.Road.All.Where(n => n.Beast != BeastKit.None).ToList();
                Assert.AreEqual(c.RunBeast == BeastKit.None ? 0 : 1, beasts.Count, "one node, and only when the run rolled one");
                if (beasts.Count == 0) continue;
                rolled++;
                RoadNode node = beasts[0];
                Assert.AreEqual(c.RunBeast, node.Beast);
                Assert.AreEqual(RoadNodeKind.Fight, node.Kind);
                Assert.AreEqual(Pal.None, node.Pal, "it drops trophies, not a Pal's favour");
                Assert.IsFalse(c.IsAmbush(node), "the hunters never take it");
                Assert.IsEmpty(c.SquadOn(node), "no tribe's fighters stand on it");

                // The same run read back from its save has the same beast on the same node.
                Campaign read = CampaignSave.Read(CampaignSave.Write(c), t);
                Assert.IsEmpty(read.LoadNote);
                RoadNode again = BeastNode(read);
                Assert.IsNotNull(again);
                Assert.AreEqual(node.Row, again.Row);
                Assert.AreEqual(node.Lane, again.Lane);
                Assert.AreEqual(node.Beast, again.Beast);
            }
            // 1 in 5 of 200 is 40; this only proves the roll is there and is neither always nor never.
            Assert.That(rolled, Is.InRange(20, 64), "rolled " + rolled + " of " + Runs);
        }

        [Test]
        public void OnlyTheFirstRoadAndOnlyFromRules39()
        {
            var t = new CampaignTuning();
            string camp = CampAfterTutorial(3910, t);
            int run = RolledRun(camp, t, false);

            // The same run started deeper has no beast on its road.
            Campaign deeper = Camp(camp, run, t);
            deeper.ChooseTribe(CampaignTribes.ThundercoastId);
            Assert.IsNull(BeastNode(deeper));

            // Capped at rules 38 the same run keeps the shark beside its first road and no beast node.
            var old = new CampaignTuning { RulesCap = Campaign.PitRules };
            Campaign before = Camp(CampAfterTutorial(3910, old), run, old);
            before.StartRun();
            Assert.IsFalse(before.BeastRollRun);
            Assert.IsNull(BeastNode(before));
            Assert.IsNotNull(before.BeastOffer, "the shark still waits beside the first road");
        }

        [Test]
        public void ItComesAtItsTierAndBeatingItRaisesTheTier()
        {
            // Weak enemies, so the reference pilot wins.
            var t = new CampaignTuning { EnemyScaleTier1 = 20 };
            string camp = CampAfterTutorial(3920, t);
            int run = RolledRun(camp, t, true, BeastKit.SandShark);

            // At tier 1, and at tier 10 written at camp: the same parts, 172 percent as tough and 145 percent as hard hitting.
            Campaign one = Camp(camp, run, t);
            one.StartRun();
            Campaign c = Camp(camp, run, t, BeastKit.SandShark, 10);
            c.StartRun();
            Assert.AreEqual(10, c.BeastTier(BeastKit.SandShark));
            Assert.AreEqual(1, c.BeastTier(BeastKit.CageBear));
            int offer = c.Offers.ToList().FindIndex(o => o.Kind == EncounterKind.Beast);
            Assert.That(offer, Is.GreaterThanOrEqualTo(0), "the beast's node is on offer in the first row");
            Encounter e = c.Offers[offer];
            Assert.AreEqual(BeastKit.SandShark, e.Beast);
            Assert.AreEqual(10, e.BeastTier);
            Assert.AreEqual(FightObjective.None, e.Objective);
            Assert.AreEqual(1, one.Offers.First(o => o.Kind == EncounterKind.Beast).BeastTier);

            CampaignFight low = one.StartEncounter(one.Offers.ToList().FindIndex(o => o.Kind == EncounterKind.Beast));
            CampaignFight fight = c.StartEncounter(offer);
            Assert.AreEqual(Campaign.RulesVersion, fight.RulesVersion);
            for (int p = 0; p < BeastKits.PartCount; p++)
            {
                UnitSpec a = low.Setup.Side(1)[p], b = fight.Setup.Side(1)[p];
                Assert.AreEqual(a.PowerPercent * BeastTierRules.PowerPercent(10) / 100, b.PowerPercent);
                Assert.AreEqual(Math.Max(1, a.HealthPercent * BeastTierRules.HealthPercent(10) / 100), b.HealthPercent);
            }

            int step = c.Step;
            int materials = c.Materials;
            EncounterResult r = CampaignPilot.PlayFight(c, fight, new CampaignPilot(PilotStyle.Reference, 3920));
            Assert.IsTrue(r.Won, "the reference pilot beat a weak tier 10 shark");
            Assert.AreEqual(11, r.BeastTierAfter);
            Assert.AreEqual(11, c.BeastTier(BeastKit.SandShark), "it comes back a tier stronger");
            Assert.AreEqual(e.Node.Row + 1, c.Step, "you stand on its node");
            Assert.AreEqual(step + 1, c.Step);
            Assert.AreEqual(BeastTierRules.Trophies(10) * r.PartsBroken.Count, r.Trophies.Count, "two trophies a part at tier 10");
            Assert.AreEqual(t.MaterialsPerFight * BeastTierRules.MaterialsPercent(10) / 100, r.MaterialsEarned);
            Assert.AreEqual(materials + r.MaterialsEarned, c.Materials);
            Assert.IsTrue(c.Archive.TryReplay(fight.Setup.FightId, out _, out string failure), failure);

            // The tier is kept at camp through a save.
            Campaign read = CampaignSave.Read(CampaignSave.Write(c), t);
            Assert.IsEmpty(read.LoadNote);
            Assert.AreEqual(11, read.BeastTier(BeastKit.SandShark));
        }

        [Test]
        public void LosingToItCostsALifeAndKeepsItsTier()
        {
            // A tier 50 cage bear against nobody giving orders: it wins.
            var t = new CampaignTuning();
            string camp = CampAfterTutorial(3930, t);
            int run = RolledRun(camp, t, true, BeastKit.CageBear);
            Campaign c = Camp(camp, run, t, BeastKit.CageBear, BeastTierRules.Max);
            c.StartRun();
            int lives = c.Lives;
            int offer = c.Offers.ToList().FindIndex(o => o.Kind == EncounterKind.Beast);
            EncounterResult r = CampaignPilot.PlayFight(c, c.StartEncounter(offer), new CampaignPilot(PilotStyle.Passive, 3930));
            Assert.IsFalse(r.Won, "nobody giving orders lost to a tier 50 cage bear");
            Assert.AreEqual(lives - 1, c.Lives, "a life, like any road fight");
            Assert.AreEqual(lives - 1, r.LivesLeft);
            Assert.AreEqual(BeastTierRules.Max, r.BeastTierAfter);
            Assert.AreEqual(BeastTierRules.Max, c.BeastTier(BeastKit.CageBear), "losing leaves its tier");
            Assert.IsEmpty(r.Trophies);
            Assert.IsNull(r.Captured);
            Assert.AreEqual(0, c.Step, "you try this step again");
            if (!r.RunOver) Assert.IsTrue(c.Offers.Any(o => o.Kind == EncounterKind.Beast), "it waits on its node");
        }

        [Test]
        public void ARolledBeastFightSavedHalfwayResumesExactly()
        {
            var t = new CampaignTuning();
            string camp = CampAfterTutorial(3940, t);
            int run = RolledRun(camp, t, true, BeastKit.SandShark);
            Campaign c = Camp(camp, run, t, BeastKit.SandShark, 7);
            c.StartRun();
            CampaignFight fight = c.StartEncounter(c.Offers.ToList().FindIndex(o => o.Kind == EncounterKind.Beast));
            var pilot = new CampaignPilot(PilotStyle.Reference, 3940);
            while (!fight.Over && fight.Sim.CurrentTick < BeastKits.SweepEveryTicks + 60)
            {
                pilot.Step(fight);
                fight.Tick();
            }
            Assert.IsFalse(fight.Over);
            Campaign half = CampaignSave.Read(CampaignSave.Write(c), t);
            Assert.IsEmpty(half.LoadNote);
            Assert.AreEqual(EncounterKind.Beast, half.Active.Encounter.Kind);
            Assert.AreEqual(7, half.Active.Encounter.BeastTier);
            Assert.AreEqual(fight.Session.Checksum, half.Active.Session.Checksum);
        }
    }
}
