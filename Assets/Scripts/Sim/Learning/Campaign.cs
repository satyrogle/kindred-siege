using System;
using System.Collections.Generic;
using System.Globalization;
using KindredSiege.Sim.Battle;

namespace KindredSiege.Sim.Learning
{
    /// One of your fighters. It fights at the tier of the fight it is in (Jacob, 8 Oct 2026).
    public sealed class CampaignFighter
    {
        internal CampaignFighter(string id, string tribeId, UnitClass unitClass, string name, Quirk quirk)
        {
            Id = id;
            TribeId = tribeId;
            Class = unitClass;
            Name = name ?? string.Empty;
            Quirk = quirk;
            Row = SliceClasses.Stats(unitClass).DefaultRow;
            GearId = string.Empty;
        }

        public string Id { get; }
        public string TribeId { get; }
        public UnitClass Class { get; }

        /// Their own name (CampaignNames, Jacob's calls on 12 Oct 2026): "Nakht", or "Nakht the Second" when
        /// one of yours had it before. A recruit keeps the name they had as an enemy.
        public string Name { get; }

        /// Their one quirk (Jacob, 13 Oct 2026; Quirks), the same all campaign for this fighter.
        public Quirk Quirk { get; }

        /// The row they stand in, front or back (formation, Jacob, 13 Oct 2026): their class's usual row until
        /// you move them (Campaign.TrySetRow).
        public Row Row { get; internal set; }

        /// The run gear piece they wear (Campaign.Gear), or empty. One slot.
        public string GearId { get; internal set; }

        /// Their rank this run, 1 to PalStoreRules.MaxRank (campaign rules 23, the Whisperer's: DESIGN_LOCKS, "The Whisperer's
        /// core" and "The floor under the three Pals"). It goes when the run does.
        public int Rank { get; internal set; } = 1;

        /// Their vow this run, from rank 2 (campaign rules 33, the Whisperer's vows: Battle.VowRules). None below rank 2; it goes
        /// with the rank that brought it, and when the run does.
        public StarVow Vow { get; internal set; }

        /// The piece in their second slot, open at rank 3 (rules 23), or empty.
        public string GearId2 { get; internal set; } = string.Empty;

        /// The road scars they earned this run breaking a beast's parts, in the order earned (Jacob, 20 Oct 2026, DESIGN_LOCKS
        /// point 7; Battle.BeastKits.ScarName and ScarText). With any, their own spell works harder against a beast. They go
        /// with the run, as the fighter does.
        public IReadOnlyList<BeastPart> Scars => _scars;

        private readonly List<BeastPart> _scars = new List<BeastPart>();

        /// Gives them this part's scar. False when they already carry it.
        internal bool AddScar(BeastPart part)
        {
            if (_scars.Contains(part)) return false;
            _scars.Add(part);
            return true;
        }

        public string DisplayName
        {
            get
            {
                CampaignTribe tribe = CampaignTribes.Find(TribeId);
                return (tribe == null ? TribeId : tribe.Name) + " " + SliceClasses.Name(Class);
            }
        }
    }

    /// A chief who knelt. His one main order is yours for the rest of the run (Jacob, 10 Oct
    /// 2026): called in once a fight, paid with order points, and the enemy can break the call
    /// like any Eclipse (Jacob, 8 Oct 2026, answers 12c, 13a and 14a).
    public sealed class Assist
    {
        internal Assist(string tribeId, string name, string chiefName, int grade)
        {
            TribeId = tribeId;
            Name = name;
            ChiefName = chiefName;
            Grade = grade;
        }

        public string TribeId { get; }
        public string Name { get; }
        public string ChiefName { get; }
        public int Grade { get; }
    }

    /// One encounter on offer: what it is, against whom, how many of yours it takes and what
    /// waits for them. Everything here can go on the card.
    public sealed class Encounter
    {
        private static readonly Feat[] NoFeats = new Feat[0];

        internal Encounter(EncounterKind kind, CampaignTribe tribe, int squadSize, int enemyFighters, int enemyUnits,
            bool enemyCasts, bool noFallBack, RoadNode node = null)
        {
            Kind = kind;
            Tribe = tribe;
            SquadSize = squadSize;
            EnemyFighters = enemyFighters;
            EnemyUnits = enemyUnits;
            EnemyCasts = enemyCasts;
            NoFallBack = noFallBack;
            Node = node;
        }

        public EncounterKind Kind { get; }
        public CampaignTribe Tribe { get; }
        public int SquadSize { get; }

        /// The road node this fight stands on (Jacob, 13 Oct 2026, the path map), or null for the tutorial, a chief and
        /// the hunt after the last chief. An ambush stands on the road node the hunters took.
        public RoadNode Node { get; }

        /// Its bonus goal, or None (FightObjectives). Hunters waiting on a node bring no goal.
        public FightObjective Objective => Node == null || Kind == EncounterKind.Ambush ? FightObjective.None : Node.Objective;

        /// The named hunter leading an ambush or the hunt after the last chief (HunterCompany), or null.
        public CampaignHunter Hunter { get; internal set; }

        /// That hunter's rank on the hunters' ladder when this was offered, or 0.
        public int HunterRank { get; internal set; }

        /// Which of its tribe's squads this fight sends (CampaignTribe.Lineup(kind, squad)): 0 to 2 (Jacob, 18 Oct 2026:
        /// "three squads per fight shown on the node").
        public int Squad { get; internal set; }

        /// Their fighters' classes in the order they come, not counting a chief, shaman or captain; a duel's champion.
        /// What the road node shows (Campaign.SquadOn).
        public IReadOnlyList<UnitClass> EnemyClasses { get; internal set; } = new UnitClass[0];

        /// Their fighters, not counting a chief, shaman, champion or captain.
        public int EnemyFighters { get; }

        /// Everyone they bring.
        public int EnemyUnits { get; }

        /// Someone on their side winds up an Eclipse you can answer.
        public bool EnemyCasts { get; }

        /// Fall Back is refused in this fight.
        public bool NoFallBack { get; }

        /// A beast fight's beast (Battle.BeastKits): the sand shark or the cage bear. None for every other fight.
        public BeastKit Beast { get; internal set; }

        public string Title
        {
            get
            {
                switch (Kind)
                {
                    case EncounterKind.WarParty: return "War party";
                    case EncounterKind.HoldTheLine: return "Hold the line";
                    case EncounterKind.Duel: return "Champion duel";
                    case EncounterKind.Trial: return Tribe.TrialName;
                    case EncounterKind.Chief: return Tribe.ChiefName + " calls you out";
                    case EncounterKind.Tutorial: return "The raid on your jailers";
                    case EncounterKind.AlphaHunt: return Hunter == null ? "The hunters" : Hunter.Name + " and the hunters";
                    case EncounterKind.Ambush: return Hunter == null ? "Ambush" : Hunter.Name + " hunts you";
                    case EncounterKind.Beast: return "The " + BeastKits.Name(Beast).ToLowerInvariant();
                    default: return "Fight";
                }
            }
        }

        public string Text
        {
            get
            {
                switch (Kind)
                {
                    case EncounterKind.WarParty: return "A band of " + Tribe.Name + " fighters. Break them before time runs out.";
                    case EncounterKind.HoldTheLine: return "More of them than you. Do not break until time runs out. No falling back.";
                    case EncounterKind.Duel:
                        return "One of yours against their champion" + (EnemyCasts ? ", who winds up a big swing you can see coming" : string.Empty) +
                            ". Your leader only gives the orders.";
                    case EncounterKind.Trial: return Tribe.TrialText;
                    case EncounterKind.Chief:
                        return "Answer his moves to break his nerve. When he kneels, Hold spares him and his whole tribe joins you for the rest " +
                            "of the run, with his order; Focus Fire executes him and you take his Princess gear. Kill him before he kneels " +
                            "and you only take the gear.";
                    case EncounterKind.Tutorial: return "Bloodridge came to take you. Command the Clarth and drive them off.";
                    case EncounterKind.AlphaHunt:
                        return "The hunters came for the bounty on your head. " + LeaderLine() + " leads " +
                            EnemyFighters.ToString(CultureInfo.InvariantCulture) +
                            " of the company and winds up a big move you can see coming. One more came for every tribe that joined you this run. " +
                            "Break their nerve and the run is won. They never join.";
                    case EncounterKind.Ambush:
                        return LeaderLine() + " waits on this road with " + EnemyFighters.ToString(CultureInfo.InvariantCulture) +
                            " of the company. Beat them to climb the hunters' ladder. They never join.";
                    case EncounterKind.Beast:
                        if (Beast == BeastKit.CageBear)
                            return "Skullpeak's steel grew through it. Its claw grabs one of yours and crushes them until you break it, " +
                                "its maw roars at your nerve, and its heart mends the rest while its ribcage stands. The ribcage halves " +
                                "every hit and hides the heart; Unleash cracks it faster. Break the heart and it drops dead, but only the " +
                                "parts you broke are trophies for the Butcher. It never runs: if time runs out, you lose.";
                        return "It hunts beside the road. Its jaw, fin, spine and tail each fight on their own; break one and its move is " +
                            "gone. It has no nerve to break. Break all four and it dies; hold out and it dives away. Win, and every part " +
                            "you broke is a trophy for the Butcher.";
                    default: return string.Empty;
                }
            }
        }

        /// Which of the tribe's code this encounter can show. Since 10 Oct 2026 nothing scores the
        /// code (Jacob dropped the respect bar), so no card shows any: always empty.
        public IReadOnlyList<Feat> CanShow => NoFeats;

        // "Vessk, Twice Paid, rank 7 on the hunters' ladder," for the card.
        private string LeaderLine()
        {
            return Hunter == null ? "Their captain" : Hunter.FullName + ", rank " + HunterRank.ToString(CultureInfo.InvariantCulture) + ",";
        }
    }

    /// A live fight in the campaign. Orders go through here so the campaign's rules hold: the
    /// assist comes once a fight, and hold the line refuses Fall Back.
    public sealed class CampaignFight
    {
        private readonly Dictionary<string, string> _names;

        internal CampaignFight(Encounter encounter, BattleSetup setup, IReadOnlyList<CampaignFighter> squad, Assist assist,
            Dictionary<string, string> names)
        {
            Encounter = encounter;
            Setup = setup;
            Squad = squad;
            Assist = assist;
            Sim = new BattleSimulation(setup);
            Session = new SimSession(Sim);
            _names = names ?? new Dictionary<string, string>();
        }

        public Encounter Encounter { get; }
        public BattleSetup Setup { get; }
        public IReadOnlyList<CampaignFighter> Squad { get; }

        /// The pieces their fighters wear in this fight, each on the first of theirs of its class (Jacob, 20 Oct 2026:
        /// "strip one of three pieces they wore"). A win strips one of these. Empty in a fight whose win strips no gear.
        public IReadOnlyList<CampaignGear> Worn { get; internal set; } = new CampaignGear[0];

        /// The campaign rules this fight began under (Campaign.RulesVersion), so a save resumes it under the same.
        public int RulesVersion { get; internal set; } = Campaign.RulesVersion;

        /// A chief fight against a chief you fought before, in this run or an earlier one: his kneel line changes (Jacob,
        /// 20 Oct 2026, "Chief fights").
        public bool MetBefore { get; internal set; }

        /// What their chief says for a Line event's key (Battle.ChiefKits.Line), or empty when he has no kit.
        public string ChiefLine(string key)
        {
            return ChiefKits.Line(Sim.Kit(1), key, MetBefore);
        }

        public Assist Assist { get; }
        public BattleSimulation Sim { get; }
        public SimSession Session { get; }
        public bool AssistUsed { get; private set; }
        public int FallBacksOrdered { get; private set; }
        public bool Over => Sim.IsFinished || Sim.CurrentTick >= Setup.TickLimit;

        public bool CanCallAssist => Assist != null && !AssistUsed && Sim.CanUse(0, Directive.Eclipse, out _);

        /// A unit's own name in this fight (CampaignNames): your fighters', each of theirs, their chief's in a chief
        /// fight, and the named hunter leading an ambush or the hunt (HunterCompany). Empty for your leader and for the
        /// company's blades.
        public string NameOf(string unitId)
        {
            for (int i = 0; i < Squad.Count; i++) if (Squad[i].Id == unitId) return Squad[i].Name;
            return unitId != null && _names.TryGetValue(unitId, out string name) ? name : string.Empty;
        }

        public bool TryOrder(Directive directive, string targetId, out string failure)
        {
            return TryOrder(directive, targetId, true, out failure);
        }

        /// live false: the order was given while the fight was paused. It does everything it would, except that a
        /// Hold given while paused can never parry (Jacob, 18 Oct 2026: the parry is a live press).
        public bool TryOrder(Directive directive, string targetId, bool live, out string failure)
        {
            if (TryOrderOnce(directive, targetId, live, out failure)) return true;
            LastRejection = new OrderRejection(directive, Sim.CurrentTick, targetId, failure);
            return false;
        }

        /// Your last order that did not go, and why (the campaign's own reasons too: no Frenzy, a chief already called, no
        /// falling back on a held line), or OrderRejection.None. Not part of the fight.
        public OrderRejection LastRejection { get; private set; } = OrderRejection.None;

        private bool TryOrderOnce(Directive directive, string targetId, bool live, out string failure)
        {
            failure = string.Empty;
            if (directive == Directive.Frenzy)
            {
                failure = "Frenzy is gone: the Pals stay at camp.";
                return false;
            }
            if (directive == Directive.Eclipse)
            {
                if (Assist == null)
                {
                    failure = "No chief answers your call yet.";
                    return false;
                }
                if (AssistUsed)
                {
                    failure = Assist.Name + " already came this fight.";
                    return false;
                }
            }
            if (directive == Directive.FallBack && Encounter.NoFallBack)
            {
                failure = "You are holding this line. No falling back.";
                return false;
            }
            if (!Session.TryIssue(Directives.CommandKind, "0", targetId ?? string.Empty, live ? 0 : HeavyRules.PausedOrderArg, 0,
                    Directives.Name(directive), out failure))
                return false;
            if (directive == Directive.Eclipse) AssistUsed = true;
            if (directive == Directive.FallBack) FallBacksOrdered++;
            return true;
        }

        public void Tick()
        {
            if (!Over) Session.Tick();
        }
    }

    /// A respect award. Unused since 10 Oct 2026 (no respect bar, no taboos): results carry none.
    public sealed class FeatAward
    {
        internal FeatAward(string tribeId, Feat feat, int points)
        {
            TribeId = tribeId;
            Feat = feat;
            Points = points;
        }

        public string TribeId { get; }
        public Feat Feat { get; }
        public int Points { get; }
        public bool Taboo => Feats.IsTaboo(Feat);
    }

    /// What one finished encounter did.
    public sealed class EncounterResult
    {
        internal EncounterResult() { }

        public Encounter Encounter { get; internal set; }
        public FightRecord Record { get; internal set; }
        public bool Won { get; internal set; }

        /// The clock ran out. Lost, unless this was hold the line and you did not break.
        public bool TimedOut { get; internal set; }

        /// Different answers you gave their Eclipse, 0 to 4.
        public int EclipseWays { get; internal set; }

        /// Always empty since 10 Oct 2026: nothing scores respect or taboos.
        public IReadOnlyList<FeatAward> Awards { get; internal set; }

        /// Since 10 Oct 2026 the respect numbers carry the path instead: fights won on this tribe's
        /// path before and after this one, and how many come before its chief. Kept so screens
        /// that still say respect compile.
        public int RespectBefore { get; internal set; }
        public int RespectBeforeHalving { get; internal set; }
        public int RespectAfter { get; internal set; }
        public int RespectNeeded { get; internal set; }

        /// The path is walked: the chief is next.
        public bool ChiefReady { get; internal set; }

        /// Never set since 10 Oct 2026: a path has a set length, so nobody is sent away.
        public bool SentAway { get; internal set; }

        /// From rules 24 a lost fight: your fighters who fell in it, each one rank down (Campaign.PalStores.cs). Empty otherwise.
        public IReadOnlyList<CampaignFighter> RanksLost { get; internal set; } = new List<CampaignFighter>();

        /// The chief was captured and his tribe joined you for the rest of the run.
        public bool Joined { get; internal set; }
        public Assist AssistUnlocked { get; internal set; }
        public IReadOnlyList<CampaignFighter> JoinedFighters { get; internal set; }
        public IReadOnlyList<CampaignFighter> Fallen { get; internal set; }

        /// A lost fight: the one of yours the winners took, by name (Jacob, 13 Oct 2026, roster as lives). Null
        /// when the fight was won, or nobody of yours was left standing to take.
        public CampaignFighter Captured { get; internal set; }

        /// A lost fight against a tribe that takes no prisoners (CampaignTribe.NoPrisoners, 18 Oct 2026): the most hurt
        /// of yours still standing, whom they killed instead of capturing. Null otherwise.
        public CampaignFighter Slain { get; internal set; }

        /// A won chief fight where his nerve broke while he still stood: his whole band joined you, with his
        /// order (Jacob, 13 Oct 2026).
        public bool ChiefCaptured { get; internal set; }

        /// A won chief fight where he fell: no tribe and no order, but his Princess piece (PrincessTaken).
        public bool ChiefKilled { get; internal set; }
        public CampaignGear PrincessTaken { get; internal set; }

        /// A won chief fight where he knelt and Focus Fire executed him (Jacob, 20 Oct 2026, "Chief fights"): ChiefKilled
        /// too. False when he fell before he knelt, which also gives his piece, with no choice.
        public bool ChiefExecuted { get; internal set; }

        /// A won fight against a chief with his own kit (Battle.ChiefKits): its grade, the Glory rank you held when it ended,
        /// for the grade card. A better grade sent home more of his materials. Graded is false otherwise.
        public bool Graded { get; internal set; }
        public GloryRank Grade { get; internal set; }

        /// True when that grade beat your best against him, which his codex page keeps (Campaign.BestGrade).
        public bool NewBestGrade { get; internal set; }

        /// The hidden chain you found against him in this fight ("Sunbreaker"), or empty: his codex page fills in its blank.
        public string ChainFound { get; internal set; } = string.Empty;

        /// A beast fight (Battle.BeastKits, 21 Oct 2026): every part broken in it, in the order they broke, won or lost.
        public IReadOnlyList<BeastPart> PartsBroken { get; internal set; } = new BeastPart[0];

        /// A won beast fight: the trophies it sent to camp, one for each part broken (Campaign.Trophies). Empty otherwise.
        public IReadOnlyList<BeastPart> Trophies { get; internal set; } = new BeastPart[0];

        /// A beast fight: the road scars your fighters earned in it, won or lost (CampaignFighter.Scars).
        public IReadOnlyList<BeastScar> ScarsEarned { get; internal set; } = new BeastScar[0];

        /// A won beast fight where it died: every part of the shark broke, or the cage bear's heart did. A shark won without
        /// that dived away (BeastFled); a cage bear is only ever beaten this way.
        public bool BeastKilled { get; internal set; }

        /// A won shark fight where it dived away with a part still whole: when its Last stand ran out (BeastEscaped), or when
        /// the fight's clock did (TimedOut).
        public bool BeastFled { get; internal set; }
        public bool BeastEscaped { get; internal set; }

        /// A won path fight: the first piece of their gear you can take in place of its materials before the next
        /// fight starts (Campaign.TryTakeSpoils). Null otherwise.
        public CampaignGear SpoilsOffered { get; internal set; }

        /// A won path fight: all the pieces of their gear on offer, to strip one of (Jacob, 20 Oct 2026: three). Empty
        /// otherwise.
        public IReadOnlyList<CampaignGear> SpoilsChoices { get; internal set; } = new CampaignGear[0];

        /// A won favour fight: the Pal's favours you can pick one of in place of its materials before the next fight
        /// starts (Campaign.TryTakeFavour, 18 Oct 2026). Null otherwise; a favour fight offers no gear.
        public FavourOffer FavourOffered { get; internal set; }

        /// A road fight's bonus goal, and whether the win met it: met, its drop went up a tier (Jacob, 13 Oct 2026).
        public FightObjective Objective { get; internal set; }
        public bool ObjectiveMet { get; internal set; }

        /// Your fighters after this fight: your lives. Under Campaign.MinSquad the run is over.
        public int FightersLeft { get; internal set; }

        /// From rules 11: the lives your run has left after this fight (Campaign.Lives).
        public int LivesLeft { get; internal set; }

        /// From rules 12: the streak this won fight's materials were multiplied by (Campaign.Streak).
        public int Streak { get; internal set; }

        /// Materials this fight sent home for the Pals. They stay even when the run is lost.
        public int MaterialsEarned { get; internal set; }

        /// The Butcher learned a gear recipe from this chief (Jacob, 11 Oct 2026, call J1): the
        /// Bloodridge chief teaches the Lava Sun Tabard. True only the first time.
        public bool RecipeLearned { get; internal set; }

        /// After a chief kneels: where the run goes on, the next tier's tribe, or AlphaHunt.Tribe
        /// once the last chief is beaten (call J2). Null when the run is over or still on the same tribe.
        public CampaignTribe NextTribe { get; internal set; }

        /// The last chief broke and the hunters come next: one fight left in the run.
        public bool HuntComing => NextTribe == AlphaHunt.Tribe;

        /// The named hunter this fight was against (an ambush or the hunt), or null (HunterCompany, 18 Oct 2026).
        public CampaignHunter Hunter { get; internal set; }

        /// You won and that hunter died in the fight: they are off the ladder for good and a recruit took their place.
        public bool HunterSlain { get; internal set; }

        /// You won and that hunter walked away: they keep their name and come back another day.
        public bool HunterFled { get; internal set; }

        /// You lost, and it was the first time this run that this hunter beat you: they climbed a place on the ladder
        /// and gained HunterRules.PromotionLevels.
        public bool HunterPromoted { get; internal set; }

        /// The promoted hunter learned NewTrait, against the order you gave most in this fight.
        public bool TraitLearned { get; internal set; }
        public HunterTrait NewTrait { get; internal set; }

        /// Your rank on the hunters' ladder before and after this fight (1 at the top).
        public int LadderRankBefore { get; internal set; }
        public int LadderRankAfter { get; internal set; }

        /// The bounty on your head before and after this fight (BountyRules).
        public int BountyBefore { get; internal set; }
        public int BountyAfter { get; internal set; }

        /// The run ended, won or lost, and you are back at camp.
        public bool RunOver { get; internal set; }
        public bool RunWon { get; internal set; }
        public bool TutorialWon { get; internal set; }
        public IReadOnlyList<Lesson> EnemyLessons { get; internal set; }
        public IReadOnlyList<Lesson> CultLessons { get; internal set; }
    }

    /// The slice campaign as runs (Jacob's go, 10 Oct 2026; DESIGN_LOCKS.md, "Run structure and
    /// progression", and "Game design, owner decisions, 12 and 13 Oct 2026"). The tutorial wins you the
    /// Clarth. Every run then leaves camp and pushes out tier by tier, one tribe per tier. Each tribe is
    /// a road you see whole (CampaignRoad, 13 Oct 2026): PathFights rows of fights and events, a route you pick, then its chief.
    /// Capture the chief, by breaking his nerve while he still stands, and his whole tribe joins you for the
    /// rest of the run with his one main order; kill him and you take his Princess gear instead. Either way
    /// the run goes on to the next tier. After the last chief the Sovereign's hunters come (AlphaHunt, 11 Oct
    /// 2026): break them and the run is won. Your roster is your lives (13 Oct 2026): lose a fight and your
    /// fallen stay down, the winners capture one of yours, and you try that step again; the run is lost once
    /// you cannot field MinSquad. Either way it ends back at camp: that run's tribes, fighters, gear, chief
    /// orders and the tabard's wearer reset and the Clarth send a fresh squad. Materials for the Pals, the
    /// Butcher's recipe and forge (CampaignForge), the codex and what every tribe learned about you stay, as
    /// does a run or a fight still going when the game is closed (CampaignSave). Stage ChooseTribe is camp. The beasts beside
    /// the roads, their trophies and the Butcher's trophy forge are in CampaignBeast.cs (21 and 22 Oct 2026).
    public sealed partial class Campaign
    {
        public const string LeaderId = "leader";

        /// You cannot go on with fewer fighters than this: the run ends (Jacob, 13 Oct 2026, roster as lives).
        public const int MinSquad = 3;

        private static readonly Lesson[] NoLessons = new Lesson[0];

