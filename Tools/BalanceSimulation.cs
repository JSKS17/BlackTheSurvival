#if LUMIA_TEST_RUNNER
using System;
using System.Collections.Generic;
using System.Linq;
using Lumia;

// Honest, fixed-policy runs: all purchases, gear, healing, levels and draws use public game actions.
// The policy uses visible intents and definitions; it never edits engine state or searches RNG.
public static class BalanceSimulation
{
    public static int Run()
    {
        int wins = 0, losses = 0, stalls = 0;
        for (int seed = 1; seed <= 40; ++seed)
        {
            var e = new GameEngine(seed);
            e.SetRunes("diamond", "tempering");
            e.SelectStartingPassive(e.State.passiveOffers.OrderBy(id => PassiveScore(GameDatabase.Passive(id))).Last());
            foreach (var id in e.State.draftOffers.Where(id => CanAcquireCard(e,GameDatabase.Card(id))).OrderByDescending(id => DraftScore(GameDatabase.Card(id))).Take(3).ToArray()) e.ToggleDraft(id);
            e.BeginJourney(); int steps = 0, turns = 0, fights = 0;
            while (e.State.stage != RunStage.Won && e.State.stage != RunStage.Lost && steps++ < 2000)
            {
                switch (e.State.stage)
                {
                    case RunStage.Map:
                        e.EnterNode(e.AvailableNodes().OrderBy(n => ZoneScore(e, n)).Last().lane); if (e.State.stage == RunStage.Combat) fights++;
                        break;
                    case RunStage.Combat:
                        while (e.State.hp <= e.State.maxHp * 0.40 && e.State.foods.Count > 0)
                        {
                            int foodIndex = Enumerable.Range(0,e.State.foods.Count).OrderBy(i => GameDatabase.Food(e.State.foods[i]).fullHeal ? e.State.maxHp : GameDatabase.Food(e.State.foods[i]).heal).Last();
                            if (!e.UseFood(foodIndex)) break;
                        }
                        int plays = 0;
                        while (e.State.stage == RunStage.Combat && plays++ < 30)
                        {
                            var candidates = Enumerable.Range(0, e.State.combat.hand.Count).Where(i => e.CanPlayCard(i)).ToArray();
                            if (candidates.Length == 0) break;
                            int pick = candidates.OrderBy(i => PlayScore(e, GameDatabase.Card(e.State.combat.hand[i]))).Last();
                            e.PlayCard(pick);
                        }
                        if (e.State.stage == RunStage.Combat) { e.EndTurn(); turns++; }
                        break;
                    case RunStage.Rewards:
                        foreach (var id in e.State.rewards.choices.OrderByDescending(x => DraftScore(GameDatabase.Card(x))).ToArray())
                        {
                            if (e.RewardRemainingBudget < e.RewardCardPrice(id) || !CanAcquireCard(e,GameDatabase.Card(id))) continue;
                            if (DraftScore(GameDatabase.Card(id)) >= 7 && e.State.deck.Count < 20) e.ClaimCard(id);
                        }
                        e.FinishRewards(); break;
                    case RunStage.Kiosk:
                        // Prefer two legendary weapons, then armor to turn object purchases into a working build.
                        var plans = new[] { "laevateinn", "juggernaut", "mithril_armor", "titan_armor", "mithril_shield", "mithril_helm", "mithril_boots" };
                        foreach (var id in plans)
                        {
                            var gear = GameDatabase.Equipment(id);
                            int owned = e.State.gear.Count(x => x == id);
                            if (owned > 0 || e.State.gear.Count(x => GameDatabase.Equipment(x).slot == gear.slot) >= 2) continue;
                            if (!e.State.objects.Contains(gear.objectId) && e.State.credits >= GameDatabase.Object(gear.objectId).price + 75) e.BuyObject(gear.objectId);
                        }
                        // Buy only actual stock. Value cooking ingredients at their recipe output only
                        // when a visible nearby camp and spare combined actions make cooking plausible.
                        bool canCookSoon = CanReachCampSoon(e) && e.State.objects.Count < 3 && e.State.hp > e.State.maxHp * .40;
                        while (e.State.foods.Count < 2)
                        {
                            var food = e.KioskFoods.Where(x => e.CanBuyFood(x.id)).OrderByDescending(x => FoodValue(e,x,canCookSoon)).FirstOrDefault();
                            if (food == null || !e.BuyFood(food.id)) break;
                        }
                        e.LeaveKiosk(); break;
                    case RunStage.Campfire:
                        var craftable = CraftPlan(e);
                        if (craftable.Count == 0) e.ChooseCamp(false);
                        else
                        {
                            e.ChooseCamp(true);
                            while (e.State.campActions > 0)
                            {
                                var choice = CraftPlan(e).FirstOrDefault(); if (choice == null) break;
                                e.Craft(choice.id);
                            }
                            while (e.State.campActions > 0)
                            {
                                int foodIndex = Enumerable.Range(0,e.State.foods.Count).Where(i => GameDatabase.Food(GameDatabase.Food(e.State.foods[i]).upgradeTo) != null)
                                    .OrderByDescending(i => GameDatabase.Food(GameDatabase.Food(e.State.foods[i]).upgradeTo).heal - GameDatabase.Food(e.State.foods[i]).heal).DefaultIfEmpty(-1).First();
                                if (foodIndex < 0 || !e.Cook(foodIndex)) break;
                            }
                            e.LeaveCamp();
                        }
                        break;
                    case RunStage.Encounter:
                        // The three authored offers and their rewards are public definitions; no RNG is inspected.
                        if (string.IsNullOrEmpty(e.State.chosenEventId)) e.SelectEncounter(e.State.encounterOffers
                            .OrderBy(id => GameDatabase.Event(id).options.Where(o => AffordableOption(e,o)).Select(o => EventScore(e,o)).DefaultIfEmpty(float.NegativeInfinity).Max()).Last());
                        var ev = GameDatabase.Event(e.State.chosenEventId);
                        var options = Enumerable.Range(0, ev.options.Length).Where(e.CanChooseEventOption).OrderBy(i => EventScore(e, ev.options[i]));
                        e.ChooseEventOption(options.Last());
                        if (e.State.runeChangePending) { e.SetRunes("diamond", "tempering"); e.FinishRuneChange(); }
                        break;
                    case RunStage.PassiveChoice:
                        int index = Enumerable.Range(0, e.State.passives.Count).OrderBy(i => PassiveScore(GameDatabase.Passive(e.State.passives[i]))).First();
                        e.ReplacePassive(PassiveScore(GameDatabase.Passive(e.State.pendingPassive)) > PassiveScore(GameDatabase.Passive(e.State.passives[index])) ? index : -1);
                        break;
                }
            }
            if (e.State.stage == RunStage.Won) wins++; else if (e.State.stage == RunStage.Lost) losses++; else stalls++;
            Console.WriteLine("seed=" + seed + " result=" + e.State.stage + " act=" + e.State.act + " row=" + e.State.row + " level=" + e.State.level + " HP=" + e.State.hp + "/" + e.State.maxHp + " fights=" + fights + " turns=" + turns + " gear=" + e.State.gear.Count + " deck=" + e.State.deck.Count);
        }
        Console.WriteLine("Honest policy: " + wins + "/40 escaped, " + losses + " defeated, " + stalls + " stalled. No engine state was modified.");
        return stalls == 0 && wins > 0 ? 0 : 1;
    }

