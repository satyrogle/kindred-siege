using System.Collections.Generic;
using System.Linq;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using NUnit.Framework;

namespace KindredSiege.Sim.Tests
{
    /// Sprint 1, the squad's spells (Jacob, 20 Oct 2026: "go"; DESIGN_LOCKS, "The squad's spells, marks and class
    /// pieces"). One quick test per spell and per mark, only to prove each one runs, then the class pieces: three
    /// offered after a win, strip one, and the save.
    public class ClassSpellTests
    {
        // A fight with the class spells: tough fighters, no nerve and no big moves, so only the spells happen.
        private static BattleSimulation Fight(string id, ulong seed, List<UnitSpec> ours, List<UnitSpec> theirs)
        {
            return new BattleSimulation(new BattleSetup(id, seed, ours, theirs, new TacticsProfile(), new TacticsProfile(),
                BattleSetup.DefaultTickLimit, BattleSetup.DefaultDirectivePoints, NerveRules.Off, 0, 400, null, null,
                FightRules.Campaign));
        }

        private static List<UnitSpec> Side(params UnitSpec[] units)
        {
            return new List<UnitSpec>(units);
        }

        private static UnitSpec U(string id, UnitClass unitClass)
        {
            return new UnitSpec(id, unitClass);
        }

        // The index of the first event from this index on that matches, ticking on as needed up to this tick; -1 if none.
        private static int RunUntil(BattleSimulation sim, int ticks, System.Func<BattleEvent, bool> match, int from = 0)
        {
            int seen = from;
            while (true)
            {
                for (; seen < sim.Events.Count; seen++) if (match(sim.Events[seen])) return seen;
                if (sim.IsFinished || sim.CurrentTick >= ticks) return -1;
                sim.Tick();
            }
        }

        // ------------------------------------------------------------ the five spells

        [Test]
        public void WardenGuardTakesTheBlowMeantForHisWard()
        {
            // Their Skirmisher goes for our back row; our Warden covers it and the blow lands on him, lighter.
            var sim = Fight("guard", 2, Side(U("w", UnitClass.Warden), U("a", UnitClass.Marksman)), Side(U("x", UnitClass.Skirmisher)));
            int at = RunUntil(sim, 1500, e => e.Kind == BattleEventKind.Guard && e.ActorId == "w" && e.TargetId == "a");
            Assert.GreaterOrEqual(at, 0, "he never covered his ward");
            BattleEvent hit = sim.Events[at + 1];
            Assert.AreEqual(BattleEventKind.Hit, hit.Kind);
            Assert.AreEqual("w", hit.TargetId);
            Assert.AreEqual(BattleSimulation.GuardedDetail, hit.Detail);
        }

