using System;
using System.Collections.Generic;
using System.Linq;

namespace Lumia
{
    public sealed partial class GameEngine
    {
        // Each bit describes a once-per-turn trigger. These fields live in CombatState so a
        // save cannot reset a consumed trigger, including during the enemy's paced actions.
        const int AugBasic = 1, AugGuard = 2, AugWeapon = 4, AugTactical = 8,
            AugReserve = 16, AugQwe = 32, AugUltimate = 64, AugDebuff = 128,
            AugWeaponBasic = 256, AugHeavy = 512, AugArsenal = 1024,
            AugDebuffUltimate = 2048, AugShield = 4096, AugAllIn = 8192;

        public IReadOnlyList<EndlessAugmentDef> AugmentOffers
        {
            get { return (State.augmentOffers ?? new List<string>()).Select(EndlessAugments.Find).Where(x => x != null).ToList().AsReadOnly(); }
        }
        public string SelectedAugmentId { get { return State.augmentSelectedId; } }
        public int AugmentCount { get { return State.augments == null ? 0 : State.augments.Count; } }
        public int AugmentStacks(string id) { return State.augments == null ? 0 : State.augments.Count(x => x == id); }
        public int SelectedEquipmentSlotIndex { get { return State.equipmentSlotSelected; } }
        public int EquipmentExpansionCycle { get { return State.equipmentExpansionCycle; } }

        public int EquipmentCapacity(GearSlot slot)
        {
            int index = (int)slot;
            if (!Enum.IsDefined(typeof(GearSlot), slot)) return 0;
            int bonus = State.mode == RunMode.Endless && State.gearSlotBonuses != null && index < State.gearSlotBonuses.Count
                ? Math.Max(0, Math.Min(int.MaxValue - 2, State.gearSlotBonuses[index])) : 0;
            return 2 + bonus;
        }

        void EnsureEquipmentExpansion()
        {
            int count = Enum.GetValues(typeof(GearSlot)).Length;
            if (State.gearSlotBonuses == null) State.gearSlotBonuses = new List<int>();
            State.gearSlotBonuses = Enumerable.Range(0, count).Select(index => index < State.gearSlotBonuses.Count
                ? Math.Max(0, Math.Min(int.MaxValue - 2, State.gearSlotBonuses[index])) : 0).ToList();
            if (State.mode != RunMode.Endless)
            {
                State.gearSlotBonuses = Enumerable.Repeat(0, count).ToList();
                if (State.stage == RunStage.EquipmentSlotChoice) State.stage = RunStage.Map;
            }
            if (State.mode == RunMode.Endless && State.stage == RunStage.EquipmentSlotChoice)
            {
                State.equipmentExpansionCycle = State.endlessCycle + 1;
                if (State.equipmentSlotSelected < 0 || State.equipmentSlotSelected >= count) State.equipmentSlotSelected = -1;
            }
            else State.equipmentSlotSelected = State.equipmentExpansionCycle = -1;
        }

        void BeginEquipmentSlotChoice()
        {
            State.stage = RunStage.EquipmentSlotChoice;
            State.equipmentSlotSelected = -1; State.equipmentExpansionCycle = State.endlessCycle + 1;
            Say("3막 완료 · 장착 한도를 1 늘릴 장비 부위를 선택하세요. 확정하면 다음 무한 주기가 시작됩니다.");
        }

        public bool ToggleEquipmentSlot(GearSlot slot)
        {
            if (State.mode != RunMode.Endless || State.stage != RunStage.EquipmentSlotChoice || !Enum.IsDefined(typeof(GearSlot), slot)) return false;
            State.equipmentSlotSelected = State.equipmentSlotSelected == (int)slot ? -1 : (int)slot;
            return Say(State.equipmentSlotSelected < 0 ? "장비 부위 선택을 취소했습니다." : "장비 부위를 선택했습니다. 확정 전까지 변경할 수 있습니다.");
        }

