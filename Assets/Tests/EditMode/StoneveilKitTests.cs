using System;
using System.Collections.Generic;
using System.Linq;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using NUnit.Framework;

namespace KindredSiege.Sim.Tests
{
    /// The Stoneveil chief's kit, the blood rite (Jacob, 22 Oct 2026, later: "go, build the Stoneveil and Thundercoast chief
    /// kits in the sim and hand the API to Codex"; DESIGN_LOCKS "The Stoneveil and Thundercoast chiefs"). One quick test per
    /// piece, only to prove it runs. Nothing here is tuning.
    public class StoneveilKitTests
    {
        // A fighter at these percents of power and health, in its class's row, its blades leaving wounds that bleed this
        // percent a second. Tough by default, so fights last.
        private static UnitSpec U(string id, UnitClass unitClass, int power = 100, int health = 1000, int bleed = 0)
        {
            return new UnitSpec(id, unitClass, SliceClasses.Stats(unitClass).DefaultRow, UnitRole.Fighter, power, health, 0, 0, 0, 0, 0,
                0, 0, 0, bleed);
        }

        // One of his band, whose blades bleed.
        private static UnitSpec Blade(string id, UnitClass unitClass = UnitClass.Skirmisher)
        {
            return U(id, unitClass, bleed: 2);
        }

        // The blood priest leads from the back, a Medic, as in the campaign.
        private static UnitSpec Chief(int health = 1000)
        {
            return new UnitSpec("chief", UnitClass.Medic, Row.Back, UnitRole.Chief, 100, health);
        }

        private static List<UnitSpec> Side(params UnitSpec[] units)
        {
            return new List<UnitSpec>(units);
        }

        // His side plays his kit with these moves and no heavy swings; ours has a grade 1 Eclipse on its 5th button, and combos.
        // No nerve unless asked, so nothing breaks; with tactics, his Blood Price too.
        private static BattleSimulation Fight(string id, ulong seed, List<UnitSpec> ours, List<UnitSpec> theirs, ChiefMove moves,
            bool nerve = false, bool tactics = false)
        {
            var kit = new SideRules(2, (moves & ChiefMove.BloodCall) != 0 ? ChiefKits.BloodCallEveryTicks : 0, false, 0,
                ChiefSpell.BloodCall, kit: ChiefKit.Stoneveil, moves: moves);
            return new BattleSimulation(new BattleSetup(id, seed, ours, theirs, new TacticsProfile(), new TacticsProfile(),
                BattleSetup.DefaultTickLimit, BattleSetup.DefaultDirectivePoints, nerve ? NerveRules.On() : NerveRules.Off, 10, 400,
                new SideRules(1, 0, true), kit, tactics ? FightRules.CampaignChiefTactics : FightRules.CampaignChiefKit));
        }

        private static bool Order(SimSession session, string directive, string target = "")
        {
            return session.TryIssue(Directives.CommandKind, "0", target, 0, 0, directive, out _);
        }

        // Ticks until an event matches, calling before ahead of each tick; events from before the call are not looked at.
        private static BattleEvent? Next(BattleSimulation sim, SimSession session, Func<BattleEvent, bool> match, Action before = null)
        {
            int seen = sim.Events.Count;
            while (!sim.IsFinished && sim.CurrentTick < BattleSetup.DefaultTickLimit)
            {
                before?.Invoke();
                session.Tick();
                for (; seen < sim.Events.Count; seen++)
                    if (match(sim.Events[seen])) return sim.Events[seen];
            }
            return null;
        }

        private static string Landed(string move, string how)
        {
            return move + "." + how;
        }

        private static bool Land(BattleEvent e)
        {
            return e.Kind == BattleEventKind.Land;
        }

        private static bool GloryAt(BattleSimulation sim, ChiefAnswer answer, long tick)
        {
            return sim.Events.Any(e => e.Kind == BattleEventKind.Glory && e.Detail == ChiefKits.Name(answer) && e.Tick == tick);
        }

        private static bool EatenAt(BattleSimulation sim, long tick)
        {
            return sim.Events.Any(e => e.Kind == BattleEventKind.Glory && e.Detail == ChiefKits.EatenDetail && e.Tick == tick);
        }

        private static int Count(ChiefMove moves)
        {
            int n = 0;
            for (int bit = 0; bit < 30; bit++)
                if (((int)moves & (1 << bit)) != 0) n++;
            return n;
        }

        private const string Call = "Blood Call";

        // ------------------------------------------------------------ Blood Call

