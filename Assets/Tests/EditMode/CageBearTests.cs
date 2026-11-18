using System;
using System.Collections.Generic;
using System.Linq;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using NUnit.Framework;

namespace KindredSiege.Sim.Tests
{
    /// The cage bear beside Skullpeak's road (Jacob, 22 Oct 2026: "cage bear bro looks and feels awesome", then "go, build the
    /// Cage Bear as planned"; BeastKits, CampaignBeast): its claw, maw, ribcage and heart, its two phases, its trophies and
    /// Skullpeak's pieces. One quick test per piece, only to prove it runs. Nothing here is tuning.
    public class CageBearTests
    {
        private static UnitSpec U(string id, UnitClass unitClass)
        {
            return new UnitSpec(id, unitClass);
        }

        private static UnitSpec Pal(string id)
        {
            return new UnitSpec(id, UnitClass.Marksman, Row.Back, UnitRole.Pal, 100, 100);
        }

        private static List<UnitSpec> Side(params UnitSpec[] units)
        {
            return new List<UnitSpec>(units);
        }

        // A full squad behind its leader, as the campaign fields it.
        private static List<UnitSpec> Squad()
        {
            return Side(Pal("leader"), U("w", UnitClass.Warden), U("s", UnitClass.Skirmisher), U("m", UnitClass.Marksman),
                U("t", UnitClass.Trapper), U("d", UnitClass.Medic));
        }

        // The bear's four parts as the campaign makes them, each this percent as tough, in its order: claw, maw, ribcage, heart.
        private static List<UnitSpec> Bear(string id, int claw = 500, int maw = 500, int ribcage = 500, int heart = 500)
        {
            var parts = new List<UnitSpec>();
            int[] percent = { claw, maw, ribcage, heart };
            IReadOnlyList<BeastPart> order = BeastKits.Parts(BeastKit.CageBear);
            for (int p = 0; p < order.Count; p++)
                parts.Add(new UnitSpec(id + ".bear." + BeastKits.Name(order[p]), UnitClass.Warden, BeastKits.RowOf(order[p]), UnitRole.Fighter,
                    100, Math.Max(1, percent[p] * BeastKits.HealthPercent(order[p]) / 100)));
            return parts;
        }

        private static BattleSimulation Fight(string id, ulong seed, List<UnitSpec> ours, List<UnitSpec> bear,
            int tickLimit = BattleSetup.DefaultTickLimit, bool nerve = true)
        {
            return new BattleSimulation(new BattleSetup(id, seed, ours, bear, new TacticsProfile(), new TacticsProfile(),
                tickLimit, BattleSetup.DefaultDirectivePoints, nerve ? NerveRules.On() : NerveRules.Off, 10, 400, new SideRules(1, 0, true),
                new SideRules(0, 0, false, beast: BeastKit.CageBear), FightRules.CampaignBeast));
        }

        private static bool Order(SimSession session, string directive, string target = "")
        {
            return session.TryIssue(Directives.CommandKind, "0", target, 0, 0, directive, out _);
        }

        // Ticks until an event matches, calling before ahead of each tick; events from before the call are not looked at.
        private static BattleEvent? Next(BattleSimulation sim, SimSession session, Func<BattleEvent, bool> match, Action before = null,
            long limit = BattleSetup.DefaultTickLimit)
        {
            int seen = sim.Events.Count;
            while (!sim.IsFinished && sim.CurrentTick < limit)
            {
                before?.Invoke();
                session.Tick();
                for (; seen < sim.Events.Count; seen++)
                    if (match(sim.Events[seen])) return sim.Events[seen];
            }
            for (; seen < sim.Events.Count; seen++)
                if (match(sim.Events[seen])) return sim.Events[seen];
            return null;
        }

        private static bool Lands(BattleEvent e, string move)
        {
            return e.Kind == BattleEventKind.Land && e.Detail.StartsWith(move + ".");
        }

