#if LUMIA_TEST_RUNNER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lumia;

public static partial class CoreSmokeTests
{
    static void SuaTargetAndBlindMechanics()
    {
        SuaDefinitionsAndChoices();
        SuaProtectionStatusRules();
        SuaProtectionCombatAndExpiry();
        SuaEnemyAndSaveParity();
        BlindBasicUseAndExpiry();
    }

    static GameEngine SuaFixture(params string[] hand)
    {
        var e=AdvancedFixture(hand);var c=e.State.combat;
        e.State.hp=e.State.maxHp=c.enemyHp=c.enemyMaxHp=1000;
        c.playerStatuses=new StatusActorState();c.enemyStatuses=new StatusActorState();
        c.energy=99;c.turn=1;
        return e;
    }
    static void SuaCastSelf(GameEngine e,int index=0)
    {
        Check(e.BeginCardChoice(index) && e.ResolveCardChoice("self"),"Sua W confirms its self-targeted branch");
    }
    static void SuaDefinitionsAndChoices()
    {
        var card=GameDatabase.Card("sua_w");string original=FieldSnapshot(card);
        Check(card.cost==3 && card.damage==8 && card.block==10,"Sua W retains three cost with authored eight-damage and ten-shield branches");
        Check(AdvancedCardMechanics.HasTargetChoice(card) && AdvancedCardMechanics.Kind(card)=="target","Sua W uses the shared explicit target selection pipeline");
        foreach(string branch in new[]{"", "power", RareCardUpgrades.BranchId})
        {
            var source=branch.Length==0?card:StrategyMechanics.Apply(card,branch);
            CardDef self=AdvancedCardMechanics.PreparedCard(source,"self"),enemy=AdvancedCardMechanics.PreparedCard(source,"enemy"),display=AdvancedCardMechanics.PreparedCard(source);
            Check(self.block==source.block && self.damage==0 && self.statuses.Length==0 && AdvancedCardMechanics.GrantsControlProtection(self),"self branch has only the upgraded shield and control protection: "+branch);
            Check(enemy.block==0 && enemy.damage==source.damage && enemy.statuses.Any(x=>x.key=="blind" && x.duration==1) && !AdvancedCardMechanics.GrantsControlProtection(enemy),"hostile branch has only upgraded damage and one-turn blind: "+branch);
            foreach(var prepared in new[]{display,self,enemy})
            {
                Check(AdvancedCardMechanics.PreparedCard(prepared,"self").block==source.block && AdvancedCardMechanics.PreparedCard(prepared,"enemy").damage==source.damage,"re-preparing either target branch preserves original upgraded numeric values: "+branch);
                Check(AdvancedCardMechanics.Description(prepared).Contains("방어도 "+source.block) && AdvancedCardMechanics.TargetChoiceDescription(prepared,"enemy").Contains("피해 "+source.damage),"display and detail metadata retain both branch amounts: "+branch);
            }
        }
        var sissela=StrategyMechanics.Apply(GameDatabase.Card("sissela_e"),"power");
        var sisselaSelf=AdvancedCardMechanics.PreparedCard(sissela,"self");
        Check(AdvancedCardMechanics.PreparedCard(sisselaSelf,"self").block==12+sissela.block,"re-preparing Sissela's self choice never adds her twelve-shield base twice");
        Check(FieldSnapshot(card)==original,"all target preparation and descriptions leave the shared Sua definition unchanged");

        var e=SuaFixture("sua_w","basic_attack");var c=e.State.combat;int energy=c.energy,rng=e.State.rngState,hp=c.enemyHp;
        Check(e.BeginCardChoice(0) && e.PendingCardChoice.kind=="target","Sua W opens an unresolved target selection");
        Check(!e.ResolveCardChoice("both") && e.PendingCardChoice!=null && c.energy==energy && c.enemyHp==hp && c.block==0 && !StatusMechanics.HasControlProtection(c.playerStatuses),"an invalid target spends nothing and applies neither branch");
        Check(e.CancelCardChoice() && c.energy==energy && e.State.rngState==rng && c.enemyHp==hp && e.CombatActions.Count==0,"canceling target selection preserves resources, RNG and action hooks");
        SuaCastSelf(e);
        Check(c.energy==energy-3 && c.block==10 && c.playerStatuses.controlProtection==10 && c.enemyHp==hp && c.enemyStatuses.effects.Count==0,"confirmed self cast pays three and shields without hostile effects");
        Check(!c.playerSkills.effects.Any(x=>x.key=="bluebird"),"the removed periodic bluebird shield cannot extend the new protected ward");
        e=SuaFixture("sua_w");c=e.State.combat;hp=c.enemyHp;energy=c.energy;
        Check(e.BeginCardChoice(0) && e.ResolveCardChoice("enemy") && c.energy==energy-3 && hp-c.enemyHp==8 && c.block==0 && !StatusMechanics.HasControlProtection(c.playerStatuses) && StatusMechanics.Has(c.enemyStatuses,"blind"),"confirmed hostile cast pays three and applies exactly base eight damage plus blind");
        Check(!c.playerSkills.synergy.charges.Any(x=>x.family=="support"),"hostile Sua W does not prepare a defensive support synergy");

        e=SuaFixture("sua_w","sua_w");c=e.State.combat;
        string upgraded=CardInstances.CreateReference(e.State,"sua_w");e.State.deck[0]=c.hand[0]=upgraded;
        e.State.cardUpgrades.Add(new CardUpgradeSelection {cardId="sua_w",instanceId=upgraded,branchId="power"});
        Check(e.DisplayCombatCard(upgraded).block==0 && AdvancedCardMechanics.Description(e.DisplayCombatCard(upgraded)).Contains("방어도 13"),"upgraded hand display keeps the thirteen-shield choice without granting both branches at once");
        SuaCastSelf(e);
        Check(c.block==13 && c.playerStatuses.controlProtection==13 && e.PlayerCard(c.hand.Single()).block==10,"only the selected physical upgraded Sua copy grants thirteen protection while its ordinary duplicate stays ten");
    }

