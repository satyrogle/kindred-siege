using System;
using System.Collections.Generic;
using KindredSiege.Sim.Battle;
using UnityEngine;

namespace KindredSiege.Slice
{
    public sealed partial class CampaignArtLoop
    {
        const float StageSeconds=.5f;
        sealed class StageBeat
        {
            public BattleEvent blow;
            public readonly List<BattleEvent> effects=new List<BattleEvent>();
            public bool move,impact;
        }
        readonly Queue<StageBeat> _stageQueue=new Queue<StageBeat>();
        StageBeat _stageBeat;
        Figure _stageActor,_stageTarget;
        float _stageAge,_stageLead,_stageClipStart,_stageStrike,_stageClipEnd;
        bool _stageLanded;
        int _stageContactFrame,_stageHomeSibling;
        bool StageHolding=>_stageBeat!=null;
        bool StageBystander(Figure f)=>StageHolding&&f!=_stageActor&&f!=_stageTarget;

        void ClearStageBeats()
        {
            _stageQueue.Clear();
            _stageBeat=null;_stageActor=_stageTarget=null;
        }
        static bool StageMove(BattleEvent e)=>e.Kind==BattleEventKind.Release||e.Kind==BattleEventKind.Land;
        static bool StageImpact(BattleEvent e)=>e.ActorId.Length!=0&&
            (e.Kind==BattleEventKind.Hit||e.Kind==BattleEventKind.Heal||e.Kind==BattleEventKind.Miss);

        // Drain the authoritative event stream once. A Release and its damage (or damage
        // followed by Land) describe the same move; an independent turn gets its own beat.
        void PresentBattleEvents(IReadOnlyList<BattleEvent> events,int first)
        {
            if(first>=events.Count)return;
            StageBeat last=null;
            for(int i=first;i<events.Count;i++)
            {
                var e=events[i];bool move=StageMove(e),impact=StageImpact(e);
                if(move||impact)
                {
                    bool pair=last!=null&&last.blow.Tick==e.Tick&&last.blow.ActorId==e.ActorId&&
                        (last.blow.TargetId==e.TargetId||last.blow.TargetId.Length==0||e.TargetId.Length==0)&&
                        (move&&!last.move&&last.impact||impact&&last.move&&!last.impact);
                    if(!pair){last=new StageBeat{blow=e};_stageQueue.Enqueue(last);}
                    if(impact){last.blow=e;last.impact=true;}
                    if(move)last.move=true;
                }
                if(last!=null)last.effects.Add(e);
                else AnimateEvent(e);
            }
            if(_seeking)
            {
                // Seek consumes the same beats without waiting. Preserve segment rotation
                // so the next played swing uses the same clip as sequential playback.
                while(_stageQueue.Count>0)
                {
                    var beat=_stageQueue.Dequeue();
                    SelectStageSegment(FindFigure(beat.blow.ActorId));
                    foreach(var e in beat.effects)AnimateEvent(e);
                }
            }
            else if(!StageHolding)BeginNextStageBeat();
        }

        static void SelectStageSegment(Figure actor)
        {
            if(actor?.pit==null)return;
            var p=actor.pit;
            p.segment=p.nextSegment;p.nextSegment=(p.segment+1)%p.segments.Length;
        }
        void BeginNextStageBeat()
        {
            if(_stageQueue.Count==0){_stageBeat=null;_stageActor=_stageTarget=null;return;}
            _stageBeat=_stageQueue.Dequeue();
            _stageActor=FindFigure(_stageBeat.blow.ActorId);
            _stageTarget=FindFigure(_stageBeat.blow.TargetId);
            if(_stageActor!=null)
            {
                _stageHomeSibling=_stageActor.root.GetSiblingIndex();
                int front=_stageHomeSibling;
                foreach(var f in _figures)front=Math.Max(front,f.root.GetSiblingIndex());
                _stageActor.root.SetSiblingIndex(front);
            }
            _stageAge=0;_stageLanded=false;_stageLead=.12f;
            if(_stageActor?.pit!=null)
            {
                var p=_stageActor.pit;
                SelectStageSegment(_stageActor);
                var seg=p.segments[p.segment];
                _stageStrike=PitFrameTime(_stageActor.attack,seg.strike);
                _stageClipEnd=PitFrameTime(_stageActor.attack,seg.end+1);
                _stageLead=.20f;
                _stageClipStart=PitFrameTime(_stageActor.attack,seg.start);
            }
        }
        void LandStageBeat()
        {
            _stageLanded=true;_stageContactFrame=Time.frameCount;
            foreach(var e in _stageBeat.effects)AnimateEvent(e);
        }
        // dt is wall time. Pausing suspends the beat; speed changes only affect time
        // between beats. Never add the held interval to the tick accumulator.
        void AdvanceStage(ref float dt)
        {
            while(StageHolding&&dt>0)
            {
                float used=Mathf.Min(dt,StageSeconds-_stageAge);
                _stageAge+=used;dt-=used;_visualTime+=used;
                AdvanceCombatPoses(used);
                if(!_stageLanded&&_stageAge>=_stageLead)LandStageBeat();
                if(_stageAge<StageSeconds)return;
                if(_stageActor!=null)
                {
                    _stageActor.root.SetSiblingIndex(_stageHomeSibling);
                    _stageActor.root.anchoredPosition=new Vector2(_stageActor.feet.x,-_stageActor.feet.y);
                    _stageActor.readoutRoot.anchoredPosition=_stageActor.root.anchoredPosition;
                }
                BeginNextStageBeat();
            }
        }
        bool StageAttackPose(Figure f,out float time)
        {
            time=0;
            if(!StageHolding||f!=_stageActor||f.pit==null)return false;
            time=_stageLanded&&_stageContactFrame==Time.frameCount?_stageStrike+.00001f:_stageStrike-_stageLead+_stageAge;
            // A short native segment can finish before its half second on stage.
            return time>=_stageClipStart&&time<_stageClipEnd;
        }
        bool IsSharkStageBeat()=>_stageActor!=null&&IsSharkPart(_stageActor.unit)||
            _stageTarget!=null&&IsSharkPart(_stageTarget.unit);
        bool StageMeleePosition(Figure f,out Vector2 position)
        {
            position=f.feet;
            if(f!=_stageActor||_stageTarget==null||f==_stageTarget||f.unit.Stats.Ranged||
                !StageHolding||_stageBeat.blow.Kind==BattleEventKind.Heal||IsSharkPart(f.unit))return false;
            // Bring the feet onto the target's lane, leaving room for the authored weapon.
            // No unit positions are written back to the battle.
            var delta=_stageTarget.feet-f.feet;
            delta.x-=Mathf.Sign(delta.x)*Mathf.Min(Mathf.Abs(delta.x),70);
            float phase=_stageAge<_stageLead?_stageAge/_stageLead:
                1-Mathf.Clamp01((_stageAge-_stageLead-.055f)/(StageSeconds-_stageLead-.055f));
            position+=delta*Mathf.SmoothStep(0,1,phase);
            return true;
        }
    }
}
