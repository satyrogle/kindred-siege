using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using KindredSiege.Sim.Battle;

namespace KindredSiege.Sim.Learning
{
    /// Versioned, ordered evidence. No engine objects and no generated battle rules.
    internal static class ReplayEvidence
    {
        // Bump when battle/command semantics change, even if an old fight happens to end identically.
        // 20261011-2: Unleash drains its own side's nerve while it runs, like Hold (Jacob's call J3).
        // 20261011-3: at half Hold's rate (Jacob: "halve the cost").
        // 20261013-1: the 13 Oct rules (FightRules: misses, earned points, Perfect Hold, nerve snaps) and the
        // tribes' doctrines. Format 2 adds the tribe's aim to each fighter, the doctrines to each side's
        // tactics and the fight rules to the setup; format 1 evidence still reads, with all of them off.
        // Format 3 (13 Oct 2026, Sprint 2) adds each fighter's rise bonus, nerve per kill and big hit guard from
        // their quirk and gear; older evidence reads them as 0, so the rules identity stays.
        // Format 4 (14 Oct 2026) adds each side's learned wait out of a parry to its tactics; older evidence reads it
        // as 0, which plays exactly as before, so the rules identity stays.
        // Format 5 (18 Oct 2026) adds each side's heavy swing rate to its side rules; older evidence reads it as 0,
        // which plays exactly as before (no heavy swings and none of the other 18 Oct changes), so the identity stays.
        // Format 6 (18 Oct 2026, later) adds each side's chief spell; older evidence reads it as the Eclipse, which
        // plays exactly as before, so the identity stays.
        // Format 7 (18 Oct 2026, the rival hunters) adds each side's band traits; older evidence reads them as none,
        // which plays exactly as before, so the identity stays.
        // Format 8 (18 Oct 2026, the Pals' favours) adds each side's favours (SideFavours) as a count and the fields;
        // older evidence reads them as none, which plays exactly as before, so the identity stays.
        // Format 9 (18 Oct 2026, the two new tribes) adds each fighter's bleeding blows and each side's flood; older
        // evidence reads them as none, which plays exactly as before, so the identity stays.
        // Format 10 (20 Oct 2026, the class spells) adds the class spells switch to the fight rules and each fighter's
        // class piece bend, after everything else in the setup; older evidence reads them as off and none, which plays
        // exactly as before, so the identity stays. A setup that uses neither keeps its format 9 setup identity, so an
        // unfinished fight saved before still resumes.
        // Format 11 (20 Oct 2026, later, the chief rules) adds that switch and which fighters are a chief's reserves,
        // after format 10's; older evidence reads them as off and none, and a setup that uses neither keeps its format 10
        // (or 9) setup identity.
        // Format 12 (21 Oct 2026, the chief kits) adds that switch and each side's kit and the moves it took, after format
        // 11's; older evidence reads them as off and none, and a setup that uses neither keeps its older setup identity.
        // Format 13 (21 Oct 2026, later, the beasts) adds that switch, the beast each side is and each fighter's road scars,
        // after format 12's; older evidence reads them as off, none and 0, and a setup that uses none keeps its older identity.
        // Format 14 (22 Oct 2026, the Bloodridge chief finished and the Skullpeak chief's kit) adds the chief tactics switch
        // after format 13's, and reads the Skullpeak kit and every newer move where format 12 and 13 allowed only the 21 Oct
        // ones; older evidence reads the switch as off, and a setup that uses none of them keeps its older identity.
        // Format 15 (24 Oct 2026, the parry at the center) adds that switch and each side's learned feint after format 14's;
        // older evidence reads them as off and 0, and a setup that uses neither keeps its older identity.
        // Format 16 (24 Oct 2026, later, the squad first) adds that switch and the move each side's learned wait out and feint
        // sit on after format 15's; older evidence reads them as off and none, and a setup that uses none keeps its older
        // identity.
        // Format 17 (25 Oct 2026, the raid chief) adds that switch after format 16's; older evidence reads it as off, and a setup
        // without it keeps its older identity.
        // Format 18 (25 Oct 2026, later, road combat) adds that switch and your side's bonded pairs after format 17's; older
        // evidence reads them as off and none, and a setup without them keeps its older identity.
        // Format 19 (25 Oct 2026, later still, the road kit) adds that switch and each fighter's Burn and carried moves after
        // format 18's; older evidence reads them as off and none, and a setup without them keeps its older identity.
        // The 18 Oct spell-strength change was shipped without an identity bump. Older evidence with
        // the previous identity is a replay candidate only: its checksum AND history must still match.
        internal const string RulesIdentity = "ks-battle-20261018-1";
        internal const string PreSpellStrengthIdentity = "ks-battle-20261013-1";
        // The rules just before 13 Oct. Each 13 Oct rule and doctrine is a switch that stays off unless a fight
        // turns it on, so a fight recorded under these that turned none on plays the same in this build.
        private const string PreSkillRulesIdentity = "ks-battle-20261011-3";
        // Format 33 (31 Oct 2026, the Shade's forms, campaign rules 28) adds that switch after format 32's; the forms themselves
        // were already in each side's Pal kit (format 28). Older evidence reads it as off.
        // Format 34 (1 Nov 2026, the Glutton, campaign rules 29) adds that switch and each side's talent levels after format
        // 33's; older evidence reads them as off and none.
        // Format 35 (1 Nov 2026, the Pals' duos, campaign rules 30) adds that switch and each side's duos held after format 34's;
        // older evidence reads them as off and none.
        // Format 36 (1 Nov 2026, the sizing pass, campaign rules 31) adds that switch after format 35's; older evidence reads it
        // as off.
        // Format 37 (2 Nov 2026, the Butcher's flavours, campaign rules 32) adds that switch after format 36's; older evidence
        // reads it as off.
        // Format 38 (2 Nov 2026, the vows and marks on every hit, campaign rules 33) adds those two switches and each side's vows
        // (fighter id and vow) after format 37's; older evidence reads them as off and none.
        private const int FormatVersion = 38;

