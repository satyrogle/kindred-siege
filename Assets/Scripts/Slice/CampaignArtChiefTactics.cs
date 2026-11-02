using System;
using System.Collections.Generic;
using System.Text;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using UnityEngine;
using UnityEngine.UI;

namespace KindredSiege.Slice
{
    public sealed partial class CampaignArtLoop
    {
        // Read from ViewSim so pause, resume and historical seeking rebuild the same cues.
        RectTransform _tacticsScreen, _tacticsPanel;
        PxText _gloryText, _forecastText, _movesText, _chiefCue;
        Image _tacticsBack;
        readonly Dictionary<string, PxText> _tacticsMarks = new Dictionary<string, PxText>();
        readonly Dictionary<string, Image[]> _tacticsTargets = new Dictionary<string, Image[]>();

        static string ForecastAmount(ChiefForecast f) => f.Amount +
            (f.Move == ChiefMove.Eclipse || f.Move == ChiefMove.SunMantle ? "% HP" : " HP");

        void SetTacticsLines(PxText label, string text, Color32 colour)
        {
            var parts = new List<string>();
            foreach (var line in text.Split('\n'))
            {
                if (parts.Count > 0) parts.Add("\n");
                parts.Add(line);
            }
            var colours = new Color32[parts.Count];
            for (int i = 0; i < colours.Length; i++) colours[i] = colour;
            SetPx(label, parts.ToArray(), colours);
        }

        static string JobStatus(BattleSimulation sim, BandJob job) => sim.JobHolderId(1, job).Length == 0 ? "--" : sim.JobDown(1, job) ? "DOWN" : "UP";

        static string ChiefWallWord(string detail)
        {
            switch (detail)
            {
                case ChiefSpells.WallUp: return "STEEL STORM";
                case ChiefSpells.WallSmashed: return "WALL SMASHED";
                case ChiefSpells.WallFell: return "WALL FELL";
                case ChiefKits.WallHalf: return "HALF WALL";
                case ChiefKits.WallLockdown: return "LOCKDOWN WALL";
                case ChiefKits.WallRebuilt: return "WALL REBUILT";
                default: return "WALL";
            }
        }

        static string MoveAnswer(ChiefMove move)
        {
            switch (move)
            {
                case ChiefMove.Cleave: return "Time Hold / Warden guard";
                case ChiefMove.Brand: return "Fall Back / guard / Medic";
                case ChiefMove.Eclipse: return "Time Hold / Fall Back / Focus chief";
                case ChiefMove.BurningRing: return "Focus chief with Marksman / Fall Back";
                case ChiefMove.SunMantle: return "Focus band / Fall Back";
                case ChiefMove.SteelStorm: return "Snare Warden / Unleash risen wall";
                case ChiefMove.Hook: return "Time Hold / Warden guard";
                case ChiefMove.RifleLine: return "Focus shooters / Fall Back / Medic";
                case ChiefMove.IronAdvance: return "Hold / Unleash / Fall Back gives ground";
                case ChiefMove.Overclock: return "Focus Medic / Hold flurry / snare";
                default: return "";
            }
        }

