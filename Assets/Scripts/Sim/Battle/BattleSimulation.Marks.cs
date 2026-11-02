using System;

namespace KindredSiege.Sim.Battle
{
    /// The Butcher's theme (Jacob, 30 Oct 2026: "Yes, build step 3"; DESIGN_LOCKS, "The Butcher's theme (builds step 3)";
    /// FightRules.Marks, campaign rules 18). Each of his three flavours has its own mark on the status list: his pull combos
    /// lay Crush, his portal combos Exposed, his time combos Stasis. A combo lays one stack each time it lands on an enemy, or
    /// its rank's worth (UnitSpec.ComboRank), up to MaxStacks; a new stack starts the clock again, and the mark runs out after
    /// Ticks. At the cap it bursts and clears. Crush: a hit for the power of whoever laid the last stack, and a pin. Exposed:
    /// he is wide open, so your side's next blow on him can't miss, can't be Guarded and crits double. Stasis: he freezes,
    /// and a big move he was winding up starts over. A chief whose band stands shrugs the marks off, as he does a snare.
    /// Every number is a starting value.
    public static class MarkRules
    {
        /// A mark's cap: it bursts when it reaches it.
        public const int MaxStacks = 3;

        /// How long a mark lasts after its last stack.
        public const int Ticks = 6 * SimClock.TicksPerSecond;

        /// A Crush burst pins him this long.
        public const int CrushPinTicks = SimClock.TicksPerSecond;

        /// An Exposed burst leaves him wide open this long, or until your side's next blow on him (Claude's call: as long
        /// as a mark lasts).
        public const int WideOpenTicks = Ticks;

        /// That blow's crit: double, in the Cash layer.
        public const int WideOpenCashPercent = 200;

        /// A Stasis burst freezes him this long.
        public const int StasisFreezeTicks = 3 * SimClock.TicksPerSecond / 2;

        /// The Mark and Burst events' details, one a mark.
        public const string Crush = "crush";
        public const string Exposed = "exposed";
        public const string Stasis = "stasis";

        /// The Hit detail of a Crush burst, and the Cash detail of the blow that spends wide open.
        public const string CrushedDetail = "crushed";
        public const string WideOpenDetail = "wide open";

        /// The mark a combo of this flavour lays: Gravity Well's pull lays Crush, Fold Door's portal Exposed, Time Stop's
        /// time Stasis.
        public static StatusKind Of(ClassBend combo)
        {
            switch (ClassSpells.AugmentOf(combo))
            {
                case Augment.GravityWell: return StatusKind.Crush;
                case Augment.FoldDoor: return StatusKind.Exposed;
                default: return StatusKind.Stasis;
            }
        }

        /// A mark's name, the detail of its Mark and Burst events.
        public static string Name(StatusKind mark)
        {
            switch (mark)
            {
                case StatusKind.Crush: return Crush;
                case StatusKind.Exposed: return Exposed;
                case StatusKind.Stasis: return Stasis;
                default: throw new ArgumentOutOfRangeException(nameof(mark));
            }
        }
    }

    public sealed partial class BattleSimulation
    {
        private readonly bool _marksOn;

        /// Whether this fight plays the Butcher's marks: rules 18, and someone wears a combo to lay them.
        public bool MarksOn => _marksOn;

        // A combo of the wearer's landed on an enemy: its flavour's mark, one stack or its piece's rank's worth. At the cap it
        // bursts. From rules 23 a fighter can carry two combos, so the hook names its own.
        private void Lay(BattleUnit wearer, BattleUnit target, ClassBend combo)
        {
            if (!_marksOn || _finished || wearer == null || target == null || !target.Alive || target.Side == wearer.Side) return;
            if (!ClassSpells.IsCombo(combo) || !HasBend(wearer, combo) || ShrugsMarks(target)) return;
            StatusKind mark = MarkRules.Of(combo);
            // Crowned Marks (rules 30, BattleSimulation.Duos.cs): the star lays more.
            int stacks = _duosOn ? CrownedStacks(wearer, RankOf(wearer, combo)) : RankOf(wearer, combo);
            // The sizing pass (rules 31): more for a side leaning into the flavour.
            if (_sizedOn) stacks = StacksSized(wearer, mark, stacks);
            target.Statuses.Apply(mark, MarkRules.Ticks, sourceId: wearer.Id, stacks: stacks);
            Emit(new BattleEvent(_tick, BattleEventKind.Mark, wearer.Side, wearer.Id, (int)wearer.Class, target.Id, (int)target.Class,
                MarkRules.Ticks, MarkRules.Name(mark)));
            if (target.Statuses[mark].Stacks >= MarkRules.MaxStacks) Burst(wearer, target, mark);
        }

