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
        CurrentsShift,   // Grid positions shuffle every 15s
        DrownedGround,   // Bottom 2 rows deal sanity damage
        Riptide,         // Movement speed halved

        // --- MIND MUTATIONS ---
        FearIsPower,     // Sanity damage converts to physical damage bonus for 5s
        ClarityInPain,   // Units below 25 sanity have 0% hesitation
        EchoesOfMadness, // Afflictions applied to one unit jump to an ally
        WhisperingShadows, // Passive sanity drain doubled

        // --- FLESH MUTATIONS ---
        PainIsShared,    // 30% of damage dealt splashes to nearest ally
        IronBlood,       // Armour doubled, healing halved
        BrittleBones,    // All physical damage taken increased by 25%
        FleshWeave,      // Units regenerate 2 HP per second when below 50% HP

        // --- VOID MUTATIONS ---
        TheRivalKnows,   // The first Gambit used fails
        TheWatcherSees,  // Directives cost double
        ExistentialDread,// Unused Directive Points drain max sanity
        TemporalAnomaly  // Battle timer runs 2x faster
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
        public List<MutationType> GenerateMutationsForPath(bool isDomainExpansion = false, MutationFamily? domainFamily = null)
        {
            if (isDomainExpansion && domainFamily.HasValue)
            {
                // Force all 4 mutations of the Overlord's family
                return GetMutationsForFamily(domainFamily.Value);
            }

            var pool = new List<MutationType>();
            foreach (MutationType m in System.Enum.GetValues(typeof(MutationType)))
            {
                if (m != MutationType.None) pool.Add(m);
            }

            int count = 1;
            if (KindredSiege.City.MythosExposure.Instance != null)
            {
                int exposure = KindredSiege.City.MythosExposure.Instance.Exposure;
                if (exposure >= 100) count = 3;       // Seer
                else if (exposure >= 75) count = Random.Range(2, 4); // Adept (2-3)
                else if (exposure >= 50) count = 2;   // Scholar
                else if (exposure >= 25) count = Random.Range(1, 3); // Acolyte (1-2)
            }
            
            return pool.OrderBy(_ => Random.value).Take(count).ToList();
        }

        private List<MutationType> GetMutationsForFamily(MutationFamily family)
        {
            return family switch
            {
                MutationFamily.Tide  => new List<MutationType> { MutationType.TheDeepCalls, MutationType.CurrentsShift, MutationType.DrownedGround, MutationType.Riptide },
                MutationFamily.Mind  => new List<MutationType> { MutationType.FearIsPower, MutationType.ClarityInPain, MutationType.EchoesOfMadness, MutationType.WhisperingShadows },
                MutationFamily.Flesh => new List<MutationType> { MutationType.PainIsShared, MutationType.IronBlood, MutationType.BrittleBones, MutationType.FleshWeave },
                MutationFamily.Void  => new List<MutationType> { MutationType.TheRivalKnows, MutationType.TheWatcherSees, MutationType.ExistentialDread, MutationType.TemporalAnomaly },
                _ => new List<MutationType>()
            };
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
                // TIDE
                MutationType.TheDeepCalls  => (MutationFamily.Tide, "THE DEEP CALLS", "All units are physically pulled toward the grid centre over time."),
                MutationType.CurrentsShift => (MutationFamily.Tide, "CURRENTS SHIFT", "Grid positions shuffle dynamically every 15s."),
                MutationType.DrownedGround => (MutationFamily.Tide, "DROWNED GROUND", "The bottom 2 rows deal massive sanity damage when occupied."),
                MutationType.Riptide       => (MutationFamily.Tide, "RIPTIDE", "All unit movement speed is halved."),
                // MIND
                MutationType.FearIsPower   => (MutationFamily.Mind, "FEAR IS POWER", "Taking Sanity damage grants a massive, temporary physical damage bonus."),
                MutationType.ClarityInPain => (MutationFamily.Mind, "CLARITY IN PAIN", "Units below 25 Sanity have 0% hesitation. The broken become utterly focused."),
                MutationType.EchoesOfMadness=> (MutationFamily.Mind, "ECHOES OF MADNESS", "Afflictions jump to the nearest ally when a unit takes sanity damage."),
                MutationType.WhisperingShadows=>(MutationFamily.Mind, "WHISPERING SHADOWS", "Passive sanity drain over time is doubled."),
                // FLESH
                MutationType.PainIsShared  => (MutationFamily.Flesh, "PAIN IS SHARED", "30% of all damage taken splashes to the nearest ally."),
                MutationType.IronBlood     => (MutationFamily.Flesh, "IRON BLOOD", "All armour values are doubled, but healing is halved."),
                MutationType.BrittleBones  => (MutationFamily.Flesh, "BRITTLE BONES", "All physical damage taken by any unit is increased by 25%."),
                MutationType.FleshWeave    => (MutationFamily.Flesh, "FLESH WEAVE", "Units natively regenerate 2 HP per second when below 50% HP."),
                // VOID
                MutationType.TheRivalKnows => (MutationFamily.Void, "THE RIVAL KNOWS", "The first Gambit slotted in combat will automatically fail."),
                MutationType.TheWatcherSees=> (MutationFamily.Void, "THE WATCHER SEES", "All Tactical Directives cost double the Directive Points."),
                MutationType.ExistentialDread=>(MutationFamily.Void, "EXISTENTIAL DREAD", "Unspent Directive Points rapidly drain max sanity."),
                MutationType.TemporalAnomaly=>(MutationFamily.Void, "TEMPORAL ANOMALY", "The battle timer runs 2x faster, accelerating sanity drains and hazard ticks."),

                _ => (MutationFamily.Void, "Unknown Anomaly", "A tear in reality.")
            };
        }
    }
}
