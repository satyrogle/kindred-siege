using System;
using System.Collections.Generic;
using System.Linq;
using KindredSiege.Sim;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using NUnit.Framework;

namespace KindredSiege.Sim.Tests
{
    /// One blow at a time, campaign rules 40 (BattleSimulation.Stage.cs): turns land one per tick from a line, Blows says when
    /// each lands, a rules 40 fight has a third of the health, and the version boundary.
    public class Stage40Tests
    {
        static UnitSpec U(string id, UnitClass c = UnitClass.Skirmisher, int hp = 30000) =>
            new UnitSpec(id, c, Row.Front, UnitRole.Fighter, 100, hp);

        // Four of ours against four of theirs, nobody falls: every turn is an auto attack.
        static BattleSimulation Brawl(FightRules rules)
        {
            return new BattleSimulation(new BattleSetup("stage40", 7,
                new[] { U("a"), U("b"), U("c", UnitClass.Warden), U("d", UnitClass.Warden) },
                new[] { U("w"), U("x"), U("y", UnitClass.Warden), U("z", UnitClass.Warden) },
                new TacticsProfile(), new TacticsProfile(), BattleSetup.DefaultTickLimit, 20, NerveRules.Off, 10, 100,
                SideRules.None, SideRules.None, rules));
        }

        // Each tick's fighters who landed a hit or a miss, how many turns each fighter landed, and the damage his hits did.
        static (int mostInATick, Dictionary<string, int> turns, Dictionary<string, long> damage) Play(BattleSimulation s, int ticks)
        {
            var ses = new SimSession(s);
            for (int i = 0; i < ticks; i++) ses.Tick();
            var turns = new Dictionary<string, int>();
            var damage = new Dictionary<string, long>();
            int most = 0;
            foreach (var tick in s.Events.Where(e => (e.Kind == BattleEventKind.Hit || e.Kind == BattleEventKind.Miss) && e.ActorId.Length > 0)
                         .GroupBy(e => e.Tick))
            {
                most = Math.Max(most, tick.Select(e => e.ActorId).Distinct().Count());
                foreach (var e in tick)
                {
                    turns.TryGetValue(e.ActorId, out int n);
                    turns[e.ActorId] = n + 1;
                    damage.TryGetValue(e.ActorId, out long d);
                    damage[e.ActorId] = d + (e.Kind == BattleEventKind.Hit ? e.Amount : 0);
                }
            }
            return (most, turns, damage);
        }

        [Test]
        public void TurnsLandOneATickAThirdAsOftenAndThreeTimesAsHard()
        {
            // The bigger hit is a layer of the damage layers (rules 17), as in every campaign fight from rules 17 on.
            var (mostBefore, before, dmgBefore) = Play(Brawl(FightRules.Off.WithLayers()), 1800);
            var (mostStage, stage, dmgStage) = Play(Brawl(FightRules.Off.WithLayers().WithStage()), 1800);
            Assert.Greater(mostBefore, 1, "without the stage two fighters land on one tick");
            Assert.AreEqual(1, mostStage, "with the stage one fighter lands a tick");
            foreach (var kv in before)
            {
                Assert.IsTrue(stage.TryGetValue(kv.Key, out int n), kv.Key);
                Assert.That(n, Is.InRange(kv.Value / StageRules.TurnTimes - 2, kv.Value / StageRules.TurnTimes + 2), kv.Key + "'s turns");
                double hitBefore = (double)dmgBefore[kv.Key] / kv.Value, hitStage = (double)dmgStage[kv.Key] / n;
                Assert.That(hitStage / hitBefore, Is.InRange(StageRules.TurnTimes * 0.8, StageRules.TurnTimes * 1.2), kv.Key + "'s hit");
            }
        }