        [Test]
        public void MarksmanAimedShotFiresAfterItsAimUnlessABlowBreaksIt()
        {
            // Their Medic never fights back, so the aim runs its whole time, then one big shot (which can miss, like any).
            var sim = Fight("aim", 4, Side(U("a", UnitClass.Marksman)), Side(U("m", UnitClass.Medic)));
            int shotAt = RunUntil(sim, 3000, e => e.Kind == BattleEventKind.Hit && e.ActorId == "a" && e.Detail == ClassSpells.AimedDetail);
            Assert.GreaterOrEqual(shotAt, 0, "no Aimed Shot landed");
            int at = shotAt;
            while (sim.Events[at].Kind != BattleEventKind.Aim || sim.Events[at].ActorId != "a") at--;
            BattleEvent aim = sim.Events[at], shot = sim.Events[shotAt];
            Assert.AreEqual(ClassSpells.AimTicks, aim.Amount);
            Assert.AreEqual(ClassSpells.AimDetail, aim.Detail);
            Assert.IsFalse(sim.Events.Skip(at + 1).Take(shotAt - at - 1).Any(e => e.ActorId == "a"), "nothing else from him while he aims");
            Assert.AreEqual(aim.Tick + aim.Amount - 1, shot.Tick);
            Assert.AreEqual(aim.TargetId, shot.TargetId);
            Assert.GreaterOrEqual(shot.Amount, 18 * 85 / 100 * ClassSpells.AimedShotPercent / 100, "a big shot");
            BattleEvent opening = sim.Events[shotAt + 1];
            Assert.AreEqual(BattleEventKind.Mark, opening.Kind, "the shot leaves an Opening");
            Assert.AreEqual(ClassSpells.OpeningMark, opening.Detail);
            Assert.AreEqual(ClassSpells.OpeningTicks, opening.Amount);

            // A Skirmisher who can reach him breaks his aim: the shot is lost.
            var hit = Fight("aim.broken", 5, Side(U("a", UnitClass.Marksman)), Side(U("x", UnitClass.Skirmisher)));
            int broke = RunUntil(hit, 3000, e => e.Kind == BattleEventKind.Broken);
            Assert.GreaterOrEqual(broke, 0, "no blow ever broke his aim");
            Assert.AreEqual("a", hit.Events[broke].TargetId);
            Assert.AreEqual("x", hit.Events[broke].ActorId);
            BattleUnit a = hit.FindById("a");
            Assert.AreEqual(0, a.AimTicks);
            int next = RunUntil(hit, 3000, e => e.ActorId == "a" && (e.Kind == BattleEventKind.Aim || e.Detail == ClassSpells.AimedDetail),
                broke + 1);
            if (next >= 0)
            {
                Assert.AreEqual(BattleEventKind.Aim, hit.Events[next].Kind, "the lost shot never fired; he aimed again");
                long wait = hit.Events[next].Tick - hit.Events[broke].Tick;
                Assert.GreaterOrEqual(wait, ClassSpells.AimRetryTicks, "after the 2 second retry");
                Assert.Less(wait, ClassSpells.AimEveryTicks, "not the full 5 seconds");
            }
        }

        [Test]
        public void SkirmisherStrikesHardOnHerOwnClock()
        {
            var sim = Fight("strike", 6, Side(U("k", UnitClass.Skirmisher)), Side(U("m", UnitClass.Medic)));
            var strikes = new List<BattleEvent>();
            while (!sim.IsFinished && sim.CurrentTick < 1200) sim.Tick();
            foreach (BattleEvent e in sim.Events)
                if (e.Kind == BattleEventKind.Hit && e.ActorId == "k" && e.Detail == ClassSpells.StrikeDetail) strikes.Add(e);
            Assert.GreaterOrEqual(strikes.Count, 3, "she struck again and again");
            for (int i = 1; i < strikes.Count; i++)
                Assert.GreaterOrEqual(strikes[i].Tick - strikes[i - 1].Tick, ClassSpells.StrikeEveryTicks, "one strike per clock");
            foreach (BattleEvent s in strikes) Assert.GreaterOrEqual(s.Amount, 10 * 85 / 100 * ClassSpells.StrikePercent / 100);
        }

