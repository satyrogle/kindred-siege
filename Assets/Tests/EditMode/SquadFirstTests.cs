using System;
using System.Collections.Generic;
using System.Linq;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using NUnit.Framework;

namespace KindredSiege.Sim.Tests
{
    /// The squad first (Jacob, 24 Oct 2026, later: "go, build rules 9 with your three picks as written"; Claude's proposal at
    /// HANDOFF_LOG line 10945; FightRules.SquadFirst, BattleSimulation.Parry.cs). One quick test per piece, only to prove it
    /// runs. Nothing here is tuning.
    public class SquadFirstTests
    {
        private static UnitSpec U(string id, UnitClass unitClass, int health = 1000)
        {
            return new UnitSpec(id, unitClass, SliceClasses.Stats(unitClass).DefaultRow, UnitRole.Fighter, 100, health);
        }

        private static UnitSpec Chief(UnitClass chiefClass, int health = 1000)
        {
            return new UnitSpec("chief", chiefClass, SliceClasses.Stats(chiefClass).DefaultRow, UnitRole.Chief, 100, health);
        }

        private static List<UnitSpec> Side(params UnitSpec[] units)
        {
            return new List<UnitSpec>(units);
        }

        private static List<UnitSpec> Ours()
        {
            return Side(U("w", UnitClass.Warden), U("s", UnitClass.Skirmisher), U("m", UnitClass.Marksman), U("h", UnitClass.Medic),
                U("t", UnitClass.Trapper));
        }

        // A band of three that swings heavies.
        private static List<UnitSpec> Band()
        {
            return Side(U("x.a", UnitClass.Warden), U("x.b", UnitClass.Skirmisher), U("x.c", UnitClass.Warden));
        }

        private static SideRules Heavies()
        {
            return new SideRules(0, 0, false, HeavyRules.EveryTicks);
        }

        private static BattleSimulation Fight(string id, ulong seed, List<UnitSpec> ours, List<UnitSpec> theirs, SideRules rules, FightRules fight,
            TacticsProfile theirTactics = null, int refill = 10)
        {
            return new BattleSimulation(new BattleSetup(id, seed, ours, theirs, new TacticsProfile(), theirTactics ?? new TacticsProfile(),
                BattleSetup.DefaultTickLimit, BattleSetup.DefaultDirectivePoints, NerveRules.On(NerveRules.Full, NerveRules.Full),
                refill, 400, new SideRules(1, 0, true), rules, fight.WithSquadFirst()));
        }

        // The Bloodridge chief with only his Cleave, and his Skirmisher swinging heavies beside him.
        private static BattleSimulation Cleaver(string id, ulong seed, List<UnitSpec> ours, TacticsProfile tactics = null, int refill = 10)
        {
            return Fight(id, seed, ours, Side(U("x.s", UnitClass.Skirmisher), Chief(UnitClass.Warden)),
                new SideRules(2, 0, false, HeavyRules.EveryTicks, ChiefSpell.Eclipse, kit: ChiefKit.Bloodridge, moves: ChiefMove.Cleave),
                FightRules.CampaignChiefTactics, tactics, refill);
        }

        private static TacticsProfile Learned(Habit habit, string move)
        {
            var t = new TacticsProfile();
            if (habit == Habit.WaitOutParry)
            {
                t.SetWaitOutParry(5);
                t.SetWaitOutMove(move);
            }
            else
            {
                t.SetFeint(5);
                t.SetFeintMove(move);
            }
            return t;
        }

        private static bool Order(SimSession session, string directive, string target = "")
        {
            return session.TryIssue(Directives.CommandKind, "0", target, 0, 0, directive, out _);
        }

        // Ticks on until a threat of this name on side 0 has its beat this many ticks off, and returns it.
        private static Threat Beat(SimSession session, BattleSimulation sim, string name, int at = 1)
        {
            while (!sim.IsFinished && sim.CurrentTick < BattleSetup.DefaultTickLimit)
            {
                foreach (Threat t in sim.Threats(0))
                    if (t.Name == name && t.BeatTicks == at) return t;
                session.Tick();
            }
            Assert.Fail(name + "'s beat never came");
            return Threat.None;
        }

