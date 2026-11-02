using KindredSiege.Sim;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using UnityEngine;

namespace KindredSiege.Slice
{
    public sealed partial class CampaignArtLoop
    {
        PxLines CompactFavourLines(Favour favour)
        {
            int rank=_campaign.FavourRank(favour)+1;
            var lines=new PxLines().Add(FavourRules.Name(favour),PxGold).Add("RANK "+rank+" / RUN ONLY",PxMuted);
            if(FavourRules.IsCombo(favour))
            {
                var bend=FavourRules.ComboBend(favour);
                var recipe=ClassSpells.Describe(bend).Split(new[]{" | "},System.StringSplitOptions.None);
                lines.Add(SliceClasses.Name(ClassSpells.ClassOf(bend))+" + "+ComboAugmentName(bend),ComboColour(bend))
                    .Add(recipe[0],PxBone)
                    .Add(rank+" MARK / "+MarkRules.MaxStacks+" = BURST",PxMint)
                    .Add((recipe.Length>1?recipe[1]:"PACT: "+ComboPactName(bend))+" / 1 GEAR SLOT",PxOrange);
                var piece=_campaign.FindGear(Campaign.ComboGearId(bend));
                if(piece!=null)lines.Add(ComboPlacement(bend),PxMuted);
                else lines.Add("AUTO-EQUIP / ELSE RUN GEAR",PxMuted);
            }
            else lines.Add(FavourRules.Describe(favour,rank),PxBone);
            return lines;
        }

        void BuildCompactResult()
        {
            var r=_result;
            PxHeader(ResultTitle(r),r.Won?PxBone:PxRed,r.Won?PxGold:PxDeepRed,
                new PxLines().Add(r.Encounter.Title,PxMuted));
            PxButton("camp","PALS",628,6,76,26,ShowCamp);
            ResultScore(16,76,"FALLEN",ResultDeaths(r).ToString(),ResultDeaths(r)>0?PxRed:PxBone);
            ResultScore(248,76,"RECRUITED",r.JoinedFighters.Count.ToString(),PxMint);
            ResultScore(480,76,FavourExchanged(r)||TakenSpoils(r)!=null?"EXCHANGED":"MATERIALS",r.MaterialsEarned.ToString(),PxGold);
            var lines=new PxLines().Add("LIVES "+_resultLives+"/"+Campaign.RunLives+" / STREAK "+_campaign.Streak,PxBone);
            if(r.AssistUnlocked!=null)lines.Add("UNLOCKED: "+r.AssistUnlocked.Name,PxGold);
            if(r.Captured!=null)lines.Add("CAPTURED: "+FighterName(r.Captured),PxRed);
            if(r.Slain!=null)lines.Add("SLAIN: "+FighterName(r.Slain),PxRed);
            if(r.RecipeLearned)lines.Add("RECIPE: "+CampaignForge.TabardName,PxGold);
            if(r.RunOver)lines.Add(r.RunWon?"RUN WON":"RUN ENDED",r.RunWon?PxMint:PxRed);
            bool offered=PendingSpoils(r),favour=_campaign.FavourOffer!=null&&r.FavourOffered!=null;
            lines.Gap().Add(favour?"PAL REWARD READY":offered?"GEAR READY":"NEXT FIGHT",PxGold);
            PxPanel(96,160,528,182,lines);
            _resultsShown++;
            if(offered&&favour)PxButton("choose-reward","CHOOSE PAL / GEAR",224,352,272,30,ShowRewardChoices);
            else if(favour)PxButton("choose-favour","CHOOSE PAL PICK",224,352,272,30,ShowFavours);
            else if(offered)PxButton("take-spoils","CHOOSE GEAR",224,352,272,30,ShowSpoilsChoices);
            PxButton("after-result",offered||favour?"BANK MATERIALS":r.RunOver?"RETURN TO CAMP":"CONTINUE",264,398,192,28,AfterResult);
            PxButton("result-tactics","DETAILS",16,398,140,28,()=>
            {
                Modal("RESULT DETAILS",string.Join("\n",ResultFighterLines(r,false).texts)+"\n\n"+string.Join("\n",FightLines(r,false).texts));
            });
            PxButton("why","WHY",612,398,92,28,()=>ShowFightWhy(r));
            _pxNotice=PxLabel(_screen,16,434);
        }

