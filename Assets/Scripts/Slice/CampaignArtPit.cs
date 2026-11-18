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
        // Version-gated presentation. No commands, damage or combat clocks are changed here.
        bool PitPresentation => (_fight!=null||_playback!=null) && ViewSim.PitOn;
        sealed class PitPose
        {
            public float start=-100, strike, confirmed=-100;
            public bool coming, ranged, move, parryable;
            public int left, segment, nextSegment;
            public long lastStrike=-1;
            public PitSegment[] segments;
            public string target="";
            public Image shot;
        }
        [Serializable] sealed class PitSegment { public int start,strike,end,leadInMs,lengthMs,strikeAtMsInClip; }
        [Serializable] sealed class PitSheet { public string name;public PitSegment[] segments; }
        [Serializable] sealed class PitSheets { public PitSheet[] clips; }
        PitSheets _pitSheets;
        float PitFrameTime(Clip clip,int frame)
        {float time=0;for(int i=0;i<Mathf.Min(frame,clip.sheet.frames.Length);i++)time+=clip.sheet.frames[i].duration/1000f;return time;}
        sealed class PitSpark { public Image image; public float at; public Vector2 point; public float size; }
        readonly List<PitSpark> _pitSparks=new List<PitSpark>();

        void PreparePitFigure(Figure f)
        {
            if(!PitPresentation||f.attack==null)return;
            if(f.table!=null){f.pit=new PitPose{segments=new[]{new PitSegment{start=0,strike=f.attack.sheet.hitFrame,end=f.attack.sheet.frames.Length-1}}};return;}
            if(_pitSheets==null)_pitSheets=JsonUtility.FromJson<PitSheets>(Resources.Load<TextAsset>("Presentation/PitStrikeFrames").text);
            PitSegment[] segments=null;
            foreach(var sheet in _pitSheets.clips)if(sheet.name==f.attack.texture.name){segments=sheet.segments;break;}
            // Beast clips already carry their own strike. Every loaded humanoid has Claude's handback.
            if(segments==null)
            {
                int frame=Mathf.Clamp(f.attack.sheet.hitFrame,0,f.attack.sheet.frames.Length-1);
                if(frame==0)Debug.LogWarning("Pit strike metadata missing: "+f.attack.texture.name);
                segments=new[]{new PitSegment{start=0,strike=frame,end=f.attack.sheet.frames.Length-1}};
            }
            f.pit=new PitPose{segments=segments,strike=PitFrameTime(f.attack,segments[0].strike)-PitFrameTime(f.attack,segments[0].start)};
            f.attackLength=f.attack.seconds;
        }
        void AnticipatePit()
        {
            AnticipateTable();
            foreach(var f in _figures)if(f.pit!=null)f.pit.coming=false;
            for(int side=0;side<2;side++)foreach(var blow in ViewSim.Blows(side))
            {
                var f=FindFigure(blow.AttackerId);if(f==null||f.pit==null||f.pit.coming)continue;
                // One blow at a time winds a blow up in its own beat; only a parryable swing shows its windup early.
                if(BeatsOn&&!blow.Parryable)continue;
                var p=f.pit;float left=((blow.Parryable?blow.Move.RealBeatTicks:blow.TicksLeft)-1)/(float)SimClock.TicksPerSecond;
                var segment=p.segments[p.nextSegment];
                float lead=PitFrameTime(f.attack,segment.strike)-PitFrameTime(f.attack,segment.start);
                if(left>lead&&blow.Kind!=BlowType.Move)continue;
                p.segment=p.nextSegment;p.strike=lead;
                p.coming=true;p.ranged=blow.Ranged;p.move=blow.Kind==BlowType.Move;p.parryable=blow.Parryable;
                p.left=blow.Parryable?blow.Move.RealBeatTicks:blow.TicksLeft;p.target=blow.TargetId;
                p.start=f.poseTime-Mathf.Max(0,p.strike-left);
            }
        }
        bool PitAttackPose(Figure f,out float time)
        {
            time=0;if(!PitPresentation||f.pit==null)return false;
            if(ViewSim.StageOn)return false;
            var p=f.pit;
            var segment=p.segments[p.segment];float start=PitFrameTime(f.attack,segment.start),strike=PitFrameTime(f.attack,segment.strike);
            if(p.coming){time=p.parryable?Mathf.Max(start,strike-.001f):start+Mathf.Clamp(f.poseTime-p.start,0,Mathf.Max(0,p.strike-.001f));return true;}
            if(p.confirmed>=0&&f.poseTime-p.confirmed<PitFrameTime(f.attack,segment.end+1)-strike)
            {time=strike+Mathf.Max(0,f.poseTime-p.confirmed);return true;}
            return false;
        }
        float PitApproach(Figure f)
        {
            if(ViewSim.StageOn||LeaderParryPresentation)return 0;
            var p=f.pit;if(p==null||p.ranged||!p.coming||!f.unit.Alive)return 0;
            var target=FindFigure(p.target);if(target==null||!target.unit.Alive)return 0;
            float gap=target.feet.x-f.feet.x;
            float phase=Mathf.Clamp01((p.strike-(p.left-1)/(float)SimClock.TicksPerSecond)/Mathf.Max(.01f,p.strike));
            return Mathf.Sign(gap)*Mathf.Min(30,Mathf.Abs(gap)*.25f)*phase;
        }
        void PitEvent(BattleEvent e,Figure actor,Figure target)
        {
            if(!PitPresentation)return;
            if(!_seeking){PitParryEvent(e,actor,target);PitSpellEvent(e,actor,target);}
            bool strike=e.Kind==BattleEventKind.Hit||e.Kind==BattleEventKind.Miss||e.Kind==BattleEventKind.Heal||
                e.Kind==BattleEventKind.TrapArmed||e.Kind==BattleEventKind.Release&&e.Detail==PitRules.Lost||
                e.Kind==BattleEventKind.Land&&e.Detail.EndsWith(".lost",StringComparison.Ordinal);
            if(!ViewSim.StageOn&&strike&&actor!=null&&actor.table==null&&actor.pit!=null&&actor.pit.lastStrike!=e.Tick)
            {
                actor.pit.coming=false;actor.pit.confirmed=actor.poseTime;actor.pit.target=e.TargetId;
                actor.pit.lastStrike=e.Tick;actor.pit.nextSegment=(actor.pit.segment+1)%actor.pit.segments.Length;
                actor.drawnClip=actor.attack;actor.drawnFrame=actor.pit.segments[actor.pit.segment].strike;
                if(actor.pit.shot!=null)actor.pit.shot.enabled=false;
            }
            if(_seeking)return;
            if(!TablePresentation&&e.Kind==BattleEventKind.Hit&&e.Amount>0&&target!=null)
            {
                var tier=ClassSpells.TierOf(e.Detail);bool heavy=tier!=HitTier.Light||e.Amount>=Mathf.Max(1,target.unit.MaxHp/5);
                target.majorWhiteUntil=_visualTime+1f/60;
                if(_pitSparks.Count<24)
                {
                    float size=tier==HitTier.Finisher?16:heavy?11:6;
                    var spark=new PitSpark{at=_visualTime,size=size,point=new Vector2(target.feet.x,target.feet.y-target.head*.45f),
                        image=Panel(_readoutLayer,0,0,size,size,Color.white)};
                    spark.image.name="Impact / ART PENDING";spark.image.raycastTarget=false;
                    spark.image.rectTransform.localEulerAngles=new Vector3(0,0,45);_pitSparks.Add(spark);
                }
            }
        }
        Color PitHitTint(Figure f,Color tint)
        {
            if(!PitPresentation||_paused||TablePresentation)return tint;
            float age=_visualTime-f.hitAt;
            return age>=.075f&&age<.11f||age>=.17f&&age<.205f?new Color(1,.42f,.35f,tint.a):tint;
        }
        void DrawPit()
        {
            if(!PitPresentation)return;
            DrawPitParry();
            DrawPitSpells();
            foreach(var f in _figures)
            {
                var p=f.pit;if(p==null)continue;
                bool stageShot=StageHolding&&f==_stageActor&&f.unit.Stats.Ranged&&!_stageLanded;
                bool beatShot=BeatShot(f,out var beatTarget,out float flight);
                bool firing=stageShot||beatShot||p.coming&&p.ranged&&p.left<=8&&f.unit.Alive&&!ViewOver;
                var target=beatShot?beatTarget:stageShot?_stageTarget:firing?FindFigure(p.target):null;
                firing&=target!=null&&(target.unit.Alive||BeatAlive(target));
                if(firing&&p.shot==null)
                {p.shot=Panel(_readoutLayer,0,0,12,3,f.unit.Side==0?PxBone:PxRed);p.shot.name="Shot / ART PENDING";p.shot.raycastTarget=false;}
                if(p.shot==null)continue;p.shot.enabled=firing;
                if(firing)
                {
                    Vector2 from=new Vector2(f.feet.x,f.feet.y-f.head*.5f),to=new Vector2(target.feet.x,target.feet.y-target.head*.5f);
                    if(PairsPresentation){from=PitChest(f);to=PitChest(target);}
                    if(beatShot){from=new Vector2(BeatNow(f).x,from.y);to=new Vector2(BeatNow(target).x,to.y);}
                    Vector2 point=Vector2.Lerp(from,to,beatShot?flight:stageShot?_stageAge/Mathf.Max(.001f,_stageLead):1-(p.left-1)/8f);
                    p.shot.rectTransform.anchoredPosition=new Vector2(Mathf.Round(point.x),-Mathf.Round(point.y));
                    p.shot.rectTransform.localEulerAngles=new Vector3(0,0,-Mathf.Atan2(to.y-from.y,to.x-from.x)*Mathf.Rad2Deg);
                }
            }
            for(int i=_pitSparks.Count-1;i>=0;i--)
            {
                var s=_pitSparks[i];float age=_visualTime-s.at;
                if(age>=.13f){Destroy(s.image.gameObject);_pitSparks.RemoveAt(i);continue;}
                s.image.rectTransform.anchoredPosition=new Vector2(s.point.x-s.size/2,-s.point.y+s.size/2);
                s.image.rectTransform.localScale=Vector3.one*(1+age*5);
                s.image.color=new Color(1,.92f,.82f,Mathf.Clamp01(1-age/.13f));
            }
        }
        static string PitMoveWord(string detail)
        {
            if(string.IsNullOrEmpty(detail))return "";
            int dot=detail.LastIndexOf('.');string word=dot>=0?detail.Substring(dot+1):detail;
            return word.Replace(" ","").Replace("-","").ToUpperInvariant();
        }
        void PitFloatNumber(Figure f,int amount,bool heal,long tick,string actor,bool focus,string detail)
        {
            int count=0;Number oldest=null;
            foreach(var n in _numbers)if(n.fighterId==f.unit.Id&&_visualTime-n.at<n.life)
            {count++;if(n.heal==heal&&(oldest==null||n.at<oldest.at))oldest=n;}
            bool payoff=!heal&&tick>=0&&CashHitAt(f,tick,actor);
            bool heavy=!heal&&(ClassSpells.TierOf(detail)!=HitTier.Light||amount>=Mathf.Max(1,f.unit.MaxHp/5));
            string word=detail==BattleSimulation.GuardedDetail?"GUARDED":heavy&&f.unit.Side==0?PitMoveWord(detail):"";
            // Firebomb shares the core's eclipse damage detail. Name the recorded caster's
            // actual spell in the review presenter, without changing that legacy event format.
            if(TablePresentation&&word=="ECLIPSE"&&FindFigure(actor) is Figure caster)
                word=PitMoveWord(ChiefSpells.Name(ViewSim.Spell(caster.unit.Side)));
            // One blow at a time keeps one running number per fighter: a blow adds to the number still showing and it
            // pops again (CampaignArtBeats.cs), so quick blows never stack a column of receipts.
            if(BeatsOn)
                foreach(var n in _numbers)
                    if(n.fighterId==f.unit.Id&&n.heal==heal&&_visualTime-n.at<n.life)
                    {
                        n.sum+=amount;n.at=_visualTime;n.life=1.1f;n.payoff|=payoff||heavy;
                        if(word.Length>0)n.word=word;
                        DrawNumber(n,heal,f);return;
                    }
            if(count>=4&&oldest!=null)
            {oldest.sum+=amount;oldest.life=_visualTime-oldest.at+1.1f;oldest.word="TOTAL";DrawNumber(oldest,heal,f);return;}
            if(count>=4)
            {
                // Four damage receipts followed by a heal (or vice versa): merge like with like,
                // never turn healing into damage or exceed the four-receipt visual budget.
                Number first=null,second=null;
                foreach(var n in _numbers)if(n.fighterId==f.unit.Id&&_visualTime-n.at<n.life)
                {if(first==null)first=n;else if(n.heal==first.heal){second=n;break;}}
                if(first!=null&&second!=null)
                {
                    second.sum+=first.sum;second.word="TOTAL";DrawNumber(second,second.heal,f);
                    if(first.image.texture!=null){_screenTextures.Remove((Texture2D)first.image.texture);Destroy(first.image.texture);}
                    Destroy(first.image.gameObject);_numbers.Remove(first);count--;
                }
            }
            var number=new Number{sum=amount,payoff=payoff||heavy,focus=focus,heal=heal,fighterId=f.unit.Id,word=word,bars=payoff?ViewSim.ParryBars(FindFigure(actor)?.unit.Side??0):0,
                at=_visualTime,life=1.1f,rise=32,origin=new Vector2(f.feet.x,Mathf.Max(145,f.feet.y-f.head*.55f-count*24))};
            number.image=Raw(_effects,"Hit receipt",0,0,1,1);number.image.raycastTarget=false;
            DrawNumber(number,heal,f);_numbers.Add(number);
        }
    }
}
