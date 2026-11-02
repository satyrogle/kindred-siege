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
        private FavourOffer WithDuos(RoadNode node, FavourOffer offer, int rules)
        {
            if (offer == null) return null;
            var dice = new DeterministicRng(node.Seed, DuoOfferStream);
            List<Favour> choices = null;
            bool moves = rules >= DuoMovesRules;
            for (int d = 1; d < DuoRules.Count; d++)
            {
                var duo = (PalDuo)d;
                // Before rules 34 the first six; from 34 the six that change a move, each on its own exact picks.
                if (DuoRules.IsMove(duo) != moves) continue;
                if (!FavourRules.DuoHas(duo, offer.Pal) || HoldsDuo(duo)) continue;
                if (moves ? !MoveQualifies(duo) : !HoldsPickOf(FavourRules.FirstOf(duo)) || !HoldsPickOf(FavourRules.SecondOf(duo))) continue;
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
            _duoLevel = 0;
        }

        // ------------------------------------------------------------ the duos that change a move (rules 34)

        // The duos' streak the run carries from fight to fight: 1 to Battle.DuoRules.MaxLevel once it holds one, else 0.
        private int _duoLevel;

        /// The duos' streak: big moves answered in a row raise it, one that lands in full, a fighter falling or a lost fight
        /// drops it to 1. 0 until the run holds one of the rules 34 duos.
        public int DuoLevel => _duoLevel;

        /// Whether this run plays the rules 34 duos.
        public bool DuoMovesRun => Math.Min(RulesVersion, _tuning.RulesCap) >= DuoMovesRules;

        /// Whether the run holds the exact picks this rules 34 duo needs: Blood Parry a combo and a Hold route (Mirror or
        /// Sanctuary); Chain Reaction a combo and a tier 2 talent; Executioner a combo and a fighter with a vow; Endless Unleash
        /// an Unleash route (Breach or Echo) and a Strikers talent; Shadow Twin a fighter with a vow and a Fall Back route
        /// (Backstab or Rift Mine); Gorge a fighter with a vow and a talent of his class's limb.
        public bool MoveQualifies(PalDuo duo)
        {
            switch (duo)
            {
                case PalDuo.BloodParry: return HoldsPickOf(Pal.Butcher) && (RouteHeld(ShadeRoute.Mirror) || RouteHeld(ShadeRoute.Sanctuary));
                case PalDuo.ChainReaction: return HoldsPickOf(Pal.Butcher) && AnyTierTwo();
                case PalDuo.Executioner: return HoldsPickOf(Pal.Butcher) && VowedFighter(-1) != null;
                case PalDuo.EndlessUnleash: return (RouteHeld(ShadeRoute.Breach) || RouteHeld(ShadeRoute.Echo)) && LimbHeld(1);
                case PalDuo.ShadowTwin: return VowedFighter(-1) != null && (RouteHeld(ShadeRoute.Backstab) || RouteHeld(ShadeRoute.RiftMine));
                case PalDuo.Gorge:
                    for (int i = 0; i < _reserve.Count; i++)
                        if (_reserve[i].Rank >= 2 && _reserve[i].Vow != StarVow.None && LimbHeld(DuoRules.LimbOf(_reserve[i].Class))) return true;
                    return false;
                default: return false;
            }
        }

        private bool RouteHeld(ShadeRoute route)
        {
            for (int i = 0; i < _routes.Count; i++) if (_routes[i].Route == route) return true;
            return false;
        }

        private bool AnyTierTwo()
        {
            for (int limb = 0; limb < GluttonRules.LimbCount; limb++) if (HoldsTalent(GluttonRules.At(limb, 1))) return true;
            return false;
        }

        private bool LimbHeld(int limb)
        {
            for (int slot = 0; slot < GluttonRules.PerLimb; slot++) if (HoldsTalent(GluttonRules.At(limb, slot))) return true;
            return false;
        }

        // A ranked fighter with a vow, of this class (-1 for any), or null.
        private CampaignFighter VowedFighter(int cls)
        {
            for (int i = 0; i < _reserve.Count; i++)
                if (_reserve[i].Rank >= 2 && _reserve[i].Vow != StarVow.None && (cls < 0 || (int)_reserve[i].Class == cls)) return _reserve[i];
            return null;
        }

        // A rules 34 duo taken: it costs nothing, the card stays with the duo off it, and the streak starts at 1.
        private void TakeFreeDuo(int choice)
        {
            Favour favour = _favourOffer.Choices[choice];
            PalDuo duo = FavourRules.DuoOf(favour);
            _duos |= 1 << (int)duo;
            _duoLevel = Math.Max(1, _duoLevel);
            var choices = new List<Favour>(_favourOffer.Choices);
            choices.RemoveAt(choice);
            List<WhisperPick> picks = null;
            if (_favourOffer.Picks.Count > 0)
            {
                picks = new List<WhisperPick>(_favourOffer.Picks);
                if (choice < picks.Count) picks.RemoveAt(choice);
            }
            _favourOffer = choices.Count == 0 ? null : new FavourOffer(_favourOffer.Pal, choices, picks);
            Discover("Favour: " + DuoRules.Name(duo) + " (" + FavourRules.PalName(FavourRules.FirstOf(duo)) + " and " +
                FavourRules.PalName(FavourRules.SecondOf(duo)) + ")");
        }

        // After a fight from rules 34: the streak it ended on comes back, and a lost fight drops it to 1.
        private void StreakAfter(CampaignFight fight, bool won)
        {
            if (_duoLevel <= 0) return;
            int level = fight.Sim.DuoLevelOf(0);
            _duoLevel = !won ? 1 : Math.Max(1, Math.Min(DuoRules.MaxLevel, level > 0 ? level : _duoLevel));
        }

        // For CampaignSave (version 32): the streak.
        internal bool RestoreDuoLevel(int level)
        {
            if (level < 1 || level > DuoRules.MaxLevel || _duoLevel != 0) return false;
            _duoLevel = level;
            return true;
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
