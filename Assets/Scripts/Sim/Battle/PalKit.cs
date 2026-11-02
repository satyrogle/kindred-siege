using System;
using System.Collections.Generic;

namespace KindredSiege.Sim.Battle
{
    /// The Shade's eight routes, two on each of four buttons (DESIGN_LOCKS, "The Shade's core" and "The floor under the three
    /// Pals"): Ghost Step is now Backstab and Death Warrant is now Fear. Saves and replays keep the numbers; never reorder.
    public enum ShadeRoute
    {
        None = 0,
        Mirror = 1,
        Sanctuary = 2,
        Backstab = 3,
        RiftMine = 4,
        Rupture = 5,
        Fear = 6,
        Breach = 7,
        Echo = 8
    }

    /// The numbers behind the Shade's tricks built so far, each one's first form (campaign rules 23; BattleSimulation.Shade.cs).
    /// Every number is a starting value.
    public static class ShadeRules
    {
        /// None and the eight routes.
        public const int RouteCount = 9;

        /// She enhances at most two of the four buttons, one route each, and a route has three forms.
        public const int MaxButtons = 2;
        public const int MaxForm = 3;

        /// Sanctuary: a clean parry gives each of your standing fighters a shield of this share of his health, for
        /// ShieldTicks. The shield soaks hits before his health, then breaks.
        public const int ShieldPercent = 15;
        public const int ShieldTicks = 3 * SimClock.TicksPerSecond;

        /// Backstab: a Fall Back leaves each of your standing fighters' next hit doubling, in the Cash layer, for
        /// BackstabTicks.
        public const int BackstabCashPercent = 200;
        public const int BackstabTicks = 6 * SimClock.TicksPerSecond;

        /// Fear: killing the enemy your Focus Fire is on takes FearNerve from each enemy beside him, and of those the one
        /// left with the least nerve panics for FearPanicTicks.
        public const int FearNerve = 150;
        public const int FearPanicTicks = 2 * SimClock.TicksPerSecond;

        /// The Trick, Shield and Cash events' details.
        public const string SanctuaryDetail = "sanctuary";
        public const string BackstabDetail = "backstab";
        public const string FearDetail = "fear";

        // ------------------------------------------------------------ the forms (campaign rules 28)
        // Jacob in chat, 31 Oct 2026: "go" on Claude's Shade design (HANDOFF_LOG, "Claim: the Shade's forms"). Every number is a
        // starting value.

        /// Sanctuary 2, Relay: the field also makes one shared pool of this share of the shields it gave. When a hit breaks a
        /// fighter's shield, the pool soaks what is left of it, until the pool is gone or the field ends (ShieldTicks).
        public const int RelayPercent = 25;

        /// Sanctuary 3, Last Refuge: once a fight, a hit that would kill one of the side while its field is up leaves him at 1
        /// health, and every shield of the field shatters.
        public const int RefugeHealth = 1;

        // Sanctuary's price from rules 28: the side's parries do not stagger (BattleSimulation.ParryLands).

        /// Backstab 2, Twin Knives: each fighter's next this many hits land double, not one.
        public const int TwinKnivesHits = 2;

        /// Backstab 3, Shadow Strike: the side keeps attacking while it falls back. Backstab's price from rules 28: the next Fall
        /// Back waits until this long after the last one ran out.
        public const int BackstabLockTicks = 2 * SimClock.TicksPerSecond;

        /// Fear 2, Spreading Dread: an enemy Fear breaks sets Fear off again around him, at most this many more times for one
        /// fall.
        public const int DreadSpreads = 2;

        /// Fear's price from rules 28: Focus Fire lasts this percent as long.
        public const int FearFocusPercent = 75;

        /// The events' details for the forms: Relay's pool soaking (Shield), Last Refuge (Trick, its target the one saved),
        /// Fear spreading (Trick, its target the one who broke), and Turncoat's blow (Trick, then the Hit's detail).
        public const string RelayDetail = "relay";
        public const string RefugeDetail = "refuge";
        public const string DreadDetail = "dread";
        public const string TurncoatDetail = "turncoat";

        // ------------------------------------------------------------ the other five routes (campaign rules 28)
        // Jacob, 1 Nov 2026: "finish shade". Their forms are the ones locked on 31 Oct ("The Shade's core": "as in Codex's paper
        // and the 28 Oct names"), in the sim's timing versions (finding F4: the sim has no positions, so a push is a delay and
        // "nearby" is the one beside him). Every number is a starting value inside the paper's ranges.

        /// Mirror 1, Return: a clean parry sends this share of the parried one's health back at him (Trick "mirror", then a Hit
        /// with the same detail). 2, Catch: times the parries in a row, up to MirrorCatchMax. 3, Pierce: the return also goes
        /// through him into the first of theirs in the row behind him, at PiercePercent of it. Price: only a clean parry returns.
        public const int MirrorPercent = 5;
        public const int MirrorCatchMax = 3;
        public const int PiercePercent = 50;

        /// Rift Mine 1, Mine: a Fall Back leaves a mine for MineTicks; the first of theirs who strikes one of yours meanwhile
        /// sets it off, MinePercent of his health (Trick "mine"). 2, Shock: his next turn waits ShockTicks longer. 3, Wide
        /// Blast: the one or two beside him take it too. Price: your side's attacks wait ReturnPercent of the Fall Back.
        public const int MineTicks = 5 * SimClock.TicksPerSecond;
        public const int MinePercent = 8;
        public const int ShockTicks = SimClock.TicksPerSecond;
        public const int ReturnPercent = 150;

        /// Rupture 1, Weak point: each blow of your side that lands on your Focus Fire's target opens it one step, up to
        /// RuptureSteps; press Focus Fire on him again to rupture it, RupturePercent of his health a step (Trick "rupture").
        /// The press is free, needs a step, and ends the Focus. 2, Crack: a full rupture also cracks him for CrackTicks, his
        /// armour CrackPercent thinner. 3, Finish: each of your fighters but a Medic whose turn comes within FinishTicks takes it
        /// now, and his next turn waits that much longer. Price: changing target, or the Focus running out, loses the steps.
        public const int RuptureSteps = 3;
        public const int RupturePercent = 4;
        public const int CrackTicks = 5 * SimClock.TicksPerSecond / 2;
        public const int CrackPercent = 40;
        public const int FinishTicks = SimClock.TicksPerSecond;

        /// Breach 1, Impact: an Unleash hits up to BreachTargets of their front row (their back row if it is empty) for
        /// ImpactPercent of each one's health, and their next turns wait ShockTicks longer (Trick "breach"). 2, Gap: those it
        /// hit cannot cover anyone for GapTicks. 3, Ram: press Unleash again while it runs to end it and ram the same targets
        /// for RamPercent of each one's health, scaled by the Unleash left (Trick "ram"); at least RamMinTicks must be left.
        /// Price: your side takes ExposedPercent more damage while it runs and ExposedAfterTicks after.
        public const int BreachTargets = 3;
        public const int ImpactPercent = 4;
        public const int GapTicks = 3 * SimClock.TicksPerSecond / 2;
        public const int RamPercent = 12;
        public const int RamMinTicks = SimClock.TicksPerSecond;
        public const int ExposedPercent = 20;
        public const int ExposedAfterTicks = 3 * SimClock.TicksPerSecond / 2;

