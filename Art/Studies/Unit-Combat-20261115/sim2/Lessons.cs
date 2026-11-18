// Layer 8: tribe lessons across fights (Alien: Isolation's locked branches, unlocked by what the player did that worked,
// never by the player's losses). After a fight the log is read for the player's side's successes; the biggest one unlocks
// its counter for the rival tribe, one lesson per fight (the 25 Oct lock: a chief learns one lesson per fight). The next
// fight shows it: a line before the fight and a changed formation.
namespace KindredSiege.Sim.Brawl2
{
    public enum Lesson { None, GuardShooter, SpreadOut, KillHexer, KillTrapper, BaitBash }

    public static class Lessons
    {
        public static string Name(Lesson l) => l switch
        {
            Lesson.GuardShooter => "guard the shooter",
            Lesson.SpreadOut => "spread out",
            Lesson.KillHexer => "kill the Hexer first",
            Lesson.KillTrapper => "kill the Trapper first",
            Lesson.BaitBash => "bait the bash",
            _ => ""
        };

        /// What the player's side (side 0) did that worked, counted from the log.
        public static Lesson Learn(Fight f, out string evidence)
        {
            int dives = 0, crowds = 0, hexCash = 0, snareCash = 0, bashes = 0;
            int burstTick = -1, burstHits = 0;
            foreach (var e in f.Events)
            {
                var a = f.Get(e.A); var b = f.Get(e.B);
                if (e.K == "kill" && a != null && b != null && a.Side == 0 && a.Cls == Cls.Skirmisher && R.Ranged(b.Cls)) dives++;
                if (e.K == "hit" && a != null && a.Side == 0 && a.Cls == Cls.Hexer)
                {
                    if (e.T != burstTick) { if (burstHits >= 3) crowds++; burstTick = e.T; burstHits = 0; }
                    burstHits++;
                }
                if (e.K == "hit" && e.Detail == "hexed" && a != null && a.Side == 0) hexCash++;
                if (e.K == "hit" && e.Detail == "pinned" && a != null && a.Side == 0) snareCash++;
                if (e.K == "stagger" && a != null && a.Side == 0 && a.Cls == Cls.Warden) bashes++;
            }
            if (burstHits >= 3) crowds++;
            // weights: a dive kill matters most, a stagger least
            int[] score = { 0, dives * 5, crowds * 3, hexCash * 2, snareCash * 2, bashes };
            Lesson best = Lesson.None; int top = 0;
            for (int i = 1; i < score.Length; i++) if (score[i] > top) { top = score[i]; best = (Lesson)i; }
            evidence = $"dive kills {dives}, curse crowds {crowds}, hex cashes {hexCash}, snare cashes {snareCash}, bash staggers {bashes}";
            return best;
        }
    }
}