        private static string Landed(string move, string how)
        {
            return move + "." + how;
        }

        // ------------------------------------------------------------ the fight

        [Test]
        public void TheBearFightsWithItsFourPartsAndHasNoNerve()
        {
            var sim = Fight("bear.parts", 7, Squad(), Bear("bear.parts"));
            var session = new SimSession(sim);
            Assert.AreEqual(BeastKit.CageBear, sim.Beast(1));
            Assert.IsNull(sim.Part(BeastPart.Jaw), "a shark's part is no part of the bear");
            IReadOnlyList<BeastPart> parts = BeastKits.Parts(BeastKit.CageBear);
            CollectionAssert.AreEqual(new[] { BeastPart.Claw, BeastPart.Maw, BeastPart.Ribcage, BeastPart.Heart }, parts);
            foreach (BeastPart part in parts)
            {
                BattleUnit unit = sim.Part(part);
                StringAssert.EndsWith(".bear." + BeastKits.Name(part), unit.Id);
                Assert.AreEqual(BeastKits.RowOf(part), unit.Row);
                Assert.IsTrue(sim.TryPartOf(unit, out BeastPart back));
                Assert.AreEqual(part, back);
            }
            Assert.AreEqual(Row.Back, sim.Part(BeastPart.Heart).Row, "the heart sits behind the rest");
            Assert.IsTrue(sim.BeastCaged);

            // Unleash's scare does nothing to it: it has no nerve.
            Assert.IsTrue(Order(session, "Unleash"));
            for (int i = 0; i < 60; i++) session.Tick();
            Assert.AreEqual(NerveRules.Full, sim.Nerve(1));

            // Its claw, maw and heart tell their moves and land them; the ribcage has none.
            var told = new List<string>();
            var landed = new List<string>();
            while (!sim.IsFinished && sim.CurrentTick < 20 * SimClock.TicksPerSecond && landed.Count < 3)
            {
                int seen = sim.Events.Count;
                Assert.AreEqual(0, sim.BeastNextTicks(BeastPart.Ribcage));
                Assert.AreEqual(0, sim.BeastTellTicks(BeastPart.Ribcage));
                session.Tick();
                for (int i = seen; i < sim.Events.Count; i++)
                {
                    BattleEvent e = sim.Events[i];
                    if (e.Kind == BattleEventKind.Tell && !told.Contains(e.Detail)) told.Add(e.Detail);
                    if (e.Kind == BattleEventKind.Land)
                    {
                        string move = e.Detail.Substring(0, e.Detail.IndexOf('.'));
                        if (!landed.Contains(move)) landed.Add(move);
                    }
                }
            }
            CollectionAssert.AreEquivalent(new[] { BeastKits.GrabName, BeastKits.RoarName, BeastKits.PulseName }, told);
            CollectionAssert.AreEquivalent(told, landed);
            Assert.AreEqual(NerveRules.Full, sim.Nerve(1), "nothing breaks its nerve");
        }

