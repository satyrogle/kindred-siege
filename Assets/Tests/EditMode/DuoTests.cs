using System;
using System.Collections.Generic;
using System.Linq;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using NUnit.Framework;

namespace KindredSiege.Sim.Tests
{
    /// The Pals' duos (Jacob in chat, 1 Nov 2026: "go write them"; FightRules.Duos, campaign rules 30): the card showing a duo
    /// once a run holds a pick from both of its Pals, the save, and each of the six in the fight. One quick test per piece, only
    /// to prove each runs (Jacob, 12 Oct 2026: build first). Nothing here is tuning.
    public class DuoTests
    {
        private static readonly FightRules Chief26 =
            FightRules.CampaignChiefTactics.WithParryCenter().WithLayers().WithFloor().WithShadeForms().WithGlutton();
        private static readonly FightRules Chief27 = Chief26.WithDuos();
        private static readonly FightRules Road26 =
            FightRules.Campaign.WithRoadKit().WithLayers().WithMarks().WithFloor().WithShadeForms().WithGlutton();
        private static readonly FightRules Road27 = Road26.WithDuos();

        private static List<UnitSpec> Side(params UnitSpec[] units) => new List<UnitSpec>(units);

        private static UnitSpec U(string id, UnitClass unitClass, int rank = 1)
        {
            var spec = new UnitSpec(id, unitClass, SliceClasses.Stats(unitClass).DefaultRow, UnitRole.Fighter, 100, 3000);
            return rank > 1 ? spec.WithRank(rank) : spec;
        }

        private static int Bit(params PalDuo[] duos) => duos.Aggregate(0, (m, d) => m | 1 << (int)d);

        // A kit with this Shade route at its first form, these talents held, and these duos.
        private static PalKit Kit(ShadeRoute route, GluttonTalent[] talents, params PalDuo[] duos)
        {
            var routes = new List<KeyValuePair<ShadeRoute, int>>();
            if (route != ShadeRoute.None) routes.Add(new KeyValuePair<ShadeRoute, int>(route, 1));
            var levels = new int[GluttonRules.TalentCount + 1];
            foreach (GluttonTalent t in talents ?? new GluttonTalent[0]) levels[(int)t] = GluttonRules.Held;
            return new PalKit(routes, 0).WithTalents(levels).WithDuos(Bit(duos));
        }

        // The Bloodridge chief with only his Cleave beside one fighter of his, against your Pal and these.
        private static BattleSimulation Cleaver(string id, PalKit kit, FightRules rules, params UnitSpec[] ours)
        {
            var side = Side(new UnitSpec("pal", UnitClass.Marksman, Row.Back, UnitRole.Pal, 100, 3000));
            side.AddRange(ours);
            return new BattleSimulation(new BattleSetup(id, 11, side,
                Side(new UnitSpec("x.s", UnitClass.Skirmisher, Row.Front, UnitRole.Fighter, 100, 3000),
                    new UnitSpec("chief", UnitClass.Warden, Row.Front, UnitRole.Chief, 100, 8000)),
                new TacticsProfile(), new TacticsProfile(), BattleSetup.DefaultTickLimit, BattleSetup.DefaultDirectivePoints,
                NerveRules.On(NerveRules.Full, NerveRules.Full), 10, 400, new SideRules(1, 0, true).WithPals(kit),
                new SideRules(2, 0, false, 0, ChiefSpell.Eclipse, kit: ChiefKit.Bloodridge, moves: ChiefMove.Cleave), rules));
        }

        // A road fight against a row of five of their Medics, m1 to m5, who never fight back.
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

        // The first event that matches, ticking on as needed; default if none came by this tick.
        private static BattleEvent First(BattleSimulation sim, SimSession session, Func<BattleEvent, bool> match, int ticks = 3000)
        {
            int seen = 0;
            while (true)
            {
                for (; seen < sim.Events.Count; seen++) if (match(sim.Events[seen])) return sim.Events[seen];
                if (sim.IsFinished || sim.CurrentTick >= ticks) return default;
                session.Tick();
            }
        }

