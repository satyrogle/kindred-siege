using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using KindredSiege.Sim;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using UnityEngine;
using UnityEngine.UI;

namespace KindredSiege.Slice
{
    public sealed partial class CampaignArtLoop
    {
        // Jacob's "fix all 9" (13 Oct 2026, after Claude played four fights and reviewed them: "am i winning how? i
        // lost why how?"). The fight now shows who is winning and what just swung it, marks both leaders, times
        // their cast down to the moment a Hold parries it, says when their shields will stop an Unleash, tips a
        // fallen token over, and the why box and the result name the moments that decided the fight. Plain kit
        // pieces, and interim: Codex's cult HUD may restyle or replace everything in this file. Every number comes
        // from the sim; nothing here changes a rule. After Claude's playtest for Jacob (14 Oct 2026, his "go"): the
        // why box also says what wore away a little at a time, its tips give advice, tags take two lines, and your
        // fighters' lessons read from your side.

        // A drop of this much nerve at one moment is a swing worth naming: 5% of a full bar. Starting values, like
        // the bar sizes, how long the words stay up, the slow loss worth a line (15%) and a tag line's width.
        const int SwingPermille = 50, WornShown = 150;
        const int NerveBarW = 300, NerveBarY = 42, NerveBarH = 5, CastBarW = 60, CastBarH = 4, TagLineW = 84;
        const float SwingShown = 1.6f, CauseWindow = .5f, TeachShown = 5f;
        RectTransform _readoutScreen, _readoutLayer;
        readonly Image[] _nerveFill = new Image[2], _nerveLag = new Image[2], _nerveBack = new Image[2];
        readonly PxText[] _nerveLabel = new PxText[2];
        readonly int[] _nerveSeen = new int[2], _nerveBarX = new int[2], _causeRank = new int[2];
        readonly float[] _nerveLagValue = new float[2], _nerveLagUntil = new float[2], _swingAt = { -100, -100 }, _causeAt = { -100, -100 };
        readonly string[] _swingText = { "", "" }, _cause = { "", "" };
        float _nerveDrawnAt;
        Image _castBack, _castFill, _castMark;
        PxText _castLabel;
        int _castTotal;
        string _castCaster = "";
        readonly Dictionary<string, PxText> _leaderMarks = new Dictionary<string, PxText>();
        bool _theyHoldOnUnleash, _warnedLeaderHurt, _leaderTaught;
        string _teach = "", _teach2 = "";
        Color32 _teachColour;
        float _teachUntil = -100;
        SwingReport _swings;
        CampaignFight _swingsFight;
        long _swingsTick = -1;

        /// Built with each fight screen, on first use: the strip under the top bar with both nerve bars, and the
        /// pieces of the cast timer. Leader marks come as each leader is drawn.
        void EnsureReadouts(BattleSimulation sim)
        {
            if (_readoutScreen == _screen) return;
            _readoutScreen = _screen;
            _readoutLayer = Rect(_stage, "Readouts", 0, 0, W, H);
            var hud=sim.PitOn?_screen:_readoutLayer;
            Panel(hud, StageX * Px, TopBarH * Px, StageW * Px, (CleanCombatHud?38:19) * Px, new Color(.06f, .05f, .07f, CleanCombatHud?.92f:.72f));
            for (int s = 0; s < 2; s++)
            {
                int x = s == 0 ? StageX + 6 : StageX + StageW - 6 - NerveBarW;
                _nerveBarX[s] = x;
                _nerveBack[s] = Panel(hud, x * Px, NerveY * Px, NerveBarW * Px, NerveHeight * Px, BarBack);
                _nerveLag[s] = Panel(hud, x * Px, NerveY * Px, NerveBarW * Px, NerveHeight * Px, PxBone);
                _nerveFill[s] = Panel(hud, x * Px, NerveY * Px, NerveBarW * Px, NerveHeight * Px, s == 0 ? PxMint : PxRed);
                _nerveLabel[s] = PxLabel(hud, s == 0 ? x : x + NerveBarW, CleanCombatHud?43:NerveBarY + NerveBarH + 2, s == 0 ? 0 : 2);
                if(CleanCombatHud)_nerveChange[s]=PxLabel(hud,s==0?x:x+NerveBarW,69,s==0?0:2);
                _nerveSeen[s] = sim.Nerve(s);
                _nerveLagValue[s] = sim.Nerve(s);
                _nerveLagUntil[s] = _swingAt[s] = _causeAt[s] = -100;
                _swingText[s] = _cause[s] = "";
                _causeRank[s] = 0;
                _cleanNerveCauseTick[s]=-1;
                // The lanes' words move under the bars.
                if (_pxLane[s] != null) { _pxLane[s].y = CleanCombatHud?159:TopBarH + 22; _pxLane[s].key = ""; }
            }
            _nerveDrawnAt = _visualTime;
            if(RoadHud){_roadWarning=PxLabel(hud,30,56);SetPx(_roadWarning,sim.ClearRoadOn?"":sim.RoadKitOn?KitStatus:RoadIssue,PxOrange);}
            _castBack = Panel(hud, 0, 0, CastBarW * Px, CastBarH * Px, BarBack);
            _castFill = Panel(hud, 0, 0, CastBarW * Px, CastBarH * Px, PxYellow);
            _castMark = Panel(hud, 0, 0, Px, (CastBarH + 4) * Px, PxGold);
            _castLabel = PxLabel(hud, 0, 0, 1);
            _castBack.enabled = _castFill.enabled = _castMark.enabled = false;
            _castTotal = 0;
            _castCaster = "";
            EnsureHeavyReadouts();
            _leaderMarks.Clear();
            _theyHoldOnUnleash = (_playback != null ? _playback.Fight.Setup : _fight.Setup).CopyTactics(1).HoldOnUnleash >= BattleSimulation.HabitThreshold;
            _warnedLeaderHurt = false;
            _teachUntil = -100;
            // Once a session: the rule the marks stand for.
            if (!_leaderTaught && !sim.UnitNerveOn && !sim.BeastOn && sim.Leader(1) != null)
            {
                _leaderTaught = true;
                Teach("Kill their LEADER and they break.", "If YOUR LEADER falls, you lose.", PxGold);
            }
        }

