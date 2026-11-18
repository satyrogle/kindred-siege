using System;
using System.Collections.Generic;
using KindredSiege.Sim.Battle;

namespace KindredSiege.Sim.Learning
{
    /// What stands on a node of a tribe's road.
    public enum RoadNodeKind
    {
        Fight = 0,
        Event = 1
    }

    /// A road fight's optional bonus goal (Jacob, 13 Oct 2026; DESIGN_LOCKS, "Sprint 3 calls"). Win and meet it, and
    /// the fight's drop goes up a tier. Miss it and you only lose the bonus.
    public enum FightObjective
    {
        None = 0,
        NobodyFalls = 1,
        Quick = 2,
        BreakWithFour = 3
    }

    public static class FightObjectives
    {
        /// Quick: win inside this many ticks, 40 seconds. A starting value.
        public const long QuickTicks = 40L * SimClock.TicksPerSecond;

        /// BreakWithFour: their nerve breaks with at least this many of theirs still standing.
        public const int BreakStanding = 4;

        /// The goal in words, for its node and its banner.
        public static string Text(FightObjective goal)
        {
            switch (goal)
            {
                case FightObjective.NobodyFalls: return "Nobody of yours falls";
                case FightObjective.Quick: return "Win inside " + (QuickTicks / SimClock.TicksPerSecond) + " seconds";
                case FightObjective.BreakWithFour: return "Break them with " + BreakStanding + " still standing";
                default: return string.Empty;
            }
        }

        /// Whether a won fight met its goal. fallen: how many of your fighters went down in it.
        public static bool Met(FightObjective goal, BattleSimulation sim, int fallen)
        {
            switch (goal)
            {
                case FightObjective.NobodyFalls: return fallen == 0;
                case FightObjective.Quick: return sim.CurrentTick <= QuickTicks;
                case FightObjective.BreakWithFour:
                    FightEnding end = sim.Ending;
                    return end.How == FightEnd.NerveBreak && end.Side == 1 && end.Standing >= BreakStanding;
                default: return false;
            }
        }

        // The goals a kind of fight can carry: Hold the line is won by lasting, so it cannot be quick, and a duel or a
        // trial brings too few to break with four standing.
        internal static FightObjective[] For(EncounterKind kind)
        {
            switch (kind)
            {
                case EncounterKind.HoldTheLine: return new[] { FightObjective.NobodyFalls, FightObjective.BreakWithFour };
                case EncounterKind.Duel:
                case EncounterKind.Trial: return new[] { FightObjective.NobodyFalls, FightObjective.Quick };
                default: return new[] { FightObjective.NobodyFalls, FightObjective.Quick, FightObjective.BreakWithFour };
            }
        }
    }

    /// What an event choice does. Every effect uses what a run already has: a fighter, gear, materials.
    public enum EventEffect
    {
        Nothing = 0,
        TakeInStraggler = 1,
        BuyPauper = 2,
        BuyMid = 3,
        Pray = 4
    }

    /// One choice in an event: its words, its price in materials and what it does.
    public sealed class RoadChoice
    {
        internal RoadChoice(string label, int cost, EventEffect effect)
        {
            Label = label;
            Cost = cost;
            Effect = effect;
        }

        public string Label { get; }

        /// Materials it costs. You cannot make the choice without them.
        public int Cost { get; }
        public EventEffect Effect { get; }
    }

    /// A short scene on a road (Jacob, 13 Oct 2026; DESIGN_LOCKS, "Sprint 3 calls").
    public sealed class RoadEvent
    {
        internal RoadEvent(string id, string title, string text, params RoadChoice[] choices)
        {
            Id = id;
            Title = title;
            Text = text;
            Choices = choices;
        }

        public string Id { get; }
        public string Title { get; }
        public string Text { get; }
        public IReadOnlyList<RoadChoice> Choices { get; }
    }

    /// The road's events. The list and every word of it are Claude's placeholders: Jacob writes the story.
    public static class RoadEvents
    {
        public const int StragglerCost = 10;
        public const int PauperCost = 10;
        public const int MidCost = 25;
        public const int PrayLoss = 10;

