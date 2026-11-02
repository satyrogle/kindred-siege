using System;
using System.Collections.Generic;
using System.Linq;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using NUnit.Framework;

namespace KindredSiege.Sim.Tests
{
    /// The Shade's forms (Jacob in chat, 31 Oct 2026: "go" on Claude's Shade design; FightRules.ShadeForms, campaign rules 28).
    /// One quick test per piece, only to prove each runs (Jacob, 12 Oct 2026: build first). Nothing here is tuning.
    public class ShadeFormsTests
    {
        private static readonly FightRules Chief25 =
            FightRules.CampaignChiefTactics.WithParryCenter().WithLayers().WithFloor().WithShadeForms();
        private static readonly FightRules Chief24 = FightRules.CampaignChiefTactics.WithParryCenter().WithLayers().WithFloor();
        private static readonly FightRules Road25 = FightRules.Campaign.WithRoadKit().WithLayers().WithFloor().WithShadeForms();
        private static readonly FightRules Road24 = FightRules.Campaign.WithRoadKit().WithLayers().WithFloor();

        private static List<UnitSpec> Side(params UnitSpec[] units) => new List<UnitSpec>(units);

        private static UnitSpec U(string id, UnitClass unitClass, int health = 3000, int power = 100)
        {
            return new UnitSpec(id, unitClass, SliceClasses.Stats(unitClass).DefaultRow, UnitRole.Fighter, power, health);
        }

        private static PalKit Kit(ShadeRoute route, int form)
        {
            return new PalKit(new List<KeyValuePair<ShadeRoute, int>> { new KeyValuePair<ShadeRoute, int>(route, form) }, 0);
        }

        // The Bloodridge chief with only his Cleave beside one fighter of his, against your Pal and three; your side brings this
        // kit. The other numbers make one of theirs hit hard (a Marksman's shots), for the shields to break under the field.
        private static BattleSimulation Cleaver(string id, PalKit kit, FightRules rules, int palHealth = 3000, int foePower = 100,
            int healthPercent = 400, UnitClass foe = UnitClass.Skirmisher, int health = 3000)
        {
            var ours = Side(new UnitSpec("pal", UnitClass.Marksman, Row.Back, UnitRole.Pal, 100, palHealth),
                U("s", UnitClass.Skirmisher, health), U("m", UnitClass.Marksman, health), U("t", UnitClass.Trapper, health));
            return new BattleSimulation(new BattleSetup(id, 11, ours,
                Side(new UnitSpec("x.s", foe, SliceClasses.Stats(foe).DefaultRow, UnitRole.Fighter, foePower, 3000),
                    new UnitSpec("chief", UnitClass.Warden, Row.Front, UnitRole.Chief, 100, 8000)),
                new TacticsProfile(), new TacticsProfile(), BattleSetup.DefaultTickLimit, BattleSetup.DefaultDirectivePoints,
                NerveRules.On(NerveRules.Full, NerveRules.Full), 10, healthPercent, new SideRules(1, 0, true).WithPals(kit),
                new SideRules(2, 0, false, 0, ChiefSpell.Eclipse, kit: ChiefKit.Bloodridge, moves: ChiefMove.Cleave), rules));
        }

        // A road fight against a row of five of their Medics, m1 to m5, who never fight back.
        private static BattleSimulation MedicRow(string id, PalKit kit, FightRules rules, NerveRules nerve, int health = 100)
        {
            var ours = Side(U("s", UnitClass.Skirmisher), U("m", UnitClass.Marksman), U("w", UnitClass.Warden));
            var theirs = new List<UnitSpec>();
            for (int i = 1; i <= 5; i++) theirs.Add(new UnitSpec("m" + i, UnitClass.Medic, Row.Front, UnitRole.Fighter, 100, health));
            return new BattleSimulation(new BattleSetup(id, 4, ours, theirs, new TacticsProfile(), new TacticsProfile(),
                BattleSetup.DefaultTickLimit, BattleSetup.DefaultDirectivePoints, nerve, 0, 400,
                new SideRules(0, 0, false).WithPals(kit), null, rules));
        }

