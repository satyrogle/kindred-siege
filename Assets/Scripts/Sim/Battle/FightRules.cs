using System;

namespace KindredSiege.Sim.Battle
{
    /// The fight rules Jacob chose on 12 and 13 Oct 2026 (DESIGN_LOCKS, "Game design, owner decisions,
    /// 12 and 13 Oct 2026") so that playing well beats spamming. Each rule is a switch and every switch
    /// is off by default, so older fights, their replays and their tests keep their numbers; the
    /// campaign turns them all on. Every number here is a starting value: Jacob's rule is build first,
    /// tune in the tuning weeks.
    ///
    /// Misses: any blow can miss. The attacker's Hit against the target's Avoid (class, then tribe)
    /// decides it. Focus Fire raises Hit on the focused enemy; a Hold raises Avoid on the holding side's
    /// front row. One roll from the fight's seeded stream, so replays stay exact. No chance is ever
    /// shown on screen (Jacob, 13 Oct: "Just show damage number"); a miss is a Miss event.
    ///
    /// Earned points: orders cost points you earn by playing well (a parry, a dodged cast, an
    /// interrupted cast, a kill, an enemy who panics, a combo), plus a slow trickle so nobody is
    /// stuck at zero (Jacob's idea, to kill spam). Points never go above the starting points.
    ///
    /// Perfect Hold: Hold in the last half second before a big hit lands and it is parried. Nothing
    /// lands and no nerve is lost, the caster staggers, and an Unleash straight after is the Riposte
    /// combo. A Hold at any other time works as before.
    ///
    /// Nerve snaps: each time a side loses another quarter of the nerve it started with, its most
    /// hurt fighter snaps. It panics (stops fighting for a while) or rises (hits harder for a while).
    /// The side's own Hold steadies it; the enemy's Unleash shakes it.
    ///
    /// Class spells (Jacob, 20 Oct 2026; DESIGN_LOCKS, "The squad's spells, marks and class pieces"): every class has
    /// its auto attack plus one spell only it has, and the spells leave marks other classes cash in (ClassSpells,
    /// BattleSimulation.Class.cs). Off unless a fight turns it on, like the four above.
    ///
    /// Chief rules (Jacob, 20 Oct 2026, later; ChiefFight, BattleSimulation.Chief.cs): a chief yields at a quarter health,
    /// his band guards him, and at half health his second phase brings his reserves in. Off unless a fight turns it on.
    ///
    /// Chief kits (Jacob, 20 Oct 2026, later still, and 21 Oct: "go do the chief build"; ChiefKits, BattleSimulation.Kit.cs):
    /// on top of the chief rules, a chief kneels when his nerve runs out instead of at a quarter health, and you spare or
    /// execute him; a chief with his own kit fights with his moves, and the side facing him earns Glory. Off unless a fight
    /// turns it on, and only with the chief rules.
    ///
    /// Beasts (Jacob, 20 Oct 2026, DESIGN_LOCKS "Beasts"; 21 Oct: "go, build the shark API with your six calls"; BeastKits,
    /// BattleSimulation.Beast.cs): a side whose rules carry a beast is that beast's parts, each a target with its own health and
    /// its own move. The beast has no nerve, so you beat it by breaking parts. Off unless a fight turns it on.
    ///
    /// Chief tactics (Jacob, 22 Oct 2026: "go, build the Skullpeak chief and the Bloodridge extras in the sim"; DESIGN_LOCKS
    /// "Chief fights": "The tactical part is its own system"): on top of the chief kits, each kit's tactical piece. Bloodridge
    /// shows each of his moves, its target and how hard 6 s before it is due; Skullpeak's band has jobs. Off unless a fight
    /// turns it on, and only with the chief kits.
    ///
    /// The parry at the center (Jacob, 24 Oct 2026: "go, build the parry at the center in the sim behind rules version 8";
    /// DESIGN_LOCKS "The parry at the center"; ParryRules, BattleSimulation.Parry.cs): every move with a tell has a beat, and
    /// every beat is a duel for nerve. Off unless a fight turns it on; campaign rules version 8 turns it on.
    ///
    /// The squad first (Jacob, 24 Oct 2026, later: "go, build rules 9 with your three picks as written"; Claude's proposal at
    /// HANDOFF_LOG line 10945; ParryRules, BattleSimulation.Parry.cs): on top of the parry at the center, a band's heavy swings
    /// are small beats (parryable, no duel), a Hold is a stance again (no block cost), points and nerve come more from the
    /// squad, and a rival's learned wait out and feint sit on one move. Off unless a fight turns it on; campaign rules version 9
    /// turns it on.
    ///
    /// The champion duel (Jacob, 30 Oct 2026: "Yes, design it", then "Yes, build it"; DESIGN_LOCKS "Duels"): in a duel the
    /// champion's heavy swing is a big beat again, a duel for nerve, and a champion whose nerve runs out yields; a Hold too
    /// early cracks your duelist's nerve; he springs your trap once. Off unless a fight turns it on; campaign rules version 16
    /// turns it on for duels.
    public sealed class FightRules
    {
        public static readonly FightRules Off = new FightRules(false, false, false, false);

