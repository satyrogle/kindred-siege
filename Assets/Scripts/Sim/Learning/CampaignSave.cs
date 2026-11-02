using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using KindredSiege.Sim.Battle;

namespace KindredSiege.Sim.Learning
{
    /// Writes a campaign down and reads it back. Version 31 (2 Nov 2026, the Whisperer's vows, campaign rules 33) keeps each
    /// ranked fighter's vow (vow: the fighter and the Battle.StarVow), and a Whisperer pick on offer can carry one (whisperpick's
    /// seventh field, written only when set). Version 30 (1 Nov 2026, the Pals' duos, campaign rules 30) keeps the run's duos
    /// held and the ones a card has shown (duo: two masks, one bit a Battle.PalDuo), and can hold a duo on a card
    /// (Favour.DuoCrownedMarks to Favour.DuoGorgedTricks). Version 29 (1 Nov 2026, the Glutton, campaign rules 29) keeps the talents bought
    /// at camp (owned, a camp record, in the order bought), the run's Feed (feed: the talent fed and the one eaten, 0 for none)
    /// and the losses Branch Hedge kept the streak through (hedge), and can hold his road card on offer (Favour.GluttonDarkMatterPlating
    /// to Favour.GluttonNodeCache) and a card of up to FavourRules.MaxOffered picks. Version 28 (31 Oct 2026, the Shade's forms, campaign rules 28) can hold her road
    /// card on offer: favour offer choices from Favour.ShadeMirror to Favour.ShadeEcho, one a route; an older game refuses it by
    /// its version. Version 27 (31 Oct 2026, the Whisperer, campaign rules 24) reads the habits
    /// as camp records, kept between runs, and keeps the run's habit upgrades (habitup) and her picks on offer (whisperpick,
    /// one for each of the favour offer's choices, in order). Version 26 (31 Oct 2026, the floor under the three Pals, campaign rules 23)
    /// keeps the Pals' stores: talent, route, palhabit, pair, rank and slot2 records. Version 25 (31 Oct 2026, fix 5, campaign rules 19) keeps whether the run's
    /// roads carry a Pal on every fight, and can hold a won fight's Pal picks and its spoils on offer together. Version 24
    /// (30 Oct 2026, the Butcher's theme, campaign rules 18) gives a
    /// combo piece taken again its rank, a last field on its piece record written only above rank 1. Version 23 (29 Oct
    /// 2026, the Butcher's first combo, campaign rules 15) can
    /// hold a combo: a piece of the run's gear whose bend is one (Battle.ClassBend.BlackHoleRound), and the Butcher's combo on
    /// offer after a won road fight (Favour.BlackHoleRound); an older game refuses it by its version. Version 19 (24 Oct
    /// 2026, later, the squad first) adds each chief's own mind,
    /// which only fights against him teach (Campaign.ChiefMind): a "chiefmind" record and its lines after the tribes' minds,
    /// written only for a chief who learned something, the move each parry habit sits on ("habitmove", written only when set)
    /// and a lesson's move as a twelfth field (only when set); an older save's chiefs have learned nothing and its habits and
    /// lessons sit on no move. Version 18 (24 Oct 2026, the parry at the center) adds where each hunter
    /// learned a trait (HunterLesson: the fight, your favourite button there and how many times), one "hunterlesson" record
    /// each after the hunters; an older save's hunters have none. Version 17 (22 Oct 2026, the cage bear) lets the trophies and the road scars
    /// be the bear's parts too (BeastPart 4 to 7), writes every forged beast piece as "beastpiece" (a version 16 "sharkpiece"
    /// still reads), and writes one "beast" record per beast beaten this run, by its BeastKit number (version 16's "beast 1"
    /// was the shark, which is still BeastKit 1). Version 16 (21 Oct 2026, the beasts) adds the trophies at camp, the shark
    /// pieces the Butcher forged and how many beast fights were ever finished to the camp, each fighter's road scars and
    /// whether this run's beast is beaten to the run, and a beast fight still going as offer -2; an older save has none of
    /// them. Version 15 (20 Oct 2026, the class spells) adds each piece's class and
    /// bend, up to three spoils on offer, and, for a fight still going, the rules it began under (Campaign.RulesVersion);
    /// an older save's pieces fit anyone and bend nothing, its spoils are one piece, and its unfinished fight resumes
    /// under the 13 Oct rules it began with. Version 14 keeps each run's route picker version: runs from older
    /// saves retain their original future tiers, while new runs use independently mixed tier choices.
    /// Version 13 (18 Oct 2026, later) adds the Pals' favours to the run: the
    /// favours it holds with their ranks, and any a won favour fight still offers; an older save's run has none.
    /// Version 12 (18 Oct 2026) adds the hunters (HunterCompany): the
    /// company, every named hunter and the ladder to the camp, and the bounty's heat and the hunters beaten and promoted
    /// this run to the run; an older save starts the company the seed gives a new campaign, with no bounty yet.
    /// Version 11 (14 Oct 2026) adds what each tribe learned about your
    /// parry, a sixth habit; an older save has it at 0. Version 10 (13 Oct 2026) adds the omens picked at camp and the lane
    /// you stand on along the road; an older save has no omens and its run opens on the whole next row of its road.
    /// Version 9 (13 Oct 2026) adds the tribes that captured one of yours
    /// this run, whom a Grudge hits harder; an older save has none. Version 8 (13 Oct 2026) adds each fighter's row
    /// and the piece they wear, the run's gear and a won path fight's spoils; in an older save everyone stands in
    /// their class's row with no gear. Version 7 (12 Oct 2026) keeps everyone's names with each
    /// recorded fight, so a replay can name them; fights recorded before have none. Version 6 (12 Oct 2026)
    /// adds names: each fighter's own, and how many of yours had each name; fighters in an older save are
    /// named from their ids, the same every time it opens. Version 5 (11 Oct 2026) adds the Butcher's forge to
    /// the camp and who wears the tabard to the run; versions 3 and 4 read as never forged. Version 4
    /// adds fight evidence and verified resume. Version 3 (Jacob's go, 10 Oct 2026) follows the run
    /// structure and saves the camp and the run separately. The camp part holds what outlives a run:
    /// materials, the forge, the names given, the codex, what every tribe learned and the counters. The run part holds the
    /// tribes with you, your fighters, the chief orders taken, the path, and a fight still going,
    /// written as the campaign stood when it began plus every order since, so reading it plays the
    /// fight back to the same tick and checking rules, setup and state. If only the run is invalid,
    /// a readable camp is kept with a note. File corruption uses WriteFile/TryReadFile backup recovery.
    /// Saves from the respect campaign (versions 1 and 2) open
    /// at camp the same way, with what the tribes learned and the codex kept. Write as often as you
    /// like: after every fight, after every order, and when the game closes.
    public static partial class CampaignSave
    {
        public const string Header = "kindred-siege-campaign";
        public const int Version = 31;

        public const string OldSaveNote =
            "This save is from the old respect campaign, so you are back at camp. What the tribes learned about you and your codex are kept.";

        public const string RunLostNote =
            "Your run could not be read, so you are back at camp. Everything you keep between runs is safe.";

        public const string FightLostNote =
            "Your unfinished fight could not be played back the same way, so it was dropped and you are back where it began.";

        // Records of the run part. Everything else belongs to the camp.
        private static readonly string[] RunRecords =
        {
            "joined", "fighter", "assist", "gear", "piece", "spoils", "grudge", "run", "road", "feat", "offer", "fight", "proof", "squad", "order",
            "bounty", "beaten", "promoted", "favour", "favouroffer", "routepick", "beast", "lives", "bond", "streak", "clearroad",
            "roadpals", "palroad", "talent", "route", "pair", "rank", "slot2", "habitup", "whisperpick", "feed", "hedge", "duo", "vow"
        };

