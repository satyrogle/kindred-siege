using System;
using KindredSiege.Sim.Battle;

namespace KindredSiege.Sim.Learning
{
    /// The Whisperer's vows and the Butcher's flavour fights on the road (campaign rules 30; Jacob in chat, 2 Nov 2026: "whisperer
    /// - 1", "butcher - 1 and 2"). Her card offers a rank up to rank 2 once for each of the fighter's two vows, and the one taken
    /// is his for the run (CampaignFighter.Vow; Battle.BattleSimulation.Vows.cs plays it). Each Butcher fight on the road shows
    /// one flavour, drawn from its node's own dice, and its card offers only that flavour's combos, so a run leans into a flavour
    /// by the road it takes. Every number is a starting value.
    public sealed partial class Campaign
    {
        // The road node's own dice for its flavour: a stream of its seed nothing else draws from.
        private const ulong FlavourStream = 20UL;

        /// Whether this run plays the vows and flavour fights: campaign rules 30.
        public bool VowsRun => Math.Min(RulesVersion, _tuning.RulesCap) >= VowFlavourRules;

        /// The flavour a Butcher fight on the road offers, for the map: Gravity Well, Fold Door or Time Stop. None for any other
        /// node, or before rules 30.
        public Augment NodeFlavour(RoadNode node)
        {
            if (node == null || node.Pal != Pal.Butcher || !VowsRun) return Augment.None;
            return FlavourOf(node);
        }

        private static Augment FlavourOf(RoadNode node)
        {
            return (Augment)(1 + new DeterministicRng(node.Seed, FlavourStream).Next(3));
        }

        // From rules 30 a Butcher card's pool: only its node's flavour, while any combo of it can still be offered.
        private bool FlavourAllows(RoadNode node, ClassBend bend, int rules)
        {
            return rules < VowFlavourRules || ClassSpells.AugmentOf(bend) == FlavourOf(node);
        }
    }
}
