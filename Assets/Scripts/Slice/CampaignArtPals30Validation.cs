using System;
using System.Collections;
using System.IO;
using System.Linq;
using KindredSiege.Sim;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using UnityEngine;

namespace KindredSiege.Slice
{
    public sealed partial class CampaignArtLoop
    {
        IEnumerator ValidatePals30()
        {
            _savePath=Path.Combine(_capture,"pals30-save.txt");IntegrationCap(30);
            _campaign=CampaignSave.Read(File.ReadAllText(Path.Combine(Application.streamingAssetsPath,"ReviewRoute","camp.txt")),ReviewTuning);
            _fight=null;_playback=null;GoToStage();yield return null;
            ShowCamp();yield return null;Check(Click("pal-routes"),"camp opens Shade route previews");yield return null;
            string previewSave=CampaignSave.Write(_campaign);
            for(int route=1;route<ShadeRules.RouteCount;route++)
            {Check(Click("shade-guide-"+route),"preview Shade route "+route);yield return null;for(int form=1;form<=3;form++){Check(Click("shade-form-"+form),"preview form "+form);yield return null;}}
            Check(previewSave==CampaignSave.Write(_campaign),"all route previews are read only");Capture("30-shade-routes");CloseModal();
            ShowCamp();yield return null;Check(Click("pal-tree"),"camp opens Glutton tree");yield return null;
            for(int limb=0;limb<GluttonRules.LimbCount;limb++)
            {
                Check(Click("tree-limb-"+limb),"tree limb "+limb);yield return null;
                for(int slot=0;slot<GluttonRules.PerLimb;slot++)
                {Check(Click("tree-node-"+slot),"inspect talent "+GluttonRules.Name(GluttonRules.At(limb,slot)));yield return null;}
            }
            _treeLimb=0;_treeTalent=GluttonRules.At(0,0);ShowTalentTree();yield return null;
            int points=_campaign.TalentPoints;Check(points>0,"camp fixture earned actual codex points");
            Check(Click("tree-buy"),"buy through actual tree control");yield return null;
            Check(_campaign.OwnedTalents.Contains(_treeTalent)&&_campaign.TalentPoints==points-GluttonRules.Cost(_treeTalent),"backend owns talent and charged its cost");Capture("30-glutton-tree");
            Save();ContinueSaved();yield return null;
            Check(_campaign.OwnedTalents.Contains(GluttonRules.At(0,0)),"camp talent survives file reopen");
            _campaign.StartRun();SetPage(Page.Offers);ShowTalentTree();yield return null;
            Check(!_buttons["tree-buy"].interactable&&!_buttons["tree-reset"].interactable,"run cannot buy or reset camp tree");CloseModal();
            Check(_campaign.TryLendTalent(GluttonRules.At(0,1)),"explicit QA fixture holds second talent of same limb");
            ShowFeed();yield return null;Check(Click("feed-talent-"+(int)GluttonRules.At(0,1)),"choose talent to double");yield return null;
            Check(Click("feed-eat-"+(int)GluttonRules.At(0,0)),"choose exact sacrifice");yield return null;
            Check(_campaign.FedTalent==GluttonTalent.None,"Feed preview spends nothing");Capture("30-feed-confirm");
            Check(Click("feed-confirm"),"Feed confirmed through actual control");yield return null;
            Check(_campaign.FedTalent==GluttonRules.At(0,1)&&_campaign.EatenTalent==GluttonRules.At(0,0),"backend stores both Feed choices");
            Save();ContinueSaved();yield return null;
            Check(_campaign.TalentLevel(GluttonRules.At(0,1))==GluttonRules.Fed&&_campaign.TalentLevel(GluttonRules.At(0,0))==0,"Feed and sacrifice survive reload");CloseModal();

            // Backend-exported maximum-size fixture; only distribution is synthetic, all controls and persistence are real.
            _campaign=CampaignSave.Read(File.ReadAllText(Path.Combine(Application.streamingAssetsPath,"Pals30QA","eight-picks.txt")));
            _fight=null;_playback=null;SetPage(Page.Offers);ShowFavours();yield return null;
            string unchanged=CampaignSave.Write(_campaign);
            for(int page=0;page<3;page++)
            {
                int start=page*3;
                for(int i=start;i<Math.Min(8,start+3);i++)Check(_buttons["favour-"+i].gameObject.activeInHierarchy,"choice "+i+" visible on page "+page);
                Check(CampaignSave.Write(_campaign)==unchanged,"browsing page never spends or rerolls");Capture("30-picks-page-"+page);
                if(page<2){Check(Click("reward-next"),"next reward page");yield return null;}
            }
            Check(!_buttons["reward-next"].interactable,"last reward page stops at eight choices");
            Check(Click("favour-7"),"eighth paid choice reachable");yield return null;
            Check(_campaign.FavourOffer==null,"taking eighth pick consumes the offer once");CloseModal();

            IntegrationCap(30);_campaign=FreshRoadPreview();_campaign.TryTakeRoute(ShadeRoute.Rupture);_campaign.TryRankUp(_campaign.Reserve[0].Id);
            _campaign.TryTakeDuo(PalDuo.ShadowStar);_fight=_campaign.StartEncounter(0);_playback=null;BeginFight();_speed=0;_paused=false;_pilotOn=false;yield return null;
            Check(ViewSim.ShadeFormsOn&&ViewSim.GluttonOn&&ViewSim.DuosOn&&ViewSim.TightParryOn,"combined fight keeps all current switches");
            var target=ViewSim.Units.First(u=>u.Side==1&&u.Alive);Focus(target);RefreshFight();yield return null;
            Check(ShadeFollowUp(ViewSim,Directive.FocusFire).Length>0,"Rupture follows current focus target");
            Check(_orderTitle[2].key.Contains("RUPTURE")||_orderTitle[2].key.Contains("Rupture"),"Focus plate becomes actual follow-up");
            Check(DisplayOrderCost(ViewSim,Directive.FocusFire)==0,"follow-up remains free");
            int ready=0;while(!ViewOver&&ViewSim.RuptureStepsOf(0)==0&&ready++<180)HudTicks(1);
            RefreshFight();Check(!ViewOver&&_buttons["order-2"].interactable,"actual landed hits enable Rupture");
            int beforePoints=ViewSim.DirectivePoints(0);Check(Click("order-2"),"actual free Rupture button");
            Check(ViewSim.DirectivePoints(0)==beforePoints,"follow-up spends no ordinary order points");Capture("30-rupture-plate");
            HudTicks(20);Save();string state=ViewSim.OrderedStateSnapshot;ContinueSaved();yield return null;
            Check(ViewSim.OrderedStateSnapshot==state,"combined in-flight fight resumes exactly");
            _fight=null;_playback=null;IntegrationCap(30);
            _campaign=CampaignSave.Read(File.ReadAllText(Path.Combine(Application.streamingAssetsPath,"ReviewRoute","camp.txt")),ReviewTuning);
            _campaign.StartRun();var squad=_campaign.DefaultSquad(_campaign.BeastOffer.SquadSize).Where(f=>f.Class!=UnitClass.Trapper).Select(f=>f.Id).ToList();
            _fight=_campaign.StartBeast(squad);BeginFight();_paused=true;_pilotOn=false;_speed=0;yield return null;
            Check(ViewSim.BeastRaidOn,"accepted raid handback is active on current boss");
            Check(_comboCall==null||_comboCall.root!=null,"road combo presenter never retains a destroyed root in a boss fight");
            Check(_buttons.ContainsKey("beast-pup-0")&&!_buttons["beast-pup-0"].interactable,"unspawned pups cannot be focused");
            Check(BeastGuide(BeastKit.SandShark,true).Contains("FEEDING FRENZY"),"raid guide teaches intermission and pups");
            var pilot=new CampaignPilot(PilotStyle.Reference,6600);bool feeding=false,exposed=false;int bossTicks=0;
            while(!ViewOver&&bossTicks++<2400)
            {
                pilot.Step(_fight);HudTicks(1);
                if(!feeding&&ViewSim.BeastIntermission.Length>0)
                {feeding=true;RefreshFight();DrawBattle();yield return null;Capture("30-feeding-frenzy");Check(_beastState.key.Contains("FEEDING"),"intermission shown from actual sim");}
                if(ViewSim.BeastExposedTicks>0)exposed=true;
                if(feeding&&ViewSim.BeastIntermission.Length==0)break;
            }
            Check(feeding,"fixed boss reaches its raid intermission");
            Check(exposed||ViewSim.BeastAddIds.Count>0,"intermission reaches a real exposure or pup outcome");
            DrawBattle();yield return null;Capture("30-boss-after-frenzy");
            foreach(var id in ViewSim.BeastAddIds)
            {
                int n=id.EndsWith("1")?0:1;Check(_buttons["beast-pup-"+n].interactable,"actual live pup has legal target button");
                Check(FindFigure(id).artKey==null&&FindFigure(id).body==null,"pups never borrow human fighter art");
                foreach(string line in ("PUP "+(n+1)+" 140/140\nFOCUS TO CLEAR\nSPINE BROKEN").Split('\n'))Check(_tinyFont.Measure(line,1)<=150,"pup card line fits existing part slot");
                Check(Click("beast-pup-"+n),"pup Focus control");Check(ViewSim.FocusedId(0)==id,"pup selection addresses actual target ID");
            }
            _fight=null;_playback=null;IntegrationCap(30);_campaign=FreshRoadPreview();SetPage(Page.Offers);yield return null;
        }
    }
}