        [Test]
        public void TrapperBarbedSnareHoldsWhoeverSpringsItAndItBleeds()
        {
            var sim = Fight("snare", 3, Side(U("w", UnitClass.Warden), U("t", UnitClass.Trapper)), Side(U("x", UnitClass.Skirmisher)));
            int at = RunUntil(sim, 1500, e => e.Kind == BattleEventKind.Mark && e.Detail == ClassSpells.SnareMark);
            Assert.GreaterOrEqual(at, 0, "nobody sprang the trap");
            BattleEvent snare = sim.Events[at];
            Assert.AreEqual("t", snare.ActorId);
            Assert.AreEqual("x", snare.TargetId);
            Assert.AreEqual(ClassSpells.SnareTicks, snare.Amount);
            Assert.IsTrue(sim.Events.Any(e => e.Kind == BattleEventKind.Mark && e.Detail == ClassSpells.BleedMark && e.TargetId == "x"));
            Assert.IsFalse(sim.Events.Any(e => e.Tick == snare.Tick && e.Kind == BattleEventKind.Hit && e.ActorId == "x"),
                "the blow that sprang it never landed");

            // Held: x does nothing and dodges nothing until the snare runs out, and it bleeds meanwhile.
            int from = sim.Events.Count;
            BattleUnit x = sim.FindById("x");
            while (!sim.IsFinished && x.SnareTicks > 0) sim.Tick();
            var held = sim.Events.Skip(from).ToList();
            Assert.IsFalse(held.Any(e => e.ActorId == "x" && (e.Kind == BattleEventKind.Hit || e.Kind == BattleEventKind.Miss)));
            Assert.IsFalse(held.Any(e => e.Kind == BattleEventKind.Miss && e.TargetId == "x"), "a snared enemy cannot dodge");
            Assert.IsTrue(held.Any(e => e.Kind == BattleEventKind.Hit && e.TargetId == "x" && e.Detail == BleedRules.Detail));
        }

        [Test]
        public void MedicTreatsAndNeverAttacks()
        {
            var sim = Fight("treat", 1, Side(U("w", UnitClass.Warden), U("m", UnitClass.Medic)),
                Side(U("x", UnitClass.Skirmisher), U("y", UnitClass.Skirmisher)));
            while (!sim.IsFinished && sim.CurrentTick < 1500) sim.Tick();
            Assert.IsTrue(sim.Events.Any(e => e.Kind == BattleEventKind.Heal && e.ActorId == "m" && e.Detail == ClassSpells.TreatDetail));
            Assert.IsFalse(sim.Events.Any(e => (e.Kind == BattleEventKind.Hit || e.Kind == BattleEventKind.Miss) && e.ActorId == "m"),
                "heal can't dps");
        }

        // ------------------------------------------------------------ the four marks

        [Test]
        public void SnareMakesTheAimedShotFastAndAlwaysCrit()
        {
            // Our Trapper's snare holds their Skirmisher; our Marksman aims at the snared one twice as fast and crits.
            for (ulong seed = 10; seed < 70; seed++)
            {
                var sim = Fight("pin", seed, Side(U("t", UnitClass.Trapper), U("a", UnitClass.Marksman)), Side(U("x", UnitClass.Skirmisher)));
                int at = RunUntil(sim, 1500, e => e.Kind == BattleEventKind.Aim && e.Detail == ClassSpells.PinDetail);
                if (at < 0) continue;
                Assert.AreEqual(ClassSpells.AimTicks * ClassSpells.PinAimPercent / 100, sim.Events[at].Amount, "twice as fast");
                int shot = RunUntil(sim, 1500, e => e.Kind == BattleEventKind.Hit && e.ActorId == "a", at + 1);
                if (shot < 0 || sim.Events[shot].Detail != ClassSpells.AimedPinDetail) continue; // the snare ran out first
                BattleEvent cash = sim.Events[shot - 1];
                Assert.AreEqual(BattleEventKind.Cash, cash.Kind);
                Assert.AreEqual(ClassSpells.PinDetail, cash.Detail);
                Assert.AreEqual(2 * cash.Amount, sim.Events[shot].Amount, "a crit, twice the shot");
                return;
            }
            Assert.Fail("no Aimed Shot crit a snared enemy in sixty fights");
        }

