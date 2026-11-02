using System;
using System.Collections;
using System.IO;
using System.Linq;
using KindredSiege.Sim;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KindredSiege.Slice
{
    public sealed partial class CampaignArtLoop
    {
        // Fixed integration scenarios, real UI controls and authoritative state. No tuning or seed sweep.
        void IntegrationCap(int cap)
        {
            _sprintReview=cap>=26;
            _floorReview=cap>=23;
            _meterReview=cap>=22;
            _dangerReview=cap>=21;
            _payoffReview=cap>=20;
            _palRoadReview=cap>=19;_themeReview=cap>=18;
            _roadReview=_gatesReview=true;
            _layersReview=cap>=17;_butcherReview=cap>=15;_squadReview=cap>=14;
            _clearReview=cap>=13;_kitReview=cap>=12;
        }

        void QaChooseDuelist()
        {
            if(_page==Page.Squad&&SelectedEncounter.Kind==EncounterKind.Duel&&_squad.Count==0)
                Check(Click("fighter-"+_campaign.Reserve[0].Id),"test explicitly chooses its duelist through the real row");
        }

        IEnumerator ValidateIntegration()
        {
            yield return null; // The title needs its first layout before a fresh-process raycast.
            string startupSave=_savePath;
            if(Argument("-ksIntegrationReopen")!=null)
            {
                string expected=File.ReadAllText(Path.Combine(_capture,"process-expected.txt"));
                Check(Click("continue"),"second process Continue raycast");_paused=true;_pilotOn=false;yield return null;
                Check(_fight.RulesVersion==17&&_fight.Sim.LayersOn,"second process keeps layers17");
                Check(CampaignSave.Write(_campaign)==expected,"fresh process restores complete saved fight and commands");
                Capture("process-reopened");yield break;
            }
            string fixtures=Argument("-ksIntegrationFixtures");
            foreach(int cap in new[]{11,12,13,14,15,17,18,19,20,21,22,23})
            {
                IntegrationCap(cap);_savePath=Path.Combine(_capture,"cap-"+cap+".txt");
                NewReviewRoute();yield return null;
                Check(_campaign.Tuning.RulesCap==cap,"NEW retains route cap "+cap);
                ShowTitle();yield return null;Check(Click("continue"),"CONTINUE route cap "+cap);yield return null;
                Check(_campaign.Tuning.RulesCap==cap,"CONTINUE retains future encounter cap "+cap);
                Check(Click("offer-0"),"choose route encounter "+cap);yield return null;
                if(SelectedEncounter.Kind==EncounterKind.Duel)Check(Click("fighter-"+_campaign.Reserve[0].Id),"explicit historical duelist "+cap);
                Check(Click("start-fight"),"start capped encounter "+cap);_pilotOn=false;_paused=true;yield return null;
                Check(_fight.RulesVersion==cap&&_fight.Sim.LayersOn==(cap>=17),"battle uses selected cap "+cap);
                HudTicks(10);Save();string state=_fight.Sim.OrderedStateSnapshot;
                _fight=null;_campaign=null;ShowTitle();yield return null;
                Check(Click("continue"),"continue active capped encounter "+cap);_paused=true;_pilotOn=false;yield return null;
                Check(_fight.RulesVersion==cap&&_fight.Sim.OrderedStateSnapshot==state,"active save keeps exact historical setup and tick "+cap);
            }

            IntegrationCap(17);_savePath=Path.Combine(_capture,"old-active-in-new-route.txt");
            _campaign=CampaignSave.Read(File.ReadAllText(Path.Combine(fixtures,"active15.txt")));Save();
            string oldState=_campaign.Active.Sim.OrderedStateSnapshot;_fight=null;ContinueSaved();_paused=true;_pilotOn=false;
            Check(_campaign.Tuning.RulesCap==17&&_fight.RulesVersion==15&&_fight.Sim.OrderedStateSnapshot==oldState,
                "new route retains persisted rules15 active battle while future fights use17");
            yield return null;

            foreach(var command in new[]{Directive.FocusFire,Directive.FallBack,Directive.Unleash})
            {
                _campaign=CampaignSave.Read(File.ReadAllText(Path.Combine(fixtures,"active15.txt")));
                _fight=null;_playback=null;_notice="";GoToStage();_pilotOn=false;_paused=true;yield return null;
                bool reached=false;
                while(!_fight.Over&&_fight.Sim.CurrentTick<4000)
                {
                    RefreshFight();DrawBattle();var sim=_fight.Sim;
                    if(_butcherFooterShown&&sim.CanUse(0,command,out _))
                    {
                        var target=sim.Units.FirstOrDefault(u=>u.Side==1&&u.Alive&&!u.Benched&&!u.Fled&&u.Row==Row.Front);
                        if(command!=Directive.FocusFire||target!=null)
                        {
                            _paused=false;if(command==Directive.FocusFire)Focus(target);else Order(command);_paused=true;
                            RefreshFight();DrawBattle();
                            Check(_orderReads.Last().Order==command&&!_butcherFooterShown&&_pxBanner.root.gameObject.activeSelf&&
                                _pxBanner2.root.gameObject.activeSelf&&_pxBanner.key.Contains(OrderName(command).ToUpperInvariant()),
                                "visible command receipt wins real simultaneous combo overlap: "+command);
                            Capture("receipt-"+command);long issue=sim.CurrentTick;
                            while(!_fight.Over&&sim.CurrentTick<6000){HudTicks(1);if(sim.CurrentTick%120==0)yield return null;}
                            Check(_fight.Over,"overlap recording reaches actual result "+command);
                            ShowResult();yield return null;var record=_campaign.Archive.Fights.Last();
                            WatchFight(record,0,record.Ticks);yield return null;SeekPlayback(issue+1);yield return null;
                            Log("Recorded overlap "+command+": clean="+CleanCombatHud+" footer="+_butcherFooterShown+" banner="+_pxBanner.key);
                            Capture("recorded-receipt-"+command);
                            Check(CleanCombatHud&&!_butcherFooterShown&&_pxBanner.root.gameObject.activeInHierarchy&&
                                _pxBanner.key.StartsWith("RECORDED / ")&&_pxBanner.key.Contains(OrderName(command).ToUpperInvariant()),
                                "clean replay visibly identifies recorded command instead of generic status "+command);
                            SeekPlayback(record.Ticks);yield return null;
                            Check(_playback.Matched&&_pxBanner.key.StartsWith("VERIFIED"),"end verification retains priority "+command);
                            LeavePlayback();CloseModal();reached=true;break;
                        }
                    }
                    HudTicks(1);if(sim.CurrentTick%120==0)yield return null;
                }
                Check(reached,"reproduced actual overlap case "+command);
            }

            string duelSave=File.ReadAllText(Path.Combine(fixtures,"skullpeak-duel.txt"));
            foreach(UnitClass cls in new[]{UnitClass.Warden,UnitClass.Skirmisher,UnitClass.Trapper,UnitClass.Marksman,UnitClass.Medic})
            {
                _fight=null;_playback=null;_campaign=CampaignSave.Read(duelSave,new CampaignTuning{RulesCap=17});
                _savePath=Path.Combine(_capture,"duel-"+cls+".txt");_notice="";SetPage(Page.Offers);yield return null;
                int offer=_campaign.Offers.ToList().FindIndex(e=>e.Kind==EncounterKind.Duel);
                Check(offer>=0&&Click("offer-"+offer),"open real duel card "+cls);yield return null;
                Check(_squad.Count==0&&!_buttons["start-fight"].interactable&&!Click("start-fight"),"duel has no implicit fighter "+cls);
                Check(EventSystem.current.currentSelectedGameObject==_buttons["fighter-"+_campaign.Reserve[0].Id].gameObject,
                    "keyboard starts on an available fighter "+cls);
                if(cls==UnitClass.Warden)Capture("duel-unselected");
                var pick=_campaign.Reserve.First(f=>f.Class==cls);
                var other=_campaign.Reserve.First(f=>f.Id!=pick.Id);
                Check(Click("fighter-"+other.Id),"choose first duelist "+cls);yield return null;
                Check(Click("fighter-"+pick.Id),"one click replaces the chosen duelist "+cls);yield return null;
                Check(_squad.Count==1&&_squad[0]==pick.Id&&_buttons["start-fight"].interactable,"exactly selected duelist can fight "+cls);
                Check(Click("squad-details"),"duel details opens "+cls);yield return null;
                CloseModal();yield return null;
                Check(_squad.Single()==pick.Id,"details keeps explicit choice "+cls);
                Capture("duel-selected-"+cls);
                Check(Click("start-fight"),"start chosen duel "+cls);_pilotOn=false;_paused=true;yield return null;
                Check(_fight.RulesVersion==17&&_fight.Setup.Fight.ChampionDuel&&_fight.Sim.LayersOn&&
                    _fight.Setup.Side(0).Count==1&&_fight.Setup.Side(0)[0].Id==pick.Id&&_figures.Count==2,"duel uses selected fighter and handed back rules "+cls);
                Check(FighterLabel(_fight.Setup.Side(1)[0].Id)=="ENEMY CHAMPION","duel threat identifies the champion "+cls);
                bool cue=false,parry=false;
                for(int tick=0;tick<3000&&!_fight.Over;tick++)
                {
                    var sim=_fight.Sim;var threat=sim.Threats(0).FirstOrDefault(t=>t.Name==HeavyRules.Heavy);
                    if(!string.IsNullOrEmpty(threat.Name)&&threat.ParryWindow&&sim.CanUse(0,Directive.Hold,out _))
                    {
                        _paused=false;RefreshFight();DrawBattle();
                        if(!cue){Check(threat.Duel&&_parryCall.key.Contains("HOLD NOW"),"visible authoritative duel parry cue "+cls);Capture("duel-cue-"+cls);cue=true;}
                        Check(Click("order-0"),"live parry plate accepts the duel beat "+cls);_paused=true;
                    }
                    HudTicks(1);
                    if(!parry&&sim.Events.Any(e=>e.Kind==BattleEventKind.Parry&&e.ActorSide==0))
                    {parry=true;Capture("duel-parried-"+cls);}
                    if(tick%60==0)yield return null;
                }
                Check(cue&&parry&&_fight.Over,"timed defence duel reaches real outcome "+cls);
                var fled=_fight.Sim.Events.FirstOrDefault(e=>e.Kind==BattleEventKind.Snap&&e.Detail==RoadRules.Fled&&e.ActorSide==1);
                if(cls==UnitClass.Medic)
                {
                    Check(fled.Detail==RoadRules.Fled&&_feed.Any(s=>s.Contains("yields. You won the duel.")),"non-attacking Medic can win on nerve with truthful yield feedback");
                    Capture("champion-yield");
                }
                string final=_fight.Sim.OrderedStateSnapshot;ShowResult();yield return null;
                var record=_campaign.Archive.Fights.Last();
                Check(_campaign.Archive.TryReplay(record.FightId,out var replay,out _)&&replay.OrderedStateSnapshot==final,
                    "layered champion duel archive reproduces full state "+cls);
                WatchFight(record,0,record.Ticks);yield return null;SeekPlayback(record.Ticks);yield return null;
                Check(_playback.Matched,"packaged duel playback matches "+cls);Capture("duel-replay-"+cls);LeavePlayback();CloseModal();
            }

            foreach(string answer in new[]{"miss","early","dodge"})
            {
                _fight=null;_playback=null;_campaign=CampaignSave.Read(duelSave,new CampaignTuning{RulesCap=17});
                _savePath=Path.Combine(_capture,"duel-"+answer+".txt");_notice="";SetPage(Page.Offers);yield return null;
                int offer=_campaign.Offers.ToList().FindIndex(e=>e.Kind==EncounterKind.Duel);
                Check(Click("offer-"+offer),answer+" scenario selects duel");yield return null;
                var pick=_campaign.Reserve.First(f=>f.Class==UnitClass.Warden);
                Check(Click("fighter-"+pick.Id),answer+" scenario explicitly selects Warden");yield return null;
                Check(Click("start-fight"),answer+" scenario starts");_pilotOn=false;_paused=true;yield return null;
                var sim=_fight.Sim;
                if(answer=="miss")
                {
                    // Let the ordinary Update clock animate and advance a missed heavy at the player's learning speed.
                    _speed=.5f;_paused=false;int frame=0,captured=0;
                    while(!_fight.Over&&frame++<360&&!sim.Events.Any(e=>e.Kind==BattleEventKind.Hit&&e.Detail==HeavyRules.HeavyDetail))
                    {
                        if(sim.HeavyTicks(1)>0&&captured<4&&frame%6==0)Capture("live-duel-motion-"+captured++);
                        yield return null;
                    }
                    _paused=true;
                    Check(captured>1&&sim.Events.Any(e=>e.Kind==BattleEventKind.Hit&&e.Detail==HeavyRules.HeavyDetail&&e.Amount>0),
                        "ordinary Update animates a missed duel heavy at half speed and applies its actual hit");
                }
                else
                {
                    int beat=answer=="early"?FightRules.PerfectHoldTicks+5:1;
                    for(int tick=0;tick<600&&!_fight.Over&&!sim.Threats(0).Any(t=>t.Name==HeavyRules.Heavy&&t.BeatTicks==beat);tick++)HudTicks(1);
                    Check(!sim.IsFinished&&sim.HeavyTicks(1)>0,answer+" reaches real heavy beat");
                    int from=sim.Events.Count;_paused=false;RefreshFight();DrawBattle();
                    Check(Click(answer=="early"?"order-0":"order-1"),answer+" uses real order plate");_paused=true;HudTicks(50);
                    Check(answer=="early"?sim.Events.Skip(from).Any(e=>e.Kind==BattleEventKind.Reaction&&e.Detail==ParryRules.BlockDetail):
                        sim.Events.Skip(from).Any(e=>e.Kind==BattleEventKind.Release&&e.Detail==Moves.Dodged),
                        answer+" produces the authoritative penalty or dodge");
                    Check(!sim.Events.Skip(from).Any(e=>e.Kind==BattleEventKind.Parry&&e.ActorSide==0),answer+" never claims a parry");
                }
                Capture("duel-answer-"+answer);Save();
                string state=sim.OrderedStateSnapshot;ContinueSaved();_paused=true;_pilotOn=false;
                Check(_fight.Sim.OrderedStateSnapshot==state,answer+" command and outcome persist exactly");yield return null;
            }

            // The existing reward/equip/effect suite now runs again with actual rules17 and layered fixture hits.
            var combos=ValidateButcher(true);while(combos.MoveNext())yield return combos.Current;
            IntegrationCap(17);_savePath=startupSave;_fight=null;_playback=null;NewReviewRoute();yield return null;
            Check(Click("offer-0"),"new17 process fixture offer");yield return null;
            if(SelectedEncounter.Kind==EncounterKind.Duel)Check(Click("fighter-"+_campaign.Reserve[0].Id),"process fixture explicit duelist");
            Check(Click("start-fight"),"new17 process fixture start");_pilotOn=false;_paused=true;yield return null;
            HudTicks(90);Save();File.WriteAllText(Path.Combine(_capture,"process-expected.txt"),CampaignSave.Write(_campaign));
            Capture("process-before-reopen");
            Log("Integration17 complete: fixed scripted UI and rendered fight scenarios; no balance sweep or native hands-on claim.");
        }
    }
}
