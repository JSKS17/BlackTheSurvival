using System;
using System.Collections.Generic;
using System.Linq;

namespace Lumia
{
    // Only source identity is cached. Token amounts, durations, conditions and
    // descriptions continue to come from the current combat snapshot.
    internal sealed class FieldCardSources
    {
        readonly CardDef[] cards;
        readonly int capacity;
        readonly Dictionary<SourceKey, string> sources = new Dictionary<SourceKey, string>();

        public int Count => sources.Count;

        public FieldCardSources(IEnumerable<CardDef> cards, int capacity = 512)
        {
            if (cards == null) throw new ArgumentNullException(nameof(cards));
            if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity));
            this.cards = cards.ToArray(); this.capacity = capacity;
        }

        public string Find(string owner, string key)
        {
            var identity = new SourceKey(owner, key);
            string source;
            if (sources.TryGetValue(identity, out source)) return source;
            source = cards.FirstOrDefault(card => card.owner == owner && card.mechanics != null
                && card.mechanics.rules.Any(rule => rule.key == key
                    && (rule.op == "gain" || rule.op == "set" || rule.op == "state_on" || rule.op == "state_toggle" || rule.op == "forge_sword")))?.id;
            if (sources.Count >= capacity) sources.Clear();
            sources.Add(identity, source);
            return source;
        }

        public void Clear() { sources.Clear(); }

        struct SourceKey : IEquatable<SourceKey>
        {
            readonly string owner, key;
            public SourceKey(string owner, string key) { this.owner = owner; this.key = key; }
            public bool Equals(SourceKey other) => owner == other.owner && key == other.key;
            public override bool Equals(object other) => other is SourceKey && Equals((SourceKey)other);
            public override int GetHashCode() => unchecked((owner == null ? 0 : owner.GetHashCode()) * 397 ^ (key == null ? 0 : key.GetHashCode()));
        }
    }

    public sealed partial class LumiaGame
    {
        FieldCardSources fieldCardSources;
        string FieldCardSource(SkillMechanicToken token)
        {
            if (!string.IsNullOrEmpty(token.sourceCard)) return token.sourceCard;
            if (fieldCardSources == null) fieldCardSources = new FieldCardSources(GameDatabase.Cards);
            return fieldCardSources.Find(token.owner, token.key);
        }
    }
}
