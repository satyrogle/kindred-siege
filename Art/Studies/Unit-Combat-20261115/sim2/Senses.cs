// Layer 2: senses and belief. Every tick the field is written into a short history. A fighter never reads the truth:
// he reads the history as it stood his reaction time ago (4 to 7 ticks, from Speed and personality; FightingICE forces
// 15 frames at 60 fps, which is 5 ticks here). His target and anyone coming for him are watched; everyone else is seen at
// twice the delay. A feint is written into the history as the heavy it pretends to be.
using System;

namespace KindredSiege.Sim.Brawl2
{
    public static class Senses
    {
        public const int HistLen = 32;
        /// The history belongs to one fight (Fight.SenseHist); the static is only a pointer to the fight being stepped
        /// (it was the history itself, v16 audit).
        [ThreadStatic] static Seen[,] _hist;
        [ThreadStatic] static int _n;
        public static void Use(Seen[,] hist) { _hist = hist; _n = hist.GetLength(1); }

        public static int ReactTicks(Unit u)
        {
            int t = 7 - u.Six.Speed / 25;                     // Speed 20 sees 6 ticks late, 100 sees 3
            if (u.Trait == Trait.Veteran) t -= 1;
            if (u.Trait == Trait.Recruit) t += 1;
            return Math.Max(3, Math.Min(7, t + 1));
        }

        public static Seen[,] Init(Fight f, int n) { _n = n; _hist = new Seen[HistLen, n]; return _hist; }

        public static void Observe(Fight f)
        {
            int row = f.Tick % HistLen;
            for (int i = 0; i < _n; i++) _hist[row, i] = Public(f, f.Units[i]);
        }

        static Seen Public(Fight f, Unit u)
        {
            var s = new Seen
            {
                X = u.X, Y = u.Y, Facing = u.Facing, Hp = u.Hp, MaxHp = u.MaxHp, Target = u.Target, MoveTarget = u.MoveTarget,
                Age = u.Age, Act = u.Act, Looks = MoveKind.None,
                Marks = (f.Tick < u.OpeningUntil ? 1 : 0) | (f.Tick < u.HexUntil ? 2 : 0) | (f.Tick < u.SnareMarkUntil ? 4 : 0) | (f.Tick < u.BleedUntil ? 8 : 0),
            };
            if (u.Move != null && (u.Act == Act.Attack || u.Act == Act.Boost || u.Act == Act.Feint))
            {
                s.Looks = u.Act == Act.Feint ? MoveKind.Heavy : u.Move.Kind;     // the lie a feint tells
                s.Tell = u.Move.Has(u.Age, Tag.Tell);
                s.Vulnerable = u.Move.Has(u.Age, Tag.Vulnerable);                   // a feint's wind back is open too
            }
            if (u.Act == Act.Stagger || u.Act == Act.Down || u.Act == Act.GetUp || u.Act == Act.Hurt || u.Act == Act.Cower) s.Vulnerable = true;
            return s;
        }

        /// What viewer believes other is doing now.
        public static Seen See(Fight f, Unit viewer, Unit other)
        {
            int delay = viewer.React;
            if (!Watching(viewer, other)) delay *= 2;
            if (delay > HistLen - 2) delay = HistLen - 2;
            int t = Math.Max(0, f.Tick - delay);
            return _hist[t % HistLen, other.Id];
        }

        /// Attention: his own man, and anyone he can see coming for him.
        public static bool Watching(Unit viewer, Unit other) => other.Id == viewer.Target || other.Target == viewer.Id || other.MoveTarget == viewer.Id;

        public static int Px(int c) => c / R.C;
    }
}