        public static readonly RoadEvent Straggler = new RoadEvent("straggler", "The straggler",
            "A Clarth hunter who lost her war band asks to walk with you. She has nothing left to eat.",
            new RoadChoice("Take her in", StragglerCost, EventEffect.TakeInStraggler),
            new RoadChoice("Send her home", 0, EventEffect.Nothing));

        public static readonly RoadEvent BoneTrader = new RoadEvent("trader", "The bone trader",
            "A trader who sells to every side has laid his wares out on a dead man's cloak.",
            new RoadChoice("Buy a Pauper piece", PauperCost, EventEffect.BuyPauper),
            new RoadChoice("Buy a Mid piece", MidCost, EventEffect.BuyMid),
            new RoadChoice("Walk on", 0, EventEffect.Nothing));

        public static readonly RoadEvent OldShrine = new RoadEvent("shrine", "The old shrine",
            "A shrine to something nobody names any more. The offering bowl is still warm.",
            new RoadChoice("Pray", 0, EventEffect.Pray),
            new RoadChoice("Walk on", 0, EventEffect.Nothing));

        public static readonly RoadEvent[] All = { Straggler, BoneTrader, OldShrine };
    }

    /// What an event choice did.
    public sealed class EventOutcome
    {
        internal EventOutcome(RoadNode node, RoadChoice choice)
        {
            Node = node;
            Choice = choice;
        }

        public RoadNode Node { get; }
        public RoadChoice Choice { get; }

        /// Materials it took, as a negative number, or 0.
        public int Materials { get; internal set; }

        /// The piece it gave, or null.
        public CampaignGear Gear { get; internal set; }

        /// The fighter it gave, or null.
        public CampaignFighter Fighter { get; internal set; }

        /// What happened, in one line.
        public string Text { get; internal set; } = string.Empty;
    }

    /// One node on a tribe's road: a fight or an event, and the lanes of the next row it leads to.
    public sealed class RoadNode
    {
        private readonly List<int> _next = new List<int>();

        internal RoadNode(int row, int lane, RoadNodeKind kind, EncounterKind fight, FightObjective objective, RoadEvent roadEvent,
            ulong seed)
        {
            Row = row;
            Lane = lane;
            Kind = kind;
            Fight = fight;
            Objective = objective;
            Event = roadEvent;
            Seed = seed;
        }

        public int Row { get; }
        public int Lane { get; }
        public RoadNodeKind Kind { get; }

        /// For a fight: what kind of fight.
        public EncounterKind Fight { get; }

        /// For a fight: its bonus goal, or None.
        public FightObjective Objective { get; }

        /// For an event: the scene, or null.
        public RoadEvent Event { get; }

        /// For a fight: the Pal whose favour it drops instead of gear (Jacob, 18 Oct 2026), or None for a gear fight.
        public Pal Pal { get; internal set; }

        /// From rules 39: the beast the run rolled onto this fight node (Campaign.RunBeast), or None. Its fight is that beast at
        /// its tier, it drops trophies and no Pal, and you can go round it.
        public BeastKit Beast { get; internal set; }

        /// Lanes in the next row this node leads to. Empty in the last row, which leads to the chief.
        public IReadOnlyList<int> Next => _next;

        // The node's own seed: what an event on it gives is fixed by it.
        internal ulong Seed { get; }

        internal void Link(int lane)
        {
            if (_next.Contains(lane)) return;
            _next.Add(lane);
            _next.Sort();
        }
    }

    /// A tribe's road to its chief, seen whole before you start (Jacob, 13 Oct 2026; DESIGN_LOCKS, "Sprint 3 calls"):
    /// Lanes lanes and Rows rows of nodes. The first row is all fights; every later row has one event while the road
    /// has events left. The fights in a row are all different kinds, and one of them carries a bonus goal. Each node
    /// leads to its own lane in the next row and a lane beside it (Jacob's "fix all 9", 13 Oct 2026: in his playtest a
    /// pick could leave one way on), so every step is a fork, the lanes cross, and with at most one event a row every
    /// node leads to at least one fight. The same campaign, run and tribe always make the same road.
    public sealed class CampaignRoad
    {
        public const int Lanes = 3;

        private static readonly EncounterKind[] Fights =
        {
            EncounterKind.WarParty, EncounterKind.HoldTheLine, EncounterKind.Duel, EncounterKind.Trial
        };