        private static bool Order(SimSession session, Directive directive, string target = "")
        {
            return session.TryIssue(Directives.CommandKind, "0", target, 0, 0, Directives.Name(directive), out _);
        }

        // Ticks on to the tick his Cleave's beat lands, then Holds: a clean parry.
        private static void ParryTheCleave(BattleSimulation sim, SimSession session)
        {
            while (!sim.IsFinished && sim.CurrentTick < BattleSetup.DefaultTickLimit)
            {
                if (sim.Threats(0).Any(t => t.Name == ChiefKits.CleaveName && t.BeatTicks == 1)) break;
                session.Tick();
            }
            Assert.IsTrue(session.TryIssue(Directives.CommandKind, "0", "", 0, 0, Directives.Name(Directive.Hold), out string why),
                "the Hold was refused: " + why);
            session.Tick();
            Assert.IsTrue(sim.Events.Any(e => e.Kind == BattleEventKind.Parry && e.ActorSide == 0), "the Cleave was not parried");
        }

        // Focus Fire on the middle Medic until Fear goes off with this detail; its event's index, -1 if it never does.
        private static int FocusUntil(BattleSimulation sim, SimSession session, string detail)
        {
            for (int n = 0; n < 60 && !sim.IsFinished; n++)
            {
                if (sim.FindById("m3").Alive) Order(session, Directive.FocusFire, "m3");
                for (int k = 0; k < 60 && !sim.IsFinished; k++) session.Tick();
                int at = sim.Events.ToList().FindIndex(e => e.Kind == BattleEventKind.Trick && e.Detail == detail);
                if (at >= 0) return at;
            }
            return -1;
        }

        // ------------------------------------------------------------ Sanctuary

        [Test]
        public void SanctuaryNoLongerStaggersFromRules25()
        {
            foreach (FightRules rules in new[] { Chief24, Chief25 })
            {
                BattleSimulation sim = Cleaver("price", Kit(ShadeRoute.Sanctuary, 1), rules);
                ParryTheCleave(sim, new SimSession(sim));
                BattleEvent parry = sim.Events.First(e => e.Kind == BattleEventKind.Parry && e.ActorSide == 0);
                Assert.AreEqual(rules.ShadeForms ? 0 : FightRules.StaggerTicks, parry.Amount, "the stagger the parry gave");
            }
        }

        [Test]
        public void RelaySoaksWhatABrokenShieldCouldNot()
        {
            // A Marksman of theirs whose shots break a shield under the field.
            BattleSimulation sim = Cleaver("relay", Kit(ShadeRoute.Sanctuary, 2), Chief25, 9000, 4000, 100, UnitClass.Marksman, 1000);
            var session = new SimSession(sim);
            ParryTheCleave(sim, session);
            int shields = sim.Units.Where(u => u.Side == 0 && u.Alive).Sum(u => Math.Max(1, u.MaxHp * ShadeRules.ShieldPercent / 100));
            Assert.AreEqual(shields * ShadeRules.RelayPercent / 100, sim.RelayLeft(0), "the pool is a share of the shields given");
            int before = sim.RelayLeft(0);
            int from = sim.Events.Count;
            for (int k = 0; k < ShadeRules.ShieldTicks && sim.FieldTicksLeft(0) > 0; k++) session.Tick();
            BattleEvent relay = sim.Events.Skip(from).FirstOrDefault(e => e.Kind == BattleEventKind.Shield && e.Detail == ShadeRules.RelayDetail);
            Assert.AreEqual(ShadeRules.RelayDetail, relay.Detail, "the pool never soaked a hit");
            Assert.Greater(relay.Amount, 0);
            Assert.LessOrEqual(sim.RelayLeft(0), before - relay.Amount);

            // At form 1 there is no pool.
            BattleSimulation plain = Cleaver("relay", Kit(ShadeRoute.Sanctuary, 1), Chief25, 9000, 4000, 100, UnitClass.Marksman, 1000);
            ParryTheCleave(plain, new SimSession(plain));
            Assert.AreEqual(0, plain.RelayLeft(0));
        }

