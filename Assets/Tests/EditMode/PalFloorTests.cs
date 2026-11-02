using System;
using System.Collections.Generic;
using System.Linq;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using NUnit.Framework;

namespace KindredSiege.Sim.Tests
{
    /// The floor under the three Pals (Jacob, 31 Oct 2026: "Go, sprint it"; DESIGN_LOCKS, "The floor under the three Pals";
    /// FightRules.Floor, campaign rules 23). One quick test per piece, only to prove each runs (Jacob, 12 Oct 2026: build
    /// first). Nothing here is tuning.
    public class PalFloorTests
    {
        private static readonly FightRules Floor = FightRules.Campaign.WithLayers().WithMarks().WithFloor();

        private static List<UnitSpec> Side(params UnitSpec[] units) => new List<UnitSpec>(units);

        private static UnitSpec U(string id, UnitClass unitClass, int health = 3000)
        {
            return new UnitSpec(id, unitClass, SliceClasses.Stats(unitClass).DefaultRow, UnitRole.Fighter, 100, health);
        }

        // Medics in their front row never fight back.
        private static List<UnitSpec> Medics(int count, int health = 100)
        {
            var medics = new List<UnitSpec>();
            for (int i = 1; i <= count; i++) medics.Add(new UnitSpec("m" + i, UnitClass.Medic, Row.Front, UnitRole.Fighter, 100, health));
            return medics;
        }

        private static BattleSimulation Fight(string id, List<UnitSpec> ours, List<UnitSpec> theirs, FightRules rules, SideRules mine = null,
            NerveRules nerve = null)
        {
            return new BattleSimulation(new BattleSetup(id, 4, ours, theirs, new TacticsProfile(), new TacticsProfile(),
                BattleSetup.DefaultTickLimit, BattleSetup.DefaultDirectivePoints, nerve ?? NerveRules.Off, 0, 400, mine, null, rules));
        }

        private static PalKit Kit(ShadeRoute route, int orderCostPercent = 0)
        {
            var routes = new List<KeyValuePair<ShadeRoute, int>>();
            if (route != ShadeRoute.None) routes.Add(new KeyValuePair<ShadeRoute, int>(route, 1));
            return new PalKit(routes, orderCostPercent);
        }

        // The Bloodridge chief with only his Cleave, as the meter tests have him, and your side bringing this kit.
        private static BattleSimulation Cleaver(string id, PalKit kit)
        {
            FightRules played = FightRules.CampaignChiefTactics.WithParryCenter().WithLayers().WithFloor();
            var ours = Side(new UnitSpec("pal", UnitClass.Marksman, Row.Back, UnitRole.Pal, 100, 3000),
                U("s", UnitClass.Skirmisher), U("m", UnitClass.Marksman), U("t", UnitClass.Trapper));
            return new BattleSimulation(new BattleSetup(id, 11, ours,
                Side(U("x.s", UnitClass.Skirmisher), new UnitSpec("chief", UnitClass.Warden, Row.Front, UnitRole.Chief, 100, 8000)),
                new TacticsProfile(), new TacticsProfile(), BattleSetup.DefaultTickLimit, BattleSetup.DefaultDirectivePoints,
                NerveRules.On(NerveRules.Full, NerveRules.Full), 10, 400, new SideRules(1, 0, true).WithPals(kit),
                new SideRules(2, 0, false, 0, ChiefSpell.Eclipse, kit: ChiefKit.Bloodridge, moves: ChiefMove.Cleave), played));
        }

        private static bool Order(SimSession session, Directive directive, string target = "")
        {
            return session.TryIssue(Directives.CommandKind, "0", target, 0, 0, Directives.Name(directive), out _);
        }

        // The index of the first event from this index on that matches, ticking on as needed up to this tick; -1 if none.
        private static int RunUntil(BattleSimulation sim, int ticks, Func<BattleEvent, bool> match, int from = 0, SimSession session = null)
        {
            int seen = from;
            while (true)
            {
                for (; seen < sim.Events.Count; seen++) if (match(sim.Events[seen])) return seen;
                if (sim.IsFinished || sim.CurrentTick >= ticks) return -1;
                if (session != null) session.Tick();
                else sim.Tick();
            }
        }

