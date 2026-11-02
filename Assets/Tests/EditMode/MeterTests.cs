using System;
using System.Collections.Generic;
using System.Linq;
using KindredSiege.Sim.Battle;
using NUnit.Framework;

namespace KindredSiege.Sim.Tests
{
    /// The parry meter (Jacob, 31 Oct 2026: "Go, lock and build"; DESIGN_LOCKS, "The parry meter"; FightRules.Meter, campaign
    /// rules 22). One quick test per piece, each against the same fight without the meter, only to prove it runs (Jacob,
    /// 12 Oct 2026: build first). Nothing here is tuning.
    public class MeterTests
    {
        private static UnitSpec U(string id, UnitClass unitClass, int health = 3000)
        {
            return new UnitSpec(id, unitClass, SliceClasses.Stats(unitClass).DefaultRow, UnitRole.Fighter, 100, health);
        }

        private static UnitSpec Chief(UnitClass chiefClass, int health = 8000)
        {
            return new UnitSpec("chief", chiefClass, SliceClasses.Stats(chiefClass).DefaultRow, UnitRole.Chief, 100, health);
        }

        private static List<UnitSpec> Side(params UnitSpec[] units) => new List<UnitSpec>(units);

        // The Bloodridge chief with only his Cleave, beside one of his band; your side may cast its chief spell, never on its own.
        private static BattleSimulation Cleaver(string id, ulong seed, List<UnitSpec> ours, bool meter)
        {
            // With the damage layers, as every campaign fight from rules 17: the meter's bonus is a layer.
            FightRules played = FightRules.CampaignChiefTactics.WithParryCenter().WithLayers();
            if (meter) played = played.WithMeter();
            return new BattleSimulation(new BattleSetup(id, seed, ours, Side(U("x.s", UnitClass.Skirmisher), Chief(UnitClass.Warden)),
                new TacticsProfile(), new TacticsProfile(), BattleSetup.DefaultTickLimit, BattleSetup.DefaultDirectivePoints,
                NerveRules.On(NerveRules.Full, NerveRules.Full), 10, 400, new SideRules(1, 0, true),
                new SideRules(2, 0, false, 0, ChiefSpell.Eclipse, kit: ChiefKit.Bloodridge, moves: ChiefMove.Cleave), played));
        }

        private static bool Order(SimSession session, string directive, string target = "")
        {
            return session.TryIssue(Directives.CommandKind, "0", target, 0, 0, directive, out _);
        }

        // Holds on the tick before his next Cleave lands, so it is parried.
        private static void ParryNextCleave(SimSession session, BattleSimulation sim)
        {
            while (!sim.IsFinished && sim.CurrentTick < BattleSetup.DefaultTickLimit)
            {
                if (sim.Threats(0).Any(t => t.Name == ChiefKits.CleaveName && t.BeatTicks == 1)) break;
                session.Tick();
            }
            int parries = sim.Events.Count(e => e.Kind == BattleEventKind.Parry && e.ActorSide == 0);
            Assert.IsTrue(Order(session, "Hold"), "the Hold was refused");
            session.Tick();
            Assert.AreEqual(parries + 1, sim.Events.Count(e => e.Kind == BattleEventKind.Parry && e.ActorSide == 0), "the Cleave was not parried");
        }

        // Your Pal leads (the chief spell needs a living leader), with a Skirmisher, a Marksman and a Trapper.
        private static List<UnitSpec> Squad() => Side(new UnitSpec("pal", UnitClass.Marksman, Row.Back, UnitRole.Pal, 100, 3000),
            U("s", UnitClass.Skirmisher), U("m", UnitClass.Marksman), U("t", UnitClass.Trapper));

