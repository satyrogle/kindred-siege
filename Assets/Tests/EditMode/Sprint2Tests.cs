using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using KindredSiege.Sim;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;

namespace KindredSiege.Sim.Tests
{
    /// Sprint 2 of the design Jacob chose on 12 and 13 Oct 2026 (his go, 13 Oct: "go start sprint 2"; DESIGN_LOCKS,
    /// "Sprint 2 calls"): capture or kill the chief, your roster as your lives, strip or bank, gear tiers, quirks,
    /// tribe sets and formation. Jacob's rule 8 is build first, tune later, so each rule gets one quick test that
    /// proves it runs. None of these is balance.
    public sealed class Sprint2Tests
    {
        private static Campaign Ready(ulong seed, CampaignTuning tuning)
        {
            Campaign c = Campaign.New(seed, tuning);
            Assert.IsTrue(CampaignPilot.PlayTutorial(c, PilotStyle.Reference, seed, 10), "the tutorial was never won");
            return c;
        }

        // ------------------------------------------------------------ the run

        [Test]
        public void ALostFightCostsTheFallenAndOneCapturedAndYouTryAgain()
        {
            // The 13 Oct roster as lives, which rules 11 replaced and old saves still play.
            var hard = new CampaignTuning { EnemyScaleTier1 = 300, RulesCap = Campaign.RaidChiefRules };
            for (ulong seed = 3300; seed < 3320; seed++)
            {
                Campaign c = CampaignSave.Read(CampaignSave.Write(Ready(seed, new CampaignTuning())), hard);
                c.StartRun();
                int step = c.Step;
                for (int tries = 0; tries < 12 && c.Stage == CampaignStage.InRun; tries++)
                {
                    List<CampaignFighter> before = c.Reserve.ToList();
                    EncounterResult r = CampaignPilot.PlayFight(c, c.StartEncounter(0), new CampaignPilot(PilotStyle.Passive, seed + (ulong)tries));
                    if (r.Won) break;
                    Assert.IsNotNull(r.Captured, "giving no orders, someone of yours still stood to be taken");
                    CollectionAssert.Contains(before, r.Captured);
                    Assert.IsNotEmpty(r.Captured.Name, "taken by name");
                    foreach (CampaignFighter f in r.Fallen) CollectionAssert.DoesNotContain(c.Reserve, f);
                    CollectionAssert.DoesNotContain(c.Reserve, r.Captured);
                    Assert.AreEqual(before.Count - r.Fallen.Count - 1, r.FightersLeft);
                    if (r.RunOver)
                    {
                        Assert.Less(r.FightersLeft, Campaign.MinSquad, "the run ends when you cannot field a squad of 3");
                        Assert.AreEqual(CampaignStage.ChooseTribe, c.Stage);
                        Assert.AreEqual(0, c.Gear.Count, "run gear goes with the run");
                        return;
                    }
                    Assert.GreaterOrEqual(r.FightersLeft, Campaign.MinSquad);
                    Assert.AreEqual(CampaignStage.InRun, c.Stage, "the run goes on");
                    Assert.AreEqual(step, c.Step, "a lost fight does not move the run on");
                    Assert.Greater(c.Offers.Count, 0, "fresh fights at the same step");
                }
            }
            Assert.Fail("giving no orders against a tribe three times as strong never ran out of fighters");
        }