        [Test]
        public void BleedIsRippedOpenByTheStrike()
        {
            // Her blades bleed (gear), and her strike rips the wound: every second of bleeding left lands at once.
            var k = new UnitSpec("k", UnitClass.Skirmisher, Row.Front, UnitRole.Fighter, 100, 100, 0, 0, 0, 0, 0, 0, 0, 0, 2);
            var sim = Fight("rip", 7, Side(k), Side(U("m", UnitClass.Medic)));
            BattleUnit m = sim.FindById("m");
            int seen = 0;
            while (!sim.IsFinished && sim.CurrentTick < 2000)
            {
                int ticks = m.BleedTicks, percent = m.BleedPercent;
                sim.Tick();
                for (; seen < sim.Events.Count; seen++)
                {
                    BattleEvent e = sim.Events[seen];
                    if (e.Kind != BattleEventKind.Cash || e.Detail != ClassSpells.RipDetail) continue;
                    int left = (ticks + SimClock.TicksPerSecond - 1) / SimClock.TicksPerSecond;
                    Assert.AreEqual(left * System.Math.Max(1, m.MaxHp * percent / 100), e.Amount);
                    BattleEvent rip = sim.Events[seen + 1];
                    Assert.AreEqual(BattleEventKind.Hit, rip.Kind);
                    Assert.AreEqual(ClassSpells.RipDetail, rip.Detail);
                    Assert.AreEqual(e.Amount, rip.Amount);
                    return;
                }
            }
            Assert.Fail("her strike never ripped a wound");
        }

        [Test]
        public void OpeningFromTheAimedShotDoublesTheStrike()
        {
            for (ulong seed = 20; seed < 60; seed++)
            {
                var sim = Fight("open", seed, Side(U("a", UnitClass.Marksman), U("k", UnitClass.Skirmisher)), Side(U("m", UnitClass.Medic)));
                int at = RunUntil(sim, 2000, e => e.Kind == BattleEventKind.Cash && e.Detail == ClassSpells.OpeningMark);
                if (at < 0) continue;
                BattleEvent cash = sim.Events[at], strike = sim.Events[at + 1];
                Assert.AreEqual("k", cash.ActorId);
                Assert.IsTrue(sim.Events.Take(at).Any(e => e.Kind == BattleEventKind.Mark && e.Detail == ClassSpells.OpeningMark &&
                    e.ActorId == "a" && e.TargetId == "m"), "the Aimed Shot left it");
                Assert.AreEqual(BattleEventKind.Hit, strike.Kind);
                Assert.AreEqual(ClassSpells.StrikeOpenDetail, strike.Detail);
                Assert.AreEqual(2 * cash.Amount, strike.Amount, "twice as hard");
                bool reopened = sim.Events.Skip(at).Any(e => e.Kind == BattleEventKind.Mark && e.Detail == ClassSpells.OpeningMark);
                if (!reopened) Assert.AreEqual(0, sim.FindById("m").OpeningTicks, "the strike closed the Opening");
                return;
            }
            Assert.Fail("no strike ever hit an Opening in forty fights");
        }

        [Test]
        public void GuardMakesTheTreatmentHealMore()
        {
            var sim = Fight("covered", 8, Side(U("w", UnitClass.Warden), U("k", UnitClass.Skirmisher), U("m", UnitClass.Medic)),
                Side(U("x", UnitClass.Skirmisher), U("y", UnitClass.Skirmisher)));
            int at = RunUntil(sim, 2000, e => e.Kind == BattleEventKind.Cash && e.Detail == ClassSpells.CoveredDetail);
            Assert.GreaterOrEqual(at, 0, "she never treated a guarded ally");
            BattleEvent cash = sim.Events[at], heal = sim.Events[at + 1];
            int power = SliceClasses.Stats(UnitClass.Medic).Power;
            Assert.AreEqual(power * ClassSpells.GuardedTreatPercent / 100 - power, cash.Amount);
            Assert.AreEqual(BattleEventKind.Heal, heal.Kind);
            Assert.AreEqual(cash.TargetId, heal.TargetId);
            Assert.AreEqual(ClassSpells.TreatDetail, heal.Detail);
            Assert.LessOrEqual(heal.Amount, power * ClassSpells.GuardedTreatPercent / 100);
        }

        // ------------------------------------------------------------ the ten pieces, two per class

