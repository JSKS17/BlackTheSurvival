using System;
using System.Collections.Generic;
using System.Linq;

namespace Lumia
{
    // Ordinary copies remain database IDs. An upgraded copy carries its own saved
    // reference through the deck and every combat pile, without changing CardDef.id.
    public static class CardInstances
    {
        const string Separator = "~u";
        public const int Version = 1;

        public static string BaseId(string reference)
        {
            int serial;
            int at = InstanceSeparator(reference, out serial);
            return at < 0 ? reference : reference.Substring(0, at);
        }

        public static bool IsInstance(string reference)
        {
            int serial;
            return InstanceSeparator(reference, out serial) >= 0;
        }

        public static bool SameType(string first, string second)
        {
            return !string.IsNullOrEmpty(first) && !string.IsNullOrEmpty(second)
                && string.Equals(BaseId(first), BaseId(second), StringComparison.Ordinal);
        }

        public static string UpgradeBranch(RunState state, string reference)
        {
            if (state == null || !IsInstance(reference) || state.cardUpgrades == null) return null;
            var selection = state.cardUpgrades.FirstOrDefault(x => x != null
                && x.instanceId == reference && SameType(x.cardId, reference));
            return selection == null ? null : selection.branchId;
        }

        public static string CreateReference(RunState state, string cardId)
        {
            if (state == null || string.IsNullOrEmpty(cardId)) return null;
            int highest = AllReferences(state).Select(Serial).DefaultIfEmpty(0).Max();
            state.nextCardInstanceId = Math.Max(Math.Max(1, state.nextCardInstanceId), highest + 1);
            return BaseId(cardId) + Separator + state.nextCardInstanceId++;
        }

        // An old upgrade affected every copy of a type. Preserve those already owned
        // (including a saved battle's temporary copies) while dropping the global rule,
        // so cards obtained after loading are ordinary cards again. This is idempotent.
        public static bool NormalizeLegacy(RunState state)
        {
            if (state == null) return false;
            if (state.cardUpgrades == null) state.cardUpgrades = new List<CardUpgradeSelection>();
            if (state.upgrades == null) state.upgrades = new List<string>();
            if (state.deck == null) state.deck = new List<string>();
            var legacy = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var selection in state.cardUpgrades.Where(x => x != null))
            {
                if (string.IsNullOrEmpty(selection.instanceId) && IsInstance(selection.cardId))
                    selection.instanceId = selection.cardId;
                if (!string.IsNullOrEmpty(selection.instanceId))
                {
                    selection.cardId = BaseId(selection.instanceId);
                    continue;
                }
                if (!string.IsNullOrEmpty(selection.cardId) && !string.IsNullOrEmpty(selection.branchId)
                    && !legacy.ContainsKey(BaseId(selection.cardId)))
                    legacy.Add(BaseId(selection.cardId), selection.branchId);
            }
            foreach (var old in state.upgrades)
            {
                if (string.IsNullOrEmpty(old)) continue;
                if (IsInstance(old))
                {
                    if (!state.cardUpgrades.Any(x => x != null && x.instanceId == old))
                        state.cardUpgrades.Add(new CardUpgradeSelection { cardId = BaseId(old), instanceId = old, branchId = "legacy" });
                }
                else if (!legacy.ContainsKey(old)) legacy.Add(old, "legacy");
            }
            bool changed = legacy.Count > 0 || state.upgrades.Count > 0;
            state.cardUpgrades.RemoveAll(x => x == null || string.IsNullOrEmpty(x.instanceId));
            state.upgrades.Clear();
            var pools = new Dictionary<string, Queue<string>>(StringComparer.Ordinal);
            foreach (var entry in legacy) pools.Add(entry.Key, new Queue<string>());
            for (int i = 0; i < state.deck.Count; ++i)
            {
                string reference = state.deck[i];
                string branch;
                if (IsInstance(reference) || reference == null || !legacy.TryGetValue(reference, out branch)) continue;
                string upgraded = AddUpgrade(state, reference, branch);
                state.deck[i] = upgraded;
                pools[reference].Enqueue(upgraded);
            }
            var combat = state.combat;
            if (combat != null && legacy.Count > 0)
            {
                foreach (var pile in new[] { combat.hand, combat.drawPile, combat.discardPile, combat.exhaustPile })
                {
                    if (pile == null) continue;
                    for (int i = 0; i < pile.Count; ++i) pile[i] = MigrateCombatCopy(state, pile[i], legacy, pools);
                }
                if (!string.IsNullOrEmpty(combat.retainedCardId))
                    combat.retainedCardId = MigrateCombatCopy(state, combat.retainedCardId, legacy, pools);
                // Skill history and free-cast targets keep database IDs. Garnet's
                // recalled R, however, is one physical copy and expires as that copy.
                if (combat.freeCasts != null && combat.freeCastSources != null
                    && combat.freeCastSources.Contains("garnet_r|garnet_r"))
                    foreach (var grant in combat.freeCasts.Where(x => x != null && x.cardId == "garnet_r"
                        && x.uses > 0 && string.IsNullOrEmpty(x.cardReference)))
                    {
                        foreach (var pile in new[] { combat.hand, combat.drawPile, combat.discardPile })
                        {
                            string recalled = pile == null ? null : pile.LastOrDefault(id => SameType(id, "garnet_r"));
                            if (recalled == null) continue;
                            grant.cardReference = recalled;
                            break;
                        }
                    }
            }
            state.cardInstanceVersion = Version;
            int highest = AllReferences(state).Select(Serial).DefaultIfEmpty(0).Max();
            state.nextCardInstanceId = Math.Max(Math.Max(1, state.nextCardInstanceId), highest + 1);
            return changed;
        }

        static string AddUpgrade(RunState state, string cardId, string branch)
        {
            string reference = CreateReference(state, cardId);
            state.cardUpgrades.Add(new CardUpgradeSelection { cardId = BaseId(cardId), instanceId = reference, branchId = branch });
            return reference;
        }

        static string MigrateCombatCopy(RunState state, string reference, Dictionary<string, string> legacy, Dictionary<string, Queue<string>> pools)
        {
            string branch;
            if (IsInstance(reference) || reference == null || !legacy.TryGetValue(reference, out branch)) return reference;
            var pool = pools[reference];
            return pool.Count > 0 ? pool.Dequeue() : AddUpgrade(state, reference, branch);
        }

        static IEnumerable<string> AllReferences(RunState state)
        {
            foreach (var reference in state.deck ?? new List<string>()) yield return reference;
            foreach (var selection in state.cardUpgrades ?? new List<CardUpgradeSelection>())
                if (selection != null) yield return selection.instanceId;
            var c = state.combat;
            if (c == null) yield break;
            foreach (var pile in new[] { c.hand, c.drawPile, c.discardPile, c.exhaustPile })
                if (pile != null) foreach (var reference in pile) yield return reference;
            yield return c.retainedCardId;
            if(c.chargedCards!=null)foreach(var held in c.chargedCards)if(held!=null)yield return held.reference;
        }

        static int Serial(string reference)
        {
            int serial;
            InstanceSeparator(reference, out serial);
            return serial;
        }

        static int InstanceSeparator(string reference, out int serial)
        {
            serial = 0;
            if (string.IsNullOrEmpty(reference)) return -1;
            int at = reference.LastIndexOf(Separator, StringComparison.Ordinal);
            int parsed;
            if (at <= 0 || !int.TryParse(reference.Substring(at + Separator.Length), out parsed) || parsed <= 0) return -1;
            serial = parsed;
            return at;
        }
    }
}
