using System.Linq;
using UnityEngine;
using KindredSiege.Battle;
using KindredSiege.City;
using KindredSiege.Core;

namespace KindredSiege.UI
{
    /// <summary>
    /// City-Phase Rest Panel (GDD §11.4 city side).
    ///
    /// Shown between battles so the player can rest fatigued units and partially
    /// recover Forbidden Knowledge MaxSanity penalties. Spending resources here is
    /// the only way to remove fatigue before the next expedition.
    ///
    /// REST OPTIONS per unit:
    ///   Light Rest  — 10 Gold  → −20 Fatigue
    ///   Full Rest   — 25 Gold  → −50 Fatigue  +  −2 MaxSanityPenalty (Apothecary care)
    ///
    /// Units with Fatigue ≥ 100 are marked BROKEN and cannot deploy until rested.
    /// Units with MaxSanityPenalty > 0 show a warning in the panel.
    ///
    /// Uses OnGUI — no Canvas prefab required.
    /// Attach to a persistent Manager GameObject in the city/map scene.
    /// Call Show() after battle resolution; "Deploy" closes the panel.
    /// </summary>
    public class CityRestPanel : MonoBehaviour
    {
        public static CityRestPanel Instance { get; private set; }

        // ─── Config ───
        private const int LightRestCost           = 10;  // Gold
        private const int LightRestAmount         = 20;  // Fatigue removed
        private const int FullRestCost            = 25;  // Gold
        private const int TreatmentCost           = 300; // Gold
        private const int TreatmentKPCost         = 2;   // Kindness Points
        private const int FullRestAmount          = 50;  // Fatigue removed
        private const int FullRestFKReduce        = 2;   // MaxSanityPenalty reduced

        // ─── State ───
        private bool      _visible;
        private UnitData[] _roster;   // Set by Show(roster)

        // ─── Layout ───
        private const int PanelW  = 720;
        private const int RowH    = 72;
        private const int Margin  = 16;
        private const int LabelW  = 200;
        private const int BtnW    = 130;
        private const int BtnH    = 30;

        // ─── Styles ───
        private GUIStyle _panelStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _subStyle;
        private GUIStyle _warnStyle;
        private GUIStyle _brokenStyle;
        private GUIStyle _buttonStyle;
        private bool     _stylesReady;

        // ════════════════════════════════════════════
        // LIFECYCLE
        // ════════════════════════════════════════════

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // ════════════════════════════════════════════
        // PUBLIC API
        // ════════════════════════════════════════════

        /// <summary>Show the rest panel for a given unit roster.</summary>
        public void Show(UnitData[] roster)
        {
            _roster  = roster;
            _visible = true;
        }

        /// <summary>Convenience overload — pulls team 1 directly from BattleManager.</summary>
        public void Show()
        {
            _roster  = BattleManager.Instance != null ? BattleManager.Instance.GetTeam1Units() : null;
            _visible = true;
        }

        public void Hide() => _visible = false;

        // ════════════════════════════════════════════
        // OnGUI
        // ════════════════════════════════════════════

        private void OnGUI()
        {
            if (!_visible) return;
            EnsureStyles();

            int unitCount = _roster != null ? _roster.Length : 0;
            int panelH    = Margin * 3 + 50 + RowH * Mathf.Max(unitCount, 1) + 60;
            int panelX    = (Screen.width  - PanelW) / 2;
            int panelY    = (Screen.height - panelH) / 2;

            GUI.Box(new Rect(panelX, panelY, PanelW, panelH), GUIContent.none, _panelStyle);

            int ix = panelX + Margin;
            int iy = panelY + Margin;
            int lw = PanelW - Margin * 2;

            // ─── Title + Gold ───────────────────────────────────────────────
            GUI.Label(new Rect(ix, iy, lw, 28), "CITY — REST & RECOVERY", _titleStyle);

            int gold = ResourceManager.Instance != null
                ? ResourceManager.Instance.GetAmount(ResourceType.Gold) : 0;
            GUI.Label(new Rect(panelX + PanelW - 180, iy, 160, 28),
                $"Gold: {gold}", _labelStyle);
            iy += 32;

            string mythosStr = KindredSiege.City.MythosExposure.Instance != null
                ? $"  Mythos: {KindredSiege.City.MythosExposure.Instance.Exposure}/100 [{KindredSiege.City.MythosExposure.Instance.GetTierName()}]"
                : "";
            GUI.Label(new Rect(ix, iy, lw, 18),
                $"Light Rest: {LightRestCost}G  (−{LightRestAmount} fatigue)     " +
                $"Full Rest: {FullRestCost}G  (−{FullRestAmount} fatigue, −{FullRestFKReduce} MaxSanity penalty)" +
                mythosStr,
                _subStyle);
            iy += 22;

            // ─── Apothecary check ───
            int apothecaryLvl = 0;
            if (CityManager.Instance != null)
            {
                var apo = CityManager.Instance.PlacedBuildings.FirstOrDefault(b => b.Data != null && b.Data.BuildingName == "Apothecary");
                apothecaryLvl = apo != null ? apo.Level : 0;
            }

            // ─── Unit rows ──────────────────────────────────────────────────
            if (unitCount == 0)
            {
                GUI.Label(new Rect(ix, iy, lw, 24),
                    "No units found. Assign team1Units in BattleManager.", _subStyle);
                iy += 30;
            }
            else
            {
                for (int i = 0; i < unitCount; i++)
                {
                    var data = _roster![i];
                    if (data == null) { iy += RowH; continue; }
                    DrawUnitRow(ix, iy, data, gold, apothecaryLvl);
                    iy += RowH;
                }
            }

            iy += Margin;

            // ─── Deploy button ───────────────────────────────────────────────
            if (GUI.Button(new Rect(panelX + (PanelW - 200) / 2, iy, 200, 40), "DEPLOY EXPEDITION", _buttonStyle))
                Hide();
        }

