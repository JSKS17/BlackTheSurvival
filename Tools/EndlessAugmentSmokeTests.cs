#if LUMIA_TEST_RUNNER
using System;
using System.Collections.Generic;
using System.Linq;
using Lumia;

public static partial class CoreSmokeTests
{
    static GameEngine PendingAugment(int cycle = 0, IEnumerable<string> owned = null)
    {
        var e = ReadyMode(RunMode.Endless); e.State.endlessCycle = cycle;
        if (owned != null) e.State.augments.AddRange(owned);
        ModeBossRewards(e, 3); int random = e.State.rngState;
        Check(e.FinishRewards(), "augment fixture confirms the final boss cards");
        if (e.State.stage == RunStage.PassiveChoice) Check(e.ReplacePassive(-1), "augment fixture declines a full passive slot");
        Check(e.State.stage == RunStage.AugmentChoice && e.State.endlessCycle == cycle && e.State.rngState == random,
            "final boss completion offers augments without replacing its map or rolling gameplay randomness");
        return e;
    }
    static void SetAugmentOffer(GameEngine e, string id)
    {
        var value = EndlessAugments.Find(id);
        e.State.stage = RunStage.AugmentChoice; e.State.augmentOfferingCycle = e.State.endlessCycle + 1;
        e.State.augmentSelectedId = null;
        e.State.augmentOffers = new[] { id }.Concat(EndlessAugments.All.Where(x => x.tier == value.tier && x.id != id && EndlessAugments.IsSupport(x)).Select(x => x.id)).Take(3).ToList();
    }
    static GameEngine AugmentBattle(params string[] ids)
    {
        var e = ReadyMode(RunMode.Endless); e.State.augments.AddRange(ids);
        e.State.deck = Enumerable.Repeat("basic_attack", 30).ToList();
        e.AvailableNodes().First(n => n.lane == 1).assignedAnimal = "bear";
        Enter(e, ZoneKind.Wildlife);
        var c = e.State.combat; c.enemyHp = c.enemyMaxHp = 99999; c.energy = 99; c.block = 0;
        c.playerSkills = new SkillActorState(); c.playerTraits = new TraitActorState(); e.State.persistentTraits = c.playerTraits;
        c.enemyTraits = new TraitActorState(); c.enemySkills = new SkillActorState();
        c.playerStatuses = new StatusActorState(); c.enemyStatuses = new StatusActorState();
        e.State.hp = e.State.maxHp = 1000; c.hand.Clear(); e.CombatActions.Clear();
        return e;
    }
    static void PlayAugmentCard(GameEngine e, string id, bool free = false)
    {
        var c = e.State.combat;
        if (free) c.freeCasts.Add(new FreeCastGrant { cardId = id, uses = 1 });
        c.hand.Add(id); Check(e.PlayCard(c.hand.Count - 1), "augment card plays: " + id + (free ? " for free" : " paid"));
    }
    static void EmptyAugmentEnemy(GameEngine e)
    {
        var c = e.State.combat; c.animal = null; c.enemyId = "augment_fixture";
        c.enemyDeck.Clear(); c.enemyDrawPile.Clear(); c.enemyDiscardPile.Clear(); c.enemyExhaustPile.Clear();
        c.enemyHand.Clear(); c.enemyPlan.Clear(); c.enemyPlanCosts.Clear(); c.enemyPlanFreeCast.Clear();
        c.enemyTraits = new TraitActorState(); c.enemySkills = new SkillActorState(); c.boss = null;
    }
    static string AugmentCard(string key, bool attack = false)
    {
        return GameDatabase.Cards.First(c => c.key == key && c.cost > 0 && (!attack || c.damage > 0)).id;
    }

