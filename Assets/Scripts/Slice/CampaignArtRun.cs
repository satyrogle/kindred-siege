using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using UnityEngine;

namespace KindredSiege.Slice
{
    public sealed partial class CampaignArtLoop
    {
        // The Sprint 2 screens (Jacob, 13 Oct 2026: "go build the sprint 2 screens"; DESIGN_LOCKS, "Sprint 2 calls"):
        // your roster as your lives, capture or kill, strip or bank, gear, quirks, sets, front and back rows and
        // grudges. Every rule stays in Campaign; this file shows them and passes the clicks on.

        /// "Fighters: 6 (need 3)". Your fighters are your lives; the run ends under Campaign.MinSquad.
        string LivesText()=>LivesRun?"Lives: "+_campaign.Lives+"/"+Campaign.RunLives:"Fighters: "+_campaign.Reserve.Count+" (need "+Campaign.MinSquad+")";

        /// Who beat you, for a sentence: the tribe, or the hunters.
        static string Takers(Encounter e)=>HunterFight(e.Kind)?"the hunters":e.Tribe.Name;

        /// "Nakht / Warden".
        static string FighterName(CampaignFighter f)=>string.IsNullOrEmpty(f.Name)?f.DisplayName:f.Name+" / "+SliceClasses.Name(f.Class);

        CampaignFighter FindReserve(string fighterId)
        {
            foreach(var f in _campaign.Reserve)if(f.Id==fighterId)return f;
            return null;
        }

        /// The fighter's Grudge counts in a fight against this tribe: they captured one of yours this run.
        bool GrudgeLive(CampaignFighter f,string tribeId)
        {
            if(f.Quirk!=Quirk.Grudge)return false;
            foreach(string g in _campaign.Grudges)if(g==tribeId)return true;
            return false;
        }

        int GrudgeCount(string tribeId)
        {
            int count=0;
            foreach(var f in _campaign.Reserve)if(GrudgeLive(f,tribeId))count++;
            return count;
        }

        /// The text in capitals, cut with ".." until the pixel font draws it within max grid pixels.
        string Fit(string text,int max)
        {
            string t=(text??"").ToUpperInvariant();
            if(_tinyFont.Measure(t,1)<=max)return t;
            while(t.Length>1&&_tinyFont.Measure(t+"..",1)>max)t=t.Substring(0,t.Length-1);
            return t.TrimEnd()+"..";
        }

        // How wide SetPx draws these parts side by side: each part's own width, and 6 between parts.
        int PartsWidth(List<string> parts)
        {
            int width=0;
            foreach(string p in parts)width+=_tinyFont.Measure(p.ToUpperInvariant(),1)+2;
            return width+6*Math.Max(0,parts.Count-1);
        }

        // ------------------------------------------------------------ the fight cards

        /// A fight card's tags, a glance each (Jacob, 11 Oct 2026: "in Hades you literally know looking at one icon").
        PxLines CardTags(Encounter e)
        {
            var tuning=_campaign.Tuning;bool chief=e.Kind==EncounterKind.Chief,hunt=HunterFight(e.Kind);
            var tags=new PxLines().Add(e.Title,PxGold).Gap().Add("Squad "+e.SquadSize+"   Enemies "+e.EnemyUnits,PxBone);
            if(chief)tags.Add(_campaign.ChiefOf(e.Tribe.Id).FullName,PxGold);
            // The named hunter and the button that answers each of their traits (CampaignArtHunters.cs).
            if(e.Hunter!=null)AddLines(tags,HunterLines(e.Hunter,e.HunterRank,true));
            if(e.EnemyCasts)tags.Add("They cast",PxYellow);
            if(e.NoFallBack)tags.Add("No Fall Back",PxOrange);
            int grudges=GrudgeCount(e.Tribe.Id);
            if(grudges>0)tags.Add("Grudge: "+grudges+" of yours +"+Quirks.GrudgePercent+"%",PxOrange);
            tags.Add("Win: +"+(chief?tuning.MaterialsPerChief:hunt?HunterReward(e,false):tuning.MaterialsPerFight)+" materials",PxMint);
            if(chief)tags.Add("Break him alive: tribe joins",PxMint).Add("His order: "+e.Tribe.AssistName,PxMint).Add("Kill him: his Princess gear",PxMint);
            if(hunt)tags.Add(e.Kind==EncounterKind.AlphaHunt?"Win the run. No recruits.":"Climb the ladder. No recruits.",PxMint);
            tags.Add(LossWarning(e),PxRed);
            return tags;
        }

