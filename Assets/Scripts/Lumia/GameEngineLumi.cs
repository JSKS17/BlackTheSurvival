using System;
using System.Collections.Generic;
using System.Linq;

namespace Lumia
{
    public sealed partial class GameEngine
    {
        public const int LumiEncounterChance = 3;
        public const int LumiTurnLimit = 3;
        public const int LumiDamageThreshold = 40;
        public const int LumiCreditReward = 20;
        public bool IsLumiCombat => State.stage == RunStage.Combat && State.combat != null && State.combat.lumi;
        public bool LumiNearKiosk => IsNearKiosk(ActiveNode());
        public string LumiObjectId => State.lumiShopObjectId;
        public int LumiObjectPrice => Math.Max(0, (GameDatabase.Object(LumiObjectId)?.price ?? int.MaxValue) - 40);
        public bool CanUseTelescopicCamera => State.telescopicCameras > 0 && State.stage == RunStage.Map
            && !State.scoutedActs.Contains(State.act)
            && ScoutNodes().Any(n => (n.kind == ZoneKind.Subject || n.kind == ZoneKind.Wildlife) && !State.revealedNodeIds.Contains(n.id));
        public bool CanUsePortableKiosk => State.portableKiosks > 0 && !State.portableKioskActive
            && (State.stage == RunStage.Map || State.stage == RunStage.Campfire || State.stage == RunStage.Encounter)
            && !State.runeChangePending && !State.cardUpgradePending && !State.rareRemovalPending && !State.rareCopyPending
            && string.IsNullOrEmpty(State.pendingPassive) && pendingCardChoice == null;

        void NormalizeLumi()
        {
            State.telescopicCameras = Math.Max(0, State.telescopicCameras);
            State.portableKiosks = Math.Max(0, State.portableKiosks);
            State.lumiShopPurchases = (State.lumiShopPurchases ?? new List<string>())
                .Where(id => id == "telescope_camera" || id == "tactical_module" || id == "object" || id == "portable_kiosk").Distinct().ToList();
            if (State.stage != RunStage.Kiosk) State.portableKioskActive = false;
            if (State.portableKioskActive && State.portableKioskReturnStage != RunStage.Map
                && State.portableKioskReturnStage != RunStage.Campfire && State.portableKioskReturnStage != RunStage.Encounter)
            { State.portableKioskReturnStage = RunStage.Map; }
            if (State.stage == RunStage.LumiShop && GameDatabase.Object(State.lumiShopObjectId) == null)
                State.lumiShopObjectId = GameDatabase.Objects[Next(GameDatabase.Objects.Count)].id;
            if (IsLumiCombat)
            {
                State.combat.lumiDamage = Math.Max(0, State.combat.lumiDamage);
                State.combat.lumiCredits = Math.Max(0, State.combat.lumiCredits);
                State.combat.enemyHp = State.combat.enemyMaxHp = 1000;
            }
        }

        // The roll is committed to the reward before displaying the offer, never
        // repeated by a reload, a merchant transaction or a challenge completion.
        bool TryStartLumiAfterReward()
        {
            var reward = State.rewards;
            var node = ActiveNode();
            if (State.stage != RunStage.Rewards || reward == null || node == null
                || (node.kind != ZoneKind.Subject && node.kind != ZoneKind.Wildlife) || reward.boss) return false;
            // Older pending rewards predate this encounter and must not gain a
            // retroactive RNG roll merely because they were loaded after update.
            if (!reward.lumiRolled) { reward.lumiRolled = true; reward.lumiAppears = false; }
            if (!reward.lumiAppears) return false;
            reward.lumiAppears = false;
            State.stage = RunStage.LumiOffer;
            ResearchProgression.LumiFound(State);
            return Say("전투가 끝난 자리에 루미가 나타났습니다. 붙잡거나 그냥 보내 줄 수 있습니다.");
        }

        public bool CatchLumi()
        {
            if (State.stage != RunStage.LumiOffer) return Fail("지금은 루미를 붙잡을 수 없습니다.");
            if (IsNearKiosk(ActiveNode())) StartLumiCombat();
            else OpenLumiShop();
            return true;
        }

        public bool SendLumiAway()
        {
            if (State.stage != RunStage.LumiOffer) return false;
            Say("루미를 그냥 보내 주었습니다.");
            CompleteNode();
            return true;
        }

        void StartLumiCombat()
        {
            SyncWeaponCards();
            var combat = new CombatState {
                lumi = true, enemyId = "lumi", enemyName = "루미", enemyLevel = State.level,
                enemyHp = 1000, enemyMaxHp = 1000, strength = Modifier("battle_start_strength"), block = Modifier("battle_start_block")
            };
            State.combat = combat; State.stage = RunStage.Combat;
            combat.playerSkills = SkillMechanics.Ensure(combat.playerSkills);
            combat.enemySkills = SkillMechanics.Ensure(combat.enemySkills);
            combat.playerTraits = TraitMechanics.NewBattle(State.persistentTraits); State.persistentTraits = combat.playerTraits;
            combat.drawPile.AddRange(State.deck); Shuffle(combat.drawPile);
            Heal(Modifier("battle_start_heal"));
            RecordCombatInformation(new CombatLogEntry { kind = "battle_start", title = "루미 도전", detail = "3턴 · 공격하지 않음 · 피해 40마다 20 크레딧" });
            TraitEvent(false, "battle_start", null, true);
            combat.playerOpeningEnergy = combat.energy;
            StartPlayerTurn(true);
            Say("루미 도전 · 3턴 동안 가한 피해 40마다 20 크레딧을 얻습니다. 루미는 공격하지 않으며 처형되지 않습니다.");
        }