    static void EndlessAugmentCatalogAndOffers()
    {
        Check((int)RunStage.AugmentChoice > (int)RunStage.Lost, "augment stage appends without changing existing save identifiers");
        Check(EndlessAugments.All.Count == 27 && EndlessAugments.All.Select(x => x.id).Distinct().Count() == 27, "twenty-seven unique augments");
        foreach (AugmentTier tier in Enum.GetValues(typeof(AugmentTier)))
        {
            var entries = EndlessAugments.All.Where(x => x.tier == tier).ToArray();
            Check(entries.Length == 9 && entries.Count(EndlessAugments.IsSupport) == 3
                && entries.Count(x => !EndlessAugments.IsSupport(x) && x.maxStacks == 3) == 6, "each tier has six capped effects and three repeatable supports");
            foreach (var def in entries) Check(!string.IsNullOrEmpty(def.name) && !string.IsNullOrEmpty(def.description)
                && !EndlessAugments.Description(def.id, 3).Contains("{"), "every augment has a complete readable description");
        }
        Check(Enumerable.Range(0, 100).Count(x => EndlessAugments.TierForOffering(4, x) == AugmentTier.Silver) == 25
            && Enumerable.Range(0, 100).Count(x => EndlessAugments.TierForOffering(4, x) == AugmentTier.Gold) == 45
            && Enumerable.Range(0, 100).Count(x => EndlessAugments.TierForOffering(4, x) == AugmentTier.Prismatic) == 30,
            "later offering tier weights are exactly 25/45/30");
        for (int cycle = 0; cycle < 5; cycle++)
        {
            var e = PendingAugment(cycle); var offers = e.AugmentOffers;
            Check(offers.Count == 3 && offers.Select(x => x.id).Distinct().Count() == 3 && offers.Select(x => x.tier).Distinct().Count() == 1,
                "three distinct choices share one tier every cycle");
            if (cycle < 3) Check((int)offers[0].tier == cycle, "first three rewards guarantee silver, gold and prismatic in order");
            var same = PendingAugment(cycle); Check(e.State.augmentOffers.SequenceEqual(same.State.augmentOffers), "the same seed and cycle offer the same choices");
        }
    }

    static void EndlessAugmentSelectionAndSave()
    {
        var e = PendingAugment(); int random = e.State.rngState, credits = e.State.credits, hp = e.State.hp;
        var choices = e.State.augmentOffers.ToArray();
        Check(!e.ConfirmAugment() && !e.ToggleAugment("invalid"), "no selection and unknown choices cannot consume a reward");
        Check(e.ToggleAugment(choices[0]) && e.SelectedAugmentId == choices[0], "choice can be previewed");
        Check(e.ToggleAugment(choices[1]) && e.SelectedAugmentId == choices[1], "choice can be changed");
        Check(e.ToggleAugment(choices[1]) && e.SelectedAugmentId == null, "clicking the selected choice cancels it");
        Check(e.State.rngState == random && e.State.credits == credits && e.State.hp == hp && e.AugmentCount == 0,
            "previews and cancellation neither spend gameplay RNG nor grant any resources");
        Check(e.ToggleAugment(choices[2]), "choice selected before saving");
        var loaded = new GameEngine((RunState)Clone(e.State));
        Check(loaded.State.augmentOffers.SequenceEqual(choices) && loaded.SelectedAugmentId == choices[2]
            && loaded.State.rngState == random && loaded.State.stage == RunStage.AugmentChoice, "save restores the exact three choices, selected preview and random stream");
        Check(loaded.ConfirmAugment() && loaded.AugmentCount == 1 && loaded.AugmentStacks(choices[2]) == 1
            && loaded.State.stage == RunStage.Map && loaded.State.endlessCycle == 1 && loaded.AugmentOffers.Count == 0 && loaded.SelectedAugmentId == null,
            "confirmation grants exactly one augment and starts exactly one new cycle");
        int after = loaded.State.rngState;
        Check(!loaded.ConfirmAugment() && !loaded.SkipAugment() && loaded.AugmentCount == 1 && loaded.State.rngState == after,
            "repeated confirmation or skip cannot duplicate a reward or map");
        Check(e.SkipAugment() && e.AugmentCount == 0 && e.State.stage == RunStage.Map && e.State.endlessCycle == 1,
            "explicit skip advances without a reward");
    }