        private static UnitSpec Wearing(string id, UnitClass unitClass, ClassBend bend)
        {
            return new UnitSpec(id, unitClass).WithBend(bend);
        }

        [Test]
        public void WardenPiecesBastionAndRetaliator()
        {
            // Bastion: his Guard takes half off a guarded blow instead of 30%.
            var bastion = Fight("bastion", 2, Side(Wearing("w", UnitClass.Warden, ClassBend.Bastion), U("a", UnitClass.Marksman)),
                Side(U("x", UnitClass.Skirmisher)));
            int at = RunUntil(bastion, 1500, e => e.Kind == BattleEventKind.Guard && e.ActorId == "w");
            Assert.GreaterOrEqual(at, 0, "he never guarded");
            BattleEvent guard = bastion.Events[at], hit = bastion.Events[at + 1];
            Assert.AreEqual((guard.Amount + hit.Amount) * (100 - ClassSpells.BastionGuardPercent) / 100, hit.Amount);

            // Retaliator: whoever struck into his Guard is left open.
            var ret = Fight("retaliator", 2, Side(Wearing("w", UnitClass.Warden, ClassBend.Retaliator), U("a", UnitClass.Marksman)),
                Side(U("x", UnitClass.Skirmisher)));
            at = RunUntil(ret, 1500, e => e.Kind == BattleEventKind.Guard && e.ActorId == "w");
            Assert.GreaterOrEqual(at, 0, "he never guarded");
            BattleEvent mark = ret.Events[at + 2];
            Assert.AreEqual(BattleEventKind.Mark, mark.Kind);
            Assert.AreEqual(ClassSpells.OpeningMark, mark.Detail);
            Assert.AreEqual("w", mark.ActorId);
            Assert.AreEqual(ret.Events[at + 1].ActorId, mark.TargetId, "on whoever struck");
        }

        [Test]
        public void MarksmanPiecesDeadeyeAndQuickdraw()
        {
            // Deadeye aims half again as long.
            var dead = Fight("deadeye", 4, Side(Wearing("a", UnitClass.Marksman, ClassBend.Deadeye)), Side(U("m", UnitClass.Medic)));
            int at = RunUntil(dead, 1500, e => e.Kind == BattleEventKind.Aim);
            Assert.AreEqual(ClassSpells.AimTicks * ClassSpells.DeadeyePercent / 100, dead.Events[at].Amount);

            // Quickdraw aims half as long, and blows that land while he aims never break it.
            var quick = Fight("quickdraw", 5, Side(Wearing("a", UnitClass.Marksman, ClassBend.Quickdraw)), Side(U("x", UnitClass.Skirmisher)));
            while (!quick.IsFinished && quick.CurrentTick < 3000) quick.Tick();
            var aims = quick.Events.Where(e => e.Kind == BattleEventKind.Aim).ToList();
            Assert.Greater(aims.Count, 0);
            foreach (BattleEvent aim in aims) Assert.AreEqual(ClassSpells.AimTicks * ClassSpells.QuickdrawAimPercent / 100, aim.Amount);
            Assert.IsTrue(quick.Events.Any(e => e.Kind == BattleEventKind.Hit && e.TargetId == "a" &&
                aims.Any(aim => e.Tick > aim.Tick && e.Tick < aim.Tick + aim.Amount - 1)), "a blow landed while he aimed");
            Assert.IsFalse(quick.Events.Any(e => e.Kind == BattleEventKind.Broken), "and never broke his aim");
        }

