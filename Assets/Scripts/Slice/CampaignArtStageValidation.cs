using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using UnityEngine;

namespace KindredSiege.Slice
{
    public sealed partial class CampaignArtLoop
    {
        // One fixed road fight and one chief. Exercise the player's normal timed Update,
        // buttons, figures and replay builder with a disposable profile.
        void StageCapture(string name)=>ScreenCapture.CaptureScreenshot(Path.Combine(_capture,name+".png"));

        IEnumerator ValidateStage40()
        {
            yield return null;
            Check(ReviewRulesCap==40,"launcher selects campaign rules 40");
            _auto=false;Camera.main.targetTexture=null;
            _canvas.renderMode=RenderMode.ScreenSpaceOverlay;_canvas.worldCamera=null;
            _campaign=FreshRoadPreview();SetPage(Page.Offers);yield return null;
            Check(Click("offer-0"),"enter the stage road node from the map");yield return null;
            Check(Click("start-fight"),"enter the new fight from the squad");
            yield return RealtimeQaWaitForBattle();
            Check(ViewSim.StageOn&&_speed==1&&!_pilotOn,"stage road opens at 1x with live input");
            var trace=new StringBuilder("tick,kind,actor,target,startSeconds\n");
            double start=Time.realtimeSinceStartupAsDouble;
            StageBeat last=null;
            Figure witness=null;
            long heldTick=-1;float witnessPose=0;
            int holds=0,sameTick=0,heldFrames=0,contactFrames=0;
            bool clicked=false,paused=false,captured=false;
            while(!ViewOver||StageHolding)
            {
                if(Time.realtimeSinceStartupAsDouble-start>280)throw new InvalidOperationException("Stage fight timed out");
                if(StageHolding)
                {
                    if(last!=_stageBeat)
                    {
                        if(ViewSim.CurrentTick==heldTick)sameTick++;
                        heldTick=ViewSim.CurrentTick;holds++;
                        last=_stageBeat;witness=_figures.First(f=>StageBystander(f));
                        witnessPose=witness.poseTime;
                        var e=last.blow;
                        trace.AppendLine(heldTick+","+e.Kind+","+e.ActorId+","+e.TargetId+","+Time.realtimeSinceStartupAsDouble.ToString("F3",System.Globalization.CultureInfo.InvariantCulture));
                    }
                    else if(ViewSim.CurrentTick!=heldTick||witness.poseTime!=witnessPose)
                        throw new InvalidOperationException("Sim or uninvolved pose advanced during a stage hold");
                    heldFrames++;
                    if(_stageActor?.pit!=null&&_stageActor.drawnClip==_stageActor.attack&&_stageLanded)
                    {
                        if(_stageActor.drawnFrame==_stageActor.pit.segments[_stageActor.pit.segment].strike)contactFrames++;
                        if(!captured&&_stageBeat.blow.Kind==BattleEventKind.Hit&&_stageTarget!=null&&
                            !_stageActor.unit.Stats.Ranged&&_stageContactFrame==Time.frameCount)
                        {
                            var gap=_stageActor.root.anchoredPosition-_stageTarget.root.anchoredPosition;
                            Check(Mathf.Abs(gap.x)<90&&Mathf.Abs(gap.y)<4,"visible melee contact reaches the actual target lane");
                            Check(_stageActor.drawnFrame==_stageActor.pit.segments[_stageActor.pit.segment].strike,
                                "the receipt and authored strike frame land together");
                            StageCapture("stage40-contact");captured=true;
                        }
                    }
                    if(!paused&&_stageActor?.pit!=null&&!_stageLanded)
                    {
                        StageCapture("stage40-lead");
                        Check(Click("pause")&&_paused,"pause button suspends active stage beat");
                        float age=_stageAge;
                        yield return new WaitForSecondsRealtime(.2f);
                        Check(age==_stageAge&&heldTick==ViewSim.CurrentTick,"paused hold preserves pose clock and sim tick");
                        Check(Click("pause")&&!_paused,"resume stage beat");
                        var reload=CampaignSave.Read(CampaignSave.Write(_campaign),ReviewTuning);
                        Check(reload.Active.Sim.OrderedStateSnapshot==ViewSim.OrderedStateSnapshot,"save during hold restores the exact sim state");
                        paused=true;
                    }
                    if(!clicked&&ViewSim.CanUse(0,Directive.Hold,out _)&&!ViewOver)
                    {
                        int count=_fight.Session.Log.Commands.Count;
                        Check(Click("order-0")&&_fight.Session.Log.Commands.Count==count+1&&_fight.Session.Log.Commands.Last().Tick==heldTick,
                            "live Hold button logs its command on the held tick");
                        clicked=true;
                    }
                }
                yield return null;
            }
            Check(holds>5&&heldFrames>holds*3&&clicked&&paused&&captured&&contactFrames>0,"road reaches result with isolated animated strikes");
            Check(sameTick>0,"separate blows on the same sim tick each received a turn on stage");
            Check(!StageImpact(new BattleEvent(0,BattleEventKind.Hit,1,"",-1,"target",0,4,"bleed")),
                "actorless damage receives no stage hold");
            var rotations=_figures.Where(f=>f.pit!=null).ToDictionary(f=>f.unit.Id,f=>f.pit.nextSegment);
            Log("Stage road elapsed wall seconds including QA pause and capture: "+(Time.realtimeSinceStartupAsDouble-start).ToString("F2"));
            File.WriteAllText(Path.Combine(_capture,"stage-beats.csv"),trace.ToString());
            Log("Stage road: "+holds+" holds, "+sameTick+" queued on an already-held tick, "+heldFrames+" hold frames, "+contactFrames+" on authored contact frames");
            ShowResult();
            var archive=_campaign.Archive.Fights.Last();
            WatchFight(archive);yield return RealtimeQaWaitForBattle();
            _paused=false;_speed=4;
            int frames=0;
            while(!StageHolding&&frames++<600)yield return null;
            Check(StageHolding,"rules40 replay schedules its own stage holds");
            SeekPlayback(archive.Ticks);yield return RealtimeQaWaitForBattle();
            Check(!StageHolding&&_stageQueue.Count==0&&_playback.Matched,"seek during a hold discards presentation and verifies checksum");
            Check(_figures.Where(f=>f.pit!=null).All(f=>rotations[f.unit.Id]==f.pit.nextSegment),
                "instant end seek preserves the live authored segment rotation");
            SeekPlayback(0);yield return RealtimeQaWaitForBattle();
            Check(_playback.Tick==0&&!StageHolding,"restart opens at tick zero without a stale beat");

            _playback=null;_campaign=FreshRaidPreview();_fight=_campaign.Active;BeginFight();
            yield return RealtimeQaWaitForBattle();
            _speed=4;
            bool parried=false,frozenFuse=false;
            start=Time.realtimeSinceStartupAsDouble;
            while(!ViewOver&&!parried&&Time.realtimeSinceStartupAsDouble-start<160)
            {
                var fuse=_pitFuses.FirstOrDefault(f=>f.seen&&f.threat.RealBeatTicks>0);
                if(StageHolding&&fuse!=null&&!frozenFuse)
                {
                    int left=fuse.threat.RealBeatTicks;var beat=_stageBeat;
                    yield return null;
                    if(_stageBeat==beat)
                    {
                        Check(fuse.threat.RealBeatTicks==left,"chief parry fuse stops during stage hold");
                        StageCapture("stage40-chief-fuse");frozenFuse=true;
                    }
                }
                if(frozenFuse&&ViewSim.Threats(0).Any(t=>t.ParryWindow)&&ViewSim.CanUse(0,Directive.Hold,out _))
                {
                    int bars=ViewSim.ParryBars(0);
                    Check(Click("order-0"),"press Hold on a real rules40 parry window");
                    for(int i=0;i<300&&!ViewOver&&ViewSim.ParryBars(0)==bars;i++)yield return null;
                    parried=ViewSim.ParryBars(0)>bars;
                }
                yield return null;
            }
            Check(frozenFuse&&parried,"real chief parry pays its meter after stage hold");
            StageCapture("stage40-chief-parry");
            _paused=true;
        }
    }
}
