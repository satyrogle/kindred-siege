using System;
using System.Collections.Generic;
using System.Globalization;
using KindredSiege.Sim.Battle;

namespace KindredSiege.Sim.Learning
{
    /// The class pieces the Butcher forges from beast trophies (Jacob, 20 Oct 2026: "the Butcher forges one part into one class
    /// piece. No fusing two parts"). The sand shark's are the Clarth's signature pieces (3 Oct 2026: they come from the shark
    /// they hunt); the cage bear's are Skullpeak's bionic pieces (DESIGN_LOCKS, the Skullpeak lock: the Warden's grappling
    /// claw arm and tower shield, the Skirmisher's blade arm, the Trapper's hook on a chain, the Marksman's rifle arm, the
    /// Medic's injector vials).
    public enum BeastPiece
    {
        JawShield = 0,
        SpineRifle = 1,
        FinBlades = 2,
        ToothNet = 3,
        ToothTotem = 4,
        ClawArm = 5,
        TowerShield = 6,
        BladeArm = 7,
        HookChain = 8,
        RifleArm = 9,
        InjectorVials = 10
    }

    /// Which trophy forges into which piece, and what each piece is. The shark (Jacob's tail call, 21 Oct 2026): the jaw makes
    /// the jaw shield, the tooth net or the tooth totem staff, since the teeth are in the jaw; the fin makes the fin blades; the
    /// spine the spine rifle; the tail, the hardest part, any one of the five. The cage bear (his "go, build the Cage Bear as
    /// planned", 22 Oct 2026): the claw makes the claw arm, the blade arm or the hook chain; the ribcage the tower shield; the
    /// maw the rifle arm; the heart the injector vials, or any one of the six. Each piece is a Princess class piece of its class
    /// with a set bend. The bends, stats and the Princess tier are Claude's starting values.
    public static class BeastPieces
    {
        public const int Count = 11;

        public static readonly BeastPiece[] All =
        {
            BeastPiece.JawShield, BeastPiece.SpineRifle, BeastPiece.FinBlades, BeastPiece.ToothNet, BeastPiece.ToothTotem,
            BeastPiece.ClawArm, BeastPiece.TowerShield, BeastPiece.BladeArm, BeastPiece.HookChain, BeastPiece.RifleArm,
            BeastPiece.InjectorVials
        };

        private static readonly BeastPiece[] None = new BeastPiece[0];

        private static readonly BeastPiece[] SharkPieces =
            { BeastPiece.JawShield, BeastPiece.SpineRifle, BeastPiece.FinBlades, BeastPiece.ToothNet, BeastPiece.ToothTotem };

        private static readonly BeastPiece[] BearPieces =
        {
            BeastPiece.ClawArm, BeastPiece.TowerShield, BeastPiece.BladeArm, BeastPiece.HookChain, BeastPiece.RifleArm,
            BeastPiece.InjectorVials
        };

        private static readonly BeastPiece[] FromJaw = { BeastPiece.JawShield, BeastPiece.ToothNet, BeastPiece.ToothTotem };
        private static readonly BeastPiece[] FromFin = { BeastPiece.FinBlades };
        private static readonly BeastPiece[] FromSpine = { BeastPiece.SpineRifle };
        private static readonly BeastPiece[] FromClaw = { BeastPiece.ClawArm, BeastPiece.BladeArm, BeastPiece.HookChain };
        private static readonly BeastPiece[] FromMaw = { BeastPiece.RifleArm };
        private static readonly BeastPiece[] FromRibcage = { BeastPiece.TowerShield };

        /// A beast's pieces.
        public static IReadOnlyList<BeastPiece> Of(BeastKit kit)
        {
            switch (kit)
            {
                case BeastKit.SandShark: return SharkPieces;
                case BeastKit.CageBear: return BearPieces;
                default: return None;
            }
        }

        /// The pieces a trophy of this part can be forged into.
        public static IReadOnlyList<BeastPiece> From(BeastPart part)
        {
            switch (part)
            {
                case BeastPart.Jaw: return FromJaw;
                case BeastPart.Fin: return FromFin;
                case BeastPart.Spine: return FromSpine;
                case BeastPart.Tail: return SharkPieces;
                case BeastPart.Claw: return FromClaw;
                case BeastPart.Maw: return FromMaw;
                case BeastPart.Ribcage: return FromRibcage;
                default: return BearPieces;
            }
        }

