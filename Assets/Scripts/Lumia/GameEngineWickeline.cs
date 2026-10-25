using System;
using System.Linq;

namespace Lumia
{
    public sealed partial class GameEngine
    {
        public bool IsWickelineCombat => State.stage==RunStage.Combat && State.combat?.wickeline!=null && State.combat.enemyId==WickelineMechanics.EnemyId;
        public bool IsWickelineNode(MapNode node) { return node!=null && node.act==3 && node.kind==ZoneKind.Campfire && node.wickeline; }
        public string EnemyWickelineStatus
        {
            get
            {
                if(!IsWickelineCombat)return "";
                var c=State.combat;var w=c.wickeline;
                return (WickelineMechanics.Collapsed(c) ? "구조 붕괴 · 받는 체력 피해 +30%" : WickelineMechanics.Isolating(c) ? "격리 충전 · 받는 체력 피해 -40%" : "실험 대상 추적")
                    + " · 구조 "+Math.Min(WickelineMechanics.StructureThreshold,w.structureDamage)+"/"+WickelineMechanics.StructureThreshold
                    + (WickelineMechanics.GasActive(c) ? "\n신경가스 · 하나 회복 -40% · "+Math.Max(0,w.gasUntilTurn-c.turn+1)+"턴" : "");
            }
        }
        public string EnemyWickelineDescription => (IsWickelineCombat ? EnemyWickelineStatus+"\n\n" : "")+WickelineMechanics.Guide;

        void EnsureWickelineAssignments()
        {
            var camps=State.map.Where(n=>n.act==3 && n.kind==ZoneKind.Campfire).OrderBy(n=>n.id).ToList();
            if(camps.Count==0 || camps.All(n=>n.wickelineVersion>0))return;
            var selected=camps.FirstOrDefault(n=>n.wickeline);
            if(selected==null)
            {
                int random=unchecked(State.seed ^ (State.endlessCycle+1)*16777619 ^ camps[0].id*(int)0x9E3779B9 ^ (int)0x71C1E11E);
                if(random==0)random=1831565813;
                selected=camps[Next(ref random,camps.Count)];
            }
            foreach(var node in camps)
            {
                node.wickeline=node==selected;node.wickelineVersion=1;
                // A pre-update camp already being used remains a camp; no surprise fight or retroactive loot.
                bool currentCamp=node.id==State.activeNodeId && (State.stage==RunStage.Campfire
                    || State.stage==RunStage.Kiosk && State.portableKioskActive && State.portableKioskReturnStage==RunStage.Campfire);
                if(node.wickeline && (node.visited || currentCamp))node.wickelineDefeated=true;
            }
        }

        void NormalizeWickeline()
        {
            if(State.combat?.enemyId!=WickelineMechanics.EnemyId)return;
            var c=State.combat;
            if(c.wickeline==null)c.wickeline=new WickelineBattleState();
            c.wickeline.structureDamage=Math.Max(0,Math.Min(WickelineMechanics.StructureThreshold-1,c.wickeline.structureDamage));
            c.enemyName=WickelineMechanics.Name;c.boss=null;c.animal=null;c.lumi=false;
            c.enemyPlanCosts=c.enemyPlan.Select(id=>0).ToList();c.enemyPlanFreeCast=c.enemyPlan.Select(id=>false).ToList();
            var node=ActiveNode();
            if(node!=null && node.act==3 && node.kind==ZoneKind.Campfire){node.wickeline=true;node.wickelineVersion=1;}
        }

        void ConfigureWickeline(CombatState c)
        {
            c.enemyId=WickelineMechanics.EnemyId;c.enemyName=WickelineMechanics.Name;c.enemyMaxHp=210+c.enemyLevel*6;
            c.wickeline=new WickelineBattleState();
            c.enemyTraits=TraitMechanics.Ensure(c.enemyTraits);
            // These IDs are saved only inside this NPC combat. No subject/reward pool is extended.
            c.enemyDeck.AddRange(new[]{"basic_attack",WickelineMechanics.Gas,WickelineMechanics.Triplet,WickelineMechanics.Control,WickelineMechanics.Isolation,WickelineMechanics.Release});
            RecordCombatInformation(new CombatLogEntry {kind="wickeline_tracking",cardId=WickelineMechanics.Tracking,enemy=true,title="위클라인 · 실험 대상 추적",detail="하나를 추적합니다. 위클라인 처치 후 이 구역의 모닥불을 이용할 수 있습니다."});
        }

