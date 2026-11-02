using System.Linq;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using NUnit.Framework;

namespace KindredSiege.Sim.Tests
{
    /// The hunters arrive knowing your parry (Jacob, 14 Oct 2026: "yes go", after Claude's checks found the parry won every
    /// hunt because the hunters' habits are a fixed list). Like Hold on Unleash, they know it every run, from the start.
    public class HunterHabitTests
    {
        [Test]
        public void TheHuntersWaitOutYourParryFromTheirFirstHunt()
        {
            const ulong seed = 920;
            Campaign c = Campaign.New(seed, new CampaignTuning());
            Assert.IsTrue(c.Mind(AlphaHunt.Id).HasHabit(Habit.WaitOutParry), "they know it before any hunt");
            Assert.IsTrue(CampaignSave.Read(CampaignSave.Write(c)).Mind(AlphaHunt.Id).HasHabit(Habit.WaitOutParry), "and after a reload");
            Assert.IsTrue(CampaignPilot.PlayTutorial(c, PilotStyle.Reference, seed, 10), "the tutorial was never won");
            c.ChooseTribe(CampaignTribes.BloodridgeId);
            // A saved-state hunt fixture: exercise the actual hunt setup without requiring a bot to win a road.
            string saved = string.Join("\n", CampaignSave.Write(c).Split('\n')
                .Where(l => !l.StartsWith("offer\t"))
                .Select(l => l.StartsWith("run\t") ? "run\t" + AlphaHunt.Id + "\t0\t-1" :
                    l == "end" ? "offer\t" + (int)EncounterKind.AlphaHunt + "\nend" : l));
            c = CampaignSave.Read(saved);
            Assert.IsEmpty(c.LoadNote);
            CampaignFight hunt = c.StartEncounter(0);
            Assert.IsTrue(hunt.Sim.WaitsOutParry(1));
            while (!hunt.Over && hunt.Sim.Events.All(e => e.Kind != BattleEventKind.Release))
            {
                // Inside the parry window: 0.2 s from rules 26 (the tight parry).
                if (hunt.Sim.HeavyTicks(1) == 4 && hunt.Sim.HoldTicksLeft(0) == 0) hunt.TryOrder(Directive.Hold, null, out _);
                hunt.Tick();
            }
            Assert.IsTrue(hunt.Sim.Events.Any(e => e.Detail == BattleSimulation.WaitOutDetail), "the first heavy swing waited out the Hold");
            // From rules 26 the Hold it waited out is a whiff by the swing: it lands in full.
            Assert.AreEqual(Moves.Full, hunt.Sim.Events.First(e => e.Kind == BattleEventKind.Release).Detail, "so the stale Hold whiffs");
        }
    }
}
