using System;
using System.Collections.Generic;
using KindredSiege.Sim;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using UnityEngine;
using UnityEngine.UI;

namespace KindredSiege.Slice
{
    public sealed partial class CampaignArtLoop
    {
        // One blow at a time from campaign rules 41 (Jacob, 8 Nov 2026: "Yes, both" to holding the screen per blow, then
        // "Not now fix the combat bro" and "Go, take it"). The sim still plays everyone at once. The screen holds the fight
        // between Tick() calls while each blow plays, as the rules 40 stage did, but nothing cuts between blows: units not in
        // the blow keep moving and keep their bars, a runner's way home overlaps the next blow, and every number, word and
        // flash lives on its own clock. Burn and bleed ticks are never blows. While a parry is coming at our side the fight
        // runs in real time, so its circle closes smoothly and the window keeps its 0.2 s. Presentation only: the sim, saves
        // and replays do not change. Numbers are starting values (HANDOFF_LOG 14572).
        sealed class Beat
        {
            public BattleEvent blow;
            public readonly List<BattleEvent> effects=new List<BattleEvent>();
            public Figure actor,target,aim,guard,fallen;
            public float lead,strikeAt,hold,poseStart;
            public bool landed,shot,big,parry;
            // Everyone the blow touches: the actor, his target, a guarding Warden and every fighter its events land on.
            public readonly List<Figure> lit=new List<Figure>();
        }
        sealed class BeatRun
        {
            public Vector2 from,to;public float start,outFor,backAt,backFor;
            // Out and home again with an ease at both ends, so a runner speeds up and slows down instead of sliding.
            public Vector2 At(float time,Vector2 home)
            {
                if(time<start)return from;
                if(time<start+outFor)return Vector2.Lerp(from,to,Mathf.SmoothStep(0,1,(time-start)/outFor));
                if(time<backAt)return to;
                if(time<backAt+backFor)return Vector2.Lerp(to,home,Mathf.SmoothStep(0,1,(time-backAt)/backFor));
                return home;
            }
            public bool Done(float time)=>time>=backAt+backFor;
        }
        sealed class BeatWord {public RawImage image;public float at;public Figure f;public int slot;public string key;}
        sealed class BeatMark {public Figure f;public string key;}
        const float BeatRunSpeed=1400,BeatRunMin=.14f,BeatLunge=.1f,BeatLungeReach=150,BeatGap=70,BeatShotFlight=.12f,
            BeatGuardStep=34,BeatWordLife=.9f;
        // The hold ladder (Jacob, 8 Nov 2026, "Build all five", from the 7 Nov big moments mock he okayed: small hits about
        // 0.3 s, big ones 0.7 s, kills 1.1 s). The swing winds up fast and the time goes to the hit; our leader's parry and
        // the champion's fall hold 2 s under their titles.
        const float BeatWindSmall=.1f,BeatWindBig=.16f,BeatHoldSmall=.2f,BeatHoldMiss=.2f,BeatHoldBig=.55f,BeatHoldTrap=.45f,
            BeatHoldKill=.95f,BeatHoldSpell=1.1f,BeatHoldMoment=2f;
        // While a chief's warning is up the blows are quick and the fight's clock runs beside them (fix 5).
        const float BeatWarnRunSpeed=2400,BeatWarnRunMin=.08f,BeatWarnLunge=.07f,BeatWarnWind=.06f,BeatWarnHold=.08f,
            BeatWarnBig=.3f,BeatWarnKill=.6f;
        const int BeatWarnQueue=3;
        const string BeatHierophant="hierophant";
        readonly List<Beat> _beatQueue=new List<Beat>();
        Beat _beat;
        float _beatAge,_beatTitleAt=-100,_beatDimAt=-100;
        // Damage, healing and deaths the sim has already dealt that the screen has not shown yet, by unit id.
        readonly Dictionary<string,int> _beatHp=new Dictionary<string,int>(),_beatKills=new Dictionary<string,int>();
        readonly Dictionary<Figure,BeatRun> _beatRuns=new Dictionary<Figure,BeatRun>();
        readonly Dictionary<Figure,float> _beatDim=new Dictionary<Figure,float>();
        readonly List<BeatWord> _beatWords=new List<BeatWord>();
        readonly List<BeatMark> _beatMarksShown=new List<BeatMark>();
        RawImage _beatTitle;
        string _beatTitleText="",_beatTitleNext;
        float _beatTitleLife,_beatTitleNextAt,_beatShakeAt=-100;

