using System;
using System.Collections.Generic;
using System.Linq;

namespace Lumia
{
    public sealed class CardStatusRule : SkillRule
    {
        public string timing = "cast";
    }
    [Serializable] public sealed class CombatStatus
    {
        public string kind, sourceCard, timing;
        public int remaining = 1;
        public bool pending;
    }
    [Serializable] public sealed class StatusActorState
    {
        public List<CombatStatus> effects = new List<CombatStatus>();
    }
    // Durations belong to the affected actor, never to the owner of the source card.
    // Hard control removes at most two energy and leaves at least one action energy.
    public static class StatusMechanics
    {
        static readonly string[] Hard = { "stun", "airborne", "knockback", "suppression", "freeze", "sleep", "stasis" };
        public static StatusActorState Ensure(StatusActorState state)
        {
            if(state==null)state=new StatusActorState();
            if(state.effects==null)state.effects=new List<CombatStatus>();
            return state;
        }
        public static StatusActorState Clone(StatusActorState state)
        {
            return new StatusActorState { effects=Ensure(state).effects.Select(x=>new CombatStatus {kind=x.kind,sourceCard=x.sourceCard,timing=x.timing,remaining=x.remaining,pending=x.pending}).ToList() };
        }
        public static bool Has(StatusActorState actor,string kind) => actor!=null && actor.effects!=null && actor.effects.Any(x=>!x.pending && x.remaining>0 && x.kind==kind);
        public static bool CanUse(StatusActorState actor,CardDef card)
        {
            if(card==null)return false;
            if(card.category=="skill" && (Has(actor,"silence") || Has(actor,"polymorph") || Has(actor,"taunt") || Has(actor,"berserk") || Has(actor,"dance")))return false;
            if(card.movement && Has(actor,"root"))return false;
            if(card.category=="basic" && card.key=="ATK" && (Has(actor,"disarm") || Has(actor,"polymorph") || Has(actor,"dance")))return false;
            return true;
        }
        public static int CostPenalty(StatusActorState actor,CardDef card) => card?.movement==true && (Has(actor,"slow") || Has(actor,"pull")) ? 1 : 0;
        public static int EnergyPenalty(StatusActorState actor)
        {
            int penalty=Ensure(actor).effects.Where(x=>!x.pending && Hard.Contains(x.kind)).Select(x=>x.kind=="suppression" || x.kind=="freeze" || x.kind=="stasis" ? 2 : 1).DefaultIfEmpty(0).Max();
            return Math.Min(2,penalty);
        }
        public static int AccuracyPenalty(StatusActorState actor) => Has(actor,"blind") ? 25 : 0;
        public static int HealingReduction(StatusActorState actor) => Has(actor,"heal_reduction") ? 20 : 0;
        public static int Damage(StatusActorState attacker,StatusActorState target,int amount)
        {
            if(Has(attacker,"fear") || Has(attacker,"charm") || Has(attacker,"attack_down"))amount=amount*4/5;
            if(Has(target,"armor_break"))amount=amount*110/100;
            return Math.Max(0,amount);
        }
        public static void EndTurn(StatusActorState actor)
        {
            foreach(var effect in Ensure(actor).effects.Where(x=>!x.pending))effect.remaining--;
            actor.effects.RemoveAll(x=>!x.pending && x.remaining<=0);
        }
        public static void DamageTaken(StatusActorState actor,int damage)
        {
            if(damage>0)Ensure(actor).effects.RemoveAll(x=>!x.pending && (x.kind=="sleep" || x.kind=="freeze"));
        }
        public static void CancelMissingInstallations(StatusActorState target,SkillActorState caster)
        {
            Ensure(target).effects.RemoveAll(x=>x.pending && x.timing!="next_basic" && !SkillMechanics.Ensure(caster).effects.Any(effect=>effect.sourceCard==x.sourceCard && effect.key==x.timing));
        }
        public static bool Eligible(CardStatusRule rule,SkillActorState before,CardDef card,bool landed)
        {
            if(rule.onHit && !landed)return false;
            int count=SkillMechanics.Resource(before,card.owner,rule.conditionKey);
            if(!string.IsNullOrEmpty(rule.conditionKey) && (rule.conditionExact ? count!=rule.conditionAmount : count<rule.conditionAmount))return false;
            count=SkillMechanics.Resource(before,card.owner,rule.conditionKey2);
            if(!string.IsNullOrEmpty(rule.conditionKey2) && (rule.conditionExact2 ? count!=rule.conditionAmount2 : count<rule.conditionAmount2))return false;
            if(!string.IsNullOrEmpty(rule.conditionPrevious))
            {
                var previous=before.history.FirstOrDefault(x=>x.owner==card.owner);
                var last=previous==null?null:GameDatabase.Card(previous.cardId);
                if(last==null || (last.id!=rule.conditionPrevious && last.key!=rule.conditionPrevious))return false;
            }
            return true;
        }
        public static bool Apply(StatusActorState target,SkillActorState before,CardDef card,bool landed)
        {
            bool control=false;
            foreach(var rule in card.statuses ?? new CardStatusRule[0])
            {
                if(!Eligible(rule,before,card,landed))continue;
                Add(target,rule.key,card.id,rule.duration,rule.timing);
                if(rule.timing=="cast" && rule.key!="armor_break" && rule.key!="heal_reduction" && rule.key!="attack_down")control=true;
            }
            return control;
        }
        public static void Add(StatusActorState actor,string kind,string sourceCard,int duration=1,string timing="cast")
        {
            actor=Ensure(actor);
            bool pending=timing!="cast";
            var effect=actor.effects.FirstOrDefault(x=>x.kind==kind && x.pending==pending && (!pending || (x.sourceCard==sourceCard && x.timing==timing)));
            if(effect==null){effect=new CombatStatus {kind=kind};actor.effects.Add(effect);}
            effect.sourceCard=sourceCard;effect.timing=timing;effect.pending=pending;effect.remaining=Math.Max(effect.remaining,SkillMechanics.Clamp(duration,1,2));
        }
        public static bool Activate(StatusActorState target,string sourceCard,string timing)
        {
            bool control=false;
            foreach(var effect in Ensure(target).effects.Where(x=>x.pending && x.timing==timing && (timing=="next_basic" || x.sourceCard==sourceCard)).ToArray())
            {
                target.effects.Remove(effect);Add(target,effect.kind,effect.sourceCard,effect.remaining);
                if(effect.kind!="armor_break" && effect.kind!="heal_reduction" && effect.kind!="attack_down")control=true;
            }
            return control;
        }
        public static List<SkillMechanicToken> Snapshot(StatusActorState actor)
        {
            return Ensure(actor).effects.Where(x=>!x.pending && x.remaining>0).Select(x=>new SkillMechanicToken {kind="status_"+x.kind,label=Name(x.kind),sourceCard=x.sourceCard,owner=GameDatabase.Card(x.sourceCard)?.owner,remaining=x.remaining,amount=0}).ToList();
        }
        public static string Name(string kind)
        {
            switch(kind)
            {
                case "stun":return "기절";case "root":return "속박";case "silence":return "침묵";case "blind":return "실명";
                case "slow":return "둔화";case "fear":return "공포";case "charm":return "매혹";case "taunt":return "도발";
                case "disarm":return "무장 해제";case "suppression":return "제압";case "airborne":return "에어본";
                case "knockback":return "넉백";case "pull":return "끌어당김";case "polymorph":return "변이";
                case "freeze":return "빙결";case "sleep":return "수면";case "berserk":return "광란";
                case "stasis":return "정지";case "dance":return "춤";
                case "heal_reduction":return "치유 감소";case "armor_break":return "방어력 감소";case "attack_down":return "공격력 감소";
                default:return kind;
            }
        }
        public static string Explain(string kind)
        {
            switch(kind)
            {
                case "stun":case "airborne":case "knockback":case "sleep":return "다음 자신의 턴에 사용할 코스트가 1 줄어듭니다. 하드 제어의 감소량은 가장 큰 값만 적용되며 최소 1 코스트는 유지됩니다."+(kind=="sleep"?" 공격 카드나 카드의 필드 효과로 체력 피해를 받으면 수면이 해제됩니다.":"");
                case "freeze":case "suppression":case "stasis":return "다음 자신의 턴에 사용할 코스트가 2 줄어듭니다. 하드 제어의 감소량은 가장 큰 값만 적용되며 최소 1 코스트는 유지됩니다."+(kind=="freeze"?" 공격 카드나 카드의 필드 효과로 체력 피해를 받으면 빙결이 해제됩니다.":"");
                case "root":return "이동·돌진·순간 이동을 포함한 카드를 사용할 수 없습니다.";
                case "silence":return "실험체의 Q·W·E·R 카드를 사용할 수 없습니다. 기본·무기·전술 카드는 사용할 수 있습니다.";
                case "blind":return "공격의 명중률이 25% 감소합니다. 각 적중마다 대상의 회피율과 합산한 판정을 한 번 합니다.";
                case "slow":case "pull":return "이동·돌진·순간 이동 카드의 코스트가 1 증가합니다. 둔화와 끌어당김은 중첩되지 않습니다.";
                case "fear":case "charm":return "공격 피해가 20% 감소합니다. 공포·매혹·공격력 감소는 중첩되지 않습니다.";
                case "taunt":case "berserk":return "실험체 기술을 사용할 수 없으며 기본·무기·전술 카드로 대응해야 합니다.";
                case "polymorph":case "dance":return "실험체 기술과 기본 공격을 사용할 수 없습니다. 경계·무기·전술 카드는 사용할 수 있습니다.";
                case "disarm":return "기본 공격 카드를 사용할 수 없습니다.";
                case "heal_reduction":return "회복하는 체력이 20% 감소합니다. 패시브·룬의 치유 감소와는 가장 큰 값만 적용됩니다.";
                case "armor_break":return "받는 공격 피해가 10% 증가합니다.";
                case "attack_down":return "공격 피해가 20% 감소합니다. 공포·매혹과 중첩되지 않습니다.";
                default:return "대상의 턴 종료에 남은 턴 수가 1 줄어듭니다.";
            }
        }
        public static List<string> Describe(CardDef card)
        {
            return DescriptionSummary.GroupEffects(DescriptionEffects(card));
        }
        public static List<ConditionEffect> DescriptionEffects(CardDef card)
        {
            var result=new List<ConditionEffect>();
            if (card == null) return result;
            foreach(var rule in card.statuses ?? new CardStatusRule[0])
            {
                var conditions = new List<string>();
                if(!string.IsNullOrEmpty(rule.conditionKey))
                    conditions.Add(SkillMechanics.StateCondition(card,rule.conditionKey,rule.conditionAmount,rule.conditionExact));
                if(!string.IsNullOrEmpty(rule.conditionPrevious))conditions.Add("직전 기술이 "+(GameDatabase.Card(rule.conditionPrevious)?.name ?? rule.conditionPrevious)+"이면");
                if(!string.IsNullOrEmpty(rule.conditionKey2))
                    conditions.Add(SkillMechanics.StateCondition(card,rule.conditionKey2,rule.conditionAmount2,rule.conditionExact2));
                bool hitCondition = rule.timing=="next_basic" || rule.timing!="cast" || rule.onHit;
                if (rule.timing != "cast" && rule.onHit) conditions.Add("이 카드의 공격이 적중했으며");
                if (hitCondition) conditions.Add(rule.timing=="next_basic" ? "다음 기본 공격이 적중하면" : rule.timing!="cast" ? "해당 설치물·추가 효과가 적중하면" : "공격이 적중하면");
                result.Add(new ConditionEffect { key=DescriptionSummary.ConditionKey(rule,rule.timing=="cast"?"cast":rule.timing+"|castHit:"+rule.onHit,hitCondition),
                    condition=DescriptionSummary.JoinConditions(conditions),
                    effect="적에게 "+CardPresentation.WithParticle(Name(rule.key),"를","을")+" "+rule.duration+"턴 부여합니다",
                    note=rule.timing=="next_basic"?"다음 기본 공격이 빗나가도 준비 효과는 소모됩니다.":"" });
            }
            return result;
        }
        public static string Rules(CardDef card)
        {
            var kinds=(card.statuses ?? new CardStatusRule[0]).Select(x=>x.key).Distinct().ToArray();
            return kinds.Length==0 ? "" : string.Join("\n",kinds.Select(x=>Name(x)+": "+Explain(x)))+"\n상태는 대상의 턴 종료에 감소하며 같은 상태의 수치와 기간은 중첩되지 않습니다.";
        }
    }
}
