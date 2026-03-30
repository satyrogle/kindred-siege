using System.Collections.Generic;
using UnityEngine;
using KindredSiege.Battle;

namespace KindredSiege.Units
{
    // ════════════════════════════════════════════════════════════════════════
    // TALENT NODE ENUM  (GDD §9)
    // 8 classes × 10 nodes (2 branches × 5) = 80 nodes.
    // Branch 0 = Psychological Durability  |  Branch 1 = Combat Power
    // ════════════════════════════════════════════════════════════════════════

    public enum TalentNodeId
    {
        None = 0,

        // ── WARDEN ─────────────────────────────────────────────────────────
        Warden_Resolve_1,   // +10 max sanity
        Warden_Resolve_2,   // Comprehension −0.1
        Warden_Resolve_3,   // Immune to first Affliction roll per battle
        Warden_Resolve_4,   // Ally-death sanity loss halved (−7 instead of −15)
        Warden_Resolve_5,   // Aura: nearby allies take 25% less Horror Rating drain
        Warden_Frontline_1, // +15% max HP
        Warden_Frontline_2, // +10% attack damage
        Warden_Frontline_3, // Armour absorbs first 5 sanity damage per battle
        Warden_Frontline_4, // Taunt: enemies prioritise this unit for 5 s at battle start
        Warden_Frontline_5, // Heroic Sacrifice: on death allies gain +10 sanity instead of −15

        // ── MARKSMAN ───────────────────────────────────────────────────────
        Marksman_Precision_1, // +1 attack range
        Marksman_Precision_2, // Attack cooldown −15%
        Marksman_Precision_3, // +5 sanity on every kill
        Marksman_Precision_4, // 25% chance to ignore target armour
        Marksman_Precision_5, // Auto-kill targets below 15% HP (once per battle)
        Marksman_Endurance_1, // +10% max HP
        Marksman_Endurance_2, // Comprehension −0.15
        Marksman_Endurance_3, // +5 max sanity
        Marksman_Endurance_4, // Fatigue accrual −25%
        Marksman_Endurance_5, // Auto-analyse nearest enemy at battle start (free Insight Flash)

        // ── SHADOW ─────────────────────────────────────────────────────────
        Shadow_Hunter_1,   // +15% move speed
        Shadow_Hunter_2,   // Vanish recharges after 8 s (can trigger again same battle)
        Shadow_Hunter_3,   // Vanish threshold raised to 50% HP (triggers earlier)
        Shadow_Hunter_4,   // Silent Strike: no EldritchHit sanity loss when Shadow attacks
        Shadow_Hunter_5,   // Backstab: +30% damage when attacking from behind (flank)
        Shadow_Survivor_1, // +10 max sanity
        Shadow_Survivor_2, // Immune to Solitude Phobia
        Shadow_Survivor_3, // +4 sanity on every kill
        Shadow_Survivor_4, // Comprehension −0.1
        Shadow_Survivor_5, // Second Vanish: unlocks a second Vanish use per battle

        // ── BERSERKER ──────────────────────────────────────────────────────
        Berserker_Bloodlust_1, // Blood Rage max stacks +3 (cap 8 instead of 5)
        Berserker_Bloodlust_2, // +3 sanity on every kill
        Berserker_Bloodlust_3, // Frenzy: below 30% HP → +25% damage bonus
        Berserker_Bloodlust_4, // Enters battle with 2 Blood Rage stacks already active
        Berserker_Bloodlust_5, // Blood Rage stacks never reset between battles
        Berserker_Toughness_1, // +20% max HP
        Berserker_Toughness_2, // +3 armour
        Berserker_Toughness_3, // Ally-death sanity loss −5 (instead of −15)
        Berserker_Toughness_4, // 15% chance to ignore incoming physical hit
        Berserker_Toughness_5, // Undying Rage: survive one lethal hit at 1 HP per battle

