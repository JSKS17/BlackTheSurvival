using System;
using System.Collections.Generic;
using System.Linq;

namespace Lumia
{
    // Equipment definitions do not change during a game UI's lifetime. Keep their
    // display text and search fields outside the immediate-mode drawing loop.
    public sealed class CraftingItem
    {
        public readonly GearDef gear;
        public readonly string description;
        internal readonly string searchText;
        internal readonly HashSet<string> tags;

        internal CraftingItem(GearDef gear)
        {
            this.gear = gear;
            description = DescriptionSummary.Normalize(DescriptionSummary.Gear(gear));
            var optionTags = gear.optionTags ?? new string[0];
            searchText = gear.name + " " + gear.weaponClass + " " + gear.description + " " + string.Join(" ", optionTags);
            tags = new HashSet<string>(optionTags, StringComparer.Ordinal);
        }
    }

    public struct CraftingVisibleRange
    {
        public readonly int first;
        public readonly int end;

        public CraftingVisibleRange(int first, int end)
        {
            this.first = first;
            this.end = end;
        }
    }

    public sealed class CraftingCatalog
    {
        static readonly string[] EmptyTags = new string[0];
        readonly Dictionary<GearSlot, CraftingItem[]> bySlot;
        readonly HashSet<string> selectedTags = new HashSet<string>(StringComparer.Ordinal);
        GearSlot previousSlot;
        string previousQuery, previousMaterial;
        bool hasFilter;
        CraftingItem[] previousResult = new CraftingItem[0];

        // These arrays are immutable UI snapshots; callers should only read them.
        public string[] Tags { get; private set; }

        public CraftingCatalog(IEnumerable<GearDef> gear)
        {
            if (gear == null) throw new ArgumentNullException(nameof(gear));
            var items = gear.Select(definition =>
            {
                if (definition == null) throw new ArgumentException("Equipment definitions must not contain null.", nameof(gear));
                return new CraftingItem(definition);
            }).ToArray();
            Tags = items.SelectMany(item => item.gear.optionTags ?? EmptyTags).Distinct().ToArray();
            bySlot = items.GroupBy(item => item.gear.slot).ToDictionary(group => group.Key, group => group.ToArray());
        }

        public CraftingItem[] Filter(GearSlot slot, string query, IEnumerable<string> tags, string materialId = null)
        {
            query = (query ?? "").Trim();
            materialId = materialId ?? "";
            tags = tags ?? EmptyTags;
            if (hasFilter && slot == previousSlot
                && string.Equals(query, previousQuery, StringComparison.OrdinalIgnoreCase)
                && string.Equals(materialId, previousMaterial, StringComparison.Ordinal)
                && selectedTags.SetEquals(tags)) return previousResult;

            selectedTags.Clear();
            selectedTags.UnionWith(tags);
            CraftingItem[] candidates;
            var result = new List<CraftingItem>();
            if (bySlot.TryGetValue(slot, out candidates))
            {
                foreach (var item in candidates)
                    if ((materialId.Length == 0 || item.gear.objectId == materialId)
                        && selectedTags.IsSubsetOf(item.tags)
                        && (query.Length == 0 || item.searchText.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0))
                        result.Add(item);
            }
            previousSlot = slot;
            previousQuery = query;
            previousMaterial = materialId;
            previousResult = result.ToArray();
            hasFilter = true;
            return previousResult;
        }

        // Return complete rows intersecting [scrollY, scrollY + viewportHeight).
        // The caller still supplies the scroll view's full content height.
        public static CraftingVisibleRange VisibleItems(int count, float scrollY, float viewportHeight, int columns = 3, float rowHeight = 151)
        {
            if (count <= 0 || columns <= 0 || rowHeight <= 0 || float.IsNaN(rowHeight) || float.IsInfinity(rowHeight)
                || viewportHeight <= 0 || float.IsNaN(viewportHeight)) return new CraftingVisibleRange(0, 0);
            if (float.IsNaN(scrollY) || scrollY < 0) scrollY = 0;
            int rows = (count - 1) / columns + 1;
            double firstRowValue = Math.Floor((double)scrollY / rowHeight);
            if (firstRowValue >= rows) return new CraftingVisibleRange(count, count);
            int firstRow = (int)firstRowValue;
            double endRowValue = Math.Ceiling(((double)scrollY + viewportHeight) / rowHeight);
            int endRow = endRowValue >= rows ? rows : (int)endRowValue;
            int first = (int)Math.Min(count, (long)firstRow * columns);
            int end = (int)Math.Min(count, (long)Math.Max(firstRow, endRow) * columns);
            return new CraftingVisibleRange(first, end);
        }
    }
}
