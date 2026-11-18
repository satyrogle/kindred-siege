using System;
using System.Collections.Generic;
using KindredSiege.Sim;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using UnityEngine;

namespace KindredSiege.Slice
{
    public sealed partial class CampaignArtLoop
    {
        bool GluttonUi => _campaign!=null&&_campaign.GluttonRun;
        int _treeLimb, _rewardPage;
        GluttonTalent _treeTalent, _feedTalent;
        FavourOffer _rewardPageOffer;
        string _treeNotice="";
        ShadeRoute _guideRoute=ShadeRoute.Mirror;
        int _guideForm=1;

        void ShowShadeRoutes()
        {
            CloseModal();Modal("SHADE / ORDER ROUTES","");
            SetPx(PxLabel(_modal,26,58),"TWO ENHANCED BUTTONS / ONE ROUTE EACH / THREE FORMS PER ROUTE",PxGold);
            for(int i=1;i<ShadeRules.RouteCount;i++)
            {
                var route=(ShadeRoute)i;
                PxButton("shade-guide-"+i,OrderName(ShadeRules.ButtonOf(route))+" / "+ShadeRules.Name(route),26,100+(i-1)*33,270,28,()=>
                {_guideRoute=route;_guideForm=1;ShowShadeRoutes();},_modal);
            }
            for(int i=1;i<=ShadeRules.MaxForm;i++)
            {
                int form=i;
                PxButton("shade-form-"+i,"FORM "+i,310+(i-1)*130,100,124,28,()=>{_guideForm=form;ShowShadeRoutes();},_modal);
            }
            int held=0;foreach(var route in _campaign.ShadeRoutes)if(route.Route==_guideRoute)held=route.Form;
            PxPanel(310,140,384,218,new PxLines().Add(ShadeRules.Name(_guideRoute),PxGold)
                .Add("PREVIEW / "+ShadeRules.FormName(_guideRoute,_guideForm),PxLilac)
                .Add(BattleSimulation.RouteDescription(_guideRoute,_guideForm,_campaign!=null&&System.Math.Min(Campaign.RulesVersion,_campaign.Tuning.RulesCap)>=Campaign.SizedRules),PxBone).Gap()
                .Add(held>0?"HELD THIS RUN / FORM "+held:"NOT HELD THIS RUN",PxMint)
                .Add("Earn upgrades after Shade-marked road fights. Viewing this screen changes nothing.",PxMuted),_modal);
            SelectControl("shade-guide-"+(int)_guideRoute);
        }

        static bool AdvancedPick(Favour f)=>FavourRules.IsShade(f)||FavourRules.IsGlutton(f)||FavourRules.IsDuo(f);

        PxLines AdvancedPickLines(Favour f,bool compact=false)
        {
            int rank=_campaign.FavourRank(f),next=Math.Min(FavourRules.MaxRank,rank+1);
            var lines=new PxLines().Add(FavourRules.Name(f),PxGold);
            if(FavourRules.IsShade(f))
            {
                var route=FavourRules.RouteOf(f);
                lines.Add(OrderName(ShadeRules.ButtonOf(route))+" / FORM "+next+" OF 3",PxLilac);
                if(!compact)lines.Add(ShadeRules.FormName(route,next),PxBone)
                    .Add(BattleSimulation.RouteDescription(route,next,System.Math.Min(Campaign.RulesVersion,_campaign.Tuning.RulesCap)>=Campaign.SizedRules),PxBone).Gap().Add("Two buttons enhanced per run. One route on each.",PxMuted);
            }
            else if(FavourRules.IsGlutton(f))
            {
                var talent=FavourRules.TalentOf(f);
                lines.Add(GluttonRules.Limbs[GluttonRules.LimbOf(talent)]+" / LENT THIS RUN",PxMint);
                if(!compact)lines.Add("BASE: "+FavourRules.Describe(f,1),PxBone).Add("Feed doubles its bonus. Fight values can grow with this limb.",PxMuted);
            }
            else
            {
                var duo=FavourRules.DuoOf(f);
                lines.Add(FavourRules.PalName(FavourRules.FirstOf(duo))+" + "+FavourRules.PalName(FavourRules.SecondOf(duo)),PxMint);
                if(!compact)
                {
                    lines.Add(DuoRules.Describe(duo),PxBone).Gap().Add(DuoRules.IsMove(duo)?"FREE / normal reward stays. Duo level 1-3; answer a big move to grow, a miss loses one level.":"Held for this run.",PxMuted);
                    if(duo==PalDuo.CrownedMarks||duo==PalDuo.ShadowStar||duo==PalDuo.FattenedStar||duo==PalDuo.Executioner||duo==PalDuo.ShadowTwin||duo==PalDuo.Gorge)
                    {bool star=false;foreach(var fighter in _campaign.Reserve)star|=fighter.Rank>=2;if(!star)lines.Add("NO STAR YET / rank up a fighter to activate its star effect.",PxOrange);}
                }
            }
            return lines;
        }