        /// Echo 1, Record: while an Unleash runs each of your fighters keeps EchoPercent of every blow he lands, and when it ends
        /// his echo hits the first one he recorded for all of it (Trick "echo", then a Hit with the same detail). 2, Release:
        /// press Unleash again to let the echoes go at once, ending it. 3, Converge: that press sends them all at your Focus
        /// Fire's target, if he stands. Price: Unleash's own bonus is halved.
        public const int EchoPercent = 35;

        /// The forms' details: on Trick events, and on the Hit their damage lands as.
        public const string MirrorDetail = "mirror";
        public const string MineDetail = "mine";
        public const string RuptureDetail = "rupture";
        public const string BreachDetail = "breach";
        public const string RamDetail = "ram";
        public const string EchoDetail = "echo";

        /// Rupture's weak point opening a step (Trick, amount the steps), and its Finish (Trick, amount the fighters it pulled).
        public const string WeakPointDetail = "weakpoint";
        public const string FinishDetail = "finish";

        /// True for the detail of damage one of her tricks dealt: it is the trick's, never a fighter's blow, so it opens no weak
        /// point and records no echo.
        public static bool IsTrickDetail(string detail)
        {
            return detail == MirrorDetail || detail == MineDetail || detail == RuptureDetail || detail == BreachDetail ||
                detail == RamDetail || detail == EchoDetail || detail == TurncoatDetail;
        }

        /// The routes the road offers from rules 28: all eight.
        public static bool Built(ShadeRoute route)
        {
            return route > ShadeRoute.None && (int)route < RouteCount;
        }

        /// A route's name, as the road card and the codex show it.
        public static string Name(ShadeRoute route)
        {
            switch (route)
            {
                case ShadeRoute.RiftMine: return "Rift Mine";
                case ShadeRoute.None: return string.Empty;
                default: return route.ToString();
            }
        }

        /// A form's name: the route's name at form 1.
        public static string FormName(ShadeRoute route, int form)
        {
            switch (route)
            {
                case ShadeRoute.Sanctuary: return form >= 3 ? "Last Refuge" : form == 2 ? "Relay" : "Sanctuary";
                case ShadeRoute.Backstab: return form >= 3 ? "Shadow Strike" : form == 2 ? "Twin Knives" : "Backstab";
                case ShadeRoute.Fear: return form >= 3 ? "Turncoat" : form == 2 ? "Spreading Dread" : "Fear";
                case ShadeRoute.Mirror: return form >= 3 ? "Pierce" : form == 2 ? "Catch" : "Mirror";
                case ShadeRoute.RiftMine: return form >= 3 ? "Wide Blast" : form == 2 ? "Shock" : "Rift Mine";
                case ShadeRoute.Rupture: return form >= 3 ? "Finish" : form == 2 ? "Crack" : "Rupture";
                case ShadeRoute.Breach: return form >= 3 ? "Ram" : form == 2 ? "Gap" : "Breach";
                case ShadeRoute.Echo: return form >= 3 ? "Converge" : form == 2 ? "Release" : "Echo";
                default: return Name(route);
            }
        }

        /// What a form adds, in one line, for the road card.
        public static string Describe(ShadeRoute route, int form)
        {
            switch (route)
            {
                case ShadeRoute.Sanctuary:
                    if (form >= 3) return "Once a fight, a killing blow on one of yours under the field leaves him at 1 health.";
                    if (form == 2) return "The field also makes a shared pool that soaks what a broken shield could not.";
                    return "A clean parry shields every fighter. Your parries no longer stagger.";
                case ShadeRoute.Backstab:
                    if (form >= 3) return "Fall Back no longer stops your attacks.";
                    if (form == 2) return "After a Fall Back, each fighter's next " + TwinKnivesHits + " hits land double.";
                    return "After a Fall Back, each fighter's next hit lands double. Fall Back waits " +
                        BackstabLockTicks / SimClock.TicksPerSecond + " s longer.";
                case ShadeRoute.Fear:
                    if (form >= 3) return "The enemy Fear panics hits the enemy beside him once.";
                    if (form == 2) return "An enemy Fear breaks sets Fear off again around him, up to " + DreadSpreads + " more times.";
                    return "When your Focus Fire target falls, those beside him lose nerve and the weakest panics. Focus Fire is a quarter shorter.";
                case ShadeRoute.Mirror:
                    if (form >= 3) return "The return also goes through him into the one behind him.";
                    if (form == 2) return "The return grows with each parry in a row, up to " + MirrorCatchMax + ".";
                    return "A clean parry sends a hit back at whoever you parried. Only a clean parry returns.";
                case ShadeRoute.RiftMine:
                    if (form >= 3) return "The blast also hits the ones beside him.";
                    if (form == 2) return "The blast also makes his next turn wait.";
                    return "Fall Back leaves a mine that blasts the first enemy who strikes one of yours. Your attacks wait longer after a Fall Back.";
                case ShadeRoute.Rupture:
                    if (form >= 3) return "A full rupture makes your fighters about to act act now, then wait that much longer.";
                    if (form == 2) return "A full rupture also cracks his armour.";
                    return "Your hits on your Focus Fire target open a weak point; press Focus Fire on him again to rupture it. Changing target loses it.";
                case ShadeRoute.Breach:
                    if (form >= 3) return "Press Unleash again to end it and ram their front line, harder the more is left.";
                    if (form == 2) return "Those it hits cannot cover anyone for a moment.";
                    return "Unleash hits their front line and makes them wait. Your side takes more damage while it runs.";
                case ShadeRoute.Echo:
                    if (form >= 3) return "Letting them go sends every echo at your Focus Fire target.";
                    if (form == 2) return "Press Unleash again to let the echoes go at once.";
                    return "While Unleash runs your fighters record their hits, and replay part of them when it ends. Unleash's own bonus is halved.";
                default: return string.Empty;
            }
        }

        /// The button a route sits on.
        public static Directive ButtonOf(ShadeRoute route)
        {
            switch (route)
            {
                case ShadeRoute.Mirror:
                case ShadeRoute.Sanctuary: return Directive.Hold;
                case ShadeRoute.Backstab:
                case ShadeRoute.RiftMine: return Directive.FallBack;
                case ShadeRoute.Rupture:
                case ShadeRoute.Fear: return Directive.FocusFire;
                case ShadeRoute.Breach:
                case ShadeRoute.Echo: return Directive.Unleash;
                default: throw new ArgumentOutOfRangeException(nameof(route));
            }
        }
    }

