using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using NUnit.Framework;

namespace KindredSiege.Sim.Tests
{
    /// The Glutton's whole tree retargeted and the live hooks (campaign rules 46; FightRules.TreeRetarget; Jacob, 16 Nov 2026:
    /// "Yes, lock it", then "Go, both"). One quick test per piece that has a live target, only to prove each runs (Jacob,
    /// 12 Oct 2026: build first). Nothing here is tuning.
    public class TreeRetargetTests
    {
        private static readonly FightRules Combos46 = FightRules.Campaign.WithFloor().WithGlutton().WithTreeRetarget();
        private static readonly FightRules Chief26 =
            FightRules.CampaignChiefTactics.WithParryCenter().WithLayers().WithFloor().WithShadeForms().WithGlutton();
        private static readonly FightRules Chief46 = Chief26.WithTreeRetarget();
        private static readonly FightRules Road46 =
            FightRules.Campaign.WithRoadKit().WithLayers().WithMarks().WithFloor().WithShadeForms().WithGlutton().WithTreeRetarget();

        private static List<UnitSpec> Side(params UnitSpec[] units) => new List<UnitSpec>(units);

        private static UnitSpec U(string id, UnitClass unitClass, int health = 3000) =>
            new UnitSpec(id, unitClass, SliceClasses.Stats(unitClass).DefaultRow, UnitRole.Fighter, 100, health);

        private static UnitSpec At(string id, UnitClass unitClass, Row row) => new UnitSpec(id, unitClass, row, UnitRole.Fighter, 100, 3000);

        private static PalKit Kit(ShadeRoute route, params GluttonTalent[] talents)
        {
            var routes = new List<KeyValuePair<ShadeRoute, int>>();
            if (route != ShadeRoute.None) routes.Add(new KeyValuePair<ShadeRoute, int>(route, 1));
            var levels = new int[GluttonRules.TalentCount + 1];
            foreach (GluttonTalent t in talents) levels[(int)t] = GluttonRules.Held;
            return new PalKit(routes, 0).WithTalents(levels);
        }

        private static PalKit Kit(params GluttonTalent[] talents) => Kit(ShadeRoute.None, talents);

        // Our side with this kit against theirs, under these rules.
        private static BattleSimulation Fight(string id, ulong seed, PalKit kit, List<UnitSpec> ours, List<UnitSpec> theirs, FightRules rules)
        {
            return new BattleSimulation(new BattleSetup(id, seed, ours, theirs, new TacticsProfile(), new TacticsProfile(),
                BattleSetup.DefaultTickLimit, BattleSetup.DefaultDirectivePoints, NerveRules.Off, 0, 400,
                new SideRules(0, 0, false).WithPals(kit), null, rules));
        }

        // The Bloodridge chief with only his Cleave beside one fighter of his, against your Pal and three (GluttonTests).
        private static BattleSimulation Cleaver(string id, PalKit kit, FightRules rules)
        {
            var ours = Side(new UnitSpec("pal", UnitClass.Marksman, Row.Back, UnitRole.Pal, 100, 3000),
                U("s", UnitClass.Skirmisher), U("m", UnitClass.Marksman), U("t", UnitClass.Trapper));
            return new BattleSimulation(new BattleSetup(id, 11, ours,
                Side(U("x.s", UnitClass.Skirmisher), new UnitSpec("chief", UnitClass.Warden, Row.Front, UnitRole.Chief, 100, 8000)),
                new TacticsProfile(), new TacticsProfile(), BattleSetup.DefaultTickLimit, BattleSetup.DefaultDirectivePoints,
                NerveRules.On(NerveRules.Full, NerveRules.Full), 10, 400, new SideRules(1, 0, true).WithPals(kit),
                new SideRules(2, 0, false, 0, ChiefSpell.Eclipse, kit: ChiefKit.Bloodridge, moves: ChiefMove.Cleave), rules));
        }

        private static bool Order(SimSession session, Directive directive, string target = "") =>
            session.TryIssue(Directives.CommandKind, "0", target, 0, 0, Directives.Name(directive), out _);

        // Ticks on until his Cleave's beat is this many ticks off, then Holds (GluttonTests).
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

        private static int RunUntil(BattleSimulation sim, int ticks, Func<BattleEvent, bool> match)
        {
            int seen = 0;
            while (true)
            {
                for (; seen < sim.Events.Count; seen++) if (match(sim.Events[seen])) return seen;
                if (sim.IsFinished || sim.CurrentTick >= ticks) return -1;
                sim.Tick();
            }
        }

