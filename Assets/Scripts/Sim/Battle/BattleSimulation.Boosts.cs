using System;

namespace KindredSiege.Sim.Battle
{
    /// The road kit's boosts and Fall Back (Jacob, 25 Oct 2026: "these are great as long as they re code and what player has not
    /// to worry", then "lets keep it as dodge"; FightRules.RoadKit, campaign rules version 12).
    /// - Fall Back is a pure dodge: while a side falls back nobody of it attacks, ranged fighters included. It still dodges the
    ///   big hits it always dodged.
    /// - Every class's boost, the kit's third move, comes on its own, every BoostCooldownTicks and only with a real target; the
    ///   player never presses it. Each one feeds a combo:
    ///   - Warden, Shield Bash: on his turn, the enemy in front of him takes BashPercent of his power, staggers and is left with
    ///     an Opening (his Skirmisher reacts to it).
    ///   - Marksman, Steady: the shot he starts aiming can't be broken, and goes through to the enemy behind its mark for
    ///     PiercePercent of his power.
    ///   - Skirmisher, Twin Cut: while her strike is not ready, two quick cuts of TwinCutPercent of her power that leave a bleed,
    ///     which her next strike rips open.
    ///   - Trapper, Rearm: her trap is armed again at once once it has sprung.
    ///   - Medic, Stim: when nobody of hers is below half health, the Marksman or Skirmisher furthest from his spell gets it at
    ///     once. Never herself, and nothing Stims a Stim.
    /// Every number is a starting value.
    public static class BoostRules
    {
        public const int BoostCooldownTicks = 15 * SimClock.TicksPerSecond;
        public const int BashPercent = 50;
        public const int PiercePercent = 50;
        public const int TwinCutPercent = 60;
        public const int StimBelowPermille = 500;

        /// The Boost event's detail for each move.
        public const string ShieldBash = "shieldbash";
        public const string Steady = "steady";
        public const string TwinCut = "twincut";
        public const string Rearm = "rearm";
        public const string Stim = "stim";

        /// The Hit details of the bash, the pierce and the twin cut.
        public const string BashDetail = "bash";
        public const string PierceDetail = "pierce";
        public const string CutDetail = "twincut";
    }

    public sealed partial class BattleSimulation
    {
        /// True when this fight plays the road kit (FightRules.RoadKit).
        private readonly bool _roadKitOn;

        /// True when this fight plays the road kit (FightRules.RoadKit).
        public bool RoadKitOn => _roadKitOn;

        // Every boost starts halfway to ready, and every carrier gets his first move's clock.
        private void StartRoadKit()
        {
            int carriers = 0;
            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit u = _units[i];
                u.BoostTicks = BoostRules.BoostCooldownTicks / 2;
                ChiefMove carries = SpecOf(u).Carries;
                if (carries == ChiefMove.None || !CarriersAvailable) continue;
                u.CarryMove = FirstCarried(carries);
                u.CarryNext = CarryRules.FirstTicks + carriers * CarryRules.StaggerTicks;
                carriers++;
            }
        }

        // While a side falls back nobody of it attacks (the road kit), unless it plays Shadow Strike (rules 28).
        private bool FallingBack(int side)
        {
            if (!_roadKitOn || side < 0 || side > 1) return false;
            // Rift Mine's price (rules 28): the side's attacks wait out its return as well.
            if (_shadeOn && _sides[side].ReturnTicks > 0) return true;
            return _sides[side].FallBackTicks > 0 && !ShadowStrike(side);
        }

        private void Boosted(BattleUnit by, BattleUnit on, int amount, string move)
        {
            by.BoostTicks = BoostRules.BoostCooldownTicks;
            Emit(EventFor(BattleEventKind.Boost, by, on, amount, move));
        }