        // ------------------------------------------------------------ the squad page

        /// One fighter on the squad page: the plate that picks them, then FRONT or BACK and GEAR. A wide row (ten
        /// fighters or fewer) shows name, class, quirk and gear; a narrow one, in two columns, name and quirk with
        /// one letter buttons. Pointing at any of it says the whole of it at the foot of the page.
        void SquadRow(CampaignFighter f,Encounter encounter,int x,int y,int width)
        {
            bool selected=_squad.Contains(f.Id),wide=width>300,front=f.Row==Row.Front;
            bool grudge=GrudgeLive(f,encounter.Tribe.Id);
            var piece=_campaign.GearOf(f.Id);
            var second=PalFloorRun?_campaign.FindGear(f.GearId2):null;
            int small=wide?54:30,pickW=width-2*small-8;
            var pick=PxButton("fighter-"+f.Id,"",x,y,pickW,28,()=>
            {
                if(_squad.Contains(f.Id))_squad.Remove(f.Id);
                else if(encounter.Kind==EncounterKind.Duel){_squad.Clear();_squad.Add(f.Id);}
                else if(_squad.Count<encounter.SquadSize)_squad.Add(f.Id);
                SetPage(Page.Squad);
            });
            if(!selected)pick.targetGraphic.color=new Color(.62f,.6f,.66f);
            var mark=selected?PxGlyph(PxCheck,PxMint):_tinyFont.Render("+",PxMuted,PxMuted,1,true);
            _screenTextures.Add(mark);
            PxImage(pick.transform,"Mark",mark,3,9);
            SquadPortrait(pick.transform,f);
            string name=string.IsNullOrEmpty(f.Name)?f.DisplayName:wide?FighterName(f):f.Name;
            if(PalFloorRun)name+=" R"+f.Rank;
            var parts=new List<string>{name};
            var colours=new List<Color32>{f.Id==_campaign.TabardWearer?PxGold:selected?PxBone:PxMuted};
            if(f.Quirk!=Quirk.None)
            {
                parts.Add(Quirks.Name(f.Quirk)+(grudge?" +"+Quirks.GrudgePercent+"%":""));
                colours.Add(grudge?PxOrange:selected?PxGold:PxMuted);
            }
            if(wide&&piece!=null)
            {
                parts.Add(ClassSpells.IsCombo(piece.Bend)?"Combo: "+piece.Name:CampaignGearRules.StatText(piece.Stat,piece.Bonus));
                colours.Add(selected?PxMint:PxMuted);
            }
            int room=pickW-54;
            if(PartsWidth(parts)>room&&parts.Count>2){parts.RemoveAt(2);colours.RemoveAt(2);}
            int over=PartsWidth(parts)-room;
            if(over>0)parts[0]=Fit(parts[0],_tinyFont.Measure(parts[0].ToUpperInvariant(),1)-over);
            SetPx(PxLabel(pick.transform,40,9),parts.ToArray(),colours.ToArray());
            int bx=x+pickW+4;
            var rowButton=PxButton("row-"+f.Id,wide?(front?"FRONT":"BACK"):(front?"F":"B"),bx,y,small,28,()=>FlipRow(f.Id));
            var gearButton=PxButton("gear-"+f.Id,wide?"GEAR":"G",bx+small+4,y,small,28,()=>ShowGearPicker(f.Id));
            if(piece!=null||second!=null)
            {
                var gem=PxGlyph(new[]{"...#...","..###..",".#####.","#######",".#####.","..###..","...#..."},PxMint);
                _screenTextures.Add(gem);PxImage(gearButton.transform,"Equipped piece",gem,5,10);
                var label=_pxButtonLabels["gear-"+f.Id];label.x+=4;label.key="";
                SetPx(label,wide?"GEAR":"G",PxMint);
            }
            string about=AboutFighter(f,encounter);
            OnHover(pick.gameObject,()=>about);
            OnHover(rowButton.gameObject,()=>FighterName(f)+" stands in the "+(front?"front":"back")+" row. Click to move them to the "+(front?"back.":"front."));
            OnHover(gearButton.gameObject,()=>(piece==null?FighterName(f)+" has no piece in slot 1.":piece.Name+". "+(piece.Bend==ClassBend.None?piece.Describe():ClassSpells.Describe(piece.Bend)))+SecondGearHint(f)+" Click to choose gear.");
        }

