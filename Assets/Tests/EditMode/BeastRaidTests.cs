using System;
using System.Collections.Generic;
using System.Linq;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using NUnit.Framework;

namespace KindredSiege.Sim.Tests
{
    /// The beasts as raid bosses, campaign rules 27 (Jacob, 1 Nov 2026: "can we finish the bear and shark just like a wow boss",
    /// then "Full plan" and "Go as written"; FightRules.BeastRaid, BattleSimulation.BeastRaid.cs): the shark's Feeding frenzy
    /// and its sand pups, the cage bear's Overload and its climbing Pulse. One quick test per piece, with the old rules beside
    /// it where they differ. Nothing here is tuning.
    public class BeastRaidTests
    {
        private static readonly FightRules Old = FightRules.CampaignBeast.WithBeastScenes();
        private static readonly FightRules Raid = FightRules.CampaignBeast.WithBeastScenes().WithBeastRaid();

        private static UnitSpec U(string id, UnitClass unitClass)
        {
            return new UnitSpec(id, unitClass);
        }

        // A full squad behind its leader; without the Trapper a second Skirmisher, so no trap waits for the burst.
        private static List<UnitSpec> Squad(bool trapper = true)
        {
            return new List<UnitSpec> { new UnitSpec("leader", UnitClass.Marksman, Row.Back, UnitRole.Pal, 100, 100), U("w", UnitClass.Warden),
                U("s", UnitClass.Skirmisher), U("m", UnitClass.Marksman), trapper ? U("t", UnitClass.Trapper) : U("t", UnitClass.Skirmisher),
                U("d", UnitClass.Medic) };
        }

        // A beast's parts, each this percent as tough in its order, and with pups the shark's two pups after them.
        private static List<UnitSpec> Beast(string id, BeastKit kit, int[] percent, bool pups)
        {
            var parts = new List<UnitSpec>();
            IReadOnlyList<BeastPart> order = BeastKits.Parts(kit);
            for (int p = 0; p < order.Count; p++)
                parts.Add(new UnitSpec(id + "." + BeastKits.Name(order[p]), UnitClass.Warden, BeastKits.RowOf(order[p]), UnitRole.Fighter, 100,
                    Math.Max(1, percent[p] * BeastKits.HealthPercent(order[p]) / 100)));
            if (pups)
                for (int n = 1; n <= BeastKits.PupCount; n++)
                    parts.Add(new UnitSpec(BeastKits.PupId(id, n), UnitClass.Skirmisher, Row.Front, UnitRole.Fighter, 100, 40));
            return parts;
        }

        private static BattleSimulation Fight(string id, ulong seed, BeastKit kit, int[] percent, FightRules rules, bool trapper = true)
        {
            bool pups = rules.BeastRaid && kit == BeastKit.SandShark;
            return new BattleSimulation(new BattleSetup(id, seed, Squad(trapper), Beast(id, kit, percent, pups), new TacticsProfile(), new TacticsProfile(),
                BattleSetup.DefaultTickLimit, BattleSetup.DefaultDirectivePoints, NerveRules.On(), 10, 400, new SideRules(1, 0, true),
                new SideRules(0, 0, false, beast: kit), rules));
        }

        private static bool Order(SimSession session, string directive, string target = "")
        {
            return session.TryIssue(Directives.CommandKind, "0", target, 0, 0, directive, out _);
        }

        private static BattleEvent Next(BattleSimulation sim, SimSession session, Func<BattleEvent, bool> match, Action before = null)
        {
            int seen = sim.Events.Count;
            while (!sim.IsFinished)
            {
                before?.Invoke();
                session.Tick();
                for (; seen < sim.Events.Count; seen++)
                    if (match(sim.Events[seen])) return sim.Events[seen];
            }
            Assert.Fail("never happened");
            return default;
        }

        private static bool Lands(BattleEvent e, string move)
        {
            return e.Kind == BattleEventKind.Land && e.Detail.StartsWith(move + ".");
        }