        [Test]
        public void TheRibcageHalvesEveryHitAndUnleashCracksIt()
        {
            // A Skirmisher whose blades bleed (4% a second), so each second's bleeding is an exact number to check.
            var cutter = new UnitSpec("s", UnitClass.Skirmisher, Row.Front, UnitRole.Fighter, 100, 100, 0, 0, 0, 0, 0, 0, 0, 0, 4);
            var sim = Fight("bear.cage", 3, Side(Pal("leader"), U("w", UnitClass.Warden), cutter), Bear("bear.cage"));
            var session = new SimSession(sim);
            BattleUnit ribcage = sim.Part(BeastPart.Ribcage);
            int second = Math.Max(1, ribcage.MaxHp * 4 / 100);
            bool Bleeds(BattleEvent e) => e.Kind == BattleEventKind.Hit && e.TargetId == ribcage.Id && e.Detail == BleedRules.Detail;

            BattleEvent plain = Next(sim, session, Bleeds,
                () => { if (sim.FocusTicksLeft(0) == 0 && sim.DirectivePoints(0) > 0) Order(session, "FocusFire", ribcage.Id); }).Value;
            Assert.AreEqual(Math.Max(1, second * BeastKits.CagePercent / 100), plain.Amount, "the cage halves every hit, its own too");

            // While your Unleash runs, hits on the ribcage land twice as hard as that.
            BattleEvent unleashed = Next(sim, session, e => Bleeds(e) && sim.UnleashTicksLeft(0) > 0, () =>
            {
                if (sim.UnleashTicksLeft(0) == 0 && sim.DirectivePoints(0) >= Directives.Cost(Directive.Unleash)) Order(session, "Unleash");
                if (sim.FocusTicksLeft(0) == 0 && sim.DirectivePoints(0) > 0) Order(session, "FocusFire", ribcage.Id);
            }).Value;
            Assert.AreEqual(Math.Max(1, second * BeastKits.CagePercent / 100 * BeastKits.CageUnleashPercent / 100), unleashed.Amount);
        }

        [Test]
        public void TheHeartCannotBeHitWhileItsRibcageStands()
        {
            var sim = Fight("bear.heart", 5, Squad(), Bear("bear.heart", ribcage: 20));
            var session = new SimSession(sim);
            BattleUnit heart = sim.Part(BeastPart.Heart);
            BattleUnit marksman = sim.FindById("m");
            Assert.AreEqual(StrikeBlock.Caged, sim.CanStrike(marksman, heart));
            Assert.IsFalse(Order(session, "FocusFire", heart.Id), "Focus Fire cannot reach it");

            string ribcage = sim.Part(BeastPart.Ribcage).Id;
            BattleEvent sever = Next(sim, session, e => e.Kind == BattleEventKind.Sever && e.TargetId == ribcage,
                () => { if (sim.FocusTicksLeft(0) == 0 && sim.DirectivePoints(0) > 0) Order(session, "FocusFire", ribcage); }).Value;
            Assert.IsFalse(sim.Events.Any(e => e.Kind == BattleEventKind.Hit && e.TargetId == heart.Id), "no blow reached it");
            Assert.IsFalse(sim.BeastCaged);

            // The cage is broken: now it can be hit and focused.
            Assert.AreEqual(StrikeBlock.None, sim.CanStrike(marksman, heart));
            Assert.IsTrue(Order(session, "FocusFire", heart.Id));
            Assert.IsNotNull(Next(sim, session, e => e.Kind == BattleEventKind.Hit && e.TargetId == heart.Id, limit: sever.Tick + 600));
        }

        [Test]
        public void BreakItsHeartAndItDropsDeadKeepingOnlyThePartsYouBroke()
        {
            var sim = Fight("bear.dies", 13, Squad(), Bear("bear.dies", ribcage: 5, heart: 5));
            sim.RunUntilFinished();
            Assert.AreEqual(0, sim.Winner);
            Assert.AreEqual(FightEnd.Wipe, sim.Ending.How);
            CollectionAssert.AreEquivalent(new[] { "ribcage", "heart" },
                sim.Events.Where(e => e.Kind == BattleEventKind.Sever).Select(e => e.Detail), "only the broken parts");
            CollectionAssert.AreEquivalent(new[] { sim.Part(BeastPart.Claw).Id, sim.Part(BeastPart.Maw).Id },
                sim.Events.Where(e => e.Kind == BattleEventKind.Kill && e.Detail == BeastKits.HeartDetail).Select(e => e.TargetId),
                "the rest go down with the heart");
            Assert.IsTrue(BeastKits.Kills(BeastKit.CageBear, new[] { BeastPart.Ribcage, BeastPart.Heart }));
            Assert.IsFalse(BeastKits.Kills(BeastKit.CageBear, new[] { BeastPart.Claw, BeastPart.Maw, BeastPart.Ribcage }));
        }

