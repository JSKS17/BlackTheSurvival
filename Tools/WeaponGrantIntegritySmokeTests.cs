#if LUMIA_TEST_RUNNER
using System;
using System.Collections.Generic;
using System.Linq;
using Lumia;

public static partial class CoreSmokeTests
{
    static GameEngine WeaponChoiceFixture(string choice, params string[] equipment)
    {
        var e = Ready(20261017);
        e.State.passives.Clear(); e.State.gear = equipment.ToList();
        e.State.deck = new List<string> { choice, "basic_attack", "basic_guard" };
        e = new GameEngine((RunState)Clone(e.State));
        Enter(e, ZoneKind.Subject);
        var c = e.State.combat;
        c.hand = e.State.deck.ToList(); c.drawPile.Clear(); c.discardPile.Clear(); c.exhaustPile.Clear();
        c.energy = 99; c.enemyHp = c.enemyMaxHp = e.State.hp = e.State.maxHp = 100000;
        return e;
    }

    static void WeaponGrantChoiceIdentity()
    {
        foreach (string choice in new[] { "sua_w", "sissela_e", "aiden_w", "adina_r", "echion_w" })
        {
            var e = WeaponChoiceFixture(choice, "fragarach");
            int before = e.State.deck.Count;
            Check(e.BeginCardChoice(0) && e.CancelCardChoice(), "open/cancel advanced choice beside an equipped D: " + choice);
            string actual = e.State.deck.Single(id => CardInstances.SameType(id, "weapon_dagger"));
            Check(CardInstances.IsInstance(actual), "choice assigns the existing D a physical identity: " + choice);
            var syncProbe = (RunState)Clone(e.State); syncProbe.stage = RunStage.Map;
            var synced = new GameEngine(syncProbe);
            Check(synced.State.deck.Count == before, "opening/canceling " + choice + " must not increase the deck at the next synchronization (before="
                + before + ", after=" + synced.State.deck.Count + ", D=" + synced.State.deck.Count(id => CardInstances.SameType(id, "weapon_dagger")) + ")");
            // This assertion failed before the fix: the deck held weapon_dagger~uN,
            // but its grant ledger still held weapon_dagger. The next sync appended D.
            Check(e.State.weaponGrantedCards.SequenceEqual(new[] { actual }), "choice identity updates the equipment grant atomically: " + choice);
            var c = e.State.combat; int rng = e.State.rngState;
            var loaded = new GameEngine((RunState)Clone(e.State));
            Check(loaded.State.deck.SequenceEqual(e.State.deck) && loaded.State.combat.hand.SequenceEqual(c.hand)
                && loaded.State.rngState == rng, "combat reload preserves references, piles and RNG: " + choice);
            loaded.State.stage = RunStage.Map;
            for (int reload = 0; reload < 3; ++reload)
            {
                loaded = new GameEngine((RunState)Clone(loaded.State));
                Check(loaded.State.deck.Count == before && loaded.State.weaponGrantedCards.SequenceEqual(new[] { actual })
                    && loaded.State.deck.Contains(actual), "between-node synchronization preserves exactly one identified D: " + choice + ":" + reload);
            }
        }
        WeaponGrantEarnedCopiesAndAlex();
        WeaponGrantForesightAndRecovery();
        WeaponGrantLegacyAndReplacement();
    }

