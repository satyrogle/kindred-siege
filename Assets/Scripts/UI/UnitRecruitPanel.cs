using UnityEngine;
using KindredSiege.Battle;
using KindredSiege.Core;

namespace KindredSiege.UI
{
    /// <summary>
    /// Unit Recruitment Panel — city-phase roster management (GDD §Campaign Loop).
    ///
    /// Two-column OnGUI overlay:
    ///   Left  — current expedition roster (dismiss units here)
    ///   Right — recruit catalog (hire new units here)
    ///
    /// Slot cap comes from CityBattleBridge.MaxUnitSlots (starts 4, grows with buildings).
    /// Hire costs Gold + Food (and optionally Materials) defined per UnitData.
    ///
    /// BROKEN units (Fatigue ≥ 100) cannot be hired — they must rest first.
    ///
    /// Uses OnGUI — no Canvas prefab required.
    /// Attach to the persistent Manager GameObject. Toggle via Show() / Hide().
    /// CityHUD calls Show() from the "Manage Roster" button.
    /// </summary>
    public class UnitRecruitPanel : MonoBehaviour
    {
        public static UnitRecruitPanel Instance { get; private set; }

        // ─── State ───
        private bool _visible;
        private Vector2 _rosterScroll;
        private Vector2 _catalogScroll;

        // ─── Layout ───
        private const int OverlayAlpha  = 200;
        private const int PanelW        = 860;
        private const int PanelH        = 520;
        private const int ColW          = 390;
        private const int RowH          = 82;
        private const int BtnW          = 110;
        private const int BtnH          = 28;
        private const int Margin        = 14;

        // ─── Styles ───
        private GUIStyle _panelStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _subStyle;
        private GUIStyle _warnStyle;
        private GUIStyle _btnStyle;
        private GUIStyle _dimBtnStyle;
        private GUIStyle _greenStyle;
        private bool     _stylesReady;

        // ════════════════════════════════════════════
        // LIFECYCLE
        // ════════════════════════════════════════════

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void Show() => _visible = true;
        public void Hide() => _visible = false;

        // ════════════════════════════════════════════
        // OnGUI
        // ════════════════════════════════════════════

        private void OnGUI()
        {
            if (!_visible) return;

            EnsureStyles();

            // Darkened backdrop
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Centred panel
            int px = (Screen.width  - PanelW) / 2;
            int py = (Screen.height - PanelH) / 2;
            GUI.Box(new Rect(px, py, PanelW, PanelH), GUIContent.none, _panelStyle);

            // Title bar
            GUI.Label(new Rect(px + Margin, py + 12, PanelW - Margin * 2 - 120, 26),
                "EXPEDITION ROSTER", _titleStyle);

            var roster = RosterManager.Instance;
            if (roster != null)
            {
                GUI.Label(new Rect(px + PanelW - 180, py + 16, 160, 20),
                    $"Slots: {roster.RosterCount} / {roster.MaxSlots}", _subStyle);
            }

            // Close button
            if (GUI.Button(new Rect(px + PanelW - 110, py + 8, 96, BtnH), "Close", _btnStyle))
                Hide();

            int contentY = py + 48;
            int contentH = PanelH - 56;

            // ── Left column: active roster ──
            DrawRosterColumn(px + Margin, contentY, ColW, contentH, roster);

            // Vertical divider
            GUI.color = new Color(0.3f, 0.3f, 0.4f);
            GUI.DrawTexture(new Rect(px + Margin + ColW + 8, contentY, 1, contentH - 8),
                Texture2D.whiteTexture);
            GUI.color = Color.white;

            // ── Right column: recruit catalog ──
            DrawCatalogColumn(px + Margin + ColW + 18, contentY, ColW, contentH, roster);
        }

        // ════════════════════════════════════════════
        // LEFT — Active Roster
        // ════════════════════════════════════════════