        // ------------------------------------------------------------ the fight

        [Test]
        public void FattenedStarCountsEveryTalentAsFedOnTheStar()
        {
            GluttonTalent[] edge = { GluttonTalent.NucleusEdge };
            int FirstHit(PalKit kit, int skirmisherRank, int marksmanRank)
            {
                BattleSimulation sim = Cleaver("fat", kit, Chief27, U("s", UnitClass.Skirmisher, skirmisherRank), U("m", UnitClass.Marksman, marksmanRank));
                BattleEvent e = First(sim, new SimSession(sim), x => x.Kind == BattleEventKind.Hit && x.ActorId == "s");
                Assert.AreEqual(BattleEventKind.Hit, e.Kind, "the Skirmisher never hit");
                return e.Amount;
            }
            var fedLevels = new int[GluttonRules.TalentCount + 1];
            fedLevels[(int)GluttonTalent.NucleusEdge] = GluttonRules.Fed;
            PalKit fed = new PalKit(null, 0).WithTalents(fedLevels);
            int held = FirstHit(Kit(ShadeRoute.None, edge), 2, 1);
            Assert.Greater(FirstHit(fed, 2, 1), held, "fed hits harder");
            Assert.AreEqual(FirstHit(fed, 2, 1), FirstHit(Kit(ShadeRoute.None, edge, PalDuo.FattenedStar), 2, 1), "the star plays it fed");
            Assert.AreEqual(held, FirstHit(Kit(ShadeRoute.None, edge, PalDuo.FattenedStar), 1, 2), "another fighter keeps it held");
        }

        [Test]
        public void GorgedTricksHitHarderForEachTalentAndHalveHerPrices()
        {
            GluttonTalent[] two = { GluttonTalent.NucleusEdge, GluttonTalent.MatterPrinter };
            int blast = 0, returns = 0;
            void Mine(PalKit kit)
            {
                BattleSimulation sim = Cleaver("gorged", kit, Chief27, U("s", UnitClass.Skirmisher), U("m", UnitClass.Marksman));
                var session = new SimSession(sim);
                session.Tick();
                Assert.IsTrue(Order(session, Directive.FallBack));
                returns = sim.ReturnTicksLeft(0);
                blast = First(sim, session, e => e.Kind == BattleEventKind.Hit && e.Detail == ShadeRules.MineDetail).Amount;
            }
            Mine(Kit(ShadeRoute.RiftMine, two));
            int plainBlast = blast, plainReturn = returns;
            Mine(Kit(ShadeRoute.RiftMine, two, PalDuo.GorgedTricks));
            Assert.Greater(plainBlast, 0, "the mine never went off");
            Assert.AreEqual(plainBlast * (100 + 2 * DuoRules.GorgedTrickPercent) / 100, blast, 1, "15% a talent");
            Assert.Less(returns, plainReturn, "Rift Mine's price, halved");
        }

        [Test]
        public void MarkedTricksLaysTheLeadingFlavourWithHerTricks()
        {
            UnitSpec round = new UnitSpec("m", UnitClass.Marksman).WithBend(ClassBend.BlackHoleRound);
            int Marked(PalKit kit)
            {
                BattleSimulation sim = MedicRow("marked", kit, Road27, round, U("w", UnitClass.Warden), U("s", UnitClass.Skirmisher));
                var session = new SimSession(sim);
                session.Tick();
                Assert.IsTrue(Order(session, Directive.Unleash));
                BattleEvent breach = First(sim, session, e => e.Kind == BattleEventKind.Hit && e.Detail == ShadeRules.BreachDetail);
                Assert.AreEqual(ShadeRules.BreachDetail, breach.Detail, "no Breach");
                return sim.Events.Count(e => e.Kind == BattleEventKind.Mark && e.Tick == breach.Tick && e.Detail == MarkRules.Crush);
            }
            Assert.AreEqual(0, Marked(Kit(ShadeRoute.Breach, null)));
            Assert.Greater(Marked(Kit(ShadeRoute.Breach, null, PalDuo.MarkedTricks)), 0, "Gravity Well's Crush, laid by her Breach");
        }