        private readonly CampaignTuning _tuning;
        private readonly DeterministicRng _seeds;
        private readonly DeterministicRng _draws;
        private readonly List<CampaignFighter> _reserve = new List<CampaignFighter>();
        private readonly List<string> _joined = new List<string>();
        private readonly List<Assist> _assists = new List<Assist>();
        private readonly TribeMind[] _minds = new TribeMind[CampaignTribes.All.Length];
        private readonly TribeMind[] _chiefMinds = new TribeMind[CampaignTribes.All.Length];
        private readonly TribeMind _huntersMind;
        private readonly List<string> _codex = new List<string>();
        private readonly List<Encounter> _offers = new List<Encounter>();
        private readonly List<Feat> _featsThisRun = new List<Feat>();
        private readonly List<string> _fightSquad = new List<string>();
        private readonly Dictionary<string, int> _namesGiven = new Dictionary<string, int>(StringComparer.Ordinal);
        private int _fighterCounter;
        private int _fightCounter;
        private int _lastKind = -1;
        private int _materials;
        private bool _tabardRecipe;
        private bool _tabardForged;
        private string _tabardWearer = string.Empty;
        private CampaignFight _active;

        // The run's gear, and the pieces a won path fight offers in place of its materials (Jacob, 13 Oct 2026): since
        // 20 Oct 2026 three to strip one of; a save from before may hold one.
        private readonly List<CampaignGear> _gear = new List<CampaignGear>();
        private readonly List<CampaignGear> _spoils = new List<CampaignGear>();
        private int _spoilsMaterials;

        /// The rules a campaign fight plays, by version: 0 the 13 Oct rules (before 20 Oct 2026); 1 those and the class
        /// spells; 2 those, the pieces their fighters wear and, in a chief fight, the chief rules; 3 those and, in a chief
        /// fight, the chief kits (21 Oct 2026): he kneels when his nerve runs out, and a chief with a kit fights with his own
        /// moves; and the Blood Moon omen keeps a fighter's bleeding blades; 4 those and, in a beast fight, the beasts (21 Oct
        /// 2026, later; the cage bear came on 22 Oct and changed nothing in a shark fight); 5 those and, in a chief fight, the
        /// kits' tactical pieces, the Bloodridge chief's five moves (he takes three a run) and the Skullpeak chief's kit (22 Oct
        /// 2026, "go, build the Skullpeak chief and the Bloodridge extras in the sim"); 6 those and, in the hunt at the end of a
        /// run, the hunt (22 Oct 2026, later, "yes go"): the chief rules, kits and tactics with the hunt captain's kit
        /// (Battle.ChiefKit.Hunt); 7 those and, in their chief fights, the Stoneveil chief's blood rite and the Thundercoast
        /// chief's tide (22 Oct 2026, later still, "go, build the Stoneveil and Thundercoast chief kits in the sim"); 8 those and,
        /// in every fight, the parry at the center (24 Oct 2026, "go, build the parry at the center in the sim behind rules version
        /// 8"; Battle.FightRules.ParryCenter): every move with a tell has a beat to parry, tribes and hunters learn around the
        /// parry, and a hunter's Quickstrike is Double swing; 9 those and, in every fight, the squad first (24 Oct 2026, later, "go,
        /// build rules 9 with your three picks as written"; Battle.FightRules.SquadFirst): a band's heavy swings come every 5
        /// seconds and are small beats, a stale Hold only soaks, marks earn and crack, a chief brings his own mind (ChiefMind),
        /// and a mind learns one lesson a fight, a wait out or feint on one move. CampaignSave writes it with an unfinished fight,
        /// so the fight resumes under the rules it began with. 10 those and the raid chief (25 Oct 2026, "show me in game those boss
        /// fights ... do 1 pass"; Battle.FightRules.RaidChief): your Hold is free and a parry pays no points, and Bloodridge's
        /// chief fights alone as a raid boss whose bar refills like posture (MakeRaidChief). 11 those and road combat (25 Oct
        /// 2026, later, "go, build rules 11 in the sim as written in 11263, 11358 and 11406"; Battle.FightRules.RoadCombat): in a
        /// road fight every fighter has his own nerve and the fight ends when one side has nobody left fighting, a mark readies
        /// a casher whose spell is at least half ready, pairs bond for the run, the classes' defence moves, and a run of 3 lives
        /// in which your whole squad comes back every fight. 12 those and the road kit (25 Oct 2026, later still, "alright bro lets
        /// keep it as dodge and build these 5 lets go"; Battle.FightRules.RoadKit): Fall Back is a pure dodge, every class's
        /// automatic boost, Burn on Bloodridge fighters' blows (your Bloodridge recruits' too) and the Aimed Shot's flare,
        /// Bloodridge's road fighters carrying small versions of their chief's moves, and the streak on a fight's materials.
        /// 15 those and the Butcher's first combo (29 Oct 2026, "Go, Black Hole Round"; ButcherComboRules). 16 those and, in a
        /// duel, the champion duel (30 Oct 2026, "Yes, build it"; Battle.FightRules.ChampionDuel; DuelRules). 17 those and the
        /// damage layers (30 Oct 2026, later, "Yes, start step 2"; Battle.FightRules.Layers; LayersRules). 18 those and the
        /// Butcher's theme (30 Oct 2026, last, "Yes, build step 3"; Battle.FightRules.Marks; MarksRules). 19 those and fix 5 (31
        /// Oct 2026, "Go, build it"; PalRoadRules): a Pal on every road fight, and a won fight offering its Pal's picks and the
        /// tribe's gear together. 20 those and fix 1 of Jacob's playtest (31 Oct 2026, "read log then go build fix 1";
        /// Battle.FightRules.Payoffs; PayoffRules): a snare pays one Aimed Shot crit, and bleeding counts at most a fighter's health.
        /// 21 those and fixes 3, 7 and 9 (31 Oct 2026, "Go, lock and build"; Battle.FightRules.Danger; DangerRules): blasts a
        /// Hold can't soak, Bloodridge's carried moves from the first road fight, and the chief's swings and blasts taking turns.
        /// 22 those and the parry meter (31 Oct 2026, "Go, lock and build"; Battle.FightRules.Meter; MeterRules): your parries
        /// fill 3 bars, your chief spell is cast with all 3, and held bars make your combos stronger.
        /// 23 those and the floor under the three Pals (31 Oct 2026, "Go, sprint it"; Battle.FightRules.Floor; FloorRules).
        /// 24 those and the Whisperer (31 Oct 2026, "Go, build her"; Battle.FightRules.Whisper; WhispererRules): habits set at
        /// camp and kept, her road card, the habits, pairs and rank 2 in the fight, and a fallen fighter's rank.
        /// 25 those and the beasts as their approved scenes show them (1 Nov 2026, "Sim follows scenes", then "Go as written";
        /// Battle.FightRules.BeastScenes; BeastSceneRules): the shark's Bite on a whole row and its Sweep on all of them, and the
        /// cage bear's Slam and Grab.
        /// 26 those and the tight parry (1 Nov 2026, "Go, build it"; Battle.FightRules.Tight; TightParryRules): a 0.2 s window, a
        /// Hold out of it that lands in full and opens your side for a second, and a counter on every parry.
        /// 27 those and the beasts as raid bosses (1 Nov 2026, "Full plan", then "Go as written"; Battle.FightRules.BeastRaid;
        /// BeastRaidRules): the shark's Feeding frenzy and its sand pups, the cage bear's Overload and its climbing Pulse.
        /// 28 those and the Shade's forms (Jacob in chat, 31 Oct 2026: "go" on Claude's Shade design; Battle.FightRules.ShadeForms;
        /// ShadeFormsRules): her road card, and Sanctuary, Backstab and Fear at their second and third forms, each with its price.
        /// 29 those and the Glutton (Jacob in chat, 1 Nov 2026: "go" on Claude's calls; Battle.FightRules.Glutton; GluttonTreeRules):
        /// his camp tree paid with codex points, his road card lending a limb's next talent, Feed, and the 35 talents.
        /// 30 those and the Pals' duos (Jacob in chat, 1 Nov 2026: "go write them"; Battle.FightRules.Duos; PalDuoRules): a duo on
        /// a card once the run holds a pick from both of its Pals, and the six in the fight.
        /// 31 those and the sizing pass (Jacob in chat, 1 Nov 2026: "okay go"; Battle.FightRules.Sized; SizedRules): the Pals'
        /// picks sized with the builds test to decide fights.
        /// 32 those and the Butcher's flavours (Jacob in chat, 2 Nov 2026: "both 1 and 2"; Battle.FightRules.Flavours;
        /// ButcherFlavourRules): combos growing with their flavour, and each flavour's final form.
        /// 33 those, the Whisperer's vows and the Butcher's flavour fights (Jacob in chat, 2 Nov 2026: "whisperer - 1", "butcher
        /// - 1 and 2"; Battle.FightRules.Vows and MarksOnHits; VowFlavourRules): a rank 2 fighter takes one of his class's two
        /// vows from her card, each Butcher fight offers one flavour, and a combo's wearer marks with every blow.
        /// 34 those and the duos that change a move (Jacob in chat, 2 Nov 2026: "merge first, then build the duos on top. Go";
        /// Battle.FightRules.DuoMoves; DuoMovesRules): six new duos, each needing one exact pick from each of its Pals, free to
        /// take, growing with the streak of big moves answered, each with its catch.
        public const int RulesVersion = 34;
        public const int SquadScreenRules = 14;
        public const int ClearRoadRules = 13;

        /// The campaign rules version from which the Butcher's road fights offer his combos (FavourRules.Combos) in place of
        /// his three button favours: a combo taken goes on a fighter of its class as a piece of the run's gear, so it takes
        /// that fighter's one slot, keeps its pact and goes when the run does (DESIGN_LOCKS, "The Pals on the road").
        public const int ButcherComboRules = 15;

        /// The campaign rules version from which a duel is a champion duel (DESIGN_LOCKS, "Duels"): his heavy swing is a big
        /// beat every 2.5 s, he yields when his nerve runs out, and he springs your trap once.
        public const int DuelRules = 16;

        /// The campaign rules version from which every fight plays the damage layers (DESIGN_LOCKS, "Builds: one theme per
        /// Pal", step 2): every hit and heal goes through one pipeline, bonuses of one layer adding, the layers multiplying,
        /// rounded once with the leftover kept.
        public const int LayersRules = 17;

        /// The campaign rules version from which the Butcher's theme plays (DESIGN_LOCKS, "The Butcher's theme", builds step 3):
        /// his combos lay Crush, Exposed and Stasis, which stack to 3 and burst; a combo taken again ranks up; and the road's
        /// Pals and his three combos lean toward what the run holds.
        public const int MarksRules = 18;

        /// The campaign rules version from which fix 5 plays (DESIGN_LOCKS, "Fix 5: a Pal on every fight"): a run's roads carry
        /// a Pal on every fight, no two the same in a row (PalRoadRun), and a won fight offers its Pal's picks and the tribe's
        /// gear on one card, one to take or the materials to bank. Fights play as at rules 18.
        public const int PalRoadRules = 19;

        /// The campaign rules version from which payoffs spread (Jacob's playtest, HANDOFF_LOG 12797, fix 1;
        /// Battle.BattleSimulation.Payoffs.cs): a snare pays one Aimed Shot crit and breaks, and bleeding and the rip count at
        /// most Battle.PayoffRules.PercentHealthCap of a chief's or a beast's health.
        public const int PayoffRules = 20;

        /// The campaign rules version from which danger plays (DESIGN_LOCKS, "Jacob's playtest fixes 3, 7 and 9";
        /// Battle.BattleSimulation.Danger.cs): a Hold that is not a parry soaks nothing from the Eclipse, the Brand or the Burning
        /// Ring; Bloodridge's road bands carry their chief's moves from the first fight, each a third of a fighter's health; and
        /// the chief's Cleave waits for one of his blasts between two Cleaves, his Brand and Ring each 35%.
        public const int DangerRules = 21;

        /// The campaign rules version from which the parry meter plays (DESIGN_LOCKS, "The parry meter";
        /// Battle.BattleSimulation.Meter.cs): every parry your side lands fills one of 3 bars, your chief spell is cast with all
        /// 3 and costs no points, and each held bar makes your combos and cash ins 10% stronger.
        public const int MeterRules = 22;

        /// The campaign rules version from which the floor under the three Pals plays (DESIGN_LOCKS, "The floor under the three
        /// Pals"; Campaign.PalStores.cs and Battle.BattleSimulation.Shade.cs): the Pals' stores, a rank 3 fighter's second slot,
        /// the faster and cheaper hooks, and the Shade's Sanctuary, Backstab and Fear.
        public const int FloorRules = 23;

        /// The campaign rules version from which the Whisperer plays (DESIGN_LOCKS, "Habits come with the game, the Whisperer
        /// upgrades them"; Campaign.PalStores.cs and Battle.BattleSimulation.Whisperer.cs): each class's habit set at camp and
        /// kept, her road card (a rank up, a pair or a habit upgrade for the run), the habits, their upgrades, the pairs and rank 2
        /// in the fight, and a fighter who falls in a lost fight losing his top rank.
        public const int WhispererRules = 24;

        /// The campaign rules version from which a beast fight plays its moves as the approved scenes show them (Jacob, 1 Nov
        /// 2026: "Sim follows scenes", then "Go as written"; Battle.FightRules.BeastScenes, Battle.BattleSimulation.Beast.cs): the
        /// sand shark's Bite takes the marked one's whole row and its tail's Sweep all of them, and the cage bear's Grab is a Slam
        /// and Grab, an ice wave on all of them and then the claw. Every number is a starting value.
        public const int BeastSceneRules = 25;

        /// The campaign rules version from which every fight plays the tight parry (DESIGN_LOCKS, "The parry: tight window, big
        /// payoff, a gold flash on the attacker"; Battle.BattleSimulation.Tight.cs): the 0.2 s window, the whiff and the counter.
        public const int TightParryRules = 26;

        /// The campaign rules version from which a beast fight plays its beast as a raid boss (Jacob, 1 Nov 2026: "can we finish
        /// the bear and shark just like a wow boss", then "Full plan" and "Go as written"; Battle.FightRules.BeastRaid,
        /// Battle.BattleSimulation.BeastRaid.cs): the shark's Feeding frenzy at its second broken part and then its two sand pups
        /// (after its parts on its side), the cage bear's Overload when its ribcage breaks, and its Last stand Pulse climbing.
        /// Built as 26 and renumbered to 27 the same day, after the tight parry took 26.
        public const int BeastRaidRules = 27;

        /// The campaign rules version from which the Shade's forms play (Jacob in chat, 31 Oct 2026: "go" on Claude's Shade
        /// design; Campaign.PalStores.cs and Battle.BattleSimulation.Shade.cs): her road card (a new route on a plain button, or
        /// the next form of one you hold), Sanctuary's Relay and Last Refuge, Backstab's Twin Knives and Shadow Strike, Fear's
        /// Spreading Dread and Turncoat, and each route's price: Sanctuary's parries do not stagger, Backstab's Fall Back waits
        /// longer, and Fear's Focus Fire is shorter.
        public const int ShadeFormsRules = 28;

        /// The campaign rules version from which the Glutton plays (Jacob in chat, 1 Nov 2026: "go" on Claude's calls;
        /// Campaign.PalStores.cs and Battle.BattleSimulation.Glutton.cs): his tree at camp, bought with talent points the codex
        /// pays, kept between runs; his road card, lending the next talent of a limb for the run (a tier 3 one with its keystone);
        /// Feed doubling one talent and eating another of its limb; and every talent's effect.
        public const int GluttonTreeRules = 29;

        /// The campaign rules version from which the Pals' duos play (Jacob in chat, 1 Nov 2026: "go write them"; Campaign.Duos.cs
        /// and Battle.BattleSimulation.Duos.cs): a pick joining two Pals' themes, shown on a card once the run holds a pick from
        /// both.
        public const int PalDuoRules = 30;

        /// The campaign rules version from which the Pals' picks play sized (Jacob in chat, 1 Nov 2026: "okay go";
        /// Battle.BattleSimulation.Sized.cs, Battle.SizeRules): big enough that leaning into one Pal decides runs.
        public const int SizedRules = 31;

        /// The campaign rules version from which the Butcher's flavours play (Jacob in chat, 2 Nov 2026: "both 1 and 2";
        /// Battle.BattleSimulation.Flavours.cs): a combo grows with the ranks of its flavour the side wears, and three ranks of
        /// one flavour unlock its final form.
        public const int ButcherFlavourRules = 32;

        /// The campaign rules version from which the Whisperer's vows and the Butcher's flavour fights play (Jacob in chat, 2 Nov
        /// 2026: "whisperer - 1", "butcher - 1 and 2"; Campaign.Vows.cs, Battle.BattleSimulation.Vows.cs and Flavours.cs).
        public const int VowFlavourRules = 33;

        /// The campaign rules version from which the duos are the six that change a move (Campaign.Duos.cs, Battle.BattleSimulation
        /// .DuoMoves.cs): Blood Parry, Chain Reaction, Executioner, Endless Unleash, Shadow Twin and Gorge.
        public const int DuoMovesRules = 34;

        /// Whether this run's roads carry a Pal on every fight (fix 5): set when the run starts under PalRoadRules, and kept
        /// with the run, so its roads come back the same when it loads.
        public bool PalRoadRun { get; internal set; }
        /// Persisted per run: older roads keep their original rows and event lanes.
        public bool ClearRoadRun { get; internal set; }
        public bool StagedBloodridgeRoad => ClearRoadRun && Target != null && Target.Id == CampaignTribes.BloodridgeId;

        // For CampaignSave: the rules version of the unfinished fight it is about to resume, or -1 for the current one.
        internal int ResumeRulesVersion { get; set; } = -1;

        // The run's favours from the Pals, and the ones a won favour fight offers in place of its materials (Jacob,
        // 18 Oct 2026). _spoilsMaterials holds what taking one gives back, as for a piece.
        private readonly List<HeldFavour> _favours = new List<HeldFavour>();
        private FavourOffer _favourOffer;

        // The tribes that captured one of yours this run, whom a Grudge hits harder (Jacob, 13 Oct 2026).
        private readonly List<string> _grudges = new List<string>();

        // The current tribe's road and the lane of the node you stand on, -1 before its first row; the omens picked at
        // camp (Jacob, 13 Oct 2026; DESIGN_LOCKS, "Sprint 3 calls").
        private CampaignRoad _road;
        private int _roadLane = -1;
        private readonly List<Omen> _omens = new List<Omen>();

        // The hunters (Jacob's go, 18 Oct 2026): the company and its ladder outlive every run; the run's heat, and the
        // hunters you beat and who were promoted this run, go with it.
        private HunterCompany _company;
        private int _heat;

        // Ambush fights ever finished, so they take no place in a tribe path's order of names. Counted when the fight
        // closes, so a save in the middle of one plays it back the same way.
        private int _ambushFights;
        private readonly List<string> _beatenThisRun = new List<string>();
        private readonly List<string> _promotedThisRun = new List<string>();

        // Where the running fight began, so a save can start it again and replay its orders.
        private ulong _fightSeedState;
        private ulong _fightSeedIncrement;
        private int _fightCounterAtStart;
        private int _fightOffer = -1;

        private Campaign(ulong seed, CampaignTuning tuning, DeterministicRng seeds, DeterministicRng draws)
        {
            Seed = seed;
            _tuning = tuning == null ? new CampaignTuning() : tuning.Clone();
            _seeds = seeds ?? DeterministicRng.ForStream(seed, SimStream.Events);
            _draws = draws ?? DeterministicRng.ForStream(seed, SimStream.Draft);
            for (int i = 0; i < CampaignTribes.All.Length; i++)
            {
                _minds[i] = new TribeMind(CampaignTribes.All[i].Id, CampaignTribes.All[i].ChiefName);
                _chiefMinds[i] = new TribeMind(CampaignTribes.All[i].Id, CampaignTribes.All[i].ChiefName);
            }

            // The hunters' habits never change, so nothing saves their mind: they arrive with the same habits every
            // fight. What grows is the company (HunterCompany): its ladder, levels and traits.
            _huntersMind = new TribeMind(AlphaHunt.Id, AlphaHunt.Tribe.ChiefName);
            TeachWars(_huntersMind, AlphaHunt.Tribe, _tuning);
            _company = HunterCompany.Make(seed);
            CultMind = new TribeMind(CampaignTribes.CultId, "your cult");
            Archive = new FightArchive();
        }

        /// A new campaign. Every tribe arrives with the habits its wars taught it, and so does its chief's own mind (ChiefMind).
        public static Campaign New(ulong seed, CampaignTuning tuning = null)
        {
            var c = new Campaign(seed, tuning, null, null);
            for (int i = 0; i < CampaignTribes.All.Length; i++)
            {
                if (CampaignTribes.All[i].IsStarter) continue;
                TeachWars(c._minds[i], CampaignTribes.All[i], c._tuning);
                TeachWars(c._chiefMinds[i], CampaignTribes.All[i], c._tuning);
            }
            c.Stage = CampaignStage.Tutorial;
            c.Discover("Tribe: the Clarth");
            return c;
        }

        // The habits a tribe's wars taught it, which it arrives with. Every tribe has also seen invaders run. A wait out or
        // feint its wars taught (the hunters' wait out, 14 Oct 2026) sits on its band's heavy swing, the move it acts on under
        // the squad first (campaign rules 9); older rules never read the move (BuildSetup drops it).
        private static void TeachWars(TribeMind mind, CampaignTribe t, CampaignTuning tuning)
        {
            for (int h = 0; h < t.WarTaught.Count; h++)
            {
                if (t.WarTaught[h] == Habit.CoverFocused && !tuning.WarTaughtCover) continue;
                mind.SetHabit(t.WarTaught[h], tuning.WarTaughtValue);
                mind.SetHabitMove(t.WarTaught[h], ParryRules.HeavyName);
            }
            if (tuning.WarTaughtPressOnFallBack) mind.SetHabit(Habit.PressOnFallBack, tuning.WarTaughtValue);
        }

        /// For CampaignSave: a chief whose own mind the save did not hold (every chief before save version 19) arrives with
        /// the habits his tribe's wars taught, as in a new campaign.
        internal void TeachNewChiefs(IReadOnlyList<string> saved)
        {
            for (int i = 0; i < CampaignTribes.All.Length; i++)
            {
                CampaignTribe t = CampaignTribes.All[i];
                bool known = false;
                for (int j = 0; j < saved.Count && !known; j++) known = saved[j] == t.Id;
                if (!known && !t.IsStarter) TeachWars(_chiefMinds[i], t, _tuning);
            }
        }

        /// For CampaignSave: a tribe whose mind an older save did not hold, one added since it was written, arrives with
        /// the habits its wars taught, as in a new campaign.
        internal void TeachNewTribes(IReadOnlyList<string> saved)
        {
            for (int i = 0; i < CampaignTribes.All.Length; i++)
            {
                CampaignTribe t = CampaignTribes.All[i];
                bool known = false;
                for (int j = 0; j < saved.Count && !known; j++) known = saved[j] == t.Id;
                if (!known && !t.IsStarter) TeachWars(_minds[i], t, _tuning);
            }
        }

        /// For CampaignSave only: an empty campaign with its streams where they were.
        internal static Campaign Restore(ulong seed, CampaignTuning tuning, DeterministicRng seeds, DeterministicRng draws)
        {
            return new Campaign(seed, tuning, seeds, draws);
        }

        /// The first tribe of the lowest tier after the starter. Since 18 Oct 2026 a run picks one of each tier's tribes at
        /// random (NextRunTribe, RunRoute); this is the same while every tier has one tribe.
        public static CampaignTribe FirstTribe()
        {
            return NextTribe(null);
        }

        /// The first tribe of the next tier up, or null after the last. After the last chief the hunters come (AlphaHunt),
        /// who are not a tribe on the path. A run picks among a tier's tribes (RunRoute); this is the same while every
        /// tier has one tribe.
        public static CampaignTribe NextTribe(CampaignTribe after)
        {
            CampaignTribe best = null;
            int floor = after == null ? int.MinValue : after.Tier;
            for (int i = 0; i < CampaignTribes.All.Length; i++)
            {
                CampaignTribe t = CampaignTribes.All[i];
                if (t.IsStarter || t.Tier <= floor) continue;
                if (best == null || t.Tier < best.Tier) best = t;
            }
            return best;
        }

        public ulong Seed { get; }
        public CampaignStage Stage { get; private set; }
        public CampaignTuning Tuning => _tuning.Clone();
        public bool TutorialDone { get; internal set; }
        public int RunsStarted { get; internal set; }
        // 0 preserves the correlated picker of an already-running pre-v14 save; new runs use 1.
        internal int RoutePickVersion { get; set; } = 1;
        public int RunsWon { get; internal set; }
        public TribeMind CultMind { get; }

        /// Recorded finished fights, kept by CampaignSave for replaying a lesson after reopening.
        public FightArchive Archive { get; }

        /// Your fighters this run (at camp: the squad the next run starts with).
        public IReadOnlyList<CampaignFighter> Reserve => _reserve;