        [Test]
        public void EachParryFillsABarUpToThree()
        {
            foreach (bool meter in new[] { false, true })
            {
                BattleSimulation sim = Cleaver("bars", 11, Squad(), meter);
                Assert.AreEqual(meter, sim.MeterOn);
                var session = new SimSession(sim);
                for (int n = 1; n <= 4; n++)
                {
                    ParryNextCleave(session, sim);
                    Assert.AreEqual(meter ? Math.Min(n, MeterRules.Bars) : 0, sim.ParryBars(0), "bars after parry " + n);
                }
                Assert.AreEqual(0, sim.ParryBars(1), "the enemy has no meter");
            }
        }

        [Test]
        public void TheChiefSpellNeedsAFullMeterSpendsItAndCostsNoPoints()
        {
            BattleSimulation sim = Cleaver("spell", 11, Squad(), true);
            var session = new SimSession(sim);
            Assert.IsFalse(sim.CanUse(0, Directive.Eclipse, out string failure));
            StringAssert.Contains("parry bars", failure);
            for (int n = 0; n < MeterRules.Bars; n++) ParryNextCleave(session, sim);
            Assert.IsTrue(sim.CanUse(0, Directive.Eclipse, out failure), failure);
            Assert.AreEqual(0, sim.OrderCost(0, Directive.Eclipse), "OrderCost does not read the meter's free spell");
            int points = sim.DirectivePoints(0);
            Assert.IsTrue(Order(session, Directives.Name(Directive.Eclipse)));
            Assert.AreEqual(points, sim.DirectivePoints(0), "the spell cost points");
            Assert.AreEqual(0, sim.ParryBars(0), "the spell did not spend the bars");

            // Without the meter the same spell needs no bars and costs its points.
            BattleSimulation old = Cleaver("spell", 11, Squad(), false);
            var then = new SimSession(old);
            Assert.IsTrue(old.CanUse(0, Directive.Eclipse, out failure), failure);
            int before = old.DirectivePoints(0);
            Assert.AreEqual(Directives.Cost(Directive.Eclipse), old.OrderCost(0, Directive.Eclipse));
            Assert.IsTrue(Order(then, Directives.Name(Directive.Eclipse)));
            Assert.AreEqual(before - Directives.Cost(Directive.Eclipse), old.DirectivePoints(0));
        }

        [Test]
        public void AHeldBarMakesACashInStronger()
        {
            // One parry, then the first Aimed Shot on a snared enemy or strike on an Opening: the same hit, a bar stronger.
            var first = new BattleEvent[2];
            for (int n = 0; n < 2; n++)
            {
                BattleSimulation sim = Cleaver("cash", 11, Squad(), n == 1);
                var session = new SimSession(sim);
                ParryNextCleave(session, sim);
                int from = sim.Events.Count;
                while (!sim.IsFinished && sim.CurrentTick < BattleSetup.DefaultTickLimit &&
                       !sim.Events.Skip(from).Any(e => e.Kind == BattleEventKind.Hit && e.ActorSide == 0 &&
                           (e.Detail == ClassSpells.AimedPinDetail || e.Detail == ClassSpells.StrikeOpenDetail)))
                    session.Tick();
                first[n] = sim.Events.Skip(from).FirstOrDefault(e => e.Kind == BattleEventKind.Hit && e.ActorSide == 0 &&
                    (e.Detail == ClassSpells.AimedPinDetail || e.Detail == ClassSpells.StrikeOpenDetail));
                Assert.AreEqual(BattleEventKind.Hit, first[n].Kind, "no cash in after the parry; your hits were: " + string.Join(", ",
                    sim.Events.Skip(from).Where(e => e.Kind == BattleEventKind.Hit && e.ActorSide == 0).Select(e => e.Detail).Distinct()) +
                    " by tick " + sim.CurrentTick + (sim.IsFinished ? ", finished" : ""));
                if (n == 1) Assert.AreEqual(1, sim.ParryBars(0));
            }
            Assert.AreEqual(first[0].Tick, first[1].Tick, "the two fights parted before the cash in");
            Assert.AreEqual(first[0].Detail, first[1].Detail);
            Assert.Greater(first[1].Amount, first[0].Amount, "a held bar added nothing");
        }
    }
}
