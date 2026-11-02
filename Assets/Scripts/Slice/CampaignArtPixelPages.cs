using System;
using System.Collections;
using System.Collections.Generic;
using KindredSiege.Sim.Learning;
using UnityEngine;

namespace KindredSiege.Slice
{
    public sealed partial class CampaignArtLoop
    {
        // The run's pages on the 2x kit (Jacob, 11 Oct 2026: "Go" to moving the tutorial, the fight choice,
        // the squad and the result onto the kit, so the whole loop looks like the camp and the fight). Same
        // words as the old pages. Every page puts its title at (14,4), PALS top right, its framed panels
        // between y 56 and 392, its main button at the foot in the middle, and the notice under that.
        static readonly Color32 PxDeepRed=new Color32(150,62,50,255),PxRule=new Color32(110,86,52,255);
        static readonly string[] TutorialOrders={"Hold","Fall Back","Focus Fire","Unleash","Chief order"};
        static readonly string[] TutorialWhat={"Brace against pressure.","Open the distance.","Then select a living enemy.",
            "Pressure them; drains your nerve too.","Break a chief and his order is yours for the rest of the run."};
        // Everyone on your side who went down in the last fight, your leader included, so the result's
        // count and names agree (Jacob, 11 Oct 2026: clear the bad news).
        readonly List<string> _lastFallen=new List<string>();
        // Results shown and how many had to scroll; the validation wants none.
        int _resultsShown,_resultScrolls;
        bool _resultUsesLives, _resultUsesKit;
        int _resultLives;

        /// Camp (Jacob, 10 Oct 2026: every run starts and ends here; camp shows what it keeps). The omens for the
        /// next run are picked here (Jacob, 13 Oct 2026), from the button beside the run strip.
        void BuildPixelCamp()
        {
            BuildCampHome();
        }

        /// The first fight's briefing: the five orders with the icons the fight's order bar uses. It says it is
        /// the tutorial, and the fight then walks you through each order (Jacob, 11 Oct 2026; CampaignArtCoach.cs).
        void BuildPixelTutorial()
        {
            PxHeader("Tutorial",PxBone,PxGold,new PxLines().Add("COMMAND THE CLARTH",PxMuted));
            PxButton("camp","PALS",628,6,76,26,ShowCamp);
            PxImage(_screen,"Panel",Plate(_kitPanel,46,688,328),16,64);
            int y=PxWrite(_screen,42,90,318,new PxLines().Add("SQUAD ATTACKS / YOU COMMAND",PxBone));
            for(int i=0;i<TutorialOrders.Length;i++)
            {
                int top=y+6+i*48;
                PxImage(_screen,TutorialOrders[i],_kitIcons[OrderIcons[i]],42,top);
                SetPx(PxLabel(_screen,80,top+6),(i+1)+"  "+TutorialOrders[i],PxGold);
                PxWrite(_screen,80,top+17,280,new PxLines().Add(TutorialWhat[i],PxBone));
            }
            Panel(_screen,380*Px,90*Px,Px,270*Px,PxRule);
            PxWrite(_screen,396,90,282,new PxLines().Add("1-5 = ORDERS",PxGold).Gap()
                .Add("SPACE = PAUSE",PxBone).Gap().Add("TAB / ARROWS = SELECT",PxBone).Gap()
                .Add("ENTER = CONFIRM",PxBone).Gap().Add("ESC = CANCEL",PxMuted).Gap().Add("WIN = CLARTH ALLIES",PxMint));
            PxButton("start-tutorial","COMMAND THE CLARTH",264,398,192,28,StartTutorialFight);
            PxButton("title","MENU",612,398,92,28,ShowTitle);
            _pxNotice=PxLabel(_screen,16,434);
        }

