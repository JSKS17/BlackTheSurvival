using System;
using System.Collections.Generic;
using System.Linq;

namespace Lumia
{
    [Serializable] public sealed class ChargedCard
    {
        public string reference;
        public int readyTurn;
    }
    [Serializable] public sealed class AdvancedEnemyAction
    {
        public string mode;
        public int charge, sacrificed, x;
        public List<string> sacrificedReferences = new List<string>();
        public bool sacrificeCommitted;
    }
    public sealed class AdvancedCardChoice
    {
        public string kind, reference;
        public List<string> handReferences = new List<string>(), drawReferences = new List<string>();
    }
    public sealed partial class GameEngine
    {
        AdvancedCardChoice pendingCardChoice;
        AdvancedEnemyAction playerAdvancedAction;
        public AdvancedCardChoice PendingCardChoice { get { return pendingCardChoice; } }

        void EnsureAdvancedCombat()
        {
            var c = State.combat; if (c == null) return;
            if (c.chargedCards == null) c.chargedCards = new List<ChargedCard>();
            if (c.enemyChargedCards == null) c.enemyChargedCards = new List<ChargedCard>();
            if (c.enemyAdvancedActions == null) c.enemyAdvancedActions = new List<AdvancedEnemyAction>();
            c.playerStatuses=StatusMechanics.Ensure(c.playerStatuses);c.enemyStatuses=StatusMechanics.Ensure(c.enemyStatuses);
            c.playerStatuses.controlProtection=Math.Max(0,Math.Min(c.block,c.playerStatuses.controlProtection));
            c.enemyStatuses.controlProtection=Math.Max(0,Math.Min(c.enemyBlock,c.enemyStatuses.controlProtection));
            c.chargedCards.RemoveAll(x => x == null || !AdvancedCardMechanics.CanCharge(GameDatabase.Card(x.reference)));
            c.enemyChargedCards.RemoveAll(x => x == null || !AdvancedCardMechanics.CanCharge(GameDatabase.Card(x.reference)));
        }
        public bool IsCharged(string reference)
        {
            var c = State.combat;
            return c != null && (c.chargedCards ?? new List<ChargedCard>()).Any(x => x.reference == reference && x.readyTurn <= c.turn);
        }
        public string ChargeLabel(string reference)
        {
            var c = State.combat;
            var held = c == null ? null : (c.chargedCards ?? new List<ChargedCard>()).FirstOrDefault(x => x.reference == reference);
            return held == null ? "" : held.readyTurn <= c.turn ? "충전 완료 · 무료 발동" : "충전 중 · 다음 내 턴";
        }
        CardDef PreparedPlayerCard(string reference)
        {
            var card = PlayerCard(reference); if (card == null) return null;
            var c = State.combat; var action = playerAdvancedAction;
            int charge = action == null ? IsCharged(reference) ? 1 : 0 : action.charge;
            int x = action == null ? EffectiveCardCost(reference) : action.x;
            return AdvancedCardMechanics.PreparedCard(card, action == null ? "" : action.mode, charge, action == null ? 0 : action.sacrificed, x);
        }
        public CardDef DisplayCombatCard(string reference) { return PreparedPlayerCard(reference); }
        SkillInstantBonus PlayerAdvancedBonus(CardDef card)
        {
            var c = State.combat; if (c == null) return new SkillInstantBonus();
            return AdvancedCardMechanics.ContextualBonus(card, State.hp, State.maxHp, c.block,
                playerAdvancedAction == null ? 0 : playerAdvancedAction.charge,
                playerAdvancedAction == null ? 0 : playerAdvancedAction.sacrificed,
                playerAdvancedAction == null ? 0 : playerAdvancedAction.x);
        }
        // Choice IDs are actual physical card references. Assigning an ordinary copy
        // an identity does not upgrade it or change its definition.
        void IdentifyChoiceCopies(List<string> pile, int limit = int.MaxValue)
        {
            for (int i = 0; i < Math.Min(pile.Count, limit); ++i)
                IdentifyChoiceCopy(pile,i);
        }
        void IdentifyChoiceCopy(List<string> pile,int index)
        {
            if(CardInstances.IsInstance(pile[index]))return;
            string old=pile[index],reference=CardInstances.CreateReference(State,old);
            pile[index]=reference;
            int owned=State.deck.IndexOf(old);if(owned>=0)State.deck[owned]=reference;
            foreach(var grant in State.combat.freeCasts.Where(x=>x.cardReference==old))grant.cardReference=reference;
        }
        public bool BeginCardChoice(int handIndex)
        {
            if (pendingCardChoice != null || !CanPlayCard(handIndex)) return false;
            var c = State.combat; var card = PlayerCard(c.hand[handIndex]);
            string kind = AdvancedCardMechanics.Kind(card);
            if (kind == "charge" && IsCharged(c.hand[handIndex])) kind = "";
            if (string.IsNullOrEmpty(kind) || kind == "x") return PlayCard(handIndex);
            IdentifyChoiceCopy(c.hand,handIndex);
            IdentifyChoiceCopies(c.hand);
            if (kind == "foresight") { c.drawPile.Reverse(); IdentifyChoiceCopies(c.drawPile, 3); c.drawPile.Reverse(); }
            pendingCardChoice = new AdvancedCardChoice { kind = kind, reference = c.hand[handIndex],
                handReferences = c.hand.Where((id, i) => i != handIndex && (kind!="foresight" || !c.chargedCards.Any(x=>x.reference==id))).ToList(), drawReferences = c.drawPile.AsEnumerable().Reverse().Take(3).ToList() };
            return true;
        }
        public bool CancelCardChoice()
        {
            if (pendingCardChoice == null) return false;
            pendingCardChoice = null; return true;
        }
        public bool ResolveCardChoice(string mode, List<string> selected = null, string drawReference = null)
        {
            var choice = pendingCardChoice; var c = State.combat;
            if (choice == null || c == null || c.enemyTurn || State.stage != RunStage.Combat) return false;
            int index = c.hand.IndexOf(choice.reference);
            if (index < 0) { pendingCardChoice = null; return false; }
            selected = selected ?? new List<string>();
            if (selected.Count != selected.Distinct().Count() || selected.Any(id => !choice.handReferences.Contains(id) || !c.hand.Contains(id))) return false;
            if (choice.kind == "target" && mode != "self" && mode != "enemy" || choice.kind == "charge" && mode != "now" && mode != "charge"
                || choice.kind == "sacrifice" && (mode != "cast" || selected.Count > AdvancedCardMechanics.SacrificeLimit(PlayerCard(choice.reference)))
                || choice.kind == "foresight" && mode != "swap" && mode != "skip") return false;
            if (choice.kind == "foresight" && mode == "swap" && (selected.Count != 1 || !choice.drawReferences.Contains(drawReference) || !c.drawPile.AsEnumerable().Reverse().Take(3).Contains(drawReference))) return false;
            pendingCardChoice = null;
            if (!CanPlayCard(index)) { pendingCardChoice = choice; return false; }
            if (choice.kind == "charge" && mode == "charge") return BeginCharge(index);
            if (choice.kind == "foresight" && mode == "swap")
            {
                int h = c.hand.IndexOf(selected[0]), d = c.drawPile.IndexOf(drawReference);
                c.hand[h] = drawReference; c.drawPile[d] = selected[0];
                RecordCombatInformation(new CombatLogEntry { kind = "foresight", cardId = choice.reference, title = "예견된 운명 교환", detail = GameDatabase.Card(selected[0]).name + " ↔ " + GameDatabase.Card(drawReference).name + " · 손패 수 유지" });
            }
            if (choice.kind == "sacrifice")
                foreach (string reference in selected)
                {
                    int at = c.hand.IndexOf(reference); c.hand.RemoveAt(at); c.exhaustPile.Add(reference);
                    if (c.retainIndex == at) c.retainIndex = -1; else if (c.retainIndex > at) c.retainIndex--;
                    c.chargedCards.RemoveAll(x => x.reference == reference);
                }
            if (choice.kind == "sacrifice" && selected.Count > 0)
                RecordCombatInformation(new CombatLogEntry { kind = "sacrifice", cardId = choice.reference, title = "손패 소멸", detail = string.Join(" · ", selected.Select(id => GameDatabase.Card(id).name)) + " · 이번 전투에서만 소멸" });
            return PlayAdvancedCard(c.hand.IndexOf(choice.reference), new AdvancedEnemyAction { mode = mode, sacrificed = choice.kind == "sacrifice" ? selected.Count : 0 });
        }
        bool BeginCharge(int handIndex)
        {
            var c = State.combat; string reference = c.hand[handIndex]; var card = PlayerCard(reference);
            if (!AdvancedCardMechanics.CanCharge(card) || c.chargedCards.Any(x => x.reference == reference)) return false;
            int price = EffectiveCardCost(reference);
            ConsumeFreeCast(c.freeCasts, reference); SkillMechanics.ConsumeDiscount(c.playerSkills, card.id); SkillMechanics.ConsumeDiscount(c.playerTraits.skills, card.id);
            c.energy -= price; c.hand.RemoveAt(handIndex);
            if (c.retainIndex == handIndex) c.retainIndex = -1; else if (c.retainIndex > handIndex) c.retainIndex--;
            c.chargedCards.Add(new ChargedCard { reference = reference, readyTurn = c.turn + 1 });
            int guard = AdvancedCardMechanics.ChargeGuard(card); c.block += guard;
            RecordBossProgress(BossMechanics.ObserveBlock(c, guard));
            RecordCombatInformation(new CombatLogEntry { kind = "charge", cardId = reference, title = card.name + " 충전 시작", paidCost = price,
                detail = "코스트 " + price + " 지불 · 다음 내 턴부터 강화된 효과를 무료로 발동합니다. 발동 전까지 손패 한 칸을 차지합니다." });
            EmitCombatAction(new CombatAction { id = ++actionSerial, cardId = card.id, cardReference = reference, kind = "charge_start", block = guard });
            RefreshEnemyIntent(); return Say(card.name + " 충전 중 · 다음 내 턴에 발동할 수 있습니다.");
        }
        bool PlayAdvancedCard(int handIndex, AdvancedEnemyAction action)
        {
            var c = State.combat;
            if (!CanPlayCard(handIndex)) return false;
            string reference = c.hand[handIndex]; var card = PlayerCard(reference);
            action = action ?? new AdvancedEnemyAction();
            action.charge = IsCharged(reference) ? 1 : 0;
            bool free = c.freeCasts.Any(g => FreeCastMechanics.Matches(g, reference));
            action.x = AdvancedCardMechanics.IsX(card) ? free ? 0 : Math.Max(0, c.energy) : 0;
            playerAdvancedAction = action;
            try { return AdvancedCardMechanics.IsX(card) && action.x==0 ? ExecuteEmptyX(handIndex) : ExecutePlayerCard(handIndex); }
            finally { playerAdvancedAction = null; }
        }
        bool ExecuteEmptyX(int handIndex)
        {
            var c=State.combat; string reference=c.hand[handIndex]; var card=PlayerCard(reference);
            ConsumeFreeCast(c.freeCasts,reference);SkillMechanics.ConsumeDiscount(c.playerSkills,card.id);SkillMechanics.ConsumeDiscount(c.playerTraits.skills,card.id);
            c.hand.RemoveAt(handIndex);
            if(c.retainIndex==handIndex)c.retainIndex=-1;else if(c.retainIndex>handIndex)c.retainIndex--;
            if(card.exhaust)c.exhaustPile.Add(reference);else c.discardPile.Add(reference);
            RecordCombatInformation(new CombatLogEntry {kind="card_result",cardId=reference,title=card.name+" · X=0",paidCost=0,detail="무료 사용 또는 남은 코스트 0: 피해·카드 효과·사용 시 발동 효과가 발생하지 않았습니다."});
            EmitCombatAction(new CombatAction {id=++actionSerial,cardId=card.id,cardReference=reference});
            RefreshEnemyIntent();return true;
        }
        void HoldChargedCards()
        {
            var c = State.combat;
            foreach (var held in c.chargedCards)
            {
                int at = c.hand.IndexOf(held.reference); if (at >= 0) c.hand.RemoveAt(at);
            }
        }
        int RestoreChargedCards()
        {
            var c = State.combat; int restored = 0;
            foreach (var held in c.chargedCards.Where(x => x.readyTurn <= c.turn + 1))
                if (!c.hand.Contains(held.reference)) { c.hand.Add(held.reference); restored++; }
            return restored;
        }
        AdvancedEnemyAction EnemyAdvancedActionAt(int index)
        {
            var c = State.combat;
            return c.enemyAdvancedActions != null && index >= 0 && index < c.enemyAdvancedActions.Count ? c.enemyAdvancedActions[index] : new AdvancedEnemyAction();
        }
        static SkillInstantBonus AddAdvancedBonus(SkillInstantBonus value, SkillInstantBonus extra)
        {
            value.damage += extra.damage; value.block += extra.block; value.heal += extra.heal; return value;
        }
        // Sua's shield is consumed before ordinary block so later block gains cannot prolong its immunity.
        int AbsorbCombatBlock(bool enemy,int amount)
        {
            var c=State.combat;int absorbed=Math.Min(Math.Max(0,enemy?c.enemyBlock:c.block),Math.Max(0,amount));
            if(enemy)c.enemyBlock-=absorbed;else c.block-=absorbed;
            StatusMechanics.ConsumeProtection(enemy?c.enemyStatuses:c.playerStatuses,absorbed);
            return absorbed;
        }
        void ClearCombatBlock(bool enemy)
        {
            var c=State.combat;if(enemy)c.enemyBlock=0;else c.block=0;
            StatusMechanics.ClearProtection(enemy?c.enemyStatuses:c.playerStatuses);
        }
        SkillInstantBonus EnemyAdvancedBonus(CardDef card, AdvancedEnemyAction action, int hp, int block)
        {
            return AdvancedCardMechanics.ContextualBonus(card, hp, State.combat.enemyMaxHp, block, action.charge, action.sacrificed, action.x);
        }
        AdvancedEnemyAction PlanAdvancedEnemyAction(CardDef card, int handIndex, int available)
        {
            var c=State.combat; var action=new AdvancedEnemyAction();
            action.charge=HasUnplannedEnemyCharge(card.id)?1:0;
            if(AdvancedCardMechanics.CanCharge(card) && action.charge==0 && !c.enemyChargedCards.Any(x=>x.reference==card.id) && c.turn%2==1 && !c.enemyAdvancedActions.Any(x=>x.mode=="charge")) action.mode="charge";
            if(AdvancedCardMechanics.Kind(card)=="target")action.mode=c.enemyHp*2<c.enemyMaxHp?"self":"enemy";
            if(AdvancedCardMechanics.IsX(card))action.x=Math.Max(0,available);
            if(AdvancedCardMechanics.SacrificeLimit(card)>0)
            {
                var other=c.enemyHand.Where((id,i)=>i!=handIndex).Where(id=>CardCost(id)>available || id=="basic_attack").Take(2).ToList();
                foreach(var id in other){c.enemyHand.Remove(id);action.sacrificedReferences.Add(id);action.sacrificed++;}
            }
            return action;
        }
        bool HasUnplannedEnemyCharge(string id)
        {
            var c=State.combat;
            int held=c.enemyChargedCards.Count(x=>x.reference==id && x.readyTurn<=c.turn);
            int claimed=c.enemyPlan.Where((planned,i)=>planned==id && EnemyAdvancedActionAt(i).charge>0).Count();
            return claimed<held;
        }
        int AdvancedEnemyPrice(CardDef card, int ordinary, int available, bool free, AdvancedEnemyAction action)
        {
            if(action.charge>0)return 0;
            return AdvancedCardMechanics.IsX(card)?free?0:Math.Max(0,available):ordinary;
        }
        bool BeginEnemyCharge(CardDef card, int price)
        {
            var c=State.combat;
            c.enemyAvailableEnergy-=price;
            ConsumeFreeCast(c.enemyFreeCasts,card.id);SkillMechanics.ConsumeDiscount(c.enemySkills,card.id);SkillMechanics.ConsumeDiscount(c.enemyTraits.skills,card.id);
            c.enemyChargedCards.Add(new ChargedCard { reference=card.id,readyTurn=c.turn+1 });
            int guard=AdvancedCardMechanics.ChargeGuard(card);c.enemyBlock+=guard;
            RecordCombatInformation(new CombatLogEntry {kind="charge",cardId=card.id,enemy=true,title=card.name+" 충전 시작",paidCost=price,detail="다음 상대 턴에 강화된 효과를 무료로 발동합니다."});
            EmitCombatAction(new CombatAction{id=++actionSerial,cardId=card.id,enemy=true,kind="charge_start",block=guard});
            return true;
        }
        void RecoverCancelledEnemyCharges()
        {
            var c=State.combat;
            for(int i=0;i<c.enemyPlan.Count;i++)
            {
                var action=EnemyAdvancedActionAt(i);
                if(action.mode=="charge" && !c.enemyChargedCards.Any(x=>x.reference==c.enemyPlan[i]))c.enemyDiscardPile.Add(c.enemyPlan[i]);
                if(!action.sacrificeCommitted && action.sacrificedReferences!=null)c.enemyDiscardPile.AddRange(action.sacrificedReferences);
            }
        }
        int SimulateSharedDirectDamage(SkillActorState defender, int hp, ref int block, int perHit, int extra, int hits, StatusActorState defenderStatuses = null)
        {
            int total=0;
            for(int i=0;i<Math.Max(1,hits) && total<hp;i++)
            {
                int raw=RunModes.ScaleEnemyDirectDamage(State,perHit+(i==0?extra:0));
                int absorbed=Math.Min(block,raw);block-=absorbed;
                if(defenderStatuses!=null)StatusMechanics.ConsumeProtection(defenderStatuses,absorbed);
                int shared=CompanionMechanics.Absorb(defender,raw-absorbed).remaining;
                int deferred=(int)((long)shared*Math.Min(20,EquipmentSum(g=>g.damageDeferral))/100);
                total+=CompanionMechanics.ClampOwnerDamage(defender,hp-total,shared-deferred);
            }
            return total;
        }
    }
}