        [Test]
        public void GorgedBurstsMakeABurstBiggerForItsLimbsTalents()
        {
            UnitSpec round = new UnitSpec("m", UnitClass.Marksman).WithBend(ClassBend.BlackHoleRound).WithComboRank(MarkRules.MaxStacks);
            GluttonTalent[] shooters = { GluttonRules.At(2, 0), GluttonRules.At(2, 1) };
            int Crushed(PalKit kit)
            {
                BattleSimulation sim = MedicRow("burst", kit, Road27, round, U("w", UnitClass.Warden));
                BattleEvent e = First(sim, new SimSession(sim), x => x.Kind == BattleEventKind.Hit && x.Detail == MarkRules.CrushedDetail, 6000);
                Assert.AreEqual(MarkRules.CrushedDetail, e.Detail, "no Crush burst");
                return e.Amount;
            }
            Assert.Greater(Crushed(Kit(ShadeRoute.None, shooters, PalDuo.GorgedBursts)), Crushed(Kit(ShadeRoute.None, shooters)));
        }

        [Test]
        public void CrownedMarksStarLaysTwiceAndCastsAgainOnHisBurst()
        {
            // A rank 2 combo lays 2 stacks: short of the cap of 3 alone, over it twice over on the star.
            UnitSpec star = new UnitSpec("m", UnitClass.Marksman).WithBend(ClassBend.BlackHoleRound).WithComboRank(2).WithRank(2);
            (bool burst, bool cast) Play(PalKit kit)
            {
                BattleSimulation sim = MedicRow("crown", kit, Road27, star, U("w", UnitClass.Warden));
                var session = new SimSession(sim);
                BattleEvent mark = First(sim, session, e => e.Kind == BattleEventKind.Mark && e.ActorId == "m", 6000);
                Assert.AreEqual(BattleEventKind.Mark, mark.Kind, "no mark");
                bool burst = sim.Events.Any(e => e.Kind == BattleEventKind.Burst && e.Tick == mark.Tick && e.TargetId == mark.TargetId);
                for (int k = 0; k < 3; k++) session.Tick();
                bool cast = sim.Events.Any(e => e.Kind == BattleEventKind.Trick && e.Detail == DuoRules.StarDetail && e.ActorId == "m");
                return (burst, cast);
            }
            var plain = Play(Kit(ShadeRoute.None, null));
            var crowned = Play(Kit(ShadeRoute.None, null, PalDuo.CrownedMarks));
            Assert.IsFalse(plain.burst, "2 stacks alone stay under the cap");
            Assert.IsTrue(crowned.burst, "twice the stacks burst at once");
            Assert.IsFalse(plain.cast);
            Assert.IsTrue(crowned.cast, "the burst gave the star his spell again");
        }

        [Test]
        public void ShadowStarGivesTheStarHisSpellWhenHerTrickHits()
        {
            bool Cast(PalKit kit, out BattleEvent star)
            {
                BattleSimulation sim = Cleaver("shadow", kit, Chief27, U("s", UnitClass.Skirmisher, 2), U("m", UnitClass.Marksman));
                var session = new SimSession(sim);
                session.Tick();
                Assert.IsTrue(Order(session, Directive.FallBack));
                BattleEvent mine = First(sim, session, e => e.Kind == BattleEventKind.Hit && e.Detail == ShadeRules.MineDetail);
                Assert.AreEqual(ShadeRules.MineDetail, mine.Detail, "the mine never went off");
                for (int k = 0; k < 3; k++) session.Tick();
                star = sim.Events.FirstOrDefault(e => e.Kind == BattleEventKind.Trick && e.Detail == DuoRules.StarDetail);
                return star.Detail == DuoRules.StarDetail;
            }
            Assert.IsFalse(Cast(Kit(ShadeRoute.RiftMine, null), out _));
            Assert.IsTrue(Cast(Kit(ShadeRoute.RiftMine, null, PalDuo.ShadowStar), out BattleEvent e2), "no star cast");
            Assert.AreEqual("s", e2.ActorId, "the star, the one ranked highest");
            Assert.AreEqual(1, e2.Amount);
        }

