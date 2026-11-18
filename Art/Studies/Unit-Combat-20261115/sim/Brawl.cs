// Kindred Siege unit combat study, 15 Nov 2026 (Claude). An isolated fight for Jacob to judge, not production.
// Every class is a fighter (DESIGN_LOCKS 14 Nov), the 17 rewritten with kinds of hit and stopping stats, the Hex,
// Shield Pin, Evil Eye, Bad Blood and the marks web (15 Nov). Built in the sim's own style: integers only, one
// DeterministicRng stream per fighter, ordered lists, every change inside Tick(). Every number is a starting value.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using KindredSiege.Sim;

namespace KindredSiege.Sim.Brawl
{
    public enum Cls { Warden, Marksman, Skirmisher, Trapper, Hexer }
    public enum Kind { Blade, Shot, Spell, Hold }
    public enum Act { Idle, Move, Attack, Boost, Dodge, Hurt, Stagger, Dead }

    [Flags]
    public enum Fx { None = 0, Class = 1, Bash = 2, Snare = 4, Projectile = 8, Finisher = 16, Bleed = 32, Pin = 64, Spread = 128 }

    public readonly struct Six
    {
        public readonly int Health, Attack, Defence, Speed, Spirit, Will;
        public Six(int h, int a, int d, int s, int sp, int w) { Health = h; Attack = a; Defence = d; Speed = s; Spirit = sp; Will = w; }
    }

    public sealed class HitSpec
    {
        public int Frame;   // the clip frame the blow lands on
        public int Step;    // resolved: the tick of the move it lands on
        public Kind Kind;
        public int Share;   // percent of the move's power
        public Fx Fx;
        public HitSpec(int frame, Kind kind, int share, Fx fx) { Frame = frame; Kind = kind; Share = share; Fx = fx; }
    }

    public sealed class MoveSpec
    {
        public string Name, Clip;
        public int[] Frames;            // the clip frame shown on each tick of the move
        public HitSpec[] Hits;
        public int Reach;               // melee: feet to feet at contact, px. Ranged: max range, px
        public int DashFrom = -1, DashTo = -1, DashPx;  // forward root motion between these clip frames
        public int Ticks => Frames.Length;

        public static MoveSpec Make(string name, string clip, string frames, int reach, params HitSpec[] hits)
        {
            var list = new List<int>();
            foreach (var part in frames.Split(','))
            {
                var p = part.Trim();
                int dash = p.IndexOf('-');
                if (dash > 0)
                {
                    int a = int.Parse(p.Substring(0, dash), CultureInfo.InvariantCulture), b = int.Parse(p.Substring(dash + 1), CultureInfo.InvariantCulture);
                    for (int f = a; f <= b; f++) list.Add(f);
                }
                else list.Add(int.Parse(p, CultureInfo.InvariantCulture));
            }
            var m = new MoveSpec { Name = name, Clip = clip, Frames = list.ToArray(), Hits = hits, Reach = reach };
            foreach (var h in hits)
            {
                h.Step = -1;
                for (int i = 0; i < m.Frames.Length; i++) if (m.Frames[i] == h.Frame) { h.Step = i; break; }
                if (h.Step < 0) throw new InvalidOperationException(name + " " + clip + ": hit frame " + h.Frame + " is not in the move");
            }
            return m;
        }

        public MoveSpec Dash(int fromFrame, int toFrame, int px) { DashFrom = Step(fromFrame); DashTo = Step(toFrame); DashPx = px; return this; }
        public int Step(int frame) { for (int i = 0; i < Frames.Length; i++) if (Frames[i] == frame) return i; throw new InvalidOperationException(Name + ": no frame " + frame); }
    }

    /// The numbers. All starting values (build first, 12 Oct): nothing here is tuned.
    public static class Rules
    {
        public const int TickMs = 50;               // one clip frame per tick
        public const int C = 100;                   // centipixels per pixel
        public const int VL = 20, L = 40, M = 60, H = 80, VH = 100;  // the 7 Nov ladder

        public static int T(int ms) => (ms + TickMs - 1) / TickMs;

        /// 15 Nov spreads (DESIGN_LOCKS "The 17 classes rewritten").
        public static Six SixOf(Cls c)
        {
            switch (c)
            {
                case Cls.Warden: return new Six(H, M, VH, VL, VL, L);
                case Cls.Marksman: return new Six(L, VH, VL, M, L, M);
                case Cls.Skirmisher: return new Six(M, VH, VL, VH, L, L);
                case Cls.Trapper: return new Six(M, M, L, H, H, M);
                default: return new Six(L, VL, L, M, VH, VH); // Hexer
            }
        }

        public static Kind KindOf(Cls c) => c == Cls.Marksman ? Kind.Shot : c == Cls.Hexer ? Kind.Spell : Kind.Blade;
        public static bool Ranged(Cls c) => c == Cls.Marksman || c == Cls.Hexer;

        /// A weak stop doubles a hit, a strong stop halves it (15 Nov, Claude's working rule).
        public static int StopPercent(int stop) => stop <= VL ? 200 : stop <= L ? 150 : stop <= M ? 100 : stop <= H ? 75 : 50;

        public static int MaxHp(Six s) => 300 + s.Health * 7;
        public const int PowerScale = 80;           // percent of a stat a whole move deals before the stop
        public const int ShieldLeaves = 25;         // a shot into the Warden's shield lands for this percent
        public const int CashPercent = 200;         // a cash lands double (20 Oct)

        public static int SpeedPx(Six s) => 70 + s.Speed * 13 / 10;            // px per second
        public static int ReactTicks(Six s) => T(520 - s.Speed * 7 / 2);      // how late he sees a wind up
        public static int DodgePx(Six s) => 50 + s.Speed * 8 / 10;
        public static int DodgeCooldown(Six s) => T(4200 - s.Speed * 25);
        public static int DodgeWill(Six s) => Math.Max(0, Math.Min(75, s.Speed - 30));  // percent, before bonuses

        public static int RestTicks(Cls c) => c switch { Cls.Warden => T(1000), Cls.Skirmisher => T(500), Cls.Trapper => T(800), Cls.Marksman => T(1100), _ => T(2000) };
        public static int Patience(Cls c) => c switch { Cls.Skirmisher => T(500), Cls.Trapper => T(300), _ => 0 };

        public static readonly int SnareCooldown = T(9000);
        public static readonly int BoostCooldown = T(15000);
        public static readonly int DefenceCooldown = T(6000);
        public static readonly int OpeningTicks = T(4000), HexTicks = T(5000), SnareMarkTicks = T(4000), BleedTicks = T(5000);
        public static readonly int HoldTicks = T(2000), PinTicks = T(2000), StaggerTicks = T(900);
        public static readonly int BleedEvery = T(500);
        public const int BleedPermille = 15;        // of max health, every BleedEvery
        public const int BracePercent = 60;
        public const int SplashPercent = 50, PiercePercent = 50;
        public const int SplashPx = 95, CursePx = 46, ProjectilePxPerSecond = 560;
        public const int HitWindowPx = 34, LaneWindowPx = 44;   // how far off the reach a blow still lands
        public const int BodyPx = 62;               // two feet closer than this push apart
        public const int TrackPx = 7, TrackMaxPx = 60; // wind up tracking, per tick and in all
        // the accepted burial court's floor (StreamingAssets/CampaignArt/Bloodridge), on the game's 1440x900 fight canvas
        public const int FloorLeft = 170, FloorRight = 1290, FloorTop = 440, FloorBottom = 790, Middle = 720;
    }

    /// Each tribe's own approved clips (Assets/Resources/ClarthStudy), cut so every strike lands as its own blow.
    public static class Moves
    {
        static HitSpec Hit(int frame, Kind k, int share, Fx fx = Fx.None) => new HitSpec(frame, k, share, fx);

