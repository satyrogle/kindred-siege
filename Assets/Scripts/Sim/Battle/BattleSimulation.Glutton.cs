using System;
using System.Collections.Generic;

namespace KindredSiege.Sim.Battle
{
    /// The Glutton's tree in the fight (campaign rules 29; FightRules.Glutton; Jacob in chat, 1 Nov 2026: "go" on Claude's calls;
    /// DESIGN_LOCKS, "The Butcher, Glutton and Shade locked" and "The Glutton's theme: one limb"). Each side brings its talents in
    /// its PalKit, held or fed. Every talent makes something the side already has bigger, longer, faster or cheaper, and a fed
    /// one's bonus doubles (GluttonRules.Bonus). The limbs are roles: the front line is the side's front row, the strikers its
    /// Skirmishers, the shooters its Marksmen, the control its Trappers, the healers its Medics; command is the side itself.
    /// Every number is a starting value.
    public sealed partial class BattleSimulation
    {
        private readonly bool _gluttonOn;

        /// Whether this fight plays the Glutton's talents: campaign rules 29.
        public bool GluttonOn => _gluttonOn;

        /// This side's level of this talent in this fight: 0, GluttonRules.Held or GluttonRules.Fed.
        public int TalentLevelOf(int side, GluttonTalent talent)
        {
            return _gluttonOn && side >= 0 && side <= 1 ? KitOf(side).TalentLevel(talent) : 0;
        }

        // A talent's bonus on this side.
        private int Bonus(int side, GluttonTalent talent, int bonus)
        {
            // Sized from rules 31 (BattleSimulation.Sized.cs).
            return GluttonRules.Bonus(TalentLevelOf(side, talent), _sizedOn ? GluttonSized(side, talent, bonus) : bonus);
        }

        private bool HasTalent(int side, GluttonTalent talent) => TalentLevelOf(side, talent) > 0;

        // ------------------------------------------------------------ blows and hits

        // A blow's talents, in the damage layers: Nucleus Edge and Answer the Parry for a Skirmisher, Probability Lock and
        // Nothing Wasted for a Marksman.
        private void GluttonBlow(BattleUnit attacker, BattleUnit target, Hit hit)
        {
            int side = attacker.Side;
            if (side < 0 || side > 1 || target.Side == side) return;
            if (attacker.Class == UnitClass.Skirmisher)
            {
                int edge = Bonus(attacker, GluttonTalent.NucleusEdge, GluttonRules.EdgePercent);
                if (edge > 0) hit.Add(HitLayer.Increased, edge);
                if (target.ParriedBySide == side && target.ParriedAtTick >= 0 && _tick - target.ParriedAtTick <= GluttonRules.AnswerTicks)
                {
                    int answer = Bonus(attacker, GluttonTalent.AnswerTheParry, GluttonRules.AnswerPercent);
                    if (answer > 0) hit.Add(HitLayer.Status, answer);
                }
            }
            else if (attacker.Class == UnitClass.Marksman)
            {
                if (target.SnareTicks > 0)
                {
                    int locked = Bonus(attacker, GluttonTalent.ProbabilityLock, GluttonRules.LockPercent);
                    if (locked > 0) hit.Add(HitLayer.Increased, locked);
                }
                SideState mine = _sides[side];
                if (mine.FocusTicks > 0 && string.Equals(mine.FocusTargetId, target.Id, StringComparison.Ordinal))
                {
                    int wasted = Bonus(attacker, GluttonTalent.NothingWasted, GluttonRules.WastedPercent);
                    if (wasted > 0) hit.Add(HitLayer.Status, wasted);
                }
            }
        }

        // Any hit on this victim from the other side: Dark Matter Plating on a front row, and Deep Node on one of the Shade's
        // tricks.
        private void GluttonHit(int sourceSide, BattleUnit victim, Hit hit, string detail)
        {
            if (victim.Row == Row.Front && victim.Side >= 0 && victim.Side <= 1)
            {
                int plating = Bonus(victim, GluttonTalent.DarkMatterPlating, GluttonRules.PlatingPercent);
                if (plating > 0) hit.Add(HitLayer.Guard, -plating);
            }
            if (sourceSide >= 0 && sourceSide <= 1 && ShadeRules.IsTrickDetail(detail))
            {
                int node = Bonus(sourceSide, GluttonTalent.DeepNode, GluttonRules.DeepNodePercent);
                if (node > 0) hit.Add(HitLayer.Kit, node);
            }
        }