        [Test]
        public void ACapturedChiefBringsHisWholeBandAndHisOrder()
        {
            var t = new CampaignTuning { PathFights = 0, ChiefHealthPercent = 3000, EnemyScaleTier1 = 40 };
            for (ulong seed = 3100; seed < 3120; seed++)
            {
                Campaign c = Ready(seed, t);
                // A run can start at either tier 1 tribe since 18 Oct 2026; this is about Bloodridge's chief.
                c.ChooseTribe(CampaignTribes.BloodridgeId);
                Assert.AreEqual(EncounterKind.Chief, c.Offers[0].Kind);
                EncounterResult r = CampaignPilot.PlayFight(c, c.StartEncounter(0), new CampaignPilot(PilotStyle.Reference, seed));
                if (!r.Won || !r.ChiefCaptured) continue;
                Assert.IsTrue(r.Joined);
                Assert.IsFalse(r.ChiefKilled);
                Assert.IsNull(r.PrincessTaken);
                Assert.AreEqual(t.ChiefFighters, r.JoinedFighters.Count, "his whole band joins, fallen ones too");
                Assert.IsNotNull(r.AssistUnlocked, "with his order");
                Assert.IsTrue(c.HasJoined(CampaignTribes.BloodridgeId));
                foreach (CampaignFighter f in r.JoinedFighters)
                {
                    Assert.AreEqual(CampaignTribes.BloodridgeId, f.TribeId);
                    CollectionAssert.Contains(c.Reserve, f);
                }
                return;
            }
            Assert.Fail("no chief was captured in twenty campaigns");
        }

        [Test]
        public void AKilledChiefLeavesHisPrincessPieceAndNoTribe()
        {
            var t = new CampaignTuning { PathFights = 0, ChiefHealthPercent = 1 };
            for (ulong seed = 3200; seed < 3220; seed++)
            {
                Campaign c = Ready(seed, t);
                // A run can start at either tier 1 tribe since 18 Oct 2026; this is about Bloodridge's chief.
                c.ChooseTribe(CampaignTribes.BloodridgeId);
                EncounterResult r = CampaignPilot.PlayFight(c, c.StartEncounter(0), new CampaignPilot(PilotStyle.Reference, seed));
                if (!r.Won || !r.ChiefKilled) continue;
                Assert.IsFalse(r.Joined);
                Assert.IsFalse(r.ChiefCaptured);
                Assert.AreEqual(0, r.JoinedFighters.Count);
                Assert.IsNull(r.AssistUnlocked);
                Assert.IsFalse(c.HasJoined(CampaignTribes.BloodridgeId));
                CampaignGear piece = r.PrincessTaken;
                Assert.AreEqual(GearTier.Princess, piece.Tier);
                Assert.AreEqual(GearSpecial.KillsBreakNerve, piece.Special);
                StringAssert.StartsWith(c.ChiefOf(CampaignTribes.BloodridgeId).Name, piece.Name, "named for the chief who wore it");
                CollectionAssert.Contains(c.Gear, piece);

                // Worn into the next fight, its wearer's kills break more nerve.
                CampaignFighter wearer = c.Reserve[0];
                Assert.IsTrue(c.TryEquip(wearer.Id, piece.Id, out string why), why);
                Assert.AreSame(piece, c.GearOf(wearer.Id));
                CampaignFight next = c.StartEncounter(0, new[] { wearer.Id });
                Assert.AreEqual(CampaignGearRules.KillNerve, next.Setup.Side(0).First(u => u.Id == wearer.Id).KillNerve);
                return;
            }
            Assert.Fail("no chief was killed in twenty campaigns");
        }

