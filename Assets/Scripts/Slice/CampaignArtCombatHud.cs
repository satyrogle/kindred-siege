using System;
using KindredSiege.Sim;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using UnityEngine;

namespace KindredSiege.Slice
{
    public sealed partial class CampaignArtLoop
    {
        // Scoped to current live Bloodridge. Historical playback and other regions retain their UI.
        bool CleanCombatHud => RoadHud || RaidHud || (_playback == null && _fight != null && ViewTribe == CampaignTribes.BloodridgeId &&
            ViewKind != EncounterKind.Tutorial && !HunterFight(ViewKind) && !ViewSim.BeastOn && ViewSim.ParryCenterOn);
        readonly PxText[] _nerveChange = new PxText[2];
        UnityEngine.UI.Image _cleanThreatBack;
        readonly long[] _cleanNerveCauseTick = {-1,-1};
        int NerveY => CleanCombatHud ? 55 : NerveBarY;
        int NerveHeight => CleanCombatHud ? 12 : NerveBarH;
        bool ShiftingBeat(BattleSimulation sim) => CurrentThreat.FakeFirst || CurrentThreat.WaitsOutHold;

        string CombatOrderHint(Directive order)
        {
            return CombatOrderBaseHint(order)+PalFloorOrderHint(order);
        }

        string CombatOrderBaseHint(Directive order)
        {
            if(order==Directive.Eclipse&&(_fight!=null||_playback!=null)&&ViewSim.MeterOn)
                return (_fight!=null&&(_fight.Assist==null||_fight.AssistUsed)?ChiefOrderHint()+" / ":"")+MeterHint()+
                    (ViewSim.CanUse(0,order,out string meterWhy)?"":" / "+meterWhy);
            if(order==Directive.Hold&&(_fight!=null||_playback!=null)&&ViewSim.TightParryOn)
                return "Watch the attacker flash gold: live Hold in 0.2s parries, counters and fills a bar. Mistime: full hit and +50% incoming damage for1s. Paused Hold cannot parry.";
            if(order==Directive.Hold&&(_fight!=null||_playback!=null)&&ViewSim.DangerOn)
                return "SWINGS: Hold guards the front. BLASTS: time a live parry"+(_fight!=null&&_fight.Encounter.NoFallBack?"":" or dodge")+"; an early or paused Hold stops nothing.";
            if(RoadHud&&order==Directive.Unleash)return "Unleash boosts damage and scares enemies. No own nerve drain in road combat.";
            if((_fight!=null||_playback!=null)&&ViewSim.RaidChiefOn&&order==Directive.Hold)
                return "Hold is free. Time one press; mashing within 1 second does not refresh parry timing.";
            if(CleanCombatHud)
            {
                if(order==Directive.Eclipse&&_fight!=null&&(_fight.Assist==null||_fight.AssistUsed))return ChiefOrderHint();
                if(!ViewSim.CanUse(0,order,out string why))return OrderName(order)+": "+why;
                if(order==Directive.FallBack&&_fight!=null&&_fight.Encounter.NoFallBack)return "Fall Back is unavailable in this fight.";
            }
            if(KitHud&&order==Directive.FallBack)return "Dodge big hits. Your whole squad stops attacking, including ranged fighters.";
            return order==Directive.Eclipse?ChiefOrderHint():OrderName(order)+": "+Effect(order);
        }

        string FighterLabel(string id)
        {
            var u=ViewSim.FindById(id);
            if(u==null)return "AREA";
            if(ViewKind==EncounterKind.Duel&&u.Side==1)return "ENEMY CHAMPION";
            return (u.Side==0?"YOUR ":"ENEMY ")+(u.IsLeader?u.Id.EndsWith(".chief")?"CHIEF":"LEADER":RoleName(u,ViewKind));
        }