        /// Every 13 Oct rule.
        public static readonly FightRules Skill = new FightRules(true, true, true, true);

        /// Every 13 Oct rule and the class spells: what the campaign plays from 20 Oct 2026.
        public static readonly FightRules Campaign = new FightRules(true, true, true, true, true);

        /// The same and the chief rules: what the campaign's chief fights played from 20 Oct 2026.
        public static readonly FightRules CampaignChief = new FightRules(true, true, true, true, true, true);

        /// The same and the chief kits: what the campaign's chief fights play from 21 Oct 2026.
        public static readonly FightRules CampaignChiefKit = new FightRules(true, true, true, true, true, true, true);

        /// The campaign's rules and the beasts: what a campaign beast fight plays from 21 Oct 2026.
        public static readonly FightRules CampaignBeast = new FightRules(true, true, true, true, true, false, false, true);

        /// The chief kits and their tactical pieces: what the campaign's chief fights play from 22 Oct 2026.
        public static readonly FightRules CampaignChiefTactics = new FightRules(true, true, true, true, true, true, true, false, true);

        public FightRules(bool misses, bool earnedPoints, bool perfectHold, bool nerveSnaps, bool classSpells = false,
            bool chiefRules = false, bool chiefKits = false, bool beasts = false, bool chiefTactics = false, bool parryCenter = false,
            bool squadFirst = false, bool raidChief = false, bool roadCombat = false, bool roadKit = false, int clearRoadStage = 0, bool squadScreen = false,
            bool championDuel = false, bool layers = false, bool marks = false, bool payoffs = false, bool danger = false,
            bool meter = false, bool floor = false, bool whisper = false, bool beastScenes = false, bool tight = false, bool beastRaid = false,
            bool shadeForms = false, bool glutton = false, bool duos = false, bool sized = false, bool flavours = false, bool vows = false,
            bool marksOnHits = false, bool duoMoves = false)
        {
            ChampionDuel = championDuel;
            Layers = layers;
            Marks = marks;
            Payoffs = payoffs;
            Danger = danger;
            Meter = meter;
            Floor = floor;
            Whisper = whisper;
            BeastScenes = beastScenes;
            Tight = tight;
            BeastRaid = beastRaid;
            ShadeForms = shadeForms;
            Glutton = glutton;
            Duos = duos;
            Sized = sized;
            Flavours = flavours;
            Vows = vows;
            MarksOnHits = marksOnHits;
            DuoMoves = duoMoves;
            Misses = misses;
            EarnedPoints = earnedPoints;
            PerfectHold = perfectHold;
            NerveSnaps = nerveSnaps;
            ClassSpells = classSpells;
            ChiefRules = chiefRules;
            ChiefKits = chiefKits;
            Beasts = beasts;
            ChiefTactics = chiefTactics;
            if (clearRoadStage < 0 || clearRoadStage > 4) throw new ArgumentOutOfRangeException(nameof(clearRoadStage));
            ClearRoadStage = clearRoadStage;
            SquadScreen = squadScreen;
            RoadKit = roadKit || clearRoadStage > 0;
            RoadCombat = roadCombat || RoadKit;
            RaidChief = raidChief || RoadCombat;
            SquadFirst = squadFirst || RaidChief;
            ParryCenter = parryCenter || SquadFirst;
        }

        public bool Misses { get; }
        public bool EarnedPoints { get; }
        public bool PerfectHold { get; }
        public bool NerveSnaps { get; }
        public bool ClassSpells { get; }
        public bool ChiefRules { get; }
        public bool ChiefKits { get; }
        public bool Beasts { get; }
        public bool ChiefTactics { get; }
        public bool ParryCenter { get; }
        public bool SquadFirst { get; }

        /// The raid chief (Jacob, 25 Oct 2026: "lets imagine our whole run like a raid bosses from World of Warcraft", then
        /// "show me in game those boss fights ... do 1 pass"; BattleSimulation.Raid.cs), on top of the squad first: your Hold is
        /// free and a parry pays no points, and a raid chief's bar refills like posture. Campaign rules version 10.
        public bool RaidChief { get; }

        /// Road combat (Jacob, 25 Oct 2026: "go, build rules 11 in the sim as written in 11263, 11358 and 11406";
        /// BattleSimulation.Road.cs), on top of the raid chief: in a road fight every fighter has his own nerve and the fight
        /// ends when one side has nobody left fighting; a mark readies a casher whose spell is at least half ready; pairs bond;
        /// the classes' defence moves. Campaign rules version 11.
        public bool RoadCombat { get; }