        [Test]
        public void AWonPathFightBanksItsMaterialsOrSwapsThemForOnePiece()
        {
            var easy = new CampaignTuning { EnemyScaleTier1 = 40 };
            for (ulong seed = 3400; seed < 3420; seed++)
            {
                Campaign c = Ready(seed, easy);
                c.StartRun();
                int before = c.Materials;
                // A favour fight drops a Pal's favour instead of gear (18 Oct 2026), so this takes a gear fight, one with no
                // bonus goal so its drop is its kind's. From rules 19 every road fight drops gear beside its Pal's picks (fix 5).
                int gear = c.Offers.ToList().FindIndex(o => o.Node != null && (o.Node.Pal == Pal.None || c.PalRoadRun) && o.Objective == FightObjective.None &&
                    o.Kind != EncounterKind.Beast);
                if (gear < 0) continue;
                EncounterResult r = CampaignPilot.PlayFight(c, c.StartEncounter(gear), new CampaignPilot(PilotStyle.Reference, seed));
                if (!r.Won) continue;
                Assert.AreEqual(before + r.MaterialsEarned, c.Materials, "banked");
                CampaignGear piece = r.SpoilsOffered;
                Assert.IsNotNull(piece);
                Assert.AreSame(piece, c.Spoils);
                Assert.AreEqual(CampaignGearRules.DropTier(r.Encounter.Kind), piece.Tier);
                Assert.AreEqual(r.MaterialsEarned, c.SpoilsMaterials);
                Assert.IsTrue(c.TryTakeSpoils(out string why), why);
                Assert.AreEqual(before, c.Materials, "taking the piece gives the materials back");
                CollectionAssert.Contains(c.Gear, piece);
                Assert.IsNull(c.Spoils);
                Assert.IsFalse(c.TryTakeSpoils(out _), "only once");

                // Not taken, the offer goes once the next fight starts.
                gear = GearFight(c);
                if (gear < 0 || !CampaignPilot.PlayFight(c, c.StartEncounter(gear), new CampaignPilot(PilotStyle.Reference, seed + 1)).Won) continue;
                Assert.IsNotNull(c.Spoils);
                c.StartEncounter(0);
                Assert.IsNull(c.Spoils, "gone once the next fight starts");
                Assert.IsFalse(c.TryTakeSpoils(out _));
                return;
            }
            Assert.Fail("the reference pilot never won two path fights in twenty campaigns");
        }

        [Test]
        public void QuirksSetsAndRowsShapeYourFighters()
        {
            Campaign c = Ready(3500, new CampaignTuning());
            foreach (CampaignFighter f in c.Reserve)
            {
                Assert.AreNotEqual(Quirk.None, f.Quirk, f.Name + " has a quirk");
                Assert.AreEqual(Quirks.For(c.Seed, f.Id), f.Quirk, "the same quirk every time");
            }
            c.StartRun();
            CampaignFighter medic = c.Reserve.First(f => f.Class == UnitClass.Medic);
            Assert.IsTrue(c.TrySetRow(medic.Id, Row.Front, out string why), why);
            int index = c.Offers.ToList().FindIndex(o => o.SquadSize >= 5);
            CampaignFight fight = c.StartEncounter(index);
            Assert.AreEqual(Row.Front, fight.Setup.Side(0).First(u => u.Id == medic.Id).Row, "formation: the medic stands in front");
            foreach (CampaignFighter f in fight.Squad)
            {
                UnitSpec spec = fight.Setup.Side(0).First(u => u.Id == f.Id);
                Assert.AreEqual(f.Row, spec.Row);
                int avoid = TribeTraits.AvoidBonus(f.TribeId) + TribeSets.ClarthAvoid + (f.Quirk == Quirk.Quick ? Quirks.QuickAvoid : 0);
                Assert.AreEqual(avoid, spec.AvoidBonus, f.Name + ": the Clarth set and their quirk");
                Assert.AreEqual(f.Quirk == Quirk.Steady ? Quirks.SteadyRise : 0, spec.RiseBonus, f.Name);
            }
            Assert.GreaterOrEqual(fight.Setup.CopyTactics(0).PickOffBack, BattleSimulation.HabitThreshold, "three Clarth bring their doctrine");
        }

