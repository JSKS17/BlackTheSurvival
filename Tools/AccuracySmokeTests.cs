#if LUMIA_TEST_RUNNER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lumia;

public static partial class CoreSmokeTests
{
    static void AccuracyAndTargeting()
    {
        var e=MechanicsFixture();var c=e.State.combat;
        Check(e.Accuracy==100 && e.EnemyAccuracy==100,"both actors start at 100 accuracy");
        c.evasion=c.enemyEvasion=50;
        StatusMechanics.Add(c.playerStatuses,"blind","weapon_camera");
        Check(e.Accuracy==75 && e.EnemyAccuracy==100 && e.Evasion==50 && e.EnemyEvasion==50,"player blind affects only player accuracy");
        StatusMechanics.Add(c.enemyStatuses,"blind","weapon_camera");
        Check(e.EnemyAccuracy==75 && e.Evasion==50,"enemy blind affects only enemy accuracy");
        foreach(bool enemy in new[]{false,true})
        {
            var statuses=enemy?c.enemyStatuses:c.playerStatuses;
            StatusMechanics.Add(statuses,"root","isol_q");
            Check((enemy?e.EnemyEvasion:e.Evasion)==30,"root reduces affected actor evasion by 20 points");
            StatusMechanics.Add(statuses,"stun","nia_r");
            Check((enemy?e.EnemyEvasion:e.Evasion)==30,"root and stun evasion penalties do not stack");
            statuses.effects.RemoveAll(x=>x.kind=="root");
            Check((enemy?e.EnemyEvasion:e.Evasion)==30,"stun independently reduces affected actor evasion");
            if(enemy)c.enemyEvasion=10;else c.evasion=10;
            Check((enemy?e.EnemyEvasion:e.Evasion)==0,"control cannot produce negative evasion");
            StatusMechanics.EndTurn(statuses);
            Check((enemy?e.EnemyEvasion:e.Evasion)==10 && (enemy?e.EnemyAccuracy:e.Accuracy)==100,"penalties end on affected actor's turn");
        }
        foreach(string id in new[]{"weapon_glove","weapon_rapier","weapon_dagger","aya_q","nicky_r","daniel_r","luke_e","magnus_e","shoichi_w","camilo_e","alonso_e","tsubame_r","istvan_r"})
        {
            e=MechanicsFixture();c=e.State.combat;c.enemyEvasion=100;
            StatusMechanics.Add(c.playerStatuses,"blind","weapon_camera");
            c.hand.Add(id);int hp=c.enemyHp;
            Check(e.PlayCard(0),"targeted cast is playable: "+id);
            var action=e.CombatActions.Last(x=>x.cardId==id && x.kind==null);
            Check(c.enemyHp<hp && action.avoided==0 && action.missed==0,"targeted direct cast ignores accuracy penalty and total evasion: "+id);
            Check(GameDatabase.Card(id).description.Contains("필중") && e.PreviewCardUse(id).guaranteedHit,"targeting is exposed in details and forecast: "+id);
        }
        foreach(string id in new[]{"basic_attack","nia_q","aya_w","weapon_camera","weapon_hammer","weapon_bat","weapon_spear","weapon_guitar","weapon_crossbow","garnet_e","abigail_r"})
            Check(!AccuracyMechanics.GuaranteedHit(GameDatabase.Card(id)),"directional, area and ordinary basic attacks are not made targeted: "+id);
        var bike=GameDatabase.Card("silvia_e");var actor=new SkillActorState();
        Check(!AccuracyMechanics.GuaranteedHit(bike,actor),"Silvia cone attack has ordinary hit checks outside bike stance");
        RecastResource(actor,bike.id,"bike",1);
        Check(AccuracyMechanics.GuaranteedHit(bike,actor),"Silvia's unit-targeted bike wheelie is guaranteed");
        Check(GameDatabase.Card("weapon_camera").statuses.Any(x=>x.key=="blind") && GameDatabase.Card("sua_w").statuses.Any(x=>x.key=="blind") && GameDatabase.Card("daniel_w").statuses.Any(x=>x.key=="blind"),"reviewed camera flash, bird blindness and inspiration vision loss reduce accuracy");
    }