        [Test]
        public void ItsClawGrabsWhoeverStandsAcrossAndHoldsThemUntilItBreaks()
        {
            // Only the Skirmisher stands in front, so the claw reaches for her, and no Warden covers her.
            var sim = Fight("bear.grab", 9, Side(Pal("leader"), U("s", UnitClass.Skirmisher), U("m", UnitClass.Marksman), U("d", UnitClass.Medic)),
                Bear("bear.grab", claw: 30));
            var session = new SimSession(sim);
            BattleEvent tell = Next(sim, session, e => e.Kind == BattleEventKind.Tell && e.Detail == BeastKits.GrabName).Value;
            Assert.AreEqual("s", tell.TargetId);
            Assert.AreEqual("s", sim.BeastTellTargetId(BeastPart.Claw));
            BattleEvent grab = Next(sim, session, e => Lands(e, BeastKits.GrabName)).Value;
            Assert.AreEqual(Landed(BeastKits.GrabName, BeastKits.Full), grab.Detail);
            Assert.AreEqual("s", sim.BeastHeldId);

            // In the claw she is out of the fight: she does nothing, nobody treats her, and the claw crushes her each second.
            BattleUnit s = sim.FindById("s");
            string claw = sim.Part(BeastPart.Claw).Id;
            BattleEvent sever = Next(sim, session, e => e.Kind == BattleEventKind.Sever && e.TargetId == claw,
                () => { if (sim.FocusTicksLeft(0) == 0 && sim.DirectivePoints(0) > 0) Order(session, "FocusFire", claw); }).Value;
            IEnumerable<BattleEvent> held = sim.Events.Where(e => e.Tick > grab.Tick && e.Tick < sever.Tick);
            Assert.IsFalse(held.Any(e => e.ActorId == "s" && (e.Kind == BattleEventKind.Hit || e.Kind == BattleEventKind.Aim)), "she does nothing");
            Assert.IsFalse(held.Any(e => e.Kind == BattleEventKind.Heal && e.TargetId == "s"), "nobody treats her");
            Assert.Greater(held.Count(e => e.Kind == BattleEventKind.Hit && e.TargetId == "s" && e.Detail == BeastKits.CrushDetail), 0);
            Assert.IsTrue(s.Alive, "the claw broke before she did");

            // The claw breaks and lets her go.
            Assert.IsTrue(sim.Events.Any(e => e.Kind == BattleEventKind.Reaction && e.Detail == BeastKits.ReleasedDetail && e.TargetId == "s" &&
                e.Tick == sever.Tick));
            Assert.AreEqual(string.Empty, sim.BeastHeldId);
        }

        [Test]
        public void FallBackGetsClearOfTheGrabAndACoveringWardenBlocksIt()
        {
            var sim = Fight("bear.dodge", 9, Side(Pal("leader"), U("s", UnitClass.Skirmisher), U("m", UnitClass.Marksman)), Bear("bear.dodge"));
            var session = new SimSession(sim);
            BattleEvent dodge = Next(sim, session, e => Lands(e, BeastKits.GrabName), () =>
            {
                int left = sim.BeastTellTicks(BeastPart.Claw);
                if (left > 0 && left <= 5 && sim.FallBackTicksLeft(0) == 0) Order(session, "FallBack");
            }).Value;
            Assert.AreEqual(Landed(BeastKits.GrabName, BeastKits.Dodged), dodge.Detail);
            Assert.AreEqual(string.Empty, sim.BeastHeldId);

            // A Warden behind her covers her, the only one of his side he can: he blocks the claw on his shield.
            var block = Fight("bear.block", 9, Side(new UnitSpec("w", UnitClass.Warden, Row.Back), U("s", UnitClass.Skirmisher)), Bear("bear.block"));
            BattleEvent shield = Next(block, new SimSession(block), e => Lands(e, BeastKits.GrabName)).Value;
            Assert.AreEqual(Landed(BeastKits.GrabName, BeastKits.Guarded), shield.Detail);
            Assert.AreEqual(string.Empty, block.BeastHeldId);
            Assert.IsTrue(block.Events.Any(e => e.Kind == BattleEventKind.Guard && e.ActorId == "w" && e.TargetId == "s"));
        }

