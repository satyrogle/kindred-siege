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
        private KindredSiege.Rivalry.RivalData _activeRival;
        private float _horrorRatingTimer = 0f;
        private const float HorrorRatingInterval = 5f;

        public void SetActiveRival(KindredSiege.Rivalry.RivalData rival) => _activeRival = rival;
        public KindredSiege.Rivalry.RivalData GetActiveRival() => _activeRival;

        // ─── Encounter Type (GDD §Encounter Types) ───
        private EncounterType             _activeEncounterType = EncounterType.Annihilation;
        private KindredSiege.City.DistrictType? _targetDistrict;
        private float _encounterTimer = 0f;           // Survival countdown / Ritual deadline
        private UnitController _rescueTarget;         // Rescue: the Vessel to protect

        private const float SurvivalDuration = 90f;
        private const float RitualDeadline   = 75f;

        public void SetActiveEncounterType(EncounterType type) => _activeEncounterType = type;
        public void SetTargetDistrict(KindredSiege.City.DistrictType? district) => _targetDistrict = district;
        public EncounterType ActiveEncounterType => _activeEncounterType;

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
            // Removed legacy auto-start block.
            // GameManager now correctly dictates when BattlePhase begins.
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
            if (to == GameManager.GameState.PreBattle)
                PrepareRoster();
            else if (to == GameManager.GameState.BattlePhase)
                StartBattle();
        }

        private void Update()
        {
            if (!battleActive) return;

            battleTimer += Time.deltaTime * battleSpeed;

            // Inject FocusFire target before ticking AI (DirectiveSystem override)
            if (DirectiveSystem.Instance != null)
                DirectiveSystem.Instance.InjectFocusFireTarget(team1);

            // --- GRUDGE AUTO-TARGETING (GDD §6.4) ---
            if (_activeRival != null && _activeRival.Memory.HasGrudge && team2.Count > 0)
            {
                var rivalUnit = team2.FirstOrDefault(u => u != null && u.IsAlive && u.UnitName == _activeRival.FullName);
                if (rivalUnit != null)
                {
                    // Find the grudge target
                    var grudgeTarget = team1.FirstOrDefault(u => u != null && u.IsAlive && u.UnitName == _activeRival.Memory.GrudgeTargetUnitName);
                    
                    // Force the Rival AI to tunnel-vision the grudge target
                    if (grudgeTarget != null)
                        rivalUnit.ForcedTarget = grudgeTarget;
                    else
                        rivalUnit.ForcedTarget = null;
                }
            }

            // Tick all living units
            foreach (var unit in allUnits)
            {
                if (unit.IsAlive)
                {
                    unit.TickAI();
                }
            }

            // Encounter-specific timer (Survival countdown, Ritual deadline)
            TickEncounterTimer();

            // Horror Rating drain — every 5 seconds, active rival drains all player unit sanity
            TickHorrorRating();

            // Apply continuous Reality Mutations (GDD §7)
            TickMutations();

            // Check win conditions
            CheckBattleEnd();
        }

        // ─── Mutations Tick ───
        private float _currentsShiftTimer = 0f;

        private void TickMutations()
        {
            var mutationEngine = KindredSiege.Modifiers.MutationEngine.Instance;
            if (mutationEngine == null) return;
            float dt = Time.deltaTime * battleSpeed;

            // VOID: Temporal Anomaly (Timer runs 2x faster, but doesn't speed up animations/movement)
            if (mutationEngine.HasMutation(KindredSiege.Modifiers.MutationType.TemporalAnomaly))
            {
                battleTimer += dt; // Added a second time
            }

            // VOID: Existential Dread (Unspent Directive Points rapidly drain max sanity)
            if (mutationEngine.HasMutation(KindredSiege.Modifiers.MutationType.ExistentialDread))
            {
                int unspentPoints = DirectiveSystem.Instance?.DirectivePoints ?? 0;
                if (unspentPoints > 0)
                {
                    float drainPerPointPerSec = 0.5f;
                    foreach (var u in team1)
                    {
                        if (u != null && u.IsAlive)
                        {
                            u.MaxSanity = Mathf.Max(10, Mathf.RoundToInt(u.MaxSanity - drainPerPointPerSec * unspentPoints * dt));
                            // Clamp current sanity
                            if (u.CurrentSanity > u.MaxSanity) u.CurrentSanity = u.MaxSanity;
                        }
                    }
                }
            }

            // TIDE: Currents Shift (Shuffle grid positions every 15s)
            if (mutationEngine.HasMutation(KindredSiege.Modifiers.MutationType.CurrentsShift))
            {
                _currentsShiftTimer -= dt;
                if (_currentsShiftTimer <= 0f)
                {
                    _currentsShiftTimer = 15f;
                    // Gather all living units and shuffle their positions within their respective zones
                    var t1Zone = grid.GetTeam1Zone();
                    var t1Units = team1.Where(u => u != null && u.IsAlive).ToList();
                    for (int i = 0; i < t1Units.Count && i < t1Zone.Count; i++)
                    {
                        grid.PlaceUnit(t1Units[i], t1Zone[Random.Range(0, t1Zone.Count)]);
                    }

                    var t2Zone = grid.GetTeam2Zone();
                    var t2Units = team2.Where(u => u != null && u.IsAlive).ToList();
                    for (int i = 0; i < t2Units.Count && i < t2Zone.Count; i++)
                    {
                        grid.PlaceUnit(t2Units[i], t2Zone[Random.Range(0, t2Zone.Count)]);
                    }
                    Debug.Log("[Mutation] Currents Shift! All unit positions randomized.");
                }
            }

            // TIDE: Drowned Ground (Bottom 2 rows deal 2 sanity damage per second)
            if (mutationEngine.HasMutation(KindredSiege.Modifiers.MutationType.DrownedGround))
            {
                foreach (var u in allUnits)
                {
                    if (u != null && u.IsAlive)
                    {
                        // Assuming grid is top-down 2D, smaller Y is "bottom" rows
                        var gridPos = grid.WorldToGrid(u.transform.position);
                        if (gridPos.y <= 1)
                        {
                            u._drownedGroundAccumulator += 2f * dt;
                            if (u._drownedGroundAccumulator >= 1f)
                            {
                                int drain = Mathf.FloorToInt(u._drownedGroundAccumulator);
                                u.ModifySanity(-drain, "DrownedGround");
                                u._drownedGroundAccumulator -= drain;
                            }
                        }
                    }
                }
            }

            // FLESH: Flesh Weave (Regenerate 2 HP per second when below 50% HP)
            if (mutationEngine.HasMutation(KindredSiege.Modifiers.MutationType.FleshWeave))
            {
                foreach (var u in allUnits)
                {
                    if (u != null && u.IsAlive && u.CurrentHP < (u.MaxHP * 0.5f))
                    {
                        u._fleshWeaveAccumulator += 2f * dt;
                        if (u._fleshWeaveAccumulator >= 1f)
                        {
                            int heal = Mathf.FloorToInt(u._fleshWeaveAccumulator);
                            u.CurrentHP = Mathf.Min(u.MaxHP, u.CurrentHP + heal);
                            u._fleshWeaveAccumulator -= heal;
                        }
                    }
                }
            }
        }

        // ─── Battle Setup ───

        /// <summary>Sync player roster early so GambitSetupPanel can view it.</summary>
        public void PrepareRoster()
        {
            var rosterMgr = RosterManager.Instance;
            if (rosterMgr != null && rosterMgr.RosterCount > 0)
                team1Units = rosterMgr.GetRosterAsArray();
        }

        /// <summary>Start a battle with the configured teams.</summary>
        public void StartBattle()
        {
            ClearBattle();
            nextUnitId = 0;
            _horrorRatingTimer  = 0f;
            _encounterTimer     = 0f;
            _rescueTarget       = null;

            PrepareRoster();

            SpawnTeam(team1Units, 1, team1, grid.GetTeam1Zone());

            // Procedural enemy roster — RivalryEngine drives composition.
            // Falls back to the Inspector array when RivalryEngine is absent (editor testing).
            var enemies = (RivalryEngine.Instance != null)
                ? GenerateEnemyRoster()
                : team2Units;
            SpawnTeam(enemies, 2, team2, grid.GetTeam2Zone());

            // Scatter hazard tiles across the battlefield (GDD §12)
            GenerateHazards();

            // Apply encounter-specific rules after spawning
            ApplyEncounterSetup();

            // Apply Rival Resonance (GDD §7)
            var activeRival = KindredSiege.Rivalry.RivalEncounterSystem.Instance?.PendingRival;
            var mutations = KindredSiege.Modifiers.MutationEngine.Instance?.ActiveMutations;
            if (activeRival != null && mutations != null && mutations.Count > 0)
            {
                bool hasResonance = false;
                foreach (var mut in mutations)
                {
                    var details = KindredSiege.Modifiers.MutationEngine.Instance.GetMutationDetails(mut);
                    if ((details.Family == KindredSiege.Modifiers.MutationFamily.Mind && activeRival.Traits.Contains(KindredSiege.Rivalry.RivalTraitType.Fearful)) ||
                        (details.Family == KindredSiege.Modifiers.MutationFamily.Flesh && activeRival.Traits.Contains(KindredSiege.Rivalry.RivalTraitType.Rage)) ||
                        (details.Family == KindredSiege.Modifiers.MutationFamily.Tide && activeRival.Traits.Contains(KindredSiege.Rivalry.RivalTraitType.Ambusher)) ||
                        (details.Family == KindredSiege.Modifiers.MutationFamily.Void && activeRival.Traits.Contains(KindredSiege.Rivalry.RivalTraitType.Tactical)))
                    {
                        hasResonance = true;
                        break;
                    }
                }

                if (hasResonance)
                {
                    // Find actual rival unit and buff them (+25% HP, +50% DMG)
                    foreach (var u in team2)
                    {
                        if (u != null && u.Data != null && u.Data.UnitName == activeRival.FullName)
                        {
                            u.ApplyModifiers(1.25f, 1.50f);
                            Debug.Log($"[Resonance] {u.UnitName} resonated with the environment! Massive stat boost.");
                            break;
                        }
                    }
                }
            }

            // Global Status Mutations (Riptide, IronBlood)
            if (mutations != null && mutations.Count > 0)
            {
                if (mutations.Contains(KindredSiege.Modifiers.MutationType.Riptide))
                {
                    foreach (var u in allUnits) { if (u != null) u.ApplyModifiers(1f, 1f, 0.5f); } // Halves MoveSpeed
                }
                if (mutations.Contains(KindredSiege.Modifiers.MutationType.IronBlood))
                {
                    foreach (var u in allUnits) { if (u != null) u.Armour *= 2; }
                }
            }

            // Apply bond buffs to any bonded pairs present on team1
            KindredSiege.Units.BondSystem.ApplyBondEffects(team1);

            // Apply Mythos Exposure battle-start sanity penalty
            int mythospenalty = KindredSiege.City.MythosExposure.Instance?.BattleStartSanityPenalty ?? 0;
            if (mythospenalty < 0)
            {
                foreach (var unit in team1)
                    if (unit != null && unit.IsAlive)
                        unit.ModifySanity(mythospenalty, "MythosExposure");
            }

            // Apply Void Gate Comprehension bonus (negative mod = less horror damage)
            float voidBonus = CityBattleBridge.Instance?.VoidGateComprehensionBonus ?? 0f;
            if (voidBonus > 0f)
            {
                foreach (var unit in team1)
                    if (unit != null)
                        unit.TalentComprehensionMod -= voidBonus;
            }

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

        // ─── Encounter Setup ───

        private void ApplyEncounterSetup()
        {
            switch (_activeEncounterType)
            {
                case EncounterType.RivalHunt:
                    if (_activeRival == null)
                        _activeRival = KindredSiege.Rivalry.RivalryEngine.Instance?.GetActiveRivals()
                            .FirstOrDefault();
                    Debug.Log($"[Encounter] RivalHunt — target: {_activeRival?.FullName ?? "none"}");
                    break;

                case EncounterType.Ambush:
                    // Re-spawn the last 2 team2 units (the flankers) into the player spawn zone
                    var playerZone = grid.GetTeam1Zone();
                    int flankerCount = 0;
                    for (int i = team2.Count - 1; i >= 0 && flankerCount < 2; i--)
                    {
                        var flanker = team2[i];
                        if (flanker == null || flanker.UnitName != "Ambush Flanker") continue;
                        int zoneIdx = Random.Range(0, playerZone.Count);
                        grid.PlaceUnit(flanker, playerZone[zoneIdx]);
                        flanker.SpawnPosition = flanker.transform.position;
                        flankerCount++;
                    }
                    Debug.Log($"[Encounter] Ambush — {flankerCount} flankers repositioned to player zone.");
                    break;

                case EncounterType.Rescue:
                    // Spawn a friendly Vessel unit near the enemy zone that must survive
                    var vesselData = ScriptableObject.CreateInstance<UnitData>();
                    vesselData.UnitName      = "Stranded Vessel";
                    vesselData.UnitType      = "vessel";
                    vesselData.MaxHP         = 60;
                    vesselData.AttackDamage  = 0;
                    vesselData.MoveSpeed     = 1.5f;
                    vesselData.AttackRange   = 0f;
                    vesselData.BaseSanity    = 60;
                    vesselData.Comprehension = 1.0f;
                    vesselData.TeamTint      = new Color(0.6f, 0.9f, 1.0f);

                    var rescueZone = grid.GetTeam2Zone();
                    if (rescueZone.Count > 0)
                    {
                        var spawnPos = rescueZone[rescueZone.Count / 2]; // Middle of enemy zone
                        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                        go.transform.SetParent(unitParent);
                        var rend = go.GetComponent<Renderer>();
                        if (rend != null) rend.material.color = vesselData.TeamTint;

                        var uc = go.AddComponent<UnitController>();
                        uc.Initialise(vesselData, 1, nextUnitId++);
                        grid.PlaceUnit(uc, spawnPos);
                        uc.SpawnPosition = uc.transform.position;

                        var ctx = new BattleContext { Owner = uc, Allies = team1, Enemies = team2, Grid = grid };
                        uc.SetContext(ctx);

                        team1.Add(uc);
                        allUnits.Add(uc);
                        _unitLookup[uc.UnitId] = uc;
                        _rescueTarget = uc;
                        Debug.Log($"[Encounter] Rescue — Vessel spawned in enemy zone.");
                    }
                    break;
            }
        }

        // ─── Encounter Timer Tick ───

        private void TickEncounterTimer()
        {
            if (_activeEncounterType != EncounterType.Survival &&
                _activeEncounterType != EncounterType.Ritual &&
                _activeEncounterType != EncounterType.SanitySiege) return;

            _encounterTimer += Time.deltaTime * battleSpeed;
        }

        // ─── Battle Resolution ───

        private void CheckBattleEnd()
        {
            bool team1Alive = team1.Any(u => u != null && u.IsAlive && u != _rescueTarget);
            bool team2Alive = team2.Any(u => u != null && u.IsAlive);

            BattleEndEvent.Result result;

            switch (_activeEncounterType)
            {
                case EncounterType.Survival:
                case EncounterType.SanitySiege:
                    if (!team1Alive)
                        result = BattleEndEvent.Result.Defeat;
                    else if (_encounterTimer >= SurvivalDuration)
                        result = BattleEndEvent.Result.Victory; // Survived long enough
                    else
                        return;
                    break;

                case EncounterType.Ritual:
                {
                    // Find the ritual keeper (last unit spawned in team2)
                    var keeper = team2.LastOrDefault(u => u != null && u.IsAlive);
                    if (!team1Alive)
                        result = BattleEndEvent.Result.Defeat;
                    else if (!team2Alive)
                        result = BattleEndEvent.Result.Victory;
                    else if (_encounterTimer >= RitualDeadline)
                        result = BattleEndEvent.Result.Defeat; // Ritual completed
                    else
                        return;
                    break;
                }

                case EncounterType.Rescue:
                    if (_rescueTarget == null || !_rescueTarget.IsAlive)
                        result = BattleEndEvent.Result.Defeat; // Vessel died
                    else if (!team2Alive)
                        result = BattleEndEvent.Result.Victory;
                    else if (!team1Alive)
                        result = BattleEndEvent.Result.Defeat;
                    else if (battleTimer >= battleTimeLimit)
                        result = BattleEndEvent.Result.Draw;
                    else
                        return;
                    break;

                case EncounterType.RivalHunt:
                    // Win only when the rival unit is dead; fodder don't count
                    bool rivalAlive = _activeRival != null && team2.Any(
                        u => u != null && u.IsAlive && u.UnitName == _activeRival.FullName);
                    if (!team1Alive)
                        result = BattleEndEvent.Result.Defeat;
                    else if (!rivalAlive && _activeRival != null)
                        result = BattleEndEvent.Result.Victory;
                    else if (!team2Alive)
                        result = BattleEndEvent.Result.Victory;
                    else if (battleTimer >= battleTimeLimit)
                        result = BattleEndEvent.Result.Defeat; // Rival escaped
                    else
                        return;
                    break;

                default: // Annihilation + Ambush
                    if (!team1Alive && !team2Alive)
                        result = BattleEndEvent.Result.Draw;
                    else if (!team2Alive)
                        result = BattleEndEvent.Result.Victory;
                    else if (!team1Alive)
                        result = BattleEndEvent.Result.Defeat;
                    else if (battleTimer >= battleTimeLimit)
                        result = BattleEndEvent.Result.Draw;
                    else
                        return;
                    break;
            }

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

            // --- GRUDGE SYSTEM HOOKS ---
            if (evt.DefeatedByUnitId != -1)
            {
                var killer = GetUnitById(evt.DefeatedByUnitId);
                var deadUnit = GetUnitById(evt.UnitId);

                if (killer != null && deadUnit != null)
                {
                    // 1. If Rival kills Player, Rival forms Grudge against that player unit type
                    if (killer.TeamId == 2 && deadUnit.TeamId == 1 && _activeRival != null && killer.UnitName == _activeRival.FullName)
                    {
                        KindredSiege.Rivalry.RivalryEngine.Instance?.RecordRivalKilledUnit(
                            _activeRival.RivalId, deadUnit.UnitName, deadUnit.UnitType);
                    }

                    // 2. If Player kills Rival, Rival forms Grudge against the killer!
                    if (killer.TeamId == 1 && deadUnit.TeamId == 2 && _activeRival != null && deadUnit.UnitName == _activeRival.FullName)
                    {
                        KindredSiege.Rivalry.RivalryEngine.Instance?.RecordRivalDefeatedByUnit(
                            _activeRival.RivalId, killer.UnitName, killer.UnitType);
                    }
                }
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
            // AND Vendetta formation (if Rival was present)
            if (result == BattleEndEvent.Result.Victory)
            {
                if (_activeRival != null)
                {
                    // Pick a surviving unit to bear the vendetta (fallback to any deployed unit if all died simultaneously)
                    var vendettaTarget = team1.FirstOrDefault(u => u != null && u.IsAlive) ?? team1.FirstOrDefault();
                    if (vendettaTarget != null)
                    {
                        KindredSiege.Rivalry.RivalryEngine.Instance?.RecordRivalDefeatedByUnit(
                            _activeRival.RivalId, vendettaTarget.UnitName, vendettaTarget.UnitType);
                    }
                }

                foreach (var unit in team1)
                {
                    if (unit != null && unit.IsAlive)
                        unit.OnBattleVictory();
                }
            }

            // Increment expedition count on surviving player units (veteran tracking)
            // Record co-survivals for bond formation
            var survivors = new List<UnitController>();
            foreach (var unit in team1)
            {
                if (unit != null && unit.IsAlive && unit.Data != null)
                {
                    unit.Data.ExpeditionCount++;
                    if (unit != _rescueTarget) survivors.Add(unit);
                }
            }
            KindredSiege.Units.BondSystem.RecordCoSurvival(survivors);

            // Calculate KP earned
            int kpEarned = CalculateKP(result);

            Debug.Log($"[Battle] Ended: {result} | Duration: {battleTimer:F1}s | KP: {kpEarned}");

            EventBus.Publish(new BattleEndEvent
            {
                BattleResult = result,
                KPEarned = kpEarned,
                Duration = battleTimer,
                ActiveEncounter = _activeEncounterType,
                TargetDistrict = _targetDistrict
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
                leader.IsTactical   = rival.Traits.Contains(KindredSiege.Rivalry.RivalTraitType.Tactical);
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

            // ── Encounter-specific additions ──

            if (_activeEncounterType == EncounterType.Ambush)
            {
                // Add two flankers — spawned into the player zone by SpawnAmbushFlankers()
                for (int i = 0; i < 2; i++)
                {
                    var flanker = ScriptableObject.CreateInstance<UnitData>();
                    flanker.UnitName     = "Ambush Flanker";
                    flanker.UnitType     = "shadow";
                    flanker.MaxHP        = Mathf.RoundToInt(55 * scale);
                    flanker.AttackDamage = Mathf.RoundToInt(10 * scale);
                    flanker.MoveSpeed    = 3.5f;
                    flanker.AttackRange  = 1.5f;
                    flanker.BaseSanity   = 100;
                    flanker.Comprehension = 0.3f;
                    flanker.TeamTint     = new Color(0.3f, 0.1f, 0.5f);
                    roster.Add(flanker);
                }
                Debug.Log("[Encounter] Ambush — 2 flankers added.");
            }
            else if (_activeEncounterType == EncounterType.Ritual)
            {
                // Ritual Keeper spawns last (back of enemy line) — tagged by name for win check
                var keeper = ScriptableObject.CreateInstance<UnitData>();
                keeper.UnitName     = "Ritual Keeper";
                keeper.UnitType     = "occultist";
                keeper.MaxHP        = Mathf.RoundToInt(120 * scale);
                keeper.AttackDamage = Mathf.RoundToInt(5 * scale);
                keeper.MoveSpeed    = 0.8f;  // Barely moves — stands at the back
                keeper.AttackRange  = 3f;
                keeper.BaseSanity   = 100;
                keeper.Comprehension = 0.5f;
                keeper.TeamTint     = new Color(0.5f, 0.0f, 0.8f);
                roster.Add(keeper);
                Debug.Log($"[Encounter] Ritual — Keeper added. Deadline: {RitualDeadline}s.");
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
