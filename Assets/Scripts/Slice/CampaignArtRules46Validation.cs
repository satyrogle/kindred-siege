using System;
using System.Collections;
using System.IO;
using System.Linq;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using UnityEngine;

namespace KindredSiege.Slice
{
    public sealed partial class CampaignArtLoop
    {
        // Explicit player QA fixtures only, selected by -ksRules46Validate. Never owner saves.
        Campaign Rules46ForgeFixture(int cap,int materials,bool discount)
        {
            var tuning=new CampaignTuning{RulesCap=cap};
            string source=File.ReadAllText(Path.Combine(Application.streamingAssetsPath,"ReviewRoute","camp.txt"));
            string[] lines=CampaignSave.Write(CampaignSave.Read(source,tuning)).Split('\n');
            for(int i=0;i<lines.Length;i++)
            {
                if(lines[i].StartsWith("camp\t",StringComparison.Ordinal))lines[i]="camp\t"+materials;
                if(lines[i].StartsWith("forge\t",StringComparison.Ordinal))lines[i]="forge\t1\t0";
            }
            if(discount)
            {
                int end=Array.FindLastIndex(lines,line=>line.TrimEnd('\r')=="end");
                if(end<0)throw new InvalidDataException("Camp fixture has no end record");
                lines[end]="owned\t31\nowned\t32\nowned\t33\nend";
            }
            return CampaignSave.Read(string.Join("\n",lines),tuning);
        }

        IEnumerator ValidateRules46()
        {
            yield return null;
            Check(ReviewRulesCap==46&&_savePath.Contains("road46"),"launcher selects rules46 and its isolated save");
            foreach(var probe in new[]{(cap:46,discount:false,price:50),(cap:46,discount:true,price:33),(cap:45,discount:true,price:50)})
            {
                IntegrationCap(probe.cap);_fight=null;_playback=null;
                _campaign=Rules46ForgeFixture(probe.cap,probe.price,probe.discount);
                string name="forge-"+probe.cap+(probe.discount?"-discount":"-normal");
                _savePath=Path.Combine(_capture,name+".txt");
                SetPage(Page.ChooseTribe);yield return null;
                Check(_campaign.Stage==CampaignStage.ChooseTribe&&_campaign.TabardPrice==probe.price,name+": authoritative quote");
                Check(ForgeSummary().Contains("for "+probe.price+" materials"),name+": camp summary agrees");
                Check(Click("forge"),name+": real camp control opens forge");yield return null;
                Check(_pxButtonLabels["forge-buy"].key.StartsWith("FORGE: "+probe.price+"|",StringComparison.Ordinal),name+": button quotes the actual charge");
                Capture(name+"-quote");
                Check(Click("forge-buy"),name+": actual purchase button accepts exact funds");yield return null;
                Check(_campaign.TabardForged&&_campaign.Materials==0,name+": backend charges the quoted amount");
                Check(_notice.Contains("Paid "+probe.price+" materials"),name+": receipt states the actual charge");
                Check(!_buttons["forge-buy"].interactable,name+": repeat purchase disabled");
                Check(CampaignSave.TryReadFile(_savePath,ReviewTuning,out var saved,out _)&&saved.TabardForged&&saved.Materials==0,name+": purchase persists to disk");
                Capture(name+"-receipt");CloseModal();
            }
            IntegrationCap(46);_campaign=Rules46ForgeFixture(46,32,true);
            _savePath=Path.Combine(_capture,"forge-insufficient.txt");SetPage(Page.ChooseTribe);yield return null;
            ShowForge();yield return null;
            Check(!_buttons["forge-buy"].interactable&&!Click("forge-buy"),"32 materials cannot buy the discounted tabard");
            Check(!_campaign.TabardForged&&_campaign.Materials==32,"rejected purchase changes neither materials nor ownership");
            Capture("forge-insufficient");CloseModal();

            Modal("APPROVED COMBAT WORDS","");int wordIndex=0;
            foreach(var group in new[]{
                (path:"CampaignArt/UI/Marks/",height:34,names:"cash_crushed cash_exposed cash_frozen cash_hexed cash_opened cash_pinned cash_ripped"),
                (path:"CampaignArt/UI/Words43/",height:30,names:"bleed burn exposed guarded heal holds opening panic pin rearm rise selfpatch sidestep snaptrap snare stasis stim treat twincut"),
                (path:"Table42Review/Words/",height:34,names:"block crit dodge parry")})
                foreach(string name in group.names.Split(' '))
                {
                    var texture=Resources.Load<Texture2D>(group.path+name);
                    Check(texture!=null&&texture.height==group.height&&texture.filterMode==FilterMode.Point&&texture.mipmapCount==1,"approved unfiltered word loads: "+name);
                    var word=Raw(_modal,"QA "+name,95+(wordIndex/10)*450,140+(wordIndex%10)*61,texture.width,texture.height);
                    word.texture=texture;word.raycastTarget=false;wordIndex++;
                }
            Check(wordIndex==30,"all thirty approved words are packaged");
            yield return null;Capture("approved-words-native-size");CloseModal();

            _campaign=FreshRoadPreview();_savePath=Path.Combine(_capture,"road46-active.txt");
            SetPage(Page.Offers);yield return null;
            Check(Click("offer-0"),"rules46 road card opens squad");yield return null;
            Check(Click("start-fight"),"rules46 squad starts a fight");yield return RealtimeQaWaitForBattle();
            yield return Rules46FightAndReplay("road46");

            _playback=null;_fight=null;_campaign=FreshRaidPreview();_fight=_campaign.Active;
            _savePath=Path.Combine(_capture,"chief46-active.txt");BeginFight();yield return RealtimeQaWaitForBattle();
            yield return Rules46FightAndReplay("chief46");
            foreach(var kit in new[]{BeastKit.SandShark,BeastKit.CageBear})
            {
                _playback=null;_fight=null;_campaign=PitBeastFixture(kit);
                _savePath=Path.Combine(_capture,kit+"46-active.txt");SetPage(Page.Offers);yield return null;
                int offer=Enumerable.Range(0,_campaign.Offers.Count).First(i=>_campaign.Offers[i].Beast==kit);
                Check(Click("offer-"+offer),kit+": actual road card opens squad");yield return null;
                Check(Click("start-fight"),kit+": actual squad control starts fight");yield return RealtimeQaWaitForBattle();
                yield return Rules46FightAndReplay(kit+"46");
            }

            string legacyPath=Argument("-ksPitLegacyFile");
            Check(legacyPath!=null&&File.Exists(legacyPath),"frozen old rules37 replay supplied");
            _fight=null;_playback=null;_campaign=CampaignSave.Read(File.ReadAllText(legacyPath));
            string legacyBefore=CampaignSave.Write(_campaign);
            Check(_campaign.Archive.Fights.Count>0,"old rules37 archive contains fights");
            foreach(var archived in _campaign.Archive.Fights)
                Check(_campaign.Archive.TryReplay(archived.FightId,out _,out var failure),"old archive checksum: "+archived.FightId+" "+failure);
            SetPage(Page.Offers);
            yield return Rules46Replay("legacy37");
            Check(!PitPresentation&&CampaignSave.Write(_campaign)==legacyBefore,"old replay retains its presentation and leaves the save unchanged");
            // Leave the playback page before releasing its state; LateUpdate still reads it this frame.
            SetPage(Page.Title);_playback=null;_fight=null;yield return null;
            Log("Rules46 frontend, real purchases, approved words, four current fights and historical replay checked.");
        }

