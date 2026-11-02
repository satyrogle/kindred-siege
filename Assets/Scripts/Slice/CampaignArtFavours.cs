using System.Collections.Generic;
using KindredSiege.Sim;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;

namespace KindredSiege.Slice
{
    public sealed partial class CampaignArtLoop
    {
        // Receipts only. The offer, price, rank and lifetime belong to Campaign, including after reload.
        EncounterResult _favourReceiptResult;
        Favour _favourReceipt;
        int _favourReceiptRank;
        int _favourReceiptCost;
        string _favourReceiptPlacement;
        string _whisperReceiptTitle, _whisperReceiptEffect;

        bool FavourExchanged(EncounterResult r)=>r!=null&&r==_favourReceiptResult;

        void FavourButton(int x,int y,int width)
        {
            PxButton("favours",_campaign.FavourOffer!=null?"PAL CHOICE":HeldComboCount()>0?"PAL PICKS":"FAVOURS",x,y,width,28,ShowFavours);
        }

        int HeldComboCount()
        {
            int count=0;
            foreach(var piece in _campaign.Gear)if(ClassSpells.IsCombo(piece.Bend))count++;
            return count;
        }

        static string ComboAugmentName(ClassBend bend)
        {
            switch(ClassSpells.AugmentOf(bend))
            {
                case Augment.GravityWell:return "Gravity Well";
                case Augment.FoldDoor:return "Fold Door";
                case Augment.TimeStop:return "Time Stop";
                default:return "";
            }
        }

        static string ComboPactName(ClassBend bend)
        {
            switch(ClassSpells.AugmentOf(bend))
            {
                case Augment.GravityWell:return "Heavy";
                case Augment.FoldDoor:return "Unnerving";
                case Augment.TimeStop:return "Borrowed time";
                default:return "";
            }
        }

        UnityEngine.Color32 ComboColour(ClassBend bend)
        {
            switch(ClassSpells.AugmentOf(bend))
            {
                case Augment.GravityWell:return PxLilac;
                case Augment.FoldDoor:return PxBlue;
                default:return PxGold;
            }
        }

        string ComboPlacement(ClassBend bend)
        {
            var piece=_campaign.FindGear(Campaign.ComboGearId(bend));
            if(piece==null)return "Not held.";
            foreach(var fighter in _campaign.Reserve)
                if(fighter.GearId==piece.Id||fighter.GearId2==piece.Id)return "Equipped: "+FighterName(fighter)+(fighter.GearId2==piece.Id?" / slot 2":" / slot 1")+".";
            return "In run gear / not equipped. Choose a wearer through GEAR.";
        }

        static string ComboThemeText(ClassBend bend,int rank)
        {
            string mark=MarkRules.Name(MarkRules.Of(bend));
            return "Rank "+rank+" / "+MarkRules.MaxStacks+" - lays "+rank+" "+mark+". At "+MarkRules.MaxStacks+": "+
                (mark==MarkRules.Crush?"burst hit + pin.":mark==MarkRules.Exposed?"next blow cannot miss or be Guarded; double crit.":
                "freeze + restart the big wind-up.");
        }

        PxLines FavourChoiceLines(Favour favour,int choice=-1)
        {
            if(FavourRules.IsWhisper(favour))return WhisperLines(choice);
            int rank=_campaign.FavourRank(favour),next=rank+1;
            if(FavourRules.IsCombo(favour))
            {
                var bend=FavourRules.ComboBend(favour);
                var comboLines=new PxLines().Add(FavourRules.Name(favour),ComboColour(bend));
                if(ThemePresentation)comboLines.Add(rank==0?"New - rank 1 / "+MarkRules.MaxStacks:"Upgrade: rank "+rank+" to "+next,PxGold);
                comboLines.Add(SliceClasses.Name(ClassSpells.ClassOf(bend))+" / "+ComboAugmentName(bend),PxBone)
                    .Add("Pact: "+ComboPactName(bend),PxOrange)
                    .Add("One gear slot / this run",PxGold).Gap()
                    .Add(ClassSpells.Describe(bend),PxBone);
                if(ThemePresentation)comboLines.Add(ComboThemeText(bend,next),ComboColour(bend));
                return comboLines;
            }
            var lines=new PxLines().Add(FavourRules.Name(favour),OrderColour(FavourRules.Button(FavourRules.PalOf(favour))))
                .Add(rank==0?"New - rank 1 / "+FavourRules.MaxRank:"Upgrade: rank "+rank+" to "+next,PxGold).Gap()
                .Add(FavourRules.Describe(favour,next),PxBone);
            if(rank>0)lines.Gap().Add("Now: "+FavourRules.Describe(favour,rank),PxMuted);
            return lines;
        }

