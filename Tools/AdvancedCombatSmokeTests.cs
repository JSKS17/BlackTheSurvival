#if LUMIA_TEST_RUNNER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lumia;

public static partial class CoreSmokeTests
{
    static void AdvancedCombatMechanics()
    {
        AdvancedPlayerChoices();
        AdvancedChargingAndPersistence();
        AdvancedForesightAndSacrifice();
        AdvancedXAndHealthScaling();
        AdvancedCompanionCombat();
        AdvancedEnemyParity();
        AdvancedChoiceIdentityRegressions();
        AdvancedEnemyCancellationRegressions();
    }

    static GameEngine AdvancedFixture(params string[] hand)
    {
        var e = MechanicsFixture(); var c = e.State.combat;
        e.State.hp = e.State.maxHp = c.enemyHp = c.enemyMaxHp = 100000;
        e.State.deck = hand.ToList(); c.hand = hand.ToList();
        c.enemyGear.Clear(); c.chargedCards.Clear(); c.enemyChargedCards.Clear(); c.enemyAdvancedActions.Clear();
        return e;
    }
    static void AdvancedRound(GameEngine e)
    {
        Check(e.BeginEndTurn(), "advanced fixture finishes its player turn");
        int remaining = 40;
        while (e.State.stage == RunStage.Combat && e.State.combat.enemyTurn && remaining-- > 0)
            Check(e.AdvanceEnemyAction(), "advanced fixture advances a finite enemy action");
        Check(remaining > 0, "advanced fixture never loops indefinitely while returning to the player");
    }
    static CombatAction AdvancedLast(GameEngine e, string id, bool enemy = false)
    {
        return e.CombatActions.Last(x => x.cardId == id && x.enemy == enemy && x.kind == null);
    }
    static List<string> AdvancedCopies(GameEngine e)
    {
        var c = e.State.combat;
        return c.hand.Concat(c.drawPile).Concat(c.discardPile).Concat(c.exhaustPile)
            .Concat(c.chargedCards.Where(x => !c.hand.Contains(x.reference)).Select(x => x.reference))
            .Concat(string.IsNullOrEmpty(c.retainedCardId) ? new string[0] : new[] { c.retainedCardId }).ToList();
    }
    static void AdvancedEnemyCast(GameEngine e, string id, AdvancedEnemyAction action = null, int energy = 99)
    {
        var c = e.State.combat; c.enemyTurn = true; c.enemyAvailableEnergy = energy; c.enemyActionIndex = 0;
        c.enemyPlan = new List<string> { id }; c.enemyPlanCosts = new List<int> { AdvancedCardMechanics.IsX(GameDatabase.Card(id)) ? energy : e.CardCost(id) };
        c.enemyPlanFreeCast = new List<bool> { false }; c.enemyAdvancedActions = new List<AdvancedEnemyAction> { action ?? new AdvancedEnemyAction() };
        Check(e.AdvanceEnemyAction(), "enemy executes advanced fixture card: " + id);
    }

    static void AdvancedPlayerChoices()
    {
        var e = AdvancedFixture("sissela_e", "basic_attack", "basic_attack"); var c = e.State.combat;
        int energy = c.energy, rng = e.State.rngState, hp = c.enemyHp;
        var original = FieldSnapshot(GameDatabase.Card("sissela_e"));
        Check(e.BeginCardChoice(0) && e.PendingCardChoice.kind == "target", "Sissela E opens a target choice");
        Check(!e.PlayCard(1) && !e.BeginEndTurn(), "an unresolved target choice prevents other plays and ending the turn");
        Check(e.PendingCardChoice.handReferences.Distinct().Count() == 2
            && e.PendingCardChoice.handReferences.All(CardInstances.IsInstance),
            "identical selectable cards receive distinct physical identities without upgrading");
        Check(!e.ResolveCardChoice("invalid") && e.PendingCardChoice != null && c.energy == energy,
            "invalid target modes keep the selection open and spend no cost");
        Check(e.CancelCardChoice() && e.PendingCardChoice == null && c.energy == energy && c.enemyHp == hp
            && e.State.rngState == rng && e.State.deck.All(id => string.IsNullOrEmpty(CardInstances.UpgradeBranch(e.State, id))),
            "canceling a card choice spends no resources, changes no outcome and does not upgrade identified copies");
        Check(e.BeginCardChoice(0) && e.ResolveCardChoice("self") && c.block == 12 && c.enemyHp == hp
            && !StatusMechanics.Has(c.enemyStatuses, "stun"),
            "self-targeted Sissela E grants block without damaging or stunning the enemy");
        Check(FieldSnapshot(GameDatabase.Card("sissela_e")) == original,
            "preparing a self-targeted Sissela copy does not mutate its shared card definition");
        Check(AdvancedCopies(e).OrderBy(x => x).SequenceEqual(e.State.deck.OrderBy(x => x)),
            "canceling and then casting target choices conserves physical cards across piles");

        e = AdvancedFixture("sissela_e"); c = e.State.combat; hp = c.enemyHp;
        Check(e.BeginCardChoice(0) && e.ResolveCardChoice("enemy") && c.block == 0 && c.enemyHp < hp
            && StatusMechanics.Has(c.enemyStatuses, "stun"),
            "enemy-targeted Sissela E applies its attack and stun without the self shield");
    }