        bool BeatsOn=>LeaderParryPresentation&&!ViewSim.Fight.LockedPairs;
        bool BeatHolding=>_beat!=null;
        bool BeatsPending=>_beat!=null||_beatQueue.Count>0;
        // A chief's warning: either side's spell is winding up (fix 5).
        bool BeatsWarning=>BeatsOn&&(ViewSim.WindupTicks(0)>0||ViewSim.WindupTicks(1)>0);
        // The sim waits for the screen while blows are left to play, except during a parry's run up, and during a chief's
        // warning while fewer than BeatWarnQueue blows wait, so the warning lasts about its real length on screen.
        bool BeatsWait=>(BeatsPending||BeatsOn&&ChiefCutInVisible)&&!(BeatsOn&&BeatsLive(ViewSim))&&
            !(BeatsWarning&&!ChiefCutInVisible&&_beatQueue.Count<BeatWarnQueue);
        // While the fight plays, the pieces that are always on stay hidden; a pause shows them all again (fix 3).
        bool BeatsQuiet=>BeatsOn&&!_paused&&!ViewOver;
        bool BeatGuarding(Figure f)=>_beat!=null&&_beat.guard==f;
        bool BeatTitleUp(string title)=>_beatTitle!=null&&_beatTitleText==title&&_visualTime-_beatTitleAt<_beatTitleLife;
        bool _beatLiveSeen;

        void ClearBeats()
        {
            _beatQueue.Clear();_beat=null;_beatAge=0;_beatTitleAt=_beatDimAt=_beatShakeAt=-100;_beatLiveSeen=false;
            _beatHp.Clear();_beatKills.Clear();_beatRuns.Clear();_beatDim.Clear();_beatWords.Clear();_beatMarksShown.Clear();
            _beatTitle=null;_beatTitleText="";_beatTitleNext=null;
        }

        // A parry coming at our side runs in real time from its first cue (PitCueStep) until it lands or is parried.
        static bool BeatsLive(BattleSimulation sim)
        {
            foreach(var t in sim.Threats(0))if(PitCueStep(t)>0)return true;
            return false;
        }
        static bool BeatDot(BattleEvent e)=>e.Kind==BattleEventKind.Hit&&(e.Detail==BurnRules.Detail||e.Detail==BleedRules.Detail);
        static bool BeatImpact(BattleEvent e)=>e.Kind==BattleEventKind.Hit||e.Kind==BattleEventKind.Heal||e.Kind==BattleEventKind.Miss;
        bool BeatBlow(BattleEvent e)
        {
            if(e.ActorId.Length==0||BeatDot(e))return false;
            bool kind=BeatImpact(e)||e.Kind==BattleEventKind.TrapArmed||e.Kind==BattleEventKind.Release||e.Kind==BattleEventKind.Land;
            return kind&&FindFigure(e.ActorId)!=null;
        }

