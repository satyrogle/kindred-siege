using System.Collections.Generic;
using KindredSiege.Sim.Battle;

namespace KindredSiege.Sim.Learning
{
    public enum PilotStyle
    {
        /// Reads the tribe. Dodges their Eclipse and punishes the worn out caster; in a trial for a
        /// tribe that respects three answers it keeps two points back and breaks the first cast,
        /// then dodges and punishes. Keeps the pressure on with Unleash where the tribe allows it
        /// (SliceAutopilot, measured 6 Oct 2026). Where Unleash is taboo, focus is its weapon:
        /// their Warden first if he covers what you focus, then their Medic, then the most hurt.
        /// Holds the line with Hold where the tribe allows it, calls the assist once they are
        /// shaken, and picks encounters for feats it has not shown this run.
        Reference = 0,
        Random = 1,
        UnleashSpam = 2,
        FocusSpam = 3,
        HoldSpam = 4,

        /// Only dodges their Eclipse.
        Turtle = 5,

        /// Written to break things: rushes whoever leads them (his fall breaks the side), dodges
        /// every Eclipse, calls the assist at once, and farms the encounter it wins most easily.
        Cheater = 6,

        /// Falls back whenever it can, to keep the lines apart.
        FallBackSpam = 7,

        /// Gives no orders at all.
        Passive = 8
    }

    /// Scripted players for the campaign. Tests and balance numbers play with them; they read the
    /// fight and give orders through CampaignFight, the way a player would. What they cannot see:
    /// whether any of it is readable or fun, and they never tire or misread a windup.
    public sealed class CampaignPilot
    {
        public const int DodgeAtWindup = 60;
        private readonly PilotStyle _style;
        private readonly DeterministicRng _rng;
        // Its own dice for a duel's parries, so a pilot set to parry draws nothing else differently.
        private readonly DeterministicRng _parryRng;
        // And its own for a random Pal pick, so picking draws nothing else differently.
        private readonly DeterministicRng _pickRng;
        private bool _answered;
        private bool _breaking;
        private bool _broke;
        private int _scanned;
        private int _lastWindup;
        private long _focusUntil = -1;
        private long _unleashUntil = -1;
        private long _lastHold = -1;
        private bool _read;
        private bool _theyCover;

        public CampaignPilot(PilotStyle style, ulong seed)
        {
            _style = style;
            _rng = new DeterministicRng(seed, 77);
            _parryRng = new DeterministicRng(seed, 78);
            _pickRng = new DeterministicRng(seed, 79);
        }

        public PilotStyle Style => _style;

        /// In a champion duel (campaign rules 16, DESIGN_LOCKS "Duels"), how often in a thousand the pilot parries the
        /// champion's beat, a Hold on the tick before it; 0, the default, never, so every pilot plays as before. Whatever its
        /// style, how well a player parries, for measuring duels.
        public int DuelParryPermille { get; set; }

        /// In any fight, how often in a thousand the pilot parries a beat coming at it, a Hold on the tick before it, one roll a
        /// beat; 0, the default, never, so every pilot plays as before. For measuring what answering big moves is worth (the
        /// rules 34 duos' streak).
        public int ParryPermille { get; set; }

        /// Which offer to take.
        public int PickOffer(Campaign c)
        {
            if (c.Offers.Count <= 1) return 0;
            switch (_style)
            {
                case PilotStyle.Random:
                    return _rng.Next(c.Offers.Count);
                case PilotStyle.Cheater:
                {
                    for (int i = 0; i < c.Offers.Count; i++) if (c.Offers[i].Kind == EncounterKind.WarParty) return i;
                    for (int i = 0; i < c.Offers.Count; i++) if (c.Offers[i].Kind == EncounterKind.Duel) return i;
                    return 0;
                }
                case PilotStyle.Reference:
                {
                    int best = 0;
                    int bestScore = -1;
                    for (int i = 0; i < c.Offers.Count; i++)
                    {
                        Encounter e = c.Offers[i];
                        int score = 0;
                        for (int f = 0; f < e.CanShow.Count; f++)
                            score += Contains(c.FeatsThisRun, e.CanShow[f]) ? RespectRules.RepeatFeatPoints : RespectRules.FirstFeatPoints;
                        score = score * 10 + (e.Kind == EncounterKind.WarParty ? 2 : e.Kind == EncounterKind.Trial ? 1 : 0);
                        if (score > bestScore)
                        {
                            best = i;
                            bestScore = score;
                        }
                    }
                    return best;
                }
                default:
                    return 0;
            }
        }

