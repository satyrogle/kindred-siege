using System;

namespace KindredSiege.Sim.Battle
{
    /// The Pals' duos in the fight (campaign rules 30; FightRules.Duos; Jacob in chat, 1 Nov 2026: "go write them"). Each side
    /// brings the duos it holds in its PalKit (PalDuo, DuoRules). The star is the side's fighter of the highest rank, 2 or more,
    /// the first of a tie in setup order; he stays the star if he falls, so a duo on him dies with him. Her tricks are those
    /// that hit (ShadeRules.IsTrickDetail): Mirror, Rift Mine, Rupture, Breach, Ram, Echo and Turncoat. Every number is a
    /// starting value.
    public sealed partial class BattleSimulation
    {
        private readonly bool _duosOn;

        /// Whether this fight plays the duos: campaign rules 30.
        public bool DuosOn => _duosOn;

        /// Whether this side holds this duo in this fight.
        public bool HasDuo(int side, PalDuo duo)
        {
            return _duosOn && side >= 0 && side <= 1 && KitOf(side).HasDuo(duo);
        }

        /// This side's star: its fighter of the highest rank, 2 or more, standing or not; null with none ranked.
        public BattleUnit StarOf(int side)
        {
            BattleUnit star = null;
            int top = 1;
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (u.Side != side || u.Benched) continue;
                int rank = SpecOf(u).Rank;
                if (rank > top)
                {
                    star = u;
                    top = rank;
                }
            }
            return star;
        }

        private bool IsStar(BattleUnit unit) => unit != null && ReferenceEquals(StarOf(unit.Side), unit);

        // ------------------------------------------------------------ Crowned Marks and Gorged Bursts (the Butcher's)

        // Crowned Marks: the stacks the star lays, times CrownStacks.
        private int CrownedStacks(BattleUnit wearer, int stacks)
        {
            return HasDuo(wearer.Side, PalDuo.CrownedMarks) && IsStar(wearer) ? stacks * (_sizedOn ? SizeRules.CrownStacks : DuoRules.CrownStacks) : stacks;
        }

        // A burst went off (Burst): Crowned Marks gives the star who set it off his spell again.
        private void DuoBurst(BattleUnit by)
        {
            if (HasDuo(by.Side, PalDuo.CrownedMarks) && by.Alive && IsStar(by)) _sides[by.Side].StarCastDue = 2;
        }

        // Gorged Bursts: how many percent bigger this one's bursts are, from the talent levels held in his class's limb.
        private int GorgedPercent(BattleUnit by)
        {
            if (by == null || !HasDuo(by.Side, PalDuo.GorgedBursts)) return 0;
            int limb = DuoRules.LimbOf(by.Class), levels = 0;
            for (int slot = 0; slot < GluttonRules.PerLimb; slot++) levels += KitOf(by.Side).TalentLevel(GluttonRules.At(limb, slot));
            return levels * (_sizedOn ? SizeRules.GorgedBurstPercent : DuoRules.GorgedBurstPercent);
        }

        // ------------------------------------------------------------ her tricks: Marked Tricks, Shadow Star, Gorged Tricks

        // One of this side's tricks hit him (Land): Marked Tricks lays a stack, Shadow Star calls the star's spell.
        private void DuoLanded(int sourceSide, BattleUnit victim, string detail)
        {
            if (sourceSide < 0 || sourceSide > 1 || victim.Side == sourceSide || !ShadeRules.IsTrickDetail(detail)) return;
            SideState state = _sides[sourceSide];
            if (HasDuo(sourceSide, PalDuo.ShadowStar) && _tick >= state.ShadowStarAt)
            {
                BattleUnit star = StarOf(sourceSide);
                if (star != null && star.Alive)
                {
                    state.StarCastDue = 1;
                    state.ShadowStarAt = _tick + (_sizedOn ? SizeRules.ShadowStarTicks : DuoRules.ShadowStarTicks);
                }
            }
            if (HasDuo(sourceSide, PalDuo.MarkedTricks) && !_finished && victim.Alive) TrickMark(sourceSide, victim);
        }

        // Marked Tricks: a stack of the side's leading flavour on him, laid for the first standing fighter who wears a combo of
        // it (whose power a Crush burst hits for). Nothing without one, and a chief whose band stands shrugs it off.
        private void TrickMark(int side, BattleUnit victim)
        {
            if (!_marksOn || ShrugsMarks(victim)) return;
            Augment lead = LeadingFlavour(side);
            if (lead == Augment.None) return;
            BattleUnit by = null;
            ClassBend combo = ClassBend.None;
            for (int i = 0; i < _units.Count && by == null; i++)
            {
                BattleUnit u = _units[i];
                if (u.Side != side || !u.Alive || u.Benched) continue;
                if (ClassSpells.IsCombo(BendOf(u)) && ClassSpells.AugmentOf(BendOf(u)) == lead) combo = BendOf(u);
                else if (ClassSpells.IsCombo(Bend2Of(u)) && ClassSpells.AugmentOf(Bend2Of(u)) == lead) combo = Bend2Of(u);
                else continue;
                by = u;
            }
            if (by == null) return;
            StatusKind mark = MarkRules.Of(combo);
            victim.Statuses.Apply(mark, MarkRules.Ticks, sourceId: by.Id, stacks: _sizedOn ? SizeRules.MarkedStacks : 1);
            Emit(new BattleEvent(_tick, BattleEventKind.Mark, side, by.Id, (int)by.Class, victim.Id, (int)victim.Class,
                MarkRules.Ticks, MarkRules.Name(mark)));
            if (victim.Statuses[mark].Stacks >= MarkRules.MaxStacks) Burst(by, victim, mark);
        }

