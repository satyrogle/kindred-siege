using System;
using System.Collections.Generic;
using KindredSiege.Sim.Battle;

namespace KindredSiege.Sim.Learning
{
    /// The Pals' stores (Jacob, 31 Oct 2026: "Go, sprint it"; DESIGN_LOCKS, "The floor under the three Pals"; campaign rules 23):
    /// each Pal's picks this run in its own place, each with its own save lines (CampaignSave): the Glutton's lent talents, the
    /// Shade's routes at their forms, the Whisperer's habit for each class and the squad's pair, and each fighter's rank and
    /// second slot (CampaignFighter.Rank and GearId2). They go when the run does. From rules 24 (Jacob, 31 Oct 2026: "Go, build
    /// her"; DESIGN_LOCKS, "Habits come with the game, the Whisperer upgrades them") the habits are set at camp and kept, and the
    /// Whisperer's road card fills her part for the run: a rank up, a pair or a habit upgrade.
    public sealed partial class Campaign
    {
        private readonly List<HeldTalent> _talents = new List<HeldTalent>();
        private readonly List<HeldRoute> _routes = new List<HeldRoute>();
        private readonly List<HeldHabit> _habits = new List<HeldHabit>();

        /// The talents the Glutton lent this run, in the order lent.
        public IReadOnlyList<HeldTalent> Talents => _talents;

        /// The Shade's routes this run, at their forms, in the order first taken.
        public IReadOnlyList<HeldRoute> ShadeRoutes => _routes;

        /// The Whisperer's habits, one a class at most, in the order set. From rules 24 they are set at camp and kept between runs,
        /// and an upgrade lasts the run.
        public IReadOnlyList<HeldHabit> Habits => _habits;

        /// The squad's pair this run, None for none.
        public SquadPair Pair { get; private set; }

        // Whether this run plays the floor: its rules version, under the tuning's cap.
        private bool FloorRun => Math.Min(RulesVersion, _tuning.RulesCap) >= FloorRules;

        /// Whether this campaign plays the Whisperer (rules 24): its rules version, under the tuning's cap.
        public bool WhispererRun => Math.Min(RulesVersion, _tuning.RulesCap) >= WhispererRules;

        /// Whether this campaign plays the Shade's forms and her road card (rules 28): its rules version, under the tuning's cap.
        public bool ShadeFormsRun => Math.Min(RulesVersion, _tuning.RulesCap) >= ShadeFormsRules;

        /// Whether this campaign plays the Glutton's tree (rules 29): its rules version, under the tuning's cap.
        public bool GluttonRun => Math.Min(RulesVersion, _tuning.RulesCap) >= GluttonTreeRules;

        /// The Glutton lends this talent for the run. False when the run holds it already, or plays rules below 23; from rules 29
        /// also when you own it at camp.
        public bool TryLendTalent(GluttonTalent talent)
        {
            if (!FloorRun || talent <= GluttonTalent.None || (int)talent > PalStoreRules.TalentCount || FindTalent(talent) != null)
                return false;
            if (GluttonRun && _owned.Contains(talent)) return false;
            _talents.Add(new HeldTalent(talent, false));
            return true;
        }

        /// Feed doubles one talent the run holds, once a run. From rules 29 he eats another of the same limb for it: the first
        /// you hold, by its place in the limb (TryFeed lets you pick which), or nothing when you hold no other there.
        public bool TryFeedTalent(GluttonTalent talent)
        {
            if (GluttonRun)
            {
                if (!GluttonRules.Valid(talent)) return false;
                int limb = GluttonRules.LimbOf(talent);
                for (int slot = 0; slot < GluttonRules.PerLimb; slot++)
                {
                    GluttonTalent other = GluttonRules.At(limb, slot);
                    if (other != talent && HoldsTalent(other)) return TryFeed(talent, other, out _);
                }
                return TryFeed(talent, GluttonTalent.None, out _);
            }
            HeldTalent held = FindTalent(talent);
            if (held == null) return false;
            for (int i = 0; i < _talents.Count; i++) if (_talents[i].Fed) return false;
            held.Fed = true;
            return true;
        }

        /// The Shade's pick: a route the run holds gets its next form, up to ShadeRules.MaxForm; a new route takes its button
        /// if the button is plain and the run enhances fewer than two.
        public bool TryTakeRoute(ShadeRoute route)
        {
            if (!FloorRun || route <= ShadeRoute.None || (int)route >= ShadeRules.RouteCount) return false;
            HeldRoute held = FindRoute(route);
            if (held != null)
            {
                if (held.Form >= ShadeRules.MaxForm) return false;
                held.Form++;
                return true;
            }
            if (_routes.Count >= ShadeRules.MaxButtons) return false;
            Directive button = ShadeRules.ButtonOf(route);
            for (int i = 0; i < _routes.Count; i++) if (ShadeRules.ButtonOf(_routes[i].Route) == button) return false;
            _routes.Add(new HeldRoute(route, 1));
            return true;
        }

