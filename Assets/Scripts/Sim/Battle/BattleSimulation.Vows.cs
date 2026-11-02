using System;
using System.Collections.Generic;

namespace KindredSiege.Sim.Battle
{
    /// The Whisperer's vows in the fight (campaign rules 33; FightRules.Vows; Jacob in chat, 2 Nov 2026: "whisperer - 1"). A
    /// fighter of rank 2 or more brings his vow in his side's PalKit (StarVow, VowRules); rank 2 plays it at tier 1, rank 3 at
    /// its final form. A vow adds to rank 2's spell once more; it never replaces it. Every number is a starting value.
    public sealed partial class BattleSimulation
    {
        private readonly bool _vowsOn;

        // A Netter's trap catching more is itself a snare; this keeps that from catching more again. Never set between ticks.
        private bool _netting;

        /// Whether this fight plays the vows: campaign rules 33.
        public bool VowsOn => _vowsOn;

        /// This fighter's vow in this fight: None below rank 2, outside rules 33, or for a vow not of his class.
        public StarVow VowOf(BattleUnit unit)
        {
            if (!_vowsOn || unit == null || unit.Side < 0 || unit.Side > 1 || FighterRank(unit) < 2) return StarVow.None;
            StarVow vow = KitOf(unit.Side).VowOf(unit.Id);
            return VowRules.Valid(vow) && VowRules.ClassOf(vow) == unit.Class ? vow : StarVow.None;
        }

        private bool Final(BattleUnit unit) => FighterRank(unit) >= 3;

        private void VowEvent(BattleUnit by, BattleUnit on, int amount, string detail)
        {
            Emit(EventFor(BattleEventKind.Whisper, by, on, amount, detail));
        }

        // ------------------------------------------------------------ the Warden

        // Bastion: his Guard also covers whoever stands beside his ward, and at its final form his ward's whole row.
        private bool BastionCovers(BattleUnit warden, BattleUnit target)
        {
            if (VowOf(warden) != StarVow.Bastion) return false;
            BattleUnit ward = FindById(warden.GuardTargetId);
            if (ward == null || !ward.Alive || ward.Side != target.Side || ReferenceEquals(ward, target)) return false;
            if (Final(warden)) return target.Row == ward.Row;
            List<BattleUnit> beside = BrandedAndBeside(ward);
            for (int k = 1; k < beside.Count; k++) if (ReferenceEquals(beside[k], target)) return true;
            return false;
        }

        // Reprisal: a blow his Guard took, he hits back, and at its final form leaves an Opening.
        private void Reprisal(BattleUnit warden, BattleUnit attacker)
        {
            if (VowOf(warden) != StarVow.Reprisal || _finished || !warden.Alive || attacker == null || !attacker.Alive) return;
            bool final = Final(warden);
            int percent = final ? VowRules.ReprisalFinalPercent : VowRules.ReprisalPercent;
            VowEvent(warden, attacker, percent, VowRules.ReprisalDetail);
            VowHit(warden, attacker, warden.Power, percent, VowRules.ReprisalDetail);
            if (final && !_finished && attacker.Alive) Open(warden, attacker);
        }

        // ------------------------------------------------------------ the Marksman

        // After his shot's blows: Sniper's goes through to the first enemy behind, Ranger's bounces on.
        private void ShotVows(BattleUnit unit, BattleUnit target)
        {
            StarVow vow = VowOf(unit);
            if ((vow != StarVow.Sniper && vow != StarVow.Ranger) || _finished || !unit.Alive) return;
            bool final = Final(unit);
            int weight = SpellWeight(unit, BlowKind.AimedShot, unit.Power);
            if (vow == StarVow.Sniper)
            {
                if (target.Row != Row.Front) return;
                BattleUnit behind = null;
                for (int i = 0; i < _units.Count && behind == null; i++)
                {
                    BattleUnit u = _units[i];
                    if (u.Alive && u.Side == target.Side && u.Row == Row.Back && !u.Benched && !LeaderProtected(u)) behind = u;
                }
                if (behind == null) return;
                int percent = final ? VowRules.SniperFinalPercent : VowRules.SniperPercent;
                VowEvent(unit, behind, percent, VowRules.SniperDetail);
                VowHit(unit, behind, weight, percent, VowRules.SniperDetail);
                return;
            }
            int bounces = final ? VowRules.RangerFinalBounces : VowRules.RangerBounces;
            int each = final ? VowRules.RangerFinalPercent : VowRules.RangerPercent;
            List<BattleUnit> next = Nearest(target, bounces);
            VowEvent(unit, target, next.Count, VowRules.RangerDetail);
            for (int k = 0; k < next.Count && !_finished; k++) VowHit(unit, next[k], weight, each, VowRules.RangerDetail);
        }

        // Up to count other standing enemies of his side, those beside him first, then in setup order.
        private List<BattleUnit> Nearest(BattleUnit from, int count)
        {
            var near = new List<BattleUnit>();
            List<BattleUnit> beside = BrandedAndBeside(from);
            for (int k = 1; k < beside.Count && near.Count < count; k++)
                if (beside[k].Alive && beside[k].Side == from.Side && !LeaderProtected(beside[k])) near.Add(beside[k]);
            for (int i = 0; i < _units.Count && near.Count < count; i++)
            {
                BattleUnit u = _units[i];
                if (u.Alive && u.Side == from.Side && !u.Benched && !ReferenceEquals(u, from) && !near.Contains(u) && !LeaderProtected(u)) near.Add(u);
            }
            return near;
        }

        // ------------------------------------------------------------ the Skirmisher