        // The flavour this side's combos carry most, counting each piece's rank; the first in Augment's order of a tie. None
        // without a combo.
        private Augment LeadingFlavour(int side)
        {
            var counts = new int[(int)Augment.TimeStop + 1];
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (u.Side != side || u.Benched) continue;
                UnitSpec spec = SpecOf(u);
                if (ClassSpells.IsCombo(BendOf(u))) counts[(int)ClassSpells.AugmentOf(BendOf(u))] += spec.ComboRank;
                if (ClassSpells.IsCombo(Bend2Of(u))) counts[(int)ClassSpells.AugmentOf(Bend2Of(u))] += spec.ComboRank2;
            }
            Augment lead = Augment.None;
            for (int a = 1; a < counts.Length; a++)
                if (counts[a] > (lead == Augment.None ? 0 : counts[(int)lead])) lead = (Augment)a;
            return lead;
        }

        // Gorged Tricks: how many percent harder her tricks hit, from every talent level held.
        private int GorgedTrickPercent(int side)
        {
            if (!HasDuo(side, PalDuo.GorgedTricks)) return 0;
            int levels = 0;
            for (int t = 1; t <= GluttonRules.TalentCount; t++) levels += KitOf(side).TalentLevel((GluttonTalent)t);
            return levels * (_sizedOn ? SizeRules.GorgedTrickPercent : DuoRules.GorgedTrickPercent);
        }

        // Gorged Tricks: what is left of her route prices, in percent of what Command Relay left.
        private int GorgedPrice(int side, int left)
        {
            return HasDuo(side, PalDuo.GorgedTricks) ? left * (_sizedOn ? SizeRules.GorgedPricePercent : DuoRules.GorgedPricePercent) / 100 : left;
        }

        // ------------------------------------------------------------ Fattened Star (the Glutton's)

        // A talent's level on this fighter: a held one counts as fed on the star with Fattened Star.
        private int TalentLevelOf(BattleUnit unit, GluttonTalent talent)
        {
            int level = TalentLevelOf(unit.Side, talent);
            if (level == GluttonRules.Held && HasDuo(unit.Side, PalDuo.FattenedStar) && IsStar(unit)) return GluttonRules.Fed;
            return level;
        }

        private int Bonus(BattleUnit unit, GluttonTalent talent, int bonus)
        {
            // Sized from rules 31 (BattleSimulation.Sized.cs), and more again on a Fattened Star.
            if (!_sizedOn) return GluttonRules.Bonus(TalentLevelOf(unit, talent), bonus);
            int sized = GluttonSized(unit.Side, talent, bonus);
            if (SizeRules.FattenedPercent > 0 && sized >= SizeRules.GluttonMinBonus && HasDuo(unit.Side, PalDuo.FattenedStar) && IsStar(unit))
                sized = sized * (100 + SizeRules.FattenedPercent) / 100;
            return GluttonRules.Bonus(TalentLevelOf(unit, talent), sized);
        }

        // ------------------------------------------------------------ the star's spell, once the tick's turns are done

        // A cast a duo called this tick goes now, free: the star's spell is ready and he acts, and his own turn stays where it
        // was. While he is held (a snare, an aim, a pin, a heavy swing, his kit) it waits; it goes with him if he falls.
        private void TickDuos()
        {
            for (int side = 0; side < 2 && !Stopped; side++)
            {
                SideState state = _sides[side];
                if (state.StarCastDue == 0) continue;
                BattleUnit star = StarOf(side);
                if (star == null || !star.Alive)
                {
                    state.StarCastDue = 0;
                    continue;
                }
                if (star.PanicTicks > 0 || (_beastOn && IsHeld(star)) || (_heavyOn && IsSwingingHeavy(star)) || (_kitsOn && KitBusy(star)))
                    continue;
                if (_classOn && (star.SnareTicks > 0 || star.AimTicks > 0 || star.PinTicks > 0 || star.EvacTicks > 0)) continue;
                int which = state.StarCastDue;
                state.StarCastDue = 0;
                int keep = star.Cooldown;
                star.SpellTicks = 0;
                Emit(new BattleEvent(_tick, BattleEventKind.Trick, side, star.Id, (int)star.Class, string.Empty, -1, which, DuoRules.StarDetail));
                Act(star);
                if (star.Alive) star.Cooldown = keep;
                if (!Stopped) CheckWipe();
            }
        }

        private void AppendDuoSide(SnapshotBuilder b, SideState state)
        {
            b.Field("starCastDue", state.StarCastDue);
            b.Field("shadowStarAt", state.ShadowStarAt);
        }
    }
}
