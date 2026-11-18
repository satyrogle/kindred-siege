using System;

namespace KindredSiege.Sim.Battle
{
    /// The Glutton's whole tree retargeted, and the hooks of the cut to 12 that have a live target (campaign rules 46;
    /// FightRules.TreeRetarget; Jacob, 16 Nov 2026: "Yes, lock it" to all 35 talents, then "Go, both"; DESIGN_LOCKS "The
    /// Glutton's whole tree retargeted", "Four hooks, six talents, stagger and the slice's classes" and "Which classes each
    /// Glutton limb covers"). Each talent makes one piece another Pal, a class or the parry already has bigger, longer, faster
    /// or cheaper. Where that piece is in this sim the talent boosts it here; where it is not yet (Shield Pin, the Hex and the
    /// Warden's bash on it, Bad Blood, and the 12 new classes' moves and combos) the talent does nothing until it is built.
    /// Every talent's 1 Nov meaning stops. A fed talent's bonus doubles, as before (GluttonRules.Bonus).
    /// - Front line: Fold Shield, Still Wall freezes RetargetRules.WallMoreTicks longer; Branch Anchor, Sanctuary's shields
    ///   thicker on anyone, not only the front row.
    /// - Strikers: Nucleus Edge, Twin Cut's cuts hit harder; Strip, her strike rips a bleed open harder; Fold Step, Collapse's
    ///   own pin lasts longer (Shield Pin keeps its own).
    /// - Shooters: Slow Light Rounds, Stopped Bullets' Bleed cash lands harder still; Dark Matter Rounds, Steady's shot goes
    ///   through at full power.
    /// - Control: Time Lock, a Snare takes one more cash before it is spent; Stasis Wind Up, on a snared man the Butcher's
    ///   flavour stacks burst one stack sooner.
    /// - Command: Precognition, a man you parry staggers longer; Glancing Blow, Mirror sends back more; Deep Node, Rupture's
    ///   Crack thins armour more; Command Relay, your chief spell hits harder.
    /// - War chest: Synthesis Discount, the forge costs less (Learning.Campaign).
    /// The hooks: Doorway's catch is left with an Opening, and Stopped Bullets' volley on a bleeding man lands double and spends
    /// the Bleed. A stagger is also a timer (BattleUnit.StaggeredUntil) for the Berserker's Bloodrush to read once he is built.
    /// Every number is a starting value.
    public sealed partial class BattleSimulation
    {
        private readonly bool _treeOn;

        /// Whether this fight plays the retargeted tree and the live hooks: campaign rules 46.
        public bool TreeOn => _treeOn;

        /// Whether this unit is staggered now (rules 46).
        public bool IsStaggered(BattleUnit u) => _treeOn && u != null && u.StaggeredUntil > _tick;

        // A 1 Nov talent's effect plays only before rules 46.
        private bool OldTree => _gluttonOn && !_treeOn;

        // A retargeted talent's bonus on this side, from rules 46; 0 before.
        private int TreeBonus(int side, GluttonTalent talent, int bonus)
        {
            return _treeOn && _gluttonOn && side >= 0 && side <= 1 ? Bonus(side, talent, bonus) : 0;
        }

        private int TreeBonus(BattleUnit unit, GluttonTalent talent, int bonus) => unit == null ? 0 : TreeBonus(unit.Side, talent, bonus);

        // ------------------------------------------------------------ the front line

        // Fold Shield: how long Still Wall freezes.
        private int WallTicks(BattleUnit warden) => ClassSpells.StillWallTicks + TreeBonus(warden, GluttonTalent.FoldShield, RetargetRules.WallMoreTicks);

        // ------------------------------------------------------------ the strikers

        // Nucleus Edge: one of Twin Cut's cuts.
        private int TwinCutDamage(BattleUnit skirmisher)
        {
            int cut = Math.Max(1, skirmisher.Power * BoostRules.TwinCutPercent / 100);
            return cut * (100 + TreeBonus(skirmisher, GluttonTalent.NucleusEdge, RetargetRules.TwinCutMorePercent)) / 100;
        }

        // Strip: how much harder her strike rips a bleed open, in percent.
        private int RipMorePercent(BattleUnit attacker) => TreeBonus(attacker, GluttonTalent.Strip, RetargetRules.RipMorePercent);

        // Fold Step: how long Collapse's black hole pins.
        private int CollapsePinTicks(BattleUnit skirmisher) => ClassSpells.CollapseTicks + TreeBonus(skirmisher, GluttonTalent.FoldStep, RetargetRules.CollapseMoreTicks);

        // ------------------------------------------------------------ the shooters

        // The hook: Stopped Bullets' volley on a bleeding man lands double and spends the Bleed; Slow Light Rounds makes it
        // land harder still. The percent it adds in the Cash layer, or 0 when it does not cash.
        private int StoppedBleedCash(BattleUnit marksman, BattleUnit mark)
        {
            if (!_treeOn || marksman == null || mark == null || !mark.Alive || mark.BleedTicks <= 0) return 0;
            if (!Wears(marksman, ClassBend.StoppedBullets) || _shotsAtOnce < ClassSpells.StoppedShots) return 0;
            return RetargetRules.StoppedBleedCashPercent + TreeBonus(marksman, GluttonTalent.SlowLightRounds, RetargetRules.SlowLightMorePercent);
        }

