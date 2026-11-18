using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using KindredSiege.Sim;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using NUnit.Framework;

namespace KindredSiege.Sim.Tests
{
    // Small deterministic reproductions. Reflection installs the exact transient state from the audit;
    // assertions observe targeting, damage/status, snapshots and command outcomes, not source text.
    public class Correctness35Tests
    {
        static readonly FightRules Old = FightRules.Campaign.WithRoadKit().WithLayers().WithMarks().WithFloor()
            .WithShadeForms().WithWhisper().WithGlutton().WithDuos().WithSized().WithFlavours().WithVows().WithDuoMoves();
        static readonly FightRules Fixed = Old.WithCorrections();
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        static object Call(object obj, string name, params object[] args)
        {
            var method = obj.GetType().GetMethods(Flags).First(m => m.Name == name && m.GetParameters().Length == args.Length &&
                m.GetParameters().Select((p, i) => args[i] == null || p.ParameterType.IsInstanceOfType(args[i])).All(x => x));
            return method.Invoke(obj, args);
        }
        static object State(BattleSimulation sim, int side) => ((Array)typeof(BattleSimulation).GetField("_sides", Flags).GetValue(sim)).GetValue(side);
        static void Field(object obj, string name, object value) => obj.GetType().GetField(name, Flags).SetValue(obj, value);
        static T Field<T>(object obj, string name) => (T)obj.GetType().GetField(name, Flags).GetValue(obj);
        static void Set(object obj, string name, object value) => obj.GetType().GetProperty(name, Flags).SetValue(obj, value);
        static PalKit Kit(PalDuo duo = PalDuo.None, ShadeRoute route = ShadeRoute.None, int form = 1, bool fed = false)
        {
            var levels = new int[GluttonRules.TalentCount + 1];
            for (int i = 1; i < levels.Length; i++) levels[i] = fed ? GluttonRules.Fed : GluttonRules.Held;
            var routes = new List<KeyValuePair<ShadeRoute, int>>();
            if (route != ShadeRoute.None) routes.Add(new KeyValuePair<ShadeRoute, int>(route, form));
            return new PalKit(routes, 0).WithTalents(levels).WithDuos(1 << (int)duo).WithDuoLevel(3);
        }
        static BattleSimulation Fight(PalKit kit, bool corrected = true, int clock = 0)
        {
            var ours = new List<UnitSpec> {
                new UnitSpec("s", UnitClass.Skirmisher, Row.Front, UnitRole.Fighter, 100, 30000).WithRank(2).WithClockPercent(clock),
                new UnitSpec("m", UnitClass.Marksman, Row.Back, UnitRole.Fighter, 100, 30000).WithBend(ClassBend.AroundTheCorner).WithComboRank(3),
                new UnitSpec("w", UnitClass.Warden, Row.Front, UnitRole.Fighter, 100, 30000) };
            var theirs = new List<UnitSpec>();
            for (int i = 0; i < 4; i++) theirs.Add(new UnitSpec("x" + i, UnitClass.Marksman, Row.Front, UnitRole.Fighter, 100, 30000));
            return new BattleSimulation(new BattleSetup("corrections", 11, ours, theirs, new TacticsProfile(), new TacticsProfile(),
                BattleSetup.DefaultTickLimit, 20, NerveRules.On(1000, 1000), 10, 400,
                new SideRules(0, 0, false).WithPals(kit), new SideRules(0, 0, false).WithPals(kit), corrected ? Fixed : Old));
        }
        [Test] public void EntropyCannotTurnTheFrontRowClockNegative()
        {
            var old = Fight(Kit(), false); var sim = Fight(Kit());
            Assert.AreEqual(1, Call(old, "Clock", old.FindById("s"), 60), "audit reproduction on rules34");
            Assert.AreEqual(51, Call(sim, "Clock", sim.FindById("s"), 60), "authored 15 percent time reduction");
            sim=Fight(Kit(fed: true),true,-75);
            Assert.AreEqual(10, Call(sim,"Clock",sim.FindById("s"),60),"fed Sink and maximum gear haste compound without a negative clock");
        }
        [Test] public void PlatingKeepsItsAuthoredHeldAndFedReduction()
        {
            var old = Fight(Kit(), false); Assert.Greater((int)Call(old, "Bonus", old.FindById("s"), GluttonTalent.DarkMatterPlating, 10), 100);
            var sim = Fight(Kit()); Assert.AreEqual(10, Call(sim, "Bonus", sim.FindById("s"), GluttonTalent.DarkMatterPlating, 10));
            sim = Fight(Kit(fed: true)); Assert.AreEqual(20, Call(sim, "Bonus", sim.FindById("s"), GluttonTalent.DarkMatterPlating, 10));
        }
        [Test] public void EchoKeepsItsAuthoredShareAndFeedBonusWithoutLimbSizing()
        {
            var old = Fight(Kit(), false); Assert.Greater((int)Call(old, "EchoShare", old.FindById("s")), 100);
            var sim = Fight(Kit()); Assert.AreEqual(100, Call(sim, "EchoShare", sim.FindById("s")));
            sim=Fight(Kit(fed:true)); Assert.AreEqual(ShadeRules.EchoPercent+2*GluttonRules.EchoStrikePercent,Call(sim,"EchoShare",sim.FindById("s")),"Feed still doubles the authored talent bonus");
        }
        [Test] public void SlowLightAddsSecondsWithoutPercentSizing()
        {
            var old = Fight(Kit(fed: true), false); var sim = Fight(Kit(fed: true));
            Assert.Greater((int)Call(old, "FreezeTicks", old.FindById("m")), ClassSpells.StoppedFreezeTicks + 60);
            Assert.AreEqual(ClassSpells.StoppedFreezeTicks + 60, Call(sim, "FreezeTicks", sim.FindById("m")));
        }
        [Test] public void PrecognitionWidensTheWindowByItsAuthoredPercent()
        {
            var none = Fight(PalKit.None); var sim = Fight(Kit());
            Assert.AreEqual(none.ParryWindowTicksFor(0) * 150 / 100, sim.ParryWindowTicksFor(0));
        }
        [Test] public void ExposedBurstKeepsItsBonusAndItsJumpInTheSnapshot()
        {
            var old = Fight(Kit(), false); Call(old, "Burst", old.FindById("m"), old.FindById("x0"), StatusKind.Exposed);
            Assert.AreEqual(0, old.FindById("x0").Statuses[StatusKind.WideOpen].Size, "audit reproduction");
            var sim = Fight(Kit()); Call(sim, "Burst", sim.FindById("m"), sim.FindById("x0"), StatusKind.Exposed);
            var wide = sim.FindById("x0").Statuses[StatusKind.WideOpen];
            Assert.Greater(wide.Size, 0); string snapshot = sim.OrderedStateSnapshot;
            Set(wide, "Store", wide.Store + 1); Assert.AreNotEqual(snapshot, sim.OrderedStateSnapshot);
            snapshot = sim.OrderedStateSnapshot; Set(wide, "Size", wide.Size + 1); Assert.AreNotEqual(snapshot, sim.OrderedStateSnapshot);
        }
        [Test] public void MeleeShadowTwinCanChooseAndStrikeATargetDuringFallBack()
        {
            var old = Fight(Kit(PalDuo.ShadowTwin), false); Field(State(old, 0), "FallBackTicks", 50);
            Assert.IsNull(Call(old, "ChooseTarget", old.FindById("s")));
            var sim = Fight(Kit(PalDuo.ShadowTwin)); Field(State(sim, 0), "FallBackTicks", 500);
            Assert.IsNotNull(Call(sim, "ChooseTarget", sim.FindById("s")));
            Assert.IsNotNull(Call(sim, "SpellTarget", sim.FindById("s"), false));
            Field(State(sim, 0), "ReturnTicks", 500);
            Assert.IsNotNull(Call(sim, "ChooseTarget", sim.FindById("s")), "the shadow fights during Rift Mine retreat too");
            Field(State(sim, 0), "FallBackTicks", 0);
            Assert.IsNull(Call(sim, "ChooseTarget", sim.FindById("s")), "Rift Mine return price resumes after retreat");
            Field(State(sim, 0), "FallBackTicks", 500);
            Set(sim.FindById("s"), "Cooldown", 0); Set(sim.FindById("s"), "SpellTicks", 0);
            var session = new SimSession(sim); for (int i = 0; i < 200; i++) session.Tick();
            Assert.IsTrue(sim.Events.Any(e => e.Kind == BattleEventKind.Hit && e.ActorId == "s"));
        }
        [Test] public void ShadowTwinStartingNerveUsesHisActualMaximumHealth()
        {
            var sim = Fight(Kit(PalDuo.ShadowTwin));
            Assert.AreEqual(sim.Units.Where(u => u.Side == 0 && !u.Benched).Sum(u => u.MaxHp), Field<int>(State(sim, 0), "StartHp"));
        }
        [Test] public void SacrificeDropsTheVictimsStreak()
        {
            var sim = Fight(Kit(PalDuo.BloodParry)); Call(sim, "Sacrifice", sim.FindById("w"), 100);
            Assert.AreEqual(2, sim.DuoLevelOf(0)); Assert.AreEqual(3, sim.DuoLevelOf(1));
        }
        [Test] public void GorgeCountsKillsRatherThanStreakLevelsForItsCatch()
        {
            var sim = Fight(Kit(PalDuo.Gorge)); var star = sim.FindById("s");
            Call(sim, "Gorged", star, 0, sim.FindById("x0")); Assert.AreEqual(1, star.GorgeKills);
            int before = sim.FindById("m").UnitNerve; Set(star,"Alive",false);
            Call(sim,"Emit",new BattleEvent(sim.CurrentTick,BattleEventKind.Kill,1,"x0",(int)UnitClass.Marksman,star.Id,(int)star.Class,0,"audit-fall"));
            Assert.AreEqual(DuoRules.GorgeNervePerKill, before - sim.FindById("m").UnitNerve);
            sim=Fight(Kit(PalDuo.Gorge));star=sim.FindById("s");Call(sim,"Gorged",star,0,sim.FindById("x0"));
            before=sim.FindById("m").UnitNerve;Call(sim,"Sacrifice",star,100);
            Assert.AreEqual(DuoRules.GorgeNervePerKill,before-sim.FindById("m").UnitNerve,"Sacrifice cannot evade Gorge's catch");
        }
        [Test] public void ExtendedBreachCannotStartAnotherPlainImpact()
        {
            var sim = Fight(Kit(PalDuo.EndlessUnleash, ShadeRoute.Breach, 1));
            Field(State(sim, 0), "UnleashTicks", 100); Field(State(sim, 0), "ExposedTicks", 0);
            Assert.IsFalse(sim.CanUseOrder(0, Directive.Unleash, "", out string why)); Assert.IsTrue(why.Contains("Breach"));
        }
        [Test] public void RuptureKillCanTriggerExecutionerBeforeFocusEnds()
        {
            var sim = Fight(Kit(PalDuo.Executioner, ShadeRoute.Rupture)); var target = sim.FindById("x0");
            Call(target.Statuses, "Apply", StatusKind.Crush, 100, 0, "m", false, 1);
            Set(target, "Hp", 1); var state = State(sim, 0);
            Field(state, "FocusTargetId", target.Id); Field(state, "FocusTicks", 100); Field(state, "RuptureSteps", 1);
            Call(sim, "Rupture", 0, state, target);
            Assert.IsTrue(sim.Events.Any(e => e.Detail == DuoRules.ExecutionerDetail)); Assert.AreEqual(0, sim.FocusTicksLeft(0));
        }
        [Test] public void CrownedFreeCastCannotScheduleItselfAgain()
        {
            var sim = Fight(Kit(PalDuo.CrownedMarks)); Field(sim, "_duoCasting", true);
            Call(sim, "DuoBurst", sim.FindById("s")); Assert.AreEqual(0, Field<int>(State(sim, 0), "StarCastDue"));
            Field(sim, "_duoCasting", false); Call(sim, "DuoBurst", sim.FindById("s"));
            Assert.AreEqual(2, Field<int>(State(sim, 0), "StarCastDue"));
        }
        [Test] public void ReadinessRejectsTheSameInvalidTargetsAsCommands()
        {
            var sim = Fight(Kit()); var session = new SimSession(sim);
            foreach (string target in new[] { "", "missing", "s" })
            {
                Assert.IsFalse(sim.CanUseOrder(0, Directive.FocusFire, target, out _));
                Assert.IsFalse(session.TryIssue(Directives.CommandKind, "0", target, 0, 0, Directives.Name(Directive.FocusFire), out _));
            }
            Assert.IsTrue(sim.CanUseOrder(0, Directive.FocusFire, "x0", out _));
            Assert.IsFalse(sim.CanUseOrder(-1, Directive.FocusFire, "x0", out _));
            sim=Fight(Kit(route:ShadeRoute.Rupture));
            Field(State(sim,0),"FocusTicks",20);Field(State(sim,0),"FocusTargetId","x0");Set(sim.FindById("x0"),"Alive",false);
            Assert.AreEqual("",sim.FollowUpOf(0,Directive.FocusFire,"x0"),"a dead target cannot advertise a free Rupture");
            Assert.IsTrue(sim.CanUseOrder(0,Directive.FocusFire,"x1",out _),"the next living enemy can be selected");
        }
        [Test] public void TakingADuoDirectlyInitialisesItsPersistentStreak()
        {
            var c = Campaign.New(7100, new CampaignTuning { EnemyScaleTier1 = 40 });
            Assert.IsTrue(CampaignPilot.PlayTutorial(c, PilotStyle.Reference, 7100, 10)); c.StartRun();
            Assert.IsTrue(c.TryTakeDuo(PalDuo.Gorge)); Assert.AreEqual(1, c.DuoLevel);
            Assert.AreEqual(1, CampaignSave.Read(CampaignSave.Write(c), c.Tuning).DuoLevel);
        }
        [Test] public void AFreeDuoDoesNotRequireTheOrdinaryRewardsMaterials()
        {
            var c = Campaign.New(7100, new CampaignTuning { EnemyScaleTier1 = 40 });
            Assert.IsTrue(CampaignPilot.PlayTutorial(c, PilotStyle.Reference, 7100, 10)); c.StartRun();
            Assert.IsTrue(c.TryRankUp(c.Reserve.First(f => f.Class == UnitClass.Skirmisher).Id, StarVow.Reaver));
            Assert.IsTrue(c.TryLendTalent(GluttonTalent.NucleusEdge));
            Call(c, "SetMaterials", 0);
            Assert.IsTrue((bool)Call(c, "RestoreFavourOffer", Pal.Glutton,
                new List<Favour> { Favour.DuoGorge, Favour.GluttonDarkMatterPlating }, 17, false));
            Assert.IsTrue(c.TryTakeFavour(0, out string why), why);
            Assert.AreEqual(0, c.Materials); Assert.IsNotNull(c.FavourOffer);
            Assert.AreEqual(1, c.FavourOffer.Choices.Count);
            Assert.IsFalse(c.CanTakeFavour(0, out _), "the normal reward still has its normal price");
        }
        static BattleSimulation VowFight(StarVow vow, int rank)
        {
            var cls=VowRules.ClassOf(vow);
            var ours=new List<UnitSpec>{new UnitSpec("v",cls,Row.Front,UnitRole.Fighter,100,3000).WithRank(rank)};
            for(int i=0;i<3;i++)ours.Add(new UnitSpec("p"+i,UnitClass.Marksman,Row.Back,UnitRole.Fighter,100,3000));
            var kit=PalKit.None.WithVows(new[]{new KeyValuePair<string,StarVow>("v",vow)});
            return new BattleSimulation(new BattleSetup("vow-final",4,ours,
                new[]{new UnitSpec("x",UnitClass.Medic,Row.Front,UnitRole.Fighter,100,30000)},
                new TacticsProfile(),new TacticsProfile(),BattleSetup.DefaultTickLimit,20,NerveRules.Off,0,400,
                new SideRules(0,0,false).WithPals(kit),null,FightRules.Campaign.WithRoadKit().WithLayers().WithFloor().WithWhisper().WithVows().WithCorrections()));
        }
        [Test] public void BastionsFinalFormCoversTheWholeWardsRow()
        {
            var rank2=VowFight(StarVow.Bastion,2);Set(rank2.FindById("v"),"GuardTargetId","p0");
            Assert.IsFalse((bool)Call(rank2,"BastionCovers",rank2.FindById("v"),rank2.FindById("p2")));
            var rank3=VowFight(StarVow.Bastion,3);Set(rank3.FindById("v"),"GuardTargetId","p0");
            Assert.IsTrue(rank3.IsGuarded(rank3.FindById("p2")),"final Bastion covers the distant ally in the ward's row");
        }
        [Test] public void MartyrsFinalFormActuallyDoublesAHalfHealthHeal()
        {
            int Heal(bool hurt)
            {
                var sim=VowFight(StarVow.Martyr,3);var medic=sim.FindById("v");var patient=sim.FindById("p0");
                Set(patient,"Hp",1000);if(hurt)Set(medic,"Hp",medic.MaxHp/2);
                Call(sim,"Treat",medic,patient);return patient.Hp-1000;
            }
            Assert.Greater(Heal(false),0);Assert.AreEqual(Heal(false)*2,Heal(true));
        }
        [Test] public void DuelistsFinalFormUsesTheFinalCritAndFiresOnFocus()
        {
            var sim=VowFight(StarVow.Duelist,3);var unit=sim.FindById("v");
            Field(State(sim,0),"FocusTicks",100);Field(State(sim,0),"FocusTargetId","x");
            Assert.AreEqual(VowRules.DuelistFinalPercent,Call(sim,"DuelistPercent",unit));
            Assert.IsTrue((bool)Call(sim,"TryStrike",unit));
            Assert.IsTrue(sim.Events.Any(e=>e.Kind==BattleEventKind.Cash&&e.Detail==VowRules.DuelistDetail));
        }
        [Test] public void WallAndLockdownDoNotApplyTheSameCutTwice()
        {
            int Damage(bool corrected,bool lockdown)
            {
                var rules=FightRules.CampaignChiefTactics.WithParryCenter().WithLayers();
                if(corrected)rules=rules.WithCorrections();
                var sim=new BattleSimulation(new BattleSetup("wall-layer",4,
                    new[]{new UnitSpec("s",UnitClass.Marksman)},
                    new[]{new UnitSpec("w",UnitClass.Warden,Row.Front,UnitRole.Fighter,100,10000),
                        new UnitSpec("chief",UnitClass.Trapper,Row.Back,UnitRole.Chief,100,10000)},
                    new TacticsProfile(),new TacticsProfile(),BattleSetup.DefaultTickLimit,20,NerveRules.Off,0,400,
                    new SideRules(1,0,false),new SideRules(1,0,false,0,ChiefSpell.SteelStorm,kit:ChiefKit.Skullpeak),rules));
                Field(State(sim,1),"WallTicks",100);Field(State(sim,1),"Lockdown",lockdown);
                var target=sim.FindById("w");int before=target.Hp;
                Call(sim,"ApplyDamage",sim.FindById("s"),0,target,100,"audit-wall",false);return before-target.Hp;
            }
            Assert.Less(Damage(false,true),Damage(false,false),"rules34 reproduction: the same wall is applied twice");
            Assert.AreEqual(100*ChiefSpells.WallDamagePercent/100,Damage(true,true),"rules35 applies exactly one Lockdown wall cut");
            Assert.Less(Damage(false,true),Damage(true,true),"the regression no longer doubles the cut");
        }
        [Test] public void CorrectedBuilderCopiesRetainAllTheirRules()
        {
            var rules=FightRules.Campaign.WithCorrections().WithFloor().WithLayers().WithRoadKit().WithClearRoad()
                .WithWhisper().WithVows().WithSized().WithDuoMoves().WithSquadScreen();
            Assert.IsTrue(rules.Corrections&&rules.Floor&&rules.Layers&&rules.Whisper&&rules.Vows&&rules.Sized&&rules.DuoMoves);
            Assert.IsFalse(FightRules.Off.WithCorrections().IsNone);
        }
    }
}
