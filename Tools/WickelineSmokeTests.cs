#if LUMIA_TEST_RUNNER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lumia;

public static partial class CoreSmokeTests
{
    static object WickInvoke(GameEngine e,string method,params object[] args)
    {return typeof(GameEngine).GetMethod(method,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(e,args);}

    static GameEngine WickelineFixture(RunMode mode=RunMode.Normal,int seed=62173,bool enter=true)
    {
        var e=ReadyMode(mode,seed);e.State.level=18;e.State.hp=e.State.maxHp=9999;
        var node=e.State.map.Single(n=>n.wickeline);e.State.act=3;e.State.row=node.row-1;e.State.lane=node.lane;
        if(enter)Check(e.EnterNode(node.lane) && e.IsWickelineCombat,"assigned Act 3 camp starts the dedicated Wickeline battle");
        return e;
    }

    static void WickelineMapAndIsolation()
    {
        var selected=new HashSet<string>();
        for(int seed=1;seed<=40;seed++)
        {
            var e=new GameEngine(seed);int rng=e.State.rngState;
            var camps=e.State.map.Where(n=>n.act==3 && n.kind==ZoneKind.Campfire).ToArray();
            Check(camps.Length==5 && camps.Count(n=>n.wickeline)==1,"exactly one of five unchanged Act 3 camp placements is Wickeline");
            Check(e.State.map.Where(n=>n.act<3).All(n=>!n.wickeline),"Act 1 and 2 never contain Wickeline");
            var chosen=camps.Single(n=>n.wickeline);selected.Add(chosen.row+":"+chosen.lane);
            var restored=new GameEngine((RunState)Clone(e.State));
            Check(restored.State.map.Single(n=>n.wickeline).id==chosen.id && restored.State.rngState==rng,"map save/load preserves assignment without consuming run RNG");
        }
        Check(selected.Count>1,"assignment varies between run seeds");
        string[] ids={WickelineMechanics.Tracking,WickelineMechanics.Gas,WickelineMechanics.Triplet,WickelineMechanics.Control,WickelineMechanics.Isolation,WickelineMechanics.Release,WickelineMechanics.Collapse};
        Check(GameDatabase.Characters.Count==91 && GameDatabase.Character(WickelineMechanics.EnemyId)==null,"NPC never becomes a 92nd selectable subject");
        Check(ids.All(id=>GameDatabase.Card(id)==null && WickelineMechanics.Card(id)?.category=="wickeline"),"all seven enemy-only actions stay outside starting, draft, collection and reward card catalog");
        var map=WickelineFixture(enter:false);var ordinary=map.State.map.First(n=>n.act==3 && n.kind==ZoneKind.Campfire && !n.wickeline);
        map.State.row=ordinary.row-1;map.State.lane=ordinary.lane;
        Check(map.EnterNode(ordinary.lane) && map.State.stage==RunStage.Campfire && map.State.combat==null,"other Act 3 camps retain ordinary camp entry");
        Check(map.EnemyWickelineDescription.Contains("100 크레딧") && map.EnemyWickelineDescription.Contains("격리"),"map guide is available before combat");

        var legacy=WickelineFixture(enter:false);var target=legacy.State.map.Single(n=>n.wickeline);int targetId=target.id;
        foreach(var node in legacy.State.map){node.wickeline=false;node.wickelineVersion=0;node.wickelineDefeated=false;}
        legacy.State.activeNodeId=targetId;legacy.State.stage=RunStage.Campfire;legacy.State.campChoice=1;legacy.State.campRemovalsRemaining=2;
        int beforeRng=legacy.State.rngState,credits=legacy.State.credits,objects=legacy.State.objects.Count;
        var loaded=new GameEngine((RunState)Clone(legacy.State));
        Check(loaded.State.stage==RunStage.Campfire && loaded.State.campRemovalsRemaining==2 && loaded.State.map.Single(n=>n.wickeline).id==targetId
            && loaded.State.map.Single(n=>n.wickeline).wickelineDefeated,"old save already using selected camp keeps its rest and removal budget");
        Check(loaded.State.credits==credits && loaded.State.objects.Count==objects && loaded.State.rngState==beforeRng,"legacy camp migration grants no retroactive loot or RNG draw");
        legacy.State.stage=RunStage.Map;legacy.State.activeNodeId=-1;target.visited=true;
        loaded=new GameEngine((RunState)Clone(legacy.State));
        Check(loaded.State.map.Single(n=>n.wickeline).wickelineDefeated,"an already visited legacy selected camp cannot spawn a retroactive fight");
    }