    /// The Glutton's 35 talents, his seven limbs of five as locked on 28 Oct 2026 (DESIGN_LOCKS, "The Butcher, Glutton and Shade
    /// locked" and its later fixes): tier 1, tier 2, the tier 3 pair, then the keystone. From rules 29 each does what
    /// GluttonRules.Describe says (Jacob in chat, 1 Nov 2026: "go" on Claude's calls for the open ones). Saves keep the numbers; never reorder.
    public enum GluttonTalent
    {
        None = 0,
        DarkMatterPlating = 1, EntropySink = 2, FoldShield = 3, NeutronMass = 4, BranchAnchor = 5,
        NucleusEdge = 6, Strip = 7, FoldStep = 8, EchoStrike = 9, AnswerTheParry = 10,
        SlowLightRounds = 11, DarkMatterRounds = 12, LightStake = 13, ProbabilityLock = 14, NothingWasted = 15,
        GravityMesh = 16, TimeLock = 17, CrawlField = 18, FoldTrap = 19, StasisWindUp = 20,
        MatterPrinter = 21, RewindWound = 22, SynthArmour = 23, EntropyPurge = 24, Recompile = 25,
        Precognition = 26, GlancingBlow = 27, DeepNode = 28, ChargedBranches = 29, CommandRelay = 30,
        RoguePlanetProspecting = 31, BranchHedge = 32, SynthesisDiscount = 33, GeneLibrary = 34, NodeCache = 35
    }

    /// The Glutton's tree in the fight and at camp (campaign rules 29; Jacob in chat, 1 Nov 2026: "go" on Claude's calls;
    /// DESIGN_LOCKS, "The Butcher, Glutton and Shade locked" and "The Glutton's theme: one limb"). Seven limbs of five, each
    /// tier 1, tier 2, a tier 3 pair you pick one of, and a keystone, in GluttonTalent's order. Every talent makes something you
    /// already have bigger, longer, faster or cheaper; fed (Feed, once a run) its bonus doubles. Every number is a starting value.
    public static class GluttonRules
    {
        public const int TalentCount = 35;
        public const int LimbCount = 7;
        public const int PerLimb = 5;

        /// A kit's level for a talent: none, held (owned at camp or lent for the run), or fed.
        public const int Held = 1;
        public const int Fed = 2;

        /// The limbs, in GluttonTalent's order.
        public static readonly string[] Limbs = { "Front line", "Strikers", "Shooters", "Control", "Healers", "Command", "War chest" };

        /// Its limb, 0 to 6.
        public static int LimbOf(GluttonTalent t) => ((int)t - 1) / PerLimb;

        /// Its place in its limb: 0 tier 1, 1 tier 2, 2 and 3 the tier 3 pair, 4 the keystone.
        public static int SlotOf(GluttonTalent t) => ((int)t - 1) % PerLimb;

        /// Its tier, 1 to 3, and 4 for a keystone.
        public static int TierOf(GluttonTalent t)
        {
            int slot = SlotOf(t);
            return slot == 4 ? 4 : slot >= 2 ? 3 : slot + 1;
        }

        public static bool IsKeystone(GluttonTalent t) => SlotOf(t) == 4;

        /// What it costs at camp: 1, 2 or 3 points by tier, a keystone 5, so a limb is 11 and the tree 77.
        public static int Cost(GluttonTalent t)
        {
            switch (TierOf(t))
            {
                case 1: return 1;
                case 2: return 2;
                case 3: return 3;
                default: return 5;
            }
        }

        /// The talent at this place of this limb.
        public static GluttonTalent At(int limb, int slot) => (GluttonTalent)(limb * PerLimb + slot + 1);

        /// The other one of a tier 3 pair; None for any other talent.
        public static GluttonTalent PartnerOf(GluttonTalent t)
        {
            int slot = SlotOf(t);
            if (slot == 2) return t + 1;
            if (slot == 3) return t - 1;
            return GluttonTalent.None;
        }

        /// Its limb's keystone.
        public static GluttonTalent KeystoneOf(GluttonTalent t) => At(LimbOf(t), 4);

        /// The campaign tier (the highest tier of a tribe you have met) that opens it in the camp tree, in the 8 Dec slice:
        /// tiers 1 and 2 from the start, tier 3 at campaign tier 2, keystones at campaign tier 3. The road lends any of them.
        public static int OpensAtTier(GluttonTalent t)
        {
            int tier = TierOf(t);
            return tier == 4 ? 3 : tier == 3 ? 2 : 1;
        }

        public static bool Valid(GluttonTalent t) => t > GluttonTalent.None && (int)t <= TalentCount;

        // ------------------------------------------------------------ what each one does (the fight's numbers)

        /// Dark Matter Plating: your front row takes this percent less from each hit.
        public const int PlatingPercent = 10;
        /// Entropy Sink: your front row's clocks run this percent faster.
        public const int SinkPercent = 15;
        /// Fold Shield (front row) and Entropy Purge (Medics): Fold Door's Unnerving takes half the nerve.
        public const int PortalNervePercent = 50;
        /// Neutron Mass: Event Horizon reaches this many places either side of its mark, not one.
        public const int NeutronReach = 2;
        /// Branch Anchor: Sanctuary's shields on your front row are this percent thicker.
        public const int AnchorPercent = 100;
        /// Nucleus Edge: Skirmishers' blows hit this percent harder.
        public const int EdgePercent = 15;
        /// Strip: the Openings your side leaves last this percent longer.
        public const int StripPercent = 100;
        /// Fold Step: Execution Corridor's blow hits this percent harder.
        public const int FoldStepPercent = 50;
        /// Echo Strike: a Skirmisher's echo keeps this many more percent of each blow, on top of Echo's own share.
        public const int EchoStrikePercent = 65;
        /// Answer the Parry: Skirmishers hit one your side parried in the last AnswerTicks this percent harder.
        public const int AnswerPercent = 100;
        public const int AnswerTicks = 2 * SimClock.TicksPerSecond;
        /// Slow Light Rounds: Stopped Bullets' freeze lasts this much longer.
        public const int SlowLightTicks = SimClock.TicksPerSecond;
        /// Dark Matter Rounds: Marksmen's blows pass through this percent of armour.
        public const int DarkRoundsPercent = 50;
        /// Light Stake: Black Hole Round crushes this percent harder.
        public const int StakePercent = 100;
        /// Probability Lock: Marksmen's blows on a snared enemy hit this percent harder.
        public const int LockPercent = 25;
        /// Nothing Wasted: Marksmen's blows on your Focus Fire target hit this percent harder.
        public const int WastedPercent = 100;
        // Gravity Mesh: a snared enemy cannot Step Back (all or nothing: Feed adds nothing).