        // Ticks on until this happens, and returns the event.
        private static BattleEvent Next(SimSession session, BattleSimulation sim, Func<BattleEvent, bool> match)
        {
            int seen = sim.Events.Count;
            while (!sim.IsFinished && sim.CurrentTick < BattleSetup.DefaultTickLimit)
            {
                session.Tick();
                for (int i = seen; i < sim.Events.Count; i++) if (match(sim.Events[i])) return sim.Events[i];
                seen = sim.Events.Count;
            }
            Assert.Fail("it never happened, tick " + sim.CurrentTick);
            return default;
        }

        // Holds on the tick before every beat coming at side 0, and plays the fight out.
        private static void ParryEverything(BattleSimulation sim)
        {
            var session = new SimSession(sim);
            while (!sim.IsFinished && sim.CurrentTick < BattleSetup.DefaultTickLimit)
            {
                if (sim.AwaitingVerdict) Order(session, "Hold");
                else if (sim.HoldTicksLeft(0) <= 0 && sim.Threats(0).Any(t => t.BeatTicks == 1)) Order(session, "Hold");
                session.Tick();
            }
        }

        private static List<Lesson> Raised(IEnumerable<Lesson> lessons)
        {
            return lessons.Where(l => RematchClue.Raises(l.Rule, out _) && l.ValueAfter > l.ValueBefore).ToList();
        }

        // ------------------------------------------------------------ big beats and small beats

        [Test]
        public void ABandsHeavySwingIsASmallBeatParriedForItsOpeningButNoNerve()
        {
            BattleSimulation sim = Fight("small", 3, Side(U("w", UnitClass.Warden), U("s", UnitClass.Skirmisher)), Band(), Heavies(), FightRules.Campaign);
            var session = new SimSession(sim);
            Assert.IsTrue(sim.SquadFirstOn);
            Assert.IsTrue(sim.ParryCenterOn, "the squad first plays on top of the parry at the center");
            Threat swing = Beat(session, sim, HeavyRules.Heavy);
            Assert.IsFalse(swing.Duel, "a band's swing is no duel");
            Assert.IsTrue(swing.ParryWindow);
            BattleUnit swinger = sim.FindById(swing.ActorId);
            Assert.IsTrue(Order(session, "Hold"));
            int before = sim.Nerve(1);
            session.Tick();
            Assert.AreEqual(ParryRules.HeavyName, sim.Events.Last(e => e.Kind == BattleEventKind.Parry).Detail);
            Assert.AreEqual(FightRules.StaggerTicks - 1, sim.ReelTicksLeft(swinger), "he still staggers and reels");
            Assert.IsTrue(sim.EnemyMarks(0).Any(m => m.UnitId == swinger.Id && m.Mark == ClassSpells.OpeningMark), "and is left open");
            Assert.IsTrue(sim.OpenCombos(0).Any(c => c.Combo == FightRules.Riposte), "with the Riposte's window");
            Assert.AreEqual(0, sim.ParryChain(0), "no step on the chain");
            Assert.Less(before - sim.Nerve(1), HeavyRules.ParryNerve, "no crack on his nerve");
            Assert.AreEqual(ParryRules.SquadParryPoints,
                sim.Events.Last(e => e.Kind == BattleEventKind.Earn && e.ActorSide == 0 && e.Detail == FightRules.ParryReason).Amount, "the Hold's point back");
        }

        [Test]
        public void ASmallBeatLandingInFullLeavesYourChainBe()
        {
            BattleSimulation sim = Cleaver("chain", 11, Side(U("s", UnitClass.Skirmisher), U("m", UnitClass.Marksman), U("h", UnitClass.Medic)));
            var session = new SimSession(sim);
            Threat cleave = Beat(session, sim, ChiefKits.CleaveName);
            Assert.IsTrue(cleave.Duel, "his Cleave is a duel");
            Assert.IsTrue(Order(session, "Hold"));
            session.Tick();
            Assert.AreEqual(1, sim.ParryChain(0));
            BattleEvent full = Next(session, sim, e => e.Kind == BattleEventKind.Release && e.ActorSide == 1 && e.Detail == Moves.Full);
            Assert.AreEqual(1, sim.ParryChain(0), "his band's swing landed in full and your chain stands");
            Assert.IsFalse(sim.Events.Any(e => e.Kind == BattleEventKind.Glory && e.Detail == ChiefKits.EatenDetail && e.Tick == full.Tick),
                "and your Glory stays");
        }