        [Test]
        public void SkirmisherPiecesOpportunistAndBloodrush()
        {
            // Opportunist: her strike hits an Opening three times as hard, not twice.
            int found = -1;
            BattleSimulation sim = null;
            for (ulong seed = 20; seed < 60 && found < 0; seed++)
            {
                sim = Fight("opportunist", seed, Side(U("a", UnitClass.Marksman), Wearing("k", UnitClass.Skirmisher, ClassBend.Opportunist)),
                    Side(U("m", UnitClass.Medic)));
                found = RunUntil(sim, 2000, e => e.Kind == BattleEventKind.Cash && e.Detail == ClassSpells.OpeningMark);
            }
            Assert.GreaterOrEqual(found, 0, "no strike hit an Opening");
            Assert.AreEqual(3 * sim.Events[found].Amount, 2 * sim.Events[found + 1].Amount, "three times the strike");

            // Bloodrush: below half health her strike comes twice as often.
            var rush = Fight("bloodrush", 9, Side(Wearing("k", UnitClass.Skirmisher, ClassBend.Bloodrush)),
                Side(U("x", UnitClass.Skirmisher), U("y", UnitClass.Skirmisher)));
            while (!rush.IsFinished && rush.CurrentTick < 3000) rush.Tick();
            var strikes = rush.Events.Where(e => e.Kind == BattleEventKind.Hit && e.ActorId == "k" && e.Detail.StartsWith(ClassSpells.StrikeDetail))
                .Select(e => e.Tick).ToList();
            bool quicker = false;
            for (int i = 1; i < strikes.Count; i++) quicker |= strikes[i] - strikes[i - 1] < ClassSpells.StrikeEveryTicks;
            Assert.IsTrue(quicker, "a strike came sooner than the usual clock once she was hurt");
        }

        [Test]
        public void TrapperPiecesBarbsAndLongSnare()
        {
            var barbs = Fight("barbs", 3, Side(U("w", UnitClass.Warden), Wearing("t", UnitClass.Trapper, ClassBend.Barbs)),
                Side(U("x", UnitClass.Skirmisher)));
            Assert.GreaterOrEqual(RunUntil(barbs, 1500, e => e.Kind == BattleEventKind.Mark && e.Detail == ClassSpells.SnareMark), 0);
            Assert.AreEqual(ClassSpells.SnareBleedPercent * ClassSpells.BarbsPercent / 100, barbs.FindById("x").BleedPercent, "bleeds twice as hard");

            var longer = Fight("longsnare", 3, Side(U("w", UnitClass.Warden), Wearing("t", UnitClass.Trapper, ClassBend.LongSnare)),
                Side(U("x", UnitClass.Skirmisher)));
            int at = RunUntil(longer, 1500, e => e.Kind == BattleEventKind.Mark && e.Detail == ClassSpells.SnareMark);
            Assert.AreEqual(ClassSpells.SnareTicks * ClassSpells.LongSnarePercent / 100, longer.Events[at].Amount, "holds twice as long");
        }

        [Test]
        public void MedicPiecesSurgeonAndFieldKit()
        {
            int power = SliceClasses.Stats(UnitClass.Medic).Power;

            // Surgeon: each treatment half again as much, and half again as slow.
            var surgeon = Fight("surgeon", 1, Side(U("k", UnitClass.Skirmisher), Wearing("m", UnitClass.Medic, ClassBend.Surgeon)),
                Side(U("x", UnitClass.Skirmisher), U("y", UnitClass.Skirmisher)));
            while (!surgeon.IsFinished && surgeon.CurrentTick < 1500) surgeon.Tick();
            var treats = surgeon.Events.Where(e => e.Kind == BattleEventKind.Heal && e.ActorId == "m").ToList();
            Assert.IsTrue(treats.Any(e => e.Amount == power * ClassSpells.SurgeonPercent / 100), "a full Surgeon's treatment");
            int slow = SliceClasses.Stats(UnitClass.Medic).CooldownTicks * ClassSpells.SurgeonPercent / 100;
            for (int i = 1; i < treats.Count; i++) Assert.GreaterOrEqual(treats[i].Tick - treats[i - 1].Tick, slow);

            // Field Kit: each treatment also treats the next most hurt for half. Their Skirmisher goes for her, their Warden
            // for our front, so two of ours get hurt.
            var kit = Fight("fieldkit", 1, Side(U("k", UnitClass.Skirmisher), Wearing("m", UnitClass.Medic, ClassBend.FieldKit)),
                Side(U("x", UnitClass.Skirmisher), U("w", UnitClass.Warden)));
            while (!kit.IsFinished && kit.CurrentTick < 1500) kit.Tick();
            var pairs = kit.Events.Where(e => e.Kind == BattleEventKind.Heal && e.ActorId == "m").GroupBy(e => e.Tick).Where(g => g.Count() == 2).ToList();
            Assert.Greater(pairs.Count, 0, "a treatment that treated two");
            foreach (var pair in pairs)
            {
                Assert.AreNotEqual(pair.First().TargetId, pair.Last().TargetId);
                Assert.LessOrEqual(pair.Last().Amount, power * ClassSpells.FieldKitPercent / 100);
            }
        }

