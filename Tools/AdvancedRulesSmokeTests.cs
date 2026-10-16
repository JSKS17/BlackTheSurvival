#if LUMIA_TEST_RUNNER
using System;
using System.Collections.Generic;
using System.Linq;
using Lumia;

public static partial class CoreSmokeTests
{
    public static void AdvancedCardRuleDefinitions()
    {
        var expectedCharges = new[] { "eleven_q", "eleven_w", "eleven_e", "aiden_w", "felix_r" };
        Check(GameDatabase.Cards.Where(AdvancedCardMechanics.CanCharge).Select(card => card.id).OrderBy(id => id)
            .SequenceEqual(expectedCharges.OrderBy(id => id)), "only original channel skills receive optional hand-card charging");
        Check(GameDatabase.Cards.Where(AdvancedCardMechanics.IsX).Select(card => card.id).OrderBy(id => id)
            .SequenceEqual(new[] { "eva_r", "sho_r" }), "X skills are the two original sustained-energy attacks");
        foreach (var card in GameDatabase.Cards) {
            string summary = AdvancedCardMechanics.Summary(card), description = AdvancedCardMechanics.Description(card);
            Check((summary.Length == 0) == (description.Length == 0), "advanced summary and full rule coverage agree: " + card.id);
            if (summary.Length > 0) Check(!description.Contains("이면:") && summary.Length < 65,
                "advanced rules use readable sentences and compact summaries: " + card.id);
        }

        var sissela = GameDatabase.Card("sissela_e");
        var self = AdvancedCardMechanics.PreparedCard(sissela, "self");
        Check(self.block == 12 && self.damage == 0 && self.statuses.Length == 0 && self.mechanics.rules.Length == 0,
            "self pull grants defense and cannot retain enemy damage, stun or conditional attack effects");
        Check(AdvancedCardMechanics.SuppressesAttack(self) && !AdvancedCardMechanics.SuppressesAttack(sissela),
            "self pull explicitly suppresses external attack empowerment and cross-subject attack bonuses");
        var enemy = AdvancedCardMechanics.PreparedCard(sissela, "enemy");
        Check(enemy.block == 0 && enemy.damage == 7 && enemy.statuses.Any(rule => rule.key == "stun")
            && enemy.mechanics.rules.Any(rule => rule.op == "bonus_damage" && rule.conditionKey == "wilson"),
            "enemy pull retains Wilson's attack setup and stun without self defense");
        Check(sissela.damage == 7 && sissela.block == 0 && sissela.statuses.Any(rule => rule.key == "stun"),
            "target selection never mutates the shared database definition");

        foreach (string id in expectedCharges) {
            var source = GameDatabase.Card(id);
            var normal = AdvancedCardMechanics.PreparedCard(source);
            var charged = AdvancedCardMechanics.PreparedCard(source, charge: 1);
            var repeated = AdvancedCardMechanics.PreparedCard(source, charge: 500);
            Check(charged.damage == source.damage + AdvancedCardMechanics.ChargeBonusDamage(source)
                && repeated.damage == charged.damage, "charging has exactly one damage stage: " + id);
            Check(normal.damage == source.damage && !ReferenceEquals(charged, source), "normal cast remains available without modifying shared values: " + id);
            if (id == "eleven_q" || id == "eleven_e") {
                Check(charged.mechanics.rules.Single(rule => rule.op == "gain" && rule.key == "slider").amount == 2
                    && source.mechanics.rules.Single(rule => rule.op == "gain" && rule.key == "slider").amount == 1,
                    "charging generates one additional burger on a hit using a copied rule: " + id);
            }
            if (id == "eleven_w") {
                Check(charged.statuses.Single(rule => rule.key == "taunt").duration == 2
                    && source.statuses.Single(rule => rule.key == "taunt").duration == 1,
                    "charged taunt lasts two target turns without changing the base status definition");
                Check(charged.mechanics.rules.Single(rule => rule.op == "counter").duration == 3
                    && source.mechanics.rules.Single(rule => rule.op == "counter").duration == 2
                    && AdvancedCardMechanics.ChargeGuard(source) == 3, "charged fork defense has a small preparation shield and longer finite counter");
            }
            if (id == "aiden_w" || id == "felix_r") {
                string status = id == "aiden_w" ? "root" : "stun";
                Check(!normal.statuses.Any(rule => rule.key == status) && charged.statuses.Count(rule => rule.key == status) == 1
                    && charged.statuses.Single(rule => rule.key == status).onHit,
                    "full-charge control is conditional on a charged successful hit: " + id);
            }
        }
        Check(!GameDatabase.Card("eleven_q").mechanics.rules.Any(rule => rule.key == "burger")
            && !GameDatabase.Card("aiden_w").mechanics.rules.Any(rule => rule.key == "dissipation")
            && !GameDatabase.Card("felix_r").mechanics.rules.Any(rule => rule.key == "hydra"),
            "optional charging replaces the former automatic fake charge damage");
        Check(GameDatabase.Card("eleven_e").movement && AdvancedCardMechanics.PreparedCard(GameDatabase.Card("eleven_e"), charge: 1).movement,
            "Eleven's flinging leap keeps its movement classification in immediate and charged casts");

        foreach (string id in new[] { "eva_r", "sho_r" }) {
            var card = GameDatabase.Card(id);
            var zero = AdvancedCardMechanics.PreparedCard(card, x: 0);
            Check(zero.damage == 0 && zero.block == 0 && zero.heal == 0 && zero.draw == 0 && zero.energy == 0
                && zero.poison == 0 && zero.evasion == 0 && zero.statuses.Length == 0 && zero.mechanics.rules.Length == 0
                && zero.freeCastCount == 0, "zero-energy X cannot get free damage, healing, resources or secondary effects: " + id);
            Check(AdvancedCardMechanics.SuppressesAttack(zero) && AdvancedCardMechanics.IsEmptyX(zero),
                "zero-energy X marks external attack suppression as well as empty own effects: " + id);
            for (int energy = 1; energy <= 8; energy++) {
                var prepared = AdvancedCardMechanics.PreparedCard(card, x: energy);
                Check(prepared.damage == card.damage * energy && prepared.hits == 1 && prepared.cost == energy
                    && prepared.mechanics.rules.Length == card.mechanics.rules.Length,
                    "X scales only its base coefficient and retains one set of resource effects: " + id + " / " + energy);
                Check(AdvancedCardMechanics.XCoefficient(prepared) == card.damage
                    && AdvancedCardMechanics.Summary(prepared) == AdvancedCardMechanics.Summary(card)
                    && AdvancedCardMechanics.Description(prepared) == AdvancedCardMechanics.Description(card),
                    "prepared X text keeps its per-energy coefficient instead of displaying the multiplied total as its coefficient: " + id + " / " + energy);
            }
            Check(card.damage == (id == "eva_r" ? 6 : 7), "X uses a conservative per-energy coefficient: " + id);
            Check(AdvancedCardMechanics.PreparedCard(card, x: int.MaxValue).damage > 0,
                "defensive integer handling cannot wrap enormous X damage negative: " + id);
        }

        var low = GameDatabase.Card("sissela_r");
        Check(AdvancedCardMechanics.ContextualBonus(low, 100, 100, 0).damage == 0
            && AdvancedCardMechanics.ContextualBonus(low, 50, 100, 0).damage == 6
            && AdvancedCardMechanics.ContextualBonus(low, 500, 1000, 0).damage == 6
            && AdvancedCardMechanics.ContextualBonus(low, 0, 100, 0).damage == 12,
            "Sissela damage uses missing-health ratio with a fixed cap rather than maximum-HP inflation");
        var chiara = GameDatabase.Card("chiara_w");
        var chiaraBonus = AdvancedCardMechanics.ContextualBonus(chiara, 50, 100, 18);
        Check(chiaraBonus.damage == 6 && chiaraBonus.block == 3
            && AdvancedCardMechanics.ContextualBonus(chiara, 0, 100, 1000).damage == 10,
            "Chiara uses pre-cast defense for capped damage and missing-health ratio for added defense");
        var lenox = GameDatabase.Card("lenox_q");
        Check(AdvancedCardMechanics.ContextualBonus(lenox, 80, 80, 0).damage == 3
            && AdvancedCardMechanics.ContextualBonus(lenox, 5000, 5000, 0).damage == 8,
            "Lenox preserves original self maximum-health scaling within a small independent cap");
        foreach (string id in new[] { "tazia_r", "echion_w" }) {
            var card = GameDatabase.Card(id);
            Check(AdvancedCardMechanics.SacrificeLimit(card) == 3, "material conversion selects at most three hand cards: " + id);
            for (int count = -1; count <= 5; count++) {
                var bonus = AdvancedCardMechanics.ContextualBonus(card, 100, 100, 0, sacrificed: count);
                int expected = Math.Max(0, Math.Min(3, count)) * 4;
                Check((id == "tazia_r" ? bonus.damage : bonus.block) == expected,
                    "sacrifice benefit is linear, optional and capped: " + id + " / " + count);
            }
        }
        var upgraded = StrategyMechanics.Apply(GameDatabase.Card("eleven_e"), "power");
        Check(AdvancedCardMechanics.PreparedCard(upgraded, charge: 1).damage == upgraded.damage + 3
            && GameDatabase.Card("eleven_e").damage == 9, "charging preserves per-copy upgrades without changing future copies");
        var upgradedReference = StrategyMechanics.Copy(GameDatabase.Card("eleven_e")); upgradedReference.id += "~u17";
        Check(AdvancedCardMechanics.CanCharge(upgradedReference), "advanced identity also resolves upgraded reference suffixes");
        Check(AdvancedCardMechanics.HasForesight(GameDatabase.Card("adina_r")) && GameDatabase.Card("adina_r").cost == 1,
            "foreseeing and swapping consumes a modest action cost");
        Check(AdvancedCardMechanics.PreparedCard(null) == null && AdvancedCardMechanics.Kind(null) == ""
            && AdvancedCardMechanics.ContextualBonus(null, 0, 0, 0).damage == 0, "pure advanced helpers safely accept missing card data");
    }
}
#endif