        // ------------------------------------------------------------ Hold is a stance again

        [Test]
        public void AStaleHoldOnlySoaksForItsPointAndItsDrain()
        {
            BattleSimulation sim = Fight("stance", 3, Side(U("w", UnitClass.Warden), U("s", UnitClass.Skirmisher)), Band(), Heavies(), FightRules.Skill);
            var session = new SimSession(sim);
            Beat(session, sim, HeavyRules.Heavy, 20);
            Assert.IsTrue(Order(session, "Hold"));
            BattleEvent release = Next(session, sim, e => e.Kind == BattleEventKind.Release && e.ActorSide == 1);
            Assert.AreNotEqual(FightRules.Parried, release.Detail, "a Hold 20 ticks early never parries");
            Assert.IsFalse(sim.Events.Any(e => e.Kind == BattleEventKind.Reaction && e.Detail == ParryRules.BlockDetail), "no block, no block cost");
            OrderResult hold = sim.OrderResults(0).Last(r => r.Order == Directive.Hold);
            Assert.IsTrue(hold.Effects.Any(e => e.Kind == OrderEffectKind.Soaked || e.Kind == OrderEffectKind.OutOfReach),
                "its receipt says what it met");
        }

        // ------------------------------------------------------------ points and nerve from the squad

        [Test]
        public void AParryGivesBackOnlyTheHoldsPointTheChainStopsAtTwiceAndGloryHalvesARepeat()
        {
            BattleSimulation sim = Cleaver("refund", 11, Side(U("s", UnitClass.Skirmisher), U("m", UnitClass.Marksman), U("h", UnitClass.Medic)));
            var session = new SimSession(sim);
            int[] percents = { 100, 150, 200 };
            var glory = new int[3];
            for (int n = 0; n < 3; n++)
            {
                Beat(session, sim, ChiefKits.CleaveName);
                Assert.AreEqual(percents[n], sim.NextParryPercent(0));
                int before = sim.Nerve(1);
                Assert.IsTrue(Order(session, "Hold"));
                session.Tick();
                Assert.AreEqual(ChiefKits.CleaveName, sim.Events.Last(e => e.Kind == BattleEventKind.Parry).Detail);
                Assert.GreaterOrEqual(before - sim.Nerve(1), ChiefKits.ParryNerve * percents[n] / 100, "parry " + (n + 1) + " cracked too little");
                Assert.AreEqual(ParryRules.SquadParryPoints,
                    sim.Events.Last(e => e.Kind == BattleEventKind.Earn && e.ActorSide == 0 && e.Detail == FightRules.ParryReason).Amount);
                glory[n] = sim.Events.Last(e => e.Kind == BattleEventKind.Glory).Amount;
            }
            Assert.AreEqual(ParryRules.SquadChainCapPercent, sim.NextParryPercent(0), "the chain stops at twice");
            Assert.AreEqual(ChiefKits.Glory(ChiefAnswer.Parry) * ChiefKits.RepeatPercent / 100, glory[1] - glory[0],
                "a second parry in a row climbs Glory half as much");
        }

