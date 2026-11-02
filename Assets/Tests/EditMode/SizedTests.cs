using System;
using System.Collections.Generic;
using System.Linq;
using KindredSiege.Sim.Battle;
using NUnit.Framework;

namespace KindredSiege.Sim.Tests
{
    /// The sizing pass (Jacob in chat, 1 Nov 2026: "okay go"; FightRules.Sized, campaign rules 28): each Pal's sized numbers
    /// play, and leaning pays more than spreading. One quick test per piece, only to prove each runs. The numbers themselves
    /// came from the builds test (tools/sim_checks/sweep/PalSweep.cs), not from here.
    public class SizedTests
    {
        private static readonly FightRules Chief27 =
            FightRules.CampaignChiefTactics.WithParryCenter().WithLayers().WithFloor().WithShadeForms().WithGlutton().WithDuos();
        private static readonly FightRules Chief28 = Chief27.WithSized();
        private static readonly FightRules Road27 =
            FightRules.Campaign.WithRoadKit().WithLayers().WithMarks().WithFloor().WithShadeForms().WithGlutton().WithDuos();
        private static readonly FightRules Road28 = Road27.WithSized();

        private static List<UnitSpec> Side(params UnitSpec[] units) => new List<UnitSpec>(units);

        private static UnitSpec U(string id, UnitClass unitClass, int rank = 1)
        {
            var spec = new UnitSpec(id, unitClass, SliceClasses.Stats(unitClass).DefaultRow, UnitRole.Fighter, 100, 3000);
            return rank > 1 ? spec.WithRank(rank) : spec;
        }

        private static PalKit Kit(ShadeRoute route, int form, params GluttonTalent[] talents)
        {
            var routes = new List<KeyValuePair<ShadeRoute, int>>();
            if (route != ShadeRoute.None) routes.Add(new KeyValuePair<ShadeRoute, int>(route, form));
            var levels = new int[GluttonRules.TalentCount + 1];
            foreach (GluttonTalent t in talents) levels[(int)t] = GluttonRules.Held;
            return new PalKit(routes, 0).WithTalents(levels);
        }

        private static BattleSimulation Cleaver(string id, PalKit kit, FightRules rules, params UnitSpec[] ours)
        {
            var side = Side(new UnitSpec("pal", UnitClass.Marksman, Row.Back, UnitRole.Pal, 100, 3000));
            side.AddRange(ours.Length > 0 ? ours : new[] { U("s", UnitClass.Skirmisher), U("m", UnitClass.Marksman) });
            return new BattleSimulation(new BattleSetup(id, 11, side,
                Side(new UnitSpec("x.s", UnitClass.Skirmisher, Row.Front, UnitRole.Fighter, 100, 3000),
                    new UnitSpec("chief", UnitClass.Warden, Row.Front, UnitRole.Chief, 100, 8000)),
                new TacticsProfile(), new TacticsProfile(), BattleSetup.DefaultTickLimit, BattleSetup.DefaultDirectivePoints,
                NerveRules.On(NerveRules.Full, NerveRules.Full), 10, 400, new SideRules(1, 0, true).WithPals(kit),
                new SideRules(2, 0, false, 0, ChiefSpell.Eclipse, kit: ChiefKit.Bloodridge, moves: ChiefMove.Cleave), rules));
        }

        private static BattleSimulation MedicRow(string id, PalKit kit, FightRules rules, params UnitSpec[] ours)
        {
            var theirs = new List<UnitSpec>();
            for (int i = 1; i <= 5; i++) theirs.Add(new UnitSpec("m" + i, UnitClass.Medic, Row.Front, UnitRole.Fighter, 100, 3000));
            return new BattleSimulation(new BattleSetup(id, 4, Side(ours), theirs, new TacticsProfile(), new TacticsProfile(),
                BattleSetup.DefaultTickLimit, BattleSetup.DefaultDirectivePoints, NerveRules.On(NerveRules.Full, NerveRules.Full), 0, 400,
                new SideRules(0, 0, false).WithPals(kit), null, rules));
        }

        private static bool Order(SimSession session, Directive directive, string target = "")
        {
            return session.TryIssue(Directives.CommandKind, "0", target, 0, 0, Directives.Name(directive), out _);
        }

        private static BattleEvent First(BattleSimulation sim, SimSession session, Func<BattleEvent, bool> match, int ticks = 6000)
        {
            int seen = 0;
            while (true)
            {
                for (; seen < sim.Events.Count; seen++) if (match(sim.Events[seen])) return sim.Events[seen];
                if (sim.IsFinished || sim.CurrentTick >= ticks) return default;
                session.Tick();
            }
        }

