using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KindredSiege.City;
using KindredSiege.Core;

namespace KindredSiege.UI
{
    /// <summary>
    /// Visual representation of the Drowned City as a district-based grid.
    ///
    /// Renders a top-down map of 5 districts arranged in a cross pattern:
    ///
    ///             [ Scholars' Quarter ]
    ///   [ Charity ]   [ Harbor ]   [ Military ]
    ///               [ The Abyss ]
    ///
    /// Each district shows its buildings as colored cells.
    /// Locked districts are grayed out with an unlock hint.
    /// Liberated districts have a golden border.
    ///
    /// Drawn inside the CityHUD centre panel area.
    /// Call DrawGrid(x, y, w, h) from CityHUD.DrawCentrePanel().
    /// </summary>
    public static class CityGridPanel
    {
        // ─── Layout ───
        private const int CellSize     = 52;
        private const int CellPad      = 4;
        private const int DistrictPad  = 8;
        private const int LabelH       = 18;
        private const int BuildingH    = 44;

        // ─── District colors ───
        private static readonly Color HarborColor    = new(0.20f, 0.45f, 0.65f);
        private static readonly Color MilitaryColor  = new(0.55f, 0.25f, 0.20f);
        private static readonly Color CharityColor   = new(0.30f, 0.55f, 0.35f);
        private static readonly Color ScholarsColor  = new(0.50f, 0.40f, 0.65f);
        private static readonly Color AbyssColor     = new(0.20f, 0.12f, 0.30f);
        private static readonly Color LockedColor    = new(0.18f, 0.18f, 0.22f);
        private static readonly Color LiberatedGold  = new(0.85f, 0.72f, 0.25f);
        private static readonly Color EmptySlotColor = new(0.10f, 0.10f, 0.14f);

        // ─── Styles (lazy init) ───
        private static bool     _stylesReady;
        private static GUIStyle _districtLabel;
        private static GUIStyle _buildingLabel;
        private static GUIStyle _levelLabel;
        private static GUIStyle _hintLabel;
        private static GUIStyle _statusLabel;

        // District grid positions (col, row) in a 3x3 grid
        // Scholars at top-center, Charity left, Harbor center, Military right, Abyss bottom-center
        private static readonly Dictionary<DistrictType, Vector2Int> GridPos = new()
        {
            { DistrictType.ScholarsQuarter, new Vector2Int(1, 0) },
            { DistrictType.CharityQuarter,  new Vector2Int(0, 1) },
            { DistrictType.Harbor,          new Vector2Int(1, 1) },
            { DistrictType.MilitaryWard,    new Vector2Int(2, 1) },
            { DistrictType.TheAbyss,        new Vector2Int(1, 2) },
        };

        // Buildings per district (max slots visual)
        private static readonly Dictionary<DistrictType, int> MaxSlots = new()
        {
            { DistrictType.Harbor,          2 },
            { DistrictType.MilitaryWard,    3 },
            { DistrictType.CharityQuarter,  2 },
            { DistrictType.ScholarsQuarter, 3 },
            { DistrictType.TheAbyss,        2 },
        };

