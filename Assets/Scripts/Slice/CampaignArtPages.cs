using System;
using System.Collections.Generic;
using System.Text;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using UnityEngine;
using UnityEngine.UI;

namespace KindredSiege.Slice
{
    public sealed partial class CampaignArtLoop
    {
        Button Btn(string id,string title,float x,float y,float w,Action action,float h=50,Transform parent=null)
        {
            var b=ActionButton(parent==null?_screen:parent,x,y,w,h,title,()=>action(),17);
            b.name=id; _buttons[id]=b; return b;
        }
        void Heading(string title,string subtitle)
        {
            Label(_screen,title,32,57,1376,52,34,Bone,FontStyle.Bold);
            Label(_screen,subtitle,34,112,1372,58,18,Muted);
            Panel(_screen,32,178,1376,2,Bronze);
        }
        RectTransform Scroll(float x,float y,float w,float h,float contentHeight,Transform parent=null)
        {
            var view=Rect(parent==null?_screen:parent,"Scrollable panel",x,y,w,h);
            var bg=view.gameObject.AddComponent<Image>(); bg.color=Hex("29242A");
            view.gameObject.AddComponent<RectMask2D>();
            var content=Rect(view,"Content",0,0,w-20,Mathf.Max(h,contentHeight));
            var scroll=view.gameObject.AddComponent<ScrollRect>(); scroll.viewport=view;scroll.content=content;
            scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=32;
            var track=Panel(view,w-10,0,8,h,Hex("3E302C"));track.raycastTarget=true;
            var bar=track.gameObject.AddComponent<Scrollbar>();bar.direction=Scrollbar.Direction.BottomToTop;
            var handle=Panel(track.transform,0,0,8,30,Bronze);bar.handleRect=handle.rectTransform;bar.targetGraphic=handle;
            scroll.verticalScrollbar=bar;return content;
        }
        void TextPanel(string value,float x,float y,float w,float h,int size=20)
        {
            int lines=value.Split('\n').Length;
            float ch=Mathf.Max(h,lines*(size+10)+Mathf.Ceil(value.Length/(w/(size*.52f)))*(size+7)+32);
            var c=Scroll(x,y,w,h,ch);
            var label=Label(c,value,20,16,w-60,ch-32,size,Bone);
            label.alignment=TextAnchor.UpperLeft;
            Canvas.ForceUpdateCanvases();
            c.sizeDelta=new Vector2(c.sizeDelta.x,Mathf.Max(h,label.preferredHeight+36));
            label.rectTransform.sizeDelta=new Vector2(w-60,c.sizeDelta.y-32);
        }
        string Forces()
        {
            var s=new StringBuilder("YOUR ROSTER\n\n");
            foreach(var f in _campaign.Reserve) s.Append(f.DisplayName).Append("\n");
            s.Append("\nCHIEF ORDERS THIS RUN\n");
            if(_campaign.Assists.Count==0)s.Append("None yet.\n");
            foreach(var a in _campaign.Assists)s.Append(a.Name).Append(", from ").Append(a.ChiefName).Append("\n");
            s.Append("\nYOUR LEARNED HABITS\n").Append(_campaign.CultMind.HabitsText());
            return s.ToString();
        }
        /// One tribe on the run's path (Jacob, 10 Oct 2026: a set path of fights, then its chief, whose
        /// order is yours for the rest of the run once he breaks).
        string TribeInfo(CampaignTribe t)
        {
            return t.Name+", tier "+t.Tier+": "+_campaign.PathLength+" fights, then "+t.ChiefName+
                ".\nBreak him and his order is yours for the rest of the run: "+t.AssistName+"."+
                "\n"+SpellCounter(t.Spell)+
                "\n\nWhat they learned about you\n"+_campaign.Mind(t.Id).HabitsText();
        }
        /// What camp keeps between runs, won or lost (Jacob, 10 Oct 2026: camp shows visible attributes,
        /// and the materials the Pals use always come home).
        string CampKeeps()
        {
            var s=new StringBuilder("CAMP KEEPS\n\n");
            s.Append("Materials: ").Append(_campaign.Materials).Append("\n").Append(ForgeSummary()).Append("\n\n");
            var won=new List<string>();
            foreach(var line in _campaign.Codex)if(line.StartsWith("Joined: ",StringComparison.Ordinal))won.Add(line.Substring(8));
            s.Append("Beast trophies: ").Append(_campaign.Trophies.Count).Append("; permanent beast pieces: ").Append(_campaign.BeastPiecesForged.Count).Append("\n\n");
            s.Append("Codex: ").Append(_campaign.Codex.Count).Append(" entries\nWon over for good: ").Append(won.Count==0?"none yet":string.Join(", ",won.ToArray())).Append("\n\n");
            s.Append("Runs: ").Append(_campaign.RunsStarted).Append(" started, ").Append(_campaign.RunsWon).Append(" won\n");
            s.Append("Hunters' ladder: you are rank ").Append(_campaign.Company.PlayerRank).Append(" of ").Append(_campaign.Company.Ladder.Count).Append("\n\n");
            s.Append("YOUR SQUAD FOR THE NEXT RUN\n");
            foreach(var f in _campaign.Reserve)s.Append(f.DisplayName).Append("\n");
            return s.ToString();
        }
        string NextRun()
        {
            var s=new StringBuilder("THE RUN\n\n");
            var route=Route();
            for(int i=0;i<route.Count;i++)
                s.Append(i+1).Append("  ").Append(route[i].Name).Append(", tier ").Append(route[i].Tier).Append("\n    ").Append(_campaign.PathLength)
                    .Append(" steps on the road, then ").Append(route[i].ChiefName).Append("\n    His order: ").Append(route[i].AssistName).Append("\n");
            s.Append("Then the hunters: one final fight for the bounty on your head. Break them to win the run.\n\n")
                .Append("Break a chief while he stands and his band joins, with his order. Kill him and you take his Princess gear.\n")
                .Append(LivesRun?"Three lives per run. A defeat costs one life; your squad and gear return. "+(_campaign.BeastRollRun?"Rolled beasts cost a life on defeat too. ":"Beast hunts cost no life. ")+"Each run starts with fresh Clarth.":"Lose a fight and they capture one of yours. Under "+Campaign.MinSquad+" fighters the run ends. Each run starts with fresh Clarth.");
            return s.ToString();
        }
        string TheyLearned()
        {
            var s=new StringBuilder("WHAT THE TRIBES LEARNED ABOUT YOU\nThey bring it to every run.\n\n");
            foreach(var t in CampaignTribes.All)
            {
                if(t.IsStarter)continue;
                var mind=_campaign.Mind(t.Id);
                s.Append(t.Name.ToUpperInvariant()).Append("\n").Append(PlainHabits(mind)).Append("\n").Append(mind.Lessons.Count).Append(" lessons so far\n\n");
            }
            s.Append("YOUR FIGHTERS LEARNED\n").Append(_campaign.CultMind.HabitsText());
            return s.ToString();
        }
        void BuildPage()
        {
            switch(_page)
            {
                case Page.Title:
                    BuildTitle();break; // the NOT A HERO style title screen, CampaignArtTitle.cs
                // The run's pages on the 2x pixel grid with the UI kit since 11 Oct 2026, CampaignArtPixelPages.cs.
                case Page.Tutorial:
                    BuildPixelTutorial();break;
                case Page.ChooseTribe:
                    // Camp (Jacob, 10 Oct 2026): every run starts and ends here, won or lost.
                    BuildPixelCamp();break;
                case Page.Offers:
                    BuildPixelOffers();break;
                case Page.Squad:
                    BuildPixelSquad();break;
                case Page.Result:
                    BuildPixelResult();break;
                case Page.Finished:
                    Heading("Every slice tribe has joined you","The current playable slice is complete. Your campaign and learned habits are saved.");
                    TextPanel(Forces(),32,200,660,572,19);
                    var info=new StringBuilder(Summary(_campaign)+"\n\n");
                    foreach(var t in CampaignTribes.All)if(!t.IsStarter)info.Append(TribeInfo(t)).Append("\n\n");
                    TextPanel(info.ToString(),718,200,690,572,19);
                    Btn("title","TITLE",32,790,260,ShowTitle);Btn("codex","CODEX",1148,790,260,ShowCodex);break;
            }
        }
        // A missing save with a good backup still counts: the safe reader opens the backup (11 Oct 2026).
        bool FileExists()=>System.IO.File.Exists(_savePath)||System.IO.File.Exists(_savePath+".bak");
        /// The result page's title: the chief you captured or killed (Jacob, 13 Oct 2026), or how the fight went.
        /// The fight's name goes under it.
        string ResultTitle(EncounterResult r)
        {
            if(r.Encounter.Kind==EncounterKind.Beast)return r.BeastKilled?"BEAST SLAIN":r.BeastFled?"BEAST FLED":"HUNT LOST";
            if(r.Won&&r.Hunter!=null)return (r.HunterSlain?"You killed ":"You beat ")+r.Hunter.Name;
            if(r.Won&&r.Encounter.Kind==EncounterKind.Chief)
            {
                var chief=_campaign.ChiefOf(r.Encounter.Tribe.Id);
                return (r.ChiefKilled?"You killed ":r.ChiefCaptured?"You captured ":"You broke ")+(chief==null?r.Encounter.Tribe.ChiefName:chief.Name);
            }
            return Outcome(r);
        }
        string ResultLine(EncounterResult r)
        {
            if(r.Encounter.Kind==EncounterKind.Beast&&r.Encounter.BeastTier>0)
                return (r.Won?"Node taken. Trophies and materials banked.":r.RunOver?"Run over. Earlier trophies stay at camp.":"Life lost. The beast still waits on its node.")+
                    " Tier "+r.BeastTierAfter+" / lives "+r.LivesLeft+"/3. Nobody captured.";
            if(r.Encounter.Kind==EncounterKind.Beast)return (r.BeastEscaped?"Last stand escape. ":r.BeastFled?"Time ran out; it fled. ":"")+(r.RunOver?"Run over. Earlier trophies stay at camp.":r.Won?"Trophies banked. Your road step is unchanged.":"No trophies. Nobody captured. Your road step is unchanged.");
            if(r.TutorialWon)return "The Clarth join you. Camp is open: every run starts and ends there.";
            if(r.Encounter.Kind==EncounterKind.Tutorial)return "The tutorial starts again.";
            if(r.RunOver)return (r.RunWon?"Run won.":_resultUsesLives?"Run over: no lives left.":"Run over: fewer than "+Campaign.MinSquad+" fighters left.")+
                " Home to camp: materials, the codex and what the tribes learned stay. The run's tribes, orders, gear and fighters reset.";
            string chief=r.ChiefKilled?"He fell, so nobody joins. His Princess piece is yours. ":r.ChiefCaptured?"His whole band and his order are yours for the rest of the run. ":"";
            if(r.HuntComing)return chief+"The hunters come next for the bounty on your head. One final fight; they never join you.";
            if(r.NextTribe!=null)return chief+"On to "+r.NextTribe.Name+".";
            if(r.ChiefReady)return "Their chief calls you out next.";
            if(!r.Won)return (r.Slain!=null?"They killed "+r.Slain.Name+" after defeat. ":r.Captured!=null?"They captured "+r.Captured.Name+". ":"")+"Your run goes on: "+TryAgain()+".";
            return "Your run goes on: "+StepName(_campaign)+" next.";
        }
        void ShowCodex()
        {
            PlaySound("codex");
            Modal("CODEX",ChiefCodexText()+string.Join("\n\n",_campaign.Codex));
            PxButton("fight-history","FIGHT HISTORY",16,398,200,28,()=>ShowFightHistory(),_modal);
            PxButton("beast-trophies","BEAST TROPHIES",484,398,220,28,()=>ShowTrophies(),_modal);
        }
        void ShowMissing()=>Modal("ART STATUS","IN THIS BUILD\nThe approved tribe fighters and all six hunter roles use their battle sheets. The Bloodridge and Stoneveil priests and all four road tribes' champions use delivered clips; animation refinement is still pending. The moving Bloodridge burial court and selected ship-bay title are included. Combat has step-in lunges, recoil, hit feedback and historical playback.\n\nSTILL PENDING\nSome character clips still need refinement. The leader's existing battle clips still await a final verdict; separate parry and movement sheets are not bound.\n\nThe four Pals play their camp work loops. The camp environment, other stage replacements, title dive, spell effect art and Lava Sun Tabard appearance remain unfinished. Rejected studies are not presented as approved art.\n\nSOUND\nSelected combat and menu effects, map/battle/chief music and chief voice lines are included. F8 opens sound controls; M toggles mute. Butcher-specific effect sounds await handback.");
        /// A popup on the 2x kit (11 Oct 2026): a solid page over the one behind, the title in the display
        /// font, the text in a frame (it scrolls only when longer than the frame, as a long codex will), and CLOSE.
        void Modal(string title,string content)
        {
            LoadPixelKit();
            _modalPreviousSelection=UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject;
            _modal=Rect(_root,title,0,0,W,H);
            Panel(_modal,0,0,W,H,Ink).raycastTarget=true;
            var heading=_logoFont.Render(title.ToUpperInvariant(),PxBone,PxGold,1,true);_screenTextures.Add(heading);
            PxImage(_modal,"Title",heading,14,4);
            PxPanel(16,44,688,348,PxTextLines(content),_modal);
            PxButton("modal-close","CLOSE",264,398,192,28,CloseModal,_modal);
            SelectControl("modal-close");
        }
        void CloseModal()
        {
            if(_modal==null)return;
            _modal.gameObject.SetActive(false);
            foreach(var button in _modal.GetComponentsInChildren<Button>(true))
                if(_buttons.TryGetValue(button.name,out Button registered)&&registered==button)
                { _buttons.Remove(button.name);_pxButtonLabels.Remove(button.name); }
            FreeModalTextures(_modal);
            Destroy(_modal.gameObject);_modal=null;
            _buttons.Remove("modal-close");_pxButtonLabels.Remove("modal-close");
            RestoreSelection(_modalPreviousSelection);_modalPreviousSelection=null;
        }
    }
}
