using System.Collections.Generic;
using System.Linq;
using KindredSiege.Sim.Battle;
using NUnit.Framework;

namespace KindredSiege.Sim.Tests
{
    /// The tight parry (Jacob, 1 Nov 2026: "Go, build it"; DESIGN_LOCKS, "The parry: tight window, big payoff, a gold flash on
    /// the attacker"; FightRules.Tight, campaign rules 26). One quick test per piece, against the same fight without it, only
    /// to prove it runs (Jacob, 12 Oct 2026: build first). Nothing here is tuning.
    public class TightParryTests
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

        private static List<UnitSpec> Squad() => Side(new UnitSpec("pal", UnitClass.Marksman, Row.Back, UnitRole.Pal, 100, 3000),
            U("s", UnitClass.Skirmisher), U("m", UnitClass.Marksman), U("t", UnitClass.Trapper));

        // The Bloodridge chief with only his Cleave, beside one of his band, through the damage layers as every campaign fight.
        private static BattleSimulation Cleaver(string id, bool tight, bool misses = true)
        {
            FightRules played = new FightRules(misses, true, true, true, true, true, true, false, true).WithParryCenter().WithLayers();
            if (tight) played = played.WithTight();
            return new BattleSimulation(new BattleSetup(id, 41, Squad(), Side(U("x.s", UnitClass.Skirmisher), Chief(UnitClass.Warden)),
                new TacticsProfile(), new TacticsProfile(), BattleSetup.DefaultTickLimit, BattleSetup.DefaultDirectivePoints,
                NerveRules.On(NerveRules.Full, NerveRules.Full), 10, 400, new SideRules(1, 0, true),
                new SideRules(2, 0, false, 0, ChiefSpell.Eclipse, kit: ChiefKit.Bloodridge, moves: ChiefMove.Cleave), played));
        }

        // Plays on until his next Cleave is this many ticks from its beat; false if it never comes.
        private static bool UntilCleave(SimSession session, BattleSimulation sim, int beatTicks)
        {
            while (!sim.IsFinished && sim.CurrentTick < BattleSetup.DefaultTickLimit)
            {
                if (sim.Threats(0).Any(t => t.Name == ChiefKits.CleaveName && t.BeatTicks == beatTicks)) return true;
                session.Tick();
            }
            return false;
        }

        // Holds this many ticks before his next Cleave's beat and plays until it has landed; returns the tick the Hold went in.
        private static long HoldBeforeCleave(SimSession session, BattleSimulation sim, int beatTicks)
        {
            Assert.IsTrue(UntilCleave(session, sim, beatTicks), "his Cleave never came");
            long at = sim.CurrentTick;
            Assert.IsTrue(session.TryIssue(Directives.CommandKind, "0", "", 0, 0, "Hold", out string failure), failure);
            for (int i = 0; i < beatTicks + 2; i++) session.Tick();
            return at;
        }

        private static int Parries(BattleSimulation sim) => sim.Events.Count(e => e.Kind == BattleEventKind.Parry && e.ActorSide == 0);

        [Test]
        public void AHoldInTheLastFifthOfASecondParriesAndHitsHimBack()
        {
            BattleSimulation sim = Cleaver("tight.parry", true);
            var session = new SimSession(sim);
            HoldBeforeCleave(session, sim, 5);
            Assert.AreEqual(1, Parries(sim), "a Hold 5 ticks before his Cleave was not a parry");
            BattleEvent counter = sim.Events.FirstOrDefault(e => e.Detail == TightRules.CounterDetail);
            Assert.AreEqual(BattleEventKind.Hit, counter.Kind, "no counter landed");
            Assert.AreEqual("chief", counter.TargetId, "the counter did not hit the one who swung");
            Assert.Greater(counter.Amount, 0);
        }

