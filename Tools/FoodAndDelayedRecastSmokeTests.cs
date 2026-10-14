#if LUMIA_TEST_RUNNER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lumia;

public static partial class CoreSmokeTests
{
    static void FoodEnergyCostsAndGuards()
    {
        foreach(var food in GameDatabase.Foods)
        {
            var e=MechanicsFixture();var c=e.State.combat;e.State.hp=1;e.State.foods=new List<string>{food.id};
            int cost=food.id=="soup"?2:1;c.energy=cost-1;
            Check(e.FoodUseCost(food.id)==cost && !e.CanUseFood(0) && !e.UseFood(0) && c.energy==cost-1 && e.State.hp==1 && e.State.foods.Count==1,"food rejects insufficient energy atomically: "+food.id);
            c.energy=cost;c.enemyTurn=true;
            Check(!e.CanUseFood(0)&&!e.UseFood(0)&&c.energy==cost&&e.State.foods.Count==1,"food cannot interrupt enemy actions: "+food.id);
            c.enemyTurn=false;e.State.hp=e.State.maxHp;
            Check(!e.UseFood(0)&&c.energy==cost&&e.State.foods.Count==1,"full health never wastes food or energy: "+food.id);
            e.State.hp=1;Check(e.CanUseFood(0)&&e.UseFood(0)&&c.energy==0&&e.State.foods.Count==0&&e.State.hp>1,"food pays exactly its combat cost and heals: "+food.id);
            Check(e.State.combatHistory.Any(x=>x.kind=="food"&&x.detail.Contains("코스트 "+cost)),"combat history discloses the spent food energy: "+food.id);
            e.State.foods.Add(food.id);e.State.hp=1;e.State.stage=RunStage.Map;c.energy=0;c.enemyTurn=true;
            Check(e.CanUseFood(0)&&e.UseFood(0)&&c.energy==0&&e.State.hp>1,"field eating never uses stale combat energy or enemy turn state: "+food.id);
            Check(food.description.Contains("코스트 "+cost),"food description matches actual combat price: "+food.id);
        }
        var invalid=MechanicsFixture();invalid.State.hp=1;
        Check(!invalid.CanUseFood(-1)&&!invalid.UseFood(99)&&invalid.FoodUseCost("missing")==0,"invalid food requests do not charge energy");
        invalid.State.foods.Add("meat");invalid.State.stage=RunStage.Preparation;
        Check(!invalid.CanUseFood(0)&&!invalid.UseFood(0),"preparation cannot consume food");
    }

    static void NextPlayerTurnFixture(GameEngine e)
    {
        Check(e.BeginEndTurn(),"scheduled recast advances through the real player turn end");
        int guard=0;while(e.State.combat.enemyTurn&&guard++<100)e.AdvanceEnemyAction();
        Check(guard<100&&e.State.stage==RunStage.Combat,"scheduled recast reaches the next player turn");
    }

    static void DelayedRecastTurnsAndPersistence()
    {
        var delayed=new[]{"vanya_q","bianca_w","chloe_e","magnus_w"};
        Check(GameDatabase.Cards.Where(x=>x.freeCastNextTurn).Select(x=>x.id).OrderBy(x=>x).SequenceEqual(delayed.OrderBy(x=>x)),"only source-reviewed timed resets are classified as next-turn grants");
        foreach(string source in delayed)
        {
            var card=GameDatabase.Card(source);string target=card.freeCastTargets[0];
            var e=RecastFixture(source,target,source=="bianca_w"?"blood":null,2);var c=e.State.combat;
            e.State.hp=e.State.maxHp=999;c.enemyHp=c.enemyMaxHp=9999;
            Check(e.PlayCard(0)&&!RecastAvailable(c,target)&&c.pendingFreeCasts.Count==1&&c.pendingFreeCasts[0].readyTurn==c.turn+1,"timed reset opens a saved future permission without an immediate free action: "+source);
            Check(e.EffectiveCardCost(target)>0,"target retains its paid cost before the scheduled turn: "+source);
            string before=InformationFingerprint(e.State);var p=e.PreviewCardUse(source);
            Check(p.conditions.Any(x=>x.scheduled&&x.effect.Contains("다음 내 턴"))&&InformationFingerprint(e.State)==before,"read-only use preview clearly marks delayed free reset: "+source);
            Check(DescriptionSummary.CardPreview(card).Contains("다음 내 턴")&&CardPresentation.Describe(card,GameDatabase.Cards).Contains("다음 내 턴"),"compact and full card text disclose next-turn timing: "+source);
            c.hand.Add(source);c.energy=99;e.State.deck.Add(source);e.PlayCard(c.hand.Count-1);
            Check(c.pendingFreeCasts.Count==1,"duplicates cannot queue an extra reset from the same source in one turn: "+source);
            var loaded=new GameEngine((RunState)Clone(e.State));
            Check(InformationFingerprint(loaded.State.combat.pendingFreeCasts)==InformationFingerprint(c.pendingFreeCasts),"future permission survives reloading before turn end: "+source);
            e=loaded;c=e.State.combat;int total=PileCount(c);NextPlayerTurnFixture(e);c.energy=0;
            Check(c.pendingFreeCasts.Count==0&&RecastAvailable(c,target)&&c.hand.Contains(target)&&e.CanPlayCard(c.hand.IndexOf(target))&&PileCount(c)==total,"next turn recalls a finite owned copy and permits a zero-energy use: "+source);
            Check(e.PlayCard(c.hand.IndexOf(target))&&!RecastAvailable(c,target)&&c.pendingFreeCasts.Count==0,"scheduled free cast is finite and cannot schedule itself again: "+source);
        }
        var expired=RecastFixture("vanya_q","vanya_q");expired.PlayCard(0);NextPlayerTurnFixture(expired);
        Check(RecastAvailable(expired.State.combat,"vanya_q"),"unused scheduled reset exists during its due turn");
        NextPlayerTurnFixture(expired);Check(!RecastAvailable(expired.State.combat,"vanya_q")&&expired.State.combat.pendingFreeCasts.Count==0,"unused scheduled reset expires instead of rolling into a third turn");
        var missed=RecastFixture("vanya_q","vanya_q",hit:false);missed.PlayCard(0);
        Check(missed.State.combat.pendingFreeCasts.Count==0,"a fully missed source never reserves a delayed grant");
        var gone=RecastFixture("chloe_e","chloe_q");gone.PlayCard(0);var gc=gone.State.combat;gc.discardPile.Remove("chloe_q");gc.exhaustPile.Add("chloe_q");
        NextPlayerTurnFixture(gone);Check(!RecastAvailable(gc,"chloe_q")&&!gc.hand.Contains("chloe_q")&&gc.exhaustPile.Contains("chloe_q"),"next-turn recalls never resurrect an exhausted target");
        var legacy=(RunState)Clone(gone.State);legacy.combat.pendingFreeCasts=null;legacy.combat.enemyPendingFreeCasts=null;
        var migrated=new GameEngine(legacy);Check(migrated.State.combat.pendingFreeCasts!=null&&migrated.State.combat.enemyPendingFreeCasts!=null,"older saves without queue fields continue with empty queues");
        foreach(string id in new[]{"jackie_q","yuki_w","yuki_e","hyejin_q","rozzi_q","rozzi_w","cathy_q","garnet_r","johann_w"})
            Check(!GameDatabase.Card(id).freeCastNextTurn,"instant hit/reset conditions remain immediate: "+id);
    }

