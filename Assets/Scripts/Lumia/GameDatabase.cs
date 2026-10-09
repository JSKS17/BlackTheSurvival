using System;
using System.Collections.Generic;

namespace Lumia
{
    // Source names and cooldown snapshots are recorded in docs/SOURCES.md.
    // Damage, healing, turn effects and credit values are the fan game's balance.
    public static class GameDatabase
    {
        public static readonly List<CardDef> Cards = new List<CardDef>();
        public static readonly List<PassiveDef> Passives = new List<PassiveDef>();
        public static readonly List<RuneDef> Runes = new List<RuneDef>();
        public static readonly List<GearDef> Gear = new List<GearDef>();
        public static readonly List<FoodDef> Foods = new List<FoodDef>();
        public static readonly List<ObjectDef> Objects = new List<ObjectDef>();
        public static readonly List<EventDef> Events = new List<EventDef>();
        public static readonly List<CharacterDef> Characters = new List<CharacterDef>();

        static GameDatabase()
        {
            BuildCards(); BuildPassives(); BuildRunes(); BuildObjects(); BuildFoods(); BuildGear(); BuildEvents();
            ClassicRoster.Populate(Cards, Passives, Characters);
            RecentRoster.Populate(Cards, Passives, Characters);
            ConfigureCoreCombos();
            CardPresentation.Configure(Cards);
            SkillMechanics.Configure(Cards);
            WeaponIdentity.Configure(Cards, Characters);
            ConfigureConditionalCombos();
            foreach (var card in Cards) card.description = CardPresentation.Describe(card, Cards);
            TraitMechanics.Configure(Passives, Runes);
            EquipmentIdentity.Configure(Gear);
            EventPresentation.Configure(Events);
        }

        public static CardDef Card(string id) => Cards.Find(x => x.id == id);
        public static PassiveDef Passive(string id) => Passives.Find(x => x.id == id);
        public static RuneDef Rune(string id) => Runes.Find(x => x.id == id);
        public static GearDef Equipment(string id) => Gear.Find(x => x.id == id);
        public static FoodDef Food(string id) => Foods.Find(x => x.id == id);
        public static ObjectDef Object(string id) => Objects.Find(x => x.id == id);
        public static EventDef Event(string id) => Events.Find(x => x.id == id);
        public static CharacterDef Character(string id) => Characters.Find(x => x.id == id);
        public static int CostForCooldown(float seconds) => CardPresentation.Cost(seconds);

        static void ConfigureCoreCombos()
        {
            Card("nia_r").freeCastTargets = new[] { "nia_q" };
            Card("nia_r").freeCastCount = 1;
            Combo("yuki_w", false, "yuki_e");
            foreach (string id in new[] { "yuki_e", "hyejin_q", "emma_q", "emma_w", "sua_e" }) Combo(id, true, id);
            Combo("sua_r", false, "sua_q", "sua_w", "sua_e");
            Card("sua_r").freeCastLastSkill = true;
            Card("sua_r").draw = Card("sua_r").energy = 0;
        }

        static void Combo(string id, bool onHit, params string[] targets)
        {
            var c = Card(id); c.freeCastTargets = targets; c.freeCastCount = 1; c.freeCastOnHit = onHit;
        }

        static void ConfigureConditionalCombos()
        {
            // A hit may finish and immediately consume a threshold, so acquisition
            // reads the state before this card (never later cross-subject additions).
            ResetCondition("cathy_q", "wounded", 2, true,
                "이번 적중으로 상처가 최대치 3에 도달해 중상을 부여한 경우입니다. 상처를 소모한 뒤에도 획득한 무료 사용권은 유지됩니다.");
            ResetCondition("shoichi_w", "dagger", 1, true);
            ResetCondition("celine_w", "bomb", 1, true);
            Card("celine_w").freeCastCondition.conditionKey2 = "fusion";
            Card("celine_w").freeCastCondition.conditionAmount2 = 1;
            Card("celine_w").freeCastEitherResource = true;
            ResetCondition("jan_q", "unyielding", 3, true);
            ResetCondition("jan_e", "unyielding", 3, false);
            ResetCondition("karla_w", "harpoon", 1, true);
            ResetCondition("bianca_w", "blood", 2, false,
                "혈액의 최대치는 4입니다. 혈액을 절반 이상 소모한 안식에서만 연계가 열립니다.");

            // These skills retain their native conditional discounts or empowered
            // attacks rather than generating an immediate, unrelated free action.
            foreach (string id in new[] { "shoichi_e", "isaac_w", "haze_w", "barbara_w", "fiora_q", "fiora_e" })
            {
                Card(id).freeCastCount = 0;
                Card(id).freeCastTargets = new string[0];
            }
        }

        static void ResetCondition(string id, string resource, int amount, bool onHit, string note = null)
        {
            var card = Card(id);
            card.freeCastOnHit = onHit;
            card.freeCastCondition = new SkillRule { conditionKey=resource, conditionAmount=amount, onHit=onHit };
            card.freeCastConditionNote = note;
        }

