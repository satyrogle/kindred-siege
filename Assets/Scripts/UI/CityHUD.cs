using System.Linq;
using UnityEngine;
using KindredSiege.Core;
using KindredSiege.City;
using KindredSiege.Rivalry;

namespace KindredSiege.UI
{
    /// <summary>
    /// City Phase HUD — the player's home base between battles (GDD §Campaign Loop).
    ///
    /// Shown whenever GameState == CityPhase. Drives the full game loop:
    ///   Resources bar → Build / Upgrade → Rest Units → Deploy Expedition
    ///
    /// Panels:
    ///   Top bar    — Gold, Materials, Food, TechPoints, KP
    ///   Left       — Building shop (buy / upgrade)
    ///   Centre     — Season + battle progress, rival warning
    ///   Right      — Placed buildings summary
    ///   Bottom bar — "Rest Units" + "Deploy Expedition" buttons
    ///   Post-battle popup — brief summary of last battle rewards (fades after confirm)
    ///
    /// Uses OnGUI — no Canvas prefab required.
    /// Attach to the persistent Manager GameObject alongside GameManager.
    /// </summary>
    public class CityHUD : MonoBehaviour
    {
        public static CityHUD Instance { get; private set; }

        // ─── Managers ───
        private GameManager       _game;
        private ResourceManager   _res;
        private CityManager       _city;

        // ─── Post-battle popup ───
        private bool   _showPostBattle;
        private int    _lastGoldEarned;
        private int    _lastMatEarned;
        private string _lastBattleResult = "";

        // ─── Layout ───
        private const int Margin    = 12;
        private const int TopBarH   = 36;
        private const int BotBarH   = 48;
        private const int ShopW     = 280;
        private const int PlacedW   = 200;
        private const int BtnH      = 32;

        // ─── Styles ───
        private GUIStyle _panelStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _subStyle;
        private GUIStyle _btnStyle;
        private GUIStyle _warnStyle;
        private GUIStyle _greenStyle;
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
            _game = GameManager.Instance;
            _res  = ResourceManager.Instance;
            _city = CityManager.Instance;

            EventBus.Subscribe<BattleEndEvent>(OnBattleEnd);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<BattleEndEvent>(OnBattleEnd);
        }

        // ════════════════════════════════════════════
        // OnGUI
        // ════════════════════════════════════════════

        private void OnGUI()
        {
            // Only show during city phase
            if (_game == null || _game.CurrentState != GameManager.GameState.CityPhase) return;

            EnsureStyles();

            // Lazy-bind managers
            if (_res  == null) _res  = ResourceManager.Instance;
            if (_city == null) _city = CityManager.Instance;

            if (_showPostBattle)
            {
                DrawPostBattlePopup();
                return;
            }

            DrawTopBar();
            DrawShopPanel();
            DrawCentrePanel();
            DrawPlacedPanel();
            DrawBottomBar();
        }

        // ─── Top bar: resources ──────────────────────────────────────────────

        private void DrawTopBar()
        {
            GUI.Box(new Rect(0, 0, Screen.width, TopBarH), GUIContent.none, _panelStyle);

            int gold   = _res?.GetAmount(ResourceType.Gold)           ?? 0;
            int mat    = _res?.GetAmount(ResourceType.Materials)       ?? 0;
            int food   = _res?.GetAmount(ResourceType.Food)            ?? 0;
            int tech   = _res?.GetAmount(ResourceType.TechPoints)      ?? 0;
            int kp     = _res?.GetAmount(ResourceType.KindnessPoints)  ?? 0;

            string bar = $"Gold: {gold}   Materials: {mat}   Food: {food}   Tech: {tech}   KP: {kp}";
            GUI.Label(new Rect(Margin, 8, Screen.width - Margin * 2, 22), bar, _labelStyle);
        }

        // ─── Left panel: building shop ───────────────────────────────────────

