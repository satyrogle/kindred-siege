using System.Collections.Generic;
using System.Linq;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using NUnit.Framework;

namespace KindredSiege.Sim.Tests
{
    /// Generated names (Jacob, 11 Oct 2026, answer 27 to Codex: "a random name generator", chiefs included;
    /// his four calls on 12 Oct 2026). Each people has its own sound, every chief a deadpan title and one name
    /// all campaign, a name given twice makes "the Second", enemies keep their names when they join you, and
    /// the hunters' company blades stay faceless while their captain is named.
    public class NameTests
    {
        private static readonly string B = CampaignTribes.BloodridgeId;
        private static readonly string S = CampaignTribes.SkullpeakId;
        private static readonly string C = CampaignTribes.ClarthId;

        private static Campaign Ready(ulong seed, CampaignTuning tuning = null)
        {
            Campaign c = Campaign.New(seed, tuning);
            Assert.IsTrue(CampaignPilot.PlayTutorial(c, PilotStyle.Reference, seed, 10), "the tutorial was never won");
            return c;
        }

        private static IEnumerable<UnitSpec> Everyone(CampaignFight fight)
        {
            return fight.Setup.Side(0).Concat(fight.Setup.Side(1));
        }

        private static void AllNamedOnce(CampaignFight fight)
        {
            var seen = new HashSet<string>();
            foreach (UnitSpec u in Everyone(fight))
            {
                string name = fight.NameOf(u.Id);
                if (u.Id == Campaign.LeaderId)
                {
                    Assert.IsEmpty(name, "your leader is a Pal, named elsewhere");
                    continue;
                }
                Assert.IsNotEmpty(name, u.Id + " has no name");
                Assert.IsTrue(seen.Add(name), name + " twice in one fight");
            }
        }

        [Test]
        public void EveryFighterOnBothSidesHasANameAndNoneRepeatInAFight()
        {
            for (ulong seed = 900; seed < 920; seed++)
            {
                AllNamedOnce(Campaign.New(seed).StartTutorial());
                Campaign c = Ready(seed);
                c.StartRun();
                for (int i = 0; i < c.Offers.Count; i++) AllNamedOnce(CampaignSave.Read(CampaignSave.Write(c)).StartEncounter(i));
            }
        }

        [Test]
        public void NobodyYouMeetOnOneTribesPathSharesAName()
        {
            // CampaignNames keeps eight names a fight from a tribe's 32, so the first four fights on a path share none.
            const int NamedFightsPerPath = 4;
            int longest = 0;
            for (ulong seed = 1060; seed < 1080; seed++)
            {
                Campaign c = Ready(seed);
                c.StartRun();
                var picker = new CampaignPilot(PilotStyle.Reference, seed);
                var met = new Dictionary<string, HashSet<string>>();
                var fights = new Dictionary<string, int>();
                for (ulong n = 0; c.Stage == CampaignStage.InRun; n++)
                {
                    CampaignFight fight = c.StartEncounter(picker.PickOffer(c));
                    string tribe = fight.Encounter.Tribe.Id;
                    // Hunter fights use the company names and are not part of this tribe's naming sequence; nor are beasts
                    // (rules 39 puts a rolled one on a road node), whose parts keep their names when tried again.
                    if (fight.Encounter.Tribe.Id == AlphaHunt.Id || fight.Encounter.Kind == EncounterKind.Beast)
                    {
                        CampaignPilot.PlayFight(c, fight, new CampaignPilot(PilotStyle.Reference, seed * 31UL + n));
                        continue;
                    }
                    if (!met.TryGetValue(tribe, out HashSet<string> names)) met[tribe] = names = new HashSet<string>();
                    fights.TryGetValue(tribe, out int count);
                    fights[tribe] = ++count;
                    longest = System.Math.Max(longest, count);
                    foreach (UnitSpec u in fight.Setup.Side(1))
                    {
                        string name = fight.NameOf(u.Id);
                        if (u.Role == UnitRole.Chief && fight.Encounter.Kind == EncounterKind.Chief)
                        {
                            Assert.AreEqual(c.ChiefOf(tribe).Name, name, "a retry meets the same chief");
                            continue;
                        }
                        if (name.Length > 0 && count <= NamedFightsPerPath)
                            Assert.IsTrue(names.Add(name), name + " came twice on the " + tribe + " path");
                    }
                    CampaignPilot.PlayFight(c, fight, new CampaignPilot(PilotStyle.Reference, seed * 31UL + n));
                }
            }
            Assert.GreaterOrEqual(longest, new CampaignTuning().PathFights + 1, "no path was walked to its chief, so this proved little");
        }