        [Test]
        public void BlowsSaysOnWhichTickEachTurnLands()
        {
            var s = Brawl(FightRules.Off.WithLayers().WithStage());
            var ses = new SimSession(s);
            var expected = new List<(string who, long tick)>();
            int checkedTurns = 0;
            for (int i = 0; i < 1800; i++)
            {
                if (i % 10 == 0 && i < 1700)
                    for (int side = 0; side < 2; side++)
                        foreach (Blow b in s.Blows(side).Where(b => b.Kind == BlowType.Turn && b.TicksLeft <= 20))
                            expected.Add((b.AttackerId, s.CurrentTick + b.TicksLeft - 1));
                ses.Tick();
            }
            foreach (var (who, tick) in expected)
            {
                Assert.IsTrue(s.Events.Any(e => e.Tick == tick && e.ActorId == who && (e.Kind == BattleEventKind.Hit || e.Kind == BattleEventKind.Miss)),
                    who + " on tick " + tick);
                checkedTurns++;
            }
            Assert.Greater(checkedTurns, 50, "turns announced");
        }

        [Test]
        public void ANewCampaignFightPlaysRules40()
        {
            Assert.IsFalse(Campaign.New(3, new CampaignTuning { RulesCap = 39 }).StartTutorial().Setup.Fight.Stage);
            Assert.IsTrue(Campaign.New(3, new CampaignTuning { RulesCap = 40 }).StartTutorial().Setup.Fight.Stage);
            Assert.IsFalse(Campaign.New(3, new CampaignTuning { RulesCap = 41 }).StartTutorial().Setup.Fight.Stage);
            Assert.IsFalse(Campaign.New(3, new CampaignTuning()).StartTutorial().Setup.Fight.Stage);
        }

        [Test]
        public void Rules41FightsAsRules39DidWithoutTheStage()
        {
            var a = Campaign.New(5, new CampaignTuning { RulesCap = 39 }).StartTutorial();
            var b = Campaign.New(5, new CampaignTuning { RulesCap = 41 }).StartTutorial();
            Assert.AreEqual(a.Setup.Fight.Stage, b.Setup.Fight.Stage);
            Assert.AreEqual(a.Setup.Fight.Pit, b.Setup.Fight.Pit);
            Assert.AreEqual(41, b.RulesVersion);
        }

        [Test]
        public void AllBuildersPreserveTheStageSwitch()
        {
            foreach (var input in new[] { FightRules.Off.WithStage(), FightRules.Campaign.WithFreezeContracts().WithPit().WithStage() })
            foreach (var m in typeof(FightRules).GetMethods().Where(m => m.Name.StartsWith("With") && m.ReturnType == typeof(FightRules)))
            {
                var r = (FightRules)m.Invoke(input, m.GetParameters().Length == 0 ? new object[0] : new object[] { 4 });
                Assert.IsTrue(r.Stage, m.Name);
            }
        }

        [Test]
        public void SavesAndReplaysKeepTheirVersionAcrossTheBoundary()
        {
            foreach (int version in new[] { 39, 40 })
            {
                var c = Campaign.New(11, new CampaignTuning { RulesCap = version });
                var f = c.StartTutorial();
                for (int i = 0; i < 60; i++) f.Tick();
                var loaded = CampaignSave.Read(CampaignSave.Write(c));
                Assert.IsEmpty(loaded.LoadNote);
                Assert.AreEqual(f.Session.Checksum, loaded.Active.Session.Checksum);
                Assert.AreEqual(version == 40, loaded.Active.Setup.Fight.Stage);
                CampaignPilot.PlayFight(c, f, new CampaignPilot(PilotStyle.Reference, 11));
                loaded = CampaignSave.Read(CampaignSave.Write(c));
                Assert.IsEmpty(loaded.LoadNote);
                Assert.IsTrue(loaded.Archive.TryReplay(f.Setup.FightId, out BattleSimulation replay, out string why), why);
                Assert.AreEqual(version == 40, replay.Setup.Fight.Stage);
                Assert.AreEqual(f.Session.Checksum, StateChecksum.Compute(replay.OrderedStateSnapshot));
            }
        }
    }
}