        [Test]
        public void HisChantEndsInABloodCallThatRipsYourBleedingUnlessYouFallBackOrYourMedicStitchesTheirWounds()
        {
            List<UnitSpec> Theirs() => Side(Blade("x1"), Blade("x2"), Chief());

            // Eaten: no Medic of ours, and their blades keep us bleeding.
            var sim = Fight("sv.call", 81, Side(U("a", UnitClass.Warden), U("b", UnitClass.Skirmisher), U("m", UnitClass.Marksman)), Theirs(),
                ChiefMove.BloodCall);
            var session = new SimSession(sim);
            Assert.AreEqual(ChiefKit.Stoneveil, sim.Kit(1));
            Assert.AreEqual(Call, ChiefKits.Name(ChiefMove.BloodCall));
            BattleEvent chant = Next(sim, session, e => e.Kind == BattleEventKind.Windup && e.ActorId == "chief").Value;
            Assert.AreEqual(ChiefKits.BloodCallEveryTicks, chant.Tick);
            Assert.AreEqual(Moves.EclipseWindupTicks, chant.Amount, "a 3 s chant");
            Assert.AreEqual(Call, chant.Detail);
            BattleEvent land = Next(sim, session, Land).Value;
            Assert.AreEqual(Landed(Call, ChiefKits.Full), land.Detail);
            Assert.AreEqual(chant.Tick + Moves.EclipseWindupTicks - 1, land.Tick);
            int ripped = sim.Events.Where(e => e.Kind == BattleEventKind.Hit && e.Detail == ChiefSpells.BloodDetail && e.Tick == land.Tick)
                .Sum(e => e.Amount);
            Assert.Greater(ripped, 0);
            Assert.AreEqual(land.Amount, ripped, "the Land says all it ripped");
            Assert.IsTrue(EatenAt(sim, land.Tick));
            Assert.IsTrue(sim.Events.Any(e => e.Kind == BattleEventKind.Line && e.Detail == ChiefKits.EclipseLine && e.Tick == land.Tick));
            Assert.AreEqual(string.Empty, ChiefKits.Line(ChiefKit.Stoneveil, ChiefKits.EclipseLine, false), "his words are Jacob's to write");

            // Dodged: Fall Back as the chant ends.
            var dodge = Fight("sv.call.dodge", 81, Side(U("a", UnitClass.Warden), U("b", UnitClass.Skirmisher), U("m", UnitClass.Marksman)),
                Theirs(), ChiefMove.BloodCall);
            var ds = new SimSession(dodge);
            BattleEvent dodged = Next(dodge, ds, Land, () =>
            {
                if (dodge.WindupTicks(1) > 0 && dodge.WindupTicks(1) <= 20 && dodge.FallBackTicksLeft(0) == 0) Order(ds, "FallBack");
            }).Value;
            Assert.AreEqual(Landed(Call, ChiefKits.Dodged), dodged.Detail);
            Assert.IsFalse(dodge.Events.Any(e => e.Kind == BattleEventKind.Hit && e.Detail == ChiefSpells.BloodDetail));
            Assert.AreEqual(ChiefKits.DodgeGlory, dodge.Glory(0));

            // Stitched: our Medic's treatments during the chant close wounds shut, which answers it.
            var stitch = Fight("sv.call.stitch", 81, Side(U("a", UnitClass.Warden), U("b", UnitClass.Skirmisher), U("h", UnitClass.Medic)),
                Theirs(), ChiefMove.BloodCall);
            var ss = new SimSession(stitch);
            BattleEvent chant2 = Next(stitch, ss, e => e.Kind == BattleEventKind.Windup && e.ActorId == "chief").Value;
            BattleEvent healed = Next(stitch, ss, Land).Value;
            Assert.AreEqual(Landed(Call, ChiefKits.Healed), healed.Detail);
            Assert.IsTrue(stitch.Events.Any(e => e.Kind == BattleEventKind.Cash && e.Detail == ChiefKits.StitchDetail && e.ActorId == "h" &&
                e.Tick >= chant2.Tick && e.Tick <= healed.Tick), "she stitched a wound during the chant");
            Assert.AreEqual(ChiefKits.HealGlory, stitch.Glory(0));
        }

        // ------------------------------------------------------------ the Basin