        // ------------------------------------------------------------ class pieces

        [Test]
        public void AWinOffersThreeClassPiecesAndYouStripOne()
        {
            for (ulong seed = 900; seed < 940; seed++)
            {
                Campaign c = Campaign.New(seed, new CampaignTuning { EnemyScaleTier1 = 40 });
                Assert.IsTrue(CampaignPilot.PlayTutorial(c, PilotStyle.Reference, seed, 10), "the tutorial was never won");
                c.StartRun();
                // From rules 19 every road fight drops gear beside its Pal's picks (fix 5).
                int gear = c.Offers.ToList().FindIndex(o => o.Node == null || o.Node.Pal == Pal.None || c.PalRoadRun);
                if (gear < 0) continue;
                EncounterResult r = CampaignPilot.PlayFight(c, c.StartEncounter(gear), new CampaignPilot(PilotStyle.Reference, seed));
                if (!r.Won || c.SpoilsChoices.Count == 0) continue;
                Assert.IsTrue(c.Archive.Fights.Last().Setup.Fight.ClassSpells, "campaign fights play the class spells");

                // Up to three pieces, each for a different class, each bending that class's own spell, and each worn in the
                // fight by one of theirs of its class.
                IReadOnlyList<CampaignGear> three = c.SpoilsChoices;
                IReadOnlyList<UnitSpec> them = c.Archive.Fights.Last().Setup.Side(1);
                Assert.AreEqual(System.Math.Min(CampaignGearRules.SpoilsOffered, them.Select(u => u.Class).Distinct().Count()), three.Count);
                Assert.AreEqual(three.Count, three.Select(p => p.ClassIndex).Distinct().Count());
                foreach (CampaignGear p in three)
                {
                    Assert.IsTrue(ClassSpells.IsFor(p.Bend, (UnitClass)p.ClassIndex), p.Name);
                    Assert.IsTrue(them.Any(u => (int)u.Class == p.ClassIndex && u.Bend == p.Bend), p.Name + " was worn in the fight");
                }
                CollectionAssert.AreEqual(three.Select(p => p.Id), r.SpoilsChoices.Select(p => p.Id));

                // The save keeps all three.
                Campaign read = CampaignSave.Read(CampaignSave.Write(c));
                CollectionAssert.AreEqual(three.Select(p => p.Id + p.ClassIndex + p.Bend), read.SpoilsChoices.Select(p => p.Id + p.ClassIndex + p.Bend));

                // Strip the last: the others are gone, and only its class can wear it.
                int last = three.Count - 1;
                CampaignGear pick = three[last];
                Assert.IsTrue(c.TryTakeSpoils(last, out string why), why);
                Assert.IsEmpty(c.SpoilsChoices);
                CollectionAssert.Contains(c.Gear, pick);
                CampaignFighter wrong = c.Reserve.First(f => (int)f.Class != pick.ClassIndex);
                Assert.IsFalse(c.TryEquip(wrong.Id, pick.Id, out why));
                CampaignFighter right = c.Reserve.FirstOrDefault(f => (int)f.Class == pick.ClassIndex);
                if (right == null || c.Stage != CampaignStage.InRun || c.Offers.Count == 0) continue;
                Assert.IsTrue(c.TryEquip(right.Id, pick.Id, out why), why);
                Assert.AreEqual(pick.Bend, CampaignSave.Read(CampaignSave.Write(c)).FindGear(pick.Id).Bend);

                // It bends that fighter's spell in the next fight.
                CampaignFight next = c.StartEncounter(0, new List<string> { right.Id });
                Assert.AreEqual(pick.Bend, next.Setup.Side(0).Single(u => u.Id == right.Id).Bend);
                return;
            }
            Assert.Fail("no won fight offered pieces in forty campaigns");
        }

