using System;
using System.Collections.Generic;
using System.Linq;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using NUnit.Framework;

namespace KindredSiege.Sim.Tests
{
    /// The Shade's other five routes (Jacob, 1 Nov 2026: "finish shade"; their forms as locked on 31 Oct, "The Shade's core";
    /// FightRules.ShadeForms, campaign rules 28): Mirror, Rift Mine, Rupture, Breach and Echo. One quick test per piece, only
    /// to prove each runs (Jacob, 12 Oct 2026: build first). Nothing here is tuning.
    public class ShadeRoutesTests
    {
        private static readonly FightRules Chief25 =
            FightRules.CampaignChiefTactics.WithParryCenter().WithLayers().WithFloor().WithShadeForms();
        private static readonly FightRules Chief24 = FightRules.CampaignChiefTactics.WithParryCenter().WithLayers().WithFloor();
        private static readonly FightRules Road25 = FightRules.Campaign.WithRoadKit().WithLayers().WithFloor().WithShadeForms();

        private static List<UnitSpec> Side(params UnitSpec[] units) => new List<UnitSpec>(units);

        private static UnitSpec U(string id, UnitClass unitClass, int health = 3000)
        {
            return new UnitSpec(id, unitClass, SliceClasses.Stats(unitClass).DefaultRow, UnitRole.Fighter, 100, health);
        }

        private static PalKit Kit(ShadeRoute route, int form)
        {
            return new PalKit(new List<KeyValuePair<ShadeRoute, int>> { new KeyValuePair<ShadeRoute, int>(route, form) }, 0);
        }

        // The Bloodridge chief with only his Cleave beside one fighter of his (x.s, of this class), against your Pal and three.
        private static BattleSimulation Cleaver(string id, PalKit kit, FightRules rules, UnitClass foe = UnitClass.Skirmisher)
        {
            var ours = Side(new UnitSpec("pal", UnitClass.Marksman, Row.Back, UnitRole.Pal, 100, 3000),
                U("s", UnitClass.Skirmisher), U("m", UnitClass.Marksman), U("t", UnitClass.Trapper));
            return new BattleSimulation(new BattleSetup(id, 11, ours,
                Side(new UnitSpec("x.s", foe, SliceClasses.Stats(foe).DefaultRow, UnitRole.Fighter, 100, 3000),
                    new UnitSpec("chief", UnitClass.Warden, Row.Front, UnitRole.Chief, 100, 8000)),
                new TacticsProfile(), new TacticsProfile(), BattleSetup.DefaultTickLimit, BattleSetup.DefaultDirectivePoints,
                NerveRules.On(NerveRules.Full, NerveRules.Full), 10, 400, new SideRules(1, 0, true).WithPals(kit),
                new SideRules(2, 0, false, 0, ChiefSpell.Eclipse, kit: ChiefKit.Bloodridge, moves: ChiefMove.Cleave), rules));
        }

        // A road fight against a row of five of their Medics, m1 to m5, who never fight back.
        private static BattleSimulation MedicRow(string id, PalKit kit, int health = 3000)
        {
            var ours = Side(U("s", UnitClass.Skirmisher), U("m", UnitClass.Marksman), U("w", UnitClass.Warden));
            var theirs = new List<UnitSpec>();
            for (int i = 1; i <= 5; i++) theirs.Add(new UnitSpec("m" + i, UnitClass.Medic, Row.Front, UnitRole.Fighter, 100, health));
            return new BattleSimulation(new BattleSetup(id, 4, ours, theirs, new TacticsProfile(), new TacticsProfile(),
                BattleSetup.DefaultTickLimit, BattleSetup.DefaultDirectivePoints, NerveRules.On(NerveRules.Full, NerveRules.Full), 0, 400,
                new SideRules(0, 0, false).WithPals(kit), null, Road25));
        }

        private static bool Order(SimSession session, Directive directive, string target, out string why)
        {
            return session.TryIssue(Directives.CommandKind, "0", target ?? string.Empty, 0, 0, Directives.Name(directive), out why);
        }