        [Test]
        public void HisAcolyteCarriesTheBasinAndItFillsFromYourBleedingUnlessYouSpillItKickItOverOrStarveIt()
        {
            List<UnitSpec> Theirs(int bleed) => Side(U("x1", UnitClass.Skirmisher, bleed: bleed), U("aco", UnitClass.Medic), Chief());
            List<UnitSpec> Ours() => Side(U("a", UnitClass.Warden, 300), U("b", UnitClass.Skirmisher, 300), U("m", UnitClass.Marksman, 300));

            // Drunk: we bleed into it, and he drinks. We focus him first, so there is something to heal.
            var sim = Fight("sv.basin", 82, Ours(), Theirs(2), ChiefMove.Basin);
            var session = new SimSession(sim);
            BattleEvent tell = Next(sim, session, e => e.Kind == BattleEventKind.Tell, () =>
            {
                if (sim.FocusTicksLeft(0) == 0 && sim.DirectivePoints(0) > 0) Order(session, "FocusFire", "chief");
            }).Value;
            Assert.AreEqual(ChiefKits.BasinName, tell.Detail);
            Assert.AreEqual("aco", tell.ActorId, "his acolyte, his band's Medic, carries it");
            Assert.AreEqual(ChiefKits.BasinEveryTicks, tell.Tick);
            Assert.AreEqual(ChiefKits.BasinTicks, tell.Amount);
            Assert.AreEqual("aco", sim.BasinCarrierId(1));
            BattleEvent drunk = Next(sim, session, Land).Value;
            Assert.AreEqual(Landed(ChiefKits.BasinName, ChiefKits.Full), drunk.Detail);
            Assert.LessOrEqual(drunk.Tick, tell.Tick + ChiefKits.BasinTicks - 1);
            Assert.AreEqual(ChiefKits.DrinkTicks, sim.DrinkTicksLeft(1), "his band hits harder now");
            Assert.Greater(drunk.Amount, 0);
            Assert.IsTrue(sim.Events.Any(e => e.Kind == BattleEventKind.Heal && e.Detail == ChiefKits.BasinDetail && e.TargetId == "chief" &&
                e.Amount == drunk.Amount && e.Tick == drunk.Tick), "he heals what the Land says");
            Assert.IsFalse(sim.Events.Any(e => e.Kind == BattleEventKind.Glory && e.Tick == drunk.Tick), "it is no answer, and nothing eaten");

            // Spilled: Focus Fire his acolyte while she carries it.
            var spill = Fight("sv.basin.spill", 82, Ours(), Theirs(2), ChiefMove.Basin);
            var ps = new SimSession(spill);
            BattleEvent spilled = Next(spill, ps, Land, () =>
            {
                if (spill.BasinTicksLeft(1) > 0 && spill.FocusedId(0) != "aco") Order(ps, "FocusFire", "aco");
            }).Value;
            Assert.AreEqual(Landed(ChiefKits.BasinName, ChiefKits.Broken), spilled.Detail);
            Assert.IsTrue(GloryAt(spill, ChiefAnswer.Break, spilled.Tick));
            Assert.AreEqual(0, spill.DrinkTicksLeft(1));

            // Kicked over: Unleash while she carries it.
            var kick = Fight("sv.basin.kick", 82, Ours(), Theirs(2), ChiefMove.Basin);
            var ks = new SimSession(kick);
            Next(kick, ks, e => e.Kind == BattleEventKind.Tell);
            Assert.IsTrue(Order(ks, "Unleash"));
            BattleEvent kicked = kick.Events.Last(Land);
            Assert.AreEqual(Landed(ChiefKits.BasinName, ChiefKits.Smashed), kicked.Detail);
            Assert.IsTrue(GloryAt(kick, ChiefAnswer.Smash, kicked.Tick));
            Assert.AreEqual(0, kick.BasinTicksLeft(1));

            // Starved: nobody of ours bleeds, so it comes back empty, a new answer.
            var starve = Fight("sv.basin.starve", 82, Ours(), Theirs(0), ChiefMove.Basin);
            var ts = new SimSession(starve);
            BattleEvent carried = Next(starve, ts, e => e.Kind == BattleEventKind.Tell).Value;
            BattleEvent empty = Next(starve, ts, Land).Value;
            Assert.AreEqual(Landed(ChiefKits.BasinName, ChiefKits.Empty), empty.Detail);
            Assert.AreEqual(carried.Tick + ChiefKits.BasinTicks - 1, empty.Tick);
            Assert.AreEqual(ChiefKits.StarveGlory, starve.Glory(0));
            Assert.AreEqual("starve", ChiefKits.Name(ChiefAnswer.Starve));
        }

        // ------------------------------------------------------------ Leeches

