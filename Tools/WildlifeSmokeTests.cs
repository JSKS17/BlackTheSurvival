#if LUMIA_TEST_RUNNER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lumia;

public static partial class CoreSmokeTests
{
    static GameEngine WildlifeFixture(string animal,int act=1,bool mutant=false,int turn=1)
    {
        var e=Ready(927);e.State.act=act;e.State.row=-1;e.State.lane=-1;
        e.State.passives.Clear();e.State.mainRune=e.State.supportRune=null;e.State.persistentTraits=new TraitActorState();
        var node=e.AvailableNodes().First(n=>n.lane==1);node.kind=ZoneKind.Wildlife;
        Check(node.act==act,"wildlife fixture selects the actual active act map");
        node.assignedAnimal=animal;node.wildlifeVariantVersion=1;node.mutantAnimal=mutant;
        Enter(e,ZoneKind.Wildlife);
        var c=e.State.combat;e.State.hp=e.State.maxHp=5000;c.enemyHp=5000;
        c.block=c.evasion=c.enemyWeak=c.vulnerable=c.enemyVulnerable=0;
        c.turn=turn;c.enemyTraits=new TraitActorState();c.playerTraits=new TraitActorState();
        ReplanWildlife(e);return e;
    }
    static void ReplanWildlife(GameEngine e)
    {
        typeof(GameEngine).GetMethod("PlanEnemyTurn",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(e,null);
    }
    static void WildlifeSpecialActions()
    {
        var boar=WildlifeFixture("boar",turn:2);var c=boar.State.combat;
        Check(c.enemyPlan.SequenceEqual(new[]{WildlifeMechanics.BoarCharge}) && c.enemyPlanCosts.SequenceEqual(new[]{0}),"boar announces its animal-only charge instead of adding another attack on every second turn");
        int raw=WildlifeMechanics.BasicDamage("boar",c.enemyLevel)+4;
        Check(boar.EnemyIntent.Contains("돌진") && c.intentDamage==raw && boar.EnemyActionCard(WildlifeMechanics.BoarCharge).movement,"charge forecast includes its named movement action and extra damage");
        Check(boar.BeginEndTurn() && boar.AdvanceEnemyAction(),"boar resolves one paced animal-only charge");
        var hit=boar.CombatActions.Last(a=>a.kind=="wildlife_skill");
        Check(hit.damage==raw && !hit.critical && StatusMechanics.Has(c.playerStatuses,"knockback"),"charge deals forecast damage, cannot critically hit, and applies original knockback");
        Check(boar.AdvanceEnemyAction() && c.energy==boar.MaxEnergy-1 && c.turn==3 && c.enemyPlan.SequenceEqual(new[]{"basic_attack"}),"knockback removes one next-turn energy and the charge cooldown returns to a basic attack");

        var bear=WildlifeFixture("bear",turn:3);c=bear.State.combat;
        Check(c.enemyPlan.SequenceEqual(new[]{WildlifeMechanics.BearSlam}) && bear.EnemyIntent.Contains("지면 강타"),"bear announces ground slam every third turn");
        raw=WildlifeMechanics.BasicDamage("bear",c.enemyLevel)+5;
        Check(c.intentDamage==raw && bear.BeginEndTurn() && bear.AdvanceEnemyAction(),"bear ground slam uses the same live damage as its forecast");
        Check(bear.CombatActions.Last(a=>a.kind=="wildlife_skill").damage==raw && StatusMechanics.Has(c.playerStatuses,"stun"),"ground slam applies damage and original stun");
        Check(bear.AdvanceEnemyAction() && c.turn==4 && c.energy==bear.MaxEnergy-1 && c.enemyPlan.SequenceEqual(new[]{"basic_attack"}),"bear stun lasts one player turn while slam remains on cooldown");

        foreach(string animal in new[]{"boar","bear"})
        {
            var e=WildlifeFixture(animal,turn:animal=="boar"?2:3);c=e.State.combat;
            StatusMechanics.Add(c.enemyStatuses,"stun","fixture",1);
            string before=InformationFingerprint(e.State);string intent=e.EnemyIntent;
            Check(c.intentDamage==0 && intent.Contains(animal=="boar"?"돌진":"지면 강타"),"hard control removes the announced special attack's damage: "+animal);
            var preview=e.PreviewCardUse(c.enemyPlan[0],true);
            Check(preview.normalDamage==0 && preview.unavailableReason.Contains("중단") && preview.classification.Contains("동물 전용"),"read-only animal skill details explain the same interruption: "+animal);
            Check(e.BeginEndTurn() && e.AdvanceEnemyAction() && e.State.hp==5000 && e.CombatActions.Any(a=>a.kind=="wildlife_interrupt"),"hard control interrupts the telegraphed animal skill: "+animal);
            Check(!StatusMechanics.Has(c.playerStatuses,animal=="boar"?"knockback":"stun"),"an interrupted special applies no crowd control: "+animal);
            if(animal=="boar")
            {
                Check(c.enemyVulnerable==2 && StatusMechanics.Has(c.enemyStatuses,"slow") && e.CombatActions.Any(a=>a.kind=="wildlife_despair"),"interrupted charge applies despair before enemy-end ticking");
                e.AdvanceEnemyAction();
                Check(c.enemyVulnerable==1 && e.CardDamage("basic_attack")>(GameDatabase.Card("basic_attack").damage+e.State.level/3),"despair persists through exactly the next player attack window");
                e.EndTurn();Check(c.enemyVulnerable==0 && !StatusMechanics.Has(c.enemyStatuses,"slow"),"despair expires after the one player response turn");
            }
        }
        var rooted=WildlifeFixture("boar",turn:2);StatusMechanics.Add(rooted.State.combat.enemyStatuses,"root","fixture",1);
        Check(rooted.EnemyIntent.Contains("돌진") && rooted.State.combat.intentDamage==0 && rooted.EndTurn() && rooted.State.combat.enemyVulnerable==1,"root stops the boar movement skill and grants its despair window");
        var protectedPlayer=WildlifeFixture("bear",turn:3);protectedPlayer.State.combat.block=999;
        Check(protectedPlayer.BeginEndTurn() && protectedPlayer.AdvanceEnemyAction() && protectedPlayer.State.hp==5000 && protectedPlayer.CombatActions.Last(a=>a.kind=="wildlife_skill").absorbed>0,"animal skills respect existing player block and report absorbed damage");
        var missed=WildlifeFixture("boar",turn:2);missed.State.combat.evasion=65;
        Check(missed.BeginEndTurn(),"prepare evasive charge fixture");
        // Pick an exact deterministic RNG state that makes this hit roll miss.
        for(int seed=1;seed<1000;seed++)
        {
            var probe=new GameEngine(CombatInformation.Copy(missed.State));probe.State.rngState=seed;
            if(probe.AdvanceEnemyAction() && probe.CombatActions.Any(a=>a.kind=="wildlife_skill" && a.avoided==1))
            {missed.State.rngState=seed;break;}
        }
        Check(missed.AdvanceEnemyAction() && missed.CombatActions.Last(a=>a.kind=="wildlife_skill").avoided==1 && missed.State.combat.enemyVulnerable==2 && missed.State.hp==5000,"dodging a charge triggers despair without damage or knockback");
        var resumed=new GameEngine(CombatInformation.Copy(missed.State));
        Check(resumed.State.combat.enemyVulnerable==2 && resumed.AdvanceEnemyAction() && resumed.State.combat.enemyVulnerable==1,"a save between charge and enemy-end preserves despair without repeating its action");
        var repair=CombatInformation.Copy(WildlifeFixture("bear",turn:3).State);repair.combat.enemyPlanCosts=null;repair.combat.enemyPlanFreeCast=null;
        var repaired=new GameEngine(repair);
        Check(repaired.State.combat.enemyPlanCosts.SequenceEqual(new[]{0}) && repaired.State.combat.enemyPlanFreeCast.SequenceEqual(new[]{false}),"missing saved animal plan metadata restores a natural action, never a 99-cost card or a free recast");
    }
    static void WildlifeMutationsAndPersistence()
    {
        Check(WildlifeMechanics.MutationChance(1)==0 && WildlifeMechanics.MutationChance(2)==8 && WildlifeMechanics.MutationChance(3)==14,"mutants are absent from act one, rare in act two and somewhat more common in act three");
        var e=Ready(81761);int rng=e.State.rngState;int[] counts=new int[3];
        for(int act=1;act<=3;act++)
        {
            for(int id=0;id<5000;id++)
            {
                var node=new MapNode{id=id+100000,act=act,kind=ZoneKind.Wildlife};
                bool mutant=e.IsMutantWildlife(node);if(mutant)counts[act-1]++;
                Check(e.IsMutantWildlife(node)==mutant && node.wildlifeVariantVersion==0,"read-only variant prediction is fixed without mutating node metadata");
            }
        }
        Check(counts[0]==0 && counts[1]>320 && counts[1]<480 && counts[2]>600 && counts[2]<800 && e.State.rngState==rng,"fixed variant stream follows 0/8/14 percent while preserving run RNG: "+string.Join("/",counts));
        Check(e.State.map.Where(n=>n.kind==ZoneKind.Wildlife).All(n=>n.wildlifeVariantVersion==1 && (n.act>1 || !n.mutantAnimal)),"new maps persist each wildlife variant before entering or scouting");
        var copy=CombatInformation.Copy(e.State);var loaded=new GameEngine(copy);
        Check(e.State.map.Select(n=>n.mutantAnimal).SequenceEqual(loaded.State.map.Select(n=>n.mutantAnimal)) && rng==loaded.State.rngState,"loading does not reroll node variants or spend battle RNG");
        foreach(int act in new[]{2,3})foreach(string animal in new[]{"chicken","dog","boar","wolf","bear"})
        {
            var normal=WildlifeFixture(animal,act);var mutant=WildlifeFixture(animal,act,true);
            Check(mutant.State.combat.enemyMaxHp==WildlifeMechanics.ScaleHealth(normal.State.combat.enemyMaxHp,true) && mutant.State.combat.mutantAnimal && mutant.State.combat.enemyName=="변이 "+WildlifeMechanics.Name(animal),"all five species gain only the intended mutant HP and visible identity: "+act+"/"+animal);
            Check(mutant.State.combat.intentDamage>normal.State.combat.intentDamage && mutant.State.combat.enemyEvasion==0,"mutants increase attack but retain zero wildlife evasion: "+animal);
            int predicted=mutant.State.combat.intentDamage;int hpBefore=mutant.State.hp;
            Check(mutant.BeginEndTurn(),"prepare actual mutant attack check");
            while(mutant.State.combat.enemyActionIndex<mutant.State.combat.enemyPlan.Count)mutant.AdvanceEnemyAction();
            Check(hpBefore-mutant.State.hp==predicted,"mutant forecast matches actual zero-evasion attack damage: "+animal);
            string fingerprint=InformationFingerprint(mutant.State);var saved=new GameEngine(CombatInformation.Copy(mutant.State));
            Check(saved.State.combat.enemyMaxHp==mutant.State.combat.enemyMaxHp && saved.State.combat.mutantAnimal && saved.State.rngState==mutant.State.rngState,"resuming a mutant never reapplies HP scaling or rerolls RNG: "+animal);
            var node=mutant.State.map.First(n=>n.id==mutant.State.activeNodeId);mutant.State.revealedNodeIds.Add(node.id);
            Check(mutant.NodeIntel(node)=="변이 "+WildlifeMechanics.Name(animal),"scouted names match the committed mutant opponent: "+animal);
            var legacy=CombatInformation.Copy(normal.State);var legacyNode=legacy.map.First(n=>n.id==legacy.activeNodeId);
            legacyNode.wildlifeVariantVersion=0;legacyNode.mutantAnimal=true;
            var restoredLegacy=new GameEngine(legacy);
            Check(!restoredLegacy.State.combat.mutantAnimal && !legacyNode.mutantAnimal && legacyNode.wildlifeVariantVersion==1,"an existing ordinary battle remains ordinary during legacy migration: "+animal);
        }
        var oldMap=CombatInformation.Copy(e.State);foreach(var node in oldMap.map){node.wildlifeVariantVersion=0;node.mutantAnimal=false;}
        var migrated=new GameEngine(oldMap);var migratedAgain=new GameEngine(CombatInformation.Copy(migrated.State));
        Check(migrated.State.map.Select(n=>n.mutantAnimal).SequenceEqual(migratedAgain.State.map.Select(n=>n.mutantAnimal)) && migrated.State.rngState==rng,"legacy unexplored maps gain stable variants exactly once");
        Console.WriteLine("Wildlife variant sample per 5000 nodes: "+string.Join("/",counts)+" (acts 1/2/3).");
    }
    static void WildlifeMutationRewardsAndIsolation()
    {
        foreach(string id in new[]{WildlifeMechanics.BoarCharge,WildlifeMechanics.BearSlam})
        {
            Check(GameDatabase.Card(id)==null && !GameDatabase.Cards.Any(c=>c.id==id) && !GameDatabase.Characters.Any(c=>c.cards.Contains(id)),"animal skills never enter the player catalog or subject skill pool: "+id);
            var e=WildlifeFixture("boar");e.State.combat.hand.Add(id);e.State.combat.energy=999;
            Check(!e.CanPlayCard(e.State.combat.hand.Count-1) && !e.PlayCard(e.State.combat.hand.Count-1),"player cannot cast an animal skill even if its id is injected into the hand: "+id);
            Win(e);e.State.rewards.choices.Add(id);
            Check(!e.CanClaimCard(id) && !e.ClaimCard(id),"animal skills cannot be claimed even from a corrupted reward list: "+id);
        }
        Check(GameEngine.WildlifeBasicAttackDropChance==3 && GameEngine.WildlifeMeatDropChance==50,"mutant update preserves existing rare basic attacks and half-chance meat");
        foreach(string animal in new[]{"chicken","dog","boar","wolf","bear"})
        {
            Check(WildlifeMechanics.ObjectDropChance(animal,true)==(animal=="wolf"?30:animal=="bear"?40:0),"mutant object bonus retains species eligibility: "+animal);
            var template=WildlifeFixture(animal,2,true);var state=CombatInformation.Copy(template.State);
            int drops=0,meat=0,basic=0;
            for(int seed=1;seed<=500;seed++)
            {
                var e=new GameEngine(CombatInformation.Copy(state));e.State.rngState=seed*7919;
                Win(e);var r=e.State.rewards;
                if(r.objectId!=null)drops++;if(r.foodId=="meat")meat++;if(r.choices.Contains("basic_attack"))basic++;
                Check(r.choices.All(id=>GameDatabase.Card(id)!=null && !WildlifeMechanics.IsAnimalSkill(id)),"mutant rewards contain only playable cards: "+animal);
                Check(r.objectId==null || animal=="wolf" && new[]{"tree","meteorite"}.Contains(r.objectId) || animal=="bear" && new[]{"tree","meteorite","mithril","force"}.Contains(r.objectId),"mutant objects obey the original wolf/bear table and never grant blood: "+animal);
            }
            Check((animal=="wolf"?drops>110 && drops<190:animal=="bear"?drops>160 && drops<240:drops==0) && meat>210 && meat<290 && basic>4 && basic<28,"mutant reward sample keeps intended object/basic/meat frequencies: "+animal+" "+drops+"/"+basic+"/"+meat);
        }
    }
}
#endif