        private static bool PhaseNamed(BattleEvent e, string name)
        {
            return e.Kind == BattleEventKind.Phase && e.Detail == name;
        }

        // The shark with its jaw and spine almost broken already, so its second break comes early; the rest are tough.
        private static readonly int[] TwoWeak = { 1, 3000, 1, 3000 };

        // Focus Fire on whichever weak part still stands, while the points last.
        private static Action FocusWeak(BattleSimulation sim, SimSession session)
        {
            return () =>
            {
                if (sim.FocusTicksLeft(0) > 0 || sim.DirectivePoints(0) <= 0) return;
                foreach (BeastPart part in new[] { BeastPart.Jaw, BeastPart.Spine })
                    if (!sim.PartBroken(part))
                    {
                        Order(session, "FocusFire", sim.Part(part).Id);
                        return;
                    }
            };
        }

        [Test]
        public void TheSharksSecondBreakStartsItsFeedingFrenzyAndItsPartsAreOutOfReach()
        {
            foreach (bool raid in new[] { false, true })
            {
                var sim = Fight("raid.feed", 3, BeastKit.SandShark, TwoWeak, raid ? Raid : Old);
                var session = new SimSession(sim);
                Assert.AreEqual(raid, sim.BeastRaidOn);
                BattleEvent phase = Next(sim, session, e => e.Kind == BattleEventKind.Phase, FocusWeak(sim, session));
                Assert.AreEqual(2, sim.Events.Count(e => e.Kind == BattleEventKind.Sever), "it came at the second break");
                if (!raid)
                {
                    Assert.AreEqual(BeastKits.FrenzyName, phase.Detail, "the old rules go straight to its Blood frenzy");
                    continue;
                }
                Assert.AreEqual(BeastKits.FeedingName, phase.Detail);
                Assert.AreEqual(BeastKits.FeedingName, sim.BeastIntermission);
                Assert.IsTrue(sim.BeastUnder);
                Assert.AreEqual(1, sim.BeastPhase, "its Blood frenzy waits for the burst");
                int fin = sim.Part(BeastPart.Fin).Hp, tail = sim.Part(BeastPart.Tail).Hp;
                Assert.AreEqual(string.Empty, sim.BeastWarning, "no warning before the churn");
                int waited = 0;
                while (sim.BeastWarning.Length == 0 && waited < BeastKits.FeedingTicks)
                {
                    session.Tick();
                    waited++;
                }
                Assert.AreEqual(BeastKits.ChurnWarning, sim.BeastWarning);
                Assert.That(waited, Is.InRange(BeastKits.FeedingTicks - BeastKits.ChurnTicks - 2, BeastKits.FeedingTicks - BeastKits.ChurnTicks + 1),
                    "the churn is its last ChurnTicks");
                Assert.IsTrue(sim.Events.Any(e => e.Kind == BattleEventKind.Tell && e.Detail == BeastKits.FeedingName));
                Assert.AreEqual(fin, sim.Part(BeastPart.Fin).Hp, "nothing reaches it under the sand");
                Assert.AreEqual(tail, sim.Part(BeastPart.Tail).Hp);
                Assert.IsFalse(sim.Events.Any(e => e.Kind == BattleEventKind.Tell && e.Tick > phase.Tick && e.Detail != BeastKits.FeedingName),
                    "its moves stop");
            }
        }