        void DrawChiefTactics(BattleSimulation sim)
        {
            if (_tacticsScreen != _screen)
            {
                _tacticsScreen = _screen; _tacticsMarks.Clear(); _tacticsTargets.Clear();
                _tacticsPanel = null; _chiefCue = null;
                if (sim.Kit(1) != ChiefKit.Bloodridge && sim.Kit(1) != ChiefKit.Skullpeak) return;
                _tacticsPanel = Rect(_readoutLayer, "Chief tactics", 0, 0, W, H);
                _tacticsBack = Panel(_tacticsPanel, 22 * Px, 102 * Px, 676 * Px, 75 * Px, new Color(.06f,.05f,.07f,.9f));
                _gloryText = PxLabel(_tacticsPanel, 28, 106);
                _forecastText = PxLabel(_tacticsPanel, 28, 119);
                _movesText = PxLabel(_tacticsPanel, 370, 106);
                PxButton("chief-moves", "MOVES", 286, 103, 70, 14, ShowChiefMoves, _tacticsPanel);
                foreach (var f in _figures)
                {
                    _tacticsMarks[f.unit.Id] = PxLabel(f.readoutRoot, 0, -31, 1);
                    var corners = new Image[2];
                    for (int i = 0; i < corners.Length; i++)
                        corners[i] = Panel(f.readoutRoot, (i == 0 ? -f.barWidth / 2 - 5 : f.barWidth / 2 + 3) * Px,
                            -4 * Px, 2 * Px, 12 * Px, PxOrange);
                    _tacticsTargets[f.unit.Id] = corners;
                    if (f.unit.Side == 1 && f.unit.Role == UnitRole.Chief)
                        _chiefCue = PxLabel(f.readoutRoot, 0, -Mathf.CeilToInt(f.head / Px) - 24, 1);
                }
            }
            if (_tacticsPanel == null) return;
            bool live = !ViewOver && !sim.AwaitingVerdict;
            _tacticsPanel.gameObject.SetActive(!ViewOver);
            var rank = sim.GloryRankOf(0);
            SetPx(_gloryText, "GLORY " + ChiefKits.Letter(rank) + "  " + sim.Glory(0) + "/" + ChiefKits.GloryMax +
                (rank == GloryRank.S ? "  5TH: 2X" : ""), rank == GloryRank.S ? PxGold : PxMint);
            var forecasts = live ? sim.Forecasts(1) : Array.Empty<ChiefForecast>();
            var forecast = new StringBuilder();
            for (int i = 0; i < forecasts.Count; i++)
            {
                var f = forecasts[i];
                if (i > 0) forecast.Append('\n');
                forecast.Append("F").Append(i + 1).Append(" ").Append(ChiefKits.Name(f.Move)).Append(" ")
                    .Append(Seconds(f.TicksLeft)).Append("  ").Append(ForecastAmount(f));
                if (f.Move == ChiefMove.Eclipse) forecast.Append(" / FRONT");
                if (f.Move == ChiefMove.SunMantle) forecast.Append(" / STRIKERS");
                if (f.Move == ChiefMove.Cleave) forecast.Append(" AVG");
                if (f.SecondTargetId.Length > 0)
                {
                    // The second Brand's HP share can differ. Its emitted amount is authoritative too.
                    for (int e = sim.Events.Count - 1; e >= 0; e--)
                    {
                        var receipt = sim.Events[e];
                        if (receipt.Kind != BattleEventKind.Forecast || receipt.ActorSide != 1 ||
                            receipt.TargetId != f.SecondTargetId || receipt.Detail != ChiefKits.Name(f.Move)) continue;
                        forecast.Append(" / B ").Append(receipt.Amount).Append(" HP"); break;
                    }
                }
            }
            if (forecast.Length == 0) forecast.Append(sim.ForecastOn(1) ? "FORECAST: waiting for next move" :
                sim.Kit(1) == ChiefKit.Skullpeak && sim.ChiefTacticsOn ? "BAND JOBS: target holders to slow moves" : "No forecasts in these recorded rules.");
            if (sim.ForecastOn(1)) forecast.Append('\n').Append("F marks target; HP before armour");
            if (sim.Kit(1) == ChiefKit.Skullpeak && sim.ChiefTacticsOn)
                forecast.Append('\n').Append("SHIELDBEARER ").Append(JobStatus(sim, BandJob.Shieldbearer))
                    .Append(" / SPOTTER ").Append(JobStatus(sim, BandJob.Spotter));
            SetTacticsLines(_forecastText, forecast.ToString(), PxGold);

            var moves = new List<string>();
            if (live)
            {
                bool pausedLive = _playback == null && _paused;
                AddMove(moves, "CLEAVE", sim.CleaveTicks(1), sim.CleaveParryable(1) ? pausedLive ? "RESUME TO PARRY / GUARD" : "TIME HOLD" : "GUARD / SOAK");
                int brand = 0;
                foreach (var u in sim.Units) if (u.Side == 0) brand = Math.Max(brand, u.BrandTicks);
                AddMove(moves, "BRAND", brand, "FALL BACK / GUARD / MEDIC");
                AddMove(moves, "RING", sim.RingTicks(1), "FOCUS CHIEF / FALL BACK");
                AddMove(moves, "MANTLE", sim.MantleTicksLeft(1), "FOCUS BAND / FALL BACK");
                AddMove(moves, "HOOK", sim.HookTellTicks(1), sim.HookParryable(1) ? pausedLive ? "RESUME TO PARRY / GUARD" : "TIME HOLD / GUARD" : "GUARD");
                AddMove(moves, "RIFLE LINE", sim.RifleLineTicks(1), "FOCUS SHOOTER / FALL BACK");
                AddMove(moves, "ADVANCE", sim.AdvanceTicksLeft(1), "HOLD / UNLEASH / FALL BACK");
                AddMove(moves, "JAB", sim.JabTicksLeft(1), "FOCUS MEDIC");
                AddMove(moves, "OVERCLOCK", sim.OverclockTicksLeft(1), "HOLD / SNARE");
                if (sim.Kit(1) == ChiefKit.Skullpeak)
                {
                    moves.Add("WALL: HITS " + sim.WallPercentNow(1) + "%" + (sim.WallSmashable(1) ? " / UNLEASH SMASHES" : " / NOT SMASHABLE"));
                    if (sim.WallRebuildTicks(1) > 0) moves.Add("OPEN " + Seconds(sim.WallOpenTicks(1)) + " / REBUILD " + Seconds(sim.WallRebuildTicks(1)));
                }
            }
            string phase = sim.BloodSun(1) ? "BLOOD SUN" : sim.InLockdown(1) ? "LOCKDOWN" : "CHIEF MOVES";
            SetTacticsLines(_movesText, phase + (moves.Count > 0 ? "\n" + string.Join("\n", moves) : "\nRead the tell; mix your answers."), PxBone);
            _tacticsBack.rectTransform.sizeDelta = new Vector2(676 * Px,
                Math.Max(40, Math.Max(19 + (Math.Max(1, forecasts.Count) + 1) * 10, 16 + moves.Count * 10)) * Px);
            if (_chiefCue != null) SetPx(_chiefCue, live ? (forecasts.Count > 0 ? "NEXT: " + ChiefKits.Name(forecasts[0].Move) + " " + Seconds(forecasts[0].TicksLeft) : phase) : "", PxOrange);
            foreach (var f in _figures)
            {
                var u = f.unit; var marks = new List<string>();
                bool visible = live && u.Alive && !u.Benched && !ChiefCutInVisible;
                if (visible)
                {
                    for (int i = 0; i < forecasts.Count; i++)
                        if (forecasts[i].TargetId == u.Id || forecasts[i].SecondTargetId == u.Id ||
                            (forecasts[i].Move == ChiefMove.Eclipse && u.Side == 0 && u.Row == Row.Front))
                            marks.Add("F" + (i + 1) + (forecasts[i].SecondTargetId.Length > 0 ? forecasts[i].SecondTargetId == u.Id ? "B" : "A" : ""));
                    if (sim.CleaveTargetId(1) == u.Id) marks.Add("CLEAVE");
                    if (u.BrandTicks > 0) marks.Add("BRAND " + Seconds(u.BrandTicks));
                    if (sim.RingTargetId(1) == u.Id) marks.Add("RING LANE");
                    if (sim.HookTargetId(1) == u.Id) marks.Add("HOOK");
                    if (sim.RifleTargetId(1) == u.Id) marks.Add("VOLLEY");
                    if (u.HookTicks > 0) marks.Add("DRAGGED " + Seconds(u.HookTicks));
                    if (u.RifleTicks > 0) marks.Add("SHOOTER");
                    if (sim.JabMedicId(1) == u.Id) marks.Add("JABBING");
                    if (sim.JobOf(u) != BandJob.None) marks.Add(ChiefKits.Name(sim.JobOf(u)));
                }
                var label = _tacticsMarks[u.Id];
                int markY = -31 - Math.Max(0, marks.Count - 1) * 10;
                if (label.y != markY) { label.y = markY; label.key = ""; }
                SetTacticsLines(label, string.Join("\n", marks), u.Side == 0 ? PxOrange : PxBlue);
                foreach (var edge in _tacticsTargets[u.Id]) edge.enabled = marks.Count > 0;
            }
        }