        // A fighter's quirk, live grudge and gear, for the foot of the squad page.
        string AboutFighter(CampaignFighter f,Encounter encounter)
        {
            string quirk=f.Quirk==Quirk.None?"No quirk.":
                GrudgeLive(f,encounter.Tribe.Id)?"Grudge: "+Quirks.GrudgePercent+"% more damage on "+Takers(encounter)+" this fight.":
                Quirks.Name(f.Quirk)+": "+Quirks.Describe(f.Quirk);
            var piece=_campaign.GearOf(f.Id);
            string scars=f.Scars.Count==0?"":" Scars: "+string.Join(", ",System.Linq.Enumerable.Select(f.Scars,BeastKits.ScarName))+". "+BeastKits.ScarText(f.Class);
            return FighterName(f)+". "+quirk+scars+" "+(piece==null?"No gear.":"Gear: "+piece.Name+". "+piece.Describe())+SecondGearHint(f);
        }

        /// The squad page's foot line: what the pointed or selected control is, whenever no notice is showing.
        IEnumerator SquadFootLine(RectTransform screen)
        {
            var line=PxLabel(screen,16,434);
            string shown=null;
            while(_screen==screen)
            {
                string now=!string.IsNullOrEmpty(_notice)?"":_hover.Length>0?_hover:Time.unscaledTime<_keyboardHintUntil?_keyboardHint:"";
                if(now!=shown){shown=now;SetPx(line,Fit(now,688),PxBone);}
                yield return null;
            }
        }

        /// The squad page's run lines: your lives, the sets this squad makes, and a grudge that counts here.
        void SquadRunLines(PxLines lines,Encounter encounter)
        {
            AddTribeBrief(lines,encounter);
            lines.Add("Your fighters are your lives: "+_campaign.Reserve.Count+". "+LossWarning(encounter)+" The fallen stay down. Under "+
                Campaign.MinSquad+" and the run ends.",PxMuted);
            var counts=new Dictionary<string,int>();var order=new List<string>();
            foreach(var f in _campaign.Reserve)
            {
                if(!_squad.Contains(f.Id))continue;
                counts.TryGetValue(f.TribeId,out int n);
                if(n==0)order.Add(f.TribeId);
                counts[f.TribeId]=n+1;
            }
            foreach(string tribe in order)
                if(counts[tribe]>=TribeSets.StatAt)lines.Gap().Add(TribeName(tribe)+" set: "+counts[tribe]+" in this squad",PxGold).Add(TribeSets.Describe(tribe),PxBone);
            int grudges=GrudgeCount(encounter.Tribe.Id);
            if(grudges>0)lines.Gap().Add("Grudge: your Grudge fighters hit "+Takers(encounter)+" "+Quirks.GrudgePercent+"% harder.",PxOrange);
        }

        /// Front or back for the next fight (formation, Jacob, 13 Oct 2026), through Campaign.TrySetRow.
        void FlipRow(string fighterId)
        {
            var f=FindReserve(fighterId);
            if(f==null)return;
            if(!_campaign.TrySetRow(f.Id,f.Row==Row.Front?Row.Back:Row.Front,out string why)){_notice=why;return;}
            Save();SetPage(Page.Squad);SelectControl("row-"+f.Id);
        }

