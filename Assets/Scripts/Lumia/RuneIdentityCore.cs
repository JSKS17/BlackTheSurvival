using System.Collections.Generic;

namespace Lumia
{
    public static class RuneIdentityCore
    {
        static TraitRule R(string trigger,string op,int amount=0,string key=null,string label=null) => new TraitRule { trigger=trigger,op=op,amount=amount,key=key,label=label };
        static TraitRule Limit(TraitRule r,int perTurn=0,int cooldown=0,int battle=0) { r.maxPerTurn=perTurn;r.cooldown=cooldown;r.maxPerBattle=battle;return r; }
        static TraitRule When(TraitRule r,string key,int count=1) { r.conditionKey=key;r.conditionAmount=count;return r; }
        static TraitMechanicProfile P(string summary,params TraitRule[] rules) => new TraitMechanicProfile { summary=summary,rules=rules };
        public static void Populate(Dictionary<string,TraitMechanicProfile> p)
        {
            var vampire=Limit(R("skill_hit","gain",1,"vampire","흡혈 중첩"),perTurn:2); vampire.cap=4;
            var leech=Limit(When(R("hit","heal",1),"vampire"),perTurn:2);
            var frenzy=Limit(When(R("before_attack","bonus_damage",1),"vampire",4),perTurn:2);
            p["vampire"]=P("기술 적중으로 흡혈을 쌓고 공격으로 체력을 회복하며 최대 중첩에서는 공격이 조금 강화됩니다.",vampire,leech,frenzy);

            var frailty=R("hit","delayed_damage",3,"frailty","취약 집중 타격"); frailty.every=3; frailty.duration=1; frailty.cooldown=2;
            var exposed=R("hit","exposure",10); exposed.duration=1; exposed.every=3; exposed.cooldown=2;
            p["frailty"]=P("세 번의 공격 적중마다 추가 피해를 주고 적의 방어를 무너뜨립니다.",frailty,exposed);

            var bolt=Limit(R("skill_hit","delayed_damage",3,"lightning","벽력"),cooldown:2); bolt.duration=1;
            p["lightning"]=P("기술을 적중시키면 벽력이 턴 종료에 추가 피해를 줍니다.",bolt);

            var vortex=R("after_skill","heal",4); vortex.every=3; vortex.cooldown=2;
            p["vortex"]=P("기술 사용을 이어 가면 와류가 발동하여 전투 중 체력을 회복합니다.",vortex);

            var diamond=Limit(R("control","block",4),cooldown:2);
            var wave=Limit(R("control","delayed_damage",2,"diamond","금강 파동"),cooldown:2); wave.duration=1;
            p["diamond"]=P("기절·속박·침묵 등의 군중 제어를 부여하면 방어를 얻고 턴 종료에 파동 피해를 줍니다.",diamond,wave);

            var light=Limit(R("battle_start","block",5),battle:1);
            var cleanse=Limit(R("block_break","cleanse"),battle:1);
            p["guardian"]=P("전투 시작에 빛의 보호막을 얻으며 방어도가 처음 깨지면 해로운 상태를 해제합니다.",light,cleanse);

            var healing=Limit(R("low_health","hot",2,"healing_drone","치유 드론"),cooldown:3); healing.duration=2;healing.hpBelowNumerator=1;healing.hpBelowDenominator=3;
            p["healing_drone"]=P("피해를 받아 체력이 최대 체력의 1/3 이하가 되면 치유 드론이 2턴 동안 회복을 돕습니다.",healing);

            var energy=Limit(R("after_ultimate","energy_buff",1),cooldown:3); energy.duration=2;
            var amp=Limit(R("after_ultimate","damage_buff",1),cooldown:3); amp.duration=2;
            p["amplification_drone"]=P("R 사용 후 이번 턴과 다음 턴에 최대 코스트와 첫 적중 피해가 각각 1 증가합니다.",energy,amp);

            var hunt=R("battle_win","gain",1,"hunting","사냥 축적"); hunt.cap=3; hunt.persistent=true;
            var mask=Limit(When(R("hit","heal",1),"hunting"),perTurn:1);
            p["dog_mask"]=P("승리할 때마다 사냥을 축적하며 이후 전투의 공격으로 소량의 체력을 회복합니다.",hunt,mask);

            var scar=Limit(R("hit","gain",1,"scar","상흔"),perTurn:2); scar.cap=2;
            var scarReduction=Limit(R("hit","heal_reduction",20),perTurn:1); scarReduction.duration=2;
            var scarDamage=Limit(When(R("before_attack","bonus_damage",2),"scar",2),cooldown:2);
            p["scar"]=P("적중으로 상흔을 남겨 적의 치유를 줄이며 최대 중첩에서는 주기적으로 추가 피해를 줍니다.",scar,scarReduction,scarDamage);

            var prepare=R("after_skill","set",1,"quickfire","속사 준비"); prepare.cap=1;
            var quick=Limit(When(R("after_basic","damage_buff",1),"quickfire"),cooldown:2); quick.duration=1;quick.onHit=true;
            var quickUse=R("after_basic","consume",0,"quickfire");
            p["quickfire"]=P("기술 사용 후 기본 공격을 적중시키면 이번 턴의 후속 공격이 강화됩니다.",prepare,quick,quickUse);

            var cycle=Limit(R("after_skill","hot",1,"cycle","VF 순환"),perTurn:1); cycle.duration=2;
            p["circular"]=P("기술 사용에 반응해 VF를 순환시키고 체력을 천천히 회복합니다.",cycle);

            var vigilance=Limit(R("before_incoming","block",2),cooldown:3); vigilance.hpBelowPercent=80;
            p["vigilance"]=P("체력이 80% 이하일 때 공격받으면 작은 방어막으로 피해를 줄입니다.",vigilance);

            var temper=R("turn_start","gain",1,"tempering","담금질"); temper.every=3;temper.cap=2;temper.persistent=true;
            var armor=R("turn_start","block",1);armor.scaleKey="tempering";armor.cap=2;
            p["tempering"]=P("시간이 흐를수록 담금질이 최대 2까지 쌓이며 매 턴 작은 방어도를 얻습니다.",temper,armor);

            p["coupon"]=P("키오스크에서 오브젝트와 음식의 구매 비용을 10% 줄입니다.",R("battle_start","shop_discount",10));
            var huntingDamage=Limit(R("wildlife_before_attack","bonus_damage",1),perTurn:2);
            var huntingHeal=Limit(R("wildlife_hit","heal",1),perTurn:1);
            p["hunting"]=P("야생동물을 공격할 때 피해를 조금 높이고 적중 시 소량의 체력을 회복합니다.",huntingDamage,huntingHeal);
        }
    }
}