        private static bool Is(BattleEvent e, BattleEventKind kind, ClassBend combo) => e.Kind == kind && e.Detail == ClassSpells.Name(combo);

        // ------------------------------------------------------------ the rules and the words

        [Test]
        public void Rules46TurnsTheTreeOnAndEveryTalentHasItsLockedWords()
        {
            Assert.AreEqual(46, Campaign.RulesVersion);
            Assert.AreEqual(46, Campaign.TreeRules);
            Assert.IsTrue(FightRules.Campaign.WithTreeRetarget().TreeRetarget);
            Assert.IsFalse(FightRules.Campaign.WithGlutton().TreeRetarget);
            Assert.IsTrue(Fight("on", 4, Kit(), Side(U("w", UnitClass.Warden)), Side(U("x", UnitClass.Medic)), Combos46).TreeOn);
            for (int i = 1; i <= GluttonRules.TalentCount; i++)
                Assert.IsNotEmpty(GluttonRules.Describe((GluttonTalent)i), ((GluttonTalent)i).ToString());
            Assert.AreEqual("Still Wall freezes 3 s, not 2.", GluttonRules.Describe(GluttonTalent.FoldShield));
            Assert.AreEqual("Fold Door takes half the nerve from your front row.", GluttonRules.DescribeRules29(GluttonTalent.FoldShield));
            Assert.AreEqual("Half Life", GluttonRules.Name(GluttonTalent.SynthArmour));
            Assert.AreEqual("Casters", GluttonRules.Limbs[4]);
        }

        // ------------------------------------------------------------ the front line and the strikers' combos

        [Test]
        public void FoldShieldHoldsStillWallLongerAndFoldStepPinsCollapseLonger()
        {
            int Wall(PalKit kit)
            {
                var sim = Fight("wall", 4, kit, Side(U("w", UnitClass.Warden).WithBend(ClassBend.StillWall)),
                    Side(U("x1", UnitClass.Warden), U("x2", UnitClass.Warden)), Combos46);
                int at = RunUntil(sim, 6000, e => Is(e, BattleEventKind.Freeze, ClassBend.StillWall));
                Assert.GreaterOrEqual(at, 0, "nobody hit his shield");
                return sim.Events[at].Amount;
            }
            Assert.AreEqual(Wall(Kit()) + RetargetRules.WallMoreTicks, Wall(Kit(GluttonTalent.FoldShield)), "Fold Shield");

            int Pin(PalKit kit)
            {
                var sim = Fight("collapse", 4, kit, Side(U("k", UnitClass.Skirmisher).WithBend(ClassBend.Collapse)),
                    Side(At("m1", UnitClass.Medic, Row.Front), At("m2", UnitClass.Medic, Row.Front), At("m3", UnitClass.Medic, Row.Front)), Combos46);
                int at = RunUntil(sim, 6000, e => Is(e, BattleEventKind.Pull, ClassBend.Collapse));
                Assert.GreaterOrEqual(at, 0, "no black hole dropped");
                return sim.FindById(sim.Events[at].TargetId).PinTicks;
            }
            Assert.AreEqual(Pin(Kit()) + RetargetRules.CollapseMoreTicks, Pin(Kit(GluttonTalent.FoldStep)), "Fold Step");
        }

        [Test]
        public void DoorwaysCatchIsLeftWithAnOpening()
        {
            var sim = Fight("doorway", 4, Kit(), Side(U("w", UnitClass.Warden), U("t", UnitClass.Trapper).WithBend(ClassBend.Doorway)),
                Side(U("x1", UnitClass.Marksman), U("x2", UnitClass.Marksman)), Combos46);
            int at = RunUntil(sim, 6000, e => Is(e, BattleEventKind.Portal, ClassBend.Doorway));
            Assert.GreaterOrEqual(at, 0, "no shooter fell into her trap");
            Assert.IsTrue(sim.FindById(sim.Events[at].TargetId).Statuses[StatusKind.Opening].On, "left with an Opening");
        }

        // ------------------------------------------------------------ command