        /// Time Lock: snares hold this percent longer.
        public const int TimeLockPercent = 50;
        /// Crawl Field: Time Snare's wind ups slow by this many more percent.
        public const int CrawlPercent = 100;
        /// Fold Trap: Doorway catches this many more chargers.
        public const int FoldTrapMore = 1;
        /// Stasis Wind Up: Singularity Net holds this many more either side of the snared one.
        public const int StasisReach = 1;
        /// Matter Printer: heals land this percent bigger.
        public const int PrinterPercent = 20;
        /// Rewind Wound: Rewind undoes this many more seconds.
        public const int RewindMoreSeconds = 2;
        /// Synth Armour: a treatment on a Guarded ally lands this many more percent bigger.
        public const int SynthPercent = 50;
        /// Recompile: heals on anyone under RecompileBelowPermille of his health land this percent bigger.
        public const int RecompilePercent = 100;
        public const int RecompileBelowPermille = 250;
        /// Precognition: the parry window is this percent wider.
        public const int PrecognitionPercent = 50;
        /// Glancing Blow (Glancing Fold): a Hold that soaks instead of parrying soaks this percent more of the hit's share it
        /// soaks.
        public const int GlancingPercent = 50;
        /// Deep Node: the Shade's tricks hit this percent harder.
        public const int DeepNodePercent = 33;
        /// Charged Branches: each held parry bar adds this many more percent to your combos and cash ins.
        public const int ChargedPercent = 5;
        /// Command Relay: the Shade's route prices are this percent smaller.
        public const int RelayPricePercent = 50;
        /// Rogue Planet Prospecting: a won fight sends home this percent more.
        public const int ProspectingPercent = 20;
        /// Branch Hedge: the streak survives this many lost fights a run.
        public const int HedgeLosses = 1;
        /// Synthesis Discount: the Butcher's pacts cost this percent less.
        public const int DiscountPercent = 33;
        /// Gene Library: a pair's free answer waits this percent less between answers.
        public const int LibraryPercent = 50;
        /// Node Cache: every Pal card shows this many more picks.
        public const int CacheMore = 1;

        /// A bonus at a kit's level: none, held, or fed and doubled.
        public static int Bonus(int level, int bonus) => level <= 0 ? 0 : level >= Fed ? bonus * 2 : bonus;

        /// Its name, as the tree and the road card show it.
        public static string Name(GluttonTalent t)
        {
            switch (t)
            {
                case GluttonTalent.None: return string.Empty;
                case GluttonTalent.GlancingBlow: return "Glancing Fold";
                default:
                    // "DarkMatterPlating" to "Dark Matter Plating".
                    string s = t.ToString();
                    var b = new System.Text.StringBuilder();
                    for (int i = 0; i < s.Length; i++)
                    {
                        if (i > 0 && char.IsUpper(s[i])) b.Append(' ');
                        b.Append(s[i]);
                    }
                    return b.ToString();
            }
        }

        /// What it does, in one line.
        public static string Describe(GluttonTalent t)
        {
            switch (t)
            {
                case GluttonTalent.DarkMatterPlating: return "Your front row takes " + PlatingPercent + "% less from each hit.";
                case GluttonTalent.EntropySink: return "Your front row acts " + SinkPercent + "% faster.";
                case GluttonTalent.FoldShield: return "Fold Door takes half the nerve from your front row.";
                case GluttonTalent.NeutronMass: return "Event Horizon pulls from " + NeutronReach + " places either side, not 1.";
                case GluttonTalent.BranchAnchor: return "Sanctuary's shields on your front row are twice as thick.";
                case GluttonTalent.NucleusEdge: return "Skirmishers hit " + EdgePercent + "% harder.";
                case GluttonTalent.Strip: return "Your Openings last twice as long.";
                case GluttonTalent.FoldStep: return "Execution Corridor hits " + FoldStepPercent + "% harder.";
                case GluttonTalent.EchoStrike: return "Echo replays Skirmishers' hits at full strength.";
                case GluttonTalent.AnswerTheParry: return "Skirmishers hit twice as hard on one you parried in the last 2 s.";
                case GluttonTalent.SlowLightRounds: return "Stopped Bullets' freeze lasts 1 s longer.";
                case GluttonTalent.DarkMatterRounds: return "Marksmen's shots pass through half of armour.";
                case GluttonTalent.LightStake: return "Black Hole Round crushes twice as hard.";
                case GluttonTalent.ProbabilityLock: return "Marksmen hit a snared enemy " + LockPercent + "% harder.";
                case GluttonTalent.NothingWasted: return "Marksmen hit your Focus Fire target twice as hard.";
                case GluttonTalent.GravityMesh: return "A snared enemy can't Step Back.";
                case GluttonTalent.TimeLock: return "Snares hold " + TimeLockPercent + "% longer.";
                case GluttonTalent.CrawlField: return "Time Snare slows wind ups three times, not twice.";
                case GluttonTalent.FoldTrap: return "Doorway catches two chargers, not one.";
                case GluttonTalent.StasisWindUp: return "Singularity Net drags in twice as many.";
                case GluttonTalent.MatterPrinter: return "Heals land " + PrinterPercent + "% bigger.";
                case GluttonTalent.RewindWound: return "Rewind undoes 5 s, not 3.";
                case GluttonTalent.SynthArmour: return "Treatment on a Guarded ally lands twice as big.";
                case GluttonTalent.EntropyPurge: return "Fold Door takes half the nerve from your Medics.";
                case GluttonTalent.Recompile: return "Heals on anyone under a quarter health land twice as big.";
                case GluttonTalent.Precognition: return "The parry window is " + PrecognitionPercent + "% wider.";
                case GluttonTalent.GlancingBlow: return "A Hold that soaks instead of parrying soaks half as much again.";
                case GluttonTalent.DeepNode: return "The Shade's tricks hit a third harder.";
                case GluttonTalent.ChargedBranches: return "Each held parry bar adds " + (MeterRules.PercentPerBar + ChargedPercent) + "%, not " + MeterRules.PercentPerBar + "%.";
                case GluttonTalent.CommandRelay: return "The Shade's route prices are halved.";
                case GluttonTalent.RoguePlanetProspecting: return "Won fights send home " + ProspectingPercent + "% more.";
                case GluttonTalent.BranchHedge: return "Your streak survives one lost fight a run.";
                case GluttonTalent.SynthesisDiscount: return "The Butcher's pacts cost a third less.";
                case GluttonTalent.GeneLibrary: return "Pairs answer twice as often.";
                case GluttonTalent.NodeCache: return "Every Pal card shows " + (FavourRulesOffered + CacheMore) + " picks, not " + FavourRulesOffered + ".";
                default: return string.Empty;
            }
        }

        // The road card's size, as Learning.FavourRules.Offered (Battle cannot see Learning).
        private const int FavourRulesOffered = 3;
    }