        private static bool Order(SimSession session, Directive directive, string target = "") => Order(session, directive, target, out _);

        private static void Run(BattleSimulation sim, SimSession session, int ticks)
        {
            for (int k = 0; k < ticks && !sim.IsFinished; k++) session.Tick();
        }

        private static List<BattleEvent> Tricks(BattleSimulation sim, string detail)
        {
            return sim.Events.Where(e => e.Kind == BattleEventKind.Trick && e.Detail == detail).ToList();
        }

        // Ticks on to the tick his next Cleave's beat lands, then Holds: a clean parry. False when the fight ended first.
        private static bool ParryNextCleave(BattleSimulation sim, SimSession session)
        {
            int parries = sim.Events.Count(e => e.Kind == BattleEventKind.Parry && e.ActorSide == 0);
            while (!sim.IsFinished && sim.CurrentTick < BattleSetup.DefaultTickLimit)
            {
                if (sim.Threats(0).Any(t => t.Name == ChiefKits.CleaveName && t.BeatTicks == 1)) break;
                session.Tick();
            }
            if (sim.IsFinished || !Order(session, Directive.Hold)) return false;
            session.Tick();
            return sim.Events.Count(e => e.Kind == BattleEventKind.Parry && e.ActorSide == 0) > parries;
        }

        // ------------------------------------------------------------ Mirror (Hold)

        [Test]
        public void MirrorSendsAHitBackOnACleanParry()
        {
            BattleSimulation sim = Cleaver("mirror", Kit(ShadeRoute.Mirror, 1), Chief25);
            Assert.IsTrue(ParryNextCleave(sim, new SimSession(sim)), "the Cleave was not parried");
            BattleEvent mirror = Tricks(sim, ShadeRules.MirrorDetail).Single();
            BattleUnit chief = sim.FindById("chief");
            Assert.AreEqual("chief", mirror.TargetId);
            Assert.AreEqual(chief.MaxHp * ShadeRules.MirrorPercent / 100, mirror.Amount);
            Assert.IsTrue(sim.Events.Any(e => e.Kind == BattleEventKind.Hit && e.Detail == ShadeRules.MirrorDetail && e.TargetId == "chief"));

            BattleSimulation old = Cleaver("mirror", Kit(ShadeRoute.Mirror, 1), Chief24);
            Assert.IsTrue(ParryNextCleave(old, new SimSession(old)));
            Assert.IsEmpty(Tricks(old, ShadeRules.MirrorDetail), "below rules 28 Mirror does nothing");
        }

        [Test]
        public void CatchGrowsWithParriesInARowAndPierceGoesThroughToTheOneBehind()
        {
            // Two Cleaves parried in a row: the second return is twice the first.
            BattleSimulation sim = Cleaver("catch", Kit(ShadeRoute.Mirror, 2), Chief25);
            var session = new SimSession(sim);
            Assert.IsTrue(ParryNextCleave(sim, session), "the first Cleave was not parried");
            Run(sim, session, 30);
            Assert.IsTrue(ParryNextCleave(sim, session), "the second Cleave was not parried");
            List<BattleEvent> returns = Tricks(sim, ShadeRules.MirrorDetail);
            Assert.AreEqual(2, returns.Count);
            Assert.AreEqual(returns[0].Amount * 2, returns[1].Amount, "two parries in a row");

            // Pierce: his Marksman stands behind him and takes half.
            BattleSimulation pierce = Cleaver("pierce", Kit(ShadeRoute.Mirror, 3), Chief25, UnitClass.Marksman);
            Assert.IsTrue(ParryNextCleave(pierce, new SimSession(pierce)), "the Cleave was not parried");
            BattleEvent back = Tricks(pierce, ShadeRules.MirrorDetail).Single();
            BattleEvent through = pierce.Events.Single(e => e.Kind == BattleEventKind.Hit && e.Detail == ShadeRules.MirrorDetail && e.TargetId == "x.s");
            Assert.AreEqual(back.Amount * ShadeRules.PiercePercent / 100, through.Amount);
        }

        // ------------------------------------------------------------ Rift Mine (Fall Back)