        public bool ConfirmEquipmentSlot()
        {
            if (State.mode != RunMode.Endless || State.stage != RunStage.EquipmentSlotChoice
                || State.equipmentExpansionCycle != State.endlessCycle + 1
                || State.equipmentSlotSelected < 0 || State.equipmentSlotSelected >= Enum.GetValues(typeof(GearSlot)).Length) return false;
            int index = State.equipmentSlotSelected;
            if (State.gearSlotBonuses == null || index >= State.gearSlotBonuses.Count || State.gearSlotBonuses[index] >= int.MaxValue - 2) return false;
            State.gearSlotBonuses[index]++;
            State.equipmentSlotSelected = State.equipmentExpansionCycle = -1;
            BeginNextEndlessCycle();
            return true;
        }

        int AugmentAmount(string effect)
        {
            if (State.mode != RunMode.Endless || State.augments == null) return 0;
            return State.augments.Select(EndlessAugments.Find).Where(x => x != null && x.effect == effect && !EndlessAugments.IsSupport(x))
                .GroupBy(x => x.id).Sum(g => g.First().amount * Math.Min(g.First().maxStacks, g.Count()));
        }
        bool AugmentEligible(EndlessAugmentDef value)
        {
            return value != null && (EndlessAugments.IsSupport(value) || AugmentStacks(value.id) < value.maxStacks);
        }
        void EnsureEndlessAugments()
        {
            EnsureEquipmentExpansion();
            if (State.augments == null) State.augments = new List<string>();
            if (State.augmentOffers == null) State.augmentOffers = new List<string>();
            if (State.mode != RunMode.Endless)
            {
                State.augments.Clear(); State.augmentOffers.Clear(); State.augmentSelectedId = null;
                State.augmentOfferingCycle = -1;
                if (State.stage == RunStage.AugmentChoice) State.stage = RunStage.Map;
                return;
            }
            var valid = new List<string>();
            foreach (var id in State.augments)
            {
                var value = EndlessAugments.Find(id);
                if (value != null && (EndlessAugments.IsSupport(value) || valid.Count(x => x == id) < value.maxStacks)) valid.Add(id);
            }
            State.augments = valid;
            if (State.stage == RunStage.AugmentChoice)
            {
                State.augmentOfferingCycle = State.endlessCycle + 1;
                bool validOffers = State.augmentOffers.Count == 3 && State.augmentOffers.Distinct().Count() == 3
                    && State.augmentOffers.All(id => AugmentEligible(EndlessAugments.Find(id)))
                    && State.augmentOffers.Select(id => EndlessAugments.Find(id).tier).Distinct().Count() == 1;
                if (!validOffers) GenerateAugmentOffers();
                if (!State.augmentOffers.Contains(State.augmentSelectedId)) State.augmentSelectedId = null;
            }
            else { State.augmentOffers.Clear(); State.augmentSelectedId = null; }
        }
        void BeginAugmentChoice()
        {
            State.stage = RunStage.AugmentChoice; State.augmentOfferingCycle = State.endlessCycle + 1;
            State.augmentSelectedId = null; GenerateAugmentOffers();
            Say("3막 완료 · " + EndlessAugments.TierName(AugmentOffers[0].tier) + " 증강 3개 중 하나를 선택하세요. 다음으로 장착 한도를 늘릴 장비 부위를 선택합니다.");
        }
        void GenerateAugmentOffers()
        {
            // A dedicated derived roll keeps inspecting/cancelling choices and migrating old
            // saves independent of the combat, encounter and next-map random stream.
            int random = unchecked(State.seed ^ State.augmentOfferingCycle * 1103515245 ^ (int)0x79A6B4C3);
            if (random == 0) random = 1831565813;
            var tier = EndlessAugments.TierForOffering(State.augmentOfferingCycle, AugmentNext(ref random, 100));
            var pool = EndlessAugments.All.Where(x => x.tier == tier && AugmentEligible(x)).Select(x => x.id).ToList();
            for (int i = pool.Count - 1; i > 0; --i)
            {
                int j = AugmentNext(ref random, i + 1); var id = pool[i]; pool[i] = pool[j]; pool[j] = id;
            }
            State.augmentOffers = pool.Take(3).ToList();
        }
        static int AugmentNext(ref int random, int maximum)
        {
            uint value = unchecked((uint)random); value ^= value << 13; value ^= value >> 17; value ^= value << 5;
            random = unchecked((int)value); return (int)(value % (uint)Math.Max(1, maximum));
        }
        public bool ToggleAugment(string id)
        {
            if (State.mode != RunMode.Endless || State.stage != RunStage.AugmentChoice || !State.augmentOffers.Contains(id)
                || !AugmentEligible(EndlessAugments.Find(id))) return false;
            State.augmentSelectedId = State.augmentSelectedId == id ? null : id;
            return Say(State.augmentSelectedId == null ? "증강 선택을 취소했습니다." : EndlessAugments.Find(id).name + " 선택 · 확정 전까지 변경할 수 있습니다.");
        }
        public bool ConfirmAugment()
        {
            if (State.mode != RunMode.Endless || State.stage != RunStage.AugmentChoice || !State.augmentOffers.Contains(State.augmentSelectedId)) return false;
            var value = EndlessAugments.Find(State.augmentSelectedId);
            if (!AugmentEligible(value)) return false;
            State.augments.Add(value.id);
            switch (value.effect)
            {
                case "support_credits": State.credits = (int)Math.Min(int.MaxValue, (long)State.credits + value.amount); break;
                case "support_heal": Heal((int)((long)State.maxHp * value.amount / 100)); break;
                case "support_max_health":
                    State.maxHealthBonus = (int)Math.Min(int.MaxValue, (long)State.maxHealthBonus + value.amount);
                    RecalculateHealth(false); break;
            }
            State.augmentOffers.Clear(); State.augmentSelectedId = null;
            BeginEquipmentSlotChoice(); Say(value.name + " 획득 · 장착 한도를 1 늘릴 장비 부위를 선택하세요.");
            return true;
        }
        public bool SkipAugment()
        {
            if (State.mode != RunMode.Endless || State.stage != RunStage.AugmentChoice) return false;
            State.augmentOffers.Clear(); State.augmentSelectedId = null;
            BeginEquipmentSlotChoice(); return Say("증강을 건너뛰었습니다. 장착 한도를 1 늘릴 장비 부위를 선택하세요.");
        }