        // Presentation only: the existing ranked threat, timing predicate and order results remain authoritative.
        void CompactThreat(BattleSimulation sim)
        {
            if(_paused)return;
            var t=CurrentThreat;
            bool now=!sim.TightParryOn&&LiveParryCue(sim,t);
            string move=t.IsNone?"":t.Name.ToUpperInvariant();
            SetPx(_threatHeading,Fit(move+(t.IsNone||t.BeatTicks<0?"":" / "+BeatTime(t.RealBeatTicks)),566),now?PxGold:PxBone);
            SetPx(_threatTarget,t.IsNone?"":Fit(FighterLabel(t.ActorId)+" TO "+FighterLabel(t.TargetId),566),PxMuted);
            string answer="";
            if(!t.IsNone)
            {
                if(sim.FallBackTicksLeft(0)>0)answer="DODGE "+Seconds(sim.FallBackTicksLeft(0))+" / ATTACKS PAUSED";
                else
                {
                    if((t.Answers&ThreatAnswer.Parry)!=0)
                        answer=sim.TightParryOn?"1 PARRY / WATCH ATTACKER":now?"1 PARRY":sim.HoldRefreshTicksLeft(0)>0?"1 / "+HoldRecoveryTime(sim.HoldRefreshTicksLeft(0)):"1 / WAIT";
                    else if((t.Answers&ThreatAnswer.Hold)!=0)answer="1 GUARD";
                    if((t.Answers&ThreatAnswer.FallBack)!=0&&(_fight==null||!_fight.Encounter.NoFallBack))answer+=(answer.Length>0?" / ":"")+"2 DODGE";
                    if((t.Answers&ThreatAnswer.FocusFire)!=0)answer+=(answer.Length>0?" / ":"")+"3 FOCUS";
                    if((t.Answers&ThreatAnswer.Unleash)!=0)answer+=(answer.Length>0?" / ":"")+"4 UNLEASH";
                }
            }
            SetPx(_threatAnswers,answer,now?PxGold:PxBone);
            if(_roadWarning!=null)SetPx(_roadWarning,"",PxMuted);
        }

        static string CompactReceipt(OrderResult r)
        {
            if(r.IsNone)return "";
            string name=Directives.Name(r.Order).ToUpperInvariant();int damage=0;bool blocked=false;
            string outcome="";
            foreach(var effect in r.Effects)
            {
                if(effect.Kind==OrderEffectKind.Damage){damage+=effect.Amount;continue;}
                if(effect.Kind==OrderEffectKind.ScareBlocked){blocked=true;continue;}
                outcome=effect.Kind==OrderEffectKind.Parried?"PARRY":effect.Kind==OrderEffectKind.Soaked?"SOAK":
                    effect.Kind==OrderEffectKind.OutOfReach?"OUT OF REACH":effect.Kind==OrderEffectKind.WaitedOut?"DELAYED":
                    effect.Kind==OrderEffectKind.BroughtDown?"DOWN":effect.Kind==OrderEffectKind.Broke?"BREAK":
                    effect.Kind==OrderEffectKind.Dodged?"DODGE":effect.Kind.ToString().ToUpperInvariant();
            }
            if(r.Order==Directive.Unleash&&blocked)
                return name+" +"+(BattleSimulation.UnleashDamagePercent-100)+"% DMG / SCARE BLOCKED"+(damage>0?" / "+damage:"");
            if(outcome.Length==0&&r.Final&&r.Miss!=OrderMiss.None)
            {
                switch(r.Miss)
                {
                    case OrderMiss.NothingCame:outcome="NO HIT";break;
                    case OrderMiss.WrongOrder:outcome="FULL HIT";break;
                    case OrderMiss.TooEarly:outcome="EARLY";break;
                    case OrderMiss.Covered:outcome="GUARDED";break;
                    case OrderMiss.OutOfReach:outcome="OUT OF REACH";break;
                    case OrderMiss.Blocked:outcome="SCARE BLOCKED";break;
                    case OrderMiss.TooLate:outcome="LATE";break;
                    case OrderMiss.MetFirst:outcome="COVERED";break;
                    case OrderMiss.Feinted:outcome="FAKE";break;
                    case OrderMiss.WaitedOut:outcome="DELAYED";break;
                }
            }
            return name+(outcome.Length>0?" / "+outcome:"")+(damage>0?" / "+damage:"");
        }

        void CompactCombatFeedback(BattleSimulation sim,ref string title,ref string detail)
        {
            if(_paused||_hover.Length>0)return;
            detail="";
            if(_targeting){title="3 FOCUS / SELECT TARGET";return;}
            if(_notice.Length>0||Time.unscaledTime<_holdRejectedUntil)return; // Rejection must retain its actual reason.
            var last=_orderReads.Count==0?OrderResult.None:_orderReads[_orderReads.Count-1];
            if(!last.IsNone&&last.Order==Directive.FocusFire&&last.Running){title="";return;}
            long at=last.Tick;foreach(var effect in last.Effects)at=System.Math.Max(at,effect.Tick);
            title=last.IsNone||sim.CurrentTick-at>3*SimClock.TicksPerSecond?"":CompactReceipt(last);
        }
    }
}