        /// The Whisperer's habit for its class, in place of the class's old one (28 Oct flip). From rules 24 habits come with the
        /// game: you set them at camp, free to switch, never in a run, and Gatekeeper is out of the slice.
        public bool TrySetHabit(WhispererHabit habit)
        {
            if (!FloorRun || habit <= WhispererHabit.None || (int)habit > PalStoreRules.HabitCount) return false;
            if (WhispererRun && (Stage == CampaignStage.InRun || habit == WhispererHabit.Gatekeeper)) return false;
            UnitClass cls = PalStoreRules.ClassOf(habit);
            for (int i = 0; i < _habits.Count; i++)
                if (_habits[i].Class == cls)
                {
                    if (_habits[i].Habit == habit) return false;
                    _habits[i] = new HeldHabit(habit);
                    return true;
                }
            _habits.Add(new HeldHabit(habit));
            return true;
        }

        /// The squad's pair, in place of the old one.
        public bool TrySetPair(SquadPair pair)
        {
            if (!FloorRun || pair <= SquadPair.None || (int)pair > PalStoreRules.PairCount || Pair == pair) return false;
            Pair = pair;
            return true;
        }

        /// A rank up to rank 2 with one of his class's vows (rules 33), as her card's pick gives it.
        public bool TryRankUp(string fighterId, StarVow vow)
        {
            CampaignFighter f = FindFighter(fighterId);
            if (!VowsRun || f == null || f.Rank != 1 || !VowRules.Valid(vow) || VowRules.ClassOf(vow) != f.Class || !TryRankUp(fighterId)) return false;
            f.Vow = vow;
            return true;
        }

        /// A rank up for one fighter, up to PalStoreRules.MaxRank.
        public bool TryRankUp(string fighterId)
        {
            CampaignFighter f = FindFighter(fighterId);
            if (!FloorRun || f == null || f.Rank >= PalStoreRules.MaxRank) return false;
            f.Rank++;
            return true;
        }

        /// A rank 3 fighter's second slot: a piece of the run's gear his class can wear, worn by nobody, his first slot
        /// included. Not in the middle of a fight.
        public bool TryEquipSecond(string fighterId, string gearId, out string why)
        {
            why = string.Empty;
            CampaignFighter f = FindFighter(fighterId);
            CampaignGear piece = FindGear(gearId);
            if (!FloorRun) why = "The second slot comes with campaign rules " + FloorRules + ".";
            else if (_active != null) why = "Not in the middle of a fight.";
            else if (f == null) why = (fighterId ?? string.Empty) + " is not one of your fighters.";
            else if (f.Rank < PalStoreRules.SecondSlotRank) why = "Only a rank " + PalStoreRules.SecondSlotRank + " fighter has a second slot.";
            else if (piece == null) why = "That piece is not in the run's gear.";
            else if (!piece.Fits(f.Class)) why = "Only a " + SliceClasses.Name((UnitClass)piece.ClassIndex) + " can wear that.";
            else
                for (int i = 0; i < _reserve.Count; i++)
                    if (_reserve[i].GearId == piece.Id || _reserve[i].GearId2 == piece.Id) why = "Someone already wears that piece.";
            if (why.Length > 0) return false;
            f.GearId2 = piece.Id;
            return true;
        }

        /// Takes the piece off this fighter's second slot, back to the run's gear, as TryUnequip does the first. Not in the
        /// middle of a fight. A fighter with nothing there stays as he is.
        public bool TryUnequipSecond(string fighterId, out string why)
        {
            why = string.Empty;
            CampaignFighter f = FindFighter(fighterId);
            if (_active != null) why = "Not in the middle of a fight.";
            else if (f == null) why = (fighterId ?? string.Empty) + " is not one of your fighters.";
            if (why.Length > 0) return false;
            f.GearId2 = string.Empty;
            return true;
        }

