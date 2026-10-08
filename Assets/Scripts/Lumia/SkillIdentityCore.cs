using System.Collections.Generic;

namespace Lumia
{
    // Source: the Q/W/E/R tooltips retained in docs/art-chibi/reference-skills.json.
    // Values are deliberately small, turn-based fan-game adaptations.
    public static class SkillIdentityCore
    {
        static SkillRule Gain(string key, string label, int amount=1, int cap=3, bool hit=false) => new SkillRule { op="gain", key=key, label=label, amount=amount, cap=cap, onHit=hit };
        static SkillRule Set(string key, string label, int amount, int cap=3) => new SkillRule { op="set", key=key, label=label, amount=amount, cap=cap };
        static SkillRule Consume(string key, int amount=0, bool hit=false) => new SkillRule { op="consume", key=key, amount=amount, onHit=hit };
        static SkillRule Bonus(string op, int amount, string key=null, int cap=3) => new SkillRule { op=op, amount=amount, scaleKey=key, cap=cap };
        static SkillRule When(SkillRule rule, string key, int amount=1) { rule.conditionKey=key; rule.conditionAmount=amount; return rule; }
        static SkillRule Effect(string op, string key, string label, int amount, int duration=2, int delay=1) => new SkillRule { op=op, key=key, label=label, amount=amount, duration=duration, delay=delay };
        static SkillRule Discount(string card, int amount=1) => new SkillRule { op="discount", targetCard=card, amount=amount };
        static SkillMechanicProfile P(string summary, params SkillRule[] rules) => new SkillMechanicProfile { summary=summary, rules=rules, poison=0, strength=0 };