        static void AddMove(List<string> lines, string name, int ticks, string answer)
        {
            if (ticks > 0) lines.Add(name + " " + Seconds(ticks) + " / " + answer);
        }

        void ShowChiefMoves()
        {
            _paused = true;
            if(RaidHud){Modal("RAID CHIEF",ViewSim.TightParryOn?"SOLO BLOODRIDGE CHIEF\nHealth weakens his posture recovery. Empty his posture to make him kneel. Spare recruits; Execute takes his gear.\n\n"+TightGuide:RaidGuide);return;}
            var sim = ViewSim;
            var text = new StringBuilder(_playback!=null?"PAUSED. Moves in this recorded fight:\n\n":"PAUSED. This chief has these moves:\n\n");
            foreach (ChiefMove move in Enum.GetValues(typeof(ChiefMove)))
                if (move != ChiefMove.None && (sim.KitMoves(1) & move) != 0)
                    text.Append(ChiefKits.Name(move)).Append(": ").Append(sim.SquadFirstOn ? "Answers: "+MoveAnswer(move)+". A timed live Hold can also parry the real beat." : sim.ParryCenterOn ? "Time live Hold at its beat. Safe answers: " + MoveAnswer(move) : MoveAnswer(move)).Append("\n\n");
            text.Append(sim.SquadFirstOn ? "Mix answers and cash squad marks. Repeated parries give less Glory; small beats do not crack nerve. S empowers the fifth order.\n" : sim.ParryCenterOn ? "Chain parries to crack nerve and raise Glory. S empowers the fifth order.\n" : "Mix answers to raise Glory. Higher ranks pay more points; S empowers the fifth order.\n");
            text.Append("A Hold issued while paused cannot parry. Resume before timing it.\n");
            if (sim.ForecastOn(1)) text.Append("F numbers match the target marks. Cleave shows its average hit; Brand and Ring show a share. Eclipse marks the front row and uses % HP; Mantle is % HP burned from each striker. Values are before armour.\n");
            if (sim.Kit(1) == ChiefKit.Skullpeak && sim.ChiefTacticsOn)
                text.Append("Kill Shieldbearer: walls build slower. Kill Spotter: Rifle Line aims slower. Lockdown: smash a whole wall, hit during OPEN, then wait for REBUILD before another smash.\n");
            Modal("CHIEF MOVES", text.ToString());
        }

