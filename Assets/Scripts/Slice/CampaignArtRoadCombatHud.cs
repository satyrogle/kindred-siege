using System;
using System.Collections.Generic;
using System.Text;
using KindredSiege.Sim;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using UnityEngine;
using UnityEngine.UI;

namespace KindredSiege.Slice
{
    public sealed partial class CampaignArtLoop
    {
        const string RoadIssue = "PREVIEW: FALL BACK SPAM IS TOO STRONG / BALANCE TEST FAILS";
        bool RoadHud => (_fight != null || _playback != null) && ViewSim.UnitNerveOn;
        bool RoadSystemsHud => (_fight != null || _playback != null) && ViewSim.RoadCombatOn;
        bool LivesRun => _campaign != null && _campaign.Tuning.RulesCap>=Campaign.RoadCombatRules && (_campaign.Active == null || _campaign.Active.RulesVersion >= Campaign.RoadCombatRules);
        sealed class RoadReadout
        {
            public Image nerve, back;
            public PxText state, defence;
            public readonly Image[] ring = new Image[12];
        }
        sealed class RoadReaction { public int chain; public long tick; public string target; }
        readonly Dictionary<string,RoadReadout> _roadUnits = new Dictionary<string,RoadReadout>();
        readonly Dictionary<string,RoadReaction> _roadReactions = new Dictionary<string,RoadReaction>();
        readonly HashSet<int> _roadPaid = new HashSet<int>();
        readonly Dictionary<string,BattleEvent> _roadDefences = new Dictionary<string,BattleEvent>();
        BattleSimulation _roadSim;
        RectTransform _roadHudScreen;
        int _roadEventCursor;
        long _roadTick=-1, _roadPayoffTick=-1000;
        string _roadPayoff="";
        PxText _roadWarning;

        PxLines RoadSummary(BattleSimulation sim)
        {
            int fled=0,defences=0,cashes=0;string broke="";
            foreach(var e in sim.Events)
            {
                if(e.Kind==BattleEventKind.Snap&&e.Detail==RoadRules.Fled&&e.ActorSide==1)fled++;
                if(e.Kind==BattleEventKind.Defence&&e.ActorSide==0)defences++;
                if(e.Kind==BattleEventKind.Cash&&e.ActorSide==0)cashes++;
                if(e.Kind==BattleEventKind.Break)broke=e.Detail;
            }
            // Every way a road fight ends, not only a wipe (rules 43: their champion falls, their leader retreats).
            var end=sim.Ending;
            string how=end.How==FightEnd.Wipe?"Fight ended: one side has nobody left fighting.":
                end.How==FightEnd.NerveBreak?(end.Side==1?(broke==LeaderRules.ChampionDetail?"Their champion fell: his band breaks.":
                    broke==LeaderRules.RetreatDetail?"Their band is done: their leader retreats.":"Their nerve broke.")
                    :"Your nerve broke."):
                end.How==FightEnd.LeaderFell?(end.Side==0?"Your leader fell.":"Their leader fell."):
                end.How==FightEnd.Escaped?"The beast dug away.":"Time limit reached; result uses the recorded health comparison.";
            return new PxLines().Add(how,PxBone)
                .Add("Still standing: your squad "+sim.StillFighting(0)+", enemy "+sim.StillFighting(1)+".",PxBone)
                .Add("Enemies fled: "+fled+" / Your automatic defences: "+defences,PxMint)
                .Add("Your mark cashes: "+cashes+". Fallen allies return next fight.",PxGold)
                .Add(sim.ClearRoadOn?"Fall Back stops your attacks while dodging.":sim.RoadKitOn?"Known issue: Steady Pierce can still hit during Fall Back.":"Known issue: repeated Fall Back is too strong.",PxOrange);
        }

