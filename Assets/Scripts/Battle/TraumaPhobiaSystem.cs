using UnityEngine;
using KindredSiege.Core;

namespace KindredSiege.Battle
{
    /// <summary>
    /// PILLAR 2 — Trauma-Linked Phobias (GDD §5.5)
    ///
    /// When a unit's sanity hits 0 but it is saved by a Mercy Token, the ordeal
    /// leaves a permanent scar: a Phobia. The type is randomly determined at the
    /// moment of rescue and written to UnitData so it persists between battles.
    ///
    /// Phobia triggering is processed in UnitController.TickAI() and the relevant
    /// combat callbacks. This class provides the static utility methods (rolling,
    /// describing) and subscribes to the mercy-saved event to assign phobias.
    ///
    /// Attach to the BattleArena alongside BattleManager.
    /// </summary>
    public class TraumaPhobiaSystem : MonoBehaviour
    {
        public static TraumaPhobiaSystem Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<MercyDecisionResolvedEvent>(OnMercyResolved);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<MercyDecisionResolvedEvent>(OnMercyResolved);
        }

        // ════════════════════════════════════════════
        // EVENT HANDLER
        // ════════════════════════════════════════════

        private void OnMercyResolved(MercyDecisionResolvedEvent evt)
        {
            if (!evt.TokenSpent) return; // Unit died — no phobia needed

            // Find the saved unit and assign a phobia if it doesn't already have one
            var unit = FindUnitById(evt.UnitId);
            if (unit == null || unit.Data == null) return;
            if (unit.Data.ActivePhobia != PhobiaType.None) return; // Already scarred

            PhobiaType phobia = RollPhobia(unit);
            unit.Data.ActivePhobia = phobia;

            EventBus.Publish(new PhobiaGainedEvent
            {
                UnitId     = unit.UnitId,
                UnitName   = unit.UnitName,
                PhobiaName = phobia.ToString()
            });

            Debug.Log($"[Phobia] {unit.UnitName} gained trauma phobia: {phobia} — the cost of surviving the abyss.");
        }

        // ════════════════════════════════════════════
        // STATIC HELPERS
        // ════════════════════════════════════════════

        /// <summary>
        /// Roll a phobia type for the saved unit.
        /// Class-specific weights lean toward narratively fitting phobias,
        /// but any class can gain any phobia on bad luck.
        /// </summary>
        public static PhobiaType RollPhobia(UnitController unit)
        {
            // Unit-type weighted rolls — class personality informs the scar
            string type = unit?.UnitType ?? "";

            float roll = Random.value;

            switch (type)
            {
                case "berserker":
                    // Berserkers rarely fear violence — more likely to fear solitude or failure
                    if (roll < 0.25f) return PhobiaType.SolitudePhobia;
                    if (roll < 0.50f) return PhobiaType.BloodPhobia;
                    if (roll < 0.70f) return PhobiaType.EldritchPhobia;
                    if (roll < 0.85f) return PhobiaType.DarkPhobia;
                    if (roll < 0.95f) return PhobiaType.FailurePhobia;
                    return PhobiaType.Claustrophobia;

                case "occultist":
                case "investigator":
                    // Scholars and ritualists fear the eldritch above all
                    if (roll < 0.35f) return PhobiaType.EldritchPhobia;
                    if (roll < 0.55f) return PhobiaType.DarkPhobia;
                    if (roll < 0.70f) return PhobiaType.BloodPhobia;
                    if (roll < 0.82f) return PhobiaType.SolitudePhobia;
                    if (roll < 0.92f) return PhobiaType.Claustrophobia;
                    return PhobiaType.FailurePhobia;

                case "herald":
                    // Heralds bond with allies — losing them is the worst horror
                    if (roll < 0.35f) return PhobiaType.BloodPhobia;
                    if (roll < 0.55f) return PhobiaType.SolitudePhobia;
                    if (roll < 0.70f) return PhobiaType.EldritchPhobia;
                    if (roll < 0.82f) return PhobiaType.DarkPhobia;
                    if (roll < 0.92f) return PhobiaType.FailurePhobia;
                    return PhobiaType.Claustrophobia;

                case "vessel":
                    // Vessels are already doomed — dark and eldritch phobias dominant
                    if (roll < 0.30f) return PhobiaType.DarkPhobia;
                    if (roll < 0.55f) return PhobiaType.EldritchPhobia;
                    if (roll < 0.70f) return PhobiaType.SolitudePhobia;
                    if (roll < 0.85f) return PhobiaType.BloodPhobia;
                    if (roll < 0.95f) return PhobiaType.Claustrophobia;
                    return PhobiaType.ViolencePhobia;

                default:
                    // Warden, Marksman, Shadow — equal distribution
                    if (roll < 0.15f) return PhobiaType.BloodPhobia;
                    if (roll < 0.30f) return PhobiaType.EldritchPhobia;
                    if (roll < 0.45f) return PhobiaType.SolitudePhobia;
                    if (roll < 0.60f) return PhobiaType.ViolencePhobia;
                    if (roll < 0.75f) return PhobiaType.DarkPhobia;
                    if (roll < 0.90f) return PhobiaType.FailurePhobia;
                    return PhobiaType.Claustrophobia;
            }
        }

        /// <summary>Human-readable description shown in the unit tooltip / devlog.</summary>
        public static string DescribePhobia(PhobiaType phobia)
        {
            return phobia switch
            {
                PhobiaType.BloodPhobia      => "Witnessing any death deals an extra −8 sanity.",
                PhobiaType.EldritchPhobia   => "Eldritch attacks inflict 50% more sanity damage.",
                PhobiaType.SolitudePhobia   => "Suffers −3 sanity every 5 s when no ally is within 4 units.",
                PhobiaType.ViolencePhobia   => "Suffers −2 sanity each time they deal damage.",
                PhobiaType.DarkPhobia       => "Suffers −4 extra sanity every 10 s of prolonged combat.",
                PhobiaType.FailurePhobia    => "Suffers −2 sanity when their attacks are negated.",
                PhobiaType.Claustrophobia   => "Suffers −1 sanity per second while surrounded by enemies.",
                _                           => "No phobia."
            };
        }

        /// <summary>
        /// Force-assign a phobia from a Dread Contest (GDD §6.2 — damage > 35).
        /// Only fires if the unit has no phobia yet. Publishes PhobiaGainedEvent.
        /// </summary>
        public static void ForceRollPhobia(UnitController unit)
        {
            if (unit == null || unit.Data == null) return;
            if (unit.Data.ActivePhobia != PhobiaType.None) return;

            PhobiaType phobia = RollPhobia(unit);
            unit.Data.ActivePhobia = phobia;

            EventBus.Publish(new PhobiaGainedEvent
            {
                UnitId     = unit.UnitId,
                UnitName   = unit.UnitName,
                PhobiaName = phobia.ToString()
            });

            Debug.Log($"[Phobia] {unit.UnitName} gained phobia from Dread Contest: {phobia}");
        }

        // ════════════════════════════════════════════
        // HELPER
        // ════════════════════════════════════════════

        private static UnitController FindUnitById(int id)
        {
            return BattleManager.Instance?.GetUnitById(id);
        }
    }
}
