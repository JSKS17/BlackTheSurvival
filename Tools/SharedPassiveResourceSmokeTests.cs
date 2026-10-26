#if LUMIA_TEST_RUNNER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lumia;

public static class SharedPassiveResourceSmokeTests
{
    static int checks;
    static readonly BindingFlags HiddenStatic=BindingFlags.NonPublic|BindingFlags.Static;
    // Filled from the independent semantic audit, rather than inferred from the
    // production registry. Format: subject/passive key/skill key/cap/binary.
    static readonly string[] Expected = new[] {
        "yuki/cuff/cuff/3/false",
        "nathapon/picturesque/picturesque/3/false",
        "nicky/rage/rage/4/false",
        "leon/pool/pool/3/false",
        "dailin/bac/bac/4/false",
        "markus/rattled/rattled/1/true",
        "magnus/tough/tough/4/false",
        "vanya/dream/dream/4/false",
        "bianca/blood/blood/4/false",
        "shoichi/dagger/dagger/4/false",
        "jan/unyielding/unyielding/3/false",
        "aiden/ampere/ampere/4/false",
        "elena/chill/chill/3/false",
        "elena/frozen/frozen/1/true",
        "william/ball/ball/4/false",
        "eleven/slider/slider/4/false",
        "zahir/eye/eye/1/true",
        "karla/readiness/readiness/3/false",
        "chiara/stigma/stigma/4/false",
        "tazia/shard/shard/4/false",
        "tazia/collection/collection/4/false",
        "tazia/sword/sword/1/false",
        "felix/weaving/weaving/3/false",
        "priya/flower/flower/3/false",
        "fiora/touche/touche/3/false",
        "debi_marlene/ink/ink/2/false",
        "arda/antiquity/antiquity/3/false",
        "tsubame/seal/seal/4/false",
        "kenneth/rage/rage/4/false",
        "justyna/astra/astra/4/false",
        "justyna/justice/justice/1/true",
        "istvan/variable/variable/3/false",
        "blair/form/form/1/true",
        "mirka/impulse/impulse/4/false",
        "coraline/shard/shard/1/true",
        "bihyung/divine/divine/3/false",
        "lucia/crystal/crystal/1/false",
        "jackie/blood_marks/bleeding/3/false",
        "cathy/wound_stacks/wounded/3/false",
        "blair/shifting/shift/1/true",
        "yumin/wind_zone/wind/2/false"
    };

    public static int Run()
    {
        checks=0;
        RegistryAndMigration();
        ProducerCapsAndOriginalRules();
        NativePlayerLinks();
        PrivateResourcesAndLifetimes();
        SaveAndPreviewIsolation();
        EnemyForecastsAndExecution();
        Console.WriteLine("Shared passive resources: PASS "+checks+" assertions.");
        return checks;
    }
    static void Check(bool value,string message) { checks++; if(!value)throw new Exception("Shared passive resources: "+message); }
    static string Owner(string subject)=>GameDatabase.Character(subject).name;
    static int Resource(SkillActorState actor,string subject,string key)=>SkillMechanics.Resource(actor,Owner(subject),key);
    static int Private(TraitActorState traits,string subject,string key)=>SkillMechanics.Resource(traits.skills,"trait:"+subject+"_p",key);
    static GameEngine Fixture(string passive=null,string enemy="fixture")
    {
        var engine=(GameEngine)typeof(CoreSmokeTests).GetMethod("MechanicsFixture",HiddenStatic).Invoke(null,new object[]{passive,enemy});
        var c=engine.State.combat;
        SkillMechanics.Ensure(c.playerSkills); SkillMechanics.Ensure(c.enemySkills);
        TraitMechanics.Ensure(c.playerTraits); TraitMechanics.Ensure(c.enemyTraits);
        engine.State.hp=engine.State.maxHp=9999; c.enemyHp=c.enemyMaxHp=9999;
        return engine;
    }
    static RunState Copy(RunState state)=>(RunState)typeof(CoreSmokeTests).GetMethod("Clone",HiddenStatic).Invoke(null,new object[]{state});
    static string Snapshot(object value)=>(string)typeof(CoreSmokeTests).GetMethod("FieldSnapshot",HiddenStatic).Invoke(null,new object[]{value});
    static void Put(SkillActorState actor,string owner,string key,int amount,int cap=4,bool state=false,bool persistent=false)
    {
        actor.resources.Add(new SkillResource{owner=owner,key=key,label=key,amount=amount,cap=cap,isState=state,persistent=persistent});
    }
    static void Cast(GameEngine engine,string reference)
    {
        var c=engine.State.combat; c.energy=99; c.hand.Clear(); c.hand.Add(reference);
        Check(engine.PlayCard(0),"native player card can be executed: "+reference);
    }
    static TraitSource Source(string id,TraitRule single=null)
    {
        var p=GameDatabase.Passive(id);
        return new TraitSource{id=p.id,name=p.name,owner=p.owner,profile=single==null?p.mechanics:
            new TraitMechanicProfile{stateOwner=p.mechanics.stateOwner,rules=new[]{single}}};
    }
    static TraitContext Context(SkillActorState skills,string card="aya_q")=>new TraitContext{
        card=GameDatabase.Card(card),skillActor=skills,landed=true,hp=50,maxHp=100,targetHp=999,targetBlock=0,damage=5,block=5,heal=5
    };
    static string Identity(SharedPassiveResourceDefinition row)=>row.subjectId+"/"+row.passiveKey+"/"+row.skillKey+"/"+row.cap+"/"+row.isState.ToString().ToLowerInvariant();