        public static bool IsFrom(BeastPart part, BeastPiece piece)
        {
            IReadOnlyList<BeastPiece> from = From(part);
            for (int i = 0; i < from.Count; i++) if (from[i] == piece) return true;
            return false;
        }

        /// The beast a piece is forged from.
        public static BeastKit KitOf(BeastPiece piece)
        {
            return (int)piece >= (int)BeastPiece.ClawArm ? BeastKit.CageBear : BeastKit.SandShark;
        }

        public static UnitClass ClassOf(BeastPiece piece)
        {
            switch (piece)
            {
                case BeastPiece.JawShield:
                case BeastPiece.ClawArm:
                case BeastPiece.TowerShield:
                    return UnitClass.Warden;
                case BeastPiece.SpineRifle:
                case BeastPiece.RifleArm:
                    return UnitClass.Marksman;
                case BeastPiece.FinBlades:
                case BeastPiece.BladeArm:
                    return UnitClass.Skirmisher;
                case BeastPiece.ToothNet:
                case BeastPiece.HookChain:
                    return UnitClass.Trapper;
                default:
                    return UnitClass.Medic;
            }
        }

        public static string Name(BeastPiece piece)
        {
            switch (piece)
            {
                case BeastPiece.JawShield: return "Jaw Shield";
                case BeastPiece.SpineRifle: return "Spine Rifle";
                case BeastPiece.FinBlades: return "Fin Blades";
                case BeastPiece.ToothNet: return "Tooth Net";
                case BeastPiece.ToothTotem: return "Tooth Totem Staff";
                case BeastPiece.ClawArm: return "Claw Arm";
                case BeastPiece.TowerShield: return "Tower Shield";
                case BeastPiece.BladeArm: return "Blade Arm";
                case BeastPiece.HookChain: return "Hook Chain";
                case BeastPiece.RifleArm: return "Rifle Arm";
                default: return "Injector Vials";
            }
        }

        public static GearStat StatOf(BeastPiece piece)
        {
            switch (piece)
            {
                case BeastPiece.JawShield: return GearStat.Health;
                case BeastPiece.SpineRifle: return GearStat.Damage;
                case BeastPiece.FinBlades: return GearStat.Damage;
                case BeastPiece.ToothNet: return GearStat.Hit;
                case BeastPiece.ToothTotem: return GearStat.Health;
                case BeastPiece.ClawArm: return GearStat.Damage;
                case BeastPiece.TowerShield: return GearStat.Health;
                case BeastPiece.BladeArm: return GearStat.Damage;
                case BeastPiece.HookChain: return GearStat.Hit;
                case BeastPiece.RifleArm: return GearStat.Hit;
                default: return GearStat.Health;
            }
        }

        /// The shark's pieces take one bend of each class, the bear's the other: the claw arm grabs back at whoever strikes the
        /// Warden's Guard (Retaliator), the tower shield takes more off it (Bastion), the blade arm cuts an Opening deeper
        /// (Opportunist), the hook chain holds longer (Long Snare), the rifle arm aims fast (Quickdraw) and the injector vials
        /// treat a second ally (Field Kit).
        public static ClassBend BendOf(BeastPiece piece)
        {
            switch (piece)
            {
                case BeastPiece.JawShield: return ClassBend.Bastion;
                case BeastPiece.SpineRifle: return ClassBend.Deadeye;
                case BeastPiece.FinBlades: return ClassBend.Bloodrush;
                case BeastPiece.ToothNet: return ClassBend.Barbs;
                case BeastPiece.ToothTotem: return ClassBend.Surgeon;
                case BeastPiece.ClawArm: return ClassBend.Retaliator;
                case BeastPiece.TowerShield: return ClassBend.Bastion;
                case BeastPiece.BladeArm: return ClassBend.Opportunist;
                case BeastPiece.HookChain: return ClassBend.LongSnare;
                case BeastPiece.RifleArm: return ClassBend.Quickdraw;
                default: return ClassBend.FieldKit;
            }
        }

        public const GearTier Tier = GearTier.Princess;