        // ── INVESTIGATOR ───────────────────────────────────────────────────
        Investigator_Scholar_1, // Forbidden Knowledge cap +10 before penalties increase
        Investigator_Scholar_2, // Insight Flash trigger chance 30% instead of 20%
        Investigator_Scholar_3, // Comprehension −0.15 when in Study stance
        Investigator_Scholar_4, // Entire team benefits from revealed enemy weaknesses
        Investigator_Scholar_5, // Mental Fortress: immune to Dread Contest hesitation lock
        Investigator_Resolve_1, // +15 max sanity
        Investigator_Resolve_2, // Full Rest FK recovery costs 0 gold at the city
        Investigator_Resolve_3, // First phobia roll per battle has 50% chance to fail
        Investigator_Resolve_4, // FK accumulation rate halved (1 instead of 2 per scan)
        Investigator_Resolve_5, // Psychic Shield: Dread Contest damage reduced by 30%

        // ── HERALD ─────────────────────────────────────────────────────────
        Herald_Devotion_1, // Martyrdom Pulse heals +5 extra sanity
        Herald_Devotion_2, // Second Pulse: Martyrdom Pulse can trigger a second time per battle
        Herald_Devotion_3, // All ally heals (from any source) +15%
        Herald_Devotion_4, // Pulse range doubles (from 4 to 8 units, or global)
        Herald_Devotion_5, // Death Blessing: on death entire team gains +20 sanity
        Herald_Endurance_1, // +15% max HP
        Herald_Endurance_2, // Comprehension reduced to 1.0 (from 1.1)
        Herald_Endurance_3, // Ally-death sanity loss −8 (instead of −15)
        Herald_Endurance_4, // Immune to first Affliction roll per battle
        Herald_Endurance_5, // Passive drain to allies −50% (Herald's presence is calming)

        // ── OCCULTIST ──────────────────────────────────────────────────────
        Occultist_Void_1,      // Psychic Recoil chance 35% (from 25%)
        Occultist_Void_2,      // Psychic Recoil damage −8 (from −5)
        Occultist_Void_3,      // Psychic Recoil has 20% chance to apply 1-tick stun to attacker
        Occultist_Void_4,      // Recoil chains: hits nearest enemy to attacker for half damage
        Occultist_Void_5,      // Maddening Aura: enemies within 3 units lose 2 sanity per 5 s
        Occultist_Endurance_1, // +10% max HP
        Occultist_Endurance_2, // Comprehension reduced to 1.1 (from 1.3)
        Occultist_Endurance_3, // +5 sanity at battle start
        Occultist_Endurance_4, // Immune to EldritchHit sanity drain when defending
        Occultist_Endurance_5, // Focus: sanity above 70 grants +10% damage bonus

        // ── VESSEL ─────────────────────────────────────────────────────────
        Vessel_Eldritch_1, // Death Denied chance 35% (from 20%)
        Vessel_Eldritch_2, // Two Death Denied uses per battle (instead of one)
        Vessel_Eldritch_3, // On Death Denied: nearby allies gain +8 sanity
        Vessel_Eldritch_4, // Passive sanity drain halved
        Vessel_Eldritch_5, // Death Burst: on death deal 30 damage to nearest enemy
        Vessel_Presence_1, // +20 max sanity
        Vessel_Presence_2, // Comprehension reduced to 0.8 (from 1.0)
        Vessel_Presence_3, // Drain doesn't start until battle round 4 (instead of passive)
        Vessel_Presence_4, // Sustaining Aura: allies within 2 units regenerate 1 HP/2 s
        Vessel_Presence_5, // Eldritch Anchor: reduces all enemy Horror Rating drain by 1
    }

    // ════════════════════════════════════════════════════════════════════════
    // TALENT SYSTEM — applies unlocked talents to a UnitController at battle start
    // ════════════════════════════════════════════════════════════════════════

    public static class TalentSystem
    {
        /// <summary>
        /// Called at the END of UnitController.Initialise().
        /// Applies all unlocked talent stat boosts and sets runtime flags.
        /// </summary>
        public static void ApplyTalents(UnitController unit)
        {
            if (unit?.Data == null || unit.Data.UnlockedTalents == null) return;

            foreach (var node in unit.Data.UnlockedTalents)
                Apply(unit, node);
        }

