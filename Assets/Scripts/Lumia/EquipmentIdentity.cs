using System;
using System.Collections.Generic;
using System.Linq;

namespace Lumia
{
    /// <summary>Item names and unique-effect identities follow the recorded original item tooltips.</summary>
    public static class EquipmentIdentity
    {
        static void Add(List<GearDef> gear,string id,string name,string material,string weaponClass=null,GearSlot slot=GearSlot.Weapon)
        {
            gear.Add(new GearDef{id=id,name=name,objectId=material,slot=slot,weaponClass=weaponClass,cardId=WeaponIdentity.CardFor(weaponClass)});
        }
        public static void AddGear(List<GearDef> gear)
        {
            Add(gear,"fragarach","프라가라흐","meteorite","단검");
            Add(gear,"doomsday","둠스데이","force","망치");
            Add(gear,"ruyi","여의봉","tree","방망이");
            Add(gear,"uranus","우라누스","tree","채찍");
            Add(gear,"eternal_frost","만년한파","mithril","톤파");
            Add(gear,"loigor","로이거 차르","force","쌍검");
            Add(gear,"judgement","저지먼트","mithril","돌격 소총");
            Add(gear,"sharanga","샤릉가","mithril","석궁");
            Add(gear,"death_book","생사부","tree","암기");
            Add(gear,"fireball","파이어 볼","meteorite","투척");
            Add(gear,"empress","여제","tree","아르카나");
            Add(gear,"ultravision","울트라비전","force","카메라");
            Add(gear,"cosmic_bident","코스믹 바이던트","meteorite","창");
            Add(gear,"cerberus","케르베로스","meteorite","쌍절곤");
            Add(gear,"wonderful_tonight","원더풀 투나잇","tree","기타");
            Add(gear,"deathadder_mt","데스애더퀸-MT","mithril","VF의수");
            Add(gear,"angel_halo","천사의 고리","force",slot:GearSlot.Head);
            Add(gear,"radar","레이더","force",slot:GearSlot.Arm);
            Add(gear,"meteor_sword","유성검","meteorite","레이피어");
            Add(gear,"light_insignia","빛의 증표","meteorite",slot:GearSlot.Head);
            Add(gear,"ghillie_suit","길리 슈트","tree",slot:GearSlot.Clothes);
            Add(gear,"alexandros","알렉산드로스","tree",slot:GearSlot.Legs);
        }
        static TraitRule R(string trigger,string op,int amount=0,string key=null,string label=null,int every=1,int cooldown=0,int turn=0,int duration=1)
            => new TraitRule{trigger=trigger,op=op,amount=amount,key=key,label=label,every=every,cooldown=cooldown,maxPerTurn=turn,duration=duration};
        static TraitRule Need(TraitRule rule,string key,int value){rule.conditionKey=key;rule.conditionAmount=value;return rule;}
        static TraitRule Category(TraitRule rule,string category){rule.conditionCategory=category;return rule;}
        static TraitRule Hit(TraitRule rule){rule.onHit=true;return rule;}
        static TraitRule Resource(string trigger,string op,string key,string label,int amount,int cap=3)
            => new TraitRule{trigger=trigger,op=op,key=key,label=label,amount=amount,cap=cap};
        static TraitRule Slow(string trigger="hit",int cooldown=1)=>Hit(R(trigger,"status",key:"slow",cooldown:cooldown));
        static TraitRule HealReduction()=>Hit(R("hit","status",key:"heal_reduction",turn:1));
        static TraitRule[] Footwork()
        {
            return new[]{Resource("after_movement","gain","stride","가벼운 발걸음",1,3),
                Need(R("before_basic","bonus_damage",2,key:"stride",label:"가벼운 발걸음"),"stride",1),
                Need(Slow("after_basic"),"stride",3),Resource("after_basic","consume","stride","가벼운 발걸음",0)};
        }
        static TraitRule[] Shock()
        {
            return new[]{R("before_basic","bonus_damage",4,label:"전자기 충격",every:3),R("after_basic","block",2,every:3)};
        }
        static TraitRule[] Charged(int amount=4)
            =>new[]{R("before_basic","bonus_damage",amount,label:"충전 - 섬광",cooldown:2),Slow("after_basic",2)};
        static TraitRule[] Drain(int heal=2)=>new[]{Hit(R("after_basic","heal",heal,cooldown:1))};
        static void Set(List<GearDef> gear,string id,int attack=0,int block=0,int health=0,int evasion=0,string summary=null,params TraitRule[] rules)
        {
            var item=gear.Find(x=>x.id==id);item.attack=attack;item.block=block;item.health=health;item.evasion=evasion;
            item.mechanics=new TraitMechanicProfile{summary=summary,rules=rules};
        }
        public static void Configure(List<GearDef> gear)
        {
            foreach(var item in gear){item.effect=null;item.amount=0;item.critChance=0;}
            Set(gear,"laevateinn",attack:3,summary:"기본 공격에 발화를 실어 두 턴 동안 타오르는 피해를 줍니다.",rules:new[]{Hit(R("after_basic","burn",2,"ignite","타오르는 고통",turn:2,duration:2))});
            Set(gear,"dainsleif",attack:5,summary:"충전된 기본 공격이 추가 피해를 주고 적을 둔화시킵니다.",rules:Charged(5));
            Set(gear,"altair",attack:2,summary:"이동 기술로 가벼운 발걸음을 쌓아 다음 기본 공격을 강화합니다.",rules:Footwork());
            Set(gear,"akelte",attack:4,summary:"연속 사격으로 세 번째 기본 공격을 강화합니다.",rules:new[]{R("before_basic","bonus_damage",3,every:3)});
            Set(gear,"blood_hands",attack:5,summary:"혈액의 힘으로 방어를 관통하는 강화 공격을 준비합니다.",rules:new[]{R("before_basic","bonus_damage",3,cooldown:1),Hit(R("after_basic","status",key:"armor_break",cooldown:2))});
            Set(gear,"juggernaut",attack:3,health:6,summary:"기본 공격으로 열정을 높이고 열정 3에서 공격과 회피가 강화됩니다.",rules:new[]{Resource("after_basic","gain","passion","열정",1),Need(R("before_attack","bonus_damage",3,turn:2),"passion",3),Need(R("after_basic","evasion_buff",5,duration:2),"passion",3)});
            Set(gear,"mistilteinn",attack:2,summary:"공격이 적의 치유 효과를 감소시킵니다.",rules:new[]{R("before_skill","bonus_damage",1),HealReduction()});
            Set(gear,"eagle_eye",attack:3,summary:"활의 생명력 흡수로 기본 공격 적중 후 체력을 회복합니다.",rules:Drain());
            Set(gear,"andromeda",attack:3,summary:"저격총의 생명력 흡수로 기본 공격 적중 후 체력을 회복합니다.",rules:Drain(1));

            Set(gear,"mithril_armor",block:2,health:10,summary:"스킬 적중으로 예열을 높이고 예열 3에서 방어와 회피가 강화됩니다.",rules:new[]{Resource("skill_hit","gain","preheat","예열 - 인내",1),Need(R("turn_start","block",3),"preheat",3),Need(R("skill_hit","evasion_buff",4,duration:2),"preheat",3)});
            Set(gear,"cabana",block:1,summary:"스킬 공격이 강화되고 적의 치유 효과를 감소시킵니다.",rules:new[]{R("before_skill","bonus_damage",2,turn:2),HealReduction()});
            Set(gear,"titan_armor",block:1,health:16,summary:"세 번째 기본 공격에 전자기 충격이 실립니다.",rules:Shock());
            Set(gear,"ao_dai",block:2,health:4,summary:"유예로 받는 체력 피해의 20%를 다음 세 턴으로 나누어 미루고 기본 공격으로 체력을 회복합니다.",rules:Drain(1));gear.Find(x=>x.id=="ao_dai").damageDeferral=20;
            Set(gear,"queen_heart",block:2,health:14,summary:"두 번의 스킬 적중마다 응집으로 둔화를 부여하고 치유 효과를 감소시킵니다.",rules:new[]{Hit(R("skill_hit","status",key:"slow",every:2,cooldown:1)),HealReduction(),R("turn_start","heal",1)});
            Set(gear,"mithril_helm",block:1,health:8,summary:"이동 기술로 가벼운 발걸음을 쌓아 기본 공격을 강화합니다.",rules:Footwork());
            Set(gear,"insight",attack:1,health:6,summary:"D 스킬 뒤 달인의 충격파가 터져 추가 피해와 둔화를 줍니다.",rules:new[]{Category(R("after_card","delayed_damage",4,"master","달인 충격파",cooldown:1),"weapon"),Category(Slow("after_card"),"weapon")});
            Set(gear,"tactical_visor",health:14,summary:"세 번째 기본 공격에 전자기 충격이 실립니다.",rules:Shock());
            Set(gear,"wilderness_star",attack:2,summary:"기본 공격의 유도 바늘이 추가 피해를 주고 적의 치유 효과를 감소시킵니다.",rules:new[]{R("before_basic","bonus_damage",2,turn:2),HealReduction()});
            Set(gear,"opera_mask",attack:3,evasion:6,summary:"가면의 관통력으로 첫 공격을 강화하고 치유 효과를 감소시킵니다.",rules:new[]{R("before_attack","bonus_damage",2,turn:1),HealReduction()});
            Set(gear,"mithril_shield",block:2,health:6,summary:"세 번 적중하면 신속으로 두 턴 동안 회피율이 증가합니다.",rules:new[]{Hit(R("hit","evasion_buff",8,every:3,cooldown:1,duration:2))});
            Set(gear,"cube_watch",attack:1,summary:"기본 공격 적중마다 타임 엣지가 마지막 Q·W·E 카드의 다음 코스트를 1 줄입니다.",rules:new[]{Hit(new TraitRule{trigger="after_basic",op="discount_last",amount=1,maxPerTurn=1})});
            Set(gear,"skadi",summary:"스킬 적중 시 한파로 둔화를 부여합니다.",rules:new[]{R("before_skill","bonus_damage",1,turn:2),Slow("skill_hit",1)});
            Set(gear,"auto_arms",attack:2,block:2,health:8,summary:"기계 팔의 방해 효과 저항으로 제어 효과의 코스트 감소를 1 완화합니다.");gear.Find(x=>x.id=="auto_arms").controlResistance=1;
            Set(gear,"prominence",block:1,health:12,summary:"불꽃 결계가 매 턴 피해를 주고 적의 치유 효과를 감소시킵니다.",rules:new[]{R("battle_start","burn",2,"flame_barrier","불꽃 결계",duration:3),HealReduction()});
            Set(gear,"mithril_boots",block:1,evasion:5,summary:"기본 공격의 후속타가 추가 피해를 줍니다.",rules:new[]{R("before_basic","bonus_damage",2,turn:2)});
            Set(gear,"glacial_shoes",evasion:5,summary:"빠른 기술 순환으로 첫 스킬의 피해가 증가하고 코스트를 조금 회복합니다.",rules:new[]{R("before_skill","bonus_damage",1),R("after_skill","energy",1,turn:1,cooldown:1)});
            Set(gear,"galaxy_step",attack:1,evasion:7,summary:"생명력 흡수로 기본 공격 적중 후 체력을 회복합니다.",rules:Drain(1));
            Set(gear,"hermes",health:10,evasion:9,summary:"헤르메스의 방해 효과 저항으로 제어 효과의 코스트 감소를 1 완화합니다.");gear.Find(x=>x.id=="hermes").controlResistance=1;
            Set(gear,"pink_shoes",attack:2,evasion:10,summary:"재생의 일격이 주기적으로 기본 공격을 강화하며 적중하면 회복하고 치유 효과를 감소시킵니다.",rules:new[]{R("before_basic","bonus_damage",4,cooldown:2),Hit(R("after_basic","heal",3,cooldown:2)),HealReduction()});

            Set(gear,"fragarach",attack:3,evasion:3,summary:"이동 기술로 가벼운 발걸음을 쌓아 기본 공격을 강화합니다.",rules:Footwork());
            Set(gear,"doomsday",attack:2,health:6,summary:"스킬 적중 뒤 파열 표식이 폭발합니다.",rules:new[]{Hit(R("skill_hit","delayed_damage",5,"rupture","파열",cooldown:1))});
            Set(gear,"ruyi",attack:3,summary:"크리티컬 블로우가 기본 공격을 강화하고 적중하면 체력을 회복합니다.",rules:new[]{R("before_basic","bonus_damage",4,cooldown:2),Hit(R("after_basic","heal",3,cooldown:2))});
            Set(gear,"uranus",attack:2,summary:"충전된 기본 공격이 섬광 피해를 주고 적을 둔화시킵니다.",rules:Charged());
            Set(gear,"eternal_frost",attack:3,block:1,summary:"스킬 적중 시 한파로 적을 둔화시킵니다.",rules:new[]{Slow("skill_hit",1)});
            Set(gear,"loigor",attack:2,summary:"이동 기술로 가벼운 발걸음을 쌓아 기본 공격을 강화합니다.",rules:Footwork());
            Set(gear,"judgement",attack:4,summary:"연장 총열이 주기적으로 기본 공격을 강화합니다.",rules:new[]{R("before_basic","bonus_damage",4,cooldown:2)});
            Set(gear,"sharanga",attack:4,summary:"연장 총열이 주기적으로 기본 공격을 강화합니다.",rules:new[]{R("before_basic","bonus_damage",4,cooldown:2)});
            Set(gear,"death_book",attack:1,summary:"스킬 적중 뒤 저주가 다음 상대 턴 시작에 폭발합니다.",rules:new[]{Hit(R("skill_hit","delayed_damage",5,"curse","저주",cooldown:2))});
            Set(gear,"fireball",attack:2,summary:"충전된 기본 공격이 섬광 피해를 주고 적을 둔화시킵니다.",rules:Charged());
            Set(gear,"empress",attack:1,summary:"스킬 적중 시 한파로 적을 둔화시킵니다.",rules:new[]{R("before_skill","bonus_damage",2,turn:2),Slow("skill_hit",1)});
            Set(gear,"ultravision",attack:2,summary:"스킬 사용으로 의념을 충전해 다음 기본 공격을 강화합니다.",rules:new[]{R("after_skill","empower_basic",4,"ideation","의념",turn:1)});
            Set(gear,"cosmic_bident",attack:2,block:1,summary:"창의 기술 피해가 증가하고 치유 효과를 감소시킵니다.",rules:new[]{R("before_skill","bonus_damage",1,turn:2),HealReduction()});
            Set(gear,"cerberus",attack:3,summary:"쌍절곤의 연타가 강화되고 적의 치유 효과를 감소시킵니다.",rules:new[]{R("before_basic","bonus_damage",2,every:3),HealReduction()});
            Set(gear,"wonderful_tonight",attack:3,summary:"기본 공격으로 열정을 높이고 열정 3에서 추가 피해를 얻습니다.",rules:new[]{Resource("after_basic","gain","passion","열정",1),Need(R("before_attack","bonus_damage",2,turn:2),"passion",3)});
            Set(gear,"deathadder_mt",attack:3,summary:"스킬 사용으로 의념을 충전하며 R 적중 시 독사의 맹독으로 방어력을 감소시킵니다.",rules:new[]{R("after_skill","empower_basic",3,"ideation","의념",turn:1),Hit(R("after_ultimate","status",key:"armor_break",cooldown:1))});
            Set(gear,"angel_halo",attack:2,block:1,summary:"스킬 사용으로 의념을 충전해 다음 기본 공격을 강화합니다.",rules:new[]{R("after_skill","empower_basic",4,"ideation","의념",turn:1)});
            Set(gear,"radar",attack:1,summary:"세 번째 기본 공격마다 포톤 런처가 추가 피해를 주고 체력을 회복합니다.",rules:new[]{R("before_basic","bonus_damage",4,every:3),Hit(R("after_basic","heal",2,every:3))});
            Set(gear,"meteor_sword",attack:2,summary:"충전된 기본 공격에 섬광 피해가 실리고 적을 둔화시킵니다.",rules:Charged(3));
            Set(gear,"light_insignia",attack:1,summary:"빛의 증표로 공격에 치유 감소를 부여합니다.",rules:new[]{HealReduction()});
            Set(gear,"ghillie_suit",block:1,health:4,summary:"기본 공격 적중으로 예열을 높이고 예열 3에서 다음 기본 공격을 강화합니다.",rules:new[]{Hit(Resource("after_basic","gain","preheat","예열",1)),Need(R("before_basic","bonus_damage",2,turn:2),"preheat",3)});
            Set(gear,"alexandros",attack:1,evasion:4,summary:"가벼운 신발로 공격력과 회피율을 높입니다.");
            foreach(string id in new[]{"cerberus","cube_watch","meteor_sword","ghillie_suit","alexandros"})gear.Find(x=>x.id==id).critChance=5;
            foreach(string id in new[]{"radar","light_insignia"})gear.Find(x=>x.id==id).critChance=6;
            foreach(var item in gear)
            {
                var sentences=new List<string>();
                if(item.attack>0)sentences.Add("공격력이 "+item.attack+" 증가합니다.");
                if(item.block>0)sentences.Add("매 턴 방어도를 "+item.block+" 얻습니다.");
                if(item.health>0)sentences.Add("최대 체력이 "+item.health+" 증가합니다.");
                if(item.evasion>0)sentences.Add("회피율이 "+item.evasion+"% 증가합니다.");
                if(item.mechanics!=null && !string.IsNullOrEmpty(item.mechanics.summary))sentences.Add(TraitMechanics.Describe(item.mechanics));
                if(item.damageDeferral>0)sentences.Add("방어도로 막은 뒤 받는 체력 피해의 "+item.damageDeferral+"%를 다음 세 번의 자신의 턴 시작으로 나누어 미룹니다. 미룬 피해는 방어도를 무시하며 전투가 끝나면 사라집니다.");
                if(item.slot==GearSlot.Weapon)sentences.Add("장착하면 "+GameDatabase.Card(item.cardId).name+" D 카드 1장이 덱에 자동으로 추가됩니다.");
                sentences.Add("같은 장비의 특수 효과는 중첩되지 않습니다.");
                item.description=string.Join("\n",sentences);
                item.optionTags=OptionTags(item);
            }
        }