        public static MoveSpec Attack(string tribe, Cls c)
        {
            bool ours = tribe == "clarth";
            switch (c)
            {
                case Cls.Warden:
                    return ours
                        ? MoveSpec.Make("attack", "attack", "0-8,8,8,9-35", 96,
                            Hit(9, Kind.Blade, 50, Fx.Class | Fx.Bash), Hit(17, Kind.Blade, 25), Hit(26, Kind.Blade, 25, Fx.Finisher))
                        : MoveSpec.Make("attack", "attack", "0-6,6,7-19,19,20-29", 96,
                            Hit(7, Kind.Blade, 30), Hit(20, Kind.Blade, 70, Fx.Class | Fx.Bash));
                case Cls.Skirmisher:
                    return ours
                        ? MoveSpec.Make("attack", "attack", "0-3,3,4-20", 84,
                            Hit(7, Kind.Blade, 45, Fx.Class), Hit(16, Kind.Blade, 55, Fx.Finisher)).Dash(3, 7, 110)
                        : MoveSpec.Make("attack", "attack", "0-36", 84,
                            Hit(3, Kind.Blade, 30, Fx.Class), Hit(8, Kind.Blade, 15), Hit(16, Kind.Blade, 20), Hit(22, Kind.Blade, 20), Hit(29, Kind.Blade, 15, Fx.Finisher)).Dash(0, 3, 70);
                case Cls.Trapper:
                    return ours
                        ? MoveSpec.Make("attack", "attack", "0-12,26-39", 92,
                            Hit(8, Kind.Hold, 45, Fx.Class | Fx.Snare), Hit(30, Kind.Blade, 55, Fx.Finisher))
                        : MoveSpec.Make("attack", "attack", "0-20", 108,
                            Hit(14, Kind.Hold, 100, Fx.Class | Fx.Snare));
                case Cls.Marksman:
                    return ours
                        ? MoveSpec.Make("attack", "attack", "2-9,9,9,10-39", 560,
                            Hit(11, Kind.Shot, 40, Fx.Class), Hit(14, Kind.Shot, 30), Hit(20, Kind.Shot, 30))
                        : MoveSpec.Make("attack", "attack", "0-9,9,9,10-39", 560,
                            Hit(10, Kind.Shot, 30, Fx.Class), Hit(13, Kind.Shot, 15), Hit(16, Kind.Shot, 15), Hit(30, Kind.Shot, 20), Hit(34, Kind.Shot, 20));
                default: // Hexer: the blast flies, bursts on her target and splashes the men next to him (14 Nov)
                    return ours
                        ? MoveSpec.Make("attack", "attack", "0-3,3,3,4-5,34-39", 470,
                            Hit(4, Kind.Spell, 100, Fx.Class | Fx.Projectile))
                        : MoveSpec.Make("attack", "attack", "0-7,7,8-14,30-39", 470,
                            Hit(9, Kind.Spell, 100, Fx.Class | Fx.Projectile));
            }
        }

        /// Slot 3 moves that play a clip. Shield Pin and Twin Cut reuse the attack's own strike frames.
        public static MoveSpec Boost(string tribe, Cls c)
        {
            bool ours = tribe == "clarth";
            switch (c)
            {
                case Cls.Warden:
                    return ours ? MoveSpec.Make("pin", "attack", "4-12,12,12,31-35", 100, Hit(9, Kind.Blade, 0, Fx.Pin))
                                : MoveSpec.Make("pin", "attack", "5-14,14,26-29", 100, Hit(12, Kind.Blade, 0, Fx.Pin));
                case Cls.Skirmisher:
                    return ours ? MoveSpec.Make("twin", "attack", "3-9,14-20", 84, Hit(7, Kind.Blade, 50, Fx.Bleed), Hit(16, Kind.Blade, 50, Fx.Bleed))
                                : MoveSpec.Make("twin", "attack", "0-10", 84, Hit(3, Kind.Blade, 50, Fx.Bleed), Hit(8, Kind.Blade, 50, Fx.Bleed));
                case Cls.Hexer:
                    return ours ? MoveSpec.Make("badblood", "attack", "0-5,5,5,4-0", 600, Hit(5, Kind.Spell, 0, Fx.Spread))
                                : MoveSpec.Make("badblood", "attack", "0-8,8,8,7-0", 600, Hit(8, Kind.Spell, 0, Fx.Spread));
                default: return null;
            }
        }
    }

    public sealed class Unit
    {
        public int Id, Side;
        public Cls Cls;
        public string Tribe, Name;
        public Six Six;
        public int Hp, MaxHp;
        public int X, Y;                       // feet, centipixels
        public int HomeX, HomeY;
        public Act Act = Act.Idle;
        public int Age;                        // ticks in the current act
        public MoveSpec Move;
        public int NextHit;                    // index into Move.Hits
        public int MoveTarget = -1;            // who the move is aimed at
        public int Target = -1;                // who his brain is on
        public int GoalX, GoalY;
        public bool Moving;
        public int AttackReadyAt, ReadySince = -1, SnareReadyAt, BoostReadyAt, DefenceReadyAt, DodgeReadyAt, RearmReadyAt;
        public int HeldUntil, PinnedUntil, StaggerUntil;   // held = snared: can't move or dodge. pinned: can't dodge
        public int OpeningUntil, HexUntil, SnareMarkUntil, BleedUntil, BleedNext;
        public int DodgeFromX, DodgeFromY, DodgeToX, DodgeToY, DodgeTicks;
        public int StaggerTicks, Tracked;
        public bool PierceNext;
        public int LastHitBy = -1, LastHitAt = -1000, LastDodgeAt = -1000;
        public int TargetPickedAt = -1000;
        public string Why = "";
        public DeterministicRng Rng;
        public bool Alive => Act != Act.Dead;
        public int Face => Side == 0 ? 1 : -1;
        public int Px => X / Rules.C;
        public int Py => Y / Rules.C;
    }

    public sealed class Projectile
    {
        public int Id, Owner, Target, FromX, FromY, ToX, ToY, Launched, Arrives, Power, Share;
        public Fx Fx;
        public bool Done;
    }

    public sealed class Ev
    {
        public int T;
        public string K;                       // what happened
        public int A = -1, B = -1;             // actor, victim
        public int Amount;
        public string Detail = "";
        public int X, Y;                       // px, where the screen should dress it
        public int Tier;                       // 0 light, 1 heavy, 2 cash, 3 kill
        public string Kind = "";
    }

    public sealed class Fight
    {
        readonly List<Unit> _units = new List<Unit>();
        readonly List<Projectile> _shots = new List<Projectile>();
        readonly List<Ev> _events = new List<Ev>();
        readonly StringBuilder _trace = new StringBuilder();
        int _tick, _nextShot;
        public int Winner = -1, EndTick = -1;
        public IReadOnlyList<Unit> Units => _units;
        public IReadOnlyList<Ev> Events => _events;
        public int Tick => _tick;
        public bool Over => EndTick >= 0;

        public Unit Add(int side, Cls cls, string tribe, string name, int px, int py, ulong seed)
        {
            var six = Rules.SixOf(cls);
            var u = new Unit
            {
                Id = _units.Count, Side = side, Cls = cls, Tribe = tribe, Name = name, Six = six,
                MaxHp = Rules.MaxHp(six), X = px * Rules.C, Y = py * Rules.C,
                Rng = new DeterministicRng(seed * 7919UL + (ulong)_units.Count * 104729UL + 17UL, (ulong)(_units.Count + 1)),
            };
            u.Hp = u.MaxHp; u.HomeX = u.X; u.HomeY = u.Y; u.GoalX = u.X; u.GoalY = u.Y;
            u.AttackReadyAt = Rules.T(300) + u.Rng.Next(Rules.T(500));
            u.BoostReadyAt = Rules.T(5000) + u.Rng.Next(Rules.T(4000));
            u.RearmReadyAt = 0;
            _units.Add(u);
            return u;
        }

        // ------------------------------------------------------------------ the tick

        public void Step()
        {
            if (Over && _tick > EndTick + Rules.T(3000)) return;
            foreach (var u in _units) Bleed(u);
            // nobody always acts first: the order flips every tick, so a blow landing on the same tick as another
            // is not decided by which band was added first
            bool flip = (_tick & 1) == 1;
            for (int k = 0; k < _units.Count; k++) { var u = _units[flip ? _units.Count - 1 - k : k]; if (u.Alive) Think(u); }
            for (int k = 0; k < _units.Count; k++) Advance(_units[flip ? _units.Count - 1 - k : k]);
            Separate();
            Projectiles();
            CheckEnd();
            Record();
            _tick++;
        }

        void CheckEnd()
        {
            if (Over) return;
            bool a = false, b = false;
            foreach (var u in _units) if (u.Alive) { if (u.Side == 0) a = true; else b = true; }
            if (a && b) return;
            Winner = a ? 0 : b ? 1 : -1;
            EndTick = _tick;
            Emit("end", -1, -1, 0, Winner == 0 ? "ours" : "theirs", 0, 0, 3);
        }

