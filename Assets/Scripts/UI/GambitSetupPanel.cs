using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KindredSiege.Battle;
using KindredSiege.AI.BehaviourTree;
using KindredSiege.Core;
using KindredSiege.City;

namespace KindredSiege.UI
{
    /// <summary>
    /// Pre-Battle Gambit Setup Panel (GDD §4.1)
    ///
    /// Shown before each expedition. The player assigns up to two Pre-Built Gambits
    /// to each unit slot. Gambits are filtered by unit class so only relevant options
    /// appear. Clicking "Begin Expedition" applies selections and starts the battle.
    ///
    /// Gambit injection happens after BattleManager spawns the units, via
    /// ApplyGambitsToTeam(). BattleManager calls this automatically.
    ///
    /// Uses OnGUI — no Canvas prefab required.
    /// Attach to the BattleArena GameObject alongside BattleManager.
    /// </summary>
    public class GambitSetupPanel : MonoBehaviour
    {
        public static GambitSetupPanel Instance { get; private set; }

        // ─── State ───
        private bool         _visible = false;
        private BattleManager _battle;

        // Per-slot gambit selections (up to 8 unit slots)
        private readonly GambitType[] _slot1 = new GambitType[8];
        private readonly GambitType[] _slot2 = new GambitType[8];

        // ─── Layout ───
        private const int PanelW   = 680;
        private const int RowH     = 60;
        private const int Margin   = 16;
        private const int LabelW   = 140;
        private const int CyclerW  = 230;
        private const int ArrowW   = 24;
        private const int RowBtnH  = 30;

        // ─── Styles ───
        private GUIStyle _panelStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _subStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _cyclerStyle;
        private GUIStyle _fatigueStyle;
        private bool     _stylesReady;

        // ════════════════════════════════════════════
        // LIFECYCLE
        // ════════════════════════════════════════════

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            _battle = BattleManager.Instance;
            if (GameManager.Instance != null)
                GameManager.Instance.OnStateChanged += OnGameStateChanged;
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnStateChanged -= OnGameStateChanged;
        }

        private void OnGameStateChanged(GameManager.GameState from, GameManager.GameState to)
        {
            if (to == GameManager.GameState.PreBattle)
                Show();
        }

        // ════════════════════════════════════════════
        // PUBLIC API
        // ════════════════════════════════════════════

        /// <summary>Show the panel (called between battles or at scene start).</summary>
        public void Show() => _visible = true;

        /// <summary>Hide the panel after "Begin Expedition" is clicked.</summary>
        public void Hide() => _visible = false;

        /// <summary>
        /// Apply stored gambit selections to a spawned team.
        /// Called by BattleManager immediately after SpawnTeam() for team 1.
        /// </summary>
        public void ApplyGambitsToTeam(List<UnitController> team)
        {
            for (int i = 0; i < team.Count && i < _slot1.Length; i++)
            {
                var unit = team[i];
                if (unit == null) continue;

                BTNode g1 = _slot1[i] != GambitType.None ? GambitLibrary.GetGambit(_slot1[i]) : null;
                BTNode g2 = _slot2[i] != GambitType.None ? GambitLibrary.GetGambit(_slot2[i]) : null;

                // VOID: The Rival Knows (First gambit automatically fails)
                if (KindredSiege.Modifiers.MutationEngine.Instance != null &&
                    KindredSiege.Modifiers.MutationEngine.Instance.HasMutation(KindredSiege.Modifiers.MutationType.TheRivalKnows))
                {
                    g1 = null;
                }

                unit.SetGambits(g1, g2);

                if (g1 != null || g2 != null)
                    Debug.Log($"[Gambits] {unit.UnitName}: P1={_slot1[i]} | P2={_slot2[i]}");
            }
        }

        // ════════════════════════════════════════════
        // OnGUI
        // ════════════════════════════════════════════

        private void OnGUI()
        {
            if (!_visible) return;
            EnsureStyles();

            if (_battle == null) _battle = BattleManager.Instance;

            var units = _battle?.GetTeam1Units();

            // Calculate panel height from unit count
            int unitCount = units != null ? units.Length : 0;
            int panelH    = Margin * 3 + 40 + RowH * Mathf.Max(unitCount, 1) + 60;
            int panelX    = (Screen.width  - PanelW) / 2;
            int panelY    = (Screen.height - panelH) / 2;

            GUI.Box(new Rect(panelX, panelY, PanelW, panelH), GUIContent.none, _panelStyle);

            int iy = panelY + Margin;
            int ix = panelX + Margin;
            int lw = PanelW - Margin * 2;

            // Title
            GUI.Label(new Rect(ix, iy, lw, 32), "⚔  ASSIGN PRE-BATTLE GAMBITS", _titleStyle);
            iy += 36;

            if (unitCount == 0)
            {
                GUI.Label(new Rect(ix, iy, lw, 24),
                    "No units assigned. Configure team1Units in BattleManager.", _subStyle);
                iy += 30;
            }
            else
            {
                // Column headers
                GUI.Label(new Rect(ix + LabelW + 10, iy, CyclerW, 18), "Priority 1 Gambit", _subStyle);
                GUI.Label(new Rect(ix + LabelW + 10 + CyclerW + 16, iy, CyclerW, 18), "Priority 2 Gambit", _subStyle);
                iy += 20;

                for (int i = 0; i < unitCount; i++)
                {
                    var data = units![i];
                    if (data == null) { iy += RowH; continue; }

                    DrawUnitRow(ix, iy, i, data);
                    iy += RowH;
                }
            }

            iy += Margin;

            // Begin Expedition button
            int btnW = 220;
            if (GUI.Button(new Rect(panelX + (PanelW - btnW) / 2, iy, btnW, 40),
                "BEGIN EXPEDITION", _buttonStyle))
            {
                Hide();
                if (GameManager.Instance != null)
                    GameManager.Instance.LaunchBattle();
                else
                    _battle?.StartBattle();
            }
        }