        int RewardPageStart(FavourOffer offer)
        {
            if(_rewardPageOffer!=offer){_rewardPageOffer=offer;_rewardPage=0;}
            _rewardPage=Math.Max(0,Math.Min(_rewardPage,(offer.Choices.Count-1)/3));
            return _rewardPage*3;
        }

        void RewardPager(int count,bool combined,int y)
        {
            if(count<=3)return;
            int pages=(count+2)/3;
            SetPx(PxLabel(_modal,300,y+7),"PAGE "+(_rewardPage+1)+" / "+pages,PxGold);
            PxButton("reward-prev","PREVIOUS",26,y,154,24,()=>
            {_rewardPage--;if(combined)ShowRewardChoices();else ShowFavours();},_modal).interactable=_rewardPage>0;
            PxButton("reward-next","NEXT",540,y,154,24,()=>
            {_rewardPage++;if(combined)ShowRewardChoices();else ShowFavours();},_modal).interactable=_rewardPage+1<pages;
        }

        void ShowTalentTree()
        {
            if(!GluttonUi)return;
            CloseModal();Modal("GLUTTON / TALENT TREE","");
            bool camp=_campaign.Stage!=CampaignStage.InRun;
            if(!GluttonRules.Valid(_treeTalent)||GluttonRules.LimbOf(_treeTalent)!=_treeLimb)_treeTalent=GluttonRules.At(_treeLimb,0);
            SetPx(PxLabel(_modal,26,56),"POINTS "+_campaign.TalentPoints+" / CAMPAIGN TIER "+_campaign.ReachedTier+(camp?" / CAMP UPGRADES":" / READ ONLY DURING A RUN"),PxGold);
            for(int i=0;i<GluttonRules.LimbCount;i++)
            {
                int limb=i;
                PxButton("tree-limb-"+i,GluttonRules.Limbs[i],16+i*99,78,94,26,()=>
                {_treeLimb=limb;_treeTalent=GluttonRules.At(limb,0);_treeNotice="";ShowTalentTree();},_modal);
            }
            for(int i=0;i<GluttonRules.PerLimb;i++)
            {
                var talent=GluttonRules.At(_treeLimb,i);bool owned=false;
                foreach(var held in _campaign.OwnedTalents)if(held==talent)owned=true;
                string label=(owned?"OWNED / ":!_campaign.CanBuyTalent(talent,out _)?"LOCKED / ":i==4?"KEY / ":"T"+GluttonRules.TierOf(talent)+" / ")+GluttonRules.Name(talent);
                var button=PxButton("tree-node-"+i,label,26,120+i*44,270,36,()=>
                {_treeTalent=talent;_treeNotice="";ShowTalentTree();},_modal);
                OnHover(button.gameObject,()=>GluttonRules.Describe(talent));
            }
            bool selectedOwned=false;foreach(var t in _campaign.OwnedTalents)if(t==_treeTalent)selectedOwned=true;
            var lines=new PxLines().Add(GluttonRules.Name(_treeTalent),PxGold)
                .Add(GluttonRules.Describe(_treeTalent),PxBone).Gap()
                .Add("COST "+GluttonRules.Cost(_treeTalent)+" POINTS / OPENS AT TIER "+GluttonRules.OpensAtTier(_treeTalent),PxMint)
                .Add("Tier 1 > tier 2 > choose one tier 3 > keystone.",PxMuted)
                .Add("Bought talents stay between runs. Road loans leave with the run.",PxMuted);
            bool canBuy=_campaign.CanBuyTalent(_treeTalent,out string buyWhy);
            if(!selectedOwned&&!canBuy)lines.Gap().Add(buyWhy,PxOrange);
            if(_treeNotice.Length>0)lines.Gap().Add(_treeNotice,PxOrange);
            PxPanel(310,120,384,218,lines,_modal);
            PxButton("tree-buy",selectedOwned?"OWNED":"BUY / "+GluttonRules.Cost(_treeTalent)+" POINTS",310,346,384,28,()=>
            {
                if(!_campaign.TryBuyTalent(_treeTalent,out _treeNotice)){ShowTalentTree();return;}
                _treeNotice="BOUGHT / "+GluttonRules.Name(_treeTalent);Save();ShowTalentTree();
            },_modal).interactable=canBuy;
            PxButton("tree-reset","RESET TREE",26,398,220,28,()=>
            {
                CloseModal();Modal("RESET GLUTTON TREE","Return every spent talent point. Your owned camp talents are removed. This is free at camp.");
                _buttons["modal-close"].gameObject.SetActive(false);
                PxButton("tree-reset-back","BACK",16,398,328,28,ShowTalentTree,_modal);
                PxButton("tree-confirm-reset","RESET AND REFUND",360,398,334,28,()=>
                {_treeNotice=_campaign.TryResetTree()?"TREE RESET / POINTS REFUNDED":"Tree cannot reset now.";Save();ShowTalentTree();},_modal);
                SelectControl("tree-reset-back");
            },_modal).interactable=camp&&_campaign.OwnedTalents.Count>0;
            SelectControl("tree-node-"+GluttonRules.SlotOf(_treeTalent));
        }

