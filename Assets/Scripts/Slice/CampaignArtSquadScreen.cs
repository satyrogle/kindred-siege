using System;
using System.Collections.Generic;
using KindredSiege.Sim;
using KindredSiege.Sim.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace KindredSiege.Slice
{
    public sealed partial class CampaignArtLoop
    {
        const string SquadGuide = "SQUAD COMBAT\n" +
            "Blue shield: your leader is protected while an active squadmate stands. Alone, he takes damage normally.\n" +
            "Enemy closing ring: incoming attack. Gold and HOLD NOW mean a fresh timed Hold is available. The line shows the threatened ally.\n" +
            "HOLD RECOVERING means another press cannot refresh parry timing yet. Pressing again restarts that wait.\n" +
            "Ordinary attacks are automatic; only marked heavy beats can be parried. A blue cast needs Focus; Brand needs Fall Back.\n" +
            "Purple trail: the teammate who set the mark. Mint trail: the teammate who cashed it. Brackets, the pulse and move name show the confirmed combo.\n" +
            "The squad makes these combos automatically. Focus chooses the enemy to concentrate on.\n" +
            "Road: combos and parry beats, then defence, boosts, and carried chief moves. Space pauses. Hold while paused cannot parry.";
        RectTransform _squadScreen;
        RawImage _parryOuter, _parryInner, _leaderShield;
        readonly RawImage[] _comboHalos = new RawImage[3];
        readonly Image[] _comboLinks = new Image[2];
        Image _parryLink;
        PxText _parryCall, _comboCall, _squadNames, _leaderCover;
        string _squadBeatKey = "";
        int _squadBeatTotal;
        BattleEvent _comboOrigin;
        sealed class SquadMoment
        {
            public string origin, finisher, target, mark;
            public long tick;
        }
        readonly Queue<SquadMoment> _squadMoments = new Queue<SquadMoment>();
        SquadMoment _squadMoment;
        long _squadMomentStart;
        const int ComboDisplayTicks = 42;

        void ResetSquadMoments()
        {
            _squadMoments.Clear(); _squadMoment = null; _comboOrigin = default;
            _squadBeatKey = ""; _squadBeatTotal = 0;
        }

        void ReadSquadMoment(BattleEvent e)
        {
            if (e.ActorSide != 0) return;
            if (e.Kind == BattleEventKind.ComboSetup) _comboOrigin = e;
            if (e.Kind != BattleEventKind.ComboFinish || _comboOrigin.Kind != BattleEventKind.ComboSetup ||
                _comboOrigin.Amount != e.Amount || _comboOrigin.TargetId != e.TargetId || _comboOrigin.Tick != e.Tick) return;
            // Events already confirm the landed chain. No guessed readiness or invented damage.
            if (_squadMoments.Count >= 3) _squadMoments.Dequeue();
            _squadMoments.Enqueue(new SquadMoment { origin = _comboOrigin.ActorId, finisher = e.ActorId,
                target = e.TargetId, mark = e.Detail, tick = e.Tick });
        }

        void EnsureSquadScreen()
        {
            if (_squadScreen == _screen) return;
            _squadScreen = _screen;
            RawImage Ring(string name)
            {
                var r = PxImage(_readoutLayer, name, (Texture2D)_raidBeatRing.texture, 0, 0);
                r.raycastTarget = false; r.enabled = false; return r;
            }
            Image Bar(string name)
            {
                var b = Panel(_readoutLayer, 0, 0, 1, 1, PxGold);
                b.name = name; b.raycastTarget = false; b.enabled = false; return b;
            }
            _parryOuter = Ring("Incoming attack closing ring"); _parryInner = Ring("Parry timing target");
            _leaderShield = Ring("Squad protects leader"); _parryLink = Bar("Attacker to threatened ally");
            var corners = new Texture2D(32,32,TextureFormat.RGBA32,false) { filterMode=FilterMode.Point };
            var pixels = new Color32[32*32];
            for(int y=0;y<32;y++) for(int x=0;x<32;x++)
            {
                int dx=Math.Min(x,31-x),dy=Math.Min(y,31-y);
                pixels[y*32+x]=(dx<2&&dy<9)||(dy<2&&dx<9)?new Color32(255,255,255,255):new Color32(0,0,0,0);
            }
            corners.SetPixels32(pixels);corners.Apply();_screenTextures.Add(corners);
            for (int i = 0; i < 3; i++) { _comboHalos[i] = Ring("Combo participant " + i); _comboHalos[i].texture=corners; }
            for (int i = 0; i < 2; i++) _comboLinks[i] = Bar("Combo trajectory " + i);
            _parryCall = PxLabel(_readoutLayer, 0, 0, 1);
            _parryCall.root.localScale = Vector3.one * 1.5f;
            _comboCall = PxLabel(_readoutLayer, 0, 0, 1);
            _squadNames = PxLabel(_readoutLayer, 360, 62, 1);
            _leaderCover = PxLabel(_readoutLayer, 0, 0, 1);
        }

        static Vector2 Body(Figure f) => new Vector2(f.feet.x / Px, (f.feet.y - f.head * .45f) / Px);
        void Circle(RawImage ring, Figure f, float size, Color colour)
        {
            ring.enabled = f != null;
            if (f == null) return;
            var p = Body(f);
            Place(ring, Mathf.RoundToInt(p.x - size / 2), Mathf.RoundToInt(p.y - size / 2), Mathf.RoundToInt(size), Mathf.RoundToInt(size));
            ring.color = colour;
        }
        void Trace(Image line, Figure from, Figure to, float progress, Color colour, int width = 2)
        {
            line.enabled = from != null && to != null && progress > 0;
            if (!line.enabled) return;
            var a = Body(from); var b = Vector2.Lerp(a, Body(to), Mathf.Clamp01(progress));
            var rect = line.rectTransform; rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2((a.x + b.x) * Px / 2, -(a.y + b.y) * Px / 2);
            rect.sizeDelta = new Vector2(Vector2.Distance(a, b) * Px, width * Px);
            rect.localRotation = Quaternion.Euler(0, 0, -Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg);
            line.color = colour;
        }
        void AtBody(PxText text, Figure f, string words, Color colour, int dy)
        {
            if (f == null) { SetPx(text, "", colour); return; }
            var p = Body(f);
            text.root.anchoredPosition = new Vector2(Mathf.Clamp(p.x, 80, W / Px - 80) * Px, -(p.y + dy) * Px);
            SetPx(text, words, colour);
        }

        void DrawSquadScreen(BattleSimulation sim, Threat beat)
        {
            EnsureSquadScreen();
            SetPx(_clearStamp, "", PxGold);
            _raidBeatRing.enabled = false;
            bool live = !ViewOver && !sim.AwaitingVerdict && !ChiefCutInVisible;
            var actor = live && !beat.IsNone && (beat.Answers & ThreatAnswer.Parry) != 0 ? FindFigure(beat.ActorId) : null;
            var target = actor != null ? FindFigure(beat.TargetId) : null;
            string key = actor == null ? "" : beat.ActorId + "/" + beat.Name;
            if (key != _squadBeatKey) { _squadBeatKey = key; _squadBeatTotal = 0; }
            if (actor != null) _squadBeatTotal = Math.Max(_squadBeatTotal, beat.RealBeatTicks);
            bool now = actor != null && LiveParryCue(sim, beat);
            bool recovering = actor != null && sim.HoldRefreshTicksLeft(0) > 0;
            bool unavailable = actor != null && !sim.CanUse(0, Directive.Hold, out _);
            Color colour = now ? PxGold : recovering || beat.FakeFirst || beat.WaitsOutHold ? PxOrange : PxBone;
            float closing = actor == null ? 0 : Mathf.Clamp01((beat.RealBeatTicks - 16f) / Math.Max(1, _squadBeatTotal - 16));
            Circle(_parryOuter, actor, 62 + 54 * closing, colour);
            Circle(_parryInner, actor, 62, now ? PxGold : new Color(1, .83f, .45f, .4f));
            Trace(_parryLink, actor, target, 1, new Color(colour.r, colour.g, colour.b, .65f));
            string cue = _playback != null ? "REPLAY / INCOMING" : _paused ? "PAUSED" : _pilotOn ? "AUTO / INCOMING" : unavailable ? "HOLD UNAVAILABLE" : recovering ? HoldRecoveryCue(sim) :
                now ? "1 HOLD NOW" : beat.FakeFirst ? "FEINT / WAIT" : beat.WaitsOutHold ? "DELAY TRICK / DETAILS" : "WAIT FOR GOLD";
            AtBody(_parryCall, actor, cue, colour, -65);
            bool urgentBrand=false;
            foreach(var unit in sim.Units) if(unit.Side==0&&unit.Alive&&unit.BrandTicks>0) { urgentBrand=true; break; }
            bool secondaryCast=false;
            foreach(var threat in _threatReads) if(threat.CastBar) { secondaryCast=true; break; }
            if (actor != null && (now || (!CurrentThreat.CastBar && !urgentBrand)))
            {
                SetPx(_threatHeading, beat.Name.ToUpperInvariant(), colour);
                if(!secondaryCast)SetPx(_threatTarget, Fit(FighterLabel(beat.ActorId) + " TO " + FighterLabel(beat.TargetId), 566), PxBone);
                SetPx(_threatAnswers, _playback != null ? "REPLAY / RECORDED COMMANDS" : _paused ? "PAUSED / SPACE TO RESUME" :
                    _pilotOn ? "AUTO / TIMED DEFENCE" : unavailable ? "HOLD UNAVAILABLE / DETAILS" : recovering ? HoldRecoveryCue(sim) : now ? "1 HOLD NOW" : beat.FakeFirst ? "WAIT FOR THE REAL STRIKE" :
                    beat.WaitsOutHold ? "THIS MOVE WAITS OUT HOLD / OTHER ANSWERS IN DETAILS" : "CLOSING RING = INCOMING / GOLD = HOLD", colour);
            }
            else if (CurrentThreat.IsNone)
            {
                SetPx(_threatHeading, "SQUAD ATTACKING", PxBone);
                SetPx(_threatAnswers, _paused && _playback==null ? "PAUSED / SPACE TO RESUME" : "NO PARRY BEAT / 3 FOCUS AN ENEMY", PxMuted);
            }
            var leader = sim.Leader(0);
            // One blow at a time hides the ring and PROTECTED while the fight plays; a pause shows them (CampaignArtBeats.cs).
            var leaderFigure = live && !BeatsQuiet && sim.LeaderProtected(leader) ? FindFigure(leader.Id) : null;
            Circle(_leaderShield, leaderFigure, 64, new Color(.3f, .8f, 1f, .55f));
            AtBody(_leaderCover, leaderFigure, "PROTECTED", PxBlue, 36);

            if (_squadMoment != null && sim.CurrentTick - _squadMomentStart >= ComboDisplayTicks) _squadMoment = null;
            while (_squadMoment == null && _squadMoments.Count > 0)
            {
                var next = _squadMoments.Dequeue();
                if (sim.CurrentTick - next.tick > 3 * SimClock.TicksPerSecond) continue;
                _squadMoment = next; _squadMomentStart = sim.CurrentTick;
            }
            foreach (var ring in _comboHalos) ring.enabled = false;
            foreach (var line in _comboLinks) line.enabled = false;
            if (_squadMoment == null || ChiefCutInVisible)
            { SetPx(_comboCall, "", PxGold); SetPx(_squadNames, "", PxGold); return; }
            var m = _squadMoment; float age = sim.CurrentTick - _squadMomentStart;
            var origin = FindFigure(m.origin); var finish = FindFigure(m.finisher); var victim = FindFigure(m.target);
            Circle(_comboHalos[0], origin, 54, PxLilac);
            Circle(_comboHalos[1], age >= 8 ? finish : null, 62, PxMint);
            Circle(_comboHalos[2], victim, age >= 18 ? 45 + 16 * Mathf.Sin((age - 18) / 24 * Mathf.PI) : 40, age >= 18 ? PxMint : PxLilac);
            Trace(_comboLinks[0], origin, victim, Mathf.Clamp01(age / 8), new Color(.7f, .45f, 1f, .6f));
            Trace(_comboLinks[1], finish, victim, Mathf.Clamp01((age - 8) / 10), PxMint, 3);
            string move = m.mark == ClassSpells.SnareMark ? "PIN SHOT" : m.mark == ClassSpells.OpeningMark ? "OPENING STRIKE" : "BLEED RIP";
            AtBody(_comboCall, victim, age >= 18 ? move : "", PxMint, 38);
            // The road has a free caption row; chief/beast fights use it for nerve/posture bars.
            SetPx(_squadNames, sim.UnitNerveOn ? Fit(SquadName(m.origin) + " + " + SquadName(m.finisher) + " / " + move, 480) : "", PxMint);
        }
        string SquadName(string id)
        {
            string name=NameOf(id);int at=name.IndexOf(" / ",StringComparison.Ordinal);
            return at<0?name:name.Substring(0,at);
        }
    }
}