        // The longest move name a learned wait out or feint can sit on, read back.
        private const int MaxMoveName = 64;
        private const int MaxEvents = 100000;
        private const int MaxUnits = 256;
        private const int MaxTicks = 1000000;

        /// Whether the rules are a supported replay candidate. This is not verification: callers must
        /// compare the complete final state and history, especially for the reused pre-spell-strength identity.
        internal static bool Replayable(ArchivedFight fight)
        {
            if (fight.RulesIdentity == RulesIdentity || fight.RulesIdentity == PreSpellStrengthIdentity) return true;
            return fight.RulesIdentity == PreSkillRulesIdentity && fight.Setup.Fight.IsNone &&
                !fight.Setup.CopyTactics(0).AnyDoctrine && !fight.Setup.CopyTactics(1).AnyDoctrine;
        }

        internal static string SetupIdentity(BattleSetup setup)
        {
            using (var stream = new MemoryStream())
            {
                using (var w = new BinaryWriter(stream, Encoding.UTF8, true)) WriteSetup(w, setup, FormatUsed(setup));
                return StateChecksum.Compute(Convert.ToBase64String(stream.ToArray()));
            }
        }

        // The oldest format that holds everything this setup uses: 32 for the beasts as raid bosses (rules 27), 31 for the tight
        // parry (rules 26), 30 for the beasts as their
        // scenes show them (rules 25), 29 for
        // the Whisperer (rules 24), 28 for the floor (rules 23), 27 for the parry meter (rules 22), 26 for danger (rules 21,
        // Jacob's playtest fixes 3, 7 and 9),
        // 25 for payoffs that spread (rules 20, Jacob's playtest fix 1),
        // 24 for the Butcher's marks and each fighter's combo rank (rules
        // 18, DESIGN_LOCKS "The Butcher's theme"), 23 for the damage layers (rules 17, DESIGN_LOCKS "Builds"), 22
        // for a champion duel (rules 16, DESIGN_LOCKS "Duels"), 21
        // for the squad screen, 20 for the clear road, 19 for the road kit, 18 for road combat or bonds, 17 for the raid chief,
        // 16 for the squad first or a wait out or feint on a move, 15
        // for the parry at the center or a learned feint, 14 for the
        // chief tactics, a kit after Bloodridge's or one of his newer moves, 13 for the beasts, a beast or a road scar, 12 for
        // the chief kits or a chief's kit, 11 for the chief rules or a chief's reserves, 10 for the class spells or a class
        // piece, else 9.
        private static int FormatUsed(BattleSetup setup)
        {
            if (setup.Fight.Vows || setup.Fight.MarksOnHits) return 38;
            if (setup.Fight.Flavours) return 37;
            if (setup.Fight.Sized) return 36;
            if (setup.Fight.Duos) return 35;
            if (setup.Fight.Glutton) return 34;
            if (setup.Fight.ShadeForms) return 33;
            if (setup.Fight.BeastRaid) return 32;
            if (setup.Fight.Tight) return 31;
            if (setup.Fight.BeastScenes) return 30;
            if (setup.Fight.Whisper) return 29;
            if (setup.Fight.Floor) return 28;
            if (setup.Fight.Meter) return 27;
            if (setup.Fight.Danger) return 26;
            if (setup.Fight.Payoffs) return 25;
            if (setup.Fight.Marks) return 24;
            if (setup.Fight.Layers) return 23;
            if (setup.Fight.ChampionDuel) return 22;
            if (setup.Fight.SquadScreen) return 21;
            if (setup.Fight.ClearRoadStage > 0) return 20;
            if (setup.Fight.RoadKit || RoadKitOn(setup)) return 19;
            if (setup.Fight.RoadCombat || setup.Bonds.Count > 0) return 18;
            if (setup.Fight.RaidChief) return 17;
            if (setup.Fight.SquadFirst || MovesOn(setup)) return 16;
            if (setup.Fight.ParryCenter || setup.CopyTactics(0).Feint > 0 || setup.CopyTactics(1).Feint > 0) return 15;
            bool newKit = false;
            for (int side = 0; side < 2; side++)
                newKit |= setup.Rules(side).Kit > ChiefKit.Bloodridge || (setup.Rules(side).KitMoves & ~ChiefKits.FirstWritten) != 0;
            if (setup.Fight.ChiefTactics || newKit) return 14;
            bool reserves = false, bends = false, scars = false;
            for (int side = 0; side < 2; side++)
            {
                IReadOnlyList<UnitSpec> units = setup.Side(side);
                for (int i = 0; i < units.Count; i++)
                {
                    reserves |= units[i].Reserve;
                    bends |= units[i].Bend != ClassBend.None;
                    scars |= units[i].BeastScars > 0;
                }
            }
            if (setup.Fight.Beasts || setup.Rules(0).Beast != BeastKit.None || setup.Rules(1).Beast != BeastKit.None || scars) return 13;
            if (setup.Fight.ChiefKits || setup.Rules(0).Kit != ChiefKit.None || setup.Rules(1).Kit != ChiefKit.None) return 12;
            if (setup.Fight.ChiefRules || reserves) return 11;
            return setup.Fight.ClassSpells || bends ? 10 : 9;
        }

        // A side's learned wait out or feint sits on a move (TacticsProfile.WaitOutMove, FeintMove).
        private static bool MovesOn(BattleSetup setup)
        {
            for (int side = 0; side < 2; side++)
            {
                TacticsProfile t = setup.CopyTactics(side);
                if (t.WaitOutMove.Length > 0 || t.FeintMove.Length > 0) return true;
            }
            return false;
        }