        // What your side brings into a fight from the stores: the Shade's routes at their forms, and from rules 24 the habits,
        // upgraded or not, and the pair; from rules 29 the Glutton's talents, owned or lent, fed or not, less the one he ate. No
        // talent sets the cheaper hook.
        private PalKit RunPalKit(int rules)
        {
            var routes = new List<KeyValuePair<ShadeRoute, int>>();
            for (int i = 0; i < _routes.Count; i++) routes.Add(new KeyValuePair<ShadeRoute, int>(_routes[i].Route, _routes[i].Form));
            var kit = new PalKit(routes, 0);
            if (rules < WhispererRules) return kit;
            var habits = new int[PalStoreRules.HabitCount + 1];
            for (int i = 0; i < _habits.Count; i++) habits[(int)_habits[i].Habit] = _habits[i].Upgraded ? 2 : 1;
            kit = kit.WithWhisper(habits, Pair);
            if (rules < GluttonTreeRules) return kit;
            var levels = new int[GluttonRules.TalentCount + 1];
            for (int t = 1; t <= GluttonRules.TalentCount; t++) levels[t] = TalentLevel((GluttonTalent)t);
            kit = kit.WithTalents(levels);
            // From rules 30 the duos held (Campaign.Duos.cs).
            if (rules < PalDuoRules) return kit;
            kit = kit.WithDuos(_duos);
            // From rules 34 the duos' streak (Campaign.Duos.cs).
            if (rules >= DuoMovesRules && _duoLevel > 0) kit = kit.WithDuoLevel(_duoLevel);
            // From rules 33 each ranked fighter's vow.
            if (rules < VowFlavourRules) return kit;
            var vows = new List<KeyValuePair<string, StarVow>>();
            for (int i = 0; i < _reserve.Count; i++)
                if (_reserve[i].Rank >= 2 && _reserve[i].Vow != StarVow.None) vows.Add(new KeyValuePair<string, StarVow>(_reserve[i].Id, _reserve[i].Vow));
            return kit.WithVows(vows);
        }

        // ------------------------------------------------------------ the Glutton's tree (rules 29)

        private readonly List<GluttonTalent> _owned = new List<GluttonTalent>();

        /// The talents bought at camp (rules 29), kept between runs, in the order bought.
        public IReadOnlyList<GluttonTalent> OwnedTalents => _owned;

        /// This run's Feed: the talent it doubles and the one he ate for it; None before it.
        public GluttonTalent FedTalent { get; private set; }
        public GluttonTalent EatenTalent { get; private set; }

        /// The lost fights Branch Hedge has kept the streak through this run.
        public int HedgesUsed { get; private set; }

        // The codex entries that pay a talent point, one each: a tribe met, a tribe won over, a chief met, a chief's hidden
        // chain, a beast slain. Repeat fights write nothing new, so they pay nothing.
        private static readonly string[] PointEntries = { "Tribe: ", "Joined: ", "Chief met: ", "Hidden chain of ", "Beast: " };

        /// The talent points the codex has paid: one for each first it records (PointEntries).
        public int TalentPointsEarned
        {
            get
            {
                int points = 0;
                for (int i = 0; i < _codex.Count; i++)
                    for (int k = 0; k < PointEntries.Length; k++)
                        if (_codex[i].StartsWith(PointEntries[k], StringComparison.Ordinal))
                        {
                            points++;
                            break;
                        }
                return points;
            }
        }

        /// What the owned talents cost, and what is left to spend.
        public int TalentPointsSpent
        {
            get
            {
                int spent = 0;
                for (int i = 0; i < _owned.Count; i++) spent += GluttonRules.Cost(_owned[i]);
                return spent;
            }
        }

        public int TalentPoints => TalentPointsEarned - TalentPointsSpent;

        /// The campaign tier you have reached: the highest tier of a tribe you have met, 1 before any.
        public int ReachedTier
        {
            get
            {
                int tier = 1;
                IReadOnlyList<CampaignTribe> all = CampaignTribes.All;
                for (int i = 0; i < all.Count; i++)
                    if (all[i].Tier > tier && _codex.Contains("Tribe: " + all[i].Name)) tier = all[i].Tier;
                return tier;
            }
        }

        /// Whether the run holds this talent (owned at camp, or lent for the run), and he has not eaten it.
        public bool HoldsTalent(GluttonTalent talent)
        {
            if (!GluttonRules.Valid(talent) || talent == EatenTalent) return false;
            return _owned.Contains(talent) || FindTalent(talent) != null;
        }

        /// This talent's level in your next fight: 0, GluttonRules.Held, or GluttonRules.Fed.
        public int TalentLevel(GluttonTalent talent)
        {
            if (!GluttonRun || !HoldsTalent(talent)) return 0;
            return talent == FedTalent ? GluttonRules.Fed : GluttonRules.Held;
        }

        /// Buys a talent in the camp tree with talent points, kept between runs. At camp only, in its limb's order (tier 1, tier
        /// 2, one of the tier 3 pair, then the keystone), and from the campaign tier that opens it.
        public bool TryBuyTalent(GluttonTalent talent, out string why)
        {
            why = string.Empty;
            if (!GluttonRun) why = "The tree comes with campaign rules " + GluttonTreeRules + ".";
            else if (!GluttonRules.Valid(talent)) why = "No such talent.";
            else if (Stage == CampaignStage.InRun) why = "Only at camp.";
            else if (_owned.Contains(talent)) why = "You own it already.";
            else if (!TreeOpen(talent, out why)) { }
            else if (ReachedTier < GluttonRules.OpensAtTier(talent)) why = "Opens at campaign tier " + GluttonRules.OpensAtTier(talent) + ".";
            else if (TalentPoints < GluttonRules.Cost(talent))
                why = "Needs " + GluttonRules.Cost(talent) + " points, you have " + TalentPoints + ".";
            if (why.Length > 0) return false;
            _owned.Add(talent);
            return true;
        }