        [Test]
        public void TheBurstHitsAllFiveUnlessYouFallBackThenItsPupsLatchOnYourBackRow()
        {
            foreach (bool fallBack in new[] { false, true })
            {
                var sim = Fight("raid.burst", 3, BeastKit.SandShark, TwoWeak, Raid, trapper: false);
                var session = new SimSession(sim);
                Next(sim, session, e => PhaseNamed(e, BeastKits.FeedingName), FocusWeak(sim, session));
                BattleEvent burst = Next(sim, session, e => Lands(e, BeastKits.FeedingName), () =>
                {
                    if (fallBack && sim.BeastIntermissionTicks > 0 && sim.BeastIntermissionTicks <= 5 && sim.FallBackTicksLeft(0) == 0)
                        Order(session, "FallBack");
                });
                var hit = sim.Events.Where(e => e.Tick == burst.Tick && e.Kind == BattleEventKind.Hit && e.Detail == BeastKits.BurstDetail)
                    .Select(e => e.TargetId).Distinct().ToList();
                if (fallBack)
                {
                    Assert.AreEqual(BeastKits.FeedingName + "." + BeastKits.Dodged, burst.Detail);
                    Assert.IsEmpty(hit);
                }
                else
                {
                    var covered = sim.Events.Where(e => e.Tick == burst.Tick && e.Kind == BattleEventKind.Guard).Select(e => e.TargetId);
                    var reached = hit.Concat(covered).Select(sim.FindById).ToList();
                    Assert.IsTrue(reached.Any(u => u.Row == Row.Front) && reached.Any(u => u.Row == Row.Back), "it bursts through both rows");
                }
                Assert.IsTrue(sim.Events.Any(e => e.Tick == burst.Tick && PhaseNamed(e, BeastKits.FrenzyName)), "then its Blood frenzy");
                Assert.AreEqual(2, sim.BeastPhase);
                Assert.AreEqual(string.Empty, sim.BeastIntermission);

                IReadOnlyList<string> pups = sim.BeastAddIds;
                Assert.AreEqual(BeastKits.PupCount, pups.Count, "its two pups are in");
                var held = pups.Select(sim.BeastAddTargetId).ToList();
                Assert.IsTrue(held.All(id => sim.FindById(id).Row == Row.Back), "on your back row");
                Assert.AreEqual(held.Count, held.Distinct().Count(), "one each");

                // A pup bites whom it holds, and they bleed.
                string pup = pups[0];
                BattleEvent bite = Next(sim, session, e => e.Kind == BattleEventKind.Hit && e.ActorId == pup && e.Detail == BeastKits.PupDetail);
                Assert.AreEqual(sim.BeastAddTargetId(pup), bite.TargetId);
            }
        }

        [Test]
        public void AnArmedTrapCatchesTheBurstAndLeavesItExposed()
        {
            var sim = Fight("raid.trap", 3, BeastKit.SandShark, TwoWeak, Raid);
            var session = new SimSession(sim);
            Next(sim, session, e => PhaseNamed(e, BeastKits.FeedingName), FocusWeak(sim, session));
            BattleEvent burst = Next(sim, session, e => Lands(e, BeastKits.FeedingName));
            Assert.AreEqual(BeastKits.FeedingName + "." + BeastKits.Snared, burst.Detail, "her trap waited for it");
            Assert.IsFalse(sim.Events.Any(e => e.Tick == burst.Tick && e.Detail == BeastKits.BurstDetail), "nobody is hit");
            Assert.Greater(sim.BeastExposedTicks, 0);
            BattleEvent trap = sim.Events.First(e => e.Tick == burst.Tick && e.Kind == BattleEventKind.Hit && e.ActorId == "t");
            Assert.AreEqual(SliceClasses.TrapDamage * BeastKits.ExposedPercent / 100, trap.Amount, "its parts take double while exposed");
        }