        [Test]
        public void ASafeAnswerCracksHalfAParryOnABigBeatAndNothingOnASmallOne()
        {
            BattleSimulation cast = Fight("safebig", 5, Side(U("w", UnitClass.Warden), U("s", UnitClass.Skirmisher)),
                Side(U("x.s", UnitClass.Skirmisher), Chief(UnitClass.Marksman)), new SideRules(2, 300, false), FightRules.Campaign);
            var session = new SimSession(cast);
            Beat(session, cast, Directives.Name(Directive.Eclipse), 3);
            Assert.IsTrue(Order(session, "FallBack"));
            session.Tick();
            session.Tick();
            int before = cast.Nerve(1);
            session.Tick();
            Assert.AreEqual(Moves.Dodged, cast.Events.Last(e => e.Kind == BattleEventKind.Release && e.ActorSide == 1).Detail);
            Assert.AreEqual(HeavyRules.EclipseParryNerve * ParryRules.SquadSafePercent / 100, before - cast.Nerve(1), "half a parry");

            BattleSimulation band = Fight("safesmall", 3, Side(U("w", UnitClass.Warden), U("s", UnitClass.Skirmisher)), Band(), Heavies(), FightRules.Skill);
            session = new SimSession(band);
            Beat(session, band, HeavyRules.Heavy, 3);
            Assert.IsTrue(Order(session, "FallBack"));
            session.Tick();
            session.Tick();
            before = band.Nerve(1);
            session.Tick();
            Assert.AreEqual(Moves.Dodged, band.Events.Last(e => e.Kind == BattleEventKind.Release && e.ActorSide == 1).Detail);
            Assert.AreEqual(0, before - band.Nerve(1), "a small beat skipped cracks nothing");
        }

        [Test]
        public void CashingAMarkEarnsAPointAndCracksTheirNerve()
        {
            // A slow trickle here, so the point the mark earns shows.
            BattleSimulation sim = Fight("mark", 7, Ours(), Band(), Heavies(), FightRules.Campaign, null, 1000);
            var session = new SimSession(sim);
            for (int i = 0; i < 3; i++) Assert.IsTrue(Order(session, "FocusFire", "x.b"));
            string[] marks = { ClassSpells.PinDetail, ClassSpells.OpeningMark, ClassSpells.RipDetail };
            int seen = sim.Events.Count, drop = 0;
            BattleEvent cash = default;
            bool found = false;
            while (!found && !sim.IsFinished && sim.CurrentTick < BattleSetup.DefaultTickLimit)
            {
                int before = sim.Nerve(1);
                session.Tick();
                for (int i = seen; i < sim.Events.Count && !found; i++)
                {
                    BattleEvent e = sim.Events[i];
                    if (e.Kind != BattleEventKind.Cash || e.ActorSide != 0 || !marks.Contains(e.Detail)) continue;
                    cash = e;
                    found = true;
                    drop = before - sim.Nerve(1);
                }
                seen = sim.Events.Count;
            }
            Assert.IsTrue(found, "no mark was cashed");
            Assert.IsTrue(sim.Events.Any(e => e.Kind == BattleEventKind.Earn && e.ActorSide == 0 && e.Detail == ParryRules.MarkReason && e.Tick == cash.Tick),
                "a point for the " + cash.Detail);
            Assert.GreaterOrEqual(drop, ParryRules.MarkNerve, "and a crack on their nerve");
        }

        [Test]
        public void TheTrickleGivesAPointBackEveryEightSeconds()
        {
            BattleSimulation sim = Fight("trickle", 3, Side(U("w", UnitClass.Warden), U("s", UnitClass.Skirmisher)), Band(), Heavies(), FightRules.Skill,
                null, SliceRules.RefillTicks);
            var session = new SimSession(sim);
            for (int i = 0; i < 3; i++) Assert.IsTrue(Order(session, "Hold"));
            var trickled = new List<long>();
            while (trickled.Count < 2 && !sim.IsFinished && sim.CurrentTick < BattleSetup.DefaultTickLimit)
            {
                int before = sim.DirectivePoints(0), seen = sim.Events.Count;
                session.Tick();
                bool earned = false;
                for (int i = seen; i < sim.Events.Count; i++) earned |= sim.Events[i].Kind == BattleEventKind.Earn && sim.Events[i].ActorSide == 0;
                if (sim.DirectivePoints(0) > before && !earned) trickled.Add(sim.CurrentTick);
            }
            Assert.AreEqual(2, trickled.Count);
            int every = SliceRules.RefillTicks * ParryRules.SquadTrickleFactor;
            Assert.AreEqual(8 * SimClock.TicksPerSecond, every);
            Assert.AreEqual(0, trickled[0] % every);
            Assert.AreEqual(every, trickled[1] - trickled[0]);
        }

        // ------------------------------------------------------------ learning, slower and on one move

