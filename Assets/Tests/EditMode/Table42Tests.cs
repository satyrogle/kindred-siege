using System.Collections.Generic;
using System.Linq;
using KindredSiege.Sim;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using NUnit.Framework;

namespace KindredSiege.Sim.Tests
{
    /// The attack table, campaign rules 42 (BattleSimulation.Table.cs): the six stats Jacob agreed on 7 Nov 2026, one roll a
    /// blow for a dodge, a parry, a block, a crit or a hit, and the version boundary.
    public class Table42Tests
    {
        static UnitSpec U(string id, UnitClass c, Row row = Row.Front) => new UnitSpec(id, c, row, UnitRole.Fighter, 100, 30000);

        // Every class on both sides, nobody falls in the time played.
        static BattleSimulation Brawl(FightRules rules)
        {
            return new BattleSimulation(new BattleSetup("table42", 7,
                new[] { U("a", UnitClass.Skirmisher), U("b", UnitClass.Warden), U("c", UnitClass.Marksman, Row.Back),
                    U("d", UnitClass.Trapper, Row.Back), U("e", UnitClass.Medic, Row.Back) },
                new[] { U("v", UnitClass.Skirmisher), U("w", UnitClass.Warden), U("x", UnitClass.Marksman, Row.Back),
                    U("y", UnitClass.Trapper, Row.Back), U("z", UnitClass.Medic, Row.Back) },
                new TacticsProfile(), new TacticsProfile(), BattleSetup.DefaultTickLimit, 20, NerveRules.Off, 10, 100,
                SideRules.None, SideRules.None, rules));
        }

        static List<BattleEvent> Play(BattleSimulation s, int ticks)
        {
            var ses = new SimSession(s);
            for (int i = 0; i < ticks; i++) ses.Tick();
            return s.Events.ToList();
        }

        static bool Melee(int unitClass) => unitClass == (int)UnitClass.Warden || unitClass == (int)UnitClass.Skirmisher;

        [Test]
        public void TheSixStatsAreTheSpreadsJacobAgreed()
        {
            int vl = AttackTable.VeryLow, l = AttackTable.Low, m = AttackTable.Mid, h = AttackTable.High, vh = AttackTable.VeryHigh;
            var agreed = new Dictionary<UnitClass, int[]>
            {
                // Health, Attack, Defence, Speed, Spirit, Will
                { UnitClass.Warden, new[] { vh, l, vh, vl, l, h } },
                { UnitClass.Skirmisher, new[] { m, h, vl, vh, l, l } },
                { UnitClass.Trapper, new[] { m, m, l, h, h, m } },
                { UnitClass.Marksman, new[] { l, vh, vl, m, l, m } },
                { UnitClass.Medic, new[] { l, vl, l, m, vh, vh } },
            };
            foreach (var kv in agreed)
            {
                ClassSix s = AttackTable.Of(kv.Key);
                Assert.AreEqual(kv.Value, new[] { s.Health, s.Attack, s.Defence, s.Speed, s.Spirit, s.Will }, kv.Key.ToString());
            }
            Assert.Greater(AttackTable.Dodge(UnitClass.Warden, UnitClass.Skirmisher), AttackTable.Dodge(UnitClass.Skirmisher, UnitClass.Warden),
                "the fast Skirmisher dodges the slow Warden, not the other way round");
            Assert.AreEqual(0, AttackTable.Parry(UnitClass.Marksman, true, UnitClass.Warden, false), "nobody parries a shot");
            Assert.AreEqual(0, AttackTable.Parry(UnitClass.Skirmisher, false, UnitClass.Medic, true), "a ranged fighter never parries");
            foreach (UnitClass c in SliceClasses.All)
                Assert.AreEqual(c == UnitClass.Warden, AttackTable.Block(c) > 0, c + " blocks only with a shield");
        }

        [Test]
        public void EveryBlowIsADodgeAParryABlockACritOrAHit()
        {
            List<BattleEvent> ev = Play(Brawl(FightRules.Off.WithLayers().WithTable()), 3600);
            List<BattleEvent> misses = ev.Where(e => e.Kind == BattleEventKind.Miss).ToList();
            List<BattleEvent> struck = ev.Where(e => e.Kind == BattleEventKind.Struck).ToList();
            Assert.IsTrue(misses.All(e => e.Detail == AttackTable.DodgeDetail || e.Detail == AttackTable.ParryDetail), "no plain miss is left");
            Assert.IsTrue(misses.Any(e => e.Detail == AttackTable.DodgeDetail), "dodges");
            Assert.IsTrue(misses.Any(e => e.Detail == AttackTable.ParryDetail), "parries");
            Assert.IsTrue(misses.Where(e => e.Detail == AttackTable.ParryDetail).All(e => Melee(e.ActorClass) && Melee(e.TargetClass)),
                "only melee parries melee");
            Assert.IsTrue(struck.Any(e => e.Detail == AttackTable.BlockDetail), "blocks");
            Assert.IsTrue(struck.Any(e => e.Detail == AttackTable.CritDetail), "crits");
            Assert.IsTrue(struck.Where(e => e.Detail == AttackTable.BlockDetail).All(e => e.TargetClass == (int)UnitClass.Warden),
                "only the Warden blocks");
            Assert.IsTrue(struck.All(e => e.Amount > 0), "a block takes points off and a crit adds them");
            foreach (BattleEvent s in struck)
                Assert.IsTrue(ev.Any(e => e.Tick == s.Tick && e.Kind == BattleEventKind.Hit && e.ActorId == s.ActorId),
                    "a struck blow still lands, on tick " + s.Tick);
        }