    /// The Whisperer's habits, two a class, with the 28 Oct audit's names (campaign rules 24; DESIGN_LOCKS, "Habits come with
    /// the game, the Whisperer upgrades them"). Saves and replays keep the numbers; never reorder.
    public enum WhispererHabit
    {
        None = 0,
        Bodyguard = 1, Rescuer = 2,
        Breaker = 3, Avenger = 4,
        CounterShot = 5, Spotter = 6,
        Ambusher = 7, Gatekeeper = 8,
        Triage = 9, FrontFirst = 10
    }

    /// The squad's pairs (28 Oct, fix 2). Saves and replays keep the numbers; never reorder.
    public enum SquadPair
    {
        None = 0,
        PinAndShoot = 1,
        OpenAndCut = 2,
        SnareAndRip = 3,
        MendAndHold = 4
    }

    /// The Whisperer in the fight (campaign rules 24; FightRules.Whisper; BattleSimulation.Whisperer.cs). Every number is a
    /// starting value.
    public static class WhisperRules
    {
        public const int HabitCount = 10;
        public const int PairCount = 4;

        /// Triage waits until someone is under this much of his health, in thousandths.
        public const int TriageBelowPermille = 250;

        /// An upgraded Triage's saved treatment lands twice as big, in the status layer, which no other heal pays into.
        public const int TriageUpPercent = 200;

        /// Rank 2: his class spell happens one more time.
        public const int RankTwo = 2;

        /// The Whisper event's details: a pair's free answer or an upgraded Avenger's free strike; a Medic keeping a
        /// treatment in hand; an Ambusher's trap springing on a heavy blow, and an upgraded one stopping it; an upgraded
        /// Counter Shot fired with no aim.
        public const string FreeDetail = "free";
        public const string InHandDetail = "inhand";
        public const string AmbushDetail = "ambush";
        public const string StoppedDetail = "stopped";
        public const string CounterDetail = "counter";

        /// The class a habit belongs to.
        public static UnitClass ClassOf(WhispererHabit habit)
        {
            switch (habit)
            {
                case WhispererHabit.Bodyguard:
                case WhispererHabit.Rescuer: return UnitClass.Warden;
                case WhispererHabit.Breaker:
                case WhispererHabit.Avenger: return UnitClass.Skirmisher;
                case WhispererHabit.CounterShot:
                case WhispererHabit.Spotter: return UnitClass.Marksman;
                case WhispererHabit.Ambusher:
                case WhispererHabit.Gatekeeper: return UnitClass.Trapper;
                case WhispererHabit.Triage:
                case WhispererHabit.FrontFirst: return UnitClass.Medic;
                default: throw new ArgumentOutOfRangeException(nameof(habit));
            }
        }

        /// A pair's two classes: the one whose spell lands first, and the one who answers it.
        public static UnitClass FirstOf(SquadPair pair)
        {
            switch (pair)
            {
                case SquadPair.PinAndShoot:
                case SquadPair.SnareAndRip: return UnitClass.Trapper;
                case SquadPair.OpenAndCut: return UnitClass.Marksman;
                case SquadPair.MendAndHold: return UnitClass.Medic;
                default: throw new ArgumentOutOfRangeException(nameof(pair));
            }
        }

        public static UnitClass AnswerOf(SquadPair pair)
        {
            switch (pair)
            {
                case SquadPair.PinAndShoot: return UnitClass.Marksman;
                case SquadPair.OpenAndCut:
                case SquadPair.SnareAndRip: return UnitClass.Skirmisher;
                case SquadPair.MendAndHold: return UnitClass.Warden;
                default: throw new ArgumentOutOfRangeException(nameof(pair));
            }
        }
    }

    /// The Whisperer's vows (campaign rules 33; Jacob in chat, 2 Nov 2026: "whisperer - 1"; BattleSimulation.Vows.cs): when a
    /// fighter reaches rank 2 he takes one of his class's two vows, and it changes how his spell works; at rank 3 it takes its
    /// final form. Saves and replays keep the numbers; never reorder.
    public enum StarVow
    {
        None = 0,
        Bastion = 1, Reprisal = 2,
        Sniper = 3, Ranger = 4,
        Reaver = 5, Duelist = 6,
        Netter = 7, Bleeder = 8,
        Hierophant = 9, Martyr = 10
    }

    /// The vows' rules and numbers. Tier 1 is rank 2, tier 2 rank 3. Every number is a starting value.
    public static class VowRules
    {
        /// One past the last vow.
        public const int Count = 11;

        /// Reprisal: every blow his Guard takes, he hits back for this percent of his power, and at tier 2 leaves an Opening.
        public const int ReprisalPercent = 50, ReprisalFinalPercent = 100;

        /// Sniper: the shot goes through to the first enemy behind, for this percent of its weight.
        public const int SniperPercent = 50, SniperFinalPercent = 100;

        /// Ranger: the shot bounces to this many more enemies, each for this percent of its weight.
        public const int RangerBounces = 2, RangerFinalBounces = 3;
        public const int RangerPercent = 50, RangerFinalPercent = 75;

        /// Duelist: his strike on your Focus Fire target never misses, and crits for this percent.
        public const int DuelistPercent = 200, DuelistFinalPercent = 300;

        /// Netter: her trap catches this many more beside the one who sprang it.
        public const int NetterMore = 1, NetterFinalMore = 2;

        /// Hierophant: her treatment splashes this percent of itself onto the allies beside the patient.
        public const int HierophantPercent = 50, HierophantFinalPercent = 100;

        /// Martyr: her heals are up to this many percent bigger, all of it at no health left, none at full.
        public const int MartyrPercent = 100, MartyrFinalPercent = 200;

        /// The Whisper event details of each vow going off.
        public const string ReprisalDetail = "reprisal", SniperDetail = "sniper", RangerDetail = "ranger", ReaverDetail = "reaver",
            DuelistDetail = "duelist", NetterDetail = "netter", BleederDetail = "bleeder", HierophantDetail = "hierophant";

        public static bool Valid(StarVow vow) => vow > StarVow.None && (int)vow < Count;

        /// The class a vow is for.
        public static UnitClass ClassOf(StarVow vow) => (UnitClass)(((int)vow - 1) / 2);

        /// A class's two vows.
        public static StarVow FirstOf(UnitClass cls) => (StarVow)((int)cls * 2 + 1);
        public static StarVow SecondOf(UnitClass cls) => (StarVow)((int)cls * 2 + 2);

        public static string Name(StarVow vow) => Valid(vow) ? vow.ToString() : "None";