        [Test]
        public void ItsRoarHitsYourNerveUnlessYouHoldOrFocusTheMaw()
        {
            int full = 0;
            {
                var sim = Fight("bear.roar", 7, Squad(), Bear("bear.roar"));
                var session = new SimSession(sim);
                BattleEvent roar = Next(sim, session, e => Lands(e, BeastKits.RoarName)).Value;
                Assert.AreEqual(Landed(BeastKits.RoarName, BeastKits.Full), roar.Detail);
                full = roar.Amount;
                Assert.AreEqual(BeastKits.RoarNerve, full);
                Assert.LessOrEqual(sim.Nerve(0), NerveRules.Full - BeastKits.RoarNerve);
            }
            {
                var sim = Fight("bear.roar", 7, Squad(), Bear("bear.roar"));
                var session = new SimSession(sim);
                BattleEvent roar = Next(sim, session, e => Lands(e, BeastKits.RoarName), () =>
                {
                    int left = sim.BeastTellTicks(BeastPart.Maw);
                    if (left > 0 && left <= 10 && sim.HoldTicksLeft(0) == 0) Order(session, "Hold");
                }).Value;
                Assert.AreEqual(Landed(BeastKits.RoarName, BeastKits.Blocked), roar.Detail, "a Hold blocks it");
            }
            {
                var sim = Fight("bear.roar", 7, Squad(), Bear("bear.roar"));
                var session = new SimSession(sim);
                string maw = sim.Part(BeastPart.Maw).Id;
                BattleEvent roar = Next(sim, session, e => Lands(e, BeastKits.RoarName), () =>
                {
                    if (sim.BeastTellTicks(BeastPart.Maw) > 0 && sim.FocusedId(0) != maw) Order(session, "FocusFire", maw);
                }).Value;
                Assert.AreEqual(Landed(BeastKits.RoarName, BeastKits.Cut), roar.Detail, "Focus Fire on the roaring maw cuts it off");
                Assert.IsTrue(sim.Events.Any(e => e.Kind == BattleEventKind.Earn && e.Tick == roar.Tick && e.Detail == FightRules.InterruptReason));
            }
        }

        [Test]
        public void ItsPulseMendsItWhileTheCageStandsThenHitsYourWholeSide()
        {
            var caged = Fight("bear.mend", 11, Squad(), Bear("bear.mend"));
            var session = new SimSession(caged);
            BattleEvent mend = Next(caged, session, e => Lands(e, BeastKits.PulseName)).Value;
            Assert.AreEqual(Landed(BeastKits.PulseName, BeastKits.Mended), mend.Detail);
            Assert.AreEqual(mend.Amount, caged.Events.Where(e => e.Kind == BattleEventKind.Heal && e.Tick == mend.Tick &&
                e.ActorId == caged.Part(BeastPart.Heart).Id).Sum(e => e.Amount), "it mends its other parts");

            // The cage is broken: the Pulse hits your whole side. The Medic heals through it, or a Hold soaks it.
            foreach (bool hold in new[] { false, true })
            {
                var sim = Fight("bear.pulse", 11, Squad(), Bear("bear.pulse", ribcage: 5));
                var s = new SimSession(sim);
                Assert.IsNotNull(Next(sim, s, e => e.Kind == BattleEventKind.Sever && e.Detail == "ribcage"));
                BattleEvent pulse = Next(sim, s, e => Lands(e, BeastKits.PulseName), () =>
                {
                    int left = sim.BeastTellTicks(BeastPart.Heart);
                    if (hold && left > 0 && left <= 10 && sim.HoldTicksLeft(0) == 0) Order(s, "Hold");
                }).Value;
                Assert.AreEqual(Landed(BeastKits.PulseName, hold ? BeastKits.Soaked : BeastKits.Healed), pulse.Detail);
                int hits = sim.Events.Count(e => e.Kind == BattleEventKind.Hit && e.Tick == pulse.Tick && e.Detail == BeastKits.PulseDetail);
                Assert.AreEqual(sim.Units.Count(u => u.Side == 0 && u.Alive && !u.IsLeader && u.Id != sim.BeastHeldId), hits,
                    "every fighter of yours, both rows");
            }
        }

