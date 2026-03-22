using System;
using System.Collections.Generic;
using UnityEngine;

namespace KindredSiege.Core
{
    /// <summary>
    /// Lightweight event bus for decoupled communication between game systems.
    /// Use this instead of direct references between managers.
    /// 
    /// Usage:
    ///   EventBus.Subscribe<BattleEndEvent>(OnBattleEnd);
    ///   EventBus.Publish(new BattleEndEvent { Winner = team, KPEarned = 50 });
    ///   EventBus.Unsubscribe<BattleEndEvent>(OnBattleEnd);
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<Type, List<Delegate>> subscribers = new();

        public static void Subscribe<T>(Action<T> handler) where T : struct
        {
            Type type = typeof(T);
            if (!subscribers.ContainsKey(type))
                subscribers[type] = new List<Delegate>();

            subscribers[type].Add(handler);
        }

        public static void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            Type type = typeof(T);
            if (subscribers.ContainsKey(type))
                subscribers[type].Remove(handler);
        }

        public static void Publish<T>(T eventData) where T : struct
        {
            Type type = typeof(T);
            if (!subscribers.ContainsKey(type)) return;

            // Iterate copy to avoid modification during iteration
            var handlers = new List<Delegate>(subscribers[type]);
            foreach (var handler in handlers)
            {
                try
                {
                    ((Action<T>)handler)?.Invoke(eventData);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[EventBus] Error in handler for {type.Name}: {e.Message}");
                }
            }
        }

        /// <summary>
        /// Clear all subscribers. Call on scene unload or game reset.
        /// </summary>
        public static void Clear()
        {
            subscribers.Clear();
        }
    }

    // ═══════════════════════════════════════════════
    // EVENT DEFINITIONS
    // Add new events here as the project grows.
    // ═══════════════════════════════════════════════

    // --- Battle Events ---
    public struct BattleStartEvent
    {
        public int BattleNumber;
        public int Season;
    }

    public struct BattleEndEvent
    {
        public enum Result { Victory, Defeat, Draw }
        public Result BattleResult;
        public int KPEarned;
        public float Duration;
    }

    public struct UnitDefeatedEvent
    {
        public int UnitId;
        public string UnitName;
        public string UnitType;
        public int TeamId;
        public int DefeatedByUnitId;
    }

    public struct UnitActionEvent
    {
        public int UnitId;
        public string ActionName;
        public Vector3 Position;
        public int TargetId;
    }

    // --- City Events ---
    public struct BuildingPlacedEvent
    {
        public string BuildingType;
        public Vector2Int GridPosition;
    }

    public struct ResourceChangedEvent
    {
        public ResourceType Type;
        public int OldAmount;
        public int NewAmount;
        public int Delta;
    }

    // --- Sanity Events ---

    public struct SanityChangedEvent
    {
        public int UnitId;
        public string UnitName;
        public int OldSanity;
        public int NewSanity;
        public string Reason;
    }

    /// <summary>Unit's sanity hit 0 — permanently consumed by madness.</summary>
    public struct UnitLostEvent
    {
        public int UnitId;
        public string UnitName;
        public string UnitType;
    }

    public struct VirtueGainedEvent
    {
        public int UnitId;
        public string UnitName;
        public string VirtueName;   // VirtueType.ToString()
    }

    public struct AfflictionGainedEvent
    {
        public int UnitId;
        public string UnitName;
        public string AfflictionName;   // AfflictionType.ToString()
    }

    // --- Rivalry Events ---

    public struct RivalEncounteredEvent
    {
        public string RivalId;
        public string RivalName;
        public string Rank;         // RivalRank.ToString()
    }

    public struct RivalDefeatedEvent
    {
        public string RivalId;
        public string RivalName;
    }

    public struct MercyTokenEarnedEvent
    {
        public int Amount;
        public string Source;
    }

    // --- Charity Events ---
    public struct KindnessPointsEarnedEvent
    {
        public int Amount;
        public string Source;
    }

    public struct SeasonDonationEvent
    {
        public int TotalKP;
        public int Season;
    }

    // --- Horror Rating Events (GDD §6.3) ---
    public struct HorrorRatingDrainEvent
    {
        public int    UnitId;
        public int    SanityLost;
        public string RivalName;
    }

    // --- Directive Events (GDD §4.2) ---
    public struct DirectiveUsedEvent
    {
        public string DirectiveName;   // DirectiveType.ToString()
        public int    UnitId;          // Target unit (-1 for global directives like FocusFire)
        public int    PointsRemaining;
    }

    // Raised when a unit hits 0 HP — battle pauses for the Mercy Decision.
    public struct MercyDecisionRequiredEvent
    {
        public int    UnitId;
        public string UnitName;
        public string UnitType;
        public int    ExpeditionCount;
        public int    MercyTokensAvailable;
    }

    // Raised when the player resolves the Mercy Decision (spend token or let die).
    public struct MercyDecisionResolvedEvent
    {
        public int  UnitId;
        public bool TokenSpent; // true = revived, false = permanent death
    }
}
