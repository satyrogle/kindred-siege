using System.Linq;
using KindredSiege.Sim.Learning;
using NUnit.Framework;

namespace KindredSiege.Sim.Tests
{
    public class RoutePickerCompatibilityTests
    {
        // A camp-only save fixture with tutorial completed; no combat pilot or tuning changes.
        static Campaign Camp(ulong seed, int runs = 0)
        {
            string[] lines = CampaignSave.Write(Campaign.New(seed)).Split('\n');
            for (int i = 0; i < lines.Length; i++)
                if (lines[i].StartsWith("counters\t"))
                {
                    string[] fields = lines[i].Split('\t');
                    fields[3] = runs.ToString(); fields[5] = "1";
                    lines[i] = string.Join("\t", fields);
                }
            return CampaignSave.Read(string.Join("\n", lines));
        }

        static string Route(Campaign c) => string.Join("/", c.RunRoute.Select(t => t.Id));
        static string Legacy(string save) => string.Join("\n", save.Split('\n').Where(l => !l.StartsWith("routepick\t") && !l.StartsWith("clearroad\t") && !l.StartsWith("palroad\t") && !l.StartsWith("beastroll\t")))
            .Replace(CampaignSave.Header + "\t" + CampaignSave.Version, CampaignSave.Header + "\t13");

        [Test]
        public void NewRunsReachEveryEarlyPairAndKeepTheirPreviewAfterReload()
        {
            var routes = new System.Collections.Generic.HashSet<string>();
            for (ulong seed = 1; seed <= 32; seed++)
            {
                var c = Camp(seed);
                string preview = Route(c);
                c.StartRun();
                Assert.AreEqual(preview, Route(c));
                Assert.AreEqual(c.RunRoute[0].Id, c.Target.Id);
                var read = CampaignSave.Read(CampaignSave.Write(c));
                Assert.AreEqual(preview, Route(read));
                Assert.IsEmpty(read.LoadNote);
                routes.Add(preview);
            }
            CollectionAssert.AreEquivalent(new[] { "raiders/skullpeak", "raiders/thundercoast", "stoneveil/skullpeak", "stoneveil/thundercoast" }, routes);
        }

        [TestCase(1UL, "raiders", "raiders/thundercoast")]
        [TestCase(2UL, "stoneveil", "stoneveil/skullpeak")]
        public void LegacyRunKeepsFutureTiersAndItsActiveFightAcrossMigration(ulong seed, string first, string expected)
        {
            var c = Camp(seed); c.ChooseTribe(first);
            c = CampaignSave.Read(Legacy(CampaignSave.Write(c)));
            Assert.AreEqual(expected, Route(c));
            var fight = c.StartEncounter(0);
            for (int tick = 0; tick < 15 && !fight.Over; tick++) fight.Tick();
            string snapshot = fight.Sim.OrderedStateSnapshot;
            // Actual v13-shaped fight proof, followed by two v14 writes/reads.
            c = CampaignSave.Read(Legacy(CampaignSave.Write(c)));
            for (int round = 0; round < 2; round++)
            {
                Assert.IsEmpty(c.LoadNote);
                Assert.AreEqual(expected, Route(c));
                Assert.IsNotNull(c.Active);
                Assert.AreEqual(snapshot, c.Active.Sim.OrderedStateSnapshot);
                string save = CampaignSave.Write(c);
                StringAssert.Contains("routepick\t0\n", save);
                c = CampaignSave.Read(save);
            }
        }

        [Test]
        public void LegacyCampStartsUsingNewPickerAndCurrentActiveFightKeepsIt()
        {
            var c = CampaignSave.Read(Legacy(CampaignSave.Write(Camp(1, 2))));
            string preview = Route(c);
            c.StartRun();
            StringAssert.Contains("routepick\t1\n", CampaignSave.Write(c));
            Assert.AreEqual(preview, Route(c));
            var fight = c.StartEncounter(0);
            for (int tick = 0; tick < 15 && !fight.Over; tick++) fight.Tick();
            var read = CampaignSave.Read(CampaignSave.Write(c));
            Assert.IsEmpty(read.LoadNote);
            Assert.AreEqual(preview, Route(read));
            Assert.AreEqual(fight.Sim.OrderedStateSnapshot, read.Active.Sim.OrderedStateSnapshot);
        }

        [TestCase("missing")]
        [TestCase("unknown")]
        [TestCase("duplicate")]
        public void BrokenCurrentPickerRecordRecoversCampInsteadOfChangingTheRoute(string mode)
        {
            var c = Camp(7); c.StartRun();
            string save = CampaignSave.Write(c);
            string replacement = mode == "missing" ? "" : mode == "unknown" ? "routepick\t99\n" : "routepick\t1\nroutepick\t1\n";
            var read = CampaignSave.Read(save.Replace("routepick\t1\n", replacement));
            Assert.AreEqual(CampaignStage.ChooseTribe, read.Stage);
            Assert.AreEqual(CampaignSave.RunLostNote, read.LoadNote);
            Assert.AreEqual(c.Materials, read.Materials);
            Assert.AreEqual(c.RunsStarted, read.RunsStarted);
        }
    }
}