        /// What it does at tier 1 (rank 2) or 2 (rank 3), in one line.
        public static string Describe(StarVow vow, int tier)
        {
            bool final = tier >= 2;
            switch (vow)
            {
                case StarVow.Bastion: return final ? "His Guard covers everyone in his ward's row." : "His Guard also covers whoever stands beside his ward.";
                case StarVow.Reprisal: return "Every blow his Guard takes, he hits back for " + (final ? ReprisalFinalPercent + "% of his power and leaves an Opening." : ReprisalPercent + "% of his power.");
                case StarVow.Sniper: return "His shot goes through to the first enemy behind for " + (final ? SniperFinalPercent : SniperPercent) + "% of it.";
                case StarVow.Ranger: return "His shot bounces to " + (final ? RangerFinalBounces : RangerBounces) + " more enemies for " + (final ? RangerFinalPercent : RangerPercent) + "% of it.";
                case StarVow.Reaver: return final ? "A kill gives him his strike back, and he strikes again at once." : "A kill gives him his strike back.";
                case StarVow.Duelist: return "His strike on your Focus Fire target never misses and crits for " + (final ? DuelistFinalPercent : DuelistPercent) + "%.";
                case StarVow.Netter: return "Her trap catches " + (final ? NetterFinalMore : NetterMore) + " more beside the one who springs it.";
                case StarVow.Bleeder: return final ? "When a bleeding enemy dies, his wound jumps to everyone beside him." : "When a bleeding enemy dies, his wound jumps to the next one beside him.";
                case StarVow.Hierophant: return "Her treatment splashes " + (final ? HierophantFinalPercent : HierophantPercent) + "% of itself onto the allies beside.";
                case StarVow.Martyr: return "Her heals are up to " + (final ? MartyrFinalPercent : MartyrPercent) + "% bigger the less health she has left.";
                default: return string.Empty;
            }
        }
    }

    /// The Pals' duos (campaign rules 30; Jacob in chat, 1 Nov 2026: "go write them"; BattleSimulation.Duos.cs): one pick that
    /// joins two Pals' themes, offered once a run holds a pick from both. One for each pair of the four.
    public enum PalDuo
    {
        None = 0,

        /// The Butcher and the Whisperer: the star lays twice his combo's stacks, and a burst he sets off gives him his spell
        /// again at once.
        CrownedMarks = 1,

        /// The Butcher and the Shade: each of her tricks lays a stack of the side's leading flavour on everyone it hits.
        MarkedTricks = 2,

        /// The Butcher and the Glutton: a burst is bigger for each talent held in the limb of the class that set it off.
        GorgedBursts = 3,

        /// The Whisperer and the Shade: each of her tricks also gives the star his spell at once, at most once every few seconds.
        ShadowStar = 4,

        /// The Whisperer and the Glutton: every talent held counts as fed on the star.
        FattenedStar = 5,

        /// The Shade and the Glutton: her tricks hit harder for each talent held, and her route prices are halved.
        GorgedTricks = 6,

        // From campaign rules 34 the duos are these six instead (Jacob in chat, 2 Nov 2026: "merge first, then build the duos on
        // top. Go"; BattleSimulation.DuoMoves.cs): each turns a move into a new one, grows with the streak of big moves answered
        // (DuoRules.MaxLevel), and has a catch.

        /// The Butcher and the Shade: a parry bursts every mark on the one parried. Catch: Fall Back costs double.
        BloodParry = 7,

        /// The Butcher and the Glutton: a burst splashes its mark onto the ones beside. Catch: plain hits are smaller.
        ChainReaction = 8,

        /// The Butcher and the Whisperer: your Focus Fire target's marks jump to others when he falls. Catch: the star takes more.
        Executioner = 9,

        /// The Shade and the Glutton: each big move answered while Unleash runs makes it last longer and hit harder. Catch: when it
        /// ends, your side is tired.
        EndlessUnleash = 10,

        /// The Whisperer and the Shade: your star's shadow keeps fighting while you Fall Back. Catch: the star has half his health.
        ShadowTwin = 11,

        /// The Whisperer and the Glutton: each kill the star makes, he grows. Catch: he slows, and his fall shakes your side.
        Gorge = 12
    }

    /// The duos' numbers. Every number is a starting value, sized to the builds test (HANDOFF_LOG, 1 Nov 2026): a duo should add
    /// about 10 points of health margin to a fight it plays, where one pick added 1 to 3.
    public static class DuoRules
    {
        /// One past the last duo.
        public const int Count = 13;

        /// The first of the rules 34 duos.
        public const PalDuo FirstMove = PalDuo.BloodParry;

        /// Whether this is one of the rules 34 duos.
        public static bool IsMove(PalDuo duo) => duo >= FirstMove && (int)duo < Count;

        /// The duos' streak: big moves answered in a row raise it to MaxLevel; one that lands in full, or a fighter falling,
        /// drops it to 1. It carries from fight to fight.
        public const int MaxLevel = 3;

        /// Endless Unleash: each answer while Unleash runs adds this many ticks a level, and this many percent a stack.
        public const int EndlessTicks = SimClock.TicksPerSecond;
        public const int EndlessPercent = 10;

        /// Endless Unleash's catch: your side is tired this long after an Unleash ends, at half speed.
        public const int TiredTicks = 2 * SimClock.TicksPerSecond;

        /// Chain Reaction's catch: plain hits this many percent smaller.
        public const int ChainCutPercent = 15;

        /// Chain Reaction: how many bursts deep one chain goes.
        public const int ChainDepth = 6;

        /// Executioner's catch: the star takes this many percent more.
        public const int ExecutionerTakenPercent = 30;

        /// Shadow Twin: his shadow's blows while you Fall Back, by level.
        public const int ShadowPercent1 = 30, ShadowPercent2 = 60, ShadowPercent3 = 100;

        /// Gorge: each kill grows him this many percent a level (health and blows), and his turns slow by half of it.
        public const int GorgePercent = 5;

        /// Gorge's catch: his fall costs his side this much of each fighter's nerve for each kill he ate.
        public const int GorgeNervePerKill = 60;

        /// The Burst and Whisper event details of the rules 34 duos.
        public const string BloodParryDetail = "bloodparry", ChainDetail = "chain", ExecutionerDetail = "executioner",
            EndlessDetail = "endless", ShadowDetail = "shadow", GorgeDetail = "gorge", LevelDetail = "duolevel";

        /// Crowned Marks: the star lays this many times his combo's stacks.
        public const int CrownStacks = 2;

        /// Gorged Bursts: a burst's hit, wide open crit and freeze, this many percent bigger for each level of a talent held in the
        /// limb of the class that set it off (fed counts twice).
        public const int GorgedBurstPercent = 25;

        /// Shadow Star: the star casts from her tricks at most once in this long.
        public const int ShadowStarTicks = 3 * SimClock.TicksPerSecond;

        /// Gorged Tricks: her tricks hit this many percent harder for each level of a talent held, and her route prices keep
        /// this percent.
        public const int GorgedTrickPercent = 15;
        public const int GorgedPricePercent = 50;

        /// The Trick detail of the star casting from a duo, amount 1 for Shadow Star and 2 for Crowned Marks.
        public const string StarDetail = "star";

        public static bool Valid(PalDuo duo) => duo > PalDuo.None && (int)duo < Count;

        /// The limb of the Glutton's tree a class plays: the Warden the front line, then strikers, shooters, control and healers.
        public static int LimbOf(UnitClass cls)
        {
            switch (cls)
            {
                case UnitClass.Warden: return 0;
                case UnitClass.Skirmisher: return 1;
                case UnitClass.Marksman: return 2;
                case UnitClass.Trapper: return 3;
                default: return 4;
            }
        }