        // ------------------------------------------------------------------ the brain

        /// One brain per fighter. It sees the field late by its Speed, picks whom to fight by utility (his kind of hit
        /// against their stopping stat, marks he can cash or lay for a partner, the wounded, threats to his back row),
        /// keeps his man unless something better is worth the switch, and chooses when to swing, when to wait for an
        /// opening and when to dodge.
        void Think(Unit u)
        {
            if (Over) { u.Moving = false; return; }
            // reactions come first, every tick: a dodge only exists while there is still time to make it
            if (u.Act == Act.Idle || u.Act == Act.Move) { if (TryDodge(u)) return; }
            if (u.Act != Act.Idle && u.Act != Act.Move) return;
            if ((_tick + u.Id) % 2 != 0) return;              // decisions every 100 ms

            // slot 3, the boost, fires on its own with a real target (15 Nov)
            if (_tick >= u.BoostReadyAt && TryBoost(u)) return;

            PickTarget(u);
            var t = Get(u.Target);
            if (t == null) { u.Moving = false; return; }

            if (Rules.Ranged(u.Cls)) RangedPlan(u, t); else MeleePlan(u, t);
        }

        void PickTarget(Unit u)
        {
            var cur = Get(u.Target);
            bool keep = cur != null && cur.Alive && _tick - u.TargetPickedAt < Rules.T(1500);
            if (keep && !Committed(cur)) { }
            Unit best = null; int bestScore = int.MinValue; string why = "";
            foreach (var e in _units)
            {
                if (!e.Alive || e.Side == u.Side) continue;
                int s = Score(u, e, out string w);
                if (e.Id == u.Target) s = s * 130 / 100;      // keep my man unless the other is clearly better
                if (s > bestScore) { bestScore = s; best = e; why = w; }
            }
            if (best == null) { u.Target = -1; return; }
            if (best.Id != u.Target)
            {
                if (cur != null && cur.Alive && _tick - u.TargetPickedAt < Rules.T(1200)) return; // no flip flop
                u.Target = best.Id; u.TargetPickedAt = _tick; u.Why = why;
                Emit("target", u.Id, best.Id, 0, why, 0, 0, 0);
            }
        }

        /// The want for one enemy, in thousandths. Every factor is a plain multiplier so the reason can be read.
        int Score(Unit u, Unit e, out string why)
        {
            long s = 1000; why = "nearest";
            int dist = Dist(u, e);
            // distance: melee by walking time (the slow Warden pays most for a far man), ranged by range
            if (Rules.Ranged(u.Cls))
            {
                int range = Moves.Attack(u.Tribe, u.Cls).Reach;
                s = s * (dist <= range ? 100 : Math.Max(20, 100 - (dist - range) / 6)) / 100;
            }
            else
            {
                int walkMs = Math.Max(0, dist - 100) * 1000 / Rules.SpeedPx(u.Six);
                s = s * 100 / (100 + walkMs * 45 / 1000);
            }
            // the Skirmisher dashes in (15 Nov): she hunts the soft men behind their front, the shooter first,
            // since shots are what catch the fast
            if (u.Cls == Cls.Skirmisher && Rules.Ranged(e.Cls)) { s = s * (e.Cls == Cls.Marksman ? 190 : 160) / 100; why = "dive their " + e.Cls; }
            // lanes: a melee man fights across his own lane first, so the pairs spread over the court instead of one ball
            if (!Rules.Ranged(u.Cls)) s = s * 100 / (100 + Math.Abs(e.Y - u.HomeY) / Rules.C / 3);
            // the chart: my kind of hit against his stopping stat
            int stop = Rules.StopPercent(StopStat(Rules.KindOf(u.Cls), e));
            if (u.Cls == Cls.Marksman && e.Cls == Cls.Warden) stop = stop * Rules.ShieldLeaves / 100;
            s = s * (60 + stop * 2 / 5) / 100;
            if (stop >= 150) why = "his weak spot";
            // marks: a casher wants a man he can cash; a layer wants a man his partner can cash
            if (CanCash(u, e)) { s = s * 300 / 100; why = "cash his " + CashName(u, e); }
            else if (LaysForPartner(u, e)) { s = s * 125 / 100; why = "set him up"; }
            // the wounded
            int missing = 1000 - e.Hp * 1000 / e.MaxHp;
            s = s * (1000 + missing) / 1000;
            if (e.Hp * 4 < e.MaxHp) why = "finish him";
            // threats to my back row (a melee man diving our shooter or our Hexer)
            if (!Rules.Ranged(u.Cls) && e.Act == Act.Attack && Get(e.MoveTarget) is Unit victim && victim.Side == u.Side && Rules.Ranged(victim.Cls))
            { s = s * 160 / 100; why = "save our " + victim.Cls; }
            // ranged men keep off whoever dives them
            if (Rules.Ranged(u.Cls) && e.Target == u.Id && !Rules.Ranged(e.Cls) && dist < 240) { s = s * 150 / 100; why = "shoot the diver"; }
            // the crowd: two of us already on him is enough, unless he dies soon
            int on = 0;
            foreach (var a in _units) if (a.Alive && a.Side == u.Side && a.Id != u.Id && a.Target == e.Id && !Rules.Ranged(a.Cls)) on++;
            if (!Rules.Ranged(u.Cls) && on >= 2 && e.Hp * 3 > e.MaxHp && !CanCash(u, e)) s = s * 45 / 100;
            if (!Rules.Ranged(u.Cls) && on == 1 && !CanCash(u, e)) s = s * 85 / 100;
            return (int)Math.Min(int.MaxValue, s);
        }

        void MeleePlan(Unit u, Unit t)
        {
            var move = Moves.Attack(u.Tribe, u.Cls);
            int startGap = move.Reach + (move.DashPx > 0 ? move.DashPx : 0);
            int gx, gy; Slot(u, t, startGap, out gx, out gy);
            bool inPlace = Math.Abs(u.Px - gx) <= 10 && Math.Abs(u.Py - gy) <= 10;
            bool ready = _tick >= u.AttackReadyAt && _tick >= u.StaggerUntil;
            if (ready && u.ReadySince < 0) u.ReadySince = _tick;
            if (!ready) u.ReadySince = -1;

            int gap = (t.X - u.X) * u.Face / Rules.C;          // how far he is in front of me
            int dy = Math.Abs(t.Y - u.Y) / Rules.C;
            // the wind up's tracking closes the last step, so he swings from his waiting spot and is seen to step in
            bool canStrike = gap >= startGap - Rules.HitWindowPx && gap <= startGap + 40 && dy <= 60;

            if (ready && canStrike)
            {
                bool open = Open(t);
                bool patient = u.ReadySince >= 0 && _tick - u.ReadySince < Rules.Patience(u.Cls);
                // a fighter waits for an opening while he can afford to; the bruiser never waits
                if (open || !patient || CanCash(u, t))
                {
                    Begin(u, move, t, open ? "he is open" : CanCash(u, t) ? "cash it" : "swing");
                    return;
                }
                u.Moving = false;   // hold, watching for the opening
                return;
            }
            // not ready: hold off a step outside his reach, so trades read as turns and nobody hugs. The Skirmisher
            // darts out further between strikes, unless her man is a shooter or a Hexer she is pinning down
            if (!ready)
            {
                int wait = startGap + 26;
                if (u.Cls == Cls.Skirmisher && !Rules.Ranged(t.Cls)) wait = startGap + 110;
                Slot(u, t, wait, out gx, out gy);
            }
            Go(u, gx, gy);
            if (inPlace) u.Moving = false;
        }

