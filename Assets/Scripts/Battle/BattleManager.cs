using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using KindredSiege.Core;
using KindredSiege.AI.BehaviourTree;
using KindredSiege.Rivalry;

namespace KindredSiege.Battle
{
    /// <summary>
    /// Orchestrates the auto-battle: spawns units, ticks AI each frame,
    /// detects win/loss, and reports results.
    /// 
    /// Attach to a "BattleArena" GameObject in your battle scene.
    /// </summary>
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
        private List<UnitController> team1 = new();
        private List<UnitController> team2 = new();
        private bool battleActive = false;
        private float battleTimer = 0f;
        private int nextUnitId = 0;

        // Results
        public float BattleDuration => battleTimer;
        public bool IsBattleActive => battleActive;

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

            // Tick all living units
            foreach (var unit in allUnits)
            {
                if (unit.IsAlive)
                {
                    unit.TickAI();
                }
            }

            // Check win conditions
            CheckBattleEnd();
        }

        // ─── Battle Setup ───

        /// <summary>Start a battle with the configured teams.</summary>
        public void StartBattle()
        {
            ClearBattle();
            nextUnitId = 0;

            SpawnTeam(team1Units, 1, team1, grid.GetTeam1Zone());
            SpawnTeam(team2Units, 2, team2, grid.GetTeam2Zone());

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

                // Place on grid
                grid.PlaceUnit(controller, spawnZone[i]);

                teamList.Add(controller);
                allUnits.Add(controller);

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

        // ─── Battle Controls (UI hooks) ───

        public void SetBattleSpeed(float speed) => battleSpeed = Mathf.Clamp(speed, 0.5f, 4f);
        public void PauseBattle() => battleSpeed = 0f;
        public void ResumeBattle() => battleSpeed = 1f;

        private void ClearBattle()
        {
            foreach (var unit in allUnits)
            {
                if (unit != null)
                    Destroy(unit.gameObject);
            }
            allUnits.Clear();
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
