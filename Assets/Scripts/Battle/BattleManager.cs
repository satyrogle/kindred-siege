using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using KindredSiege.Core;
using KindredSiege.AI.BehaviourTree;
using KindredSiege.Rivalry;
using KindredSiege.UI;
using KindredSiege.City;

namespace KindredSiege.Battle
{
    /// <summary>
    /// Orchestrates the auto-battle: spawns units, ticks AI each frame,
    /// detects win/loss, and reports results.
    /// 
    /// Attach to a "BattleArena" GameObject in your battle scene.
    /// </summary>
    // TODO: Refactor — split into BattleSpawner, BattleFlowController, BattleRewardsCalculator (see peer review)
    public class BattleManager : MonoBehaviour
    {
        public static BattleManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private BattleGrid grid;
        [SerializeField] private Transform unitParent;

        [Header("Battle Config")]
        [SerializeField] private float battleTimeLimit = 120f; // Max seconds
        [SerializeField] private float battleSpeed = 1f;       // Playback speed multiplier

        [Header("Debug — Test Setup")]
        [SerializeField] private UnitData[] team1Units;
        [SerializeField] private UnitData[] team2Units;

        // Runtime state
        private List<UnitController> allUnits = new();
        private Dictionary<int, UnitController> _unitLookup = new();
        private List<UnitController> team1 = new();
        private List<UnitController> team2 = new();
        private bool battleActive = false;
        private float battleTimer = 0f;
        private int nextUnitId = 0;

        // Results
        public float BattleDuration => battleTimer;
        public bool IsBattleActive => battleActive;

        // ─── Team accessors (for GambitSetupPanel + FatigueSystem) ───
        /// <summary>Returns the UnitData asset array for team 1 (used by GambitSetupPanel).</summary>
        public UnitData[] GetTeam1Units() => team1Units;
        /// <summary>Returns the live UnitController list for team 1 (used by FatigueSystem).</summary>
        public List<UnitController> GetTeam1Controllers() => team1;
        public List<UnitController> GetTeam2Controllers() => team2;

        /// <summary>Fast lookup for systems finding units by ID.</summary>
        public UnitController GetUnitById(int id) => _unitLookup.GetValueOrDefault(id);

        // ─── Horror Rating (GDD §6.3) ───
        // The rival currently present on the battlefield. Set before StartBattle().
        // Their Horror Rating passively drains all player units every 5 seconds.
        private KindredSiege.Rivalry.RivalData _activeRival;
        private float _horrorRatingTimer = 0f;
        private const float HorrorRatingInterval = 5f;

        /// <summary>Assign the rival that will appear in this battle (for Horror Rating drain).</summary>
        public void SetActiveRival(KindredSiege.Rivalry.RivalData rival) => _activeRival = rival;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnStateChanged += OnGameStateChanged;

            // Subscribe to unit events for sanity propagation
            EventBus.Subscribe<UnitDefeatedEvent>(OnUnitDefeated);
            EventBus.Subscribe<UnitLostEvent>(OnUnitLost);
            // Reset runtime state on ScriptableObjects (prevents editor persistence bug)
            if (team1Units != null)
            {
                foreach (var data in team1Units)
                {
                    if (data != null)
                    {
                        data.FatigueLevel = 0;
                        data.ActivePhobia = PhobiaType.None;
                    }
                }
            }
            // Show gambit setup panel if present, otherwise auto-start after 1 second
            if (GambitSetupPanel.Instance != null)
                GambitSetupPanel.Instance.Show();
            else
                Invoke("StartBattle", 1f);
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnStateChanged -= OnGameStateChanged;

            EventBus.Unsubscribe<UnitDefeatedEvent>(OnUnitDefeated);
            EventBus.Unsubscribe<UnitLostEvent>(OnUnitLost);
        }

        private void OnGameStateChanged(GameManager.GameState from, GameManager.GameState to)
        {
            if (to == GameManager.GameState.BattlePhase)
                StartBattle();
        }