        /// The tribe whose pieces these are: the Clarth's from the shark, Skullpeak's from the bear.
        public static string TribeOf(BeastPiece piece)
        {
            return KitOf(piece) == BeastKit.CageBear ? CampaignTribes.SkullpeakId : CampaignTribes.ClarthId;
        }

        /// The id the piece has in a run's gear (Campaign.Gear): the same every run. The shark's pieces keep the ids they had
        /// before the bear: "gear.shark.0" to "gear.shark.4"; the bear's are "gear.bear.5" to "gear.bear.10".
        public static string GearId(BeastPiece piece)
        {
            return "gear." + BeastKits.Id(KitOf(piece)) + "." + ((int)piece).ToString(CultureInfo.InvariantCulture);
        }

        /// The piece as a run's gear, for its class only.
        public static CampaignGear Gear(BeastPiece piece)
        {
            return new CampaignGear(GearId(piece), TribeOf(piece), Tier, StatOf(piece), GearSpecial.None, Name(piece),
                (int)ClassOf(piece), BendOf(piece));
        }

        /// The piece a run's gear id names, or false.
        public static bool TryFromGearId(string gearId, out BeastPiece piece)
        {
            for (int i = 0; i < All.Length; i++)
            {
                if (GearId(All[i]) != gearId) continue;
                piece = All[i];
                return true;
            }
            piece = BeastPiece.JawShield;
            return false;
        }
    }

    /// A road scar a fighter earned in a beast fight (CampaignFighter.Scars), for the result screen.
    public sealed class BeastScar
    {
        internal BeastScar(CampaignFighter fighter, BeastPart part)
        {
            FighterId = fighter.Id;
            FighterName = fighter.Name;
            Class = fighter.Class;
            Part = part;
        }

        public string FighterId { get; }
        public string FighterName { get; }
        public UnitClass Class { get; }

        /// The part they broke.
        public BeastPart Part { get; }

        /// The scar's name ("Jawbreaker") and what it does for their class.
        public string Name => BeastKits.ScarName(Part);
        public string Text => BeastKits.ScarText(Class);
    }

    /// Beasts as rolls (Jacob, 2 Nov 2026: "integrate beasts as rolls they appear in 1 in 5 run pure randomness every time it
    /// comes back after defeat it will be even more powerful tier 1 to tier 50"; DESIGN_LOCKS "Beasts as rolls, tier 1 to 50";
    /// built on his "go", 4 Nov, as campaign rules 39). Every number here is Claude's starting value, his to change.
    public static class BeastTierRules
    {
        /// A run rolls a beast 1 time in this many.
        public const int OneIn = 5;

        /// The tiers a beast climbs, starting at 1.
        public const int Max = 50;

        /// Each tier above 1 adds this percent to its parts' health and to the power its moves hit by: tier 50 has 492 percent of
        /// tier 1's health and 345 percent of its power.
        public const int HealthPercentEach = 8;
        public const int PowerPercentEach = 5;

        /// Each tier above 1 adds this percent to the materials a win sends home, and every TrophiesEvery tiers each part broken
        /// sends one more trophy: tier 1 to 9 one each, 10 to 19 two, up to six at tier 50.
        public const int MaterialsPercentEach = 10;
        public const int TrophiesEvery = 10;

        public static int Clamp(int tier) => Math.Min(Max, Math.Max(1, tier));
        public static int HealthPercent(int tier) => 100 + HealthPercentEach * (Clamp(tier) - 1);
        public static int PowerPercent(int tier) => 100 + PowerPercentEach * (Clamp(tier) - 1);
        public static int MaterialsPercent(int tier) => 100 + MaterialsPercentEach * (Clamp(tier) - 1);
        public static int Trophies(int tier) => 1 + Clamp(tier) / TrophiesEvery;
    }

