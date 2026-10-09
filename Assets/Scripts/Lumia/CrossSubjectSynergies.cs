using System;
using System.Collections.Generic;
using System.Linq;

namespace Lumia
{
    [Serializable] public sealed class SynergyCharge
    {
        public string family, owner, sourceCard;
        public int amount = 1, remaining = 2, power = 2;
    }
    [Serializable] public sealed class SynergyUse
    {
        public string family, sourceCard;
        public int count;
    }
    [Serializable] public sealed class SynergyState
    {
        public List<SynergyCharge> charges = new List<SynergyCharge>();
        public List<SynergyUse> uses = new List<SynergyUse>();
        public List<SynergyUse> generations = new List<SynergyUse>();
        public int damageUsed, blockUsed, healUsed;
        public string budgetSourceCard;
    }

    // Cooperative preparations are separate from every subject's original resources.
    // Only manual, paid card actions prepare them; existing preparations can be spent
    // by a free action. Pulses and passive resolution never enter this module.
    public static class CrossSubjectSynergies
    {
        public static readonly string[] Families = { "vf", "bomb", "oil", "wound", "displace", "mobility", "bloom", "support" };
        static readonly string[] BombSetups = { "isol_q", "isol_r", "rozzi_r", "celine_q", "theodore_q", "theodore_e", "theodore_w" };
        static readonly string[] BombConsumers = { "isol_q", "isol_w", "isol_r", "rozzi_q", "rozzi_w", "rozzi_e", "rozzi_r", "celine_q", "celine_w", "theodore_q", "theodore_e", "theodore_r" };
        static readonly string[] FireCards = { "kenneth_w", "kenneth_r", "bihyung_w" };
        static readonly string[] SupportCards = { "sua_w", "johann_w", "johann_r", "leni_q", "leni_e", "charlotte_w", "charlotte_e" };
        static readonly string[] MobileOwners = { "laura", "leon", "rozzi", "silvia" };
        static readonly string[] VfConsumers = { "echion_w", "echion_r", "blair_w", "blair_r" };
        sealed class Activation
        {
            public string family, kind, effectKey;
            public SynergyCharge charge;
            public int damage, block, heal, power;
        }

