using System;
using System.Collections;
using System.Linq;
using KindredSiege.Sim;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using UnityEngine;
using UnityEngine.UI;

namespace KindredSiege.Slice
{
    public sealed partial class CampaignArtLoop
    {
        BattleSetup LeadersArtSetup(bool retreat)
        {
            var source = TableSpellSetup(ChiefSpell.Eclipse, 1);
            var theirs = source.Side(1).ToArray();
            theirs[0] = new UnitSpec("bloodridge.champion", UnitClass.Skirmisher, Row.Front, UnitRole.Chief, 100, 800);
            if (retreat) theirs = new[] {
                new UnitSpec("bloodridge.guard1", UnitClass.Warden, Row.Front, UnitRole.Fighter, 100, 20),
                new UnitSpec("bloodridge.boss", UnitClass.Warden, Row.Back, UnitRole.Chief, 150, 30000) };
            return new BattleSetup("camp." + (retreat ? "944." : "943.") + CampaignTribes.BloodridgeId + ".WarParty", 7, source.Side(0), theirs,
                source.CopyTactics(0), source.CopyTactics(1), retreat ? 3600 : 420, 20, NerveRules.Off, 30, 100,
                SideRules.None, retreat ? SideRules.None : source.Rules(1),
                retreat ? source.Fight.WithLeaders() : source.Fight.WithChampionFight());
        }