        /// Every frame of a live fight, after the rest of RefreshFight.
        void RefreshReadouts(BattleSimulation sim)
        {
            if (_playback != null || _fight == null) return;
            EnsureReadouts(sim);
            DrawNerve(sim);
            DrawLeaderMarks(sim);
            if(!CleanCombatHud){DrawCastTimer(sim);DrawHeavyTarget(sim);}
            DrawFallenTokens();
        }

        // ------------------------------------------------------------ nerve

        /// Two bars under the top bar, yours filling from the left and theirs from the right. A big drop leaves its
        /// chunk showing for a moment and names what did it beside the number.
        void DrawNerve(BattleSimulation sim)
        {
            if(RoadHud){DrawRoadCounts(sim);return;}
            float dt = Mathf.Max(0f, _visualTime - _nerveDrawnAt);
            _nerveDrawnAt = _visualTime;
            for (int s = 0; s < 2; s++)
            {
                if(sim.Beast(s)!=BeastKit.None){_nerveFill[s].enabled=_nerveLag[s].enabled=false;SetPx(_nerveLabel[s],"NO NERVE / BREAK PARTS",PxGold);continue;}
                int now = sim.Nerve(s), drop = _nerveSeen[s] - now;
                if (drop >= SwingPermille || (CleanCombatHud&&drop>0&&_causeRank[s]>=4&&_cleanNerveCauseTick[s]==sim.CurrentTick))
                {
                    string cause = (CleanCombatHud?_cleanNerveCauseTick[s]==sim.CurrentTick:_visualTime - _causeAt[s] <= CauseWindow) ? _cause[s] : "";
                    _swingText[s] = "-" + Percent(drop) + (cause.Length > 0 ? " " + cause : "");
                    _swingAt[s] = Time.unscaledTime;
                    _nerveLagUntil[s] = _visualTime + .8f;
                }
                _nerveSeen[s] = now;
                // The bone chip holds a moment on the fight's clock, then drains, like the fighters' bars.
                if (_nerveLagValue[s] < now) _nerveLagValue[s] = now;
                else if (_visualTime >= _nerveLagUntil[s]) _nerveLagValue[s] = Mathf.MoveTowards(_nerveLagValue[s], now, 500f * dt);
                PlaceBar(_nerveFill[s], s, Mathf.RoundToInt(NerveBarW * Mathf.Clamp01(now / (float)NerveRules.Full)));
                PlaceBar(_nerveLag[s], s, Mathf.RoundToInt(NerveBarW * Mathf.Clamp01(_nerveLagValue[s] / NerveRules.Full)));
                string words = (s == 0 ? "YOUR NERVE " : "THEIR NERVE ") + Percent(now);
                Color32 own = s == 0 ? PxMint : PxRed, hurt = s == 0 ? PxRed : PxGold;
                // Shown for real seconds, so it can be read at 4X too.
                bool swing = _swingText[s].Length > 0 && Time.unscaledTime - _swingAt[s] < SwingShown;
                if(CleanCombatHud)CleanNerveLabel(sim,s,swing);
                else if (!swing) SetPx(_nerveLabel[s], words, own);
                else if (s == 0) SetPx(_nerveLabel[s], new[] { words, _swingText[s] }, new[] { own, hurt });
                else SetPx(_nerveLabel[s], new[] { _swingText[s], words }, new[] { hurt, own });
            }
        }

        // Whole percent, rounded down like the top bar's.
        static string Percent(int permille) => permille / 10 + "%";

        void PlaceBar(Image bar, int side, int width)
        {
            Place(bar, side == 0 ? _nerveBarX[side] : _nerveBarX[side] + NerveBarW - width, NerveY, width, NerveHeight);
        }

        static void Place(Graphic image, int x, int y, int w, int h)
        {
            image.rectTransform.anchoredPosition = new Vector2(x * Px, -y * Px);
            image.rectTransform.sizeDelta = new Vector2(w * Px, h * Px);
        }

        /// From WhyEvent, for every event as it is drawn: what hit a side's nerve just now, and the words for an
        /// Unleash their shields stopped.
        void ReadoutEvent(BattleEvent e)
        {
            if (_playback != null || _fight == null || e.ActorSide < 0 || e.ActorSide > 1) return;
            var sim = ViewSim;
            int other = 1 - e.ActorSide;
            switch (e.Kind)
            {
                case BattleEventKind.Reaction:
                    if(CleanCombatHud&&e.Detail==ParryRules.BlockDetail)Cause(e.ActorSide,"AFTER BLOCK",6);
                    break;
                case BattleEventKind.Parry:
                    if(CleanCombatHud)Cause(other,"PARRIED",6);
                    break;
                case BattleEventKind.Wall:
                    if (e.Detail == ChiefSpells.WallSmashed) Cause(e.ActorSide, "WALL SMASHED", 4);
                    break;
                case BattleEventKind.Break:
                    if (e.Detail == Moves.LeaderDetail) Cause(e.ActorSide, "LEADER FELL", 6);
                    break;
                case BattleEventKind.Kill:
                    {
                        var u = sim.FindById(e.TargetId);
                        if (u != null) Cause(u.Side, (u.Side == 1 ? "KILLED " : "LOST ") + RoleName(u, ViewKind).ToUpperInvariant(), 5);
                        break;
                    }
                case BattleEventKind.Release:
                    if (e.Detail == Moves.Full) Cause(other, (e.ActorSide == 1 ? "THEIR " : "YOUR ")+CastName(e).ToUpperInvariant(), 4);
                    else if (e.Detail == Moves.Soaked) Cause(other, (e.ActorSide == 1 ? "THEIR " : "YOUR ")+CastName(e).ToUpperInvariant()+", HALVED", 4);
                    break;
                case BattleEventKind.Combo:
                    Cause(other, e.Detail.ToUpperInvariant(), 3);
                    break;
                case BattleEventKind.Directive:
                    if (e.Detail == BattleSimulation.BlockedDetail && e.ActorSide == 1)
                    {
                        if (RecentWallSmash(1))
                        {
                            Callout(0, "WALL SMASHED", PxOrange);
                            Teach("Steel Storm smashed. Their Hold remains.", "The wall's nerve loss landed; their Hold blocked Unleash's separate pressure.", PxOrange);
                            break;
                        }
                        Callout(0, "SCARE BLOCKED", PxOrange);
                        Teach("UNLEASH +"+(BattleSimulation.UnleashDamagePercent-100)+"% DAMAGE", "SCARE BLOCKED", PxOrange);
                    }
                    else if (e.Detail == Directives.Name(Directive.Unleash)) Cause(other, e.ActorSide == 0 ? "YOUR UNLEASH" : "THEIR UNLEASH", 2);
                    break;
            }
        }