    static void WickelineCombatAndPersistence()
    {
        // Check committed intent directly, before any information getter or preview can refresh it.
        var direct=WickelineFixture();var committed=direct.State.combat;
        Check(committed.turn==1 && committed.intentDamage==14,"first Wickeline plan commits fourteen damage without an observer refresh");
        Check(direct.EndTurn() && committed.turn==2 && committed.intentDamage==44
            && committed.enemyPlan.SequenceEqual(new[]{WickelineMechanics.Triplet,"basic_attack"}),
            "turn transition commits forty-four Triplet-plus-basic damage before any EnemyIntent or Preview call");
        var directLoad=new GameEngine((RunState)Clone(direct.State));
        Check(directLoad.State.combat.intentDamage==44,"turn-two save/reload retains the committed forty-four damage without opening information");
        Check(direct.EndTurn() && committed.turn==3 && committed.intentDamage==28,"Control turn commits its two basic attacks without an observer refresh");
        Check(direct.EndTurn() && committed.turn==4 && committed.intentDamage==0,"isolation charge commits zero incoming damage without an observer refresh");
        Check(direct.EndTurn() && committed.turn==5 && committed.intentDamage==24,"isolation release commits its damage without an observer refresh");
        var e=WickelineFixture();var c=e.State.combat;
        Check(c.enemyId==WickelineMechanics.EnemyId && c.boss==null && string.IsNullOrEmpty(c.animal) && c.enemyGear.Count==0 && c.enemyTacticalCardId==null,"Wickeline has no subject passive, weapon, tactical, animal or act-boss profile");
        Check(c.enemyPlan.SequenceEqual(new[]{WickelineMechanics.Gas,"basic_attack"}) && c.enemyPlanCosts.All(x=>x==0),"first turn clearly announces gas and one basic attack");
        string before=InformationFingerprint(e.State);int actions=e.CombatActions.Count;
        for(int i=0;i<4;i++)
        {
            string intent=e.EnemyIntent,guide=e.EnemyWickelineDescription,status=e.EnemyWickelineStatus;
            foreach(string id in c.enemyPlan)Check(e.PreviewCardUse(id,true).currentCost==0,"dedicated planned NPC action displays zero resource cost");
        }
        Check(InformationFingerprint(e.State)==before && e.CombatActions.Count==actions,"guides, status and card/intent previews mutate no saved field, RNG or action queue");
        int forecast=c.intentDamage,hp=e.State.hp;
        Check(e.BeginEndTurn() && e.AdvanceEnemyAction() && WickelineMechanics.GasActive(c),"gas activates as an independently paced enemy action");
        var loaded=new GameEngine((RunState)Clone(e.State));
        Check(loaded.State.combat.enemyActionIndex==1 && loaded.State.combat.wickeline.gasUntilTurn==3,"mid-enemy-turn reload retains consumed gas action and its expiry");
        while(e.State.stage==RunStage.Combat && c.enemyTurn)e.AdvanceEnemyAction();
        while(loaded.State.stage==RunStage.Combat && loaded.State.combat.enemyTurn)loaded.AdvanceEnemyAction();
        Check(e.State.hp==hp-forecast,"paced attack resolution matches displayed forecast");
        Check(InformationFingerprint(loaded.State)==InformationFingerprint(e.State),"paced resume resolves the same remaining state and RNG as uninterrupted combat");
        e.State.hp=100;WickInvoke(e,"Heal",10);Check(e.State.hp==106,"gas reduces ordinary healing by exactly forty percent");
        e.State.foods.Add("soup");e.State.hp=e.State.maxHp-100;
        Check(e.UseFood(e.State.foods.LastIndexOf("soup")) && e.State.hp==e.State.maxHp-40,"full-heal food obeys active gas rather than bypassing it");
        e.State.hp=9999;c.block=0;hp=e.State.hp;string preview=e.EnemyIntent;forecast=c.intentDamage;
        Check(c.enemyPlan.SequenceEqual(new[]{WickelineMechanics.Triplet,"basic_attack"}) && e.EndTurn() && hp-e.State.hp==forecast,"gas attack-speed conversion adds exactly one attack after the three independent Triplet hits");
        Check(StatusMechanics.Has(c.playerStatuses,"slow"),"Triplet grants its short slow on a landed hit");
        c.enemyHp=200;c.turn=4;c.wickeline.gasUntilTurn=0;c.enemyStatuses.effects.Clear();
        StatusMechanics.Add(c.enemyStatuses,"heal_reduction","fixture",2);
        WickInvoke(e,"PlanEnemyTurn");int expectedHeal=(20+(c.enemyMaxHp-c.enemyHp)/10)*80/100;
        Check(e.EndTurn() && c.enemyHp==200+expectedHeal && WickelineMechanics.Isolating(c),"isolation heals once with antiheal applied and protects the following player turn");
        int enemyHp=c.enemyHp;int received=(int)WickInvoke(e,"ApplyExternalHealthDamage",true,40,false);
        Check(received==24 && c.enemyHp==enemyHp-24,"latest isolation converts to forty-percent reduction of incoming health damage");
        before=InformationFingerprint(e.State);var cardPreview=e.PreviewCardUse("basic_attack");
        Check(cardPreview.damageNote.Contains("40%") && InformationFingerprint(e.State)==before,"player preview explains isolation without changing its charge");
        loaded=new GameEngine((RunState)Clone(e.State));
        Check(loaded.State.combat.wickeline.isolationPending && loaded.State.combat.enemyPlan.Contains(WickelineMechanics.Release),"save/load preserves pending charge and exact planned release");
        Check(loaded.EndTurn() && !loaded.State.combat.wickeline.isolationPending && StatusMechanics.Has(loaded.State.combat.playerStatuses,"knockback"),"charged release executes once and grants short knockback");

        e=WickelineFixture();c=e.State.combat;c.turn=3;WickInvoke(e,"PlanEnemyTurn");StatusMechanics.Add(c.enemyStatuses,"root","fixture",1);hp=e.State.hp;
        Check(e.EndTurn() && e.State.hp==hp,"root interrupts Control and cancels its two following basic attacks");
        e=WickelineFixture();c=e.State.combat;
        var statuses=StatusMechanics.Snapshot(c.enemyStatuses);StatusMechanics.Add(c.enemyStatuses,"slow","fixture",2);
        WickInvoke(e,"RecordStatusChanges",false,c.enemyStatuses,statuses);
        Check(c.enemyStatuses.effects.Single(x=>x.kind=="slow").remaining==1,"permanent fifty-percent CC resistance halves a two-turn status without removing it");

        e=WickelineFixture();c=e.State.combat;c.turn=4;c.wickeline.isolationPending=true;c.wickeline.isolationUntilTurn=5;WickInvoke(e,"PlanEnemyTurn");
        enemyHp=c.enemyHp;received=(int)WickInvoke(e,"ApplyExternalHealthDamage",true,150,false);
        Check(received==90 && c.enemyHp==enemyHp-90-c.enemyMaxHp/10 && WickelineMechanics.Collapsed(c)
            && !c.wickeline.isolationPending && c.wickeline.structureDamage==0,"actual reduced damage fills structure, applies max-HP collapse and cancels isolation");
        Check(e.State.combatHistory.Last().detail.Contains("위클라인 체력 피해"),"collapse history identifies its self-damage target correctly");
        hp=e.State.hp;Check(e.EndTurn() && e.State.hp==hp && c.turn==5,"collapse cancels the next enemy action turn");
        received=(int)WickInvoke(e,"ApplyExternalHealthDamage",true,10,false);
        Check(received==13 && c.wickeline.structureDamage==0,"next player turn receives collapse vulnerability without immediately refilling structure");
        hp=e.State.hp;Check(e.EndTurn() && e.State.hp<hp && c.turn==6,"collapse skips one enemy turn rather than permanently locking the NPC");

        e=WickelineFixture();c=e.State.combat;c.turn=2;WickInvoke(e,"PlanEnemyTurn");
        string baseline=e.EnemyIntent;int baselineDamage=c.intentDamage;
        StatusMechanics.Add(c.playerStatuses,"armor_break","fixture",1);
        c.playerTraits.buffs.Add(new TraitBuff{sourceId="fixture",kind="exposure",amount=30,remaining=1});
        before=InformationFingerprint(e.State);preview=e.EnemyIntent;
        Check(c.intentDamage==baselineDamage && before==InformationFingerprint(e.State),"NPC forecast expires one-turn armor break/exposure on clones before estimating enemy actions");
        hp=e.State.hp;Check(e.EndTurn() && hp-e.State.hp==baselineDamage,"expired defensive debuffs do not inflate actual NPC multihit damage");

        e=WickelineFixture();c=e.State.combat;c.wickeline.isolationPending=true;c.wickeline.isolationUntilTurn=2;
        c.enemyHp=50;c.enemyBlock=500;c.hand.Clear();c.hand.Add("garnet_r");c.energy=0;
        e.State.deck.Add("garnet_r");c.freeCasts.Add(new FreeCastGrant{cardId="garnet_r",uses=1});
        c.playerSkills.resources.Add(new SkillResource{owner=GameDatabase.Card("garnet_r").owner,key="embrace",amount=1,cap=1});
        Check(e.PlayCard(0) && c.enemyHp==0 && c.enemyBlock==500 && e.State.stage==RunStage.Campfire
            && e.CombatActions.Any(x=>x.execution),"real Garnet free-recast execution bypasses isolation percentage mitigation and shield");
    }