        void ReadRoadEvents(BattleSimulation sim)
        {
            if(_roadSim!=sim||_roadHudScreen!=_screen||sim.CurrentTick<_roadTick)
            {
                _roadSim=sim;_roadHudScreen=_screen;_roadEventCursor=0;
                _roadUnits.Clear();_roadReactions.Clear();_roadPaid.Clear();_roadDefences.Clear();
                _kitUnits.Clear();_kitBoosts.Clear();_kitFlares.Clear();
                _roadPayoff="";_roadPayoffTick=-1000;
                ResetSquadMoments();
            }
            _roadTick=sim.CurrentTick;
            for(;_roadEventCursor<sim.Events.Count;_roadEventCursor++)
            {
                var e=sim.Events[_roadEventCursor];
                if(sim.SquadScreenOn)ReadSquadMoment(e);
                if(e.Kind==BattleEventKind.Defence)_roadDefences[e.ActorId]=e;
                if(sim.RoadKitOn&&e.Kind==BattleEventKind.Boost)_kitBoosts[e.ActorId]=e;
                if((!sim.ClearRoadOn||e.ActorSide==0)&&sim.RoadKitOn&&e.Kind==BattleEventKind.Cash&&e.Detail==BurnRules.FlareDetail)
                {_kitFlares[e.TargetId]=e.Tick;_roadPayoff=(e.ActorSide==0?"YOUR ":"ENEMY ")+"FLARE / "+NameOf(e.ActorId)+" -> "+NameOf(e.TargetId);_roadPayoffTick=e.Tick;}
                if(e.Kind==BattleEventKind.Chain&&!e.Detail.EndsWith("."+ChainRules.Unready))
                    _roadReactions[e.ActorId]=new RoadReaction{chain=e.Amount,tick=e.Tick,target=e.TargetId};
                // Chain means a reaction STARTED, not that it hit. Stamp only an actual cash.
                if((!sim.ClearRoadOn||e.ActorSide==0)&&e.Kind==BattleEventKind.Cash&&e.Detail!=BurnRules.FlareDetail&&_roadReactions.TryGetValue(e.ActorId,out var r)&&
                    r.target==e.TargetId&&e.Tick-r.tick<=ClassSpells.AimTicks&&_roadPaid.Add(r.chain))
                {
                    _roadPayoff=(e.ActorSide==0?"YOUR ":"ENEMY ")+"COMBO HIT / "+NameOf(e.ActorId)+" -> "+NameOf(e.TargetId);
                    _roadPayoffTick=e.Tick;
                }
                if(e.Kind==BattleEventKind.Broken)_roadReactions.Remove(e.TargetId);
                if(e.Kind==BattleEventKind.Bond&&(!sim.ClearRoadOn||e.ActorSide==0))
                {_roadPayoff="BOND FORMED / "+NameOf(e.ActorId)+" + "+NameOf(e.TargetId);_roadPayoffTick=e.Tick;}
            }
        }

        void DrawRoadThreat(BattleSimulation sim)
        {
            if(sim.RoadKitOn){DrawKitThreat(sim);return;}
            _beatTrack.enabled=_beatWindow.enabled=_beatCursor.enabled=false;
            var t=CurrentThreat;
            SetPx(_threatHeading,Fit(t.IsNone?"YOUR SQUAD FIGHTS AND COMBOS AUTOMATICALLY":
                (t.Duel?"BIG MOVE: ":"INCOMING: ")+t.Name+" / "+FighterLabel(t.TargetId),566),t.Duel?PxGold:PxBone);
            SetPx(_threatTarget,Fit(_paused?"PAUSED / SPACE TO RESUME":t.IsNone?
                "FOCUS A THREAT / UNLEASH AN OPENING / HOLD TO GUARD":ThreatAnswers(sim,t),566),PxMuted);
            SetPx(_threatAnswers,sim.CurrentTick-_roadPayoffTick<2*SimClock.TicksPerSecond?Fit(_roadPayoff,566):"",PxGold);
        }

        void DrawRoadCounts(BattleSimulation sim)
        {
            for(int side=0;side<2;side++)
            {
                _nerveBack[side].enabled=_nerveFill[side].enabled=_nerveLag[side].enabled=false;
                SetPx(_nerveLabel[side],side==0?"YOUR SQUAD: "+sim.StillFighting(0)+
                    (_playback==null?" / LIVES "+_campaign.Lives+"/"+Campaign.RunLives: " / REPLAY"):
                    "ENEMIES LEFT: "+sim.StillFighting(1),side==0?PxMint:PxBone);
                SetPx(_nerveChange[side],sim.ClearRoadOn?"":side==0&&sim.RoadKitOn&&_playback==null?"WIN STREAK "+_campaign.Streak+"/"+Campaign.StreakCap:side==0?"HP AND NERVE ARE ON EACH FIGHTER":"WIN WHEN EVERY ENEMY IS DOWN OR GONE",PxMuted);
            }
        }

        void DrawRoadUnit(Figure f)
        {
            if(!RoadSystemsHud)return;
            var u=f.unit;
            if(!_roadUnits.TryGetValue(u.Id,out var r))
            {
                r=new RoadReadout();_roadUnits.Add(u.Id,r);
                r.back=Panel(f.readoutRoot,-f.barWidth*Px/2,7*Px,f.barWidth*Px,2*Px,BarBack);
                r.nerve=Panel(f.readoutRoot,-f.barWidth*Px/2,7*Px,f.barWidth*Px,2*Px,PxGold);
                r.state=PxLabel(f.readoutRoot,0,-Mathf.RoundToInt(f.head/Px)-12,1);
                r.defence=PxLabel(f.readoutRoot,Mathf.RoundToInt(-f.barWidth/2)-12,0,2);
                for(int i=0;i<r.ring.Length;i++)
                {
                    float angle=(i*30-90)*Mathf.Deg2Rad;
                    r.ring[i]=Panel(f.readoutRoot,Mathf.Round(Mathf.Cos(angle)*13)*Px,
                        (-f.head/Px-28+Mathf.Round(Mathf.Sin(angle)*13))*Px,3*Px,3*Px,PxOrange);
                }
            }
            r.back.enabled=r.nerve.enabled=RoadHud&&u.Alive;
            r.nerve.rectTransform.sizeDelta=new Vector2(Mathf.Round(f.barWidth*Mathf.Clamp01(u.UnitNerve/(float)RoadRules.UnitNerve))*Px,2*Px);
            r.nerve.color=u.UnitNerve<=RoadRules.UnitNerve/4?PxOrange:PxGold;
            bool frenzy=RoadHud&&u.Alive&&u.Frenzied;
            for(int i=0;i<r.ring.Length;i++)r.ring[i].enabled=frenzy&&u.RiseTicks>i*RoadRules.FrenzyTicks/r.ring.Length;
            string state=frenzy?(ViewSim.ClearRoadOn?"":"FRENZY ")+Seconds(u.RiseTicks):RoadHud&&u.Alive&&u.PanicTicks>0?(ViewSim.ClearRoadOn?"":"FROZEN ")+Seconds(u.PanicTicks):"";
            SetPx(r.state,state,frenzy?PxOrange:PxBlue);
            SetPx(r.defence,!ViewSim.ClearRoadOn&&u.Alive&&_roadDefences.TryGetValue(u.Id,out var e)&&ViewSim.CurrentTick-e.Tick<SimClock.TicksPerSecond?
                DefenceWord(e.Detail):"",PxBlue);
            if(ViewSim.RoadKitOn)DrawKitUnit(f);
        }