    static void AdvancedChargingAndPersistence()
    {
        foreach (string id in new[] { "eleven_q", "eleven_w", "eleven_e", "aiden_w", "felix_r" })
        {
            var e = AdvancedFixture(id); var c = e.State.combat;
            c.drawPile.AddRange(new[] { "basic_guard", "basic_guard", "basic_guard", "basic_guard", "basic_guard" });
            e.State.deck.AddRange(c.drawPile);
            int price = e.EffectiveCardCost(id), energy = c.energy, hp = c.enemyHp;
            string definition = FieldSnapshot(GameDatabase.Card(id));
            Check(e.BeginCardChoice(0) && e.ResolveCardChoice("charge"), "a chargeable skill explicitly starts charging: " + id);
            string reference = c.chargedCards.Single().reference;
            Check(c.energy == energy - price && c.hand.Count == 0 && !e.IsCharged(reference)
                && c.enemyHp == hp && !c.discardPile.Contains(reference),
                "charging pays once, deals no immediate damage and reserves the physical copy: " + id);
            Check(c.block == AdvancedCardMechanics.ChargeGuard(GameDatabase.Card(id)),
                "charge-start shielding is limited to the configured defensive charge: " + id);
            var saved = new GameEngine((RunState)Clone(e.State));
            Check(saved.State.combat.chargedCards.Single().reference == reference && !saved.IsCharged(reference),
                "a saved unfinished charge retains its exact copy and ready turn: " + id);
            AdvancedRound(saved); var resumed = saved.State.combat;
            Check(saved.IsCharged(reference) && resumed.hand.Contains(reference) && resumed.hand.Count == 5
                && saved.EffectiveCardCost(reference) == 0,
                "next turn restores the charge while reserving one of the five opening hand slots: " + id);
            resumed.energy = 0;
            Check(saved.PlayCard(resumed.hand.IndexOf(reference)) && resumed.energy == 0 && resumed.chargedCards.Count == 0,
                "a completed charge resolves once for zero additional energy: " + id);
            if (id == "eleven_q" || id == "eleven_e")
                Check(SkillMechanics.Resource(resumed.playerSkills, "일레븐", "slider") == 2,
                    "charged Eleven offense creates two burgers on hit: " + id);
            if (id == "eleven_w")
                Check(resumed.enemyStatuses.effects.Any(x => x.kind == "taunt" && x.remaining == 2)
                    && resumed.playerSkills.effects.Any(x => x.key == "fork" && x.remaining == 3),
                    "charged Eleven W extends both taunt and counter duration by one turn");
            if (id == "aiden_w" || id == "felix_r")
                Check(StatusMechanics.Has(resumed.enemyStatuses, id == "aiden_w" ? "root" : "stun"),
                    "full charge unlocks source-specific crowd control: " + id);
            Check(FieldSnapshot(GameDatabase.Card(id)) == definition,
                "charged preparation never adds damage, burgers or status duration to the global definition: " + id);
            Check(AdvancedCopies(saved).OrderBy(x => x).SequenceEqual(saved.State.deck.OrderBy(x => x)),
                "charging, save/load and free release conserve the deck's physical copies: " + id);
        }
        var immediate = AdvancedFixture("aiden_w");
        Check(immediate.BeginCardChoice(0) && immediate.ResolveCardChoice("now")
            && !StatusMechanics.Has(immediate.State.combat.enemyStatuses, "root"),
            "immediate Aiden W remains an ordinary attack without full-charge root");
    }

