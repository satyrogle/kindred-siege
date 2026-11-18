using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using KindredSiege.Sim;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using NUnit.Framework;

namespace KindredSiege.Sim.Tests
{
    /// The Pit, campaign rules 38 (BattleSimulation.Pit.cs): every blow announced ahead, a parryable move's mark locked from its
    /// windup, one parryable move winding up at a time. One quick test for each, and the version boundary.
    public class Pit38Tests
    {
        const BindingFlags F = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static void Set(object o, string name, object v) => o.GetType().GetProperty(name, F).SetValue(o, v);

        static UnitSpec U(string id, UnitClass c = UnitClass.Skirmisher, Row row = Row.Front, int hp = 30000) =>
            new UnitSpec(id, c, row, UnitRole.Fighter, 100, hp);

        // Three of ours against three of theirs, nobody falls: every turn is an auto attack.
        static BattleSimulation Brawl(FightRules rules, SideRules theirs = null, int hp = 30000)
        {
            return new BattleSimulation(new BattleSetup("pit38", 7,
                new[] { U("a", hp: hp), U("b", hp: hp), U("c", hp: hp) },
                new[] { U("x", hp: hp), U("y", hp: hp), U("z", hp: hp) },
                new TacticsProfile(), new TacticsProfile(), BattleSetup.DefaultTickLimit, 20, NerveRules.Off, 10, 100,
                SideRules.None, theirs ?? SideRules.None, rules));
        }

        [Test]
        public void EveryTurnIsAnnouncedOnTheTickBeforeItLands()
        {
            var s = Brawl(FightRules.Off.WithPit());
            var ses = new SimSession(s);
            int checkedTurns = 0;
            for (int i = 0; i < 600; i++)
            {
                var due = new List<Blow>();
                for (int side = 0; side < 2; side++) due.AddRange(s.Blows(side).Where(b => b.Kind == BlowType.Turn && b.TicksLeft == 1));
                int start = s.Events.Count;
                ses.Tick();
                foreach (Blow b in due)
                {
                    Assert.IsTrue(s.Events.Skip(start).Any(e => (e.Kind == BattleEventKind.Hit || e.Kind == BattleEventKind.Miss) &&
                        e.ActorId == b.AttackerId && e.TargetId == b.TargetId), b.AttackerId + " at " + b.TargetId + ", tick " + s.CurrentTick);
                    checkedTurns++;
                }
            }
            Assert.Greater(checkedTurns, 50, "turns announced");
        }