        /// The higher ranked cause wins when two land together.
        void Cause(int side, string words, int rank)
        {
            if (side < 0 || side > 1) return;
            if (_visualTime - _causeAt[side] <= CauseWindow && rank < _causeRank[side]) return;
            _cause[side] = words;
            if(CleanCombatHud)_cleanNerveCauseTick[side]=ViewSim.CurrentTick;
            _causeRank[side] = rank;
            _causeAt[side] = _visualTime;
        }

        // ------------------------------------------------------------ leaders, fallen tokens, casts and tags

        /// A mark over each leader while it stands: kill theirs and they break, lose yours and you lose. One warning
        /// when yours is badly hurt.
        void DrawLeaderMarks(BattleSimulation sim)
        {
            foreach (var f in _figures)
            {
                if (!f.unit.IsLeader) continue;
                if (!_leaderMarks.TryGetValue(f.unit.Id, out PxText mark))
                {
                    int y = Mathf.Max(StageY + 41, Mathf.RoundToInt((f.feet.y - f.head) / Px) - 13);
                    mark = PxLabel(_readoutLayer, Mathf.RoundToInt(f.feet.x / Px), y, 1);
                    _leaderMarks[f.unit.Id] = mark;
                }
                bool mine = f.unit.Side == 0;
                // One blow at a time hides the tag while the fight plays; a pause shows it (CampaignArtBeats.cs, fix 3).
                SetPx(mark, f.unit.Alive && !BeatsQuiet ? (mine ? "YOUR LEADER" : ViewKind==EncounterKind.Duel?"CHAMPION":"LEADER") : "", mine ? PxMint : PxGold);
            }
            var leader = sim.Leader(0);
            if (!sim.UnitNerveOn && !_warnedLeaderHurt && leader != null && leader.Hp * 100 < leader.MaxHp * 40)
            {
                _warnedLeaderHurt = true;
                Callout(0, "YOUR LEADER IS HURT", PxRed);
                Teach("Your leader is hurt.", "If he falls, you lose the fight.", PxRed);
            }
        }

        /// A placeholder token has no death clip, so a fallen one tips over and darkens. Live fights and replays.
        void DrawFallenTokens()
        {
            foreach (var f in _figures)
            {
                if (f.body != null || f.unit.Alive || f.unit.Benched || f.root.localEulerAngles.z != 0f) continue;
                f.root.localRotation = Quaternion.Euler(0, 0, f.unit.Side == 0 ? 90 : -90);
                foreach (var g in f.root.GetComponentsInChildren<Graphic>(true))
                    g.color = new Color(g.color.r * .5f, g.color.g * .5f, g.color.b * .5f, g.color.a * .8f);
            }
        }

