using System;

namespace KindredSiege.Sim.Battle
{
    /// Payoffs that spread (Jacob's playtest, 31 Oct 2026: "read log then go build fix 1"; HANDOFF_LOG 12797, fix 1;
    /// FightRules.Payoffs, campaign rules 20). His run's biggest hits were two Marksmen both critting one snared enemy with
    /// their Aimed Shots, and rips taking a share of a 4104 health chief. So a snare pays one Aimed Shot crit and breaks,
    /// and bleeding and the rip count at most PercentHealthCap of anyone's health. Every number is a starting value.
    public static class PayoffRules
    {
        /// The most health a share of health is taken from: above every fighter's today, so only chiefs and beasts feel it.
        public const int PercentHealthCap = 800;
    }

    public sealed partial class BattleSimulation
    {
        private readonly bool _payoffsOn;

        /// Whether payoffs spread in this fight: campaign rules 20.
        public bool PayoffsOn => _payoffsOn;

        // The health a share of health comes from: all of it, or from rules 20 at most PayoffRules.PercentHealthCap.
        private int ShareHealth(BattleUnit u) => _payoffsOn ? Math.Min(u.MaxHp, PayoffRules.PercentHealthCap) : u.MaxHp;

        // From rules 20 a snare pays one Aimed Shot crit: the shot that cashed it breaks it, after its teamwork and bond have
        // counted it (ConfirmTeamwork, RoadCash).
        private void SnareSpent(BattleUnit target, string detail)
        {
            if (!_payoffsOn || !target.Alive || detail != ClassSpells.AimedPinDetail) return;
            // Time Lock (rules 46, BattleSimulation.Tree.cs): the Snare takes more cashes before it is spent.
            if (_treeOn && ++target.SnareCashes < SnareCashesFor(1 - target.Side)) return;
            target.Statuses.Clear(StatusKind.Snare);
        }
    }
}