        public static string Write(Campaign c)
        {
            if (c == null) throw new ArgumentNullException(nameof(c));
            CampaignFight fight = c.Active;
            var b = new StringBuilder();
            Line(b, Header, N(Version));

            // The camp. A running fight is written as the campaign stood when it began.
            Line(b, "seed", c.Seed.ToString(CultureInfo.InvariantCulture));
            ulong seedState = fight == null ? c.SeedStream.State : c.FightSeedState;
            ulong seedIncrement = fight == null ? c.SeedStream.Increment : c.FightSeedIncrement;
            Line(b, "streams", U(seedState), U(seedIncrement), U(c.DrawStream.State), U(c.DrawStream.Increment));
            Line(b, "counters", N(c.FighterCounter), N(fight == null ? c.FightCounter : c.FightCounterAtStart), N(c.RunsStarted),
                N(c.RunsWon), c.TutorialDone ? "1" : "0");
            Line(b, "camp", N(c.Materials));
            Line(b, "forge", c.TabardRecipeKnown ? "1" : "0", c.TabardForged ? "1" : "0");
            // The beasts (version 16): the trophies at camp, the beast pieces forged (version 17), and beast fights ever finished.
            for (int i = 0; i < c.Trophies.Count; i++) Line(b, "trophy", N((int)c.Trophies[i]));
            for (int i = 0; i < c.BeastPiecesForged.Count; i++) Line(b, "beastpiece", N((int)c.BeastPiecesForged[i]));
            if (c.BeastFights > 0) Line(b, "beasts", N(c.BeastFights));
            for (int i = 0; i < c.Omens.Count; i++) Line(b, "omen", N((int)c.Omens[i]));
            var named = new List<string>(c.NamesGiven.Keys);
            named.Sort(StringComparer.Ordinal);
            for (int i = 0; i < named.Count; i++) Line(b, "named", named[i], N(c.NamesGiven[named[i]]));
            for (int i = 0; i < c.Codex.Count; i++) Line(b, "codex", c.Codex[i]);
            // The hunters' company (version 12): who they are, then the ladder top first.
            HunterCompany company = c.Company;
            Line(b, "company", company.NemesisId, N(company.Recruited), N(c.AmbushFights));
            IReadOnlyList<CampaignHunter> hunters = company.Hunters;
            for (int i = 0; i < hunters.Count; i++)
            {
                CampaignHunter h = hunters[i];
                var fields = new List<string>
                {
                    "hunter", h.Id, h.Name, h.Title, N((int)h.Class), N(h.Level), N(h.Wins), N(h.Escapes), N((int)h.Weakness)
                };
                for (int t = 0; t < h.Traits.Count; t++) fields.Add(N((int)h.Traits[t]));
                Line(b, fields.ToArray());
            }
            // Version 18: where each hunter learned a trait.
            for (int i = 0; i < hunters.Count; i++)
                for (int k = 0; k < hunters[i].Lessons.Count; k++)
                {
                    HunterLesson l = hunters[i].Lessons[k];
                    Line(b, "hunterlesson", hunters[i].Id, N((int)l.Trait), N((int)l.Button), N(l.Times), l.FightId);
                }
            var ladder = new List<string> { "ladder" };
            for (int i = 0; i < company.Ladder.Count; i++) ladder.Add(company.Ladder[i]);
            Line(b, ladder.ToArray());
            WriteMind(b, c.CultMind);
            for (int i = 0; i < CampaignTribes.All.Length; i++) WriteMind(b, c.Mind(CampaignTribes.All[i].Id));
            // Version 19: each chief's own mind, for a chief who learned something.
            for (int i = 0; i < CampaignTribes.All.Length; i++)
            {
                TribeMind chief = c.ChiefMind(CampaignTribes.All[i].Id);
                if (chief.IsBlank) continue;
                Line(b, "chiefmind", chief.TribeId);
                WriteMindLines(b, chief);
            }
            for (int i = 0; i < c.Archive.Fights.Count; i++) Line(b, "archive", ReplayEvidence.Write(c.Archive.Fights[i]));
            for (int i = 0; i < c.Archive.Fights.Count; i++) WriteNames(b, c.Archive.Fights[i]);

            // The run: who is with you, where the run stands, and a fight still going.
            for (int i = 0; i < c.Joined.Count; i++) Line(b, "joined", c.Joined[i]);
            for (int i = 0; i < c.Reserve.Count; i++)
            {
                CampaignFighter f = c.Reserve[i];
                // Version 16: their road scars, one digit per part, in the order earned.
                Line(b, "fighter", f.Id, f.TribeId, N((int)f.Class), f.Name, N((int)f.Row), f.GearId, Scars(f));
            }
            for (int i = 0; i < c.Gear.Count; i++) WritePiece(b, "piece", c.Gear[i]);
            for (int i = 0; i < c.SpoilsChoices.Count; i++) WritePiece(b, "spoils", c.SpoilsChoices[i], N(c.SpoilsMaterials));
            // The run's favours and any a won favour fight still offers (version 13).
            for (int i = 0; i < c.Favours.Count; i++) Line(b, "favour", N((int)c.Favours[i].Favour), N(c.Favours[i].Rank));
            // The Pals' stores (version 26, rules 23): the Glutton's talents, the Shade's routes, the Whisperer's habits and
            // pair, each fighter's rank above 1 and his second slot.
            for (int i = 0; i < c.Talents.Count; i++) Line(b, "talent", N((int)c.Talents[i].Talent), c.Talents[i].Fed ? "1" : "0");
            // The Glutton's (version 29, rules 29): the talents bought at camp, the run's Feed and its Branch Hedge.
            for (int i = 0; i < c.OwnedTalents.Count; i++) Line(b, "owned", N((int)c.OwnedTalents[i]));
            if (c.FedTalent != GluttonTalent.None) Line(b, "feed", N((int)c.FedTalent), N((int)c.EatenTalent));
            if (c.HedgesUsed > 0) Line(b, "hedge", N(c.HedgesUsed));
            // The duos (version 30, rules 30): held, and shown.
            if (c.DuoMask != 0 || c.DuosShownMask != 0) Line(b, "duo", N(c.DuoMask), N(c.DuosShownMask));
            for (int i = 0; i < c.ShadeRoutes.Count; i++) Line(b, "route", N((int)c.ShadeRoutes[i].Route), N(c.ShadeRoutes[i].Form));
            for (int i = 0; i < c.Habits.Count; i++) Line(b, "palhabit", N((int)c.Habits[i].Habit));
            // The habits the run upgraded (version 27, rules 24).
            for (int i = 0; i < c.Habits.Count; i++) if (c.Habits[i].Upgraded) Line(b, "habitup", N((int)c.Habits[i].Habit));
            if (c.Pair != SquadPair.None) Line(b, "pair", N((int)c.Pair));
            for (int i = 0; i < c.Reserve.Count; i++)
            {
                if (c.Reserve[i].Rank > 1) Line(b, "rank", c.Reserve[i].Id, N(c.Reserve[i].Rank));
                // The vow (version 31, rules 33), after the rank.
                if (c.Reserve[i].Rank > 1 && c.Reserve[i].Vow != StarVow.None) Line(b, "vow", c.Reserve[i].Id, N((int)c.Reserve[i].Vow));
                if (c.Reserve[i].GearId2.Length > 0) Line(b, "slot2", c.Reserve[i].Id, c.Reserve[i].GearId2);
            }
            if (c.FavourOffer != null)
            {
                var offer = new List<string> { "favouroffer", N((int)c.FavourOffer.Pal), N(c.SpoilsMaterials) };
                for (int i = 0; i < c.FavourOffer.Choices.Count; i++) offer.Add(N((int)c.FavourOffer.Choices[i]));
                Line(b, offer.ToArray());
                // The Whisperer's picks, one for each choice (version 27, rules 24).
                for (int i = 0; i < c.FavourOffer.Picks.Count; i++)
                {
                    WhisperPick pick = c.FavourOffer.Picks[i];
                    if (pick.Vow == StarVow.None) Line(b, "whisperpick", N(i), N((int)pick.Kind), pick.FighterId, N((int)pick.Habit), N((int)pick.Pair));
                    else Line(b, "whisperpick", N(i), N((int)pick.Kind), pick.FighterId, N((int)pick.Habit), N((int)pick.Pair), N((int)pick.Vow));
                }
            }
            for (int i = 0; i < c.Grudges.Count; i++) Line(b, "grudge", c.Grudges[i]);
            // The run's lives and bonded pairs (version 20, rules 11).
            if (c.Lives != Campaign.RunLives) Line(b, "lives", N(c.Lives));
            for (int i = 0; i < c.Bonds.Count; i++) Line(b, "bond", c.Bonds[i].OriginId, c.Bonds[i].CasherId);
            // The run's streak (version 21, rules 12).
            if (c.Streak > 0) Line(b, "streak", N(c.Streak));
            for (int i = 0; i < c.Assists.Count; i++) Line(b, "assist", c.Assists[i].TribeId, N(c.Assists[i].Grade));
            if (c.TabardWearer.Length > 0) Line(b, "gear", c.TabardWearer);
            // The run's bounty and hunters (version 12).
            if (c.Heat > 0) Line(b, "bounty", N(c.Heat));
            for (int i = 0; i < c.HuntersBeatenThisRun.Count; i++) Line(b, "beaten", c.HuntersBeatenThisRun[i]);
            for (int i = 0; i < c.HuntersPromotedThisRun.Count; i++) Line(b, "promoted", c.HuntersPromotedThisRun[i]);
            if (c.Stage == CampaignStage.InRun)
            {
                Line(b, "run", c.Target.Id, N(c.Step), N(c.LastKind));
                Line(b, "routepick", N(c.RoutePickVersion));
                Line(b, "clearroad", c.ClearRoadRun ? "1" : "0");
                // The Pals the run held picks from when its road was made (version 24, rules 18), so the same road comes back.
                if (c.RoadPals != 0) Line(b, "roadpals", N(c.RoadPals));
                // A Pal on every fight of its roads (version 25, rules 19).
                if (c.PalRoadRun) Line(b, "palroad", "1");
                Line(b, "road", N(c.RoadLane));
                // The beasts beaten this run, by kit (version 17; version 16 wrote "1" for the shark).
                for (int i = 0; i < c.BeastsBeatenThisRun.Count; i++) Line(b, "beast", N((int)c.BeastsBeatenThisRun[i]));
                for (int i = 0; i < c.Offers.Count; i++) Line(b, "offer", N((int)c.Offers[i].Kind));
            }
            if (fight != null)
            {
                // Version 15: the rules it began under (Campaign.RulesVersion), so it resumes under the same.
                Line(b, "fight", N(c.FightOffer), L(fight.Sim.CurrentTick), N(fight.RulesVersion));
                Line(b, "proof", ResumeRules(c.Tuning), ReplayEvidence.SetupIdentity(fight.Setup), fight.Session.Checksum);
                for (int i = 0; i < c.FightSquad.Count; i++) Line(b, "squad", c.FightSquad[i]);
                IReadOnlyList<SimCommand> orders = fight.Session.Log.Commands;
                for (int i = 0; i < orders.Count; i++)
                {
                    // An order given while paused carries a fifth field (18 Oct 2026): its Hold can never parry, so the
                    // replay has to give it paused too. An older save never has it.
                    if (orders[i].Arg0 == HeavyRules.PausedOrderArg)
                        Line(b, "order", L(orders[i].Tick), orders[i].TextArg, orders[i].TargetId, PausedOrder);
                    else Line(b, "order", L(orders[i].Tick), orders[i].TextArg, orders[i].TargetId);
                }
            }
            Line(b, "end");
            return b.ToString();
        }