        [Test]
        public void BelowRules30DuosDoNothing()
        {
            UnitSpec round = new UnitSpec("m", UnitClass.Marksman).WithBend(ClassBend.BlackHoleRound).WithRank(2);
            GluttonTalent[] some = { GluttonTalent.NucleusEdge, GluttonRules.At(2, 0) };
            var all = Enumerable.Range(1, DuoRules.Count - 1).Select(d => (PalDuo)d).ToArray();
            string Played(PalKit kit)
            {
                BattleSimulation sim = MedicRow("old", kit, Road26, round, U("s", UnitClass.Skirmisher), U("w", UnitClass.Warden));
                var session = new SimSession(sim);
                for (int n = 0; n < 20 && !sim.IsFinished; n++)
                {
                    if (n % 4 == 0) Order(session, Directive.FallBack);
                    else if (n % 4 == 2) Order(session, Directive.Unleash);
                    for (int k = 0; k < 40 && !sim.IsFinished; k++) session.Tick();
                }
                return string.Join("\n", sim.Events.Select(e => e.Tick + " " + e.Kind + " " + e.ActorId + " " + e.TargetId + " " + e.Amount + " " + e.Detail));
            }
            Assert.AreEqual(Played(Kit(ShadeRoute.Breach, some)), Played(Kit(ShadeRoute.Breach, some, all)));
        }

        // ------------------------------------------------------------ the road

        [Test]
        public void ACardShowsTheDuoOnceTheRunHoldsBothPalsAndItSavesAndPlays()
        {
            // The first six duos show up to rules 33; from 34 the six that change a move take their place (DuoMoveTests).
            var easy = new CampaignTuning { EnemyScaleTier1 = 40, RulesCap = Campaign.VowFlavourRules };
            bool seen = false;
            for (ulong seed = 6600; seed < 6660 && !seen; seed++)
            {
                Campaign c = Campaign.New(seed, easy);
                if (!CampaignPilot.PlayTutorial(c, PilotStyle.Reference, seed, 10)) continue;
                c.StartRun();
                Assert.IsTrue(c.TryTakeRoute(ShadeRoute.Rupture));
                Assert.IsTrue(c.HoldsPickOf(Pal.Shade));
                Assert.IsFalse(c.HoldsPickOf(Pal.Glutton));
                Assert.IsTrue(c.TryLendTalent(GluttonTalent.NucleusEdge));
                int index = c.Offers.ToList().FindIndex(o => o.Kind != EncounterKind.Ambush && o.Node != null &&
                    (o.Node.Pal == Pal.Shade || o.Node.Pal == Pal.Glutton) && c.PalPickOffered(o.Node));
                if (index < 0) continue;
                EncounterResult r = CampaignPilot.PlayFight(c, c.StartEncounter(index), new CampaignPilot(PilotStyle.Reference, seed));
                if (!r.Won || c.FavourOffer == null) continue;
                seen = true;
                IReadOnlyList<Favour> choices = c.FavourOffer.Choices;
                Assert.AreEqual(Favour.DuoGorgedTricks, choices.Last(), "the first card after both shows their duo, last");
                Assert.AreEqual(1, choices.Count(FavourRules.IsDuo), "only the pair the run holds");
                Assert.IsTrue(c.DuoShown(PalDuo.GorgedTricks));

                // Saved with it standing, it comes back the same.
                string saved = CampaignSave.Write(c);
                Campaign back = CampaignSave.Read(saved, easy);
                Assert.IsEmpty(back.LoadNote);
                Assert.AreEqual(saved, CampaignSave.Write(back));

                Assert.IsTrue(back.TryTakeFavour(choices.Count - 1, out string why), why);
                Assert.IsTrue(back.HoldsDuo(PalDuo.GorgedTricks));
                Assert.AreEqual(1, back.FavourRank(Favour.DuoGorgedTricks));
                string taken = CampaignSave.Write(back);
                Campaign again = CampaignSave.Read(taken, easy);
                Assert.IsEmpty(again.LoadNote);
                Assert.IsTrue(again.HoldsDuo(PalDuo.GorgedTricks));
                Assert.AreEqual(taken, CampaignSave.Write(again));

                // The next fight brings it.
                CampaignFight next = again.StartEncounter(0);
                Assert.IsTrue(next.Sim.DuosOn);
                Assert.IsTrue(next.Sim.HasDuo(0, PalDuo.GorgedTricks));
            }
            Assert.IsTrue(seen, "no Shade or Glutton fight was won");
        }