        // ------------------------------------------------------------ its phases

        [Test]
        public void ItsSecondBrokenPartBringsOverdriveAndItsGripCrushesTwiceAsHard()
        {
            // Its maw and ribcage break fast; its claw and heart hold.
            var sim = Fight("bear.overdrive", 21, Squad(), Bear("bear.overdrive", claw: 800, maw: 5, ribcage: 5, heart: 800));
            var session = new SimSession(sim);
            BattleEvent phase = Next(sim, session, e => e.Kind == BattleEventKind.Phase).Value;
            Assert.AreEqual(BeastKits.OverdriveName, phase.Detail);
            Assert.AreEqual(2, sim.BeastPhase);
            Assert.LessOrEqual(sim.BeastNextTicks(BeastPart.Claw), BeastKits.GrabEveryTicks * BeastKits.FrenzyEveryPercent / 100,
                "its moves come a third faster");

            BattleEvent crush = Next(sim, session, e => e.Kind == BattleEventKind.Hit && e.Detail == BeastKits.CrushDetail).Value;
            BattleUnit held = sim.FindById(crush.TargetId);
            Assert.AreEqual(Math.Max(1, held.MaxHp * BeastKits.CrushPercent * BeastKits.OverdriveCrushPercent / 100 / 100), crush.Amount);
        }

        [Test]
        public void WithOnlyItsHeartLeftItPulsesToTheDeathAndNeverRuns()
        {
            var sim = Fight("bear.last", 23, Squad(), Bear("bear.last", claw: 5, maw: 5, ribcage: 5, heart: 3000), 1500);
            var session = new SimSession(sim);
            BattleEvent last = Next(sim, session, e => e.Kind == BattleEventKind.Phase && e.Detail == BeastKits.LastStandName).Value;
            Assert.AreEqual(3, sim.BeastPhase);
            Assert.AreEqual(sim.Part(BeastPart.Heart).Id, last.ActorId);
            Assert.LessOrEqual(sim.BeastNextTicks(BeastPart.Heart), BeastKits.LastStandPulseTicks);
            Assert.AreEqual(0, sim.BeastEscapeTicks, "it never digs away");

            // Too tough to break in time: the clock runs out and it holds its ground.
            sim.RunUntilFinished();
            Assert.AreEqual(1, sim.Winner, "it never runs, so you are driven off");
            Assert.AreEqual(FightEnd.TimeUp, sim.Ending.How);
            Assert.IsFalse(sim.Events.Any(e => e.Kind == BattleEventKind.Flee));
            Assert.Greater(sim.Events.Count(e => Lands(e, BeastKits.PulseName) && e.Tick > last.Tick), 1);
        }

        // ------------------------------------------------------------ the campaign

        private static Campaign OnSkullpeaksRoad(ulong seed, CampaignTuning tuning)
        {
            Campaign c = Campaign.New(seed, tuning);
            Assert.IsTrue(CampaignPilot.PlayTutorial(c, PilotStyle.Reference, seed, 10), "the tutorial was never won");
            c.ChooseTribe(CampaignTribes.SkullpeakId);
            Assert.AreEqual(CampaignStage.InRun, c.Stage);
            return c;
        }

