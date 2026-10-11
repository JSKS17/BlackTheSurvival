using System;
using System.Collections.Generic;
using System.Linq;

namespace Lumia
{
    public enum BossGoal { Damage, Block, Basic, Skill, Movement, MixedKeys, CheapCards, DebuffSkill, NoDebuffSkill, NoHeavyHit, NoMovement }
    public enum BossEffect { None, Strike, Shield, Heal, DelayedStrike, Prepare, Status, Counter, Drain, Evasion, ResourcePrepare, Cleanse }
    public sealed class BossPhaseDef
    {
        public string name, tell, response, signatureCardId, statusKey, resourceKey, resourceLabel;
        public BossGoal goal, fallbackGoal = BossGoal.Damage;
        public int goalAmount, fallbackAmount, interval = 2, effectAmount, secondaryAmount, resourceCap = 3;
        public BossEffect effect, secondaryEffect;
    }
    public sealed class BossProfile
    {
        public string subjectId, title, overview;
        public BossPhaseDef[] phases = new BossPhaseDef[0];
    }
    [Serializable] public sealed class BossPreparedResource
    {
        public string key, label;
        public int amount, cap;
        public bool isState;
    }
    [Serializable] public sealed class BossBattleState
    {
        public int version = 2, phase = 1, pendingPhase, nextMechanicTurn = 2, challengeTurn;
        public bool challengeActive, interrupted, specialResolved, violated;
        public string violationCardId;
        public int violationDamage;
        public int progress, fallbackProgress, rewardUntilTurn;
        public int preparedEnergy, pendingDrain, delayedDamage, delayedDueTurn, counterDamage, counterUntilTurn;
        public string delayedSourceCard;
        public List<string> playedKeys = new List<string>();
        public List<BossPreparedResource> preparedResources = new List<BossPreparedResource>();
    }
    public sealed class BossGuideView
    {
        public string title, mechanicDescription, phaseRules, counterplay, previewEffects;
    }
    public sealed class BossCombatView
    {
        public int phase, pendingPhase, progress, goalAmount, fallbackProgress, fallbackAmount, nextTurn;
        public bool challengeActive, interrupted, restricted, violated, needsTurnEnd;
        public string violationCardId;
        public int violationDamage, forbiddenDamageThreshold;
        public string phaseName, pendingPhaseText, progressText, nextCondition, nextAction, nextActionDetails, restrictionText;
        public BossGuideView guide;
    }