    static void WickelineRewardsAndEndless()
    {
        var e=WickelineFixture();var c=e.State.combat;var node=e.State.map.Single(n=>n.wickeline);
        int nodeId=node.id,credits=e.State.credits,objects=e.State.objects.Count,xp=e.State.xp,deck=e.State.deck.Count,bosses=e.State.bossVictories,energy=e.State.maxEnergyBonus;
        c.enemyHp=1;c.enemyBlock=c.enemyEvasion=0;c.hand.Add("basic_attack");c.energy=99;
        var stale=(RunState)Clone(e.State);
        Check(e.PlayCard(c.hand.Count-1) && e.State.stage==RunStage.Campfire && e.State.combat==null && e.State.rewards==null,"victory opens campfire immediately without ordinary rewards");
        Check(node.wickelineDefeated && e.IsWickelineNode(node) && !node.visited && e.State.activeNodeId==nodeId && e.State.campActions==3,"victory preserves the same uncompleted camp node and full camp actions");
        Check(e.State.credits==credits+100 && e.State.objects.Count==objects+2 && e.State.objects.Count(x=>x=="blood")==1
            && e.State.objects.Count(x=>x=="tree" || x=="meteorite")==1,"victory grants one guaranteed sample, exactly one tree/meteorite and one hundred credits");
        Check(e.State.xp==xp && e.State.deck.Count==deck && e.State.bossVictories==bosses && e.State.maxEnergyBonus==energy && e.State.pendingPassive==null,"special NPC grants no subject cards, XP, passive or act-boss energy growth");
        var loaded=new GameEngine((RunState)Clone(e.State));
        Check(loaded.State.credits==e.State.credits && loaded.State.objects.SequenceEqual(e.State.objects) && !loaded.EndTurn() && !loaded.FinishRewards(),"post-victory camp save cannot resolve another combat/reward payout");
        Check(loaded.ChooseCamp(false) && loaded.LeaveCamp() && loaded.State.stage==RunStage.Map && loaded.State.map.Single(n=>n.id==nodeId).visited,"normal rest/removal/leave flow completes that original camp exactly once");
        stale.map.Single(n=>n.wickeline).wickelineDefeated=true;loaded=new GameEngine(stale);
        Check(loaded.PlayCard(loaded.State.combat.hand.Count-1) && loaded.State.stage==RunStage.Campfire && loaded.State.credits==credits && loaded.State.objects.Count==objects,"persisted defeated marker prevents reward duplication even with a stale combat snapshot");

        foreach(bool gas in new[]{false,true})
        {
            var healing=WickelineFixture(RunMode.Endless);healing.State.augments.Add("aug_s_victory_heal");healing.State.hp=100;
            var battle=healing.State.combat;battle.enemyHp=1;battle.enemyBlock=battle.enemyEvasion=0;
            battle.hand.Clear();battle.hand.Add("basic_attack");battle.energy=99;
            if(gas)battle.wickeline.gasUntilTurn=battle.turn+2;
            var staleHealing=(RunState)Clone(healing.State);
            Check(healing.PlayCard(0) && healing.State.stage==RunStage.Campfire && healing.State.hp==104,
                "Wickeline first victory restores the full four-HP victory augment after combat and gas end: gas="+gas);
            var healedLoad=new GameEngine((RunState)Clone(healing.State));
            Check(healedLoad.State.hp==104 && !healedLoad.EndTurn() && healedLoad.State.hp==104,
                "loading rewarded Wickeline camp cannot repeat victory healing: gas="+gas);
            staleHealing.map.Single(n=>n.wickeline).wickelineDefeated=true;
            var staleLoad=new GameEngine(staleHealing);
            Check(staleLoad.PlayCard(0) && staleLoad.State.stage==RunStage.Campfire && staleLoad.State.hp==100,
                "already defeated marker suppresses victory healing with stale combat data: gas="+gas);
        }

        e=WickelineFixture();credits=e.State.credits;objects=e.State.objects.Count;e.State.hp=1;c=e.State.combat;c.block=c.evasion=0;
        Check(e.EndTurn() && e.State.stage==RunStage.Lost && !e.State.map.Single(n=>n.wickeline).wickelineDefeated,"death in the NPC encounter is an ordinary terminal defeat");
        loaded=new GameEngine((RunState)Clone(e.State));
        Check(loaded.State.stage==RunStage.Lost && loaded.State.credits==credits && loaded.State.objects.Count==objects && !loaded.FinishRewards(),"defeat reload grants no loot or camp access");

        var normal=WickelineFixture();var hard=WickelineFixture(RunMode.Hard);int maximum=hard.State.combat.enemyMaxHp;
        Check(maximum==(normal.State.combat.enemyMaxHp*125+99)/100,"NPC health uses existing difficulty scaling exactly once");
        loaded=new GameEngine((RunState)Clone(hard.State));Check(loaded.State.combat.enemyMaxHp==maximum,"NPC reload never reapplies hard health scaling");
        e=ReadyMode(RunMode.Endless);int first=e.State.map.Single(n=>n.wickeline).id;
        var old=e.State.map.Single(n=>n.wickeline);old.visited=true;old.wickelineDefeated=true;
        for(int act=1;act<=3;act++)CompleteModeBoss(e,act);
        Check(e.State.endlessCycle==1 && e.State.map.Count(n=>n.wickeline)==1 && e.State.map.Single(n=>n.wickeline).id!=first
            && !e.State.map.Single(n=>n.wickeline).wickelineDefeated,"each endless map independently gets one fresh undefeated Wickeline camp");
        Check(e.State.archivedMapNodes.Any(n=>n.id==first && n.wickelineDefeated),"visited old-cycle NPC camp retains its defeated marker in the archive");
    }
}
#endif
