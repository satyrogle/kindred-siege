using System;
using System.Collections.Generic;
using System.Linq;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using NUnit.Framework;

namespace KindredSiege.Sim.Tests
{
    /// The beasts as their approved scenes show them, campaign rules 25 (Jacob, 1 Nov 2026: "Sim follows scenes", then "Go as
    /// written"; FightRules.BeastScenes, BattleSimulation.Beast.cs): the sand shark's Bite takes the marked one's whole row and
    /// its tail's Sweep all of them, and the cage bear's Grab is a Slam and Grab. Each test checks the old rules too, so older
    /// fights keep playing as they did. Nothing here is tuning.
    public class BeastSceneTests
    {
        private static readonly FightRules Old = FightRules.CampaignBeast;
        private static readonly FightRules Scenes = FightRules.CampaignBeast.WithBeastScenes();

        private static UnitSpec U(string id, UnitClass unitClass)
        {
            return new UnitSpec(id, unitClass);
        }

        private static UnitSpec Pal(string id)
        {
            return new UnitSpec(id, UnitClass.Marksman, Row.Back, UnitRole.Pal, 100, 100);
        }

        // A full squad behind its leader, as the campaign fields it.
        private static List<UnitSpec> Squad()
        {
            return new List<UnitSpec> { Pal("leader"), U("w", UnitClass.Warden), U("s", UnitClass.Skirmisher), U("m", UnitClass.Marksman),
                U("t", UnitClass.Trapper), U("d", UnitClass.Medic) };
        }

        // A beast's parts as the campaign makes them, each this percent as tough.
        private static List<UnitSpec> Beast(string id, BeastKit kit, int healthPercent = 500)
        {
            var parts = new List<UnitSpec>();
            IReadOnlyList<BeastPart> order = BeastKits.Parts(kit);
            for (int p = 0; p < order.Count; p++)
                parts.Add(new UnitSpec(id + "." + BeastKits.Name(order[p]), UnitClass.Warden, BeastKits.RowOf(order[p]), UnitRole.Fighter, 100,
                    Math.Max(1, healthPercent * BeastKits.HealthPercent(order[p]) / 100)));
            return parts;
        }

        private static BattleSimulation Fight(string id, ulong seed, BeastKit kit, FightRules rules)
        {
            return new BattleSimulation(new BattleSetup(id, seed, Squad(), Beast(id, kit), new TacticsProfile(), new TacticsProfile(),
                BattleSetup.DefaultTickLimit, BattleSetup.DefaultDirectivePoints, NerveRules.On(), 10, 400, new SideRules(1, 0, true),
                new SideRules(0, 0, false, beast: kit), rules));
        }

        private static bool Lands(BattleEvent e, string move)
        {
            return e.Kind == BattleEventKind.Land && e.Detail.StartsWith(move + ".");
        }

        // Ticks until an event matches, calling before ahead of each tick.
        private static BattleEvent Next(BattleSimulation sim, SimSession session, Func<BattleEvent, bool> match, Action before = null)
        {
            int seen = sim.Events.Count;
            while (!sim.IsFinished)
            {
                before?.Invoke();
                session.Tick();
                for (; seen < sim.Events.Count; seen++)
                    if (match(sim.Events[seen])) return sim.Events[seen];
            }
            Assert.Fail("never happened");
            return default;
        }

        // Everyone a move on that tick reached: who it hit with this detail, and whoever a Warden covered from it.
        private static HashSet<string> Reached(BattleSimulation sim, long tick, string detail)
        {
            var ids = new HashSet<string>();
            foreach (BattleEvent e in sim.Events)
            {
                if (e.Tick != tick) continue;
                if (e.Kind == BattleEventKind.Hit && e.Detail == detail && e.ActorSide == 1) ids.Add(e.TargetId);
                if (e.Kind == BattleEventKind.Guard && e.ActorSide == 0) ids.Add(e.TargetId);
            }
            return ids;
        }

        private static bool AnyIn(BattleSimulation sim, IEnumerable<string> ids, Row row)
        {
            return ids.Any(id => sim.FindById(id) is BattleUnit u && u.Side == 0 && u.Row == row);
        }

        [Test]
        public void TheSharksBiteTakesTheMarkedOnesWholeRow()
        {
            foreach (bool scenes in new[] { false, true })
            {
                var sim = Fight("scene.bite", 5, BeastKit.SandShark, scenes ? Scenes : Old);
                var session = new SimSession(sim);
                Assert.AreEqual(scenes, sim.BeastScenesOn);
                BattleEvent first = Next(sim, session, e => Lands(e, BeastKits.BiteName));
                Row row = sim.FindById(first.TargetId).Row;
                var bitten = sim.Events.Where(e => e.Tick == first.Tick && Lands(e, BeastKits.BiteName)).Select(e => e.TargetId).ToList();
                var rowNow = sim.Units.Where(u => u.Side == 0 && !u.IsLeader && u.Row == row && (u.Alive || bitten.Contains(u.Id)))
                    .Select(u => u.Id).ToList();
                if (!scenes)
                {
                    Assert.AreEqual(1, bitten.Count, "the old Bite takes one");
                    continue;
                }
                Assert.Greater(rowNow.Count, 1, "the test needs a row of more than one");
                CollectionAssert.AreEquivalent(rowNow, bitten, "one share of the Bite on each of that row");
            }
        }