        [Test]
        public void ARunningFightKeepsItsNamesWhenTheGameIsClosed()
        {
            Campaign c = Ready(921);
            c.StartRun();
            CampaignFight fight = c.StartEncounter(0);
            for (int t = 0; t < 40 && !fight.Over; t++) fight.Tick();
            string saved = CampaignSave.Write(c);
            Campaign read = CampaignSave.Read(saved);
            Assert.IsNotNull(read.Active, read.LoadNote);
            foreach (UnitSpec u in Everyone(fight)) Assert.AreEqual(fight.NameOf(u.Id), read.Active.NameOf(u.Id), u.Id);
            CollectionAssert.AreEqual(c.Reserve.Select(f => f.Name).ToList(), read.Reserve.Select(f => f.Name).ToList());
            Assert.AreEqual(saved, CampaignSave.Write(read));
        }

        [Test]
        public void TutorialSurvivorsKeepTheirNames()
        {
            for (ulong seed = 922; seed < 952; seed++)
            {
                Campaign c = Campaign.New(seed);
                CampaignFight fight = c.StartTutorial();
                List<string> names = fight.Squad.Select(f => f.Name).ToList();
                Assert.AreEqual(names.Count, names.Distinct().Count(), "the Clarth you command all have their own names");
                EncounterResult r = CampaignPilot.PlayFight(c, fight, new CampaignPilot(PilotStyle.Reference, seed));
                if (!r.Won) continue;
                List<string> standing = fight.Squad.Where(f => fight.Sim.FindById(f.Id).Alive).Select(f => f.Name).ToList();
                CollectionAssert.AreEqual(standing, r.JoinedFighters.Select(f => f.Name).ToList());
                return;
            }
            Assert.Fail("the reference pilot never won the tutorial in thirty tries");
        }

        [Test]
        public void ARecruitKeepsTheNameTheyHadAsAnEnemy()
        {
            for (ulong seed = 952; seed < 992; seed++)
            {
                Campaign c = Ready(seed);
                c.StartRun();
                var picker = new CampaignPilot(PilotStyle.Reference, seed);
                for (ulong n = 0; c.Stage == CampaignStage.InRun && c.Target == CampaignTribes.Bloodridge &&
                    c.Offers[0].Kind != EncounterKind.Chief; n++)
                    CampaignPilot.PlayFight(c, c.StartEncounter(picker.PickOffer(c)), new CampaignPilot(PilotStyle.Reference, seed * 31UL + n));
                if (c.Stage != CampaignStage.InRun || c.Target != CampaignTribes.Bloodridge) continue;

                CampaignFight chief = c.StartEncounter(0);
                UnitSpec leader = chief.Setup.Side(1).First(u => u.Role == UnitRole.Chief);
                Assert.AreEqual(c.ChiefOf(B).Name, chief.NameOf(leader.Id), "the chief fights under their own name");
                EncounterResult r = CampaignPilot.PlayFight(c, chief, new CampaignPilot(PilotStyle.Reference, seed));
                if (!r.Won || !r.ChiefCaptured) continue;
                // Since 13 Oct 2026 his whole band joins on capture, fallen ones too, each under their own name. A reserve
                // of his (20 Oct 2026) counts once it came into the fight.
                List<string> band = chief.Setup.Side(1).Where(u => u.Role == UnitRole.Fighter && !chief.Sim.FindById(u.Id).Benched)
                    .Select(u => chief.NameOf(u.Id)).ToList();
                // A raid chief fought alone (rules 10): nobody fought beside him, so his tribe joins under fresh names.
                if (chief.Setup.Fight.RaidChief && band.Count == 0)
                {
                    Assert.AreEqual(chief.Encounter.EnemyFighters, r.JoinedFighters.Count);
                    Assert.IsTrue(r.JoinedFighters.All(f => !string.IsNullOrEmpty(f.Name)));
                    Assert.AreEqual(r.JoinedFighters.Count, r.JoinedFighters.Select(f => f.Name).Distinct().Count(), "nobody shares a name");
                    return;
                }
                CollectionAssert.AreEqual(band, r.JoinedFighters.Select(f => CampaignNames.BaseOf(f.Name)).ToList());
                return;
            }
            Assert.Fail("the reference pilot never captured the Bloodridge chief in forty campaigns");
        }