        [Test]
        public void HisLeechesDrinkFromYourBackRowAndFeedHimUnlessYourMedicPlucksThemOffOrYouFallBack()
        {
            List<UnitSpec> Theirs() => Side(U("x1", UnitClass.Skirmisher), Chief());

            // Eaten: they drink our only back row fighter each second for six seconds.
            var sim = Fight("sv.leech", 83, Side(U("a", UnitClass.Warden), U("m", UnitClass.Marksman)), Theirs(), ChiefMove.Leeches);
            var session = new SimSession(sim);
            BattleEvent tell = Next(sim, session, e => e.Kind == BattleEventKind.Tell).Value;
            Assert.AreEqual(ChiefKits.LeechName, tell.Detail);
            Assert.AreEqual(ChiefKits.LeechEveryTicks, tell.Tick);
            Assert.AreEqual(ChiefKits.LeechTicks, tell.Amount);
            Assert.AreEqual("m", tell.TargetId, "our back row");
            Assert.AreEqual("m", sim.LeechTargetId(1));
            BattleEvent land = Next(sim, session, Land).Value;
            Assert.AreEqual(Landed(ChiefKits.LeechName, ChiefKits.Full), land.Detail);
            Assert.AreEqual(tell.Tick + ChiefKits.LeechTicks - 1, land.Tick);
            List<BattleEvent> drinks = sim.Events.Where(e => e.Kind == BattleEventKind.Hit && e.Detail == ChiefKits.LeechDetail).ToList();
            CollectionAssert.AreEqual(Enumerable.Range(1, 6).Select(s => tell.Tick + s * 30L - 1).ToList(), drinks.Select(e => e.Tick).ToList(),
                "a drink each second");
            Assert.IsTrue(drinks.All(e => e.TargetId == "m"));
            Assert.AreEqual(drinks.Sum(e => e.Amount), land.Amount);
            Assert.IsTrue(sim.Events.Any(e => e.Kind == BattleEventKind.Heal && e.Detail == ChiefKits.LeechDetail && e.ActorSide == 1),
                "what they drink feeds his side");
            Assert.IsTrue(EatenAt(sim, land.Tick));

            // Plucked: our Medic's next treatment goes to whoever they drink from, and plucks them off; here on the tick they
            // land, since her turn comes then.
            var pluck = Fight("sv.leech.pluck", 83, Side(U("a", UnitClass.Warden), U("h", UnitClass.Medic), U("m", UnitClass.Marksman)), Theirs(),
                ChiefMove.Leeches);
            var ps = new SimSession(pluck);
            BattleEvent plucked = Next(pluck, ps, Land).Value;
            BattleEvent latched = pluck.Events.Last(e => e.Kind == BattleEventKind.Tell && e.Tick <= plucked.Tick);
            Assert.AreEqual(ChiefKits.LeechName, latched.Detail);
            Assert.AreEqual(Landed(ChiefKits.LeechName, ChiefKits.Healed), plucked.Detail);
            Assert.AreEqual(latched.TargetId, plucked.TargetId);
            Assert.IsTrue(pluck.Events.Any(e => e.Kind == BattleEventKind.Heal && e.ActorId == "h" && e.TargetId == latched.TargetId &&
                e.Tick == plucked.Tick) || pluck.FindById(latched.TargetId).HpPermille == 1000, "her treatment went to them");
            Assert.AreEqual(ChiefKits.HealGlory, pluck.Glory(0));
            Assert.AreEqual(0, pluck.LeechTicksLeft(1));

            // Shaken loose: Fall Back.
            var shake = Fight("sv.leech.shake", 83, Side(U("a", UnitClass.Warden), U("m", UnitClass.Marksman)), Theirs(), ChiefMove.Leeches);
            var ks = new SimSession(shake);
            Next(shake, ks, e => e.Kind == BattleEventKind.Tell);
            Assert.IsTrue(Order(ks, "FallBack"));
            BattleEvent shaken = Next(shake, ks, Land).Value;
            Assert.AreEqual(Landed(ChiefKits.LeechName, ChiefKits.Dodged), shaken.Detail);
            Assert.AreEqual(ChiefKits.DodgeGlory, shake.Glory(0));
        }

        // ------------------------------------------------------------ Open Veins