    static void RegistryAndMigration()
    {
        var rows=SkillMechanics.SharedPassiveResourceDefinitions.ToArray();
        Check(rows.Select(Identity).OrderBy(x=>x).SequenceEqual(Expected.OrderBy(x=>x)),"explicit registry exactly matches the independent semantic audit");
        Check(rows.Select(x=>x.passiveId+"|"+x.passiveKey).Distinct().Count()==rows.Length,"each passive resource has one canonical mapping");
        foreach(var row in rows)
        {
            Check(row.passiveId==row.subjectId+"_p" && row.owner==Owner(row.subjectId),"registry source and canonical owner agree: "+Identity(row));
            Check(row.cap>=1 && row.cap<=8 && (!row.isState||row.cap==1),"binary and numerical caps are valid: "+Identity(row));
            Check(SkillMechanics.SharedResourcesFor(row.passiveId).Any(x=>Identity(x)==Identity(row)),"per-passive registry includes its declared row");
            foreach(bool malformed in new[]{false,true})
            {
                var skills=new SkillActorState(); var legacy=new SkillActorState();
                Put(skills,row.owner,row.skillKey,malformed?999:1,row.cap,row.isState);
                Put(skills,row.subjectId,row.skillKey,1,row.cap,row.isState);
                Put(legacy,"trait:"+row.passiveId,row.passiveKey,malformed?999:1,99,!row.isState,true);
                Put(legacy,"trait:"+row.passiveId,row.passiveKey,-999,99,!row.isState,true);
                Put(skills,"other actor",row.skillKey,7,8);
                Put(legacy,"trait:unknown_p",row.passiveKey,5,8);
                SkillMechanics.SharePassiveResources(skills,legacy);
                var canonical=skills.resources.Where(x=>x.owner==row.owner && x.key==row.skillKey).ToArray();
                int expected=malformed?row.cap:Math.Min(row.cap,3);
                Check(canonical.Length==1 && canonical[0].amount==expected && canonical[0].cap==row.cap,"legacy aliases and duplicate values merge once under the canonical cap: "+Identity(row));
                Check(canonical[0].isState==row.isState && !canonical[0].persistent && canonical[0].fraction==0,"shared combat resources retain canonical binary/numerical lifetime semantics");
                Check(!legacy.resources.Any(x=>x.owner=="trait:"+row.passiveId && x.key==row.passiveKey),"the legacy shared entry is removed, avoiding duplicate field tokens");
                Check(skills.resources.Any(x=>x.owner=="other actor"&&x.amount==7) && legacy.resources.Any(x=>x.owner=="trait:unknown_p"&&x.amount==5),"migration preserves unrelated owners even with identical resource keys");
                string before=Snapshot(skills)+Snapshot(legacy);
                SkillMechanics.SharePassiveResources(skills,legacy);
                Check(Snapshot(skills)+Snapshot(legacy)==before,"repeated migration cannot duplicate or replenish resources");
            }
        }
    }