        /// The run's gear for one fighter: a piece per button, the one they wear marked ON. A piece someone else
        /// wears moves over (Campaign.TryEquip). Ten to a page, like the tabard's wearers.
        void ShowGearPicker(string fighterId,int page=0,int gearSlot=0)
        {
            CloseModal();
            var f=FindReserve(fighterId);
            if(f==null)return;
            if(!PalFloorRun||f.Rank<PalStoreRules.SecondSlotRank)gearSlot=0;
            var gear=_campaign.Gear;var worn=gearSlot==0?_campaign.GearOf(f.Id):_campaign.FindGear(f.GearId2);
            int pages=Math.Max(1,(gear.Count+9)/10);page=Math.Max(0,Math.Min(page,pages-1));
            Modal("GEAR"+(PalFloorRun?" / RANK "+f.Rank+" / SLOT "+(gearSlot+1):""),"");
            var gearHint=PxLabel(_modal,42,68);
            GearPickerHint(gearHint,f,worn,page,pages);
            var gearFailure=PxLabel(_modal,42,338);
            for(int i=page*10;i<Math.Min(gear.Count,(page+1)*10);i++)
            {
                var piece=gear[i];int slot=i-page*10;bool on=worn!=null&&worn.Id==piece.Id;
                string wearer="";
                foreach(var r in _campaign.Reserve)if(r.GearId==piece.Id||r.GearId2==piece.Id)wearer=(r.Id==f.Id?"YOU":r.Name)+" / "+(r.GearId2==piece.Id?"SLOT 2":"SLOT 1");
                bool combo=ClassSpells.IsCombo(piece.Bend);
                string detail=combo?"COMBO / "+GearClass(piece):CampaignGearRules.StatText(piece.Stat,piece.Bonus);
                string label=(on?"ON: ":"")+piece.Name+"  "+detail+(wearer.Length>0?"  ("+wearer+")":"");
                var button=PxButton("equip-"+piece.Id,Fit(label,282),42+slot/5*326,126+slot%5*45,306,30,()=>
                {
                    string why;
                    bool equipped=gearSlot==0?_campaign.TryEquip(f.Id,piece.Id,out why):_campaign.TryEquipSecond(f.Id,piece.Id,out why);
                    if(!equipped){SetPx(gearFailure,Fit(why,636),PxRed);return;}
                    PlaySound(piece.Tier==GearTier.Princess?"gear_princess":piece.Tier==GearTier.Mid?"gear_mid":"gear_pauper");
                    Save();SetPage(Page.Squad);SelectControl("gear-"+f.Id);
                },_modal);
                button.interactable=!on&&piece.Fits(f.Class);
                OnHover(button.gameObject,()=>
                {
                    GearPickerHint(gearHint,f,piece,page,pages);
                    return piece.Name+". "+piece.Describe();
                });
                if(!piece.Fits(f.Class))SetPx(_pxButtonLabels["equip-"+piece.Id],Fit(GearClass(piece)+" ONLY: "+piece.Name,282),PxMuted);
            }
            if(worn!=null)PxButton("unequip","TAKE IT OFF",42,350,200,28,()=>
            {
                string why;
                bool removed=gearSlot==0?_campaign.TryUnequip(f.Id,out why):_campaign.TryUnequipSecond(f.Id,out why);
                if(!removed){_notice=why;return;}
                Save();SetPage(Page.Squad);SelectControl("gear-"+f.Id);
            },_modal);
            if(PalFloorRun)
            {
                if(f.Rank>=PalStoreRules.SecondSlotRank)
                {
                    PxButton("gear-slot1","SLOT 1",404,350,128,28,()=>ShowGearPicker(fighterId,page,0),_modal).interactable=gearSlot!=0;
                    PxButton("gear-slot2","SLOT 2",548,350,128,28,()=>ShowGearPicker(fighterId,page,1),_modal).interactable=gearSlot!=1;
                    if(gearSlot==1)SetPx(gearFailure,"SLOT 2: CHOOSE AN UNUSED PIECE TO EQUIP OR REPLACE.",PxMuted);
                }
                else SetPx(gearFailure,"SECOND GEAR SLOT OPENS AT RANK "+PalStoreRules.SecondSlotRank+".",PxMuted);
            }
            if(page>0)PxButton("gear-prev","PREVIOUS",16,398,160,28,()=>ShowGearPicker(fighterId,page-1,gearSlot),_modal);
            if(page+1<pages)PxButton("gear-next","NEXT",544,398,160,28,()=>ShowGearPicker(fighterId,page+1,gearSlot),_modal);
        }