        private static void Apply(UnitController u, TalentNodeId node)
        {
            switch (node)
            {
                // ── WARDEN ─────────────────────────────────────────────────
                case TalentNodeId.Warden_Resolve_1:   u.MaxSanity  += 10; u.CurrentSanity = Mathf.Min(u.CurrentSanity + 10, u.MaxSanity); break;
                case TalentNodeId.Warden_Resolve_2:   u.TalentComprehensionMod -= 0.1f; break;
                case TalentNodeId.Warden_Resolve_3:   u.TalentImmuneFirstAffliction = true; break;
                case TalentNodeId.Warden_Resolve_4:   u.TalentAllyDeathSanityAmount = -7; break;
                case TalentNodeId.Warden_Resolve_5:   u.TalentHorrorAura = true; break;
                case TalentNodeId.Warden_Frontline_1: u.MaxHP = Mathf.RoundToInt(u.MaxHP * 1.15f); u.CurrentHP = u.MaxHP; break;
                case TalentNodeId.Warden_Frontline_2: u.AttackDamage = Mathf.RoundToInt(u.AttackDamage * 1.10f); break;
                case TalentNodeId.Warden_Frontline_3: u.TalentArmourSanityAbsorb = true; break;
                case TalentNodeId.Warden_Frontline_4: u.TalentTauntOnStart = true; break;
                case TalentNodeId.Warden_Frontline_5: u.TalentHeroicSacrifice = true; break;

                // ── MARKSMAN ───────────────────────────────────────────────
                case TalentNodeId.Marksman_Precision_1: u.AttackRange += 1f; break;
                case TalentNodeId.Marksman_Precision_2: u.AttackCooldownMod *= 0.85f; break;
                case TalentNodeId.Marksman_Precision_3: u.TalentSanityOnKillBonus += 5; break;
                case TalentNodeId.Marksman_Precision_4: u.TalentIgnoreArmourChance = 0.25f; break;
                case TalentNodeId.Marksman_Precision_5: u.TalentExecute = true; break;
                case TalentNodeId.Marksman_Endurance_1: u.MaxHP = Mathf.RoundToInt(u.MaxHP * 1.10f); u.CurrentHP = u.MaxHP; break;
                case TalentNodeId.Marksman_Endurance_2: u.TalentComprehensionMod -= 0.15f; break;
                case TalentNodeId.Marksman_Endurance_3: u.MaxSanity += 5; u.CurrentSanity = Mathf.Min(u.CurrentSanity + 5, u.MaxSanity); break;
                case TalentNodeId.Marksman_Endurance_4: u.TalentReducedFatigue = true; break;
                case TalentNodeId.Marksman_Endurance_5: u.TalentBattleStartAnalyse = true; break;

                // ── SHADOW ─────────────────────────────────────────────────
                case TalentNodeId.Shadow_Hunter_1:   u.MoveSpeed *= 1.15f; break;
                case TalentNodeId.Shadow_Hunter_2:   u.TalentVanishRecharge = true; break;
                case TalentNodeId.Shadow_Hunter_3:   u.TalentVanishThreshold = 0.50f; break;
                case TalentNodeId.Shadow_Hunter_4:   u.TalentSilentStrike = true; break;
                case TalentNodeId.Shadow_Hunter_5:   u.TalentBackstabBonus = 0.30f; break;
                case TalentNodeId.Shadow_Survivor_1: u.MaxSanity += 10; u.CurrentSanity = Mathf.Min(u.CurrentSanity + 10, u.MaxSanity); break;
                case TalentNodeId.Shadow_Survivor_2: u.TalentSolitudeImmune = true; break;
                case TalentNodeId.Shadow_Survivor_3: u.TalentSanityOnKillBonus += 4; break;
                case TalentNodeId.Shadow_Survivor_4: u.TalentComprehensionMod -= 0.1f; break;
                case TalentNodeId.Shadow_Survivor_5: u.TalentSecondVanish = true; break;

                // ── BERSERKER ──────────────────────────────────────────────
                case TalentNodeId.Berserker_Bloodlust_1: u.TalentBloodRageMaxStacks = 8; break;
                case TalentNodeId.Berserker_Bloodlust_2: u.TalentSanityOnKillBonus += 3; break;
                case TalentNodeId.Berserker_Bloodlust_3: u.TalentFrenzy = true; break;
                case TalentNodeId.Berserker_Bloodlust_4: u.TalentBloodRageStartStacks = 2; break;
                case TalentNodeId.Berserker_Bloodlust_5: u.TalentBloodRagePersists = true; break;
                case TalentNodeId.Berserker_Toughness_1: u.MaxHP = Mathf.RoundToInt(u.MaxHP * 1.20f); u.CurrentHP = u.MaxHP; break;
                case TalentNodeId.Berserker_Toughness_2: u.Armour += 3; break;
                case TalentNodeId.Berserker_Toughness_3: u.TalentAllyDeathSanityAmount = -5; break;
                case TalentNodeId.Berserker_Toughness_4: u.TalentHitIgnoreChance = 0.15f; break;
                case TalentNodeId.Berserker_Toughness_5: u.TalentUndyingRage = true; break;

                // ── INVESTIGATOR ───────────────────────────────────────────
                case TalentNodeId.Investigator_Scholar_1: u.TalentFKCapBonus = 10; break;
                case TalentNodeId.Investigator_Scholar_2: u.TalentInsightFlashChance = 0.30f; break;
                case TalentNodeId.Investigator_Scholar_3: u.TalentComprehensionMod -= 0.15f; break;
                case TalentNodeId.Investigator_Scholar_4: u.TalentTeamRevealBonus = true; break;
                case TalentNodeId.Investigator_Scholar_5: u.TalentMentalFortress = true; break;
                case TalentNodeId.Investigator_Resolve_1: u.MaxSanity += 15; u.CurrentSanity = Mathf.Min(u.CurrentSanity + 15, u.MaxSanity); break;
                case TalentNodeId.Investigator_Resolve_2: u.TalentFKRecoveryFree = true; break;
                case TalentNodeId.Investigator_Resolve_3: u.TalentPhobiaRollResist = true; break;
                case TalentNodeId.Investigator_Resolve_4: u.TalentFKRateHalved = true; break;
                case TalentNodeId.Investigator_Resolve_5: u.TalentDreadDamageReduction = 0.30f; break;

                // ── HERALD ─────────────────────────────────────────────────
                case TalentNodeId.Herald_Devotion_1: u.TalentHeraldPulseBonus = 5; break;
                case TalentNodeId.Herald_Devotion_2: u.TalentSecondHeraldPulse = true; break;
                case TalentNodeId.Herald_Devotion_3: u.TalentHealAmplify = 1.15f; break;
                case TalentNodeId.Herald_Devotion_4: u.TalentPulseGlobal = true; break;
                case TalentNodeId.Herald_Devotion_5: u.TalentDeathBlessing = true; break;
                case TalentNodeId.Herald_Endurance_1: u.MaxHP = Mathf.RoundToInt(u.MaxHP * 1.15f); u.CurrentHP = u.MaxHP; break;
                case TalentNodeId.Herald_Endurance_2: u.TalentComprehensionMod -= 0.1f; break;
                case TalentNodeId.Herald_Endurance_3: u.TalentAllyDeathSanityAmount = -8; break;
                case TalentNodeId.Herald_Endurance_4: u.TalentImmuneFirstAffliction = true; break;
                case TalentNodeId.Herald_Endurance_5: u.TalentPassiveDrainHalved = true; break;

                // ── OCCULTIST ──────────────────────────────────────────────
                case TalentNodeId.Occultist_Void_1:      u.TalentPsychicRecoilChance = 0.35f; break;
                case TalentNodeId.Occultist_Void_2:      u.TalentPsychicRecoilDamage = 8; break;
                case TalentNodeId.Occultist_Void_3:      u.TalentPsychicRecoilStun = 0.20f; break;
                case TalentNodeId.Occultist_Void_4:      u.TalentRecoilChain = true; break;
                case TalentNodeId.Occultist_Void_5:      u.TalentMaddeningAura = true; break;
                case TalentNodeId.Occultist_Endurance_1: u.MaxHP = Mathf.RoundToInt(u.MaxHP * 1.10f); u.CurrentHP = u.MaxHP; break;
                case TalentNodeId.Occultist_Endurance_2: u.TalentComprehensionMod -= 0.2f; break;
                case TalentNodeId.Occultist_Endurance_3: u.CurrentSanity = Mathf.Min(u.CurrentSanity + 5, u.MaxSanity); break;
                case TalentNodeId.Occultist_Endurance_4: u.TalentImmuneEldritchHit = true; break;
                case TalentNodeId.Occultist_Endurance_5: u.TalentFocusedDamageBonus = true; break;

                // ── VESSEL ─────────────────────────────────────────────────
                case TalentNodeId.Vessel_Eldritch_1: u.TalentDeathDeniedChance = 0.35f; break;
                case TalentNodeId.Vessel_Eldritch_2: u.TalentTwoDeathDenied = true; break;
                case TalentNodeId.Vessel_Eldritch_3: u.TalentDeathDeniedAllyBoost = true; break;
                case TalentNodeId.Vessel_Eldritch_4: u.TalentPassiveDrainHalved = true; break;
                case TalentNodeId.Vessel_Eldritch_5: u.TalentDeathBurst = true; break;
                case TalentNodeId.Vessel_Presence_1: u.MaxSanity += 20; u.CurrentSanity = Mathf.Min(u.CurrentSanity + 20, u.MaxSanity); break;
                case TalentNodeId.Vessel_Presence_2: u.TalentComprehensionMod -= 0.2f; break;
                case TalentNodeId.Vessel_Presence_3: u.TalentDrainStartsLate = true; break;
                case TalentNodeId.Vessel_Presence_4: u.TalentSustainingAura = true; break;
                case TalentNodeId.Vessel_Presence_5: u.TalentEldritchAnchor = true; break;
            }
        }