        [Test]
        public void AChiefIsTheSamePersonAllCampaign()
        {
            Campaign c = Ready(992);
            CampaignChief chief = c.ChiefOf(B);
            Assert.IsNotNull(chief);
            Assert.IsNotEmpty(chief.Name);
            Assert.IsNotEmpty(chief.Title);
            Assert.AreEqual(chief.Name + ", " + chief.Title, chief.FullName);
            CampaignPilot.PlayRun(c, B, PilotStyle.Reference, 992);
            Assert.AreEqual(chief.FullName, c.ChiefOf(B).FullName, "after a run");
            Assert.AreEqual(chief.FullName, CampaignSave.Read(CampaignSave.Write(c)).ChiefOf(B).FullName, "after closing the game");
            Assert.AreEqual(chief.FullName, Campaign.New(992).ChiefOf(B).FullName, "the campaign's seed decides who it is");
            Assert.AreNotEqual(c.ChiefOf(B).Name, c.ChiefOf(S).Name);

            var met = new HashSet<string>();
            for (ulong seed = 0; seed < 40; seed++) met.Add(Campaign.New(seed).ChiefOf(B).FullName);
            Assert.Greater(met.Count, 10, "other campaigns meet other chiefs");
        }

        [Test]
        public void TheHunterCaptainIsNamedAndTheCompanyBladesStayFaceless()
        {
            Assert.IsFalse(CampaignNames.Named(AlphaHunt.Id));
            Assert.IsNull(Campaign.New(1).ChiefOf(AlphaHunt.Id));
            Assert.IsNull(Campaign.New(1).ChiefOf(CampaignTribes.CultId));
            for (ulong seed = 993; seed < 1053; seed++)
            {
                Campaign c = Campaign.New(seed, new CampaignTuning { EnemyScaleTier1 = 50, EnemyScaleTier2 = 50 });
                if (!CampaignPilot.PlayTutorial(c, PilotStyle.Reference, seed, 10)) continue;
                c.StartRun();
                var picker = new CampaignPilot(PilotStyle.Reference, seed);
                for (ulong n = 0; c.Stage == CampaignStage.InRun && c.Target != AlphaHunt.Tribe; n++)
                    CampaignPilot.PlayFight(c, c.StartEncounter(picker.PickOffer(c)), new CampaignPilot(PilotStyle.Reference, seed * 31UL + n));
                if (c.Target != AlphaHunt.Tribe) continue;
                CampaignFight hunt = c.StartEncounter(0);
                Assert.IsNotNull(hunt.Encounter.Hunter);
                foreach (UnitSpec u in hunt.Setup.Side(1))
                    if (u.Role == UnitRole.Chief) Assert.AreEqual(hunt.Encounter.Hunter.Name, hunt.NameOf(u.Id));
                    else Assert.IsEmpty(hunt.NameOf(u.Id), u.Id);
                foreach (CampaignFighter f in hunt.Squad) Assert.AreEqual(f.Name, hunt.NameOf(f.Id), "yours keep theirs");
                return;
            }
            Assert.Fail("the reference pilot never reached the hunt in sixty campaigns");
        }