        [Test]
        public void RiftMineBlastsTheFirstOneWhoStrikesYouAfterAFallBack()
        {
            BattleSimulation sim = Cleaver("mine", Kit(ShadeRoute.RiftMine, 1), Chief25);
            var session = new SimSession(sim);
            session.Tick();
            Assert.IsTrue(Order(session, Directive.FallBack));
            Assert.AreEqual(ShadeRules.MineTicks, sim.MineTicksLeft(0));
            Assert.AreEqual(sim.FallBackTicksLeft(0) * ShadeRules.ReturnPercent / 100, sim.ReturnTicksLeft(0), "the return outlasts the dodge");
            while (sim.MineTicksLeft(0) > 0 && !sim.IsFinished) session.Tick();
            BattleEvent blast = Tricks(sim, ShadeRules.MineDetail).Single();
            Assert.AreEqual(1, blast.Amount, "one blasted at form 1");
            BattleUnit struck = sim.FindById(blast.TargetId);
            Assert.AreEqual(1, struck.Side);
            Assert.IsTrue(sim.Events.Any(e => e.Kind == BattleEventKind.Hit && e.Detail == ShadeRules.MineDetail && e.TargetId == struck.Id));

            // Wide Blast takes the one beside him too.
            BattleSimulation wide = Cleaver("mine", Kit(ShadeRoute.RiftMine, 3), Chief25);
            var wideSession = new SimSession(wide);
            wideSession.Tick();
            Assert.IsTrue(Order(wideSession, Directive.FallBack));
            while (wide.MineTicksLeft(0) > 0 && !wide.IsFinished) wideSession.Tick();
            Assert.AreEqual(2, Tricks(wide, ShadeRules.MineDetail).Single().Amount, "he and the one beside him");
        }

        [Test]
        public void RiftMinesReturnKeepsYourAttacksWaitingAfterTheDodge()
        {
            BattleSimulation sim = MedicRow("return", Kit(ShadeRoute.RiftMine, 1));
            var session = new SimSession(sim);
            Run(sim, session, 30);
            Assert.IsTrue(Order(session, Directive.FallBack));
            int from = sim.Events.Count;
            while (sim.ReturnTicksLeft(0) > 1 && !sim.IsFinished)
            {
                session.Tick();
                if (sim.FallBackTicksLeft(0) == 0) Assert.Greater(sim.ReturnTicksLeft(0), 0);
            }
            Assert.AreEqual(0, sim.Events.Skip(from).Count(e => e.Kind == BattleEventKind.Hit && e.ActorSide == 0 && e.ActorId.Length > 0),
                "nobody of yours attacked before the return ran out");
        }

        // ------------------------------------------------------------ Rupture (Focus Fire)

        [Test]
        public void RuptureOpensAWeakPointAndTheSecondPressRupturesIt()
        {
            foreach (int form in new[] { 1, 2, 3 })
            {
                BattleSimulation sim = Cleaver("rupture" + form, Kit(ShadeRoute.Rupture, form), Chief25);
                var session = new SimSession(sim);
                session.Tick();
                Assert.IsTrue(Order(session, Directive.FocusFire, "chief"));
                int points = sim.DirectivePoints(0);
                Assert.IsFalse(Order(session, Directive.FocusFire, "chief", out string why), "nothing to rupture yet");
                Assert.AreEqual("Nothing to rupture yet.", why);
                Assert.AreEqual(points, sim.DirectivePoints(0), "a refused follow up spends nothing");
                while (sim.RuptureStepsOf(0) < ShadeRules.RuptureSteps && sim.FocusTicksLeft(0) > 0 && !sim.IsFinished) session.Tick();
                Assert.AreEqual(ShadeRules.RuptureSteps, sim.RuptureStepsOf(0), "the weak point never opened fully");
                Assert.AreEqual(ShadeRules.RuptureSteps, Tricks(sim, ShadeRules.WeakPointDetail).Count);
                points = sim.DirectivePoints(0);
                BattleUnit target = sim.FindById("chief");
                int health = target.MaxHp;
                Assert.IsTrue(Order(session, Directive.FocusFire, "chief", out why), why);
                Assert.AreEqual(points, sim.DirectivePoints(0), "the follow up is free");
                Assert.AreEqual(0, sim.FocusTicksLeft(0), "cashing it ends the Focus");
                BattleEvent rupture = Tricks(sim, ShadeRules.RuptureDetail).Single();
                Assert.AreEqual(ShadeRules.RuptureSteps, rupture.Amount);
                Assert.IsTrue(sim.Events.Any(e => e.Kind == BattleEventKind.Hit && e.Detail == ShadeRules.RuptureDetail && e.TargetId == "chief"));
                Assert.AreEqual(form >= 2, target.Statuses[StatusKind.Crack].On, "Crack at form 2");
                Assert.AreEqual(form >= 3, Tricks(sim, ShadeRules.FinishDetail).Count == 1, "Finish at form 3");
            }
        }