        static void BuildCards()
        {
            Cards.Add(new CardDef { id="basic_attack", name="기본 공격", owner="하나", key="ATK", category="basic", cost=1, damage=6, description="6 피해. 장착 무기의 공격력 보너스를 받습니다." });
            Cards.Add(new CardDef { id="basic_guard", name="경계", owner="하나", key="DEF", category="basic", cost=1, block=5, description="5 방어도. 방어도는 다음 내 턴 시작에 사라집니다." });

            Skill("nia_q", "아케이드 드롭", "니아", "Q", 6, damage:9, draw:1);
            Skill("nia_w", "싱크 포인트", "니아", "W", 12, damage:13, vulnerable:2);
            Skill("nia_e", "1UP", "니아", "E", 32, heal:20, block:15, evasion:40, duration:2, exhaust:true);
            Skill("nia_r", "니아의 게임월드", "니아", "R", 80, damage:24, block:14, draw:2, exhaust:true);

            Skill("jackie_q", "연참", "재키", "Q", 11, damage:7, hits:2, heal:3, poison:2);
            Skill("jackie_w", "힘줄 절단", "재키", "W", 6, damage:8, weak:2);
            Skill("jackie_e", "습격", "재키", "E", 16, damage:12, evasion:20, duration:2);
            Skill("jackie_r", "전기톱 살인마", "재키", "R", 60, damage:22, strength:3, poison:4, exhaust:true);

            Skill("aya_q", "2연발", "아야", "Q", 10, damage:5, hits:2);
            Skill("aya_w", "고정 사격", "아야", "W", 13, damage:4, hits:4);
            Skill("aya_e", "무빙턴", "아야", "E", 19, block:8, evasion:35, duration:2, draw:1);
            Skill("aya_r", "공포탄", "아야", "R", 80, damage:25, weak:3, exhaust:true);

            Skill("hyunwoo_q", "발 밟기", "현우", "Q", 8, damage:8, weak:1);
            Skill("hyunwoo_w", "허세", "현우", "W", 20, block:15, heal:4);
            Skill("hyunwoo_e", "선빵필승", "현우", "E", 14, damage:13, weak:1, vulnerable:1);
            Skill("hyunwoo_r", "핵펀치", "현우", "R", 60, damage:32, vulnerable:3, exhaust:true);

            Skill("yuki_q", "머리치기!", "유키", "Q", 5, damage:8, weak:1);
            Skill("yuki_w", "옷매무새 정리", "유키", "W", 15, block:12, draw:2);
            Skill("yuki_e", "빗겨치고 일격", "유키", "E", 14, damage:12, block:6, evasion:15, duration:1);
            Skill("yuki_r", "화무십일홍", "유키", "R", 90, damage:33, vulnerable:2, exhaust:true);

            Skill("hyejin_q", "제압부", "혜진", "Q", 11, damage:13, weak:2);
            Skill("hyejin_w", "흡령부", "혜진", "W", 14, damage:11, weak:2, vulnerable:1);
            Skill("hyejin_e", "이동부", "혜진", "E", 13, damage:10, evasion:30, duration:2);
            Skill("hyejin_r", "오대존명왕진", "혜진", "R", 100, damage:7, hits:5, weak:2, exhaust:true);

            Skill("sua_q", "오딧세이", "수아", "Q", 12, damage:12, vulnerable:2);
            Skill("sua_w", "파랑새", "수아", "W", 19, block:13, weak:2);
            Skill("sua_e", "돈키호테", "수아", "E", 16, damage:12, heal:5, evasion:15, duration:1);
            Skill("sua_r", "기억력", "수아", "R", 30, draw:3, energy:1, block:10, exhaust:true);

            Skill("isol_q", "셈텍스 폭탄", "아이솔", "Q", 15, damage:10, poison:4);
            Skill("isol_w", "화망", "아이솔", "W", 13, damage:3, hits:5, weak:1);
            Skill("isol_e", "은밀 기동", "아이솔", "E", 18, evasion:40, duration:2, draw:2);
            Skill("isol_r", "Mok제 폭탄", "아이솔", "R", 30, damage:22, poison:6, exhaust:true);

            Skill("nadine_q", "황소의 눈", "나딘", "Q", 8, damage:10);
            Skill("nadine_w", "다람쥐 덫", "나딘", "W", 18, damage:10, weak:2, vulnerable:1);
            Skill("nadine_e", "원숭이 와이어", "나딘", "E", 24, draw:2, evasion:30, duration:2);
            Skill("nadine_r", "늑대 맹습", "나딘", "R", 80, damage:8, hits:3, strength:2, exhaust:true);

            Skill("emma_q", "비둘기 딜러", "엠마", "Q", 5, damage:7, draw:1);
            Skill("emma_w", "폭죽 모자", "엠마", "W", 11, damage:15, vulnerable:1);
            Skill("emma_e", "마술 토끼", "엠마", "E", 18, weak:3, heal:6, block:6);
            Skill("emma_r", "Change★", "엠마", "R", 9, damage:7, evasion:20, duration:1, draw:1);

            Skill("weapon_glove", "어퍼컷", "글러브", "D", 15, damage:14, vulnerable:1, category:"weapon");
            Skill("weapon_pistol", "무빙 리로드", "권총", "D", 40, draw:3, energy:1, evasion:35, duration:2, exhaust:true, category:"weapon");
            Skill("weapon_dagger", "망토와 단검", "단검", "D", 40, damage:23, evasion:40, duration:2, exhaust:true, category:"weapon");
            Skill("weapon_axe", "피의 나선", "도끼", "D", 8, damage:9, heal:4, category:"weapon");
            Skill("weapon_sword", "빗겨 흘리기", "양손검", "D", 30, damage:15, block:24, category:"weapon");
            Skill("weapon_bow", "곡사", "활", "D", 35, damage:28, weak:2, category:"weapon");
            Skill("weapon_hammer", "갑옷 깨기", "망치", "D", 30, damage:23, vulnerable:3, category:"weapon");
            Skill("weapon_rapier", "섬격", "레이피어", "D", 30, damage:24, evasion:25, duration:1, category:"weapon");
            Skill("weapon_sniper", "저격", "저격총", "D", 40, damage:32, category:"weapon");
            WeaponIdentity.AddCards(Cards);
            Skill("tactical_blink", "블링크", "전술", "F", 45, block:12, evasion:60, duration:2, exhaust:true, category:"tactical");
            Skill("tactical_wind", "치유의 바람", "전술", "F", 50, heal:25, block:8, exhaust:true, category:"tactical");
            Skill("tactical_plasma", "플라즈마 대시", "전술", "F", 50, damage:25, evasion:40, duration:2, exhaust:true, category:"tactical");
        }

        static void Skill(string id, string name, string owner, string key, float cooldown, int damage=0, int block=0, int heal=0, int draw=0, int energy=0, int poison=0, int vulnerable=0, int weak=0, int strength=0, int evasion=0, int duration=1, int hits=1, bool exhaust=false, string category="skill")
        {
            var c = new CardDef { id=id, name=name, owner=owner, key=key, cooldown=cooldown, cost=CostForCooldown(cooldown), damage=damage, block=block, heal=heal, draw=draw, energy=energy, poison=poison, vulnerable=vulnerable, weak=weak, strength=strength, evasion=evasion, duration=duration, hits=hits, exhaust=exhaust, category=category };
            var text = new List<string>();
            if (damage>0) text.Add(hits>1 ? $"{damage} 피해 × {hits}회" : $"{damage} 피해");
            if (block>0) text.Add($"방어도 {block}");
            if (heal>0) text.Add($"체력 {heal} 회복");
            if (draw>0) text.Add($"{draw}장 뽑기");
            if (energy>0) text.Add($"에너지 +{energy}");
            if (poison>0) text.Add($"지속 피해 {poison} (출혈·화상·트랩 통합)");
            if (vulnerable>0) text.Add($"취약 {vulnerable}턴");
            if (weak>0) text.Add($"약화 {weak}턴");
            if (strength>0) text.Add($"전투 중 힘 +{strength}");
            if (evasion>0) text.Add($"회피 +{evasion}% / {duration}턴");
            if (exhaust) text.Add("소멸");
            c.description=string.Join(" · ",text);
            Cards.Add(c);
        }

        static void BuildPassives()
        {
            P("nia_p","K.O.","니아","스킬 적중 때마다 축적하는 처형력을 추가 스킬 피해 +2로 전환합니다.","skill_bonus",2);
            P("jackie_p","피의 축제","재키","피의 축제에서 얻는 전투 지속력을 승리 후 체력 9 회복으로 전환합니다.","kill_heal",9);
            P("aya_p","아야의 정의","아야","정의를 지키는 보호막. 전투 시작에 방어도 12.","battle_start_block",12);
            P("hyunwoo_p","도그파이트","현우","싸울수록 버티는 회복력. 매 턴 체력 2 회복.","turn_heal",2);
            P("yuki_p","완벽한 옷매무새","유키","단추의 추가 피해를 기본 공격 피해 +3으로 전환합니다.","attack_bonus",3);
            P("hyejin_p","삼재","혜진","삼재가 만드는 빈틈. 매 턴 방어도 3.","turn_block",3);
            P("sua_p","마음의 양식","수아","이야기에서 얻은 힘. 매 턴 체력 3 회복.","turn_heal",3);
            P("isol_p","유격전","아이솔","방어를 무너뜨리는 유격전. 스킬 피해 +3.","skill_bonus",3);
            P("nadine_p","야성","나딘","사냥 경험이 남는다. 승리 보상 크레딧 +20.","reward_credit",20);
            P("emma_p","CheerUP♥","엠마","마술사의 보호막을 매 턴 방어도 4로 전환합니다.","turn_block",4);
            foreach (var pair in new [] { new[]{"nia","니아"},new[]{"jackie","재키"},new[]{"aya","아야"},new[]{"hyunwoo","현우"},new[]{"yuki","유키"},new[]{"hyejin","혜진"},new[]{"sua","수아"},new[]{"isol","아이솔"},new[]{"nadine","나딘"},new[]{"emma","엠마"} })
                Characters.Add(new CharacterDef { id=pair[0], name=pair[1], passiveId=pair[0]+"_p", cards=new[]{pair[0]+"_q",pair[0]+"_w",pair[0]+"_e",pair[0]+"_r"} });
        }

        static void P(string id,string name,string owner,string description,string trigger,int amount) => Passives.Add(new PassiveDef { id=id,name=name,owner=owner,description=description,trigger=trigger,amount=amount });