        /// The road kit (Jacob, 25 Oct 2026: "alright bro lets keep it as dodge and build these 5 lets go"; BattleSimulation.Boosts.cs,
        /// Burn.cs and Carriers.cs), on top of road combat: Fall Back is a pure dodge (nobody of the falling side attacks), every
        /// class's automatic boost, Burn on a burning fighter's blows and the Aimed Shot's flare, and a road fighter carrying a
        /// small version of his chief's move. Campaign rules version 12.
        public bool RoadKit { get; }

        /// Rules 13: 0 retains historical behaviour; 1 combos, 2 defence, 3 boosts, 4 carriers.
        /// Stored in the fight setup, so replay never consults a live campaign's progress.
        public int ClearRoadStage { get; }
        /// Rules 14: player leader protection and explicit, confirmed teamwork events.
        public bool SquadScreen { get; }

        /// Rules 16, a duel only: the champion duel (DESIGN_LOCKS, "Duels").
        public bool ChampionDuel { get; }

        /// These rules for a champion duel: what a duel plays from campaign rules version 16.
        public FightRules WithChampionDuel() => ChampionDuel ? this : new FightRules(Misses, EarnedPoints, PerfectHold, NerveSnaps,
            ClassSpells, ChiefRules, ChiefKits, Beasts, ChiefTactics, ParryCenter, SquadFirst, RaidChief, RoadCombat, RoadKit,
            ClearRoadStage, SquadScreen, true, Layers, Marks, Payoffs, Danger, Meter, Floor, Whisper, BeastScenes, Tight, BeastRaid, ShadeForms, Glutton, Duos, Sized, Flavours, Vows, MarksOnHits, DuoMoves);

        /// Rules 17: the damage layers (BattleSimulation.Layers.cs; DESIGN_LOCKS, "Builds: one theme per Pal", step 2). Every hit
        /// and heal goes through one pipeline: bonuses of one layer add, the layers multiply, and it rounds once.
        public bool Layers { get; }

        /// These rules with the damage layers: what every fight plays from campaign rules version 17. The other With rules
        /// drop it, so it comes last, as the champion duel does.
        public FightRules WithLayers() => Layers ? this : new FightRules(Misses, EarnedPoints, PerfectHold, NerveSnaps,
            ClassSpells, ChiefRules, ChiefKits, Beasts, ChiefTactics, ParryCenter, SquadFirst, RaidChief, RoadCombat, RoadKit,
            ClearRoadStage, SquadScreen, ChampionDuel, true, Marks, Payoffs, Danger, Meter, Floor, Whisper, BeastScenes, Tight, BeastRaid, ShadeForms, Glutton, Duos, Sized, Flavours, Vows, MarksOnHits, DuoMoves);

        /// Rules 18: the Butcher's theme (BattleSimulation.Marks.cs; DESIGN_LOCKS, "The Butcher's theme", builds step 3). His
        /// combos lay Crush, Exposed and Stasis, which stack to 3 and burst.
        public bool Marks { get; }

        /// These rules with the Butcher's marks: what every fight plays from campaign rules version 18. Last, after the layers.
        public FightRules WithMarks() => Marks ? this : new FightRules(Misses, EarnedPoints, PerfectHold, NerveSnaps,
            ClassSpells, ChiefRules, ChiefKits, Beasts, ChiefTactics, ParryCenter, SquadFirst, RaidChief, RoadCombat, RoadKit,
            ClearRoadStage, SquadScreen, ChampionDuel, Layers, true, Payoffs, Danger, Meter, Floor, Whisper, BeastScenes, Tight, BeastRaid, ShadeForms, Glutton, Duos, Sized, Flavours, Vows, MarksOnHits, DuoMoves);

        /// Rules 20: payoffs that spread (BattleSimulation.Payoffs.cs; Jacob's playtest, HANDOFF_LOG 12797, fix 1). A snare pays
        /// one Aimed Shot crit and breaks, and bleeding counts at most PayoffRules.PercentHealthCap of a big target's health.
        public bool Payoffs { get; }

        /// These rules with payoffs that spread: what every fight plays from campaign rules version 20. Last, after the marks.
        public FightRules WithPayoffs() => Payoffs ? this : new FightRules(Misses, EarnedPoints, PerfectHold, NerveSnaps,
            ClassSpells, ChiefRules, ChiefKits, Beasts, ChiefTactics, ParryCenter, SquadFirst, RaidChief, RoadCombat, RoadKit,
            ClearRoadStage, SquadScreen, ChampionDuel, Layers, Marks, true, Danger, Meter, Floor, Whisper, BeastScenes, Tight, BeastRaid, ShadeForms, Glutton, Duos, Sized, Flavours, Vows, MarksOnHits, DuoMoves);

