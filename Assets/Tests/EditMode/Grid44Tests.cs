using System.Collections.Generic;
using System.Linq;
using KindredSiege.Sim;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using NUnit.Framework;

namespace KindredSiege.Sim.Tests
{
    /// The grid, campaign rules 44 (BattleSimulation.Grid.cs): each band on its own 3 by 3 panels, the five built class
    /// shapes (DESIGN_LOCKS line 1362), steps, lane shots stopping at the first body, the trap on a panel, the Medic's touch,
    /// Fall Back and Focus Fire on the grid, and the version boundary.
    public class Grid44Tests
    {
        static UnitSpec U(string id, UnitClass c, Row row = Row.Front, int health = 30000, UnitRole role = UnitRole.Fighter, int power = 100)
        {
            return new UnitSpec(id, c, row, role, power, health);
        }

        static BattleSimulation Fight(FightRules rules, UnitSpec[] ours, UnitSpec[] theirs, ulong seed = 7)
        {
            return new BattleSimulation(new BattleSetup("grid44", seed, ours, theirs, new TacticsProfile(), new TacticsProfile(),
                BattleSetup.DefaultTickLimit, 20, NerveRules.Off, 10, 100, SideRules.None, SideRules.None, rules));
        }

        static List<BattleEvent> Play(BattleSimulation s, int ticks)
        {
            var ses = new SimSession(s);
            for (int i = 0; i < ticks && !s.IsFinished; i++) ses.Tick();
            return s.Events.ToList();
        }

        static FightRules Old => FightRules.Campaign.WithLayers().WithTable().WithLeaders();
        static FightRules Grid => Old.WithGrid();

        static BattleUnit Unit(BattleSimulation s, string id) => s.Units.First(u => u.Id.EndsWith(id));
        static int Panel(BattleUnit u) => GridRules.Panel(u.GridCol, u.GridLane);
        static bool Lands(BattleEvent e, BattleUnit actor) => e.Kind == BattleEventKind.Hit && e.ActorId == actor.Id;

        static UnitSpec[] Squad(string p = "") => new[]
        {
            U(p + "w", UnitClass.Warden), U(p + "sk", UnitClass.Skirmisher), U(p + "mk", UnitClass.Marksman, Row.Back),
            U(p + "tr", UnitClass.Trapper, Row.Back), U(p + "md", UnitClass.Medic, Row.Back)
        };

        [Test]
        public void EachBandStandsOnItsOwnPanels()
        {
            UnitSpec[] theirs = Squad("e").Concat(new[] { U("boss", UnitClass.Warden, Row.Back, 30000, UnitRole.Chief, 150) }).ToArray();
            BattleSimulation s = Fight(Grid, Squad(), theirs);
            Assert.IsTrue(s.GridRulesOn);
            for (int side = 0; side < 2; side++)
            {
                List<BattleUnit> band = s.Units.Where(u => u.Side == side).ToList();
                Assert.IsTrue(band.All(u => u.GridCol >= 0 && u.GridCol < 3 && u.GridLane >= 0 && u.GridLane < 3));
                Assert.AreEqual(band.Count, band.Select(Panel).Distinct().Count(), "one fighter a panel");
            }
            BattleUnit ourWarden = s.Units.First(u => u.Side == 0 && u.Class == UnitClass.Warden);
            Assert.AreEqual(GridRules.Panel(GridRules.Middle, GridRules.Middle), Panel(ourWarden), "the Warden in the centre");
            Assert.AreEqual(GridRules.Panel(GridRules.Front, GridRules.Middle), Panel(s.Units.First(u => u.Side == 0 && u.Class == UnitClass.Skirmisher)));
            Assert.AreEqual(GridRules.Back, s.Units.First(u => u.Side == 0 && u.Class == UnitClass.Marksman).GridCol);
            BattleUnit boss = s.Units.First(u => u.Side == 1 && u.Role == UnitRole.Chief);
            Assert.AreEqual(GridRules.Panel(GridRules.Back, GridRules.Middle), Panel(boss), "their leader in the back middle");
            // Before rules 44 nobody has a panel, and the snapshot never mentions one.
            BattleSimulation old = Fight(Old, Squad(), theirs);
            Assert.IsFalse(old.GridRulesOn);
            Assert.IsTrue(old.Units.All(u => u.GridCol == -1 && u.GridLane == -1));
            Assert.IsFalse(old.OrderedStateSnapshot.Contains("panel0"));
            Assert.IsTrue(s.OrderedStateSnapshot.Contains("panel0"));
        }