        void RecordLumiDamage(int damage)
        {
            if (!IsLumiCombat || damage <= 0) return;
            var combat = State.combat;
            combat.lumiDamage = (int)Math.Min(int.MaxValue, (long)combat.lumiDamage + damage);
            int total = combat.lumiDamage / LumiDamageThreshold * LumiCreditReward;
            int granted = Math.Max(0, total - combat.lumiCredits);
            if (granted == 0) return;
            combat.lumiCredits = total;
            State.credits = (int)Math.Min(int.MaxValue, (long)State.credits + granted);
            ResearchProgression.LumiCreditsEarned(State, granted);
            Say("루미 · 누적 피해 " + combat.lumiDamage + " · 크레딧 +" + granted + " (합계 " + total + ")");
        }

        void FinishLumiCombat()
        {
            if (!IsLumiCombat) return;
            var combat = State.combat;
            RecordCombatInformation(new CombatLogEntry { kind = "battle_end", title = "루미 도전 종료", detail = "3턴 종료 · 누적 피해 " + combat.lumiDamage + " · 획득 " + combat.lumiCredits + " 크레딧" });
            Say("루미가 떠났습니다. 누적 피해 " + combat.lumiDamage + " · 획득 " + combat.lumiCredits + " 크레딧.");
            CompleteNode();
        }

        void OpenLumiShop()
        {
            State.lumiShopObjectId = GameDatabase.Objects[Next(GameDatabase.Objects.Count)].id;
            State.lumiShopPurchases.Clear();
            State.stage = RunStage.LumiShop;
            Say("루미 상점 · 각 상품은 한 번 구매할 수 있습니다. 오브젝트는 정가에서 40 크레딧 할인되며 할인 쿠폰이 적용되지 않습니다.");
        }

        // ChooseEventOption marks this rare encounter's one reward before entry.
        // The original node is completed only after the merchant is dismissed.
        public bool OpenLumiShopFromEncounter()
        {
            if (State.stage != RunStage.Encounter || ActiveNode() == null) return false;
            OpenLumiShop();
            return true;
        }

        public bool LeaveLumiShop()
        {
            if (State.stage != RunStage.LumiShop) return false;
            Say("루미 상점에서 나왔습니다.");
            CompleteNode();
            return true;
        }

        public bool LumiItemSold(string id) => State.lumiShopPurchases.Contains(id);
        public int LumiItemPrice(string id)
        {
            switch (id)
            {
                case "telescope_camera": return 20;
                case "tactical_module": return 160;
                case "object": return GameDatabase.Object(LumiObjectId) == null ? int.MaxValue : LumiObjectPrice;
                case "portable_kiosk": return 100;
                default: return int.MaxValue;
            }
        }
        public bool CanBuyLumiItem(string id) => State.stage == RunStage.LumiShop && !LumiItemSold(id)
            && LumiItemPrice(id) != int.MaxValue && State.credits >= LumiItemPrice(id);

        public bool BuyLumiItem(string id)
        {
            if (!CanBuyLumiItem(id)) return Fail("이미 구매한 상품이거나 크레딧이 부족합니다.");
            int price = LumiItemPrice(id);
            State.credits -= price;
            State.lumiShopPurchases.Add(id);
            switch (id)
            {
                case "telescope_camera": State.telescopicCameras++; break;
                case "tactical_module": State.tacticalModules++; break;
                case "object": State.objects.Add(LumiObjectId); ResearchProgression.LumiObjectPurchased(State); break;
                case "portable_kiosk": State.portableKiosks++; break;
            }
            return Say("루미 상점에서 구매했습니다. 크레딧 -" + price);
        }

        public bool UseTelescopicCamera()
        {
            if (!CanUseTelescopicCamera) return Fail("망원 카메라는 지도에서 아직 확인하지 않은 다음 두 줄을 살펴볼 때 사용할 수 있습니다.");
            foreach (var node in ScoutNodes())
            {
                AssignScoutedOpponent(node);
                if (!State.revealedNodeIds.Contains(node.id)) State.revealedNodeIds.Add(node.id);
            }
            State.telescopicCameras--; State.scoutedActs.Add(State.act);
            return Say("망원 카메라 사용 · 앞으로 두 줄의 실험체·야생동물과 키오스크 재고를 확인했습니다.");
        }

        public bool UsePortableKiosk()
        {
            if (!CanUsePortableKiosk) return Fail("휴대용 키오스크는 진행 중인 선택을 마친 뒤 지도·모닥불·조우에서 사용할 수 있습니다.");
            State.portableKiosks--;
            State.portableKioskReturnStage = State.stage;
            State.portableKioskActive = true;
            State.stage = RunStage.Kiosk;
            return Say("휴대용 키오스크를 열었습니다. 닫으면 이전 장소로 돌아갑니다.");
        }

        bool LeavePortableKiosk()
        {
            if (!State.portableKioskActive || State.stage != RunStage.Kiosk) return false;
            State.stage = State.portableKioskReturnStage;
            State.portableKioskActive = false;
            return Say("휴대용 키오스크를 닫고 이전 장소로 돌아왔습니다.");
        }

        void GrantLumiCookedFoods(int amount)
        {
            var cooked = GameDatabase.Foods.Where(food => !food.fullHeal && string.IsNullOrEmpty(food.upgradeTo)
                && GameDatabase.Foods.Any(ingredient => ingredient.upgradeTo == food.id)).ToList();
            if (cooked.Count == 0) return;
            for (int i = 0; i < amount; i++) State.foods.Add(cooked[Next(cooked.Count)].id);
        }
    }
}
