using System;
using System.Collections.Generic;
using System.Linq;

namespace Lumia
{
    // Reset conditions read the skill state immediately before this card's own effects.
    // Keeping this pure makes enemy planning and previews use the same checks as play.
    public static class FreeCastMechanics
    {
        public static bool IsReplaySkill(CardDef card)
        {
            return card != null && card.category == "skill" && (card.key == "Q" || card.key == "W" || card.key == "E");
        }

        public static bool Matches(FreeCastGrant grant, string reference)
        {
            return grant != null && grant.uses > 0 && CardInstances.SameType(grant.cardId,reference)
                && (string.IsNullOrEmpty(grant.cardReference) || grant.cardReference == reference);
        }
        public static void SanitizePending(List<PendingFreeCast> pending)
        {
            if (pending == null) return;
            pending.RemoveAll(x => x == null || x.uses <= 0 || GameDatabase.Card(x.cardId) == null
                || GameDatabase.Card(x.sourceCardId)?.freeCastNextTurn != true
                || !GameDatabase.Card(x.sourceCardId).freeCastTargets.Contains(x.cardId));
            var seen = new HashSet<string>();
            pending.RemoveAll(x => !seen.Add(x.sourceCardId + "|" + x.cardId));
            foreach (var grant in pending) { grant.uses = Math.Min(1, grant.uses); grant.readyTurn = Math.Max(0, grant.readyTurn); }
        }

        // Enemy planning peeks at the queue and moves real copies once; enemy turn
        // start then consumes it without recalling another copy. Saves keep the queue
        // until that actual turn starts, so previews never award gameplay permissions.
        public static int ActivatePending(List<PendingFreeCast> pending, int turn, IEnumerable<string> owned,
            List<FreeCastGrant> grants, List<string> sources, List<string> hand, List<string> draw, List<string> discard,
            bool recall = true, bool consume = true)
        {
            if (pending == null) return 0;
            int activated = 0;
            foreach (var queued in pending.Where(x => x.readyTurn <= turn).ToArray())
            {
                if (consume) pending.Remove(queued);
                string source = queued.sourceCardId + "|" + queued.cardId;
                if (queued.uses <= 0 || !owned.Any(id=>CardInstances.SameType(id,queued.cardId)) || GameDatabase.Card(queued.cardId) == null || sources.Contains(source)) continue;
                if (recall && !new[]{hand,draw,discard}.Any(p=>p.Any(id=>CardInstances.SameType(id,queued.cardId)))) continue;
                sources.Add(source);
                var grant = grants.FirstOrDefault(x => x.cardId == queued.cardId);
                if (grant == null) { grant = new FreeCastGrant { cardId = queued.cardId }; grants.Add(grant); }
                grant.uses += queued.uses; activated++;
                if (!recall || hand.Any(id=>CardInstances.SameType(id,queued.cardId)) || hand.Count >= 12) continue;
                int index = discard.FindLastIndex(id=>CardInstances.SameType(id,queued.cardId));
                if (index >= 0) { string actual=discard[index];discard.RemoveAt(index); hand.Add(actual); continue; }
                index = draw.FindLastIndex(id=>CardInstances.SameType(id,queued.cardId));
                if (index >= 0) { string actual=draw[index];draw.RemoveAt(index); hand.Add(actual); }
            }
            return activated;
        }

        // The fan game's conservative threshold is lower than Garnet's original 25%.
        // Read health before this recast's damage, and require the original target mark.
        public const int GarnetExecutionPercent = 20;
        public static bool GarnetExecutionReady(CardDef card, SkillActorState before, bool freeCast, int hp, int maximumHp)
        {
            return card?.id == "garnet_r" && freeCast && hp > 0 && maximumHp > 0
                && (long)hp * 100 <= (long)maximumHp * GarnetExecutionPercent
                && SkillMechanics.Resource(before, card.owner, "embrace") > 0;
        }
        public static bool GarnetRecallBeforeExhaust(CardDef card, bool freeCast, IEnumerable<string> sources)
        {
            return card?.id == "garnet_r" && !freeCast && !(sources ?? Enumerable.Empty<string>()).Contains("garnet_r|garnet_r");
        }
        public static string ExhaustDescription(CardDef card)
        {
            return card?.id == "garnet_r"
                ? "첫 사용이 적중하면 이번 턴에 무료로 다시 사용할 수 있도록 회수하며, 재사용하거나 첫 사용이 빗나가면 이번 전투에서 소멸합니다. 무료 재사용을 남긴 채 턴을 끝내도 회수한 카드는 소멸합니다."
                : "사용 후 이번 전투에서 소멸합니다.";
        }
        public static bool ExpireGarnetRecall(CombatState combat)
        {
            if (combat == null || !combat.freeCasts.Any(x => x.cardId == "garnet_r" && x.uses > 0)
                || !combat.freeCastSources.Contains("garnet_r|garnet_r")) return false;
            var grant=combat.freeCasts.First(x=>x.cardId=="garnet_r" && x.uses>0);
            foreach (var pile in new[] { combat.hand, combat.drawPile, combat.discardPile })
            {
                int index = string.IsNullOrEmpty(grant.cardReference) ? pile.FindLastIndex(id=>CardInstances.SameType(id,"garnet_r")) : pile.LastIndexOf(grant.cardReference);
                if (index < 0) continue;
                string actual=pile[index];pile.RemoveAt(index); combat.exhaustPile.Add(actual);
                if (pile == combat.hand)
                {
                    if (combat.retainIndex == index) combat.retainIndex = -1;
                    else if (combat.retainIndex > index) combat.retainIndex--;
                }
                return true;
            }
            return false;
        }
        public static bool Eligible(CardDef card, SkillActorState before, bool landed = true)
        {
            if (card == null || card.freeCastCount <= 0 || card.freeCastTargets == null || card.freeCastTargets.Length == 0) return false;
            var rule = card.freeCastCondition;
            if ((card.freeCastOnHit || (rule != null && rule.onHit)) && !landed) return false;
            if (rule == null) return true;
            bool first = ResourceCondition(before,card,rule.conditionKey,rule.conditionAmount,rule.conditionExact);
            bool second = ResourceCondition(before,card,rule.conditionKey2,rule.conditionAmount2,rule.conditionExact2);
            if (EitherResource(card) ? !first && !second : !first || !second) return false;
            if (!string.IsNullOrEmpty(rule.conditionPrevious))
            {
                var entry = before?.history?.FirstOrDefault(x => x.owner == card.owner);
                var last = entry == null ? null : GameDatabase.Card(entry.cardId);
                if (last == null || (last.id != rule.conditionPrevious && last.key != rule.conditionPrevious)) return false;
            }
            return true;
        }