        [Test]
        public void AWardenTakesEveryShareHeCoversAndTheJawTakesEachBack()
        {
            var sim = Fight("scene.shield", 3, BeastKit.SandShark, Scenes);
            var session = new SimSession(sim);
            BattleEvent first = Next(sim, session, e => Lands(e, BeastKits.BiteName));
            var land = sim.Events.Where(e => e.Tick == first.Tick && Lands(e, BeastKits.BiteName)).ToList();
            int guarded = land.Count(e => e.Detail == BeastKits.BiteName + "." + BeastKits.Guarded);
            int backs = sim.Events.Count(e => e.Tick == first.Tick && e.Kind == BattleEventKind.Hit && e.Detail == BeastKits.BiteBackDetail);
            Assert.Greater(guarded, 0, "the Warden stands in the bitten row, so at least his own share is on his shield");
            Assert.AreEqual(guarded, backs, "the jaw takes each share on his shield back");
        }

        [Test]
        public void TheSharksSweepTakesBothRows()
        {
            foreach (bool scenes in new[] { false, true })
            {
                var sim = Fight("scene.sweep", 9, BeastKit.SandShark, scenes ? Scenes : Old);
                var session = new SimSession(sim);
                BattleEvent land = Next(sim, session, e => Lands(e, BeastKits.SweepName));
                HashSet<string> reached = Reached(sim, land.Tick, BeastKits.SweepDetail);
                Assert.IsTrue(AnyIn(sim, reached, Row.Front), "the front row takes it");
                Assert.AreEqual(scenes, AnyIn(sim, reached, Row.Back), scenes ? "and the back row too" : "the old Sweep stays on the front row");
            }
        }

        [Test]
        public void TheBearSlamsEveryoneThenGrabs()
        {
            foreach (bool scenes in new[] { false, true })
            {
                var sim = Fight("scene.slam", 7, BeastKit.CageBear, scenes ? Scenes : Old);
                var session = new SimSession(sim);
                BattleEvent grab = Next(sim, session, e => Lands(e, BeastKits.GrabName));
                int grabAt = sim.Events.ToList().FindIndex(e => e.Tick == grab.Tick && Lands(e, BeastKits.GrabName));
                int slamAt = sim.Events.ToList().FindIndex(e => e.Tick == grab.Tick && Lands(e, BeastKits.SlamName));
                if (!scenes)
                {
                    Assert.AreEqual(-1, slamAt, "the old Grab has no Slam");
                    Assert.IsFalse(sim.Events.Any(e => e.Detail == BeastKits.SlamDetail));
                    continue;
                }
                Assert.GreaterOrEqual(slamAt, 0, "the Slam lands with the Grab");
                Assert.Less(slamAt, grabAt, "the ice wave first, then the claw");
                HashSet<string> reached = Reached(sim, grab.Tick, BeastKits.SlamDetail);
                Assert.IsTrue(AnyIn(sim, reached, Row.Front) && AnyIn(sim, reached, Row.Back), "the wave reaches both rows");
                if (grab.Detail == BeastKits.GrabName + "." + BeastKits.Full) Assert.AreEqual(grab.TargetId, sim.BeastHeldId, "then the claw holds its mark");
            }
        }

        [Test]
        public void FallBackGetsClearOfTheWholeSlamAndGrab()
        {
            var sim = Fight("scene.dodge", 9, BeastKit.CageBear, Scenes);
            var session = new SimSession(sim);
            BattleEvent dodge = Next(sim, session, e => Lands(e, BeastKits.GrabName), () =>
            {
                int left = sim.BeastTellTicks(BeastPart.Claw);
                if (left > 0 && left <= 5 && sim.FallBackTicksLeft(0) == 0)
                    session.TryIssue(Directives.CommandKind, "0", string.Empty, 0, 0, "FallBack", out _);
            });
            Assert.AreEqual(BeastKits.GrabName + "." + BeastKits.Dodged, dodge.Detail);
            Assert.IsFalse(sim.Events.Any(e => e.Tick == dodge.Tick && (Lands(e, BeastKits.SlamName) || e.Detail == BeastKits.SlamDetail)),
                "no wave either");
            Assert.AreEqual(string.Empty, sim.BeastHeldId);
        }

        [Test]
        public void ACampaignBeastFightPlaysTheScenesFromRules25AndPlaysBack()
        {
            foreach (int cap in new[] { Campaign.WhispererRules, Campaign.BeastSceneRules })
            {
                var t = new CampaignTuning { RulesCap = cap };
                Campaign c = Campaign.New(41, t);
                Assert.IsTrue(CampaignPilot.PlayTutorial(c, PilotStyle.Reference, 41, 10), "the tutorial was never won");
                c.StartRun();
                CampaignFight fight = c.StartBeast();
                Assert.AreEqual(cap, fight.RulesVersion);
                Assert.AreEqual(cap >= Campaign.BeastSceneRules, fight.Setup.Fight.BeastScenes);
                CampaignPilot.PlayFight(c, fight, new CampaignPilot(PilotStyle.Reference, 41));
                Assert.IsTrue(c.Archive.TryReplay(fight.Setup.FightId, out _, out string failure), failure);
                Campaign read = CampaignSave.Read(CampaignSave.Write(c), t);
                Assert.IsEmpty(read.LoadNote);
                Assert.IsTrue(read.Archive.TryReplay(fight.Setup.FightId, out _, out failure), failure);
            }
        }
    }
}