        [Test]
        public void ChangingTargetLosesTheWeakPoint()
        {
            BattleSimulation sim = Cleaver("switch", Kit(ShadeRoute.Rupture, 1), Chief25);
            var session = new SimSession(sim);
            session.Tick();
            Assert.IsTrue(Order(session, Directive.FocusFire, "chief"));
            while (sim.RuptureStepsOf(0) == 0 && sim.FocusTicksLeft(0) > 0 && !sim.IsFinished) session.Tick();
            Assert.Greater(sim.RuptureStepsOf(0), 0);
            Assert.IsTrue(Order(session, Directive.FocusFire, "x.s"));
            Assert.AreEqual(0, sim.RuptureStepsOf(0));
            Assert.AreEqual("x.s", sim.RuptureTargetOf(0));
        }

        // ------------------------------------------------------------ Breach (Unleash)

        [Test]
        public void BreachHitsTheirFrontLineAndExposesYours()
        {
            foreach (int form in new[] { 1, 2 })
            {
                BattleSimulation sim = MedicRow("breach" + form, Kit(ShadeRoute.Breach, form));
                var session = new SimSession(sim);
                Run(sim, session, 30);
                var waits = Enumerable.Range(1, 3).Select(i => sim.FindById("m" + i).Cooldown).ToList();
                Assert.IsTrue(Order(session, Directive.Unleash));
                Assert.AreEqual(ShadeRules.BreachTargets, Tricks(sim, ShadeRules.BreachDetail).Single().Amount);
                for (int i = 1; i <= 3; i++)
                {
                    BattleUnit m = sim.FindById("m" + i);
                    Assert.IsTrue(sim.Events.Any(e => e.Kind == BattleEventKind.Hit && e.Detail == ShadeRules.BreachDetail && e.TargetId == m.Id));
                    Assert.AreEqual(waits[i - 1] + ShadeRules.ShockTicks, m.Cooldown, "his next turn waits");
                    Assert.AreEqual(form >= 2, m.Statuses[StatusKind.NoCover].On, "Gap at form 2");
                }
                Assert.IsFalse(sim.Events.Any(e => e.Kind == BattleEventKind.Hit && e.Detail == ShadeRules.BreachDetail && e.TargetId == "m4"));
                Assert.AreEqual(sim.UnleashTicksLeft(0) + ShadeRules.ExposedAfterTicks, sim.ExposedTicksLeft(0));
                Assert.IsFalse(Order(session, Directive.Unleash, "", out string why), "one Impact a Breach");
                StringAssert.StartsWith("Breach is still open", why);
            }
        }