        [Test]
        public void LastRefugeLeavesOneHealthOnceAFight()
        {
            // A Marksman of theirs whose shot under the field would kill one of yours.
            BattleSimulation sim = Cleaver("refuge", Kit(ShadeRoute.Sanctuary, 3), Chief25, 6000, 5000, 100, UnitClass.Marksman, 1500);
            var session = new SimSession(sim);
            ParryTheCleave(sim, session);
            BattleEvent refuge = default;
            for (int k = 0; k < ShadeRules.ShieldTicks && !sim.IsFinished && refuge.Detail != ShadeRules.RefugeDetail; k++)
            {
                session.Tick();
                refuge = sim.Events.FirstOrDefault(e => e.Kind == BattleEventKind.Trick && e.Detail == ShadeRules.RefugeDetail);
            }
            Assert.AreEqual(ShadeRules.RefugeDetail, refuge.Detail, "no blow under the field would have killed");
            BattleUnit saved = sim.FindById(refuge.TargetId);
            Assert.IsTrue(saved.Alive, "the one it saved stands");
            Assert.AreEqual(ShadeRules.RefugeHealth, saved.Hp);
            Assert.IsTrue(sim.RefugeSpent(0));
            Assert.AreEqual(0, sim.FieldTicksLeft(0), "the field shattered");
            Assert.IsFalse(sim.Units.Any(u => u.Side == 0 && u.Statuses[StatusKind.Shield].On), "every shield with it");
            // Once a fight only: a later blow can still kill him (Codex's Shade paper, Last refuge).

            // The same fight at Relay: that blow kills.
            BattleSimulation two = Cleaver("refuge", Kit(ShadeRoute.Sanctuary, 2), Chief25, 6000, 5000, 100, UnitClass.Marksman, 1500);
            var twoSession = new SimSession(two);
            ParryTheCleave(two, twoSession);
            while (two.CurrentTick <= refuge.Tick && !two.IsFinished) twoSession.Tick();
            Assert.IsFalse(two.FindById(refuge.TargetId).Alive, "without Last Refuge the blow kills");
        }

        // ------------------------------------------------------------ Backstab

        [Test]
        public void TwinKnivesLandTwoHitsDouble()
        {
            BattleSimulation sim = Cleaver("twin", Kit(ShadeRoute.Backstab, 2), Chief25);
            var session = new SimSession(sim);
            session.Tick();
            Assert.IsTrue(Order(session, Directive.FallBack), "the Fall Back was refused");
            Assert.AreEqual(ShadeRules.TwinKnivesHits, sim.BackstabHitsLeft(sim.FindById("m")));
            for (int k = 0; k < ShadeRules.BackstabTicks && !sim.IsFinished; k++) session.Tick();
            var cashes = sim.Events.Where(e => e.Kind == BattleEventKind.Cash && e.Detail == ShadeRules.BackstabDetail && e.ActorSide == 0)
                .GroupBy(e => e.ActorId).ToList();
            Assert.IsTrue(cashes.Any(g => g.Count() == ShadeRules.TwinKnivesHits), "nobody landed two hits from behind");
            Assert.IsFalse(cashes.Any(g => g.Count() > ShadeRules.TwinKnivesHits), "nobody lands more");
        }

