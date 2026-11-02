using System;
using System.Collections.Generic;
using KindredSiege.Sim.Battle;

namespace KindredSiege.Sim.Learning
{
    /// The Pals' duos on the road (campaign rules 30; Jacob in chat, 1 Nov 2026: "go write them"; Battle.BattleSimulation.Duos.cs
    /// plays them). Once the run holds a pick from both Pals of a pair, the first card of either shows their duo as one more
    /// pick, and after that one card of theirs in DuoAgainOneIn, until it is taken. Taking it is a pick like any: it costs the
    /// fight's gear. A duo is held for the run. Every number is a starting value.
    public sealed partial class Campaign
    {
        /// After a duo first shows, one card of its Pals in this many shows it again. A starting value: with the about 2 cards
        /// of the two Pals a run sees once it holds both (the duo timing check, HANDOFF_LOG 1 Nov 2026), a duo shows about
        /// once or twice a run.
        public const int DuoAgainOneIn = 3;

        // The road node's own dice for whether a duo shows again: a stream of its seed nothing else draws from.
        private const ulong DuoOfferStream = 19UL;

        // The duos the run holds, and the ones a card has shown, one bit a duo (1 << (int)PalDuo).
        private int _duos;
        private int _duosShown;

        /// Whether this run plays the duos: campaign rules 30.
        public bool DuosRun => Math.Min(RulesVersion, _tuning.RulesCap) >= PalDuoRules;

        /// The duos the run holds, in PalDuo's order.
        public IReadOnlyList<PalDuo> Duos
        {
            get
            {
                var held = new List<PalDuo>();
                for (int d = 1; d < DuoRules.Count; d++) if ((_duos & (1 << d)) != 0) held.Add((PalDuo)d);
                return held;
            }
        }

        public bool HoldsDuo(PalDuo duo) => DuoRules.Valid(duo) && (_duos & (1 << (int)duo)) != 0;

        /// Whether a card has shown this duo this run.
        public bool DuoShown(PalDuo duo) => DuoRules.Valid(duo) && (_duosShown & (1 << (int)duo)) != 0;

        /// Whether the run holds a pick from this Pal, as a duo counts it: a combo for the Butcher; a fighter ranked up, a pair or
        /// an upgraded habit for the Whisperer; a route for the Shade; a talent lent for the Glutton.
        public bool HoldsPickOf(Pal pal)
        {
            switch (pal)
            {
                case Pal.Butcher:
                    for (int i = 0; i < _gear.Count; i++) if (ClassSpells.IsCombo(_gear[i].Bend)) return true;
                    return false;
                case Pal.Whisperer:
                    if (Pair != SquadPair.None) return true;
                    for (int i = 0; i < _reserve.Count; i++) if (_reserve[i].Rank > 1) return true;
                    for (int i = 0; i < _habits.Count; i++) if (_habits[i].Upgraded) return true;
                    return false;
                case Pal.Shade: return _routes.Count > 0;
                case Pal.Glutton: return _talents.Count > 0;
                default: return false;
            }
        }

        /// A duo for the run, as its pick gives it. False outside a run, before rules 30, or held already.
        public bool TryTakeDuo(PalDuo duo)
        {
            if (!DuosRun || Stage != CampaignStage.InRun || !DuoRules.Valid(duo) || HoldsDuo(duo)) return false;
            _duos |= 1 << (int)duo;
            return true;
        }

        // This card with the duos it shows added after its picks: each duo of its Pal the run can take, holding a pick from both,
        // on the first card since that held, and after it on one in DuoAgainOneIn by the node's own dice. A Whisperer card
        // carries an empty pick for each, so its picks still line up with its choices.
        private FavourOffer WithDuos(RoadNode node, FavourOffer offer)
        {
            if (offer == null) return null;
            var dice = new DeterministicRng(node.Seed, DuoOfferStream);
            List<Favour> choices = null;
            for (int d = 1; d < DuoRules.Count; d++)
            {
                var duo = (PalDuo)d;
                if (!FavourRules.DuoHas(duo, offer.Pal) || HoldsDuo(duo)) continue;
                if (!HoldsPickOf(FavourRules.FirstOf(duo)) || !HoldsPickOf(FavourRules.SecondOf(duo))) continue;
                if (DuoShown(duo) && dice.Next(DuoAgainOneIn) != 0) continue;
                if (choices == null) choices = new List<Favour>(offer.Choices);
                choices.Add(FavourRules.DuoPick(duo));
            }
            if (choices == null) return offer;
            if (offer.Pal != Pal.Whisperer) return new FavourOffer(offer.Pal, choices);
            var picks = new List<WhisperPick>(offer.Picks);
            for (int i = picks.Count; i < choices.Count; i++) picks.Add(new WhisperPick(choices[i], string.Empty, WhispererHabit.None, SquadPair.None));
            return new FavourOffer(offer.Pal, choices, picks);
        }

        // A card was put on offer: the duos it shows have shown.
        private void MarkDuosShown(FavourOffer offer)
        {
            for (int i = 0; i < offer.Choices.Count; i++)
                if (FavourRules.IsDuo(offer.Choices[i])) _duosShown |= 1 << (int)FavourRules.DuoOf(offer.Choices[i]);
        }

        // Takes a duo pick; its name for the codex.
        private string TakeDuo(Favour favour)
        {
            PalDuo duo = FavourRules.DuoOf(favour);
            _duos |= 1 << (int)duo;
            return DuoRules.Name(duo);
        }

        private void ClearDuos()
        {
            _duos = 0;
            _duosShown = 0;
        }

        // For CampaignSave (version 30): the duos held and shown.
        internal int DuoMask => _duos;
        internal int DuosShownMask => _duosShown;

        internal bool RestoreDuos(int mask, int shown)
        {
            int valid = 0;
            for (int d = 1; d < DuoRules.Count; d++) valid |= 1 << d;
            if ((mask & ~valid) != 0 || (shown & ~valid) != 0 || _duos != 0 || _duosShown != 0) return false;
            _duos = mask;
            _duosShown = shown;
            return true;
        }
    }
}
