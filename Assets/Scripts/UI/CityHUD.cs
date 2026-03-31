using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KindredSiege.Battle;
using KindredSiege.Core;
using KindredSiege.City;
using KindredSiege.Rivalry;
using KindredSiege.Units;

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

        // ─── Post-battle panel ───
        private bool   _showPostBattle;
        private string _lastBattleResult = "";
        private string _lastEncounterType = "";
        private int    _lastGoldEarned;
        private int    _lastMatEarned;
        private int    _lastKPEarned;
        private float  _lastBattleDuration;
        private int    _mythosExposureDelta;
        private string _rivalOutcome = "";
        private List<UnitBattleSnapshot> _unitSnapshots = new();

        private struct UnitBattleSnapshot
        {
            public string Name;
            public string UnitType;
            public int    HPRemaining;
            public int    MaxHP;
            public int    SanityRemaining;
            public int    FatigueGained;
            public bool   HasPhobia;
            public string PhobiaName;
            public bool   NewBond;
            public bool   Survived;
        }

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
            // Show during city phase, or during PostBattle to display the popup
            if (_game == null || (_game.CurrentState != GameManager.GameState.CityPhase && _game.CurrentState != GameManager.GameState.PostBattle)) return;

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

        // ─── Left panel: building shop (grouped by district) ─────────────────

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

            var dm = DistrictManager.Instance;

            foreach (DistrictType district in System.Enum.GetValues(typeof(DistrictType)))
            {
                if (iy + 24 > y + h - 10) break;

                bool unlocked = dm == null || dm.IsUnlocked(district);

                // ─── District header ───────────────────────────────────────────
                GUI.color = unlocked ? new Color(0.68f, 0.84f, 1.0f) : new Color(0.42f, 0.42f, 0.52f);
                string districtHeader = unlocked
                    ? DistrictManager.GetName(district)
                    : $"{DistrictManager.GetName(district)}  [LOCKED]";
                GUI.Label(new Rect(ix, iy, lw, 18), districtHeader, _labelStyle);
                GUI.color = Color.white;
                iy += 20;

                if (!unlocked)
                {
                    GUI.Label(new Rect(ix + 8, iy, lw - 8, 16),
                        DistrictManager.GetUnlockHint(district), _subStyle);
                    iy += 18;
                    GUI.color = new Color(0.25f, 0.25f, 0.35f);
                    GUI.DrawTexture(new Rect(ix, iy, lw, 2), Texture2D.whiteTexture);
                    GUI.color = Color.white;
                    iy += 8;
                    continue;
                }

                // ─── Buildings in this district ────────────────────────────────
                foreach (var building in _city.Catalog)
                {
                    if (building.District != district) continue;
                    if (iy + 60 > y + h - 10) break;

                    bool built      = _city.IsBuilt(building);
                    int  level      = _city.GetLevel(building);
                    bool canAfford  = _city.CanAfford(building);
                    bool atMaxLevel = built && level >= building.MaxLevel;

                    GUI.Label(new Rect(ix, iy, lw, 18), building.BuildingName, _labelStyle);
                    iy += 18;
                    GUI.Label(new Rect(ix, iy, lw, 16), building.Description, _subStyle);
                    iy += 16;

                    string costLine = built
                        ? $"Level {level}/{building.MaxLevel}"
                        : $"Cost: {building.GoldCost}G" + (building.MaterialCost > 0 ? $" + {building.MaterialCost}M" : "");
                    GUI.Label(new Rect(ix, iy, lw - 100, 18), costLine, _subStyle);

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
                        var placed  = _city.PlacedBuildings.FirstOrDefault(p => p.Data == building);
                        int upgCost = placed?.UpgradeCost ?? 0;
                        bool canUpg = _res != null && _res.CanAfford(ResourceType.Gold, upgCost);
                        GUI.enabled = canUpg;
                        if (GUI.Button(new Rect(btnX, iy - 2, 90, BtnH - 6), $"Upgrade\n{upgCost}G", _btnStyle))
                            _city.UpgradeBuilding(building);
                        GUI.enabled = true;
                    }
                    else
                    {
                        GUI.Label(new Rect(btnX, iy, 90, 18), "MAX", _greenStyle);
                    }

                    iy += 24;
                    GUI.color = new Color(0.28f, 0.28f, 0.38f);
                    GUI.DrawTexture(new Rect(ix, iy, lw, 1), Texture2D.whiteTexture);
                    GUI.color = Color.white;
                    iy += 6;
                }

                // District section end divider (thicker)
                GUI.color = new Color(0.35f, 0.35f, 0.50f);
                GUI.DrawTexture(new Rect(ix, iy, lw, 2), Texture2D.whiteTexture);
                GUI.color = Color.white;
                iy += 8;
            }
        }

        // ─── Centre panel: progress + mythos + rival warning + bonds ─────────

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

            int season    = _game?.CurrentSeason    ?? 1;
            int battles   = _game?.BattlesCompleted ?? 0;
            int remaining = _game?.BattlesRemaining ?? 8;

            GUI.Label(new Rect(ix, iy, lw, 24), $"Season {season}", _titleStyle);
            iy += 28;

            GUI.Label(new Rect(ix, iy, lw, 20),
                $"Battles completed: {battles}   |   Remaining: {remaining}", _labelStyle);
            iy += 24;

            // Battle progress bar
            DrawProgressBar(ix, iy, lw, 16, battles, battles + remaining);
            iy += 28;

            // ── Mythos Exposure ──────────────────────────────────────────────
            Divider(ix, iy, lw); iy += 10;
            GUI.Label(new Rect(ix, iy, lw, 18), "MYTHOS EXPOSURE", _titleStyle);
            iy += 22;

            var mythos   = KindredSiege.City.MythosExposure.Instance;
            int exposure = mythos?.Exposure ?? 0;
            var tier     = mythos?.GetTier() ?? KindredSiege.City.ExposureTier.Initiate;
            string tierName = mythos?.GetTierName() ?? "Initiate";

            Color mythosColor = tier switch
            {
                KindredSiege.City.ExposureTier.Initiate  => new Color(0.25f, 0.75f, 0.35f),
                KindredSiege.City.ExposureTier.Acolyte   => new Color(0.88f, 0.82f, 0.25f),
                KindredSiege.City.ExposureTier.Scholar   => new Color(0.92f, 0.55f, 0.15f),
                KindredSiege.City.ExposureTier.Adept     => new Color(0.88f, 0.18f, 0.12f),
                _                                         => new Color(0.58f, 0.10f, 0.78f)
            };

            GUI.color = new Color(0.10f, 0.10f, 0.15f);
            GUI.DrawTexture(new Rect(ix, iy, lw, 16), Texture2D.whiteTexture);
            if (exposure > 0)
            {
                GUI.color = mythosColor;
                GUI.DrawTexture(new Rect(ix, iy, lw * (exposure / 100f), 16), Texture2D.whiteTexture);
            }
            GUI.color = Color.white;
            GUI.Label(new Rect(ix + 4, iy, lw - 4, 16), $"{exposure}/100  [{tierName}]", _subStyle);
            iy += 20;

            string effectStr = tier switch
            {
                KindredSiege.City.ExposureTier.Initiate  => "No current effect.",
                KindredSiege.City.ExposureTier.Acolyte   => "Battle start: -5 sanity to all units.",
                KindredSiege.City.ExposureTier.Scholar   => "Battle start: -10 sanity.  Rival Dread +2.",
                KindredSiege.City.ExposureTier.Adept     => "Battle start: -15 sanity.  City sanity draining.",
                _                                         => "THE CITY HAS FALLEN.",
            };
            GUI.color = tier >= KindredSiege.City.ExposureTier.Acolyte
                ? mythosColor : new Color(0.50f, 0.50f, 0.60f);
            GUI.Label(new Rect(ix, iy, lw, 18), effectStr, _subStyle);
            GUI.color = Color.white;
            iy += 26;

            // ── Rival warning ─────────────────────────────────────────────────
            var pending = RivalEncounterSystem.Instance?.PendingRival;
            if (pending != null)
            {
                iy += 4;
                GUI.color = new Color(1f, 0.3f, 0.3f, 0.95f);
                GUI.Box(new Rect(ix, iy, lw, 54), GUIContent.none, _panelStyle);
                GUI.color = Color.white;

                GUI.Label(new Rect(ix + 8, iy + 6, lw - 16, 20),
                    $"⚠  RIVAL INCOMING: {pending.FullName}", _warnStyle);
                GUI.Label(new Rect(ix + 8, iy + 26, lw - 16, 18),
                    $"Rank: {pending.Rank}   |   Horror Rating: {pending.HorrorRating}   |   Traits: {string.Join(", ", pending.Traits)}",
                    _subStyle);
                iy += 62;

                bool isBoss = remaining <= 1;
                GUI.enabled = !isBoss;
                if (GUI.Button(new Rect(ix, iy, 160, BtnH), "Avoid Encounter", _btnStyle))
                    RivalEncounterSystem.Instance?.AvoidPendingEncounter();
                GUI.enabled = true;

                if (isBoss)
                    GUI.Label(new Rect(ix + 170, iy + 6, lw - 170, 20),
                        "Season boss cannot be avoided.", _subStyle);

                iy += BtnH + 8;
            }

            // ── Unit Bonds ────────────────────────────────────────────────────
            Divider(ix, iy, lw); iy += 10;
            GUI.Label(new Rect(ix, iy, lw, 18), "UNIT BONDS", _titleStyle);
            iy += 22;

            var activeRoster = KindredSiege.Battle.RosterManager.Instance?.ActiveRoster;
            if (activeRoster != null && activeRoster.Count > 0)
            {
                var shownPairs = new HashSet<string>();
                bool anyShown  = false;

                // Formed bonds
                foreach (var ud in activeRoster)
                {
                    if (ud?.BondedWith == null) continue;
                    foreach (var partnerAssetName in ud.BondedWith)
                    {
                        string key = string.CompareOrdinal(ud.name, partnerAssetName) < 0
                            ? $"{ud.name}|{partnerAssetName}"
                            : $"{partnerAssetName}|{ud.name}";
                        if (!shownPairs.Add(key)) continue;

                        string partnerDisplay = activeRoster
                            .FirstOrDefault(u => u != null && u.name == partnerAssetName)?.UnitName
                            ?? partnerAssetName;

                        GUI.color = new Color(0.65f, 0.88f, 1.0f);
                        GUI.Label(new Rect(ix, iy, lw, 18),
                            $"♥  {ud.UnitName}  ↔  {partnerDisplay}", _labelStyle);
                        GUI.color = Color.white;
                        iy += 20;
                        anyShown = true;
                    }
                }

                // Co-survival progress toward bond
                foreach (var ud in activeRoster)
                {
                    if (ud?.CoSurvivedWith == null) continue;
                    var seenCo = new HashSet<string>();
                    foreach (var coName in ud.CoSurvivedWith)
                    {
                        if (!seenCo.Add(coName)) continue;
                        if (ud.BondedWith != null && ud.BondedWith.Contains(coName)) continue;

                        string key = string.CompareOrdinal(ud.name, coName) < 0
                            ? $"{ud.name}|{coName}"
                            : $"{coName}|{ud.name}";
                        if (!shownPairs.Add(key)) continue;

                        int count = ud.CoSurvivedWith.Count(s => s == coName);
                        if (count <= 0) continue;

                        string partnerDisplay = activeRoster
                            .FirstOrDefault(u => u != null && u.name == coName)?.UnitName ?? coName;

                        GUI.color = new Color(0.70f, 0.70f, 0.80f);
                        GUI.Label(new Rect(ix, iy, lw, 18),
                            $"  {ud.UnitName}  ↔  {partnerDisplay}:  {count}/{KindredSiege.Units.BondSystem.BondThreshold}",
                            _subStyle);
                        GUI.color = Color.white;
                        iy += 18;
                        anyShown = true;
                    }
                }

                if (!anyShown)
                {
                    GUI.Label(new Rect(ix, iy, lw, 18), "No bonds formed yet.", _subStyle);
                }
            }
            else
            {
                GUI.Label(new Rect(ix, iy, lw, 18), "No roster assigned.", _subStyle);
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

            // Talent Trees
            if (GUI.Button(new Rect(Margin + 340, barY + 8, 140, BtnH), "Talent Trees", _btnStyle))
            {
                var roster = KindredSiege.Battle.RosterManager.Instance?.ActiveRoster?.ToList();
                if (roster != null && roster.Count > 0)
                {
                    if (TalentTreePanel.Instance == null)
                        gameObject.AddComponent<TalentTreePanel>();
                    TalentTreePanel.Instance?.Show(roster);
                }
            }

            // Inspect Dominions (The War Table)
            if (GUI.Button(new Rect(Margin + 490, barY + 8, 160, BtnH), "Inspect Dominions", _btnStyle))
            {
                // Auto-attach if missing from scene hierarchy so player doesn't have to manually bind
                if (RivalryBoardPanel.Instance == null)
                    gameObject.AddComponent<RivalryBoardPanel>();
                
                RivalryBoardPanel.Instance?.Show();
            }

            // To The Lighthouse (Expedition Map)
            int deployX = Screen.width - 220 - Margin;
            GUI.color = new Color(0.3f, 0.8f, 0.4f);
            if (GUI.Button(new Rect(deployX, barY + 8, 220, BtnH), "TO THE LIGHTHOUSE", _btnStyle))
            {
                GUI.color = Color.white;
                if (LighthouseMapPanel.Instance == null)
                    gameObject.AddComponent<LighthouseMapPanel>();
                LighthouseMapPanel.Instance?.Show();
            }
            GUI.color = Color.white;
        }

        // ─── Post-battle panel ───────────────────────────────────────────────

        private void DrawPostBattlePopup()
        {
            int popW = 700, popH = 520;
            int popX = (Screen.width  - popW) / 2;
            int popY = (Screen.height - popH) / 2;

            // Backdrop
            GUI.color = new Color(0f, 0f, 0f, 0.65f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUI.Box(new Rect(popX, popY, popW, popH), GUIContent.none, _panelStyle);

            int ix = popX + 24;
            int iy = popY + 18;
            int lw = popW - 48;

            // ── Title ──
            bool isVictory = _lastBattleResult == "Victory";
            bool isDefeat  = _lastBattleResult == "Defeat";
            GUI.color = isVictory ? new Color(0.5f, 1.0f, 0.55f)
                      : isDefeat  ? new Color(1.0f, 0.4f, 0.35f)
                                  : new Color(0.9f, 0.85f, 0.45f);
            GUI.Label(new Rect(ix, iy, lw, 30), $"{_lastBattleResult.ToUpper()}", _titleStyle);
            GUI.color = Color.white;

            GUI.Label(new Rect(ix + 120, iy, lw - 120, 30),
                $"  {_lastEncounterType}   {_lastBattleDuration:F0}s", _labelStyle);
            iy += 34;

            Divider(ix, iy, lw); iy += 10;

            // ── Two-column layout ──
            int colW = (lw - 16) / 2;

            // ── LEFT: Unit roster ──
            GUI.Label(new Rect(ix, iy, colW, 20), "ROSTER", _titleStyle);
            int unitY = iy + 24;

            foreach (var snap in _unitSnapshots)
            {
                Color rowCol = !snap.Survived    ? new Color(0.55f, 0.55f, 0.58f)
                             : snap.HasPhobia    ? new Color(0.95f, 0.5f, 0.3f)
                             : snap.FatigueGained >= 20 ? new Color(0.9f, 0.8f, 0.3f)
                                                        : new Color(0.7f, 0.9f, 0.7f);
                GUI.color = rowCol;

                string status = !snap.Survived ? "LOST"
                              : snap.HasPhobia ? $"PHOBIA:{snap.PhobiaName}"
                              : snap.NewBond   ? "BOND!"
                                               : $"+{snap.FatigueGained}fatigue";

                GUI.Label(new Rect(ix, unitY, colW - 10, 18),
                    $"{snap.Name}  HP:{snap.HPRemaining}/{snap.MaxHP}  SAN:{snap.SanityRemaining}  {status}",
                    _labelStyle);
                GUI.color = Color.white;
                unitY += 20;
            }

            // ── RIGHT: Rewards + context ──
            int rx = ix + colW + 16;
            int ry = iy;

            GUI.Label(new Rect(rx, ry, colW, 20), "REWARDS", _titleStyle);
            ry += 24;

            GUI.color = new Color(0.95f, 0.85f, 0.35f);
            GUI.Label(new Rect(rx, ry, colW, 18), $"Gold        +{_lastGoldEarned}", _labelStyle);
            ry += 20;
            GUI.color = new Color(0.6f, 0.75f, 0.9f);
            GUI.Label(new Rect(rx, ry, colW, 18), $"Materials   +{_lastMatEarned}", _labelStyle);
            ry += 20;
            GUI.color = new Color(0.5f, 0.9f, 0.6f);
            GUI.Label(new Rect(rx, ry, colW, 18), $"KP          +{_lastKPEarned}", _labelStyle);
            ry += 28;
            GUI.color = Color.white;

            if (!string.IsNullOrEmpty(_rivalOutcome))
            {
                Divider(rx, ry, colW); ry += 10;
                GUI.Label(new Rect(rx, ry, colW, 18), "RIVAL", _titleStyle);
                ry += 22;
                GUI.Label(new Rect(rx, ry, colW, 36), _rivalOutcome, _subStyle);
                ry += 40;
            }

            if (_mythosExposureDelta != 0)
            {
                Divider(rx, ry, colW); ry += 10;
                GUI.color = _mythosExposureDelta > 0 ? new Color(0.9f, 0.4f, 0.8f) : new Color(0.4f, 0.8f, 0.5f);
                string mythosLabel = _mythosExposureDelta > 0
                    ? $"Mythos Exposure  +{_mythosExposureDelta}  ({KindredSiege.City.MythosExposure.Instance?.GetTierName() ?? ""})"
                    : $"Mythos Exposure  {_mythosExposureDelta}  ({KindredSiege.City.MythosExposure.Instance?.GetTierName() ?? ""})";
                GUI.Label(new Rect(rx, ry, colW, 20), mythosLabel, _labelStyle);
                GUI.color = Color.white;
            }

            // ── Bottom button ──
            int btnY = popY + popH - 54;
            Divider(ix, btnY, lw); btnY += 10;

            string btnLabel = (_game != null && _game.BattlesRemaining <= 0)
                ? "End of Season →"
                : "Return to City";

            int btnW = 200;
            GUI.color = isVictory ? new Color(0.3f, 0.75f, 0.4f) : new Color(0.55f, 0.55f, 0.6f);
            if (GUI.Button(new Rect(popX + (popW - btnW) / 2, btnY, btnW, 36), btnLabel, _btnStyle))
            {
                GUI.color = Color.white;
                _showPostBattle = false;
                if (_game != null && _game.BattlesRemaining <= 0)
                    _game.TriggerSeasonEnd();
                else
                    _game?.ReturnToCity();
            }
            GUI.color = Color.white;
        }

        private static void Divider(int x, int y, int w)
        {
            GUI.color = new Color(0.35f, 0.35f, 0.45f, 0.7f);
            GUI.DrawTexture(new Rect(x, y, w, 1), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        // ════════════════════════════════════════════
        // EVENT HANDLERS
        // ════════════════════════════════════════════

        private void OnBattleEnd(BattleEndEvent evt)
        {
            _lastBattleResult   = evt.BattleResult.ToString();
            _lastKPEarned       = evt.KPEarned;
            _lastBattleDuration = evt.Duration;

            var bm = KindredSiege.Battle.BattleManager.Instance;
            _lastEncounterType = bm != null
                ? KindredSiege.Battle.EncounterTypeInfo.GetName(bm.ActiveEncounterType)
                : "";

            // Rewards — match what CityBattleBridge actually grants
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

            // Rival outcome
            var rival = bm?.GetActiveRival();
            if (rival != null)
            {
                bool rivalDead = bm.GetTeam2Controllers()
                    .All(u => u == null || !u.IsAlive);
                _rivalOutcome = rivalDead
                    ? $"{rival.FullName} [{rival.Rank}] — DEFEATED"
                    : $"{rival.FullName} [{rival.Rank}] — escaped";
            }
            else
            {
                _rivalOutcome = "";
            }

            // Mythos exposure delta
            int exposureBefore = KindredSiege.City.MythosExposure.Instance?.Exposure ?? 0;
            _mythosExposureDelta = evt.BattleResult switch
            {
                BattleEndEvent.Result.Victory => -2,
                BattleEndEvent.Result.Draw    =>  3,
                _                             =>  8
            };
            // (MythosExposure already updated itself via its own BattleEndEvent subscription)

            // Unit snapshots — must be captured before BattleManager clears units
            _unitSnapshots.Clear();
            var team1 = bm?.GetTeam1Controllers();
            if (team1 != null)
            {
                var roster = KindredSiege.Battle.RosterManager.Instance;
                foreach (var uc in team1)
                {
                    if (uc == null || uc.Data == null) continue;
                    // A "new bond" formed this battle = co-survival count just hit the threshold
                    bool newBond = false;
                    if (uc.IsAlive && uc.Data.BondedWith.Count > 0 && roster != null)
                    {
                        foreach (var partnerName in uc.Data.BondedWith)
                        {
                            var partner = roster.ActiveRoster.FirstOrDefault(u => u != null && u.name == partnerName);
                            if (partner != null &&
                                BondSystem.GetCoSurvivalCount(uc.Data, partner) == BondSystem.BondThreshold)
                            { newBond = true; break; }
                        }
                    }

                    _unitSnapshots.Add(new UnitBattleSnapshot
                    {
                        Name            = uc.UnitName,
                        UnitType        = uc.UnitType,
                        HPRemaining     = uc.CurrentHP,
                        MaxHP           = uc.MaxHP,
                        SanityRemaining = uc.CurrentSanity,
                        FatigueGained   = uc.Data != null ? uc.Data.FatigueLevel : 0,
                        HasPhobia       = uc.Data != null && uc.Data.ActivePhobia != KindredSiege.Battle.PhobiaType.None,
                        PhobiaName      = uc.Data?.ActivePhobia.ToString() ?? "",
                        NewBond         = newBond,
                        Survived        = uc.IsAlive
                    });
                }
            }

            _showPostBattle = true;
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
