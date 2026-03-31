using UnityEngine;
using KindredSiege.Core;

namespace KindredSiege.UI
{
    /// <summary>
    /// Main Menu — shown on startup and after game-over.
    ///
    /// Options:
    ///   • Continue — loads existing save (only if campaign.json exists)
    ///   • New Game — wipes progress and starts a fresh campaign
    ///
    /// Visible only when GameState == MainMenu.
    /// Attach to the persistent Manager GameObject.
    /// </summary>
    public class MainMenuPanel : MonoBehaviour
    {
        public static MainMenuPanel Instance { get; private set; }

        private bool _stylesReady;

        // Styles
        private GUIStyle _panelStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _subtitleStyle;
        private GUIStyle _btnStyle;
        private GUIStyle _labelStyle;

        private const int PanelW = 480;
        private const int PanelH = 440;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        private void OnGUI()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.CurrentState != GameManager.GameState.MainMenu) return;

            EnsureStyles();

            // Full-screen dark backdrop
            GUI.color = new Color(0.01f, 0.01f, 0.04f, 0.96f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            int px = (Screen.width  - PanelW) / 2;
            int py = (Screen.height - PanelH) / 2;

            GUI.Box(new Rect(px, py, PanelW, PanelH), GUIContent.none, _panelStyle);

            int ix = px + 40;
            int iy = py + 32;
            int lw = PanelW - 80;

            // Title
            GUI.color = new Color(0.45f, 0.78f, 0.95f);
            GUI.Label(new Rect(px, iy, PanelW, 36), "KINDRED SIEGE", _titleStyle);
            GUI.color = Color.white;
            iy += 40;

            GUI.Label(new Rect(px, iy, PanelW, 24), "The Drowned City", _subtitleStyle);
            iy += 40;

            // Divider
            GUI.color = new Color(0.25f, 0.40f, 0.55f, 0.5f);
            GUI.DrawTexture(new Rect(ix, iy, lw, 1), Texture2D.whiteTexture);
            GUI.color = Color.white;
            iy += 20;

            int btnW = 240;
            int btnH = 44;
            int bx   = px + (PanelW - btnW) / 2;

            // Continue button — only if save exists
            bool hasSave = SaveManager.Instance != null && SaveManager.Instance.HasSave;
            if (hasSave)
            {
                GUI.color = new Color(0.30f, 0.70f, 0.40f);
                if (GUI.Button(new Rect(bx, iy, btnW, btnH), "Continue", _btnStyle))
                {
                    GUI.color = Color.white;
                    SaveManager.Instance.LoadGame();
                    gm.ChangeState(GameManager.GameState.CityPhase);
                }
                GUI.color = Color.white;
                iy += btnH + 16;
            }

            // New Game button
            GUI.color = new Color(0.35f, 0.55f, 0.85f);
            if (GUI.Button(new Rect(bx, iy, btnW, btnH), "New Game", _btnStyle))
            {
                GUI.color = Color.white;
                gm.NewGame();
            }
            GUI.color = Color.white;
            iy += btnH + 16;

            // Settings button
            GUI.color = new Color(0.45f, 0.45f, 0.55f);
            if (GUI.Button(new Rect(bx, iy, btnW, btnH), "Settings", _btnStyle))
            {
                GUI.color = Color.white;
                SettingsPanel.Instance?.Show();
            }
            GUI.color = Color.white;
            iy += btnH + 16;

            // Quit button
            GUI.color = new Color(0.50f, 0.50f, 0.55f);
            if (GUI.Button(new Rect(bx, iy, btnW, btnH), "Quit", _btnStyle))
            {
                GUI.color = Color.white;
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
            GUI.color = Color.white;

            // Version / flavour at the bottom
            iy = py + PanelH - 32;
            GUI.Label(new Rect(px, iy, PanelW, 20), "v0.3  —  Phase 3", _labelStyle);
        }

        private void EnsureStyles()
        {
            if (_stylesReady) return;
            _stylesReady = true;

            var bg = new Texture2D(1, 1);
            bg.SetPixel(0, 0, new Color(0.03f, 0.03f, 0.07f, 0.98f));
            bg.Apply();

            _panelStyle = new GUIStyle(GUI.skin.box) { normal = { background = bg } };

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 28,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = new Color(0.45f, 0.78f, 0.95f) }
            };

            _subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 16,
                fontStyle = FontStyle.Italic,
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = new Color(0.55f, 0.55f, 0.65f) }
            };

            _btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize  = 16,
                fontStyle = FontStyle.Bold,
                normal    = { textColor = Color.white }
            };

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 11,
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = new Color(0.40f, 0.40f, 0.48f) }
            };
        }
    }
}