        [Test]
        public void PrecognitionStaggersAParriedManLongerAndNoLongerWidensTheWindow()
        {
            int Stagger(PalKit kit, out BattleSimulation sim)
            {
                sim = Cleaver("precog", kit, Chief46);
                HoldBefore(sim, new SimSession(sim), 1);
                return sim.Events.First(e => e.Kind == BattleEventKind.Parry && e.ActorSide == 0).Amount;
            }
            Assert.AreEqual(FightRules.StaggerTicks, Stagger(Kit(), out _));
            Assert.AreEqual(FightRules.StaggerTicks + RetargetRules.StaggerMoreTicks, Stagger(Kit(GluttonTalent.Precognition), out BattleSimulation longer));
            Assert.IsTrue(longer.IsStaggered(longer.FindById("chief")), "the stagger is a timer");

            var early = Cleaver("window", Kit(GluttonTalent.Precognition), Chief46);
            HoldBefore(early, new SimSession(early), 20);
            Assert.IsFalse(early.Events.Any(e => e.Kind == BattleEventKind.Parry && e.ActorSide == 0), "the window stays 0.2 s");
        }

        [Test]
        public void BranchAnchorThickensAnyonesShieldAndCommandRelayNoLongerPaysSanctuarysPrice()
        {
            int ShieldOn(string who, PalKit kit, FightRules rules, out int stagger)
            {
                var sim = Cleaver("anchor", kit, rules);
                HoldBefore(sim, new SimSession(sim), 1);
                stagger = sim.Events.First(e => e.Kind == BattleEventKind.Parry && e.ActorSide == 0).Amount;
                return sim.FindById(who).Statuses[StatusKind.Shield].Size;
            }
            int plain = ShieldOn("m", Kit(ShadeRoute.Sanctuary), Chief46, out _);
            Assert.AreEqual(plain * 2, ShieldOn("m", Kit(ShadeRoute.Sanctuary, GluttonTalent.BranchAnchor), Chief46, out _), "on the back row too");
            Assert.AreEqual(plain, ShieldOn("m", Kit(ShadeRoute.Sanctuary, GluttonTalent.BranchAnchor), Chief26, out _), "not before rules 46");
            ShieldOn("m", Kit(ShadeRoute.Sanctuary, GluttonTalent.CommandRelay), Chief46, out int stagger);
            Assert.AreEqual(0, stagger, "Sanctuary's price stands");
        }

        [Test]
        public void GlancingBlowDoublesMirrorAndDeepNodeNoLongerBoostsTheRiftMine()
        {
            int Mirror(PalKit kit)
            {
                var sim = Cleaver("mirror", kit, Chief46);
                HoldBefore(sim, new SimSession(sim), 1);
                return sim.Events.First(e => e.Kind == BattleEventKind.Trick && e.Detail == ShadeRules.MirrorDetail).Amount;
            }
            Assert.AreEqual(Mirror(Kit(ShadeRoute.Mirror)) * 2, Mirror(Kit(ShadeRoute.Mirror, GluttonTalent.GlancingBlow)), 1, "Glancing Blow");

            int Blast(PalKit kit)
            {
                var sim = Cleaver("node", kit, Chief46);
                var session = new SimSession(sim);
                session.Tick();
                Assert.IsTrue(Order(session, Directive.FallBack));
                int at = RunUntil(sim, 3000, e => e.Kind == BattleEventKind.Hit && e.Detail == ShadeRules.MineDetail);
                Assert.GreaterOrEqual(at, 0, "the mine never went off");
                return sim.Events[at].Amount;
            }
            Assert.AreEqual(Blast(Kit(ShadeRoute.RiftMine)), Blast(Kit(ShadeRoute.RiftMine, GluttonTalent.DeepNode)), "the safe answer pays no more");
        }

        // ------------------------------------------------------------ the strikers' and shooters' boosts

        [Test]
        public void NucleusEdgeDoublesTwinCutAndStripRipsHarder()
        {
            BattleSimulation Road(PalKit kit)
            {
                var sim = Fight("cuts", 4, kit, Side(U("s", UnitClass.Skirmisher), U("w", UnitClass.Warden)),
                    Side(At("m1", UnitClass.Medic, Row.Front), At("m2", UnitClass.Medic, Row.Front), At("m3", UnitClass.Medic, Row.Front)), Road46);
                return sim;
            }
            int First(BattleSimulation sim, Func<BattleEvent, bool> match)
            {
                int at = RunUntil(sim, 9000, match);
                Assert.GreaterOrEqual(at, 0, "it never came");
                return sim.Events[at].Amount;
            }
            Func<BattleEvent, bool> cut = e => e.Kind == BattleEventKind.Hit && e.ActorId == "s" && e.Detail == BoostRules.CutDetail;
            Assert.AreEqual(First(Road(Kit()), cut) * 2, First(Road(Kit(GluttonTalent.NucleusEdge)), cut), 1, "Nucleus Edge");

            Func<BattleEvent, bool> rip = e => e.Kind == BattleEventKind.Cash && e.ActorId == "s" && e.Detail == ClassSpells.RipDetail;
            Assert.AreEqual(First(Road(Kit()), rip) * (100 + RetargetRules.RipMorePercent) / 100, First(Road(Kit(GluttonTalent.Strip)), rip), 1, "Strip");
        }

