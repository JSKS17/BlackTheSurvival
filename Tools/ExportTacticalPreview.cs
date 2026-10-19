using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Lumia;

// Links against the same pure runtime assembly used by RunCoreTests. The preview
// contains real configured cards and transformations, never a second balance table.
public static class ExportTacticalPreview
{
    static string Q(string value)
    {
        var result = new StringBuilder("\"");
        foreach (char c in value ?? "")
            switch (c)
            {
                case '\\': result.Append("\\\\"); break;
                case '"': result.Append("\\\""); break;
                case '\n': result.Append("\\n"); break;
                case '\r': result.Append("\\r"); break;
                case '\t': result.Append("\\t"); break;
                default: if (c < 32) result.Append("\\u" + ((int)c).ToString("x4")); else result.Append(c); break;
            }
        return result.Append('"').ToString();
    }
    static string P(string key, string value) => Q(key) + ":" + Q(value);
    static string N(string key, int value) => Q(key) + ":" + value;
    static string B(string key, bool value) => Q(key) + ":" + (value ? "true" : "false");
    static string O(params string[] properties) => "{" + string.Join(",", properties) + "}";
    static string A(IEnumerable<string> values) => "[" + string.Join(",", values) + "]";
    static string SubjectName(string id) => GameDatabase.Character(id)?.name ?? id;
    static string Card(CardDef card) => O(N("cost", card.cost), N("damage", card.damage), N("hits", card.hits),
        N("block", card.block), N("heal", card.heal), B("exhaust", card.exhaust), B("movement", card.movement),
        P("summary", DescriptionSummary.CardPreview(card, GameDatabase.Cards)),
        P("description", card.description), P("rules", CardPresentation.Rules(card)),
        P("classification", CardPresentation.ClassificationLabel(card)));

    public static int Main(string[] args)
    {
        if (args.Length != 1) throw new ArgumentException("Expected an output JSON path.");
        var cards = TacticalSkills.Definitions.Select(definition =>
        {
            var card = GameDatabase.Card(definition.id);
            if (card == null) throw new InvalidOperationException("Missing tactical: " + definition.id);
            var enhanced = TacticalSkills.ApplyModuleUpgrade(card);
            var sales = TacticalEncounterOptions.Sales.Where(sale => sale.cardId == card.id).Select(sale =>
                O(P("subjectId", sale.subjectId), P("subject", SubjectName(sale.subjectId)),
                    N("normalPrice", sale.price), N("rareEnhancedPrice", sale.rarePrice), P("story", sale.narrative)));
            return O(P("id", card.id), P("name", card.name), N("sourceCode", definition.sourceCode),
                B("cobalt", definition.cobalt), B("legacy", definition.legacy),
                P("moduleChange", TacticalSkills.ModuleUpgradeDescription(card)),
                "\"base\":" + Card(card), "\"enhanced\":" + Card(enhanced), "\"sales\":" + A(sales));
        }).ToArray();
        if (cards.Length != 26 || GameDatabase.Cards.Count(card => card.key == "F") != 26)
            throw new InvalidOperationException("Expected exactly 26 unique F cards, with Transcendence excluded.");
        var data = O(P("generatedAtUtc", DateTime.UtcNow.ToString("o")), N("total", cards.Length), N("runtimeCardCount", GameDatabase.Cards.Count),
            N("added", cards.Length - 3), N("normalModuleAmount", 1), N("rareModuleAmount", 2),
            "\"moduleSubjects\":" + A(TacticalEncounterOptions.ModuleSubjects.Select(id => O(P("id", id), P("name", SubjectName(id))))),
            "\"cards\":" + A(cards));
        string output = Path.GetFullPath(args[0]);
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        File.WriteAllText(output, data, new UTF8Encoding(false));
        Console.WriteLine("Exported " + cards.Length + " live tactical definitions and module variants.");
        return 0;
    }
}