        internal static string Write(ArchivedFight fight)
        {
            string setupId = SetupIdentity(fight.Setup);
            if (fight.CachedEvidence != null && fight.CachedSetupIdentity == setupId) return fight.CachedEvidence;
            using (var stream = new MemoryStream())
            {
                using (var w = new BinaryWriter(stream, Encoding.UTF8, true))
                {
                    w.Write(FormatVersion); w.Write(fight.RulesIdentity);
                    WriteSetup(w, fight.Setup, FormatVersion);
                    w.Write(fight.Ticks);
                    var commands = fight.Log.Commands;
                    w.Write(commands.Count);
                    for (int i = 0; i < commands.Count; i++)
                    {
                        var c = commands[i];
                        w.Write(c.SchemaVersion); w.Write(c.Tick); w.Write(c.Sequence); w.Write(c.Kind);
                        w.Write(c.ActorId); w.Write(c.TargetId); w.Write(c.Arg0); w.Write(c.Arg1); w.Write(c.TextArg);
                    }
                    WriteRecord(w, fight.Record);
                }
                using (var compressed = new MemoryStream())
                {
                    using (var deflate = new DeflateStream(compressed, CompressionLevel.Fastest, true))
                        stream.WriteTo(deflate);
                    fight.CachedSetupIdentity = setupId;
                    fight.CachedEvidence = Convert.ToBase64String(compressed.ToArray());
                    return fight.CachedEvidence;
                }
            }
        }

        internal static bool TryRead(string text, out ArchivedFight fight, out string failure)
        {
            fight = null; failure = string.Empty;
            try
            {
                if (text.Length > 32 * 1024 * 1024) throw new FormatException("Fight evidence is too large.");
                using (var stream = Inflate(text))
                using (var r = new BinaryReader(stream, Encoding.UTF8))
                {
                    int version = r.ReadInt32();
                    if (version < 1 || version > FormatVersion) throw new FormatException("Unknown fight evidence version.");
                    string rules = r.ReadString();
                    var setup = ReadSetup(r, version);
                    long ticks = r.ReadInt64();
                    if (ticks < 0 || ticks > setup.TickLimit) throw new FormatException("Invalid fight length.");
                    var log = new SimCommandLog();
                    int count = Count(r, MaxEvents);
                    long previousTick = -1; int previousSequence = 0;
                    // An order may sit on the fight's last tick: it can end the fight on that tick.
                    for (int i = 0; i < count; i++)
                    {
                        var c = new SimCommand(r.ReadInt32(), r.ReadInt64(), r.ReadInt32(), r.ReadString(),
                            r.ReadString(), r.ReadString(), r.ReadInt32(), r.ReadInt32(), r.ReadString());
                        if (c.Tick > ticks || c.Tick < previousTick || c.Sequence <= previousSequence ||
                            !log.TryRecord(c, out _)) throw new FormatException("Invalid fight command tape.");
                        previousTick = c.Tick; previousSequence = c.Sequence;
                    }
                    int winner = Range(r.ReadInt32(), -1, 1);
                    long endTick = r.ReadInt64();
                    if (endTick < 0 || endTick > ticks) throw new FormatException("Invalid end tick.");
                    string checksum = r.ReadString();
                    if (checksum.Length != 16) throw new FormatException("Invalid fight checksum.");
                    int survivors0 = Range(r.ReadInt32(), 0, setup.Side(0).Count);
                    int survivors1 = Range(r.ReadInt32(), 0, setup.Side(1).Count);
                    var events = new List<BattleEvent>();
                    count = Count(r, MaxEvents); previousTick = -1;
                    for (int i = 0; i < count; i++)
                    {
                        long tick = r.ReadInt64();
                        int kind = r.ReadInt32(); int side = Range(r.ReadInt32(), -1, 1);
                        if (tick < 0 || tick < previousTick || tick > ticks || !Enum.IsDefined(typeof(BattleEventKind), kind))
                            throw new FormatException("Invalid battle event.");
                        events.Add(new BattleEvent(tick, (BattleEventKind)kind, side, r.ReadString(),
                            Range(r.ReadInt32(), -1, SliceClasses.Count - 1), r.ReadString(),
                            Range(r.ReadInt32(), -1, SliceClasses.Count - 1), r.ReadInt32(), r.ReadString()));
                        previousTick = tick;
                    }
                    if (stream.Position != stream.Length) throw new FormatException("Trailing fight evidence.");
                    fight = new ArchivedFight(setup, log, ticks,
                        new FightRecord(setup, winner, endTick, checksum, survivors0, survivors1, events), rules);
                    fight.CachedSetupIdentity = SetupIdentity(setup);
                    fight.CachedEvidence = text;
                    return true;
                }
            }
            catch (Exception e) when (e is IOException || e is ArgumentException || e is FormatException || e is OverflowException)
            {
                failure = "Fight evidence could not be read: " + e.Message;
                return false;
            }
        }

        internal static bool SameRecord(FightRecord a, FightRecord b)
        {
            using (var left = new MemoryStream())
            using (var right = new MemoryStream())
            {
                using (var w = new BinaryWriter(left, Encoding.UTF8, true)) WriteRecord(w, a);
                using (var w = new BinaryWriter(right, Encoding.UTF8, true)) WriteRecord(w, b);
                return Convert.ToBase64String(left.ToArray()) == Convert.ToBase64String(right.ToArray());
            }
        }