        [Test]
        public void AGrudgeHitsTheTribeThatCapturedOneOfYoursHarderUntilTheRunEnds()
        {
            // Jacob, 13 Oct 2026: "yes go grudge on the tribe that captured". Once a tribe captures one of yours, your
            // Grudge fighters, the Clarth too, do Quirks.GrudgePercent more damage on that tribe for the rest of the run.
            // The 13 Oct roster as lives, which rules 11 replaced and old saves still play.
            var hard = new CampaignTuning { EnemyScaleTier1 = 300, RulesCap = Campaign.RaidChiefRules };
            for (ulong seed = 3700; seed < 3760; seed++)
            {
                Campaign c = CampaignSave.Read(CampaignSave.Write(Ready(seed, new CampaignTuning())), hard);
                c.StartRun();
                CampaignFighter grudge = c.Reserve.FirstOrDefault(f => f.Quirk == Quirk.Grudge);
                CampaignFighter plain = c.Reserve.FirstOrDefault(f => f.Quirk != Quirk.Grudge && f.Quirk != Quirk.Brute);
                int offer = TwoOrMore(c);
                if (grudge == null || plain == null || offer < 0) continue;
                CampaignFight first = c.StartEncounter(offer, SquadWith(c, offer, grudge, plain));
                Assert.AreEqual(PowerOf(first, plain), PowerOf(first, grudge), "no grudge before anyone is captured");
                EncounterResult lost = CampaignPilot.PlayFight(c, first, new CampaignPilot(PilotStyle.Passive, seed));
                if (lost.Won || lost.Captured == null || lost.RunOver) continue;
                // Whoever of each kind still stands after the loss (since the three squads, 18 Oct 2026, the first two often
                // fall in it).
                grudge = c.Reserve.FirstOrDefault(f => f.Quirk == Quirk.Grudge);
                plain = c.Reserve.FirstOrDefault(f => f.Quirk != Quirk.Grudge && f.Quirk != Quirk.Brute);
                if (grudge == null || plain == null) continue;
                CollectionAssert.AreEqual(new[] { c.Target.Id }, c.Grudges, "the tribe that captured one of yours");

                offer = TwoOrMore(c);
                if (offer < 0) continue;
                CampaignFight again = c.StartEncounter(offer, SquadWith(c, offer, grudge, plain));
                Assert.AreEqual(PowerOf(again, plain) * (100 + Quirks.GrudgePercent) / 100, PowerOf(again, grudge),
                    "the grudge hits them harder");

                // Saved part way through that fight, the grudge comes back and the fight plays back the same.
                for (int t = 0; t < 40 && !again.Over; t++) again.Tick();
                string saved = CampaignSave.Write(c);
                Campaign read = CampaignSave.Read(saved, hard);
                Assert.IsEmpty(read.LoadNote, "the fight plays back the same, grudge and all");
                CollectionAssert.AreEqual(c.Grudges, read.Grudges);
                Assert.AreEqual(saved, CampaignSave.Write(read));

                // Lose until the run ends: its grudges go with it.
                CampaignPilot.PlayFight(c, again, new CampaignPilot(PilotStyle.Passive, seed + 1));
                for (int tries = 0; tries < 12 && c.Stage == CampaignStage.InRun; tries++)
                    CampaignPilot.PlayFight(c, c.StartEncounter(0), new CampaignPilot(PilotStyle.Passive, seed + 2 + (ulong)tries));
                if (c.Stage != CampaignStage.ChooseTribe) continue;
                Assert.AreEqual(0, c.Grudges.Count, "grudges go with the run");
                return;
            }
            Assert.Fail("no Grudge fighter came through a lost fight in sixty campaigns");
        }

        // The first fight on offer that drops gear, not a Pal's favour (18 Oct 2026), or -1. From rules 19 every road fight
        // drops gear beside its Pal's picks (fix 5).
        private static int GearFight(Campaign c)
        {
            return c.Offers.ToList().FindIndex(o => o.Node == null || o.Node.Pal == Pal.None || c.PalRoadRun);
        }

        // The first fight on offer that fields two or more of yours, or -1.
        private static int TwoOrMore(Campaign c)
        {
            return c.Offers.ToList().FindIndex(o => o.SquadSize >= 2);
        }

