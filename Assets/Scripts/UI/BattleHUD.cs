using System.Collections.Generic;
using UnityEngine;
using KindredSiege.Battle;
using KindredSiege.Core;

namespace KindredSiege.UI
{
    /// <summary>
    /// Screen-space battle HUD (GDD §HUD, §4.2, §15).
    ///
    /// Displays:
    ///   - Directive Points remaining (5-point budget)
    ///   - Mercy Token count
    ///   - Battle speed controls (0.5x / 1x / 2x / 4x / Pause)
    ///   - Directive buttons when a player unit is selected
    ///   - Mercy Decision popup when a unit hits 0 HP
    ///   - Sanity / affliction / virtue events as toasts
    ///
    /// Uses OnGUI — no Canvas prefab required.
    /// Attach to a persistent HUD GameObject in the battle scene.
    /// </summary>
    public class BattleHUD : MonoBehaviour
    {
        // ════════════════════════════════════════════
        // REFERENCES
        // ════════════════════════════════════════════

        private BattleManager   _battle;
        private DirectiveSystem _directives;

        // ─── Selection ───
        private UnitController _selectedUnit;
        private Camera         _mainCamera;

        // ─── Mercy Pause ───
        private bool          _mercyPopupActive;
        private int           _mercyUnitId;
        private string        _mercyUnitName;
        private string        _mercyUnitType;
        private int           _mercyExpeditions;
        private int           _mercyTokensAvailable;

        // ─── Toast notifications ───
        private readonly Queue<ToastMessage> _toasts = new();
        private float _toastTimer;
        private const float ToastDuration = 2.5f;

        private struct ToastMessage
        {
            public string Text;
            public Color  Colour;
        }

        // ─── Layout constants ───
        private const int Margin     = 12;
        private const int PanelW     = 220;
        private const int ButtonH    = 34;
        private const int SpeedBtnW  = 46;

        // ─── GUI styles (lazily initialised) ───
        private GUIStyle _panelStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _toastStyle;
        private bool     _stylesReady;

        // ════════════════════════════════════════════
        // LIFECYCLE
        // ════════════════════════════════════════════

        private void Start()
        {
            _mainCamera = Camera.main;
            EventBus.Subscribe<MercyDecisionRequiredEvent>(OnMercyRequired);
            EventBus.Subscribe<MercyDecisionResolvedEvent>(OnMercyResolved);
            EventBus.Subscribe<VirtueGainedEvent>(OnVirtueGained);
            EventBus.Subscribe<AfflictionGainedEvent>(OnAfflictionGained);
            EventBus.Subscribe<HorrorRatingDrainEvent>(OnHorrorDrain);
            EventBus.Subscribe<PhobiaGainedEvent>(OnPhobiaGained);
            EventBus.Subscribe<ForbiddenKnowledgeEvent>(OnForbiddenKnowledge);
            EventBus.Subscribe<DreadContestEvent>(OnDreadContest);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<MercyDecisionRequiredEvent>(OnMercyRequired);
            EventBus.Unsubscribe<MercyDecisionResolvedEvent>(OnMercyResolved);
            EventBus.Unsubscribe<VirtueGainedEvent>(OnVirtueGained);
            EventBus.Unsubscribe<AfflictionGainedEvent>(OnAfflictionGained);
            EventBus.Unsubscribe<HorrorRatingDrainEvent>(OnHorrorDrain);
            EventBus.Unsubscribe<PhobiaGainedEvent>(OnPhobiaGained);
            EventBus.Unsubscribe<ForbiddenKnowledgeEvent>(OnForbiddenKnowledge);
            EventBus.Unsubscribe<DreadContestEvent>(OnDreadContest);
        }

        private void Update()
        {
            // Lazy-find managers (they may start after HUD)
            if (_battle     == null) _battle     = BattleManager.Instance;
            if (_directives == null) _directives = DirectiveSystem.Instance;

            // Unit selection via left-click
            HandleUnitSelection();

            // Toast timer
            if (_toasts.Count > 0)
            {
                _toastTimer -= Time.unscaledDeltaTime;
                if (_toastTimer <= 0f) _toasts.Dequeue();
            }
        }

        // ════════════════════════════════════════════
        // UNIT SELECTION (click to select)
        // ════════════════════════════════════════════

        private void HandleUnitSelection()
        {
            if (_mainCamera == null) return;
            if (GUIUtility.hotControl != 0) return;

            // Left-click: select a player unit
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    var unit = hit.collider.GetComponentInParent<UnitController>();
                    if (unit != null && unit.TeamId == 1 && unit.IsAlive)
                    {
                        _selectedUnit = unit;
                        return;
                    }
                }
                _selectedUnit = null;
            }