        [Test]
        public void ALaneShotStopsAtTheFirstEnemyInItsLane()
        {
            // Ours: a Marksman in the back of the top lane. Theirs: s1 front middle, s2 front top, m back top.
            UnitSpec[] ours = { U("mk", UnitClass.Marksman, Row.Back) };
            UnitSpec[] theirs = { U("s1", UnitClass.Skirmisher), U("s2", UnitClass.Skirmisher), U("m", UnitClass.Marksman, Row.Back, 30000) };
            BattleSimulation s = Fight(Grid, ours, theirs);
            BattleUnit mk = Unit(s, "mk"), s2 = Unit(s, "s2"), m = Unit(s, "m");
            Assert.AreEqual(GridRules.Panel(GridRules.Back, GridRules.Top), Panel(mk));
            Assert.AreEqual(GridRules.Panel(GridRules.Front, GridRules.Top), Panel(s2));
            Assert.AreEqual(GridRules.Panel(GridRules.Back, GridRules.Top), Panel(m));
            List<BattleEvent> ev = Play(s, 1500);
            List<BattleEvent> ours1 = ev.Where(e => Lands(e, mk)).ToList();
            Assert.IsNotEmpty(ours1);
            long s2Fell = ev.Where(e => e.Kind == BattleEventKind.Kill && e.TargetId == s2.Id).Select(e => e.Tick).DefaultIfEmpty(long.MaxValue).First();
            Assert.IsTrue(ours1.Where(e => e.Tick < s2Fell).All(e => e.TargetId == s2.Id), "the shot stops at the front of the lane");
            Assert.IsFalse(ours1.Any(e => e.TargetId == m.Id && e.Tick < s2Fell), "never past him to the back");
        }

        [Test]
        public void WithNothingInReachAFighterStepsOnePanelThenStrikes()
        {
            // Our Skirmisher stands in the middle lane; their only fighter is in the top lane.
            UnitSpec[] ours = { U("sk", UnitClass.Skirmisher) };
            UnitSpec[] theirs = { U("m", UnitClass.Marksman, Row.Back) };
            BattleSimulation s = Fight(Grid, ours, theirs);
            BattleUnit sk = Unit(s, "sk"), m = Unit(s, "m");
            Assert.AreEqual(GridRules.Middle, sk.GridLane);
            Assert.AreEqual(GridRules.Top, m.GridLane);
            List<BattleEvent> ev = Play(s, 900);
            BattleEvent step = ev.First(e => e.Kind == BattleEventKind.Step && e.ActorId == sk.Id);
            Assert.AreEqual(GridRules.StepDetail, step.Detail);
            Assert.AreEqual(GridRules.Panel(GridRules.Front, GridRules.Top), step.Amount, "one panel up, into his lane");
            Assert.IsFalse(ev.Any(e => Lands(e, sk) && e.Tick < step.Tick), "nothing landed before the step");
            Assert.IsTrue(ev.Any(e => Lands(e, sk) && e.TargetId == m.Id && e.Tick > step.Tick), "the lane dash reaches the back row");
            Assert.IsTrue(ev.Where(e => e.Kind == BattleEventKind.Step).All(e => e.Amount >= 0 && e.Amount < 9));
        }

        [Test]
        public void TheWardenGuardsTheCrossAndHitsNothing()
        {
            // Ours: the Warden in the centre, the Skirmisher in front of him, a Marksman off his cross in the back top.
            UnitSpec[] ours = { U("w", UnitClass.Warden), U("sk", UnitClass.Skirmisher), U("mk", UnitClass.Marksman, Row.Back) };
            UnitSpec[] theirs = { U("es", UnitClass.Skirmisher), U("em", UnitClass.Marksman, Row.Back), U("em2", UnitClass.Marksman, Row.Back) };
            BattleSimulation s = Fight(Grid, ours, theirs);
            BattleUnit w = Unit(s, "w"), sk = Unit(s, "sk"), mk = Unit(s, "mk");
            List<BattleEvent> ev = Play(s, 1800);
            long otherFell = ev.Where(e => e.Kind == BattleEventKind.Kill && e.ActorSide == 1).Select(e => e.Tick).DefaultIfEmpty(long.MaxValue).Max();
            List<BattleEvent> cross = ev.Where(e => e.Kind == BattleEventKind.Defence && e.Detail == GridRules.CrossDetail && e.ActorId == w.Id).ToList();
            Assert.IsNotEmpty(cross, "he steps in front of blows on his cross");
            Assert.IsTrue(cross.All(e => e.TargetId == sk.Id), "only the ally touching his panel");
            foreach (BattleEvent c in cross)
                Assert.IsTrue(ev.Any(e => e.Tick == c.Tick && e.Kind == BattleEventKind.Hit && e.TargetId == w.Id), "it lands on him, tick " + c.Tick);
            Assert.IsFalse(ev.Any(e => Lands(e, w) && sk.Alive && mk.Alive), "he hits nothing while they stand");
            Assert.IsFalse(ev.Any(e => e.Kind == BattleEventKind.Step && e.ActorId == w.Id), "he never leaves the centre");
            Assert.IsFalse(ev.Any(e => e.Detail == LeaderRules.MiddleDetail), "the cross replaces rules 43's middle");
        }