        private static void WriteRecord(BinaryWriter w, FightRecord f)
        {
            w.Write(f.Winner); w.Write(f.EndTick); w.Write(f.Checksum);
            w.Write(f.Survivors0); w.Write(f.Survivors1); w.Write(f.Events.Count);
            for (int i = 0; i < f.Events.Count; i++)
            {
                var e = f.Events[i];
                w.Write(e.Tick); w.Write((int)e.Kind); w.Write(e.ActorSide); w.Write(e.ActorId);
                w.Write(e.ActorClass); w.Write(e.TargetId); w.Write(e.TargetClass); w.Write(e.Amount); w.Write(e.Detail);
            }
        }

        // Writes the setup as this format lays it out: evidence always as the newest, the setup identity as the oldest
        // format that holds everything it uses (FormatUsed), so an older setup keeps its identity.
        private static void WriteSetup(BinaryWriter w, BattleSetup s, int format)
        {
            w.Write(s.FightId); w.Write(s.Seed); w.Write(s.TickLimit); w.Write(s.StartingDirectivePoints);
            w.Write(s.RefillTicks); w.Write(s.HealthPercent); w.Write(s.Nerve.Enabled);
            w.Write(s.Nerve.StartNerve(0)); w.Write(s.Nerve.StartNerve(1));
            w.Write(s.Nerve.FearBonusPercent(0)); w.Write(s.Nerve.FearBonusPercent(1));
            w.Write(s.Nerve.UnleashCost); w.Write(s.Nerve.HoldDrainEveryTicks);
            for (int side = 0; side < 2; side++)
            {
                var units = s.Side(side); w.Write(units.Count);
                for (int i = 0; i < units.Count; i++)
                {
                    var u = units[i]; w.Write(u.Id); w.Write((int)u.Class); w.Write((int)u.Row); w.Write((int)u.Role);
                    w.Write(u.PowerPercent); w.Write(u.HealthPercent); w.Write(u.FearPercent);
                    w.Write(u.WoundedBonusPercent); w.Write(u.ArmorPercent);
                    w.Write(u.HitBonus); w.Write(u.AvoidBonus);
                    w.Write(u.RiseBonus); w.Write(u.KillNerve); w.Write(u.BigHitArmorPercent);
                    w.Write(u.BleedPercent);
                }
                var t = s.CopyTactics(side);
                w.Write(SliceClasses.Count);
                for (int i = 0; i < SliceClasses.Count; i++) w.Write(t.TargetWeight((UnitClass)i));
                w.Write(t.AvoidGuarded); w.Write(t.TrapCaution); w.Write(t.CoverFocused);
                w.Write(t.HoldOnUnleash); w.Write(t.PressOnFallBack); w.Write(t.FocusOpener);
                w.Write(t.Rally); w.Write(t.GuardCaster); w.Write(t.HuntWounded);
                w.Write(t.HoldFormation); w.Write(t.Kite); w.Write(t.PickOffBack);
                w.Write(t.WaitOutParry);
                var rules = s.Rules(side);
                w.Write(rules.EclipseGrade); w.Write(rules.AutoEclipseTicks); w.Write(rules.Combos);
                w.Write(rules.HeavyEveryTicks); w.Write((int)rules.Spell); w.Write((int)rules.Band);
                int[] favours = rules.Favours.ToArray();
                w.Write(favours.Length);
                for (int i = 0; i < favours.Length; i++) w.Write(favours[i]);
                w.Write(rules.Flood);
            }
            var fight = s.Fight;
            w.Write(fight.Misses); w.Write(fight.EarnedPoints); w.Write(fight.PerfectHold); w.Write(fight.NerveSnaps);
            if (format < 10) return;
            // Format 10: the class spells and each fighter's class piece, side 0 first, in setup order.
            w.Write(fight.ClassSpells);
            for (int side = 0; side < 2; side++)
            {
                var units = s.Side(side);
                for (int i = 0; i < units.Count; i++) w.Write((int)units[i].Bend);
            }
            if (format < 11) return;
            // Format 11: the chief rules and which fighters are a chief's reserves.
            w.Write(fight.ChiefRules);
            for (int side = 0; side < 2; side++)
            {
                var units = s.Side(side);
                for (int i = 0; i < units.Count; i++) w.Write(units[i].Reserve);
            }
            if (format < 12) return;
            // Format 12: the chief kits, and each side's kit and the moves it took.
            w.Write(fight.ChiefKits);
            for (int side = 0; side < 2; side++)
            {
                w.Write((int)s.Rules(side).Kit);
                w.Write((int)s.Rules(side).KitMoves);
            }
            if (format < 13) return;
            // Format 13: the beasts, the beast each side is, and each fighter's road scars, side 0 first, in setup order.
            w.Write(fight.Beasts);
            for (int side = 0; side < 2; side++) w.Write((int)s.Rules(side).Beast);
            for (int side = 0; side < 2; side++)
            {
                var units = s.Side(side);
                for (int i = 0; i < units.Count; i++) w.Write(units[i].BeastScars);
            }
            if (format < 14) return;
            // Format 14: the chief tactics.
            w.Write(fight.ChiefTactics);
            if (format < 15) return;
            // Format 15: the parry at the center, and each side's learned feint.
            w.Write(fight.ParryCenter);
            for (int side = 0; side < 2; side++) w.Write(s.CopyTactics(side).Feint);
            if (format < 16) return;
            // Format 16: the squad first, and the move each side's learned wait out and feint sit on.
            w.Write(fight.SquadFirst);
            for (int side = 0; side < 2; side++)
            {
                TacticsProfile t = s.CopyTactics(side);
                w.Write(t.WaitOutMove);
                w.Write(t.FeintMove);
            }
            if (format < 17) return;
            // Format 17: the raid chief.
            w.Write(fight.RaidChief);
            if (format < 18) return;
            // Format 18: road combat, and your side's bonded pairs, in order.
            w.Write(fight.RoadCombat);
            w.Write(s.Bonds.Count);
            for (int i = 0; i < s.Bonds.Count; i++)
            {
                w.Write(s.Bonds[i].OriginId);
                w.Write(s.Bonds[i].CasherId);
            }
            if (format < 19) return;
            // Format 19: the road kit, and each fighter's Burn and carried moves, side 0 first, in setup order.
            w.Write(fight.RoadKit);
            for (int side = 0; side < 2; side++)
            {
                var units = s.Side(side);
                for (int i = 0; i < units.Count; i++)
                {
                    w.Write(units[i].BurnPercent);
                    w.Write((int)units[i].Carries);
                }
            }
            if (format >= 20) w.Write(fight.ClearRoadStage);
            if (format >= 21) w.Write(fight.SquadScreen);
            // Format 22: a champion duel (rules 16).
            if (format >= 22) w.Write(fight.ChampionDuel);
            // Format 23: the damage layers (rules 17); older evidence reads them as off.
            if (format >= 23) w.Write(fight.Layers);
            if (format < 24) return;
            // Format 24: the Butcher's marks (rules 18), and each fighter's combo rank, side 0 first, in setup order.
            w.Write(fight.Marks);
            for (int side = 0; side < 2; side++)
            {
                var units = s.Side(side);
                for (int i = 0; i < units.Count; i++) w.Write(units[i].ComboRank);
            }
            // Format 25: payoffs that spread (rules 20); older evidence reads them as off.
            if (format >= 25) w.Write(fight.Payoffs);
            // Format 26: danger (rules 21); older evidence reads it as off.
            if (format >= 26) w.Write(fight.Danger);
            // Format 27: the parry meter (rules 22); older evidence reads it as off.
            if (format >= 27) w.Write(fight.Meter);
            if (format < 28) return;
            // Format 28: the floor under the three Pals (rules 23): the switch; each fighter's second piece, its combo rank and
            // his clock percent, side 0 first, in setup order; then each side's Pal kit, its route forms and order cost percent.
            w.Write(fight.Floor);
            for (int side = 0; side < 2; side++)
            {
                var units = s.Side(side);
                for (int i = 0; i < units.Count; i++)
                {
                    w.Write((int)units[i].Bend2);
                    w.Write(units[i].ComboRank2);
                    w.Write(units[i].ClockPercent);
                }
            }
            for (int side = 0; side < 2; side++)
            {
                PalKit kit = s.Rules(side).Pals;
                int[] forms = kit.Forms();
                w.Write(forms.Length);
                for (int i = 0; i < forms.Length; i++) w.Write(forms[i]);
                w.Write(kit.OrderCostPercent);
            }
            if (format < 29) return;
            // Format 29: the Whisperer (rules 24): the switch; each fighter's rank, side 0 first, in setup order; then each
            // side's habits (0 none, 1 set, 2 upgraded, in WhispererHabit's order) and its pair.
            w.Write(fight.Whisper);
            for (int side = 0; side < 2; side++)
            {
                var units = s.Side(side);
                for (int i = 0; i < units.Count; i++) w.Write(units[i].Rank);
            }
            for (int side = 0; side < 2; side++)
            {
                PalKit kit = s.Rules(side).Pals;
                int[] habits = kit.Habits();
                w.Write(habits.Length);
                for (int i = 0; i < habits.Length; i++) w.Write(habits[i]);
                w.Write((int)kit.Pair);
            }
            if (format < 30) return;
            // Format 30: the beasts as their scenes show them (rules 25): the switch.
            w.Write(fight.BeastScenes);
            if (format < 31) return;
            // Format 31: the tight parry (rules 26); older evidence reads it as off.
            w.Write(fight.Tight);
            if (format < 32) return;
            // Format 32: the beasts as raid bosses (rules 27): the switch.
            w.Write(fight.BeastRaid);
            if (format < 33) return;
            // Format 33: the Shade's forms (rules 28): the switch.
            w.Write(fight.ShadeForms);
            if (format < 34) return;
            // Format 34: the Glutton (rules 29): the switch, then each side's talent levels in GluttonTalent's order.
            w.Write(fight.Glutton);
            for (int side = 0; side < 2; side++)
            {
                int[] talents = s.Rules(side).Pals.Talents();
                w.Write(talents.Length);
                for (int i = 0; i < talents.Length; i++) w.Write(talents[i]);
            }
            if (format < 35) return;
            // Format 35: the duos (rules 30): the switch, then each side's duos, one bit a Battle.PalDuo.
            w.Write(fight.Duos);
            for (int side = 0; side < 2; side++) w.Write(s.Rules(side).Pals.DuoMask);
            if (format < 36) return;
            // Format 36: the sizing pass (rules 31): the switch.
            w.Write(fight.Sized);
            if (format < 37) return;
            // Format 37: the Butcher's flavours (rules 32): the switch.
            w.Write(fight.Flavours);
            if (format < 38) return;
            // Format 38: the vows and marks on every hit (rules 33): both switches, then each side's vows.
            w.Write(fight.Vows);
            w.Write(fight.MarksOnHits);
            for (int side = 0; side < 2; side++)
            {
                var vows = s.Rules(side).Pals.Vows;
                w.Write(vows.Count);
                for (int i = 0; i < vows.Count; i++)
                {
                    w.Write(vows[i].Key);
                    w.Write((int)vows[i].Value);
                }
            }
        }