    static void ProducerCapsAndOriginalRules()
    {
        foreach(var row in SkillMechanics.SharedPassiveResourceDefinitions)
        foreach(var native in GameDatabase.Passive(row.passiveId).mechanics.rules.Where(r=>r.key==row.passiveKey))
        {
            string original=Snapshot(native);
            var converted=SkillMechanics.SharedPassiveRule(row.passiveId,native);
            Check(Snapshot(native)==original,"binding a shared rule never mutates the database profile");
            if(new[]{"gain","set","state_on","state_off","state_toggle"}.Contains(native.op))
                Check(converted.cap==row.cap,"resource writes use the canonical cap rather than lowering it to an old passive-only cap");
            if(native.op!="gain" || !string.IsNullOrEmpty(native.conditionKey) || !string.IsNullOrEmpty(native.conditionKey2))continue;
            var skills=new SkillActorState(); var traits=TraitMechanics.Ensure(new TraitActorState());
            Put(skills,row.owner,row.skillKey,row.cap,row.cap,row.isState);
            var context=Context(skills);
            for(int eventIndex=0;eventIndex<Math.Max(1,native.every);eventIndex++)
                TraitMechanics.Resolve(traits,new[]{Source(row.passiveId,native)},native.trigger,context);
            Check(Resource(skills,row.subjectId,row.skillKey)==row.cap,"a passive gain cannot shrink an already full skill resource: "+Identity(row));
            Check(skills.resources.Single(x=>x.owner==row.owner&&x.key==row.skillKey).cap==row.cap,"passive gains preserve the skill's original higher cap");
        }
    }

