#if LUMIA_TEST_RUNNER
using System;
using System.Collections.Generic;
using System.Linq;
using Lumia;

public static partial class CoreSmokeTests
{
    public static void WeaponAndTacticalDamageBalance()
    {
        WeaponDamageCatalogBudget();
        WeaponDamageScopeAndRounding();
        WeaponDamagePlayerAndEnemyForecasts();
        WeaponDamageTimedAndBasicEffects();
        WeaponDamageExternalBuildBonuses();
    }

    // These are the shipped pre-change, cooldown-scaled values, not raw database literals.
    static readonly Dictionary<string, int> PriorWeaponAndTacticalDamage = new Dictionary<string, int>
    {
        { "weapon_glove", 21 }, { "weapon_pistol", 0 }, { "weapon_dagger", 46 }, { "weapon_axe", 11 },
        { "weapon_sword", 26 }, { "weapon_bow", 49 }, { "weapon_hammer", 40 }, { "weapon_rapier", 42 },
        { "weapon_sniper", 64 }, { "weapon_tonfa", 21 }, { "weapon_bat", 32 }, { "weapon_whip", 18 },
        { "weapon_throw", 21 }, { "weapon_shuriken", 0 }, { "weapon_crossbow", 23 }, { "weapon_rifle", 0 },
        { "weapon_dual", 21 }, { "weapon_spear", 32 }, { "weapon_nunchaku", 37 }, { "weapon_guitar", 28 },
        { "weapon_camera", 28 }, { "weapon_arcana", 0 }, { "weapon_vf", 0 }, { "tactical_blink", 0 },
        { "tactical_wind", 0 }, { "tactical_plasma", 50 }
    };

    static void WeaponDamageCatalogBudget()
    {
        var cards = GameDatabase.Cards.Where(card => card.key == "D" || card.key == "F").ToArray();
        Check(cards.Count(card => card.key == "D") == 23 && cards.Count(card => card.key == "F") == 3
            && cards.Select(card => card.id).OrderBy(id => id).SequenceEqual(PriorWeaponAndTacticalDamage.Keys.OrderBy(id => id)),
            "damage audit covers all 23 weapon and three tactical cards, including nondamaging utility cards");
        foreach (var card in cards)
        {
            int previous = PriorWeaponAndTacticalDamage[card.id];
            int expected = (int)Math.Round(previous * .8, MidpointRounding.AwayFromZero);
            Check(card.damage == expected, "D/F cooldown-scaled direct damage is reduced by the integer 80-percent budget: " + card.id);
            Check(card.cost == CardPresentation.Cost(card.cooldown) && card.hits == (card.id == "weapon_whip" ? 2 : 1),
                "D/F original cooldown cost and hit count are preserved: " + card.id);
            Check(card.description == CardPresentation.Describe(card, GameDatabase.Cards),
                "D/F full text is generated after the damage adjustment without stale literals: " + card.id);
            if (card.damage > 0)
                Check(DescriptionSummary.CardPreview(card).Contains(card.damage.ToString()),
                    "compact D/F description displays its current direct damage: " + card.id);
        }
        var ruleAmounts = new Dictionary<string, int>
        {
            { "weapon_tonfa", 3 }, { "weapon_throw", 6 }, { "weapon_shuriken", 6 },
            { "weapon_crossbow", 8 }, { "weapon_rifle", 6 }, { "weapon_arcana", 6 }
        };
        foreach (var entry in ruleAmounts)
        {
            var card = GameDatabase.Card(entry.Key); var rule = card.mechanics.rules.Single();
            Check(rule.amount == entry.Value && card.description.Contains(entry.Value.ToString()),
                "counter, delayed, summon and basic-empowerment damage each use their reduced original amount: " + entry.Key);
        }
        Check(cards.Sum(card => card.mechanics?.rules.Count(rule => new[] { "bonus_damage", "bleed", "burn", "delayed_damage", "summon", "counter", "empower_basic" }.Contains(rule.op)) ?? 0) == 6,
            "all six authored D/F secondary damage rules are included in this audit");
        Check(GameDatabase.Card("weapon_tonfa").block == 27 && GameDatabase.Card("weapon_sword").block == 36
            && GameDatabase.Card("tactical_wind").heal == 40 && GameDatabase.Card("tactical_wind").block == 13
            && GameDatabase.Card("tactical_blink").block == 19 && GameDatabase.Card("tactical_blink").evasion == 60,
            "representative D/F shield, healing and evasion budgets are retained rather than reduced with damage");
        Check(GameDatabase.Card("weapon_vf").mechanics.rules.All(rule => rule.op == "discount" && rule.amount == 1)
            && GameDatabase.Card("weapon_pistol").draw == 3 && GameDatabase.Card("weapon_pistol").energy == 1
            && GameDatabase.Card("weapon_dual").freeCastCount == 1 && GameDatabase.Card("weapon_dual").freeCastOnHit,
            "weapon reload, discounts, free recast and resource benefits keep their original values");
    }