        // A fighter's turn under the road kit: his boost, if it is ready and has a real target. True when it took the turn.
        private bool TryBoost(BattleUnit unit)
        {
            if (!_roadKitOn || !BoostsAvailable || SimpleEnemy(unit) || unit.BoostTicks > 0 || FallingBack(unit.Side) || unit.Frenzied) return false;
            switch (unit.Class)
            {
                case UnitClass.Warden:
                {
                    BattleUnit target = ChooseTarget(unit);
                    if (target == null || target.Row != Row.Front) return false;
                    int bash = Math.Max(1, unit.Power * BoostRules.BashPercent / 100);
                    Boosted(unit, target, bash, BoostRules.ShieldBash);
                    target.Cooldown += FightRules.StaggerTicks;
                    ApplyDamage(unit, unit.Side, target, Armored(target, bash), BoostRules.BashDetail);
                    if (target.Alive && !_finished) Open(unit, target);
                    return true;
                }
                case UnitClass.Skirmisher:
                {
                    if (unit.SpellTicks <= 0) return false;
                    BattleUnit target = SpellTarget(unit, false);
                    if (target == null) return false;
                    Boosted(unit, target, 0, BoostRules.TwinCut);
                    int cut = Math.Max(1, unit.Power * BoostRules.TwinCutPercent / 100);
                    for (int i = 0; i < 2 && target.Alive && !_finished; i++)
                        ApplyDamage(unit, unit.Side, target, Armored(target, cut), BoostRules.CutDetail);
                    if (!target.Alive || _finished) return true;
                    target.Statuses.Apply(StatusKind.Bleed, BleedRules.Ticks, ClassSpells.SnareBleedPercent);
                    MarkEvent(unit.Side, unit, target, BleedRules.Ticks, ClassSpells.BleedMark);
                    return true;
                }
                case UnitClass.Trapper:
                {
                    SideState own = _sides[unit.Side];
                    if (own.TrapArmed) return false;
                    Boosted(unit, null, 0, BoostRules.Rearm);
                    own.TrapArmed = true;
                    own.TrapOwnerId = unit.Id;
                    // Twice for a rank 2 Trapper (rules 24, BattleSimulation.Whisperer.cs).
                    if (_whisperOn) own.TrapSprings = SpringsOf(unit) + FoldTrapSprings(unit);
                    _trapReadyIn[unit.Index] = SliceClasses.TrapRearmActions;
                    Emit(EventFor(BattleEventKind.TrapArmed, unit, null, 0, string.Empty));
                    return true;
                }
                case UnitClass.Medic:
                {
                    for (int i = 0; i < _units.Count; i++)
                    {
                        BattleUnit u = _units[i];
                        if (u.Alive && u.Side == unit.Side && u.HpPermille < BoostRules.StimBelowPermille) return false;
                    }
                    BattleUnit best = null;
                    for (int i = 0; i < _units.Count; i++)
                    {
                        BattleUnit u = _units[i];
                        if (!u.Alive || u.Side != unit.Side || u.PanicTicks > 0 || u.Frenzied) continue;
                        if (u.Class != UnitClass.Marksman && u.Class != UnitClass.Skirmisher) continue;
                        if (HalfReady(u)) continue;
                        if (best == null || u.SpellTicks > best.SpellTicks) best = u;
                    }
                    if (best == null) return false;
                    Boosted(unit, best, best.SpellTicks, BoostRules.Stim);
                    best.SpellTicks = 0;
                    return true;
                }
                default:
                    return false;
            }
        }

        // A Marksman starting an aim with his Steady ready: the shot can't be broken and goes through.
        private void SteadyAim(BattleUnit unit, BattleUnit target)
        {
            if (!_roadKitOn || !BoostsAvailable || SimpleEnemy(unit) || (ClearRoadOn && FallingBack(unit.Side)) || unit.BoostTicks > 0 || unit.Steady) return;
            unit.Steady = true;
            Boosted(unit, target, 0, BoostRules.Steady);
        }

        // A Steady shot landed: it goes through to the enemy behind its mark.
        private void Pierce(BattleUnit marksman, BattleUnit target)
        {
            marksman.Steady = false;
            // Version-gated: rules 12 evidence retains its original piercing hit.
            if (ClearRoadOn && FallingBack(marksman.Side)) return;
            if (_finished || target == null || target.Row != Row.Front) return;
            BattleUnit behind = NearestInLane(target.Side, Row.Back, LanePosition(target));
            if (behind == null || !behind.Alive) return;
            ApplyDamage(marksman, marksman.Side, behind, Armored(behind, Math.Max(1, marksman.Power * BoostRules.PiercePercent / 100)),
                BoostRules.PierceDetail);
        }
    }
}
