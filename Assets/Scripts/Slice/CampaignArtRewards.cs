using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;

namespace KindredSiege.Slice
{
    public sealed partial class CampaignArtLoop
    {
        bool ThemePresentation => _campaign!=null&&System.Math.Min(Campaign.RulesVersion,_campaign.Tuning.RulesCap)>=Campaign.MarksRules;
        bool CombinedRewardPending => _campaign.FavourOffer!=null&&_campaign.SpoilsChoices.Count>0;

        // Both lists are the same authoritative offer. Viewing a card never spends anything.
        void ShowRewardChoices()
        {
            if(!CombinedRewardPending){if(_campaign.FavourOffer!=null)ShowFavours();else ShowSpoilsChoices();return;}
            CloseModal();Modal(FavourRules.PalName(_campaign.FavourOffer.Pal).ToUpperInvariant()+" / PICK ONE","");
            _buttons["modal-close"].gameObject.SetActive(false);
            int price=_campaign.SpoilsMaterials;
            PxWrite(_modal,36,66,648,new PxLines().Add("PAL PICK OR GEAR OR "+price+" MAT",PxGold));
            var offer=_campaign.FavourOffer;
            for(int i=0;i<offer.Choices.Count;i++)
            {
                int choice=i,x=26+i*226;var favour=offer.Choices[i];int rank=_campaign.FavourRank(favour);
                var lines=new PxLines().Add(Fit(FavourRules.Name(favour),164),PxGold)
                    .Add(rank>0?"UPGRADE "+rank+" > "+(rank+1):"NEW / RANK 1",PxBone);
                if(FavourRules.IsCombo(favour))
                {
                    var bend=FavourRules.ComboBend(favour);
                    lines.Add(SliceClasses.Name(ClassSpells.ClassOf(bend))+" / "+MarkRules.Name(MarkRules.Of(bend)),ComboColour(bend));
                }
                else lines.Add(FavourRules.PalName(offer.Pal),PxMuted);
                if(FavourRules.IsWhisper(favour))lines=WhisperLines(choice,true);
                PxPanel(x,108,216,88,lines,_modal);
                PxButton("reward-pal-"+i,"VIEW PAL PICK",x,200,216,26,()=>ShowRewardDetail(true,choice),_modal);
            }
            for(int i=0;i<_campaign.SpoilsChoices.Count;i++)
            {
                int choice=i,x=26+i*226;var piece=_campaign.SpoilsChoices[i];
                PxPanel(x,240,216,88,new PxLines().Add(Fit(piece.Name,164),PxBone).Add(GearClass(piece),PxGold)
                    .Add(piece.Tier+" / "+CampaignGearRules.StatText(piece.Stat,piece.Bonus),PxMint),_modal);
                PxButton("reward-gear-"+i,"VIEW TRIBE GEAR",x,332,216,26,()=>ShowRewardDetail(false,choice),_modal);
            }
            PxButton("bank-reward","KEEP MATERIALS",264,398,192,28,()=>{CloseModal();if(_page==Page.Result)AfterResult();},_modal);
            SelectControl("reward-pal-0");
        }

        void ShowRewardDetail(bool pal,int choice)
        {
            if(!CombinedRewardPending){ShowRewardChoices();return;}
            var offer=_campaign.FavourOffer;
            if(choice<0||choice>=(pal?offer.Choices.Count:_campaign.SpoilsChoices.Count))return;
            int price=_campaign.SpoilsMaterials;bool allowed;string why;PxLines lines,full;string title,action;
            if(pal)
            {
                var favour=offer.Choices[choice];title="PAL PICK";full=FavourChoiceLines(favour,choice);lines=FavourRules.IsWhisper(favour)?WhisperLines(choice):CompactFavourLines(favour);
                if(FavourRules.IsCombo(favour))full.Gap().Add(_campaign.FavourRank(favour)>0?
                    "Ranks up this piece. Its current wearer stays the same.":
                    "Auto-equips the first class fighter without a combo; otherwise stays in run gear.",PxMuted);
                lines.Gap().Add("+0 BOUNTY",PxMint);
                action=_campaign.FavourRank(favour)>0?"UPGRADE":"TAKE PICK";
                allowed=_campaign.CanTakeFavour(choice,out why);
            }
            else
            {
                var piece=_campaign.SpoilsChoices[choice];title="TRIBE GEAR";
                full=new PxLines().Add(piece.Name,PxBone).Add(GearClass(piece),PxGold).Add(piece.Describe(),PxBone)
                    .Gap().Add("Stripping raises bounty. Equip through GEAR. For this run.",PxOrange);
                lines=new PxLines().Add(piece.Name,PxBone).Add(GearClass(piece)+" / "+CampaignGearRules.StatText(piece.Stat,piece.Bonus),PxMint)
                    .Add(piece.Bend==ClassBend.None?"":ClassSpells.Describe(piece.Bend),PxBone)
                    .Gap().Add("RUN ONLY / +"+BountyRules.HeatPerStrip+" HEAT / EQUIP IN GEAR",PxOrange);
                action="STRIP GEAR";allowed=_campaign.CanTakeSpoils(choice,out why);
            }
            if(!allowed)lines.Gap().Add(why,PxRed);
            lines.Gap().Add("-"+price+" MAT = THIS PICK / OTHERS LOST",PxGold);
            CloseModal();Modal(title,"");_buttons["modal-close"].gameObject.SetActive(false);PxWrite(_modal,36,66,648,lines);
            PxButton(pal?"favour-"+choice:"strip-"+choice,action+" (-"+price+" MAT)",360,398,328,28,
                ()=>{if(pal)TakeFavour(choice);else TakeClassSpoils(choice);},_modal).interactable=allowed;
            PxButton("reward-back","BACK TO ALL REWARDS",16,398,328,28,ShowRewardChoices,_modal);
            PxButton("reward-details","DETAILS",548,310,140,26,()=>
            {
                CloseModal();Modal("REWARD DETAILS",string.Join("\n",full.texts));
                PxButton("reward-detail-back","BACK TO PICK",16,398,216,28,()=>ShowRewardDetail(pal,choice),_modal);
            },_modal);
            SelectControl("reward-back");
        }
    }
}