        /// <summary>
        /// Draw the visual city grid. Returns the height consumed.
        /// </summary>
        public static int DrawGrid(int areaX, int areaY, int areaW)
        {
            EnsureStyles();

            var dm   = DistrictManager.Instance;
            var cm   = CityManager.Instance;
            if (dm == null) return 0;

            // Compute district cell dimensions
            int districtW = (areaW - DistrictPad * 4) / 3;
            int districtH = LabelH + BuildingH + CellPad * 2 + 18; // label + buildings + status

            int gridW = districtW * 3 + DistrictPad * 2;
            int startX = areaX + (areaW - gridW) / 2;

            int totalH = 0;

            // Title
            GUI.color = new Color(0.70f, 0.75f, 0.85f);
            GUI.Label(new Rect(areaX, areaY, areaW, 20), "THE DROWNED CITY", _districtLabel);
            GUI.color = Color.white;
            totalH += 24;

            foreach (var kvp in GridPos)
            {
                DistrictType dtype = kvp.Key;
                Vector2Int   gpos  = kvp.Value;

                int dx = startX + gpos.x * (districtW + DistrictPad);
                int dy = areaY + 24 + gpos.y * (districtH + DistrictPad);

                bool unlocked  = dm.IsUnlocked(dtype);
                bool liberated = dm.IsLiberated(dtype);

                // District background
                Color bgColor = unlocked ? GetDistrictColor(dtype) : LockedColor;
                GUI.color = new Color(bgColor.r, bgColor.g, bgColor.b, 0.35f);
                GUI.DrawTexture(new Rect(dx, dy, districtW, districtH), Texture2D.whiteTexture);

                // Liberated border
                if (liberated)
                {
                    GUI.color = new Color(LiberatedGold.r, LiberatedGold.g, LiberatedGold.b, 0.8f);
                    // Top
                    GUI.DrawTexture(new Rect(dx, dy, districtW, 2), Texture2D.whiteTexture);
                    // Bottom
                    GUI.DrawTexture(new Rect(dx, dy + districtH - 2, districtW, 2), Texture2D.whiteTexture);
                    // Left
                    GUI.DrawTexture(new Rect(dx, dy, 2, districtH), Texture2D.whiteTexture);
                    // Right
                    GUI.DrawTexture(new Rect(dx + districtW - 2, dy, 2, districtH), Texture2D.whiteTexture);
                }
                else if (unlocked)
                {
                    // Subtle border
                    GUI.color = new Color(bgColor.r, bgColor.g, bgColor.b, 0.6f);
                    GUI.DrawTexture(new Rect(dx, dy, districtW, 1), Texture2D.whiteTexture);
                    GUI.DrawTexture(new Rect(dx, dy + districtH - 1, districtW, 1), Texture2D.whiteTexture);
                    GUI.DrawTexture(new Rect(dx, dy, 1, districtH), Texture2D.whiteTexture);
                    GUI.DrawTexture(new Rect(dx + districtW - 1, dy, 1, districtH), Texture2D.whiteTexture);
                }

                GUI.color = Color.white;

                // District name
                string dname = DistrictManager.GetName(dtype);
                Color labelColor = unlocked ? GetDistrictColor(dtype) : new Color(0.40f, 0.40f, 0.48f);
                GUI.color = labelColor;
                GUI.Label(new Rect(dx + 4, dy + 2, districtW - 8, LabelH), dname, _districtLabel);
                GUI.color = Color.white;

                if (!unlocked)
                {
                    // Locked overlay
                    string hint = DistrictManager.GetUnlockHint(dtype);
                    GUI.color = new Color(0.45f, 0.45f, 0.55f);
                    GUI.Label(new Rect(dx + 4, dy + LabelH + 6, districtW - 8, 32), hint, _hintLabel);
                    GUI.color = Color.white;
                }
                else
                {
                    // Building slots
                    DrawBuildingSlots(dx, dy + LabelH + 2, districtW, dtype, cm);

                    // Liberation status
                    if (liberated)
                    {
                        GUI.color = LiberatedGold;
                        GUI.Label(new Rect(dx + 4, dy + districtH - 16, districtW - 8, 14),
                            "LIBERATED", _statusLabel);
                        GUI.color = Color.white;
                    }
                }

                // Track max Y
                int endY = dy + districtH - areaY;
                if (endY > totalH) totalH = endY;
            }

            return totalH + 4;
        }

