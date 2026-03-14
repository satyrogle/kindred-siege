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
            SeasonEnd
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
            // Start in main menu — transition to city when ready
            ChangeState(GameState.MainMenu);
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
    }
}