        static string DefenceWord(string move)
        {
            switch(move)
            {
                case DefenceRules.Brace:return "BRACE";
                case DefenceRules.Sidestep:return "SIDESTEP";
                case DefenceRules.StepBack:return "STEP BACK";
                case DefenceRules.SnapTrap:return "SNAP TRAP";
                case DefenceRules.SelfPatch:return "SELF PATCH";
                default:return move.ToUpperInvariant();
            }
        }

        void RoadDetails(StringBuilder text,BattleSimulation sim)
        {
            if(!sim.RoadKitOn)text.Append(RoadIssue).Append("\nKnown road regression: repeated Fall Back won 23/40 in the handback test (ceiling 20%). Not tuned.\n\n");
            else if(sim.PitOn)text.Append(TightGuide).Append("\n\n");
            else if(sim.DangerOn)text.Append("SQUAD COMBAT\n")
                .Append("Gold closing ring: a fresh live parry is available. The line identifies the threatened ally. Repeated Hold presses restart its recovery.\n")
                .Append("Swings can be guarded. Blasts need their displayed parry or dodge answer; an early Hold does not soften a blast.\n")
                .Append("Bloodridge bands carry chief moves from the first road fight.\n")
                .Append("Blue shield: the squad protects its leader while an active ally stands.\n")
                .Append("Purple trail: mark setter. Mint trail: the fighter cashing it. Brackets and the move name identify the confirmed combo.\n")
                .Append("Focus chooses your target. Its DMG total is damage received during the order, not extra bonus damage. Space pauses; a paused Hold cannot parry.\n\n");
            else text.Append(sim.SquadScreenOn?SquadGuide:sim.ClearRoadOn?ClearGuide:KitGuide).Append("\n\n");
            if(sim.UnitNerveOn)text.Append("ROAD COMBAT: each fighter has personal nerve. Leaders falling do not end this fight.\n")
                .Append(sim.ClearRoadOn?"Your melee Frenzy: below 25% HP with nerve exhausted. Enemies: below 50%. +50% hits, no healing, collapse after 30s.\n":"Frenzy: melee below half health, +50% hits, no healing, collapse after 30s. Frozen allies remain alive.\n")
                .Append("Hold is free. Your Hold and Unleash do not drain your nerve here.\n");
            text.Append("Chains react only when set up; an unready setup is not queued. Bonds need three pair cashes.\n");
            foreach(var pair in sim.Setup.Bonds)text.Append("STARTING BOND: ").Append(NameOf(pair.OriginId)).Append(" -> ").Append(NameOf(pair.CasherId)).Append("\n");
            text.Append("\n");
            int shown=0;
            for(int i=sim.Events.Count-1;i>=0&&shown<8;i--)
            {
                var e=sim.Events[i];
                if(e.Kind!=BattleEventKind.Chain&&e.Kind!=BattleEventKind.Bond&&e.Kind!=BattleEventKind.Defence&&e.Kind!=BattleEventKind.Boost&&e.Kind!=BattleEventKind.Cash)continue;
                shown++;text.Append(Seconds(e.Tick)).Append(" / ").Append(e.ActorSide==0?"YOUR ":"ENEMY ");
                if(e.Kind==BattleEventKind.Chain)text.Append(e.Detail.EndsWith("."+ChainRules.Unready)?
                    "SETUP UNREADY: no eligible finisher (sim does not report which eligibility check failed)":
                    "REACTION STARTED: "+NameOf(e.ActorId)+" -> "+NameOf(e.TargetId));
                else text.Append(e.Kind==BattleEventKind.Bond?"BOND: "+NameOf(e.ActorId)+" + "+NameOf(e.TargetId):BoostWord(e.Detail)+": "+NameOf(e.ActorId));
                text.Append(" / ").Append(e.Detail).Append("\n");
            }
            text.Append("\n");
        }
    }
}
