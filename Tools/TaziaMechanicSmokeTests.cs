#if LUMIA_TEST_RUNNER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lumia;

// Authored gameplay regressions. The existing core fixture supplies a neutral
// real-engine battle; expected resource transitions are stated independently.
public static class TaziaMechanicSmokeTests
{
    static int checks;
    static string Owner => GameDatabase.Card("tazia_q").owner;
    static readonly BindingFlags HiddenStatic = BindingFlags.NonPublic | BindingFlags.Static;
    static readonly BindingFlags HiddenInstance = BindingFlags.NonPublic | BindingFlags.Instance;

    public static int Run()
    {
        checks = 0;
        ECompletesThisCast();
        SwordAndUltimate();
        PassiveEffectsAndSharedGlass();
        ActualPlayerCombo();
        LegacyResourcesAndIsolation();
        PreviewsAndEnemyCombo();
        ForgeConditionPreviews();
        DescriptionsAndOrdinarySnapshotRules();
        BossSwordAndFieldSource();
        ResearchSwordCompletion();
        Console.WriteLine("Tazia mechanics: PASS " + checks + " assertions.");
        return checks;
    }

    static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new Exception("Tazia: " + message);
    }

    static GameEngine Fixture(string passive = "tazia_p", string enemy = "fixture") =>
        (GameEngine)typeof(CoreSmokeTests).GetMethod("MechanicsFixture", HiddenStatic)
            .Invoke(null, new object[] { passive, enemy });
    static RunState Copy(RunState state) => (RunState)typeof(CoreSmokeTests).GetMethod("Clone", HiddenStatic)
        .Invoke(null, new object[] { state });
    static string Snapshot(object value) => (string)typeof(CoreSmokeTests).GetMethod("FieldSnapshot", HiddenStatic)
        .Invoke(null, new object[] { value });
    static void Invoke(GameEngine engine, string method) => typeof(GameEngine).GetMethod(method, HiddenInstance)
        .Invoke(engine, null);
    static int Resource(SkillActorState actor, string key) => SkillMechanics.Resource(actor, Owner, key);
    static void Put(SkillActorState actor, string key, int amount, string owner = null, int cap = 4)
    {
        actor.resources.Add(new SkillResource { owner=owner ?? Owner, key=key,
            label=key == "shard" ? "유리 파편" : key == "collection" ? "파편 수집" : "유리 검", amount=amount, cap=key == "sword" ? 1 : cap });
    }
    static void Cast(GameEngine engine, string id)
    {
        var c=engine.State.combat;
        c.energy=99; c.hand.Clear(); c.hand.Add(id);
        Check(engine.PlayCard(0), "real player cast succeeds: " + id);
    }
    static TraitSource Passive()
    {
        var passive=GameDatabase.Passive("tazia_p");
        return new TraitSource { id=passive.id, name=passive.name, owner=passive.owner, profile=passive.mechanics };
    }
    static TraitResult Trigger(TraitActorState traits, SkillActorState skills, string trigger, string card="tazia_q", bool landed=true) =>
        TraitMechanics.Resolve(traits, new[] { Passive() }, trigger, new TraitContext {
            card=GameDatabase.Card(card), landed=landed, skillActor=skills, hp=100, maxHp=100, targetHp=999
        });

    static void ECompletesThisCast()
    {
        foreach (string variant in new[] { "base", "power", "rare" })
        foreach (bool landed in new[] { false, true })
        foreach (int shards in Enumerable.Range(0,5))
        foreach (int collection in Enumerable.Range(0,5))
        foreach (int oldSword in new[] { 0, 1 })
        {
            var card=variant == "base" ? GameDatabase.Card("tazia_e") : StrategyMechanics.Apply(GameDatabase.Card("tazia_e"),variant);
            var actor=new SkillActorState();
            Put(actor,"shard",shards); Put(actor,"collection",collection); Put(actor,"sword",oldSword);
            var before=SkillMechanics.Clone(actor); string previous=Snapshot(before);
            int gathered=Math.Min(4,collection+shards+(variant == "rare" && shards>0 ? 1 : 0));
            SkillMechanics.AfterCard(actor,before,card,landed);
            string label=variant+"/hit="+landed+"/shard="+shards+"/collection="+collection+"/sword="+oldSword;
            Check(Resource(actor,"shard")==0,"E consumes this cast's available shards: "+label);
            Check(Resource(actor,"collection")== (gathered>=3 ? 0 : gathered),"E consumes collection immediately at three: "+label);
            Check(Resource(actor,"sword")== (gathered>=3 ? 1 : oldSword),"E completes a single sword in this cast: "+label);
            Check(Snapshot(before)==previous,"E keeps its pre-card snapshot immutable: "+label);
            Check(actor.resources.Count(x=>x.owner==Owner && x.key=="sword")==1,"repeated forging cannot duplicate sword tokens: "+label);
        }
        var plain=new SkillActorState(); Put(plain,"shard",3);
        var bonus=SkillMechanics.Bonuses(plain,GameDatabase.Card("tazia_e"));
        Check(bonus.damage==9 && bonus.block==6,"E damage and block still scale from shards before collecting");
    }

    static void SwordAndUltimate()
    {
        foreach (bool landed in new[] { false, true })
        {
            var actor=new SkillActorState(); Put(actor,"sword",1);
            var q=GameDatabase.Card("tazia_q"); var before=SkillMechanics.Clone(actor);
            Check(SkillMechanics.Bonuses(before,q,landed).damage==8,"a prepared sword adds Q's existing eight damage");
            SkillMechanics.AfterCard(actor,before,q,landed);
            Check(Resource(actor,"sword")==0,"Q consumes the sword even when the direct attack misses");
            Check(Resource(actor,"shard")== (landed ? 1 : 0),"Q only leaves its shard on a hit");
        }
        var ultimate=new SkillActorState(); Put(ultimate,"shard",2); Put(ultimate,"sword",1); Put(ultimate,"collection",2);
        var r=GameDatabase.Card("tazia_r"); var snapshot=SkillMechanics.Clone(ultimate);
        Check(SkillMechanics.Bonuses(snapshot,r).damage==8,"R receives four extra damage per shared shard");
        SkillMechanics.AfterCard(ultimate,snapshot,r,true);
        Check(Resource(ultimate,"shard")==0 && Resource(ultimate,"sword")==1 && Resource(ultimate,"collection")==2,
            "R spends shards while preserving the independent sword and collection");
    }

    static void PassiveEffectsAndSharedGlass()
    {
        var traits=new TraitActorState(); var skills=new SkillActorState();
        for (int i=1;i<=3;i++)
        {
            var before=Trigger(traits,skills,"before_basic","basic_attack");
            Trigger(traits,skills,"after_basic","basic_attack");
            Check(before.bonus.damage==(i==3 ? 3 : 0),"passive keeps its third-basic extra three damage");
            Check(Resource(skills,"shard")== (i==3 ? 1 : 0),"passive's third basic deposits a skill-usable shard");
        }
        Check(!traits.skills.resources.Any(x=>x.owner=="trait:tazia_p" && x.key=="shard"),"shared passive glass has no duplicate trait token");
        Put(skills,"collection",2);
        SkillMechanics.AfterCard(skills,SkillMechanics.Clone(skills),GameDatabase.Card("tazia_e"),true);
        Check(Resource(skills,"sword")==1 && Resource(skills,"collection")==0,
            "a shard generated exclusively by the passive can immediately finish E's sword");
        traits=new TraitActorState(); skills=new SkillActorState(); Put(skills,"shard",2);
        Trigger(traits,skills,"after_skill","nia_q");
        Check(Resource(skills,"shard")==1 && Resource(skills,"collection")==1,"another subject's skill still collects one shard");
        Trigger(traits,skills,"after_skill","nia_q");
        Check(Resource(skills,"shard")==1 && Resource(skills,"collection")==1,"passive collection remains once per own turn");
        TraitMechanics.StartTurn(traits); Trigger(traits,skills,"after_skill","nia_q");
        Check(Resource(skills,"shard")==0 && Resource(skills,"collection")==2,"passive collection refreshes next turn");
        foreach (bool hit in new[] { false, true })
        {
            traits=new TraitActorState(); skills=new SkillActorState(); Put(skills,"collection",4);
            Check(Trigger(traits,skills,"before_skill","nia_q",hit).bonus.damage==5,"four collection retains the existing next-skill five damage");
            Trigger(traits,skills,"after_skill","nia_q",hit);
            Check(Resource(skills,"collection")== (hit ? 0 : 4),"collection's old hit-gated passive consumption stays intact");
        }
    }

    static void ActualPlayerCombo()
    {
        var engine=Fixture(); var c=engine.State.combat;
        Cast(engine,"tazia_q");
        Check(Resource(c.playerSkills,"collection")==1 && Resource(c.playerSkills,"shard")==0,"actual Q's newly created shard is visible to the passive");
        Cast(engine,"tazia_w");
        Check(Resource(c.playerSkills,"collection")==1 && Resource(c.playerSkills,"shard")==2,"same-turn W retains its shards after the passive's one collection use");
        Cast(engine,"tazia_e");
        Check(Resource(c.playerSkills,"collection")==0 && Resource(c.playerSkills,"shard")==0 && Resource(c.playerSkills,"sword")==1,
            "actual same-turn Q W E reaches three and forges immediately");
        Check(engine.SkillStateSnapshot().Count(x=>x.owner==Owner && x.key=="sword")==1,
            "combined engine state displays one sword token");
        int hp=c.enemyHp, forecast=engine.CardTotalDamage("tazia_q"); Cast(engine,"tazia_q");
        Check(Resource(c.playerSkills,"sword")==0 && hp-c.enemyHp==forecast,"actual sword Q matches displayed damage and spends its sword");
        Check(Resource(c.enemySkills,"shard")==0 && Resource(c.enemySkills,"collection")==0 && Resource(c.enemySkills,"sword")==0,
            "player glass never leaks into the opposing actor");
    }

    static void LegacyResourcesAndIsolation()
    {
        var engine=Fixture("tazia_p","tazia"); var c=engine.State.combat;
        foreach (bool enemy in new[] { false, true })
        {
            var skills=enemy ? c.enemySkills : c.playerSkills;
            var legacy=enemy ? c.enemyTraits.skills : c.playerTraits.skills;
            Put(skills,"shard",2); Put(skills,"shard",1,"tazia"); Put(legacy,"shard",3,"trait:tazia_p");
            Put(skills,"collection",2); Put(legacy,"collection",999,"trait:tazia_p");
            Put(skills,"sword",1); Put(legacy,"sword",999,"trait:tazia_p");
            Put(legacy,"blocks",2,"trait:nia_p",3);
        }
        var loaded=new GameEngine(Copy(engine.State));
        foreach (bool enemy in new[] { false, true })
        {
            var skills=enemy ? loaded.State.combat.enemySkills : loaded.State.combat.playerSkills;
            var legacy=enemy ? loaded.State.combat.enemyTraits.skills : loaded.State.combat.playerTraits.skills;
            Check(Resource(skills,"shard")==4 && Resource(skills,"collection")==4 && Resource(skills,"sword")==1,"legacy pools merge once within physical resource caps on both actors");
            Check(skills.resources.Count(x=>x.owner==Owner && new[]{"shard","collection","sword"}.Contains(x.key))==3,
                "legacy aliases and duplicate entries consolidate into one entry per resource");
            Check(!legacy.resources.Any(x=>new[]{Owner,"tazia","trait:tazia_p"}.Contains(x.owner)),"legacy Tazia resources are removed after migration");
            Check(legacy.resources.Any(x=>x.owner=="trait:nia_p" && x.key=="blocks" && x.amount==2),"migration leaves another passive's resources untouched");
            foreach (string key in new[]{"shard","collection","sword"})
                Check(loaded.SkillStateSnapshot(enemy).Count(x=>x.owner==Owner && x.key==key)==1,"migrated combined snapshot has one "+key+" token");
        }
        var reloaded=new GameEngine(Copy(loaded.State));
        Check(Snapshot(reloaded.State.combat.playerSkills)==Snapshot(loaded.State.combat.playerSkills)
            && Snapshot(reloaded.State.combat.enemySkills)==Snapshot(loaded.State.combat.enemySkills),"migration is idempotent across another save/load");
        Check(Resource(c.playerSkills,"shard")==2 && c.playerTraits.skills.resources.Any(x=>x.owner=="trait:tazia_p"),"loading a copied save cannot mutate its source actors");
        foreach (int amount in new[]{-10,999})
        {
            var skills=new SkillActorState(); var legacy=new SkillActorState();
            Put(skills,"shard",amount); skills.resources[0].fraction=7; skills.resources[0].persistent=true; skills.resources[0].isState=true;
            SkillMechanics.ShareTaziaResources(skills,legacy);
            var shard=skills.resources.Single();
            Check(shard.amount==(amount<0 ? 0 : 4) && shard.cap==4 && shard.fraction==0 && !shard.persistent && !shard.isState,
                "a standalone malformed skill entry is normalized even without an old trait entry");
        }
    }

    static void PreviewsAndEnemyCombo()
    {
        // Force the shield-break forecast branch as well as ordinary card previews.
        var engine=Fixture("tazia_p","tazia"); var c=engine.State.combat;
        engine.State.hp=engine.State.maxHp=9999; c.enemyHp=c.enemyMaxHp=9999; c.enemyLevel=20; c.block=1;
        SkillMechanics.Ensure(c.playerSkills); SkillMechanics.Ensure(c.enemySkills);
        TraitMechanics.Ensure(c.playerTraits); TraitMechanics.Ensure(c.enemyTraits);
        Put(c.playerSkills,"shard",999); Put(c.playerTraits.skills,"shard",1,"trait:tazia_p");
        Put(c.enemySkills,"shard",1); Put(c.enemyTraits.skills,"collection",1,"trait:tazia_p");
        c.enemyPlan=new List<string>{"basic_attack"}; c.enemyPlanCosts=new List<int>{engine.CardCost("basic_attack")}; c.enemyPlanFreeCast=new List<bool>{false};
        string player=Snapshot(c.playerSkills), enemy=Snapshot(c.enemySkills), playerTraits=Snapshot(c.playerTraits), enemyTraits=Snapshot(c.enemyTraits); int rng=engine.State.rngState;
        for (int i=0;i<5;i++)
        {
            string intent=engine.EnemyIntent; engine.CardTotalDamage("tazia_e"); engine.PreviewCardUse("tazia_q"); engine.SkillStateSnapshot();
        }
        Check(Snapshot(c.playerSkills)==player,"repeated previews including shield break cannot migrate or spend live player glass");
        Check(Snapshot(c.enemySkills)==enemy,"repeated previews cannot migrate or spend live enemy glass");
        Check(Snapshot(c.playerTraits)==playerTraits,"repeated previews cannot migrate or spend live player passive glass/counters");
        Check(Snapshot(c.enemyTraits)==enemyTraits,"repeated previews cannot migrate or spend live enemy passive glass/counters");
        Check(engine.State.rngState==rng,"Tazia previews consume no live RNG");

        engine=Fixture(null,"tazia"); c=engine.State.combat;
        engine.State.hp=engine.State.maxHp=9999; c.enemyHp=c.enemyMaxHp=9999; c.enemyLevel=20;
        SkillMechanics.Ensure(c.enemySkills); TraitMechanics.Ensure(c.enemyTraits);
        string[] combo={"tazia_q","tazia_w","tazia_e","tazia_q"};
        c.enemyDeck=combo.ToList(); c.enemyDrawPile=combo.Reverse().ToList();
        string unplanned=Snapshot(c.enemySkills), originalTraits=Snapshot(c.enemyTraits);
        Invoke(engine,"PlanEnemyTurn");
        Check(c.enemyPlan.SequenceEqual(combo),"enemy planner preserves the affordable original Q W E Q combo");
        Check(Snapshot(c.enemySkills)==unplanned && Snapshot(c.enemyTraits)==originalTraits,"enemy planning advances cloned glass and passive counters only");
        string forecastText=engine.EnemyIntent; int expected=c.intentDamage, expectedBlock=c.intentBlock, hp=engine.State.hp;
        Check(engine.EndTurn(),"actual enemy Q W E Q turn completes");
        Check(hp-engine.State.hp==expected && c.enemyBlock==expectedBlock,
            "enemy shared-resource combo and scheduled barrier damage equal its sequential intent: expected="+expected+",actual="+(hp-engine.State.hp));
        Check(Resource(c.enemySkills,"sword")==0 && Resource(c.enemySkills,"collection")==0 && Resource(c.enemySkills,"shard")==1,
            "actual enemy forges within E, spends that sword on the following Q, and retains only Q's fresh shard");
        Check(Resource(c.playerSkills,"shard")==0 && Resource(c.playerSkills,"collection")==0 && Resource(c.playerSkills,"sword")==0,
            "enemy glass never leaks into the player actor");
    }

    static void DescriptionsAndOrdinarySnapshotRules()
    {
        var e=GameDatabase.Card("tazia_e");
        Check(e.description.Contains("직후") && e.description.Contains("3 이상") && e.description.Contains("유리 검"),"full E text states same-cast collection threshold and sword");
        Check(CardSummaryMechanics.Text(e).Contains("유리 검") && CardSummaryMechanics.Text(e).Contains("3"),"card preview retains E's defining sword threshold");
        string passive=GameDatabase.Passive("tazia_p").description;
        Check(passive.Contains("공유") && passive.Contains("최대 1") && passive.Contains("빗나가도"),"passive details explain shared glass, sword cap and Q consumption");
        // The new operation is deliberately local to Tazia E. Existing condition
        // checks must continue to read one pre-card snapshot, even after a gain.
        var guard=GameDatabase.Card("basic_guard"); var old=guard.mechanics;
        try
        {
            guard.mechanics=new SkillMechanicProfile { rules=new[] {
                new SkillRule {op="gain",key="review_marker",amount=1,cap=4},
                new SkillRule {op="gain",key="review_followup",amount=1,cap=4,conditionKey="review_marker",conditionAmount=1},
                new SkillRule {op="forge_sword",key="sword",amount=1,cap=1,conditionKey="review_marker",conditionAmount=1}
            }};
            var actor=new SkillActorState(); SkillMechanics.AfterCard(actor,SkillMechanics.Clone(actor),guard,true);
            Check(SkillMechanics.Resource(actor,guard.owner,"review_marker")==1 && SkillMechanics.Resource(actor,guard.owner,"review_followup")==0,
                "ordinary rules do not see an earlier gain in the same card");
            Check(SkillMechanics.Resource(actor,guard.owner,"sword")==0,"another card cannot execute Tazia's local forging operation");
            SkillMechanics.AfterCard(actor,SkillMechanics.Clone(actor),guard,true);
            Check(SkillMechanics.Resource(actor,guard.owner,"review_followup")==1,"ordinary conditions see their resource on the following card");
        }
        finally { guard.mechanics=old; }
    }

    static void ForgeConditionPreviews()
    {
        foreach (string variant in new[]{"base","power","rare"})
        foreach (int shards in Enumerable.Range(0,5))
        foreach (int collection in Enumerable.Range(0,5))
        foreach (int sword in new[]{0,1})
        {
            var card=variant == "base" ? GameDatabase.Card("tazia_e") : StrategyMechanics.Apply(GameDatabase.Card("tazia_e"),variant);
            var actor=new SkillActorState(); Put(actor,"shard",shards); Put(actor,"collection",collection); Put(actor,"sword",sword);
            int expected=Math.Min(4,collection+shards+(variant=="rare" && shards>0 ? 1 : 0));
            string before=Snapshot(actor);
            var condition=CombatInformation.Conditions(card,actor).Single(x=>x.current.Contains("수집 직후 예상"));
            Check(condition.met==(expected>=3) && condition.current.Contains("파편 수집 "+expected+" /"),
                "E inspector evaluates the new collection, including upgraded gains, rather than the old sword");
            Check(!condition.requiresHit && condition.result.Contains(expected>=3 ? "즉시" : "미충족"),
                "E inspector exposes same-cast forging without inventing a hit requirement");
            Check(Snapshot(actor)==before,"E collection-condition preview is pure");
        }
    }
    static void BossSwordAndFieldSource()
    {
        var sources=new FieldCardSources(GameDatabase.Cards);
        Check(sources.Find(Owner,"sword")=="tazia_e","the forged sword's field card source is E, not Q's consumption rule");
        int count=sources.Count;
        Check(sources.Find(Owner,"sword")=="tazia_e" && sources.Count==count,"forged sword source lookups use the existing bounded cache");
        Check(SkillMechanics.ResourceName(GameDatabase.Card("tazia_q"),"sword")=="유리 검","the existing resource-name fallback still labels Q's sword correctly");
        var phase=BossProfiles.Get("tazia").phases[1];
        foreach (int existing in new[]{0,1})
        {
            var engine=(GameEngine)typeof(CoreSmokeTests).GetMethod("BossFixture",HiddenStatic).Invoke(null,new object[]{"tazia",2});
            var c=engine.State.combat;
            if(existing>0)Put(c.enemySkills,"sword",existing);
            typeof(GameEngine).GetMethod("QueueBossResource",HiddenInstance).Invoke(engine,new object[]{phase,999});
            var ready=c.boss.preparedResources.Single();
            Check(ready.key=="sword" && ready.cap==1 && ready.amount==1 && !ready.isState,"Tazia's physical sword preparation is capped at one before it is displayed");
            Check(Resource(c.enemySkills,"sword")==existing,"boss preparation queues the sword without secretly advancing the current skill pool");
            Invoke(engine,"PrepareBossNextTurn");
            var sword=c.enemySkills.resources.Single(x=>x.owner==Owner && x.key=="sword");
            Check(sword.amount==1 && sword.cap==1 && !sword.isState && c.boss.preparedResources.Count==0,"next-turn boss preparation produces or refreshes exactly one physical sword");
            Check(sources.Find(sword.owner,sword.key)=="tazia_e","a boss-prepared sword retains its actual generating card source");
        }
    }

    static void ResearchSwordCompletion()
    {
        string task=ResearchProgression.Tasks(new ResearchProfile(),"tazia")[2].description;
        Check(task.Contains("(E)") && task.Contains("직후") && task.Contains("유리 검"),"Tazia's existing E research objective follows the same-cast forging rule");
        foreach (bool hit in new[]{false,true})
        foreach (int existing in new[]{0,1})
        {
            var engine=Fixture(null); var c=engine.State.combat;
            Put(c.playerSkills,"shard",3); Put(c.playerSkills,"sword",existing); c.enemyEvasion=hit?0:100;
            Cast(engine,"tazia_e");
            Check(engine.State.research.completedUniqueSubjects.Contains("tazia"),"a real E starting at collection zero completes research when it forges, including a sword refresh and an avoided attack");
            Check(Resource(c.playerSkills,"sword")==1 && Resource(c.playerSkills,"collection")==0,"the research witness is backed by a produced sword and spent collection");
        }
        foreach (int shards in new[]{0,2})
        {
            var engine=Fixture(null); var c=engine.State.combat; Put(c.playerSkills,"shard",shards); Put(c.playerSkills,"sword",1);
            Cast(engine,"tazia_e");
            Check(!engine.State.research.completedUniqueSubjects.Contains("tazia"),"an already owned sword cannot fake research when this E never reaches the collection threshold");
        }
        var upgraded=Fixture(null); var battle=upgraded.State.combat;
        string reference=CardInstances.CreateReference(upgraded.State,"tazia_e");
        upgraded.State.deck.Add(reference); upgraded.State.cardUpgrades.Add(new CardUpgradeSelection {cardId="tazia_e",instanceId=reference,branchId=RareCardUpgrades.BranchId});
        Put(battle.playerSkills,"shard",2); Cast(upgraded,reference);
        Check(upgraded.State.research.completedUniqueSubjects.Contains("tazia") && Resource(battle.playerSkills,"sword")==1,"research reads the actual rare E's extra collection gain rather than the base card's lower projection");
        foreach (bool enemy in new[]{false,true})
        {
            var engine=Fixture(null); var before=new SkillActorState(); Put(before,"shard",3);
            var after=SkillMechanics.Clone(before); SkillMechanics.AfterCard(after,before,GameDatabase.Card("tazia_e"),true);
            engine.State.combat.playerSkills=after;
            var actions=enemy ? new[]{new CombatAction {cardId="tazia_e",enemy=true,damage=10}} : new CombatAction[0];
            ResearchProgression.AfterCard(engine.State,GameDatabase.Card("tazia_e"),before,true,10,actions);
            Check(!engine.State.research.completedUniqueSubjects.Contains("tazia"),"forecast-only or enemy-only sword creation never completes player research");
        }
    }
}
#endif