        /// Rules 21: danger (BattleSimulation.Danger.cs; DESIGN_LOCKS, "Jacob's playtest fixes 3, 7 and 9"). A Hold that is not a
        /// parry soaks nothing from a blast, Bloodridge's road bands carry their chief's moves from the first fight and hit for a
        /// third, and the chief's swings and blasts take turns.
        public bool Danger { get; }

        /// These rules with danger: what every fight plays from campaign rules version 21. Last, after payoffs.
        public FightRules WithDanger() => Danger ? this : new FightRules(Misses, EarnedPoints, PerfectHold, NerveSnaps,
            ClassSpells, ChiefRules, ChiefKits, Beasts, ChiefTactics, ParryCenter, SquadFirst, RaidChief, RoadCombat, RoadKit,
            ClearRoadStage, SquadScreen, ChampionDuel, Layers, Marks, Payoffs, true, Meter, Floor, Whisper, BeastScenes, Tight, BeastRaid, ShadeForms, Glutton, Duos, Sized, Flavours, Vows, MarksOnHits, DuoMoves);

        /// Rules 22: the parry meter (BattleSimulation.Meter.cs; DESIGN_LOCKS, "The parry meter"). Your parries fill 3 bars, your
        /// chief spell is cast with all 3, and each held bar makes your combos and cash ins 10% stronger.
        public bool Meter { get; }

        /// These rules with the parry meter: what every fight plays from campaign rules version 22. Last, after danger.
        public FightRules WithMeter() => Meter ? this : new FightRules(Misses, EarnedPoints, PerfectHold, NerveSnaps,
            ClassSpells, ChiefRules, ChiefKits, Beasts, ChiefTactics, ParryCenter, SquadFirst, RaidChief, RoadCombat, RoadKit,
            ClearRoadStage, SquadScreen, ChampionDuel, Layers, Marks, Payoffs, Danger, true, Floor, Whisper, BeastScenes, Tight, BeastRaid, ShadeForms, Glutton, Duos, Sized, Flavours, Vows, MarksOnHits, DuoMoves);

        /// Rules 23: the floor under the three Pals (BattleSimulation.Shade.cs; DESIGN_LOCKS, "The floor under the three Pals"): a
        /// fighter's second piece, the faster and cheaper hooks and the Shade's tricks.
        public bool Floor { get; }

        /// These rules with the floor: what every fight plays from campaign rules version 23. Last, after the meter.
        public FightRules WithFloor() => Floor ? this : new FightRules(Misses, EarnedPoints, PerfectHold, NerveSnaps,
            ClassSpells, ChiefRules, ChiefKits, Beasts, ChiefTactics, ParryCenter, SquadFirst, RaidChief, RoadCombat, RoadKit,
            ClearRoadStage, SquadScreen, ChampionDuel, Layers, Marks, Payoffs, Danger, Meter, true, Whisper, BeastScenes, Tight, BeastRaid, ShadeForms, Glutton, Duos, Sized, Flavours, Vows, MarksOnHits, DuoMoves);

        /// Rules 24: the Whisperer (BattleSimulation.Whisperer.cs; DESIGN_LOCKS, "Habits come with the game, the Whisperer upgrades
        /// them"): each class's habit and its upgrade, the squad's pair on the bonds, and a fighter's rank 2.
        public bool Whisper { get; }

        /// These rules with the Whisperer: what every fight plays from campaign rules version 24. Last, after the floor.
        public FightRules WithWhisper() => Whisper ? this : new FightRules(Misses, EarnedPoints, PerfectHold, NerveSnaps,
            ClassSpells, ChiefRules, ChiefKits, Beasts, ChiefTactics, ParryCenter, SquadFirst, RaidChief, RoadCombat, RoadKit,
            ClearRoadStage, SquadScreen, ChampionDuel, Layers, Marks, Payoffs, Danger, Meter, Floor, true, BeastScenes, Tight, BeastRaid, ShadeForms, Glutton, Duos, Sized, Flavours, Vows, MarksOnHits, DuoMoves);

        /// Rules 25: the beasts as their approved scenes show them (Jacob, 1 Nov 2026: "Sim follows scenes", then "Go as written";
        /// BattleSimulation.Beast.cs). The shark's Bite takes the marked one's whole row and its tail's Sweep all of them; the
        /// cage bear's Grab is a Slam and Grab, an ice wave on all of them and then the claw. A beast fight only.
        public bool BeastScenes { get; }