    private static float PassiveScore(PassiveDef p)
    {
        if (p == null) return 0;
        return p.amount * (p.trigger == "turn_block" ? 2 : p.trigger == "turn_heal" ? 2.5f : p.trigger == "skill_bonus" ? 2 : p.trigger == "attack_bonus" ? 1.7f : p.trigger == "kill_heal" ? 0.5f : p.trigger == "battle_start_block" ? 0.4f : 0.1f);
    }
    private static float DraftScore(CardDef c)
    {
        return (c.damage * Math.Max(1, c.hits) + c.draw * 3 + c.energy * 5 + c.block * 0.5f + c.heal * 0.4f + c.strength * 3 + c.weak * 2 + c.evasion * 0.08f) / Math.Max(1, c.cost);
    }
    private static float PlayScore(GameEngine e, CardDef c)
    {
        float incoming = Math.Max(0, e.State.combat.intentDamage - e.State.combat.block);
        float missing = e.State.maxHp - e.State.hp;
        float damage = Math.Min(e.State.combat.enemyHp, e.CardTotalDamage(c.id));
        float block = Math.Min(incoming, c.block) * (e.State.hp < incoming * 2 ? 1.8f : 0.65f);
        float setup=0;
        foreach(var rule in c.mechanics?.rules ?? new SkillRule[0])
        {
            if(!string.IsNullOrEmpty(rule.conditionKey) && SkillMechanics.Resource(e.State.combat.playerSkills,c.owner,rule.conditionKey)<rule.conditionAmount)continue;
            if(rule.op=="delayed_damage")setup+=rule.amount*.5f;
            if(rule.op=="summon" || rule.op=="bleed" || rule.op=="burn")setup+=rule.amount*Math.Min(2,rule.duration)*.5f;
            if(rule.op=="empower_basic")setup+=rule.amount*.5f;
            if(rule.op=="gain" || rule.op=="set")setup+=1;
        }
        float control=c.statuses.Any(x=>new[]{"stun","suppression","freeze","silence","polymorph"}.Contains(x.key))?incoming*.2f:0;
        return (damage + setup + control + block + Math.Min(missing, c.heal) * 0.8f + c.evasion * incoming / 200 + (e.State.combat.energy > c.cost ? c.draw * 2 : 0) + c.energy * 5 + c.poison * 2 + c.strength * 3 + c.weak * incoming / 8) / Math.Max(1, e.EffectiveCardCost(c.id));
    }
    private static float ZoneScore(GameEngine e, MapNode n)
    {
        return n.kind == ZoneKind.Boss ? 1000 : n.kind == ZoneKind.Campfire ? (e.State.objects.Count > 0 || e.State.hp < e.State.maxHp * 0.75 ? 100 : 50)
            : n.kind == ZoneKind.Kiosk ? (e.State.credits >= 275 ? 90 : 10)
            : n.kind == ZoneKind.Subject ? 70 : n.kind == ZoneKind.Wildlife ? 60 : 30;
    }
    private static List<GearDef> CraftPlan(GameEngine e)
    {
        return GameDatabase.Gear.Where(g => e.State.objects.Contains(g.objectId) && !e.State.gear.Contains(g.id) && e.State.gear.Count(id => GameDatabase.Equipment(id).slot == g.slot) < 2)
            .OrderByDescending(g => g.slot == GearSlot.Weapon ? 100 + g.attack : g.health + g.block * 4 + g.attack * 3 + g.evasion * 0.25f).ToList();
    }
    private static float EventScore(GameEngine e, EventOption o)
    {
        // Permanent energy is useful every remaining turn. Favor it over one material while capacity
        // still limits the card pool; healing can outrank it when the player is badly wounded.
        if(o.effect=="max_energy" || o.effect=="energy")return Math.Max(0,o.amount)*(e.PlayerBaseEnergy<GameDatabase.Cards.Max(c=>c.cost)?80:40);
        return o.effect == "heal" ? Math.Min(o.amount, e.State.maxHp - e.State.hp) : o.effect == "credits" ? o.amount / 3 : o.effect == "object" ? 70 : o.effect == "max_health" ? o.amount * 3
            : o.effect == "card" ? RewardCardScore(e,o.cardId) : o.effect == "upgrade_card" ? 15
            : o.effect=="trade_card"?RewardCardScore(e,o.cardId)-o.amount/20f
            : o.effect=="risky_card"?RewardCardScore(e,o.cardId)-o.amount*.5f
            : o.effect=="trade_object"?45-o.amount/15f
            : o.effect=="risky_object"?50-o.amount*.5f
            : o.effect=="food"?10:0;
    }
    private static bool CanAcquireCard(GameEngine e,CardDef card)
    {
        return card!=null && (card.cost<=e.PlayerBaseEnergy || e.State.deck.Select(GameDatabase.Card).Any(source=>source!=null && source.freeCastCount>0 && (source.freeCastTargets ?? new string[0]).Contains(card.id)));
    }
    private static float RewardCardScore(GameEngine e,string id) { var card=GameDatabase.Card(id);return CanAcquireCard(e,card)?DraftScore(card):0; }
    private static bool AffordableOption(GameEngine e,EventOption option)
    {
        if(option.effect=="trade_card" || option.effect=="trade_object")return e.State.credits>=Math.Max(0,option.amount);
        if(option.effect=="risky_card" || option.effect=="risky_object")return e.State.hp>Math.Max(0,option.amount);
        if(option.effect=="remove_card")return e.State.deck.Count>5 && e.State.deck.Any(id=>GameDatabase.Card(id)?.category!="weapon");
        if(option.effect=="upgrade_card")return e.State.deck.Any(id=>GameDatabase.Card(id)?.category!="weapon" && !e.State.upgrades.Contains(id));
        return true;
    }
    private static bool CanReachCampSoon(GameEngine e)
    {
        var here=e.State.map.FirstOrDefault(n=>n.id==e.State.activeNodeId);
        return here!=null && e.State.map.Any(n=>n.act==here.act && n.kind==ZoneKind.Campfire && n.row>here.row && n.row<=here.row+2 && Math.Abs(n.lane-here.lane)<=n.row-here.row);
    }
    private static float FoodValue(GameEngine e,FoodDef food,bool canCookSoon)
    {
        var cooked=canCookSoon?GameDatabase.Food(food.upgradeTo):null;
        return (cooked==null?food.heal:cooked.heal)/(float)Math.Max(1,e.FoodPrice(food.id));
    }
}
#endif