        // A mark reached its cap: it clears and bursts.
        private void Burst(BattleUnit by, BattleUnit target, StatusKind mark)
        {
            target.Statuses.Clear(mark);
            // Gorged Bursts (rules 30, BattleSimulation.Duos.cs): each burst this percent bigger.
            int gorged = _duosOn ? GorgedPercent(by) : 0;
            // The sizing pass (rules 31, BattleSimulation.Sized.cs): every burst bigger, more for its flavour's combos.
            if (_sizedOn) gorged += BurstSized(by, mark);
            switch (mark)
            {
                case StatusKind.Crush:
                    // A hit for the power of whoever laid the last stack, through the damage layers; it is a combo's payoff,
                    // so whatever raises the Combo layer will raise it. Then he is pinned.
                {
                    Emit(EventFor(BattleEventKind.Burst, by, target, by.Power, MarkRules.Crush));
                    Hit crush = null;
                    if (_layersOn)
                    {
                        crush = new Hit(by.Power);
                        if (gorged > 0) crush.Add(HitLayer.Combo, gorged);
                        ApplyHit(by, by.Side, target, crush.Copy(), MarkRules.CrushedDetail);
                    }
                    else ApplyDamage(by, by.Side, target, by.Power * (100 + gorged) / 100, MarkRules.CrushedDetail);
                    // The Butcher's flavours (rules 32, BattleSimulation.Flavours.cs): a longer pin, and at the final form the row.
                    int pin = FlavourHold(by, StatusKind.Crush, MarkRules.CrushPinTicks);
                    if (!_finished && target.Alive) Pin(target, pin);
                    if (!_finished && _flavoursOn) CrushRow(by, target, crush, by.Power * (100 + gorged) / 100, pin);
                    break;
                }
                case StatusKind.Exposed:
                    target.Statuses.Apply(StatusKind.WideOpen, MarkRules.WideOpenTicks, size: gorged, sourceId: by.Id);
                    // The Fold Door's final form (rules 32): it can jump on.
                    if (FinalForm(by.Side, StatusKind.Exposed)) target.Statuses[StatusKind.WideOpen].Store = SizeRules.ExposedJumps;
                    Emit(EventFor(BattleEventKind.Burst, by, target, MarkRules.WideOpenTicks, MarkRules.Exposed));
                    break;
                case StatusKind.Stasis:
                    // Frozen as a time combo freezes (a pin), and his wind up starts over.
                    int freeze = FlavourHold(by, StatusKind.Stasis, MarkRules.StasisFreezeTicks * (100 + gorged) / 100);
                    Emit(EventFor(BattleEventKind.Burst, by, target, freeze, MarkRules.Stasis));
                    Pin(target, freeze);
                    RestartWindUp(target);
                    // Time Stop's final form (rules 32): their whole side.
                    if (!_finished && _flavoursOn) StasisAll(by, target);
                    break;
            }
            // Crowned Marks (rules 30): the star who set it off casts again.
            if (_duosOn) DuoBurst(by);
            // Chain Reaction (rules 34, BattleSimulation.DuoMoves.cs): it splashes on.
            if (_duoMovesOn) Chain(by, target, mark);
        }

        // A Stasis burst: a big move he was winding up starts over, from its whole wind up, as if he had only begun it: his
        // side's big spell if he casts it, its heavy swing if he swings it, his kit's Cleave if he is its chief, or the
        // chief's move he carries. It never shortens a wind up that is longer.
        private void RestartWindUp(BattleUnit unit)
        {
            if (_finished || !unit.Alive) return;
            SideState state = _sides[unit.Side];
            if (state.Windup > 0 && state.CasterId == unit.Id)
                state.Windup = Math.Max(state.Windup, Slowed(unit, _kitsTwoOn ? KitWindupTicks(unit.Side) : Moves.EclipseWindupTicks));
            if (state.HeavyWindup > 0 && state.HeavySwingerId == unit.Id)
                state.HeavyWindup = Math.Max(state.HeavyWindup, Slowed(unit, HeavyRules.WindupTicks));
            if (state.CleaveWindup > 0 && ReferenceEquals(Leader(unit.Side), unit))
                state.CleaveWindup = Math.Max(state.CleaveWindup, ChiefKits.CleaveWindupTicks);
            if (unit.CarryTell > 0)
                unit.CarryTell = Math.Max(unit.CarryTell,
                    Slowed(unit, unit.CarryMove == ChiefMove.Cleave ? CarryRules.CleaveTellTicks : CarryRules.RingTellTicks));
        }

        // Whether this blow is the one an Exposed burst left him wide open to, from the other side. The blow spends it.
        private bool SpendWideOpen(BattleUnit attacker, BattleUnit target)
        {
            if (!_marksOn || attacker.Side == target.Side || !target.Statuses[StatusKind.WideOpen].On) return false;
            Status open = target.Statuses[StatusKind.WideOpen];
            int jumps = open.Store, size = open.Size;
            string source = open.SourceId;
            target.Statuses.Clear(StatusKind.WideOpen);
            // The Fold Door's final form (rules 32): it jumps on to the next one.
            if (_flavoursOn) JumpWideOpen(target, jumps, size, source);
            return true;
        }

        // The marks run out, and wide open with them; a mark's stacks go with its clock.
        private void CountDownTheme()
        {
            for (int i = 0; i < _units.Count; i++)
            {
                StatusList statuses = _units[i].Statuses;
                for (StatusKind k = StatusKind.Crush; k <= StatusKind.WideOpen; k++)
                    if (statuses.CountDown(k)) statuses.Clear(k);
            }
        }
    }
}