        private void DrawRosterColumn(int x, int y, int w, int h, RosterManager roster)
        {
            GUI.Label(new Rect(x, y, w, 22), "CURRENT EXPEDITION", _titleStyle);
            y += 28;
            h -= 28;

            if (roster == null || roster.RosterCount == 0)
            {
                GUI.Label(new Rect(x, y + 20, w, 22), "No units deployed.", _subStyle);
                return;
            }

            // Scrollable list
            Rect viewRect = new Rect(x, y, w, h);
            Rect contentRect = new Rect(0, 0, w - 16, roster.RosterCount * RowH);
            _rosterScroll = GUI.BeginScrollView(viewRect, _rosterScroll, contentRect);

            int iy = 0;
            for (int i = 0; i < roster.RosterCount; i++)
            {
                var unit = roster.ActiveRoster[i];
                if (unit == null) continue;

                bool broken = FatigueSystem.IsUndeployable(unit);

                // Row background
                GUI.color = broken
                    ? new Color(0.5f, 0.1f, 0.1f, 0.4f)
                    : new Color(0.15f, 0.2f, 0.3f, 0.5f);
                GUI.DrawTexture(new Rect(0, iy, w - 16, RowH - 4), Texture2D.whiteTexture);
                GUI.color = Color.white;

                int ix = 8;
                int ry = iy + 8;

                // Unit name + class
                GUI.Label(new Rect(ix, ry, w - 130, 20), unit.UnitName, _labelStyle);
                GUI.Label(new Rect(ix, ry + 20, w - 130, 16),
                    $"{CapFirst(unit.UnitType)}  |  HP {unit.MaxHP}  ATK {unit.AttackDamage}  " +
                    $"Sanity {unit.BaseSanity - unit.MaxSanityPenalty}/{unit.BaseSanity}", _subStyle);

                // Status line: fatigue + phobia
                string statusLine = FatigueDescription(unit.FatigueLevel);
                if (unit.ActivePhobia != PhobiaType.None)
                    statusLine += $"  |  {PhobiaShort(unit.ActivePhobia)}";
                if (unit.MaxSanityPenalty > 0)
                    statusLine += $"  |  FK -{unit.MaxSanityPenalty}";

                GUIStyle statusStyle = broken ? _warnStyle : _subStyle;
                GUI.Label(new Rect(ix, ry + 36, w - 130, 16), statusLine, statusStyle);

                // Fatigue bar (22 px wide bar at right of text)
                DrawMiniBar(w - 130, ry + 20, 120, 10, unit.FatigueLevel, 100, FatigueBarColour(unit.FatigueLevel));

                // Dismiss button
                int dismissX = w - 16 - BtnW - 4;
                if (GUI.Button(new Rect(dismissX, iy + (RowH / 2) - BtnH / 2, BtnW, BtnH),
                    broken ? "Dismiss\n(BROKEN)" : "Dismiss", _btnStyle))
                {
                    roster.Dismiss(i);
                    GUIUtility.hotControl = 0;
                    break; // list mutated — skip rest of frame
                }

                iy += RowH;
            }

            GUI.EndScrollView();
        }

        // ════════════════════════════════════════════
        // RIGHT — Recruit Catalog
        // ════════════════════════════════════════════

        private void DrawCatalogColumn(int x, int y, int w, int h, RosterManager roster)
        {
            GUI.Label(new Rect(x, y, w, 22), "AVAILABLE RECRUITS", _titleStyle);
            y += 28;
            h -= 28;

            if (roster == null || roster.RecruitCatalog == null || roster.RecruitCatalog.Length == 0)
            {
                GUI.Label(new Rect(x, y + 20, w, 22),
                    "No recruits available.\n(Add UnitData assets to RosterManager.recruitCatalog)", _subStyle);
                return;
            }

            Rect viewRect    = new Rect(x, y, w, h);
            Rect contentRect = new Rect(0, 0, w - 16, roster.RecruitCatalog.Length * RowH);
            _catalogScroll = GUI.BeginScrollView(viewRect, _catalogScroll, contentRect);

            int iy = 0;
            foreach (var unit in roster.RecruitCatalog)
            {
                if (unit == null) continue;

                bool inRoster  = roster.IsInRoster(unit);
                bool broken    = FatigueSystem.IsUndeployable(unit);
                bool canHire   = roster.CanRecruit(unit);
                bool rosterFull = roster.RosterCount >= roster.MaxSlots;

                // Row background — dimmer if unavailable
                GUI.color = inRoster
                    ? new Color(0.1f, 0.35f, 0.15f, 0.5f)
                    : new Color(0.15f, 0.2f, 0.3f, 0.5f);
                GUI.DrawTexture(new Rect(0, iy, w - 16, RowH - 4), Texture2D.whiteTexture);
                GUI.color = Color.white;

                int ix = 8;
                int ry = iy + 8;

                // Name + class
                GUI.Label(new Rect(ix, ry, w - 130, 20), unit.UnitName, _labelStyle);
                GUI.Label(new Rect(ix, ry + 20, w - 130, 16),
                    $"{CapFirst(unit.UnitType)}  |  HP {unit.MaxHP}  ATK {unit.AttackDamage}  San {unit.BaseSanity}", _subStyle);

                // Cost line
                string cost = $"{unit.GoldCost}G";
                if (unit.FoodCost     > 0) cost += $" + {unit.FoodCost}F";
                if (unit.MaterialCost > 0) cost += $" + {unit.MaterialCost}M";
                GUI.Label(new Rect(ix, ry + 36, w - 130, 16), cost, _subStyle);

                // Hire / status button
                int btnX = w - 16 - BtnW - 4;

                if (inRoster)
                {
                    GUI.Label(new Rect(btnX, iy + (RowH / 2) - 10, BtnW, 20), "IN ROSTER", _greenStyle);
                }
                else if (broken)
                {
                    GUI.enabled = false;
                    GUI.Button(new Rect(btnX, iy + (RowH / 2) - BtnH / 2, BtnW, BtnH), "BROKEN", _dimBtnStyle);
                    GUI.enabled = true;
                }
                else if (rosterFull)
                {
                    GUI.enabled = false;
                    GUI.Button(new Rect(btnX, iy + (RowH / 2) - BtnH / 2, BtnW, BtnH), "FULL", _dimBtnStyle);
                    GUI.enabled = true;
                }
                else
                {
                    GUI.enabled = canHire;
                    if (GUI.Button(new Rect(btnX, iy + (RowH / 2) - BtnH / 2, BtnW, BtnH), "Hire", _btnStyle))
                    {
                        roster.Recruit(unit);
                        GUIUtility.hotControl = 0;
                    }
                    GUI.enabled = true;

                    if (!canHire && !rosterFull)
                    {
                        GUI.color = new Color(1f, 0.4f, 0.4f, 0.9f);
                        GUI.Label(new Rect(btnX - 60, iy + (RowH / 2) + 6, 56, 14), "Can't afford", _subStyle);
                        GUI.color = Color.white;
                    }
                }

                iy += RowH;
            }

            GUI.EndScrollView();
        }