    static void AccuracyMissAndEvasionHooks()
    {
        var e=MechanicsFixture();var c=e.State.combat;
        e.State.hp=e.State.maxHp=100000;c.enemyHp=c.enemyMaxHp=100000;
        StatusMechanics.Add(c.playerStatuses,"blind","weapon_camera",2);
        c.enemyEvasion=50;
        int hit=0,miss=0,evade=0;
        for(int i=0;i<2000;i++)
        {
            c.energy=99;c.hand.Add("nia_q");e.PlayCard(0);
            var action=e.CombatActions.Last(x=>x.cardId=="nia_q" && x.kind==null);
            hit+=action.damage>0?1:0;miss+=action.missed;evade+=action.avoided;
            Check(action.missed+action.avoided<=1,"a single hit is never counted as both accuracy failure and evasion");
            e.CombatActions.Clear();
        }
        Check(miss>400 && miss<600,"blind produces about 25 percent accuracy misses independently");
        Check(hit>650 && hit<850 && evade>650 && evade<850,"75 accuracy and 50 evasion resolve multiplicatively, not as 75 percent combined avoidance");
        Check(e.State.combatHistory.Any(x=>x.missed>0 && x.detail.Contains("명중 실패")),"accuracy miss is recorded separately in structured history");

        e=MechanicsFixture();c=e.State.combat;c.enemyEvasion=0;
        // A synthetic enemy evade hook proves accuracy failures do not award evasion events.
        var original=GameDatabase.Passive("aya_p").mechanics;
        try
        {
            GameDatabase.Passive("aya_p").mechanics=new TraitMechanicProfile {rules=new[]{new TraitRule {trigger="evade",op="block",amount=5}}};
            c.enemyId="aya";c.enemyTraits=new TraitActorState();
            StatusMechanics.Add(c.playerStatuses,"blind","weapon_camera");
            bool found=false;
            for(int seed=1;seed<100 && !found;seed++)
            {
                e.State.rngState=seed;c.energy=99;c.hand.Add("nia_q");e.PlayCard(0);
                found=e.CombatActions.Last(x=>x.cardId=="nia_q" && x.kind==null).missed>0;
            }
            Check(found && c.enemyBlock==0,"accuracy failure does not trigger defender evade passives");
            c.playerStatuses.effects.Clear();c.enemyEvasion=100;c.energy=99;c.hand.Add("basic_attack");e.PlayCard(0);
            Check(c.enemyBlock==5,"actual evasion still triggers defender evade passives");
        }
        finally{GameDatabase.Passive("aya_p").mechanics=original;}
    }

    static void AccuracyEnemyAndPersistence()
    {
        foreach(string id in new[]{"weapon_glove","weapon_rapier","aya_q"})
        {
            var e=MechanicsFixture();var c=e.State.combat;c.evasion=65;
            StatusMechanics.Add(c.enemyStatuses,"blind","weapon_camera");
            AccuracyEnemyCast(e,id);
            var action=e.CombatActions.Last(x=>x.cardId==id && x.kind==null);
            Check(action.damage>0 && action.missed==0 && action.avoided==0,"enemy targeted attack bypasses its blind and player evasion: "+id);
        }
        var source=MechanicsFixture();var combat=source.State.combat;combat.evasion=50;combat.enemyEvasion=50;
        StatusMechanics.Add(combat.playerStatuses,"blind","weapon_camera");StatusMechanics.Add(combat.enemyStatuses,"root","isol_q");
        combat.hand.Add("nia_q");var restored=new GameEngine((RunState)Clone(source.State));
        string before=InformationFingerprint(source.State);
        var p=source.PreviewCardUse(0);
        Check(p.accuracyChance==75 && p.evasionChance==30 && !p.guaranteedHit && InformationFingerprint(source.State)==before,"accuracy forecast is correct and does not mutate state or consume RNG");
        Check(source.PlayCard(0) && restored.PlayCard(0),"source and saved states play the next accuracy-checked card");
        Check(InformationFingerprint(source.State)==InformationFingerprint(restored.State),"existing status saves preserve accuracy, control and random replay determinism");
        bool missed=false;
        for(int seed=1;seed<100 && !missed;seed++)
        {
            var e=MechanicsFixture();StatusMechanics.Add(e.State.combat.enemyStatuses,"blind","weapon_camera");e.State.rngState=seed;
            AccuracyEnemyCast(e,"nia_q");var action=e.CombatActions.Last(x=>x.cardId=="nia_q" && x.kind==null);
            missed=action.missed>0;if(missed)Check(action.avoided==0 && action.damage==0,"enemy accuracy failures are distinct from player evasion");
        }
        Check(missed,"enemy blind participates in the accuracy system");
    }