        // ────────────────────────────────────────────────────────────────────
        // UI helpers — used by TalentTreePanel
        // ────────────────────────────────────────────────────────────────────

        public static string GetNodeName(TalentNodeId id) => id.ToString().Replace("_", " ");

        public static string GetNodeDescription(TalentNodeId id) => id switch
        {
            // WARDEN
            TalentNodeId.Warden_Resolve_1   => "+10 max sanity.",
            TalentNodeId.Warden_Resolve_2   => "Comprehension −0.1 (suffer slightly less from horror).",
            TalentNodeId.Warden_Resolve_3   => "Immune to the first Affliction roll per battle.",
            TalentNodeId.Warden_Resolve_4   => "Ally-death sanity loss halved (−7 instead of −15).",
            TalentNodeId.Warden_Resolve_5   => "Aura: nearby allies take 25% less Horror Rating drain.",
            TalentNodeId.Warden_Frontline_1 => "+15% max HP.",
            TalentNodeId.Warden_Frontline_2 => "+10% attack damage.",
            TalentNodeId.Warden_Frontline_3 => "Armour absorbs first 5 sanity damage per battle.",
            TalentNodeId.Warden_Frontline_4 => "Taunt: enemies focus this unit for 5 s at battle start.",
            TalentNodeId.Warden_Frontline_5 => "Heroic Sacrifice: on death allies gain +10 sanity instead of −15.",
            // MARKSMAN
            TalentNodeId.Marksman_Precision_1 => "+1 attack range.",
            TalentNodeId.Marksman_Precision_2 => "Attack cooldown −15% (faster shots).",
            TalentNodeId.Marksman_Precision_3 => "+5 sanity on every kill.",
            TalentNodeId.Marksman_Precision_4 => "25% chance to ignore target armour.",
            TalentNodeId.Marksman_Precision_5 => "Execute: auto-kill targets below 15% HP (once per battle).",
            TalentNodeId.Marksman_Endurance_1 => "+10% max HP.",
            TalentNodeId.Marksman_Endurance_2 => "Comprehension −0.15.",
            TalentNodeId.Marksman_Endurance_3 => "+5 max sanity.",
            TalentNodeId.Marksman_Endurance_4 => "Fatigue accrual −25% after expeditions.",
            TalentNodeId.Marksman_Endurance_5 => "Auto-analyse nearest enemy at battle start (free Insight Flash).",
            // SHADOW
            TalentNodeId.Shadow_Hunter_1   => "+15% move speed.",
            TalentNodeId.Shadow_Hunter_2   => "Vanish recharges after 8 s — can trigger again same battle.",
            TalentNodeId.Shadow_Hunter_3   => "Vanish threshold raised to 50% HP.",
            TalentNodeId.Shadow_Hunter_4   => "Silent Strike: attacking doesn't trigger EldritchHit on targets.",
            TalentNodeId.Shadow_Hunter_5   => "Backstab: +30% damage when flanking.",
            TalentNodeId.Shadow_Survivor_1 => "+10 max sanity.",
            TalentNodeId.Shadow_Survivor_2 => "Immune to Solitude Phobia.",
            TalentNodeId.Shadow_Survivor_3 => "+4 sanity on every kill.",
            TalentNodeId.Shadow_Survivor_4 => "Comprehension −0.1.",
            TalentNodeId.Shadow_Survivor_5 => "Second Vanish: two Vanish uses per battle.",
            // BERSERKER
            TalentNodeId.Berserker_Bloodlust_1 => "Blood Rage cap raised to 8 stacks.",
            TalentNodeId.Berserker_Bloodlust_2 => "+3 sanity on every kill.",
            TalentNodeId.Berserker_Bloodlust_3 => "Frenzy: below 30% HP → +25% damage.",
            TalentNodeId.Berserker_Bloodlust_4 => "Enter battle with 2 Blood Rage stacks already active.",
            TalentNodeId.Berserker_Bloodlust_5 => "Blood Rage stacks persist between battles.",
            TalentNodeId.Berserker_Toughness_1 => "+20% max HP.",
            TalentNodeId.Berserker_Toughness_2 => "+3 armour.",
            TalentNodeId.Berserker_Toughness_3 => "Ally-death sanity loss only −5.",
            TalentNodeId.Berserker_Toughness_4 => "15% chance to ignore an incoming hit entirely.",
            TalentNodeId.Berserker_Toughness_5 => "Undying Rage: survive one lethal hit at 1 HP per battle.",
            // INVESTIGATOR
            TalentNodeId.Investigator_Scholar_1 => "FK accumulation cap +10 before max sanity consequences worsen.",
            TalentNodeId.Investigator_Scholar_2 => "Insight Flash chance 30% (from 20%).",
            TalentNodeId.Investigator_Scholar_3 => "Comprehension −0.15 when in study stance.",
            TalentNodeId.Investigator_Scholar_4 => "Team shares revealed enemy weakness bonus.",
            TalentNodeId.Investigator_Scholar_5 => "Mental Fortress: immune to Dread Contest hesitation lock.",
            TalentNodeId.Investigator_Resolve_1 => "+15 max sanity.",
            TalentNodeId.Investigator_Resolve_2 => "Full Rest FK recovery costs 0 gold.",
            TalentNodeId.Investigator_Resolve_3 => "First phobia roll per battle has 50% chance to fail.",
            TalentNodeId.Investigator_Resolve_4 => "FK accumulation rate halved (1 per scan instead of 2).",
            TalentNodeId.Investigator_Resolve_5 => "Psychic Shield: Dread Contest damage −30%.",
            // HERALD
            TalentNodeId.Herald_Devotion_1 => "Martyrdom Pulse heals +5 extra sanity to allies.",
            TalentNodeId.Herald_Devotion_2 => "Second Pulse: Martyrdom Pulse can fire twice per battle.",
            TalentNodeId.Herald_Devotion_3 => "All ally heals from any source +15%.",
            TalentNodeId.Herald_Devotion_4 => "Pulse range doubles (global reach).",
            TalentNodeId.Herald_Devotion_5 => "Death Blessing: on death the entire team gains +20 sanity.",
            TalentNodeId.Herald_Endurance_1 => "+15% max HP.",
            TalentNodeId.Herald_Endurance_2 => "Comprehension 1.0 (reduced from 1.1).",
            TalentNodeId.Herald_Endurance_3 => "Ally-death sanity loss −8 (instead of −15).",
            TalentNodeId.Herald_Endurance_4 => "Immune to first Affliction roll per battle.",
            TalentNodeId.Herald_Endurance_5 => "Passive Drain −50%: Herald's presence calms nearby allies.",
            // OCCULTIST
            TalentNodeId.Occultist_Void_1      => "Psychic Recoil chance 35% (from 25%).",
            TalentNodeId.Occultist_Void_2      => "Psychic Recoil deals −8 sanity (from −5).",
            TalentNodeId.Occultist_Void_3      => "Psychic Recoil has 20% chance to stun attacker for 1 tick.",
            TalentNodeId.Occultist_Void_4      => "Recoil chains to nearest enemy of attacker for half damage.",
            TalentNodeId.Occultist_Void_5      => "Maddening Aura: enemies within 3 units lose 2 sanity per 5 s.",
            TalentNodeId.Occultist_Endurance_1 => "+10% max HP.",
            TalentNodeId.Occultist_Endurance_2 => "Comprehension 1.1 (from 1.3).",
            TalentNodeId.Occultist_Endurance_3 => "+5 sanity at battle start.",
            TalentNodeId.Occultist_Endurance_4 => "Immune to EldritchHit sanity drain when defending.",
            TalentNodeId.Occultist_Endurance_5 => "Focus: sanity above 70 grants +10% damage.",
            // VESSEL
            TalentNodeId.Vessel_Eldritch_1 => "Death Denied chance 35% (from 20%).",
            TalentNodeId.Vessel_Eldritch_2 => "Two Death Denied uses per battle.",
            TalentNodeId.Vessel_Eldritch_3 => "On Death Denied: nearby allies gain +8 sanity.",
            TalentNodeId.Vessel_Eldritch_4 => "Passive sanity drain halved.",
            TalentNodeId.Vessel_Eldritch_5 => "Death Burst: on death deal 30 damage to nearest enemy.",
            TalentNodeId.Vessel_Presence_1 => "+20 max sanity.",
            TalentNodeId.Vessel_Presence_2 => "Comprehension 0.8 (from 1.0).",
            TalentNodeId.Vessel_Presence_3 => "Drain doesn't start until battle round 4.",
            TalentNodeId.Vessel_Presence_4 => "Sustaining Aura: allies within 2 units regenerate 1 HP per 2 s.",
            TalentNodeId.Vessel_Presence_5 => "Eldritch Anchor: reduces all enemy Horror Rating drain by 1.",
            _ => "No description."
        };