        /// Gives every point back, at camp. Free.
        public bool TryResetTree()
        {
            if (!GluttonRun || Stage == CampaignStage.InRun || _owned.Count == 0) return false;
            _owned.Clear();
            return true;
        }

        // Whether the camp tree has its limb open below it: tier 2 needs tier 1, tier 3 needs tier 2 and not its partner, a
        // keystone needs one of the tier 3 pair.
        private bool TreeOpen(GluttonTalent talent, out string why)
        {
            why = string.Empty;
            int limb = GluttonRules.LimbOf(talent);
            switch (GluttonRules.SlotOf(talent))
            {
                case 0: return true;
                case 1:
                    if (!_owned.Contains(GluttonRules.At(limb, 0))) why = "Needs " + GluttonRules.Name(GluttonRules.At(limb, 0)) + " first.";
                    break;
                case 2:
                case 3:
                    if (!_owned.Contains(GluttonRules.At(limb, 1))) why = "Needs " + GluttonRules.Name(GluttonRules.At(limb, 1)) + " first.";
                    else if (_owned.Contains(GluttonRules.PartnerOf(talent)))
                        why = "You took " + GluttonRules.Name(GluttonRules.PartnerOf(talent)) + "; reset to switch.";
                    break;
                default:
                    if (!_owned.Contains(GluttonRules.At(limb, 2)) && !_owned.Contains(GluttonRules.At(limb, 3)))
                        why = "Needs a tier 3 talent of " + GluttonRules.Limbs[limb] + " first.";
                    break;
            }
            return why.Length == 0;
        }

        /// Feed (rules 29): once a run, this talent you hold doubles for the run, and he eats another you hold in the same limb
        /// for it. Eaten None only when you hold no other talent in that limb: then he eats nothing.
        public bool TryFeed(GluttonTalent fed, GluttonTalent eaten, out string why)
        {
            why = string.Empty;
            if (!GluttonRun) why = "Feed eats a talent from campaign rules " + GluttonTreeRules + ".";
            else if (Stage != CampaignStage.InRun) why = "Feed is for a run.";
            else if (_active != null) why = "Not in the middle of a fight.";
            else if (FedTalent != GluttonTalent.None) why = "He has fed once this run.";
            else if (!HoldsTalent(fed)) why = "You don't hold that talent.";
            else if (eaten == GluttonTalent.None && OtherInLimb(fed)) why = "He eats another talent of the same limb: pick which.";
            else if (eaten != GluttonTalent.None && !HoldsTalent(eaten)) why = "You don't hold the one he would eat.";
            else if (fed == eaten) why = "He eats another talent, not the one he feeds.";
            else if (eaten != GluttonTalent.None && GluttonRules.LimbOf(fed) != GluttonRules.LimbOf(eaten)) why = "He eats one of the same limb.";
            if (why.Length > 0) return false;
            FedTalent = fed;
            EatenTalent = eaten;
            HeldTalent lent = FindTalent(fed);
            if (lent != null) lent.Fed = true;
            return true;
        }

        // Whether the run holds a talent of this one's limb other than it.
        private bool OtherInLimb(GluttonTalent talent)
        {
            int limb = GluttonRules.LimbOf(talent);
            for (int slot = 0; slot < GluttonRules.PerLimb; slot++)
            {
                GluttonTalent other = GluttonRules.At(limb, slot);
                if (other != talent && HoldsTalent(other)) return true;
            }
            return false;
        }

        // ------------------------------------------------------------ the Glutton's road card (rules 29)

        // The road node's own dice for his picks: a stream of its seed nothing else draws from.
        private const ulong GluttonOfferStream = 18UL;

        /// What a pick weighs in his offer: the next talent of a limb the run has picked from weighs this, any other 1, so a
        /// run climbs one limb (the Butcher's ComboRankUpWeight). A starting value.
        public const int ClimbWeight = 2;

        /// How many picks a Pal card shows: FavourRules.Offered, more with Node Cache (rules 29).
        public int OfferSize => FavourRules.Offered + (GluttonRun ? GluttonRules.Bonus(TalentLevel(GluttonTalent.NodeCache), GluttonRules.CacheMore) : 0);