        static void BuildRunes()
        {
            R("vampire","흡혈마","파괴",true,"흡혈을 턴 회복으로 변환. 매 턴 체력 2 회복.","turn_heal",2);
            R("frailty","취약","파괴",true,"집중 타격을 스킬 피해 +3으로 변환.","skill_bonus",3);
            R("lightning","벽력","혼돈",true,"벽력의 추가 피해. 스킬 피해 +4.","skill_bonus",4);
            R("vortex","와류","혼돈",true,"와류의 재생을 승리 후 체력 10 회복으로 변환.","kill_heal",10);
            R("diamond","금강","저항",true,"금강의 내구력. 매 턴 방어도 5.","turn_block",5);
            R("guardian","빛의 수호","저항",true,"빛의 보호막. 전투 시작에 방어도 18.","battle_start_block",18);
            R("healing_drone","치유 드론","지원",true,"치유 드론의 지속 회복. 매 턴 체력 3 회복.","turn_heal",3);
            R("amplification_drone","증폭 드론","지원",true,"증폭 드론의 지원. 전투 시작에 힘 2.","battle_start_strength",2);
            R("dog_mask","들개 탈","파괴",false,"사냥으로 축적하는 흡혈을 승리 후 체력 4 회복으로 전환.","kill_heal",4);
            R("scar","상흔","파괴",false,"상흔의 연속 타격을 기본 공격 피해 +2로 전환.","attack_bonus",2);
            R("quickfire","속사","혼돈",false,"속사의 연계를 기본 공격 피해 +2로 전환.","attack_bonus",2);
            R("circular","서큘러 시스템","혼돈",false,"순환하는 VF 에너지. 매 턴 체력 1 회복.","turn_heal",1);
            R("vigilance","경계심","저항",false,"위험에 대비. 전투 시작에 방어도 8.","battle_start_block",8);
            R("tempering","담금질","저항",false,"단련된 방어력. 매 턴 방어도 2.","turn_block",2);
            R("coupon","할인 쿠폰","지원",false,"키오스크 할인 효과를 전투 보상 크레딧 +15로 전환.","reward_credit",15);
            R("hunting","사냥의 전율","지원",false,"사냥 뒤 숨 돌리기. 승리 후 체력 4 회복.","kill_heal",4);
        }

        static void R(string id,string name,string tree,bool main,string description,string trigger,int amount) => Runes.Add(new RuneDef { id=id,name=name,tree=tree,main=main,description=description,trigger=trigger,amount=amount });

        static void BuildObjects()
        {
            Objects.Add(new ObjectDef { id="meteorite",name="운석",price=200,description="별에서 떨어진 제작 재료. 늑대·곰 또는 키오스크 주변 실험체에서 획득." });
            Objects.Add(new ObjectDef { id="tree",name="생명의 나무",price=200,description="생명력이 깃든 제작 재료. 늑대·곰 또는 키오스크 주변 실험체에서 획득." });
            Objects.Add(new ObjectDef { id="mithril",name="미스릴",price=250,description="단단하고 가벼운 제작 재료. 곰 또는 키오스크 주변 실험체에서 획득." });
            Objects.Add(new ObjectDef { id="force",name="포스코어",price=350,description="고밀도 VF 제작 재료. 곰 또는 키오스크 주변 실험체에서 획득." });
            Objects.Add(new ObjectDef { id="blood",name="VF 혈액 샘플",price=500,description="초월 장비 제작 재료. 일반 전투에서 드롭되지 않음." });
        }

        static void BuildFoods()
        {
            F("potato","감자",25,7,"fries","모닥불에서 감자튀김으로 요리할 수 있는 재료.");
            F("meat","고기",30,9,"steak","모닥불에서 웰던 스테이크로 요리할 수 있는 재료.");
            F("salmon","연어",45,12,"salmon_steak","모닥불에서 연어 스테이크로 요리할 수 있는 재료.");
            F("sweet_potato","호박고구마",45,12,"honey_potato","모닥불에서 꿀고구마로 요리할 수 있는 재료.");
            F("truffle","트러플",50,12,"truffle_pasta","모닥불에서 트러플 파스타로 요리할 수 있는 재료.");
            F("fries","감자튀김",45,18,null,"체력 18 회복. 완성 요리라 모닥불 재강화 불가.");
            F("steak","웰던 스테이크",50,20,null,"체력 20 회복. 완성 요리라 모닥불 재강화 불가.");
            F("watermelon","수박",55,22,null,"체력 22 회복. 시원한 보급 음식.");
            F("chicken_food","후라이드 치킨",55,22,null,"체력 22 회복. 바삭한 보급 음식.");
            F("fish_chips","피쉬 앤 칩스",75,28,null,"체력 28 회복. 완성 요리라 모닥불 재강화 불가.");
            F("salmon_steak","연어 스테이크",90,35,null,"체력 35 회복. 연어 + 모닥불.");
            F("honey_potato","꿀고구마",90,35,null,"체력 35 회복. 호박고구마 + 모닥불.");
            F("truffle_pasta","트러플 파스타",95,38,null,"체력 38 회복. 트러플 + 모닥불.");
            Foods.Add(new FoodDef { id="soup",name="만년 스프",price=150,fullHeal=true,description="최대 체력까지 회복. 모닥불에서 식사를 선택하면 1개 획득. 팬 게임에서는 원본 수치 대신 완전 회복 적용." });
        }

        static void F(string id,string name,int price,int heal,string upgradeTo,string description) => Foods.Add(new FoodDef { id=id,name=name,price=price,heal=heal,upgradeTo=upgradeTo,description=description });

        static void BuildGear()
        {
            EquipmentIdentity.AddGear(Gear);
            G("laevateinn","레바테인",GearSlot.Weapon,"tree",attack:4,weaponClass:"양손검",cardId:"weapon_sword",effect:"attack_bonus",amount:1,description:"불꽃을 두른 양손검. 공격력 +4, 기본 공격 추가 피해 +1.");
            G("dainsleif","다인슬라이프 - 진홍",GearSlot.Weapon,"blood",attack:7,weaponClass:"양손검",cardId:"weapon_sword",effect:"attack_bonus",amount:2,rarity:"초월",description:"혈액의 힘을 품은 검. 공격력 +7, 기본 공격 추가 피해 +2.");
            G("altair","알타이르",GearSlot.Weapon,"mithril",attack:3,weaponClass:"권총",cardId:"weapon_pistol",effect:"skill_bonus",amount:2,description:"빛을 모으는 권총. 공격력 +3, 스킬 추가 피해 +2.");
            G("akelte","악켈테",GearSlot.Weapon,"mithril",attack:6,weaponClass:"권총",cardId:"weapon_pistol",description:"정밀한 권총. 공격력 +6.");
            G("blood_hands","블러디 핸즈",GearSlot.Weapon,"blood",attack:5,weaponClass:"글러브",cardId:"weapon_glove",effect:"turn_heal",amount:2,rarity:"초월",description:"혈액을 품은 글러브. 공격력 +5, 매 턴 체력 2 회복.");
            G("juggernaut","저거너트",GearSlot.Weapon,"tree",attack:4,health:6,weaponClass:"도끼",cardId:"weapon_axe",description:"열정의 전설 도끼. 공격력 +4, 최대 체력 +6.");
            G("mistilteinn","미스틸테인",GearSlot.Weapon,"tree",attack:3,weaponClass:"레이피어",cardId:"weapon_rapier",effect:"skill_bonus",amount:2,description:"생명력을 품은 레이피어. 공격력 +3, 스킬 추가 피해 +2.");
            G("eagle_eye","이글 아이",GearSlot.Weapon,"mithril",attack:5,weaponClass:"활",cardId:"weapon_bow",description:"미스릴 활. 공격력 +5.");
            G("andromeda","안드로메다",GearSlot.Weapon,"meteorite",attack:4,weaponClass:"저격총",cardId:"weapon_sniper",description:"별의 저격총. 공격력 +4.");

            G("mithril_armor","미스릴 갑옷",GearSlot.Clothes,"mithril",block:3,health:8,description:"가벼운 갑옷. 매 턴 방어도 +3, 최대 체력 +8.");
            G("cabana","카바나",GearSlot.Clothes,"meteorite",block:2,effect:"skill_bonus",amount:2,description:"별의 장식이 달린 옷. 매 턴 방어도 +2, 스킬 추가 피해 +2.");
            G("titan_armor","타이탄 아머",GearSlot.Clothes,"tree",block:2,health:18,description:"거대한 생명력. 매 턴 방어도 +2, 최대 체력 +18.");
            G("ao_dai","아오자이",GearSlot.Clothes,"force",block:3,effect:"skill_bonus",amount:3,description:"고밀도 VF 의상. 매 턴 방어도 +3, 스킬 추가 피해 +3.");
            G("queen_heart","퀸 오브 하트",GearSlot.Clothes,"blood",block:4,health:16,effect:"turn_heal",amount:1,rarity:"초월",description:"심장을 지키는 초월 의상. 매 턴 방어도 +4와 체력 1 회복, 최대 체력 +16.");

            G("mithril_helm","미스릴 투구",GearSlot.Head,"mithril",block:2,evasion:5,description:"미스릴 투구. 매 턴 방어도 +2, 회피 +5%.");
            G("insight","인사이트",GearSlot.Head,"meteorite",attack:1,effect:"battle_start_block",amount:10,description:"위험을 읽는 시야. 공격력 +1, 전투 시작 방어도 10.");
            G("tactical_visor","택티컬 바이저",GearSlot.Head,"tree",block:1,effect:"skill_bonus",amount:2,description:"전술 보조 장치. 매 턴 방어도 +1, 스킬 추가 피해 +2.");
            G("wilderness_star","황야의 별",GearSlot.Head,"meteorite",attack:3,evasion:5,description:"황야를 누비는 모자. 공격력 +3, 회피 +5%.");
            G("opera_mask","변검",GearSlot.Head,"blood",attack:3,evasion:10,rarity:"초월",description:"얼굴을 숨기는 초월 가면. 공격력 +3, 회피 +10%.");

            G("mithril_shield","미스릴 방패",GearSlot.Arm,"mithril",block:3,description:"튼튼한 방패. 매 턴 방어도 +3.");
            G("cube_watch","큐브 워치",GearSlot.Arm,"meteorite",attack:2,effect:"attack_bonus",amount:1,description:"공격 흐름을 재는 시계. 공격력 +2, 기본 공격 추가 피해 +1.");
            G("skadi","스카디의 팔찌",GearSlot.Arm,"tree",effect:"skill_bonus",amount:3,description:"차가운 VF 기운. 스킬 추가 피해 +3.");
            G("auto_arms","오토-암즈",GearSlot.Arm,"force",attack:2,block:2,health:8,description:"자동 방어 장치. 공격력 +2, 매 턴 방어도 +2, 최대 체력 +8.");
            G("prominence","프로미넌스",GearSlot.Arm,"blood",attack:3,block:2,effect:"turn_heal",amount:1,rarity:"초월",description:"태양의 홍염. 공격력 +3, 매 턴 방어도 +2와 체력 1 회복.");

            G("mithril_boots","미스릴 부츠",GearSlot.Legs,"mithril",block:1,evasion:8,description:"경량 부츠. 매 턴 방어도 +1, 회피 +8%.");
            G("glacial_shoes","글레이셜 슈즈",GearSlot.Legs,"tree",evasion:5,effect:"skill_bonus",amount:2,description:"얼음 위의 발걸음. 회피 +5%, 스킬 추가 피해 +2.");
            G("galaxy_step","갤럭시 스텝",GearSlot.Legs,"meteorite",attack:2,evasion:8,description:"별빛 걸음. 공격력 +2, 회피 +8%.");
            G("hermes","헤르메스의 부츠",GearSlot.Legs,"force",health:10,evasion:12,description:"날개 달린 발걸음. 최대 체력 +10, 회피 +12%.");
            G("pink_shoes","분홍신",GearSlot.Legs,"blood",attack:2,evasion:15,effect:"turn_heal",amount:1,rarity:"초월",description:"멈추지 않는 춤. 공격력 +2, 회피 +15%, 매 턴 체력 1 회복.");
        }

