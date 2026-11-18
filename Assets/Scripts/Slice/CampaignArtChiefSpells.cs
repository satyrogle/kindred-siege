using System;
using KindredSiege.Sim;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using UnityEngine;
using UnityEngine.UI;

namespace KindredSiege.Slice
{
    // Presentation only. Spells, counters, wall duration and assist eligibility belong to the sim.
    public sealed partial class CampaignArtLoop
    {
        RectTransform _chiefScreen;
        readonly RectTransform[] _chiefPanels = new RectTransform[2], _wallPanels = new RectTransform[2];
        readonly PxText[] _spellTimers = new PxText[2], _wallLabels = new PxText[2];
        readonly Image[] _spellFills = new Image[2];
        readonly float[] _cutUntil = { -100, -100 }, _wallSmashedAt = { -100, -100 };
        readonly int[] _spellTotal = new int[2];
        RawImage _chiefOrderIcon;
        RawImage _wallBreakWord;
        // Sequential fights and deterministic captures share the presentation clock.
        // Legacy simultaneous/stage fights retain their original wall-clock band.
        float ChiefCutClock => BeatsOn ? _visualTime : Time.unscaledTime;

        bool ChiefCutInVisible => (_chiefPanels[0] != null && _chiefPanels[0].gameObject.activeSelf) ||
            (_chiefPanels[1] != null && _chiefPanels[1].gameObject.activeSelf);

        bool RecentWallSmash(int side) => !ViewOver && _visualTime - _wallSmashedAt[side] < .8f;

        static string SpellCounter(ChiefSpell spell, bool parryCenter = false)
        {
            if(parryCenter)return ChiefSpells.Name(spell)+": time live Hold at the beat to parry. "+SpellCounter(spell);
            switch (spell)
            {
                case ChiefSpell.SteelStorm: return "Steel Storm: Unleash smashes the wall once it rises.";
                case ChiefSpell.Firebomb: return "Firebomb: Fall Back dodges the fire.";
                case ChiefSpell.WarCry: return "War Cry: Focus Fire on the caster interrupts it.";
                case ChiefSpell.DustShark: return "Dust Shark: Fall Back protects the back row.";
                case ChiefSpell.BloodCall: return "Blood Call: time Fall Back near release to protect bleeding fighters.";
                case ChiefSpell.Tide: return "Tide: time Fall Back near release to protect the back row.";
                default: return "Eclipse: time Hold in the final half second to parry.";
            }
        }

        static string ChiefEffect(ChiefSpell spell)
        {
            switch (spell)
            {
                case ChiefSpell.SteelStorm: return "Shields our side";
                case ChiefSpell.Firebomb: return "Burns their row";
                case ChiefSpell.WarCry: return "Restores nerve";
                case ChiefSpell.DustShark: return "Hits back row";
                case ChiefSpell.BloodCall: return "Hits bleeding foes";
                case ChiefSpell.Tide: return "Hits back row";
                default: return "Hits front row";
            }
        }

        static string SpellIcon(ChiefSpell spell)
        {
            return spell == ChiefSpell.SteelStorm ? "hold" : "eclipse";
        }

        string ChiefOrderHint()
        {
            if (_fight == null || _fight.Assist == null) return "Chief order: no chief available in this fight.";
            return _fight.Assist.Name + ": " + ChiefEffect(ViewSim.Spell(0)) + ". Once per fight. " +
                (_fight.AssistUsed ? "Already used." : ViewSim.MeterOn?"Costs "+MeterRules.Bars+" parry bars; no order points.":"Costs " + Directives.Cost(Directive.Eclipse) + " points.");
        }

        void AddSpellBrief(PxLines lines, Encounter encounter)
        {
            if (encounter != null && encounter.EnemyCasts && !HunterFight(encounter.Kind))
                lines.Add(SpellCounter(encounter.Tribe.Spell, true), OrderColour(ChiefSpells.Answer(encounter.Tribe.Spell)));
        }

        void EnsureChiefPresentation(BattleSimulation sim)
        {
            if (_chiefScreen == _screen) return;
            _chiefScreen = _screen;
            for (int side = 0; side < 2; side++)
            {
                _chiefPanels[side] = null;
                _cutUntil[side] = _wallSmashedAt[side] = -100;
                _spellTotal[side] = 0;
                int x = side == 0 ? 22 : 406;
                _spellTimers[side] = PxLabel(_stage, x, 83);
                _spellFills[side] = Panel(_stage, x * Px, 95 * Px, 0, 2 * Px, OrderColour(sim.SpellAnswer(side)));
                var wall = _wallPanels[side] = Rect(_stage, "Steel Storm " + side, 0, 0, W, H);
                // HUD shields outline the protected lane. Transparent centres keep fighters visible.
                int wx = side == 0 ? 337 : 375;
                for (int i = 0; i < 5; i++)
                {
                    int y = 190 + i * 29;
                    Panel(wall, wx * Px, y * Px, 8, 48, PxBlue);
                    Panel(wall, (wx - 8) * Px, y * Px, 34, 4, PxBone);
                    Panel(wall, (wx - 8) * Px, (y + 22) * Px, 34, 4, PxBlue);
                    var shield = PxImage(wall, "Guard", _kitIcons["hold"], wx - 14, y - 3);
                    shield.color = new Color(1, 1, 1, .65f);
                }
                _wallLabels[side] = PxLabel(_stage, side == 0 ? 240 : 480, 350, 1);
                wall.gameObject.SetActive(false);
            }
            var smash = _logoFont.Render("WALL SMASHED", PxOrange, PxGold, 1, true);
            _screenTextures.Add(smash);
            _wallBreakWord = PxImage(_stage, "Wall smash moment", smash, (720 - smash.width) / 2, 361);
            _wallBreakWord.enabled = false;
        }