    static void SuaProtectionStatusRules()
    {
        var actor=new StatusActorState();StatusMechanics.Add(actor,"root","fixture",2);
        StatusMechanics.GrantControlProtection(actor,10);
        Check(StatusMechanics.Has(actor,"root") && !StatusMechanics.CanUse(actor,GameDatabase.Card("aya_e")),"Sua protection does not cleanse an already active crowd control");
        foreach(string kind in new[]{"stun","root","silence","blind","slow","pull","fear","charm","taunt","disarm","suppression","airborne","knockback","polymorph","freeze","sleep","berserk","stasis","dance"})
            Check(!StatusMechanics.Add(actor,kind,"fixture",2) && !actor.effects.Any(x=>x.kind==kind && x.sourceCard=="fixture" && kind!="root"),"protection blocks newly applied crowd control: "+kind);
        foreach(string kind in new[]{"armor_break","heal_reduction","attack_down"})
            Check(StatusMechanics.Add(actor,kind,"fixture") && StatusMechanics.Has(actor,kind),"protection leaves non-control combat debuffs effective: "+kind);
        var clone=StatusMechanics.Clone(actor);StatusMechanics.ConsumeProtection(clone,4);
        Check(clone.controlProtection==6 && actor.controlProtection==10 && StatusMechanics.Has(clone,"root"),"cloned protection and status lists are independent of their live actor");
        Check(StatusMechanics.Snapshot(actor).Single(x=>x.kind=="control_protection").amount==10,"field snapshot shows the actual remaining protection amount");
        StatusMechanics.ConsumeProtection(actor,10);
        Check(!StatusMechanics.HasControlProtection(actor) && StatusMechanics.Add(actor,"blind","after_break"),"control applies normally immediately after protection is exhausted");

        actor=new StatusActorState();StatusMechanics.Add(actor,"stun","isol_q",1,"semtex");StatusMechanics.GrantControlProtection(actor,10);
        Check(!StatusMechanics.Activate(actor,"isol_q","semtex") && !StatusMechanics.Has(actor,"stun") && !actor.effects.Any(x=>x.pending),"delayed control is checked at activation and its blocked preparation is consumed");
        StatusMechanics.Add(actor,"root","isol_q",1,"semtex");StatusMechanics.ClearProtection(actor);
        Check(StatusMechanics.Activate(actor,"isol_q","semtex") && StatusMechanics.Has(actor,"root"),"a delayed control still activates if its target's protection has ended");
    }