        /// These rules with the beasts as their scenes show them: what a beast fight plays from campaign rules version 25. Last,
        /// after the Whisperer.
        public FightRules WithBeastScenes() => BeastScenes ? this : new FightRules(Misses, EarnedPoints, PerfectHold, NerveSnaps,
            ClassSpells, ChiefRules, ChiefKits, Beasts, ChiefTactics, ParryCenter, SquadFirst, RaidChief, RoadCombat, RoadKit,
            ClearRoadStage, SquadScreen, ChampionDuel, Layers, Marks, Payoffs, Danger, Meter, Floor, Whisper, true, Tight, BeastRaid, ShadeForms, Glutton, Duos, Sized, Flavours, Vows, MarksOnHits, DuoMoves);

        /// Rules 26: the tight parry (BattleSimulation.Tight.cs; DESIGN_LOCKS, "The parry: tight window, big payoff, a gold flash
        /// on the attacker"): a 0.2 s window, a whiff that lands in full and opens your side, and a counter on every parry.
        public bool Tight { get; }

        /// These rules with the tight parry: what every fight plays from campaign rules version 26. Last, after the beasts.
        public FightRules WithTight() => Tight ? this : new FightRules(Misses, EarnedPoints, PerfectHold, NerveSnaps,
            ClassSpells, ChiefRules, ChiefKits, Beasts, ChiefTactics, ParryCenter, SquadFirst, RaidChief, RoadCombat, RoadKit,
            ClearRoadStage, SquadScreen, ChampionDuel, Layers, Marks, Payoffs, Danger, Meter, Floor, Whisper, BeastScenes, true, BeastRaid, ShadeForms, Glutton, Duos, Sized, Flavours, Vows, MarksOnHits, DuoMoves);

        /// Rules 27: the beasts as raid bosses (Jacob, 1 Nov 2026: "can we finish the bear and shark just like a wow boss", then
        /// "Full plan" and "Go as written"; BattleSimulation.BeastRaid.cs). The shark's Feeding frenzy at its second broken part, then
        /// its sand pups; the cage bear's Overload when its ribcage breaks, and its Last stand Pulse climbing. A beast fight only.
        /// Built as rules 26 and renumbered to 27 the same day, after the tight parry took 26.
        public bool BeastRaid { get; }

        /// These rules with the beasts as raid bosses: what a beast fight plays from campaign rules version 27. Last, after the
        /// tight parry.
        public FightRules WithBeastRaid() => BeastRaid ? this : new FightRules(Misses, EarnedPoints, PerfectHold, NerveSnaps,
            ClassSpells, ChiefRules, ChiefKits, Beasts, ChiefTactics, ParryCenter, SquadFirst, RaidChief, RoadCombat, RoadKit,
            ClearRoadStage, SquadScreen, ChampionDuel, Layers, Marks, Payoffs, Danger, Meter, Floor, Whisper, BeastScenes, Tight, true, ShadeForms, Glutton, Duos, Sized, Flavours, Vows, MarksOnHits, DuoMoves);

        /// Rules 28: the Shade's forms (BattleSimulation.Shade.cs; Jacob in chat, 31 Oct 2026: "go" on Claude's Shade design):
        /// Sanctuary's Relay and Last Refuge, Backstab's Twin Knives and Shadow Strike, Fear's Spreading Dread and Turncoat, and
        /// each route's price.
        public bool ShadeForms { get; }

        /// These rules with the Shade's forms: what every fight plays from campaign rules version 28. Last, after the Whisperer.
        public FightRules WithShadeForms() => ShadeForms ? this : new FightRules(Misses, EarnedPoints, PerfectHold, NerveSnaps,
            ClassSpells, ChiefRules, ChiefKits, Beasts, ChiefTactics, ParryCenter, SquadFirst, RaidChief, RoadCombat, RoadKit,
            ClearRoadStage, SquadScreen, ChampionDuel, Layers, Marks, Payoffs, Danger, Meter, Floor, Whisper, BeastScenes, Tight, BeastRaid, true, Glutton, Duos, Sized, Flavours, Vows, MarksOnHits, DuoMoves);
        /// Rules 29: the Glutton's tree (BattleSimulation.Glutton.cs; Jacob in chat, 1 Nov 2026: "go" on Claude's calls): each
        /// side's talents make what it has bigger, longer, faster or cheaper.
        public bool Glutton { get; }

        /// These rules with the Glutton: what every fight plays from campaign rules version 29. Last, after the Shade's forms.
        public FightRules WithGlutton() => Glutton ? this : new FightRules(Misses, EarnedPoints, PerfectHold, NerveSnaps,
            ClassSpells, ChiefRules, ChiefKits, Beasts, ChiefTactics, ParryCenter, SquadFirst, RaidChief, RoadCombat, RoadKit,
            ClearRoadStage, SquadScreen, ChampionDuel, Layers, Marks, Payoffs, Danger, Meter, Floor, Whisper, BeastScenes, Tight, BeastRaid, ShadeForms, true, Duos, Sized, Flavours, Vows, MarksOnHits, DuoMoves);