    static void EndlessAugmentCycleAndLegacy()
    {
        var e = ReadyMode(RunMode.Endless); e.State.passives.AddRange(new[] { "isaac_p", "jackie_p", "aya_p" });
        ModeBossRewards(e, 3); var ids = e.State.map.Select(n => n.id).ToArray();
        string card = e.State.rewards.choices[0]; Check(e.ClaimCard(card), "final boss card selected before the augment");
        Check(!e.ConfirmAugment() && e.FinishRewards() && e.State.stage == RunStage.PassiveChoice,
            "augment cannot bypass final boss card and passive rewards");
        Check(e.ReplacePassive(-1) && e.State.stage == RunStage.AugmentChoice && e.State.deck.Contains(card)
            && e.State.map.Select(n => n.id).SequenceEqual(ids), "augment comes after confirmed rewards while retaining the completed map");
        string owned = e.State.augmentOffers[0]; Check(e.ToggleAugment(owned) && e.ConfirmAugment(), "final reward can start a new cycle");
        Check(e.State.map.All(n => !ids.Contains(n.id)) && e.State.archivedMapNodes.All(n => n.visited)
            && e.State.archivedMapNodes.Select(n => n.id).Distinct().Count() == e.State.archivedMapNodes.Count,
            "augment transition preserves unique current and archived map identities");
        foreach (var mode in new[] { RunMode.Normal, RunMode.Hard })
        {
            var finite = ReadyMode(mode); CompleteModeBoss(finite, 3);
            Check(finite.State.stage == RunStage.Won && finite.AugmentCount == 0 && finite.AugmentOffers.Count == 0, "finite modes still escape without augment choices");
            finite.State.augments.Add("aug_p_boss_energy"); finite.State.augmentOffers = null;
            var normalized = new GameEngine((RunState)Clone(finite.State)); Check(normalized.AugmentCount == 0 && normalized.AugmentOffers.Count == 0, "finite saves ignore endless-only fields");
        }
        var old = ReadyMode(RunMode.Endless); old.State.endlessCycle = 4; old.State.augments = old.State.augmentOffers = null;
        var migrated = new GameEngine((RunState)Clone(old.State));
        Check(migrated.State.stage == RunStage.Map && migrated.State.endlessCycle == 4 && migrated.AugmentCount == 0 && migrated.AugmentOffers.Count == 0,
            "old endless saves gain no retroactive rewards and keep their current cycle");
        old.State.stage = RunStage.AugmentChoice;
        var repaired = new GameEngine((RunState)Clone(old.State)); var repairedAgain = new GameEngine((RunState)Clone(old.State));
        Check(repaired.AugmentOffers.Count == 3 && repaired.State.augmentOffers.SequenceEqual(repairedAgain.State.augmentOffers)
            && repaired.State.rngState == old.State.rngState, "a missing pending offer is repaired deterministically without consuming gameplay randomness");
    }