        [Test]
        public void TheSkirmisherDodgesMoreThanTheWarden()
        {
            // The Warden is struck far more often (he covers the hurt), so the chances are compared, not the counts.
            BattleSimulation s = Brawl(FightRules.Off.WithLayers().WithTable());
            BattleUnit Find(string id) => s.Units.First(u => u.Id.EndsWith(id));
            int skirmisherDodges = s.DodgeChance(Find("w"), Find("a"));
            int wardenDodges = s.DodgeChance(Find("v"), Find("b"));
            Assert.Greater(skirmisherDodges, wardenDodges);
            Assert.AreEqual(100 - wardenDodges - s.ParryChance(Find("v"), Find("b")), s.HitChance(Find("v"), Find("b")));
        }

        [Test]
        public void AFightWithoutTheTableNeverReachesIt()
        {
            List<BattleEvent> ev = Play(Brawl(new FightRules(true, false, false, false).WithLayers()), 3600);
            Assert.IsFalse(ev.Any(e => e.Kind == BattleEventKind.Struck));
            Assert.IsTrue(ev.Where(e => e.Kind == BattleEventKind.Miss).All(e => e.Detail == FightRules.MissDetail));
            Assert.IsTrue(ev.Any(e => e.Kind == BattleEventKind.Miss), "misses still happen as before");
        }

        [Test]
        public void ANewCampaignFightPlaysRules42()
        {
            Assert.IsFalse(Campaign.New(3, new CampaignTuning { RulesCap = 41 }).StartTutorial().Setup.Fight.Table);
            Assert.IsTrue(Campaign.New(3, new CampaignTuning { RulesCap = 42 }).StartTutorial().Setup.Fight.Table);
            Assert.IsTrue(Campaign.New(3, new CampaignTuning()).StartTutorial().Setup.Fight.Table);
            Assert.GreaterOrEqual(Campaign.RulesVersion, 42);
        }

        [Test]
        public void ACampaignFightRollsTheTable()
        {
            var c = Campaign.New(5, new CampaignTuning());
            var f = c.StartTutorial();
            CampaignPilot.PlayFight(c, f, new CampaignPilot(PilotStyle.Reference, 5));
            List<BattleEvent> misses = f.Sim.Events.Where(e => e.Kind == BattleEventKind.Miss).ToList();
            Assert.IsNotEmpty(misses);
            Assert.IsTrue(misses.All(e => e.Detail == AttackTable.DodgeDetail || e.Detail == AttackTable.ParryDetail));
        }

        [Test]
        public void AllBuildersPreserveTheTableSwitch()
        {
            foreach (var input in new[] { FightRules.Off.WithTable(), FightRules.Campaign.WithFreezeContracts().WithPit().WithTable() })
            foreach (var m in typeof(FightRules).GetMethods().Where(m => m.Name.StartsWith("With") && m.ReturnType == typeof(FightRules)))
            {
                var r = (FightRules)m.Invoke(input, m.GetParameters().Length == 0 ? new object[0] : new object[] { 4 });
                Assert.IsTrue(r.Table, m.Name);
            }
        }

        [Test]
        public void SavesAndReplaysKeepTheirVersionAcrossTheBoundary()
        {
            foreach (int version in new[] { 41, 42 })
            {
                var c = Campaign.New(11, new CampaignTuning { RulesCap = version });
                var f = c.StartTutorial();
                for (int i = 0; i < 60; i++) f.Tick();
                var loaded = CampaignSave.Read(CampaignSave.Write(c));
                Assert.IsEmpty(loaded.LoadNote);
                Assert.AreEqual(f.Session.Checksum, loaded.Active.Session.Checksum);
                Assert.AreEqual(version == 42, loaded.Active.Setup.Fight.Table);
                CampaignPilot.PlayFight(c, f, new CampaignPilot(PilotStyle.Reference, 11));
                loaded = CampaignSave.Read(CampaignSave.Write(c));
                Assert.IsEmpty(loaded.LoadNote);
                Assert.IsTrue(loaded.Archive.TryReplay(f.Setup.FightId, out BattleSimulation replay, out string why), why);
                Assert.AreEqual(version == 42, replay.Setup.Fight.Table);
                Assert.AreEqual(f.Session.Checksum, StateChecksum.Compute(replay.OrderedStateSnapshot));
            }
        }
    }
}
