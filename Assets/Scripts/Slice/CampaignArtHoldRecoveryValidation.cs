using System.Collections;
using System.IO;
using System.Linq;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;

namespace KindredSiege.Slice
{
    public sealed partial class CampaignArtLoop
    {
        IEnumerator ValidateHoldRecoveryHud()
        {
            IntegrationCap(23); // Historical ring/recovery contract; tight26 has its own validator.
            _savePath = Path.Combine(_capture, "hold-recovery-save.txt");
            _campaign = FreshRaidPreview(); _fight = _campaign.Active; _playback = null;
            BeginFight(); _pilotOn = false; _paused = true;
            yield return null;
            string start = CampaignSave.Write(_campaign);
            Check(HoldRecoveryTime(1)=="0.1s", "one remaining recovery tick never displays zero seconds");
            bool early = false;
            for (int i = 0; i < 1200 && !ViewOver; i++)
            {
                if (_threatReads.Any(t => t.Name == ChiefKits.CleaveName && t.BeatTicks == FightRules.PerfectHoldTicks + 10))
                { early = true; break; }
                HudTicks(1);
                if (i % 120 == 0) yield return null;
            }
            Check(early, "real chief Cleave reaches the early-Hold fixture");
            _paused = false; RefreshFight(); DrawBattle();
            Check(Click("order-0"), "early live Hold uses the real button");
            _paused = true; HudTicks(20);
            Check(_threatReads.Any(t => t.ParryWindow), "raw threat enters its timing window");
            Check(ViewSim.CanUse(0, Directive.Hold, out _), "another guard command remains accepted");
            int wait = ViewSim.HoldRefreshTicksLeft(0);
            Check(wait > 0, "authoritative timing rejects a fresh window despite CanUse");
            _paused = false; RefreshFight(); DrawBattle();
            Check(!_parryCall.key.Contains("HOLD NOW") && _parryCall.key.Contains("RECOVERING"), "fighter warns about recovery instead of false Hold now");
            Check(!_orderEffect[0].key.Contains("PARRY NOW") && _orderEffect[0].key.Contains("WAIT"), "Hold button agrees with the recovery cue");
            Check(_parryOuter.color != (UnityEngine.Color)PxGold && !_raidFlash.enabled, "recovering Hold produces no gold ring or spell flash");
            Check(ThreatAnswers(ViewSim, _threatReads.First(t => t.ParryWindow)).Contains("RECOVERING"), "Details uses the same authoritative recovery");
            Capture("01-hold-recovering"); CheckCleanTextBounds();
            _paused = true;
            string saved = CampaignSave.Write(_campaign), snapshot = ViewSim.OrderedStateSnapshot;
            GateRestore(saved); yield return null;
            Check(ViewSim.OrderedStateSnapshot == snapshot && ViewSim.HoldRefreshTicksLeft(0) == wait, "save/reload restores the precise recovery and state");
            _paused = false; RefreshFight(); DrawBattle();
            Check(_parryCall.key.Contains("RECOVERING"), "reload preserves the truthful cue");
            _paused = true;
            GateRestore(start); yield return null;
            for (int i = 0; i < 1200 && !ViewOver && !_threatReads.Any(t => t.Name == ChiefKits.CleaveName && t.ParryWindow); i++)
            { HudTicks(1); if (i % 120 == 0) yield return null; }
            Check(ViewSim.HoldRefreshTicksLeft(0) == 0 && _threatReads.Any(t => t.ParryWindow), "a fresh timed press becomes available");
            _paused = false; RefreshFight(); DrawBattle();
            Check(_parryCall.key.Contains("HOLD NOW") && _orderEffect[0].key.Contains("PARRY NOW"), "ring and button agree on a fresh actionable parry");
            Capture("02-fresh-parry");
            _pilotOn = true; RefreshFight(); DrawBattle();
            Check(!_parryCall.key.Contains("HOLD NOW") && !_orderEffect[0].key.Contains("PARRY NOW"), "pilot control never asks for a live parry press");
            _pilotOn = false; RefreshFight(); DrawBattle();
            Check(Click("order-0"), "fresh parry uses the real Hold button");
            _paused = true; HudTicks(18);
            Check(ViewSim.Events.Any(e => e.Kind == BattleEventKind.Parry && e.Detail == ChiefKits.CleaveName), "fresh advertised Hold actually parries Cleave");
            Capture("03-confirmed-parry");
            string owner = Argument("-ksOwner14");
            Check(owner != null && CampaignSave.TryReadFile(owner, null, out _, out _), "frozen owner replay supplied for noninteractive cues");
            CampaignSave.TryReadFile(owner, null, out var old, out _);
            _campaign = old;
            var recorded = old.Archive.Fights.First(f => f.FightId == "camp.4.raiders.warparty");
            WatchFight(recorded, 0, recorded.Ticks); yield return null;
            SeekPlayback(503); yield return null;
            Check(ViewSim.HoldRefreshTicksLeft(0)==1, "original owner one-tick recovery fixture stays exact");
            Check(!_parryCall.key.Contains("HOLD NOW") && _parryCall.key.Contains("REPLAY"), "replay never asks for a live Hold");
            Capture("04-owner-replay");
            Log("Hold recovery HUD checks complete. Combat rules and the future three-bar system were not changed.");
        }
    }
}