        [Test]
        public void ANameGivenTwiceIsTheSecond()
        {
            Assert.AreEqual("Nakht", CampaignNames.WithOrdinal("Nakht", 1));
            Assert.AreEqual("Nakht the Second", CampaignNames.WithOrdinal("Nakht", 2));
            Assert.AreEqual("Nakht the Twelfth", CampaignNames.WithOrdinal("Nakht", 12));
            Assert.AreEqual("Nakht", CampaignNames.BaseOf("Nakht the Second"));
            Assert.AreEqual("Mostly Ash", CampaignNames.BaseOf("Mostly Ash the Third"));
            Assert.AreEqual("Nakht", CampaignNames.BaseOf(CampaignNames.WithOrdinal("Nakht", 13)));
            Assert.AreEqual("Nakht", CampaignNames.BaseOf("Nakht"));

            // Run after run the Clarth send fresh fighters and beaten tribes join, so names come round again:
            // the k-th of yours to be given a name is its k-th, and nobody shares a name with anyone beside them.
            bool repeated = false;
            for (ulong seed = 1053; seed < 1056; seed++)
            {
                Campaign c = Ready(seed);
                var given = new Dictionary<string, int>();
                var counted = new HashSet<string>();
                for (int run = 0; run < 10; run++)
                {
                    c.StartRun();
                    Count(c, given, counted, ref repeated);
                    var picker = new CampaignPilot(PilotStyle.Reference, seed + (ulong)run);
                    for (ulong n = 0; c.Stage == CampaignStage.InRun; n++)
                    {
                        CampaignPilot.PlayFight(c, c.StartEncounter(picker.PickOffer(c)),
                            new CampaignPilot(PilotStyle.Reference, seed * 97UL + (ulong)run * 13UL + n));
                        Count(c, given, counted, ref repeated);
                    }
                }
            }
            Assert.IsTrue(repeated, "ten runs never gave a name twice, so this proved nothing");
        }

        private static void Count(Campaign c, Dictionary<string, int> given, HashSet<string> counted, ref bool repeated)
        {
            Assert.AreEqual(c.Reserve.Count, c.Reserve.Select(f => f.Name).Distinct().Count(), "two of yours share a name");
            foreach (CampaignFighter f in c.Reserve)
            {
                if (!counted.Add(f.Id)) continue;
                string name = CampaignNames.BaseOf(f.Name);
                given.TryGetValue(name, out int n);
                given[name] = ++n;
                Assert.AreEqual(CampaignNames.WithOrdinal(name, n), f.Name);
                if (n > 1) repeated = true;
            }
        }

        [Test]
        public void ASaveFromBeforeNamesOpensWithNamesThatStay()
        {
            Campaign c = Ready(1056);
            c.StartRun();
            string now = CampaignSave.Write(c);
            string old = string.Join("\n", now.Split('\n')
                    .Where(l => !l.StartsWith("named\t") && !l.StartsWith("clearroad\t") && !l.StartsWith("palroad\t") && !l.StartsWith("beastroll\t"))
                    .Select(l => l.StartsWith("fighter\t") ? string.Join("\t", l.Split('\t').Take(4)) : l))
                .Replace(CampaignSave.Header + "\t" + CampaignSave.Version, CampaignSave.Header + "\t5");
            Assert.AreNotEqual(now, old);
            Campaign first = CampaignSave.Read(old);
            Assert.IsEmpty(first.LoadNote);
            Assert.AreEqual(CampaignStage.InRun, first.Stage);
            List<string> names = first.Reserve.Select(f => f.Name).ToList();
            Assert.IsTrue(names.All(n => n.Length > 0), "everyone gets a name");
            Assert.AreEqual(names.Count, names.Distinct().Count(), "and nobody shares one");
            CollectionAssert.AreEqual(names, CampaignSave.Read(old).Reserve.Select(f => f.Name).ToList(), "the same names each time it opens");
            CollectionAssert.AreEqual(names, CampaignSave.Read(CampaignSave.Write(first)).Reserve.Select(f => f.Name).ToList(),
                "and they stay once it is saved again");
        }

        [Test]
        public void EachPeopleHasItsOwnNames()
        {
            var owner = new Dictionary<string, string>();
            foreach (string tribe in new[] { B, C, S })
                for (int run = 0; run < 75; run++)
                    for (int step = 0; step < 4; step++)
                        foreach (string name in CampaignNames.ForFight(7, run, step, tribe, 8))
                        {
                            Assert.IsNotEmpty(name);
                            Assert.IsFalse(name.Contains(" the "), name + " reads like a repeat");
                            Assert.IsFalse(owner.TryGetValue(name, out string other) && other != tribe, name + " belongs to " + other + " and " + tribe);
                            owner[name] = tribe;
                        }
            Assert.Greater(owner.Count, 90, "about thirty names a people, and a few cheeky ones");
            CollectionAssert.AreEqual(CampaignNames.ForFight(7, 3, 1, B, 6), CampaignNames.ForFight(7, 3, 1, B, 6));
            CollectionAssert.AreEqual(new[] { "", "", "" }, CampaignNames.ForFight(7, 3, 1, AlphaHunt.Id, 3));
        }
    }
}
