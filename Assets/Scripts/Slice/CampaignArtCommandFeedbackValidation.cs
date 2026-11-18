using System.Collections;
using System.IO;
using System.Linq;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;

namespace KindredSiege.Slice
{
    public sealed partial class CampaignArtLoop
    {
        IEnumerator ValidateCommandFeedback()
        {
            Log("Command feedback: accepted Focus, target, actual hit, expiry, save and replay; chief loss facts.");
            _savePath=Path.Combine(_capture,"command-feedback-save.txt");
            _squadReview=_clearReview=_kitReview=_roadReview=_gatesReview=true;
            NewReviewRoute();yield return null;
            Check(Click("offer-0"),"select isolated road encounter");yield return null;
            _auto=false;Check(Click("start-fight"),"start isolated road fight");_auto=true;_pilotOn=false;_paused=true;yield return null;
            HudTicks(10);
            var sim=ViewSim;
            Check(sim.SquadScreenOn&&_focusCorners.All(c=>c!=null&&!c.enabled),"no Focus brackets before an order");
            var target=sim.Units.Where(u=>u.Side==1&&u.Alive&&!u.IsLeader).OrderByDescending(u=>u.Hp).First();
            long focusedAt=sim.CurrentTick;
            Check(Click("order-2")&&_targeting,"real Focus button opens target selection");
            SelectFigure(target);RefreshFight();DrawBattle();
            Check(sim.FocusedId(0)==target.Id&&_focusCorners.All(c=>c.enabled),"accepted Focus marks its authoritative target");
            Check(_focusTargetLabel.key.Contains("FOCUS"),"Focus target has a persistent readable label");
            string before=sim.OrderedStateSnapshot;
            DrawFocusTarget(sim);
            Check(sim.OrderedStateSnapshot==before,"drawing Focus feedback does not mutate combat state");
            Capture("focus-01-target");
            bool hit=false;long hitAt=-1;
            for(int i=0;i<BattleSimulation.FocusTicks+5&&!ViewOver;i++)
            {
                HudTicks(1);
                if(!hit&&_focusHitTarget==target.Id&&sim.CurrentTick-_focusHitTick<12)
                {
                    hit=true;hitAt=_focusHitTick;
                    Check(sim.Events.Any(e=>e.Kind==BattleEventKind.Hit&&e.ActorSide==0&&e.TargetId==target.Id&&e.Amount>0&&e.Tick==_focusHitTick),"Focus hit pulse has a matching landed hit");
                    Check(_focusDamage==sim.OrderResults(0).Last(r=>r.Order==Directive.FocusFire).Effects.Where(e=>e.Kind==OrderEffectKind.Damage).Sum(e=>e.Amount)&&_focusDamage>0,
                        "running Focus number equals actual damage received during this order");
                    _paused=false;_hover="";RefreshFight();DrawBattle();CaptureAsDrawn("focus-live-hit");_paused=true;
                    Capture("focus-02-hit");
                    string beforeRenewal=CampaignSave.Write(_campaign);
                    Check(Click("order-2")&&_targeting,"renew Focus immediately after its landed hit");
                    SelectFigure(target);RefreshFight();DrawBattle();
                    Check(_focusHitTarget==""&&_focusDamage==0,"new Focus does not claim the previous order's hit or damage, including same-tick hits");
                    GateRestore(beforeRenewal);sim=ViewSim;RefreshFight();DrawBattle();
                }
                if(i%60==0)yield return null;
            }
            Check(hit,"real focused target receives a visible confirmed hit pulse");
            Check(!_focusRing.enabled&&!_focusRunning,"active Focus marker ends when its target falls or its order expires");
            if(!ViewOver)
            {
                var restored=CampaignSave.Read(CampaignSave.Write(_campaign));
                Check(restored.Active.Sim.OrderedStateSnapshot==sim.OrderedStateSnapshot,"Focus presentation needs no new authoritative save state");
            }
            int guard=0;while(!ViewOver&&guard++<3600){HudTicks(1);if(guard%120==0)yield return null;}
            Check(ViewOver,"isolated ordinary fight finishes");
            ShowResult();yield return null;
            var archive=_campaign.Archive.Fights.Last();
            Check(_campaign.Archive.TryReplay(archive.FightId,out var replay,out var why),"Focus archive verifies: "+why);
            WatchFight(archive,0,archive.Ticks);yield return null;
            SeekPlayback(focusedAt+1);yield return null;
            Check(ViewSim.FocusedId(0)==target.Id&&_focusCorners.All(c=>c.enabled),"recorded Focus target reconstructs during replay");
            SeekPlayback(hitAt+1);yield return null;
            Check(_focusHitTarget==target.Id&&ViewSim.CurrentTick-_focusHitTick<12,"replay seek reconstructs the actual Focus hit pulse");
            SeekPlayback(0);yield return null;
            Check(_focusCorners.All(c=>!c.enabled),"rewinding before Focus clears its marker");
            SeekPlayback(archive.Ticks);yield return null;
            Check(ViewSim.OrderedStateSnapshot==replay.OrderedStateSnapshot,"Focus overlay preserves exact archived ending");
            LeavePlayback();CloseModal();yield return null;

            string owner=Argument("-ksOwner14");
            Check(owner!=null&&CampaignSave.TryReadFile(owner,null,out _,out _),"frozen rules 14 owner save is supplied for loss facts");
            CampaignSave.TryReadFile(owner,null,out var old,out why);
            var chiefArchive=old.Archive.Fights.Last(f=>f.Setup.Fight.RaidChief&&f.Setup.Fight.SquadScreen);
            Check(old.Archive.TryReplay(chiefArchive.FightId,out var chief,out why),"original owner chief archive verifies: "+why);
            Check(chief.Ending.How==FightEnd.NerveBreak&&chief.Ending.Side==0,"owner chief outcome is authoritative team nerve defeat");
            var lines=new PxLines().Add(Ending(chief.Ending,EncounterKind.Chief,false),PxBone);
            ChiefOutcomeLines(lines,chief,false);
            string facts=string.Join("\n",lines.texts);
            Check(facts.Contains(chief.Ending.Standing+" still standing")&&facts.Contains("Zero team nerve"),"chief recap explains nerve defeat despite survivors");
            Check(facts.Contains(chief.Leader(0).Hp+"/"+chief.Leader(0).MaxHp)&&facts.Contains(chief.Leader(1).Hp+"/"+chief.Leader(1).MaxHp),"chief recap retains exact leader health and nearly defeated chief health");
            Modal("CHIEF LOSS FACTS",facts);yield return null;Capture("focus-03-chief-loss-facts");CloseModal();
            Log("Command feedback checks complete; scripted evidence, no claim of owner approval or hands-on feel.");
        }
    }
}