        private static BattleSetup ReadSetup(BinaryReader r, int version)
        {
            string id = r.ReadString(); ulong seed = r.ReadUInt64();
            if (id.Length == 0) throw new FormatException("Missing fight id.");
            var bonds = new List<BondPair>();
            int limit = Range(r.ReadInt32(), 1, MaxTicks);
            int points = Range(r.ReadInt32(), 0, MaxTicks), refill = Range(r.ReadInt32(), 0, MaxTicks);
            int health = Range(r.ReadInt32(), 1, 10000); bool enabled = r.ReadBoolean();
            int n0 = Range(r.ReadInt32(), 1, NerveRules.Full), n1 = Range(r.ReadInt32(), 1, NerveRules.Full);
            int f0 = Range(r.ReadInt32(), 0, 10000), f1 = Range(r.ReadInt32(), 0, 10000);
            int cost = Range(r.ReadInt32(), 0, MaxTicks), drain = Range(r.ReadInt32(), 1, MaxTicks);
            var nerve = (enabled ? NerveRules.On(n0, n1, f0, f1) : NerveRules.Off).WithOrderCosts(cost, drain);
            var units = new List<UnitSpec>[2]; var tactics = new TacticsProfile[2]; var rules = new SideRules[2];
            var ids = new HashSet<string>(StringComparer.Ordinal); // membership only
            for (int side = 0; side < 2; side++)
            {
                units[side] = new List<UnitSpec>(); int count = Count(r, MaxUnits);
                if (count == 0) throw new FormatException("An empty side cannot fight.");
                for (int i = 0; i < count; i++)
                {
                    string uid = r.ReadString();
                    if (uid.Length == 0 || !ids.Add(uid)) throw new FormatException("Invalid or duplicate fighter id.");
                    var unitClass = (UnitClass)Range(r.ReadInt32(), 0, SliceClasses.Count - 1);
                    var row = (Row)Range(r.ReadInt32(), 0, 1);
                    var role = (UnitRole)Range(r.ReadInt32(), 0, 2);
                    int power = Range(r.ReadInt32(), 1, 10000), toughness = Range(r.ReadInt32(), 1, 10000);
                    int fear = Range(r.ReadInt32(), 0, 10000), wounded = Range(r.ReadInt32(), 0, 10000);
                    int armor = Range(r.ReadInt32(), 0, UnitSpec.MaxArmorPercent);
                    int hit = 0, avoid = 0, rise = 0, killNerve = 0, bigHit = 0;
                    if (version >= 2)
                    {
                        hit = Range(r.ReadInt32(), -UnitSpec.MaxAimBonus, UnitSpec.MaxAimBonus);
                        avoid = Range(r.ReadInt32(), -UnitSpec.MaxAimBonus, UnitSpec.MaxAimBonus);
                    }
                    if (version >= 3)
                    {
                        rise = Range(r.ReadInt32(), 0, 100);
                        killNerve = Range(r.ReadInt32(), 0, NerveRules.Full);
                        bigHit = Range(r.ReadInt32(), 0, UnitSpec.MaxArmorPercent);
                    }
                    int bleed = version >= 9 ? Range(r.ReadInt32(), 0, BleedRules.MaxPercent) : 0;
                    units[side].Add(new UnitSpec(uid, unitClass, row, role, power, toughness, fear, wounded, armor, hit, avoid,
                        rise, killNerve, bigHit, bleed));
                }
                var t = new TacticsProfile(); tactics[side] = t;
                if (r.ReadInt32() != SliceClasses.Count) throw new FormatException("Different class catalogue.");
                for (int i = 0; i < SliceClasses.Count; i++) t.SetTargetWeight((UnitClass)i, Range(r.ReadInt32(), 0, 10));
                t.SetAvoidGuarded(Range(r.ReadInt32(), 0, 10)); t.SetTrapCaution(Range(r.ReadInt32(), 0, 10));
                t.SetCoverFocused(Range(r.ReadInt32(), 0, 10)); t.SetHoldOnUnleash(Range(r.ReadInt32(), 0, 10));
                t.SetPressOnFallBack(Range(r.ReadInt32(), 0, 10)); t.SetFocusOpener(Range(r.ReadInt32(), -1, SliceClasses.Count - 1));
                if (version >= 2)
                {
                    t.SetRally(Range(r.ReadInt32(), 0, 10)); t.SetGuardCaster(Range(r.ReadInt32(), 0, 10));
                    t.SetHuntWounded(Range(r.ReadInt32(), 0, 10)); t.SetHoldFormation(Range(r.ReadInt32(), 0, 10));
                    t.SetKite(Range(r.ReadInt32(), 0, 10)); t.SetPickOffBack(Range(r.ReadInt32(), 0, 10));
                }
                if (version >= 4) t.SetWaitOutParry(Range(r.ReadInt32(), 0, 10));
                int grade = Range(r.ReadInt32(), 0, Moves.MaxEclipseGrade), autoEclipse = Range(r.ReadInt32(), 0, MaxTicks);
                bool combos = r.ReadBoolean();
                int heavy = version >= 5 ? Range(r.ReadInt32(), 0, MaxTicks) : 0;
                var spell = version >= 6 ? (ChiefSpell)Range(r.ReadInt32(), 0, ChiefSpells.Count - 1) : ChiefSpell.Eclipse;
                var band = version >= 7 ? (BandTrait)Range(r.ReadInt32(), 0, BandRules.All) : BandTrait.None;
                SideFavours favours = SideFavours.None;
                if (version >= 8)
                {
                    int favourCount = Range(r.ReadInt32(), 0, 64);
                    var values = new int[favourCount];
                    for (int i = 0; i < favourCount; i++) values[i] = r.ReadInt32();
                    favours = SideFavours.FromArray(values);
                }
                bool flood = version >= 9 && r.ReadBoolean();
                rules[side] = new SideRules(grade, autoEclipse, combos, heavy, spell, band, favours, flood);
            }
            FightRules fight = FightRules.Off;
            if (version >= 2)
            {
                bool misses = r.ReadBoolean(), earned = r.ReadBoolean(), perfectHold = r.ReadBoolean(), snaps = r.ReadBoolean();
                bool classSpells = version >= 10 && r.ReadBoolean();
                bool chiefRules = false;
                if (version >= 10)
                {
                    for (int side = 0; side < 2; side++)
                        for (int i = 0; i < units[side].Count; i++)
                            units[side][i] = units[side][i].WithBend((ClassBend)Range(r.ReadInt32(), 0, ClassSpells.BendCount - 1));
                }
                if (version >= 11)
                {
                    chiefRules = r.ReadBoolean();
                    for (int side = 0; side < 2; side++)
                        for (int i = 0; i < units[side].Count; i++)
                            units[side][i] = units[side][i].AsReserve(r.ReadBoolean());
                }
                bool chiefKits = false;
                if (version >= 12)
                {
                    chiefKits = r.ReadBoolean();
                    for (int side = 0; side < 2; side++)
                    {
                        // Format 14 knows the Skullpeak kit and every newer move (22 Oct 2026); before it only the 21 Oct ones.
                        var kit = (ChiefKit)Range(r.ReadInt32(), 0, (int)(version >= 14 ? ChiefKits.LastKit : ChiefKit.Bloodridge));
                        var moves = (ChiefMove)Range(r.ReadInt32(), 0, (int)(version >= 14 ? ChiefKits.AllMoves : ChiefKits.FirstWritten));
                        SideRules o = rules[side];
                        rules[side] = new SideRules(o.EclipseGrade, o.AutoEclipseTicks, o.Combos, o.HeavyEveryTicks, o.Spell, o.Band,
                            o.Favours, o.Flood, kit, moves);
                    }
                }
                bool beasts = false;
                if (version >= 13)
                {
                    beasts = r.ReadBoolean();
                    for (int side = 0; side < 2; side++)
                    {
                        // The cage bear (22 Oct 2026) is written the same way as the shark.
                        var beast = (BeastKit)Range(r.ReadInt32(), 0, (int)BeastKit.CageBear);
                        SideRules o = rules[side];
                        rules[side] = new SideRules(o.EclipseGrade, o.AutoEclipseTicks, o.Combos, o.HeavyEveryTicks, o.Spell, o.Band,
                            o.Favours, o.Flood, o.Kit, o.KitMoves, beast);
                    }
                    for (int side = 0; side < 2; side++)
                        for (int i = 0; i < units[side].Count; i++)
                            units[side][i] = units[side][i].WithScars(Range(r.ReadInt32(), 0, BeastKits.PartCount));
                }
                bool chiefTactics = version >= 14 && r.ReadBoolean();
                bool parryCenter = false;
                if (version >= 15)
                {
                    parryCenter = r.ReadBoolean();
                    for (int side = 0; side < 2; side++) tactics[side].SetFeint(Range(r.ReadInt32(), 0, 10));
                }
                bool squadFirst = false;
                if (version >= 16)
                {
                    squadFirst = r.ReadBoolean();
                    for (int side = 0; side < 2; side++)
                    {
                        tactics[side].SetWaitOutMove(MoveName(r.ReadString()));
                        tactics[side].SetFeintMove(MoveName(r.ReadString()));
                    }
                }
                bool raidChief = version >= 17 && r.ReadBoolean();
                bool roadCombat = false;
                if (version >= 18)
                {
                    roadCombat = r.ReadBoolean();
                    int count = Range(r.ReadInt32(), 0, 64);
                    for (int i = 0; i < count; i++) bonds.Add(new BondPair(r.ReadString(), r.ReadString()));
                }
                bool roadKit = false;
                if (version >= 19)
                {
                    roadKit = r.ReadBoolean();
                    for (int side = 0; side < 2; side++)
                        for (int i = 0; i < units[side].Count; i++)
                        {
                            int burn = Range(r.ReadInt32(), 0, BurnRules.MaxPercent);
                            int carries = r.ReadInt32();
                            units[side][i] = units[side][i].WithRoadKit(burn, (ChiefMove)carries);
                        }
                }
                int clearRoad = version >= 20 ? Range(r.ReadInt32(), 0, 4) : 0;
                bool squadScreen = version >= 21 && r.ReadBoolean();
                bool duel = version >= 22 && r.ReadBoolean();
                bool layers = version >= 23 && r.ReadBoolean();
                bool marks = false;
                if (version >= 24)
                {
                    marks = r.ReadBoolean();
                    for (int side = 0; side < 2; side++)
                        for (int i = 0; i < units[side].Count; i++)
                            units[side][i] = units[side][i].WithComboRank(Range(r.ReadInt32(), 1, MarkRules.MaxStacks));
                }
                bool payoffs = version >= 25 && r.ReadBoolean();
                bool danger = version >= 26 && r.ReadBoolean();
                bool meter = version >= 27 && r.ReadBoolean();
                bool floor = false;
                if (version >= 28)
                {
                    floor = r.ReadBoolean();
                    for (int side = 0; side < 2; side++)
                        for (int i = 0; i < units[side].Count; i++)
                        {
                            var bend2 = (ClassBend)Range(r.ReadInt32(), 0, ClassSpells.BendCount - 1);
                            int rank2 = Range(r.ReadInt32(), 1, MarkRules.MaxStacks);
                            int clock = Range(r.ReadInt32(), UnitSpec.MinClockPercent, UnitSpec.MaxClockPercent);
                            units[side][i] = units[side][i].WithSecondPiece(bend2, rank2).WithClockPercent(clock);
                        }
                    for (int side = 0; side < 2; side++)
                    {
                        int count = Range(r.ReadInt32(), 0, ShadeRules.RouteCount);
                        var forms = new int[count];
                        for (int k = 0; k < count; k++) forms[k] = Range(r.ReadInt32(), 0, ShadeRules.MaxForm);
                        int orderCost = Range(r.ReadInt32(), PalKit.MinPercent, PalKit.MaxPercent);
                        rules[side] = rules[side].WithPals(PalKit.FromForms(forms, orderCost));
                    }
                }
                bool whisper = false;
                if (version >= 29)
                {
                    whisper = r.ReadBoolean();
                    for (int side = 0; side < 2; side++)
                        for (int i = 0; i < units[side].Count; i++)
                            units[side][i] = units[side][i].WithRank(Range(r.ReadInt32(), 1, UnitSpec.MaxRank));
                    for (int side = 0; side < 2; side++)
                    {
                        int count = Range(r.ReadInt32(), 0, WhisperRules.HabitCount + 1);
                        var habits = new int[count];
                        for (int k = 0; k < count; k++) habits[k] = Range(r.ReadInt32(), 0, 2);
                        var pair = (SquadPair)Range(r.ReadInt32(), 0, WhisperRules.PairCount);
                        rules[side] = rules[side].WithPals(rules[side].Pals.WithWhisper(habits, pair));
                    }
                }
                // Format 30: the beasts as their scenes show them (rules 25); older evidence reads it as off.
                bool beastScenes = version >= 30 && r.ReadBoolean();
                // Format 31: the tight parry (rules 26); older evidence reads it as off.
                bool tight = version >= 31 && r.ReadBoolean();
                // Format 32: the beasts as raid bosses (rules 27); older evidence reads it as off.
                bool beastRaid = version >= 32 && r.ReadBoolean();
                bool shadeForms = version >= 33 && r.ReadBoolean();
                bool glutton = false;
                if (version >= 34)
                {
                    glutton = r.ReadBoolean();
                    for (int side = 0; side < 2; side++)
                    {
                        int count = Range(r.ReadInt32(), 0, GluttonRules.TalentCount + 1);
                        var levels = new int[count];
                        for (int k = 0; k < count; k++) levels[k] = Range(r.ReadInt32(), 0, GluttonRules.Fed);
                        rules[side] = rules[side].WithPals(rules[side].Pals.WithTalents(levels));
                    }
                }
                bool duos = false;
                if (version >= 35)
                {
                    duos = r.ReadBoolean();
                    for (int side = 0; side < 2; side++)
                        rules[side] = rules[side].WithPals(rules[side].Pals.WithDuos(Range(r.ReadInt32(), 0, (1 << DuoRules.Count) - 1)));
                }
                fight = new FightRules(misses, earned, perfectHold, snaps, classSpells, chiefRules, chiefKits, beasts, chiefTactics,
                    parryCenter, squadFirst, raidChief, roadCombat, roadKit, clearRoad, squadScreen, duel, layers, marks, payoffs, danger,
                    meter, floor, whisper, beastScenes, tight, beastRaid, shadeForms, glutton, duos, version >= 36 && r.ReadBoolean(), version >= 37 && r.ReadBoolean(),
                    version >= 38 && r.ReadBoolean(), version >= 38 && r.ReadBoolean());
                if (version >= 38)
                    for (int side = 0; side < 2; side++)
                    {
                        int count = Range(r.ReadInt32(), 0, 64);
                        var vows = new List<KeyValuePair<string, StarVow>>();
                        for (int k = 0; k < count; k++)
                        {
                            string fighter = r.ReadString();
                            vows.Add(new KeyValuePair<string, StarVow>(fighter, (StarVow)Range(r.ReadInt32(), 0, VowRules.Count - 1)));
                        }
                        rules[side] = rules[side].WithPals(rules[side].Pals.WithVows(vows));
                    }
                // A beast's side is exactly its parts, or the fight cannot be played back (the audit, 22 Oct 2026); from rules 27 the
                // shark's sand pups come after them.
                for (int side = 0; side < 2 && beasts; side++)
                    if (rules[side].Beast != BeastKit.None && !BeastKits.SideFits(rules[side].Beast, units[side].Count, beastRaid))
                        throw new FormatException("A beast fight's beast side is not its parts.");
            }
            return new BattleSetup(id, seed, units[0], units[1], tactics[0], tactics[1], limit, points,
                nerve, refill, health, rules[0], rules[1], fight, bonds);
        }