        [Test]
        public void OpenVeinsMakeYourWoundsBleedTwiceAsFastUnlessAHoldPressesThemOrYourMedicStitches()
        {
            List<UnitSpec> Theirs() => Side(Blade("x1"), Blade("x2"), Chief());
            int Plain(BattleSimulation s, string id) => Math.Max(1, s.FindById(id).MaxHp * 2 / 100);
            List<BattleEvent> Bleeds(BattleSimulation s, long after, long until) => s.Events.Where(e => e.Kind == BattleEventKind.Hit &&
                e.Detail == BleedRules.Detail && s.FindById(e.TargetId).Side == 0 && e.Tick > after && e.Tick <= until).ToList();

            // Eaten: a second of our bleeding takes twice as much while it runs.
            var sim = Fight("sv.veins", 84, Side(U("a", UnitClass.Warden), U("b", UnitClass.Skirmisher)), Theirs(), ChiefMove.OpenVeins);
            var session = new SimSession(sim);
            BattleEvent tell = Next(sim, session, e => e.Kind == BattleEventKind.Tell).Value;
            Assert.AreEqual(ChiefKits.VeinsName, tell.Detail);
            Assert.AreEqual(ChiefKits.VeinsEveryTicks, tell.Tick);
            Assert.AreEqual(ChiefKits.VeinsTicks, tell.Amount);
            BattleEvent land = Next(sim, session, Land).Value;
            Assert.AreEqual(Landed(ChiefKits.VeinsName, ChiefKits.Full), land.Detail);
            Assert.AreEqual(tell.Tick + ChiefKits.VeinsTicks - 1, land.Tick);
            List<BattleEvent> before = Bleeds(sim, 0, tell.Tick - 1), during = Bleeds(sim, tell.Tick - 1, land.Tick);
            Assert.IsNotEmpty(before);
            Assert.IsNotEmpty(during);
            Assert.IsTrue(before.All(e => e.Amount == Plain(sim, e.TargetId)), "a second of bleeding as it always was");
            Assert.IsTrue(during.All(e => e.Amount == 2 * Plain(sim, e.TargetId)), "twice as much");
            Assert.IsTrue(EatenAt(sim, land.Tick));

            // Pressed: a Hold through it brings each second back to as much.
            var press = Fight("sv.veins.press", 84, Side(U("a", UnitClass.Warden), U("b", UnitClass.Skirmisher)), Theirs(), ChiefMove.OpenVeins);
            var ps = new SimSession(press);
            BattleEvent opened = Next(press, ps, e => e.Kind == BattleEventKind.Tell).Value;
            Assert.IsTrue(Order(ps, "Hold"));
            BattleEvent braced = Next(press, ps, Land).Value;
            Assert.AreEqual(Landed(ChiefKits.VeinsName, ChiefKits.Braced), braced.Detail);
            List<BattleEvent> held = Bleeds(press, opened.Tick, braced.Tick);
            Assert.IsNotEmpty(held);
            Assert.IsTrue(held.All(e => e.Amount == Plain(press, e.TargetId)));
            Assert.IsTrue(GloryAt(press, ChiefAnswer.Brace, braced.Tick));

            // Stitched: our Medic closes a wound while it runs.
            var stitch = Fight("sv.veins.stitch", 84, Side(U("a", UnitClass.Warden), U("b", UnitClass.Skirmisher), U("h", UnitClass.Medic)),
                Theirs(), ChiefMove.OpenVeins);
            var ss = new SimSession(stitch);
            BattleEvent open = Next(stitch, ss, e => e.Kind == BattleEventKind.Tell).Value;
            BattleEvent healed = Next(stitch, ss, Land).Value;
            Assert.AreEqual(Landed(ChiefKits.VeinsName, ChiefKits.Healed), healed.Detail);
            Assert.IsTrue(stitch.Events.Any(e => e.Kind == BattleEventKind.Cash && e.Detail == ChiefKits.StitchDetail && e.Tick >= open.Tick &&
                e.Tick <= healed.Tick));
            Assert.AreEqual(ChiefKits.HealGlory, stitch.Glory(0));
        }

        // ------------------------------------------------------------ Transfusion