        [Test]
        public void ALearnedWaitOutActsOnlyOnItsMoveAndComesAtLeastASecondLate()
        {
            BattleSimulation sim = Cleaver("waitmove", 11, Side(U("s", UnitClass.Skirmisher), U("m", UnitClass.Marksman), U("h", UnitClass.Medic)),
                Learned(Habit.WaitOutParry, ChiefKits.CleaveName));
            var session = new SimSession(sim);
            Threat swing = Beat(session, sim, HeavyRules.Heavy);
            Assert.IsFalse(swing.WaitsOutHold, "his band's swing never learned it");
            Assert.IsTrue(Order(session, "Hold"));
            session.Tick();
            Assert.AreEqual(ParryRules.HeavyName, sim.Events.Last(e => e.Kind == BattleEventKind.Parry).Detail);
            Threat cleave = Beat(session, sim, ChiefKits.CleaveName);
            Assert.IsTrue(cleave.WaitsOutHold);
            Assert.IsFalse(cleave.ParryWindow, "a Hold now would be waited out, so the ring stays empty");
            Assert.IsTrue(Order(session, "Hold"));
            session.Tick();
            BattleEvent wait = sim.Events.Last(e => e.Kind == BattleEventKind.Reaction);
            Assert.AreEqual(BattleSimulation.WaitOutDetail, wait.Detail);
            Assert.GreaterOrEqual(wait.Amount, ParryRules.WaitOutMinTicks, "late enough for a Focus Fire or a Fall Back");
            OrderResult hold = sim.OrderResults(0).Last(r => r.Order == Directive.Hold);
            Assert.IsTrue(hold.Effects.Any(e => e.Kind == OrderEffectKind.WaitedOut && e.Move == ChiefKits.CleaveName), "the Hold's receipt says so");
        }

        [Test]
        public void AFeintAndAWaitOutNeverLandOnTheSameBeat()
        {
            TacticsProfile both = Learned(Habit.Feint, HeavyRules.Heavy);
            both.SetWaitOutParry(5);
            both.SetWaitOutMove(HeavyRules.Heavy);
            BattleSimulation sim = Fight("both", 3, Side(U("w", UnitClass.Warden), U("s", UnitClass.Skirmisher)), Band(), Heavies(), FightRules.Skill, both);
            var session = new SimSession(sim);
            Threat swing = Beat(session, sim, HeavyRules.Heavy);
            Assert.IsTrue(swing.FakeFirst);
            Assert.IsFalse(swing.WaitsOutHold);
            Assert.IsFalse(swing.ParryWindow, "the fake beat never fills the ring");
            Assert.AreEqual(swing.BeatTicks + ParryRules.FeintTicks, swing.RealBeatTicks);
            Assert.IsTrue(Order(session, "Hold"));
            session.Tick();
            BattleEvent feint = sim.Events.Last(e => e.Kind == BattleEventKind.Reaction);
            Assert.AreEqual(ParryRules.FeintDetail, feint.Detail);
            Assert.IsTrue(sim.OrderResults(0).Last(r => r.Order == Directive.Hold).Effects.Any(e => e.Kind == OrderEffectKind.Feinted),
                "the Hold's receipt names the fake");
            Threat real = Beat(session, sim, HeavyRules.Heavy);
            Assert.IsFalse(real.FakeFirst);
            Assert.IsFalse(real.WaitsOutHold, "a beat that feinted never waits");
            Assert.IsTrue(real.ParryWindow);
            Assert.IsTrue(Order(session, "Hold"));
            session.Tick();
            Assert.AreEqual(ParryRules.HeavyName, sim.Events.Last(e => e.Kind == BattleEventKind.Parry).Detail);
            Assert.IsFalse(sim.Events.Any(e => e.Tick > feint.Tick && e.Detail == BattleSimulation.WaitOutDetail));
        }