        [Test]
        public void TheCageBearWaitsBesideSkullpeaksRoad()
        {
            Campaign c = OnSkullpeaksRoad(6100, new CampaignTuning { RulesCap = Campaign.PitRules });
            Assert.AreEqual(BeastKit.CageBear, c.RoadBeast);
            Encounter bear = c.BeastOffer;
            Assert.IsNotNull(bear);
            Assert.AreEqual(EncounterKind.Beast, bear.Kind);
            Assert.AreEqual(BeastKit.CageBear, bear.Beast);
            Assert.AreEqual("The cage bear", bear.Title);
            Assert.IsFalse(c.Offers.Any(o => o.Kind == EncounterKind.Beast), "it is not one of the road's offers");
            Assert.IsTrue(c.CanStartBeast(out string why), why);

            CampaignFight fight = c.StartBeast();
            Assert.AreEqual(BeastKit.CageBear, fight.Sim.Beast(1));
            BattleUnit claw = fight.Sim.Part(BeastPart.Claw);
            StringAssert.EndsWith("." + Campaign.BearId + ".claw", claw.Id);
            Assert.AreEqual("Cage bear claw", fight.NameOf(claw.Id));

            // The first road still has the shark.
            Campaign first = Campaign.New(6100, new CampaignTuning { RulesCap = Campaign.PitRules });
            Assert.IsTrue(CampaignPilot.PlayTutorial(first, PilotStyle.Reference, 6100, 10));
            first.StartRun();
            Assert.AreEqual(BeastKit.SandShark, first.RoadBeast);
            Assert.AreEqual(BeastKit.SandShark, first.BeastOffer.Beast);
        }

        [Test]
        public void FixtureAWinSendsHomeOnlyThePartsYouBroke()
        {
            // A weak bear, so the bot breaks its heart.
            var t = new CampaignTuning { EnemyScaleTier2 = 25, RulesCap = Campaign.PitRules };
            for (ulong seed = 6200; seed < 6240; seed++)
            {
                Campaign c = OnSkullpeaksRoad(seed, t);
                int step = c.Step;
                CampaignFight fight = c.StartBeast();
                EncounterResult r = CampaignPilot.PlayFight(c, fight, new CampaignPilot(PilotStyle.Reference, seed));
                if (!r.Won) continue;

                Assert.IsTrue(r.BeastKilled, "a bear is only beaten by breaking its heart");
                CollectionAssert.Contains(r.PartsBroken, BeastPart.Heart);
                Assert.IsFalse(r.BeastFled);
                CollectionAssert.AreEqual(r.PartsBroken, r.Trophies);
                CollectionAssert.AreEqual(r.PartsBroken, c.Trophies, "the trophies are at camp");
                CollectionAssert.AreEqual(new[] { BeastKit.CageBear }, c.BeastsBeatenThisRun);
                Assert.IsTrue(c.BeastBeatenThisRun);
                Assert.IsNull(c.BeastOffer, "gone for the run");
                Assert.AreEqual(step, c.Step, "it costs no step of the road");
                foreach (BeastScar scar in r.ScarsEarned) Assert.AreEqual(BeastKits.ScarName(scar.Part), scar.Name);
                Assert.IsTrue(c.Archive.TryReplay(fight.Setup.FightId, out _, out string failure), failure);

                // The save keeps the trophies, the scars and the beaten bear, and the fight still plays back.
                Campaign read = CampaignSave.Read(CampaignSave.Write(c), t);
                Assert.IsEmpty(read.LoadNote);
                CollectionAssert.AreEqual(c.Trophies, read.Trophies);
                CollectionAssert.AreEqual(new[] { BeastKit.CageBear }, read.BeastsBeatenThisRun);
                Assert.IsNull(read.BeastOffer);
                foreach (CampaignFighter f in c.Reserve)
                    CollectionAssert.AreEqual(f.Scars, read.Reserve.First(x => x.Id == f.Id).Scars);
                Assert.IsTrue(read.Archive.TryReplay(fight.Setup.FightId, out _, out failure), failure);
                return;
            }
            Assert.Fail("the bot never beat a weak bear in forty campaigns");
        }

