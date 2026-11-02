using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KindredSiege.Slice
{
    public sealed partial class CampaignArtLoop
    {
        static readonly string[] PalArtIds = { "butcher", "glutton", "shade", "whisperer" };
        static readonly string[] PalNames = { "BUTCHER", "GLUTTON", "SHADE", "WHISPERER" };
        static readonly string[] PalRoles = { "Weapons & gear", "Armour & body augments", "New orders", "Skins & regalia" };
        // Where each Pal stands at their station in the camp art, screen px at their feet: the Butcher at his
        // forge, the Glutton asleep under his awning, the Shade over her shrine, the Whisperer by her hides.
        static readonly Vector2[] PalFeet = { new Vector2(478, 400), new Vector2(1144, 396), new Vector2(268, 706), new Vector2(962, 766) };
        readonly Dictionary<string, Texture2D> _innerCircleTextures = new Dictionary<string, Texture2D>();
        readonly RectTransform[] _palCards = new RectTransform[4];
        readonly RawImage[] _palImages = new RawImage[4];
        readonly Clip[] _palLoops = new Clip[4];
        int _palHover = -1;
        float _palTime;
        bool _campOpen;
        GameObject _campPreviousSelection;
        Texture2D _campArt;

        Texture2D InnerCircleTexture(string id)
        {
            if (_innerCircleTextures.TryGetValue(id, out Texture2D texture)) return texture;
            texture = Resources.Load<Texture2D>("CampaignArt/InnerCircle/" + id + "_front");
            if (texture == null) throw new InvalidOperationException("Inner-circle sprite missing: " + id);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            _innerCircleTextures.Add(id, texture);
            return texture;
        }

        void AddCampAccess()
        {
            // Presentation of the four existing camp roles; no new upgrade state or shop rules. The title has
            // PALS in its own list since 11 Oct 2026, so only the old Finished page still uses this button.
            if (_page == Page.Title) return;
            Btn("camp", "PALS", 1188, 20, 220, ShowCamp, 32);
        }

        void ShowCamp()
        {
            if (_modal != null || _page == Page.Fight) return;
            LoadPixelKit();
            _campPreviousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            _campOpen = true;
            // An open camp at 1x (Jacob, 11 Oct 2026: "an open land ... where there is enough space for all the
            // guys to have all their tools"): the Broken Wastes ground with each Pal's own station, PixelLab, in
            // the fight's stage frame. The top bar, names and RETURN stay on the 2x kit.
            _modal = Rect(_root, "Camp", 0, 0, W, H);
            Panel(_modal, 0, 0, W, H, Ink).raycastTarget = true;
            _campArt = Resources.Load<Texture2D>("CampaignArt/Camp/camp");
            if (_campArt == null) throw new InvalidOperationException("Camp art missing: CampaignArt/Camp/camp");
            _campArt.filterMode = FilterMode.Point;
            Raw(_modal, "Camp ground", StageX * Px, StageY * Px, StageW * Px, StageH * Px).texture = _campArt;
            _palHover = -1; _palTime = 0f;
            for (int i = 0; i < PalArtIds.Length; i++)
            {
                // Their camp work loops (PixelLab, 10 Oct 2026; Jacob: the camp loops are okay) at 1x. Each stands
                // still until you point at them, then plays the loop, as the locked camp asks.
                Vector2 feet = PalFeet[i];
                var card = Rect(_modal, PalNames[i], feet.x - 96, feet.y - 192, 192, 222);
                _palCards[i] = card;
                _palLoops[i] = LoadClip("CampLoops/" + PalArtIds[i] + "_camp");
                _palImages[i] = Raw(card, PalNames[i] + " camp loop", 0, 0, 192, 192);
                _palImages[i].texture = _palLoops[i].texture;
                ShowPalFrame(i, 0f);
                int x = Mathf.RoundToInt(feet.x / Px), y = Mathf.RoundToInt(feet.y / Px);
                SetPx(PxLabel(_modal, x, y + 3, 1), PalNames[i], PxBone);
                // The pixel font has no "&".
                SetPx(PxLabel(_modal, x, y + 14, 1), PalRoles[i].Replace("&", "and"), PxGold);
            }
            PxImage(_modal, "Top bar", Plate(_kitButton, 12, 720, TopBarH), 0, 0);
            SetPx(PxLabel(_modal, 14, 8), "CAMP", PxBone);
            SetPx(PxLabel(_modal, 14, 21), "The four who stayed with you. Point at one to watch them work.", PxMuted);
            if(_campaign!=null)PxButton("pal-forge","BUTCHER'S FORGE",16,420,216,28,()=>ShowForge(),_modal);
            if(WhispererRun)PxButton("pal-habits","SQUAD HABITS",488,420,216,28,ShowHabits,_modal);
            var close = PxButton("camp-close", "RETURN", 264, 420, 192, 28, CloseCamp, _modal);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(close.gameObject);
        }

        void ShowPalFrame(int i, float time)
        {
            Clip c = _palLoops[i];
            if (c == null || _palImages[i] == null) return;
            FrameRect frame = c.sheet.frames[c.FrameAt(time, true)].frame;
            _palImages[i].uvRect = new UnityEngine.Rect(frame.x / (float)c.texture.width, 1 - (frame.y + frame.h) / (float)c.texture.height,
                frame.w / (float)c.texture.width, frame.h / (float)c.texture.height);
        }

        /// The Pal you point at plays their loop from the start; -1 puts them all back to standing still.
        void PointAtPal(int index, float dt)
        {
            if (index != _palHover)
            {
                if (_palHover >= 0) ShowPalFrame(_palHover, 0f);
                _palHover = index;
                _palTime = 0f;
            }
            if (_palHover < 0) return;
            _palTime += dt;
            ShowPalFrame(_palHover, _palTime);
        }

        void CloseCamp()
        {
            if (!_campOpen) return;
            _campOpen = false;
            _palHover = -1;
            if (_modal != null) CloseModal();
            if (_campArt != null) { Resources.UnloadAsset(_campArt); _campArt = null; }
            _buttons.Remove("camp-close");
            _pxButtonLabels.Remove("camp-close");
            RestoreSelection(_campPreviousSelection);
            _campPreviousSelection = null;
        }

        void InnerCircleInput()
        {
            if (!_campOpen || _modal == null || _auto) return;
            Camera eye = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
            int hover = -1;
            for (int i = 0; i < _palCards.Length; i++)
                if (_palCards[i] != null && RectTransformUtility.RectangleContainsScreenPoint(_palCards[i], Input.mousePosition, eye)) hover = i;
            PointAtPal(hover, Time.unscaledDeltaTime);
        }

        void ClearInnerCircle()
        {
            foreach (var texture in _innerCircleTextures.Values) if (texture != null) Resources.UnloadAsset(texture);
            _innerCircleTextures.Clear();
        }

        // Opt-in player smoke check, using real raycast controls and an isolated save directory.
        IEnumerator ValidateInnerCircle()
        {
            var run = InnerCircleChecks();
            while (true)
            {
                object current = null; bool more;
                try { more = run.MoveNext(); if (more) current = run.Current; }
                catch (Exception e)
                {
                    File.WriteAllText(Path.Combine(_capture, "innercircle-FAILED.txt"), e + "\n" + _log);
                    Debug.LogException(e); Application.Quit(1); yield break;
                }
                if (!more) break;
                yield return current;
            }
            File.WriteAllText(Path.Combine(_capture, "innercircle-PASS.txt"), _log.ToString());
            Application.Quit(0);
        }

        IEnumerator InnerCircleChecks()
        {
            yield return null;
            Capture("innercircle-title");
            Check(Click("camp"), "camp opens from title through raycast"); yield return null;
            Check(_campOpen && _modal != null, "camp modal visible");
            Check(_campArt != null && _campArt.width == StageW * Px && _campArt.height == StageH * Px, "the camp is drawn at 1x in the stage frame");
            for (int i = 0; i < PalArtIds.Length; i++)
            {
                Clip loop = _palLoops[i];
                Check(loop != null && loop.sheet.frames.Length == 40 && loop.texture.filterMode == FilterMode.Point,
                    PalArtIds[i] + " camp loop loads with 40 frames and point sampling");
                Check(Mathf.Approximately(_palImages[i].rectTransform.sizeDelta.x, loop.sheet.frames[0].frame.w), PalArtIds[i] + " stands at 1x");
            }
            Check(!Click("new"), "camp blocks underlying new-campaign button");
            Capture("innercircle-camp");
            var still = _palImages[0].uvRect;
            PointAtPal(0, 1.2f);
            Check(_palImages[0].uvRect != still, "pointing at the Butcher plays his loop");
            Capture("innercircle-camp-butcher-working");
            PointAtPal(-1, 0f);
            Check(_palImages[0].uvRect == still, "the Butcher stands still again when you look away");
            Check(Click("camp-close"), "camp return button works"); yield return null;
            Check(!_campOpen && _modal == null, "camp closes cleanly");
            Check(Click("new"), "new campaign"); yield return null;
            Check(Click("start-tutorial"), "tutorial starts"); yield return null;
            Figure leader = FindFigure(Campaign.LeaderId);
            Check(leader != null && !leader.staticFront && leader.idle != null && leader.body.texture == leader.idle.texture, "leader fights with his own battle clips");
            Check(Mathf.Approximately(leader.body.rectTransform.sizeDelta.x, leader.idle.sheet.frames[0].frame.w * ArtScale), "leader and battlefield share source pixel scale");
            Check(leader.feet.x - 96 * ArtScale + 38 * ArtScale >= StageX * Px, "leader visible pixels stay inside battlefield");
            _paused = true; DrawBattle(); RefreshFight();
            Capture("innercircle-leader-battle");
            Check(_fight.TryOrder(Directive.Hold, null, out _), "Hold given for the leader's guard");
            DrawBattle();
            _visualTime += leader.block.seconds / 2; DrawBattle();
            Check(leader.body.texture == leader.block.texture, "the leader's guard is up while Hold runs");
            Capture("innercircle-leader-guard");
            _paused = false; _pilotOn = true; _speed = 12;
            int guard = 0; while (!_fight.Over && guard++ < 2400) yield return null;
            Check(_fight.Over, "tutorial completes with the leader's clips");
            for (int i = 0; i < 80; i++) yield return null;
            Capture("innercircle-leader-end");
            if (_fight.Sim.Winner == 0) Check(!leader.unit.Alive || leader.body.texture == leader.victory.texture, "the leader plays his victory when the fight is won");
            Check(Click("see-result"), "result opens"); yield return null;
            Capture("innercircle-result");
            Check(Click("camp"), "camp opens from campaign result"); yield return null;
            Capture("innercircle-camp-after-fight");
            Check(Click("camp-close"), "camp closes after fight"); yield return null;
            Check(Click("after-result"), "campaign continues after visiting camp"); yield return null;
            Capture("innercircle-tribe-choice");
            Save();
            Check(CampaignSave.TryRead(File.ReadAllText(_savePath), null, out Campaign saved, out _), "campaign save remains readable");
            Check(CampaignSave.Write(saved) == CampaignSave.Write(_campaign), "camp visit preserves campaign state");
        }
    }
}
