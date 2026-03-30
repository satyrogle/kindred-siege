using System.Collections.Generic;
using UnityEngine;
using KindredSiege.Battle;
using KindredSiege.Units;

namespace KindredSiege.UI
{
    /// <summary>
    /// GDD §9 — Talent Tree Panel.
    ///
    /// City-phase OnGUI panel. Shows per-unit talent trees (2 branches × 5 nodes).
    /// Units earn 1 talent point per survived expedition. Points are spent here.
    ///
    /// Attach to a persistent Manager GameObject. Call Show(units) from CityHUD.
    /// </summary>
    public class TalentTreePanel : MonoBehaviour
    {
        public static TalentTreePanel Instance { get; private set; }

        private bool _visible;
        private bool _stylesReady;
        private List<UnitData> _roster;
        private int  _selectedIndex = 0;
        private Vector2 _scrollPos;

        // Styles
        private GUIStyle _panelStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _subStyle;
        private GUIStyle _nodeStyle;
        private GUIStyle _nodeLockedStyle;
        private GUIStyle _nodeUnlockedStyle;
        private GUIStyle _btnStyle;
        private GUIStyle _descStyle;

        private const int PanelW = 860;
        private const int PanelH = 560;
        private const int NodeW  = 180;
        private const int NodeH  = 54;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        public void Show(List<UnitData> roster)
        {
            _roster        = roster;
            _selectedIndex = 0;
            _visible       = true;
            _scrollPos     = Vector2.zero;
        }

        public void Hide() => _visible = false;