        // Every blow of this drain becomes a beat (one actor's blows on one tick are one beat); every other event plays with
        // the blow it belongs to, or at once when it belongs to none.
        void PresentBeats(IReadOnlyList<BattleEvent> events,int first)
        {
            int n=events.Count-first;if(n<=0)return;
            var owner=new Beat[n];var made=new List<Beat>();Beat last=null;
            for(int i=0;i<n;i++)
            {
                var e=events[first+i];if(!BeatBlow(e))continue;
                if(last!=null&&last.blow.Tick==e.Tick&&last.blow.ActorId==e.ActorId)
                {
                    // A swing's Release and then its damage are one blow: run at whoever the damage lands on. A spell or a
                    // cry (a Release with no target) stays the blow, so its caster never runs at his first victim.
                    if(BeatImpact(e)&&!BeatImpact(last.blow)&&last.blow.TargetId.Length>0){last.blow=e;last.target=FindFigure(e.TargetId);}
                }
                else{last=new Beat{blow=e,actor=FindFigure(e.ActorId),target=FindFigure(e.TargetId)};made.Add(last);}
                owner[i]=last;
            }
            for(int i=0;i<n;i++)
            {
                var e=events[first+i];
                var b=owner[i]??BeatFor(events,first,owner,i);
                if(b==null){AnimateEvent(e);continue;}
                b.effects.Add(e);Pend(e,1);
                if(e.Kind==BattleEventKind.Defence&&e.Detail==LeaderRules.MiddleDetail)
                {b.aim=FindFigure(e.TargetId);b.guard=FindFigure(e.ActorId);}
            }
            // While a parry runs up in real time its fight's other blows wait their turn, so the run up stays clean. The
            // drain that ends it holds the swing and the parry: that plays first, then the blows that waited.
            bool live=BeatsLive(ViewSim);
            if(!live&&_beatLiveSeen)_beatQueue.InsertRange(0,made);else _beatQueue.AddRange(made);
            _beatLiveSeen=live;
            if(_seeking){FlushBeats();return;}
            if(_beat==null&&!live)NextBeat();
        }
        Beat BeatFor(IReadOnlyList<BattleEvent> events,int first,Beat[] owner,int i)
        {
            var e=events[first+i];
            if(BeatDot(e))return null;   // a burn or bleed tick pops on its own, never as somebody's blow
            if(e.Kind==BattleEventKind.Kill)
            {
                // The death plays with the blow that dealt it; after a burn or bleed tick, at once.
                for(int j=i-1;j>=0;j--)
                {
                    var d=events[first+j];
                    if(d.Kind!=BattleEventKind.Hit||d.TargetId!=e.TargetId)continue;
                    return owner[j]??(BeatDot(d)?null:LatestBeat(owner,i));
                }
                return LatestBeat(owner,i);
            }
            if(e.ActorId.Length>0)
            {
                // A Struck, a Mark or a Boost goes with its actor's own blow on this tick: the next one, else the last one.
                for(int j=i+1;j<owner.Length;j++)if(owner[j]!=null&&owner[j].blow.Tick==e.Tick&&owner[j].blow.ActorId==e.ActorId)return owner[j];
                for(int j=i-1;j>=0;j--)if(owner[j]!=null&&owner[j].blow.ActorId==e.ActorId)return owner[j];
                // A Guard or a middle hold goes with the blow that lands on the Warden.
                for(int j=i+1;j<owner.Length;j++)if(owner[j]!=null&&owner[j].blow.Tick==e.Tick&&owner[j].blow.TargetId==e.ActorId)return owner[j];
            }
            if(e.TargetId.Length>0)
            {
                // A Parry's target is the swinger; marks and chains go with the blow on their target.
                for(int j=i+1;j<owner.Length;j++)if(owner[j]!=null&&owner[j].blow.Tick==e.Tick&&owner[j].blow.ActorId==e.TargetId)return owner[j];
                for(int j=i-1;j>=0;j--)if(owner[j]!=null&&(owner[j].blow.TargetId==e.TargetId||owner[j].blow.ActorId==e.TargetId))return owner[j];
            }
            return LatestBeat(owner,i);
        }
        static Beat LatestBeat(Beat[] owner,int i){for(int j=i-1;j>=0;j--)if(owner[j]!=null)return owner[j];return null;}
        void Pend(BattleEvent e,int sign)
        {
            if(e.TargetId.Length==0)return;
            if(e.Kind==BattleEventKind.Hit&&e.Amount>0)BeatAdd(_beatHp,e.TargetId,sign*e.Amount);
            else if(e.Kind==BattleEventKind.Heal&&e.Amount>0)BeatAdd(_beatHp,e.TargetId,-sign*e.Amount);
            else if(e.Kind==BattleEventKind.Kill)BeatAdd(_beatKills,e.TargetId,sign);
        }
        static void BeatAdd(Dictionary<string,int> map,string id,int amount)
        {
            map.TryGetValue(id,out int now);now+=amount;
            if(now==0)map.Remove(id);else map[id]=now;
        }
        // What the bar shows: the health before every blow still waiting its turn on screen.
        int BeatHp(Figure f)
        {
            int hp=f.unit.Hp;
            if(_beatHp.TryGetValue(f.unit.Id,out int pending))hp=Mathf.Clamp(hp+pending,0,f.unit.MaxHp);
            return hp;
        }
        // Dead in the sim, but the blow that kills him has not played yet.
        bool BeatAlive(Figure f)=>!f.unit.Alive&&_beatKills.ContainsKey(f.unit.Id);
        // A blow still waiting its turn names this fighter: a chief's Release or his death the screen has not shown yet.
        bool BeatPendingFor(Figure f)
        {
            if(f==null)return false;
            string id=f.unit.Id;
            if(_beat!=null&&!_beat.landed&&BeatNames(_beat,id))return true;
            foreach(var b in _beatQueue)if(BeatNames(b,id))return true;
            return false;
        }
        static bool BeatNames(Beat b,string id){foreach(var e in b.effects)if(e.ActorId==id||e.TargetId==id)return true;return false;}