    static void EndlessAugmentSupportAndCaps()
    {
        foreach (var def in EndlessAugments.All.Where(EndlessAugments.IsSupport))
        {
            var e = ReadyMode(RunMode.Endless); e.State.maxHp = 80; e.State.hp = 13; e.State.maxHealthBonus = 0;
            for (int repetition = 0; repetition < 2; repetition++)
            {
                int credits = e.State.credits, health = e.State.maxHp, hp = e.State.hp;
                SetAugmentOffer(e, def.id); Check(e.ToggleAugment(def.id) && e.ConfirmAugment(), "repeatable support is selected: " + def.id);
                Check(e.AugmentStacks(def.id) == repetition + 1, "support acquisitions remain in the expedition history");
                if (def.effect == "support_credits") Check(e.State.credits == credits + def.amount && e.State.hp == hp, "support credits pay exactly once");
                if (def.effect == "support_heal") Check(e.State.hp == Math.Min(health, hp + health * def.amount / 100) && e.State.maxHp == health, "support heal uses maximum HP, floors fractions and clamps to full health");
                if (def.effect == "support_max_health") Check(e.State.maxHp == health + def.amount && e.State.hp == hp + def.amount, "maximum-health support heals its increase exactly once");
            }
        }
        foreach (var def in EndlessAugments.All.Where(x => !EndlessAugments.IsSupport(x)))
        {
            var e = ReadyMode(RunMode.Endless);
            for (int stack = 1; stack <= 3; stack++)
            {
                SetAugmentOffer(e, def.id); Check(e.ToggleAugment(def.id) && e.ConfirmAugment() && e.AugmentStacks(def.id) == stack, "persistent augment reaches its three-stack cap");
            }
            SetAugmentOffer(e, def.id); Check(!e.ToggleAugment(def.id) && !e.ConfirmAugment() && e.AugmentStacks(def.id) == 3, "capped persistent augment cannot be acquired again");
        }
        foreach (AugmentTier tier in Enum.GetValues(typeof(AugmentTier)))
        {
            var allCapped = EndlessAugments.All.Where(x => x.tier == tier && !EndlessAugments.IsSupport(x)).SelectMany(x => Enumerable.Repeat(x.id, 3));
            var e = PendingAugment((int)tier, allCapped);
            Check(e.AugmentOffers.Count == 3 && e.AugmentOffers.All(EndlessAugments.IsSupport), "fully capped tier still offers its three repeatable supports");
        }
    }

    static void EndlessAugmentSilverCombat()
    {
        var e = AugmentBattle("aug_s_basic_guard"); PlayAugmentCard(e, "basic_guard"); Check(e.State.combat.block == GameDatabase.Card("basic_guard").block, "basic guard is not a basic attack");
        e.State.combat.block = 0; PlayAugmentCard(e, "basic_attack", true); Check(e.State.combat.block == 0, "free basic attack does not trigger paid augment");
        PlayAugmentCard(e, "basic_attack"); PlayAugmentCard(e, "basic_attack"); Check(e.State.combat.block == 3, "first paid basic attack gives block only once per turn");
        e = AugmentBattle("aug_s_guard_draw"); PlayAugmentCard(e, "basic_guard"); int cards = e.State.combat.hand.Count;
        PlayAugmentCard(e, "basic_guard"); Check(cards == 1 && e.State.combat.hand.Count == cards, "first native blocking card draws once");
        e = AugmentBattle("aug_s_weapon_damage"); string weapon = AugmentCard("D", true); var baseline = AugmentBattle();
        Check(e.CardTotalDamage(weapon) == baseline.CardTotalDamage(weapon) + 6, "weapon augment preview adds a single six-damage bonus");
        PlayAugmentCard(e, weapon); PlayAugmentCard(baseline, weapon);
        Check(baseline.State.combat.enemyHp - e.State.combat.enemyHp == 6, "weapon bonus applies once per card, including multihit weapons");
        Check(e.CardTotalDamage(weapon) == baseline.CardTotalDamage(weapon), "second paid D attack does not repeat the bonus");
        e = AugmentBattle("aug_s_tactical_guard"); string tactical = AugmentCard("F"); PlayAugmentCard(e, tactical, true);
        int block = e.State.combat.block; PlayAugmentCard(e, tactical); Check(e.State.combat.block >= block + 5, "first paid F gains tactical cover after a free F");
        e = AugmentBattle("aug_s_reserve_guard"); e.State.combat.energy = 2; block = e.State.combat.block;
        Check(e.BeginEndTurn() && e.State.combat.block == block + 5 && !e.BeginEndTurn(), "two unused energy grant one reserved shield at turn end");
        e = AugmentBattle("aug_s_reserve_guard"); e.State.combat.energy = 1; Check(e.BeginEndTurn() && e.State.combat.block == 0, "one unused energy does not meet the reserve condition");
        e = AugmentBattle("aug_s_victory_heal"); baseline = AugmentBattle(); e.State.hp = baseline.State.hp = 20;
        Win(e); Win(baseline); Check(e.State.hp == baseline.State.hp + 4, "victory recovery occurs exactly once after a real combat victory");
    }