        [Test]
        public void ASaveFromBeforeKeepsItsOnePieceOnOfferAndReadsEveryChiefsPiece()
        {
            for (ulong seed = 950; seed < 990; seed++)
            {
                Campaign c = Campaign.New(seed, new CampaignTuning { EnemyScaleTier1 = 40 });
                Assert.IsTrue(CampaignPilot.PlayTutorial(c, PilotStyle.Reference, seed, 10), "the tutorial was never won");
                c.StartRun();
                for (int n = 0; n < 6 && c.Stage == CampaignStage.InRun && (c.Gear.Count == 0 || c.SpoilsChoices.Count == 0); n++)
                {
                    if (c.SpoilsChoices.Count > 0) c.TryTakeSpoils(0, out _);
                    if (c.Offers.Count == 0) break;
                    var pilot = new CampaignPilot(PilotStyle.Reference, seed * 7 + (ulong)n);
                    CampaignPilot.PlayFight(c, c.StartEncounter(pilot.PickOffer(c)), pilot);
                }
                if (c.Gear.Count == 0 || c.SpoilsChoices.Count == 0) continue;

                // Written as version 14 would have: pieces without class and bend, one piece on offer, and a piece with
                // the cave chief's bleeding special, which version 14 could not read.
                var lines = new List<string>();
                bool spoils = false;
                foreach (string line in CampaignSave.Write(c).Split('\n'))
                {
                    if (line.StartsWith("clearroad\t")) continue; // Not present in version 14.
                    // Nor fix 5's (version 25): the road kind, and a Pal's picks on offer beside the piece.
                    if (line.StartsWith("palroad\t") || line.StartsWith("beastroll\t") || line.StartsWith("favouroffer\t")) continue;
                    string[] f = line.Split('\t');
                    if (f[0] == CampaignSave.Header) f[1] = "14";
                    if (f[0] == "piece" || f[0] == "spoils")
                    {
                        if (f[0] == "spoils" && spoils) continue;
                        spoils |= f[0] == "spoils";
                        var old = f.Take(7).ToList();
                        if (f[0] == "piece") old[5] = ((int)GearSpecial.Bleeding).ToString();
                        else old.Add(f[f.Length - 1]);
                        f = old.ToArray();
                    }
                    lines.Add(string.Join("\t", f));
                }
                Campaign read = CampaignSave.Read(string.Join("\n", lines));
                Assert.IsEmpty(read.LoadNote);
                Assert.AreEqual(1, read.SpoilsChoices.Count, "one piece on offer");
                Assert.AreEqual(CampaignGear.AnyClass, read.SpoilsChoices[0].ClassIndex, "an old piece fits anyone");
                Assert.AreEqual(c.SpoilsChoices[0].Id, read.SpoilsChoices[0].Id);
                Assert.IsTrue(read.Gear.All(g => g.Special == GearSpecial.Bleeding && g.ClassIndex == CampaignGear.AnyClass));
                Assert.IsTrue(read.TryTakeSpoils(0, out string why), why);
                return;
            }
            Assert.Fail("no run held gear with spoils on offer in forty campaigns");
        }
    }
}
