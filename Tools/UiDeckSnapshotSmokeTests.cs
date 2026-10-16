#if LUMIA_TEST_RUNNER
using System;
using System.Collections.Generic;
using System.Linq;
using Lumia;

public static partial class CoreSmokeTests
{
    private static void UiDeckSnapshotCaching()
    {
        var state = new RunState { deck = new List<string> { "nia_q", "basic_attack", "nia_q", "nia_w" }, cardUpgrades = new List<CardUpgradeSelection>() };
        var snapshot = new UiDeckSnapshot();
        Check(snapshot.Refresh(state), "first deck snapshot is created");
        Check(snapshot.Groups.Select(group => group.reference).SequenceEqual(state.deck.Distinct()), "UI deck grouping preserves first occurrence order");
        Check(snapshot.Groups.Select(group => group.count).SequenceEqual(new[] { 2, 1, 1 }), "UI deck grouping preserves duplicate quantities");
        Check(snapshot.UpgradeIndices.SequenceEqual(Enumerable.Range(0, state.deck.Count).Where(i => StrategyMechanics.CanUpgrade(GameDatabase.Card(state.deck[i])))), "initial upgrade indices use absolute deck indices");
        var groups = snapshot.Groups; var candidates = snapshot.UpgradeIndices; int revision = snapshot.Revision;
        for (int i = 0; i < 120; i++) Check(!snapshot.Refresh(state) && ReferenceEquals(groups, snapshot.Groups) && ReferenceEquals(candidates, snapshot.UpgradeIndices), "unchanged GUI events reuse deck grouping and candidates");
        Check(snapshot.Revision == revision, "unchanged deck keeps the same revision");

        state.deck[2] = "nia_q~u1";
        state.cardUpgrades.Add(new CardUpgradeSelection { cardId = "nia_q", instanceId = "nia_q~u1", branchId = "arcade" });
        Check(snapshot.Refresh(state) && snapshot.Groups.Length == 4, "same-size instance upgrade invalidates grouped deck");
        Check(snapshot.Branch("nia_q~u1") == "arcade" && snapshot.Branch("nia_q") == null, "upgrade applies only to the selected instance");
        Check(!snapshot.UpgradeIndices.Contains(2) && snapshot.UpgradeIndices.Contains(0), "upgraded copy is excluded while ordinary copy remains eligible");
        state.cardUpgrades[0].branchId = RareCardUpgrades.BranchId;
        Check(snapshot.Refresh(state) && snapshot.Branch("nia_q~u1") == RareCardUpgrades.BranchId, "same-count branch mutation invalidates the snapshot");
        state.deck.Reverse();
        Check(snapshot.Refresh(state) && snapshot.Groups.Select(group => group.reference).SequenceEqual(state.deck.Distinct()), "same-count deck reorder preserves fresh absolute positions");
        state.deck.RemoveAt(1);
        Check(snapshot.Refresh(state), "deck removal invalidates the snapshot");
        state.deck.Add("aya_q");
        Check(snapshot.Refresh(state) && snapshot.Groups.Last().reference == "aya_q", "new reward card appears at the correct position");

        var second = new RunState { deck = new List<string> { "nia_q~u1", "nia_q" }, cardUpgrades = new List<CardUpgradeSelection> { new CardUpgradeSelection { cardId = "nia_q", instanceId = "nia_q~u1", branchId = "power" } } };
        Check(snapshot.Refresh(second) && snapshot.Branch("nia_q~u1") == "power", "a new run can reuse an instance serial with a different branch");
        second.cardUpgrades.Insert(0, new CardUpgradeSelection { cardId = "aya_q", instanceId = "nia_q~u1", branchId = "efficiency" });
        Check(snapshot.Refresh(second) && snapshot.Branch("nia_q~u1") == CardInstances.UpgradeBranch(second, "nia_q~u1"), "invalid instance metadata cannot override a valid matching upgrade");
        second.cardUpgrades.Add(new CardUpgradeSelection { cardId = "nia_q", instanceId = "nia_q~u1", branchId = "arcade" });
        Check(snapshot.Refresh(second) && snapshot.Branch("nia_q~u1") == "power", "duplicate metadata preserves original first matching selection semantics");
        second.cardUpgrades.Clear();
        Check(snapshot.Refresh(second) && snapshot.Branch("nia_q~u1") == null && snapshot.UpgradeIndices.Contains(0), "removing metadata restores an ordinary eligible display");
        Check(snapshot.Refresh(null) && snapshot.Groups.Length == 0 && snapshot.UpgradeIndices.Count == 0, "closing a run clears its UI snapshot");
    }
}
#endif