    /// The beasts beside the roads (Jacob, 20 Oct 2026, DESIGN_LOCKS "Beasts" and points 2 and 7; his six calls and "go, build
    /// the shark API with your six calls", 21 Oct 2026; its phases on 22 Oct; "go, build the Cage Bear as planned", 22 Oct). The
    /// Broken Wastes' sand shark waits beside each run's first road, and Skullpeak's cage bear beside Skullpeak's road when a
    /// run goes through it: you can hunt each any time before that road's chief, or never. A beast is not one of the road's
    /// offers and taking it costs no step of the road. Win, and every part you broke comes home as a trophy, kept at camp even
    /// if the run is lost; then that beast is gone for the run. You beat the shark by breaking all four of its parts or by
    /// holding out until it dives away (when the clock runs out, or its Last stand does); the cage bear only by breaking its
    /// heart, which drops it dead with whatever else stands. Lose, and it carries everything away, captures nobody, and waits
    /// for you to try again. Whoever breaks a part earns its road scar for the run. At camp the Butcher forges one trophy into
    /// one of its beast's pieces, once and for good, like the tabard: from then on every run starts with it in the gear, on the
    /// first fighter of its class.
    public sealed partial class Campaign
    {
        /// The ids a beast's parts carry after the fight's id: "camp.5.bloodridge.beast.shark.jaw",
        /// "camp.9.skullpeak.beast.bear.claw".
        public const string SharkId = BeastKits.SharkId;
        public const string BearId = BeastKits.BearId;

        /// For CampaignSave: the offer number a beast fight is saved under (offers are 0 up, the tutorial -1).
        internal const int BeastOfferIndex = -2;

        // The trophies at camp, in the order won; the beast pieces the Butcher forged; the beasts beaten this run; and beast
        // fights ever finished, which take no place in a tribe path's order of names.
        private readonly List<BeastPart> _trophies = new List<BeastPart>();
        private readonly List<BeastPiece> _beastPieces = new List<BeastPiece>();
        private readonly List<BeastKit> _beastsBeaten = new List<BeastKit>();
        private int _beastFights;

        /// The beast you can hunt now: the one beside this road (RoadBeast) until you face its chief, until you beat it. Null
        /// otherwise. Start it with StartBeast.
        public Encounter BeastOffer => BeastWaits ? BeastEncounter() : null;

        /// The beast beside the road you are on, beaten or not: the sand shark beside a run's first road, the cage bear beside
        /// Skullpeak's; None beside any other road, at camp and on the hunt.
        public BeastKit RoadBeast
        {
            get
            {
                // A run that rolls for its beast (rules 39) has none beside its roads: its beast, if any, stands on a node.
                if (BeastRollRun) return BeastKit.None;
                if (Stage != CampaignStage.InRun || Target == null || Target == AlphaHunt.Tribe) return BeastKit.None;
                if (Target.Id == CampaignTribes.SkullpeakId) return BeastKit.CageBear;
                return Target.Tier == LowestTier() ? BeastKit.SandShark : BeastKit.None;
            }
        }

        /// True once you beat the beast beside this road this run (killed, or the shark dived away): it is gone until the next
        /// run.
        public bool BeastBeatenThisRun => RoadBeast != BeastKit.None && _beastsBeaten.Contains(RoadBeast);

        /// The beasts beaten this run, in the order beaten.
        public IReadOnlyList<BeastKit> BeastsBeatenThisRun => _beastsBeaten;

        /// The trophies at camp, one per part broken in a won beast fight, in the order won. The forge spends them.
        public IReadOnlyList<BeastPart> Trophies => _trophies;

        /// The beast pieces the Butcher forged, for good, in the order forged.
        public IReadOnlyList<BeastPiece> BeastPiecesForged => _beastPieces;

        public int TrophyCount(BeastPart part)
        {
            int n = 0;
            for (int i = 0; i < _trophies.Count; i++) if (_trophies[i] == part) n++;
            return n;
        }

        public bool BeastPieceForged(BeastPiece piece) => _beastPieces.Contains(piece);

        // A road with rows left before its chief, and a beast beside it not beaten this run.
        private bool BeastWaits =>
            RoadBeast != BeastKit.None && !_beastsBeaten.Contains(RoadBeast) && _road != null && Step < RoadRows;

        private static int LowestTier()
        {
            int tier = int.MaxValue;
            for (int i = 0; i < CampaignTribes.All.Length; i++)
                if (!CampaignTribes.All[i].IsStarter && CampaignTribes.All[i].Tier < tier) tier = CampaignTribes.All[i].Tier;
            return tier;
        }

        private Encounter BeastEncounter()
        {
            Encounter encounter = MakeEncounter(EncounterKind.Beast, Target);
            encounter.Beast = RoadBeast;
            return encounter;
        }