        [Test]
        public void TheTrapSitsOnTheirFrontPanelInHerLaneAndSpringsOnWhoeverStandsThere()
        {
            // Our Trapper stands in the back of the top lane. Theirs: e1 front middle, e2 front top.
            UnitSpec[] ours = { U("tr", UnitClass.Trapper, Row.Back) };
            UnitSpec[] theirs = { U("e1", UnitClass.Marksman, Row.Front), U("e2", UnitClass.Marksman, Row.Front) };
            BattleSimulation s = Fight(Grid, ours, theirs);
            BattleUnit tr = Unit(s, "tr"), e2 = Unit(s, "e2");
            Assert.AreEqual(GridRules.Top, tr.GridLane);
            Assert.AreEqual(GridRules.Panel(GridRules.Front, GridRules.Top), Panel(e2));
            List<BattleEvent> ev = Play(s, 900);
            BattleEvent armed = ev.First(e => e.Kind == BattleEventKind.TrapArmed && e.ActorSide == 0);
            BattleEvent sprung = ev.First(e => e.Kind == BattleEventKind.TrapSprung && e.ActorSide == 0);
            Assert.AreEqual(e2.Id, sprung.TargetId, "on whoever stands on their front panel of her lane");
            Assert.AreEqual(armed.Tick, sprung.Tick, "he stood there already, so it springs at once, with no blow");
            Assert.IsTrue(ev.Any(e => e.Kind == BattleEventKind.Mark && e.Detail == ClassSpells.SnareMark && e.TargetId == e2.Id && e.Tick == sprung.Tick),
                "the barbed snare holds him");

            // With nobody on that panel it never springs.
            BattleSimulation empty = Fight(Grid, ours, new[] { U("b", UnitClass.Marksman, Row.Back) });
            List<BattleEvent> none = Play(empty, 600);
            Assert.IsTrue(none.Any(e => e.Kind == BattleEventKind.TrapArmed && e.ActorSide == 0));
            Assert.AreEqual(GridRules.Top, empty.GridTrapLane(0), "it waits in her lane");
            Assert.IsFalse(none.Any(e => e.Kind == BattleEventKind.TrapSprung && e.ActorSide == 0));
        }

        [Test]
        public void TheMedicTreatsOnlyAnAllyTouchingHer()
        {
            // Ours: near and t in the back top and bottom, the Medic between them, far in the front middle.
            UnitSpec[] ours = { U("far", UnitClass.Skirmisher, Row.Front, 3000), U("near", UnitClass.Marksman, Row.Back, 3000),
                                U("t", UnitClass.Trapper, Row.Back, 3000), U("md", UnitClass.Medic, Row.Back, 3000) };
            UnitSpec[] theirs = { U("a", UnitClass.Marksman, Row.Back), U("b", UnitClass.Marksman, Row.Back), U("c", UnitClass.Skirmisher) };
            BattleSimulation s = Fight(Grid, ours, theirs);
            BattleUnit md = Unit(s, "md");
            Assert.AreEqual(GridRules.Panel(GridRules.Back, GridRules.Middle), Panel(md));
            var ses = new SimSession(s);
            int seen = 0, treated = 0;
            for (int i = 0; i < 1800 && !s.IsFinished; i++)
            {
                var before = s.Units.ToDictionary(u => u.Id, u => Panel(u));
                ses.Tick();
                for (; seen < s.Events.Count; seen++)
                {
                    BattleEvent e = s.Events[seen];
                    if (e.Kind != BattleEventKind.Heal || e.ActorId != md.Id) continue;
                    treated++;
                    BattleUnit p = s.FindById(e.TargetId);
                    bool touchNow = p == md || System.Math.Abs(p.GridCol - md.GridCol) + System.Math.Abs(p.GridLane - md.GridLane) == 1;
                    int a = before[md.Id], b = before[p.Id];
                    bool touchBefore = p == md || System.Math.Abs(a / 3 - b / 3) + System.Math.Abs(a % 3 - b % 3) == 1;
                    Assert.IsTrue(touchNow || touchBefore, "tick " + e.Tick + ": she treated " + p.Id + " from afar");
                }
            }
            Assert.Greater(treated, 0, "she treats someone");
        }

