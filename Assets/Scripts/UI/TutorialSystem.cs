using UnityEngine;
using KindredSiege.Core;

namespace KindredSiege.UI
{
    /// <summary>
    /// First-play tutorial overlay system.
    ///
    /// Triggers contextual hints at key moments during the first campaign.
    /// Each hint shows once per campaign (tracked via PlayerPrefs).
    /// The system is passive — hints auto-dismiss after a timeout or on click.
    ///
    /// Hint triggers:
    ///   1. First CityPhase entry   → "Welcome" overview
    ///   2. First PreBattle entry   → Gambit + Directive explanation
    ///   3. First BattlePhase entry → Sanity and Mercy explanation
    ///   4. First PostBattle entry  → Fatigue and rest explanation
    ///   5. First SeasonEnd entry   → Season progression explanation
    ///
    /// Attach to the persistent Manager GameObject.
    /// </summary>
    public class TutorialSystem : MonoBehaviour
    {
        public static TutorialSystem Instance { get; private set; }

        // ─── Active hint state ───
        private bool   _hintActive;
        private string _hintTitle;
        private string _hintBody;
        private float  _hintTimer;
        private const float HintDuration = 12f;

        // ─── Tracking which hints have been shown ───
        private bool _shownWelcome;
        private bool _shownPreBattle;
        private bool _shownBattle;
        private bool _shownPostBattle;
        private bool _shownSeasonEnd;

        private const string PrefKey = "KS_TutorialFlags";

        // ─── Styles ───
        private bool     _stylesReady;
        private GUIStyle _panelStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _bodyStyle;
        private GUIStyle _dismissStyle;

        private const int HintW = 480;
        private const int HintH = 180;
        private const int HintMargin = 20;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            LoadFlags();
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

        /// <summary>Reset all tutorial flags (for new campaigns).</summary>
        public void ResetTutorial()
        {
            _shownWelcome = _shownPreBattle = _shownBattle = _shownPostBattle = _shownSeasonEnd = false;
            SaveFlags();
        }

        private void OnStateChanged(GameManager.GameState from, GameManager.GameState to)
        {
            // Don't show hints if tooltips are disabled
            if (SettingsPanel.Instance != null && !SettingsPanel.Instance.ShowTooltips)
                return;

            switch (to)
            {
                case GameManager.GameState.CityPhase when !_shownWelcome:
                    ShowHint("Welcome to the Drowned City",
                        "This is your stronghold. Build structures to strengthen your forces, " +
                        "rest weary units at the Sanatorium, and prepare for expeditions.\n\n" +
                        "Choose an expedition path at the Lighthouse to begin your next battle. " +
                        "Liberate all five districts to save the city.");
                    _shownWelcome = true;
                    SaveFlags();
                    break;

                case GameManager.GameState.PreBattle when !_shownPreBattle:
                    ShowHint("Preparing for Battle",
                        "Assign Gambits to your units before deploying. Gambits modify AI behaviour " +
                        "during combat — aggressive stances, defensive formations, or dark rituals " +
                        "that trade sanity for power.\n\n" +
                        "Each unit has slots unlocked by the Archive building.");
                    _shownPreBattle = true;
                    SaveFlags();
                    break;

                case GameManager.GameState.BattlePhase when !_shownBattle:
                    ShowHint("Combat & Sanity",
                        "Your units fight automatically. Watch their sanity — when it drops, " +
                        "they hesitate, suffer afflictions, or break entirely.\n\n" +
                        "Use Directive Points (top-left) to issue tactical commands. " +
                        "Mercy Tokens can save a unit from permanent loss when sanity hits zero.\n\n" +
                        "Press Escape to pause.");
                    _shownBattle = true;
                    SaveFlags();
                    break;

                case GameManager.GameState.PostBattle when !_shownPostBattle:
                    ShowHint("After the Battle",
                        "Units accumulate Fatigue after each deployment. Fatigued units suffer " +
                        "stat penalties and may become undeployable if pushed too hard.\n\n" +
                        "Rest them at the Sanatorium in the city phase. Units saved by Mercy Tokens " +
                        "may develop Phobias — permanent afflictions that require treatment.");
                    _shownPostBattle = true;
                    SaveFlags();
                    break;

                case GameManager.GameState.SeasonEnd when !_shownSeasonEnd:
                    ShowHint("Season Complete",
                        "A full season has passed. New districts may unlock, rivals grow stronger, " +
                        "and Mythos Exposure continues to rise.\n\n" +
                        "Mythos Exposure is a one-way clock. If it reaches 100, the city falls. " +
                        "Liberating districts through Sanity Siege battles is the only way to push it back.");
                    _shownSeasonEnd = true;
                    SaveFlags();
                    break;
            }
        }