        // Reaver: a strike that killed gives her strike back, and at its final form she strikes again at once, once.
        private void Reaved(BattleUnit unit, BattleUnit target, bool wasAlive)
        {
            if (VowOf(unit) != StarVow.Reaver || !wasAlive || target.Alive || _finished || !unit.Alive) return;
            unit.SpellTicks = 0;
            VowEvent(unit, target, 0, VowRules.ReaverDetail);
            if (!Final(unit)) return;
            BattleUnit next = SpellTarget(unit, false);
            if (next != null) Blow(unit, next, unit.Power, BlowKind.Strike);
        }

        // Duelist: her strike on her side's Focus Fire target, which never misses and crits.
        private bool DuelistSure(BattleUnit attacker, BattleUnit target, BlowKind kind)
        {
            if (!_vowsOn || kind != BlowKind.Strike || VowOf(attacker) != StarVow.Duelist) return false;
            SideState mine = _sides[attacker.Side];
            return mine.FocusTicks > 0 && string.Equals(mine.FocusTargetId, target.Id, StringComparison.Ordinal);
        }

        private int DuelistPercent(BattleUnit attacker) => Final(attacker) ? VowRules.DuelistFinalPercent : VowRules.DuelistPercent;

        // ------------------------------------------------------------ the Trapper

        // Netter: her trap catches more beside the one who sprang it.
        private void Netted(BattleUnit trapper, int side, BattleUnit victim)
        {
            if (_netting || trapper == null || VowOf(trapper) != StarVow.Netter || _finished) return;
            int more = Final(trapper) ? VowRules.NetterFinalMore : VowRules.NetterMore;
            List<BattleUnit> beside = BrandedAndBeside(victim);
            var caught = new List<BattleUnit>();
            for (int k = 1; k < beside.Count && caught.Count < more; k++)
                if (beside[k].Alive && beside[k].Side == victim.Side) caught.Add(beside[k]);
            if (caught.Count == 0) return;
            VowEvent(trapper, victim, caught.Count, VowRules.NetterDetail);
            _netting = true;
            for (int k = 0; k < caught.Count && !_finished; k++) Snare(trapper, side, caught[k]);
            _netting = false;
        }

        // Bleeder: a bleeding enemy fell; his wound jumps to the next one beside him, and at its final form to all of them.
        private void BledOn(int killerSide, BattleUnit victim)
        {
            if (killerSide < 0 || killerSide > 1 || victim.Side == killerSide || !victim.Statuses[StatusKind.Bleed].On) return;
            BattleUnit bleeder = null;
            for (int i = 0; i < _units.Count && bleeder == null; i++)
                if (_units[i].Alive && _units[i].Side == killerSide && VowOf(_units[i]) == StarVow.Bleeder) bleeder = _units[i];
            if (bleeder == null) return;
            Status wound = victim.Statuses[StatusKind.Bleed];
            int ticks = wound.Ticks, size = wound.Size;
            bool final = Final(bleeder);
            List<BattleUnit> beside = BrandedAndBeside(victim);
            int spread = 0;
            for (int k = 1; k < beside.Count && (final || spread == 0); k++)
            {
                BattleUnit u = beside[k];
                if (!u.Alive || u.Side != victim.Side) continue;
                u.Statuses.Apply(StatusKind.Bleed, ticks, size, keepLonger: true);
                MarkEvent(killerSide, bleeder, u, ticks, ClassSpells.BleedMark);
                spread++;
            }
            if (spread > 0) VowEvent(bleeder, victim, spread, VowRules.BleederDetail);
        }

        // ------------------------------------------------------------ the Medic

        // Hierophant: her treatment splashes onto the allies beside the patient.
        private void Splashed(BattleUnit medic, BattleUnit patient, int amount)
        {
            if (amount <= 0 || VowOf(medic) != StarVow.Hierophant || Stopped) return;
            int splash = amount * (Final(medic) ? VowRules.HierophantFinalPercent : VowRules.HierophantPercent) / 100;
            if (splash <= 0) return;
            List<BattleUnit> beside = BrandedAndBeside(patient);
            int healed = 0;
            for (int k = 1; k < beside.Count && !Stopped; k++)
            {
                BattleUnit u = beside[k];
                if (!u.Alive || u.Side != medic.Side || u.Hp >= u.MaxHp || u.EvacTicks > 0 || (_beastOn && IsHeld(u))) continue;
                Heal(medic, u, splash, VowRules.HierophantDetail);
                healed++;
            }
            if (healed > 0) VowEvent(medic, patient, healed, VowRules.HierophantDetail);
        }

        // Martyr: how many percent bigger her heals are, more the less health she has.
        private int MartyrPercent(BattleUnit healer)
        {
            if (healer == null || VowOf(healer) != StarVow.Martyr) return 0;
            int most = Final(healer) ? VowRules.MartyrFinalPercent : VowRules.MartyrPercent;
            return most * Math.Max(0, 1000 - healer.HpPermille) / 1000;
        }

        // ------------------------------------------------------------ a vow's own hit

        // A hit of this percent of a weight, through his armour, as the vow's.
        private void VowHit(BattleUnit by, BattleUnit target, int weight, int percent, string detail)
        {
            if (_finished || !target.Alive || weight <= 0 || percent <= 0) return;
            if (_layersOn) ApplyHit(by, by.Side, target, Armor(target, Hit.Percent(weight, percent), by), detail);
            else ApplyDamage(by, by.Side, target, Armored(target, Math.Max(1, weight * percent / 100)), detail);
        }
    }
}