    static void NativePlayerLinks()
    {
        var engine=Fixture("yuki_p"); var c=engine.State.combat;
        Check(Resource(c.playerSkills,"yuki","cuff")==3,"Yuki's battle-start passive supplies actual skill buttons");
        Check(engine.CardTotalDamage("yuki_q")==13,"neutral Yuki Q retains base six, native buttons four and passive three damage");
        int targetHp=c.enemyHp;
        Cast(engine,"yuki_q");
        Check(Resource(c.playerSkills,"yuki","cuff")==1 && targetHp-c.enemyHp==13,"Yuki Q and the on-hit passive consume their existing costs from one pool");
        Cast(engine,"yuki_q");
        Check(Resource(c.playerSkills,"yuki","cuff")==0,"one remaining Yuki button is consumed without becoming negative");
        Cast(engine,"yuki_w");
        Check(Resource(c.playerSkills,"yuki","cuff")==3,"Yuki W refills the pool read by both passive and Q");
        // Earlier Q casts stun the target, reducing even raw 100 evasion to 80.
        // Isolate the guaranteed-miss case from that separate control mechanic.
        c.enemyStatuses=new StatusActorState(); c.enemyEvasion=100;
        Check(engine.EnemyEvasion==100,"the miss fixture has one hundred effective evasion without leftover control");
        targetHp=c.enemyHp; Cast(engine,"yuki_q");
        Check(Resource(c.playerSkills,"yuki","cuff")==3 && c.enemyHp==targetHp,"a missed Yuki Q spends neither the skill nor passive hit-gated button cost");
        c.enemyEvasion=0;
        Cast(engine,"basic_attack");
        Check(Resource(c.playerSkills,"yuki","cuff")==2,"a foreign basic attack still spends a shared button through the passive");

        engine=Fixture("leon_p"); c=engine.State.combat;
        Cast(engine,"basic_attack");
        Check(Resource(c.playerSkills,"leon","pool")==1,"Leon's basic passive creates a skill-usable puddle");
        Check(SkillMechanics.Bonuses(c.playerSkills,GameDatabase.Card("leon_r")).damage==4,"R reads the puddle generated by the passive");
        Cast(engine,"leon_r");
        Check(Resource(c.playerSkills,"leon","pool")==0,"R consumes the same puddle rather than a separate skill-only counter");

        engine=Fixture("bianca_p"); c=engine.State.combat; engine.State.hp=100;
        Cast(engine,"basic_attack");
        Check(Resource(c.playerSkills,"bianca","blood")==1,"Bianca's passive creates blood in the actual skill pool");
        int hp=engine.State.hp; Cast(engine,"bianca_w");
        Check(Resource(c.playerSkills,"bianca","blood")==0 && engine.State.hp-hp>=6,"W consumes passive blood and receives its three-per-blood healing");
        engine=Fixture("chiara_p"); c=engine.State.combat;
        Cast(engine,"aya_q");
        Check(Resource(c.playerSkills,"chiara","stigma")==1,"a foreign skill's passive stigma is visible to Chiara R");
        Check(SkillMechanics.Bonuses(c.playerSkills,GameDatabase.Card("chiara_r")).damage==4,"Chiara R scales from passive-produced stigma");
        Cast(engine,"chiara_r");
        Check(Resource(c.playerSkills,"chiara","stigma")==0,"R consumes the existing stigma while passive turn limits remain enforced");

        engine=Fixture("markus_p"); c=engine.State.combat;
        Cast(engine,"hyunwoo_q");
        Check(Resource(c.playerSkills,"markus","rattled")==1,"the passive control trigger prepares the real binary rattled state");
        Cast(engine,"markus_q");
        Check(Resource(c.playerSkills,"markus","rattled")==0 && c.playerSkills.effects.Any(x=>x.key=="fatal"&&x.kind=="empower_basic"&&x.amount==9),"Markus Q consumes the passive's state and prepares its conditional fatal strike");

        engine=Fixture("jackie_p"); c=engine.State.combat;
        Cast(engine,"aya_q");
        Check(Resource(c.playerSkills,"jackie","bleeding")>0,"Jackie's passive blood-mark alias supplies the native bleeding resource");
        Check(!c.playerTraits.skills.resources.Any(x=>x.owner=="trait:jackie_p"&&x.key=="blood_marks"),"shared alias tokens are not duplicated in the private passive pool");
        engine=Fixture("yumin_p"); c=engine.State.combat;
        Check(Resource(c.playerSkills,"yumin","wind")==1,"Yumin's turn-start binary preparation maps to one numeric wind charge");
        Check(SkillMechanics.Bonuses(c.playerSkills,GameDatabase.Card("yumin_q")).damage==7,"Yumin Q reads wind created by the passive alias");
        Cast(engine,"yumin_q");
        Check(Resource(c.playerSkills,"yumin","wind")==0,"Q consumes the shared wind without leaving a second prepared zone");
        engine=Fixture("blair_p"); c=engine.State.combat;
        Cast(engine,"blair_r");
        Check(Resource(c.playerSkills,"blair","shift")==1 && Resource(c.playerSkills,"blair","form")==1,"Blair's passive R prepares the actual next-skill shift and weapon form");
        Check(!c.playerTraits.skills.resources.Any(x=>x.owner=="trait:blair_p"&&x.key=="shifting"),"Blair's old shifting alias does not become a second field state");
        Check(GameDatabase.Passive("blair_p").description.Contains("공유") && GameDatabase.Passive("blair_p").description.Contains("시프트"),"Blair's passive description identifies its shared native shift state");
        Cast(engine,"blair_q");
        Check(Resource(c.playerSkills,"blair","shift")==0,"Blair Q consumes the state prepared by the passive R");
    }