        void FlushBeats()
        {
            if(_beat!=null&&!_beat.landed)LandBeat(_beat);
            _beat=null;
            while(_beatQueue.Count>0){var b=_beatQueue[0];_beatQueue.RemoveAt(0);LandBeat(b);}
            _beatRuns.Clear();
        }
        void LandBeat(Beat b)
        {
            b.landed=true;
            foreach(var e in b.effects){Pend(e,-1);AnimateEvent(e);}
            // A shot's follow through already played while it flew: carry on from there, not from the release again.
            var a=b.actor;
            if(a?.pit!=null&&a.pit.confirmed>=0&&b.lead>b.strikeAt)a.pit.confirmed-=b.lead-b.strikeAt;
            if(a?.table!=null&&a.table.current!=null&&b.lead>b.strikeAt)a.table.start-=b.lead-b.strikeAt;
            if(_seeking||!BeatsOn)return;
            OneWordPerBlow();
            bool kill=false;foreach(var e in b.effects)if(e.Kind==BattleEventKind.Kill)kill=true;
            if(kill||b.parry)
            {
                // A kill or our leader's parry: the fighters in it freeze 0.3 s, the fallen flash white 0.1 s, the field
                // shakes 0.3 s.
                _beatShakeAt=_visualTime;
                foreach(var e in b.effects)
                {
                    if(e.Kind!=BattleEventKind.Kill)continue;
                    var dead=FindFigure(e.TargetId);if(dead==null)continue;
                    dead.majorWhiteUntil=_visualTime+.1f;
                    if(dead.table!=null)dead.table.holdUntil=Mathf.Max(dead.table.holdUntil,_tableClock+.3f);
                }
                if(a?.table!=null)a.table.holdUntil=Mathf.Max(a.table.holdUntil,_tableClock+.3f);
            }
            // Our leader's parry that kills the champion: PARRIED first, then CHAMPION FALLS.
            if(b.parry&&b.fallen!=null){ShowBeatTitle("PARRIED",.9f);_beatTitleNext="CHAMPION FALLS";_beatTitleNextAt=_visualTime+.9f;}
            else
            {
                if(b.parry)ShowBeatTitle("PARRIED",1.6f);
                if(b.fallen!=null)ShowBeatTitle("CHAMPION FALLS",float.MaxValue);
            }
        }
        void NextBeat()
        {
            _beat=null;_beatAge=0;
            while(_beatQueue.Count>0)
            {
                var b=_beatQueue[0];_beatQueue.RemoveAt(0);
                if(!ShapeBeat(b)){LandBeat(b);continue;}
                _beat=b;return;
            }
        }
        // dt is the fight's own time (speed applied). A beat lands at its lead, then holds; the next starts after the hold.
        void AdvanceBeats(float dt)
        {
            if(!BeatsPending||ChiefCutInVisible)return;   // the spell's band covers the field: nothing plays behind it
            if(_beat==null){if(BeatsLive(ViewSim))return;NextBeat();}
            while(_beat!=null&&dt>0)
            {
                float until=_beat.landed?_beat.lead+_beat.hold:_beat.lead;
                float used=Mathf.Min(dt,until-_beatAge);
                _beatAge+=used;dt-=used;
                if(_beatAge<until)return;
                if(!_beat.landed){LandBeat(_beat);continue;}
                NextBeat();
            }
        }