    static void AdvancedForesightAndSacrifice()
    {
        var e = AdvancedFixture("adina_r", "nia_q", "nia_q"); var c = e.State.combat;
        c.drawPile.AddRange(new[] { "basic_guard", "aya_q", "nia_e", "nia_w", "isaac_e" });
        e.State.deck.AddRange(c.drawPile);
        string upgradedHand = CardInstances.CreateReference(e.State, "nia_q"), upgradedDraw = CardInstances.CreateReference(e.State, "isaac_e");
        c.hand[2] = upgradedHand; e.State.deck[2] = upgradedHand;
        c.drawPile[c.drawPile.Count - 1] = upgradedDraw; e.State.deck[e.State.deck.Count - 1] = upgradedDraw;
        e.State.cardUpgrades.Add(new CardUpgradeSelection { cardId = "nia_q", instanceId = upgradedHand, branchId = "power" });
        e.State.cardUpgrades.Add(new CardUpgradeSelection { cardId = "isaac_e", instanceId = upgradedDraw, branchId = "power" });
        int count = c.hand.Count, energy = c.energy;
        Check(e.BeginCardChoice(0) && e.PendingCardChoice.kind == "foresight", "Adina R opens a foresight exchange");
        var choice = e.PendingCardChoice;
        Check(choice.drawReferences.Select(CardInstances.BaseId).SequenceEqual(new[] { "isaac_e", "nia_w", "nia_e" }),
            "foresight reveals the actual next three cards from the draw pile's top");
        Check(!e.ResolveCardChoice("swap", new List<string> { choice.handReferences[0] }, c.drawPile[0])
            && c.energy == energy && e.PendingCardChoice != null,
            "foresight cannot exchange a card below its next-three inspection window");
        string selected = choice.handReferences[1], incoming = choice.drawReferences[0];
        int selectedAt = c.hand.IndexOf(selected), incomingAt = c.drawPile.IndexOf(incoming);
        Check(e.ResolveCardChoice("swap", new List<string> { selected }, incoming)
            && c.hand.Count == count - 1 && c.hand[selectedAt - 1] == incoming && c.drawPile[incomingAt] == selected,
            "foresight swaps only the selected duplicate, preserves positions, then spends the R card");
        Check(c.hand.Count(x => CardInstances.SameType(x, "nia_q")) == 1
            && AdvancedCopies(e).OrderBy(x => x).SequenceEqual(e.State.deck.OrderBy(x => x)),
            "foresight does not erase or multiply a same-type sibling");
        Check(CardInstances.UpgradeBranch(e.State, selected) == "power"
            && CardInstances.UpgradeBranch(e.State, incoming) == "power"
            && c.hand.Any(id => CardInstances.SameType(id, "nia_q") && id != selected),
            "foresight preserves both exchanged upgrades and the untouched unupgraded twin");

        foreach (string id in new[] { "tazia_r", "echion_w" })
        {
            e = AdvancedFixture(id, "basic_attack", "basic_attack", "nia_q", "nia_w"); c = e.State.combat;
            Check(e.BeginCardChoice(0) && e.PendingCardChoice.kind == "sacrifice", "sacrifice card opens explicit selection: " + id);
            var options = e.PendingCardChoice.handReferences.ToList(); int beforeEnergy = c.energy;
            Check(!e.ResolveCardChoice("cast", options) && c.exhaustPile.Count == 0 && c.energy == beforeEnergy,
                "sacrifice rejects more than three cards without partially consuming them: " + id);
            Check(!e.ResolveCardChoice("cast", new List<string> { options[0], options[0] })
                && !e.ResolveCardChoice("cast", new List<string> { e.PendingCardChoice.reference }),
                "sacrifice rejects duplicate references and its own source card: " + id);
            int baseline = e.CardTotalDamage(e.PendingCardChoice.reference), hp = c.enemyHp, block = c.block;
            Check(e.ResolveCardChoice("cast", options.Take(2).ToList()) && options.Take(2).All(c.exhaustPile.Contains),
                "sacrifice exhausts exactly the two explicitly selected copies: " + id);
            if (id == "tazia_r") Check(hp - c.enemyHp == baseline + 8, "Tazia R gains four damage per sacrificed card");
            else Check(c.block - block == e.PlayerCard(id).block + 8, "Echion W converts each sacrificed card into four additional block");
            Check(e.State.deck.Count == 5 && AdvancedCopies(e).OrderBy(x => x).SequenceEqual(e.State.deck.OrderBy(x => x)),
                "sacrifice only changes combat piles and never deletes cards from the permanent deck: " + id);
        }
    }