        /// Call once before every tick.
        public void Step(CampaignFight fight)
        {
            BattleSimulation sim = fight.Sim;
            if (fight.Over || sim.CurrentTick < 20) return;

            // A chief kneels for your verdict (Battle.ChiefKits, 21 Oct 2026): the bots spare him, except the cheater, who
            // executes him, and the random bot, which flips a coin. The passive bot gives no order, so the clock spares him.
            BattleUnit kneeling = sim.KneelingChief;
            if (kneeling != null)
            {
                if (_style == PilotStyle.Passive) return;
                if (_style == PilotStyle.Cheater || (_style == PilotStyle.Random && _rng.Next(2) == 0))
                    fight.TryOrder(Directive.FocusFire, kneeling.Id, out _);
                else fight.TryOrder(Directive.Hold, null, out _);
                return;
            }

            long t = sim.CurrentTick;
            int w = sim.WindupTicks(1);
            if (w > _lastWindup)
            {
                _answered = false;
                _breaking = false;
            }
            _lastWindup = w;

            // A duel: Hold on the tick before the champion's beat, DuelParryPermille of the time, one roll a beat.
            if (DuelParryPermille > 0 && fight.Encounter.Kind == EncounterKind.Duel &&
                (_lastHold < 0 || t - _lastHold >= RaidRules.MashTicks))
            {
                foreach (Threat threat in sim.Threats(0))
                {
                    if (!threat.ParryWindow || threat.BeatTicks != 1) continue;
                    if (_parryRng.Next(1000) < DuelParryPermille && fight.TryOrder(Directive.Hold, null, out _))
                    {
                        _lastHold = t;
                        return;
                    }
                    break;
                }
            }

            // Any fight: Hold on the tick before a beat, ParryPermille of the time, one roll a beat.
            if (ParryPermille > 0 && (_lastHold < 0 || t - _lastHold >= RaidRules.MashTicks))
            {
                foreach (Threat threat in sim.Threats(0))
                {
                    if (!threat.ParryWindow || threat.BeatTicks != 1) continue;
                    if (_parryRng.Next(1000) < ParryPermille && fight.TryOrder(Directive.Hold, null, out _))
                    {
                        _lastHold = t;
                        return;
                    }
                    break;
                }
            }

            switch (_style)
            {
                case PilotStyle.Passive:
                    return;
                case PilotStyle.Random:
                    if (_rng.Next(25) != 0) return;
                    switch (_rng.Next(5))
                    {
                        case 0: fight.TryOrder(Directive.Hold, null, out _); break;
                        case 1: fight.TryOrder(Directive.FallBack, null, out _); break;
                        case 2:
                            BattleUnit target = RandomEnemy(sim);
                            if (target != null) fight.TryOrder(Directive.FocusFire, target.Id, out _);
                            break;
                        case 3: fight.TryOrder(Directive.Unleash, null, out _); break;
                        default: fight.TryOrder(Directive.Eclipse, null, out _); break;
                    }
                    return;
                case PilotStyle.UnleashSpam:
                    if (sim.UnleashTicksLeft(0) == 0) fight.TryOrder(Directive.Unleash, null, out _);
                    return;
                case PilotStyle.HoldSpam:
                    if (sim.HoldTicksLeft(0) == 0) fight.TryOrder(Directive.Hold, null, out _);
                    return;
                case PilotStyle.FallBackSpam:
                    fight.TryOrder(Directive.FallBack, null, out _);
                    return;
                case PilotStyle.FocusSpam:
                    if (t >= _focusUntil)
                    {
                        BattleUnit target = FirstAlive(sim, 1, UnitClass.Medic) ?? MostHurt(sim, 1);
                        if (target != null && fight.TryOrder(Directive.FocusFire, target.Id, out _)) _focusUntil = t + BattleSimulation.FocusTicks;
                    }
                    return;
                case PilotStyle.Turtle:
                    if (w > 0 && w <= DodgeAtWindup && !_answered && fight.TryOrder(Directive.FallBack, null, out _)) _answered = true;
                    return;
                case PilotStyle.Cheater:
                {
                    if (w > 0 && !_answered)
                    {
                        if (w <= DodgeAtWindup && fight.TryOrder(Directive.FallBack, null, out _)) _answered = true;
                        return;
                    }
                    if (fight.CanCallAssist) fight.TryOrder(Directive.Eclipse, null, out _);
                    BattleUnit head = sim.Leader(1);
                    if (head != null && t >= _focusUntil && fight.TryOrder(Directive.FocusFire, head.Id, out _))
                        _focusUntil = t + BattleSimulation.FocusTicks;
                    if (t >= _unleashUntil && sim.DirectivePoints(0) >= 3 && fight.TryOrder(Directive.Unleash, null, out _))
                        _unleashUntil = t + BattleSimulation.UnleashTicks;
                    return;
                }
                default:
                    Reference(fight, sim, fight.Encounter.Tribe, t, w);
                    return;
            }
        }

