#if LUMIA_TEST_RUNNER
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using Lumia;

public static partial class CoreSmokeTests
{
    private static void CombatInformationCatalogAndIsolation()
    {
        var e = SkillBattle(73172); var c = e.State.combat;
        int actions = e.CombatActions.Count;
        foreach (var card in GameDatabase.Cards)
        {
            c.hand = new List<string> { card.id, card.id }; c.energy = 99;
            int expected = e.CardTotalDamage(card.id);
            string before = InformationFingerprint(e.State);
            var p = e.PreviewCardUse(1);
            Check(p.cardId == card.id && p.name == card.name && p.handIndex == 1 && p.inHand, "preview retains the exact hand copy: " + card.id);
            Check(p.currentCost == e.EffectiveCardCost(card.id) && p.canUse == e.CanPlayCard(1), "preview uses actual cost and playable rules: " + card.id);
            Check(p.normalDamage == expected && p.damageNote.Contains("별도"), "forecast reuses actual normal damage and discloses separate effects: " + card.id);
            var enemy = e.PreviewCardUse(card.id, true);
            Check(enemy != null && !enemy.canUse && enemy.cardId == card.id, "enemy preview never permits a player cast: " + card.id);
            Check(before == InformationFingerprint(e.State) && actions == e.CombatActions.Count, "preview leaves every saved field and action queue unchanged: " + card.id);
        }
        var missing = e.PreviewCardUse(999);
        Check(!missing.canUse && missing.unavailableReason.Contains("없"), "invalid hand index explains unavailable card");
        c.hand = new List<string> { "aya_e" }; c.energy = 0;
        c.playerStatuses.effects.Add(new CombatStatus { kind = "slow", remaining = 2 });
        var slowed = e.PreviewCardUse(0);
        Check(slowed.costPenalty == 1 && slowed.currentCost == e.PlayerCard("aya_e").cost + 1 && !slowed.canUse, "movement slow tax is visible in the real cost");
        c.freeCasts.Add(new FreeCastGrant { cardId = "aya_e", uses = 2 });
        var free = e.PreviewCardUse(0);
        Check(free.hasFreeCast && free.freeUses == 2 && free.currentCost == 0 && free.canUse, "free grant overrides zero energy and status cost tax");
        c.playerStatuses.effects.Add(new CombatStatus { kind = "root", remaining = 1 });
        var rooted = e.PreviewCardUse(0);
        Check(!rooted.canUse && rooted.unavailableReason.Contains("속박") && rooted.freeUses == 2, "free cast remains forbidden by root without consuming the grant");
        c.playerStatuses.effects.Clear(); c.enemyTurn = true;
        Check(!e.PreviewCardUse(0).canUse && e.PreviewCardUse(0).unavailableReason.Contains("상대"), "enemy turn is a separate unavailable reason");
    }

    private static void CombatInformationConditionsAndBoss()
    {
        var e = SkillBattle(); var c = e.State.combat; var card = GameDatabase.Card("cathy_q");
        c.hand = new List<string> { card.id };
        var rule = card.freeCastCondition;
        Check(rule != null, "Cathy reset fixture has an authored resource condition");
        var notReady = e.PreviewCardUse(0).conditions.Last(x => x.effect.Contains("무료"));
        Check(!notReady.met, "conditional recast is not advertised as ready without resources");
        c.playerSkills.resources.Add(new SkillResource { owner = card.owner, key = rule.conditionKey, amount = rule.conditionAmount, cap = Math.Max(1, rule.conditionAmount) });
        var ready = e.PreviewCardUse(0).conditions.Last(x => x.effect.Contains("무료"));
        Check(ready.met && ready.requiresHit && ready.result.Contains("적중"), "resource-ready reset still discloses the unknown hit requirement");
        int grant = c.freeCasts.Count, resource = c.playerSkills.resources[0].amount;
        for (int i = 0; i < 10; i++) e.PreviewCardUse(0);
        Check(c.freeCasts.Count == grant && c.playerSkills.resources[0].amount == resource, "preview never grants the prospective reset or spends its resource");
        c.hand = new List<string> { "basic_attack" }; e.State.gear.Add("alexandros");
        Check(e.PreviewCardUse(0).criticalChance == 5, "only the basic attack preview exposes actual equipment critical chance");
        c.hand[0] = "nia_q";
        Check(e.PreviewCardUse(0).criticalChance == 0, "skill preview never borrows basic attack critical chance");
        var profile = BossProfiles.Get("nia"); var original = profile.phases[0];
        try
        {
            var movement = RestrictionFixture(BossGoal.NoMovement); movement.State.combat.hand.Add("aya_e");
            var move = movement.PreviewCardUse(0);
            Check(move.bossViolation && move.canUse && move.bossWarning.Contains("사용 자체"), "boss movement prohibition is explained as a challenge violation, not a hard cast lock");
            var debuff = RestrictionFixture(BossGoal.NoDebuffSkill); debuff.State.combat.hand.Add("hyunwoo_q");
            Check(debuff.PreviewCardUse(0).bossViolation, "debuff boss warning uses the canonical semantic classification");
            var damage = RestrictionFixture(BossGoal.NoHeavyHit, 8); damage.State.combat.hand.Add("basic_attack"); damage.State.combat.strength = 30;
            var heavy = damage.PreviewCardUse(0);
            Check(heavy.bossPossibleViolation && !heavy.bossViolation && heavy.bossWarning.Contains("방어도"), "heavy damage warning distinguishes predicted risk from certain actual violation");
            damage.State.combat.boss.challengeActive = false;
            Check(!damage.PreviewCardUse(0).bossPossibleViolation, "inactive boss restrictions do not produce active warnings");
        }
        finally { profile.phases[0] = original; }
    }