        static void G(string id,string name,GearSlot slot,string objectId,int attack=0,int block=0,int health=0,int evasion=0,string weaponClass=null,string cardId=null,string effect=null,int amount=0,string rarity="전설",string description="") => Gear.Add(new GearDef { id=id,name=name,slot=slot,objectId=objectId,attack=attack,block=block,health=health,evasion=evasion,weaponClass=weaponClass,cardId=cardId,effect=effect,amount=amount,rarity=rarity,description=description });

        static void BuildEvents()
        {
            EventIdentity.Populate(Events, Characters);
        }

        static EventOption Choice(string label,string effect,int amount=0,string cardId=null,string objectId=null,string passiveId=null)
        {
            string outcome = effect=="heal" ? $"체력 {amount} 회복" : effect=="credits" ? $"{amount} 크레딧 획득" : effect=="damage" ? $"체력 {amount} 소모" : effect=="max_health" ? $"최대 체력 +{amount}" : effect=="object" ? $"{Object(objectId)?.name} 1개 획득" : effect=="card" ? $"{Card(cardId)?.name} 카드 획득" : effect=="passive" ? $"{Passive(passiveId)?.name} 패시브 획득" : effect=="rune_change" ? "메인·보조 룬을 다른 계열로 변경" : effect=="remove_card" ? "무작위 일반 카드 1장 제거" : effect=="upgrade_card" ? "무작위 일반 카드 1종 강화" : "잠시 이야기를 나눈다";
            return new EventOption { label=label,description=outcome,effect=effect,amount=amount,cardId=cardId,objectId=objectId,passiveId=passiveId };
        }

        static void Encounter(string id,string owner,string title,string story,EventOption a,EventOption b,EventOption c)
        {
            Events.Add(new EventDef { id="event_"+id,owner=owner,title=title,story=story,options=new[]{a,b,c} });
            if (Character(id)==null) Characters.Add(new CharacterDef { id=id,name=owner,cards=Array.Empty<string>() });
        }

        static void BuildCoreEvents()
        {
            Encounter("nia","니아","세이브 포인트","니아가 깨진 아케이드 화면을 두드린다. 하나의 탈출 경로는 아직 미해금 상태다.",Choice("블록 퍼즐을 푼다","card",cardId:"nia_q"),Choice("안전한 저장 지점을 만든다","heal",22),Choice("설정 파일을 다시 쓴다","rune_change"));
            Encounter("jackie","재키","붉은 제안","재키가 전기톱을 내려놓고 웃는다. 자신의 사냥 방식에 관심이 있느냐고 묻는다.",Choice("연참의 자세를 배운다","card",cardId:"jackie_q"),Choice("사냥감을 알려준다","credits",55),Choice("날이 닿기 전에 비켜선다","remove_card"));
            Encounter("aya","아야","깨진 무전기","아야가 경찰서에서 생존 신호를 보내고 있다. 하나가 가진 연구소 주파수로 답장이 잡힌다.",Choice("사격 훈련을 돕는다","card",cardId:"aya_q"),Choice("구호품 위치를 공유한다","heal",20),Choice("무전기 수리에 협력한다","credits",45));
            Encounter("hyunwoo","현우","학교 뒤의 주먹","현우는 말없이 낡은 샌드백을 붙잡아 준다. 하나에게 먼저 한 번 쳐보라고 고갯짓한다.",Choice("주먹을 끝까지 뻗는다","upgrade_card"),Choice("발 밟기를 배운다","card",cardId:"hyunwoo_q"),Choice("연습 뒤 숨을 고른다","max_health",6));
            Encounter("yuki","유키","마지막 단추","유키의 옷에서 단추 하나가 떨어졌다. 하나의 연구복 주머니에 마침 바늘과 실이 있다.",Choice("단추를 달아 준다","credits",50),Choice("검의 기본을 배운다","card",cardId:"yuki_q"),Choice("옷매무새를 고친다","heal",18));
            Encounter("hyejin","혜진","다른 운명","혜진은 하나가 이 세계의 흐름에 없는 사람이라고 말한다. 새 부적 세 장이 손 위에 놓인다.",Choice("새로운 운명을 고른다","rune_change"),Choice("제압부를 받아 든다","card",cardId:"hyejin_q"),Choice("낡은 인연을 끊는다","remove_card"));
            Encounter("sua","수아","끝나지 않은 책","수아가 도서관의 빈 책장을 펼친다. 하나가 읽는 순간 탈출 이야기가 한 문장 더 생긴다.",Choice("용감한 주인공을 상상한다","upgrade_card"),Choice("파랑새 이야기를 듣는다","card",cardId:"sua_w"),Choice("책 사이에서 쉰다","heal",24));
            Encounter("isol","아이솔","철사 너머의 길","아이솔이 골목의 트랩을 표시한다. 하나에게 길을 열어 주는 대신 흔적을 지워 달라고 한다.",Choice("폭탄 도면을 기억한다","card",cardId:"isol_q"),Choice("무거운 흔적을 버린다","remove_card"),Choice("보급 상자를 운반한다","credits",60));
            Encounter("nadine","나딘","숲의 발자국","나딘이 거대한 곰의 흔적을 살핀다. 하나가 뒤를 따르면 나무 사이로 안전한 쉼터가 보인다.",Choice("사냥의 눈을 익힌다","card",cardId:"nadine_q"),Choice("숲의 열매를 나눈다","heal",20),Choice("빛나는 나무를 찾는다","object",objectId:"tree"));
            Encounter("emma","엠마","모자 속의 비둘기","엠마가 하나를 관객으로 초대한다. 모자에서 탈출구 대신 비둘기와 카드가 쏟아진다.",Choice("첫 번째 마술을 배운다","card",cardId:"emma_q"),Choice("숨겨진 패를 내려놓는다","remove_card"),Choice("공연의 동전을 모은다","credits",55));
        }

