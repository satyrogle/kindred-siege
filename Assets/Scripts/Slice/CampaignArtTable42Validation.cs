using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KindredSiege.Sim;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using UnityEngine;

namespace KindredSiege.Slice
{
    public sealed partial class CampaignArtLoop
    {
        void TableCapture(string name)
        {
            var mode=_canvas.renderMode;var camera=_canvas.worldCamera;var target=Camera.main.targetTexture;
            try
            {
                Camera.main.targetTexture=_captureTarget;_canvas.renderMode=RenderMode.ScreenSpaceCamera;
                _canvas.worldCamera=Camera.main;_canvas.planeDistance=1;Capture(name);
            }
            finally{Camera.main.targetTexture=target;_canvas.renderMode=mode;_canvas.worldCamera=camera;}
        }
        IEnumerator ValidateTable42()
        {
            yield return null;
            Check(_table42Review&&ReviewRulesCap==42,"review launcher selects rules42 and isolated review art");
            _auto=false;Camera.main.targetTexture=null;_canvas.renderMode=RenderMode.ScreenSpaceOverlay;_canvas.worldCamera=null;
            _campaign=FreshRoadPreview();SetPage(Page.Offers);yield return null;
            Check(Click("offer-0"),"road offer opens squad");yield return null;
            SelectControl("start-fight");HandleUiKey(KeyCode.Return);
            yield return RealtimeQaWaitForBattle();
            Check(_page==Page.Fight&&TablePresentation&&!ViewSim.StageOn&&!StageHolding,"keyboard fight entry builds rules42 without stage holds");
            Check(_speed==1&&!_pilotOn,"new fight starts 1x with player control");
            foreach(var f in _figures.Where(f=>f.artKey!=null&&HasTableUnit(f.artKey+"_idle")))
                Check(f.table!=null&&f.block!=null&&f.table.power!=null&&f.idle.texture.name==f.artKey+"_idle","review unit has native idle, defence and power clips: "+f.unit.Id);
            var start=Time.realtimeSinceStartupAsDouble;int frames=0,held=0;bool paused=false,captured=false;
            long oldTick=ViewSim.CurrentTick;float maxFrame=0;var samples=new List<float>();
            // One blow at a time (CampaignArtBeats.cs, 8 Nov 2026) holds the sim while each blow plays, so a road fight
            // takes several times its sim length on screen.
            while(!ViewOver)
            {
                if(Time.realtimeSinceStartupAsDouble-start>900)throw new InvalidOperationException("rules42 road exceeded QA timeout");
                frames++;samples.Add(Time.unscaledDeltaTime*1000);maxFrame=Mathf.Max(maxFrame,Time.unscaledDeltaTime*1000);
                if(BeatHolding)held++;
                oldTick=ViewSim.CurrentTick;
                if(!captured&&_tableWords.Count>0){TableCapture("table42-road-feedback");captured=true;}
                if(!paused&&ViewSim.CurrentTick>50)
                {
                    Check(Click("pause")&&_paused,"pause via real control");long tick=ViewSim.CurrentTick;float clock=_tableClock;
                    yield return new WaitForSecondsRealtime(.2f);
                    Check(ViewSim.CurrentTick==tick&&_tableClock==clock,"pause stops sim and presentation clocks");
                    var reload=CampaignSave.Read(CampaignSave.Write(_campaign),ReviewTuning);
                    Check(reload.Active.Sim.OrderedStateSnapshot==ViewSim.OrderedStateSnapshot,"save reload preserves rules42 current fight");
                    Check(Click("pause")&&!_paused,"resume via real control");paused=true;
                }
                yield return null;
            }
            samples.Sort();Log("rules42 road wall="+(Time.realtimeSinceStartupAsDouble-start).ToString("F2")+"s sim="+(ViewSim.CurrentTick/30f).ToString("F2")+"s frames="+frames+" frame median="+samples[samples.Count/2].ToString("F2")+"ms p95="+samples[(int)(samples.Count*.95)].ToString("F2")+"ms max="+maxFrame.ToString("F2")+"ms");
            Check(_tableHitsSeen>0&&held>0&&captured,"landed blows render one blow at a time while the sim waits");
            Log("road hits="+_tableHitsSeen+" dodge="+_tableDodgesSeen+" parry="+_tableParriesSeen+" block="+_tableBlocksSeen+" crit="+_tableCritsSeen);
            ShowResult();var road=_campaign.Archive.Fights.Last();WatchFight(road);yield return RealtimeQaWaitForBattle();
            SeekPlayback(road.Ticks);yield return RealtimeQaWaitForBattle();
            Check(_playback.Matched&&_chiefReceipts.Count==0&&_reviewFx.Count==0,"rules42 end seek matches checksum without stale FX");
            SeekPlayback(0);yield return RealtimeQaWaitForBattle();Check(_playback.Tick==0,"rules42 restart returns to zero");

            // Presenter-only probes: explicit events, no sim mutation or claims about random outcomes.
            _paused=true;var a=_figures.First(f=>f.unit.Side==1&&f.table!=null);var t=_figures.First(f=>f.unit.Side==0&&f.unit.Class==UnitClass.Warden);
            string before=ViewSim.OrderedStateSnapshot;int seq=0;
            foreach(string kind in new[]{"dodge","parry","block","crit"})
            {
                var ev=new BattleEvent(900+seq,BattleEventKind.Miss,a.unit.Side,a.unit.Id,(int)a.unit.Class,t.unit.Id,(int)t.unit.Class,10,kind);
                if(kind=="block"||kind=="crit")
                {
                    AnimateEvent(new BattleEvent(ev.Tick,BattleEventKind.Struck,ev.ActorSide,ev.ActorId,ev.ActorClass,ev.TargetId,ev.TargetClass,5,kind));
                    AnimateEvent(new BattleEvent(ev.Tick,BattleEventKind.Hit,ev.ActorSide,ev.ActorId,ev.ActorClass,ev.TargetId,ev.TargetClass,10,"attack"));
                }
                else AnimateEvent(ev);
                DrawBattle();Check(_tableWords[t.unit.Id].image.texture.name==kind,"presenter probe routes "+kind+" word");
                Check(_reviewFx.Any(f=>f.clip.texture.name==kind),"presenter probe routes "+kind+" authored FX");
                if(kind=="parry"||kind=="block")Check(t.table.current==t.block,"defensive pose for "+kind);
                TableCapture("probe-"+kind);yield return null;seq++;
            }
            Check(before==ViewSim.OrderedStateSnapshot,"all feedback probes leave authoritative state unchanged");
            _tableRecent.Clear();_tableImpacts.Clear();
            var targets=_figures.Where(f=>f.table!=null).Take(3).ToArray();
            for(int i=0;i<3;i++)_tableImpacts.Add(new TableImpact{target=targets[i],at=_tableClock,amount=10+i,hold=i==2?.2f:.06f,crit=i==2});
            ResolveTableImpacts();
            Check(targets[2].table.whiteFrame==Time.frameCount&&targets.Take(2).All(f=>f.table.whiteFrame<0&&f.table.holdUntil<0),"three hits within100ms only freeze and flash the strongest");

            _playback=null;_campaign=FreshRaidPreview();_fight=_campaign.Active;BeginFight();
            yield return RealtimeQaWaitForBattle();_paused=false;_speed=1;
            bool linked=false,parried=false;float chiefDeadline=Time.realtimeSinceStartup+400;
            while(!ViewOver&&!parried&&Time.realtimeSinceStartup<chiefDeadline)
            {
                var fuse=_pitFuses.FirstOrDefault(f=>f.seen&&f.threat.RealBeatTicks>0);
                var leader=FindFigure(Campaign.LeaderId);
                if(fuse!=null&&!linked&&leader!=null)
                {
                    Check(Mathf.Abs(fuse.dots[0].rectTransform.anchoredPosition.x-leader.feet.x)<2&&fuse.link!=null,
                        "parry floor circle and mind link use the leader while the threat retains its real target");
                    TableCapture("leader-parry-warning");linked=true;
                }
                if(linked&&ViewSim.Threats(0).Any(x=>x.ParryWindow)&&ViewSim.CanUse(0,Directive.Hold,out _))
                {
                    int bars=ViewSim.ParryBars(0);Check(Click("order-0"),"live Hold on authoritative chief parry window");
                    for(int i=0;i<180&&!ViewOver&&ViewSim.ParryBars(0)==bars;i++)yield return null;
                    parried=ViewSim.ParryBars(0)>bars;
                }
                yield return null;
            }
            Check(linked&&parried,"leader parry earns its real meter without staging combat");TableCapture("leader-parry-success");_paused=true;

            IntegrationCap(41);_playback=null;_campaign=FreshRoadPreview();_fight=_campaign.StartEncounter(0);
            while(!_fight.Over)_fight.Tick();_campaign.Finish(_fight);var old=_campaign.Archive.Fights.Last();
            WatchFight(old);yield return RealtimeQaWaitForBattle();
            Check(!ViewSim.StageOn&&!ViewSim.Fight.Table&&!TablePresentation,"rules41 retains simultaneous old-table playback");
            SeekPlayback(old.Ticks);yield return RealtimeQaWaitForBattle();Check(_playback.Matched,"rules41 end checksum matches");
            IntegrationCap(42);
            foreach(var beast in new[]{BeastKit.SandShark,BeastKit.CageBear})
            {
                _playback=null;_campaign=PitBeastFixture(beast);int offer=_campaign.Offers.ToList().FindIndex(x=>x.Beast==beast);
                _fight=_campaign.StartEncounter(offer);while(!_fight.Over)_fight.Tick();_campaign.Finish(_fight);var archive=_campaign.Archive.Fights.Last();
                WatchFight(archive);yield return RealtimeQaWaitForBattle();Check(ViewSim.Fight.Table,"current beast uses rules42: "+beast);
                SeekPlayback(Math.Min(150,archive.Ticks));yield return RealtimeQaWaitForBattle();TableCapture("beast42-"+beast);
                SeekPlayback(archive.Ticks);yield return RealtimeQaWaitForBattle();Check(_playback.Matched,"current beast seek and checksum: "+beast);
            }

            // Fixed artificial spell fixtures exercise both casting directions. Increased fixture HP
            // keeps every authored layer observable; no CampaignTuning or production values change.
            foreach(ChiefSpell spell in Enum.GetValues(typeof(ChiefSpell)))for(int side=0;side<2;side++)
            {
                _playback=null;_fight=null;_campaign=FreshRoadPreview();
                var setup=TableSpellSetup(spell,side);var sim=new BattleSimulation(setup);var session=new SimSession(sim);
                while(!sim.IsFinished)session.Tick();
                var archive=_campaign.Archive.Add(setup,session.Log,sim);
                Check(_campaign.Archive.TryReplay(archive.FightId,out _,out _),"fixture checksum "+spell+" side"+side);
                var windup=sim.Events.FirstOrDefault(e=>e.Kind==BattleEventKind.Windup&&e.ActorSide==side&&e.Detail==ChiefSpells.Name(spell));
                Check(windup.Kind==BattleEventKind.Windup,"fixture casts "+spell+" side"+side);
                WatchFight(archive);yield return RealtimeQaWaitForBattle();
                // Start just before Windup so the actual recorded event owns the tell lifecycle.
                SeekPlayback(Math.Max(0,windup.Tick-1));yield return RealtimeQaWaitForBattle();_paused=false;_speed=1;
                // One blow at a time (CampaignArtBeats.cs) plays the blows around a tell in turn, so the release comes
                // later than 3 s on screen: wait for it, then watch the same 3.4 s tail as the old fixed 7.2 s window.
                float until=Time.realtimeSinceStartup+90f;bool seen=false,contact=false;float releaseSeen=-1,tailUntil=-1;
                // A lethal release can end the archive before its authored impact frame.
                // Playback deliberately continues presentation time at the end; observe that tail.
                while(Time.realtimeSinceStartup<until&&(tailUntil<0||_visualTime<tailUntil))
                {
                    var cast=_chiefCasts[side];
                    if(spell==ChiefSpell.Firebomb&&_numbers.Any(n=>n.word=="ECLIPSE"))
                        throw new InvalidOperationException("Firebomb receipt inherited the core's Eclipse damage label");
                    if(cast!=null&&cast.release<0&&!seen){seen=true;TableCapture("spell-"+spell+"-"+side+"-tell");}
                    if(cast!=null&&cast.release>=0&&releaseSeen<0)releaseSeen=_visualTime;
                    if(releaseSeen>=0&&_visualTime-releaseSeen>.8f&&!contact)
                    {
                        contact=true;tailUntil=_visualTime+3.4f;TableCapture("spell-"+spell+"-"+side+"-hit");
                        if(spell==ChiefSpell.SteelStorm)Check(!_wallPanels[side].gameObject.activeSelf,"authored domes replace placeholder shield column side"+side);
                    }
                    yield return null;
                }
                Log("spell probe "+spell+" side"+side+" tick="+_playback.Tick+" releases="+ViewSim.Events.Count(e=>e.Kind==BattleEventKind.Release&&e.ActorSide==side)+" impacts="+_chiefFxImpacts);
                Check(seen&&contact,"real recorded windup and release rendered "+spell+" side"+side);
                if(spell!=ChiefSpell.SteelStorm&&spell!=ChiefSpell.WarCry)
                    Check(_chiefFxImpacts>0,"spell receipts reach authored contact "+spell+" side"+side);
                SeekPlayback(windup.Tick+15);yield return RealtimeQaWaitForBattle();
                Check(_chiefCasts[side]!=null&&_chiefCasts[side].tells.Count>0&&_chiefReceipts.Count==0,"seek restores warning without past impacts "+spell+" side"+side);
                SeekPlayback(archive.Ticks);yield return RealtimeQaWaitForBattle();
                Check(_playback.Matched&&_chiefReceipts.Count==0,"spell end seek clears effects and keeps checksum "+spell+" side"+side);
            }
            Log("Table42 QA complete; native input, visual inspection and listening are separate evidence.");
        }
        BattleSetup TableSpellSetup(ChiefSpell spell,int casterSide)
        {
            var sides=new List<UnitSpec>[2]{new List<UnitSpec>(),new List<UnitSpec>()};
            for(int side=0;side<2;side++)
            {
                sides[side].Add(new UnitSpec(side==0?Campaign.LeaderId:"bloodridge.chief",UnitClass.Warden,Row.Back,side==0?UnitRole.Pal:UnitRole.Chief,100,800));
                foreach(var cls in new[]{UnitClass.Warden,UnitClass.Skirmisher,UnitClass.Trapper,UnitClass.Marksman,UnitClass.Medic})
                    sides[side].Add(new UnitSpec((side==0?CampaignTribes.ClarthId:CampaignTribes.BloodridgeId)+".qa."+cls,cls,cls==UnitClass.Warden||cls==UnitClass.Skirmisher?Row.Front:Row.Back,UnitRole.Fighter,100,800,100,0,0,0,0,0,0,0,10));
            }
            var rule=new SideRules(1,140,false,spell:spell);
            return new BattleSetup("camp."+(100+(int)spell*2+casterSide)+"."+CampaignTribes.BloodridgeId+".Chief",7,sides[0],sides[1],new TacticsProfile(),new TacticsProfile(),420,20,NerveRules.Off,30,100,
                casterSide==0?rule:SideRules.None,casterSide==1?rule:SideRules.None,FightRules.Campaign.WithRoadCombat().WithRoadKit().WithParryCenter().WithSquadFirst().WithPit().WithTable());
        }
    }
}