        /// While their cast winds up: a bar over the caster that runs down past a gold mark, the moment from which a
        /// Hold parries it (FightRules.PerfectHoldTicks), unless this caster's side has learned to wait it out.
        void DrawCastTimer(BattleSimulation sim)
        {
            bool heavy = sim.HeavyTicks(1) > 0;
            int left = heavy ? sim.HeavyTicks(1) : sim.WindupTicks(1);
            string actor = heavy ? sim.HeavySwingerId(1) : sim.WindupCasterId(1);
            Figure f = left > 0 && !ViewOver ? FindFigure(actor) : null;
            if (f == null)
            {
                _castBack.enabled = _castFill.enabled = _castMark.enabled = false;
                SetPx(_castLabel, "", PxYellow);
                _castTotal = 0;
                _castCaster = "";
                return;
            }
            string tellKey = f.unit.Id + (heavy ? ":heavy" : ":cast");
            if (tellKey != _castCaster || left > _castTotal) { _castCaster = tellKey; _castTotal = heavy ? Mathf.Max(HeavyRules.WindupTicks,left) : left; }
            int x = Mathf.RoundToInt(f.feet.x / Px) - CastBarW / 2;
            int y = Mathf.Max(StageY + 52, Mathf.RoundToInt((f.feet.y - f.head) / Px) - 24);
            if (ChiefCutInVisible) y = 117;
            // The priority card already owns this area. Do not draw a second timer through its text.
            int cardTop=sim.BeastOn?161:102;
            if(y>=cardTop-3 && y<cardTop+83)
            { _castBack.enabled=_castFill.enabled=_castMark.enabled=false;SetPx(_castLabel,"",PxYellow);return; }
            bool waitsOut = !sim.ParryCenterOn && sim.WaitsOutParry(1) && !sim.HeavyOn;
            bool parry = sim.ParryCenterOn || (heavy ? sim.HeavyParryable(1) : sim.SpellAnswer(1) == Directive.Hold && !waitsOut);
            bool pausedOrder = _playback == null && _paused && (sim.ParryCenterOn || sim.HeavyOn);
            Directive answer = heavy ? Directive.Hold : sim.SpellAnswer(1);
            bool available = sim.CanUse(0,parry ? Directive.Hold : answer,out _);
            bool observing = _playback != null || _pilotOn;
            bool now = parry && !pausedOrder && !observing && available && sim.HoldRefreshTicksLeft(0) == 0 && left <= FightRules.PerfectHoldTicks;
            var timedThreat = sim.ParryCenterOn ? TimerParryThreat(sim, actor, heavy) : Threat.None;
            if (sim.ParryCenterOn) now &= LiveParryCue(sim, timedThreat);
            float total = Mathf.Max(1, _castTotal);
            Place(_castBack, x, y, CastBarW, CastBarH);
            Place(_castFill, x, y, Mathf.Max(1, Mathf.RoundToInt(CastBarW * left / total)), CastBarH);
            Place(_castMark, x + Mathf.RoundToInt(CastBarW * FightRules.PerfectHoldTicks / total), y - 2, 1, CastBarH + 4);
            // The mark is a dark notch in the full bar, then gold on the emptied bar once the moment comes.
            _castFill.color = now ? (Color)PxGold : (Color)PxYellow;
            _castMark.color = now ? (Color)PxGold : BarBack;
            _castBack.enabled = _castFill.enabled = true;
            _castMark.enabled = parry;
            int lx = x + CastBarW / 2, ly = y - 11;
            if (_castLabel.x != lx || _castLabel.y != ly) { _castLabel.x = lx; _castLabel.y = ly; _castLabel.key = ""; }
            string label = pausedOrder && parry ? "RESUME TO PARRY" : now ? "HOLD NOW!" : heavy ? "HEAVY " + Seconds(left) : waitsOut ? "HOLD WON'T PARRY" : !parry ? OrderName(sim.SpellAnswer(1)).ToUpperInvariant()+" / "+Seconds(left) : "CAST " + Seconds(left);
            if (parry && !pausedOrder && !available) label = "HOLD UNAVAILABLE";
            else if (parry && !pausedOrder && sim.HoldRefreshTicksLeft(0) > 0) label = HoldRecoveryCue(sim);
            else if (parry && !pausedOrder && timedThreat.FakeFirst) label = "FAKE FIRST / WAIT";
            else if (parry && !pausedOrder && timedThreat.WaitsOutHold) label = "HOLD WON'T PARRY";
            if (heavy && !parry) label = "CANNOT PARRY / " + Seconds(left);
            if (!heavy && sim.Spell(1) == ChiefSpell.SteelStorm) label = "WALL IN " + Seconds(left);
            else if (!heavy && !parry && !available) label = OrderName(answer).ToUpperInvariant() + " UNAVAILABLE";
            if (observing && parry) label = _playback != null ? "REPLAY / RECORDED HOLD" : "AUTO / TIMED DEFENCE";
            SetPx(_castLabel, label, heavy && !parry ? PxRed : now ? PxGold : heavy ? PxYellow : OrderColour(answer));
        }

        /// A fighter's tags, TagLineW pixels a line at most and the rest on a second line under it (a "\n" part,
        /// SetPx), so the tags of fighters side by side never run into each other (Claude's playtest, 14 Oct 2026:
        /// PANIC ran into COVERED BY WARDEN). Null clears them. Live fights and replays.
        void DrawTags(Figure f, List<string> texts, List<Color32> colours)
        {
            var parts = new List<string>(); var partColours = new List<Color32>();
            int width = 0;
            bool broken = false;
            for (int i = 0; texts != null && i < texts.Count; i++)
            {
                int w = _tinyFont.Measure(texts[i].ToUpperInvariant(), 1);
                if (!broken && parts.Count > 0 && width + 6 + w > TagLineW) { parts.Add("\n"); partColours.Add(PxBone); broken = true; }
                if (!broken) width += (parts.Count > 0 ? 6 : 0) + w;
                parts.Add(texts[i]); partColours.Add(colours[i]);
            }
            if((_fight!=null||_playback!=null)&&ViewSim.ClearRoadOn) SetPx(f.tag,"",PxBone);
            else SetPx(f.tag, parts.ToArray(), partColours.ToArray());
        }

        // ------------------------------------------------------------ words

        /// The Unleash plate's words when a plain Unleash will not scare them: their shields are up, or they learned
        /// to raise them the moment you Unleash and have the point to (BattleSimulation.ReflexHoldOnUnleash). Empty
        /// otherwise.
        string UnleashBlockedText(BattleSimulation sim)
        {
            if (_playback != null || _fight == null) return "";
            EnsureReadouts(sim);
            string benefit="+"+(BattleSimulation.UnleashDamagePercent-100)+"% DMG";
            if (sim.HoldTicksLeft(1) > 0) return benefit+" / SCARE BLOCKED";
            if (_theyHoldOnUnleash && sim.DirectivePoints(1) >= Directives.Cost(Directive.Hold)) return benefit+" / SCARE GUARDED";
            return "";
        }

        void Teach(string first, string second, Color32 colour)
        {
            _teach = first;
            _teach2 = second;
            _teachColour = colour;
            _teachUntil = Time.unscaledTime + TeachShown;
        }

        /// The banner's two lines for a few seconds: what just happened and what to do about it. Leaves them alone
        /// when there is nothing to teach.
        bool ReadoutTeach(ref string first, ref string second, ref Color32 colour)
        {
            if (_playback != null || _teach.Length == 0 || Time.unscaledTime >= _teachUntil) return false;
            first = _teach;
            second = _teach2;
            colour = _teachColour;
            return true;
        }

        /// A fighter by what it is in this fight: Leader, Chief, Shaman, Champion, Captain, or its class.
        static string RoleName(BattleUnit u, EncounterKind kind)
        {
            if(kind==EncounterKind.Beast&&u.Side==1){string tail=u.Id.Substring(u.Id.LastIndexOf('.')+1);if(BeastKits.TryParse(tail,out var part))return BeastKits.Name(part);}
            if (u.Id == Campaign.LeaderId) return "Leader";
            if (u.Id.EndsWith(".chief", StringComparison.Ordinal)) return "Chief";
            if (u.Id.EndsWith(".shaman", StringComparison.Ordinal)) return "Shaman";
            if (u.Id.EndsWith(".champion", StringComparison.Ordinal)) return "Champion";
            if (HunterFight(kind) && u.Side == 1) return u.IsLeader ? "Hunter" : "Hunter " + SliceClasses.Name(u.Class);
            return SliceClasses.Name(u.Class);
        }

