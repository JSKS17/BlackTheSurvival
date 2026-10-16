#if LUMIA_TEST_RUNNER
using System;
using System.Collections.Generic;
using System.Linq;
using Lumia;

public static partial class CoreSmokeTests
{
    private static void CardDisplayCaching()
    {
        var catalog = new CardDisplayCatalog(GameDatabase.Cards, GameDatabase.Characters);
        var originalDescriptions = GameDatabase.Cards.Select(card => card.description).ToArray();
        var originalCardNumbers = GameDatabase.Cards.Select(CardDisplayNumbers).ToArray();
        foreach (var source in GameDatabase.Cards)
        {
            Check(ReferenceEquals(catalog.Definition(source.id), source), "display lookup preserves the original database definition: " + source.id);
            Check(ReferenceEquals(catalog.Definition(source.id + "~u17"), source), "display lookup parses an upgraded physical reference once: " + source.id);
            var ordinary = catalog.Variant(source.id);
            Check(!ReferenceEquals(ordinary.card, source) && ordinary.card.id == source.id, "ordinary UI definitions are isolated snapshots: " + source.id);
            Check(ordinary.label == "" && ordinary.suffix == "" && ordinary.level == 0, "ordinary card has no upgrade badge: " + source.id);
            Check(ordinary.classification == CardPresentation.ClassificationLabel(source)
                && ordinary.rules == CardPresentation.Rules(source)
                && ordinary.classificationRules == CardPresentation.ClassificationRules(source)
                && ordinary.description == source.description, "static display content matches the original generators: " + source.id);
            var owner = GameDatabase.Characters.Find(character => character.id == source.owner || character.name == source.owner);
            Check(ordinary.ownerId == (owner == null ? null : owner.id), "display owner lookup preserves id/name matching: " + source.id);
            string normal = catalog.Preview(ordinary);
            Check(normal == DescriptionSummary.CardPreview(source, GameDatabase.Cards), "ordinary cached card preview matches original output: " + source.id);
            Check(ReferenceEquals(normal, catalog.Preview(ordinary, source.damage, source.block, source.heal)), "default and explicit base-number previews reuse one cached string: " + source.id);
            string live = catalog.Preview(ordinary, 99, 37, 21);
            Check(live == DescriptionSummary.CardPreview(source, GameDatabase.Cards, 99, 37, 21), "numeric combat overrides are never replaced by cached base values: " + source.id);
            Check(ReferenceEquals(live, catalog.Preview(ordinary, 99, 37, 21)), "unchanged live-number preview reuses its result: " + source.id);
            Check(catalog.Preview(ordinary, 98, 36, 20) == DescriptionSummary.CardPreview(source, GameDatabase.Cards, 98, 36, 20), "new numeric values compute the current preview: " + source.id);
        }

        foreach (string id in new[] { "basic_attack", "nia_q", "nia_w", "nia_r", "sua_w", "eleven_q", "garnet_r", "eva_r", "irem_r", "isaac_e" })
        {
            var source = GameDatabase.Card(id);
            foreach (string branch in new[] { "power", "legacy", "efficiency", "arcade", RareCardUpgrades.BranchId })
            {
                var expected = StrategyMechanics.Apply(source, branch);
                var variant = catalog.Variant(id + "~u1", branch);
                Check(variant.branch == branch && variant.label == StrategyMechanics.BranchName(id, branch)
                    && variant.suffix == RareCardUpgrades.SuffixForLabel(variant.label)
                    && variant.level == (branch == RareCardUpgrades.BranchId ? 2 : 1), "upgrade branch labels and ++ level remain distinct: " + id + "/" + branch);
                Check(CardDisplayNumbers(variant.card) == CardDisplayNumbers(expected)
                    && variant.description == expected.description
                    && variant.classification == CardPresentation.ClassificationLabel(expected), "cached upgrade preserves exact branch behavior: " + id + "/" + branch);
                Check(catalog.Preview(variant) == DescriptionSummary.CardPreview(expected, GameDatabase.Cards), "cached upgraded preview matches uncached output: " + id + "/" + branch);
                Check(ReferenceEquals(variant, catalog.Variant(id + "~u999", branch)), "display variants share definition and branch, independent of physical serial: " + id + "/" + branch);
            }
        }

        var normalQ = catalog.Variant("nia_q");
        var powerQ = catalog.Variant("nia_q~u1", "power");
        var arcadeQ = catalog.Variant("nia_q~u1", "arcade");
        var rareQ = catalog.Variant("nia_q~u1", RareCardUpgrades.BranchId);
        Check(!ReferenceEquals(normalQ, powerQ) && !ReferenceEquals(powerQ, arcadeQ) && !ReferenceEquals(powerQ, rareQ), "ordinary, branch+, and rare++ never share display definitions");
        var firstRun = new RunState();
        firstRun.cardUpgrades.Add(new CardUpgradeSelection { cardId = "nia_q", instanceId = "nia_q~u1", branchId = "power" });
        var secondRun = new RunState();
        secondRun.cardUpgrades.Add(new CardUpgradeSelection { cardId = "nia_q", instanceId = "nia_q~u1", branchId = "arcade" });
        Check(ReferenceEquals(powerQ, catalog.Variant("nia_q~u1", CardInstances.UpgradeBranch(firstRun, "nia_q~u1")))
            && ReferenceEquals(arcadeQ, catalog.Variant("nia_q~u1", CardInstances.UpgradeBranch(secondRun, "nia_q~u1"))), "reused physical serial in another run resolves its own current branch");
        Check(catalog.Variant("nia_q~u1").level == 0 && catalog.Variant("nia_q").level == 0, "a physical identity and later ordinary copies do not imply upgrades");

        var attack = catalog.Variant("basic_attack");
        string baseline = catalog.Preview(attack, 6, 0, 0);
        string damageChanged = catalog.Preview(attack, 26, 0, 0);
        string blockChanged = catalog.Preview(attack, 6, 7, 0);
        string healChanged = catalog.Preview(attack, 6, 0, 5);
        Check(baseline != damageChanged && baseline != blockChanged && baseline != healChanged, "each changed combat number invalidates the summary's numeric cache key");
        Check(ReferenceEquals(baseline, catalog.Preview(attack, 6, 0, 0)), "returning to previous live values returns their correct cached summary");

        string[] queries = { null, "", " ", "Q", "q", "이동", "설치", "디버프", "니아", "없는 카드", " NIA ", "기본 공격" };
        foreach (string query in queries.Concat(GameDatabase.Cards.Select(card => card.name)).Concat(GameDatabase.Cards.Select(card => card.owner).Distinct()))
        {
            var expected = GameDatabase.Cards.Where(card => string.IsNullOrEmpty(query)
                || (card.name + card.owner + card.key + " " + CardPresentation.ClassificationLabel(card)).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0);
            Check(catalog.Filter(query).SequenceEqual(expected), "catalog cache preserves raw search matching and original order");
        }
        var all = catalog.Filter(null);
        Check(ReferenceEquals(all, catalog.Filter("")), "null and empty catalog searches reuse one snapshot");
        var lower = catalog.Filter("q");
        Check(ReferenceEquals(lower, catalog.Filter("Q")), "case-insensitive catalog searches reuse one snapshot");
        Check(!ReferenceEquals(lower, catalog.Filter(" q ")), "catalog search preserves meaningful surrounding spaces");

        foreach (var source in GameDatabase.Cards)
        {
            var expected = StrategyMechanics.Options(source);
            var actual = catalog.UpgradeOptions(source);
            Check(actual.Select(option => option.id + "|" + option.name + "|" + option.description)
                .SequenceEqual(expected.Select(option => option.id + "|" + option.name + "|" + option.description)), "upgrade option cache matches each original branch description: " + source.id);
            Check(ReferenceEquals(actual, catalog.UpgradeOptions(source.id + "~u7")), "unchanged upgrade options are reused by base definition: " + source.id);
        }

        Check(GameDatabase.Cards.Select(card => card.description).SequenceEqual(originalDescriptions)
            && GameDatabase.Cards.Select(CardDisplayNumbers).SequenceEqual(originalCardNumbers), "card display construction does not modify shared game data");

        var first = new CharacterDef { id = "first", name = "collision" };
        var second = new CharacterDef { id = "collision", name = "second" };
        var byName = new CardDef { id = "duplicate", name = "first card", owner = "collision", key = "Q", damage = 3 };
        var byId = new CardDef { id = "duplicate", name = "second card", owner = "first", key = "Q", damage = 8 };
        var synthetic = new CardDisplayCatalog(new[] { byName, byId }, new[] { first, second }, 3);
        Check(ReferenceEquals(synthetic.Definition("duplicate"), byName), "duplicate IDs preserve the first original Find match");
        Check(synthetic.Variant(byName).ownerId == first.id && synthetic.Variant(byId).ownerId == first.id, "owner id/name alias collisions preserve original character order");
        Check(!ReferenceEquals(synthetic.Variant(byName), synthetic.Variant(byId)) && synthetic.Variant(byId).card.damage == 8, "same-id source replacements use definition identity, not only card ID");
        Check(synthetic.Filter("").SequenceEqual(new[] { byName, byId }), "catalog filter retains duplicate definitions in source order");

        CardDisplayIsolationChecks();
        CardDisplayBoundedChecks();
        FieldCardSourceCaching();

        var beforeClear = synthetic.Variant(byName);
        byName.damage = 19;
        Check(ReferenceEquals(beforeClear, synthetic.Variant(byName)) && beforeClear.card.damage == 3, "runtime catalog intentionally retains immutable definition snapshots");
        synthetic.Clear();
        Check(synthetic.VariantCount == 0 && synthetic.PreviewCount == 0 && synthetic.UpgradeOptionsCount == 0,
            "explicit definition refresh releases all bounded result caches");
        Check(synthetic.Variant(byName).card.damage == 19 && !ReferenceEquals(beforeClear, synthetic.Variant(byName)), "explicit refresh observes a deliberate in-place definition edit");
        Check(catalog.Definition("not_a_card") == null && catalog.Variant("not_a_card") == null && catalog.Preview(null) == ""
            && catalog.UpgradeOptions("not_a_card").Length == 0, "unknown definitions retain safe empty display behavior");
        var empty = new CardDisplayCatalog(new CardDef[0], new CharacterDef[0]);
        Check(empty.Filter("").Length == 0 && ReferenceEquals(empty.Filter(""), empty.Filter(null)), "empty catalogs reuse empty filter snapshots");
    }

