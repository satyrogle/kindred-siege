using System;
using System.Globalization;

namespace KindredSiege.Sim.Battle
{
    /// The grid, campaign rules 44 (FightRules.Grid). Jacob, 9 Nov 2026: "since they are behaving like grid based battle
    /// ground.. why not use megaman battle network rules not they jsut teleport where they want then wahts the point of having
    /// traits when unis can behave how ever thy want"; the shapes and kit moves locked at DESIGN_LOCKS lines 1353 and 1362;
    /// then "Build it". Only the rules idea from that game, never its look.
    /// - Each band stands on its own 3 by 3 panels: back, middle and front rows across the top, middle and bottom lanes,
    ///   counted from its own side. A band never walks onto the other's panels.
    /// - A fighter only hits whoever his class shape reaches from his panel. The five built shapes (option 1, the Warden's
    ///   option 2): the Warden guards the cross, the four panels touching his, and hits nothing; the Skirmisher's lane dash
    ///   and the Marksman's lane shot hit the first enemy in their lane, even in the back row; the Trapper sets the trap on the
    ///   enemy front row panel of her lane and it springs on whoever stands there; the Medic treats an ally touching her.
    /// - With nothing in reach a fighter steps one panel toward a panel that reaches, a short turn. Fall Back steps the band
    ///   back a row. Focus Fire sends whoever can't reach the focused enemy a step toward him.
    /// - Allies make room: a step onto an ally's panel trades places with him (for his leader, moves him one panel forward
    ///   in his own lane when it can), only when that leaves him no worse off, and only as the step that gets the stepper
    ///   something to do. Never a
    ///   leader, the Warden holding the centre, an aiming Marksman, a snared fighter, or one pushed in the last 2 s. A leader
    ///   slides along the back row while his band stands.
    /// Claude's calls, Jacob's to change: a shot passes over its own side and stops at the first enemy who can be hit; a
    /// Warden steps only back to the centre panel, and fights as plain melee once he is the last fighter of his band; allies
    /// trade panels so a crowded band never seals itself in; the
    /// Warden's cross takes shots too, by his Defence as rules 43's middle did; a leader stays in the back row while his band
    /// stands; any other class, a leader and the champion use a plain shape (a shot down the lane when ranged, else melee two
    /// panels across the line); placement puts the leader in the back middle, the champion in the front middle, the first
    /// Warden in the centre, the front row from the middle lane out and the back row from the top lane. Every number is a
    /// starting value.
    public static class GridRules
    {
        /// Rows counted from a band's own side, and lanes.
        public const int Back = 0;
        public const int Middle = 1;
        public const int Front = 2;
        public const int Top = 0;
        public const int Bottom = 2;
        public const int Size = 3;

        /// A step is a short turn: he acts again this many ticks after it.
        public const int StepTicks = 12;

        /// A fighter pushed off his panel by an ally's step can't be pushed again for this long (2 s).
        public const int TradeRestTicks = 60;

        /// Plain melee reaches this many panels across the line: from its front row the enemy front and middle rows, from its
        /// middle row the enemy front row.
        public const int MeleeReach = 2;

        /// The details of a Step event, and of the Warden's Defence event when his cross takes a blow.
        public const string StepDetail = "step";
        public const string FallBackDetail = "fallback";
        public const string JoinDetail = "join";
        public const string SwapDetail = "swap";
        public const string RoomDetail = "room";
        public const string CrossDetail = "cross";

        /// A panel as one number, for events and the snapshot: row times three plus lane.
        public static int Panel(int col, int lane) => col * Size + lane;
    }

    public sealed partial class BattleSimulation
    {
        private readonly bool _gridOn;
        // The tick each fighter was last pushed off his panel by an ally, -1 for never (by unit index).
        private long[] _gridPushedAt;
        // Set by a step and read once by the tick loop right after the turn, so it never outlives the turn.
        private bool _gridStepped;

        /// True when this fight plays the grid (campaign rules 44). For the screen: each fighter's panel is GridCol and GridLane.
        public bool GridRulesOn => _gridOn;

        /// The lane of the enemy front row panel this side's armed trap sits on, or -1. For the screen.
        public int GridTrapLane(int side)
        {
            if (!_gridOn || side < 0 || side > 1 || !_sides[side].TrapArmed) return -1;
            return _sides[side].TrapLane;
        }

