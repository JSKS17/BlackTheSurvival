using System;
using System.Collections.Generic;
using System.Linq;

namespace Lumia
{
    public sealed class TraitMechanicProfile
    {
        public string summary, stateOwner;
        public bool replaceLegacy = true;
        public TraitRule[] rules = new TraitRule[0];
    }
    public sealed class TraitRule : SkillRule
    {
        public string trigger;
        public string conditionCategory;
        public int every = 1, cooldown, maxPerBattle, maxPerTurn, hpBelowPercent, hpBelowNumerator, hpBelowDenominator;
        public bool persistent;
    }
    [Serializable] public sealed class TraitCounter
    {
        public string sourceId, label, trigger;
        public int ruleIndex, count, every, cooldown, battleUses, turnUses;
        public bool persistent;
    }
    [Serializable] public sealed class TraitBuff
    {
        public string sourceId, label, kind;
        public int amount, remaining;
    }
    [Serializable] public sealed class TraitActorState
    {
        public SkillActorState skills = new SkillActorState();
        public List<TraitCounter> counters = new List<TraitCounter>();
        public List<TraitBuff> buffs = new List<TraitBuff>();
    }
    public sealed class TraitSource
    {
        public string id, name, owner;
        public TraitMechanicProfile profile;
    }
    public sealed class TraitContext
    {
        public CardDef card;
        public bool landed = true;
        public int hp, maxHp, damage, block, heal, avoided;
        public int targetHp, targetBlock;
        public bool wildlife;
        public SkillActorState skillActor;
    }
    public sealed class TraitPulse
    {
        public string sourceId, sourceCard, kind;
        public int amount;
        public int duration;
    }
    public sealed class TraitResult
    {
        public SkillInstantBonus bonus;
        public int damageReduction;
        public List<TraitPulse> pulses = new List<TraitPulse>();
    }
    public static class TraitMechanics
    {
        public static void Configure(List<PassiveDef> passives, List<RuneDef> runes)
        {
            var profiles = new Dictionary<string, TraitMechanicProfile>(StringComparer.Ordinal);
            TraitIdentityCore.Populate(profiles);TraitIdentityClassic.Populate(profiles);TraitIdentityRecent.Populate(profiles);RuneIdentityCore.Populate(profiles);
            // Three Calamities is fear, even when triggered by another owner's skill card.
            var hyejin=profiles["hyejin_p"];hyejin.summary="어느 실험체의 기술이든 세 번 적중하면 삼재가 발동해 추가 피해와 공포를 부여합니다.";
            foreach(var rule in hyejin.rules.Where(x=>x.op=="weak")){rule.op="status";rule.key="fear";rule.duration=1;}
            foreach(var id in new[]{"abigail_p","darko_p","adriana_p"})
                foreach(var rule in profiles[id].rules.Where(x=>x.op=="vulnerable")){rule.op="status";rule.key="armor_break";rule.duration=1;}
            profiles["darko_p"].summary="기본 공격이 적중하면 적의 방어력을 줄이고 자신의 방어도를 얻습니다. 이 효과는 두 턴마다 발동합니다.";
            foreach(var rule in profiles["elena_p"].rules.Where(x=>x.op=="weak")){rule.op="status";rule.key="freeze";rule.duration=1;}
            foreach (var passive in passives)
            {
                TraitMechanicProfile profile;
                if (!profiles.TryGetValue(passive.id, out profile)) continue;
                profile.summary = DescriptionSummary.Normalize(profile.summary);
                profile.stateOwner = "trait:" + passive.id;
                SkillMechanics.ConfigureStates(profile.stateOwner, profile.rules);
                passive.mechanics = profile;
                passive.description = Describe(profile);
            }
            foreach (var rune in runes)
            {
                TraitMechanicProfile profile;
                if (!profiles.TryGetValue(rune.id, out profile)) continue;
                profile.summary = DescriptionSummary.Normalize(profile.summary);
                rune.mechanics = profile;
                rune.description = Describe(profile);
            }
        }
        public static TraitActorState Ensure(TraitActorState actor)
        {
            if (actor == null) actor = new TraitActorState();
            actor.skills = SkillMechanics.Ensure(actor.skills);
            if (actor.counters == null) actor.counters = new List<TraitCounter>();
            if (actor.buffs == null) actor.buffs = new List<TraitBuff>();
            return actor;
        }
        public static TraitActorState Clone(TraitActorState actor)
        {
            actor = Ensure(actor);
            return new TraitActorState {
                skills = SkillMechanics.Clone(actor.skills),
                counters = actor.counters.Select(x => new TraitCounter { sourceId=x.sourceId,label=x.label,trigger=x.trigger,ruleIndex=x.ruleIndex,count=x.count,every=x.every,cooldown=x.cooldown,battleUses=x.battleUses,turnUses=x.turnUses,persistent=x.persistent }).ToList(),
                buffs = actor.buffs.Select(x => new TraitBuff { sourceId=x.sourceId,label=x.label,kind=x.kind,amount=x.amount,remaining=x.remaining }).ToList()
            };
        }
        public static int EnergyBonus(TraitActorState actor) { return actor == null ? 0 : Math.Min(2, actor.buffs.Where(x=>x.kind=="energy_buff" && x.remaining>0).Sum(x=>x.amount)); }
        public static int DamageBonus(TraitActorState actor) { return actor == null ? 0 : Math.Min(6, actor.buffs.Where(x=>x.kind=="damage_buff" && x.remaining>0).Sum(x=>x.amount)); }
        public static int EvasionBonus(TraitActorState actor) { return actor == null ? 0 : Math.Min(15, actor.buffs.Where(x=>x.kind=="evasion_buff" && x.remaining>0).Sum(x=>x.amount)); }
        public static int HealingReduction(TraitActorState actor) { return actor == null ? 0 : Math.Min(20, actor.buffs.Where(x=>x.kind=="heal_reduction" && x.remaining>0).Sum(x=>x.amount)); }
        public static int Exposure(TraitActorState actor) { return actor == null ? 0 : Math.Min(10, actor.buffs.Where(x=>x.kind=="exposure" && x.remaining>0).Sum(x=>x.amount)); }
        public static int EmpowerBonus(TraitActorState actor,CardDef card) { return actor==null || card?.category!="basic" || card.key!="ATK" ? 0 : Math.Min(16,actor.skills.effects.Where(x=>x.kind=="empower_basic").Sum(x=>x.amount)); }
        public static TraitActorState NewBattle(TraitActorState run)
        {
            var actor=Clone(run); actor.skills.resources.RemoveAll(x=>!x.persistent);actor.skills.effects.Clear();actor.skills.discounts.Clear();actor.skills.history.Clear();actor.skills.discountSources.Clear();actor.buffs.Clear();
            actor.counters.RemoveAll(x=>!x.persistent);
            foreach(var counter in actor.counters) { counter.cooldown=counter.battleUses=counter.turnUses=0; }
            return actor;
        }
        public static void StartTurn(TraitActorState actor)
        {
            SkillMechanics.StartTurn(actor.skills);
            foreach(var buff in actor.buffs.Where(x=>x.kind=="evasion_buff")) buff.remaining--;
            actor.buffs.RemoveAll(x=>x.remaining<=0);
            foreach (var counter in actor.counters) { if (counter.cooldown>0) counter.cooldown--; counter.turnUses=0; }
        }
        public static void EndTurn(TraitActorState actor)
        {
            foreach (var buff in actor.buffs.Where(x=>x.kind!="evasion_buff")) buff.remaining--;
            actor.buffs.RemoveAll(x=>x.remaining<=0);
        }
        private static bool Conditions(TraitRule rule, SkillActorState actor, CardDef scoped, TraitContext context)
        {
            if (rule.onHit && !context.landed) return false;
            if (!string.IsNullOrEmpty(rule.conditionCategory) && context.card?.category!=rule.conditionCategory) return false;
            if (rule.hpBelowPercent>0 && context.hp*100>context.maxHp*SkillMechanics.Clamp(rule.hpBelowPercent,1,100)) return false;
            if (rule.hpBelowDenominator>0 && (long)context.hp*rule.hpBelowDenominator>(long)context.maxHp*rule.hpBelowNumerator) return false;
            int value=SkillMechanics.Resource(actor,scoped.owner,rule.conditionKey);
            if (!string.IsNullOrEmpty(rule.conditionKey) && (rule.conditionExact ? value!=rule.conditionAmount : value<rule.conditionAmount)) return false;
            value=SkillMechanics.Resource(actor,scoped.owner,rule.conditionKey2);
            if (!string.IsNullOrEmpty(rule.conditionKey2) && (rule.conditionExact2 ? value!=rule.conditionAmount2 : value<rule.conditionAmount2)) return false;
            if (!string.IsNullOrEmpty(rule.conditionPrevious))
            {
                var entry=actor.history.FirstOrDefault(x=>x.owner==scoped.owner); var previous=entry==null ? null : GameDatabase.Card(entry.cardId);
                if (previous==null || (previous.id!=rule.conditionPrevious && previous.key!=rule.conditionPrevious)) return false;
            }
            return true;
        }
        private static int Amount(TraitRule rule, SkillActorState before, CardDef scoped, int cap)
        {
            int amount=Math.Max(0,rule.amount);
            if (!string.IsNullOrEmpty(rule.scaleKey)) amount*=Math.Min(SkillMechanics.Clamp(rule.cap,1,8),SkillMechanics.Resource(before,scoped.owner,rule.scaleKey));
            return SkillMechanics.Clamp(amount,0,cap);
        }
        public static TraitResult Resolve(TraitActorState actor, IEnumerable<TraitSource> sources, string trigger, TraitContext context)
        {
            var result=new TraitResult();
            foreach (var source in sources.Where(x=>x?.profile!=null).GroupBy(x=>x.id).Select(x=>x.First()))
            {
                var scoped=new CardDef { id=context.card?.id ?? "basic_guard",name=source.name,owner="trait:"+source.id,category=context.card?.category ?? "trait",key=context.card?.key ?? "T" };
                var before=SkillMechanics.Clone(actor.skills);
                var eligible=new List<SkillRule>();
                var rules=source.profile.rules ?? new TraitRule[0];
                for (int i=0;i<rules.Length;++i)
                {
                    var rule=rules[i];
                    if (rule.trigger!=trigger) continue;
                    var counter=actor.counters.FirstOrDefault(x=>x.sourceId==source.id && x.ruleIndex==i);
                    if (counter==null) { counter=new TraitCounter { sourceId=source.id,label=source.name,trigger=trigger,ruleIndex=i,every=SkillMechanics.Clamp(rule.every,1,8),persistent=rule.persistent };actor.counters.Add(counter); }
                    // Count observed attacks even when their hit-gated effect misses.
                    counter.count=(counter.count+1)%counter.every;
                    if (counter.count!=0 || counter.cooldown>0 || (rule.maxPerBattle>0 && counter.battleUses>=rule.maxPerBattle) || (rule.maxPerTurn>0 && counter.turnUses>=rule.maxPerTurn) || !Conditions(rule,rule.op=="execute" ? actor.skills : before,scoped,context)) continue;
                    counter.battleUses++; counter.turnUses++; counter.cooldown=SkillMechanics.Clamp(rule.cooldown,0,6);
                    if (rule.op=="bonus_damage" && !(context.card!=null && context.card.damage<=0 && SkillMechanics.Bonuses(context.skillActor,context.card).damage<=0 && EmpowerBonus(actor,context.card)<=0)) {int amount=Amount(rule,before,scoped,5);result.bonus.damage+=amount;result.pulses.Add(new TraitPulse{sourceId=source.id,sourceCard=scoped.id,kind="bonus_damage",amount=amount});}
                    else if (rule.op=="bonus_block") {int amount=Amount(rule,before,scoped,5);result.bonus.block+=amount;result.pulses.Add(new TraitPulse{sourceId=source.id,sourceCard=scoped.id,kind="bonus_block",amount=amount});}
                    else if (rule.op=="bonus_heal") {int amount=Amount(rule,before,scoped,6);result.bonus.heal+=amount;result.pulses.Add(new TraitPulse{sourceId=source.id,sourceCard=scoped.id,kind="bonus_heal",amount=amount});}
                    else if (rule.op=="reduce_damage") result.damageReduction+=Amount(rule,before,scoped,3);
                    else if (rule.op=="damage_resource")
                    {
                        string key=rule.key ?? "damage";
                        var resource=actor.skills.resources.FirstOrDefault(x=>x.owner==scoped.owner && x.key==key);
                        if(resource==null) {resource=new SkillResource {owner=scoped.owner,key=key,label=rule.label??source.name,cap=SkillMechanics.Clamp(rule.cap,1,12),persistent=rule.persistent};actor.skills.resources.Add(resource);}
                        int scaled=Math.Max(0,context.damage)*SkillMechanics.Clamp(rule.amount,0,25)+resource.fraction;
                        resource.amount=Math.Min(resource.cap,resource.amount+scaled/100);resource.fraction=resource.amount>=resource.cap?0:scaled%100;
                    }
                    else if (rule.op=="execute")
                    {
                        if(context.targetHp>0 && context.targetHp+context.targetBlock<=SkillMechanics.Resource(actor.skills,scoped.owner,rule.conditionKey??rule.key)) result.pulses.Add(new TraitPulse {sourceId=source.id,sourceCard=scoped.id,kind="execute",amount=context.targetHp});
                        else {counter.battleUses--;counter.turnUses--;counter.cooldown=0;}
                    }
                    else if(rule.op=="discount_last")
                    {
                        var history=actor.skills.history.FirstOrDefault(x=>x.owner==scoped.owner);
                        var target=history==null?null:GameDatabase.Card(history.cardId);
                        if(target!=null && target.category=="skill" && new[]{"Q","W","E"}.Contains(target.key))
                        {
                            var discount=actor.skills.discounts.FirstOrDefault(x=>x.sourceCard==scoped.id && x.targetCard==target.id);
                            if(discount==null){discount=new SkillCostDiscount{sourceCard=scoped.id,targetCard=target.id};actor.skills.discounts.Add(discount);}
                            discount.amount=Math.Max(discount.amount,Math.Min(1,rule.amount));
                            result.pulses.Add(new TraitPulse{sourceId=source.id,sourceCard=scoped.id,kind="state"});
                        }
                    }
                    else if(rule.op=="status")result.pulses.Add(new TraitPulse{sourceId=source.id,sourceCard=scoped.id,kind="status_"+rule.key,duration=1});
                    else if (rule.op=="damage_buff" || rule.op=="energy_buff" || rule.op=="evasion_buff")
                    {
                        int previousEnergy=EnergyBonus(actor);
                        var buff=actor.buffs.FirstOrDefault(x=>x.sourceId==source.id && x.kind==rule.op);
                        if (buff==null) { buff=new TraitBuff { sourceId=source.id,label=source.name,kind=rule.op };actor.buffs.Add(buff); }
                        buff.amount=Math.Max(buff.amount,Amount(rule,before,scoped,rule.op=="energy_buff"?1:rule.op=="evasion_buff"?10:2));
                        buff.remaining=Math.Max(buff.remaining,SkillMechanics.Clamp(rule.duration,1,3));
                        if (rule.op=="energy_buff" && EnergyBonus(actor)>previousEnergy) result.pulses.Add(new TraitPulse { sourceId=source.id,sourceCard=scoped.id,kind="energy",amount=EnergyBonus(actor)-previousEnergy });
                        else result.pulses.Add(new TraitPulse{sourceId=source.id,sourceCard=scoped.id,kind=rule.op,amount=buff.amount});
                    }
                    else if (new[] { "energy","heal","block","draw","poison","strength","evasion","weak","vulnerable","cleanse","heal_reduction","exposure" }.Contains(rule.op))
                    {
                        int cap=rule.op=="heal"?6:rule.op=="block"?5:rule.op=="evasion" || rule.op=="exposure"?10:rule.op=="poison"?2:rule.op=="heal_reduction"?20:1;
                        result.pulses.Add(new TraitPulse { sourceId=source.id,sourceCard=scoped.id,kind=rule.op,amount=Amount(rule,before,scoped,cap),duration=SkillMechanics.Clamp(rule.duration,1,3) });
                    }
                    else eligible.Add(rule);
                }
                if (eligible.Count>0)
                {
                    scoped.mechanics=new SkillMechanicProfile { rules=eligible.ToArray() };
                    SkillMechanics.AfterCard(actor.skills,before,scoped,context.landed,false);
                    result.pulses.Add(new TraitPulse{sourceId=source.id,sourceCard=scoped.id,kind="state"});
                    foreach(var rule in eligible.OfType<TraitRule>().Where(x=>x.persistent))
                    {
                        foreach(var resource in actor.skills.resources.Where(x=>x.owner==scoped.owner && x.key==(rule.key??rule.op))) resource.persistent=true;
                        foreach(var effect in actor.skills.effects.Where(x=>x.owner==scoped.owner && x.key==(rule.key??rule.op))) effect.persistent=true;
                    }
                }
            }
            result.bonus.damage=Math.Min(10,result.bonus.damage); result.bonus.block=Math.Min(10,result.bonus.block);result.bonus.heal=Math.Min(12,result.bonus.heal);
            return result;
        }
        public static void RecordCard(TraitActorState actor, IEnumerable<TraitSource> sources, CardDef card)
        {
            if (card?.category!="skill") return;
            foreach (var source in sources)
            {
                if(source.id.StartsWith("gear:",StringComparison.Ordinal) && card.key=="R")continue;
                string owner="trait:"+source.id;
                var entry=actor.skills.history.FirstOrDefault(x=>x.owner==owner);
                if (entry==null) { entry=new SkillOwnerHistory { owner=owner };actor.skills.history.Add(entry); }
                entry.cardId=card.id;
            }
        }
        public static List<SkillMechanicToken> Snapshot(TraitActorState actor)
        {
            var result=actor==null ? new List<SkillMechanicToken>() : SkillMechanics.Snapshot(actor.skills);
            if (actor==null) return result;
            result.AddRange(actor.buffs.Select(x=>new SkillMechanicToken { owner=x.sourceId,key=x.kind,label=x.label,kind=x.kind,amount=x.amount,remaining=x.remaining }));
            result.AddRange(actor.counters.Where(x=>x.every>1 && x.count>0).GroupBy(x=>new {x.sourceId,x.every}).Select(g=>g.First()).Select(x=>new SkillMechanicToken { owner=x.sourceId,key="counter",label=x.label,kind="resource",amount=x.count,cap=x.every }));
            return result;
        }
        public static string Describe(TraitMechanicProfile profile) { return TraitPresentation.Describe(profile); }
    }
}
