using UnityEngine;
using KindredSiege.Core;

namespace KindredSiege.UI
{
    /// <summary>
    /// Settings panel — accessible from Main Menu and Pause Menu.
    ///
    /// Controls:
    ///   - Master Volume (0–100)
    ///   - Default Battle Speed (1x / 2x / 4x)
    ///   - Show Tooltips toggle
    ///
    /// All settings are persisted via PlayerPrefs.
    /// Attach to the persistent Manager GameObject.
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        public static SettingsPanel Instance { get; private set; }

        private bool _visible;
        private bool _stylesReady;

        // ─── Settings state ───
        private float _masterVolume;
        private int   _defaultBattleSpeed; // 0 = 1x, 1 = 2x, 2 = 4x
        private bool  _showTooltips;

        private static readonly string[] SpeedLabels = { "1x", "2x", "4x" };
        private static readonly float[]  SpeedValues = { 1f, 2f, 4f };

        // ─── Styles ───
        private GUIStyle _panelStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _valueStyle;
        private GUIStyle _btnStyle;
        private GUIStyle _sliderThumb;
        private GUIStyle _sliderBg;

        private const int PanelW = 420;
        private const int PanelH = 340;

        // ─── PlayerPrefs keys ───
        private const string KeyVolume  = "KS_MasterVolume";
        private const string KeySpeed   = "KS_DefaultBattleSpeed";
        private const string KeyTips    = "KS_ShowTooltips";

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            LoadSettings();
        }

        public void Show()  => _visible = true;
        public void Hide()  => _visible = false;
        public void Toggle() => _visible = !_visible;
        public bool IsVisible => _visible;

        public float MasterVolume     => _masterVolume;
        public float DefaultBattleSpeed => SpeedValues[_defaultBattleSpeed];
        public bool  ShowTooltips     => _showTooltips;

        private void OnGUI()
        {
            if (!_visible) return;
            EnsureStyles();

            // Semi-transparent backdrop
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            int px = (Screen.width  - PanelW) / 2;
            int py = (Screen.height - PanelH) / 2;
            GUI.Box(new Rect(px, py, PanelW, PanelH), GUIContent.none, _panelStyle);

            int ix = px + 32;
            int iy = py + 24;
            int lw = PanelW - 64;

            // Title
            GUI.color = new Color(0.45f, 0.78f, 0.95f);
            GUI.Label(new Rect(px, iy, PanelW, 28), "SETTINGS", _titleStyle);
            GUI.color = Color.white;
            iy += 38;

            // Divider
            GUI.color = new Color(0.25f, 0.40f, 0.55f, 0.5f);
            GUI.DrawTexture(new Rect(ix, iy, lw, 1), Texture2D.whiteTexture);
            GUI.color = Color.white;
            iy += 16;

            // ── Master Volume ──
            GUI.Label(new Rect(ix, iy, 140, 22), "Master Volume", _labelStyle);
            GUI.Label(new Rect(ix + lw - 40, iy, 40, 22), $"{Mathf.RoundToInt(_masterVolume * 100)}%", _valueStyle);
            iy += 24;
            _masterVolume = GUI.HorizontalSlider(new Rect(ix, iy, lw, 16), _masterVolume, 0f, 1f);
            AudioListener.volume = _masterVolume;
            iy += 30;

            // ── Default Battle Speed ──
            GUI.Label(new Rect(ix, iy, 160, 22), "Default Battle Speed", _labelStyle);
            iy += 26;
            int btnW = 80;
            int gap  = 12;
            int totalBtnW = btnW * 3 + gap * 2;
            int bx = ix + (lw - totalBtnW) / 2;
            for (int i = 0; i < 3; i++)
            {
                bool selected = _defaultBattleSpeed == i;
                GUI.color = selected ? new Color(0.35f, 0.65f, 0.90f) : new Color(0.35f, 0.35f, 0.40f);
                if (GUI.Button(new Rect(bx + i * (btnW + gap), iy, btnW, 30), SpeedLabels[i], _btnStyle))
                    _defaultBattleSpeed = i;
            }
            GUI.color = Color.white;
            iy += 44;

            // ── Show Tooltips ──
            GUI.Label(new Rect(ix, iy, 140, 22), "Show Tooltips", _labelStyle);
            string tipLabel = _showTooltips ? "ON" : "OFF";
            GUI.color = _showTooltips ? new Color(0.30f, 0.70f, 0.40f) : new Color(0.55f, 0.35f, 0.35f);
            if (GUI.Button(new Rect(ix + lw - 70, iy - 2, 70, 26), tipLabel, _btnStyle))
                _showTooltips = !_showTooltips;
            GUI.color = Color.white;
            iy += 40;

            // Divider
            GUI.color = new Color(0.25f, 0.40f, 0.55f, 0.5f);
            GUI.DrawTexture(new Rect(ix, iy, lw, 1), Texture2D.whiteTexture);
            GUI.color = Color.white;
            iy += 20;

            // ── Close button ──
            int closeBtnW = 160;
            GUI.color = new Color(0.40f, 0.40f, 0.50f);
            if (GUI.Button(new Rect(px + (PanelW - closeBtnW) / 2, iy, closeBtnW, 36), "Close", _btnStyle))
            {
                SaveSettings();
                Hide();
            }
            GUI.color = Color.white;
        }

        private void LoadSettings()
        {
            _masterVolume      = PlayerPrefs.GetFloat(KeyVolume, 1f);
            _defaultBattleSpeed = PlayerPrefs.GetInt(KeySpeed, 0);
            _showTooltips      = PlayerPrefs.GetInt(KeyTips, 1) == 1;
            AudioListener.volume = _masterVolume;
        }

        private void SaveSettings()
        {
            PlayerPrefs.SetFloat(KeyVolume, _masterVolume);
            PlayerPrefs.SetInt(KeySpeed, _defaultBattleSpeed);
            PlayerPrefs.SetInt(KeyTips, _showTooltips ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void OnApplicationQuit() => SaveSettings();

        private void EnsureStyles()
        {
            if (_stylesReady) return;
            _stylesReady = true;

            var bg = new Texture2D(1, 1);
            bg.SetPixel(0, 0, new Color(0.04f, 0.04f, 0.08f, 0.97f));
            bg.Apply();

            _panelStyle = new GUIStyle(GUI.skin.box) { normal = { background = bg } };

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = new Color(0.45f, 0.78f, 0.95f) }
            };

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 14,
                normal    = { textColor = new Color(0.80f, 0.80f, 0.85f) }
            };

            _valueStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleRight,
                normal    = { textColor = new Color(0.45f, 0.78f, 0.95f) }
            };

            _btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize  = 13,
                fontStyle = FontStyle.Bold,
                normal    = { textColor = Color.white }
            };
        }
    }
}