        // How long a blow takes on screen (Jacob, 8 Nov 2026, "Build all five": the 7 Nov big moments mock he okayed held
        // small hits about 0.3 s, big ones 0.7 s and kills 1.1 s, and dimmed the field only for the big ones). The swing
        // winds up fast and the time goes to the hit: 0.2 s after contact for a small blow, 0.55 s big, 0.95 s a kill, 2 s
        // for our leader's parry and the champion's fall. While a chief's warning is up the blows are quick (fix 5).
        bool ShapeBeat(Beat b)
        {
            var a=b.actor;var e=b.blow;
            if(a==null||!a.unit.Alive&&!BeatAlive(a))return false;
            bool impact=BeatImpact(e),release=e.Kind==BattleEventKind.Release||e.Kind==BattleEventKind.Land;
            bool kill=false,big=false,spell=false;
            b.lit.Clear();b.fallen=null;
            foreach(var x in b.effects)
            {
                if(x.Kind==BattleEventKind.Kill)
                {
                    kill=true;var dead=FindFigure(x.TargetId);
                    if(dead!=null&&dead.unit.Id.EndsWith(".champion",StringComparison.Ordinal))b.fallen=dead;
                }
                if(x.Kind==BattleEventKind.Struck&&x.Detail==AttackTable.CritDetail)big=true;
                if(x.Kind==BattleEventKind.Release&&string.IsNullOrEmpty(x.TargetId))spell=true;
                if(x.Kind==BattleEventKind.Hit&&x.Amount>0&&!BeatDot(x))
                {
                    var t=FindFigure(x.TargetId);
                    if(ClassSpells.TierOf(x.Detail)!=HitTier.Light||t!=null&&x.Amount>=Mathf.Max(1,t.unit.MaxHp/5))big=true;
                }
                // Lit: whom this blow's own actor touches and whoever it kills. A drain's other events can fall to this beat
                // (a parry's drain carries the whole run up), and they must not light the whole field.
                if(x.ActorId==e.ActorId||x.Kind==BattleEventKind.Kill)
                {var touched=FindFigure(x.TargetId);if(touched!=null&&!b.lit.Contains(touched))b.lit.Add(touched);}
            }
            b.parry=BeatIsParry(b);
            if(b.parry){var leader=FindFigure(Campaign.LeaderId);if(leader!=null&&!b.lit.Contains(leader))b.lit.Add(leader);}
            bool moment=b.parry||b.fallen!=null,quick=BeatsWarning&&!moment;
            b.big=big||kill||spell||release||moment;
            foreach(var f in new[]{a,b.target,b.aim,b.guard})if(f!=null&&!b.lit.Contains(f))b.lit.Add(f);
            SelectStageSegment(a);
            b.poseStart=a.poseTime;
            float windup=0;
            if(a.attack!=null&&!release)
            {
                BeatClip(b,out float start,out float strike);
                windup=Mathf.Min(quick?BeatWarnWind:b.big?BeatWindBig:BeatWindSmall,Mathf.Max(0,strike-start));
            }
            var aim=b.aim??b.target;
            bool melee=impact&&e.Kind!=BattleEventKind.Heal&&aim!=null&&aim!=a&&!a.unit.Stats.Ranged&&
                e.Detail!=BattleSimulation.TrapDetail&&!ViewSim.TryPartOf(a.unit,out _)&&!IsSharkAdd(a.unit);
            float hold=release?(spell?BeatHoldSpell:BeatHoldBig):e.Kind==BattleEventKind.Miss?BeatHoldMiss:big?BeatHoldBig:BeatHoldSmall;
            if(e.Kind==BattleEventKind.TrapArmed)hold=BeatHoldTrap;
            if(kill)hold=Mathf.Max(hold,BeatHoldKill);
            if(quick&&!spell)hold=kill?BeatWarnKill:b.big?BeatWarnBig:BeatWarnHold;
            if(moment)hold=BeatHoldMoment;
            b.hold=hold;
            if(melee)
            {
                var home=BeatNow(a);
                var to=aim.feet;float dx=aim.feet.x-a.feet.x;
                to.x-=Mathf.Sign(dx)*Mathf.Min(Mathf.Abs(dx),BeatGap);
                float dist=Vector2.Distance(home,to);
                float run=dist<=BeatLungeReach?(quick?BeatWarnLunge:BeatLunge):
                    Mathf.Max(quick?BeatWarnRunMin:BeatRunMin,dist/(quick?BeatWarnRunSpeed:BeatRunSpeed));
                b.lead=b.strikeAt=Mathf.Max(run,windup);
                float contact=_visualTime+b.lead;
                _beatRuns[a]=new BeatRun{from=home,to=to,start=contact-run,outFor=run,backAt=contact+(quick?.06f:.12f),backFor=run};
                // A Warden holding the middle steps in front of the ally the blow was meant for (rules 43).
                var g=b.guard;
                if(g!=null&&g!=aim&&g.unit.Alive)
                {
                    var gFrom=BeatNow(g);var gTo=aim.feet;gTo.x+=Mathf.Sign(a.feet.x-aim.feet.x)*BeatGuardStep;
                    float step=Mathf.Clamp(Vector2.Distance(gFrom,gTo)/BeatRunSpeed,.1f,.3f);
                    _beatRuns[g]=new BeatRun{from=gFrom,to=gTo,start=contact-step,outFor=step,backAt=contact+.3f,backFor=step};
                    b.lead=b.strikeAt=Mathf.Max(b.lead,step);
                }
            }
            else if(impact||e.Kind==BattleEventKind.TrapArmed)
            {
                b.shot=a.unit.Stats.Ranged&&e.Kind!=BattleEventKind.Heal&&b.target!=null&&b.target!=a;
                b.strikeAt=windup;b.lead=windup+(b.shot?(quick?BeatShotFlight*.6f:BeatShotFlight):.04f);
            }
            else b.lead=b.strikeAt=.06f;
            BeatOrder();
            return true;
        }
        // The clip a blow plays, and where in it the swing starts and strikes: the attack table's own pick in the review
        // presenter (TableActionClip), else the pit segment that comes next.
        Clip BeatClip(Beat b,out float start,out float strike)
        {
            var f=b.actor;var e=b.blow;start=0;
            if(f.table!=null)
            {
                var c=TableActionClip(f,e.Kind==BattleEventKind.Heal,
                    e.Detail==ClassSpells.StrikeDetail||e.Detail==ClassSpells.StrikeOpenDetail||e.Kind==BattleEventKind.TrapArmed);
                strike=PitFrameTime(c,c.sheet.hitFrame);return c;
            }
            var a=f.attack;
            if(f.pit!=null){var seg=f.pit.segments[f.pit.segment];start=PitFrameTime(a,seg.start);strike=PitFrameTime(a,seg.strike);}
            else strike=PitFrameTime(a,Mathf.Clamp(a.sheet.hitFrame,0,a.sheet.frames.Length-1));
            return a;
        }
        // The actor winds up on the way in and strikes on contact; after contact the pit or table pose plays the follow through.
        bool BeatAttackPose(Figure f,out Clip c,out float time)
        {
            c=null;time=0;var b=_beat;
            if(b==null||b.landed||f!=b.actor||f.attack==null||b.blow.Kind==BattleEventKind.Release||b.blow.Kind==BattleEventKind.Land)return false;
            c=BeatClip(b,out float start,out float strike);
            // The whole windup always plays, from the clip's first frame: a windup longer than the way in plays faster
            // instead of skipping its first frames.
            float elapsed=f.poseTime-b.poseStart,windup=strike-start;
            if(elapsed>=b.strikeAt)time=strike+elapsed-b.strikeAt;
            else if(windup<=b.strikeAt)time=Mathf.Max(start,strike-b.strikeAt+elapsed);
            else time=start+windup*elapsed/Mathf.Max(.0001f,b.strikeAt);
            return time<c.seconds;
        }
        bool BeatShot(Figure f,out Figure target,out float flight)
        {
            target=null;flight=0;var b=_beat;
            if(b==null||!b.shot||b.landed||f!=b.actor||_beatAge<b.strikeAt)return false;
            target=b.target;flight=Mathf.Clamp01((_beatAge-b.strikeAt)/Mathf.Max(.01f,b.lead-b.strikeAt));
            return target!=null;
        }
        Vector2 BeatNow(Figure f)=>_beatRuns.TryGetValue(f,out var r)&&!r.Done(_visualTime)?r.At(_visualTime,f.feet):f.feet;
        bool BeatPosition(Figure f,out Vector2 position)
        {
            position=f.feet;
            if(!_beatRuns.TryGetValue(f,out var r))return false;
            if(r.Done(_visualTime)){_beatRuns.Remove(f);BeatOrder();return false;}
            position=r.At(_visualTime,f.feet);return true;
        }
        // Whoever stands lower on the stage is drawn in front; a runner in front of whom he runs to.
        void BeatOrder()
        {
            if(_figures.Count==0)return;
            int low=int.MaxValue;foreach(var f in _figures)low=Math.Min(low,f.root.GetSiblingIndex());
            var order=new List<Figure>(_figures);
            order.Sort((x,y)=>{int c=BeatNow(x).y.CompareTo(BeatNow(y).y);return c!=0?c:_beatRuns.ContainsKey(x).CompareTo(_beatRuns.ContainsKey(y));});
            for(int i=0;i<order.Count;i++)order[i].root.SetSiblingIndex(low+i);
        }