        public static Campaign Read(string text, CampaignTuning tuning = null)
        {
            if (!TryRead(text, tuning, out Campaign c, out string failure)) throw new FormatException(failure);
            return c;
        }

        public static bool TryRead(string text, CampaignTuning tuning, out Campaign campaign, out string failure)
        {
            campaign = null;
            if (!Unseal(text, out text, out failure)) return false;
            if (!Split(text, out int version, out List<Record> records, out failure)) return false;
            if (!ReadCamp(records, tuning, version >= 3, out Campaign c, out failure)) return false;
            if (version < 3)
            {
                // The respect campaign's tribes, fighters and runs meant something else: start at camp.
                c.ResetToCamp();
                c.LoadNote = OldSaveNote;
                campaign = c;
                return true;
            }
            if (ReadRun(c, records, tuning, version, out campaign, out failure)) return true;

            // The run part could not be read: open at camp, keeping everything that outlives a run.
            if (!ReadCamp(records, tuning, version >= 3, out Campaign camp, out failure)) return false;
            camp.ResetToCamp();
            camp.LoadNote = RunLostNote;
            campaign = camp;
            failure = string.Empty;
            return true;
        }

        private sealed class Record
        {
            public Record(string[] fields, int line)
            {
                F = fields;
                Where = "Line " + line.ToString(CultureInfo.InvariantCulture) + ": ";
            }

            public string[] F { get; }
            public string Where { get; }
        }

        private static bool Split(string text, out int version, out List<Record> records, out string failure)
        {
            version = 0;
            records = new List<Record>();
            failure = string.Empty;
            if (string.IsNullOrEmpty(text)) return Fail("The save is empty.", out failure);
            string[] lines = text.Replace("\r\n", "\n").Split('\n');
            bool headerSeen = false;
            bool ended = false;
            for (int n = 0; n < lines.Length; n++)
            {
                if (lines[n].Length == 0) continue;
                string[] f = lines[n].Split('\t');
                for (int i = 0; i < f.Length; i++) f[i] = Unescape(f[i]);
                string where = "Line " + (n + 1).ToString(CultureInfo.InvariantCulture) + ": ";
                if (ended) return Fail(where + "text after the end of the save.", out failure);
                if (!headerSeen)
                {
                    if (f.Length != 2 || f[0] != Header) return Fail(where + "this is not a Kindred Siege campaign save.", out failure);
                    if (!TryInt(f[1], out version) || version < 1 || version > Version)
                        return Fail(where + "saved as version " + f[1] + ", this game reads versions 1 to " + N(Version) + ".", out failure);
                    headerSeen = true;
                    continue;
                }
                if (f[0] == "end")
                {
                    if (f.Length != 1) return Fail(where + "bad end record.", out failure);
                    ended = true;
                    continue;
                }
                records.Add(new Record(f, n + 1));
            }
            if (!headerSeen) return Fail("This is not a Kindred Siege campaign save.", out failure);
            if (!ended) return Fail("The save stops before its end, so it was cut short.", out failure);
            return true;
        }

        // A Whisperer's pick on offer, read before the offer it belongs to is restored.
        private sealed class WhisperPickRecord
        {
            internal WhisperPickRecord(int index, Favour kind, string fighterId, WhispererHabit habit, SquadPair pair, StarVow vow)
            {
                Index = index;
                Kind = kind;
                FighterId = fighterId;
                Habit = habit;
                Pair = pair;
                Vow = vow;
            }

            internal StarVow Vow { get; }

            internal int Index { get; }
            internal Favour Kind { get; }
            internal string FighterId { get; }
            internal WhispererHabit Habit { get; }
            internal SquadPair Pair { get; }
        }

        private static bool IsRunRecord(string name)
        {
            for (int i = 0; i < RunRecords.Length; i++) if (RunRecords[i] == name) return true;
            return false;
        }