        /// The fight choice. On a tribe's road it is the road page (Jacob, 13 Oct 2026, the path map;
        /// CampaignArtRoad.cs). The hunters have no road: their one fight is a card that reads at a glance, like
        /// a door in Hades (Jacob, 11 Oct 2026: "so much text ... in Hades you literally know looking at one
        /// icon"): its icon, its name and a few short tags. Point at the card for the full text.
        void BuildPixelOffers()
        {
            if(_campaign.Road!=null){BuildPixelRoad();return;}
            var target=_campaign.Target;
            bool hunt=target.Id==AlphaHunt.Id;
            // The named hunter the bounty sends leads the hunt (HunterCompany, 18 Oct 2026).
            var leader=hunt&&_campaign.Offers.Count>0?_campaign.Offers[0].Hunter:null;
            string step=hunt?(leader==null?"One final fight. ":leader.FullName+", rank "+_campaign.Offers[0].HunterRank+" on the hunters' ladder, leads the hunt. "+BountyText()+". ")+
                "Break them to win the run. They never join you.":_campaign.Step>=_campaign.PathLength?"Their chief calls you out. Break him while he stands and his whole tribe joins this run; kill him and you take his Princess gear."
                :"Fight "+(_campaign.Step+1)+" of "+_campaign.PathLength+", then their chief. Pick one.";
            PxHeader(hunt?"The final hunt":target.Name+", tier "+target.Tier,PxBone,PxGold,new PxLines().Add(hunt?"FINAL FIGHT / NO RECRUITS":
                _campaign.Step>=_campaign.PathLength?"SPARE = ALLIES / EXECUTE = GEAR":"FIGHT "+(_campaign.Step+1)+"/"+_campaign.PathLength,PxMuted));
            if(hunt)PxButton("ladder","LADDER",468,60,116,24,ShowLadder);
            PxButton("codex","CODEX",468,6,76,26,ShowCodex);
            PxButton("title","MENU",548,6,76,26,ShowTitle);
            PxButton("camp","PALS",628,6,76,26,ShowCamp);
            PxPathMap(50);
            // Label and value pairs: the small font's comma reads as a full stop ("Materials 20, 5 fighters").
            SetPx(PxLabel(_screen,704,88,2),"Materials: "+_campaign.Materials+"   "+LivesText(),PxMuted);
            var clue=_campaign.Offers.Count==1&&_campaign.Offers[0].Kind==EncounterKind.Chief?RematchClue.For(_campaign,_campaign.Offers[0]):RematchClue.For(_campaign,target.Id);
            if(clue!=null)PxButton("rematch","THEY REMEMBER",588,60,116,24,()=>ShowRematch(clue));
            int count=_campaign.Offers.Count,left=(720-(count*224+(count-1)*8))/2;
            for(int i=0;i<count;i++)
            {
                int index=i,x=left+i*232;var e=_campaign.Offers[i];
                var card=PxImage(_screen,"Card",Plate(_kitPanel,46,224,288),x,104);
                if(e.Kind==EncounterKind.AlphaHunt)SetPx(PxLabel(_screen,x+112,132,1),"FINAL HUNT",PxRed);
                else PxImage(_screen,"Icon",EncounterArt(e.Kind),x+88,126);
                int y=PxCentred(_screen,x+112,172,172,CardTags(e));
                var more=Rect(_screen,"More",0,0,1,1);
                var description=new PxLines().Add(e.Text,PxMuted);
                int available=370-y-10;
                if(PxLineCount(description,172)*11<=available)PxWrite(more,x+26,y+10,172,description);
                else PxWrite(PxScroll(more,x+26,y+10,172,Math.Max(22,available),PxLineCount(description,164)*11,x+200),0,0,164,description);
                more.gameObject.SetActive(false);
                card.raycastTarget=true;
                ShowOnHover(card.gameObject,more.gameObject);
                var choose=PxButton("offer-"+i,"CHOOSE ENCOUNTER",x,398,224,28,()=>TakeOffer(index));
                var focus=choose.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                var select=new UnityEngine.EventSystems.EventTrigger.Entry{eventID=UnityEngine.EventSystems.EventTriggerType.Select};
                select.callback.AddListener(_=>{if(more!=null)more.gameObject.SetActive(true);});focus.triggers.Add(select);
                var deselect=new UnityEngine.EventSystems.EventTrigger.Entry{eventID=UnityEngine.EventSystems.EventTriggerType.Deselect};
                deselect.callback.AddListener(_=>{if(more!=null)more.gameObject.SetActive(false);});focus.triggers.Add(deselect);
            }
            _pxNotice=PxLabel(_screen,16,434);
        }

        /// Lines of pixel text wrapped to max and centred on x, from y down. Returns the y under them.
        int PxCentred(Transform parent,int x,int y,int max,PxLines lines)
        {
            for(int i=0;i<lines.texts.Count;i++)
                foreach(string part in _tinyFont.Wrap(lines.texts[i].ToUpperInvariant(),max,1))
                {
                    if(part.Length>0)SetPx(PxLabel(parent,x,y,1),part,lines.colours[i]);
                    y+=11;
                }
            return y;
        }

