using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using NUnit.Framework;

namespace KindredSiege.Sim.Tests
{
    public class Pals30IntegrationTests
    {
        [Test]
        public void Released26CannotAcquireCloudPalRulesAndWithMethodsKeepAllLaterSwitches()
        {
            Assert.AreEqual(25,Campaign.BeastSceneRules);
            Assert.AreEqual(26,Campaign.TightParryRules);
            Assert.AreEqual(27,Campaign.BeastRaidRules);
            Assert.AreEqual(28,Campaign.ShadeFormsRules);
            Assert.AreEqual(29,Campaign.GluttonTreeRules);
            Assert.AreEqual(30,Campaign.PalDuoRules);
            var c=Campaign.New(6600,new CampaignTuning{RulesCap=26,EnemyScaleTier1=40});
            Assert.IsTrue(CampaignPilot.PlayTutorial(c,PilotStyle.Reference,6600,10));c.StartRun();
            var fight=c.StartEncounter(0);
            Assert.IsTrue(fight.Sim.TightParryOn);
            Assert.IsFalse(fight.Sim.ShadeFormsOn||fight.Sim.GluttonOn||fight.Sim.DuosOn||fight.Sim.BeastRaidOn);
            var rules=FightRules.Campaign.WithFloor().WithShadeForms().WithGlutton().WithDuos().WithBeastScenes().WithTight().WithBeastRaid().WithWhisper();
            Assert.IsTrue(rules.ShadeForms&&rules.Glutton&&rules.Duos&&rules.BeastScenes&&rules.Tight&&rules.BeastRaid&&rules.Whisper);
        }

        [Test]
        public void PrecognitionThreatCueAndAcceptedHoldUseTheSameActiveTightWindow()
        {
            var levels=new int[GluttonRules.TalentCount+1];levels[(int)GluttonTalent.Precognition]=1;
            var kit=PalKit.None.WithTalents(levels);
            UnitSpec U(string id,UnitClass cls)=>new UnitSpec(id,cls,SliceClasses.Stats(cls).DefaultRow,UnitRole.Fighter,100,3000);
            var ours=new List<UnitSpec>{new UnitSpec("pal",UnitClass.Marksman,Row.Back,UnitRole.Pal,100,3000),U("s",UnitClass.Skirmisher),U("m",UnitClass.Marksman),U("t",UnitClass.Trapper)};
            var theirs=new List<UnitSpec>{U("foe",UnitClass.Skirmisher),new UnitSpec("chief",UnitClass.Warden,Row.Front,UnitRole.Chief,100,8000)};
            var rules=new FightRules(true,true,true,true,true,true,true,false,true).WithParryCenter().WithLayers().WithFloor().WithGlutton().WithTight();
            var sim=new BattleSimulation(new BattleSetup("precognition.tight",41,ours,theirs,new TacticsProfile(),new TacticsProfile(),BattleSetup.DefaultTickLimit,BattleSetup.DefaultDirectivePoints,
                NerveRules.On(NerveRules.Full,NerveRules.Full),10,400,new SideRules(1,0,true).WithPals(kit),new SideRules(2,0,false,0,ChiefSpell.Eclipse,kit:ChiefKit.Bloodridge,moves:ChiefMove.Cleave),rules));
            var session=new SimSession(sim);Threat beat=default;
            while(!sim.IsFinished&&sim.CurrentTick<1800)
            {beat=sim.Threats(0).FirstOrDefault(t=>t.Name==ChiefKits.CleaveName&&t.BeatTicks==9);if(!string.IsNullOrEmpty(beat.Name))break;session.Tick();}
            Assert.IsFalse(string.IsNullOrEmpty(beat.Name));Assert.AreEqual(9,sim.ParryWindowTicksFor(0));Assert.AreEqual(6,sim.ParryWindowTicksFor(1));
            Assert.AreEqual(9,beat.WindowTicks);Assert.IsTrue(beat.ParryWindow);
            Assert.IsTrue(session.TryIssue(Directives.CommandKind,"0","",0,0,"Hold",out string why),why);
            for(int i=0;i<11;i++)session.Tick();
            Assert.IsTrue(sim.Events.Any(e=>e.Kind==BattleEventKind.Parry&&e.ActorSide==0));
            Assert.IsFalse(sim.Events.Any(e=>e.Kind==BattleEventKind.Reaction&&e.Detail=="whiff"&&e.ActorSide==0));
        }

        [Test]
        public void EightChoiceOfferRoundTripsAndExportsOnlyWhenExplicitlyRequested()
        {
            var c=Campaign.New(6600,new CampaignTuning{EnemyScaleTier1=40});
            Assert.IsTrue(CampaignPilot.PlayTutorial(c,PilotStyle.Reference,6600,10));c.StartRun();
            var picks=Enum.GetValues(typeof(Favour)).Cast<Favour>().Where(FavourRules.IsCombo).Take(8).ToList();
            Assert.AreEqual(8,picks.Count);
            string text=CampaignSave.Write(c);int end=text.LastIndexOf("end\n",StringComparison.Ordinal);
            text=text.Insert(end,"favouroffer\t"+(int)Pal.Butcher+"\t0\t"+string.Join("\t",picks.Select(f=>(int)f))+"\n");
            var read=CampaignSave.Read(text,c.Tuning);
            Assert.IsEmpty(read.LoadNote);CollectionAssert.AreEqual(picks,read.FavourOffer.Choices);
            string output=Environment.GetEnvironmentVariable("KS_PALS30_FIXTURE_OUT");
            if(!string.IsNullOrEmpty(output)){Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"eight-picks.txt"),text);}
        }
    }
}