        // The panels each kind of fighter takes at the start, first choice first.
        private static readonly int[] LeaderPanels = { 1, 0, 2, 4, 3, 5, 7, 6, 8 };
        private static readonly int[] ChampionPanels = { 7, 6, 8, 4, 3, 5, 1, 0, 2 };
        private static readonly int[] WardenPanels = { 4, 7, 6, 8, 3, 5, 1, 0, 2 };
        private static readonly int[] FrontPanels = { 7, 6, 8, 3, 5, 4, 0, 2, 1 };
        private static readonly int[] BackPanels = { 0, 2, 1, 3, 5, 4, 7, 6, 8 };

        // ------------------------------------------------------------ placement

        // Everyone on the field takes a panel: leaders, then the champion, then each side's first Warden, then the rest in the
        // fight's unit order. Benched reserves wait for GridJoin.
        private void PlaceOnGrid()
        {
            _gridPushedAt = new long[_units.Count];
            for (int i = 0; i < _gridPushedAt.Length; i++) _gridPushedAt[i] = -1;
            for (int pass = 0; pass < 4; pass++)
            {
                bool[] wardenPlaced = new bool[2];
                for (int i = 0; i < _units.Count; i++)
                {
                    BattleUnit u = _units[i];
                    if (!OnField(u) || u.GridCol >= 0) continue;
                    bool leader = u.IsLeader && !IsChampion(u);
                    bool champion = IsChampion(u);
                    bool warden = !u.IsLeader && u.Class == UnitClass.Warden;
                    if (pass == 0 && !leader) continue;
                    if (pass == 1 && !champion) continue;
                    if (pass == 2 && (!warden || wardenPlaced[u.Side])) continue;
                    if (pass == 2) wardenPlaced[u.Side] = true;
                    TakePanel(u, PanelsFor(u, pass));
                }
            }
        }

        // A reserve joining in his chief's second phase takes the first free panel his row would.
        private void GridJoin(BattleUnit u)
        {
            if (u.GridCol >= 0) return;
            if (TakePanel(u, PanelsFor(u, 3)))
                Emit(new BattleEvent(_tick, BattleEventKind.Step, u.Side, u.Id, (int)u.Class, string.Empty, -1,
                    GridRules.Panel(u.GridCol, u.GridLane), GridRules.JoinDetail));
        }

        private int[] PanelsFor(BattleUnit u, int pass)
        {
            if (pass == 0) return LeaderPanels;
            if (pass == 1) return ChampionPanels;
            if (pass == 2) return WardenPanels;
            return u.Row == Row.Front ? FrontPanels : BackPanels;
        }

        // True when he took a panel; with all nine taken he stays off the grid and fights by the old rules.
        private bool TakePanel(BattleUnit u, int[] order)
        {
            for (int k = 0; k < order.Length; k++)
            {
                int p = order[k];
                if (!FreeFor(u, p / GridRules.Size, p % GridRules.Size)) continue;
                u.GridCol = p / GridRules.Size;
                u.GridLane = p % GridRules.Size;
                return true;
            }
            return false;
        }

        private bool IsChampion(BattleUnit u) => ChampionOn && u.Side == 1 && u.Role == UnitRole.Chief;

        // On the field and standing: alive, not fled, not waiting on the bench.
        private static bool OnField(BattleUnit u) => u.Alive && !u.Fled && !u.Benched;

        // Nobody of his side but himself stands on this panel.
        private bool FreeFor(BattleUnit unit, int col, int lane)
        {
            if (col < 0 || col >= GridRules.Size || lane < 0 || lane >= GridRules.Size) return false;
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (ReferenceEquals(u, unit) || u.Side != unit.Side || !OnField(u)) continue;
                if (u.GridCol == col && u.GridLane == lane) return false;
            }
            return true;
        }

        // Two of one side on panels that share an edge.
        private bool Touches(BattleUnit a, BattleUnit b)
        {
            if (a == null || b == null || a.Side != b.Side || a.GridCol < 0 || b.GridCol < 0) return false;
            return Math.Abs(a.GridCol - b.GridCol) + Math.Abs(a.GridLane - b.GridLane) == 1;
        }

        // ------------------------------------------------------------ reach

        // Whoever his class shape reaches from his own panel, or null.
        private BattleUnit GridTarget(BattleUnit attacker)
        {
            return GridReachFrom(attacker, attacker.GridCol, attacker.GridLane);
        }