        // The fight types' icons (PixelLab, 11 Oct 2026, 48 px on the game's palette), loaded once.
        readonly Dictionary<EncounterKind,Texture2D> _encounterIcons=new Dictionary<EncounterKind,Texture2D>();
        Texture2D EncounterArt(EncounterKind kind)
        {
            if(_encounterIcons.TryGetValue(kind,out Texture2D icon))return icon;
            string id=kind==EncounterKind.HoldTheLine?"holdline":kind==EncounterKind.Duel?"duel":kind==EncounterKind.Trial?"trial":
                kind==EncounterKind.Chief?"chief":"warparty";
            icon=TitleArt("CampaignArt/UI/enc_"+id,false);
            _encounterIcons[kind]=icon;
            return icon;
        }

        static void ShowOnHover(GameObject target,GameObject shown)
        {
            var trigger=target.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            var enter=new UnityEngine.EventSystems.EventTrigger.Entry{eventID=UnityEngine.EventSystems.EventTriggerType.PointerEnter};
            enter.callback.AddListener(_=>shown.SetActive(true));trigger.triggers.Add(enter);
            var exit=new UnityEngine.EventSystems.EventTrigger.Entry{eventID=UnityEngine.EventSystems.EventTriggerType.PointerExit};
            exit.callback.AddListener(_=>shown.SetActive(false));trigger.triggers.Add(exit);
        }

        /// The squad: a plate per fighter, ticked when picked, with FRONT or BACK and GEAR beside it (SquadRow,
        /// 13 Oct 2026), and what waits for them, the fight's bonus goal included.
        void BuildPixelSquad()
        {
            var encounter=SelectedEncounter;
            PxHeader(encounter.Kind==EncounterKind.Duel?"Choose your duelist":"Choose your squad",PxBone,PxGold,new PxLines());
            SetPx(PxLabel(_screen,16,38),Fit(encounter.Title+" / "+_squad.Count+"/"+encounter.SquadSize+" / "+LivesText(),688),PxBone);
            PxButton("camp","PALS",628,6,76,26,ShowCamp);
            var clue=RematchClue.For(_campaign,encounter);
            if(clue!=null)PxButton("rematch","THEY REMEMBER",460,6,156,26,()=>ShowRematch(clue));
            int count=_campaign.Reserve.Count,columns=count>10?2:1,rows=(count+columns-1)/columns,width=columns==1?436:214;
            for(int i=0;i<count;i++)SquadRow(_campaign.Reserve[i],encounter,16+i/rows*222,56+i%rows*32,width);
            StartCoroutine(SquadFootLine(_screen));
            var briefing=new PxLines().Add(encounter.Text,PxBone).Gap();
            if(encounter.Kind==EncounterKind.Beast)BeastBrief(briefing,encounter);
            else {
            RoadSquadLines(briefing,encounter);
            AddSpellBrief(briefing,encounter);
            SquadRunLines(briefing,encounter);
            briefing.Gap().Add("Held favours: "+_campaign.Favours.Count+". Open FAVOURS for ranks and effects.",PxGold);
            // The hunter you face, their traits in the colour of the button that answers each (CampaignArtHunters.cs).
            if(encounter.Hunter!=null){briefing.Gap();AddLines(briefing,HunterLines(encounter.Hunter,encounter.HunterRank,false));}
            briefing.Gap().Add(HunterFight(encounter.Kind)?"The hunters arrive knowing:":_campaign.Target.Name+" remember:",PxGold)
                .Add(PlainHabits(_campaign.MindFor(encounter),true),PxBone);
            if(clue!=null)briefing.Gap().Add(clue.ChiefName+" learned from you:",PxGold).Add(clue.Text,PxBone);
            if(_campaign.TabardForged)
            {
                briefing.Gap().Add("Tabard: "+TabardWearerName(),PxGold)
                    .Add(_squad.Contains(_campaign.TabardWearer)?"Wearer selected. +"+CampaignForge.TabardFear+"% nerve pressure on their hits.":"Wearer not selected. No tabard effect in this squad.",PxMuted);
                PxButton("tabard-picker","TABARD",612,398,92,28,()=>ShowTabardPicker());
            }
            }
            PxPanel(460,56,244,encounter.Kind==EncounterKind.Beast?268:290,SquadDecision(encounter));
            PxButton("squad-details","DETAILS",464,encounter.Kind==EncounterKind.Beast?332:354,232,26,
                ()=>Modal("FIGHT DETAILS",string.Join("\n",briefing.texts)));
            var fight=PxButton("start-fight",encounter.Kind==EncounterKind.Duel&&_squad.Count==0?"PICK A FIGHTER":"FIGHT",264,398,192,28,StartEncounterFight);
            fight.interactable=_squad.Count>0&&_squad.Count<=encounter.SquadSize;
            if(!fight.interactable)SetPx(_pxButtonLabels["start-fight"],encounter.Kind==EncounterKind.Duel?"PICK A FIGHTER":"FIGHT",PxMuted);
            PxButton("back",_campaign.Road!=null?"BACK TO THE ROAD":"BACK",16,398,140,28,()=>SetPage(Page.Offers));
            FavourButton(464,398,140);
            _pxNotice=PxLabel(_screen,16,434);
        }