        IEnumerator ValidateLeaders43()
        {
            yield return null;
            Check(_table42Review && ReviewRulesCap == 43, "isolated rules43 review enabled");
            _auto = false; Camera.main.targetTexture = null;
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay; _canvas.worldCamera = null;
            _campaign = FreshRoadPreview();
            SetPage(Page.Offers); yield return null;
            var championNode = _campaign.Road.All.First(n => _campaign.ChampionOn(n));
            Check(NodeDetail(championNode, NodeAhead, null).texts.Contains("CHAMPION"), "road details identify authoritative champion node");
            TableCapture("leaders43-road-badges");
            Check(Click("offer-0"), "open current road squad"); yield return null;
            Check(Click("start-fight"), "start current road fight"); yield return RealtimeQaWaitForBattle();
            _paused = true; _fight = null;
            var setup = LeadersArtSetup(false); var sim = new BattleSimulation(setup); var session = new SimSession(sim);
            while (!sim.IsFinished) session.Tick();
            var archive = _campaign.Archive.Add(setup, session.Log, sim);
            WatchFight(archive); yield return RealtimeQaWaitForBattle(); _paused = true;
            Check(ClassMovesOn && BeatsOn && ViewSim.ChampionFightOn, "rules43 art and beat paths enabled on real replay");
            string state = ViewSim.OrderedStateSnapshot;
            for (int side = 0; side < 2; side++)
            {
                var target = _figures.First(f => f.unit.Side == 1 - side && !f.unit.IsLeader);
                foreach (string move in new[] { "strike", "aim", "treat", "rise", "jaws" })
                {
                    var clip = _clips["Table42Review/ClassMoves/" + (side == 0 ? "ours" : "theirs") + "/" + move];
                    Check(clip.sheet.frames.Length == 9 && clip.texture.filterMode == FilterMode.Point,
                        "nine native point-filtered frames " + side + "/" + move);
                }
                var strike = _figures.First(f => f.unit.Side == side && f.unit.Class == UnitClass.Skirmisher && !f.unit.IsLeader);
                var ev = new BattleEvent(900 + side, BattleEventKind.Hit, side, strike.unit.Id, (int)strike.unit.Class,
                    target.unit.Id, (int)target.unit.Class, 10, ClassSpells.StrikeDetail);
                // Deliberate presentation probe: it does not issue a command or alter authoritative state.
                var b = new Beat { blow = ev, actor = strike, target = target }; b.effects.Add(ev);
                Check(ShapeBeat(b), "strike probe shapes on side " + side); _beat = b; _beatAge = 0;
                PrepareClassMoves(); Check(_classBlink == strike && !_beatRuns.ContainsKey(strike), "only Strike takes blink path " + side);
                _visualTime += b.lead; _beatAge = b.lead; ClassMoveEvent(ev, strike, target); DrawBattle();
                Check(Vector2.Distance(new Vector2(strike.root.anchoredPosition.x, -strike.root.anchoredPosition.y), _classBlinkContact) < 2,
                    "Strike reaches target contact " + side);
                TableCapture("class43-strike-" + side);
                _visualTime += .3f; DrawBattle(); Check(_classBlink == null, "Strike returns home " + side);
                _beat = null; _beatRuns.Clear();
                foreach (var kind in new[] { BattleEventKind.Heal, BattleEventKind.Guard, BattleEventKind.Hit })
                {
                    var cls = kind == BattleEventKind.Heal ? UnitClass.Medic : kind == BattleEventKind.Guard ? UnitClass.Warden : UnitClass.Trapper;
                    var actor = _figures.First(f => f.unit.Side == side && f.unit.Class == cls && !f.unit.IsLeader);
                    var receiver = kind == BattleEventKind.Hit ? target : strike;
                    string detail = kind == BattleEventKind.Hit ? BattleSimulation.TrapDetail : "";
                    int before = _classMoveFx.Count;
                    ClassMoveEvent(new BattleEvent(910, kind, side, actor.unit.Id, (int)cls, receiver.unit.Id, (int)receiver.unit.Class, 10, detail), actor, receiver);
                    Check(_classMoveFx.Count - before == (kind == BattleEventKind.Guard ? 2 : 1), "authored " + kind + " routes to correct band " + side);
                    _visualTime += .16f; DrawBattle(); TableCapture("class43-" + kind + "-" + side);
                    _visualTime += 2; DrawBattle();
                }
            }
            var champion = _figures.First(f => f.unit.Side == 1 && f.unit.IsLeader);
            _targeting = true; DrawBattle();
            Check(!ViewSim.LeaderProtected(champion.unit) && champion.target.IsInteractable(), "champion remains targetable from the start");
            _targeting = false;
            foreach (var actor in new[] { champion, _figures.First(f => f.unit.Side == 0 && f.unit.Class == UnitClass.Skirmisher) })
            {
                var target = _figures.First(f => f.unit.Side != actor.unit.Side && !f.unit.IsLeader);
                var ev = new BattleEvent(920, BattleEventKind.Hit, actor.unit.Side, actor.unit.Id, (int)actor.unit.Class,
                    target.unit.Id, (int)target.unit.Class, 10, "attack");
                var b = new Beat { blow = ev, actor = actor, target = target }; b.effects.Add(ev);
                Check(ShapeBeat(b), "ordinary blow shapes " + actor.unit.Id); _beat = b; _beatAge = 0;
                PrepareClassMoves(); Check(_classBlink == null && _beatRuns.ContainsKey(actor), "ordinary blow runs without blinking " + actor.unit.Id);
                _beat = null; _beatRuns.Clear();
            }
            ChiefSpellEvent(new BattleEvent(921, BattleEventKind.Windup, 1, champion.unit.Id, (int)champion.unit.Class, "", -1, 30, ChiefSpells.Name(ChiefSpell.Eclipse)));
            float cut = _cutUntil[1], clock = ChiefCutClock;
            yield return new WaitForSecondsRealtime(.1f);
            Check(ChiefCutClock == clock && Mathf.Abs(cut - clock - .9f) < .001f, "paused sequential cut-in uses presentation time, not wall clock");
            _cutUntil[1] = -100;
            var panel = BuildChiefPanel(1, ChiefSpell.Eclipse, champion); panel.gameObject.SetActive(true);
            var identity = panel.GetComponentsInChildren<RawImage>().Single(x => x.name == "Champion cut-in identity");
            Check(identity.texture == champion.idle.texture && !panel.GetComponentsInChildren<RawImage>().Any(x => x.name == "Chief portrait"),
                "champion cut-in uses actual champion sheet without a chief portrait");
            TableCapture("champion43-identity"); panel.gameObject.SetActive(false); Destroy(panel.gameObject);
            Check(state == ViewSim.OrderedStateSnapshot, "all class and identity probes preserve authoritative state");
            SeekPlayback(archive.Ticks); yield return RealtimeQaWaitForBattle();
            Check(_playback.Matched && _classMoveFx.Count == 0 && _classBlink == null, "champion end seek matches and clears class FX");
            SeekPlayback(0); yield return RealtimeQaWaitForBattle(); Check(_playback.Tick == 0, "champion replay restarts");

            setup = LeadersArtSetup(true); sim = new BattleSimulation(setup); session = new SimSession(sim);
            while (!sim.IsFinished) session.Tick();
            Check(sim.Events.Any(e => e.Kind == BattleEventKind.Break && e.Detail == LeaderRules.RetreatDetail), "fixed real fight produces retreat");
            archive = _campaign.Archive.Add(setup, session.Log, sim);
            WatchFight(archive); yield return RealtimeQaWaitForBattle();
            _targeting = true; DrawBattle();
            var protectedLeader = _figures.First(f => f.unit.Side == 1 && f.unit.IsLeader);
            Check(ViewSim.LeaderProtected(protectedLeader.unit) && !protectedLeader.target.IsInteractable(), "protected leader excluded from target buttons");
            _targeting = false;
            _paused = false; _speed = 1; float deadline = Time.realtimeSinceStartup + 120;
            while (!ViewOver && Time.realtimeSinceStartup < deadline) yield return null;
            Check(ViewOver && _playback.Matched && BeatTitleUp("RETREAT"), "real replay finishes with RETREAT and matching checksum");
            yield return new WaitForSecondsRealtime(.8f); TableCapture("leaders43-retreat-moving");
            yield return new WaitForSecondsRealtime(1.4f); TableCapture("leaders43-retreat-offscreen");
            var leader = _figures.First(f => f.unit.Side == 1 && f.unit.IsLeader);
            Check(leader.root.anchoredPosition.x > W && leader.unit.Alive, "retreating leader leaves screen alive");
            foreach (var beast in new[] { BeastKit.SandShark, BeastKit.CageBear })
            {
                _playback = null; _campaign = PitBeastFixture(beast);
                _fight = _campaign.StartEncounter(_campaign.Offers.ToList().FindIndex(x => x.Beast == beast));
                while (!_fight.Over) _fight.Tick(); _campaign.Finish(_fight); archive = _campaign.Archive.Fights.Last();
                WatchFight(archive); yield return RealtimeQaWaitForBattle();
                Check(BeatsOn && ViewSim.LeadersRulesOn, "current beast uses sequential presentation " + beast);
                _paused = false; _speed = 1; int held = 0; float beastDeadline = Time.realtimeSinceStartup + 240;
                bool movingCapture = false;
                while (!ViewOver && Time.realtimeSinceStartup < beastDeadline)
                {
                    if (BeatHolding) held++;
                    if (!movingCapture && BeatHolding && _beat.landed)
                    { TableCapture("beast43-beat-" + beast); movingCapture = true; }
                    foreach (var f in _figures)
                        if (ViewSim.TryPartOf(f.unit, out _) && _beatRuns.ContainsKey(f))
                            throw new InvalidOperationException("A beast body part was moved as a humanoid runner");
                    yield return null;
                }
                Check(ViewOver && _playback.Matched && held > 0, "current beast sequential playback reaches matching ending " + beast);
                _paused = true;
                SeekPlayback(Math.Min(150, archive.Ticks)); yield return RealtimeQaWaitForBattle(); TableCapture("beast43-" + beast);
                SeekPlayback(archive.Ticks); yield return RealtimeQaWaitForBattle(); Check(_playback.Matched, "current beast seek checksum " + beast);
                SeekPlayback(0); yield return RealtimeQaWaitForBattle(); Check(_playback.Tick == 0, "current beast restart " + beast);
            }
        }
    }
}
