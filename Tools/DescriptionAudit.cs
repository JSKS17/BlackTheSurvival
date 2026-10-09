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
            case "gain":return "증가시킵니다";case "set":return "만듭니다";case "consume":return "소모";
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
    static void TraitEffects(string id,TraitMechanicProfile profile,string summary)
    {
        foreach(var rule in profile?.rules ?? new TraitRule[0])
        {
            Check(!SkillMechanics.IsState(id,rule.scaleKey),id+": a binary state is not multiplied as a resource");
            Check(summary.Contains(EffectWord(rule.op)),id+": "+rule.op+" effect retained");
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
    public static int Main(string[] args)
    {
        try
        {
            var output=new StringBuilder("# 카드 미리보기와 효과 설명 검토\n\n작은 카드와 상세 화면의 요약 모드는 `CardPreview`를 사용합니다. 즉시 행동과 핵심 연계를 짧은 완결 문장으로 표시하며 스크롤하지 않습니다. 아래 효과 정리에는 부가 효과까지 빠짐없이 기록합니다. 전체 설명에는 공통 규칙도 추가됩니다.\n\n");
            foreach(var c in GameDatabase.Cards)
            {
                string summary=DescriptionSummary.Card(c,GameDatabase.Cards),full=CardPresentation.Describe(c,GameDatabase.Cards);
                string preview=DescriptionSummary.CardPreview(c,GameDatabase.Cards);
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
                    Check(summary.Contains(EffectWord(r.op)),c.id+": "+r.op+" effect retained");
                    if(r.op=="gain")Check(summary.Contains("최대 "+SkillMechanics.Clamp(r.cap,1,8)),c.id+": resource maximum retained");
                    if(!string.IsNullOrEmpty(r.conditionKey))Check(SkillMechanics.IsState(c,r.conditionKey)?summary.Contains(SkillMechanics.StateName(c,r.conditionKey)):summary.Contains(r.conditionAmount.ToString()),c.id+": condition value or semantic state retained");
                    if(!string.IsNullOrEmpty(r.scaleKey))Check(summary.Contains("× "+r.amount),c.id+": resource scaling retained");
                }
                foreach(var r in c.statuses ?? new CardStatusRule[0])
                    Check(summary.Contains(StatusMechanics.Name(r.key))&&summary.Contains(r.duration+"턴"),c.id+": named status and duration retained");
                if(c.exhaust)Check(summary.Contains("소멸"),c.id+": exhaust retained");
                if(c.freeCastCount>0)Check(summary.Contains("코스트 없이"),c.id+": recall retained");
                output.Append("## "+c.owner+" · "+c.name+" ["+c.key+"] · "+c.cost+"코스트\n\n**미리보기·요약**\n\n"+preview.Replace("\n","  \n")+"\n\n**전체 효과 정리**\n\n"+summary.Replace("\n","  \n")+"\n\n");
            }
            foreach(var p in GameDatabase.Passives)
            {
                string summary=DescriptionSummary.Passive(p);Text(p.id+"/summary",summary);Text(p.id+"/full",p.description);
                NoDuplicateDataRules(p.id,p.mechanics?.rules);TraitEffects(p.id,p.mechanics,summary);
                output.Append("## 패시브 · "+p.owner+" · "+p.name+"\n\n"+summary.Replace("\n","  \n")+"\n\n");
            }
            foreach(var r in GameDatabase.Runes)
            {
                string summary=DescriptionSummary.Rune(r);Text(r.id+"/summary",summary);Text(r.id+"/full",r.description);
                NoDuplicateDataRules(r.id,r.mechanics?.rules);TraitEffects(r.id,r.mechanics,summary);
                output.Append("## 룬 · "+r.name+"\n\n"+summary.Replace("\n","  \n")+"\n\n");
            }
            foreach(var gear in GameDatabase.Gear)
            {
                string summary=DescriptionSummary.Gear(gear);Text(gear.id+"/summary",summary);Text(gear.id+"/full",gear.description);
                NoDuplicateDataRules(gear.id,gear.mechanics?.rules);TraitEffects(gear.id,gear.mechanics,summary);
                foreach(var r in gear.mechanics?.rules ?? new TraitRule[0])
                {
                    Check(summary.Contains(EffectWord(r.op)),gear.id+": "+r.op+" effect retained");
                    if(r.trigger=="after_movement")Check(summary.Contains("이동 기술 사용 후"),gear.id+": movement trigger is distinct from a generic card trigger");
                }
                if(gear.controlResistance>0)Check(summary.Contains("군중 제어")&&summary.Contains(gear.controlResistance.ToString()),gear.id+": resistance retained");
                if(gear.damageDeferral>0)Check(summary.Contains(gear.damageDeferral+"%")&&summary.Contains("3턴"),gear.id+": precise deferred damage retained");
                if(gear.slot==GearSlot.Weapon)Check(summary.Contains(GameDatabase.Card(gear.cardId).name)&&summary.Contains("D 카드 1장"),gear.id+": automatic weapon skill grant retained");
                output.Append("## 장비 · "+gear.name+"\n\n"+summary.Replace("\n","  \n")+"\n\n");
            }
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
            if(args.Length>0)File.WriteAllText(args[0],output.ToString(),new UTF8Encoding(false));
            Console.WriteLine("PASS: "+assertions+" readability/effect assertions; cards="+GameDatabase.Cards.Count+", passives="+GameDatabase.Passives.Count+", runes="+GameDatabase.Runes.Count+", equipment="+GameDatabase.Gear.Count+".");
            foreach(string id in new[]{"nia_w","nia_r","jackie_q"})Console.WriteLine("\n"+id+"\n"+DescriptionSummary.Card(GameDatabase.Card(id),GameDatabase.Cards));
            foreach(string id in new[]{"isaac_p","alex_p"})Console.WriteLine("\n"+id+"\n"+DescriptionSummary.Passive(GameDatabase.Passive(id)));
            Console.WriteLine("\nhealing_drone\n"+drone);
            return 0;
        }
        catch(Exception ex){Console.Error.WriteLine("FAIL: "+ex);return 1;}
    }
}