        // ─── Single unit row ─────────────────────────────────────────────────

        private void DrawUnitRow(int ix, int rowY, UnitData data, int currentGold, int apothecaryLvl)
        {
            // Name + class
            GUI.Label(new Rect(ix, rowY + 4,  LabelW, 20), data.UnitName, _labelStyle);
            GUI.Label(new Rect(ix, rowY + 24, LabelW, 16), $"[{data.UnitType}]", _subStyle);

            // Fatigue bar + description
            int barX    = ix + LabelW + 8;
            int barW    = 180;
            DrawFatigueBar(barX, rowY + 8, barW, data.FatigueLevel);

            string fatigueDesc = FatigueSystem.DescribeFatigue(data.FatigueLevel);
            GUI.Label(new Rect(barX, rowY + 32, barW, 16), fatigueDesc, _subStyle);

            // Forbidden Knowledge / phobia info
            int infoX = barX + barW + 8;
            int infoW = 140;

            if (data.MaxSanityPenalty > 0)
            {
                GUI.Label(new Rect(infoX, rowY + 4, infoW, 18),
                    $"MaxSanity: {data.BaseSanity - data.MaxSanityPenalty}/{data.BaseSanity}", _warnStyle);
                GUI.Label(new Rect(infoX, rowY + 22, infoW, 16),
                    $"FK penalty: −{data.MaxSanityPenalty}", _warnStyle);
            }

            if (data.ActivePhobia != PhobiaType.None)
                GUI.Label(new Rect(infoX, rowY + 44, infoW, 16),
                    $"Phobia: {data.ActivePhobia}", _warnStyle);

            // Broken warning (fatigue 100) — no rest buttons, just warning
            if (FatigueSystem.IsUndeployable(data))
            {
                GUI.Label(new Rect(ix + PanelW - Margin * 2 - 200, rowY + 20, 200, 24),
                    "BROKEN — must rest before deploying", _brokenStyle);
            }

            // Rest & Treat buttons
            int buttonsNeeded = data.ActivePhobia != PhobiaType.None ? 3 : 2;
            int btnX = ix + PanelW - Margin * 2 - BtnW * buttonsNeeded - 8 * (buttonsNeeded - 1);

            // Treatment button (if phobia exists)
            if (data.ActivePhobia != PhobiaType.None)
            {
                if (apothecaryLvl >= 3)
                {
                    int currentKP = ResourceManager.Instance?.GetAmount(ResourceType.KindnessPoints) ?? 0;
                    bool canTreat = currentGold >= TreatmentCost && currentKP >= TreatmentKPCost;
                    GUI.enabled = canTreat;
                    if (GUI.Button(new Rect(btnX, rowY + 10, BtnW, BtnH + 10), $"Treat Phobia\n({TreatmentCost}G, {TreatmentKPCost}KP)", _buttonStyle))
                    {
                        if (ResourceManager.Instance != null &&
                            ResourceManager.Instance.GetAmount(ResourceType.Gold) >= TreatmentCost &&
                            ResourceManager.Instance.GetAmount(ResourceType.KindnessPoints) >= TreatmentKPCost)
                        {
                            ResourceManager.Instance.Spend(ResourceType.Gold, TreatmentCost);
                            ResourceManager.Instance.Spend(ResourceType.KindnessPoints, TreatmentKPCost);
                            TraumaPhobiaSystem.CurePhobia(data);
                        }
                    }
                    GUI.enabled = true;
                }
                else
                {
                    GUI.Label(new Rect(btnX, rowY + 20, BtnW, BtnH), "Requires\nApothecary L3", _subStyle);
                }
                btnX += BtnW + 8;
            }

            // Light Rest button
            bool canLight = currentGold >= LightRestCost && data.FatigueLevel > 0;
            GUI.enabled = canLight;
            if (GUI.Button(new Rect(btnX, rowY + 20, BtnW, BtnH),
                $"Light Rest\n({LightRestCost}G)", _buttonStyle))
            {
                if (ResourceManager.Instance != null &&
                    ResourceManager.Instance.Spend(ResourceType.Gold, LightRestCost))
                {
                    FatigueSystem.Rest(data, LightRestAmount);
                    currentGold -= LightRestCost;
                    Debug.Log($"[CityRest] {data.UnitName}: Light Rest — fatigue now {data.FatigueLevel}.");
                }
            }
            GUI.enabled = true;

            // Full Rest button
            bool canFull = currentGold >= FullRestCost && (data.FatigueLevel > 0 || data.MaxSanityPenalty > 0);
            GUI.enabled = canFull;
            if (GUI.Button(new Rect(btnX + BtnW + 8, rowY + 20, BtnW, BtnH),
                $"Full Rest\n({FullRestCost}G)", _buttonStyle))
            {
                if (ResourceManager.Instance != null &&
                    ResourceManager.Instance.Spend(ResourceType.Gold, FullRestCost))
                {
                    FatigueSystem.Rest(data, FullRestAmount);

                    if (data.MaxSanityPenalty > 0)
                    {
                        int recovered = Mathf.Min(FullRestFKReduce, data.MaxSanityPenalty);
                        data.MaxSanityPenalty = Mathf.Max(0, data.MaxSanityPenalty - recovered);
                        Debug.Log($"[CityRest] {data.UnitName}: Apothecary recovered {recovered} MaxSanity penalty → {data.MaxSanityPenalty} remaining.");
                    }

                    Debug.Log($"[CityRest] {data.UnitName}: Full Rest — fatigue now {data.FatigueLevel}, FK penalty {data.MaxSanityPenalty}.");
                }
            }
            GUI.enabled = true;
        }