        static void BuildRosterEvents()
        {
            Encounter("charlotte","샬럿","꺼지지 않는 빛","샬럿이 연구소의 깜박이는 비상등 아래에서 작은 빛을 피운다. 돌아갈 곳을 이야기하는 하나에게 희망은 함께 나누는 것이라고 말한다.",Choice("빛의 궤적을 배운다","card",cardId:"charlotte_e"),Choice("희망을 나눠 가진다","max_health",6),Choice("따스한 빛 아래에서 쉰다","heal",26));
            Encounter("nathapon","나타폰","멈춘 셔터","나타폰이 하나의 사진을 찍는다. 인화된 사진에는 현실의 연구소 창문이 흐릿하게 겹쳐 있다.",Choice("사진의 빛을 분석한다","upgrade_card"),Choice("안전한 풍경을 찍는다","heal",18),Choice("인화지를 정리한다","credits",45));
            Encounter("nicky","니키","스턴트의 기본","니키가 위험한 착지 장면을 연습한다. 하나에게 낙법과 주먹 중 무엇부터 배울지 묻는다.",Choice("낙법을 익힌다","max_health",5),Choice("분노의 어퍼컷을 배운다","card",cardId:"nicky_r"),Choice("촬영 장비를 옮긴다","credits",50));
            Encounter("daniel","다니엘","불길한 실루엣","다니엘이 하나의 머리카락과 실루엣을 살핀다. 벽의 그림자에는 그의 가위가 먼저 움직인다.",Choice("쓸모없는 패를 잘라낸다","remove_card"),Choice("새로운 스타일을 받아들인다","upgrade_card"),Choice("거울 앞에서 숨을 고른다","heal",16));
            Encounter("tia","띠아","세 가지 물감","띠아가 연구소 문을 빨강과 파랑, 노랑으로 그린다. 어떤 문이 진짜인지 하나에게 골라 달라고 한다.",Choice("빨간 선을 덧칠한다","upgrade_card"),Choice("파란 문 뒤에서 쉰다","heal",22),Choice("노란 빛의 원석을 찾는다","object",objectId:"meteorite"));
            Encounter("laura","라우라","보이지 않는 금고","라우라가 키오스크의 잠긴 금고를 보여 준다. 크레딧보다 지키고 싶은 것이 무엇인지 묻는다.",Choice("잊은 패를 금고에 넣는다","remove_card"),Choice("훔친 정보를 나눈다","credits",65),Choice("괴도의 발걸음을 익힌다","card",cardId:"laura_e"));
            Encounter("lenox","레녹스","긴 낚싯줄","레녹스가 낚싯줄을 당긴다. 물속에서 올라온 것은 물고기 대신 빛나는 재료 주머니다.",Choice("줄을 함께 당긴다","object",objectId:"tree"),Choice("낚시가 끝날 때까지 쉰다","heal",20),Choice("매듭을 다시 묶는다","upgrade_card"));
            Encounter("leon","레온","물 위의 호흡","레온이 텅 빈 수영장에 남은 물결을 가른다. 하나에게 긴장을 풀고 물의 흐름을 느껴 보라고 한다.",Choice("호흡 훈련을 한다","max_health",6),Choice("잠영의 호흡을 배운다","card",cardId:"leon_e"),Choice("시원한 물로 얼굴을 씻는다","heal",20));
            Encounter("rozzi","로지","두 발의 계산","로지가 탄환 두 발을 책상 위에 세운다. 탈출 작전에서 망설일 시간은 없다고 말한다.",Choice("이지샷을 연습한다","card",cardId:"rozzi_q"),Choice("작전의 빈틈을 없앤다","remove_card"),Choice("정찰 보고서를 작성한다","credits",60));
            Encounter("luke","루크","청소가 끝난 골목","루크가 골목의 파편을 쓸어 담는다. 하나의 길을 막던 장애물도 함께 치울 수 있다고 한다.",Choice("덱의 잡동사니를 버린다","remove_card"),Choice("빗자루를 도와 잡는다","credits",50),Choice("깨끗한 벤치에서 쉰다","heal",20));
            Encounter("dailin","리 다이린","빈 술병의 무게","리 다이린이 빈 술병을 흔들며 웃는다. 하나의 굳은 어깨를 보고 주먹보다 몸부터 풀라고 한다.",Choice("취권의 리듬을 배운다","upgrade_card"),Choice("마실 물을 나눈다","heal",20),Choice("병을 모아 되판다","credits",40));
            Encounter("rio","리오","한 발의 정사필중","리오가 과녁 한가운데에 화살을 놓는다. 손이 아닌 마음이 흔들려 빗나간 것이라고 한다.",Choice("마음을 가라앉힌다","remove_card"),Choice("하나레의 사격 자세를 배운다","card",cardId:"rio_w"),Choice("호흡과 자세를 단련한다","max_health",5));
            Encounter("martina","마르티나","기록되지 않은 연구원","마르티나가 하나에게 카메라를 향한다. 게임 세계에 나타난 연구원은 기사 한 장으로 설명되지 않는다.",Choice("탈출 계획을 인터뷰한다","credits",70),Choice("녹화 영상을 분석한다","upgrade_card"),Choice("촬영 뒤 따뜻한 차를 마신다","heal",18));
            Encounter("mai","마이","연구복 수선","마이가 하나의 찢어진 연구복을 보고 바느질 상자를 펼친다. 움직이기 좋은 옷이 살아남기 좋은 옷이다.",Choice("상처 난 소매를 꿰맨다","heal",24),Choice("안감을 두껍게 덧댄다","max_health",6),Choice("남은 미스릴 실을 받는다","object",objectId:"mithril"));
            Encounter("markus","마커스","금 간 바위","마커스가 무거운 도끼로 바위를 깨고 있다. 안쪽에서 번쩍이는 돌이 드러났지만 손이 모자란다.",Choice("광물을 함께 캐낸다","object",objectId:"meteorite"),Choice("도끼질의 힘을 익힌다","upgrade_card"),Choice("무거운 짐을 내려놓는다","remove_card"));
            Encounter("magnus","매그너스","엔진과 우정","매그너스의 바이크가 길을 가로막는다. 엔진을 고쳐 주면 좋은 부품을 하나 주겠다고 한다.",Choice("배선을 다시 연결한다","object",objectId:"force"),Choice("헤진 장비를 정리한다","remove_card"),Choice("엔진 열기로 손을 녹인다","heal",18));
            Encounter("vanya","바냐","나비의 낮잠","바냐의 나비들이 하나의 모니터 빛에 모인다. 날갯짓 사이로 잠깐 평온한 꿈이 열린다.",Choice("나비를 따라 잠든다","heal",25),Choice("꿈의 장면을 기억한다","upgrade_card"),Choice("눈앞의 빛을 잡는다","object",objectId:"tree"));
            Encounter("barbara","바바라","무인 포탑 점검","바바라가 부서진 포탑 옆에서 숫자를 중얼거린다. 하나가 연구원이라고 밝히자 회로도를 밀어 준다.",Choice("포탑을 재조정한다","upgrade_card"),Choice("보조 코어를 교환한다","object",objectId:"force"),Choice("점검 수당을 받는다","credits",65));
            Encounter("bernice","버니스","사냥꾼의 쉬는 시간","버니스가 나무 아래 장비를 정리한다. 무작정 뛰어드는 사냥보다 돌아올 길을 아는 사냥이 낫다고 한다.",Choice("사냥 덫을 놓는 법을 배운다","card",cardId:"bernice_w"),Choice("소박한 식사를 나눈다","heal",25),Choice("함께 길의 흔적을 지운다","remove_card"));
            Encounter("bianca","비앙카","밤의 손님","비앙카가 검붉은 잔을 내려놓는다. 하나의 VF 수치를 흥미롭게 바라보며 거래를 제안한다.",Choice("밀봉된 샘플을 받는다","object",objectId:"blood"),Choice("혈액 분석을 도와준다","credits",50),Choice("달빛 아래 조용히 쉰다","heal",18));
            Encounter("celine","셀린","폭발물의 안전거리","셀린이 폭탄 세 개를 차례로 놓는다. 하나에게 가장 안전한 폭발 순서를 계산해 달라고 한다.",Choice("기폭 순서를 최적화한다","upgrade_card"),Choice("실험용 폭탄을 받는다","card",cardId:"celine_q"),Choice("미사용 도선을 판매한다","credits",55));
            Encounter("sho","쇼우","셰프의 한 그릇","쇼우가 큰 냄비 앞에서 손짓한다. 탈출도 배가 든든해야 하는 법이라며 접시를 세 개 내민다.",Choice("뜨끈한 음식을 먹는다","heal",30),Choice("힘이 나는 식단을 고른다","max_health",7),Choice("오늘의 식재료를 정리한다","credits",40));
            Encounter("shoichi","쇼이치","잔업 없는 계약","쇼이치가 손상된 계약서를 넘긴다. 하나의 탈출은 투자할 만한 일인지 냉정하게 계산한다.",Choice("계약의 숫자를 검토한다","credits",75),Choice("위험한 조항을 지운다","remove_card"),Choice("효율적인 공격을 배운다","upgrade_card"));
            Encounter("sissela","시셀라","윌슨의 걱정","시셀라가 윌슨을 끌어안고 있다. 하나가 구급함을 꺼내자 조금만 곁에 있어 달라고 말한다.",Choice("구급함을 함께 사용한다","heal",26),Choice("윌슨을 수선한다","max_health",5),Choice("불안한 기록을 지운다","remove_card"));
            Encounter("silvia","실비아","달려야 하는 이유","실비아가 바이크 뒤를 두드린다. 이 게임의 끝에 무엇이 있는지 같이 확인해 보자고 한다.",Choice("기동전의 감각을 익힌다","card",cardId:"silvia_r"),Choice("새 부품을 단다","object",objectId:"mithril"),Choice("피트에서 숨을 고른다","heal",20));
            Encounter("adela","아델라","비어 있는 왕의 칸","아델라가 체스판을 내민다. 하나의 왕은 이미 포위됐지만 탈출할 한 칸이 남아 있다.",Choice("다음 수를 계산한다","upgrade_card"),Choice("희생할 패를 정한다","remove_card"),Choice("차분히 승리를 완성한다","credits",65));
            Encounter("adriana","아드리아나","남은 불꽃","아드리아나가 꺼진 가로등 주변에 불을 놓는다. 하나는 불길 속에서도 길이 이어지는 것을 본다.",Choice("불꽃의 궤적을 연구한다","card",cardId:"adriana_q"),Choice("남아 있는 잔열에 쉰다","heal",18),Choice("타지 않은 재료를 챙긴다","object",objectId:"meteorite"));
            Encounter("adina","아디나","다른 하늘의 별자리","아디나는 이 게임의 하늘에 하나의 별자리가 없다고 한다. 종이에 새 별 세 개를 이어 그린다.",Choice("새 별자리를 따른다","rune_change"),Choice("관측 기록을 다듬는다","upgrade_card"),Choice("떨어진 별 조각을 받는다","object",objectId:"meteorite"));
            Encounter("isaac","아이작","네 번째 경고","아이작이 하나의 길을 막는다. 잠깐의 훈련만 버틸 수 있다면 자기 방식도 알려 주겠다고 한다.",Choice("버티는 방법을 익힌다","max_health",7),Choice("약한 패를 버린다","remove_card"),Choice("현장 급습의 자세를 익힌다","card",cardId:"isaac_q"));
            Encounter("alex","알렉스","가려진 신원","알렉스가 하나의 출입증을 살핀다. 코드가 다른 세계에서 발급됐다는 사실을 알아차린다.",Choice("펄스 스팅의 움직임을 익힌다","card",cardId:"alex_e"),Choice("추적 기록을 지운다","remove_card"),Choice("정보의 대가를 받는다","credits",60));
            Encounter("jan","얀","링 밖의 수업","얀이 골목에 임시 링을 그린다. 살아 돌아가기 위한 싸움에는 정해진 라운드가 없다고 한다.",Choice("기본 자세를 바로잡는다","upgrade_card"),Choice("맷집을 단련한다","max_health",6),Choice("니 스트라이크를 배운다","card",cardId:"jan_q"));
            Encounter("estelle","에스텔","구조 우선순위","에스텔이 위험한 건물의 출구를 표시한다. 하나에게 자신의 숨부터 고르고 움직이라고 한다.",Choice("구조용 응급처치를 받는다","heal",28),Choice("안전한 탈출 동선을 짠다","remove_card"),Choice("내화 장비를 챙긴다","object",objectId:"mithril"));
            Encounter("aiden","에이든","불안정한 전압","에이든의 검 주위로 전류가 튄다. 하나의 분석기로 보면 파형은 탈출 신호와 닮아 있다.",Choice("전류의 파형을 조정한다","upgrade_card"),Choice("남은 코어를 모은다","object",objectId:"force"),Choice("충전이 끝나기를 기다린다","heal",18));
            Encounter("echion","에키온","VF 의수의 균열","에키온이 의수의 칼날을 접는다. 하나가 알고 있는 연구소의 장치와 연결될 수 있어 보인다.",Choice("의수의 회로를 연구한다","upgrade_card"),Choice("남은 VF 재료를 나눈다","object",objectId:"mithril"),Choice("메마른 송곳니의 돌진을 익힌다","card",cardId:"echion_e"));
            Encounter("elena","엘레나","얼어붙은 링크","엘레나가 얼음 위에 하나의 발자국을 지운다. 미끄러지기 전에 중심부터 잡으라고 한다.",Choice("스파이럴의 균형을 익힌다","card",cardId:"elena_e"),Choice("차가운 생각을 털어낸다","remove_card"),Choice("링크 옆에서 몸을 녹인다","heal",20));
            Encounter("johann","요한","조용한 성당","요한이 하나에게 잠시 앉으라고 권한다. 탈출을 재촉하는 경보도 이곳에서는 희미하게 들린다.",Choice("응급처치를 받는다","heal",30),Choice("마음의 짐을 내려놓는다","remove_card"),Choice("다시 일어설 힘을 얻는다","max_health",5));
            Encounter("william","윌리엄","돌아오는 공","윌리엄의 공이 담을 넘어 하나의 발밑에 떨어진다. 제대로 돌려주려면 손목에 힘을 빼야 한다.",Choice("던지는 자세를 배운다","upgrade_card"),Choice("분실된 공을 모은다","credits",50),Choice("훈련 뒤 간식을 나눈다","heal",20));
            Encounter("irem","이렘","골목의 고양이","이렘이 햇볕이 남은 창틀에서 하나를 바라본다. 부드러운 발걸음으로 길을 알려 주는 듯하다.",Choice("사뿐한 보폭을 따른다","card",cardId:"irem_e"),Choice("양지에서 잠깐 쉰다","heal",25),Choice("낡은 방울을 정리한다","remove_card"));
            Encounter("eva","이바","푸른 잔광","이바가 손끝의 VF 빛을 모은다. 하나가 연구소로 돌아갈 수 있는지 먼저 묻는다.",Choice("VF 패턴을 함께 읽는다","upgrade_card"),Choice("푸른 재료를 모은다","object",objectId:"force"),Choice("서로의 상태를 확인한다","heal",22));
            Encounter("ian","이안","거울 속의 목소리","이안은 깨진 거울을 보지 않으려 한다. 하나의 뒤에서 다른 목소리가 잠깐 들린다.",Choice("내면의 목소리를 기록한다","upgrade_card"),Choice("해로운 기억을 놓아준다","remove_card"),Choice("거울을 덮고 쉰다","heal",20));
            Encounter("eleven","일레븐","오늘의 방송","일레븐이 하나를 임시 방송의 특별 손님으로 소개한다. 연구복을 입고 게임을 하는 모습이 신기하다고 한다.",Choice("버거 시식에 참여한다","heal",30),Choice("생존 팁을 방송한다","credits",70),Choice("다음 도전에 힘을 보탠다","max_health",6));
            Encounter("zahir","자히르","진리의 질문","자히르가 하나에게 믿고 있는 끝이 무엇인지 묻는다. 돌아가야 할 세계라는 대답에 차크람을 내려놓는다.",Choice("나라야나스트라의 궤적을 배운다","card",cardId:"zahir_q"),Choice("다른 길을 받아들인다","rune_change"),Choice("의심 하나를 지운다","remove_card"));
            Encounter("jenny","제니","즉흥적인 무대","제니가 버려진 극장을 무대로 만든다. 하나에게 탈출한 연구원 역할을 미리 연기해 보라고 한다.",Choice("다음 장면을 연습한다","upgrade_card"),Choice("관객의 팁을 모은다","credits",55),Choice("막간에 조용히 쉰다","heal",20));
            Encounter("camilo","카밀로","두엔데의 리듬","카밀로가 조용한 골목에서 박자를 밟는다. 하나의 긴장한 발걸음도 음악의 일부가 된다.",Choice("알 꼼빠스의 리듬을 배운다","card",cardId:"camilo_e"),Choice("호흡을 길게 이어 간다","max_health",5),Choice("무거운 박자를 버린다","remove_card"));
            Encounter("karla","칼라","해적의 지도","칼라가 낡은 지도를 펼친다. 보물 표시 하나가 연구소 출구 표시와 겹쳐 있다.",Choice("표시된 상자를 연다","credits",75),Choice("항해용 재료를 교환한다","object",objectId:"meteorite"),Choice("지도에서 위험을 지운다","remove_card"));
            Encounter("cathy","캐시","야전 진료","캐시가 하나의 맥박과 상처를 확인한다. 연구 기록보다 지금의 환자가 먼저라고 말한다.",Choice("세심한 치료를 받는다","heal",32),Choice("재활 운동을 배운다","max_health",7),Choice("구급 장비를 정돈한다","credits",45));
            Encounter("chloe","클로에","니나의 실","클로에가 니나의 엉킨 실을 풀고 있다. 하나가 한쪽을 잡아 주자 작은 인형이 손을 흔든다.",Choice("끊어진 실을 잇는다","upgrade_card"),Choice("불필요한 매듭을 푼다","remove_card"),Choice("인형 곁에서 쉰다","heal",22));
            Encounter("chiara","키아라","날개 없는 성상","키아라가 성상의 깨진 날개를 바라본다. 하나가 도착할 수 있는 곳에 관해 조용히 이야기한다.",Choice("빛을 향해 다시 선다","max_health",6),Choice("흔들리는 믿음을 바꾼다","rune_change"),Choice("낡은 조각을 놓아준다","remove_card"));
            Encounter("tazia","타지아","유리의 탈출구","타지아가 유리 조각들을 작은 문 모양으로 세운다. 문은 열리지 않지만 빛의 방향은 알려 준다.",Choice("빛의 각도를 계산한다","upgrade_card"),Choice("날카로운 파편을 정리한다","remove_card"),Choice("반짝이는 재료를 받는다","object",objectId:"meteorite"));
            Encounter("theodore","테오도르","먼 곳의 신호","테오도르가 조준경 너머 연구소를 확인한다. 하나가 보지 못한 안전한 길을 천천히 짚어 준다.",Choice("에너지 포 사격을 배운다","card",cardId:"theodore_q"),Choice("정찰 보수를 받는다","credits",60),Choice("안전한 엄폐물에서 쉰다","heal",22));
            Encounter("felix","펠릭스","꺾이지 않는 창","펠릭스가 무너진 문을 창으로 받친다. 하나가 지나가기 전까지 이 자세를 지킬 수 있다고 한다.",Choice("창끝의 집중을 배운다","upgrade_card"),Choice("버티는 자세를 익힌다","max_health",6),Choice("문 너머 재료를 챙긴다","object",objectId:"mithril"));
            Encounter("priya","프리야","꽃이 핀 자리","프리야의 연주를 따라 작은 꽃들이 피어난다. 하나의 신호 수신기도 잠깐 부드러운 소리를 낸다.",Choice("연주에 귀를 기울인다","heal",28),Choice("꽃의 기운을 받는다","object",objectId:"tree"),Choice("낯선 화음을 선택한다","rune_change"));
            Encounter("fiora","피오라","펜싱의 간격","피오라가 하나의 검끝을 바로잡는다. 한 걸음을 절약하는 것이 한 번 더 살아남는 길이다.",Choice("정확한 찌르기를 배운다","upgrade_card"),Choice("어긋난 자세를 버린다","remove_card"),Choice("발걸음을 단련한다","max_health",5));
            Encounter("piolo","피올로","커피와 수련","피올로가 수련을 끝내고 따뜻한 커피를 내린다. 하나에게 힘을 빼는 연습도 하라고 말한다.",Choice("따뜻한 한 잔을 마신다","heal",25),Choice("기본 타격을 다듬는다","upgrade_card"),Choice("남은 주문을 돕는다","credits",45));
            Encounter("hart","하트","전원이 없는 앰프","하트가 전원 없이도 기타를 울린다. 하나가 배선을 연결하자 연구소 경보가 음악으로 바뀐다.",Choice("라이브를 끝까지 듣는다","heal",26),Choice("공연 팁을 나눈다","credits",60),Choice("새로운 리듬을 선택한다","rune_change"));
            Encounter("haze","헤이즈","닫힌 거래 상자","헤이즈가 튼튼한 무기 상자를 보여 준다. 하나에게 필요한 화력이 무엇인지 묻는다.",Choice("40mm 유탄의 탄도를 배운다","card",cardId:"haze_q"),Choice("포장 안의 코어를 받는다","object",objectId:"force"),Choice("거래 장부를 정리한다","credits",65));

            Encounter("debi_marlene","데비&마를렌","둘이서 여는 문","데비와 마를렌이 하나에게 서로 다른 길을 가리킨다. 문을 열려면 두 의견의 박자가 맞아야 한다.",Choice("파란 길을 함께 지킨다","heal",23),Choice("붉은 길을 베어 연다","upgrade_card"),Choice("겹치는 선택을 줄인다","remove_card"));
            Encounter("arda","아르다","게임보다 오래된 유물","아르다가 땅속의 낡은 문자를 읽는다. 하나의 출입증과 유물에 같은 문양이 새겨져 있다.",Choice("고대 문양을 연구한다","upgrade_card"),Choice("유물의 별 조각을 받는다","object",objectId:"meteorite"),Choice("탐사 보상을 나눈다","credits",55));
            Encounter("abigail","아비게일","사라진 발자국","아비게일의 발자국이 벽 앞에서 끊긴다. 하나가 돌아보자 그녀는 벽 반대편에서 손을 흔든다.",Choice("차원의 틈을 관찰한다","card",cardId:"abigail_e"),Choice("오래된 흔적을 지운다","remove_card"),Choice("빈 방에서 잠시 쉰다","heal",22));
            Encounter("alonso","알론소","붙어 버린 부품","알론소 주변의 금속 부품들이 서로 달라붙는다. 하나가 나침반을 들자 바늘도 그를 향해 돌아간다.",Choice("자기장을 안정시킨다","upgrade_card"),Choice("붙은 미스릴을 떼어낸다","object",objectId:"mithril"),Choice("튼튼한 자세를 연습한다","max_health",6));
            Encounter("leni","레니","오늘은 새로운 장난","레니가 숨겨 놓은 세 상자를 꺼낸다. 하나가 웃을 때까지 장난을 멈추지 않을 태세다.",Choice("장난의 답을 맞힌다","credits",65),Choice("곰 인형 옆에 앉는다","heal",24),Choice("새로운 규칙을 고른다","rune_change"));
            Encounter("tsubame","츠바메","반딧불 거리의 의뢰","츠바메가 하나의 탈출 의뢰를 받아 적는다. 어떤 길이든 대가를 치르면 열 수 있다고 한다.",Choice("안개의 술을 익힌다","card",cardId:"tsubame_e"),Choice("의뢰의 증거를 정리한다","remove_card"),Choice("일을 마친 보수를 나눈다","credits",60));
            Encounter("kenneth","케네스","분노를 지키는 불","케네스가 작은 불씨를 손에 쥔다. 잃어버린 사람을 찾는 마음이 하나의 귀환 의지와 닮아 있다.",Choice("불씨를 다루는 법을 듣는다","upgrade_card"),Choice("온기로 몸을 회복한다","heal",25),Choice("떨어진 나무 재료를 줍는다","object",objectId:"tree"));
            Encounter("katja","카티야","잿빛 조준선","카티야가 지도 위에 아주 가는 선을 긋는다. 하나가 그 선을 벗어나면 살아 돌아갈 수 있다고 한다.",Choice("조준 사격의 호흡을 익힌다","card",cardId:"katja_q"),Choice("표적 기록을 폐기한다","remove_card"),Choice("감시의 대가를 받는다","credits",65));
            Encounter("darko","다르코","탈출의 채무","다르코가 하나의 탈출 비용을 손가락으로 센다. 연구소가 그에게 빚이 있다는 설명에 웃음을 터뜨린다.",Choice("보상 청구서를 작성한다","credits",80),Choice("불필요한 채무를 지운다","remove_card"),Choice("버티는 힘을 얻는다","max_health",6));
            Encounter("lenore","르노어","끝나지 않는 노래","르노어의 목소리가 빈 극장의 벽을 울린다. 하나의 두려움도 음표 하나가 되어 밖으로 흘러나간다.",Choice("노래가 끝날 때까지 쉰다","heal",28),Choice("새 선율을 따라간다","rune_change"),Choice("기록의 음량을 조정한다","upgrade_card"));
            Encounter("garnet","가넷","다시 잡은 손","가넷은 하나가 떠날 것인지 먼저 묻는다. 하나는 돌아갈 곳과 남겨 둘 약속에 관해 이야기한다.",Choice("약속을 잊지 않겠다고 한다","max_health",7),Choice("같이 상처를 돌본다","heal",28),Choice("부서진 표식을 정리한다","remove_card"));
            Encounter("yumin","유민","바람이 쉬는 자리","유민이 바람의 흐름을 읽고 하나에게 멈추라고 한다. 바로 다음 순간 위험한 신호가 골목을 스친다.",Choice("바람의 걸음을 배운다","card",cardId:"yumin_e"),Choice("쉼터에서 쉬어 간다","heal",23),Choice("새 방향으로 마음을 돌린다","rune_change"));
            Encounter("hisui","히스이","검집에 남은 무게","히스이가 검을 뽑지 않은 채 문을 지킨다. 하나에게 지나가려면 먼저 발걸음을 정하라고 한다.",Choice("거합의 중심을 배운다","card",cardId:"hisui_w"),Choice("마음을 단단히 다진다","max_health",6),Choice("짐 하나를 내려놓는다","remove_card"));
            Encounter("justyna","유스티나","히어로의 비상계획","유스티나가 하나의 탈출을 새로운 히어로 작전으로 선언한다. 분주한 설명 뒤에는 이미 세 계획이 적혀 있다.",Choice("구조 작전에 참여한다","heal",25),Choice("포격 계획을 다듬는다","upgrade_card"),Choice("히어로의 지원금을 받는다","credits",65));
            Encounter("istvan","이슈트반","다른 가능성","이슈트반 옆에 또 다른 선택의 흔적이 겹친다. 하나에게 지금의 세계만이 답일 필요는 없다고 한다.",Choice("다른 가능성을 선택한다","rune_change"),Choice("겹친 기록 하나를 없앤다","remove_card"),Choice("가장 강한 가능성을 남긴다","upgrade_card"));
            Encounter("xuelin","슈린","어검이 여는 길","슈린이 날아가는 검으로 길의 장애물을 치운다. 하나에게 검끝보다 멀리 있는 목적을 보라고 한다.",Choice("어검의 움직임을 배운다","card",cardId:"xuelin_q"),Choice("단단한 검의 재료를 받는다","object",objectId:"mithril"),Choice("긴 호흡을 단련한다","max_health",5));
            Encounter("henry","헨리","멈춘 시계의 틈","헨리가 하나의 수신기 옆에 시계를 놓는다. 서로 다른 세계의 시간이 잠깐 같은 속도로 움직인다.",Choice("동기화 기록을 저장한다","upgrade_card"),Choice("불필요한 시간을 덜어낸다","remove_card"),Choice("정지한 순간에 쉰다","heal",25));
            Encounter("blair","블레어","VF 사냥꾼의 감지","블레어의 장치가 하나의 VF 흔적을 포착한다. 탈출하려는 연구원이라는 설명 뒤에 무기를 조금 내린다.",Choice("추적 장치를 교정한다","upgrade_card"),Choice("남은 샘플을 나눈다","object",objectId:"blood"),Choice("끝없는 추적의 움직임을 익힌다","card",cardId:"blair_r"));
            Encounter("mirka","미르카","거리의 온기","미르카가 골목의 문을 열어 하나를 안으로 들인다. 이 거리를 지나는 동안에는 자기 뒤에 있으라고 한다.",Choice("함께 몸을 단련한다","max_health",7),Choice("뜨거운 식사를 나눈다","heal",28),Choice("거리의 물자를 정리한다","credits",55));
            Encounter("fenrir","펜리르","목줄 없는 발걸음","펜리르가 연구소의 봉쇄선을 노려본다. 하나의 탈출 계획을 듣고 자신의 앞길도 막지 말라고 한다.",Choice("앞으로 나아갈 힘을 얻는다","upgrade_card"),Choice("사냥의 보폭을 익힌다","card",cardId:"fenrir_e"),Choice("무거운 표식을 끊어낸다","remove_card"));
            Encounter("coraline","코렐라인","거울이 밝히는 것","코렐라인이 흰 거울과 검은 거울을 번갈아 보여 준다. 연구소로 돌아온 하나의 얼굴이 양쪽에서 다르게 웃는다.",Choice("새로운 자신을 고른다","rune_change"),Choice("거짓된 흔적을 지운다","remove_card"),Choice("거울의 빛을 연구한다","upgrade_card"));
            Encounter("bihyung","비형","가면 너머의 웃음","비형이 하나에게 가면을 내민다. 이 게임의 규칙에도 장난을 걸 수 있지 않겠느냐고 웃는다.",Choice("규칙의 빈틈을 고른다","rune_change"),Choice("도깨비 불을 다루는 법을 배운다","card",cardId:"bihyung_w"),Choice("장난의 동전을 받는다","credits",65));
            Encounter("craver","크레이버","깨지지 않는 갈망","크레이버가 길 한복판의 장애물을 부순다. 하나의 탈출을 듣더니 도망가는 길에도 싸움은 있다고 말한다.",Choice("강한 타격을 배운다","upgrade_card"),Choice("부서진 코어를 챙긴다","object",objectId:"force"),Choice("버티는 몸을 만든다","max_health",7));
            Encounter("lucia","루치아","총사의 약속","루치아가 하나에게 정중히 여정을 묻는다. 세레스를 기다리는 마음으로 하나의 귀환도 응원하겠다고 한다.",Choice("총사의 자세를 익힌다","card",cardId:"lucia_q"),Choice("다시 만날 약속을 한다","max_health",5),Choice("찻잔 옆에서 쉬어 간다","heal",25));
            Encounter("ceres","세레스","이어 붙인 검","세레스가 검의 작은 조각을 하나씩 모은다. 누군가를 지키기 위해 다시 검을 쥐는 마음이 전해진다.",Choice("검의 조각을 함께 잇는다","upgrade_card"),Choice("약속의 무게를 나눈다","max_health",6),Choice("함께 차를 마신다","heal",25));
        }
    }
}