    static void WeaponGrantEarnedCopiesAndAlex()
    {
        var e = WeaponChoiceFixture("sua_w", "fragarach", "fragarach");
        // This is an independently earned matching D, before both equipment grants.
        e.State.deck.Insert(0, "weapon_dagger"); e.State.combat.hand.Insert(0, "weapon_dagger");
        Check(e.BeginCardChoice(1) && e.CancelCardChoice(), "choice with an earned D and two identical equipped D copies");
        string earned = e.State.deck[0]; var grants = e.State.weaponGrantedCards.ToArray();
        Check(CardInstances.IsInstance(earned) && grants.Length == 2 && grants.Distinct().Count() == 2
            && grants.All(CardInstances.IsInstance) && !grants.Contains(earned), "only the two grants acquire equipment provenance; earned D stays independent");
        Check(!e.IsEquipmentGrantedCard(0) && grants.All(reference => e.IsEquipmentGrantedCard(e.State.deck.IndexOf(reference))), "identified earned D is removable and both exact grants are protected");
        e.State.stage = RunStage.Campfire; e.State.campChoice = 1; e.State.campRemovalsRemaining = 2;
        Check(e.RemoveCampCards(new[] { 0 }), "the independently earned D can still be removed at a camp");
        e = new GameEngine((RunState)Clone(e.State));
        Check(e.State.weaponGrantedCards.SequenceEqual(grants) && grants.All(e.State.deck.Contains)
            && e.State.deck.Count(id => CardInstances.SameType(id, "weapon_dagger")) == 2, "removing the earned D neither removes nor re-creates either equipment grant");

        var alex = Ready(20261018); alex.State.passives = new List<string> { "alex_p" };
        alex.State.gear = new List<string> { "fragarach", "fragarach" };
        alex.State.deck = new List<string> { "sua_w", "basic_attack", "basic_guard" };
        alex = new GameEngine((RunState)Clone(alex.State)); Enter(alex, ZoneKind.Subject);
        var c = alex.State.combat; c.hand = alex.State.deck.ToList(); c.drawPile.Clear(); c.energy = 99;
        var expected = WeaponIdentity.EquipmentCards(alex.State.gear, true); int size = alex.State.deck.Count;
        Check(alex.BeginCardChoice(0) && alex.CancelCardChoice(), "Alex opens and cancels an advanced choice with four equipment grants");
        Check(alex.State.weaponGrantedCards.Count == 4 && alex.State.weaponGrantedCards.Distinct().Count() == 4
            && alex.State.weaponGrantedCards.Select(CardInstances.BaseId).SequenceEqual(expected), "both weapons and their distinct Alex alternates retain exact provenance");
        alex.State.stage = RunStage.Map;
        for (int i = 0; i < 12; ++i)
        {
            alex = new GameEngine((RunState)Clone(alex.State));
            Check(alex.State.deck.Count == size && alex.State.weaponGrantedCards.Count == 4
                && alex.State.weaponGrantedCards.All(alex.State.deck.Contains), "repeated save/load never multiplies Alex grants: " + i);
        }
        alex.State.stage = RunStage.PassiveChoice; alex.State.pendingPassive = "isaac_p";
        Check(alex.ReplacePassive(0) && alex.State.weaponGrantedCards.Count == 2 && alex.State.deck.Count == size - 2,
            "replacing Alex removes exactly his two identified alternate grants");
    }

