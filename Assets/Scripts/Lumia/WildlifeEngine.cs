using System;
using System.Linq;

namespace Lumia
{
    public sealed partial class GameEngine
    {
        public CardDef EnemyActionCard(string id) { return GameDatabase.Card(id) ?? WildlifeMechanics.Card(id) ?? WickelineMechanics.Card(id); }
        public bool IsMutantWildlife(MapNode node)
        {
            if(node==null || node.kind!=ZoneKind.Wildlife || node.act<2)return false;
            if(node.wildlifeVariantVersion>0)return node.mutantAnimal;
            // A separate stream fixes this node before scouting, entering or saving, without
            // advancing the run RNG or changing any existing draft/reward/encounter rolls.
            int variantRandom=unchecked(State.seed ^ (node.id+1)*(int)0x9E3779B9 ^ node.act*16777619 ^ (int)0xA11A1102);
            if(variantRandom==0)variantRandom=1831565813;
            return Next(ref variantRandom,100)<WildlifeMechanics.MutationChance(node.act);
        }
        void AssignWildlifeVariant(MapNode node)
        {
            if(node==null || node.kind!=ZoneKind.Wildlife || node.wildlifeVariantVersion>0)return;
            node.mutantAnimal=IsMutantWildlife(node);node.wildlifeVariantVersion=1;
        }
        void EnsureWildlifeAssignments()
        {
            // Never turn an already spawned ordinary animal into a mutant after an update.
            var active=ActiveNode();
            if(active!=null && State.combat!=null && !string.IsNullOrEmpty(State.combat.animal))
            { active.mutantAnimal=State.combat.mutantAnimal;active.wildlifeVariantVersion=1; }
            foreach(var node in State.map.Concat(State.archivedMapNodes))AssignWildlifeVariant(node);
        }
        public string EnemyWildlifeDescription
        {
            get
            {
                var c=State.combat;if(c==null || string.IsNullOrEmpty(c.animal))return "";
                string text=c.mutantAnimal ? "변이 동물 · 체력 +20% · 공격 피해 +15%"+(WildlifeMechanics.ObjectDropChance(c.animal,false)>0 ? " · 오브젝트 확률 +5%p" : "") : "일반 야생동물";
                string skill=c.animal=="boar" ? WildlifeMechanics.BoarCharge : c.animal=="bear" ? WildlifeMechanics.BearSlam : null;
                if(skill!=null)
                {
                    text+="\n"+WildlifeMechanics.Description(skill);
                    if(c.enemyPlan.Skip(c.enemyTurn?c.enemyActionIndex:0).Contains(skill))
                        text+="\n이번 행동: "+(WildlifeMechanics.Interrupted(skill,c.enemyStatuses) ? "제어로 시전 중단 예정" : WildlifeMechanics.Card(skill).name+" 예고");
                }
                int chance=WildlifeMechanics.ObjectDropChance(c.animal,c.mutantAnimal);
                return text+"\n오브젝트 드롭 확률 "+chance+"% · 기본 공격 카드 "+WildlifeBasicAttackDropChance+"% · 고기 "+WildlifeMeatDropChance+"%";
            }
        }
        void PlanWildlifeTurn()
        {
            var c=State.combat;
            string skill=c.animal=="boar" && c.turn%2==0 ? WildlifeMechanics.BoarCharge
                : c.animal=="bear" && c.turn%3==0 ? WildlifeMechanics.BearSlam : null;
            if(skill!=null)
            {c.enemyPlan.Add(skill);c.enemyPlanCosts.Add(0);c.enemyPlanFreeCast.Add(false);return;}
            c.enemyPlan.Add("basic_attack");c.enemyPlanCosts.Add(CardCost("basic_attack"));c.enemyPlanFreeCast.Add(false);
            if(c.animal=="wolf" || c.animal=="dog" && c.enemyLevel>=8)
            {c.enemyPlan.Add("basic_attack");c.enemyPlanCosts.Add(CardCost("basic_attack"));c.enemyPlanFreeCast.Add(false);}
        }
        void ApplyBoarDespair(string source)
        {
            var c=State.combat;
            c.enemyVulnerable=Math.Max(c.enemyVulnerable,2);
            StatusMechanics.Add(c.enemyStatuses,"slow",source,2);
            EmitCombatAction(new CombatAction {id=++actionSerial,cardId=source,kind="wildlife_despair",enemy=true});
            Say(c.enemyName+" · 좌절 · 다음 내 턴까지 받는 공격 피해 +50%");
        }
        bool ResolveWildlifeSkill(string id)
        {
            var card=WildlifeMechanics.Card(id);var c=State.combat;
            if(card==null)return false;
            if(!WildlifeMechanics.BelongsTo(id,c.animal))return true;
            if(WildlifeMechanics.Interrupted(id,c.enemyStatuses))
            {
                EmitCombatAction(new CombatAction {id=++actionSerial,cardId=id,kind="wildlife_interrupt",enemy=true});
                Say(c.enemyName+" · "+card.name+" 시전 중단");
                if(id==WildlifeMechanics.BoarCharge)ApplyBoarDespair(id);
                return true;
            }
            RecordCombatCardStart(card,true,0,false);
            int raw=EnemyCardDamage(card),dealt=0,absorbed=0,defenderBlock=c.block;
            bool accuracyMiss;
            bool landed=TryDirectHit(card,true,out accuracyMiss);
            if(landed)
            {
                var incoming=TraitEvent(false,"before_incoming",card,true);
                int damage=Math.Max(0,RunModes.ScaleEnemyDirectDamage(State,raw)-incoming.damageReduction);
                absorbed=AbsorbCombatBlock(false,damage);
                dealt=TakeHealthDamage(false,damage-absorbed);
                StatusMechanics.DamageTaken(c.playerStatuses,dealt);
                ApplyCardStatuses(true,c.enemySkills,card,true);
            }
            int avoided=!landed && !accuracyMiss ? 1 : 0;
            EmitCombatAction(new CombatAction {id=++actionSerial,cardId=id,kind="wildlife_skill",enemy=true,damage=dealt,absorbed=absorbed,avoided=avoided,missed=accuracyMiss?1:0});
            AfterCardTraits(true,card,landed,dealt,0,0,avoided,true);
            if(defenderBlock>0 && c.block==0)TraitEvent(false,"block_break",card,landed,dealt);
            Say(c.enemyName+" · "+card.name+(landed ? " · 피해 "+dealt : accuracyMiss ? " · 공격이 빗나갔습니다." : " · 하나가 회피했습니다."));
            if(!landed && id==WildlifeMechanics.BoarCharge)ApplyBoarDespair(id);
            if(landed)ResolveCounters(false);
            CheckBattleEnd();return true;
        }
    }
}
