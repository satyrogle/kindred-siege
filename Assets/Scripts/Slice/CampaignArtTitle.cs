using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using KindredSiege.Sim.Learning;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KindredSiege.Slice
{
    // The title screen (Claude for Jacob): a title card once a launch, then one still pixel picture with a
    // small list. Since 15 Oct 2026 the picture is Jacob's pick B: our leader at the open bay of our ship
    // above the Sovereign's planet, the bay and planet drawn 10% bigger so he blocks less of them (made on
    // the local ComfyUI from the locked leader concept). It replaced the 10 Oct town at night that drifted
    // behind a dark strip. Deadpan readouts sit under the list. Everything is drawn at 1x and shown at 3x,
    // the text included. The small font was drawn in code; the logo font is the PixelLab "KS Title Sharp".
    // The files are PNG bytes saved as .bytes, so Unity keeps every pixel as drawn.
    public sealed partial class CampaignArtLoop
    {
        const int TitlePx = 3, ListW = 128, ListX = 24, ListY = 112, ListPitch = 14, ReadoutY = 228, ReadoutPitch = 11;
        const string FontGlyphs = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz.,!?:;'\"-+()0123456789/%*=_$";
        static readonly string[] Jokes = { "SNEEZE", "CHEESE", "BREEZE", "SQUEEZE", "FREEZE" };
        static readonly Color32 TitleNight = new Color32(9, 17, 24, 255), TitleDim = new Color32(44, 88, 100, 255),
            TitleHint = new Color32(66, 118, 124, 255), TitleLamp = new Color32(236, 156, 92, 255),
            TitleGlow = new Color32(255, 214, 160, 255), TitleHud = new Color32(150, 230, 170, 255),
            TitleClear = new Color32(0, 0, 0, 0);
        // The ship's deadpan readouts under the list (intro B, Jacob's pick, 14 Oct 2026).
        static readonly string[] TitleReadouts = { "THREAT: LOW (EST.)", "ARMY: 2000", "CONFIDENCE: 100%", "PREVIOUS ARMIES: 0 (SCANNING)" };

        sealed class TitleItem
        {
            public Button Button;
            public RectTransform Row;
            public RawImage Words;
            public Texture2D On, Off;
            public int Y;
            public string Hint;
        }

        bool _titleCardPlayed;
        Texture2D _titleBay, _titleCursorArt;
        PixelFont _logoFont, _tinyFont;
        readonly List<TitleItem> _titleItems = new List<TitleItem>();
        readonly List<Texture2D> _hintTextures = new List<Texture2D>();
        List<Texture2D> _titleTextures;
        RectTransform _titleCard, _titleHint;
        RawImage _titleCursor;
        int _titleSelected;

        void BuildTitle()
        {
            LoadTitleArt();
            RectTransform screen = _screen;
            _titleTextures = new List<Texture2D>();
            RawImage bay = Raw(screen, "The bay above her planet", 0, 0, W, H);
            bay.texture = _titleBay;
            Texture2D kindred = KeepTitle(_logoFont.Render("KINDRED", TitleGlow, TitleLamp, 2, true));
            Texture2D siege = KeepTitle(_logoFont.Render("SIEGE", TitleGlow, TitleLamp, 2, true));
            PixelImage(screen, "KINDRED", kindred, 12, 14);
            PixelImage(screen, "SIEGE", siege, 12, 16 + kindred.height);

            bool saved = FileExists();
            _titleItems.Clear();
            AddTitleItem(screen, "continue", "CONTINUE", ContinueSaved, saved, saved ? SavedHint() : string.Empty);
            // COMMENCE was NEW CAMPAIGN until 15 Oct 2026: the word on the main screen Jacob picked.
            AddTitleItem(screen, "new", _gatesReview ? "NEW REVIEW" : "COMMENCE", NewCampaign, true, _roadReview ? (_clearReview?"Rules 13. Combos, defence, boosts, then carriers. Separate save.":_kitReview?"Rules 12. Automatic boosts and Burn. Separate save.":"Rules 11 road preview. Fall Back spam is too strong. Separate save.") : _gatesReview ? "Fresh "+ReviewCheckpoint+" checkpoint. Resets only this review save." : saved ? "Starts over. Your saved campaign is replaced." : string.Empty);
            // PALS sits in the list since 11 Oct 2026, in place of the old corner button.
            AddTitleItem(screen, "camp", "PALS", ShowCamp, true, "The four who stayed with you.");
            AddTitleItem(screen, "missing", "MISSING ART", ShowMissing, true, "What is still a placeholder in this build.");
            AddTitleItem(screen, "quit", "QUIT", () => Application.Quit(), true, string.Empty);
            _titleCursor = PixelImage(screen, "Cursor", _titleCursorArt, 16, ListY);
            _titleHint = Rect(screen, "Hint", ListX * TitlePx, (ListY + _titleItems.Count * ListPitch + 8) * TitlePx, 104 * TitlePx, 60 * TitlePx);
            for (int i = 0; i < TitleReadouts.Length; i++)
                PixelImage(screen, TitleReadouts[i], KeepTitle(_tinyFont.Render(TitleReadouts[i], TitleHud, TitleHud, 1, true)), ListX, ReadoutY + i * ReadoutPitch);
            _titleSelected = -1;
            SelectTitle(saved ? 0 : 1);
            // The notice is drawn in the title's own pixel text (CampaignArtPixelPages.cs), not the old line.
            if (_noticeText != null) _noticeText.gameObject.SetActive(false);
            StartCoroutine(TitleNotice(screen));
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            StartCoroutine(TitleLife(screen, _titleTextures));
            if (!_auto && !_titleCardPlayed) StartCoroutine(TitleCard());
        }

        string SavedHint()
        {
            if (!CampaignSave.TryReadFile(_savePath, null, out Campaign c, out _)) return _saveSummary;
            var names = new List<string>();
            for (int i = 0; i < c.Joined.Count; i++) names.Add(TribeName(c.Joined[i]));
            string with = names.Count == 0 ? "The tutorial is next." : string.Join(", ", names.ToArray()) + " with you.";
            return with + "\nRuns " + c.RunsStarted + ", won " + c.RunsWon + ".";
        }

        void AddTitleItem(RectTransform screen, string id, string label, Action action, bool enabled, string hint)
        {
            int y = ListY + _titleItems.Count * ListPitch;
            RectTransform row = Rect(screen, id, 0, (y - 2) * TitlePx, ListW * TitlePx, ListPitch * TitlePx);
            Image hit = row.gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0);
            Button button = row.gameObject.AddComponent<Button>();
            button.targetGraphic = hit;
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.interactable = enabled;
            button.onClick.AddListener(() => action());
            _buttons[id] = button;
            Color32 idle = enabled ? TitleGlow : TitleDim;
            var item = new TitleItem
            {
                Button = button,
                Row = row,
                Y = y,
                Hint = hint,
                Off = KeepTitle(_tinyFont.Render(label, idle, idle, 1, true)),
                On = KeepTitle(_tinyFont.Render(label, TitleLamp, TitleLamp, 1, true)),
            };
            item.Words = PixelImage(row, label, item.Off, ListX, 2);
            _titleItems.Add(item);
        }

        void SelectTitle(int index)
        {
            if (index == _titleSelected || index < 0 || index >= _titleItems.Count || !_titleItems[index].Button.interactable) return;
            if (_titleSelected >= 0) _titleItems[_titleSelected].Words.texture = _titleItems[_titleSelected].Off;
            _titleSelected = index;
            TitleItem item = _titleItems[index];
            item.Words.texture = item.On;
            _titleCursor.rectTransform.anchoredPosition = new Vector2(16 * TitlePx, -(item.Y + 2) * TitlePx);
            ShowTitleHint(item.Hint);
        }

        void StepTitle(int direction)
        {
            int count = _titleItems.Count;
            for (int i = 1; i <= count; i++)
            {
                int next = ((_titleSelected + direction * i) % count + count) % count;
                if (_titleItems[next].Button.interactable)
                {
                    SelectTitle(next);
                    return;
                }
            }
        }

        void ShowTitleHint(string text)
        {
            foreach (Transform child in _titleHint) Destroy(child.gameObject);
            for (int i = 0; i < _hintTextures.Count; i++) Destroy(_hintTextures[i]);
            _hintTextures.Clear();
            int y = 0;
            foreach (string line in _tinyFont.Wrap(text, 104, 1))
            {
                if (line.Length == 0) continue;
                Texture2D words = _tinyFont.Render(line, TitleHint, TitleHint, 1, false);
                _hintTextures.Add(words);
                PixelImage(_titleHint, line, words, 0, y);
                y += words.height;
            }
        }

        /// While the title is up: the cursor bobs and the list follows the mouse.
        /// Keyboard input belongs to KeyboardInput. The picture stands still since 15 Oct 2026.
        IEnumerator TitleLife(RectTransform screen, List<Texture2D> made)
        {
            float age = 0f;
            Vector3 mouse = Input.mousePosition;
            while (_screen == screen)
            {
                age += Time.unscaledDeltaTime;
                if (_titleSelected >= 0)
                {
                    int nudge = Mathf.FloorToInt(age * 2.5f) % 2;
                    _titleCursor.rectTransform.anchoredPosition = new Vector2((16 - nudge) * TitlePx, -(_titleItems[_titleSelected].Y + 2) * TitlePx);
                }
                if (_modal == null && _titleCard == null)
                {
                    if (Input.mousePosition != mouse)
                    {
                        mouse = Input.mousePosition;
                        Camera eye = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
                        for (int i = 0; i < _titleItems.Count; i++)
                            if (RectTransformUtility.RectangleContainsScreenPoint(_titleItems[i].Row, mouse, eye)) SelectTitle(i);
                    }
                }
                yield return null;
            }
            for (int i = 0; i < made.Count; i++) Destroy(made[i]);
        }

        /// Black, then the name lands with a shake, holds, and the menu fades in with the logo. The wrong names
        /// that ran first (KINDRED FREEZE, CHEESE, BEES, 11 Oct 2026) are cut: too cringy for some players
        /// (Jacob, 14 Oct 2026). Any key or click skips it. Once a launch, never in the automatic validation.
        IEnumerator TitleCard()
        {
            _titleCardPlayed = true;
            RectTransform card = Rect(_root, "Title card", 0, 0, W, H);
            _titleCard = card;
            Image black = card.gameObject.AddComponent<Image>();
            black.color = TitleNight;
            CanvasGroup fade = card.gameObject.AddComponent<CanvasGroup>();
            Texture2D[] names = { _logoFont.Render("KINDRED SIEGE", TitleGlow, TitleLamp, 2, true) };
            const float First = 0.5f, Step = 0.75f, Hold = 1.6f;
            float landed = First + Step * (names.Length - 1), end = landed + Hold;
            RawImage words = null;
            float age = 0f;
            while (age < end + 0.5f && !Input.anyKeyDown)
            {
                age += Time.unscaledDeltaTime;
                if (words == null && age >= First) words = PixelImage(card, "Words", names[0], 0, 0);
                if (words != null)
                {
                    int index = Mathf.Min(names.Length - 1, Mathf.FloorToInt((age - First) / Step));
                    Texture2D shown = names[index];
                    float since = age - (First + Step * index);
                    int shake = since < 0.12f ? (Mathf.FloorToInt(age * 40f) % 2 == 0 ? 1 : -1) : 0;
                    if (index == names.Length - 1 && since < 0.25f) shake *= 2;
                    words.texture = shown;
                    words.rectTransform.sizeDelta = new Vector2(shown.width * TitlePx, shown.height * TitlePx);
                    words.rectTransform.anchoredPosition = new Vector2(((480 - shown.width) / 2 + shake) * TitlePx, -((300 - shown.height) / 2) * TitlePx);
                }
                if (age > end) fade.alpha = Mathf.Clamp01(1f - (age - end) / 0.5f);
                yield return null;
            }
            _titleCard = null;
            Destroy(card.gameObject);
            foreach (Texture2D name in names) Destroy(name);
        }

        Texture2D KeepTitle(Texture2D texture)
        {
            _titleTextures.Add(texture);
            return texture;
        }

        static RawImage PixelImage(Transform parent, string name, Texture2D texture, int x, int y)
        {
            RawImage image = Raw(parent, name, x * TitlePx, y * TitlePx, texture.width * TitlePx, texture.height * TitlePx);
            image.texture = texture;
            return image;
        }

        void LoadTitleArt()
        {
            if (_titleBay != null) return;
            _titleBay = TitleArt("CampaignArt/Menu/title_bay", false);
            Texture2D logo = TitleArt("CampaignArt/Menu/font_logo", false), tiny = TitleArt("CampaignArt/Menu/font_tiny", false);
            _logoFont = new PixelFont(logo);
            _tinyFont = new PixelFont(tiny);
            Destroy(logo);
            Destroy(tiny);
            _titleCursorArt = MakeCursor();
        }

        static Texture2D TitleArt(string path, bool repeat)
        {
            TextAsset asset = Resources.Load<TextAsset>(path);
            if (asset == null) throw new InvalidOperationException("Missing title art: Resources/" + path + ".bytes");
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.LoadImage(asset.bytes);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            return texture;
        }

        static Texture2D MakeCursor()
        {
            int[] runs = { 1, 2, 3, 2, 1 };
            var pixels = new Color32[3 * 5];
            for (int y = 0; y < 5; y++)
                for (int x = 0; x < 3; x++)
                    pixels[(4 - y) * 3 + x] = x < runs[y] ? TitleLamp : TitleClear;
            var texture = new Texture2D(3, 5, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        /// A pixel font atlas of 8 columns in FontGlyphs order. Text comes out with a one pixel dark
        /// ring and a one pixel drop shadow; capitals are cut to the capitals' rows, other text keeps
        /// room for descenders so lines share a baseline. The lower part can take a second colour.
        sealed class PixelFont
        {
            readonly Color32[] _pixels;
            readonly int _width, _height, _space, _capTop, _capRows, _allTop, _allRows;
            readonly Dictionary<char, RectInt> _glyphs = new Dictionary<char, RectInt>();

            public PixelFont(Texture2D atlas)
            {
                _pixels = atlas.GetPixels32();
                _width = atlas.width;
                _height = atlas.height;
                int cell = _width / 8;
                _space = Mathf.Max(3, cell / 3);
                int capTop = cell, capBottom = -1, allTop = cell, allBottom = -1;
                for (int i = 0; i < FontGlyphs.Length; i++)
                {
                    int cx = i % 8 * cell, cy = i / 8 * cell, left = cell, right = -1, top = cell, bottom = -1;
                    for (int y = 0; y < cell; y++)
                        for (int x = 0; x < cell; x++)
                            if (Ink(cx + x, cy + y))
                            {
                                left = Mathf.Min(left, x);
                                right = Mathf.Max(right, x);
                                top = Mathf.Min(top, y);
                                bottom = Mathf.Max(bottom, y);
                            }
                    if (right < left) continue;
                    _glyphs[FontGlyphs[i]] = new RectInt(cx + left, cy, right - left + 1, cell);
                    allTop = Mathf.Min(allTop, top);
                    allBottom = Mathf.Max(allBottom, bottom);
                    if (i < 26)
                    {
                        capTop = Mathf.Min(capTop, top);
                        capBottom = Mathf.Max(capBottom, bottom);
                    }
                }
                _capTop = capTop;
                _capRows = capBottom - capTop + 1;
                _allTop = allTop;
                _allRows = allBottom - allTop + 1;
            }

            // Rows counted from the top of the atlas, as it was drawn.
            bool Ink(int x, int y) => _pixels[(_height - 1 - y) * _width + x].a > 127;

            public int Measure(string text, int spacing)
            {
                int width = 0;
                foreach (char ch in text)
                    if (ch == ' ') width += _space;
                    else if (_glyphs.TryGetValue(ch, out RectInt g)) width += g.width + spacing;
                return Mathf.Max(1, width - spacing);
            }

            public List<string> Wrap(string text, int maxWidth, int spacing)
            {
                var lines = new List<string>();
                foreach (string paragraph in text.Split('\n'))
                {
                    string line = string.Empty;
                    foreach (string word in paragraph.Split(' '))
                    {
                        string longer = line.Length == 0 ? word : line + " " + word;
                        if (line.Length > 0 && Measure(longer, spacing) > maxWidth)
                        {
                            lines.Add(line);
                            line = word;
                        }
                        else line = longer;
                    }
                    lines.Add(line);
                }
                return lines;
            }

            sealed class Raster { public int width,height; public Color32[] pixels; }
            readonly Dictionary<string,Raster> _rasterCache=new Dictionary<string,Raster>();
            readonly Queue<string> _rasterKeys=new Queue<string>();
            int _rasterPixels;
            public bool RasterCacheEnabled=true;
            public int CachedPixels=>_rasterPixels;
            const int RasterBudget=512000;
            public Texture2D Render(string text, Color32 fill, Color32 lower, int spacing, bool caps)
            {
                string key=text+"|"+fill.r+","+fill.g+","+fill.b+","+fill.a+"|"+lower.r+","+lower.g+","+lower.b+","+lower.a+"|"+spacing+"|"+caps;
                if(!RasterCacheEnabled||!_rasterCache.TryGetValue(key,out Raster raster))
                {
                    raster=Rasterize(text,fill,lower,spacing,caps);
                    if(RasterCacheEnabled&&raster.pixels.Length<=RasterBudget)
                    {
                        while(_rasterPixels+raster.pixels.Length>RasterBudget&&_rasterKeys.Count>0)
                        {string oldest=_rasterKeys.Dequeue();_rasterPixels-=_rasterCache[oldest].pixels.Length;_rasterCache.Remove(oldest);}
                        _rasterCache[key]=raster;_rasterKeys.Enqueue(key);_rasterPixels+=raster.pixels.Length;
                    }
                }
                var texture=new Texture2D(raster.width,raster.height,TextureFormat.RGBA32,false){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
                texture.SetPixels32(raster.pixels);texture.Apply();return texture;
            }
            Raster Rasterize(string text, Color32 fill, Color32 lower, int spacing, bool caps)
            {
                int top = caps ? _capTop : _allTop, rows = caps ? _capRows : _allRows, split = rows * 11 / 20;
                int w = Measure(text, spacing) + 2, h = rows + 3;
                var ink = new bool[w * h];
                int pen = 1;
                foreach (char ch in text)
                {
                    if (ch == ' ')
                    {
                        pen += _space;
                        continue;
                    }
                    if (!_glyphs.TryGetValue(ch, out RectInt g)) continue;
                    for (int y = 0; y < rows; y++)
                        for (int x = 0; x < g.width && pen + x < w - 1; x++)
                            if (Ink(g.x + x, g.y + top + y)) ink[(y + 1) * w + pen + x] = true;
                    pen += g.width + spacing;
                }
                var pixels = new Color32[w * h];
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        Color32 c = TitleClear;
                        if (ink[y * w + x]) c = y - 1 >= split ? lower : fill;
                        else if (Near(ink, w, h, x, y) || Near(ink, w, h, x, y - 1)) c = TitleNight;
                        pixels[(h - 1 - y) * w + x] = c;
                    }
                return new Raster{width=w,height=h,pixels=pixels};
            }

            static bool Near(bool[] ink, int w, int h, int x, int y)
            {
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (nx >= 0 && ny >= 0 && nx < w && ny < h && ink[ny * w + nx]) return true;
                    }
                return false;
            }
        }
    }
}
