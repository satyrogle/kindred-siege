using UnityEngine;
using KindredSiege.Core;

namespace KindredSiege.UI
{
    /// <summary>
    /// Season End Panel — bridges the SeasonEnd state back to CityPhase.
    ///
    /// Subscribes to GameManager.OnSeasonEnd. Displays a full-screen overlay
    /// summarising the completed season, then routes the player into the next
    /// season's city phase via GameManager.ReturnToCity().
    ///
    /// Uses OnGUI — no Canvas prefab required.
    /// Attach to the persistent Manager GameObject alongside GameManager.
    /// </summary>
    public class SeasonEndPanel : MonoBehaviour
    {
        public static SeasonEndPanel Instance { get; private set; }

        private bool _visible;
        private int  _completedSeason;

        // ─── Layout ───
        private const int PanelW = 520;
        private const int PanelH = 280;

        // ─── Styles ───
        private GUIStyle _panelStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _bodyStyle;
        private GUIStyle _subStyle;
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
                GameManager.Instance.OnSeasonEnd      += OnSeasonEnd;
                GameManager.Instance.OnStateChanged   += OnStateChanged;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnSeasonEnd      -= OnSeasonEnd;
                GameManager.Instance.OnStateChanged   -= OnStateChanged;
            }
        }

        // ════════════════════════════════════════════
        // EVENT HANDLERS
        // ════════════════════════════════════════════

        private void OnSeasonEnd()
        {
            _completedSeason = GameManager.Instance?.CurrentSeason ?? 1;
            _visible = true;
        }

        private void OnStateChanged(GameManager.GameState from, GameManager.GameState to)
        {
            // Hide once we've successfully entered the next city phase
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
            GUI.color = new Color(0f, 0f, 0f, 0.75f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            int px = (Screen.width  - PanelW) / 2;
            int py = (Screen.height - PanelH) / 2;

            GUI.Box(new Rect(px, py, PanelW, PanelH), GUIContent.none, _panelStyle);

            int ix = px + 30;
            int iy = py + 30;
            int lw = PanelW - 60;

            GUI.Label(new Rect(ix, iy, lw, 32),
                $"SEASON {_completedSeason} COMPLETE", _titleStyle);
            iy += 40;

            GUI.color = new Color(0.6f, 0.6f, 0.7f);
            GUI.DrawTexture(new Rect(ix, iy, lw, 1), Texture2D.whiteTexture);
            GUI.color = Color.white;
            iy += 10;

            GUI.Label(new Rect(ix, iy, lw, 24),
                "Survivors rest. The dead are mourned.", _bodyStyle);
            iy += 30;

            GUI.Label(new Rect(ix, iy, lw, 20),
                "The drowned city does not sleep — new horrors stir.", _subStyle);
            iy += 14;

            GUI.Label(new Rect(ix, iy, lw, 20),
                $"Season {_completedSeason + 1} begins with stronger rivals and deeper corruption.", _subStyle);
            iy += 44;

            // Begin Next Season button
            int btnW = 220;
            int btnX = px + (PanelW - btnW) / 2;
            GUI.color = new Color(0.35f, 0.75f, 0.45f);
            if (GUI.Button(new Rect(btnX, iy, btnW, 36), $"Begin Season {_completedSeason + 1}", _btnStyle))
            {
                GUI.color = Color.white;
                GameManager.Instance?.ReturnToCity();
            }
            GUI.color = Color.white;
        }

        // ════════════════════════════════════════════
        // STYLES
        // ════════════════════════════════════════════

        private void EnsureStyles()
        {
            if (_stylesReady) return;
            _stylesReady = true;

            _panelStyle = new GUIStyle(GUI.skin.box);
            _panelStyle.normal.background = MakeTex(new Color(0.07f, 0.09f, 0.13f, 0.98f));

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            _titleStyle.normal.textColor = new Color(0.95f, 0.85f, 0.55f);

            _bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 14,
                alignment = TextAnchor.MiddleCenter
            };
            _bodyStyle.normal.textColor = new Color(0.88f, 0.88f, 0.88f);

            _subStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 11,
                alignment = TextAnchor.MiddleCenter
            };
            _subStyle.normal.textColor = new Color(0.60f, 0.60f, 0.65f);

            _btnStyle = new GUIStyle(GUI.skin.button) { fontSize = 13, fontStyle = FontStyle.Bold };
            _btnStyle.normal.textColor  = Color.white;
            _btnStyle.hover.textColor   = Color.white;
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