        private static int Count(BinaryReader r, int max) => Range(r.ReadInt32(), 0, max);

        // Whether any fighter of the setup burns or carries a chief's move (the road kit, format 19).
        private static bool RoadKitOn(BattleSetup setup)
        {
            for (int side = 0; side < 2; side++)
            {
                var units = setup.Side(side);
                for (int i = 0; i < units.Count; i++) if (units[i].BurnPercent > 0 || units[i].Carries != ChiefMove.None) return true;
            }
            return false;
        }

        private static string MoveName(string move)
        {
            if (move.Length > MaxMoveName) throw new FormatException("Invalid move name.");
            return move;
        }

        private static MemoryStream Inflate(string text)
        {
            var result = new MemoryStream();
            try
            {
                using (var source = new MemoryStream(Convert.FromBase64String(text)))
                using (var deflate = new DeflateStream(source, CompressionMode.Decompress))
                {
                    var buffer = new byte[8192]; int count;
                    while ((count = deflate.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        if (result.Length + count > 32 * 1024 * 1024) throw new FormatException("Inflated fight evidence is too large.");
                        result.Write(buffer, 0, count);
                    }
                }
                result.Position = 0;
                return result;
            }
            catch { result.Dispose(); throw; }
        }
        private static int Range(int value, int min, int max)
        {
            if (value < min || value > max) throw new FormatException("Fight evidence contains an out-of-range value.");
            return value;
        }
    }
}