        [Test]
        public void TheStoresKeepEachPalsPicksAndTheSaveKeepsThem()
        {
            var easy = new CampaignTuning { EnemyScaleTier1 = 40 };
            Campaign c = Campaign.New(5400, easy);
            Assert.IsTrue(CampaignPilot.PlayTutorial(c, PilotStyle.Reference, 5400, 10), "the tutorial was never won");
            // From rules 24 habits are set at camp (WhispererTests): the habit replaces its class's old one.
            Assert.IsTrue(c.TrySetHabit(WhispererHabit.Spotter));
            Assert.IsTrue(c.TrySetHabit(WhispererHabit.CounterShot));
            Assert.AreEqual(WhispererHabit.CounterShot, c.Habits.Single(h => h.Class == UnitClass.Marksman).Habit);
            c.StartRun();

            // The Glutton's talents, lent once each, and Feed once a run.
            Assert.IsTrue(c.TryLendTalent(GluttonTalent.LightStake));
            Assert.IsFalse(c.TryLendTalent(GluttonTalent.LightStake), "held already");
            Assert.IsTrue(c.TryLendTalent(GluttonTalent.NucleusEdge));
            Assert.IsTrue(c.TryFeedTalent(GluttonTalent.LightStake));
            Assert.IsFalse(c.TryFeedTalent(GluttonTalent.NucleusEdge), "Feed once a run");

            // The Shade's routes: a held one climbs its forms, two buttons at most, one route a button.
            Assert.IsTrue(c.TryTakeRoute(ShadeRoute.Sanctuary));
            Assert.IsTrue(c.TryTakeRoute(ShadeRoute.Sanctuary));
            Assert.AreEqual(2, c.ShadeRoutes.Single(r => r.Route == ShadeRoute.Sanctuary).Form);
            Assert.IsFalse(c.TryTakeRoute(ShadeRoute.Mirror), "Hold already carries Sanctuary");
            Assert.IsTrue(c.TryTakeRoute(ShadeRoute.Backstab));
            Assert.IsFalse(c.TryTakeRoute(ShadeRoute.Fear), "two buttons at most");

            // The pair replaces the old pair.
            Assert.IsTrue(c.TrySetPair(SquadPair.PinAndShoot));

            // A fighter ranks to 3, which opens his second slot: here a combo of his class, from a save as the road would give it.
            CampaignFighter star = c.Reserve[0];
            Assert.IsFalse(c.TryEquipSecond(star.Id, "nothing", out _), "no second slot below rank 3");
            Assert.IsTrue(c.TryRankUp(star.Id));
            Assert.IsTrue(c.TryRankUp(star.Id));
            Assert.IsFalse(c.TryRankUp(star.Id), "rank 3 is the top");
            ClassBend bend = Enumerable.Range((int)ClassBend.BlackHoleRound, 15).Select(i => (ClassBend)i)
                .First(b => ClassSpells.IsFor(b, star.Class) && ClassSpells.AugmentOf(b) == Augment.TimeStop);
            var lines = CampaignSave.Write(c).Split('\n').ToList();
            int at = lines.FindLastIndex(l => l.StartsWith("fighter\t"));
            lines.Insert(at + 1, string.Join("\t", "piece", Campaign.ComboGearId(bend), CampaignTribes.ClarthId, "0", "0", "0",
                ClassSpells.Name(bend), ((int)star.Class).ToString(), ((int)bend).ToString()));
            Campaign r = CampaignSave.Read(string.Join("\n", lines), easy);
            Assert.IsEmpty(r.LoadNote);
            Assert.IsTrue(r.TryEquipSecond(star.Id, Campaign.ComboGearId(bend), out string why), why);

            // All of it comes back from a save.
            string saved = CampaignSave.Write(r);
            Campaign back = CampaignSave.Read(saved, easy);
            Assert.IsEmpty(back.LoadNote);
            Assert.AreEqual(saved, CampaignSave.Write(back));
            Assert.AreEqual(2, back.Talents.Count);
            Assert.IsTrue(back.Talents.Single(t => t.Talent == GluttonTalent.LightStake).Fed);
            Assert.AreEqual(2, back.ShadeRoutes.Count);
            Assert.AreEqual(SquadPair.PinAndShoot, back.Pair);
            Assert.AreEqual(3, back.Reserve.First(f => f.Id == star.Id).Rank);
            Assert.AreEqual(Campaign.ComboGearId(bend), back.Reserve.First(f => f.Id == star.Id).GearId2);

            // TryUnequipSecond takes it off again, and it can go back on.
            Campaign off = CampaignSave.Read(saved, easy);
            Assert.IsTrue(off.TryUnequipSecond(star.Id, out why), why);
            Assert.AreEqual(string.Empty, off.Reserve.First(f => f.Id == star.Id).GearId2);
            Assert.IsTrue(off.TryEquipSecond(star.Id, Campaign.ComboGearId(bend), out why), why);
            Assert.IsFalse(off.TryUnequipSecond("nobody", out _), "not one of yours");

            // And the second piece rides into his next fight.
            CampaignFight fight = back.StartEncounter(new CampaignPilot(PilotStyle.Reference, 5400).PickOffer(back));
            UnitSpec spec = fight.Setup.Side(0).SingleOrDefault(s => s.Id == star.Id);
            Assert.IsNotNull(spec, "the star fights");
            Assert.AreEqual(bend, spec.Bend2);
            Assert.IsTrue(fight.Sim.FloorOn);

            // A run capped at rules 22 keeps no store.
            Campaign old = Campaign.New(5400, new CampaignTuning { EnemyScaleTier1 = 40, RulesCap = Campaign.FloorRules - 1 });
            Assert.IsTrue(CampaignPilot.PlayTutorial(old, PilotStyle.Reference, 5400, 10));
            old.StartRun();
            Assert.IsFalse(old.TryLendTalent(GluttonTalent.LightStake));
            Assert.IsFalse(old.TryRankUp(old.Reserve[0].Id));
        }

