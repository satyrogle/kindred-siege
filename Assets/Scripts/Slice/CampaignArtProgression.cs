using System;
using System.Text;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;

namespace KindredSiege.Slice
{
    public sealed partial class CampaignArtLoop
    {
        // Camp purchases and equipment go through Campaign; this presenter owns no progression rules.
        string TabardWearerName()
        {
            foreach(var fighter in _campaign.Reserve)
                if(fighter.Id==_campaign.TabardWearer)return fighter.DisplayName;
            return "First Warden next run";
        }

        string ForgeSummary()=>!_campaign.TabardRecipeKnown?"Break the Bloodridge chief to learn the recipe.":
            _campaign.TabardForged?"Forged for good. Wearer: "+TabardWearerName()+".":
            "Recipe learned. Forge at camp for "+_campaign.TabardPrice+" materials.";

        void ShowForge(string receipt="")
        {
            if(_campaign==null)return;
            if(_campOpen)CloseCamp();else CloseModal();
            bool canForge=_campaign.CanForgeTabard(out string why);
            var text=new StringBuilder("THE BUTCHER\n\n").Append(CampaignForge.TabardName).Append("\n\n")
                .Append("The wearer's hits drain enemy nerve ").Append(CampaignForge.TabardFear).Append("% harder.\n\n")
                .Append("Price: ").Append(_campaign.TabardPrice).Append(" materials, paid once.\n")
                .Append("Materials at camp: ").Append(_campaign.Materials).Append("\n\n")
                .Append("KEEPS: the forge unlock, even after a loss.\n")
                .Append("RESETS: the tabard comes off at the end of each run. Choose a wearer again, or let the first Warden take it.\n\n")
                .Append(ForgeSummary()).Append("\n\n");
            if(!canForge&&!_campaign.TabardForged)text.Append(why).Append("\n\n");
            if(receipt.Length>0)text.Append(receipt);
            Modal("BUTCHER'S FORGE",text.ToString());
            PxButton("beast-trophies","TROPHIES: "+_campaign.Trophies.Count,480,8,224,26,()=>ShowTrophies(),_modal);
            var forge=PxButton("forge-buy",_campaign.TabardForged?"FORGED":"FORGE: "+_campaign.TabardPrice,16,398,200,28,()=>
            {
                int paid=_campaign.TabardPrice;
                if(!_campaign.TryForgeTabard(out string failure)){ShowForge(failure);return;}
                PlaySound("forge");
                _notice="Forged "+CampaignForge.TabardName+". Paid "+paid+" materials. The unlock stays after a loss.";
                Save();string note=_notice;SetPage(_page);ShowForge(note);
            },_modal);
            forge.interactable=canForge;
            var wear=PxButton("forge-wear","CHOOSE WEARER",504,398,200,28,()=>ShowTabardPicker(),_modal);
            wear.interactable=_campaign.TabardForged&&_campaign.Active==null;
        }

        void ShowTabardPicker(int page=0)
        {
            CloseModal();
            int pages=Math.Max(1,(_campaign.Reserve.Count+9)/10);page=Math.Max(0,Math.Min(page,pages-1));
            Modal("TABARD WEARER","");
            PxWrite(_modal,42,68,636,new PxLines().Add(CampaignForge.TabardName+": "+TabardWearerName(),PxGold)
                .Add("Choose a fighter. Only the wearer gains +"+CampaignForge.TabardFear+"% nerve pressure on hits.",PxBone)
                .Add("Bring the wearer in your selected squad to use it. Page "+(page+1)+" / "+pages+".",PxMuted));
            for(int i=page*10;i<Math.Min(_campaign.Reserve.Count,(page+1)*10);i++)
            {
                var fighter=_campaign.Reserve[i];int slot=i-page*10;
                bool wearing=fighter.Id==_campaign.TabardWearer;
                var button=PxButton("wear-"+fighter.Id,(wearing?"ON: ":"")+fighter.DisplayName,42+slot/5*326,126+slot%5*45,306,30,()=>
                {
                    if(!_campaign.TryWearTabard(fighter.Id,out string failure)){_notice=failure;return;}
                    _notice=fighter.DisplayName+" wears "+CampaignForge.TabardName+".";
                    Save();SetPage(_page);
                },_modal);
                button.interactable=_campaign.CanWearTabard(fighter.Id,out _);
            }
            if(page>0)PxButton("wear-prev","PREVIOUS",16,398,160,28,()=>ShowTabardPicker(page-1),_modal);
            if(page+1<pages)PxButton("wear-next","NEXT",544,398,160,28,()=>ShowTabardPicker(page+1),_modal);
        }

