using System;
using System.Collections.Generic;
using System.Linq;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using NUnit.Framework;

namespace KindredSiege.Sim.Tests
{
    /// The Glutton (Jacob in chat, 1 Nov 2026: "go" on Claude's calls; FightRules.Glutton, campaign rules 29): his camp tree paid
    /// with codex points, his road card, Feed, and the talents in the fight. One quick test per piece, only to prove each runs
    /// (Jacob, 12 Oct 2026: build first). Nothing here is tuning.
    public class GluttonTests
    {
        private static readonly FightRules Chief26 =
            FightRules.CampaignChiefTactics.WithParryCenter().WithLayers().WithFloor().WithShadeForms().WithGlutton();
        private static readonly FightRules Chief25 =
            FightRules.CampaignChiefTactics.WithParryCenter().WithLayers().WithFloor().WithShadeForms();
        private static readonly FightRules Road26 =
            FightRules.Campaign.WithRoadKit().WithLayers().WithMarks().WithFloor().WithShadeForms().WithGlutton();
        private static readonly FightRules Road25 = FightRules.Campaign.WithRoadKit().WithLayers().WithMarks().WithFloor().WithShadeForms();

        private static List<UnitSpec> Side(params UnitSpec[] units) => new List<UnitSpec>(units);

        private static UnitSpec U(string id, UnitClass unitClass, int health = 3000)
        {
            return new UnitSpec(id, unitClass, SliceClasses.Stats(unitClass).DefaultRow, UnitRole.Fighter, 100, health);
        }

        // A kit holding these talents (and this Shade route at its first form), the first one fed if fed is true.
        private static PalKit Kit(ShadeRoute route, bool fed, params GluttonTalent[] talents)
        {
            var routes = new List<KeyValuePair<ShadeRoute, int>>();
            if (route != ShadeRoute.None) routes.Add(new KeyValuePair<ShadeRoute, int>(route, 1));
            var levels = new int[GluttonRules.TalentCount + 1];
            for (int i = 0; i < talents.Length; i++) levels[(int)talents[i]] = fed && i == 0 ? GluttonRules.Fed : GluttonRules.Held;
            return new PalKit(routes, 0).WithTalents(levels);
        }

        private static PalKit Kit(params GluttonTalent[] talents) => Kit(ShadeRoute.None, false, talents);

        // The Bloodridge chief with only his Cleave beside one fighter of his, against your Pal and three (this one in the last
        // place, a Trapper unless said).
        private static BattleSimulation Cleaver(string id, PalKit kit, FightRules rules, UnitSpec? last = null)
        {
            var ours = Side(new UnitSpec("pal", UnitClass.Marksman, Row.Back, UnitRole.Pal, 100, 3000),
                U("s", UnitClass.Skirmisher), U("m", UnitClass.Marksman), last ?? U("t", UnitClass.Trapper));
            return new BattleSimulation(new BattleSetup(id, 11, ours,
                Side(U("x.s", UnitClass.Skirmisher), new UnitSpec("chief", UnitClass.Warden, Row.Front, UnitRole.Chief, 100, 8000)),
                new TacticsProfile(), new TacticsProfile(), BattleSetup.DefaultTickLimit, BattleSetup.DefaultDirectivePoints,
                NerveRules.On(NerveRules.Full, NerveRules.Full), 10, 400, new SideRules(1, 0, true).WithPals(kit),
                new SideRules(2, 0, false, 0, ChiefSpell.Eclipse, kit: ChiefKit.Bloodridge, moves: ChiefMove.Cleave), rules));
        }

