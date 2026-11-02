using System;
using System.Collections.Generic;
using System.Linq;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using NUnit.Framework;

namespace KindredSiege.Sim.Tests
{
    /// The sand shark beside the first road, its trophies, the Butcher's trophy forge and road scars (Jacob, 20 Oct 2026,
    /// DESIGN_LOCKS "Beasts" and points 2 and 7; 21 Oct: "go, build the shark API with your six calls"; BeastKits,
    /// CampaignBeast). One quick test per piece, only to prove it runs, plus Codex's two fixed fixtures: a win with some parts
    /// broken, and a loss. Nothing here is tuning.
    public class BeastTests
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

        // The shark's four parts as the campaign makes them, this percent as tough.
        private static List<UnitSpec> Shark(string id, int healthPercent)
        {
            var parts = new List<UnitSpec>();
            for (int p = 0; p < BeastKits.PartCount; p++)
            {
                var part = (BeastPart)p;
                parts.Add(new UnitSpec(id + ".shark." + BeastKits.Name(part), UnitClass.Warden, BeastKits.RowOf(part), UnitRole.Fighter, 100,
                    Math.Max(1, healthPercent * BeastKits.HealthPercent(part) / 100)));
            }
            return parts;
        }

        private static BattleSimulation Fight(string id, ulong seed, List<UnitSpec> ours, int sharkHealthPercent = 100,
            int tickLimit = BattleSetup.DefaultTickLimit, bool nerve = true)
        {
            return Fight(id, seed, ours, Shark(id, sharkHealthPercent), tickLimit, nerve);
        }

        private static BattleSimulation Fight(string id, ulong seed, List<UnitSpec> ours, List<UnitSpec> shark,
            int tickLimit = BattleSetup.DefaultTickLimit, bool nerve = true)
        {
            return new BattleSimulation(new BattleSetup(id, seed, ours, shark, new TacticsProfile(), new TacticsProfile(),
                tickLimit, BattleSetup.DefaultDirectivePoints, nerve ? NerveRules.On() : NerveRules.Off, 10, 400, new SideRules(1, 0, true),
                new SideRules(0, 0, false, beast: BeastKit.SandShark), FightRules.CampaignBeast));
        }

        // The shark with each part this percent as tough, in BeastPart order.
        private static List<UnitSpec> Shark(string id, int jaw, int fin, int spine, int tail)
        {
            List<UnitSpec> parts = Shark(id, 100);
            int[] percent = { jaw, fin, spine, tail };
            for (int p = 0; p < BeastKits.PartCount; p++)
            {
                UnitSpec s = parts[p];
                parts[p] = new UnitSpec(s.Id, s.Class, s.Row, s.Role, 100, Math.Max(1, percent[p] * BeastKits.HealthPercent((BeastPart)p) / 100));
            }
            return parts;
        }