        [Test]
        public void CappedAtRules29NoCardShowsADuo()
        {
            var old = new CampaignTuning { EnemyScaleTier1 = 40, RulesCap = Campaign.GluttonTreeRules };
            int cards = 0;
            for (ulong seed = 6600; seed < 6630 && cards < 3; seed++)
            {
                Campaign c = Campaign.New(seed, old);
                if (!CampaignPilot.PlayTutorial(c, PilotStyle.Reference, seed, 10)) continue;
                c.StartRun();
                Assert.IsFalse(c.DuosRun);
                Assert.IsFalse(c.TryTakeDuo(PalDuo.GorgedTricks));
                c.TryTakeRoute(ShadeRoute.Rupture);
                c.TryLendTalent(GluttonTalent.NucleusEdge);
                int index = c.Offers.ToList().FindIndex(o => o.Kind != EncounterKind.Ambush && o.Node != null &&
                    (o.Node.Pal == Pal.Shade || o.Node.Pal == Pal.Glutton) && c.PalPickOffered(o.Node));
                if (index < 0) continue;
                CampaignFight fight = c.StartEncounter(index);
                Assert.IsFalse(fight.Sim.DuosOn);
                EncounterResult r = CampaignPilot.PlayFight(c, fight, new CampaignPilot(PilotStyle.Reference, seed));
                if (!r.Won || c.FavourOffer == null) continue;
                cards++;
                Assert.IsFalse(c.FavourOffer.Choices.Any(FavourRules.IsDuo));
            }
            Assert.Greater(cards, 0, "no card came");
        }

        [Test]
        public void WholeRunsPlayWithEverySixDuos()
        {
            var easy = new CampaignTuning { EnemyScaleTier1 = 40 };
            int tricks = 0;
            for (ulong seed = 6700; seed < 6702; seed++)
            {
                Campaign c = Campaign.New(seed, easy);
                Assert.IsTrue(CampaignPilot.PlayTutorial(c, PilotStyle.Reference, seed, 10), "the tutorial was never won");
                c.StartRun();
                for (int d = 1; d < DuoRules.Count; d++) Assert.IsTrue(c.TryTakeDuo((PalDuo)d));
                for (int form = 0; form < ShadeRules.MaxForm; form++)
                {
                    c.TryTakeRoute(ShadeRoute.RiftMine);
                    c.TryTakeRoute(ShadeRoute.Breach);
                }
                for (int slot = 0; slot < GluttonRules.PerLimb; slot++) c.TryLendTalent(GluttonRules.At(1, slot));
                Assert.IsTrue(c.TryRankUp(c.Reserve.First(f => f.Class == UnitClass.Skirmisher).Id));
                var picker = new CampaignPilot(PilotStyle.Reference, seed);
                for (int n = 0; c.Stage == CampaignStage.InRun && n < 30; n++)
                {
                    CampaignFight fight = c.StartEncounter(picker.PickOffer(c));
                    Assert.IsTrue(fight.Sim.DuosOn);
                    CampaignPilot.PlayFight(c, fight, new CampaignPilot(PilotStyle.Reference, seed * 31UL + (ulong)n));
                    tricks += fight.Sim.Events.Count(e => e.Kind == BattleEventKind.Trick && e.Detail == DuoRules.StarDetail);
                    Assert.IsEmpty(CampaignSave.Read(CampaignSave.Write(c), easy).LoadNote, "the save reads back");
                }
            }
            TestContext.WriteLine("star casts: " + tricks);
            Assert.Greater(tricks, 0, "the star never cast from a duo");
        }
    }
}