    static void EndlessAugmentGoldCombat()
    {
        var e = AugmentBattle("aug_g_qwe_energy"); var baseline = AugmentBattle();
        foreach (var id in new[] { "nia_q", "nia_w", "nia_e" }) { PlayAugmentCard(e, id); PlayAugmentCard(baseline, id); }
        Check(e.State.combat.energy == baseline.State.combat.energy + 1, "paid Q W E completion refunds one energy independent of owner");
        foreach (var id in new[] { "nia_q", "nia_w", "nia_e" }) { PlayAugmentCard(e, id); PlayAugmentCard(baseline, id); }
        Check(e.State.combat.energy == baseline.State.combat.energy + 1, "Q W E refund occurs only once each turn");
        e = AugmentBattle("aug_g_ultimate_draw"); baseline = AugmentBattle(); string ultimate = AugmentCard("R");
        PlayAugmentCard(e, ultimate); PlayAugmentCard(baseline, ultimate);
        Check(e.State.combat.hand.Count == baseline.State.combat.hand.Count + 2, "first paid ultimate draws two additional cards");
        e = AugmentBattle("aug_g_debuffed_damage"); baseline = AugmentBattle();
        Check(e.CardTotalDamage("basic_attack") == baseline.CardTotalDamage("basic_attack"), "debuff damage cannot trigger against an unaffected target");
        e.State.combat.enemyPoison = baseline.State.combat.enemyPoison = 1;
        Check(e.CardTotalDamage("basic_attack") == baseline.CardTotalDamage("basic_attack") + 7, "existing poison qualifies before the attack");
        PlayAugmentCard(e, "basic_attack", true); PlayAugmentCard(baseline, "basic_attack", true);
        Check(e.CardTotalDamage("basic_attack") == baseline.CardTotalDamage("basic_attack") + 7, "free attacks preserve the first paid debuff bonus");
        PlayAugmentCard(e, "basic_attack"); Check(e.CardTotalDamage("basic_attack") == baseline.CardTotalDamage("basic_attack"), "paid debuff bonus is consumed once");
        foreach (int hp in new[] { 40, 41 })
        {
            var low = ReadyMode(RunMode.Endless); low.State.augments.Add("aug_g_low_health"); low.State.maxHp = 80; low.State.hp = hp;
            low.AvailableNodes().First(n => n.lane == 1).assignedAnimal = "bear"; Enter(low, ZoneKind.Wildlife);
            Check(low.State.combat.block == (hp == 40 ? 8 : 0), "half-health block uses the exact inclusive threshold at turn start");
        }
        e = AugmentBattle("aug_g_weapon_basic"); baseline = AugmentBattle(); string weapon = AugmentCard("D");
        PlayAugmentCard(e, weapon, true); PlayAugmentCard(baseline, weapon, true);
        PlayAugmentCard(e, "basic_guard"); PlayAugmentCard(baseline, "basic_guard");
        Check(e.CardTotalDamage("basic_attack") == baseline.CardTotalDamage("basic_attack") + 10, "free D enables a later paid basic attack while basic guard does not consume it");
        PlayAugmentCard(e, "basic_attack"); Check(e.CardTotalDamage("basic_attack") == baseline.CardTotalDamage("basic_attack"), "weapon-basic crossing cannot repeat this turn");
        foreach (int size in new[] { 18, 19 })
        {
            var small = ReadyMode(RunMode.Endless); small.State.augments.Add("aug_g_small_deck"); small.State.deck = Enumerable.Repeat("basic_attack", size).ToList();
            small.AvailableNodes().First(n => n.lane == 1).assignedAnimal = "bear"; Enter(small, ZoneKind.Wildlife);
            Check(small.State.combat.hand.Count == (size == 18 ? 6 : 5), "small-deck draw counts actual deck copies at the eighteen-card boundary");
        }
    }

