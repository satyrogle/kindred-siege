using KindredSiege.Sim.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace KindredSiege.Slice
{
    public sealed partial class CampaignArtLoop
    {
        static readonly int ParryGold=Shader.PropertyToID("_ParryGold"), ParryRim=Shader.PropertyToID("_ParryRim"), FlatWhite=Shader.PropertyToID("_FlatWhite");
        RectTransform _tightScreen;
        RawImage _tightClash;
        Clip _tightClashClip;
        float _tightLandedAt=-100, _tightShakeAt=-100, _counterSoundAt=-100;
        static readonly Vector2[] TightShake={new Vector2(4,0),new Vector2(-3,2),new Vector2(3,-2),new Vector2(-2,1),new Vector2(1,-1)};
        const string TightGuide="PARRY / WATCH THE ATTACKER\n\nThe attacking fighter flashes gold for the last 0.2 seconds. Press a fresh Hold while the fight runs. Ordinary attacks have no parry cue.\n\nA parry stops the move, staggers the attacker for 2 seconds, counters and fills one bar. Three bars let you cast your chief spell. Held bars strengthen combos.\n\nA mistimed Hold lets the move hit in full and leaves your squad taking 50% more damage for 1 second. Fall Back avoids supported moves but stops your attacks.\n\nBlue casts can also be interrupted with Focus Fire. Check each move's answers. Pause to read; paused Hold cannot parry.";

        void TightParryEvent(BattleEvent e,Figure actor,Figure target)
        {
            if(!ViewSim.TightParryOn||_seeking)return;
            if(e.Kind==BattleEventKind.Parry)
            {
                _tightLandedAt=_tightShakeAt=_visualTime;
                if(target!=null)target.majorWhiteUntil=_visualTime+1f/12;
                if(_tightClash!=null&&target!=null)
                    _tightClash.rectTransform.anchoredPosition=new Vector2(target.feet.x-130,-target.feet.y+161);
            }
            if(e.Kind==BattleEventKind.Hit&&e.Detail==TightRules.CounterDetail)
                _counterSoundAt=_tightLandedAt+.15f;
        }

        void DrawTightBody(BattleSimulation sim,Figure figure)
        {
            if(figure.hitMaterial==null)return;
            int frame=-1;
            if(sim.TightParryOn&&!_paused&&!ViewOver&&!ChiefCutInVisible)
                foreach(var threat in _threatReads)
                    if(threat.ActorId==figure.unit.Id&&threat.ParryWindow&&(threat.Answers&ThreatAnswer.Parry)!=0)
                    {frame=Mathf.Clamp(threat.WindowTicks+1-threat.BeatTicks,0,5);break;}
            figure.hitMaterial.SetFloat(ParryGold,frame==0||frame==1||frame==3||frame==4?1:0);
            figure.hitMaterial.SetFloat(ParryRim,frame==0||frame==1||frame==3?1:0);
            figure.hitMaterial.SetFloat(FlatWhite,sim.TightParryOn&&!_paused&&_visualTime<figure.majorWhiteUntil?1:0);
        }

        void DrawTightParry(BattleSimulation sim)
        {
            if(_tightScreen!=_screen)
            {
                _tightScreen=_screen;_tightLandedAt=_tightShakeAt=_counterSoundAt=-100;_tightClash=null;
                if(sim.TightParryOn)
                {
                    _tightClashClip=LoadClip("UI/parry_clash");
                    _tightClash=Raw(_readoutLayer,"Parry clash",0,0,268,273);
                    _tightClash.texture=_tightClashClip.texture;_tightClash.raycastTarget=false;
                }
            }
            if(!sim.TightParryOn)return;
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
                _stage.anchoredPosition=TightShake[Mathf.Min(4,Mathf.FloorToInt(shake/.04f))];
            }
        }
    }
}