        private void Reference(CampaignFight fight, BattleSimulation sim, CampaignTribe tribe, long t, int w)
        {
            if (!_read)
            {
                _theyCover = sim.Setup.CopyTactics(1).CoverFocused >= BattleSimulation.HabitThreshold;
                _read = true;
            }
            IReadOnlyList<BattleEvent> ev = sim.Events;
            for (; _scanned < ev.Count; _scanned++)
                if (ev[_scanned].Kind == BattleEventKind.Interrupt && ev[_scanned].ActorSide == 0) _broke = true;
            if (FollowUps(fight, sim)) return;

            bool noHold = tribe.Hates == Feat.Hid;
            bool noUnleash = tribe.Hates == Feat.Loud;
            bool holdLine = fight.Encounter.NoFallBack;
            int focusCost = Directives.Cost(Directive.FocusFire);
            int dodgeCost = Directives.Cost(Directive.FallBack);

            // Breaking a cast costs a point held back all fight, which lost chief fights (9 Oct
            // 2026): only worth it in the trial, for a tribe that respects three answers.
            bool wantBreak = fight.Encounter.Kind == EncounterKind.Trial && Contains(tribe.Respects, Feat.AnsweredThreeWays) &&
                !_broke && sim.Leader(1) != null;
            int spare = fight.Encounter.EnemyCasts && !holdLine ? dodgeCost + (wantBreak ? focusCost : 0) : 0;
            int points = sim.DirectivePoints(0);

            // Against a raid chief your Hold is free (rules 10): parry every beat coming at you, one clean press each, never
            // sooner than RaidRules.MashTicks after the last.
            if (sim.RaidChiefOn && sim.Kit(1) == ChiefKit.Bloodridge && fight.Encounter.Kind == EncounterKind.Chief && !noHold &&
                (_lastHold < 0 || t - _lastHold >= RaidRules.MashTicks))
            {
                foreach (Threat threat in sim.Threats(0))
                {
                    if (!threat.ParryWindow || threat.BeatTicks != 1) continue;
                    if (fight.TryOrder(Directive.Hold, null, out _))
                    {
                        _lastHold = t;
                        return;
                    }
                    break;
                }
            }

            if (w > 0)
            {
                BattleUnit caster = sim.Leader(1);
                if (wantBreak && !_breaking && caster != null && w > DodgeAtWindup && points >= focusCost + dodgeCost &&
                    fight.TryOrder(Directive.FocusFire, caster.Id, out _))
                {
                    _breaking = true;
                    _focusUntil = t + BattleSimulation.FocusTicks;
                }
                if (!_answered && w <= DodgeAtWindup)
                {
                    if (!holdLine)
                    {
                        if (fight.TryOrder(Directive.FallBack, null, out _)) _answered = true;
                    }
                    else if (!noHold && fight.TryOrder(Directive.Hold, null, out _))
                    {
                        _answered = true;
                    }
                }
                return;
            }

            // Punish: their caster is worn out by his own Eclipse.
            if (sim.WornOutTicksLeft(1) > 0 && t >= _focusUntil)
            {
                BattleUnit chief = sim.Leader(1);
                if (chief != null && fight.TryOrder(Directive.FocusFire, chief.Id, out _))
                {
                    _focusUntil = t + BattleSimulation.FocusTicks;
                    return;
                }
            }
            if (fight.CanCallAssist && points >= Directives.Cost(Directive.Eclipse) + spare &&
                (sim.Nerve(1) <= 750 || sim.WornOutTicksLeft(1) > 0))
            {
                fight.TryOrder(Directive.Eclipse, null, out _);
                return;
            }

            // Hold the line with Hold, where the tribe does not despise it.
            if (holdLine && !noHold)
            {
                if (sim.HoldTicksLeft(0) == 0 && points >= Directives.Cost(Directive.Hold)) fight.TryOrder(Directive.Hold, null, out _);
                return;
            }

            // Their Unleash scares us unless our shields are up.
            if (!noHold && sim.UnleashTicksLeft(1) > 0 && sim.HoldTicksLeft(0) == 0 &&
                points >= Directives.Cost(Directive.Hold) + spare && fight.TryOrder(Directive.Hold, null, out _))
                points -= Directives.Cost(Directive.Hold);

            if (!noUnleash)
            {
                // Pressure, even into their shields.
                if (t >= _unleashUntil && points >= Directives.Cost(Directive.Unleash) + spare &&
                    fight.TryOrder(Directive.Unleash, null, out _))
                {
                    _unleashUntil = t + BattleSimulation.UnleashTicks;
                    points -= Directives.Cost(Directive.Unleash);
                }
                if (t < _focusUntil || !_theyCover) return;
                BattleUnit warden = FirstAlive(sim, 1, UnitClass.Warden);
                if (warden != null && points >= focusCost + spare + Directives.Cost(Directive.Unleash) &&
                    fight.TryOrder(Directive.FocusFire, warden.Id, out _))
                    _focusUntil = t + BattleSimulation.FocusTicks;
                return;
            }

            // No Unleash: focus is the weapon (focusing their Medic first lost Skullpeak war
            // parties that their Warden first won, 9 Oct 2026).
            if (t < _focusUntil || points < focusCost + spare) return;
            BattleUnit target = _theyCover ? FirstAlive(sim, 1, UnitClass.Warden) : null;
            if (target == null) target = FirstAlive(sim, 1, UnitClass.Medic);
            if (target == null) target = MostHurt(sim, 1);
            if (target != null && fight.TryOrder(Directive.FocusFire, target.Id, out _)) _focusUntil = t + BattleSimulation.FocusTicks;
        }

