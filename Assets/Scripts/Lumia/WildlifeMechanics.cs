using System;

namespace Lumia
{
    /// <summary>Enemy-only actions, deliberately absent from the playable card catalog.</summary>
    public static class WildlifeMechanics
    {
        public const string BoarCharge = "wildlife_boar_charge";
        public const string BearSlam = "wildlife_bear_slam";
        public const int MutantHealthPercent = 120;
        public const int MutantAttackPercent = 115;
        public const int MutantObjectBonus = 5;
        static readonly CardDef charge = new CardDef {
            id=BoarCharge, name="돌진", owner="멧돼지", key="특수", category="wildlife", cost=0,
            damage=4, movement=true, effect="dash", effectColor="#D9A775",
            statuses=new[] {new CardStatusRule {key="knockback", duration=1, onHit=true}},
            description="2턴마다 기본 공격 대신 돌진합니다. 기본 공격보다 4 높은 피해를 입히고, 적중하면 1턴 동안 넉백을 부여합니다. 회피당하거나 강한 제어·속박으로 중단되면 다음 플레이어 턴까지 좌절 상태가 됩니다. 동물 전용 기술이므로 획득하거나 사용할 수 없습니다."
        };
        static readonly CardDef slam = new CardDef {
            id=BearSlam, name="지면 강타", owner="곰", key="특수", category="wildlife", cost=0,
            damage=5, effect="earth", effectColor="#D6B37D",
            statuses=new[] {new CardStatusRule {key="stun", duration=1, onHit=true}},
            description="3턴마다 기본 공격 대신 지면을 강타합니다. 기본 공격보다 5 높은 피해를 입히고, 적중하면 1턴 동안 기절을 부여합니다. 강한 제어로 시전을 중단할 수 있습니다. 동물 전용 기술이므로 획득하거나 사용할 수 없습니다."
        };

        public static CardDef Card(string id) { return id==BoarCharge ? charge : id==BearSlam ? slam : null; }
        public static bool IsAnimalSkill(string id) { return Card(id)!=null; }
        public static string Name(string animal)
        {
            switch(animal) {case "chicken":return "닭";case "dog":return "들개";case "boar":return "멧돼지";case "wolf":return "늑대";case "bear":return "곰";default:return "야생동물";}
        }
        public static string Description(string id)
        {
            var card=Card(id); if(card==null)return "";
            string text=card.description;
            if(id==BoarCharge)text+="\n좌절: 다음 플레이어 턴 동안 받는 공격 피해가 50% 증가하고 둔화됩니다.";
            foreach(var status in card.statuses)text+="\n"+StatusMechanics.Name(status.key)+": "+StatusMechanics.Explain(status.key);
            return text;
        }
        public static int MutationChance(int act) { return act>=3 ? 14 : act==2 ? 8 : 0; }
        public static int ObjectDropChance(string animal,bool mutant)
        {
            int normal=animal=="wolf" ? 25 : animal=="bear" ? 35 : 0;
            return normal>0 ? normal+(mutant?MutantObjectBonus:0) : 0;
        }
        public static int ScaleHealth(int health,bool mutant) { return mutant ? (health*MutantHealthPercent+99)/100 : health; }
        public static int ScaleAttack(int damage,bool mutant) { return mutant ? (damage*MutantAttackPercent+99)/100 : damage; }
        public static int BasicDamage(string animal,int level)
        {
            return (animal=="bear" ? 11 : animal=="boar" ? 8 : animal=="wolf" ? 7 : animal=="dog" ? 6 : 5)+level/2;
        }
        public static bool BelongsTo(string id,string animal) { return id==BoarCharge && animal=="boar" || id==BearSlam && animal=="bear"; }
        public static bool Interrupted(string id,StatusActorState statuses)
        {
            return StatusMechanics.EnergyPenalty(statuses)>0 || id==BoarCharge && StatusMechanics.Has(statuses,"root");
        }
    }
}