        [Test]
        public void RamEndsTheUnleashAndRamsHarderTheMoreIsLeft()
        {
            BattleSimulation sim = MedicRow("ram", Kit(ShadeRoute.Breach, 3));
            var session = new SimSession(sim);
            Run(sim, session, 30);
            Assert.IsTrue(Order(session, Directive.Unleash));
            Assert.AreEqual("Ram", sim.FollowUpOf(0, Directive.Unleash));
            Run(sim, session, 30);
            int left = sim.UnleashTicksLeft(0);
            int points = sim.DirectivePoints(0);
            Assert.IsTrue(Order(session, Directive.Unleash, "", out string why), why);
            Assert.AreEqual(points, sim.DirectivePoints(0), "the Ram is free");
            Assert.AreEqual(0, sim.UnleashTicksLeft(0), "it ends the Unleash");
            Assert.AreEqual(left, Tricks(sim, ShadeRules.RamDetail).Single().Amount);
            Assert.AreEqual(ShadeRules.BreachTargets, sim.Events.Count(e => e.Kind == BattleEventKind.Hit && e.Detail == ShadeRules.RamDetail));
            Assert.Greater(sim.ExposedTicksLeft(0), 0, "the Ram does not end the exposure");
        }

        // ------------------------------------------------------------ Echo (Unleash)

        [Test]
        public void EchoRecordsTheUnleashAndReplaysPartOfItWhenItEnds()
        {
            int FirstBlow(PalKit kit, out BattleSimulation sim)
            {
                sim = MedicRow("echo", kit);
                var session = new SimSession(sim);
                Run(sim, session, 30);
                Assert.IsTrue(Order(session, Directive.Unleash));
                int from = sim.Events.Count;
                while (sim.UnleashTicksLeft(0) > 0 && !sim.IsFinished) session.Tick();
                Run(sim, session, 2);
                return sim.Events.Skip(from).First(e => e.Kind == BattleEventKind.Hit && e.ActorSide == 0 && e.Detail.Length == 0).Amount;
            }
            int echoed = FirstBlow(Kit(ShadeRoute.Echo, 1), out BattleSimulation withEcho);
            int plain = FirstBlow(PalKit.None, out _);
            Assert.Less(echoed, plain, "Echo halves Unleash's own bonus");
            List<BattleEvent> echoes = Tricks(withEcho, ShadeRules.EchoDetail);
            Assert.IsNotEmpty(echoes, "no echo replayed");
            foreach (BattleEvent e in echoes)
                Assert.IsTrue(withEcho.Events.Any(h => h.Kind == BattleEventKind.Hit && h.Detail == ShadeRules.EchoDetail &&
                    h.ActorId == e.ActorId && h.TargetId == e.TargetId), "each echo lands as a hit");
            Assert.IsFalse(withEcho.EchoRecordingOf(0));
        }

        [Test]
        public void ReleaseLetsTheEchoesGoAtOnceAndConvergeSendsThemAtTheFocus()
        {
            foreach (int form in new[] { 2, 3 })
            {
                BattleSimulation sim = MedicRow("release" + form, Kit(ShadeRoute.Echo, form));
                var session = new SimSession(sim);
                Run(sim, session, 30);
                Assert.IsTrue(Order(session, Directive.FocusFire, "m5"));
                Assert.IsTrue(Order(session, Directive.Unleash));
                Assert.IsFalse(Order(session, Directive.Unleash, "", out string why), "nothing recorded yet");
                Assert.AreEqual("No echoes to let go yet.", why);
                Run(sim, session, 60);
                Assert.AreEqual("Release", sim.FollowUpOf(0, Directive.Unleash));
                int points = sim.DirectivePoints(0);
                Assert.IsTrue(Order(session, Directive.Unleash, "", out why), why);
                Assert.AreEqual(points, sim.DirectivePoints(0), "the Release is free");
                Assert.AreEqual(0, sim.UnleashTicksLeft(0));
                List<BattleEvent> echoes = Tricks(sim, ShadeRules.EchoDetail);
                Assert.IsNotEmpty(echoes);
                if (form >= 3) Assert.IsTrue(echoes.All(e => e.TargetId == "m5"), "Converge sends them all at the Focus");
                else Assert.IsTrue(echoes.All(e => e.TargetId != "m5" || sim.FindById("m5").Row == Row.Front));
            }
        }
        // ------------------------------------------------------------ whole runs

