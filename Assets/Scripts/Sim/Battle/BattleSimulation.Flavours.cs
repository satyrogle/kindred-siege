using System;
using System.Collections.Generic;

namespace KindredSiege.Sim.Battle
{
    /// The Butcher's flavours (campaign rules 29; FightRules.Flavours; Jacob in chat, 2 Nov 2026: "i think both 1 and 2"). A
    /// combo grows with the ranks of combos of its flavour the side wears (SizeRules.FlavourComboPercent, FlavourHoldPercent,
    /// FlavourReach), and SizeRules.FinalRanks of one flavour unlock its final form: Gravity's Crush crushes a whole row,
    /// the Fold Door's wide open jumps on, Time Stop's Stasis freezes their whole side. Every number is a starting value.
    public sealed partial class BattleSimulation
    {
        private readonly bool _flavoursOn;
        private readonly bool _marksOnHitsOn;

        /// Whether a combo's wearer marks with every blow: campaign rules 30.
        public bool MarksOnHitsOn => _marksOnHitsOn;

        /// Whether this fight plays the Butcher's flavours: campaign rules 29.
        public bool FlavoursOn => _flavoursOn;

        /// The ranks of combos laying this mark that this side wears, for the screen's flavour count.
        public int FlavourRanksOf(int side, StatusKind mark) => _flavoursOn && side >= 0 && side <= 1 ? FlavourRanks(side, mark) : 0;

        // Whether this side's flavour of this mark is at its final form.
        private bool FinalForm(int side, StatusKind mark) => _flavoursOn && FlavourRanks(side, mark) >= SizeRules.FinalRanks;

        // The ranks past the first of the flavour of this combo on its wearer's side.
        private int MoreRanks(BattleUnit by, ClassBend combo)
        {
            if (!_flavoursOn || by == null || !ClassSpells.IsCombo(combo)) return 0;
            return Math.Max(0, FlavourRanks(by.Side, MarkRules.Of(combo)) - 1);
        }

        // The ranks past the first of the deepest flavour this fighter's own combos carry.
        private int MoreRanks(BattleUnit by)
        {
            if (by == null) return 0;
            return Math.Max(MoreRanks(by, BendOf(by)), MoreRanks(by, Bend2Of(by)));
        }

        // A combo's hit: how many percent bigger for its flavour's ranks.
        private int FlavourHitPercent(BattleUnit source, string detail)
        {
            if (!_flavoursOn || source == null) return 0;
            if (detail != ClassSpells.BlackHoleDetail && detail != ClassSpells.EventHorizonDetail && detail != ClassSpells.CorridorDetail &&
                detail != ClassSpells.FlurryDetail) return 0;
            return SizeRules.FlavourComboPercent * MoreRanks(source);
        }

        // A combo's pin, freeze or slow, longer for its flavour's ranks.
        private int FlavourHold(BattleUnit by, ClassBend combo, int ticks)
        {
            int more = MoreRanks(by, combo);
            return more == 0 ? ticks : ticks * (100 + SizeRules.FlavourHoldPercent * more) / 100;
        }

        // A burst's pin or freeze, longer for its flavour's ranks.
        private int FlavourHold(BattleUnit by, StatusKind mark, int ticks)
        {
            if (!_flavoursOn || by == null) return ticks;
            int more = Math.Max(0, FlavourRanks(by.Side, mark) - 1);
            return more == 0 ? ticks : ticks * (100 + SizeRules.FlavourHoldPercent * more) / 100;
        }

        // How many more places a Gravity pull reaches.
        private int FlavourReachOf(BattleUnit by)
        {
            if (!_flavoursOn || by == null) return 0;
            return SizeRules.FlavourReach * Math.Max(0, FlavourRanks(by.Side, StatusKind.Crush) - 1);
        }

        // ------------------------------------------------------------ marks on every hit (rules 30)