        // A heal's talents: Matter Printer, and Recompile on one under a quarter of his health.
        private void GluttonHeal(BattleUnit healer, BattleUnit patient, Hit heal)
        {
            int side = healer != null ? healer.Side : patient.Side;
            if (side < 0 || side > 1) return;
            int printer = healer != null ? Bonus(healer, GluttonTalent.MatterPrinter, GluttonRules.PrinterPercent) : Bonus(side, GluttonTalent.MatterPrinter, GluttonRules.PrinterPercent);
            if (printer > 0) heal.Add(HitLayer.Increased, printer);
            if (patient.HpPermille < GluttonRules.RecompileBelowPermille)
            {
                int recompile = healer != null ? Bonus(healer, GluttonTalent.Recompile, GluttonRules.RecompilePercent) : Bonus(side, GluttonTalent.Recompile, GluttonRules.RecompilePercent);
                if (recompile > 0) heal.Add(HitLayer.Status, recompile);
            }
        }

        // Dark Matter Rounds: a Marksman's blow passes through part of the armour.
        private int ArmorAgainst(BattleUnit attacker, int armor)
        {
            if (!_gluttonOn || attacker == null || armor <= 0 || attacker.Class != UnitClass.Marksman) return armor;
            int through = Math.Min(100, Bonus(attacker, GluttonTalent.DarkMatterRounds, GluttonRules.DarkRoundsPercent));
            return armor * (100 - through) / 100;
        }

        // ------------------------------------------------------------ clocks, shields, marks and snares

        // Entropy Sink: a front row fighter's clocks run faster, on the faster hook's percent.
        private int SinkPercent(BattleUnit unit)
        {
            if (!_gluttonOn || unit.Row != Row.Front || unit.Side < 0 || unit.Side > 1) return 0;
            return Bonus(unit, GluttonTalent.EntropySink, GluttonRules.SinkPercent);
        }

        // Branch Anchor: a Sanctuary shield on a front row fighter, thicker.
        private int Anchored(BattleUnit unit, int size)
        {
            if (!_gluttonOn || unit.Row != Row.Front) return size;
            return size * (100 + Bonus(unit, GluttonTalent.BranchAnchor, GluttonRules.AnchorPercent)) / 100;
        }

        // Strip: an Opening this side leaves lasts longer.
        private int Stripped(int side, int ticks)
        {
            if (!_gluttonOn || side < 0 || side > 1) return ticks;
            return ticks * (100 + Bonus(side, GluttonTalent.Strip, GluttonRules.StripPercent)) / 100;
        }

        // Time Lock: a snare this side lays holds longer.
        private int Locked(int side, int ticks)
        {
            if (!_gluttonOn || side < 0 || side > 1) return ticks;
            return ticks * (100 + Bonus(side, GluttonTalent.TimeLock, GluttonRules.TimeLockPercent)) / 100;
        }

        // Gravity Mesh: a snared enemy of this side's cannot Step Back.
        private bool Meshed(BattleUnit attacker, BattleUnit target)
        {
            return _gluttonOn && target.SnareTicks > 0 && HasTalent(attacker.Side, GluttonTalent.GravityMesh);
        }

        // Crawl Field: the wind up percent a Time Snare slows by, from the side whose Trapper laid it.
        private int CrawlPercent(BattleUnit unit)
        {
            int percent = ClassSpells.TimeSnarePercent;
            if (!_gluttonOn) return percent;
            int side = 1 - unit.Side;
            return side < 0 || side > 1 ? percent : percent + Bonus(side, GluttonTalent.CrawlField, GluttonRules.CrawlPercent);
        }

        // How many places either side a pull reaches: Neutron Mass for Event Horizon, Stasis Wind Up for Singularity Net.
        private int Reach(BattleUnit by, ClassBend bend)
        {
            if (!_gluttonOn) return 1;
            if (bend == ClassBend.EventHorizon) return 1 + Bonus(by, GluttonTalent.NeutronMass, GluttonRules.NeutronReach - 1);
            if (bend == ClassBend.SingularityNet) return 1 + Bonus(by, GluttonTalent.StasisWindUp, GluttonRules.StasisReach);
            return 1;
        }