        /// A lesson your fighters learned, told from your side. Every lesson speaks to the other side ("your Warden
        /// covered..."), which on your own panel read as yours (Claude's playtest, 14 Oct 2026).
        static string TheirVoice(string text)
        {
            text = Regex.Replace(text, @"\bYour\b", "Their");
            text = Regex.Replace(text, @"\byour\b", "their");
            text = Regex.Replace(text, @"\bYou\b", "They");
            return Regex.Replace(text, @"\byou\b", "they");
        }

        // ------------------------------------------------------------ the swings that decided a fight

        sealed class Swing
        {
            public long Tick;
            public int Side, Before, After;
            public string Kind = "", Cause = "";
            public int Size => Before - After;
        }

        /// A fight's swings, each side's nerve at the start and the end, and what each side lost a little at a time:
        /// its whole loss less its swings.
        sealed class SwingReport
        {
            public readonly List<Swing> Swings = new List<Swing>();
            public readonly int[] Start = new int[2], End = new int[2], Worn = new int[2];
            public FightEnding Ending;
        }

        /// Plays the fight again from its setup and orders, exactly as ReplayRunner does, and keeps every moment a
        /// side lost SwingPermille or more of its nerve, with what did it.
        static SwingReport NerveSwings(BattleSetup setup, SimCommandLog log, long ticks, EncounterKind kind)
        {
            var report = new SwingReport();
            var sim = new BattleSimulation(setup);
            report.Start[0] = sim.Nerve(0);
            report.Start[1] = sim.Nerve(1);
            var commands = log.Commands;
            var happened = new List<BattleEvent>();
            int ci = 0, ei = 0;
            for (long t = 0; ; t++)
            {
                int before0 = sim.Nerve(0), before1 = sim.Nerve(1);
                while (ci < commands.Count && commands[ci].Tick <= sim.CurrentTick)
                {
                    sim.TryApply(commands[ci], out _);
                    ci++;
                }
                bool more = t < ticks && !sim.IsFinished;
                if (more) sim.Tick();
                happened.Clear();
                for (; ei < sim.Events.Count; ei++) happened.Add(sim.Events[ei]);
                AddSwing(report.Swings, sim, happened, 0, before0, kind);
                AddSwing(report.Swings, sim, happened, 1, before1, kind);
                if (!more) break;
            }
            report.Ending = sim.Ending;
            for (int s = 0; s < 2; s++)
            {
                report.End[s] = sim.Nerve(s);
                int worn = report.Start[s] - report.End[s];
                foreach (var w in report.Swings) if (w.Side == s) worn -= w.Size;
                report.Worn[s] = Math.Max(0, worn);
            }
            return report;
        }

        static void AddSwing(List<Swing> swings, BattleSimulation sim, List<BattleEvent> happened, int side, int before, EncounterKind kind)
        {
            int after = sim.Nerve(side);
            if (before - after < SwingPermille) return;
            var swing = new Swing { Tick = sim.CurrentTick, Side = side, Before = before, After = after };
            Classify(sim, happened, swing, kind);
            swings.Add(swing);
        }

        /// What took the nerve, most telling first: a fallen leader, a kill, a cast, a combo, an Unleash, then hits.
        static void Classify(BattleSimulation sim, List<BattleEvent> happened, Swing swing, EncounterKind kind)
        {
            int side = swing.Side, other = 1 - side;
            string whose = side == 0 ? "your" : "their", others = side == 0 ? "their" : "your", them = side == 0 ? "you" : "them";
            foreach (var e in happened)
                if (e.Kind == BattleEventKind.Break && e.ActorSide == side && e.Detail == Moves.LeaderDetail)
                { Name(swing, e, "leader", whose + " leader fell"); return; }
            foreach (var e in happened)
            {
                if (e.Kind != BattleEventKind.Kill) continue;
                var victim = sim.FindById(e.TargetId);
                if (victim == null || victim.Side != side) continue;
                var killer = sim.FindById(e.ActorId);
                Name(swing, e, "kill", others + " " + (killer != null ? RoleName(killer, kind) : "fighters") + " killed " + whose + " " + RoleName(victim, kind));
                return;
            }
            foreach (var e in happened)
                if (e.Kind == BattleEventKind.Release && e.ActorSide == other && (e.Detail == Moves.Full || e.Detail == Moves.Soaked))
                {
                    bool full = e.Detail == Moves.Full;
                    bool heavy=!string.IsNullOrEmpty(e.TargetId);
                    string move=heavy?"heavy swing":"cast";
                    if(!heavy)
                        for(int i=sim.Events.Count-1;i>=0;i--)
                        {
                            var tell=sim.Events[i];
                            if(tell.Tick<=e.Tick&&tell.Kind==BattleEventKind.Windup&&tell.ActorId==e.ActorId&&tell.Detail!=HeavyRules.Heavy)
                            {move=tell.Detail;break;}
                        }
                    Name(swing, e, heavy?"heavy":full ? "cast" : "halved", others + " "+move+" hit " + them + (full ? " in full" : ", halved by " + whose + " Hold"));
                    return;
                }
            foreach (var e in happened)
                if (e.Kind == BattleEventKind.Combo && e.ActorSide == other)
                { Name(swing, e, "combo", others + " " + e.Detail); return; }
            foreach (var e in happened)
                if (e.Kind == BattleEventKind.Directive && e.ActorSide == other && e.Detail == Directives.Name(Directive.Unleash))
                { Name(swing, e, "unleash", others + " Unleash scared " + them); return; }
            foreach (var e in happened)
                if (e.Kind == BattleEventKind.Hit && e.ActorSide == other)
                { Name(swing, e, "hits", "hits from " + others + " fighters"); return; }
            swing.Kind = "pressure";
            swing.Cause = whose + " nerve wore down";
        }

