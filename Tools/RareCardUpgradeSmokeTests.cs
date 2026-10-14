#if LUMIA_TEST_RUNNER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lumia;

public static partial class CoreSmokeTests
{
    public static void RareCardUpgradesV2()
    {
        RareUpgradeCatalogAndIdentity();
        RareUpgradeCopiesAndPersistence();
        RareUpgradeActualCombatAndRecalls();
        RareUpgradeBoundedUtility();
        RareUpgradeDescriptionNumbers();
        RareUpgradeSelectorContinuity();
    }

    static void RareUpgradeCatalogAndIdentity()
    {
        foreach (var source in GameDatabase.Cards.Where(card => card.category == "skill" && new[] { "Q", "W", "E" }.Contains(card.key)))
        {
            string snapshot = FieldSnapshot(source);
            var rare = RareCardUpgrades.Apply(source);
            Check(!ReferenceEquals(rare, source) && FieldSnapshot(source) == snapshot, "rare upgrade never changes shared original skill: " + source.id);
            Check(rare.id == source.id && rare.name == source.name && rare.owner == source.owner && rare.key == source.key
                && rare.cooldown == source.cooldown && rare.movement == source.movement && rare.exhaust == source.exhaust && rare.hits == source.hits,
                "rare upgrade preserves skill identity, movement and exhaustion: " + source.id);
            Check(rare.freeCastCount == source.freeCastCount && rare.freeCastOnHit == source.freeCastOnHit
                && rare.freeCastNextTurn == source.freeCastNextTurn && rare.freeCastLastSkill == source.freeCastLastSkill
                && rare.freeCastEitherResource == source.freeCastEitherResource && rare.freeCastConditionNote == source.freeCastConditionNote
                && rare.freeCastTargets.SequenceEqual(source.freeCastTargets) && FieldSnapshot(rare.freeCastCondition) == FieldSnapshot(source.freeCastCondition)
                && FieldSnapshot(rare.statuses) == FieldSnapshot(source.statuses),
                "rare upgrade preserves original recast conditions, timing, uses and debuff rules: " + source.id);
            Check(rare.cost >= 0 && rare.cost == (source.cost >= 3 ? source.cost - 1 : source.cost)
                && RareCardUpgrades.Price(source) == 120 + 20 * Math.Max(0, source.cost),
                "rare upgrade has one expensive-card discount and the advertised purchase price: " + source.id);
            var originalRules = source.mechanics?.rules ?? new SkillRule[0];
            var changedRules = rare.mechanics?.rules ?? new SkillRule[0];
            Check(changedRules.Length >= originalRules.Length, "rare upgrade retains every original mechanic: " + source.id);
            for (int i = 0; i < originalRules.Length; i++)
            {
                var oldRule = originalRules[i]; var newRule = changedRules[i];
                string oldIdentity = RareRuleIdentity(oldRule), newIdentity = RareRuleIdentity(newRule);
                Check(oldIdentity == newIdentity, "rare upgrade retains each mechanic condition, cap and duration: " + source.id + "/" + i);
                if (oldRule != null && (oldRule.isState || new[] { "consume", "set", "state_toggle", "state_on", "state_off" }.Contains(oldRule.op)))
                    Check(oldRule.amount == newRule.amount, "rare upgrade never converts states or changes consumption: " + source.id + "/" + i);
                if (newRule != null && !newRule.isState && newRule.op == "gain" && newRule.amount != oldRule.amount)
                    Check(newRule.amount <= Math.Min(8, newRule.cap), "rare resource gain stays within the original resource cap: " + source.id);
            }
            bool changed = rare.damage > source.damage || rare.block > source.block || rare.heal > source.heal
                || rare.evasion > source.evasion || rare.poison > source.poison || rare.weak > source.weak || rare.vulnerable > source.vulnerable
                || rare.cost < source.cost || changedRules.Length > originalRules.Length
                || changedRules.Take(originalRules.Length).Where((rule, index) => rule != null && rule.amount != originalRules[index]?.amount).Any();
            Check(changed, "every representative-capable skill has a meaningful rare improvement: " + source.id);
            if (source.damage > 0 || source.block > 0 || source.heal > 0)
            {
                var ordinary = StrategyMechanics.Apply(source, "power");
                Check((source.damage == 0 || rare.damage > ordinary.damage) && (source.block == 0 || rare.block > ordinary.block)
                    && (source.heal == 0 || rare.heal > ordinary.heal), "rare direct output is stronger than the ordinary + branch: " + source.id);
            }
            Check(!StrategyMechanics.Options(source).Any(option => option.id == RareCardUpgrades.BranchId),
                "ordinary upgrade menu cannot grant the rare branch: " + source.id);
            Check(!string.IsNullOrWhiteSpace(rare.description) && !string.IsNullOrWhiteSpace(DescriptionSummary.CardPreview(rare)),
                "rare skill has generated full and compact descriptions: " + source.id);
        }
    }