        [Test]
        public void OneLessonAFightAndItSitsOnTheMoveYouParriedMost()
        {
            var mind = new TribeMind("x", "x");
            BattleSimulation first = Cleaver("learn1", 11, Ours());
            ParryEverything(first);
            FightRecord record = FightRecord.From(first);
            Assert.IsTrue(record.SquadFirst);
            List<Lesson> raised = Raised(LessonRules.Learn(mind, record, 1));
            Assert.AreEqual(1, raised.Count, "one new lesson a fight");
            Lesson wait = raised[0];
            Assert.AreEqual(LessonRules.Parry, wait.Rule, "you leaned on Hold");
            Assert.AreEqual(ChiefKits.CleaveName, wait.Move, "his Cleave, a big beat, before his band's swings");
            Assert.AreEqual(ChiefKits.CleaveName, mind.WaitOutMove);
            Assert.AreEqual(wait.Evidence, LessonRules.Evidence(wait.Rule, record, 1, wait.Subject, wait.Move), "a replay bears it out");

            BattleSimulation second = Cleaver("learn2", 12, Ours());
            ParryEverything(second);
            FightRecord again = FightRecord.From(second);
            raised = Raised(LessonRules.Learn(mind, again, 1));
            Assert.AreEqual(1, raised.Count);
            Lesson feint = raised[0];
            Assert.AreEqual(LessonRules.FeintRule, feint.Rule);
            Assert.AreEqual(ParryRules.HeavyName, feint.Move, "never the move the wait out sits on");
            Assert.AreEqual(ParryRules.HeavyName, mind.FeintMove);
            Assert.AreEqual(ChiefKits.CleaveName, mind.WaitOutMove);
            Assert.AreEqual(feint.Evidence, LessonRules.Evidence(feint.Rule, again, 1, feint.Subject, feint.Move));
            StringAssert.Contains("(" + ChiefKits.CleaveName + ")", mind.HabitsText(), "his habits name their moves");
        }

        [Test]
        public void AChiefLearnsOnlyFromFightsAgainstHim()
        {
            const ulong seed = 4610;
            // Held at rules 42: from rules 43 a one row road is all the champion's fights, which this is not about.
            var tuning = new CampaignTuning { PathFights = 1, RulesCap = Campaign.LeadersRules - 1 };
            Campaign c = Campaign.New(seed, tuning);
            Assert.IsTrue(CampaignPilot.PlayTutorial(c, PilotStyle.Reference, seed, 10), "the tutorial was never won");
            c.ChooseTribe(CampaignTribes.BloodridgeId);
            TribeMind chief = c.ChiefMind(CampaignTribes.BloodridgeId), tribe = c.Mind(CampaignTribes.BloodridgeId);
            Assert.AreNotSame(chief, tribe);
            string before = chief.Snapshot();
            int roads = 0;
            for (int guard = 0; guard < 10 && c.Offers.Count > 0 && c.Offers.All(o => o.Kind != EncounterKind.Chief); guard++)
            {
                Assert.AreSame(tribe, c.MindFor(c.Offers[0]), "a road fight brings the tribe's mind");
                CampaignFight road = c.StartEncounter(0);
                Assert.AreEqual(Campaign.LeadersRules - 1, road.RulesVersion);
                EncounterKind kind = road.Encounter.Kind;
                CampaignPilot.PlayFight(c, road, new CampaignPilot(PilotStyle.Reference, seed));
                Assert.AreEqual(before, chief.Snapshot(), "a road fight never arms the chief");
                if (kind != EncounterKind.Beast && kind != EncounterKind.Ambush && kind != EncounterKind.AlphaHunt)
                {
                    Assert.IsTrue(tribe.HasLearnedFrom(road.Setup.FightId), "it teaches the tribe");
                    roads++;
                }
            }
            Assert.Greater(roads, 0, "no road fight came before the chief");
            int at = c.Offers.ToList().FindIndex(o => o.Kind == EncounterKind.Chief);
            Assert.GreaterOrEqual(at, 0, "the chief is not on offer");
            Assert.AreSame(chief, c.MindFor(c.Offers[at]), "his fight brings his own mind");
            CampaignFight fight = c.StartEncounter(at);
            TacticsProfile his = fight.Setup.CopyTactics(1);
            Assert.AreEqual(chief.HabitValue(Habit.CoverFocused), his.CoverFocused);
            Assert.AreEqual(chief.HabitValue(Habit.HoldOnUnleash), his.HoldOnUnleash);
            Assert.AreEqual(chief.HabitValue(Habit.PressOnFallBack), his.PressOnFallBack);
            Assert.AreEqual(chief.HabitValue(Habit.WaitOutParry), his.WaitOutParry);
            Assert.AreEqual(chief.HabitValue(Habit.Feint), his.Feint);
            Assert.LessOrEqual(c.TribeLearning(fight).Count, 1, "the card shows the one lesson he would take");
            CampaignPilot.PlayFight(c, fight, new CampaignPilot(PilotStyle.Reference, seed));
            Assert.IsTrue(chief.HasLearnedFrom(fight.Setup.FightId), "his fight teaches him");
            Assert.IsFalse(tribe.HasLearnedFrom(fight.Setup.FightId), "and only him");
            Assert.LessOrEqual(Raised(chief.Lessons).Count, 1, "one lesson a fight");
        }

