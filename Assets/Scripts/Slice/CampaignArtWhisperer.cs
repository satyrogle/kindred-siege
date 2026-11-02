using System;
using KindredSiege.Sim;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;

namespace KindredSiege.Slice
{
    public sealed partial class CampaignArtLoop
    {
        bool WhispererRun => _campaign!=null&&Math.Min(Campaign.RulesVersion,_campaign.Tuning.RulesCap)>=Campaign.WhispererRules;
        CampaignFighter WhisperFighter(string id)
        {
            foreach(var fighter in _campaign.Reserve)if(fighter.Id==id)return fighter;
            return null;
        }

        WhisperPick WhisperChoice(int choice)
        {
            var offer=_campaign.FavourOffer;
            return offer!=null&&choice>=0&&choice<offer.Picks.Count?offer.Picks[choice]:null;
        }

        string WhisperTitle(WhisperPick pick)
        {
            if(pick.Kind==Favour.WhisperPair)return FavourRules.PairName(pick.Pair);
            if(pick.Kind==Favour.WhisperHabitUp)return FavourRules.HabitName(pick.Habit);
            var fighter=WhisperFighter(pick.FighterId);
            return fighter==null?pick.FighterId:fighter.Name;
        }

        string WhisperEffect(WhisperPick pick)
        {
            if(pick.Kind==Favour.WhisperHabitUp)return HabitEffect(pick.Habit,true);
            if(pick.Kind==Favour.WhisperPair)return PairEffect(pick.Pair);
            var fighter=WhisperFighter(pick.FighterId);
            if(fighter==null)return "Fighter unavailable";
            string rankTwo=fighter.Class==UnitClass.Warden?"Guard two allies.":fighter.Class==UnitClass.Skirmisher?"Two strikes per spell.":
                fighter.Class==UnitClass.Marksman?"Two shots per aim.":fighter.Class==UnitClass.Trapper?"Two springs per trap.":"Can keep one treatment in hand.";
            return SliceClasses.Name(fighter.Class)+" / R"+fighter.Rank+" TO R"+(fighter.Rank+1)+". "+
                (fighter.Rank>=2?"Second full gear slot.":rankTwo);
        }

        PxLines WhisperLines(int choice,bool compact=false)
        {
            var pick=WhisperChoice(choice);
            if(pick==null)return new PxLines().Add("Pick unavailable",PxRed);
            var lines=new PxLines().Add(WhisperTitle(pick),PxMint)
                .Add(pick.Kind==Favour.WhisperRankUp?"FIGHTER RANK":pick.Kind==Favour.WhisperPair?"SQUAD PAIR":"HABIT UPGRADE",PxGold);
            if(compact&&pick.Kind==Favour.WhisperRankUp)
            {
                var fighter=WhisperFighter(pick.FighterId);
                if(fighter!=null)lines.Add("R"+fighter.Rank+" TO R"+(fighter.Rank+1)+" / "+SliceClasses.Name(fighter.Class),PxBone);
            }
            if(!compact)lines.Gap().Add(WhisperEffect(pick),PxBone)
                .Add("For this run. Fallen in a lost fight: top rank lost. Capture: rank 1.",PxMuted);
            return lines;
        }

        static string PairEffect(SquadPair pair) => pair==SquadPair.MendAndHold?
            "After the Medic treats, the Warden Guards the patient and Medic.":
            SliceClasses.Name(WhisperRules.FirstOf(pair))+" sets up "+SliceClasses.Name(WhisperRules.AnswerOf(pair))+
            ". One free answer per spell cycle, at least 1.5s apart.";

        static string HabitEffect(WhispererHabit habit,bool up)
        {
            switch(habit)
            {
                case WhispererHabit.Bodyguard:return up?"Guard the back row while they aim.":"Guard the Marksman while he aims.";
                case WhispererHabit.Rescuer:return up?"Guard jumps immediately to the lowest health ally.":"Guard the lowest health ally.";
                case WhispererHabit.Breaker:return up?"Strike restarts a winding enemy's wind-up.":"Strike the enemy winding up a big move.";
                case WhispererHabit.Avenger:return up?"Strike back immediately, once per spell cycle.":"Strike whoever last hit an ally.";
                case WhispererHabit.CounterShot:return up?"Shoot the back row's attacker without aiming.":"Aim at whoever last hit your back row.";
                case WhispererHabit.Spotter:return up?"Shoot a winding enemy; Opening stays until spent.":"Shoot a winding enemy and leave an Opening.";
                case WhispererHabit.Ambusher:return up?"Trap stops the heavy blow that springs it.":"Trap waits for a heavy blow.";
                case WhispererHabit.Triage:return up?"Wait below 25% health; saved treatment heals double.":"Save treatment for an ally below 25% health.";
                case WhispererHabit.FrontFirst:return up?"Treatment reaches the entire front row.":"Treat the front row first.";
                default:return "Unavailable in this build.";
            }
        }

        void ShowHabits()
        {
            CloseCamp();CloseModal();Modal("SQUAD HABITS","");
            PxWrite(_modal,44,74,632,new PxLines().Add("FREE AT CAMP / KEPT BETWEEN RUNS",PxGold)
                .Add("Pick a class habit. The Whisperer's road picks upgrade it for one run.",PxBone));
            bool camp=_campaign.Stage!=CampaignStage.InRun;
            int row=0;
            foreach(var cls in new[]{UnitClass.Warden,UnitClass.Skirmisher,UnitClass.Marksman,UnitClass.Trapper,UnitClass.Medic})
            {
                int y=124+row++*46;
                SetPx(PxLabel(_modal,42,y+4),SliceClasses.Name(cls),PxBone);
                for(int h=1;h<=WhisperRules.HabitCount;h++)
                {
                    var habit=(WhispererHabit)h;
                    if(habit==WhispererHabit.Gatekeeper||WhisperRules.ClassOf(habit)!=cls)continue;
                    bool selected=false;
                    foreach(var held in _campaign.Habits)if(held.Habit==habit)selected=true;
                    int x=(h%2==1)?188:438;
                    var button=PxButton("habit-"+h,(selected?"ON / ":"")+FavourRules.HabitName(habit),x,y,244,28,()=>
                    {
                        if(!_campaign.TrySetHabit(habit)){_notice="Habits can only change at camp.";return;}
                        Save();ShowHabits();
                    },_modal);
                    button.interactable=camp&&!selected;
                    OnHover(button.gameObject,()=>HabitEffect(habit,false)+" Upgrade: "+HabitEffect(habit,true));
                }
            }
            SetPx(PxLabel(_modal,44,359),camp?"Changes are free. Point at a habit for its job and upgrade.":"Return to camp to change habits.",PxMuted);
        }
    }
}