        void PlanWickelineTurn()
        {
            var c=State.combat;
            switch((c.turn-1)%5)
            {
                case 0:c.enemyPlan.AddRange(new[]{WickelineMechanics.Gas,"basic_attack"});break;
                case 1:c.enemyPlan.Add(WickelineMechanics.Triplet);if(WickelineMechanics.GasActive(c))c.enemyPlan.Add("basic_attack");break;
                case 2:c.enemyPlan.AddRange(new[]{WickelineMechanics.Control,"basic_attack","basic_attack"});break;
                case 3:c.enemyPlan.Add(WickelineMechanics.Isolation);break;
                default:c.enemyPlan.Add(c.wickeline.isolationPending ? WickelineMechanics.Release : "basic_attack");break;
            }
            foreach(var id in c.enemyPlan){c.enemyPlanCosts.Add(0);c.enemyPlanFreeCast.Add(false);}
            c.enemyEnergy=c.enemyAvailableEnergy=0;
        }

        bool WickelineInterrupted(CardDef card)
        {
            var c=State.combat;
            return c.wickeline.skipEnemyTurn==c.turn || !TacticalSkills.CanAct(c.enemySkills)
                || StatusMechanics.EnergyPenalty(c.enemyStatuses)>0 || !StatusMechanics.CanUse(c.enemyStatuses,card)
                || card.category=="wickeline" && new[]{"silence","polymorph","taunt","berserk","dance"}.Any(k=>StatusMechanics.Has(c.enemyStatuses,k));
        }

        void RefreshWickelineIntent()
        {
            var c=State.combat;c.intentDamage=c.intentBlock=0;
            var defenderStatuses=StatusMechanics.Clone(c.playerStatuses);
            var defenderTraits=TraitMechanics.Clone(c.playerTraits);
            if(!c.enemyTurn){StatusMechanics.EndTurn(defenderStatuses);TraitMechanics.EndTurn(defenderTraits);}
            bool controlCancelled=false;
            foreach(var id in c.enemyPlan.Skip(c.enemyTurn?c.enemyActionIndex:0))
            {
                var card=EnemyActionCard(id);if(card==null)continue;
                if(id==WickelineMechanics.Control && WickelineInterrupted(card))controlCancelled=true;
                if(controlCancelled || WickelineInterrupted(card) || id==WickelineMechanics.Release && !c.wickeline.isolationPending)continue;
                // Use the same direct-damage scaling as resolution; previews never roll hit/evasion or mutate phases.
                int vulnerable=c.enemyTurn?c.vulnerable:Math.Max(0,c.vulnerable-1);
                c.intentDamage+=EnemyDirectHitTotal(CalculateEnemyDamage(card,c.enemyStrength,vulnerable,TraitMechanics.Exposure(defenderTraits),defenderStatuses:defenderStatuses),0,Math.Max(1,card.hits));
            }
        }