        [Test]
        public void TheButcherForgesItsTrophiesIntoSkullpeaksPieces()
        {
            Campaign camp = Campaign.New(6400);
            Assert.IsTrue(CampaignPilot.PlayTutorial(camp, PilotStyle.Reference, 6400, 10));
            // A claw and a heart at camp, and a shark piece from a version 16 save, written in as a won hunt would leave them.
            string text = CampaignSave.Write(camp);
            text = text.Replace("\nforge\t", "\ntrophy\t4\ntrophy\t7\nsharkpiece\t0\nforge\t");
            Campaign c = CampaignSave.Read(text);
            CollectionAssert.AreEqual(new[] { BeastPart.Claw, BeastPart.Heart }, c.Trophies);
            CollectionAssert.AreEqual(new[] { BeastPiece.JawShield }, c.BeastPiecesForged, "a version 16 shark piece still reads");

            Assert.IsFalse(c.CanForgeTrophy(BeastPart.Claw, BeastPiece.TowerShield, out _), "the claw makes the claw arm, the blade arm or the hook chain");
            Assert.IsTrue(c.TryForgeTrophy(BeastPart.Claw, BeastPiece.ClawArm, out string why), why);
            Assert.IsFalse(c.CanForgeTrophy(BeastPart.Heart, BeastPiece.ToothTotem, out _), "a bear's heart makes none of the shark's pieces");
            Assert.IsTrue(c.TryForgeTrophy(BeastPart.Heart, BeastPiece.InjectorVials, out why), why);
            CollectionAssert.AreEqual(new[] { BeastPiece.JawShield, BeastPiece.ClawArm, BeastPiece.InjectorVials }, c.BeastPiecesForged);
            Assert.IsEmpty(c.Trophies);

            // Every run after starts with them in the gear: Skullpeak's pieces, each on the first fighter of its class who
            // wears nothing.
            Campaign read = CampaignSave.Read(CampaignSave.Write(c));
            CollectionAssert.AreEqual(c.BeastPiecesForged, read.BeastPiecesForged);
            read.StartRun();
            CampaignGear vials = read.FindGear(BeastPieces.GearId(BeastPiece.InjectorVials));
            Assert.IsNotNull(vials);
            Assert.AreEqual("gear.bear.10", vials.Id);
            Assert.AreEqual(CampaignTribes.SkullpeakId, vials.TribeId);
            Assert.AreEqual(ClassBend.FieldKit, vials.Bend);
            Assert.AreEqual(UnitClass.Medic, read.Reserve.First(f => f.GearId == vials.Id).Class);
            CampaignGear arm = read.FindGear(BeastPieces.GearId(BeastPiece.ClawArm));
            Assert.AreEqual((int)UnitClass.Warden, arm.ClassIndex);
            Assert.AreEqual(ClassBend.Retaliator, arm.Bend);
        }

        [Test]
        public void ABearFightSavedHalfwayResumesExactly()
        {
            var t = new CampaignTuning { RulesCap = Campaign.PitRules };
            Campaign c = OnSkullpeaksRoad(6500, t);
            CampaignFight fight = c.StartBeast();
            var pilot = new CampaignPilot(PilotStyle.Reference, 6500);
            while (!fight.Over && fight.Sim.CurrentTick < BeastKits.GrabEveryTicks + BeastKits.GrabTellTicks + 60)
            {
                pilot.Step(fight);
                fight.Tick();
            }
            Assert.IsFalse(fight.Over);
            Campaign half = CampaignSave.Read(CampaignSave.Write(c), t);
            Assert.IsEmpty(half.LoadNote);
            Assert.AreEqual(EncounterKind.Beast, half.Active.Encounter.Kind);
            Assert.AreEqual(BeastKit.CageBear, half.Active.Encounter.Beast);
            Assert.AreEqual(fight.Session.Checksum, half.Active.Session.Checksum);
            Assert.AreEqual(fight.Sim.BeastHeldId, half.Active.Sim.BeastHeldId);
        }
    }
}
