using System;
using System.Collections.Generic;
using System.Linq;

namespace Lumia
{
    public sealed class TacticalSkillDefinition
    {
        public string id, name, upgradeDescription;
        public int sourceCode;
        public bool cobalt, legacy;
    }
    public sealed class TacticalTurnStart
    {
        public int reward, evasion;
        public List<SkillPulse> pulses=new List<SkillPulse>();
    }

    // Original identities are shared by both actors; module upgrades always copy one card.
    public static class TacticalSkills
    {
        static TacticalSkillDefinition D(string id,string name,int code,string upgrade,bool cobalt=false,bool legacy=false)
            => new TacticalSkillDefinition { id="tactical_"+id,name=name,sourceCode=code,upgradeDescription=upgrade,cobalt=cobalt,legacy=legacy };
        public static readonly TacticalSkillDefinition[] Definitions = {
            D("blink","블링크",30,"회피 효과가 1턴 더 지속됩니다."),
            D("quake","퀘이크",40,"적중한 적에게 2턴간 여진 피해를 줍니다."),
            D("protocol","프로토콜 위반",50,"고정 피해가 6, 방어도가 6 증가합니다."),
            D("redstorm","붉은 폭풍",60,"다음 기본 공격 강화 피해가 4 증가합니다."),
            D("artifact","아티팩트",80,"경직이 끝나는 다음 자기 턴에 코스트를 1 회복하고 카드를 1장 뽑습니다."),
            D("nullification","무효화",90,"다음 자기 턴까지 새로 받는 군중 제어의 지속 시간이 절반으로 줄어듭니다(최소 1턴)."),
            D("soul","강한 결속",110,"결속 에너지의 50%를 더 활용하며 흡혈 효율이 높아집니다."),
            D("strider","스트라이더 - A13",120,"추격 공격이 적중하면 둔화를 1턴 부여합니다."),
            D("truth_blade","진실의 칼날",130,"같은 피해를 주는 칼날을 하나 더 발사합니다."),
            D("false_oath","거짓 서약",140,"공격 강화량이 2 증가하고 소모한 체력을 더 빨리 회복합니다.",legacy:true),
            D("wind","치유의 바람",150,"2턴간 체력을 추가로 5씩 회복합니다."),
            D("repulsor","리펄서 미사일",160,"동일한 고정 피해의 미사일을 1발 더 발사합니다."),
            D("plasma","플라즈마 대시",170,"적중 시 방어력 감소를 2턴 부여합니다."),
            D("lightwing","라이트 윙",190,"기본 공격 적중으로 회피 효과를 연장합니다(턴당 1회, 최대 3턴)."),
            D("gravity","중력장",500020,"흡인 피해가 4 증가하며 기절이 1턴 길어집니다.",true),
            D("rolling_thunder","롤링썬더",500030,"회피율이 10%p 증가하고 카드를 1장 더 뽑습니다.",true),
            D("truth_burst","폭진",500040,"적중 보상으로 코스트를 1 더 회복합니다.",true,true),
            D("impulse","임펄스",500050,"다음 턴 돌진의 피해가 5 증가하고 방어도 5를 얻습니다.",true),
            D("prayer","기원",500060,"방어도가 8 증가하고 2턴간 방어도 3을 얻습니다.",true,true),
            D("fissure","대지분쇄",500070,"피해가 4 증가하고 둔화가 1턴 길어집니다.",true),
            D("punch","힘껏 펀치",500080,"피해가 5 증가하고 넉백과 함께 방어력 감소를 1턴 부여합니다.",true),
            D("meteor","메테오",500090,"메테오의 고정 피해가 8 증가합니다.",true),
            D("lightning_shield","라이트닝 쉴드",500100,"방어도가 5, 피격 반격 피해가 2 증가합니다.",true,true),
            D("meditation","블레싱: 명상",500110,"즉시 회복량이 최대 체력의 5%만큼 증가합니다.",true,true),
            D("lock_on","부착",500240,"첫 투사체와 재사용 돌진의 피해가 각각 4 증가합니다.",true),
            D("asthenia","쇠약",500270,"둔화와 공격력 감소가 1턴 길어집니다.",true)
        };
        public static IEnumerable<CardDef> CardDefinitions => GameDatabase.Cards.Where(IsTactical);
        public static TacticalSkillDefinition Definition(string reference) => Definitions.FirstOrDefault(x=>x.id==CardInstances.BaseId(reference));
        public static bool IsTactical(string reference) => Definition(reference)!=null;
        public static bool IsTactical(CardDef card) => card!=null && IsTactical(card.id);
        public static bool IsModuleUpgraded(CardDef card) => IsTactical(card) && card.tacticalEnhanced;
        public static string ModuleUpgradeDescription(CardDef card)
        {
            if(Id(card)=="tactical_lightning_shield")return "방어도가 5, 피격 반격 피해가 "+Math.Max(0,Math.Min(8,(card.mechanics?.rules.FirstOrDefault()?.amount??0)+2)-(card.mechanics?.rules.FirstOrDefault()?.amount??0))+" 증가합니다(반격 상한 8).";
            return Definition(card?.id)?.upgradeDescription ?? "";
        }
        public static bool TrueDamage(CardDef card) => card!=null && (card.id=="tactical_protocol" || card.id=="tactical_repulsor");
        public static bool IsDelayed(CardDef card) => card!=null && new[]{"tactical_protocol","tactical_gravity","tactical_impulse","tactical_meteor"}.Contains(Id(card));
        static string Id(CardDef card) => CardInstances.BaseId(card?.id) ?? "";
        static string P(string text) => CardPresentation.WithParticle(text,"를","을");
        static SkillRule R(string op,string key,int amount,int duration=1,bool hit=false,int delay=1)
            => new SkillRule {op=op,key=key,label=key,amount=amount,duration=duration,onHit=hit,delay=delay};
        static CardStatusRule S(string key,int duration=1,string timing="cast",bool hit=true)
            => new CardStatusRule {op="status",key=key,label=StatusMechanics.Name(key),duration=duration,timing=timing,onHit=hit};
        static void Add(List<CardDef> cards,string id,int cd,int damage=0,int block=0,int heal=0,int evasion=0,int draw=0)
        {
            var d=Definitions.First(x=>x.id=="tactical_"+id);
            cards.Add(new CardDef {id=d.id,name=d.name,owner="전술",key="F",category="tactical",cooldown=cd,damage=damage,block=block,heal=heal,evasion=evasion,draw=draw,duration=2,exhaust=true});
        }
        public static void AddCards(List<CardDef> cards)
        {
            Add(cards,"quake",18,damage:12); Add(cards,"protocol",30,damage:12,block:10);
            Add(cards,"redstorm",12,evasion:20); Add(cards,"artifact",35); Add(cards,"nullification",20,evasion:20);
            Add(cards,"soul",30,evasion:15); Add(cards,"strider",12,evasion:20); Add(cards,"truth_blade",20,damage:12,evasion:15);
            Add(cards,"false_oath",20); Add(cards,"repulsor",30,damage:12,evasion:20); Add(cards,"lightwing",20,draw:1);
            Add(cards,"gravity",30); Add(cards,"rolling_thunder",18,evasion:45,draw:1); Add(cards,"truth_burst",18,damage:17);
            Add(cards,"impulse",20); Add(cards,"prayer",30,block:18); Add(cards,"fissure",30,damage:13);
            Add(cards,"punch",30,damage:17); Add(cards,"meteor",35); Add(cards,"lightning_shield",30,block:12);
            Add(cards,"meditation",30); Add(cards,"lock_on",12,damage:7); Add(cards,"asthenia",20);
        }
        public static void Configure(List<CardDef> cards)
        {
            foreach(var c in cards.Where(IsTactical))
            {
                c.name=Definition(c.id).name; c.mechanics=new SkillMechanicProfile(); c.statuses=new CardStatusRule[0];
                c.movement=new[]{"tactical_blink","tactical_redstorm","tactical_repulsor","tactical_plasma","tactical_impulse","tactical_lock_on"}.Contains(c.id);
                switch(c.id)
                {
                    case "tactical_quake": c.statuses=new[]{S("slow",2)}; break;
                    case "tactical_protocol": c.mechanics.rules=new[]{R("delayed_damage","프로토콜 폭발",c.damage)};c.damage=0;break;
                    case "tactical_redstorm": c.mechanics.rules=new[]{R("empower_basic","폭풍의 사거리",8,2)}; break;
                    case "tactical_plasma": c.statuses=new[]{S("slow",2)}; break;
                    case "tactical_gravity": c.mechanics.rules=new[]{R("delayed_damage","중력 붕괴",WeaponDamageBalance.ReducedDamage(20))}; c.statuses=new[]{S("pull",1,"중력 붕괴",false),S("stun",1,"중력 붕괴",false)}; break;
                    case "tactical_impulse": c.mechanics.rules=new[]{R("delayed_damage","임펄스 돌진",WeaponDamageBalance.ReducedDamage(20))}; c.statuses=new[]{S("knockback",1,"임펄스 돌진",false)}; break;
                    case "tactical_fissure": c.statuses=new[]{S("stun"),S("slow")}; break;
                    case "tactical_punch": c.statuses=new[]{S("knockback")}; break;
                    case "tactical_meteor": c.mechanics.rules=new[]{R("delayed_damage","메테오 낙하",WeaponDamageBalance.ReducedDamage(32))}; c.statuses=new[]{S("stun",1,"메테오 낙하",false)}; break;
                    case "tactical_lightning_shield": c.mechanics.rules=new[]{R("counter","번개 방패",WeaponDamageBalance.ReducedDamage(6),2)}; break;
                    case "tactical_lock_on": c.exhaust=false; c.freeCastCount=1; c.freeCastTargets=new[]{c.id}; c.freeCastOnHit=true; c.freeCastConditionNote="첫 투사체가 적중했을 때만 이번 턴 돌진을 무료로 재사용합니다. 같은 원천의 무료 사용권은 턴당 1회입니다."; break;
                    case "tactical_asthenia": c.statuses=new[]{S("slow",1,hit:false),S("attack_down",1,hit:false)}; break;
                }
                c.effect="tactical:"+c.id.Substring(9); c.effectColor=Definition(c.id).cobalt?"9BD4FF":"83E3D7";
            }
        }

