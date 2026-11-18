using System.Collections;
using System.Collections.Generic;
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
        void CheckMeter(int expected,string moment)
        {
            var sim=ViewSim;
            Check(sim.MeterOn&&sim.ParryBars(0)==expected&&_meterPanel.gameObject.activeInHierarchy,moment+": authoritative meter visible");
            Check(_meterSuns.texture==_meterStates[expected],moment+": delivered sun state matches the sim");
            Check(_meterLabel.key.Contains("PARRY "+expected+"/"+MeterRules.Bars),moment+": readable bar count");
            Check(_meterBonus.key.Contains("COMBO +"+(expected*MeterRules.PercentPerBar)+"%"),moment+": held combo bonus is labelled");
        }

        IEnumerator ValidateMeter()
        {
            yield return null;
            if(Argument("-ksMeterReopen")!=null)
            {
                Check(Click("continue"),"meter checkpoint Continue in a fresh process");yield return null;
                _paused=true;_pilotOn=false;RefreshFight();DrawBattle();
                CheckMeter(2,"fresh process");
                Check(ViewSim.OrderedStateSnapshot==File.ReadAllText(Path.Combine(_capture,"restart-state.txt")),"fresh process preserves exact rules22 fight state");
                Check(_orderEffect[4].key.Contains("2/3 BARS")&&!_buttons["order-4"].interactable,"reopened spell still needs its third bar");
                Capture("meter-reopened");yield break;
            }
            IntegrationCap(22);
            _campaign=FreshRaidPreview();_fight=_campaign.Active;_playback=null;
            BeginFight();_pilotOn=false;_paused=true;_speed=.5f;yield return null;
            Check(_fight.RulesVersion==22&&ViewSim.MeterOn&&_fight.Assist!=null,"released22 chief fixture has the meter and a real inherited spell");
            RefreshFight();DrawBattle();CheckMeter(0,"new fight");
            Check(DisplayOrderCost(ViewSim,Directive.Eclipse)==0&&_orderCost[4].key.Split('|')[0]=="","chief spell has no displayed order-point cost");
            Check(_orderEffect[4].key.Contains("0/3 BARS")&&!_buttons["order-4"].interactable,"empty meter explains unavailable chief spell");
            string unchanged=ViewSim.OrderedStateSnapshot;
            Check(!_fight.TryOrder(Directive.Eclipse,null,false,out string failure)&&failure.Contains("parry bars"),"campaign refuses an early cast with the actual bar-count reason");
            Check(ViewSim.OrderedStateSnapshot==unchanged,"refused cast changes no combat state");
            CheckCleanTextBounds();Capture("meter-0");
            var milestones=new List<(long tick,int bars)>();string twoBars="";
            for(int bars=1;bars<=MeterRules.Bars;bars++)
            {
                int guard=0;
                while(!ViewOver&&ViewSim.ParryBars(0)<bars&&guard++<1800)
                {
                    var sim=ViewSim;
                    if(sim.Threats(0).Any(t=>t.ParryWindow&&t.BeatTicks==1)&&sim.CanUse(0,Directive.Hold,out _))
                    {
                        _paused=false;RefreshFight();DrawBattle();
                        Check(Click("order-0"),"actual Hold control at a real parry beat");_paused=true;
                    }
                    HudTicks(1);if(guard%60==0)yield return null;
                }
                Check(!ViewOver,"chief fixture remains in combat for bar "+bars);
                CheckMeter(bars,"parry "+bars);milestones.Add((ViewSim.CurrentTick,bars));
                string state=ViewSim.OrderedStateSnapshot;DrawParryMeter(ViewSim);
                Check(ViewSim.OrderedStateSnapshot==state,"meter drawing never mutates the sim");
                _paused=false;_hover="";RefreshFight();DrawBattle();CaptureAsDrawn("meter-"+bars);_paused=true;
                Save();ContinueSaved();_paused=true;_pilotOn=false;yield return null;
                Check(ViewSim.OrderedStateSnapshot==state,"save/reload preserves bar "+bars+" and all combat state");
                CheckMeter(bars,"reloaded "+bars);
                if(bars==2)twoBars=CampaignSave.Write(_campaign);
            }
            int drain=0;
            while(ViewSim.DirectivePoints(0)>0&&drain++<20)
            {
                int before=ViewSim.DirectivePoints(0);
                Check(Click("order-1"),"real Fall Back spends an order point without earning bars");RefreshFight();DrawBattle();
                Check(ViewSim.DirectivePoints(0)<before&&ViewSim.ParryBars(0)==MeterRules.Bars,"Fall Back changes points, not held parry bars");
            }
            Check(ViewSim.DirectivePoints(0)==0,"chief spell test reaches zero order points through real commands");
            Check(_fight.CanCallAssist&&_buttons["order-4"].interactable&&_orderEffect[4].key.Contains("READY"),"full meter lights the real chief spell even with zero order points");
            int points=ViewSim.DirectivePoints(0);
            Check(Click("order-4"),"chief spell cast through its real UI control");RefreshFight();DrawBattle();
            CheckMeter(0,"spent spell");
            Check(ViewSim.DirectivePoints(0)==points&&_fight.AssistUsed,"spell spends bars rather than order points");
            Check(!_buttons["order-4"].interactable&&_orderEffect[4].key.Split('|')[0]!="READY","spent once-per-fight spell cannot remain ready");
            Capture("meter-spent");HudTicks(1);long spentAt=ViewSim.CurrentTick;
            Save();string spentState=ViewSim.OrderedStateSnapshot;ContinueSaved();_paused=true;_pilotOn=false;yield return null;
            Check(ViewSim.OrderedStateSnapshot==spentState&&_fight.AssistUsed,"spent meter and spell usage persist exactly");

            int finish=0;while(!ViewOver&&finish++<4200)
            {
                if(ViewSim.AwaitingVerdict)Check(Click("order-0"),"recording explicitly spares a kneeling chief");
                else HudTicks(1);
                if(finish%120==0)yield return null;
            }
            Check(ViewOver,"meter recording reaches an authoritative result");
            ShowResult();CloseModal();yield return null;
            var record=_campaign.Archive.Fights.Last();string campaign=CampaignSave.Write(_campaign);
            Check(_campaign.Archive.TryReplay(record.FightId,out _,out string why),"meter archive verifies: "+why);
            WatchFight(record,0,record.Ticks);yield return null;
            foreach(var mark in milestones)
            {
                SeekPlayback(mark.tick);yield return null;CheckMeter(mark.bars,"recorded "+mark.bars);Capture("meter-replay-"+mark.bars);
            }
            SeekPlayback(spentAt);yield return null;CheckMeter(0,"recorded spend");
            SeekPlayback(0);yield return null;CheckMeter(0,"rewound before parries");
            SeekPlayback(record.Ticks);yield return null;Check(_playback.Matched,"meter replay preserves full final checksum");
            Check(CampaignSave.Write(_campaign)==campaign,"watching and seeking the meter does not change campaign state");LeavePlayback();CloseModal();

            IntegrationCap(21);NewReviewRoute();yield return null;
            Check(Click("offer-0"),"historical21 encounter");yield return null;QaChooseDuelist();yield return null;
            Check(Click("start-fight"),"historical21 fight starts");_pilotOn=false;_paused=true;yield return null;
            Check(!ViewSim.MeterOn&&!_meterPanel.gameObject.activeSelf,"rules21 keeps its original HUD without a false meter");
            Check(DisplayOrderCost(ViewSim,Directive.Eclipse)==Directives.Cost(Directive.Eclipse),"rules21 retains original chief point cost");Capture("meter-legacy21");
            IntegrationCap(22);NewReviewRoute();yield return null;Check(Click("offer-0"),"new22 encounter after replay");yield return null;
            QaChooseDuelist();yield return null;Check(Click("start-fight"),"new22 fight after replay");_pilotOn=false;_paused=true;yield return null;
            CheckMeter(0,"another new fight");
            _campaign=CampaignSave.Read(twoBars,ReviewTuning);_fight=_campaign.Active;_playback=null;BeginFight();_pilotOn=false;_paused=true;Save();yield return null;
            File.WriteAllText(Path.Combine(_capture,"restart-state.txt"),ViewSim.OrderedStateSnapshot);
            CheckMeter(2,"saved separate-process checkpoint");
            Log("Meter UI proof uses real parries, the real spell control, saves and recorded seeking; no native feel or balance approval implied.");
        }
    }
}