        [Test]
        public void DarkMatterRoundsSendsSteadyThroughAtFullPower()
        {
            int Pierce(PalKit kit)
            {
                var sim = Fight("steady", 4, kit, Side(U("a", UnitClass.Marksman), U("w", UnitClass.Warden)),
                    Side(At("m1", UnitClass.Medic, Row.Front), At("m2", UnitClass.Medic, Row.Back)), Road46);
                int at = RunUntil(sim, 9000, e => e.Kind == BattleEventKind.Hit && e.ActorId == "a" && e.Detail == BoostRules.PierceDetail);
                Assert.GreaterOrEqual(at, 0, "no Steady shot went through");
                return sim.Events[at].Amount;
            }
            Assert.AreEqual(Pierce(Kit()) * 2, Pierce(Kit(GluttonTalent.DarkMatterRounds)), 1);
        }

        // ------------------------------------------------------------ control and the Stopped Bullets hook

        [Test]
        public void TimeLockMakesASnareTakeTwoCashes()
        {
            // Our Trapper snares their Skirmishers as they dive in, and our Marksman's Aimed Shot cashes the Snare.
            bool StillSnared(PalKit kit)
            {
                var sim = Fight("timelock", 4, kit, Side(U("t", UnitClass.Trapper), U("a", UnitClass.Marksman), U("w", UnitClass.Warden)),
                    Side(U("x1", UnitClass.Skirmisher), U("x2", UnitClass.Skirmisher)), Combos46.WithPayoffs());
                int at = RunUntil(sim, 12000, e => e.Kind == BattleEventKind.Cash && e.ActorId == "a" && e.Detail == ClassSpells.PinDetail);
                Assert.GreaterOrEqual(at, 0, "no Aimed Shot ever cashed a Snare");
                return sim.FindById(sim.Events[at].TargetId).SnareTicks > 0;
            }
            Assert.IsFalse(StillSnared(Kit()), "one cash spends it");
            Assert.IsTrue(StillSnared(Kit(GluttonTalent.TimeLock)), "Time Lock: it takes a second cash");
        }

        [Test]
        public void StoppedBulletsCashesABleedAndSlowLightRoundsMakesItTriple()
        {
            // Our Trapper's barbed snare leaves their divers bleeding; the third Stopped Bullet fires all three into one.
            int Cash(PalKit kit)
            {
                var sim = Fight("stoppedbleed", 4, kit,
                    Side(U("t", UnitClass.Trapper), U("a", UnitClass.Marksman).WithBend(ClassBend.StoppedBullets), U("w", UnitClass.Warden)),
                    Side(U("x1", UnitClass.Skirmisher, 9000), U("x2", UnitClass.Skirmisher, 9000)), Combos46);
                int at = RunUntil(sim, 20000, e => e.Kind == BattleEventKind.Cash && e.ActorId == "a" && e.Detail == ClassSpells.RipDetail);
                Assert.GreaterOrEqual(at, 0, "the volley never landed on a bleeding man");
                Assert.AreEqual(0, sim.FindById(sim.Events[at].TargetId).BleedTicks, "the Bleed is spent");
                return sim.Events[at].Amount;
            }
            int doubled = Cash(Kit());
            Assert.Greater(doubled, 0);
            Assert.AreEqual(doubled * 2, Cash(Kit(GluttonTalent.SlowLightRounds)), 2, "triple, not double: twice the extra");
        }

        // ------------------------------------------------------------ the four first left untested (Jacob: "Yes, write them")

        // Reflection into the sim's own private checks, as Correctness35Tests does: exact, and no fight has to arrange it.
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        private static object Call(object obj, string name, params object[] args)
        {
            MethodInfo method = obj.GetType().GetMethods(Flags).First(m => m.Name == name && m.GetParameters().Length == args.Length &&
                m.GetParameters().Select((p, i) => args[i] == null || p.ParameterType.IsInstanceOfType(args[i])).All(x => x));
            return method.Invoke(obj, args);
        }

        private static void Put(BattleUnit u, StatusKind kind) => Call(u.Statuses, "Apply", kind, 100, 0, "", false, 1);