        public static CardDef ApplyModuleUpgrade(CardDef source)
        {
            var c=StrategyMechanics.Copy(source); if(!IsTactical(c) || c.tacticalEnhanced)return c;
            c.statuses=(source.statuses??new CardStatusRule[0]).Select(x=>{var y=new CardStatusRule();foreach(var f in typeof(CardStatusRule).GetFields())f.SetValue(y,f.GetValue(x));return y;}).ToArray();
            c.tacticalEnhanced=true;
            switch(Id(c))
            {
                case "tactical_blink": c.duration++; break;
                case "tactical_quake": c.mechanics.rules=new[]{R("summon","퀘이크 여진",WeaponDamageBalance.ReducedDamage(5),2,true)}; break;
                case "tactical_protocol": c.mechanics.rules[0].amount+=6;c.block+=6;break;
                case "tactical_redstorm": c.mechanics.rules[0].amount+=4;break;
                case "tactical_truth_blade": case "tactical_repulsor": c.hits++;break;
                case "tactical_wind": c.mechanics.rules=new[]{R("hot","치유의 바람",5,2)};break;
                case "tactical_plasma": c.statuses=c.statuses.Concat(new[]{S("armor_break",2)}).ToArray();break;
                case "tactical_gravity": c.mechanics.rules[0].amount+=4;c.statuses.First(x=>x.key=="stun").duration++;break;
                case "tactical_rolling_thunder": c.evasion+=10;c.draw++;break;
                case "tactical_impulse": c.mechanics.rules[0].amount+=5;c.block+=5;break;
                case "tactical_prayer": c.block+=8;c.mechanics.rules=new[]{R("guard","기원의 보호",3,2)};break;
                case "tactical_fissure": c.damage+=4;c.statuses.First(x=>x.key=="slow").duration++;break;
                case "tactical_punch": c.damage+=5;c.statuses=c.statuses.Concat(new[]{S("armor_break")}).ToArray();break;
                case "tactical_meteor": c.mechanics.rules[0].amount+=8;break;
                case "tactical_lightning_shield": c.block+=5;c.mechanics.rules[0].amount+=2;break;
                case "tactical_lock_on": c.damage+=4;break;
                case "tactical_asthenia": foreach(var s in c.statuses)s.duration++;break;
            }
            foreach(var r in c.mechanics.rules)
            {
                int max=r.op=="counter"?8:r.op=="empower_basic"?16:r.op=="delayed_damage"?40:new[]{"summon","hot","guard","burn","bleed"}.Contains(r.op)?10:int.MaxValue;
                r.amount=Math.Min(max,r.amount);
            }
            c.description=CardPresentation.Describe(c,GameDatabase.Cards);return c;
        }
        public static bool CanAct(SkillActorState actor) => !Has(actor,"stasis");
        static SkillTimedEffect Effect(SkillActorState actor,string key) => actor?.effects?.FirstOrDefault(x=>x.kind=="tactical_buff" && x.key==key && x.remaining>0);
        public static bool Has(SkillActorState actor,string key) => Effect(actor,key)!=null;
        static void Buff(SkillActorState actor,CardDef card,string key,string label,int amount,int turns)
        {
            var b=Effect(actor,key);if(b==null){b=new SkillTimedEffect {owner="전술",kind="tactical_buff",key=key};actor.effects.Add(b);}
            b.sourceCard=card.id;b.label=label;b.amount=amount;b.remaining=Math.Max(b.remaining,turns);
        }
        public static int EvasionBonus(SkillActorState actor) => Math.Max(Effect(actor,"lightwing")?.amount ?? 0,Effect(actor,"impulse_active")?.amount??0);
        public static int IncomingDamage(SkillActorState actor,int amount) => Has(actor,"stasis")?0:Math.Max(0,amount);
        public static int Healing(CardDef card,int maxHp) => Id(card)=="tactical_meditation"?Math.Max(1,maxHp*(card.tacticalEnhanced?25:20)/100):0;
        public static int LifeSteal(SkillActorState actor,int damage) => Math.Min(8,Math.Max(0,damage)*(Effect(actor,"lifesteal")?.amount??0)/100);
        public static int SoulCharge(SkillActorState actor) => Math.Max(0,Math.Min(30,actor?.resources?.FirstOrDefault(x=>x.owner=="전술"&&x.key=="soul_charge")?.amount??0));
        public static void RecordDamage(SkillActorState actor,int damage)
        {
            if(damage<=0)return;var r=actor.resources.FirstOrDefault(x=>x.owner=="전술"&&x.key=="soul_charge");
            if(r==null){r=new SkillResource {owner="전술",key="soul_charge",label="결속 에너지",cap=30};actor.resources.Add(r);}r.amount=Math.Min(30,r.amount+damage);
        }
        public static SkillInstantBonus Bonuses(SkillActorState actor,CardDef card,bool landed)
        {
            var b=new SkillInstantBonus();if(card==null || actor==null)return b;
            if(card.damage>0){b.damage+=Effect(actor,"oath")?.amount??0;b.damage+=Effect(actor,"strider")?.amount??0;}
            if(Id(card)=="tactical_soul")b.heal=Math.Min(18,SoulCharge(actor)*(card.tacticalEnhanced?3:2)/5);
            return b;
        }
        public static void AfterCard(SkillActorState actor,SkillActorState before,CardDef card,bool landed)
        {
            if(card.damage>0 && landed)actor.effects.RemoveAll(x=>x.kind=="tactical_buff"&&x.key=="strider");
            if(card.id=="basic_attack" && landed)
            {var wing=Effect(actor,"lightwing");if(wing!=null && wing.tacticalEnhanced && wing.uses==0){wing.remaining=Math.Min(3,wing.remaining+1);wing.uses=1;}}
            switch(Id(card))
            {
                case "tactical_artifact": Buff(actor,card,"stasis","아티팩트 경직",card.tacticalEnhanced?1:0,1);break;
                case "tactical_soul":
                    int charge=SoulCharge(before);
                    foreach(var r in actor.resources.Where(x=>x.owner=="전술"&&x.key=="soul_charge"))r.amount=0;
                    Buff(actor,card,"lifesteal","강한 결속 흡혈",charge>0?(card.tacticalEnhanced?35:25):0,2);break;
                case "tactical_strider": Buff(actor,card,"strider",card.tacticalEnhanced?"추격 공격 · 둔화":"추격 공격",WeaponDamageBalance.ReducedDamage(10),2);Effect(actor,"strider").tacticalEnhanced=card.tacticalEnhanced;break;
                case "tactical_false_oath": Buff(actor,card,"oath","거짓 서약",card.tacticalEnhanced?6:4,2);break;
                case "tactical_lightwing": Buff(actor,card,"lightwing",card.tacticalEnhanced?"라이트 윙 · 연장":"라이트 윙",30,2);Effect(actor,"lightwing").tacticalEnhanced=card.tacticalEnhanced;break;
                case "tactical_nullification": if(card.tacticalEnhanced)Buff(actor,card,"tenacity","무효화 제어 저항",1,1);break;
                case "tactical_meditation":
                    var hot=actor.effects.FirstOrDefault(x=>x.sourceCard==card.id && x.key=="명상의 회복");
                    if(hot==null)actor.effects.Add(new SkillTimedEffect {owner="전술",kind="hot",key="명상의 회복",label="명상의 회복",sourceCard=card.id,amount=5,remaining=2});break;
            }
            if(IsDelayed(card))
                foreach(var pending in actor.effects.Where(x=>x.sourceCard==card.id && x.kind=="delayed_damage"))pending.kind="tactical_pending";
        }
        // Returns the enhanced artifact's deferred draw/energy reward, once.
        public static int StartTurn(SkillActorState actor)
        {
            int reward=Effect(actor,"stasis")?.amount??0;
            foreach(var b in actor.effects.Where(x=>x.kind=="tactical_buff")){b.remaining--;b.uses=0;}
            actor.effects.RemoveAll(x=>x.kind=="tactical_buff"&&x.remaining<=0);return reward;
        }
        public static List<SkillPulse> TurnStartPulses(SkillActorState actor)
        {
            var pending=actor.effects.Where(x=>x.kind=="tactical_pending").ToArray();
            var result=pending.Select(x=>new SkillPulse {sourceCard=x.sourceCard,key=x.key,kind=x.sourceCard=="tactical_meteor" || x.sourceCard=="tactical_protocol"?"tactical_true":"delayed_damage",amount=x.amount}).ToList();
            foreach(var p in pending)actor.effects.Remove(p);return result;
        }
        public static TacticalTurnStart BeginTurn(SkillActorState actor)
        {
            var result=new TacticalTurnStart {reward=StartTurn(actor),pulses=TurnStartPulses(actor)};
            if(result.pulses.Any(x=>x.sourceCard=="tactical_impulse"))
            {
                result.evasion=35;
                actor.effects.Add(new SkillTimedEffect {owner="전술",sourceCard="tactical_impulse",kind="tactical_buff",key="impulse_active",label="임펄스 가속",amount=35,remaining=1});
            }
            return result;
        }
        public static string FieldEffectDescription(SkillMechanicToken token)
        {
            if(token?.kind=="tactical_pending")return "다음 자기 턴 시작에 피해 "+token.amount+"를 줍니다."+(token.sourceCard=="tactical_meteor"?" 방어도를 무시하고 기절을 부여합니다.":token.sourceCard=="tactical_protocol"?" 방어도를 무시합니다.":token.sourceCard=="tactical_gravity"?" 끌어당김과 기절을 부여합니다.":" 넉백을 부여하고 회피율이 35% 증가합니다.");
            if(token?.kind!="tactical_buff")return "";
            switch(token.key)
            {
                case "stasis":return "다음 자기 턴까지 받는 체력 피해가 0이며 카드를 더 사용할 수 없습니다.";
                case "lifesteal":return "직접 공격으로 준 체력 피해의 "+token.amount+"%를 회복합니다(카드당 최대 8).";
                case "strider":return "다음으로 적중하는 공격 카드에 추가 피해 "+token.amount+"를 줍니다.";
                case "oath":return "공격 카드에 추가 피해 "+token.amount+"를 줍니다.";
                case "tenacity":return "새로 받는 군중 제어의 지속 시간을 절반으로 줄입니다(최소 1턴).";
                case "lightwing":case "impulse_active":return "회피율이 "+token.amount+"% 증가합니다. 다른 카드 회피 효과와는 가장 높은 값만 적용합니다.";
                default:return "남은 자기 턴 시작 횟수에 따라 유지되는 전술 효과입니다.";
            }
        }
        public static bool IgnoreControl(SkillActorState actor)
        {
            var b=Effect(actor,"tenacity");if(b==null || b.amount<=0)return false;b.amount--;return true;
        }