        // The same explicit, readable tags drive campfire filtering and gear details.
        public static string[] OptionTags(GearDef item)
        {
            var tags=new List<string>();
            var rules=item.mechanics?.rules ?? new TraitRule[0];
            if(item.attack>0 || rules.Any(r=>new[]{"bonus_damage","delayed_damage","burn","damage"}.Contains(r.op)))tags.Add("공격");
            if(item.block>0 || item.controlResistance>0 || item.damageDeferral>0 || rules.Any(r=>r.op=="block" || r.op=="bonus_block"))tags.Add("방어");
            if(item.health>0)tags.Add("체력");
            if(rules.Any(r=>r.op=="heal" || r.op=="bonus_heal" || r.op=="revive"))tags.Add("회복");
            if(item.evasion>0 || rules.Any(r=>r.op=="evasion_buff"))tags.Add("회피");
            if(item.critChance>0 || rules.Any(r=>r.trigger=="before_basic" || r.trigger=="after_basic" || r.op=="empower_basic"))tags.Add("일반 공격");
            if(item.critChance>0)tags.Add("치명타");
            if(rules.Any(r=>r.trigger=="before_skill" || r.trigger=="after_skill" || r.trigger=="skill_hit" || r.trigger=="after_ultimate" || r.conditionCategory=="weapon" || r.conditionCategory=="skill"))tags.Add("스킬");
            if(rules.Any(r=>r.op=="status" || r.op=="burn"))tags.Add("상태이상");
            if(rules.Any(r=>r.op=="discount" || r.op=="discount_last" || r.op=="energy" || r.op=="energy_buff"))tags.Add("코스트");
            return tags.ToArray();
        }
    }
}