        string HeldFavourText()
        {
            var lines=new List<string>{"FOR THIS RUN", "Pal picks leave when the run ends. Materials stay at camp.", ""};
            foreach(var piece in _campaign.Gear)
            {
                if(!ClassSpells.IsCombo(piece.Bend))continue;
                lines.Add("Butcher combo: "+piece.Name+" / "+SliceClasses.Name(ClassSpells.ClassOf(piece.Bend)));
                lines.Add(ComboAugmentName(piece.Bend)+" / pact: "+ComboPactName(piece.Bend)+" / one gear slot.");
                lines.Add(ComboPlacement(piece.Bend));
                lines.Add(ClassSpells.Describe(piece.Bend));
                if(ThemePresentation)lines.Add(ComboThemeText(piece.Bend,piece.Rank));
                lines.Add("");
            }
            foreach(var held in _campaign.Favours)
            {
                lines.Add(FavourRules.PalName(FavourRules.PalOf(held.Favour))+": "+FavourRules.Name(held.Favour)+" - rank "+held.Rank+" / "+FavourRules.MaxRank);
                lines.Add(FavourRules.Describe(held.Favour,held.Rank));
                lines.Add("");
            }
            if(_campaign.Favours.Count==0&&HeldComboCount()==0)lines.Add("No Pal picks yet. Win a Pal's road fight to earn a choice.");
            if(PalFloorRun)lines.Add(PalFloorText());
            return string.Join("\n",lines);
        }

        void ShowFavours()
        {
            if(CombinedRewardPending){ShowRewardChoices();return;}
            CloseModal();
            var offer=_campaign.FavourOffer;
            if(offer==null){Modal("PAL FAVOURS",HeldFavourText());return;}
            LoadPixelKit();
            _modalPreviousSelection=UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject;
            _modal=Rect(_root,"Pal favour choice",0,0,W,H);
            Panel(_modal,0,0,W,H,Ink).raycastTarget=true;
            bool combos=false;foreach(var choice in offer.Choices)combos|=FavourRules.IsCombo(choice);
            var heading=_logoFont.Render(((combos?"A COMBO FROM ":"A FAVOUR FROM ")+FavourRules.PalName(offer.Pal)).ToUpperInvariant(),PxBone,PxGold,1,true);
            _screenTextures.Add(heading);PxImage(_modal,"Title",heading,14,4);
            PxButton("modal-close","KEEP MATERIALS",264,398,192,28,CloseModal,_modal);
            SelectControl("modal-close");
            int price=_campaign.SpoilsMaterials;
            var brief=new PxLines()
                .Add("Choose ONE for "+price+" materials, or keep the materials already at camp.",PxGold)
                .Add("Offer lasts until the next fight or event. Picks last for this run.",PxMuted);
            if(combos)brief.Add("Auto-equips the first class fighter without a combo; their old gear stays.",PxOrange)
                .Add("If every class fighter wears a combo, this stays unworn. Change it through GEAR.",PxBone);
            PxWrite(_modal,36,55,648,brief);
            for(int i=0;i<offer.Choices.Count;i++)
            {
                int choice=i,x=26+i*226;var favour=offer.Choices[i];
                int rank=_campaign.FavourRank(favour);
                var lines=FavourChoiceLines(favour,choice);
                bool can=_campaign.CanTakeFavour(choice,out string why);
                if(!can)lines.Gap().Add(why,PxRed);
                int top=combos?112:94;
                PxPanel(x,top,216,326-top,lines,_modal);
                PxButton("favour-"+choice,(rank>0?"UPGRADE":"TAKE")+" (-"+price+" MAT)",x,334,216,28,()=>TakeFavour(choice),_modal).interactable=can;
            }
            SetPx(PxLabel(_modal,36,375),"At camp: "+_campaign.Materials+" materials. Held: "+HeldComboCount()+" combos / "+_campaign.Favours.Count+" favours.",PxMuted);
            PxButton("held-favours","HELD FAVOURS",16,398,216,28,()=>
            {
                CloseModal();Modal("HELD FAVOURS",HeldFavourText());
                PxButton("favour-offer","BACK TO CHOICE",16,398,216,28,ShowFavours,_modal);
            },_modal);
        }