        // The rules 43 formation (Jacob, 8 Nov 2026: "Warden should always be in the middle, guarding the party"): each band
        // stands round its Warden, as in the champion clip (Art/Studies/Leaders-20261108/stage47.py FORM); the champion leads
        // from the front of his band. A second fighter of one class keeps the old place.
        bool LeadersFormation(BattleUnit u,HashSet<string> taken,ref float x,ref float y)
        {
            if(!ViewSim.LeadersRulesOn||ViewSim.BeastOn||ViewKind==EncounterKind.Duel)return false;
            if(u.Id.EndsWith(".champion",StringComparison.Ordinal)){x=720;y=560;return true;}
            if(u.IsLeader)return false;
            Vector2 spot;
            switch(u.Class)
            {
                case UnitClass.Warden:spot=new Vector2(560,560);break;
                case UnitClass.Skirmisher:spot=new Vector2(650,440);break;
                case UnitClass.Trapper:spot=new Vector2(650,680);break;
                case UnitClass.Marksman:spot=new Vector2(470,450);break;
                case UnitClass.Medic:spot=new Vector2(470,670);break;
                default:return false;
            }
            if(u.Side==1)spot.x=W-spot.x;
            if(!taken.Add(u.Side+"/"+u.Class))return false;
            x=spot.x;y=spot.y;return true;
        }

