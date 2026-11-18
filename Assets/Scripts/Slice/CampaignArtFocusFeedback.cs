using System.Collections.Generic;
using KindredSiege.Sim.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace KindredSiege.Slice
{
    public sealed partial class CampaignArtLoop
    {
        Image _focusRing;
        Sprite _focusRingSprite;
        readonly Image[] _focusEdges=new Image[8];
        readonly Dictionary<string,PxText> _fearPanic=new Dictionary<string,PxText>();
        PxText _focusOutcomeLabel;
        CanvasGroup _focusOutcomeFade;
        BattleSimulation _focusFeedbackSim;
        long _focusSyncTick=-1,_focusEndTick=-1,_focusStartTick=-1,_fearTick=-1000;
        int _focusSyncEvents=-1,_focusStartLength,_focusStartEvent=-1;
        string _focusTrackedId="",_focusOutcome="";
        bool _focusRunning,_focusFear;

        void ClearFocusFeedback()
        {
            if(_focusRingSprite!=null)Destroy(_focusRingSprite);
            _focusRingSprite=null;_focusScreen=null;_focusFeedbackSim=null;
            _focusSyncTick=-1;_focusSyncEvents=-1;_focusTrackedId="";_focusOutcome="";
            _focusStartTick=-1;_focusStartLength=0;_fearPanic.Clear();_focusTraces.Clear();
        }

        void ReadFocusFeedback(BattleSimulation sim)
        {
            if(_focusFeedbackSim==sim&&_focusSyncTick==sim.CurrentTick&&_focusSyncEvents==sim.Events.Count)return;
            if(_focusFeedbackSim!=sim||sim.CurrentTick<_focusSyncTick)
            {_focusStartTick=-1;_focusStartLength=0;_focusTrackedId="";}
            _focusFeedbackSim=sim;_focusSyncTick=sim.CurrentTick;_focusSyncEvents=sim.Events.Count;
            _focusFear=sim.FloorOn&&sim.Rules(0).Pals.Has(ShadeRoute.Fear);
            ReadClarity(sim);
            OrderResult order=OrderResult.None;
            for(int i=_orderReads.Count-1;i>=0;i--)if(_orderReads[i].Order==Directive.FocusFire){order=_orderReads[i];break;}
            // Expiry changes no event; obtain the final authoritative span immediately instead of
            // waiting for the ordinary receipt cache's quarter-second refresh.
            if(!order.IsNone&&order.Running&&sim.FocusTicksLeft(0)==0)
                foreach(var result in sim.OrderResults(0))if(result.Order==Directive.FocusFire)order=result;
            _focusDamage=0;_focusRunning=false;_focusOutcome="";_fearTick=-1000;
            _focusHitTarget="";_focusHitTick=-1000;
            if(order.IsNone){_focusTrackedId="";return;}
            int startEvent=-1;
            for(int i=sim.Events.Count-1;i>=0;i--)
                if(sim.Events[i].Kind==BattleEventKind.Directive&&sim.Events[i].ActorSide==0&&sim.Events[i].Detail==Directives.Name(Directive.FocusFire))
                {startEvent=i;break;}
            if(_focusTrackedId!=order.TargetId||_focusStartTick!=order.Tick||_focusStartEvent!=startEvent)
            {
                _focusTrackedId=order.TargetId;_focusStartTick=order.Tick;
                _focusStartEvent=startEvent;
                _focusStartLength=(int)(order.EndTick-order.Tick);
            }
            _focusEndTick=order.EndTick;
            string fallen="";
            for(int i=0;i<sim.Events.Count;i++)
            {
                var e=sim.Events[i];
                if(e.Kind==BattleEventKind.Trick&&e.ActorSide==0&&(e.Detail==ShadeRules.FearDetail||e.Detail==ShadeRules.DreadDetail))_fearTick=e.Tick;
                if(i<=startEvent||e.Tick>=order.EndTick)continue;
                if(e.Kind==BattleEventKind.Hit&&e.ActorSide==0&&e.TargetId==order.TargetId)
                {_focusDamage+=e.Amount;_focusHitTarget=e.TargetId;_focusHitTick=e.Tick;}
                if(fallen.Length==0&&e.Kind==BattleEventKind.Kill&&e.TargetId==order.TargetId)
                {fallen="KILLED";_focusEndTick=e.Tick+1;}
                if(fallen.Length==0&&e.Kind==BattleEventKind.Snap&&e.ActorId==order.TargetId&&e.Detail==RoadRules.Fled)
                {fallen="BROKE";_focusEndTick=e.Tick+1;}
            }
            _focusRunning=fallen.Length==0&&sim.FocusedId(0)==order.TargetId&&sim.FocusTicksLeft(0)>0&&!sim.IsFinished;
            if(!_focusRunning)_focusOutcome=fallen.Length>0?fallen:_focusFear?"NO FEAR":"RAN OUT";
        }

        void DrawFocusFeedback(BattleSimulation sim)
        {
            if(!sim.SquadScreenOn)return;
            if(_focusScreen!=_screen)
            {
                EnsureRaidHud();
                _focusScreen=_screen;_focusFeedbackSim=null;_focusTraces.Clear();_fearPanic.Clear();
                for(int i=0;i<_focusCorners.Length;i++)
                {
                    _focusEdges[i]=Panel(_readoutLayer,0,0,1,1,PxLilac);_focusEdges[i].raycastTarget=false;
                    _focusCorners[i]=Panel(_readoutLayer,0,0,1,1,PxRed);_focusCorners[i].raycastTarget=false;
                }
                _focusRing=Panel(_readoutLayer,0,0,1,1,PxRed);_focusRing.raycastTarget=false;
                _focusRingSprite=Sprite.Create((Texture2D)_raidBeatRing.texture,new UnityEngine.Rect(0,0,_raidBeatRing.texture.width,_raidBeatRing.texture.height),new Vector2(.5f,.5f));
                _focusRing.sprite=_focusRingSprite;_focusRing.type=Image.Type.Filled;
                _focusRing.fillMethod=Image.FillMethod.Radial360;_focusRing.fillOrigin=2;_focusRing.fillClockwise=true;
                _focusTargetLabel=PxLabel(_readoutLayer,0,0,1);_focusOutcomeLabel=PxLabel(_readoutLayer,0,0,1);
                _focusOutcomeFade=_focusOutcomeLabel.root.gameObject.AddComponent<CanvasGroup>();_focusOutcomeFade.blocksRaycasts=false;
            }
            ReadFocusFeedback(sim);_focusDrawTick=sim.CurrentTick;
            var target=FindFigure(_focusTrackedId);
            bool visible=_focusRunning&&!ViewOver&&!sim.AwaitingVerdict&&!ChiefCutInVisible&&target!=null&&target.unit.Alive&&!target.unit.Fled;
            foreach(var corner in _focusCorners)corner.enabled=visible;
            foreach(var edge in _focusEdges)edge.enabled=visible&&_focusFear;
            foreach(var trace in _focusTraces.Values)trace.enabled=false;
            _focusRing.enabled=visible;
            if(!visible)SetPx(_focusTargetLabel,"",PxRed);
            if(visible)
            {
                int x=Mathf.RoundToInt(target.feet.x/Px),y=Mathf.Max(145,Mathf.RoundToInt((target.feet.y-target.head)/Px)-16);
                bool landed=_focusHitTarget==target.unit.Id&&sim.CurrentTick-_focusHitTick<12;
                int spread=landed?19:17;
                for(int i=0;i<4;i++)
                {
                    bool right=(i&1)!=0,bottom=(i&2)!=0;
                    int cx=x+(right?spread:-spread),cy=y+(bottom?spread:-spread);
                    Place(_focusCorners[2*i],right?cx-7:cx,cy,7,2);
                    Place(_focusCorners[2*i+1],cx,bottom?cy-5:cy,2,7);
                    Place(_focusEdges[2*i],right?cx-8:cx-1,cy-1,9,4);
                    Place(_focusEdges[2*i+1],cx-1,bottom?cy-6:cy-1,4,9);
                }
                Place(_focusRing,x-13,y-13,26,26);
                _focusRing.fillAmount=Mathf.Clamp01(sim.FocusTicksLeft(0)/(float)System.Math.Max(1,_focusStartLength));
                _focusTargetLabel.root.anchoredPosition=new Vector2(x*Px,-(y+22)*Px);
                SetPx(_focusTargetLabel,"FOCUS "+_focusDamage+" DMG",PxRed);
                for(int i=sim.Events.Count-1;i>=0&&sim.CurrentTick-sim.Events[i].Tick<6;i--)
                {
                    var hit=sim.Events[i];
                    if(hit.Kind!=BattleEventKind.Hit||hit.ActorSide!=0||hit.TargetId!=target.unit.Id||hit.Tick< _focusStartTick)continue;
                    var from=FindFigure(hit.ActorId);if(from==null)continue;
                    if(!_focusTraces.TryGetValue(hit.ActorId,out var line))
                    {line=Panel(_readoutLayer,0,0,1,1,PxRed);line.raycastTarget=false;_focusTraces.Add(hit.ActorId,line);}
                    line.enabled=true;Trace(line,from,target,1,PxRed,2);
                }
            }
            bool ended=!visible&&target!=null&&_focusOutcome.Length>0&&sim.CurrentTick>=_focusEndTick&&sim.CurrentTick-_focusEndTick<30;
            _focusOutcomeFade.alpha=ended?Mathf.Clamp01(1-(sim.CurrentTick-_focusEndTick)/30f):0;
            if(ended)
            {
                Color colour=_focusOutcome=="KILLED"?PxRed:_focusOutcome=="BROKE"?PxLilac:PxMuted;
                AtBody(_focusOutcomeLabel,target,_focusOutcome,colour,-35);
                float close=(sim.CurrentTick-_focusEndTick)/5f;
                if(close<1&&!ChiefCutInVisible)
                {
                    int x=Mathf.RoundToInt(target.feet.x/Px),y=Mathf.Max(145,Mathf.RoundToInt((target.feet.y-target.head)/Px)-16);
                    int spread=Mathf.RoundToInt(17*(1-close));
                    for(int i=0;i<4;i++)
                    {
                        bool right=(i&1)!=0,bottom=(i&2)!=0;
                        Place(_focusCorners[2*i],x+(right?spread-7:-spread),y+(bottom?spread:-spread),7,2);
                        Place(_focusCorners[2*i+1],x+(right?spread:-spread),y+(bottom?spread-5:-spread),2,7);
                        _focusCorners[2*i].enabled=_focusCorners[2*i+1].enabled=true;
                        _focusCorners[2*i].color=_focusCorners[2*i+1].color=colour;
                    }
                }
            }
            else SetPx(_focusOutcomeLabel,"",PxMuted);
            // Each label receives one value per draw. Clearing then refilling it would rebuild
            // pixel text twice every frame while a panic is visible.
            bool panicVisible=_focusFear&&sim.CurrentTick-_fearTick<30&&!ChiefCutInVisible;
            foreach(var pair in _fearPanic)
            {
                var unit=sim.FindById(pair.Key);
                if(!panicVisible||unit==null||!unit.Alive||unit.PanicTicks<=0)SetPx(pair.Value,"",PxLilac);
            }
            if(panicVisible)
                foreach(var f in _figures)if(f.unit.Side==1&&f.unit.Alive&&f.unit.PanicTicks>0)
                {
                    if(!_fearPanic.TryGetValue(f.unit.Id,out var label)){label=PxLabel(_readoutLayer,0,0,1);_fearPanic.Add(f.unit.Id,label);}
                    AtBody(label,f,"PANIC",PxLilac,-28);
                }
        }
    }
}