        // Dark Matter Rounds: Steady's piercing share of his power, in percent.
        private int PiercePercentOf(BattleUnit marksman) => BoostRules.PiercePercent + TreeBonus(marksman, GluttonTalent.DarkMatterRounds, RetargetRules.SteadyMorePercent);

        // ------------------------------------------------------------ control

        // Time Lock: how many cashes this side's Snare takes before it is spent.
        private int SnareCashesFor(int side) => 1 + TreeBonus(side, GluttonTalent.TimeLock, RetargetRules.SnareMoreCashes);

        // Stasis Wind Up: how many of the Butcher's flavour stacks burst on this man, laid by this side.
        private int BurstAt(BattleUnit by, BattleUnit target)
        {
            if (by == null || target == null || target.SnareTicks <= 0) return MarkRules.MaxStacks;
            return Math.Max(1, MarkRules.MaxStacks - TreeBonus(by, GluttonTalent.StasisWindUp, RetargetRules.BurstFewer));
        }

        // ------------------------------------------------------------ command

        // Precognition: how long a man this side parries staggers.
        private int ParryStagger(int parryingSide) => FightRules.StaggerTicks + TreeBonus(parryingSide, GluttonTalent.Precognition, RetargetRules.StaggerMoreTicks);

        // Glancing Blow: Mirror's return, bigger.
        private int MirrorDamage(int side, int damage) => damage * (100 + TreeBonus(side, GluttonTalent.GlancingBlow, RetargetRules.MirrorMorePercent)) / 100;

        // Deep Node: the percent of armour a Crack laid by this side thins, at most all of it.
        private int CrackPercentFrom(int side) => Math.Min(100, ShadeRules.CrackPercent * (100 + TreeBonus(side, GluttonTalent.DeepNode, RetargetRules.CrackMorePercent)) / 100);

        // Command Relay: a chief spell from this side lands harder (the details the screen counts as chief damage).
        private void ChiefSpellRelay(int sourceSide, Hit hit, string detail)
        {
            if (!_treeOn || !IsChiefSpellDetail(detail)) return;
            int more = TreeBonus(sourceSide, GluttonTalent.CommandRelay, RetargetRules.ChiefSpellMorePercent);
            if (more > 0) hit.Add(HitLayer.Kit, more);
        }

        private static bool IsChiefSpellDetail(string detail) =>
            detail == Moves.EclipseDetail || detail == Moves.PyreDetail || detail == ChiefSpells.BiteDetail ||
            detail == ChiefSpells.BloodDetail || detail == ChiefSpells.TideDetail;

        // ------------------------------------------------------------ the stagger timer and the snapshot

        // A stagger of this many ticks on this unit: from rules 46 also a timer.
        private void StaggerTimer(BattleUnit u, int ticks)
        {
            if (!_treeOn || u == null || ticks <= 0) return;
            u.StaggeredUntil = Math.Max(u.StaggeredUntil, _tick + ticks);
        }

        private void AppendTreeUnit(SnapshotBuilder b, BattleUnit u)
        {
            b.Field(u.Id + ".staggeredUntil", u.StaggeredUntil);
            b.Field(u.Id + ".snareCashes", u.SnareCashes);
        }
    }

    /// Rules 46's numbers (BattleSimulation.Tree.cs). Each is a starting value, the bonus a held talent gives; a fed one
    /// doubles it.
    public static class RetargetRules
    {
        /// Fold Shield: Still Wall freezes 3 s, not 2.
        public const int WallMoreTicks = SimClock.TicksPerSecond;
        /// Nucleus Edge: Twin Cut's two cuts hit twice as hard.
        public const int TwinCutMorePercent = 100;
        /// Strip: her strike rips a bleed open half again as hard.
        public const int RipMorePercent = 50;
        /// Fold Step: Collapse's own pin lasts 3 s, not 2.
        public const int CollapseMoreTicks = SimClock.TicksPerSecond;
        /// The hook: Stopped Bullets' volley on a bleeding man lands double (the Cash layer's 100 more).
        public const int StoppedBleedCashPercent = 100;
        /// Slow Light Rounds: that volley lands triple, not double.
        public const int SlowLightMorePercent = 100;
        /// Dark Matter Rounds: Steady's shot goes through at full power, not half.
        public const int SteadyMorePercent = 50;
        /// Time Lock: a Snare takes two cashes before it is spent.
        public const int SnareMoreCashes = 1;
        /// Stasis Wind Up: on a snared man the flavour stacks burst at 2, not 3.
        public const int BurstFewer = 1;
        /// Precognition: a man you parry staggers 3 s, not 2.
        public const int StaggerMoreTicks = SimClock.TicksPerSecond;
        /// Glancing Blow: Mirror sends back twice as much.
        public const int MirrorMorePercent = 100;
        /// Deep Node: Rupture's Crack thins armour twice as much.
        public const int CrackMorePercent = 100;
        /// Command Relay: your chief spell hits twice as hard.
        public const int ChiefSpellMorePercent = 100;
        /// Synthesis Discount: the forge costs a third less.
        public const int ForgeOffPercent = 33;
    }
}
