using KindredSiege.Sim;
using KindredSiege.Sim.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace KindredSiege.Slice
{
    public sealed partial class CampaignArtLoop
    {
        static readonly int ParryGold=Shader.PropertyToID("_ParryGold"), ParryRim=Shader.PropertyToID("_ParryRim"), FlatWhite=Shader.PropertyToID("_FlatWhite");
        static readonly int CounterRim=Shader.PropertyToID("_CounterRim"),CounterColor=Shader.PropertyToID("_CounterColor");
        static readonly Color[] CounterColours={new Color32(90,18,8,255),new Color32(218,107,81,255),Color.white};
        RectTransform _tightScreen;
        RawImage _tightClash;
        Clip _tightClashClip;
        Clip _tightCounterClip;
        string _tightLandedTarget="";
        float _tightLandedAt=-100, _tightShakeAt=-100, _counterSoundAt=-100;
        static readonly Vector2[] TightShake={new Vector2(4,0),new Vector2(-3,2),new Vector2(3,-2),new Vector2(-2,1),new Vector2(1,-1)};
        const string OriginalTightGuide="PARRY / WATCH THE ATTACKER\n\nA steady gold rim warns that a parryable attack is coming. The body flashes gold in the final 0.2 seconds: press a fresh Hold then, while the fight runs. The rim alone is not the parry window. Ordinary attacks have neither cue.\n\nA parry stops the move, staggers the attacker for 2 seconds, counters and fills one bar. Three bars let you cast your chief spell. Held bars strengthen combos.\n\nA mistimed Hold lets the move hit in full and leaves your squad taking 50% more damage for 1 second. Fall Back avoids supported moves but stops your attacks.\n\nBlue casts can also be interrupted with Focus Fire. Check each move's answers. Pause to read; paused Hold cannot parry.";

        string TightGuide => LeaderParryPresentation ? "PARRY / WATCH YOUR LEADER\n\nBig blows link to your leader. Press a fresh Hold when the circle reaches gold and PARRY appears. A parry stops the blow; a miss hits the originally threatened fighter. Units keep fighting. Pause to inspect, resume before timing Hold. Three earned suns let you cast your chief spell." : PitPresentation ? "PARRY / WATCH THE FLOOR\n\nThe ring under the target fills toward its gold section. Press a fresh Hold when the fill enters gold and PARRY appears. The attacker flashes gold at the same time. Ordinary attacks have no parry ring. The window is 0.2 seconds of fight time.\n\nA fake stays a warning; WAIT means the enemy delays this beat. Pause to inspect. Resume before pressing Hold.\n\nA parry stops the move, staggers its attacker, counters and earns one sun. Three suns let you cast your chief spell. Held suns boost confirmed combos.\n\nAn early Hold that is still up when the beat lands whiffs: full damage, then 50% extra incoming damage for 1 second. Red outlines show that penalty. Fall Back avoids supported moves but stops your attacks.\n\nMint tethers show the Warden's wards. Purple then mint wires show a confirmed setter-to-target-to-casher combo. Details lists each move's answers. New effect shapes and quiet guard ticks are placeholders." : OriginalTightGuide;
        void TightParryEvent(BattleEvent e,Figure actor,Figure target)
        {
            if(!ViewSim.TightParryOn||_seeking)return;
            if(e.Kind==BattleEventKind.Parry)
            {
                _tightLandedTarget=e.TargetId;_tightLandedAt=_tightShakeAt=_visualTime;
                if(target!=null)target.majorWhiteUntil=_visualTime+1f/12;
                if(_tightClash!=null&&target!=null)
                    _tightClash.rectTransform.anchoredPosition=new Vector2(target.feet.x-130,-target.feet.y+161);
            }
            if(e.Kind==BattleEventKind.Hit&&e.Detail==TightRules.CounterDetail)
            {
                _counterSoundAt=_tightLandedAt+.15f;
                if(target!=null)
                {
                    target.counterAt=Mathf.Max(_visualTime,_tightLandedAt+.15f);
                    if(target.counterImage==null)
                    {
                        _tightCounterClip=LoadClip("UI/parry_counter");
                        target.counterImage=Raw(_readoutLayer,"Counter hit star",0,0,115,107);
                        target.counterImage.texture=_tightCounterClip.texture;target.counterImage.raycastTarget=false;
                    }
                }
            }
        }

        void DrawTightBody(BattleSimulation sim,Figure figure)
        {
            if(figure.hitMaterial==null)return;
            figure.hitMaterial.SetFloat("_CounterWidth",1);
            int frame=-1;bool warning=false;
            if(sim.TightParryOn&&!_paused&&!ViewOver&&!ChiefCutInVisible)
                foreach(var threat in _threatReads)
                {
                    if(threat.ActorId!=figure.unit.Id||(threat.Answers&ThreatAnswer.Parry)==0)continue;
                    if(threat.ParryWindow)
                    {frame=Mathf.Clamp(Mathf.FloorToInt((threat.WindowTicks+1-threat.BeatTicks)*6f/threat.WindowTicks),0,5);break;}
                    // The warning identifies the attacker before the existing press-now flash.
                    // A fake or wait-out can warn too; only the sim's ParryWindow permits fill.
                    warning|=sim.PitOn?threat.RealBeatTicks>0:threat.BeatTicks>=threat.WindowTicks+2&&
                        threat.BeatTicks<=threat.WindowTicks+1+SimClock.TicksPerSecond;
                }
            figure.hitMaterial.SetFloat(ParryGold,frame==0||frame==1||frame==3||frame==4?1:0);
            bool rim=frame<0?warning:frame==0||frame==1||frame==3;
            figure.hitMaterial.SetFloat(ParryRim,rim?1:0);
            figure.hitMaterial.SetFloat(FlatWhite,!_paused&&(TablePresentation&&figure.table!=null&&figure.table.whiteFrame==Time.frameCount||sim.TightParryOn&&_visualTime<figure.majorWhiteUntil)?1:0);
            if(LeaderParryPresentation&&figure.unit.Id==KindredSiege.Sim.Learning.Campaign.LeaderId&&!_paused&&_visualTime-_pitParryAt<.15f)figure.hitMaterial.SetFloat(ParryGold,1);
            float counterAge=_visualTime-figure.counterAt;
            bool counter=sim.TightParryOn&&!_paused&&counterAge>=0&&counterAge<6f/16;
            figure.hitMaterial.SetFloat(CounterRim,counter?1:0);
            if(counter)figure.hitMaterial.SetColor(CounterColor,CounterColours[Mathf.FloorToInt(counterAge*16)%3]);
            if(sim.PitOn&&figure.unit.Alive&&sim.WhiffTicksLeft(figure.unit.Side)>0)
            {figure.hitMaterial.SetFloat(CounterRim,1);figure.hitMaterial.SetColor(CounterColor,PxRed);}
        }

        void DrawTightParry(BattleSimulation sim)
        {
            if(_tightScreen!=_screen)
            {
                _tightScreen=_screen;_tightLandedAt=_tightShakeAt=_counterSoundAt=-100;_tightClash=null;
                if(sim.TightParryOn)
                {
                    _tightClashClip=LoadClip("UI/parry_clash");
                    _tightCounterClip=LoadClip("UI/parry_counter");
                    _tightClash=Raw(_readoutLayer,"Parry clash",0,0,268,273);
                    _tightClash.texture=_tightClashClip.texture;_tightClash.raycastTarget=false;
                }
            }
            if(!sim.TightParryOn)return;
            foreach(var figure in _figures)if(figure.counterImage!=null)
            {
                float counterAge=_visualTime-figure.counterAt;
                figure.counterImage.enabled=!_paused&&counterAge>=0&&counterAge<_tightCounterClip.seconds;
                if(!figure.counterImage.enabled)continue;
                var rect=_tightCounterClip.sheet.frames[_tightCounterClip.FrameAt(counterAge,false)].frame;
                figure.counterImage.uvRect=new UnityEngine.Rect(rect.x/(float)_tightCounterClip.texture.width,1-(rect.y+rect.h)/(float)_tightCounterClip.texture.height,rect.w/(float)_tightCounterClip.texture.width,rect.h/(float)_tightCounterClip.texture.height);
                // Keep the delivered pixels at 1x, moving the authored chest offset with the
                // target's height instead of putting a chief-sized offset above a smaller fighter.
                // The released chief idle has a224px ground anchor; the authored effect's
                // cropped reference has a193px anchor. Preserve its native offset on that chief.
                float h=figure.head/224f;
                figure.counterImage.rectTransform.anchoredPosition=new Vector2(Mathf.Round(figure.feet.x-28.5f*h-57.5f),-Mathf.Round(figure.feet.y-122.5f*h-53.5f));
            }
            if(_parryOuter!=null)_parryOuter.enabled=false;
            if(_parryInner!=null)_parryInner.enabled=false;
            if(_parryLink!=null)_parryLink.enabled=false;
            if(_parryCall!=null)SetPx(_parryCall,"",PxMuted);
            if(_raidBeatRing!=null)_raidBeatRing.enabled=false;
            if(_raidFlash!=null)_raidFlash.enabled=false;
            if(_clearStamp!=null)SetPx(_clearStamp,"",PxMuted);
            if(_castLabel!=null&&!CurrentThreat.CastBar)
            {
                SetPx(_castLabel,"",PxMuted);
                if(_castBack!=null)_castBack.enabled=false;
                if(_castFill!=null)_castFill.enabled=false;
                if(_castMark!=null)_castMark.enabled=false;
            }
            float age=_visualTime-_tightLandedAt;
            if(_tightClash!=null)
            {
                _tightClash.enabled=age>=0&&age<_tightClashClip.seconds;
                if(_tightClash.enabled)
                {
                    var rect=_tightClashClip.sheet.frames[_tightClashClip.FrameAt(age,false)].frame;
                    _tightClash.uvRect=new UnityEngine.Rect(rect.x/(float)_tightClashClip.texture.width,1-(rect.y+rect.h)/(float)_tightClashClip.texture.height,rect.w/(float)_tightClashClip.texture.width,rect.h/(float)_tightClashClip.texture.height);
                }
            }
            if(!_paused&&_counterSoundAt>=0&&_visualTime>=_counterSoundAt)
            {PlaySound("riposte",1,0);_counterSoundAt=-100;}
            float shake=_visualTime-_tightShakeAt;
            if(!_paused&&shake>=0&&shake<.2f)
            {
                _stage.anchoredPosition=TightShake[Mathf.Min(4,Mathf.FloorToInt(shake/.04f))]*(sim.PitOn?.5f:1);
            }
        }
    }
}