        void ChiefTacticsEvent(BattleEvent e)
        {
            if (_seeking || !ViewSim.ChiefKitsOn) return;
            if (e.Kind == BattleEventKind.Land && ViewSim.Kit(1) != ChiefKit.None) Feed(e.Detail.Replace('.', ' '));
            if (e.Kind == BattleEventKind.Combo && (e.Detail == ChiefKits.Sunbreaker || e.Detail == ChiefKits.Breach))
            { Callout(e.ActorSide, e.Detail, PxGold); Feed("Chain discovered: " + e.Detail); }
            if (e.Kind == BattleEventKind.Empowered) { Callout(e.ActorSide, "EMPOWERED " + e.Amount + "%", PxGold); Feed("Glory empowers the fifth order."); }
            if (e.Kind == BattleEventKind.Glory) Feed("Glory " + ChiefKits.Letter(ChiefKits.Rank(e.Amount)) + ": " + e.Detail);
            // Historical archives do not retain MetBefore. Never borrow today's codex for an old kneel line.
            if (e.Kind == BattleEventKind.Line)
            {
                string line = _playback == null ? _fight.ChiefLine(e.Detail) :
                    e.Detail == ChiefKits.KneelLine ? "" : ChiefKits.Line(ViewSim.Kit(e.ActorSide), e.Detail, false);
                if (line.Length > 0) { Callout(e.ActorSide, line, PxGold); Feed(line); }
            }
        }

        static void ChiefGradeLines(PxLines lines, EncounterResult r)
        {
            if (r.Graded) lines.Add("GLORY GRADE " + ChiefKits.Letter(r.Grade) + (r.NewBestGrade ? " / NEW BEST" : ""), PxGold);
            if (r.ChainFound.Length > 0) lines.Add("Chain discovered: " + r.ChainFound, PxMint);
        }

        string ChiefCodexText()
        {
            var text = new StringBuilder();
            foreach (var tribe in CampaignTribes.All)
            {
                if (tribe.Kit == ChiefKit.None || !_campaign.MetChief(tribe.Id)) continue;
                text.Append(tribe.ChiefName).Append("\nBest grade: ")
                    .Append(_campaign.BestGrade(tribe.Id, out var grade) ? ChiefKits.Letter(grade) : "--")
                    .Append("\nHidden chain: ").Append(_campaign.ChiefChain(tribe.Id)).Append("\n\n");
            }
            return text.ToString();
        }
    }
}