        /// Rules 30: the Pals' duos (BattleSimulation.Duos.cs; Jacob in chat, 1 Nov 2026: "go write them"): a pick that joins two
        /// Pals' themes, held in the side's PalKit.
        public bool Duos { get; }

        /// These rules with the duos: what every fight plays from campaign rules version 30. Last, after the Glutton.
        public FightRules WithDuos() => Duos ? this : new FightRules(Misses, EarnedPoints, PerfectHold, NerveSnaps,
            ClassSpells, ChiefRules, ChiefKits, Beasts, ChiefTactics, ParryCenter, SquadFirst, RaidChief, RoadCombat, RoadKit,
            ClearRoadStage, SquadScreen, ChampionDuel, Layers, Marks, Payoffs, Danger, Meter, Floor, Whisper, BeastScenes, Tight, BeastRaid, ShadeForms, Glutton, true, Sized, Flavours, Vows, MarksOnHits, DuoMoves);

        /// Rules 31: the sizing pass (BattleSimulation.Sized.cs; Jacob in chat, 1 Nov 2026: "okay go"): the Pals' picks sized to
        /// decide fights.
        public bool Sized { get; }

        /// These rules with the sized picks: what every fight plays from campaign rules version 31. Last, after the duos.
        public FightRules WithSized() => Sized ? this : new FightRules(Misses, EarnedPoints, PerfectHold, NerveSnaps,
            ClassSpells, ChiefRules, ChiefKits, Beasts, ChiefTactics, ParryCenter, SquadFirst, RaidChief, RoadCombat, RoadKit,
            ClearRoadStage, SquadScreen, ChampionDuel, Layers, Marks, Payoffs, Danger, Meter, Floor, Whisper, BeastScenes, Tight, BeastRaid, ShadeForms, Glutton, Duos, true, Flavours, Vows, MarksOnHits, DuoMoves);

        /// Rules 32: the Butcher's flavours (BattleSimulation.Flavours.cs; Jacob in chat, 2 Nov 2026: "both 1 and 2"): a combo grows
        /// with the combos of its flavour the side wears, and three ranks of one flavour unlock its final form.
        public bool Flavours { get; }

        /// These rules with the Butcher's flavours: what every fight plays from campaign rules version 32. Last, after sizing.
        public FightRules WithFlavours() => Flavours ? this : new FightRules(Misses, EarnedPoints, PerfectHold, NerveSnaps,
            ClassSpells, ChiefRules, ChiefKits, Beasts, ChiefTactics, ParryCenter, SquadFirst, RaidChief, RoadCombat, RoadKit,
            ClearRoadStage, SquadScreen, ChampionDuel, Layers, Marks, Payoffs, Danger, Meter, Floor, Whisper, BeastScenes, Tight, BeastRaid, ShadeForms, Glutton, Duos, Sized, true, Vows, MarksOnHits, DuoMoves);

        /// Rules 33: the Whisperer's vows (BattleSimulation.Vows.cs; Jacob in chat, 2 Nov 2026: "whisperer - 1"): a fighter's vow
        /// changes how his spell works from rank 2.
        public bool Vows { get; }

        /// Rules 33 too: the Butcher's marks on every hit (BattleSimulation.Flavours.cs; Jacob in chat, 2 Nov 2026: "butcher - 1
        /// and 2"): every blow of a fighter wearing a combo lays a stack of its flavour.
        public bool MarksOnHits { get; }

        /// These rules with both: what every fight plays from campaign rules version 33. Last, after the flavours.
        public FightRules WithVows() => Vows && MarksOnHits ? this : new FightRules(Misses, EarnedPoints, PerfectHold, NerveSnaps,
            ClassSpells, ChiefRules, ChiefKits, Beasts, ChiefTactics, ParryCenter, SquadFirst, RaidChief, RoadCombat, RoadKit,
            ClearRoadStage, SquadScreen, ChampionDuel, Layers, Marks, Payoffs, Danger, Meter, Floor, Whisper, BeastScenes, Tight, BeastRaid, ShadeForms, Glutton, Duos, Sized,
            Flavours, true, true, DuoMoves);

        /// Rules 34: the duos that change a move (BattleSimulation.DuoMoves.cs; Jacob in chat, 2 Nov 2026: "merge first, then
        /// build the duos on top. Go"): Blood Parry, Chain Reaction, Executioner, Endless Unleash, Shadow Twin and Gorge, each
        /// growing with the streak of big moves answered, each with its catch.
        public bool DuoMoves { get; }