        /// The tribes with you this run. The Clarth always are, once the tutorial is won.
        public IReadOnlyList<string> Joined => _joined;

        /// The chief orders taken this run.
        public IReadOnlyList<Assist> Assists => _assists;
        public IReadOnlyList<string> Codex => _codex;
        public IReadOnlyList<Encounter> Offers => _offers;

        /// Materials at camp for the Pals. They come home even from a lost run.
        public int Materials => _materials;

        /// Rows on each tribe's road before its chief, Long Road counted.
        public int PathLength => RoadRows;

        /// Always empty since 10 Oct 2026: nothing scores feats.
        public IReadOnlyList<Feat> FeatsThisRun => _featsThisRun;

        /// The tribe this run is on now, AlphaHunt.Tribe once the hunters come, or null at camp.
        public CampaignTribe Target { get; private set; }

        /// Fights won on the current tribe's path.
        public int Step { get; private set; }

        /// The fight going on, if any. After reading a save it can already be part way through.
        public CampaignFight Active => _active;

        /// Set when a save could not be read in full, so the campaign reopened with less: where an
        /// unfinished fight began, or back at camp. Empty otherwise.
        public string LoadNote { get; internal set; } = string.Empty;

        /// The Butcher knows the Lava Sun Tabard (call J1): he learned it when the Bloodridge chief
        /// broke. Kept for good.
        public bool TabardRecipeKnown => _tabardRecipe;

        /// The tabard is forged, for good: from then on one fighter wears it every run.
        public bool TabardForged => _tabardForged;

        /// The fighter wearing the tabard (an id in Reserve), or empty. Their hits drain
        /// CampaignForge.TabardFear percent more nerve. It comes off when the run ends.
        public string TabardWearer => _tabardWearer;

        /// The run's gear: every piece you hold, worn or not. It goes when the run ends.
        public IReadOnlyList<CampaignGear> Gear => _gear;

        /// The pieces the last won path fight offers to strip one of in place of its materials (Jacob, 20 Oct 2026:
        /// "strip one of three pieces or bank"), or none. Gone once the next fight starts or the run moves on.
        public IReadOnlyList<CampaignGear> SpoilsChoices => _spoils;

        /// The first piece on offer, or null. For screens built when a won fight offered one piece.
        public CampaignGear Spoils => _spoils.Count == 0 ? null : _spoils[0];

        /// The materials taking the spoils, or a favour on offer, gives back.
        public int SpoilsMaterials => _spoilsMaterials;

        /// The run's favours from the Pals (Jacob, 18 Oct 2026), in the order first taken. Gone when the run ends.
        public IReadOnlyList<HeldFavour> Favours => _favours;

        /// The favours the last won favour fight offers in place of its materials, or null. Gone once the next fight
        /// starts or the run moves on.
        public FavourOffer FavourOffer => _favourOffer;

        /// How many times the run took this favour: 0 to FavourRules.MaxRank. A combo: its piece's rank while the run holds
        /// it, 1, or up to MarkRules.MaxStacks once the Butcher's offer of it is taken again (rules 18).
        public int FavourRank(Favour favour)
        {
            if (FavourRules.IsCombo(favour))
            {
                CampaignGear piece = ComboPiece(FavourRules.ComboBend(favour));
                return piece == null ? 0 : piece.Rank;
            }
            // A duo's rank is 1 once the run holds it (rules 30).
            if (FavourRules.IsDuo(favour)) return HoldsDuo(FavourRules.DuoOf(favour)) ? 1 : 0;
            // A Glutton pick's rank is 1 once the run holds that talent (rules 29).
            if (FavourRules.IsGlutton(favour)) return Holds(FavourRules.TalentOf(favour)) ? 1 : 0;
            // A Shade pick's rank is its route's form (rules 28).
            if (FavourRules.IsShade(favour))
            {
                HeldRoute route = FindRoute(FavourRules.RouteOf(favour));
                return route == null ? 0 : route.Form;
            }
            HeldFavour held = FindFavour(favour);
            return held == null ? 0 : held.Rank;
        }

        /// What your side's orders get from the run's favours in its next fight.
        public SideFavours RunFavours => FavourRules.ToSide(_favours);

        /// The tribes that captured one of yours this run, by id (AlphaHunt.Id for the hunters), in the order they
        /// did. Your Grudge fighters do Quirks.GrudgePercent more damage on them. Forgotten when the run ends.
        public IReadOnlyList<string> Grudges => _grudges;

        /// The hunters (Jacob's go, 18 Oct 2026: the rival, built like Odyssey's mercenaries): their ladder, with you on
        /// it, and every named hunter. It outlives every run.
        public HunterCompany Company => _company;

        /// The bounty on your head this run, 1 to BountyRules.Max. Beating a chief and stripping gear raise it; it
        /// sends more hunters, and better ones.
        public int Bounty => BountyRules.Level(_heat);

        /// The heat behind the bounty, and how much more raises it a level (0 at the top).
        public int Heat => _heat;
        public int HeatToNext => Bounty >= BountyRules.Max ? 0 : Bounty * BountyRules.HeatPerLevel - _heat;

        /// The company's might: one for every run that has ended. Every hunter fights that many levels above their own.
        public int Might => Math.Max(0, RunsStarted - (Stage == CampaignStage.InRun ? 1 : 0));

        /// A hunter's level in a fight now: their own plus the company's might.
        public int HunterLevel(CampaignHunter hunter)
        {
            return hunter == null ? Might : Math.Min(HunterRules.MaxLevel, hunter.Level + Might);
        }

        /// The hunters you beat this run, by id. They do not come for you again this run.
        public IReadOnlyList<string> HuntersBeatenThisRun => _beatenThisRun;

        /// The hunter waiting on this road's ambushes, or null while none would come (bounty under
        /// BountyRules.AmbushFrom, no road, or nobody the bounty sends).
        public CampaignHunter HunterOnRoad
        {
            get
            {
                if (_road == null || Stage != CampaignStage.InRun || Bounty < BountyRules.AmbushFrom) return null;
                return PickHunter(false, "road." + RunsStarted.ToString(CultureInfo.InvariantCulture) + "." + _road.TribeId);
            }
        }

        /// Whether hunters wait on this road node (Jacob's go, 18 Oct 2026). From bounty 2 on, some rows of the road
        /// you have not walked yet have one fight node taken by them; more rows the higher the bounty. You can go
        /// round them. Ask for every node when drawing the road: it changes as the bounty rises.
        public bool IsAmbush(RoadNode node)
        {
            if (node == null || node.Kind != RoadNodeKind.Fight || _road == null || node.Row < Step ||
                !ReferenceEquals(_road.Node(node.Row, node.Lane), node))
                return false;
            return AmbushLane(node.Row) == node.Lane && HunterOnRoad != null;
        }

        /// The current tribe's road, seen whole (Jacob, 13 Oct 2026, the path map), or null at camp, in the tutorial and
        /// when the hunters come.
        public CampaignRoad Road => _road;

        /// The lane of the road node you stand on, or -1 before the road's first row.
        public int RoadLane => _roadLane;

        /// The road nodes you can go to next, fights and events: the whole first row, then the lanes the node you
        /// stand on leads to. Empty once the chief is next. The fights among them are Offers; an event is taken with
        /// TryTakeEvent.
        public IReadOnlyList<RoadNode> NextNodes
        {
            get
            {
                var next = new List<RoadNode>();
                if (_road == null || Stage != CampaignStage.InRun || Step >= _road.Rows) return next;
                RoadNode here = Step == 0 ? null : _road.Node(Step - 1, _roadLane);
                if (here == null)
                    for (int lane = 0; lane < CampaignRoad.Lanes; lane++) next.Add(_road.Node(Step, lane));
                else
                    foreach (int lane in here.Next) next.Add(_road.Node(Step, lane));
                return next;
            }
        }

        /// The tier of gear a road fight drops, omens counted. Meeting its bonus goal raises it one more.
        public GearTier DropOf(RoadNode node)
        {
            if (node == null || node.Kind != RoadNodeKind.Fight) return GearTier.Pauper;
            return Raise(CampaignGearRules.DropTier(node.Fight), _omens.Count >= OmenRules.TierUpAt ? 1 : 0);
        }

        /// The omens picked at camp (Jacob, 13 Oct 2026). They shape every run until you change them.
        public IReadOnlyList<Omen> Omens => _omens;

        public bool HasOmen(Omen omen) => _omens.Contains(omen);

        /// A piece of the run's gear by id, or null.
        public CampaignGear FindGear(string gearId)
        {
            if (string.IsNullOrEmpty(gearId)) return null;
            for (int i = 0; i < _gear.Count; i++) if (_gear[i].Id == gearId) return _gear[i];
            return null;
        }

        /// The piece this fighter wears, or null.
        public CampaignGear GearOf(string fighterId)
        {
            CampaignFighter f = FindFighter(fighterId);
            return f == null ? null : FindGear(f.GearId);
        }

        internal DeterministicRng SeedStream => _seeds;
        internal DeterministicRng DrawStream => _draws;
        internal int FighterCounter { get => _fighterCounter; set => _fighterCounter = value; }
        internal int FightCounter { get => _fightCounter; set => _fightCounter = value; }

        /// For CampaignSave: the run's last encounter kind, and the campaign as it stood when the
        /// running fight began (offer -1 is the tutorial).
        internal int LastKind => _lastKind;
        internal ulong FightSeedState => _fightSeedState;
        internal ulong FightSeedIncrement => _fightSeedIncrement;
        internal int FightCounterAtStart => _fightCounterAtStart;
        internal int FightOffer => _fightOffer;
        internal IReadOnlyList<string> FightSquad => _fightSquad;

        /// For CampaignSave: how many of your fighters were given each name, so the next is "the Second".
        internal IReadOnlyDictionary<string, int> NamesGiven => _namesGiven;

        /// A tribe's chief this campaign (CampaignNames): the same person every time they come back. Null for
        /// the Sovereign's hunters, who stay faceless, and for your cult.
        public CampaignChief ChiefOf(string tribeId)
        {
            return CampaignNames.Chief(Seed, tribeId);
        }

        /// The moves a chief with his own kit takes into this run (Battle.ChiefKits): ChiefKits.Takes of his pool, picked by
        /// the campaign's seed and the run from their own dice, so no fight's draw moves (Jacob, 20 Oct 2026: "Each run he
        /// takes 3 of his 5 moves"). None without a kit.
        public ChiefMove ChiefMovesThisRun(CampaignTribe tribe)
        {
            return ChiefMoves(tribe, RulesVersion);
        }

        // The same under a rules version: before 5 a kit had only the three moves written on 21 Oct, so he took all three,
        // and the Skullpeak chief had no kit, nor before 7 the Stoneveil and Thundercoast chiefs (KitUnder).
        private ChiefMove ChiefMoves(CampaignTribe tribe, int rules)
        {
            ChiefMove pool = tribe == null ? ChiefMove.None : ChiefKits.Pool(KitUnder(tribe, rules));
            if (rules < 5) pool &= ChiefKits.FirstWritten;
            var moves = new List<ChiefMove>();
            for (int bit = 0; bit < 30; bit++)
                if (((int)pool & (1 << bit)) != 0) moves.Add((ChiefMove)(1 << bit));
            if (moves.Count <= ChiefKits.Takes) return pool;
            var dice = new DeterministicRng(CampaignRoad.Mix(Seed, RunsStarted, tribe.Id + ".moves"), 12UL);
            ChiefMove taken = ChiefMove.None;
            for (int k = 0; k < ChiefKits.Takes; k++)
            {
                int pick = dice.Next(moves.Count);
                taken |= moves[pick];
                moves.RemoveAt(pick);
            }
            return taken;
        }

        // The kit a tribe's chief fights with under a rules version: Bloodridge's from 3, Skullpeak's from 5, the hunt
        // captain's from 6, Stoneveil's and Thundercoast's from 7.
        private static ChiefKit KitUnder(CampaignTribe tribe, int rules)
        {
            if (tribe == null) return ChiefKit.None;
            int since = tribe.Kit == ChiefKit.Stoneveil || tribe.Kit == ChiefKit.Thundercoast ? 7
                : tribe.Kit == ChiefKit.Hunt ? 6 : tribe.Kit == ChiefKit.Skullpeak ? 5 : 3;
            return rules >= since ? tribe.Kit : ChiefKit.None;
        }

        /// You have fought this tribe's chief before, in any run: his kneel line changes (Jacob, 20 Oct 2026).
        public bool MetChief(string tribeId)
        {
            CampaignTribe tribe = FindTarget(tribeId);
            return tribe != null && _codex.Contains(MetEntry(tribe));
        }

        /// Your best grade against this tribe's chief, which his codex page keeps (Jacob, 20 Oct 2026). False until you beat
        /// him in a fight with his kit. AlphaHunt.Id gives the hunt captain's, from the hunt (22 Oct 2026).
        public bool BestGrade(string tribeId, out GloryRank grade)
        {
            grade = GloryRank.D;
            CampaignTribe tribe = FindTarget(tribeId);
            if (tribe == null) return false;
            string prefix = GradePrefix(tribe);
            for (int i = 0; i < _codex.Count; i++)
            {
                if (!_codex[i].StartsWith(prefix, StringComparison.Ordinal)) continue;
                string letter = _codex[i].Substring(prefix.Length);
                for (int r = (int)GloryRank.S; r >= 0; r--)
                {
                    if (letter != ChiefKits.Letter((GloryRank)r)) continue;
                    grade = (GloryRank)r;
                    return true;
                }
            }
            return false;
        }

        /// The hidden chain you found against this tribe's chief ("Sunbreaker"), or empty: his codex page keeps a blank until
        /// you find it. ChiefKits.ChainName(tribe.Kit) says whether he hides one.
        public string ChiefChain(string tribeId)
        {
            CampaignTribe tribe = FindTarget(tribeId);
            string chain = tribe == null ? string.Empty : ChiefKits.ChainName(tribe.Kit);
            return chain.Length > 0 && _codex.Contains(ChainEntry(tribe, chain)) ? chain : string.Empty;
        }

        // His codex page's lines (21 Oct 2026). The codex is saved and outlives every run.
        private static string MetEntry(CampaignTribe tribe) => "Chief met: " + tribe.ChiefName;
        private static string GradePrefix(CampaignTribe tribe) => "Best grade against " + tribe.ChiefName + ": ";
        private static string ChainEntry(CampaignTribe tribe, string chain) => "Hidden chain of " + tribe.ChiefName + ": " + chain;

        // Puts a grade on his codex page when it beats your best. True when it did.
        private bool RecordGrade(CampaignTribe tribe, GloryRank grade)
        {
            if (BestGrade(tribe.Id, out GloryRank best) && best >= grade) return false;
            string prefix = GradePrefix(tribe);
            _codex.RemoveAll(entry => entry.StartsWith(prefix, StringComparison.Ordinal));
            _codex.Add(prefix + ChiefKits.Letter(grade));
            return true;
        }

        // Whether your side did this in the fight: an event of this kind, with this detail unless it is empty.
        private static bool Happened(FightRecord record, BattleEventKind kind, string detail)
        {
            IReadOnlyList<BattleEvent> ev = record.Events;
            for (int i = 0; i < ev.Count; i++)
                if (ev[i].Kind == kind && ev[i].ActorSide == 0 && (detail.Length == 0 || ev[i].Detail == detail)) return true;
            return false;
        }

        /// What a tribe learned about you. The Sovereign's hunters have a mind that never learns.
        public TribeMind Mind(string tribeId)
        {
            if (tribeId == CampaignTribes.CultId) return CultMind;
            if (tribeId == AlphaHunt.Id) return _huntersMind;
            int i = IndexOf(tribeId);
            return i < 0 ? null : _minds[i];
        }

        /// What a tribe's chief learned about you in fights against him (Jacob, 24 Oct 2026: "a chief learns only from fights
        /// against him, one lesson per fight, on one move"). From campaign rules 9 his fight brings this mind, not the tribe's,
        /// and only his fights teach it, so road fights never arm him. Null for a tribe there is none of.
        public TribeMind ChiefMind(string tribeId)
        {
            int i = IndexOf(tribeId);
            return i < 0 ? null : _chiefMinds[i];
        }

        /// The mind that will fight you in this encounter, and learn from it: from campaign rules 9 a chief's own
        /// (ChiefMind), otherwise the tribe's (Mind).
        public TribeMind MindFor(Encounter encounter)
        {
            if (encounter == null || encounter.Tribe == null) return null;
            int rules = Math.Min(ResumeRulesVersion < 0 ? RulesVersion : ResumeRulesVersion, _tuning.RulesCap);
            return LearnerMind(encounter, rules >= SquadFirstRules);
        }

        // The mind a fight against this encounter fights with and teaches: a chief's own under the squad first.
        private TribeMind LearnerMind(Encounter encounter, bool squadFirst)
        {
            return squadFirst && encounter.Kind == EncounterKind.Chief ? ChiefMind(encounter.Tribe.Id) : Mind(encounter.Tribe.Id);
        }

        /// The campaign rules version that plays the squad first (FightRules.SquadFirst).
        public const int SquadFirstRules = 9;

        /// The campaign rules version that plays the raid chief (FightRules.RaidChief).
        public const int RaidChiefRules = 10;

        /// The campaign rules version that plays road combat (FightRules.RoadCombat) and the run of lives.
        public const int RoadCombatRules = 11;

        /// The campaign rules version that plays the road kit (FightRules.RoadKit) and the streak.
        public const int RoadKitRules = 12;

        /// The streak's cap (Jacob, 25 Oct 2026: "streak is 3 with multplier die loose the multiploier"; "yes to all 3": it
        /// multiplies a won fight's materials). Each fight won in a row climbs it, up to this; a lost one resets it.
        public const int StreakCap = 3;

        /// Fights won in a row this run, up to StreakCap: a won fight's materials are multiplied by it (rules 12).
        public int Streak { get; private set; }

        internal bool RestoreStreak(int streak)
        {
            if (streak < 0 || streak > StreakCap) return false;
            Streak = streak;
            return true;
        }

        /// Lives a run starts with from rules 11 (Jacob, 25 Oct 2026: "yea like hades death defiance bro"). A lost fight
        /// costs one; the run ends at none.
        public const int RunLives = 3;

        /// The lives this run has left (rules 11).
        public int Lives { get; private set; } = RunLives;

        internal bool RestoreLives(int lives)
        {
            if (lives < 0 || lives > RunLives) return false;
            Lives = lives;
            return true;
        }

        private readonly List<BondPair> _bonds = new List<BondPair>();

        /// Your bonded pairs this run (road combat, rules 11): they end when the run does.
        public IReadOnlyList<BondPair> Bonds => _bonds;

        internal bool RestoreBond(string originId, string casherId)
        {
            if (string.IsNullOrEmpty(originId) || string.IsNullOrEmpty(casherId) || HasBond(originId, casherId)) return false;
            _bonds.Add(new BondPair(originId, casherId));
            return true;
        }

        private bool HasBond(string originId, string casherId)
        {
            for (int i = 0; i < _bonds.Count; i++)
                if (_bonds[i].OriginId == originId && _bonds[i].CasherId == casherId) return true;
            return false;
        }

        // The bonds of this squad: both fighters of a pair must fight.
        private List<BondPair> BondsFor(List<UnitSpec> squad)
        {
            var pairs = new List<BondPair>();
            for (int i = 0; i < _bonds.Count; i++)
            {
                bool origin = false, casher = false;
                for (int k = 0; k < squad.Count; k++)
                {
                    if (squad[k].Id == _bonds[i].OriginId) origin = true;
                    if (squad[k].Id == _bonds[i].CasherId) casher = true;
                }
                if (origin && casher) pairs.Add(_bonds[i]);
            }
            return pairs;
        }

        /// True when this tribe's fight of this kind is a raid chief's under these rules: so far only Bloodridge's chief.
        public static bool IsRaidChief(CampaignTribe tribe, EncounterKind kind, int rules)
        {
            return rules >= RaidChiefRules && kind == EncounterKind.Chief && tribe != null && tribe.Id == CampaignTribes.BloodridgeId;
        }

        /// Since 10 Oct 2026: fights won on this tribe's path in the current run (there is no
        /// respect bar). Kept for screens that still say respect.
        public int Respect(string tribeId)
        {
            return Target != null && Target.Id == tribeId ? Step : 0;
        }

        /// Since 10 Oct 2026: how many fights come before this tribe's chief.
        public int RespectNeeded(string tribeId)
        {
            CampaignTribe t = CampaignTribes.Find(tribeId);
            return t == null || t.IsStarter ? 0 : RoadRows;
        }

        /// The tribe is with you this run.
        public bool HasJoined(string tribeId)
        {
            return _joined.Contains(tribeId);
        }

        /// Where the next run starts: the first tier's tribe it picks (NextRunTribe), once the tutorial is won.
        public List<CampaignTribe> Choosable()
        {
            var list = new List<CampaignTribe>();
            if (TutorialDone) list.Add(NextRunTribe);
            return list;
        }

        /// The tribe the next run starts at: one of the first tier's, picked at random for that run (Jacob, 18 Oct 2026:
        /// "random tribe per tier").
        public CampaignTribe NextRunTribe => RunTribe(null, RunsStarted + 1);

        /// The tribes this run meets, one per tier in order, or the next run's while at camp (18 Oct 2026). The hunters
        /// come after the last.
        public IReadOnlyList<CampaignTribe> RunRoute
        {
            get
            {
                int run = Stage == CampaignStage.InRun ? RunsStarted : RunsStarted + 1;
                var route = new List<CampaignTribe>();
                for (CampaignTribe t = RunTribe(null, run); t != null; t = RunTribe(t, run)) route.Add(t);
                return route;
            }
        }

        // The tribe a run meets after this one, or the first tier's for null (Jacob, 18 Oct 2026: "random tribe per tier").
        private CampaignTribe RunTribe(CampaignTribe after, int run)
        {
            bool legacy = Stage == CampaignStage.InRun && run == RunsStarted && RoutePickVersion == 0;
            return PickInTier(CampaignTribes.All, after == null ? int.MinValue : after.Tier, Seed, run, legacy);
        }

        /// The tribe a run meets in the lowest tier above floorTier: one of that tier's tribes, never the starter, picked
        /// by the campaign's seed and the run, so one run can meet a different tribe than the last and the same run always
        /// meets the same one. Null when no tier is left.
        internal static CampaignTribe PickInTier(IReadOnlyList<CampaignTribe> tribes, int floorTier, ulong seed, int run, bool legacy = false)
        {
            int tier = int.MaxValue;
            for (int i = 0; i < tribes.Count; i++)
                if (!tribes[i].IsStarter && tribes[i].Tier > floorTier && tribes[i].Tier < tier) tier = tribes[i].Tier;
            if (tier == int.MaxValue) return null;
            var inTier = new List<CampaignTribe>();
            for (int i = 0; i < tribes.Count; i++)
                if (!tribes[i].IsStarter && tribes[i].Tier == tier) inTier.Add(tribes[i]);
            return inTier[legacy ? (int)(CampaignRoad.Mix(seed, run, "tier." + tier.ToString(CultureInfo.InvariantCulture)) % (ulong)inTier.Count)
                : TierPick(inTier.Count, seed, run, tier)];
        }

        /// Which of a tier's tribes a run meets (Jacob, 18 Oct 2026: "random tribe per tier"): an index below
        /// tribesInTier, the same every time for the same campaign seed, run and tier.
        public static int TierPick(int tribesInTier, ulong seed, int run, int tier)
        {
            if (tribesInTier <= 1) return 0;
            ulong h = CampaignRoad.Mix(seed, run, "tier." + tier.ToString(CultureInfo.InvariantCulture));
            // Avalanche the high bits into the low bits before reducing to a small tier roster.
            // Raw FNV's low bit otherwise forces tiers 1 and 2 to pick opposite indices.
            unchecked
            {
                h ^= h >> 30;
                h *= 0xbf58476d1ce4e5b9UL;
                h ^= h >> 27;
                h *= 0x94d049bb133111ebUL;
                h ^= h >> 31;
            }
            return (int)(h % (ulong)tribesInTier);
        }

        /// Whether the Butcher can forge the tabard now: at camp, with the recipe known, not forged
        /// yet, and CampaignForge.TabardPrice materials.
        public bool CanForgeTabard(out string why)
        {
            why = string.Empty;
            if (Stage != CampaignStage.ChooseTribe) why = "The forge is at camp.";
            else if (!_tabardRecipe) why = "The Butcher does not know this recipe yet. Break the Bloodridge chief.";
            else if (_tabardForged) why = "The tabard is already forged.";
            else if (_materials < CampaignForge.TabardPrice)
                why = "The tabard costs " + CampaignForge.TabardPrice.ToString(CultureInfo.InvariantCulture) + " materials.";
            return why.Length == 0;
        }