        void RangedPlan(Unit u, Unit t)
        {
            var move = Moves.Attack(u.Tribe, u.Cls);
            // a diver on me: back off if I can, else stand and shoot him
            Unit diver = null; int dd = int.MaxValue;
            foreach (var e in _units)
            {
                if (!e.Alive || e.Side == u.Side || Rules.Ranged(e.Cls)) continue;
                int d = Dist(u, e);
                if (d < 170 && d < dd) { dd = d; diver = e; }
            }
            if (diver != null)
            {
                int bx = u.Px - u.Face * 120, by = u.Py + (u.Py < 615 ? -20 : 20);
                if (InFloor(bx, by) && Math.Abs(bx - (u.Side == 0 ? Rules.FloorLeft : Rules.FloorRight)) > 40)
                { Go(u, bx, by); u.Why = "back off the diver"; return; }
            }
            int dist = Dist(u, t);
            bool ready = _tick >= u.AttackReadyAt && _tick >= u.StaggerUntil;
            if (dist <= move.Reach && ready && Front(u, t))
            {
                Begin(u, move, t, "in range");
                return;
            }
            // keep my distance: inside range and behind the nearest enemy by a safe gap, wherever the front has moved,
            // in my own lane, leaning at most 60 px toward his
            int want = move.Reach - 90;
            int homeY = u.HomeY / Rules.C;
            int gx = t.Px - u.Face * want, gy = homeY + Math.Sign(t.Py - homeY) * Math.Min(60, Math.Abs(t.Py - homeY));
            var near = Nearest(u, enemy: true);
            if (near != null) gx = u.Side == 0 ? Math.Min(gx, near.Px - 220) : Math.Max(gx, near.Px + 220);
            gx = Math.Max(Rules.FloorLeft + 20, Math.Min(Rules.FloorRight - 20, gx));
            if (Math.Abs(gx - u.Px) < 24 && Math.Abs(gy - u.Py) < 16) { u.Moving = false; return; }
            Go(u, gx, gy);
        }

        /// A melee man's place in front of his target: at the given gap, in the first lane slot no ally holds.
        void Slot(Unit u, Unit t, int gap, out int gx, out int gy)
        {
            int[] offs = { 0, -44, 44, -80, 80 };
            gx = t.Px - u.Face * gap; gy = t.Py;
            foreach (int o in offs)
            {
                int x = t.Px - u.Face * gap, y = t.Py + o;
                bool free = true;
                foreach (var a in _units)
                {
                    if (a == u || !a.Alive || a.Side != u.Side) continue;
                    int ax = a.Moving ? a.GoalX / Rules.C : a.Px, ay = a.Moving ? a.GoalY / Rules.C : a.Py;
                    if (Math.Abs(ax - x) < 70 && Math.Abs(ay - y) < 40) { free = false; break; }
                }
                if (free && InFloor(x, y)) { gx = x; gy = y; return; }
            }
        }

        bool TryDodge(Unit u)
        {
            if (_tick < u.DodgeReadyAt || _tick < u.HeldUntil || _tick < u.PinnedUntil || _tick < u.StaggerUntil) return false;
            if (Rules.DodgeWill(u.Six) <= 0) return false;
            int react = Rules.ReactTicks(u.Six);
            // a blade swinging at me
            foreach (var a in _units)
            {
                if (!a.Alive || a.Side == u.Side || a.Act != Act.Attack || a.MoveTarget != u.Id || a.Move == null) continue;
                if (a.NextHit >= a.Move.Hits.Length) continue;
                var h = a.Move.Hits[a.NextHit];
                if (h.Kind == Kind.Shot || (h.Fx & Fx.Projectile) != 0) continue;   // shots can't be dodged
                if (a.Age < react) continue;                                         // not seen yet
                int left = h.Step - a.Age;
                if (left < 2 || left > 12) continue;
                if (!WouldLand(a, u)) continue;
                int will = Rules.DodgeWill(u.Six) + ((h.Fx & Fx.Class) != 0 ? 15 : 0);
                if (u.Rng.Next(100) < will) { StartDodge(u, a, "read the swing"); return true; }
                u.DodgeReadyAt = _tick + Rules.T(400);  // missed the read; can't try again at once
                return false;
            }
            // a curse flying at me: spells travel, so Speed dodges them (15 Nov)
            foreach (var p in _shots)
            {
                if (p.Done || p.Target != u.Id) continue;
                if (_tick - p.Launched < react) continue;
                int left = p.Arrives - _tick;
                if (left < 2) continue;
                int will = Rules.DodgeWill(u.Six) + 20;
                if (u.Rng.Next(100) < will) { StartDodge(u, Get(p.Owner), "step out of the curse"); return true; }
                u.DodgeReadyAt = _tick + Rules.T(400);
                return false;
            }
            return false;
        }

        bool TryBoost(Unit u)
        {
            switch (u.Cls)
            {
                case Cls.Warden:
                {
                    // Shield Pin: the man in front of him, pinned 2 s, can't dodge
                    Unit best = null;
                    foreach (var e in _units)
                    {
                        if (!e.Alive || e.Side == u.Side || _tick < e.PinnedUntil) continue;
                        int gap = (e.X - u.X) * u.Face / Rules.C, dy = Math.Abs(e.Y - u.Y) / Rules.C;
                        if (gap > 40 && gap < 135 && dy < Rules.LaneWindowPx && (best == null || e.Six.Speed > best.Six.Speed)) best = e;
                    }
                    if (best == null || _tick < u.AttackReadyAt - Rules.T(200)) return false;
                    Begin(u, Moves.Boost(u.Tribe, u.Cls), best, "pin the fast one", Act.Boost);
                    u.BoostReadyAt = _tick + Rules.BoostCooldown;
                    return true;
                }
                case Cls.Skirmisher:
                {
                    // Twin Cut: while her strike is not ready, two quick cuts that leave a bleed
                    if (_tick >= u.AttackReadyAt) return false;
                    var t = Get(u.Target);
                    if (t == null || !t.Alive) return false;
                    int gap = (t.X - u.X) * u.Face / Rules.C, dy = Math.Abs(t.Y - u.Y) / Rules.C;
                    if (gap < 60 || gap > 120 || dy > Rules.LaneWindowPx) return false;
                    Begin(u, Moves.Boost(u.Tribe, u.Cls), t, "twin cut", Act.Boost);
                    u.BoostReadyAt = _tick + Rules.BoostCooldown;
                    return true;
                }
                case Cls.Hexer:
                {
                    // Bad Blood: a bleeding man's bleed jumps to the men next to him
                    Unit best = null; int most = 0;
                    foreach (var e in _units)
                    {
                        if (!e.Alive || e.Side == u.Side || _tick >= e.BleedUntil) continue;
                        int n = 0;
                        foreach (var o in _units) if (o != e && o.Alive && o.Side == e.Side && _tick >= o.BleedUntil && Near(o, e, 110)) n++;
                        if (n > most) { most = n; best = e; }
                    }
                    if (best == null) return false;
                    Begin(u, Moves.Boost(u.Tribe, u.Cls), best, "spread his bleed", Act.Boost);
                    u.BoostReadyAt = _tick + Rules.BoostCooldown;
                    return true;
                }
                case Cls.Marksman:
                    // Steady: the shot he starts aiming goes through to the enemy behind
                    if (u.PierceNext) return false;
                    u.PierceNext = true; u.BoostReadyAt = _tick + Rules.BoostCooldown;
                    Emit("boost", u.Id, -1, 0, "steady", u.Px, u.Py - 90, 0);
                    return false;
                default:
                    return false; // the Trapper's Rearm fires when her snare springs
            }
        }

        // ------------------------------------------------------------------ acts

        void Begin(Unit u, MoveSpec move, Unit t, string why, Act act = Act.Attack)
        {
            u.Act = act; u.Age = 0; u.Move = move; u.NextHit = 0; u.MoveTarget = t.Id; u.Moving = false; u.ReadySince = -1; u.Tracked = 0;
            u.Why = why;
            Emit(act == Act.Attack ? "swing" : "boostmove", u.Id, t.Id, 0, move.Name + ":" + why, u.Px, u.Py, 0);
        }

        void StartDodge(Unit u, Unit from, string why)
        {
            int away = from != null ? -Math.Sign(from.X - u.X) : -u.Face;
            if (away == 0) away = -u.Face;
            int dist = Rules.DodgePx(u.Six);
            int tx = u.Px + away * dist, ty = u.Py + (u.Rng.Next(2) == 0 ? -18 : 18);
            tx = Math.Max(Rules.FloorLeft, Math.Min(Rules.FloorRight, tx));
            ty = Math.Max(Rules.FloorTop, Math.Min(Rules.FloorBottom, ty));
            u.Act = Act.Dodge; u.Age = 0; u.Moving = false;
            u.DodgeFromX = u.X; u.DodgeFromY = u.Y; u.DodgeToX = tx * Rules.C; u.DodgeToY = ty * Rules.C; u.DodgeTicks = Rules.T(300);
            u.DodgeReadyAt = _tick + Rules.DodgeCooldown(u.Six);
            u.LastDodgeAt = _tick;
            Emit("dodge", u.Id, from != null ? from.Id : -1, 0, why, u.Px, u.Py, 0);
        }