        private void DrawShopPanel()
        {
            if (_city == null) return;

            int x = Margin;
            int y = TopBarH + Margin;
            int h = Screen.height - TopBarH - BotBarH - Margin * 3;

            GUI.Box(new Rect(x, y, ShopW, h), GUIContent.none, _panelStyle);

            int iy = y + 10;
            int ix = x + 10;
            int lw = ShopW - 20;

            GUI.Label(new Rect(ix, iy, lw, 22), "BUILDINGS", _titleStyle);
            iy += 26;

            foreach (var building in _city.Catalog)
            {
                if (iy + 60 > y + h - 10) break; // Overflow guard

                bool built       = _city.IsBuilt(building);
                int  level       = _city.GetLevel(building);
                bool canAfford   = _city.CanAfford(building);
                bool atMaxLevel  = built && level >= building.MaxLevel;

                // Name + description
                GUI.Label(new Rect(ix, iy, lw, 18), building.BuildingName, _labelStyle);
                iy += 18;
                GUI.Label(new Rect(ix, iy, lw, 16), building.Description, _subStyle);
                iy += 16;

                // Cost / level info
                string costLine = built
                    ? $"Level {level}/{building.MaxLevel}"
                    : $"Cost: {building.GoldCost}G" + (building.MaterialCost > 0 ? $" + {building.MaterialCost}M" : "");
                GUI.Label(new Rect(ix, iy, lw - 100, 18), costLine, _subStyle);

                // Buy / Upgrade button
                int btnX = ix + lw - 95;
                if (!built)
                {
                    GUI.enabled = canAfford;
                    if (GUI.Button(new Rect(btnX, iy - 2, 90, BtnH - 6), "Build", _btnStyle))
                        _city.PurchaseBuilding(building);
                    GUI.enabled = true;
                }
                else if (!atMaxLevel)
                {
                    var placed    = _city.PlacedBuildings.FirstOrDefault(p => p.Data == building);
                    int upgCost   = placed?.UpgradeCost ?? 0;
                    bool canUpg   = _res != null && _res.CanAfford(ResourceType.Gold, upgCost);
                    GUI.enabled   = canUpg;
                    if (GUI.Button(new Rect(btnX, iy - 2, 90, BtnH - 6), $"Upgrade\n{upgCost}G", _btnStyle))
                        _city.UpgradeBuilding(building);
                    GUI.enabled = true;
                }
                else
                {
                    GUI.Label(new Rect(btnX, iy, 90, 18), "MAX", _greenStyle);
                }

                iy += 24;

                // Divider
                GUI.color = new Color(0.3f, 0.3f, 0.4f);
                GUI.DrawTexture(new Rect(ix, iy, lw, 1), Texture2D.whiteTexture);
                GUI.color = Color.white;
                iy += 6;
            }
        }

        // ─── Centre panel: progress + rival warning ──────────────────────────

        private void DrawCentrePanel()
        {
            int x  = Margin + ShopW + Margin;
            int w  = Screen.width - x - PlacedW - Margin * 2;
            int y  = TopBarH + Margin;
            int h  = Screen.height - TopBarH - BotBarH - Margin * 3;

            GUI.Box(new Rect(x, y, w, h), GUIContent.none, _panelStyle);

            int ix = x + 16;
            int iy = y + 14;
            int lw = w - 32;

            int season   = _game?.CurrentSeason       ?? 1;
            int battles  = _game?.BattlesCompleted    ?? 0;
            int remaining = _game?.BattlesRemaining   ?? 8;

            GUI.Label(new Rect(ix, iy, lw, 24), $"Season {season}", _titleStyle);
            iy += 28;

            GUI.Label(new Rect(ix, iy, lw, 20),
                $"Battles completed: {battles}   |   Remaining: {remaining}", _labelStyle);
            iy += 24;

            // Battle progress bar
            DrawProgressBar(ix, iy, lw, 16, battles, battles + remaining);
            iy += 24;

            // Rival warning
            var pending = RivalEncounterSystem.Instance?.PendingRival;
            if (pending != null)
            {
                iy += 10;
                GUI.color = new Color(1f, 0.3f, 0.3f, 0.95f);
                GUI.Box(new Rect(ix, iy, lw, 54), GUIContent.none, _panelStyle);
                GUI.color = Color.white;

                GUI.Label(new Rect(ix + 8, iy + 6, lw - 16, 20),
                    $"⚠  RIVAL INCOMING: {pending.FullName}", _warnStyle);
                GUI.Label(new Rect(ix + 8, iy + 26, lw - 16, 18),
                    $"Rank: {pending.Rank}   |   Horror Rating: {pending.HorrorRating}   |   Traits: {string.Join(", ", pending.Traits)}",
                    _subStyle);

                iy += 62;

                // Avoid button (if not a season boss)
                bool isBoss = remaining <= 1;
                GUI.enabled = !isBoss;
                if (GUI.Button(new Rect(ix, iy, 160, BtnH), "Avoid Encounter", _btnStyle))
                    RivalEncounterSystem.Instance?.AvoidPendingEncounter();
                GUI.enabled = true;

                if (isBoss)
                    GUI.Label(new Rect(ix + 170, iy + 6, lw - 170, 20),
                        "Season boss cannot be avoided.", _subStyle);
            }
        }

