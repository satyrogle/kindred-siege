using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using UnityEngine;

namespace KindredSiege.Slice
{
    public sealed partial class CampaignArtLoop
    {
        IEnumerator ValidatePals35()
        {
            string fixtures=Argument("-ksPals35Fixtures");
            Check(fixtures!=null,"explicit generated UI fixtures supplied");
            _savePath=Path.Combine(_capture,"pals35-save.txt");
            Check(Path.IsPathRooted(ReviewProfileRoot("1"))&&ReviewProfileRoot("1")!=Path.GetFullPath("1"),"relative flag cannot create a repo profile");
            foreach(var cls in new[]{UnitClass.Warden,UnitClass.Marksman,UnitClass.Skirmisher,UnitClass.Trapper,UnitClass.Medic})
            for(int choice=0;choice<2;choice++)
            {
                _campaign=CampaignSave.Read(File.ReadAllText(Path.Combine(fixtures,cls+".txt")));
                _fight=null;_playback=null;_result=null;SetPage(Page.Offers);
                var pick=_campaign.FavourOffer.Picks[choice];string id=pick.FighterId;
                Check(_campaign.VowsRun&&pick.Vow!=StarVow.None,"vow choice is an authoritative offered pick");
                ShowFavours();yield return null;
                Check(WhisperTitle(pick).Contains(VowRules.Name(pick.Vow)),"vow name distinguishes the two rank-up choices");
                Check(WhisperEffect(pick).Contains(VowRules.Describe(pick.Vow,1)),"card explains the actual vow");
                Check(_buttons["favour-"+choice].interactable,"vow card is actionable");
                if(choice==0)Capture("35-vow-"+cls);
                Check(Click("favour-"+choice),"take vow using actual reward control");yield return null;
                var fighter=_campaign.Reserve.First(f=>f.Id==id);
                Check(fighter.Rank==2&&fighter.Vow==pick.Vow,"chosen vow reaches its intended fighter");
                Check(SecondGearHint(fighter).Contains(VowRules.Name(pick.Vow)),"fighter details retain the vow");
                Save();ContinueSaved();yield return null;
                Check(_campaign.LoadNote.Length==0&&_campaign.Reserve.First(f=>f.Id==id).Vow==pick.Vow,"vow survives file reopen");
                Check(_campaign.TryRankUp(id),"explicit QA rank3 progression");
                Check(SecondGearHint(_campaign.Reserve.First(f=>f.Id==id)).Contains(VowRules.Describe(pick.Vow,2)),"rank3 fighter shows final vow form");
            }
            _campaign=CampaignSave.Read(File.ReadAllText(Path.Combine(fixtures,"duo.txt")));
            _fight=null;_playback=null;_result=null;SetPage(Page.Offers);ShowFavours();yield return null;
            int materials=_campaign.Materials;var offer=_campaign.FavourOffer;
            Check(FreeDuo(offer.Choices[0]),"rules35 duo is an extra free choice");
            Check(_buttons["favour-0"].interactable,"free duo control enabled");Capture("35-free-duo");
            Check(Click("favour-0"),"take free duo using reward control");yield return null;
            Check(_campaign.HoldsDuo(PalDuo.Gorge)&&_campaign.Materials==materials,"duo is held without spending materials");
            Check(_campaign.FavourOffer!=null&&_campaign.FavourOffer.Choices.Count==offer.Choices.Count-1,"normal choice remains open after the duo");
            Check(_campaign.DuoLevel==1,"duo starts its persistent level");
            Check(Click("favour-0"),"take remaining normal reward");yield return null;
            Check(_campaign.FavourOffer==null&&_campaign.Materials==materials-17,"normal pick alone pays the normal cost");
            Save();ContinueSaved();yield return null;
            Check(_campaign.HoldsDuo(PalDuo.Gorge)&&_campaign.DuoLevel==1,"duo and level survive file reopen");
            SetPage(Page.Offers);yield return null;
            int flavoured=0;
            if(_campaign.Road!=null)for(int row=0;row<_campaign.Road.Rows;row++)for(int lane=0;lane<CampaignRoad.Lanes;lane++)
            {
                var node=_campaign.Road.Node(row,lane);if(node.Pal!=Pal.Butcher)continue;
                Check(_campaign.NodeFlavour(node)!=Augment.None,"Butcher road carries authoritative flavour");flavoured++;
            }
            Check(flavoured>0,"fixture road contains a Butcher flavour fight");Capture("35-flavour-road");
            _fight=_campaign.StartEncounter(0);_paused=true;SetPage(Page.Fight);yield return null;
            Check(ViewSim.Setup.Fight.FreezeContracts&&ViewSim.Setup.Fight.Contracts&&ViewSim.Setup.Fight.Corrections&&ViewSim.DuoMovesOn,"new fight uses current rules37 contracts");
            Check(_duoLevelRead!=null&&_duoLevelRead.key.StartsWith("DUO 1 / 3"),"duo level is drawn separately from the parry suns");
            Check(_buttons["order-2"].interactable,"Focus can begin target selection without an existing target");
            Check(Click("order-2"),"Focus enters target selection");yield return null;
            Check(_targeting,"Focus asks for an enemy");
            var enemy=ViewSim.Units.First(u=>u.Side==1&&u.Alive);SelectFigure(enemy);yield return null;
            Check(ViewSim.FocusedId(0)==enemy.Id,"target selection sends the actual Focus command");
            Capture("35-duo-hud-focus");

            // A/B identical transitions in this process. No change to rules or simulation speed.
            var timings=new List<string>{"cache,sample,ms,clips"};
            for(int mode=0;mode<2;mode++)for(int i=0;i<4;i++)
            {
                _keepBattleClips=mode==1;_tinyFont.RasterCacheEnabled=_logoFont.RasterCacheEnabled=mode==1;
                _campaign=CampaignSave.Read(File.ReadAllText(Path.Combine(fixtures,"duo.txt")));
                _fight=_campaign.StartEncounter(0);_paused=true;
                var watch=System.Diagnostics.Stopwatch.StartNew();SetPage(Page.Fight);watch.Stop();yield return null;
                timings.Add(mode+","+i+","+watch.Elapsed.TotalMilliseconds.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+","+_clips.Count);
                Check(_clips.Count==_battleClipKeys.Count,"clip cache contains only current battle clips");
                Check(_tinyFont.CachedPixels<=512000&&_logoFont.CachedPixels<=512000,"font raster caches stay within their memory budgets");
            }
            File.WriteAllLines(Path.Combine(_capture,"transitions.csv"),timings);
            _keepBattleClips=true;_tinyFont.RasterCacheEnabled=_logoFont.RasterCacheEnabled=true;
            var cached=_tinyFont.Render("PARRY / DUO 3 / FOCUS",PxGold,PxBone,1,true);
            _tinyFont.RasterCacheEnabled=false;var plain=_tinyFont.Render("PARRY / DUO 3 / FOCUS",PxGold,PxBone,1,true);
            Check(cached.GetPixels32().SequenceEqual(plain.GetPixels32()),"cached font pixels match uncached rendering exactly");
            Destroy(cached);Destroy(plain);_tinyFont.RasterCacheEnabled=true;
            Capture("35-final-fight");
            _campaign=CampaignSave.Read(File.ReadAllText(Path.Combine(fixtures,"duo.txt")));
            _fight=_campaign.StartEncounter(0);_paused=false;_asyncBattleQa=true;
            SetPage(Page.Fight);Check(_buildingBattle,"actual player path begins with loading feedback");
            long tick=ViewSim.CurrentTick;Capture("35-loading");
            int frames=0;
            while(_buildingBattle&&frames++<120)
            {Check(ViewSim.CurrentTick==tick,"combat remains frozen during construction");yield return null;}
            Check(!_buildingBattle&&_figures.Count==ViewSim.Units.Count,"construction completes across frames");
            Check(_buildSlices>5,"world was constructed in multiple bounded steps");
            Check(!_screen.GetComponentsInChildren<RectTransform>().Any(t=>t.name=="Forming squad"&&t.gameObject.activeInHierarchy),"loading blocker and caption disappear immediately when ready");
            File.WriteAllText(Path.Combine(_capture,"loading-slices.txt"),"slices="+_buildSlices+" maxMs="+_buildSliceMaxMs.ToString("F3",System.Globalization.CultureInfo.InvariantCulture));
            _asyncBattleQa=false;_paused=true;Capture("35-after-loading");
        }
    }
}
