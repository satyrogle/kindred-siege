using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using UnityEngine;

namespace KindredSiege.Slice
{
    public sealed partial class CampaignArtLoop
    {
        IEnumerator RecordPairs45()
        {
            yield return null;IntegrationCap(45);_learningSpeed=1;_campaign=FreshRoadPreview();SetPage(Page.Offers);yield return null;
            Check(Click("offer-0"),"record pair offer");yield return null;Check(Click("start-fight"),"record pair entry");_paused=true;_pilotOn=false;_speed=1;
            yield return RealtimeQaWaitForBattle();
            var encoder=TrailerEncoder(Argument("-ksReviewEncoder"),"pairs45");
            var trace=new System.Text.StringBuilder("frame,tick,pairs,attacking,reacting\n");
            var motion=new System.Text.StringBuilder("frame,tick,id,x,y,homeX,homeY,windup,left,clipFrame,blood\n");
            _paused=false;
            try
            {
                for(int i=0;i<600&&!ViewOver;i++)
                {
                    if(ViewSim.Threats(0).Any(t=>t.ParryWindow)&&ViewSim.HoldTicksLeft(0)==0&&ViewSim.CanUse(0,Directive.Hold,out _))Click("order-0");
                    TrailerFrame(encoder);
                    foreach(var f in _figures)if(PairBody(f))motion.Append(i).Append(',').Append(ViewSim.CurrentTick).Append(',').Append(f.unit.Id).Append(',')
                        .Append(f.root.anchoredPosition.x).Append(',').Append(-f.root.anchoredPosition.y).Append(',').Append(f.feet.x).Append(',').Append(f.feet.y).Append(',')
                        .Append(f.pair.coming?1:0).Append(',').Append(f.pair.left).Append(',').Append(f.drawnFrame).Append(',').Append(_pairBloodSeen).Append('\n');
                    trace.Append(i).Append(',').Append(ViewSim.CurrentTick).Append(',').Append(ViewSim.Units.Count(u=>ViewSim.PairedOpponentId(u).Length>0)/2).Append(',')
                        .Append(_figures.Count(f=>f.drawnClip==f.attack||f.table!=null&&(f.drawnClip==f.table.alternate||f.drawnClip==f.table.power))).Append(',')
                        .Append(_figures.Count(f=>f.drawnClip==f.hit||f.drawnClip==f.block)).Append('\n');
                    yield return null;
                }
            }
            finally{TrailerEncoderDone(encoder,"pairs45");}
            File.WriteAllText(Path.Combine(_capture,"pairs45-trace.csv"),trace.ToString());
            File.WriteAllText(Path.Combine(_capture,"physical45-motion.csv"),motion.ToString());
            Log("physical45 visible maxAdvancePx="+_pairMaxAdvance+" recoilPx="+_pairMaxRecoil+" bloodReceipts="+_pairBloodSeen);
            Check(ViewSim.Events.Any(e=>e.Kind==BattleEventKind.PairWindup),"recording contains real autonomous exchanges");
        }
        IEnumerator ValidatePairs45()
        {
            yield return null;
            IntegrationCap(45);_learningSpeed=1;_campaign=FreshRoadPreview();SetPage(Page.Offers);yield return null;
            Check(Click("offer-0"),"pairs45 offer opens squad");yield return null;
            SelectControl("start-fight");HandleUiKey(KeyCode.Return);_paused=true;_pilotOn=false;_speed=1;
            yield return RealtimeQaWaitForBattle();
            _pilotOn=false;_paused=false;_speed=1;
            _auto=false;Camera.main.targetTexture=null;_canvas.renderMode=RenderMode.ScreenSpaceOverlay;_canvas.worldCamera=null;
            Check(PairsPresentation&&TablePresentation&&!BeatsOn&&!ViewSim.StageOn,"pairs45 has concurrent presentation and authoritative exchanges");
            foreach(var f in _figures.Where(f=>f.artKey!=null&&f.artKey.StartsWith("clarth_")))
                Check(f.pair?.step!=null,"identity-matched movement loaded: "+f.unit.Id);
            float start=Time.realtimeSinceStartup,next=2;int parries=0,frames=0;float maxFrame=0,maxOverlap=0;var frameTimes=new List<float>();int captureCooldown=3;
            while(!ViewOver&&Time.realtimeSinceStartup-start<25)
            {
                frames++;if(captureCooldown>0)captureCooldown--;else{frameTimes.Add(Time.unscaledDeltaTime*1000);maxFrame=Mathf.Max(maxFrame,Time.unscaledDeltaTime*1000);}
                for(int a=0;a<_figures.Count;a++)for(int b=a+1;b<_figures.Count;b++)
                {
                    var first=_figures[a];var second=_figures[b];if(!PairBody(first)||!PairBody(second))continue;
                    var gap=second.pair.render-first.pair.render;
                    if(Mathf.Abs(gap.y)<78)maxOverlap=Mathf.Max(maxOverlap,PairWidth(first)+PairWidth(second)+6-Mathf.Abs(gap.x));
                }
                if(ViewSim.Threats(0).Any(t=>t.ParryWindow)&&ViewSim.HoldTicksLeft(0)==0&&ViewSim.CanUse(0,Directive.Hold,out _))
                {if(Click("order-0"))parries++;}
                float age=Time.realtimeSinceStartup-start;
                if(age>=next)
                {
                    string before=ViewSim.OrderedStateSnapshot;DrawBattle();
                    Check(before==ViewSim.OrderedStateSnapshot,"pair drawing cannot advance or decide combat at "+next);
                    Check(!BeatsWait,"independent exchanges never wait for a global visual queue");
                    TableCapture("pairs45-"+next.ToString("00"));captureCooldown=3;next+=3;
                }
                yield return null;
            }
            Check(ViewSim.Events.Any(e=>e.Kind==BattleEventKind.PairWindup),"real pair windups reached the native player");
            Check(ViewSim.Events.Count(e=>e.Kind==BattleEventKind.Hit)>5,"autonomous combat landed actual hits");
            Check(_tableHitsSeen>0,"hit reactions received real sim events");
            Check(_pairMaxAdvance>=60,"physical attacks advance at least60 native pixels instead of four");
            Check(_pairMaxRecoil>=20,"confirmed damage visibly knocks the defender back");
            Check(_pairBloodSeen>0,"confirmed wounds draw PixelLab blood");
            Check(maxOverlap<1,"physical combat keeps living torsos separate: "+maxOverlap);
            Log("physical45 maxAdvancePx="+_pairMaxAdvance+" recoilPx="+_pairMaxRecoil+" bloodReceipts="+_pairBloodSeen+" maxOverlapPx="+maxOverlap);
            var probeActor=_figures.First(f=>f.unit.Side==0&&f.pair!=null);var probeTarget=_figures.First(f=>f.unit.Side==1&&f.pair!=null);
            int bloodBefore=_pairBloodSeen;string snapshotBefore=ViewSim.OrderedStateSnapshot;
            PairCombatEvent(new BattleEvent(ViewSim.CurrentTick,BattleEventKind.Miss,0,probeActor.unit.Id,(int)probeActor.unit.Class,probeTarget.unit.Id,(int)probeTarget.unit.Class,0,AttackTable.DodgeDetail),probeActor,probeTarget,AttackTable.DodgeDetail);
            PairCombatEvent(new BattleEvent(ViewSim.CurrentTick,BattleEventKind.Hit,0,probeActor.unit.Id,(int)probeActor.unit.Class,probeTarget.unit.Id,(int)probeTarget.unit.Class,1,"block"),probeActor,probeTarget,"block");
            Check(_pairBloodSeen==bloodBefore,"dodge and blocked contact never emit wound blood");
            Check(snapshotBefore==ViewSim.OrderedStateSnapshot,"physical receipts cannot change authoritative state");
            Check(Click("pause")&&_paused,"pause through player control");
            long tick=ViewSim.CurrentTick;yield return new WaitForSecondsRealtime(.2f);
            Check(ViewSim.CurrentTick==tick,"pause freezes authoritative exchange clocks");
            string save=CampaignSave.Write(_campaign);var reload=CampaignSave.Read(save,ReviewTuning);
            Check(reload.Active.Sim.OrderedStateSnapshot==ViewSim.OrderedStateSnapshot,"rules45 active save reload is exact");
            _paused=false;_speed=0;
            for(int i=0;i<4000&&!ViewOver;i++){HudTicks(1);if(i%120==0)yield return null;}
            Check(ViewOver,"current pair fight reaches its result");
            ShowResult();yield return null;
            WatchFight(_campaign.Archive.Fights.Last());yield return RealtimeQaWaitForBattle();
            Check(_playback!=null&&PairsPresentation,"rules45 replay uses pair presentation");
            SeekPlayback(_playback.Length/2);yield return RealtimeQaWaitForBattle();
            Check(_playback.Failure.Length==0,"rules45 replay seeks to midpoint");
            Check(_figures.Where(f=>f.unit.Alive).All(f=>(f.feet-PairFeet(f.unit)).sqrMagnitude<1),"paused seek places living units at their actual formation");
            SeekPlayback(_playback.Length);yield return RealtimeQaWaitForBattle();
            Check(_playback.Matched,"rules45 replay end checksum and event history match");
            SeekPlayback(0);yield return RealtimeQaWaitForBattle();
            Check(_playback.Tick==0,"rules45 replay restarts");
            Check(_pairBlood.Count==0&&_figures.All(f=>f.pair==null||f.pair.reacted<0&&f.pair.landed<0),"restart clears old wound effects and physical reactions");
            frameTimes.Sort();Log("pairs45 steady medianMs="+frameTimes[frameTimes.Count/2]+" p95Ms="+frameTimes[(int)(frameTimes.Count*.95)]+" samples="+frameTimes.Count);
            Log("pairs45 realtime frames="+frames+" maxFrameMs="+maxFrame+" parryInputs="+parries);
            // Reuse existing exact window/early press, legacy37 and both beast checks.
            _auto=true;Camera.main.targetTexture=_captureTarget;_canvas.renderMode=RenderMode.ScreenSpaceCamera;_canvas.worldCamera=Camera.main;_canvas.planeDistance=1;
            yield return ValidatePit();
        }
    }
}