        void Advance(Unit u)
        {
            switch (u.Act)
            {
                case Act.Dead: u.Age++; return;
                case Act.Idle:
                case Act.Move:
                    if (u.Moving) Walk(u); else u.Act = Act.Idle;
                    u.Age++;
                    return;
                case Act.Dodge:
                {
                    u.Age++;
                    int n = Math.Min(u.Age, u.DodgeTicks);
                    // ease out: most of the distance in the first half
                    int k = 1000 - (u.DodgeTicks - n) * (u.DodgeTicks - n) * 1000 / (u.DodgeTicks * u.DodgeTicks);
                    u.X = u.DodgeFromX + (u.DodgeToX - u.DodgeFromX) * k / 1000;
                    u.Y = u.DodgeFromY + (u.DodgeToY - u.DodgeFromY) * k / 1000;
                    if (u.Age >= u.DodgeTicks + Rules.T(150)) { u.Act = Act.Idle; u.Age = 0; }
                    return;
                }
                case Act.Hurt:
                    u.Age++;
                    if (u.Age >= Rules.T(450)) { u.Act = Act.Idle; u.Age = 0; }
                    return;
                case Act.Stagger:
                    u.Age++;
                    if (u.Age >= u.StaggerTicks) { u.Act = Act.Idle; u.Age = 0; }
                    return;
                case Act.Attack:
                case Act.Boost:
                {
                    var m = u.Move;
                    var tgt = Get(u.MoveTarget);
                    // a melee man whose target died before the first blow lowers his weapon
                    if (u.NextHit == 0 && !Rules.Ranged(u.Cls) && m.Hits.Length > 0 && (m.Hits[0].Fx & (Fx.Projectile | Fx.Spread)) == 0 && (tgt == null || !tgt.Alive))
                    {
                        u.Act = Act.Idle; u.Age = 0; u.Move = null; u.MoveTarget = -1;
                        if (u.AttackReadyAt < _tick) u.AttackReadyAt = _tick + Rules.T(200);
                        return;
                    }
                    int first = m.Hits.Length > 0 ? m.Hits[0].Step : 0;
                    bool meleeMove = !Rules.Ranged(u.Cls) && m.Reach < 200;
                    // root motion: the Skirmisher dashes in, steering the dash to land at her reach
                    if (m.DashPx > 0 && u.Age >= m.DashFrom && u.Age < m.DashTo)
                    {
                        int left = m.DashTo - u.Age;
                        int per = m.DashPx * Rules.C / Math.Max(1, m.DashTo - m.DashFrom);
                        if (tgt != null && tgt.Alive && tgt.Act != Act.Dodge)
                        {
                            int want = tgt.X - u.Face * m.Reach * Rules.C;
                            int need = (want - u.X) * u.Face / Math.Max(1, left);
                            per = Math.Max(per / 2, Math.Min(per * 3 / 2, need));
                            u.Y += Math.Sign(tgt.Y - u.Y) * Math.Min(Math.Abs(tgt.Y - u.Y), Rules.TrackPx * Rules.C);
                        }
                        u.X += u.Face * per;
                    }
                    // tracking: through the wind up a melee man keeps his reach on a man who shifts, never one who
                    // dodges; once the first blow lands he is committed (Arkham's freeflow magnetism, brawlers' tracking)
                    else if (meleeMove && u.Age < first && tgt != null && tgt.Alive && tgt.Act != Act.Dodge && u.Tracked < Rules.TrackMaxPx)
                    {
                        int wantX = tgt.X - u.Face * m.Reach * Rules.C, wantY = tgt.Y;
                        int sx = Math.Sign(wantX - u.X) * Math.Min(Math.Abs(wantX - u.X), Rules.TrackPx * Rules.C);
                        int sy = Math.Sign(wantY - u.Y) * Math.Min(Math.Abs(wantY - u.Y), Rules.TrackPx * Rules.C);
                        u.X += sx; u.Y += sy; u.Tracked += (Math.Abs(sx) + Math.Abs(sy)) / Rules.C;
                    }
                    while (u.NextHit < m.Hits.Length && m.Hits[u.NextHit].Step == u.Age)
                    {
                        Land(u, m.Hits[u.NextHit]);
                        u.NextHit++;
                        if (u.Act != Act.Attack && u.Act != Act.Boost) return;   // he was staggered by a reflex
                    }
                    u.Age++;
                    if (u.Age >= m.Ticks)
                    {
                        if (u.Act == Act.Attack) u.AttackReadyAt = _tick + Rules.RestTicks(u.Cls);
                        u.Act = Act.Idle; u.Age = 0; u.Move = null; u.MoveTarget = -1;
                    }
                    return;
                }
            }
        }

        void Walk(Unit u)
        {
            if (_tick < u.HeldUntil) { u.Act = Act.Idle; return; }
            int dx = u.GoalX - u.X, dy = u.GoalY - u.Y;
            long d2 = (long)dx * dx + (long)dy * dy;
            int step = Rules.SpeedPx(u.Six) * Rules.C * Rules.TickMs / 1000;
            if (d2 <= (long)step * step) { u.X = u.GoalX; u.Y = u.GoalY; u.Moving = false; u.Act = Act.Idle; return; }
            int d = (int)Math.Sqrt(d2);
            u.X += (int)((long)dx * step / d); u.Y += (int)((long)dy * step / d);
            if (u.Act != Act.Move) { u.Act = Act.Move; u.Age = 0; }
        }

        void Go(Unit u, int px, int py)
        {
            px = Math.Max(Rules.FloorLeft, Math.Min(Rules.FloorRight, px));
            py = Math.Max(Rules.FloorTop, Math.Min(Rules.FloorBottom, py));
            u.GoalX = px * Rules.C; u.GoalY = py * Rules.C;
            u.Moving = Math.Abs(u.GoalX - u.X) > 4 * Rules.C || Math.Abs(u.GoalY - u.Y) > 4 * Rules.C;
        }

        /// Bodies never overlap: any two feet closer than BodyPx (depth counts double) push apart, the one standing
        /// still or attacking moving least.
        void Separate()
        {
            for (int i = 0; i < _units.Count; i++)
                for (int j = i + 1; j < _units.Count; j++)
                {
                    var a = _units[i]; var b = _units[j];
                    if (!a.Alive || !b.Alive) continue;
                    int dx = (b.X - a.X) / Rules.C, dy = (b.Y - a.Y) / Rules.C * 2;
                    int min = Rules.BodyPx;
                    int d2 = dx * dx + dy * dy;
                    if (d2 >= min * min) continue;
                    int d = Math.Max(1, (int)Math.Sqrt(d2));
                    int push = (min - d) * Rules.C;
                    int ux = dx == 0 && dy == 0 ? (a.Side == 0 ? -1 : 1) * 1000 : dx * 1000 / d, uy = dy * 1000 / d / 2;
                    bool aFixed = a.Act == Act.Attack || a.Act == Act.Boost || _tick < a.HeldUntil;
                    bool bFixed = b.Act == Act.Attack || b.Act == Act.Boost || _tick < b.HeldUntil;
                    int pa = aFixed && !bFixed ? 0 : bFixed && !aFixed ? 1000 : 500;
                    a.X -= ux * push / 1000 * pa / 1000; a.Y -= uy * push / 1000 * pa / 1000;
                    b.X += ux * push / 1000 * (1000 - pa) / 1000; b.Y += uy * push / 1000 * (1000 - pa) / 1000;
                    Clamp(a); Clamp(b);
                }
        }

        void Clamp(Unit u)
        {
            u.X = Math.Max(Rules.FloorLeft * Rules.C, Math.Min(Rules.FloorRight * Rules.C, u.X));
            u.Y = Math.Max(Rules.FloorTop * Rules.C, Math.Min(Rules.FloorBottom * Rules.C, u.Y));
        }

        // ------------------------------------------------------------------ blows

        bool WouldLand(Unit a, Unit t)
        {
            var m = a.Move;
            int gap = (t.X - a.X) * a.Face / Rules.C;
            // the dash still to come brings him closer
            if (m.DashPx > 0 && a.Age < m.DashTo) gap -= m.DashPx * (m.DashTo - Math.Max(a.Age, m.DashFrom)) / Math.Max(1, m.DashTo - m.DashFrom);
            int dy = Math.Abs(t.Y - a.Y) / Rules.C;
            return gap >= m.Reach - Rules.HitWindowPx - 16 && gap <= m.Reach + Rules.HitWindowPx && dy <= Rules.LaneWindowPx;
        }

