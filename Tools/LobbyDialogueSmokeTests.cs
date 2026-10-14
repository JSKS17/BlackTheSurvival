#if LUMIA_TEST_RUNNER
using System;
using System.Collections.Generic;
using System.Linq;
using Lumia;

public static partial class CoreSmokeTests
{
    static void LobbyDialogueCatalog()
    {
        Check(LobbyDialogue.Count >= 40 && LobbyDialogue.Count <= 50 && LobbyDialogue.Lines.Count == LobbyDialogue.Count,
            "lobby dialogue has a complete, bounded catalog");
        Check(LobbyDialogue.Lines.All(line => !string.IsNullOrWhiteSpace(line) && line.Length <= 70 && !line.Contains("\n")),
            "every lobby line fits the short speech-bubble content budget");
        Check(LobbyDialogue.Lines.Distinct(StringComparer.Ordinal).Count() == LobbyDialogue.Count,
            "all lobby dialogue lines are distinct");
        var list = LobbyDialogue.Lines as IList<string>;
        Check(list != null && list.IsReadOnly, "callers cannot edit the shared lobby catalog");
        string original = LobbyDialogue.At(0); bool blocked = false;
        try { list[0] = "changed"; } catch (NotSupportedException) { blocked = true; }
        Check(blocked && LobbyDialogue.At(0) == original, "attempted external mutation leaves lobby dialogue intact");
    }

    static void LobbyDialogueRotationIsolation()
    {
        for (int start = 0; start < LobbyDialogue.Count; start++)
        {
            int cursor = start; var seen = new HashSet<int>();
            for (int i = 0; i < LobbyDialogue.Count; i++)
            {
                Check(cursor >= 0 && cursor < LobbyDialogue.Count && seen.Add(cursor), "one rotation visits each valid dialogue index exactly once");
                int next = LobbyDialogue.NextIndex(cursor);
                Check(next != cursor && LobbyDialogue.At(next) != LobbyDialogue.At(cursor), "clicking dialogue never immediately repeats the preceding line");
                cursor = next;
            }
            Check(cursor == start && seen.Count == LobbyDialogue.Count, "all lines remain reachable in a closed dialogue cycle");
        }
        Check(LobbyDialogue.NextIndex(-1) == 0 && LobbyDialogue.At(-1) == LobbyDialogue.At(LobbyDialogue.Count - 1),
            "the unstarted UI cursor and a negative lookup normalize safely");
        foreach (int index in new[] { int.MinValue, -123456789, int.MaxValue })
        {
            int normalized = (int)(((long)index % LobbyDialogue.Count + LobbyDialogue.Count) % LobbyDialogue.Count);
            Check(LobbyDialogue.At(index) == LobbyDialogue.At(normalized)
                && LobbyDialogue.NextIndex(index) == (normalized + 1) % LobbyDialogue.Count,
                "dialogue navigation safely handles integer boundaries without overflow");
        }

        var shown = Ready(63010); var untouched = new GameEngine((RunState)Clone(shown.State));
        int rng = shown.State.rngState, hp = shown.State.hp, credits = shown.State.credits;
        var deck = shown.State.deck.ToArray(); var unlocks = shown.State.research.unlockedReports.ToArray();
        int at = -1;
        for (int i = 0; i < LobbyDialogue.Count * 20; i++) { at = LobbyDialogue.NextIndex(at); LobbyDialogue.At(at); }
        Check(shown.State.rngState == rng && shown.State.hp == hp && shown.State.credits == credits
            && shown.State.deck.SequenceEqual(deck) && shown.State.research.unlockedReports.SequenceEqual(unlocks),
            "dialogue reads and clicks leave run RNG, resources, deck and research snapshot unchanged");
        Enter(shown, ZoneKind.Encounter); Enter(untouched, ZoneKind.Encounter);
        Check(shown.State.encounterOffers.SequenceEqual(untouched.State.encounterOffers) && shown.State.rngState == untouched.State.rngState,
            "clicking lobby dialogue cannot change the next randomized encounter or its saved RNG state");
    }

    static void LobbyDialogueRuleAccuracy()
    {
        var run = Ready(63011); int initial = run.PlayerBaseEnergy; run.State.level = 20;
        Check(initial == GameEngine.StartingMaxEnergy && run.PlayerBaseEnergy == initial
            && LobbyDialogue.At(0).Contains("코스트는 " + initial) && LobbyDialogue.At(1).Contains("레벨이 올라도"),
            "the starter and level tips agree with actual player energy");
        Check(LobbyDialogue.At(12).Contains("150%") && LobbyDialogue.At(12).Contains("장비") && LobbyDialogue.At(12).Contains("노말·하드 " + GameEngine.CriticalChanceCap + "%")
            && LobbyDialogue.At(12).Contains("무한 " + GameEngine.EndlessCriticalChanceCap + "%")
            && LobbyDialogue.At(26).Contains(GameEngine.WildlifeBasicAttackDropChance + "%")
            && GameEngine.WildlifeMeatDropChance == 50 && LobbyDialogue.At(26).Contains("절반"),
            "critical and wildlife supply tips use current caps and drop rates");
        Check(LobbyDialogue.At(36).Contains(GameEngine.FollowupOfferChance + "%")
            && LobbyDialogue.At(38).Contains("데이터 " + ResearchProgression.ReportCost)
            && LobbyDialogue.At(40).Contains(GameEngine.RareOfferChance + "%")
            && LobbyDialogue.At(40).Contains("18%") && LobbyDialogue.At(40).Contains("25%") && LobbyDialogue.At(40).Contains("막마다 한 번") && LobbyDialogue.At(40).Contains("++"),
            "encounter and journal tips use current probability and reward constants");
        Check(LobbyDialogue.At(44).Contains(RunModes.Name(RunMode.Normal))
            && LobbyDialogue.At(44).Contains(RunModes.Name(RunMode.Hard)) && LobbyDialogue.At(44).Contains(RunModes.Name(RunMode.Endless))
            && !RunModes.IsUnlocked(new ResearchProfile(), RunMode.Hard)
            && RunModes.IsUnlocked(new ResearchProfile { normalCleared = true }, RunMode.Hard)
            && !RunModes.IsUnlocked(new ResearchProfile { normalCleared = true }, RunMode.Endless)
            && RunModes.IsUnlocked(new ResearchProfile { hardCleared = true }, RunMode.Endless),
            "mode dialogue agrees with the real ordered normal-to-hard-to-endless unlock rules");
        Check(LobbyDialogue.At(45).Contains("체력 " + (RunModes.EnemyHealthPercent(RunMode.Hard) - 100) + "%")
            && LobbyDialogue.At(45).Contains("직접 피해 " + (RunModes.EnemyDamagePercent(RunMode.Hard) - 100) + "%")
            && LobbyDialogue.At(42).Contains("추가 " + RunModes.EndlessBossEnergyLimit)
            && RunModes.EnemyHealthPercent(RunMode.Endless, 1) > RunModes.EnemyHealthPercent(RunMode.Endless, 0)
            && RunModes.EnemyDamagePercent(RunMode.Endless, 1) > RunModes.EnemyDamagePercent(RunMode.Endless, 0),
            "difficulty and endless tips match actual scaling and the separate boss-energy cap");
    }
}
#endif