        public static ConditionEffect Effect(CardDef card, bool compact = false)
        {
            if (card == null || card.freeCastCount <= 0 || card.freeCastTargets == null || card.freeCastTargets.Length == 0) return null;
            var rule = card.freeCastCondition ?? new SkillRule();
            bool onHit = card.freeCastOnHit || rule.onHit;
            var conditions = new List<string>();
            if (EitherResource(card))
            {
                string first = Condition(card,rule.conditionKey,rule.conditionAmount,rule.conditionExact,compact);
                string second = Condition(card,rule.conditionKey2,rule.conditionAmount2,rule.conditionExact2,compact);
                conditions.Add(compact ? "("+first+" 또는 "+second+")" : "("+WithoutEnding(first)+" 또는 "+WithoutEnding(second)+")인 상태이면");
            }
            else
            {
                if (!string.IsNullOrEmpty(rule.conditionKey)) conditions.Add(Condition(card, rule.conditionKey, rule.conditionAmount, rule.conditionExact, compact));
                if (!string.IsNullOrEmpty(rule.conditionKey2)) conditions.Add(Condition(card, rule.conditionKey2, rule.conditionAmount2, rule.conditionExact2, compact));
            }
            if (!string.IsNullOrEmpty(rule.conditionPrevious))
            {
                string previous = compact ? SkillReferences.ShortName(rule.conditionPrevious,card) : SkillReferences.Name(rule.conditionPrevious, card);
                conditions.Add(compact ? "직전 " + previous : card.owner + "의 직전 기술이 " + previous + "이면");
            }
            if (onHit) conditions.Add(compact ? "적중 시" : "공격이 적중하면");
            string[] targets = card.freeCastLastSkill ? new string[0] : card.freeCastTargets;
            string targetNames = compact ? SkillReferences.Keys(targets,card) : SkillReferences.Names(targets,card);
            string availableTurn = card.freeCastNextTurn ? "다음 내 턴" : "이번 턴";
            string effect = card.freeCastLastSkill
                ? compact ? "실험체 구분 없이 마지막 Q·W·E 한 장을 이번 턴에 무료로 " + card.freeCastCount + "회 다시 사용합니다"
                    : "실험체에 관계없이 이번 전투에서 마지막으로 사용한 Q·W·E 카드 한 장을 이번 턴에 코스트 없이 " + card.freeCastCount + "회 다시 사용할 수 있습니다. 기본 공격과 R·D·F 카드는 대상에서 제외됩니다"
                : compact ? card.freeCastCondition != null ? targetNames + " 무료 사용권을 " + card.freeCastCount + "회 " + (card.freeCastNextTurn ? "예약합니다(다음 내 턴)" : "얻습니다(이번 턴)")
                    : CardPresentation.WithParticle(targetNames,"를","을") + " " + availableTurn + "에 무료로 " + card.freeCastCount + "회 사용할 수 있습니다"
                : availableTurn + "에 " + targetNames + " 카드를 " + (targets.Length > 1 ? "각각 " : "") + card.freeCastCount + "회 코스트 없이 사용할 수 있습니다";
            return new ConditionEffect {
                key = DescriptionSummary.ConditionKey(rule, onHit:onHit) + (EitherResource(card)?"|resource_or":""),
                condition = compact ? string.Join("·", conditions) : DescriptionSummary.JoinConditions(conditions),
                effect = effect, note = compact ? "" : card.freeCastConditionNote ?? ""
            };
        }

        static bool EitherResource(CardDef card)
        {
            return card.freeCastEitherResource && card.freeCastCondition != null
                && !string.IsNullOrEmpty(card.freeCastCondition.conditionKey) && !string.IsNullOrEmpty(card.freeCastCondition.conditionKey2);
        }

        static bool ResourceCondition(SkillActorState before,CardDef card,string key,int amount,bool exact)
        {
            if (string.IsNullOrEmpty(key)) return true;
            int count = SkillMechanics.Resource(before,card.owner,key);
            return exact ? count == amount : count >= amount;
        }

        static string WithoutEnding(string condition)
        {
            if (condition.EndsWith("이면",StringComparison.Ordinal)) return condition.Substring(0,condition.Length-2);
            if (condition.EndsWith("아니면",StringComparison.Ordinal)) return condition.Substring(0,condition.Length-3)+"아님";
            return condition;
        }

        static string Condition(CardDef card, string key, int amount, bool exact, bool compact)
        {
            if (!compact) return SkillMechanics.StateCondition(card,key,amount,exact);
            if (SkillMechanics.IsState(card,key)) return SkillMechanics.StateCondition(card,key,amount,exact).Replace("이면", "").Replace("가 아니면", " 아님").Replace("이 아니면", " 아님");
            return SkillMechanics.ResourceName(card,key) + " " + amount + (exact ? "" : " 이상");
        }
    }
}