        IEnumerator Rules46FightAndReplay(string name)
        {
            Check(ViewSim.Fight.TreeRetarget,name+": current fight enables rules46");
            _pilotOn=false;_paused=false;_speed=0;
            HudTicks(60);_paused=true;
            string snapshot=ViewSim.OrderedStateSnapshot;
            DrawBattle();Capture(name+"-live");
            Check(ViewSim.OrderedStateSnapshot==snapshot,name+": drawing leaves authoritative state unchanged");
            string save=CampaignSave.Write(_campaign);File.WriteAllText(_savePath,save);
            Check(CampaignSave.Read(save,ReviewTuning).Active.Sim.OrderedStateSnapshot==snapshot,name+": active save reload is exact");
            long tick=ViewSim.CurrentTick;yield return null;yield return null;
            Check(ViewSim.CurrentTick==tick,name+": pause preserves the fight clock");
            _paused=false;
            for(int i=0;i<6000&&!ViewOver;i++)
            {
                if(ViewSim.AwaitingVerdict)_fight.TryOrder(Directive.Hold,null,false,out _);
                HudTicks(1);if(i%100==0)yield return null;
            }
            Check(ViewOver,name+": fixed no-pilot fight completes");
            ShowResult();yield return null;Capture(name+"-result");
            File.WriteAllText(Path.Combine(_capture,name+"-archive.txt"),CampaignSave.Write(_campaign));
            yield return Rules46Replay(name);
        }

        IEnumerator Rules46Replay(string name)
        {
            var archived=_campaign.Archive.Fights.Last();
            Check(FightPlayback.TryOpen(_campaign.Archive,archived.FightId,out var expected,out var failure),name+": independent replay opens: "+failure);
            WatchFight(archived);yield return RealtimeQaWaitForBattle();
            long middle=_playback.Length/2;
            while(expected.Tick<middle)expected.Step();
            SeekPlayback(middle);yield return RealtimeQaWaitForBattle();
            Check(_playback.Tick==middle&&_playback.Failure.Length==0&&ViewSim.OrderedStateSnapshot==expected.Sim.OrderedStateSnapshot,name+": seek matches independently stepped state");
            Capture(name+"-seek");
            SeekPlayback(_playback.Length);yield return RealtimeQaWaitForBattle();
            Check(_playback.Matched,name+": end checksum and event history match");
            SeekPlayback(0);yield return RealtimeQaWaitForBattle();
            Check(_playback.Tick==0,name+": replay restarts at zero");
            SeekPlayback(0);SeekPlayback(middle);yield return RealtimeQaWaitForBattle();
            Check(_playback.Tick==middle&&ViewSim.OrderedStateSnapshot==expected.Sim.OrderedStateSnapshot,name+": latest seek wins during rebuild");
        }
    }
}