    static void AdvancedXAndHealthScaling()
    {
        foreach (string id in new[] { "eva_r", "sho_r" })
        {
            var e = AdvancedFixture(id); var c = e.State.combat; c.energy = 4; c.strength = 3;
            int expected = e.CardTotalDamage(id), hp = c.enemyHp;
            Check(e.EffectiveCardCost(id) == 4 && e.PlayCard(0) && c.energy == 0 && hp - c.enemyHp == expected,
                "X skill spends all remaining energy and matches its pre-use damage preview: " + id);
            e = AdvancedFixture(id); c = e.State.combat; c.energy = 5; hp = c.enemyHp;
            c.freeCasts.Add(new FreeCastGrant { cardId = id, uses = 1 });
            Check(e.EffectiveCardCost(id) == 0 && e.PlayCard(0) && c.energy == 5 && c.enemyHp == hp
                && !c.playerSkills.effects.Any(x => x.sourceCard == id),
                "a free X cast has X zero and neither invents energy nor installs free pressure: " + id);
            e = AdvancedFixture(id); c = e.State.combat; c.energy = 0; hp = c.enemyHp;
            Check(e.PlayCard(0) && c.enemyHp == hp && c.energy == 0,
                "zero-energy X cards may be cleared from hand without dealing damage: " + id);
        }
        var full = AdvancedFixture("sissela_r"); var low = AdvancedFixture("sissela_r");
        full.State.maxHp = low.State.maxHp = 100; full.State.hp = 100; low.State.hp = 25;
        int normal = full.CardTotalDamage("sissela_r"), boosted = low.CardTotalDamage("sissela_r");
        Check(boosted - normal == 9 && full.PlayCard(0) && low.PlayCard(0)
            && AdvancedLast(low, "sissela_r").damage - AdvancedLast(full, "sissela_r").damage == 9,
            "Sissela R gains damage from the user's pre-cast missing-health ratio in actual combat");
        var shielded = AdvancedFixture("chiara_w"); var bare = AdvancedFixture("chiara_w");
        shielded.State.maxHp = bare.State.maxHp = 100; shielded.State.hp = bare.State.hp = 50;
        shielded.State.combat.block = 30;
        Check(shielded.PlayCard(0) && bare.PlayCard(0)
            && AdvancedLast(shielded, "chiara_w").damage - AdvancedLast(bare, "chiara_w").damage == 10
            && shielded.State.combat.block - bare.State.combat.block == 30,
            "Chiara W scales damage with existing block without spending it or counting its newly granted shield");
        Check(AdvancedLast(bare, "chiara_w").block == bare.PlayerCard("chiara_w").block + 3,
            "Chiara's additional shield uses pre-cast missing health independently from its damage");
    }