        private void Update()
        {
            if (!battleActive) return;

            battleTimer += Time.deltaTime * battleSpeed;

            // Inject FocusFire target before ticking AI (DirectiveSystem override)
            if (DirectiveSystem.Instance != null)
                DirectiveSystem.Instance.InjectFocusFireTarget(team1);

            // Tick all living units
            foreach (var unit in allUnits)
            {
                if (unit.IsAlive)
                {
                    unit.TickAI();
                }
            }

            // Horror Rating drain — every 5 seconds, active rival drains all player unit sanity
            TickHorrorRating();

            // Check win conditions
            CheckBattleEnd();
        }

        // ─── Battle Setup ───

        /// <summary>Start a battle with the configured teams.</summary>
        public void StartBattle()
        {
            ClearBattle();
            nextUnitId = 0;
            _horrorRatingTimer = 0f;

            // Prefer live roster from RosterManager (city-driven); fall back to Inspector array.
            // Assign back to team1Units so GambitSetupPanel.GetTeam1Units() sees the same list.
            var rosterMgr = RosterManager.Instance;
            if (rosterMgr != null && rosterMgr.RosterCount > 0)
                team1Units = rosterMgr.GetRosterAsArray();

            SpawnTeam(team1Units, 1, team1, grid.GetTeam1Zone());

            // Procedural enemy roster — RivalryEngine drives composition.
            // Falls back to the Inspector array when RivalryEngine is absent (editor testing).
            var enemies = (RivalryEngine.Instance != null)
                ? GenerateEnemyRoster()
                : team2Units;
            SpawnTeam(enemies, 2, team2, grid.GetTeam2Zone());

            // Scatter hazard tiles across the battlefield (GDD §12)
            GenerateHazards();

            // Apply pre-configured gambits to player team
            GambitSetupPanel.Instance?.ApplyGambitsToTeam(team1);

            battleTimer = 0f;
            battleActive = true;

            EventBus.Publish(new BattleStartEvent
            {
                BattleNumber = GameManager.Instance?.BattlesCompleted + 1 ?? 1,
                Season = GameManager.Instance?.CurrentSeason ?? 1
            });

            Debug.Log($"[Battle] Started! Team 1: {team1.Count} units | Team 2: {team2.Count} units");
        }

        /// <summary>Start a battle with custom unit lists (for city-to-battle bridge).</summary>
        public void StartBattle(UnitData[] playerUnits, UnitData[] enemyUnits)
        {
            team1Units = playerUnits;
            team2Units = enemyUnits;
            StartBattle();
        }

