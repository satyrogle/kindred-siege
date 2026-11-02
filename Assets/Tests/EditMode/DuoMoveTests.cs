using System;
using System.Collections.Generic;
using System.Linq;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using NUnit.Framework;

namespace KindredSiege.Sim.Tests
{
    /// The duos that change a move (Jacob in chat, 2 Nov 2026: "merge first, then build the duos on top. Go"; FightRules.DuoMoves,
    /// campaign rules 34): the streak, the duos going off, their catches, and the free card. One quick test per piece, only to
    /// prove each runs. Nothing here is tuning.
    public class DuoMoveTests
    {
        private static readonly FightRules Chief33 = FightRules.CampaignChiefTactics.WithParryCenter().WithLayers().WithFloor().WithWhisper()
            .WithShadeForms().WithGlutton().WithDuos().WithSized().WithFlavours().WithVows();
        private static readonly FightRules Chief34 = Chief33.WithDuoMoves();
        private static readonly FightRules Road34 = FightRules.Campaign.WithRoadKit().WithLayers().WithMarks().WithFloor().WithWhisper()
            .WithShadeForms().WithGlutton().WithDuos().WithSized().WithFlavours().WithVows().WithDuoMoves();

        private static List<UnitSpec> Side(params UnitSpec[] units) => new List<UnitSpec>(units);

        private static UnitSpec U(string id, UnitClass cls, int rank = 1)
        {
            var spec = new UnitSpec(id, cls, SliceClasses.Stats(cls).DefaultRow, UnitRole.Fighter, 100, 3000);
            return rank > 1 ? spec.WithRank(rank) : spec;
        }

        private static PalKit Duo(PalDuo duo, int level = 1) => new PalKit(null, 0).WithDuos(1 << (int)duo).WithDuoLevel(level);

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

        // Ticks on until his Cleave's beat is this many ticks off, then Holds.
        private static void HoldBefore(BattleSimulation sim, SimSession session, int beatTicks)
        {
            while (!sim.IsFinished && sim.CurrentTick < BattleSetup.DefaultTickLimit)
            {
                if (sim.Threats(0).Any(t => t.Name == ChiefKits.CleaveName && t.BeatTicks == beatTicks)) break;
                session.Tick();
            }
            Assert.IsTrue(Order(session, Directive.Hold), "the Hold was refused");
            for (int k = 0; k < beatTicks; k++) session.Tick();
        }

        private static bool Seen(BattleSimulation sim, Func<BattleEvent, bool> match, int ticks = 9000)
        {
            var session = new SimSession(sim);
            while (!sim.IsFinished && sim.CurrentTick < ticks && !sim.Events.Any(match)) session.Tick();
            return sim.Events.Any(match);
        }

        [Test]
        public void AParryRaisesTheStreakAndAMoveThatLandsDropsIt()
        {
            BattleSimulation sim = Cleaver("streak", Duo(PalDuo.BloodParry), Chief34);
            Assert.AreEqual(1, sim.DuoLevelOf(0), "a side with one starts at 1");
            var session = new SimSession(sim);
            HoldBefore(sim, session, 1);
            Assert.IsTrue(sim.Events.Any(e => e.Kind == BattleEventKind.Parry && e.ActorSide == 0), "no parry");
            Assert.AreEqual(2, sim.DuoLevelOf(0), "the parry raised it");
            // Then nothing: the next Cleave lands in full, or one of yours falls.
            while (!sim.IsFinished && sim.DuoLevelOf(0) > 1 && sim.CurrentTick < BattleSetup.DefaultTickLimit) session.Tick();
            Assert.AreEqual(1, sim.DuoLevelOf(0), "a miss drops it to 1");
            Assert.AreEqual(0, Cleaver("none", PalKit.None, Chief34).DuoLevelOf(0), "no duo, no streak");
            Assert.AreEqual(Directives.Cost(Directive.FallBack) * 2, sim.OrderCost(0, Directive.FallBack), "Blood Parry's catch");
        }

        [Test]
        public void ChainReactionSplashesABurstOntoTheOnesBeside()
        {
            UnitSpec round = new UnitSpec("m", UnitClass.Marksman).WithBend(ClassBend.BlackHoleRound).WithComboRank(MarkRules.MaxStacks);
            Assert.IsTrue(Seen(MedicRow("chain", Duo(PalDuo.ChainReaction), Road34, round, U("w", UnitClass.Warden)),
                e => e.Kind == BattleEventKind.Burst && e.Detail == DuoRules.ChainDetail), "no chain");
            Assert.IsFalse(Seen(MedicRow("plain", PalKit.None, Road34, round, U("w", UnitClass.Warden)),
                e => e.Kind == BattleEventKind.Burst && e.Detail == DuoRules.ChainDetail), "not without it");
        }