    static void PrivateResourcesAndLifetimes()
    {
        var engine=Fixture("shoichi_p"); var c=engine.State.combat;
        for(int i=0;i<3;i++)Cast(engine,"aya_q");
        Check(Private(c.playerTraits,"shoichi","deal")==3 && Resource(c.playerSkills,"shoichi","deal")==0,"Shoichi's private deal counter stays separate from shared physical daggers");
        Cast(engine,"basic_attack");
        Check(Resource(c.playerSkills,"shoichi","dagger")==1 && Private(c.playerTraits,"shoichi","deal")==0,"private passive conditions can create a shared dagger and spend only their own counter");
        engine=Fixture("hisui_p"); c=engine.State.combat;
        Cast(engine,"hisui_q");
        Check(Private(c.playerTraits,"hisui","memory")==1 && c.playerSkills.effects.Any(x=>x.key=="memory"&&x.kind=="empower_basic"),"same-named passive memory and timed skill empowerment remain different kinds");
        Check(Resource(c.playerSkills,"hisui","memory")==0,"a timed empowerment is never converted into a scalar shared resource");
        engine=Fixture("cathy_p"); c=engine.State.combat;
        Cast(engine,"aya_q");
        Check(Resource(c.playerSkills,"cathy","wounded")==1 && c.playerTraits.skills.effects.Any(x=>x.key=="wounded"&&x.kind=="bleed"),"Cathy's shared wound count and same-key timed bleeding remain different kinds");
        Cast(engine,"aya_q"); Cast(engine,"aya_q");
        Check(c.playerTraits.skills.effects.Any(x=>x.key=="severe"&&x.kind=="bleed") && Resource(c.playerSkills,"cathy","severe")==0,"passive severe bleeding is not incorrectly converted into the skill's binary severe-wound state");
        engine=Fixture("chloe_p"); c=engine.State.combat;
        Check(c.playerTraits.skills.effects.Any(x=>x.key=="nina"&&x.kind=="summon") && !CompanionMechanics.Alive(c.playerSkills),"passive Nina support does not manufacture the skill companion's HP state");
        Cast(engine,"chloe_q");
        Check(CompanionMechanics.Alive(c.playerSkills) && c.playerTraits.skills.effects.Any(x=>x.key=="nina"&&x.kind=="summon"),"a real deployed Nina and independent passive support retain their existing subsystems");

        foreach(var growth in new[]{new[]{"nadine","wild"},new[]{"luke","tidy"},new[]{"silvia","visited"},new[]{"echion","evolution"}})
        {
            var traits=new TraitActorState(); Put(traits.skills,"trait:"+growth[0]+"_p",growth[1],2,4,false,true);
            var skills=new SkillActorState(); SkillMechanics.SharePassiveResources(skills,traits.skills);
            Check(skills.resources.Count==0 && Private(traits,growth[0],growth[1])==2,"persistent private growth is excluded from unrelated skill resources: "+growth[0]);
            var next=TraitMechanics.NewBattle(traits);
            Check(Private(next,growth[0],growth[1])==2,"persistent private growth still survives the next battle");
        }
        engine=Fixture("leon_p"); c=engine.State.combat; Cast(engine,"basic_attack");
        var nextTraits=TraitMechanics.NewBattle(c.playerTraits); var nextSkills=new SkillActorState();
        SkillMechanics.SharePassiveResources(nextSkills,nextTraits.skills);
        Check(Resource(nextSkills,"leon","pool")==0 && !nextTraits.skills.resources.Any(x=>x.key=="pool"),"shared combat-local puddles are cleared for a new battle");
    }

    static void SaveAndPreviewIsolation()
    {
        foreach(string subject in SkillMechanics.SharedPassiveResourceDefinitions.Select(x=>x.subjectId).Distinct())
        {
            var engine=Fixture(subject+"_p",subject); var c=engine.State.combat;
            foreach(var row in SkillMechanics.SharedResourcesFor(subject+"_p"))
            {
                Put(c.playerSkills,row.owner,row.skillKey,1,row.cap,row.isState);
                Put(c.playerTraits.skills,"trait:"+row.passiveId,row.passiveKey,1,row.cap,row.isState);
                Put(c.enemySkills,row.owner,row.skillKey,1,row.cap,row.isState);
                Put(c.enemyTraits.skills,"trait:"+row.passiveId,row.passiveKey,1,row.cap,row.isState);
            }
            SkillMechanics.Ensure(c.playerSkills); SkillMechanics.Ensure(c.enemySkills);
            TraitMechanics.Ensure(c.playerTraits); TraitMechanics.Ensure(c.enemyTraits);
            string source=Snapshot(c.playerSkills)+Snapshot(c.playerTraits)+Snapshot(c.enemySkills)+Snapshot(c.enemyTraits);
            var loaded=new GameEngine(Copy(engine.State));
            foreach(var row in SkillMechanics.SharedResourcesFor(subject+"_p"))
            foreach(bool enemy in new[]{false,true})
            {
                var skills=enemy?loaded.State.combat.enemySkills:loaded.State.combat.playerSkills;
                var traits=enemy?loaded.State.combat.enemyTraits:loaded.State.combat.playerTraits;
                Check(Resource(skills,subject,row.skillKey)>=Math.Min(2,row.cap),"loaded shared values include both old pools on either actor: "+Identity(row));
                Check(!traits.skills.resources.Any(x=>x.owner=="trait:"+row.passiveId&&x.key==row.passiveKey),"loaded state removes old shared passive tokens");
                Check(loaded.SkillStateSnapshot(enemy).Count(x=>x.owner==row.owner&&x.key==row.skillKey)==1,"the combined field contains exactly one shared token per actor");
            }
            var again=new GameEngine(Copy(loaded.State));
            Check(Snapshot(again.State.combat.playerSkills)==Snapshot(loaded.State.combat.playerSkills)&&Snapshot(again.State.combat.enemySkills)==Snapshot(loaded.State.combat.enemySkills),"repeat loading is idempotent for both actors: "+subject);
            int rng=engine.State.rngState;
            c.enemyPlan=new List<string>{"basic_attack"}; c.enemyPlanCosts=new List<int>{1}; c.enemyPlanFreeCast=new List<bool>{false}; c.block=1;
            foreach(string card in GameDatabase.Character(subject).cards)
            {
                engine.CardTotalDamage(card); engine.PreviewCardUse(card); string intent=engine.EnemyIntent;
            }
            Check(Snapshot(c.playerSkills)+Snapshot(c.playerTraits)+Snapshot(c.enemySkills)+Snapshot(c.enemyTraits)==source,"per-subject previews cannot migrate, spend or duplicate the live pools: "+subject);
            Check(engine.State.rngState==rng,"per-subject previews preserve live RNG: "+subject);
        }
    }