        /// Pays CampaignForge.TabardPrice materials and forges the tabard, for good.
        public bool TryForgeTabard(out string why)
        {
            if (!CanForgeTabard(out why)) return false;
            _materials -= CampaignForge.TabardPrice;
            _tabardForged = true;
            Discover("Forged: " + CampaignForge.TabardName);
            return true;
        }

        /// Whether this fighter can put the tabard on now: it is forged, no fight is running, and the
        /// fighter is yours (at camp: one of the squad the next run starts with).
        public bool CanWearTabard(string fighterId, out string why)
        {
            why = string.Empty;
            if (!_tabardForged) why = "The tabard is not forged yet.";
            else if (_active != null) why = "Not in the middle of a fight.";
            else if (FindFighter(fighterId) == null) why = (fighterId ?? string.Empty) + " is not one of your fighters.";
            return why.Length == 0;
        }

        /// Hands the tabard to this fighter. They wear it until the run ends or they fall.
        public bool TryWearTabard(string fighterId, out string why)
        {
            if (!CanWearTabard(fighterId, out why)) return false;
            _tabardWearer = fighterId;
            return true;
        }

        /// Whether this piece on offer can be taken now: a won path fight offered it, no fight has started since, and
        /// the materials it banked are still at camp.
        public bool CanTakeSpoils(int choice, out string why)
        {
            why = string.Empty;
            if (_spoils.Count == 0) why = "No piece is on offer.";
            else if (choice < 0 || choice >= _spoils.Count) why = "That piece is not on offer.";
            else if (_active != null) why = "Not in the middle of a fight.";
            else if (_materials < _spoilsMaterials) why = "The materials it banked are already spent.";
            return why.Length == 0;
        }

        /// The first piece on offer, for screens built when a won fight offered one.
        public bool CanTakeSpoils(out string why)
        {
            return CanTakeSpoils(0, out why);
        }

        public bool TryTakeSpoils(out string why)
        {
            return TryTakeSpoils(0, out why);
        }

        /// Strip instead of bank (Jacob, 13 Oct 2026; one of three since 20 Oct 2026): gives back the materials the last
        /// won path fight banked and keeps the piece you chose in the run's gear. The other pieces are gone.
        public bool TryTakeSpoils(int choice, out string why)
        {
            if (!CanTakeSpoils(choice, out why)) return false;
            _materials -= _spoilsMaterials;
            _gear.Add(_spoils[choice]);
            _spoils.Clear();
            // From rules 19 the fight's Pal's picks were on the same card: taking a piece clears them too (fix 5).
            _favourOffer = null;
            _spoilsMaterials = 0;
            // Stripping a tribe's gear raises the bounty on your head (18 Oct 2026), and hunters may now wait ahead.
            _heat += BountyRules.HeatPerStrip;
            MakeOffers();
            return true;
        }

        /// Whether this favour on offer can be taken now: a won favour fight offered it, no fight has started since, and
        /// the materials it banked are still at camp.
        public bool CanTakeFavour(int choice, out string why)
        {
            why = string.Empty;
            if (_favourOffer == null) why = "No favour is on offer.";
            else if (choice < 0 || choice >= _favourOffer.Choices.Count) why = "That favour is not on offer.";
            else if (_active != null) why = "Not in the middle of a fight.";
            else if (_materials < _spoilsMaterials) why = "The materials it banked are already spent.";
            return why.Length == 0;
        }

        /// Takes a favour in place of the materials the last won favour fight banked (Jacob, 18 Oct 2026): a new one at
        /// rank 1, one the run holds a rank stronger. A favour is the Pal's gift, so it raises no bounty. A combo (29 Oct
        /// 2026) goes on a fighter of its class as a piece of the run's gear instead (TakeCombo).
        public bool TryTakeFavour(int choice, out string why)
        {
            if (!CanTakeFavour(choice, out why)) return false;
            Favour favour = _favourOffer.Choices[choice];
            // From rules 34 a duo is free: it leaves the card standing (Campaign.Duos.cs).
            if (FavourRules.IsDuo(favour) && DuoRules.IsMove(FavourRules.DuoOf(favour)) && DuoMovesRun)
            {
                TakeFreeDuo(choice);
                return true;
            }
            _materials -= _spoilsMaterials;
            string name = FavourRules.Name(favour);
            string pals = FavourRules.PalName(FavourRules.PalOf(favour));
            // A duo (rules 30, Campaign.Duos.cs).
            if (FavourRules.IsDuo(favour))
            {
                name = TakeDuo(favour);
                PalDuo duo = FavourRules.DuoOf(favour);
                pals = FavourRules.PalName(FavourRules.FirstOf(duo)) + " and " + FavourRules.PalName(FavourRules.SecondOf(duo));
            }
            else if (FavourRules.IsCombo(favour)) TakeCombo(favour);
            // Her road card (rules 24, Campaign.PalStores.cs).
            else if (FavourRules.IsWhisper(favour)) name = TakeWhisper(_favourOffer.Picks[choice]);
            // Hers (rules 28, Campaign.PalStores.cs).
            else if (FavourRules.IsShade(favour)) name = TakeShade(favour);
            // His (rules 29).
            else if (FavourRules.IsGlutton(favour)) name = TakeGlutton(favour);
            else
            {
                HeldFavour held = FindFavour(favour);
                if (held == null) _favours.Add(new HeldFavour(favour, 1));
                else held.Rank = Math.Min(FavourRules.MaxRank, held.Rank + 1);
            }
            _favourOffer = null;
            // From rules 19 the tribe's gear was on the same card: taking a pick clears it too (fix 5).
            _spoils.Clear();
            _spoilsMaterials = 0;
            Discover("Favour: " + name + " (" + pals + ")");
            return true;
        }

        private HeldFavour FindFavour(Favour favour)
        {
            for (int i = 0; i < _favours.Count; i++) if (_favours[i].Favour == favour) return _favours[i];
            return null;
        }

        // What a won favour fight on this road node offers under these campaign rules: its Pal's favours the run can still
        // take a rank of, in order, up to FavourRules.Offered. From ButcherComboRules the Butcher offers his combos instead:
        // three of those for a class in your squad that the run does not hold yet (DESIGN_LOCKS: "His fights never offer a
        // combo you already hold"), drawn from the node's own dice, so the same node always offers the same three (the flip:
        // "a random three from the bigger pool drawn from the node's own seed"), in his order. Null when there are none, and
        // then the fight drops gear as usual.
        private FavourOffer MakeFavourOffer(RoadNode node, int rules)
        {
            FavourOffer offer = MakePalOffer(node, rules);
            // From rules 30 a card can show a duo of its Pal as one more pick (Campaign.Duos.cs).
            return rules >= PalDuoRules ? WithDuos(node, offer, rules) : offer;
        }

        // The Pal's own picks on that card.
        private FavourOffer MakePalOffer(RoadNode node, int rules)
        {
            Pal pal = node.Pal;
            if (pal == Pal.None) return null;
            // From rules 24 the Whisperer offers her road card (Campaign.PalStores.cs).
            if (pal == Pal.Whisperer && rules >= WhispererRules) return WhisperOffer(node);
            // From rules 28 the Shade offers her road card (Campaign.PalStores.cs).
            if (pal == Pal.Shade && rules >= ShadeFormsRules) return ShadeOffer(node);
            // From rules 29 the Glutton offers his road card (Campaign.PalStores.cs).
            if (pal == Pal.Glutton && rules >= GluttonTreeRules) return GluttonOffer(node);
            var choices = new List<Favour>();
            if (pal == Pal.Butcher && rules >= ButcherComboRules)
            {
                IReadOnlyList<Favour> combos = FavourRules.Combos(pal);
                if (rules >= MarksRules) return WeightedComboOffer(node, pal, combos, rules);
                for (int i = 0; i < combos.Count; i++)
                {
                    ClassBend bend = FavourRules.ComboBend(combos[i]);
                    if (!HoldsCombo(bend) && SquadHas(ClassSpells.ClassOf(bend))) choices.Add(combos[i]);
                }
                var dice = new DeterministicRng(node.Seed, ComboOfferStream);
                while (choices.Count > OfferSize) choices.RemoveAt(dice.Next(choices.Count));
                return choices.Count == 0 ? null : new FavourOffer(pal, choices);
            }
            IReadOnlyList<Favour> all = FavourRules.Of(pal);
            for (int i = 0; i < all.Count && choices.Count < OfferSize; i++)
                if (FavourRank(all[i]) < FavourRules.MaxRank) choices.Add(all[i]);
            return choices.Count == 0 ? null : new FavourOffer(pal, choices);
        }

        // The road node's own dice for the Butcher's three combos: a stream of its seed nothing else draws from.
        private const ulong ComboOfferStream = 15UL;

        // From MarksRules (DESIGN_LOCKS, "The Butcher's theme"): every combo for a class in your squad that the run does not
        // hold at its top rank, by its ComboOfferWeight, three drawn by weight from the node's own dice, in his order.
        private FavourOffer WeightedComboOffer(RoadNode node, Pal pal, IReadOnlyList<Favour> combos, int rules = 0)
        {
            var pool = new List<Favour>();
            var weights = new List<int>();
            // From rules 33 only the node's flavour (Campaign.Vows.cs); a card with none of it left offers them all.
            bool anyOfFlavour = false;
            for (int i = 0; i < combos.Count && rules >= VowFlavourRules; i++)
            {
                ClassBend bend = FavourRules.ComboBend(combos[i]);
                if (FlavourAllows(node, bend, rules) && SquadHas(ClassSpells.ClassOf(bend)) && ComboOfferWeight(bend) > 0) anyOfFlavour = true;
            }
            for (int i = 0; i < combos.Count; i++)
            {
                ClassBend bend = FavourRules.ComboBend(combos[i]);
                int weight = SquadHas(ClassSpells.ClassOf(bend)) ? ComboOfferWeight(bend) : 0;
                if (anyOfFlavour && !FlavourAllows(node, bend, rules)) weight = 0;
                if (weight <= 0) continue;
                pool.Add(combos[i]);
                weights.Add(weight);
            }
            var dice = new DeterministicRng(node.Seed, ComboOfferStream);
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
            return choices.Count == 0 ? null : new FavourOffer(pal, choices);
        }

        /// How much this combo weighs in the Butcher's offer from campaign rules 18 (DESIGN_LOCKS, "The Butcher's theme"): 1,
        /// and one more for each combo of its flavour the run holds; a combo the run holds weighs 2 while it can still rank
        /// up, and 0 at MarkRules.MaxStacks, so it is never offered again.
        public int ComboOfferWeight(ClassBend bend)
        {
            CampaignGear held = ComboPiece(bend);
            if (held != null) return held.Rank >= MarkRules.MaxStacks ? 0 : ComboRankUpWeight;
            Augment flavour = ClassSpells.AugmentOf(bend);
            int weight = 1;
            for (int i = 0; i < _gear.Count; i++)
                if (ClassSpells.IsCombo(_gear[i].Bend) && ClassSpells.AugmentOf(_gear[i].Bend) == flavour) weight++;
            return weight;
        }

        /// What a combo the run holds weighs in his offer while it can still rank up. A starting value.
        public const int ComboRankUpWeight = 2;

        /// Whether winning this road fight now would offer its Pal's picks, for the road's preview, under the rules the next
        /// fight plays: false for a gear fight, and for a Pal fight whose Pal has nothing left to give, which drops gear.
        public bool PalPickOffered(RoadNode node)
        {
            if (node == null || node.Kind != RoadNodeKind.Fight || node.Pal == Pal.None) return false;
            int rules = Math.Min(ResumeRulesVersion < 0 ? RulesVersion : ResumeRulesVersion, _tuning.RulesCap);
            return MakeFavourOffer(node, rules) != null;
        }

        // True when a piece of the run's gear carries this combo.
        private bool HoldsCombo(ClassBend bend)
        {
            return ComboPiece(bend) != null;
        }

        // The run's piece that carries this combo, or null.
        private CampaignGear ComboPiece(ClassBend bend)
        {
            for (int i = 0; i < _gear.Count; i++) if (_gear[i].Bend == bend) return _gear[i];
            return null;
        }

        /// The Pals the run holds a pick from, one bit a Pal (1 << (int)pal): a combo for the Butcher, a favour for any.
        public int HeldPals()
        {
            int held = 0;
            for (int i = 0; i < _gear.Count; i++) if (ClassSpells.IsCombo(_gear[i].Bend)) held |= 1 << (int)Pal.Butcher;
            for (int i = 0; i < _favours.Count; i++) held |= 1 << (int)FavourRules.PalOf(_favours[i].Favour);
            // From rules 28 a route she gave counts as a pick of hers.
            if (ShadeFormsRun && _routes.Count > 0) held |= 1 << (int)Pal.Shade;
            // From rules 29 a talent he lent counts as a pick of his.
            if (GluttonRun && _talents.Count > 0) held |= 1 << (int)Pal.Glutton;
            return held;
        }

        /// The Pals the run held a pick from when its road to this tribe was made (HeldPals), 0 for none: from campaign rules
        /// 18 the road's Pal fights lean toward them (FavourRules.PalWeight). The first road of a run has none, so it is made
        /// as before; kept with the run so the same road comes back when it loads.
        public int RoadPals { get; internal set; }

        // True when a fighter of this class is in the run's squad.
        private bool SquadHas(UnitClass unitClass)
        {
            for (int i = 0; i < _reserve.Count; i++) if (_reserve[i].Class == unitClass) return true;
            return false;
        }

        // A combo taken on the road (29 Oct 2026): a piece of the run's gear with the combo as its bend and no stat, from the
        // road's tribe. The first fighter of its class in the squad who wears no combo yet puts it on at once; their old piece
        // stays in the run's gear, so TryEquip can swap them back. It goes when the run does, with the rest of the run's gear.
        private void TakeCombo(Favour favour)
        {
            ClassBend bend = FavourRules.ComboBend(favour);
            // From rules 18 a combo the run holds can be offered again: taking it ranks it up (DESIGN_LOCKS, "The Butcher's
            // theme"), so it lays more of its mark at once.
            CampaignGear held = ComboPiece(bend);
            if (held != null)
            {
                held.Rank = Math.Min(MarkRules.MaxStacks, held.Rank + 1);
                return;
            }
            UnitClass unitClass = ClassSpells.ClassOf(bend);
            var piece = new CampaignGear(ComboGearId(bend), Target == null ? CampaignTribes.ClarthId : Target.Id, GearTier.Pauper,
                GearStat.Damage, GearSpecial.None, FavourRules.Name(favour), (int)unitClass, bend);
            _gear.Add(piece);
            for (int i = 0; i < _reserve.Count; i++)
            {
                CampaignFighter f = _reserve[i];
                if (f.Class != unitClass) continue;
                CampaignGear worn = FindGear(f.GearId);
                if (worn != null && ClassSpells.IsCombo(worn.Bend)) continue;
                f.GearId = piece.Id;
                return;
            }
        }

        /// The id of the run's piece for a combo: a run holds each combo once.
        public static string ComboGearId(ClassBend bend)
        {
            return "combo." + ((int)bend).ToString(CultureInfo.InvariantCulture);
        }

        /// Puts a piece of the run's gear on this fighter, taking it off whoever wore it. One slot: their old
        /// piece goes back to the run's gear. Not in the middle of a fight.
        public bool TryEquip(string fighterId, string gearId, out string why)
        {
            why = string.Empty;
            CampaignFighter f = FindFighter(fighterId);
            CampaignGear piece = FindGear(gearId);
            if (_active != null) why = "Not in the middle of a fight.";
            else if (f == null) why = (fighterId ?? string.Empty) + " is not one of your fighters.";
            else if (piece == null) why = "That piece is not in the run's gear.";
            // A class piece fits only its own class (Jacob, 20 Oct 2026).
            else if (!piece.Fits(f.Class)) why = "Only a " + SliceClasses.Name((UnitClass)piece.ClassIndex) + " can wear that.";
            if (why.Length > 0) return false;
            for (int i = 0; i < _reserve.Count; i++)
            {
                if (_reserve[i].GearId == piece.Id) _reserve[i].GearId = string.Empty;
                // Out of a second slot too (rules 23).
                if (_reserve[i].GearId2 == piece.Id) _reserve[i].GearId2 = string.Empty;
            }
            f.GearId = piece.Id;
            return true;
        }

        /// Takes this fighter's piece off. It stays in the run's gear.
        public bool TryUnequip(string fighterId, out string why)
        {
            why = string.Empty;
            CampaignFighter f = FindFighter(fighterId);
            if (_active != null) why = "Not in the middle of a fight.";
            else if (f == null) why = (fighterId ?? string.Empty) + " is not one of your fighters.";
            if (why.Length > 0) return false;
            f.GearId = string.Empty;
            return true;
        }

        /// Formation (Jacob, 13 Oct 2026): stands this fighter in the front or the back row from the next fight
        /// on. Not in the middle of a fight.
        public bool TrySetRow(string fighterId, Row row, out string why)
        {
            why = string.Empty;
            CampaignFighter f = FindFighter(fighterId);
            if (_active != null) why = "Not in the middle of a fight.";
            else if (f == null) why = (fighterId ?? string.Empty) + " is not one of your fighters.";
            else if (row != Row.Front && row != Row.Back) why = "A fighter stands in the front or the back row.";
            if (why.Length > 0) return false;
            f.Row = row;
            return true;
        }

        /// Picks or drops an omen (Jacob, 13 Oct 2026). Only at camp, before a run.
        public bool TrySetOmen(Omen omen, bool picked, out string why)
        {
            why = string.Empty;
            if (!OmenRules.IsOmen((int)omen)) why = "There is no such omen.";
            else if (Stage != CampaignStage.ChooseTribe) why = "Omens are picked at camp, before a run.";
            if (why.Length > 0) return false;
            if (!picked) _omens.Remove(omen);
            else if (!_omens.Contains(omen))
            {
                _omens.Add(omen);
                _omens.Sort();
            }
            return true;
        }

        /// Takes an event node you can reach and makes one of its choices (Jacob, 13 Oct 2026): pay its cost, get what
        /// it gives, and stand on that node, so the run moves on and the spoils offer goes. False, with why, if the
        /// event or the choice cannot be taken.
        public bool TryTakeEvent(int lane, int choice, out EventOutcome outcome, out string why)
        {
            outcome = null;
            why = string.Empty;
            RoadNode node = null;
            if (Stage != CampaignStage.InRun) why = "No run is going.";
            else if (_active != null) why = "Not in the middle of a fight.";
            else
            {
                foreach (RoadNode n in NextNodes) if (n.Lane == lane && n.Kind == RoadNodeKind.Event) node = n;
                if (node == null) why = "There is no event you can reach in that lane.";
                else if (choice < 0 || choice >= node.Event.Choices.Count) why = "That event has no such choice.";
                else if (_materials < node.Event.Choices[choice].Cost)
                    why = "That costs " + node.Event.Choices[choice].Cost.ToString(CultureInfo.InvariantCulture) + " materials.";
            }
            if (why.Length > 0) return false;
            RoadChoice pick = node.Event.Choices[choice];
            outcome = new EventOutcome(node, pick);
            _materials -= pick.Cost;
            outcome.Materials = -pick.Cost;
            switch (pick.Effect)
            {
                case EventEffect.TakeInStraggler:
                {
                    IReadOnlyList<UnitClass> roster = CampaignTribes.Clarth.Roster;
                    CampaignFighter f = NewFighter(CampaignTribes.ClarthId, roster[(int)(node.Seed / 4UL % (ulong)roster.Count)], null);
                    _reserve.Add(f);
                    outcome.Fighter = f;
                    outcome.Text = f.Name + " walks with you now.";
                    break;
                }
                case EventEffect.BuyPauper:
                case EventEffect.BuyMid:
                {
                    CampaignGear piece = EventGear(node, pick.Effect == EventEffect.BuyMid ? GearTier.Mid : GearTier.Pauper);
                    _gear.Add(piece);
                    outcome.Gear = piece;
                    outcome.Text = "You bought " + piece.Name + ".";
                    break;
                }
                case EventEffect.Pray:
                    // The node's own seed decides, so the same shrine always answers the same way.
                    if (node.Seed / 16UL % 2UL == 0UL)
                    {
                        CampaignGear gift = EventGear(node, GearTier.Mid);
                        _gear.Add(gift);
                        outcome.Gear = gift;
                        outcome.Text = "Something answered. " + gift.Name + " lies in the bowl.";
                    }
                    else
                    {
                        int lost = Math.Min(RoadEvents.PrayLoss, _materials);
                        _materials -= lost;
                        outcome.Materials -= lost;
                        outcome.Text = "Something answered, and took " + lost.ToString(CultureInfo.InvariantCulture) + " materials.";
                    }
                    break;
                default:
                    outcome.Text = "You walk on.";
                    break;
            }
            _spoils.Clear();
            _favourOffer = null;
            _spoilsMaterials = 0;
            Step = node.Row + 1;
            _roadLane = node.Lane;
            MakeOffers();
            KeepTabardWorn();
            return true;
        }

        /// The tutorial (Jacob, 8 Oct 2026, correction 6): the Clarth rescued and jailed you;
        /// Bloodridge came to take you; command the Clarth and win, and they are yours.
        public CampaignFight StartTutorial()
        {
            if (Stage != CampaignStage.Tutorial) throw new InvalidOperationException("The tutorial is behind you.");
            if (_active != null) throw new InvalidOperationException("A fight is already running.");
            var squad = new List<CampaignFighter>();
            for (int i = 0; i < CampaignTribes.Clarth.Roster.Count; i++)
                squad.Add(new CampaignFighter("tutorial." + i.ToString(CultureInfo.InvariantCulture), CampaignTribes.ClarthId,
                    CampaignTribes.Clarth.Roster[i], CampaignNames.Roster(Seed, CampaignTribes.ClarthId, i), Quirk.None));
            Encounter encounter = MakeEncounter(EncounterKind.Tutorial, CampaignTribes.Bloodridge);
            RememberFightStart(-1, squad);
            BattleSetup setup = BuildSetup(encounter, squad, true, out List<CampaignGear> worn, out int rules);
            _active = new CampaignFight(encounter, setup, squad, FightAssist(encounter, true), EnemyNames(encounter, setup))
            {
                Worn = worn,
                RulesVersion = rules
            };
            return _active;
        }

        /// Leaves camp on a new run, at the first tier's tribe this run picks (NextRunTribe).
        public void StartRun()
        {
            ChooseTribe(NextRunTribe.Id);
        }

        /// Leaves camp on a new run that starts at this tribe. The game always starts at the first
        /// tier (StartRun); tools and tests may start deeper.
        public void ChooseTribe(string tribeId)
        {
            if (Stage != CampaignStage.ChooseTribe) throw new InvalidOperationException("A run can only start from camp.");
            CampaignTribe tribe = CampaignTribes.Find(tribeId);
            if (tribe == null || tribe.IsStarter) throw new ArgumentException("A run cannot go after " + tribeId + ".");
            RunsStarted++;
            RoutePickVersion = 1;
            ClearRoadRun = _tuning.RulesCap >= ClearRoadRules && _tuning.PathFights >= 3;
            PalRoadRun = Math.Min(RulesVersion, _tuning.RulesCap) >= PalRoadRules;
            ClearGearAndGrudges();
            // A run starts with all its lives, no bonds (rules 11) and no streak (rules 12).
            Lives = RunLives;
            _bonds.Clear();
            Streak = 0;
            ClearRunHunt();
            TopUpReserve();
            // Thin Ranks: the run starts one fighter short.
            if (HasOmen(Omen.ThinRanks))
                while (_reserve.Count > Math.Max(MinSquad, Math.Max(3, _tuning.TopUpTo) - 1)) _reserve.RemoveAt(_reserve.Count - 1);
            KeepTabardWorn();
            // The beasts wait beside their roads again, and the beast pieces the Butcher forged come along (CampaignBeast.cs).
            _beastsBeaten.Clear();
            ArmBeastPieces();
            Target = tribe;
            Step = 0;
            _lastKind = -1;
            RoadPals = 0;
            _road = MakeRoad(tribe);
            _roadLane = -1;
            _featsThisRun.Clear();
            Discover("Tribe: " + tribe.Name);
            Stage = CampaignStage.InRun;
            MakeOffers();
        }