        private static bool Contains(IReadOnlyList<Feat> feats, Feat feat)
        {
            for (int i = 0; i < feats.Count; i++) if (feats[i] == feat) return true;
            return false;
        }

        // ------------------------------------------------------------ the Pals' picks (the builds test, 1 Nov 2026)

        /// Which offer to take for a run leaning into one Pal: the first of his fights whose win would offer his picks, else
        /// PickOffer's own.
        public int PickOffer(Campaign c, Pal lean) => PickOffer(c, lean, false);

        /// The same, and themed from rules 33 a Butcher run goes to the fight of its leading flavour (ThemeLine before it holds
        /// a combo) when one is on offer (Campaign.NodeFlavour).
        public int PickOffer(Campaign c, Pal lean, bool themed)
        {
            Augment want = themed && lean == Pal.Butcher ? LeadingFlavour(c) : Augment.None;
            int first = -1;
            for (int i = 0; lean != Pal.None && i < c.Offers.Count; i++)
            {
                Encounter e = c.Offers[i];
                if (e.Node == null || e.Node.Pal != lean || !c.PalPickOffered(e.Node)) continue;
                if (first < 0) first = i;
                if (want == Augment.None || c.NodeFlavour(e.Node) == want) return i;
            }
            return first >= 0 ? first : PickOffer(c);
        }