        [Test]
        public void ShadowStrikeKeepsYourSideAttackingWhileItFallsBack()
        {
            int Blows(int form, FightRules rules)
            {
                BattleSimulation sim = MedicRow("shadow", Kit(ShadeRoute.Backstab, form), rules, NerveRules.On(NerveRules.Full, NerveRules.Full), 3000);
                var session = new SimSession(sim);
                for (int k = 0; k < 30; k++) session.Tick();
                Assert.IsTrue(Order(session, Directive.FallBack), "the Fall Back was refused");
                int from = sim.Events.Count;
                while (sim.FallBackTicksLeft(0) > 1 && !sim.IsFinished) session.Tick();
                return sim.Events.Skip(from).Count(e => e.Kind == BattleEventKind.Hit && e.ActorSide == 0 && e.ActorId.Length > 0);
            }
            Assert.AreEqual(0, Blows(2, Road25), "at Twin Knives a Fall Back stops your attacks");
            Assert.Greater(Blows(3, Road25), 0, "at Shadow Strike your side keeps hitting");
            Assert.AreEqual(0, Blows(3, Road24), "below rules 28 only the first form plays");
        }

        [Test]
        public void BackstabMakesTheNextFallBackWait()
        {
            BattleSimulation sim = Cleaver("lock", Kit(ShadeRoute.Backstab, 1), Chief25);
            var session = new SimSession(sim);
            session.Tick();
            Assert.IsTrue(Order(session, Directive.FallBack));
            Assert.AreEqual(sim.FallBackTicksLeft(0) + ShadeRules.BackstabLockTicks, sim.FallBackLockLeft(0));
            Assert.IsFalse(sim.CanUse(0, Directive.FallBack, out string why));
            StringAssert.StartsWith("Fall Back is back in", why);
            Assert.IsFalse(Order(session, Directive.FallBack), "refused while it waits");
            while (sim.FallBackLockLeft(0) > 0 && !sim.IsFinished) session.Tick();
            Assert.IsTrue(sim.CanUse(0, Directive.FallBack, out why), why);

            // Below rules 28 there is no wait.
            BattleSimulation old = Cleaver("lock", Kit(ShadeRoute.Backstab, 1), Chief24);
            var oldSession = new SimSession(old);
            oldSession.Tick();
            Assert.IsTrue(Order(oldSession, Directive.FallBack));
            Assert.AreEqual(0, old.FallBackLockLeft(0));
            Assert.IsTrue(old.CanUse(0, Directive.FallBack, out why), why);
        }

        // ------------------------------------------------------------ Fear

        [Test]
        public void FearMakesFocusFireAQuarterShorter()
        {
            int Focus(FightRules rules, ShadeRoute route)
            {
                BattleSimulation sim = Cleaver("focus", Kit(route, 1), rules);
                var session = new SimSession(sim);
                session.Tick();
                Assert.IsTrue(Order(session, Directive.FocusFire, "x.s"));
                return sim.FocusTicksLeft(0);
            }
            Assert.AreEqual(BattleSimulation.FocusTicks * ShadeRules.FearFocusPercent / 100, Focus(Chief25, ShadeRoute.Fear));
            Assert.AreEqual(BattleSimulation.FocusTicks, Focus(Chief24, ShadeRoute.Fear), "below rules 28 no price");
            Assert.AreEqual(BattleSimulation.FocusTicks, Focus(Chief25, ShadeRoute.Backstab), "only with Fear");
        }