    static void WeaponDamageScopeAndRounding()
    {
        foreach (int amount in new[] { -1, 0, 1, 2, 3, 4, 5, 8, 10, 18, 21, 64, int.MaxValue })
        {
            int expected = amount <= 0 ? amount : (int)Math.Round(amount * .8, MidpointRounding.AwayFromZero);
            Check(WeaponDamageBalance.ReducedDamage(amount) == expected, "D/F nearest-integer scaling is stable and cannot overflow: " + amount);
        }
        var skill = new CardDef { id="fixture_q", key="Q", category="skill", damage=50,
            mechanics=new SkillMechanicProfile { rules=new[] { new SkillRule { op="bonus_damage", amount=10 } } } };
        var basic = new CardDef { id="fixture_atk", key="ATK", category="basic", damage=50 };
        var card = new CardDef { id="fixture_d", key="D", category="weapon", damage=50, cost=4, block=17, heal=13,
            draw=2, energy=1, evasion=25, duration=3, hits=2, exhaust=true, movement=true, poison=3, weak=2,
            vulnerable=2, strength=3, freeCastCount=1, freeCastNextTurn=true, freeCastOnHit=true,
            freeCastTargets=new[] { "fixture_d" }, mechanics=new SkillMechanicProfile {
                rules=new[] {
                    new SkillRule { op="bonus_damage", amount=10, conditionKey="charge", conditionAmount=3,
                        conditionKey2="prepared", conditionAmount2=1, onHit=true, scaleKey="charge", cap=3, delay=2, duration=3 },
                    new SkillRule { op="bleed", amount=5 }, new SkillRule { op="burn", amount=5 },
                    new SkillRule { op="gain", amount=2, cap=4 }, new SkillRule { op="bonus_block", amount=10 },
                    new SkillRule { op="bonus_heal", amount=10 }, new SkillRule { op="discount", amount=2 },
                    new SkillRule { op="hot", amount=5 }, new SkillRule { op="guard", amount=5 },
                    new SkillRule { op="revive", amount=20 }, null
                } } };
        var original = CombatInformation.Copy(card);
        string skillBefore = FieldSnapshot(skill), basicBefore = FieldSnapshot(basic);
        WeaponDamageBalance.Configure(new[] { skill, card, basic, null });
        Check(card.damage == 40 && card.mechanics.rules[0].amount == 8 && card.mechanics.rules[1].amount == 4
            && card.mechanics.rules[2].amount == 4, "conditional direct and persistent damage use the same D budget");
        // Undo only the three intentional damage edits before comparing all other public fields.
        card.damage = original.damage;
        for (int i=0;i<3;i++) card.mechanics.rules[i].amount = original.mechanics.rules[i].amount;
        Check(FieldSnapshot(card) == FieldSnapshot(original),
            "damage adjustment preserves every non-damage field, condition, duration, resource, cost and follow-up rule");
        Check(FieldSnapshot(skill) == skillBefore && FieldSnapshot(basic) == basicBefore,
            "QWER and ordinary attacks retain their complete definitions when sharing the database hook");
        WeaponDamageBalance.Configure(null);
    }

    static void WeaponDamagePlayerAndEnemyForecasts()
    {
        foreach (var entry in PriorWeaponAndTacticalDamage)
        {
            var card = GameDatabase.Card(entry.Key);
            var player = MechanicsFixture(); var combat = player.State.combat;
            combat.hand.Add(card.id); int expected = card.damage * card.hits;
            int rng = player.State.rngState, hp = combat.enemyHp;
            var preview = player.PreviewCardUse(0);
            Check(preview.normalDamage == expected && player.State.rngState == rng,
                "player use preview reads the reduced shared D/F definition without RNG mutation: " + card.id);
            Check(player.PlayCard(0) && hp - combat.enemyHp == expected,
                "actual player D/F direct damage agrees with the reduced forecast: " + card.id);

            var enemy = MechanicsFixture(); combat = enemy.State.combat;
            enemy.State.hp = enemy.State.maxHp = 9999; combat.enemyLevel = 10;
            combat.enemyPlan = new List<string> { card.id }; combat.enemyPlanCosts = new List<int> { card.cost };
            combat.enemyPlanFreeCast = new List<bool> { false };
            expected = card.damage > 0 ? (card.damage + 5) * card.hits : 0;
            preview = enemy.PreviewCardUse(card.id, true);
            Check(preview.normalDamage == expected,
                "enemy preview uses the same reduced D/F base while retaining level attack bonuses: " + card.id);
            Check(enemy.BeginEndTurn(), "enemy D/F fixture begins its scheduled turn: " + card.id);
            hp = enemy.State.hp;
            Check(enemy.AdvanceEnemyAction() && hp - enemy.State.hp == expected,
                "actual enemy D/F direct attack agrees with its shared-base preview: " + card.id);
        }
    }

