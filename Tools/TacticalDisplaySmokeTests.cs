#if LUMIA_TEST_RUNNER
using System;
using System.Collections.Generic;
using System.Linq;
using Lumia;

public static partial class CoreSmokeTests
{
    static void TacticalDisplaySnapshots()
    {
        var catalog = new CardDisplayCatalog(GameDatabase.Cards, GameDatabase.Characters);
        foreach (var source in GameDatabase.Cards.Where(TacticalSkills.IsTactical))
        {
            string original = FieldSnapshot(source);
            foreach (string branch in new[] { "", "power", RareCardUpgrades.BranchId })
            {
                var ordinary = catalog.Variant(source.id, branch);
                var enhanced = catalog.Variant(source.id, branch, true);
                var expected = TacticalSkills.ApplyModuleUpgrade(branch.Length == 0 ? source : StrategyMechanics.Apply(source, branch));
                Check(!ReferenceEquals(ordinary, enhanced) && !ordinary.tacticalEnhanced && enhanced.tacticalEnhanced,
                    "display cache separates module state as well as ordinary/rare branch: " + source.id + "/" + branch);
                Check(FieldSnapshot(enhanced.card) == FieldSnapshot(expected), "module display is identical to actual engine transformation: " + source.id);
                Check(enhanced.suffix.Contains("전술+") && enhanced.label.Contains("전술 강화")
                    && enhanced.level == ordinary.level, "module badge does not overwrite ordinary +/rare ++ level: " + source.id);
                Check(catalog.Preview(enhanced) == DescriptionSummary.CardPreview(expected, GameDatabase.Cards),
                    "module preview uses enhanced mechanics and values: " + source.id);
                Check(ReferenceEquals(enhanced, catalog.Variant(source.id + "~u777", branch, true)),
                    "same definition, branch and module state reuse one UI snapshot: " + source.id);
            }
            Check(FieldSnapshot(source) == original, "module display leaves database/enemy card immutable: " + source.id);
        }
        Check(ReferenceEquals(catalog.Variant("nia_q"), catalog.Variant("nia_q", null, true)),
            "non-tactical cards cannot acquire a module badge through a display flag");

        var state = new RunState { deck = new List<string> { "tactical_blink~u1", "tactical_blink" } };
        var snapshot = new UiDeckSnapshot();
        Check(snapshot.Refresh(state) && !snapshot.TacticalEnhanced(state.deck[0]), "physical serial alone is not a tactical enhancement");
        int revision = snapshot.Revision;
        state.tacticalCardUpgrades.Add(state.deck[0]);
        Check(snapshot.Refresh(state) && snapshot.Revision == revision + 1 && snapshot.TacticalEnhanced(state.deck[0])
            && !snapshot.TacticalEnhanced(state.deck[1]), "same-length deck refreshes when module flags change");
        Check(!snapshot.Refresh(state), "unchanged module state does not rebuild the deck snapshot every frame");
        state.tacticalCardUpgrades[0] = "tactical_blink";
        Check(snapshot.Refresh(state) && !snapshot.TacticalEnhanced(state.deck[0]) && !snapshot.TacticalEnhanced("tactical_blink"),
            "replacing a same-length flag list is detected; a global database id is never enhanced");
        var other = new RunState { deck = new List<string>(state.deck), tacticalCardUpgrades = new List<string> { state.deck[0] } };
        Check(snapshot.Refresh(other) && snapshot.TacticalEnhanced(other.deck[0]), "another run owns its own module flags despite reused serials");
    }
}
#endif
