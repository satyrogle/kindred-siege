using System.Collections.Generic;
using System.Linq;
using KindredSiege.Sim;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using NUnit.Framework;

namespace KindredSiege.Sim.Tests
{
    /// The leaders, campaign rules 43 (BattleSimulation.Leaders.cs): their leader can't be hit while his fighters stand and
    /// leaves when they are gone, a chief in his own fight stays, the champion's fall breaks his band, a Warden holds the
    /// middle, the road's last row is the champion's, and the version boundary.
    public class Leaders43Tests
    {
        static UnitSpec U(string id, UnitClass c, Row row = Row.Front, int health = 30000, UnitRole role = UnitRole.Fighter, int power = 100)
        {
            return new UnitSpec(id, c, row, role, power, health);
        }

        static BattleSimulation Fight(FightRules rules, UnitSpec[] ours, UnitSpec[] theirs)
        {
            return new BattleSimulation(new BattleSetup("leaders43", 7, ours, theirs, new TacticsProfile(), new TacticsProfile(),
                BattleSetup.DefaultTickLimit, 20, NerveRules.Off, 10, 100, SideRules.None, SideRules.None, rules));
        }

        static List<BattleEvent> Play(BattleSimulation s, int ticks)
        {
            var ses = new SimSession(s);
            for (int i = 0; i < ticks && !s.IsFinished; i++) ses.Tick();
            return s.Events.ToList();
        }

        static UnitSpec[] Ours() => new[] { U("a", UnitClass.Skirmisher), U("b", UnitClass.Warden), U("c", UnitClass.Marksman, Row.Back) };

        static bool OursHit(BattleEvent e, string id) => e.Kind == BattleEventKind.Hit && e.ActorSide == 0 && e.TargetId.EndsWith(id);

        static FightRules Leaders => FightRules.Off.WithLayers().WithTable().WithLeaders();

        [Test]
        public void TheirLeaderIsNeverHitWhileAFighterOfHisStands()
        {
            UnitSpec[] theirs = { U("guard1", UnitClass.Warden), U("boss", UnitClass.Warden, Row.Back, 30000, UnitRole.Chief, 150) };
            BattleSimulation s = Fight(Leaders, Ours(), theirs);
            Assert.IsTrue(s.LeaderProtected(s.Units.First(u => u.Id.EndsWith("boss"))));
            List<BattleEvent> ev = Play(s, 1800);
            Assert.IsFalse(ev.Any(e => OursHit(e, "boss")), "nothing of ours lands on him");
            Assert.IsTrue(ev.Any(e => OursHit(e, "guard1")), "we fight his fighter");
            // Before rules 43 our Skirmisher went for the back row, and he was hit.
            Assert.IsTrue(Play(Fight(FightRules.Off.WithLayers().WithTable(), Ours(), theirs), 1800).Any(e => OursHit(e, "boss")));
        }

        [Test]
        public void WhenHisLastFighterFallsHeLeavesAndTheFightIsWon()
        {
            UnitSpec[] theirs = { U("guard1", UnitClass.Warden, Row.Front, 20), U("boss", UnitClass.Warden, Row.Back, 30000, UnitRole.Chief, 150) };
            BattleSimulation s = Fight(Leaders, Ours(), theirs);
            List<BattleEvent> ev = Play(s, 3600);
            Assert.IsTrue(s.IsFinished);
            Assert.AreEqual(0, s.Winner);
            BattleEvent broke = ev.Single(e => e.Kind == BattleEventKind.Break);
            Assert.AreEqual(1, broke.ActorSide);
            Assert.AreEqual(LeaderRules.RetreatDetail, broke.Detail);
            Assert.AreEqual(FightEnd.NerveBreak, s.Ending.How);
            Assert.IsTrue(s.Units.First(u => u.Id.EndsWith("boss")).Alive, "he leaves alive");
            Assert.IsFalse(ev.Any(e => OursHit(e, "boss")));
        }