        /// These rules with the new duos: what every fight plays from campaign rules version 34. Last, after the vows.
        public FightRules WithDuoMoves() => DuoMoves ? this : new FightRules(Misses, EarnedPoints, PerfectHold, NerveSnaps,
            ClassSpells, ChiefRules, ChiefKits, Beasts, ChiefTactics, ParryCenter, SquadFirst, RaidChief, RoadCombat, RoadKit,
            ClearRoadStage, SquadScreen, ChampionDuel, Layers, Marks, Payoffs, Danger, Meter, Floor, Whisper, BeastScenes, Tight, BeastRaid,
            ShadeForms, Glutton, Duos, Sized, Flavours, Vows, MarksOnHits, true);
        public FightRules WithSquadScreen() => new FightRules(Misses, EarnedPoints, PerfectHold, NerveSnaps,
            ClassSpells, ChiefRules, ChiefKits, Beasts, ChiefTactics, true, true, true, true, true,
            ClearRoadStage > 0 ? ClearRoadStage : 4, true);
        public FightRules WithClearRoad(int stage = 4)
        {
            if (stage < 1 || stage > 4) throw new ArgumentOutOfRangeException(nameof(stage));
            return new FightRules(Misses, EarnedPoints, PerfectHold, NerveSnaps, ClassSpells, ChiefRules, ChiefKits,
                Beasts, ChiefTactics, true, true, true, true, true, stage, SquadScreen);
        }

        /// Any of the four 13 Oct rules.
        public bool AnySkill => Misses || EarnedPoints || PerfectHold || NerveSnaps;
        public bool IsNone => !AnySkill && !ClassSpells && !ChiefRules && !ChiefKits && !Beasts && !ChiefTactics && !ParryCenter && !SquadFirst &&
            !RaidChief && !RoadCombat && !RoadKit && !Layers && !Marks && !Payoffs && !Danger && !Meter && !Floor;

        /// These rules with the parry at the center on: what campaign rules version 8 plays (24 Oct 2026).
        public FightRules WithParryCenter()
        {
            return ParryCenter ? this : new FightRules(Misses, EarnedPoints, PerfectHold, NerveSnaps, ClassSpells, ChiefRules, ChiefKits,
                Beasts, ChiefTactics, true, SquadFirst, RaidChief, RoadCombat, RoadKit);
        }

        /// These rules with the squad first on (and the parry at the center under it): what campaign rules version 9 plays.
        public FightRules WithSquadFirst()
        {
            return SquadFirst ? this : new FightRules(Misses, EarnedPoints, PerfectHold, NerveSnaps, ClassSpells, ChiefRules, ChiefKits,
                Beasts, ChiefTactics, true, true, RaidChief, RoadCombat, RoadKit);
        }

        /// These rules with the raid chief on (and the squad first under it): what campaign rules version 10 plays.
        public FightRules WithRaidChief()
        {
            return RaidChief ? this : new FightRules(Misses, EarnedPoints, PerfectHold, NerveSnaps, ClassSpells, ChiefRules, ChiefKits,
                Beasts, ChiefTactics, true, true, true, RoadCombat, RoadKit);
        }

        /// These rules with road combat on (and the raid chief under it): what campaign rules version 11 plays.
        public FightRules WithRoadCombat()
        {
            return RoadCombat ? this : new FightRules(Misses, EarnedPoints, PerfectHold, NerveSnaps, ClassSpells, ChiefRules, ChiefKits,
                Beasts, ChiefTactics, true, true, true, true, RoadKit);
        }

        /// These rules with the road kit on (and road combat under it): what campaign rules version 12 plays.
        public FightRules WithRoadKit()
        {
            return RoadKit ? this : new FightRules(Misses, EarnedPoints, PerfectHold, NerveSnaps, ClassSpells, ChiefRules, ChiefKits,
                Beasts, ChiefTactics, true, true, true, true, true);
        }

        // ------------------------------------------------------------ misses

        /// A blow never lands less often than this, nor more often than MaxHitPercent.
        public const int MinHitPercent = 40;
        public const int MaxHitPercent = 98;

        /// Focus Fire adds this to Hit on the focused enemy; a Hold adds HoldAvoidBonus to the
        /// holding side's front row's Avoid.
        public const int FocusHitBonus = 10;
        public const int HoldAvoidBonus = 15;

        public const string MissDetail = "miss";

        // ------------------------------------------------------------ earned points

        /// The slow trickle: a point back every this many refill periods instead of every one.
        public const int TrickleFactor = 3;

        public const int ParryPoints = 2;
        public const int DodgePoints = 1;
        public const int InterruptPoints = 1;
        public const int KillPoints = 1;
        public const int PanicPoints = 1;
        public const int ComboPoints = 1;

