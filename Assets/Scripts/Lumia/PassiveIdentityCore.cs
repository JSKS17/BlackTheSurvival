using System.Collections.Generic;

namespace Lumia
{
    public static class TraitIdentityCore
    {
        static TraitRule R(string trigger,string op,int amount=0,string key=null,string label=null) => new TraitRule { trigger=trigger,op=op,amount=amount,key=key,label=label };
        static TraitRule Limit(TraitRule r,int perTurn=0,int cooldown=0,int battle=0) { r.maxPerTurn=perTurn; r.cooldown=cooldown; r.maxPerBattle=battle; return r; }
        static TraitRule When(TraitRule r,string key,int count=1) { r.conditionKey=key; r.conditionAmount=count; return r; }
        static TraitMechanicProfile P(string summary,params TraitRule[] rules) => new TraitMechanicProfile { summary=summary,rules=rules };
        public static void Populate(Dictionary<string,TraitMechanicProfile> p)
        {
            var stored=R("hit","damage_resource",15,"ko","K.O. 피해 축적"); stored.cap=12;
            var execute=Limit(R("hit","execute",0,"ko","K.O."),battle:1); execute.conditionKey="ko";
            p["nia_p"]=P("기본 공격과 모든 실험체의 기술 피해를 축적해 체력이 낮은 적을 마무리합니다.",stored,execute);

            var bleed=Limit(R("hit","bleed",1,"blood","피의 축제 출혈"),perTurn:2); bleed.duration=2;
            var blood=R("hit","gain",1,"blood_marks","출혈 표식"); blood.cap=3;
            var heal=Limit(When(R("hit","heal",3),"blood_marks",3),perTurn:1); heal.hpBelowPercent=80;
            var burst=Limit(When(R("before_attack","bonus_damage",3),"blood_marks",3),perTurn:1);
            p["jackie_p"]=P("모든 공격으로 출혈을 쌓고 최대 중첩에서는 추가 피해와 잃은 체력 회복을 얻습니다.",bleed,blood,burst,heal);

            var shield=Limit(R("before_incoming","block",5),cooldown:2);
            p["aya_p"]=P("피격 직전에 보호막이 생성되어 해당 공격부터 막습니다.",shield);

            var dogStacks=R("hit","gain",1,"dogfight","도그파이트");dogStacks.cap=3;
            var dog=When(R("before_attack","bonus_damage",4),"dogfight",3);
            var dogHeal=When(R("hit","heal",3),"dogfight",3);
            var dogUse=When(R("hit","consume",0,"dogfight"),"dogfight",3);
            p["hyunwoo_p"]=P("공격 적중으로 도그파이트를 쌓으며 최대 중첩의 다음 공격은 추가 피해를 주고 적중하면 체력을 회복합니다.",dogStacks,dog,dogHeal,dogUse);

            var cuff=R("battle_start","set",3,"cuff","단추"); cuff.cap=3;
            var cuffDamage=When(R("before_attack","bonus_damage",3),"cuff");
            var cuffUse=R("hit","consume",1,"cuff");
            p["yuki_p"]=P("전투마다 단추 3개를 준비하며 어느 실험체의 공격도 단추로 강화할 수 있습니다.",cuff,cuffDamage,cuffUse);

            var calamity=R("skill_hit","delayed_damage",4,"calamity","삼재"); calamity.every=3; calamity.duration=1;
            var fear=R("skill_hit","weak",1); fear.every=3; fear.cooldown=1;
            p["hyejin_p"]=P("어느 실험체의 기술이든 세 번 적중하면 삼재가 발동해 추가 피해와 공포에 따른 약화를 줍니다.",calamity,fear);

            var book=R("after_skill","set",1,"book","마음의 양식"); book.cap=1;
            var extra=When(R("before_basic","bonus_damage",4),"book");
            var nourish=When(R("after_basic","heal",2),"book"); nourish.onHit=true;
            var useBook=R("after_basic","consume",0,"book");
            p["sua_p"]=P("모든 기술을 사용한 뒤 다음 기본 공격이 강화되며 적중하면 체력을 회복합니다.",book,extra,nourish,useBook);

            var guerilla=Limit(R("install_hit","damage_buff",1),perTurn:1); guerilla.duration=2;
            p["isol_p"]=P("어느 실험체의 설치물이나 소환물로든 적을 맞히면 잠시 공격이 강화됩니다.",guerilla);

            var wild=R("battle_win","gain",1,"wild","야성"); wild.cap=3; wild.persistent=true;
            var wildShot=Limit(R("before_attack","bonus_damage",1),perTurn:2); wildShot.scaleKey="wild"; wildShot.cap=3;
            p["nadine_p"]=P("사냥에 성공할 때마다 야성이 최대 3까지 영구적으로 쌓이며 이후 전투의 모든 공격을 강화합니다.",wild,wildShot);

            var cheer=Limit(R("before_basic","bonus_damage",3),cooldown:2);
            var cheerShield=Limit(R("after_basic","block",4),cooldown:2); cheerShield.onHit=true;
            p["emma_p"]=P("일정 턴마다 기본 공격이 강화되며 적중하면 보호막을 얻습니다.",cheer,cheerShield);
        }
    }
}