        [Test]
        public void ARank3FighterCarriesTwoCombosAndBothFire()
        {
            // A Marksman with Black Hole Round and, in his second slot, Stopped Bullets: pull and time on one fighter.
            UnitSpec both = new UnitSpec("a", UnitClass.Marksman).WithBend(ClassBend.BlackHoleRound).WithSecondPiece(ClassBend.StoppedBullets, 1);
            var sim = Fight("two", Side(both), Medics(3, 400), Floor);
            Assert.GreaterOrEqual(RunUntil(sim, 4000, e => e.Kind == BattleEventKind.Freeze && e.Detail == ClassSpells.Name(ClassBend.StoppedBullets)), 0,
                "Stopped Bullets never froze anyone");
            Assert.GreaterOrEqual(RunUntil(sim, 6000, e => e.Kind == BattleEventKind.Pull && e.Detail == ClassSpells.Name(ClassBend.BlackHoleRound)), 0,
                "Black Hole Round never opened");

            // Both pacts apply: Borrowed time, the Time Stop's, takes an eighth of his health.
            var one = Fight("one", Side(new UnitSpec("a", UnitClass.Marksman).WithBend(ClassBend.BlackHoleRound)), Medics(3, 400), Floor);
            Assert.Less(sim.FindById("a").MaxHp, one.FindById("a").MaxHp);

            // Below rules 23 the second piece does nothing.
            var old = Fight("old", Side(both), Medics(3, 400), FightRules.Campaign.WithLayers().WithMarks());
            Assert.IsFalse(old.FloorOn);
            Assert.AreEqual(one.FindById("a").MaxHp, old.FindById("a").MaxHp);
        }

        [Test]
        public void TheFasterHookShortensClocksAndTheCheaperHookOrders()
        {
            // His aim at half the time with a clock percent of minus 50.
            var plain = Fight("aim", Side(new UnitSpec("a", UnitClass.Marksman)), Medics(1, 400), Floor);
            var fast = Fight("aim", Side(new UnitSpec("a", UnitClass.Marksman).WithClockPercent(-50)), Medics(1, 400), Floor);
            BattleEvent slow = plain.Events[RunUntil(plain, 4000, e => e.Kind == BattleEventKind.Aim)];
            BattleEvent quick = fast.Events[RunUntil(fast, 4000, e => e.Kind == BattleEventKind.Aim)];
            Assert.AreEqual(Math.Max(1, slow.Amount * 50 / 100), quick.Amount);

            // Focus Fire at half its points with an order cost percent of minus 50.
            int full = Directives.Cost(Directive.FocusFire);
            BattleSimulation sim = Cleaver("cost", Kit(ShadeRoute.None, -50));
            var session = new SimSession(sim);
            int points = sim.DirectivePoints(0);
            Assert.IsTrue(Order(session, Directive.FocusFire, "x.s"));
            Assert.AreEqual(points - full * 50 / 100, sim.DirectivePoints(0));
        }

        [Test]
        public void SanctuaryShieldsTheSquadOnACleanParry()
        {
            BattleSimulation sim = Cleaver("sanctuary", Kit(ShadeRoute.Sanctuary));
            var session = new SimSession(sim);
            while (!sim.IsFinished && sim.CurrentTick < BattleSetup.DefaultTickLimit)
            {
                if (sim.Threats(0).Any(t => t.Name == ChiefKits.CleaveName && t.BeatTicks == 1)) break;
                session.Tick();
            }
            Assert.IsTrue(Order(session, Directive.Hold), "the Hold was refused");
            session.Tick();
            Assert.IsTrue(sim.Events.Any(e => e.Kind == BattleEventKind.Parry && e.ActorSide == 0), "the Cleave was not parried");
            Assert.IsTrue(sim.Events.Any(e => e.Kind == BattleEventKind.Trick && e.Detail == ShadeRules.SanctuaryDetail));
            BattleUnit skirmisher = sim.FindById("s");
            Assert.IsTrue(skirmisher.Statuses[StatusKind.Shield].On, "the squad is shielded");
            Assert.AreEqual(skirmisher.MaxHp * ShadeRules.ShieldPercent / 100, skirmisher.Statuses[StatusKind.Shield].Size);
        }