        /// Starts the offered encounter. Pass squad ids, or null for the default squad.
        public CampaignFight StartEncounter(int offerIndex, IReadOnlyList<string> squadIds = null)
        {
            if (Stage != CampaignStage.InRun) throw new InvalidOperationException("No run is going.");
            if (_active != null) throw new InvalidOperationException("A fight is already running.");
            if (offerIndex < 0 || offerIndex >= _offers.Count) throw new ArgumentOutOfRangeException(nameof(offerIndex));
            Encounter encounter = _offers[offerIndex];
            List<CampaignFighter> squad = squadIds == null ? DefaultSquad(encounter.SquadSize) : PickSquad(squadIds, encounter.SquadSize);
            _spoils.Clear();
            _favourOffer = null;
            _spoilsMaterials = 0;

            // In a duel the leader only gives orders, so no chief can be called to it either.
            bool leaderFights = encounter.Kind != EncounterKind.Duel;
            RememberFightStart(offerIndex, squad);
            BattleSetup setup = BuildSetup(encounter, squad, leaderFights, out List<CampaignGear> worn, out int rules);
            _active = new CampaignFight(encounter, setup, squad, FightAssist(encounter, leaderFights), EnemyNames(encounter, setup))
            {
                Worn = worn,
                RulesVersion = rules,
                MetBefore = encounter.Kind == EncounterKind.Chief && MetChief(encounter.Tribe.Id)
            };
            return _active;
        }

        /// For CampaignSave: puts a saved run back on the same road and node, so the same offers (13 Oct 2026: offers
        /// come from the road). Call after Settle. False if that node is not on the road.
        internal bool ResumeRun(CampaignTribe target, int step, int lastKind, IReadOnlyList<Feat> feats, int roadLane)
        {
            Target = target;
            Step = step;
            _lastKind = lastKind;
            _featsThisRun.Clear();
            for (int i = 0; i < feats.Count; i++) _featsThisRun.Add(feats[i]);
            _road = MakeRoad(target);
            _roadLane = roadLane;
            Stage = CampaignStage.InRun;
            if (roadLane >= 0 && (_road == null || step < 1 || _road.Node(step - 1, roadLane) == null)) return false;
            MakeOffers();
            return true;
        }

        // Before BuildSetup moves the fight counter and the seed stream on.
        private void RememberFightStart(int offer, IReadOnlyList<CampaignFighter> squad)
        {
            _fightSeedState = _seeds.State;
            _fightSeedIncrement = _seeds.Increment;
            _fightCounterAtStart = _fightCounter;
            _fightOffer = offer;
            _fightSquad.Clear();
            for (int i = 0; i < squad.Count; i++) _fightSquad.Add(squad[i].Id);
        }

        /// Up to size fighters from the reserve, a mix of classes first.
        public List<CampaignFighter> DefaultSquad(int size)
        {
            var squad = new List<CampaignFighter>();
            UnitClass[] order = size == 1
                ? new[] { UnitClass.Warden, UnitClass.Skirmisher, UnitClass.Marksman, UnitClass.Trapper, UnitClass.Medic }
                : new[] { UnitClass.Warden, UnitClass.Medic, UnitClass.Skirmisher, UnitClass.Marksman, UnitClass.Trapper };
            for (int o = 0; o < order.Length && squad.Count < size; o++)
            {
                for (int i = 0; i < _reserve.Count; i++)
                {
                    if (_reserve[i].Class == order[o] && !squad.Contains(_reserve[i]))
                    {
                        squad.Add(_reserve[i]);
                        break;
                    }
                }
            }
            for (int i = 0; i < _reserve.Count && squad.Count < size; i++)
                if (!squad.Contains(_reserve[i])) squad.Add(_reserve[i]);
            return squad;
        }

        /// Whether a finished fight counts as won, by the campaign's own rule (the one Finish applies).
        /// False while it runs. The screen asks this instead of keeping its own copy (11 Oct 2026).
        public bool FightWon(CampaignFight fight)
        {
            if (fight == null || !fight.Sim.IsFinished) return false;
            return Outcome(fight.Encounter.Kind, fight.Sim.Winner, fight.Sim.BrokenSide, fight.Sim.Ending.How == FightEnd.TimeUp);
        }

        /// Running out of time loses, except Hold the line, which is won by still standing.
        private bool Outcome(EncounterKind kind, int winner, int brokenSide, bool timedOut)
        {
            bool won = winner == 0;
            if (kind == EncounterKind.HoldTheLine)
            {
                if (!won && brokenSide != 0 && timedOut) won = true;
            }
            else if (kind == EncounterKind.Beast)
            {
                // The fight already says who holds the field when the clock runs out: the shark dives away and leaves it to you,
                // the cage bear holds its ground.
            }
            else if (timedOut && _tuning.TimeOutLoses)
            {
                won = false;
            }
            return won;
        }

        // Everyone's names in a fight by unit id, kept with it in the archive so a replay names them too.
        private static Dictionary<string, string> NamesIn(CampaignFight fight)
        {
            var names = new Dictionary<string, string>(StringComparer.Ordinal);
            for (int side = 0; side < 2; side++)
            {
                IReadOnlyList<UnitSpec> units = fight.Setup.Side(side);
                for (int i = 0; i < units.Count; i++)
                {
                    string name = fight.NameOf(units[i].Id);
                    if (name.Length > 0) names[units[i].Id] = name;
                }
            }
            return names;
        }

        /// Closes the running fight: archives it, both sides learn and the dead leave. A win sends
        /// materials home and moves the run along, with a piece of their gear on offer in place of the
        /// materials after a path fight. A captured chief brings his tribe and his order, a killed one his
        /// Princess piece, and either way the run goes to the next tier, or after the last chief to the
        /// Sovereign's hunters. A loss costs the fallen and one of yours captured, and the step is tried again.
        /// The run ends back at camp when the hunters break or you cannot field MinSquad.
        public EncounterResult Finish(CampaignFight fight)
        {
            if (fight == null) throw new ArgumentNullException(nameof(fight));
            if (!ReferenceEquals(fight, _active)) throw new ArgumentException("That is not the running fight.");
            if (!fight.Sim.IsFinished) throw new InvalidOperationException("The fight has not finished.");
            _active = null;

            Archive.Add(fight.Setup, fight.Session.Log, fight.Sim, NamesIn(fight));
            FightRecord record = Archive.Find(fight.Setup.FightId).Record;
            Encounter encounter = fight.Encounter;
            bool hunt = encounter.Kind == EncounterKind.AlphaHunt;
            bool ambush = encounter.Kind == EncounterKind.Ambush;
            bool beast = encounter.Kind == EncounterKind.Beast;
            CampaignHunter hunter = encounter.Hunter;
            if (ambush) _ambushFights++;
            if (beast) _beastFights++;
            bool timedOut = EndedOnTime(record);
            bool won = Outcome(encounter.Kind, record.Winner, fight.Sim.BrokenSide, timedOut);
            // From rules 34 the duos' streak comes back from the fight, and a lost one drops it (Campaign.Duos.cs).
            if (fight.RulesVersion >= DuoMovesRules) StreakAfter(fight, won);

            var result = new EncounterResult
            {
                Encounter = encounter, Record = record, Won = won, TimedOut = timedOut, EclipseWays = Feats.EclipseWays(record.Events),
                Hunter = hunter, LadderRankBefore = _company.PlayerRank, LadderRankAfter = _company.PlayerRank, BountyBefore = Bounty,
                BountyAfter = Bounty
            };

            // The hunters' habits never change (call J2): they grow through their company instead (PromoteHunter).
            // Nobody learns from the tutorial (Jacob, 13 Oct 2026: "fix all 9"): its orders are the coach's, not yours.
            bool practice = encounter.Kind == EncounterKind.Tutorial;
            // A beast is no tribe and teaches yours nothing about tribes (Battle.BeastKits, 21 Oct 2026).
            if (hunt || ambush || practice || beast) result.EnemyLessons = NoLessons;
            else result.EnemyLessons = LessonRules.Learn(LearnerMind(encounter, record.SquadFirst), record, 1);
            result.CultLessons = practice || beast ? NoLessons : LessonRules.Learn(CultMind, record, 0);

            // From rules 11 your whole squad comes back every fight, gear and all: a death only costs you inside it (Jacob,
            // 25 Oct 2026).
            bool lives = fight.RulesVersion >= RoadCombatRules;
            var fallen = new List<CampaignFighter>();
            for (int i = 0; i < fight.Sim.Units.Count; i++)
            {
                BattleUnit u = fight.Sim.Units[i];
                if (u.Side != 0 || u.Alive || u.Role != UnitRole.Fighter) continue;
                CampaignFighter f = FindFighter(u.Id);
                if (f == null) continue;
                fallen.Add(f);
                if (lives) continue;
                _reserve.Remove(f);
                // What they wore goes back to the run's gear.
                f.GearId = string.Empty;
            }
            result.Fallen = fallen;
            // Pairs that bonded in this fight stay bonded for the run (Battle.BattleSimulation.Road.cs).
            if (lives)
                for (int i = 0; i < fight.Sim.Events.Count; i++)
                {
                    BattleEvent e = fight.Sim.Events[i];
                    if (e.Kind == BattleEventKind.Bond && e.ActorSide == 0 && !HasBond(e.TargetId, e.ActorId))
                        _bonds.Add(new BondPair(e.TargetId, e.ActorId));
                }
            result.JoinedFighters = new List<CampaignFighter>();
            result.Awards = new List<FeatAward>();

            // The tabard's wearer fell: someone else picks it up before the next fight.
            if (_tabardWearer.Length > 0 && FindFighter(_tabardWearer) == null) _tabardWearer = string.Empty;

            if (encounter.Kind == EncounterKind.Tutorial)
            {
                if (!won) return result;
                var joined = new List<CampaignFighter>();
                for (int i = 0; i < fight.Squad.Count; i++)
                {
                    BattleUnit u = fight.Sim.FindById(fight.Squad[i].Id);
                    if (u != null && u.Alive) joined.Add(NewFighter(CampaignTribes.ClarthId, fight.Squad[i].Class, fight.Squad[i].Name));
                }
                _reserve.AddRange(joined);
                result.JoinedFighters = joined;
                if (!_joined.Contains(CampaignTribes.ClarthId)) _joined.Add(CampaignTribes.ClarthId);
                TopUpReserve();
                TutorialDone = true;
                result.TutorialWon = true;
                result.Joined = true;
                Discover("Joined: the Clarth");
                Stage = CampaignStage.ChooseTribe;
                return result;
            }

            // The beast beside the road: its trophies, its scars, and the road where you left it (CampaignBeast.cs).
            if (beast) return FinishBeast(record, result, won);

            CampaignTribe tribe = encounter.Tribe;
            result.RespectNeeded = RoadRows;
            result.RespectBefore = Step;

            // His codex page (Jacob, 20 Oct 2026, "Chief fights"): you have met him now, and the hidden chain you found
            // against him fills in its blank.
            if (encounter.Kind == EncounterKind.Chief)
            {
                Discover(MetEntry(tribe));
                string chain = ChiefKits.ChainName(tribe.Kit);
                if (chain.Length > 0 && Happened(record, BattleEventKind.Combo, chain))
                {
                    result.ChainFound = chain;
                    Discover(ChainEntry(tribe, chain));
                }
            }
            // The hunt's hidden chain fills the hunt captain's blank the same way, win or lose (Battle.ChiefKit.Hunt).
            if (hunt && fight.Sim.Kit(1) == ChiefKit.Hunt && Happened(record, BattleEventKind.Combo, ChiefKits.TurnedHunt))
            {
                result.ChainFound = ChiefKits.TurnedHunt;
                Discover(ChainEntry(AlphaHunt.Tribe, ChiefKits.TurnedHunt));
            }

            if (!won && lives)
            {
                // From rules 11 a lost fight costs a life (Jacob, 25 Oct 2026: "yea like hades death defiance bro"), nobody of
                // yours is captured, and you try this step again. Your Grudge fighters hit the tribe that took it harder for the
                // rest of the run ("yes to all 3", 25 Oct). The run ends with no lives left.
                Lives = Math.Max(0, Lives - 1);
                result.LivesLeft = Lives;
                // From rules 24 each of yours who fell loses his top rank (DESIGN_LOCKS, "The Whisperer's core").
                if (fight.RulesVersion >= WhispererRules) result.RanksLost = DropRanks(fallen);
                // The streak is lost with the life (rules 12).
                // From rules 29 Branch Hedge keeps it through a loss a run (Campaign.PalStores.cs).
                if (fight.RulesVersion >= RoadKitRules && !(fight.RulesVersion >= GluttonTreeRules && Hedged())) Streak = 0;
                string taker = hunt || ambush ? AlphaHunt.Id : tribe.Id;
                if (!_grudges.Contains(taker)) _grudges.Add(taker);
                if (hunter != null) PromoteHunter(hunter, record, result);
                result.LadderRankAfter = _company.PlayerRank;
                result.RespectBeforeHalving = result.RespectAfter = Step;
                result.FightersLeft = _reserve.Count;
                if (Lives <= 0)
                {
                    result.RunOver = true;
                    EndRun();
                    return result;
                }
                if (encounter.Kind != EncounterKind.Chief && !hunt && !ambush) _lastKind = (int)encounter.Kind;
                MakeOffers();
                KeepTabardWorn();
                return result;
            }

            if (!won)
            {
                // Your roster is your lives (Jacob, 13 Oct 2026): the fallen stay down, the winners capture your
                // most hurt survivor by name, and you try this step again: fresh fights on a path, or the same
                // chief or hunters. The run ends once you cannot field MinSquad.
                CampaignFighter captured = MostHurtSurvivor(fight);
                if (captured != null)
                {
                    _reserve.Remove(captured);
                    // The winners take what they wore with them.
                    CampaignGear taken = FindGear(captured.GearId);
                    if (taken != null) _gear.Remove(taken);
                    captured.GearId = string.Empty;
                    // From rules 24 his second piece too, and his rank (DESIGN_LOCKS, "The Whisperer's core").
                    if (fight.RulesVersion >= WhispererRules) Captured(captured);
                    if (tribe.NoPrisoners)
                    {
                        // A tribe that takes no prisoners kills them instead (the sea tribe, 18 Oct 2026). Nobody was
                        // captured, so no grudge.
                        result.Slain = captured;
                        Discover("Slain by " + tribe.Name + ": " + captured.Name);
                    }
                    else
                    {
                        result.Captured = captured;
                        // Your Grudge fighters hit this tribe harder for the rest of the run.
                        if (!_grudges.Contains(tribe.Id)) _grudges.Add(tribe.Id);
                    }
                }
                // A hunter who beats you climbs the ladder and learns against your favourite order.
                if (hunter != null) PromoteHunter(hunter, record, result);
                result.LadderRankAfter = _company.PlayerRank;
                result.RespectBeforeHalving = result.RespectAfter = Step;
                result.FightersLeft = _reserve.Count;
                if (_reserve.Count < MinSquad)
                {
                    result.RunOver = true;
                    EndRun();
                    return result;
                }
                if (encounter.Kind != EncounterKind.Chief && !hunt && !ambush) _lastKind = (int)encounter.Kind;
                MakeOffers();
                KeepTabardWorn();
                return result;
            }

            // A chief, and the hunt at the end of the run, send home a chief's materials; a named hunter adds the reward
            // for their rank on the ladder (HunterRules.Reward), and an ambush pays that alone.
            int materials = encounter.Kind == EncounterKind.Chief || hunt ? _tuning.MaterialsPerChief : ambush ? 0 : _tuning.MaterialsPerFight;
            if (hunter != null) materials += HunterRules.Reward(_company.Rank(hunter));
            // Each omen picked adds to what the run earns (Jacob, 13 Oct 2026).
            materials = materials * (100 + OmenRules.MaterialsPercentEach * _omens.Count) / 100;
            // From rules 29 Rogue Planet Prospecting sends home more (Campaign.PalStores.cs).
            if (fight.RulesVersion >= GluttonTreeRules) materials = Prospected(materials);
            // A chief with his own kit, and from rules 6 the hunt, sends home more for a better grade (Battle.ChiefKits).
            bool graded = (encounter.Kind == EncounterKind.Chief || hunt) && fight.Sim.GloryOn(0);
            if (graded) materials = materials * (100 + ChiefKits.GradeMaterialsPercent * (int)fight.Sim.GloryRankOf(0)) / 100;
            // From rules 12 each fight won in a row multiplies what it sends home, up to StreakCap times.
            if (fight.RulesVersion >= RoadKitRules)
            {
                Streak = Math.Min(StreakCap, Streak + 1);
                materials *= Streak;
                result.Streak = Streak;
            }
            _materials += materials;
            result.MaterialsEarned = materials;

            // You climb past the hunter you beat; one who died is gone for good.
            if (hunter != null) BeatHunter(hunter, fight, result);

            if (ambush)
            {
                // You stand on the node the hunters took. No spoils: the company carries nothing worth stripping.
                Step = encounter.Node.Row + 1;
                _roadLane = encounter.Node.Lane;
                result.RespectBeforeHalving = result.RespectAfter = Step;
                result.ChiefReady = Step >= RoadRows;
                result.FightersLeft = _reserve.Count;
                MakeOffers();
                KeepTabardWorn();
                return result;
            }

            if (hunt)
            {
                // Breaking the hunters wins the run (Jacob, 11 Oct 2026, call J2). They never join. From rules 6 the grade
                // card too, and the hunt captain's codex page keeps your best.
                if (graded)
                {
                    result.Graded = true;
                    result.Grade = fight.Sim.GloryRankOf(0);
                    result.NewBestGrade = RecordGrade(AlphaHunt.Tribe, result.Grade);
                }
                result.RespectBeforeHalving = result.RespectAfter = Step;
                result.FightersLeft = _reserve.Count;
                result.RunOver = true;
                result.RunWon = true;
                RunsWon++;
                EndRun();
                return result;
            }

            if (encounter.Kind == EncounterKind.Chief)
            {
                // Capture or kill (Jacob, 13 Oct 2026; this replaces call J4 of 11 Oct). His nerve broke while he
                // still stood: his whole band from this fight joins you, fallen ones too, under the names they
                // fought with (12 Oct 2026), and his order is yours. He fell: no tribe and no order, but you take
                // his Princess piece.
                BattleUnit chief = null;
                for (int i = 0; i < fight.Sim.Units.Count; i++)
                    if (fight.Sim.Units[i].Side == 1 && fight.Sim.Units[i].Role == UnitRole.Chief) chief = fight.Sim.Units[i];
                if (chief != null && chief.Alive)
                {
                    var joined = new List<CampaignFighter>();
                    for (int i = 0; i < fight.Sim.Units.Count; i++)
                    {
                        BattleUnit u = fight.Sim.Units[i];
                        // A reserve of his who never came into the fight is not of his band from it (Battle.ChiefFight).
                        if (u.Side == 1 && u.Role == UnitRole.Fighter && !u.Benched) joined.Add(NewFighter(tribe.Id, u.Class, fight.NameOf(u.Id)));
                    }
                    // A raid chief fought alone (rules 10): his tribe's chief fight lineup joins you, under fresh names.
                    if (joined.Count == 0 && fight.Setup.Fight.RaidChief)
                    {
                        IReadOnlyList<UnitClass> lineup = tribe.Lineup(EncounterKind.Chief, encounter.Squad);
                        for (int i = 0; i < encounter.EnemyFighters && lineup.Count > 0; i++)
                            joined.Add(NewFighter(tribe.Id, lineup[i % lineup.Count], string.Empty));
                    }
                    _reserve.AddRange(joined);
                    result.JoinedFighters = joined;
                    if (!_joined.Contains(tribe.Id)) _joined.Add(tribe.Id);
                    var assist = new Assist(tribe.Id, tribe.AssistName, tribe.ChiefName, tribe.AssistGrade);
                    _assists.Add(assist);
                    result.AssistUnlocked = assist;
                    result.Joined = true;
                    result.ChiefCaptured = true;
                    Discover("Joined: " + tribe.Name);
                    Discover("Assist: " + tribe.AssistName + " (" + tribe.ChiefName + ")");
                }
                else
                {
                    CampaignGear piece = MakePrincess(tribe, fight.Setup.FightId);
                    _gear.Add(piece);
                    result.PrincessTaken = piece;
                    result.ChiefKilled = true;
                    // Executed on his knees, for that run only: the same chief leads his tribe next run.
                    result.ChiefExecuted = Happened(record, BattleEventKind.Execute, string.Empty);
                }
                // The grade card, and his codex page keeps your best (Battle.ChiefKits).
                if (graded)
                {
                    result.Graded = true;
                    result.Grade = fight.Sim.GloryRankOf(0);
                    result.NewBestGrade = RecordGrade(tribe, result.Grade);
                }
                result.RespectBeforeHalving = result.RespectAfter = Step;

                // The Pal who broke a chief learns his gear (call J1). Only Bloodridge's is designed:
                // the Lava Sun Tabard, for the Butcher.
                if (tribe.Id == CampaignTribes.BloodridgeId && !_tabardRecipe)
                {
                    _tabardRecipe = true;
                    result.RecipeLearned = true;
                    Discover("Recipe: " + CampaignForge.TabardName);
                }

                // Beating a chief, captured or killed, raises the bounty on your head (18 Oct 2026).
                _heat += BountyRules.HeatPerChief;
                result.BountyAfter = Bounty;

                // On to the next tier's tribe, or after the last chief the hunters (call J2).
                // The next tier's tribe this run picks (18 Oct 2026: a random tribe per tier), then the hunters.
                CampaignTribe next = RunTribe(tribe, RunsStarted) ?? AlphaHunt.Tribe;
                result.NextTribe = next;
                Target = next;
                Step = 0;
                _lastKind = -1;
                // From rules 18 the next road leans toward the Pals the run holds picks from (DESIGN_LOCKS, "The Butcher's theme").
                RoadPals = fight.RulesVersion >= MarksRules ? HeldPals() : 0;
                _road = MakeRoad(next);
                _roadLane = -1;
                Discover("Tribe: " + next.Name);
                MakeOffers();
                KeepTabardWorn();
                result.FightersLeft = _reserve.Count;
                return result;
            }

            // Along the road (13 Oct 2026): you now stand on the node you won, and a met bonus goal raises its drop a tier.
            if (encounter.Node != null)
            {
                Step = encounter.Node.Row + 1;
                _roadLane = encounter.Node.Lane;
            }
            else Step++;
            _lastKind = (int)encounter.Kind;
            result.RespectBeforeHalving = result.RespectAfter = Step;
            result.ChiefReady = Step >= RoadRows;
            result.Objective = encounter.Objective;
            result.ObjectiveMet = encounter.Objective != FightObjective.None &&
                FightObjectives.Met(encounter.Objective, fight.Sim, fallen.Count);

            // Strip or bank (Jacob, 13 Oct 2026): the materials are home, and one piece of their gear is on offer in
            // their place until the next fight starts. A favour fight offers its Pal's favours instead (18 Oct 2026); from
            // rules 19 it offers both on one card, one to take (fix 5).
            FavourOffer favours = encounter.Node == null ? null : MakeFavourOffer(encounter.Node, fight.RulesVersion);
            if (favours != null)
            {
                _favourOffer = favours;
                if (fight.RulesVersion >= PalDuoRules) MarkDuosShown(favours);
                _spoilsMaterials = materials;
                result.FavourOffered = favours;
            }
            if (favours == null || fight.RulesVersion >= PalRoadRules)
            {
                // The pieces they wore (CampaignFight.Worn); a fight from before they wore any offers what they would have.
                GearTier drop = BaseDrop(encounter);
                IReadOnlyList<CampaignGear> worn = fight.Worn.Count > 0 ? fight.Worn
                    : PiecesFor(encounter, fight.Setup.FightId, fight.Setup.Seed, fight.Setup.Side(1), drop);
                _spoils.Clear();
                _spoils.AddRange(MakeSpoils(worn, Raise(drop, result.ObjectiveMet ? 1 : 0), encounter.Tribe));
                _spoilsMaterials = materials;
                result.SpoilsOffered = Spoils;
                result.SpoilsChoices = new List<CampaignGear>(_spoils);
            }
            result.FightersLeft = _reserve.Count;
            MakeOffers();
            KeepTabardWorn();
            return result;
        }

        // ------------------------------------------------------------ internals

        internal void SetMaterials(int value)
        {
            _materials = Math.Max(0, value);
        }

        /// For CampaignSave: the forge as it stood. A forged tabard means the recipe was known.
        internal void RestoreForge(bool recipe, bool forged)
        {
            _tabardRecipe = recipe || forged;
            _tabardForged = forged;
        }

        /// For CampaignSave: false if the tabard is not forged or that fighter is not yours.
        internal bool RestoreTabardWearer(string fighterId)
        {
            if (!_tabardForged || FindFighter(fighterId) == null) return false;
            _tabardWearer = fighterId;
            return true;
        }

        /// A run's target by id: a tribe on the path, or the Sovereign's hunters.
        internal static CampaignTribe FindTarget(string tribeId)
        {
            return tribeId == AlphaHunt.Id ? AlphaHunt.Tribe : CampaignTribes.Find(tribeId);
        }

        internal void AddJoined(string tribeId)
        {
            if (!_joined.Contains(tribeId)) _joined.Add(tribeId);
        }