        // His card: the next talent of each limb you don't hold yet (tier 1, tier 2, both of the tier 3 pair, then the keystone),
        // weighing ClimbWeight in a limb he has lent from this run, drawn by weight from the node's own dice, in his order.
        private FavourOffer GluttonOffer(RoadNode node)
        {
            var pool = new List<Favour>();
            var weights = new List<int>();
            for (int limb = 0; limb < GluttonRules.LimbCount; limb++)
            {
                bool climbing = false;
                for (int i = 0; i < _talents.Count; i++) if (GluttonRules.LimbOf(_talents[i].Talent) == limb) climbing = true;
                List<GluttonTalent> next = NextInLimb(limb);
                for (int k = 0; k < next.Count; k++)
                {
                    pool.Add(FavourRules.TalentPick(next[k]));
                    weights.Add(climbing ? ClimbWeight : 1);
                }
            }
            var dice = new DeterministicRng(node.Seed, GluttonOfferStream);
            var taken = new bool[pool.Count];
            for (int n = 0; n < OfferSize && n < pool.Count; n++)
            {
                int total = 0;
                for (int i = 0; i < pool.Count; i++) if (!taken[i]) total += weights[i];
                int roll = dice.Next(total);
                for (int i = 0; i < pool.Count; i++)
                {
                    if (taken[i]) continue;
                    if (roll < weights[i])
                    {
                        taken[i] = true;
                        break;
                    }
                    roll -= weights[i];
                }
            }
            var choices = new List<Favour>();
            for (int i = 0; i < pool.Count; i++) if (taken[i]) choices.Add(pool[i]);
            return choices.Count == 0 ? null : new FavourOffer(Pal.Glutton, choices);
        }

        // A limb's next talents for the road: its lowest tier you hold nothing of; both of the tier 3 pair while you hold
        // neither; none once it is full.
        private List<GluttonTalent> NextInLimb(int limb)
        {
            var next = new List<GluttonTalent>();
            if (!Holds(GluttonRules.At(limb, 0))) next.Add(GluttonRules.At(limb, 0));
            else if (!Holds(GluttonRules.At(limb, 1))) next.Add(GluttonRules.At(limb, 1));
            else if (!Holds(GluttonRules.At(limb, 2)) && !Holds(GluttonRules.At(limb, 3)))
            {
                next.Add(GluttonRules.At(limb, 2));
                next.Add(GluttonRules.At(limb, 3));
            }
            else if (!Holds(GluttonRules.At(limb, 4))) next.Add(GluttonRules.At(limb, 4));
            return next;
        }

        // Held for the road's climb: owned or lent, eaten or not.
        private bool Holds(GluttonTalent talent) => _owned.Contains(talent) || FindTalent(talent) != null;

        // Takes one of his picks; its name for the codex. A tier 3 talent brings its limb's keystone with it.
        private string TakeGlutton(Favour favour)
        {
            GluttonTalent talent = FavourRules.TalentOf(favour);
            TryLendTalent(talent);
            string name = GluttonRules.Name(talent);
            GluttonTalent keystone = GluttonRules.KeystoneOf(talent);
            if (GluttonRules.TierOf(talent) == 3 && !Holds(keystone) && TryLendTalent(keystone)) name += " and " + GluttonRules.Name(keystone);
            return name;
        }

        // Branch Hedge (rules 29): a lost fight keeps the streak while the run has hedges left. True when it kept it.
        private bool Hedged()
        {
            if (!GluttonRun || HedgesUsed >= GluttonRules.Bonus(TalentLevel(GluttonTalent.BranchHedge), GluttonRules.HedgeLosses)) return false;
            HedgesUsed++;
            return true;
        }

        // Rogue Planet Prospecting (rules 29): what a won fight sends home, more.
        private int Prospected(int materials)
        {
            if (!GluttonRun) return materials;
            return materials * (100 + GluttonRules.Bonus(TalentLevel(GluttonTalent.RoguePlanetProspecting), GluttonRules.ProspectingPercent)) / 100;
        }

        // Each of yours in the fight brings his rank (rules 24).
        private void RankSpecs(List<UnitSpec> side)
        {
            for (int i = 0; i < side.Count; i++)
            {
                CampaignFighter f = FindFighter(side[i].Id);
                if (f != null && f.Rank > 1) side[i] = side[i].WithRank(f.Rank);
            }
        }

        // ------------------------------------------------------------ the Whisperer's road card (rules 24)

        // The road node's own dice for her three picks: a stream of its seed nothing else draws from.
        private const ulong WhisperOfferStream = 16UL;

        /// What a pick weighs in her offer: a rank up for a fighter already ranked weighs this, every other pick 1, so leaning
        /// on one star pays (the Butcher's ComboRankUpWeight). A starting value.
        public const int RankUpWeight = 2;