        [Test]
        public void FallBackStepsTheBandBackARow()
        {
            UnitSpec[] ours = { U("w", UnitClass.Warden), U("sk", UnitClass.Skirmisher), U("mk", UnitClass.Marksman, Row.Back) };
            UnitSpec[] theirs = { U("e", UnitClass.Marksman, Row.Back) };
            BattleSimulation s = Fight(Grid, ours, theirs);
            BattleUnit w = Unit(s, "w"), sk = Unit(s, "sk"), mk = Unit(s, "mk");
            var ses = new SimSession(s);
            ses.Tick();
            int skBefore = sk.GridCol, mkBefore = Panel(mk);
            Assert.IsTrue(ses.TryIssue(Directives.CommandKind, "0", string.Empty, 0, 0, "FallBack", out string why), why);
            Assert.AreEqual(GridRules.Panel(GridRules.Back, GridRules.Middle), Panel(w), "the centre steps back first");
            Assert.AreEqual(skBefore - 1, sk.GridCol, "then the front follows into the panel he left");
            Assert.AreEqual(mkBefore, Panel(mk), "the back row has nowhere to go");
            Assert.AreEqual(2, s.Events.Count(e => e.Kind == BattleEventKind.Step && e.Detail == GridRules.FallBackDetail));
        }

        [Test]
        public void FocusFireSendsAFighterWhoCantReachTheMarkTowardIt()
        {
            // Our Marksman shoots the top lane; the focused enemy stands in the middle lane.
            UnitSpec[] ours = { U("mk", UnitClass.Marksman, Row.Back) };
            UnitSpec[] theirs = { U("mid", UnitClass.Warden, Row.Back), U("top", UnitClass.Marksman, Row.Back) };
            BattleSimulation s = Fight(Grid, ours, theirs);
            BattleUnit mk = Unit(s, "mk"), mid = Unit(s, "mid");
            Assert.AreEqual(GridRules.Top, mk.GridLane);
            Assert.AreNotEqual(GridRules.Top, mid.GridLane);
            var ses = new SimSession(s);
            ses.Tick();
            Assert.IsTrue(ses.TryIssue(Directives.CommandKind, "0", mid.Id, 0, 0, "FocusFire", out string why), why);
            for (int i = 0; i < 400 && !s.IsFinished; i++) ses.Tick();
            List<BattleEvent> ev = s.Events.ToList();
            Assert.IsTrue(ev.Any(e => e.Kind == BattleEventKind.Step && e.ActorId == mk.Id), "he steps toward the mark");
            Assert.IsTrue(ev.Any(e => Lands(e, mk) && e.TargetId == mid.Id), "and hits it");
        }

        [Test]
        public void AnAllyMakesRoomSoABoxedInFighterReachesHisLane()
        {
            // Ours: the Warden holds the centre, the Skirmisher front middle, a Marksman back top, a Medic back bottom with lots
            // of health. Their only fighter stands in the bottom lane: the Skirmisher's way there runs over panels the others hold.
            UnitSpec[] ours = { U("w", UnitClass.Warden), U("sk", UnitClass.Skirmisher), U("mk", UnitClass.Marksman, Row.Back),
                                U("md", UnitClass.Medic, Row.Back), U("t2", UnitClass.Trapper, Row.Back) };
            UnitSpec[] theirs = { U("e", UnitClass.Marksman, Row.Front) };
            BattleSimulation s = Fight(Grid, ours, theirs);
            BattleUnit sk = Unit(s, "sk"), e = Unit(s, "e");
            List<BattleEvent> ev = Play(s, 900);
            Assert.IsTrue(ev.Any(x => Lands(x, sk) && x.TargetId == e.Id), "she gets to his lane and strikes");
            foreach (BattleEvent x in ev.Where(x => x.Kind == BattleEventKind.Step && (x.Detail == GridRules.SwapDetail || x.Detail == GridRules.RoomDetail)))
                Assert.IsTrue(ev.Any(y => y.Tick == x.Tick && y.Kind == BattleEventKind.Step && y.Detail == GridRules.StepDetail),
                    "a fighter only makes room for an ally's step, tick " + x.Tick);
        }

