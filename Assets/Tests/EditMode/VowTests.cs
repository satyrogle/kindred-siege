using System;
using System.Collections.Generic;
using System.Linq;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using NUnit.Framework;

namespace KindredSiege.Sim.Tests
{
    /// The Whisperer's vows (Jacob in chat, 2 Nov 2026: "whisperer - 1"; FightRules.Vows, campaign rules 30): her card offering a
    /// rank up with a vow, the save, and vows going off in the fight. One quick test per piece, only to prove each runs.
    public class VowTests
    {
        private static readonly FightRules Road29 = FightRules.Campaign.WithRoadKit().WithLayers().WithMarks().WithFloor().WithWhisper()
            .WithShadeForms().WithGlutton().WithDuos().WithSized().WithFlavours();
        private static readonly FightRules Road30 = Road29.WithVows();

        private static List<UnitSpec> Side(params UnitSpec[] units) => new List<UnitSpec>(units);

        private static UnitSpec U(string id, UnitClass cls, int rank = 1, Row? row = null)
        {
            var spec = new UnitSpec(id, cls, row ?? SliceClasses.Stats(cls).DefaultRow, UnitRole.Fighter, 100, 3000);
            return rank > 1 ? spec.WithRank(rank) : spec;
        }

        private static PalKit Vows(params (string id, StarVow vow)[] vows)
        {
            return new PalKit(null, 0).WithVows(vows.Select(v => new KeyValuePair<string, StarVow>(v.id, v.vow)).ToList());
        }

        // A road fight against these, who never fight back unless they can.
        private static BattleSimulation Fight(string id, PalKit kit, FightRules rules, List<UnitSpec> ours, List<UnitSpec> theirs)
        {
            return new BattleSimulation(new BattleSetup(id, 4, ours, theirs, new TacticsProfile(), new TacticsProfile(),
                BattleSetup.DefaultTickLimit, BattleSetup.DefaultDirectivePoints, NerveRules.On(NerveRules.Full, NerveRules.Full), 0, 400,
                new SideRules(0, 0, false).WithPals(kit), null, rules));
        }

        private static List<UnitSpec> Medics(int count, Row row = Row.Front)
        {
            var theirs = new List<UnitSpec>();
            for (int i = 1; i <= count; i++) theirs.Add(new UnitSpec("e" + i, UnitClass.Medic, row, UnitRole.Fighter, 100, 3000));
            return theirs;
        }

        private static bool Order(SimSession session, Directive directive, string target = "")
        {
            return session.TryIssue(Directives.CommandKind, "0", target, 0, 0, Directives.Name(directive), out _);
        }

        private static bool Seen(BattleSimulation sim, Func<BattleEvent, bool> match, int ticks = 6000, Action<SimSession> start = null)
        {
            var session = new SimSession(sim);
            start?.Invoke(session);
            while (!sim.IsFinished && sim.CurrentTick < ticks)
            {
                if (sim.Events.Any(match)) return true;
                session.Tick();
            }
            return sim.Events.Any(match);
        }

        [Test]
        public void RangerBouncesAndSniperShootsThroughToTheOneBehind()
        {
            Func<string, Func<BattleEvent, bool>> hit = d => e => e.Kind == BattleEventKind.Hit && e.Detail == d && e.ActorId == "m";
            BattleSimulation Ranger(FightRules rules) =>
                Fight("ranger", Vows(("m", StarVow.Ranger)), rules, Side(U("m", UnitClass.Marksman, 2), U("w", UnitClass.Warden)), Medics(5));
            Assert.IsTrue(Seen(Ranger(Road30), hit(VowRules.RangerDetail)), "no bounce");
            Assert.IsFalse(Seen(Ranger(Road29), hit(VowRules.RangerDetail)), "not before rules 30");

            var theirs = Medics(2);
            theirs.Add(new UnitSpec("back", UnitClass.Marksman, Row.Back, UnitRole.Fighter, 100, 3000));
            BattleSimulation sniper = Fight("sniper", Vows(("m", StarVow.Sniper)), Road30, Side(U("m", UnitClass.Marksman, 2), U("w", UnitClass.Warden)), theirs);
            // Focus Fire on a front row Medic, so the shot lands in front and goes through.
            Assert.IsTrue(Seen(sniper, e => hit(VowRules.SniperDetail)(e) && e.TargetId == "back", 6000, s => { s.Tick(); Order(s, Directive.FocusFire, "e1"); }),
                "nothing went through to the back row");
        }

        [Test]
        public void ADuelistCritsHerFocusTarget()
        {
            BattleSimulation duel = Fight("duel", Vows(("s", StarVow.Duelist)), Road30, Side(U("s", UnitClass.Skirmisher, 2), U("w", UnitClass.Warden)), Medics(5));
            Assert.IsTrue(Seen(duel, e => e.Kind == BattleEventKind.Cash && e.Detail == VowRules.DuelistDetail, 6000,
                s => { s.Tick(); Order(s, Directive.FocusFire, "e1"); }), "no Duelist crit on her Focus target");

        }