        private void OnGUI()
        {
            if (!_visible || _roster == null || _roster.Count == 0) return;
            EnsureStyles();

            // Dark backdrop
            GUI.color = new Color(0f, 0f, 0f, 0.75f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            int px = (Screen.width  - PanelW) / 2;
            int py = (Screen.height - PanelH) / 2;

            GUI.Box(new Rect(px, py, PanelW, PanelH), GUIContent.none, _panelStyle);

            // Title
            GUI.Label(new Rect(px + 20, py + 14, PanelW - 120, 28), "TALENT TREES", _titleStyle);

            if (GUI.Button(new Rect(px + PanelW - 100, py + 14, 80, 28), "Close", _btnStyle))
                Hide();

            // Unit selector tabs
            int tabX = px + 20;
            int tabY = py + 52;
            for (int i = 0; i < _roster.Count; i++)
            {
                var u = _roster[i];
                if (u == null) continue;
                bool selected = i == _selectedIndex;
                GUI.color = selected ? new Color(0.9f, 0.8f, 0.4f) : new Color(0.5f, 0.5f, 0.55f);
                if (GUI.Button(new Rect(tabX, tabY, 100, 26), u.UnitName, _btnStyle))
                    _selectedIndex = i;
                GUI.color = Color.white;
                tabX += 106;
                if (tabX > px + PanelW - 120) break; // Safety — don't overflow
            }

            // Draw selected unit tree
            var unit = _roster[_selectedIndex];
            if (unit != null)
                DrawTree(unit, px + 20, py + 90, PanelW - 40, PanelH - 110);
        }

        private void DrawTree(UnitData unit, int x, int y, int w, int h)
        {
            var nodes = TalentSystem.GetNodesForClass(unit.UnitType);
            if (nodes == null || nodes.Count == 0)
            {
                GUI.Label(new Rect(x, y + 20, w, 30), $"No talent tree defined for class: {unit.UnitType}", _descStyle);
                return;
            }

            int available = unit.TalentPointsAvailable;
            string branch0Name = TalentSystem.GetBranchName(unit.UnitType, 0);
            string branch1Name = TalentSystem.GetBranchName(unit.UnitType, 1);

            // Info bar
            GUI.Label(new Rect(x, y, w, 22),
                $"{unit.UnitName}  [{unit.UnitType}]   Expeditions: {unit.ExpeditionCount}   " +
                $"Points available: {available}   Unlocked: {unit.UnlockedTalents.Count}",
                _subStyle);

            int branchY = y + 28;

            // Branch 0 (left half)
            GUI.color = new Color(0.4f, 0.8f, 1.0f, 0.9f);
            GUI.Label(new Rect(x, branchY, (w / 2) - 10, 22), branch0Name.ToUpper(), _subStyle);
            GUI.color = Color.white;
            DrawBranch(unit, nodes, 0, x, branchY + 24, (w / 2) - 10, available);

            // Branch 1 (right half)
            GUI.color = new Color(1.0f, 0.55f, 0.2f, 0.9f);
            GUI.Label(new Rect(x + w / 2 + 10, branchY, (w / 2) - 10, 22), branch1Name.ToUpper(), _subStyle);
            GUI.color = Color.white;
            DrawBranch(unit, nodes, 1, x + w / 2 + 10, branchY + 24, (w / 2) - 10, available);
        }

        private void DrawBranch(UnitData unit, List<TalentNodeId> allNodes, int branch,
                                 int bx, int by, int bw, int available)
        {
            int nodeIndex = 0;
            for (int i = 0; i < allNodes.Count; i++)
            {
                var nodeId = allNodes[i];
                if (TalentSystem.GetBranch(nodeId) != branch) continue;

                bool unlocked  = unit.HasTalent(nodeId);
                bool canUnlock = !unlocked && available > 0 && nodeIndex <= unit.UnlockedTalents.Count;

                int ny = by + nodeIndex * (NodeH + 6);
                DrawNode(unit, nodeId, bx, ny, bw, unlocked, canUnlock);
                nodeIndex++;
            }
        }

        private void DrawNode(UnitData unit, TalentNodeId nodeId,
                               int nx, int ny, int nw, bool unlocked, bool canUnlock)
        {
            // Background
            GUI.color = unlocked    ? new Color(0.2f, 0.45f, 0.2f, 0.95f) :
                        canUnlock   ? new Color(0.25f, 0.25f, 0.3f, 0.95f) :
                                      new Color(0.15f, 0.15f, 0.18f, 0.95f);
            GUI.DrawTexture(new Rect(nx, ny, nw, NodeH), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Node name
            string label = unlocked ? $"✓ {FormatNodeName(nodeId)}" : FormatNodeName(nodeId);
            GUI.Label(new Rect(nx + 6, ny + 4, nw - 70, 20), label,
                      unlocked ? _nodeUnlockedStyle : _nodeLockedStyle);

            // Description
            GUI.Label(new Rect(nx + 6, ny + 26, nw - 70, 22),
                      TalentSystem.GetNodeDescription(nodeId), _descStyle);

            // Unlock button
            if (!unlocked)
            {
                GUI.color = canUnlock
                    ? new Color(0.3f, 0.85f, 0.4f)
                    : new Color(0.35f, 0.35f, 0.38f);
                if (GUI.Button(new Rect(nx + nw - 62, ny + 10, 56, 26),
                               canUnlock ? "Unlock" : "Locked", _btnStyle))
                {
                    if (canUnlock)
                        UnlockNode(unit, nodeId);
                }
                GUI.color = Color.white;
            }
        }

        private static void UnlockNode(UnitData unit, TalentNodeId nodeId)
        {
            if (unit.TalentPointsAvailable <= 0) return;
            if (unit.HasTalent(nodeId)) return;
            unit.UnlockedTalents.Add(nodeId);
            Debug.Log($"[Talent] {unit.UnitName} unlocked: {nodeId}");
        }

        private static string FormatNodeName(TalentNodeId id)
        {
            // "Warden_Resolve_1" → "Resolve 1"
            var parts = id.ToString().Split('_');
            if (parts.Length >= 3)
                return $"{parts[parts.Length - 2]} {parts[parts.Length - 1]}";
            return id.ToString();
        }

        private void EnsureStyles()
        {
            if (_stylesReady) return;
            _stylesReady = true;

            var bg = MakeTex(new Color(0.06f, 0.06f, 0.09f, 0.97f));
            _panelStyle = new GUIStyle(GUI.skin.box) { normal = { background = bg } };

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20, fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.95f, 0.85f, 0.45f) }
            };
            _subStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13, fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.75f, 0.75f, 0.8f) }
            };
            _nodeUnlockedStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12, fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.5f, 1f, 0.5f) }
            };
            _nodeLockedStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                normal = { textColor = new Color(0.85f, 0.85f, 0.9f) }
            };
            _descStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11, wordWrap = true,
                normal = { textColor = new Color(0.6f, 0.6f, 0.65f) }
            };
            _btnStyle = new GUIStyle(GUI.skin.button) { fontSize = 12 };
        }

        private static Texture2D MakeTex(Color c)
        {
            var t = new Texture2D(1, 1);
            t.SetPixel(0, 0, c);
            t.Apply();
            return t;
        }
    }
}