        /// The result: what the fight earned and cost on the left, what both sides learned on the right.
        /// Everything shows at once (Jacob, 11 Oct 2026: clear the bad news, the learned panel scrolled):
        /// a long result runs its lines tighter instead of scrolling. The path map waits for the next page.
        void BuildPixelResult()
        {
            if(ThemePresentation){BuildCompactResult();return;}
            var r=_result;bool broke=r.Won&&r.Encounter.Kind==EncounterKind.Chief;
            var header=new PxLines();
            if(!broke)header.Add(r.Encounter.Title,PxBone);
            header.Add(ResultLine(r),PxMuted);
            if(_resultUsesLives)header.Add("LIVES "+_resultLives+"/3 / "+(r.RunOver?"RETURN TO CAMP":"FALLEN FIGHTERS RETURN NEXT FIGHT"),_resultLives>0?PxMint:PxRed);
            if(_resultUsesKit)header.Add(r.Streak>0?"WIN STREAK x"+r.Streak+" / MATERIALS BELOW ALREADY INCLUDE MULTIPLIER":"WIN STREAK "+_campaign.Streak+"/3",PxGold);
            ChiefGradeLines(header,r);
            int y=PxHeader(ResultTitle(r),r.Won?PxBone:PxRed,r.Won?PxGold:PxDeepRed,header)+8;
            PxButton("camp","PALS",628,6,76,26,ShowCamp);
            ResultScore(16,y,_resultUsesLives?"FALLEN THIS FIGHT":"DIED",ResultDeaths(r).ToString(),ResultDeaths(r)>0?PxRed:PxBone);
            ResultScore(248,y,"RECRUITED",r.JoinedFighters.Count.ToString(),PxMint);
            bool exchanged=FavourExchanged(r)||TakenSpoils(r)!=null;
            ResultScore(480,y,exchanged?"MATERIALS EXCHANGED":"MATERIALS EARNED",r.MaterialsEarned.ToString(),PxGold);
            y+=58;
            bool whole=PxPanel(16,y,336,392-y,new[]{ResultFighterLines(r,false),ResultFighterLines(r,true)});
            whole&=PxPanel(360,y,344,392-y,new[]{FightLines(r,false)});
            _resultsShown++;
            if(!whole)_resultScrolls++;
            bool offered=PendingSpoils(r);
            bool favour=r.FavourOffered!=null&&_campaign.FavourOffer!=null;
            PxButton("after-result",offered||favour?"KEEP MATERIALS":r.RunOver?"RETURN TO CAMP":"CONTINUE",264,398,192,28,AfterResult);
            if(offered&&favour)
                PxButton("choose-reward","PAL PICK / GEAR",16,398,240,28,ShowRewardChoices);
            else if(offered)
                PxButton("take-spoils",_campaign.SpoilsChoices.Count>1?"CHOOSE CLASS GEAR":"VIEW GEAR",16,398,240,28,ShowSpoilsChoices).interactable=_campaign.CanTakeSpoils(out _);
            else if(favour)PxButton("choose-favour","CHOOSE A PAL FAVOUR",16,398,240,28,ShowFavours);
            PxButton("result-tactics","TACTICS",464,398,140,28,()=>ShowResultTactics(r));
            // The moments that decided it ("fix all 9", 13 Oct 2026; CampaignArtReadouts.cs).
            PxButton("why","WHY",612,398,92,28,()=>ShowFightWhy(r));
            _pxNotice=PxLabel(_screen,16,434);
        }