        // ─── Fatigue bar (coloured background fill) ───────────────────────────

        private void DrawFatigueBar(int x, int y, int w, int fatigueLevel)
        {
            int h = 18;

            // Background
            GUI.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);
            GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);

            // Fill
            float ratio = Mathf.Clamp01(fatigueLevel / 100f);
            Color fill  = fatigueLevel >= 80 ? new Color(0.85f, 0.15f, 0.10f)   // Exhausted — red
                        : fatigueLevel >= 50 ? new Color(0.90f, 0.60f, 0.05f)   // Weary — amber
                        :                      new Color(0.15f, 0.75f, 0.25f);  // Rested — green

            GUI.color = fill;
            GUI.DrawTexture(new Rect(x, y, w * ratio, h), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Label
            GUI.Label(new Rect(x + 4, y, w - 4, h),
                $"Fatigue: {fatigueLevel}/100", _subStyle);
        }

        // ════════════════════════════════════════════
        // STYLES
        // ════════════════════════════════════════════

        private void EnsureStyles()
        {
            if (_stylesReady) return;

            var panelTex = new Texture2D(1, 1);
            panelTex.SetPixel(0, 0, new Color(0.04f, 0.04f, 0.09f, 0.95f));
            panelTex.Apply();

            _panelStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = panelTex },
                border = new RectOffset(4, 4, 4, 4)
            };

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 17,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal    = { textColor = new Color(0.95f, 0.90f, 1.0f) }
            };

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 13,
                fontStyle = FontStyle.Bold,
                normal    = { textColor = new Color(0.90f, 0.88f, 0.95f) }
            };

            _subStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 11,
                normal    = { textColor = new Color(0.65f, 0.62f, 0.72f) }
            };

            _warnStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 11,
                fontStyle = FontStyle.Bold,
                normal    = { textColor = new Color(1.0f, 0.65f, 0.15f) }
            };

            _brokenStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = new Color(0.9f, 0.1f, 0.1f) }
            };

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize  = 12,
                fontStyle = FontStyle.Bold,
                normal    = { textColor = Color.white }
            };

            _stylesReady = true;
        }
    }
}