        private readonly RoadNode[] _nodes;

        private CampaignRoad(string tribeId, int rows)
        {
            TribeId = tribeId;
            Rows = rows;
            _nodes = new RoadNode[rows * Lanes];
        }

        public string TribeId { get; }

        /// Rows of nodes before the chief.
        public int Rows { get; }

        /// Every node, row by row, lane by lane.
        public IReadOnlyList<RoadNode> All => _nodes;

        /// The node at this row and lane, or null off the road.
        public RoadNode Node(int row, int lane)
        {
            return row < 0 || row >= Rows || lane < 0 || lane >= Lanes ? null : _nodes[row * Lanes + lane];
        }

        internal static CampaignRoad Make(ulong campaignSeed, int run, string tribeId, int rows, bool combatLessons = false,
            int heldPals = 0, bool palEveryFight = false)
        {
            rows = Math.Max(0, rows);
            ulong seed = Mix(campaignSeed, run, tribeId);
            var rng = new DeterministicRng(seed, 7UL);
            var road = new CampaignRoad(tribeId, rows);
            var events = new List<RoadEvent>(RoadEvents.All);
            // One pile of the four fight kinds for the whole road, refilled when it runs out, so no kind comes up a
            // third time before every kind has come up twice (Jacob, 18 Oct 2026: one road had three Eclipse Trials).
            var pile = new List<EncounterKind>();
            for (int r = 0; r < rows; r++)
            {
                int eventLane = combatLessons || r == 0 || events.Count == 0 ? -1 : rng.Next(Lanes);
                int goalLane = rng.Next(Lanes);
                if (goalLane == eventLane) goalLane = (goalLane + 1) % Lanes;
                var usedInRow = new List<EncounterKind>();
                for (int l = 0; l < Lanes; l++)
                {
                    ulong nodeSeed = seed ^ ((ulong)(r * Lanes + l + 1) * 0x9E3779B97F4A7C15UL);
                    if (l == eventLane)
                    {
                        int pick = rng.Next(events.Count);
                        road._nodes[r * Lanes + l] = new RoadNode(r, l, RoadNodeKind.Event, EncounterKind.WarParty, FightObjective.None,
                            events[pick], nodeSeed);
                        events.RemoveAt(pick);
                        continue;
                    }
                    EncounterKind kind = combatLessons ? EncounterKind.WarParty : DrawKind(pile, usedInRow, rng);
                    usedInRow.Add(kind);
                    FightObjective goal = FightObjective.None;
                    if (!combatLessons && l == goalLane)
                    {
                        FightObjective[] goals = FightObjectives.For(kind);
                        goal = goals[rng.Next(goals.Length)];
                    }
                    road._nodes[r * Lanes + l] = new RoadNode(r, l, RoadNodeKind.Fight, kind, goal, null, nodeSeed);
                }
            }
            for (int r = 0; r + 1 < rows; r++)
            {
                for (int l = 0; l < Lanes; l++)
                {
                    RoadNode node = road.Node(r, l);
                    node.Link(l);
                    // Its own lane and one beside it: the only one for an edge lane, either side for a middle lane.
                    node.Link(l == 0 ? 1 : l == Lanes - 1 ? Lanes - 2 : rng.Next(2) == 0 ? l - 1 : l + 1);
                }
            }
            // One fight a row drops a Pal's favour instead of gear (Jacob, 18 Oct 2026: "favours from the Pals on road
            // nodes"): never the fight with the bonus goal and never an event, so each row offers a different drop. These
            // are their own dice, so every road keeps the layout it had before favours.
            var favourDice = new DeterministicRng(seed, 8UL);
            var plain = new List<int>();
            for (int r = 0; r < rows; r++)
            {
                // Fix 5 (campaign rules 19): every fight in the row carries a Pal, no two the same, each drawn by weight
                // (FavourRules.PalWeight) from the Pals the row has not used yet.
                if (palEveryFight)
                {
                    int used = 0;
                    for (int l = 0; l < Lanes; l++)
                    {
                        RoadNode node = road.Node(r, l);
                        if (node.Kind != RoadNodeKind.Fight) continue;
                        node.Pal = FavourRules.WeightedPal(favourDice, heldPals, used);
                        used |= 1 << (int)node.Pal;
                    }
                    continue;
                }
                plain.Clear();
                for (int l = 0; l < Lanes; l++)
                {
                    RoadNode node = road.Node(r, l);
                    if (node.Kind == RoadNodeKind.Fight && node.Objective == FightObjective.None) plain.Add(l);
                }
                if (plain.Count == 0) continue;
                RoadNode pick = road.Node(r, plain[favourDice.Next(plain.Count)]);
                // From campaign rules 18 the Pals the run held picks from come up more (FavourRules.PalWeight); with none
                // held, each is as likely as ever, drawn the same way.
                pick.Pal = heldPals == 0 ? FavourRules.Pals[favourDice.Next(FavourRules.Pals.Length)] : FavourRules.WeightedPal(favourDice, heldPals);
            }
            return road;
        }