        /// Roster consequences come from Finish, not every downed battle unit (the leader returns).
        PxLines ResultFighterLines(EncounterResult r,bool together)
        {
            var lines=new PxLines().Add("YOUR FIGHTERS",PxGold);
            bool practice=r.Encounter.Kind==EncounterKind.Tutorial;
            if(!practice&&_resultUsesLives)
            {
                lines.Add("Lives remaining: "+_resultLives+" / "+Campaign.RunLives,_resultLives>0?PxMint:PxRed)
                    .Add("Fallen this fight: "+ResultDeaths(r),PxBone)
                    .Add(r.RunOver?"Run ended. Fighters and gear reset for the next run.":"Your whole squad returns with its gear. Nobody captured.",PxMint);
                var names=new List<string>();foreach(var f in r.Fallen)names.Add(FighterName(f));
                Names(lines,names,PxMuted,together);
                if(!r.Won&&!r.RunOver)lines.Add("Retry this step. Check Grudge on your fighters.",PxOrange);
            }
            else if(!practice)
            {
                lines.Add(r.FightersLeft+(r.RunOver?" left when the run ended.":" available for the next fight."),r.RunOver&&!r.RunWon?PxRed:PxBone);
                lines.Gap().Add("Died: "+ResultDeaths(r),ResultDeaths(r)>0?PxRed:PxMuted);
                var fallen=new List<string>();foreach(var f in r.Fallen)fallen.Add(FighterName(f));
                Names(lines,fallen,PxRed,together);
                if(r.Slain!=null)lines.Add("Killed after defeat: "+FighterName(r.Slain),PxRed)
                    .Add(Takers(r.Encounter)+" take no prisoners.",PxMuted);
                lines.Gap().Add(r.Captured==null?"Captured: none":"Captured by "+Takers(r.Encounter),r.Captured==null?PxMuted:PxRed);
                if(r.Captured!=null)lines.Add(FighterName(r.Captured),PxRed);
                if(!r.Won&&!r.RunOver)
                {
                    lines.Add("Need "+Campaign.MinSquad+" fighters to continue.",PxMuted);
                    int grudges=GrudgeCount(r.Encounter.Tribe.Id);
                    if(grudges>0)lines.Add("Grudge active against "+Takers(r.Encounter)+".",PxOrange);
                }
            }
            else lines.Add("Training fight: no run roster lost.",PxMuted);
            foreach(var fighter in r.RanksLost)lines.Add(fighter.Name+" / RANK LOST / now R"+fighter.Rank,PxOrange);
            lines.Gap().Add("Recruited: "+r.JoinedFighters.Count,r.JoinedFighters.Count>0?PxMint:PxMuted);
            if(r.JoinedFighters.Count>0)
            {
                lines.Add(practice?"The Clarth join you.":"Joined for the rest of this run.",PxMuted);
                var joined=new List<string>();
                foreach(var f in r.JoinedFighters)joined.Add(FighterName(f));
                Names(lines,joined,PxMint,together);
            }
            else if(r.ChiefKilled)lines.Add("Chief killed: his band and order do not join.",PxMuted);
            else if(HunterFight(r.Encounter.Kind))lines.Add("The hunters never join.",PxMuted);
            if(r.AssistUnlocked!=null&&!r.RunOver)
                lines.Gap().Add("Order unlocked: "+r.AssistUnlocked.Name,PxGold).Add("Key 5, once per fight, for this run.",PxMuted);
            return lines;
        }

        /// Materials are banked by Finish; taking spoils exchanges them through Campaign.TryTakeSpoils.
        PxLines FightLines(EncounterResult r,bool together)
        {
            var lines=new PxLines().Add("REWARDS",PxGold);
            bool exchanged=FavourExchanged(r)||TakenSpoils(r)!=null;
            if(r.Encounter.Kind==EncounterKind.Tutorial)lines.Add("Training: no material or gear rewards.",PxMuted);
            else
            {
                lines.Add(exchanged?"Materials exchanged: "+r.MaterialsEarned:"Materials gained: +"+r.MaterialsEarned,exchanged?PxMuted:PxGold);
                lines.Add("At camp now: "+_campaign.Materials+" materials",PxBone);
                if(!r.Won)lines.Add("Earlier earnings are safe at camp.",PxMuted);
                RunResultLines(lines,r);
                FavourResultLines(lines,r);
                if(r.PrincessTaken==null&&r.SpoilsOffered==null)lines.Gap().Add("Gear gained: none",PxMuted);
                RoadResultLines(lines,r);
                // The hunters' ladder and the bounty (CampaignArtHunters.cs, 18 Oct 2026).
                HunterResultLines(lines,r,together);
                if(r.BountyAfter>r.BountyBefore)lines.Gap().Add("Bounty up: "+r.BountyBefore+" to "+r.BountyAfter+". More hunters come, and better ones.",PxOrange);
            }
            BeastResultLines(lines,r);
            if(r.RecipeLearned)lines.Gap().Add("Recipe unlocked",PxGold).Add(CampaignForge.TabardName,PxBone)
                .Add("Not gear yet. The Butcher can forge it at camp for "+CampaignForge.TabardPrice+" materials.",PxMuted);
            if(r.RunOver)lines.Gap().Add("KEPT AT CAMP",PxGold).Add("Materials, trophies, forged pieces, recipes, codex and learned habits.",PxBone)
                .Add("Run fighters, orders, gear and favours reset.",PxMuted);
            return lines;
        }