        [Test]
        public void TheHuntersWaitOutSitsOnTheirSwingsUnderTheSquadFirst()
        {
            Campaign c = Campaign.New(920, new CampaignTuning());
            TribeMind hunters = c.Mind(AlphaHunt.Id);
            Assert.IsTrue(hunters.HasHabit(Habit.WaitOutParry), "they know your parry from the start (14 Oct 2026)");
            Assert.AreEqual(ParryRules.HeavyName, hunters.WaitOutMove, "on their heavy swings");
        }

        // ------------------------------------------------------------ honest reads

        [Test]
        public void AHoldOutOfReachSaysSoAndCostsNothingMore()
        {
            // Only your back row stands: a Hold covers nobody the band can swing at.
            BattleSimulation sim = Fight("reach", 3, Side(U("m", UnitClass.Marksman), U("h", UnitClass.Medic)), Band(), Heavies(), FightRules.Skill);
            var session = new SimSession(sim);
            Beat(session, sim, HeavyRules.Heavy, 20);
            Assert.IsTrue(Order(session, "Hold"));
            BattleEvent release = Next(session, sim, e => e.Kind == BattleEventKind.Release && e.ActorSide == 1);
            Assert.AreEqual(Moves.Full, release.Detail, "it lands in full on the back row");
            Assert.IsFalse(sim.Events.Any(e => e.Detail == ParryRules.BlockDetail), "and costs no block");
            OrderResult hold = sim.OrderResults(0).Last(r => r.Order == Directive.Hold);
            while (!hold.Final && !sim.IsFinished)
            {
                session.Tick();
                hold = sim.OrderResults(0).Last(r => r.Order == Directive.Hold && r.Tick == hold.Tick);
            }
            Assert.IsTrue(hold.Effects.Any(e => e.Kind == OrderEffectKind.OutOfReach && e.Move == HeavyRules.Heavy));
            Assert.AreEqual(OrderMiss.OutOfReach, hold.Miss);
            Assert.AreEqual(HeavyRules.Heavy, hold.MissMove);
        }

        [Test]
        public void ARefusedOrderKeepsItsReason()
        {
            BattleSimulation sim = Fight("refused", 3, Side(U("w", UnitClass.Warden), U("s", UnitClass.Skirmisher)), Band(), Heavies(), FightRules.Skill);
            var session = new SimSession(sim);
            Assert.IsTrue(sim.LastRejection(0).IsNone);
            Assert.IsFalse(session.TryIssue(Directives.CommandKind, "0", "nobody", 0, 0, "FocusFire", out string why));
            OrderRejection refused = sim.LastRejection(0);
            Assert.AreEqual(Directive.FocusFire, refused.Order);
            Assert.AreEqual("nobody", refused.TargetId);
            Assert.AreEqual(why, refused.Reason);
            Assert.AreEqual(sim.CurrentTick, refused.Tick);

            Campaign c = Campaign.New(4600, new CampaignTuning { PathFights = 0 });
            Assert.IsTrue(CampaignPilot.PlayTutorial(c, PilotStyle.Reference, 4600, 10));
            c.ChooseTribe(CampaignTribes.BloodridgeId);
            CampaignFight fight = c.StartEncounter(0);
            Assert.IsFalse(fight.TryOrder(Directive.Frenzy, null, out string gone));
            Assert.AreEqual(Directive.Frenzy, fight.LastRejection.Order);
            Assert.AreEqual(gone, fight.LastRejection.Reason, "the campaign's own reasons are kept too");
        }