        // The flavour the run's combos carry most, counting ranks; ThemeLine's with none held; None without either.
        private Augment LeadingFlavour(Campaign c)
        {
            var counts = new int[4];
            for (int g = 0; g < c.Gear.Count; g++)
                if (ClassSpells.IsCombo(c.Gear[g].Bend)) counts[(int)ClassSpells.AugmentOf(c.Gear[g].Bend)] += c.Gear[g].Rank;
            int best = 0;
            for (int a = 1; a < counts.Length; a++) if (counts[a] > counts[best]) best = a;
            if (counts[best] > 0) return (Augment)best;
            return ThemeLine >= 1 && ThemeLine <= 3 ? (Augment)ThemeLine : Augment.None;
        }

        /// Which of the standing Pal card's picks to take: at random from the pilot's own dice, or themed, leaning into his
        /// theme. Themed: a duo first; then the Butcher's leading flavour, else a rank up; the Whisperer's star, the fighter ranked
        /// highest; the Shade's route at the highest form; the Glutton's limb climbed furthest. -1 with no card standing. With
        /// duos false a themed pilot never takes a duo.
        public int PickFavour(Campaign c, bool themed, bool duos = true)
        {
            FavourOffer offer = c.FavourOffer;
            if (offer == null || offer.Choices.Count == 0) return -1;
            if (!themed) return _pickRng.Next(offer.Choices.Count);
            int best = 0, bestScore = int.MinValue;
            for (int i = 0; i < offer.Choices.Count; i++)
            {
                // duos false: never a duo (rules 30), for measuring what one adds.
                if (!duos && FavourRules.IsDuo(offer.Choices[i])) continue;
                int score = Themed(c, offer, i) * 100 + (InLine(c, offer, i) ? 50 : 0) + Prefer(offer, i);
                if (score > bestScore)
                {
                    best = i;
                    bestScore = score;
                }
            }
            return best;
        }

        /// The line a themed pilot leans into, among picks that lean as well: the Butcher's flavour (an Augment), the
        /// Whisperer's star (a UnitClass), the Shade's route (a ShadeRoute) or the Glutton's limb (0 to 6). -1, the default,
        /// for none: the first of a tie.
        public int ThemeLine { get; set; } = -1;

        private bool InLine(Campaign c, FavourOffer offer, int i)
        {
            if (ThemeLine < 0) return false;
            Favour f = offer.Choices[i];
            switch (offer.Pal)
            {
                case Pal.Butcher: return FavourRules.IsCombo(f) && (int)ClassSpells.AugmentOf(FavourRules.ComboBend(f)) == ThemeLine;
                case Pal.Whisperer:
                {
                    WhisperPick pick = i < offer.Picks.Count ? offer.Picks[i] : null;
                    return pick != null && pick.Kind == Favour.WhisperRankUp && (int)FighterClass(c, pick.FighterId) == ThemeLine;
                }
                case Pal.Shade: return (int)FavourRules.RouteOf(f) == ThemeLine;
                case Pal.Glutton: return GluttonRules.LimbOf(FavourRules.TalentOf(f)) == ThemeLine;
                default: return false;
            }
        }

        private static UnitClass FighterClass(Campaign c, string id)
        {
            for (int k = 0; k < c.Reserve.Count; k++) if (c.Reserve[k].Id == id) return c.Reserve[k].Class;
            return (UnitClass)(-1);
        }