    // These readable boss challenges are fan-game turn rules, separate from the original skill rules.
    public static class BossMechanics
    {
        public const int MaxExtraDamage = 20, MaxSupport = 24, MaxCounter = 6, RestrictionMinimumCards = 2;
        public static readonly string[] StatusKeys = { "slow", "blind", "armor_break", "heal_reduction", "root", "disarm", "silence", "fear", "charm", "attack_down", "pull" };
        public static BossProfile Profile(string subjectId) { return BossProfiles.Get(subjectId); }
        public static BossPhaseDef Phase(CombatState combat)
        {
            var profile = combat == null ? null : Profile(combat.enemyId);
            return profile == null || combat.boss == null || profile.phases.Length == 0 ? null : profile.phases[Math.Max(0, Math.Min(profile.phases.Length - 1, combat.boss.phase - 1))];
        }
        public static BossBattleState Ensure(BossBattleState state)
        {
            if (state == null) state = new BossBattleState();
            if (state.playedKeys == null) state.playedKeys = new List<string>();
            if (state.preparedResources == null) state.preparedResources = new List<BossPreparedResource>();
            state.phase = Math.Max(1, state.phase); state.nextMechanicTurn = Math.Max(2, state.nextMechanicTurn);
            return state;
        }
        public static bool IsRestriction(BossGoal goal) { return goal == BossGoal.NoDebuffSkill || goal == BossGoal.NoHeavyHit || goal == BossGoal.NoMovement; }
        public static bool IsDebuffSkill(CardDef card)
        {
            return card != null && card.category != "basic" && new[] { "Q", "W", "E", "R", "D", "F" }.Contains(card.key)
                && CardPresentation.DebuffKinds(card).Count > 0;
        }
        public static BossBattleState Clone(BossBattleState state)
        {
            if (state == null) return null;
            return new BossBattleState {
                version = state.version, phase = state.phase, pendingPhase = state.pendingPhase, nextMechanicTurn = state.nextMechanicTurn, challengeTurn = state.challengeTurn,
                challengeActive = state.challengeActive, interrupted = state.interrupted, specialResolved = state.specialResolved, violated = state.violated,
                violationCardId = state.violationCardId, violationDamage = state.violationDamage, progress = state.progress, fallbackProgress = state.fallbackProgress,
                rewardUntilTurn = state.rewardUntilTurn, preparedEnergy = state.preparedEnergy, pendingDrain = state.pendingDrain,
                delayedDamage = state.delayedDamage, delayedDueTurn = state.delayedDueTurn, delayedSourceCard = state.delayedSourceCard,
                counterDamage = state.counterDamage, counterUntilTurn = state.counterUntilTurn,
                playedKeys = state.playedKeys == null ? new List<string>() : state.playedKeys.ToList(),
                preparedResources = state.preparedResources == null ? new List<BossPreparedResource>() : state.preparedResources.Select(x => new BossPreparedResource { key = x.key, label = x.label, amount = x.amount, cap = x.cap, isState = x.isState }).ToList()
            };
        }
        public static BossBattleState PreviewAtTurnEnd(CombatState combat)
        {
            if (combat?.boss == null) return null;
            var preview = Clone(combat.boss);
            if (!combat.enemyTurn) FinalizeChallenge(new CombatState { enemyId = combat.enemyId, turn = combat.turn, boss = preview });
            return preview;
        }
        public static void CheckPhase(CombatState combat)
        {
            if (combat?.boss == null || combat.enemyHp <= 0 || combat.enemyMaxHp <= 0) return;
            var profile = Profile(combat.enemyId);
            if (profile == null) return;
            int desired = combat.enemyHp * 100L <= combat.enemyMaxHp * 25L && profile.phases.Length >= 3 ? 3
                : combat.enemyHp * 2L <= combat.enemyMaxHp ? Math.Min(2, profile.phases.Length) : 1;
            if (desired > combat.boss.phase) combat.boss.pendingPhase = Math.Max(combat.boss.pendingPhase, desired);
        }
        public static void BeginPlayerTurn(CombatState combat)
        {
            if (combat?.boss == null) return;
            var state = combat.boss;
            CheckPhase(combat);
            if (state.pendingPhase > state.phase)
            {
                state.phase = state.pendingPhase; state.pendingPhase = 0;
                state.nextMechanicTurn = Math.Max(2, combat.turn);
            }
            state.specialResolved = false; state.challengeActive = false;
            if (combat.turn < state.nextMechanicTurn || combat.turn < 2) return;
            var phase = Phase(combat); if (phase == null) return;
            state.challengeActive = true; state.challengeTurn = combat.turn; state.interrupted = false;
            state.progress = state.fallbackProgress = 0; state.playedKeys.Clear();
            state.violated = false; state.violationCardId = null; state.violationDamage = 0;
            state.nextMechanicTurn = combat.turn + Math.Max(2, Math.Min(3, phase.interval));
        }
        public static bool ObserveDamage(CombatState combat, int amount)
        {
            if (!CanObserve(combat)) return false;
            var phase = Phase(combat);
            if (IsRestriction(phase.goal)) return false;
            if (phase.goal == BossGoal.Damage) combat.boss.progress += Math.Max(0, amount);
            if (phase.fallbackGoal == BossGoal.Damage) combat.boss.fallbackProgress += Math.Max(0, amount);
            return TryInterrupt(combat);
        }
        public static bool ObserveBlock(CombatState combat, int amount)
        {
            if (!CanObserve(combat)) return false;
            var phase = Phase(combat);
            if (IsRestriction(phase.goal)) return false;
            if (phase.goal == BossGoal.Block) combat.boss.progress += Math.Max(0, amount);
            if (phase.fallbackGoal == BossGoal.Block) combat.boss.fallbackProgress += Math.Max(0, amount);
            return TryInterrupt(combat);
        }
        public static bool ObserveCard(CombatState combat, CardDef card, int paidCost, int directDamage = 0)
        {
            if (!CanObserve(combat) || card == null) return false;
            var phase = Phase(combat); bool counts = false;
            if (IsRestriction(phase.goal))
            {
                combat.boss.progress++;
                bool violation = phase.goal == BossGoal.NoDebuffSkill ? IsDebuffSkill(card)
                    : phase.goal == BossGoal.NoMovement ? card.movement : directDamage >= Math.Max(1, phase.goalAmount);
                if (violation && !combat.boss.violated)
                {
                    combat.boss.violated = true; combat.boss.violationCardId = card.id;
                    combat.boss.violationDamage = Math.Max(0, directDamage);
                }
                return false;
            }
            switch (phase.goal)
            {
                case BossGoal.Basic: counts = card.id == "basic_attack"; break;
                case BossGoal.Skill: counts = card.category == "skill"; break;
                case BossGoal.Movement: counts = card.movement; break;
                case BossGoal.CheapCards: counts = paidCost <= 1; break;
                case BossGoal.DebuffSkill: counts = IsDebuffSkill(card); break;
                case BossGoal.MixedKeys:
                    if (!combat.boss.playedKeys.Contains(card.key)) combat.boss.playedKeys.Add(card.key);
                    combat.boss.progress = combat.boss.playedKeys.Count; break;
            }
            if (counts) combat.boss.progress++;
            return TryInterrupt(combat);
        }
        private static bool CanObserve(CombatState combat)
        {
            return combat?.boss != null && !combat.enemyTurn && combat.boss.challengeActive && !combat.boss.interrupted && Phase(combat) != null;
        }
        private static bool TryInterrupt(CombatState combat)
        {
            var phase = Phase(combat); var state = combat.boss;
            if (IsRestriction(phase.goal)) return false;
            if (state.progress < Math.Max(1, phase.goalAmount) && (phase.fallbackAmount <= 0 || state.fallbackProgress < phase.fallbackAmount)) return false;
            return Interrupt(combat);
        }
        private static bool Interrupt(CombatState combat)
        {
            var state = combat.boss;
            state.interrupted = true; state.rewardUntilTurn = Math.Max(state.rewardUntilTurn, combat.turn + 1);
            return true;
        }
        public static bool FinalizeChallenge(CombatState combat)
        {
            if (!CanObserve(combat) || combat.boss.specialResolved || !IsRestriction(Phase(combat).goal)
                || combat.boss.violated || combat.boss.progress < RestrictionMinimumCards) return false;
            return Interrupt(combat);
        }
        public static bool RewardVulnerable(CombatState combat) { return combat?.boss != null && combat.turn <= combat.boss.rewardUntilTurn && combat.boss.rewardUntilTurn > 0; }
        public static bool NeedsSpecialAction(CombatState combat, BossBattleState preview = null)
        {
            var state = preview ?? combat?.boss;
            return combat != null && state != null && !state.specialResolved &&
                ((state.challengeActive && !state.interrupted) || (state.delayedDamage > 0 && combat.turn >= state.delayedDueTurn));
        }
        public static string GoalText(BossGoal goal, int amount)
        {
            switch (goal)
            {
                case BossGoal.Damage: return "누적 피해 " + amount + " (방어에 준 피해 포함)";
                case BossGoal.Block: return "방어도 획득 " + amount;
                case BossGoal.Basic: return "기본 공격 " + amount + "회 사용";
                case BossGoal.Skill: return "실험체 기술 " + amount + "회 사용";
                case BossGoal.Movement: return "이동 기술 " + amount + "회 사용";
                case BossGoal.MixedKeys: return "서로 다른 카드 키 " + amount + "종 사용";
                case BossGoal.DebuffSkill: return "해로운 효과를 가진 기술 카드 " + amount + "회 사용";
                case BossGoal.NoDebuffSkill: return "해로운 효과를 가진 기술 카드 사용 금지";
                case BossGoal.NoHeavyHit: return "카드 한 번의 직접 피해 합계 " + Math.Max(1, amount) + " 이상 금지";
                case BossGoal.NoMovement: return "이동 카드 사용 금지";
                default: return "지불 코스트 0~1 카드 " + amount + "회 사용";
            }
        }
        public static string GoalLabel(BossGoal goal)
        {
            switch (goal)
            {
                case BossGoal.Damage: return "누적 피해";
                case BossGoal.Block: return "방어도 획득";
                case BossGoal.Basic: return "기본 공격";
                case BossGoal.Skill: return "실험체 기술";
                case BossGoal.Movement: return "이동 기술";
                case BossGoal.MixedKeys: return "다른 키 사용";
                case BossGoal.DebuffSkill: return "디버프 기술";
                case BossGoal.NoDebuffSkill: return "디버프 기술 금지";
                case BossGoal.NoHeavyHit: return "강한 직접 공격 금지";
                case BossGoal.NoMovement: return "이동 카드 금지";
                default: return "코스트 0~1 사용";
            }
        }
        public static string ResponseText(BossPhaseDef phase)
        {
            if (IsRestriction(phase.goal)) return GoalText(phase.goal, phase.goalAmount) + " · 수동 카드 최소 " + RestrictionMinimumCards + "장 사용 후 턴 종료 시 판정";
            return GoalText(phase.goal, phase.goalAmount) + (phase.fallbackAmount > 0 ? " 또는 " + GoalText(phase.fallbackGoal, phase.fallbackAmount) : "");
        }
        public static string ShortResponseText(BossPhaseDef phase)
        {
            if (IsRestriction(phase.goal)) return (phase.goal == BossGoal.NoHeavyHit ? "카드당 직접 피해 " + Math.Max(1, phase.goalAmount) + " 미만" : GoalLabel(phase.goal)) + " · 카드 " + RestrictionMinimumCards + "장";
            return GoalLabel(phase.goal) + " " + phase.goalAmount + (phase.fallbackAmount > 0 ? " 또는 " + GoalLabel(phase.fallbackGoal) + " " + phase.fallbackAmount : "");
        }
        public static int Amount(BossEffect effect, int amount)
        {
            int cap = effect == BossEffect.Strike ? 20 : effect == BossEffect.DelayedStrike ? 16
                : effect == BossEffect.Shield ? 18 : effect == BossEffect.Heal ? 12 : effect == BossEffect.Counter ? MaxCounter
                : effect == BossEffect.Evasion ? 20 : effect == BossEffect.ResourcePrepare ? 2 : 1;
            return effect == BossEffect.None ? 0 : Math.Max(1, Math.Min(cap, amount));
        }
        public static string EffectText(BossEffect effect, int amount, BossPhaseDef phase)
        {
            amount = Amount(effect, amount);
            switch (effect)
            {
                case BossEffect.Strike: return "즉시 공격 기본 피해 " + amount + " (방어·회피 가능)";
                case BossEffect.Shield: return "보스 방어도 +" + amount;
                case BossEffect.Heal: return "보스 체력 " + amount + " 회복";
                case BossEffect.DelayedStrike: return "다음 플레이어 턴 이후의 적 턴에 폭발 기본 피해 " + amount + " (방어·회피 가능)";
                case BossEffect.Prepare: return "다음 적 턴의 사용 코스트 +1";
                case BossEffect.Status: return "플레이어에게 " + StatusMechanics.Name(phase.statusKey) + " 1턴 부여";
                case BossEffect.Counter: return "다음 플레이어 턴 첫 공격 적중에 반격 기본 피해 " + amount + " (1회, 방어 가능)";
                case BossEffect.Drain: return "다음 플레이어 턴의 사용 가능한 코스트 -1 (최소 1, 최대치 유지)";
                case BossEffect.Evasion: return "다음 플레이어 턴 동안 보스 회피 보너스 " + amount + "% (다른 회피 보너스와 큰 값만 적용)";
                case BossEffect.ResourcePrepare: return (SkillMechanics.IsState(GameDatabase.Card(phase.signatureCardId)?.owner, phase.resourceKey) ? "다음 플레이어 턴 시작에 " + phase.resourceLabel + " 활성화" : "다음 플레이어 턴 시작에 " + (phase.resourceLabel ?? phase.resourceKey) + " +" + amount + " (원본 상한 유지)");
                case BossEffect.Cleanse: return "보스의 중독·약화·취약과 해로운 상태이상 해제";
                default: return "";
            }
        }
        public static string EffectsText(BossPhaseDef phase)
        {
            return string.Join(" · ", new[] { EffectText(phase.effect, phase.effectAmount, phase), EffectText(phase.secondaryEffect, phase.secondaryAmount, phase) }.Where(x => x.Length > 0));
        }
        public static string ShortEffectsText(BossPhaseDef phase)
        {
            Func<BossEffect, int, string> shortText = (effect, amount) =>
            {
                amount = Amount(effect, amount);
                switch (effect)
                {
                    case BossEffect.Strike: return "공격 " + amount;
                    case BossEffect.Shield: return "방어 +" + amount;
                    case BossEffect.Heal: return "회복 " + amount;
                    case BossEffect.DelayedStrike: return "다음 턴 폭발 " + amount;
                    case BossEffect.Prepare: return "다음 적 코스트 +1";
                    case BossEffect.Status: return StatusMechanics.Name(phase.statusKey) + " 1턴";
                    case BossEffect.Counter: return "첫 공격 반격 " + amount;
                    case BossEffect.Drain: return "다음 내 코스트 -1";
                    case BossEffect.Evasion: return "회피 보너스 " + amount + "%";
                    case BossEffect.ResourcePrepare: return (phase.resourceLabel ?? phase.resourceKey) + (SkillMechanics.IsState(GameDatabase.Card(phase.signatureCardId)?.owner, phase.resourceKey) ? " 활성화" : " +" + amount);
                    case BossEffect.Cleanse: return "해로운 효과 정화";
                    default: return "";
                }
            };
            return string.Join(" · ", new[] { shortText(phase.effect, phase.effectAmount), shortText(phase.secondaryEffect, phase.secondaryAmount) }.Where(x => x.Length > 0));
        }
        public static BossGuideView Guide(string subjectId)
        {
            var profile = Profile(subjectId); if (profile == null) return null;
            var character = GameDatabase.Character(subjectId);
            return new BossGuideView
            {
                title = (character?.name ?? subjectId) + " / " + profile.title,
                mechanicDescription = profile.overview + "\n2턴부터 특수행동을 준비합니다. 표시된 대응 조건을 충족하면 중단할 수 있습니다.",
                phaseRules = string.Join("\n\n", profile.phases.Select((phase, index) => (index + 1) + "페이즈 · " + phase.name + (index == 0 ? " (전투 시작)" : index == 1 ? " (체력 50% 이하에서 다음 내 턴 적용)" : " (체력 25% 이하에서 다음 내 턴 적용)") + "\n" + phase.tell + "\n특수행동 간격: " + Math.Max(2, Math.Min(3, phase.interval)) + "턴 · 우선 기술: " + (GameDatabase.Card(phase.signatureCardId)?.name ?? phase.signatureCardId))),
                counterplay = string.Join("\n\n", profile.phases.Select((phase, index) => (index + 1) + "페이즈 · " + ResponseText(phase) + "\n" + phase.response)) + "\n\n대응에 성공하면 이번에 준비 중인 특수행동이 중단됩니다. 이미 예약된 폭발과 효과는 유지됩니다. 보스는 다음 플레이어 턴 종료까지 취약해져 공격으로 받는 피해가 1.5배가 됩니다. 일반 취약과 중첩되지 않습니다."
                    + (profile.phases.Any(phase => phase.goal == BossGoal.MixedKeys) ? "\n서로 다른 키는 ATK·DEF·Q·W·E·R·D·F를 각각 한 종류로 셉니다." : "")
                    + (profile.phases.Any(phase => IsRestriction(phase.goal)) ? "\n금지 조건은 예고가 활성화된 턴에 수동 카드를 최소 " + RestrictionMinimumCards + "장 사용하고 끝까지 위반하지 않아야 성공합니다. 턴 종료 시 판정하며, 한 번 위반하면 그 턴에는 되돌릴 수 없습니다. 횟수 미달과 위반 모두 표시된 특수행동으로 이어집니다." : "")
                    + (profile.phases.Any(phase => phase.goal == BossGoal.DebuffSkill || phase.goal == BossGoal.NoDebuffSkill) ? "\n디버프 기술은 해로운 효과가 정의된 Q·W·E·R·D·F 카드입니다. 조건부·예약된 효과도 포함하며, 명중하거나 실제 효과가 발동했는지와 관계없이 수동 사용으로 판정합니다. 기본 공격·경계와 별도로 발동한 패시브·룬·장비 효과는 제외합니다." : "")
                    + (profile.phases.Any(phase => phase.goal == BossGoal.NoHeavyHit) ? "\n강한 공격 제한은 카드 한 번으로 실제 줄인 적 체력과 흡수된 방어도의 합으로 판정합니다. 다단 공격과 카드 본체에 붙는 보너스는 합산하고, 회피한 타격·초과 피해·지연·설치물·별도 패시브 피해는 제외합니다." : ""),
                previewEffects = string.Join("\n\n", profile.phases.Select((phase, index) => (index + 1) + "페이즈 실패 시 · " + EffectsText(phase))) + "\n\n특수행동은 적 턴의 첫 행동입니다. 직접·지연 공격 합계는 턴당 기본 피해 20, 방어·회복 합계는 24가 상한입니다. 피해는 약화·취약·상태이상의 영향을 받습니다. 페이즈는 다음 내 턴에 바뀌며 전환 자체는 공격하지 않습니다."
            };
        }
    }
}