        public static string Name(PalDuo duo)
        {
            switch (duo)
            {
                case PalDuo.CrownedMarks: return "Crowned Marks";
                case PalDuo.MarkedTricks: return "Marked Tricks";
                case PalDuo.GorgedBursts: return "Gorged Bursts";
                case PalDuo.ShadowStar: return "Shadow Star";
                case PalDuo.FattenedStar: return "Fattened Star";
                case PalDuo.GorgedTricks: return "Gorged Tricks";
                case PalDuo.BloodParry: return "Blood Parry";
                case PalDuo.ChainReaction: return "Chain Reaction";
                case PalDuo.Executioner: return "Executioner";
                case PalDuo.EndlessUnleash: return "Endless Unleash";
                case PalDuo.ShadowTwin: return "Shadow Twin";
                case PalDuo.Gorge: return "Gorge";
                default: return "None";
            }
        }

        public static string Describe(PalDuo duo)
        {
            switch (duo)
            {
                case PalDuo.CrownedMarks: return "Your star lays twice his combo's stacks, and a burst he sets off gives him his spell again at once.";
                case PalDuo.MarkedTricks: return "Each of the Shade's tricks lays a stack of your leading flavour's mark on everyone it hits.";
                case PalDuo.GorgedBursts: return "A burst is 25% bigger for each talent you hold in the limb of the class that set it off.";
                case PalDuo.ShadowStar: return "Each of the Shade's tricks also gives your star his spell at once, at most once every 3 s.";
                case PalDuo.FattenedStar: return "Every talent you hold counts as fed on your star.";
                case PalDuo.GorgedTricks: return "The Shade's tricks hit 15% harder for each talent you hold, and her route prices are halved.";
                case PalDuo.BloodParry: return "Parry + marks = boom. Catch: Fall Back costs double.";
                case PalDuo.ChainReaction: return "Burst + streak = it spreads. Catch: plain hits 15% smaller.";
                case PalDuo.Executioner: return "Focus Fire kill + marks = they jump. Catch: your star takes 30% more.";
                case PalDuo.EndlessUnleash: return "Unleash + answers = it keeps going. Catch: tired when it ends.";
                case PalDuo.ShadowTwin: return "Fall Back + star = his shadow fights on. Catch: star at half health.";
                case PalDuo.Gorge: return "Star kill = star grows. Catch: he slows, and his fall shakes you.";
                default: return string.Empty;
            }
        }
    }

    /// What a side brings into a fight from the Pals' stores (campaign rules 23; DESIGN_LOCKS, "The floor under the three
    /// Pals"): the Shade's routes on its buttons, each at its form, and one percent on its order costs, the cheaper hook (a
    /// talent that makes orders cheaper sets it, below 0). From rules 24 the Whisperer's too: each class's habit, upgraded or
    /// not, and the squad's pair (WithWhisper). None for a side that brings nothing.
    public sealed class PalKit
    {
        public static readonly PalKit None = new PalKit(null, 0);

        public const int MinPercent = -90;
        public const int MaxPercent = 300;

        private readonly int[] _forms = new int[ShadeRules.RouteCount];

        // Each habit: 0 not set, 1 set, 2 set and upgraded, at most one a class.
        private readonly int[] _habits = new int[WhisperRules.HabitCount + 1];

        // From rules 29 each of the Glutton's talents: 0 none, GluttonRules.Held, or GluttonRules.Fed.
        private readonly int[] _talents = new int[GluttonRules.TalentCount + 1];

        // From rules 30 the duos held, one bit a duo (1 << (int)PalDuo).
        private int _duos;

        // From rules 33 each fighter's vow, by id, in the order given.
        private readonly List<KeyValuePair<string, StarVow>> _vows = new List<KeyValuePair<string, StarVow>>();

        private PalKit(PalKit routes, int[] habits, SquadPair pair)
        {
            Array.Copy(routes._forms, _forms, _forms.Length);
            Array.Copy(routes._talents, _talents, _talents.Length);
            _duos = routes._duos;
            _vows.AddRange(routes._vows);
            _duoLevel = routes._duoLevel;
            OrderCostPercent = routes.OrderCostPercent;
            var taken = new bool[(int)UnitClass.Medic + 1];
            for (int h = 1; habits != null && h < habits.Length && h <= WhisperRules.HabitCount; h++)
            {
                if (habits[h] <= 0) continue;
                int cls = (int)WhisperRules.ClassOf((WhispererHabit)h);
                if (taken[cls]) continue;
                taken[cls] = true;
                _habits[h] = Math.Min(2, habits[h]);
            }
            Pair = pair < SquadPair.None || (int)pair > WhisperRules.PairCount ? SquadPair.None : pair;
        }

        /// The same kit with the Whisperer's picks (rules 24): habits as Habits gives them (0, 1 set, 2 upgraded), and the pair.
        public PalKit WithWhisper(IReadOnlyList<int> habits, SquadPair pair)
        {
            int[] copy = new int[WhisperRules.HabitCount + 1];
            for (int h = 1; habits != null && h < habits.Count && h <= WhisperRules.HabitCount; h++) copy[h] = habits[h];
            return new PalKit(this, copy, pair);
        }

        /// The same kit with the Glutton's talents (rules 29): each at 0, GluttonRules.Held or GluttonRules.Fed, in GluttonTalent's
        /// order from index 1.
        public PalKit WithTalents(IReadOnlyList<int> levels)
        {
            var kit = new PalKit(this, _habits, Pair);
            for (int t = 1; levels != null && t < levels.Count && t <= GluttonRules.TalentCount; t++)
                kit._talents[t] = Math.Max(0, Math.Min(GluttonRules.Fed, levels[t]));
            return kit;
        }

        /// This talent's level on this side: 0, GluttonRules.Held or GluttonRules.Fed.
        public int TalentLevel(GluttonTalent talent) => GluttonRules.Valid(talent) ? _talents[(int)talent] : 0;

        /// Every talent's level, in GluttonTalent's order, for the replay evidence.
        public int[] Talents()
        {
            return (int[])_talents.Clone();
        }

        /// The same kit with these duos (rules 30), one bit a duo (1 << (int)PalDuo); bits for no duo are dropped.
        public PalKit WithDuos(int mask)
        {
            var kit = new PalKit(this, _habits, Pair);
            int valid = 0;
            for (int d = 1; d < DuoRules.Count; d++) valid |= 1 << d;
            kit._duos = mask & valid;
            return kit;
        }

        /// The duos held, one bit a duo, for the replay evidence.
        public int DuoMask => _duos;

        public bool HasDuo(PalDuo duo) => DuoRules.Valid(duo) && (_duos & (1 << (int)duo)) != 0;