        [Test]
        public void HeChannelsBloodFromYourMostHurtIntoHisBandUnlessYouFocusHimOrYourWardenCoversThem()
        {
            List<UnitSpec> Theirs() => Side(U("x1", UnitClass.Skirmisher), U("x2", UnitClass.Skirmisher), Chief());

            // Eaten: three draws, one a second, from our most hurt.
            var sim = Fight("sv.draw", 85, Side(U("b", UnitClass.Skirmisher, 100, 300), U("m", UnitClass.Marksman)), Theirs(),
                ChiefMove.Transfusion);
            var session = new SimSession(sim);
            BattleEvent tell = Next(sim, session, e => e.Kind == BattleEventKind.Tell).Value;
            Assert.AreEqual(ChiefKits.TransfusionName, tell.Detail);
            Assert.AreEqual(ChiefKits.TransfusionEveryTicks, tell.Tick);
            Assert.AreEqual(ChiefKits.TransfusionTicks, tell.Amount);
            Assert.AreEqual(tell.TargetId, sim.TransfusionTargetId(1));
            BattleEvent land = Next(sim, session, Land).Value;
            Assert.AreEqual(Landed(ChiefKits.TransfusionName, ChiefKits.Full), land.Detail);
            Assert.AreEqual(tell.Tick + ChiefKits.TransfusionTicks - 1, land.Tick);
            List<BattleEvent> draws = sim.Events.Where(e => e.Kind == BattleEventKind.Hit && e.Detail == ChiefKits.TransfusionDetail).ToList();
            CollectionAssert.AreEqual(new[] { 29L, 59L, 89L }.Select(d => tell.Tick + d).ToList(), draws.Select(e => e.Tick).ToList());
            Assert.IsTrue(draws.All(e => e.TargetId == tell.TargetId));
            Assert.AreEqual(draws.Sum(e => e.Amount), land.Amount);
            Assert.IsTrue(sim.Events.Where(e => e.Kind == BattleEventKind.Heal && e.Detail == ChiefKits.TransfusionDetail)
                .All(e => sim.FindById(e.TargetId).Role == UnitRole.Fighter && e.ActorSide == 1), "it feeds his band, not him");
            Assert.IsFalse(sim.Events.Any(e => e.ActorId == "chief" && e.Kind == BattleEventKind.Heal && e.Detail == ClassSpells.TreatDetail &&
                e.Tick > tell.Tick && e.Tick < land.Tick), "he does nothing else while he channels");
            Assert.IsTrue(EatenAt(sim, land.Tick));

            // Broken: Focus Fire him while he channels.
            var cut = Fight("sv.draw.cut", 85, Side(U("b", UnitClass.Skirmisher, 100, 300), U("m", UnitClass.Marksman)), Theirs(),
                ChiefMove.Transfusion);
            var cs = new SimSession(cut);
            Next(cut, cs, e => e.Kind == BattleEventKind.Tell);
            Assert.IsTrue(Order(cs, "FocusFire", "chief"));
            BattleEvent broken = Next(cut, cs, Land).Value;
            Assert.AreEqual(Landed(ChiefKits.TransfusionName, ChiefKits.Broken), broken.Detail);
            Assert.IsTrue(GloryAt(cut, ChiefAnswer.Break, broken.Tick));

            // Blocked: a Warden of ours covers the one he draws from. With two of them, one covers the other if he draws from
            // a Warden, since a Warden never covers himself.
            var cover = Fight("sv.draw.cover", 85, Side(U("g1", UnitClass.Warden), U("g2", UnitClass.Warden), U("m", UnitClass.Marksman)),
                Theirs(), ChiefMove.Transfusion);
            var gs = new SimSession(cover);
            BattleEvent drawing = Next(cover, gs, e => e.Kind == BattleEventKind.Tell).Value;
            BattleEvent guarded = Next(cover, gs, Land).Value;
            Assert.AreEqual(Landed(ChiefKits.TransfusionName, ChiefKits.Guarded), guarded.Detail);
            Assert.IsTrue(cover.Events.Any(e => e.Kind == BattleEventKind.Guard && (e.ActorId == "g1" || e.ActorId == "g2") &&
                e.ActorId != drawing.TargetId && e.TargetId == drawing.TargetId && e.Tick == guarded.Tick));
            Assert.IsTrue(GloryAt(cover, ChiefAnswer.Guard, guarded.Tick));
        }

        // ------------------------------------------------------------ Blood Price, his tactical piece

        [Test]
        public void EverySixthOrderYouGiveFeedsHisRiteAndHisNextMoveComesAtOnce()
        {
            List<UnitSpec> Ours() => Side(U("a", UnitClass.Warden), U("m", UnitClass.Marksman));
            List<UnitSpec> Theirs() => Side(U("x1", UnitClass.Skirmisher), U("aco", UnitClass.Medic), Chief());
            const ChiefMove moves = ChiefMove.Leeches | ChiefMove.Basin | ChiefMove.OpenVeins;
            var sim = Fight("sv.price", 86, Ours(), Theirs(), moves, tactics: true);
            var session = new SimSession(sim);
            Assert.IsTrue(sim.PriceOn(1));
            Assert.AreEqual(0, sim.PriceDrops(1));

            for (int n = 1; n < ChiefKits.PriceOrders; n++)
            {
                while (sim.DirectivePoints(0) < 1) session.Tick();
                Assert.IsTrue(Order(session, "Hold"));
                Assert.AreEqual(n, sim.PriceDrops(1));
            }
            while (sim.DirectivePoints(0) < 1) session.Tick();
            Assert.Less(sim.CurrentTick, ChiefKits.LeechEveryTicks, "all six orders came before his first move was due");
            Assert.IsTrue(Order(session, "Hold"));
            BattleEvent price = sim.Events.Last(Land);
            Assert.AreEqual(Landed(ChiefKits.BloodPriceName, ChiefKits.Full), price.Detail);
            Assert.AreEqual(ChiefKits.PriceOrders, price.Amount);
            Assert.AreEqual(0, sim.PriceDrops(1));
            session.Tick();
            BattleEvent tell = sim.Events.Last(e => e.Kind == BattleEventKind.Tell);
            Assert.AreEqual(ChiefKits.LeechName, tell.Detail, "his soonest move");
            Assert.AreEqual(price.Tick, tell.Tick, "it came at once, on the tick the order went in");

            // Without the tactics there is no price.
            var plain = Fight("sv.price.off", 86, Ours(), Theirs(), moves);
            Assert.IsFalse(plain.PriceOn(1));
            var ps = new SimSession(plain);
            Assert.IsTrue(Order(ps, "Hold"));
            Assert.AreEqual(0, plain.PriceDrops(1));
        }