    static string RareRuleIdentity(SkillRule rule)
    {
        if (rule == null) return "null";
        return string.Join("|", typeof(SkillRule).GetFields().Where(field => field.Name != "amount")
            .OrderBy(field => field.Name).Select(field => field.Name + "=" + FieldSnapshot(field.GetValue(rule))));
    }

    static void RareUpgradeCopiesAndPersistence()
    {
        var e = StrategyReady(1510);
        e.State.deck = new List<string> { "nia_q", "nia_q", "nia_e" };
        string ordinary = StrategyUpgradeCopy(e, 1, "power");
        int rng = e.State.rngState, before = e.State.deck.Count;
        string first = e.AddRareRewardCard("nia_q"), second = e.AddRareRewardCard("nia_q");
        Check(first != second && CardInstances.SameType(first, second) && e.State.deck.Count == before + 2
            && e.State.rngState == rng, "rare rewards add two independent physical copies without sampling run RNG");
        Check(e.CardUpgradeLevel("nia_q") == 0 && e.CardUpgradeLevel(ordinary) == 1 && e.CardUpgradeLevel(first) == 2
            && e.CardUpgradeSuffix("nia_q") == "" && e.CardUpgradeSuffix(ordinary) == " +" && e.CardUpgradeSuffix(first) == " ++"
            && e.CardUpgradeLabel(first) == RareCardUpgrades.Label,
            "ordinary, + and rare ++ copies have unambiguous levels, suffixes and labels");
        Check(e.PlayerCard(first).damage > e.PlayerCard(ordinary).damage && ReferenceEquals(e.PlayerCard("nia_q"), GameDatabase.Card("nia_q")),
            "rare Q output applies only to its actual rare copy, independently of + and ordinary twins");
        Check(!e.UpgradeCardIndices().Contains(1) && !e.UpgradeCardIndices().Contains(before) && !e.UpgradeCardIndices().Contains(before + 1)
            && e.UpgradeCardIndices().Contains(0), "+ and ++ copies cannot be upgraded again while ordinary twins remain eligible");
        e.State.deck.Add("nia_q");
        var expectedDeck = e.State.deck.ToArray(); var expectedRare = FieldSnapshot(e.PlayerCard(first));
        e = new GameEngine((RunState)Clone(e.State));
        Check(e.State.deck.SequenceEqual(expectedDeck) && FieldSnapshot(e.PlayerCard(first)) == expectedRare
            && e.CardUpgradeLevel(e.State.deck.Last()) == 0 && e.CardUpgradeLevel(second) == 2,
            "save migration preserves rare identity, independent + copies and newly obtained ordinary copies");
        Check(e.AddRareRewardCard("missing_card") == null && e.State.deck.SequenceEqual(expectedDeck), "invalid rare rewards cannot append a broken card");
    }

