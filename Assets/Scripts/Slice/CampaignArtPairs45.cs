using System.Collections.Generic;
using KindredSiege.Sim;
using KindredSiege.Sim.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace KindredSiege.Slice
{
    public sealed partial class CampaignArtLoop
    {
        bool PairsPresentation => (_fight!=null||_playback!=null)&&ViewSim.LockedPairsOn;
        // Screen positions only. The sim decides actors, targets, timing and damage.
        sealed class PairPose
        {
            public Vector2 from,to,render,follow;
            public float at=-100,landed=-100,reacted=-100,left,back;
            public bool coming,dodge,guard;
            public string target="";
            public Clip step,action;
        }
        sealed class PairBlood
        {public RawImage image;public Figure target;public Vector2 origin;public float at;public bool mirror;}
        readonly List<PairBlood> _pairBlood=new List<PairBlood>();
        Clip _pairBloodClip;
        int _pairBloodSeen;
        float _pairMaxAdvance,_pairMaxRecoil;
        const float PairLead=.48f,PairFollow=.38f,PairReact=.24f;

        Vector2 PairFeet(BattleUnit u)
        {
            int col=Mathf.Max(0,u.GridCol),lane=Mathf.Max(0,u.GridLane);
            bool paired=ViewSim.PairedOpponentId(u).Length>0;
            float x=paired?620:290+180*col;
            return new Vector2(u.Side==0?x:W-x,440+140*lane);
        }
        void PreparePairFigure(Figure f)
        {
            if(!PairsPresentation)return;
            f.pair=new PairPose{from=f.feet,to=f.feet,render=f.feet};
            string key="Pairs45/"+f.artKey+"_step";
            if(f.artKey!=null&&Resources.Load<TextAsset>("CampaignArt/"+key)!=null)f.pair.step=LoadClip(key);
            _pairBloodClip=LoadClip("Physical45/blood");
        }
        void ClearPairPresentation()
        {
            foreach(var blood in _pairBlood)if(blood.image!=null)Destroy(blood.image.gameObject);
            _pairBlood.Clear();_pairBloodClip=null;_pairBloodSeen=0;_pairMaxAdvance=_pairMaxRecoil=0;
        }
        static bool PairBody(Figure f)=>f.pair!=null&&f.unit.Alive&&!f.unit.Benched&&!f.unit.Fled;
        static float PairWidth(Figure f)=>f.unit.Class==UnitClass.Warden?55:43;
        void DrawPairPositions()
        {
            if(!PairsPresentation)return;
            foreach(var f in _figures)
            {
                var p=f.pair;if(p==null)continue;
                p.coming=false;p.left=100;
                if(!f.unit.Alive)continue;
                Vector2 to=PairFeet(f.unit);
                if(to!=p.to){p.from=f.feet;p.to=to;p.at=_visualTime;}
                float t=Mathf.Clamp01((_visualTime-p.at)/.4f);
                f.feet=Vector2.Lerp(p.from,p.to,t*t*(3-2*t));
            }
            for(int side=0;side<2;side++)foreach(var blow in ViewSim.Blows(side))
            {
                var f=FindFigure(blow.AttackerId);if(f?.pair==null||!f.unit.Alive)continue;
                var p=f.pair;float left=(blow.TicksLeft-1)/(float)SimClock.TicksPerSecond;
                if(left<0||left>PairLead||left>=p.left)continue;
                p.coming=true;p.left=left;p.target=blow.TargetId;
                p.action=f.table!=null?TableActionClip(f,f.unit.Class==UnitClass.Medic,blow.Kind==BlowType.Move):f.attack;
            }
            foreach(var f in _figures)
            {
                var p=f.pair;if(p==null||!f.unit.Alive)continue;
                var offset=Vector2.zero;
                float after=_visualTime-p.landed;
                if(after>=0&&after<PairFollow)offset=p.follow*(1-Mathf.SmoothStep(0,1,Mathf.Max(0,after-.07f)/(PairFollow-.07f)));
                if(p.coming)
                {
                    var target=FindFigure(p.target);
                    float direction=f.unit.Side==0?1:-1;
                    float distance=target!=null?Mathf.Abs(target.feet.x-f.feet.x):0;
                    bool close=!f.unit.Stats.Ranged&&f.unit.Class!=UnitClass.Medic&&f.unit.Class!=UnitClass.Warden;
                    float reach=close?Mathf.Clamp(distance-100,0,120):14;
                    float phase=1-p.left/PairLead;
                    // Plant, then drive into the authored contact pose.
                    float drive=phase<.22f?-8*Mathf.Sin(phase/.22f*Mathf.PI):reach*Mathf.SmoothStep(0,1,(phase-.22f)/.78f);
                    offset.x=direction*drive;
                }
                float reaction=_visualTime-p.reacted;
                if(reaction>=0&&reaction<PairReact)
                {
                    float weight=reaction<.05f?reaction/.05f:1-Mathf.SmoothStep(0,1,(reaction-.05f)/(PairReact-.05f));
                    offset.x+=p.back*weight;
                    if(p.dodge)offset.y-=8*Mathf.Sin(Mathf.PI*reaction/PairReact);
                    _pairMaxRecoil=Mathf.Max(_pairMaxRecoil,Mathf.Abs(p.back*weight));
                }
                p.render=f.feet+offset;
            }
            // Torso clearance, including simultaneous heavy/ordinary windups.
            // No authoritative grid positions are touched.
            for(int pass=0;pass<4;pass++)for(int i=0;i<_figures.Count;i++)
            {
                var a=_figures[i];if(!PairBody(a))continue;
                for(int j=i+1;j<_figures.Count;j++)
                {
                    var b=_figures[j];if(!PairBody(b))continue;
                    var delta=b.pair.render-a.pair.render;
                    if(Mathf.Abs(delta.y)>=78)continue;
                    float gap=PairWidth(a)+PairWidth(b)+6;
                    if(Mathf.Abs(delta.x)>=gap)continue;
                    float direction=delta.x==0?(b.feet.x>=a.feet.x?1:-1):Mathf.Sign(delta.x);
                    float push=(gap-Mathf.Abs(delta.x))*.5f;
                    a.pair.render.x-=direction*push;b.pair.render.x+=direction*push;
                }
            }
            foreach(var f in _figures)if(PairBody(f))_pairMaxAdvance=Mathf.Max(_pairMaxAdvance,Mathf.Abs(f.pair.render.x-f.feet.x));
        }
        void SnapPairPositions()
        {
            if(!PairsPresentation)return;
            foreach(var f in _figures)if(f.pair!=null&&f.unit.Alive)
            {
                f.feet=PairFeet(f.unit);var p=f.pair;p.from=p.to=p.render=f.feet;
                p.at=p.landed=p.reacted=-100;p.coming=false;p.follow=Vector2.zero;
            }
        }
        void PairCombatEvent(BattleEvent e,Figure actor,Figure target,string result="")
        {
            if(!PairsPresentation||_seeking)return;
            if(e.Kind==BattleEventKind.Parry)
            {
                if(actor?.pair!=null){actor.pair.reacted=_visualTime;actor.pair.guard=true;actor.pair.dodge=false;actor.pair.back=(actor.unit.Side==0?-1:1)*12;}
                if(target?.pair!=null){target.pair.reacted=_visualTime;target.pair.guard=target.pair.dodge=false;target.pair.back=(target.unit.Side==0?-1:1)*38;}
                return;
            }
            bool attack=e.Kind==BattleEventKind.Hit||e.Kind==BattleEventKind.Miss||e.Kind==BattleEventKind.Heal||e.Kind==BattleEventKind.TrapArmed;
            if(actor?.pair!=null&&(attack||e.Kind==BattleEventKind.Guard)&&!ChiefDamage(e))
            {
                var p=actor.pair;
                p.action=actor.table!=null?TableActionClip(actor,e.Kind==BattleEventKind.Heal,
                    e.Kind==BattleEventKind.Guard||e.Kind==BattleEventKind.TrapArmed||e.Detail==ClassSpells.StrikeDetail||e.Detail==ClassSpells.StrikeOpenDetail):actor.attack;
                p.landed=_visualTime;p.target=target?.unit.Id??"";
                float direction=actor.unit.Side==0?1:-1;
                p.follow=p.render-actor.feet;
                if(e.Kind==BattleEventKind.Guard)p.follow=new Vector2(direction*38,0);
                else if(!actor.unit.Stats.Ranged&&target!=null&&target.unit.Side!=actor.unit.Side)
                    p.follow.x=direction*Mathf.Clamp(Mathf.Abs(target.feet.x-actor.feet.x)-100,0,120);
                else if(actor.unit.Stats.Ranged)p.follow.x=direction*14;
            }
            if(target?.pair==null)return;
            var victim=target.pair;
            if(e.Kind==BattleEventKind.Miss)
            {
                victim.reacted=_visualTime;victim.dodge=result==AttackTable.DodgeDetail;victim.guard=!victim.dodge;
                victim.back=(target.unit.Side==0?-1:1)*(victim.dodge?38:12);
            }
            else if(e.Kind==BattleEventKind.Hit&&e.Amount>0)
            {
                victim.reacted=_visualTime;victim.dodge=false;victim.guard=result=="block";
                victim.back=(actor==null?(target.unit.Side==0?-1:1):Mathf.Sign(target.feet.x-actor.feet.x))*(victim.guard?12:result=="crit"?38:26);
                if(!victim.guard)SpawnPairBlood(target);
            }
        }
        bool PairCombatPose(Figure f,out Clip clip,out float time)
        {
            clip=null;time=0;if(!PairsPresentation||f.pair==null)return false;
            var p=f.pair;float reaction=_visualTime-p.reacted,after=_visualTime-p.landed;
            // A hit/block cannot swallow the whole next strike.
            if(reaction>=0&&reaction<.16f&&(!p.coming||p.left>.14f))
            {clip=p.dodge?(p.step??f.idle):p.guard?(f.block??f.hit):f.hit;time=reaction/.16f*clip.seconds*.7f;return true;}
            if(p.coming&&p.action!=null)
            {
                if(p.step!=null&&p.left>.36f&&!f.unit.Stats.Ranged&&f.unit.Class!=UnitClass.Warden&&f.unit.Class!=UnitClass.Medic)
                {clip=p.step;time=(PairLead-p.left)/(PairLead-.36f)*clip.seconds*.35f;return true;}
                clip=p.action;float lead=PitFrameTime(clip,clip.sheet.hitFrame);
                float windup=p.step!=null&&!f.unit.Stats.Ranged&&f.unit.Class!=UnitClass.Warden&&f.unit.Class!=UnitClass.Medic?.36f:PairLead;
                time=Mathf.Clamp01(1-p.left/windup)*lead;return true;
            }
            if(after>=0&&after<PairFollow&&p.action!=null)
            {clip=p.action;float hit=PitFrameTime(clip,clip.sheet.hitFrame);time=hit+(clip.seconds-hit)*Mathf.Clamp01(after/PairFollow);return true;}
            if(p.step!=null&&_visualTime-p.at<.4f)
            {clip=p.step;time=Mathf.Clamp01((_visualTime-p.at)/.4f)*clip.seconds;return true;}
            clip=f.idle;time=f.poseTime%f.idle.seconds;return true;
        }
        void SpawnPairBlood(Figure target)
        {
            if(_pairBloodClip==null)return;
            if(_pairBlood.Count>=32){Destroy(_pairBlood[0].image.gameObject);_pairBlood.RemoveAt(0);}
            var image=Raw(_stage,"Confirmed damage blood",0,0,1,1);image.raycastTarget=false;image.texture=_pairBloodClip.texture;
            image.transform.SetSiblingIndex(_readoutLayer.GetSiblingIndex());
            _pairBlood.Add(new PairBlood{image=image,target=target,origin=target.pair.render,at=_visualTime,mirror=target.unit.Side==0});
            _pairBloodSeen++;
        }
        void DrawPairBlood()
        {
            for(int i=_pairBlood.Count-1;i>=0;i--)
            {
                var fx=_pairBlood[i];float age=_visualTime-fx.at;
                if(age>=_pairBloodClip.seconds){Destroy(fx.image.gameObject);_pairBlood.RemoveAt(i);continue;}
                var c=_pairBloodClip;var rect=c.sheet.frames[c.FrameAt(age,false)].frame;
                float tx=rect.x/(float)c.texture.width,ty=1-(rect.y+rect.h)/(float)c.texture.height,tw=rect.w/(float)c.texture.width;
                fx.image.uvRect=new Rect(fx.mirror?tx+tw:tx,ty,fx.mirror?-tw:tw,rect.h/(float)c.texture.height);
                // Detach from the wound instead of riding the recovery back home.
                var point=fx.origin+new Vector2(fx.mirror?-14:14,-fx.target.head*.55f);
                fx.image.rectTransform.anchoredPosition=new Vector2(Mathf.Round(point.x-(fx.mirror?rect.w-c.sheet.anchorX:c.sheet.anchorX)),-Mathf.Round(point.y-c.sheet.anchorY));
                fx.image.rectTransform.sizeDelta=new Vector2(rect.w,rect.h);
            }
        }
    }
}