    static void EndlessAugmentPrismaticCombat()
    {
        string heavy = GameDatabase.Cards.First(c => c.cost >= 4 && c.damage > 0 && c.energy == 0).id;
        var e = AugmentBattle("aug_p_heavy_refund", "aug_p_heavy_refund", "aug_p_heavy_refund"); var baseline = AugmentBattle();
        int paid = e.EffectiveCardCost(heavy); PlayAugmentCard(e, heavy); PlayAugmentCard(baseline, heavy);
        Check(e.State.combat.energy == baseline.State.combat.energy + Math.Min(6, paid), "stacked heavy refund never exceeds the actual amount paid");
        PlayAugmentCard(e, heavy); PlayAugmentCard(baseline, heavy);
        Check(e.State.combat.energy == baseline.State.combat.energy + Math.Min(6, paid), "heavy refund cannot form a repeatable same-turn energy loop");
        e = AugmentBattle("aug_p_heavy_refund"); e.State.combat.playerSkills.discounts.Add(new SkillCostDiscount { targetCard = heavy, amount = GameDatabase.Card(heavy).cost - 3 });
        Check(e.EffectiveCardCost(heavy) == 3, "heavy fixture uses a real discounted payment"); int energy = e.State.combat.energy;
        PlayAugmentCard(e, heavy); Check(e.State.combat.energy == energy - 3, "original high cost does not trigger a refund when actual payment is below four");
        e = AugmentBattle("aug_p_arsenal"); baseline = AugmentBattle();
        foreach (var id in new[] { "nia_q", "nia_w", "nia_e" }) { PlayAugmentCard(e, id); PlayAugmentCard(baseline, id); }
        Check(e.State.combat.block == baseline.State.combat.block, "Q W E alone cannot trigger the four-key arsenal");
        PlayAugmentCard(e, "nia_r"); PlayAugmentCard(baseline, "nia_r"); Check(e.State.combat.block == baseline.State.combat.block + 14, "paid Q W E R completion grants one arsenal shield");
        string ultimate = GameDatabase.Cards.First(c => c.key == "R" && c.cost > 0 && c.damage > 0 && c.hits > 1).id;
        e = AugmentBattle("aug_p_debuffed_r"); baseline = AugmentBattle(); e.State.combat.enemyPoison = baseline.State.combat.enemyPoison = 1;
        Check(e.CardTotalDamage(ultimate) == baseline.CardTotalDamage(ultimate) + 18, "debuffed ultimate preview adds eighteen once for a multihit card");
        PlayAugmentCard(e, ultimate); PlayAugmentCard(baseline, ultimate);
        Check(baseline.State.combat.enemyHp - e.State.combat.enemyHp == 18, "multihit ultimate applies the augment bonus only to its first landed hit");
        e = AugmentBattle("aug_p_shield"); baseline = AugmentBattle(); e.State.combat.block = 15;
        Check(e.CardTotalDamage("basic_attack") == baseline.CardTotalDamage("basic_attack") + 12, "shield conversion previews one additional hit bonus");
        e.State.combat.enemyEvasion = 100; PlayAugmentCard(e, "basic_attack");
        Check(e.State.combat.block == 10 && e.State.combat.enemyHp == e.State.combat.enemyMaxHp, "a fully avoided attack still spends the shield conversion's five block");
        e.State.combat.enemyEvasion = 0; e.State.combat.block = 15;
        Check(e.CardTotalDamage("basic_attack") == baseline.CardTotalDamage("basic_attack"), "a miss also consumes the once-per-turn conversion opportunity");
        e = AugmentBattle("aug_p_all_in", "aug_p_heavy_refund"); baseline = AugmentBattle(); e.State.combat.energy = baseline.State.combat.energy = e.EffectiveCardCost(heavy);
        PlayAugmentCard(e, heavy); PlayAugmentCard(baseline, heavy);
        Check(e.State.combat.block == baseline.State.combat.block + 10 && e.State.combat.energy > 0, "all-in observes payment before the later heavy refund restores energy");
        foreach (var kind in new[] { ZoneKind.Boss, ZoneKind.Wildlife })
        {
            var opening = ReadyMode(RunMode.Endless); opening.State.augments.AddRange(Enumerable.Repeat("aug_p_boss_energy", 3));
            Enter(opening, kind); Check(opening.State.combat.energy == opening.MaxEnergy + (kind == ZoneKind.Boss ? 3 : 0), "boss opening energy is temporary and absent in ordinary fights");
            EmptyAugmentEnemy(opening); Check(opening.EndTurn() && opening.State.combat.energy == opening.MaxEnergy, "boss opening energy cannot recur on the second turn");
        }
    }

