using UnityEngine;
using KindredSiege.Core;
using KindredSiege.Battle;

namespace KindredSiege.UI
{
    /// <summary>
    /// In-battle pause menu overlay.
    ///
    /// Toggle with Escape key during BattlePhase.
    /// Pauses the battle and shows Resume / Settings / Quit to Menu.
    /// Attach to the persistent Manager GameObject.
    /// </summary>
    public class PauseMenuPanel : MonoBehaviour
    {
        public static PauseMenuPanel Instance { get; private set; }

        private bool _visible;
        private bool _stylesReady;

        // Styles
        private GUIStyle _panelStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _btnStyle;
        private GUIStyle _labelStyle;

        private const int PanelW = 360;
        private const int PanelH = 320;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        private void Update()
        {
            // Only respond to Escape during battle
            if (!Input.GetKeyDown(KeyCode.Escape)) return;

            var gm = GameManager.Instance;
            if (gm == null) return;

            // If settings is open, close it instead
            if (SettingsPanel.Instance != null && SettingsPanel.Instance.IsVisible)
            {
                SettingsPanel.Instance.Hide();
                return;
            }

            if (gm.CurrentState == GameManager.GameState.BattlePhase)
            {
                if (_visible)
                    Resume();
                else
                    Pause();
            }
            // Also allow Escape on main menu to close settings
            else if (gm.CurrentState == GameManager.GameState.MainMenu)
            {
                SettingsPanel.Instance?.Hide();
            }
        }

        private void Pause()
        {
            _visible = true;
            BattleManager.Instance?.PauseBattle();
        }

        private void Resume()
        {
            _visible = false;
            BattleManager.Instance?.ResumeBattle();
        }

        private void OnGUI()
        {
            if (!_visible) return;
            EnsureStyles();

            // Semi-transparent dark backdrop
            GUI.color = new Color(0.01f, 0.01f, 0.03f, 0.75f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            int px = (Screen.width  - PanelW) / 2;
            int py = (Screen.height - PanelH) / 2;
            GUI.Box(new Rect(px, py, PanelW, PanelH), GUIContent.none, _panelStyle);

            int ix = px + 32;
            int iy = py + 28;
            int lw = PanelW - 64;

            // Title
            GUI.color = new Color(0.45f, 0.78f, 0.95f);
            GUI.Label(new Rect(px, iy, PanelW, 28), "PAUSED", _titleStyle);
            GUI.color = Color.white;
            iy += 38;

            // Divider
            GUI.color = new Color(0.25f, 0.40f, 0.55f, 0.5f);
            GUI.DrawTexture(new Rect(ix, iy, lw, 1), Texture2D.whiteTexture);
            GUI.color = Color.white;
            iy += 24;

            int btnW = 220;
            int btnH = 40;
            int bx   = px + (PanelW - btnW) / 2;

            // Resume
            GUI.color = new Color(0.30f, 0.70f, 0.40f);
            if (GUI.Button(new Rect(bx, iy, btnW, btnH), "Resume", _btnStyle))
                Resume();
            GUI.color = Color.white;
            iy += btnH + 14;

            // Settings
            GUI.color = new Color(0.45f, 0.45f, 0.55f);
            if (GUI.Button(new Rect(bx, iy, btnW, btnH), "Settings", _btnStyle))
                SettingsPanel.Instance?.Show();
            GUI.color = Color.white;
            iy += btnH + 14;

            // Quit to Main Menu
            GUI.color = new Color(0.65f, 0.25f, 0.25f);
            if (GUI.Button(new Rect(bx, iy, btnW, btnH), "Quit to Main Menu", _btnStyle))
            {
                _visible = false;
                Time.timeScale = 1f;
                GameManager.Instance?.ChangeState(GameManager.GameState.GameOver);
                GameManager.Instance?.ChangeState(GameManager.GameState.MainMenu);
            }
            GUI.color = Color.white;
            iy += btnH + 20;

            // Hint
            GUI.Label(new Rect(px, iy, PanelW, 18), "Press Escape to resume", _labelStyle);
        }

        private void EnsureStyles()
        {
            if (_stylesReady) return;
            _stylesReady = true;

            var bg = new Texture2D(1, 1);
            bg.SetPixel(0, 0, new Color(0.03f, 0.03f, 0.06f, 0.97f));
            bg.Apply();

            _panelStyle = new GUIStyle(GUI.skin.box) { normal = { background = bg } };

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = new Color(0.45f, 0.78f, 0.95f) }
            };

            _btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize  = 15,
                fontStyle = FontStyle.Bold,
                normal    = { textColor = Color.white }
            };

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 11,
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = new Color(0.45f, 0.45f, 0.55f) }
            };
        }
    }
}
