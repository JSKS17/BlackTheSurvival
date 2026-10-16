using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Lumia
{
    public sealed class CardConditionPreview
    {
        public string condition, effect, current, result;
        public bool met, requiresHit, scheduled;
    }

    public sealed class CardUsePreview
    {
        public string cardId, name, classification, unavailableReason, bossWarning, damageNote;
        public bool enemy, canUse, inHand, hasFreeCast, bossViolation, bossPossibleViolation, guaranteedHit;
        public int handIndex = -1, baseCost, currentCost, energy, discount, costPenalty, freeUses;
        public int normalDamage, hits, targetBlock, accuracyChance, evasionChance, criticalChance;
        public List<CardConditionPreview> conditions = new List<CardConditionPreview>();
    }

    [Serializable]
    public sealed class CombatLogEntry
    {
        public int sequence, nodeId, act, turn;
        public string opponent, kind, cardId, traitId, title, detail;
        public bool enemy, critical, execution;
        public int damage, block, heal, avoided, missed, absorbed, destroyedBlock, companionAbsorbed, paidCost = -1;
    }

    /// <summary>Read-only information models. Never resolve a card, sample RNG, or consume a grant.</summary>
    public static class CombatInformation
    {
        public const int HistoryLimit = 240;

        // Public serializable fields are copied before calling shared forecast helpers. Several legacy
        // Ensure/Clone helpers normalize their input, so they must never see the live run here.
        internal static T Copy<T>(T value) where T : class { return (T)CopyObject(value); }
        static object CopyObject(object value)
        {
            if (value == null) return null;
            Type type = value.GetType();
            if (type.IsValueType || value is string) return value;
            if (type.IsArray)
            {
                var input = (Array)value; var result = Array.CreateInstance(type.GetElementType(), input.Length);
                for (int i = 0; i < input.Length; i++) result.SetValue(CopyObject(input.GetValue(i)), i);
                return result;
            }
            if (value is IList)
            {
                var result = (IList)Activator.CreateInstance(type);
                foreach (var item in (IList)value) result.Add(CopyObject(item));
                return result;
            }
            var clone = Activator.CreateInstance(type);
            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public))
                field.SetValue(clone, CopyObject(field.GetValue(value)));
            return clone;
        }

        internal static bool ConditionsMet(SkillRule rule, SkillActorState actor, CardDef card)
        {
            if (rule == null) return true;
            if (!ResourceMet(rule.conditionKey, rule.conditionAmount, rule.conditionExact, actor, card)) return false;
            if (!ResourceMet(rule.conditionKey2, rule.conditionAmount2, rule.conditionExact2, actor, card)) return false;
            if (!string.IsNullOrEmpty(rule.conditionPrevious))
            {
                var previous = actor?.history?.FirstOrDefault(x => x.owner == card.owner);
                var last = previous == null ? null : GameDatabase.Card(previous.cardId);
                if (last == null || last.id != rule.conditionPrevious && last.key != rule.conditionPrevious) return false;
            }
            return true;
        }
        static bool ResourceMet(string key, int amount, bool exact, SkillActorState actor, CardDef card)
        {
            if (string.IsNullOrEmpty(key)) return true;
            int current = SkillMechanics.Resource(actor, card.owner, key);
            return exact ? current == amount : current >= amount;
        }
        static bool Conditional(SkillRule rule)
        {
            return rule != null && (rule.onHit || !string.IsNullOrEmpty(rule.conditionKey) || !string.IsNullOrEmpty(rule.conditionKey2) || !string.IsNullOrEmpty(rule.conditionPrevious));
        }
        static string CurrentCondition(SkillRule rule, SkillActorState actor, CardDef card)
        {
            var parts = new List<string>();
            AddCurrent(parts, rule.conditionKey, rule.conditionAmount, rule.conditionExact, actor, card);
            AddCurrent(parts, rule.conditionKey2, rule.conditionAmount2, rule.conditionExact2, actor, card);
            if (!string.IsNullOrEmpty(rule.conditionPrevious))
            {
                var previous = actor?.history?.FirstOrDefault(x => x.owner == card.owner);
                parts.Add("직전 기술: " + (GameDatabase.Card(previous?.cardId)?.name ?? "없음"));
            }
            return string.Join(" · ", parts);
        }
        static void AddCurrent(List<string> parts, string key, int amount, bool exact, SkillActorState actor, CardDef card)
        {
            if (string.IsNullOrEmpty(key)) return;
            int current = SkillMechanics.Resource(actor, card.owner, key);
            parts.Add(SkillMechanics.ResourceName(card, key) + " " + current + " / 필요 " + amount + (exact ? "" : " 이상"));
        }
        internal static List<CardConditionPreview> Conditions(CardDef card, SkillActorState actor)
        {
            var result = new List<CardConditionPreview>();
            var rules = card.mechanics?.rules ?? new SkillRule[0];
            var descriptions = SkillMechanics.DescriptionEffects(card);
            foreach (var group in rules.Where(Conditional).GroupBy(r => DescriptionSummary.ConditionKey(r)))
            {
                var rule = group.First();
                var text = descriptions.Where(x => x.key == group.Key).ToList();
                bool met = ConditionsMet(rule, actor, card);
                result.Add(new CardConditionPreview { condition = DescriptionSummary.ConditionPrefix(text.FirstOrDefault()?.condition ?? "추가 효과 조건").TrimEnd(',', ' '),
                    effect = DescriptionSummary.Normalize(string.Join(", ", text.Select(x => x.effect).Distinct())),
                    current = CurrentCondition(rule, actor, card), met = met, requiresHit = rule.onHit,
                    result = !met ? "현재 수치·직전 기술 조건 미충족" : rule.onHit ? "현재 수치 조건 충족 · 적중 후 발동 여부 결정" : "현재 사용 전 조건 충족" });
            }
            if (card.freeCastCount > 0 && card.freeCastTargets?.Length > 0)
            {
                var effect = FreeCastMechanics.Effect(card);
                bool met = FreeCastMechanics.Eligible(card, actor, true);
                bool hit = card.freeCastOnHit || card.freeCastCondition?.onHit == true;
                string targetNote = card.freeCastLastSkill ? " 실제 직전 기술과 보유 카드가 있어야 다시 가져옵니다." : " 보유한 대상 카드에만 무료 사용권이 생깁니다.";
                result.Add(new CardConditionPreview { condition = DescriptionSummary.ConditionPrefix(effect.condition).TrimEnd(',', ' '), effect = effect.effect + targetNote,
                    current = CurrentCondition(card.freeCastCondition ?? new SkillRule(), actor, card), met = met, requiresHit = hit, scheduled = card.freeCastNextTurn,
                    result = !met ? "현재 연계 조건 미충족" : card.freeCastNextTurn ? "다음 자기 턴에 사용할 무료 연계를 예약합니다" + (hit ? " · 적중 필요" : "") : hit ? "현재 수치 조건 충족 · 적중해야 무료 사용권 획득" : "현재 무료 연계 조건 충족" });
            }
            foreach (var group in (card.statuses ?? new CardStatusRule[0]).GroupBy(r => DescriptionSummary.ConditionKey(r, r.timing)))
            {
                var rule = group.First();
                bool met = StatusMechanics.Eligible(rule, actor, card, true), pending = rule.timing != "cast";
                result.Add(new CardConditionPreview { condition = "상태이상 조건" + (rule.onHit ? " · 적중 필요" : ""),
                    effect = string.Join(" · ", group.Select(r => StatusMechanics.Name(r.key) + " " + r.duration + "턴")) + (pending ? " · 예약 효과(" + TimingName(rule.timing) + ")" : ""),
                    current = CurrentCondition(rule, actor, card), met = met, requiresHit = rule.onHit, scheduled = pending,
                    result = !met ? "현재 조건 미충족" : pending ? "현재 예약 조건 충족 · 실제 발동은 후속 행동에서 결정" : rule.onHit ? "현재 수치 조건 충족 · 적중 후 부여" : "현재 부여 조건 충족" });
            }
            return result;
        }
        public static string TimingName(string timing)
        {
            return timing == "next_basic" ? "다음 기본 공격" : timing == "cast" ? "기술 사용" : timing == "summon" ? "소환물 공격" : timing == "delayed_damage" ? "설치물 발동" : timing;
        }
        public static string HistoryLine(CombatLogEntry entry)
        {
            if (entry == null) return "";
            return entry.act + "막 · " + entry.opponent + " · " + entry.turn + "턴 · " + (entry.enemy ? "상대" : "하나") + " / " + entry.title + "\n" + entry.detail;
        }
    }

    public sealed partial class GameEngine
    {
        GameEngine(RunState informationCopy, bool readOnlyInformation) { State = informationCopy; }
        public CardUsePreview PreviewCardUse(int handIndex)
        {
            var hand = State.combat?.hand;
            return PreviewCardUse(hand != null && handIndex >= 0 && handIndex < hand.Count ? hand[handIndex] : null, false, handIndex);
        }
        public CardUsePreview PreviewCardUse(string id, bool enemy = false, int handIndex = -1)
        {
            var copy = new GameEngine(CombatInformation.Copy(State), true);
            return copy.BuildCardUsePreview(id, enemy, handIndex);
        }
        CardUsePreview BuildCardUsePreview(string id, bool enemy, int handIndex)
        {
            var card = enemy ? EnemyActionCard(id) : PreparedPlayerCard(id);
            var result = new CardUsePreview { cardId = id, enemy = enemy, handIndex = handIndex };
            if (card == null) { result.unavailableReason = "카드 정보가 없습니다."; return result; }
            result.name = card.name; result.classification = CardPresentation.ClassificationLabel(card); result.baseCost = card.cost;
            var c = State.combat;
            if (c == null) { result.currentCost = card.cost; result.unavailableReason = "전투 중에 사용 전 정보를 확인할 수 있습니다."; return result; }
            var actor = enemy ? c.enemySkills : c.playerSkills;
            var traits = enemy ? c.enemyTraits : c.playerTraits;
            var statuses = enemy ? c.enemyStatuses : c.playerStatuses;
            result.currentCost = enemy ? EnemyEffectiveCardCost(id) : EffectiveCardCost(id);
            result.energy = enemy ? c.enemyTurn?c.enemyAvailableEnergy:c.enemyEnergy : c.energy;
            result.discount = Math.Max(SkillMechanics.Discount(actor, card.id), SkillMechanics.Discount(traits.skills, card.id));
            result.costPenalty = StatusMechanics.CostPenalty(statuses, card);
            result.freeUses = (enemy ? c.enemyFreeCasts : c.freeCasts).Where(x => FreeCastMechanics.Matches(x,id)).Sum(x => x.uses);
            result.hasFreeCast = result.freeUses > 0;
            if (!enemy && (handIndex < 0 || handIndex >= c.hand.Count || c.hand[handIndex] != id)) result.handIndex = c.hand.IndexOf(id);
            result.inHand = enemy ? c.enemyHand.Contains(id) || c.enemyPlan.Skip(c.enemyActionIndex).Contains(id) : result.handIndex >= 0;
            result.canUse = !enemy && CanPlayCard(result.handIndex);
            if (State.stage != RunStage.Combat) result.unavailableReason = "진행 중인 전투가 아닙니다.";
            else if (!enemy && c.enemyTurn) result.unavailableReason = "상대의 행동이 진행 중입니다.";
            else if (!result.inHand) result.unavailableReason = enemy ? "현재 상대의 남은 예정 행동에 없습니다." : "현재 손패에 없습니다.";
            else if (!StatusMechanics.CanUse(statuses, card)) result.unavailableReason = InformationUseRestriction(statuses, card);
            else if (result.currentCost > result.energy && (!enemy || c.enemyTurn)) result.unavailableReason = "코스트가 " + (result.currentCost - result.energy) + " 부족합니다.";
            else result.unavailableReason = enemy ? "상대 예정 행동입니다. 앞선 행동의 적중·연계에 따라 변경될 수 있습니다." : "현재 사용할 수 있습니다.";
            if(enemy)
            {
                int planned=c.enemyPlan.FindIndex(c.enemyActionIndex, x=>x==id);
                var action=EnemyAdvancedActionAt(planned);
                int available=c.enemyTurn?c.enemyAvailableEnergy:c.enemyEnergy;
                int price=AdvancedEnemyPrice(card,result.currentCost,available,result.hasFreeCast,action);
                if(!c.enemyTurn && planned>=0 && planned<c.enemyPlanCosts.Count)price=c.enemyPlanCosts[planned];
                result.currentCost=price;
                card=AdvancedCardMechanics.PreparedCard(card,action.mode,action.charge,action.sacrificed,AdvancedCardMechanics.IsX(card)?result.hasFreeCast?0:price:0);
                result.normalDamage=action.mode=="charge"?0:EnemyDirectHitTotal(EnemyCardDamage(card),EnemyTotalBonusDamage(card,actor,traits,c.vulnerable,EnemyAdvancedBonus(card,action,c.enemyHp,c.enemyBlock).damage),Math.Max(1,card.hits));
            }
            else result.normalDamage = CardTotalDamage(id);
            result.hits = Math.Max(1, card.hits); result.targetBlock = enemy ? c.block : c.enemyBlock;
            result.evasionChance = enemy ? Evasion : EnemyEvasion;
            result.accuracyChance = enemy ? EnemyAccuracy : Accuracy;
            result.guaranteedHit = AccuracyMechanics.GuaranteedHit(card, actor, result.hasFreeCast);
            result.criticalChance = card.id == "basic_attack" ? enemy ? EnemyCritChance : CritChance : 0;
            result.damageNote = "일반 적중을 가정한 직접 피해 합계입니다. 현재 방어도·명중 실패·회피·피격 패시브·치명타·체력 제한은 차감하지 않았습니다. 설치물·지속 피해·후속 패시브 피해는 별도입니다.";
            result.damageNote += result.guaranteedHit ? " 이 카드의 직접 공격은 필중하여 명중률 감소와 대상 회피를 무시합니다." : " 각 직접 타격은 명중 판정 후 회피 판정을 따로 합니다. 명중 실패는 회피 성공 효과를 발동하지 않습니다.";
            string advancedDescription=AdvancedCardMechanics.Description(enemy?GameDatabase.Card(id):PlayerCard(id));
            if(!string.IsNullOrEmpty(advancedDescription))result.damageNote+=" "+advancedDescription;
            if(!enemy && IsCharged(id))result.damageNote+=" 충전 완료: 현재 강화된 효과와 무료 발동을 반영했습니다.";
            if ((card.mechanics?.rules ?? new SkillRule[0]).Any(rule => rule.op == "break_block"))
                result.damageNote += SkillMechanics.BreaksBlock(actor, card)
                    ? " 현재 검거가 준비되어 있어 적중하면 피해 전에 대상 방어도 " + result.targetBlock + "을 모두 없앱니다. 파괴한 방어도는 피해 합계에 포함되지 않습니다."
                    : " 이번 사용은 검거를 준비하며 보호막을 파괴하지 않습니다. 준비 후 다음 사용이 적중해야 피해 전에 대상 방어도를 모두 없앱니다.";
            result.conditions = CombatInformation.Conditions(card, actor);
            if(enemy && WildlifeMechanics.IsAnimalSkill(id))
            {
                bool interrupted=WildlifeMechanics.Interrupted(id,c.enemyStatuses);
                result.classification="동물 전용 · "+(card.movement ? "이동" : "공격");
                if(interrupted) {result.normalDamage=0;result.unavailableReason="강한 제어 또는 속박으로 동물 기술 시전이 중단됩니다.";}
                result.damageNote=WildlifeMechanics.Description(id);
            }
            if (card.id == "garnet_r")
            {
                int targetHp = enemy ? State.hp : c.enemyHp, targetMaxHp = enemy ? State.maxHp : c.enemyMaxHp;
                bool mark = SkillMechanics.Resource(actor, card.owner, "embrace") > 0;
                bool ready = FreeCastMechanics.GarnetExecutionReady(card, actor, result.hasFreeCast, targetHp, targetMaxHp);
                result.conditions.Add(new CardConditionPreview {
                    condition="이번 턴 무료 재사용 · 철처녀 표식 · 사용 전 체력 " + FreeCastMechanics.GarnetExecutionPercent + "% 이하 · 필중",
                    effect="방어도를 무시하고 적의 남은 체력을 모두 없애 처형합니다.",
                    current="무료 사용권 " + result.freeUses + "회 · 철처녀 표식 " + (mark ? "활성" : "없음") + " · 대상 체력 " + targetHp + "/" + targetMaxHp,
                    met=ready, requiresHit=true,
                    result=ready ? "현재 처형 조건 충족 · 필중 재사용 시 처형" : "현재 처형 조건 미충족 · 일반 피해 적용" });
                result.damageNote += " 가넷의 처형은 위 직접 피해와 별도로 표시한 조건을 확인하며, 재사용 피해를 주기 전 체력으로 판정합니다.";
            }
            if (!enemy) InformationBossWarning(result, card);
            return result;
        }
        static string InformationUseRestriction(StatusActorState statuses, CardDef card)
        {
            var reasons = new List<string>();
            if (card.movement && StatusMechanics.Has(statuses, "root")) reasons.Add("속박으로 이동 카드 사용 불가");
            if (card.category == "skill") foreach (string kind in new[] { "silence", "polymorph", "taunt", "berserk", "dance" }) if (StatusMechanics.Has(statuses, kind)) reasons.Add(StatusMechanics.Name(kind) + "으로 기술 사용 불가");
            if (card.category == "basic" && card.key == "ATK") foreach (string kind in new[] { "blind", "disarm", "polymorph", "dance" }) if (StatusMechanics.Has(statuses, kind)) reasons.Add(StatusMechanics.Name(kind) + "으로 기본 공격 사용 불가");
            return reasons.Count == 0 ? "현재 상태이상으로 사용할 수 없습니다." : string.Join(" · ", reasons);
        }
        void InformationBossWarning(CardUsePreview result, CardDef card)
        {
            var c = State.combat; var phase = BossMechanics.Phase(c);
            if (phase == null || c.boss == null || !c.boss.challengeActive || c.boss.interrupted || c.enemyTurn) { result.bossWarning = "현재 활성화된 보스 대응 조건이 없습니다."; return; }
            if (phase.goal == BossGoal.NoMovement && card.movement || phase.goal == BossGoal.NoDebuffSkill && BossMechanics.IsDebuffSkill(card))
            {
                result.bossViolation = true; result.bossWarning = "이 카드를 사용하면 보스의 " + BossMechanics.GoalText(phase.goal, phase.goalAmount) + " 조건을 위반합니다. 사용 자체가 막히지는 않습니다.";
            }
            else if (phase.goal == BossGoal.NoHeavyHit)
            {
                int ceiling = result.normalDamage;
                if (FreeCastMechanics.GarnetExecutionReady(card, c.playerSkills, result.hasFreeCast, c.enemyHp, c.enemyMaxHp)) ceiling = Math.Max(ceiling, c.enemyHp);
                if (result.criticalChance > 0) ceiling += (GearCriticalDamage(CardDamage(result.cardId)) - CardDamage(result.cardId)) * result.hits;
                result.bossPossibleViolation = ceiling >= Math.Max(1, phase.goalAmount);
                result.bossWarning = "직접 피해 " + Math.Max(1, phase.goalAmount) + " 이상이면 금지 조건을 위반합니다. 방어도에 준 피해도 포함합니다. " + (result.bossPossibleViolation ? "이 카드의 일반 적중·치명타 예상치가 금지 기준에 닿을 수 있습니다." : "현재 예상치는 금지 기준 미만입니다. 실제 피해로 판정합니다.");
            }
            else result.bossWarning = "현재 보스 대응: " + BossMechanics.GoalText(phase.goal, phase.goalAmount) + ". 카드 분류는 '" + result.classification + "'입니다.";
            if (c.boss.violated) result.bossWarning += " 이번 턴은 이미 금지 조건을 위반했습니다.";
        }

        void EnsureCombatInformation()
        {
            if (State.combatHistory == null) State.combatHistory = new List<CombatLogEntry>();
            State.combatHistory.RemoveAll(x => x == null);
            if (State.combatHistory.Count > CombatInformation.HistoryLimit) State.combatHistory.RemoveRange(0, State.combatHistory.Count - CombatInformation.HistoryLimit);
            State.combatHistorySerial = Math.Max(State.combatHistorySerial, State.combatHistory.Select(x => x.sequence).DefaultIfEmpty(0).Max());
        }
        void RecordCombatInformation(CombatLogEntry entry)
        {
            if (State.combat == null) return;
            if (State.combatHistory == null) State.combatHistory = new List<CombatLogEntry>();
            entry.sequence = ++State.combatHistorySerial; entry.nodeId = State.activeNodeId; entry.act = State.act;
            entry.turn = State.combat.turn; entry.opponent = State.combat.enemyName;
            State.combatHistory.Add(entry);
            if (State.combatHistory.Count > CombatInformation.HistoryLimit) State.combatHistory.RemoveRange(0, State.combatHistory.Count - CombatInformation.HistoryLimit);
        }
        void RecordCombatCardStart(CardDef card, bool enemy, int paidCost, bool free, string reference = null)
        {
            string upgrade=enemy ? "" : CardUpgradeLabel(reference);
            RecordCombatInformation(new CombatLogEntry { kind = "card_start", cardId = reference ?? card.id, title = card.name + (enemy ? "" : CardUpgradeSuffix(reference)) + " 사용", enemy = enemy, paidCost = paidCost,
                detail = "지불 코스트 " + paidCost + (free ? " · 무료 사용권 사용" : "") + " · " + CardPresentation.ClassificationLabel(card) + (string.IsNullOrEmpty(upgrade) ? "" : " · 강화: "+upgrade) });
        }
        void RecordLegacyDebuffs(CardDef card, bool enemy, bool landed, int poisonBefore, int weakBefore, int vulnerableBefore, string reference = null)
        {
            if (!landed) return;
            var c = State.combat;
            int poison = enemy ? c.poison : c.enemyPoison, weak = enemy ? c.weak : c.enemyWeak, vulnerable = enemy ? c.vulnerable : c.enemyVulnerable;
            var parts = new List<string>();
            if (poison > poisonBefore) parts.Add("중독 " + poison);
            if (weak > weakBefore) parts.Add("약화 " + weak + "턴");
            if (vulnerable > vulnerableBefore) parts.Add("취약 " + vulnerable + "턴");
            string marker=enemy ? "" : CardUpgradeSuffix(reference);
            if (parts.Count > 0) RecordCombatInformation(new CombatLogEntry { kind = "debuff", cardId = reference ?? card.id, enemy = enemy, title = card.name + marker + " · 디버프 부여", detail = (enemy ? "하나" : c.enemyName) + "에게 " + string.Join(" · ", parts) });
        }
        void EmitCombatAction(CombatAction action)
        {
            CombatActions.Add(action);
            var c = State.combat; if (c == null) return;
            var card = action.enemy ? EnemyActionCard(action.cardId) : GameDatabase.Card(action.cardId); var passive = GameDatabase.Passive(action.traitId); var rune = GameDatabase.Rune(action.traitId);
            var gear = (action.traitId ?? "").StartsWith("gear:", StringComparison.Ordinal) ? GameDatabase.Equipment(action.traitId.Substring(5)) : action.kind == "deferred_damage" ? GameDatabase.Equipment(action.cardId) : null;
            bool direct = card != null && string.IsNullOrEmpty(action.kind) && string.IsNullOrEmpty(action.traitId);
            bool status = (action.kind ?? "").StartsWith("status_", StringComparison.Ordinal);
            string identity = passive != null ? "패시브 · " + passive.name : rune != null ? "룬 · " + rune.name : gear != null ? "장비 · " + gear.name
                : action.cardId == "status_poison" ? "중독" : (card?.name ?? "전투 효과") + RareCardUpgrades.SuffixForLabel(action.upgradeLabel);
            var parts = new List<string>();
            string target = action.enemy ? "하나" : c.enemyName;
            if (action.damage > 0) parts.Add(target + " 체력 피해 " + action.damage);
            if (action.absorbed > 0) parts.Add(target + " 방어도 흡수 " + action.absorbed);
            if (action.destroyedBlock > 0) parts.Add(target + " 방어도 파괴 " + action.destroyedBlock + " · 직접 피해 전 제거");
            if (action.companionAbsorbed > 0) parts.Add(target + "의 니나가 피해 " + action.companionAbsorbed + "을 대신 받았습니다.");
            if (action.avoided > 0) parts.Add(target + " 회피 " + action.avoided + "회");
            if (action.missed > 0) parts.Add("명중 실패 " + action.missed + "회");
            if (action.block > 0) parts.Add("방어도 +" + action.block);
            if (action.heal > 0) parts.Add("체력 회복 +" + action.heal);
            if (action.critical) parts.Add("치명타 " + Math.Max(1, action.criticalHits) + "회");
            if (action.execution) parts.Add("처형 · 재사용 전 체력 " + FreeCastMechanics.GarnetExecutionPercent + "% 이하");
            if (status) parts.Add(target + " · " + StatusMechanics.Name(action.kind.Substring(7)));
            if (!direct && !status && !string.IsNullOrEmpty(action.kind)) parts.Add(InformationKind(action.kind));
            if (parts.Count == 0) parts.Add(direct ? "직접 피해·방어·회복 수치 없음 · 카드의 조건·필드 효과를 처리했습니다." : "효과 발동");
            RecordCombatInformation(new CombatLogEntry { kind = direct ? "card_result" : status ? "debuff" : "effect", cardId = direct ? action.cardReference ?? action.cardId : action.cardId, traitId = action.traitId,
                title = identity + (direct ? " 결과" : " 발동"), detail = string.Join(" · ", parts), enemy = action.enemy,
                damage = action.damage, block = action.block, heal = action.heal, avoided = action.avoided, missed = action.missed, absorbed = action.absorbed, destroyedBlock = action.destroyedBlock, companionAbsorbed = action.companionAbsorbed, critical = action.critical, execution = action.execution });
        }
        static string InformationKind(string kind)
        {
            switch (kind)
            {
                case "bleed": return "출혈 피해"; case "burn": return "화상 피해"; case "summon": return "소환물 공격";
                case "delayed_damage": return "설치물 발동"; case "deferred_damage": return "유예 피해"; case "hot": return "지속 회복";
                case "guard": return "지속 보호"; case "counter": return "반격"; case "revive": return "치명상 방지";
                case "companion_damage": return "니나 피해 분담"; case "charge_start": return "충전 시작";
                case "boss_interrupt": return "보스 특수 행동 저지"; case "augment": return "증강 발동";
                case "wildlife_skill": return "동물 전용 기술"; case "wildlife_interrupt": return "동물 기술 시전 중단";
                case "wildlife_despair": return "좌절 · 다음 내 턴까지 받는 피해 +50%";
                default: return kind.StartsWith("boss_", StringComparison.Ordinal) ? "보스 특수 행동" : "부가 효과";
            }
        }
    }
}