    static void SuaProtectionCombatAndExpiry()
    {
        var e=SuaFixture("sua_w");var c=e.State.combat;SuaCastSelf(e);c.block+=20;
        AdvancedEnemyCast(e,"sua_w",new AdvancedEnemyAction {mode="enemy"});
        Check(c.block==22 && c.playerStatuses.controlProtection==2 && !StatusMechanics.Has(c.playerStatuses,"blind"),"a protected hit consumes Sua's ward before unrelated block and ignores blind while two protection remains");
        AdvancedEnemyCast(e,"sua_w",new AdvancedEnemyAction {mode="enemy"});
        Check(c.block==14 && c.playerStatuses.controlProtection==0 && StatusMechanics.Has(c.playerStatuses,"blind"),"a hit breaking the protected ward applies its crowd control even when unrelated block remains");

        e=SuaFixture("sua_w");c=e.State.combat;SuaCastSelf(e);c.block+=30;
        RecastResource(c.enemySkills,"isaac_e","arrest",1,1);
        AdvancedEnemyCast(e,"isaac_e");
        Check(c.block==0 && !StatusMechanics.HasControlProtection(c.playerStatuses) && AdvancedLast(e,"isaac_e",true).destroyedBlock==40,"Isaac E2 destroys the protected ward together with ordinary block before damage");
        Check(StatusMechanics.Add(c.playerStatuses,"stun","after_destruction"),"shield destruction immediately permits new crowd control");

        e=SuaFixture("sua_w");c=e.State.combat;SuaCastSelf(e);
        var pulse=new SkillPulse {kind="delayed_damage",sourceCard="isol_q",key="semtex",amount=4};
        StatusMechanics.Add(c.playerStatuses,"root","isol_q",1,"semtex");
        typeof(GameEngine).GetMethod("ResolveSkillPulse",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(e,new object[]{pulse,true});
        Check(c.block==6 && c.playerStatuses.controlProtection==6 && !StatusMechanics.Has(c.playerStatuses,"root"),"installation damage consumes ward and its subsequent root is ignored while protection remains");
        pulse=new SkillPulse {kind="burn",sourceCard="adriana_q",key="oil",amount=3};int hp=e.State.hp;
        typeof(GameEngine).GetMethod("ResolveSkillPulse",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(e,new object[]{pulse,true});
        Check(e.State.hp==hp-3 && c.playerStatuses.controlProtection==6,"damage-over-time bypasses the shield and does not consume or get blocked by CC protection");

        e=SuaFixture("sua_w");c=e.State.combat;SuaCastSelf(e);AdvancedRound(e);
        Check(c.block==0 && !StatusMechanics.HasControlProtection(c.playerStatuses) && StatusMechanics.Add(c.playerStatuses,"blind","next_turn"),"protection expires with the actual next-player-turn block reset");
    }

    static void SuaEnemyAndSaveParity()
    {
        var e=SuaFixture("sua_w");var c=e.State.combat;int hp=e.State.hp;
        AdvancedEnemyCast(e,"sua_w",new AdvancedEnemyAction {mode="self"});
        Check(c.enemyBlock==10 && c.enemyStatuses.controlProtection==10 && e.State.hp==hp && !StatusMechanics.Has(c.playerStatuses,"blind"),"enemy self-targeted Sua W uses the same shield-only branch and control protection");
        c.enemyTurn=false;c.hand=new List<string>{"sua_w"};
        Check(e.BeginCardChoice(0) && e.ResolveCardChoice("enemy") && c.enemyBlock==2 && !StatusMechanics.Has(c.enemyStatuses,"blind"),"enemy protection ignores player crowd control after a partly absorbed hit");

        e=SuaFixture();c=e.State.combat;c.enemyLevel=20;
        AdvancedEnemyCast(e,"sua_w",new AdvancedEnemyAction {mode="self"});
        Check(c.enemyBlock==10 && c.enemyStatuses.controlProtection==10,"level-twenty enemy protection equals the shield actually granted by its self-targeted skill");
        c.enemyTurn=false;c.enemyPlan.Clear();
        Check(e.BeginEndTurn() && c.enemyBlock==0 && !StatusMechanics.HasControlProtection(c.enemyStatuses),"enemy protection expires with its own real turn-start block reset");

        e=SuaFixture();c=e.State.combat;c.enemyId="sua";c.enemyHp=100;c.enemyMaxHp=1000;c.enemyDeck=new List<string>{"sua_w"};c.enemyDrawPile.Add("sua_w");
        var planner=typeof(GameEngine).GetMethod("PlanEnemyTurn",BindingFlags.NonPublic|BindingFlags.Instance);planner.Invoke(e,null);
        Check(c.enemyPlan.Contains("sua_w") && c.enemyAdvancedActions[c.enemyPlan.IndexOf("sua_w")].mode=="self" && c.intentDamage==0 && c.intentBlock==10,"low-health enemy planning chooses Sua's protective branch and forecasts its shield");
        c.enemyHp=1000;c.enemyDrawPile.Clear();c.enemyDiscardPile.Clear();c.enemyDrawPile.Add("sua_w");planner.Invoke(e,null);
        Check(c.enemyPlan.Contains("sua_w") && c.enemyAdvancedActions[c.enemyPlan.IndexOf("sua_w")].mode=="enemy" && c.intentDamage==8 && c.intentBlock==0,"healthy enemy planning chooses Sua's hostile branch and forecasts damage without self-shield");

        e=SuaFixture("sua_w","basic_attack");c=e.State.combat;SuaCastSelf(e);
        var continued=new GameEngine((RunState)Clone(e.State));
        Check(continued.State.combat.playerStatuses.controlProtection==10 && continued.State.combat.playerStatuses.controlProtectionSource=="sua_w","save/continue retains the remaining protected shield and its source");
        string before=InformationFingerprint(e.State);int rng=e.State.rngState,actions=e.CombatActions.Count;
        var preview=e.PreviewCardUse(0);var tokens=e.SkillStateSnapshot();
        typeof(GameEngine).GetMethod("RefreshEnemyIntent",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(e,null);
        Check(before==InformationFingerprint(e.State) && rng==e.State.rngState && actions==e.CombatActions.Count && tokens.Any(x=>x.kind=="control_protection" && x.amount==10),"card previews, field snapshots and enemy intent leave protection, RNG and all saved state untouched");
        AdvancedEnemyCast(e,"sua_w",new AdvancedEnemyAction {mode="enemy"});AdvancedEnemyCast(continued,"sua_w",new AdvancedEnemyAction {mode="enemy"});
        Check(InformationFingerprint(e.State)==InformationFingerprint(continued.State),"continued and live protected combat resolve to identical actor state and history");
    }

    static void BlindBasicUseAndExpiry()
    {
        var e=SuaFixture("basic_attack","basic_guard","nia_q");var c=e.State.combat;
        StatusMechanics.Add(c.playerStatuses,"blind","sua_w");
        c.freeCasts.Add(new FreeCastGrant {cardId="basic_attack",uses=1});
        c.playerSkills.discounts.Add(new SkillCostDiscount {sourceCard="fixture",targetCard="basic_attack",amount=1});
        string before=InformationFingerprint(c);int rng=e.State.rngState,energy=c.energy,actions=e.CombatActions.Count;
        var preview=e.PreviewCardUse(0);
        Check(!e.CanPlayCard(0) && !e.PlayCard(0) && !e.BeginCardChoice(0) && !preview.canUse && preview.unavailableReason.Contains("실명"),"blind blocks player basic attack, including a free grant, and explains its unavailable reason");
        Check(InformationFingerprint(c)==before && c.energy==energy && e.State.rngState==rng && e.CombatActions.Count==actions,"blocked player basic spends no energy, free grant, discount, RNG, physical card or action hook");
        Check(e.CanPlayCard(1) && e.CanPlayCard(2) && e.Accuracy==75,"blind leaves guarding and subject skills usable while retaining the accuracy penalty");
        StatusMechanics.EndTurn(c.playerStatuses);
        Check(e.CanPlayCard(0) && e.PlayCard(0) && c.freeCasts.All(x=>x.uses==0),"basic attacks and their retained free grant work normally when blind expires");

        e=SuaFixture();c=e.State.combat;StatusMechanics.Add(c.enemyStatuses,"blind","sua_w");c.enemyFreeCasts.Add(new FreeCastGrant {cardId="basic_attack",uses=1});
        int hp=e.State.hp;actions=e.CombatActions.Count;rng=e.State.rngState;
        AdvancedEnemyCast(e,"basic_attack");
        Check(c.enemyAvailableEnergy==99 && e.State.hp==hp && c.enemyFreeCasts.Single().uses==1 && e.CombatActions.Count==actions && e.State.rngState==rng,"blind blocks the enemy's already-planned free basic without energy, RNG, damage or hook consumption");
        StatusMechanics.EndTurn(c.enemyStatuses);AdvancedEnemyCast(e,"basic_attack");
        Check(e.State.hp<hp && c.enemyFreeCasts.All(x=>x.uses==0) && AdvancedLast(e,"basic_attack",true).damage>0,"the same enemy basic and free grant resolve after blind expires");

        e=SuaFixture();c=e.State.combat;c.enemyDeck=new List<string>{"basic_attack","nia_q"};c.enemyDrawPile.AddRange(c.enemyDeck);StatusMechanics.Add(c.enemyStatuses,"blind","sua_w");
        typeof(GameEngine).GetMethod("PlanEnemyTurn",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(e,null);
        Check(!c.enemyPlan.Contains("basic_attack") && c.enemyPlan.Contains("nia_q"),"enemy planning omits blinded basics but can still choose affordable subject skills");
    }
}
#endif