        /// Whether you can start the beast fight now: a run is going, no fight is, and a beast waits (BeastOffer).
        public bool CanStartBeast(out string why)
        {
            why = string.Empty;
            BeastKit kit = RoadBeast;
            if (Stage != CampaignStage.InRun) why = "No run is going.";
            else if (_active != null) why = "A fight is already running.";
            else if (kit != BeastKit.None && _beastsBeaten.Contains(kit))
                why = "You beat the " + BeastKits.Name(kit).ToLowerInvariant() + " this run.";
            else if (!BeastWaits && BeastRollRun)
                why = "No beast waits beside the road. When a run rolls one, it stands on a fight node of the first road.";
            else if (!BeastWaits)
                why = "No beast waits here. The sand shark hunts beside a run's first road and the cage bear beside Skullpeak's, each before that road's chief.";
            return why.Length == 0;
        }

        /// Starts the beast fight. Pass squad ids, or null for the default squad. Like any fight it clears a won fight's spoils
        /// and favours on offer; it moves you nowhere on the road.
        public CampaignFight StartBeast(IReadOnlyList<string> squadIds = null)
        {
            if (!CanStartBeast(out string why)) throw new InvalidOperationException(why);
            Encounter encounter = BeastEncounter();
            List<CampaignFighter> squad = squadIds == null ? DefaultSquad(encounter.SquadSize) : PickSquad(squadIds, encounter.SquadSize);
            _spoils.Clear();
            _favourOffer = null;
            _spoilsMaterials = 0;
            RememberFightStart(BeastOfferIndex, squad);
            BattleSetup setup = BuildSetup(encounter, squad, true, out List<CampaignGear> worn, out int rules);
            _active = new CampaignFight(encounter, setup, squad, FightAssist(encounter, true), EnemyNames(encounter, setup))
            {
                Worn = worn,
                RulesVersion = rules
            };
            return _active;
        }

        // Closes a beast fight (Finish): each part broken gives its breaker the part's road scar, won or lost; a win sends home
        // a trophy for each and a fight's materials, and the beast is gone for the run; a loss takes nobody and leaves the beast
        // waiting. Read from the fight's own events, so a replay agrees.
        private EncounterResult FinishBeast(FightRecord record, EncounterResult result, bool won)
        {
            BeastKit kit = result.Encounter.Beast;
            var broken = new List<BeastPart>();
            var scars = new List<BeastScar>();
            IReadOnlyList<BattleEvent> ev = record.Events;
            for (int i = 0; i < ev.Count; i++)
            {
                if (ev[i].Kind != BattleEventKind.Sever || !BeastKits.TryParse(ev[i].Detail, out BeastPart part)) continue;
                if (!broken.Contains(part)) broken.Add(part);
                CampaignFighter breaker = ev[i].ActorSide == 0 ? FindFighter(ev[i].ActorId) : null;
                if (breaker != null && breaker.AddScar(part)) scars.Add(new BeastScar(breaker, part));
            }
            result.PartsBroken = broken;
            result.ScarsEarned = scars;
            result.RespectNeeded = RoadRows;
            result.RespectBefore = result.RespectBeforeHalving = result.RespectAfter = Step;
            // From rules 39 a rolled beast stands on a road node (PlaceRolledBeast).
            RoadNode node = result.Encounter.Node;
            int tier = result.Encounter.BeastTier;
            if (node != null) result.BeastTierAfter = BeastTier(kit);
            if (!won && node != null) return LoseRolledBeast(result);

            if (!won)
            {
                // Your fallen stay down, but it captures nobody, and it waits beside the road for you to try again.
                result.FightersLeft = _reserve.Count;
                if (_reserve.Count < MinSquad)
                {
                    result.RunOver = true;
                    EndRun();
                    return result;
                }
                MakeOffers();
                KeepTabardWorn();
                return result;
            }

            // A rolled beast sends home more trophies the higher its tier: every part broken, BeastTierRules.Trophies times.
            var sent = new List<BeastPart>(broken);
            if (node != null)
                for (int extra = 1; extra < BeastTierRules.Trophies(tier); extra++) sent.AddRange(broken);
            _trophies.AddRange(sent);
            result.Trophies = sent;
            result.BeastKilled = BeastKits.Kills(kit, broken);
            for (int i = 0; i < ev.Count; i++)
            {
                if (ev[i].Kind != BattleEventKind.Flee) continue;
                result.BeastFled = true;
                result.BeastEscaped = ev[i].Detail == BeastKits.EscapeDetail;
            }
            int materials = _tuning.MaterialsPerFight * (100 + OmenRules.MaterialsPercentEach * _omens.Count) / 100;
            if (node != null) materials = materials * BeastTierRules.MaterialsPercent(tier) / 100;
            _materials += materials;
            result.MaterialsEarned = materials;
            _beastsBeaten.Add(kit);
            Discover("Beast: " + BeastKits.Name(kit));
            if (node != null)
            {
                // Beaten, it climbs a tier at camp and comes back that much stronger the next time a run rolls it; you stand on
                // its node, as on any road fight won.
                _beastTiers[(int)kit] = Math.Min(BeastTierRules.Max, BeastTier(kit) + 1);
                result.BeastTierAfter = BeastTier(kit);
                Step = node.Row + 1;
                _roadLane = node.Lane;
                result.RespectBeforeHalving = result.RespectAfter = Step;
                result.ChiefReady = Step >= RoadRows;
            }
            result.FightersLeft = _reserve.Count;
            MakeOffers();
            KeepTabardWorn();
            return result;
        }

