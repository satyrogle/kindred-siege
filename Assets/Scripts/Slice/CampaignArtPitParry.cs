using System;
using System.Collections.Generic;
using KindredSiege.Sim;
using KindredSiege.Sim.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace KindredSiege.Slice
{
    public sealed partial class CampaignArtLoop
    {
        sealed class PitFuse
        {
            public string key;public int total, cue;public bool seen;
            public Image link,bolt;public Threat threat;public readonly Image[] dots=new Image[64];public PxText word;
        }
        readonly List<PitFuse> _pitFuses=new List<PitFuse>();
        RectTransform _pitScreen;
        float _pitLean, _pitDrawAt, _pitParryAt=-100;
        Vector2 _pitParryPoint;
        Image _pitSunFlight;
        PxText _pitParryWord;
        Outline _pitSpellGlow;

        Image PitLine(string name,Transform parent)
        {var line=Panel(parent,0,0,1,1,PxBone);line.name=name+" / ART PENDING";line.raycastTarget=false;return line;}
        void PitLineAt(Image line,Vector2 from,Vector2 to,Color colour,float width=2)
        {
            var delta=to-from;var rt=line.rectTransform;rt.pivot=new Vector2(0,.5f);
            rt.anchoredPosition=new Vector2(Mathf.Round(from.x),-Mathf.Round(from.y));
            rt.sizeDelta=new Vector2(delta.magnitude,width);rt.localEulerAngles=new Vector3(0,0,-Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);
            line.color=colour;line.enabled=true;
        }
        Vector2 PitChest(Figure f)=>new Vector2(f.root.anchoredPosition.x,-f.root.anchoredPosition.y-f.head*.5f);
        void PreparePitReadouts()
        {
            if(_pitScreen==_screen)return;
            _pitScreen=_screen;_pitFuses.Clear();_pitLean=0;_pitDrawAt=_visualTime;_pitParryAt=-100;
            _pitSunFlight=PitLine("Earned sun",_screen);_pitSunFlight.rectTransform.sizeDelta=new Vector2(8,8);_pitSunFlight.enabled=false;
            _pitParryWord=PxLabel(_readoutLayer,0,0,1);
            _pitSpellGlow=null;
            if(_buttons.TryGetValue("order-4",out var button))
            {_pitSpellGlow=button.gameObject.AddComponent<Outline>();_pitSpellGlow.effectDistance=new Vector2(2,-2);_pitSpellGlow.enabled=false;}
            if(!TablePresentation)SetPx(PxLabel(_screen,22,365),"FX ART PENDING",PxMuted);
        }
        void PitParryEvent(BattleEvent e,Figure actor,Figure target)
        {
            if(e.Kind!=BattleEventKind.Parry||e.ActorSide!=0)return;
            PreparePitReadouts();_pitParryAt=_visualTime;
            Figure defender=null;
            foreach(var fuse in _pitFuses)if(fuse.threat.ActorId==e.TargetId){defender=FindFigure(fuse.threat.TargetId);break;}
            if(LeaderParryPresentation)defender=FindFigure(KindredSiege.Sim.Learning.Campaign.LeaderId);
            _pitParryPoint=target!=null?PitChest(target):new Vector2(720,460);
            if(defender!=null)_pitParryPoint=LeaderParryPresentation?PitChest(defender):(_pitParryPoint+PitChest(defender))*.5f;
            if(_tightClash!=null)_tightClash.rectTransform.anchoredPosition=new Vector2(_pitParryPoint.x-134,-_pitParryPoint.y+136);
            if(!LeaderParryPresentation&&defender!=null&&target!=null)
            {defender.strikeAt=defender.poseTime;defender.strikePixels=30*Mathf.Sign(target.feet.x-defender.feet.x);}
            foreach(var f in _figures)if(f==target||f==defender)
            {f.poseHold=Mathf.Max(f.poseHold,.15f);f.majorFreeze=true;}
        }
        Color PitFocusTint(Figure f,Color tint)
        {
            if(!PitPresentation||_paused||ViewOver)return tint;
            bool any=false,involved=false;
            foreach(var threat in _threatReads)
            {
                if(threat.RealBeatTicks<1||(threat.Answers&ThreatAnswer.Parry)==0)continue;
                any=true;involved|=threat.ActorId==f.unit.Id||threat.TargetId==f.unit.Id;
            }
            float k=any&&!involved?.8f:1;
            // Fades instead of flipping the field; a big blow dims everyone outside it too (CampaignArtBeats.cs).
            if(BeatsOn)k=BeatFade(f,Mathf.Min(k,BeatDimWant(f)));
            return k<1?new Color(tint.r*k,tint.g*k,tint.b*k,tint.a):tint;
        }
        int PitFuseTotal(Threat threat)
        {
            int total=threat.RealBeatTicks;
            var events=ViewSim.Events;
            for(int i=events.Count-1;i>=0;i--)
            {
                var e=events[i];if(ViewSim.CurrentTick-e.Tick>600)break;
                if(e.Kind!=BattleEventKind.Windup&&e.Kind!=BattleEventKind.Tell)continue;
                if(e.ActorId!=threat.ActorId)continue;
                total=Mathf.Max(total,e.Amount+(threat.FakeFirst?ParryRules.FeintTicks:0));break;
            }
            return Mathf.Max(threat.WindowTicks+2,total);
        }
        static int PitCueStep(Threat threat)
        {
            int left=threat.RealBeatTicks,window=threat.WindowTicks+1;
            if(left<1||(threat.Answers&ThreatAnswer.Parry)==0||threat.WaitsOutHold)return 0;
            return threat.ParryWindow?3:left<=window+6?2:left<=window+12?1:0;
        }
        void DrawPitParry()
        {
            PreparePitReadouts();
            if(_threatPanel!=null&&_threatPanel.parent!=_screen)_threatPanel.SetParent(_screen,false);
            foreach(var f in _pitFuses)f.seen=false;
            Vector2 centre=new Vector2(720,450);int active=0;
            foreach(var threat in _threatReads)
            {
                if(ViewOver||ChiefCutInVisible||threat.RealBeatTicks<1||(threat.Answers&ThreatAnswer.Parry)==0)continue;
                var attacker=FindFigure(threat.ActorId);var target=FindFigure(threat.TargetId);
                if(attacker==null||!attacker.unit.Alive||target!=null&&!target.unit.Alive)continue;
                string key=threat.ActorId+"/"+threat.Name;
                var fuse=_pitFuses.Find(f=>f.key==key);
                if(fuse==null)
                {
                    fuse=new PitFuse{key=key,total=PitFuseTotal(threat),word=PxLabel(_readoutLayer,0,0,1)};
                    for(int i=0;i<fuse.dots.Length;i++)fuse.dots[i]=PitLine("Parry floor fuse",_readoutLayer);
                    _pitFuses.Add(fuse);
                }
                fuse.seen=true;fuse.threat=threat;fuse.total=Mathf.Max(fuse.total,threat.RealBeatTicks);
                var mark=LeaderParryPresentation?FindFigure(KindredSiege.Sim.Learning.Campaign.LeaderId)??target??attacker:target??attacker;Vector2 point=new Vector2(mark.feet.x,mark.feet.y+19);
                float progress=1-(threat.RealBeatTicks-1)/(float)fuse.total;
                if(LeaderParryPresentation)
                {
                    if(fuse.link==null){fuse.link=PitLine("Mind link",_readoutLayer);fuse.bolt=PitLine("Linked incoming blow",_readoutLayer);}
                    var source=target??attacker;PitLineAt(fuse.link,PitChest(source),PitChest(mark),new Color(.7f,.65f,.95f,.5f),1);
                    float flight=Mathf.Clamp01(1-(threat.RealBeatTicks-1)/12f);var p=Vector2.Lerp(PitChest(attacker),PitChest(mark),flight);
                    PitLineAt(fuse.bolt,p,p+new Vector2(6,0),threat.ParryWindow?PxGold:PxLilac,3);
                }
                float gold=1-(threat.WindowTicks+1)/(float)fuse.total;
                for(int i=0;i<fuse.dots.Length;i++)
                {
                    float p=i/(float)fuse.dots.Length,a=p*Mathf.PI*2-Mathf.PI/2,b=(p+.0125f)*Mathf.PI*2-Mathf.PI/2;
                    Color colour=p>=gold?(threat.ParryWindow?(Color)PxGold:Hex("9E7C39")):p<=progress?(Color)PxBone:Hex("3D3433");
                    PitLineAt(fuse.dots[i],point+new Vector2(Mathf.Cos(a)*57,Mathf.Sin(a)*19),point+new Vector2(Mathf.Cos(b)*57,Mathf.Sin(b)*19),colour,threat.ParryWindow?5:3);
                }
                fuse.word.root.anchoredPosition=new Vector2(Mathf.Round(point.x),-Mathf.Round(point.y+24));
                SetPx(fuse.word,threat.ParryWindow?(_paused?"PAUSED":"PARRY"):threat.WaitsOutHold?"WAIT":"",threat.ParryWindow?PxGold:PxBone);
                // Three cues on the actual combat clock. A fake or learned delay never gets the press-now tick.
                int cue=PitCueStep(threat);
                if(!_paused&&cue>fuse.cue)
                {PlaySound("guard",.18f,0);fuse.cue=cue;}
                if(active==0)centre=target==null?PitChest(attacker):(PitChest(attacker)+PitChest(target))*.5f;
                active++;
            }
            for(int i=_pitFuses.Count-1;i>=0;i--)
            {
                var f=_pitFuses[i];if(f.seen)continue;
                foreach(var dot in f.dots)Destroy(dot.gameObject);SetPx(f.word,"",PxBone);
                // Text objects are screen-owned and reused when possible; destroy this expired label too.
                Destroy(f.word.root.gameObject);if(f.link!=null)Destroy(f.link.gameObject);if(f.bolt!=null)Destroy(f.bolt.gameObject);_pitFuses.RemoveAt(i);
            }
            float dt=Mathf.Max(0,_visualTime-_pitDrawAt);_pitDrawAt=_visualTime;
            if(active>0&&_chiefCue!=null)SetPx(_chiefCue,"",PxBone);
            if(!StageHolding)_pitLean=Mathf.MoveTowards(_pitLean,active>0&&!ViewOver?.05f:0,dt*.2f);
            _stage.localScale=Vector3.one*(1+_pitLean);
            _stage.anchoredPosition+=new Vector2(-centre.x*_pitLean,centre.y*_pitLean);
            float age=_visualTime-_pitParryAt;
            _pitSunFlight.enabled=age>=0&&age<.45f;
            if(_pitSunFlight.enabled)
            {
                int bars=ViewSim.ParryBars(0);var dest=new Vector2(716+Mathf.Max(0,bars-1)*88,37);
                var from=_pitParryPoint*(1+_pitLean)+new Vector2(_stage.anchoredPosition.x,-_stage.anchoredPosition.y);
                var point=Vector2.Lerp(from,dest,Mathf.Clamp01(age/.45f));
                point.y-=Mathf.Sin(age/.45f*Mathf.PI)*65;
                _pitSunFlight.rectTransform.anchoredPosition=new Vector2(point.x,-point.y);_pitSunFlight.color=PxGold;
            }
            _pitParryWord.root.anchoredPosition=new Vector2(Mathf.Round(_pitParryPoint.x),-Mathf.Round(_pitParryPoint.y-45-age*20));
            SetPx(_pitParryWord,age>=0&&age<.65f&&!BeatTitleUp("PARRIED")?"PARRY":"",PxGold);
            if(_heavyParryWord!=null)_heavyParryWord.enabled=false;
            if(_heavyRiposteWord!=null)_heavyRiposteWord.enabled=false;
            if(_heavyTargetBack!=null)_heavyTargetBack.enabled=false;
            if(_heavyTargetLabel!=null)SetPx(_heavyTargetLabel,"",PxBone);
            if(!_paused&&_cleanThreatBack!=null)
            {
                _cleanThreatBack.enabled=false;
                SetPx(_threatHeading,"",PxBone);SetPx(_threatTarget,"",PxBone);SetPx(_threatAnswers,"",PxBone);
                _beatTrack.enabled=_beatWindow.enabled=_beatCursor.enabled=false;
            }
            if(_pitSpellGlow!=null)
            {
                _pitSpellGlow.enabled=!ViewOver&&ViewSim.ParryBars(0)==3&&_buttons["order-4"].interactable;
                _pitSpellGlow.effectColor=Color.Lerp(PxGold,Color.white,.25f+.25f*Mathf.Sin(_visualTime*7));
            }
        }
    }
}
