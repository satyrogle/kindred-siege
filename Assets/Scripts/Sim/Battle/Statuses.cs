using System;
using System.Collections.Generic;

namespace KindredSiege.Sim.Battle
{
    /// The status list (DESIGN_LOCKS, "Builds: one theme per Pal", step 1; Jacob, 30 Oct 2026: "Yes, start step 1"). Every
    /// status on a fighter is one slot of his StatusList: how many stacks it has, how long it still lasts, how big each stack
    /// is, who put it there, and a mark's chain. Its row in StatusRules says its cap, how big a stack can be, how often it
    /// ticks and how a new one refreshes it. Step 1 moved the old statuses onto it with no change to how a fight plays: each
    /// still counts down where it did (CountDownMarks, CountDownCombos, CountDownKit, CountDownSnaps, TickBleeding, TickBurn),
    /// each kind keeps only the numbers its old fields kept, and BattleUnit's old names (SnareTicks, BurnStacks and the rest)
    /// read the list, so the snapshot and the screens read what they always did.
    ///
    /// The kinds, in the order they came into the game. A Pal's theme adds its own at the end with a row in StatusRules; never
    /// reorder them.
    public enum StatusKind
    {
        Panic,
        Bleed,
        Snare,
        Opening,
        Stun,
        Burn,
        Pin,
        Flurry,
        Slow,

        /// The Butcher's theme, one mark a flavour (DESIGN_LOCKS, "The Butcher's theme", builds step 3; BattleSimulation.Marks.cs):
        /// pull lays Crush, portal lays Exposed, time lays Stasis. Each bursts at its cap. WideOpen is what an Exposed burst
        /// leaves: the next blow on it from the other side.
        Crush,
        Exposed,
        Stasis,
        WideOpen,

        /// The floor under the three Pals (campaign rules 23; BattleSimulation.Shade.cs): Sanctuary's shield, its size the
        /// hit points it still soaks, and Backstab, a Fall Back's next hit from behind.
        Shield,
        Backstab,
        /// The Shade's other routes (campaign rules 28; BattleSimulation.Shade.cs): Rupture's Crack thins his armour, and
        /// Breach's Gap stops him covering anyone.
        Crack,
        NoCover
    }

    /// How a new one lands on a status already on: its clock starts again from the new length, or keeps the longer.
    public enum StatusClock
    {
        Reset,
        KeepLonger
    }

    /// One kind's row. A new one adds a stack up to MaxStacks and keeps the bigger size up to MaxSize.
    public sealed class StatusRule
    {
        internal StatusRule(StatusKind kind, int maxStacks, int maxSize, int period, StatusClock clock)
        {
            Kind = kind;
            MaxStacks = maxStacks;
            MaxSize = maxSize;
            Period = period;
            Clock = clock;
        }

        public StatusKind Kind { get; }

        /// Its cap: the most stacks it can hold. 1 for a status that does not stack.
        public int MaxStacks { get; }

        /// The biggest one stack can be (a bleed's percent of health a second). 0 for a status with no size.
        public int MaxSize { get; }

        /// How often it ticks: a bleed and a burn hurt every second. 0 for a status that only holds.
        public int Period { get; }

        public StatusClock Clock { get; }
    }

    /// The status table, one row a kind in StatusKind's order. Step 1 wrote the old statuses down as they already played.
    public static class StatusRules
    {
        private static readonly StatusRule[] Rows =
        {
            new StatusRule(StatusKind.Panic, 1, 0, 0, StatusClock.Reset),
            new StatusRule(StatusKind.Bleed, 1, BleedRules.MaxPercent, SimClock.TicksPerSecond, StatusClock.Reset),
            new StatusRule(StatusKind.Snare, 1, 0, 0, StatusClock.KeepLonger),
            new StatusRule(StatusKind.Opening, 1, 0, 0, StatusClock.Reset),
            new StatusRule(StatusKind.Stun, 1, 0, 0, StatusClock.Reset),
            new StatusRule(StatusKind.Burn, BurnRules.MaxStacks, 0, SimClock.TicksPerSecond, StatusClock.Reset),
            new StatusRule(StatusKind.Pin, 1, 0, 0, StatusClock.KeepLonger),
            new StatusRule(StatusKind.Flurry, 1, 0, 0, StatusClock.Reset),
            new StatusRule(StatusKind.Slow, 1, 0, 0, StatusClock.Reset),
            new StatusRule(StatusKind.Crush, MarkRules.MaxStacks, 0, 0, StatusClock.Reset),
            new StatusRule(StatusKind.Exposed, MarkRules.MaxStacks, 0, 0, StatusClock.Reset),
            new StatusRule(StatusKind.Stasis, MarkRules.MaxStacks, 0, 0, StatusClock.Reset),
            new StatusRule(StatusKind.WideOpen, 1, 0, 0, StatusClock.Reset),
            new StatusRule(StatusKind.Shield, 1, ShieldMaxSize, 0, StatusClock.Reset),
            new StatusRule(StatusKind.Backstab, 1, 0, 0, StatusClock.Reset),
            new StatusRule(StatusKind.Crack, 1, 0, 0, StatusClock.Reset),
            new StatusRule(StatusKind.NoCover, 1, 0, 0, StatusClock.Reset)
        };