        [Test]
        public void ReadingTheBlowsChangesNothing()
        {
            var quiet = Campaign.New(5, new CampaignTuning());
            var asked = Campaign.New(5, new CampaignTuning());
            var fq = quiet.StartTutorial();
            var fa = asked.StartTutorial();
            Assert.IsTrue(fq.Setup.Fight.Pit, "a new fight plays rules 38");
            var pq = new CampaignPilot(PilotStyle.Reference, 5);
            var pa = new CampaignPilot(PilotStyle.Reference, 5);
            int blows = 0;
            while (!fq.Over)
            {
                pq.Step(fq); fq.Tick();
                pa.Step(fa); blows += fa.Sim.Blows(0).Count + fa.Sim.Blows(1).Count; fa.Tick();
                Assert.AreEqual(fq.Session.Checksum, fa.Session.Checksum, "tick " + fq.Sim.CurrentTick);
            }
            Assert.IsTrue(fa.Over);
            Assert.Greater(blows, 0);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void AHeavySwingWhoseMarkFallsLandsOnNobody(bool pit)
        {
            FightRules rules = new FightRules(false, true, true, true, true).WithParryCenter();
            if (pit) rules = rules.WithPit();
            var s = Brawl(rules, new SideRules(0, 0, false, 90));
            var ses = new SimSession(s);
            while (s.HeavyTargetId(1).Length == 0 && s.CurrentTick < 2000) ses.Tick();
            string swinger = s.HeavySwingerId(1), mark = s.HeavyTargetId(1);
            Assert.IsNotEmpty(mark, "a heavy swing wound up");
            BattleUnit marked = s.FindById(mark);
            Set(marked, "Hp", 0);
            Set(marked, "Alive", false);
            int start = s.Events.Count;
            while (s.HeavyTargetId(1).Length > 0 && s.CurrentTick < 4000) ses.Tick();
            BattleEvent release = s.Events.Skip(start).First(e => e.Kind == BattleEventKind.Release && e.ActorId == swinger);
            if (pit)
            {
                Assert.AreEqual(PitRules.Lost, release.Detail);
                Assert.AreEqual(0, release.Amount);
                Assert.AreEqual(mark, release.TargetId);
            }
            else Assert.AreNotEqual(mark, release.TargetId, "before rules 38 it finds a new mark");
        }

        [Test]
        public void ASideWindsUpOneParryableMoveAtATime()
        {
            int most = 0, seen = 0, fights = 0;
            var c = Campaign.New(9, new CampaignTuning());
            CampaignPilot.PlayTutorial(c, PilotStyle.Reference, 9);
            c.ChooseTribe(CampaignTribes.BloodridgeId);
            var picker = new CampaignPilot(PilotStyle.Reference, 9);
            for (int n = 0; c.Stage == CampaignStage.InRun && n < 12; n++)
            {
                CampaignFight fight = c.StartEncounter(picker.PickOffer(c));
                var pilot = new CampaignPilot(PilotStyle.Reference, 9UL * 31UL + (ulong)n);
                // The beasts' parts and the hunt's steps are not held back yet (BattleSimulation.Pit.cs).
                bool held = fight.Setup.Rules(1).Beast == BeastKit.None && fight.Setup.Rules(1).Kit != ChiefKit.Hunt;
                Assert.IsTrue(fight.Setup.Fight.Pit);
                while (!fight.Over)
                {
                    pilot.Step(fight);
                    fight.Tick();
                    if (!held) continue;
                    int now = fight.Sim.Blows(1).Where(b => b.Parryable).Select(b => b.AttackerId + "/" + b.Move.Name).Distinct().Count();
                    if (now > 0) seen++;
                    most = Math.Max(most, now);
                }
                c.Finish(fight);
                if (held) fights++;
            }
            Assert.Greater(fights, 0);
            Assert.Greater(seen, 0, "parryable moves came");
            Assert.LessOrEqual(most, 1);
        }

        [Test]
        public void AllBuildersPreserveThePitSwitch()
        {
            foreach (var input in new[] { FightRules.Off.WithPit(), FightRules.Campaign.WithFreezeContracts().WithPit() })
            foreach (var m in typeof(FightRules).GetMethods().Where(m => m.Name.StartsWith("With") && m.ReturnType == typeof(FightRules)))
            {
                var r = (FightRules)m.Invoke(input, m.GetParameters().Length == 0 ? new object[0] : new object[] { 4 });
                Assert.IsTrue(r.Pit, m.Name);
            }
        }

        [Test]
        public void SavesAndReplaysKeepTheirVersionAcrossTheBoundary()
        {
            foreach (int version in new[] { 37, 38 })
            {
                var c = Campaign.New(11, new CampaignTuning { RulesCap = version });
                var f = c.StartTutorial();
                for (int i = 0; i < 60; i++) f.Tick();
                var loaded = CampaignSave.Read(CampaignSave.Write(c));
                Assert.IsEmpty(loaded.LoadNote);
                Assert.AreEqual(f.Session.Checksum, loaded.Active.Session.Checksum);
                Assert.AreEqual(version == 38, loaded.Active.Setup.Fight.Pit);
                CampaignPilot.PlayFight(c, f, new CampaignPilot(PilotStyle.Reference, 11));
                loaded = CampaignSave.Read(CampaignSave.Write(c));
                Assert.IsEmpty(loaded.LoadNote);
                Assert.IsTrue(loaded.Archive.TryReplay(f.Setup.FightId, out BattleSimulation replay, out string why), why);
                Assert.AreEqual(version == 38, replay.Setup.Fight.Pit);
                Assert.AreEqual(f.Session.Checksum, StateChecksum.Compute(replay.OrderedStateSnapshot));
            }
        }
    }
}