        // A blow of a fighter wearing a combo landed: SizeRules.HitStacks stacks of his deepest combo's mark on him, bursting at
        // the cap. A chief whose band stands shrugs it off.
        private void HitMark(BattleUnit attacker, BattleUnit target)
        {
            if (attacker == null || target.Side == attacker.Side || ShrugsMarks(target) || SizeRules.HitStacks <= 0) return;
            ClassBend combo = ClassSpells.IsCombo(BendOf(attacker)) ? BendOf(attacker) : ClassBend.None;
            ClassBend second = Bend2Of(attacker);
            if (ClassSpells.IsCombo(second) && (combo == ClassBend.None || MoreRanks(attacker, second) > MoreRanks(attacker, combo))) combo = second;
            if (combo == ClassBend.None) return;
            StatusKind mark = MarkRules.Of(combo);
            target.Statuses.Apply(mark, MarkRules.Ticks, sourceId: attacker.Id, stacks: SizeRules.HitStacks);
            Emit(new BattleEvent(_tick, BattleEventKind.Mark, attacker.Side, attacker.Id, (int)attacker.Class, target.Id, (int)target.Class,
                MarkRules.Ticks, MarkRules.Name(mark)));
            if (target.Statuses[mark].Stacks >= MarkRules.MaxStacks) Burst(attacker, target, mark);
        }

        // ------------------------------------------------------------ the final forms

        // Gravity's final form: a Crush burst on him crushes every other standing enemy of his row as hard, and pins them.
        private void CrushRow(BattleUnit by, BattleUnit target, Hit crush, int damage, int pin)
        {
            if (!FinalForm(by.Side, StatusKind.Crush)) return;
            var row = new List<BattleUnit>();
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (u.Alive && u.Side == target.Side && u.Row == target.Row && !u.Benched && !ReferenceEquals(u, target) && !ShrugsMarks(u)) row.Add(u);
            }
            Emit(EventFor(BattleEventKind.Burst, by, target, row.Count, SizeRules.CrushRowDetail));
            for (int k = 0; k < row.Count && !_finished; k++)
            {
                if (crush != null) ApplyHit(by, by.Side, row[k], crush.Copy(), MarkRules.CrushedDetail);
                else ApplyDamage(by, by.Side, row[k], damage, MarkRules.CrushedDetail);
                if (!_finished && row[k].Alive) Pin(row[k], pin);
            }
        }

        // The Fold Door's final form: a wide open that is spent jumps on to the next standing enemy beside him, while it has
        // jumps left (its Store).
        private void JumpWideOpen(BattleUnit target, int jumps, int size, string sourceId)
        {
            if (!_flavoursOn || jumps <= 0 || _finished) return;
            List<BattleUnit> beside = BrandedAndBeside(target);
            for (int k = 1; k < beside.Count; k++)
            {
                BattleUnit next = beside[k];
                if (!next.Alive || next.Side != target.Side || next.Statuses[StatusKind.WideOpen].On || ShrugsMarks(next)) continue;
                next.Statuses.Apply(StatusKind.WideOpen, MarkRules.WideOpenTicks, size: size, sourceId: sourceId);
                next.Statuses[StatusKind.WideOpen].Store = jumps - 1;
                BattleUnit by = FindById(sourceId);
                Emit(new BattleEvent(_tick, BattleEventKind.Burst, 1 - target.Side, sourceId, by == null ? -1 : (int)by.Class,
                    next.Id, (int)next.Class, MarkRules.WideOpenTicks, SizeRules.ExposedJumpDetail));
                return;
            }
        }

        // Time Stop's final form: a Stasis burst freezes every standing enemy of his side for StasisAllTicks.
        private void StasisAll(BattleUnit by, BattleUnit target)
        {
            if (!FinalForm(by.Side, StatusKind.Stasis)) return;
            int frozen = 0;
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (!u.Alive || u.Side != target.Side || u.Benched || ReferenceEquals(u, target) || ShrugsMarks(u)) continue;
                Pin(u, SizeRules.StasisAllTicks);
                frozen++;
            }
            Emit(EventFor(BattleEventKind.Burst, by, target, frozen, SizeRules.StasisAllDetail));
        }
    }
}