        /// The most hit points one shield can soak.
        public const int ShieldMaxSize = 1000000;

        public static int Count => Rows.Length;

        public static StatusRule Of(StatusKind kind) => Rows[(int)kind];
    }

    /// One status on one fighter. It is on while its clock runs. Its numbers stay when it runs out, as the old fields' did,
    /// so who left a mark and its chain read the same afterwards.
    public sealed class Status
    {
        private int _stacks;

        internal Status(StatusKind kind)
        {
            Kind = kind;
        }

        public StatusKind Kind { get; }

        /// Ticks it still lasts. 0 when it is off.
        public int Ticks { get; internal set; }

        /// How many stacks are on. A status that does not stack has one while it is on.
        public int Stacks
        {
            get => StatusRules.Of(Kind).MaxStacks > 1 ? _stacks : (Ticks > 0 ? 1 : 0);
            internal set => _stacks = value;
        }

        /// How big each stack is: a bleed's percent of health a second. 0 for a status with no size.
        public int Size { get; internal set; }

        /// Who put it there: whoever left the mark, lit the burn or froze the flurry. Empty for a kind that keeps no one.
        public string SourceId { get; internal set; } = string.Empty;

        /// A mark's chain (BattleSimulation.Chains.cs). 0 for none, and once it is cashed.
        public int Chain { get; internal set; }

        /// What it keeps for later: the hits a Frozen Flurry holds back for the thaw.
        public int Store { get; internal set; }

        public bool On => Ticks > 0;
    }

    /// Every status on one fighter: one slot a kind, on or off, in StatusKind's order.
    public sealed class StatusList
    {
        private readonly Status[] _slots;

        internal StatusList()
        {
            _slots = new Status[StatusRules.Count];
            for (int i = 0; i < _slots.Length; i++) _slots[i] = new Status((StatusKind)i);
        }

        public Status this[StatusKind kind] => _slots[(int)kind];

        /// Every slot, on or off, in StatusKind's order.
        public IReadOnlyList<Status> All => _slots;

        /// A new one lands. Its clock starts again from ticks, or keeps the longer if its row says so; keepLonger keeps the
        /// longer whatever the row says, for the old rules that did (a Riptide's pull and a Bore's knock stun that way, and the
        /// Butcher's Wound bleeds that way). It adds stacks (one, or a ranked combo's more) up to the cap, keeps the bigger size
        /// up to the row's, and a source given replaces who put it there.
        internal void Apply(StatusKind kind, int ticks, int size = 0, string sourceId = null, bool keepLonger = false, int stacks = 1)
        {
            Status s = _slots[(int)kind];
            StatusRule rule = StatusRules.Of(kind);
            s.Ticks = keepLonger || rule.Clock == StatusClock.KeepLonger ? Math.Max(s.Ticks, ticks) : ticks;
            if (rule.MaxStacks > 1) s.Stacks = Math.Min(rule.MaxStacks, s.Stacks + stacks);
            if (rule.MaxSize > 0) s.Size = Math.Min(rule.MaxSize, Math.Max(s.Size, size));
            if (sourceId != null) s.SourceId = sourceId;
        }

        /// One tick off its clock. True when that ran it out.
        internal bool CountDown(StatusKind kind)
        {
            Status s = _slots[(int)kind];
            if (s.Ticks <= 0) return false;
            s.Ticks--;
            return s.Ticks == 0;
        }

        /// It comes off at once: its clock, stacks and size go to 0. Who put it there and its chain stay.
        internal void Clear(StatusKind kind)
        {
            Status s = _slots[(int)kind];
            s.Ticks = 0;
            s.Stacks = 0;
            s.Size = 0;
        }

        /// A mark was left with it (BattleSimulation.Chains.cs): who left it, and its chain.
        internal void Mark(StatusKind kind, string originId, int chain)
        {
            Status s = _slots[(int)kind];
            s.SourceId = originId;
            s.Chain = chain;
        }
    }
}