        // Her card: three of every rank up for one of your fighters below the top rank, every pair your squad can field that the
        // run does not hold, and an upgrade for every habit set and not yet upgraded whose class is in your squad, drawn by
        // weight from the node's own dice, in that order. Null when there is nothing, and the fight drops gear as usual.
        private FavourOffer WhisperOffer(RoadNode node)
        {
            var pool = new List<WhisperPick>();
            var weights = new List<int>();
            for (int i = 0; i < _reserve.Count; i++)
            {
                CampaignFighter f = _reserve[i];
                if (f.Rank >= PalStoreRules.MaxRank) continue;
                // From rules 33 a rank up to rank 2 comes once for each of his class's two vows (Campaign.Vows.cs).
                if (f.Rank == 1 && VowsRun)
                {
                    pool.Add(new WhisperPick(Favour.WhisperRankUp, f.Id, WhispererHabit.None, SquadPair.None, VowRules.FirstOf(f.Class)));
                    weights.Add(1);
                    pool.Add(new WhisperPick(Favour.WhisperRankUp, f.Id, WhispererHabit.None, SquadPair.None, VowRules.SecondOf(f.Class)));
                    weights.Add(1);
                    continue;
                }
                pool.Add(new WhisperPick(Favour.WhisperRankUp, f.Id, WhispererHabit.None, SquadPair.None));
                weights.Add(f.Rank > 1 ? RankUpWeight : 1);
            }
            for (int p = 1; p <= PalStoreRules.PairCount; p++)
            {
                var pair = (SquadPair)p;
                if (pair == Pair || !SquadHas(WhisperRules.FirstOf(pair)) || !SquadHas(WhisperRules.AnswerOf(pair))) continue;
                pool.Add(new WhisperPick(Favour.WhisperPair, string.Empty, WhispererHabit.None, pair));
                weights.Add(1);
            }
            for (int i = 0; i < _habits.Count; i++)
            {
                if (_habits[i].Upgraded || !SquadHas(_habits[i].Class)) continue;
                pool.Add(new WhisperPick(Favour.WhisperHabitUp, string.Empty, _habits[i].Habit, SquadPair.None));
                weights.Add(1);
            }
            var dice = new DeterministicRng(node.Seed, WhisperOfferStream);
            var taken = new bool[pool.Count];
            for (int n = 0; n < OfferSize && n < pool.Count; n++)
            {
                int total = 0;
                for (int i = 0; i < pool.Count; i++) if (!taken[i]) total += weights[i];
                int roll = dice.Next(total);
                for (int i = 0; i < pool.Count; i++)
                {
                    if (taken[i]) continue;
                    if (roll < weights[i])
                    {
                        taken[i] = true;
                        break;
                    }
                    roll -= weights[i];
                }
            }
            var choices = new List<Favour>();
            var picks = new List<WhisperPick>();
            for (int i = 0; i < pool.Count; i++)
            {
                if (!taken[i]) continue;
                choices.Add(pool[i].Kind);
                picks.Add(pool[i]);
            }
            return choices.Count == 0 ? null : new FavourOffer(Pal.Whisperer, choices, picks);
        }

        // Takes one of her picks; its name for the codex.
        private string TakeWhisper(WhisperPick pick)
        {
            switch (pick.Kind)
            {
                case Favour.WhisperRankUp:
                {
                    TryRankUp(pick.FighterId);
                    // From rules 33 the vow it brings (Campaign.Vows.cs).
                    CampaignFighter f = FindFighter(pick.FighterId);
                    if (f != null && f.Rank == 2 && VowRules.Valid(pick.Vow) && VowRules.ClassOf(pick.Vow) == f.Class)
                    {
                        f.Vow = pick.Vow;
                        return "Rank up: " + VowRules.Name(pick.Vow);
                    }
                    return "Rank up";
                }
                case Favour.WhisperPair:
                    TrySetPair(pick.Pair);
                    return FavourRules.PairName(pick.Pair);
                default:
                    for (int i = 0; i < _habits.Count; i++) if (_habits[i].Habit == pick.Habit) _habits[i].Upgraded = true;
                    return FavourRules.HabitName(pick.Habit) + " upgraded";
            }
        }

        // A lost fight from rules 24: each of yours who fell loses his top rank, and a rank 3 fighter dropping to 2 takes his
        // second piece off (it stays in the run's gear). The ones who lost a rank.
        private List<CampaignFighter> DropRanks(IReadOnlyList<CampaignFighter> fallen)
        {
            var lost = new List<CampaignFighter>();
            for (int i = 0; i < fallen.Count; i++)
            {
                CampaignFighter f = fallen[i];
                if (f.Rank <= 1) continue;
                f.Rank--;
                if (f.Rank < PalStoreRules.SecondSlotRank) f.GearId2 = string.Empty;
                // His vow goes with rank 2 (rules 33).
                if (f.Rank < 2) f.Vow = StarVow.None;
                lost.Add(f);
            }
            return lost;
        }

        // A captured fighter from rules 24 loses his rank, and the winners take his second piece with the first.
        private void Captured(CampaignFighter f)
        {
            CampaignGear second = FindGear(f.GearId2);
            if (second != null) _gear.Remove(second);
            f.GearId2 = string.Empty;
            f.Rank = 1;
            f.Vow = StarVow.None;
        }

