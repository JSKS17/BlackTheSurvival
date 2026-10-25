using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lumia
{
    public sealed partial class LumiaGame
    {
        CardDisplayCatalog cardDisplays;
        readonly UiDeckSnapshot uiDeck = new UiDeckSnapshot();
        readonly Dictionary<TextMeasureKey, TextMeasure> textMeasures = new Dictionary<TextMeasureKey, TextMeasure>();
        readonly Dictionary<SummaryLayoutKey, SummaryLayout> summaryLayouts = new Dictionary<SummaryLayoutKey, SummaryLayout>();
        int textMeasureRevision = -1;
        static readonly Dictionary<string, string> normalizedUIStrings = new Dictionary<string, string>(StringComparer.Ordinal);
        static readonly Dictionary<TraitMechanicProfile, TraitMemo> traitUIStrings = new Dictionary<TraitMechanicProfile, TraitMemo>();
        readonly Dictionary<GearDef, GearMemo> gearUIStrings = new Dictionary<GearDef, GearMemo>();
        CardDisplayCatalog CardDisplays => cardDisplays ?? (cardDisplays = new CardDisplayCatalog(GameDatabase.Cards, GameDatabase.Characters));
        UiDeckSnapshot EnsureDeckSnapshot() { uiDeck.Refresh(Engine?.State); return uiDeck; }
        IList<CardUpgradeOption> CachedUpgradeOptions(int index)
        {
            var candidates = EnsureDeckSnapshot().UpgradeIndices;
            return index >= 0 && Engine != null && index < Engine.State.deck.Count && candidates.Contains(index)
                ? CardDisplays.UpgradeOptions(Engine.State.deck[index], EnsureDeckSnapshot().TacticalEnhanced(Engine.State.deck[index])) : CardDisplays.UpgradeOptions((CardDef)null);
        }
        CardDisplayVariant DisplayCard(string reference, bool enemy = false, bool rare = false, bool tacticalReward = false)
        {
            string branch = rare ? RareCardUpgrades.BranchId : Engine != null && !enemy ? EnsureDeckSnapshot().Branch(reference) : null;
            bool tacticalEnhanced = tacticalReward || Engine != null && !enemy && !rare && EnsureDeckSnapshot().TacticalEnhanced(reference);
            return CardDisplays.Variant(reference, branch, tacticalEnhanced);
        }
        static string CachedNormalize(string text)
        {
            text = text ?? ""; string result;
            if (normalizedUIStrings.TryGetValue(text, out result)) return result;
            if (normalizedUIStrings.Count >= 512) normalizedUIStrings.Clear();
            result = DescriptionSummary.Normalize(text); normalizedUIStrings.Add(text, result); return result;
        }
        sealed class TraitMemo { public string fallback, summary, text; public TraitRule[] rules; }
        static string CachedTraitSummary(TraitMechanicProfile profile, string fallback)
        {
            if (profile == null) return CachedNormalize(fallback);
            TraitMemo memo;
            if (traitUIStrings.TryGetValue(profile, out memo) && memo.fallback == fallback && memo.summary == profile.summary && ReferenceEquals(memo.rules, profile.rules)) return memo.text;
            if (traitUIStrings.Count >= 256) traitUIStrings.Clear();
            memo = new TraitMemo { fallback = fallback, summary = profile.summary, rules = profile.rules, text = DescriptionSummary.Trait(profile, fallback) };
            traitUIStrings[profile] = memo; return memo.text;
        }
        string CachedPassiveSummary(PassiveDef passive) => passive == null ? "" : CachedTraitSummary(passive.mechanics, passive.description);
        string CachedRuneSummary(RuneDef rune) => rune == null ? "" : CachedTraitSummary(rune.mechanics, rune.description);
        sealed class GearMemo { public TraitMechanicProfile profile; public string description, text; }
        string CachedGearSummary(GearDef gear)
        {
            if (gear == null) return "";
            GearMemo memo;
            if (gearUIStrings.TryGetValue(gear, out memo) && ReferenceEquals(memo.profile, gear.mechanics) && memo.description == gear.description) return memo.text;
            if (gearUIStrings.Count >= 128) gearUIStrings.Clear();
            memo = new GearMemo { profile = gear.mechanics, description = gear.description, text = DescriptionSummary.Gear(gear) }; gearUIStrings[gear] = memo; return memo.text;
        }
        struct TextMeasureKey : IEquatable<TextMeasureKey>
        {
            public string text; public int size; public float width;
            public bool Equals(TextMeasureKey other) => text == other.text && size == other.size && width == other.width;
            public override bool Equals(object other) => other is TextMeasureKey && Equals((TextMeasureKey)other);
            public override int GetHashCode() { unchecked { return ((text.GetHashCode() * 397) ^ size) * 397 ^ width.GetHashCode(); } }
        }
        sealed class TextMeasure { public GUIContent content; public float height; }
        TextMeasure MeasureUIText(string text, int size, float width)
            => MeasureUITextAtSize(text, UIFontSize(size), width);
        TextMeasure MeasureUITextAtSize(string text, int size, float width)
        {
            if (textMeasureRevision != craftingFontRevision) { textMeasures.Clear(); summaryLayouts.Clear(); textMeasureRevision = craftingFontRevision; }
            var key = new TextMeasureKey { text = text ?? "", size = size, width = width }; TextMeasure measure;
            if (textMeasures.TryGetValue(key, out measure)) return measure;
            wrapped.fontSize = size;
            measure = new TextMeasure { content = new GUIContent(key.text) };
            measure.height = wrapped.CalcHeight(measure.content, width);
            if (textMeasures.Count >= 1024) textMeasures.Clear();
            textMeasures[key] = measure; return measure;
        }
        float CachedTextHeight(string text, int size, float width) => MeasureUIText(text, size, width).height;
        struct SummaryLayoutKey : IEquatable<SummaryLayoutKey>
        {
            public string text; public int size; public float width, height; public bool normalize;
            public bool Equals(SummaryLayoutKey other) => text == other.text && size == other.size && width == other.width && height == other.height && normalize == other.normalize;
            public override bool Equals(object other) => other is SummaryLayoutKey && Equals((SummaryLayoutKey)other);
            public override int GetHashCode() { unchecked { return (((text.GetHashCode() * 397 ^ size) * 397 ^ width.GetHashCode()) * 397 ^ height.GetHashCode()) * 397 ^ normalize.GetHashCode(); } }
        }
        sealed class SummaryLayout { public TextMeasure plain, scrolling; public float scrollWidth, scrollHeight; public string scrollKey; }
        void DrawCachedSummary(Rect rect, string text, int size, Color color, string key, bool normalize)
        {
            size = BodyUIFontSize(size, rect.height);
            // The content and native height are independent of selection colors and position.
            if (textMeasureRevision != craftingFontRevision) { textMeasures.Clear(); summaryLayouts.Clear(); textMeasureRevision = craftingFontRevision; }
            var cacheKey = new SummaryLayoutKey { text = text ?? "", size = size, width = rect.width, height = rect.height, normalize = normalize };
            SummaryLayout layout;
            if (!summaryLayouts.TryGetValue(cacheKey, out layout))
            {
                string content = normalize ? CachedNormalize(text) : text ?? "";
                layout = new SummaryLayout { plain = MeasureUITextAtSize(content, size, rect.width) };
                layout.scrollWidth = Math.Max(30, rect.width - 15);
                if (layout.plain.height > rect.height) { layout.scrolling = MeasureUITextAtSize(content, size, layout.scrollWidth); layout.scrollHeight = layout.scrolling.height + 5; }
                layout.scrollKey = (normalize ? "preview/" : "body/") + text + "/" + rect.width + "/" + rect.height + "/" + size;
                if (summaryLayouts.Count >= 512) summaryLayouts.Clear(); summaryLayouts[cacheKey] = layout;
            }
            wrapped.fontSize = size; wrapped.normal.textColor = color;
            if (layout.scrolling == null) { GUI.Label(rect, layout.plain.content, wrapped); return; }
            string scrollKey = normalize || key == null ? layout.scrollKey : key;
            Vector2 position; summaryScroll.TryGetValue(scrollKey, out position);
            position = BeginTouchScrollView(scrollKey, rect, position, new Rect(0, 0, layout.scrollWidth, layout.scrollHeight), false, true);
            GUI.Label(new Rect(0, 0, layout.scrollWidth, layout.scrollHeight), layout.scrolling.content, wrapped);
            EndTouchScrollView(); summaryScroll[scrollKey] = position;
        }
    }
}