        /// <summary>Returns branch index: 0 = Resolve/Psychological, 1 = Combat/Power.</summary>
        public static int GetBranch(TalentNodeId id)
        {
            string s = id.ToString();
            // Branch 1 keywords
            if (s.Contains("Frontline") || s.Contains("Precision") || s.Contains("Hunter") ||
                s.Contains("Bloodlust") || s.Contains("Scholar")   || s.Contains("Devotion") ||
                s.Contains("Void")      || s.Contains("Eldritch"))
                return 1;
            return 0;
        }

        public static string GetBranchName(string unitType, int branch)
        {
            return unitType.ToLowerInvariant() switch
            {
                "warden"       => branch == 0 ? "Resolve"    : "Frontline",
                "marksman"     => branch == 0 ? "Endurance"  : "Precision",
                "shadow"       => branch == 0 ? "Survivor"   : "Hunter",
                "berserker"    => branch == 0 ? "Toughness"  : "Bloodlust",
                "investigator" => branch == 0 ? "Resolve"    : "Scholar",
                "herald"       => branch == 0 ? "Endurance"  : "Devotion",
                "occultist"    => branch == 0 ? "Endurance"  : "Void",
                "vessel"       => branch == 0 ? "Presence"   : "Eldritch",
                _              => branch == 0 ? "Branch A"   : "Branch B"
            };
        }