        // ------------------------------------------------------------ the Shade's road card (rules 28)

        // The road node's own dice for her three picks: a stream of its seed nothing else draws from.
        private const ulong ShadeOfferStream = 17UL;

        /// What a pick weighs in her offer: the next form of a route the run holds weighs this, a new route 1, so leaning on her
        /// two buttons pays (the Butcher's ComboRankUpWeight). A starting value.
        public const int ShadeFormWeight = 2;

        // Her card: three of the next form of every built route the run holds below its last form, and every built route whose
        // button is plain while the run enhances fewer than two, drawn by weight from the node's own dice, in ShadeRoute's order.
        // Null when there is nothing, and the fight drops gear as usual.
        private FavourOffer ShadeOffer(RoadNode node)
        {
            var pool = new List<Favour>();
            var weights = new List<int>();
            for (int r = (int)ShadeRoute.Mirror; r < ShadeRules.RouteCount; r++)
            {
                var route = (ShadeRoute)r;
                if (!ShadeRules.Built(route)) continue;
                HeldRoute held = FindRoute(route);
                if (held != null)
                {
                    if (held.Form >= ShadeRules.MaxForm) continue;
                    pool.Add(FavourRules.ShadePick(route));
                    weights.Add(ShadeFormWeight);
                    continue;
                }
                if (_routes.Count >= ShadeRules.MaxButtons || ButtonTaken(ShadeRules.ButtonOf(route))) continue;
                pool.Add(FavourRules.ShadePick(route));
                weights.Add(1);
            }
            var dice = new DeterministicRng(node.Seed, ShadeOfferStream);
            var taken = new bool[pool.Count];
            for (int n = 0; n < OfferSize && n < pool.Count; n++)
            {
                int total = 0;
                for (int i = 0; i < pool.Count; i++) if (!taken[i]) total += weights[i];
                int roll = dice.Next(total);
                for (int i = 0; i < pool.Count; i++)
                {
                    if (taken[i]) continue;
                    if (roll < weights[i])
                    {
                        taken[i] = true;
                        break;
                    }
                    roll -= weights[i];
                }
            }
            var choices = new List<Favour>();
            for (int i = 0; i < pool.Count; i++) if (taken[i]) choices.Add(pool[i]);
            return choices.Count == 0 ? null : new FavourOffer(Pal.Shade, choices);
        }

        // Takes one of her picks; its name for the codex: the form it gave.
        private string TakeShade(Favour favour)
        {
            ShadeRoute route = FavourRules.RouteOf(favour);
            TryTakeRoute(route);
            HeldRoute held = FindRoute(route);
            return ShadeRules.FormName(route, held == null ? 1 : held.Form);
        }

        // Whether one of the run's routes sits on this button.
        private bool ButtonTaken(Directive button)
        {
            for (int i = 0; i < _routes.Count; i++) if (ShadeRules.ButtonOf(_routes[i].Route) == button) return true;
            return false;
        }

        private HeldTalent FindTalent(GluttonTalent talent)
        {
            for (int i = 0; i < _talents.Count; i++) if (_talents[i].Talent == talent) return _talents[i];
            return null;
        }

        private HeldRoute FindRoute(ShadeRoute route)
        {
            for (int i = 0; i < _routes.Count; i++) if (_routes[i].Route == route) return _routes[i];
            return null;
        }

        // The stores go when the run does (ClearGearAndGrudges). From rules 24 the habits stay, set at camp, and only their
        // upgrades go.
        private void ClearPalStores()
        {
            ClearDuos();
            _talents.Clear();
            FedTalent = GluttonTalent.None;
            EatenTalent = GluttonTalent.None;
            HedgesUsed = 0;
            _routes.Clear();
            if (WhispererRun)
                for (int i = 0; i < _habits.Count; i++) _habits[i].Upgraded = false;
            else _habits.Clear();
            Pair = SquadPair.None;
            for (int i = 0; i < _reserve.Count; i++)
            {
                _reserve[i].Rank = 1;
                _reserve[i].GearId2 = string.Empty;
                _reserve[i].Vow = StarVow.None;
            }
        }

        // ------------------------------------------------------------ for CampaignSave

        internal bool RestoreTalent(GluttonTalent talent, bool fed)
        {
            if (talent <= GluttonTalent.None || (int)talent > PalStoreRules.TalentCount || FindTalent(talent) != null) return false;
            if (fed) for (int i = 0; i < _talents.Count; i++) if (_talents[i].Fed) return false;
            _talents.Add(new HeldTalent(talent, fed));
            return true;
        }

        // A talent bought at camp (version 29): in the order bought, so its limb is open below it.
        internal bool RestoreOwned(GluttonTalent talent)
        {
            if (!GluttonRules.Valid(talent) || _owned.Contains(talent) || !TreeOpen(talent, out _)) return false;
            _owned.Add(talent);
            return true;
        }

