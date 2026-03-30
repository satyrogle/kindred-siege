using System.Collections.Generic;
using UnityEngine;
using KindredSiege.Core;
using KindredSiege.Battle;
using KindredSiege.Rivalry;

namespace KindredSiege.UI
{
    /// <summary>
    /// Season End Panel — GDD §Campaign Loop.
    ///
    /// Displays a full-screen summary after all battles in a season are complete:
    ///   • Battles fought
    ///   • Roster status (each unit: expeditions, fatigue, phobia)
    ///   • Rivals defeated
    ///   • Flavour transition text
    ///
    /// Subscribes to GameManager.OnSeasonEnd. "Begin Next Season" calls ReturnToCity().
    /// Attach to the persistent Manager GameObject alongside GameManager.
    /// </summary>
    public class SeasonEndPanel : MonoBehaviour
    {
        public static SeasonEndPanel Instance { get; private set; }

        private bool _visible;
        private int  _completedSeason;
        private int  _battlesCompleted;

        // Snapshot taken when the panel opens
        private List<UnitData>  _rosterSnapshot  = new();
        private int             _rivalsDefeated;

        // ─── Layout ───
        private const int PanelW = 660;
        private const int PanelH = 480;

        // ─── Styles ───
        private GUIStyle _panelStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _sectionStyle;
        private GUIStyle _rowStyle;
        private GUIStyle _dimStyle;
        private GUIStyle _badStyle;
        private GUIStyle _btnStyle;
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
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnSeasonEnd    += OnSeasonEnd;
                GameManager.Instance.OnStateChanged += OnStateChanged;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnSeasonEnd    -= OnSeasonEnd;
                GameManager.Instance.OnStateChanged -= OnStateChanged;
            }
        }

        // ════════════════════════════════════════════
        // EVENT HANDLERS
        // ════════════════════════════════════════════

        private void OnSeasonEnd()
        {
            var gm = GameManager.Instance;
            _completedSeason  = gm?.CurrentSeason ?? 1;
            _battlesCompleted = gm?.BattlesCompleted ?? 0;

            // Snapshot roster
            _rosterSnapshot.Clear();
            var roster = RosterManager.Instance;
            if (roster != null)
                foreach (var u in roster.ActiveRoster)
                    if (u != null) _rosterSnapshot.Add(u);

            // Count defeated rivals
            _rivalsDefeated = 0;
            var rivalry = RivalryEngine.Instance;
            if (rivalry != null)
                _rivalsDefeated = rivalry.GetDefeatedForSave()?.Count ?? 0;

            _visible = true;
        }

        private void OnStateChanged(GameManager.GameState from, GameManager.GameState to)
        {
            if (to == GameManager.GameState.CityPhase)
                _visible = false;
        }

        // ════════════════════════════════════════════
        // OnGUI
        // ════════════════════════════════════════════

        private void OnGUI()
        {
            if (!_visible) return;
            EnsureStyles();

            // Darkened backdrop
            GUI.color = new Color(0f, 0f, 0f, 0.80f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            int px = (Screen.width  - PanelW) / 2;
            int py = (Screen.height - PanelH) / 2;

            GUI.Box(new Rect(px, py, PanelW, PanelH), GUIContent.none, _panelStyle);

            int ix = px + 30;
            int iy = py + 26;
            int lw = PanelW - 60;

            // ── Title ──
            GUI.Label(new Rect(ix, iy, lw, 34), $"SEASON {_completedSeason} COMPLETE", _titleStyle);
            iy += 42;

            Divider(ix, iy, lw); iy += 12;

            // ── Campaign stats ──
            GUI.Label(new Rect(ix, iy, lw, 22),
                $"Battles fought: {_battlesCompleted}    Rivals defeated (total): {_rivalsDefeated}",
                _sectionStyle);
            iy += 30;

            Divider(ix, iy, lw); iy += 12;

            // ── Roster summary ──
            GUI.Label(new Rect(ix, iy, lw, 20), "SURVIVING ROSTER", _sectionStyle);
            iy += 24;

            if (_rosterSnapshot.Count == 0)
            {
                GUI.Label(new Rect(ix, iy, lw, 20), "  No units deployed this season.", _dimStyle);
                iy += 22;
            }
            else
            {
                foreach (var unit in _rosterSnapshot)
                {
                    bool hasPenalty = unit.ActivePhobia != PhobiaType.None || unit.FatigueLevel >= 50;
                    var  style      = hasPenalty ? _badStyle : _rowStyle;

                    string phobiaTag  = unit.ActivePhobia != PhobiaType.None
                        ? $"  [PHOBIA: {unit.ActivePhobia}]" : "";
                    string fatigueTag = unit.FatigueLevel >= 80
                        ? $"  [FATIGUED {unit.FatigueLevel}%]"
                        : unit.FatigueLevel >= 50
                            ? $"  [Tired {unit.FatigueLevel}%]"
                            : "";
                    string talentTag  = unit.UnlockedTalents != null && unit.UnlockedTalents.Count > 0
                        ? $"  ({unit.UnlockedTalents.Count} talents)"
                        : "";

                    GUI.Label(new Rect(ix + 8, iy, lw - 8, 20),
                        $"{unit.UnitName}  [{unit.UnitType}]  " +
                        $"Expeditions: {unit.ExpeditionCount}" +
                        fatigueTag + phobiaTag + talentTag,
                        style);
                    iy += 22;
                }
            }

            iy += 6;
            Divider(ix, iy, lw); iy += 12;

            // ── Flavour ──
            GUI.Label(new Rect(ix, iy, lw, 20),
                "The drowned city does not sleep. Season " + (_completedSeason + 1) +
                " brings stronger rivals and deeper corruption.",
                _dimStyle);
            iy += 30;

            // ── Button ──
            int btnW = 240;
            int btnX = px + (PanelW - btnW) / 2;
            GUI.color = new Color(0.35f, 0.75f, 0.45f);
            if (GUI.Button(new Rect(btnX, iy, btnW, 38), $"Begin Season {_completedSeason + 1}", _btnStyle))
            {
                GUI.color = Color.white;
                GameManager.Instance?.ReturnToCity();
            }
            GUI.color = Color.white;
        }

        // ════════════════════════════════════════════
        // HELPERS
        // ════════════════════════════════════════════

        private static void Divider(int x, int y, int w)
        {
            GUI.color = new Color(0.4f, 0.4f, 0.5f, 0.6f);
            GUI.DrawTexture(new Rect(x, y, w, 1), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        // ════════════════════════════════════════════
        // STYLES
        // ════════════════════════════════════════════

        private void EnsureStyles()
        {
            if (_stylesReady) return;
            _stylesReady = true;

            _panelStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = MakeTex(new Color(0.06f, 0.08f, 0.12f, 0.98f)) }
            };

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24, fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.95f, 0.85f, 0.45f) }
            };

            _sectionStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13, fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.80f, 0.80f, 0.85f) }
            };

            _rowStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                normal = { textColor = new Color(0.75f, 0.82f, 0.75f) }
            };

            _badStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                normal = { textColor = new Color(0.90f, 0.55f, 0.35f) }
            };

            _dimStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11, fontStyle = FontStyle.Italic,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.52f, 0.52f, 0.58f) }
            };

            _btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14, fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
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