    static void DelayedRecastEnemyParity()
    {
        var e=MechanicsFixture();var c=e.State.combat;e.State.hp=e.State.maxHp=9999;c.enemyHp=c.enemyMaxHp=9999;
        c.enemyDeck.Add("magnus_w");c.enemyDrawPile.Add("magnus_w");
        typeof(GameEngine).GetMethod("PlanEnemyTurn",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(e,null);
        Check(c.enemyPlan.SequenceEqual(new[]{"magnus_w"})&&!c.enemyPlanFreeCast.Any(x=>x)&&c.enemyPendingFreeCasts.Count==0,"enemy planning neither instantly chains nor mutates the actual delayed queue");
        e.BeginEndTurn();e.AdvanceEnemyAction();
        Check(c.enemyPendingFreeCasts.Count==1&&c.enemyPendingFreeCasts[0].readyTurn==c.turn+1&&!c.enemyFreeCasts.Any(x=>x.uses>0),"enemy actual hit schedules one free recast for its next turn");
        e=new GameEngine((RunState)Clone(e.State));c=e.State.combat;
        while(c.enemyTurn)e.AdvanceEnemyAction();
        Check(c.enemyPlan.SequenceEqual(new[]{"magnus_w"})&&c.enemyPlanCosts.SequenceEqual(new[]{0})&&c.enemyPlanFreeCast.All(x=>x)&&c.enemyPendingFreeCasts.Count==1,"next enemy forecast uses the queued free copy without consuming its saved permission early");
        string before=InformationFingerprint(e.State);var preview=e.PreviewCardUse("magnus_w",true);
        typeof(GameEngine).GetMethod("RefreshEnemyIntent",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(e,null);
        Check(before==InformationFingerprint(e.State)&&preview.conditions.Any(x=>x.scheduled),"repeated queued enemy forecasts are read-only and explain delayed conditions");
        int forecast=c.intentDamage;Check(e.BeginEndTurn()&&c.enemyPendingFreeCasts.Count==0&&c.enemyFreeCasts.Sum(x=>x.uses)==1,"queued enemy permission is consumed once at actual enemy turn start");
        c.enemyAvailableEnergy=0;e.AdvanceEnemyAction();
        Check(c.enemyAvailableEnergy==0&&c.enemyPendingFreeCasts.Count==0&&c.enemyFreeCasts.Sum(x=>x.uses)==0&&e.CombatActions.Last(x=>x.enemy&&x.cardId=="magnus_w").damage<=forecast,"enemy spends the earned copy at zero energy and cannot requeue it forever");
        var missed=MechanicsFixture();var mc=missed.State.combat;mc.enemyDeck.Add("vanya_q");mc.enemyDrawPile.Add("vanya_q");mc.evasion=100;mc.evasionTurns=2;missed.State.rngState=2;
        typeof(GameEngine).GetMethod("PlanEnemyTurn",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(missed,null);missed.BeginEndTurn();missed.AdvanceEnemyAction();
        Check(mc.enemyPendingFreeCasts.Count==0,"missed enemy source cannot gain a speculative scheduled recast");
    }
}
#endif