        void ShowFeed()
        {
            if(!GluttonUi)return;
            CloseModal();Modal("GLUTTON / FEED","");
            SetPx(PxLabel(_modal,26,60),"ONCE PER RUN / DOUBLE ONE TALENT / LOSE ANOTHER FROM ITS LIMB",PxGold);
            var held=new List<GluttonTalent>();
            for(int i=1;i<=GluttonRules.TalentCount;i++)if(_campaign.HoldsTalent((GluttonTalent)i))held.Add((GluttonTalent)i);
            if(held.Count==0){PxWrite(_modal,36,112,648,new PxLines().Add("No talents held. Buy at camp or take a Glutton road pick.",PxBone));return;}
            if(!held.Contains(_feedTalent))_feedTalent=held[0];
            int chosen=held.IndexOf(_feedTalent),page=chosen/5;
            for(int i=page*5;i<Math.Min(held.Count,page*5+5);i++)
            {
                var talent=held[i];
                PxButton("feed-talent-"+(int)talent,GluttonRules.Name(talent),26,104+(i-page*5)*42,270,34,()=>
                {_feedTalent=talent;ShowFeed();},_modal);
            }
            if(page>0)PxButton("feed-prev","PREVIOUS",26,326,130,26,()=>{_feedTalent=held[(page-1)*5];ShowFeed();},_modal);
            if((page+1)*5<held.Count)PxButton("feed-next","NEXT",166,326,130,26,()=>{_feedTalent=held[(page+1)*5];ShowFeed();},_modal);
            PxWrite(_modal,318,104,370,new PxLines().Add("DOUBLE "+GluttonRules.Name(_feedTalent),PxMint)
                .Add(GluttonRules.Describe(_feedTalent),PxBone).Gap().Add("CHOOSE WHAT HE EATS",PxOrange));
            bool available=_campaign.Stage==CampaignStage.InRun&&_campaign.Active==null&&_campaign.FedTalent==GluttonTalent.None;
            var victims=new List<GluttonTalent>();
            foreach(var t in held)if(t!=_feedTalent&&GluttonRules.LimbOf(t)==GluttonRules.LimbOf(_feedTalent))victims.Add(t);
            if(victims.Count==0)victims.Add(GluttonTalent.None);
            for(int i=0;i<victims.Count;i++)
            {
                var victim=victims[i];
                PxButton("feed-eat-"+(int)victim,victim==GluttonTalent.None?"NOTHING ELSE IN THIS LIMB":GluttonRules.Name(victim),318,224+i*31,370,27,()=>ConfirmFeed(victim),_modal).interactable=available;
            }
            SetPx(PxLabel(_modal,26,366),_campaign.FedTalent!=GluttonTalent.None?"FED "+GluttonRules.Name(_campaign.FedTalent):available?"Select both. Nothing is spent until you confirm.":"Feed between fights during a run.",PxGold);
            SelectControl("feed-talent-"+(int)_feedTalent);
        }

