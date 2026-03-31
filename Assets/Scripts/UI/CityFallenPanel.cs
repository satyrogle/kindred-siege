using UnityEngine;
using KindredSiege.Core;
using KindredSiege.City;
using KindredSiege.Rivalry;

namespace KindredSiege.UI
{
    /// <summary>
    /// GDD §Mythos Exposure — City Fallen (game-over screen).
    ///
    /// Shown when Mythos Exposure reaches 100. Displays campaign stats and
    /// offers "Try Again" to start a fresh run or "Quit" to return to main menu.
    ///
    /// Subscribes to MythosExposure.OnCityFallen and triggers GameState.GameOver.
    /// Attach to the persistent Manager GameObject.
    /// </summary>
    public class CityFallenPanel : MonoBehaviour
    {
        public static CityFallenPanel Instance { get; private set; }

        private bool _visible;
        private bool _stylesReady;

        // Snapshot taken at the moment of city fall
        private int _finalSeason;
        private int _finalBattles;
        private int _rivalsDefeated;
        private int _rosterSize;

        // Styles
        private GUIStyle _panelStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _subStyle;
        private GUIStyle _btnStyle;

        private const int PanelW = 540;
        private const int PanelH = 420;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        private void Start()
        {
            if (MythosExposure.Instance != null)
                MythosExposure.Instance.OnCityFallen += OnCityFallen;
        }

        private void OnDestroy()
        {
            if (MythosExposure.Instance != null)
                MythosExposure.Instance.OnCityFallen -= OnCityFallen;
        }

        private void OnCityFallen()
        {
            // Snapshot campaign state before the game-over transition
            var gm = GameManager.Instance;
            _finalSeason    = gm?.CurrentSeason    ?? 1;
            _finalBattles   = gm?.BattlesCompleted ?? 0;
            _rivalsDefeated = RivalryEngine.Instance?.GetDefeatedForSave()?.Count ?? 0;
            _rosterSize     = KindredSiege.Battle.RosterManager.Instance?.RosterCount ?? 0;

            _visible = true;
            gm?.ChangeState(GameManager.GameState.GameOver);
        }

        private void OnGUI()
        {
            if (!_visible) return;
            EnsureStyles();

            // Full-screen dark backdrop
            GUI.color = new Color(0.02f, 0f, 0.04f, 0.92f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            int px = (Screen.width  - PanelW) / 2;
            int py = (Screen.height - PanelH) / 2;

            GUI.Box(new Rect(px, py, PanelW, PanelH), GUIContent.none, _panelStyle);

            int ix = px + 32;
            int iy = py + 28;
            int lw = PanelW - 64;

            // Title — ominous purple
            GUI.color = new Color(0.60f, 0.12f, 0.78f);
            GUI.Label(new Rect(ix, iy, lw, 36), "THE CITY HAS FALLEN", _titleStyle);
            GUI.color = Color.white;
            iy += 44;

            // Flavour text
            GUI.Label(new Rect(ix, iy, lw, 48),
                "The drowned city's corruption could not be held back. " +
                "The streets are silent now — only the tide remembers what was lost.",
                _subStyle);
            iy += 56;

            // Divider
            GUI.color = new Color(0.40f, 0.10f, 0.55f, 0.6f);
            GUI.DrawTexture(new Rect(ix, iy, lw, 1), Texture2D.whiteTexture);
            GUI.color = Color.white;
            iy += 14;

            // Campaign stats
            GUI.Label(new Rect(ix, iy, lw, 22), "CAMPAIGN SUMMARY", _labelStyle);
            iy += 28;

            DrawStat(ix, ref iy, lw, "Season reached", _finalSeason.ToString());
            DrawStat(ix, ref iy, lw, "Battles completed", _finalBattles.ToString());
            DrawStat(ix, ref iy, lw, "Rivals defeated", _rivalsDefeated.ToString());
            DrawStat(ix, ref iy, lw, "Surviving roster", _rosterSize.ToString());

            iy += 16;

            // Divider
            GUI.color = new Color(0.40f, 0.10f, 0.55f, 0.6f);
            GUI.DrawTexture(new Rect(ix, iy, lw, 1), Texture2D.whiteTexture);
            GUI.color = Color.white;
            iy += 18;

            // Buttons
            int btnW = 180;
            int btnH = 38;
            int gap  = 24;
            int totalW = btnW * 2 + gap;
            int bx = px + (PanelW - totalW) / 2;

            GUI.color = new Color(0.45f, 0.20f, 0.65f);
            if (GUI.Button(new Rect(bx, iy, btnW, btnH), "Try Again", _btnStyle))
            {
                GUI.color = Color.white;
                _visible = false;
                GameManager.Instance?.NewGame();
            }
            GUI.color = Color.white;

            if (GUI.Button(new Rect(bx + btnW + gap, iy, btnW, btnH), "Main Menu", _btnStyle))
            {
                _visible = false;
                GameManager.Instance?.ChangeState(GameManager.GameState.MainMenu);
            }
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
            bg.SetPixel(0, 0, new Color(0.04f, 0.02f, 0.08f, 0.97f));
            bg.Apply();

            _panelStyle = new GUIStyle(GUI.skin.box) { normal = { background = bg } };

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = new Color(0.60f, 0.12f, 0.78f) }
            };

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 14,
                fontStyle = FontStyle.Bold,
                normal    = { textColor = new Color(0.90f, 0.86f, 0.95f) }
            };

            _subStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                wordWrap = true,
                normal   = { textColor = new Color(0.60f, 0.55f, 0.68f) }
            };

            _btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize  = 14,
                fontStyle = FontStyle.Bold,
                normal    = { textColor = Color.white }
            };
        }
    }
}
