using KindredSiege.Sim.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace KindredSiege.Slice
{
    public sealed partial class CampaignArtLoop
    {
        RectTransform _sharkScreen;
        RawImage _sharkImage,_sharkShadow;
        Clip _sharkIdle,_sharkBite,_sharkSweep,_sharkDive,_sharkSurface,_sharkFin,_sharkShadowClip,_sharkAction;
        Material _sharkMaterial;
        float _sharkActionAt;
        bool _sharkWasUnder;
        static readonly Vector2 SharkFeet=new Vector2(1060,585);
        bool IsSharkPart(BattleUnit unit)=>unit!=null&&unit.Side==1&&ViewSim.Beast(1)==BeastKit.SandShark&&ViewSim.TryPartOf(unit,out _);
        Vector2 SharkPartFeet(BattleUnit unit)
        {
            ViewSim.TryPartOf(unit,out var part);
            var pixel=part==BeastPart.Jaw?new Vector2(51,115):part==BeastPart.Fin?new Vector2(168,87):
                part==BeastPart.Spine?new Vector2(142,93):new Vector2(242,131);
            return SharkFeet+(pixel-new Vector2(144,148))*2;
        }
        static float HitTime(Clip clip)
        {
            float time=0;for(int i=0;i<clip.sheet.hitFrame;i++)time+=clip.sheet.frames[i].duration/1000f;return time;
        }
        void SharkEvent(BattleEvent e)
        {
            if(ViewSim.Beast(1)!=BeastKit.SandShark||_sharkImage==null||_seeking)return;
            if(e.Kind!=BattleEventKind.Land)return;
            var clip=e.Detail.StartsWith("Bite.")?_sharkBite:e.Detail.StartsWith("Sweep.")?_sharkSweep:null;
            if(clip!=null){_sharkAction=clip;_sharkActionAt=_visualTime-HitTime(clip);}
        }
        void SharkFrame(RawImage image,Clip clip,float time,bool loop,Vector2 feet)
        {
            var frame=clip.sheet.frames[clip.FrameAt(time,loop)].frame;
            image.texture=clip.texture;
            image.uvRect=new UnityEngine.Rect(frame.x/(float)clip.texture.width,1-(frame.y+frame.h)/(float)clip.texture.height,frame.w/(float)clip.texture.width,frame.h/(float)clip.texture.height);
            image.rectTransform.sizeDelta=new Vector2(frame.w*2,frame.h*2);
            image.rectTransform.anchoredPosition=new Vector2(feet.x-clip.sheet.anchorX*2,-feet.y+clip.sheet.anchorY*2);
        }
        void DrawShark(BattleSimulation sim)
        {
            if(_sharkScreen!=_screen)
            {
                _sharkScreen=_screen;_sharkImage=null;_sharkAction=null;_sharkWasUnder=false;
                if(sim.Beast(1)!=BeastKit.SandShark)return;
                _sharkIdle=LoadClip("Beasts/beast_sandshark_idle");_sharkBite=LoadClip("Beasts/beast_sandshark_bite");
                _sharkSweep=LoadClip("Beasts/beast_sandshark_sweep");_sharkDive=LoadClip("Beasts/beast_sandshark_dive");
                _sharkSurface=LoadClip("Beasts/beast_sandshark_surface");_sharkFin=LoadClip("Beasts/beast_sandshark_fin");
                _sharkShadowClip=LoadClip("Beasts/beast_sandshark_shadow");
                _sharkShadow=Raw(_stage,"Shark shadow",0,0,1,1);_sharkShadow.raycastTarget=false;
                _sharkImage=Raw(_stage,"Sand shark / one body",0,0,512,512);_sharkImage.raycastTarget=false;
                _sharkMaterial=new Material(Resources.Load<Shader>("CampaignArt/UI/CombatHitFlash"));_sharkImage.material=_sharkMaterial;
            }
            if(_sharkImage==null)return;
            if(sim.BeastUnder!=_sharkWasUnder)
            {_sharkWasUnder=sim.BeastUnder;_sharkAction=sim.BeastUnder?_sharkDive:_sharkSurface;_sharkActionAt=_visualTime;}
            Clip clip=_sharkIdle;float time=_visualTime;bool loop=true;
            bool action=_sharkAction!=null&&_visualTime-_sharkActionAt<_sharkAction.seconds;
            if(action){clip=_sharkAction;time=_visualTime-_sharkActionAt;loop=false;}
            else if(sim.BeastUnder){clip=_sharkFin;}
            else
            {
                foreach(var part in new[]{BeastPart.Jaw,BeastPart.Tail})
                    if(sim.BeastTellTicks(part)>0)
                    {
                        clip=part==BeastPart.Jaw?_sharkBite:_sharkSweep;
                        time=Mathf.Max(0,HitTime(clip)-sim.BeastTellTicks(part)/30f);loop=false;break;
                    }
            }
            Vector2 feet=SharkFeet;
            if(sim.BeastUnder&&!action)feet.x+=Mathf.Round(Mathf.Sin(_visualTime)*90);
            SharkFrame(_sharkImage,clip,time,loop,feet);
            var uv=_sharkImage.uvRect;_sharkMaterial.SetVector(SpriteRect,new Vector4(uv.x,uv.y,uv.width,uv.height));
            _sharkShadow.enabled=sim.BeastUnder;
            if(_sharkShadow.enabled)SharkFrame(_sharkShadow,_sharkShadowClip,_visualTime,true,feet);
            int gold=-1;
            if(sim.TightParryOn&&!_paused&&!ViewOver)foreach(var t in _threatReads)
                if(t.ParryWindow&&(t.Answers&ThreatAnswer.Parry)!=0&&IsSharkPart(sim.FindById(t.ActorId)))
                {gold=Mathf.Clamp(t.WindowTicks+1-t.BeatTicks,0,5);break;}
            _sharkMaterial.SetFloat(ParryGold,gold==0||gold==1||gold==3||gold==4?1:0);
            _sharkMaterial.SetFloat(ParryRim,gold==0||gold==1||gold==3?1:0);
            _sharkMaterial.SetFloat(FlatWhite,!_paused&&_visualTime-_tightLandedAt<1f/12?1:0);
        }
    }
}