        internal void AddFighter(CampaignFighter f)
        {
            _reserve.Add(f);
        }

        internal void AddAssist(Assist a)
        {
            _assists.Add(a);
        }

        /// For CampaignSave: a piece of the run's gear. False if its id is empty or already held.
        internal bool RestoreGear(CampaignGear piece)
        {
            if (piece == null || piece.Id.Length == 0 || FindGear(piece.Id) != null) return false;
            _gear.Add(piece);
            return true;
        }

        /// For CampaignSave: the pieces on offer after a won path fight and the materials they banked. False if there are
        /// none or too many, one is held already or repeats, or something else is already on offer.
        internal bool RestoreSpoils(IReadOnlyList<CampaignGear> pieces, int materials)
        {
            if (pieces == null || pieces.Count == 0 || pieces.Count > CampaignGearRules.SpoilsOffered || materials < 0 ||
                _spoils.Count > 0 || _favourOffer != null) return false;
            for (int i = 0; i < pieces.Count; i++)
            {
                CampaignGear piece = pieces[i];
                if (piece == null || piece.Id.Length == 0 || FindGear(piece.Id) != null) return false;
                for (int j = 0; j < i; j++) if (pieces[j].Id == piece.Id) return false;
            }
            _spoils.AddRange(pieces);
            _spoilsMaterials = materials;
            return true;
        }

        /// For CampaignSave: a favour the run holds, at this rank. False if it is not a button favour (a combo is saved as its
        /// piece of the run's gear), is held already or the rank is out of range.
        internal bool RestoreFavour(Favour favour, int rank)
        {
            if (FavourRules.IsCombo(favour) || FavourRules.PalOf(favour) == Pal.None || rank < 1 || rank > FavourRules.MaxRank ||
                FindFavour(favour) != null)
                return false;
            _favours.Add(new HeldFavour(favour, rank));
            return true;
        }

        /// For CampaignSave: the favours on offer after a won favour fight and the materials it banked. False if they
        /// are not all that Pal's, or something else is already on offer; from save version 25 the same fight's spoils can be
        /// beside them (besideSpoils, fix 5).
        internal bool RestoreFavourOffer(Pal pal, IReadOnlyList<Favour> choices, int materials, bool besideSpoils = false)
        {
            if (pal == Pal.None || choices == null || choices.Count == 0 || choices.Count > FavourRules.MaxOffered || materials < 0 ||
                _favourOffer != null || (_spoils.Count > 0 && !(besideSpoils && materials == _spoilsMaterials))) return false;
            for (int i = 0; i < choices.Count; i++)
                if (FavourRules.PalOf(choices[i]) != pal && !(FavourRules.IsDuo(choices[i]) && FavourRules.DuoHas(FavourRules.DuoOf(choices[i]), pal)))
                    return false;
            _favourOffer = new FavourOffer(pal, new List<Favour>(choices));
            _spoilsMaterials = materials;
            return true;
        }

        /// For CampaignSave: this fighter wears this piece. False if either is missing or the piece is worn already.
        internal bool RestoreWorn(string fighterId, string gearId)
        {
            CampaignFighter f = FindFighter(fighterId);
            CampaignGear piece = FindGear(gearId);
            if (f == null || piece == null || !piece.Fits(f.Class)) return false;
            for (int i = 0; i < _reserve.Count; i++) if (_reserve[i].GearId == piece.Id) return false;
            f.GearId = piece.Id;
            return true;
        }

        /// For CampaignSave: a tribe that captured one of yours this run. False if no run fights them or they are
        /// there already.
        internal bool RestoreGrudge(string tribeId)
        {
            CampaignTribe tribe = FindTarget(tribeId);
            if (tribe == null || tribe.IsStarter || _grudges.Contains(tribeId)) return false;
            _grudges.Add(tribeId);
            return true;
        }

        /// For CampaignSave: an omen picked at camp. False if it is no omen or already picked.
        internal bool RestoreOmen(Omen omen)
        {
            if (!OmenRules.IsOmen((int)omen) || _omens.Contains(omen)) return false;
            _omens.Add(omen);
            _omens.Sort();
            return true;
        }

        internal void Discover(string entry)
        {
            if (!_codex.Contains(entry)) _codex.Add(entry);
        }

        /// After loading: where the campaign stands with no run going.
        internal void Settle()
        {
            _active = null;
            Target = null;
            _road = null;
            _roadLane = -1;
            _offers.Clear();
            _lastKind = -1;
            _beastsBeaten.Clear();
            ClearRunHunt();
            Stage = TutorialDone ? CampaignStage.ChooseTribe : CampaignStage.Tutorial;
        }

        /// For CampaignSave: the hunters' company as saved.
        internal void RestoreCompany(HunterCompany company)
        {
            if (company != null) _company = company;
        }

        /// For CampaignSave: the run's heat, and the hunters beaten and promoted this run. Call before ResumeRun, so
        /// its offers see them. False if any of it cannot be right.
        internal bool RestoreRunHunt(int heat, IReadOnlyList<string> beaten, IReadOnlyList<string> promoted)
        {
            if (heat < 0) return false;
            ClearRunHunt();
            _heat = heat;
            for (int i = 0; i < beaten.Count; i++)
            {
                if (string.IsNullOrEmpty(beaten[i]) || _beatenThisRun.Contains(beaten[i])) return false;
                _beatenThisRun.Add(beaten[i]);
            }
            for (int i = 0; i < promoted.Count; i++)
            {
                if (string.IsNullOrEmpty(promoted[i]) || _promotedThisRun.Contains(promoted[i])) return false;
                _promotedThisRun.Add(promoted[i]);
            }
            return true;
        }

        /// For CampaignSave: the hunters promoted this run, by id.
        internal IReadOnlyList<string> HuntersPromotedThisRun => _promotedThisRun;

        /// For CampaignSave: ambush fights ever finished.
        internal int AmbushFights { get => _ambushFights; set => _ambushFights = Math.Max(0, value); }

        /// After loading only the camp: drop whatever run was there and send a fresh squad.
        internal void ResetToCamp()
        {
            Settle();
            _featsThisRun.Clear();
            _assists.Clear();
            _joined.Clear();
            _reserve.Clear();
            _tabardWearer = string.Empty;
            ClearGearAndGrudges();
            if (!TutorialDone) return;
            _joined.Add(CampaignTribes.ClarthId);
            TopUpReserve();
        }

        /// For CampaignSave: a saved fighter, with the quirk that fighter always has in this campaign.
        internal CampaignFighter MakeFighter(string id, string tribeId, UnitClass unitClass, string name)
        {
            return new CampaignFighter(id, tribeId, unitClass, name, Quirks.For(Seed, id));
        }

        /// For CampaignSave: how many of your fighters had this name. False if it is no name or already known.
        internal bool RestoreNameGiven(string name, int count)
        {
            if (string.IsNullOrEmpty(name) || count < 1 || _namesGiven.ContainsKey(name)) return false;
            _namesGiven[name] = count;
            return true;
        }

        /// For CampaignSave: a name for a fighter saved before names (version 5 and older), picked from their
        /// id, so that save opens with the same names every time.
        internal string NameForSavedFighter(string tribeId, string fighterId)
        {
            return GiveName(CampaignNames.FromKey(Seed, tribeId, fighterId));
        }

        internal static Assist MakeAssist(string tribeId, int grade)
        {
            CampaignTribe t = CampaignTribes.Find(tribeId);
            return new Assist(tribeId, t == null ? "Eclipse" : t.AssistName, t == null ? tribeId : t.ChiefName, grade);
        }

        /// The chief once the road is walked; before that the fights among the road's next nodes (13 Oct 2026, the path
        /// map; until then three random encounters). The Sovereign's hunters are one fight.
        private void MakeOffers()
        {
            _offers.Clear();
            if (Target == null) return;
            if (Target == AlphaHunt.Tribe)
            {
                _offers.Add(MakeEncounter(EncounterKind.AlphaHunt, Target));
                return;
            }
            if (Step >= RoadRows)
            {
                _offers.Add(MakeEncounter(EncounterKind.Chief, Target));
                return;
            }
            // A fight node the hunters took is their ambush instead (18 Oct 2026).
            foreach (RoadNode node in NextNodes)
                if (node.Kind == RoadNodeKind.Fight) _offers.Add(IsAmbush(node) ? MakeAmbush(node) : MakeEncounter(node.Fight, Target, node));
        }

        // The hunters waiting on a road node: the road's hunter and AmbushFighters of the company, one more for every
        // bounty above AmbushFrom. They wind up a move only if the hunter has a cast trait.
        private Encounter MakeAmbush(RoadNode node)
        {
            CampaignHunter hunter = HunterOnRoad;
            int fighters = HunterRules.AmbushFighters + Math.Max(0, Bounty - BountyRules.AmbushFrom);
            var ambush = new Encounter(EncounterKind.Ambush, AlphaHunt.Tribe, _tuning.SquadSize, fighters, fighters + 1, hunter.Casts, false, node)
            {
                Hunter = hunter, HunterRank = _company.Rank(hunter)
            };
            ambush.EnemyClasses = EnemyClassesOf(ambush);
            return ambush;
        }

        // Which of the tribe's squads a fight sends: a road node's own dice, and for the chief the run's (18 Oct 2026). Its
        // own stream, so nothing else the node's seed decides changes.
        private int SquadFor(EncounterKind kind, CampaignTribe tribe, RoadNode node)
        {
            int count = tribe.SquadCount(kind);
            if (count <= 1) return 0;
            ulong seed = node != null ? node.Seed : CampaignRoad.Mix(Seed, RunsStarted, tribe.Id + ".chief");
            return new DeterministicRng(seed, 9UL).Next(count);
        }

        // Their fighters' classes, in the order EnemySpecs adds them; a duel's champion.
        private static IReadOnlyList<UnitClass> EnemyClassesOf(Encounter e)
        {
            if (e.Kind == EncounterKind.Duel) return new[] { DuelChampion(e.Tribe) };
            // A raid chief brings no band (rules 10), so his node shows nobody beside him.
            if (IsRaidChief(e.Tribe, e.Kind, RulesVersion)) return Array.Empty<UnitClass>();
            IReadOnlyList<UnitClass> lineup = e.Tribe.Lineup(e.Kind, e.Squad);
            var classes = new UnitClass[Math.Max(0, e.EnemyFighters)];
            for (int i = 0; i < classes.Length; i++) classes[i] = lineup[i % lineup.Count];
            return classes;
        }

        // A duel's champion: the tribe's first Warden or Skirmisher.
        private static UnitClass DuelChampion(CampaignTribe tribe)
        {
            for (int i = 0; i < tribe.Roster.Count; i++)
                if (tribe.Roster[i] == UnitClass.Warden || tribe.Roster[i] == UnitClass.Skirmisher) return tribe.Roster[i];
            return UnitClass.Skirmisher;
        }

        private static readonly UnitClass[] NoClasses = new UnitClass[0];

        /// Who stands on this road node (Jacob, 18 Oct 2026: "three squads per fight shown on the node"): their fighters in
        /// the order they come, not counting a chief, shaman or captain; a duel's champion; the hunters' band on a node
        /// they took. Empty for an event and for a node that is not on this run's road.
        public IReadOnlyList<UnitClass> SquadOn(RoadNode node)
        {
            if (node == null || _road == null || node.Kind != RoadNodeKind.Fight || _road.Node(node.Row, node.Lane) != node)
                return NoClasses;
            return (IsAmbush(node) ? MakeAmbush(node) : MakeEncounter(node.Fight, Target, node)).EnemyClasses;
        }

        /// The chief's band this run, not counting the chief (18 Oct 2026). Empty with no road tribe, and on the hunt.
        public IReadOnlyList<UnitClass> ChiefSquad =>
            Target == null || Target == AlphaHunt.Tribe ? NoClasses : MakeEncounter(EncounterKind.Chief, Target).EnemyClasses;

        private Encounter MakeEncounter(EncounterKind kind, CampaignTribe tribe, RoadNode node = null)
        {
            int squad = _tuning.SquadSize;
            int fighters;
            int units;
            bool casts = false;
            switch (kind)
            {
                case EncounterKind.Tutorial:
                    squad = CampaignTribes.Clarth.Roster.Count;
                    fighters = units = _tuning.TutorialFighters;
                    break;
                case EncounterKind.WarParty:
                    fighters = units = _tuning.WarPartyCount;
                    break;
                case EncounterKind.HoldTheLine:
                    fighters = units = _tuning.HoldCount;
                    break;
                case EncounterKind.Duel:
                    squad = 1;
                    casts = _tuning.DuelChampionCasts;
                    fighters = casts ? 0 : 1;
                    units = 1;
                    break;
                case EncounterKind.Trial:
                    if (tribe.TrialCasts)
                    {
                        fighters = _tuning.TrialFighters;
                        units = fighters + 1;
                        casts = true;
                    }
                    else
                    {
                        squad = _tuning.PrecisionTrialSquad;
                        fighters = units = _tuning.PrecisionTrialEnemies;
                    }
                    break;
                case EncounterKind.AlphaHunt:
                    // The more tribes join you during the run, and the higher the bounty, the harder the hunters come.
                    fighters = AlphaHunt.Fighters + AlphaHunt.PerTribeJoined * TribesJoinedThisRun() +
                        (HasOmen(Omen.HungryHunters) ? OmenRules.HungryHuntersExtra : 0) + Math.Max(0, Bounty - HunterRules.HuntBountyFrom);
                    units = fighters + 1;
                    casts = tribe.ChiefEclipseGrade > 0;
                    break;
                case EncounterKind.Beast:
                    // The beast is its parts (Battle.BeastKits); no fighters of a tribe, and its moves are tells, not a cast.
                    fighters = 0;
                    units = BeastKits.PartCount;
                    break;
                default:
                    fighters = _tuning.ChiefFighters;
                    units = fighters + 1;
                    casts = tribe.ChiefEclipseGrade > 0;
                    break;
            }
            bool noFallBack = kind == EncounterKind.HoldTheLine && _tuning.HoldForbidsFallBack;
            var encounter = new Encounter(kind, tribe, squad, fighters, units, casts, noFallBack, node);
            // Three squads per fight (Jacob, 18 Oct 2026): a road node sends the one its own dice pick, a chief the run's.
            encounter.Squad = SquadFor(kind, tribe, node);
            encounter.EnemyClasses = EnemyClassesOf(encounter);
            // A named hunter leads the hunt after the last chief: the best the bounty sends (HunterCompany).
            if (kind == EncounterKind.AlphaHunt)
            {
                encounter.Hunter = PickHunter(true, string.Empty);
                encounter.HunterRank = _company.Rank(encounter.Hunter);
            }
            return encounter;
        }

        /// A run ends back at camp, won or lost: its tribes, fighters, chief orders, gear, grudges and the
        /// tabard's wearer reset and the Clarth send a fresh squad. Materials, the forge, the codex and every
        /// tribe's memory stay.
        private void EndRun()
        {
            Target = null;
            Step = 0;
            _road = null;
            _roadLane = -1;
            _lastKind = -1;
            _offers.Clear();
            _featsThisRun.Clear();
            _assists.Clear();
            _joined.Clear();
            _joined.Add(CampaignTribes.ClarthId);
            _reserve.Clear();
            _tabardWearer = string.Empty;
            _beastsBeaten.Clear();
            ClearGearAndGrudges();
            ClearRunHunt();
            TopUpReserve();
            Stage = CampaignStage.ChooseTribe;
        }

        // The bounty and the run's hunters go when the run does; the company stays.
        private void ClearRunHunt()
        {
            _heat = 0;
            _beatenThisRun.Clear();
            _promotedThisRun.Clear();
        }

        /// Who the company sends: a living hunter you have not beaten this run, ranked no higher than the bounty allows
        /// (HunterRules.TopRankSent). Your nemesis comes first: when the bounty allows them, and always in a run where
        /// they already beat you, so the fight you try again is against them. The hunt after the last chief sends the
        /// best of them, and if the bounty allows nobody, the best you have not beaten; an ambush sends one by the road's
        /// own dice.
        private CampaignHunter PickHunter(bool hunt, string key)
        {
            int top = HunterRules.TopRankSent(Bounty);
            IReadOnlyList<CampaignHunter> all = _company.Hunters;
            var sent = new List<CampaignHunter>();
            CampaignHunter best = null;
            for (int i = 0; i < all.Count; i++)
            {
                if (_beatenThisRun.Contains(all[i].Id)) continue;
                if (best == null) best = all[i];
                if (_company.Rank(all[i]) >= top) sent.Add(all[i]);
            }
            CampaignHunter nemesis = _company.Nemesis;
            if (nemesis != null && !_beatenThisRun.Contains(nemesis.Id) && (sent.Contains(nemesis) || _promotedThisRun.Contains(nemesis.Id)))
                return nemesis;
            if (sent.Count == 0) return hunt ? best ?? (all.Count > 0 ? all[0] : null) : null;
            if (hunt) return sent[0];
            return sent[(int)(Mix(Seed, key) % (ulong)sent.Count)];
        }

        // The lane of this road row the hunters wait in, or -1: from BountyRules.AmbushFrom on, a row is taken when its
        // own dice fall under HunterRules.AmbushChance, so a row once taken stays taken as the bounty rises.
        private int AmbushLane(int row)
        {
            int chance = HunterRules.AmbushChance(Bounty);
            if (_road == null || chance <= 0) return -1;
            ulong h = Mix(Seed, "ambush." + RunsStarted.ToString(CultureInfo.InvariantCulture) + "." + _road.TribeId + "." +
                row.ToString(CultureInfo.InvariantCulture));
            if ((int)(h % 100UL) >= chance) return -1;
            var lanes = new List<int>();
            for (int lane = 0; lane < CampaignRoad.Lanes; lane++)
            {
                RoadNode n = _road.Node(row, lane);
                if (n != null && n.Kind == RoadNodeKind.Fight) lanes.Add(lane);
            }
            return lanes.Count == 0 ? -1 : lanes[(int)((h >> 16) % (ulong)lanes.Count)];
        }

        // You lost to this hunter: they mark you as theirs, and the first time this run they climb a place, gain levels
        // and learn a trait against the order you gave most (Jacob's go, 18 Oct 2026: "they learn your favourite
        // tricks"). With no room for a trait they take a level instead.
        private void PromoteHunter(CampaignHunter hunter, FightRecord record, EncounterResult result)
        {
            hunter.Wins++;
            _company.NemesisId = hunter.Id;
            if (_promotedThisRun.Contains(hunter.Id)) return;
            _promotedThisRun.Add(hunter.Id);
            _company.TheyBeat(hunter);
            hunter.Level = Math.Min(HunterRules.MaxLevel, hunter.Level + HunterRules.PromotionLevels);
            result.HunterPromoted = true;
            // Under the parry at the center (rules 8) the favourite counts your parries for Hold, and the hunter keeps where
            // he learned it (HunterLesson).
            int times = 0;
            int favourite = record.ParryCenter ? Favourite(record.Events, true, out times) : Favourite(record);
            if (TryLearnAgainst(hunter, favourite, record.ParryCenter, out HunterTrait learned))
            {
                result.TraitLearned = true;
                result.NewTrait = learned;
                if (record.ParryCenter && favourite >= 0) hunter.Record(new HunterLesson(learned, record.FightId, (Directive)favourite, times));
                Discover("Hunter: " + hunter.Name + " learned " + HunterTraits.Name(learned));
            }
            else hunter.Level = Math.Min(HunterRules.MaxLevel, hunter.Level + 1);
        }

        // A trait that counters the favourite order, else one from the hunter's own dice. False when they have no room.
        private bool TryLearnAgainst(CampaignHunter hunter, int favourite, bool parryCenter, out HunterTrait learned)
        {
            learned = HunterTrait.LongReach;
            if (hunter.Traits.Count >= HunterTraits.MaxPerHunter) return false;
            if (favourite >= 0)
            {
                HunterTrait[] counters = HunterTraits.Counters((Directive)favourite, parryCenter);
                for (int i = 0; i < counters.Length; i++)
                {
                    if (!hunter.CanLearn(counters[i])) continue;
                    learned = counters[i];
                    hunter.Learn(learned);
                    return true;
                }
            }
            DeterministicRng dice = HunterNames.Dice(Seed, hunter.Id + ".learn." + hunter.Traits.Count.ToString(CultureInfo.InvariantCulture));
            for (int tries = 0; tries < 64; tries++)
            {
                var trait = (HunterTrait)dice.Next(HunterTraits.Rolled);
                if (!hunter.CanLearn(trait)) continue;
                learned = trait;
                hunter.Learn(learned);
                return true;
            }
            return false;
        }

        // Under the parry at the center: your parries for Hold ("parried most"), then the Fall Backs, Focus Fires and Unleashes
        // you ordered; the most used as a Directive, and how many times; -1 when you used none. A tie goes to the first of
        // Hold, Fall Back, Focus Fire, Unleash. It reads a fight still going too, for the screen (Campaign.HunterLearning).
        internal static int Favourite(IReadOnlyList<BattleEvent> events, bool parryCenter, out int times)
        {
            Directive[] buttons = { Directive.Hold, Directive.FallBack, Directive.FocusFire, Directive.Unleash };
            int best = -1;
            times = 0;
            for (int i = 0; i < buttons.Length; i++)
            {
                int n = buttons[i] == Directive.Hold && parryCenter ? Parries(events) : Feats.Ordered(events, buttons[i]);
                if (n <= times) continue;
                times = n;
                best = (int)buttons[i];
            }
            return best;
        }

        // Your side's parries (Parry events of side 0).
        private static int Parries(IReadOnlyList<BattleEvent> events)
        {
            int n = 0;
            for (int i = 0; i < events.Count; i++) if (events[i].Kind == BattleEventKind.Parry && events[i].ActorSide == 0) n++;
            return n;
        }

        // The order you gave most in this fight, of the four buttons, as a Directive; -1 when you gave none. A tie goes
        // to the first of Hold, Fall Back, Focus Fire, Unleash.
        private static int Favourite(FightRecord record)
        {
            Directive[] buttons = { Directive.Hold, Directive.FallBack, Directive.FocusFire, Directive.Unleash };
            int best = -1, most = 0;
            for (int i = 0; i < buttons.Length; i++)
            {
                int n = Feats.Ordered(record.Events, buttons[i]);
                if (n <= most) continue;
                most = n;
                best = (int)buttons[i];
            }
            return best;
        }

        // You beat this hunter: you climb past them, they do not come for you again this run, and one who died is off the
        // ladder for good, a recruit joining at the bottom in their place.
        private void BeatHunter(CampaignHunter hunter, CampaignFight fight, EncounterResult result)
        {
            _company.YouBeat(hunter);
            if (!_beatenThisRun.Contains(hunter.Id)) _beatenThisRun.Add(hunter.Id);
            BattleUnit leader = null;
            for (int i = 0; i < fight.Sim.Units.Count; i++)
                if (fight.Sim.Units[i].Side == 1 && fight.Sim.Units[i].Role == UnitRole.Chief) leader = fight.Sim.Units[i];
            if (leader == null || !leader.Alive)
            {
                _company.Slay(hunter);
                _company.Recruit(Seed, 0, 1);
                result.HunterSlain = true;
                Discover("Slain: " + hunter.FullName);
            }
            else
            {
                hunter.Escapes++;
                result.HunterFled = true;
            }
            result.LadderRankAfter = _company.PlayerRank;
        }

        // FNV 1a over the campaign's seed and a key: the same on every machine, and never one of the campaign's streams.
        private static ulong Mix(ulong seed, string key)
        {
            ulong h = 14695981039346656037UL;
            for (int i = 0; i < 8; i++)
            {
                h ^= (seed >> (8 * i)) & 0xFFUL;
                h *= 1099511628211UL;
            }
            for (int i = 0; i < key.Length; i++)
            {
                h ^= key[i];
                h *= 1099511628211UL;
            }
            return h;
        }

        /// Every run starts with a full squad: the Clarth send fresh fighters when you are short.
        private void TopUpReserve()
        {
            int i = 0;
            while (_reserve.Count < Math.Max(3, _tuning.TopUpTo))
            {
                _reserve.Add(NewFighter(CampaignTribes.ClarthId, CampaignTribes.Clarth.Roster[i % CampaignTribes.Clarth.Roster.Count], null));
                i++;
            }
        }

        // Nobody wears the forged tabard (a new run, or its wearer fell): the first Warden takes it,
        // else the first fighter, until the player hands it to someone else.
        private void KeepTabardWorn()
        {
            if (!_tabardForged) return;
            if (_tabardWearer.Length > 0 && FindFighter(_tabardWearer) != null) return;
            _tabardWearer = string.Empty;
            for (int i = 0; i < _reserve.Count && _tabardWearer.Length == 0; i++)
                if (_reserve[i].Class == UnitClass.Warden) _tabardWearer = _reserve[i].Id;
            if (_tabardWearer.Length == 0 && _reserve.Count > 0) _tabardWearer = _reserve[0].Id;
        }