    private static void CardDisplayIsolationChecks()
    {
        var source = new CardDef { id = "display_isolation", owner = "unknown", key = "Q", damage = 5,
            freeCastTargets = new[] { "nia_q" }, freeCastCount = 1, freeCastCondition = new SkillRule { conditionKey = "blocks", conditionAmount = 2 },
            mechanics = new SkillMechanicProfile { freeCastTargets = new[] { "nia_q" }, rules = new[] { new SkillRule { op = "gain", key = "blocks", amount = 1 } } },
            statuses = new[] { new CardStatusRule { op = "status", key = "slow", duration = 1 } } };
        var catalog = new CardDisplayCatalog(new[] { source }, new CharacterDef[0]);
        var normal = catalog.Variant(source);
        var power = catalog.Variant(source, "power");
        var rare = catalog.Variant(source, "rare");
        normal.card.damage = 999;
        normal.card.freeCastTargets[0] = "mutated";
        normal.card.freeCastCondition.conditionAmount = 99;
        normal.card.mechanics.rules[0].amount = 99;
        normal.card.mechanics.freeCastTargets[0] = "mutated";
        normal.card.statuses[0].duration = 99;
        Check(source.damage == 5 && source.freeCastTargets[0] == "nia_q" && source.freeCastCondition.conditionAmount == 2
            && source.mechanics.rules[0].amount == 1 && source.mechanics.freeCastTargets[0] == "nia_q" && source.statuses[0].duration == 1,
            "even an accidental display mutation cannot alter the database's nested mutable fields");
        Check(power.card.damage == 8 && power.card.freeCastTargets[0] == "nia_q" && power.card.freeCastCondition.conditionAmount == 2
            && power.card.mechanics.rules[0].amount == 1 && power.card.mechanics.freeCastTargets[0] == "nia_q" && power.card.statuses[0].duration == 1,
            "normal and power display variants do not share nested mutable definitions");
        Check(rare.card.damage == 11 && rare.card.freeCastTargets[0] == "nia_q" && rare.card.freeCastCondition.conditionAmount == 2
            && rare.card.mechanics.freeCastTargets[0] == "nia_q" && rare.card.statuses[0].duration == 1,
            "rare display variants stay isolated from ordinary display mutations");
    }