        /// The duos' streak this side brings into the fight (rules 34): 1 to DuoRules.MaxLevel, 0 for none.
        public int DuoLevel => _duoLevel;

        private int _duoLevel;

        /// The same kit with this streak (rules 34).
        public PalKit WithDuoLevel(int level)
        {
            var kit = new PalKit(this, _habits, Pair);
            kit._duoLevel = Math.Max(0, Math.Min(DuoRules.MaxLevel, level));
            return kit;
        }

        /// The same kit with these fighters' vows (rules 33), by fighter id; a fighter named twice keeps his first.
        public PalKit WithVows(IReadOnlyList<KeyValuePair<string, StarVow>> vows)
        {
            var kit = new PalKit(this, _habits, Pair);
            kit._vows.Clear();
            for (int i = 0; vows != null && i < vows.Count; i++)
                if (VowRules.Valid(vows[i].Value) && !string.IsNullOrEmpty(vows[i].Key) && kit.VowOf(vows[i].Key) == StarVow.None)
                    kit._vows.Add(vows[i]);
            return kit;
        }

        /// This fighter's vow, None for none.
        public StarVow VowOf(string fighterId)
        {
            for (int i = 0; i < _vows.Count; i++) if (string.Equals(_vows[i].Key, fighterId, StringComparison.Ordinal)) return _vows[i].Value;
            return StarVow.None;
        }

        /// Every fighter's vow, in order, for the replay evidence.
        public IReadOnlyList<KeyValuePair<string, StarVow>> Vows => _vows;

        /// The squad's pair, None for none.
        public SquadPair Pair { get; }

        /// This class's habit on this side, None for its plain behaviour.
        public WhispererHabit HabitOf(UnitClass cls)
        {
            for (int h = 1; h <= WhisperRules.HabitCount; h++)
                if (_habits[h] > 0 && WhisperRules.ClassOf((WhispererHabit)h) == cls) return (WhispererHabit)h;
            return WhispererHabit.None;
        }

        /// Whether this habit is set and upgraded.
        public bool Upgraded(WhispererHabit habit)
        {
            return habit > WhispererHabit.None && (int)habit <= WhisperRules.HabitCount && _habits[(int)habit] >= 2;
        }

        /// Every habit's state, in WhispererHabit's order, for the replay evidence.
        public int[] Habits()
        {
            return (int[])_habits.Clone();
        }

        /// routes: each route and its form, in order. A route on a button already taken, or past the second button, is
        /// left out; a form is kept between 1 and ShadeRules.MaxForm.
        public PalKit(IReadOnlyList<KeyValuePair<ShadeRoute, int>> routes, int orderCostPercent)
        {
            int buttons = 0;
            for (int i = 0; routes != null && i < routes.Count; i++)
            {
                ShadeRoute route = routes[i].Key;
                if (route <= ShadeRoute.None || (int)route >= ShadeRules.RouteCount || routes[i].Value <= 0) continue;
                if (buttons >= ShadeRules.MaxButtons || RouteOn(ShadeRules.ButtonOf(route)) != ShadeRoute.None) continue;
                _forms[(int)route] = Math.Min(ShadeRules.MaxForm, routes[i].Value);
                buttons++;
            }
            OrderCostPercent = Math.Max(MinPercent, Math.Min(MaxPercent, orderCostPercent));
        }

        /// The cheaper hook: a percent on every one of this side's order costs. 0 for none.
        public int OrderCostPercent { get; }

        /// A route's form on this side, 0 without it.
        public int FormOf(ShadeRoute route)
        {
            return route <= ShadeRoute.None || (int)route >= ShadeRules.RouteCount ? 0 : _forms[(int)route];
        }

        public bool Has(ShadeRoute route) => FormOf(route) > 0;

        /// The route on this button, None for a plain button.
        public ShadeRoute RouteOn(Directive button)
        {
            for (int r = 1; r < ShadeRules.RouteCount; r++)
                if (_forms[r] > 0 && ShadeRules.ButtonOf((ShadeRoute)r) == button) return (ShadeRoute)r;
            return ShadeRoute.None;
        }

        public bool IsNone
        {
            get
            {
                if (OrderCostPercent != 0 || Pair != SquadPair.None || _duos != 0 || _vows.Count > 0 || _duoLevel != 0) return false;
                for (int r = 1; r < ShadeRules.RouteCount; r++) if (_forms[r] > 0) return false;
                for (int h = 1; h <= WhisperRules.HabitCount; h++) if (_habits[h] > 0) return false;
                for (int t = 1; t <= GluttonRules.TalentCount; t++) if (_talents[t] > 0) return false;
                return true;
            }
        }

        /// Every route's form, in ShadeRoute's order, for the replay evidence.
        public int[] Forms()
        {
            return (int[])_forms.Clone();
        }

        /// The same kit back from Forms and the percent.
        public static PalKit FromForms(IReadOnlyList<int> forms, int orderCostPercent)
        {
            var routes = new List<KeyValuePair<ShadeRoute, int>>();
            for (int r = 1; forms != null && r < forms.Count && r < ShadeRules.RouteCount; r++)
                if (forms[r] > 0) routes.Add(new KeyValuePair<ShadeRoute, int>((ShadeRoute)r, forms[r]));
            return new PalKit(routes, orderCostPercent);
        }

        public void AppendTo(SnapshotBuilder b, string prefix)
        {
            for (int r = 1; r < ShadeRules.RouteCount; r++)
                if (_forms[r] > 0) b.Field(prefix + "shade." + ((ShadeRoute)r).ToString().ToLowerInvariant(), _forms[r]);
            if (OrderCostPercent != 0) b.Field(prefix + "orderCost", OrderCostPercent);
            // The Whisperer's (rules 24), written only when set, so every older fight keeps its checksum.
            for (int h = 1; h <= WhisperRules.HabitCount; h++)
                if (_habits[h] > 0) b.Field(prefix + "habit." + ((WhispererHabit)h).ToString().ToLowerInvariant(), _habits[h]);
            if (Pair != SquadPair.None) b.Field(prefix + "pair", (int)Pair);
            // The Glutton's (rules 29), the same way.
            for (int t = 1; t <= GluttonRules.TalentCount; t++)
                if (_talents[t] > 0) b.Field(prefix + "talent." + ((GluttonTalent)t).ToString().ToLowerInvariant(), _talents[t]);
            // The duos (rules 30), the same way.
            for (int d = 1; d < DuoRules.Count; d++)
                if ((_duos & (1 << d)) != 0) b.Field(prefix + "duo." + ((PalDuo)d).ToString().ToLowerInvariant(), true);
            // The vows (rules 33), the same way.
            for (int i = 0; i < _vows.Count; i++) b.Field(prefix + "vow." + _vows[i].Key, (int)_vows[i].Value);
            // The duos' streak (rules 34), the same way.
            if (_duoLevel != 0) b.Field(prefix + "duoLevel", _duoLevel);
        }
    }
}