        [Test]
        public void TheBotPressesRuptureAndItsThemedPickClimbsTheRouteItHolds()
        {
            var easy = new CampaignTuning { EnemyScaleTier1 = 40 };
            int presses = 0, offered = 0;
            for (ulong seed = 6200; seed < 6230 && (presses == 0 || offered == 0); seed++)
            {
                Campaign c = Campaign.New(seed, easy);
                if (!CampaignPilot.PlayTutorial(c, PilotStyle.Reference, seed, 10)) continue;
                c.StartRun();
                Assert.IsTrue(c.TryTakeRoute(ShadeRoute.Rupture));
                var picker = new CampaignPilot(PilotStyle.Reference, seed);
                for (int n = 0; c.Stage == CampaignStage.InRun && n < 12; n++)
                {
                    CampaignFight fight = c.StartEncounter(picker.PickOffer(c, Pal.Shade));
                    CampaignPilot.PlayFight(c, fight, new CampaignPilot(PilotStyle.Reference, seed * 31UL + (ulong)n));
                    presses += fight.Sim.Events.Count(e => e.Kind == BattleEventKind.Trick && e.ActorSide == 0 && e.Detail == ShadeRules.RuptureDetail);
                    if (c.FavourOffer == null || c.FavourOffer.Pal != Pal.Shade) continue;
                    // The held route at the highest form on the card is the one the themed bot climbs.
                    int Form(Favour f) => c.ShadeRoutes.Where(h => h.Route == FavourRules.RouteOf(f)).Select(h => h.Form).DefaultIfEmpty(0).First();
                    int top = c.FavourOffer.Choices.Max(Form);
                    int pick = picker.PickFavour(c, true);
                    if (top > 0)
                    {
                        offered++;
                        Assert.AreEqual(top, Form(c.FavourOffer.Choices[pick]), "the themed bot climbs the route it holds");
                    }
                    Assert.IsTrue(c.TryTakeFavour(pick, out string why), why);
                }
            }
            Assert.Greater(presses, 0, "the bot never popped a full weak point");
            Assert.Greater(offered, 0, "no card offered a held route's next form");
        }

        [Test]
        public void WholeRunsPlayWithEveryRouteAtItsLastForm()
        {
            // Two routes a run, on two buttons, each at form 3: all eight across four runs. The pilot plays the run through.
            var pairs = new[]
            {
                new[] { ShadeRoute.Mirror, ShadeRoute.Rupture }, new[] { ShadeRoute.Sanctuary, ShadeRoute.Breach },
                new[] { ShadeRoute.RiftMine, ShadeRoute.Echo }, new[] { ShadeRoute.Backstab, ShadeRoute.Fear }
            };
            var easy = new CampaignTuning { EnemyScaleTier1 = 40 };
            var seen = new HashSet<string>();
            for (int p = 0; p < pairs.Length; p++)
            {
                ulong seed = 6100UL + (ulong)p;
                Campaign c = Campaign.New(seed, easy);
                Assert.IsTrue(CampaignPilot.PlayTutorial(c, PilotStyle.Reference, seed, 10), "the tutorial was never won");
                c.StartRun();
                foreach (ShadeRoute route in pairs[p])
                    for (int form = 0; form < ShadeRules.MaxForm; form++) Assert.IsTrue(c.TryTakeRoute(route));
                var picker = new CampaignPilot(PilotStyle.Reference, seed);
                for (int n = 0; c.Stage == CampaignStage.InRun && n < 30; n++)
                {
                    CampaignFight fight = c.StartEncounter(picker.PickOffer(c));
                    Assert.IsTrue(fight.Sim.ShadeFormsOn);
                    CampaignPilot.PlayFight(c, fight, new CampaignPilot(PilotStyle.Reference, seed * 31UL + (ulong)n));
                    foreach (BattleEvent e in fight.Sim.Events)
                        if (e.Kind == BattleEventKind.Trick && e.ActorSide == 0) seen.Add(e.Detail);
                    Assert.IsEmpty(CampaignSave.Read(CampaignSave.Write(c), easy).LoadNote, "the save reads back");
                }
            }
            TestContext.WriteLine("tricks seen: " + string.Join(", ", seen.OrderBy(s => s)));
            Assert.IsNotEmpty(seen, "no trick of hers went off in four runs");
        }
    }
}