        // The ones up to reach places either side of this one in its row, standing, the nearest first.
        private List<BattleUnit> Beside(BattleUnit unit, int reach)
        {
            if (reach <= 1) return BrandedAndBeside(unit);
            var row = new List<BattleUnit>();
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (u.Side == unit.Side && u.Row == unit.Row && !u.Benched) row.Add(u);
            }
            int at = row.IndexOf(unit);
            var near = new List<BattleUnit> { unit };
            for (int d = 1; d <= reach && at >= 0; d++)
            {
                if (at - d >= 0 && row[at - d].Alive) near.Add(row[at - d]);
                if (at + d < row.Count && row[at + d].Alive) near.Add(row[at + d]);
            }
            return near;
        }

        // Fold Trap: a Doorway Trapper's trap springs this many more times.
        private int FoldTrapSprings(BattleUnit trapper)
        {
            if (!_gluttonOn || trapper == null || !Wears(trapper, ClassBend.Doorway)) return 0;
            return Bonus(trapper, GluttonTalent.FoldTrap, GluttonRules.FoldTrapMore);
        }

        // ------------------------------------------------------------ the combos and their pacts

        // Light Stake: Black Hole Round's crush. Fold Step: Execution Corridor's blow. Slow Light Rounds: Stopped Bullets' freeze.
        private int StakePercent(BattleUnit marksman)
        {
            return ClassSpells.BlackHoleCrushPercent * (100 + (_gluttonOn ? Bonus(marksman, GluttonTalent.LightStake, GluttonRules.StakePercent) : 0)) / 100;
        }

        private int CorridorPercent(BattleUnit skirmisher)
        {
            return ClassSpells.CorridorPercent + (_gluttonOn ? Bonus(skirmisher, GluttonTalent.FoldStep, GluttonRules.FoldStepPercent) : 0);
        }

        private int FreezeTicks(BattleUnit marksman)
        {
            return ClassSpells.StoppedFreezeTicks + (_gluttonOn ? Bonus(marksman, GluttonTalent.SlowLightRounds, GluttonRules.SlowLightTicks) : 0);
        }

        // Synthesis Discount: what is left of a pact's cost, in percent.
        private int PactLeft(int side)
        {
            if (!_gluttonOn || side < 0 || side > 1) return 100;
            int left = Math.Max(0, 100 - Bonus(side, GluttonTalent.SynthesisDiscount, GluttonRules.DiscountPercent));
            // The sizing pass (rules 31, BattleSimulation.Sized.cs).
            return _sizedOn ? left * SizeRules.PactPercent / 100 : left;
        }

        // Heavy's percent, Unnerving's share of nerve (in percent of an eighth: Fold Shield and Entropy Purge halve it for the
        // front row and for Medics) and Borrowed time's permille, after the discount.
        private int HeavyPactPercent(BattleUnit unit)
        {
            return 100 + (ClassSpells.HeavyPactPercent - 100) * PactLeft(unit.Side) / 100;
        }

        private int UnnervingPercent(BattleUnit unit)
        {
            int percent = PactLeft(unit.Side);
            if (!_gluttonOn) return percent;
            if (unit.Row == Row.Front) percent = percent * (100 - Math.Min(100, Bonus(unit, GluttonTalent.FoldShield, GluttonRules.PortalNervePercent))) / 100;
            if (unit.Class == UnitClass.Medic) percent = percent * (100 - Math.Min(100, Bonus(unit, GluttonTalent.EntropyPurge, GluttonRules.PortalNervePercent))) / 100;
            return percent;
        }

        private int BorrowedPermille(BattleUnit unit)
        {
            return 1000 - (1000 - ClassSpells.BorrowedTimePermille) * PactLeft(unit.Side) / 100;
        }

        // ------------------------------------------------------------ the Medic, the Whisperer's pairs and the Shade

        // Synth Armour: a treatment on a Guarded ally, bigger by this many more percent.
        private int SynthBonus(BattleUnit medic)
        {
            return _gluttonOn ? Bonus(medic, GluttonTalent.SynthArmour, GluttonRules.SynthPercent) : 0;
        }

