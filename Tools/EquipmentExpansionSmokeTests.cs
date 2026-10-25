#if LUMIA_TEST_RUNNER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lumia;

public static partial class CoreSmokeTests
{
    private static void ExpandedEquipmentCoverageAndCraft()
    {
        string[] newIds={"intervention","joyeuse","quad_eye","mithril_armband","racing_boots","dice_of_destiny"};
        Check(GameDatabase.Gear.Count==57 && newIds.All(id=>GameDatabase.Equipment(id)!=null),"six original items expand equipment from 51 to 57");
        string[] families=GameDatabase.Characters.SelectMany(x=>x.weaponClasses).Distinct().ToArray();
        Check(families.Length==23,"complete supported weapon-family fixture");
        foreach(string family in families)
        {
            var item=GameDatabase.Gear.FirstOrDefault(g=>g.slot==GearSlot.Weapon && g.weaponClass==family);
            Check(item!=null && new[]{"전설","초월"}.Contains(item.rarity),"every weapon family has a craftable high-grade weapon: "+family);
            Check(GameDatabase.Card(WeaponIdentity.CardFor(family))?.key=="D","every weapon family maps to a real D skill: "+family);
            var e=Ready(111);e.State.passives.Clear();Enter(e,ZoneKind.Campfire);e.ChooseCamp(true);e.State.objects.Add(item.objectId);
            Check(e.Craft(item.id) && e.State.weaponGrantedCards.SequenceEqual(new[]{WeaponIdentity.CardFor(family)}),"crafting grants the matching D card exactly once: "+family);
            var loaded=new GameEngine((RunState)Clone(e.State));
            Check(loaded.State.deck.SequenceEqual(e.State.deck) && loaded.State.weaponGrantedCards.SequenceEqual(e.State.weaponGrantedCards),"weapon and D grant persist without duplicates: "+family);
        }
        foreach(string id in newIds)
        {
            var item=GameDatabase.Equipment(id);var e=Ready(121);Enter(e,ZoneKind.Campfire);e.ChooseCamp(true);e.State.objects.Add(item.objectId);
            Check(e.Craft(id) && e.State.gear.Contains(id) && e.State.campActions==2 && !e.State.objects.Contains(item.objectId),"new item uses one original-type object and one craft action: "+id);
            Check(item.critChance>=4 && item.critChance<=6 && item.optionTags.Contains("치명타") && item.optionTags.Contains("치명타 확률"),"new critical chances stay modest and searchable: "+id);
        }
        foreach(string id in new[]{"intervention","joyeuse"})
        {
            var item=GameDatabase.Equipment(id);var cards=WeaponIdentity.EquipmentCards(new[]{id},true);
            Check(cards.Count==2 && cards[0]==item.cardId && cards[1]!=cards[0] && GameDatabase.Card(cards[1])?.key=="D","new weapons retain Alex's different-class extra D: "+id);
        }
        Check(GameDatabase.Gear.Count(g=>g.critChance>0)==13 && GameDatabase.Gear.Count(g=>g.critDamageBonus>0)==4,"expanded chance and critical-damage choices are both available");
        foreach(var item in GameDatabase.Gear)
        {
            Check(item.optionTags.Contains("치명타 피해")== (item.critDamageBonus>0),"damage tag agrees with actual equipment stat: "+item.id);
            Check(item.optionTags.Contains("치명타 확률")== (item.critChance>0),"chance tag agrees with actual equipment stat: "+item.id);
            if(item.critDamageBonus>0)Check(DescriptionSummary.Gear(item).Contains(item.critDamageBonus+"%p"),"critical-damage option displays exact additive percentage points: "+item.id);
        }
        EquipmentStackingScopes();
    }