        void ShowRematch(RematchClue clue,string checkedMessage="")
        {
            CloseModal();
            string source=clue.Source==null?"The source fight is missing from this save.":
                "Source fight: "+clue.FightId+"\nMoment: "+Seconds(clue.Lesson.FromTick)+" to "+Seconds(clue.Lesson.ToTick)+".";
            Modal("THEY REMEMBER",clue.ChiefName+"\n\n"+clue.Text+"\n\n"+source+"\n\n"+checkedMessage);
            var check=PxButton("lesson-check","CHECK THE LESSON",16,398,210,28,()=>
            {
                bool valid=_campaign.Archive.TryVerifyLesson(clue.Lesson,out string failure);
                ShowRematch(clue,valid?"Checked: replaying the recorded fight reproduces this lesson.":"Could not verify: "+failure);
            },_modal);
            check.interactable=clue.Source!=null;
            var sourceButton=PxButton("lesson-source","SOURCE FIGHT",504,398,200,28,()=>ShowArchivedFight(clue.Source),_modal);
            sourceButton.interactable=clue.Source!=null;
            MoveModalCloseToTop();
            var watch=PxButton("lesson-watch","WATCH MOMENT",252,398,220,28,()=>WatchFight(clue.Source,clue.Lesson.FromTick,clue.Lesson.ToTick),_modal);
            watch.interactable=clue.Source!=null;
        }

        void ShowFightHistory(int page=0)
        {
            CloseModal();var fights=_campaign.Archive.Fights;
            int pages=Math.Max(1,(fights.Count+7)/8);page=Math.Max(0,Math.Min(page,pages-1));
            Modal("FIGHT HISTORY",fights.Count==0?"No completed fights yet.":"");
            if(fights.Count>0)SetPx(PxLabel(_modal,42,68),"Newest first. Page "+(page+1)+" / "+pages+". Select a recorded fight.",PxMuted);
            for(int n=0;n<8&&page*8+n<fights.Count;n++)
            {
                var fight=fights[fights.Count-1-page*8-n];
                PxButton("history-"+n,fight.FightId+"  /  "+Seconds(fight.Ticks),42,92+n*35,636,28,()=>ShowArchivedFight(fight),_modal);
            }
            if(page>0)PxButton("history-prev","PREVIOUS",16,398,160,28,()=>ShowFightHistory(page-1),_modal);
            if(page+1<pages)PxButton("history-next","NEXT",544,398,160,28,()=>ShowFightHistory(page+1),_modal);
        }

        void ShowArchivedFight(ArchivedFight fight,string checkedMessage="")
        {
            CloseModal();
            var text=new StringBuilder(fight.FightId).Append("\nDuration: ").Append(Seconds(fight.Ticks)).Append("\n\n");
            if(checkedMessage.Length>0)text.Append(checkedMessage).Append("\n\n");
            text.Append("RECORDED MOMENTS\n");
            foreach(var e in fight.Record.Events)
            {
                string who=e.ActorSide==0?"You":e.ActorSide==1?"They":"Both sides",line=null;
                switch(e.Kind)
                {
                    case BattleEventKind.Directive:
                        line=e.Detail==BattleSimulation.BlockedDetail?who+": shields stopped Unleash.":who+": "+Pretty(e.Detail)+".";break;
                    case BattleEventKind.Windup:line=who+": a cast starts.";break;
                    case BattleEventKind.Release:line=who+": cast "+(e.Detail==FightRules.Parried?"parried":e.Detail==Moves.Dodged?"dodged":e.Detail==Moves.Soaked?"halved":"lands")+".";break;
                    case BattleEventKind.Kill:line=who+": "+SliceClasses.Name((UnitClass)e.TargetClass)+" on the other side fell.";break;
                    case BattleEventKind.Break:line=who+": nerve broke, "+e.Amount+" standing.";break;
                    case BattleEventKind.End:line="Fight ended.";break;
                }
                if(line!=null)text.Append(Seconds(e.Tick)).Append("  ").Append(line).Append('\n');
            }
            Modal("RECORDED FIGHT",text.ToString());
            MoveModalCloseToTop();
            PxButton("replay-watch","WATCH FIGHT",252,398,220,28,()=>WatchFight(fight),_modal);
            PxButton("history-back","FIGHT HISTORY",16,398,200,28,()=>ShowFightHistory(),_modal);
            PxButton("replay-check","REPLAY CHECK",504,398,200,28,()=>
            {
                bool valid=_campaign.Archive.TryReplay(fight.FightId,out _,out string failure);
                ShowArchivedFight(fight,valid?"Replay matches the saved fight.":"Replay unavailable: "+failure);
            },_modal);
        }
    }
}