        // ------------------------------------------------------------ the result page

        /// Gear receipts and the existing materials-for-gear choice, never both rewards at once.
        void RunResultLines(PxLines lines,EncounterResult r)
        {
            if(r.Encounter.Kind==EncounterKind.Tutorial)return;
            if(r.PrincessTaken!=null)
                lines.Gap().Add("Gear gained",PxGold).Add(r.PrincessTaken.Name,PxBone).Add(r.PrincessTaken.Describe(),PxMuted)
                    .Add("Added to run gear. Equip on the squad page.",PxMuted);
            if(r.SpoilsOffered!=null)
            {
                var taken=TakenSpoils(r);
                lines.Gap().Add(taken!=null?"Gear gained":"Gear choice - not taken",PxGold);
                if(taken!=null)lines.Add(taken.Name,PxBone).Add(taken.Describe(),PxMuted)
                    .Add("Paid "+r.MaterialsEarned+" materials. Equip on the squad page.",PxMuted);
                else if(_campaign.Spoils!=null&&_campaign.Spoils.Id==r.SpoilsOffered.Id)
                {
                    lines.Add("Keep the +"+r.MaterialsEarned+" materials already at camp, or strip one of "+_campaign.SpoilsChoices.Count+" pieces.",PxBone);
                    foreach(var choice in r.SpoilsChoices)lines.Add(GearClass(choice)+": "+ClassSpells.Name(choice.Bend),PxMuted);
                    if(!_campaign.CanTakeSpoils(out string why))lines.Add("Cannot take gear: "+why,PxRed);
                }
            }
        }

        /// Strip instead of bank (Jacob, 13 Oct 2026): the piece for the materials the fight banked, then the
        /// result again, now saying it was taken.
        void TakeSpoils()
        {
            if(!_campaign.TryTakeSpoils(out string why)){_notice=why;return;}
            PlaySound("strip");
            Save();SetPage(Page.Result);
        }

        /// After a loss you try the same step again (Jacob, 13 Oct 2026).
        string TryAgain()=>_campaign.Target!=null&&_campaign.Target.Id==AlphaHunt.Id?"the hunters come again":
            _campaign.Step>=_campaign.PathLength?"the chief calls you out again":StepName(_campaign)+" again";

        // ------------------------------------------------------------ validation