    private static void EquipmentStackingScopes()
    {
        var e=MechanicsFixture();var c=e.State.combat;
        e.State.gear.AddRange(new[]{"fragarach","fragarach","mithril_helm"});
        Check(e.TotalAttack==6 && e.TotalBlock==1,"identical equipment copies add their base stats");
        c.hand.Add("tactical_blink");
        Check(e.PlayCard(0),"a real movement card activates equipped Footwork effects");
        var resources=c.playerTraits.skills;
        Check(SkillMechanics.Resource(resources,"trait:gear:fragarach","stride")==1
            && SkillMechanics.Resource(resources,"trait:gear:mithril_helm","stride")==1
            && resources.resources.Count(x=>x.key=="stride")==2,
            "duplicate gear has one trigger source, while different gear with the same named Footwork effect owns separate resources");
        resources.resources.Single(x=>x.owner=="trait:gear:fragarach" && x.key=="stride").amount=3;
        c.hand.Add("basic_attack");int hp=c.enemyHp;
        Check(e.PlayCard(0) && hp-c.enemyHp==GameDatabase.Card("basic_attack").damage+e.TotalAttack+4,
            "different equipment with the same Footwork effect each adds its bonus once; identical copies do not multiply it");
        Check(StatusMechanics.Has(c.enemyStatuses,"slow")
            && SkillMechanics.Resource(resources,"trait:gear:fragarach","stride")==0
            && SkillMechanics.Resource(resources,"trait:gear:mithril_helm","stride")==0,
            "each equipment source checks its own Footwork threshold and consumes only its own resource");

        e=MechanicsFixture();c=e.State.combat;e.State.gear.AddRange(new[]{"skadi","eternal_frost","empress"});
        c.hand.Add("nia_q");Check(e.PlayCard(0),"a real skill hit activates three different coldwave equipment sources");
        Check(e.CombatActions.Count(x=>x.kind=="status_slow" && x.traitId!=null && x.traitId.StartsWith("gear:"))==3
            && c.enemyStatuses.effects.Count(x=>x.kind=="slow" && !x.pending)==1
            && c.enemyStatuses.effects.Single(x=>x.kind=="slow" && !x.pending).remaining==1,
            "same named coldwave effects trigger separately, but their shared slow status never multiplies its duration");
    }

