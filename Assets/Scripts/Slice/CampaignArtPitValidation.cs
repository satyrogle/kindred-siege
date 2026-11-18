using System.Collections;
using System;
using System.IO;
using System.Linq;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using UnityEngine;

namespace KindredSiege.Slice
{
    public sealed partial class CampaignArtLoop
    {
        IEnumerator ValidatePit()
        {
            yield return null;
            _playback=null;_fight=null;
            IntegrationCap(39);_campaign=FreshRoadPreview();_fight=_campaign.StartEncounter(0);
            BeginFight();_pilotOn=false;_paused=true;_speed=0;yield return null;
            Check(PitPresentation,"new run uses authoritative Pit rules");
            foreach(var f in _figures)if(f.attack!=null)
            {
                Check(f.pit!=null&&Mathf.Abs(f.attackLength-f.attack.seconds)<.001f,"native attack duration: "+f.unit.Id);
                Check(f.pit.segments.Length>0&&f.pit.segments.All(s=>s.start<=s.strike&&s.strike<=s.end&&s.end<f.attack.sheet.frames.Length),"authored strike segments: "+f.unit.Id);
            }
            Capture("pit-00-formation");
            int hitCount=0;bool attackSeen=false,idleSeen=false,guardShot=false,comboShot=false,healShot=false;
            for(int tick=0;tick<600&&!_fight.Over;tick++)
            {
                int cursor=ViewSim.Events.Count;_paused=false;HeavyTestTick();RefreshFight();DrawBattle();
                string before=ViewSim.OrderedStateSnapshot;DrawBattle();
                Check(before==ViewSim.OrderedStateSnapshot,"presentation leaves sim unchanged at "+tick);
                for(int side=0;side<2;side++)
                {
                    var trap=ViewSim.ReadTrap(side);
                    if(trap.Armed)
                        Check(_pitSpellReads.TryGetValue(trap.OwnerId,out var read)&&read.trap.All(d=>d!=null&&d.enabled),"armed ring follows stored trap owner and springs at "+tick);
                }
                foreach(var e in ViewSim.Events.Skip(cursor))if(e.Kind==BattleEventKind.Hit)hitCount++;
                attackSeen|=_figures.Any(f=>f.attack!=null&&f.drawnClip==f.attack);
                idleSeen|=_figures.Any(f=>f.pit!=null&&!f.pit.coming&&f.drawnClip==f.idle);
                if(tick==25||tick==75||tick==150||tick==300)Capture("pit-fight-"+tick);
                if(!guardShot&&_pitGuardSeen>0){guardShot=true;Capture("pit-31-guard");}
                if(!comboShot&&_pitComboSeen>0){comboShot=true;Capture("pit-32-combo");}
                if(!healShot&&_pitHealSeen>0){healShot=true;Capture("pit-33-heal");}
                _paused=true;if(tick%15==0)yield return null;
            }
            Check(hitCount>0&&attackSeen&&idleSeen,"real fight shows hits, attacks and resting poses");
            Check(_pitGuardSeen>0&&_pitHealSeen>0&&_pitComboSeen>0&&_pitSnareSeen>0,"real guard, heal, combo and snare events reach their presentation");
            var mark=_figures.First(f=>f.unit.Side==0&&!f.unit.IsLeader);
            foreach(var n in _numbers){if(n.image.texture!=null){_screenTextures.Remove((Texture2D)n.image.texture);Destroy(n.image.texture);}Destroy(n.image.gameObject);}
            _numbers.Clear();
            for(int i=0;i<7;i++)PitFloatNumber(mark,10,false,-1,"",false,"cleave");
            for(int i=0;i<3;i++)PitFloatNumber(mark,4,true,-1,"",false,"");
            Check(_numbers.Count<=4&&_numbers.Where(n=>n.heal).Sum(n=>n.sum)==12&&_numbers.Where(n=>!n.heal).Sum(n=>n.sum)==70,
                "crowded receipts stay at four and preserve separate healing/damage totals");
            string save=CampaignSave.Write(_campaign);var reload=CampaignSave.Read(save,ReviewTuning);
            Check(CampaignSave.Write(reload)==save,"active current-rule campaign reload preserves exact save");
            _paused=true;long at=ViewSim.CurrentTick;float clock=_visualTime;
            for(int i=0;i<4;i++)yield return null;
            Check(ViewSim.CurrentTick==at&&_visualTime==clock,"pause preserves combat and presentation clocks");
            if(Argument("-ksPitStep2")!=null)
            {
                IntegrationCap(39);_campaign=FreshRaidPreview();_fight=_campaign.Active;_playback=null;
                BeginFight();_pilotOn=false;_paused=false;_speed=0;yield return null;
                int guard=0;bool sawFuse=false;
                while(!ViewOver&&!ViewSim.Threats(0).Any(t=>t.ParryWindow)&&guard++<1800)
                {
                    HudTicks(1);
                    if(!sawFuse&&_pitFuses.Count>0)
                    {
                        sawFuse=true;Capture("pit-21-fuse-warning");
                        Check(_pitFuses.All(f=>f.total>=f.threat.RealBeatTicks&&f.dots.All(d=>d.enabled)),"floor fuse tracks authoritative windup");
                    }
                }
                Check(!ViewOver&&guard<1800&&sawFuse,"chief reaches real parry window with prior floor warning");
                var beat=ViewSim.Threats(0).First(t=>t.ParryWindow);
                Check(_pitFuses.Any(f=>f.threat.ActorId==beat.ActorId&&f.word.key.StartsWith("PARRY|")),"floor PARRY word appears only at real window");
                Capture("pit-22-parry-window");int bars=ViewSim.ParryBars(0);
                Check(Click("order-0"),"actual Hold button accepts live parry input");HudTicks(beat.RealBeatTicks);
                Check(ViewSim.ParryBars(0)==bars+1&&_pitSunFlight.enabled,"real parry confirms sun flight and meter");
                Capture("pit-23-parry-success");
                var fake=new Threat("Heavy",ThreatTier.Basic,"enemy","ally",1,20,ThreatAnswer.Parry,"",1,true,false,false,false,6);
                var wait=new Threat("Heavy",ThreatTier.Basic,"enemy","ally",1,20,ThreatAnswer.Parry,"",1,false,true,false,false,6);
                Check(PitCueStep(fake)<3&&PitCueStep(wait)==0,"fake and learned wait never emit press-now cue");
                IntegrationCap(39);_campaign=FreshRoadPreview();_fight=_campaign.StartEncounter(0);BeginFight();_pilotOn=false;_paused=false;_speed=0;yield return null;
                guard=0;
                while(!ViewOver&&!ViewSim.Threats(0).Any(t=>t.RealBeatTicks==15&&(t.Answers&ThreatAnswer.Parry)!=0)&&guard++<600)HudTicks(1);
                Check(guard<600&&!ViewOver,"reaches early-Hold test beat");
                Check(Click("order-0"),"early Hold button input accepted");HudTicks(15);
                Check(ViewSim.WhiffTicksLeft(0)>0,"early Hold really whiffs");
                foreach(var f in _figures.Where(f=>f.unit.Side==0&&f.unit.Alive))
                    Check(f.hitMaterial.GetFloat(CounterRim)==1&&f.hitMaterial.GetColor(CounterColor)==(Color)PxRed,"whiff outline on "+f.unit.Id);
                Capture("pit-24-whiff");
            }
            IntegrationCap(37);_campaign=FreshRoadPreview();_fight=_campaign.StartEncounter(0);BeginFight();_pilotOn=false;_paused=true;yield return null;
            Check(!PitPresentation&&_figures.All(f=>f.pit==null),"rules37 keeps original presentation path");
            Capture("pit-legacy37");
            if(Argument("-ksPitBeasts")!=null)yield return ValidatePitBeasts();
            string legacyPath=Argument("-ksPitLegacyFile");
            if(legacyPath!=null)
            {
                var legacy=CampaignSave.Read(File.ReadAllText(legacyPath));
                string before=CampaignSave.Write(legacy);int checkedFights=0;
                foreach(var archived in legacy.Archive.Fights)
                {
                    Check(legacy.Archive.TryReplay(archived.FightId,out _,out var failure),"existing owner archive replays: "+archived.FightId+" "+failure);
                    checkedFights++;yield return null;
                }
                Check(checkedFights>0&&CampaignSave.Write(legacy)==before,"existing owner archive checks do not change the loaded save");
            }
        }
        Campaign PitBeastFixture(BeastKit kit)
        {
            string camp=File.ReadAllText(Path.Combine(Application.streamingAssetsPath,"ReviewRoute","camp.txt"));
            // Select a reproducible rolled-node UI fixture, without running fights or tuning numbers.
            for(int run=0;run<128;run++)
            {
                string[] lines=camp.Split('\n');
                for(int i=0;i<lines.Length;i++)if(lines[i].StartsWith("counters\t",StringComparison.Ordinal))
                {var fields=lines[i].Split('\t');fields[3]=run.ToString();lines[i]=string.Join("\t",fields);}
                var c=CampaignSave.Read(string.Join("\n",lines),ReviewTuning);c.ChooseTribe(CampaignTribes.BloodridgeId);
                if(c.Offers.Any(e=>e.Kind==EncounterKind.Beast&&e.Beast==kit))return c;
            }
            throw new InvalidOperationException("No first-row "+kit+" fixture found");
        }
        IEnumerator ValidatePitBeasts()
        {
            foreach(var kit in new[]{BeastKit.SandShark,BeastKit.CageBear})
            {
                IntegrationCap(Argument("-ksPairs45Validate")!=null?45:39);_playback=null;_fight=null;_campaign=PitBeastFixture(kit);_speed=0;
                _savePath=Path.Combine(_capture,"beast-"+kit+"-save.txt");
                SetPage(Page.Offers);yield return null;
                var node=_campaign.NextNodes.First(n=>n.Beast==kit);
                int offer=Enumerable.Range(0,_campaign.Offers.Count).First(i=>_campaign.Offers[i].Beast==kit);
                Check(_campaign.BeastOffer==null&&!_buttons.ContainsKey("beast-offer"),kit+": legacy side hunt absent");
                Check(NodeTitle(node)==BeastKits.Name(kit)&&NodeDetail(node,NodeLit,_campaign.Offers[offer]).texts.Any(t=>t.Contains("TIER 1")),kit+": rolled road node names beast and camp tier");
                Capture("beast39-"+kit+"-road");
                Check(Click("offer-"+offer),kit+": enter beast through actual road button");yield return null;
                Check(BeastLossText(SelectedEncounter).Contains("-1 life"),kit+": preflight explains real life penalty");
                Capture("beast39-"+kit+"-squad");
                Check(Click("start-fight"),kit+": squad button starts encounter");yield return null;
                _pilotOn=false;_paused=false;_speed=0;HudTicks(60);
                Check(_fight.Encounter.BeastTier==1&&_beastState.key.Contains("TIER 1"),kit+": fight uses persisted encounter tier");
                if(Argument("-ksPairs45Validate")!=null)Check(!BeatsOn,kit+": current unit attacks never join the old serial presentation queue");
                Capture("beast39-"+kit+"-fight");
                string save=CampaignSave.Write(_campaign);var read=CampaignSave.Read(save,ReviewTuning);
                Check(read.Active!=null&&read.Active.Sim.OrderedStateSnapshot==ViewSim.OrderedStateSnapshot,kit+": partial fight reload is exact");
                for(int tick=0;tick<4000&&!ViewOver;tick++){HudTicks(1);if(tick%120==0)yield return null;}
                Check(ViewOver,kit+": unchanged no-order fixture reaches a result");
                ShowResult();yield return null;
                Check(_result.Encounter.BeastTier==1&&_result.BeastTierAfter==( _result.Won?2:1),kit+": result tier matches authoritative outcome");
                string result=ResultLine(_result);
                Check(result.Contains("lives "+_result.LivesLeft+"/3")&&!result.Contains("unchanged"),kit+": result reports actual lives and road outcome");
                string fightId=_campaign.Archive.Fights.Last().FightId;
                Check(FightPlayback.TryOpen(_campaign.Archive,fightId,out var replay,out var failure),kit+": archived beast opens: "+failure);
                while(!replay.AtEnd&&replay.Failure.Length==0)replay.Step();
                Check(replay.Failure.Length==0,kit+": archived new beast replay completes");
                Capture("beast39-"+kit+"-result");
                WatchFight(_campaign.Archive.Fights.Last());yield return null;
                SeekPlayback(Math.Min(150,_playback.Length));yield return null;
                Check(_playback.Failure.Length==0&&PitPresentation,kit+": Pit replay seeks without state mismatch");
                Capture("beast39-"+kit+"-replay");
                SeekPlayback(_playback.Length);yield return null;
                Check(_playback.Matched,kit+": full UI replay preserves recorded outcome");
            }
        }
    }
}