        // A kind from the road's pile that this row has not used yet; the pile is refilled with all four when it has
        // none left that fit.
        private static EncounterKind DrawKind(List<EncounterKind> pile, List<EncounterKind> usedInRow, DeterministicRng rng)
        {
            var fits = new List<int>();
            for (int i = 0; i < pile.Count; i++) if (!usedInRow.Contains(pile[i])) fits.Add(i);
            if (fits.Count == 0)
            {
                pile.AddRange(Fights);
                for (int i = 0; i < pile.Count; i++) if (!usedInRow.Contains(pile[i])) fits.Add(i);
            }
            int at = fits[rng.Next(fits.Count)];
            EncounterKind kind = pile[at];
            pile.RemoveAt(at);
            return kind;
        }

        // FNV-1a over the campaign's seed, the run and the tribe: the same on every machine.
        internal static ulong Mix(ulong seed, int run, string tribeId)
        {
            ulong h = 14695981039346656037UL;
            for (int i = 0; i < 8; i++)
            {
                h ^= (seed >> (8 * i)) & 0xFFUL;
                h *= 1099511628211UL;
            }
            for (int i = 0; i < 4; i++)
            {
                h ^= ((ulong)(uint)run >> (8 * i)) & 0xFFUL;
                h *= 1099511628211UL;
            }
            foreach (char ch in tribeId ?? string.Empty)
            {
                h ^= ch;
                h *= 1099511628211UL;
            }
            return h;
        }
    }

    /// Picked at camp before a run (Jacob, 13 Oct 2026; DESIGN_LOCKS, "Sprint 3 calls"): each makes the run harder in
    /// one plain way, and each one picked raises the run's drops. The four and their numbers are Claude's placeholders.
    public enum Omen
    {
        BloodMoon = 0,
        HungryHunters = 1,
        LongRoad = 2,
        ThinRanks = 3
    }

    public static class OmenRules
    {
        public const int BloodMoonPercent = 20;
        public const int HungryHuntersExtra = 2;

        /// Each omen picked adds this percent to the materials the run earns.
        public const int MaterialsPercentEach = 25;

        /// With this many omens picked, every gear drop on the road goes up a tier.
        public const int TierUpAt = 2;

        public static readonly Omen[] All = { Omen.BloodMoon, Omen.HungryHunters, Omen.LongRoad, Omen.ThinRanks };

        public static bool IsOmen(int value) => value >= (int)Omen.BloodMoon && value <= (int)Omen.ThinRanks;

        public static string Name(Omen omen)
        {
            switch (omen)
            {
                case Omen.BloodMoon: return "Blood Moon";
                case Omen.HungryHunters: return "Hungry Hunters";
                case Omen.LongRoad: return "Long Road";
                case Omen.ThinRanks: return "Thin Ranks";
                default: return string.Empty;
            }
        }

        /// One line for the omen pick.
        public static string Describe(Omen omen)
        {
            switch (omen)
            {
                case Omen.BloodMoon: return "Their fighters hit " + BloodMoonPercent + "% harder.";
                case Omen.HungryHunters: return "The hunters bring " + HungryHuntersExtra + " more.";
                case Omen.LongRoad: return "One more row on every road.";
                case Omen.ThinRanks: return "The run starts with one fighter fewer.";
                default: return string.Empty;
            }
        }
    }
}