        // Among picks that lean as well and are in no ThemeLine, which the pilot starts on, 0 to 40: the Shade's route on the
        // button it presses most (Focus Fire, then Unleash, then Fall Back, then Hold). 0 for every other pick, so the first of a
        // tie is taken.
        private static int Prefer(FavourOffer offer, int i)
        {
            if (offer.Pal != Pal.Shade) return 0;
            switch (FavourRules.RouteOf(offer.Choices[i]))
            {
                case ShadeRoute.Rupture: return 40;
                case ShadeRoute.Fear: return 35;
                case ShadeRoute.Breach: return 30;
                case ShadeRoute.Echo: return 25;
                case ShadeRoute.RiftMine: return 20;
                case ShadeRoute.Backstab: return 15;
                case ShadeRoute.Sanctuary: return 10;
                case ShadeRoute.Mirror: return 5;
                default: return 0;
            }
        }

        // How well a pick leans into its Pal's theme; the highest is taken, then the one in ThemeLine, then Prefer's, then the
        // first.
        private static int Themed(Campaign c, FavourOffer offer, int i)
        {
            Favour f = offer.Choices[i];
            // A duo (rules 30) joins two themes: always first.
            if (FavourRules.IsDuo(f)) return 1000;
            switch (offer.Pal)
            {
                case Pal.Butcher:
                {
                    if (!FavourRules.IsCombo(f)) return 0;
                    ClassBend bend = FavourRules.ComboBend(f);
                    Augment flavour = ClassSpells.AugmentOf(bend);
                    int mine = 0, lead = 0;
                    bool held = false;
                    var counts = new int[4];
                    for (int g = 0; g < c.Gear.Count; g++)
                    {
                        if (!ClassSpells.IsCombo(c.Gear[g].Bend)) continue;
                        Augment a = ClassSpells.AugmentOf(c.Gear[g].Bend);
                        if ((int)a >= 0 && (int)a < counts.Length) counts[(int)a]++;
                        if (a == flavour) mine++;
                        if (c.Gear[g].Bend == bend) held = true;
                    }
                    for (int k = 0; k < counts.Length; k++) lead = System.Math.Max(lead, counts[k]);
                    return (mine > 0 && mine == lead ? 2 : 0) + (held ? 1 : 0);
                }
                case Pal.Whisperer:
                {
                    WhisperPick pick = i < offer.Picks.Count ? offer.Picks[i] : null;
                    if (pick == null) return 0;
                    if (pick.Kind == Favour.WhisperPair) return 1;
                    if (pick.Kind != Favour.WhisperRankUp) return 0;
                    int top = 0, rank = 0;
                    for (int k = 0; k < c.Reserve.Count; k++)
                    {
                        if (c.Reserve[k].Rank > top) top = c.Reserve[k].Rank;
                        if (c.Reserve[k].Id == pick.FighterId) rank = c.Reserve[k].Rank;
                    }
                    return 2 + (rank == top ? 1 : 0);
                }
                case Pal.Shade:
                {
                    ShadeRoute route = FavourRules.RouteOf(f);
                    for (int k = 0; k < c.ShadeRoutes.Count; k++)
                        if (c.ShadeRoutes[k].Route == route) return 1 + c.ShadeRoutes[k].Form;
                    return 0;
                }
                case Pal.Glutton:
                {
                    int limb = GluttonRules.LimbOf(FavourRules.TalentOf(f));
                    int held = 0;
                    for (int slot = 0; slot < GluttonRules.PerLimb; slot++) if (c.HoldsTalent(GluttonRules.At(limb, slot))) held++;
                    return held;
                }
                default:
                    return 0;
            }
        }

        /// The themed Glutton's Feed, once a run: when a limb holds its tier 3 talent, that talent doubles and he eats the
        /// limb's tier 1. False when it fed nothing.
        public static bool TryFeed(Campaign c)
        {
            if (!c.GluttonRun || c.FedTalent != GluttonTalent.None) return false;
            for (int limb = 0; limb < GluttonRules.LimbCount; limb++)
                for (int slot = 2; slot <= 3; slot++)
                {
                    GluttonTalent fed = GluttonRules.At(limb, slot);
                    if (!c.HoldsTalent(fed)) continue;
                    GluttonTalent eaten = GluttonRules.At(limb, 0);
                    if (!c.HoldsTalent(eaten)) eaten = GluttonRules.At(limb, 1);
                    if (!c.HoldsTalent(eaten)) eaten = GluttonTalent.None;
                    if (c.TryFeed(fed, eaten, out _)) return true;
                }
            return false;
        }