        // A road fight against a row of five of their Medics, m1 to m5, who never fight back.
        private static BattleSimulation MedicRow(string id, PalKit kit, FightRules rules, List<UnitSpec> ours = null)
        {
            var theirs = new List<UnitSpec>();
            for (int i = 1; i <= 5; i++) theirs.Add(new UnitSpec("m" + i, UnitClass.Medic, Row.Front, UnitRole.Fighter, 100, 3000));
            return new BattleSimulation(new BattleSetup(id, 4, ours ?? Side(U("s", UnitClass.Skirmisher), U("m", UnitClass.Marksman), U("w", UnitClass.Warden)),
                theirs, new TacticsProfile(), new TacticsProfile(), BattleSetup.DefaultTickLimit, BattleSetup.DefaultDirectivePoints,
                NerveRules.On(NerveRules.Full, NerveRules.Full), 0, 400, new SideRules(0, 0, false).WithPals(kit), null, rules));
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

        private static Campaign Ready(ulong seed, CampaignTuning tuning)
        {
            Campaign c = Campaign.New(seed, tuning);
            Assert.IsTrue(CampaignPilot.PlayTutorial(c, PilotStyle.Reference, seed, 10), "the tutorial was never won");
            return c;
        }

        // The same campaign with these codex entries added, through a save.
        private static Campaign WithCodex(Campaign c, CampaignTuning tuning, params string[] entries)
        {
            var lines = CampaignSave.Write(c).Split('\n').ToList();
            int at = lines.FindLastIndex(l => l.StartsWith("codex\t", StringComparison.Ordinal));
            foreach (string e in entries) lines.Insert(++at, "codex\t" + e);
            Campaign back = CampaignSave.Read(string.Join("\n", lines), tuning);
            Assert.IsEmpty(back.LoadNote);
            return back;
        }

        // ------------------------------------------------------------ the camp tree

        [Test]
        public void TheCodexPaysTalentPointsAndTheTreeIsBoughtInOrderAtCamp()
        {
            var easy = new CampaignTuning { EnemyScaleTier1 = 40 };
            Campaign c = Ready(6400, easy);
            Assert.IsTrue(c.GluttonRun);
            int earned = c.TalentPointsEarned;
            Assert.AreEqual(c.Codex.Count(e => e.StartsWith("Tribe: ") || e.StartsWith("Joined: ") || e.StartsWith("Chief met: ") ||
                e.StartsWith("Hidden chain of ") || e.StartsWith("Beast: ")), earned, "one point a first");
            // Enough points and the campaign's tiers through the codex, as the road would give them.
            c = WithCodex(c, easy, "Tribe: Test One", "Tribe: Test Two", "Tribe: Test Three", "Joined: Test One", "Joined: Test Two",
                "Joined: Test Three", "Beast: Test", "Chief met: Test", "Chief met: Test Two", "Chief met: Test Three");
            Assert.AreEqual(earned + 10, c.TalentPointsEarned);

            Assert.IsFalse(c.TryBuyTalent(GluttonTalent.Strip, out string why), "tier 2 before tier 1");
            StringAssert.StartsWith("Needs Nucleus Edge first", why);
            Assert.IsTrue(c.TryBuyTalent(GluttonTalent.NucleusEdge, out why), why);
            Assert.IsTrue(c.TryBuyTalent(GluttonTalent.Strip, out why), why);
            int tier = c.ReachedTier;
            bool opened = c.TryBuyTalent(GluttonTalent.FoldStep, out why);
            Assert.AreEqual(tier >= 2, opened, why);
            if (opened) Assert.IsFalse(c.TryBuyTalent(GluttonTalent.EchoStrike, out why), "one of the tier 3 pair");
            Assert.AreEqual(earned + 10 - (1 + 2 + (opened ? 3 : 0)), c.TalentPoints);

            // Kept between runs, bought only at camp, and reset for free.
            Campaign back = CampaignSave.Read(CampaignSave.Write(c), easy);
            Assert.IsEmpty(back.LoadNote);
            CollectionAssert.AreEqual(c.OwnedTalents, back.OwnedTalents);
            back.StartRun();
            Assert.IsFalse(back.TryBuyTalent(GluttonTalent.DarkMatterPlating, out why));
            Assert.AreEqual("Only at camp.", why);
            Assert.AreEqual(GluttonRules.Held, back.TalentLevel(GluttonTalent.NucleusEdge), "an owned talent rides into the run");
            Assert.IsTrue(c.TryResetTree());
            Assert.AreEqual(earned + 10, c.TalentPoints);
        }

        // ------------------------------------------------------------ the road card and Feed

        [Test]
        public void HisCardLendsTheNextTalentOfEachLimbAndATierThreeBringsItsKeystone()
        {
            var easy = new CampaignTuning { EnemyScaleTier1 = 40 };
            bool seen = false;
            for (ulong seed = 6500; seed < 6560 && !seen; seed++)
            {
                Campaign c = Ready(seed, easy);
                c.StartRun();
                // The strikers climbed to tier 2 already, and Node Cache makes every card one bigger.
                Assert.IsTrue(c.TryLendTalent(GluttonTalent.NucleusEdge));
                Assert.IsTrue(c.TryLendTalent(GluttonTalent.Strip));
                Assert.IsTrue(c.TryLendTalent(GluttonTalent.NodeCache));
                Assert.AreEqual(FavourRules.Offered + GluttonRules.CacheMore, c.OfferSize);
                int index = c.Offers.ToList().FindIndex(o => o.Kind != EncounterKind.Ambush && o.Node != null && o.Node.Pal == Pal.Glutton);
                if (index < 0) continue;
                EncounterResult r = CampaignPilot.PlayFight(c, c.StartEncounter(index), new CampaignPilot(PilotStyle.Reference, seed));
                if (!r.Won || c.FavourOffer == null) continue;
                seen = true;
                IReadOnlyList<Favour> choices = c.FavourOffer.Choices;
                Assert.AreEqual(Pal.Glutton, c.FavourOffer.Pal);
                Assert.AreEqual(c.OfferSize, choices.Count, "Node Cache's bigger card");
                foreach (Favour f in choices)
                {
                    GluttonTalent t = FavourRules.TalentOf(f);
                    Assert.IsFalse(c.HoldsTalent(t), "never one you hold");
                    if (GluttonRules.LimbOf(t) == 1) Assert.AreEqual(3, GluttonRules.TierOf(t), "the strikers climb to tier 3");
                    else if (GluttonRules.LimbOf(t) != 6) Assert.AreEqual(1, GluttonRules.TierOf(t), "a limb not picked starts at tier 1");
                }

                // Saved with his card standing, it comes back the same; a tier 3 pick brings the keystone.
                string saved = CampaignSave.Write(c);
                Campaign back = CampaignSave.Read(saved, easy);
                Assert.IsEmpty(back.LoadNote);
                Assert.AreEqual(saved, CampaignSave.Write(back));
                int third = choices.ToList().FindIndex(f => GluttonRules.TierOf(FavourRules.TalentOf(f)) == 3);
                if (third >= 0)
                {
                    Assert.IsTrue(back.TryTakeFavour(third, out string why), why);
                    Assert.IsTrue(back.HoldsTalent(FavourRules.TalentOf(choices[third])));
                    Assert.IsTrue(back.HoldsTalent(GluttonTalent.AnswerTheParry), "the keystone came with it");
                }
                Assert.AreNotEqual(0, back.HeldPals() & (1 << (int)Pal.Glutton), "his talents count as his picks on the road");
            }
            Assert.IsTrue(seen, "no Glutton fight was won");
        }

        [Test]
        public void FeedDoublesOneTalentAndEatsAnotherOfItsLimb()
        {
            var easy = new CampaignTuning { EnemyScaleTier1 = 40 };
            Campaign c = Ready(6600, easy);
            c.StartRun();
            Assert.IsTrue(c.TryLendTalent(GluttonTalent.NucleusEdge));
            Assert.IsTrue(c.TryLendTalent(GluttonTalent.Strip));
            Assert.IsTrue(c.TryLendTalent(GluttonTalent.MatterPrinter));
            Assert.IsFalse(c.TryFeed(GluttonTalent.NucleusEdge, GluttonTalent.MatterPrinter, out string why), "one of the same limb");
            Assert.IsTrue(c.TryFeed(GluttonTalent.NucleusEdge, GluttonTalent.Strip, out why), why);
            Assert.AreEqual(GluttonRules.Fed, c.TalentLevel(GluttonTalent.NucleusEdge));
            Assert.AreEqual(0, c.TalentLevel(GluttonTalent.Strip), "he ate it for the run");
            Assert.IsFalse(c.TryFeed(GluttonTalent.MatterPrinter, GluttonTalent.None, out why), "once a run");
            Campaign back = CampaignSave.Read(CampaignSave.Write(c), easy);
            Assert.IsEmpty(back.LoadNote);
            Assert.AreEqual(GluttonTalent.NucleusEdge, back.FedTalent);
            Assert.AreEqual(GluttonTalent.Strip, back.EatenTalent);
        }

        // ------------------------------------------------------------ the talents in the fight

        [Test]
        public void BranchAnchorThickensFrontRowShieldsAndCommandRelayHalvesSanctuarysPrice()
        {
            int ShieldOn(string id, PalKit kit, out int stagger)
            {
                BattleSimulation sim = Cleaver(id, kit, Chief26);
                HoldBefore(sim, new SimSession(sim), 1);
                stagger = sim.Events.First(e => e.Kind == BattleEventKind.Parry && e.ActorSide == 0).Amount;
                return sim.FindById("s").Statuses[StatusKind.Shield].Size;
            }
            int plain = ShieldOn("anchor", Kit(ShadeRoute.Sanctuary, false), out int noStagger);
            int anchored = ShieldOn("anchor", Kit(ShadeRoute.Sanctuary, false, GluttonTalent.BranchAnchor, GluttonTalent.CommandRelay), out int half);
            Assert.AreEqual(plain * 2, anchored, "twice as thick on the front row");
            Assert.AreEqual(0, noStagger);
            Assert.AreEqual(FightRules.StaggerTicks / 2, half, "half of Sanctuary's price");
        }

        [Test]
        public void PrecognitionWidensTheParryWindowAndStripDoublesTheOpening()
        {
            bool Parried(PalKit kit, out BattleSimulation sim)
            {
                sim = Cleaver("window", kit, Chief26);
                HoldBefore(sim, new SimSession(sim), 20);
                return sim.Events.Any(e => e.Kind == BattleEventKind.Parry && e.ActorSide == 0);
            }
            Assert.IsFalse(Parried(Kit(), out _), "20 ticks early is outside the plain window");
            Assert.IsTrue(Parried(Kit(GluttonTalent.Precognition, GluttonTalent.Strip), out BattleSimulation wide), "inside a wider one");
            Assert.AreEqual(ClassSpells.OpeningTicks * 2 - 1, wide.FindById("chief").Statuses[StatusKind.Opening].Ticks, "Strip's longer Opening");
        }

        [Test]
        public void TheStrikersShootersAndFrontLineTalentsChangeTheirHits()
        {
            // The same fight with and without each talent: the first hit it touches.
            int FirstHit(PalKit kit, Func<BattleEvent, bool> match, Action<SimSession> start = null)
            {
                BattleSimulation sim = Cleaver("hits", kit, Chief26);
                var session = new SimSession(sim);
                start?.Invoke(session);
                BattleEvent e = First(sim, session, match);
                Assert.AreEqual(BattleEventKind.Hit, e.Kind, "no such hit");
                return e.Amount;
            }
            Func<BattleEvent, bool> bySkirmisher = e => e.Kind == BattleEventKind.Hit && e.ActorId == "s";
            Assert.Greater(FirstHit(Kit(GluttonTalent.NucleusEdge), bySkirmisher), FirstHit(Kit(), bySkirmisher), "Nucleus Edge");
            Assert.Greater(FirstHit(Kit(ShadeRoute.None, true, GluttonTalent.NucleusEdge), bySkirmisher), FirstHit(Kit(GluttonTalent.NucleusEdge), bySkirmisher),
                "fed, more");

            Func<BattleEvent, bool> marksmanOnChief = e => e.Kind == BattleEventKind.Hit && e.ActorId == "m" && e.TargetId == "chief";
            Action<SimSession> focus = s => { s.Tick(); Order(s, Directive.FocusFire, "chief"); };
            Assert.Greater(FirstHit(Kit(GluttonTalent.NothingWasted), marksmanOnChief, focus), FirstHit(Kit(), marksmanOnChief, focus), "Nothing Wasted");

            Func<BattleEvent, bool> onFrontRow = e => e.Kind == BattleEventKind.Hit && e.ActorSide == 1 && e.TargetId == "s";
            Assert.Less(FirstHit(Kit(GluttonTalent.DarkMatterPlating), onFrontRow), FirstHit(Kit(), onFrontRow), "Dark Matter Plating");
        }

        [Test]
        public void DeepNodeMakesTheShadesTricksHitHarder()
        {
            int Blast(PalKit kit)
            {
                BattleSimulation sim = Cleaver("node", kit, Chief26);
                var session = new SimSession(sim);
                session.Tick();
                Assert.IsTrue(Order(session, Directive.FallBack));
                return First(sim, session, e => e.Kind == BattleEventKind.Hit && e.Detail == ShadeRules.MineDetail).Amount;
            }
            int plain = Blast(Kit(ShadeRoute.RiftMine, false));
            int deep = Blast(Kit(ShadeRoute.RiftMine, false, GluttonTalent.DeepNode));
            Assert.Greater(deep, plain);
            Assert.AreEqual(plain * (100 + GluttonRules.DeepNodePercent) / 100, deep, 1);
        }

        [Test]
        public void MatterPrinterMakesHealsBigger()
        {
            int FirstHeal(PalKit kit)
            {
                BattleSimulation sim = Cleaver("heal", kit, Chief26, U("h", UnitClass.Medic));
                return First(sim, new SimSession(sim), e => e.Kind == BattleEventKind.Heal && e.ActorId == "h" && e.Amount > 0, 6000).Amount;
            }
            int plain = FirstHeal(Kit());
            Assert.Greater(plain, 0, "she never healed");
            Assert.Greater(FirstHeal(Kit(GluttonTalent.MatterPrinter)), plain);
        }

        [Test]
        public void SynthesisDiscountAndFoldShieldCutTheButchersPacts()
        {
            // Borrowed time, the Time Stop's pact, on a Marksman wearing Stopped Bullets: an eighth of his health, a third less of it.
            UnitSpec stopped = new UnitSpec("m", UnitClass.Marksman).WithBend(ClassBend.StoppedBullets);
            int Health(PalKit kit) => MedicRow("pact", kit, Road26, Side(stopped, U("w", UnitClass.Warden))).FindById("m").MaxHp;
            int plain = Health(Kit());
            int discounted = Health(Kit(GluttonTalent.SynthesisDiscount));
            Assert.Greater(discounted, plain);

            // Unnerving, the Fold Door's pact, on a front row Skirmisher wearing Execution Corridor: half of it with Fold Shield.
            UnitSpec door = new UnitSpec("s", UnitClass.Skirmisher).WithBend(ClassBend.ExecutionCorridor);
            int Nerve(PalKit kit) => MedicRow("door", kit, Road26, Side(door, U("w", UnitClass.Warden))).FindById("s").UnitNerve;
            int lost = RoadRules.UnitNerve - Nerve(Kit());
            Assert.Greater(lost, 0, "Unnerving took nerve");
            Assert.AreEqual(RoadRules.UnitNerve - lost / 2, Nerve(Kit(GluttonTalent.FoldShield)), 1, "half the nerve");
        }

        [Test]
        public void BelowRules29TalentsDoNothing()
        {
            // A kit holding every talent, fed and not, plays exactly as an empty one under rules 28.
            var all = Enumerable.Range(1, GluttonRules.TalentCount).Select(i => (GluttonTalent)i).ToArray();
            string Played(PalKit kit)
            {
                BattleSimulation sim = MedicRow("old", kit, Road25);
                var session = new SimSession(sim);
                for (int n = 0; n < 20 && !sim.IsFinished; n++)
                {
                    if (n % 4 == 0) Order(session, Directive.FallBack);
                    else if (n % 4 == 1 && sim.FindById("m3").Alive) Order(session, Directive.FocusFire, "m3");
                    else if (n % 4 == 2) Order(session, Directive.Unleash);
                    for (int k = 0; k < 40 && !sim.IsFinished; k++) session.Tick();
                }
                return string.Join("\n", sim.Events.Select(e => e.Tick + " " + e.Kind + " " + e.ActorId + " " + e.TargetId + " " + e.Amount + " " + e.Detail));
            }
            Assert.AreEqual(Played(Kit()), Played(Kit(ShadeRoute.None, true, all)));
        }

        [Test]
        public void WholeRunsPlayWithTalentsInEveryLimb()
        {
            var easy = new CampaignTuning { EnemyScaleTier1 = 40 };
            for (int p = 0; p < 2; p++)
            {
                ulong seed = 6700UL + (ulong)p;
                Campaign c = Ready(seed, easy);
                c.StartRun();
                // Every limb lent to its keystone, one tier 3 of each pair, alternating.
                for (int limb = 0; limb < GluttonRules.LimbCount; limb++)
                {
                    Assert.IsTrue(c.TryLendTalent(GluttonRules.At(limb, 0)));
                    Assert.IsTrue(c.TryLendTalent(GluttonRules.At(limb, 1)));
                    Assert.IsTrue(c.TryLendTalent(GluttonRules.At(limb, 2 + (limb + p) % 2)));
                    Assert.IsTrue(c.TryLendTalent(GluttonRules.At(limb, 4)));
                }
                Assert.IsTrue(c.TryFeed(GluttonTalent.NucleusEdge, GluttonTalent.Strip, out string why), why);
                var picker = new CampaignPilot(PilotStyle.Reference, seed);
                for (int n = 0; c.Stage == CampaignStage.InRun && n < 30; n++)
                {
                    CampaignFight fight = c.StartEncounter(picker.PickOffer(c));
                    Assert.IsTrue(fight.Sim.GluttonOn);
                    CampaignPilot.PlayFight(c, fight, new CampaignPilot(PilotStyle.Reference, seed * 31UL + (ulong)n));
                    Assert.IsEmpty(CampaignSave.Read(CampaignSave.Write(c), easy).LoadNote, "the save reads back");
                }
            }
        }
    }
}
