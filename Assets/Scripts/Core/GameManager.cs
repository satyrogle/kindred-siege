using UnityEngine;
using System;

namespace KindredSiege.Core
{
    /// <summary>
    /// Central game manager controlling state transitions between City and Battle phases.
    /// Singleton pattern — persists across scenes.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public enum GameState
        {
            MainMenu,
            CityPhase,
            PreBattle,
            BattlePhase,
            PostBattle,
            SeasonEnd,
            GameOver
        }

        [Header("Current State")]
        [SerializeField] private GameState currentState = GameState.MainMenu;
        public GameState CurrentState => currentState;

        // Events — subscribe to these from other systems
        public event Action<GameState, GameState> OnStateChanged;
        public event Action OnSeasonEnd;

        [Header("Season Config")]
        [SerializeField] private int currentSeason = 1;
        [SerializeField] private int battlesPerSeason = 8;
        private int battlesCompleted = 0;

        public int CurrentSeason => currentSeason;
        public int BattlesCompleted => battlesCompleted;
        public int BattlesRemaining => battlesPerSeason - battlesCompleted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // Stay on MainMenu — MainMenuPanel handles Continue / New Game
            currentState = GameState.MainMenu;
        }

        /// <summary>
        /// Transition to a new game state. Validates the transition is legal.
        /// </summary>
        public void ChangeState(GameState newState)
        {
            if (newState == currentState) return;

            GameState previousState = currentState;

            // Validate transition
            if (!IsValidTransition(previousState, newState))
            {
                Debug.LogWarning($"Invalid state transition: {previousState} -> {newState}");
                return;
            }

            currentState = newState;
            Debug.Log($"[GameManager] State: {previousState} -> {newState}");

            OnStateChanged?.Invoke(previousState, newState);

            // Handle state-specific logic
            HandleStateEntry(newState);
        }

        private bool IsValidTransition(GameState from, GameState to)
        {
            // Define valid transitions
            return (from, to) switch
            {
                (GameState.MainMenu, GameState.CityPhase) => true,
                (GameState.CityPhase, GameState.PreBattle) => true,
                (GameState.PreBattle, GameState.BattlePhase) => true,
                (GameState.PreBattle, GameState.CityPhase) => true,  // Cancel battle
                (GameState.BattlePhase, GameState.PostBattle) => true,
                (GameState.PostBattle, GameState.CityPhase) => true,
                (GameState.PostBattle, GameState.SeasonEnd) => true,
                (GameState.SeasonEnd, GameState.CityPhase) => true,  // New season
                // GameOver can be reached from any active phase
                (GameState.CityPhase, GameState.GameOver)   => true,
                (GameState.PostBattle, GameState.GameOver)   => true,
                (GameState.SeasonEnd, GameState.GameOver)    => true,
                (GameState.GameOver, GameState.MainMenu)     => true,
                (GameState.GameOver, GameState.CityPhase)    => true,  // Try Again
                _ => false
            };
        }

        private void HandleStateEntry(GameState state)
        {
            switch (state)
            {
                case GameState.PostBattle:
                    battlesCompleted++;
                    if (battlesCompleted >= battlesPerSeason)
                    {
                        // Auto-transition to season end after post-battle review
                        // (Give player time to see results first)
                    }
                    break;

                case GameState.SeasonEnd:
                    OnSeasonEnd?.Invoke();
                    break;

                case GameState.CityPhase:
                    if (battlesCompleted >= battlesPerSeason)
                    {
                        // New season
                        currentSeason++;
                        battlesCompleted = 0;
                    }
                    break;
            }
        }

        // --- Convenience methods for other scripts ---

        public void StartBattle() => ChangeState(GameState.PreBattle);
        public void LaunchBattle() => ChangeState(GameState.BattlePhase);
        public void EndBattle() => ChangeState(GameState.PostBattle);
        public void ReturnToCity() => ChangeState(GameState.CityPhase);
        public void TriggerSeasonEnd() => ChangeState(GameState.SeasonEnd);

        public void StartGame()
        {
            currentSeason = 1;
            battlesCompleted = 0;
            ChangeState(GameState.CityPhase);
        }

        /// <summary>
        /// Full campaign reset — wipe all systems and start a fresh run.
        /// Called from MainMenuPanel "New Game" or CityFallenPanel "Try Again".
        /// </summary>
        public void NewGame()
        {
            currentSeason    = 1;
            battlesCompleted = 0;

            // Reset mythos exposure
            City.MythosExposure.Instance?.LoadFromSave(0);

            // Reset districts (Harbor only)
            City.DistrictManager.Instance?.LoadFromSave(new System.Collections.Generic.List<int> { 0 });

            // Reset resources
            ResourceManager.Instance?.ResetResources();

            // Clear roster
            Battle.RosterManager.Instance?.ClearRoster();

            // Delete save file
            SaveManager.Instance?.DeleteSave();

            ChangeState(GameState.CityPhase);
        }

        /// <summary>
        /// Restore serialized campaign progress without changing state.
        /// The caller (MainMenuPanel) is responsible for the state transition.
        /// </summary>
        public void LoadState(int season, int battles)
        {
            currentSeason    = season;
            battlesCompleted = battles;
            Debug.Log($"[GameManager] State loaded: Season {season}, Battles {battles}");
        }
    }
}