        [Test]
        public void SpreadingDreadSetsFearOffAroundWhoItBreaks()
        {
            // Your side frightens twice as hard, so Fear's drain breaks a Medic outright and he flees.
            NerveRules nerve = NerveRules.On(NerveRules.Full, NerveRules.Full, 100, 0);
            BattleSimulation sim = MedicRow("dread", Kit(ShadeRoute.Fear, 2), Road25, nerve);
            int at = FocusUntil(sim, new SimSession(sim), ShadeRules.DreadDetail);
            Assert.GreaterOrEqual(at, 0, "Fear never spread");
            int dread = sim.Events.Count(e => e.Kind == BattleEventKind.Trick && e.Detail == ShadeRules.DreadDetail);
            Assert.LessOrEqual(dread, ShadeRules.DreadSpreads, "at most two more for one fall");
            BattleEvent spread = sim.Events[at];
            Assert.IsTrue(sim.FindById(spread.TargetId).NerveSnapped, "it spread from one Fear broke");

            // At form 1 it never spreads.
            BattleSimulation one = MedicRow("dread", Kit(ShadeRoute.Fear, 1), Road25, nerve);
            Assert.GreaterOrEqual(FocusUntil(one, new SimSession(one), ShadeRules.FearDetail), 0, "Fear never went off");
            for (int k = 0; k < 300 && !one.IsFinished; k++) one.Tick();
            Assert.IsFalse(one.Events.Any(e => e.Kind == BattleEventKind.Trick && e.Detail == ShadeRules.DreadDetail));
        }

        [Test]
        public void TurncoatHitsTheOneBesideHim()
        {
            BattleSimulation sim = MedicRow("turncoat", Kit(ShadeRoute.Fear, 3), Road25, NerveRules.On(NerveRules.Full, NerveRules.Full));
            int at = FocusUntil(sim, new SimSession(sim), ShadeRules.TurncoatDetail);
            Assert.GreaterOrEqual(at, 0, "nobody turned");
            BattleEvent turned = sim.Events[at];
            Assert.AreEqual(1, sim.FindById(turned.ActorId).Side, "one of theirs turned");
            Assert.AreEqual(1, sim.FindById(turned.TargetId).Side, "on one of his own");
            Assert.IsTrue(sim.Events.Skip(at).Any(e => e.Kind == BattleEventKind.Hit && e.Detail == ShadeRules.TurncoatDetail &&
                e.TargetId == turned.TargetId && e.ActorId == turned.ActorId), "his blow landed");
        }

        // ------------------------------------------------------------ the old rules and the road card

        [Test]
        public void BelowRules28AKitAtItsLastFormsPlaysItsFirst()
        {
            // The same fight with each route at form 3 and at form 1 under rules 24: the same events.
            foreach (ShadeRoute route in new[] { ShadeRoute.Sanctuary, ShadeRoute.Backstab, ShadeRoute.Fear })
            {
                string Played(int form)
                {
                    BattleSimulation sim = MedicRow("old." + route, Kit(route, form), Road24, NerveRules.On(NerveRules.Full, NerveRules.Full), 400);
                    var session = new SimSession(sim);
                    for (int n = 0; n < 20 && !sim.IsFinished; n++)
                    {
                        if (n % 3 == 0) Order(session, Directive.FallBack);
                        else if (sim.FindById("m3").Alive) Order(session, Directive.FocusFire, "m3");
                        for (int k = 0; k < 40 && !sim.IsFinished; k++) session.Tick();
                    }
                    return string.Join("\n", sim.Events.Select(e => e.Tick + " " + e.Kind + " " + e.ActorId + " " + e.TargetId + " " + e.Amount + " " + e.Detail));
                }
                Assert.AreEqual(Played(1), Played(3), route + " at form 3 under rules 24");
            }
        }