        Texture2D ChiefPortrait(int side, ChiefSpell spell)
        {
            if (side == 1 && HunterFight(ViewKind)) return null;
            string key = spell == ChiefSpell.Eclipse ? "bloodridge" : spell == ChiefSpell.SteelStorm ? "skullpeak" : spell == ChiefSpell.DustShark ? "clarth" : spell == ChiefSpell.BloodCall ? "stoneveil" : spell == ChiefSpell.Tide ? "thundercoast" : null;
            if (key == null) return null; // No invented portraits for undelivered chiefs or hunter spells.
            var texture = TitleArt("CampaignArt/ChiefCutIns/" + key, false);
            _screenTextures.Add(texture);
            return texture;
        }

        RectTransform BuildChiefPanel(int side, ChiefSpell spell, Figure caster = null)
        {
            var panel = Rect(_stage, "Chief spell cut-in " + side, 0, 0, W, H);
            Color32 dark = spell == ChiefSpell.SteelStorm ? new Color32(43, 39, 35, 255) : new Color32(87, 22, 21, 255);
            Color32 stripe = spell == ChiefSpell.SteelStorm ? new Color32(58, 53, 48, 255) : new Color32(102, 28, 26, 255);
            if (spell == ChiefSpell.BloodCall) { dark = new Color32(90, 13, 21, 255); stripe = new Color32(102, 13, 22, 255); }
            if (spell == ChiefSpell.Tide) { dark = new Color32(29, 58, 67, 255); stripe = new Color32(37, 69, 77, 255); }
            var pixels = new Color32[720 * 190];
            for (int y = 0; y < 190; y++)
                for (int x = 0; x < 720; x++) pixels[y * 720 + x] = ((x - y / 2 + 190) / 12) % 2 == 0 ? dark : stripe;
            var band = new Texture2D(720, 190, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            band.SetPixels32(pixels); band.Apply(); _screenTextures.Add(band);
            PxImage(panel, "Chief band", band, 0, 135);
            Panel(panel, 0, 135 * Px, W, 2 * Px, PxBone);
            Panel(panel, 0, 323 * Px, W, 2 * Px, PxBone);
            bool champion = side == 1 && ViewSim.ChampionFightOn && caster != null && caster.unit.IsLeader;
            var portrait = champion ? null : ChiefPortrait(side, spell);
            if (portrait != null) PxImage(panel, "Chief portrait", portrait, 24, 325 - portrait.height);
            bool championArt = champion && caster.idle != null;
            if (championArt)
            {
                // Use the actual bound champion sheet at native pixels. A spell's
                // chief portrait is a different character, not this spell's caster.
                var clip = caster.idle; var frame = clip.sheet.frames[0].frame;
                var body = Raw(panel, "Champion cut-in identity", 320 - frame.w / 2, 646 - frame.h, frame.w, frame.h);
                body.texture = clip.texture;
                body.uvRect = new UnityEngine.Rect(frame.x / (float)clip.texture.width,
                    1 - (frame.y + frame.h) / (float)clip.texture.height,
                    frame.w / (float)clip.texture.width, frame.h / (float)clip.texture.height);
                body.raycastTarget = false;
            }
            int textX = portrait == null && !championArt ? 70 : 380;
            SetPx(PxLabel(panel, textX, 151), side == 0 ? "YOUR CHIEF" : champion ? "THEIR CHAMPION" : "THEIR CHIEF", side == 0 ? PxMint : PxRed);
            var title = _logoFont.Render(ChiefSpells.Name(spell).ToUpperInvariant(), PxBone, PxMuted, 1, true);
            _screenTextures.Add(title); PxImage(panel, "Spell name", title, textX, 169);
            var answer = ChiefSpells.Answer(spell);
            string word = side == 0 ? ChiefEffect(spell) : OrderName(answer);
            var text = _logoFont.Render(word.ToUpperInvariant(), side == 0 ? PxMint : OrderColour(answer), PxMuted, 1, true);
            _screenTextures.Add(text);
            if (side == 1)
            {
                string icon = answer == Directive.Hold ? "hold" : answer == Directive.Unleash ? "unleash" : answer == Directive.FocusFire ? "focus" : "fallback";
                PxImage(panel, "Answer icon", _kitIcons[icon], textX, 215);
            }
            PxImage(panel, "Spell answer", text, textX + (side == 1 ? 36 : 0), 220);
            SetPx(PxLabel(panel, textX, 260), side == 0 ? "ONCE THIS FIGHT" : RaidHud&&spell==ChiefSpell.Eclipse ? "WATCH THE BEAT RING / OR FALL BACK" : spell == ChiefSpell.SteelStorm ? "WHEN THE WALL RISES" : answer == Directive.Hold ? "TIME IT AT THE GOLD MARK" : spell == ChiefSpell.BloodCall || spell == ChiefSpell.Tide ? "TIME FALL BACK NEAR RELEASE" : "BEFORE THE CAST LANDS", PxBone);
            // Decorative UI must never capture a click intended for a fighter or an order.
            foreach (var graphic in panel.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
            panel.gameObject.SetActive(false);
            return panel;
        }

        void ChiefSpellEvent(BattleEvent e)
        {
            if (_seeking || e.ActorSide < 0 || e.ActorSide > 1) return;
            var sim = ViewSim; int side = e.ActorSide;
            EnsureChiefPresentation(sim);
            if (e.Kind == BattleEventKind.Windup && e.Detail == ChiefSpells.Name(sim.Spell(side)))
            {
                if (_chiefPanels[side] == null) _chiefPanels[side] = BuildChiefPanel(side, sim.Spell(side), FindFigure(e.ActorId));
                _cutUntil[side] = ChiefCutClock + .9f;
                _cutUntil[1 - side] = -100;
            }
            if (e.Kind == BattleEventKind.Wall && e.Detail == ChiefSpells.WallSmashed)
                _wallSmashedAt[side] = _visualTime;
        }

        void DrawChiefSpells(BattleSimulation sim)
        {
            EnsureChiefPresentation(sim);
            for (int side = 0; side < 2; side++)
            {
                int ticks = ViewOver ? 0 : sim.WindupTicks(side);
                if (ticks > _spellTotal[side]) _spellTotal[side] = ticks;
                if (ticks == 0) _spellTotal[side] = 0;
                bool urgentHeavy = sim.HeavyTicks(1) > 0 && sim.HeavyTicks(1) <= FightRules.PerfectHoldTicks;
                bool show = ticks > FightRules.PerfectHoldTicks && !urgentHeavy && !_targeting && ChiefCutClock < _cutUntil[side] && _modal == null;
                if(RaidHud&&side==1)show=show&&!_paused&&!ViewOver&&ticks>FightRules.PerfectHoldTicks+1;
                if (_chiefPanels[side] != null) _chiefPanels[side].gameObject.SetActive(show);
                string spell = ChiefSpells.Name(sim.Spell(side));
                string answer = sim.Spell(side) == ChiefSpell.SteelStorm ? "UNLEASH AFTER WALL" : OrderName(sim.SpellAnswer(side)).ToUpperInvariant();
                SetPx(_spellTimers[side], ticks > 0 ? (side == 0 ? "YOUR " : "THEIR ") + spell + " " + Seconds(ticks) + (side == 1 ? " / " + answer : "") : "", OrderColour(sim.SpellAnswer(side)));
                Place(_spellFills[side], side == 0 ? 22 : 406, 95, ticks == 0 ? 0 : Mathf.RoundToInt(280f * ticks / Math.Max(1, _spellTotal[side])), 2);
                int wallTicks = ViewOver ? 0 : sim.WallTicks(side);
                bool lockdownWall = !ViewOver && sim.InLockdown(side) && sim.WallPercentNow(side) < 100;
                _wallPanels[side].gameObject.SetActive(wallTicks > 0 || lockdownWall);
                SetPx(_wallLabels[side], wallTicks > 0 || lockdownWall ? (side == 0 ? "OUR WALL " : "THEIR WALL ") +
                    (lockdownWall ? "LOCKDOWN" : Seconds(wallTicks)) + " / HITS " + sim.WallPercentNow(side) + "%" : "", PxBlue);
            }
            _wallBreakWord.enabled = RecentWallSmash(0) || RecentWallSmash(1);
            _wallBreakWord.transform.SetAsLastSibling();
            // Cast and heavy tells stay above the temporary comic band.
            if (_readoutLayer != null) _readoutLayer.SetAsLastSibling();
            foreach (var mark in _leaderMarks.Values) mark.root.gameObject.SetActive(!ChiefCutInVisible);
            if (_castBack != null && !CleanCombatHud) DrawCastTimer(sim);
            foreach (var timer in _spellTimers) timer.root.SetAsLastSibling();
        }
    }
}
