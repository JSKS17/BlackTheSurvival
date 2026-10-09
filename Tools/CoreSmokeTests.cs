#if LUMIA_TEST_RUNNER
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lumia;

// Compiled outside Unity by Tools/RunCoreTests.ps1. No graphics or Unity APIs required.
public static class CoreSmokeTests
{
    private static int checks;
    public static int Main(string[] args)
    {
        try
        {
            if (args.Contains("--balance")) return BalanceSimulation.Run();
            DatabaseIntegrity(); PreparationAndLocks(); RouteProgression(); CombatPilesAndEnergy();
            EnemyDeckAndStatus(); EnemyPreview(); RewardsOnlyOnce(); WildlifeDropTables(); SubjectDropTables();
            ShopEconomy(); CampAndEquipment(); FoodAndCooking(); PassiveCapacity(); EventEffects();
            StarterLocksAndCeiling(); FullEnemyLoadouts(); EquipmentIdentityAndAlex();
            SaveDeterminism(); PassiveCardOffers(); NiaSkillIdentity(); SkillRulesAndIsolation(); BinarySkillStates(); SkillTimedEffects(); SkillEnemyPlanning(); SkillPersistenceAndLimits(); TargetedFreeCasts(); LastOwnSkillReplay(); PacedEnemyActions(); SaveMigration(); LevelCap(); LevelTwentyOnCombatRoute(); CompleteEscapeAndDeath();
            TraitCoverageAndDescriptions(); GlobalPassiveCombat(); GlobalRuneCombat(); TraitHitAndHealingHooks(); TraitPersistenceAndEconomy(); TraitEnemyIntent(); ConditionalSkillRecallAndEffects();
            OriginalStatusCoverage(); StatusUseRestrictions(); StatusTimingAndForecast(); StatusPersistenceAndRates(); HealingDroneHealthTrigger(); RevisedEconomyAndEncounters();
            Console.WriteLine("PASS: " + checks + " assertions across 45 game-rule scenarios.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine("FAIL: " + ex); return 1; }
    }

    private static void Check(bool condition, string reason)
    {
        checks++; if (!condition) throw new Exception(reason);
    }

    private static GameEngine Ready(int seed = 771)
    {
        var e = new GameEngine(seed);
        var main = GameDatabase.Runes.First(r => r.main);
        var support = GameDatabase.Runes.First(r => !r.main && r.tree == main.tree);
        Check(e.SetRunes(main.id, support.id), "valid rune pair");
        Check(e.SelectStartingPassive(e.State.passiveOffers[0]), "starting passive");
        foreach (var id in e.State.draftOffers.Take(3).ToArray()) Check(e.ToggleDraft(id), "draft selection");
        Check(e.BeginJourney(), "begin complete preparation");
        return e;
    }

    private static void Enter(GameEngine e, ZoneKind kind, int lane = 1)
    {
        Check(e.State.stage == RunStage.Map, "fixture enters zone from map");
        var node = e.AvailableNodes().First(n => n.lane == lane);
        node.kind = kind; Check(e.EnterNode(lane), "enter " + kind);
    }

    private static void Win(GameEngine e)
    {
        // A source passive may legitimately prevent its first lethal result once.
        for(int attempt=0;attempt<3 && e.State.stage==RunStage.Combat;++attempt)
        {
            e.State.combat.enemyHp = 1; e.State.combat.enemyBlock = 0; e.State.combat.enemyEvasion = 0;
            e.State.combat.hand.Add("basic_attack"); e.State.combat.energy = 99;
            Check(e.PlayCard(e.State.combat.hand.Count - 1), "winning attack, including finite enemy lethal prevention");
        }
        Check(e.State.stage == RunStage.Rewards, "combat transitions to rewards");
    }

    private static void DatabaseIntegrity()
    {
        Check(GameDatabase.Characters.Count >= 90, "complete playable character roster");
        Check(GameDatabase.Cards.Select(x => x.id).Distinct().Count() == GameDatabase.Cards.Count, "unique card identifiers");
        foreach (var c in GameDatabase.Characters)
        {
            Check(GameDatabase.Passive(c.passiveId) != null, "character passive exists: " + c.id);
            Check(c.cards != null && c.cards.Length == 4, "QWER per included character: " + c.id);
            Check(GameDatabase.Events.Any(e => e.owner == c.name), "every combat character has an encounter: " + c.id);
            foreach (var id in c.cards) Check(GameDatabase.Card(id) != null, "character skill exists: " + id);
        }
        foreach (var c in GameDatabase.Cards.Where(x => x.category != "basic"))
            Check(c.cost >= 0 && c.cost <= 7 && (c.cost == Math.Max(!c.exhaust && (c.draw > 0 || c.energy > 0) ? 1 : 0, GameDatabase.CostForCooldown(c.cooldown)) || new[] { "justyna_q", "hisui_w", "tsubame_r" }.Contains(c.id)), "cooldown and resource-derived cost: " + c.id);
        Check(GameDatabase.Cards.Select(c => c.cost).Distinct().OrderBy(x => x).SequenceEqual(Enumerable.Range(0, 8)), "cards cover zero through seven cost for a broad decision range");
        foreach (var c in GameDatabase.Cards)
        {
            Check(!string.IsNullOrEmpty(c.description) && c.description.Split('\n').All(line => line.Trim().EndsWith(".")), "every card uses complete readable sentences: " + c.id);
            Check(!string.IsNullOrEmpty(c.effect) && !string.IsNullOrEmpty(c.effectColor), "every card has a combat effect: " + c.id);
            if (c.freeCastCount > 0) Check(c.freeCastTargets != null && c.freeCastTargets.All(id => GameDatabase.Card(id) != null), "all reset targets exist: " + c.id);
        }
        foreach (var g in GameDatabase.Gear)
        {
            Check(GameDatabase.Object(g.objectId) != null, "gear recipe exists: " + g.id);
            Check(g.rarity == "전설" || g.rarity == "초월", "gear rarity: " + g.id);
            if (g.slot == GearSlot.Weapon) Check(GameDatabase.Card(g.cardId) != null, "weapon card: " + g.id);
        }
        foreach (var f in GameDatabase.Foods.Where(x => !string.IsNullOrEmpty(x.upgradeTo))) Check(GameDatabase.Food(f.upgradeTo) != null, "camp recipe output: " + f.id);
        foreach (var e in GameDatabase.Events)
        {
            Check(e.options != null && e.options.Length >= 2, "event decisions: " + e.id);
            foreach (var o in e.options)
            {
                if (o.effect == "card") Check(GameDatabase.Card(o.cardId) != null, "event card exists");
                if (o.effect == "object") Check(GameDatabase.Object(o.objectId) != null, "event object exists");
                if (o.effect == "passive") Check(GameDatabase.Passive(o.passiveId) != null, "event passive exists");
            }
        }
    }

    private static void PreparationAndLocks()
    {
        var e = new GameEngine(3); Check(!e.BeginJourney(), "incomplete preparation blocked");
        Check(e.State.deck.Count(x => x == "basic_attack") == 5 && e.State.deck.Count(x => x == "basic_guard") == 3, "starter basics");
        var m = GameDatabase.Runes.First(x => x.main);
        var wrong = GameDatabase.Runes.First(x => !x.main && x.tree != m.tree);
        Check(!e.SetRunes(m.id, wrong.id), "mismatched support blocked");
        Check(e.SelectStartingPassive(e.State.passiveOffers[0]), "select before reroll");
        Check(e.RerollPassives() && e.RerollPassives() && !e.RerollPassives(), "exactly two passive rerolls");
        Check(e.State.passives.Count == 0 && string.IsNullOrEmpty(e.State.chosenPassive), "reroll resets prior passive");
        Check(e.ToggleDraft(e.State.draftOffers[0]) && e.RerollDraft(), "draft reroll after selecting");
        Check(e.State.draftSelected.Count == 1 && e.State.draftOffers.Contains(e.State.draftSelected[0]), "reroll retains selected cards");
        Check(e.RerollDraft() && !e.RerollDraft(), "exactly two card rerolls");
        e = Ready(); Check(!e.SetRunes(m.id, GameDatabase.Runes.First(x => !x.main && x.tree == m.tree).id), "runes locked after departure");
        Check(!e.BeginJourney() && e.State.deck.Count == 11, "starter cards cannot duplicate");
    }

    private static void StarterLocksAndCeiling()
    {
        for(int seed=1;seed<=100;++seed)
        {
            var e=new GameEngine(seed);
            Check(e.State.draftOffers.Count==6 && e.State.draftOffers.All(id=>GameDatabase.Card(id).cost<5),"new starter offers exclude cost five or more");
            int[] slots={0,2,4};foreach(int slot in slots)e.ToggleDraft(e.State.draftOffers[slot]);
            var selected=e.State.draftSelected.ToArray();var original=e.State.draftOffers.ToArray();
            Check(e.RerollDraft(),"locked starter reroll succeeds");
            Check(e.State.draftSelected.SequenceEqual(selected) && slots.All(slot=>e.State.draftOffers[slot]==original[slot]),"selected cards keep their slots and selection across reroll");
            Check(e.State.draftOffers.Count==6 && e.State.draftOffers.Distinct().Count()==6 && e.State.draftOffers.All(id=>e.CardCost(id)<5),"rerolled offers are six distinct usable starter cards");
            var saved=new GameEngine((RunState)Clone(e.State));
            Check(saved.RerollDraft() && e.RerollDraft() && saved.State.draftOffers.SequenceEqual(e.State.draftOffers) && saved.State.draftSelected.SequenceEqual(selected) && saved.State.rngState==e.State.rngState,"locked draft persists deterministic rolls through save and reload");
            Check(e.ToggleDraft(selected[0]) && !e.State.draftSelected.Contains(selected[0]),"locked cards can be unselected normally");
        }
    }

    private static void FullEnemyLoadouts()
    {
        Check(GameDatabase.Cards.Count(x=>x.category=="weapon")==23,"every original weapon class has one D card");
        foreach(var character in GameDatabase.Characters)
        {
            Check(character.weaponClasses.Length>0 && character.weaponClasses.All(x=>GameDatabase.Card(WeaponIdentity.CardFor(x))!=null),"every subject has its original supported weapon classes: "+character.id);
        }
        var observed=new HashSet<string>();
        for(int seed=1;seed<=240;++seed)
        {
            var e=Ready(seed);e.State.act=3;e.State.row=8;Enter(e,ZoneKind.Subject);var c=e.State.combat;
            var character=GameDatabase.Character(c.enemyId);observed.Add(c.enemyWeaponClass);
            Check(character.weaponClasses.Contains(c.enemyWeaponClass) && c.enemyPassiveId==character.passiveId,"enemy carries its own weapon class and passive");
            Check(c.enemyDeck.Contains("basic_attack") && c.enemyDeck.Contains("basic_guard") && c.enemyDeck.Contains(c.enemyWeaponCardId) && c.enemyDeck.Contains(c.enemyTacticalCardId),"enemy deck includes ATK, DEF, D and F");
            Check(c.enemyGear.Count==5 && c.enemyGear.Select(GameDatabase.Equipment).GroupBy(x=>x.slot).All(x=>x.Count()==1),"late subject equips five distinct slots");
            Check(c.enemyGear.Select(GameDatabase.Equipment).Single(x=>x.slot==GearSlot.Weapon).weaponClass==c.enemyWeaponClass,"enemy equipment and D use the same original weapon class");
            var saved=new GameEngine((RunState)Clone(e.State));int rng=e.State.rngState;
            Check(saved.State.combat.enemyGear.SequenceEqual(c.enemyGear) && saved.State.combat.enemyTacticalCardId==c.enemyTacticalCardId && saved.State.combat.enemyPlan.SequenceEqual(c.enemyPlan),"save preserves enemy gear, tactical assignment and pending actions");
            string intent=e.EnemyIntent;Check(e.State.rngState==rng,"loadout preview consumes no randomness");
        }
        Check(observed.Contains("VF의수") && observed.Contains("카메라") && observed.Contains("아르카나"),"unique specialist weapon classes occur in actual fights");
        var firstBoss=Ready(77);firstBoss.State.row=10;Enter(firstBoss,ZoneKind.Boss);Check(firstBoss.State.combat.enemyGear.Count==2,"first boss has two equipment pieces");
        var lateBoss=Ready(78);lateBoss.State.act=3;lateBoss.State.row=10;Enter(lateBoss,ZoneKind.Boss);Check(lateBoss.State.combat.enemyGear.Count==5,"final boss has a complete equipment loadout");
        var isaac=MechanicsFixture(null,"isaac");var state=isaac.State.combat;state.enemyHp=500;state.enemyMaxHp=999;isaac.State.hp=isaac.State.maxHp=999;
        state.enemyPlan=new List<string>{"basic_attack","basic_attack","basic_attack"};state.enemyPlanCosts=new List<int>{1,1,1};state.enemyPlanFreeCast=new List<bool>{false,false,false};
        string preview=isaac.EnemyIntent;int expected=state.intentDamage,before=isaac.State.hp;
        Check(isaac.EndTurn() && isaac.CombatActions.Count(x=>x.enemy && x.cardId=="basic_attack" && x.kind==null)==3 && state.enemyHp>500,"enemy basic attacks trigger its own Isaac third-attack recovery");
        Check(before-isaac.State.hp==expected,"enemy passive-enhanced basic forecast matches actual unblocked damage");
    }

    private static void EquipmentIdentityAndAlex()
    {
        Check(GameDatabase.Gear.Count==47 && GameDatabase.Gear.Where(x=>x.slot==GearSlot.Weapon).Select(x=>x.weaponClass).Distinct().Count()==23,"47 original legendary or mythic items cover every weapon class");
        foreach(var item in GameDatabase.Gear)
            Check(item.mechanics!=null && item.effect==null && item.description.Split('\n').All(x=>x.Trim().EndsWith(".")),"equipment has one authored mechanic profile and sentence-based description: "+item.id);
        var e=Ready(40);e.State.passives.Clear();e.State.passives.Add("alex_p");Enter(e,ZoneKind.Campfire);e.ChooseCamp(true);
        e.State.objects.AddRange(new[]{"meteorite","meteorite","mithril"});
        Check(e.Craft("fragarach") && e.Craft("fragarach"),"Alex equips two same-class weapons");
        var granted=e.State.deck.Where(id=>GameDatabase.Card(id).category=="weapon").ToArray();
        Check(granted.Length==4 && granted.Count(id=>id=="weapon_dagger")==2 && granted.Count(id=>id!="weapon_dagger")==2,"each equipped weapon grants own D and one different-class Alex D");
        Check(e.Craft("akelte",0) && e.State.deck.Count(id=>GameDatabase.Card(id).category=="weapon")==4 && e.State.deck.Count(id=>id=="weapon_dagger")==1 && e.State.deck.Count(id=>id=="weapon_pistol")>=1,"weapon replacement removes obsolete grants without accumulating cards");
        e.State.stage=RunStage.PassiveChoice;e.State.pendingPassive="isaac_p";
        Check(e.ReplacePassive(0) && e.State.deck.Count(id=>GameDatabase.Card(id).category=="weapon")==2,"removing Alex removes only the extra weapon grants");
        var loaded=new GameEngine((RunState)Clone(e.State));Check(loaded.State.deck.SequenceEqual(e.State.deck),"weapon card synchronization stays idempotent after save reload");

        e=Ready(43);e.State.deck.AddRange(new[]{"weapon_dagger","weapon_dagger","weapon_hammer"});
        e.State.passives.Clear();e.State.passives.Add("alex_p");Enter(e,ZoneKind.Campfire);e.ChooseCamp(true);e.State.objects.AddRange(new[]{"meteorite","meteorite","mithril"});
        Check(e.Craft("fragarach") && e.Craft("fragarach") && e.State.weaponGrantedCards.Count==4 && e.State.deck.Count(id=>id=="weapon_dagger")==4,"two weapon grants are tracked separately from two independently acquired matching D cards");
        Check(e.Craft("akelte",0) && e.State.deck.Count(id=>id=="weapon_dagger")==3 && e.State.deck.Count(id=>id=="weapon_hammer")==1,"weapon replacement retains acquired D copies including other classes");
        e.State.stage=RunStage.PassiveChoice;e.State.pendingPassive="isaac_p";e.ReplacePassive(0);
        Check(e.State.weaponGrantedCards.Count==2 && e.State.deck.Count(id=>id=="weapon_dagger")==3 && e.State.deck.Count(id=>id=="weapon_hammer")==1,"Alex replacement removes only tracked grants while preserving earned D cards");
        loaded=new GameEngine((RunState)Clone(e.State));Check(loaded.State.deck.SequenceEqual(e.State.deck) && loaded.State.weaponGrantedCards.SequenceEqual(e.State.weaponGrantedCards),"saved grant provenance preserves all acquired D cards");
        e.State.gear.Clear();loaded=new GameEngine((RunState)Clone(e.State));
        Check(loaded.State.weaponGrantedCards.Count==0 && loaded.State.deck.Count(id=>id=="weapon_dagger")==2 && loaded.State.deck.Count(id=>id=="weapon_hammer")==1,"unequipping all weapons keeps acquired D cards");
        var legacy=(RunState)Clone(e.State);legacy.weaponGrantVersion=0;legacy.weaponGrantedCards=null;legacy.gear=new List<string>{"fragarach"};legacy.deck=new List<string>{"basic_attack","weapon_dagger","weapon_dagger","weapon_hammer"};
        loaded=new GameEngine(legacy);Check(loaded.State.deck.Count(id=>id=="weapon_dagger")==2 && loaded.State.deck.Contains("weapon_hammer") && loaded.State.weaponGrantedCards.SequenceEqual(new[]{"weapon_dagger"}) && loaded.State.weaponGrantVersion==1,"legacy migration infers one equipment grant and preserves extra matching and unrelated D copies");
        legacy=(RunState)Clone(e.State);legacy.weaponGrantVersion=0;legacy.weaponGrantedCards=null;legacy.gear.Clear();legacy.deck=new List<string>{"basic_attack","weapon_dagger","weapon_hammer"};
        loaded=new GameEngine(legacy);Check(loaded.State.deck.SequenceEqual(legacy.deck) && loaded.State.weaponGrantedCards.Count==0,"legacy save without weapons retains every acquired D card");

        e=MechanicsFixture();var c=e.State.combat;e.State.gear.Add("laevateinn");c.hand.Add("basic_attack");e.PlayCard(0);
        Check(c.playerTraits.skills.effects.Any(x=>x.kind=="burn" && x.owner=="trait:gear:laevateinn"),"Laevateinn ignites on a basic attack");
        e=MechanicsFixture();c=e.State.combat;e.State.gear.Add("cube_watch");c.hand.Add("nia_w");e.PlayCard(0);c.hand.Add("basic_attack");e.PlayCard(0);
        Check(e.EffectiveCardCost("nia_w")==e.CardCost("nia_w")-1,"Cube Watch discounts the last used QWE after a basic hit");
        e=MechanicsFixture();c=e.State.combat;e.State.gear.Add("skadi");c.hand.Add("nia_q");e.PlayCard(0);
        Check(StatusMechanics.Has(c.enemyStatuses,"slow"),"Skadi's original coldwave inflicts slow on a skill hit");
        e=MechanicsFixture();c=e.State.combat;e.State.gear.Add("ao_dai");e.State.hp=e.State.maxHp=1000;c.enemyLevel=20;
        c.enemyPlan=new List<string>{"basic_attack"};c.enemyPlanCosts=new List<int>{1};c.enemyPlanFreeCast=new List<bool>{false};c.block=0;
        int raw=GameDatabase.Card("basic_attack").damage+10;e.BeginEndTurn();e.AdvanceEnemyAction();
        Check(1000-e.State.hp==raw-raw/5 && c.playerDeferredDamage.Sum(x=>x.amount)==raw/5,"Ao Dai defers one fifth of post-block damage rather than deleting it");
        var resumed=new GameEngine((RunState)Clone(e.State));Check(resumed.State.combat.playerDeferredDamage.Sum(x=>x.amount)==raw/5,"deferred damage is saved during a paced enemy turn");
        int hp=e.State.hp;while(c.enemyTurn)e.AdvanceEnemyAction();Check(e.State.hp<hp && c.playerDeferredDamage.Sum(x=>x.amount)<raw/5,"deferred damage begins repaying on the wearer's next turn");
        e=MechanicsFixture();c=e.State.combat;e.State.gear.Add("auto_arms");c.enemyPlan.Clear();e.BeginEndTurn();StatusMechanics.Add(c.playerStatuses,"suppression","echion_r");while(c.enemyTurn)e.AdvanceEnemyAction();
        Check(c.energy==GameEngine.EnergyForLevel(e.State.level)-1,"control-resistant equipment mitigates one of suppression's two lost costs");
    }

    private static void RouteProgression()
    {
        var e = Ready(); Check(e.State.mapRows == 12 && e.State.map.Count == 108, "three acts, twelve rows, three lanes");
        Check(e.AvailableNodes().Count == 3, "all initial lanes reachable");
        Enter(e, ZoneKind.Kiosk, 0); Check(e.State.row == -1, "unresolved zone does not advance row");
        Check(!e.EnterNode(1), "cannot move during zone"); Check(e.LeaveKiosk(), "resolve kiosk");
        Check(e.State.row == 0 && e.State.lane == 0 && e.AvailableNodes().Count == 2, "resolved branch restricts next links");
        Check(!e.EnterNode(2), "cannot jump two lanes");
        Check(e.State.map.Count(x => x.visited) == 1, "single node resolved");
    }

    private static void CombatPilesAndEnergy()
    {
        var e = Ready(); e.State.passives.Clear(); e.State.mainRune = e.State.supportRune = null;
        Enter(e, ZoneKind.Wildlife); var c = e.State.combat;
        c.enemyHp = c.enemyMaxHp = 999; e.State.hp = e.State.maxHp = 999;
        Check(c.hand.Count == 5 && PileCount(c) == e.State.deck.Count, "initial draw preserves entire deck");
        c.hand.Clear(); c.drawPile.Clear(); c.discardPile.Clear(); c.exhaustPile.Clear();
        c.hand.Add("basic_attack"); c.hand.Add("tactical_blink"); c.energy = 1;
        int hp = c.enemyHp; Check(!e.PlayCard(1) && c.energy == 1 && c.hand.Count == 2, "unaffordable card has no effect");
        Check(e.PlayCard(0) && c.energy == 0 && c.enemyHp < hp && c.discardPile.Contains("basic_attack"), "attack spends energy and discards");
        c.energy = e.CardCost("tactical_blink"); Check(e.PlayCard(0) && c.exhaustPile.Contains("tactical_blink") && !c.discardPile.Contains("tactical_blink"), "exhaust separated from discard");
        Check(c.evasion == 60 && c.evasionTurns == 2, "movement adapted to temporary evasion");
        Check(e.EndTurn() && c.turn == 2 && c.energy == e.MaxEnergy && c.evasionTurns == 1, "next turn replenishes energy and ticks evasion");
        Check(c.hand.Contains("basic_attack") && !c.hand.Contains("tactical_blink"), "discard reshuffles, exhaust does not");
    }

    private static int PileCount(CombatState c) { return c.hand.Count + c.drawPile.Count + c.discardPile.Count + c.exhaustPile.Count; }

    private static void EnemyDeckAndStatus()
    {
        foreach (int level in new[] { 1, 8, 15, 20 })
        {
            var e = Ready(level * 83); e.State.act = level <= 7 ? 1 : level <= 14 ? 2 : 3;
            e.State.row = Math.Min(5, (level - 1) % 7) - 1;
            Enter(e, ZoneKind.Subject); var c = e.State.combat;
            Check(c.enemyLevel <= 20 && c.enemyLevel >= 1, "enemy level bounds");
            Check(c.enemyPlanCosts.Select((cost, i) => cost - GameDatabase.Card(c.enemyPlan[i]).energy).Sum() <= GameEngine.EnergyForLevel(c.enemyLevel), "enemy plan respects level cost ceiling");
            var ch = GameDatabase.Character(c.enemyId); Check(c.enemyDeck.All(id => ch.cards.Contains(id) || GameDatabase.Card(id).category!="skill"), "subject skill cards belong to its own QWER");
            Check(c.enemyDeck.Contains("basic_attack") && c.enemyDeck.Contains(c.enemyWeaponCardId) && c.enemyDeck.Contains(c.enemyTacticalCardId),"subject has basic attacks, matching D and assigned F");
            if (c.enemyLevel < 7) Check(c.enemyDeck.All(id => GameDatabase.Card(id).key != "R"), "ultimate locked at low level");
        }
        var status = Ready(); Enter(status, ZoneKind.Wildlife); var s = status.State.combat;
        s.enemyHp = 999; s.enemyEvasion = 0; s.enemyPlan = new List<string> { "basic_attack" }; s.enemyPlanCosts = new List<int> { 1 }; s.hand.Clear(); s.hand.Add("hyunwoo_q"); s.energy = 3;
        Check(status.PlayCard(0) && StatusMechanics.Has(s.enemyStatuses,"slow") && s.enemyWeak==0, "Hyunwoo's original slow replaces unrelated generic weakness");
        int beforeHp = status.State.hp; status.EndTurn();
        Check(!StatusMechanics.Has(s.enemyStatuses,"slow"), "original slow expires after the affected enemy turn");
    }

    private static void RewardsOnlyOnce()
    {
        var e = Ready(); Enter(e, ZoneKind.Subject); int beforeCredit = e.State.credits; Win(e);
        Check(e.State.row == -1 && e.State.credits == beforeCredit + e.State.rewards.credits, "rewards awarded once before route resolves");
        int credit = e.State.credits, budget = e.State.rewards.cardBudget;
        Check(!e.EndTurn() && !e.PlayCard(0) && e.State.credits == credit, "combat cannot re-award rewards");
        var id = e.State.rewards.choices.First(); int count = e.State.deck.Count, copies = e.State.deck.Count(x => x == id);
        if (e.RewardCardPrice(id) <= budget)
        {
            Check(e.ClaimCard(id) && e.State.deck.Count == count && e.State.rewards.taken.Contains(id), "selection remains pending before confirm");
            Check(e.RewardRemainingBudget == budget - e.RewardCardPrice(id) && e.State.rewards.cardBudget == budget, "pending selection computes remaining budget without changing total");
            Check(e.ClaimCard(id) && e.State.deck.Count == count && e.RewardRemainingBudget == budget, "click selected card cancels and refunds selection budget");
            Check(e.ClaimCard(id), "cancelled choice can be selected again");
        }
        bool selected = e.State.rewards.taken.Contains(id);
        Check(e.FinishRewards() && e.State.row == 0 && !e.FinishRewards(), "rewards resolve node exactly once");
        Check(e.State.deck.Count(x => x == id) == copies + (selected ? 1 : 0), "only confirmed reward appended exactly once");
        e = Ready(); Enter(e, ZoneKind.Wildlife); Win(e);
        e.State.rewards.choices = new List<string> { "basic_attack", "basic_guard" }; e.State.rewards.cardBudget = 1;
        Check(e.ClaimCard("basic_attack") && !e.ClaimCard("basic_guard"), "budget rejects alternate card while full");
        Check(e.ClaimCard("basic_attack") && e.ClaimCard("basic_guard"), "deselect enables an alternate card at original budget");
        count = e.State.deck.Count; e.FinishRewards();
        Check(e.State.deck.Count == count + 1 && e.State.deck.Last() == "basic_guard", "changed selection commits only the replacement");
    }

    private static void EnemyPreview()
    {
        var e = Ready(338); Enter(e, ZoneKind.Subject); var c = e.State.combat;
        e.State.hp = e.State.maxHp = 1000; e.State.gear.Clear(); e.State.passives.Clear(); e.State.mainRune = e.State.supportRune = null;
        c.enemyLevel = 20; c.enemyHp = 1000; c.enemyBlock = 0; c.enemyEvasion = 0; c.enemyStrength = 2; c.block = c.evasion = c.vulnerable = c.poison = 0;
        c.enemyPlan = new List<string> { "nadine_r", "basic_attack" }; c.enemyPlanCosts = c.enemyPlan.Select(e.CardCost).ToList();
        string preview = e.EnemyIntent; int initial = c.intentDamage;
        int firstHit = GameDatabase.Card("nadine_r").damage + c.enemyLevel / 2 + 2;
        int hits = GameDatabase.Card("nadine_r").hits;
        int nextHit = GameDatabase.Card("basic_attack").damage + c.enemyLevel / 2 + 2 + GameDatabase.Card("nadine_r").strength;
        Check(initial == firstHit * hits + nextHit + 5, "preview includes strength gained before subsequent attacks and Nadine's scheduled wolf attack");
        c.hand.Clear(); c.hand.Add("aya_r"); c.energy = 99;
        Check(e.PlayCard(0), "apply source fear during player turn");
        Check(c.intentDamage == firstHit * 4 / 5 * hits + nextHit * 4 / 5 + 5 && c.intentDamage < initial, "preview immediately updates after fear while the scheduled wolf attack remains distinct");
        int rng = e.State.rngState; var plan = c.enemyPlan.ToArray(); var draw = c.enemyDrawPile.ToArray(); int strength = c.enemyStrength;
        for (int i = 0; i < 5; ++i) preview = e.EnemyIntent;
        Check(e.State.rngState == rng && c.enemyPlan.SequenceEqual(plan) && c.enemyDrawPile.SequenceEqual(draw) && c.enemyStrength == strength, "preview never changes RNG, plans, piles or live strength");
        int expected = c.intentDamage, before = e.State.hp;
        Check(e.EndTurn() && before - e.State.hp == expected, "telegraphed raw damage matches unblocked, unevaded action sequence");
    }

    private static void WildlifeDropTables()
    {
        var drops = new Dictionary<string, HashSet<string>>();
        for (int seed = 1; seed <= 260; ++seed)
        {
            var e = Ready(seed); Enter(e, ZoneKind.Wildlife); string animal = e.State.combat.animal;
            Check(e.State.combat.enemyEvasion == 0, "wildlife never has evasion"); Win(e);
            string obj = e.State.rewards.objectId;
            if (!drops.ContainsKey(animal)) drops[animal] = new HashSet<string>();
            if (obj != null) drops[animal].Add(obj);
            if (animal == "chicken" || animal == "dog" || animal == "boar") Check(obj == null, "small wildlife no rare objects");
            if (animal == "wolf" && obj != null) Check(obj == "meteorite" || obj == "tree", "wolf only meteorite or tree");
            if (animal == "bear" && obj != null) Check(obj != "blood", "bear never VF blood sample");
        }
        Check(drops.Count == 5 && drops["wolf"].Count == 2 && drops["bear"].Count == 4, "all species and allowed drops sampled");
    }

    private static void SubjectDropTables()
    {
        int nearbyDrops = 0;
        for (int seed = 1; seed <= 70; ++seed)
        {
            var e = Ready(seed); var node = e.AvailableNodes().First(n => n.lane == 1); node.nearKiosk = false;
            Enter(e, ZoneKind.Subject); Win(e); Check(e.State.rewards.objectId == null, "distant subject drops no object");
            e = Ready(seed); node = e.AvailableNodes().First(n => n.lane == 1); node.nearKiosk = true;
            Enter(e, ZoneKind.Subject); Win(e);
            if (e.State.rewards.objectId != null) { nearbyDrops++; Check(e.State.rewards.objectId != "blood", "near kiosk excludes blood sample"); }
        }
        Check(nearbyDrops > 0 && nearbyDrops < 70, "near kiosk object chance is probabilistic");
    }

    private static void ShopEconomy()
    {
        var expected = new[] { 200, 200, 250, 350, 500 };
        Check(GameDatabase.Objects.Select(x => x.price).SequenceEqual(expected), "requested exact object prices");
        var e = Ready(); Enter(e, ZoneKind.Kiosk); e.State.credits = 499;
        Check(!e.BuyObject("blood") && e.State.credits == 499, "insufficient purchase is atomic");
        e.State.credits = 500; Check(e.BuyObject("blood") && e.State.credits == 0 && e.State.objects.Contains("blood"), "blood purchase consumes 500 credits");
        Check(!e.BuyFood(GameDatabase.Foods[0].id), "food cannot overspend credits");
    }

    private static void CampAndEquipment()
    {
        var e = Ready(); e.State.hp = 1; Enter(e, ZoneKind.Campfire);
        Check(e.State.hp == e.State.maxHp, "camp heals before either action");
        Check(e.ChooseCamp(true), "craft camp choice");
        var weapon = GameDatabase.Gear.First(x => x.slot == GearSlot.Weapon);
        var weapon2 = GameDatabase.Gear.First(x => x.slot == GearSlot.Weapon && x.cardId != weapon.cardId);
        for (int i = 0; i < 3; ++i) e.State.objects.Add(weapon.objectId);
        Check(e.Craft(weapon.id) && e.Craft(weapon.id), "two weapons equipped");
        Check(e.State.deck.Count(id => id == weapon.cardId) == 2, "each weapon grants its skill card");
        Check(!e.Craft(weapon.id) && e.State.campActions == 1, "third same-slot item needs explicit replacement");
        e.State.objects.Add(weapon2.objectId);
        Check(e.Craft(weapon2.id, 0) && e.State.gear.Count == 2 && e.State.campActions == 0, "weapon replacement consumes single action");
        Check(e.State.deck.Count(id => id == weapon.cardId) == 1 && e.State.deck.Count(id => id == weapon2.cardId) == 1, "old weapon skill removed on replacement");
        Check(!e.Craft(weapon.id), "fourth combined craft/cook action blocked");
        Check(e.LeaveCamp() && e.State.row == 0, "camp resolved after work");
        var hpGear = GameDatabase.Gear.First(x => x.health > 0 && x.slot == GearSlot.Clothes);
        var other = GameDatabase.Gear.First(x => x.slot == hpGear.slot && x.id != hpGear.id);
        Enter(e, ZoneKind.Campfire); e.ChooseCamp(true);
        int before = e.State.maxHp; e.State.objects.Add(hpGear.objectId); e.State.objects.Add(other.objectId);
        Check(e.Craft(hpGear.id) && e.State.maxHp == before + hpGear.health, "health gear increases max HP");
        Check(e.Craft(other.id, e.State.gear.Count - 1) && e.State.maxHp == before + other.health, "replaced health bonus removed accurately");
    }

    private static void FoodAndCooking()
    {
        var e = Ready(); Enter(e, ZoneKind.Campfire); Check(e.ChooseCamp(false), "soup choice");
        Check(e.State.foods.Contains("soup") && e.State.stage == RunStage.Map, "soup choice immediately resolves camp");
        e.State.hp = 1; Check(e.UseFood(0) && e.State.hp == e.State.maxHp && e.State.foods.Count == 0, "one-use soup fully heals");
        Enter(e, ZoneKind.Campfire); e.ChooseCamp(true);
        var ingredient = GameDatabase.Foods.First(x => !string.IsNullOrEmpty(x.upgradeTo));
        e.State.foods.Add(ingredient.id); e.State.foods.Add("soup");
        Check(e.Cook(0) && e.State.foods[0] == ingredient.upgradeTo && e.State.campActions == 2, "eligible ingredient is consumed into actual recipe output");
        Check(!e.Cook(1) && e.State.campActions == 2, "ineligible food cannot waste cooking actions");
    }

    private static void PassiveCapacity()
    {
        var e = Ready(); e.State.passives = GameDatabase.Passives.Take(3).Select(x => x.id).ToList();
        Enter(e, ZoneKind.Boss); var expected = GameDatabase.Character(e.State.combat.enemyId).passiveId;
        // Ensure this boss's passive is outside the current three-passive fixture.
        e.State.passives = GameDatabase.Passives.Where(x => x.id != expected).Take(3).Select(x => x.id).ToList();
        Win(e); e.FinishRewards();
        Check(e.State.stage == RunStage.PassiveChoice && e.State.passives.Count == 3 && e.State.pendingPassive == expected, "boss passive enforces capacity");
        Check(e.ReplacePassive(1) && e.State.passives[1] == expected && e.State.passives.Count == 3, "explicit passive replacement");
        e.State.pendingPassive = GameDatabase.Passives.First(x => !e.State.passives.Contains(x.id)).id; e.State.stage = RunStage.PassiveChoice;
        var old = e.State.passives.ToArray();
        Check(e.ReplacePassive(-1) && e.State.passives.SequenceEqual(old), "pending passive can be declined");
    }

    private static void EventEffects()
    {
        foreach (var def in GameDatabase.Events)
        {
            for (int i = 0; i < def.options.Length; ++i)
            {
                var e = Ready(89 + i); Enter(e, ZoneKind.Encounter);
                e.State.credits=1000;
                e.State.encounterOffers = new List<string> { def.id };
                Check(e.SelectEncounter(def.id), "select encounter " + def.id);
                Check(e.ChooseEventOption(i), "resolve event option " + def.id + "/" + i);
                if (e.State.runeChangePending)
                {
                    var main = GameDatabase.Runes.First(x => x.main && x.id != e.State.mainRune);
                    Check(e.SetRunes(main.id, GameDatabase.Runes.First(x => !x.main && x.tree == main.tree).id), "event unlocks matching rune change");
                    Check(e.FinishRuneChange() && !e.State.runeChangePending, "event rune change resolves zone");
                }
                Check(e.State.stage == RunStage.Map || e.State.stage == RunStage.Lost || e.State.stage == RunStage.PassiveChoice, "event terminates in valid state");
                Check(!e.ChooseEventOption(i), "event option cannot repeat");
            }
        }
    }

    private static void SaveDeterminism()
    {
        var e = Ready(910); Enter(e, ZoneKind.Subject);
        e.State.combat.enemyHp = e.State.combat.enemyMaxHp = 999; e.State.hp = e.State.maxHp = 999;
        var loaded = new GameEngine((RunState)Clone(e.State));
        for (int turn = 0; turn < 5; ++turn)
        {
            while (e.State.combat.hand.Count > 0)
            {
                int index = -1; for (int i = 0; i < e.State.combat.hand.Count; ++i) if (e.CanPlayCard(i)) { index = i; break; }
                if (index < 0) break;
                Check(e.PlayCard(index) == loaded.PlayCard(index), "restored card action matches");
            }
            Check(e.EndTurn() == loaded.EndTurn(), "restored turn matches");
            Check(e.State.rngState == loaded.State.rngState && e.State.hp == loaded.State.hp && e.State.combat.enemyHp == loaded.State.combat.enemyHp, "restored RNG and damage match");
            Check(e.State.combat.hand.SequenceEqual(loaded.State.combat.hand) && e.State.combat.enemyPlan.SequenceEqual(loaded.State.combat.enemyPlan), "restored piles and telegraph match");
        }
        var a = new GameEngine(17); var b = new GameEngine(17);
        Check(a.State.map.Select(x => x.kind).SequenceEqual(b.State.map.Select(x => x.kind)) && a.State.passiveOffers.SequenceEqual(b.State.passiveOffers), "seed reproducibly generates run");
    }

    private static List<string> SampleCards(GameEngine engine, IEnumerable<string> pool, int amount)
    {
        object[] args = { pool, amount, engine.State.rngState };
        var result = (List<string>)typeof(GameEngine).GetMethod("CardOffer", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(engine, args);
        engine.State.rngState = (int)args[2]; return result;
    }

    private static void PassiveCardOffers()
    {
        var e = new GameEngine(8181);
        foreach (var character in GameDatabase.Characters)
        {
            e.State.passives = new List<string> { character.passiveId };
            Check(character.cards.All(e.IsPassiveFavoredCard), "all four owner skills receive preference: " + character.id);
        }
        e.State.passives = new List<string> { "nia_p", "jackie_p", "aya_p", "nia_p", null, "missing_passive" };
        Check(new[] { "nia_q", "nia_w", "nia_e", "nia_r", "jackie_q", "aya_q" }.All(e.IsPassiveFavoredCard), "several owned passives favor each owner's QWER");
        Check(new[] { "hyunwoo_q", "tactical_blink", "weapon_glove", "basic_attack", "missing_card", null }.All(id => !e.IsPassiveFavoredCard(id)), "other skills, tactical, weapon, basic and invalid cards stay unfavored");

        var duplicate = new GameEngine((RunState)Clone(e.State));
        duplicate.State.passives = new List<string> { "nia_p", "nia_p", "missing_passive", null };
        e.State.passives = new List<string> { "nia_p" };
        bool duplicateMatches = true;
        var pair = new[] { "nia_q", "jackie_q" };
        for (int i = 0; i < 1000; ++i) duplicateMatches &= SampleCards(e, pair, 1).SequenceEqual(SampleCards(duplicate, pair, 1));
        Check(duplicateMatches && e.State.rngState == duplicate.State.rngState, "duplicate and invalid passives never stack or alter preference");

        var uniform = new GameEngine(917); uniform.State.passives.Clear();
        var weighted = new GameEngine((RunState)Clone(uniform.State)); weighted.State.passives.Add("nia_p");
        int normalHits = 0, favoredHits = 0;
        const int samples = 20000;
        for (int i = 0; i < samples; ++i)
        {
            if (SampleCards(uniform, pair, 1)[0] == "nia_q") normalHits++;
            if (SampleCards(weighted, pair, 1)[0] == "nia_q") favoredHits++;
        }
        Check(normalHits > samples * .48 && normalHits < samples * .52, "unowned equal candidates remain approximately 50/50");
        Check(favoredHits > samples * .58 && favoredHits < samples * .62 && favoredHits > normalHits + 1500, "1.5x weighting yields approximately 60/40 for one owned candidate");
        Console.WriteLine("Passive offer distribution: uniform " + normalHits + "/" + samples + ", favored " + favoredHits + "/" + samples + ".");

        var boundaryPool = new[] { "nia_q", "nia_q", "jackie_q", "basic_attack", "tactical_blink", null, "missing_card" };
        var all = SampleCards(weighted, boundaryPool, 99);
        Check(all.Count == 4 && all.Distinct().Count() == 4 && all.All(id => GameDatabase.Card(id) != null), "offers ignore invalid entries and sample unique valid candidates without replacement");
        int beforeRng = weighted.State.rngState;
        Check(SampleCards(weighted, new string[0], 3).Count == 0 && SampleCards(weighted, pair, 0).Count == 0 && weighted.State.rngState == beforeRng, "empty and zero-size offers consume no randomness");

        e = new GameEngine(3701);
        int draftSeed = e.State.draftSeed, rng = e.State.rngState;
        string first = e.State.passiveOffers[0], second = e.State.passiveOffers[1];
        Check(draftSeed != 0 && e.SelectStartingPassive(first) && e.State.draftBiasPassive == first && e.State.rngState == rng, "initial passive selection reweights the saved draft without an extra RNG roll");
        var firstOffers = e.State.draftOffers.ToArray();
        foreach (var id in firstOffers.Take(3)) e.ToggleDraft(id);
        var initialSelected = e.State.draftSelected.ToArray();
        Check(e.SelectStartingPassive(first) && e.State.draftOffers.SequenceEqual(firstOffers) && e.State.draftSelected.SequenceEqual(initialSelected), "reselecting the same passive preserves offers and selections");
        Check(e.SelectStartingPassive(second) && e.State.draftSelected.All(id => e.State.draftOffers.Contains(id)) && e.State.draftSeed == draftSeed, "switching passive retains only still-offered selections and uses the same draft roll");
        Check(e.SelectStartingPassive(first) && e.State.draftOffers.SequenceEqual(firstOffers) && e.State.rngState == rng, "switching away and back cannot farm new random offers");
        var loaded = new GameEngine((RunState)Clone(e.State));
        var storedSelection = e.State.draftSelected.ToArray();
        Check(loaded.State.draftOffers.SequenceEqual(firstOffers) && loaded.State.draftSelected.SequenceEqual(storedSelection) && loaded.SelectStartingPassive(first), "preparation save keeps weighted offers, selection and active passive");
        Check(e.RerollDraft() && loaded.RerollDraft() && e.State.draftSeed != draftSeed && e.State.draftOffers.SequenceEqual(loaded.State.draftOffers) && e.State.rngState == loaded.State.rngState && e.State.draftSelected.SequenceEqual(storedSelection), "explicit weighted draft reroll is reproducible and preserves selected cards after saving");
        Check(e.RerollPassives() && loaded.RerollPassives() && e.State.draftOffers.SequenceEqual(loaded.State.draftOffers) && e.State.draftBiasPassive == null && e.State.passives.Count == 0, "passive reroll removes its preference consistently without spending a card reroll");

        bool initialBiasChangesOffers = false;
        for (int seed = 1; seed <= 20; ++seed)
        {
            var fresh = new GameEngine(seed); var original = fresh.State.draftOffers.ToArray();
            fresh.State.passiveOffers = new List<string> { "nia_p" }; fresh.SelectStartingPassive("nia_p");
            initialBiasChangesOffers |= !fresh.State.draftOffers.SequenceEqual(original);
            Check(fresh.State.draftOffers.Count == 6 && fresh.State.draftOffers.Distinct().Count() == 6, "initial weighted draft still offers six different cards");
        }
        Check(initialBiasChangesOffers, "first displayed selected-passive draft actually uses the preference");

        foreach (int version in new[] { 1, 2 })
        {
            var old = new GameEngine(7901); old.State.version = version; old.State.draftSeed = 0; old.State.draftBiasPassive = null;
            string passive = old.State.passiveOffers[0]; old.State.chosenPassive = passive; old.State.passives = new List<string> { passive };
            old.ToggleDraft(old.State.draftOffers[0]); var offers = old.State.draftOffers.ToArray(); var selections = old.State.draftSelected.ToArray();
            loaded = new GameEngine((RunState)Clone(old.State));
            Check(loaded.SelectStartingPassive(passive) && loaded.State.draftOffers.SequenceEqual(offers) && loaded.State.draftSelected.SequenceEqual(selections), "legacy preparation snapshot remains intact for version " + version);
            Check(loaded.SelectStartingPassive(loaded.State.passiveOffers[1]) && loaded.State.draftOffers.SequenceEqual(offers) && loaded.State.draftSelected.SequenceEqual(selections), "changing a legacy preparation passive does not silently discard its saved cards");
            Check(loaded.RerollDraft() && loaded.State.draftSeed != 0 && loaded.State.draftBiasPassive == loaded.State.chosenPassive, "legacy explicit reroll opts into weighted cards");
        }

        e = Ready(7021); Enter(e, ZoneKind.Wildlife);
        e.State.passives = new List<string> { "nia_p", "jackie_p" };
        loaded = new GameEngine((RunState)Clone(e.State));
        Win(e); Win(loaded);
        Check(e.State.rewards.choices.SequenceEqual(loaded.State.rewards.choices) && e.State.rngState == loaded.State.rngState && e.State.rewards.choices.Count == 3 && e.State.rewards.choices.Distinct().Count() == 3, "wildlife reward preference resumes identically from a combat save with unique cards");
        // WinBattle spends one RNG draw for credits before drawing the weighted wildlife offer.
        var predicted = new GameEngine(2); predicted.State.passives = new List<string> { "nia_p" }; predicted.State.rngState = 987654321;
        e = Ready(8821); Enter(e, ZoneKind.Wildlife); e.State.passives = new List<string> { "nia_p" }; e.State.rngState = predicted.State.rngState;
        typeof(GameEngine).GetMethod("Next", BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(int) }, null).Invoke(predicted, new object[] { 31 });
        var expectedReward = SampleCards(predicted, GameDatabase.Cards.Where(c => c.category == "skill" || c.category == "tactical").Select(c => c.id), 3);
        typeof(GameEngine).GetMethod("WinBattle", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(e, null);
        Check(e.State.rewards.choices.SequenceEqual(expectedReward), "wildlife rewards call the weighted acquisition sampler");

        e = Ready(818); Enter(e, ZoneKind.Subject); loaded = new GameEngine((RunState)Clone(e.State));
        e.State.passives = new List<string> { "nia_p" }; loaded.State.passives = new List<string> { "aya_p" };
        var subjectSkills = e.State.combat.enemyDeck.Distinct().Where(id => GameDatabase.Card(id).category == "skill").ToArray();
        Win(e); Win(loaded);
        Check(e.State.rewards.choices.SequenceEqual(subjectSkills) && loaded.State.rewards.choices.SequenceEqual(subjectSkills), "subject rewards keep all eligible defeated subject skills regardless of player preference");

        e = Ready(889); Enter(e, ZoneKind.Encounter);
        var cardEvent = GameDatabase.Events.First(ev => ev.options.Any(o => o.effect == "card"));
        e.State.encounterOffers = new List<string> { cardEvent.id }; e.SelectEncounter(cardEvent.id);
        int option = Array.FindIndex(cardEvent.options, o => o.effect == "card"); string eventCard = cardEvent.options[option].cardId;
        int copies = e.State.deck.Count(id => id == eventCard); e.State.passives = new List<string> { "aya_p" };
        Check(e.ChooseEventOption(option) && e.State.deck.Count(id => id == eventCard) == copies + 1, "fixed event cards are awarded exactly as authored");

        e = Ready(209); loaded = new GameEngine((RunState)Clone(e.State));
        // These passives have no battle-start trigger; only their acquisition preference differs.
        e.State.passives = new List<string> { "nia_p" }; loaded.State.passives = new List<string> { "jackie_p" };
        Enter(e, ZoneKind.Wildlife); Enter(loaded, ZoneKind.Wildlife);
        Check(e.State.combat.hand.SequenceEqual(loaded.State.combat.hand) && e.State.combat.drawPile.SequenceEqual(loaded.State.combat.drawPile) && e.State.rngState == loaded.State.rngState, "passive card preference never biases combat hand shuffling");

        e.State.stage = RunStage.PassiveChoice; e.State.pendingPassive = "aya_p";
        Check(e.ReplacePassive(0) && !e.IsPassiveFavoredCard("nia_q") && e.IsPassiveFavoredCard("aya_q"), "replacing a passive immediately changes the owners favored in future offers");
    }

    private static GameEngine SkillBattle(int seed = 484)
    {
        var e = Ready(seed); e.State.passives.Clear(); e.State.gear.Clear(); e.State.mainRune = e.State.supportRune = null;
        Enter(e, ZoneKind.Wildlife); var c = e.State.combat;
        e.State.hp = e.State.maxHp = 999; c.enemyHp = c.enemyMaxHp = 9999; c.enemyBlock = c.enemyEvasion = c.strength = c.weak = c.enemyVulnerable = 0;
        c.energy = 99; return e;
    }
    private static void CastSkill(GameEngine e, string id)
    {
        var c = e.State.combat; c.hand = new List<string> { id }; c.drawPile.Clear(); c.discardPile.Clear(); c.exhaustPile.Clear();
        Check(e.PlayCard(0), "identity fixture casts " + id);
    }
    private static void NiaSkillIdentity()
    {
        var e = SkillBattle(); var c = e.State.combat;
        Check(GameDatabase.Card("nia_q").mechanics != null && GameDatabase.Card("nia_w").mechanics != null, "Nia Q and W have explicit skill mechanics");
        for (int i = 0; i < 4; ++i) CastSkill(e, "nia_q");
        Check(SkillMechanics.Resource(c.playerSkills, "니아", "blocks") == 3, "Nia Q leaves at most three persistent Arcade Blocks");
        int baseDamage = e.CardDamage("nia_w"), predicted = e.CardTotalDamage("nia_w"), beforeHp = c.enemyHp;
        c.energy = e.CardCost("nia_w"); CastSkill(e, "nia_w");
        Check(beforeHp - c.enemyHp == predicted && predicted > baseDamage && SkillMechanics.Resource(c.playerSkills, "니아", "blocks") == 0, "Nia W spends all blocks for their proportional extra damage");
        Check(c.energy == 0 && e.EffectiveCardCost("nia_q") == Math.Max(0, e.CardCost("nia_q") - 1), "Nia W discounts only the next Q after spending the last energy");
        c.hand = new List<string> { "nia_q" }; Check(e.CanPlayCard(0), "discounted next Nia Q can be cast at zero remaining energy");
        Check(e.PlayCard(0) && e.EffectiveCardCost("nia_q") == e.CardCost("nia_q") && SkillMechanics.Resource(c.playerSkills, "니아", "blocks") == 1, "discount is consumed once and Q starts a new block chain");
        c.enemyVulnerable = 0; c.energy = 99; beforeHp = c.enemyHp; predicted = e.CardTotalDamage("nia_w"); CastSkill(e, "nia_w");
        Check(beforeHp - c.enemyHp == predicted && predicted < baseDamage + 12, "one block provides less extra W damage than three blocks");
        e = SkillBattle(); c = e.State.combat; c.energy = 99; CastSkill(e, "nia_e");
        Check(c.playerSkills.effects.Any(x => x.kind == "revive" && x.sourceCard == "nia_e") && c.exhaustPile.Contains("nia_e"), "Nia 1UP installs a finite lethal prevention and exhausts");
        e.State.hp = 0; typeof(GameEngine).GetMethod("CheckBattleEnd", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(e, null);
        Check(e.State.stage == RunStage.Combat && e.State.hp > 0 && !c.playerSkills.effects.Any(x => x.kind == "revive") && e.CombatActions.Any(a => a.kind == "revive" && a.cardId == "nia_e" && a.heal > 0), "1UP prevents a fatal result exactly once and emits its own recovery action");
        e.State.hp = 0; typeof(GameEngine).GetMethod("CheckBattleEnd", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(e, null);
        Check(e.State.stage == RunStage.Lost, "used 1UP cannot prevent a second fatal result");
    }
    private static void SkillRulesAndIsolation()
    {
        var guard = GameDatabase.Card("basic_guard"); var attack = GameDatabase.Card("basic_attack");
        var oldGuard = guard.mechanics; var oldAttack = attack.mechanics; int oldHits = attack.hits;
        try
        {
            guard.mechanics = new SkillMechanicProfile { rules = new[] {
                new SkillRule { op="gain", key="charge", label="검증 충전", amount=1, cap=3 },
                new SkillRule { op="empower_basic", key="empower", amount=5, duration=1 },
                new SkillRule { op="set", key="stance", amount=1, cap=2 }
            }};
            attack.mechanics = new SkillMechanicProfile { rules = new[] {
                new SkillRule { op="bonus_damage", scaleKey="charge", amount=4, cap=3, conditionKey="stance", conditionAmount=1, conditionExact=true },
                new SkillRule { op="gain", key="landed", amount=1, cap=2, onHit=true },
                new SkillRule { op="consume", key="charge", amount=0, onHit=true },
                new SkillRule { op="set", key="stance", amount=2, cap=2, conditionKey="stance", conditionAmount=1, conditionExact=true }
            }};
            var e = SkillBattle(); CastSkill(e, "basic_guard"); var c = e.State.combat;
            Check(SkillMechanics.Resource(c.playerSkills, "하나", "charge") == 1 && SkillMechanics.Resource(c.playerSkills, "다른 실험체", "charge") == 0, "owner-scoped resources do not leak to another owner's identically named state");
            attack.hits = 3; int before = c.enemyHp, total = e.CardTotalDamage("basic_attack"); CastSkill(e, "basic_attack");
            Check(before - c.enemyHp == total && total == e.CardDamage("basic_attack") * 3 + 9, "resource and empowered-basic bonuses apply once to the whole multi-hit card");
            Check(SkillMechanics.Resource(c.playerSkills, "하나", "charge") == 0 && SkillMechanics.Resource(c.playerSkills, "하나", "stance") == 2 && !c.playerSkills.effects.Any(x => x.kind == "empower_basic"), "consume and conditional stance changes use the same pre-cast snapshot");
            CastSkill(e, "basic_guard"); c.enemyEvasion = 100; CastSkill(e, "basic_attack");
            Check(SkillMechanics.Resource(c.playerSkills, "하나", "charge") == 1 && SkillMechanics.Resource(c.playerSkills, "하나", "landed") == 1, "fully avoided attacks neither consume hit-gated resources nor add marks");
            c.enemyEvasion = 0; c.enemyBlock = 999; CastSkill(e, "basic_attack");
            Check(SkillMechanics.Resource(c.playerSkills, "하나", "charge") == 0 && SkillMechanics.Resource(c.playerSkills, "하나", "landed") == 2, "an attack absorbed by defense still counts as an actual hit for skill marks");

            var actor = new SkillActorState(); var alternate = new CardDef { id="nia_q", owner="니아", category="skill", key="Q", mechanics = new SkillMechanicProfile { rules=new[] { new SkillRule { op="gain", key="alternation", amount=1, cap=2, conditionPrevious="nia_w" } } } };
            actor.history.Add(new SkillOwnerHistory { owner="니아", cardId="nia_w" });
            SkillMechanics.AfterCard(actor, SkillMechanics.Clone(actor), alternate, true);
            Check(SkillMechanics.Resource(actor, "니아", "alternation") == 1, "explicit previous-own-skill conditions implement alternation chains");
            alternate.owner = "아야"; SkillMechanics.AfterCard(actor, SkillMechanics.Clone(actor), alternate, true);
            Check(SkillMechanics.Resource(actor, "아야", "alternation") == 0, "foreign skills never satisfy another owner's history condition");
        }
        finally { guard.mechanics=oldGuard; attack.mechanics=oldAttack; attack.hits=oldHits; }
    }
    private static void BinarySkillStates()
    {
        var actor = new SkillActorState(); var r = GameDatabase.Card("irem_r");
        Check(r.mechanics.rules.Count(x=>x.op=="state_toggle" && x.key=="cat")==1 && !r.mechanics.rules.Any(x=>x.key=="cat" && new[]{"gain","set"}.Contains(x.op)), "Irem R has one explicit form toggle instead of a numerical increment/reset pair");
        for (int use=1;use<=6;++use)
        {
            SkillMechanics.AfterCard(actor,SkillMechanics.Clone(actor),r,true);
            bool cat=use%2==1;
            Check(SkillMechanics.Resource(actor,"이렘","cat")== (cat ? 1 : 0), "Irem transformation reverses on use " + use);
            Check(SkillMechanics.Bonuses(actor,GameDatabase.Card("irem_q")).damage== (cat ? 4 : 0) && SkillMechanics.Bonuses(actor,GameDatabase.Card("irem_w")).block== (cat ? 6 : 0), "Irem Q and W retain their form-dependent effects on use " + use);
            var visible=SkillMechanics.Snapshot(actor).Where(x=>x.key=="cat").ToArray();
            Check(cat ? visible.Length==1 && visible[0].kind=="state" && visible[0].label=="고양이 상태" : visible.Length==0, "only active cat state is visible without a numerical stack token");
        }
        foreach (var id in new[]{"rio_q","estelle_e","jenny_e"})
        {
            var card=GameDatabase.Card(id); var toggle=card.mechanics.rules.First(x=>x.op=="state_toggle");
            var isolated=new SkillActorState();
            SkillMechanics.AfterCard(isolated,SkillMechanics.Clone(isolated),card,true);
            Check(SkillMechanics.Resource(isolated,card.owner,toggle.key)==1,"enter explicit form: "+id);
            SkillMechanics.AfterCard(isolated,SkillMechanics.Clone(isolated),card,true);
            Check(SkillMechanics.Resource(isolated,card.owner,toggle.key)==0,"leave explicit form: "+id);
        }
        var old=new SkillActorState{resources=new List<SkillResource>{new SkillResource{owner="이렘",key="cat",label="고양이",amount=1,cap=1}}};
        SkillMechanics.Ensure(old);
        Check(old.resources[0].isState && old.resources[0].amount==1 && old.resources[0].label=="고양이 상태", "existing ongoing saves migrate active forms without changing values or keys");
        var copy=SkillMechanics.Clone(old); SkillMechanics.AfterCard(copy,SkillMechanics.Clone(copy),r,true);
        Check(old.resources[0].amount==1 && copy.resources[0].amount==0 && copy.resources[0].isState, "enemy planning and save clones preserve state metadata and never mutate live forms");
        Check(!SkillMechanics.Summary(old).Contains("1/1") && !SkillMechanics.Summary(old).Contains("0턴"), "state summary has no stack denominator or fabricated duration");
        actor=new SkillActorState(); var guard=GameDatabase.Card("nicky_w");
        for(int use=0;use<3;++use)SkillMechanics.AfterCard(actor,SkillMechanics.Clone(actor),guard,true);
        Check(actor.resources.Single(x=>x.key=="guard_ready").amount==1 && actor.resources.Single(x=>x.key=="guard_ready").isState, "reapplying a preparation activates a state rather than accumulating stacks");
        var legacyPhysical=new SkillActorState();
        foreach(var cardId in new[]{"tsubame_e","adela_w","lucia_w"})
        {
            var card=GameDatabase.Card(cardId);SkillMechanics.AfterCard(legacyPhysical,SkillMechanics.Clone(legacyPhysical),card,true);
        }
        foreach(var key in new[]{"wooden_log","knight","crystal"})Check(SkillMechanics.Snapshot(legacyPhysical).Any(x=>x.key==key && x.kind=="resource" && x.cap==1), "physical single-unit resource stays numerical: "+key);
        var e=MechanicsFixture("sua_p");var combat=e.State.combat;combat.hand=new List<string>{"nia_q","basic_attack"};
        Check(e.PlayCard(0) && e.SkillStateSnapshot().Any(x=>x.owner=="trait:sua_p" && x.key=="book" && x.kind=="state"), "another owner's skill can activate a passive preparation state");
        Check(e.PlayCard(0) && !e.SkillStateSnapshot().Any(x=>x.owner=="trait:sua_p" && x.key=="book"), "basic attack releases the preparation state once");
        string description=string.Join(" ",SkillMechanics.Describe(r));
        Check(description.Contains("고양이 상태로 진입합니다") && description.Contains("고양이 상태인 경우에는 고양이 상태를 해제합니다") && !description.Contains("고양이를 1"), "full Irem R description explains reversible transformation");
        foreach(var card in GameDatabase.Cards.Where(x=>x.mechanics!=null))
        {
            foreach(var rule in card.mechanics.rules.Where(x=>x.isState))Check(rule.cap==1 && new[]{"state_on","state_off","state_toggle"}.Contains(rule.op), "every classified skill state uses a binary state operation: "+card.id+"/"+rule.key);
            foreach(var rule in card.mechanics.rules.Where(x=>new[]{"gain","set","damage_resource"}.Contains(x.op)))Check(!SkillMechanics.IsState(card,rule.key), "state classification never masks a multi-value skill resource: "+card.id+"/"+rule.key);
        }
        foreach(var passive in GameDatabase.Passives)
            foreach(var rule in passive.mechanics.rules.Where(x=>new[]{"gain","set","damage_resource"}.Contains(x.op)))Check(!SkillMechanics.IsState("trait:"+passive.id,rule.key), "state classification never masks a multi-value passive resource: "+passive.id+"/"+rule.key);
        Check(!GameDatabase.Passive("camilo_p").description.Contains("스텝을 1로") && GameDatabase.Passive("aiden_p").description.Contains("과전하 완료 상태"), "one-use alternating and overcharged passive preparations are named states");
        int namedStatusConditions=0;
        foreach(var card in GameDatabase.Cards)
        {
            string statusDescription=string.Join(" ",StatusMechanics.Describe(card));
            foreach(var status in card.statuses ?? new CardStatusRule[0])
            {
                if(SkillMechanics.IsState(card,status.conditionKey))
                {
                    ++namedStatusConditions;
                    Check(statusDescription.Contains(SkillMechanics.StateCondition(card,status.conditionKey,status.conditionAmount,status.conditionExact)), "full control description uses named active/inactive state: "+card.id+"/"+status.conditionKey);
                    Check(!statusDescription.Contains(SkillMechanics.StateName(card,status.conditionKey)+"가 "+status.conditionAmount) && !statusDescription.Contains(SkillMechanics.StateName(card,status.conditionKey)+"이 "+status.conditionAmount), "full control description omits numerical state condition: "+card.id+"/"+status.conditionKey);
                }
                if(SkillMechanics.IsState(card,status.conditionKey2))Check(statusDescription.Contains(SkillMechanics.StateCondition(card,status.conditionKey2,status.conditionAmount2,status.conditionExact2)), "secondary control condition uses named state: "+card.id);
            }
        }
        Check(namedStatusConditions==8, "all eight authored control rules that depend on binary states are covered");
        string rioStatus=string.Join(" ",StatusMechanics.Describe(GameDatabase.Card("rio_r")));
        Check(rioStatus.Contains("장궁 자세 상태이면") && rioStatus.Contains("장궁 자세 상태가 아니면"), "Rio control descriptions distinguish longbow active and inactive without 0/1 values");
        Check(string.Join(" ",StatusMechanics.Describe(GameDatabase.Card("nia_w"))).Contains("아케이드 블록이 1 이상이면"), "numerical resource control conditions retain their actual threshold");
    }
    private static void SkillTimedEffects()
    {
        var guard = GameDatabase.Card("basic_guard"); var old = guard.mechanics;
        try
        {
            guard.mechanics = new SkillMechanicProfile { rules = new[] {
                new SkillRule { op="bleed", key="bleed", amount=3, duration=2 },
                new SkillRule { op="burn", key="burn", amount=2, duration=2 },
                new SkillRule { op="delayed_damage", key="trap", amount=7, delay=2 },
                new SkillRule { op="summon", key="summon", amount=4, duration=2 },
                new SkillRule { op="hot", key="hot", amount=3, duration=2 },
                new SkillRule { op="guard", key="guard", amount=5, duration=2 }
            }};
            var e = SkillBattle(); CastSkill(e, "basic_guard"); var c = e.State.combat; e.State.hp = 100; c.enemyBlock = 20;
            int hp = c.enemyHp; var tick = typeof(GameEngine).GetMethod("ResolveSkillTicks", BindingFlags.NonPublic | BindingFlags.Instance);
            tick.Invoke(e, new object[] { false });
            Check(hp - c.enemyHp == 5 && c.enemyBlock == 16 && e.State.hp == 103, "bleed and burn tick separately through block while summons use block and HoT heals");
            Check(c.playerSkills.effects.Any(x => x.kind == "delayed_damage" && x.delay == 1) && c.enemyPoison == 0 && c.block >= 10, "traps wait for their delay and periodic guard creates real defense independent of poison");
            c.enemyBlock = 0; hp = c.enemyHp; tick.Invoke(e, new object[] { false });
            Check(hp - c.enemyHp == 16 && e.State.hp == 106 && c.playerSkills.effects.Count == 0, "second tick detonates the trap once and finite summons/DoT/HoT expire");
            Check(e.CombatActions.Any(a => a.cardId == "basic_guard" && a.kind == "delayed_damage" && a.damage == 7) && e.CombatActions.Any(a => a.kind == "guard" && a.block == 5), "every delayed, damage, healing and guard pulse preserves the source-card action for VFX");
            guard.mechanics = new SkillMechanicProfile { rules=new[] { new SkillRule { op="delayed_damage", key="fatal_trap", amount=10, delay=1 } } };
            e = SkillBattle(); CastSkill(e, "basic_guard"); e.State.combat.enemyHp = 3; e.State.combat.enemyBlock = 0;
            tick.Invoke(e, new object[] { false });
            Check(e.State.stage == RunStage.Rewards && e.CombatActions.Any(a => a.kind == "delayed_damage" && a.damage == 3), "delayed lethal damage ends combat and emits the actual lethal effect before rewards");
            guard.mechanics = new SkillMechanicProfile { rules=new[] { new SkillRule { op="counter", key="counter", amount=4, duration=2 } } };
            e = SkillBattle(); CastSkill(e, "basic_guard"); c = e.State.combat;
            Check(SkillMechanics.Counters(c.playerSkills).Count == 1 && SkillMechanics.Counters(c.playerSkills).Count == 0, "counter can react only once per actor turn");
            SkillMechanics.StartTurn(c.playerSkills);
            Check(SkillMechanics.Counters(c.playerSkills).Count == 1, "counter is available again on its second protected turn");
            SkillMechanics.StartTurn(c.playerSkills);
            Check(SkillMechanics.Counters(c.playerSkills).Count == 0, "counter protection expires within its capped duration");
        }
        finally { guard.mechanics=old; }
    }
    private static void SkillEnemyPlanning()
    {
        var e = SkillBattle(109); var c = e.State.combat; c.animal = null; c.enemyId = "nia"; c.enemyName = "니아"; c.enemyLevel=1;
        c.enemyDeck = new List<string> { "nia_q", "nia_w", "nia_q" }; c.enemyDrawPile = new List<string> { "nia_q", "nia_w", "nia_q" }; c.enemyDiscardPile.Clear(); c.enemyExhaustPile.Clear(); c.enemyLastSkills.Clear();
        c.enemySkills = new SkillActorState(); e.State.hp=e.State.maxHp=9999; c.evasion=0;
        typeof(GameEngine).GetMethod("PlanEnemyTurn", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(e, null);
        Check(c.enemyPlan.Take(3).SequenceEqual(new[] { "nia_q", "nia_w", "nia_q" }) && c.enemyPlanCosts[2] == Math.Max(0, e.CardCost("nia_q") - 1), "enemy planner simulates Nia block generation, consumption and the next-Q discount");
        Check(c.enemySkills.resources.Count == 0 && c.enemySkills.discounts.Count == 0, "planning never modifies live enemy resources or discounts");
        int rng = e.State.rngState, raw = c.intentDamage; var plan = c.enemyPlan.ToArray(); var costs = c.enemyPlanCosts.ToArray();
        for (int i=0;i<5;++i) { string intent=e.EnemyIntent; var tokens=e.SkillStateSnapshot(true); }
        Check(e.State.rngState == rng && c.intentDamage == raw && c.enemySkills.resources.Count == 0 && c.enemyPlan.SequenceEqual(plan) && c.enemyPlanCosts.SequenceEqual(costs), "repeated enemy intent and resource snapshots consume no RNG and mutate no live actor state");
        c.block = c.vulnerable = c.weak = 0; int hp=e.State.hp;
        Check(e.EndTurn() && hp - e.State.hp == raw, "resource-dependent enemy damage matches its all-hit unblocked preview");
        Check(SkillMechanics.Resource(c.enemySkills, "니아", "blocks") <= 3 && c.enemySkills.discounts.Count <= 1, "enemy execution uses the same finite skill-resource and discount model");
    }
    private static void SkillPersistenceAndLimits()
    {
        var e = SkillBattle(); CastSkill(e, "nia_q"); CastSkill(e, "nia_w"); var c=e.State.combat;
        var loaded = new GameEngine((RunState)Clone(e.State));
        Check(loaded.ResourceSummary() == e.ResourceSummary() && loaded.EffectiveCardCost("nia_q") == e.EffectiveCardCost("nia_q") && loaded.State.rngState == e.State.rngState, "saved resources, skill discounts and history resume with identical RNG");
        for (int version=1;version<=2;++version)
        {
            var old=(RunState)Clone(e.State); old.version=version; old.combat.playerSkills=null; old.combat.enemySkills=null; old.combat.enemyPlanFreeCast=null;
            var migrated=new GameEngine(old);
            Check(migrated.State.combat.playerSkills != null && migrated.State.combat.enemySkills != null && migrated.SkillStateSnapshot().Count == 0, "old version " + version + " combat restores empty skill mechanics safely");
        }
        var actor = new SkillActorState(); var card=new CardDef { id="nia_w",owner="니아",category="skill",key="W",mechanics=new SkillMechanicProfile { rules=new[] {
            new SkillRule { op="gain",key="bounded",amount=999,cap=999 },
            new SkillRule { op="discount",targetCard="nia_q",amount=99 },
            new SkillRule { op="summon",key="bounded_summon",amount=999,duration=999 },
            new SkillRule { op="revive",key="bounded_revive",amount=999,duration=999 }
        } } };
        for (int i=0;i<100;++i) SkillMechanics.AfterCard(actor,SkillMechanics.Clone(actor),card,true);
        Check(SkillMechanics.Resource(actor,"니아","bounded") == 8 && actor.discounts.Count == 1 && actor.discounts[0].amount == 7 && actor.effects.All(x=>x.remaining<=3), "resources, next-cast discounts and persistent durations cannot stack without bounds");
        SkillMechanics.ConsumeDiscount(actor,"nia_q"); SkillMechanics.AfterCard(actor,SkillMechanics.Clone(actor),card,true);
        Check(actor.discounts.Count == 0, "replaying one discount source in the same turn cannot replenish a consumed free-cost loop");
        SkillMechanics.StartTurn(actor); SkillMechanics.AfterCard(actor,SkillMechanics.Clone(actor),card,true);
        Check(actor.discounts.Count == 1, "a discount source can operate normally again on the next own turn");
        var copy = SkillMechanics.Clone(actor); copy.resources[0].amount=0;
        Check(SkillMechanics.Resource(actor,"니아","bounded") == 8, "preview actor snapshots own their lists and entries");
        var missingMechanics=GameDatabase.Cards.Where(x=>x.category=="skill" && (x.mechanics==null || (x.mechanics.rules.Length==0 && x.freeCastCount==0))).Select(x=>x.id).ToArray();
        Check(missingMechanics.Length==0, "every subject skill has an authored persistent mechanism or explicit source recast: "+string.Join(",",missingMechanics));
    }

    private static GameEngine MechanicsFixture(string passive=null,string enemy="fixture")
    {
        var e=Ready();e.State.passives.Clear();if(passive!=null)e.State.passives.Add(passive);
        e.State.mainRune=e.State.supportRune=null;e.State.gear.Clear();e.State.persistentTraits=new TraitActorState();
        Enter(e,ZoneKind.Wildlife);var c=e.State.combat;
        e.State.hp=e.State.maxHp=100;c.energy=99;c.block=c.strength=c.evasion=c.evasionTurns=0;
        c.enemyId=enemy;c.animal=null;c.enemyHp=c.enemyMaxHp=999;c.enemyBlock=c.enemyStrength=c.enemyEvasion=c.enemyWeak=c.enemyVulnerable=0;c.enemyLevel=1;
        c.enemyTraits=new TraitActorState();c.enemySkills=new SkillActorState();c.enemyOpeningEnergy=0;
        c.enemyDeck.Clear();c.enemyDrawPile.Clear();c.enemyDiscardPile.Clear();c.enemyExhaustPile.Clear();c.enemyHand.Clear();c.enemyPlan.Clear();c.enemyPlanCosts.Clear();c.enemyPlanFreeCast.Clear();
        c.hand.Clear();c.drawPile.Clear();c.discardPile.Clear();c.exhaustPile.Clear();e.CombatActions.Clear();return e;
    }

    private static void TraitCoverageAndDescriptions()
    {
        Check(GameDatabase.Passives.Count==91 && GameDatabase.Runes.Count==16,"complete authored passive and rune rosters");
        foreach(var p in GameDatabase.Passives)Check(p.mechanics!=null && p.mechanics.rules.Length>0 && p.mechanics.replaceLegacy,"global original trait profile: "+p.id);
        foreach(var r in GameDatabase.Runes)Check(r.mechanics!=null && r.mechanics.rules.Length>0 && r.mechanics.replaceLegacy,"original rune profile: "+r.id);
        string nathapon=GameDatabase.Passive("nathapon_p").description;
        Check(nathapon.Contains("구도 × 1") && nathapon.Contains("최대 3"),"scaled passive description names its numerical resource and effective maximum");
        Check(GameDatabase.Passive("tsubame_p").description.Contains("3 이상이면"),"conditional maximum-mark passive description includes its numerical condition");
        Check(GameDatabase.Passive("fenrir_p").description.Contains("사용될 때까지"),"persistent fatal prevention is described as lasting until used");
        Check(GameDatabase.Passive("isaac_p").description.Contains("기본 공격 사용 3회마다"),"third-basic passive describes its actual trigger count");
        foreach(var card in GameDatabase.Cards.Where(x=>x.mechanics!=null))Check(!string.Join(" ",SkillMechanics.Describe(card)).Contains("(를)") && !string.Join(" ",SkillMechanics.Describe(card)).Contains("이(가)"),"mechanic sentence has natural particles: "+card.id);
    }

    private static void GlobalPassiveCombat()
    {
        var e=MechanicsFixture("isaac_p");var c=e.State.combat;e.State.hp=50;
        c.hand=new List<string>{"basic_attack","basic_guard","basic_attack","basic_attack"};int basic=e.CardDamage("basic_attack");
        Check(e.CardTotalDamage("basic_attack")==basic+2 && e.PlayCard(0),"Isaac adds damage to the universal basic attack");
        Check(e.PlayCard(0) && e.CardTotalDamage("basic_attack")==basic+2 && e.PlayCard(0),"guard does not count toward Isaac's third basic attack");
        Check(e.CardTotalDamage("basic_attack")==basic+6 && e.PlayCard(0) && e.State.hp==54,"Isaac's third basic adds both damage and actual healing");
        Check(e.CombatActions.Count(x=>x.traitId=="isaac_p")>=4,"passive activations retain their identity for effects");
        e=MechanicsFixture("yuki_p");c=e.State.combat;c.hand.Add("aya_q");int predicted=e.CardTotalDamage("aya_q"),before=c.enemyHp;
        Check(e.PlayCard(0) && before-c.enemyHp==predicted && SkillMechanics.Resource(c.playerTraits.skills,"trait:yuki_p","cuff")==2,"Yuki's buttons strengthen a different owner's skill and consume once per card");
        e=MechanicsFixture("abigail_p");c=e.State.combat;c.hand=new List<string>{"nia_q","basic_attack"};
        Check(e.PlayCard(0) && TraitMechanics.EmpowerBonus(c.playerTraits,GameDatabase.Card("basic_attack"))==4,"a foreign skill can prepare Abigail's next basic attack");
        int hp=c.enemyHp;basic=e.CardDamage("basic_attack");Check(e.PlayCard(0) && hp-c.enemyHp==basic+4 && TraitMechanics.EmpowerBonus(c.playerTraits,GameDatabase.Card("basic_attack"))==0,"trait basic empowerment resolves once and is consumed");
    }

    private static void GlobalRuneCombat()
    {
        var e=MechanicsFixture();var c=e.State.combat;e.State.mainRune="amplification_drone";
        c.hand=new List<string>{"aya_r","basic_guard","charlotte_w"};c.energy=10;int price=e.CardCost("aya_r");
        Check(e.PlayCard(0) && e.MaxEnergy==6 && c.energy==10-price+1,"any owner's R grants a modest current-turn energy increase");
        Check(TraitMechanics.DamageBonus(c.playerTraits)==1 && e.CardTotalDamage("basic_guard")==0 && e.CardTotalDamage("charlotte_w")==0,"amplification increases attacks without turning preparation or healing into attacks");
        int hp=c.enemyHp;Check(e.PlayCard(0) && c.enemyHp==hp,"amplified guard leaves enemy health unchanged");
        Check(e.EndTurn() && e.MaxEnergy==6 && c.energy==6,"amplification maximum energy lasts through the next own turn");
        Check(e.EndTurn() && e.MaxEnergy==5 && TraitMechanics.DamageBonus(c.playerTraits)==0,"amplification ends after two own turns");
        e=MechanicsFixture();c=e.State.combat;e.State.mainRune="diamond";c.hand.Add("hyunwoo_q");
        Check(e.PlayCard(0) && e.CombatActions.Any(x=>x.traitId=="diamond" && x.block==4),"control applied by a foreign skill triggers the defensive rune");
        Check(e.BeginEndTurn() && e.CombatActions.Any(x=>x.traitId=="diamond" && x.kind=="delayed_damage" && x.damage==2),"rune delayed damage preserves its source identity");
    }

    private static void TraitHitAndHealingHooks()
    {
        var e=MechanicsFixture("arda_p");var c=e.State.combat;e.State.hp=50;c.hand=new List<string>{"charlotte_w","nia_q"};
        Check(e.PlayCard(0) && SkillMechanics.Resource(c.playerTraits.skills,"trait:arda_p","antiquity")==0,"healing setup does not count as a skill hit");
        c.enemyBlock=999;Check(e.PlayCard(0) && SkillMechanics.Resource(c.playerTraits.skills,"trait:arda_p","antiquity")==1,"a landed skill blocked by armor still counts as a hit");
        e=MechanicsFixture(null,"aya");c=e.State.combat;e.State.hp=50;c.hand.Add("charlotte_w");
        Check(e.PlayCard(0) && c.enemyTraits.counters.Count==0 && c.enemyBlock==0,"healing setup does not consume the defender's before-incoming protection");
        e.EndTurn();c.hand.Add("basic_attack");c.energy=99;Check(e.PlayCard(c.hand.Count-1) && e.CombatActions.Any(x=>x.enemy && x.traitId=="aya_p" && x.block==5),"Aya's protection is ready for the later real attack");
        e=MechanicsFixture("dailin_p");c=e.State.combat;e.State.hp=50;e.State.foods.Add("soup");
        Check(e.UseFood(0) && SkillMechanics.Resource(c.playerTraits.skills,"trait:dailin_p","bac")==1,"combat food recovery fires global healing traits");
        e=MechanicsFixture("tia_p");c=e.State.combat;e.State.passives.Add("dailin_p");e.State.hp=50;
        c.playerTraits.skills.resources.Add(new SkillResource{owner="trait:tia_p",key="mixed",label="혼합",amount=1,cap=3});
        // A synthetic before-card bonus heal must not masquerade as the original card's recovery event.
        var tia=GameDatabase.Passive("tia_p");var profile=tia.mechanics;
        try{tia.mechanics=new TraitMechanicProfile{rules=new[]{new TraitRule{trigger="before_card",op="bonus_heal",amount=2}}};c.hand.Add("basic_attack");Check(e.PlayCard(0) && e.State.hp==52 && SkillMechanics.Resource(c.playerTraits.skills,"trait:dailin_p","bac")==0,"trait bonus healing cannot recursively grant another healing trait");}
        finally{tia.mechanics=profile;}
        e=MechanicsFixture();c=e.State.combat;c.enemyHp=50;c.enemyMaxHp=100;
        c.enemyTraits.buffs.Add(new TraitBuff{sourceId="fixture",kind="heal_reduction",amount=20,remaining=2});c.enemySkills.effects.Add(new SkillTimedEffect{owner="펜리르",key="hot",kind="hot",sourceCard="fenrir_r",amount=3,remaining=1});
        e.BeginEndTurn();e.AdvanceEnemyAction();Check(c.enemyHp==52,"enemy HoT uses the same healing reduction as player recovery");
    }

    private static void TraitPersistenceAndEconomy()
    {
        var e=MechanicsFixture("nadine_p");Win(e);e.FinishRewards();Enter(e,ZoneKind.Wildlife);
        Check(SkillMechanics.Resource(e.State.combat.playerTraits.skills,"trait:nadine_p","wild")==1,"victory growth persists into the next battle");
        var loaded=new GameEngine((RunState)Clone(e.State));Check(SkillMechanics.Resource(loaded.State.combat.playerTraits.skills,"trait:nadine_p","wild")==1,"persistent trait growth survives saved continuation");
        e.State.stage=RunStage.PassiveChoice;e.State.pendingPassive="aya_p";Check(e.ReplacePassive(0) && !e.State.persistentTraits.skills.resources.Any(x=>x.owner=="trait:nadine_p") && !e.SkillStateSnapshot().Any(x=>x.owner=="trait:nadine_p"),"replacing a passive discards its saved growth and visible tokens");
        e=MechanicsFixture("jenny_p");var c=e.State.combat;
        for(int turn=0;turn<5;++turn)e.EndTurn();Check(c.playerTraits.skills.effects.Any(x=>x.kind=="revive" && x.persistent),"Jenny's protection survives more than three turns until used");
        e.State.hp=1;c.enemyPlan=new List<string>{"basic_attack"};c.enemyPlanCosts=new List<int>{1};c.enemyPlanFreeCast=new List<bool>{false};e.BeginEndTurn();e.AdvanceEnemyAction();
        Check(e.State.stage==RunStage.Combat && e.State.hp==18 && e.CombatActions.Any(x=>x.traitId=="jenny_p" && x.kind=="revive"),"persistent passive revive prevents exactly one fatal attack");
        loaded=new GameEngine((RunState)Clone(e.State));Check(!loaded.State.combat.playerTraits.skills.effects.Any(x=>x.kind=="revive"),"loading a consumed protection cannot reload it");
        foreach(int version in new[]{1,2}){var old=(RunState)Clone(e.State);old.version=version;old.combat.playerTraits=old.combat.enemyTraits=null;old.persistentTraits=null;int hp=old.hp,rng=old.rngState;loaded=new GameEngine(old);Check(loaded.State.hp==hp && loaded.State.rngState==rng && !loaded.State.combat.playerTraits.skills.effects.Any(),"old ongoing combat preserves health and RNG; new opening traits start next battle v"+version);}
        e=MechanicsFixture("silvia_p");e.State.stage=RunStage.Map;e.State.persistentTraits.skills.resources.Clear();e.State.persistentTraits.skills.resources.Add(new SkillResource{owner="trait:silvia_p",key="visited",label="탐방",amount=3,cap=4,persistent=true});Enter(e,ZoneKind.Wildlife);
        Check(e.State.combat.energy==GameEngine.EnergyForLevel(e.State.level)+1,"battle-start action energy is preserved into the initial player turn");
        e=Ready();e.State.supportRune="coupon";e.State.passives=new List<string>{"katja_p"};e.State.credits=1000;Enter(e,ZoneKind.Kiosk);
        Check(e.ObjectPrice("meteorite")==180 && e.FoodPrice("soup")==GameDatabase.Food("soup").price*9/10,"shop quotes a single non-stacking coupon discount for objects and food");
        int credits=e.State.credits;Check(e.BuyObject("meteorite") && e.State.credits==credits-180,"shop purchase charges exactly the displayed discounted price");
    }

    private static void TraitEnemyIntent()
    {
        var e=MechanicsFixture(null,"nia");var c=e.State.combat;e.State.hp=20;c.enemyLevel=4;c.enemyPlan=new List<string>{"nia_q","nia_q"};c.enemyPlanCosts=new List<int>{1,1};c.enemyPlanFreeCast=new List<bool>{false,false};
        int rng=e.State.rngState;for(int i=0;i<20;++i){string intent=e.EnemyIntent;}
        Check(c.intentDamage==20 && e.State.hp==20 && e.State.rngState==rng && c.enemyTraits.skills.resources.Count==0,"Nia intent accumulates damage, previews same-hit execution and leaves real health/resources/RNG untouched");
        e.BeginEndTurn();e.AdvanceEnemyAction();e.AdvanceEnemyAction();Check(e.State.stage==RunStage.Lost && e.CombatActions.Where(x=>x.enemy).Sum(x=>x.damage)==20,"predicted Nia execution agrees with actual sequential damage");
        e=MechanicsFixture(null,"isaac");c=e.State.combat;e.State.hp=e.State.maxHp=999;c.enemyPlan=new List<string>{"basic_attack","basic_attack","basic_attack"};c.enemyPlanCosts=new List<int>{1,1,1};c.enemyPlanFreeCast=new List<bool>{false,false,false};
        string description=e.EnemyIntent;int expected=c.intentDamage;var before=(CombatState)Clone(c);rng=e.State.rngState;
        for(int i=0;i<10;++i){description=e.EnemyIntent;}
        Check(c.enemyTraits.counters.Count==before.enemyTraits.counters.Count && e.State.rngState==rng,"global enemy passive intent is a pure deterministic clone simulation");
        e.BeginEndTurn();e.AdvanceEnemyAction();e.AdvanceEnemyAction();e.AdvanceEnemyAction();Check(e.CombatActions.Where(x=>x.enemy && x.kind==null).Sum(x=>x.damage)==expected,"enemy uses the same global third-basic damage rules as player");
        e=MechanicsFixture(null,"sissela");c=e.State.combat;c.enemyHp=49;c.enemyMaxHp=100;c.enemyPlan=new List<string>{"nia_q"};c.enemyPlanCosts=new List<int>{1};c.enemyPlanFreeCast=new List<bool>{false};
        description=e.EnemyIntent;expected=c.intentDamage;e.BeginEndTurn();e.AdvanceEnemyAction();Check(c.enemyHp==52 && e.CombatActions.Last(x=>x.enemy && x.kind==null).damage==expected,"intent applies opening healing before checking low-health damage conditions");
    }

    private static void ConditionalSkillRecallAndEffects()
    {
        var e=MechanicsFixture();var c=e.State.combat;c.enemySkills.resources.Add(new SkillResource{owner="아비게일",key="coordinates",label="좌표",amount=1,cap=1});
        c.enemyDeck=new List<string>{"abigail_e"};c.enemyDrawPile=new List<string>{"abigail_e"};c.evasion=65;c.evasionTurns=2;
        typeof(GameEngine).GetMethod("PlanEnemyTurn",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(e,null);
        Check(c.enemyPlan.SequenceEqual(new[]{"abigail_e","abigail_e"}) && c.enemyPlanCosts.SequenceEqual(new[]{2,0}),"a conditional full discount recalls one actual copy for enemy planning");
        e.CombatActions.Clear();e.BeginEndTurn();e.State.rngState=2;e.AdvanceEnemyAction();e.AdvanceEnemyAction();
        Check(c.enemyAvailableEnergy==3 && e.CombatActions.Count(x=>x.enemy && x.kind==null)==1 && SkillMechanics.Discount(c.enemySkills,"abigail_e")==0,"missed conditional recall is cancelled even when energy could pay for the recalled copy");
        e=MechanicsFixture();c=e.State.combat;e.State.deck=new List<string>{"echion_e"};c.hand.Add("echion_e");c.enemyEvasion=100;
        Check(e.PlayCard(0) && SkillMechanics.Resource(c.playerSkills,"에키온","bite")==0 && SkillMechanics.Resource(c.playerSkills,"에키온","vf")==0 && c.playerSkills.discounts.Count==0 && c.hand.Count==0 && c.discardPile.SequenceEqual(new[]{"echion_e"}),"missed Echion E grants no mark, VF, discount or returned copy");
        e=MechanicsFixture();c=e.State.combat;c.hand.Add("celine_w");c.playerSkills.resources.Add(new SkillResource{owner="셀린",key="bomb",label="폭탄",amount=2,cap=4});c.playerSkills.resources.Add(new SkillResource{owner="셀린",key="fusion",label="융합",amount=1,cap=4});c.enemyBlock=5;int hp=c.enemyHp;
        Check(e.CardDamage("celine_w")==0 && e.CardTotalDamage("celine_w")==16 && e.PlayCard(0) && hp-c.enemyHp==11,"conditional bonus damage creates Celine's detonation attack even with zero baseline damage and respects armor");
        e=MechanicsFixture();c=e.State.combat;c.playerSkills.effects.Add(new SkillTimedEffect{owner="fixture",key="counter",kind="counter",sourceCard="basic_guard",amount=8,remaining=2});c.enemyHp=8;c.enemyPlan=new List<string>{"basic_attack"};c.enemyPlanCosts=new List<int>{1};c.enemyPlanFreeCast=new List<bool>{false};
        e.BeginEndTurn();e.AdvanceEnemyAction();Check(e.State.stage==RunStage.Rewards && e.CombatActions.Count==2 && e.CombatActions[0].enemy && e.CombatActions[1].kind=="counter" && e.CombatActions[1].damage==8,"fatal counter retains both causing attack and reaction effects before ending combat");
        e=MechanicsFixture();c=e.State.combat;for(int i=0;i<8;++i){c.playerSkills.effects.Add(new SkillTimedEffect{owner="fixture",key="damage"+i,kind="delayed_damage",sourceCard="basic_attack",amount=24,delay=1,remaining=1});c.playerTraits.skills.effects.Add(new SkillTimedEffect{owner="trait:fixture",key="burn"+i,kind="burn",sourceCard="basic_attack",amount=10,remaining=1});c.playerSkills.effects.Add(new SkillTimedEffect{owner="fixture",key="guard"+i,kind="guard",sourceCard="basic_guard",amount=10,remaining=1});c.playerTraits.skills.effects.Add(new SkillTimedEffect{owner="trait:fixture",key="hot"+i,kind="hot",sourceCard="basic_guard",amount=10,remaining=1});}
        e.State.hp=50;e.BeginEndTurn();Check(e.CombatActions.Sum(x=>x.damage)==40 && e.CombatActions.Sum(x=>x.heal+x.block)==25,"skill and trait end-of-turn effects share one finite actor damage/support budget");
    }

    private static void TargetedFreeCasts()
    {
        var reducer = GameDatabase.Card("basic_guard"); var target = GameDatabase.Card("basic_attack");
        var oldTargets = reducer.freeCastTargets; int oldCount = reducer.freeCastCount; bool oldHit = reducer.freeCastOnHit;
        int oldReducerCost = reducer.cost, oldTargetCost = target.cost, oldTargetHits = target.hits;
        var targetTargets = target.freeCastTargets; int targetCount = target.freeCastCount; bool targetHit = target.freeCastOnHit;
        try
        {
            reducer.cost = 1; reducer.freeCastTargets = new[] { "basic_attack", "nia_q" }; reducer.freeCastCount = 1; reducer.freeCastOnHit = false;
            var e = Ready(); Enter(e, ZoneKind.Wildlife); var c = e.State.combat;
            e.State.passives.Clear();e.State.mainRune=e.State.supportRune=null;c.playerTraits=new TraitActorState();
            e.State.deck = new List<string> { "basic_guard", "basic_attack" }; c.hand = new List<string> { "basic_guard" };
            c.drawPile.Clear(); c.discardPile = new List<string> { "basic_attack" }; c.exhaustPile.Clear(); c.enemyHp = 999; c.enemyEvasion = 0; c.energy = 1;
            int pileCount = PileCount(c);
            Check(e.PlayCard(0) && c.energy == 0 && c.hand.SequenceEqual(new[] { "basic_attack" }), "reducer recalls an owned target from discard after last energy spent");
            Check(e.CardCost("basic_attack") == 1 && e.EffectiveCardCost("basic_attack") == 0 && e.CanPlayCard(0), "target can be used at zero energy while base reward cost stays intact");
            Check(e.EffectiveCardCost("nia_q") == e.CardCost("nia_q") && !c.hand.Contains("nia_q"), "reducer never grants or creates an unowned target");
            Check(e.PlayCard(0) && c.energy == 0 && e.EffectiveCardCost("basic_attack") == 1 && PileCount(c) == pileCount, "target consumes its single grant and card count remains finite");
            c.hand.Add("basic_guard"); c.energy = 1;
            Check(e.PlayCard(0) && e.EffectiveCardCost("basic_attack") == 1, "same reducer source cannot produce an infinite per-turn reset loop");
            e.EndTurn(); Check(c.freeCasts.Count == 0, "unused free cast grants expire at turn end");
            c.hand = new List<string> { "basic_guard" }; c.drawPile = new List<string> { "basic_attack" }; c.discardPile.Clear(); c.exhaustPile.Clear(); c.energy = 1;
            Check(e.PlayCard(0) && c.hand.Contains("basic_attack") && c.drawPile.Count == 0, "missing hand target may be recalled from draw pile");
            e.EndTurn(); c.hand = new List<string> { "basic_guard" }; c.drawPile.Clear(); c.discardPile.Clear(); c.exhaustPile = new List<string> { "basic_attack" }; c.energy = 1;
            Check(e.PlayCard(0) && !c.hand.Contains("basic_attack") && c.exhaustPile.Contains("basic_attack"), "reset does not recover exhausted cards");

            reducer.freeCastTargets = new string[0]; target.freeCastTargets = new[] { "basic_guard" }; target.freeCastCount = 1; target.freeCastOnHit = true;
            e = Ready(); Enter(e, ZoneKind.Wildlife); c = e.State.combat;
            e.State.deck = new List<string> { "basic_guard", "basic_attack" }; c.hand = new List<string> { "basic_attack", "basic_guard" }; c.energy = 1; c.enemyHp = 999; c.enemyEvasion = 100;
            Check(e.PlayCard(0) && !e.CanPlayCard(0), "hit-triggered reset is not granted after an avoided attack");
            c.hand.Add("basic_attack"); c.enemyEvasion = 0; c.energy = 1;
            Check(e.PlayCard(c.hand.Count - 1) && e.CanPlayCard(0), "hit-triggered reset is granted when at least one hit lands");

            target.cost = 5; reducer.cost = 2;
            e = Ready(); Enter(e, ZoneKind.Subject); c = e.State.combat;
            e.State.hp = e.State.maxHp = 999; e.State.gear.Clear(); e.State.passives.Clear(); e.State.mainRune = e.State.supportRune = null;
            c.enemyId="fixture";c.enemyTraits=new TraitActorState();c.enemyLevel = 1; c.enemyDeck = new List<string> { "basic_guard", "basic_attack" }; c.enemyDrawPile = new List<string> { "basic_guard", "basic_attack" }; c.enemyDiscardPile.Clear(); c.enemyExhaustPile.Clear();
            c.block = c.evasion = 0; c.enemyHp = 999;
            typeof(GameEngine).GetMethod("PlanEnemyTurn", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(e, null);
            Check(c.enemyPlan.SequenceEqual(new[] { "basic_attack", "basic_guard" }) && c.enemyPlanCosts.SequenceEqual(new[] { 5, 0 }), "enemy planner uses the same targeted zero-cost combo");
            e.CombatActions.Clear(); e.BeginEndTurn(); e.AdvanceEnemyAction(); e.AdvanceEnemyAction();
            Check(c.enemyAvailableEnergy == 0 && c.enemyBlock == reducer.block, "enemy executes its owned target at zero remaining energy");

            e = Ready(); Enter(e, ZoneKind.Subject); c = e.State.combat;
            e.State.hp = e.State.maxHp = 999; e.State.gear.Clear(); e.State.passives.Clear(); e.State.mainRune = e.State.supportRune = null;
            c.enemyId="fixture";c.enemyTraits=new TraitActorState();c.enemyLevel = 1; c.enemyDeck = new List<string> { "basic_guard", "basic_attack" }; c.enemyDrawPile = new List<string> { "basic_guard", "basic_attack" }; c.enemyDiscardPile.Clear(); c.enemyExhaustPile.Clear(); c.evasion = 65; c.evasionTurns = 2;
            typeof(GameEngine).GetMethod("PlanEnemyTurn", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(e, null);
            e.CombatActions.Clear(); e.BeginEndTurn(); e.State.rngState = 2; e.AdvanceEnemyAction(); e.AdvanceEnemyAction();
            Check(c.enemyAvailableEnergy == 0 && c.enemyBlock == 0, "enemy cancels an unaffordable target if hit-triggered reduction misses");

            target.cost = 1; target.hits = 1; target.freeCastTargets = new[] { "basic_attack" };
            e = Ready(); Enter(e, ZoneKind.Subject); c = e.State.combat;
            e.State.hp = e.State.maxHp = 999; e.State.gear.Clear(); e.State.passives.Clear(); e.State.mainRune = e.State.supportRune = null;
            c.enemyId="fixture";c.enemyTraits=new TraitActorState();c.enemyLevel = 1; c.enemyDeck = new List<string> { "basic_attack" }; c.enemyDrawPile = new List<string> { "basic_attack" }; c.enemyDiscardPile.Clear(); c.enemyExhaustPile.Clear(); c.evasion = 65; c.evasionTurns = 2;
            typeof(GameEngine).GetMethod("PlanEnemyTurn", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(e, null);
            Check(c.enemyPlan.SequenceEqual(new[] { "basic_attack", "basic_attack" }) && c.enemyPlanCosts.SequenceEqual(new[] { 1, 0 }), "one enemy card copy may be planned twice only through a conditional reset");
            int beforeMiss = e.State.hp;
            e.CombatActions.Clear(); e.BeginEndTurn(); e.State.rngState = 2; e.AdvanceEnemyAction(); e.AdvanceEnemyAction();
            Check(c.enemyAvailableEnergy == 4 && e.State.hp == beforeMiss && e.CombatActions.Count(x => x.enemy) == 1, "missed self-reset cannot recast its predicted recalled copy even with enough remaining energy to pay");

            target.hits = 2;
            e = Ready(); Enter(e, ZoneKind.Subject); c = e.State.combat;
            e.State.hp = e.State.maxHp = 999; e.State.gear.Clear(); e.State.passives.Clear(); e.State.mainRune = e.State.supportRune = null;
            c.enemyId="fixture";c.enemyTraits=new TraitActorState();c.enemyLevel = 1; c.enemyDeck = new List<string> { "basic_attack" }; c.enemyDrawPile = new List<string> { "basic_attack" }; c.enemyDiscardPile.Clear(); c.enemyExhaustPile.Clear(); c.evasion = 65; c.evasionTurns = 2;
            typeof(GameEngine).GetMethod("PlanEnemyTurn", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(e, null);
            e.CombatActions.Clear(); e.BeginEndTurn(); e.State.rngState = 2; e.AdvanceEnemyAction();
            var partial = e.CombatActions.Last(x => x.enemy);
            Check(partial.avoided == 1 && partial.damage > 0, "one landed hit among two grants a hit-triggered reset despite a partial miss");
            e.AdvanceEnemyAction();
            Check(c.enemyAvailableEnergy == 4 && e.CombatActions.Count(x => x.enemy) == 2, "a legitimately granted self-reset still executes at zero cost after a partial miss");
        }
        finally
        {
            reducer.freeCastTargets = oldTargets; reducer.freeCastCount = oldCount; reducer.freeCastOnHit = oldHit; reducer.cost = oldReducerCost;
            target.freeCastTargets = targetTargets; target.freeCastCount = targetCount; target.freeCastOnHit = targetHit; target.cost = oldTargetCost; target.hits = oldTargetHits;
        }
    }

    private static void LastOwnSkillReplay()
    {
        var e = Ready(); Enter(e, ZoneKind.Wildlife); var c = e.State.combat;
        e.State.deck = new List<string> { "sua_q", "sua_w", "sua_e", "sua_r", "aya_q" };
        c.hand = new List<string> { "sua_q", "aya_q", "sua_r" }; c.drawPile = new List<string> { "sua_w", "sua_e" }; c.discardPile.Clear(); c.exhaustPile.Clear(); c.enemyHp = 999; c.enemyEvasion = 0; c.energy = 99;
        Check(e.PlayCard(0) && e.PlayCard(0), "last skill records each owner independently");
        c.energy = e.CardCost("sua_r");
        Check(e.PlayCard(0) && c.energy == 0 && c.hand.SequenceEqual(new[] { "sua_q" }), "Sua ultimate recalls her most recent own QWE despite an intervening foreign skill");
        Check(e.EffectiveCardCost("sua_q") == 0 && e.EffectiveCardCost("sua_w") == e.CardCost("sua_w") && e.EffectiveCardCost("sua_e") == e.CardCost("sua_e"), "last skill replay grants exactly one own target rather than all three");
        Check(e.PlayCard(0) && c.exhaustPile.Contains("sua_r") && PileCount(c) == e.State.deck.Count, "last skill replay consumes a real card copy and ultimate remains exhausted");
        var loaded = new GameEngine((RunState)Clone(e.State));
        Check(loaded.State.combat.lastSkills.Any(x => x.owner == "수아" && x.cardId == "sua_q"), "last own skill history survives saved state restoration");
    }

    private static void PacedEnemyActions()
    {
        var e = Ready(); Enter(e, ZoneKind.Wildlife); var c = e.State.combat;
        e.State.hp = e.State.maxHp = 999; c.enemyHp = 999; c.enemyPlan = new List<string> { "basic_attack", "basic_attack" }; c.enemyPlanCosts = new List<int> { 1, 1 };
        e.State.gear.Clear(); e.State.passives.Clear(); e.State.mainRune = e.State.supportRune = null; c.block = c.evasion = 0;
        int hp = e.State.hp, turn = c.turn;
        Check(e.BeginEndTurn() && c.enemyTurn && e.State.hp == hp && !e.PlayCard(0), "starting enemy phase delays attacks and locks player action");
        Check(e.AdvanceEnemyAction() && c.enemyActionIndex == 1 && e.State.hp < hp && e.CombatActions.Count(x => x.enemy) == 1, "one advance emits one enemy card and one attack");
        var afterFirstLoad = new GameEngine((RunState)Clone(e.State));
        int afterFirst = e.State.hp;
        Check(e.AdvanceEnemyAction() && c.enemyActionIndex == 2 && e.State.hp < afterFirst && c.turn == turn, "second enemy card has a separate paced action");
        Check(afterFirstLoad.AdvanceEnemyAction() && afterFirstLoad.State.hp == e.State.hp && afterFirstLoad.State.rngState == e.State.rngState, "saving after first enemy card resumes at the second with matching damage and RNG");
        var loaded = new GameEngine((RunState)Clone(e.State));
        Check(loaded.State.combat.enemyTurn && loaded.AdvanceEnemyAction() && !loaded.State.combat.enemyTurn && loaded.State.combat.turn == turn + 1, "save resumes a paced turn without replaying prior cards");
        Check(e.AdvanceEnemyAction() && !c.enemyTurn && c.turn == turn + 1 && e.CombatActions.Count(x => x.enemy) == 2, "next player turn starts after all enemy actions finish");
        e = Ready(); Enter(e, ZoneKind.Wildlife); c = e.State.combat; e.State.hp = 1;
        e.State.gear.Clear(); e.State.passives.Clear(); e.State.mainRune = e.State.supportRune = null; c.block = c.evasion = 0;
        c.enemyPlan = new List<string> { "basic_attack", "basic_attack" }; c.enemyPlanCosts = new List<int> { 1, 1 };
        e.CombatActions.Clear(); e.BeginEndTurn(); e.AdvanceEnemyAction();
        Check(e.State.stage == RunStage.Lost && e.CombatActions.Count == 1 && e.CombatActions[0].enemy && e.CombatActions[0].damage == 1 && !e.AdvanceEnemyAction(), "lethal enemy card preserves its effect event and cancels following actions");
    }

    private static void SaveMigration()
    {
        var e = Ready(); Enter(e, ZoneKind.Subject); Win(e);
        string id = "nia_r"; e.State.version = 1;
        e.State.rewards.choices = new List<string> { "nia_q", "hyejin_q", "nia_r" }; e.State.rewards.cardBudget = 0; e.State.rewards.taken = new List<string> { id };
        int beforeCopies = e.State.deck.Count(x => x == id); e.State.deck.Add(id);
        e.State.map.RemoveAll(n => n.row >= 7); int copies = beforeCopies + 1;
        var loaded = new GameEngine((RunState)Clone(e.State));
        Check(loaded.State.version == 2 && loaded.State.mapRows == 7, "legacy seven-row maps keep their original route on load");
        Check(loaded.State.deck.Count(x => x == id) == beforeCopies && loaded.State.rewards.taken.Contains(id), "legacy migration removes one appended reward and preserves all initial copies");
        Check(loaded.State.rewards.cardBudget == 3 && loaded.RewardCardPrice("nia_q") == 1 && loaded.RewardCardPrice("hyejin_q") == 2 && loaded.RewardCardPrice(id) == 3 && loaded.CardCost(id) == 6, "legacy remaining budget recovers exact original cost ceiling and per-choice prices");
        var reloadedPending = new GameEngine((RunState)Clone(loaded.State));
        Check(reloadedPending.State.deck.Count(x => x == id) == beforeCopies && reloadedPending.RewardRemainingBudget == 0, "reloading migrated pending reward does not remove an initial copy again");
        Check(loaded.ClaimCard(id) && loaded.RewardRemainingBudget == 3, "legacy selected reward can be cancelled after migration");
        Check(loaded.ClaimCard("nia_q") && loaded.ClaimCard("hyejin_q") && loaded.RewardRemainingBudget == 0, "legacy cancellation enables replacements with original one and two cost prices");
        loaded.ClaimCard("nia_q"); loaded.ClaimCard("hyejin_q"); loaded.ClaimCard(id); loaded.FinishRewards();
        Check(loaded.State.deck.Count(x => x == id) == copies, "legacy selection confirmation does not duplicate the previous reward");
        var again = new GameEngine((RunState)Clone(loaded.State));
        Check(again.State.deck.Count(x => x == id) == copies, "version two load does not migrate rewards twice");
        e = Ready(); Enter(e, ZoneKind.Subject); var c = e.State.combat;
        c.enemyDeck = new List<string> { "basic_attack", "basic_guard" }; c.enemyPlan = new List<string> { "basic_attack" }; c.enemyHand = new List<string> { "basic_guard" };
        c.enemyDrawPile.Clear(); c.enemyDiscardPile.Clear(); c.enemyExhaustPile.Clear(); e.State.version = 1;
        loaded = new GameEngine((RunState)Clone(e.State)); c = loaded.State.combat;
        Check(c.enemyDiscardPile.SequenceEqual(new[] { "basic_attack", "basic_guard" }) && c.enemyHand.Count == 0, "legacy enemy plans return their real copies to the new planning piles");
    }

    private static void OriginalStatusCoverage()
    {
        Check(GameDatabase.Cards.Count(x=>x.statuses.Length>0)>=200,"source statuses cover the reviewed skill and weapon catalog");
        Check(GameDatabase.Cards.All(x=>x.poison==0),"no generic poison remains on skills without original poison; source bleed and burn are separate authored effects");
        Check(GameDatabase.Passives.All(x=>x.mechanics.rules.All(r=>r.op!="weak" && r.op!="vulnerable" && r.op!="poison")),"passive crowd control also retains original fear, freeze and defense reduction instead of generic debuffs");
        foreach(var card in GameDatabase.Cards.Where(x=>x.category!="basic"))
        {
            Check(card.weak==0 && card.vulnerable==0,"unrelated generic weakness/vulnerability removed: "+card.id);
            foreach(var status in card.statuses)
            {
                Check(status.duration==1 && StatusMechanics.Name(status.key)!=status.key,"localized, bounded source status: "+card.id+"/"+status.key);
                Check(card.description.Contains(StatusMechanics.Name(status.key)),"card detail names actual status: "+card.id+"/"+status.key);
                if(status.timing!="cast" && status.timing!="next_basic")Check(card.mechanics!=null && card.mechanics.rules.Any(x=>x.key==status.timing && new[]{"delayed_damage","counter","summon","bleed","burn"}.Contains(x.op)),"delayed status has a real source effect: "+card.id+"/"+status.timing);
            }
        }
        Check(GameDatabase.Card("sua_w").statuses.Any(x=>x.key=="blind"),"Sua's bird preserves original blindness");
        Check(GameDatabase.Card("daniel_r").statuses.Any(x=>x.key=="silence"),"Daniel's shadow preserves original silence");
        Check(GameDatabase.Card("weapon_glove").statuses.Length==0 && GameDatabase.Card("weapon_hammer").statuses.Any(x=>x.key=="armor_break"),"weapon uppercut is plain damage, hammer applies actual defense reduction");
    }
    private static void StatusUseRestrictions()
    {
        var e=MechanicsFixture();var c=e.State.combat;
        c.hand=new List<string>{"nia_q","basic_attack","basic_guard","tactical_blink","hyunwoo_e"};
        StatusMechanics.Add(c.playerStatuses,"silence","daniel_r");
        Check(!e.CanPlayCard(0) && e.CanPlayCard(1) && e.CanPlayCard(2) && e.CanPlayCard(3),"silence locks QWER while basic and tactical responses remain available");
        int energy=c.energy,rng=e.State.rngState;Check(!e.PlayCard(0) && c.energy==energy && e.State.rngState==rng && c.hand.Count==5,"locked card rejection is atomic");
        c.playerStatuses.effects.Clear();StatusMechanics.Add(c.playerStatuses,"root","coraline_e");
        Check(!e.CanPlayCard(3) && !e.CanPlayCard(4) && e.CanPlayCard(0),"root locks actual displacement including blink, without locking stationary skills");
        c.playerStatuses.effects.Clear();StatusMechanics.Add(c.playerStatuses,"slow","hyunwoo_q");
        Check(e.EffectiveCardCost("hyunwoo_e")==e.CardCost("hyunwoo_e")+1 && e.EffectiveCardCost("nia_q")==e.CardCost("nia_q"),"slow raises only movement card cost");
        c.playerStatuses.effects.Clear();StatusMechanics.Add(c.playerStatuses,"polymorph","emma_e");
        Check(!e.CanPlayCard(0) && !e.CanPlayCard(1) && e.CanPlayCard(2) && e.CanPlayCard(3),"rabbit transformation leaves guard and tactical responses available");
        c.playerStatuses.effects.Clear();StatusMechanics.Add(c.playerStatuses,"disarm","basic_guard");
        Check(!e.CanPlayCard(1) && e.CanPlayCard(0),"disarm locks only basic attacks");
        c.playerStatuses.effects.Clear();StatusMechanics.Add(c.playerStatuses,"taunt","mai_e");
        Check(!e.CanPlayCard(0) && e.CanPlayCard(1) && e.CanPlayCard(2),"taunt directs responses away from character skill cards");
    }
    private static void StatusTimingAndForecast()
    {
        var e=MechanicsFixture();var c=e.State.combat;c.enemyLevel=20;e.State.hp=e.State.maxHp=1000;
        c.enemyPlan=Enumerable.Repeat("basic_attack",8).ToList();c.enemyPlanCosts=Enumerable.Repeat(1,8).ToList();c.enemyPlanFreeCast=Enumerable.Repeat(false,8).ToList();
        c.hand.Add("nia_r");Check(e.PlayCard(0) && StatusMechanics.Has(c.enemyStatuses,"stun"),"Nia game-world edge applies source stun");
        var preview=e.EnemyIntent;int expected=(GameDatabase.Card("basic_attack").damage+10)*7;
        Check(c.intentDamage==expected,"stun forecast respects reduced available energy");
        Check(e.EndTurn() && e.CombatActions.Count(x=>x.enemy && x.kind==null)==7 && !StatusMechanics.Has(c.enemyStatuses,"stun"),"enemy loses one action energy and stun expires after its turn");
        e=MechanicsFixture();c=e.State.combat;c.hand.Add("isol_q");e.PlayCard(0);
        Check(!StatusMechanics.Has(c.enemyStatuses,"root"),"Isol's bomb does not root before detonation");
        typeof(GameEngine).GetMethod("ResolveSkillTicks",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(e,new object[]{false});
        Check(!StatusMechanics.Has(c.enemyStatuses,"root"),"long bomb fuse keeps control pending after its first timer");
        typeof(GameEngine).GetMethod("ResolveSkillTicks",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(e,new object[]{false});
        Check(StatusMechanics.Has(c.enemyStatuses,"root") && e.CombatActions.Any(x=>x.kind=="status_root" && x.cardId=="isol_q"),"actual bomb explosion roots and emits its status effect");
        e=MechanicsFixture();c=e.State.combat;c.hand.Add("jackie_w");e.PlayCard(0);
        Check(!StatusMechanics.Has(c.enemyStatuses,"slow"),"Jackie prepares slow on the next basic, without applying it on setup");
        c.hand.Add("basic_attack");e.PlayCard(0);Check(StatusMechanics.Has(c.enemyStatuses,"slow") && !c.enemyStatuses.effects.Any(x=>x.pending),"prepared crippling basic applies slow once");
        e=MechanicsFixture();c=e.State.combat;c.hand.Add("jackie_w");e.PlayCard(0);c.enemyEvasion=100;c.hand.Add("basic_attack");e.PlayCard(0);
        Check(!c.enemyStatuses.effects.Any(x=>x.pending || x.kind=="slow") && !c.playerSkills.effects.Any(x=>x.kind=="empower_basic"),"missed first basic consumes prepared control together with its basic damage enhancement");
        c.enemyEvasion=0;c.hand.Add("basic_attack");e.PlayCard(0);Check(!StatusMechanics.Has(c.enemyStatuses,"slow"),"a following basic cannot reuse control consumed by the missed attack");
        e=MechanicsFixture();c=e.State.combat;c.hand.Add("emma_r");c.playerSkills.resources.Add(new SkillResource{owner="엠마",key="dove",amount=1,cap=1});c.playerSkills.history.Add(new SkillOwnerHistory{owner="엠마",cardId="emma_q"});e.PlayCard(0);
        Check(StatusMechanics.Has(c.enemyStatuses,"root") && !StatusMechanics.Has(c.enemyStatuses,"polymorph") && !StatusMechanics.Has(c.enemyStatuses,"pull"),"Emma ultimate controls only its actual previous-skill branch");
        e=MechanicsFixture();c=e.State.combat;c.hand.Add("vanya_r");e.PlayCard(0);c.hand.Add("vanya_e");e.PlayCard(0);
        Check(!c.enemyStatuses.effects.Any(x=>x.pending && x.timing=="sleep_fuse"),"waking Vanya's prepared target cancels the installation's saved pending sleep");
        e=MechanicsFixture(null,"alonso");c=e.State.combat;e.State.hp=e.State.maxHp=1000;c.enemyLevel=20;
        c.enemyTraits.skills.resources.Add(new SkillResource{owner="trait:alonso_p",key="barrier_charge",amount=2,cap=2});
        StatusMechanics.Add(c.enemyStatuses,"stun","nia_r");StatusMechanics.Add(c.enemyStatuses,"silence","daniel_r");StatusMechanics.Add(c.enemyStatuses,"fear","aya_r");
        c.enemyPlan=new List<string>{"nia_q","basic_attack"};c.enemyPlanCosts=new List<int>{1,1};c.enemyPlanFreeCast=new List<bool>{false,false};
        var forecast=e.EnemyIntent;int raw=GameDatabase.Card("nia_q").damage+10+GameDatabase.Card("basic_attack").damage+10;
        Check(c.intentDamage==raw && e.BeginEndTurn() && c.enemyAvailableEnergy==8 && e.AdvanceEnemyAction(),"opening barrier cleanse releases predicted skill restrictions and hard-control energy before enemy actions");
        while(c.enemyTurn)e.AdvanceEnemyAction();Check(1000-e.State.hp==raw,"opening cleanse forecast and actual damage agree without fear remaining");
        e=MechanicsFixture();c=e.State.combat;e.State.mainRune="guardian";e.State.hp=e.State.maxHp=1000;c.enemyLevel=20;c.block=1;
        c.enemyPlan=new List<string>{"weapon_hammer","basic_attack","basic_attack"};c.enemyPlanCosts=c.enemyPlan.Select(e.CardCost).ToList();c.enemyPlanFreeCast=new List<bool>{false,false,false};
        forecast=e.EnemyIntent;raw=GameDatabase.Card("weapon_hammer").damage+10+(GameDatabase.Card("basic_attack").damage+10)*2;
        Check(c.intentDamage==raw && e.EndTurn() && 1000-e.State.hp==raw-1,"shield-breaking guardian cleanse removes fresh armor reduction before both forecast and actual following attacks");
    }
    private static void StatusPersistenceAndRates()
    {
        var e=MechanicsFixture();var c=e.State.combat;StatusMechanics.Add(c.playerStatuses,"silence","daniel_r");StatusMechanics.Add(c.enemyStatuses,"blind","sua_w");
        StatusMechanics.Add(c.enemyStatuses,"root","isol_q",1,"semtex");
        var loaded=new GameEngine((RunState)Clone(e.State));
        Check(StatusMechanics.Has(loaded.State.combat.playerStatuses,"silence") && loaded.State.combat.enemyStatuses.effects.Any(x=>x.pending),"active and pending source statuses survive saves");
        int rng=e.State.rngState;string snapshot=string.Join("|",e.SkillStateSnapshot(true).Select(x=>x.kind));for(int i=0;i<5;++i){var ignored=e.EnemyIntent;e.SkillStateSnapshot(true);}
        Check(e.State.rngState==rng && snapshot==string.Join("|",e.SkillStateSnapshot(true).Select(x=>x.kind)),"status previews are read-only and deterministic");
        var copy=StatusMechanics.Clone(c.enemyStatuses);copy.effects.Clear();Check(c.enemyStatuses.effects.Count==2,"status preview clones do not share effect instances");
        Check(StatusMechanics.AccuracyPenalty(c.enemyStatuses)==25 && e.Evasion==25,"blind adds a single combined accuracy penalty");
        StatusMechanics.Add(c.playerStatuses,"fear","aya_r");int raw=GameDatabase.Card("basic_attack").damage;Check(e.CardDamage("basic_attack")==raw*4/5,"fear reduces attack damage moderately");
        StatusMechanics.Add(c.playerStatuses,"attack_down","darko_w");Check(e.CardDamage("basic_attack")==raw*4/5,"fear and attack reduction do not stack");
        c.playerStatuses.effects.RemoveAll(x=>x.kind=="silence");StatusMechanics.Add(c.playerStatuses,"heal_reduction","rozzi_w");e.State.hp=50;c.hand.Add("charlotte_w");e.PlayCard(0);Check(e.State.hp==56,"original healing reduction affects source recovery by twenty percent");
        var actor=new StatusActorState();StatusMechanics.Add(actor,"stun","nia_r");StatusMechanics.Add(actor,"freeze","elena_r");StatusMechanics.Add(actor,"suppression","darko_r");Check(StatusMechanics.EnergyPenalty(actor)==2,"multiple hard controls use only their strongest energy penalty");
        e=MechanicsFixture();c=e.State.combat;c.enemyPlan=new List<string>{"basic_attack"};c.enemyPlanCosts=new List<int>{1};c.enemyPlanFreeCast=new List<bool>{false};StatusMechanics.Add(c.playerStatuses,"armor_break","weapon_hammer");
        var intent=e.EnemyIntent;Check(c.intentDamage==GameDatabase.Card("basic_attack").damage,"one-turn player armor reduction expires before the forecast enemy turn");
    }
    private static void HealingDroneHealthTrigger()
    {
        var e=MechanicsFixture();var c=e.State.combat;e.State.mainRune="healing_drone";e.State.hp=30;e.State.maxHp=90;c.hand.Add("nia_r");e.PlayCard(0);
        Check(!c.playerTraits.skills.effects.Any(x=>x.key=="healing_drone"),"healing drone does not trigger from R even at low health");
        var trigger=typeof(GameEngine).GetMethod("TraitEvent",BindingFlags.NonPublic|BindingFlags.Instance);
        e.State.hp=31;trigger.Invoke(e,new object[]{false,"low_health",GameDatabase.Card("basic_attack"),true,1,0,0,0});
        Check(!c.playerTraits.skills.effects.Any(x=>x.key=="healing_drone"),"health just above one third does not trigger healing drone");
        e.State.hp=30;trigger.Invoke(e,new object[]{false,"low_health",GameDatabase.Card("basic_attack"),true,1,0,0,0});
        var drone=c.playerTraits.skills.effects.Single(x=>x.key=="healing_drone");Check(drone.amount==2 && drone.remaining==2,"exactly one third health activates two-turn modest healing");
        trigger.Invoke(e,new object[]{false,"low_health",GameDatabase.Card("basic_attack"),true,1,0,0,0});Check(c.playerTraits.skills.effects.Count(x=>x.key=="healing_drone")==1 && drone.remaining==2,"same-turn repeated damage cannot stack or extend healing drone");
        Check(GameDatabase.Rune("healing_drone").description.Contains("1/3") && !GameDatabase.Rune("healing_drone").description.Contains("R 카드"),"healing drone detail describes its exact health trigger");
        e=MechanicsFixture();c=e.State.combat;e.State.mainRune="healing_drone";e.State.hp=36;e.State.maxHp=90;c.enemyPlan=new List<string>{"basic_attack"};c.enemyPlanCosts=new List<int>{1};c.enemyPlanFreeCast=new List<bool>{false};
        Check(e.BeginEndTurn() && e.AdvanceEnemyAction() && c.playerTraits.skills.effects.Any(x=>x.key=="healing_drone"),"real incoming health damage fires healing drone at the exact low-health threshold");
    }
    private static void RevisedEconomyAndEncounters()
    {
        foreach(var kind in new[]{ZoneKind.Wildlife,ZoneKind.Subject,ZoneKind.Boss})
        {
            var e=Ready(331);e.State.act=3;e.State.row=10;Enter(e,kind);var c=e.State.combat;
            int old=kind==ZoneKind.Boss?80+c.enemyLevel*8:kind==ZoneKind.Subject?35+c.enemyLevel*6:(c.animal=="bear"?62:c.animal=="wolf"?44:c.animal=="boar"?38:c.animal=="dog"?28:20)+c.enemyLevel*3;
            Check(c.enemyMaxHp>old && (kind!=ZoneKind.Boss || c.enemyMaxHp>old*2),"late-game health increases, with stronger boss scaling: "+kind);
            Win(e);var reward=e.State.rewards;int oldMinimum=(kind==ZoneKind.Boss?220:kind==ZoneKind.Subject?100:60)+c.enemyLevel*6;
            Check(reward.credits<oldMinimum && reward.credits>40,"credits decrease without removing material purchasing progression: "+kind);
        }
        Check(GameDatabase.Object("meteorite").price==200 && GameDatabase.Object("tree").price==200 && GameDatabase.Object("mithril").price==250 && GameDatabase.Object("force").price==350 && GameDatabase.Object("blood").price==500,"original object prices remain fixed");
        foreach(var effect in new[]{"trade_card","trade_object","risky_card","risky_object","food"})
        {
            var def=GameDatabase.Events.First(x=>x.options.Any(o=>o.effect==effect));int index=Array.FindIndex(def.options,o=>o.effect==effect);var o=def.options[index];
            var e=Ready();Enter(e,ZoneKind.Encounter);e.State.encounterOffers=new List<string>{def.id};e.SelectEncounter(def.id);e.State.credits=1000;e.State.hp=80;
            int credit=e.State.credits,hp=e.State.hp,deck=e.State.deck.Count,objects=e.State.objects.Count,foods=e.State.foods.Count;
            if(effect.StartsWith("trade")){e.State.credits=o.amount-1;Check(!e.CanChooseEventOption(index) && !e.ChooseEventOption(index) && e.State.stage==RunStage.Encounter,"unaffordable encounter remains open: "+effect);e.State.credits=credit;}
            if(effect.StartsWith("risky")){e.State.hp=o.amount;Check(!e.CanChooseEventOption(index) && !e.ChooseEventOption(index) && e.State.stage==RunStage.Encounter,"encounter risk cannot silently kill the player: "+effect);e.State.hp=hp;}
            Check(e.ChooseEventOption(index),"authored transaction resolves: "+effect);
            Check(e.State.credits==credit-(effect.StartsWith("trade")?o.amount:0) && e.State.hp==hp-(effect.StartsWith("risky")?o.amount:0),"encounter charges its exact stated cost: "+effect);
            Check(e.State.deck.Count==deck+(effect.EndsWith("card")?1:0) && e.State.objects.Count==objects+(effect.EndsWith("object")?1:0) && e.State.foods.Count==foods+(effect=="food"?1:0),"encounter grants only its stated reward: "+effect);
        }
    }

    private static object Clone(object source)
    {
        if (source == null) return null; var type = source.GetType();
        if (type.IsPrimitive || type.IsEnum || type == typeof(string)) return source;
        if (source is IList)
        {
            var list = (IList)Activator.CreateInstance(type);
            foreach (var entry in (IList)source) list.Add(Clone(entry)); return list;
        }
        var copy = Activator.CreateInstance(type);
        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance)) field.SetValue(copy, Clone(field.GetValue(source)));
        return copy;
    }

    private static void LevelCap()
    {
        var e = Ready(); e.State.level = 19; e.State.xp = 9999;
        Enter(e, ZoneKind.Subject); Win(e);
        Check(e.State.level == 20 && e.State.xp == 0 && e.MaxEnergy == 8, "player caps at level twenty and eight cost");
        Check(GameEngine.EnergyForLevel(1) == 5 && GameEngine.EnergyForLevel(6) == 6 && GameEngine.EnergyForLevel(12) == 7 && GameEngine.EnergyForLevel(18) == 8, "level energy thresholds");
    }

    private static void LevelTwentyOnCombatRoute()
    {
        var e = Ready(2171); int battles = 0;
        while (e.State.stage != RunStage.Won)
        {
            var node = e.AvailableNodes().OrderBy(n => n.kind == ZoneKind.Boss ? 9 : n.kind == ZoneKind.Subject ? 8 : n.kind == ZoneKind.Wildlife ? 7 : n.kind == ZoneKind.Campfire ? 6 : n.kind == ZoneKind.Kiosk ? 5 : 4).Last();
            Check(e.EnterNode(node.lane), "real generated route remains reachable");
            if (e.State.stage == RunStage.Combat) { battles++; Win(e); e.FinishRewards(); }
            else if (e.State.stage == RunStage.Campfire) e.ChooseCamp(false);
            else if (e.State.stage == RunStage.Kiosk) e.LeaveKiosk();
            else if (e.State.stage == RunStage.Encounter) { e.SelectEncounter(e.State.encounterOffers[0]); e.ChooseEventOption(0); if (e.State.runeChangePending) e.FinishRuneChange(); }
            if (e.State.stage == RunStage.PassiveChoice) e.ReplacePassive(-1);
        }
        Check(e.State.level == 20 && battles >= 12, "max level twenty is reachable through earned combat XP");
        Console.WriteLine("Combat-heavy generated route: " + battles + " victories, final level " + e.State.level + ".");
    }

    private static void CompleteEscapeAndDeath()
    {
        var e = Ready(); e.State.passives.Clear();
        for (int act = 1; act <= 3; ++act)
        {
            for (int row = 0; row < GameEngine.RowsPerAct - 1; ++row) { Enter(e, ZoneKind.Kiosk); e.LeaveKiosk(); }
            Enter(e, ZoneKind.Boss); Check(e.State.combat.enemyLevel <= 20, "boss level cap"); Win(e); e.FinishRewards();
            if (e.State.stage == RunStage.PassiveChoice) e.ReplacePassive(-1);
            if (act < 3) Check(e.State.act == act + 1 && e.State.row == -1 && e.State.stage == RunStage.Map, "boss advances to next act");
        }
        Check(e.State.stage == RunStage.Won && !e.EnterNode(1), "third boss produces terminal escape");
        e = Ready(); Enter(e, ZoneKind.Wildlife); e.State.hp = 1;
        e.State.combat.block = 0; e.State.combat.evasion = 0; e.State.gear.Clear(); e.State.passives.Clear(); e.State.mainRune = e.State.supportRune = null;
        Check(e.EndTurn() && e.State.stage == RunStage.Lost && e.State.hp == 0, "lethal damage produces terminal defeat");
        Check(!e.EndTurn() && !e.FinishRewards(), "defeat cannot continue or gain rewards");
    }
}
#endif

