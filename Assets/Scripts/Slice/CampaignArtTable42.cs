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
        bool _table42Review;
        bool TablePresentation => (_table42Review||PairsPresentation)&&(_fight!=null||_playback!=null)&&ViewSim.Fight.Table;
        bool LeaderParryPresentation => (_fight!=null||_playback!=null)&&ViewSim.PitOn&&!ViewSim.StageOn&&
            (ViewSim.Fight.Table||_playback==null&&_fight.RulesVersion>=41);
        float _tableClock;
        sealed class TablePose
        {
            public Clip alternate,heal,power,current;
            public float start=-100,holdUntil=-100,impactAt=-100,dodgeAt=-100;
            public float kick,freeze;
            public int whiteFrame=-1,turn;
            public long strikeTick=-1;
            public bool guard;
        }
        sealed class TableImpact
        {public Figure actor,target;public float at,hold;public int amount;public bool crit;}
        readonly List<TableImpact> _tableImpacts=new List<TableImpact>();
        readonly List<TableImpact> _tableRecent=new List<TableImpact>();
        BattleEvent _tableStruck;
        readonly Dictionary<string,CashWord> _tableWords=new Dictionary<string,CashWord>();
        int _tableHitsSeen,_tableDodgesSeen,_tableParriesSeen,_tableBlocksSeen,_tableCritsSeen;

        void ClearTablePresentation()
        {
            _tableClock=0;_tableImpacts.Clear();_tableRecent.Clear();_tableWords.Clear();_tableStruck=default;
            _tableHitsSeen=_tableDodgesSeen=_tableParriesSeen=_tableBlocksSeen=_tableCritsSeen=0;
            ClearChiefEffects();
        }
        void PrepareTableFigure(Figure f)
        {
            if(!TablePresentation||f.attack==null)return;
            f.table=new TablePose{power=f.attack};
            if(f.artKey==null||!HasTableUnit(f.artKey+"_idle"))return;
            f.block=LoadClip(f.artKey+"_block");
            f.table.power=LoadClip(f.artKey+"_power");
            if(f.unit.Class==UnitClass.Medic)f.table.heal=LoadClip(f.artKey+"_heal");
            else f.table.alternate=LoadClip(f.artKey+"_attack2");
        }
        static bool HasTableUnit(string key)=>Resources.Load<TextAsset>("Table42Review/Units/"+key)!=null;
        void TableWord(Figure target,string kind)
        {
            if(!_tableWords.TryGetValue(target.unit.Id,out var pop))
            {pop=new CashWord{image=Raw(_readoutLayer,"Attack table "+kind,0,0,1,1)};pop.image.raycastTarget=false;_tableWords.Add(target.unit.Id,pop);}
            pop.image.texture=Resources.Load<Texture2D>("Table42Review/Words/"+kind);
            pop.at=_visualTime;pop.word=kind;pop.position=new Vector2(target.feet.x,target.feet.y-target.head-22);
        }
        bool ChiefDamage(BattleEvent e)=>e.Kind==BattleEventKind.Hit&&
            (e.Detail==Moves.EclipseDetail||e.Detail==Moves.PyreDetail||e.Detail==ChiefSpells.BiteDetail||e.Detail==ChiefSpells.BloodDetail||e.Detail==ChiefSpells.TideDetail);

        void TableEvent(BattleEvent e,Figure actor,Figure target)
        {
            if(!TablePresentation||_seeking)return;
            if(e.Kind==BattleEventKind.Struck){_tableStruck=e;return;}
            string pairResult=e.Kind==BattleEventKind.Hit&&_tableStruck.Kind==BattleEventKind.Struck&&
                _tableStruck.Tick==e.Tick&&_tableStruck.ActorId==e.ActorId&&_tableStruck.TargetId==e.TargetId?_tableStruck.Detail:e.Detail;
            PairCombatEvent(e,actor,target,pairResult);
            bool strike=e.Kind==BattleEventKind.Hit||e.Kind==BattleEventKind.Miss||e.Kind==BattleEventKind.Heal||e.Kind==BattleEventKind.TrapArmed;
            if(strike&&actor?.table!=null&&actor.table.strikeTick!=e.Tick&&!ChiefDamage(e))
            {
                var p=actor.table;Clip clip=TableActionClip(actor,e.Kind==BattleEventKind.Heal,
                    e.Detail==ClassSpells.StrikeDetail||e.Detail==ClassSpells.StrikeOpenDetail||e.Kind==BattleEventKind.TrapArmed);
                p.current=clip;p.start=actor.poseTime-PitFrameTime(clip,clip.sheet.hitFrame);p.guard=false;p.strikeTick=e.Tick;p.turn++;
                actor.drawnClip=clip;actor.drawnFrame=clip.sheet.hitFrame;
            }
            if(actor?.table!=null&&(e.Kind==BattleEventKind.Guard||e.Kind==BattleEventKind.Mark&&e.Detail==ClassSpells.SnareMark))
            {actor.table.current=actor.table.power;actor.table.start=actor.poseTime-PitFrameTime(actor.table.power,actor.table.power.sheet.hitFrame);actor.table.guard=false;}
            if(target==null)return;
            if(e.Kind==BattleEventKind.Miss&&(e.Detail==AttackTable.DodgeDetail||e.Detail==AttackTable.ParryDetail))
            {
                string kind=e.Detail;TableWord(target,kind);SpawnReviewFx("Effects/"+kind,target,0);
                if(kind==AttackTable.DodgeDetail)
                { _tableDodgesSeen++;if(target.table!=null){target.table.dodgeAt=_tableClock;target.table.current=null;} }
                else
                { _tableParriesSeen++;TableGuard(target); }
            }
            if(e.Kind==BattleEventKind.Hit&&e.Amount>0&&!ChiefDamage(e))
            {
                bool matched=_tableStruck.Kind==BattleEventKind.Struck&&_tableStruck.Tick==e.Tick&&
                    _tableStruck.ActorId==e.ActorId&&_tableStruck.TargetId==e.TargetId;
                string kind=matched?_tableStruck.Detail:"hit";
                if(matched)_tableStruck=default;
                if(kind!="block"&&kind!="crit")kind="hit";
                if(kind!="hit")TableWord(target,kind);
                if(!PairsPresentation||kind=="block")SpawnReviewFx("Effects/"+kind,target,0);
                _tableHitsSeen++;if(kind=="block")_tableBlocksSeen++;if(kind=="crit")_tableCritsSeen++;
                if(kind=="block")TableGuard(target);
                else if(target.table!=null){target.table.current=target.hit;target.table.start=target.poseTime;target.table.guard=false;target.drawnClip=target.hit;target.drawnFrame=0;}
                bool heavy=ClassSpells.TierOf(e.Detail)!=HitTier.Light||e.Amount>=Mathf.Max(1,target.unit.MaxHp/5);
                _tableImpacts.Add(new TableImpact{actor=actor,target=target,at=_tableClock,amount=e.Amount,crit=kind=="crit",hold=kind=="crit"?.2f:kind=="block"||heavy?.12f:.06f});
                if(target.table!=null)
                {target.table.impactAt=_tableClock;target.table.kick=(heavy||kind=="crit"?8:4)*(actor==null?(target.unit.Side==0?-1:1):Mathf.Sign(target.feet.x-actor.feet.x));}
            }
            if(e.Kind==BattleEventKind.Hit&&ChiefDamage(e)&&target.table!=null)
            {target.table.current=target.hit;target.table.start=target.poseTime;target.table.guard=false;}
            if(e.Kind==BattleEventKind.Kill){_shakeAt=_lastShakeAt=_visualTime;}
        }
        void TableGuard(Figure f)
        {
            if(f.table==null||f.block==null)return;
            f.table.current=f.block;f.table.start=f.poseTime-PitFrameTime(f.block,4);f.table.guard=true;f.drawnClip=f.block;f.drawnFrame=4;
        }
        Clip TableActionClip(Figure f,bool heal,bool power)
        {return power?f.table.power:heal&&f.table.heal!=null?f.table.heal:f.table.turn%2==1&&f.table.alternate!=null?f.table.alternate:f.attack;}
        void AnticipateTable()
        {
            if(!TablePresentation)return;
            for(int side=0;side<2;side++)foreach(var blow in ViewSim.Blows(side))
            {
                var f=FindFigure(blow.AttackerId);if(f?.table==null||!f.unit.Alive)continue;
                if(BeatsOn&&!blow.Parryable)continue;   // its beat winds it up (CampaignArtBeats.cs)
                var p=f.table;
                if(p.current==f.hit&&f.poseTime-p.start<f.hit.seconds||p.guard&&f.poseTime-p.start<f.block.seconds)continue;
                var c=TableActionClip(f,f.unit.Class==UnitClass.Medic,blow.Kind==BlowType.Move);
                float lead=PitFrameTime(c,c.sheet.hitFrame),left=(blow.TicksLeft-1)/(float)SimClock.TicksPerSecond;
                if(left<0||left>lead)continue;
                p.current=c;p.guard=false;p.start=f.poseTime-Mathf.Max(0,lead-left);
            }
        }
        bool TableAttackPose(Figure f,out Clip c,out float time)
        {
            c=null;time=0;if(!TablePresentation||f.table==null)return false;
            var p=f.table;
            if(_tableClock-p.dodgeAt<.25f){c=f.idle;time=0;return true;}
            if(p.current==null)return false;
            time=f.poseTime-p.start;c=p.current;
            return time>=0&&time<c.seconds;
        }
        void ResolveTableImpacts()
        {
            if(!TablePresentation)return;
            _tableRecent.RemoveAll(x=>_tableClock-x.at>.1f);
            foreach(var hit in _tableImpacts)_tableRecent.Add(hit);
            TableImpact biggest=null;
            foreach(var hit in _tableRecent)if(biggest==null||hit.hold>biggest.hold||hit.hold==biggest.hold&&hit.amount>biggest.amount)biggest=hit;
            if(_tableRecent.Count>=3)
                foreach(var f in _figures)if(f.table!=null&&f!=biggest?.target&&f!=biggest?.actor){f.table.holdUntil=-100;f.table.whiteFrame=-1;}
            foreach(var hit in _tableImpacts)
            {
                if(_tableRecent.Count>=3&&hit!=biggest)continue;
                foreach(var f in _figures)if(f.table!=null&&(f==hit.target||f==hit.actor))f.table.holdUntil=Mathf.Max(f.table.holdUntil,_tableClock+hit.hold);
                if(hit.target.table!=null){hit.target.table.whiteFrame=Time.frameCount;hit.target.table.freeze=hit.hold;}
                if(hit.crit)_shakeAt=_lastShakeAt=_visualTime;
            }
            _tableImpacts.Clear();
        }
        Vector2 TableOffset(Figure f)
        {
            if(f.table==null)return Vector2.zero;
            var p=f.table;float age=_tableClock-p.impactAt;
            float kick=age>=0?p.kick*Settle(Mathf.Max(0,age-p.freeze),.1f):0;
            float jitter=!_paused&&_tableClock<p.holdUntil?(Time.frameCount/2%2==0?1:-1):0;
            float dodge=Settle(_tableClock-p.dodgeAt,.25f)*6*(f.unit.Side==0?-1:1);
            return new Vector2(kick+jitter+dodge,0);
        }
        void DrawTableWords()
        {
            foreach(var pop in _tableWords.Values)
            {
                float age=_visualTime-pop.at;pop.image.enabled=age>=0&&age<.65f;
                if(!pop.image.enabled)continue;
                float scale=age<.1f?1.2f-.2f*age/.1f:1;
                pop.image.rectTransform.localScale=Vector3.one*scale;
                pop.image.rectTransform.sizeDelta=new Vector2(pop.image.texture.width,pop.image.texture.height);
                pop.image.rectTransform.anchoredPosition=new Vector2(Mathf.Round(pop.position.x-pop.image.texture.width/2f),-Mathf.Round(pop.position.y-age*22));
            }
        }
    }
}
