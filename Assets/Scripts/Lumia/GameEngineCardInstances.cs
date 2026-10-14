using System.Collections.Generic;
using System.Linq;

namespace Lumia
{
    public sealed partial class GameEngine
    {
        public CardDef PlayerCard(string reference)
        {
            var source = GameDatabase.Card(reference);
            if (source == null) return null;
            string branch = CardInstances.UpgradeBranch(State, reference);
            return string.IsNullOrEmpty(branch) ? source : StrategyMechanics.Apply(source, branch);
        }

        public string CardUpgradeLabel(string reference)
        {
            string branch = CardInstances.UpgradeBranch(State, reference);
            return string.IsNullOrEmpty(branch) ? "" : StrategyMechanics.BranchName(CardInstances.BaseId(reference), branch);
        }

        public List<int> UpgradeCardIndices()
        {
            return Enumerable.Range(0, State.deck.Count).Where(index => string.IsNullOrEmpty(CardUpgradeLabel(State.deck[index]))
                && StrategyMechanics.CanUpgrade(GameDatabase.Card(State.deck[index]))).ToList();
        }

        public List<string> UpgradeCandidates()
        {
            return UpgradeCardIndices().Select(index => State.deck[index]).ToList();
        }

        public List<CardUpgradeOption> CardUpgradeOptions(int deckIndex)
        {
            return UpgradeCardIndices().Contains(deckIndex) ? StrategyMechanics.Options(GameDatabase.Card(State.deck[deckIndex]))
                : new List<CardUpgradeOption>();
        }

        public List<CardUpgradeOption> CardUpgradeOptions(string reference)
        {
            int index = State.deck.FindIndex(id => id == reference && string.IsNullOrEmpty(CardUpgradeLabel(id)));
            return CardUpgradeOptions(index);
        }

        public bool UpgradeCard(int deckIndex, string branchId)
        {
            if (State.stage != RunStage.Encounter || !State.cardUpgradePending || !CardUpgradeOptions(deckIndex).Any(x => x.id == branchId))
                return Fail("강화할 카드 1장과 하나의 강화 방향을 선택하세요.");
            string original = State.deck[deckIndex];
            string reference = CardInstances.CreateReference(State, original);
            State.deck[deckIndex] = reference;
            State.cardUpgrades.Add(new CardUpgradeSelection { cardId = CardInstances.BaseId(original), instanceId = reference, branchId = branchId });
            State.cardUpgradePending = false;
            Say(GameDatabase.Card(reference).name + "+ · " + CardUpgradeLabel(reference) + " 완료. 선택한 카드 1장에만 적용됩니다.");
            CompleteNode();
            return true;
        }

        public bool UpgradeCard(string reference, string branchId)
        {
            int index = State.deck.FindIndex(id => id == reference && string.IsNullOrEmpty(CardUpgradeLabel(id)));
            return UpgradeCard(index, branchId);
        }
    }
}