    private static void CombatInformationHistoryAndSave()
    {
        var e = SkillBattle(); var c = e.State.combat;
        e.State.passives.Add("isaac_p"); c.playerTraits = new TraitActorState(); e.State.persistentTraits = c.playerTraits;
        e.State.hp = 900;
        CastSkill(e, "basic_attack");
        Check(e.State.combatHistory.Any(x => x.kind == "card_start" && x.cardId == "basic_attack" && x.paidCost >= 0), "history records card identity and paid cost before resolution");
        Check(e.State.combatHistory.Any(x => x.kind == "card_result" && x.cardId == "basic_attack" && x.damage > 0), "history records actual health damage separately from cast start");
        Check(e.State.combatHistory.Any(x => x.traitId == "isaac_p" && x.title.Contains("패시브")), "history identifies passive pulses instead of attributing them to the card");
        CastSkill(e, "hyunwoo_q");
        Check(e.State.combatHistory.Any(x => x.kind == "debuff" && x.cardId == "hyunwoo_q"), "status application appears in structured history");
        c.energy = 99; c.enemyHp = c.enemyMaxHp = 99999;
        Check(e.BeginEndTurn(), "history fixture enters the paced enemy turn");
        int before = e.State.combatHistory.Count;
        Check(e.AdvanceEnemyAction(), "history fixture resolves an actual enemy action");
        Check(e.State.combatHistory.Skip(before).Any(x => x.enemy && x.kind == "card_start"), "enemy card use is captured before the UI clears effects");
        var restored = new GameEngine((RunState)Clone(e.State));
        Check(InformationFingerprint(e.State.combatHistory) == InformationFingerprint(restored.State.combatHistory), "structured history and ordering survive a save reload during enemy actions");
        Check(e.State.combatHistorySerial == restored.State.combatHistorySerial, "saved sequence serial does not reset on continue");
        e.CombatActions.Clear();
        Check(e.State.combatHistory.Count > 0, "consuming presentation actions never clears saved combat history");
        c.enemyTurn = false; e.State.stage = RunStage.Combat;
        for (int i = 0; i < 140; i++) { c.energy = 99; CastSkill(e, "basic_guard"); }
        Check(e.State.combatHistory.Count == CombatInformation.HistoryLimit, "history has a bounded 240-entry tail");
        Check(e.State.combatHistory.Select(x => x.sequence).Distinct().Count() == e.State.combatHistory.Count && e.State.combatHistory[0].sequence < e.State.combatHistory.Last().sequence, "bounded history preserves unique chronological sequence numbers");
        var legacy = (RunState)Clone(e.State); legacy.combatHistory = null; legacy.combatHistorySerial = 0;
        var migrated = new GameEngine(legacy);
        Check(migrated.State.combatHistory != null && migrated.State.combatHistory.Count == 0, "legacy saves start an empty history without fabricating prior events");
        var oversized = (RunState)Clone(e.State); oversized.combatHistory.AddRange(oversized.combatHistory.Select(x => (CombatLogEntry)Clone(x)).ToArray());
        var normalized = new GameEngine(oversized);
        Check(normalized.State.combatHistory.Count == CombatInformation.HistoryLimit, "restored oversized history is bounded before drawing");
    }

    private static string InformationFingerprint(object value)
    {
        var text = new StringBuilder(); InformationFingerprintValue(value, text); return text.ToString();
    }
    private static void InformationFingerprintValue(object value, StringBuilder text)
    {
        if (value == null) { text.Append("null;"); return; }
        var type = value.GetType();
        if (type.IsPrimitive || type.IsEnum || value is string) { string data = Convert.ToString(value, CultureInfo.InvariantCulture); text.Append(data.Length).Append(':').Append(data).Append(';'); return; }
        if (value is IEnumerable) { text.Append('['); foreach (var item in (IEnumerable)value) InformationFingerprintValue(item, text); text.Append(']'); return; }
        text.Append('{'); foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance).OrderBy(x => x.Name)) { text.Append(field.Name).Append('='); InformationFingerprintValue(field.GetValue(value), text); } text.Append('}');
    }
}
#endif