    private static void CardDisplayBoundedChecks()
    {
        var catalog = new CardDisplayCatalog(GameDatabase.Cards, GameDatabase.Characters, 3);
        var first = catalog.Variant("basic_attack");
        var second = catalog.Variant("basic_guard");
        catalog.Variant("nia_q");
        Check(ReferenceEquals(first, catalog.Variant("basic_attack")), "variant LRU hits retain the same display object");
        catalog.Variant("nia_w");
        Check(catalog.VariantCount == 3 && ReferenceEquals(first, catalog.Variant("basic_attack")), "variant cache never exceeds its capacity and preserves recently used entries");
        Check(!ReferenceEquals(second, catalog.Variant("basic_guard")), "least recently used display variants are evicted");
        for (int value = 0; value < 30; value++)
        {
            Check(catalog.Preview(first, value, 0, 0) == DescriptionSummary.CardPreview(first.card, GameDatabase.Cards, value, 0, 0), "bounded numeric previews preserve each recomputed current result");
            Check(catalog.PreviewCount <= 3, "preview cache remains bounded during repeated battle-number changes");
        }
        foreach (var source in GameDatabase.Cards.Take(30))
        {
            catalog.Variant(source);
            catalog.UpgradeOptions(source);
            Check(catalog.VariantCount <= 3 && catalog.UpgradeOptionsCount <= 3, "variant and upgrade-option caches remain bounded during browsing");
        }
        catalog.Clear();
        Check(catalog.VariantCount == 0 && catalog.PreviewCount == 0 && catalog.UpgradeOptionsCount == 0, "clearing a catalog releases cached source references and live-number results");
    }