        void ShowResultTactics(EncounterResult r)
        {
            // Use the existing scrolling popup so full lessons remain available without crowding rewards.
            Modal("TACTICS",string.Join("\n",LearnedLines(r,0).texts));
        }

        static void Names(PxLines lines,List<string> names,Color32 colour,bool together)
        {
            if(!together){foreach(string name in names)lines.Add(name,colour);return;}
            if(names.Count>0)lines.Add(string.Join(" / ",names.ToArray()),colour);
        }

        /// What the fight taught both sides. detail 0 puts a gap between lessons, 1 runs them together in
        /// turns of bone and muted, 2 keeps only what each side does now.
        PxLines LearnedLines(EncounterResult r,int detail)
        {
            bool hunters=HunterFight(r.Encounter.Kind);
            var lines=new PxLines().Add(hunters?"The hunters' tactics":r.Encounter.Tribe.Name+" learned",PxGold).Gap();
            if(hunters)lines.Add("The hunters' habits never change. A hunter who beats you learns a trait instead (see the rewards).",PxMuted);
            else if(r.EnemyLessons.Count==0)lines.Add("Nothing new this fight.",PxMuted);
            Lessons(lines,r.EnemyLessons,detail);
            lines.Gap().Add("Current habits",PxGold).Add(PlainHabits(r.Record.SquadFirst&&r.Encounter.Kind==EncounterKind.Chief?_campaign.ChiefMind(r.Encounter.Tribe.Id):_campaign.Mind(r.Encounter.Tribe.Id),r.Record.SquadFirst),PxBone)
                .Gap().Add("Your fighters learned",PxGold);
            Lessons(lines,r.CultLessons,detail,true);
            // Labelled, or it reads as one more lesson.
            lines.Add("Your habits: "+_campaign.CultMind.HabitsText(),PxBone);
            return lines;
        }

        static void Lessons(PxLines lines,IReadOnlyList<Lesson> lessons,int detail,bool yours=false)
        {
            for(int i=0;i<lessons.Count;i++)
            {
                if(detail==0&&i>0)lines.Gap();
                // Every lesson speaks to the other side; your fighters' are turned round to read from yours (TheirVoice).
                string why=yours?TheirVoice(lessons[i].Why):lessons[i].Why;
                int now=why.IndexOf(" Now ",StringComparison.Ordinal);
                if(detail==2&&now>=0)why=why.Substring(now+1);
                lines.Add(why,detail>0&&i%2==1?PxMuted:PxBone);
            }
        }

        /// Called as the result opens, while the fight is still there to read.
        void RememberFallen()
        {
            _lastFallen.Clear();
            foreach(var u in _fight.Sim.Units)
                if(u.Side==0&&!u.Alive)_lastFallen.Add(u.Id==Campaign.LeaderId?UnitName(u)+", back next fight":UnitName(u));
        }

        /// The notice on the title in the title's own 3x pixel text (11 Oct 2026, in place of the old line):
        /// a save that could not be read, for one.
        IEnumerator TitleNotice(RectTransform screen)
        {
            var area=Rect(screen,"Notice",ListX*TitlePx,276*TitlePx,432*TitlePx,22*TitlePx);
            var made=new List<Texture2D>();
            string shown=null;
            while(_screen==screen)
            {
                if(shown!=_notice)
                {
                    shown=_notice;
                    foreach(Transform child in area)Destroy(child.gameObject);
                    foreach(var texture in made)Destroy(texture);
                    made.Clear();
                    int y=0;
                    foreach(string line in _tinyFont.Wrap((shown??"").ToUpperInvariant(),432,1))
                    {
                        if(line.Length==0)continue;
                        var words=_tinyFont.Render(line,PxRed,PxRed,1,true);
                        made.Add(words);
                        PixelImage(area,"Notice line",words,0,y);
                        y+=11;
                    }
                }
                yield return null;
            }
            foreach(var texture in made)Destroy(texture);
        }
    }
}