        /// Whether the Butcher can forge this piece from a trophy of this part now: at camp, with that trophy, from a part that
        /// makes it, and not forged yet. The trophy is all it costs.
        public bool CanForgeTrophy(BeastPart part, BeastPiece piece, out string why)
        {
            why = string.Empty;
            if (Stage != CampaignStage.ChooseTribe) why = "The forge is at camp.";
            else if (TrophyCount(part) == 0) why = "There is no " + BeastKits.Name(part) + " trophy at camp.";
            else if (!BeastPieces.IsFrom(part, piece))
                why = "A " + BeastKits.Name(part) + " cannot be forged into the " + BeastPieces.Name(piece) + ".";
            else if (_beastPieces.Contains(piece)) why = "The " + BeastPieces.Name(piece) + " is already forged.";
            return why.Length == 0;
        }

        /// Spends one trophy of this part and forges the piece, for good: every run after starts with it.
        public bool TryForgeTrophy(BeastPart part, BeastPiece piece, out string why)
        {
            if (!CanForgeTrophy(part, piece, out why)) return false;
            _trophies.RemoveAt(_trophies.IndexOf(part));
            _beastPieces.Add(piece);
            Discover("Forged: " + BeastPieces.Name(piece));
            return true;
        }

        // A run starts with every forged beast piece in its gear, each on the first fighter of its class who wears nothing.
        private void ArmBeastPieces()
        {
            for (int k = 0; k < _beastPieces.Count; k++)
            {
                CampaignGear piece = BeastPieces.Gear(_beastPieces[k]);
                if (FindGear(piece.Id) != null) continue;
                _gear.Add(piece);
                for (int i = 0; i < _reserve.Count; i++)
                {
                    if (_reserve[i].GearId.Length > 0 || !piece.Fits(_reserve[i].Class)) continue;
                    _reserve[i].GearId = piece.Id;
                    break;
                }
            }
        }

        // ------------------------------------------------------------ rules 39, beasts as rolls

        // Each beast's tier at camp, by kit; 0 is tier 1. It outlives runs.
        private readonly int[] _beastTiers = new int[3];

        // The roll's own dice: nothing else draws from them, so a saved run always keeps its beast or its lack of one.
        private const ulong BeastRollStream = 14UL;

        /// A beast's tier at camp, 1 to BeastTierRules.Max: 1 until you first beat it, then one up for every time you beat it.
        public int BeastTier(BeastKit kit)
        {
            return (int)kit <= 0 || (int)kit >= _beastTiers.Length ? 1 : Math.Max(1, _beastTiers[(int)kit]);
        }

        /// The beast this run rolled (rules 39), or None: none rolled, an older run, or at camp. It stands on one fight node of
        /// the run's first road (RoadNode.Beast).
        public BeastKit RunBeast => Stage == CampaignStage.InRun && BeastRollRun ? RollBeast(out _) : BeastKit.None;

