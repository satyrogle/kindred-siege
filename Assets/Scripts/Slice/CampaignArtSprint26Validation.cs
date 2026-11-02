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
        IEnumerator ValidateSprint26()
        {
            _savePath=Path.Combine(_capture,"sprint26-save.txt");
            IntegrationCap(26);
            _campaign=CampaignSave.Read(File.ReadAllText(Path.Combine(Application.streamingAssetsPath,"ReviewRoute","camp.txt")),ReviewTuning);
            _fight=null;_playback=null;GoToStage();yield return null;
            Check(_campaign.Stage==CampaignStage.ChooseTribe,"habit fixture starts at camp");
            ShowCamp();yield return null;Check(Click("pal-habits"),"camp opens actual habit picker");yield return null;
            Check(Click("habit-1"),"Bodyguard selected through actual control");yield return null;
            Check(_campaign.Habits.Any(h=>h.Habit==WhispererHabit.Bodyguard),"base habit read from Campaign");
            Check(Click("habit-6"),"Spotter selected through actual control");yield return null;Capture("26-habits");
            Save();ContinueSaved();yield return null;
            Check(_campaign.Habits.Any(h=>h.Habit==WhispererHabit.Bodyguard)&&_campaign.Habits.Any(h=>h.Habit==WhispererHabit.Spotter),"base habits survive file reload");
            _campaign.StartRun();SetPage(Page.Offers);ShowHabits();yield return null;
            Check(!_buttons["habit-2"].interactable,"in-run habit controls refuse changes");CloseModal();

            // One fixed QA fight, at the core test's explicit fixture difficulty; not a tuning sweep.
            var easy=new CampaignTuning{RulesCap=24,EnemyScaleTier1=40};
            _campaign=Campaign.New(5702,easy);
            Check(CampaignPilot.PlayTutorial(_campaign,PilotStyle.Reference,5702,10),"fixed Whisperer fixture completes tutorial");
            _campaign.TrySetHabit(WhispererHabit.Bodyguard);_campaign.TrySetHabit(WhispererHabit.Spotter);_campaign.StartRun();
            int offer=_campaign.Offers.ToList().FindIndex(o=>o.Node!=null&&o.Node.Pal==Pal.Whisperer);
            Check(offer>=0,"fixed seed has a Whisperer road");
            _result=CampaignPilot.PlayFight(_campaign,_campaign.StartEncounter(offer),new CampaignPilot(PilotStyle.Reference,5702));
            Check(_result.Won&&_campaign.FavourOffer!=null,"fixed fight earns actual Whisperer offer");
            _fight=null;SetPage(Page.Result);ShowFavours();yield return null;Capture("26-whisper-rewards");
            var saved=CampaignSave.Write(_campaign);
            for(int i=0;i<_campaign.FavourOffer.Choices.Count;i++)
            {
                var pick=WhisperChoice(i);var title=WhisperTitle(pick);
                Check(!string.IsNullOrEmpty(title)&&!WhisperLines(i).texts.Any(t=>t.Contains("unavailable")),"actual choice identity and effect: "+title);
            }
            int count=_campaign.FavourOffer.Choices.Count;
            for(int i=0;i<count;i++)
            {
                _campaign=CampaignSave.Read(saved,easy);SetPage(Page.Result);
                if(CombinedRewardPending)ShowRewardDetail(true,i);else ShowFavours();yield return null;
                var pick=WhisperChoice(i);var title=WhisperTitle(pick);
                Check(Click("favour-"+i),"actual paid choice control: "+title);yield return null;
                Check(_notice.Contains(title),"receipt names the actual picked subject");
                Check(_campaign.FavourOffer==null,"taking one clears other choices");
            }

            _campaign=CampaignSave.Read(saved,easy);
            var recorded=_campaign.Archive.Fights.Last();
            Check(_campaign.Archive.TryReplay(recorded.FightId,out var cashReplay,out string cashFailure),"earned fight replays: "+cashFailure);
            var cash=cashReplay.Events.FirstOrDefault(e=>e.Amount>0&&CashWordFor(e).Length>0);
            Check(CashWordFor(cash).Length>0,"fixed earned fight contains a real mark cash-in");
            WatchFight(recorded,0,recorded.Ticks);yield return null;
            SeekPlayback(cash.Tick-1);yield return null;
            while(_playback.Tick<=cash.Tick)
            {_visualTime+=1f/SimClock.TicksPerSecond;_playback.Step();DrainPlayback();}
            RefreshPlayback();DrawBattle();yield return null;
            Check(_cashWords.TryGetValue(cash.TargetId,out var word)&&word.image.enabled&&word.image.gameObject.activeInHierarchy,"actual replay cash-in appears on its real target");
            Check(word.image.texture.height>=32,"cash-in uses the approved large pixel font");
            Check(_numbers.Any(number=>number.payoff),"actual payoff hit gets a larger damage number");Capture("26-real-cash-in");

            IntegrationCap(26);_campaign=FreshRaidPreview();_fight=_campaign.Active;_playback=null;
            BeginFight();_pilotOn=false;_paused=false;_speed=0;yield return null;
            Check(ViewSim.TightParryOn&&_fight.RulesVersion==26,"chief fixture uses released tight26");
            int guard=0;
            while(!ViewOver&&!ViewSim.Threats(0).Any(t=>t.ParryWindow&&t.BeatTicks==7)&&guard++<1800)HudTicks(1);
            Check(!ViewOver&&guard<1800,"actual chief reaches start of tight window");
            var beat=ViewSim.Threats(0).First(t=>t.ParryWindow&&t.BeatTicks==7);var attacker=FindFigure(beat.ActorId);
            Check(attacker!=null&&attacker.hitMaterial!=null,"flash is attached to actual attacker");
            for(int frame=0;frame<6;frame++)
            {
                ReadClarity(ViewSim);DrawBattle();
                Check(attacker.hitMaterial.GetFloat(ParryGold)==(frame==0||frame==1||frame==3||frame==4?1:0),"approved flash frame "+frame);
                Check(!_parryOuter.enabled&&!_parryInner.enabled&&!_raidBeatRing.enabled&&!_raidFlash.enabled,"no competing HUD parry cue frame "+frame);
                if(frame==0){yield return null;Capture("26-attacker-gold");}
                if(frame<5)HudTicks(1);
            }
            Check(Click("order-0"),"fresh Hold through actual button inside window");
            HudTicks(2);yield return null;
            Check(ViewSim.ParryBars(0)==1,"real parry fills actual meter");
            Check(_tightClash.enabled,"approved sprite-only clash visible");Capture("26-parried");
            foreach(var figure in _figures.Where(f=>f.unit.Side==0&&f.hitMaterial!=null))
                Check(figure.hitMaterial.GetFloat(FlatWhite)==0,"parry whites the attacker only: "+figure.unit.Id);
            Check(_soundVoices[4].clip==_soundClips[(int)Cue.Shield],"parry uses protected confirmation voice");
            var clip=_soundVoices[4].clip;
            PlaySound("hit",1,0);PlaySound("omen",1,0);PlaySound("riposte",1,0);
            Check(_soundVoices[4].clip==clip&&_soundVoices[4].isPlaying,"ordinary hit, omen and riposte cannot steal clang");
            guard=0;
            while(!ViewOver&&!ViewSim.Threats(0).Any(t=>t.BeatTicks==12&&!t.FakeFirst&&!t.WaitsOutHold)&&guard++<1800)HudTicks(1);
            Check(!ViewOver&&guard<1800,"next real tell reaches early-Hold fixture");
            Check(Click("order-0"),"mistimed Hold accepted through actual button");
            for(int tick=0;tick<14&&ViewSim.WhiffTicksLeft(0)==0;tick++)HudTicks(1);yield return null;
            Check(ViewSim.WhiffTicksLeft(0)>0,"mistimed Hold shows actual vulnerability");
            Check(_orderEffect[0].key.StartsWith("WHIFF"),"actual vulnerability takes priority over Hold recovery label");
            Check(_soundVoices[4].clip==Named("whiff"),"actual whiff event plays handed-back clunk");Capture("26-whiff");
            Save();string state=ViewSim.OrderedStateSnapshot;ContinueSaved();_paused=true;_pilotOn=false;yield return null;
            Check(ViewSim.OrderedStateSnapshot==state,"tight active fight resumes exactly");

            _campaign=CampaignSave.Read(File.ReadAllText(Path.Combine(Application.streamingAssetsPath,"ReviewRoute","camp.txt")),ReviewTuning);
            _campaign.StartRun();_fight=_campaign.StartBeast();_playback=null;BeginFight();_paused=true;_pilotOn=false;yield return null;
            DrawBattle();
            Check(_sharkImage!=null&&_sharkImage.rectTransform.sizeDelta==new Vector2(512,512),"one approved shark drawn at2x");
            Check(_stageArt!=null&&_stageArt.name=="broken_wastes","shark uses existing Broken Wastes stage");
            Check(ViewSim.BeastScenesOn,"shark has delivered row-hit rules25");
            Check(BeastGuide(BeastKit.SandShark,true).Contains("whole row")&&BeastGuide(BeastKit.SandShark,true).Contains("both rows"),"guide describes actual bite and sweep coverage");
            foreach(var part in BeastKits.Parts(BeastKit.SandShark))Check(_buttons.ContainsKey("beast-part-"+part),"actual selectable part: "+part);
            Check(Click("order-2"),"Focus target mode on shark");yield return null;
            Check(Click("beast-part-Jaw"),"Focus selects real jaw");yield return null;
            Check(ViewSim.FocusedId(0)==ViewSim.Part(BeastPart.Jaw).Id,"shared boss art retains authoritative part focus");Capture("26-shark");
            for(int tick=0;tick<400&&!ViewOver;tick++)HudTicks(1);
            Capture("26-shark-moving");
            Log("Sprint26 integration checks complete. Native input and subjective feel remain owner review.");
        }
    }
}