        void Land(Unit a, HitSpec h)
        {
            var t = Get(a.MoveTarget);
            var m = a.Move;
            if ((h.Fx & Fx.Projectile) != 0)
            {
                if (t == null || !t.Alive) { t = Nearest(a, enemy: true); if (t == null) return; a.MoveTarget = t.Id; }
                Launch(a, t, h);
                return;
            }
            if ((h.Fx & Fx.Spread) != 0) { BadBlood(a, t); return; }
            if ((t == null || !t.Alive) && h.Kind != Kind.Shot)
            {
                Emit("whiff", a.Id, -1, 0, "nobody there", a.Px + a.Face * m.Reach, a.Py - 40, 0, h.Kind.ToString());
                return;
            }
            if ((h.Fx & Fx.Pin) != 0)
            {
                if (WouldLand(a, t))
                {
                    t.PinnedUntil = _tick + Rules.PinTicks;
                    Emit("pin", a.Id, t.Id, 0, "pinned", t.Px, t.Py - 60, 1);
                }
                else Emit("whiff", a.Id, t.Id, 0, "pin missed", a.Px + a.Face * 90, a.Py - 40, 0, "Blade");
                return;
            }
            if (h.Kind == Kind.Shot && (t == null || !t.Alive))
            {
                // a shooter whose man fell turns the rest of his string on the nearest one in range
                var next = Nearest(a, enemy: true);
                if (next == null || Dist(a, next) > m.Reach || !Front(a, next)) { Emit("whiff", a.Id, -1, 0, "nobody there", a.Px + a.Face * 200, a.Py - 50, 0, "Shot"); return; }
                a.MoveTarget = next.Id; t = next;
                Emit("retarget", a.Id, t.Id, 0, "next man", t.Px, t.Py, 0);
            }
            if (h.Kind == Kind.Shot)
            {
                if (Dist(a, t) > m.Reach + 40) { Emit("whiff", a.Id, t.Id, 0, "out of range", t.Px, t.Py - 50, 0, "Shot"); return; }
                Strike(a, t, h, a.Six.Attack, h.Share, true);
                if ((h.Fx & Fx.Class) != 0 && a.PierceNext)
                {
                    a.PierceNext = false;
                    var behind = Behind(a, t);
                    if (behind != null) Strike(a, behind, h, a.Six.Attack, h.Share * Rules.PiercePercent / 100, true, pierce: true);
                }
                return;
            }
            // a blade or a hold: it lands only if he is still where the blow goes
            if (!WouldLand(a, t))
            {
                Emit("miss", a.Id, t.Id, 0, t.Act == Act.Dodge ? "dodged" : "out of reach", t.Px, t.Py - 60, (h.Fx & Fx.Class) != 0 ? 1 : 0, h.Kind.ToString());
                // a bash that misses leaves him wide open (14 Nov)
                if ((h.Fx & Fx.Bash) != 0) { a.OpeningUntil = _tick + Rules.OpeningTicks; Emit("mark", a.Id, a.Id, 0, "opening", a.Px, a.Py - 100, 0); }
                return;
            }
            Strike(a, t, h, a.Six.Attack, h.Share, false);
        }

        /// One blow reaching one man: defences first, then the stop, the cash, the damage and what it leaves.
        void Strike(Unit a, Unit t, HitSpec h, int stat, int share, bool shot, bool pierce = false)
        {
            bool cls = (h.Fx & Fx.Class) != 0;
            bool melee = !shot && h.Kind != Kind.Spell;
            int cx = (a.Px + t.Px) / 2 + t.Face * 0, cy = t.Py - 58;
            if (melee) cx = t.Px - a.Face * 18;

            // slot 2, the defence, fires when he is hit (15 Nov), on its cooldown
            if (_tick >= t.DefenceReadyAt && t.Alive)
            {
                if (t.Cls == Cls.Marksman && melee && _tick >= t.HeldUntil && _tick >= t.PinnedUntil)
                {
                    t.DefenceReadyAt = _tick + Rules.DefenceCooldown;
                    Emit("defence", t.Id, a.Id, 0, "step back", t.Px, t.Py - 90, 0);
                    Emit("miss", a.Id, t.Id, 0, "step back", cx, cy, cls ? 1 : 0, h.Kind.ToString());
                    StartDodge(t, a, "step back");
                    return;
                }
                if (t.Cls == Cls.Skirmisher && melee && cls && _tick >= t.HeldUntil && _tick >= t.PinnedUntil)
                {
                    t.DefenceReadyAt = _tick + Rules.DefenceCooldown;
                    Emit("defence", t.Id, a.Id, 0, "sidestep", t.Px, t.Py - 90, 0);
                    Emit("miss", a.Id, t.Id, 0, "sidestep", cx, cy, 1, h.Kind.ToString());
                    StartDodge(t, a, "sidestep");
                    return;
                }
            }

            int stop = StopStat(h.Kind, t);
            int pct = Rules.StopPercent(stop);
            int dmg = stat * share / 100 * Rules.PowerScale / 100;
            dmg = dmg * pct / 100;
            bool blocked = false;
            if (shot && t.Cls == Cls.Warden) { dmg = dmg * Rules.ShieldLeaves / 100; blocked = true; }

            // the cash: double, spends the mark, pops its word (1 Nov, 15 Nov)
            string cash = cls && !pierce ? Cash(a, t) : null;
            if (cash != null) dmg = dmg * Rules.CashPercent / 100;

            // Brace: a heavy swing reaching the Warden lands lighter, and the swinger is left open
            bool braced = false;
            if (t.Cls == Cls.Warden && melee && cls && _tick >= t.DefenceReadyAt)
            {
                dmg = dmg * Rules.BracePercent / 100; braced = true;
                t.DefenceReadyAt = _tick + Rules.DefenceCooldown;
            }
            dmg = Math.Max(1, dmg);
            int tier = cash != null ? 2 : cls || (h.Fx & Fx.Finisher) != 0 ? 1 : 0;
            if (blocked) tier = 0;
            Damage(a, t, dmg, h.Kind.ToString(), blocked ? "blocked" : cash ?? (pierce ? "pierce" : cls ? "class" : "hit"), cx, cy, tier);
            if (braced)
            {
                Emit("defence", t.Id, a.Id, 0, "brace", t.Px, t.Py - 100, 1);
                a.OpeningUntil = _tick + Rules.OpeningTicks;
                Emit("mark", t.Id, a.Id, 0, "opening", a.Px, a.Py - 100, 0);
            }
            if (!t.Alive) return;

            // what the blow leaves
            if ((h.Fx & Fx.Bash) != 0)
            {
                Stagger(t, a);
                t.OpeningUntil = _tick + Rules.OpeningTicks;
                Emit("mark", a.Id, t.Id, 0, "opening", t.Px, t.Py - 100, 0);
                Knock(t, a, 30);
            }
            else if ((h.Fx & Fx.Finisher) != 0) Knock(t, a, 14);
            if (a.Cls == Cls.Marksman && cls && !pierce)
            {
                t.OpeningUntil = _tick + Rules.OpeningTicks;
                Emit("mark", a.Id, t.Id, 0, "opening", t.Px, t.Py - 100, 0);
            }
            if ((h.Fx & Fx.Snare) != 0)
            {
                if (_tick >= a.SnareReadyAt)
                {
                    a.SnareReadyAt = _tick + Rules.SnareCooldown;
                    t.HeldUntil = _tick + Rules.HoldTicks; t.SnareMarkUntil = _tick + Rules.SnareMarkTicks;
                    t.BleedUntil = _tick + Rules.BleedTicks; t.BleedNext = _tick + Rules.BleedEvery;
                    if (t.Act == Act.Dodge || t.Act == Act.Move) { t.Act = Act.Idle; t.Age = 0; t.Moving = false; }
                    Emit("mark", a.Id, t.Id, 0, "snare", t.Px, t.Py, 1);
                    Emit("mark", a.Id, t.Id, 0, "bleed", t.Px, t.Py - 100, 0);
                    // Rearm: her trap is armed again at once (rules 12, kept)
                    if (_tick >= a.RearmReadyAt) { a.SnareReadyAt = _tick; a.RearmReadyAt = _tick + Rules.BoostCooldown; Emit("boost", a.Id, -1, 0, "rearm", a.Px, a.Py - 100, 0); }
                }
            }
            if ((h.Fx & Fx.Bleed) != 0)
            {
                bool fresh = _tick >= t.BleedUntil;
                t.BleedUntil = _tick + Rules.BleedTicks; t.BleedNext = _tick + Rules.BleedEvery;
                if (fresh) Emit("mark", a.Id, t.Id, 0, "bleed", t.Px, t.Py - 100, 0);
            }
            if (melee && !t.Alive) return;
            // slot 2 reflexes that answer a melee blow that landed
            if (melee && _tick >= t.DefenceReadyAt)
            {
                if (t.Cls == Cls.Trapper)
                {
                    t.DefenceReadyAt = _tick + Rules.DefenceCooldown;
                    a.HeldUntil = _tick + Rules.HoldTicks; a.SnareMarkUntil = _tick + Rules.SnareMarkTicks;
                    Emit("defence", t.Id, a.Id, 0, "snap trap", a.Px, a.Py, 1);
                    Emit("mark", t.Id, a.Id, 0, "snare", a.Px, a.Py, 1);
                }
                else if (t.Cls == Cls.Hexer)
                {
                    t.DefenceReadyAt = _tick + Rules.DefenceCooldown;
                    a.HexUntil = _tick + Rules.HexTicks;
                    Emit("defence", t.Id, a.Id, 0, "evil eye", t.Px, t.Py - 100, 1);
                    Emit("mark", t.Id, a.Id, 0, "hex", a.Px, a.Py - 100, 0);
                }
            }
            // a big blow on a man standing or walking makes him flinch; chips and a man mid swing only flash,
            // so a string of light hits never pins anyone in place
            if (tier >= 1 && (t.Act == Act.Idle || t.Act == Act.Move)) { t.Act = Act.Hurt; t.Age = 0; t.Moving = false; }
        }