        // The run's Feed (version 29): both held, of one limb.
        internal bool RestoreFeed(GluttonTalent fed, GluttonTalent eaten)
        {
            if (FedTalent != GluttonTalent.None || !GluttonRules.Valid(fed) || fed == eaten || !Holds(fed)) return false;
            if (eaten != GluttonTalent.None && (!GluttonRules.Valid(eaten) || GluttonRules.LimbOf(fed) != GluttonRules.LimbOf(eaten) || !Holds(eaten)))
                return false;
            FedTalent = fed;
            EatenTalent = eaten;
            return true;
        }

        internal bool RestoreHedges(int used)
        {
            if (used < 1 || used > GluttonRules.HedgeLosses * 2 || HedgesUsed != 0) return false;
            HedgesUsed = used;
            return true;
        }

        internal bool RestoreRoute(ShadeRoute route, int form)
        {
            if (route <= ShadeRoute.None || (int)route >= ShadeRules.RouteCount || form < 1 || form > ShadeRules.MaxForm ||
                FindRoute(route) != null || _routes.Count >= ShadeRules.MaxButtons) return false;
            for (int i = 0; i < _routes.Count; i++) if (ShadeRules.ButtonOf(_routes[i].Route) == ShadeRules.ButtonOf(route)) return false;
            _routes.Add(new HeldRoute(route, form));
            return true;
        }

        internal bool RestoreHabit(WhispererHabit habit)
        {
            if (habit <= WhispererHabit.None || (int)habit > PalStoreRules.HabitCount) return false;
            for (int i = 0; i < _habits.Count; i++) if (_habits[i].Class == PalStoreRules.ClassOf(habit)) return false;
            _habits.Add(new HeldHabit(habit));
            return true;
        }

        // A habit the run upgraded (version 27): it must be set.
        internal bool RestoreHabitUp(WhispererHabit habit)
        {
            for (int i = 0; i < _habits.Count; i++)
                if (_habits[i].Habit == habit && !_habits[i].Upgraded)
                {
                    _habits[i].Upgraded = true;
                    return true;
                }
            return false;
        }

        // One of her picks on offer (version 27), in its place: it must match the offer's choice there.
        internal bool RestoreWhisperPick(int index, Favour kind, string fighterId, WhispererHabit habit, SquadPair pair, StarVow vow = StarVow.None)
        {
            FavourOffer offer = _favourOffer;
            if (offer == null || offer.Pal != Pal.Whisperer || index != offer.Picks.Count || index >= offer.Choices.Count ||
                offer.Choices[index] != kind) return false;
            if (kind == Favour.WhisperRankUp && FindFighter(fighterId) == null) return false;
            if (kind == Favour.WhisperPair && (pair <= SquadPair.None || (int)pair > PalStoreRules.PairCount)) return false;
            if (kind == Favour.WhisperHabitUp && (habit <= WhispererHabit.None || (int)habit > PalStoreRules.HabitCount)) return false;
            if (vow != StarVow.None && (kind != Favour.WhisperRankUp || !VowRules.Valid(vow))) return false;
            offer.AddPick(new WhisperPick(kind, fighterId ?? string.Empty, habit, pair, vow));
            return true;
        }

        internal bool RestorePair(SquadPair pair)
        {
            if (pair <= SquadPair.None || (int)pair > PalStoreRules.PairCount || Pair != SquadPair.None) return false;
            Pair = pair;
            return true;
        }

        // For CampaignSave (version 31): a fighter's vow, after his rank.
        internal bool RestoreVow(string fighterId, StarVow vow)
        {
            CampaignFighter f = FindFighter(fighterId);
            if (f == null || f.Rank < 2 || f.Vow != StarVow.None || !VowRules.Valid(vow) || VowRules.ClassOf(vow) != f.Class) return false;
            f.Vow = vow;
            return true;
        }

        internal bool RestoreRank(string fighterId, int rank)
        {
            CampaignFighter f = FindFighter(fighterId);
            if (f == null || f.Rank != 1 || rank < 2 || rank > PalStoreRules.MaxRank) return false;
            f.Rank = rank;
            return true;
        }

        internal bool RestoreSecond(string fighterId, string gearId)
        {
            CampaignFighter f = FindFighter(fighterId);
            CampaignGear piece = FindGear(gearId);
            if (f == null || piece == null || f.Rank < PalStoreRules.SecondSlotRank || !piece.Fits(f.Class) || f.GearId2.Length > 0) return false;
            for (int i = 0; i < _reserve.Count; i++) if (_reserve[i].GearId == piece.Id || _reserve[i].GearId2 == piece.Id) return false;
            f.GearId2 = piece.Id;
            return true;
        }
    }
}