        [Test]
        public void YouCanKillAPupAndThePupsDieWithTheShark()
        {
            var sim = Fight("raid.pups", 5, BeastKit.SandShark, new[] { 1, 1, 1, 1 }, Raid);
            var session = new SimSession(sim);
            Next(sim, session, e => e.Kind == BattleEventKind.Join && e.Detail == BeastKits.PupName, () =>
            {
                if (sim.FocusTicksLeft(0) > 0 || sim.DirectivePoints(0) <= 0) return;
                foreach (BeastPart part in new[] { BeastPart.Jaw, BeastPart.Spine })
                    if (!sim.PartBroken(part))
                    {
                        Order(session, "FocusFire", sim.Part(part).Id);
                        return;
                    }
            });
            string first = sim.BeastAddIds[0];
            BattleEvent kill = Next(sim, session, e => e.Kind == BattleEventKind.Kill && e.TargetId == first,
                () => { if (sim.FocusTicksLeft(0) == 0 && sim.DirectivePoints(0) > 0 && sim.FindById(first).Alive) Order(session, "FocusFire", first); });
            Assert.AreEqual(0, kill.ActorSide, "you killed it");
            Assert.IsFalse(sim.BeastAddIds.Contains(first));
            Assert.AreEqual(0, sim.Events.Count(e => e.Kind == BattleEventKind.Sever && e.TargetId == first), "a pup is no part: no trophy");

            sim.RunUntilFinished();
            Assert.AreEqual(0, sim.Winner);
            Assert.IsTrue(BeastKits.Parts(BeastKit.SandShark).All(sim.PartBroken), "it died with all four broken");
            Assert.IsEmpty(sim.BeastAddIds, "its pups went with it");
        }

        [Test]
        public void TheBearsBrokenCageBringsItsOverloadAndTheFrostFreezesWhoeverStands()
        {
            foreach (bool fallBack in new[] { false, true })
            {
                // Its ribcage almost broken, the rest tough.
                var sim = Fight("raid.overload", 7, BeastKit.CageBear, new[] { 3000, 3000, 1, 3000 }, Raid);
                var session = new SimSession(sim);
                BattleEvent overload = Next(sim, session, e => PhaseNamed(e, BeastKits.OverloadName), () =>
                {
                    BattleUnit cage = sim.Part(BeastPart.Ribcage);
                    if (cage.Alive && sim.FocusTicksLeft(0) == 0 && sim.DirectivePoints(0) > 0) Order(session, "FocusFire", cage.Id);
                });
                Assert.IsTrue(sim.PartBroken(BeastPart.Ribcage));
                Assert.AreEqual(BeastKits.OverloadName, sim.BeastIntermission);
                Assert.IsTrue(sim.Events.Any(e => e.Tick == overload.Tick && e.Detail == BeastKits.RoarName + "." + BeastKits.Full), "it roars");
                Assert.IsTrue(sim.Events.Any(e => e.Tick == overload.Tick && e.Kind == BattleEventKind.Tell && e.Detail == BeastKits.FrostName));
                Assert.AreEqual(BeastKits.FrostWarning, sim.BeastWarning);

                BattleEvent frost = Next(sim, session, e => Lands(e, BeastKits.FrostName), () =>
                {
                    if (fallBack && sim.BeastIntermissionTicks > 0 && sim.BeastIntermissionTicks <= 5 && sim.FallBackTicksLeft(0) == 0)
                        Order(session, "FallBack");
                });
                Assert.AreEqual(overload.Tick + BeastKits.FrostTicks, frost.Tick);
                var ours = sim.Units.Where(u => u.Side == 0 && u.Alive && !u.IsLeader).ToList();
                if (fallBack)
                {
                    Assert.AreEqual(BeastKits.FrostName + "." + BeastKits.Dodged, frost.Detail);
                    Assert.IsTrue(ours.All(u => sim.FrozenTicks(u) == 0));
                    continue;
                }
                Assert.AreEqual(BeastKits.FrostName + "." + BeastKits.Full, frost.Detail);
                Assert.IsTrue(ours.All(u => sim.FrozenTicks(u) > 0), "everyone standing is frozen");
                // A frozen fighter's turn does not come round: his clock stands still until the ice lets go.
                var clocks = ours.ToDictionary(u => u.Id, u => u.Cooldown);
                for (int i = 0; i < BeastKits.FrozenTicks - 2 && !sim.IsFinished; i++) session.Tick();
                foreach (BattleUnit u in ours)
                    if (u.Alive) Assert.AreEqual(clocks[u.Id], u.Cooldown, u.Id + " stood frozen");
                for (int i = 0; i < 4 && !sim.IsFinished; i++) session.Tick();
                Assert.IsTrue(ours.All(u => sim.FrozenTicks(u) == 0), "and then it lets go");
            }
        }