        /// Why a side earned points, the detail of its Earn event.
        public const string ParryReason = "parry";
        public const string DodgeReason = "dodge";
        public const string InterruptReason = "interrupt";
        public const string KillReason = "kill";
        public const string PanicReason = "panic";
        public const string ComboReason = "combo";

        // ------------------------------------------------------------ Perfect Hold

        /// A Hold given this many ticks or fewer before a big hit lands parries it (half a second).
        public const int PerfectHoldTicks = 15;

        /// A parried caster loses this many ticks.
        public const int StaggerTicks = 60;

        /// The Release detail of a parried big hit.
        public const string Parried = "parried";

        /// Parry, then an Unleash within RiposteWindowTicks: the staggered caster takes this percent
        /// of its max health and the enemy side loses RiposteNerveCost nerve.
        public const string Riposte = "Riposte";
        public const int RiposteWindowTicks = 60;
        public const int RipostePercent = 12;
        public const int RiposteNerveCost = 120;
        public const string RiposteDetail = "riposte";

        // ------------------------------------------------------------ nerve snaps

        /// Snaps come with each quarter of a side's starting nerve lost, three at most a fight.
        public const int SnapsPerFight = 3;

        public const int SnapTicks = 90;

        /// A risen fighter's blows hit this percent as hard.
        public const int RiseDamagePercent = 150;

        /// The chance a snap is a rise: this, plus SteadyRiseBonus while its side holds, minus
        /// ShakenRisePenalty while the enemy unleashes. The rest panic.
        public const int BaseRisePercent = 50;
        public const int SteadyRiseBonus = 25;
        public const int ShakenRisePenalty = 25;

        public const string Panic = "panic";
        public const string Rise = "rise";

        public void AppendTo(SnapshotBuilder b)
        {
            b.Field("rules.misses", Misses);
            b.Field("rules.earned", EarnedPoints);
            b.Field("rules.perfectHold", PerfectHold);
            b.Field("rules.snaps", NerveSnaps);
            // Written only when on, so every older fight keeps its checksum.
            if (ClassSpells) b.Field("rules.classSpells", true);
            if (ChiefRules) b.Field("rules.chiefRules", true);
            if (ChiefKits) b.Field("rules.chiefKits", true);
            if (Beasts) b.Field("rules.beasts", true);
            if (ChiefTactics) b.Field("rules.chiefTactics", true);
            if (ParryCenter) b.Field("rules.parryCenter", true);
            if (SquadFirst) b.Field("rules.squadFirst", true);
            if (RaidChief) b.Field("rules.raidChief", true);
            if (RoadCombat) b.Field("rules.roadCombat", true);
            if (RoadKit) b.Field("rules.roadKit", true);
            if (ClearRoadStage > 0) b.Field("rules.clearRoadStage", ClearRoadStage);
            if (SquadScreen) b.Field("rules.squadScreen", true);
            if (ChampionDuel) b.Field("rules.championDuel", true);
            if (Layers) b.Field("rules.layers", true);
            if (Marks) b.Field("rules.marks", true);
            if (Payoffs) b.Field("rules.payoffs", true);
            if (Danger) b.Field("rules.danger", true);
            if (Meter) b.Field("rules.meter", true);
            if (Floor) b.Field("rules.floor", true);
            if (Whisper) b.Field("rules.whisper", true);
            if (BeastScenes) b.Field("rules.beastScenes", true);
            if (Tight) b.Field("rules.tight", true);
            if (BeastRaid) b.Field("rules.beastRaid", true);
            if (ShadeForms) b.Field("rules.shadeForms", true);
            if (Glutton) b.Field("rules.glutton", true);
            if (Duos) b.Field("rules.duos", true);
            if (Sized) b.Field("rules.sized", true);
            if (Flavours) b.Field("rules.flavours", true);
            if (Vows) b.Field("rules.vows", true);
            if (MarksOnHits) b.Field("rules.marksOnHits", true);
            if (DuoMoves) b.Field("rules.duoMoves", true);
        }
    }

    /// The next order that finishes a combo a side has started, for the screen (13 Oct 2026). Step is
    /// how many of its orders are given, Steps how many it takes, TicksLeft how long the next one has.
    public readonly struct ComboHint
    {
        public static readonly ComboHint None = new ComboHint(string.Empty, Directive.Hold, 0, 0, 0);

        public ComboHint(string combo, Directive next, int ticksLeft, int step, int steps)
        {
            Combo = combo ?? string.Empty;
            Next = next;
            TicksLeft = Math.Max(0, ticksLeft);
            Step = step;
            Steps = steps;
        }

        public string Combo { get; }
        public Directive Next { get; }
        public int TicksLeft { get; }
        public int Step { get; }
        public int Steps { get; }
        public bool IsNone => Combo.Length == 0;
    }
}