    private static void EquipmentCriticalDamageAndPersistence()
    {
        var bare=MechanicsFixture();
        Check(bare.CriticalDamagePercent==150 && bare.EnemyCriticalDamagePercent==150 && bare.GearCriticalDamage(7)==10 && bare.GearCriticalDamage(-1)==0,"unmodified critical multiplier and floor rounding remain 150 percent");
        Check(bare.GearCriticalDamage(int.MaxValue)==int.MaxValue,"critical arithmetic saturates rather than overflowing large damage");
        var e=MechanicsFixture();e.State.mode=RunMode.Endless;var c=e.State.combat;
        e.State.gear=Enumerable.Repeat("light_insignia",20).Concat(new[]{"joyeuse","dice_of_destiny"}).ToList();
        Check(e.CritChance==100 && e.CriticalDamagePercent==243,"endless expanded slots can accumulate chance to100 and critical-damage points above150");
        c.hand.Add("basic_attack");int normal=e.CardDamage("basic_attack"),before=c.enemyHp;
        Check(e.PlayCard(0) && before-c.enemyHp==normal*243/100 && e.CombatActions.Last(x=>x.cardId=="basic_attack" && x.kind==null).critical,"actual player critical basic uses accumulated equipment damage before integer rounding");
        var saved=new GameEngine((RunState)Clone(e.State));
        Check(saved.CriticalDamagePercent==243 && saved.CritChance==100 && saved.State.rngState==e.State.rngState,"critical build survives save and continue without new migration fields or RNG rolls");
        foreach(RunMode mode in new[]{RunMode.Normal,RunMode.Hard})
        {
            saved.State.mode=mode;
            Check(saved.CritChance==30 && saved.CriticalDamagePercent==243,"finite-mode chance ceiling stays30 while damage uses the equipped stats: "+mode);
        }
        foreach(string id in new[]{"nia_q","weapon_dagger","tactical_plasma"})
        {
            var skill=MechanicsFixture();skill.State.mode=RunMode.Endless;skill.State.gear=Enumerable.Repeat("light_insignia",20).ToList();
            skill.State.combat.hand.Add(id);int predicted=skill.CardTotalDamage(id),hp=skill.State.combat.enemyHp;
            Check(skill.PlayCard(0) && hp-skill.State.combat.enemyHp==predicted && !skill.CombatActions.First(x=>x.cardId==id && x.kind==null).critical,"critical-damage equipment cannot multiply QWER, D or F: "+id);
        }
        var enemy=MechanicsFixture();enemy.State.mode=RunMode.Endless;enemy.State.hp=enemy.State.maxHp=5000;var ec=enemy.State.combat;
        ec.enemyGear=Enumerable.Repeat("light_insignia",20).Concat(new[]{"dice_of_destiny"}).ToList();
        ec.enemyPlan=new List<string>{"basic_attack"};ec.enemyPlanCosts=new List<int>{1};ec.enemyPlanFreeCast=new List<bool>{false};
        int raw=(int)typeof(GameEngine).GetMethod("EnemyCardDamage",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(enemy,new object[]{GameDatabase.Card("basic_attack")});
        int expected=RunModes.ScaleEnemyDirectDamage(enemy.State,raw*238/100);
        Check(enemy.EnemyCriticalDamagePercent==238 && enemy.EnemyCritChance==100 && enemy.BeginEndTurn(),"enemy equipment uses its own multiplier and normal turn flow");
        before=enemy.State.hp;var restored=new GameEngine((RunState)Clone(enemy.State));
        Check(enemy.AdvanceEnemyAction() && restored.AdvanceEnemyAction() && before-enemy.State.hp==expected && restored.State.hp==enemy.State.hp && restored.State.rngState==enemy.State.rngState,"enemy critical damage and mode scaling are deterministic after save and continue");
        ec.animal="wolf";
        Check(enemy.EnemyCritChance==0 && enemy.EnemyCriticalDamagePercent==150,"wildlife never acquires the subject equipment critical build");
        var legacy=(RunState)Clone(bare.State);legacy.version=1;legacy.stage=RunStage.Map;legacy.combat=null;
        legacy.gear=new List<string>{"meteor_sword"};legacy.deck=new List<string>{"basic_attack","weapon_rapier"};
        legacy.weaponGrantVersion=0;legacy.weaponGrantedCards=null;
        var old=new GameEngine(legacy);
        Check(old.CriticalDamagePercent==156 && old.State.gear.Contains("meteor_sword") && old.State.weaponGrantedCards.Contains("weapon_rapier"),"old save resolves revised original item stats and keeps its matching weapon skill");
        var profile=BossProfiles.Get("nia");var originalPhase=profile.phases[0];
        try
        {
            var restricted=RestrictionFixture(BossGoal.NoHeavyHit,24);restricted.State.mode=RunMode.Endless;
            restricted.State.gear=new List<string>{"dice_of_destiny"};var rc=restricted.State.combat;
            rc.strength=15-restricted.CardDamage("basic_attack");rc.hand.Add("basic_attack");
            Check(restricted.CardDamage("basic_attack")==15 && restricted.GearCriticalDamage(15)==23 && !restricted.PreviewCardUse(0).bossPossibleViolation,"critical boss warning respects the rounded damage immediately below its threshold");
            restricted.State.gear.Add("joyeuse");
            rc.strength-=GameDatabase.Equipment("joyeuse").attack;
            Check(restricted.CardDamage("basic_attack")==15 && restricted.GearCriticalDamage(15)==24 && restricted.PreviewCardUse(0).bossPossibleViolation,"critical boss warning includes current equipment damage bonus across the threshold");
        }
        finally{profile.phases[0]=originalPhase;}
        var negative=GameDatabase.Equipment("joyeuse");int original=negative.critDamageBonus;
        try{negative.critDamageBonus=-200;bare.State.gear.Add(negative.id);Check(bare.CriticalDamagePercent==150,"negative critical bonus cannot reduce the baseline multiplier");}
        finally{negative.critDamageBonus=original;}
    }
}
#endif
