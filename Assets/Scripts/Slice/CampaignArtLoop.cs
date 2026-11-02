using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using KindredSiege.Sim;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace KindredSiege.Slice
{
    // Presentation over the Campaign API. Rules and persistence format remain in Sim.
    public sealed partial class CampaignArtLoop : MonoBehaviour
    {
        const float W=1440, H=900;
        static readonly Color Ink=Hex("201C23"), Bone=Hex("EFDFBE"), Muted=Hex("AD9A86"), Bronze=Hex("A87848"), Red=Hex("DA6B51"), Mint=Hex("99BEAC");
        static readonly Directive[] Orders={Directive.Hold,Directive.FallBack,Directive.FocusFire,Directive.Unleash,Directive.Eclipse};
        enum Page {Title,Tutorial,ChooseTribe,Offers,Squad,Fight,Result,Finished,Playback}
        Page _page;
        Campaign _campaign;
        CampaignFight _fight;
        CampaignPilot _pilot;
        EncounterResult _result;
        readonly List<string> _squad=new List<string>(), _feed=new List<string>();
        readonly StringBuilder _log=new StringBuilder();
        readonly Dictionary<string,Button> _buttons=new Dictionary<string,Button>();
        int _offer=-1,_eventCursor,_lastLosses;
        bool _paused,_pilotOn,_targeting,_auto;
        float _speed=1,_accum,_visualTime,_endAge;
        string _notice="",_saveSummary="",_savePath,_theyKnow="",_capture;
        Canvas _canvas;
        RectTransform _root,_screen,_modal;
        Font _font;
        Text _noticeText,_banner,_stats,_tracker,_events;
        Button _resultButton;
        RenderTexture _captureTarget;
        Texture2D _capturePixels;
        readonly CampaignSave.FileWriter _saveWriter = new CampaignSave.FileWriter();
        void Start()
        {
            Application.runInBackground=true;
            _combatFeedback=Argument("-ksNoCombatFeedback")==null;
            _font=Resources.GetBuiltinResource<Font>("Arial.ttf");
            _capture=Argument("-ksArtValidate")??Argument("-ksInnerCircleValidate"); _auto=_capture!=null;
            // Manual review keeps the real intro, coach and window scaling, but never uses the owner's save.
            string review=Argument("-ksArtReview");
            _manualReviewDir=!_auto&&review!=null?Path.GetFullPath(review):null;
            if(_manualReviewDir!=null)Directory.CreateDirectory(_manualReviewDir);
            _savePath=Path.Combine(_auto?_capture:_manualReviewDir??Application.persistentDataPath,"campaign_art_loop.txt");
            if (_auto) Directory.CreateDirectory(_capture);
            _canvas=new GameObject("Campaign art canvas",typeof(Canvas),typeof(GraphicRaycaster)).GetComponent<Canvas>();
            _canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            Camera.main.backgroundColor=Ink; Camera.main.clearFlags=CameraClearFlags.SolidColor;
            if (_auto)
            {
                _captureTarget=new RenderTexture((int)W,(int)H,24); _captureTarget.Create();
                _capturePixels=new Texture2D((int)W,(int)H,TextureFormat.RGB24,false);
                Camera.main.targetTexture=_captureTarget;
                _canvas.renderMode=RenderMode.ScreenSpaceCamera; _canvas.worldCamera=Camera.main; _canvas.planeDistance=1;
                QualitySettings.vSyncCount=0; Application.targetFrameRate=60;
            }
            if (FindObjectOfType<EventSystem>()==null) new GameObject("Campaign input",typeof(EventSystem),typeof(StandaloneInputModule));
            EventSystem.current.sendNavigationEvents=false;
            _root=Rect(_canvas.transform,"1440 x 900",0,0,W,H);
            _root.anchorMin=_root.anchorMax=_root.pivot=new Vector2(.5f,.5f); _root.anchoredPosition=Vector2.zero;
            PrepareReviewRoute();
            InitSound();
            ShowTitle();
            if (_auto) StartCoroutine(Argument("-ksInnerCircleValidate")!=null?ValidateInnerCircle():Validate());
            else if(_manualReviewDir!=null)
            {
                StartCoroutine(ManualReview());
                if(Argument("-ksArtTour")!=null)StartCoroutine(PresentationTour());
            }
        }
        void Update()
        {
            ReportSaveError();
            // Whole number scales only (Jacob, 11 Oct 2026: "scaling is shit"): the 1440x900 canvas is shown at
            // 1x, 2x and so on with a dark border, and shrinks only when the window is smaller than 1440x900.
            float fit=Mathf.Min(Screen.width/W,Screen.height/H);
            _canvas.scaleFactor=_auto?1:fit>=1?Mathf.Floor(fit):fit;
            if(_noticeText!=null)_noticeText.text=_notice;
            if(_pxNotice!=null)SetPx(_pxNotice,_notice,PxRed);
            InnerCircleInput();
            KeyboardInput();
            if(_page==Page.Playback){UpdatePlayback();return;}
            if (_page!=Page.Fight || _fight==null) return;
            float dt=_auto?1f/30:Mathf.Min(Time.unscaledDeltaTime,.1f);
            if(!_paused)
            {
                _visualTime+=dt*(_fight.Over?1:_speed);
                AdvanceCombatPoses(dt*(_fight.Over?1:_speed));
                if(!_fight.Over&&!_fight.Sim.AwaitingVerdict)
                {
                    _accum+=dt*_speed*SimClock.TicksPerSecond;
                    while(_accum>=1&&!_fight.Over&&!_fight.Sim.AwaitingVerdict)
                    {
                        Anticipate(); if(_pilotOn) _pilot.Step(_fight); _fight.Tick(); DrainEvents(); _accum--;
                    }
                }
                else if(_fight.Over) _endAge+=dt;
            }
            if(_fight.Sim.AwaitingVerdict)_accum=0;
            // Coach can pause this frame. Resolve it before drawing, so no paused frame retains a flash.
            RefreshFight(); DrawBattle();
        }
        void SetPage(Page page)
        {
            StopSound();
            _hover=string.Empty;
            _keyboardHint=string.Empty;
            _campOpen=false;
            ClearBattle();
            if(_screen!=null){_screen.gameObject.SetActive(false);Destroy(_screen.gameObject);}
            if(_modal!=null){Destroy(_modal.gameObject);_modal=null;}
            FreeScreenTextures();
            _buttons.Clear(); _page=page;
            _screen=Rect(_root,page.ToString(),0,0,W,H);
            Panel(_screen,0,0,W,H,Ink);
            // Every page but the title is on the 2x pixel grid with its own pixel text and notice line
            // (11 Oct 2026); the title keeps its own art. Finished is never reached and keeps the old look.
            bool pixel=page!=Page.Title&&page!=Page.Finished;
            _noticeText=null;
            if(!pixel)
            {
                Label(_screen,"K I N D R E D   S I E G E",32,18,760,24,15,Bronze,FontStyle.Bold);
                _noticeText=Label(_screen,_notice,32,846,1110,34,16,Red);
            }
            if(page==Page.Fight||page==Page.Playback) BuildBattle(); else { BuildPage(); if(!pixel) AddCampAccess(); }
            SelectPageControl();
        }
        void OnDestroy()
        {
            _saveWriter.Dispose();
            ClearSound();
            ClearBattle();
            ClearInnerCircle();
            if(_canvas!=null) Destroy(_canvas.gameObject);
            if(_capturePixels!=null) Destroy(_capturePixels);
            if(_captureTarget!=null){_captureTarget.Release();Destroy(_captureTarget);}
        }
        static string Argument(string key){var a=Environment.GetCommandLineArgs();for(int i=0;i<a.Length-1;i++)if(a[i]==key)return a[i+1];return null;}
        /// Reads the save through the safe reader (CampaignSave.TryReadFile, Codex's save repair, 11 Oct 2026):
        /// a damaged or missing save opens the last good backup, and the title says so.
        void ShowTitle()
        {
            _saveWriter.Flush(); ReportSaveError();
            _saveSummary = string.Empty;
            if (FileExists())
            {
                if (CampaignSave.TryReadFile(_savePath, ReviewTuning, out Campaign saved, out string failure))
                {
                    _saveSummary = Summary(saved);
                    if (!string.IsNullOrEmpty(saved.LoadNote)) _notice = saved.LoadNote;
                }
                else
                    _saveSummary = "The save could not be read: " + failure;
            }
            SetPage(Page.Title);
        }

        void NewCampaign()
        {
            if(_gatesReview){NewReviewRoute();return;}
            ulong seed = _auto ? 21UL : (ulong)(DateTime.Now.Ticks % 1000000L) + 1UL;
            _campaign = Campaign.New(seed);
            _notice = string.Empty;
            Save();
            Log("New campaign, seed " + seed);
            SetPage(Page.Tutorial);
        }

        /// A save that cannot be read is reported and left alone; it is never replaced by a new campaign.
        void ContinueSaved()
        {
            _saveWriter.Flush(); ReportSaveError();
            if (!FileExists())
            {
                _notice = "No save found.";
                return;
            }
            if (!CampaignSave.TryReadFile(_savePath, ReviewTuning, out Campaign c, out string failure))
            {
                _notice = "The save could not be read: " + failure;
                return;
            }
            _campaign = c;
            _notice = c.LoadNote;
            Log("Reopened the save: " + Summary(c));
            GoToStage();
        }

        void GoToStage()
        {
            if (_campaign.Active != null)
            {
                ResumeFight();
                return;
            }
            switch (_campaign.Stage)
            {
                case CampaignStage.Tutorial: SetPage(Page.Tutorial); break;
                case CampaignStage.ChooseTribe: SetPage(Page.ChooseTribe); break;
                case CampaignStage.InRun: SetPage(Page.Offers); break;
                default: SetPage(Page.Finished); break;
            }
        }

        /// A save made mid fight reopens inside that fight, paused, at the second it was left
        /// (Claude for Jacob, 10 Oct 2026). Earlier events are not replayed into the feed.
        void ResumeFight()
        {
            _fight = _campaign.Active;
            string note = _notice;
            BeginFight();
            _notice = note;
            _eventCursor = _fight.Sim.Events.Count;
            _feed.Clear();
            Feed(Seconds(_fight.Sim.CurrentTick) + "  " + _fight.Encounter.Title + " picks up where you left it. Space to carry on.");
            _paused = true;
            // A finished fight saved before its receipt must not wait on a paused death timer.
            if(_fight.Over)_endAge=2.4f;
        }

        /// Saves where the campaign stands, a run or a half played fight included: when a tribe is chosen,
        /// when a fight starts, after every order, after every fight, and when the game closes or loses focus.
        /// Since 11 Oct 2026 through Codex's safe writer (CampaignSave version 4): a temp file is written and
        /// checked, then replaces the save, and the old save is kept as a .bak backup.
        void Save(bool background = false)
        {
            if (_campaign == null) return;
            try
            {
                _saveWriter.Submit(_savePath, CampaignSave.CaptureFile(_campaign));
                if (!background || _auto) { _saveWriter.Flush(); ReportSaveError(); }
            }
            catch (Exception e)
            {
                _notice = "Saving failed: " + e.Message;
            }
        }

        void ReportSaveError()
        {
            string error = _saveWriter.TakeError();
            if (!string.IsNullOrEmpty(error)) { _notice = error; Debug.LogWarning(error); }
        }

        // The automatic validation saves where it chooses, so closing it never writes over its save.
        void OnApplicationQuit()
        {
            if (!_auto) Save();
        }

        void OnApplicationPause(bool paused)
        {
            if (paused && !_auto) Save();
        }

        void OnApplicationFocus(bool focused)
        {
            if (!focused && !_auto) Save();
        }

        void GoAfter(string tribeId)
        {
            _campaign.ChooseTribe(_gatesReview ? CampaignTribes.BloodridgeId : tribeId);
            Log("Run " + _campaign.RunsStarted + " leaves camp for " + TribeName(tribeId) + ", materials at camp " + _campaign.Materials);
            Save();
            SetPage(Page.Offers);
        }

        void TakeOffer(int index)
        {
            PlaySound("node_pick");
            _offer = index;
            _squad.Clear();
            if(_campaign.Offers[index].Kind!=EncounterKind.Duel)
            {
                List<CampaignFighter> squad = _campaign.DefaultSquad(_campaign.Offers[index].SquadSize);
                for (int i = 0; i < squad.Count; i++) _squad.Add(squad[i].Id);
            }
            SetPage(Page.Squad);
        }

        void StartTutorialFight()
        {
            _fight = _campaign.StartTutorial();
            BeginFight();
            Save();
        }

        void StartEncounterFight()
        {
            if(SelectedEncounter.Kind==EncounterKind.Duel&&_squad.Count!=1)
            {
                _notice="Choose one fighter to face the champion.";
                return;
            }
            try
            {
                _fight = _offer == -2 ? _campaign.StartBeast(_squad) : _campaign.StartEncounter(_offer, _squad);
            }
            catch (ArgumentException e)
            {
                _notice = e.Message;
                return;
            }
            BeginFight();
            Save();
        }

        void BeginFight()
        {
            _pilot = new CampaignPilot(PilotStyle.Reference, _campaign.Seed + (ulong)_campaign.Step);
            _pilotOn = _auto;
            _speed = _auto ? 12f : _learningSpeed;
            _paused = false;
            _accum = 0f;
            _targeting = false;
            _eventCursor = 0;
            _feed.Clear();
            _notice = string.Empty;
            TacticsProfile theirs = _fight.Setup.CopyTactics(1);
            var known = new List<string>();
            if (theirs.CoverFocused >= BattleSimulation.HabitThreshold) known.Add("their Warden covers whoever you focus");
            if (theirs.HoldOnUnleash >= BattleSimulation.HabitThreshold) known.Add("they raise shields when you Unleash");
            if (theirs.PressOnFallBack >= BattleSimulation.HabitThreshold) known.Add("they Unleash when you fall back");
            if (!_fight.Sim.ParryCenterOn && theirs.AvoidGuarded >= BattleSimulation.HabitThreshold) known.Add("their archers shoot whoever your Warden is not covering");
            if (theirs.TrapCaution >= BattleSimulation.HabitThreshold) known.Add("they let their Warden spring your traps");
            if (_fight.Sim.SquadFirstOn)
            {
                if(theirs.WaitOutMove.Length>0&&_fight.Sim.WaitsOutOn(1,theirs.WaitOutMove))known.Add("waits out Hold on "+theirs.WaitOutMove);
                if(theirs.FeintMove.Length>0&&_fight.Sim.FeintsOn(1,theirs.FeintMove))known.Add("fakes "+theirs.FeintMove);
            }
            else if (_fight.Sim.WaitsOutParry(1)) known.Add(_fight.Sim.ParryCenterOn ? "they may delay a beat once: watch and time a fresh Hold" : "they wait out your Hold: it halves their cast, but cannot parry it");
            _theyKnow = known.Count == 0 ? "nothing yet" : string.Join("; ", known.ToArray());
            Feed(_fight.Encounter.Title + " begins.");
            SetPage(Page.Fight);
        }

        void ShowResult()
        {
            _notice = string.Empty;
            RememberFallen();
            _lastLosses = Lost(_fight.Sim);
            _resultUsesLives=_fight.RulesVersion>=Campaign.RoadCombatRules;
            _resultUsesKit=_fight.RulesVersion>=Campaign.RoadKitRules;
            _result = _campaign.Finish(_fight);
            _resultLives=_campaign.Lives;
            _fight = null;
            Save();
            Log(_result.Encounter.Title + ": " + Outcome(_result) + ", materials +" + _result.MaterialsEarned +
                (_result.Joined ? ", JOINED" : string.Empty) + (_result.NextTribe != null ? ", on to " + _result.NextTribe.Name : string.Empty) +
                (_result.RunWon ? ", RUN WON" : _result.RunOver ? ", RUN OVER" : string.Empty));
            SetPage(Page.Result);
            if(_result.MaterialsEarned>0||_result.TutorialWon)PlayCue(Cue.Reward);
            if(ThemePresentation&&_campaign.FavourOffer!=null)ShowFavours();
        }

        void AfterResult()
        {
            _result = null;
            GoToStage();
        }

        // ------------------------------------------------------------ orders

        void Order(Directive directive)
        {
            if(_playback!=null)return;
            if (_fight == null || _fight.Over) return;
            if(_fight.Sim.AwaitingVerdict)
            {
                // Both choices use the authoritative free verdict orders. Execute always
                // addresses the kneeling chief, never a stale Focus Fire selection.
                var chief=_fight.Sim.KneelingChief;
                string target=directive==Directive.FocusFire&&chief!=null?chief.Id:null;
                if(!_fight.TryOrder(directive,target,false,out string why)){_notice=why;PlayCue(Cue.Unavailable);return;}
                _targeting=false;_notice=string.Empty;
                DrainEvents();Save();_endAge=2.4f;
                RefreshFight();
                return;
            }
            if (directive == Directive.FocusFire)
            {
                _targeting = !_targeting;
                if(_targeting){var targets=KeyboardButtons();if(targets.Count>0)EventSystem.current.SetSelectedGameObject(targets[0].gameObject);}
                else SelectControl("order-2");
                return;
            }
            if (!_fight.TryOrder(directive, null, !_paused, out string failure))
            {
                OrderRejected();
                PlayCue(Cue.Unavailable);
            }
            else
            {
                HoldAccepted();
                _notice = string.Empty;
                DrainEvents(); // the order shows on the fighters at once, even while paused
                Save(background: true);
            }
        }

        void Focus(BattleUnit target)
        {
            if(_playback!=null)return;
            _targeting = false;
            SelectControl("order-2");
            if (!_fight.TryOrder(Directive.FocusFire, target.Id, !_paused, out string failure)){OrderRejected();PlayCue(Cue.Unavailable);}
            else
            {
                HoldAccepted();
                _notice = string.Empty;
                DrainEvents();
                Save(background: true);
            }
        }

        /// Where this fight sits on the run and what rides on it (Jacob, 10 Oct 2026: a set path per
        /// tribe, then its chief, whose tribe and order stay for the rest of the run; no respect, no taboos).
        string Tracker(BattleSimulation sim, Encounter e)
        {
            if (e.Kind == EncounterKind.Tutorial) return "TUTORIAL: win and the Clarth join you.";
            if (e.Kind == EncounterKind.Beast) return BeastGuide(e.Beast);
            if (HunterFight(e.Kind)) return HunterTracker(e);
            CampaignTribe t = e.Tribe;
            var b = new StringBuilder(t.Name.ToUpperInvariant() + ", TIER " + t.Tier + ", " + StepName(_campaign).ToUpperInvariant());
            if (e.Kind == EncounterKind.Chief)
                b.Append(sim.ChiefKitsOn?"\nBreak the chief's nerve, then choose without a timer. Spare: the tribe joins with its order, ":"\nBreak his nerve while he still stands and his whole tribe joins you for the rest of the run, with his order, ")
                    .Append(t.AssistName).Append(". Kill him and you only take his Princess gear.");
            else if(e.Kind==EncounterKind.AlphaHunt)
                b.Append("\nOne final fight. Win: +").Append(_campaign.Tuning.MaterialsPerChief).Append(" materials and the run is won. The hunters never join.");
            else
                b.Append("\nWin: +").Append(_campaign.Tuning.MaterialsPerFight).Append(" materials for camp, or one piece of their gear in their place.");
            if(sim.RoadCombatOn)b.Append("\nLose: one life. Your whole squad and gear return. No lives left ends the run.");
            else b.Append("\nLose: they capture one of yours and you try again. Under ").Append(Campaign.MinSquad).Append(" fighters the run ends.");
            b.Append("\nChief orders this run: ").Append(OrdersThisRun()).Append('.');
            return b.ToString();
        }

        string States(BattleSimulation sim)
        {
            var b = new StringBuilder();
            if (sim.HoldTicksLeft(1) > 0) b.Append("Their shields are up for ").Append(Seconds(sim.HoldTicksLeft(1))).Append(": your Unleash will not scare them.\n");
            if (sim.UnleashTicksLeft(1) > 0) b.Append("They are unleashing for ").Append(Seconds(sim.UnleashTicksLeft(1))).Append(": Hold to keep your nerve.\n");
            if (sim.HoldTicksLeft(0) > 0) b.Append("Your Hold: ").Append(Seconds(sim.HoldTicksLeft(0))).Append(".\n");
            if (sim.UnleashTicksLeft(0) > 0) b.Append("Your Unleash: ").Append(Seconds(sim.UnleashTicksLeft(0))).Append(".\n");
            if (sim.WindupTicks(0) > 0) b.Append("Your chief order is winding up: ").Append(Seconds(sim.WindupTicks(0))).Append(".\n");
            return b.ToString();
        }

        void DrainEvents()
        {
            BattleSimulation sim = _fight.Sim;
            IReadOnlyList<BattleEvent> ev = sim.Events;
            for (; _eventCursor < ev.Count; _eventCursor++)
            {
                BattleEvent e = ev[_eventCursor];
                AnimateEvent(e);
                string time = Seconds(e.Tick) + "  ";
                switch (e.Kind)
                {
                    case BattleEventKind.Directive:
                        if (e.Detail == BattleSimulation.BlockedDetail) Feed(time + (e.ActorSide == 0 ? "Your shields stopped their Unleash." : "Their shields stopped your Unleash."));
                        else if (Directives.TryParse(e.Detail, out Directive order))
                            Feed(time + (e.ActorSide == 0 ? "You: " : "They: ") +
                                (order == Directive.Eclipse ? ChiefSpells.Name(ViewSim.Spell(e.ActorSide)) : Pretty(e.Detail)) +
                                (e.TargetId.Length > 0 ? " on " + NameOf(e.TargetId) : string.Empty) + ".");
                        break;
                    case BattleEventKind.Windup:
                        Feed(time + (e.Detail == HeavyRules.Heavy
                            ? NameOf(e.ActorId) + " winds up a heavy swing" + (e.TargetId.Length > 0 ? " at " + NameOf(e.TargetId) : string.Empty) + "."
                            : e.ActorSide == 0 ? "Your chief order is winding up."
                            : NameOf(e.ActorId) + " is preparing " + (e.Detail == Directives.Name(Directive.Eclipse) ? "a cast." : "a " + e.Detail + ".")));
                        break;
                    case BattleEventKind.Release:
                    {
                        // What went off: an Eclipse, a heavy swing, a hunter's Firebomb or War Cry (CampaignArtHunters.cs).
                        if (e.Detail == ChiefSpells.Cried) { Feed(time + NameOf(e.ActorId) + " cries out: " + (e.ActorSide == 1 ? "their" : "your") + " nerve comes back."); break; }
                        string move = CastName(e);
                        bool heavy = move == "heavy swing";
                        Feed(time + (e.ActorSide == 1
                            ? (e.Detail == FightRules.Parried ? "You parried their " + move + ": " + NameOf(e.ActorId) + " staggers." : e.Detail == Moves.Dodged ? "You dodged their " + move + "." : e.Detail == Moves.Soaked ? "Your Hold halved their " + move + "." : heavy ? NameOf(e.ActorId) + "'s heavy swing hit " + NameOf(e.TargetId) + "." : "Their " + move + " hit your front line.")
                            : (e.Detail == FightRules.Parried ? "They parried your " + move + "." : e.Detail == Moves.Dodged ? "They dodged your " + move + "." : e.Detail == Moves.Soaked ? "Their Hold halved your " + move + "." : "Your " + move + " hit their front line.")));
                        break;
                    }
                    case BattleEventKind.Interrupt:
                        Feed(time + (e.ActorSide == 0 ? "You broke " + NameOf(e.TargetId) + "'s cast. Your next order is doubled." : "They broke your cast."));
                        break;
                    case BattleEventKind.Kill:
                        Feed(time + NameOf(e.TargetId) + " is down.");
                        break;
                    case BattleEventKind.Break:
                        Feed(time + (e.ActorSide == 0 ? "YOUR NERVE BROKE." : "THEIR NERVE BROKE with " + e.Amount + " standing."));
                        break;
                    case BattleEventKind.Combo:
                        Feed(time + (e.ActorSide == 0 ? "You: " : "They: ") + e.Detail + (e.TargetId.Length > 0 ? " on " + NameOf(e.TargetId) : string.Empty) + ".");
                        break;
                    case BattleEventKind.Snap:
                        if(e.Detail==RoadRules.Fled)
                        {
                            Feed(time+NameOf(e.ActorId)+(ViewKind==EncounterKind.Duel?" yields. You won the duel.":" fled."));
                            break;
                        }
                        Feed(time + NameOf(e.ActorId) + (e.Detail == FightRules.Rise ? " rises: hits " + (FightRules.RiseDamagePercent - 100) + "% harder for " : " panics: does nothing for ") + Seconds(e.Amount) + ".");
                        break;
                    case BattleEventKind.Reaction:
                        Feed(time + ReactionLine(e));
                        break;
                    case BattleEventKind.Wall:
                        Feed(time + (e.Detail == ChiefSpells.WallUp
                            ? (e.ActorSide == 1 ? "Their Steel Storm wall is up for " + Seconds(e.Amount) + ": your hits barely land. Unleash smashes it." : "Your Steel Storm wall is up for " + Seconds(e.Amount) + ".")
                            : e.Detail == ChiefSpells.WallSmashed ? (e.ActorSide == 1 ? "Your Unleash smashed their wall: they lose " + e.Amount / 10 + "% nerve." : "Their Unleash smashed your wall.")
                            : (e.ActorSide == 1 ? "Their wall fell." : "Your wall fell.")));
                        break;
                    case BattleEventKind.End:
                        Feed(time + "Fight over.");
                        break;
                }
            }
        }

        string NameOf(string unitId)
        {
            BattleUnit u = _playback!=null?_playback.Sim.FindById(unitId):_fight == null ? null : _fight.Sim.FindById(unitId);
            return u == null ? unitId : UnitName(u);
        }

        string UnitName(BattleUnit u)
        {
            if(ViewSim.TryPartOf(u,out var part))return BeastKits.Name(ViewSim.Beast(u.Side))+" "+BeastKits.Name(part);
            string name=_playback!=null?_playback.NameOf(u.Id):_fight?.NameOf(u.Id);
            // By what it is in this fight, so the shaman token is a Shaman in words too ("fix all 9", 13 Oct 2026).
            if(!string.IsNullOrEmpty(name))return name+" / "+RoleName(u,ViewKind);
            if (u.Side == 0)
            {
                if (u.Id == Campaign.LeaderId) return "Your leader (" + SliceClasses.Name(u.Class) + ")";
                if(_playback==null)for (int i = 0; i < _fight.Squad.Count; i++) if (_fight.Squad[i].Id == u.Id) return _fight.Squad[i].DisplayName;
                return SliceClasses.Name(u.Class);
            }
            string tribe = TribeName(ViewTribe);
            if(HunterFight(ViewKind))return u.IsLeader?"The hunter":"Hunter "+SliceClasses.Name(u.Class);
            if (u.Id.EndsWith(".chief", StringComparison.Ordinal)) return tribe + " chief";
            if (u.Id.EndsWith(".shaman", StringComparison.Ordinal)) return tribe + " shaman";
            if (u.Id.EndsWith(".champion", StringComparison.Ordinal)) return tribe + " champion";
            return tribe + " " + SliceClasses.Name(u.Class);
        }

        int Lost(BattleSimulation sim)
        {
            int lost = 0;
            for (int i = 0; i < sim.Units.Count; i++) if (sim.Units[i].Side == 0 && !sim.Units[i].Alive) lost++;
            return lost;
        }

        // ------------------------------------------------------------ the run

        /// "step 1 of 3" on the tribe's road (13 Oct 2026: a step is a fight or an event), or "the chief" once the
        /// road is behind you.
        static string StepName(Campaign c)
        {
            if(c.Target!=null&&c.Target.Id==AlphaHunt.Id)return "final hunt - one fight";
            return c.Step >= c.PathLength ? "the chief" : "step " + (c.Step + 1) + " of " + c.PathLength;
        }

        /// "Bloodridge, step 1 of 3", or "at camp" between runs.
        static string PathPlace(Campaign c)
        {
            return c.Target == null ? "at camp" : c.Target.Name + ", " + StepName(c);
        }

        /// The tribes a run meets, one per tier, in order (Jacob, 10 Oct 2026: a run crosses tribes).
        List<CampaignTribe> Route()
        {
            return new List<CampaignTribe>(_campaign.RunRoute);
        }

        string OrdersThisRun()
        {
            if (_campaign.Assists.Count == 0) return "none yet";
            var names = new List<string>();
            for (int i = 0; i < _campaign.Assists.Count; i++) names.Add(_campaign.Assists[i].Name);
            return string.Join(", ", names.ToArray());
        }

        // ------------------------------------------------------------ helpers

        void Feed(string line)
        {
            _feed.Add(line);
            if (_feed.Count > 40) _feed.RemoveAt(0);
        }

        void Log(string line)
        {
            _log.Append(line).Append('\n');
            Debug.Log("[CampaignArt] " + line);
        }

        static string Summary(Campaign c)
        {
            var b = new StringBuilder("Seed " + c.Seed.ToString(CultureInfo.InvariantCulture) + ". Tribes with you: ");
            for (int i = 0; i < c.Joined.Count; i++) b.Append(i > 0 ? ", " : string.Empty).Append(TribeName(c.Joined[i]));
            if (c.Joined.Count == 0) b.Append("none (the tutorial is next)");
            b.Append(". Runs started ").Append(c.RunsStarted).Append(", won ").Append(c.RunsWon).Append(". Materials ").Append(c.Materials).Append('.');
            if (c.Target != null) b.Append(" In a run: ").Append(PathPlace(c)).Append('.');
            for (int i = 0; i < CampaignTribes.All.Length; i++)
            {
                CampaignTribe t = CampaignTribes.All[i];
                if (t.IsStarter) continue;
                b.Append(" ").Append(t.Name).Append(": ").Append(c.Mind(t.Id).Lessons.Count).Append(" lessons about you.");
            }
            return b.ToString();
        }

        static string TribeName(string tribeId)
        {
            CampaignTribe t = CampaignTribes.Find(tribeId);
            return t == null ? tribeId : t.Name;
        }

        static string Outcome(EncounterResult r)
        {
            if (r.Won && r.Encounter.Kind == EncounterKind.HoldTheLine && r.TimedOut) return "You held the line";
            if (r.Won) return "Won";
            return r.TimedOut ? "Lost: time ran out" : "Lost";
        }

        static string OrderName(Directive d)
        {
            switch (d)
            {
                case Directive.Hold: return "Hold";
                case Directive.FallBack: return "Fall Back";
                case Directive.FocusFire: return "Focus Fire";
                case Directive.Unleash: return "Unleash";
                case Directive.Eclipse: return "Eclipse";
                default: return d.ToString();
            }
        }

        static string Pretty(string directiveName)
        {
            for (int i = 0; i < Orders.Length; i++) if (Directives.Name(Orders[i]) == directiveName) return OrderName(Orders[i]);
            return directiveName;
        }

        static bool Contains(IReadOnlyList<Feat> feats, Feat feat)
        {
            for (int i = 0; i < feats.Count; i++) if (feats[i] == feat) return true;
            return false;
        }

        static string Seconds(long ticks)
        {
            return (ticks / (float)SimClock.TicksPerSecond).ToString("0.0", CultureInfo.InvariantCulture) + "s";
        }

        static Color Hex(string value) { ColorUtility.TryParseHtmlString("#" + value, out Color c); return c; }
        static Color Alpha(Color c, float a) { c.a = a; return c; }
        static RectTransform Rect(Transform parent, string name, float x, float y, float w, float h)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            r.SetParent(parent, false); r.anchorMin = r.anchorMax = r.pivot = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(w, h); return r;
        }
        static Image Panel(Transform parent, float x, float y, float w, float h, Color c)
        {
            Image image = Rect(parent, "Panel", x, y, w, h).gameObject.AddComponent<Image>();
            image.color = c; image.raycastTarget = false; return image;
        }
        static RawImage Raw(Transform parent, string name, float x, float y, float w, float h)
        { RawImage image = Rect(parent, name, x, y, w, h).gameObject.AddComponent<RawImage>(); image.raycastTarget = false; return image; }
        Text Label(Transform parent, string value, float x, float y, float w, float h, int size, Color c,
            FontStyle style = FontStyle.Normal, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            Text text = Rect(parent, value, x, y, w, h).gameObject.AddComponent<Text>();
            text.font = _font; text.text = value; text.fontSize = size; text.fontStyle = style; text.color = c;
            text.alignment = alignment; text.raycastTarget = false; text.horizontalOverflow = HorizontalWrapMode.Wrap;
            return text;
        }
        Button ActionButton(Transform parent, float x, float y, float w, float h, string title, UnityEngine.Events.UnityAction action, int size = 18)
        {
            Image image = Panel(parent, x, y, w, h, Hex("3E302C")); image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var colours = button.colors; colours.normalColor = Color.white; colours.highlightedColor = Hex("FFE0AF");
            colours.pressedColor = Hex("D49359"); colours.disabledColor = new Color(.7f, .7f, .7f, .52f); button.colors = colours;
            button.onClick.AddListener(action);
            Label(button.transform, title, 14, 6, w - 28, h > 60 ? 34 : h - 12, size, Bone, FontStyle.Bold);
            return button;
        }
    }
}