    static void RareUpgradeActualCombatAndRecalls()
    {
        var e = StrategyReady(1511);
        e.State.deck = new List<string> { "nia_q", "nia_r" };
        string rare = e.AddRareRewardCard("nia_q");
        Enter(e, ZoneKind.Wildlife); var c = e.State.combat;
        c.enemyHp = c.enemyMaxHp = 10000; c.enemyEvasion = 0;
        c.hand = new List<string> { "nia_r" }; c.drawPile.Clear(); c.discardPile = new List<string> { "nia_q", rare }; c.exhaustPile.Clear();
        c.energy = e.EffectiveCardCost("nia_r");
        var inventory = c.hand.Concat(c.discardPile).OrderBy(id => id).ToArray();
        Check(e.PlayCard(0) && c.hand.Contains(rare) && c.discardPile.Contains("nia_q") && e.EffectiveCardCost(rare) == 0,
            "actual Nia R recalls the owned ++ Q at zero energy without changing its ordinary twin");
        e = new GameEngine((RunState)Clone(e.State)); c = e.State.combat;
        int actualDamage = e.CardTotalDamage(rare), health = c.enemyHp;
        Check(e.PreviewCardUse(c.hand.IndexOf(rare)).normalDamage == actualDamage && e.CardUpgradeSuffix(rare) == " ++",
            "saved free rare copy retains its actual damage in the read-only use preview");
        Check(e.PlayCard(c.hand.IndexOf(rare)) && c.enemyHp == health - actualDamage && c.discardPile.Contains(rare)
            && SkillMechanics.Resource(c.playerSkills, "니아", "blocks") == 2,
            "actual ++ Q applies its upgraded damage and bounded resource gain on the recalled physical copy");
        Check(c.hand.Concat(c.drawPile).Concat(c.discardPile).Concat(c.exhaustPile).OrderBy(id => id).SequenceEqual(inventory),
            "using a recalled rare card preserves complete combat pile identity");
        Check(e.State.combatHistory.Any(entry => entry.kind == "card_start" && entry.cardId == rare && entry.title.Contains(" ++"))
            && e.State.combatHistory.Any(entry => entry.kind == "card_result" && entry.cardId == rare && entry.title.Contains(" ++")),
            "rare card start and result history keep both the instance reference and ++ name");

        var garnish = GarnetFixture(); var gc = garnish.State.combat;
        string rareR = garnish.AddRareRewardCard("garnet_r");
        gc.hand = new List<string> { rareR, "garnet_r" }; gc.drawPile.Clear(); gc.discardPile.Clear(); gc.exhaustPile.Clear();
        gc.enemyHp = gc.enemyMaxHp = 1000; gc.energy = garnish.EffectiveCardCost(rareR);
        Check(garnish.PlayCard(0) && gc.freeCasts.Any(grant => grant.cardReference == rareR && grant.uses == 1)
            && gc.hand.Contains(rareR) && garnish.EffectiveCardCost(rareR) == 0 && garnish.EffectiveCardCost("garnet_r") > 0,
            "rare Garnet recast is tied to the exact ++ copy, not an ordinary R already in hand");
        garnish = new GameEngine((RunState)Clone(garnish.State)); gc = garnish.State.combat;
        Check(garnish.BeginEndTurn() && gc.exhaustPile.Contains(rareR) && !gc.exhaustPile.Contains("garnet_r")
            && gc.discardPile.Contains("garnet_r"), "saved unused ++ Garnet recast expires only its exact rare copy");

        var delayed = StrategyReady(1512); delayed.State.deck = new List<string>();
        string delayedRare = delayed.AddRareRewardCard("vanya_q");
        var owned = new[] { delayedRare }; var hand = new List<string>(); var draw = new List<string>(); var discard = new List<string> { delayedRare };
        var grants = new List<FreeCastGrant>(); var sources = new List<string>();
        var pending = new List<PendingFreeCast> { new PendingFreeCast { sourceCardId = "vanya_q", cardId = "vanya_q", uses = 1, readyTurn = 2 } };
        Check(FreeCastMechanics.ActivatePending(pending, 1, owned, grants, sources, hand, draw, discard) == 0
            && hand.Count == 0 && pending.Count == 1, "rare delayed recast cannot activate before the original next-turn boundary");
        Check(FreeCastMechanics.ActivatePending(pending, 2, owned, grants, sources, hand, draw, discard) == 1
            && hand.SequenceEqual(owned) && discard.Count == 0 && pending.Count == 0 && grants.Single().uses == 1,
            "next-turn recast moves the physical ++ card without duplicating it or raising the granted uses");
    }

    static void RareUpgradeBoundedUtility()
    {
        var source = new CardDef { id = "rare_fixture_state", name = "형태 전환", owner = "이렘", key = "E", cost = 0,
            mechanics = new SkillMechanicProfile { rules = new[] { new SkillRule { op = "state_toggle", key = "cat", label = "고양이", isState = true, cap = 1, amount = 1 } } } };
        var rare = RareCardUpgrades.Apply(source); var state = new SkillActorState();
        for (int i = 0; i < 20; ++i) SkillMechanics.AfterCard(state, SkillMechanics.Clone(state), rare, true, paidCost: 0);
        Check(rare.cost == 0 && rare.draw == 0 && rare.energy == 0 && rare.damage == 0 && rare.block == 0
            && source.mechanics.rules.Length == 1 && rare.mechanics.rules[0].cap == 1 && rare.mechanics.rules[0].isState,
            "state-only free utility remains free, preserves its binary form and gains no immediate damage, block or draw loop");
        Check(state.effects.Count(effect => effect.key == "rare_guard") == 1 && state.effects.Single(effect => effect.key == "rare_guard").amount == 4
            && state.effects.Single(effect => effect.key == "rare_guard").remaining == 2,
            "replaying rare utility refreshes one bounded guard instead of stacking permanent protection");
        Check(SkillMechanics.Tick(state).Where(pulse => pulse.kind == "guard").Sum(pulse => pulse.amount) == 4
            && SkillMechanics.Tick(state).Where(pulse => pulse.kind == "guard").Sum(pulse => pulse.amount) == 4
            && SkillMechanics.Tick(state).All(pulse => pulse.kind != "guard"), "rare utility guard produces only two bounded end-turn pulses");
    }