        private void ShowHint(string title, string body)
        {
            _hintTitle  = title;
            _hintBody   = body;
            _hintTimer  = HintDuration;
            _hintActive = true;
        }

        private void Update()
        {
            if (!_hintActive) return;

            _hintTimer -= Time.unscaledDeltaTime;

            // Dismiss on click or timeout
            if (_hintTimer <= 0f || Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
                _hintActive = false;
        }

        private void OnGUI()
        {
            if (!_hintActive) return;
            EnsureStyles();

            // Position: top-centre with margin
            int px = (Screen.width - HintW) / 2;
            int py = HintMargin;

            // Fade based on remaining time
            float alpha = Mathf.Clamp01(_hintTimer / 2f);
            GUI.color = new Color(1f, 1f, 1f, alpha);

            GUI.Box(new Rect(px, py, HintW, HintH), GUIContent.none, _panelStyle);

            int ix = px + 20;
            int iy = py + 14;
            int lw = HintW - 40;

            // Title
            GUI.Label(new Rect(ix, iy, lw, 24), _hintTitle, _titleStyle);
            iy += 28;

            // Body
            GUI.Label(new Rect(ix, iy, lw, HintH - 70), _hintBody, _bodyStyle);

            // Dismiss hint
            GUI.Label(new Rect(px, py + HintH - 22, HintW, 18),
                "Click or press Space to dismiss", _dismissStyle);

            GUI.color = Color.white;
        }

        // ─── Persistence ───

        private void LoadFlags()
        {
            int flags = PlayerPrefs.GetInt(PrefKey, 0);
            _shownWelcome    = (flags & 1)  != 0;
            _shownPreBattle  = (flags & 2)  != 0;
            _shownBattle     = (flags & 4)  != 0;
            _shownPostBattle = (flags & 8)  != 0;
            _shownSeasonEnd  = (flags & 16) != 0;
        }

        private void SaveFlags()
        {
            int flags = 0;
            if (_shownWelcome)    flags |= 1;
            if (_shownPreBattle)  flags |= 2;
            if (_shownBattle)     flags |= 4;
            if (_shownPostBattle) flags |= 8;
            if (_shownSeasonEnd)  flags |= 16;
            PlayerPrefs.SetInt(PrefKey, flags);
            PlayerPrefs.Save();
        }

        // ─── Styles ───

        private void EnsureStyles()
        {
            if (_stylesReady) return;
            _stylesReady = true;

            var bg = new Texture2D(1, 1);
            bg.SetPixel(0, 0, new Color(0.06f, 0.08f, 0.14f, 0.94f));
            bg.Apply();

            _panelStyle = new GUIStyle(GUI.skin.box) { normal = { background = bg } };

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 16,
                fontStyle = FontStyle.Bold,
                normal    = { textColor = new Color(0.45f, 0.78f, 0.95f) }
            };

            _bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                wordWrap = true,
                normal   = { textColor = new Color(0.80f, 0.80f, 0.85f) }
            };

            _dismissStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 10,
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = new Color(0.45f, 0.45f, 0.55f) }
            };
        }
    }
}
