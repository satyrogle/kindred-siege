using System;
using System.Collections;
using System.Collections.Generic;
using KindredSiege.Sim;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KindredSiege.Slice
{
    public sealed partial class CampaignArtLoop
    {
        // The Sprint 3 screens (Jacob, 13 Oct 2026: "Do sprint 3 screens"; DESIGN_LOCKS, "Sprint 3 calls"): the tribe's
        // road seen whole in place of the fight cards, its events, the bonus goals and the omens picked at camp. Every
        // rule stays in Campaign; this file shows them and passes the clicks on.

        // The road on the 2x grid: its rows left to right in three lanes, the chief's plate on the right, and five lines
        // under it for the node you point at. Starting values.
        const int RoadTop=63,RoadNodeH=84,RoadLaneGap=8,RoadChiefW=96,RoadDetailY=340,RoadLinksW=688;
        const int RoadHeight=CampaignRoad.Lanes*RoadNodeH+(CampaignRoad.Lanes-1)*RoadLaneGap;
        // A node you cannot reach any more, one ahead of you, one you can take now, and the one you stand on.
        const int NodeShut=0,NodeAhead=1,NodeLit=2,NodeHere=3;
        // What the lines under the road show: the node pointed at, else the one selected, else the key.
        PxLines _roadHover,_roadSelected;
        PxText[] _roadDetailSlots;
        readonly Dictionary<PxLines, (string risk, string reward)> _roadDecisions = new Dictionary<PxLines, (string, string)>();
        PxLines RoadDecision(PxLines lines, string risk, string reward)
        { _roadDecisions[lines]=(risk,reward); return lines; }
        // The fight's bonus goal in its top bar, made on the fight page's first refresh.
        PxText _pxGoal;RectTransform _pxGoalScreen;
        // Why an event choice was refused, inside its scene.
        PxText _eventWhy;
        // Where the run strip ends (PxPathMap), for the check that it fits beside the camp's OMENS button.
        int _pathMapEnd;

        // ------------------------------------------------------------ the road page

        /// The road (Jacob, 13 Oct 2026, the path map): this tribe's road seen whole, where you stand on it, and its
        /// chief at the end. Lit nodes are the ones you can take now: a fight opens its squad page, an event its scene.
        /// Point at any node to read it in the lines under the road.
        void BuildPixelRoad()
        {
            var road=_campaign.Road;var target=_campaign.Target;
            bool chiefNext=_campaign.Step>=road.Rows;
            _roadHover=_roadSelected=null; _roadDecisions.Clear();
            PxHeader(target.Name,PxBone,PxGold,new PxLines());
            PxWrite(_screen,16,38,440,new PxLines().Add("TIER "+target.Tier+" / "+(chiefNext?"CHIEF":"STEP "+(_campaign.Step+1)+"/"+road.Rows),PxMuted));
            SetPx(PxLabel(_screen,704,38,2),"Materials: "+_campaign.Materials+"   "+LivesText(),PxMuted);
            if(_campaign.Omens.Count>0)SetPx(PxLabel(_screen,16,49),"Omens: "+OmenNames()+". "+OmenGains(),PxGold);
            // The bounty on your head (Campaign.Bounty, 18 Oct 2026): from 2 the hunters wait on the road.
            SetPx(PxLabel(_screen,704,49,2),(KitRun?"STREAK "+_campaign.Streak+"/3   ":"")+BountyText(),_campaign.Bounty>=BountyRules.AmbushFrom?PxRed:PxMuted);
            FavourButton(468,6,76);
            PxButton("codex","CODEX",276,6,68,26,ShowCodex);
            PxButton("title","MENU",548,6,76,26,ShowTitle);
            PxButton("camp","PALS",628,6,76,26,ShowCamp);
            var clue=_campaign.Offers.Count==1&&_campaign.Offers[0].Kind==EncounterKind.Chief?RematchClue.For(_campaign,_campaign.Offers[0]):RematchClue.For(_campaign,target.Id);
            if(clue!=null)PxButton("rematch","THEY REMEMBER",348,6,116,26,()=>ShowRematch(clue));

            // Lit: the nodes you can take now. Ahead: every node a lit one still leads to. Here: the node you won or took.
            var next=_campaign.NextNodes;
            var lit=new HashSet<RoadNode>(next);
            RoadNode here=_campaign.Step>0?road.Node(_campaign.Step-1,_campaign.RoadLane):null;
            var ahead=new HashSet<RoadNode>(next);
            for(int r=_campaign.Step;r+1<road.Rows;r++)
                for(int l=0;l<CampaignRoad.Lanes;l++)
                {
                    var n=road.Node(r,l);
                    if(!ahead.Contains(n))continue;
                    foreach(int lane in n.Next)ahead.Add(road.Node(r+1,lane));
                }
            int State(RoadNode n)=>lit.Contains(n)?NodeLit:n==here?NodeHere:ahead.Contains(n)?NodeAhead:NodeShut;

            int rows=road.Rows,chiefX=704-RoadChiefW;
            int nodeW=rows==0?0:Math.Min(120,(chiefX-16-rows*20)/rows),gap=rows==0?0:(chiefX-16-rows*nodeW)/rows;
            int X(int row)=>16+row*(nodeW+gap);
            int Y(int lane)=>RoadTop+lane*(RoadNodeH+RoadLaneGap);

            // The links, dotted on the road's own pixels: dim where the road is shut to you, brown where it is still open,
            // gold from where you stand to where you can go. The dim go first so the brighter are drawn over them.
            var pixels=new Color32[RoadLinksW*RoadHeight];
            for(int pass=0;pass<3;pass++)
                for(int r=0;r<rows;r++)
                    for(int l=0;l<CampaignRoad.Lanes;l++)
                    {
                        var from=road.Node(r,l);int a=State(from);
                        int x0=X(r)+nodeW+3-16,y0=Y(l)+RoadNodeH/2-RoadTop;
                        if(r+1==rows)
                        {
                            int toChief=a==NodeHere&&chiefNext?2:a==NodeLit||a==NodeAhead?1:0;
                            if(toChief==pass)Dots(pixels,x0,y0,chiefX-3-16,y0,pass);
                            continue;
                        }
                        foreach(int lane in from.Next)
                        {
                            int b=State(road.Node(r+1,lane));
                            int kind=a==NodeHere&&b==NodeLit?2:(a==NodeLit||a==NodeAhead)&&b==NodeAhead?1:0;
                            if(kind==pass)Dots(pixels,x0,y0,X(r+1)-3-16,Y(lane)+RoadNodeH/2-RoadTop,pass);
                        }
                    }
            var links=new Texture2D(RoadLinksW,RoadHeight,TextureFormat.RGBA32,false){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
            links.SetPixels32(pixels);links.Apply();_screenTextures.Add(links);
            PxImage(_screen,"Road links",links,16,RoadTop);

            for(int r=0;r<rows;r++)
                for(int l=0;l<CampaignRoad.Lanes;l++)
                {
                    var node=road.Node(r,l);int state=State(node),offer=-1;Encounter e=null;
                    if(state==NodeLit&&node.Kind==RoadNodeKind.Fight)
                        for(int i=0;i<_campaign.Offers.Count;i++)if(_campaign.Offers[i].Node==node){offer=i;e=_campaign.Offers[i];}
                    DrawRoadNode(node,state,offer,e,X(r),Y(l),nodeW);
                }
            DrawRoadChief(target,chiefNext,chiefX);
            var key=new PxLines().Add("Nodes in gold corners are the ones you can take now. Point at any node to read it.",PxBone)
                .Add(_campaign.PalRoadRun?"Pal victory: choose one Pal pick OR gear OR keep materials. A goal improves gear by one tier.":"A Pal offers a favour instead of gear. A goal improves a gear drop by one tier.",PxGold)
                .Add("? is an event: a short scene with a choice. Some choices cost materials.",PxMuted)
                .Add(LivesRun?"LIVES "+_campaign.Lives+"/3. Squad returns after fights. "+(_campaign.BeastRollRun?"Every defeat costs a life, including beasts.":"A defeat costs a life; beast hunts are exempt."):"Lose a fight and they capture one of yours; you try that step again.",PxRed);
            if(_campaign.StagedBloodridgeRoad)key.Add("NEXT: "+RoadLesson(Math.Min(4,_campaign.Step+1)),PxMint);
            else if(_roadReview)key.Add(KitRun?KitStatus:RoadIssue,PxOrange);
            bool hunted=false;
            foreach(var n in road.All)if(_campaign.IsAmbush(n))hunted=true;
            if(hunted)key.Add("! is an ambush: a hunter waits there. Beat them to climb the hunters' ladder, or go round.",PxRed);
            RoadDecision(key, LivesRun?"DEFEAT = -1 LIFE / "+_campaign.Lives+" LEFT":"DEFEAT = FIGHTERS LOST",
                _campaign.PalRoadRun?"PAL PICK / GEAR / MATERIALS":"GEAR / PAL PICK / MATERIALS");
            PxButton("road-details","DETAILS",612,394,92,20,()=>Modal("ROAD DETAILS",string.Join("\n",(_roadHover??_roadSelected??key).texts)));
            StartCoroutine(RoadDetailLines(_screen,key));
            BeastRoadButton();
            _pxNotice=PxLabel(_screen,_campaign.BeastOffer!=null?250:16,_campaign.BeastOffer!=null?440:434);
        }

        /// One node: a button in gold corners when you can take it, a plate otherwise, darker the further it is out of
        /// reach. Its icon, its name, what it drops and its goal.
        void DrawRoadNode(RoadNode node,int state,int offer,Encounter e,int x,int y,int w)
        {
            Transform parent;
            if(state==NodeLit)
            {
                GoldCorners(x,y,w,RoadNodeH);
                int lane=node.Lane;
                var button=node.Kind==RoadNodeKind.Event?PxButton("event-"+lane,"",x,y,w,RoadNodeH,()=>ShowEvent(lane))
                    :PxButton("offer-"+offer,"",x,y,w,RoadNodeH,()=>TakeOffer(offer));
                parent=button.transform;
            }
            else
            {
                var plate=PxImage(_screen,"Road node",Plate(_kitButton,12,w,RoadNodeH),x,y);
                plate.color=state==NodeHere?new Color(.72f,.95f,.8f):state==NodeAhead?new Color(.62f,.6f,.66f):new Color(.42f,.42f,.46f);
                plate.raycastTarget=true;
                parent=plate.transform;
            }
            bool dim=state==NodeShut;
            // A node the hunters hold (Campaign.IsAmbush, 18 Oct 2026) shows a red ! and the hunter waiting there.
            bool ambush=_campaign.IsAmbush(node);
            bool beast=node.Beast!=BeastKit.None;
            bool champion=_campaign.ChampionOn(node);
            bool pal=HasPalReward(node);
            var hunter=ambush?_campaign.HunterOnRoad:null;
            if(node.Kind==RoadNodeKind.Event||ambush||beast)
            {
                var mark=_logoFont.Render(beast?"B":ambush?"!":"?",dim?PxMuted:ambush?PxRed:PxBone,dim?PxMuted:ambush?PxDeepRed:PxGold,1,true);_screenTextures.Add(mark);
                PxImage(parent,beast?"Beast symbol / ART PENDING":ambush?"Ambush":"Event",mark,(w-mark.width)/2,5+(48-mark.height)/2);
            }
            else
            {
                var icon=PxImage(parent,"Icon",EncounterArt(beast?EncounterKind.Beast:node.Fight),(w-48)/2,1);
                if(dim)icon.color=new Color(.5f,.5f,.5f);
                else if(state==NodeAhead)icon.color=new Color(.8f,.8f,.8f);
            }
            string lesson=node.Row==0?"COMBOS":node.Row==1?"DEFENCE":node.Row==2?"BOOSTS":"CARRIERS";
            SetPx(PxLabel(parent,w/2,49,1),Fit(beast?BeastKits.Name(node.Beast):ambush?"Ambush":_campaign.StagedBloodridgeRoad?lesson:NodeTitle(node),w-16),ambush&&!dim?PxRed:state==NodeLit?PxGold:state==NodeHere?PxMint:dim?PxMuted:PxBone);
            bool fight=node.Kind==RoadNodeKind.Fight;
            SetPx(PxLabel(parent,w/2,59,1),Fit(beast?"TIER "+_campaign.BeastTier(node.Beast):ambush?(hunter==null?"Hunters":hunter.Name):pal?FavourRules.PalName(node.Pal):fight?_campaign.DropOf(node)+" gear":"Event",w-16),
                dim||state==NodeHere?PxMuted:ambush?PxBone:fight?PxMint:PxLilac);
            string third=state==NodeHere?"You are here":beast?"TROPHIES":ambush?(hunter==null?"":"Rank "+_campaign.Company.Rank(hunter)):champion?"CHAMPION":pal?(_campaign.NodeFlavour(node)==Augment.None?"Pal favour":PalPickName(_campaign.NodeFlavour(node))):GoalShort(node.Objective);
            if(third.Length>0)SetPx(PxLabel(parent,w/2,69,1),Fit(third,w-16),state==NodeHere?PxMint:dim?PxMuted:ambush?PxRed:PxGold);
            var detail=ambush?AmbushDetail(state,e):NodeDetail(node,state,e);
            if(ambush){detail.texts.Insert(1,NodeEnemyPreview(node,e));detail.colours.Insert(1,PxBone);}
            RoadHover(parent.gameObject,detail);
        }

        /// The chief's plate at the end of the road, the whole height of it: lit when he calls you out.
        void DrawRoadChief(CampaignTribe tribe,bool chiefNext,int x)
        {
            Transform parent;
            if(chiefNext)
            {
                GoldCorners(x,RoadTop,RoadChiefW,RoadHeight);
                parent=PxButton("offer-0","",x,RoadTop,RoadChiefW,RoadHeight,()=>TakeOffer(0)).transform;
            }
            else
            {
                var plate=PxImage(_screen,"Chief",Plate(_kitButton,12,RoadChiefW,RoadHeight),x,RoadTop);
                plate.color=new Color(.8f,.78f,.84f);plate.raycastTarget=true;
                parent=plate.transform;
            }
            var chief=_campaign.ChiefOf(tribe.Id);
            int y=RoadHeight/2-60;
            PxImage(parent,"Icon",EncounterArt(EncounterKind.Chief),(RoadChiefW-48)/2,y);
            var lines=new PxLines().Add("Their chief",chiefNext?PxGold:PxBone).Add(chief==null?tribe.ChiefName:chief.Name,PxBone);
            if(chief!=null)lines.Add(chief.Title,PxMuted);
            lines.Gap().Add("Break him:",PxMuted).Add("tribe joins",PxMint).Add("Kill him:",PxMuted).Add("Princess gear",PxMint);
            PxCentred(parent,RoadChiefW/2,y+54,RoadChiefW-16,lines);
            Encounter e=chiefNext&&_campaign.Offers.Count>0?_campaign.Offers[0]:null;
            RoadHover(parent.gameObject,ChiefDetail(tribe,chief,e));
        }

        // Gold corners round a node you can take now, three pixels out from its plate.
        void GoldCorners(int x,int y,int w,int h)
        {
            const int arm=10;
            int left=x-3,top=y-3,right=x+w+2,bottom=y+h+2;
            foreach(var (cx,cy,dx,dy) in new[]{(left,top,1,1),(right,top,-1,1),(left,bottom,1,-1),(right,bottom,-1,-1)})
            {
                Panel(_screen,(dx>0?cx:cx-arm+1)*Px,cy*Px,arm*Px,Px,PxGold);
                Panel(_screen,cx*Px,(dy>0?cy:cy-arm+1)*Px,Px,arm*Px,PxGold);
            }
        }

        // A dotted link on the road's pixels, two by two every six; y counts down from the road's top.
        static void Dots(Color32[] pixels,int x0,int y0,int x1,int y1,int kind)
        {
            Color32 c=kind==2?PxGold:kind==1?PxRule:new Color32(58,48,44,255);
            int steps=Math.Max(1,Math.Max(Math.Abs(x1-x0),Math.Abs(y1-y0)));
            for(int t=0;t<=steps;t+=6)
            {
                int x=x0+(x1-x0)*t/steps,y=y0+(y1-y0)*t/steps;
                for(int dy=0;dy<2;dy++)
                    for(int dx=0;dx<2;dx++)
                    {
                        int px=x+dx,py=y+dy;
                        if(px>=0&&py>=0&&px<RoadLinksW&&py<RoadHeight)pixels[(RoadHeight-1-py)*RoadLinksW+px]=c;
                    }
            }
        }

        string NodeTitle(RoadNode node)
        {
            if(node.Beast!=BeastKit.None)return BeastKits.Name(node.Beast);
            if(node.Kind==RoadNodeKind.Event)return node.Event.Title;
            switch(node.Fight)
            {
                case EncounterKind.HoldTheLine:return "Hold the line";
                case EncounterKind.Duel:return "Champion duel";
                case EncounterKind.Trial:return _campaign.Target.TrialName;
                default:return "War party";
            }
        }

        /// The materials a won road fight or chief brings this run, omens counted (Campaign.Finish).
        int RoadMaterials(bool chief)
        {
            int materials=chief?_campaign.Tuning.MaterialsPerChief:_campaign.Tuning.MaterialsPerFight;
            return materials*(100+OmenRules.MaterialsPercentEach*_campaign.Omens.Count)/100;
        }

        /// A node in full, for the lines under the road.
        PxLines NodeDetail(RoadNode node,int state,Encounter e)
        {
            var lines=new PxLines();
            string where=state==NodeLit?"":state==NodeHere?" You stand here.":state==NodeAhead?" Ahead on the road.":" Out of your reach now.";
            if(node.Beast!=BeastKit.None)
                return RoadDecision(lines.Add(BeastKits.Name(node.Beast)+" / TIER "+_campaign.BeastTier(node.Beast)+where,PxBone)
                    .Add("Win: take this node, bank trophies and materials, raise this beast's camp tier (maximum 50).",PxMint)
                    .Add("Lose: one life. Fallen fighters lose their top rank; beast tier stays. No capture.",PxRed),
                    "ROAD BEAST / TIER "+_campaign.BeastTier(node.Beast),"TROPHIES + MATERIALS / DEFEAT = -1 LIFE");
            if(node.Kind==RoadNodeKind.Event)
            {
                var choices=new List<string>();
                foreach(var c in node.Event.Choices)choices.Add(c.Label+(c.Cost>0?" ("+c.Cost+" materials)":""));
                return RoadDecision(lines.Add(node.Event.Title+": an event."+where,PxGold).Add(node.Event.Text,PxBone)
                    .Add("Choices: "+string.Join(" / ",choices.ToArray())+".",PxMuted),
                    node.Event.Title+": read the scene before choosing. Some choices cost materials.",string.Join(" / ",choices));
            }
            var tier=_campaign.DropOf(node);
            string head=NodeTitle(node)+".";
            if(e!=null)head+=" Squad "+e.SquadSize+", enemies "+e.EnemyUnits+"."+(e.EnemyCasts?" They cast.":"")+(e.NoFallBack?" No Fall Back.":"");
            lines.Add(head+where,PxGold);
            if(_campaign.ChampionOn(node))lines.Add(e!=null?e.Text:"CHAMPION",PxGold);
            lines.Add(NodeEnemyPreview(node,e),PxBone);
            lines.Add(HasPalReward(node)?"Win: a favour from "+FavourRules.PalName(node.Pal)+", or +"+RoadMaterials(false)+" materials.":
                "Win: a "+tier+" piece of their gear, or +"+RoadMaterials(false)+" materials.",PxMint);
            if(_campaign.NodeFlavour(node)!=Augment.None)lines.Add("BUTCHER: "+PalPickName(_campaign.NodeFlavour(node))+" combos on this road.",PxLilac);
            AddSpellBrief(lines,e);
            if(node.Objective!=FightObjective.None)
                lines.Add("Bonus goal: "+FightObjectives.Text(node.Objective)+". Meet it and their piece is "+TierUp(tier)+".",PxGold);
            if(e!=null)
            {
                int grudges=GrudgeCount(e.Tribe.Id);
                if(grudges>0)lines.Add("Grudge: "+grudges+" of yours hit them "+Quirks.GrudgePercent+"% harder.",PxOrange);
            }
            return RoadDecision(lines, NodeTitle(node)+". "+(e!=null?"Enemies "+e.EnemyUnits+". "+(e.NoFallBack?"No Fall Back. ":"")+LossWarning(e):"Ahead: inspect its squad when reachable."),
                HasPalReward(node)?"Pal favour or "+RoadMaterials(false)+" materials.":tier+" gear or "+RoadMaterials(false)+" materials." );
        }

        PxLines ChiefDetail(CampaignTribe tribe,CampaignChief chief,Encounter e)
        {
            string name=chief==null?tribe.ChiefName:chief.FullName;
            if(Campaign.IsRaidChief(tribe,EncounterKind.Chief,Campaign.RulesVersion))
            {
                var raid=new PxLines().Add(name+" / SOLO RAID CHIEF",PxGold)
                    .Add("Your squad wears down his health. Answer his moves to empty his posture.",PxBone)
                    .Add("He recovers posture faster while healthy. Hold is free; mashing will not renew parry timing.",PxBone)
                    .Add("Blue cast bars: Focus him to interrupt. Eclipse: parry or Fall Back.",PxBlue)
                    .Add("At zero posture he kneels. Spare recruits his tribe; Execute takes his piece.",PxMint);
                if(e!=null)raid.Add(LossWarning(e),PxRed);
                return RoadDecision(raid,name+" fights alone.","Spare: tribe + "+tribe.AssistName+". Execute: chief gear.");
            }
            var lines=new PxLines().Add(name+", their chief."+(e!=null?" Squad "+e.SquadSize+", enemies "+e.EnemyUnits+".":" He waits at the end of the road."),PxGold)
                .Add(EnemyPreview(_campaign.ChiefSquad,EncounterKind.Chief,tribe,true),PxBone)
                .Add("Break the chief's nerve, then choose without a timer. Spare: the band joins this run with "+tribe.AssistName+".",PxMint)
                .Add("Kill him: you take his Princess gear and nobody joins. Win: +"+RoadMaterials(true)+" materials.",PxMint);
            lines.Add(SpellCounter(tribe.Spell,true),OrderColour(ChiefSpells.Answer(tribe.Spell)));
            if(e!=null)lines.Add(LossWarning(e)+" The chief calls you out again.",PxRed);
            return RoadDecision(lines,name+". "+(e!=null?"Enemies "+e.EnemyUnits+". "+LossWarning(e):"Chief fight after the road."),
                "Spare: tribe + "+tribe.AssistName+". Execute: chief gear. Base materials: "+RoadMaterials(true)+"; Glory may add more.");
        }

        /// "no losses", for a node.
        static string GoalShort(FightObjective goal)
        {
            switch(goal)
            {
                case FightObjective.NobodyFalls:return "no losses";
                case FightObjective.Quick:return "win in "+FightObjectives.QuickTicks/SimClock.TicksPerSecond+"s";
                case FightObjective.BreakWithFour:return "break at "+FightObjectives.BreakStanding;
                default:return string.Empty;
            }
        }

        static GearTier TierUp(GearTier tier)=>tier==GearTier.Pauper?GearTier.Mid:GearTier.Princess;

        void RoadHover(GameObject target,PxLines detail)
        {
            var trigger=target.AddComponent<EventTrigger>();
            AddTrigger(trigger,EventTriggerType.PointerEnter,()=>_roadHover=detail);
            AddTrigger(trigger,EventTriggerType.PointerExit,()=>{if(_roadHover==detail)_roadHover=null;});
            AddTrigger(trigger,EventTriggerType.Select,()=>_roadSelected=detail);
            AddTrigger(trigger,EventTriggerType.Deselect,()=>{if(_roadSelected==detail)_roadSelected=null;});
        }

        static void AddTrigger(EventTrigger trigger,EventTriggerType type,Action action)
        {
            var entry=new EventTrigger.Entry{eventID=type};
            entry.callback.AddListener(_=>action());
            trigger.triggers.Add(entry);
        }

        /// The five lines under the road: the node pointed at, else the one selected, else the key.
        IEnumerator RoadDetailLines(RectTransform screen,PxLines key)
        {
            var slots=new PxText[10];
            _roadDetailSlots=slots;
            for(int i=0;i<slots.Length;i++)slots[i]=PxLabel(screen,i<5?16:364,RoadDetailY+(i%5)*10);
            PxLines shown=null;
            while(_screen==screen)
            {
                var now=_roadHover??_roadSelected??key;
                if(now!=shown)
                {
                    shown=now;
                    if(!_roadDecisions.TryGetValue(now,out var choice))
                        choice=(now.texts.Count>0?now.texts[0]:"Unknown risk","Read DETAILS for this encounter.");
                    for(int col=0;col<2;col++)
                    {
                        int slot=col*5;
                        SetPx(slots[slot++],col==0?"RISK":"REWARD",col==0?PxOrange:PxMint);
                        foreach(string part in _tinyFont.Wrap((col==0?choice.risk:choice.reward).ToUpperInvariant(),330,1))
                            if(slot<col*5+5&&part.Length>0)SetPx(slots[slot++],part,PxBone);
                        while(slot<col*5+5)SetPx(slots[slot++],"",PxBone);
                    }
                }
                yield return null;
            }
        }

        // ------------------------------------------------------------ events

        /// An event's scene (Jacob, 13 Oct 2026: events are map nodes with short scenes): its text and its choices with
        /// their prices. A choice you cannot pay for says why and changes nothing; CLOSE walks away without choosing.
        void ShowEvent(int lane)
        {
            RoadNode node=null;
            foreach(var n in _campaign.NextNodes)if(n.Lane==lane&&n.Kind==RoadNodeKind.Event)node=n;
            if(node==null)return;
            PlaySound("node_pick");
            CloseModal();
            var scene=node.Event;
            Modal(scene.Title,"");
            int y=PxWrite(_modal,42,68,636,new PxLines().Add(scene.Text,PxBone).Gap().Add("Materials: "+_campaign.Materials,PxMuted));
            for(int i=0;i<scene.Choices.Count;i++)
            {
                int index=i;var c=scene.Choices[i];bool afford=_campaign.Materials>=c.Cost;
                var button=PxButton("choice-"+i,"",42,y+10+i*36,636,30,()=>TakeEvent(lane,index),_modal);
                if(!afford)button.targetGraphic.color=new Color(.62f,.6f,.66f);
                // The shrine's price is what answers, so it is not shown as free.
                string price=c.Effect==EventEffect.Pray?"price unknown":c.Cost>0?c.Cost+" materials":"free";
                SetPx(PxLabel(button.transform,16,10),new[]{c.Label,price},new[]{afford?PxBone:PxMuted,afford?PxGold:PxRed});
            }
            _eventWhy=PxLabel(_modal,42,y+18+scene.Choices.Count*36);
            SelectControl("choice-0");
        }

        /// The choice through Campaign.TryTakeEvent. Taken, the road moves on under the scene, which then says what happened.
        void TakeEvent(int lane,int choice)
        {
            if(!_campaign.TryTakeEvent(lane,choice,out EventOutcome outcome,out string why))
            {
                if(_eventWhy!=null)SetPx(_eventWhy,why,PxRed);
                return;
            }
            Save();
            PlaySound("event");
            Log("Event "+outcome.Node.Event.Id+": "+outcome.Choice.Label+". "+outcome.Text);
            _eventWhy=null;
            SetPage(Page.Offers);
            var lines=new PxLines().Add(outcome.Choice.Label+".",PxGold).Add(outcome.Text,PxBone);
            if(outcome.Materials!=0)lines.Gap().Add("Materials: "+outcome.Materials+" (now "+_campaign.Materials+")",PxMuted);
            if(outcome.Fighter!=null)lines.Gap().Add("Walks with you for this run",PxGold).Add(FighterName(outcome.Fighter),PxMint)
                .Add(LivesText(),PxMuted);
            if(outcome.Gear!=null)lines.Gap().Add("Gear",PxGold).Add(outcome.Gear.Name,PxBone).Add(outcome.Gear.Describe(),PxMuted)
                .Add("Put it on from the squad page.",PxMuted);
            Modal(outcome.Node.Event.Title,"");
            PxWrite(_modal,42,68,636,lines);
        }

        // ------------------------------------------------------------ omens

        /// The omens (Jacob, 13 Oct 2026: picked at camp): each makes every run harder in one plain way, each one picked
        /// adds to the materials a run brings, and enough of them make every drop on the road a tier better.
        void ShowOmens()
        {
            CloseModal();
            Modal("OMENS","");
            PxWrite(_modal,42,68,636,new PxLines().Add("Pick any before a run. Each makes every run harder in one plain way until you drop it.",PxBone)
                .Add("Each one picked: +"+OmenRules.MaterialsPercentEach+"% materials from the run. "+OmenRules.TierUpAt+
                    " or more: every drop on the road is a tier better.",PxGold));
            for(int i=0;i<OmenRules.All.Length;i++)
            {
                var omen=OmenRules.All[i];bool on=_campaign.HasOmen(omen);
                var button=PxButton("omen-"+i,"",42,112+i*40,636,30,()=>ToggleOmen(omen),_modal);
                if(!on)button.targetGraphic.color=new Color(.62f,.6f,.66f);
                var mark=on?PxGlyph(PxCheck,PxMint):_tinyFont.Render("+",PxMuted,PxMuted,1,true);
                _screenTextures.Add(mark);
                PxImage(button.transform,"Mark",mark,14,10);
                SetPx(PxLabel(button.transform,28,10),new[]{OmenRules.Name(omen),OmenRules.Describe(omen)},new[]{on?PxGold:PxBone,on?PxBone:PxMuted});
            }
            PxWrite(_modal,42,282,636,new PxLines().Add(_campaign.Omens.Count==0?"None picked.":"Picked: "+OmenNames()+".",
                _campaign.Omens.Count==0?PxMuted:PxGold).Add(OmenGains(),PxBone));
            SelectControl("omen-0");
        }

        /// Picks or drops one through Campaign.TrySetOmen, then shows camp and the pick again.
        void ToggleOmen(Omen omen)
        {
            if(!_campaign.TrySetOmen(omen,!_campaign.HasOmen(omen),out string why)){_notice=why;return;}
            Save();
            SetPage(Page.ChooseTribe);
            ShowOmens();
            SelectControl("omen-"+Array.IndexOf(OmenRules.All,omen));
        }

        string OmenNames()
        {
            var names=new List<string>();
            foreach(var o in _campaign.Omens)names.Add(OmenRules.Name(o));
            return string.Join(", ",names.ToArray());
        }

        /// What the picked omens bring: "Materials +50%, every drop on the road a tier better."
        string OmenGains()
        {
            int count=_campaign.Omens.Count;
            if(count==0)return "No omens: runs as they come.";
            return "Materials +"+count*OmenRules.MaterialsPercentEach+"%"+(count>=OmenRules.TierUpAt?", every drop on the road a tier better.":
                ". Pick "+(OmenRules.TierUpAt-count)+" more and every drop on the road is a tier better.");
        }

        /// The camp's OMENS button: the omens picked, or how many when their names will not fit.
        string OmenButtonLabel(int max)
        {
            if(_campaign.Omens.Count==0)return "Omens: none";
            string names="Omens: "+OmenNames();
            return _tinyFont.Measure(names.ToUpperInvariant(),1)<=max?names:"Omens: "+_campaign.Omens.Count+" picked";
        }

        // ------------------------------------------------------------ bonus goals

        /// The fight's bonus goal in its top bar while it is in reach, then met or missed (Jacob, 13 Oct 2026).
        void RefreshGoal(BattleSimulation sim)
        {
            if(_pxGoalScreen!=_screen){_pxGoal=PxLabel(_screen,520,8,2);_pxGoalScreen=_screen;}
            var goal=_fight.Encounter.Objective;
            if(goal==FightObjective.None){SetPx(_pxGoal,"",PxGold);return;}
            if(_fight.Over)
            {
                bool met=FightWon()&&FightObjectives.Met(goal,sim,FallenOfYours(sim));
                SetPx(_pxGoal,met?"Goal met: the piece goes up a tier":"Goal missed",met?PxMint:PxMuted);
                return;
            }
            string live;bool missed;
            switch(goal)
            {
                case FightObjective.NobodyFalls:
                {
                    int fallen=FallenOfYours(sim);
                    missed=fallen>0;live=!missed?"nobody of yours falls":fallen==1?"one of yours fell":fallen+" of yours fell";
                    break;
                }
                case FightObjective.Quick:
                {
                    long left=FightObjectives.QuickTicks-sim.CurrentTick;
                    missed=left<0;live=missed?FightObjectives.QuickTicks/SimClock.TicksPerSecond+"s are up":"win inside "+
                        FightObjectives.QuickTicks/SimClock.TicksPerSecond+"s, "+Seconds(left)+" left";
                    break;
                }
                default:
                {
                    int standing=0;
                    foreach(var u in sim.Units)if(u.Side==1&&u.Alive)standing++;
                    missed=standing<FightObjectives.BreakStanding;
                    live=missed?"fewer than "+FightObjectives.BreakStanding+" of theirs stand":"break them with "+
                        FightObjectives.BreakStanding+"+ standing ("+standing+" now)";
                    break;
                }
            }
            SetPx(_pxGoal,(missed?"Goal missed: ":"Goal: ")+live,missed?PxMuted:PxGold);
        }

        /// Your fighters down in this fight, the way Campaign.Finish counts them for the goal: your leader not among them.
        int FallenOfYours(BattleSimulation sim)
        {
            int fallen=0;
            foreach(var f in _fight.Squad){var u=sim.FindById(f.Id);if(u!=null&&!u.Alive)fallen++;}
            return fallen;
        }

        /// The squad page's goal lines.
        void RoadSquadLines(PxLines lines,Encounter e)
        {
            lines.Gap().Add(EnemyPreview(e),PxGold);
            if(e.Node!=null&&HasPalReward(e.Node))lines.Add("Win: choose a favour from "+FavourRules.PalName(e.Node.Pal)+" or keep the materials.",PxMint);
            if(e.Objective==FightObjective.None)return;
            var tier=_campaign.DropOf(e.Node);
            lines.Gap().Add("Bonus goal: "+FightObjectives.Text(e.Objective)+".",PxGold)
                .Add("Win and meet it: their piece is "+TierUp(tier)+", not "+tier+".",PxBone);
        }

        /// The result's goal line, one line so long results still fit.
        void RoadResultLines(PxLines lines,EncounterResult r)
        {
            if(!r.Won||r.Objective==FightObjective.None)return;
            lines.Add(r.ObjectiveMet?"Goal met: "+GoalShort(r.Objective)+". A tier up.":"Goal missed: "+GoalShort(r.Objective)+".",r.ObjectiveMet?PxMint:PxMuted);
        }

        // ------------------------------------------------------------ validation

        /// The Sprint 3 screens through the real buttons (13 Oct 2026), each on its own campaign and save file: omens
        /// picked at camp and the longer road they make, an event taken from the road (a choice you cannot pay for
        /// refused first), and a fight with a bonus goal from its node to its result.
        IEnumerator SprintThreeScreens()
        {
            _savePath=System.IO.Path.Combine(_capture,"sprint3-screens-save.txt");
            int blood=Array.IndexOf(OmenRules.All,Omen.BloodMoon),longRoad=Array.IndexOf(OmenRules.All,Omen.LongRoad);
            bool omens=false;
            for(ulong seed=1;seed<=12&&!omens;seed++)
            {
                _campaign=Campaign.New(seed);
                if(!CampaignPilot.PlayTutorial(_campaign,PilotStyle.Reference,seed))continue;
                SetPage(Page.ChooseTribe);yield return null;
                Check(_pathMapEnd<=452,"the run strip fits beside the OMENS button");
                Check(Click("omens"),"the camp's OMENS button");yield return null;
                Check(_modal!=null&&_buttons.ContainsKey("omen-"+blood),"the omen pick opens");
                Capture("s3-01-omens");
                Check(Click("omen-"+blood),"Blood Moon picked");yield return null;
                Check(Click("omen-"+longRoad),"Long Road picked");yield return null;
                Check(_campaign.HasOmen(Omen.BloodMoon)&&_campaign.HasOmen(Omen.LongRoad)&&_campaign.Omens.Count==2,"the omens are picked through Campaign");
                Capture("s3-02-omens-picked");
                Check(Click("omen-"+longRoad),"Long Road dropped");yield return null;
                Check(!_campaign.HasOmen(Omen.LongRoad)&&_campaign.Omens.Count==1,"an omen drops again");
                Check(Click("omen-"+longRoad),"Long Road picked again");yield return null;
                Check(Click("modal-close"),"the omen pick closes");yield return null;
                Check(_modal==null&&_page==Page.ChooseTribe,"back at camp with two omens");
                Capture("s3-03-camp-omens");
                var tribe=_campaign.Choosable()[0];
                Check(Click("tribe-"+tribe.Id),"a run starts with omens");yield return null;
                Check(_page==Page.Offers&&_campaign.Road!=null&&_campaign.Road.Rows==_campaign.Tuning.PathFights+1,"Long Road puts a row more on the road page");
                CheckRoadButtons();
                Capture("s3-04-road-omens");
                omens=true;
            }
            Check(omens,"omens picked at camp and a run started with them");

            bool taken=false,refused=false;
            for(ulong seed=1;seed<=40&&!(taken&&refused);seed++)
            {
                _campaign=Campaign.New(seed,new CampaignTuning{EnemyScaleTier1=40});
                if(!CampaignPilot.PlayTutorial(_campaign,PilotStyle.Reference,seed))continue;
                _campaign.StartRun();
                RoadNode scene=null;
                for(int n=0;n<4&&_campaign.Stage==CampaignStage.InRun&&scene==null;n++)
                {
                    foreach(var node in _campaign.NextNodes)if(node.Kind==RoadNodeKind.Event)scene=node;
                    if(scene!=null||_campaign.NextNodes.Count==0)break;
                    CampaignPilot.PlayFight(_campaign,_campaign.StartEncounter(new CampaignPilot(PilotStyle.Reference,seed).PickOffer(_campaign)),
                        new CampaignPilot(PilotStyle.Reference,seed*31+(ulong)n));
                }
                if(scene==null)continue;
                int step=_campaign.Step,materials=_campaign.Materials,dear=-1,cheap=-1;
                for(int i=0;i<scene.Event.Choices.Count;i++)
                {
                    var c=scene.Event.Choices[i];
                    if(c.Cost>materials&&dear<0)dear=i;
                    if(c.Cost<=materials&&c.Effect!=EventEffect.Nothing&&cheap<0)cheap=i;
                }
                if(taken&&dear<0)continue;
                SetPage(Page.Offers);yield return null;
                CheckRoadButtons();
                Capture("s3-05-road-event");
                Check(Click("event-"+scene.Lane),"an event node opens its scene");yield return null;
                Check(_modal!=null&&_buttons.ContainsKey("choice-0"),"the scene shows its choices");
                Capture("s3-06-event");
                if(dear>=0)
                {
                    Check(Click("choice-"+dear),"a choice you cannot pay for");yield return null;
                    Check(_campaign.Step==step&&_campaign.Materials==materials&&_modal!=null,"a refused choice changes nothing");
                    Check(_eventWhy!=null&&_eventWhy.key.Contains("costs"),"the scene says why the choice is refused");
                    Capture("s3-07-event-refused");
                    refused=true;
                }
                if(cheap<0)cheap=scene.Event.Choices.Count-1;
                Check(Click("choice-"+cheap),"an event choice taken");yield return null;
                Check(_campaign.Step==scene.Row+1&&_campaign.RoadLane==scene.Lane&&_campaign.Materials<=materials,"the event moves you along the road through Campaign");
                Check(_page==Page.Offers&&_modal!=null,"the scene says what happened, over the road");
                Capture("s3-08-event-outcome");
                Check(Click("modal-close"),"on from the event");yield return null;
                Check(_modal==null&&_page==Page.Offers,"the road again, one step on");
                CheckRoadButtons();
                Capture("s3-09-road-after-event");
                taken=true;
            }
            Check(taken&&refused,"an event taken from the road, and a choice refused for its price");

            bool goal=false;
            for(ulong seed=1;seed<=24&&!goal;seed++)
            {
                _campaign=Campaign.New(seed,new CampaignTuning{EnemyScaleTier1=40});
                if(!CampaignPilot.PlayTutorial(_campaign,PilotStyle.Reference,seed))continue;
                _campaign.StartRun();
                int offer=-1;
                for(int i=0;i<_campaign.Offers.Count;i++)if(_campaign.Offers[i].Objective!=FightObjective.None)offer=i;
                if(offer<0)continue;
                SetPage(Page.Offers);yield return null;
                Check(Click("offer-"+offer),"a fight node with a bonus goal");yield return null;
                Check(_page==Page.Squad,"the goal fight's squad page");
                Capture("s3-10-goal-squad");
                Check(Click("start-fight"),"the goal fight starts");
                _pilot=new CampaignPilot(PilotStyle.Reference,seed);_pilotOn=true;_speed=24;
                yield return null;
                Check(_pxGoal!=null&&_pxGoal.key.StartsWith("Goal",StringComparison.Ordinal),"the fight shows its bonus goal");
                Capture("s3-11-goal-fight");
                int frames=0;while(!_fight.Over&&frames++<2400)yield return null;
                Check(_fight.Over,"the goal fight ends");
                for(int i=0;i<76;i++)yield return null;
                Capture("s3-12-goal-fight-over");
                Check(Click("see-result"),"the goal fight's result");yield return null;
                if(!_result.Won)continue;
                Check(string.Join(" ",FightLines(_result,false).texts).Contains(_result.ObjectiveMet?"Goal met":"Goal missed"),"the result says the goal was met or missed");
                Capture("s3-13-goal-result");
                goal=true;
            }
            Check(goal,"a fight with a bonus goal played from its node to its result");
        }

        /// Every node you can take has its button, and nothing else on the road is one.
        void CheckRoadButtons()
        {
            var next=_campaign.NextNodes;
            for(int i=0;i<_campaign.Offers.Count;i++)Check(_buttons.ContainsKey("offer-"+i),"the road has a node for offer "+i);
            foreach(var n in next)if(n.Kind==RoadNodeKind.Event)Check(_buttons.ContainsKey("event-"+n.Lane),"the road has a node for the event in lane "+n.Lane);
            int lit=0;
            foreach(string id in _buttons.Keys)if(id.StartsWith("offer-",StringComparison.Ordinal)||id.StartsWith("event-",StringComparison.Ordinal))lit++;
            Check(lit==(next.Count==0?_campaign.Offers.Count:next.Count),"only the nodes you can take are lit");
        }
    }
}