        static void Name(Swing swing, BattleEvent e, string kind, string cause)
        {
            swing.Kind = kind;
            swing.Cause = cause;
            swing.Tick = e.Tick;
        }

        /// The live fight's swings, worked out once when they are asked for. Kept by the fight itself, not its id:
        /// ids repeat from one campaign to the next.
        SwingReport FightSwings()
        {
            if (_fight == null) return null;
            if (!ReferenceEquals(_swingsFight, _fight) || _swingsTick != _fight.Sim.CurrentTick)
            {
                _swings = NerveSwings(_fight.Setup, _fight.Session.Log.CloneForReplay(), _fight.Sim.CurrentTick, _fight.Encounter.Kind);
                _swingsFight = _fight;
                _swingsTick = _fight.Sim.CurrentTick;
            }
            return _swings;
        }

        static string SwingLine(Swing s)
        {
            return Seconds(s.Tick) + "  " + char.ToUpperInvariant(s.Cause[0]) + s.Cause.Substring(1) + ": " + (s.Side == 0 ? "your" : "their") +
                " nerve " + Percent(s.Before) + " to " + Percent(s.After) + ".";
        }

        static string WornLine(int side, int worn)
        {
            return "Another " + Percent(worn) + " of " + (side == 0 ? "your" : "their") + " nerve wore away a little at a time.";
        }

        /// What decided it: the biggest swing, or the slow loss when that took more.
        static string Deciding(List<Swing> against, int worn)
        {
            int biggest = 0;
            string kind = "";
            foreach (var s in against) if (s.Size > biggest) { biggest = s.Size; kind = s.Kind; }
            return worn > biggest ? "hits" : kind;
        }

        /// What to do about what decided the fight, in the rules' own terms.
        static string Tip(string kind, bool won, bool noFallBack, bool waitsOutParry, bool parryCenter = false, bool squadFirst = false, bool danger = false)
        {
            if(danger&&(kind=="cast"||kind=="halved"||kind=="heavy"))
                return "Hold guards swings. Blasts need a timed live parry"+(noFallBack?".":" or Fall Back; dodging stops your attacks.")+" Early or paused Hold does not soften a blast.";
            if(squadFirst && (kind=="cast"||kind=="halved"||kind=="heavy"))
                return "Your squad can build openings. Hold guards the front row; Focus and Unleash can cash marks. Watch big beats and their available answers.";
            if(parryCenter && (kind=="cast"||kind=="halved"||kind=="heavy"))
                return "Time a fresh live Hold in the last half second of the beat. Early Hold soaks but costs nerve; paused Hold never parries." + (waitsOutParry ? " They may delay once: follow the real beat." : "");
            string hold = (100 - BattleSimulation.HoldDamagePercent) + "%";
            if (!won && waitsOutParry && (kind == "cast" || kind == "halved"))
                return "They learned to wait out your Hold: it halves their cast, but cannot parry it. Try interrupting an uncovered caster" +
                    (noFallBack ? "." : ", or Fall Back to dodge.");
            switch (kind)
            {
                case "leader": return won ? "Their leader is the key: kill it and they break at once." : "When your leader falls you lose at once. Watch the YOUR LEADER mark.";
                case "kill": return won ? "Each kill breaks their nerve: Focus Fire one target until it falls." : "Each fighter you lose breaks your nerve: Hold so your front line takes " + hold + " less while it is under pressure.";
                case "heavy": return won ? "Your heavy swing broke their nerve." : "Watch the heavy timer. Time Hold only when it shows a parry window; some heavy attacks cannot be parried.";
                case "cast": return won ? "Your spell hit their front line in full: cast it while their shields are down." : "Answer their cast: Hold in its last half second to parry it, or Hold earlier to halve it" + (noFallBack ? "." : ", or Fall Back to dodge it.");
                case "halved": return won ? "Their Hold halved your spell: cast it when their shields are down." : "Your Hold halved their cast. Hold in its last half second and you parry it instead.";
                case "combo": return won ? "Combos hit nerve hard: parry their cast, then Unleash for a Riposte." : "Their combo hit your nerve hard.";
                case "unleash": return won ? "Unleash scares them when their shields are down." : "When they Unleash, Hold: raised shields stop the scare.";
                case "hits": return won ? "Hits wear nerve slowly. Kills, parries and combos break it fast: Focus Fire one target until it falls." : "Their hits wore you down: Hold so your front line takes " + hold + " less, at a cost to your nerve.";
                default: return won ? "No single moment broke them: kills, parries and combos end a fight sooner." : "Your nerve wore away: kills, parries and combos end a fight before that happens.";
            }
        }

        /// For WhyLines: the biggest swings against the side that lost, in order, then what wore away a little at a
        /// time, then what to do about whichever took more. False when no single moment swung it, and the totals stay.
        bool WhySwingLines(PxLines lines, bool won)
        {
            var report = FightSwings();
            if (report == null) return false;
            int loser = won ? 1 : 0;
            var against = report.Swings.FindAll(s => s.Side == loser);
            if (against.Count == 0) return false;
            string deciding = Deciding(against, report.Worn[loser]);
            against.Sort((a, b) => b.Size.CompareTo(a.Size));
            if (against.Count > 3) against.RemoveRange(3, against.Count - 3);
            against.Sort((a, b) => a.Tick.CompareTo(b.Tick));
            foreach (var s in against) lines.Add(SwingLine(s), won ? PxMint : PxRed);
            if (report.Worn[loser] >= WornShown) lines.Add(WornLine(loser, report.Worn[loser]), won ? PxMint : PxRed);
            lines.Add(Tip(deciding, won, _fight.Encounter.NoFallBack, _fight.Sim.WaitsOutParry(1), _fight.Sim.ParryCenterOn, _fight.Sim.SquadFirstOn, _fight.Sim.DangerOn), PxGold);
            return true;
        }

