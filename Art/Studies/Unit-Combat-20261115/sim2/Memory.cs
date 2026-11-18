// Layer 6: memory inside one fight (Killer Instinct's Shadow drops what keeps getting punished; F.E.A.R.'s soldier tries
// the door, then the window). Plain integer counts per enemy, read by the brain: what each of my moves did against him,
// what he did when he saw my wind up, who hurt me most, who killed my friend.
namespace KindredSiege.Sim.Brawl2
{
    public sealed class Memory
    {
        // my heavy against each enemy
        public readonly int[] HeavyLanded, HeavyDodged, HeavyBlocked, HeavyWhiffed;
        public readonly int[] QuickLanded, QuickBlocked;
        // what each enemy does when he sees my tell
        public readonly int[] SawDodge, SawBlock, SawTrade;
        // grudges
        public readonly int[] DamageFrom;
        public int Killer = -1;          // who killed the friend I saw fall
        public int FeintsBitten;         // how often I dodged a feint

        public Memory(int n)
        {
            HeavyLanded = new int[n]; HeavyDodged = new int[n]; HeavyBlocked = new int[n]; HeavyWhiffed = new int[n];
            QuickLanded = new int[n]; QuickBlocked = new int[n];
            SawDodge = new int[n]; SawBlock = new int[n]; SawTrade = new int[n];
            DamageFrom = new int[n];
        }

        /// A known dodger: he has slipped my heavy more than it landed, at least twice.
        public bool Dodger(int e) => SawDodge[e] >= 2 && SawDodge[e] > HeavyLanded[e];
        /// A known blocker: my quick keeps hitting his guard.
        public bool Blocker(int e) => QuickBlocked[e] >= 2 && QuickBlocked[e] > QuickLanded[e];
        /// My heavy is failing on him.
        public bool HeavyFails(int e) => HeavyDodged[e] + HeavyWhiffed[e] >= 2 && HeavyDodged[e] + HeavyWhiffed[e] > HeavyLanded[e];

        public int Grudge()
        {
            int best = -1, bestDmg = 0;
            for (int i = 0; i < DamageFrom.Length; i++) if (DamageFrom[i] > bestDmg) { bestDmg = DamageFrom[i]; best = i; }
            return best;
        }
    }
}