        // ════════════════════════════════════════════
        // STYLE HELPERS
        // ════════════════════════════════════════════

        private void EnsureStyles()
        {
            if (_stylesReady) return;
            _stylesReady = true;

            _panelStyle = new GUIStyle(GUI.skin.box);
            _panelStyle.normal.background = MakeTex(new Color(0.08f, 0.10f, 0.14f, 0.97f));

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 14,
                fontStyle = FontStyle.Bold,
            };
            _titleStyle.normal.textColor = new Color(0.85f, 0.80f, 0.65f);

            _labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 12 };
            _labelStyle.normal.textColor = new Color(0.90f, 0.90f, 0.90f);

            _subStyle = new GUIStyle(GUI.skin.label) { fontSize = 10 };
            _subStyle.normal.textColor = new Color(0.65f, 0.65f, 0.70f);

            _warnStyle = new GUIStyle(_subStyle);
            _warnStyle.normal.textColor = new Color(1f, 0.4f, 0.3f);

            _greenStyle = new GUIStyle(_labelStyle);
            _greenStyle.normal.textColor = new Color(0.4f, 0.9f, 0.5f);
            _greenStyle.fontStyle = FontStyle.Bold;

            _btnStyle = new GUIStyle(GUI.skin.button) { fontSize = 11 };
            _btnStyle.normal.textColor    = Color.white;
            _btnStyle.hover.textColor     = Color.white;
            _btnStyle.focused.textColor   = Color.white;

            _dimBtnStyle = new GUIStyle(_btnStyle);
            _dimBtnStyle.normal.textColor = new Color(0.5f, 0.5f, 0.5f);
        }

        private static Texture2D MakeTex(Color c)
        {
            var t = new Texture2D(1, 1);
            t.SetPixel(0, 0, c);
            t.Apply();
            return t;
        }

        private void DrawMiniBar(int x, int y, int w, int h, int val, int max, Color col)
        {
            GUI.color = new Color(0.1f, 0.1f, 0.12f);
            GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);
            if (max > 0 && val > 0)
            {
                GUI.color = col;
                GUI.DrawTexture(new Rect(x, y, w * ((float)val / max), h), Texture2D.whiteTexture);
            }
            GUI.color = Color.white;
        }

        // ════════════════════════════════════════════
        // FORMATTING HELPERS
        // ════════════════════════════════════════════

        private static string FatigueDescription(int f) => f switch
        {
            0           => "Rested",
            < 50        => $"Rested ({f}/100)",
            < 80        => $"Weary ({f}/100)",
            < 100       => $"Exhausted ({f}/100)",
            _           => "BROKEN — must rest"
        };

        private static Color FatigueBarColour(int f) => f switch
        {
            < 50  => new Color(0.3f, 0.8f, 0.4f),
            < 80  => new Color(0.85f, 0.75f, 0.2f),
            < 100 => new Color(0.9f, 0.4f, 0.1f),
            _     => new Color(0.8f, 0.1f, 0.1f)
        };

        private static string PhobiaShort(PhobiaType p) => p switch
        {
            PhobiaType.BloodPhobia     => "Phobia: Blood",
            PhobiaType.EldritchPhobia  => "Phobia: Eldritch",
            PhobiaType.SolitudePhobia  => "Phobia: Solitude",
            PhobiaType.ViolencePhobia  => "Phobia: Violence",
            PhobiaType.DarkPhobia      => "Phobia: Dark",
            _                          => ""
        };

        private static string CapFirst(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return char.ToUpper(s[0]) + s.Substring(1);
        }
    }
}