        // Status words over whoever they land on (Jacob, 8 Nov 2026: "I don't see the status effects text much"), the
        // titles when their leader retreats or their champion falls, and the leader walking off.
        void BeatEvent(BattleEvent e,Figure actor,Figure target)
        {
            if(_seeking||!BeatsOn)return;
            string word=BeatWordFor(e,out bool onActor);
            var on=onActor?actor:target;
            if(word.Length>0&&on!=null)PopBeatWord(on,word);
            if(e.Kind==BattleEventKind.Break&&e.ActorSide==1&&(e.Detail==LeaderRules.RetreatDetail||e.Detail==LeaderRules.ChampionDetail))
            {
                string title=e.Detail==LeaderRules.RetreatDetail?"RETREAT":"CHAMPION FALLS";
                Callout(1,title,PxGold);if(_beatTitleNext!=title)ShowBeatTitle(title,float.MaxValue);
                if(e.Detail==LeaderRules.RetreatDetail)
                    foreach(var f in _figures)if(f.unit.Side==1&&f.unit.IsLeader&&f.unit.Alive)WalkOff(f);
            }
        }
        static string BeatWordFor(BattleEvent e,out bool onActor)
        {
            onActor=false;
            switch(e.Kind)
            {
                case BattleEventKind.Mark:
                    return e.Detail==BurnRules.Mark?"burn":e.Detail==ClassSpells.BleedMark?"bleed":e.Detail==ClassSpells.SnareMark?"snare":
                        e.Detail==ClassSpells.OpeningMark?"opening":"";
                case BattleEventKind.Defence:
                    onActor=true;
                    return e.Detail==LeaderRules.MiddleDetail?"holds":e.Detail==DefenceRules.StepBack?"sidestep":
                        e.Detail==DefenceRules.SnapTrap?"snaptrap":e.Detail==DefenceRules.SelfPatch?"selfpatch":"";
                case BattleEventKind.Snap:
                    onActor=true;
                    return e.Detail==FightRules.Panic?"panic":e.Detail==FightRules.Rise?"rise":"";
                case BattleEventKind.Boost:
                    onActor=e.Detail!=BoostRules.Stim;
                    return e.Detail==BoostRules.Stim?"stim":e.Detail==BoostRules.TwinCut?"twincut":e.Detail==BoostRules.Rearm?"rearm":"";
                case BattleEventKind.Heal:
                    return e.Detail==ClassSpells.TreatDetail?"treat":e.Detail==BeatHierophant?"heal":"";
            }
            return "";
        }
        void PopBeatWord(Figure f,string key)
        {
            var texture=Resources.Load<Texture2D>("CampaignArt/UI/Words43/"+key);
            if(texture==null)throw new InvalidOperationException("Rules 43 status word missing: "+key);
            // A status word shows once when its mark lands; while the mark stays on, its icon under the bar carries it.
            if(BeatMarkKey(key))
            {
                foreach(var m in _beatMarksShown)if(m.f==f&&m.key==key)return;
                _beatMarksShown.Add(new BeatMark{f=f,key=key});
            }
            int slot=0;
            foreach(var w in _beatWords)if(w.f==f&&_visualTime-w.at<.45f)slot=Math.Max(slot,w.slot+1);
            var image=Raw(_readoutLayer,"Status word "+key,0,0,texture.width,texture.height);
            image.texture=texture;image.raycastTarget=false;
            _beatWords.Add(new BeatWord{image=image,at=_visualTime,f=f,slot=slot,key=key});
        }
        // A title over the field: our leader's parry for 1.6 s, the champion's fall and the retreat until the fight ends.
        void ShowBeatTitle(string title,float life)
        {
            if(BeatTitleUp(title))return;
            if(_beatTitle==null){_beatTitle=Raw(_readoutLayer,"Fight title",0,0,1,1);_beatTitle.raycastTarget=false;}
            var old=_beatTitle.texture as Texture2D;if(old!=null&&_screenTextures.Remove(old))Destroy(old);
            var t=_logoFont.Render(title,PxGold,new Color32(160,110,40,255),1,true);_screenTextures.Add(t);
            _beatTitle.texture=t;_beatTitleAt=_visualTime;_beatTitleText=title;_beatTitleLife=life;
        }
        void WalkOff(Figure f)
        {
            var from=BeatNow(f);
            _beatRuns[f]=new BeatRun{from=from,to=new Vector2(W+160,from.y),start=_visualTime+.35f,outFor=1.6f,backAt=float.MaxValue,backFor=0};
            f.root.localScale=new Vector3(-1,1,1);   // he turns his back on the field
        }
        void DrawBeatWords()
        {
            // A mark that wore off may show its word again the next time it lands.
            _beatMarksShown.RemoveAll(m=>m.f==null||!m.f.unit.Alive||!BeatHasMark(m.f.unit,m.key));
            if(_beatTitleNext!=null&&_visualTime>=_beatTitleNextAt){var next=_beatTitleNext;_beatTitleNext=null;ShowBeatTitle(next,float.MaxValue);}
            for(int i=_beatWords.Count-1;i>=0;i--)
            {
                var w=_beatWords[i];float age=_visualTime-w.at;
                if(w.image==null||age>=BeatWordLife){if(w.image!=null)Destroy(w.image.gameObject);_beatWords.RemoveAt(i);continue;}
                var texture=w.image.texture;
                // Pops in a little big, rises a few pixels, then wipes away from the top like the cash words.
                float scale=age<.1f?1.25f-.25f*age/.1f:1;
                float crop=Mathf.Clamp01((BeatWordLife-age)/.3f);
                var at=BeatNow(w.f);
                float y=at.y-w.f.head-16-w.slot*(texture.height+2)-Mathf.Min(1,age/.3f)*8;
                w.image.enabled=!ChiefCutInVisible;
                w.image.rectTransform.localScale=new Vector3(scale,scale,1);
                w.image.uvRect=new UnityEngine.Rect(0,1-crop,1,crop);
                w.image.rectTransform.sizeDelta=new Vector2(texture.width,texture.height*crop);
                w.image.rectTransform.anchoredPosition=new Vector2(Mathf.Round(at.x-texture.width/2f),-Mathf.Round(y+texture.height*(1-crop)));
            }
            if(_beatTitle!=null&&_beatTitle.texture!=null)
            {
                float age=_visualTime-_beatTitleAt;var t=_beatTitle.texture;
                _beatTitle.enabled=age<_beatTitleLife&&!ChiefCutInVisible;
                float scale=2*(age<.15f?1.3f-.3f*age/.15f:1);
                _beatTitle.rectTransform.localScale=Vector3.one;
                _beatTitle.rectTransform.sizeDelta=new Vector2(t.width*scale,t.height*scale);
                _beatTitle.rectTransform.anchoredPosition=new Vector2(Mathf.Round(W/2-t.width*scale/2),-Mathf.Round(230-t.height*scale/2));
            }
        }
        // The spotlight on a coming parry fades in and out over 0.2 s instead of flipping the field in one frame.
        float BeatFade(Figure f,float want)
        {
            if(!_beatDim.TryGetValue(f,out float now))now=1;
            float dt=Mathf.Clamp(_visualTime-_beatDimAt,0,.1f);
            now=Mathf.MoveTowards(now,want,dt*(1-.8f)/.2f);
            _beatDim[f]=now;
            return now;
        }
        void BeatFadeClock(){_beatDimAt=_visualTime;}