    static void WeaponDamageTimedAndBasicEffects()
    {
        foreach (string id in new[] { "weapon_throw", "weapon_shuriken", "weapon_crossbow", "weapon_arcana" })
        {
            var card = GameDatabase.Card(id); var actor = new SkillActorState(); var rule = card.mechanics.rules.Single();
            SkillMechanics.AfterCard(actor, SkillMechanics.Clone(actor), card, true);
            Check(actor.effects.Count == 1 && actor.effects[0].amount == rule.amount
                && actor.effects[0].remaining == rule.duration && actor.effects[0].delay == (rule.op == "delayed_damage" ? rule.delay : 0),
                "D installation keeps its original lifetime but stores the reduced pulse damage: " + id);
            var pulse = SkillMechanics.Tick(actor).Single();
            Check(pulse.sourceCard == id && pulse.amount == rule.amount,
                "D installed damage really ticks at the displayed reduced amount: " + id);
            var copy = SkillMechanics.Clone(actor);
            Check(FieldSnapshot(actor) == FieldSnapshot(copy), "D reduced installation persists in a copied save-state actor: " + id);
            if (rule.op == "summon")
                Check(SkillMechanics.Tick(copy).Single().amount == rule.amount && copy.effects.Count == 0,
                    "both D summon turns apply the reduced amount and expire on schedule: " + id);
            else Check(actor.effects.Count == 0, "one-shot D delayed damage expires on its original schedule: " + id);
        }
        var rifle = GameDatabase.Card("weapon_rifle"); var skills = new SkillActorState();
        SkillMechanics.AfterCard(skills, SkillMechanics.Clone(skills), rifle, true);
        Check(SkillMechanics.Bonuses(skills, GameDatabase.Card("basic_attack")).damage == 6,
            "rifle D grants six rather than eight extra damage to the universal basic attack");
        SkillMechanics.AfterCard(skills, SkillMechanics.Clone(skills), GameDatabase.Card("basic_attack"), true);
        Check(SkillMechanics.Bonuses(skills, GameDatabase.Card("basic_attack")).damage == 0,
            "reduced rifle D empowerment still applies to one basic attack only");
        var tonfa = GameDatabase.Card("weapon_tonfa"); skills = new SkillActorState();
        SkillMechanics.AfterCard(skills, SkillMechanics.Clone(skills), tonfa, true);
        Check(SkillMechanics.Counters(skills).Single().amount == 3,
            "tonfa reflection emits its reduced three damage through the actual counter resolver");
    }

    static void WeaponDamageExternalBuildBonuses()
    {
        foreach (string id in new[] { "weapon_glove", "weapon_whip", "tactical_plasma" })
        {
            var e = MechanicsFixture(); var c = e.State.combat; var card = GameDatabase.Card(id);
            int baseDamage = e.CardDamage(id); c.strength = 7;
            Check(e.CardDamage(id) - baseDamage == 7, "D/F damage reduction does not rescale externally gained strength: " + id);
            e.State.level = 18;
            Check(e.CardDamage(id) - baseDamage == 13, "D/F damage reduction preserves level attack gains: " + id);
            e.State.gear.Add("laevateinn");
            Check(e.CardDamage(id) - baseDamage == 13 + GameDatabase.Equipment("laevateinn").attack,
                "D/F damage reduction leaves equipment attack stats at their full existing amount: " + id);
            c.playerTraits.buffs.Add(new TraitBuff { kind="damage_buff", amount=4, remaining=2 });
            Check(e.CardTotalDamage(id) == e.CardDamage(id) * card.hits + 4,
                "D/F damage reduction keeps separately triggered rune/passive/gear damage buffs intact: " + id);
        }
    }
}
#endif