        [Test]
        public void AHoldOutOfTheWindowWhiffsLandsInFullAndOpensYourSide()
        {
            // Without the tight parry a Hold 10 ticks early is still in the half second window: parried.
            BattleSimulation before = Cleaver("tight.whiff", false);
            HoldBeforeCleave(new SimSession(before), before, 10);
            Assert.AreEqual(1, Parries(before), "the old half second window should parry it");

            BattleSimulation sim = Cleaver("tight.whiff", true);
            var session = new SimSession(sim);
            HoldBeforeCleave(session, sim, 10);
            Assert.AreEqual(0, Parries(sim), "a Hold 10 ticks early parried under the 0.2 s window");
            BattleEvent whiff = sim.Events.FirstOrDefault(e => e.Detail == TightRules.WhiffDetail);
            Assert.AreEqual(BattleEventKind.Reaction, whiff.Kind, "no whiff");
            Assert.AreEqual(0, whiff.ActorSide);
            Assert.AreEqual(0, sim.HoldTicksLeft(0), "the whiffed Hold did not end");
            Assert.Greater(sim.WhiffTicksLeft(0), 0, "your side is not open after the whiff");
            for (int i = 0; i < TightRules.WhiffTicks; i++) session.Tick();
            Assert.AreEqual(0, sim.WhiffTicksLeft(0), "the whiff lasts longer than a second");
            Assert.IsFalse(sim.Events.Any(e => e.Detail == TightRules.CounterDetail), "a whiff hit him back");
        }

        [Test]
        public void BlowsLandHarderForASecondAfterAWhiff()
        {
            // No misses, so both fights draw the same numbers: one whiffs, the other never Holds.
            BattleSimulation whiffed = Cleaver("tight.open", true, false);
            var a = new SimSession(whiffed);
            long holdTick = HoldBeforeCleave(a, whiffed, 10);
            BattleSimulation plain = Cleaver("tight.open", true, false);
            var b = new SimSession(plain);
            while (plain.CurrentTick < whiffed.CurrentTick) b.Tick();
            for (int i = 0; i < TightRules.WhiffTicks; i++)
            {
                a.Tick();
                b.Tick();
            }
            long whiffTick = whiffed.Events.First(e => e.Detail == TightRules.WhiffDetail).Tick;
            int Taken(BattleSimulation s) => s.Events.Where(e => e.Kind == BattleEventKind.Hit && e.ActorSide == 1 &&
                e.Tick > whiffTick && e.Tick < whiffTick + TightRules.WhiffTicks).Sum(e => e.Amount);
            Assert.Greater(holdTick, 0);
            Assert.Greater(Taken(plain), 0, "nothing hit your side in the second after his Cleave");
            Assert.Greater(Taken(whiffed) * 100, Taken(plain) * 130, "blows after the whiff did not land harder");
        }

        [Test]
        public void TheThreatWindowFollowsTheTightRules()
        {
            BattleSimulation sim = Cleaver("tight.read", true);
            var session = new SimSession(sim);
            Assert.IsTrue(UntilCleave(session, sim, 8));
            Threat far = sim.Threats(0).First(t => t.Name == ChiefKits.CleaveName);
            Assert.AreEqual(TightRules.WindowTicks, far.WindowTicks);
            Assert.IsFalse(far.ParryWindow, "8 ticks out is outside the 0.2 s window");
            Assert.IsTrue(UntilCleave(session, sim, 7));
            Threat near = sim.Threats(0).First(t => t.Name == ChiefKits.CleaveName);
            Assert.IsTrue(near.ParryWindow, "7 ticks out (a Hold now lands 6 before) is inside it");
            Assert.AreEqual("chief", near.ActorId, "the window names who swings, for the gold flash");

            BattleSimulation old = Cleaver("tight.read", false);
            var oldSession = new SimSession(old);
            Assert.IsTrue(UntilCleave(oldSession, old, 10));
            Assert.IsTrue(old.Threats(0).First(t => t.Name == ChiefKits.CleaveName).ParryWindow, "the half second window is gone without it");
        }
    }
}