        string Cash(Unit a, Unit t)
        {
            switch (a.Cls)
            {
                case Cls.Warden: if (_tick < t.HexUntil) { t.HexUntil = _tick; return "hexed"; } break;
                case Cls.Marksman: if (_tick < t.SnareMarkUntil) { t.SnareMarkUntil = _tick; return "pinned"; } break;
                case Cls.Skirmisher:
                    if (_tick < t.OpeningUntil) { t.OpeningUntil = _tick; return "opened"; }
                    if (_tick < t.BleedUntil) { t.BleedUntil = _tick; return "ripped"; }
                    break;
            }
            return null;
        }

        void Damage(Unit a, Unit t, int dmg, string kind, string detail, int x, int y, int tier)
        {
            t.Hp = Math.Max(0, t.Hp - dmg);
            t.LastHitBy = a != null ? a.Id : -1; t.LastHitAt = _tick;
            Emit("hit", a != null ? a.Id : -1, t.Id, dmg, detail, x, y, tier, kind);
            if (t.Hp == 0) Die(t, a);
        }

        void Die(Unit t, Unit by)
        {
            t.Act = Act.Dead; t.Age = 0; t.Moving = false; t.Move = null;
            Emit("kill", by != null ? by.Id : -1, t.Id, 0, t.Cls.ToString(), t.Px, t.Py - 60, 3);
        }

        void Stagger(Unit t, Unit by)
        {
            if (!t.Alive) return;
            // he loses his next swing (14 Nov): a swing in progress is broken, the next one waits
            if (t.Act == Act.Attack || t.Act == Act.Boost) Emit("broken", by.Id, t.Id, 0, t.Move != null ? t.Move.Name : "", t.Px, t.Py - 80, 1);
            t.Act = Act.Stagger; t.Age = 0; t.Move = null; t.MoveTarget = -1; t.Moving = false;
            t.StaggerTicks = Rules.StaggerTicks; t.StaggerUntil = _tick + Rules.StaggerTicks;
            t.AttackReadyAt = Math.Max(t.AttackReadyAt, _tick + Rules.StaggerTicks + Rules.RestTicks(t.Cls));
        }

        /// Knockback is the screen's (fight-feel: a few pixels that settle back). In the sim it drifted whole pairs across
        /// the court, bash by bash, until the back row could not reach them, so the sim keeps everyone where he stands.
        void Knock(Unit t, Unit a, int px) { }

        void BadBlood(Unit a, Unit t)
        {
            if (t == null || !t.Alive || _tick >= t.BleedUntil) return;
            Emit("spread", a.Id, t.Id, 0, "bad blood", t.Px, t.Py - 70, 1);
            foreach (var o in _units)
            {
                if (o == t || !o.Alive || o.Side != t.Side || !Near(o, t, 110)) continue;
                bool fresh = _tick >= o.BleedUntil;
                o.BleedUntil = _tick + Rules.BleedTicks; o.BleedNext = _tick + Rules.BleedEvery;
                if (fresh) Emit("mark", a.Id, o.Id, 0, "bleed", o.Px, o.Py - 100, 0, "from:" + t.Id);
            }
        }

        void Bleed(Unit u)
        {
            if (!u.Alive || _tick >= u.BleedUntil || _tick < u.BleedNext) return;
            u.BleedNext = _tick + Rules.BleedEvery;
            int dmg = Math.Max(1, u.MaxHp * Rules.BleedPermille / 1000);
            u.Hp = Math.Max(0, u.Hp - dmg);
            Emit("bleedtick", -1, u.Id, dmg, "bleed", u.Px, u.Py - 50, 0);
            if (u.Hp == 0) Die(u, null);
        }

        // ------------------------------------------------------------------ the curse in flight

        void Launch(Unit a, Unit t, HitSpec h)
        {
            int fx = a.Px + a.Face * 34, fy = a.Py - 78;
            int tx = t.Px, ty = t.Py - 40;
            int dist = (int)Math.Sqrt((double)(tx - fx) * (tx - fx) + (double)(ty - fy) * (ty - fy));
            int ticks = Math.Max(2, dist * 1000 / Rules.ProjectilePxPerSecond / Rules.TickMs);
            var p = new Projectile { Id = _nextShot++, Owner = a.Id, Target = t.Id, FromX = fx, FromY = fy, ToX = tx, ToY = ty, Launched = _tick, Arrives = _tick + ticks, Power = a.Six.Spirit, Share = h.Share, Fx = h.Fx };
            _shots.Add(p);
            Emit("launch", a.Id, t.Id, ticks, "curse", fx, fy, 1, "Spell");
        }

        void Projectiles()
        {
            foreach (var p in _shots)
            {
                if (p.Done || _tick < p.Arrives) continue;
                p.Done = true;
                var a = Get(p.Owner);
                Emit("burst", p.Owner, p.Target, 0, "curse", p.ToX, p.ToY, 1, "Spell");
                var main = Get(p.Target);
                var hit = new HitSpec(0, Kind.Spell, p.Share, Fx.Class);
                bool any = false;
                foreach (var u in _units)
                {
                    if (!u.Alive || a == null || u.Side == a.Side) continue;
                    int dx = u.Px - p.ToX, dy = (u.Py - 40 - p.ToY) * 2;
                    int d = (int)Math.Sqrt((double)dx * dx + (double)dy * dy);
                    bool isMain = main != null && u.Id == main.Id;
                    if (isMain && d <= Rules.CursePx)
                    {
                        Spell(a, u, hit, p.Share, true); any = true;
                    }
                    else if (d <= Rules.SplashPx)
                    {
                        Spell(a, u, hit, p.Share * Rules.SplashPercent / 100, false); any = true;
                    }
                }
                if (!any) Emit("miss", p.Owner, p.Target, 0, main != null && main.Act == Act.Dodge ? "dodged" : "missed", p.ToX, p.ToY, 1, "Spell");
            }
        }

        void Spell(Unit a, Unit t, HitSpec h, int share, bool main)
        {
            int pct = Rules.StopPercent(t.Six.Will);
            int dmg = Math.Max(1, a.Six.Spirit * share / 100 * Rules.PowerScale / 100 * pct / 100);
            Damage(a, t, dmg, "Spell", main ? "class" : "splash", t.Px, t.Py - 58, main ? 1 : 0);
            if (!t.Alive) return;
            t.HexUntil = _tick + Rules.HexTicks;
            Emit("mark", a.Id, t.Id, 0, "hex", t.Px, t.Py - 100, 0);
            if (main && (t.Act == Act.Idle || t.Act == Act.Move)) { t.Act = Act.Hurt; t.Age = 0; t.Moving = false; }
        }

