using System.Collections.Generic;
using System.Linq;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using NUnit.Framework;

namespace KindredSiege.Sim.Tests
{
    /// A replay names everyone the way the fight did (Jacob's go, 12 Oct 2026): each recorded fight keeps its
    /// names, in the save from version 7. Fights recorded before then replay without names.
    public class ReplayNameTests
    {
        private static Campaign Ready(ulong seed, CampaignTuning tuning = null)
        {
            Campaign c = Campaign.New(seed, tuning);
            Assert.IsTrue(CampaignPilot.PlayTutorial(c, PilotStyle.Reference, seed, 10), "the tutorial was never won");
            return c;
        }

        // Plays a fight on offer and returns everyone's names the way the live fight had them.
        private static Dictionary<string, string> PlayOne(Campaign c, ulong seed, int offer, out string fightId)
        {
            CampaignFight fight = c.StartEncounter(offer);
            var names = new Dictionary<string, string>();
            foreach (UnitSpec u in fight.Setup.Side(0).Concat(fight.Setup.Side(1))) names[u.Id] = fight.NameOf(u.Id);
            fightId = fight.Setup.FightId;
            CampaignPilot.PlayFight(c, fight, new CampaignPilot(PilotStyle.Reference, seed));
            return names;
        }

        [Test]
        public void AReplayNamesEveryoneTheWayTheFightDid()
        {
            Campaign c = Ready(1201);
            c.StartRun();
            int offer = c.Offers.ToList().FindIndex(e => e.Kind != EncounterKind.Duel);
            Dictionary<string, string> live = PlayOne(c, 1201, offer, out string fightId);
            Assert.Greater(live.Values.Count(n => n.Length > 0), 3, "the fight had names to keep");
            foreach (Campaign source in new[] { c, CampaignSave.Read(CampaignSave.Write(c)) })
            {
                Assert.IsTrue(FightPlayback.TryOpen(source.Archive, fightId, out FightPlayback p, out string why), why);
                foreach (KeyValuePair<string, string> unit in live) Assert.AreEqual(unit.Value, p.NameOf(unit.Key), unit.Key);
                Assert.IsTrue(p.Seek(p.Length), p.Failure);
                Assert.IsTrue(p.Matched);
            }
        }

        [Test]
        public void TheChiefAndHunterCaptainKeepTheirNamesInReplay()
        {
            for (ulong seed = 1202; seed < 1262; seed++)
            {
                Campaign c = Campaign.New(seed, new CampaignTuning { EnemyScaleTier1 = 50, EnemyScaleTier2 = 50 });
                if (!CampaignPilot.PlayTutorial(c, PilotStyle.Reference, seed, 10)) continue;
                c.ChooseTribe(CampaignTribes.BloodridgeId);
                var picker = new CampaignPilot(PilotStyle.Reference, seed);
                for (ulong n = 0; c.Stage == CampaignStage.InRun && c.Target != AlphaHunt.Tribe; n++)
                    CampaignPilot.PlayFight(c, c.StartEncounter(picker.PickOffer(c)), new CampaignPilot(PilotStyle.Reference, seed * 31UL + n));
                if (c.Target != AlphaHunt.Tribe) continue;
                string captainName = c.Offers[0].Hunter.Name;
                PlayOne(c, seed, 0, out string huntId);
                c = CampaignSave.Read(CampaignSave.Write(c));

                Assert.IsTrue(FightPlayback.TryOpen(c.Archive, huntId, out FightPlayback hunt, out string why), why);
                foreach (UnitSpec u in hunt.Fight.Setup.Side(1))
                    if (u.Role == UnitRole.Chief) Assert.AreEqual(captainName, hunt.NameOf(u.Id));
                    else Assert.IsEmpty(hunt.NameOf(u.Id), u.Id + " is an unnamed company blade");
                Assert.IsTrue(hunt.Seek(hunt.Length), hunt.Failure);
                Assert.IsTrue(hunt.Matched);
                foreach (UnitSpec u in hunt.Fight.Setup.Side(0))
                    if (u.Id != Campaign.LeaderId) Assert.IsNotEmpty(hunt.NameOf(u.Id), u.Id + " is yours and has a name");

                ArchivedFight chiefFight = c.Archive.Fights.First(f => f.FightId.EndsWith("." + CampaignTribes.BloodridgeId + ".chief"));
                UnitSpec chief = chiefFight.Setup.Side(1).First(u => u.Role == UnitRole.Chief);
                Assert.AreEqual(c.ChiefOf(CampaignTribes.BloodridgeId).Name, chiefFight.NameOf(chief.Id));
                return;
            }
            Assert.Fail("the reference pilot never reached the hunt in sixty campaigns");
        }

        [Test]
        public void FightsFromOlderSavesReplayWithoutNames()
        {
            Campaign c = Ready(1262);
            c.StartRun();
            PlayOne(c, 1262, 0, out string fightId);
            string now = CampaignSave.Write(c);
            Assert.IsTrue(now.Contains("\nnames\t"));
            string old = string.Join("\n", now.Split('\n').Where(l => !l.StartsWith("names\t") && !l.StartsWith("clearroad\t") && !l.StartsWith("palroad\t") && !l.StartsWith("beastroll\t") && !l.StartsWith("favouroffer\t")))
                .Replace(CampaignSave.Header + "\t" + CampaignSave.Version, CampaignSave.Header + "\t6");
            Campaign read = CampaignSave.Read(old);
            Assert.IsEmpty(read.LoadNote);
            Assert.IsTrue(FightPlayback.TryOpen(read.Archive, fightId, out FightPlayback p, out string why), why);
            Assert.IsTrue(p.Fight.Setup.Side(1).All(u => p.NameOf(u.Id).Length == 0), "nobody was named back then");
            Assert.IsTrue(p.Seek(p.Length), p.Failure);
            Assert.IsTrue(p.Matched);
            Assert.IsFalse(CampaignSave.Write(read).Contains("\nnames\t"));
        }

        [Test]
        public void ABrokenNamesRecordIsRefused()
        {
            Campaign c = Ready(1263);
            string saved = CampaignSave.Write(c);
            string line = saved.Split('\n').First(l => l.StartsWith("names\t"));
            string fightId = line.Split('\t')[1];
            foreach (string bad in new[] { saved.Replace(line, "names\t" + fightId + "\tonly.an.id"), saved.Replace(line, line + "\n" + line),
                saved.Replace(line, "names\t" + fightId + "\t\tNakht") })
                Assert.IsFalse(CampaignSave.TryRead(bad, null, out _, out string why), "read: " + bad.Length);

            // Names for a fight the archive no longer keeps are skipped.
            Campaign trimmed = CampaignSave.Read(string.Join("\n", saved.Split('\n').Where(l => !l.StartsWith("archive\t"))));
            Assert.AreEqual(0, trimmed.Archive.Fights.Count);
        }
    }
}