        /// <summary>Returns all 10 talent nodes for a given unit type, ordered branch-0 (5) then branch-1 (5).</summary>
        public static List<TalentNodeId> GetNodesForClass(string unitType)
        {
            return unitType.ToLowerInvariant() switch
            {
                "warden" => new List<TalentNodeId>
                {
                    TalentNodeId.Warden_Resolve_1,   TalentNodeId.Warden_Resolve_2,
                    TalentNodeId.Warden_Resolve_3,   TalentNodeId.Warden_Resolve_4,   TalentNodeId.Warden_Resolve_5,
                    TalentNodeId.Warden_Frontline_1, TalentNodeId.Warden_Frontline_2,
                    TalentNodeId.Warden_Frontline_3, TalentNodeId.Warden_Frontline_4, TalentNodeId.Warden_Frontline_5
                },
                "marksman" => new List<TalentNodeId>
                {
                    TalentNodeId.Marksman_Endurance_1, TalentNodeId.Marksman_Endurance_2,
                    TalentNodeId.Marksman_Endurance_3, TalentNodeId.Marksman_Endurance_4, TalentNodeId.Marksman_Endurance_5,
                    TalentNodeId.Marksman_Precision_1, TalentNodeId.Marksman_Precision_2,
                    TalentNodeId.Marksman_Precision_3, TalentNodeId.Marksman_Precision_4, TalentNodeId.Marksman_Precision_5
                },
                "shadow" => new List<TalentNodeId>
                {
                    TalentNodeId.Shadow_Survivor_1, TalentNodeId.Shadow_Survivor_2,
                    TalentNodeId.Shadow_Survivor_3, TalentNodeId.Shadow_Survivor_4, TalentNodeId.Shadow_Survivor_5,
                    TalentNodeId.Shadow_Hunter_1,   TalentNodeId.Shadow_Hunter_2,
                    TalentNodeId.Shadow_Hunter_3,   TalentNodeId.Shadow_Hunter_4,   TalentNodeId.Shadow_Hunter_5
                },
                "berserker" => new List<TalentNodeId>
                {
                    TalentNodeId.Berserker_Toughness_1, TalentNodeId.Berserker_Toughness_2,
                    TalentNodeId.Berserker_Toughness_3, TalentNodeId.Berserker_Toughness_4, TalentNodeId.Berserker_Toughness_5,
                    TalentNodeId.Berserker_Bloodlust_1, TalentNodeId.Berserker_Bloodlust_2,
                    TalentNodeId.Berserker_Bloodlust_3, TalentNodeId.Berserker_Bloodlust_4, TalentNodeId.Berserker_Bloodlust_5
                },
                "investigator" => new List<TalentNodeId>
                {
                    TalentNodeId.Investigator_Resolve_1, TalentNodeId.Investigator_Resolve_2,
                    TalentNodeId.Investigator_Resolve_3, TalentNodeId.Investigator_Resolve_4, TalentNodeId.Investigator_Resolve_5,
                    TalentNodeId.Investigator_Scholar_1, TalentNodeId.Investigator_Scholar_2,
                    TalentNodeId.Investigator_Scholar_3, TalentNodeId.Investigator_Scholar_4, TalentNodeId.Investigator_Scholar_5
                },
                "herald" => new List<TalentNodeId>
                {
                    TalentNodeId.Herald_Endurance_1, TalentNodeId.Herald_Endurance_2,
                    TalentNodeId.Herald_Endurance_3, TalentNodeId.Herald_Endurance_4, TalentNodeId.Herald_Endurance_5,
                    TalentNodeId.Herald_Devotion_1,  TalentNodeId.Herald_Devotion_2,
                    TalentNodeId.Herald_Devotion_3,  TalentNodeId.Herald_Devotion_4,  TalentNodeId.Herald_Devotion_5
                },
                "occultist" => new List<TalentNodeId>
                {
                    TalentNodeId.Occultist_Endurance_1, TalentNodeId.Occultist_Endurance_2,
                    TalentNodeId.Occultist_Endurance_3, TalentNodeId.Occultist_Endurance_4, TalentNodeId.Occultist_Endurance_5,
                    TalentNodeId.Occultist_Void_1,      TalentNodeId.Occultist_Void_2,
                    TalentNodeId.Occultist_Void_3,      TalentNodeId.Occultist_Void_4,      TalentNodeId.Occultist_Void_5
                },
                "vessel" => new List<TalentNodeId>
                {
                    TalentNodeId.Vessel_Presence_1, TalentNodeId.Vessel_Presence_2,
                    TalentNodeId.Vessel_Presence_3, TalentNodeId.Vessel_Presence_4, TalentNodeId.Vessel_Presence_5,
                    TalentNodeId.Vessel_Eldritch_1, TalentNodeId.Vessel_Eldritch_2,
                    TalentNodeId.Vessel_Eldritch_3, TalentNodeId.Vessel_Eldritch_4, TalentNodeId.Vessel_Eldritch_5
                },
                _ => new List<TalentNodeId>()
            };
        }
    }
}
