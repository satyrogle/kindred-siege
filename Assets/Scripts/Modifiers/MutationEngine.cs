using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace KindredSiege.Modifiers
{
    public enum MutationFamily
    {
        Tide,   // Spatial/Movement rules
        Mind,   // Sanity/Psychology rules
        Flesh,  // Combat/HP rules
        Void    // Existential/Rivalry rules
    }

    public enum MutationType
    {
        None,

        // --- TIDE MUTATIONS ---
        TheDeepCalls,    // All units pulled toward grid center (1 unit/sec)
        CurrentsShift,   // (Placeholder) Grid positions shuffle every 15s
        DrownedGround,   // (Placeholder) Bottom 2 rows deal sanity damage

        // --- MIND MUTATIONS ---
        FearIsPower,     // Sanity damage converts to physical damage bonus for 5s
        ClarityInPain,   // Units below 25 sanity have 0% hesitation

        // --- FLESH MUTATIONS ---
        PainIsShared,    // 30% of damage dealt splashes to nearest ally
        IronBlood,       // (Placeholder) Armour doubled, healing halved

        // --- VOID MUTATIONS ---
        TheRivalKnows,   // (Placeholder) First Gambit slot fails
        TheWatcherSees   // (Placeholder) Directives cost double
    }

    /// <summary>
    /// PILLAR 1/2/3: The Reality Mutation Engine
    /// Generates and tracks procedural rule changes that apply to specific expeditions.
    /// Mutations synergize with Rival Traits (Resonance).
    /// </summary>
    public class MutationEngine : MonoBehaviour
    {
        public static MutationEngine Instance { get; private set; }

        private readonly List<MutationType> _activeMutations = new();
        public IReadOnlyList<MutationType> ActiveMutations => _activeMutations;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Rolls 1-3 random mutations based on the player's Mythos Exposure level.
        /// (Currently defaults to 1 since Mythos Exposure isn't fully implemented yet).
        /// </summary>
        public List<MutationType> GenerateMutationsForPath()
        {
            var pool = new List<MutationType>
            {
                MutationType.TheDeepCalls,
                MutationType.FearIsPower,
                MutationType.ClarityInPain,
                MutationType.PainIsShared
            };

            // In the future, this scales with Mythos Exposure (Initiate = 1, Scholar = 2, Seer = 3)
            int count = 1; 
            
            return pool.OrderBy(_ => Random.value).Take(count).ToList();
        }

        public void SetActiveMutations(List<MutationType> mutations)
        {
            _activeMutations.Clear();
            if (mutations != null)
                _activeMutations.AddRange(mutations);

            if (_activeMutations.Count > 0)
            {
                Debug.Log($"[MutationEngine] Reality altered! Active Mutations: {string.Join(", ", _activeMutations)}");
            }
        }

        public void ClearMutations()
        {
            _activeMutations.Clear();
        }

        public bool HasMutation(MutationType type) => _activeMutations.Contains(type);

        // ════════════════════════════════════════════
        // FRONTEND HELPERS (For UI)
        // ════════════════════════════════════════════

        public (MutationFamily Family, string Name, string Desc) GetMutationDetails(MutationType type)
        {
            return type switch
            {
                MutationType.TheDeepCalls  => (MutationFamily.Tide, "THE DEEP CALLS", "All units are physically pulled toward the grid centre over time."),
                MutationType.FearIsPower   => (MutationFamily.Mind, "FEAR IS POWER", "Taking Sanity damage grants a massive, temporary physical damage bonus."),
                MutationType.ClarityInPain => (MutationFamily.Mind, "CLARITY IN PAIN", "Units below 25 Sanity have 0% hesitation. The broken become utterly focused."),
                MutationType.PainIsShared  => (MutationFamily.Flesh, "PAIN IS SHARED", "30% of all damage taken splashes to the nearest ally."),
                _ => (MutationFamily.Void, "Unknown Anomaly", "A tear in reality.")
            };
        }
    }
}