        void DrawCleanThreat(BattleSimulation sim)
        {
            if(_clarityScreen!=_screen)
            {
                _clarityScreen=_screen;
                _threatPanel=Rect(_readoutLayer,"Priority threat",0,0,W,H);
                _cleanThreatBack=Panel(_threatPanel,22*Px,82*Px,676*Px,58*Px,new Color(.06f,.05f,.07f,.94f));
                _threatHeading=PxLabel(_threatPanel,30,88);
                _threatTarget=PxLabel(_threatPanel,30,102);
                _threatAnswers=PxLabel(_threatPanel,30,128);
                // Retain receipt data for details/validation without another live text strip.
                _threatOthers=PxLabel(_threatPanel,30,128);
                _orderReceipt=PxLabel(_threatPanel,30,128);
                _threatOthers.root.gameObject.SetActive(false);
                _orderReceipt.root.gameObject.SetActive(false);
                PxButton("fight-read","DETAILS",610,85,80,24,ShowClarityDetails,_threatPanel);
                if(sim.Kit(1)==ChiefKit.Bloodridge)
                    PxButton("chief-moves","MOVES",610,112,80,24,ShowChiefMoves,_threatPanel);
                _beatTrack=Raw(_threatPanel,"Time to scheduled beat",30*Px,116*Px,566*Px,7*Px);
                _beatWindow=Raw(_threatPanel,"Timing window",30*Px,116*Px,141*Px,7*Px);
                _beatCursor=Raw(_threatPanel,"Beat cursor",30*Px,114*Px,3*Px,11*Px);
                _beatTrack.color=new Color(.22f,.20f,.24f,1);
                _beatCursor.color=PxBone;
            }
            _threatPanel.gameObject.SetActive(!ViewOver&&!sim.AwaitingVerdict&&!ChiefCutInVisible);
            _cleanThreatBack.enabled=_paused||!CurrentThreat.IsNone;
            if(_tacticsPanel!=null)_tacticsPanel.gameObject.SetActive(false);
            foreach(var mark in _tacticsMarks.Values)mark.root.gameObject.SetActive(false);
            // These cast labels duplicate the single ranked threat. Own casts remain on their order button.
            for(int s=0;s<2;s++)
            {
                if(_spellTimers[s]!=null)_spellTimers[s].root.gameObject.SetActive(false);
                if(_spellFills[s]!=null)_spellFills[s].enabled=false;
            }
            if(sim.SquadScreenOn){DrawKitThreat(sim);CompactThreat(sim);return;}
            if(RoadHud){DrawRoadThreat(sim);CompactThreat(sim);return;}
            if(RaidHud){DrawRaidThreat(sim);CompactThreat(sim);return;}
            var t=CurrentThreat;bool shifts=t.FakeFirst||t.WaitsOutHold;
            bool timing=!t.IsNone&&t.BeatTicks>=0;
            _beatTrack.enabled=_beatWindow.enabled=_beatCursor.enabled=timing;
            _beatWindow.color=shifts?PxMuted:PxGold;
            if(timing)
            {
                var pos=_beatCursor.rectTransform.anchoredPosition;
                pos.x=(30+Mathf.Clamp01(Math.Max(0,t.RealBeatTicks-1)/(2f*SimClock.TicksPerSecond))*563)*Px;
                _beatCursor.rectTransform.anchoredPosition=pos;
            }
            string when=!timing?"":_paused?" / "+Seconds(t.RealBeatTicks)+" FIGHT TIME":
                (t.FakeFirst?" / REAL IN ":t.WaitsOutHold?" / BEAT IN ":" / IMPACT IN ")+BeatTime(t.RealBeatTicks);
            string extra=_threatReads.Count>1?" / +"+(_threatReads.Count-1)+" IN DETAILS":"";
            SetPx(_threatHeading,Fit(t.IsNone?"NO BIG ATTACK INCOMING":(t.BeatTicks<0?"ACTIVE ":t.Duel?"BIG ":"SMALL ")+t.Name+when+extra,566),PxBone);
            SetPx(_threatTarget,Fit(t.IsNone?"Your squad fights automatically.":FighterLabel(t.ActorId)+" TO "+FighterLabel(t.TargetId),566),PxBone);
            string cue=BeatCue(t);
            if(_paused)cue="PAUSED / SPACE TO RESUME";
            else if(timing&&sim.HoldRefreshTicksLeft(0)>0)cue=HoldRecoveryCue(sim);
            else if(!shifts&&timing&&t.ParryWindow&&!sim.CanUse(0,Directive.Hold,out _))cue="PARRY WINDOW / HOLD UNAVAILABLE";
            SetPx(_threatAnswers,Fit(cue,566),_paused?PxMuted:shifts?PxOrange:LiveParryCue(sim,t)?PxGold:PxMuted);
            var last=_orderReads.Count==0?OrderResult.None:_orderReads[_orderReads.Count-1];
            SetPx(_orderReceipt,last.IsNone?"":Receipt(last),PxBone);
            CompactThreat(sim);
        }

        void CleanNerveLabel(BattleSimulation sim,int side,bool swing)
        {
            int value=sim.Nerve(side);
            if(RaidHud&&side==1)
            {
                var chief=sim.Leader(1);
                string phase=chief!=null&&chief.HpPermille<=RaidRules.DesperationPermille?" / DESPERATE":"";
                SetPx(_nerveLabel[side],"CHIEF POSTURE "+Percent(value)+(sim.AwaitingVerdict?" / KNEELING":phase),PxGold);
                _nerveFill[side].color=PxGold;
                SetPx(_nerveChange[side],chief==null?"":"HP "+Percent(chief.HpPermille),PxBone);
                return;
            }
            bool danger=value>0&&value<=NerveRules.Full/4;
            string state=value<=0?" / BROKEN":danger?" / NEAR BREAK":"";
            var colour=danger?PxOrange:side==0?PxMint:PxRed;
            SetPx(_nerveLabel[side],(side==0?"YOUR NERVE ":"ENEMY NERVE ")+Percent(value)+state,colour);
            _nerveFill[side].color=colour;
            SetPx(_nerveChange[side],_paused&&swing?Fit(_swingText[side],300):"",swing?PxOrange:PxMuted);
        }