        // ─── Right panel: placed buildings ───────────────────────────────────

        private void DrawPlacedPanel()
        {
            if (_city == null) return;

            int x = Screen.width - PlacedW - Margin;
            int y = TopBarH + Margin;
            int h = Screen.height - TopBarH - BotBarH - Margin * 3;

            GUI.Box(new Rect(x, y, PlacedW, h), GUIContent.none, _panelStyle);

            int ix = x + 10;
            int iy = y + 10;
            int lw = PlacedW - 20;

            GUI.Label(new Rect(ix, iy, lw, 22), "BUILT", _titleStyle);
            iy += 26;

            if (_city.PlacedBuildings.Count == 0)
            {
                GUI.Label(new Rect(ix, iy, lw, 20), "Nothing built yet.", _subStyle);
                return;
            }

            foreach (var pb in _city.PlacedBuildings)
            {
                if (iy + 38 > y + h - 10) break;

                GUI.Label(new Rect(ix, iy, lw, 18), $"{pb.Data.BuildingName}  Lv{pb.Level}", _labelStyle);
                iy += 18;

                string prod = pb.Data.ProductionAmount > 0
                    ? $"+{Mathf.RoundToInt(pb.Data.ProductionAmount * pb.ProductionMultiplier)} {pb.Data.ProducesResource}/phase"
                    : "";
                if (!string.IsNullOrEmpty(prod))
                {
                    GUI.Label(new Rect(ix, iy, lw, 16), prod, _subStyle);
                    iy += 16;
                }
                iy += 4;
            }
        }

        // ─── Bottom bar: action buttons ──────────────────────────────────────

        private void DrawBottomBar()
        {
            int barY = Screen.height - BotBarH - Margin;
            GUI.Box(new Rect(0, barY, Screen.width, BotBarH + Margin), GUIContent.none, _panelStyle);

            // Rest Units
            if (GUI.Button(new Rect(Margin, barY + 8, 160, BtnH), "Rest Units", _btnStyle))
            {
                if (CityRestPanel.Instance != null)
                    CityRestPanel.Instance.Show();
            }

            // Manage Roster
            if (GUI.Button(new Rect(Margin + 170, barY + 8, 160, BtnH), "Manage Roster", _btnStyle))
            {
                if (UnitRecruitPanel.Instance != null)
                    UnitRecruitPanel.Instance.Show();
            }

            // Deploy Expedition
            int deployX = Screen.width - 220 - Margin;
            GUI.color = new Color(0.3f, 0.8f, 0.4f);
            if (GUI.Button(new Rect(deployX, barY + 8, 220, BtnH), "DEPLOY EXPEDITION", _btnStyle))
            {
                GUI.color = Color.white;
                _game?.StartBattle(); // → PreBattle → GambitSetupPanel shows
            }
            GUI.color = Color.white;
        }