            // Right-click: Focus Fire on an enemy unit (1pt directive)
            if (Input.GetMouseButtonDown(1) && _directives != null)
            {
                Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    var unit = hit.collider.GetComponentInParent<UnitController>();
                    if (unit != null && unit.TeamId != 1 && unit.IsAlive)
                    {
                        bool applied = _directives.SpendDirective(DirectiveType.FocusFire, unit);
                        if (applied)
                            Debug.Log($"[HUD] Right-click Focus Fire → {unit.UnitName}");
                    }
                }
            }
        }

        // ════════════════════════════════════════════
        // OnGUI
        // ════════════════════════════════════════════

        private void OnGUI()
        {
            if (_battle == null || !_battle.IsBattleActive) return;

            EnsureStyles();

            if (_mercyPopupActive)
            {
                DrawMercyPopup();
                return; // Block other input during Mercy Decision
            }

            DrawDirectivesPanel();
            DrawSpeedControls();
            DrawSelectedUnitPanel();
            DrawToasts();
        }

        // ─── Directive Points + Mercy Tokens panel (top-left) ────────────────

        private void DrawDirectivesPanel()
        {
            if (_directives == null) return;

            bool focusActive = _directives.FocusFireTarget != null;
            int panelH = focusActive ? 104 : 82;
            Rect panel = new Rect(Margin, Margin, PanelW, panelH);
            GUI.Box(panel, GUIContent.none, _panelStyle);

            int pts    = _directives.DirectivePoints;
            int tokens = _directives.MercyTokens;
            int ix     = Margin + 10;
            int iy     = Margin + 8;

            GUI.Label(new Rect(ix, iy, PanelW - 20, 20),
                $"Directive Points: {pts}   |   Mercy Tokens: {tokens}", _labelStyle);
            iy += 22;

            // Battle timer
            string timer = _battle != null
                ? $"Battle: {_battle.BattleDuration:F0}s"
                : "Battle: --";
            GUI.Label(new Rect(ix, iy, PanelW - 20, 18), timer, _labelStyle);
            iy += 20;

            // Tip for Focus Fire
            GUI.Label(new Rect(ix, iy, PanelW - 20, 16),
                "Right-click enemy → Focus Fire (1pt)", _labelStyle);
            iy += 18;

            // Active Focus Fire status
            if (focusActive)
            {
                GUI.color = new Color(1f, 0.85f, 0.2f);
                GUI.Label(new Rect(ix, iy, PanelW - 20, 18),
                    $"FOCUS: {_directives.FocusFireTarget.UnitName} ({_directives.FocusFireTimer:F0}s)", _labelStyle);
                GUI.color = Color.white;
            }
        }

        // ─── Speed controls (top-right) ───────────────────────────────────────

        private void DrawSpeedControls()
        {
            if (_battle == null) return;

            float[] speeds  = { 0f, 0.5f, 1f, 2f, 4f };
            string[] labels = { "⏸", "½×", "1×", "2×", "4×" };

            int totalW = speeds.Length * (SpeedBtnW + 4);
            int x      = Screen.width - totalW - Margin;
            int y      = Margin;

            for (int i = 0; i < speeds.Length; i++)
            {
                Rect btn = new Rect(x + i * (SpeedBtnW + 4), y, SpeedBtnW, ButtonH);
                if (GUI.Button(btn, labels[i], _buttonStyle))
                {
                    if (speeds[i] == 0f)
                        _battle.PauseBattle();
                    else
                        _battle.SetBattleSpeed(speeds[i]);
                }
            }
        }

        // ─── Selected unit panel + directives (bottom-left) ───────────────────

        private void DrawSelectedUnitPanel()
        {
            if (_selectedUnit == null || !_selectedUnit.IsAlive) return;
            if (_directives == null) return;

            int panelH = 260;
            int panelY = Screen.height - panelH - Margin;
            Rect panel = new Rect(Margin, panelY, PanelW, panelH);
            GUI.Box(panel, GUIContent.none, _panelStyle);

            int iy = panelY + 10;
            int ix = Margin + 10;
            int lw = PanelW - 20;

            GUI.Label(new Rect(ix, iy, lw, 22), $"► {_selectedUnit.UnitName} [{_selectedUnit.UnitType}]", _labelStyle);
            iy += 24;

            GUI.Label(new Rect(ix, iy, lw, 18),
                $"HP {_selectedUnit.CurrentHP}/{_selectedUnit.MaxHP}  |  Sanity {_selectedUnit.CurrentSanity}/{_selectedUnit.MaxSanity}",
                _labelStyle);
            iy += 20;

            GUI.Label(new Rect(ix, iy, lw, 18),
                $"State: {_selectedUnit.SanityState}  |  Comp: {_selectedUnit.Comprehension:F1}",
                _labelStyle);
            iy += 24;

            GUI.Label(new Rect(ix, iy, lw, 18), "── Directives ──", _labelStyle);
            iy += 22;

            int pts = _directives.DirectivePoints;

            DrawDirectiveButton(ref iy, ix, lw, "Hold Position [1pt]", DirectiveType.HoldPosition, _selectedUnit, pts);
            DrawDirectiveButton(ref iy, ix, lw, "Fall Back [1pt]",     DirectiveType.FallBack,     _selectedUnit, pts);
            DrawDirectiveButton(ref iy, ix, lw, "Unleash [2pt]",       DirectiveType.Unleash,      _selectedUnit, pts);
            DrawDirectiveButton(ref iy, ix, lw, "Sacrifice [3pt]",     DirectiveType.Sacrifice,    _selectedUnit, pts);

            // Invoke Mercy (token cost, only usable on fallen units — shown greyed here)
            GUI.enabled = _directives.MercyTokens > 0;
            if (GUI.Button(new Rect(ix, iy, lw, ButtonH), $"Invoke Mercy [Token]", _buttonStyle))
                _directives.SpendDirective(DirectiveType.InvokeMercy, _selectedUnit);
            GUI.enabled = true;
        }

        private void DrawDirectiveButton(ref int iy, int ix, int lw,
            string label, DirectiveType type, UnitController target, int pts)
        {
            int cost = DirectiveSystem.GetDirectiveCost(type);
            GUI.enabled = pts >= cost;
            if (GUI.Button(new Rect(ix, iy, lw, ButtonH), label, _buttonStyle))
                _directives.SpendDirective(type, target);
            GUI.enabled = true;
            iy += ButtonH + 4;
        }

        // ─── Focus Fire: right-click enemy while selecting ─────────────────────
        // (Focus Fire targets an ENEMY; handled via right-click in Update then OnGUI)

        // Handled separately: player right-clicks an enemy unit to trigger Focus Fire.
        // Add to Update():

        // ════════════════════════════════════════════
        // MERCY DECISION POPUP
        // ════════════════════════════════════════════

        private void DrawMercyPopup()
        {
            // Darken background
            GUI.color = new Color(0, 0, 0, 0.6f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            int popW = 420;
            int popH = 240;
            int popX = (Screen.width  - popW) / 2;
            int popY = (Screen.height - popH) / 2;

            Rect popRect = new Rect(popX, popY, popW, popH);
            GUI.Box(popRect, GUIContent.none, _panelStyle);

            int iy = popY + 16;
            int ix = popX + 20;
            int lw = popW - 40;

            GUI.Label(new Rect(ix, iy, lw, 26), "⚠ MERCY DECISION", _labelStyle);
            iy += 28;

            GUI.Label(new Rect(ix, iy, lw, 22), $"{_mercyUnitName}  [{_mercyUnitType}]", _labelStyle);
            iy += 24;

            GUI.Label(new Rect(ix, iy, lw, 20), $"Expeditions survived: {_mercyExpeditions}", _labelStyle);
            iy += 22;

            GUI.Label(new Rect(ix, iy, lw, 20),
                _mercyTokensAvailable > 0
                    ? $"Mercy Tokens available: {_mercyTokensAvailable}"
                    : "No Mercy Tokens remaining.",
                _labelStyle);
            iy += 32;

            // Spend Token button
            GUI.enabled = _mercyTokensAvailable > 0;
            if (GUI.Button(new Rect(ix, iy, lw / 2 - 8, ButtonH + 4),
                "Spend Token\n(Revive 30% HP, +15 sanity)", _buttonStyle))
            {
                if (_directives != null)
                {
                    var unit = FindUnitById(_mercyUnitId);
                    if (unit != null)
                        _directives.SpendDirective(DirectiveType.InvokeMercy, unit);
                }
                _mercyPopupActive = false;
            }
            GUI.enabled = true;

            // Let die button
            if (GUI.Button(new Rect(ix + lw / 2 + 8, iy, lw / 2 - 8, ButtonH + 4),
                "Let Them Die\n(Permanent)", _buttonStyle))
            {
                _directives?.LetUnitDie();
                _mercyPopupActive = false;
            }
        }

        // ════════════════════════════════════════════
        // TOAST NOTIFICATIONS
        // ════════════════════════════════════════════

        private void DrawToasts()
        {
            if (_toasts.Count == 0) return;

            var toast = _toasts.Peek();
            int toastW = 320;
            int toastH = 44;
            int toastX = (Screen.width - toastW) / 2;
            int toastY = Screen.height / 4;

            GUI.color = new Color(toast.Colour.r, toast.Colour.g, toast.Colour.b, 0.88f);
            GUI.Box(new Rect(toastX, toastY, toastW, toastH), toast.Text, _toastStyle);
            GUI.color = Color.white;
        }

        private void PushToast(string text, Color colour)
        {
            _toasts.Enqueue(new ToastMessage { Text = text, Colour = colour });
            if (_toasts.Count == 1) _toastTimer = ToastDuration;
        }

        // ════════════════════════════════════════════
        // EVENT HANDLERS
        // ════════════════════════════════════════════

        private void OnMercyRequired(MercyDecisionRequiredEvent evt)
        {
            _mercyPopupActive      = true;
            _mercyUnitId           = evt.UnitId;
            _mercyUnitName         = evt.UnitName;
            _mercyUnitType         = evt.UnitType;
            _mercyExpeditions      = evt.ExpeditionCount;
            _mercyTokensAvailable  = evt.MercyTokensAvailable;
        }

        private void OnMercyResolved(MercyDecisionResolvedEvent evt)
        {
            _mercyPopupActive = false;
            string msg = evt.TokenSpent
                ? $"{FindUnitName(evt.UnitId)} was saved by Mercy."
                : $"{FindUnitName(evt.UnitId)} is gone forever.";
            PushToast(msg, evt.TokenSpent ? Color.cyan : Color.red);
        }

        private void OnVirtueGained(VirtueGainedEvent evt)
        {
            PushToast($"✦ {evt.UnitName}: VIRTUE — {evt.VirtueName}!", Color.yellow);
        }

        private void OnAfflictionGained(AfflictionGainedEvent evt)
        {
            PushToast($"✦ {evt.UnitName}: AFFLICTION — {evt.AfflictionName}.", Color.magenta);
        }

        private void OnHorrorDrain(HorrorRatingDrainEvent evt)
        {
            // Only show dramatic drain events (avoid spam)
            if (evt.SanityLost >= 6)
                PushToast($"Horror aura of {evt.RivalName} drains the battlefield.", new Color(0.5f, 0f, 0.8f));
        }

        private void OnPhobiaGained(PhobiaGainedEvent evt)
        {
            PushToast($"✦ {evt.UnitName}: PHOBIA — {evt.PhobiaName}!", new Color(1.0f, 0.45f, 0.1f));
        }

        private void OnForbiddenKnowledge(ForbiddenKnowledgeEvent evt)
        {
            PushToast($"✦ {evt.UnitName}: MaxSanity −{evt.MaxSanityLost} → {evt.NewMaxSanity} (Forbidden Knowledge)", new Color(0.4f, 0.8f, 1.0f));
        }

        private void OnDreadContest(DreadContestEvent evt)
        {
            if (evt.SanityDamage == 0)
            {
                PushToast($"✦ {evt.UnitName} resisted {evt.RivalName}'s taunt!", new Color(0.4f, 1.0f, 0.5f));
            }
            else if (evt.HesitationLock)
            {
                PushToast($"✦ {evt.UnitName} BROKEN by {evt.RivalName}! −{evt.SanityDamage} sanity. STUNNED.", new Color(0.9f, 0.2f, 0.9f));
            }
            else
            {
                PushToast($"✦ {evt.UnitName} shaken by {evt.RivalName}. −{evt.SanityDamage} sanity.", new Color(0.8f, 0.5f, 0.1f));
            }
        }

        // ════════════════════════════════════════════
        // HELPERS
        // ════════════════════════════════════════════

        private UnitController FindUnitById(int id)
        {
            return BattleManager.Instance?.GetUnitById(id);
        }

        private string FindUnitName(int id)
        {
            var u = FindUnitById(id);
            return u != null ? u.UnitName : "Unit";
        }

        private void EnsureStyles()
        {
            if (_stylesReady) return;

            // Semi-transparent dark panel
            var panelTex = new Texture2D(1, 1);
            panelTex.SetPixel(0, 0, new Color(0.05f, 0.05f, 0.08f, 0.88f));
            panelTex.Apply();

            _panelStyle = new GUIStyle(GUI.skin.box)
            {
                normal  = { background = panelTex },
                border  = new RectOffset(4, 4, 4, 4),
                padding = new RectOffset(8, 8, 8, 8)
            };

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 13,
                fontStyle = FontStyle.Normal,
                normal    = { textColor = new Color(0.9f, 0.85f, 0.95f) }
            };

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize  = 12,
                fontStyle = FontStyle.Bold,
                wordWrap  = true,
                normal    = { textColor = Color.white }
            };

            _toastStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize  = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = Color.white, background = panelTex }
            };

            _stylesReady = true;
        }
    }
}