        bool ResolveWickelineAction(string id)
        {
            if(!IsWickelineCombat)return false;
            var c=State.combat;var w=c.wickeline;var card=EnemyActionCard(id);
            if(card==null)return true;
            if(WickelineInterrupted(card))
            {
                if(id==WickelineMechanics.Control)c.enemyActionIndex=c.enemyPlan.Count;
                if(id==WickelineMechanics.Release){w.isolationPending=false;w.isolationUntilTurn=0;}
                RecordCombatInformation(new CombatLogEntry {kind="wickeline_interrupt",enemy=true,cardId=id,title="위클라인 · 행동 중단",detail=card.name+" · 구조 붕괴 또는 제어 효과로 중단"});
                Say("위클라인 · "+card.name+" 행동 중단");return true;
            }
            RecordCombatCardStart(card,true,0,false);
            int dealt=0,absorbed=0,avoided=0,missed=0,healed=0;bool landed=false;int defenderBlock=c.block;
            if(id==WickelineMechanics.Gas)w.gasUntilTurn=c.turn+2;
            else if(id==WickelineMechanics.Isolation)
            {
                int before=c.enemyHp;HealEnemy(20+(c.enemyMaxHp-c.enemyHp)/10);healed=c.enemyHp-before;
                w.isolationPending=true;w.isolationUntilTurn=c.turn+1;
            }
            else if(id==WickelineMechanics.Release)
            {
                if(!w.isolationPending)return true;
                w.isolationPending=false;w.isolationUntilTurn=0;
            }
            if(card.damage>0)
            {
                int hits=Math.Max(1,card.hits),perHit=EnemyCardDamage(card);
                var incoming=TraitEvent(false,"before_incoming",card,true);bool reductionUsed=false;
                for(int hit=0;hit<hits && State.hp>0;hit++)
                {
                    bool accuracyMiss;
                    if(!TryDirectHit(card,true,out accuracyMiss)){if(accuracyMiss)missed++;else avoided++;continue;}
                    landed=true;
                    int damage=Math.Max(0,RunModes.ScaleEnemyDirectDamage(State,perHit)-(reductionUsed?0:incoming.damageReduction));reductionUsed=true;
                    int blocked=AbsorbCombatBlock(false,damage);absorbed+=blocked;
                    int health=TakeHealthDamage(false,damage-blocked);dealt+=health;
                    StatusMechanics.DamageTaken(c.playerStatuses,health);
                }
                if(landed && (id==WickelineMechanics.Triplet || id==WickelineMechanics.Release))
                {
                    var previous=StatusMechanics.Snapshot(c.playerStatuses);
                    StatusMechanics.Add(c.playerStatuses,id==WickelineMechanics.Triplet?"slow":"knockback",id,1);
                    RecordStatusChanges(true,c.playerStatuses,previous);
                }
            }
            EmitCombatAction(new CombatAction {id=++actionSerial,cardId=id,enemy=true,damage=dealt,absorbed=absorbed,heal=healed,avoided=avoided,missed=missed});
            AfterCardTraits(true,card,landed,dealt,0,healed,avoided,true);
            if(defenderBlock>0 && c.block==0)TraitEvent(false,"block_break",card,landed,dealt);
            Say("위클라인 · "+card.name+(dealt>0?" · 피해 "+dealt:healed>0?" · 회복 "+healed:""));
            if(landed)ResolveCounters(false);
            CheckBattleEnd();return true;
        }

        void ObserveWickelineDamage(int damage)
        {
            if(!IsWickelineCombat || damage<=0)return;
            var c=State.combat;var w=c.wickeline;
            if(c.enemyHp<=0 || WickelineMechanics.Collapsed(c))return;
            w.structureDamage=(int)Math.Min(int.MaxValue,(long)w.structureDamage+damage);
            if(w.structureDamage<WickelineMechanics.StructureThreshold)return;
            w.structureDamage=0;w.collapsedUntilTurn=c.turn+1;w.skipEnemyTurn=c.turn;w.isolationPending=false;w.isolationUntilTurn=0;
            int collapse=Math.Min(c.enemyHp,Math.Max(1,c.enemyMaxHp/10));c.enemyHp-=collapse;
            EmitCombatAction(new CombatAction {id=++actionSerial,cardId=WickelineMechanics.Collapse,kind="wickeline_collapse",enemy=true,damage=collapse});
            Say("위클라인 · 구조 붕괴 · 체력 -"+collapse+" · 다음 상대 행동 중단 · 받는 체력 피해 +30%");
        }

        void WinWickelineBattle()
        {
            var node=ActiveNode();
            bool firstVictory=node!=null && !node.wickelineDefeated;
            RecordCombatInformation(new CombatLogEntry {kind="battle_end",title="위클라인 제압",detail="같은 구역의 모닥불로 이동합니다."});
            if(firstVictory)
            {
                node.wickelineDefeated=true;
                State.credits=(int)Math.Min(int.MaxValue,(long)State.credits+WickelineMechanics.CreditReward);
                State.objects.Add("blood");string material=Next(2)==0?"tree":"meteorite";State.objects.Add(material);
                TraitEvent(false,"battle_win",null,true);
                Say("위클라인 처치 · VF 혈액샘플 1개 · 크레딧 +100 · "+GameDatabase.Object(material).name+" 1개 · 같은 구역의 모닥불을 이용합니다.");
            }
            State.combat=null;State.rewards=null;State.pendingPassive=null;State.stage=RunStage.Campfire;
            State.campChoice=0;State.campActions=3;State.campRemovalsRemaining=0;
            if(firstVictory)Heal(Modifier("kill_heal")+AugmentAmount("victory_heal"));
        }
    }
}