        public static IList<string> SupportedCardIds
        {
            get { return GameDatabase.Cards.Where(c => FullDescription(c).Any()).Select(c => c.id).ToList().AsReadOnly(); }
        }
        public static void Ensure(SkillActorState actor)
        {
            if (actor.synergy == null) actor.synergy = new SynergyState();
            if (actor.synergy.charges == null) actor.synergy.charges = new List<SynergyCharge>();
            if (actor.synergy.uses == null) actor.synergy.uses = new List<SynergyUse>();
            if (actor.synergy.generations == null) actor.synergy.generations = new List<SynergyUse>();
            actor.synergy.charges.RemoveAll(x => x == null || x.amount <= 0 || x.remaining <= 0);
        }
        public static SynergyState Clone(SynergyState state)
        {
            return state == null ? new SynergyState() : new SynergyState {
                charges=(state.charges ?? new List<SynergyCharge>()).Where(x=>x!=null).Select(x=>new SynergyCharge {family=x.family,owner=x.owner,sourceCard=x.sourceCard,amount=x.amount,remaining=x.remaining,power=x.power}).ToList(),
                uses=(state.uses ?? new List<SynergyUse>()).Where(x=>x!=null).Select(x=>new SynergyUse {family=x.family,sourceCard=x.sourceCard,count=x.count}).ToList(),
                generations=(state.generations ?? new List<SynergyUse>()).Where(x=>x!=null).Select(x=>new SynergyUse {family=x.family,sourceCard=x.sourceCard,count=x.count}).ToList(),
                damageUsed=state.damageUsed,blockUsed=state.blockUsed,healUsed=state.healUsed,budgetSourceCard=state.budgetSourceCard
            };
        }
        public static void StartTurn(SkillActorState actor)
        {
            Ensure(actor);
            foreach (var charge in actor.synergy.charges) --charge.remaining;
            actor.synergy.charges.RemoveAll(x=>x.remaining<=0);
            actor.synergy.uses.Clear(); actor.synergy.generations.Clear();
            actor.synergy.damageUsed=actor.synergy.blockUsed=actor.synergy.healUsed=0;
            actor.synergy.budgetSourceCard=null;
        }
        static IEnumerable<SynergyCharge> Charges(SkillActorState actor, string family)
        {
            return (actor?.synergy?.charges ?? new List<SynergyCharge>()).Where(x=>x!=null && x.family==family && x.amount>0 && x.remaining>0);
        }
        static string Subject(CardDef card) => card?.id==null || card.id.LastIndexOf('_')<0 ? "" : card.id.Substring(0,card.id.LastIndexOf('_'));
        static string Owner(string id) => GameDatabase.Card(id+"_q")?.owner ?? id;
        static bool Used(SkillActorState actor,string family) => (actor?.synergy?.uses ?? new List<SynergyUse>()).Any(x=>x!=null && x.family==family && x.count>0);
        static SynergyCharge Foreign(SkillActorState actor,string family,CardDef card) => Charges(actor,family).FirstOrDefault(x=>x.owner!=card.owner);
        static int Resource(SkillActorState actor,CardDef card,string key) => SkillMechanics.Resource(actor,card.owner,key);
        static bool Increased(SkillActorState actor,SkillActorState before,CardDef card,string key) => Resource(actor,card,key)>Resource(before,card,key);
        static bool IsAttack(SkillActorState actor,CardDef card)
        {
            return card.damage>0 || (card.mechanics?.rules ?? new SkillRule[0]).Any(r=>r.op=="bonus_damage" && r.amount>0 && RuleEligible(actor,card,r,true)
                && (string.IsNullOrEmpty(r.scaleKey) || Resource(actor,card,r.scaleKey)>0));
        }
        static bool RuleEligible(SkillActorState actor,CardDef card,SkillRule rule,bool landed)
        {
            if (rule.onHit && !landed) return false;
            if (!string.IsNullOrEmpty(rule.conditionKey) && (rule.conditionExact ? Resource(actor,card,rule.conditionKey)!=rule.conditionAmount : Resource(actor,card,rule.conditionKey)<rule.conditionAmount)) return false;
            if (!string.IsNullOrEmpty(rule.conditionKey2) && (rule.conditionExact2 ? Resource(actor,card,rule.conditionKey2)!=rule.conditionAmount2 : Resource(actor,card,rule.conditionKey2)<rule.conditionAmount2)) return false;
            if (string.IsNullOrEmpty(rule.conditionPrevious)) return true;
            var history=(actor?.history ?? new List<SkillOwnerHistory>()).FirstOrDefault(x=>x.owner==card.owner);
            var previous=history==null ? null : GameDatabase.Card(history.cardId);
            return previous!=null && (previous.id==rule.conditionPrevious || previous.key==rule.conditionPrevious);
        }
        static bool ForeignBleed(SkillActorState actor,SynergyCharge charge)
        {
            return charge!=null && (actor?.effects ?? new List<SkillTimedEffect>()).Any(x=>x.kind=="bleed" && x.owner==charge.owner && x.remaining>0);
        }
        sealed class ProjectedEffects
        {
            public readonly List<SkillTimedEffect> effects=new List<SkillTimedEffect>();
            public readonly HashSet<string> applied=new HashSet<string>();
        }
        static string EffectKey(string owner,string kind,string key) => owner+"|"+kind+"|"+key;
        // Predict only native timed-effect placement. This leaves previews read-only
        // and follows native rule order, including cleared effects and the 12 slots.
        static ProjectedEffects ProjectTimedEffects(SkillActorState actor,CardDef card,bool landed)
        {
            var result=new ProjectedEffects();
            foreach(var effect in actor.effects ?? new List<SkillTimedEffect>())
                result.effects.Add(new SkillTimedEffect {owner=effect.owner,key=effect.key,kind=effect.kind,sourceCard=effect.sourceCard,amount=effect.amount,remaining=effect.remaining,synergyBoosted=effect.synergyBoosted});
            foreach(var rule in card.mechanics?.rules ?? new SkillRule[0])
            {
                if(!RuleEligible(actor,card,rule,landed))continue;
                string key=string.IsNullOrEmpty(rule.key)?rule.op:rule.key;
                if(rule.op=="clear_effect")
                {
                    result.effects.RemoveAll(x=>x.owner==card.owner && x.key==key);
                    continue;
                }
                if(!new[]{"bleed","burn","delayed_damage","summon","hot","guard","counter","empower_basic","revive"}.Contains(rule.op))continue;
                int amount=Math.Max(0,rule.amount);
                if(!string.IsNullOrEmpty(rule.scaleKey))amount*=Math.Min(SkillMechanics.Clamp(rule.cap,1,8),Resource(actor,card,rule.scaleKey));
                int limit=rule.op=="counter"?8:rule.op=="empower_basic"?16:rule.op=="revive"?40:rule.op=="delayed_damage"?24:10;
                amount=SkillMechanics.Clamp(amount,0,limit);
                if(amount==0)continue;
                var effect=result.effects.FirstOrDefault(x=>x.owner==card.owner && x.kind==rule.op && x.key==key);
                if(effect==null)
                {
                    if(result.effects.Count>=12)continue;
                    effect=new SkillTimedEffect {owner=card.owner,key=key,kind=rule.op};result.effects.Add(effect);
                }
                effect.sourceCard=card.id;effect.amount=Math.Max(effect.amount,amount);
                effect.remaining=Math.Max(effect.remaining,SkillMechanics.Clamp(rule.duration,1,3));
                result.applied.Add(EffectKey(effect.owner,effect.kind,effect.key));
            }
            return result;
        }
        static List<Activation> Plan(SkillActorState actor,CardDef card,bool landed)
        {
            var result=new List<Activation>();
            if (actor==null || card==null || card.category!="skill" || string.IsNullOrEmpty(card.owner)) return result;
            var state=actor.synergy ?? new SynergyState();
            Action<Activation> add=activation=> {
                if (activation.kind!="transfer" && Used(actor,activation.family)) return;
                activation.damage=Math.Min(activation.damage,Math.Max(0,6-state.damageUsed-result.Sum(x=>x.damage)));
                activation.block=Math.Min(activation.block,Math.Max(0,6-state.blockUsed-result.Sum(x=>x.block)));
                activation.heal=Math.Min(activation.heal,Math.Max(0,4-state.healUsed-result.Sum(x=>x.heal)));
                if (activation.damage+activation.block+activation.heal>0 || activation.kind=="delayed" || activation.kind=="plasma" || activation.kind=="kindle" || activation.kind=="wound") result.Add(activation);
            };
            if (VfConsumers.Contains(card.id) && Charges(actor,"vf").Select(x=>x.owner).Distinct().Count()==2)
            {
                if (card.key=="W") add(new Activation {family="vf",block=3});
                else if (landed) add(new Activation {family="vf",damage=Subject(card)=="echion"?4:6});
            }
            // Rozzi may detonate a previously strengthened fuse before its timer.
            // This transfers that one stored boost; it is not a new bomb activation.
            var carriedFuse=new[]{"rozzi_q","rozzi_w","rozzi_e"}.Contains(card.id)
                ? (actor.effects ?? new List<SkillTimedEffect>()).FirstOrDefault(x=>x.owner==card.owner && x.key=="semtex_fuse" && x.kind=="delayed_damage" && x.remaining>0 && x.synergyBoosted && x.synergyBoostAmount>0)
                : null;
            bool earlyDetonation=carriedFuse!=null && (card.mechanics?.rules ?? new SkillRule[0]).Any(r=>r.op=="clear_effect" && r.key=="semtex_fuse" && RuleEligible(actor,card,r,landed))
                && (card.mechanics?.rules ?? new SkillRule[0]).Any(r=>r.op=="bonus_damage" && r.amount>0 && r.conditionKey=="semtex" && RuleEligible(actor,card,r,landed));
            if(earlyDetonation)add(new Activation {family="bomb",kind="transfer",damage=carriedFuse.synergyBoostAmount});
            var bomb=Foreign(actor,"bomb",card);
            if (bomb!=null && BombConsumers.Contains(card.id) && !earlyDetonation)
            {
                if (new[]{"isol_q","isol_r","rozzi_r","theodore_q"}.Contains(card.id))
                {
                    var projected=ProjectTimedEffects(actor,card,landed);
                    var target=projected.effects.FirstOrDefault(x=>x.owner==card.owner && x.sourceCard==card.id && x.kind=="delayed_damage" && !x.synergyBoosted && x.remaining>0 && projected.applied.Contains(EffectKey(x.owner,x.kind,x.key)));
                    if (target!=null) add(new Activation {family="bomb",kind="delayed",charge=bomb,power=bomb.power,effectKey=target.key});
                }
                else if (card.id=="celine_q")
                {
                    if (Resource(actor,card,"bomb")<3) add(new Activation {family="bomb",kind="plasma",charge=bomb});
                }
                else if (landed) add(new Activation {family="bomb",damage=2,charge=bomb});
            }
            var oil=Foreign(actor,"oil",card);
            if (oil!=null && FireCards.Contains(card.id) && SkillMechanics.Resource(actor,Owner("adriana"),"oil")>0)
            {
                var projected=ProjectTimedEffects(actor,card,landed);
                bool applies=projected.effects.Any(x=>x.owner==card.owner && x.sourceCard==card.id && x.kind=="burn" && projected.applied.Contains(EffectKey(x.owner,x.kind,x.key)))
                    && (projected.effects.Count<12 || projected.effects.Any(x=>x.owner=="synergy:oil" && x.key=="kindled_oil"));
                if (applies) add(new Activation {family="oil",kind="kindle",charge=oil});
            }
            var wound=Foreign(actor,"wound",card);
            if (landed && new[]{"cathy_q","cathy_w"}.Contains(card.id) && ForeignBleed(actor,wound)) add(new Activation {family="wound",kind="wound",charge=wound});
            var displace=Foreign(actor,"displace",card);
            if (displace!=null && landed && card.id=="magnus_e" && Resource(actor,card,"wall_pressure")==0) add(new Activation {family="displace",damage=4,charge=displace});
            var mobility=Foreign(actor,"mobility",card);
            if (mobility!=null && MobileOwners.Contains(Subject(card)) && landed && IsAttack(actor,card)) add(new Activation {family="mobility",damage=2,charge=mobility});
            var bloom=Foreign(actor,"bloom",card);
            if (bloom!=null && new[]{"priya","vanya"}.Contains(Subject(card)))
            {
                if (card.key=="W") add(new Activation {family="bloom",block=2,charge=bloom});
                else if (card.id=="priya_r") add(new Activation {family="bloom",heal=2,charge=bloom});
                else if (landed && IsAttack(actor,card)) add(new Activation {family="bloom",damage=2,charge=bloom});
            }
            var support=Foreign(actor,"support",card);
            if (support!=null && SupportCards.Contains(card.id)) add(new Activation {family="support",charge=support,heal=card.heal>0?2:0,block=card.heal>0?0:2});
            return result;
        }
        public static SkillInstantBonus Bonuses(SkillActorState actor,CardDef card,bool landed=true)
        {
            var plan=Plan(actor,card,landed);
            return new SkillInstantBonus {damage=plan.Sum(x=>x.damage),block=plan.Sum(x=>x.block),heal=plan.Sum(x=>x.heal)};
        }
        static void Spend(SkillActorState actor,CardDef card,Activation activation)
        {
            var state=actor.synergy;
            state.damageUsed+=activation.damage;state.blockUsed+=activation.block;state.healUsed+=activation.heal;
            if(activation.damage+activation.block+activation.heal>0)state.budgetSourceCard=card.id;
            if(activation.kind=="transfer")return;
            if (activation.family=="vf") state.charges.RemoveAll(x=>x.family=="vf");
            else if (activation.charge!=null)
            {
                var charge=state.charges.FirstOrDefault(x=>x.family==activation.family && x.owner==activation.charge.owner && x.sourceCard==activation.charge.sourceCard);
                if (charge!=null) --charge.amount;
            }
            state.uses.Add(new SynergyUse {family=activation.family,sourceCard=card.id,count=1});
        }
        static void AddResource(SkillActorState actor,string owner,string key,int amount,int cap,string label)
        {
            var resource=actor.resources.FirstOrDefault(x=>x.owner==owner && x.key==key);
            if (resource==null) {resource=new SkillResource {owner=owner,key=key,label=label,cap=cap};actor.resources.Add(resource);}
            resource.amount=Math.Min(cap,resource.amount+amount);
        }
        static void Prepare(SkillActorState actor,CardDef card,string family,int amount=1,int power=2)
        {
            var state=actor.synergy;
            var generation=state.generations.FirstOrDefault(x=>x.family==family);
            if (generation!=null && generation.count>=2) return;
            if (generation==null) {generation=new SynergyUse {family=family,sourceCard=card.id};state.generations.Add(generation);}
            ++generation.count;
            var charge=state.charges.FirstOrDefault(x=>x.family==family && x.owner==card.owner);
            if (charge==null) {charge=new SynergyCharge {family=family,owner=card.owner,sourceCard=card.id,amount=0};state.charges.Add(charge);}
            if(charge.amount<=0 || power>=charge.power)charge.sourceCard=card.id;
            charge.power=charge.amount>0?Math.Max(charge.power,power):power;
            charge.amount=Math.Min(family=="oil"?3:1,charge.amount+amount);charge.remaining=2;
        }
        static bool NewEffect(SkillActorState actor,SkillActorState before,CardDef card,string kind)
        {
            return actor.effects.Any(x=>x.owner==card.owner && x.sourceCard==card.id && x.kind==kind && x.remaining>0
                && !before.effects.Any(old=>old.owner==x.owner && old.key==x.key && old.kind==x.kind && old.remaining>0));
        }
        public static void AfterCard(SkillActorState actor,SkillActorState before,CardDef card,bool landed,int paidCost)
        {
            if (actor==null || before==null || card==null || card.category!="skill") return;
            Ensure(actor);
            foreach (var activation in Plan(before,card,landed))
            {
                // A full field can prevent the native setup itself. In that case the
                // external preparation must remain available rather than be wasted.
                if (activation.kind=="delayed" && !actor.effects.Any(x=>x.owner==card.owner && x.key==activation.effectKey && x.sourceCard==card.id && x.kind=="delayed_damage" && x.remaining>0 && !x.synergyBoosted)) continue;
                if (activation.kind=="kindle" && actor.effects.Count>=12 && !actor.effects.Any(x=>x.owner=="synergy:oil" && x.key=="kindled_oil")) continue;
                Spend(actor,card,activation);
                if (activation.kind=="delayed")
                {
                    var effect=actor.effects.FirstOrDefault(x=>x.owner==card.owner && x.key==activation.effectKey && x.sourceCard==card.id && x.kind=="delayed_damage" && x.remaining>0);
                    if (effect!=null && !effect.synergyBoosted) {int old=effect.amount;effect.amount=Math.Min(24,effect.amount+activation.power);effect.synergyBoostAmount=effect.amount-old;effect.synergyBoosted=true;}
                }
                else if (activation.kind=="plasma") AddResource(actor,Owner("celine"),"bomb",1,4,"플라즈마 폭탄");
                else if (activation.kind=="wound") AddResource(actor,Owner("cathy"),"wounded",1,3,"상처");
                else if (activation.kind=="kindle")
                {
                    var resource=actor.resources.First(x=>x.owner==Owner("adriana") && x.key=="oil");--resource.amount;
                    var effect=actor.effects.FirstOrDefault(x=>x.owner=="synergy:oil" && x.key=="kindled_oil");
                    if (effect==null && actor.effects.Count<12) {effect=new SkillTimedEffect {owner="synergy:oil",key="kindled_oil",kind="burn"};actor.effects.Add(effect);}
                    if (effect!=null) {effect.label="연계 기름 화재";effect.sourceCard=card.id;effect.amount=3;effect.remaining=2;}
                }
            }
            if (paidCost>0)
            {
                if ((Subject(card)=="echion" && Increased(actor,before,card,"vf")) || (Subject(card)=="blair" && (Increased(actor,before,card,"vp") || Resource(actor,card,"form")!=Resource(before,card,"form")))) Prepare(actor,card,"vf");
                if (BombSetups.Contains(card.id))
                {
                    bool valid=NewEffect(actor,before,card,"delayed_damage") || (card.id=="celine_q" && Increased(actor,before,card,"bomb"))
                        || (card.id=="theodore_e" && landed) || (card.id=="theodore_w" && Increased(actor,before,card,"screen"));
                    if (valid) Prepare(actor,card,"bomb",power:card.id=="theodore_w"?3:2);
                }
                if (Subject(card)=="adriana" && Increased(actor,before,card,"oil")) Prepare(actor,card,"oil",Resource(actor,card,"oil")-Resource(before,card,"oil"));
                if (Subject(card)!="cathy" && actor.effects.Any(x=>x.owner==card.owner && x.sourceCard==card.id && x.kind=="bleed" && x.remaining>0)
                    && (card.mechanics?.rules ?? new SkillRule[0]).Any(r=>r.op=="bleed" && RuleEligible(before,card,r,landed))) Prepare(actor,card,"wound");
                if (Subject(card)!="magnus" && (card.statuses ?? new CardStatusRule[0]).Any(r=>r.key=="knockback" && r.timing=="cast" && StatusMechanics.Eligible(r,before,card,landed))) Prepare(actor,card,"displace");
                if (card.movement && MobileOwners.Contains(Subject(card))) Prepare(actor,card,"mobility");
                if ((Subject(card)=="priya" && Increased(actor,before,card,"flower")) || (Subject(card)=="vanya" && Increased(actor,before,card,"dream"))) Prepare(actor,card,"bloom");
                if (SupportCards.Contains(card.id)) Prepare(actor,card,"support");
            }
            if (SkillMechanics.Resource(actor,Owner("adriana"),"oil")==0) actor.synergy.charges.RemoveAll(x=>x.family=="oil");
            actor.synergy.charges.RemoveAll(x=>x.amount<=0);
        }