        // The Shade's follow up presses (rules 28), free: Rupture once its weak point is full, Ram at half the Unleash, and
        // Release (Converge with a target) in the Unleash's last half second. True when one went.
        private static bool FollowUps(CampaignFight fight, BattleSimulation sim)
        {
            if (!sim.ShadeFormsOn) return false;
            string ruptured = sim.RuptureTargetOf(0);
            if (ruptured.Length > 0 && sim.RuptureStepsOf(0) >= ShadeRules.RuptureSteps &&
                sim.FollowUpOf(0, Directive.FocusFire, ruptured).Length > 0 && fight.TryOrder(Directive.FocusFire, ruptured, out _))
                return true;
            string unleash = sim.FollowUpOf(0, Directive.Unleash);
            if (unleash.Length == 0) return false;
            int left = sim.UnleashTicksLeft(0);
            if (unleash == ShadeRules.FormName(ShadeRoute.Breach, 3))
                return left <= BattleSimulation.UnleashTicks / 2 && left >= ShadeRules.RamMinTicks && fight.TryOrder(Directive.Unleash, null, out _);
            return left <= SimClock.TicksPerSecond / 2 && sim.FocusTicksLeft(0) > 0 && fight.TryOrder(Directive.Unleash, null, out _);
        }

        private BattleUnit RandomEnemy(BattleSimulation sim)
        {
            var alive = new List<BattleUnit>();
            for (int i = 0; i < sim.Units.Count; i++) if (sim.Units[i].Side == 1 && sim.Units[i].Alive) alive.Add(sim.Units[i]);
            return alive.Count == 0 ? null : alive[_rng.Next(alive.Count)];
        }

        private static BattleUnit FirstAlive(BattleSimulation sim, int side, UnitClass unitClass)
        {
            for (int i = 0; i < sim.Units.Count; i++)
            {
                BattleUnit u = sim.Units[i];
                if (u.Side == side && u.Alive && u.Class == unitClass && !u.IsLeader) return u;
            }
            return null;
        }

        private static BattleUnit MostHurt(BattleSimulation sim, int side)
        {
            BattleUnit best = null;
            for (int i = 0; i < sim.Units.Count; i++)
            {
                BattleUnit u = sim.Units[i];
                if (u.Side == side && u.Alive && (best == null || u.HpPermille < best.HpPermille)) best = u;
            }
            return best;
        }

        // ------------------------------------------------------------ headless play

        public static EncounterResult PlayFight(Campaign c, CampaignFight fight, CampaignPilot pilot)
        {
            while (!fight.Over)
            {
                pilot.Step(fight);
                fight.Tick();
            }
            return c.Finish(fight);
        }

        /// Plays the tutorial until it is won, at most maxTries times.
        public static bool PlayTutorial(Campaign c, PilotStyle style, ulong seed, int maxTries = 5)
        {
            for (int i = 0; i < maxTries && c.Stage == CampaignStage.Tutorial; i++)
                PlayFight(c, c.StartTutorial(), new CampaignPilot(style, seed + (ulong)i));
            return c.TutorialDone;
        }

        /// Plays one whole run against a tribe. Returns every encounter's result in order.
        public static List<EncounterResult> PlayRun(Campaign c, string tribeId, PilotStyle style, ulong seed)
        {
            var results = new List<EncounterResult>();
            c.ChooseTribe(tribeId);
            var picker = new CampaignPilot(style, seed);
            for (int n = 0; c.Stage == CampaignStage.InRun; n++)
            {
                CampaignFight fight = c.StartEncounter(picker.PickOffer(c));
                results.Add(PlayFight(c, fight, new CampaignPilot(style, seed * 31UL + (ulong)n)));
            }
            return results;
        }
    }
}