    static void WeaponGrantForesightAndRecovery()
    {
        var e = WeaponChoiceFixture("adina_r", "fragarach"); var c = e.State.combat;
        c.hand = new List<string> { "adina_r", "basic_attack", "basic_guard" };
        c.drawPile = new List<string> { "weapon_dagger" };
        Check(e.BeginCardChoice(0) && e.CancelCardChoice(), "foresight identifies an equipped D among its draw-pile candidates");
        string actual = c.drawPile.Single();
        Check(CardInstances.IsInstance(actual) && e.State.weaponGrantedCards.SequenceEqual(new[] { actual }), "draw-pile identity also renames the same equipment grant");
        int size = e.State.deck.Count; e.State.stage = RunStage.Map;
        e = new GameEngine((RunState)Clone(e.State));
        Check(e.State.deck.Count == size && e.State.weaponGrantedCards.SequenceEqual(new[] { actual }), "foresight does not append a second D between battles");

        e = WeaponChoiceFixture("sua_w", "fragarach"); c = e.State.combat;
        // An actual pre-fix saved battle: the physical D was renamed, but the
        // equipment ledger was still raw. Recovery must reconnect, not add/delete.
        string identified = CardInstances.CreateReference(e.State, "weapon_dagger");
        e.State.deck[e.State.deck.IndexOf("weapon_dagger")] = identified;
        c.hand[c.hand.IndexOf("weapon_dagger")] = identified;
        var before = (RunState)Clone(e.State); int rng = e.State.rngState;
        e = new GameEngine((RunState)Clone(e.State));
        Check(e.State.deck.SequenceEqual(before.deck) && e.State.combat.hand.SequenceEqual(before.combat.hand)
            && e.State.combat.drawPile.SequenceEqual(before.combat.drawPile) && e.State.rngState == rng,
            "pre-fix in-combat recovery preserves every card, its pile and RNG");
        Check(e.State.weaponGrantedCards.SequenceEqual(new[] { identified }), "pre-fix in-combat recovery reconnects the missing raw grant to its identified copy");
        e.State.stage = RunStage.Map; e = new GameEngine((RunState)Clone(e.State));
        Check(e.State.deck.SequenceEqual(before.deck) && e.State.deck.Count(id => CardInstances.SameType(id, "weapon_dagger")) == 1,
            "recovered saved battle reaches the map without generating a duplicate");

        // An already synchronized old save is ambiguous: this same inventory
        // could include a legitimately acquired D. Never delete it by card type.
        before.weaponGrantedCards = new List<string> { "weapon_dagger" }; before.deck.Add("weapon_dagger"); before.stage = RunStage.Map;
        e = new GameEngine((RunState)Clone(before));
        Check(e.State.deck.SequenceEqual(before.deck) && e.State.deck.Contains(identified)
            && !e.IsEquipmentGrantedCard(e.State.deck.IndexOf(identified)), "already-existing independent or ambiguous D is preserved and remains removable");
    }

    static void WeaponGrantLegacyAndReplacement()
    {
        var e = Ready(20261019); e.State.passives = new List<string> { "alex_p" };
        e.State.gear = new List<string> { "fragarach", "fragarach" };
        var expected = WeaponIdentity.EquipmentCards(e.State.gear, true);
        e.State.deck = new[] { "basic_attack", "weapon_hammer", "weapon_dagger" }.Concat(expected).ToList();
        e.State.weaponGrantVersion = 0; e.State.weaponGrantedCards = null;
        e = new GameEngine((RunState)Clone(e.State));
        Check(e.State.deck.Count == expected.Count + 3 && e.State.weaponGrantedCards.SequenceEqual(expected)
            && e.State.deck.Count(id => id == "weapon_dagger") == 3 && e.State.deck.Contains("weapon_hammer"), "legacy inference includes existing Alex grants and preserves earned D copies");

        e = WeaponChoiceFixture("sua_w", "fragarach");
        Check(e.BeginCardChoice(0) && e.CancelCardChoice(), "identify a D before a same-class weapon replacement");
        string reference = e.State.weaponGrantedCards.Single();
        // Preserve even a legacy-upgraded D if its equipped family stays the same.
        e.State.cardUpgrades.Add(new CardUpgradeSelection { cardId = "weapon_dagger", instanceId = reference, branchId = "legacy" });
        e.State.stage = RunStage.Campfire; e.State.campChoice = 2; e.State.campActions = 3;
        var same = GameDatabase.Gear.First(item => item.slot == GearSlot.Weapon && item.weaponClass == "단검");
        e.State.objects.Add(same.objectId);
        Check(e.Craft(same.id, 0) && e.State.weaponGrantedCards.SequenceEqual(new[] { reference })
            && e.CardUpgradeLevel(reference) == 1, "same-family weapon replacement preserves the identified legacy-upgraded grant");
        var different = GameDatabase.Gear.First(item => item.slot == GearSlot.Weapon && item.weaponClass == "망치");
        e.State.objects.Add(different.objectId);
        Check(e.Craft(different.id, 0) && !e.State.deck.Contains(reference)
            && e.State.weaponGrantedCards.SequenceEqual(new[] { WeaponIdentity.CardFor(different.weaponClass) }), "different-family replacement removes only the old exact grant and adds the new D");
    }
}
#endif