        // ─── Post-battle popup ───────────────────────────────────────────────

        private void DrawPostBattlePopup()
        {
            int popW = 400, popH = 200;
            int popX = (Screen.width  - popW) / 2;
            int popY = (Screen.height - popH) / 2;

            GUI.Box(new Rect(popX, popY, popW, popH), GUIContent.none, _panelStyle);

            int iy = popY + 18;
            int ix = popX + 20;
            int lw = popW - 40;

            GUI.Label(new Rect(ix, iy, lw, 26), $"BATTLE RESULT: {_lastBattleResult.ToUpper()}", _titleStyle);
            iy += 30;

            GUI.Label(new Rect(ix, iy, lw, 22), $"Gold earned:      +{_lastGoldEarned}", _labelStyle);
            iy += 24;
            GUI.Label(new Rect(ix, iy, lw, 22), $"Materials earned: +{_lastMatEarned}", _labelStyle);
            iy += 32;

            if (GUI.Button(new Rect(ix + lw / 2 - 80, iy, 160, 36), "Return to City", _btnStyle))
            {
                _showPostBattle = false;
                // Route to SeasonEnd if this was the last battle; otherwise back to city
                if (_game != null && _game.BattlesRemaining <= 0)
                    _game.TriggerSeasonEnd();
                else
                    _game?.ReturnToCity();
            }
        }

        // ════════════════════════════════════════════
        // EVENT HANDLERS
        // ════════════════════════════════════════════

        private void OnBattleEnd(BattleEndEvent evt)
        {
            _lastBattleResult = evt.BattleResult.ToString();

            // Snapshot the rewards (CityBattleBridge already added them, we just display)
            _lastGoldEarned = evt.BattleResult switch
            {
                BattleEndEvent.Result.Victory => 100,
                BattleEndEvent.Result.Draw    => 30,
                _                             => 10
            };
            _lastMatEarned = evt.BattleResult switch
            {
                BattleEndEvent.Result.Victory => 50,
                BattleEndEvent.Result.Draw    => 15,
                _                             => 5
            };

            _showPostBattle = true;
            // State transition happens when the player clicks "Return to City" in the popup.
        }

        // ════════════════════════════════════════════
        // HELPERS
        // ════════════════════════════════════════════

        private void DrawProgressBar(int x, int y, int w, int h, int current, int max)
        {
            GUI.color = new Color(0.1f, 0.1f, 0.15f);
            GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);

            if (max > 0)
            {
                GUI.color = new Color(0.3f, 0.7f, 0.4f);
                GUI.DrawTexture(new Rect(x, y, w * ((float)current / max), h), Texture2D.whiteTexture);
            }

            GUI.color = Color.white;
        }

        private void EnsureStyles()
        {
            if (_stylesReady) return;

            var panelTex = new Texture2D(1, 1);
            panelTex.SetPixel(0, 0, new Color(0.05f, 0.05f, 0.10f, 0.92f));
            panelTex.Apply();

            _panelStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = panelTex },
                border = new RectOffset(4, 4, 4, 4)
            };

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 14,
                fontStyle = FontStyle.Bold,
                normal    = { textColor = new Color(0.95f, 0.92f, 1.0f) }
            };

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 12,
                fontStyle = FontStyle.Normal,
                normal    = { textColor = new Color(0.88f, 0.86f, 0.94f) }
            };

            _subStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 11,
                wordWrap  = true,
                normal    = { textColor = new Color(0.62f, 0.60f, 0.70f) }
            };

            _warnStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 13,
                fontStyle = FontStyle.Bold,
                normal    = { textColor = new Color(1.0f, 0.35f, 0.35f) }
            };

            _greenStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = new Color(0.3f, 0.9f, 0.4f) }
            };

            _btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize  = 12,
                fontStyle = FontStyle.Bold,
                wordWrap  = true,
                normal    = { textColor = Color.white }
            };

            _stylesReady = true;
        }
    }
}