        // Our leader's parry: the swing he parried, or a Parry from our side on the swinger's own blow.
        static bool BeatIsParry(Beat b)
        {
            if(b.blow.Kind==BattleEventKind.Release&&b.blow.Detail==FightRules.Parried)return true;
            foreach(var x in b.effects)if(x.Kind==BattleEventKind.Parry&&x.ActorSide==0&&x.TargetId==b.blow.ActorId)return true;
            return false;
        }
        // One word per blow (Jacob, 8 Nov 2026, "Build all five"): every word this blow's events raised just now, from the
        // status words, the pit words, the cash words and the attack table words, competes; the most important one stays.
        void OneWordPerBlow()
        {
            float now=_visualTime;int best=-1;object keep=null;
            void Offer(object word,int rank){if(rank>best){best=rank;keep=word;}}
            foreach(var pop in _cashWords.Values)if(pop.at==now)Offer(pop,WordRank(pop.word));
            foreach(var pop in _tableWords.Values)if(pop.at==now)Offer(pop,WordRank(pop.word));
            foreach(var read in _pitSpellReads.Values)if(read.wordAt==now)Offer(read,WordRank(read.text));
            foreach(var w in _beatWords)if(w.at==now)Offer(w,WordRank(w.key));
            if(keep==null)return;
            foreach(var pop in _cashWords.Values)if(pop.at==now&&pop!=keep)pop.at=-100;
            foreach(var pop in _tableWords.Values)if(pop.at==now&&pop!=keep)pop.at=-100;
            foreach(var read in _pitSpellReads.Values)if(read.wordAt==now&&read!=keep)read.wordAt=-100;
            for(int i=_beatWords.Count-1;i>=0;i--)
            {
                var w=_beatWords[i];if(w.at!=now||w==keep)continue;
                if(w.image!=null)Destroy(w.image.gameObject);
                _beatWords.RemoveAt(i);
            }
            // One word on screen at a time: a word an earlier blow left still showing wipes out within 0.15 s.
            const float wipe=.15f;
            foreach(var w in _beatWords)if(w!=keep&&w.at<now)w.at=Mathf.Min(w.at,now-(BeatWordLife-wipe));
            foreach(var pop in _tableWords.Values)if(pop!=keep&&pop.at<now)pop.at=Mathf.Min(pop.at,now-(.65f-wipe));
            foreach(var pop in _cashWords.Values)if(pop!=keep&&pop.at<now)pop.at=Mathf.Min(pop.at,now-(.75f-wipe));
            foreach(var read in _pitSpellReads.Values)if(read!=keep&&read.wordAt<now)read.wordAt=Mathf.Min(read.wordAt,now-(.65f-wipe));
        }
        // A payoff beats what the blow did, which beats a defence, which beats a new mark, which beats a boost or a heal.
        static int WordRank(string word)
        {
            switch((word??"").ToUpperInvariant())
            {
                case "PINNED":case "OPENED":case "RIPPED":case "EXPOSED":case "CRUSHED":case "FROZEN":return 6;
                case "CRIT":case "PARRY":case "BLOCK":case "DODGE":return 5;
                case "COUNTER":case "AMBUSH":case "STOPPED":case "READY":case "FREE ANSWER":return 4;
                case "HOLDS":case "SIDESTEP":case "SNAPTRAP":case "SELFPATCH":case "GUARDED":return 3;
                case "BURN":case "BLEED":case "SNARE":case "SNARED":case "OPENING":case "OPEN":return 2;
                default:return 1;
            }
        }
        static bool BeatMarkKey(string key)=>key=="burn"||key=="bleed"||key=="snare"||key=="opening";
        static bool BeatHasMark(BattleUnit u,string key)=>
            key=="burn"?u.BurnTicks>0:key=="bleed"?u.BleedTicks>0:key=="snare"?u.SnareTicks>0:u.OpeningTicks>0;
        // Big blows dim everyone outside them to 70%, as the mock did; small ones never do, so the field does not strobe.
        float BeatDimWant(Figure f)
        {
            var b=_beat;
            if(!BeatsOn||b==null||!b.big||_paused)return 1;
            return b.lit.Contains(f)?1:.7f;
        }
        // Fighters outside the blow breathe at half speed while it plays, so the eye goes to the blow (fix 4).
        float BeatPoseRate(Figure f)
        {
            var b=_beat;
            if(!BeatsOn||b==null||_paused||b.lit.Contains(f)||_beatRuns.ContainsKey(f))return 1;
            return f.drawnClip==f.idle?.5f:1;
        }
        // The field shakes 0.3 s on a kill or our leader's parry: up to 4 px in whole pixels, settling.
        Vector2 BeatShakeOffset()
        {
            float age=_visualTime-_beatShakeAt;
            if(!BeatsOn||_paused||age<0||age>=.3f)return Vector2.zero;
            int step=Mathf.FloorToInt(age/.03f);float size=Mathf.Ceil(4*(1-age/.3f));
            return new Vector2(step%2==0?size:-size,step%4<2?Mathf.Round(size*.5f):-Mathf.Round(size*.5f));
        }
    }
}