    static void EnemyForecastsAndExecution()
    {
        var mismatches=new List<string>();
        var representatives=new[]{new[]{"leon","basic_attack","leon_q","leon_r"},new[]{"bianca","basic_attack","bianca_q","bianca_w"},
            new[]{"markus","hyunwoo_q","markus_q","basic_attack"},new[]{"justyna","justyna_q","justyna_e","basic_attack"},
            new[]{"debi_marlene","basic_attack","debi_marlene_q","debi_marlene_e"},new[]{"blair","blair_e","blair_q","blair_w"},
            new[]{"mirka","hyunwoo_q","mirka_q","mirka_w","mirka_e"}};
        var native=SkillMechanics.SharedPassiveResourceDefinitions.Select(x=>x.subjectId).Distinct().OrderBy(x=>x)
            .Select(subject=>new[]{subject}.Concat(GameDatabase.Character(subject).cards).Concat(new[]{subject+"_q"}).ToArray());
        foreach(var combo in representatives.Concat(native))
        {
            string subject=combo[0]; var engine=Fixture(null,subject); var c=engine.State.combat;
            c.enemyLevel=20; c.enemyOpeningEnergy=40; c.enemyEnergy=48;
            c.enemyPlan=combo.Skip(1).ToList(); c.enemyPlanCosts=c.enemyPlan.Select(engine.CardCost).ToList(); c.enemyPlanFreeCast=c.enemyPlan.Select(_=>false).ToList();
            c.enemyDeck=c.enemyPlan.ToList();
            string before=Snapshot(c.enemySkills)+Snapshot(c.enemyTraits)+Snapshot(c.playerSkills)+Snapshot(c.playerTraits); int rng=engine.State.rngState;
            string text=engine.EnemyIntent; int expected=c.intentDamage, block=c.intentBlock;
            for(int i=0;i<3;i++)text=engine.EnemyIntent;
            Check(before==Snapshot(c.enemySkills)+Snapshot(c.enemyTraits)+Snapshot(c.playerSkills)+Snapshot(c.playerTraits)&&engine.State.rngState==rng,"shared-resource enemy forecasts remain pure: "+subject);
            int hp=engine.State.hp; Check(engine.EndTurn(),"enemy shared-resource sequence completes: "+subject);
            if(hp-engine.State.hp!=expected || c.enemyBlock!=block)
                mismatches.Add(subject+"/expected="+expected+"/actual="+(hp-engine.State.hp)+"/block="+block+"/actualBlock="+c.enemyBlock
                    +"/actions="+string.Join(",",engine.CombatActions.Where(x=>x.enemy).Select(x=>x.cardId+":"+x.kind+":"+x.damage)));
            Check(!c.playerSkills.resources.Any(x=>x.owner==Owner(subject)),"enemy resources do not leak into the player: "+subject);
        }
        Check(mismatches.Count==0,"actual shared-resource enemy sequences and ticks match forecasts: "+string.Join("; ",mismatches));
    }
}
#endif
