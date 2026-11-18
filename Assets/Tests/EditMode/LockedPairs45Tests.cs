using System.Linq;
using KindredSiege.Sim;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using NUnit.Framework;

namespace KindredSiege.Sim.Tests
{
    public class LockedPairs45Tests
    {
        static UnitSpec U(string id, UnitClass c, Row row = Row.Front) => new UnitSpec(id, c, row, UnitRole.Fighter, 100, 10000);
        static UnitSpec[] Squad(string p) => new[] { U(p+"w",UnitClass.Warden),U(p+"s",UnitClass.Skirmisher),U(p+"m",UnitClass.Marksman,Row.Back),U(p+"t",UnitClass.Trapper,Row.Back),U(p+"h",UnitClass.Medic,Row.Back) };
        static FightRules Rules => FightRules.Campaign.WithLayers().WithTable().WithLeaders().WithGrid().WithLockedPairs();
        static BattleSimulation Make(FightRules rules = null) => new BattleSimulation(new BattleSetup("pairs45", 7, Squad("a"),Squad("b"),new TacticsProfile(),new TacticsProfile(),BattleSetup.DefaultTickLimit,20,NerveRules.Off,10,100,SideRules.None,SideRules.None,rules??Rules));
        static void Play(BattleSimulation s,int ticks) { for(int i=0;i<ticks&&!s.IsFinished;i++)s.Tick(); }

        [Test] public void OpponentsAreMutualAndGuardsAndHealersStayOutsidePairs()
        {
            var s=Make();Play(s,100);
            Assert.IsTrue(s.LockedPairsOn);
            foreach(var u in s.Units)
            {
                string id=s.PairedOpponentId(u);
                if(u.Class==UnitClass.Warden||u.Class==UnitClass.Medic) Assert.IsEmpty(id);
                if(id.Length>0) Assert.AreEqual(u.Id,s.PairedOpponentId(s.Units.First(v=>v.Id==id)));
            }
            Assert.IsTrue(s.Units.Any(u=>s.PairedOpponentId(u).Length>0));
        }
        [Test] public void LivingPairsAlternateAndTheirWindupsRunConcurrently()
        {
            var s=Make();Play(s,1000);
            var swings=s.Events.Where(e=>e.Kind==BattleEventKind.PairWindup).ToArray();
            Assert.Greater(swings.Length,10);
            foreach(var g in swings.GroupBy(e=>e.Detail))
            { var a=g.ToArray();for(int i=1;i<a.Length;i++)Assert.AreNotEqual(a[i-1].ActorId,a[i].ActorId,"pair "+g.Key); }
            Assert.Greater(swings.GroupBy(e=>e.Tick).Max(g=>g.Count()),1,"pairs must not join one global queue");
        }
        [Test] public void ReadoutsDoNotChangeStateAndTheSameFightReplaysExactly()
        {
            var a=Make();var b=Make();
            for(int i=0;i<1200;i++) { a.Blows(0);a.Blows(1);foreach(var u in a.Units){a.PairedOpponentId(u);a.PairHasTurn(u);a.PairWindupTicks(u);}a.Tick();b.Tick(); }
            Assert.AreEqual(b.OrderedStateSnapshot,a.OrderedStateSnapshot);Assert.AreEqual(b.EventHash,a.EventHash);
        }
        [Test] public void OlderRulesHaveNoPairStateOrPairEvents()
        {
            var s=Make(FightRules.Campaign.WithLayers().WithTable().WithLeaders().WithGrid());Play(s,500);
            Assert.IsFalse(s.LockedPairsOn);Assert.IsFalse(s.OrderedStateSnapshot.Contains("rules.lockedPairs"));
            Assert.IsFalse(s.Events.Any(e=>e.Kind==BattleEventKind.Pair||e.Kind==BattleEventKind.PairWindup));
        }
        [Test] public void BuildersKeepTheNewFlag()
        {
            foreach(var method in typeof(FightRules).GetMethods().Where(m=>m.Name.StartsWith("With")&&m.GetParameters().Length==0))
                Assert.IsTrue(((FightRules)method.Invoke(Rules,null)).LockedPairs,method.Name);
        }
        [Test] public void FallBackReleasesTheLocksAndTheyReformAfterTheOrder()
        {
            var s=Make();var session=new SimSession(s);Play(s,100);
            Assert.IsTrue(s.Units.Any(u=>s.PairedOpponentId(u).Length>0));
            Assert.IsTrue(session.TryIssue(Directives.CommandKind,"0","",0,0,"FallBack",out string why),why);
            session.Tick();
            Assert.IsTrue(s.Events.Any(e=>e.Kind==BattleEventKind.Pair&&e.Detail==PairRules.Ended));
            Assert.IsFalse(s.Units.Any(u=>s.PairedOpponentId(u).Length>0));
            Play(s,600);Assert.IsTrue(s.Units.Any(u=>s.PairedOpponentId(u).Length>0));
        }
        [Test] public void CurrentSaveAndArchiveKeepPairsAcrossReloadSeekAndRestart()
        {
            var c=Campaign.New(5100);Assert.IsTrue(CampaignPilot.PlayTutorial(c,PilotStyle.Reference,5100,10));c.StartRun();
            var f=c.StartEncounter(0);for(int i=0;i<120&&!f.Over;i++)f.Session.Tick();
            string save=CampaignSave.Write(c);var reload=CampaignSave.Read(save);
            Assert.AreEqual(f.Sim.OrderedStateSnapshot,reload.Active.Sim.OrderedStateSnapshot);
            Assert.IsTrue(reload.Active.Sim.LockedPairsOn);
            CampaignPilot.PlayFight(c,f,new CampaignPilot(PilotStyle.Reference,99));
            string id=f.Setup.FightId;
            Assert.IsTrue(FightPlayback.TryOpen(c.Archive,id,out var p,out string why),why);
            Assert.IsTrue(p.Seek(p.Length/2),p.Failure);Assert.IsTrue(p.Seek(0),p.Failure);Assert.IsTrue(p.Seek(p.Length),p.Failure);
            Assert.IsTrue(p.Matched,p.Failure);p.Restart();while(p.Step()){}Assert.IsTrue(p.Matched,p.Failure);
        }
        [Test] public void ACurrentRoadFightDoesNotFreezeAfterExchangesLoseMembers()
        {
            const ulong seed=5100;var c=Campaign.New(seed,new CampaignTuning{EnemyScaleTier1=40});
            Assert.IsTrue(CampaignPilot.PlayTutorial(c,PilotStyle.Reference,seed,10));c.StartRun();var picker=new CampaignPilot(PilotStyle.Reference,seed);
            for(int n=0;c.Stage==CampaignStage.InRun&&n<12;n++)
            {
                var f=c.StartEncounter(picker.PickOffer(c));CampaignPilot.PlayFight(c,f,new CampaignPilot(PilotStyle.Reference,seed*31UL+(ulong)n));
                if(f.Setup.FightId.Contains("holdtheline"))continue;
                Assert.AreNotEqual(FightEnd.TimeUp,f.Sim.Ending.How,f.Setup.FightId+"\n"+string.Join("\n",f.Sim.Units.Select(u=>u.Id+" "+u.Class+" hp="+u.Hp+" panel="+u.GridCol+","+u.GridLane+" pair="+f.Sim.PairedOpponentId(u)+" turn="+f.Sim.PairHasTurn(u)+" cd="+u.Cooldown)));
            }
        }
    }
}