        /// The result page's WHY: how the fight ended, every big swing on both sides, what wore away a little at a
        /// time, from the archived fight.
        void ShowFightWhy(EncounterResult r)
        {
            if(_campaign.Archive.TryReplay(r.Record.FightId,out var roadReplay,out string roadFailure)&&roadReplay.UnitNerveOn)
            {CloseModal();Modal("WHY",string.Join("\n",RoadSummary(roadReplay).texts));return;}
            var archived = _campaign.Archive.Find(r.Record.FightId);
            CloseModal();
            Modal("WHY", "");
            var lines = new PxLines();
            if (archived == null) lines.Add("This fight's history is not kept.", PxMuted);
            else
            {
                var report = NerveSwings(archived.Setup, archived.Log.CloneForReplay(), archived.Ticks, r.Encounter.Kind);
                lines.Add(r.Encounter.Title, PxGold).Add(Ending(report.Ending, r.Encounter.Kind, r.Won), PxBone).Gap();
                if (roadReplay != null) ChiefOutcomeLines(lines, roadReplay, r.Won);
                if (report.Swings.Count == 0) lines.Add("No single moment swung it: nerve wore down a little at a time.", PxMuted);
                var shown = new List<Swing>(report.Swings);
                shown.Sort((a, b) => b.Size.CompareTo(a.Size));
                if (shown.Count > 16) shown.RemoveRange(16, shown.Count - 16);
                shown.Sort((a, b) => a.Tick.CompareTo(b.Tick));
                foreach (var s in shown) lines.Add(SwingLine(s), s.Side == 0 ? PxRed : PxMint);
                for (int s = 1; s >= 0; s--)
                    if (report.Worn[s] >= WornShown) lines.Add(WornLine(s, report.Worn[s]), s == 0 ? PxRed : PxMint);
                int loser = r.Won ? 1 : 0;
                var against = report.Swings.FindAll(s => s.Side == loser);
                if (against.Count > 0 || report.Worn[loser] >= WornShown)
                    lines.Gap().Add(Tip(Deciding(against, report.Worn[loser]), r.Won, r.Encounter.NoFallBack,
                        new BattleSimulation(archived.Setup).WaitsOutParry(1), new BattleSimulation(archived.Setup).ParryCenterOn, new BattleSimulation(archived.Setup).SquadFirstOn, archived.Setup.Fight.Danger), PxGold);
            }
            PxWrite(_modal, 42, 68, 636, lines);
        }

        // ------------------------------------------------------------ validation

        /// Capture without redrawing the fight first, so a frame set up by hand is what gets saved.
        void CaptureAsDrawn(string filename)
        {
            Canvas.ForceUpdateCanvases(); Camera.main.Render(); var previous = RenderTexture.active; RenderTexture.active = _captureTarget;
            _capturePixels.ReadPixels(new UnityEngine.Rect(0, 0, W, H), 0, 0); _capturePixels.Apply(); RenderTexture.active = previous;
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(_capture, filename + ".png"), _capturePixels.EncodeToPNG());
        }