        // ------------------------------------------------------------ the Red Rite

        [Test]
        public void AtHalfHealthTheRedRiteMakesYourWoundsBleedTwiceAsLongAndHisBloodCallHealsHim()
        {
            // His band's Skirmisher does not guard him, so our heavy blades bring him to half.
            List<UnitSpec> Ours() => Side(U("a", UnitClass.Skirmisher, 300), U("b", UnitClass.Skirmisher, 300), U("c", UnitClass.Skirmisher, 300),
                U("d", UnitClass.Skirmisher, 300));
            var sim = Fight("sv.rite", 87, Ours(), Side(Blade("x1"), Blade("x2"), Chief()), ChiefMove.BloodCall);
            var session = new SimSession(sim);
            Action focus = () =>
            {
                if (sim.FocusTicksLeft(0) == 0 && sim.DirectivePoints(0) > 0) Order(session, "FocusFire", "chief");
            };
            BattleEvent phase = Next(sim, session, e => e.Kind == BattleEventKind.Phase, focus).Value;
            Assert.AreEqual(ChiefKits.PhaseName(ChiefKit.Stoneveil), phase.Detail);
            Assert.AreEqual("Red Rite", phase.Detail);
            Assert.IsTrue(sim.Events.Any(e => e.Kind == BattleEventKind.Line && e.Detail == ChiefKits.RedRiteLine && e.Tick == phase.Tick));
            Assert.IsTrue(sim.RedRite(1));

            // His Blood Call heals him for what it rips out. We stop focusing him, so he lives through it.
            BattleEvent call = Next(sim, session, e => Land(e) && e.Detail.StartsWith(Call, StringComparison.Ordinal)).Value;
            Assert.AreEqual(Landed(Call, ChiefKits.Full), call.Detail);
            int ripped = sim.Events.Where(e => e.Kind == BattleEventKind.Hit && e.Detail == ChiefSpells.BloodDetail && e.Tick == call.Tick)
                .Sum(e => e.Amount);
            BattleEvent heal = sim.Events.Single(e => e.Kind == BattleEventKind.Heal && e.Detail == ChiefSpells.BloodDetail);
            Assert.AreEqual("chief", heal.TargetId);
            Assert.AreEqual(call.Tick, heal.Tick);
            Assert.That(heal.Amount, Is.InRange(1, ripped));

            // A wound left on us now bleeds twice as long.
            bool longer = false;
            for (int i = 0; i < 300 && !sim.IsFinished && !longer; i++)
            {
                session.Tick();
                longer = sim.Units.Any(u => u.Side == 0 && u.BleedTicks > BleedRules.Ticks);
            }
            Assert.IsTrue(longer, "a wound bled longer than any wound before the Red Rite");
        }

        // ------------------------------------------------------------ Clean Hands, his hidden chain