        // A full squad behind its leader, as the campaign fields it.
        private static List<UnitSpec> Squad()
        {
            return Side(Pal("leader"), U("w", UnitClass.Warden), U("s", UnitClass.Skirmisher), U("m", UnitClass.Marksman),
                U("t", UnitClass.Trapper), U("d", UnitClass.Medic));
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

        private static string Landed(string move, string how)
        {
            return move + "." + how;
        }

        // ------------------------------------------------------------ the fight

        [Test]
        public void TheSharkFightsWithItsFourPartsAndHasNoNerve()
        {
            var sim = Fight("beast.parts", 7, Squad(), 500);
            var session = new SimSession(sim);
            Assert.IsTrue(sim.BeastOn);
            Assert.AreEqual(BeastKit.SandShark, sim.Beast(1));
            for (int p = 0; p < BeastKits.PartCount; p++)
            {
                var part = (BeastPart)p;
                BattleUnit unit = sim.Part(part);
                StringAssert.EndsWith("." + BeastKits.Name(part), unit.Id);
                Assert.AreEqual(BeastKits.RowOf(part), unit.Row);
                Assert.IsTrue(sim.TryPartOf(unit, out BeastPart back));
                Assert.AreEqual(part, back);
            }

            // Unleash's scare does nothing to it: it has no nerve.
            Assert.IsTrue(Order(session, "Unleash"));
            for (int i = 0; i < 60; i++) session.Tick();
            Assert.AreEqual(NerveRules.Full, sim.Nerve(1));

            // Every part tells its move and lands it; the Bite falls on your front row.
            var told = new List<string>();
            var landed = new List<string>();
            while (!sim.IsFinished && sim.CurrentTick < 30 * SimClock.TicksPerSecond && landed.Count < 4)
            {
                int seen = sim.Events.Count;
                session.Tick();
                for (int i = seen; i < sim.Events.Count; i++)
                {
                    BattleEvent e = sim.Events[i];
                    if (e.Kind == BattleEventKind.Tell && !told.Contains(e.Detail))
                    {
                        told.Add(e.Detail);
                        if (e.Detail == BeastKits.BiteName) Assert.AreEqual(Row.Front, sim.FindById(e.TargetId).Row);
                    }
                    if (e.Kind == BattleEventKind.Land)
                    {
                        string move = e.Detail.Substring(0, e.Detail.IndexOf('.'));
                        if (!landed.Contains(move)) landed.Add(move);
                    }
                }
            }
            CollectionAssert.AreEquivalent(new[] { BeastKits.BiteName, BeastKits.DiveName, BeastKits.VolleyName, BeastKits.SweepName }, told);
            CollectionAssert.AreEquivalent(told, landed);
            Assert.AreEqual(NerveRules.Full, sim.Nerve(1), "nothing breaks its nerve");
        }

        [Test]
        public void ABrokenPartLosesItsMoveAndCannotBeFocused()
        {
            var sim = Fight("beast.break", 11, Squad(), 40);
            var session = new SimSession(sim);
            string jaw = sim.Part(BeastPart.Jaw).Id;
            BattleEvent sever = Next(sim, session, e => e.Kind == BattleEventKind.Sever && e.TargetId == jaw,
                () => { if (sim.FocusTicksLeft(0) == 0 && sim.DirectivePoints(0) > 0) Order(session, "FocusFire", jaw); }).Value;
            Assert.AreEqual(BeastKits.Name(BeastPart.Jaw), sever.Detail);
            Assert.AreEqual(0, sever.ActorSide);
            Assert.IsTrue(sim.PartBroken(BeastPart.Jaw));
            Assert.AreEqual(0, sim.BeastTellTicks(BeastPart.Jaw));
            Assert.IsFalse(Order(session, "FocusFire", jaw), "a broken part cannot be focused");

            // Its Bite is gone for the rest of the fight.
            long after = sim.CurrentTick;
            while (!sim.IsFinished && sim.CurrentTick < after + 2 * BeastKits.BiteEveryTicks) session.Tick();
            Assert.IsFalse(sim.Events.Any(e => e.Tick > after && e.Kind == BattleEventKind.Tell && e.Detail == BeastKits.BiteName));
        }

        [Test]
        public void AWardenTakesTheBiteOnHisShieldAndTheJawTakesItBack()
        {
            // Only the Warden stands in front, so the Bite falls on him.
            var sim = Fight("beast.shield", 3, Side(Pal("leader"), U("w", UnitClass.Warden), U("m", UnitClass.Marksman), U("d", UnitClass.Medic)), 500);
            var session = new SimSession(sim);
            BattleEvent land = Next(sim, session, e => e.Kind == BattleEventKind.Land && e.Detail.StartsWith(BeastKits.BiteName)).Value;
            Assert.AreEqual(Landed(BeastKits.BiteName, BeastKits.Guarded), land.Detail);
            Assert.IsTrue(sim.Events.Any(e => e.Kind == BattleEventKind.Hit && e.Detail == BeastKits.BiteBackDetail && e.ActorId == "w" &&
                e.TargetId == sim.Part(BeastPart.Jaw).Id), "the jaw takes part of it back from his shield");
        }

        [Test]
        public void AHoldAsTheBiteLandsParriesIt()
        {
            var sim = Fight("beast.parry", 5, Squad(), 500);
            var session = new SimSession(sim);
            BattleEvent land = Next(sim, session, e => e.Kind == BattleEventKind.Land && e.Detail.StartsWith(BeastKits.BiteName), () =>
            {
                int left = sim.BeastTellTicks(BeastPart.Jaw);
                if (left > 0 && left <= 10 && sim.HoldTicksLeft(0) == 0) Order(session, "Hold");
            }).Value;
            Assert.AreEqual(Landed(BeastKits.BiteName, BeastKits.Parried), land.Detail);
            Assert.IsTrue(sim.Events.Any(e => e.Kind == BattleEventKind.Parry && e.TargetId == sim.Part(BeastPart.Jaw).Id));
        }

        [Test]
        public void AnArmedSnareCatchesTheDiveAndBlowsMostlyMissWhileItIsUnder()
        {
            var sim = Fight("beast.dive", 9, Squad(), 500);
            var session = new SimSession(sim);
            BattleUnit fin = sim.Part(BeastPart.Fin);
            BattleUnit skirmisher = sim.FindById("s");
            int above = sim.HitChance(skirmisher, fin);
            Assert.IsNotNull(Next(sim, session, e => e.Kind == BattleEventKind.Tell && e.Detail == BeastKits.DiveName));
            Assert.IsTrue(sim.BeastUnder);
            Assert.Less(sim.HitChance(skirmisher, fin), above);
            BattleEvent land = Next(sim, session, e => e.Kind == BattleEventKind.Land && e.Detail.StartsWith(BeastKits.DiveName)).Value;
            Assert.AreEqual(Landed(BeastKits.DiveName, BeastKits.Snared), land.Detail, "the Trapper's armed snare springs on it");
            Assert.Greater(fin.SnareTicks, 0, "it is held on top");
        }

        [Test]
        public void AScarredWardenTakesLessFromTheBite()
        {
            int Taken(bool scarred)
            {
                UnitSpec warden = U("w", UnitClass.Warden);
                var sim = Fight("beast.scar", 3, Side(Pal("leader"), scarred ? warden.WithScars(1) : warden, U("m", UnitClass.Marksman)), 500);
                var session = new SimSession(sim);
                return Next(sim, session, e => e.Kind == BattleEventKind.Land && e.Detail.StartsWith(BeastKits.BiteName)).Value.Amount;
            }
            Assert.Less(Taken(true), Taken(false), "Jawbreaker's Guard holds better against beasts");
        }

        [Test]
        public void BreakAllFourAndItDiesOrHoldOutAndItDivesAway()
        {
            var dies = Fight("beast.dies", 13, Squad(), 5);
            dies.RunUntilFinished();
            Assert.AreEqual(0, dies.Winner);
            Assert.AreEqual(FightEnd.Wipe, dies.Ending.How);
            Assert.AreEqual(BeastKits.PartCount, dies.Events.Count(e => e.Kind == BattleEventKind.Sever));

            var dives = Fight("beast.dives", 13, Squad(), 500, 300);
            dives.RunUntilFinished();
            Assert.AreEqual(0, dives.Winner, "it dives away and leaves you the field");
            Assert.AreEqual(FightEnd.TimeUp, dives.Ending.How);
        }

        // ------------------------------------------------------------ the audit's fixes (22 Oct 2026)

        [Test]
        public void ALoneLeaderIsHitAndHisFallLosesTheFight()
        {
            // Nerve off, so only falling can end it: once the one fighter falls, the shark goes for the leader.
            var sim = Fight("beast.alone", 1, Side(Pal("leader"), new UnitSpec("s", UnitClass.Skirmisher, Row.Front, UnitRole.Fighter, 100, 20)),
                2000, nerve: false);
            sim.RunUntilFinished();
            Assert.IsFalse(sim.FindById("s").Alive);
            Assert.IsFalse(sim.FindById("leader").Alive, "the leader is hit once nobody else stands");
            Assert.AreEqual(1, sim.Winner, "his fall loses the fight; the clock no longer gives it to you");
        }

        [Test]
        public void APausedHoldSoaksTheBiteButNeverParriesIt()
        {
            // No Warden in front, so a Hold that does not parry soaks.
            var sim = Fight("beast.paused", 5, Side(Pal("leader"), U("s", UnitClass.Skirmisher), U("m", UnitClass.Marksman)), 500);
            var session = new SimSession(sim);
            BattleEvent land = Next(sim, session, e => e.Kind == BattleEventKind.Land && e.Detail.StartsWith(BeastKits.BiteName), () =>
            {
                int left = sim.BeastTellTicks(BeastPart.Jaw);
                if (left > 0 && left <= 10 && sim.HoldTicksLeft(0) == 0)
                    session.TryIssue(Directives.CommandKind, "0", string.Empty, HeavyRules.PausedOrderArg, 0, "Hold", out _);
            }).Value;
            Assert.AreEqual(Landed(BeastKits.BiteName, BeastKits.Soaked), land.Detail);
        }

        [Test]
        public void TheSweepIsHealedThroughWhileTheMedicStands()
        {
            var sim = Fight("beast.heal", 9, Squad(), 500);
            var session = new SimSession(sim);
            BattleEvent land = Next(sim, session, e => e.Kind == BattleEventKind.Land && e.Detail.StartsWith(BeastKits.SweepName)).Value;
            Assert.AreEqual(Landed(BeastKits.SweepName, BeastKits.Healed), land.Detail, "the Medic stands and nobody falls to it");
        }

        // ------------------------------------------------------------ its phases (22 Oct 2026)

        [Test]
        public void ItsSecondBrokenPartBringsItsBloodFrenzy()
        {
            // The front two break fast; the back two hold.
            var sim = Fight("beast.frenzy", 21, Squad(), Shark("beast.frenzy", 10, 10, 800, 800));
            var session = new SimSession(sim);
            Assert.AreEqual(1, sim.BeastPhase);
            BattleEvent phase = Next(sim, session, e => e.Kind == BattleEventKind.Phase).Value;
            Assert.AreEqual(BeastKits.FrenzyName, phase.Detail);
            Assert.AreEqual(2, sim.Events.Count(e => e.Kind == BattleEventKind.Sever));
            Assert.AreEqual(2, sim.BeastPhase);
            Assert.LessOrEqual(sim.BeastNextTicks(BeastPart.Spine), BeastKits.VolleyEveryTicks * BeastKits.FrenzyEveryPercent / 100,
                "its moves come a third faster");

            // Every hit it lands now leaves its victim bleeding.
            BattleEvent wound = Next(sim, session, e => e.Kind == BattleEventKind.Mark && e.Detail == ClassSpells.BleedMark &&
                (e.ActorId == sim.Part(BeastPart.Spine).Id || e.ActorId == sim.Part(BeastPart.Tail).Id)).Value;
            Assert.Greater(sim.FindById(wound.TargetId).BleedPercent, 0);
        }

        [Test]
        public void InItsLastStandItDigsAwayUnlessYouBreakItsLastPart()
        {
            var sim = Fight("beast.last", 23, Squad(), Shark("beast.last", 10, 10, 10, 2000));
            var session = new SimSession(sim);
            BattleEvent last = Next(sim, session, e => e.Kind == BattleEventKind.Phase && e.Detail == BeastKits.LastStandName).Value;
            Assert.AreEqual(3, sim.BeastPhase);
            Assert.AreEqual(sim.Part(BeastPart.Tail).Id, last.ActorId, "its last part");
            Assert.AreEqual(BeastKits.LastStandTicks - 1, sim.BeastEscapeTicks, "fifteen seconds from the tick its third part broke");

            // The tail is too tough to break in time: it digs away, and you hold the field with three parts broken.
            sim.RunUntilFinished();
            Assert.AreEqual(0, sim.Winner);
            Assert.AreEqual(FightEnd.Escaped, sim.Ending.How);
            Assert.IsTrue(sim.Events.Any(e => e.Kind == BattleEventKind.Flee && e.Detail == BeastKits.EscapeDetail));
            Assert.AreEqual(3, sim.Events.Count(e => e.Kind == BattleEventKind.Sever));
            Assert.AreEqual(last.Tick + BeastKits.LastStandTicks, sim.EndTick);
        }

        // ------------------------------------------------------------ the campaign

        private static Campaign OnTheFirstRoad(ulong seed, CampaignTuning tuning)
        {
            Campaign c = Campaign.New(seed, tuning);
            Assert.IsTrue(CampaignPilot.PlayTutorial(c, PilotStyle.Reference, seed, 10), "the tutorial was never won");
            c.StartRun();
            Assert.AreEqual(CampaignStage.InRun, c.Stage);
            return c;
        }

        [Test]
        public void TheSharkWaitsBesideTheFirstRoadOnly()
        {
            Campaign c = OnTheFirstRoad(5100, new CampaignTuning());
            Encounter shark = c.BeastOffer;
            Assert.IsNotNull(shark);
            Assert.AreEqual(EncounterKind.Beast, shark.Kind);
            Assert.AreEqual("The sand shark", shark.Title);
            Assert.IsFalse(c.Offers.Any(o => o.Kind == EncounterKind.Beast), "it is not one of the road's offers");
            Assert.IsTrue(c.CanStartBeast(out string why), why);

            // Not on a deeper tribe's road (Skullpeak's has the cage bear instead, CageBearTests), and not on a road with no
            // rows before its chief.
            Campaign deeper = Campaign.New(5100, new CampaignTuning());
            Assert.IsTrue(CampaignPilot.PlayTutorial(deeper, PilotStyle.Reference, 5100, 10));
            deeper.ChooseTribe(CampaignTribes.ThundercoastId);
            Assert.IsNull(deeper.BeastOffer);
            Assert.IsNull(OnTheFirstRoad(5100, new CampaignTuning { PathFights = 0 }).BeastOffer);
        }

        [Test]
        public void FixturePartialBreakWinSendsTrophiesHomeAndScarsTheBreakers()
        {
            // A short clock, so it dives away before every part breaks. 30 s from rules 25 (1 Nov 2026), whose Bite on a whole row
            // sends more back into the jaw, so a 40 s clock saw it die in all forty.
            var t = new CampaignTuning { FightTicks = 30 * SimClock.TicksPerSecond };
            for (ulong seed = 5200; seed < 5240; seed++)
            {
                Campaign c = OnTheFirstRoad(seed, t);
                int step = c.Step;
                int offers = c.Offers.Count;
                CampaignFight fight = c.StartBeast();
                Assert.AreEqual(Campaign.RulesVersion, fight.RulesVersion);
                Assert.IsTrue(fight.Setup.Fight.Beasts);
                EncounterResult r = CampaignPilot.PlayFight(c, fight, new CampaignPilot(PilotStyle.Reference, seed));
                if (!r.Won || r.PartsBroken.Count == 0 || r.BeastKilled) continue;

                Assert.IsTrue(r.BeastFled, "it dived away");
                CollectionAssert.AreEqual(r.PartsBroken, r.Trophies);
                CollectionAssert.AreEqual(r.PartsBroken, c.Trophies, "the trophies are at camp");
                Assert.AreEqual(t.MaterialsPerFight, r.MaterialsEarned);
                Assert.IsTrue(c.BeastBeatenThisRun);
                Assert.IsNull(c.BeastOffer, "gone for the run");
                Assert.AreEqual(step, c.Step, "it costs no step of the road");
                Assert.AreEqual(offers, c.Offers.Count);
                Assert.IsNull(r.Captured);
                foreach (BeastScar scar in r.ScarsEarned)
                {
                    CampaignFighter f = c.Reserve.First(x => x.Id == scar.FighterId);
                    CollectionAssert.Contains(f.Scars, scar.Part);
                    Assert.AreEqual(BeastKits.ScarName(scar.Part), scar.Name);
                }
                Assert.IsTrue(c.Archive.TryReplay(fight.Setup.FightId, out _, out string failure), failure);

                // The save keeps the trophies, the scars and the beaten beast, and the fight still plays back.
                Campaign read = CampaignSave.Read(CampaignSave.Write(c), t);
                Assert.IsEmpty(read.LoadNote);
                CollectionAssert.AreEqual(c.Trophies, read.Trophies);
                Assert.IsTrue(read.BeastBeatenThisRun);
                foreach (CampaignFighter f in c.Reserve)
                    CollectionAssert.AreEqual(f.Scars, read.Reserve.First(x => x.Id == f.Id).Scars);
                Assert.IsTrue(read.Archive.TryReplay(fight.Setup.FightId, out _, out failure), failure);
                return;
            }
            Assert.Fail("no beast fight ended in a win with some parts broken in forty campaigns");
        }

        [Test]
        public void FixtureLossCarriesEverythingAwayAndCapturesNobody()
        {
            // A shark this tough and nobody giving orders: it wins.
            // The 13 Oct roster as lives, which rules 11 replaced and old saves still play.
            var t = new CampaignTuning { EnemyScaleTier1 = 400, RulesCap = Campaign.RaidChiefRules };
            for (ulong seed = 5300; seed < 5340; seed++)
            {
                Campaign c = OnTheFirstRoad(seed, t);
                int before = c.Reserve.Count;
                EncounterResult r = CampaignPilot.PlayFight(c, c.StartBeast(), new CampaignPilot(PilotStyle.Passive, seed));
                if (r.Won) continue;
                Assert.IsEmpty(r.Trophies);
                Assert.IsEmpty(c.Trophies, "it carries everything away");
                Assert.IsNull(r.Captured, "a beast takes no prisoners");
                Assert.IsNull(r.Slain);
                Assert.AreEqual(before - r.Fallen.Count, c.Reserve.Count, "only the fallen are gone");
                Assert.IsFalse(c.BeastBeatenThisRun);
                if (!r.RunOver) Assert.IsNotNull(c.BeastOffer, "it waits for you to try again");
                return;
            }
            Assert.Fail("giving no orders never lost to the shark in forty campaigns");
        }

        [Test]
        public void TheButcherForgesOneTrophyIntoOnePieceForGood()
        {
            Campaign camp = Campaign.New(5400);
            Assert.IsTrue(CampaignPilot.PlayTutorial(camp, PilotStyle.Reference, 5400, 10));
            // Two trophies at camp, a jaw and a tail, written into the save as a won hunt would leave them.
            string text = CampaignSave.Write(camp);
            text = text.Replace("\nforge\t", "\ntrophy\t0\ntrophy\t3\nforge\t");
            Campaign c = CampaignSave.Read(text);
            CollectionAssert.AreEqual(new[] { BeastPart.Jaw, BeastPart.Tail }, c.Trophies);

            Assert.IsFalse(c.CanForgeTrophy(BeastPart.Jaw, BeastPiece.FinBlades, out _), "a jaw makes the shield, the net or the staff");
            Assert.IsTrue(c.TryForgeTrophy(BeastPart.Jaw, BeastPiece.JawShield, out string why), why);
            Assert.AreEqual(0, c.TrophyCount(BeastPart.Jaw), "one trophy, one piece");
            Assert.IsFalse(c.CanForgeTrophy(BeastPart.Tail, BeastPiece.JawShield, out _), "once and for good");
            Assert.IsFalse(c.CanForgeTrophy(BeastPart.Tail, BeastPiece.ClawArm, out _), "a shark's tail makes none of the bear's pieces");
            Assert.IsTrue(c.TryForgeTrophy(BeastPart.Tail, BeastPiece.SpineRifle, out why), "the tail makes any of the five: " + why);
            CollectionAssert.AreEqual(new[] { BeastPiece.JawShield, BeastPiece.SpineRifle }, c.BeastPiecesForged);
            Assert.IsEmpty(c.Trophies);

            // Every run after starts with them in the gear, each on the first fighter of its class.
            Campaign read = CampaignSave.Read(CampaignSave.Write(c));
            CollectionAssert.AreEqual(c.BeastPiecesForged, read.BeastPiecesForged);
            read.StartRun();
            CampaignGear shield = read.FindGear(BeastPieces.GearId(BeastPiece.JawShield));
            Assert.IsNotNull(shield);
            Assert.AreEqual("gear.shark.0", shield.Id, "the shark's pieces keep the ids they had before the bear");
            Assert.AreEqual((int)UnitClass.Warden, shield.ClassIndex);
            Assert.AreEqual(UnitClass.Warden, read.Reserve.First(f => f.GearId == shield.Id).Class);
            Assert.IsFalse(read.CanForgeTrophy(BeastPart.Tail, BeastPiece.ToothNet, out why));
            Assert.AreEqual("The forge is at camp.", why);
        }

        [Test]
        public void ABeastFightSavedHalfwayResumesExactly()
        {
            var t = new CampaignTuning();
            Campaign c = OnTheFirstRoad(5500, t);
            CampaignFight fight = c.StartBeast();
            var pilot = new CampaignPilot(PilotStyle.Reference, 5500);
            while (!fight.Over && fight.Sim.CurrentTick < BeastKits.SweepEveryTicks + 60)
            {
                pilot.Step(fight);
                fight.Tick();
            }
            Assert.IsFalse(fight.Over);
            Campaign half = CampaignSave.Read(CampaignSave.Write(c), t);
            Assert.IsEmpty(half.LoadNote);
            Assert.AreEqual(EncounterKind.Beast, half.Active.Encounter.Kind);
            Assert.AreEqual(fight.Session.Checksum, half.Active.Session.Checksum);
        }
    }
}
