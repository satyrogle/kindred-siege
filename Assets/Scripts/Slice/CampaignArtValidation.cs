using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KindredSiege.Slice
{
    public sealed partial class CampaignArtLoop
    {
        // Opt-in isolated player validation. Uses the real controls and Campaign API; never owner saves.
        IEnumerator Validate()
        {
            // Unity UI event handlers log and swallow exceptions. Treat those as failures too.
            string runtimeFailure=null;
            Application.LogCallback runtimeLog=(message,stack,type)=>
            {
                if(type==LogType.Exception||type==LogType.Error||type==LogType.Assert)
                    runtimeFailure=message+"\n"+stack;
            };
            Application.logMessageReceived+=runtimeLog;
            var run=Argument("-ksSprint26Validate")!=null?ValidateSprint26():Argument("-ksPalFloorValidate")!=null?ValidatePalFloor():Argument("-ksMeterValidate")!=null?ValidateMeter():Argument("-ksReadabilityValidate")!=null?ValidateReadability():Argument("-ksThemeValidate")!=null?ValidateTheme():Argument("-ksIntegrationValidate")!=null?ValidateIntegration():Argument("-ksButcherValidate")!=null?ValidateButcher():Argument("-ksHoldRecoveryValidate")!=null?ValidateHoldRecoveryHud():Argument("-ksCommandFeedbackValidate")!=null?ValidateCommandFeedback():Argument("-ksRoad14Validate")!=null?ValidateRoad14Hud():Argument("-ksRoad13Validate")!=null?ValidateRoad13Hud():Argument("-ksRoad12Validate")!=null?ValidateRoad12Hud():Argument("-ksRoad11Validate")!=null?ValidateRoad11Hud():Argument("-ksRaidValidate")!=null?ValidateRaidHud():Argument("-ksRules9Validate")!=null?ValidateRules9Hud():Argument("-ksHudValidate")!=null?ValidateCombatHud():Argument("-ksLearningValidate")!=null?ValidateLearningCombat():Argument("-ksGatesValidate")!=null?ValidateClarity():Argument("-ksBeastValidate")!=null?ValidateBeasts():Argument("-ksTacticsValidate")!=null?ValidateChiefTactics():Argument("-ksVerdictValidate")!=null?ValidateChiefVerdict():Argument("-ksClassValidate")!=null?ValidateClassFrontend():Argument("-ksHeadlessValidate")!=null?ValidateHeadlessCampaign():Argument("-ksReplayValidate")!=null?ValidateReplayCompatibility():Argument("-ksTribesValidate")!=null?ValidateTribes():Argument("-ksFavourValidate")!=null?ValidateFavours():Argument("-ksChiefValidate")!=null?ValidateChiefSpells():Argument("-ksRegionalStageValidate")!=null?ValidateRegionalStages():Argument("-ksJobsValidate")!=null?ValidateJobs():Argument("-ksHeavyValidate")!=null?ValidateHeavyPresentation():Argument("-ksHuntersValidate")!=null?ValidateHunters():Argument("-ksSpecialArtValidate")!=null?ValidateSpecialArt():Argument("-ksResultValidate")!=null?ValidateResultScreens():Argument("-ksParryPromptValidate")!=null?ValidateParryPrompts():Argument("-ksTrailerParry")!=null?RecordTrailerParry():Argument("-ksBloodridgeReview")!=null||Argument("-ksBloodridgeValidate")!=null?RecordBloodridgeReview():Argument("-ksSprintValidate")!=null?ValidateSprint():Argument("-ksFeedbackCompare")!=null?RecordCombatFeedback():Argument("-ksFeedbackValidate")!=null?ValidateCombatFeedback():Argument("-ksInputValidate")!=null?ValidateInputFlow():Argument("-ksProgressionValidate")!=null?ValidateProgressionWiring():Argument("-ksWhyValidate")!=null?ValidateWhyWiring():ValidateRun();
            // Run nested validation enumerators here too. Unity otherwise swallows a child exception,
            // leaving the parent parked forever and never writing FAILED.txt or an exit code.
            if(Argument("-ksValidationFailureProbe")!=null)run=ValidateNestedFailureProbe();
            var runs=new Stack<IEnumerator>();runs.Push(run);
            while(runs.Count>0)
            {
                object current=null;bool more=false;
                try
                {
                    var step=runs.Peek();more=step.MoveNext();if(more)current=step.Current;
                    if(runtimeFailure!=null)throw new InvalidOperationException("Player logged a runtime error:\n"+runtimeFailure);
                }
                catch(Exception e)
                {
                    Application.logMessageReceived-=runtimeLog;
                    Debug.LogException(e);File.WriteAllText(Path.Combine(_capture,"FAILED.txt"),e.ToString()+"\n"+_log);
                    Application.Quit(1);yield break;
                }
                if(!more){runs.Pop();continue;}
                if(current is IEnumerator nested){runs.Push(nested);continue;}
                yield return current;
            }
            Application.logMessageReceived-=runtimeLog;
            File.WriteAllText(Path.Combine(_capture,Argument("-ksArtReopen")!=null?"reopen-PASS.txt":"PASS.txt"),_log.ToString());
            Application.Quit(0);
        }
        IEnumerator ValidateNestedFailureProbe(){yield return ValidateFailureChild();}
        IEnumerator ValidateFailureChild()
        {
            yield return null;
            throw new InvalidOperationException("EXPECTED nested validation failure probe");
        }
        void Check(bool condition,string what)
        {
            if(!condition)throw new InvalidOperationException("Validation failed: "+what);
            Log("PASS "+what);
        }
        void CheckStagePresentation()
        {
            bool court=ViewKind!=EncounterKind.Tutorial&&ViewTribe==CampaignTribes.BloodridgeId&&
                Argument("-ksBloodridgeBaseline")==null;
            Check(ArtScale==1,"fighters retain native 1x scale");
            if(court)
            {
                Check(_bloodridgeActive&&_stageArt==null&&_bloodridgeTextures.Count==8,
                    "Bloodridge uses its complete layered court without loading the old stage");
                Check(_bloodridgeContacts.Count==_figures.Count,"every actual fighter has a ground contact");
                foreach(var texture in _bloodridgeTextures)
                    Check(texture.filterMode==FilterMode.Point&&texture.mipmapCount==1,"native unfiltered court texture: "+texture.name);
                Check(_bloodridgeLinen.texture.width==1808&&_bloodridgeLinen.texture.height==2248&&
                    _bloodridgeDustA.width==6144&&_bloodridgeDustA.height==2400&&
                    _bloodridgeDustB.width==6144&&_bloodridgeDustB.height==1200,
                    "packaged cloth and ash sheets retain full source dimensions");
            }
            else
            {
                string region=ViewKind==EncounterKind.Tutorial?"broken_wastes":
                    ViewTribe==CampaignTribes.BloodridgeId?"bloodridge":ViewTribe==CampaignTribes.SkullpeakId?"skullpeak":"broken_wastes";
                Check(!_bloodridgeActive&&_bloodridgeTextures.Count==0&&_bloodridgeContacts.Count==0,
                    "other region has no retained Bloodridge layers or contacts");
                Check(_stageArt!=null&&_stageArt.name==region&&_stageArt.width==StageW*Px&&_stageArt.height==StageH*Px,
                    "other region keeps its own stage at 1x: "+region);
            }
        }
        bool Click(string id)
        {
            Canvas.ForceUpdateCanvases();var button=_buttons[id];
            var rect=button.GetComponent<RectTransform>();
            var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(_canvas.worldCamera,rect.TransformPoint(rect.rect.center)),button=PointerEventData.InputButton.Left};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
            if(!button.interactable||hits.Count==0||hits[0].gameObject.GetComponentInParent<Button>()!=button)return false;
            ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);return true;
        }
        void Capture(string filename)
        {
            // A fight capture shows the fight as it stands, orders given this frame included.
            if(_page==Page.Fight&&_fight!=null){RefreshFight();DrawBattle();}
            if(_page==Page.Playback){RefreshPlayback();DrawBattle();}
            Canvas.ForceUpdateCanvases();Camera.main.Render();var previous=RenderTexture.active;RenderTexture.active=_captureTarget;
            _capturePixels.ReadPixels(new UnityEngine.Rect(0,0,W,H),0,0);_capturePixels.Apply();RenderTexture.active=previous;
            File.WriteAllBytes(Path.Combine(_capture,filename+".png"),_capturePixels.EncodeToPNG());
        }
        IEnumerator ValidateRun()
        {
            yield return null;Capture("00-title");
            if(Argument("-ksArtReopen")!=null)
            {
                Check(Click("continue"),"continue button raycast in second process");yield return null;
                Check(CampaignSave.Write(_campaign)==File.ReadAllText(Path.Combine(_capture,"expected-save.txt")),"actual process restart preserves roster, materials, codex and learned state");
                Capture("90-reopened");yield break;
            }
            Check(Click("new"),"new campaign button raycast");yield return null;Capture("01-tutorial");
            Check(Click("start-tutorial"),"tutorial start button raycast");yield return null;
            Check(Click("pause"),"pause button raycast");long tick=_fight.Sim.CurrentTick;
            for(int i=0;i<5;i++)yield return null;
            Check(_fight.Sim.CurrentTick==tick,"pause freezes simulation");
            _pilotOn=false;RefreshFight();
            Check(!_buttons["order-4"].interactable&&!_fight.TryOrder(Directive.Eclipse,null,out _),"key 5 is gated chief assist before recruitment");
            Check(Click("order-2"),"Focus Fire button raycast");
            BattleUnit enemy=null;foreach(var u in _fight.Sim.Units)if(u.Side==1&&u.Alive){enemy=u;break;}
            int points=_fight.Sim.DirectivePoints(0);
            Check(Click("target-"+enemy.Id),"enemy target button raycast");
            Check(_fight.Sim.DirectivePoints(0)<points,"Focus Fire goes through campaign order API");
            Capture("02a-tutorial-focus-paused");
            Check(Click("pause"),"resume button raycast");_pilotOn=true;_speed=12;
            Capture("02-tutorial-fight");
            yield return null;Capture("02b-tutorial-orders");
            // Codex re-audit, 10 Oct 2026: the damage chip drained once per drawn frame, so it kept falling
            // while paused and on every capture. It now follows the fight's own clock.
            _paused=true;var probe=_figures[0];probe.shownHp=probe.unit.Hp+probe.unit.MaxHp*.3f;probe.lagUntil=-1;
            float chipBefore=probe.shownHp;
            for(int i=0;i<10;i++){DrawBattle();yield return null;}
            Capture("02c-tutorial-paused-chip");
            Check(Mathf.Approximately(probe.shownHp,chipBefore),"the damage chip holds while paused and when captured");
            _paused=false;
            int guard=0;while(!_fight.Over&&guard++<1800)yield return null;
            Check(_fight.Over,"tutorial completes");
            for(int i=0;i<76;i++)yield return null;
            Check(Click("see-result"),"result button raycast");yield return null;Capture("03-tutorial-result");
            Check(_result.TutorialWon,"tutorial earned alliance");
            Check(Click("after-result"),"result continue button raycast");yield return null;Capture("04-choose-tribe");
            int encounters=0;bool carried=false;
            while(_campaign.RunsStarted<2&&_campaign.Stage==CampaignStage.ChooseTribe)
            {
                var choice=_campaign.Choosable()[0];Check(Click("tribe-"+choice.Id),"choose "+choice.Name);yield return null;
                // 13 Oct 2026: roads of 3 rows make two whole runs 18 to 23 fights for the reference pilot.
                while(_campaign.Stage==CampaignStage.InRun&&encounters<30)
                {
                    Capture("offers-"+encounters);
                    int offer=new CampaignPilot(PilotStyle.Reference,21).PickOffer(_campaign);
                    Check(Click("offer-"+offer),"encounter card raycast");yield return null;
                    Capture("squad-"+encounters);
                    Check(Click("start-fight"),"squad fight button raycast");yield return null;
                    Capture("fight-"+encounters+"-"+_fight.Encounter.Kind);
                    CheckStagePresentation();
                    guard=0;while(!_fight.Over&&guard++<2400)yield return null;
                    Check(_fight.Over,"encounter completes");
                    for(int i=0;i<76;i++)yield return null;
                    Check(Click("see-result"),"encounter result opens");yield return null;
                    Capture("result-"+encounters);encounters++;
                    // 10 Oct 2026 runs: a beaten chief's tribe and order stay for the rest of the run, which goes on to the next tribe.
                    if(_result.AssistUnlocked!=null&&!_result.RunOver)
                    {
                        Check(_result.NextTribe!=null&&_campaign.Target!=null&&_campaign.Target.Id==_result.NextTribe.Id&&_campaign.Step==0&&_campaign.Assists.Count>0,
                            "a beaten chief gives his order and the run goes on to the next tribe");
                        carried=true;
                    }
                    Check(Click("after-result"),"continue after encounter");yield return null;
                }
            }
            Check(_campaign.Stage==CampaignStage.ChooseTribe&&_campaign.RunsStarted>=2,"two whole runs end back at camp");
            Check(carried,"a run carried on past a beaten chief");
            Check(_campaign.Assists.Count==0&&_campaign.Joined.Count==1&&_campaign.Materials>0,"camp keeps the materials; chief orders and joined tribes reset with the run");
            File.WriteAllText(Path.Combine(_capture,"expected-save.txt"),CampaignSave.Write(_campaign));
            // The save on disk is sealed by Codex's safe writer (11 Oct 2026), so it is read back, not compared as text.
            Check(CampaignSave.TryReadFile(_savePath,null,out Campaign onDisk,out _)&&CampaignSave.Write(onDisk)==CampaignSave.Write(_campaign),
                "between-run save matches live campaign");
            Capture("20-runs-complete");
            // 11 Oct 2026, the safe save: a damaged or missing save opens the last good backup, says so, and is never
            // replaced by a new campaign. On its own file, so the saved campaign above is untouched.
            Campaign beforeDamage=_campaign;
            _savePath=Path.Combine(_capture,"recovery-save.txt");Save();Save();
            Check(File.Exists(_savePath+".bak"),"a second save keeps the first as a backup");
            File.WriteAllText(_savePath,"damaged");
            _campaign=null;_fight=null;ShowTitle();yield return null;
            Check(_notice.StartsWith(CampaignSave.BackupLoadedNote,StringComparison.Ordinal),"the title says the backup was opened");
            Capture("37-backup-title");
            Check(Click("continue"),"CONTINUE opens the backup when the save is damaged");yield return null;
            Check(_campaign!=null&&_campaign.LoadNote.StartsWith(CampaignSave.BackupLoadedNote,StringComparison.Ordinal)&&
                CampaignSave.Write(_campaign)==CampaignSave.Write(beforeDamage),"the backup holds the campaign as it was");
            Capture("38-backup-continue");
            File.Delete(_savePath);_campaign=null;_fight=null;ShowTitle();yield return null;
            Check(Click("continue")&&_campaign!=null,"CONTINUE opens the backup when the save is missing");yield return null;
            // 10 Oct 2026: every fight and order now saves, so the checks below save to their own files and
            // never touch the saved campaign. First: a fight left half played comes back at the same tick, paused.
            _savePath=Path.Combine(_capture,"resume-save.txt");_campaign=null;_fight=null;SetPage(Page.Title);yield return null;
            Check(Click("new"),"new campaign for the resume check");yield return null;
            Check(Click("start-tutorial"),"start a fight to leave half played");yield return null;
            _pilotOn=true;_speed=12;for(int i=0;i<20;i++)yield return null;
            Save();long leftAt=_fight.Sim.CurrentTick;string leftAs=_fight.Session.Checksum;
            Check(leftAt>0&&!_fight.Over,"the fight is half played when saved");
            _fight=null;_campaign=null;SetPage(Page.Title);yield return null;
            Check(Click("continue"),"continue into the half played fight");yield return null;
            Check(_page==Page.Fight&&_fight!=null&&_paused,"reopens inside the fight, paused");
            Check(_fight.Sim.CurrentTick==leftAt&&_fight.Session.Checksum==leftAs,"the fight comes back at the same tick, unchanged");
            Capture("21-resumed-fight");
            _fight=null;SetPage(Page.Title);yield return null;
            // Coverage fixtures are replayed exclusively through public APIs and save to their own file.
            _savePath=Path.Combine(_capture,"isolated-fixture-save.txt");
            var covered=new HashSet<string>();bool assistTested=false,runWon=false,mixed=false;
            for(ulong seed=1;seed<=24&&(!AllCovered(covered)||!assistTested||!runWon||!mixed);seed++)
            {
                var c=Campaign.New(seed);if(!CampaignPilot.PlayTutorial(c,PilotStyle.Reference,seed))continue;
                for(int runIndex=0;runIndex<5&&c.Stage==CampaignStage.ChooseTribe;runIndex++)
                {
                    string tribe=c.Choosable()[0].Id;string beforeRun=CampaignSave.Write(c);
                    c.ChooseTribe(tribe);var path=new List<int>();
                    while(c.Stage==CampaignStage.InRun&&path.Count<10)
                    {
                        for(int i=0;i<c.Offers.Count;i++)
                        {
                            // A run crosses tribes now, so trials and chiefs are keyed by the tribe being fought.
                            var e=c.Offers[i];string key=e.Kind==EncounterKind.Trial||e.Kind==EncounterKind.Chief?e.Tribe.Id+"-"+e.Kind:e.Kind.ToString();
                            if(covered.Contains(key)&&!(c.Assists.Count>0&&!assistTested)&&!(c.Assists.Count>0&&!mixed))continue;
                            Check(CampaignSave.TryRead(beforeRun,null,out Campaign fixture,out _),"read fixture seed "+seed);
                            fixture.ChooseTribe(tribe);
                            for(int p=0;p<path.Count;p++)CampaignPilot.PlayFight(fixture,fixture.StartEncounter(path[p]),new CampaignPilot(PilotStyle.Reference,seed*31+(ulong)p));
                            _campaign=fixture;TakeOffer(i);StartEncounterFight();_pilotOn=false;_paused=true;RefreshFight();yield return null;
                            // 11 Oct 2026: the stage is on the 2x grid now, so its bounds come from the stage itself.
                            foreach(var figure in _figures)Check(figure.feet.y>=(StageY+60)*Px&&figure.feet.y<=(StageY+StageH)*Px&&figure.feet.x>=StageX*Px&&figure.feet.x<=(StageX+StageW)*Px,"formation stays on stage: "+figure.unit.Id);
                            if(e.NoFallBack)Check(!_buttons["order-1"].interactable&&!_fight.TryOrder(Directive.FallBack,null,out _),"hold-line disables Fall Back in UI and API");
                            if(_fight.CanCallAssist&&!assistTested)
                            {
                                Check(Click("order-4"),"earned chief assist button raycast");
                                Check(_fight.AssistUsed&&!_fight.TryOrder(Directive.Eclipse,null,out _),"assist is once per fight");assistTested=true;
                            }
                            foreach(var f in _figures)if(f.unit.Side==0&&f.artKey!=null&&!f.artKey.StartsWith("clarth"))mixed=true;
                            _paused=false;_pilotOn=true;_speed=1;
                            for(int frame=0;frame<75;frame++)
                            {
                                yield return null;
                                if(e.Kind==EncounterKind.Chief&&e.Tribe.Id==CampaignTribes.SkullpeakId&&frame%2==0)Capture("motion-"+(frame/2).ToString("D4"));
                            }
                            Capture("coverage-"+key);covered.Add(key);
                            Log("COVERAGE "+key+" art="+string.Join(",",_figures.ConvertAll(f=>f.artKey??"labelled-placeholder")));
                            _fight=null;SetPage(Page.Title);yield return null;
                        }
                        int pick=new CampaignPilot(PilotStyle.Reference,seed).PickOffer(c);
                        CampaignPilot.PlayFight(c,c.StartEncounter(pick),new CampaignPilot(PilotStyle.Reference,seed*31+(ulong)path.Count));path.Add(pick);
                    }
                    // The slice is one run now: win it and you are back at camp, keeping what camp keeps.
                    if(c.RunsWon>0&&!runWon)
                    {
                        runWon=true;_campaign=c;SetPage(Page.ChooseTribe);yield return null;Capture("coverage-run-won");
                    }
                }
            }
            Check(AllCovered(covered),"war party, hold line, duel, both trials and both chiefs rendered");
            Check(assistTested,"earned assist exercised through UI");Check(mixed,"recruited tribe uses its own art in player squad");
            Check(runWon,"a whole slice run won and shown back at camp");
            // The Sprint 2 and Sprint 3 controls first, each on its own campaign, so the lost run below still leaves the
            // campaign at camp for the checks after it.
            string savedPath=_savePath;
            var screens=SprintTwoScreens();while(screens.MoveNext())yield return screens.Current;
            var road=SprintThreeScreens();while(road.MoveNext())yield return road.Current;
            var readouts=FixNineScreens();while(readouts.MoveNext())yield return readouts.Current;
            _savePath=Path.Combine(_capture,"isolated-loss-save.txt");
            // 13 Oct 2026, your roster is your lives: a loss costs the fallen and one of yours captured, and the run
            // ends only when you cannot field Campaign.MinSquad.
            bool loss=false,runLost=false;
            for(ulong seed=1;seed<=8&&!runLost;seed++)
            {
                _campaign=Campaign.New(seed);if(!CampaignPilot.PlayTutorial(_campaign,PilotStyle.Reference,seed))continue;
                _campaign.ChooseTribe(_campaign.Choosable()[0].Id);
                for(int n=0;n<16&&_campaign.Stage==CampaignStage.InRun;n++)
                {
                    TakeOffer(0);StartEncounterFight();_pilot=new CampaignPilot(PilotStyle.Passive,seed);_pilotOn=true;_speed=24;
                    int frames=0;while(!_fight.Over&&frames++<1800)yield return null;
                    Check(_fight.Over,"passive validation encounter ends");
                    for(int i=0;i<76;i++)yield return null;
                    Check(Click("see-result"),"loss scenario result control");yield return null;
                    if(!_result.Won)
                    {
                        if(!loss){loss=true;Capture("coverage-defeat");}
                        // 11 Oct 2026: the count and the names come from one list, your leader included.
                        Check(_lastFallen.Count==_lastLosses,"the fallen count and the fallen names agree");
                        if(_result.Captured!=null)
                            Check(string.Join(" ",ResultFighterLines(_result,false).texts).Contains(_result.Captured.Name),"the result names who they captured");
                        Check(_result.RunOver==(_result.FightersLeft<Campaign.MinSquad),"a loss ends the run only under "+Campaign.MinSquad+" fighters");
                        Check(File.Exists(_savePath),"defeat saves");
                        if(_result.RunOver){runLost=true;Capture("coverage-run-lost");}
                    }
                    Check(Click("after-result"),"the result returns to the campaign");yield return null;
                    if(runLost)Capture("coverage-after-defeat");
                }
            }
            Check(loss&&runLost,"a loss captures one of yours and the run ends on its lives");
            _savePath=savedPath;
            // 11 Oct 2026: every result shows all of its text at once, and the popups are on the kit.
            Check(_resultsShown>0&&_resultScrolls==0,"all "+_resultsShown+" results show their text without scrolling");
            SetPage(Page.Title);yield return null;Capture("29-title");
            Check(Click("missing"),"art placeholders open from the title");yield return null;Capture("30-missing-art");
            Check(Click("modal-close"),"a popup closes");yield return null;
            Check(_modal==null,"the popup is gone");
            SetPage(Page.ChooseTribe);yield return null;
            Check(Click("codex"),"codex opens from camp");yield return null;Capture("31-codex");
            Check(Click("modal-close"),"codex closes");yield return null;
            Check(Click("tribe-"+_campaign.Choosable()[0].Id),"a run starts for the tactics check");yield return null;
            Check(Click("offer-0"),"a fight is chosen");yield return null;
            Check(Click("start-fight"),"the fight starts");yield return null;
            Check(Click("tactics"),"tactics opens in the fight");yield return null;Capture("32-tactics");
            Check(Click("modal-close"),"tactics closes");yield return null;
            // 11 Oct 2026 sprint: the tutorial walks you through the orders, the fight says why an order does
            // not land, and the end of a fight says why it was won or lost.
            _coachTest=true;_campaign=Campaign.New(7);SetPage(Page.Tutorial);yield return null;Capture("33-tutorial-page");
            Check(Click("start-tutorial"),"tutorial starts for the walkthrough check");_pilotOn=false;RefreshFight();yield return null;
            Check(_paused&&_coachStep==0,"the tutorial stops and asks for Hold first");Capture("34-tutorial-step-hold");
            Check(Click("order-0"),"Hold given through the walkthrough");yield return null;
            Check(!_paused&&_coachStep==1,"Hold moves the tutorial on and the fight carries on");
            BattleUnit far=null;foreach(var u in _fight.Sim.Units)if(u.Side==1&&u.Alive&&u.Row==Row.Back){far=u;break;}
            if(far!=null&&_fight.Sim.StrikersOn(far,out int living)<living)
            {
                Check(_fight.TryOrder(Directive.FocusFire,far.Id,out _),"Focus Fire on their back row");
                DrainEvents();DrawBattle();RefreshFight();
                bool tagged=false;foreach(var f in _figures)if(f.unit.Side==0&&f.tag.key.Contains("CAN'T REACH"))tagged=true;
                Check(tagged&&_flashText[0].StartsWith("ONLY ",StringComparison.Ordinal),"the fight says who cannot reach a back row target");
                Capture("35-focus-out-of-reach");
            }
            _coachStep=-1;_coachTest=false;_pilotOn=true;_speed=12;_paused=false;
            int whyGuard=0;while(!_fight.Over&&whyGuard++<2400)yield return null;
            for(int i=0;i<45;i++)yield return null;
            Check(_fight.Over&&_whyPanel!=null,"the end of the fight says why it went that way");Capture("36-why-panel");
            CheckStagePresentation();
            Log("144 approved clips available; no generation calls. Death holds final frame; names show when you point at a fighter.");
            File.WriteAllText(Path.Combine(_capture,"coverage.txt"),string.Join("\n",covered));
        }
        static bool AllCovered(HashSet<string> s)=>s.Contains("WarParty")&&s.Contains("HoldTheLine")&&s.Contains("Duel")&&
            s.Contains(CampaignTribes.BloodridgeId+"-Trial")&&s.Contains(CampaignTribes.SkullpeakId+"-Trial")&&
            s.Contains(CampaignTribes.BloodridgeId+"-Chief")&&s.Contains(CampaignTribes.SkullpeakId+"-Chief");

        // Focused presenter regression. All campaigns and saves belong to the validator directory.
        IEnumerator ValidateWhyWiring()
        {
            _campaign=Campaign.New(7);_fight=_campaign.StartTutorial();BeginFight();
            _pilotOn=false;_paused=true;_coachStep=-1;
            yield return null;
            BattleUnit far=null,front=null;
            foreach(var u in _fight.Sim.Units)if(u.Side==1&&u.Alive)
            {
                if(u.Row==Row.Back)far=u;else front=u;
            }
            Check(far!=null&&front!=null,"reach fixture has both enemy rows");
            Check(_fight.TryOrder(Directive.FocusFire,far.Id,out _),"back-row focus accepted");
            DrainEvents();DrawBattle();RefreshFight();
            int reach=_fight.Sim.StrikersOn(far,out int living);
            Check(reach<living&&_flashText[0]=="ONLY "+reach+" OF "+living+" CAN STRIKE NOW","focus callout matches current simulation count");
            CheckWhyTags("back-row focus");Capture("why-01-front-line");
            Check(_fight.TryOrder(Directive.FallBack,null,out _),"Fall Back accepted during focus");
            DrawBattle();CheckWhyTags("Fall Back");
            bool apart=false;foreach(var f in _figures)if(f.tag.key.Contains("LINES APART"))apart=true;
            Check(apart,"melee separation is explained during Fall Back");Capture("why-02-fall-back");
            Check(_fight.TryOrder(Directive.FocusFire,front.Id,out _),"front-row focus accepted during Fall Back");
            DrainEvents();DrawBattle();RefreshFight();
            reach=_fight.Sim.StrikersOn(front,out living);
            Check(reach<living&&_flashText[0]=="ONLY "+reach+" OF "+living+" CAN STRIKE NOW","front-row callout also respects Fall Back");
            CheckWhyTags("front focus during Fall Back");Capture("why-03-front-focus-apart");
            Check(StrikeNote(StrikeBlock.WaitsOutTrap)=="WAITING OUT TRAP","trap caution has a distinct plain-language label");
            while(!_fight.Over&&_fight.Sim.FallBackTicksLeft(0)>0)_fight.Tick();
            DrawBattle();CheckWhyTags("after Fall Back");
            foreach(var f in _figures)Check(!f.tag.key.Contains("LINES APART"),"separation label clears for "+f.unit.Id);

            // Actual short campaigns prove the header and ending agree with the result API,
            // including the same timeout with a different campaign policy.
            for(int policy=0;policy<2;policy++)
            {
                _campaign=Campaign.New(5,new CampaignTuning{FightTicks=30,TimeOutLoses=policy==0});
                _fight=_campaign.StartTutorial();BeginFight();_pilotOn=false;_paused=true;_coachStep=-1;
                while(!_fight.Over)_fight.Tick();
                _endAge=2;DrainEvents();DrawBattle();RefreshFight();
                yield return null;
                bool won=_campaign.FightWon(_fight);var lines=WhyLines();
                Check(_fight.Sim.Ending.How==FightEnd.TimeUp,"short tutorial reaches a real timeout");
                Check(lines.texts[0]==(won?"Why you won":"Why you lost"),"timeout header matches campaign result, policy "+policy);
                Check(lines.texts[1].EndsWith(won?"win.":"loss.",StringComparison.Ordinal),"timeout wording matches campaign result, policy "+policy);
                Check(_pxBanner.key.Contains(won?"YOU WON":"YOU LOST")||_pxBanner.key.Contains(won?"you won":"you lost"),"fight banner matches campaign result, policy "+policy);
                Capture("why-04-timeout-policy-"+policy);
                bool result=_campaign.Finish(_fight).Won;
                Check(result==won,"displayed outcome matches the awarded result, policy "+policy);
            }
            bool hold=false;
            for(ulong seed=300;seed<340&&!hold;seed++)
            {
                var campaign=Campaign.New(seed,new CampaignTuning{HoldTicks=40});
                if(!CampaignPilot.PlayTutorial(campaign,PilotStyle.Reference,seed,10))continue;
                campaign.StartRun();int pick=-1;
                for(int i=0;i<campaign.Offers.Count;i++)if(campaign.Offers[i].Kind==EncounterKind.HoldTheLine){pick=i;break;}
                if(pick<0)continue;
                _campaign=campaign;_fight=campaign.StartEncounter(pick);BeginFight();_pilotOn=false;_paused=true;_coachStep=-1;
                while(!_fight.Over)_fight.Tick();
                _endAge=2;DrainEvents();DrawBattle();RefreshFight();yield return null;
                var lines=WhyLines();hold=true;
                Check(_campaign.FightWon(_fight)&&_fight.Sim.Ending.How==FightEnd.TimeUp,"Hold the line reaches its winning timeout");
                Check(lines.texts[0]=="Why you held"&&lines.texts[1]=="Time ran out and you held the line.","Hold the line uses its own outcome wording");
                Capture("why-05-held-the-line");
            }
            Check(hold,"Hold the line fixture found");
            Check(Ending(new FightEnding(FightEnd.Wipe,-1,0,30),EncounterKind.WarParty,false)=="Both sides went down.","simultaneous wipe does not name the wrong side");
            Check(Ending(new FightEnding(FightEnd.Wipe,1,0,30),EncounterKind.WarParty,true)=="Every one of theirs went down.","wipe side is the defeated side");
            Check(Ending(new FightEnding(FightEnd.LeaderFell,0,4,30),EncounterKind.WarParty,false).StartsWith("Your leader fell",StringComparison.Ordinal),"leader-fall wording uses the affected side");
            Check(Ending(new FightEnding(FightEnd.NerveBreak,1,4,30),EncounterKind.WarParty,true).Contains("4 still standing"),"nerve-break wording keeps the authoritative survivor count");
        }

        void CheckWhyTags(string context)
        {
            var sim=_fight.Sim;var target=sim.FindById(sim.FocusedId(0));
            string before=_fight.Session.Checksum;ulong events=sim.EventHash;
            DrawBattle();
            foreach(var f in _figures)
            {
                if(f.unit.Side!=0||!f.unit.Alive)continue;
                string note=StrikeNote(sim.CanStrike(f.unit,target));
                Check(note.Length==0?(!f.tag.key.Contains("CAN'T REACH")&&!f.tag.key.Contains("LINES APART")&&!f.tag.key.Contains("WAITING OUT TRAP")):f.tag.key.Contains(note),context+" rendered label matches simulation for "+f.unit.Id);
                Check(f.tag.key.Contains("COVERED")== (sim.CoveredBy(f.unit)!=null),context+" cover label matches simulation for "+f.unit.Id);
            }
            Check(before==_fight.Session.Checksum&&events==sim.EventHash,context+" presentation queries do not mutate the fight");
        }
    }
}