        [Test]
        public void ThreeOfHisMovesLandingWithNobodyOfYoursBleedingBreakHisRite()
        {
            // We keep falling back, so his blade never lands and nobody of ours bleeds while his moves land.
            var sim = Fight("sv.clean", 88, Side(U("a", UnitClass.Warden), U("b", UnitClass.Skirmisher), U("m", UnitClass.Marksman)),
                Side(Blade("x1"), U("aco", UnitClass.Medic), Chief()), ChiefMove.Leeches | ChiefMove.Basin | ChiefMove.OpenVeins);
            var session = new SimSession(sim);
            BattleEvent combo = Next(sim, session, e => e.Kind == BattleEventKind.Combo && e.Detail == ChiefKits.CleanHands, () =>
            {
                if (sim.FallBackTicksLeft(0) == 0) Order(session, "FallBack");
            }).Value;
            Assert.AreEqual(ChiefKits.CleanHands, ChiefKits.ChainName(ChiefKit.Stoneveil));
            Assert.AreEqual(0, combo.ActorSide);
            Assert.AreEqual("chief", combo.TargetId);
            Assert.AreEqual(ChiefKits.CleanHandsStunTicks, combo.Amount);
            List<BattleEvent> landed = sim.Events.Where(Land).ToList();
            Assert.AreEqual(ChiefKits.CleanHandsLands, landed.Count, "three of his moves landed");
            Assert.AreEqual(combo.Tick, landed.Last().Tick);
            Assert.Greater(sim.FindById("chief").StunTicks, 0, "he is stunned");
            Assert.IsTrue(sim.RiteBroken(1));
            Assert.IsFalse(sim.Units.Any(u => u.Side == 0 && u.BleedTicks > 0));

            // From now on his band's blades leave no wounds.
            long from = sim.CurrentTick;
            bool bled = false;
            for (int i = 0; i < 300 && !sim.IsFinished; i++)
            {
                session.Tick();
                bled |= sim.Units.Any(u => u.Side == 0 && u.BleedTicks > 0);
            }
            Assert.IsTrue(sim.Events.Any(e => e.Kind == BattleEventKind.Hit && e.ActorId == "x1" && e.Tick > from), "his blade landed");
            Assert.IsFalse(bled, "and left no wound");
        }

        // ------------------------------------------------------------ the campaign

        [Test]
        public void TheStoneveilChiefFightsWithHisKitAndHisFightSavesAndPlaysBackExactly()
        {
            var t = new CampaignTuning { PathFights = 0 };
            // About one fight in twenty lasts that long, so it looks through two hundred (rules 42 moved the one forty had).
            for (ulong seed = 4600; seed < 4800; seed++)
            {
                Campaign c = Campaign.New(seed, t);
                Assert.IsTrue(CampaignPilot.PlayTutorial(c, PilotStyle.Reference, seed, 10), "the tutorial was never won");
                c.ChooseTribe(CampaignTribes.StoneveilId);
                Assert.AreEqual(EncounterKind.Chief, c.Offers[0].Kind);
                CampaignFight fight = c.StartEncounter(0);
                Assert.AreEqual(Campaign.RulesVersion, fight.RulesVersion);
                // His kit plays from rules 7 on (8 added the parry at the center, 24 Oct 2026).
                Assert.That(Campaign.RulesVersion, Is.GreaterThanOrEqualTo(7));
                Assert.IsTrue(fight.Setup.Fight.ChiefTactics);
                Assert.AreEqual(ChiefKit.Stoneveil, fight.Sim.Kit(1));
                ChiefMove moves = fight.Sim.KitMoves(1);
                Assert.AreEqual(c.ChiefMovesThisRun(CampaignTribes.Stoneveil), moves);
                Assert.AreEqual(ChiefKits.Takes, Count(moves), "three of his five");
                Assert.AreEqual(moves, moves & ChiefKits.Pool(ChiefKit.Stoneveil));
                Assert.AreEqual((moves & ChiefMove.BloodCall) != 0 ? ChiefKits.BloodCallEveryTicks : 0, fight.Setup.Rules(1).AutoEclipseTicks);
                Assert.IsTrue(fight.Sim.PriceOn(1));
                Assert.AreEqual(string.Empty, fight.ChiefLine(ChiefKits.KneelLine), "his words are Jacob's to write");

                // Halfway the save holds the fight exactly.
                var pilot = new CampaignPilot(PilotStyle.Reference, seed);
                while (!fight.Over && fight.Sim.CurrentTick < ChiefKits.BloodCallEveryTicks + 30)
                {
                    pilot.Step(fight);
                    fight.Tick();
                }
                if (fight.Over) continue;
                Campaign half = CampaignSave.Read(CampaignSave.Write(c), t);
                Assert.IsEmpty(half.LoadNote);
                Assert.AreEqual(fight.Session.Checksum, half.Active.Session.Checksum);

                // Played out, you have met him, and the recorded fight plays back exactly.
                CampaignPilot.PlayFight(c, fight, pilot);
                Assert.IsTrue(c.MetChief(CampaignTribes.StoneveilId), "you have met him now, win or lose");
                Assert.IsTrue(c.Archive.TryReplay(fight.Setup.FightId, out _, out string failure), failure);
                return;
            }
            Assert.Fail("no Stoneveil chief fight lasted past his first Blood Call in two hundred campaigns");
        }
    }
}