        [Test]
        public void HerCardOffersARankUpWithAVowAndItSavesAndPlays()
        {
            var easy = new CampaignTuning { EnemyScaleTier1 = 40 };
            bool seen = false;
            for (ulong seed = 6800; seed < 6860 && !seen; seed++)
            {
                Campaign c = Campaign.New(seed, easy);
                if (!CampaignPilot.PlayTutorial(c, PilotStyle.Reference, seed, 10)) continue;
                c.StartRun();
                Assert.IsTrue(c.VowsRun);
                int index = c.Offers.ToList().FindIndex(o => o.Kind != EncounterKind.Ambush && o.Node != null && o.Node.Pal == Pal.Whisperer &&
                    c.PalPickOffered(o.Node));
                if (index < 0) continue;
                EncounterResult r = CampaignPilot.PlayFight(c, c.StartEncounter(index), new CampaignPilot(PilotStyle.Reference, seed));
                if (!r.Won || c.FavourOffer == null) continue;
                int vowPick = c.FavourOffer.Picks.ToList().FindIndex(p => p.Vow != StarVow.None);
                if (vowPick < 0) continue;
                seen = true;
                WhisperPick pick = c.FavourOffer.Picks[vowPick];
                CampaignFighter f = c.Reserve.First(x => x.Id == pick.FighterId);
                Assert.AreEqual(f.Class, VowRules.ClassOf(pick.Vow), "a vow of his own class");
                Assert.AreEqual(1, f.Rank);

                string saved = CampaignSave.Write(c);
                Campaign back = CampaignSave.Read(saved, easy);
                Assert.IsEmpty(back.LoadNote);
                Assert.AreEqual(saved, CampaignSave.Write(back), "the vow on offer saves");
                Assert.IsTrue(back.TryTakeFavour(vowPick, out string why), why);
                CampaignFighter ranked = back.Reserve.First(x => x.Id == pick.FighterId);
                Assert.AreEqual(2, ranked.Rank);
                Assert.AreEqual(pick.Vow, ranked.Vow);

                string taken = CampaignSave.Write(back);
                Campaign again = CampaignSave.Read(taken, easy);
                Assert.IsEmpty(again.LoadNote);
                Assert.AreEqual(pick.Vow, again.Reserve.First(x => x.Id == pick.FighterId).Vow);
                Assert.AreEqual(taken, CampaignSave.Write(again));
                CampaignFight next = again.StartEncounter(0);
                Assert.IsTrue(next.Sim.VowsOn);
                Assert.AreEqual(pick.Vow, next.Setup.Rules(0).Pals.VowOf(pick.FighterId));
            }
            Assert.IsTrue(seen, "no card offered a vow");
        }

        [Test]
        public void BelowRules30VowsDoNothing()
        {
            var all = new[] { ("w", StarVow.Reprisal), ("m", StarVow.Ranger), ("s", StarVow.Duelist), ("t", StarVow.Netter), ("h", StarVow.Martyr) };
            string Played(PalKit kit)
            {
                var ours = Side(U("w", UnitClass.Warden, 2), U("m", UnitClass.Marksman, 2), U("s", UnitClass.Skirmisher, 2), U("t", UnitClass.Trapper, 2),
                    U("h", UnitClass.Medic, 2));
                var theirs = new List<UnitSpec> { U("x1", UnitClass.Skirmisher), U("x2", UnitClass.Skirmisher), U("x3", UnitClass.Marksman) };
                BattleSimulation sim = Fight("old", kit, Road29, ours, theirs);
                var session = new SimSession(sim);
                for (int n = 0; n < 600 && !sim.IsFinished; n++) session.Tick();
                return string.Join("\n", sim.Events.Select(e => e.Tick + " " + e.Kind + " " + e.ActorId + " " + e.TargetId + " " + e.Amount + " " + e.Detail));
            }
            Assert.AreEqual(Played(new PalKit(null, 0)), Played(Vows(all)));
        }

        [Test]
        public void WholeRunsPlayWithAVowOnEveryClass()
        {
            var easy = new CampaignTuning { EnemyScaleTier1 = 40 };
            var seen = new HashSet<string>();
            for (ulong seed = 6900; seed < 6904; seed++)
            {
                Campaign c = Campaign.New(seed, easy);
                Assert.IsTrue(CampaignPilot.PlayTutorial(c, PilotStyle.Reference, seed, 10), "the tutorial was never won");
                c.StartRun();
                bool second = seed % 2 == 1;
                foreach (CampaignFighter f in c.Reserve.ToList())
                {
                    StarVow vow = second ? VowRules.SecondOf(f.Class) : VowRules.FirstOf(f.Class);
                    Assert.IsTrue(c.TryRankUp(f.Id, vow));
                    if (seed >= 6902) Assert.IsTrue(c.TryRankUp(f.Id), "to rank 3");
                }
                var picker = new CampaignPilot(PilotStyle.Reference, seed);
                for (int n = 0; c.Stage == CampaignStage.InRun && n < 30; n++)
                {
                    CampaignFight fight = c.StartEncounter(picker.PickOffer(c));
                    Assert.IsTrue(fight.Sim.VowsOn);
                    CampaignPilot.PlayFight(c, fight, new CampaignPilot(PilotStyle.Reference, seed * 31UL + (ulong)n));
                    foreach (BattleEvent e in fight.Sim.Events)
                        if (e.Kind == BattleEventKind.Whisper && e.ActorSide == 0) seen.Add(e.Detail);
                    Assert.IsEmpty(CampaignSave.Read(CampaignSave.Write(c), easy).LoadNote, "the save reads back");
                }
            }
            TestContext.WriteLine("vows seen: " + string.Join(", ", seen.OrderBy(s => s)));
            Assert.GreaterOrEqual(seen.Count(d => d == VowRules.ReprisalDetail || d == VowRules.SniperDetail || d == VowRules.RangerDetail ||
                d == VowRules.ReaverDetail || d == VowRules.NetterDetail || d == VowRules.BleederDetail || d == VowRules.HierophantDetail), 5,
                "fewer than five vows went off in four runs");
        }
    }
}