        [Test]
        public void InItsLastStandEachPulseHitsHarderThanTheLast()
        {
            foreach (bool raid in new[] { false, true })
            {
                // Everything but its heart almost broken, so its Last stand comes early.
                var sim = Fight("raid.ramp", 9, BeastKit.CageBear, new[] { 1, 1, 1, 3000 }, raid ? Raid : Old);
                var session = new SimSession(sim);
                var pulses = new List<BattleEvent>();
                while (!sim.IsFinished && pulses.Count < 3)
                {
                    int seen = sim.Events.Count;
                    if (sim.FocusTicksLeft(0) == 0 && sim.DirectivePoints(0) > 0)
                        foreach (BeastPart part in new[] { BeastPart.Ribcage, BeastPart.Claw, BeastPart.Maw })
                            if (!sim.PartBroken(part))
                            {
                                Order(session, "FocusFire", sim.Part(part).Id);
                                break;
                            }
                    session.Tick();
                    for (int i = seen; i < sim.Events.Count; i++)
                        if (sim.BeastPhase >= 3 && Lands(sim.Events[i], BeastKits.PulseName) && !sim.Events[i].Detail.EndsWith(BeastKits.Parried))
                        {
                            // The same five each time: compare the hit on one who stands throughout.
                            pulses.Add(sim.Events[i]);
                        }
                }
                Assert.AreEqual(3, pulses.Count, "three Pulses in its Last stand");
                Dictionary<string, int> Shares(BattleEvent pulse) => sim.Events.Where(e => e.Tick == pulse.Tick && e.Kind == BattleEventKind.Hit &&
                    e.Detail == BeastKits.PulseDetail).GroupBy(e => e.TargetId).ToDictionary(g => g.Key, g => g.First().Amount);
                Dictionary<string, int> first = Shares(pulses[0]), third = Shares(pulses[2]);
                string same = first.Keys.Where(third.ContainsKey).OrderBy(k => k, StringComparer.Ordinal).First();
                if (raid) Assert.Less(first[same], third[same], "it climbs");
                else Assert.AreEqual(first[same], third[same], "the old rules keep it flat");
            }
        }

        [Test]
        public void ACampaignSharkFightBringsItsPupsFromRules27AndPlaysBack()
        {
            foreach (int cap in new[] { Campaign.BeastSceneRules, Campaign.BeastRaidRules })
            {
                var t = new CampaignTuning { RulesCap = cap };
                Campaign c = Campaign.New(43, t);
                Assert.IsTrue(CampaignPilot.PlayTutorial(c, PilotStyle.Reference, 43, 10), "the tutorial was never won");
                c.StartRun();
                CampaignFight fight = c.StartBeast();
                bool raid = cap >= Campaign.BeastRaidRules;
                Assert.AreEqual(raid, fight.Setup.Fight.BeastRaid);
                int pups = fight.Setup.Rules(1).Beast == BeastKit.SandShark && raid ? BeastKits.PupCount : 0;
                Assert.AreEqual(BeastKits.PartCount + pups, fight.Setup.Side(1).Count);
                CampaignPilot.PlayFight(c, fight, new CampaignPilot(PilotStyle.Reference, 43));
                Assert.IsTrue(c.Archive.TryReplay(fight.Setup.FightId, out _, out string failure), failure);
                Campaign read = CampaignSave.Read(CampaignSave.Write(c), t);
                Assert.IsEmpty(read.LoadNote);
                Assert.IsTrue(read.Archive.TryReplay(fight.Setup.FightId, out _, out failure), failure);
            }
        }
    }
}