        bool AugmentReady(int flag) { return (State.combat.augmentTurnMask & flag) == 0; }
        bool TriggerAugment(int flag, string effect)
        {
            if (AugmentAmount(effect) <= 0 || !AugmentReady(flag)) return false;
            State.combat.augmentTurnMask |= flag; return true;
        }
        bool AugmentIsAttack(CardDef card)
        {
            return card != null && (card.damage > 0 || SkillMechanics.Bonuses(State.combat.playerSkills, card).damage > 0
                || TraitMechanics.EmpowerBonus(State.combat.playerTraits, card) > 0);
        }
        bool AugmentTargetDebuffed()
        {
            var c = State.combat;
            return c.enemyPoison > 0 || c.enemyWeak > 0 || c.enemyVulnerable > 0
                || c.enemyStatuses.effects.Any(x => !x.pending && x.remaining > 0)
                || c.enemyTraits.buffs.Any(x => x.remaining > 0 && x.amount > 0 && (x.kind == "heal_reduction" || x.kind == "exposure"))
                || c.playerSkills.effects.Concat(c.playerTraits.skills.effects).Any(x => x.remaining > 0 && x.amount > 0 && (x.kind == "bleed" || x.kind == "burn"));
        }
        int AugmentAttackBonus(CardDef card, int paidCost)
        {
            var c = State.combat;
            if (State.mode != RunMode.Endless || c == null || paidCost <= 0 || !AugmentIsAttack(card)) return 0;
            int bonus = 0;
            if (card.key == "D" && AugmentReady(AugWeapon)) bonus += AugmentAmount("first_weapon_damage");
            if (card.id == "basic_attack" && c.augmentWeaponUsed && AugmentReady(AugWeaponBasic)) bonus += AugmentAmount("weapon_basic_damage");
            if (AugmentTargetDebuffed())
            {
                if (AugmentReady(AugDebuff)) bonus += AugmentAmount("debuffed_damage");
                if (card.key == "R" && AugmentReady(AugDebuffUltimate)) bonus += AugmentAmount("debuffed_ultimate_damage");
            }
            if (c.block >= 15 && AugmentReady(AugShield)) bonus += AugmentAmount("shield_conversion");
            return bonus;
        }
        void ConsumeAugmentAttack(CardDef card, int paidCost)
        {
            var c = State.combat;
            if (State.mode != RunMode.Endless || paidCost <= 0 || !AugmentIsAttack(card)) return;
            if (card.key == "D") TriggerAugment(AugWeapon, "first_weapon_damage");
            if (card.id == "basic_attack" && c.augmentWeaponUsed) TriggerAugment(AugWeaponBasic, "weapon_basic_damage");
            if (AugmentTargetDebuffed())
            {
                TriggerAugment(AugDebuff, "debuffed_damage");
                if (card.key == "R") TriggerAugment(AugDebuffUltimate, "debuffed_ultimate_damage");
            }
            if (c.block >= 15 && TriggerAugment(AugShield, "shield_conversion")) c.block -= 5;
        }
        void AugmentBlock(int amount, CardDef source = null)
        {
            if (amount <= 0) return;
            State.combat.block += amount; RecordBossProgress(BossMechanics.ObserveBlock(State.combat, amount));
            EmitCombatAction(new CombatAction { id = ++actionSerial, cardId = source == null ? "basic_guard" : source.id, kind = "augment", block = amount });
        }
        void AugmentAfterCard(CardDef card, int price, int energyBefore, int cardBlock)
        {
            var c = State.combat;
            if (State.mode != RunMode.Endless || c == null || State.stage != RunStage.Combat) return;
            if (card.key == "D") c.augmentWeaponUsed = true;
            if (price <= 0) return;
            if (card.id == "basic_attack" && TriggerAugment(AugBasic, "first_basic_block")) AugmentBlock(AugmentAmount("first_basic_block"), card);
            if (cardBlock > 0 && TriggerAugment(AugGuard, "first_guard_draw")) DrawCards(AugmentAmount("first_guard_draw"));
            if (card.key == "F" && TriggerAugment(AugTactical, "first_tactical_block")) AugmentBlock(AugmentAmount("first_tactical_block"), card);
            if (card.key == "R" && TriggerAugment(AugUltimate, "first_ultimate_draw")) DrawCards(AugmentAmount("first_ultimate_draw"));
            int key = card.key == "Q" ? 1 : card.key == "W" ? 2 : card.key == "E" ? 4 : card.key == "R" ? 8 : 0;
            c.augmentKeyMask |= key;
            if ((c.augmentKeyMask & 7) == 7 && TriggerAugment(AugQwe, "qwe_energy")) c.energy += AugmentAmount("qwe_energy");
            if ((c.augmentKeyMask & 15) == 15 && TriggerAugment(AugArsenal, "arsenal_block")) AugmentBlock(AugmentAmount("arsenal_block"), card);
            if (price >= 4 && TriggerAugment(AugHeavy, "heavy_refund")) c.energy += Math.Min(price, AugmentAmount("heavy_refund"));
            if (price == energyBefore && TriggerAugment(AugAllIn, "all_in_block")) AugmentBlock(AugmentAmount("all_in_block"), card);
        }
        void ResetAugmentTurn()
        {
            State.combat.augmentTurnMask = State.combat.augmentKeyMask = 0; State.combat.augmentWeaponUsed = false;
        }
        void AugmentTurnStart(bool first)
        {
            if (State.mode != RunMode.Endless) return;
            var c = State.combat;
            if ((long)State.hp * 2 <= State.maxHp) AugmentBlock(AugmentAmount("low_health_block"));
            if (first && c.boss != null) c.energy += AugmentAmount("boss_opening_energy");
        }
        int AugmentOpeningDraw()
        {
            return State.mode == RunMode.Endless && State.deck.Count <= 18 ? AugmentAmount("small_deck_draw") : 0;
        }
        void AugmentTurnEnd()
        {
            if (State.mode == RunMode.Endless && State.combat.energy >= 2 && TriggerAugment(AugReserve, "reserve_block")) AugmentBlock(AugmentAmount("reserve_block"));
        }
    }
}