        [Test]
        public void InHisOwnChiefFightHeFightsOnAloneOnceHisBandIsDown()
        {
            UnitSpec[] theirs = { U("guard1", UnitClass.Warden, Row.Front, 20), U("boss", UnitClass.Warden, Row.Back, 30000, UnitRole.Chief, 150) };
            BattleSimulation s = Fight(FightRules.CampaignChief.WithLayers().WithLeaders(), Ours(), theirs);
            List<BattleEvent> ev = Play(s, 3600);
            long fell = ev.First(e => e.Kind == BattleEventKind.Kill && e.TargetId.EndsWith("guard1")).Tick;
            Assert.IsFalse(ev.Any(e => e.Kind == BattleEventKind.Break && e.Detail == LeaderRules.RetreatDetail), "he stays");
            Assert.IsFalse(ev.Any(e => OursHit(e, "boss") && e.Tick < fell), "not hit while his band stands");
            Assert.IsTrue(ev.Any(e => OursHit(e, "boss") && e.Tick >= fell), "alone, he can be hit");
        }

        [Test]
        public void TheChampionCanBeHitAndHisFallBreaksHisBand()
        {
            FightRules rules = FightRules.Off.WithLayers().WithTable().WithChampionFight();
            Assert.IsTrue(rules.Leaders, "the champion's fight brings the leaders");
            // No Medic: hers would heal him faster than two Wardens wear him down.
            UnitSpec[] theirs =
            {
                U("champ", UnitClass.Skirmisher, Row.Front, 60, UnitRole.Chief, 150), U("archer", UnitClass.Marksman, Row.Back)
            };
            UnitSpec[] ours = { U("a", UnitClass.Warden), U("b", UnitClass.Warden), U("c", UnitClass.Skirmisher) };
            BattleSimulation s = Fight(rules, ours, theirs);
            Assert.IsFalse(s.LeaderProtected(s.Units.First(u => u.Id.EndsWith("champ"))), "he is no shielded leader");
            List<BattleEvent> ev = Play(s, 3600);
            Assert.IsTrue(ev.Any(e => OursHit(e, "champ")));
            BattleEvent broke = ev.Single(e => e.Kind == BattleEventKind.Break);
            Assert.AreEqual(1, broke.ActorSide);
            Assert.AreEqual(LeaderRules.ChampionDetail, broke.Detail);
            Assert.AreEqual(0, s.Winner);
            Assert.IsTrue(s.Units.Where(u => u.Side == 1 && !u.Id.EndsWith("champ")).All(u => u.Alive), "his band runs");
        }

        static BattleSimulation Brawl(FightRules rules)
        {
            return Fight(rules,
                new[] { U("a", UnitClass.Skirmisher), U("b", UnitClass.Warden), U("c", UnitClass.Marksman, Row.Back),
                    U("d", UnitClass.Trapper, Row.Back), U("e", UnitClass.Medic, Row.Back) },
                new[] { U("v", UnitClass.Skirmisher), U("w", UnitClass.Warden), U("x", UnitClass.Marksman, Row.Back),
                    U("y", UnitClass.Trapper, Row.Back), U("z", UnitClass.Medic, Row.Back) });
        }

        [Test]
        public void AWardenHoldsTheMiddleByHisDefence()
        {
            BattleSimulation s = Brawl(Leaders);
            Assert.AreEqual(AttackTable.VeryHigh * LeaderRules.MiddlePercent / 100, s.MiddleChance(s.Units.First(u => u.Class == UnitClass.Warden)));
            List<BattleEvent> ev = Play(s, 3600);
            List<BattleEvent> holds = ev.Where(e => e.Kind == BattleEventKind.Defence && e.Detail == LeaderRules.MiddleDetail).ToList();
            Assert.IsNotEmpty(holds);
            Assert.IsTrue(holds.All(e => e.ActorClass == (int)UnitClass.Warden && e.TargetClass != (int)UnitClass.Warden));
            foreach (BattleEvent h in holds)
                Assert.IsTrue(ev.Any(e => e.Tick == h.Tick && e.Kind == BattleEventKind.Hit && e.TargetId == h.ActorId),
                    "the blow lands on his shield, tick " + h.Tick);
            Assert.IsFalse(Play(Brawl(FightRules.Off.WithLayers().WithTable()), 3600).Any(e => e.Detail == LeaderRules.MiddleDetail),
                "before rules 43 nobody holds the middle");
        }