    static void RareUpgradeDescriptionNumbers()
    {
        foreach (string id in new[] { "debi_marlene_q", "debi_marlene_w", "debi_marlene_e" })
        {
            var source = GameDatabase.Card(id); string before = FieldSnapshot(source);
            var rare = RareCardUpgrades.Apply(source);
            var originalDamage = source.mechanics.rules.Where(rule => rule.op == "bonus_damage").ToArray();
            var increased = rare.mechanics.rules.Where(rule => rule.op == "bonus_damage").ToArray();
            Check(increased.Length == 2 && increased.All(rule => rule.amount == originalDamage[0].amount + 2),
                "rare twin skill increases both original color conditions equally: " + id);
            int amount = increased[0].amount;
            string summary = DescriptionSummary.CardPreview(rare), full = CardPresentation.Describe(rare) + "\n" + CardPresentation.Rules(rare);
            Check(summary.Contains("추가 피해 " + amount) && full.Contains("추가 피해를 " + amount + "만큼")
                && source.mechanics.rules.Where(rule => rule.op == "bonus_damage").All(rule => rule.amount == originalDamage[0].amount)
                && FieldSnapshot(source) == before, "rare twin summary and full explanation show the actual extra damage without altering original data: " + id);
            var blue = new SkillActorState();
            blue.resources.Add(new SkillResource { owner = source.owner, key = "stance", amount = 0, cap = 1, isState = true });
            blue.resources.Add(new SkillResource { owner = source.owner, key = "ink", amount = 1, cap = 2 });
            var red = SkillMechanics.Clone(blue); red.resources.First(rule => rule.key == "stance").amount = 1;
            red.resources.First(rule => rule.key == "ink").amount = 2;
            if (source.key != "E")
            {
                blue.resources.First(rule => rule.key == "ink").amount = 2;
                red.resources.First(rule => rule.key == "ink").amount = 1;
            }
            Check(SkillMechanics.Bonuses(blue, rare, true).damage == amount && SkillMechanics.Bonuses(red, rare, true).damage == amount,
                "both real twin-color conditions grant exactly the damage printed in the ++ summary: " + id);
        }
        var nia = RareCardUpgrades.Apply(GameDatabase.Card("nia_r"));
        var battery = nia.mechanics.rules.First(rule => rule.op == "bonus_block" && rule.scaleKey == "battery");
        int maximum = Math.Min(24, battery.amount * battery.cap);
        Check(DescriptionSummary.CardPreview(nia).Contains("배터리 ×" + battery.amount + "(최대" + maximum + ")")
            && CardPresentation.Describe(nia).Contains("VF 배터리 × " + battery.amount + "(최대 " + maximum + ")"),
            "rare Nia R summary and full explanation share the actual bounded battery multiplier");
        var actor = new SkillActorState(); actor.resources.Add(new SkillResource { owner = nia.owner, key = "battery", amount = battery.cap, cap = battery.cap });
        Check(SkillMechanics.Bonuses(actor, nia, true).block == maximum, "rare Nia R printed maximum matches actual bounded shield computation");
    }

    static void RareUpgradeSelectorContinuity()
    {
        foreach (string id in new[] { "tia_w", "adina_q", "adina_w", "adina_e", "adina_r" })
        {
            var source = GameDatabase.Card(id); string sourceSnapshot = FieldSnapshot(source);
            var rare = RareCardUpgrades.Apply(source); string selector = id == "tia_w" ? "brush" : "sign";
            var oldGain = source.mechanics.rules.First(rule => rule.op == "gain" && rule.key == selector);
            var newGain = rare.mechanics.rules.First(rule => rule.op == "gain" && rule.key == selector);
            Check(oldGain.amount == newGain.amount && newGain.amount == 1, "rare upgrade never skips an original brush color or constellation: " + id);
            var ordinaryState = new SkillActorState(); var rareState = new SkillActorState();
            foreach (int expected in new[] { 1, 2, 0, 1, 2, 0 })
            {
                SkillMechanics.AfterCard(ordinaryState, SkillMechanics.Clone(ordinaryState), source, true, paidCost: source.cost);
                SkillMechanics.AfterCard(rareState, SkillMechanics.Clone(rareState), rare, true, paidCost: rare.cost);
                Check(SkillMechanics.Resource(ordinaryState, source.owner, selector) == expected
                    && SkillMechanics.Resource(rareState, source.owner, selector) == expected,
                    "actual ordinary and rare selector casts keep the same six-step cycle: " + id);
            }
            Check(FieldSnapshot(source) == sourceSnapshot, "selector protection leaves the original shared skill unchanged: " + id);
            if (id == "tia_w")
            {
                string summary = DescriptionSummary.CardPreview(rare);
                Check(rare.mechanics.rules.Any(rule => rule.op == "guard" && rule.key == "rare_guard" && rule.amount == 4 && rule.duration == 2)
                    && summary.Contains("붓") && summary.Contains("방어도") && summary.Contains("4") && summary.Contains("2회"),
                    "rare pure brush selector shows both the preserved color cycle and its meaningful bounded guard on the card face");
            }
        }
    }
}
#endif
