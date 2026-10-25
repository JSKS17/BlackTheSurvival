#if LUMIA_TEST_RUNNER
using System;
using System.Collections.Generic;
using System.Linq;
using Lumia;

public static partial class CoreSmokeTests
{
    private static void CraftingCatalogCaching()
    {
        var source = GameDatabase.Gear;
        var catalog = new CraftingCatalog(source);
        var allTags = source.SelectMany(gear => gear.optionTags ?? new string[0]).Distinct().ToArray();
        Check(source.Count == 57, "crafting cache covers all 57 equipment definitions");
        Check(source.Count(gear => gear.slot == GearSlot.Weapon) == 28, "crafting performance fixture covers 28 actual weapons");
        Check(catalog.Tags.SequenceEqual(allTags), "crafting tag order matches the original first occurrence order");
        var definitionsBefore = source.Select(CraftingDefinitionSnapshot).ToArray();
        foreach (GearSlot slot in Enum.GetValues(typeof(GearSlot)))
        {
            foreach (var item in catalog.Filter(slot, "", null))
            {
                Check(ReferenceEquals(item.gear, source.First(gear => gear.id == item.gear.id)), "cached crafting entry uses the original immutable gear definition");
                Check(item.description == DescriptionSummary.Normalize(DescriptionSummary.Gear(item.gear)), "cached crafting description preserves normalized display text: " + item.gear.id);
            }
        }

        var tagSets = new List<string[]> { new string[0], new[] { "missing option" } };
        tagSets.AddRange(allTags.Select(tag => new[] { tag }));
        tagSets.AddRange(source.Where(gear => gear.optionTags != null && gear.optionTags.Length > 1)
            .Select(gear => gear.optionTags.Take(2).ToArray()));
        string[] queries = { null, "", "  ", "피해", "D", "치명타", "라 다 르", "missing equipment" };
        queries = queries.Concat(source.Select(gear => gear.name))
            .Concat(source.Select(gear => gear.weaponClass).Where(value => !string.IsNullOrEmpty(value)).Distinct())
            .Concat(source.Where(gear => !string.IsNullOrEmpty(gear.description)).Select(gear => gear.description.Substring(0, Math.Min(4, gear.description.Length))).Distinct())
            .ToArray();
        foreach (GearSlot slot in Enum.GetValues(typeof(GearSlot)))
            foreach (var query in queries)
                foreach (var tags in tagSets)
                {
                    var expected = OriginalCraftFilter(source, slot, query, tags).Select(gear => gear.id);
                    var actual = catalog.Filter(slot, query, tags).Select(item => item.gear.id);
                    Check(actual.SequenceEqual(expected), "crafting cache preserves slot, raw-text query and AND-tag filtering");
                }

        var selected = new HashSet<string>();
        var weapons = catalog.Filter(GearSlot.Weapon, "", selected);
        for (int frame = 0; frame < 240; frame++)
            Check(ReferenceEquals(weapons, catalog.Filter(GearSlot.Weapon, "", selected)), "unchanged crafting filter reuses the same result array");
        Check(ReferenceEquals(weapons, catalog.Filter(GearSlot.Weapon, "   ", null)), "trimmed empty query and null tags share the current filter");
        var d = catalog.Filter(GearSlot.Weapon, " d ", null);
        Check(ReferenceEquals(d, catalog.Filter(GearSlot.Weapon, "D", new string[0])), "case-insensitive trimmed queries reuse the same cached result");
        var otherQuery = catalog.Filter(GearSlot.Weapon, "피해", null);
        Check(!ReferenceEquals(d, otherQuery), "changing the query invalidates the result array");
        var otherSlot = catalog.Filter(GearSlot.Head, "피해", null);
        Check(!ReferenceEquals(otherQuery, otherSlot), "changing the equipment slot invalidates the result array");
        selected.Add("치명타 확률");
        var critical = catalog.Filter(GearSlot.Weapon, "", selected);
        Check(!ReferenceEquals(weapons, critical), "changing selected tags rebuilds crafting results");
        Check(critical.Length > 0 && critical.All(item => item.gear.optionTags.Contains("치명타 확률")), "critical option selects real matching weapons");
        Check(ReferenceEquals(critical, catalog.Filter(GearSlot.Weapon, "", new[] { "치명타 확률", "치명타 확률" })), "duplicate selected tags preserve set semantics and cache identity");
        selected.Add("치명타 피해");
        var combined = catalog.Filter(GearSlot.Weapon, "", selected);
        Check(!ReferenceEquals(critical, combined), "adding a tag invalidates crafting results");
        Check(ReferenceEquals(combined, catalog.Filter(GearSlot.Weapon, "", selected.Reverse())), "selected-tag order does not invalidate crafting results");
        selected.Remove("치명타 확률");
        Check(!ReferenceEquals(combined, catalog.Filter(GearSlot.Weapon, "", selected)), "removing a selected tag invalidates crafting results");
        selected.Clear();
        var cleared = catalog.Filter(GearSlot.Weapon, "", selected);
        Check(cleared.Select(item => item.gear.id).SequenceEqual(weapons.Select(item => item.gear.id)), "clearing selected tags restores all weapons");

        // A material selection composes with slot, raw name search and AND tags;
        // it must also participate in cache invalidation.
        foreach (GearSlot slot in Enum.GetValues(typeof(GearSlot)))
            foreach (string material in new[] { "meteorite", "tree", "mithril", "force", "blood", "missing material" })
                foreach (string query in new[] { "", "피해", "레이더" })
                    foreach (var tags in new[] { new string[0], new[] { "치명타 확률" }, new[] { "치명타 확률", "일반 공격" } })
                    {
                        var expected = OriginalCraftFilter(source, slot, query, tags).Where(item => item.objectId == material).Select(item => item.id);
                        var actual = catalog.Filter(slot, query, tags, material);
                        Check(actual.Select(item => item.gear.id).SequenceEqual(expected), "material filter composes with slot, query and all option tags: " + material);
                        Check(ReferenceEquals(actual, catalog.Filter(slot, query, tags.Reverse(), material)), "unchanged material and reordered tags reuse the cached array");
                    }
        var meteorite = catalog.Filter(GearSlot.Weapon, "", null, "meteorite");
        var tree = catalog.Filter(GearSlot.Weapon, "", null, "tree");
        Check(meteorite.Length > 0 && tree.Length > 0 && !ReferenceEquals(meteorite, tree), "changing a real material rebuilds results even with identical slot/query/tags");
        var allMaterials = catalog.Filter(GearSlot.Weapon, "", null, "");
        Check(allMaterials.Select(item => item.gear.id).SequenceEqual(weapons.Select(item => item.gear.id)), "all materials restores every weapon");
        Check(ReferenceEquals(allMaterials, catalog.Filter(GearSlot.Weapon, "", null, null)), "null and empty material share the all-materials cache");

        CheckCraftingRange(28, 0, 284, 0, 6, "top viewport only draws two rows");
        CheckCraftingRange(28, 150, 284, 0, 9, "partial rows include all intersecting items");
        CheckCraftingRange(28, 151, 284, 3, 9, "exact row boundary excludes the row above");
        CheckCraftingRange(28, 604, 284, 12, 18, "middle viewport selects the correct full rows");
        CheckCraftingRange(28, 1226, 284, 24, 28, "bottom viewport includes the final partial row");
        CheckCraftingRange(28, 1359, 151, 27, 28, "last row is bounded by actual equipment count");
        CheckCraftingRange(28, 0, 244, 0, 6, "material-filter viewport includes the first two rows");
        CheckCraftingRange(28, 151, 244, 3, 9, "material-filter viewport keeps middle rows visible");
        CheckCraftingRange(28, 1266, 244, 24, 28, "material-filter viewport reaches the actual bottom row");
        CheckCraftingRange(28, 1510, 284, 28, 28, "scroll beyond content draws nothing");
        CheckCraftingRange(28, -50, 284, 0, 6, "negative scrolling clamps to the top");
        CheckCraftingRange(28, 0, 151, 0, 3, "viewport ending at row boundary excludes the next row");
        CheckCraftingRange(28, 151, 0, 0, 0, "zero-height viewport draws nothing");
        CheckCraftingRange(0, 0, 284, 0, 0, "empty search results have no visible entries");
        CheckCraftingRange(1, 0, 284, 0, 1, "single result is safe");
        CheckCraftingRange(28, float.NaN, 284, 0, 6, "invalid scroll offset safely starts at the top");
        CheckCraftingRange(28, float.PositiveInfinity, 284, 28, 28, "unbounded scroll offset is safe");
        CheckCraftingRange(28, 0, float.PositiveInfinity, 0, 28, "unbounded viewport includes remaining items safely");
        Check(CraftingCatalog.VisibleItems(int.MaxValue, 0, 284).end == 6, "visible range avoids integer overflow for large counts");
        Check(CraftingCatalog.VisibleItems(28, 0, 284, 0).end == 0, "invalid column count is safe");
        Check(CraftingCatalog.VisibleItems(28, 0, 284, 3, 0).end == 0, "invalid row height is safe");
        Check(CraftingCatalog.VisibleItems(28, float.PositiveInfinity, 284, 3, float.PositiveInfinity).end == 0, "invalid infinite row height is safe");
        int maxVisible = 0;
        for (int y = 0; y <= 1510; y++)
        {
            var range = CraftingCatalog.VisibleItems(28, y, 284);
            Check(range.first >= 0 && range.end >= range.first && range.end <= 28, "all weapon scroll positions stay inside result bounds");
            maxVisible = Math.Max(maxVisible, range.end - range.first);
        }
        Check(maxVisible == 9, "284px crafting viewport never processes more than nine of 28 weapon tiles");
        Check(maxVisible * 3 < weapons.Length, "visible-only rendering removes more than two thirds of weapon tile work");
        Check(source.Select(CraftingDefinitionSnapshot).SequenceEqual(definitionsBefore), "crafting cache and filters do not mutate equipment definitions");

        var empty = new CraftingCatalog(new GearDef[0]);
        Check(empty.Tags.Length == 0 && empty.Filter(GearSlot.Weapon, "", null).Length == 0, "empty equipment catalog is supported");
        var emptyResult = empty.Filter(GearSlot.Weapon, "", null);
        Check(ReferenceEquals(emptyResult, empty.Filter(GearSlot.Weapon, "", new string[0])), "empty results are also reused");
        var nullTags = new GearDef { id = "no_tags", name = "Plain", weaponClass = "Case", description = "Original", slot = GearSlot.Weapon, optionTags = null };
        var synthetic = new CraftingCatalog(new[] { nullTags });
        Check(synthetic.Tags.Length == 0 && synthetic.Filter(GearSlot.Weapon, " case ", null).Length == 1, "missing option tags retain case-insensitive raw search behavior");
        Check(synthetic.Filter(GearSlot.Weapon, "Original", null).Length == 1, "search still includes the original equipment description");
        Check(synthetic.Filter(GearSlot.Weapon, "자동으로 추가", null).Length == 0, "generated display prose does not silently expand search matching");
    }

    private static IEnumerable<GearDef> OriginalCraftFilter(IEnumerable<GearDef> gear, GearSlot slot, string query, IEnumerable<string> tags)
    {
        query = (query ?? "").Trim();
        return gear.Where(item => item.slot == slot
            && tags.All(tag => (item.optionTags ?? new string[0]).Contains(tag))
            && (query.Length == 0 || (item.name + " " + item.weaponClass + " " + item.description + " " + string.Join(" ", item.optionTags ?? new string[0])).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0));
    }

    private static void CheckCraftingRange(int count, float scrollY, float height, int first, int end, string reason)
    {
        var range = CraftingCatalog.VisibleItems(count, scrollY, height);
        Check(range.first == first && range.end == end, reason);
    }

    private static string CraftingDefinitionSnapshot(GearDef gear)
    {
        return gear.id + "|" + gear.name + "|" + gear.slot + "|" + gear.weaponClass + "|" + gear.description + "|"
            + gear.attack + "|" + gear.block + "|" + gear.health + "|" + gear.critChance + "|" + gear.critDamageBonus + "|"
            + string.Join(" ", gear.optionTags ?? new string[0]);
    }
}
#endif