        /// The camp part: everything that outlives a run. Settled at camp, or in the tutorial.
        private static bool ReadCamp(List<Record> records, CampaignTuning tuning, bool requiresCamp, out Campaign campaign, out string failure)
        {
            campaign = null;
            failure = string.Empty;
            Campaign c = null;
            ulong seed = 0;
            TribeMind mind = null;
            bool seedSeen = false, streamsSeen = false, countersSeen = false, campSeen = false, forgeSeen = false;
            bool companySeen = false;
            string nemesis = string.Empty;
            int recruited = 0;
            var hunters = new List<CampaignHunter>();
            List<string> ladder = null;
            // The tribes whose minds the save holds: one added since it was written learns its wars now (18 Oct 2026).
            var mindsSaved = new List<string>();
            // The chiefs whose own minds it holds (version 19): every other chief learns his tribe's wars now.
            var chiefsSaved = new List<string>();
            for (int n = 0; n < records.Count; n++)
            {
                string[] f = records[n].F;
                string where = records[n].Where;
                if (IsRunRecord(f[0])) continue;
                switch (f[0])
                {
                    case "seed":
                        if (seedSeen || f.Length != 2 || !ulong.TryParse(f[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out seed))
                            return Fail(where + "bad seed record.", out failure);
                        seedSeen = true;
                        break;
                    case "streams":
                        if (!seedSeen || streamsSeen || f.Length != 5 || !TryU(f[1], out ulong s1) || !TryU(f[2], out ulong i1) || !TryU(f[3], out ulong s2) ||
                            !TryU(f[4], out ulong i2))
                            return Fail(where + "bad streams record.", out failure);
                        c = Campaign.Restore(seed, tuning, DeterministicRng.Restore(s1, i1), DeterministicRng.Restore(s2, i2));
                        streamsSeen = true;
                        break;
                    case "counters":
                        if (c == null || countersSeen || f.Length != 6 || !TryInt(f[1], out int fighters) || !TryInt(f[2], out int fights) ||
                            !TryInt(f[3], out int started) || !TryInt(f[4], out int won) || fighters < 0 || fights < 0 || started < 0 || won < 0 ||
                            (f[5] != "0" && f[5] != "1"))
                            return Fail(where + "bad counters record.", out failure);
                        countersSeen = true;
                        c.FighterCounter = fighters;
                        c.FightCounter = fights;
                        c.RunsStarted = started;
                        c.RunsWon = won;
                        c.TutorialDone = f[5] == "1";
                        break;
                    case "camp":
                        if (c == null || campSeen || f.Length != 2 || !TryInt(f[1], out int materials) || materials < 0)
                            return Fail(where + "bad camp record.", out failure);
                        campSeen = true;
                        c.SetMaterials(materials);
                        break;
                    case "forge":
                        // Recipe known, tabard forged. Saves before version 5 have none: never forged.
                        if (c == null || forgeSeen || f.Length != 3 || (f[1] != "0" && f[1] != "1") || (f[2] != "0" && f[2] != "1") ||
                            (f[1] == "0" && f[2] == "1"))
                            return Fail(where + "bad forge record.", out failure);
                        forgeSeen = true;
                        c.RestoreForge(f[1] == "1", f[2] == "1");
                        break;
                    case "trophy":
                        // A beast's part at camp for the Butcher to forge (version 16; the cage bear's from version 17). Older
                        // saves have none.
                        if (c == null || f.Length != 2 || !TryInt(f[1], out int trophy) || trophy < 0 || trophy >= BeastKits.AllPartCount)
                            return Fail(where + "bad trophy record.", out failure);
                        c.RestoreTrophy((BeastPart)trophy);
                        break;
                    case "sharkpiece":
                        // A shark piece the Butcher forged, for good (version 16, the same numbers as BeastPiece's first five).
                        if (c == null || f.Length != 2 || !TryInt(f[1], out int sharkPiece) || sharkPiece < 0 ||
                            BeastPieces.KitOf((BeastPiece)sharkPiece) != BeastKit.SandShark || !c.RestoreBeastPiece((BeastPiece)sharkPiece))
                            return Fail(where + "bad or repeated shark piece record.", out failure);
                        break;
                    case "beastpiece":
                        // A beast piece the Butcher forged, for good (version 17). Older saves have none.
                        if (c == null || f.Length != 2 || !TryInt(f[1], out int beastPiece) || !c.RestoreBeastPiece((BeastPiece)beastPiece))
                            return Fail(where + "bad or repeated beast piece record.", out failure);
                        break;
                    case "beasts":
                        // Beast fights ever finished (version 16), which take no place in a tribe path's order of names.
                        if (c == null || f.Length != 2 || !TryInt(f[1], out int beasts) || beasts < 0)
                            return Fail(where + "bad beasts record.", out failure);
                        c.BeastFights = beasts;
                        break;
                    case "omen":
                        // An omen picked at camp (version 10). Older saves have none.
                        if (c == null || f.Length != 2 || !TryInt(f[1], out int omen) || !OmenRules.IsOmen(omen) || !c.RestoreOmen((Omen)omen))
                            return Fail(where + "bad or repeated omen record.", out failure);
                        break;
                    case "named":
                        // How many of your fighters had this name (version 6). Older saves have none.
                        if (c == null || f.Length != 3 || !TryInt(f[2], out int given) || !c.RestoreNameGiven(f[1], given))
                            return Fail(where + "bad or repeated named record.", out failure);
                        break;
                    case "names":
                        // Everyone's names in a recorded fight (version 7). Fights recorded before have none.
                        if (c == null || f.Length < 4 || f.Length % 2 != 0 || !c.Archive.RestoreNames(f[1], f, 2))
                            return Fail(where + "bad or repeated names record.", out failure);
                        break;
                    case "company":
                        // The hunters' company (version 12): the nemesis's id, or empty, how many were ever recruited, and
                        // how many ambush fights were ever finished.
                        if (c == null || companySeen || f.Length != 4 || !TryInt(f[2], out recruited) || recruited < 0 ||
                            !TryInt(f[3], out int ambushes) || ambushes < 0)
                            return Fail(where + "bad company record.", out failure);
                        companySeen = true;
                        nemesis = f[1];
                        c.AmbushFights = ambushes;
                        break;
                    case "hunter":
                        if (c == null || !TryHunter(f, out CampaignHunter hunter) || FindHunter(hunters, hunter.Id) != null)
                            return Fail(where + "bad or repeated hunter record.", out failure);
                        hunters.Add(hunter);
                        break;
                    case "hunterlesson":
                    {
                        // Version 18: where a hunter learned a trait. Their hunter record comes first.
                        CampaignHunter taught = f.Length == 6 ? FindHunter(hunters, f[1]) : null;
                        if (c == null || taught == null || !TryInt(f[2], out int trait) || !taught.Has((HunterTrait)trait) ||
                            !TryInt(f[3], out int button) || button < 0 || button > (int)Directive.Unleash || !TryInt(f[4], out int times) ||
                            times < 0 || taught.LessonFor((HunterTrait)trait) != null)
                            return Fail(where + "bad hunter lesson record.", out failure);
                        taught.Record(new HunterLesson((HunterTrait)trait, f[5], (Directive)button, times));
                        break;
                    }
                    case "ladder":
                        if (c == null || ladder != null || f.Length < 2) return Fail(where + "bad ladder record.", out failure);
                        ladder = new List<string>();
                        for (int i = 1; i < f.Length; i++) ladder.Add(f[i]);
                        break;
                    case "respect":
                        // The respect campaign's bars, gone since 10 Oct 2026.
                        break;
                    case "archive":
                        if (c == null || f.Length != 2 || !ReplayEvidence.TryRead(f[1], out ArchivedFight archived, out _) ||
                            !c.Archive.Restore(archived)) return Fail(where + "bad or duplicate fight evidence.", out failure);
                        break;
                    case "codex":
                        if (c == null || f.Length != 2) return Fail(where + "bad codex record.", out failure);
                        c.Discover(f[1]);
                        break;
                    case "owned":
                        // A talent bought at camp (version 29), a camp record.
                        if (c == null || f.Length != 2 || !TryInt(f[1], out int ownedId) || !c.RestoreOwned((GluttonTalent)ownedId))
                            return Fail(where + "bad or repeated owned talent record.", out failure);
                        break;
                    case "palhabit":
                        // A class's habit (version 26). From version 27 a camp record: habits come with the game, kept between runs.
                        if (c == null || f.Length != 2 || !TryInt(f[1], out int habitId) || !c.RestoreHabit((WhispererHabit)habitId))
                            return Fail(where + "bad or repeated habit record.", out failure);
                        break;
                    case "mind":
                        if (c == null || f.Length != 2) return Fail(where + "bad mind record.", out failure);
                        mind = c.Mind(f[1]);
                        if (mind == null) return Fail(where + "no tribe called " + f[1] + ".", out failure);
                        mindsSaved.Add(f[1]);
                        break;
                    case "chiefmind":
                        if (c == null || f.Length != 2) return Fail(where + "bad chiefmind record.", out failure);
                        mind = c.ChiefMind(f[1]);
                        if (mind == null) return Fail(where + "no tribe called " + f[1] + ".", out failure);
                        chiefsSaved.Add(f[1]);
                        break;
                    case "habit":
                        if (mind == null || f.Length != 3 || !TryInt(f[1], out int habit) || habit < 0 ||
                            habit >= TribeMind.HabitCount || !TryInt(f[2], out int value))
                            return Fail(where + "bad habit record.", out failure);
                        mind.SetHabit((Habit)habit, value);
                        break;
                    case "habitmove":
                        if (mind == null || f.Length != 3 || !TryInt(f[1], out int moved) ||
                            (moved != (int)Habit.WaitOutParry && moved != (int)Habit.Feint) || f[2].Length == 0)
                            return Fail(where + "bad habitmove record.", out failure);
                        mind.SetHabitMove((Habit)moved, f[2]);
                        break;
                    case "threat":
                        if (mind == null || f.Length != 3 || !TryInt(f[1], out int unitClass) || unitClass < 0 ||
                            unitClass >= SliceClasses.Count || !TryInt(f[2], out int threat))
                            return Fail(where + "bad threat record.", out failure);
                        mind.SetThreatMemory((UnitClass)unitClass, threat);
                        break;
                    case "learned":
                        if (mind == null || f.Length != 2) return Fail(where + "bad learned record.", out failure);
                        mind.MarkLearned(f[1]);
                        break;
                    case "lesson":
                        if (mind == null || (f.Length != 11 && f.Length != 12) || !TryInt(f[2], out int side) || !TryInt(f[4], out int subject) ||
                            subject < 0 || subject >= SliceClasses.Count || !TryInt(f[5], out int before) ||
                            !TryInt(f[6], out int after) || !TryInt(f[7], out int evidence) ||
                            !TryLong(f[8], out long from) || !TryLong(f[9], out long to))
                            return Fail(where + "bad lesson record.", out failure);
                        mind.Record(new Lesson(mind.TribeId, f[1], side, f[3], (UnitClass)subject, before, after, evidence, from, to, f[10],
                            f.Length == 12 ? f[11] : string.Empty));
                        break;
                    default:
                        return Fail(where + "unknown record " + f[0] + ".", out failure);
                }
            }
            if (c == null || !countersSeen || (requiresCamp && !campSeen))
                return Fail("The campaign is missing required records.", out failure);
            // A save before version 12 has no company: it keeps the one the seed gives a new campaign.
            if (companySeen || hunters.Count > 0 || ladder != null)
            {
                if (!companySeen || ladder == null) return Fail("The hunters' company is only partly saved.", out failure);
                if (!HunterCompany.Restore(hunters, ladder, nemesis, recruited, out HunterCompany company, out string why))
                    return Fail(why, out failure);
                c.RestoreCompany(company);
            }
            c.TeachNewTribes(mindsSaved);
            c.TeachNewChiefs(chiefsSaved);
            c.Settle();
            campaign = c;
            return true;
        }

        // A hunter record: id, name, title, class, level, wins, escapes, weakness, then one to three traits that fit
        // together and leave the weakness standing.
        private static bool TryHunter(string[] f, out CampaignHunter hunter)
        {
            hunter = null;
            if (f.Length < 10 || f.Length > 9 + HunterTraits.MaxPerHunter || f[1].Length == 0 || f[1] == HunterCompany.You ||
                !TryInt(f[4], out int cls) || cls < 0 || cls >= SliceClasses.Count || !TryInt(f[5], out int level) || level < 0 ||
                level > HunterRules.MaxLevel || !TryInt(f[6], out int wins) || wins < 0 || !TryInt(f[7], out int escapes) || escapes < 0 ||
                !TryInt(f[8], out int weakness) || weakness < 0 || weakness >= HunterWeaknesses.Count)
                return false;
            var h = new CampaignHunter(f[1], f[2], f[3], (UnitClass)cls, level, (HunterWeakness)weakness) { Wins = wins, Escapes = escapes };
            for (int i = 9; i < f.Length; i++)
            {
                if (!TryInt(f[i], out int trait) || !h.CanLearn((HunterTrait)trait)) return false;
                h.Learn((HunterTrait)trait);
            }
            if (h.Weakness != (HunterWeakness)weakness) return false;
            hunter = h;
            return true;
        }

        private static CampaignHunter FindHunter(List<CampaignHunter> hunters, string id)
        {
            for (int i = 0; i < hunters.Count; i++) if (hunters[i].Id == id) return hunters[i];
            return null;
        }

        /// The run part, on top of a campaign read from the camp part. A fight that will not play back
        /// the same way is dropped and the campaign reopens where it began.
        private static bool ReadRun(Campaign c, List<Record> records, CampaignTuning tuning, int version, out Campaign campaign, out string failure)
        {
            campaign = null;
            failure = string.Empty;
            string runTarget = null;
            int runStep = 0;
            int runLastKind = -1;
            int roadLane = -1;
            bool roadSeen = false;
            bool routePickSeen = false;
            bool clearRoadSeen = false, roadPalsSeen = false, palRoadSeen = false;
            c.RoutePickVersion = 0; // Saves through v13 keep their current run's original future tiers.
            var offers = new List<EncounterKind>();
            string wearer = null;
            var worn = new List<KeyValuePair<string, string>>();
            var seconds = new List<KeyValuePair<string, string>>();
            var spoils = new List<CampaignGear>();
            int spoilsMaterials = -1;
            int fightRules = 0;
            var favourChoices = new List<Favour>();
            var whisperPicks = new List<WhisperPickRecord>();
            int favourPal = 0;
            int favourMaterials = 0;
            bool favourOfferSeen = false;
            bool fightSeen = false;
            int fightOffer = -1;
            long fightTick = 0;
            var squad = new List<string>();
            var orders = new List<SavedOrder>();
            string[] proof = null;
            int heat = 0;
            bool bountySeen = false;
            var beaten = new List<string>();
            var promoted = new List<string>();
            for (int n = 0; n < records.Count; n++)
            {
                string[] f = records[n].F;
                string where = records[n].Where;
                switch (f[0])
                {
                    case "bounty":
                        // The run's heat (version 12). Older saves have none: bounty 1.
                        if (bountySeen || f.Length != 2 || !TryInt(f[1], out heat) || heat < 0) return Fail(where + "bad bounty record.", out failure);
                        bountySeen = true;
                        break;
                    case "beaten":
                    case "promoted":
                        // A hunter beaten, or promoted, this run (version 12). A beaten one may since have died.
                        if (f.Length != 2 || f[1].Length == 0) return Fail(where + "bad " + f[0] + " record.", out failure);
                        (f[0] == "beaten" ? beaten : promoted).Add(f[1]);
                        break;
                    case "joined":
                        if (f.Length != 2 || CampaignTribes.Find(f[1]) == null) return Fail(where + "bad joined record.", out failure);
                        c.AddJoined(f[1]);
                        break;
                    case "fighter":
                        // Version 6 adds the fighter's name: one saved before has none and is named from their id. Version 8
                        // adds their row and the piece they wear: one saved before stands in their class's row. Version 16 adds
                        // their road scars, one digit per part: one saved before has none.
                        if ((f.Length != 4 && f.Length != 5 && f.Length != 7 && f.Length != 8) || CampaignTribes.Find(f[2]) == null ||
                            !TryInt(f[3], out int cls) || cls < 0 || cls >= SliceClasses.Count)
                            return Fail(where + "bad fighter record.", out failure);
                        CampaignFighter fighter = c.MakeFighter(f[1], f[2], (UnitClass)cls,
                            f.Length >= 5 && f[4].Length > 0 ? f[4] : c.NameForSavedFighter(f[2], f[1]));
                        if (f.Length >= 7)
                        {
                            if (!TryInt(f[5], out int row) || (row != (int)Row.Front && row != (int)Row.Back))
                                return Fail(where + "bad fighter record.", out failure);
                            fighter.Row = (Row)row;
                            if (f[6].Length > 0) worn.Add(new KeyValuePair<string, string>(fighter.Id, f[6]));
                        }
                        if (f.Length == 8 && !TryScars(f[7], fighter)) return Fail(where + "bad fighter record.", out failure);
                        c.AddFighter(fighter);
                        break;
                    case "beast":
                        // A beast beaten this run, by kit (version 16 only ever wrote 1, the shark): it is gone until the next run.
                        if (f.Length != 2 || !TryInt(f[1], out int beatenKit) || !c.RestoreBeastBeaten((BeastKit)beatenKit))
                            return Fail(where + "bad or repeated beast record.", out failure);
                        break;
                    case "piece":
                        if (!TryPiece(f, 0, out CampaignGear piece) || !c.RestoreGear(piece))
                            return Fail(where + "bad or repeated piece record.", out failure);
                        break;
                    case "spoils":
                        // One record per piece on offer, each ending with the same materials: one before version 15, up to
                        // three since.
                        if (spoils.Count >= CampaignGearRules.SpoilsOffered || !TryPiece(f, 1, out CampaignGear offered) ||
                            !TryInt(f[f.Length - 1], out int banked) || banked < 0 || (spoilsMaterials >= 0 && banked != spoilsMaterials))
                            return Fail(where + "bad spoils record.", out failure);
                        spoils.Add(offered);
                        spoilsMaterials = banked;
                        break;
                    case "talent":
                        if (version < 26 || f.Length != 3 || !TryInt(f[1], out int talentId) || (f[2] != "0" && f[2] != "1") ||
                            !c.RestoreTalent((GluttonTalent)talentId, f[2] == "1"))
                            return Fail(where + "bad or repeated talent record.", out failure);
                        break;
                    case "feed":
                        if (version < 29 || f.Length != 3 || !TryInt(f[1], out int fedId) || !TryInt(f[2], out int eatenId) ||
                            !c.RestoreFeed((GluttonTalent)fedId, (GluttonTalent)eatenId))
                            return Fail(where + "bad feed record.", out failure);
                        break;
                    case "hedge":
                        if (version < 29 || f.Length != 2 || !TryInt(f[1], out int hedges) || !c.RestoreHedges(hedges))
                            return Fail(where + "bad hedge record.", out failure);
                        break;
                    case "duo":
                        if (version < 30 || f.Length != 3 || !TryInt(f[1], out int duoMask) || !TryInt(f[2], out int duoShown) ||
                            !c.RestoreDuos(duoMask, duoShown))
                            return Fail(where + "bad duo record.", out failure);
                        break;
                    case "route":
                        if (version < 26 || f.Length != 3 || !TryInt(f[1], out int routeId) || !TryInt(f[2], out int routeForm) ||
                            !c.RestoreRoute((ShadeRoute)routeId, routeForm))
                            return Fail(where + "bad or repeated route record.", out failure);
                        break;
                    case "habitup":
                        // A habit the run upgraded (version 27). The habits themselves are camp records.
                        if (version < 27 || f.Length != 2 || !TryInt(f[1], out int upId) || !c.RestoreHabitUp((WhispererHabit)upId))
                            return Fail(where + "bad or repeated habit upgrade record.", out failure);
                        break;
                    case "whisperpick":
                        // Restored with the favour offer below.
                        // A seventh field, the vow, from version 31.
                        int pickVow = 0;
                        if (version < 27 || (f.Length != 6 && !(f.Length == 7 && version >= 31 && TryInt(f[6], out pickVow))) ||
                            !TryInt(f[1], out int pickIndex) || !TryInt(f[2], out int pickKind) ||
                            !TryInt(f[4], out int pickHabit) || !TryInt(f[5], out int pickPair))
                            return Fail(where + "bad Whisperer pick record.", out failure);
                        whisperPicks.Add(new WhisperPickRecord(pickIndex, (Favour)pickKind, f[3], (WhispererHabit)pickHabit, (SquadPair)pickPair,
                            (StarVow)pickVow));
                        break;
                    case "pair":
                        if (version < 26 || f.Length != 2 || !TryInt(f[1], out int pairId) || !c.RestorePair((SquadPair)pairId))
                            return Fail(where + "bad or repeated pair record.", out failure);
                        break;
                    case "rank":
                        if (version < 26 || f.Length != 3 || !TryInt(f[2], out int fighterRank) || !c.RestoreRank(f[1], fighterRank))
                            return Fail(where + "bad or repeated rank record.", out failure);
                        break;
                    case "vow":
                        // A ranked fighter's vow (version 31), after his rank.
                        if (version < 31 || f.Length != 3 || !TryInt(f[2], out int vowId) || !c.RestoreVow(f[1], (StarVow)vowId))
                            return Fail(where + "bad or repeated vow record.", out failure);
                        break;
                    case "slot2":
                        // Restored with the worn pieces below, once the run's gear is read.
                        if (version < 26 || f.Length != 3 || f[1].Length == 0 || f[2].Length == 0)
                            return Fail(where + "bad second slot record.", out failure);
                        seconds.Add(new KeyValuePair<string, string>(f[1], f[2]));
                        break;
                    case "favour":
                        // A favour the run holds, with its rank (version 13). Older saves have none.
                        if (f.Length != 3 || !TryInt(f[1], out int favourId) || !TryInt(f[2], out int favourRank) ||
                            !c.RestoreFavour((Favour)favourId, favourRank))
                            return Fail(where + "bad or repeated favour record.", out failure);
                        break;
                    case "favouroffer":
                        // The favours a won favour fight still offers: the Pal, the materials taking one gives back, then
                        // one to FavourRules.Offered favours (version 13).
                        if (favourOfferSeen || f.Length < 4 || f.Length > 3 + FavourRules.MaxOffered || !TryInt(f[1], out favourPal) ||
                            !TryInt(f[2], out favourMaterials) || favourMaterials < 0)
                            return Fail(where + "bad favour offer record.", out failure);
                        for (int i = 3; i < f.Length; i++)
                        {
                            if (!TryInt(f[i], out int choice)) return Fail(where + "bad favour offer record.", out failure);
                            favourChoices.Add((Favour)choice);
                        }
                        favourOfferSeen = true;
                        break;
                    case "streak":
                        // Fights won in a row this run (version 21). Older saves have none.
                        if (f.Length != 2 || !TryInt(f[1], out int streak) || !c.RestoreStreak(streak))
                            return Fail(where + "bad streak record.", out failure);
                        break;
                    case "lives":
                        // The lives the run has left (version 20). Older saves have them all.
                        if (f.Length != 2 || !TryInt(f[1], out int lives) || !c.RestoreLives(lives))
                            return Fail(where + "bad lives record.", out failure);
                        break;
                    case "bond":
                        // A bonded pair of this run (version 20). Older saves have none.
                        if (f.Length != 3 || !c.RestoreBond(f[1], f[2])) return Fail(where + "bad or repeated bond record.", out failure);
                        break;
                    case "grudge":
                        // A tribe that captured one of yours this run (version 9). Older saves have none.
                        if (f.Length != 2 || !c.RestoreGrudge(f[1])) return Fail(where + "bad or repeated grudge record.", out failure);
                        break;
                    case "assist":
                        if (f.Length != 3 || CampaignTribes.Find(f[1]) == null || !TryInt(f[2], out int grade))
                            return Fail(where + "bad assist record.", out failure);
                        c.AddAssist(Campaign.MakeAssist(f[1], grade));
                        break;
                    case "gear":
                        if (wearer != null || f.Length != 2 || f[1].Length == 0) return Fail(where + "bad gear record.", out failure);
                        wearer = f[1];
                        break;
                    case "run":
                        if (runTarget != null || f.Length != 4 || Campaign.FindTarget(f[1]) == null || !TryInt(f[2], out runStep) ||
                            runStep < 0 || !TryInt(f[3], out runLastKind) || runLastKind < -1 || runLastKind > (int)EncounterKind.Chief)
                            return Fail(where + "bad run record.", out failure);
                        runTarget = f[1];
                        break;
                    case "clearroad":
                        if (version < 22 || runTarget == null || clearRoadSeen || f.Length != 2 || (f[1] != "0" && f[1] != "1"))
                            return Fail(where + "bad clear road record.", out failure);
                        clearRoadSeen = true;
                        c.ClearRoadRun = f[1] == "1";
                        break;
                    case "palroad":
                        if (version < 25 || runTarget == null || palRoadSeen || f.Length != 2 || f[1] != "1")
                            return Fail(where + "bad Pal road record.", out failure);
                        palRoadSeen = true;
                        c.PalRoadRun = true;
                        break;
                    case "roadpals":
                        if (version < 24 || runTarget == null || roadPalsSeen || f.Length != 2 || !TryInt(f[1], out int roadPals) ||
                            roadPals <= 0 || roadPals >= 1 << (FavourRules.Pals.Length + 1))
                            return Fail(where + "bad road Pals record.", out failure);
                        roadPalsSeen = true;
                        c.RoadPals = roadPals;
                        break;
                    case "routepick":
                        if (runTarget == null || routePickSeen || f.Length != 2 || !TryInt(f[1], out int routePick) || routePick < 0 || routePick > 1)
                            return Fail(where + "bad route picker record.", out failure);
                        routePickSeen = true;
                        c.RoutePickVersion = routePick;
                        break;
                    case "road":
                        // The lane you stand on along the road (version 10). Older saves open on the whole next row.
                        if (runTarget == null || roadSeen || f.Length != 2 || !TryInt(f[1], out roadLane) || roadLane < -1 ||
                            roadLane >= CampaignRoad.Lanes)
                            return Fail(where + "bad road record.", out failure);
                        roadSeen = true;
                        break;
                    case "feat":
                        // The respect campaign's feats, gone since 10 Oct 2026.
                        break;
                    case "offer":
                        if (runTarget == null || f.Length != 2 || !TryInt(f[1], out int offerKind) || offerKind < 0 ||
                            offerKind > (int)EncounterKind.Ambush || offerKind == (int)EncounterKind.Tutorial)
                            return Fail(where + "bad offer record.", out failure);
                        offers.Add((EncounterKind)offerKind);
                        break;
                    case "fight":
                        // Version 15 adds the rules it began under (Campaign.RulesVersion); a fight saved before plays rules 0.
                        // Version 16 adds the beast fight, offer -2 (Campaign.BeastOfferIndex).
                        if (fightSeen || (f.Length != 3 && f.Length != 4) || !TryInt(f[1], out fightOffer) || fightOffer < Campaign.BeastOfferIndex ||
                            !TryLong(f[2], out fightTick) || fightTick < 0 ||
                            (f.Length == 4 && (!TryInt(f[3], out fightRules) || fightRules < 0 || fightRules > Campaign.RulesVersion)))
                            return Fail(where + "bad fight record.", out failure);
                        fightSeen = true;
                        break;
                    case "squad":
                        if (!fightSeen || f.Length != 2) return Fail(where + "bad squad record.", out failure);
                        squad.Add(f[1]);
                        break;
                    case "proof":
                        if (!fightSeen || proof != null || f.Length != 4) return Fail(where + "bad fight proof.", out failure);
                        proof = f;
                        break;
                    case "order":
                        if (!fightSeen || (f.Length != 4 && !(f.Length == 5 && f[4] == PausedOrder)) ||
                            !TryLong(f[1], out long orderTick) || orderTick < 0 || orderTick > fightTick ||
                            !Directives.TryParse(f[2], out Directive directive))
                            return Fail(where + "bad order record.", out failure);
                        orders.Add(new SavedOrder(orderTick, directive, f[3], f.Length == 4));
                        break;
                }
            }
            if (wearer != null && !c.RestoreTabardWearer(wearer))
                return Fail("The tabard is on someone who is not one of your fighters.", out failure);
            for (int i = 0; i < worn.Count; i++)
                if (!c.RestoreWorn(worn[i].Key, worn[i].Value))
                    return Fail("A fighter wears a piece the run does not have, or one someone else wears.", out failure);
            for (int i = 0; i < seconds.Count; i++)
                if (!c.RestoreSecond(seconds[i].Key, seconds[i].Value))
                    return Fail("A fighter's second slot holds a piece the run does not have, or one someone else wears.", out failure);
            if (spoils.Count > 0 && !c.RestoreSpoils(spoils, spoilsMaterials))
                return Fail("The spoils on offer cannot be read.", out failure);
            if (favourOfferSeen && !c.RestoreFavourOffer((Pal)favourPal, favourChoices, favourMaterials, version >= 25))
                return Fail("The favours on offer cannot be read.", out failure);
            // The Whisperer's picks, one for each of her choices (version 27).
            for (int i = 0; i < whisperPicks.Count; i++)
            {
                WhisperPickRecord w = whisperPicks[i];
                if (!c.RestoreWhisperPick(w.Index, w.Kind, w.FighterId, w.Habit, w.Pair, w.Vow))
                    return Fail("The Whisperer's picks on offer cannot be read.", out failure);
            }
            if (c.FavourOffer != null)
                for (int i = 0; i < c.FavourOffer.Choices.Count; i++)
                    if (FavourRules.IsWhisper(c.FavourOffer.Choices[i]) && c.FavourOffer.Picks.Count <= i)
                        return Fail("The Whisperer's picks on offer cannot be read.", out failure);
            // Before the run resumes, so its offers see the bounty and who was beaten.
            if (!c.RestoreRunHunt(heat, beaten, promoted)) return Fail("The run's hunters cannot be read.", out failure);
            if (runTarget != null)
            {
                if (version >= 14 && !routePickSeen) return Fail("The saved run has no route picker version.", out failure);
                if (version >= 22 && !clearRoadSeen) return Fail("The saved run has no road layout version.", out failure);
                CampaignTribe target = Campaign.FindTarget(runTarget);
                if (!c.TutorialDone || target.IsStarter || c.HasJoined(target.Id))
                    return Fail("The saved run is against a tribe a run cannot go after.", out failure);
                if (offers.Count == 0) return Fail("The saved run has no encounters on offer.", out failure);

                // The hunt after the last chief is one fight, and no road offers it (an ambush on a road is its own kind).
                bool hunt = target == AlphaHunt.Tribe;
                if (hunt && offers.Count != 1) return Fail("The saved hunt offers more than one fight.", out failure);
                for (int i = 0; i < offers.Count; i++)
                    if ((offers[i] == EncounterKind.AlphaHunt) != hunt)
                        return Fail("The saved run's encounters do not match who it is against.", out failure);
                if (!c.ResumeRun(target, runStep, runLastKind, new Feat[0], roadLane))
                    return Fail("The saved run's place on its road cannot be read.", out failure);
            }
            if (fightSeen)
            {
                bool beast = fightOffer == Campaign.BeastOfferIndex;
                bool tutorial = fightOffer < 0 && !beast;
                if (beast ? c.BeastOffer == null
                    : tutorial ? c.Stage != CampaignStage.Tutorial : c.Stage != CampaignStage.InRun || fightOffer >= c.Offers.Count)
                    return Fail("The saved fight does not match where the campaign stands.", out failure);
                string beforeFight = Write(c);
                c.ResumeRulesVersion = fightRules;
                // Correcting the reused battle identity must not discard a recent, still-exact active fight.
                // Both candidate proofs require the same tuning hash, setup, accepted orders and state checks.
                if (proof == null || (proof[1] != ResumeRules(c.Tuning) &&
                    proof[1] != ResumeRules(c.Tuning, ReplayEvidence.PreSpellStrengthIdentity)) ||
                    !Replay(c, fightOffer, squad, fightTick, orders, proof))
                {
                    // Old proofs, changed rules or a damaged tape: restore the pre-fight campaign
                    // including its RNG and counters, without applying rewards or lessons twice.
                    if (!TryRead(beforeFight, tuning, out Campaign back, out failure)) return false;
                    back.LoadNote = FightLostNote;
                    campaign = back;
                    return true;
                }
            }
            campaign = c;
            return true;
        }

        /// Starts the saved fight again and gives its orders on the ticks they were given, up to the
        /// tick it was saved on. False if anything comes out differently.
        private static bool Replay(Campaign c, int offer, List<string> squad, long tick, List<SavedOrder> orders, string[] proof)
        {
            CampaignFight fight;
            try
            {
                fight = offer == Campaign.BeastOfferIndex ? c.StartBeast(squad.Count == 0 ? null : squad)
                    : offer < 0 ? c.StartTutorial() : c.StartEncounter(offer, squad.Count == 0 ? null : squad);
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
            if (tick > fight.Setup.TickLimit || proof[2] != ReplayEvidence.SetupIdentity(fight.Setup)) return false;
            int next = 0;
            while (true)
            {
                while (next < orders.Count && orders[next].Tick == fight.Sim.CurrentTick)
                {
                    if (!fight.TryOrder(orders[next].Directive, orders[next].Target, orders[next].Live, out _)) return false;
                    next++;
                }
                if (fight.Sim.CurrentTick >= tick || fight.Over) break;
                if (next < orders.Count && orders[next].Tick < fight.Sim.CurrentTick) return false;
                fight.Tick();
            }
            return next == orders.Count && fight.Sim.CurrentTick == tick && proof[3] == fight.Session.Checksum;
        }

        /// The fifth field of an order given while the fight was paused.
        private const string PausedOrder = "paused";

        private readonly struct SavedOrder
        {
            public SavedOrder(long tick, Directive directive, string target, bool live)
            {
                Tick = tick;
                Directive = directive;
                Target = target;
                Live = live;
            }

            public long Tick { get; }
            public Directive Directive { get; }
            public string Target { get; }
            public bool Live { get; }
        }

        private static void WriteMind(StringBuilder b, TribeMind mind)
        {
            Line(b, "mind", mind.TribeId);
            WriteMindLines(b, mind);
        }

        // A mind's lines after its record: habits (and, version 19, the moves the parry habits sit on), threat, the fights it
        // learned from and its lessons (with a lesson's move as a twelfth field when it has one).
        private static void WriteMindLines(StringBuilder b, TribeMind mind)
        {
            for (int i = 0; i < TribeMind.HabitCount; i++)
                if (mind.SavesHabit((Habit)i)) Line(b, "habit", N(i), N(mind.HabitValue((Habit)i)));
            if (mind.WaitOutMove.Length > 0) Line(b, "habitmove", N((int)Habit.WaitOutParry), mind.WaitOutMove);
            if (mind.FeintMove.Length > 0) Line(b, "habitmove", N((int)Habit.Feint), mind.FeintMove);
            for (int i = 0; i < SliceClasses.Count; i++) Line(b, "threat", N(i), N(mind.ThreatMemory((UnitClass)i)));
            for (int i = 0; i < mind.FightsLearned.Count; i++) Line(b, "learned", mind.FightsLearned[i]);
            for (int i = 0; i < mind.Lessons.Count; i++)
            {
                Lesson l = mind.Lessons[i];
                if (l.Move.Length > 0)
                    Line(b, "lesson", l.FightId, N(l.LearnerSide), l.Rule, N((int)l.Subject), N(l.ValueBefore), N(l.ValueAfter),
                        N(l.Evidence), l.FromTick.ToString(CultureInfo.InvariantCulture), l.ToTick.ToString(CultureInfo.InvariantCulture), l.Why,
                        l.Move);
                else
                    Line(b, "lesson", l.FightId, N(l.LearnerSide), l.Rule, N((int)l.Subject), N(l.ValueBefore), N(l.ValueAfter),
                        N(l.Evidence), l.FromTick.ToString(CultureInfo.InvariantCulture), l.ToTick.ToString(CultureInfo.InvariantCulture), l.Why);
            }
        }

        // Everyone's names in one recorded fight: its id, then each unit id and name, in id order.
        private static void WriteNames(StringBuilder b, ArchivedFight fight)
        {
            if (fight.Names.Count == 0) return;
            var ids = new List<string>(fight.Names.Keys);
            ids.Sort(StringComparer.Ordinal);
            var fields = new List<string> { "names", fight.FightId };
            for (int i = 0; i < ids.Count; i++)
            {
                fields.Add(ids[i]);
                fields.Add(fight.Names[ids[i]]);
            }
            Line(b, fields.ToArray());
        }

        // A fighter's road scars (version 16): one digit per part (BeastPart, the cage bear's 4 to 7 from version 17), in the
        // order earned; empty for none.
        private static string Scars(CampaignFighter f)
        {
            var s = new StringBuilder();
            for (int i = 0; i < f.Scars.Count; i++) s.Append(N((int)f.Scars[i]));
            return s.ToString();
        }

        // Gives the fighter the scars a record names. False for anything but distinct part digits.
        private static bool TryScars(string text, CampaignFighter fighter)
        {
            for (int i = 0; i < text.Length; i++)
            {
                int part = text[i] - '0';
                if (part < 0 || part >= BeastKits.AllPartCount || !fighter.AddScar((BeastPart)part)) return false;
            }
            return true;
        }

        // A piece of run gear: its id, tribe, tier, stat, special and name, its class and bend (version 15), then anything
        // extra (the spoils' materials).
        private static void WritePiece(StringBuilder b, string record, CampaignGear piece, params string[] extra)
        {
            var fields = new List<string>
            {
                record, piece.Id, piece.TribeId, N((int)piece.Tier), N((int)piece.Stat), N((int)piece.Special), piece.Name,
                N(piece.ClassIndex), N((int)piece.Bend)
            };
            // A ranked combo's rank (version 24). Combos never drop, so no record with extra fields carries one.
            if (Version >= 24 && piece.Rank > 1) fields.Add(N(piece.Rank));
            fields.AddRange(extra);
            Line(b, fields.ToArray());
        }

        // A piece record with this many extra fields at the end: id, tribe, tier, stat, special and name first, then its
        // class and bend since version 15, then a ranked combo's rank since version 24. Every special reads, the new tribes'
        // chiefs' included (18 Oct 2026).
        private static bool TryPiece(string[] f, int extra, out CampaignGear piece)
        {
            piece = null;
            int n = f.Length - extra;
            if ((n != 7 && n != 9 && n != 10) || f[1].Length == 0 || CampaignTribes.Find(f[2]) == null || !TryInt(f[3], out int tier) ||
                !TryInt(f[4], out int stat) || !TryInt(f[5], out int special) || tier < 0 || tier > (int)GearTier.Princess ||
                stat < 0 || stat > (int)GearStat.Avoid || special < 0 || special > (int)GearSpecial.Predator)
                return false;
            int cls = CampaignGear.AnyClass, bend = 0, rank = 1;
            if (n == 10 && (!TryInt(f[9], out rank) || rank < 2 || rank > MarkRules.MaxStacks)) return false;
            if (n >= 9 && (!TryInt(f[7], out cls) || cls < CampaignGear.AnyClass || cls >= SliceClasses.Count ||
                !TryInt(f[8], out bend) || bend < 0 || bend >= ClassSpells.BendCount ||
                (bend != 0 && (cls == CampaignGear.AnyClass || !ClassSpells.IsFor((ClassBend)bend, (UnitClass)cls)))))
                return false;
            if (n == 10 && !ClassSpells.IsCombo((ClassBend)bend)) return false;
            piece = new CampaignGear(f[1], f[2], (GearTier)tier, (GearStat)stat, (GearSpecial)special, f[6], cls, (ClassBend)bend);
            piece.Rank = rank;
            return true;
        }

        private static void Line(StringBuilder b, params string[] fields)
        {
            for (int i = 0; i < fields.Length; i++)
            {
                if (i > 0) b.Append('\t');
                b.Append(Escape(fields[i]));
            }
            b.Append('\n');
        }

        private static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            var b = new StringBuilder(s.Length);
            foreach (char ch in s)
            {
                switch (ch)
                {
                    case '\\': b.Append("\\\\"); break;
                    case '\t': b.Append("\\t"); break;
                    case '\n': b.Append("\\n"); break;
                    case '\r': b.Append("\\r"); break;
                    default: b.Append(ch); break;
                }
            }
            return b.ToString();
        }

        private static string Unescape(string s)
        {
            if (s.IndexOf('\\') < 0) return s;
            var b = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                char ch = s[i];
                if (ch != '\\' || i + 1 >= s.Length)
                {
                    b.Append(ch);
                    continue;
                }
                char next = s[++i];
                b.Append(next == 't' ? '\t' : next == 'n' ? '\n' : next == 'r' ? '\r' : next);
            }
            return b.ToString();
        }

        private static bool Fail(string why, out string failure)
        {
            failure = why;
            return false;
        }

        private static bool TryInt(string s, out int value)
        {
            return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryLong(string s, out long value)
        {
            return long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryU(string s, out ulong value)
        {
            return ulong.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        private static string N(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        private static string L(long value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        private static string U(ulong value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }
    }
}
