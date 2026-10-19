using System;
using System.Linq;

namespace Lumia
{
    public sealed partial class GameEngine
    {
        void ResolveTacticalCard(bool enemy,CardDef card,bool landed,int damage,SkillActorState before)
        {
            var c=State.combat;var actor=enemy?c.enemySkills:c.playerSkills;var statuses=enemy?c.enemyStatuses:c.playerStatuses;
            if(TacticalSkills.IsTactical(card))
            {
                switch(CardInstances.BaseId(card.id))
                {
                    case "tactical_nullification": case "tactical_lightning_shield":
                        statuses.effects.Clear();
                        (enemy?c.playerSkills:c.enemySkills).effects.RemoveAll(x=>x.kind=="bleed" || x.kind=="burn");
                        (enemy?c.playerTraits:c.enemyTraits).skills.effects.RemoveAll(x=>x.kind=="bleed" || x.kind=="burn");
                        (enemy?c.enemyTraits:c.playerTraits).buffs.RemoveAll(x=>x.kind=="heal_reduction" || x.kind=="exposure");
                        if(enemy){c.enemyPoison=0;c.enemyWeak=0;c.enemyVulnerable=0;}else{c.poison=0;c.weak=0;c.vulnerable=0;}break;
                    case "tactical_lightwing": statuses.effects.RemoveAll(x=>x.kind=="slow");break;
                    case "tactical_artifact": if(enemy)c.enemyAvailableEnergy=0;else c.energy=0;break;
                    case "tactical_false_oath":
                        int hp=enemy?c.enemyHp:State.hp,maxHp=enemy?c.enemyMaxHp:State.maxHp;
                        int paid=Math.Min(Math.Max(0,hp-1),Math.Min(20,Math.Max(1,maxHp/10)));
                        if(enemy)c.enemyHp-=paid;else State.hp-=paid;
                        if(paid>0)actor.effects.Add(new SkillTimedEffect {owner="전술",key="서약 회복",label="서약 회복",kind="hot",sourceCard=card.id,amount=card.tacticalEnhanced?paid:(paid+1)/2,remaining=card.tacticalEnhanced?1:2});break;
                    case "tactical_truth_burst":
                        if(landed){if(enemy)c.enemyAvailableEnergy+=card.tacticalEnhanced?2:1;else c.energy+=card.tacticalEnhanced?2:1;}
                        else if(enemy)c.enemyHp=Math.Max(1,c.enemyHp-5);else State.hp=Math.Max(1,State.hp-5);break;
                }
                int healing=TacticalSkills.Healing(card,enemy?c.enemyMaxHp:State.maxHp);
                if(healing>0){if(enemy)HealEnemy(healing);else Heal(healing);}
            }
            var strider=before.effects.FirstOrDefault(x=>x.kind=="tactical_buff"&&x.key=="strider"&&x.remaining>0);
            if(landed && card.damage>0 && strider!=null && strider.tacticalEnhanced)
                StatusMechanics.Add(enemy?c.playerStatuses:c.enemyStatuses,"slow",strider.sourceCard,1);
            int leech=TacticalSkills.LifeSteal(before,damage);
            if(leech>0){if(enemy)HealEnemy(leech);else Heal(leech);}
        }
        void StartTacticalTurn(bool enemy)
        {
            var c=State.combat;var actor=enemy?c.enemySkills:c.playerSkills;
            var opening=TacticalSkills.BeginTurn(actor);int reward=opening.reward;
            if(reward>0)
            {
                if(enemy)c.enemyAvailableEnergy+=reward;else{c.energy+=reward;DrawCards(reward);}
            }
            foreach(var pulse in opening.pulses)
            {
                ResolveSkillPulse(pulse,enemy);
                if(pulse.sourceCard=="tactical_impulse")
                {if(enemy){c.enemyEvasion=Math.Max(c.enemyEvasion,35);c.enemyEvasionTurns=Math.Max(c.enemyEvasionTurns,1);}else{c.evasion=Math.Max(c.evasion,35);c.evasionTurns=Math.Max(c.evasionTurns,1);}}
                if(CheckBattleEnd())break;
            }
        }
        int PreviewTacticalTurnStart(SkillActorState actor,SkillActorState defender,StatusActorState defenderStatuses,ref int defenderHp,ref int defenderBlock,out int rawDamage)
        {
            var opening=TacticalSkills.BeginTurn(actor);rawDamage=0;
            foreach(var pulse in opening.pulses)
            {
                rawDamage+=pulse.amount;
                int absorbed=pulse.kind=="tactical_true"?0:Math.Min(defenderBlock,pulse.amount);defenderBlock-=absorbed;
                StatusMechanics.ConsumeProtection(defenderStatuses,absorbed);
                int shared=CompanionMechanics.Absorb(defender,TacticalSkills.IncomingDamage(defender,pulse.amount-absorbed)).remaining;
                int delayed=(int)((long)shared*Math.Min(20,EquipmentSum(g=>g.damageDeferral))/100);
                int damage=CompanionMechanics.ClampOwnerDamage(defender,defenderHp,shared-delayed);defenderHp-=damage;
                StatusMechanics.DamageTaken(defenderStatuses,damage);StatusMechanics.Activate(defenderStatuses,pulse.sourceCard,pulse.key);
            }
            return opening.reward;
        }
        // Forecast copies use exactly the same immediate tactical outcomes without touching live state.
        void PreviewTacticalCard(CardDef card,SkillActorState actor,StatusActorState statuses,bool landed,ref int hp,int maxHp,ref int weak,ref int energy,SkillActorState before,int damage,SkillActorState opposingActor,TraitActorState traits,TraitActorState opposingTraits,StatusActorState opposingStatuses)
        {
            switch(CardInstances.BaseId(card.id))
            {
                case "tactical_nullification": case "tactical_lightning_shield":
                    statuses.effects.Clear();opposingActor.effects.RemoveAll(x=>x.kind=="bleed" || x.kind=="burn");opposingTraits.skills.effects.RemoveAll(x=>x.kind=="bleed" || x.kind=="burn");
                    traits.buffs.RemoveAll(x=>x.kind=="heal_reduction" || x.kind=="exposure");weak=0;break;
                case "tactical_lightwing":statuses.effects.RemoveAll(x=>x.kind=="slow");break;
                case "tactical_false_oath": hp-=Math.Min(Math.Max(0,hp-1),Math.Min(20,Math.Max(1,maxHp/10)));break;
                case "tactical_truth_burst": if(landed)energy+=card.tacticalEnhanced?2:1;else hp=Math.Max(1,hp-5);break;
                case "tactical_artifact":energy=0;break;
            }
            var strider=before.effects.FirstOrDefault(x=>x.kind=="tactical_buff"&&x.key=="strider"&&x.remaining>0);
            if(landed && card.damage>0 && strider!=null && strider.tacticalEnhanced)StatusMechanics.Add(opposingStatuses,"slow",strider.sourceCard,1);
            int healing=(TacticalSkills.Healing(card,maxHp)+TacticalSkills.LifeSteal(before,damage))*(100-Math.Max(TraitMechanics.HealingReduction(traits),StatusMechanics.HealingReduction(statuses)))/100;
            hp=Math.Min(maxHp,hp+healing);
        }
    }
}
