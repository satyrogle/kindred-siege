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
        IEnumerator ValidateCombatReceipts()
        {
            _savePath=Path.Combine(_capture,"receipts-save.txt");
            string fixture=Argument("-ksReceiptFixture");
            Check(fixture!=null,"frozen owner replay fixture supplied");
            _campaign=CampaignSave.Read(File.ReadAllText(fixture));_fight=_campaign.Active;
            var recorded=_campaign.Archive.Find("camp.2.raiders.warparty");
            Check(recorded!=null&&_campaign.Archive.TryReplay(recorded.FightId,out _,out _),"actual owner road replay verifies before presentation");
            string saved=CampaignSave.Write(_campaign);
            WatchFight(recorded);yield return null;
            SeekPlayback(550);yield return null;
            Check(_focusRing.enabled&&_focusCorners.All(c=>c.enabled),"actual Focus target has crosshair and draining ring");
            Check(_focusRing.fillAmount>0&&_focusRing.fillAmount<=1,"ring uses actual remaining Focus time");
            float before=_focusRing.fillAmount;SeekPlayback(558);yield return null;
            Check(_focusRing.fillAmount<before,"ring visibly drains as recorded Focus runs");
            Capture("receipts-focus-running");
            SeekPlayback(610);yield return null;
            Check(_focusOutcome=="BROKE"&&_focusOutcomeLabel.key.StartsWith("BROKE|"),"recorded flee ends Focus with BROKE");
            Check(_focusDamage==203,"running total matches203 recorded target damage, not bonus damage");
            Capture("receipts-focus-broke");
            SeekPlayback(0);yield return null;
            Check(!_focusRing.enabled&&_focusDamage==0&&_focusOutcomeLabel.key.StartsWith("|"),"rewind clears future Focus damage/outcome");
            SeekPlayback(recorded.Ticks);yield return null;
            Check(_playback.Matched&&CampaignSave.Write(_campaign)==saved,"Focus presentation preserves exact replay and owner campaign");

            // Explicit presentation fixture: a real Focus kill beside two standing enemies.
            // Its temporary setup is never used as a campaign balance result or owner history.
            var setup=new BattleSetup("camp.999.raiders.warparty",71,
                new[]{new UnitSpec(Campaign.LeaderId,UnitClass.Marksman,Row.Back,UnitRole.Pal,100,100)},
                new[]{new UnitSpec("receipt-target",UnitClass.Warden,Row.Front,UnitRole.Fighter,1,1),
                    new UnitSpec("receipt-neighbour",UnitClass.Warden),new UnitSpec("receipt-other",UnitClass.Warden)},
                null,null,3600,5,NerveRules.On(),side0Rules:recorded.Setup.Rules(0).WithPals(
                    new PalKit(new[]{new System.Collections.Generic.KeyValuePair<ShadeRoute,int>(ShadeRoute.Fear,1)},100)),fightRules:recorded.Setup.Fight);
            var fixtureSim=new BattleSimulation(setup);var session=new SimSession(fixtureSim);
            Check(session.TryIssue(Directives.CommandKind,"0","receipt-target",0,0,Directives.Name(Directive.FocusFire),out _),"fixture gives a real Focus command");
            int fixtureGuard=0;while(!fixtureSim.IsFinished&&fixtureGuard++<3600)session.Tick();
            Check(fixtureSim.IsFinished,"fixed kill/panic presentation fixture finishes");
            long kill=fixtureSim.Events.First(e=>e.Kind==BattleEventKind.Kill&&e.TargetId=="receipt-target").Tick;
            _campaign=Campaign.New(71);var receiptArchive=_campaign.Archive.Add(setup,session.Log,fixtureSim);
            WatchFight(receiptArchive);SeekPlayback(kill+1);yield return null;
            Check(_focusOutcome=="KILLED"&&_focusOutcomeLabel.key.StartsWith("KILLED|"),"a recorded kill inside Focus displays KILLED");
            Check(ViewSim.Events.Any(e=>e.Kind==BattleEventKind.Trick&&e.Detail==ShadeRules.FearDetail),"killing the focused enemy really triggers Fear");
            Check(_fearPanic.Values.Any(p=>p.key.StartsWith("PANIC|")),"actual panic status after Fear is visible on a surviving enemy");
            Capture("receipts-killed-panic");

            // Fixed current-rule UI scenario, not a balance sweep. Focus the largest enemy to
            // exercise a real expiry while the target is still standing.
            _playback=null;IntegrationCap(30);_campaign=FreshRoadPreview();
            Check(_campaign.TryTakeRoute(ShadeRoute.Fear),"real campaign equips approved Fear route");
            _fight=_campaign.StartEncounter(0);BeginFight();_pilotOn=false;_paused=true;_speed=.5f;yield return null;
            HudTicks(10);var sim=ViewSim;
            var target=sim.Units.Where(u=>u.Side==1&&u.Alive).OrderByDescending(u=>u.Hp).First();
            Check(Click("order-2")&&_targeting,"actual Focus control enters targeting");SelectFigure(target);RefreshFight();DrawBattle();
            Check(_focusEdges.All(e=>e.enabled),"Fear gives the focused crosshair its purple edge");
            Check(_focusRing.fillAmount==1,"fresh Fear Focus begins with full priced duration");
            int left=sim.FocusTicksLeft(0);Check(left<BattleSimulation.FocusTicks,"ring includes Fear's shorter authoritative price");
            bool redHit=false;
            for(int tick=0;tick<=left+1;tick++)
            {
                HudTicks(1);
                if(_numbers.Any(n=>n.focus)){redHit=true;Check(_numbers.Where(n=>n.focus).All(n=>n.image.rectTransform.sizeDelta.y>=32),"focused hits bypass small-hit suppression and use larger numbers");}
                if(tick%30==0)yield return null;
            }
            Check(redHit,"actual Focus attacks produced red enlarged damage feedback");
            Check(target.Alive&&_focusOutcome=="NO FEAR"&&_focusOutcomeLabel.key.StartsWith("NO FEAR|"),"Fear expiry on a standing target shows NO FEAR");
            Capture("receipts-no-fear");
            string noFear=CampaignSave.Write(_campaign);_campaign=CampaignSave.Read(noFear);_fight=_campaign.Active;BeginFight();_pilotOn=false;_paused=true;yield return null;
            Check(_focusOutcome=="NO FEAR","save/resume reconstructs expiry without inventing a later kill");

            // The original chief validator proves the real command/parry path. Extend it with
            // the delivered counter clip and its pause/reset lifecycle.
            IntegrationCap(30);_campaign=FreshRaidPreview();_fight=_campaign.Active;_playback=null;BeginFight();_paused=false;_pilotOn=false;_speed=0;yield return null;
            sim=ViewSim;int guard=0;
            while(!ViewOver&&!sim.Threats(0).Any(t=>t.ParryWindow)&&guard++<1800)HudTicks(1);
            Check(guard<1800&&!ViewOver,"chief reaches a genuine parry beat for delivered counter effect");
            var threat=sim.Threats(0).First(t=>t.ParryWindow);var attacker=FindFigure(threat.ActorId);
            Check(Click("order-0"),"live real Hold starts parry for counter visual");HudTicks(8);
            Check(sim.ParryBars(0)==1,"counter follows an actual parry and earned meter bar");
            _visualTime=attacker.counterAt+.08f;DrawBattle();yield return null;
            Check(attacker.counterImage!=null&&attacker.counterImage.enabled,"delivered star plays on the actual counter victim");
            Check(attacker.hitMaterial.GetFloat(CounterRim)==1,"counter victim has approved red rim strobe");
            Check(_figures.Where(f=>f!=attacker&&f.hitMaterial!=null).All(f=>f.hitMaterial.GetFloat(CounterRim)==0),"counter strobe never bleeds onto other fighters");
            Check(_meterSuns.texture==_meterStates[1]&&_meterSuns.rectTransform.sizeDelta==new Vector2(288,64),"one actual parry swaps to one lit sun at approved2x size");
            Capture("receipts-counter-star");
            _paused=true;DrawBattle();Check(!attacker.counterImage.enabled&&attacker.hitMaterial.GetFloat(CounterRim)==0,"pause clears counter star and rim");
            _paused=false;_visualTime=attacker.counterAt+1;DrawBattle();
            Check(!attacker.counterImage.enabled&&attacker.hitMaterial.GetFloat(CounterRim)==0,"counter expires without stale shader state");
            string snapshot=sim.OrderedStateSnapshot;DrawBattle();Check(sim.OrderedStateSnapshot==snapshot,"new presentation mutates no authoritative fight state");
            Log("Combat receipts checked. Live recognition, native input and subjective audio remain owner review.");
        }
    }
}