        void ConfirmFeed(GluttonTalent eaten)
        {
            var fed=_feedTalent;CloseModal();Modal("CONFIRM FEED","Double "+GluttonRules.Name(fed)+" for this run.\nLose "+(eaten==GluttonTalent.None?"no other talent":GluttonRules.Name(eaten))+" for this run.\nFeed can only be used once this run.");
            _buttons["modal-close"].gameObject.SetActive(false);
            PxButton("feed-back","BACK",16,398,328,28,ShowFeed,_modal);
            PxButton("feed-confirm","FEED",360,398,334,28,()=>
            {
                if(!_campaign.TryFeed(fed,eaten,out string why)){_notice=why;ShowFeed();return;}
                Save();_notice="FED "+GluttonRules.Name(fed);ShowFeed();
            },_modal);
            SelectControl("feed-back");
        }

        static string ShadeFollowUp(BattleSimulation sim,Directive order)
        {
            string target=order==Directive.FocusFire?sim.FocusedId(0):"";
            if(order==Directive.FocusFire){var unit=sim.FindById(target);if(unit==null||!unit.Alive)return "";}
            return sim.FollowUpOf(0,order,target);
        }

        void PalOrderPlate(BattleSimulation sim,Directive order,bool verdict,ref string title,ref string effect,ref string cost,ref bool ok,ref bool lit)
        {
            if(verdict||!sim.ShadeFormsOn)return;
            string follow=ShadeFollowUp(sim,order);
            if(follow.Length>0)
            {title=(order==Directive.FocusFire?"3 ":"4 ")+follow;effect=order==Directive.FocusFire?"Weak point "+sim.RuptureStepsOf(0):"Press to follow up";cost="FREE";ok=!ViewOver&&!_pilotOn&&_playback==null&&sim.CanUseOrder(0,order,order==Directive.FocusFire?sim.FocusedId(0):"",out _);lit=ok;return;}
            var route=sim.Setup.Rules(0).Pals.RouteOn(order);
            if(route==ShadeRoute.None)return;
            int form=sim.Setup.Rules(0).Pals.FormOf(route);
            title=(Array.IndexOf(Orders,order)+1)+" "+ShadeRules.FormName(route,form);
            if(order==Directive.FallBack&&sim.FallBackLockLeft(0)>0)effect="Wait "+Seconds(sim.FallBackLockLeft(0));
            else if(order==Directive.FallBack&&sim.ReturnTicksLeft(0)>0)effect="Returning "+Seconds(sim.ReturnTicksLeft(0));
            else if(order==Directive.Hold&&sim.FieldTicksLeft(0)>0&&sim.WhiffTicksLeft(0)==0)effect="Field "+Seconds(sim.FieldTicksLeft(0))+" / "+sim.RelayLeft(0);
            title=Fit(title,90);effect=Fit(effect,90);
        }
    }
}