        // These fighters first, then the reserve in order, up to the fight's squad size.
        private static string[] SquadWith(Campaign c, int offer, params CampaignFighter[] first)
        {
            List<string> ids = first.Select(f => f.Id).ToList();
            foreach (CampaignFighter f in c.Reserve)
                if (ids.Count < c.Offers[offer].SquadSize && !ids.Contains(f.Id)) ids.Add(f.Id);
            return ids.ToArray();
        }

        private static int PowerOf(CampaignFight fight, CampaignFighter f)
        {
            return fight.Setup.Side(0).First(u => u.Id == f.Id).PowerPercent;
        }

        [Test]
        public void TheSaveKeepsRowsGearAndTheSpoilsOnOffer()
        {
            var easy = new CampaignTuning { EnemyScaleTier1 = 40 };
            for (ulong seed = 3600; seed < 3620; seed++)
            {
                Campaign c = Ready(seed, easy);
                c.StartRun();
                // A favour fight drops no gear (18 Oct 2026): take a gear fight.
                int gear = c.Offers.ToList().FindIndex(o => o.Node == null || o.Node.Pal == Pal.None || c.PalRoadRun);
                if (gear < 0 || !CampaignPilot.PlayFight(c, c.StartEncounter(gear), new CampaignPilot(PilotStyle.Reference, seed)).Won) continue;
                Assert.IsTrue(c.TryTakeSpoils(out string why), why);
                // A class piece fits only its own class (20 Oct 2026).
                CampaignFighter wearer = c.Reserve.FirstOrDefault(f => c.Gear[0].Fits(f.Class));
                if (wearer == null) continue;
                Assert.IsTrue(c.TryEquip(wearer.Id, c.Gear[0].Id, out why), why);
                Assert.IsTrue(c.TrySetRow(wearer.Id, wearer.Row == Row.Front ? Row.Back : Row.Front, out why), why);
                gear = c.Offers.ToList().FindIndex(o => o.Node == null || o.Node.Pal == Pal.None || c.PalRoadRun);
                if (gear < 0 || !CampaignPilot.PlayFight(c, c.StartEncounter(gear), new CampaignPilot(PilotStyle.Reference, seed + 1)).Won) continue;
                Assert.IsNotNull(c.Spoils, "a second piece is on offer");
                string saved = CampaignSave.Write(c);
                Campaign read = CampaignSave.Read(saved, easy);
                Assert.IsEmpty(read.LoadNote);
                Assert.AreEqual(saved, CampaignSave.Write(read), "every row, piece, wearer and the spoils come back");
                CollectionAssert.AreEqual(c.Reserve.Select(f => f.Row).ToList(), read.Reserve.Select(f => f.Row).ToList());
                CollectionAssert.AreEqual(c.Reserve.Select(f => f.GearId).ToList(), read.Reserve.Select(f => f.GearId).ToList());
                CollectionAssert.AreEqual(c.Gear.Select(g => g.Describe()).ToList(), read.Gear.Select(g => g.Describe()).ToList());
                Assert.AreEqual(c.Spoils.Id, read.Spoils.Id);
                Assert.AreEqual(c.SpoilsMaterials, read.SpoilsMaterials);
                return;
            }
            Assert.Fail("the reference pilot never won two path fights in twenty campaigns");
        }

        // ------------------------------------------------------------ the fight: quirk and gear effects

        private static List<UnitSpec> Clarth(int rise = 0, int killNerve = 0, int bigHit = 0)
        {
            var list = new List<UnitSpec>();
            foreach (var (id, cls) in new[]
            {
                ("c.warden", UnitClass.Warden), ("c.skirm", UnitClass.Skirmisher), ("c.marks", UnitClass.Marksman),
                ("c.trap", UnitClass.Trapper), ("c.medic", UnitClass.Medic)
            })
                list.Add(new UnitSpec(id, cls, SliceClasses.Stats(cls).DefaultRow, UnitRole.Fighter, 100, 100, 0, 0, 0, 0, 0,
                    rise, killNerve, bigHit));
            return list;
        }

