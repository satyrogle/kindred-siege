using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using KindredSiege.Sim;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using NUnit.Framework;

namespace KindredSiege.Sim.Tests
{
    // Audit fixtures seed an exact transient state, then assert damage, actions, identities and replay outcomes.
    public class Contract36Tests
    {
        static readonly FightRules Old = FightRules.Campaign.WithRoadKit().WithClearRoad().WithSquadScreen()
            .WithLayers().WithMarks().WithPayoffs().WithDanger().WithMeter().WithFloor().WithWhisper().WithShadeForms()
            .WithGlutton().WithDuos().WithSized().WithFlavours().WithVows().WithDuoMoves().WithTight().WithCorrections();
        static readonly FightRules Current = Old.WithContracts();
        const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        static object Call(object o, string name, params object[] args) => o.GetType().GetMethods(Flags).First(m =>
            m.Name == name && m.GetParameters().Length == args.Length && m.GetParameters().Select((p, i) =>
                args[i] == null || p.ParameterType.IsInstanceOfType(args[i])).All(x => x)).Invoke(o, args);
        static void Set(object o, string name, object value) => o.GetType().GetProperty(name, Flags).SetValue(o, value);
        static object State(BattleSimulation s, int side) => ((Array)typeof(BattleSimulation).GetField("_sides", Flags).GetValue(s)).GetValue(side);
        static void Field(object o, string name, object value) => o.GetType().GetField(name, Flags).SetValue(o, value);
        static UnitSpec Unit(string id, UnitClass c = UnitClass.Marksman, int rank = 1, ClassBend bend = ClassBend.None, int comboRank = 1, int armor = 0) =>
            new UnitSpec(id, c, Row.Front, UnitRole.Fighter, 100, 30000, 0, 0, armor).WithRank(rank).WithBend(bend).WithComboRank(comboRank);
        static PalKit Kit(ShadeRoute route = ShadeRoute.None, int form = 1, int level = 1, params GluttonTalent[] talents)
        {
            var routes = new List<KeyValuePair<ShadeRoute, int>>();
            if (route != ShadeRoute.None) routes.Add(new KeyValuePair<ShadeRoute, int>(route, form));
            var levels = new int[GluttonRules.TalentCount + 1];
            foreach (var t in talents) levels[(int)t] = level;
            return new PalKit(routes, 0).WithTalents(levels);
        }
        static BattleSetup Setup(bool current = true, PalKit kit = null, int rank = 1, ClassBend bend = ClassBend.None,
            int comboRank = 1, UnitSpec? target = null, bool nerve = false) => new BattleSetup("contracts", 11,
                new[] { Unit("a", UnitClass.Skirmisher, rank, bend, comboRank), Unit("b", UnitClass.Warden) },
                new[] { target ?? Unit("x"), Unit("y"), Unit("z") }, new TacticsProfile(), new TacticsProfile(),
                BattleSetup.DefaultTickLimit, 20, nerve ? NerveRules.On() : NerveRules.Off, 10, 100,
                new SideRules(0, 0, false).WithPals(kit ?? PalKit.None), new SideRules(0, 0, false), current ? Current : Old);
        static BattleSimulation Fight(bool current = true, PalKit kit = null, int rank = 1, ClassBend bend = ClassBend.None,
            int comboRank = 1) => new BattleSimulation(Setup(current, kit, rank, bend, comboRank));
        static int EchoRelease(bool current, int rank, int form, bool dead = false)
        {
            var s = Fight(current, Kit(ShadeRoute.Echo, form), rank); var st = State(s, 0);
            Field(st, "EchoRecording", true); Field(st, "UnleashTicks", 120);
            var a = s.FindById("a"); var x = s.FindById("x");
            Call(s, "ApplyDamage", a, 0, x, 100, "audit", false);
            Assert.AreEqual(rank == 3 ? 245 : 35, a.EchoStored);
            if (dead) Set(a, "Alive", false);
            int hp = x.Hp; Call(s, "ReleaseEchoes", 0, st, false);
            Assert.AreEqual(0, a.EchoStored);
            Call(s, "ReleaseEchoes", 0, st, false);
            return hp - x.Hp;
        }
        [Test] public void EchoDoesNotApplyRankTwiceButRetainsItsOwnFormGrowth()
        {
            Assert.AreEqual(1837, EchoRelease(false, 3, 1), "rules35 reproduction");
            Assert.AreEqual(52, EchoRelease(true, 1, 1));
            Assert.AreEqual(367, EchoRelease(true, 3, 1));
            Assert.AreEqual(735, EchoRelease(true, 3, 2));
            Assert.AreEqual(1225, EchoRelease(true, 3, 3));
        }
        [Test] public void EchoStillReleasesOnceAfterItsFighterFalls() => Assert.AreEqual(367, EchoRelease(true, 3, 1, true));
        [Test] public void PinnedFighterCannotReactButCanReactAfterRelease()
        {
            foreach (bool current in new[] { false, true })
            {
                var s = Fight(current, bend: ClassBend.Collapse); var a = s.FindById("a"); var x = s.FindById("x");
                Call(s, "Pin", a, 500); Call(s, "Pin", x, 500); Set(a, "SpellTicks", 0); Set(x, "DefenceTicks", 500);
                Call(s, "Open", s.FindById("b"), x); int start = s.Events.Count; var ses = new SimSession(s); ses.Tick();
                Assert.Greater(a.PinTicks, 0);
                Assert.AreEqual(!current, s.Events.Skip(start).Any(e => e.Kind == BattleEventKind.Hit && e.ActorId == "a"));
                if (!current) continue;
                Call(a.Statuses, "Clear", StatusKind.Pin); Call(s, "Open", s.FindById("b"), x); start = s.Events.Count; ses.Tick();
                Assert.IsTrue(s.Events.Skip(start).Any(e => e.Kind == BattleEventKind.Hit && e.ActorId == "a"));
            }
        }
        static BattleSimulation Bear(bool current, PalKit kit = null, bool heavy = false)
        {
            var parts = new List<UnitSpec>();
            foreach (var p in BeastKits.Parts(BeastKit.CageBear)) parts.Add(new UnitSpec("bear." + BeastKits.Name(p), UnitClass.Warden, BeastKits.RowOf(p), UnitRole.Fighter, 100, 30000));
            var rules = FightRules.CampaignBeast.WithBeastScenes().WithBeastRaid().WithLayers().WithMarks().WithPayoffs()
                .WithDanger().WithMeter().WithFloor().WithWhisper().WithShadeForms().WithGlutton().WithDuos().WithSized()
                .WithFlavours().WithVows().WithDuoMoves().WithTight().WithCorrections();
            if (current) rules = rules.WithContracts();
            return new BattleSimulation(new BattleSetup("frozen", 11, new[] { Unit("m", rank: kit == null ? 1 : 2), Unit("w", UnitClass.Warden) }, parts,
                new TacticsProfile(), new TacticsProfile(), BattleSetup.DefaultTickLimit, 20, NerveRules.Off, 10, 100,
                new SideRules(0, 0, false, heavyEveryTicks: heavy ? 300 : 0).WithPals(kit ?? PalKit.None), new SideRules(0, 0, false, beast: BeastKit.CageBear), rules));
        }
        static void Thaw(BattleSimulation s) => Array.Clear((Array)typeof(BattleSimulation).GetField("_frozenUntil", Flags).GetValue(s), 0, s.Units.Count);
        [Test] public void BearFreezePausesAnAimAndAllowsItToFinishAfterThawing()
        {
            foreach (bool current in new[] { false, true })
            {
                var s = Bear(current); var m = s.FindById("m"); var target = s.Units.First(u => u.Side == 1);
                Set(m, "AimTicks", 1); Set(m, "AimTargetId", target.Id); Call(s, "Frost");
                int start = s.Events.Count; var ses = new SimSession(s); ses.Tick();
                Assert.Greater(s.FrozenTicks(m), 0);
                Assert.AreEqual(!current, s.Events.Skip(start).Any(e => e.Kind == BattleEventKind.Hit && e.ActorId == m.Id));
                if (!current) continue;
                Assert.AreEqual(1, m.AimTicks); Thaw(s); start = s.Events.Count; ses.Tick();
                Assert.IsTrue(s.Events.Skip(start).Any(e => e.Kind == BattleEventKind.Hit && e.ActorId == m.Id));
            }
        }
        [Test] public void AFreeStarCastWaitsForTheBearFreezeToEnd()
        {
            foreach (bool current in new[] { false, true })
            {
                var s = Bear(current, Kit().WithDuos(1 << (int)PalDuo.ShadowStar));
                Field(State(s, 0), "StarCastDue", 1); Call(s, "Frost");
                var ses = new SimSession(s); int start = s.Events.Count; ses.Tick();
                Assert.AreEqual(!current, s.Events.Skip(start).Any(e => e.Detail == DuoRules.StarDetail));
                if (!current) continue;
                Set(s.FindById("m"), "Cooldown", 100); Set(s.FindById("m"), "SpellTicks", 100);
                Thaw(s); ses.Tick(); Assert.IsTrue(s.Events.Skip(start).Any(e => e.Detail == DuoRules.StarDetail));
            }
        }
        [Test] public void BearFreezeAlsoInterruptsAnAlreadyWindingHeavySwing()
        {
            foreach (bool current in new[] { false, true })
            {
                var s = Bear(current, heavy: true); var st = State(s, 0);
                Field(st, "HeavyWindup", 1); Field(st, "HeavySwingerId", "m");
                Field(st, "HeavyTargetId", s.Units.First(u => u.Side == 1).Id);
                Call(s, "Frost"); int start = s.Events.Count; new SimSession(s).Tick();
                Assert.Greater(s.FrozenTicks(s.FindById("m")), 0);
                Assert.AreEqual(!current, s.Events.Skip(start).Any(e => e.Kind == BattleEventKind.Hit && e.ActorId == "m"));
            }
        }
        [Test] public void BasicCrushReceivesTheParryBonusWithoutAnotherComboBonus()
        {
            foreach (bool current in new[] { false, true }) foreach (int comboRank in new[] { 1, 2 }) foreach (int bars in new[] { 0, 3 })
            {
                var s = Fight(current, bend: ClassBend.Collapse, comboRank: comboRank); Field(State(s, 0), "ParryBars", bars);
                var x = s.FindById("x"); int hp = x.Hp; Call(s, "Burst", s.FindById("a"), x, StatusKind.Crush);
                int expected = comboRank == 2 ? (bars == 3 ? 78 : 60) : (current && bars == 3 ? 13 : 10);
                Assert.AreEqual(expected, hp - x.Hp);
            }
        }
        [Test] public void OrdinaryDamageDoesNotGainTheComboParryBonus()
        {
            var s = Fight(); Field(State(s, 0), "ParryBars", 3); var x = s.FindById("x"); int hp = x.Hp;
            Call(s, "ApplyDamage", s.FindById("a"), 0, x, 100, "audit", false); Assert.AreEqual(100, hp - x.Hp);
        }
        [Test] public void StasisDurationUsesItsDurationGrowthInsteadOfMultiplyingDamageGrowthToo()
        {
            foreach (bool current in new[] { false, true }) foreach (int comboRank in new[] { 1, 2, 3 })
            {
                var s = Fight(current, bend: ClassBend.FrozenFlurry, comboRank: comboRank); var x = s.FindById("x");
                Call(s, "Burst", s.FindById("a"), x, StatusKind.Stasis);
                int[] expected = current ? new[] { 45, 135, 225 } : new[] { 45, 810, 2475 };
                Assert.AreEqual(expected[comboRank - 1], x.PinTicks);
            }
        }
        [Test] public void NaturalTicksProduceTheCorrectedStasisDuration()
        {
            var s = new BattleSimulation(Setup(bend: ClassBend.FrozenFlurry, comboRank: 3, nerve: true)); var ses = new SimSession(s);
            for (int tick = 0; tick < 600 && !s.IsFinished; tick++)
            {
                ses.Tick(); var burst = s.Events.FirstOrDefault(e => e.Kind == BattleEventKind.Burst && e.Detail == MarkRules.Stasis);
                if (burst.Detail != MarkRules.Stasis) continue;
                Assert.AreEqual(225, burst.Amount); return;
            }
            Assert.Fail("No naturally produced Stasis burst");
        }
        [Test] public void LimbGrowthCannotEraseHeldRelayOrSynthesisCosts()
        {
            var relay = Kit(talents: new[] { GluttonTalent.Precognition, GluttonTalent.GlancingBlow, GluttonTalent.DeepNode, GluttonTalent.CommandRelay });
            Assert.AreEqual(0, Call(Fight(false, relay), "PriceLeft", 0)); Assert.AreEqual(25, Call(Fight(true, relay), "PriceLeft", 0));
            var discount = Kit(talents: new[] { GluttonTalent.RoguePlanetProspecting, GluttonTalent.BranchHedge, GluttonTalent.SynthesisDiscount });
            Assert.AreEqual(0, Call(Fight(false, discount), "PactLeft", 0)); Assert.AreEqual(33, Call(Fight(true, discount), "PactLeft", 0));
        }
        [Test] public void SetupChecksumsDistinguishPreviouslyMissingCombatInputs()
        {
            var plain = new BattleSimulation(Setup()).OrderedStateSnapshot;
            var variants = new[] {
                Unit("x", armor: 50),
                new UnitSpec("x", UnitClass.Marksman, Row.Front, UnitRole.Fighter, 100, 30000, 0, 20, 0, 0, 0, 0, 0, 0),
                new UnitSpec("x", UnitClass.Marksman, Row.Front, UnitRole.Fighter, 100, 30000, 0, 0, 0, 0, 0, 20, 0, 0),
                new UnitSpec("x", UnitClass.Marksman, Row.Front, UnitRole.Fighter, 100, 30000, 0, 0, 0, 0, 0, 0, 20, 0),
                new UnitSpec("x", UnitClass.Marksman, Row.Front, UnitRole.Fighter, 100, 30000, 0, 0, 0, 0, 0, 0, 0, 20)
            };
            foreach (var spec in variants) Assert.AreNotEqual(plain, new BattleSimulation(Setup(target: spec)).OrderedStateSnapshot);
            Assert.AreEqual(new BattleSimulation(Setup(false)).OrderedStateSnapshot, new BattleSimulation(Setup(false, target: Unit("x", armor: 50))).OrderedStateSnapshot, "historical layout retained");
        }
        [Test] public void AllRuleBuildersRetainTheContractSwitchAndSnapshotIdentity()
        {
            foreach (var input in new[] { FightRules.Off.WithContracts(), Current })
            foreach (var method in typeof(FightRules).GetMethods().Where(m => m.Name.StartsWith("With") && m.ReturnType == typeof(FightRules)))
            {
                object[] args = method.GetParameters().Length == 0 ? new object[0] : new object[] { 4 };
                var result = (FightRules)method.Invoke(input, args);
                Assert.IsTrue(result.Contracts, method.Name); Assert.IsTrue(result.Corrections, method.Name);
            }
        }
        [Test] public void ARecordedCurrentFightReplaysExactly()
        {
            var setup = Setup(bend: ClassBend.FrozenFlurry, comboRank: 3); var s = new BattleSimulation(setup); var ses = new SimSession(s);
            Assert.IsTrue(ses.TryIssue(Directives.CommandKind, "0", "", 0, 0, "Hold", out string why), why);
            for (int i = 0; i < 120; i++) ses.Tick();
            var replay = ReplayRunner.Run(new BattleSimulation(setup), ses.Log.CloneForReplay(), s.CurrentTick);
            Assert.IsTrue(replay.Success, replay.Failure); Assert.AreEqual(ses.Checksum, replay.Checksum);
        }
        [Test] public void AnUnrecordableCommandCannotChangeTheFight()
        {
            var s = Fight(); var ses = new SimSession(s); var before = ses.Checksum; ses.Log.SetRecordingEnabled(false);
            Assert.IsFalse(ses.TryIssue(Directives.CommandKind, "0", "", 0, 0, "Hold", out string why));
            Assert.IsTrue(why.Contains("disabled")); Assert.AreEqual(before, ses.Checksum); Assert.AreEqual(0, ses.Log.Count);
            ses.Log.SetRecordingEnabled(true); Assert.IsTrue(ses.TryIssue(Directives.CommandKind, "0", "", 0, 0, "Hold", out why), why);
            Assert.AreEqual(1, ses.Log.Count);
        }
        [Test] public void ActiveSavesRetainTheirRulesAndChecksumsAcrossTheVersionBoundary()
        {
            foreach (int version in new[] { 35, 36 })
            {
                var c = Campaign.New(11, new CampaignTuning { RulesCap = version }); var fight = c.StartTutorial();
                for (int i = 0; i < 30; i++) fight.Tick();
                var restored = CampaignSave.Read(CampaignSave.Write(c));
                Assert.IsEmpty(restored.LoadNote); Assert.IsNotNull(restored.Active);
                Assert.AreEqual(version, restored.Active.RulesVersion);
                Assert.AreEqual(version == 36, restored.Active.Setup.Fight.Contracts);
                Assert.AreEqual(fight.Session.Checksum, restored.Active.Session.Checksum);
            }
        }
        [Test] public void CurrentArchiveEvidenceSurvivesSerializationAndReplays()
        {
            var c = Campaign.New(11); var fight = c.StartTutorial(); string id = fight.Setup.FightId;
            CampaignPilot.PlayFight(c, fight, new CampaignPilot(PilotStyle.Reference, 11));
            var restored = CampaignSave.Read(CampaignSave.Write(c));
            Assert.IsEmpty(restored.LoadNote);
            Assert.IsTrue(restored.Archive.TryReplay(id, out BattleSimulation replay, out string why), why);
            Assert.IsTrue(replay.Setup.Fight.Contracts); Assert.AreEqual(fight.Session.Checksum, StateChecksum.Compute(replay.OrderedStateSnapshot));
        }
        [Test] public void AContinuedSessionUsesTheNextAvailableCommandSequence()
        {
            var s = Fight(); var first = new SimSession(s);
            Assert.IsTrue(first.TryIssue(Directives.CommandKind, "0", "", 0, 0, "Hold", out string why), why);
            first.Tick(); var continued = new SimSession(s, first.Log);
            Assert.IsTrue(continued.TryIssue(Directives.CommandKind, "0", "x", 0, 0, "FocusFire", out why), why);
            Assert.AreEqual(2, continued.Log.Count); Assert.AreEqual(2, continued.Log.Commands[1].Sequence);
        }
    }
}