    private static void FieldCardSourceCaching()
    {
        var sources = new FieldCardSources(GameDatabase.Cards);
        foreach (var card in GameDatabase.Cards.Where(card => card.mechanics != null))
            foreach (var rule in card.mechanics.rules)
            {
                string expected = GameDatabase.Cards.FirstOrDefault(candidate => candidate.owner == card.owner && candidate.mechanics != null
                    && candidate.mechanics.rules.Any(candidateRule => candidateRule.key == rule.key
                        && (candidateRule.op == "gain" || candidateRule.op == "set" || candidateRule.op == "state_on" || candidateRule.op == "state_toggle")))?.id;
                Check(sources.Find(card.owner, rule.key) == expected, "field source cache preserves first owner/key generator lookup: " + card.id + "/" + rule.key);
                int count = sources.Count;
                Check(sources.Find(card.owner, rule.key) == expected && sources.Count == count, "positive and negative field source lookups reuse cached results");
            }
        var first = new CardDef { id = "first", owner = "test", mechanics = new SkillMechanicProfile { rules = new[] { new SkillRule { op = "gain", key = "resource" } } } };
        var later = new CardDef { id = "later", owner = "test", mechanics = new SkillMechanicProfile { rules = new[] { new SkillRule { op = "set", key = "resource" } } } };
        var synthetic = new FieldCardSources(new[] { first, later }, 3);
        Check(synthetic.Find("test", "resource") == "first", "multiple field resource generators retain original card order");
        Check(synthetic.Find("test", "missing") == null, "unknown field sources produce safe empty results");
        first.mechanics.rules[0].key = "missing";
        Check(synthetic.Find("test", "missing") == null, "a missing source is cached without repeatedly scanning the catalog");
        synthetic.Clear();
        Check(synthetic.Find("test", "missing") == "first", "an explicit source refresh observes deliberate definition edits");
        for (int i = 0; i < 50; i++)
        {
            Check(synthetic.Find("test", "unknown_" + i) == null && synthetic.Count <= 3, "field source cache has a fixed upper bound including misses");
        }
        Check(synthetic.Find(null, null) == null && synthetic.Count <= 3, "null field identity remains safe and bounded");

        int maxVisible = 0;
        for (int x = 0; x <= 2160; x++)
        {
            var range = CraftingCatalog.VisibleItems(12, x, 683, 1, 180);
            Check(range.first >= 0 && range.end >= range.first && range.end <= 12, "horizontal choice culling preserves original physical hand indices");
            maxVisible = Math.Max(maxVisible, range.end - range.first);
        }
        Check(maxVisible == 5, "advanced horizontal card choices render at most five of twelve tiles");
        var middle = CraftingCatalog.VisibleItems(12, 720, 683, 1, 180);
        Check(middle.first == 4 && middle.end == 8, "scrolling choices keeps absolute selected-reference ordering");
    }

    private static string CardDisplayNumbers(CardDef card)
    {
        return card.id + "|" + card.cost + "|" + card.damage + "|" + card.block + "|" + card.heal + "|" + card.draw + "|" + card.energy + "|" + card.evasion + "|"
            + card.poison + "|" + card.weak + "|" + card.vulnerable + "|"
            + string.Join(";", (card.mechanics?.rules ?? new SkillRule[0]).Select(rule => rule.op + ":" + rule.key + ":" + rule.amount + ":" + rule.cap));
    }
}
#endif
