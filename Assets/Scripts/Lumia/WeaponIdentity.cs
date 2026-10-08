using System;
using System.Collections.Generic;
using System.Linq;

namespace Lumia
{
    /// <summary>Original weapon classes, one D card per class, and deterministic equipment grants.</summary>
    public static class WeaponIdentity
    {
        static readonly Dictionary<string,string> Skills=new Dictionary<string,string> {
            {"글러브","weapon_glove"},{"톤파","weapon_tonfa"},{"방망이","weapon_bat"},{"채찍","weapon_whip"},
            {"투척","weapon_throw"},{"암기","weapon_shuriken"},{"활","weapon_bow"},{"석궁","weapon_crossbow"},
            {"권총","weapon_pistol"},{"돌격 소총","weapon_rifle"},{"저격총","weapon_sniper"},{"망치","weapon_hammer"},
            {"도끼","weapon_axe"},{"단검","weapon_dagger"},{"양손검","weapon_sword"},{"쌍검","weapon_dual"},
            {"창","weapon_spear"},{"쌍절곤","weapon_nunchaku"},{"레이피어","weapon_rapier"},{"기타","weapon_guitar"},
            {"카메라","weapon_camera"},{"아르카나","weapon_arcana"},{"VF의수","weapon_vf"}
        };
        static readonly Dictionary<string,string[]> Classes=new Dictionary<string,string[]> {
            { "nia", new[]{"권총"} },
            { "jackie", new[]{"단검","양손검","도끼","쌍검"} },
            { "aya", new[]{"권총","돌격 소총","저격총"} },
            { "hyunwoo", new[]{"글러브","톤파"} },
            { "yuki", new[]{"양손검","쌍검"} },
            { "hyejin", new[]{"활","암기"} },
            { "sua", new[]{"망치","방망이"} },
            { "isol", new[]{"돌격 소총","권총"} },
            { "nadine", new[]{"활","석궁"} },
            { "emma", new[]{"암기","아르카나"} },
            { "charlotte", new[]{"아르카나"} },
            { "nathapon", new[]{"카메라"} },
            { "nicky", new[]{"글러브"} },
            { "daniel", new[]{"단검"} },
            { "tia", new[]{"방망이"} },
            { "laura", new[]{"채찍"} },
            { "lenox", new[]{"채찍"} },
            { "leon", new[]{"글러브","톤파"} },
            { "rozzi", new[]{"권총"} },
            { "luke", new[]{"방망이"} },
            { "dailin", new[]{"글러브","쌍절곤"} },
            { "rio", new[]{"활"} },
            { "martina", new[]{"카메라"} },
            { "mai", new[]{"채찍"} },
            { "markus", new[]{"도끼","망치"} },
            { "magnus", new[]{"망치","방망이"} },
            { "vanya", new[]{"아르카나"} },
            { "barbara", new[]{"권총"} },
            { "bernice", new[]{"저격총"} },
            { "bianca", new[]{"아르카나"} },
            { "celine", new[]{"투척"} },
            { "sho", new[]{"단검","창"} },
            { "shoichi", new[]{"단검"} },
            { "sissela", new[]{"투척","암기"} },
            { "silvia", new[]{"권총"} },
            { "adela", new[]{"레이피어","방망이"} },
            { "adriana", new[]{"투척"} },
            { "adina", new[]{"아르카나"} },
            { "isaac", new[]{"톤파"} },
            { "alex", new[]{"톤파","양손검","암기","권총"} },
            { "jan", new[]{"글러브","톤파"} },
            { "estelle", new[]{"도끼"} },
            { "aiden", new[]{"양손검"} },
            { "echion", new[]{"VF의수"} },
            { "elena", new[]{"레이피어"} },
            { "johann", new[]{"아르카나"} },
            { "william", new[]{"투척"} },
            { "irem", new[]{"투척"} },
            { "eva", new[]{"투척"} },
            { "ian", new[]{"단검"} },
            { "eleven", new[]{"망치"} },
            { "zahir", new[]{"투척","암기"} },
            { "jenny", new[]{"권총"} },
            { "camilo", new[]{"쌍검","레이피어"} },
            { "karla", new[]{"석궁"} },
            { "cathy", new[]{"단검","쌍검"} },
            { "chloe", new[]{"암기"} },
            { "chiara", new[]{"레이피어"} },
            { "tazia", new[]{"암기"} },
            { "theodore", new[]{"저격총"} },
            { "felix", new[]{"창"} },
            { "priya", new[]{"기타"} },
            { "fiora", new[]{"레이피어","양손검","창"} },
            { "piolo", new[]{"쌍절곤"} },
            { "hart", new[]{"기타"} },
            { "haze", new[]{"돌격 소총"} },
            { "debi_marlene", new[]{"양손검"} },
            { "arda", new[]{"아르카나"} },
            { "abigail", new[]{"도끼"} },
            { "alonso", new[]{"글러브"} },
            { "leni", new[]{"권총"} },
            { "tsubame", new[]{"암기"} },
            { "kenneth", new[]{"도끼"} },
            { "katja", new[]{"저격총"} },
            { "darko", new[]{"방망이"} },
            { "lenore", new[]{"기타"} },
            { "garnet", new[]{"방망이"} },
            { "yumin", new[]{"아르카나"} },
            { "hisui", new[]{"양손검"} },
            { "justyna", new[]{"석궁"} },
            { "istvan", new[]{"창"} },
            { "xuelin", new[]{"레이피어"} },
            { "henry", new[]{"암기"} },
            { "blair", new[]{"쌍검"} },
            { "mirka", new[]{"망치"} },
            { "fenrir", new[]{"글러브"} },
            { "coraline", new[]{"아르카나"} },
            { "bihyung", new[]{"방망이"} },
            { "craver", new[]{"권총"} },
            { "lucia", new[]{"저격총"} },
            { "ceres", new[]{"양손검"} },
        };
        public static string CardFor(string weaponClass)
        {
            string result;return weaponClass!=null && Skills.TryGetValue(weaponClass,out result)?result:null;
        }
        public static string AlternateCard(string weaponClass,int weaponIndex)
        {
            // Alex originally changes between four weapon classes. Use these as his extra loadout.
            string[] family={"톤파","양손검","암기","권총"};
            var alternatives=family.Where(x=>x!=weaponClass).ToArray();
            return CardFor(alternatives[Math.Abs(weaponIndex)%alternatives.Length]);
        }
        public static List<string> EquipmentCards(IEnumerable<string> equipment,bool alex)
        {
            var result=new List<string>();int index=0;
            foreach(var item in equipment.Select(GameDatabase.Equipment).Where(x=>x!=null && x.slot==GearSlot.Weapon))
            {
                string card=CardFor(item.weaponClass) ?? item.cardId;
                if(GameDatabase.Card(card)!=null)result.Add(card);
                if(alex)result.Add(AlternateCard(item.weaponClass,index));
                index++;
            }
            return result;
        }
        static void Add(List<CardDef> cards,string id,string name,string owner,float cooldown,int damage=0,int block=0,int draw=0,int energy=0,int evasion=0,int hits=1)
        {
            cards.Add(new CardDef{id=id,name=name,owner=owner,key="D",category="weapon",cooldown=cooldown,
                damage=damage,block=block,draw=draw,energy=energy,evasion=evasion,hits=hits});
        }
        public static void AddCards(List<CardDef> cards)
        {
            Add(cards,"weapon_tonfa","고속 회전","톤파",30,damage:12,block:18);
            Add(cards,"weapon_bat","풀스윙","방망이",25,damage:18);
            Add(cards,"weapon_whip","바람 가르기","채찍",25,damage:10,hits:2);
            Add(cards,"weapon_throw","연막","투척",25,damage:12);
            Add(cards,"weapon_shuriken","마름쇠 투척","암기",25);
            Add(cards,"weapon_crossbow","강노","석궁",25,damage:13);
            Add(cards,"weapon_rifle","과열","돌격 소총",15,draw:1,energy:1);
            Add(cards,"weapon_dual","쌍검 난무","쌍검",25,damage:12);
            Add(cards,"weapon_spear","그림자 찌르기","창",30,damage:18);
            Add(cards,"weapon_nunchaku","맹룡과강","쌍절곤",25,damage:21);
            Add(cards,"weapon_guitar","Love&...","기타",30,damage:16);
            Add(cards,"weapon_camera","플래시","카메라",25,damage:16);
            Add(cards,"weapon_arcana","VF 매개","아르카나",30);
            Add(cards,"weapon_vf","VF 안정화","VF의수",13,draw:1,energy:1,evasion:15);
        }
        static SkillRule Effect(string op,int amount,string key,string label,int duration=2)
            => new SkillRule{op=op,amount=amount,key=key,label=label,duration=duration,delay=1,cap=3};
        static void Profile(List<CardDef> cards,string id,string summary,params SkillRule[] rules)
        {
            cards.Find(x=>x.id==id).mechanics=new SkillMechanicProfile{summary=summary,rules=rules};
        }
        static void Status(List<CardDef> cards,string id,string kind)
        {
            cards.Find(x=>x.id==id).statuses=new[]{new CardStatusRule{op="status",key=kind,duration=1,onHit=true}};
        }
        public static void Configure(List<CardDef> cards,List<CharacterDef> characters)
        {
            foreach(var character in characters){string[] mapped;if(Classes.TryGetValue(character.id,out mapped))character.weaponClasses=mapped.ToArray();}
            Profile(cards,"weapon_tonfa","회전 방어를 유지하며 맞받아치는 피해를 준비합니다.",Effect("counter",4,"spin","고속 회전",1));
            Status(cards,"weapon_bat","knockback");
            Status(cards,"weapon_whip","slow");
            Profile(cards,"weapon_throw","연막이 한 번 더 터져 피해를 줍니다.",Effect("delayed_damage",8,"smoke","두 번째 연막",1));
            Status(cards,"weapon_throw","slow");
            Profile(cards,"weapon_shuriken","마름쇠를 깔아 두 턴 동안 피해를 줍니다.",Effect("summon",8,"caltrops","마름쇠",2));
            Status(cards,"weapon_shuriken","slow");
            Profile(cards,"weapon_crossbow","표식이 다음 상대 턴 시작에 폭발합니다.",Effect("delayed_damage",10,"strongbow","강노 표식",1));
            Status(cards,"weapon_crossbow","slow");
            Profile(cards,"weapon_rifle","총열을 과열시켜 다음 기본 공격을 강화합니다.",Effect("empower_basic",8,"overheat","과열",2));
            var dual=cards.Find(x=>x.id=="weapon_dual");dual.freeCastTargets=new[]{dual.id};dual.freeCastCount=1;dual.freeCastOnHit=true;dual.movement=true;
            Status(cards,"weapon_spear","knockback");
            Status(cards,"weapon_nunchaku","stun");
            Status(cards,"weapon_guitar","slow");
            Status(cards,"weapon_camera","blind");
            Profile(cards,"weapon_arcana","VF 구체가 두 턴 동안 적에게 날아갑니다.",Effect("summon",8,"vf_orb","VF 구체",2));
            var vf=cards.Find(x=>x.id=="weapon_vf");vf.movement=false;
            Profile(cards,vf.id,"VF를 안정시키고 다음 에키온 기술의 코스트를 줄입니다.",
                new SkillRule{op="discount",amount=1,targetCard="echion_q"},new SkillRule{op="discount",amount=1,targetCard="echion_w"},
                new SkillRule{op="discount",amount=1,targetCard="echion_e"},new SkillRule{op="discount",amount=1,targetCard="echion_r"});
        }
    }
}