        [Test]
        public void HerTricksGrowWithTheirFormAndHerPricesHalve()
        {
            int returns = 0;
            int Blast(int form, FightRules rules)
            {
                BattleSimulation sim = Cleaver("mine", Kit(ShadeRoute.RiftMine, form), rules);
                var session = new SimSession(sim);
                session.Tick();
                Assert.IsTrue(Order(session, Directive.FallBack));
                returns = sim.ReturnTicksLeft(0);
                BattleEvent e = First(sim, session, x => x.Kind == BattleEventKind.Hit && x.Detail == ShadeRules.MineDetail);
                Assert.AreEqual(ShadeRules.MineDetail, e.Detail, "the mine never went off");
                return e.Amount;
            }
            int plain = Blast(1, Chief27);
            int plainReturn = returns;
            int sized = Blast(1, Chief28);
            Assert.AreEqual(plain * (100 + SizeRules.ShadeForm1Percent) / 100, sized, 1, "form 1, sized");
            Assert.Less(returns, plainReturn, "Rift Mine's price, halved");
            Assert.Greater(Blast(2, Chief28), sized, "form 2 hits harder again");
        }

        [Test]
        public void ATalentGrowsWithTheOthersHeldInItsLimb()
        {
            int FirstHit(PalKit kit, FightRules rules)
            {
                BattleSimulation sim = Cleaver("limb", kit, rules);
                BattleEvent e = First(sim, new SimSession(sim), x => x.Kind == BattleEventKind.Hit && x.ActorId == "s");
                Assert.AreEqual(BattleEventKind.Hit, e.Kind, "the Skirmisher never hit");
                return e.Amount;
            }
            int plain = FirstHit(Kit(ShadeRoute.None, 0, GluttonTalent.NucleusEdge), Chief27);
            int sized = FirstHit(Kit(ShadeRoute.None, 0, GluttonTalent.NucleusEdge), Chief28);
            int climbed = FirstHit(Kit(ShadeRoute.None, 0, GluttonTalent.NucleusEdge, GluttonTalent.Strip), Chief28);
            Assert.Greater(sized, plain, "sized");
            Assert.Greater(climbed, sized, "a second Strikers talent makes Nucleus Edge bigger");
        }

        [Test]
        public void ARankThreeStarHitsFarHarder()
        {
            int FirstHit(int rank, FightRules rules)
            {
                BattleSimulation sim = Cleaver("star", PalKit.None, rules, U("s", UnitClass.Skirmisher, rank), U("m", UnitClass.Marksman));
                BattleEvent e = First(sim, new SimSession(sim), x => x.Kind == BattleEventKind.Hit && x.ActorId == "s");
                Assert.AreEqual(BattleEventKind.Hit, e.Kind, "the Skirmisher never hit");
                return e.Amount;
            }
            int plain = FirstHit(3, Chief27);
            int expected = plain * (100 + SizeRules.Rank3Percent) / 100;
            Assert.AreEqual(expected, FirstHit(3, Chief28), Math.Max(1, expected / 20), "the plain hit rounds before it is multiplied");
            Assert.AreEqual(FirstHit(1, Chief27), FirstHit(1, Chief28), "an unranked fighter is the same");
        }

        [Test]
        public void ThreeRanksOfGravityCrushTheWholeRow()
        {
            // Black Hole Round at rank 2 and Event Horizon: three ranks of Gravity, its final form from rules 29.
            UnitSpec round = new UnitSpec("m", UnitClass.Marksman).WithBend(ClassBend.BlackHoleRound).WithComboRank(2);
            UnitSpec horizon = new UnitSpec("w", UnitClass.Warden).WithBend(ClassBend.EventHorizon);
            FightRules road29 = Road28.WithFlavours();
            bool Row(FightRules rules)
            {
                BattleSimulation sim = MedicRow("row", PalKit.None, rules, round, horizon);
                BattleEvent e = First(sim, new SimSession(sim), x => x.Kind == BattleEventKind.Burst && x.Detail == SizeRules.CrushRowDetail, 9000);
                return e.Detail == SizeRules.CrushRowDetail;
            }
            Assert.IsTrue(Row(road29), "no row crushed");
            Assert.IsFalse(Row(Road28), "not before rules 29");
            Assert.AreEqual(3, MedicRow("count", PalKit.None, road29, round, horizon).FlavourRanksOf(0, StatusKind.Crush));
        }

        [Test]
        public void ASecondComboOfAFlavourBurstsSoonerAndBigger()
        {
            // Black Hole Round and Event Horizon both lay Crush: two ranks of one flavour.
            UnitSpec round = new UnitSpec("m", UnitClass.Marksman).WithBend(ClassBend.BlackHoleRound).WithComboRank(MarkRules.MaxStacks);
            UnitSpec horizon = new UnitSpec("w", UnitClass.Warden).WithBend(ClassBend.EventHorizon);
            int Crushed(FightRules rules)
            {
                BattleSimulation sim = MedicRow("flavour", PalKit.None, rules, round, horizon);
                BattleEvent e = First(sim, new SimSession(sim), x => x.Kind == BattleEventKind.Hit && x.Detail == MarkRules.CrushedDetail);
                Assert.AreEqual(MarkRules.CrushedDetail, e.Detail, "no Crush burst");
                return e.Amount;
            }
            Assert.Greater(Crushed(Road28), Crushed(Road27));
        }
    }
}