        /// The readouts through the real buttons (14 Oct 2026, "fix all 9"), on their own campaign and save: the bars,
        /// the leader's mark and name, the Unleash plate's warning and a blocked Unleash, the cast timer and its parry
        /// moment, a parry, a swing named on the bar, two line tags, a fallen token, the why box with what wore away,
        /// the result's WHY, and your fighters' lessons in their own voice.
        IEnumerator FixNineScreens()
        {
            _savePath = System.IO.Path.Combine(_capture, "fix9-screens-save.txt");
            bool done = false;
            for (ulong seed = 1; seed <= 40 && !done; seed++)
            {
                _campaign = Campaign.New(seed, new CampaignTuning { EnemyScaleTier1 = 40 });
                if (!CampaignPilot.PlayTutorial(_campaign, PilotStyle.Reference, seed)) continue;
                _campaign.StartRun();
                int offer = -1;
                for (int n = 0; n < 6 && _campaign.Stage == CampaignStage.InRun; n++)
                {
                    for (int i = 0; i < _campaign.Offers.Count; i++)
                        if (_campaign.Offers[i].Kind == EncounterKind.Trial && _campaign.Offers[i].EnemyCasts) offer = i;
                    if (offer >= 0 || _campaign.Offers.Count == 0) break;
                    CampaignPilot.PlayFight(_campaign, _campaign.StartEncounter(new CampaignPilot(PilotStyle.Reference, seed).PickOffer(_campaign)),
                        new CampaignPilot(PilotStyle.Reference, seed * 31 + (ulong)n));
                }
                if (offer < 0 || _campaign.Stage != CampaignStage.InRun) continue;
                SetPage(Page.Offers); yield return null;
                Check(Click("offer-" + offer), "a trial on the road"); yield return null;
                Check(Click("start-fight"), "the trial starts");
                _pilotOn = false; _paused = true; _speed = 1;
                yield return null;
                var sim = _fight.Sim;
                var leader = sim.Leader(1);
                if (leader == null || !_theyHoldOnUnleash) continue;

                // Run it with nobody giving your orders until they have a point to Hold with and you have two.
                _paused = false; _speed = 4;
                int frames = 0;
                while (!_fight.Over && frames++ < 3000 && !(sim.DirectivePoints(1) >= Directives.Cost(Directive.Hold) &&
                       sim.DirectivePoints(0) >= Directives.Cost(Directive.Unleash) && sim.HoldTicksLeft(1) == 0 && sim.WindupTicks(1) == 0))
                    yield return null;
                _paused = true; yield return null;
                if (_fight.Over || frames >= 3000) continue;
                Check(_leaderMarks.TryGetValue(leader.Id, out PxText mark) && mark.key.StartsWith("LEADER", StringComparison.Ordinal), "their leader wears its mark");
                Check(_nerveLabel[0].key.StartsWith("YOUR NERVE", StringComparison.Ordinal) && _nerveLabel[1].key.Contains("THEIR NERVE"), "both nerve bars have their words");
                string named = UnitName(leader);
                Check(named.EndsWith("Shaman", StringComparison.Ordinal) || named.EndsWith("shaman", StringComparison.Ordinal), "the shaman is called a shaman: " + named);
                Check(UnleashBlockedText(sim).Contains("% DMG / SCARE GUARDED"), "Unleash prediction preserves damage and names only the scare counter");
                // Tags too long for one line take a second line under it; captured as drawn, before a redraw.
                var probe = FindFigure(leader.Id);
                DrawTags(probe, new List<string> { "YOUR FOCUS 4.0S", "COVERED", "PANIC 2.4S" }, new List<Color32> { PxRed, PxBlue, PxLilac });
                Check(probe.tag.key.Contains("\n") && probe.tag.key.Contains("PANIC"), "long tags break onto a second line");
                CaptureAsDrawn("f9-00-two-line-tags");
                Capture("f9-01-bars-leader-warning");
                Check(Click("order-3"), "a plain Unleash into their learned Hold");
                DrainEvents(); yield return null;
                Check(sim.Events.Any(e => e.Kind == BattleEventKind.Directive && e.Detail == BattleSimulation.BlockedDetail), "their Hold blocked it");
                Check(UnleashBlockedText(sim).Contains("% DMG / SCARE BLOCKED"), "active shields block scare while damage remains boosted");
                Check(_pxBanner.key.Contains("DAMAGE")||_pxBanner.key.Contains("DMG"), "Unleash receipt retains its damage benefit");
                Capture("f9-02-unleash-blocked");

                // Their caster winds up on its own: the timer over it, then the moment a Hold parries it.
                _paused = false; _speed = 4; frames = 0;
                while (!_fight.Over && sim.WindupTicks(1) <= FightRules.PerfectHoldTicks + SimClock.TicksPerSecond && frames++ < 4000) yield return null;
                _paused = true; yield return null;
                if (_fight.Over || sim.WindupTicks(1) <= FightRules.PerfectHoldTicks) continue;
                Check(_castLabel.key.StartsWith("CAST", StringComparison.Ordinal), "the cast timer counts down over the caster");
                Capture("f9-03-cast-timer");
                while (!_fight.Over && sim.WindupTicks(1) > FightRules.PerfectHoldTicks) { _fight.Tick(); DrainEvents(); }
                yield return null;
                if (_fight.Over || sim.WindupTicks(1) == 0) continue;
                Check(_castLabel.key.StartsWith("HOLD NOW", StringComparison.Ordinal), "the timer calls the parry moment");
                bool canHold = sim.CanUse(0, Directive.Hold, out _);
                if (canHold) Check(_pxBanner.key.StartsWith("NOW: Hold to parry", StringComparison.Ordinal), "and so does the banner");
                Capture("f9-04-parry-now");
                if (canHold)
                {
                    Check(Click("order-0"), "Hold in the parry moment");
                    while (!_fight.Over && sim.WindupTicks(1) > 0) { _fight.Tick(); DrainEvents(); }
                    yield return null;
                    Check(sim.Events.Any(e => e.Kind == BattleEventKind.Parry && e.ActorSide == 0), "the Hold parried their cast");
                    Capture("f9-05-parried");
                }

                // The rest by the reference pilot: a swing named on a bar, then the why box and the result's WHY.
                _pilot = new CampaignPilot(PilotStyle.Reference, seed); _pilotOn = true; _paused = false; _speed = 24;
                frames = 0;
                bool swingSeen = false;
                while (!_fight.Over && frames++ < 6000)
                {
                    yield return null;
                    if (!swingSeen && (_nerveLabel[1].key.StartsWith("-", StringComparison.Ordinal) || _nerveLabel[0].key.Contains("/-")))
                    { swingSeen = true; Capture("f9-06-nerve-swing"); }
                }
                Check(_fight.Over, "the trial ends");
                for (int i = 0; i < 76; i++) yield return null;
                Check(_whyPanel != null, "the why box is up");
                Check(WhySwingLines(new PxLines(), FightWon()), "the why box names the moments that swung the fight");
                var fresh = NerveSwings(_fight.Setup, _fight.Session.Log.CloneForReplay(), _fight.Sim.CurrentTick, _fight.Encounter.Kind);
                var kept = FightSwings();
                Check(kept.Swings.Count == fresh.Swings.Count && kept.Swings.Select((s, i) => s.Tick == fresh.Swings[i].Tick && s.After == fresh.Swings[i].After).All(same => same),
                    "the why box's swings are this fight's own, not an earlier fight's with the same id");
                bool adds = true;
                for (int s = 0; s < 2; s++)
                {
                    int swung = 0;
                    foreach (var w in kept.Swings) if (w.Side == s) swung += w.Size;
                    adds &= kept.End[s] == sim.Nerve(s) && kept.Start[s] - kept.End[s] == swung + kept.Worn[s];
                }
                Check(adds, "the replay ends on the live fight's nerve, and its swings and slow loss add up to all that was lost");
                foreach (var f in _figures)
                    if (f.body == null && !f.unit.Alive) Check(f.root.localEulerAngles.z != 0f, "a fallen token tips over: " + f.unit.Id);
                Capture("f9-07-why-box");
                Check(Click("see-result"), "the trial's result"); yield return null;
                var learned = LearnedLines(_result, 0).texts;
                int from = learned.IndexOf("Your fighters learned");
                bool theirs = from >= 0;
                for (int i = from + 1; theirs && i < learned.Count && !learned[i].StartsWith("Your habits", StringComparison.Ordinal); i++)
                    if (Regex.IsMatch(learned[i], @"\b[Yy]our?\b")) theirs = false;
                Check(theirs, "your fighters' lessons speak of the enemy as them, not you");
                Check(Click("why"), "the result's WHY button"); yield return null;
                Check(_modal != null, "WHY opens over the result");
                Capture("f9-08-result-why");
                Check(Click("modal-close"), "WHY closes"); yield return null;
                done = true;
            }
            Check(done, "a trial played through the readouts, from the warning to the result's WHY");
        }
    }
}