        private static void DrawBuildingSlots(int dx, int dy, int districtW,
            DistrictType dtype, CityManager cm)
        {
            int maxSlots = MaxSlots.GetValueOrDefault(dtype, 2);
            int slotW = (districtW - CellPad * 2 - (maxSlots - 1) * CellPad) / maxSlots;
            slotW = Mathf.Min(slotW, CellSize);

            // Get placed buildings for this district
            var buildings = cm?.PlacedBuildings?
                .Where(b => b.Data != null && b.Data.District == dtype)
                .ToList() ?? new List<CityManager.PlacedBuilding>();

            int sx = dx + CellPad;
            for (int i = 0; i < maxSlots; i++)
            {
                int cellX = sx + i * (slotW + CellPad);

                if (i < buildings.Count)
                {
                    var b = buildings[i];
                    Color bColor = GetDistrictColor(dtype);

                    // Filled building cell — brightness by level
                    float levelBright = 0.5f + b.Level * 0.15f;
                    GUI.color = new Color(
                        bColor.r * levelBright,
                        bColor.g * levelBright,
                        bColor.b * levelBright, 0.85f);
                    GUI.DrawTexture(new Rect(cellX, dy, slotW, BuildingH), Texture2D.whiteTexture);

                    // Building name (truncated)
                    GUI.color = Color.white;
                    string shortName = TruncateName(b.Data.BuildingName, slotW);
                    GUI.Label(new Rect(cellX + 2, dy + 2, slotW - 4, 16), shortName, _buildingLabel);

                    // Level stars
                    string stars = new string('*', b.Level);
                    GUI.color = new Color(0.95f, 0.85f, 0.40f);
                    GUI.Label(new Rect(cellX + 2, dy + 18, slotW - 4, 14), stars, _levelLabel);

                    // Production hint
                    if (b.Data.ProductionAmount > 0)
                    {
                        int prod = Mathf.RoundToInt(b.Data.ProductionAmount * b.ProductionMultiplier);
                        string resName = b.Data.ProducesResource.ToString().Substring(0, 3);
                        GUI.color = new Color(0.75f, 0.75f, 0.80f);
                        GUI.Label(new Rect(cellX + 2, dy + 30, slotW - 4, 12),
                            $"+{prod} {resName}", _levelLabel);
                    }
                    GUI.color = Color.white;
                }
                else
                {
                    // Empty slot
                    GUI.color = EmptySlotColor;
                    GUI.DrawTexture(new Rect(cellX, dy, slotW, BuildingH), Texture2D.whiteTexture);
                    GUI.color = new Color(0.30f, 0.30f, 0.38f);
                    GUI.Label(new Rect(cellX + 2, dy + 14, slotW - 4, 16), "Empty", _buildingLabel);
                    GUI.color = Color.white;
                }
            }
        }

        private static Color GetDistrictColor(DistrictType dtype) => dtype switch
        {
            DistrictType.Harbor          => HarborColor,
            DistrictType.MilitaryWard    => MilitaryColor,
            DistrictType.CharityQuarter  => CharityColor,
            DistrictType.ScholarsQuarter => ScholarsColor,
            DistrictType.TheAbyss        => AbyssColor,
            _                            => LockedColor
        };

        private static string TruncateName(string name, int slotW)
        {
            // Rough character limit based on slot width at ~7px per char
            int maxChars = Mathf.Max(4, slotW / 7);
            if (name.Length <= maxChars) return name;
            return name.Substring(0, maxChars - 1) + ".";
        }

        private static void EnsureStyles()
        {
            if (_stylesReady) return;
            _stylesReady = true;

            _districtLabel = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = Color.white }
            };

            _buildingLabel = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 9,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperLeft,
                clipping  = TextClipping.Clip,
                normal    = { textColor = new Color(0.95f, 0.95f, 1.0f) }
            };

            _levelLabel = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 9,
                alignment = TextAnchor.UpperLeft,
                normal    = { textColor = new Color(0.95f, 0.85f, 0.40f) }
            };

            _hintLabel = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 9,
                wordWrap  = true,
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = new Color(0.50f, 0.50f, 0.58f) }
            };

            _statusLabel = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 8,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = new Color(0.85f, 0.72f, 0.25f) }
            };
        }
    }
}