        private static BattleSetup Setup(string id, ulong seed, FightRules fight, List<UnitSpec> ours, SideRules theirRules = null,
            bool chief = false)
        {
            var theirs = new List<UnitSpec>
            {
                new UnitSpec("r.warden", UnitClass.Warden), new UnitSpec("r.skirm", UnitClass.Skirmisher),
                new UnitSpec("r.marks", UnitClass.Marksman), new UnitSpec("r.medic", UnitClass.Medic)
            };
            if (chief) theirs.Add(new UnitSpec("r.chief", UnitClass.Skirmisher, Row.Back, UnitRole.Chief, 100, 100));
            return new BattleSetup(id, seed, ours, theirs, new TacticsProfile(), new TacticsProfile(), BattleSetup.DefaultTickLimit,
                BattleSetup.DefaultDirectivePoints, NerveRules.On(), SliceRules.RefillTicks, 400, null, theirRules, fight);
        }

        [Test]
        public void ASteadyFighterRisesWhenHisNerveSnaps()
        {
            var sim = new BattleSimulation(Setup("steady", 14, new FightRules(false, false, false, true), Clarth(rise: 100)));
            sim.RunUntilFinished();
            List<BattleEvent> ours = sim.Events.Where(e => e.Kind == BattleEventKind.Snap && e.ActorSide == 0).ToList();
            Assert.Greater(ours.Count, 0, "your side snapped");
            Assert.IsTrue(ours.All(e => e.Detail == FightRules.Rise), "a fighter this steady always rises");
        }

        [Test]
        public void TheBloodridgeChiefsPieceMakesKillsBreakMoreNerve()
        {
            // The same fight twice, up to your first kill: with the piece, their nerve is that much lower.
            var plain = new BattleSimulation(Setup("kill.nerve", 21, FightRules.Off, Clarth()));
            var worn = new BattleSimulation(Setup("kill.nerve", 21, FightRules.Off, Clarth(killNerve: CampaignGearRules.KillNerve)));
            Assert.AreEqual(ToFirstKill(plain), ToFirstKill(worn), "the same fight up to the first kill");
            Assert.AreEqual(plain.Nerve(1) - CampaignGearRules.KillNerve, worn.Nerve(1));
        }

        private static long ToFirstKill(BattleSimulation sim)
        {
            while (!sim.IsFinished && !sim.Events.Any(e => e.Kind == BattleEventKind.Kill && e.ActorSide == 0)) sim.Tick();
            Assert.IsFalse(sim.IsFinished, "the fight ended before your first kill");
            return sim.CurrentTick;
        }

        [Test]
        public void TheSkullpeakChiefsPieceHalvesBigHits()
        {
            BattleEvent plain = FirstEclipseHit(new BattleSimulation(Setup("big.hit", 13, FightRules.Off, Clarth(),
                new SideRules(2, 60, false), true)));
            BattleEvent guarded = FirstEclipseHit(new BattleSimulation(Setup("big.hit", 13, FightRules.Off,
                Clarth(bigHit: CampaignGearRules.BigHitGuardPercent), new SideRules(2, 60, false), true)));
            Assert.AreEqual(plain.Tick, guarded.Tick);
            Assert.AreEqual(plain.TargetId, guarded.TargetId);
            Assert.AreEqual(Math.Max(1, plain.Amount * (100 - CampaignGearRules.BigHitGuardPercent) / 100), guarded.Amount);
        }

        private static BattleEvent FirstEclipseHit(BattleSimulation sim)
        {
            int seen = 0;
            while (!sim.IsFinished)
            {
                sim.Tick();
                for (; seen < sim.Events.Count; seen++)
                {
                    BattleEvent e = sim.Events[seen];
                    if (e.Kind == BattleEventKind.Hit && e.Detail == Moves.EclipseDetail) return e;
                }
            }
            Assert.Fail("no Eclipse landed");
            return default;
        }
    }
}