        /// The Sprint 2 controls through the real buttons (13 Oct 2026), each on its own campaign and save file:
        /// front or back, the spoils taken, the gear picker, and a captured and a killed chief's result.
        IEnumerator SprintTwoScreens()
        {
            _savePath=System.IO.Path.Combine(_capture,"sprint2-screens-save.txt");
            bool spoils=false;
            for(ulong seed=1;seed<=12&&!spoils;seed++)
            {
                _campaign=Campaign.New(seed,new CampaignTuning{EnemyScaleTier1=40});
                if(!CampaignPilot.PlayTutorial(_campaign,PilotStyle.Reference,seed))continue;
                _campaign.StartRun();
                int offer=-1;
                for(int i=0;i<_campaign.Offers.Count&&offer<0;i++)if(_campaign.Offers[i].SquadSize>=2)offer=i;
                if(offer<0)continue;
                TakeOffer(offer);yield return null;
                var first=FindReserve(_squad[0]);var before=first.Row;
                Check(Click("row-"+first.Id),"the squad page's front or back button");yield return null;
                Check(first.Row!=before&&_page==Page.Squad,"front or back moves the fighter through Campaign");
                Capture("s2-01-squad-row");
                Check(Click("start-fight"),"the squad starts the fight");
                _pilot=new CampaignPilot(PilotStyle.Reference,seed);_pilotOn=true;_speed=24;
                int frames=0;while(!_fight.Over&&frames++<1800)yield return null;
                Check(_fight.Over,"the easy path fight ends");
                for(int i=0;i<76;i++)yield return null;
                Check(Click("see-result"),"the easy fight's result opens");yield return null;
                if(!_result.Won||_result.SpoilsOffered==null)continue;
                var piece=_result.SpoilsOffered;int materials=_campaign.Materials;
                Capture("s2-02-spoils-offered");
                Check(Click("take-spoils"),"TAKE THE PIECE on the result");yield return null;
                Check(_modal!=null&&_campaign.Materials==materials&&_campaign.FindGear(piece.Id)==null,
                    "previewing gear does not spend materials");
                Check(Click("strip-0"),"strip the first offered piece");yield return null;
                Check(_campaign.Spoils==null&&_campaign.FindGear(piece.Id)!=null&&_campaign.Materials==materials-_result.MaterialsEarned,
                    "taking the piece gives its materials back");
                Check(_page==Page.Result&&!_buttons.ContainsKey("take-spoils"),"the piece is taken once");
                Capture("s2-03-spoils-taken");
                Check(Click("after-result"),"on from the spoils");yield return null;
                TakeOffer(0);yield return null;
                var wearer=_campaign.Reserve.FirstOrDefault(f=>piece.Fits(f.Class));
                Check(wearer!=null,"a matching class can wear the chosen piece");
                Check(Click("gear-"+wearer.Id),"the squad page's GEAR button");yield return null;
                Check(_modal!=null,"the gear picker opens");
                Capture("s2-04-gear-picker");
                Check(Click("equip-"+piece.Id),"a piece chosen in the gear picker");yield return null;
                Check(_campaign.GearOf(wearer.Id)==piece&&_modal==null&&_page==Page.Squad,"the piece is on the fighter and the squad page is back");
                Capture("s2-05-squad-geared");
                spoils=true;
            }
            Check(spoils,"front or back, the spoils and the gear picker exercised");
            foreach(bool kill in new[]{false,true})
            {
                bool seen=false;
                for(ulong seed=1;seed<=24&&!seen;seed++)
                {
                    var tuning=kill?new CampaignTuning{PathFights=0,ChiefHealthPercent=1}:new CampaignTuning{PathFights=0,ChiefHealthPercent=3000,EnemyScaleTier1=40};
                    _campaign=Campaign.New(seed,tuning);
                    if(!CampaignPilot.PlayTutorial(_campaign,PilotStyle.Reference,seed))continue;
                    _campaign.StartRun();
                    TakeOffer(0);yield return null;
                    Check(Click("start-fight"),"the chief fight starts");
                    _pilot=new CampaignPilot(PilotStyle.Reference,seed);_pilotOn=true;_speed=24;
                    int frames=0;while(!_fight.Over&&frames++<2400)yield return null;
                    Check(_fight.Over,"the chief fight ends");
                    for(int i=0;i<76;i++)yield return null;
                    Check(Click("see-result"),"the chief's result opens");yield return null;
                    if(!_result.Won||(kill?!_result.ChiefKilled:!_result.ChiefCaptured))continue;
                    seen=true;
                    Check(ResultTitle(_result).StartsWith(kill?"You killed ":"You captured ",StringComparison.Ordinal),"the result says captured or killed");
                    if(kill)Check(string.Join(" ",FightLines(_result,false).texts).Contains(_result.PrincessTaken.Name),"a killed chief's result shows his Princess piece");
                    else Check(_result.JoinedFighters.Count==tuning.ChiefFighters,"a captured chief's whole band joins");
                    Capture(kill?"s2-07-chief-killed":"s2-06-chief-captured");
                }
                Check(seen,kill?"a killed chief's result shown":"a captured chief's result shown");
            }
        }
    }
}