    static void EndlessAugmentPersistenceAndIsolation()
    {
        var e = AugmentBattle("aug_s_basic_guard", "aug_g_qwe_energy", "aug_g_weapon_basic");
        PlayAugmentCard(e, "basic_attack"); PlayAugmentCard(e, "nia_q"); PlayAugmentCard(e, "nia_w"); PlayAugmentCard(e, AugmentCard("D"), true);
        var loaded = new GameEngine((RunState)Clone(e.State));
        Check(loaded.State.combat.augmentTurnMask == e.State.combat.augmentTurnMask && loaded.State.combat.augmentKeyMask == e.State.combat.augmentKeyMask
            && loaded.State.combat.augmentWeaponUsed && loaded.State.augments.SequenceEqual(e.State.augments), "combat save preserves consumed triggers, partial key sequence and D preparation");
        int block = loaded.State.combat.block; PlayAugmentCard(loaded, "basic_attack"); Check(loaded.State.combat.block == block, "loading cannot repeat the basic shield in the same turn");
        int random = loaded.State.rngState, mask = loaded.State.combat.augmentTurnMask, keys = loaded.State.combat.augmentKeyMask;
        for (int i = 0; i < 30; i++) { loaded.CardTotalDamage("basic_attack"); loaded.CardTotalDamage("nia_r"); var offers = loaded.AugmentOffers; }
        Check(loaded.State.rngState == random && loaded.State.combat.augmentTurnMask == mask && loaded.State.combat.augmentKeyMask == keys,
            "repeated card and augment previews preserve RNG and all trigger state");
        EmptyAugmentEnemy(loaded); Check(loaded.EndTurn() && loaded.State.combat.augmentTurnMask == 0 && loaded.State.combat.augmentKeyMask == 0 && !loaded.State.combat.augmentWeaponUsed,
            "the next actual player turn resets all augment limits and preparations");
        PlayAugmentCard(loaded, "basic_attack"); Check(loaded.State.combat.block == 3, "a new turn legitimately restores its basic shield trigger");
        foreach (string kind in new[] { "heal_reduction", "exposure" })
        {
            var debuff = AugmentBattle("aug_g_debuffed_damage"); var baseline = AugmentBattle();
            debuff.State.combat.enemyTraits.buffs.Add(new TraitBuff { kind = kind, amount = 10, remaining = 1 });
            baseline.State.combat.enemyTraits.buffs.Add(new TraitBuff { kind = kind, amount = 10, remaining = 1 });
            Check(debuff.CardTotalDamage("basic_attack") > baseline.CardTotalDamage("basic_attack"), "existing negative trait buffs also qualify as a debuff: " + kind);
        }
        var normal = AugmentBattle("aug_s_basic_guard", "aug_p_shield"); var bare = AugmentBattle();
        normal.State.mode = bare.State.mode = RunMode.Normal; normal.State.combat.block = bare.State.combat.block = 15;
        Check(normal.CardTotalDamage("basic_attack") == bare.CardTotalDamage("basic_attack"), "imported augment fields cannot boost a finite mode");
        PlayAugmentCard(normal, "basic_attack"); PlayAugmentCard(bare, "basic_attack");
        Check(normal.State.combat.block == bare.State.combat.block && normal.State.combat.enemyHp == bare.State.combat.enemyHp
            && normal.State.rngState == bare.State.rngState, "finite-mode damage, block and random results remain identical");
    }
}
#endif