        [Test]
        public void DeepNodeMakesRupturesCrackThinArmourTwiceAsMuch()
        {
            int Thinned(PalKit kit, FightRules rules)
            {
                BattleSimulation sim = Cleaver("crack", kit, rules);
                BattleUnit chief = sim.FindById("chief");
                Put(chief, StatusKind.Crack);
                return (int)Call(sim, "Cracked", chief, 60);
            }
            Assert.AreEqual(60 * (100 - ShadeRules.CrackPercent) / 100, Thinned(Kit(), Chief46), "a plain Crack");
            int twice = Math.Min(100, ShadeRules.CrackPercent * 2);
            Assert.AreEqual(60 * (100 - twice) / 100, Thinned(Kit(GluttonTalent.DeepNode), Chief46), "Deep Node: twice as thin");
            Assert.AreEqual(60 * (100 - ShadeRules.CrackPercent) / 100, Thinned(Kit(GluttonTalent.DeepNode), Chief26), "not before rules 46");
        }

        [Test]
        public void StasisWindUpBurstsTheFlavourStacksAtTwoOnASnaredMan()
        {
            int BurstAt(PalKit kit, bool snared)
            {
                BattleSimulation sim = Cleaver("stasis", kit, Chief46);
                BattleUnit them = sim.FindById("x.s");
                if (snared) Put(them, StatusKind.Snare);
                return (int)Call(sim, "BurstAt", sim.FindById("s"), them);
            }
            Assert.AreEqual(MarkRules.MaxStacks, BurstAt(Kit(), true), "3 without the talent");
            Assert.AreEqual(MarkRules.MaxStacks, BurstAt(Kit(GluttonTalent.StasisWindUp), false), "3 on a man who is not snared");
            Assert.AreEqual(MarkRules.MaxStacks - RetargetRules.BurstFewer, BurstAt(Kit(GluttonTalent.StasisWindUp), true), "2 on a snared man");
        }

        [Test]
        public void CommandRelayMakesYourChiefSpellHitHarder()
        {
            // Our side casts its Eclipse at their chief and his man; the first of its hits on them.
            int Eclipse(PalKit kit)
            {
                BattleSimulation sim = Cleaver("relay", kit, Chief46);
                var session = new SimSession(sim);
                session.Tick();
                Assert.IsTrue(Order(session, Directive.Eclipse, "chief") || Order(session, Directive.Eclipse), "the Eclipse was refused");
                int at = RunUntil(sim, 3000, e => e.Kind == BattleEventKind.Hit && e.Detail == Moves.EclipseDetail &&
                    (e.TargetId == "chief" || e.TargetId == "x.s"));
                Assert.GreaterOrEqual(at, 0, "our Eclipse never hit them");
                return sim.Events[at].Amount;
            }
            int plain = Eclipse(Kit()), relayed = Eclipse(Kit(GluttonTalent.CommandRelay));
            Assert.Greater(relayed, plain, "Command Relay");
            Assert.LessOrEqual(relayed, plain * 2 + 2, "at most twice as hard");
        }

        [Test]
        public void TheWardensBashStartsTheStaggerTimer()
        {
            var sim = Fight("bash", 4, Kit(), Side(U("w", UnitClass.Warden)),
                Side(At("m1", UnitClass.Medic, Row.Front), At("m2", UnitClass.Medic, Row.Front)), Road46);
            int at = RunUntil(sim, 9000, e => e.Kind == BattleEventKind.Boost && e.ActorId == "w" && e.Detail == BoostRules.ShieldBash);
            Assert.GreaterOrEqual(at, 0, "the Warden never bashed");
            BattleUnit hit = sim.FindById(sim.Events[at].TargetId);
            Assert.IsTrue(sim.IsStaggered(hit), "staggered right after the bash");
            for (int k = 0; k < FightRules.StaggerTicks && !sim.IsFinished; k++) sim.Tick();
            Assert.IsFalse(sim.IsStaggered(hit), "and not 2 s later");
        }

        // ------------------------------------------------------------ the war chest

        [Test]
        public void TheTabardKeepsItsPriceWithoutSynthesisDiscount()
        {
            Campaign c = Campaign.New(4600, new CampaignTuning());
            Assert.AreEqual(CampaignForge.TabardPrice, c.TabardPrice);
            Assert.AreEqual(CampaignForge.TabardPrice * (100 - RetargetRules.ForgeOffPercent) / 100, 33, "a third less is 33");
        }
    }
}