        private void SpawnTeam(UnitData[] unitDataList, int teamId, List<UnitController> teamList, List<Vector2Int> spawnZone)
        {
            if (unitDataList == null) return;

            for (int i = 0; i < unitDataList.Length && i < spawnZone.Count; i++)
            {
                var data = unitDataList[i];
                if (data == null) continue;

                // Create unit GameObject
                GameObject unitGO;
                if (data.Prefab != null)
                {
                    unitGO = Instantiate(data.Prefab, unitParent);
                }
                else
                {
                    // Placeholder: simple cube
                    unitGO = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    unitGO.transform.SetParent(unitParent);

                    // Colour by team
                    var renderer = unitGO.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        // Create a new material so URP works
                        var mat = new Material(Shader.Find("Standard"));
                        mat.color = teamId == 1
                            ? new Color(0.2f, 0.5f, 0.9f)
                            : new Color(0.9f, 0.3f, 0.2f);
                        renderer.material = mat;
                    }
                }

                unitGO.name = $"[Team{teamId}] {data.UnitName} #{nextUnitId}";

                // Add or get UnitController
                var controller = unitGO.GetComponent<UnitController>();
                if (controller == null)
                    controller = unitGO.AddComponent<UnitController>();

                controller.Initialise(data, teamId, nextUnitId);
                nextUnitId++;

                // Apply fatigue penalties for player team (GDD §11.4)
                if (teamId == 1 && !FatigueSystem.IsUndeployable(data))
                {
                    var (hpMult, dmgMult, extraHesitation) = FatigueSystem.GetFatigueModifiers(data.FatigueLevel);
                    if (hpMult < 1f || dmgMult < 1f)
                        controller.ApplyModifiers(hpMult, dmgMult);
                    controller.ExtraHesitationFromFatigue = extraHesitation;
                }

                // Attach health/sanity bars (player team only — prototype shows enemy bars too)
                var healthBar = unitGO.AddComponent<UnitHealthBar>();
                healthBar.Initialise(controller);

                // Place on grid
                grid.PlaceUnit(controller, spawnZone[i]);
                controller.SpawnPosition = controller.transform.position; // Record actual grid position

                teamList.Add(controller);
                allUnits.Add(controller);
                _unitLookup[controller.UnitId] = controller;

                // Set up battle context
                var context = new BattleContext
                {
                    Owner = controller,
                    Allies = teamList,
                    Enemies = teamId == 1 ? team2 : team1,
                    Grid = grid
                };
                controller.SetContext(context);
            }
        }

        // ─── Battle Resolution ───

        private void CheckBattleEnd()
        {
            bool team1Alive = team1.Any(u => u.IsAlive);
            bool team2Alive = team2.Any(u => u.IsAlive);

            BattleEndEvent.Result result;

            if (!team1Alive && !team2Alive)
                result = BattleEndEvent.Result.Draw;
            else if (!team2Alive)
                result = BattleEndEvent.Result.Victory;
            else if (!team1Alive)
                result = BattleEndEvent.Result.Defeat;
            else if (battleTimer >= battleTimeLimit)
                result = BattleEndEvent.Result.Draw; // Timeout
            else
                return; // Battle still in progress

            EndBattle(result);
        }

        // ─── Sanity Event Handlers ───

        /// <summary>
        /// When any unit dies, notify all living units on the same team.
        /// Each witness takes -15 sanity (GDD §5.2).
        /// </summary>
        private void OnUnitDefeated(UnitDefeatedEvent evt)
        {
            if (!battleActive) return;

            var deadTeam = evt.TeamId == 1 ? team1 : team2;

            foreach (var unit in deadTeam)
            {
                if (unit != null && unit.IsAlive && unit.UnitId != evt.UnitId)
                    unit.OnWitnessAllyDeath(null);
            }
        }

        /// <summary>
        /// When a unit is Lost (sanity = 0), ALL surviving units on both teams take -20 sanity.
        /// The horror of watching someone consumed by madness is universal (GDD §5.2).
        /// </summary>
        private void OnUnitLost(UnitLostEvent evt)
        {
            if (!battleActive) return;

            foreach (var unit in allUnits)
            {
                if (unit != null && unit.IsAlive && unit.UnitId != evt.UnitId)
                    unit.OnWitnessUnitLost();
            }
        }

        private void EndBattle(BattleEndEvent.Result result)
        {
            battleActive = false;

            // Victory sanity boost for surviving player units (GDD §5.2: +10 on win)
            if (result == BattleEndEvent.Result.Victory)
            {
                foreach (var unit in team1)
                {
                    if (unit != null && unit.IsAlive)
                        unit.OnBattleVictory();
                }
            }

            // Increment expedition count on surviving player units (veteran tracking)
            foreach (var unit in team1)
            {
                if (unit != null && unit.IsAlive && unit.Data != null)
                    unit.Data.ExpeditionCount++;
            }

            // Calculate KP earned
            int kpEarned = CalculateKP(result);

            Debug.Log($"[Battle] Ended: {result} | Duration: {battleTimer:F1}s | KP: {kpEarned}");

            EventBus.Publish(new BattleEndEvent
            {
                BattleResult = result,
                KPEarned = kpEarned,
                Duration = battleTimer
            });

            // Add KP to resources
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.Add(ResourceType.KindnessPoints, kpEarned);
            }

            // Transition to post-battle
            if (GameManager.Instance != null)
            {
                GameManager.Instance.EndBattle();
            }
        }

        private int CalculateKP(BattleEndEvent.Result result)
        {
            int baseKP = result switch
            {
                BattleEndEvent.Result.Victory => 30,
                BattleEndEvent.Result.Draw => 10,
                BattleEndEvent.Result.Defeat => 5,
                _ => 0
            };

            // Bonus KP from surviving Emissary units
            int emissaryBonus = team1
                .Where(u => u.IsAlive && u.Data != null && u.Data.IsCharityUnit)
                .Sum(u => u.Data.BonusKPPerSurvival);

            return baseKP + emissaryBonus;
        }

        // ─── Horror Rating Tick (GDD §6.3) ───

        /// <summary>
        /// Every 5 seconds, if a rival with Horror Rating > 0 is present,
        /// all living player units take sanity damage scaled by their Comprehension.
        /// </summary>
        private void TickHorrorRating()
        {
            if (_activeRival == null || _activeRival.HorrorRatingDrainPerTick <= 0) return;

            _horrorRatingTimer += Time.deltaTime * battleSpeed;
            if (_horrorRatingTimer < HorrorRatingInterval) return;

            _horrorRatingTimer = 0f;

            foreach (var unit in team1)
            {
                if (unit != null && unit.IsAlive)
                    unit.ApplyHorrorRatingDrain(_activeRival.HorrorRatingDrainPerTick, _activeRival.FullName);
            }

            Debug.Log($"[HorrorRating] {_activeRival.FullName} (HR {_activeRival.HorrorRating}) drained {_activeRival.HorrorRatingDrainPerTick} sanity from all player units.");
        }

        // ─── Battle Controls (UI hooks) ───

        public void SetBattleSpeed(float speed)
        {
            battleSpeed = Mathf.Clamp(speed, 0.5f, 4f);
            Time.timeScale = battleSpeed;
        }
        public void PauseBattle()
        {
            battleSpeed = 0f;
            Time.timeScale = 0f;
        }
        public void ResumeBattle()
        {
            battleSpeed = 1f;
            Time.timeScale = 1f;
        }
        // ─── Procedural Enemy Composition ───

        /// <summary>
        /// Build the enemy team dynamically each battle.
        ///
        /// Composition:
        ///   Leader — the pending rival (if any), stats drawn from RivalData.
        ///            Rank maps to a unit class archetype for BT purposes.
        ///   Fodder — filler units whose HP and damage scale with season and battle index.
        ///            Count grows by 1 each season (more pressure over time).
        ///
        /// All UnitData objects here are runtime ScriptableObject.CreateInstance<> —
        /// they are never written to disk and do not contaminate project assets.
        /// </summary>
        private UnitData[] GenerateEnemyRoster()
        {
            int season = GameManager.Instance?.CurrentSeason    ?? 1;
            int battle = GameManager.Instance?.BattlesCompleted ?? 0;

            var roster = new List<UnitData>();

            // ── Leader: pending rival (if an encounter was scheduled) ──
            var rival = RivalEncounterSystem.Instance?.PendingRival;
            if (rival != null)
            {
                var leader = ScriptableObject.CreateInstance<UnitData>();
                leader.UnitName     = rival.FullName;
                leader.UnitType     = RivalRankToUnitType(rival.Rank);
                leader.MaxHP        = rival.BaseHP  + rival.PromotionCount * 20;
                leader.AttackDamage = rival.BaseDamage + rival.PromotionCount * 3;
                leader.MoveSpeed    = 2.5f;
                leader.AttackRange  = 2f;
                leader.AttackCooldown = 1.2f;
                leader.BaseSanity   = 100;
                leader.Comprehension = 0.4f; // enemies are less susceptible to cosmic horror
                leader.TeamTint     = new Color(0.9f, 0.3f, 0.1f);
                roster.Add(leader);
                Debug.Log($"[Enemies] Leader spawned: {rival.FullName} [{rival.Rank}] " +
                          $"HP:{leader.MaxHP} DMG:{leader.AttackDamage}");
            }

            // ── Fodder: filler units scaling with season difficulty ──
            int maxSlots    = CityBattleBridge.Instance?.MaxUnitSlots ?? 4;
            int fodderCount = Mathf.Min(maxSlots - roster.Count, 2 + (season - 1));
            float scale     = 1f + (season - 1) * 0.15f + battle * 0.01f;

            for (int i = 0; i < fodderCount; i++)
            {
                var fodder = ScriptableObject.CreateInstance<UnitData>();
                fodder.UnitName      = s_FodderNames[i % s_FodderNames.Length];
                fodder.UnitType      = s_FodderTypes[i % s_FodderTypes.Length];
                fodder.MaxHP         = Mathf.RoundToInt(70 * scale);
                fodder.AttackDamage  = Mathf.RoundToInt(8  * scale);
                fodder.MoveSpeed     = 2.8f;
                fodder.AttackRange   = 1.8f;
                fodder.AttackCooldown = 1.1f;
                fodder.BaseSanity    = 100;
                fodder.Comprehension  = 0.4f;
                fodder.TeamTint      = new Color(0.65f, 0.2f, 0.2f);
                roster.Add(fodder);
            }

            // Safety: always at least 2 enemies
            while (roster.Count < 2)
            {
                var fallback = ScriptableObject.CreateInstance<UnitData>();
                fallback.UnitName    = "Drowned Wraith";
                fallback.UnitType    = "warden";
                fallback.MaxHP       = 70;
                fallback.AttackDamage = 8;
                fallback.MoveSpeed   = 2.8f;
                fallback.AttackRange = 1.8f;
                fallback.BaseSanity  = 100;
                fallback.Comprehension = 0.4f;
                fallback.TeamTint    = new Color(0.65f, 0.2f, 0.2f);
                roster.Add(fallback);
            }

            return roster.ToArray();
        }

        private static string RivalRankToUnitType(KindredSiege.Rivalry.RivalRank rank) => rank switch
        {
            KindredSiege.Rivalry.RivalRank.Overlord    => "vessel",
            KindredSiege.Rivalry.RivalRank.Captain     => "herald",
            KindredSiege.Rivalry.RivalRank.Lieutenant  => "occultist",
            _                                          => "warden"
        };

        private static readonly string[] s_FodderNames =
        {
            "Drowned Soldier", "Tide Wraith", "Hollow Guard",
            "Sunken Cultist",  "Shore Lurker"
        };

        private static readonly string[] s_FodderTypes =
        {
            "warden", "marksman", "berserker", "warden", "shadow"
        };

        // ─── Hazard Generation (GDD §12) ───

        /// <summary>
        /// Scatter terrain hazards across the neutral zone of the grid.
        ///   • 1 Shrine at dead centre (static — always present as a strategic objective)
        ///   • ~10% of neutral tiles become Deep Water (Harbour district flavour)
        ///   • ~5% of neutral tiles become Eldritch Ground (corruption seeping in)
        /// Spawn zones (left and right thirds) are kept clear so units aren't penalised at deploy.
        /// </summary>
        private void GenerateHazards()
        {
            if (grid == null) return;

            int w = grid.Width;
            int h = grid.Height;

            // Shrine — dead centre of the battlefield
            grid.SetHazard(w / 2, h / 2, HazardType.Shrine);

            // Neutral zone bounds (middle third, avoiding team spawn zones)
            int neutralStart = w / 3;
            int neutralEnd   = w - w / 3;

            for (int x = neutralStart; x < neutralEnd; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    // Skip the shrine tile
                    if (x == w / 2 && y == h / 2) continue;

                    float roll = Random.value;
                    if      (roll < 0.10f) grid.SetHazard(x, y, HazardType.DeepWater);
                    else if (roll < 0.15f) grid.SetHazard(x, y, HazardType.EldritchGround);
                }
            }

            Debug.Log("[Hazards] Battlefield hazards generated.");
        }

        private void ClearBattle()
        {
            foreach (var unit in allUnits)
            {
                if (unit != null)
                    Destroy(unit.gameObject);
            }
            allUnits.Clear();
            _unitLookup.Clear();
            team1.Clear();
            team2.Clear();
            grid.ClearGrid();
        }

        // ─── Debug ───

        [ContextMenu("Debug: Start Test Battle")]
        public void DebugStartBattle()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.LaunchBattle();
            else
                StartBattle();
        }
    }
}
