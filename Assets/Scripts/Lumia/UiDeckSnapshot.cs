using System;
using System.Collections.Generic;

namespace Lumia
{
    public sealed class UiDeckGroup
    {
        public readonly string reference;
        public readonly int count;
        public UiDeckGroup(string reference, int count) { this.reference = reference; this.count = count; }
    }

    // UI-only snapshots. Compare values as well as lengths: an upgrade or weapon
    // replacement can change a deck without changing its size.
    public sealed class UiDeckSnapshot
    {
        RunState state;
        string[] deck = new string[0], upgrades = new string[0], tacticalUpgrades = new string[0];
        readonly Dictionary<string, string> branches = new Dictionary<string, string>(StringComparer.Ordinal);
        readonly HashSet<string> tacticalReferences = new HashSet<string>(StringComparer.Ordinal);
        public UiDeckGroup[] Groups { get; private set; } = new UiDeckGroup[0];
        public List<int> UpgradeIndices { get; private set; } = new List<int>();
        public int Revision { get; private set; }

        public bool Refresh(RunState value)
        {
            var cards = value?.deck;
            var selections = value?.cardUpgrades;
            var tactical = value?.tacticalCardUpgrades;
            int count = cards?.Count ?? 0, selectionCount = selections?.Count ?? 0, tacticalCount = tactical?.Count ?? 0;
            bool changed = !ReferenceEquals(state, value) || deck.Length != count || upgrades.Length != selectionCount * 3 || tacticalUpgrades.Length != tacticalCount;
            for (int i = 0; !changed && i < count; i++) changed = deck[i] != cards[i];
            for (int i = 0; !changed && i < selectionCount; i++)
            {
                var entry = selections[i];
                changed = upgrades[i * 3] != entry?.cardId || upgrades[i * 3 + 1] != entry?.instanceId || upgrades[i * 3 + 2] != entry?.branchId;
            }
            for (int i = 0; !changed && i < tacticalCount; i++) changed = tacticalUpgrades[i] != tactical[i];
            if (!changed) return false;
            state = value; deck = cards == null ? new string[0] : cards.ToArray();
            tacticalUpgrades = tactical == null ? new string[0] : tactical.ToArray();
            tacticalReferences.Clear();
            foreach (string reference in tacticalUpgrades)
                if (CardInstances.IsInstance(reference) && TacticalSkills.IsTactical(GameDatabase.Card(reference))) tacticalReferences.Add(reference);
            upgrades = new string[selectionCount * 3]; branches.Clear();
            for (int i = 0; i < selectionCount; i++)
            {
                var entry = selections[i];
                upgrades[i * 3] = entry?.cardId; upgrades[i * 3 + 1] = entry?.instanceId; upgrades[i * 3 + 2] = entry?.branchId;
                if (entry != null && CardInstances.IsInstance(entry.instanceId) && CardInstances.SameType(entry.cardId, entry.instanceId)
                    && !branches.ContainsKey(entry.instanceId)) branches.Add(entry.instanceId, entry.branchId);
            }
            var order = new List<string>(); var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (string reference in deck)
            {
                if (reference == null) continue;
                int held;
                if (counts.TryGetValue(reference, out held)) counts[reference] = held + 1;
                else { counts.Add(reference, 1); order.Add(reference); }
            }
            Groups = new UiDeckGroup[order.Count];
            for (int i = 0; i < order.Count; i++) Groups[i] = new UiDeckGroup(order[i], counts[order[i]]);
            UpgradeIndices = new List<int>();
            for (int i = 0; i < deck.Length; i++)
                if (string.IsNullOrEmpty(Branch(deck[i])) && StrategyMechanics.CanUpgrade(GameDatabase.Card(deck[i]))) UpgradeIndices.Add(i);
            Revision++;
            return true;
        }

        public string Branch(string reference)
        {
            string branch;
            return reference != null && branches.TryGetValue(reference, out branch) ? branch : null;
        }

        public bool TacticalEnhanced(string reference) => reference != null && tacticalReferences.Contains(reference);
    }
}