        // Tribes that joined you this run, not counting the Clarth.
        private int TribesJoinedThisRun()
        {
            int count = 0;
            for (int i = 0; i < _joined.Count; i++) if (_joined[i] != CampaignTribes.ClarthId) count++;
            return count;
        }

        // A fighter joins you under the name they came with: a tutorial survivor's, or a recruit's from the fight
        // they lost. A fresh one takes their people's next name after the tutorial's five (CampaignNames).
        private CampaignFighter NewFighter(string tribeId, UnitClass unitClass, string name)
        {
            _fighterCounter++;
            if (string.IsNullOrEmpty(name))
                name = CampaignNames.Roster(Seed, tribeId, CampaignTribes.Clarth.Roster.Count + _fighterCounter);
            string id = tribeId + "." + SliceClasses.Name(unitClass).ToLowerInvariant() + "." +
                _fighterCounter.ToString(CultureInfo.InvariantCulture);
            return new CampaignFighter(id, tribeId, unitClass, GiveName(CampaignNames.BaseOf(name)), Quirks.For(Seed, id));
        }

        // The first of yours given a name keeps it plain; the next is "the Second", and so on, all campaign
        // (Jacob, 12 Oct 2026).
        private string GiveName(string name)
        {
            if (string.IsNullOrEmpty(name)) return string.Empty;
            _namesGiven.TryGetValue(name, out int given);
            _namesGiven[name] = ++given;
            return CampaignNames.WithOrdinal(name, given);
        }

        private CampaignFighter FindFighter(string id)
        {
            for (int i = 0; i < _reserve.Count; i++) if (_reserve[i].Id == id) return _reserve[i];
            return null;
        }

        private List<CampaignFighter> PickSquad(IReadOnlyList<string> ids, int size)
        {
            if (ids.Count == 0 || ids.Count > size) throw new ArgumentException("Pick between 1 and " + size + " fighters.");
            var squad = new List<CampaignFighter>();
            for (int i = 0; i < ids.Count; i++)
            {
                CampaignFighter f = FindFighter(ids[i]);
                if (f == null) throw new ArgumentException(ids[i] + " is not in your reserve.");
                if (squad.Contains(f)) throw new ArgumentException(ids[i] + " is picked twice.");
                squad.Add(f);
            }
            return squad;
        }

        private Assist BestAssist()
        {
            Assist best = null;
            for (int i = 0; i < _assists.Count; i++) if (best == null || _assists[i].Grade > best.Grade) best = _assists[i];
            return best;
        }

        /// The Clarth's own order, Dust Shark: your 5th button carries it while no chief's order is taken this run, from
        /// the tutorial on (Jacob, 18 Oct 2026). It is not one of the run's Assists, so it is never lost, saved or listed.
        private static readonly Assist StarterAssist = new Assist(CampaignTribes.ClarthId, CampaignTribes.Clarth.AssistName,
            CampaignTribes.Clarth.ChiefName, CampaignTribes.Clarth.AssistGrade);

        /// The order your 5th button carries in a fight: the best one taken this run, else the Clarth's own. None when
        /// the leader is not in the fight, since the leader casts it.
        private Assist FightAssist(Encounter encounter, bool leaderFights)
        {
            if (!leaderFights) return null;
            Assist best = encounter.Kind == EncounterKind.Tutorial ? null : BestAssist();
            return best ?? StarterAssist;
        }

        private BattleSetup BuildSetup(Encounter encounter, IReadOnlyList<CampaignFighter> squad, bool leaderFights,
            out List<CampaignGear> worn, out int rules)
        {
            // An unfinished fight resumes under the rules it began with (CampaignSave); a new one plays the current rules.
            rules = Math.Min(ResumeRulesVersion < 0 ? RulesVersion : ResumeRulesVersion, _tuning.RulesCap);
            ResumeRulesVersion = -1;
            _fightCounter++;
            CampaignTribe tribe = encounter.Tribe;
            string fightId = "camp." + _fightCounter.ToString(CultureInfo.InvariantCulture) + "." + tribe.Id + "." +
                encounter.Kind.ToString().ToLowerInvariant();
            int tier = encounter.Kind == EncounterKind.Tutorial ? 1 : tribe.Tier;
            int ours = _tuning.TierPercent(tier);
            var side0 = new List<UnitSpec>();
            if (leaderFights)
                side0.Add(new UnitSpec(LeaderId, _tuning.LeaderClass, Row.Back, UnitRole.Pal, _tuning.LeaderPercent, _tuning.LeaderPercent));
            // Two of one tribe give each of them its stat, three also one of its doctrines to your side (TribeSets).
            var tribesHere = new List<string>();
            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < squad.Count; i++)
            {
                counts.TryGetValue(squad[i].TribeId, out int n);
                if (n == 0) tribesHere.Add(squad[i].TribeId);
                counts[squad[i].TribeId] = n + 1;
            }
            for (int i = 0; i < squad.Count; i++)
            {
                CampaignFighter f = squad[i];
                // The tabard's wearer frightens: their hits drain more nerve (call J1).
                int fear = f.Id == _tabardWearer ? CampaignForge.TabardFear : 0;
                // A Grudge is against a tribe; the beast beside its road is not that tribe (the audit, 22 Oct 2026).
                string foe = encounter.Kind == EncounterKind.Beast ? BeastKits.Id(encounter.Beast) : tribe.Id;
                side0.Add(FighterSpec(f, ours, fear, foe, counts[f.TribeId]));
            }
            // From rules 3 the Blood Moon omen keeps a fighter's bleeding blades (21 Oct 2026).
            bool keepBleed = rules >= 3;
            List<UnitSpec> side1 = EnemySpecs(encounter, fightId, keepBleed);
            Assist assist = FightAssist(encounter, leaderFights);
            // Your 5th button does what its chief's spell does (ChiefSpells, 18 Oct 2026).
            CampaignTribe assistTribe = assist == null ? null : CampaignTribes.Find(assist.TribeId);
            // The run's favours from the Pals bend your orders (18 Oct 2026); the tutorial is practice and takes none.
            var playerRules = new SideRules(assist == null ? 0 : assist.Grade, 0, true,
                spell: assistTribe == null ? ChiefSpell.Eclipse : assistTribe.Spell,
                favours: encounter.Kind == EncounterKind.Tutorial ? SideFavours.None : RunFavours,
                flood: SquadFloods(tribesHere, counts));
            // Each tribe fights to its own doctrine, and every campaign fight plays the 13 Oct rules (Jacob,
            // 13 Oct 2026; DESIGN_LOCKS, "Game design, owner decisions, 12 and 13 Oct 2026"). The tutorial is practice
            // ("fix all 9", 13 Oct 2026): they bring their doctrine but none of their habits, so each order the coach
            // asks for does what it says, Unleash included.
            // A beast has no habits and no doctrine (Battle.BeastKits, 21 Oct 2026).
            bool beast = encounter.Kind == EncounterKind.Beast;
            // From rules 9 a chief brings his own mind (ChiefMind), which only fights against him teach.
            TacticsProfile theirTactics = encounter.Kind == EncounterKind.Tutorial || beast ? new TacticsProfile()
                : LearnerMind(encounter, rules >= SquadFirstRules).CopyTactics();
            if (!beast) TribeDoctrines.Apply(tribe.Id, theirTactics);
            TacticsProfile ourTactics = CultMind.CopyTactics();
            // The move a wait out or feint sits on is the squad first's (rules 9): an older fight's setup never holds one, so it
            // plays and resumes exactly as it did.
            if (rules < SquadFirstRules)
            {
                theirTactics.SetWaitOutMove(string.Empty);
                theirTactics.SetFeintMove(string.Empty);
                ourTactics.SetWaitOutMove(string.Empty);
                ourTactics.SetFeintMove(string.Empty);
            }
            for (int i = 0; i < tribesHere.Count; i++)
                if (counts[tribesHere[i]] >= TribeSets.DoctrineAt) TribeDoctrines.ApplySet(tribesHere[i], ourTactics);
            // Every fight plays the class spells from 20 Oct 2026 (Jacob: "go" to Sprint 1), and a chief fight the chief
            // rules too (his "go", later that day), and from 21 Oct the chief kits ("go do the chief build"), unless it is an
            // unfinished fight from before them.
            bool chiefRules = rules >= 2 && encounter.Kind == EncounterKind.Chief;
            bool chiefKits = rules >= 3 && chiefRules;
            // A beast fight plays the beasts from 21 Oct 2026, later ("go, build the shark API with your six calls").
            bool beasts = rules >= 4 && beast;
            // A chief fight plays the kits' tactical pieces from 22 Oct 2026 ("go, build the Skullpeak chief and the
            // Bloodridge extras in the sim"), and the hunt at the end of a run plays the hunt, with the same rules, from later
            // that day ("yes go"). The hunt brings no reserves.
            bool chiefTactics = rules >= 5 && chiefKits;
            bool huntKit = rules >= 6 && encounter.Kind == EncounterKind.AlphaHunt;
            FightRules fightRules = rules == 0 ? FightRules.Skill
                : beasts ? FightRules.CampaignBeast
                : chiefTactics || huntKit ? FightRules.CampaignChiefTactics
                : chiefKits ? FightRules.CampaignChiefKit : chiefRules ? FightRules.CampaignChief : FightRules.Campaign;
            // From rules 8 every fight plays the parry at the center (Jacob, 24 Oct 2026).
            if (rules >= 8) fightRules = fightRules.WithParryCenter();
            // From rules 9 the squad first on top of it (Jacob, 24 Oct 2026, later).
            if (rules >= SquadFirstRules) fightRules = fightRules.WithSquadFirst();
            // From rules 10 the raid chief on top of it, and Bloodridge's chief fights alone as a raid boss (Jacob, 25 Oct 2026:
            // "then it comes to chief, there's only chief and his mechanics"). He brings no band and no reserves.
            if (rules >= RaidChiefRules) fightRules = fightRules.WithRaidChief();
            // From rules 11 road combat on top of it (Battle.BattleSimulation.Road.cs).
            if (rules >= RoadCombatRules) fightRules = fightRules.WithRoadCombat();
            // From rules 12 the road kit on top of it (Battle.BattleSimulation.Boosts.cs, Burn.cs and Carriers.cs): Bloodridge's
            // fighters burn, yours from Bloodridge too, and their road fighters carry their chief's moves.
            if (rules >= RoadKitRules)
            {
                fightRules = fightRules.WithRoadKit();
                RoadKit(side0, side1, encounter);
            }
            if (rules >= ClearRoadRules)
                fightRules = fightRules.WithClearRoad(encounter.Kind == EncounterKind.Tutorial ? 1 :
                    StagedBloodridgeRoad && encounter.Node != null ? Math.Min(4, encounter.Node.Row + 1) : 4);
            if (rules >= SquadScreenRules) fightRules = fightRules.WithSquadScreen();
            // From rules 16 a duel is a champion duel (DESIGN_LOCKS, "Duels", 30 Oct 2026).
            if (rules >= DuelRules && encounter.Kind == EncounterKind.Duel) fightRules = fightRules.WithChampionDuel();
            // From rules 17 every fight plays the damage layers (DESIGN_LOCKS, "Builds", step 2). Last, as the other With rules drop it.
            if (rules >= LayersRules) fightRules = fightRules.WithLayers();
            // From rules 18 the Butcher's marks (DESIGN_LOCKS, "The Butcher's theme"). Last, after the layers.
            if (rules >= MarksRules) fightRules = fightRules.WithMarks();
            // From rules 20 payoffs spread (Jacob's playtest, fix 1). Last, after the marks.
            if (rules >= PayoffRules) fightRules = fightRules.WithPayoffs();
            // From rules 21 danger (DESIGN_LOCKS, "Jacob's playtest fixes 3, 7 and 9"). Last, after payoffs.
            if (rules >= DangerRules) fightRules = fightRules.WithDanger();
            // From rules 22 the parry meter (DESIGN_LOCKS, "The parry meter"). Last, after danger.
            if (rules >= MeterRules) fightRules = fightRules.WithMeter();
            // From rules 23 the floor under the three Pals, and your side brings the Shade's routes (DESIGN_LOCKS, "The floor under
            // the three Pals"). Last, after the meter.
            if (rules >= FloorRules)
            {
                fightRules = fightRules.WithFloor();
                if (encounter.Kind != EncounterKind.Tutorial) playerRules = playerRules.WithPals(RunPalKit(rules));
                // From rules 24 the Whisperer: your habits and pair come in the kit, and each fighter brings his rank (DESIGN_LOCKS,
                // "Habits come with the game, the Whisperer upgrades them"). Last, after the floor.
                if (rules >= WhispererRules)
                {
                    fightRules = fightRules.WithWhisper();
                    if (encounter.Kind != EncounterKind.Tutorial) RankSpecs(side0);
                }
                // From rules 28 the Shade's forms and prices play. Last, after the Whisperer.
                if (rules >= ShadeFormsRules) fightRules = fightRules.WithShadeForms();
                // From rules 29 the Glutton's talents play. Last, after the Shade's forms.
                if (rules >= GluttonTreeRules) fightRules = fightRules.WithGlutton();
                // From rules 30 the duos play. Last, after the Glutton.
                if (rules >= PalDuoRules) fightRules = fightRules.WithDuos();
                // From rules 31 the sized picks. Last, after the duos.
                if (rules >= SizedRules) fightRules = fightRules.WithSized();
                // From rules 32 the Butcher's flavours. Last, after sizing.
                if (rules >= ButcherFlavourRules) fightRules = fightRules.WithFlavours();
                // From rules 33 the vows and marks on every hit. Last, after the flavours.
                if (rules >= VowFlavourRules) fightRules = fightRules.WithVows();
                // From rules 34 the duos that change a move. Last, after the vows.
                if (rules >= DuoMovesRules) fightRules = fightRules.WithDuoMoves();
            }
            // From rules 25 a beast fight plays its moves as the approved scenes show them (Jacob, 1 Nov 2026). Last, after the
            // Whisperer.
            if (rules >= BeastSceneRules && beasts) fightRules = fightRules.WithBeastScenes();
            // From rules 26 the tight parry (DESIGN_LOCKS, "The parry: tight window, big payoff, a gold flash on the attacker").
            // Last, after the beasts.
            if (rules >= TightParryRules) fightRules = fightRules.WithTight();
            // From rules 27 the beast is a raid boss, and the shark brings its two sand pups, after its parts. Last, after the
            // tight parry.
            if (rules >= BeastRaidRules && beasts)
            {
                fightRules = fightRules.WithBeastRaid();
                if (encounter.Beast == BeastKit.SandShark) AddPups(side1, fightId);
            }
            bool raidChief = IsRaidChief(tribe, encounter.Kind, rules);
            if (raidChief) MakeRaidChief(side1);
            // His second phase brings his reserves into the fight (Battle.ChiefFight).
            if (chiefRules && !raidChief) AddReserves(side1, encounter, fightId, keepBleed);
            ulong seed = NextSeed();
            // Their fighters wear the pieces a win strips (Jacob, 20 Oct 2026: "strip one of three pieces they wore").
            worn = rules >= 2 ? DressThem(encounter, fightId, seed, side1, rules) : new List<CampaignGear>();
            return new BattleSetup(fightId, seed, side0, side1, ourTactics, theirTactics,
                encounter.Kind == EncounterKind.HoldTheLine ? _tuning.HoldTicks : _tuning.FightTicks,
                BattleSetup.DefaultDirectivePoints,
                NerveRules.On(NerveRules.Full, NerveRules.Full).WithOrderCosts(_tuning.UnleashNerve, _tuning.HoldDrainEveryTicks),
                SliceRules.RefillTicks, SliceRules.HealthPercent, playerRules, EnemyRules(encounter, chiefKits, rules), fightRules,
                rules >= RoadCombatRules ? BondsFor(side0) : null);
        }

        // Three fighters of a tribe that floods bring the flood to your side (TribeSets, 18 Oct 2026).
        private static bool SquadFloods(List<string> tribesHere, Dictionary<string, int> counts)
        {
            for (int i = 0; i < tribesHere.Count; i++)
            {
                CampaignTribe t = CampaignTribes.Find(tribesHere[i]);
                if (t != null && t.Floods && counts[tribesHere[i]] >= TribeSets.DoctrineAt) return true;
            }
            return false;
        }

        // A fighter's spec for this fight: the tier's numbers, then their quirk, their gear and their tribe's set
        // (Jacob, 13 Oct 2026; DESIGN_LOCKS, "Sprint 2 calls"), in the row you stood them in.
        private UnitSpec FighterSpec(CampaignFighter f, int tierPercent, int fear, string enemyTribeId, int sameTribe)
        {
            int damage = 0, health = 0, hit = 0, avoid = 0, rise = 0, killNerve = 0, bigHit = 0, wounded = 0, bleed = 0;
            switch (f.Quirk)
            {
                case Quirk.Grudge:
                    if (_grudges.Contains(enemyTribeId)) damage += Quirks.GrudgePercent;
                    break;
                case Quirk.Brute: damage += Quirks.BrutePercent; break;
                case Quirk.Tough: health += Quirks.ToughPercent; break;
                case Quirk.SharpEye: hit += Quirks.SharpEyeHit; break;
                case Quirk.Quick: avoid += Quirks.QuickAvoid; break;
                case Quirk.Steady: rise += Quirks.SteadyRise; break;
            }
            CampaignGear piece = FindGear(f.GearId);
            // A rank 3 fighter's second slot carries a second piece, its stat and special too (rules 23).
            CampaignGear second = f.Rank >= PalStoreRules.SecondSlotRank ? FindGear(f.GearId2) : null;
            for (int p = 0; p < 2; p++)
            {
                CampaignGear worn = p == 0 ? piece : second;
                if (worn == null) continue;
                switch (worn.Stat)
                {
                    case GearStat.Damage: damage += worn.Bonus; break;
                    case GearStat.Health: health += worn.Bonus; break;
                    case GearStat.Hit: hit += worn.Bonus; break;
                    default: avoid += worn.Bonus; break;
                }
                if (worn.Special == GearSpecial.KillsBreakNerve) killNerve += CampaignGearRules.KillNerve;
                if (worn.Special == GearSpecial.BigHitGuard) bigHit += CampaignGearRules.BigHitGuardPercent;
                if (worn.Special == GearSpecial.Bleeding) bleed += CampaignGearRules.BleedingPercent;
                if (worn.Special == GearSpecial.Predator) wounded += CampaignGearRules.PredatorPercent;
            }
            if (sameTribe >= TribeSets.StatAt)
            {
                switch (f.TribeId)
                {
                    case CampaignTribes.ClarthId: avoid += TribeSets.ClarthAvoid; break;
                    case CampaignTribes.BloodridgeId: damage += TribeSets.BloodridgeDamagePercent; break;
                    case CampaignTribes.SkullpeakId: health += TribeSets.SkullpeakHealthPercent; break;
                    case CampaignTribes.StoneveilId: hit += TribeSets.StoneveilHit; break;
                    case CampaignTribes.ThundercoastId: avoid += TribeSets.ThundercoastAvoid; break;
                }
            }
            UnitSpec spec = TribeTraits.Spec(f.Id, f.Class, f.Row, UnitRole.Fighter, tierPercent * (100 + damage) / 100,
                tierPercent * (100 + health) / 100, fear, f.TribeId, hit, avoid, rise, killNerve, bigHit, wounded, bleed);
            // A class piece bends its own class's spell (Jacob, 20 Oct 2026).
            if (piece != null && ClassSpells.IsFor(piece.Bend, f.Class))
            {
                spec = spec.WithBend(piece.Bend);
                // A ranked combo lays more of its mark at once (rules 18).
                if (piece.Rank > 1) spec = spec.WithComboRank(piece.Rank);
            }
            // His second piece bends his spell too, a combo with its own rank (rules 23).
            if (second != null && ClassSpells.IsFor(second.Bend, f.Class)) spec = spec.WithSecondPiece(second.Bend, second.Rank);
            // Road scars: their own spell works harder against a beast (Battle.BeastKits, 21 Oct 2026).
            return f.Scars.Count > 0 ? spec.WithScars(f.Scars.Count) : spec;
        }

        // The one of yours the winners take after a lost fight: the most hurt still standing; on a tie, the one
        // earlier in the squad. Null when none of yours is standing.
        private CampaignFighter MostHurtSurvivor(CampaignFight fight)
        {
            CampaignFighter pick = null;
            int lowest = int.MaxValue;
            for (int i = 0; i < fight.Squad.Count; i++)
            {
                BattleUnit u = fight.Sim.FindById(fight.Squad[i].Id);
                CampaignFighter f = FindFighter(fight.Squad[i].Id);
                if (u == null || !u.Alive || f == null || u.HpPermille >= lowest) continue;
                lowest = u.HpPermille;
                pick = f;
            }
            return pick;
        }

        // Whether a win in this fight, under these campaign rules, strips their gear: a path fight (a war party, hold the line,
        // a trial or a duel) that is not a favour fight. A favour node whose Pal has nothing left to give drops gear as usual
        // (18 Oct 2026). From rules 19 every path fight does, its Pal's picks beside the gear (fix 5).
        private bool StripsGear(Encounter encounter, int rules)
        {
            bool path = encounter.Kind == EncounterKind.WarParty || encounter.Kind == EncounterKind.HoldTheLine ||
                encounter.Kind == EncounterKind.Trial || encounter.Kind == EncounterKind.Duel;
            return path && (rules >= PalRoadRules || encounter.Node == null || MakeFavourOffer(encounter.Node, rules) == null);
        }

        // The tier a fight's pieces are, before a met bonus goal raises the drop (DropOf, omens counted).
        private GearTier BaseDrop(Encounter encounter)
        {
            return encounter.Node != null ? DropOf(encounter.Node) : CampaignGearRules.DropTier(encounter.Kind);
        }

        // Their fighters put on the pieces a win strips (Jacob, 20 Oct 2026: "strip one of three pieces they wore"): each
        // piece goes on the first of theirs of its class and raises its stat and bends its spell, as yours do. None in a
        // fight whose win strips no gear.
        private List<CampaignGear> DressThem(Encounter encounter, string fightId, ulong seed, List<UnitSpec> them, int rules)
        {
            if (!StripsGear(encounter, rules)) return new List<CampaignGear>();
            List<CampaignGear> worn = PiecesFor(encounter, fightId, seed, them, BaseDrop(encounter));
            for (int k = 0; k < worn.Count; k++)
                for (int i = 0; i < them.Count; i++)
                {
                    if ((int)them[i].Class != worn[k].ClassIndex) continue;
                    them[i] = Wear(them[i], worn[k]);
                    break;
                }
            return worn;
        }

        // The pieces a fight's fighters wear: one for each of up to three classes among them, in their order from a start
        // the fight's seed picks, so a duel's champion wears one. Each is a class piece with one of its class's bends and
        // a stat, both from the seed, so the same fight always dresses the same way. A piece you already hold, the same
        // tribe, class, bend, stat and tier, moves on to the next stat (Claude's playtest for Jacob, 14 Oct 2026: the same
        // Eye Charm was offered twice running).
        private List<CampaignGear> PiecesFor(Encounter encounter, string fightId, ulong seed, IReadOnlyList<UnitSpec> them, GearTier tier)
        {
            var classes = new List<UnitClass>();
            for (int i = 0; i < them.Count; i++) if (!them[i].Reserve && !classes.Contains(them[i].Class)) classes.Add(them[i].Class);
            var pieces = new List<CampaignGear>();
            if (classes.Count == 0) return pieces;
            int start = (int)(seed % (ulong)classes.Count);
            for (int k = 0; k < CampaignGearRules.SpoilsOffered && k < classes.Count; k++)
            {
                UnitClass cls = classes[(start + k) % classes.Count];
                ulong mix = seed >> (8 * k + 3);
                ClassBend bend = ClassSpells.BendOf(cls, (int)(mix % (ulong)ClassSpells.BendsPerClass));
                var stat = (GearStat)(int)((mix >> 2) % 4UL);
                for (int i = 0; i < 4 && HoldsGear(encounter.Tribe.Id, stat, tier, (int)cls, bend); i++) stat = (GearStat)(((int)stat + 1) % 4);
                pieces.Add(new CampaignGear("gear." + fightId + "." + k.ToString(CultureInfo.InvariantCulture), encounter.Tribe.Id,
                    tier, stat, GearSpecial.None, CampaignGearRules.ClassName(encounter.Tribe, stat, bend), (int)cls, bend));
            }
            return pieces;
        }