        // Whoever his class shape would reach from this panel of his side, or null. Read only.
        private BattleUnit GridReachFrom(BattleUnit attacker, int col, int lane)
        {
            if (col < 0) return null;
            // Guard the cross: a Warden of the band hits nothing while another fighter of his stands; the last one fights on.
            if (attacker.Class == UnitClass.Warden && !attacker.IsLeader && OtherFighterStands(attacker)) return null;
            bool downLane = attacker.Stats.Ranged || attacker.Class == UnitClass.Skirmisher;
            // Down his lane from the line outward: their front row, then middle, then back. The first enemy who can be hit.
            for (int theirCol = GridRules.Front; theirCol >= GridRules.Back; theirCol--)
            {
                // Plain melee: two panels across the line at most.
                if (!downLane && (GridRules.Front - col) + (GridRules.Front - theirCol) + 1 > GridRules.MeleeReach) return null;
                for (int i = 0; i < _units.Count; i++)
                {
                    BattleUnit e = _units[i];
                    if (e.Side == attacker.Side || e.GridCol != theirCol || e.GridLane != lane || !Hittable(attacker, e)) continue;
                    return e;
                }
            }
            return null;
        }

        // The first enemy who can be hit standing behind this one in his lane (a Steady shot's pierce), or null.
        private BattleUnit GridBehind(BattleUnit attacker, BattleUnit target)
        {
            for (int col = target.GridCol - 1; col >= GridRules.Back; col--)
                for (int i = 0; i < _units.Count; i++)
                {
                    BattleUnit e = _units[i];
                    if (e.Side == target.Side && e.GridCol == col && e.GridLane == target.GridLane && Hittable(attacker, e)) return e;
                }
            return null;
        }

        private bool Hittable(BattleUnit attacker, BattleUnit e)
        {
            return OnField(e) && e.Side != attacker.Side && e.GridCol >= 0 && !LeaderProtected(e) && !Spared(attacker, e) &&
                   e.EvacTicks <= 0;
        }