        [Test]
        public void HerRoadCardGivesARouteOrItsNextFormAndTheSaveKeepsIt()
        {
            var easy = new CampaignTuning { EnemyScaleTier1 = 40 };
            bool seen = false;
            for (ulong seed = 5800; seed < 5860 && !seen; seed++)
            {
                Campaign c = Campaign.New(seed, easy);
                Assert.IsTrue(CampaignPilot.PlayTutorial(c, PilotStyle.Reference, seed, 10), "the tutorial was never won");
                Assert.IsTrue(c.ShadeFormsRun);
                c.StartRun();
                // Two buttons taken, Sanctuary at its last form: only Backstab's next form is left to offer.
                Assert.IsTrue(c.TryTakeRoute(ShadeRoute.Sanctuary));
                Assert.IsTrue(c.TryTakeRoute(ShadeRoute.Sanctuary));
                Assert.IsTrue(c.TryTakeRoute(ShadeRoute.Sanctuary));
                Assert.IsTrue(c.TryTakeRoute(ShadeRoute.Backstab));
                int index = c.Offers.ToList().FindIndex(o => o.Kind != EncounterKind.Ambush && o.Node != null && o.Node.Pal == Pal.Shade);
                if (index < 0) continue;
                EncounterResult r = CampaignPilot.PlayFight(c, c.StartEncounter(index), new CampaignPilot(PilotStyle.Reference, seed));
                if (!r.Won || c.FavourOffer == null) continue;
                seen = true;
                FavourOffer offer = c.FavourOffer;
                Assert.AreEqual(Pal.Shade, offer.Pal);
                CollectionAssert.AreEqual(new[] { Favour.ShadeBackstab }, offer.Choices, "only the next form of a held route below its last");
                Assert.AreEqual(1, c.FavourRank(Favour.ShadeBackstab));
                StringAssert.StartsWith("Twin Knives", FavourRules.Describe(Favour.ShadeBackstab, 2));

                // Saved with her card standing, it comes back the same.
                string saved = CampaignSave.Write(c);
                Campaign back = CampaignSave.Read(saved, easy);
                Assert.IsEmpty(back.LoadNote);
                Assert.AreEqual(saved, CampaignSave.Write(back));
                Assert.IsTrue(back.TryTakeFavour(0, out string why), why);
                Assert.AreEqual(2, back.ShadeRoutes.Single(h => h.Route == ShadeRoute.Backstab).Form);
                Assert.AreNotEqual(0, back.HeldPals() & (1 << (int)Pal.Shade), "her route counts as a pick of hers on the road");
                Campaign kept = CampaignSave.Read(CampaignSave.Write(back), easy);
                Assert.IsEmpty(kept.LoadNote);
                Assert.AreEqual(CampaignSave.Write(back), CampaignSave.Write(kept), "and a save keeps it");
            }
            Assert.IsTrue(seen, "no Shade fight was won");
        }

        [Test]
        public void HerFirstCardOffersThreeNewRoutes()
        {
            var easy = new CampaignTuning { EnemyScaleTier1 = 40 };
            bool seen = false;
            for (ulong seed = 5900; seed < 5960 && !seen; seed++)
            {
                Campaign c = Campaign.New(seed, easy);
                Assert.IsTrue(CampaignPilot.PlayTutorial(c, PilotStyle.Reference, seed, 10), "the tutorial was never won");
                c.StartRun();
                int index = c.Offers.ToList().FindIndex(o => o.Kind != EncounterKind.Ambush && o.Node != null && o.Node.Pal == Pal.Shade);
                if (index < 0) continue;
                EncounterResult r = CampaignPilot.PlayFight(c, c.StartEncounter(index), new CampaignPilot(PilotStyle.Reference, seed));
                if (!r.Won || c.FavourOffer == null) continue;
                seen = true;
                // From all eight (1 Nov 2026, "finish shade"), three different routes, none held yet, in her order.
                IReadOnlyList<Favour> choices = c.FavourOffer.Choices;
                Assert.AreEqual(FavourRules.Offered, choices.Count);
                Assert.IsTrue(choices.All(FavourRules.IsShade));
                Assert.AreEqual(choices.Count, choices.Distinct().Count());
                Assert.IsTrue(choices.All(f => c.FavourRank(f) == 0));
                CollectionAssert.IsOrdered(choices.Select(f => (int)f).ToList());
                ShadeRoute taken = FavourRules.RouteOf(choices[2]);
                Assert.IsTrue(c.TryTakeFavour(2, out string why), why);
                Assert.AreEqual(1, c.ShadeRoutes.Single(h => h.Route == taken).Form, "a new route comes at form 1");
            }
            Assert.IsTrue(seen, "no Shade fight was won");
        }
    }
}