        // ─── Unit row: name/class label + two gambit cyclers ──────────────────

        private void DrawUnitRow(int ix, int rowY, int slotIdx, UnitData data)
        {
            // Name and class
            GUI.Label(new Rect(ix, rowY + 6,  LabelW, 20), data.UnitName, _labelStyle);
            GUI.Label(new Rect(ix, rowY + 26, LabelW, 18), $"[{data.UnitType}]", _subStyle);

            // Fatigue indicator
            if (data.FatigueLevel >= 50)
            {
                string fatTxt = data.FatigueLevel >= 80 ? "EXHAUSTED" : "WEARY";
                GUI.Label(new Rect(ix, rowY + 42, LabelW, 16), fatTxt, _fatigueStyle);
            }

            // Find Archive level
            int archiveLevel = 2; // Default for testing without CityManager
            if (CityManager.Instance != null)
            {
                var arch = CityManager.Instance.PlacedBuildings.FirstOrDefault(b => b.Data != null && b.Data.BuildingName == "Archive");
                archiveLevel = arch != null ? arch.Level : 0;
            }

            int g1x = ix + LabelW + 10;
            if (archiveLevel >= 1)
                DrawGambitCycler(g1x, rowY + 16, slotIdx, data.UnitType, isSlot1: true);
            else
                GUI.Label(new Rect(g1x, rowY + 16, CyclerW, 30), "Requires Archive L1", _subStyle);

            int g2x = g1x + CyclerW + 16;
            if (archiveLevel >= 2)
                DrawGambitCycler(g2x, rowY + 16, slotIdx, data.UnitType, isSlot1: false);
            else
                GUI.Label(new Rect(g2x, rowY + 16, CyclerW, 30), "Requires Archive L2", _subStyle);
        }

        // ─── Cycler: < GambitName > ───────────────────────────────────────────

        private void DrawGambitCycler(int x, int y, int slotIdx, string unitType, bool isSlot1)
        {
            var options    = GetGambitsForClass(unitType);
            GambitType cur = isSlot1 ? _slot1[slotIdx] : _slot2[slotIdx];
            int curIdx     = options.IndexOf(cur);
            if (curIdx < 0) curIdx = 0;

            // ← button
            if (GUI.Button(new Rect(x, y, ArrowW, RowBtnH), "<", _buttonStyle))
            {
                curIdx = (curIdx - 1 + options.Count) % options.Count;
                if (isSlot1) _slot1[slotIdx] = options[curIdx];
                else         _slot2[slotIdx] = options[curIdx];
            }

            // Label
            int labelW = CyclerW - ArrowW * 2 - 4;
            GUI.Label(new Rect(x + ArrowW + 2, y, labelW, RowBtnH),
                FormatGambitName(options[curIdx]), _cyclerStyle);

            // → button
            if (GUI.Button(new Rect(x + CyclerW - ArrowW, y, ArrowW, RowBtnH), ">", _buttonStyle))
            {
                curIdx = (curIdx + 1) % options.Count;
                if (isSlot1) _slot1[slotIdx] = options[curIdx];
                else         _slot2[slotIdx] = options[curIdx];
            }
        }

        // ════════════════════════════════════════════
        // HELPERS
        // ════════════════════════════════════════════

        /// <summary>
        /// Return the class-appropriate gambit list for a given unit type.
        /// "None" is always the first entry so players can leave slots empty.
        /// Falls back to all gambits if no class match is found (future-proofing).
        /// </summary>
        private static List<GambitType> GetGambitsForClass(string unitType)
        {
            var list   = new List<GambitType> { GambitType.None };
            string key = unitType.ToLowerInvariant();

            foreach (GambitType g in System.Enum.GetValues(typeof(GambitType)))
            {
                if (g == GambitType.None) continue;
                if (g.ToString().ToLowerInvariant().StartsWith(key))
                    list.Add(g);
            }

            // Fallback — show all gambits if prefix match found nothing
            if (list.Count == 1)
            {
                foreach (GambitType g in System.Enum.GetValues(typeof(GambitType)))
                    if (g != GambitType.None) list.Add(g);
            }

            return list;
        }

        private static string FormatGambitName(GambitType g)
        {
            if (g == GambitType.None) return "— None —";
            string raw        = g.ToString();
            int    underscore = raw.IndexOf('_');
            // "Warden_HoldTheLine" → "Hold The Line"
            string after = underscore >= 0 ? raw.Substring(underscore + 1) : raw;
            return System.Text.RegularExpressions.Regex.Replace(after, "([A-Z])", " $1").Trim();
        }

        private void EnsureStyles()
        {
            if (_stylesReady) return;

            var panelTex = new Texture2D(1, 1);
            panelTex.SetPixel(0, 0, new Color(0.04f, 0.04f, 0.09f, 0.94f));
            panelTex.Apply();

            _panelStyle = new GUIStyle(GUI.skin.box)
            {
                normal  = { background = panelTex },
                border  = new RectOffset(4, 4, 4, 4)
            };

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 17,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
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
                fontStyle = FontStyle.Normal,
                normal    = { textColor = new Color(0.65f, 0.62f, 0.72f) }
            };

            _cyclerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 12,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = new Color(0.88f, 0.85f, 0.98f) }
            };

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize  = 13,
                fontStyle = FontStyle.Bold,
                normal    = { textColor = Color.white }
            };

            _fatigueStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 10,
                fontStyle = FontStyle.Bold,
                normal    = { textColor = new Color(1.0f, 0.55f, 0.15f) }
            };

            _stylesReady = true;
        }
    }
}