        void TakeFavour(int choice)
        {
            var offer=_campaign.FavourOffer;
            if(offer==null||choice<0||choice>=offer.Choices.Count)return;
            var favour=offer.Choices[choice];
            var whisper=WhisperChoice(choice);
            string whisperTitle=whisper==null?null:WhisperTitle(whisper), whisperEffect=whisper==null?null:WhisperEffect(whisper);
            int cost=_campaign.SpoilsMaterials;
            if(!_campaign.TryTakeFavour(choice,out string why)){_notice=why;ShowFavours();return;}
            _favourReceiptResult=_page==Page.Result?_result:null;
            _favourReceipt=favour;_favourReceiptRank=_campaign.FavourRank(favour);
            _favourReceiptCost=cost;
            _whisperReceiptTitle=whisperTitle;_whisperReceiptEffect=whisperEffect;
            _favourReceiptPlacement=FavourRules.IsCombo(favour)?ComboPlacement(FavourRules.ComboBend(favour)):"";
            _notice=FavourRules.IsCombo(favour)?FavourRules.Name(favour)+(ThemePresentation?" - rank "+_favourReceiptRank:"")+". "+_favourReceiptPlacement:
                FavourRules.Name(favour)+" - rank "+_favourReceiptRank+". For this run.";
            if(whisper!=null)_notice=whisperTitle+". "+whisperEffect+" For this run.";
            PlaySound("favour");
            Save();SetPage(_page);
        }

        void FavourResultLines(PxLines lines,EncounterResult r)
        {
            if(FavourExchanged(r))
            {
                if(!string.IsNullOrEmpty(_whisperReceiptTitle))
                {
                    lines.Gap().Add(_whisperReceiptTitle,PxMint).Add(_whisperReceiptEffect,PxBone)
                        .Add("For this run. Paid "+_favourReceiptCost+" materials.",PxMuted);
                    return;
                }
                bool combo=FavourRules.IsCombo(_favourReceipt);
                lines.Gap().Add((combo?"Combo taken: ":"Favour taken: ")+FavourRules.Name(_favourReceipt)+(!combo||ThemePresentation?" - rank "+_favourReceiptRank:""),PxGold)
                    .Add(FavourRules.Describe(_favourReceipt,_favourReceiptRank),PxBone);
                if(combo)lines.Add(_favourReceiptPlacement,PxMint);
                lines.Add("For this run. Paid "+_favourReceiptCost+" materials.",PxMuted);
            }
            else if(r.FavourOffered!=null&&_campaign.FavourOffer!=null)
                lines.Gap().Add("A favour from "+FavourRules.PalName(r.FavourOffered.Pal),PxGold)
                    .Add("Choose one instead of the +"+_campaign.SpoilsMaterials+" materials. Or keep them.",PxBone)
                    .Add("Choice stays on the road until the next encounter.",PxMuted);
        }

        // Squads are supplied by Campaign; only collapse repeated class names for a readable preview.
        static string BandNames(IReadOnlyList<UnitClass> classes)
        {
            var names=new List<string>();var seen=new List<UnitClass>();
            foreach(var c in classes)
            {
                if(seen.Contains(c))continue;
                seen.Add(c);int count=0;foreach(var other in classes)if(other==c)count++;
                names.Add(count+" "+SliceClasses.Name(c));
            }
            return string.Join(", ",names);
        }

        static string EnemyPreview(IReadOnlyList<UnitClass> classes,EncounterKind kind,CampaignTribe tribe,bool casts,CampaignHunter hunter=null)
        {
            if(kind==EncounterKind.Duel)return "Champion: "+BandNames(classes)+(casts?" (casts)":"");
            string extra=kind==EncounterKind.Chief?" + chief ("+SliceClasses.Name(tribe.ChiefClass)+")":
                kind==EncounterKind.Trial&&casts?" + shaman":HunterFight(kind)?" + captain ("+SliceClasses.Name(hunter==null?tribe.ChiefClass:hunter.Class)+")":"";
            return "Enemies: "+BandNames(classes)+extra;
        }

        static string EnemyPreview(Encounter e)=>e.Kind==EncounterKind.Chief&&e.EnemyClasses.Count==0?
            "SOLO CHIEF: "+e.Tribe.ChiefName:EnemyPreview(e.EnemyClasses,e.Kind,e.Tribe,e.EnemyCasts,e.Hunter);

        string NodeEnemyPreview(RoadNode node,Encounter e)
        {
            if(e!=null)return EnemyPreview(e);
            if(Campaign.IsRaidChief(_campaign.Target,node.Fight,Campaign.RulesVersion))return "SOLO CHIEF: "+_campaign.Target.ChiefName;
            bool ambush=_campaign.IsAmbush(node);
            return EnemyPreview(_campaign.SquadOn(node),ambush?EncounterKind.Ambush:node.Fight,
                ambush?AlphaHunt.Tribe:_campaign.Target,
                node.Fight==EncounterKind.Trial&&_campaign.Target.TrialCasts||node.Fight==EncounterKind.Duel&&_campaign.Tuning.DuelChampionCasts,
                ambush?_campaign.HunterOnRoad:null);
        }

        bool HasPalReward(RoadNode node)
        {
            return node!=null&&!_campaign.IsAmbush(node)&&_campaign.PalPickOffered(node);
        }
    }
}