        // The run's roll: 1 in BeastTierRules.OneIn, then which beast, each built beast as likely. The same dice then place it.
        private BeastKit RollBeast(out DeterministicRng dice)
        {
            dice = new DeterministicRng(CampaignRoad.Mix(Seed, RunsStarted, "beast.roll"), BeastRollStream);
            if (dice.Next(BeastTierRules.OneIn) != 0) return BeastKit.None;
            return dice.Next(2) == 0 ? BeastKit.SandShark : BeastKit.CageBear;
        }

        // On the run's first road (its lowest tier's tribe, like the shark beside it before), the rolled beast takes one fight
        // node: a random row, never the chief's, then a random fight in it. The node drops trophies, so it carries no Pal.
        private void PlaceRolledBeast(CampaignRoad road, CampaignTribe tribe)
        {
            if (tribe.Tier != LowestTier() || road.Rows == 0) return;
            BeastKit kit = RollBeast(out DeterministicRng dice);
            if (kit == BeastKit.None) return;
            int row = dice.Next(road.Rows);
            var fights = new List<int>();
            for (int l = 0; l < CampaignRoad.Lanes; l++)
                if (road.Node(row, l).Kind == RoadNodeKind.Fight) fights.Add(l);
            if (fights.Count == 0) return;
            RoadNode node = road.Node(row, fights[dice.Next(fights.Count)]);
            node.Beast = kit;
            node.Pal = Pal.None;
        }

        // What waits on a road node: its rolled beast, the hunters who took it, or its own fight.
        private Encounter NodeEncounter(RoadNode node)
        {
            if (node.Beast != BeastKit.None)
            {
                Encounter beast = MakeEncounter(EncounterKind.Beast, Target, node);
                beast.Beast = node.Beast;
                beast.BeastTier = BeastTier(node.Beast);
                return beast;
            }
            return IsAmbush(node) ? MakeAmbush(node) : MakeEncounter(node.Fight, Target, node);
        }

        // A rolled beast is a road fight, so losing to it costs a life like any (rules 11): each of yours who fell loses his top
        // rank (rules 24) and the streak goes with the life (rules 12; Branch Hedge keeps it, rules 29). It captures nobody, takes
        // no grudge, keeps its tier and waits on its node: try it again, or go round it.
        private EncounterResult LoseRolledBeast(EncounterResult result)
        {
            Lives = Math.Max(0, Lives - 1);
            result.LivesLeft = Lives;
            result.RanksLost = DropRanks(result.Fallen);
            if (!Hedged()) Streak = 0;
            result.FightersLeft = _reserve.Count;
            if (Lives <= 0)
            {
                result.RunOver = true;
                EndRun();
                return result;
            }
            MakeOffers();
            KeepTabardWorn();
            return result;
        }

        // ------------------------------------------------------------ for CampaignSave

        /// A beast's tier at camp (rules 39). False if it is no beast, the tier is not 2 to BeastTierRules.Max, or it is set.
        internal bool RestoreBeastTier(BeastKit kit, int tier)
        {
            if (BeastKits.Parts(kit).Count == 0 || (int)kit >= _beastTiers.Length || tier < 2 || tier > BeastTierRules.Max ||
                _beastTiers[(int)kit] != 0)
                return false;
            _beastTiers[(int)kit] = tier;
            return true;
        }

        internal int BeastFights { get => _beastFights; set => _beastFights = Math.Max(0, value); }

        internal void RestoreTrophy(BeastPart part)
        {
            _trophies.Add(part);
        }

        /// False if the piece is no piece or already forged.
        internal bool RestoreBeastPiece(BeastPiece piece)
        {
            if ((int)piece < 0 || (int)piece >= BeastPieces.Count || _beastPieces.Contains(piece)) return false;
            _beastPieces.Add(piece);
            return true;
        }

        /// A beast beaten this run. False if it is no beast or already beaten.
        internal bool RestoreBeastBeaten(BeastKit kit)
        {
            if (BeastKits.Parts(kit).Count == 0 || _beastsBeaten.Contains(kit)) return false;
            _beastsBeaten.Add(kit);
            return true;
        }
    }
}