        [Test]
        public void ANewCampaignFightPlaysRules43()
        {
            Assert.IsFalse(Campaign.New(3, new CampaignTuning { RulesCap = 42 }).StartTutorial().Setup.Fight.Leaders);
            Assert.IsTrue(Campaign.New(3, new CampaignTuning()).StartTutorial().Setup.Fight.Leaders);
            Assert.AreEqual(43, Campaign.LeadersRules);
            Assert.GreaterOrEqual(Campaign.RulesVersion, 43);
        }

        [Test]
        public void TheRoadsLastRowIsTheChampions()
        {
            foreach (int cap in new[] { 42, 43 })
            {
                const ulong seed = 4610;
                var c = Campaign.New(seed, new CampaignTuning { RulesCap = cap });
                Assert.IsTrue(CampaignPilot.PlayTutorial(c, PilotStyle.Reference, seed, 10), "the tutorial was never won");
                c.ChooseTribe(CampaignTribes.BloodridgeId);
                List<RoadNode> fights = c.Road.All.Where(n => n.Kind == RoadNodeKind.Fight).ToList();
                Assert.IsNotEmpty(fights);
                foreach (RoadNode n in fights)
                {
                    bool expected = cap >= 43 && n.Row == c.Road.Rows - 1 && n.Beast == BeastKit.None &&
                        (n.Fight == EncounterKind.WarParty || n.Fight == EncounterKind.HoldTheLine || n.Fight == EncounterKind.Trial);
                    Assert.AreEqual(expected, c.ChampionOn(n), "row " + n.Row + " lane " + n.Lane + " " + n.Fight + " at rules " + cap);
                }
                if (cap >= 43) Assert.IsTrue(fights.Any(c.ChampionOn), "the last row has a champion's fight");
            }
        }

        [Test]
        public void AllBuildersPreserveTheLeadersSwitches()
        {
            foreach (var input in new[] { FightRules.Off.WithLeaders(), FightRules.Campaign.WithTable().WithChampionFight() })
            foreach (var m in typeof(FightRules).GetMethods().Where(m => m.Name.StartsWith("With") && m.ReturnType == typeof(FightRules)))
            {
                var r = (FightRules)m.Invoke(input, m.GetParameters().Length == 0 ? new object[0] : new object[] { 4 });
                Assert.IsTrue(r.Leaders, m.Name);
                Assert.AreEqual(input.ChampionFight || m.Name == "WithChampionFight", r.ChampionFight, m.Name);
            }
        }

        [Test]
        public void SavesAndReplaysKeepTheirVersionAcrossTheBoundary()
        {
            foreach (int version in new[] { 42, 43 })
            {
                var c = Campaign.New(11, new CampaignTuning { RulesCap = version });
                var f = c.StartTutorial();
                for (int i = 0; i < 60; i++) f.Tick();
                var loaded = CampaignSave.Read(CampaignSave.Write(c));
                Assert.IsEmpty(loaded.LoadNote);
                Assert.AreEqual(f.Session.Checksum, loaded.Active.Session.Checksum);
                Assert.AreEqual(version == 43, loaded.Active.Setup.Fight.Leaders);
                CampaignPilot.PlayFight(c, f, new CampaignPilot(PilotStyle.Reference, 11));
                loaded = CampaignSave.Read(CampaignSave.Write(c));
                Assert.IsEmpty(loaded.LoadNote);
                Assert.IsTrue(loaded.Archive.TryReplay(f.Setup.FightId, out BattleSimulation replay, out string why), why);
                Assert.AreEqual(version == 43, replay.Setup.Fight.Leaders);
                Assert.AreEqual(f.Session.Checksum, StateChecksum.Compute(replay.OrderedStateSnapshot));
            }
        }
    }
}