        // Rewind Wound: how many seconds this Medic's Rewind looks back.
        private int RewindSecondsOf(BattleUnit medic)
        {
            return ClassSpells.RewindSeconds + (_gluttonOn ? Bonus(medic, GluttonTalent.RewindWound, GluttonRules.RewindMoreSeconds) : 0);
        }

        // Gene Library: how long a pair waits between free answers.
        private int BondGap(int side)
        {
            if (!_gluttonOn || side < 0 || side > 1) return ChainRules.BondGapTicks;
            return ChainRules.BondGapTicks * (100 - Math.Min(90, Bonus(side, GluttonTalent.GeneLibrary, GluttonRules.LibraryPercent))) / 100;
        }

        // Echo Strike: a Skirmisher's echo keeps more.
        private int EchoShare(BattleUnit striker)
        {
            int share = ShadeRules.EchoPercent;
            if (_gluttonOn && striker.Class == UnitClass.Skirmisher) share += Bonus(striker, GluttonTalent.EchoStrike, GluttonRules.EchoStrikePercent);
            return share;
        }

        // Command Relay: what is left of a Shade route's price, in percent.
        private int PriceLeft(int side)
        {
            if (side < 0 || side > 1) return 100;
            int left = _gluttonOn ? Math.Max(0, 100 - Bonus(side, GluttonTalent.CommandRelay, GluttonRules.RelayPricePercent)) : 100;
            // Gorged Tricks (rules 30, BattleSimulation.Duos.cs) halves what is left, and the sizing pass (rules 31) sizes it.
            if (_duosOn) left = GorgedPrice(side, left);
            return _sizedOn ? PriceSized(left) : left;
        }

        // Precognition: how many ticks before a beat a live Hold parries it.
        private int ParryWindow(int side)
        {
            // The window it widens is the tight parry's from rules 26 (BattleSimulation.Tight.cs).
            if (!_gluttonOn || side < 0 || side > 1) return ParryWindowTicks;
            return ParryWindowTicks * (100 + Bonus(side, GluttonTalent.Precognition, GluttonRules.PrecognitionPercent)) / 100;
        }

        // Glancing Fold: a Hold that soaks takes this percent of the hit, soaking more.
        private int Glanced(int side, int taken)
        {
            if (!_gluttonOn || side < 0 || side > 1) return taken;
            int more = Bonus(side, GluttonTalent.GlancingBlow, GluttonRules.GlancingPercent);
            return more <= 0 ? taken : Math.Max(0, 100 - (100 - taken) * (100 + more) / 100);
        }

        // Charged Branches: what each held parry bar adds.
        private int PerBar(int side)
        {
            return MeterRules.PercentPerBar + (_gluttonOn ? Bonus(side, GluttonTalent.ChargedBranches, GluttonRules.ChargedPercent) : 0);
        }

        // ------------------------------------------------------------ Rewind's longer memory and the snapshot

        // Rewind Wound's memory: what each fighter lost in each of the last few seconds, kept only where the talents play.
        private void HurtLonger(BattleUnit victim, int hpLost)
        {
            long second = _tick / SimClock.TicksPerSecond;
            int slot = (int)(second % victim.HurtLongLost.Length);
            if (victim.HurtLongSecond[slot] != second)
            {
                victim.HurtLongSecond[slot] = second;
                victim.HurtLongLost[slot] = 0;
            }
            victim.HurtLongLost[slot] += hpLost;
        }

        private int LostOver(BattleUnit unit, int seconds)
        {
            long second = _tick / SimClock.TicksPerSecond;
            int lost = 0;
            for (int k = 0; k < unit.HurtLongLost.Length; k++)
                if (unit.HurtLongSecond[k] >= 0 && second - unit.HurtLongSecond[k] < seconds) lost += unit.HurtLongLost[k];
            return lost;
        }

        private void AppendGluttonUnit(SnapshotBuilder b, BattleUnit u)
        {
            b.Field(u.Id + ".parriedAt", u.ParriedAtTick);
            b.Field(u.Id + ".parriedBy", u.ParriedBySide);
            for (int k = 0; k < u.HurtLongLost.Length; k++)
            {
                b.Field(u.Id + ".hurtLong" + k, u.HurtLongLost[k]);
                b.Field(u.Id + ".hurtLongAt" + k, u.HurtLongSecond[k]);
            }
        }
    }
}