        [Test]
        public void BackstabDoublesTheNextHitAfterAFallBack()
        {
            BattleSimulation sim = Cleaver("backstab", Kit(ShadeRoute.Backstab));
            var session = new SimSession(sim);
            session.Tick();
            Assert.IsTrue(Order(session, Directive.FallBack), "the Fall Back was refused");
            Assert.IsTrue(sim.Events.Any(e => e.Kind == BattleEventKind.Trick && e.Detail == ShadeRules.BackstabDetail));
            int at = RunUntil(sim, 3000, e => e.Kind == BattleEventKind.Cash && e.Detail == ShadeRules.BackstabDetail && e.ActorSide == 0, 0, session);
            Assert.GreaterOrEqual(at, 0, "no blow came from behind");
            Assert.Greater(sim.Events[at].Amount, 0, "it doubled the blow");
        }

        [Test]
        public void FearShakesTheEnemiesBesideTheFocusedOneWhenHeFalls()
        {
            // Three fragile fighters of theirs in a row (Medics would heal each other): Focus Fire the middle one. A fight where they die rather than break on nerve (in a road
            // fight they mostly break first, so this one has no nerve of its own; the nerve they lose is UnitDrain's, as tested
            // with road combat).
            FightRules rules = FightRules.Campaign.WithLayers().WithFloor();
            var ours = Side(U("s", UnitClass.Skirmisher), U("m", UnitClass.Marksman), U("w", UnitClass.Warden));
            var theirs = Side(new UnitSpec("m1", UnitClass.Skirmisher, Row.Front, UnitRole.Fighter, 100, 40),
                new UnitSpec("m2", UnitClass.Skirmisher, Row.Front, UnitRole.Fighter, 100, 40),
                new UnitSpec("m3", UnitClass.Skirmisher, Row.Front, UnitRole.Fighter, 100, 40));
            BattleSimulation sim = Fight("fear", ours, theirs, rules, new SideRules(0, 0, false).WithPals(Kit(ShadeRoute.Fear)));
            var session = new SimSession(sim);
            int at = -1;
            for (int n = 0; n < 40 && at < 0 && !sim.IsFinished; n++)
            {
                if (sim.FindById("m2").Alive) Order(session, Directive.FocusFire, "m2");
                at = RunUntil(sim, (int)sim.CurrentTick + 60, e => e.Kind == BattleEventKind.Trick && e.Detail == ShadeRules.FearDetail, 0, session);
            }
            Assert.GreaterOrEqual(at, 0, "Fear never went off");
            Assert.AreEqual("m2", sim.Events[at].TargetId);
            Assert.Greater(sim.Events[at].Amount, 0, "someone stood beside him");
            Assert.IsTrue(new[] { "m1", "m3" }.Any(id => sim.FindById(id).Alive && sim.FindById(id).PanicTicks > 0), "one beside him panics");
        }

        [Test]
        public void FearAlsoGoesOffWhenTheFocusedOneBreaks()
        {
            // Jacob, 31 Oct 2026: "Yes, on break too". In a road fight his Medics break on nerve and flee before anyone kills
            // them: the middle one under your Focus Fire breaks, and those beside him are shaken.
            FightRules rules = FightRules.Campaign.WithRoadCombat().WithLayers().WithFloor();
            var ours = Side(U("s", UnitClass.Skirmisher), U("m", UnitClass.Marksman), U("w", UnitClass.Warden));
            BattleSimulation sim = Fight("fear.break", ours, Medics(3, 100), rules, new SideRules(0, 0, false).WithPals(Kit(ShadeRoute.Fear)),
                NerveRules.On(NerveRules.Full, NerveRules.Full));
            var session = new SimSession(sim);
            int at = -1;
            for (int n = 0; n < 40 && at < 0 && !sim.IsFinished; n++)
            {
                if (sim.FindById("m2").Alive) Order(session, Directive.FocusFire, "m2");
                at = RunUntil(sim, (int)sim.CurrentTick + 60, e => e.Kind == BattleEventKind.Trick && e.Detail == ShadeRules.FearDetail, 0, session);
            }
            Assert.GreaterOrEqual(at, 0, "Fear never went off");
            Assert.AreEqual("m2", sim.Events[at].TargetId);
            Assert.IsFalse(sim.Events.Any(e => e.Kind == BattleEventKind.Kill && e.TargetId == "m2"), "he broke, nobody killed him");
            Assert.IsTrue(sim.FindById("m2").Fled, "he fled");
            Assert.AreEqual(2, sim.Events[at].Amount, "both beside him were shaken");
        }
    }
}
