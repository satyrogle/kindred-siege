using UnityEngine;
using KindredSiege.Core;
using KindredSiege.City;
using KindredSiege.Rivalry;

namespace KindredSiege.UI
{
    /// <summary>
    /// Campaign victory screen — shown when all 5 districts are liberated.
    ///
    /// Displays final campaign stats and offers Main Menu return.
    /// Visible only when GameState == Victory.
    /// Attach to the persistent Manager GameObject.
    /// </summary>
    public class VictoryPanel : MonoBehaviour
    {
        public static VictoryPanel Instance { get; private set; }

        private bool _stylesReady;

        // Snapshot
        private int _finalSeason;
        private int _finalBattles;
        private int _rivalsDefeated;
        private int _rosterSize;
        private int _mythosExposure;

        // Styles
        private GUIStyle _panelStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _subtitleStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _subStyle;
        private GUIStyle _btnStyle;

        private const int PanelW = 560;
        private const int PanelH = 480;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        private void Start()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnStateChanged += OnStateChanged;
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnStateChanged -= OnStateChanged;
        }

        private void OnStateChanged(GameManager.GameState from, GameManager.GameState to)
        {
            if (to != GameManager.GameState.Victory) return;

            var gm = GameManager.Instance;
            _finalSeason    = gm?.CurrentSeason    ?? 1;
            _finalBattles   = gm?.BattlesCompleted ?? 0;
            _rivalsDefeated = RivalryEngine.Instance?.GetDefeatedForSave()?.Count ?? 0;
            _rosterSize     = KindredSiege.Battle.RosterManager.Instance?.RosterCount ?? 0;
            _mythosExposure = MythosExposure.Instance?.Exposure ?? 0;
        }

        private void OnGUI()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.CurrentState != GameManager.GameState.Victory) return;

            EnsureStyles();

            // Full-screen dark backdrop with golden tint
            GUI.color = new Color(0.02f, 0.02f, 0.01f, 0.94f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            int px = (Screen.width  - PanelW) / 2;
            int py = (Screen.height - PanelH) / 2;
            GUI.Box(new Rect(px, py, PanelW, PanelH), GUIContent.none, _panelStyle);

            int ix = px + 36;
            int iy = py + 30;
            int lw = PanelW - 72;

            // Title — warm gold
            GUI.color = new Color(0.95f, 0.80f, 0.30f);
            GUI.Label(new Rect(px, iy, PanelW, 36), "THE CITY IS SAVED", _titleStyle);
            GUI.color = Color.white;
            iy += 44;

            // Subtitle
            GUI.color = new Color(0.85f, 0.75f, 0.45f);
            GUI.Label(new Rect(px, iy, PanelW, 24), "All Districts Liberated", _subtitleStyle);
            GUI.color = Color.white;
            iy += 36;

            // Flavour text
            GUI.Label(new Rect(ix, iy, lw, 60),
                "The tide recedes. The streets stir with life once more. " +
                "Every district stands free of the drowned corruption. " +
                "The kindred who fought beside you will not be forgotten.",
                _subStyle);
            iy += 68;

            // Divider
            GUI.color = new Color(0.70f, 0.60f, 0.25f, 0.6f);
            GUI.DrawTexture(new Rect(ix, iy, lw, 1), Texture2D.whiteTexture);
            GUI.color = Color.white;
            iy += 16;

            // Stats header
            GUI.Label(new Rect(ix, iy, lw, 22), "CAMPAIGN RECORD", _labelStyle);
            iy += 28;

            DrawStat(ix, ref iy, lw, "Seasons survived", _finalSeason.ToString());
            DrawStat(ix, ref iy, lw, "Battles won", _finalBattles.ToString());
            DrawStat(ix, ref iy, lw, "Rivals destroyed", _rivalsDefeated.ToString());
            DrawStat(ix, ref iy, lw, "Final roster size", _rosterSize.ToString());
            DrawStat(ix, ref iy, lw, "Mythos Exposure", $"{_mythosExposure}/100");
            iy += 10;

            // Divider
            GUI.color = new Color(0.70f, 0.60f, 0.25f, 0.6f);
            GUI.DrawTexture(new Rect(ix, iy, lw, 1), Texture2D.whiteTexture);
            GUI.color = Color.white;
            iy += 22;

            // Main Menu button
            int btnW = 200;
            int btnH = 42;
            GUI.color = new Color(0.85f, 0.72f, 0.25f);
            if (GUI.Button(new Rect(px + (PanelW - btnW) / 2, iy, btnW, btnH), "Main Menu", _btnStyle))
            {
                GUI.color = Color.white;
                gm.ChangeState(GameManager.GameState.MainMenu);
            }
            GUI.color = Color.white;
        }

        private void DrawStat(int x, ref int y, int w, string label, string value)
        {
            GUI.Label(new Rect(x, y, w - 80, 20), label, _subStyle);
            GUI.Label(new Rect(x + w - 80, y, 80, 20), value, _labelStyle);
            y += 24;
        }

        private void EnsureStyles()
        {
            if (_stylesReady) return;
            _stylesReady = true;

            var bg = new Texture2D(1, 1);
            bg.SetPixel(0, 0, new Color(0.05f, 0.04f, 0.02f, 0.97f));
            bg.Apply();

            _panelStyle = new GUIStyle(GUI.skin.box) { normal = { background = bg } };

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 26,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = new Color(0.95f, 0.80f, 0.30f) }
            };

            _subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 16,
                fontStyle = FontStyle.Italic,
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = new Color(0.85f, 0.75f, 0.45f) }
            };

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 14,
                fontStyle = FontStyle.Bold,
                normal    = { textColor = new Color(0.95f, 0.88f, 0.65f) }
            };

            _subStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                wordWrap = true,
                normal   = { textColor = new Color(0.70f, 0.65f, 0.50f) }
            };

            _btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize  = 16,
                fontStyle = FontStyle.Bold,
                normal    = { textColor = Color.white }
            };
        }
    }
}