        public static void Populate(Dictionary<string,SkillMechanicProfile> p)
        {
            var q=P("아케이드 블록을 최대 3개 남기고 적중 시 배터리를 충전합니다.", Gain("blocks","아케이드 블록"), Gain("battery","VF 배터리",1,4,true));
            q.damage=7; q.draw=0; p["nia_q"]=q;
            q=P("블록마다 추가 피해를 주고 블록을 회수하며 다음 Q의 코스트를 1 줄입니다.", Bonus("bonus_damage",4,"blocks"), Consume("blocks"), Discount("nia_q"), Gain("battery","VF 배터리",1,4,true));
            q.damage=10; q.vulnerable=1; p["nia_w"]=q;
            q=P("2턴 동안 치명상을 한 번 막고 체력을 20 회복합니다.", Effect("revive","one_up","1UP",20));
            q.block=8; q.heal=0; q.evasion=0; p["nia_e"]=q;
            q=P("배터리를 보호막으로 바꾸고 게임월드를 2턴 유지합니다.", Bonus("bonus_block",5,"battery",4), Consume("battery"), Effect("summon","world","게임월드",4));
            q.damage=20; q.block=10; q.draw=0; p["nia_r"]=q;

            q=P("적중하면 출혈을 남기며 출혈이 3중첩일 때 연참이 강화됩니다.", Effect("bleed","bleeding","출혈",3), Gain("bleeding","출혈 표식",1,3,true), When(Bonus("bonus_damage",4),"bleeding",3));
            q.rules[0].onHit=true; q.damage=5; q.hits=2; q.heal=3; q.freeCastTargets=new[]{"jackie_q"}; q.freeCastCount=1; q.freeCastOnHit=true; p["jackie_q"]=q;
            q=P("다음 기본 공격의 피해를 8 높입니다.", Effect("empower_basic","tendon","힘줄 절단",8));
            q.damage=0; q.weak=0; p["jackie_w"]=q;
            q=P("출혈 표식이 가득 찬 적에게 추가 피해를 줍니다.", When(Bonus("bonus_damage",4),"bleeding",3));
            q.damage=16; q.evasion=20; q.duration=1; p["jackie_e"]=q;
            q=P("전기톱으로 다음 기본 공격을 강화하고 다음 턴 종료에 학살을 터뜨립니다.", Effect("empower_basic","chainsaw","전기톱",8), Effect("delayed_damage","massacre","학살",10,1,2));
            q.damage=30; p["jackie_r"]=q;

            q=P("적중하면 연발 준비를 쌓으며 공포에 걸린 적에게 추가 피해를 줍니다.", Gain("rapid","연발 준비",1,2,true), When(Bonus("bonus_damage",2),"fear"), Consume("fear",1,true));
            q.damage=5; q.hits=2; p["aya_q"]=q;
            q=P("쌓인 연발 준비를 소비해 추가 피해를 줍니다.", Bonus("bonus_damage",2,"rapid",2), Consume("rapid"), When(Bonus("bonus_damage",2),"fear"), Consume("fear",1,true));
            q.damage=4; q.hits=4; p["aya_w"]=q;
            q=P("위치를 옮기며 다음 Q와 W의 코스트를 각각 1 줄입니다.", Discount("aya_q"), Discount("aya_w"));
            q.block=6; q.draw=0; q.evasion=25; q.duration=2; p["aya_e"]=q;
            q=P("공포를 남겨 다음 사격의 피해를 높입니다.", Set("fear","공포",1,1));
            q.damage=38; q.weak=2; p["aya_r"]=q;

            q=P("적중하면 돌진 기세를 얻습니다.", Gain("momentum","돌진 기세",1,2,true));
            q.damage=10; q.weak=1; q.evasion=15; q.duration=1; p["hyunwoo_q"]=q;
            q=P("버티는 동안 적의 공격에 턴당 한 번 반격합니다.", Effect("counter","guard","허세 반격",4));
            q.block=14; q.heal=0; p["hyunwoo_w"]=q;
            q=P("돌진 기세를 소비해 벽으로 몰아붙이는 피해를 높입니다.", Bonus("bonus_damage",3,"momentum",2), Consume("momentum"));
            q.damage=16; q.weak=1; q.vulnerable=1; p["hyunwoo_e"]=q;
            q=P("충격파가 턴 종료에 한 번 더 피해를 줍니다.", Effect("delayed_damage","punch","충격파",8,1,1));
            q.damage=42; q.vulnerable=1; p["hyunwoo_r"]=q;

            q=P("단추를 하나 소비하면 공격이 강화됩니다.", When(Bonus("bonus_damage",4),"cuff"), Consume("cuff",1,true));
            q.damage=6; q.weak=1; p["yuki_q"]=q;
            q=P("단추를 3개 보충하고 E의 연계 사용권을 얻습니다.", Set("cuff","단추",3));
            q.block=12; q.draw=0; p["yuki_w"]=q;
            q=P("적중하면 같은 턴에 빗겨치기를 한 번 다시 사용할 수 있습니다.");
            q.damage=16; q.block=6; q.evasion=15; q.duration=1; p["yuki_e"]=q;
            q=P("새긴 검흔이 턴 종료에 폭발합니다.", Effect("delayed_damage","scar","검흔",8,1,1));
            q.damage=42; q.vulnerable=1; p["yuki_r"]=q;

            q=P("적중하면 흉조를 쌓고 한 번 다시 사용할 수 있습니다.", Gain("omen","흉조",1,3,true));
            q.damage=14; q.weak=1; p["hyejin_q"]=q;
            q=P("지면에 놓은 부적이 턴 종료에 피해를 줍니다.", Effect("delayed_damage","charm","제압 부적",10));
            q.damage=0; q.weak=1; q.vulnerable=0; p["hyejin_w"]=q;
            q=P("이동 부적을 남기며 다시 사용하면 부적을 소비해 추가 피해를 줍니다.", When(Bonus("bonus_damage",4),"nomad"), When(Consume("nomad"),"nomad"), When(Set("nomad","이동 부적",1,1),"nomad",0));
            q.rules[2].conditionExact=true; q.damage=12; q.evasion=25; q.duration=1; q.freeCastTargets=new[]{"hyejin_e"}; q.freeCastCount=1; q.freeCastOnHit=true; p["hyejin_e"]=q;
            q=P("흉조를 소비해 피해를 높이고 오대존명왕을 2턴 유지합니다.", Bonus("bonus_damage",2,"omen"), Consume("omen"), Effect("summon","orbit","오대존명왕",5));
            q.damage=5; q.hits=5; q.weak=1; p["hyejin_r"]=q;

            q=P("책갈피를 남기며 기존 책갈피가 있으면 추가 피해를 줍니다.", When(Bonus("bonus_damage",6),"bookmark"), Set("bookmark","책갈피",1,1));
            q.rules[1].onHit=true; q.damage=12; q.vulnerable=1; p["sua_q"]=q;
            q=P("파랑새가 보호막을 보충하며 적을 실명시킵니다.", Effect("guard","bluebird","파랑새 보호",2));
            q.block=10; q.weak=1; p["sua_w"]=q;
            q=P("책갈피가 있는 적에게 추가 피해를 주고 책갈피를 다시 남깁니다.", When(Bonus("bonus_damage",6),"bookmark"), Set("bookmark","책갈피",1,1));
            q.rules[1].onHit=true; q.damage=10; q.heal=3; q.evasion=15; q.duration=1; p["sua_e"]=q;
            q=P("마지막으로 사용한 수아의 Q·W·E 중 하나를 기억해 다시 사용합니다.");
            q.block=8; p["sua_r"]=q;

            q=P("부착 폭탄을 남기고 폭탄 공격 준비를 쌓습니다.", Effect("delayed_damage","semtex","셈텍스",8,1,2), Gain("charge","폭탄 공격 준비"));
            q.damage=6; p["isol_q"]=q;
            q=P("폭탄 공격 준비에 따라 추가 피해를 주고 준비를 한 단계 높입니다.", Bonus("bonus_damage",2,"charge"), Gain("charge","폭탄 공격 준비",1,3,true));
            q.damage=3; q.hits=5; q.weak=1; p["isol_w"]=q;
            q=P("은신 후 다음 기본 공격의 피해를 6 높입니다.", Effect("empower_basic","stealth","은신 사격",6));
            q.evasion=35; q.duration=2; q.draw=0; p["isol_e"]=q;
            q=P("지뢰가 적의 공격에 반응하며 다음 턴 종료에 폭발합니다.", Effect("counter","mine","MOK 지뢰",8), Effect("delayed_damage","mine_end","지뢰 폭발",10,1,2));
            q.damage=12; p["isol_r"]=q;

            q=P("조준을 이어 갈수록 화살의 피해가 높아집니다.", Bonus("bonus_damage",2,"aim",2), Gain("aim","조준",1,2));
            q.damage=10; p["nadine_q"]=q;
            q=P("다람쥐 덫이 적의 공격에 턴당 한 번 피해를 줍니다.", Effect("counter","trap","다람쥐 덫",5));
            q.damage=0; q.weak=1; q.vulnerable=0; p["nadine_w"]=q;
            q=P("와이어로 조준을 준비하고 다음 기본 공격을 강화합니다.", Gain("aim","조준",1,2), Effect("empower_basic","wire","와이어 사격",5));
            q.draw=0; q.evasion=25; q.duration=2; p["nadine_e"]=q;
            q=P("늑대가 3턴 동안 추격 피해를 줍니다.", Effect("summon","wolf","늑대 맹습",5,3));
            q.damage=14; q.hits=2; p["nadine_r"]=q;

            q=P("비둘기를 남기며 남아 있는 비둘기와 함께 추가 피해를 줍니다.", When(Bonus("bonus_damage",4),"dove"), Set("dove","비둘기",1,1));
            q.damage=7; q.draw=0; p["emma_q"]=q;
            q=P("모자를 설치해 턴 종료에 폭발시키고 모자 잔상을 남깁니다.", Effect("delayed_damage","hat","모자 폭발",10), Set("hat","모자 잔상",1,1));
            q.damage=0; q.vulnerable=0; q.freeCastCount=0; p["emma_w"]=q;
            q=P("토끼를 남기고 적을 변이시킵니다.", Set("rabbit","토끼",1,1));
            q.damage=0; q.weak=2; q.heal=0; q.block=5; p["emma_e"]=q;
            var dove=When(Bonus("bonus_damage",6),"dove"); dove.conditionPrevious="emma_q";
            var hat=When(Bonus("bonus_damage",8),"hat"); hat.conditionPrevious="emma_w";
            var rabbit=When(Bonus("bonus_block",8),"rabbit"); rabbit.conditionPrevious="emma_e";
            var consumeDove=When(Consume("dove"),"dove"); consumeDove.conditionPrevious="emma_q";
            var consumeHat=When(Consume("hat"),"hat"); consumeHat.conditionPrevious="emma_w";
            var consumeRabbit=When(Consume("rabbit"),"rabbit"); consumeRabbit.conditionPrevious="emma_e";
            var clear=When(new SkillRule { op="clear_effect",key="hat" },"hat"); clear.conditionPrevious="emma_w";
            q=P("직전에 사용한 자신의 마술에 따라 비둘기·모자·토끼를 소비해 다른 효과를 얻습니다.", dove,hat,rabbit,consumeDove,consumeHat,consumeRabbit,clear);
            q.damage=7; q.evasion=20; q.duration=1; q.draw=0; p["emma_r"]=q;
        }
    }
}
