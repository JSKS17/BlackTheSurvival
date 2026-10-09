using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Lumia;

// Validates readability and effect coverage against the game data, without starting a run.
public static class DescriptionAudit
{
    static int assertions;
    static void Check(bool value, string reason) { ++assertions; if (!value) throw new Exception(reason); }
    static void Text(string id, string text)
    {
        Check(!string.IsNullOrWhiteSpace(text), id+": text is present");
        Check(!Regex.IsMatch(text, @"\d+\s*개(?:로|마다|이면| 이상| 쌓|\))"), id+": resources are numerical values");
        Check(!text.Contains("이면 적중하면") && !text.Contains("이면 사용하면"), id+": nested conditions read as one clause");
        Check(!Regex.IsMatch(text,@"상태\s*(?:를|을|가|이)\s*\d|상태\s*×"),id+": semantic states are never described as numeric stacks");
        var lines=text.Split('\n').Where(x=>!string.IsNullOrWhiteSpace(x)).ToArray();
        Check(lines.Length==lines.Distinct().Count(), id+": no exact repeated lines");
    }
    static string EffectWord(string op)
    {
        switch(op)
        {
            case "gain":return "증가";case "set":return "만듭";case "consume":return "소모";
            case "state_on":return "상태";case "state_off":return "해제";case "state_toggle":return "진입";
            case "bonus_damage":case "damage_buff":case "delayed_damage":case "burn":case "bleed":case "summon":case "counter":case "empower_basic":case "reduce_damage":return "피해";
            case "bonus_block":case "block":case "guard":return "방어도";case "bonus_heal":case "heal":case "hot":case "revive":return "회복";
            case "energy":case "energy_buff":case "discount":case "discount_last":return "코스트";case "draw":return "뽑";
            case "evasion":case "evasion_buff":return "회피율";case "poison":return "중독";case "strength":return "공격력";
            case "weak":return "약화";case "vulnerable":return "취약";case "cleanse":return "해제";
            case "status":return "부여";case "heal_reduction":return "치유";case "exposure":return "피해";
            case "shop_discount":return "구매";case "damage_resource":return "축적";case "execute":return "처형";
            case "clear_effect":return "회수";default:throw new Exception("Unknown description operation: "+op);
        }
    }
    static bool EffectRetained(string text,string op)
    {
        // Connecting Korean verbs change the stem of '만듭니다' to '만들고'.
        return op=="set" ? text.Contains("만듭")||text.Contains("만들") : text.Contains(EffectWord(op));
    }
    static void TraitEffects(string id,TraitMechanicProfile profile,string summary)
    {
        foreach(var rule in profile?.rules ?? new TraitRule[0])
        {
            Check(!SkillMechanics.IsState(id,rule.scaleKey),id+": a binary state is not multiplied as a resource");
            Check(EffectRetained(summary,rule.op),id+": "+rule.op+" effect retained");
            if(rule.every>1)Check(summary.Contains(SkillMechanics.Clamp(rule.every,1,8)+"회마다"),id+": trigger frequency retained");
            if(rule.cooldown>0)Check(summary.Contains("쿨다운 "+SkillMechanics.Clamp(rule.cooldown,0,6)+"턴"),id+": cooldown retained");
            if(rule.maxPerTurn>0)Check(summary.Contains("턴당 "+rule.maxPerTurn+"회"),id+": per-turn cap retained");
            if(rule.maxPerBattle>0)Check(summary.Contains("전투당 "+rule.maxPerBattle+"회"),id+": per-battle cap retained");
            if(rule.op=="gain")Check(summary.Contains("최대 "+SkillMechanics.Clamp(rule.cap,1,8)),id+": resource cap retained");
            if(rule.hpBelowDenominator>0)Check(summary.Contains(rule.hpBelowNumerator+"/"+rule.hpBelowDenominator),id+": fractional health trigger retained");
            if(rule.hpBelowPercent>0)Check(summary.Contains(rule.hpBelowPercent+"%"),id+": health threshold retained");
        }
    }
    static void NoDuplicateDataRules(string id,IEnumerable<SkillRule> rules)
    {
        // Repeating a fixed-value assignment has the same result; repeating an additive rule does not.
        var additive=(rules ?? Enumerable.Empty<SkillRule>()).Where(rule=>!new[]{"set","state_on","state_off","discount","clear_effect","counter","revive","empower_basic","burn","bleed","summon","hot","guard","delayed_damage"}.Contains(rule.op) && !(rule.op=="consume"&&rule.amount<=0));
        var keys=additive.Select(rule=>string.Join("|",rule.GetType().GetFields().Where(f=>f.Name!="label").OrderBy(f=>f.Name).Select(f=>f.Name+"="+f.GetValue(rule)))).ToArray();
        Check(keys.Length==keys.Distinct().Count(),id+": no identical data rules were hidden by prose deduplication");
    }
    static string DataSnapshot(object value)
    {
        if(value==null)return "null";
        if(value is string)return "s:"+(string)value;
        var type=value.GetType();
        if(type.IsPrimitive||type.IsEnum||value is decimal)return type.Name+":"+Convert.ToString(value,System.Globalization.CultureInfo.InvariantCulture);
        if(value is System.Collections.IEnumerable)return "["+string.Join(",",((System.Collections.IEnumerable)value).Cast<object>().Select(DataSnapshot))+"]";
        return type.Name+"{"+string.Join("|",type.GetFields().OrderBy(f=>f.Name).Select(f=>f.Name+"="+DataSnapshot(f.GetValue(value))))+"}";
    }
    static int Occurrences(string text,string value)
    {
        int count=0,index=0;
        while((index=(text??"").IndexOf(value,index,StringComparison.Ordinal))>=0){++count;index+=value.Length;}
        return count;
    }
    static string SharedLine(string text,string id,params string[] terms)
    {
        var lines=text.Split('\n').Where(line=>terms.All(line.Contains)).ToArray();
        Check(lines.Length==1,id+": related effects share exactly one condition line ("+string.Join(", ",terms)+"): "+text);
        return lines[0];
    }
    static void OneSentence(string text,string id)
    {
        Check(text.EndsWith(".",StringComparison.Ordinal),id+": grouped action is a complete sentence");
        Check(!Regex.IsMatch(text,@"(?:합니다|줍니다|얻습니다|만듭니다|높입니다|늘립니다)\.\s+"),id+": shared actions use connecting verbs instead of separate effect sentences");
    }
    static void GroupedEffectCases()
    {
        var structural=DescriptionSummary.GroupEffects(new[] {
            new ConditionEffect {key="cast|hit",condition="공격이 적중하면",effect="체력을 3만큼 회복합니다"},
            new ConditionEffect {key="cast|hit",condition="공격이 적중하면",effect="사용 가능한 코스트를 1 회복합니다"},
            new ConditionEffect {key="next_basic|hit",condition="공격이 적중하면",effect="적에게 실명을 1턴 부여합니다"}
        });
        Check(structural.Count==2,"Structural grouping: identical printed conditions with different activation phases stay separate");
        string structuralCast=SharedLine(string.Join("\n",structural),"Structural grouping","체력","코스트");
        Check(!structuralCast.Contains("실명")&&Occurrences(structuralCast,"공격이 적중하면")==1,"Structural grouping: key, not the first condition phrase, determines shared activation");
        OneSentence(structuralCast,"Structural grouping");
        // These fixtures use distinct effect names/values, so merging only the first
        // textual '이면' would lose a condition or incorrectly join two activations.
        var card=new CardDef {id="audit_shared_cast",owner="검토",category="skill",key="Q",mechanics=new SkillMechanicProfile {rules=new[] {
            new SkillRule {op="gain",key="충전",label="충전",amount=2,cap=4,onHit=true,conditionKey="mark",conditionAmount=2,conditionKey2="tempo",conditionAmount2=3},
            new SkillRule {op="discount",targetCard="nia_q",amount=1,onHit=true,conditionKey="tempo",conditionAmount=3,conditionKey2="mark",conditionAmount2=2},
            new SkillRule {op="consume",key="mark",label="표식",amount=0,onHit=true,conditionKey="mark",conditionAmount=2,conditionKey2="tempo",conditionAmount2=3}
        }},statuses=new[] {new CardStatusRule {op="status",key="slow",duration=2,onHit=true,conditionKey="tempo",conditionAmount=3,conditionKey2="mark",conditionAmount2=2}}};
        foreach(string text in new[]{CardPresentation.Describe(card,GameDatabase.Cards),DescriptionSummary.Card(card,GameDatabase.Cards)})
        {
            string line=SharedLine(text,card.id,"충전","코스트","소모","둔화");
            Check(Occurrences(line,"적중")==1,card.id+": an identical hit requirement is printed once across mechanics and status paths");
            Check(Occurrences(line," 2 이상")==1&&Occurrences(line," 3 이상")==1,card.id+": reordered AND conditions are printed once with both thresholds");
            Check(line.Contains("최대 4")&&line.Contains("2턴"),card.id+": resource cap and debuff duration survive grouping");
            OneSentence(line,card.id);
        }
        var separate=new CardDef {id="audit_distinct_conditions",owner="검토",category="skill",key="Q",mechanics=new SkillMechanicProfile {rules=new[] {
            new SkillRule {op="gain",key="알파",label="알파",amount=1,conditionKey="mark",conditionAmount=2},
            new SkillRule {op="gain",key="베타",label="베타",amount=1,conditionKey="mark",conditionAmount=3},
            new SkillRule {op="gain",key="감마",label="감마",amount=1,conditionKey="mark",conditionAmount=2,onHit=true},
            new SkillRule {op="gain",key="델타",label="델타",amount=1,conditionKey="mark",conditionAmount=2,conditionExact=true},
            new SkillRule {op="gain",key="엡실론",label="엡실론",amount=1,conditionKey="mark",conditionAmount=2,conditionPrevious="nia_q"}
        }}};
        foreach(string text in new[]{CardPresentation.Describe(separate,GameDatabase.Cards),DescriptionSummary.Card(separate,GameDatabase.Cards)})
        {
            foreach(string name in new[]{"알파","베타","감마","델타","엡실론"})
            {
                string line=SharedLine(text,separate.id,name);
                Check(new[]{"알파","베타","감마","델타","엡실론"}.Count(line.Contains)==1,separate.id+": different threshold/hit/exact/history conditions stay separate");
            }
        }
        var phases=new CardDef {id="audit_status_phases",owner="검토",category="skill",key="Q",statuses=new[] {
            new CardStatusRule {op="status",key="root",duration=1,onHit=true},
            new CardStatusRule {op="status",key="slow",duration=2,onHit=true},
            new CardStatusRule {op="status",key="blind",duration=1,timing="next_basic"},
            new CardStatusRule {op="status",key="silence",duration=2,timing="trap"}
        }};
        foreach(string text in new[]{CardPresentation.Describe(phases,GameDatabase.Cards),DescriptionSummary.Card(phases,GameDatabase.Cards)})
        {
            string cast=SharedLine(text,phases.id,"속박","둔화");
            Check(!cast.Contains("실명")&&!cast.Contains("침묵"),phases.id+": immediate status does not imply next-basic or installation control is immediate");
            Check(SharedLine(text,phases.id,"실명").Contains("다음 기본 공격"),phases.id+": next-basic timing remains explicit");
            Check(SharedLine(text,phases.id,"침묵").Contains("설치"),phases.id+": delayed/installation timing remains explicit");
        }
        var preparation=new CardDef {id="audit_next_basic_eligibility",owner="검토",category="skill",key="W",statuses=new[] {
            new CardStatusRule {op="status",key="root",duration=1,timing="next_basic",onHit=false},
            new CardStatusRule {op="status",key="slow",duration=2,timing="next_basic",onHit=true}
        }};
        foreach(string text in new[]{CardPresentation.Describe(preparation,GameDatabase.Cards),DescriptionSummary.Card(preparation,GameDatabase.Cards)})
        {
            string ungated=SharedLine(text,preparation.id,"속박"),gated=SharedLine(text,preparation.id,"둔화");
            Check(!ungated.Contains("둔화")&&!gated.Contains("속박"),preparation.id+": both apply on the next basic attack but only one requires this card to hit");
            Check(!ungated.Contains("이 카드의 공격")&&gated.Contains("이 카드의 공격"),preparation.id+": preparatory hit eligibility remains visible independently of the future hit trigger");
        }
        foreach(string text in new[]{CardPresentation.Describe(GameDatabase.Card("irem_r"),GameDatabase.Cards),DescriptionSummary.Card(GameDatabase.Card("irem_r"),GameDatabase.Cards)})
            Check(!Regex.IsMatch(text,@"고양이 상태인 경우에는 [^\n]*생선"),"Irem R: entering/leaving a form never moves unconditional fish-mark clearing into only the cat-form exit branch");
        var future=new CardDef {id="audit_future_setup",owner="검토",category="skill",key="W",mechanics=new SkillMechanicProfile {rules=new[] {
            new SkillRule {op="counter",amount=4,duration=2},
            new SkillRule {op="gain",key="충전",label="충전",amount=1,cap=3}
        }}};
        foreach(string text in new[]{CardPresentation.Describe(future,GameDatabase.Cards),DescriptionSummary.Card(future,GameDatabase.Cards)})
        {
            string line=SharedLine(text,future.id,"반격","충전");
            int triggerAt=line.IndexOf("적중",StringComparison.Ordinal);if(triggerAt<0)triggerAt=line.IndexOf("맞으면",StringComparison.Ordinal);
            int setupAt=line.IndexOf("효과",StringComparison.Ordinal),gainAt=line.IndexOf("충전",StringComparison.Ordinal);
            Check(triggerAt>=0&&setupAt>triggerAt&&gainAt>setupAt,future.id+": an explicit future retaliation trigger ends inside an effect being granted, before the immediate resource gain");
            Check(line.Contains("2턴")&&line.Contains("1회"),future.id+": future counter duration and once-per-turn limit survive grouping");
        }
        var futureTrait=new TraitMechanicProfile {rules=new[] {
            new TraitRule {trigger="battle_start",op="counter",amount=4,duration=2},
            new TraitRule {trigger="battle_start",op="gain",key="charge",label="충전",amount=1,cap=3}
        }};
        foreach(string text in new[]{TraitPresentation.Describe(futureTrait),TraitPresentation.Summary(futureTrait)})
        {
            string line=SharedLine(text,"future-trait","반격","충전");
            int triggerAt=line.IndexOf("적중",StringComparison.Ordinal);if(triggerAt<0)triggerAt=line.IndexOf("맞으면",StringComparison.Ordinal);
            int setupAt=line.IndexOf("효과",StringComparison.Ordinal),gainAt=line.IndexOf("충전",StringComparison.Ordinal);
            Check(triggerAt>=0&&setupAt>triggerAt&&gainAt>setupAt,"future-trait: an explicit enemy-hit retaliation trigger never gates the resource gained when battle starts");
        }
        var limits=new TraitMechanicProfile {rules=new[] {
            new TraitRule {trigger="after_skill",op="heal",amount=3,onHit=true,conditionKey="mark",conditionAmount=2,conditionKey2="tempo",conditionAmount2=3,maxPerTurn=1},
            new TraitRule {trigger="after_skill",op="energy",amount=1,onHit=true,conditionKey="tempo",conditionAmount=3,conditionKey2="mark",conditionAmount2=2,cooldown=2,maxPerTurn=1},
            new TraitRule {trigger="after_skill",op="consume",key="mark",label="표식",amount=0,onHit=true,conditionKey="mark",conditionAmount=2,conditionKey2="tempo",conditionAmount2=3},
            new TraitRule {trigger="before_skill",op="bonus_damage",amount=4,onHit=true,conditionKey="mark",conditionAmount=2,conditionKey2="tempo",conditionAmount2=3}
        }};
        foreach(string text in new[]{TraitPresentation.Describe(limits),TraitPresentation.Summary(limits)})
        {
            string after=SharedLine(text,"mixed-limits","체력","코스트","소모");
            Check(Occurrences(after,"적중")==1&&Occurrences(after," 2 이상")==1&&Occurrences(after," 3 이상")==1,"mixed-limits: shared trigger and reordered conditions are stated once");
            Check(after.Contains("2턴")&&after.Contains("1회"),"mixed-limits: cooldown and per-turn limits are retained");
            int healAt=after.IndexOf("체력",StringComparison.Ordinal),energyAt=after.IndexOf("코스트",StringComparison.Ordinal),consumeAt=after.LastIndexOf("소모",StringComparison.Ordinal);
            Check(after.Substring(healAt,energyAt-healAt).Contains("1회"),"mixed-limits: heal's own per-turn limit stays attached to healing");
            Check(after.Substring(energyAt,consumeAt-energyAt).Contains("2턴"),"mixed-limits: energy's cooldown stays attached to energy recovery");
            Check(!Regex.IsMatch(after.Substring(consumeAt),@"쿨다운|턴당|한 턴에|전투당|전투마다"),"mixed-limits: unlimited consumption is not governed by another effect's cap");
            Check(!after.Contains("추가 피해"),"mixed-limits: pre-card bonus does not merge into post-card recovery");
            SharedLine(text,"mixed-limits","추가 피해");OneSentence(after,"mixed-limits");
        }
        var limited=new TraitMechanicProfile {rules=new[] {
            new TraitRule {trigger="hit",op="heal",amount=2,every=3,cooldown=2,maxPerTurn=1,maxPerBattle=2},
            new TraitRule {trigger="hit",op="energy",amount=1,every=3,cooldown=2,maxPerTurn=1,maxPerBattle=2}
        }};
        foreach(string text in new[]{TraitPresentation.Describe(limited),TraitPresentation.Summary(limited)})
        {
            string line=SharedLine(text,"shared-limits","체력","코스트");
            Check(Occurrences(line,"3회마다")==1&&Occurrences(line,"2턴")==1,"shared-limits: trigger frequency and common cooldown appear once");
            Check(line.Contains("1회")&&line.Contains("2회"),"shared-limits: both per-turn and per-battle limits remain visible");
            OneSentence(line,"shared-limits");
        }
        var frequencies=new TraitMechanicProfile {rules=new[] {
            new TraitRule {trigger="after_basic",op="heal",amount=2,every=2},
            new TraitRule {trigger="after_basic",op="energy",amount=1,every=3},
            new TraitRule {trigger="hit",op="block",amount=3,every=2}
        }};
        foreach(string text in new[]{TraitPresentation.Describe(frequencies),TraitPresentation.Summary(frequencies)})
        {
            Check(SharedLine(text,"frequency","체력").Contains("2회마다"),"frequency: two-use healing stays separate");
            Check(SharedLine(text,"frequency","코스트").Contains("3회마다"),"frequency: three-use energy stays separate");
            Check(!SharedLine(text,"frequency","방어도").Contains("체력"),"frequency: hits are distinct from post-basic uses even with equal frequency");
        }
        var fiora=GameDatabase.Passive("fiora_p");
        foreach(string text in new[]{TraitPresentation.Describe(fiora.mechanics),DescriptionSummary.Passive(fiora)})
        {
            string line=SharedLine(text,"fiora_p","체력","코스트","모두 소모");
            Check(Occurrences(line,"뚜셰가 3 이상")==1&&Occurrences(line,"적중")==1,"Fiora: the full shared threshold/hit condition is stated once");
            Check(line.Contains("2턴")&&line.Contains("1회"),"Fiora: energy cooldown and recovery caps remain visible");
            Check(!Regex.IsMatch(line.Substring(line.LastIndexOf("모두 소모",StringComparison.Ordinal)),@"쿨다운|턴당|한 턴에"),"Fiora: consumption remains uncapped, as implemented");
            Check(!line.Contains("추가 피해"),"Fiora: before-skill bonus remains separate from after-skill recovery");
            OneSentence(line,"fiora_p");
        }
        var amp=GameDatabase.Rune("amplification_drone");
        foreach(string text in new[]{TraitPresentation.Describe(amp.mechanics),DescriptionSummary.Rune(amp)})
        {
            string line=SharedLine(text,"amplification_drone","최대 코스트","첫 적중 피해");
            Check(Occurrences(line,"R 카드")==1&&Occurrences(line,"3턴")==1,"Amplification Drone: R activation and common cooldown are stated once");
            Check(line.Contains("2턴")&&line.Contains("1"),"Amplification Drone: buff duration and value remain visible");
            OneSentence(line,"amplification_drone");
        }
        // Compare independently limited effects to their actual resolver. In particular,
        // Fiora still spends Touche on a second same-turn proc while healing/energy are capped.
        var actor=new TraitActorState();
        var source=new TraitSource {id=fiora.id,name=fiora.name,owner=fiora.owner,profile=fiora.mechanics};
        var sources=new[]{source};
        var touche=new SkillResource {owner="trait:fiora_p",key="touche",label="뚜셰",amount=3,cap=3};actor.skills.resources.Add(touche);
        var context=new TraitContext {card=GameDatabase.Card("nia_q"),landed=true,hp=40,maxHp=80,skillActor=new SkillActorState()};
        Check(TraitMechanics.Resolve(actor,sources,"before_skill",context).bonus.damage==4,"Fiora pre-skill damage remains a separate engine phase");
        var first=TraitMechanics.Resolve(actor,sources,"after_skill",context);
        Check(first.pulses.Any(p=>p.kind=="heal"&&p.amount==3)&&first.pulses.Any(p=>p.kind=="energy"&&p.amount==1)&&touche.amount==0,"Fiora first activation still heals 3, recovers 1 cost, and consumes Touche");
        touche.amount=3;
        var second=TraitMechanics.Resolve(actor,sources,"after_skill",context);
        Check(!second.pulses.Any(p=>p.kind=="heal"||p.kind=="energy")&&touche.amount==0,"Fiora resource consumption remains independent of healing/energy's turn and cooldown caps");
        TraitMechanics.StartTurn(actor);touche.amount=3;
        var next=TraitMechanics.Resolve(actor,sources,"after_skill",context);
        Check(next.pulses.Any(p=>p.kind=="heal")&&!next.pulses.Any(p=>p.kind=="energy")&&touche.amount==0,"Fiora healing resets next turn while cost recovery keeps its two-turn cooldown");
    }
    // Independent expectations come from the engine's three storage paths, rather than
    // from the presentation collector that these checks are meant to validate.
    static readonly Dictionary<string,string> DebuffNames=new Dictionary<string,string>(StringComparer.Ordinal)
    {
        {"poison","중독"},{"weak","약화"},{"vulnerable","취약"},{"bleed","출혈"},{"burn","화상"},
        {"stun","기절"},{"root","속박"},{"silence","침묵"},{"blind","실명"},{"slow","둔화"},
        {"fear","공포"},{"charm","매혹"},{"taunt","도발"},{"disarm","무장 해제"},{"suppression","제압"},
        {"airborne","에어본"},{"knockback","넉백"},{"pull","끌어당김"},{"polymorph","변이"},
        {"freeze","빙결"},{"sleep","수면"},{"berserk","광란"},{"stasis","정지"},{"dance","춤"},
        {"heal_reduction","치유 감소"},{"armor_break","방어력 감소"},{"attack_down","공격력 감소"}
    };
    static HashSet<string> ExpectedDebuffs(CardDef card)
    {
        var result=new HashSet<string>(StringComparer.Ordinal);
        if(card.poison>0)result.Add("poison");
        if(card.weak>0)result.Add("weak");
        if(card.vulnerable>0)result.Add("vulnerable");
        foreach(var status in card.statuses ?? new CardStatusRule[0])result.Add(status.key);
        foreach(var effect in card.mechanics?.rules ?? new SkillRule[0])
            if(effect.amount>0 && (effect.op=="bleed" || effect.op=="burn"))result.Add(effect.op);
        return result;
    }
    static void DebuffFooter(CardDef card)
    {
        string id=card.id,footer=CardPresentation.DebuffRules(card),rules=CardPresentation.Rules(card);
        string body=CardPresentation.Describe(card,GameDatabase.Cards);
        var expected=ExpectedDebuffs(card);
        Check(expected.All(DebuffNames.ContainsKey),id+": newly added debuff kinds require an explicit glossary review");
        Check(string.IsNullOrEmpty(footer)==(expected.Count==0),id+": only cards that actually apply debuffs have a glossary");
        Check(!body.Contains("디버프 설명"),id+": skill application text stays separate from reference rules");
        string preview=DescriptionSummary.CardPreview(card,GameDatabase.Cards);
        Check(!preview.Contains("디버프 설명")&&!DebuffNames.Values.Any(name=>preview.Split('\n').Any(line=>line.StartsWith(name+": ",StringComparison.Ordinal))),id+": compact preview has no glossary footer");
        if(expected.Count==0)
        {
            Check(!rules.Contains("디버프 설명"),id+": beneficial effects and marks do not create a debuff glossary");
            return;
        }
        Text(id+"/debuff-footer",footer);
        Check(footer.StartsWith("디버프 설명\n",StringComparison.Ordinal),id+": glossary has a clear final-section label");
        Check(rules.EndsWith(footer,StringComparison.Ordinal),id+": glossary is the very last part of the full rules");
        Check(rules.IndexOf("디버프 설명",StringComparison.Ordinal)==rules.LastIndexOf("디버프 설명",StringComparison.Ordinal),id+": glossary is appended once");
        var lines=footer.Split('\n');
        var actual=lines.Where(line=>line.Contains(": ")).ToArray();
        Check(actual.Length==expected.Count,id+": each debuff has exactly one explanation, including repeated or conditional applications");
        foreach(var entry in DebuffNames)
        {
            string prefix=entry.Value+": ";
            Check(actual.Count(line=>line.StartsWith(prefix,StringComparison.Ordinal))==(expected.Contains(entry.Key)?1:0),id+": glossary coverage for "+entry.Key);
            Check(!preview.Contains(prefix),id+": "+entry.Key+" reference explanation never appears in compact preview");
        }
        string beforeFooter=rules.Substring(0,rules.Length-footer.Length);
        Check(actual.All(line=>!beforeFooter.Contains(line)),id+": definitions are not repeated before the glossary");
        Check(actual.All(line=>line.EndsWith(".",StringComparison.Ordinal)),id+": glossary entries are complete sentences");
    }
    static void DebuffEdgeCases()
    {
        // A delayed effect and a next-attack preparation must retain their definitions
        // even when the card itself has no immediate attack damage or on-cast status.
        var conditional=new CardDef {id="audit_delayed_control",owner="검토",name="지연 제어",category="skill",key="Q",statuses=new[] {
            new CardStatusRule {op="status",key="root",timing="trap",duration=1,onHit=true},
            new CardStatusRule {op="status",key="slow",timing="next_basic",duration=1,onHit=true},
            new CardStatusRule {op="status",key="slow",conditionKey="counter",conditionAmount=3,duration=2,onHit=true}
        }};
        DebuffFooter(conditional);
        var combined=new CardDef {id="audit_combined_debuff",owner="검토",name="복합 제어",category="skill",key="Q",poison=2,weak=1,vulnerable=1,mechanics=new SkillMechanicProfile {rules=new[] {
            new SkillRule {op="bleed",amount=3,label="피흘림",duration=2,onHit=true},
            new SkillRule {op="bleed",amount=2,label="다른 출혈",duration=1,onHit=true,conditionKey="mark"},
            new SkillRule {op="burn",amount=4,label="발화",duration=2,onHit=true},
            new SkillRule {op="summon",amount=4,label="포탑",duration=2}
        }}};
        DebuffFooter(combined);
        string combinedFooter=CardPresentation.DebuffRules(combined);
        Check(combinedFooter.Contains("25%")&&combinedFooter.Contains("50%"),"Legacy weakness and vulnerability reference their actual damage multipliers");
        Check(combinedFooter.Contains("방어도")&&combinedFooter.Contains("시전자")&&combinedFooter.Contains("턴 종료"),"Bleed/burn reference shield bypass and the caster's ticking clock");
        Check(!combinedFooter.Contains("포탑:"),"Summons deal persistent attacks without being mislabeled as debuffs");
        var noDebuff=new CardDef {id="audit_zero_dot",owner="검토",name="가드",category="skill",key="W",block=8,mechanics=new SkillMechanicProfile {rules=new[] {
            new SkillRule {op="bleed",amount=0,label="빈 출혈"},new SkillRule {op="burn",amount=0,label="빈 화상"},
            new SkillRule {op="guard",amount=3,label="방패",duration=2},new SkillRule {op="hot",amount=3,label="치유",duration=2},
            new SkillRule {op="gain",key="mark",amount=1,cap=1,label="준비"}
        }}};
        DebuffFooter(noDebuff);
        // Explicit numerical and interaction checks keep the explanatory text anchored
        // to the rules the engine implements, rather than merely checking its presence.
        var statusDetails=new Dictionary<string,string[]> {
            {"stun",new[]{"코스트가 1","최소 1"}},{"airborne",new[]{"코스트가 1","최소 1"}},
            {"knockback",new[]{"코스트가 1","최소 1"}},{"sleep",new[]{"코스트가 1","피해를 받으면"}},
            {"freeze",new[]{"코스트가 2","피해를 받으면"}},{"suppression",new[]{"코스트가 2","최소 1"}},
            {"stasis",new[]{"코스트가 2","최소 1"}},{"blind",new[]{"25%"}},
            {"slow",new[]{"코스트가 1 증가","중첩되지"}},{"pull",new[]{"코스트가 1 증가","중첩되지"}},
            {"fear",new[]{"20%","중첩되지"}},{"charm",new[]{"20%","중첩되지"}},
            {"attack_down",new[]{"20%","중첩되지"}},{"heal_reduction",new[]{"20%","가장 큰 값"}},
            {"armor_break",new[]{"10%"}},{"silence",new[]{"Q·W·E·R","기본·무기·전술"}},
            {"root",new[]{"이동·돌진·순간 이동","사용할 수 없습니다"}},{"disarm",new[]{"기본 공격","사용할 수 없습니다"}},
            {"taunt",new[]{"실험체 기술","기본·무기·전술"}},{"berserk",new[]{"실험체 기술","기본·무기·전술"}},
            {"polymorph",new[]{"실험체 기술과 기본 공격","경계·무기·전술"}},{"dance",new[]{"실험체 기술과 기본 공격","경계·무기·전술"}}
        };
        foreach(var entry in statusDetails)
        {
            var card=new CardDef {id="audit_status_"+entry.Key,owner="검토",name="상태 설명",category="skill",key="Q",statuses=new[] {new CardStatusRule {op="status",key=entry.Key,duration=1}}};
            DebuffFooter(card);
            string footer=CardPresentation.DebuffRules(card);
            Check(entry.Value.All(footer.Contains),entry.Key+": exact numerical effect and restrictions are explained");
            Check(footer.Contains("대상의 턴 종료")&&footer.Contains("중첩되지"),entry.Key+": glossary retains the affected actor's duration and reapplication rules");
        }
        foreach(string kind in new[]{"stun","airborne","knockback","sleep","freeze","suppression","stasis"})
        {
            var actor=new StatusActorState();StatusMechanics.Add(actor,kind,"audit",1);
            int expected=kind=="freeze"||kind=="suppression"||kind=="stasis"?2:1;
            Check(StatusMechanics.EnergyPenalty(actor)==expected,kind+": the described maximum-cost penalty matches the engine");
        }
        var afflicted=new StatusActorState();
        StatusMechanics.Add(afflicted,"blind","audit",1);
        Check(StatusMechanics.AccuracyPenalty(afflicted)==25,"The blindness explanation matches the engine's 25-point accuracy penalty");
        StatusMechanics.Add(afflicted,"heal_reduction","audit",1);
        Check(StatusMechanics.HealingReduction(afflicted)==20,"The healing reduction explanation matches the engine's 20-percent reduction");
        foreach(string kind in new[]{"fear","charm","attack_down"})StatusMechanics.Add(afflicted,kind,"audit",1);
        Check(StatusMechanics.Damage(afflicted,new StatusActorState(),100)==80,"Fear, charm and attack reduction apply one shared 20-percent reduction");
        var broken=new StatusActorState();StatusMechanics.Add(broken,"armor_break","audit",1);
        Check(StatusMechanics.Damage(new StatusActorState(),broken,100)==110,"The armor break explanation matches the engine's 10-percent damage increase");
        var duration=new StatusActorState();
        StatusMechanics.Add(duration,"slow","first",1);StatusMechanics.Add(duration,"slow","second",2);StatusMechanics.Add(duration,"slow","third",1);
        Check(duration.effects.Count==1&&duration.effects[0].remaining==2,"Reapplying a named status keeps the longest remaining duration without addition");
        StatusMechanics.EndTurn(duration);
        Check(duration.effects[0].remaining==1,"Status duration falls once at the affected actor's turn end");
        StatusMechanics.EndTurn(duration);
        Check(duration.effects.Count==0,"A status expires when its remaining duration reaches zero");
        foreach(string kind in new[]{"bleed","burn"})
        {
            var dot=new CardDef {id="audit_tick_"+kind,owner="검토",name="지속 피해",category="skill",key="Q",mechanics=new SkillMechanicProfile {rules=new[] {new SkillRule {op=kind,amount=3,duration=2}}}};
            var actor=new SkillActorState();SkillMechanics.AfterCard(actor,SkillMechanics.Clone(actor),dot,true);
            dot.mechanics.rules[0].amount=5;dot.mechanics.rules[0].duration=1;
            SkillMechanics.AfterCard(actor,SkillMechanics.Clone(actor),dot,true);
            Check(actor.effects.Count==1&&actor.effects[0].amount==5&&actor.effects[0].remaining==2,kind+": repeat casts keep the larger damage and longer duration rather than adding either");
            var pulse=SkillMechanics.Tick(actor);
            Check(pulse.Count==1&&pulse[0].amount==5&&actor.effects[0].remaining==1,kind+": each caster-end tick applies the advertised damage and consumes one application");
            SkillMechanics.Tick(actor);
            Check(actor.effects.Count==0,kind+": the advertised number of applications expires the effect");
        }
    }
    static void CrossSubjectDescriptions()
    {
        var supported=CrossSubjectSynergies.SupportedCardIds.ToArray();
        Check(supported.Length>0&&supported.Distinct().Count()==supported.Length,"Cross-subject support exposes a nonempty unique supported-card catalog");
        foreach(var card in GameDatabase.Cards)
        {
            string before=DataSnapshot(card);var lines=CrossSubjectSynergies.FullDescription(card).ToArray();
            string brief=CrossSubjectSynergies.BriefDescription(card);
            Check(supported.Contains(card.id)==(lines.Length>0),card.id+": the support catalog matches its actual full explanation");
            if(lines.Length==0)
                Check(string.IsNullOrEmpty(brief),card.id+": a card without a supported interaction cannot promise a compact synergy bonus");
            else
            {
                Check(card.category=="skill",card.id+": cross-subject rules are attached to subject skills only");
                Check(lines.Distinct().Count()==lines.Length&&lines.All(line=>line.EndsWith(".",StringComparison.Ordinal)),card.id+": synergy roles and limits are distinct complete sentences");
                Check(!string.IsNullOrEmpty(brief)&&brief.EndsWith(".",StringComparison.Ordinal),card.id+": every supported role has a complete compact explanation");
                string full=CardPresentation.Describe(card,GameDatabase.Cards),digest=DescriptionSummary.Card(card,GameDatabase.Cards);
                foreach(string line in lines)
                {
                    string normalized=DescriptionSummary.Normalize(line);
                    Check(Occurrences(full,normalized)==1,card.id+": detailed card rules include each exact synergy condition and limit once");
                    Check(Occurrences(digest,normalized)==1,card.id+": effect digest includes each exact synergy condition and limit once");
                }
                string rules=string.Join("\n",lines);
                Check(rules.Contains("1회")&&rules.Contains("2회")&&rules.Contains("턴"),card.id+": own-turn activation, preparation generation and expiry limits remain visible");
                Check(rules.Contains("피해")&&rules.Contains("6")&&rules.Contains("방어도")&&rules.Contains("회복")&&rules.Contains("4"),card.id+": immediate shared damage/shield/heal budgets remain visible");
                Check(rules.Contains("무료")&&rules.Contains("0코스트")&&rules.Contains("준비")&&rules.Contains("소모"),card.id+": zero-price generation restriction and existing-preparation consumption are explained");
                string preview=DescriptionSummary.CardPreview(card,GameDatabase.Cards);
                Check(DescriptionSummary.PreviewLines(preview)<=5&&DescriptionSummary.PreviewWidth(preview)<=66,card.id+": card summary remains bounded when optional cooperation is present");
                Check(!preview.Contains("턴당 2회까지")&&!preview.Contains("피해 합계 6"),card.id+": full shared limits do not crowd the compact preview");
            }
            Check(DataSnapshot(card)==before,card.id+": synergy role selection, catalog and rendering never rewrite original skill data");
        }
        foreach(string id in new[]{"basic_attack","basic_guard","nia_q","nia_w","nia_e","nia_r","weapon_glove","tactical_blink"})
            Check(!CrossSubjectSynergies.FullDescription(GameDatabase.Card(id)).Any(),id+": unsupported basics, D/F and Nia's independent VF battery do not advertise Echion/Blair's shared support");
        var expected=new Dictionary<string,string[]> {
            {"blair_w",new[]{"에키온","블레어","VF","방어도","3"}},
            {"celine_q",new[]{"아이솔","로지","셀린","테오도르","폭탄","1"}},
            {"kenneth_w",new[]{"아드리아나","기름","3","2회"}},
            {"cathy_w",new[]{"출혈","상처","1","중상","소급"}},
            {"magnus_e",new[]{"밀려난 위치","벽 압박","4","중복"}},
            {"rozzi_q",new[]{"라우라","레온","로지","실비아","2","기본 공격"}},
            {"vanya_q",new[]{"프리야","바냐","꽃","꿈","2"}},
            {"charlotte_w",new[]{"수아","요한","레니","샬럿","회복","2"}}
        };
        foreach(var pair in expected)
        {
            var card=GameDatabase.Card(pair.Key);
            string text=card.owner+"\n"+string.Join("\n",CrossSubjectSynergies.FullDescription(card));
            Check(pair.Value.All(text.Contains),pair.Key+": actual family partners, source resources and characteristic effect are named");
        }
        foreach(string id in new[]{"isol_q","rozzi_r","celine_q","theodore_w","adriana_w","kenneth_w"})
            Check(string.Join("\n",CrossSubjectSynergies.FullDescription(GameDatabase.Card(id))).Contains("40"),id+": scheduled bomb/fire support retains the existing turn-end damage ceiling");
    }
    static void ConditionalRecastDescriptions()
    {
        string[][] gates={new[]{"cathy_q","wounded","2"},new[]{"shoichi_w","dagger","1"},
            new[]{"celine_w","bomb","1"},new[]{"jan_q","unyielding","3"},
            new[]{"jan_e","unyielding","3"},new[]{"karla_w","harpoon","1"},new[]{"bianca_w","blood","2"}};
        foreach(var gate in gates)
        {
            var card=GameDatabase.Card(gate[0]);string before=DataSnapshot(card);
            string summary=DescriptionSummary.Card(card,GameDatabase.Cards),full=CardPresentation.Describe(card,GameDatabase.Cards),preview=DescriptionSummary.CardPreview(card,GameDatabase.Cards);
            string resource=SkillMechanics.ResourceName(card,gate[1]);int threshold=int.Parse(gate[2]);
            foreach(string description in new[]{summary,full})
            {
                Check(description.Contains(resource)&&description.Contains(threshold.ToString()),card.id+": resource reset description retains its actual pre-cast preparation threshold");
                Check(description.Contains("코스트 없이")&&description.Contains("이번 턴")&&description.Contains("1회"),card.id+": conditioned free-use duration and amount remain explicit");
                if(card.freeCastOnHit)Check(description.Contains("적중"),card.id+": reset describes the required landed attack");
            }
            if(preview.Contains("무료")||preview.Contains("코스트 없이"))
                Check(preview.Contains(resource),card.id+": compact preview cannot advertise a free reset while omitting its resource condition");
            if(card.id=="celine_w")
            {
                string fusion=SkillMechanics.ResourceName(card,"fusion");
                Check(summary.Contains(fusion)&&full.Contains(fusion)&&summary.Contains("또는")&&full.Contains("또는"),"Celine's description states plasma OR fusion eligibility rather than requiring both");
            }
            Check(DataSnapshot(card)==before,card.id+": rendering reset conditions does not mutate preparation or grant metadata");
        }
        var cathy=GameDatabase.Card("cathy_q");string cathyFull=CardPresentation.Describe(cathy,GameDatabase.Cards);
        Check(cathyFull.Contains("최대치 3")&&cathyFull.Contains("중상")&&cathyFull.Contains("소모한 뒤"),"Cathy's explanation ties the reset to this cast's maximum-wound transition and preserves the earned grant after consumption");
        var bianca=GameDatabase.Card("bianca_w");string biancaFull=CardPresentation.Describe(bianca,GameDatabase.Cards);
        Check(biancaFull.Contains("절반")&&biancaFull.Contains("최대치는 4"),"Bianca explains why blood two opens the half-resource reset");
        foreach(string id in new[]{"shoichi_e","isaac_w","haze_w","barbara_w","fiora_q","fiora_e"})
        {
            var card=GameDatabase.Card(id);
            Check(!DescriptionSummary.Card(card,GameDatabase.Cards).Contains("코스트 없이")&&!CardPresentation.Describe(card,GameDatabase.Cards).Contains("코스트 없이"),id+": removed reset is not advertised in effect digests or full explanations");
        }
    }
    public static int Main(string[] args)
    {
        try
        {
            var output=new StringBuilder("# 카드 미리보기와 효과 설명 검토\n\n작은 카드와 상세 화면의 요약 모드는 `CardPreview`를 사용합니다. 즉시 행동과 핵심 연계를 짧은 완결 문장으로 표시하며 스크롤하지 않습니다. 아래 효과 정리에는 부가 효과까지 빠짐없이 기록합니다. 전체 설명에는 공통 규칙도 추가됩니다.\n\n");
            foreach(var c in GameDatabase.Cards)
            {
                string sourceBefore=DataSnapshot(c);
                string summary=DescriptionSummary.Card(c,GameDatabase.Cards),full=CardPresentation.Describe(c,GameDatabase.Cards);
                string preview=DescriptionSummary.CardPreview(c,GameDatabase.Cards);
                DebuffFooter(c);
                Text(c.id+"/summary",summary);Text(c.id+"/full",full);
                Text(c.id+"/preview",preview);
                Check(DescriptionSummary.PreviewWidth(preview)<=66,c.id+": preview fits the compact pixel-card budget ("+DescriptionSummary.PreviewWidth(preview)+"): "+preview);
                Check(preview.Split('\n').Length<=2,c.id+": at most two compact preview paragraphs");
                Check(DescriptionSummary.PreviewLines(preview)<=5,c.id+": preview stays within five conservatively estimated native lines: "+preview);
                Check(preview.Split('\n').All(line=>line.EndsWith(".")),c.id+": preview never cuts a sentence");
                Check(!preview.Contains("…")&&!preview.Contains("..."),c.id+": preview has no clipped ellipsis");
                if((c.damage>0||c.block>0||c.heal>0)&&(c.mechanics?.rules?.Length??0)>0)
                    Check(preview.Contains("\n"),c.id+": the defining mechanic is not replaced by an immediate-stat-only preview");
                if(c.damage>0)Check(preview.Contains("피해 "+c.damage),c.id+": preview retains the immediate damage");
                if(c.damage<=0 && c.block>0)Check(preview.Contains("방어도 "+c.block),c.id+": preview retains the leading shield");
                if(c.damage<=0 && c.block<=0 && c.heal>0)Check(preview.Contains("체력 "+c.heal),c.id+": preview retains the leading heal");
                string large=DescriptionSummary.CardPreview(c,GameDatabase.Cards,c.damage>0?99:0,c.block>0?99:0,c.heal>0?99:0);
                Check(DescriptionSummary.PreviewLines(large)<=5,c.id+": upgraded/combat two-digit values still fit the preview");
                if(c.damage>0)Check(large.Contains("피해 99"),c.id+": combat preview preserves its current damage");
                if(c.damage<=0&&c.block>0)Check(large.Contains("방어도 99"),c.id+": combat preview preserves its current shield");
                if(c.damage<=0&&c.block<=0&&c.heal>0)Check(large.Contains("체력 99"),c.id+": combat preview preserves its current heal");
                NoDuplicateDataRules(c.id,c.mechanics?.rules);
                if(c.damage>0)Check(summary.Contains(c.damage.ToString())&&summary.Contains("피해"),c.id+": base damage retained");
                if(c.hits>1 && c.damage>0)Check(summary.Contains(c.hits+"회"),c.id+": hit count retained");
                if(c.block>0)Check(summary.Contains(c.block.ToString())&&summary.Contains("방어도"),c.id+": block retained");
                if(c.heal>0)Check(summary.Contains(c.heal.ToString())&&summary.Contains("회복"),c.id+": healing retained");
                foreach(var r in c.mechanics?.rules ?? new SkillRule[0])
                {
                    Check(!SkillMechanics.IsState(c,r.scaleKey),c.id+": a binary state is not multiplied as a resource");
                    Check(EffectRetained(summary,r.op),c.id+": "+r.op+" effect retained");
                    if(r.op=="gain")Check(summary.Contains("최대 "+SkillMechanics.Clamp(r.cap,1,8)),c.id+": resource maximum retained");
                    if(!string.IsNullOrEmpty(r.conditionKey))Check(SkillMechanics.IsState(c,r.conditionKey)?summary.Contains(SkillMechanics.StateName(c,r.conditionKey)):summary.Contains(r.conditionAmount.ToString()),c.id+": condition value or semantic state retained");
                    if(!string.IsNullOrEmpty(r.scaleKey))Check(summary.Contains("× "+r.amount),c.id+": resource scaling retained");
                }
                foreach(var r in c.statuses ?? new CardStatusRule[0])
                    Check(summary.Contains(StatusMechanics.Name(r.key))&&summary.Contains(r.duration+"턴"),c.id+": named status and duration retained");
                if(c.exhaust)Check(summary.Contains("소멸"),c.id+": exhaust retained");
                if(c.freeCastCount>0)Check(summary.Contains("코스트 없이"),c.id+": recall retained");
                Check(DataSnapshot(c)==sourceBefore,c.id+": rendering grouped descriptions does not mutate card stats, conditions, timings or effects");
                output.Append("## "+c.owner+" · "+c.name+" ["+c.key+"] · "+c.cost+"코스트\n\n**미리보기·요약**\n\n"+preview.Replace("\n","  \n")+"\n\n**전체 효과 정리**\n\n"+summary.Replace("\n","  \n")+"\n\n");
                string debuffFooter=CardPresentation.DebuffRules(c);
                if(!string.IsNullOrEmpty(debuffFooter))output.Append(debuffFooter.Replace("\n","  \n")+"\n\n");
            }
            foreach(var p in GameDatabase.Passives)
            {
                string sourceBefore=DataSnapshot(p);
                string summary=DescriptionSummary.Passive(p);Text(p.id+"/summary",summary);Text(p.id+"/full",p.description);
                NoDuplicateDataRules(p.id,p.mechanics?.rules);TraitEffects(p.id,p.mechanics,summary);
                Check(DataSnapshot(p)==sourceBefore,p.id+": grouping keeps independent passive trigger/cooldown/cap rule data intact");
                output.Append("## 패시브 · "+p.owner+" · "+p.name+"\n\n"+summary.Replace("\n","  \n")+"\n\n");
            }
            foreach(var r in GameDatabase.Runes)
            {
                string sourceBefore=DataSnapshot(r);
                string summary=DescriptionSummary.Rune(r);Text(r.id+"/summary",summary);Text(r.id+"/full",r.description);
                NoDuplicateDataRules(r.id,r.mechanics?.rules);TraitEffects(r.id,r.mechanics,summary);
                Check(DataSnapshot(r)==sourceBefore,r.id+": grouping keeps independent rune trigger/cooldown/cap rule data intact");
                output.Append("## 룬 · "+r.name+"\n\n"+summary.Replace("\n","  \n")+"\n\n");
            }
            foreach(var gear in GameDatabase.Gear)
            {
                string sourceBefore=DataSnapshot(gear);
                string summary=DescriptionSummary.Gear(gear);Text(gear.id+"/summary",summary);Text(gear.id+"/full",gear.description);
                string[] allowedTags={"공격","방어","체력","회복","회피","일반 공격","치명타","스킬","상태이상","코스트"};
                Check(gear.optionTags!=null&&gear.optionTags.Length>0,gear.id+": searchable equipment options are present");
                Check(gear.optionTags.Distinct().Count()==gear.optionTags.Length,gear.id+": searchable option tags are distinct");
                Check(gear.optionTags.All(t=>allowedTags.Contains(t)),gear.id+": tags use the shared ten-option vocabulary");
                Check(gear.critChance>=0&&gear.critChance<=8,gear.id+": equipment critical chance stays modest");
                Check(gear.optionTags.Contains("치명타")== (gear.critChance>0),gear.id+": critical filter agrees with the actual stat");
                if(gear.critChance>0)
                {
                    Check(gear.optionTags.Contains("일반 공격"),gear.id+": critical equipment can be found under basic attacks");
                    Check(summary.Contains(gear.critChance+"%")&&summary.Contains("치명타 확률"),gear.id+": brief equipment description shows the exact critical chance");
                }
                NoDuplicateDataRules(gear.id,gear.mechanics?.rules);TraitEffects(gear.id,gear.mechanics,summary);
                foreach(var r in gear.mechanics?.rules ?? new TraitRule[0])
                {
                    Check(EffectRetained(summary,r.op),gear.id+": "+r.op+" effect retained");
                    if(r.trigger=="after_movement")Check(summary.Contains("이동 기술 사용 후"),gear.id+": movement trigger is distinct from a generic card trigger");
                }
                if(gear.controlResistance>0)Check(summary.Contains("군중 제어")&&summary.Contains(gear.controlResistance.ToString()),gear.id+": resistance retained");
                if(gear.damageDeferral>0)Check(summary.Contains(gear.damageDeferral+"%")&&summary.Contains("3턴"),gear.id+": precise deferred damage retained");
                if(gear.slot==GearSlot.Weapon)Check(summary.Contains(GameDatabase.Card(gear.cardId).name)&&summary.Contains("D 카드 1장"),gear.id+": automatic weapon skill grant retained");
                Check(DataSnapshot(gear)==sourceBefore,gear.id+": grouping does not mutate equipment or independent effect rules");
                output.Append("## 장비 · "+gear.name+"\n\n"+summary.Replace("\n","  \n")+"\n\n");
            }
            var criticalGear=GameDatabase.Gear.Where(g=>g.critChance>0).ToArray();
            Check(criticalGear.Length==7,"Seven original critical equipment items include four new crafting choices");
            foreach(string id in new[]{"meteor_sword","light_insignia","ghillie_suit","alexandros"})
            {
                var gear=GameDatabase.Equipment(id);
                Check(gear!=null&&gear.rarity=="전설"&&gear.critChance>0,id+": new original legendary equipment is registered");
                Check(GameDatabase.Object(gear.objectId)!=null,id+": crafting material exists");
                if(gear.slot==GearSlot.Weapon)Check(WeaponIdentity.CardFor(gear.weaponClass)==gear.cardId,id+": the original weapon class supplies its matching D skill");
            }
            var energyOptions=GameDatabase.Events.SelectMany(e=>e.options).Where(o=>o.effect=="max_energy").ToArray();
            Check(energyOptions.Length==5,"Five character encounters offer permanent maximum-cost growth");
            foreach(var option in energyOptions)
            {
                Check(option.amount==1,"Encounter maximum-cost growth remains one per choice");
                string reward=EventPresentation.RewardSummary(option);
                Check(reward.Contains("이번 탈출 동안")&&reward.Contains("최대 코스트")&&reward.Contains("1 증가"),"Encounter energy rewards explicitly show permanent maximum-cost growth");
                Check(option.description.EndsWith(reward),"The visible encounter choice retains its exact energy reward");
                Text("max_energy/encounter",option.description);
            }
            Check(DescriptionSummary.CardPreview(GameDatabase.Card("basic_attack"),GameDatabase.Cards).Contains("1.5배"),"The basic attack brief preview shows its critical damage multiplier");
            string nia=DescriptionSummary.Card(GameDatabase.Card("nia_w"),GameDatabase.Cards);
            Check(nia.Contains("아케이드 블록 × 4")&&nia.Contains("최대 12")&&nia.Contains("모두 소모")&&nia.Contains("아케이드 드롭")&&nia.Contains("둔화"),"Nia W's scaling, consumption, discount and conditional status are all visible");
            string isaac=DescriptionSummary.Passive(GameDatabase.Passive("isaac_p"));
            Check(isaac.Contains("3회마다")&&isaac.Contains("회복"),"Isaac third basic trigger and heal remain visible");
            string drone=DescriptionSummary.Rune(GameDatabase.Rune("healing_drone"));
            Check(drone.Contains("1/3")&&!drone.Contains("R 카드"),"Healing Drone uses the precise low-health trigger");
            Check(DescriptionSummary.Passive(GameDatabase.Passive("alex_p")).Contains("장착한 무기마다"),"Alex's equipment-dependent extra weapon skill is visible");
            var scaled=DescriptionSummary.Card(GameDatabase.Card("nia_w"),GameDatabase.Cards,18,7,3);
            Check(scaled.Contains("피해 18")&&scaled.Contains("방어도 7")&&scaled.Contains("체력 3"),"Combat preview retains the caller's dynamic numbers");
            var niaPreview=DescriptionSummary.CardPreview(GameDatabase.Card("nia_w"),GameDatabase.Cards,18);
            Check(niaPreview.Contains("피해 18")&&niaPreview.Contains("모두 소모")&&niaPreview.Contains("블록당 피해 4")&&niaPreview.Contains("Q 코스트를 1"),"Nia W brief preview retains live damage and its core block/discount combo");
            var iremPreview=DescriptionSummary.CardPreview(GameDatabase.Card("irem_r"),GameDatabase.Cards);
            Check(iremPreview.Contains("고양이 상태")&&iremPreview.Contains("진입하거나 해제")&&!iremPreview.Contains("고양이를 1"),"Irem R brief preview describes entering/leaving a state");
            var iremFull=CardPresentation.Describe(GameDatabase.Card("irem_r"),GameDatabase.Cards);
            Check(iremFull.Contains("고양이 상태로 진입")&&iremFull.Contains("고양이 상태를 해제"),"Irem R full description spells out both toggle branches");
            Check(DescriptionSummary.Normalize("자원을 1로 만듭니다.")=="자원을 1로 만듭니다.","Rieul-final numerical directional particle stays 1로");
            Check(DescriptionSummary.Normalize("자원을 3로 만듭니다.")=="자원을 3으로 만듭니다.","Consonant-final numerical directional particle becomes 3으로");
            Check(DescriptionSummary.Normalize("자원을 8로 만듭니다.")=="자원을 8로 만듭니다.","Rieul-final numerical directional particle stays 8로");
            Check(DescriptionSummary.Normalize("자원을 3로 만들고, 충전을 0로 만듭니다.")=="자원을 3으로 만들고, 충전을 0으로 만듭니다.","Directional particles remain correct after effect verbs are joined");
            Check(CardPresentation.Describe(GameDatabase.Card("yuki_w"),GameDatabase.Cards).Contains("3으로 만들고"),"Yuki's actual three-button reset retains its correct particle in a grouped effect sentence");
            DebuffEdgeCases();
            GroupedEffectCases();
            CrossSubjectDescriptions();
            ConditionalRecastDescriptions();
            if(args.Length>0)File.WriteAllText(args[0],output.ToString(),new UTF8Encoding(false));
            Console.WriteLine("PASS: "+assertions+" readability/effect assertions; cards="+GameDatabase.Cards.Count+", passives="+GameDatabase.Passives.Count+", runes="+GameDatabase.Runes.Count+", equipment="+GameDatabase.Gear.Count+".");
            var debuffCards=GameDatabase.Cards.Where(c=>ExpectedDebuffs(c).Count>0).ToArray();
            Console.WriteLine("Debuff footer coverage: "+debuffCards.Length+" cards; "+string.Join(", ",debuffCards.SelectMany(c=>ExpectedDebuffs(c)).GroupBy(k=>k).OrderBy(g=>g.Key).Select(g=>g.Key+"="+g.Count())));
            Console.WriteLine("Direct legacy debuffs: "+string.Join(", ",GameDatabase.Cards.Where(c=>c.poison>0||c.weak>0||c.vulnerable>0).Select(c=>c.id+" (poison="+c.poison+", weak="+c.weak+", vulnerable="+c.vulnerable+")")));
            foreach(string id in new[]{"nia_w","nia_r","jackie_q"})Console.WriteLine("\n"+id+"\n"+DescriptionSummary.Card(GameDatabase.Card(id),GameDatabase.Cards));
            foreach(string id in new[]{"isaac_p","alex_p"})Console.WriteLine("\n"+id+"\n"+DescriptionSummary.Passive(GameDatabase.Passive(id)));
            Console.WriteLine("\nhealing_drone\n"+drone);
            return 0;
        }
        catch(Exception ex){Console.Error.WriteLine("FAIL: "+ex);return 1;}
    }
}