        void CleanCombatFeedback(BattleSimulation sim,out string title,out string detail,out Color32 colour)
        {
            ReadClarity(sim);colour=PxBone;
            title="CHOOSE YOUR MOMENT";
            detail=_paused?(sim.RaidChiefOn?"Space resumes / Hold is free; paused Hold cannot parry.":"Space resumes / orders still cost points while paused."):"1-5 orders / Space pauses / Details explains threats and outcomes.";
            if(_targeting)
            {
                title="FOCUS FIRE / CHOOSE AN ENEMY";
                detail="Click a fighter / arrows select / Enter confirms / Escape cancels.";
                colour=PxGold;return;
            }
            if(Time.unscaledTime<_holdRejectedUntil)
            {title=OrderName(_rejectedOrder)+" NOT SENT: "+_holdRejected;detail="No points spent. Details shows previous outcomes.";colour=PxRed;return;}
            if(_notice.Length>0){title=_notice;detail="Details shows threats and recent orders.";return;}
            var last=_orderReads.Count==0?OrderResult.None:_orderReads[_orderReads.Count-1];
            if(!last.IsNone)
            {
                if(last.Order==Directive.Hold)
                {
                    HoldFeedback(sim,out title,out detail,out colour);

                }
                else
                {
                    title=Receipt(last);
                    detail=last.TargetId.Length>0?"TARGET: "+FighterLabel(last.TargetId):"Details shows the full order history.";
                    colour=last.Helped?PxMint:last.Final?PxOrange:PxBone;
                }
            }
            if(_hover.Length>0)detail=_hover;
            else if(_paused&&last.IsNone)detail="PAUSED / Space resumes / Details explains the outcome.";
            if(sim.SquadScreenOn&&last.IsNone&&_hover.Length==0)
            { title=_paused?"PAUSED":""; detail=_paused?"SPACE TO RESUME":""; }
            CompactCombatFeedback(sim,ref title,ref detail);
        }

        void CleanThreatMarkers(BattleSimulation sim)
        {
            ReadClarity(sim);var t=CurrentThreat;
            bool moment=!ViewOver&&_visualTime<_heavyMomentUntil;
            _heavyParryWord.enabled=moment&&_heavyMomentText=="PARRIED!";
            _heavyRiposteWord.enabled=moment&&_heavyMomentText=="RIPOSTE!";
            bool visible=!ViewOver&&!sim.AwaitingVerdict&&!ChiefCutInVisible&&!t.IsNone;
            var actor=visible?FindFigure(t.ActorId):null;
            _castBack.enabled=_castFill.enabled=_castMark.enabled=false;
            if(actor!=null&&actor.unit.Alive)
            {
                int x=Mathf.RoundToInt(actor.feet.x/Px),y=Mathf.Max(143,Mathf.RoundToInt((actor.feet.y-actor.head)/Px)-12);
                if(_castLabel.x!=x||_castLabel.y!=y){_castLabel.x=x;_castLabel.y=y;_castLabel.key="";}
                Place(_castBack,x-36,y-2,72,12);_castBack.enabled=!sim.ClearRoadOn;
                SetPx(_castLabel,sim.ClearRoadOn?"":"ATTACKING",PxGold);
                if(_leaderMarks.TryGetValue(actor.unit.Id,out var leaderMark))leaderMark.root.gameObject.SetActive(false);
            }
            else SetPx(_castLabel,"",PxGold);
            var target=visible?FindFigure(t.TargetId):null;
            _heavyTargetBack.enabled=target!=null&&target.unit.Alive;
            if(_heavyTargetBack.enabled)
            {
                int x=Mathf.RoundToInt(target.feet.x/Px),y=Mathf.Clamp(Mathf.RoundToInt(target.feet.y/Px)+38,143,365);
                Place(_heavyTargetBack,x-28,y-2,56,12);
                if(_heavyTargetLabel.x!=x||_heavyTargetLabel.y!=y){_heavyTargetLabel.x=x;_heavyTargetLabel.y=y;_heavyTargetLabel.key="";}
                SetPx(_heavyTargetLabel,sim.ClearRoadOn?"":"INCOMING",PxRed);
            }
            else SetPx(_heavyTargetLabel,"",PxRed);
            if(sim.ClearRoadOn)_heavyTargetBack.enabled=false;
            // Existing impact words keep their art, below the compact threat panel.
            foreach(var word in new[]{_heavyParryWord,_heavyRiposteWord})
            {var p=word.rectTransform.anchoredPosition;p.y=-143*Px;word.rectTransform.anchoredPosition=p;}
        }
    }
}