    static void AccuracyEnemyCast(GameEngine e,string id)
    {
        var c=e.State.combat;c.enemyTurn=true;c.enemyAvailableEnergy=99;c.enemyActionIndex=0;
        c.enemyPlan=new List<string>{id};c.enemyPlanCosts=new List<int>{e.CardCost(id)};c.enemyPlanFreeCast=new List<bool>{false};
        Check(e.AdvanceEnemyAction(),"enemy fixture resolves card: "+id);
    }

    static void AccuracySpecialAttacksAndDefense()
    {
        foreach(bool enemy in new[]{false,true})
        {
            var e=MechanicsFixture();var c=e.State.combat;int hp=enemy?e.State.hp:c.enemyHp;
            if(enemy){c.block=1000;StatusMechanics.Add(c.enemyStatuses,"blind","weapon_camera");AccuracyEnemyCast(e,"weapon_glove");}
            else{c.enemyBlock=1000;c.enemyEvasion=100;StatusMechanics.Add(c.playerStatuses,"blind","weapon_camera");c.hand.Add("weapon_glove");e.PlayCard(0);}
            var action=e.CombatActions.Last(x=>x.cardId=="weapon_glove" && x.kind==null);
            Check(action.damage==0 && action.absorbed>0 && action.missed==0 && action.avoided==0 && (enemy?e.State.hp:c.enemyHp)==hp,"guaranteed hit keeps ordinary block absorption for "+(enemy?"enemy":"player"));
        }
        bool wildlifeMiss=false,bossMiss=false;
        var wildlife=typeof(GameEngine).GetMethod("ResolveWildlifeSkill",BindingFlags.NonPublic|BindingFlags.Instance);
        var boss=typeof(GameEngine).GetMethod("ResolveBossStrike",BindingFlags.NonPublic|BindingFlags.Instance);
        for(int seed=1;seed<=100 && (!wildlifeMiss || !bossMiss);seed++)
        {
            if(!wildlifeMiss)
            {
                var e=MechanicsFixture();var c=e.State.combat;c.animal="boar";e.State.rngState=seed;
                StatusMechanics.Add(c.enemyStatuses,"blind","weapon_camera");
                wildlife.Invoke(e,new object[]{WildlifeMechanics.BoarCharge});
                var action=e.CombatActions.Last(x=>x.kind=="wildlife_skill");wildlifeMiss=action.missed>0;
                if(wildlifeMiss)Check(action.damage==0 && action.avoided==0 && !StatusMechanics.Has(c.playerStatuses,"knockback"),"wildlife blind miss neither damages nor applies on-hit crowd control nor counts as evasion");
            }
            if(!bossMiss)
            {
                var e=MechanicsFixture();e.State.rngState=seed;StatusMechanics.Add(e.State.combat.enemyStatuses,"blind","weapon_camera");
                boss.Invoke(e,new object[]{10,"boss_accuracy_test","daniel_r",true});
                var action=e.CombatActions.Last(x=>x.kind=="boss_accuracy_test");bossMiss=action.missed>0;
                if(bossMiss)Check(action.damage==0 && action.avoided==0,"separate boss special can miss and does not borrow guaranteed hit from its source card");
            }
        }
        Check(wildlifeMiss && bossMiss,"wildlife and evadable boss special both use enemy accuracy");
        var fixedStrike=MechanicsFixture();StatusMechanics.Add(fixedStrike.State.combat.enemyStatuses,"blind","weapon_camera");fixedStrike.State.combat.evasion=65;
        boss.Invoke(fixedStrike,new object[]{10,"boss_accuracy_test","daniel_r",false});
        var fixedAction=fixedStrike.CombatActions.Last(x=>x.kind=="boss_accuracy_test");
        Check(fixedAction.damage==10 && fixedAction.missed==0 && fixedAction.avoided==0,"existing unavoidable boss effects keep their damage behavior");
    }
}
#endif