        private bool OtherFighterStands(BattleUnit unit)
        {
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (!ReferenceEquals(u, unit) && u.Side == unit.Side && OnField(u) && !u.IsLeader) return true;
            }
            return false;
        }

        // ------------------------------------------------------------ steps

        // His turn under the grid: true when he stepped instead of acting. Whatever holds his side back (a Fall Back) holds
        // his step too.
        private bool GridStepInstead(BattleUnit unit)
        {
            _gridStepped = false;
            if (unit.GridCol < 0 || MeleeHeldBack(unit) != StrikeBlock.None) return false;
            // A leader keeps to the back row while his band stands: he only slides along it.
            bool backRowOnly = unit.IsLeader && LeaderProtected(unit);
            if (WardenOfTheBand(unit))
            {
                // He steps only back onto the centre panel, when something moved him off it.
                if (unit.GridCol == GridRules.Middle && unit.GridLane == GridRules.Middle) return false;
                bool[] centre = new bool[GridRules.Size * GridRules.Size];
                centre[GridRules.Panel(GridRules.Middle, GridRules.Middle)] = GoalOk(unit, GridRules.Middle, GridRules.Middle, false);
                return StepToward(unit, centre, false);
            }
            if (unit.Class == UnitClass.Medic)
            {
                if (GridPatient(unit) != null) return false;
                // With class spells she never attacks: she steps toward the most hurt ally anywhere, or waits.
                if (_classOn)
                {
                    BattleUnit hurt = MostHurtAlly(unit, MedicHealsBelowPermille, false);
                    if (hurt == null || hurt.GridCol < 0) return false;
                    bool[] beside = new bool[GridRules.Size * GridRules.Size];
                    for (int c = 0; c < GridRules.Size; c++)
                        for (int l = 0; l < GridRules.Size; l++)
                            beside[GridRules.Panel(c, l)] = GoalOk(unit, c, l, backRowOnly) &&
                                                            Math.Abs(c - hurt.GridCol) + Math.Abs(l - hurt.GridLane) == 1;
                    return StepToward(unit, beside, backRowOnly);
                }
            }
            // Her trap needs no one in reach: she sets it on the enemy front row panel of her lane.
            if (unit.Class == UnitClass.Trapper && !_sides[unit.Side].TrapArmed && _trapReadyIn[unit.Index] <= 0) return false;

            BattleUnit reached = GridTarget(unit);
            BattleUnit focus = GridFocus(unit);
            // Focus Fire: whoever can't reach the focused enemy steps toward him, if a step gets him closer.
            if (focus != null && !ReferenceEquals(reached, focus) && StepToward(unit, ReachGoal(unit, focus, backRowOnly), backRowOnly)) return true;
            if (reached != null) return false;
            return StepToward(unit, ReachGoal(unit, null, backRowOnly), backRowOnly);
        }

        // A Warden of the band while another fighter of his stands: he holds the centre and hits nothing.
        private bool WardenOfTheBand(BattleUnit u) => u.Class == UnitClass.Warden && !u.IsLeader && OtherFighterStands(u);

        // His side's Focus Fire target while it runs, when it can be hit; a frenzied fighter ignores it.
        private BattleUnit GridFocus(BattleUnit unit)
        {
            SideState mine = _sides[unit.Side];
            if (mine.FocusTicks <= 0 || mine.FocusTargetId.Length == 0 || unit.Frenzied) return null;
            BattleUnit focus = FindById(mine.FocusTargetId);
            return focus != null && Hittable(unit, focus) ? focus : null;
        }

        // The panels of his side he could stand on from which his shape reaches the given enemy, or anyone when null.
        private bool[] ReachGoal(BattleUnit unit, BattleUnit only, bool backRowOnly)
        {
            bool[] goal = new bool[GridRules.Size * GridRules.Size];
            for (int c = 0; c < GridRules.Size; c++)
                for (int l = 0; l < GridRules.Size; l++)
                {
                    if (!GoalOk(unit, c, l, backRowOnly)) continue;
                    BattleUnit e = GridReachFrom(unit, c, l);
                    goal[GridRules.Panel(c, l)] = e != null && (only == null || ReferenceEquals(e, only));
                }
            return goal;
        }

        // The ally of his standing on this panel, or null.
        private BattleUnit AllyOn(BattleUnit unit, int col, int lane)
        {
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (!ReferenceEquals(u, unit) && u.Side == unit.Side && OnField(u) && u.GridCol == col && u.GridLane == lane) return u;
            }
            return null;
        }

        // Allies make room: one who isn't a leader, the Warden holding the centre, aiming, snared or pushed in the last
        // GridRules.TradeRestTicks can trade panels. The rest keeps a leader and their shooter from chasing his Medic from
        // lane to lane every step.
        private bool Movable(BattleUnit ally)
        {
            if (ally.IsLeader || ally.AimTicks > 0 || ally.SnareTicks > 0 || ExchangeOf(ally) != null) return false;
            if (_gridPushedAt[ally.Index] >= 0 && _tick - _gridPushedAt[ally.Index] < GridRules.TradeRestTicks) return false;
            return !(WardenOfTheBand(ally) && ally.GridCol == GridRules.Middle && ally.GridLane == GridRules.Middle);
        }

        // Where an ally goes to make room for a step onto his panel from (fromCol, fromLane): for his leader sliding along the
        // back row, one panel forward in his own lane when it is free and leaves him no worse off, so his lane and his job
        // stay the same and nobody chases him from lane to lane; else onto the stepper's panel when that leaves him no worse
        // off. Never worse off, so two fighters never keep trading one panel back and forth.
        private bool MakesRoom(BattleUnit ally, int fromCol, int fromLane, bool forLeader, out int toCol, out int toLane)
        {
            toCol = -1;
            toLane = -1;
            if (!Movable(ally)) return false;
            int here = JobAt(ally, ally.GridCol, ally.GridLane);
            int fc = ally.GridCol + 1;
            if (forLeader && fc < GridRules.Size && !(fc == fromCol && ally.GridLane == fromLane) && FreeFor(ally, fc, ally.GridLane) &&
                JobAt(ally, fc, ally.GridLane) >= here)
            {
                toCol = fc;
                toLane = ally.GridLane;
                return true;
            }
            if (JobAt(ally, fromCol, fromLane) < here) return false;
            toCol = fromCol;
            toLane = fromLane;
            return true;
        }

        // What he has to do from this panel: 2 when he reaches his side's Focus Fire target (with no focus, anyone), or for
        // a Medic a patient touching it; 1 for another job (someone else in reach, a trap to set); 0 for nothing.
        private int JobAt(BattleUnit u, int col, int lane)
        {
            if (WardenOfTheBand(u)) return 0;
            if (u.Class == UnitClass.Medic && _classOn) return GridPatientAt(u, col, lane) != null ? 2 : 0;
            BattleUnit e = GridReachFrom(u, col, lane);
            if (e == null) return u.Class == UnitClass.Trapper && !_sides[u.Side].TrapArmed && _trapReadyIn[u.Index] <= 0 ? 1 : 0;
            BattleUnit focus = GridFocus(u);
            return focus == null || ReferenceEquals(e, focus) ? 2 : 1;
        }

        // A panel of his side he could end a step on, free or held by an ally who can make room.
        private bool Standable(BattleUnit unit, int col, int lane, bool backRowOnly)
        {
            if (col < 0 || col >= GridRules.Size || lane < 0 || lane >= GridRules.Size) return false;
            if (backRowOnly && col != GridRules.Back) return false;
            if (!PairPanelAvailable(unit, col, lane)) return false;
            BattleUnit ally = AllyOn(unit, col, lane);
            return ally == null || Movable(ally);
        }

        // A free panel of his side he could pass over on the way.
        private bool Passable(BattleUnit unit, int col, int lane, bool backRowOnly)
        {
            if (backRowOnly && col != GridRules.Back) return false;
            if (!PairPanelAvailable(unit, col, lane)) return false;
            return FreeFor(unit, col, lane);
        }

        // A panel he could really end on: free, or held by an ally who would trade onto a panel beside it that he could step
        // from. Without this he would walk up to an ally who never gives way and trade places with the wrong one on the way.
        private bool GoalOk(BattleUnit unit, int col, int lane, bool backRowOnly)
        {
            if (!Standable(unit, col, lane, backRowOnly)) return false;
            BattleUnit ally = AllyOn(unit, col, lane);
            if (ally == null) return true;
            for (int k = 0; k < 4; k++)
            {
                int nc = col + StepCol(k), nl = lane + StepLane(k);
                if (nc < 0 || nc >= GridRules.Size || nl < 0 || nl >= GridRules.Size) continue;
                if (Passable(unit, nc, nl, backRowOnly) && MakesRoom(ally, nc, nl, backRowOnly, out _, out _)) return true;
            }
            return false;
        }

        // One step along the shortest way to the nearest goal panel, over free panels. Only the last step, onto the goal
        // itself, may trade places with an ally standing there, so every trade gives the band something it lacked and trades
        // can never go round in circles. Ties go forward, then up, then down, then back. False when he already stands on a
        // goal or no step gets him closer: then he takes his turn as usual.
        private bool StepToward(BattleUnit unit, bool[] goal, bool backRowOnly)
        {
            const int far = 99;
            int n = GridRules.Size * GridRules.Size;
            int[] dist = new int[n];
            int[] queue = new int[n];
            int head = 0, tail = 0;
            for (int p = 0; p < n; p++)
            {
                dist[p] = far;
                if (!goal[p]) continue;
                dist[p] = 0;
                queue[tail++] = p;
            }
            while (head < tail)
            {
                int p = queue[head++];
                int c = p / GridRules.Size, l = p % GridRules.Size;
                // A goal an ally holds is only reached from a side he would make room from.
                BattleUnit holder = dist[p] == 0 ? AllyOn(unit, c, l) : null;
                for (int k = 0; k < 4; k++)
                {
                    int nc = c + StepCol(k), nl = l + StepLane(k);
                    if (!Passable(unit, nc, nl, backRowOnly)) continue;
                    if (holder != null && !MakesRoom(holder, nc, nl, backRowOnly, out _, out _)) continue;
                    int q = GridRules.Panel(nc, nl);
                    if (dist[q] != far) continue;
                    dist[q] = dist[p] + 1;
                    queue[tail++] = q;
                }
            }
            int fromCol = unit.GridCol, fromLane = unit.GridLane;
            int here = dist[GridRules.Panel(fromCol, fromLane)];
            if (here == 0 || here == far) return false;
            for (int k = 0; k < 4; k++)
            {
                int nc = fromCol + StepCol(k), nl = fromLane + StepLane(k);
                if (!Standable(unit, nc, nl, backRowOnly) || dist[GridRules.Panel(nc, nl)] != here - 1) continue;
                BattleUnit ally = AllyOn(unit, nc, nl);
                int toCol = -1, toLane = -1;
                if (ally != null && (!goal[GridRules.Panel(nc, nl)] || !MakesRoom(ally, fromCol, fromLane, backRowOnly, out toCol, out toLane))) continue;
                MoveTo(unit, nc, nl, GridRules.StepDetail);
                if (ally != null)
                {
                    MoveTo(ally, toCol, toLane, toCol == fromCol && toLane == fromLane ? GridRules.SwapDetail : GridRules.RoomDetail);
                    _gridPushedAt[ally.Index] = _tick;
                }
                _gridStepped = true;
                return true;
            }
            return false;
        }

        // Forward, up, down, back.
        private static int StepCol(int k) => k == 0 ? 1 : k == 3 ? -1 : 0;
        private static int StepLane(int k) => k == 1 ? -1 : k == 2 ? 1 : 0;

        private void MoveTo(BattleUnit unit, int col, int lane, string detail)
        {
            unit.GridCol = col;
            unit.GridLane = lane;
            Emit(new BattleEvent(_tick, BattleEventKind.Step, unit.Side, unit.Id, (int)unit.Class, string.Empty, -1,
                GridRules.Panel(col, lane), detail));
        }

        // Read once by the tick loop right after the turn.
        private bool StepTaken()
        {
            bool stepped = _gridStepped;
            _gridStepped = false;
            return stepped;
        }

        // Fall Back: the band steps back one row, where the panel behind is free. The middle row first, so the front can follow.
        private void GridFallBack(int side)
        {
            for (int col = GridRules.Middle; col <= GridRules.Front; col++)
                for (int lane = 0; lane < GridRules.Size; lane++)
                    for (int i = 0; i < _units.Count; i++)
                    {
                        BattleUnit u = _units[i];
                        if (u.Side != side || !OnField(u) || u.GridCol != col || u.GridLane != lane) continue;
                        if (FreeFor(u, col - 1, lane)) MoveTo(u, col - 1, lane, GridRules.FallBackDetail);
                        break;
                    }
        }

        // ------------------------------------------------------------ the Medic

        // Touch: the most hurt ally on her own panel or one touching it, her hurt chief first while his band stands. Her
        // Triage habit treats only under its line. Null when nobody there needs her.
        private BattleUnit GridPatient(BattleUnit medic)
        {
            return GridPatientAt(medic, medic.GridCol, medic.GridLane);
        }

        // The same, as if she stood on this panel.
        private BattleUnit GridPatientAt(BattleUnit medic, int col, int lane)
        {
            if (_riteOn)
            {
                BattleUnit rite = RitePatient(medic);
                if (rite != null && Within(rite, medic, col, lane)) return rite;
            }
            int below = _whisperOn && HabitOf(medic) == WhispererHabit.Triage ? WhisperRules.TriageBelowPermille : MedicHealsBelowPermille;
            bool chiefFirst = _chiefOn && BandStands(medic.Side);
            BattleUnit chief = null;
            BattleUnit best = null;
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                if (!u.Alive || u.Side != medic.Side || !Within(u, medic, col, lane)) continue;
                if (chiefFirst && chief == null && u.Role == UnitRole.Chief && u.HpPermille < MedicHealsBelowPermille) chief = u;
                if (u.HpPermille >= below) continue;
                if (best == null || u.HpPermille < best.HpPermille) best = u;
            }
            return chief ?? best;
        }

        // Herself, or an ally on a panel touching this one.
        private static bool Within(BattleUnit u, BattleUnit medic, int col, int lane)
        {
            if (ReferenceEquals(u, medic)) return true;
            return u.GridCol >= 0 && Math.Abs(u.GridCol - col) + Math.Abs(u.GridLane - lane) == 1;
        }

        // ------------------------------------------------------------ the Warden's cross

        // Guard the cross: a Warden of the band on a panel touching the target's may step in front of the blow, melee or shot,
        // by his Defence as rules 43's middle did. One roll on the fight's stream. Null when the blow goes past him.
        private BattleUnit CrossWarden(BattleUnit attacker, BattleUnit target)
        {
            if (attacker == null || target.Class == UnitClass.Warden || target.GridCol < 0) return null;
            // Turned Hunt: nobody covers the hunters while it lasts (BattleSimulation.Hunt.cs).
            if (_huntOn && Turned(target)) return null;
            BattleUnit warden = null;
            for (int i = 0; i < _units.Count && warden == null; i++)
            {
                BattleUnit u = _units[i];
                if (u.Class == UnitClass.Warden && u.Role == UnitRole.Fighter && !ReferenceEquals(u, target) && OnField(u) &&
                    !u.Yielded && u.SnareTicks <= 0 && u.StunTicks <= 0 && u.PanicTicks <= 0 && Touches(u, target))
                    warden = u;
            }
            if (warden == null) return null;
            int chance = AttackTable.Of(warden.Class).Defence * LeaderRules.MiddlePercent / 100;
            if (_rng.Next(100) >= chance) return null;
            Emit(EventFor(BattleEventKind.Defence, warden, target, chance, GridRules.CrossDetail));
            return warden;
        }

        // ------------------------------------------------------------ the trap on a panel

        // Each tick: an armed trap sits on the enemy front row panel of the lane its Trapper stood in when she set it, and
        // springs on whoever stands there. An Ambusher's trap still waits for a heavy swing (BattleSimulation.Whisperer.cs).
        private void TickGrid()
        {
            for (int side = 0; side < 2 && !Stopped; side++)
            {
                SideState state = _sides[side];
                if (!state.TrapArmed)
                {
                    state.TrapLane = -1;
                    state.TrapSprungOn = string.Empty;
                    continue;
                }
                if (state.TrapLane < 0)
                {
                    BattleUnit owner = FindById(state.TrapOwnerId);
                    state.TrapLane = owner != null && owner.GridLane >= 0 ? owner.GridLane : GridRules.Middle;
                }
                if (AmbushWaits(side)) continue;
                for (int i = 0; i < _units.Count; i++)
                {
                    BattleUnit e = _units[i];
                    if (e.Side == side || !OnField(e) || e.GridCol != GridRules.Front || e.GridLane != state.TrapLane) continue;
                    if (LeaderProtected(e) || e.EvacTicks > 0) continue;
                    if (string.Equals(state.TrapSprungOn, e.Id, StringComparison.Ordinal)) continue;
                    SpringOnPanel(side, e);
                    break;
                }
            }
        }

        private void SpringOnPanel(int side, BattleUnit victim)
        {
            SideState state = _sides[side];
            BattleUnit trapper = FindById(state.TrapOwnerId);
            // A rank 2 Trapper's trap springs twice (rules 24), never twice on one body standing still.
            if (!SpringLeft(state))
            {
                state.TrapArmed = false;
                state.TrapOwnerId = string.Empty;
                state.TrapLane = -1;
                state.TrapSprungOn = string.Empty;
            }
            else state.TrapSprungOn = victim.Id;
            Emit(new BattleEvent(_tick, BattleEventKind.TrapSprung, side,
                trapper == null ? string.Empty : trapper.Id, trapper == null ? -1 : (int)trapper.Class,
                victim.Id, (int)victim.Class, SliceClasses.TrapDamage, string.Empty));
            if (!_classOn) victim.Cooldown += SliceClasses.TrapSlowTicks;
            ApplyDamage(trapper, side, victim, SliceClasses.TrapDamage, TrapDetail);
            // A barbed snare holds whoever stood there (ClassSpells).
            if (_classOn && victim.Alive && !_finished) Snare(trapper, side, victim);
            // Doorway's hook (rules 46, BattleSimulation.Tree.cs): her catch lands face down, left with an Opening.
            if (_treeOn && _comboOn && trapper != null && victim.Alive && !_finished && Wears(trapper, ClassBend.Doorway)) Open(trapper, victim);
            CheckWipe();
        }

        // A trap just set: it sits in the lane its Trapper stands in.
        private void SetTrapLane(SideState state, BattleUnit trapper)
        {
            state.TrapLane = trapper.GridLane >= 0 ? trapper.GridLane : GridRules.Middle;
            state.TrapSprungOn = string.Empty;
        }

        // ------------------------------------------------------------ snapshot

        private void AppendGrid(SnapshotBuilder b)
        {
            b.Field("grid", true);
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                b.Field("panel" + i.ToString(CultureInfo.InvariantCulture), u.GridCol < 0 ? -1 : GridRules.Panel(u.GridCol, u.GridLane));
                b.Field("pushedAt" + i.ToString(CultureInfo.InvariantCulture), _gridPushedAt[i]);
            }
            for (int s = 0; s < 2; s++)
            {
                string key = s.ToString(CultureInfo.InvariantCulture);
                b.Field("trapLane" + key, _sides[s].TrapLane);
                b.Field("trapSprungOn" + key, _sides[s].TrapSprungOn);
            }
        }
    }
}