        // An enemy fighter wearing a piece: its stat raised and its bend on, as a piece does for yours.
        private static UnitSpec Wear(UnitSpec s, CampaignGear piece)
        {
            int power = s.PowerPercent, health = s.HealthPercent, hit = s.HitBonus, avoid = s.AvoidBonus;
            switch (piece.Stat)
            {
                case GearStat.Damage: power = power * (100 + piece.Bonus) / 100; break;
                case GearStat.Health: health = health * (100 + piece.Bonus) / 100; break;
                case GearStat.Hit: hit += piece.Bonus; break;
                default: avoid += piece.Bonus; break;
            }
            return new UnitSpec(s.Id, s.Class, s.Row, s.Role, power, health, s.FearPercent, s.WoundedBonusPercent, s.ArmorPercent,
                hit, avoid, s.RiseBonus, s.KillNerve, s.BigHitArmorPercent, s.BleedPercent, piece.Bend, s.Reserve);
        }

        // What a won fight offers: the pieces they wore, a tier higher with a met bonus goal. Raised, a piece you already
        // hold at that tier moves on to the next stat, as above.
        private List<CampaignGear> MakeSpoils(IReadOnlyList<CampaignGear> worn, GearTier tier, CampaignTribe tribe)
        {
            var pieces = new List<CampaignGear>();
            for (int k = 0; k < worn.Count; k++)
            {
                CampaignGear w = worn[k];
                if (w.Tier == tier)
                {
                    pieces.Add(w);
                    continue;
                }
                GearStat stat = w.Stat;
                for (int i = 0; i < 4 && HoldsGear(w.TribeId, stat, tier, w.ClassIndex, w.Bend); i++) stat = (GearStat)(((int)stat + 1) % 4);
                pieces.Add(new CampaignGear(w.Id, w.TribeId, tier, stat, GearSpecial.None, CampaignGearRules.ClassName(tribe, stat, w.Bend),
                    w.ClassIndex, w.Bend));
            }
            return pieces;
        }

        private bool HoldsGear(string tribeId, GearStat stat, GearTier tier, int classIndex, ClassBend bend)
        {
            for (int i = 0; i < _gear.Count; i++)
                if (_gear[i].TribeId == tribeId && _gear[i].Stat == stat && _gear[i].Tier == tier && _gear[i].ClassIndex == classIndex &&
                    _gear[i].Bend == bend) return true;
            return false;
        }

        // A killed chief's Princess piece, named for him.
        private CampaignGear MakePrincess(CampaignTribe tribe, string fightId)
        {
            CampaignChief chief = ChiefOf(tribe.Id);
            GearStat stat = CampaignGearRules.ChiefStat(tribe.Id);
            return new CampaignGear("gear." + fightId, tribe.Id, GearTier.Princess, stat, CampaignGearRules.ChiefSpecial(tribe.Id),
                CampaignGearRules.Name(tribe, stat, GearTier.Princess, chief == null ? tribe.ChiefName : chief.Name));
        }

        // Rows on a road: the tuning's, one more with Long Road.
        private int RoadRows => Math.Max(0, (StagedBloodridgeRoad ? Math.Max(4, _tuning.PathFights) : _tuning.PathFights) + (HasOmen(Omen.LongRoad) ? 1 : 0));

        // This run's road to this tribe; the hunters have none.
        private CampaignRoad MakeRoad(CampaignTribe tribe)
        {
            return tribe == null || tribe == AlphaHunt.Tribe ? null : CampaignRoad.Make(Seed, RunsStarted, tribe.Id, RoadRows, StagedBloodridgeRoad,
                RoadPals, PalRoadRun);
        }

        private static GearTier Raise(GearTier tier, int steps)
        {
            return (GearTier)Math.Min((int)GearTier.Princess, (int)tier + steps);
        }

        // A piece an event gives: the road's tribe's, its stat from the node's own seed. A piece you already hold, the
        // same tribe, stat and tier, moves on to the next stat, as a won fight's spoils do (Jacob, 14 Oct 2026: "fix the
        // loose end").
        // Since 20 Oct 2026 it is a class piece too, for one of the classes you have, its class and bend also from the
        // node's seed.
        private CampaignGear EventGear(RoadNode node, GearTier tier)
        {
            CampaignTribe tribe = CampaignTribes.Find(_road.TribeId);
            var yours = new List<UnitClass>();
            for (int i = 0; i < _reserve.Count; i++) if (!yours.Contains(_reserve[i].Class)) yours.Add(_reserve[i].Class);
            if (yours.Count == 0) yours.AddRange(SliceClasses.All);
            UnitClass cls = yours[(int)((node.Seed >> 5) % (ulong)yours.Count)];
            ClassBend bend = ClassSpells.BendOf(cls, (int)((node.Seed >> 9) % (ulong)ClassSpells.BendsPerClass));
            var stat = (GearStat)(int)(node.Seed % 4UL);
            for (int i = 0; i < 4 && HoldsGear(tribe.Id, stat, tier, (int)cls, bend); i++) stat = (GearStat)(((int)stat + 1) % 4);
            return new CampaignGear("gear.event." + RunsStarted.ToString(CultureInfo.InvariantCulture) + "." + tribe.Id + "." +
                node.Row.ToString(CultureInfo.InvariantCulture) + "." + node.Lane.ToString(CultureInfo.InvariantCulture),
                tribe.Id, tier, stat, GearSpecial.None, CampaignGearRules.ClassName(tribe, stat, bend), (int)cls, bend);
        }

        // A run's gear and grudges go when the run does.
        private void ClearGearAndGrudges()
        {
            _gear.Clear();
            _spoils.Clear();
            _spoilsMaterials = 0;
            // The Pals' favours go with the run too (18 Oct 2026).
            _favours.Clear();
            _favourOffer = null;
            for (int i = 0; i < _reserve.Count; i++) _reserve[i].GearId = string.Empty;
            // And the Pals' stores (rules 23, Campaign.PalStores.cs).
            ClearPalStores();
            _grudges.Clear();
        }

        // Names for their side (CampaignNames): their chief fights under their own name, everyone else takes the
        // tribe's next names in this run, fight by fight, so nobody you meet on its path shares one, a fight tried
        // again after a loss included (13 Oct 2026), until its four fights' worth of names run out. The Sovereign's
        // side has none.
        private Dictionary<string, string> EnemyNames(Encounter encounter, BattleSetup setup)
        {
            var names = new Dictionary<string, string>(StringComparer.Ordinal);
            CampaignTribe tribe = encounter.Tribe;
            // A beast's parts go by the beast's name and theirs (Battle.BeastKits, 21 Oct 2026).
            if (encounter.Kind == EncounterKind.Beast)
            {
                IReadOnlyList<UnitSpec> parts = setup.Side(1);
                for (int i = 0; i < parts.Count && i < BeastKits.PartCount; i++)
                    names[parts[i].Id] = BeastKits.Name(encounter.Beast) + " " + BeastKits.Name(BeastKits.PartAt(encounter.Beast, i));
                return names;
            }
            // A named hunter fights under their own name; the company's blades have none (18 Oct 2026).
            if (encounter.Hunter != null)
            {
                IReadOnlyList<UnitSpec> band = setup.Side(1);
                for (int i = 0; i < band.Count; i++) if (band[i].Role == UnitRole.Chief) names[band[i].Id] = encounter.Hunter.Name;
                return names;
            }
            if (!CampaignNames.Named(tribe.Id)) return names;
            IReadOnlyList<UnitSpec> side = setup.Side(1);
            // Ambushes are not on the tribe's path, so they take no place in its order of names (18 Oct 2026); nor are beasts.
            List<string> generated = CampaignNames.ForFight(Seed, RunsStarted, _fightCounter - _ambushFights - _beastFights, tribe.Id,
                side.Count);
            CampaignChief chief = encounter.Kind == EncounterKind.Chief ? ChiefOf(tribe.Id) : null;
            for (int i = 0; i < side.Count; i++)
                names[side[i].Id] = chief != null && side[i].Role == UnitRole.Chief ? chief.Name : generated[i];
            return names;
        }

        private SideRules EnemyRules(Encounter encounter, bool chiefKits, int rules)
        {
            // A beast swings no heavies and casts nothing: its parts' moves are its own (Battle.BeastKits, 21 Oct 2026).
            if (encounter.Kind == EncounterKind.Beast) return new SideRules(0, 0, false, beast: encounter.Beast);
            // Their fighters swing heavies you can parry in every real fight (Jacob, 18 Oct 2026: "parry always wins
            // thats the whole point"). The tutorial stays practice for the orders.
            // From rules 9 they swing every 5 seconds, not every 2.5 (ParryRules.SquadSwingTicks); from rules 16 a duel's champion,
            // who is no band, every 2.5 again (DESIGN_LOCKS, "Duels").
            bool championDuel = rules >= DuelRules && encounter.Kind == EncounterKind.Duel;
            int heavy = encounter.Kind == EncounterKind.Tutorial ? 0 : rules >= SquadFirstRules && !championDuel ? ParryRules.SquadSwingTicks : HeavyRules.EveryTicks;
            if (rules == ClearRoadRules && StagedBloodridgeRoad && encounter.Node != null && encounter.Node.Row == 0) heavy = 0;
            if (encounter.Kind == EncounterKind.AlphaHunt || encounter.Kind == EncounterKind.Ambush)
                return HuntersRules(encounter, heavy, rules >= 6 && encounter.Kind == EncounterKind.AlphaHunt, rules >= 8);
            // The sea tribe floods every fight it is in (Battle.FloodRules, 18 Oct 2026).
            bool flood = encounter.Tribe.Floods;
            if (!encounter.EnemyCasts) return heavy == 0 && !flood ? SideRules.None : new SideRules(0, 0, false, heavy, flood: flood);
            // Their caster casts their tribe's own spell (ChiefSpells, 18 Oct 2026).
            ChiefSpell spell = encounter.Tribe.Spell;
            // A chief with his own kit fights with the moves he took this run, his spell (Bloodridge's Eclipse, Skullpeak's
            // Steel Storm, Stoneveil's Blood Call, Thundercoast's Tide) on its own clock if he took it (Battle.ChiefKits, 21 and
            // 22 Oct 2026). His band keeps its heavy swings.
            ChiefKit kit = chiefKits && encounter.Kind == EncounterKind.Chief ? KitUnder(encounter.Tribe, rules) : ChiefKit.None;
            if (kit != ChiefKit.None)
            {
                ChiefMove moves = ChiefMoves(encounter.Tribe, rules);
                return new SideRules(encounter.Tribe.ChiefEclipseGrade, (moves & ChiefKits.SpellMove(kit)) != 0 ? ChiefKits.SpellEveryTicks(kit) : 0,
                    false, heavy, spell, flood: flood, kit: kit, moves: moves);
            }
            switch (encounter.Kind)
            {
                case EncounterKind.Chief:
                case EncounterKind.AlphaHunt:
                    return new SideRules(encounter.Tribe.ChiefEclipseGrade, _tuning.ChiefEclipseTicks, false, heavy, spell, flood: flood);
                case EncounterKind.Duel:
                    return new SideRules(_tuning.DuelEclipseGrade, _tuning.DuelEclipseTicks, false, heavy, spell, flood: flood);
                default: return new SideRules(1, _tuning.TrialEclipseTicks, false, heavy, spell, flood: flood);
            }
        }

        // A named hunter's band (HunterTraits): their traits and weakness as flags, Quickstrike's faster swings, and the
        // move a cast trait winds up. The hunt after the last chief always winds up a big hit (11 Oct 2026, call J2): Big
        // Hit unless the hunter has another cast. From rules 6 its captain also runs the hunt (Battle.ChiefKit.Hunt). From
        // rules 8 (the parry at the center) Quickstrike is Double swing instead of faster swings (Battle.BandTrait.Double).
        private SideRules HuntersRules(Encounter encounter, int heavy, bool huntKit, bool parryCenter)
        {
            ChiefKit kit = huntKit ? ChiefKit.Hunt : ChiefKit.None;
            CampaignHunter hunter = encounter.Hunter;
            BandTrait band = hunter == null ? BandTrait.None : hunter.Band;
            if (hunter != null && hunter.Has(HunterTrait.Quickstrike))
            {
                if (parryCenter) band |= BandTrait.Double;
                else heavy = heavy * HunterTraits.QuickstrikePercent / 100;
            }
            ChiefSpell spell = ChiefSpell.Eclipse;
            bool casts = encounter.Kind == EncounterKind.AlphaHunt;
            if (hunter != null)
                for (int i = 0; i < hunter.Traits.Count; i++)
                    if (HunterTraits.IsCast(hunter.Traits[i]))
                    {
                        spell = HunterTraits.Spell(hunter.Traits[i]);
                        casts = true;
                    }
            if (!casts) return new SideRules(0, 0, false, heavy, ChiefSpell.Eclipse, band, kit: kit);
            return new SideRules(encounter.Tribe.ChiefEclipseGrade, _tuning.ChiefEclipseTicks, false, heavy, spell, band, kit: kit);
        }

        // Their fighters' strength in percent of a plain fighter's, for this fight.
        private int EnemyPercent(Encounter encounter)
        {
            int tier = encounter.Kind == EncounterKind.Tutorial ? 1 : encounter.Tribe.Tier;
            int kindPercent = encounter.Kind == EncounterKind.Trial && !encounter.EnemyCasts
                ? _tuning.PrecisionTrialPercent
                : encounter.Kind == EncounterKind.HoldTheLine && tier >= 2
                    ? _tuning.HoldPercentTier2
                    : _tuning.KindPercent(encounter.Kind);
            return _tuning.TierPercent(tier) * _tuning.EnemyScale(tier) / 100 * kindPercent / 100;
        }

        // A chief's reserves (Battle.ChiefFight, 20 Oct 2026): the next of his tribe's line up after his band, as strong
        // as his band, who join the fight at his second phase.
        private void AddReserves(List<UnitSpec> specs, Encounter encounter, string fightId, bool keepBleed)
        {
            CampaignTribe tribe = encounter.Tribe;
            IReadOnlyList<UnitClass> lineup = tribe.Lineup(encounter.Kind, encounter.Squad);
            int pct = EnemyPercent(encounter);
            for (int i = 0; i < ChiefFight.Reserves; i++)
            {
                int n = encounter.EnemyFighters + i;
                UnitClass c = lineup[n % lineup.Count];
                UnitSpec s = TribeTraits.Spec(fightId + "." + tribe.Id + "." + n.ToString(CultureInfo.InvariantCulture), c,
                    SliceClasses.Stats(c).DefaultRow, UnitRole.Fighter, pct, pct, 0, tribe.Id);
                if (HasOmen(Omen.BloodMoon)) s = WithPower(s, 100 + OmenRules.BloodMoonPercent, keepBleed);
                specs.Add(s.AsReserve());
            }
        }

        // The shark's sand pups (rules 27, Battle.BeastKits): after its parts, each BeastKits.PupHealthPercent of their average
        // health, at their power.
        private static void AddPups(List<UnitSpec> side1, string fightId)
        {
            int health = 0;
            for (int p = 0; p < BeastKits.PartCount; p++) health += side1[p].HealthPercent;
            health /= BeastKits.PartCount;
            for (int n = 1; n <= BeastKits.PupCount; n++)
                side1.Add(new UnitSpec(BeastKits.PupId(fightId, n), UnitClass.Skirmisher, Row.Front, UnitRole.Fighter, side1[0].PowerPercent,
                    Math.Max(1, health * BeastKits.PupHealthPercent / 100)));
        }

        private List<UnitSpec> EnemySpecs(Encounter encounter, string fightId, bool keepBleed)
        {
            CampaignTribe tribe = encounter.Tribe;
            int pct = EnemyPercent(encounter);
            var specs = new List<UnitSpec>();

            // Each tribe sends one of its three squads for this kind of fight, the one the road node shows (Jacob, 18 Oct
            // 2026). From 11 Oct it sent the same lineup every time, and before that it turned with the fights played.
            IReadOnlyList<UnitClass> lineup = tribe.Lineup(encounter.Kind, encounter.Squad);
            switch (encounter.Kind)
            {
                case EncounterKind.Tutorial:
                    AddFighters(specs, tribe, lineup, fightId, encounter.EnemyFighters, pct * _tuning.TutorialPercent / 100);
                    break;
                case EncounterKind.WarParty:
                case EncounterKind.HoldTheLine:
                    AddFighters(specs, tribe, lineup, fightId, encounter.EnemyFighters, pct);
                    break;
                case EncounterKind.Duel:
                {
                    UnitClass champion = DuelChampion(tribe);
                    specs.Add(TribeTraits.Spec(fightId + "." + tribe.Id + ".champion", champion, SliceClasses.Stats(champion).DefaultRow,
                        encounter.EnemyCasts ? UnitRole.Chief : UnitRole.Fighter, pct, pct, 0, tribe.Id));
                    break;
                }
                case EncounterKind.Trial:
                    AddFighters(specs, tribe, lineup, fightId, encounter.EnemyFighters, pct);
                    if (encounter.EnemyCasts)
                        specs.Add(TribeTraits.Spec(fightId + "." + tribe.Id + ".shaman", UnitClass.Medic, Row.Back, UnitRole.Chief,
                            pct, pct * _tuning.LieutenantHealthPercent / 100, 0, tribe.Id));
                    break;
                case EncounterKind.Chief:
                    AddFighters(specs, tribe, lineup, fightId, encounter.EnemyFighters, pct);
                    specs.Add(TribeTraits.Spec(fightId + "." + tribe.Id + ".chief", tribe.ChiefClass, Row.Back, UnitRole.Chief,
                        pct * _tuning.ChiefPowerPercent / 100, pct * _tuning.ChiefHealthPercent / 100, 0, tribe.Id));
                    break;
                case EncounterKind.AlphaHunt:
                case EncounterKind.Ambush:
                {
                    // The named hunter leads from the back with a chief's power and health (call J2), and the company's
                    // blades come with them. Every level makes a hunter HunterRules.PerLevelPercent stronger, compounding;
                    // the blades fight at the company's might, the hunter at their own level plus it (18 Oct 2026).
                    CampaignHunter hunter = encounter.Hunter;
                    AddFighters(specs, tribe, lineup, fightId, encounter.EnemyFighters, pct * HunterRules.LevelPercent(Might) / 100);
                    int level = HunterRules.LevelPercent(HunterLevel(hunter));
                    specs.Add(TribeTraits.Spec(fightId + "." + tribe.Id + ".captain", hunter == null ? tribe.ChiefClass : hunter.Class, Row.Back,
                        UnitRole.Chief, pct * _tuning.ChiefPowerPercent / 100 * level / 100, pct * _tuning.ChiefHealthPercent / 100 * level / 100, 0,
                        tribe.Id));
                    break;
                }
                case EncounterKind.Beast:
                {
                    // The beast's parts in their order (BeastKits.Parts), each with its own health at the fight's strength; their
                    // moves hit by plain power, which Blood Moon raises below (Battle.BeastKits, 21 and 22 Oct 2026).
                    IReadOnlyList<BeastPart> parts = BeastKits.Parts(encounter.Beast);
                    for (int p = 0; p < parts.Count; p++)
                        specs.Add(new UnitSpec(fightId + "." + BeastKits.Id(encounter.Beast) + "." + BeastKits.Name(parts[p]), UnitClass.Warden,
                            BeastKits.RowOf(parts[p]), UnitRole.Fighter, UnitSpec.PlainPercent,
                            Math.Max(1, pct * BeastKits.HealthPercent(parts[p]) / 100)));
                    break;
                }
            }
            // Blood Moon (13 Oct 2026): everyone on their side hits harder, the tutorial aside.
            if (HasOmen(Omen.BloodMoon) && encounter.Kind != EncounterKind.Tutorial)
                for (int i = 0; i < specs.Count; i++) specs[i] = WithPower(specs[i], 100 + OmenRules.BloodMoonPercent, keepBleed);
            return specs;
        }

        // The same unit, hitting this percent as hard. Before rules 3 (21 Oct 2026) it also lost its bleeding blades, by
        // mistake; an unfinished fight from then still plays that way.
        private static UnitSpec WithPower(UnitSpec s, int percent, bool keepBleed)
        {
            return new UnitSpec(s.Id, s.Class, s.Row, s.Role, s.PowerPercent * percent / 100, s.HealthPercent, s.FearPercent,
                s.WoundedBonusPercent, s.ArmorPercent, s.HitBonus, s.AvoidBonus, s.RiseBonus, s.KillNerve, s.BigHitArmorPercent,
                keepBleed ? s.BleedPercent : 0, s.Bend, s.Reserve);
        }

        // The road kit (rules 12): a Bloodridge fighter's blows burn, on either side (recruits bring their own element, Jacob,
        // 25 Oct 2026), and on a Bloodridge road the first fighters of theirs carry their chief's moves, a pair a fight: a war
        // party's Cleave and Brand, a hold the line's Cleave and Burning Ring, a trial's Brand and Burning Ring, and a duel's
        // champion Cleave and Brand.
        private void RoadKit(List<UnitSpec> side0, List<UnitSpec> side1, Encounter encounter)
        {
            for (int i = 0; i < side0.Count; i++)
            {
                CampaignFighter f = FindFighter(side0[i].Id);
                if (f != null && f.TribeId == CampaignTribes.BloodridgeId) side0[i] = side0[i].WithRoadKit(BurnRules.StackPercent, side0[i].Carries);
            }
            // The tutorial is practice: its raiders neither burn nor carry.
            if (encounter.Kind == EncounterKind.Tutorial || encounter.Tribe == null || encounter.Tribe.Id != CampaignTribes.BloodridgeId) return;
            ChiefMove first, second;
            switch (encounter.Kind)
            {
                case EncounterKind.WarParty: first = ChiefMove.Cleave; second = ChiefMove.Brand; break;
                case EncounterKind.HoldTheLine: first = ChiefMove.Cleave; second = ChiefMove.BurningRing; break;
                case EncounterKind.Trial: first = ChiefMove.Brand; second = ChiefMove.BurningRing; break;
                case EncounterKind.Duel: first = ChiefMove.Cleave | ChiefMove.Brand; second = ChiefMove.None; break;
                default: first = second = ChiefMove.None; break;
            }
            int carriers = 0;
            for (int i = 0; i < side1.Count; i++)
            {
                UnitSpec s = side1[i];
                bool fighter = s.Role == UnitRole.Fighter || encounter.Kind == EncounterKind.Duel;
                ChiefMove carries = !fighter || s.Reserve ? ChiefMove.None : carriers == 0 ? first : carriers == 1 ? second : ChiefMove.None;
                if (fighter && !s.Reserve) carriers++;
                side1[i] = s.WithRoadKit(s.Role == UnitRole.Chief && encounter.Kind == EncounterKind.Chief ? 0 : BurnRules.StackPercent, carries);
            }
        }

        // A raid chief fights alone (rules 10): his band leaves the fight, and he stands in for it with
        // Battle.RaidRules.RaidHealthPercent of his health.
        private static void MakeRaidChief(List<UnitSpec> specs)
        {
            for (int i = specs.Count - 1; i >= 0; i--)
            {
                UnitSpec s = specs[i];
                if (s.Role != UnitRole.Chief)
                {
                    specs.RemoveAt(i);
                    continue;
                }
                specs[i] = new UnitSpec(s.Id, s.Class, s.Row, s.Role, s.PowerPercent, s.HealthPercent * RaidRules.RaidHealthPercent / 100,
                    s.FearPercent, s.WoundedBonusPercent, s.ArmorPercent, s.HitBonus, s.AvoidBonus, s.RiseBonus, s.KillNerve,
                    s.BigHitArmorPercent, s.BleedPercent, s.Bend, s.Reserve);
            }
        }

        // The lineup in order; a fight that needs more fighters takes it from its start again.
        private static void AddFighters(List<UnitSpec> specs, CampaignTribe tribe, IReadOnlyList<UnitClass> classes, string fightId,
            int count, int pct)
        {
            for (int i = 0; i < count; i++)
            {
                UnitClass c = classes[i % classes.Count];
                specs.Add(TribeTraits.Spec(fightId + "." + tribe.Id + "." + i.ToString(CultureInfo.InvariantCulture), c,
                    SliceClasses.Stats(c).DefaultRow, UnitRole.Fighter, pct, pct, 0, tribe.Id));
            }
        }

        private static bool EndedOnTime(FightRecord record)
        {
            IReadOnlyList<BattleEvent> ev = record.Events;
            return ev.Count > 0 && ev[ev.Count - 1].Kind == BattleEventKind.End && ev[ev.Count - 1].Detail == "time";
        }

        private static int IndexOf(string tribeId)
        {
            for (int i = 0; i < CampaignTribes.All.Length; i++) if (CampaignTribes.All[i].Id == tribeId) return i;
            return -1;
        }

        private ulong NextSeed()
        {
            ulong high = _seeds.NextUInt();
            ulong low = _seeds.NextUInt();
            return (high << 32) | low;
        }
    }
}
