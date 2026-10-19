using System;
using System.Collections.Generic;
using System.Linq;

namespace Lumia
{
    public sealed partial class GameEngine
    {
        public bool IsTacticalUpgraded(string reference) => CardInstances.IsInstance(reference)
            && State.tacticalCardUpgrades != null && State.tacticalCardUpgrades.Contains(reference)
            && TacticalSkills.IsTactical(GameDatabase.Card(reference));

        public string TacticalUpgradeLabel(string reference) => IsTacticalUpgraded(reference) ? TacticalModules.UpgradeLabel : "";

        public List<int> TacticalModuleCardIndices() => Enumerable.Range(0, State.deck.Count)
            .Where(index => TacticalSkills.IsTactical(GameDatabase.Card(State.deck[index]))
                && !IsTacticalUpgraded(State.deck[index])).ToList();

        public string TacticalModuleUnavailableReason()
        {
            if (State.tacticalModules <= 0) return "전술강화 모듈을 보유하고 있지 않습니다.";
            if (State.stage == RunStage.Combat) return "전투 중에는 전술강화 모듈을 사용할 수 없습니다.";
            if (State.stage != RunStage.Map && State.stage != RunStage.Kiosk
                && State.stage != RunStage.Campfire && State.stage != RunStage.Encounter)
                return "지도, 키오스크, 모닥불 또는 실험체 조우에서 사용할 수 있습니다.";
            if (State.runeChangePending || State.cardUpgradePending || State.rareRemovalPending
                || State.rareCopyPending || !string.IsNullOrEmpty(State.pendingPassive) || pendingCardChoice != null)
                return "진행 중인 선택을 먼저 완료하거나 취소하세요.";
            if (TacticalModuleCardIndices().Count == 0) return "강화할 수 있는 전술 스킬(F) 카드가 없습니다.";
            return "";
        }

        public bool CanUseTacticalModule() => string.IsNullOrEmpty(TacticalModuleUnavailableReason());

        public CardDef TacticalModulePreview(int deckIndex)
        {
            if (!TacticalModuleCardIndices().Contains(deckIndex)) return null;
            return TacticalSkills.ApplyModuleUpgrade(PlayerCard(State.deck[deckIndex]));
        }

        public string AddEnhancedTacticalRewardCard(string cardId)
        {
            var source = GameDatabase.Card(cardId);
            if (!TacticalSkills.IsTactical(source)) return null;
            string reference = CardInstances.CreateReference(State, source.id);
            State.deck.Add(reference);
            State.tacticalCardUpgrades.Add(reference);
            return reference;
        }

        // The picker is UI-local. Nothing is reserved or consumed when it opens
        // or closes; validation and inventory/deck changes happen together here.
        public bool UpgradeTacticalCard(int deckIndex)
        {
            string unavailable = TacticalModuleUnavailableReason();
            if (!string.IsNullOrEmpty(unavailable)) return Fail(unavailable);
            if (!TacticalModuleCardIndices().Contains(deckIndex)) return Fail("강화되지 않은 전술 스킬(F) 카드 1장을 선택하세요.");
            string original = State.deck[deckIndex];
            string branch = CardInstances.UpgradeBranch(State, original);
            string reference = CardInstances.CreateReference(State, original);
            State.deck[deckIndex] = reference;
            if (!string.IsNullOrEmpty(branch))
                State.cardUpgrades.Add(new CardUpgradeSelection { cardId = CardInstances.BaseId(original), instanceId = reference, branchId = branch });
            State.tacticalCardUpgrades.Add(reference);
            State.tacticalModules--;
            return Say(GameDatabase.Card(reference).name + " · 전술 강화 완료. 선택한 카드 1장에만 적용됩니다.");
        }

        void TransferTacticalUpgrade(string original, string replacement)
        {
            if (State.tacticalCardUpgrades == null || !State.tacticalCardUpgrades.Contains(original)) return;
            if (!State.tacticalCardUpgrades.Contains(replacement)) State.tacticalCardUpgrades.Add(replacement);
            if (!State.deck.Contains(original)) State.tacticalCardUpgrades.Remove(original);
        }

        void NormalizeTacticalModules()
        {
            State.tacticalModules = Math.Max(0, State.tacticalModules);
            State.tacticalCardUpgrades = (State.tacticalCardUpgrades ?? new List<string>())
                .Where(reference => CardInstances.IsInstance(reference) && State.deck.Contains(reference)
                    && TacticalSkills.IsTactical(GameDatabase.Card(reference))).Distinct().ToList();
        }
    }
}