    static void AdvancedCompanionCombat()
    {
        var e = AdvancedFixture("chloe_q"); var c = e.State.combat;
        Check(e.PlayCard(0) && c.playerSkills.companion.hp == 18, "actual Chloe Q deploys the player's separate Nina HP");
        c.enemyPlan = new List<string> { "basic_attack" }; c.enemyPlanCosts = new List<int> { 1 }; c.enemyPlanFreeCast = new List<bool> { false };
        c.enemyAdvancedActions = new List<AdvancedEnemyAction> { new AdvancedEnemyAction() };
        string before = FieldSnapshot(c.playerSkills.companion); int hp = e.State.hp;
        typeof(GameEngine).GetMethod("RefreshEnemyIntent", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(e, null);
        Check(FieldSnapshot(c.playerSkills.companion) == before, "enemy intent inspection never spends live Nina HP");
        AdvancedEnemyCast(e, "basic_attack");
        int ownerDamage = hp - e.State.hp, petDamage = 18 - c.playerSkills.companion.hp;
        Check(ownerDamage > 0 && petDamage > 0 && ownerDamage + petDamage == GameDatabase.Card("basic_attack").damage,
            "actual enemy damage is split between player and Nina rather than duplicated or ignored");
        Check(e.CombatActions.Any(x => x.kind == "companion_damage" && x.companionAbsorbed == petDamage),
            "pet interception produces a separate readable damage event");
        var saved = new GameEngine((RunState)Clone(e.State));
        Check(saved.State.combat.playerSkills.companion.hp == c.playerSkills.companion.hp,
            "actual combat pet HP survives run save/load");
        c.playerSkills.companion.hp = 1; hp = e.State.hp;
        AdvancedEnemyCast(e, "basic_attack");
        Check(c.playerSkills.companion.hp == 0 && hp - e.State.hp == GameDatabase.Card("basic_attack").damage - 1
            && !c.playerSkills.effects.Any(x => x.key == "nina"),
            "Nina death stops support immediately and sends excess attack damage to the owner");

        e = AdvancedFixture("basic_attack"); c = e.State.combat;
        CompanionCast(c.enemySkills, "chloe_q"); int enemyHp = c.enemyHp;
        Check(e.PlayCard(0) && c.enemySkills.companion.hp < 18
            && enemyHp - c.enemyHp + 18 - c.enemySkills.companion.hp == e.CardDamage("basic_attack"),
            "enemy Nina uses the same finite post-block damage sharing as player Nina");
    }

    static void AdvancedEnemyParity()
    {
        var e = AdvancedFixture(); var c = e.State.combat;
        AdvancedEnemyCast(e, "sissela_e", new AdvancedEnemyAction { mode = "self" });
        Check(c.enemyBlock == 12 && !StatusMechanics.Has(c.playerStatuses, "stun")
            && e.State.hp == e.State.maxHp, "enemy Sissela self-target gives defense without harming the player");
        e = AdvancedFixture(); c = e.State.combat;
        AdvancedEnemyCast(e, "sissela_e", new AdvancedEnemyAction { mode = "enemy" });
        Check(c.enemyBlock == 0 && e.State.hp < e.State.maxHp && StatusMechanics.Has(c.playerStatuses, "stun"),
            "enemy Sissela offensive target applies the same exclusive attack/control behavior");
        e = AdvancedFixture(); c = e.State.combat;
        AdvancedEnemyCast(e, "chloe_r");
        Check(c.enemySkills.companion.hp == 12 && c.enemySkills.companion.linkTurns == 2,
            "enemy Chloe R revives Nina with the same HP and link duration");
        e = AdvancedFixture(); c = e.State.combat;
        AdvancedEnemyCast(e, "eleven_q", new AdvancedEnemyAction { mode = "charge" });
        Check(c.enemyChargedCards.Count == 1 && e.State.hp == e.State.maxHp,
            "an enemy charge is visible and reserves its attack without immediate damage");
        var saved = new GameEngine((RunState)Clone(e.State));
        Check(saved.AdvanceEnemyAction(), "enemy charge save advances to the following player turn");
        var sc = saved.State.combat;
        Check(sc.enemyPlan.Contains("eleven_q") && sc.enemyAdvancedActions.Any(x => x.charge == 1)
            && sc.enemyPlanCosts[sc.enemyPlan.IndexOf("eleven_q")] == 0,
            "enemy charging survives a save and plans a free charged release on its next turn");
        Check(saved.BeginEndTurn() && saved.AdvanceEnemyAction() && sc.enemyChargedCards.Count == 0
            && SkillMechanics.Resource(sc.enemySkills, "일레븐", "slider") == 2,
            "the actual next enemy turn releases one charged attack with enhanced burger creation");
        foreach (string id in new[] { "eva_r", "sho_r" })
        {
            e = AdvancedFixture(); c = e.State.combat;
            AdvancedEnemyCast(e, id, new AdvancedEnemyAction { x = 4 }, 4);
            Check(c.enemyAvailableEnergy == 0 && AdvancedLast(e, id, true).damage >= GameDatabase.Card(id).damage * 4,
                "enemy X cards spend their own remaining energy and receive proportional offense: " + id);
        }
    }

    static void AdvancedChoiceIdentityRegressions()
    {
        var duplicate=AdvancedFixture("eleven_q","eleven_q");var dc=duplicate.State.combat;dc.energy=0;
        dc.freeCasts.Add(new FreeCastGrant {cardId="eleven_q",cardReference="eleven_q",uses=1});
        Check(duplicate.BeginCardChoice(1) && duplicate.ResolveCardChoice("now") && dc.energy==0 && dc.hand.Count==1,
            "the clicked second ordinary duplicate keeps its valid exact free grant when choice identities are assigned");
        var e = AdvancedFixture("adina_r", "eleven_q", "basic_attack"); var c = e.State.combat;
        string charge = CardInstances.CreateReference(e.State, "eleven_q");
        c.hand[1] = charge; e.State.deck[1] = charge;
        c.chargedCards.Add(new ChargedCard { reference = charge, readyTurn = c.turn });
        c.drawPile.Add("nia_q"); e.State.deck.Add("nia_q");
        Check(e.BeginCardChoice(0), "foresight may be inspected while a different card is charged");
        string incoming = e.PendingCardChoice.drawReferences.Single(); int energy = c.energy;
        Check(!e.ResolveCardChoice("swap", new List<string> { charge }, incoming)
            && c.hand.Contains(charge) && c.chargedCards.Single().reference == charge && c.energy == energy,
            "foresight cannot tuck a charged card into the draw pile while also retaining its reserved copy");
        Check(e.CancelCardChoice() && AdvancedCopies(e).OrderBy(x => x).SequenceEqual(e.State.deck.OrderBy(x => x)),
            "rejecting a charged outgoing exchange leaves every physical copy conserved");

        e = AdvancedFixture("adina_r", "garnet_r", "basic_attack"); c = e.State.combat;
        var exact = new FreeCastGrant { cardId = "garnet_r", cardReference = "garnet_r", uses = 1 };
        c.freeCasts.Add(exact); c.freeCastSources.Add("garnet_r|garnet_r");
        Check(e.BeginCardChoice(0), "another card choice identifies an ordinary recalled free-use copy");
        string recalled = c.hand.Single(id => CardInstances.SameType(id, "garnet_r"));
        Check(CardInstances.IsInstance(recalled) && exact.cardReference == recalled
            && FreeCastMechanics.Matches(exact, recalled) && e.EffectiveCardCost(recalled) == 0,
            "assigning physical choice identities preserves an existing exact-card free-use grant");
        Check(e.CancelCardChoice(), "the unrelated choice can be canceled without consuming the recalled grant");
        c.energy = 0;
        Check(e.PlayCard(c.hand.IndexOf(recalled)) && c.exhaustPile.Contains(recalled),
            "the identified exact recalled card still plays for free and exhausts only its own copy");
    }

    static void AdvancedEnemyCancellationRegressions()
    {
        var planner = typeof(GameEngine).GetMethod("PlanEnemyTurn", BindingFlags.NonPublic | BindingFlags.Instance);
        var e = AdvancedFixture(); var c = e.State.combat; c.turn = 2;
        c.enemyDeck = new List<string> { "eleven_q", "eleven_q" }; c.enemyDrawPile.Add("eleven_q");
        c.enemyChargedCards.Add(new ChargedCard { reference = "eleven_q", readyTurn = 2 });
        planner.Invoke(e, null);
        Check(c.enemyPlan.Count(id => id == "eleven_q") == 2 && c.enemyAdvancedActions.Count(x => x.charge > 0) == 1
            && c.enemyPlanCosts.Count(x => x == 0) == 1,
            "one completed enemy charge does not turn a second ordinary same-type card into another free charged attack");
        c.enemyTurn = true; c.enemyAvailableEnergy = 99;
        Check(e.AdvanceEnemyAction() && e.AdvanceEnemyAction() && c.enemyChargedCards.Count == 0,
            "enemy charged and ordinary duplicate copies each resolve exactly once");
        var attacks = e.CombatActions.Where(x => x.enemy && x.cardId == "eleven_q" && x.kind == null).ToList();
        Check(attacks.Count == 2 && attacks[0].damage - attacks[1].damage == AdvancedCardMechanics.ChargeBonusDamage(GameDatabase.Card("eleven_q"))
            && c.enemyDiscardPile.Count(id => id == "eleven_q") == 2,
            "only the completed enemy copy receives charge damage and both physical copies return to finite piles");

        e = AdvancedFixture(); c = e.State.combat; c.turn = 2;
        c.enemyDeck = new List<string> { "eleven_q" };
        c.enemyChargedCards.Add(new ChargedCard { reference = "eleven_q", readyTurn = 2 });
        planner.Invoke(e, null); c.enemyTurn = true; c.enemyAvailableEnergy = 99;
        StatusMechanics.Add(c.enemyStatuses, "silence", "daniel_r");
        Check(e.AdvanceEnemyAction() && c.enemyChargedCards.Count == 1,
            "a blocked completed enemy release keeps its paid charge instead of consuming it");
        Check(e.AdvanceEnemyAction() && c.enemyChargedCards.Count == 1
            && c.enemyAdvancedActions.Count(x => x.charge > 0) == 1
            && c.enemyDrawPile.Count + c.enemyDiscardPile.Count + c.enemyExhaustPile.Count == 0,
            "the next enemy plan retains one canceled completed charge without inventing an ordinary duplicate");

        e = AdvancedFixture(); c = e.State.combat; c.turn = 1;
        c.enemyDeck = new List<string> { "eleven_q" }; c.enemyDrawPile.Add("eleven_q");
        planner.Invoke(e, null);
        Check(c.enemyAdvancedActions.Single().mode == "charge", "odd-turn enemy charge is planned before interference");
        c.enemyTurn = true; c.enemyAvailableEnergy = 99;
        StatusMechanics.Add(c.enemyStatuses, "silence", "daniel_r");
        Check(e.AdvanceEnemyAction() && c.enemyChargedCards.Count == 0 && c.enemyAvailableEnergy == 99,
            "a canceled charge start neither pays energy nor deploys a reserved card");
        Check(e.AdvanceEnemyAction()
            && c.enemyDrawPile.Concat(c.enemyDiscardPile).Concat(c.enemyExhaustPile).Count(id => id == "eleven_q") == 1,
            "a canceled enemy charge start returns its physical source copy to the finite deck");

        foreach (bool cancel in new[] { false, true })
        {
            e = AdvancedFixture(); c = e.State.combat; c.turn = 2; c.enemyOpeningEnergy = 99;
            c.enemyDeck = new List<string> { "tazia_r", "basic_attack", "basic_attack" };
            c.enemyDrawPile.AddRange(new[] { "basic_attack", "basic_attack", "tazia_r" });
            planner.Invoke(e, null);
            var action = c.enemyAdvancedActions.First();
            Check(action.sacrificed == 2 && action.sacrificedReferences.Count == 2
                && !c.enemyExhaustPile.Contains("basic_attack"),
                "enemy sacrifice reserves two fodder cards without exhausting them while merely forecasting");
            c.enemyTurn = true; c.enemyAvailableEnergy = 99;
            if (cancel) StatusMechanics.Add(c.enemyStatuses, "silence", "daniel_r");
            Check(e.AdvanceEnemyAction(), "enemy sacrifice source resolves or is blocked after the player's interference");
            if (cancel)
            {
                Check(!action.sacrificeCommitted && c.enemyAvailableEnergy == 99 && !c.enemyExhaustPile.Contains("basic_attack"),
                    "a canceled sacrifice cast spends neither energy nor its reserved fodder");
                Check(e.AdvanceEnemyAction()
                    && c.enemyDrawPile.Concat(c.enemyDiscardPile).Concat(c.enemyExhaustPile).Count(id => id == "basic_attack") == 2,
                    "the two canceled sacrifice cards return to ordinary enemy piles exactly once");
            }
            else Check(action.sacrificeCommitted && c.enemyExhaustPile.Count(id => id == "basic_attack") == 2,
                "successful enemy sacrifice commits both reserved cards only when the source actually casts");
        }
    }
}
#endif