        [Test]
        public void GorgeGrowsTheStarAndShadowTwinHalvesHim()
        {
            // Weak foes in a plain fight, so they fall to him: on the road they flee once their nerve breaks.
            var weak = new List<UnitSpec>();
            for (int i = 1; i <= 4; i++) weak.Add(new UnitSpec("x" + i, UnitClass.Skirmisher, Row.Front, UnitRole.Fighter, 100, 120));
            BattleSimulation sim = new BattleSimulation(new BattleSetup("gorge", 4, Side(U("s", UnitClass.Skirmisher, 2), U("w", UnitClass.Warden)), weak,
                new TacticsProfile(), new TacticsProfile(), BattleSetup.DefaultTickLimit, BattleSetup.DefaultDirectivePoints,
                NerveRules.On(NerveRules.Full, NerveRules.Full), 0, 400, new SideRules(0, 0, false).WithPals(Duo(PalDuo.Gorge)), null, Chief34));
            int before = sim.FindById("s").MaxHp;
            bool ate = Seen(sim, e => e.Kind == BattleEventKind.Whisper && e.Detail == DuoRules.GorgeDetail);
            Assert.IsTrue(ate, "he never ate");
            Assert.Greater(sim.FindById("s").MaxHp, before, "he grew");
            Assert.Greater(sim.GorgedOf(sim.FindById("s")), 0);

            BattleSimulation twin = MedicRow("twin", Duo(PalDuo.ShadowTwin), Road34, U("s", UnitClass.Skirmisher, 2), U("w", UnitClass.Warden));
            Assert.AreEqual(MedicRow("plain", PalKit.None, Road34, U("s", UnitClass.Skirmisher, 2), U("w", UnitClass.Warden)).FindById("s").MaxHp / 2,
                twin.FindById("s").MaxHp, "Shadow Twin's catch");
        }

        [Test]
        public void Below34TheNewDuosDoNothing()
        {
            int all = 0;
            for (int d = (int)DuoRules.FirstMove; d < DuoRules.Count; d++) all |= 1 << d;
            PalKit kit = new PalKit(null, 0).WithDuos(all).WithDuoLevel(3);
            string Played(PalKit k)
            {
                BattleSimulation sim = Cleaver("old", k, Chief33, U("s", UnitClass.Skirmisher, 2), U("m", UnitClass.Marksman));
                var session = new SimSession(sim);
                for (int n = 0; n < 900 && !sim.IsFinished; n++) session.Tick();
                return string.Join("\n", sim.Events.Select(e => e.Tick + " " + e.Kind + " " + e.ActorId + " " + e.TargetId + " " + e.Amount + " " + e.Detail));
            }
            Assert.AreEqual(Played(PalKit.None.WithDuoLevel(0)), Played(kit));
        }

        [Test]
        public void ADuoOnItsExactPicksShowsFreeAndTheStreakSaves()
        {
            var easy = new CampaignTuning { EnemyScaleTier1 = 40 };
            bool seen = false;
            for (ulong seed = 7100; seed < 7160 && !seen; seed++)
            {
                Campaign c = Campaign.New(seed, easy);
                if (!CampaignPilot.PlayTutorial(c, PilotStyle.Reference, seed, 10)) continue;
                c.StartRun();
                Assert.IsTrue(c.DuoMovesRun);
                // Gorge's picks: a vowed Skirmisher and a Strikers talent.
                CampaignFighter s = c.Reserve.First(f => f.Class == UnitClass.Skirmisher);
                Assert.IsTrue(c.TryRankUp(s.Id, StarVow.Reaver));
                Assert.IsFalse(c.MoveQualifies(PalDuo.Gorge));
                Assert.IsTrue(c.TryLendTalent(GluttonTalent.NucleusEdge));
                Assert.IsTrue(c.MoveQualifies(PalDuo.Gorge));
                Assert.IsFalse(c.MoveQualifies(PalDuo.BloodParry), "no combo, no Hold route");
                int index = c.Offers.ToList().FindIndex(o => o.Kind != EncounterKind.Ambush && o.Node != null &&
                    (o.Node.Pal == Pal.Whisperer || o.Node.Pal == Pal.Glutton) && c.PalPickOffered(o.Node));
                if (index < 0) continue;
                EncounterResult r = CampaignPilot.PlayFight(c, c.StartEncounter(index), new CampaignPilot(PilotStyle.Reference, seed));
                if (!r.Won || c.FavourOffer == null) continue;
                int duo = c.FavourOffer.Choices.ToList().IndexOf(Favour.DuoGorge);
                if (duo < 0) continue;
                seen = true;
                Assert.IsFalse(c.FavourOffer.Choices.Any(f => FavourRules.IsDuo(f) && f != Favour.DuoGorge), "only the duo whose picks it holds");
                int cards = c.FavourOffer.Choices.Count, materials = c.Materials;
                Assert.IsTrue(c.TryTakeFavour(duo, out string why), why);
                Assert.IsTrue(c.HoldsDuo(PalDuo.Gorge));
                Assert.IsNotNull(c.FavourOffer, "the card stays");
                Assert.AreEqual(cards - 1, c.FavourOffer.Choices.Count);
                Assert.AreEqual(materials, c.Materials, "free");
                Assert.AreEqual(1, c.DuoLevel);
                string saved = CampaignSave.Write(c);
                Campaign back = CampaignSave.Read(saved, easy);
                Assert.IsEmpty(back.LoadNote);
                Assert.AreEqual(saved, CampaignSave.Write(back));
                Assert.IsTrue(back.TryTakeFavour(0, out why), why);
                CampaignFight next = back.StartEncounter(0);
                Assert.IsTrue(next.Sim.DuoMovesOn);
                Assert.AreEqual(1, next.Sim.DuoLevelOf(0));
            }
            Assert.IsTrue(seen, "no card showed Gorge");
        }
    }
}
