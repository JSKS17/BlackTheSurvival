using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Lumia
{
    // UI-only snapshots. The engine continues to create its own per-action cards;
    // callers must treat these definitions and option arrays as read-only.
    public sealed class CardDisplayVariant
    {
        public readonly CardDef card;
        public readonly string branch, label, suffix, classification, ownerId;
        public readonly string description, rules, classificationRules;
        public readonly int level;
        public readonly bool tacticalEnhanced;

        internal CardDisplayVariant(CardDef source, string branch, string ownerId, bool tacticalEnhanced = false)
        {
            this.branch = branch;
            this.ownerId = ownerId;
            this.tacticalEnhanced = tacticalEnhanced && TacticalSkills.IsTactical(source);
            // Even an ordinary display copy is isolated: Copy itself intentionally
            // shares some arrays which are safe in the engine's read-only paths.
            var upgraded = branch.Length == 0 ? source : StrategyMechanics.Apply(source, branch);
            card = CardDisplayCatalog.IsolatedCopy(this.tacticalEnhanced ? TacticalSkills.ApplyModuleUpgrade(upgraded) : upgraded);
            string ordinaryLabel = branch.Length == 0 ? "" : StrategyMechanics.BranchName(source.id, branch);
            label = ordinaryLabel + (this.tacticalEnhanced ? (ordinaryLabel.Length > 0 ? " · " : "") + "전술 강화" : "");
            suffix = RareCardUpgrades.SuffixForLabel(ordinaryLabel) + (this.tacticalEnhanced ? " [전술+]" : "");
            level = branch.Length == 0 ? 0 : branch == RareCardUpgrades.BranchId ? 2 : 1;
            classification = CardPresentation.ClassificationLabel(card);
            description = card.description;
            rules = CardPresentation.Rules(card);
            classificationRules = CardPresentation.ClassificationRules(card);
        }
    }

    public sealed class CardDisplayCatalog
    {
        readonly CardDef[] definitions;
        readonly CharacterDef[] characters;
        readonly Dictionary<string, CardDef> byId = new Dictionary<string, CardDef>(StringComparer.Ordinal);
        readonly Dictionary<string, string> ownerIds = new Dictionary<string, string>(StringComparer.Ordinal);
        readonly Lru<VariantKey, CardDisplayVariant> variants;
        readonly Lru<PreviewKey, string> previews;
        readonly Lru<UpgradeOptionsKey, CardUpgradeOption[]> options;
        string[] searchText;
        string previousQuery;
        bool hasFilter;
        CardDef[] previousResult;
        string nullOwnerId;

        public int Capacity { get; private set; }
        public int VariantCount => variants.Count;
        public int PreviewCount => previews.Count;
        public int UpgradeOptionsCount => options.Count;

        public CardDisplayCatalog(IEnumerable<CardDef> cards, IEnumerable<CharacterDef> characters, int capacity = 512)
        {
            if (cards == null) throw new ArgumentNullException(nameof(cards));
            if (characters == null) throw new ArgumentNullException(nameof(characters));
            if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity));
            definitions = cards.ToArray();
            this.characters = characters.ToArray();
            if (definitions.Any(card => card == null)) throw new ArgumentException("Card definitions must not contain null.", nameof(cards));
            Capacity = capacity;
            variants = new Lru<VariantKey, CardDisplayVariant>(capacity);
            previews = new Lru<PreviewKey, string>(capacity);
            options = new Lru<UpgradeOptionsKey, CardUpgradeOption[]>(capacity);
            Clear();
        }

        // Runtime definitions are fixed after GameDatabase initialization. A tool
        // that deliberately edits a definition in place can explicitly refresh it.
        public void Clear()
        {
            variants.Clear(); previews.Clear(); options.Clear();
            byId.Clear(); ownerIds.Clear(); nullOwnerId = null;
            foreach (var definition in definitions)
                if (definition.id != null && !byId.ContainsKey(definition.id)) byId.Add(definition.id, definition);
            bool foundNullOwner = false;
            foreach (var character in characters)
            {
                if (character == null) continue;
                foreach (string alias in new[] { character.id, character.name })
                    if (alias == null)
                    {
                        if (!foundNullOwner) { nullOwnerId = character.id; foundNullOwner = true; }
                    }
                    else if (!ownerIds.ContainsKey(alias)) ownerIds.Add(alias, character.id);
            }
            searchText = definitions.Select(card => card.name + card.owner + card.key + " " + CardPresentation.ClassificationLabel(card)).ToArray();
            hasFilter = false; previousQuery = null; previousResult = null;
        }

        public CardDef Definition(string reference)
        {
            string id = CardInstances.BaseId(reference);
            CardDef value;
            return id != null && byId.TryGetValue(id, out value) ? value : null;
        }

        public CardDisplayVariant Variant(string reference, string branch = null, bool tacticalEnhanced = false) => Variant(Definition(reference), branch, tacticalEnhanced);

        public CardDisplayVariant Variant(CardDef source, string branch = null, bool tacticalEnhanced = false)
        {
            if (source == null) return null;
            branch = branch ?? "";
            tacticalEnhanced = tacticalEnhanced && TacticalSkills.IsTactical(source);
            var key = new VariantKey(source, branch, tacticalEnhanced);
            CardDisplayVariant value;
            if (variants.TryGet(key, out value)) return value;
            string ownerId;
            if (source.owner == null) ownerId = nullOwnerId;
            else if (!ownerIds.TryGetValue(source.owner, out ownerId)) ownerId = null;
            value = new CardDisplayVariant(source, branch, ownerId, tacticalEnhanced);
            variants.Add(key, value);
            return value;
        }

        // Resolve live numeric values before calling this method. No combat state,
        // effective cost, charge state, or selected target is retained here.
        public string Preview(CardDisplayVariant variant, int damage = -1, int block = -1, int heal = -1)
        {
            if (variant == null) return "";
            damage = damage < 0 ? variant.card.damage : damage;
            block = block < 0 ? variant.card.block : block;
            heal = heal < 0 ? variant.card.heal : heal;
            var key = new PreviewKey(variant, damage, block, heal);
            string value;
            if (previews.TryGet(key, out value)) return value;
            value = DescriptionSummary.CardPreview(variant.card, GameDatabase.Cards, damage, block, heal);
            previews.Add(key, value);
            return value;
        }

        // Retain original raw-query matching and source ordering. Spaces are part
        // of the query, unlike campfire equipment's separately trimmed search.
        public CardDef[] Filter(string query)
        {
            query = query ?? "";
            if (hasFilter && string.Equals(query, previousQuery, StringComparison.OrdinalIgnoreCase)) return previousResult;
            var result = new List<CardDef>();
            for (int i = 0; i < definitions.Length; i++)
                if (query.Length == 0 || searchText[i].IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) result.Add(definitions[i]);
            previousQuery = query; previousResult = result.ToArray(); hasFilter = true;
            return previousResult;
        }

        public CardUpgradeOption[] UpgradeOptions(string reference, bool tacticalEnhanced = false) => UpgradeOptions(Definition(reference), tacticalEnhanced);

        public CardUpgradeOption[] UpgradeOptions(CardDef source, bool tacticalEnhanced = false)
        {
            if (source == null) return EmptyOptions;
            tacticalEnhanced = tacticalEnhanced && TacticalSkills.IsTactical(source);
            var key = new UpgradeOptionsKey(source, tacticalEnhanced);
            CardUpgradeOption[] value;
            if (options.TryGet(key, out value)) return value;
            value = TacticalModules.UpgradeOptions(source, tacticalEnhanced).ToArray();
            options.Add(key, value);
            return value;
        }

        static readonly CardUpgradeOption[] EmptyOptions = new CardUpgradeOption[0];

        internal static CardDef IsolatedCopy(CardDef source)
        {
            var card = StrategyMechanics.Copy(source);
            if (card == null) return null;
            card.freeCastTargets = source.freeCastTargets == null ? null : (string[])source.freeCastTargets.Clone();
            card.freeCastCondition = CopyRule(source.freeCastCondition);
            card.statuses = source.statuses == null ? null : source.statuses.Select(rule => CopyRule(rule)).ToArray();
            if (card.mechanics != null)
                card.mechanics.freeCastTargets = source.mechanics.freeCastTargets == null ? null : (string[])source.mechanics.freeCastTargets.Clone();
            return card;
        }

        static T CopyRule<T>(T source) where T : SkillRule, new()
        {
            if (source == null) return null;
            var copy = new T();
            foreach (var field in typeof(T).GetFields()) field.SetValue(copy, field.GetValue(source));
            return copy;
        }

        struct UpgradeOptionsKey : IEquatable<UpgradeOptionsKey>
        {
            readonly CardDef source;
            readonly bool tacticalEnhanced;
            public UpgradeOptionsKey(CardDef source, bool tacticalEnhanced) { this.source = source; this.tacticalEnhanced = tacticalEnhanced; }
            public bool Equals(UpgradeOptionsKey other) => ReferenceEquals(source, other.source) && tacticalEnhanced == other.tacticalEnhanced;
            public override bool Equals(object other) => other is UpgradeOptionsKey && Equals((UpgradeOptionsKey)other);
            public override int GetHashCode() => unchecked(RuntimeHelpers.GetHashCode(source) * 397 ^ tacticalEnhanced.GetHashCode());
        }

        struct VariantKey : IEquatable<VariantKey>
        {
            readonly CardDef source;
            readonly string branch;
            readonly bool tacticalEnhanced;
            public VariantKey(CardDef source, string branch, bool tacticalEnhanced) { this.source = source; this.branch = branch; this.tacticalEnhanced = tacticalEnhanced; }
            public bool Equals(VariantKey other) => ReferenceEquals(source, other.source) && branch == other.branch && tacticalEnhanced == other.tacticalEnhanced;
            public override bool Equals(object other) => other is VariantKey && Equals((VariantKey)other);
            public override int GetHashCode() => unchecked((RuntimeHelpers.GetHashCode(source) * 397 ^ branch.GetHashCode()) * 397 ^ tacticalEnhanced.GetHashCode());
        }

        struct PreviewKey : IEquatable<PreviewKey>
        {
            readonly CardDisplayVariant variant;
            readonly int damage, block, heal;
            public PreviewKey(CardDisplayVariant variant, int damage, int block, int heal) { this.variant = variant; this.damage = damage; this.block = block; this.heal = heal; }
            public bool Equals(PreviewKey other) => ReferenceEquals(variant, other.variant) && damage == other.damage && block == other.block && heal == other.heal;
            public override bool Equals(object other) => other is PreviewKey && Equals((PreviewKey)other);
            public override int GetHashCode() => unchecked(((RuntimeHelpers.GetHashCode(variant) * 397 ^ damage) * 397 ^ block) * 397 ^ heal);
        }

        sealed class ReferenceComparer<T> : IEqualityComparer<T> where T : class
        {
            public static readonly ReferenceComparer<T> Instance = new ReferenceComparer<T>();
            public bool Equals(T first, T second) => ReferenceEquals(first, second);
            public int GetHashCode(T value) => RuntimeHelpers.GetHashCode(value);
        }

        sealed class Lru<TKey, TValue>
        {
            sealed class Entry
            {
                public TKey key;
                public TValue value;
            }
            readonly int capacity;
            readonly Dictionary<TKey, LinkedListNode<Entry>> entries;
            readonly LinkedList<Entry> recency = new LinkedList<Entry>();
            public int Count => entries.Count;
            public Lru(int capacity, IEqualityComparer<TKey> comparer = null)
            {
                this.capacity = capacity;
                entries = new Dictionary<TKey, LinkedListNode<Entry>>(comparer ?? EqualityComparer<TKey>.Default);
            }
            public bool TryGet(TKey key, out TValue value)
            {
                LinkedListNode<Entry> entry;
                if (entries.TryGetValue(key, out entry))
                {
                    recency.Remove(entry); recency.AddLast(entry); value = entry.Value.value; return true;
                }
                value = default(TValue); return false;
            }
            public void Add(TKey key, TValue value)
            {
                if (entries.Count == capacity)
                {
                    var oldest = recency.First;
                    entries.Remove(oldest.Value.key); recency.RemoveFirst();
                }
                var node = recency.AddLast(new Entry { key = key, value = value });
                entries.Add(key, node);
            }
            public void Clear() { entries.Clear(); recency.Clear(); }
        }
    }
}