        public static List<string> Description(CardDef c,int damage=-1,int block=-1,int heal=-1)
        {
            var lines=new List<string>();int d=damage<0?c.damage:damage,b=block<0?c.block:block,h=heal<0?c.heal:heal;
            if(d>0)lines.Add(P((TrueDamage(c)?"방어도를 무시하는 고정 피해 ":"피해 ")+d)+(c.hits>1?" "+c.hits+"회 줍니다.":" 줍니다."));
            if(b>0)lines.Add(P("방어도 "+b)+" 얻습니다.");if(h>0)lines.Add(P("체력 "+h)+" 회복합니다.");
            if(c.evasion>0)lines.Add(c.duration+"턴간 회피율이 "+c.evasion+"% 증가합니다.");if(c.draw>0)lines.Add("카드 "+c.draw+"장을 뽑습니다.");
            bool pending=IsDelayed(c);
            if(!pending)lines.AddRange(SkillMechanics.Describe(c));
            if(!pending)lines.AddRange(StatusMechanics.Describe(c));
            switch(Id(c))
            {
                case "tactical_artifact": lines.Add("다음 자기 턴까지 경직되어 카드를 더 사용할 수 없고 받는 체력 피해가 0이 됩니다."+(c.tacticalEnhanced?" 경직이 끝날 때 코스트 1을 회복하고 카드 1장을 뽑습니다.":""));break;
                case "tactical_protocol": lines.Add("개조 E.M.O.T.E를 설치하여 다음 자기 턴 시작에 방어도를 무시하는 "+P("고정 피해 "+c.mechanics.rules[0].amount)+" 줍니다. 원작의 일시적인 체력 증가는 이 카드의 방어도로 전환합니다.");break;
                case "tactical_nullification": lines.Add("자신의 중독·약화·취약·상태이상과 지속 피해를 해제합니다."+(c.tacticalEnhanced?" 다음 자기 턴까지 새로 받는 군중 제어의 지속 시간을 절반으로 줄입니다(최소 1턴).":""));break;
                case "tactical_soul": lines.Add("이번 전투에서 이 카드를 보유한 채 잃은 체력만큼 결속 에너지를 얻습니다(최대 30). 모두 소모하여 "+(c.tacticalEnhanced?"60":"40")+"%만큼 체력을 회복하며, 에너지를 소모했다면 2턴간 직접 공격 피해의 "+(c.tacticalEnhanced?"35":"25")+"%를 흡혈합니다(카드당 최대 8). ");break;
                case "tactical_strider": lines.Add("2턴 내 다음으로 적중하는 공격 카드에 추가 피해 8을 줍니다."+(c.tacticalEnhanced?" 이 추격 공격이 적중하면 둔화를 1턴 부여합니다.":""));break;
                case "tactical_false_oath": lines.Add("최대 체력의 10%만큼 체력을 소모합니다(최대 20, 최소 체력 1). 2턴간 공격 카드에 추가 피해 "+(c.tacticalEnhanced?6:4)+"를 주고 소모한 체력을 "+(c.tacticalEnhanced?"이번 턴 종료에":"2턴에 걸쳐")+" 회복합니다. 턴 종료의 회복·방어 효과는 합계 25까지 적용됩니다.");break;
                case "tactical_lightwing": lines.Add("자신의 둔화를 해제하고 2턴간 회피율을 30% 높입니다."+(c.tacticalEnhanced?" 기본 공격 적중 시 지속 시간을 1턴 늘립니다(턴당 1회, 최대 3턴).": ""));break;
                case "tactical_truth_burst": lines.Add("공격이 적중하면 코스트를 "+(c.tacticalEnhanced?2:1)+" 회복합니다. 빗나가면 체력을 5 소모합니다(최소 체력 1). ");break;
                case "tactical_impulse": lines.Add("다음 자기 턴 시작에 돌진하여 "+P("피해 "+c.mechanics.rules[0].amount)+" 주고 넉백을 1턴 부여하며 회피율이 35% 증가합니다(1턴). ");break;
                case "tactical_lightning_shield": lines.Add("자신의 중독·약화·취약·상태이상과 지속 피해를 해제합니다.");break;
                case "tactical_meditation": lines.Add("최대 체력의 "+(c.tacticalEnhanced?25:20)+"%를 즉시 회복하고, 2턴간 체력을 5씩 회복합니다.");break;
                case "tactical_lock_on": lines.Add("첫 공격이 적중했을 때만 이번 턴 무료로 재사용할 수 있습니다. 무료 사용권은 턴당 1회이며 재사용 시 다시 획득하지 않습니다.");break;
                case "tactical_meteor": lines.Add("다음 자기 턴 시작에 방어도를 무시하는 "+P("고정 피해 "+c.mechanics.rules[0].amount)+" 주고 기절을 1턴 부여합니다.");break;
                case "tactical_gravity": lines.Add("다음 자기 턴 시작에 "+P("피해 "+c.mechanics.rules[0].amount)+" 주고 끌어당김을 1턴, 기절을 "+c.statuses.First(x=>x.key=="stun").duration+"턴 부여합니다.");break;
            }
            if(c.exhaust)lines.Add("사용 후 소멸합니다.");return DescriptionSummary.CleanLines(lines);
        }
        public static string Brief(CardDef c,int damage=-1,int block=-1,int heal=-1)
        {
            int d=damage<0?c.damage:damage,b=block<0?c.block:block,h=heal<0?c.heal:heal;string t="";
            switch(Id(c))
            {
                case "tactical_blink":t="방어 "+b+". 회피 +"+c.evasion+"% · "+c.duration+"턴.";break;
                case "tactical_quake":t="피해 "+d+", 둔화 2턴."+(c.tacticalEnhanced?" 여진 피해 4 · 2턴.":"");break;
                case "tactical_protocol":t="방어 "+b+". 다음 내 턴 고정 피해 "+c.mechanics.rules[0].amount+".";break;
                case "tactical_redstorm":t="다음 기본 공격 피해 +"+(c.mechanics.rules.FirstOrDefault()?.amount??8)+". 회피 +"+c.evasion+"% · 2턴.";break;
                case "tactical_artifact":t="다음 내 턴까지 체력 피해 무시·추가 행동 불가."+(c.tacticalEnhanced?" 다음 턴 코스트·뽑기 +1.":"");break;
                case "tactical_nullification":t="내 디버프를 모두 해제합니다. 회피 +"+c.evasion+"%."+(c.tacticalEnhanced?" 군중 제어 기간 절반.":"");break;
                case "tactical_soul":t="잃은 체력을 결속 에너지로 축적·소모해 회복합니다. 2턴간 직접 공격 흡혈·회피 +"+c.evasion+"%.";break;
                case "tactical_strider":t="다음 적중 공격 피해 +8. 회피 +"+c.evasion+"%."+(c.tacticalEnhanced?" 추격 적중 시 둔화.":"");break;
                case "tactical_truth_blade":t="피해 "+d+(c.hits>1?" × "+c.hits:"")+". 회피 +"+c.evasion+"%.";break;
                case "tactical_false_oath":t="체력 10%(최대 20) 소모·회복 예약. 2턴간 공격 피해 +"+(c.tacticalEnhanced?6:4)+".";break;
                case "tactical_wind":t="체력 "+h+" 회복, 방어 "+b+"."+(c.tacticalEnhanced?" 2턴간 5씩 추가 회복.":"");break;
                case "tactical_repulsor":t="고정 피해 "+d+(c.hits>1?" × "+c.hits:"")+". 회피 +"+c.evasion+"%.";break;
                case "tactical_plasma":t="피해 "+d+", 둔화 2턴. 회피 +"+c.evasion+"%."+(c.tacticalEnhanced?" 방어력 감소 2턴.":"");break;
                case "tactical_lightwing":t="둔화 해제. 2턴간 회피 +30%, 카드 "+c.draw+"장을 뽑습니다."+(c.tacticalEnhanced?" 기본 공격 적중 시 기간 연장.":"");break;
                case "tactical_gravity":t="다음 내 턴 피해 "+c.mechanics.rules[0].amount+"·끌어당김·기절.";break;
                case "tactical_rolling_thunder":t="회피 +"+c.evasion+"% · 2턴. 카드 "+c.draw+"장을 뽑습니다.";break;
                case "tactical_truth_burst":t="피해 "+d+". 적중 시 코스트 +"+(c.tacticalEnhanced?2:1)+", 빗나가면 체력 -5.";break;
                case "tactical_impulse":t="다음 내 턴 피해 "+c.mechanics.rules[0].amount+"·넉백·회피 +35%."+(b>0?" 방어 "+b+".":"");break;
                case "tactical_prayer":t="방어 "+b+"."+(c.tacticalEnhanced?" 2턴간 방어 +3.":"");break;
                case "tactical_fissure":t="피해 "+d+", 기절 1턴·둔화 "+c.statuses.First(x=>x.key=="slow").duration+"턴.";break;
                case "tactical_punch":t="피해 "+d+", 넉백 1턴."+(c.tacticalEnhanced?" 방어력 감소 1턴.":"");break;
                case "tactical_meteor":t="다음 내 턴 고정 피해 "+c.mechanics.rules[0].amount+"·기절 1턴.";break;
                case "tactical_lightning_shield":t="내 디버프 해제, 방어 "+b+". 피격 시 반격 "+c.mechanics.rules[0].amount+"(턴당 1회).";break;
                case "tactical_meditation":t="최대 체력 "+(c.tacticalEnhanced?25:20)+"% 회복. 2턴간 체력 +5.";break;
                case "tactical_lock_on":t="피해 "+d+". 적중하면 F를 이번 턴 1회 무료로 돌진 재사용.";break;
                case "tactical_asthenia":t="적에게 둔화·공격력 감소를 "+c.statuses[0].duration+"턴 부여합니다.";break;
            }
            var rareGuard=c.mechanics?.rules.FirstOrDefault(rule=>rule.op=="guard" && rule.key=="rare_guard");
            if(rareGuard!=null)t+=" 턴 끝 방어 +"+rareGuard.amount+" · "+rareGuard.duration+"턴.";
            return t+(c.exhaust?" 소멸.":"");
        }
    }
}