        // ------------------------------------------------------------ rules version 9

        [Test]
        public void EveryCampaignFightFromRulesNinePlaysTheSquadFirst()
        {
            Assert.GreaterOrEqual(Campaign.RulesVersion, Campaign.SquadFirstRules);
            Campaign c = Campaign.New(4600, new CampaignTuning { PathFights = 0 });
            Assert.IsTrue(CampaignPilot.PlayTutorial(c, PilotStyle.Reference, 4600, 10));
            c.ChooseTribe(CampaignTribes.BloodridgeId);
            CampaignFight fight = c.StartEncounter(0);
            Assert.AreEqual(Campaign.RulesVersion, fight.RulesVersion);
            Assert.IsTrue(fight.Setup.Fight.SquadFirst);
            Assert.IsTrue(fight.Sim.SquadFirstOn);
            Assert.AreEqual(ParryRules.SquadSwingTicks, fight.Setup.Rules(1).HeavyEveryTicks, "his band swings every 5 s");
            for (int i = 0; i < 200 && !fight.Sim.IsFinished; i++) fight.Session.Tick();
            string checksum = StateChecksum.Compute(fight.Sim.OrderedStateSnapshot);
            Campaign back = CampaignSave.Read(CampaignSave.Write(c), new CampaignTuning { PathFights = 0 });
            Assert.AreEqual(string.Empty, back.LoadNote);
            Assert.AreEqual(checksum, StateChecksum.Compute(back.Active.Sim.OrderedStateSnapshot), "it saves and plays back exactly mid fight");
        }

        [Test]
        public void AChiefsOwnMindAndItsMovesSurviveASaveAndHisFightPlaysBack()
        {
            Campaign c = Campaign.New(4600, new CampaignTuning { PathFights = 0 });
            Assert.IsTrue(CampaignPilot.PlayTutorial(c, PilotStyle.Reference, 4600, 10));
            c.ChooseTribe(CampaignTribes.BloodridgeId);
            TribeMind chief = c.ChiefMind(CampaignTribes.BloodridgeId);
            // Teach his own mind a lesson on a move, from a fight under the squad first.
            BattleSimulation taught = Cleaver("teach", 11, Ours());
            ParryEverything(taught);
            Lesson lesson = Raised(LessonRules.Learn(chief, FightRecord.From(taught), 1)).Single();
            Assert.AreEqual(ChiefKits.CleaveName, lesson.Move);
            RematchClue clue = RematchClue.For(c, c.Offers[0]);
            Assert.IsNotNull(clue, "the card before his fight shows it");
            Assert.AreEqual(ChiefKits.CleaveName, clue.Move);

            string text = CampaignSave.Write(c);
            StringAssert.Contains("chiefmind\t" + CampaignTribes.BloodridgeId, text);
            StringAssert.Contains("habitmove\t" + (int)Habit.WaitOutParry + "\t" + ChiefKits.CleaveName, text);
            Campaign back = CampaignSave.Read(text);
            Assert.AreEqual(chief.Snapshot(), back.ChiefMind(CampaignTribes.BloodridgeId).Snapshot(), "his mind comes back as it was");
            Assert.AreEqual(ChiefKits.CleaveName, back.ChiefMind(CampaignTribes.BloodridgeId).Lessons.Last(l => l.Rule == LessonRules.Parry).Move);

            CampaignFight fight = c.StartEncounter(0);
            Assert.AreEqual(ChiefKits.CleaveName, fight.Setup.CopyTactics(1).WaitOutMove, "his fight brings it");
            CampaignPilot.PlayFight(c, fight, new CampaignPilot(PilotStyle.Reference, 4600));
            Assert.IsTrue(c.Archive.TryReplay(fight.Setup.FightId, out _, out string failure), failure);
            Campaign again = CampaignSave.Read(CampaignSave.Write(c));
            Assert.IsTrue(again.Archive.TryReplay(fight.Setup.FightId, out _, out failure), "and from its saved evidence: " + failure);
        }
    }
}