        // ------------------------------------------------------------------ helpers

        Unit Get(int id) => id >= 0 && id < _units.Count ? _units[id] : null;
        static int Dist(Unit a, Unit b) { int dx = (a.X - b.X) / Rules.C, dy = (a.Y - b.Y) / Rules.C; return (int)Math.Sqrt((double)dx * dx + (double)dy * dy); }
        static bool Near(Unit a, Unit b, int px) => Dist(a, b) <= px;
        static bool InFloor(int x, int y) => x >= Rules.FloorLeft && x <= Rules.FloorRight && y >= Rules.FloorTop && y <= Rules.FloorBottom;
        bool Front(Unit u, Unit t) => (t.X - u.X) * u.Face > 0;
        bool Committed(Unit u) => u.Act == Act.Attack || u.Act == Act.Boost;

        /// Open: busy losing (staggered, held, pinned, flinching) or deep in his own swing's recovery.
        bool Open(Unit t)
        {
            if (t.Act == Act.Stagger || t.Act == Act.Hurt || _tick < t.HeldUntil || _tick < t.PinnedUntil) return true;
            if (t.Act == Act.Attack && t.Move != null && t.NextHit >= t.Move.Hits.Length) return true;
            return false;
        }

        int StopStat(Kind k, Unit t)
        {
            switch (k)
            {
                case Kind.Spell: return t.Six.Will;
                case Kind.Hold: return t.Six.Defence;
                default: return t.Six.Defence;
            }
        }

        bool CanCash(Unit u, Unit e)
        {
            switch (u.Cls)
            {
                case Cls.Warden: return _tick < e.HexUntil;
                case Cls.Marksman: return _tick < e.SnareMarkUntil;
                case Cls.Skirmisher: return _tick < e.OpeningUntil || _tick < e.BleedUntil;
                default: return false;
            }
        }

        string CashName(Unit u, Unit e) => u.Cls == Cls.Warden ? "hex" : u.Cls == Cls.Marksman ? "snare" : _tick < e.OpeningUntil ? "opening" : "bleed";

        /// A layer's want: he lays what a living partner of his can cash, on a man who has none yet.
        bool LaysForPartner(Unit u, Unit e)
        {
            bool HasAlly(Cls c) { foreach (var a in _units) if (a.Alive && a.Side == u.Side && a.Cls == c) return true; return false; }
            switch (u.Cls)
            {
                case Cls.Hexer: return _tick >= e.HexUntil && HasAlly(Cls.Warden);
                case Cls.Trapper: return _tick >= e.SnareMarkUntil && _tick >= u.SnareReadyAt && (HasAlly(Cls.Marksman) || HasAlly(Cls.Skirmisher));
                case Cls.Warden: return _tick >= e.OpeningUntil && HasAlly(Cls.Skirmisher);
                case Cls.Marksman: return _tick >= e.OpeningUntil && HasAlly(Cls.Skirmisher);
                default: return false;
            }
        }

        Unit Nearest(Unit u, bool enemy)
        {
            Unit best = null; int bd = int.MaxValue;
            foreach (var e in _units) { if (!e.Alive || (e.Side != u.Side) != enemy || e == u) continue; int d = Dist(u, e); if (d < bd) { bd = d; best = e; } }
            return best;
        }

        Unit Behind(Unit a, Unit t)
        {
            Unit best = null; int bd = int.MaxValue;
            foreach (var e in _units)
            {
                if (!e.Alive || e.Side == a.Side || e == t) continue;
                int along = (e.X - t.X) * a.Face / Rules.C, dy = Math.Abs(e.Y - t.Y) / Rules.C;
                if (along > 10 && along < 280 && dy < 70 && along < bd) { bd = along; best = e; }
            }
            return best;
        }

        void Emit(string k, int a, int b, int amount, string detail, int x, int y, int tier, string kind = "")
        {
            _events.Add(new Ev { T = _tick, K = k, A = a, B = b, Amount = amount, Detail = detail ?? "", X = x, Y = y, Tier = tier, Kind = kind });
        }

        // ------------------------------------------------------------------ the trace the screen reads

        void Record()
        {
            _trace.Append(_tick == 0 ? "[" : ",[");
            for (int i = 0; i < _units.Count; i++)
            {
                var u = _units[i];
                int marks = (_tick < u.OpeningUntil ? 1 : 0) | (_tick < u.HexUntil ? 2 : 0) | (_tick < u.SnareMarkUntil ? 4 : 0) | (_tick < u.BleedUntil ? 8 : 0)
                          | (_tick < u.HeldUntil ? 16 : 0) | (_tick < u.PinnedUntil ? 32 : 0);
                int frame = (u.Act == Act.Attack || u.Act == Act.Boost) && u.Move != null ? u.Move.Frames[Math.Min(u.Age, u.Move.Frames.Length - 1)] : u.Age;
                string move = u.Move != null ? u.Move.Name : "";
                if (i > 0) _trace.Append(',');
                _trace.Append('[').Append(u.X).Append(',').Append(u.Y).Append(",\"").Append(u.Act).Append("\",\"").Append(move).Append("\",")
                      .Append(frame).Append(',').Append(u.Age).Append(',').Append(u.Hp).Append(',').Append(marks).Append(',').Append(u.Target).Append(',').Append(u.MoveTarget).Append(']');
            }
            _trace.Append(']');
        }

        public string TraceJson(int seed)
        {
            var sb = new StringBuilder();
            sb.Append("{\"tickMs\":").Append(Rules.TickMs).Append(",\"seed\":").Append(seed).Append(",\"winner\":").Append(Winner).Append(",\"end\":").Append(EndTick).Append(",\"units\":[");
            for (int i = 0; i < _units.Count; i++)
            {
                var u = _units[i];
                if (i > 0) sb.Append(',');
                sb.Append("{\"id\":").Append(u.Id).Append(",\"side\":").Append(u.Side).Append(",\"cls\":\"").Append(u.Cls).Append("\",\"tribe\":\"").Append(u.Tribe)
                  .Append("\",\"name\":\"").Append(u.Name).Append("\",\"maxHp\":").Append(u.MaxHp).Append('}');
            }
            sb.Append("],\"frames\":[").Append(_trace).Append("],\"events\":[");
            for (int i = 0; i < _events.Count; i++)
            {
                var e = _events[i];
                if (i > 0) sb.Append(',');
                sb.Append("{\"t\":").Append(e.T).Append(",\"k\":\"").Append(e.K).Append("\",\"a\":").Append(e.A).Append(",\"b\":").Append(e.B)
                  .Append(",\"n\":").Append(e.Amount).Append(",\"d\":\"").Append(e.Detail.Replace("\"", "'")).Append("\",\"x\":").Append(e.X).Append(",\"y\":").Append(e.Y)
                  .Append(",\"tier\":").Append(e.Tier).Append(",\"kind\":\"").Append(e.Kind).Append("\"}");
            }
            sb.Append("]}");
            return sb.ToString();
        }

        /// The ordered snapshot the checksum would read: every field of every fighter and every shot, in id order.
        public ulong Checksum()
        {
            ulong h = 1469598103934665603UL;
            void Mix(long v) { unchecked { h ^= (ulong)v; h *= 1099511628211UL; } }
            Mix(_tick);
            foreach (var u in _units)
            {
                Mix(u.X); Mix(u.Y); Mix((int)u.Act); Mix(u.Age); Mix(u.Hp); Mix(u.NextHit); Mix(u.MoveTarget); Mix(u.Target);
                Mix(u.AttackReadyAt); Mix(u.SnareReadyAt); Mix(u.BoostReadyAt); Mix(u.DefenceReadyAt); Mix(u.DodgeReadyAt);
                Mix(u.HeldUntil); Mix(u.PinnedUntil); Mix(u.StaggerUntil); Mix(u.OpeningUntil); Mix(u.HexUntil); Mix(u.SnareMarkUntil); Mix(u.BleedUntil);
                Mix((long)u.Rng.State);
            }
            foreach (var p in _shots) { Mix(p.Id); Mix(p.Arrives); Mix(p.Done ? 1 : 0); }
            return h;
        }
    }
}