        [Test]
        public void AWholeRunHasNoBackAndForthAndNoFightStalls()
        {
            const ulong seed = 5100;
            var c = Campaign.New(seed, new CampaignTuning { EnemyScaleTier1 = 40 });
            Assert.IsTrue(CampaignPilot.PlayTutorial(c, PilotStyle.Reference, seed, 10));
            c.StartRun();
            var picker = new CampaignPilot(PilotStyle.Reference, seed);
            int fights = 0;
            for (int n = 0; c.Stage == CampaignStage.InRun && n < 40; n++)
            {
                CampaignFight fight = c.StartEncounter(picker.PickOffer(c));
                CampaignPilot.PlayFight(c, fight, new CampaignPilot(PilotStyle.Reference, seed * 31UL + (ulong)n));
                BattleSimulation s = fight.Sim;
                if (!s.GridRulesOn) continue;
                fights++;
                // Nobody steps back onto the panel he left less than two seconds before, unless an order (Fall Back) moved him.
                foreach (IGrouping<string, BattleEvent> moves in s.Events.Where(x => x.Kind == BattleEventKind.Step).GroupBy(x => x.ActorId))
                {
                    List<BattleEvent> m = moves.ToList();
                    for (int i = 2; i < m.Count; i++)
                    {
                        if (m[i].Detail == GridRules.FallBackDetail || m[i - 1].Detail == GridRules.FallBackDetail) continue;
                        Assert.IsFalse(m[i].Amount == m[i - 2].Amount && m[i].Tick - m[i - 2].Tick < 60,
                            fight.Setup.FightId + ": " + moves.Key + " went back and forth at tick " + m[i].Tick);
                    }
                }
                // A fight on the clock (Hold the Line) may run out; any other ends before it.
                if (fight.Setup.FightId.Contains("holdtheline")) continue;
                Assert.AreNotEqual(FightEnd.TimeUp, s.Ending.How, fight.Setup.FightId + " stalled to the time limit");
            }
            Assert.Greater(fights, 3);
        }

        [Test]
        public void TheSameFightPlaysTheSameTwice()
        {
            UnitSpec[] theirs = Squad("e");
            BattleSimulation a = Fight(Grid, Squad(), theirs, 19), b = Fight(Grid, Squad(), theirs, 19);
            Play(a, 2400);
            Play(b, 2400);
            Assert.AreEqual(StateChecksum.Compute(a.OrderedStateSnapshot), StateChecksum.Compute(b.OrderedStateSnapshot));
            Assert.AreEqual(a.EventHash, b.EventHash);
        }

        [Test]
        public void ANewCampaignFightPlaysRules44()
        {
            Assert.IsFalse(Campaign.New(3, new CampaignTuning { RulesCap = 43 }).StartTutorial().Setup.Fight.Grid);
            Assert.IsTrue(Campaign.New(3, new CampaignTuning()).StartTutorial().Setup.Fight.Grid);
            Assert.AreEqual(44, Campaign.GridRules);
            Assert.GreaterOrEqual(Campaign.RulesVersion, 44);
        }

        [Test]
        public void AllBuildersPreserveTheGridSwitch()
        {
            foreach (var input in new[] { FightRules.Off.WithGrid(), FightRules.Campaign.WithTable().WithChampionFight().WithGrid() })
            foreach (var m in typeof(FightRules).GetMethods().Where(m => m.Name.StartsWith("With") && m.ReturnType == typeof(FightRules)))
            {
                var r = (FightRules)m.Invoke(input, m.GetParameters().Length == 0 ? new object[0] : new object[] { 4 });
                Assert.IsTrue(r.Grid, m.Name);
            }
            Assert.IsFalse(FightRules.Campaign.WithLeaders().Grid);
        }

        [Test]
        public void SavesAndReplaysKeepTheirVersionAcrossTheBoundary()
        {
            foreach (int version in new[] { 43, 44 })
            {
                var c = Campaign.New(11, new CampaignTuning { RulesCap = version });
                var f = c.StartTutorial();
                for (int i = 0; i < 60; i++) f.Tick();
                var loaded = CampaignSave.Read(CampaignSave.Write(c));
                Assert.IsEmpty(loaded.LoadNote);
                Assert.AreEqual(f.Session.Checksum, loaded.Active.Session.Checksum);
                Assert.AreEqual(version == 44, loaded.Active.Setup.Fight.Grid);
                CampaignPilot.PlayFight(c, f, new CampaignPilot(PilotStyle.Reference, 11));
                loaded = CampaignSave.Read(CampaignSave.Write(c));
                Assert.IsEmpty(loaded.LoadNote);
                Assert.IsTrue(loaded.Archive.TryReplay(f.Setup.FightId, out BattleSimulation replay, out string why), why);
                Assert.AreEqual(version == 44, replay.Setup.Fight.Grid);
                Assert.AreEqual(f.Session.Checksum, StateChecksum.Compute(replay.OrderedStateSnapshot));
            }
        }
    }
}