        public static string FamilyName(string family)
        {
            switch (family) {case "vf":return "VF 공명";case "bomb":return "폭파 연계";case "oil":return "기름 점화";case "wound":return "상처 연계";case "displace":return "밀려난 위치";case "mobility":return "기동 연계";case "bloom":return "꽃과 나비";case "support":return "보호 지원";default:return family;}
        }
        public static List<SkillMechanicToken> Snapshot(SkillActorState actor)
        {
            var result=new List<SkillMechanicToken>();
            foreach(var charge in actor?.synergy?.charges ?? new List<SynergyCharge>())
                if(charge.amount>0 && charge.remaining>0) result.Add(new SkillMechanicToken {owner="synergy",key=charge.family,label=FamilyName(charge.family),kind="synergy_ready",sourceCard=charge.sourceCard,amount=charge.amount,cap=charge.family=="oil"?3:charge.family=="vf"?2:1,remaining=charge.remaining});
            foreach(var use in actor?.synergy?.uses ?? new List<SynergyUse>())
                if(use.count>0)result.Add(new SkillMechanicToken {owner="synergy",key=use.family,label=FamilyName(use.family),kind="synergy_limit",sourceCard=use.sourceCard,amount=use.count,cap=1});
            foreach(var generation in actor?.synergy?.generations ?? new List<SynergyUse>())
                if(generation.count>0)result.Add(new SkillMechanicToken {owner="synergy",key=generation.family,label=FamilyName(generation.family),kind="synergy_generation",sourceCard=generation.sourceCard,amount=generation.count,cap=2});
            string source=actor?.synergy?.budgetSourceCard ?? result.Select(x=>x.sourceCard).FirstOrDefault(x=>!string.IsNullOrEmpty(x));
            if(source!=null)
            {
                result.Add(new SkillMechanicToken {owner="synergy",key="damage_budget",label="외부 연계 추가 피해",kind="synergy_budget",sourceCard=source,amount=actor.synergy.damageUsed,cap=6});
                result.Add(new SkillMechanicToken {owner="synergy",key="block_budget",label="외부 연계 추가 방어도",kind="synergy_budget",sourceCard=source,amount=actor.synergy.blockUsed,cap=6});
                result.Add(new SkillMechanicToken {owner="synergy",key="heal_budget",label="외부 연계 추가 회복",kind="synergy_budget",sourceCard=source,amount=actor.synergy.healUsed,cap=4});
            }
            return result;
        }
        public static string TokenSummary(SkillMechanicToken token)
        {
            if(token.kind=="synergy_budget")return token.label+" · 이번 턴 "+token.amount+"/"+token.cap;
            if(token.kind=="synergy_generation")return token.label+" · 준비 생성 "+token.amount+"/2";
            if(token.kind=="synergy_limit")return token.label+" · 이번 턴 사용 완료";
            return (GameDatabase.Card(token.sourceCard)?.owner ?? "실험체")+"의 "+token.label+" "+token.amount+" · 준비";
        }
        public static string TokenDescription(SkillMechanicToken token)
        {
            if(token.kind=="synergy_budget")return "모든 외부 연계가 공유하는 이번 턴의 즉시 추가량입니다. 남은 양은 "+Math.Max(0,token.cap-token.amount)+"입니다.";
            if(token.kind=="synergy_generation")return "이번 턴에 이 준비를 "+token.amount+"회 만들었습니다. 턴당 생성은 최대 2회이며 무료 사용은 준비를 만들지 않습니다.";
            if(token.kind=="synergy_limit")return "이번 턴의 "+token.label+" 연계를 사용했습니다. 다음 자신의 턴에 다시 발동할 수 있습니다.";
            return "다른 실험체가 소모하는 준비입니다. 계열당 턴당 1회 발동하며 자신의 턴 시작 "+token.remaining+"회 후 사라집니다.";
        }
        public static IEnumerable<string> FullDescription(CardDef card)
        {
            if (card==null || card.category!="skill") yield break;
            var lines=new List<string>();
            string subject=Subject(card);
            var rules=card.mechanics?.rules ?? new SkillRule[0];
            bool attack=card.damage>0 || rules.Any(r=>r.op=="bonus_damage" && r.amount>0);
            if(new[]{"echion_q","echion_e"}.Contains(card.id))
                lines.Add("코스트를 지불한 뒤 자신의 VF가 실제 증가하면 블레어를 위한 VF 공명을 준비합니다. 블레어의 공명도 함께 있으면 두 공명으로 에키온·블레어의 W 또는 R을 강화할 수 있습니다. 외부 공명은 고유 VF의 획득과 소모를 바꾸지 않습니다.");
            if(new[]{"blair_q","blair_w","blair_e","blair_r"}.Contains(card.id))
                lines.Add("코스트를 지불한 뒤 VF 추적력이 실제 증가하거나 블레이드 형태가 바뀌면 에키온을 위한 VF 공명을 준비합니다. 에키온의 공명도 함께 있으면 두 공명으로 에키온·블레어의 W 또는 R을 강화할 수 있습니다. 외부 공명은 고유 추적력과 형태 조건을 바꾸지 않습니다.");
            if(card.id=="echion_w" || card.id=="blair_w")
                lines.Add("에키온과 블레어의 VF 공명이 모두 준비되어 있으면: 두 공명을 모두 소모하고 방어도 3을 추가로 얻습니다.");
            if(card.id=="echion_r" || card.id=="blair_r")
                lines.Add("에키온과 블레어의 VF 공명이 모두 준비되어 있고 공격이 적중하면: 두 공명을 모두 소모하고 첫 적중에 "+CardPresentation.WithParticle("추가 피해 "+(card.id=="echion_r"?4:6),"를","을")+" 줍니다. 고유 자원 조건과 소모량은 바뀌지 않습니다.");

            bool delayedBomb=new[]{"isol_q","isol_r","rozzi_r","theodore_q"}.Contains(card.id);
            if(delayedBomb)
                lines.Add("코스트를 지불해 이 카드의 지연 폭발을 새로 설치하면 아이솔·로지·셀린·테오도르 중 다른 실험체를 위한 폭파 연계를 준비합니다. 기존 폭발 효과의 갱신으로는 준비하지 않습니다.");
            if(card.id=="celine_q")
                lines.Add("코스트를 지불한 뒤 플라즈마 폭탄이 실제 증가하면 아이솔·로지·테오도르를 위한 폭파 연계를 준비합니다.");
            if(card.id=="theodore_e")
                lines.Add("코스트를 지불한 이 카드가 적중하면 아이솔·로지·셀린을 위한 폭파 연계를 준비합니다.");
            if(card.id=="theodore_w")
                lines.Add("코스트를 지불해 증폭 스크린을 새로 얻으면 아이솔·로지·셀린을 위한 폭파 연계를 준비합니다. 이 준비는 아이솔 Q·R과 로지 R의 첫 폭발 피해를 3 강화하거나, 아이솔 W·로지 Q·W·E·셀린 W의 첫 적중에 피해 2를 더하거나, 셀린 Q에 폭탄 1을 더합니다. 이미 켜진 스크린을 다시 사용하는 것으로는 준비하지 않습니다.");
            if(delayedBomb)
                lines.Add("다른 실험체의 폭파 준비가 있고 이 카드의 지연 폭발을 설치·갱신할 때 아직 외부 강화를 받지 않았다면: 준비를 소모해 해당 폭발의 피해를 최초 1회만 2 늘립니다."
                    +(subject!="theodore"?" 테오도르의 증폭 스크린으로 만든 준비라면 3 늘립니다.":""));
            if(card.id=="celine_q")
                lines.Add("다른 실험체의 폭파 준비가 있고 사용 전 플라즈마 폭탄이 2 이하이면: 준비를 소모해 플라즈마 폭탄을 추가로 1 얻습니다(최대 4). 고유 폭탄은 공유하지 않습니다.");
            if(new[]{"isol_w","celine_w","theodore_e","theodore_r"}.Contains(card.id))
                lines.Add("다른 실험체의 폭파 준비가 있고 공격이 적중하면: 준비를 소모해 첫 적중에 추가 피해 2를 줍니다. 고유 부착 상태와 폭탄은 공유하지 않습니다.");
            if(new[]{"rozzi_q","rozzi_w","rozzi_e"}.Contains(card.id))
                lines.Add("기존 셈텍스탄의 외부 강화를 이어 받는 경우가 아니고 다른 실험체의 폭파 준비가 있으며 공격이 적중하면: 준비를 소모해 첫 적중에 추가 피해 2를 줍니다.");
            if(new[]{"rozzi_q","rozzi_w","rozzi_e","rozzi_r"}.Contains(card.id))
                lines.Add("기존 셈텍스탄을 Q·W·E로 조기 기폭하면 저장된 외부 강화량을 이어 받으며 새 폭파 준비는 중복해서 소모하지 않습니다. 이월 피해도 즉시 추가 피해의 턴당 합계 6에 포함되고 새 폭파 연계의 발동으로 세지 않습니다.");

            if(subject=="adriana" && rules.Any(r=>r.key=="oil" && r.op=="gain"))
                lines.Add("코스트를 지불한 뒤 기름이 실제 증가하면 다른 실험체의 점화를 준비합니다. 케네스의 업화·화염 분쇄 또는 비형의 도깨비 불 지대가 이 준비와 기름 1을 소모하면 턴 종료마다 피해 3을 2회 주는 연계 기름 화재를 남깁니다.");
            if(FireCards.Contains(card.id))
                lines.Add("아드리아나의 기름 준비가 남아 있고 이 카드의 화염 효과가 만들어지면: 준비와 기름 1을 소모해 시전자의 턴 종료마다 방어도를 무시하는 피해 3을 2회 주는 연계 기름 화재를 남깁니다. 화상 틱은 점화를 다시 발동하지 않습니다.");
            if(subject!="cathy" && rules.Any(r=>r.op=="bleed"))
                lines.Add("코스트를 지불한 이 카드의 출혈이 실제 적용되면 캐시를 위한 상처 연계를 준비합니다. 그 출혈이 남아 있는 적에게 캐시의 동맥절제술·앰퓨테이션이 적중하면 카드 처리 후 캐시의 상처가 추가로 1 증가합니다.");
            if(new[]{"cathy_q","cathy_w"}.Contains(card.id))
                lines.Add("다른 실험체의 상처 준비와 그 실험체의 출혈이 남아 있고 공격이 적중하면: 준비를 소모하고 카드 처리가 끝난 뒤 상처를 추가로 1 증가시킵니다. 현재 카드의 중상 조건에는 추가 상처가 소급 적용되지 않습니다.");
            if(subject!="magnus" && (card.statuses ?? new CardStatusRule[0]).Any(r=>r.key=="knockback" && r.timing=="cast"))
                lines.Add("코스트를 지불한 이 카드의 넉백이 적중하면 매그너스의 강타를 위한 밀려난 위치를 준비합니다. 강타는 이 준비로 추가 피해 4를 주지만 외부 연계로 기절을 추가하지 않습니다.");
            if(card.id=="magnus_e")
                lines.Add("다른 실험체가 밀려난 위치를 준비했고 고유 벽 압박이 없으며 공격이 적중하면: 준비를 소모해 추가 피해 4를 줍니다. 고유 벽 압박 연계와 중복되지 않으며 외부 연계는 기절을 추가하지 않습니다.");
            if(MobileOwners.Contains(subject))
            {
                if(card.movement) lines.Add("코스트를 지불해 이 이동 카드를 사용하면 기동 연계를 준비합니다. 라우라·레온·로지·실비아 중 다른 실험체의 공격 기술은 이 준비로 첫 적중에 추가 피해 2를 줍니다.");
                if(attack) lines.Add("라우라·레온·로지·실비아 중 다른 실험체의 기동 준비가 있고 공격이 적중하면: 준비를 소모해 첫 적중에 추가 피해 2를 줍니다. 기본 공격은 이 준비를 소모하지 않습니다.");
            }
            if(subject=="priya" || subject=="vanya")
            {
                string other=subject=="priya"?"바냐":"프리야";
                if(rules.Any(r=>r.op=="gain" && r.key==(subject=="priya"?"flower":"dream")))
                    lines.Add("코스트를 지불한 뒤 "+(subject=="priya"?"사라스바티 꽃":"나비의 꿈")+"이 실제 증가하면 "+other+"를 위한 꽃과 나비를 준비합니다. 고유 꽃과 꿈은 공유하지 않습니다.");
                if(card.key=="W") lines.Add(other+"의 꽃과 나비 준비가 있으면: 준비를 소모해 방어도 2를 추가로 얻습니다.");
                else if(card.id=="priya_r") lines.Add("바냐의 꽃과 나비 준비가 있으면: 준비를 소모해 체력 2를 추가로 회복합니다.");
                else if(attack) lines.Add(other+"의 꽃과 나비 준비가 있고 공격이 적중하면: 준비를 소모해 첫 적중에 추가 피해 2를 줍니다.");
            }
            if(SupportCards.Contains(card.id))
            {
                lines.Add("코스트를 지불해 이 지원 카드를 사용하면 다른 실험체를 위한 보호 지원을 준비합니다. 수아 W, 요한 W·R, 레니 Q·E, 샬럿 W·E가 이 준비를 사용할 수 있습니다.");
                lines.Add("다른 실험체의 보호 지원이 있으면: 준비를 소모해 "+(card.heal>0?"체력 2를 추가로 회복합니다.":"방어도 2를 추가로 얻습니다."));
            }
            foreach(var line in lines)yield return line;
            if(lines.Count>0)
            {
                yield return "각 외부 연계는 자신의 턴당 1회 발동하고 준비는 연계마다 턴당 2회까지 만듭니다. 준비는 자신의 턴 시작 2회 후 사라집니다.";
                yield return "모든 외부 연계의 한 턴 즉시 추가량은 피해 합계 6, 방어도 합계 6, 회복 합계 4까지입니다. 무료·0코스트 사용은 준비를 만들지 않지만 이미 가진 준비는 소모할 수 있습니다. 추가 효과는 다른 연계를 다시 준비하지 않습니다.";
                if(BombSetups.Contains(card.id) || BombConsumers.Contains(card.id) || FireCards.Contains(card.id) || (subject=="adriana" && rules.Any(r=>r.key=="oil" && r.op=="gain")))
                    yield return "폭탄과 기름 화재의 피해는 기존 턴 종료 피해 합계 40 안에서 적용됩니다. 같은 폭탄의 외부 강화는 최초 1회만 적용됩니다.";
            }
        }
        public static string BriefDescription(CardDef card)
        {
            if (card==null || card.category!="skill")return "";
            if(card.id=="echion_w" || card.id=="blair_w")return "두 실험체의 VF 공명을 소모해 방어도 3을 더 얻습니다.";
            if(card.id=="echion_r")return "두 실험체의 VF 공명을 소모해 첫 적중 피해 4를 더합니다.";
            if(card.id=="blair_r")return "두 실험체의 VF 공명을 소모해 첫 적중 피해 6을 더합니다.";
            if(new[]{"isol_q","isol_r","rozzi_r","theodore_q"}.Contains(card.id))
                return Subject(card)=="theodore"?"다른 실험체의 폭파 준비로 첫 폭발 피해 2를 더합니다.":"다른 실험체의 폭파 준비로 첫 폭발 피해 2를 더합니다(스크린 준비는 3).";
            if(card.id=="celine_q")return "사용 전 폭탄이 2 이하이면 외부 폭파 준비로 폭탄을 1 더 얻습니다.";
            if(new[]{"isol_w","celine_w","theodore_e","theodore_r"}.Contains(card.id))return "다른 실험체의 폭파 준비로 첫 적중 피해 2를 더합니다.";
            if(new[]{"rozzi_q","rozzi_w","rozzi_e"}.Contains(card.id))return "폭파 준비로 피해 2를 더하거나 기존 폭탄의 강화를 이어 받습니다.";
            if(card.id=="theodore_w")return "새 스크린을 얻으면 다른 실험체의 첫 폭발 피해 3을 준비합니다.";
            if(FireCards.Contains(card.id))return "준비된 기름 1을 점화해 피해 3을 2회 줍니다.";
            if(new[]{"cathy_q","cathy_w"}.Contains(card.id))return "다른 실험체의 출혈 준비를 소모해 사용 후 상처를 1 더합니다.";
            if(card.id=="magnus_e")return "다른 실험체의 넉백 준비를 소모해 피해 4를 더합니다.";
            if(new[]{"priya","vanya"}.Contains(Subject(card)))
                return card.key=="W"?"다른 실험체의 꽃·나비 준비로 방어도 2를 더 얻습니다."
                    : card.id=="priya_r"?"바냐의 꽃·나비 준비로 체력 2를 더 회복합니다.":"다른 실험체의 꽃·나비 준비로 첫 적중 피해 2를 더합니다.";
            if(SupportCards.Contains(card.id))return card.heal>0?"다른 실험체의 지원 준비로 체력 2를 더 회복합니다.":"다른 실험체의 지원 준비로 방어도 2를 더 얻습니다.";
            if(card.movement&&MobileOwners.Contains(Subject(card)))return "코스트를 지불하면 다른 실험체 공격의 추가 피해 2를 준비합니다.";
            if(MobileOwners.Contains(Subject(card)) && (card.damage>0 || (card.mechanics?.rules??new SkillRule[0]).Any(r=>r.op=="bonus_damage" && r.amount>0)))return "다른 실험체의 기동 준비로 첫 적중 피해 2를 더합니다.";
            if(new[]{"echion_q","echion_e"}.Contains(card.id))return "코스트를 지불해 VF가 늘면 블레어를 위한 공명을 준비합니다.";
            if(new[]{"blair_q","blair_e"}.Contains(card.id))return "코스트를 지불해 VF가 늘거나 형태를 바꾸면 에키온을 위한 공명을 준비합니다.";
            if(Subject(card)!="cathy" && (card.mechanics?.rules??new SkillRule[0]).Any(r=>r.op=="bleed"))return "코스트를 지불한 출혈로 캐시의 추가 상처 1을 준비합니다.";
            if(Subject(card)!="magnus" && (card.statuses??new CardStatusRule[0]).Any(r=>r.key=="knockback"&&r.timing=="cast"))return "코스트를 지불한 넉백으로 매그너스의 추가 피해 4를 준비합니다.";
            if(Subject(card)=="adriana"&&(card.mechanics?.rules??new SkillRule[0]).Any(r=>r.key=="oil"&&r.op=="gain"))return "코스트를 지불한 기름을 다른 실험체의 화염으로 점화할 수 있습니다.";
            return "";
        }
    }
}
