using System;
using System.Collections.Generic;
using System.Linq;

namespace Lumia
{
    [Serializable] public class MapNode
    {
        public int id, act, row, lane;
        public ZoneKind kind;
        public bool visited, nearKiosk;
        public string assignedOpponent, assignedAnimal;
        public bool mutantAnimal;
        public int wildlifeVariantVersion;
    }

    [Serializable] public class FreeCastGrant
    {
        public string cardId, cardReference;
        public int uses;
    }

    [Serializable] public class PendingFreeCast
    {
        public string sourceCardId, cardId;
        public int uses, readyTurn;
    }

    [Serializable] public class CombatAction
    {
        public int id;
        public string cardId, cardReference, upgradeLabel;
        public string kind;
        public string traitId;
        public bool enemy;
        public int damage, block, heal, avoided, missed, absorbed, destroyedBlock, companionAbsorbed;
        public bool critical, execution;
        public int criticalHits;
    }

    [Serializable] public class SkillHistory
    {
        public string owner, cardId;
    }

    [Serializable] public class DeferredDamage
    {
        public int amount, remaining=3;
    }

    [Serializable] public class CombatState
    {
        public string enemyName, enemyId, animal, lastSkillCard;
        public bool mutantAnimal;
        public int enemyLevel, enemyHp, enemyMaxHp, enemyBlock, enemyEnergy;
        public int turn, energy, block, strength, evasion, evasionTurns, poison, vulnerable, weak;
        public int enemyStrength, enemyEvasion, enemyEvasionTurns, enemyPoison, enemyVulnerable, enemyWeak;
        public int intentDamage, intentBlock, attacksPlayed, enemyActionIndex, enemyAvailableEnergy;
        public int playerOpeningEnergy, enemyOpeningEnergy;
        // Endless augment limits are saved with combat, including a paced enemy turn.
        public int augmentTurnMask, augmentKeyMask;
        public bool augmentWeaponUsed;
        public bool enemyTurn;
        public int retainIndex = -1, retainUses;
        public string retainedCardId;
        public int retainVersion = 1;
        public BossBattleState boss;
        public string enemyWeaponClass, enemyWeaponCardId, enemyTacticalCardId, enemyPassiveId;
        public List<string> enemyGear = new List<string>();
        public List<DeferredDamage> playerDeferredDamage = new List<DeferredDamage>(), enemyDeferredDamage = new List<DeferredDamage>();
        public List<FreeCastGrant> freeCasts = new List<FreeCastGrant>(), enemyFreeCasts = new List<FreeCastGrant>();
        public List<PendingFreeCast> pendingFreeCasts = new List<PendingFreeCast>(), enemyPendingFreeCasts = new List<PendingFreeCast>();
        public List<string> freeCastSources = new List<string>(), enemyFreeCastSources = new List<string>();
        public List<int> enemyPlanCosts = new List<int>();
        public List<bool> enemyPlanFreeCast = new List<bool>();
        public SkillActorState playerSkills = new SkillActorState(), enemySkills = new SkillActorState();
        public TraitActorState playerTraits = new TraitActorState(), enemyTraits = new TraitActorState();
        public StatusActorState playerStatuses = new StatusActorState(), enemyStatuses = new StatusActorState();
        public List<SkillHistory> lastSkills = new List<SkillHistory>(), enemyLastSkills = new List<SkillHistory>();
        public List<string> hand = new List<string>(), drawPile = new List<string>(), discardPile = new List<string>(), exhaustPile = new List<string>();
        public List<ChargedCard> chargedCards = new List<ChargedCard>(), enemyChargedCards = new List<ChargedCard>();
        public List<AdvancedEnemyAction> enemyAdvancedActions = new List<AdvancedEnemyAction>();
        public List<string> enemyDeck = new List<string>(), enemyDrawPile = new List<string>(), enemyDiscardPile = new List<string>(), enemyExhaustPile = new List<string>(), enemyHand = new List<string>(), enemyPlan = new List<string>();
    }

    [Serializable] public class RewardPrice
    {
        public string cardId;
        public int cost;
    }

    public enum RewardSource { Unknown, Wildlife, Subject, Boss }

    [Serializable] public class RewardState
    {
        public int xp, credits, cardBudget;
        public string objectId, foodId;
        public bool boss, cardsCommitted;
        public RewardSource source;
        public List<string> choices = new List<string>(), taken = new List<string>();
        public List<RewardPrice> legacyPrices = new List<RewardPrice>();
    }

    [Serializable] public class RunState
    {
        public int version = 2, mapRows = 12, seed, rngState, act = 1, row = -1, lane = -1, activeNodeId = -1;
        public RunMode mode = RunMode.Normal;
        public int endlessCycle, nextMapNodeId;
        public bool endlessRetired;
        public List<string> augments = new List<string>(), augmentOffers = new List<string>();
        public string augmentSelectedId;
        public int augmentOfferingCycle = -1;
        public List<int> gearSlotBonuses = new List<int> { 0, 0, 0, 0, 0 };
        public int equipmentSlotSelected = -1, equipmentExpansionCycle = -1;
        public RunStage stage = RunStage.Preparation;
        public int level = 1, xp, hp = 80, maxHp = 80, credits = 120, maxHealthBonus;
        public string mainRune, supportRune, chosenPassive, chosenEventId, pendingPassive, message;
        public int passiveRerolls = 2, draftRerolls = 2, campChoice, campActions, campRemovalsRemaining;
        // One saved seed per paid draft roll: changing the passive reweights this same roll.
        // Zero marks an older saved draft whose displayed offers must remain intact.
        public int draftSeed;
        public int weaponGrantVersion;
        public int energyGrowthVersion, maxEnergyBonus, bossVictories;
        public string draftBiasPassive;
        public bool runeChangePending;
        public bool cardUpgradePending;
        public bool rareRemovalPending, rareCopyPending;
        public int rareEncounterVersion;
        public List<int> rareOfferedActs = new List<int>();
        public string rareRevealEventId, rareRevealSourceEventId;
        public bool rareRevealSeen;
        public List<string> additionalSupportRunes = new List<string>();
        public int cardInstanceVersion, nextCardInstanceId = 1;
        public List<CardUpgradeSelection> cardUpgrades = new List<CardUpgradeSelection>();
        public List<int> scoutedActs = new List<int>(), hyperloopActs = new List<int>(), revealedNodeIds = new List<int>();
        public List<PendingEncounterFollowup> pendingEncounterFollowups = new List<PendingEncounterFollowup>();
        public List<string> completedChainEvents = new List<string>();
        public List<string> deck = new List<string>(), passives = new List<string>(), gear = new List<string>(), foods = new List<string>(), objects = new List<string>();
        public List<string> weaponGrantedCards = new List<string>();
        public List<string> passiveOffers = new List<string>(), draftOffers = new List<string>(), draftSelected = new List<string>(), upgrades = new List<string>();
        public List<string> encounterOffers = new List<string>(), defeatedBosses = new List<string>(), log = new List<string>();
        // Legacy representative/selected boss history. Actual opponents live on each boss node.
        public List<string> actBosses = new List<string>();
        public int bossAssignmentVersion;
        public ResearchRunState research;
        public List<MapNode> map = new List<MapNode>();
        public List<MapNode> archivedMapNodes = new List<MapNode>();
        public StoryRunProgress story;
        public List<CombatLogEntry> combatHistory = new List<CombatLogEntry>();
        public int combatHistorySerial;
        public CombatState combat;
        public RewardState rewards;
        public TraitActorState persistentTraits = new TraitActorState();
    }

    /// <summary>Serializable, deterministic game rules. UI and persistence are deliberately external.</summary>
    public sealed partial class GameEngine
    {
        public const int RowsPerAct = 12;
        public const float PassiveSkillOfferMultiplier = 1.5f;
        public const int StartingMaxEnergy = 5;
        public const int CriticalChanceCap = 30;
        public const int EndlessCriticalChanceCap = 100;
        public const int WildlifeBasicAttackDropChance = 3;
        public const int WildlifeMeatDropChance = 50;
        public const int SecurityConsolePrice = 40;
        public const int FollowupOfferChance = 80;
        public const int RareOfferChance = 12;
        public RunState State { get; private set; }
        public readonly List<CombatAction> CombatActions = new List<CombatAction>();
        private int actionSerial;
        private bool resolvingTraits;
        public bool RewardUsesCardLimit { get { return CurrentRewardSource == RewardSource.Subject || CurrentRewardSource == RewardSource.Boss; } }
        public int RewardCardLimit { get { return RewardUsesCardLimit ? 2 : 0; } }
        public int RewardRemainingCards { get { return !RewardUsesCardLimit || State.rewards.cardsCommitted ? 0 : Math.Max(0, RewardCardLimit - (State.rewards.taken?.Count ?? 0)); } }
        public int RewardRemainingBudget { get { return State.rewards == null || RewardUsesCardLimit || State.rewards.cardsCommitted ? 0 : Math.Max(0, State.rewards.cardBudget - (State.rewards.taken == null ? 0 : State.rewards.taken.Sum(RewardCardPrice))); } }
        public int PlayerBaseEnergy { get { return StartingMaxEnergy + Math.Max(0, State.maxEnergyBonus); } }
        public int MaxEnergy { get { return PlayerBaseEnergy + (State.stage==RunStage.Combat && State.combat!=null ? TraitMechanics.EnergyBonus(State.combat.playerTraits) : 0); } }
        public int DefeatedBossCount { get { return State.bossVictories; } }
        public int DynamicCriticalChanceCap { get { return RunModes.ModeFor(State) == RunMode.Endless ? EndlessCriticalChanceCap : CriticalChanceCap; } }
        public int CritChance { get { return Math.Min(DynamicCriticalChanceCap, Math.Max(0, EquipmentSum(g => g.critChance))); } }
        public int EnemyCritChance { get { return State.combat == null || !string.IsNullOrEmpty(State.combat.animal) ? 0 : Math.Min(DynamicCriticalChanceCap, Math.Max(0, EnemyEquipmentSum(g => g.critChance))); } }
        public int NextLevelXp { get { return 50 + State.level * 12; } }
        public int TotalAttack { get { return EquipmentSum(g => g.attack); } }
        public int TotalBlock { get { return EquipmentSum(g => g.block); } }
        public int RetainAvailable { get { return State.combat == null ? 0 : Math.Max(0, 2 - State.combat.retainUses); } }
        public EventDef CurrentEncounter { get { return GameDatabase.Event(State.chosenEventId); } }
        public List<EventDef> CurrentEncounterOffers { get { return State.encounterOffers.Select(GameDatabase.Event).Where(x => x != null).ToList(); } }
        public string PendingFollowupDescription { get { var next = NextPromisedEncounter(); return next == null ? "" : "다음 약속: " + next.owner + " · 다음 조우 등장 확률 80% · 남은 약속 " + State.pendingEncounterFollowups.Count + "건. 다른 조우를 선택해도 유지됩니다."; } }
        public bool CanUseSecurityConsole { get { return State.stage == RunStage.Map && State.credits >= SecurityConsolePrice && !State.scoutedActs.Contains(State.act) && ScoutNodes().Any(n => (n.kind == ZoneKind.Subject || n.kind == ZoneKind.Wildlife) && !State.revealedNodeIds.Contains(n.id)); } }
        public bool CanUseHyperloop { get { return State.stage == RunStage.Map && !State.hyperloopActs.Contains(State.act) && AvailableHyperloopNodes().Count > 0; } }
        public bool IsBossCombat { get { return State.stage == RunStage.Combat && State.combat?.boss != null; } }
        public BossGuideView BossGuideFor(string subjectId) { return BossMechanics.Guide(subjectId); }
        public BossCombatView CurrentBossView
        {
            get
            {
                var c = State.combat; if (!IsBossCombat) return null;
                var b = c.boss; var phase = BossMechanics.Phase(c); if (phase == null) return null;
                bool restricted = BossMechanics.IsRestriction(phase.goal);
                bool endTurnSuccess = restricted && !b.interrupted && BossMechanics.PreviewAtTurnEnd(c).interrupted;
                string progress = restricted ? (!b.challengeActive ? "다음 예고 대기" : b.interrupted ? "준수 완료" : b.violated ? "위반" : c.enemyTurn ? "사용 횟수 미달" : "준수 중") + " · 수동 카드 " + Math.Min(b.progress, BossMechanics.RestrictionMinimumCards) + "/" + BossMechanics.RestrictionMinimumCards + (!c.enemyTurn && b.challengeActive && !b.interrupted && !b.violated ? " · 턴 종료 판정" : "")
                    : BossMechanics.GoalLabel(phase.goal) + " " + Math.Min(b.progress, Math.Max(1, phase.goalAmount)) + "/" + Math.Max(1, phase.goalAmount);
                if (!restricted && phase.fallbackAmount > 0) progress += " 또는 " + BossMechanics.GoalLabel(phase.fallbackGoal) + " " + Math.Min(b.fallbackProgress, phase.fallbackAmount) + "/" + phase.fallbackAmount;
                string action = b.challengeActive ? b.interrupted ? "특수행동 중단 · 다음 내 턴까지 보스 취약" : "이번 적 턴 · " + BossMechanics.ShortEffectsText(phase)
                    : b.nextMechanicTurn + "턴에 특수행동 예고";
                if (b.challengeActive && endTurnSuccess) action = "턴 종료 시 중단 예정 · 제한 유지";
                if (c.enemyTurn && b.specialResolved && b.challengeTurn == c.turn) action = b.interrupted ? "특수행동 중단 완료 · 다음 내 턴까지 보스 취약" : "이번 특수행동 처리 완료";
                string details = b.challengeActive && !b.interrupted ? BossMechanics.EffectsText(phase) : "";
                if (restricted && b.challengeActive && !b.interrupted)
                {
                    string condition = b.violated ? "제한 위반: " + (GameDatabase.Card(b.violationCardId)?.name ?? b.violationCardId ?? "카드")
                        + (phase.goal == BossGoal.NoHeavyHit ? " · 직접 피해 " + b.violationDamage + " / 금지 기준 " + Math.Max(1, phase.goalAmount) : " 사용") + ". 이번 턴에는 되돌릴 수 없습니다."
                        : endTurnSuccess ? "지금 턴을 종료하면 제한 준수에 성공합니다. 추가로 사용하는 카드도 제한을 지켜야 합니다."
                        : c.enemyTurn ? "수동 카드 사용 횟수가 " + BossMechanics.RestrictionMinimumCards + "장보다 적어 대응에 실패했습니다."
                        : "제한을 지키며 수동 카드를 최소 " + BossMechanics.RestrictionMinimumCards + "장 사용해야 합니다. 지금 턴을 종료하면 사용 횟수 미달입니다.";
                    details = condition + "\n실패 시: " + details;
                }
                int attackForecast = BossSpecialDamageForecast();
                if (attackForecast > 0) details += "\n이번 적 턴의 특수공격 예상 피해: " + attackForecast + " (현재 보정 반영, 방어·회피 전)";
                if (b.interrupted && b.delayedDamage > 0) details += "\n이번 준비는 중단했지만, 이미 예약된 폭발은 유지됩니다.";
                if (b.delayedDamage > 0) { action += " · " + b.delayedDueTurn + "턴 폭발 " + b.delayedDamage; details += "\n" + b.delayedDueTurn + "턴 적의 첫 행동에 지연 공격 기본 피해 " + b.delayedDamage + ". 방어·회피로 대응할 수 있습니다."; }
                if (b.counterDamage > 0 && c.turn <= b.counterUntilTurn) { action += " · 첫 공격 반격 " + b.counterDamage; details += "\n이번 플레이어 턴의 첫 공격 적중에 반격 예상 피해 " + BossStrikeDamage(b.counterDamage, c.enemyWeak, c.vulnerable, c.enemyStatuses, c.playerStatuses) + "가 한 번 발동합니다. 방어할 수 있습니다."; }
                return new BossCombatView { phase = b.phase, pendingPhase = b.pendingPhase, phaseName = phase.name,
                    pendingPhaseText = b.pendingPhase > b.phase ? b.pendingPhase + "페이즈 전환 예약 · 다음 내 턴에 적용" : "",
                    challengeActive = b.challengeActive, interrupted = b.interrupted, restricted = restricted, violated = b.challengeActive && b.violated,
                    needsTurnEnd = restricted && !c.enemyTurn && b.challengeActive && !b.interrupted, violationCardId = b.challengeActive ? b.violationCardId : null, violationDamage = b.challengeActive ? b.violationDamage : 0,
                    forbiddenDamageThreshold = phase.goal == BossGoal.NoHeavyHit ? Math.Max(1, phase.goalAmount) : 0,
                    restrictionText = restricted ? (phase.goal == BossGoal.NoHeavyHit ? "카드당 직접 피해 " + Math.Max(1, phase.goalAmount) + " 이상 금지" : BossMechanics.GoalLabel(phase.goal)) : "",
                    progress = b.progress, goalAmount = restricted ? BossMechanics.RestrictionMinimumCards : phase.goalAmount,
                    fallbackProgress = b.fallbackProgress, fallbackAmount = restricted ? 0 : phase.fallbackAmount, nextTurn = b.challengeActive ? c.turn : b.nextMechanicTurn,
                    progressText = progress, nextCondition = phase.name + " · " + BossMechanics.ShortResponseText(phase), nextAction = action,
                    nextActionDetails = details, guide = BossGuideFor(c.enemyId) };
            }
        }
        public int Accuracy { get { return AccuracyMechanics.Accuracy(State.combat?.playerStatuses); } }
        public int EnemyAccuracy { get { return AccuracyMechanics.Accuracy(State.combat?.enemyStatuses); } }
        public int Evasion { get { return AccuracyMechanics.Evasion(Math.Min(65, EquipmentSum(g => g.evasion) + Modifier("evade_bonus") + (State.combat == null ? 0 : State.combat.evasion + Modifier("battle_start_evasion") + TraitMechanics.EvasionBonus(State.combat.playerTraits))), State.combat?.playerStatuses); } }
        public int EnemyEvasion { get { var c=State.combat;return c==null?0:AccuracyMechanics.Evasion(c.enemyEvasion>=100?100:Math.Min(65,c.enemyEvasion+TraitMechanics.EvasionBonus(c.enemyTraits)),c.enemyStatuses); } }
        public string EnemyIntent
        {
            get
            {
                var c = State.combat;
                if (c == null) return "";
                RefreshEnemyIntent();
                var names = c.enemyPlan.Select(EnemyActionCard).Where(x => x != null).Select(x => x.name);
                string bossIntent = c.boss != null && BossMechanics.NeedsSpecialAction(c, BossMechanics.PreviewAtTurnEnd(c)) ? " · 보스 특수행동 포함" : "";
                return string.Join(" · ", names) + "  / 피해 " + c.intentDamage + " · 방어 " + c.intentBlock + " · 코스트 " + c.enemyPlanCosts.Sum() + "/" + c.enemyEnergy + (c.enemyPlan.Any(id => GameDatabase.Card(id) != null && GameDatabase.Card(id).freeCastOnHit) ? " · 적중 시 연계 포함" : "") + bossIntent;
            }
        }

        public GameEngine(int seed) : this(seed, RunMode.Normal) { }

        public GameEngine(int seed, RunMode mode)
        {
            State = new RunState { seed = seed, rngState = seed == 0 ? 1831565813 : seed, mode = RunModes.Normalize(mode), weaponGrantVersion=1, energyGrowthVersion=1, cardInstanceVersion=1, rareEncounterVersion=2 };
            ResearchProgression.EnsureRun(State);
            GenerateMap();
            EnsureBossAssignments();
            State.passiveOffers = Offer(GameDatabase.Passives.Select(x => x.id), 3);
            RollStartingDraft();
            for (int i = 0; i < 5; ++i) State.deck.Add("basic_attack");
            for (int i = 0; i < 3; ++i) State.deck.Add("basic_guard");
            Say("니아의 VF 속으로 진입했습니다. 룬, 패시브, 시작 카드를 선택하세요.");
        }

        public GameEngine(RunState savedState)
        {
            if (savedState == null || savedState.version < 1 || savedState.version > 2) throw new ArgumentException("지원하지 않는 저장 데이터입니다.");
            State = savedState;
            EnsureCombatInformation();
            State.mode = RunModes.ModeFor(State);
            State.endlessCycle = State.mode == RunMode.Endless ? Math.Max(0, State.endlessCycle) : 0;
            if (State.mode != RunMode.Endless) State.endlessRetired = false;
            if (State.archivedMapNodes == null) State.archivedMapNodes = new List<MapNode>();
            ResearchProgression.EnsureRun(State, migrateExisting: true);
            if (State.rewards != null)
            {
                if (State.rewards.choices == null) State.rewards.choices = new List<string>();
                if (State.rewards.taken == null) State.rewards.taken = new List<string>();
                if (State.rewards.legacyPrices == null) State.rewards.legacyPrices = new List<RewardPrice>();
            }
            if (State.cardUpgrades == null) State.cardUpgrades = new List<CardUpgradeSelection>();
            if (State.scoutedActs == null) State.scoutedActs = new List<int>();
            if (State.hyperloopActs == null) State.hyperloopActs = new List<int>();
            if (State.revealedNodeIds == null) State.revealedNodeIds = new List<int>();
            if (State.pendingEncounterFollowups == null) State.pendingEncounterFollowups = new List<PendingEncounterFollowup>();
            if (State.completedChainEvents == null) State.completedChainEvents = new List<string>();
            State.pendingEncounterFollowups.RemoveAll(x => x == null || GameDatabase.Event(x.eventId) == null || State.completedChainEvents.Contains(x.eventId));
            if (State.stage != RunStage.Encounter) State.cardUpgradePending = false;
            State.persistentTraits=TraitMechanics.Ensure(State.persistentTraits);
            if (State.rngState == 0) State.rngState = 1831565813;
            if (State.level < 1 || State.level > 20 || State.map == null || State.deck == null) throw new ArgumentException("손상된 저장 데이터입니다.");
            State.nextMapNodeId = Math.Max(State.nextMapNodeId, State.map.Concat(State.archivedMapNodes).Select(n => n.id).DefaultIfEmpty(-1).Max() + 1);
            // Old saves never stayed at the campfire after resting, so an absent budget remains zero.
            State.campRemovalsRemaining = State.stage == RunStage.Campfire && State.campChoice == 1
                ? Math.Max(0, Math.Min(2, State.campRemovalsRemaining)) : 0;
            if (State.defeatedBosses == null) State.defeatedBosses = new List<string>();
            if (State.energyGrowthVersion == 0)
            {
                // Existing victories grant the new permanent boss bonus once; levels grant no energy.
                State.bossVictories = Math.Max(State.defeatedBosses.Count, Math.Max(0, State.act - 1));
                State.maxEnergyBonus = State.bossVictories;
                State.energyGrowthVersion = 1;
            }
            RefreshMapProximity();
            EnsureWildlifeAssignments();
            State.mapRows = State.map.Count > 0 ? State.map.Max(n => n.row) + 1 : RowsPerAct;
            EnsureBossAssignments();
            if (State.version == 1)
            {
                // Old saves committed rewards on click. Preserve the choice, but undo only those appended copies.
                if (State.stage == RunStage.Rewards && State.rewards != null && !State.rewards.cardsCommitted)
                {
                    var r = State.rewards;
                    r.cardBudget += r.taken.Sum(LegacyRewardCardPrice);
                    r.legacyPrices = r.choices.Select(id => new RewardPrice { cardId = id, cost = LegacyRewardCardPrice(id) }).ToList();
                    foreach (var id in r.taken)
                    {
                        int index = State.deck.LastIndexOf(id);
                        if (index >= 0) State.deck.RemoveAt(index);
                    }
                }
                if (State.combat != null)
                {
                    // Version 1 kept planned enemy cards outside its piles until the enemy turn ended.
                    // Version 2 resolves that pile movement while planning so saved paced turns remain finite.
                    var c = State.combat;
                    foreach (var id in c.enemyPlan)
                    {
                        var card = GameDatabase.Card(id);
                        if (card == null) continue;
                        if (card.exhaust) c.enemyExhaustPile.Add(id); else c.enemyDiscardPile.Add(id);
                    }
                    c.enemyDiscardPile.AddRange(c.enemyHand); c.enemyHand.Clear();
                }
                State.version = 2;
            }
            if (State.rewards != null)
            {
                // Older saves did not distinguish an animal's budget from a subject's reward.
                // Recover the actual source before migrating only the unconfirmed selection.
                State.rewards.source = CurrentRewardSource;
                if (State.stage == RunStage.Rewards && !State.rewards.cardsCommitted && RewardUsesCardLimit && State.rewards.taken.Count > RewardCardLimit)
                    State.rewards.taken = State.rewards.taken.Take(RewardCardLimit).ToList();
            }
            if (State.combat != null)
            {
                var c = State.combat;
                if (c.retainVersion == 0) { c.retainIndex = -1; c.retainedCardId = null; c.retainUses = 0; c.retainVersion = 1; }
                c.retainUses = Math.Max(0, Math.Min(2, c.retainUses));
                if (c.enemyTurn || c.retainIndex < 0 || c.retainIndex >= c.hand.Count || c.retainUses >= 2) c.retainIndex = -1;
                if (c.lastSkills == null) c.lastSkills = new List<SkillHistory>();
                if (c.enemyLastSkills == null) c.enemyLastSkills = new List<SkillHistory>();
                if (c.freeCasts == null) c.freeCasts = new List<FreeCastGrant>();
                if (c.freeCastSources == null) c.freeCastSources = new List<string>();
                if (c.enemyFreeCasts == null) c.enemyFreeCasts = new List<FreeCastGrant>();
                if (c.enemyFreeCastSources == null) c.enemyFreeCastSources = new List<string>();
                if (c.pendingFreeCasts == null) c.pendingFreeCasts = new List<PendingFreeCast>();
                if (c.enemyPendingFreeCasts == null) c.enemyPendingFreeCasts = new List<PendingFreeCast>();
                FreeCastMechanics.SanitizePending(c.pendingFreeCasts);
                FreeCastMechanics.SanitizePending(c.enemyPendingFreeCasts);
                if (c.enemyPlanCosts == null) c.enemyPlanCosts = new List<int>();
                if (c.enemyPlanCosts.Count != c.enemyPlan.Count) c.enemyPlanCosts = c.enemyPlan.Select(id => WildlifeMechanics.IsAnimalSkill(id) ? 0 : CardCost(id)).ToList();
                if (c.enemyPlanFreeCast == null || c.enemyPlanFreeCast.Count != c.enemyPlan.Count)
                    c.enemyPlanFreeCast = c.enemyPlan.Select((id, i) => !WildlifeMechanics.IsAnimalSkill(id) && c.enemyPlanCosts[i] == 0 && CardCost(id) > 0).ToList();
                c.playerSkills = SkillMechanics.Ensure(c.playerSkills); c.enemySkills = SkillMechanics.Ensure(c.enemySkills);
                EnsureAdvancedCombat();
                c.playerTraits=TraitMechanics.Ensure(c.playerTraits);c.enemyTraits=TraitMechanics.Ensure(c.enemyTraits);
                c.playerStatuses=StatusMechanics.Ensure(c.playerStatuses);c.enemyStatuses=StatusMechanics.Ensure(c.enemyStatuses);
                if(c.enemyGear==null)c.enemyGear=new List<string>();
                if(c.playerDeferredDamage==null)c.playerDeferredDamage=new List<DeferredDamage>();
                if(c.enemyDeferredDamage==null)c.enemyDeferredDamage=new List<DeferredDamage>();
                State.persistentTraits=c.playerTraits;
                bool bossNode = ActiveNode()?.kind == ZoneKind.Boss && string.IsNullOrEmpty(c.animal) && BossMechanics.Profile(c.enemyId) != null;
                if (bossNode)
                {
                    bool legacyBoss = c.boss == null;
                    bool legacyChallenge = c.boss != null && c.boss.version < 2;
                    c.boss = BossMechanics.Ensure(c.boss);
                    if (legacyBoss) { c.boss.nextMechanicTurn = Math.Max(2, c.turn + 1); c.boss.specialResolved = c.enemyTurn; }
                    var phase = BossMechanics.Phase(c);
                    if (legacyChallenge && c.boss.challengeActive && !c.boss.interrupted && phase != null
                        && (BossMechanics.IsRestriction(phase.goal) || phase.goal == BossGoal.DebuffSkill))
                    {
                        // Version 1 did not record these actions. Give a new complete turn
                        // of warning instead of judging cards played before the update.
                        c.boss.challengeActive = false; c.boss.nextMechanicTurn = Math.Max(c.turn + 1, c.boss.nextMechanicTurn);
                        c.boss.progress = c.boss.fallbackProgress = 0; c.boss.playedKeys.Clear();
                        c.boss.violated = false; c.boss.violationCardId = null; c.boss.violationDamage = 0;
                    }
                    c.boss.version = 2;
                    BossMechanics.CheckPhase(c);
                }
                else c.boss = null;
            }
            CardInstances.NormalizeLegacy(State);
            NormalizeRareEncounters();
            // Equipment grants synchronize between nodes; instance migration preserves existing piles.
            EnsureEndlessAugments();
            if(State.stage!=RunStage.Combat)SyncWeaponCards();
        }

        public static int EnergyForLevel(int level) { return level >= 18 ? 8 : level >= 12 ? 7 : level >= 6 ? 6 : 5; }

        public bool SetRunes(string main, string support)
        {
            if (State.rareRemovalPending || State.rareCopyPending || (State.stage != RunStage.Preparation && !State.runeChangePending)) return Fail("룬은 조우 이벤트에서만 변경할 수 있습니다.");
            var m = GameDatabase.Rune(main); var s = GameDatabase.Rune(support);
            if (m == null || s == null || !m.main || s.main || m.tree != s.tree) return Fail("메인 룬과 같은 계열의 보조 룬 1개를 선택하세요.");
            State.mainRune = main; State.supportRune = support;
            PruneUnownedTraits();
            RecalculateHealth(State.stage == RunStage.Preparation);
            return Say(m.name + " / " + s.name + " 룬을 선택했습니다.");
        }

        public bool RerollPassives()
        {
            if (State.stage != RunStage.Preparation || State.passiveRerolls <= 0) return Fail("패시브 리롤 기회를 모두 사용했습니다.");
            State.passiveRerolls--;
            State.chosenPassive = null; State.passives.Clear();
            PruneUnownedTraits();
            State.passiveOffers = Offer(GameDatabase.Passives.Select(x => x.id), 3);
            RefreshStartingDraft();
            RecalculateHealth(true);
            return Say("시작 패시브 선택지를 다시 뽑았습니다.");
        }

        public bool SelectStartingPassive(string id)
        {
            if (State.stage != RunStage.Preparation || !State.passiveOffers.Contains(id)) return Fail("현재 제시된 패시브를 선택하세요.");
            State.chosenPassive = id; State.passives.Clear(); State.passives.Add(id);
            PruneUnownedTraits();
            RefreshStartingDraft();
            RecalculateHealth(true);
            return Say(GameDatabase.Passive(id).name + " 패시브를 선택했습니다.");
        }

        public bool RerollDraft()
        {
            if (State.stage != RunStage.Preparation || State.draftRerolls <= 0) return Fail("카드 리롤 기회를 모두 사용했습니다.");
            State.draftRerolls--;
            RollStartingDraft();
            return Say("선택한 시작 카드는 유지하고 나머지 선택지를 다시 뽑았습니다.");
        }

        public bool ToggleDraft(string id)
        {
            if (State.stage != RunStage.Preparation || !State.draftOffers.Contains(id)) return Fail("현재 제시된 시작 카드를 선택하세요.");
            if (State.draftSelected.Contains(id)) State.draftSelected.Remove(id);
            else if (State.draftSelected.Count < 3) State.draftSelected.Add(id);
            else return Fail("시작 카드는 3장까지 선택할 수 있습니다.");
            return true;
        }

        public bool BeginJourney()
        {
            if (State.stage != RunStage.Preparation) return Fail("이미 탐사를 시작했습니다.");
            if (string.IsNullOrEmpty(State.mainRune) || string.IsNullOrEmpty(State.supportRune) || string.IsNullOrEmpty(State.chosenPassive) || State.draftSelected.Count != 3)
                return Fail("메인·보조 룬, 패시브 1개, 카드 3장을 모두 선택하세요.");
            State.deck.AddRange(State.draftSelected); State.stage = RunStage.Map;
            ResearchProgression.Begin(State);
            return Say("하나: 연구소의 출구를 찾아야 해. 니아, 길을 보여 줘.");
        }

        public bool SetResearchUnlocks(IEnumerable<string> subjectIds)
        {
            if (State.stage != RunStage.Preparation) return false;
            ResearchProgression.EnsureRun(State);
            State.research.unlockedReports = (subjectIds ?? Enumerable.Empty<string>())
                .Where(id => GameDatabase.Character(id) != null).Distinct().ToList();
            return true;
        }

        public List<MapNode> AvailableNodes()
        {
            if (State.stage != RunStage.Map) return new List<MapNode>();
            return State.map.Where(n => !n.visited && n.act == State.act && n.row == State.row + 1 && (State.lane < 0 || Math.Abs(n.lane - State.lane) <= 1)).ToList();
        }

        public List<MapNode> AvailableHyperloopNodes()
        {
            if (State.stage != RunStage.Map || State.lane < 0 || State.hyperloopActs.Contains(State.act)) return new List<MapNode>();
            return State.map.Where(n => !n.visited && n.act == State.act && n.row == State.row + 1 && Math.Abs(n.lane - State.lane) > 1).ToList();
        }

        private IEnumerable<MapNode> ScoutNodes()
        {
            return State.map.Where(n => !n.visited && n.act == State.act && n.row > State.row && n.row <= State.row + 2
                && (n.kind == ZoneKind.Subject || n.kind == ZoneKind.Wildlife || n.kind == ZoneKind.Kiosk));
        }

        public bool UseSecurityConsole()
        {
            if (!CanUseSecurityConsole) return Fail("보안 콘솔은 구역마다 한 번, 40 크레딧으로 앞으로 두 줄의 정보를 확인합니다.");
            foreach (var node in ScoutNodes())
            {
                AssignScoutedOpponent(node);
                if (!State.revealedNodeIds.Contains(node.id)) State.revealedNodeIds.Add(node.id);
            }
            State.credits -= SecurityConsolePrice; State.scoutedActs.Add(State.act);
            return Say("보안 콘솔 · 앞으로 두 줄의 실험체·야생동물과 키오스크 재고를 확인했습니다.");
        }

        // Scouting never advances battle RNG. A paid reveal commits its own stable opponent assignment.
        private void AssignScoutedOpponent(MapNode node)
        {
            int random = unchecked(State.seed ^ (node.id + 1) * (int)0x9E3779B9 ^ (int)0x51C0A7E1);
            if (random == 0) random = 1831565813;
            if (node.kind == ZoneKind.Wildlife && string.IsNullOrEmpty(node.assignedAnimal))
            {
                var animals = new[] { "chicken", "dog", "boar", "wolf", "bear" };
                node.assignedAnimal = animals[Next(ref random, animals.Length)];
                AssignWildlifeVariant(node);
            }
            if (node.kind == ZoneKind.Subject && string.IsNullOrEmpty(node.assignedOpponent))
            {
                var roster = GameDatabase.Characters.Where(x => x.cards != null && x.cards.Length > 0).ToList();
                node.assignedOpponent = roster[Next(ref random, roster.Count)].id;
            }
        }

        public string NodeIntel(MapNode node)
        {
            if (node == null || !State.revealedNodeIds.Contains(node.id)) return "";
            if (node.kind == ZoneKind.Subject) return GameDatabase.Character(node.assignedOpponent)?.name ?? "실험체";
            if (node.kind == ZoneKind.Wildlife)
            {
                var animals = new[] { "chicken", "dog", "boar", "wolf", "bear" };
                var names = new[] { "닭", "들개", "멧돼지", "늑대", "곰" };
                int index = Array.IndexOf(animals, node.assignedAnimal);
                return (IsMutantWildlife(node) ? "변이 " : "") + (index >= 0 ? names[index] : "야생동물");
            }
            if (node.kind == ZoneKind.Kiosk)
            {
                var stock = GameDatabase.Objects.Select(item => item.name + (IsKioskObjectUnlocked(item.id) ? " " + ObjectPrice(item.id) + " CR" : " · 잠김"));
                var foods = KioskFoods.Select(food => food.name + " " + FoodPrice(food.id) + " CR");
                return "오브젝트\n" + string.Join(" · ", stock) + "\n\n식재료\n" + string.Join(" · ", foods);
            }
            return "";
        }

        public bool EnterHyperloopNode(int lane)
        {
            var node = AvailableHyperloopNodes().FirstOrDefault(n => n.lane == lane);
            if (node == null) return Fail("하이퍼루프는 같은 다음 줄에서 평소 연결되지 않은 구역으로 한 번 이동할 수 있습니다.");
            State.hyperloopActs.Add(State.act);
            return EnterMapNode(node);
        }

        public bool EnterNode(int lane)
        {
            RefreshMapProximity();
            var node = AvailableNodes().FirstOrDefault(n => n.lane == lane);
            if (node == null) return Fail("연결된 다음 구역만 이동할 수 있습니다.");
            return EnterMapNode(node);
        }

        private bool EnterMapNode(MapNode node)
        {
            RefreshMapProximity();
            State.activeNodeId = node.id; State.combat = null; State.rewards = null;
            State.pendingPassive = null; State.runeChangePending = false; State.chosenEventId = null;
            State.cardUpgradePending = false;
            ClearRarePending();
            switch (node.kind)
            {
                case ZoneKind.Wildlife: StartBattle(true, false); break;
                case ZoneKind.Subject: StartBattle(false, false); break;
                case ZoneKind.Boss: StartBattle(false, true); break;
                case ZoneKind.Kiosk: State.stage = RunStage.Kiosk; Say("키오스크에 도착했습니다. 크레딧으로 오브젝트와 음식을 구매하세요."); break;
                case ZoneKind.Campfire:
                    State.stage = RunStage.Campfire; State.campChoice = 0; State.campActions = 3; State.campRemovalsRemaining = 0;
                    Say("휴식하면 체력을 모두 회복하고 만년 스프를 받으며 카드를 최대 2장 제거할 수 있습니다. 제작·요리를 선택하면 회복 없이 최대 3번 작업합니다."); break;
                case ZoneKind.Encounter:
                    State.stage = RunStage.Encounter; RollEncounterOffers();
                    Say("세 실험체의 흔적을 발견했습니다. 하나의 조우를 선택하세요."); break;
            }
            return true;
        }

        public int CardCost(string id) { var c = GameDatabase.Card(id); return c == null ? 99 : Math.Max(0, c.cost); }
        public bool SkipCardUpgrade()
        {
            if (State.stage != RunStage.Encounter || !State.cardUpgradePending || State.rareRemovalPending || State.rareCopyPending) return false;
            State.cardUpgradePending = false; Say("이번 카드 강화를 포기했습니다."); CompleteNode(); return true;
        }
        public int EffectiveCardCost(string id)
        {
            var c = State.combat;
            var card = PlayerCard(id);
            string skillId = CardInstances.BaseId(id);
            if (IsCharged(id)) return 0;
            if (c != null && AdvancedCardMechanics.IsX(card)) return c.freeCasts.Any(g => FreeCastMechanics.Matches(g,id)) ? 0 : Math.Max(0,c.energy);
            return c != null && c.freeCasts.Any(g => FreeCastMechanics.Matches(g,id)) ? 0 : Math.Max(0, (card == null ? 99 : card.cost) + (c==null?0:StatusMechanics.CostPenalty(c.playerStatuses,card)) - (c == null ? 0 : Math.Max(SkillMechanics.Discount(c.playerSkills,skillId),SkillMechanics.Discount(c.playerTraits.skills,skillId))));
        }
        public int EnemyEffectiveCardCost(string id)
        {
            var c = State.combat;
            if(WildlifeMechanics.IsAnimalSkill(id))return 0;
            if(c != null && AdvancedCardMechanics.IsX(GameDatabase.Card(id))) return c.enemyFreeCasts.Any(g => g.cardId == id && g.uses > 0) ? 0 : Math.Max(0,c.enemyAvailableEnergy);
            return c != null && c.enemyFreeCasts.Any(g => g.cardId == id && g.uses > 0) ? 0 : Math.Max(0, CardCost(id) + (c==null?0:StatusMechanics.CostPenalty(c.enemyStatuses,GameDatabase.Card(id))) - (c == null ? 0 : Math.Max(SkillMechanics.Discount(c.enemySkills,id),SkillMechanics.Discount(c.enemyTraits.skills,id))));
        }
        public List<SkillMechanicToken> SkillStateSnapshot(bool enemy = false)
        {
            var tokens=SkillMechanics.Snapshot(State.combat == null ? null : enemy ? State.combat.enemySkills : State.combat.playerSkills);
            if(State.combat!=null) tokens.AddRange(TraitMechanics.Snapshot(enemy?State.combat.enemyTraits:State.combat.playerTraits));
            if(State.combat!=null)tokens.AddRange(StatusMechanics.Snapshot(enemy?State.combat.enemyStatuses:State.combat.playerStatuses));
            if(State.combat!=null)
            {
                var deferred=enemy?State.combat.enemyDeferredDamage:State.combat.playerDeferredDamage;
                if(deferred.Count>0)tokens.Add(new SkillMechanicToken{owner="gear:ao_dai",key="deferred_damage",label="유예 피해",kind="deferred_damage",sourceCard="ao_dai",amount=deferred.Sum(x=>x.amount),remaining=deferred.Max(x=>x.remaining)});
                tokens.AddRange(CompanionMechanics.Snapshot(enemy?State.combat.enemySkills:State.combat.playerSkills));
                foreach(var held in enemy ? State.combat.enemyChargedCards : State.combat.chargedCards)
                    tokens.Add(new SkillMechanicToken { owner=GameDatabase.Card(held.reference)?.owner, key="charge", label=GameDatabase.Card(held.reference)?.name+" 충전", kind="charge", sourceCard=CardInstances.BaseId(held.reference), amount=1, remaining=Math.Max(0,held.readyTurn-State.combat.turn) });
            }
            return tokens;
        }
        public string ResourceSummary(bool enemy = false)
        {
            return SkillMechanics.Summary(State.combat == null ? (SkillActorState)null : enemy ? State.combat.enemySkills : State.combat.playerSkills);
        }
        public List<int> PredictedEnemyCosts { get { return State.combat == null ? new List<int>() : State.combat.enemyPlanCosts.ToList(); } }
        public string EnemyMechanicAssumptions
        {
            get
            {
                var c = State.combat;
                if (c == null) return "";
                string text = c.enemyPlan.Any(id => GameDatabase.Card(id)?.mechanics?.rules.Any(r => r.onHit) == true) ? "추가 피해와 적중 연계는 명중을 가정합니다." : "";
                if (EnemyCritChance > 0 && c.enemyPlan.Contains("basic_attack")) text += (text.Length > 0 ? " " : "") + "피해 예측은 일반 적중 기준이며, 기본 공격 치명타 확률은 " + EnemyCritChance + "%입니다.";
                return text;
            }
        }

        public bool CanPlayCard(int handIndex)
        {
            var c = State.combat;
            return pendingCardChoice == null && State.stage == RunStage.Combat && c != null && !c.enemyTurn && handIndex >= 0 && handIndex < c.hand.Count
                && GameDatabase.Card(c.hand[handIndex]) != null && StatusMechanics.CanUse(c.playerStatuses,GameDatabase.Card(c.hand[handIndex])) && EffectiveCardCost(c.hand[handIndex]) <= c.energy;
        }

        public int CardDamage(string id)
        {
            var card = PreparedPlayerCard(id);
            if (card == null || card.damage <= 0) return 0;
            int value = card.damage + State.level / 3 + TotalAttack + (State.combat == null ? 0 : State.combat.strength);
            value += Modifier(card.category == "basic" ? "attack_bonus" : "skill_bonus");
            if (State.combat != null && State.combat.weak > 0) value = value * 3 / 4;
            if (State.combat != null && (State.combat.enemyVulnerable > 0 || BossMechanics.RewardVulnerable(State.combat))) value = value * 3 / 2;
            if (State.combat != null) value=value*(100+TraitMechanics.Exposure(State.combat.enemyTraits))/100;
            if(State.combat!=null)value=StatusMechanics.Damage(State.combat.playerStatuses,State.combat.enemyStatuses,value);
            return Math.Max(0, value);
        }
        private int PlayerBonusDamage(string id, int paidCost = -1)
        {
            var c = State.combat; var card = PreparedPlayerCard(id);
            if (c == null || card == null) return 0;
            if(AdvancedCardMechanics.SuppressesAttack(card))return 0;
            int skillDamage=SkillMechanics.Bonuses(c.playerSkills,card).damage+PlayerAdvancedBonus(card).damage,empower=TraitMechanics.EmpowerBonus(c.playerTraits,card);
            if(card.damage<=0 && skillDamage<=0 && empower<=0) return 0;
            int damage = skillDamage + PreviewBeforeTraits(false,card,c.playerSkills).bonus.damage + TraitMechanics.DamageBonus(c.playerTraits) + empower
                + AugmentAttackBonus(card, paidCost < 0 ? EffectiveCardCost(id) : paidCost);
            if (c.weak > 0) damage = damage * 3 / 4;
            if (c.enemyVulnerable > 0 || BossMechanics.RewardVulnerable(c)) damage = damage * 3 / 2;
            damage=damage*(100+TraitMechanics.Exposure(c.enemyTraits))/100;
            return StatusMechanics.Damage(c.playerStatuses,c.enemyStatuses,damage);
        }
        public int CardTotalDamage(string id)
        {
            var card = PreparedPlayerCard(id);
            return card == null ? 0 : CardDamage(id) * Math.Max(1, card.hits) + PlayerBonusDamage(id);
        }

        public bool PlayCard(int handIndex)
        {
            return PlayAdvancedCard(handIndex, null);
        }
        private bool ExecutePlayerCard(int handIndex)
        {
            var c = State.combat;
            if (!CanPlayCard(handIndex)) return Fail(c != null && c.enemyTurn ? "적의 행동이 끝나기를 기다리세요." : "카드를 사용할 코스트가 부족하거나 사용할 수 없는 손패입니다.");
            var reference = c.hand[handIndex]; var id = CardInstances.BaseId(reference); var card = PreparedPlayerCard(reference);
            var advancedBonus = PlayerAdvancedBonus(card);
            int researchActionStart = CombatActions.Count;
            int price = EffectiveCardCost(reference);
            bool intrinsicCharge=IsCharged(reference);
            bool freeGranted = !intrinsicCharge && c.freeCasts.Any(g => FreeCastMechanics.Matches(g,reference));
            bool garnetRecall = FreeCastMechanics.GarnetRecallBeforeExhaust(card, freeGranted, c.freeCastSources);
            bool garnetExecution = FreeCastMechanics.GarnetExecutionReady(card, c.playerSkills, freeGranted, c.enemyHp, c.enemyMaxHp);
            bool execution = false;
            RecordCombatCardStart(card, false, price, freeGranted, reference);
            int poisonBefore = c.enemyPoison, weakBefore = c.enemyWeak, vulnerableBefore = c.enemyVulnerable;
            int augmentEnergyBefore = c.energy;
            if(!intrinsicCharge)
            {
                ConsumeFreeCast(c.freeCasts, reference);
                SkillMechanics.ConsumeDiscount(c.playerSkills, id);
                SkillMechanics.ConsumeDiscount(c.playerTraits.skills,id);
            }
            var skillBefore = SkillMechanics.Clone(c.playerSkills);
            int traitHpBefore=State.hp;
            c.energy -= price; c.hand.RemoveAt(handIndex);
            if (c.retainIndex == handIndex) c.retainIndex = -1;
            else if (c.retainIndex > handIndex) c.retainIndex--;
            if (card.exhaust && !garnetRecall) c.exhaustPile.Add(reference); else c.discardPile.Add(reference);
            int hits = Math.Max(1, card.hits), dealt = 0, avoided = 0, missed = 0, criticalHits = 0, directDamage = 0, absorbedTotal = 0, landedHits = 0, destroyedBlock = 0;
            int bonusDamage = PlayerBonusDamage(reference, price);
            ConsumeAugmentAttack(card, price);
            var incoming=card.damage>0 || bonusDamage>0 ? TraitEvent(true,"before_incoming",card,true) : new TraitResult();int incomingReduction=incoming.damageReduction,defenderBlock=c.enemyBlock;
                bool landed = card.damage == 0 && bonusDamage == 0, bonusUsed = false;
            for (int i = 0; i < hits && c.enemyHp > 0; ++i)
            {
                if (card.damage <= 0 && bonusDamage <= 0) break;
                bool accuracyMiss;
                if (!TryDirectHit(card, false, out accuracyMiss, freeGranted))
                {
                    if (accuracyMiss) { missed++; Say(card.name + " 공격이 빗나갔습니다."); }
                    else { avoided++; Say(c.enemyName + "이(가) 공격을 회피했습니다."); }
                    continue;
                }
                landed = true;
                landedHits++;
                if (landedHits == 1 && SkillMechanics.BreaksBlock(skillBefore, card))
                {
                    destroyedBlock = c.enemyBlock; c.enemyBlock = 0;
                }
                if (garnetExecution)
                {
                    int remainingHp = c.enemyHp;
                    c.enemyHp = 0; dealt += remainingHp; directDamage += remainingHp; execution = true;
                    BossMechanics.CheckPhase(c); RecordBossProgress(BossMechanics.ObserveDamage(c, remainingHp));
                    break;
                }
                int baseDamage = CardDamage(reference);
                // Only the universal basic attack's own hit is multiplied. Skill/passive bonus pulses remain separate.
                if (id == "basic_attack" && baseDamage > 0 && Roll(CritChance)) { baseDamage = GearCriticalDamage(baseDamage); criticalHits++; }
                int damage = Math.Max(0,baseDamage + (bonusUsed ? 0 : bonusDamage) - (bonusUsed?0:incomingReduction)), absorbed = Math.Min(c.enemyBlock, damage);
                bonusUsed = true;
                c.enemyBlock -= absorbed; damage -= absorbed; absorbedTotal += absorbed;
                int healthDamage = TakeHealthDamage(true, damage); dealt += healthDamage; directDamage += absorbed + healthDamage;
                RecordBossProgress(BossMechanics.ObserveDamage(c, absorbed));
            }
            if (card.damage > 0 || bonusDamage > 0) c.attacksPlayed++;
            var bonuses = AddAdvancedBonus(SkillMechanics.Bonuses(skillBefore, card, landed), advancedBonus);
            int block = Math.Max(0, card.block) + bonuses.block, beforeHp = State.hp;
            c.block += block;
            RecordBossProgress(BossMechanics.ObserveBlock(c, block));
            if (card.heal > 0 || bonuses.heal > 0) Heal(Math.Max(0, card.heal) + bonuses.heal);
            c.strength += card.strength;
            if (landed) { c.enemyPoison += card.poison; c.enemyVulnerable = Math.Max(c.enemyVulnerable, card.vulnerable); c.enemyWeak = Math.Max(c.enemyWeak, card.weak); }
            if (card.evasion > 0) { c.evasion = Math.Max(c.evasion, card.evasion); c.evasionTurns = Math.Max(c.evasionTurns, card.duration); }
            RecordLegacyDebuffs(card, false, landed, poisonBefore, weakBefore, vulnerableBefore, reference);
            c.energy += card.energy;
            DrawCards(card.draw);
            SkillMechanics.AfterCard(c.playerSkills, skillBefore, card, landed, paidCost: price);
            CompanionMechanics.AfterCard(c.playerSkills, card);
            c.chargedCards.RemoveAll(x => x.reference == reference);
            StatusMechanics.CancelMissingInstallations(c.enemyStatuses,c.playerSkills);
            StatusMechanics.DamageTaken(c.enemyStatuses,dealt);
            bool statusControl=ApplyCardStatuses(false,skillBefore,card,landed);
            RecallSkillDiscounts(c.playerSkills, skillBefore, State.deck, c.hand, c.drawPile, c.discardPile, true);
            if (RecordLastSkill(c.lastSkills, card)) c.lastSkillCard = id;
            if (garnetRecall && !landed)
            {
                int missedCopy = c.discardPile.LastIndexOf(reference);
                if (missedCopy >= 0) { c.discardPile.RemoveAt(missedCopy); c.exhaustPile.Add(reference); }
            }
            GrantFreeCasts(card, State.deck, c.freeCasts, c.freeCastSources, c.hand, c.drawPile, c.discardPile, true, LastSkill(c.lastSkills, card.owner), skillBefore, landed, c.pendingFreeCasts, c.turn, reference);
            int cardHeal=State.hp-beforeHp;
            EmitCombatAction(new CombatAction { id = ++actionSerial, cardId = id, cardReference = reference, upgradeLabel = CardUpgradeLabel(reference), enemy = false, damage = dealt, block = block, heal = cardHeal, avoided = avoided, missed = missed, absorbed = absorbedTotal, destroyedBlock = destroyedBlock, critical = criticalHits > 0, criticalHits = criticalHits, execution = execution });
            bool attackAttempt=card.damage>0 || bonusDamage>0;
            ApplyBeforeTraits(false,card,landed && attackAttempt,skillBefore,traitHpBefore);
            AfterCardTraits(false,card,landed,dealt,block,cardHeal,avoided,attackAttempt);
            AugmentAfterCard(card, price, augmentEnergyBefore, block);
            if(statusControl)TraitEvent(false,"control",card,true,dealt);
            if(defenderBlock>0 && c.enemyBlock==0) TraitEvent(true,"block_break",card,landed,dealt);
            Say(card.name + " 사용" + (execution ? " · 처형!" : "") + (criticalHits > 0 ? " · 치명타!" : "") + (card.damage > 0 ? " · 피해 " + dealt : "") + (block > 0 ? " · 방어 +" + block : ""));
            // The played card is complete here. A counter can end combat before the next line returns.
            ResearchProgression.AfterCard(State, card, skillBefore, landed, dealt, CombatActions.Skip(researchActionStart));
            if (landed && (card.damage > 0 || bonusDamage > 0)) ResolveCounters(true, landedHits);
            RecordBossProgress(BossMechanics.ObserveCard(c, card, price, directDamage));
            if (State.stage == RunStage.Combat) RefreshEnemyIntent();
            CheckBattleEnd();
            return true;
        }

        public bool ToggleRetainCard(int handIndex)
        {
            var c = State.combat;
            if (pendingCardChoice != null || State.stage != RunStage.Combat || c == null || c.enemyTurn || RetainAvailable <= 0 || handIndex < 0 || handIndex >= c.hand.Count || IsCharged(c.hand[handIndex]))
                return Fail("손패 보관은 전투마다 2회, 내 턴에 손패 1장을 선택할 수 있습니다.");
            c.retainIndex = c.retainIndex == handIndex ? -1 : handIndex;
            return Say(c.retainIndex < 0 ? "손패 보관 예약을 취소했습니다." : GameDatabase.Card(c.hand[handIndex]).name + " 보관 예약 · 턴 종료 시 확정하고 다음 턴 새로 뽑는 카드가 1장 줄어듭니다.");
        }

        public bool BeginEndTurn()
        {
            var c = State.combat;
            if (pendingCardChoice != null || State.stage != RunStage.Combat || c == null || c.enemyTurn) return Fail("현재 플레이어의 턴이 아니거나 카드 선택이 진행 중입니다.");
            RecordCombatInformation(new CombatLogEntry { kind = "turn_end", title = "내 턴 종료", detail = "남은 코스트 " + c.energy + " · 지속 효과와 상대 행동을 처리합니다." });
            AugmentTurnEnd();
            if (FreeCastMechanics.ExpireGarnetRecall(c)) Say("무료 재사용을 마치지 않은 처형식이 소멸했습니다.");
            if (c.retainIndex >= 0 && c.retainIndex < c.hand.Count && c.retainUses < 2)
            {
                c.retainedCardId = c.hand[c.retainIndex]; c.hand.RemoveAt(c.retainIndex); c.retainUses++;
            }
            c.retainIndex = -1;
            HoldChargedCards();
            c.discardPile.AddRange(c.hand); c.hand.Clear();
            c.freeCasts.Clear(); c.freeCastSources.Clear();
            if (c.weak > 0) c.weak--;
            if (c.vulnerable > 0) c.vulnerable--;
            if (c.enemyPoison > 0)
            {
                int damage = TakeHealthDamage(true,c.enemyPoison);
                Say(c.enemyName + " 중독 피해 " + damage); c.enemyPoison--;
                EmitCombatAction(new CombatAction { id = ++actionSerial, cardId = "status_poison", enemy = false, damage = damage });
                if(damage>0){TraitEvent(true,"damaged",null,true,damage);TraitEvent(true,"low_health",null,true,damage);}
            }
            ResolveSkillTicks(false);
            TraitEvent(false,"turn_end",null,true);
            TraitMechanics.EndTurn(c.playerTraits);
            StatusMechanics.EndTurn(c.playerStatuses);
            if (CheckBattleEnd()) return true;
            RecordBossProgress(BossMechanics.FinalizeChallenge(c));
            c.enemyBlock = 0;
            if (c.enemyEvasionTurns > 0 && --c.enemyEvasionTurns == 0) c.enemyEvasion = BaseEnemyEvasion();
            c.enemyTurn = true; c.enemyActionIndex = 0; c.enemyFreeCasts.Clear(); c.enemyFreeCastSources.Clear();
            // Planning already moved the exact recalled copies into the enemy's finite piles.
            var availableEnemyCopies = c.enemyDeck.Where(id => c.enemyHand.Contains(id) || c.enemyDrawPile.Contains(id) || c.enemyDiscardPile.Contains(id)
                || c.enemyPlan.Where((planned,i) => i < c.enemyPlanFreeCast.Count && c.enemyPlanFreeCast[i]).Contains(id));
            FreeCastMechanics.ActivatePending(c.enemyPendingFreeCasts, c.turn, availableEnemyCopies, c.enemyFreeCasts, c.enemyFreeCastSources, null, null, null, false);
            RecordCombatInformation(new CombatLogEntry { kind = "turn_start", enemy = true, title = "상대 턴 시작", detail = "상대의 턴 시작 효과와 예정 행동을 처리합니다." });
            c.enemyAvailableEnergy = EnergyForLevel(c.enemyLevel)+TraitMechanics.EnergyBonus(c.enemyTraits)+c.enemyOpeningEnergy;c.enemyOpeningEnergy=0;
            SkillMechanics.StartTurn(c.enemySkills);
            CompanionMechanics.StartTurn(c.enemySkills);
            TraitMechanics.StartTurn(c.enemyTraits);
            TraitEvent(true,"turn_start",null,true);
            c.enemyBlock+=EnemyEquipmentSum(g=>g.block);
            ResolveDeferredDamage(true);
            if(CheckBattleEnd())return true;
            c.enemyAvailableEnergy=Math.Max(1,c.enemyAvailableEnergy-ControlEnergyPenalty(true,c.enemyStatuses));
            return true;
        }

        public bool AdvanceEnemyAction()
        {
            var c = State.combat;
            if (State.stage != RunStage.Combat || c == null || !c.enemyTurn) return false;
            if (BossMechanics.NeedsSpecialAction(c)) { ResolveBossSpecial(); CheckBattleEnd(); return true; }
            if (c.boss != null) { c.boss.specialResolved = true; c.boss.challengeActive = false; }
            if (c.enemyActionIndex >= c.enemyPlan.Count)
            {
                RecoverCancelledEnemyCharges();
                c.enemyTurn = false; c.enemyPlan.Clear(); c.enemyPlanCosts.Clear(); c.enemyPlanFreeCast.Clear(); c.enemyAdvancedActions.Clear(); c.enemyHand.Clear();
                ResolveSkillTicks(true);
                TraitEvent(true,"turn_end",null,true);
                TraitMechanics.EndTurn(c.enemyTraits);
                StatusMechanics.EndTurn(c.enemyStatuses);
                if (CheckBattleEnd()) return true;
                if (c.enemyWeak > 0) c.enemyWeak--;
                if (c.enemyVulnerable > 0) c.enemyVulnerable--;
                if (c.evasionTurns > 0 && --c.evasionTurns == 0) c.evasion = 0;
                StartPlayerTurn();
                return true;
            }
            int index = c.enemyActionIndex++;
            var id = c.enemyPlan[index];
            if(ResolveWildlifeSkill(id))return true;
            var card = GameDatabase.Card(id);
            var advancedAction = EnemyAdvancedActionAt(index);
            if (card == null) return true;
            if(!StatusMechanics.CanUse(c.enemyStatuses,card)){Say(c.enemyName+"의 "+card.name+" 사용이 상태이상으로 막혔습니다.");return true;}
            bool freeGranted = advancedAction.charge==0 && c.enemyFreeCasts.Any(g => g.cardId == id && g.uses > 0);
            bool garnetExecution = FreeCastMechanics.GarnetExecutionReady(card, c.enemySkills, freeGranted, State.hp, State.maxHp);
            bool execution = false;
            bool plannedFree = index < c.enemyPlanFreeCast.Count && c.enemyPlanFreeCast[index];
            // A predicted reset may have recalled this exact copy. A missed source must not turn it into a second paid cast.
            if (plannedFree && !freeGranted && advancedAction.charge==0) { Say(c.enemyName + "의 " + card.name + " 연계가 취소되었습니다."); return true; }
            int price = EnemyEffectiveCardCost(id);
            price=AdvancedEnemyPrice(card,price,c.enemyAvailableEnergy,freeGranted,advancedAction);
            if(index<c.enemyPlanCosts.Count && c.enemyPlanCosts[index]==0 && CardCost(id)>0 && price>0 && !AdvancedCardMechanics.IsX(card))
            {Say(c.enemyName+"의 "+card.name+" 할인 연계가 취소되었습니다.");return true;}
            if (price > c.enemyAvailableEnergy) { Say(c.enemyName + "의 " + card.name + " 연계가 취소되었습니다."); return true; }
            if(advancedAction.mode=="charge")return BeginEnemyCharge(card,price);
            if(advancedAction.charge>0 && !c.enemyChargedCards.Any(held=>held.reference==id && held.readyTurn<=c.turn))return true;
            int x=AdvancedCardMechanics.IsX(card)?freeGranted?0:price:0;
            card=AdvancedCardMechanics.PreparedCard(card,advancedAction.mode,advancedAction.charge,advancedAction.sacrificed,x);
            if(AdvancedCardMechanics.IsEmptyX(card))
            {
                ConsumeFreeCast(c.enemyFreeCasts,id);SkillMechanics.ConsumeDiscount(c.enemySkills,id);SkillMechanics.ConsumeDiscount(c.enemyTraits.skills,id);
                RecordCombatInformation(new CombatLogEntry {kind="card_result",cardId=id,enemy=true,title=card.name+" · X=0",paidCost=0,detail="피해·카드 효과·사용 시 발동 효과가 발생하지 않았습니다."});
                EmitCombatAction(new CombatAction{id=++actionSerial,cardId=id,enemy=true});return true;
            }
            if(!advancedAction.sacrificeCommitted && advancedAction.sacrificedReferences!=null)
            {c.enemyExhaustPile.AddRange(advancedAction.sacrificedReferences);advancedAction.sacrificeCommitted=true;}
            if(advancedAction.charge>0)
            {
                var held=c.enemyChargedCards.First(x=>x.reference==id);c.enemyChargedCards.Remove(held);
                if(card.exhaust)c.enemyExhaustPile.Add(id);else c.enemyDiscardPile.Add(id);
            }
            var advancedBonus=EnemyAdvancedBonus(card,advancedAction,c.enemyHp,c.enemyBlock);
            RecordCombatCardStart(card, true, price, freeGranted);
            int poisonBefore = c.poison, weakBefore = c.weak, vulnerableBefore = c.vulnerable;
            if(advancedAction.charge==0)
            {
                ConsumeFreeCast(c.enemyFreeCasts, id);
                SkillMechanics.ConsumeDiscount(c.enemySkills, id);
                SkillMechanics.ConsumeDiscount(c.enemyTraits.skills,id);
            }
            var skillBefore = SkillMechanics.Clone(c.enemySkills);
            int traitHpBefore=c.enemyHp;
            c.enemyAvailableEnergy -= price; c.enemyAvailableEnergy += card.energy;
            int damagePerHit = EnemyCardDamage(card), bonusDamage = EnemyTotalBonusDamage(card,c.enemySkills,c.enemyTraits,c.vulnerable,advancedBonus.damage), hits = Math.Max(1, card.hits), dealt = 0, avoided = 0, missed = 0, criticalHits = 0, absorbedTotal = 0, landedHits = 0, destroyedBlock = 0;
            var incoming=damagePerHit>0 || bonusDamage>0 ? TraitEvent(false,"before_incoming",card,true) : new TraitResult();int incomingReduction=incoming.damageReduction,defenderBlock=c.block;
            bool landed = damagePerHit == 0 && bonusDamage == 0, bonusUsed = false;
            for (int i = 0; i < hits && State.hp > 0; ++i)
            {
                if (damagePerHit == 0 && bonusDamage == 0) break;
                bool accuracyMiss;
                if (!TryDirectHit(card, true, out accuracyMiss, freeGranted))
                {
                    if (accuracyMiss) { missed++; Say(c.enemyName + "의 " + card.name + " 공격이 빗나갔습니다."); }
                    else { avoided++; Say("하나가 " + card.name + "을(를) 회피했습니다."); }
                    continue;
                }
                landed = true;
                landedHits++;
                if (landedHits == 1 && SkillMechanics.BreaksBlock(skillBefore, card))
                {
                    destroyedBlock = c.block; c.block = 0;
                }
                if (garnetExecution)
                {
                    dealt += State.hp; State.hp = 0; execution = true;
                    break;
                }
                int baseDamage = damagePerHit;
                if (id == "basic_attack" && baseDamage > 0 && Roll(EnemyCritChance)) { baseDamage = GearCriticalDamage(baseDamage,true); criticalHits++; }
                int damage = Math.Max(0,RunModes.ScaleEnemyDirectDamage(State, baseDamage + (bonusUsed ? 0 : bonusDamage)) - (bonusUsed?0:incomingReduction)), absorbed = Math.Min(c.block, damage); c.block -= absorbed; absorbedTotal += absorbed;
                bonusUsed = true;
                dealt += TakeHealthDamage(false,damage-absorbed);
            }
            var bonuses = AddAdvancedBonus(SkillMechanics.Bonuses(skillBefore, card, landed),advancedBonus);
            int addedBlock = EnemyAuxiliaryValue(card,card.block) + bonuses.block;
            c.enemyBlock += addedBlock;
            int beforeHp = c.enemyHp;
            HealEnemy(EnemyAuxiliaryValue(card,card.heal)+bonuses.heal);
            c.enemyStrength += card.strength;
            if (landed) { c.poison += card.poison; c.vulnerable = Math.Max(c.vulnerable, card.vulnerable); c.weak = Math.Max(c.weak, card.weak); }
            if (card.evasion > 0 && string.IsNullOrEmpty(c.animal)) { c.enemyEvasion = Math.Max(BaseEnemyEvasion(), card.evasion); c.enemyEvasionTurns = Math.Max(c.enemyEvasionTurns, card.duration); }
            RecordLegacyDebuffs(card, true, landed, poisonBefore, weakBefore, vulnerableBefore);
            SkillMechanics.AfterCard(c.enemySkills, skillBefore, card, landed, paidCost: price);
            CompanionMechanics.AfterCard(c.enemySkills,card);
            StatusMechanics.CancelMissingInstallations(c.playerStatuses,c.enemySkills);
            StatusMechanics.DamageTaken(c.playerStatuses,dealt);
            bool statusControl=ApplyCardStatuses(true,skillBefore,card,landed);
            RecordLastSkill(c.enemyLastSkills, card);
            GrantFreeCasts(card, c.enemyDeck, c.enemyFreeCasts, c.enemyFreeCastSources, c.enemyHand, c.enemyDrawPile, c.enemyDiscardPile, false, LastSkill(c.enemyLastSkills, card.owner), skillBefore, landed, c.enemyPendingFreeCasts, c.turn);
            int cardHeal=c.enemyHp-beforeHp;
            EmitCombatAction(new CombatAction { id = ++actionSerial, cardId = id, enemy = true, damage = dealt, block = addedBlock, heal = cardHeal, avoided = avoided, missed = missed, absorbed = absorbedTotal, destroyedBlock = destroyedBlock, critical = criticalHits > 0, criticalHits = criticalHits, execution = execution });
            bool attackAttempt=damagePerHit>0 || bonusDamage>0;
            ApplyBeforeTraits(true,card,landed && attackAttempt,skillBefore,traitHpBefore);
            AfterCardTraits(true,card,landed,dealt,addedBlock,cardHeal,avoided,attackAttempt);
            if(statusControl)TraitEvent(true,"control",card,true,dealt);
            if(defenderBlock>0 && c.block==0) TraitEvent(false,"block_break",card,landed,dealt);
            Say(c.enemyName + " · " + card.name + (execution ? " · 처형!" : "") + (criticalHits > 0 ? " · 치명타!" : "") + (card.damage > 0 ? " · 피해 " + dealt : ""));
            if (landed && (damagePerHit > 0 || bonusDamage > 0)) ResolveCounters(false, landedHits);
            CheckBattleEnd();
            return true;
        }

        public bool EndTurn()
        {
            if (!BeginEndTurn()) return false;
            while (State.stage == RunStage.Combat && State.combat.enemyTurn) AdvanceEnemyAction();
            return true;
        }

        public static int CriticalBaseDamage(int damage) { return Math.Max(0, damage) * 3 / 2; }

        private static string LastSkill(List<SkillHistory> history, string owner)
        {
            var entry = history.FirstOrDefault(x => x.owner == owner);
            return entry == null ? null : entry.cardId;
        }

        private static bool RecordLastSkill(List<SkillHistory> history, CardDef card)
        {
            if (card.category != "skill" || (card.key != "Q" && card.key != "W" && card.key != "E")) return false;
            var entry = history.FirstOrDefault(x => x.owner == card.owner);
            if (entry == null) { entry = new SkillHistory { owner = card.owner }; history.Add(entry); }
            entry.cardId = card.id;
            return true;
        }

        private static void ConsumeFreeCast(List<FreeCastGrant> grants, string id)
        {
            var grant = grants.FirstOrDefault(g => FreeCastMechanics.Matches(g,id));
            if (grant != null) grant.uses--;
        }

        private static void GrantFreeCasts(CardDef card, IEnumerable<string> owned, List<FreeCastGrant> grants, List<string> sources, List<string> hand, List<string> draw, List<string> discard, bool recall = true, string lastSkill = null, SkillActorState before = null, bool landed = true, List<PendingFreeCast> pending = null, int turn = 0, string playedReference = null)
        {
            if (!FreeCastMechanics.Eligible(card,before,landed)) return;
            var targets = card.freeCastTargets.Distinct().Where(id => !card.freeCastLastSkill || id == lastSkill);
            foreach (var target in targets)
            {
                string source = card.id + "|" + target;
                if (!owned.Any(id => CardInstances.SameType(id,target)) || GameDatabase.Card(target) == null || sources.Contains(source)) continue;
                sources.Add(source);
                if (card.freeCastNextTurn)
                {
                    if (pending != null && !pending.Any(x => x.sourceCardId == card.id && x.cardId == target))
                        pending.Add(new PendingFreeCast { sourceCardId = card.id, cardId = target, uses = card.freeCastCount, readyTurn = turn + 1 });
                    continue;
                }
                var grant = grants.FirstOrDefault(g => g.cardId == target);
                if (grant == null) { grant = new FreeCastGrant { cardId = target }; grants.Add(grant); }
                if(card.id=="garnet_r" && !string.IsNullOrEmpty(playedReference)) grant.cardReference=playedReference;
                grant.uses += card.freeCastCount;
                // Recall an actual owned copy. Exhausted cards stay exhausted and no new cards are created.
                // Garnet temporarily withheld exhaustion from this played copy. Recall that
                // copy even when an independently owned duplicate is already in hand.
                if (!recall || hand.Count >= 12 || (hand.Any(id => CardInstances.SameType(id,target)) && card.id != "garnet_r")) continue;
                int index = card.id=="garnet_r" && !string.IsNullOrEmpty(playedReference) ? discard.LastIndexOf(playedReference) : discard.FindLastIndex(id => CardInstances.SameType(id,target));
                if (index >= 0) { string actual=discard[index]; discard.RemoveAt(index); hand.Add(actual); continue; }
                index = draw.FindLastIndex(id => CardInstances.SameType(id,target));
                if (index >= 0) { string actual=draw[index]; draw.RemoveAt(index); hand.Add(actual); }
            }
        }

        private RewardSource CurrentRewardSource
        {
            get
            {
                var r = State.rewards;
                if (r == null) return RewardSource.Unknown;
                if (r.source == RewardSource.Wildlife || r.source == RewardSource.Subject || r.source == RewardSource.Boss) return r.source;
                var node = ActiveNode();
                if (node != null)
                {
                    if (node.kind == ZoneKind.Wildlife) return RewardSource.Wildlife;
                    if (node.kind == ZoneKind.Subject) return RewardSource.Subject;
                    if (node.kind == ZoneKind.Boss) return RewardSource.Boss;
                }
                // The boss marker also survives older passive-choice screens. Ambiguous
                // legacy rewards without a combat node retain their existing budget.
                if (r.boss) return RewardSource.Boss;
                return !string.IsNullOrEmpty(State.combat?.animal) ? RewardSource.Wildlife : RewardSource.Unknown;
            }
        }

        private static int LegacyRewardCardPrice(string id)
        {
            // Frozen version 1 prices, verified against its Windows build. Later data revisions cannot change a pending reward's budget.
            string[] one = { "basic_attack", "basic_guard", "nia_q", "jackie_w", "aya_q", "hyunwoo_q", "yuki_q", "nadine_q", "emma_q", "emma_r", "weapon_axe" };
            string[] two = { "nia_w", "jackie_q", "jackie_e", "aya_w", "aya_e", "hyunwoo_w", "hyunwoo_e", "yuki_w", "yuki_e", "hyejin_q", "hyejin_w", "hyejin_e", "sua_q", "sua_w", "sua_e", "isol_q", "isol_w", "isol_e", "nadine_w", "nadine_e", "emma_w", "emma_e", "weapon_glove" };
            return one.Contains(id) ? 1 : two.Contains(id) ? 2 : 3;
        }

        public int RewardCardPrice(string id)
        {
            var r = State.rewards;
            var legacy = r == null || r.legacyPrices == null ? null : r.legacyPrices.FirstOrDefault(x => x.cardId == id);
            return legacy != null ? Math.Max(1, legacy.cost) : Math.Max(1, CardCost(id));
        }

        public bool CanClaimCard(string id)
        {
            var r = State.rewards;
            if (State.stage != RunStage.Rewards || r == null || r.cardsCommitted || string.IsNullOrEmpty(id)
                || r.choices == null || !r.choices.Contains(id) || GameDatabase.Card(id) == null) return false;
            if (r.taken != null && r.taken.Contains(id)) return true;
            return RewardUsesCardLimit ? RewardRemainingCards > 0 : RewardCardPrice(id) <= RewardRemainingBudget;
        }

        public bool ClaimCard(string id)
        {
            var r = State.rewards;
            if (State.stage != RunStage.Rewards || r == null || r.cardsCommitted || string.IsNullOrEmpty(id)
                || r.choices == null || !r.choices.Contains(id) || GameDatabase.Card(id) == null) return Fail("현재 보상에 있는 카드를 선택하세요.");
            if (r.taken == null) r.taken = new List<string>();
            if (r.taken.Remove(id)) return Say(GameDatabase.Card(id).name + " 카드 선택을 취소했습니다.");
            if (!CanClaimCard(id)) return Fail(RewardUsesCardLimit ? "보상 카드는 최대 2장까지 선택할 수 있습니다. 선택한 카드를 눌러 취소할 수 있습니다." : "보상 카드 선택 코스트가 부족합니다. 선택한 카드를 눌러 취소할 수 있습니다.");
            r.taken.Add(id);
            return Say(GameDatabase.Card(id).name + " 카드를 선택했습니다. 확정 전까지 변경할 수 있습니다.");
        }

        public bool FinishRewards()
        {
            if (State.stage != RunStage.Rewards || State.rewards == null) return Fail("현재 받을 보상이 없습니다.");
            var r = State.rewards;
            if (!r.cardsCommitted && RewardUsesCardLimit && r.taken.Count > RewardCardLimit)
                return Fail("보상 카드는 최대 2장까지 선택할 수 있습니다. 선택한 카드를 눌러 취소한 뒤 확정하세요.");
            if (!r.cardsCommitted) { State.deck.AddRange(r.taken); r.cardsCommitted = true; }
            if (!string.IsNullOrEmpty(State.pendingPassive)) return AcceptPassiveOrChoose();
            CompleteNode(); return true;
        }
        public int ShopPrice(int basePrice)
        {
            bool discounted=ActorTraits(false).Any(x=>x.profile.rules.Any(r=>r.op=="shop_discount" && r.amount>0));
            return Math.Max(0,discounted?basePrice*90/100:basePrice);
        }
        public int ObjectPrice(string id) { var item=GameDatabase.Object(id);return item==null?int.MaxValue:ShopPrice(item.price); }
        public int FoodPrice(string id) { var item=GameDatabase.Food(id);return item==null?int.MaxValue:ShopPrice(item.price); }
        public bool IsKioskObjectUnlocked(string id) { return GameDatabase.Object(id) != null && (id != "blood" || DefeatedBossCount >= 2); }
        public bool CanBuyObject(string id) { return State.stage == RunStage.Kiosk && IsKioskObjectUnlocked(id) && State.credits >= ObjectPrice(id); }
        public bool IsKioskFood(string id)
        {
            var food = GameDatabase.Food(id);
            // Stock is ingredients and raw food. Finished meals are obtained by cooking and encounters.
            return food != null && !food.fullHeal && (id == "watermelon" || GameDatabase.Food(food.upgradeTo) != null);
        }
        public IEnumerable<FoodDef> KioskFoods { get { return GameDatabase.Foods.Where(food => IsKioskFood(food.id)); } }
        public bool CanBuyFood(string id) { return State.stage == RunStage.Kiosk && IsKioskFood(id) && State.credits >= FoodPrice(id); }
        public bool BuyObject(string id)
        {
            var item = GameDatabase.Object(id);
            int price=ObjectPrice(id);
            if (id == "blood" && !IsKioskObjectUnlocked(id)) return Fail("VF혈액샘플은 두 번째 보스를 이긴 뒤 키오스크에서 구매할 수 있습니다.");
            if (!CanBuyObject(id)) return Fail("오브젝트를 구매할 크레딧이 부족하거나 키오스크에 있지 않습니다.");
            State.credits -= price; State.objects.Add(id);
            return Say(item.name + " 구매 · " + price + " 크레딧");
        }

        public bool BuyFood(string id)
        {
            var item = GameDatabase.Food(id);
            int price=FoodPrice(id);
            if (!IsKioskFood(id)) return Fail("만년 스프와 완성 요리는 키오스크에서 판매하지 않습니다.");
            if (!CanBuyFood(id)) return Fail("음식을 구매할 크레딧이 부족하거나 키오스크에 있지 않습니다.");
            State.credits -= price; State.foods.Add(id);
            return Say(item.name + " 구매 · " + price + " 크레딧");
        }

        public bool LeaveKiosk() { if (State.stage != RunStage.Kiosk) return false; CompleteNode(); return true; }

        public bool ChooseCamp(bool craft)
        {
            if (State.stage != RunStage.Campfire || State.campChoice != 0) return Fail("이미 모닥불 행동을 선택했습니다.");
            State.campChoice = craft ? 2 : 1;
            State.campRemovalsRemaining = craft ? 0 : 2;
            if (craft) return Say("체력 회복 없이 제작과 요리를 합쳐 최대 3번 할 수 있습니다.");
            State.hp = State.maxHp;
            State.foods.Add("soup");
            return Say("체력을 모두 회복하고 만년 스프를 1개 받았습니다. 원하는 카드를 최대 2장 제거하거나 바로 떠날 수 있습니다.");
        }

        public bool CanRemoveCampCard(int deckIndex)
        {
            if (State.stage != RunStage.Campfire || State.campChoice != 1 || State.campRemovalsRemaining <= 0
                || deckIndex < 0 || deckIndex >= State.deck.Count || GameDatabase.Card(State.deck[deckIndex]) == null) return false;
            return !IsEquipmentGrantedCard(deckIndex);
        }

        public bool IsEquipmentGrantedCard(int deckIndex)
        {
            if (deckIndex < 0 || deckIndex >= State.deck.Count) return false;
            string id = State.deck[deckIndex];
            int grants = State.weaponGrantedCards == null ? 0 : State.weaponGrantedCards.Count(cardId => cardId == id);
            // SyncWeaponCards appends equipment grants after earned copies and removes those last copies.
            // Protect only those grants; an independently earned copy of the same D skill can be removed.
            int laterCopies = 0;
            for (int i = deckIndex + 1; i < State.deck.Count; i++)
                if (State.deck[i] == id) laterCopies++;
            return laterCopies < grants;
        }

        public bool RemoveCampCards(IEnumerable<int> deckIndices)
        {
            if (State.stage != RunStage.Campfire || State.campChoice != 1 || State.campRemovalsRemaining <= 0)
                return Fail("카드 제거는 모닥불에서 휴식을 선택한 뒤 최대 2장까지 가능합니다.");
            if (deckIndices == null) return Fail("제거할 카드를 선택하세요.");
            var indices = deckIndices.ToList();
            if (indices.Count == 0) return Fail("제거할 카드를 선택하거나 바로 떠나세요.");
            if (indices.Count > State.campRemovalsRemaining || indices.Distinct().Count() != indices.Count)
                return Fail("남은 제거 횟수 안에서 서로 다른 카드를 선택하세요.");
            // Validate the entire batch before mutating the deck or spending its saved budget.
            if (indices.Any(index => !CanRemoveCampCard(index)))
                return Fail("선택한 카드를 제거할 수 없습니다. 장비로 얻은 무기 스킬은 장비와 함께 유지됩니다.");
            var names = indices.Select(index => GameDatabase.Card(State.deck[index]).name).ToArray();
            foreach (int index in indices.OrderByDescending(index => index)) State.deck.RemoveAt(index);
            State.campRemovalsRemaining -= indices.Count;
            return Say(string.Join(" · ", names) + " 제거 · 추가로 " + State.campRemovalsRemaining + "장까지 제거할 수 있습니다.");
        }

        public bool Craft(string id, int replaceIndex = -1)
        {
            var item = GameDatabase.Equipment(id);
            if (State.stage != RunStage.Campfire || State.campChoice != 2 || State.campActions <= 0 || item == null) return Fail("제작은 모닥불에서 최대 3번 가능합니다.");
            if (!State.objects.Contains(item.objectId)) return Fail("제작에 필요한 오브젝트가 없습니다.");
            int count = State.gear.Count(g => GameDatabase.Equipment(g) != null && GameDatabase.Equipment(g).slot == item.slot);
            if (replaceIndex >= 0 && (replaceIndex >= State.gear.Count || GameDatabase.Equipment(State.gear[replaceIndex]).slot != item.slot)) return Fail("같은 부위의 장비를 교체하세요.");
            if (count >= EquipmentCapacity(item.slot) && replaceIndex < 0) return Fail("이 부위는 " + EquipmentCapacity(item.slot) + "개까지 착용합니다. 교체할 장비를 선택하세요.");
            if (replaceIndex >= 0) State.gear.RemoveAt(replaceIndex);
            State.objects.Remove(item.objectId); State.gear.Add(id); State.campActions--;
            PruneUnownedTraits(); SyncWeaponCards(); RecalculateHealth(false);
            return Say(item.name + " 제작 · 남은 제작·요리 " + State.campActions + "회");
        }

        public bool Cook(int foodIndex)
        {
            if (State.stage != RunStage.Campfire || State.campChoice != 2 || State.campActions <= 0 || foodIndex < 0 || foodIndex >= State.foods.Count) return Fail("요리는 모닥불에서 최대 3번 가능합니다.");
            var food = GameDatabase.Food(State.foods[foodIndex]);
            var upgraded = food == null ? null : GameDatabase.Food(food.upgradeTo);
            if (upgraded == null) return Fail("이 음식은 모닥불에서 강화할 수 없습니다.");
            State.foods[foodIndex] = upgraded.id; State.campActions--;
            return Say(food.name + " → " + upgraded.name + " · 남은 제작·요리 " + State.campActions + "회");
        }

        public bool LeaveCamp()
        {
            if (State.stage != RunStage.Campfire || State.campChoice == 0) return Fail("모닥불 행동을 먼저 선택하세요.");
            State.campRemovalsRemaining = 0;
            CompleteNode(); return true;
        }

        public bool SelectEncounter(string id)
        {
            if (State.stage != RunStage.Encounter || !State.encounterOffers.Contains(id) || !string.IsNullOrEmpty(State.chosenEventId)) return Fail("제시된 조우 3개 중 하나를 선택하세요.");
            var encounter = GameDatabase.Event(id);
            if (encounter == null) return Fail("선택할 수 없는 조우입니다.");
            string researchSubject = ResearchEncounters.SubjectForEvent(id);
            if (researchSubject != null && (!State.research.unlockedReports.Contains(researchSubject)
                || State.research.completedResearchEvents.Contains(id))) return Fail("아직 해금하지 않았거나 이번 원정에서 마친 연구 조우입니다.");
            State.chosenEventId = id;
            ResearchProgression.ObserveSubject(State, GameDatabase.Characters.FirstOrDefault(x => x.name == encounter.owner)?.id);
            return Say(encounter.story);
        }

        private void RollEncounterOffers()
        {
            RollRareEncounterOffers();
        }

        private PendingEncounterFollowup NextPromisedEncounter()
        {
            return State.pendingEncounterFollowups.LastOrDefault(x => x.priorityNext) ?? State.pendingEncounterFollowups.FirstOrDefault();
        }

        public bool CanChooseEventOption(int index)
        {
            var ev=GameDatabase.Event(State.chosenEventId);
            if(State.stage!=RunStage.Encounter || ev==null || State.runeChangePending || State.cardUpgradePending || State.rareRemovalPending || State.rareCopyPending || index<0 || index>=ev.options.Length)return false;
            var option=ev.options[index];
            if (State.research.completedResearchEvents.Contains(ev.id)) return false;
            if (ev.chainOnly && State.completedChainEvents.Contains(ev.id)) return false;
            if (!CanChooseRareOption(option)) return false;
            int totalCreditCost = Math.Max(0, option.creditCost) + (option.effect == "trade_card" || option.effect == "trade_object" || option.effect == "rare_trade_card" ? Math.Max(0, option.amount) : 0);
            if (totalCreditCost > State.credits || (!string.IsNullOrEmpty(option.requiredFoodId) && !State.foods.Contains(option.requiredFoodId))
                || (!string.IsNullOrEmpty(option.requiredObjectId) && !State.objects.Contains(option.requiredObjectId))) return false;
            if (!string.IsNullOrEmpty(option.followupId) && (State.completedChainEvents.Contains(option.followupId) || State.pendingEncounterFollowups.Any(x => x.eventId == option.followupId))) return false;
            if(option.effect=="trade_card" || option.effect=="trade_object") return State.credits>=Math.Max(0,option.amount);
            if(option.effect=="risky_card" || option.effect=="risky_object") return State.hp>Math.Max(0,option.amount);
            if(option.effect=="remove_card")return State.deck.Count>5 && State.deck.Any(id=>GameDatabase.Card(id)!=null && GameDatabase.Card(id).category!="weapon");
            if(option.effect=="upgrade_card")return UpgradeCandidates().Count > 0;
            return true;
        }
        public string EventOptionUnavailableReason(int index)
        {
            if(CanChooseEventOption(index))return "";
            var ev=GameDatabase.Event(State.chosenEventId);
            if(ev==null || index<0 || index>=ev.options.Length)return "선택할 수 없는 행동입니다.";
            if (State.cardUpgradePending) return "먼저 카드 강화를 완료하거나 포기하세요.";
            if (State.rareRemovalPending || State.rareCopyPending) return "먼저 레어 조우의 카드 선택을 완료하거나 포기하세요.";
            if (ev.chainOnly && State.completedChainEvents.Contains(ev.id)) return "이미 보상을 받은 후속 조우입니다.";
            if (State.research.completedResearchEvents.Contains(ev.id)) return "이번 원정에서 이미 마친 연구 조우입니다.";
            var option = ev.options[index];
            string rareReason = RareOptionUnavailableReason(option);
            if (!string.IsNullOrEmpty(rareReason)) return rareReason;
            if (option.creditCost > State.credits) return "약속에 필요한 크레딧이 부족합니다.";
            if (!string.IsNullOrEmpty(option.requiredFoodId) && !State.foods.Contains(option.requiredFoodId)) return "필요한 음식: " + (GameDatabase.Food(option.requiredFoodId)?.name ?? option.requiredFoodId);
            if (!string.IsNullOrEmpty(option.requiredObjectId) && !State.objects.Contains(option.requiredObjectId)) return "필요한 오브젝트: " + (GameDatabase.Object(option.requiredObjectId)?.name ?? option.requiredObjectId);
            if (!string.IsNullOrEmpty(option.followupId) && State.completedChainEvents.Contains(option.followupId)) return "이 약속의 후속 조우를 이미 마쳤습니다.";
            if (!string.IsNullOrEmpty(option.followupId) && State.pendingEncounterFollowups.Any(x => x.eventId == option.followupId)) return "이미 다음 만남을 약속했습니다.";
            switch(ev.options[index].effect)
            {
                case "trade_card":case "trade_object":return "필요한 크레딧이 부족합니다.";
                case "risky_card":case "risky_object":return "생존할 체력이 부족합니다.";
                case "remove_card":return "제거할 카드가 없거나 덱에 최소 5장을 남길 수 없습니다.";
                case "upgrade_card":return "강화할 일반 카드가 없습니다.";
                default:return "현재 선택할 수 없는 행동입니다.";
            }
        }
        public bool ChooseEventOption(int index)
        {
            var e = GameDatabase.Event(State.chosenEventId);
            if (State.stage != RunStage.Encounter || e == null || State.runeChangePending || State.cardUpgradePending || State.rareRemovalPending || State.rareCopyPending || index < 0 || index >= e.options.Length) return Fail("조우의 행동을 선택하세요.");
            if(!CanChooseEventOption(index)) return Fail(EventOptionUnavailableReason(index));
            var o = e.options[index]; bool pending = false;
            if (ResearchEncounters.SubjectForEvent(e.id) != null && !State.research.completedResearchEvents.Contains(e.id))
                State.research.completedResearchEvents.Add(e.id);
            State.credits -= Math.Max(0, o.creditCost);
            State.credits += Math.Max(0, o.bonusCredits);
            if (!string.IsNullOrEmpty(o.requiredFoodId)) State.foods.Remove(o.requiredFoodId);
            if (!string.IsNullOrEmpty(o.requiredObjectId)) State.objects.Remove(o.requiredObjectId);
            if (e.chainOnly)
            {
                if (!State.completedChainEvents.Contains(e.id)) State.completedChainEvents.Add(e.id);
                State.pendingEncounterFollowups.RemoveAll(x => x.eventId == e.id);
            }
            if (!string.IsNullOrEmpty(o.followupId) && !State.completedChainEvents.Contains(o.followupId) && !State.pendingEncounterFollowups.Any(x => x.eventId == o.followupId))
            {
                var followup = GameDatabase.Event(o.followupId);
                if (followup != null) State.pendingEncounterFollowups.Add(new PendingEncounterFollowup { eventId = followup.id, owner = followup.owner, priorityNext = true });
            }
            switch (o.effect)
            {
                case "rare_trade_card":
                    State.credits -= Math.Max(0, o.amount); AddRareRewardCard(o.cardId); break;
                case "rare_remove_cards":
                    State.rareRemovalPending = true; return Say("제거할 카드를 최대 2장 선택하세요. 무기 카드는 제거할 수 없으며 덱에는 최소 5장을 남깁니다.");
                case "rare_copy_cards":
                    State.rareCopyPending = true; return Say("강화되지 않은 카드 중 원본 코스트가 1 이하인 카드 1장을 선택하세요. 같은 종류의 강화되지 않은 카드 2장을 덱에 추가합니다.");
                case "rare_support_runes":
                    State.additionalSupportRunes.AddRange(AvailableRareSupportRunes());
                    PruneUnownedTraits(); RecalculateHealth(false); break;
                case "rare_tactical":
                    var tactics = GameDatabase.Cards.Where(card => card.category == "tactical").Select(card => card.id).Distinct().ToList();
                    State.deck.Add(tactics[Next(tactics.Count)]); break;
                case "heal": Heal(o.amount); break;
                case "credits": State.credits = Math.Max(0, State.credits + o.amount); break;
                case "damage": State.hp = Math.Max(0, State.hp - Math.Max(0, o.amount)); break;
                case "max_health": State.maxHealthBonus += o.amount; RecalculateHealth(false); break;
                case "max_energy": case "energy": State.maxEnergyBonus += Math.Max(0, o.amount); break;
                case "card": if (GameDatabase.Card(o.cardId) != null) State.deck.Add(o.cardId); break;
                case "object": if (GameDatabase.Object(o.objectId) != null) State.objects.Add(o.objectId); break;
                case "food": if(GameDatabase.Food(o.objectId)!=null) State.foods.Add(o.objectId);break;
                case "trade_card": if(GameDatabase.Card(o.cardId)!=null){State.credits-=Math.Max(0,o.amount);State.deck.Add(o.cardId);}break;
                case "trade_object": if(GameDatabase.Object(o.objectId)!=null){State.credits-=Math.Max(0,o.amount);State.objects.Add(o.objectId);}break;
                case "risky_card": if(GameDatabase.Card(o.cardId)!=null){State.hp-=Math.Max(0,o.amount);State.deck.Add(o.cardId);}break;
                case "risky_object": if(GameDatabase.Object(o.objectId)!=null){State.hp-=Math.Max(0,o.amount);State.objects.Add(o.objectId);}break;
                case "passive": if (GameDatabase.Passive(o.passiveId) != null && !State.passives.Contains(o.passiveId)) { State.pendingPassive = o.passiveId; pending = true; } break;
                case "rune_change": case "swap_rune": State.runeChangePending = true; Say("원하는 메인 룬과 같은 계열의 보조 룬을 새로 선택하세요."); return true;
                case "remove_card":
                    var removable = State.deck.Where(id => GameDatabase.Card(id) != null && GameDatabase.Card(id).category != "weapon").ToList();
                    if (removable.Count > 0 && State.deck.Count > 5) { string removed = removable[Next(removable.Count)]; State.deck.Remove(removed); Say(GameDatabase.Card(removed).name + " 카드 1장을 제거했습니다."); }
                    break;
                case "upgrade_card":
                    State.cardUpgradePending = true;
                    return Say("덱에서 강화할 카드 1장과 강화 방향을 선택하세요. 선택한 한 장만 강화되며, 같은 종류의 다른 카드와 이후 획득하는 카드는 강화되지 않습니다.");
            }
            Say(o.description);
            if (State.hp <= 0) { Lose(); return true; }
            if (pending) return AcceptPassiveOrChoose();
            CompleteNode(); return true;
        }

        public bool FinishRuneChange()
        {
            if (State.stage != RunStage.Encounter || !State.runeChangePending || State.rareRemovalPending || State.rareCopyPending || string.IsNullOrEmpty(State.mainRune) || string.IsNullOrEmpty(State.supportRune)) return false;
            State.runeChangePending = false; CompleteNode(); return true;
        }

        public bool ReplacePassive(int index = -1)
        {
            if (State.stage != RunStage.PassiveChoice || string.IsNullOrEmpty(State.pendingPassive)) return Fail("교체할 패시브가 없습니다.");
            if (index < -1 || index >= State.passives.Count) return Fail("교체할 패시브를 선택하세요.");
            if (index >= 0) { State.passives[index] = State.pendingPassive; PruneUnownedTraits(); SyncWeaponCards(); RecalculateHealth(false); Say("패시브를 " + GameDatabase.Passive(State.pendingPassive).name + "(으)로 교체했습니다."); }
            else Say("새 패시브를 포기했습니다.");
            State.pendingPassive = null; CompleteNode(); return true;
        }

        public int FoodUseCost(string id) => GameDatabase.Food(id) == null ? 0 : id == "soup" ? 2 : 1;

        public bool CanUseFood(int index)
        {
            if (State.stage == RunStage.Preparation || State.stage == RunStage.Won || State.stage == RunStage.Lost || index < 0 || index >= State.foods.Count || State.hp >= State.maxHp) return false;
            var food = GameDatabase.Food(State.foods[index]);
            if (food == null) return false;
            return pendingCardChoice == null && (State.stage != RunStage.Combat || (State.combat != null && !State.combat.enemyTurn && State.combat.energy >= FoodUseCost(food.id)));
        }

        public bool UseFood(int index)
        {
            if (!CanUseFood(index)) return Fail(State.stage == RunStage.Combat ? "내 턴에 음식에 필요한 코스트와 회복할 체력이 있어야 합니다." : "사용할 음식과 회복할 체력을 확인하세요.");
            var food = GameDatabase.Food(State.foods[index]); if (food == null) return false;
            if (State.hp >= State.maxHp) return Fail("체력이 이미 가득합니다.");
            int price = State.stage == RunStage.Combat ? FoodUseCost(food.id) : 0;
            if (price > 0) State.combat.energy -= price;
            State.foods.RemoveAt(index); int before = State.hp;
            if (food.fullHeal) State.hp = State.maxHp; else Heal(food.heal);
            int recovered=State.hp-before;
            if(State.stage==RunStage.Combat && recovered>0) TraitEvent(false,"heal",null,false,heal:recovered);
            if (State.stage == RunStage.Combat)
            {
                RecordCombatInformation(new CombatLogEntry { kind = "food", title = food.name + " 섭취", detail = "코스트 " + price + " 소모 · 체력 " + recovered + " 회복" });
                RefreshEnemyIntent();
            }
            return Say(food.name + " 사용" + (price > 0 ? " · 코스트 -" + price : "") + " · 체력 +" + recovered);
        }

        private void GenerateMap()
        {
            // Twelve linked rows per act keep repeat services interspersed with fights and encounters.
            for (int act = 1; act <= 3; ++act)
            {
                for (int row = 0; row < State.mapRows; ++row)
                {
                    var kinds = row == State.mapRows - 1 ? new List<ZoneKind> { ZoneKind.Boss, ZoneKind.Boss, ZoneKind.Boss }
                        : row == 0 ? new List<ZoneKind> { ZoneKind.Wildlife, ZoneKind.Wildlife, ZoneKind.Subject }
                        : row == 1 || row == 7 ? new List<ZoneKind> { ZoneKind.Subject, ZoneKind.Wildlife, ZoneKind.Encounter }
                        : row == 2 || row == 6 || row == 9 ? new List<ZoneKind> { ZoneKind.Kiosk, ZoneKind.Encounter, ZoneKind.Subject }
                        : row == 4 || row == 8 ? new List<ZoneKind> { ZoneKind.Campfire, ZoneKind.Campfire, ZoneKind.Encounter }
                        : row == State.mapRows - 2 ? new List<ZoneKind> { ZoneKind.Subject, ZoneKind.Campfire, ZoneKind.Wildlife }
                        : new List<ZoneKind> { ZoneKind.Subject, ZoneKind.Wildlife, ZoneKind.Subject };
                    Shuffle(kinds);
                    for (int lane = 0; lane < 3; ++lane) State.map.Add(new MapNode { id = State.nextMapNodeId++, act = act, row = row, lane = lane, kind = kinds[lane] });
                }
            }
            RefreshMapProximity();
            EnsureWildlifeAssignments();
        }

        public CharacterDef BossForAct(int act)
        {
            if (act < 1 || act > 3) return null;
            var nodes = State.map.Where(n => n.kind == ZoneKind.Boss && n.act == act).OrderBy(n => n.lane).ThenBy(n => n.id).ToList();
            var selected = nodes.FirstOrDefault(n => n.id == State.activeNodeId) ?? nodes.FirstOrDefault(n => n.visited);
            if (selected != null && BossForNode(selected) != null) return BossForNode(selected);
            string previous = State.actBosses != null && State.actBosses.Count >= act ? State.actBosses[act - 1] : null;
            var representative = nodes.FirstOrDefault(n => n.assignedOpponent == previous && BossForNode(n) != null)
                ?? nodes.FirstOrDefault(n => n.lane == 1 && BossForNode(n) != null) ?? nodes.FirstOrDefault(n => BossForNode(n) != null);
            return BossForNode(representative);
        }

        public CharacterDef BossForAct(int act, int lane)
        {
            return BossForNode(State.map.FirstOrDefault(n => n.kind == ZoneKind.Boss && n.act == act && n.lane == lane));
        }

        public CharacterDef BossForNode(MapNode node)
        {
            if (node == null || node.kind != ZoneKind.Boss) return null;
            var character = GameDatabase.Character(node.assignedOpponent);
            return character?.cards != null && character.cards.Length > 0 && GameDatabase.Passive(character.passiveId) != null ? character : null;
        }

        private void EnsureBossAssignments()
        {
            var roster = GameDatabase.Characters.Where(x => x.cards != null && x.cards.Length > 0 && GameDatabase.Passive(x.passiveId) != null).ToList();
            var valid = new HashSet<string>(roster.Select(x => x.id));
            var previous = State.actBosses ?? new List<string>();
            var nodes = State.map.Where(n => n.kind == ZoneKind.Boss && n.act >= 1 && n.act <= 3).OrderBy(n => n.act).ThenBy(n => n.lane).ThenBy(n => n.id).ToList();
            var active = ActiveNode();
            if (active != null && active.kind == ZoneKind.Boss && State.combat != null && valid.Contains(State.combat.enemyId))
                active.assignedOpponent = State.combat.enemyId;

            for (int act = 1; act <= 3; ++act)
            {
                var actNodes = nodes.Where(n => n.act == act).ToList();
                var anchor = actNodes.FirstOrDefault(n => n.id == State.activeNodeId) ?? actNodes.FirstOrDefault(n => n.visited)
                    ?? actNodes.FirstOrDefault(n => n.lane == 1) ?? actNodes.FirstOrDefault();
                if (anchor == null) continue;
                string historical = act <= State.defeatedBosses.Count && valid.Contains(State.defeatedBosses[act - 1]) ? State.defeatedBosses[act - 1] : null;
                string legacy = act <= previous.Count && valid.Contains(previous[act - 1]) ? previous[act - 1] : null;
                if (State.bossAssignmentVersion == 0)
                {
                    // Keep the old revealed opponent on its actual visited/active lane, or the middle path.
                    // A spawned enemy and an explicitly assigned node remain authoritative.
                    if (!valid.Contains(anchor.assignedOpponent))
                        anchor.assignedOpponent = historical ?? legacy ?? anchor.assignedOpponent;
                }
                else if (!valid.Contains(anchor.assignedOpponent) && (anchor.visited || anchor.id == State.activeNodeId))
                    anchor.assignedOpponent = historical ?? legacy;
            }

            // Preserve completed/in-progress fights even when a damaged or unusual legacy save
            // contains a duplicate or a former final boss. Only unvisited alternatives are repaired.
            var preserved = new HashSet<MapNode>(nodes.Where(n => (n.visited || n.id == State.activeNodeId) && valid.Contains(n.assignedOpponent)));
            var used = new HashSet<string>(preserved.Where(n => n.act < 3).Select(n => n.assignedOpponent));
            foreach (var node in nodes.Where(n => n.act < 3 && !preserved.Contains(n)))
            {
                if (!valid.Contains(node.assignedOpponent) || node.assignedOpponent == "nia" || !used.Add(node.assignedOpponent)) node.assignedOpponent = null;
            }

            // This independent seed never advances map, draft, encounter or combat RNG.
            int bossRandom = unchecked(State.seed ^ (int)0xB055A17D ^ State.endlessCycle * 16777619);
            if (bossRandom == 0) bossRandom = 1831565813;
            foreach (var node in nodes.Where(n => n.act < 3 && string.IsNullOrEmpty(n.assignedOpponent)))
            {
                var candidates = roster.Where(x => x.id != "nia" && !used.Contains(x.id)).ToList();
                if (candidates.Count == 0) candidates = roster.Where(x => x.id != "nia").ToList();
                node.assignedOpponent = candidates[Next(ref bossRandom, candidates.Count)].id;
                used.Add(node.assignedOpponent);
            }
            foreach (var node in nodes.Where(n => n.act == 3 && !preserved.Contains(n))) node.assignedOpponent = "nia";
            State.actBosses = Enumerable.Range(1, 3).Select(act => BossForAct(act)?.id).ToList();
            State.bossAssignmentVersion = 1;
        }

        public bool IsNearKiosk(MapNode node)
        {
            return node != null && (node.kind == ZoneKind.Subject || node.kind == ZoneKind.Wildlife)
                && State.map.Any(k => k.act == node.act && k.kind == ZoneKind.Kiosk
                    && Math.Abs(k.row - node.row) == 1 && Math.Abs(k.lane - node.lane) <= 1);
        }
        public void RefreshMapProximity()
        {
            foreach (var node in State.map) node.nearKiosk = IsNearKiosk(node);
        }

        private MapNode ActiveNode() { return State.map.FirstOrDefault(n => n.id == State.activeNodeId); }

        private void RecordBossProgress(bool interrupted)
        {
            if (!interrupted || State.combat?.boss == null) return;
            var phase = BossMechanics.Phase(State.combat);
            ResearchProgression.BossInterrupted(State, State.combat.boss.phase);
            EmitCombatAction(new CombatAction { id = ++actionSerial, cardId = phase?.signatureCardId, kind = "boss_interrupt", enemy = false });
            Say("공략 성공 · " + phase?.name + " 중단. 다음 플레이어 턴 종료까지 보스가 취약해집니다.");
        }

        private void PrioritizeBossSignature(CombatState combat, int available)
        {
            if (combat.boss == null || !combat.boss.challengeActive) return;
            string id = BossMechanics.Phase(combat)?.signatureCardId;
            if (string.IsNullOrEmpty(id) || !combat.enemyDeck.Contains(id) || CardCost(id) > available) return;
            int index = combat.enemyHand.IndexOf(id);
            if (index < 0 && combat.enemyHand.Count > 0)
            {
                var source = combat.enemyDrawPile.Contains(id) ? combat.enemyDrawPile : combat.enemyDiscardPile;
                int sourceIndex = source.LastIndexOf(id); if (sourceIndex < 0) return;
                source.RemoveAt(sourceIndex);
                int last = combat.enemyHand.Count - 1;
                source.Add(combat.enemyHand[last]); combat.enemyHand[last] = id; index = last;
            }
            if (index > 0) { combat.enemyHand.RemoveAt(index); combat.enemyHand.Insert(0, id); }
        }

        private void PrepareBossNextTurn()
        {
            var c = State.combat; var b = c?.boss; if (b == null) return;
            c.enemyOpeningEnergy += Math.Min(1, Math.Max(0, b.preparedEnergy)); b.preparedEnergy = 0;
            var character = GameDatabase.Character(c.enemyId);
            foreach (var ready in b.preparedResources)
            {
                var resource = c.enemySkills.resources.FirstOrDefault(x => x.owner == character.name && x.key == ready.key);
                if (resource == null) { resource = new SkillResource { owner = character.name, key = ready.key }; c.enemySkills.resources.Add(resource); }
                resource.label = ready.label; resource.cap = Math.Max(1, ready.cap); resource.isState = ready.isState;
                resource.amount = ready.isState ? 1 : Math.Max(resource.amount, Math.Min(resource.cap, resource.amount + Math.Max(0, ready.amount)));
            }
            b.preparedResources.Clear();
            if (c.turn > b.counterUntilTurn) b.counterDamage = 0;
        }

        private void QueueBossResource(BossPhaseDef phase, int amount)
        {
            var c = State.combat; var character = GameDatabase.Character(c.enemyId);
            if (character == null || string.IsNullOrEmpty(phase.resourceKey)) return;
            var rules = character.cards.Select(GameDatabase.Card).Where(card => card?.mechanics != null)
                .SelectMany(card => card.mechanics.rules ?? new SkillRule[0])
                .Where(rule => rule.key == phase.resourceKey && new[] { "gain", "set", "state_on", "state_toggle" }.Contains(rule.op)).ToList();
            if (rules.Count == 0) return;
            bool state = SkillMechanics.IsState(character.name, phase.resourceKey) || rules.Any(rule => rule.isState);
            int cap = state ? 1 : Math.Max(1, rules.Max(rule => rule.cap));
            var ready = c.boss.preparedResources.FirstOrDefault(x => x.key == phase.resourceKey);
            if (ready == null) { ready = new BossPreparedResource { key = phase.resourceKey }; c.boss.preparedResources.Add(ready); }
            ready.label = state ? SkillMechanics.StateName(character.name, phase.resourceKey, phase.resourceLabel) : phase.resourceLabel ?? phase.resourceKey;
            ready.isState = state; ready.cap = cap; ready.amount = state ? 1 : Math.Min(2, Math.Max(ready.amount, amount));
        }

        private void ResolveBossSpecial()
        {
            var c = State.combat; var b = c.boss; var phase = BossMechanics.Phase(c);
            bool failure = b.challengeActive && !b.interrupted;
            // Commit before applying effects: save/load, retaliation and damage callbacks cannot replay this action.
            b.specialResolved = true; b.challengeActive = false;
            int damageBudget = BossMechanics.MaxExtraDamage, supportBudget = BossMechanics.MaxSupport;
            if (b.delayedDamage > 0 && c.turn >= b.delayedDueTurn)
            {
                int amount = Math.Min(damageBudget, b.delayedDamage); damageBudget -= amount;
                string source = b.delayedSourceCard;
                b.delayedDamage = b.delayedDueTurn = 0;
                b.delayedSourceCard = null;
                ResolveBossStrike(amount, "boss_delayed", source ?? phase?.signatureCardId, true);
            }
            if (!failure || State.hp <= 0 || c.enemyHp <= 0 || phase == null) return;
            ApplyBossEffect(phase.effect, phase.effectAmount, phase, ref damageBudget, ref supportBudget);
            if (State.hp > 0) ApplyBossEffect(phase.secondaryEffect, phase.secondaryAmount, phase, ref damageBudget, ref supportBudget);
            Say("보스 특수행동 · " + phase.name + " · " + BossMechanics.ShortEffectsText(phase));
            RefreshEnemyIntent();
        }

        private int BossStrikeDamage(int amount, int weak, int vulnerable, StatusActorState attacker, StatusActorState defender)
        {
            if (weak > 0) amount = amount * 3 / 4;
            if (vulnerable > 0) amount = amount * 3 / 2;
            return RunModes.ScaleEnemyDirectDamage(State, StatusMechanics.Damage(attacker, defender, amount));
        }

        private void ResolveBossStrike(int amount, string kind, string source, bool evadable)
        {
            var c = State.combat; var card = GameDatabase.Card(source);
            // Receiving a boss strike uses the same defensive hooks as a skill hit. It is
            // not another card use and must not recursively trigger retaliation.
            var incoming = TraitEvent(false, "before_incoming", card, true);
            bool accuracyMiss = false;
            // Special mechanics are separate attacks, not the source card's targeted cast.
            bool landed = !evadable || TryDirectHit(null, true, out accuracyMiss);
            int defenderBlock = c.block, avoided = !landed && !accuracyMiss ? 1 : 0, missed = accuracyMiss ? 1 : 0;
            int damage = !landed ? 0 : Math.Max(0, BossStrikeDamage(amount, c.enemyWeak, c.vulnerable, c.enemyStatuses, c.playerStatuses) - incoming.damageReduction);
            int absorbed = Math.Min(c.block, damage); c.block -= absorbed; damage = TakeHealthDamage(false, damage - absorbed);
            StatusMechanics.DamageTaken(c.playerStatuses, damage);
            EmitCombatAction(new CombatAction { id = ++actionSerial, cardId = source, kind = kind, enemy = true, damage = damage, avoided = avoided, missed = missed });
            if (landed) TraitEvent(false, "incoming_hit", card, true, damage);
            if (avoided > 0) TraitEvent(false, "evade", card, true, avoided: avoided);
            if (damage > 0) { TraitEvent(false, "damaged", card, true, damage); TraitEvent(false, "low_health", card, true, damage); }
            if (defenderBlock > 0 && c.block == 0) TraitEvent(false, "block_break", card, landed, damage);
        }

        private void ApplyBossEffect(BossEffect effect, int rawAmount, BossPhaseDef phase, ref int damageBudget, ref int supportBudget)
        {
            var c = State.combat; var b = c.boss; int amount = BossMechanics.Amount(effect, rawAmount), healed = 0, block = 0;
            switch (effect)
            {
                case BossEffect.None: return;
                case BossEffect.Strike:
                    amount = Math.Min(damageBudget, amount); damageBudget -= amount;
                    if (amount > 0) ResolveBossStrike(amount, "boss_strike", phase.signatureCardId, true); return;
                case BossEffect.Shield: block = Math.Min(supportBudget, amount); supportBudget -= block; c.enemyBlock += block; break;
                case BossEffect.Heal:
                    amount = Math.Min(supportBudget, amount); supportBudget -= amount;
                    int before = c.enemyHp; HealEnemy(amount); healed = c.enemyHp - before; break;
                case BossEffect.DelayedStrike:
                    b.delayedDamage = Math.Min(16, b.delayedDamage + amount); b.delayedDueTurn = c.turn + 1; b.delayedSourceCard = phase.signatureCardId; break;
                case BossEffect.Prepare: b.preparedEnergy = 1; break;
                case BossEffect.ResourcePrepare: QueueBossResource(phase, amount); break;
                case BossEffect.Status:
                    if (phase.statusKey == "weak") c.weak = Math.Max(c.weak, 1);
                    else if (phase.statusKey == "vulnerable") c.vulnerable = Math.Max(c.vulnerable, 1);
                    else if (BossMechanics.StatusKeys.Contains(phase.statusKey)) StatusMechanics.Add(c.playerStatuses, phase.statusKey, phase.signatureCardId, 1);
                    break;
                case BossEffect.Counter: b.counterDamage = amount; b.counterUntilTurn = c.turn + 1; break;
                case BossEffect.Drain: b.pendingDrain = 1; break;
                case BossEffect.Evasion: c.enemyEvasion = Math.Max(c.enemyEvasion, Math.Min(65, BaseEnemyEvasion() + amount)); c.enemyEvasionTurns = Math.Max(c.enemyEvasionTurns, 1); break;
                case BossEffect.Cleanse:
                    c.enemyPoison = c.enemyWeak = c.enemyVulnerable = 0; c.enemyStatuses.effects.RemoveAll(x => !x.pending);
                    c.enemyTraits.buffs.RemoveAll(x => x.kind == "heal_reduction" || x.kind == "exposure"); break;
            }
            EmitCombatAction(new CombatAction { id = ++actionSerial, cardId = phase.signatureCardId, kind = "boss_" + effect.ToString().ToLowerInvariant(), enemy = true, block = block, heal = healed });
        }

        private void StartBattle(bool wildlife, bool boss)
        {
            SyncWeaponCards();
            var node = ActiveNode();
            int progress = (State.act - 1) * State.mapRows + node.row;
            int level = Math.Min(20, 1 + progress * 19 / Math.Max(1, State.mapRows * 3 - 1));
            if (State.mode == RunMode.Endless && State.endlessCycle > 0) level = 20;
            var c = new CombatState { enemyLevel = level, enemyEnergy = EnergyForLevel(level), strength = Modifier("battle_start_strength"), block = Modifier("battle_start_block") };
            State.combat = c; State.stage = RunStage.Combat;
            c.playerSkills=SkillMechanics.Ensure(c.playerSkills);c.enemySkills=SkillMechanics.Ensure(c.enemySkills);
            c.playerTraits=TraitMechanics.NewBattle(State.persistentTraits);State.persistentTraits=c.playerTraits;
            if (wildlife)
            {
                string[] animals = { "chicken", "dog", "boar", "wolf", "bear" };
                string[] names = { "닭", "들개", "멧돼지", "늑대", "곰" };
                int[] health = { 20, 28, 38, 44, 62 };
                int pick = Next(animals.Length);
                if (!string.IsNullOrEmpty(node.assignedAnimal) && Array.IndexOf(animals, node.assignedAnimal) >= 0) pick = Array.IndexOf(animals, node.assignedAnimal);
                AssignWildlifeVariant(node);c.mutantAnimal=IsMutantWildlife(node);
                c.animal = c.enemyId = animals[pick]; c.enemyName = (c.mutantAnimal ? "변이 " : "") + names[pick];
                c.enemyMaxHp = (int)Math.Ceiling((health[pick] + level * 3) * (1.15f + (State.act-1)*.12f)); c.enemyDeck.Add("basic_attack");
            }
            else
            {
                var combatRoster = GameDatabase.Characters.Where(x => x.cards != null && x.cards.Length > 0).ToList();
                if (boss && BossForNode(node) == null) EnsureBossAssignments();
                var character = boss ? BossForNode(node) : combatRoster[Next(combatRoster.Count)];
                if (boss) State.actBosses[State.act - 1] = character.id;
                if (!boss && !string.IsNullOrEmpty(node.assignedOpponent)) character = combatRoster.FirstOrDefault(x => x.id == node.assignedOpponent) ?? character;
                c.enemyId = character.id; c.enemyName = (boss ? "보스 · " : "") + character.name;
                ResearchProgression.ObserveSubject(State, character.id);
                if (boss && BossMechanics.Profile(character.id) != null) c.boss = new BossBattleState();
                c.enemyMaxHp = (int)Math.Ceiling((boss ? 80 + level * 8 : 35 + level * 6) * (boss ? 1.40f + (State.act-1)*.40f : 1.15f + (State.act-1)*.30f) + (State.act-1)*15);
                foreach (var id in character.cards)
                {
                    var card = GameDatabase.Card(id); if (card == null) continue;
                    int unlock = card.key == "R" ? 7 : card.key == "E" ? 3 : card.key == "W" ? 2 : 1;
                    if (level >= unlock) c.enemyDeck.Add(id);
                }
                c.enemyPassiveId=character.passiveId;
                var availableClasses=character.weaponClasses.Where(x=>WeaponIdentity.CardFor(x)!=null).ToArray();
                c.enemyWeaponClass=availableClasses.Length>0?availableClasses[Next(availableClasses.Length)]:"글러브";
                c.enemyWeaponCardId=WeaponIdentity.CardFor(c.enemyWeaponClass);
                var tacticals=GameDatabase.Cards.Where(x=>x.category=="tactical").ToArray();
                c.enemyTacticalCardId=tacticals[Next(tacticals.Length)].id;
                // Basics enable passives and fill the gaps between larger skill investments.
                c.enemyDeck.AddRange(new[]{"basic_attack","basic_attack","basic_attack","basic_guard"});
                c.enemyDeck.Add(c.enemyWeaponCardId);c.enemyDeck.Add(c.enemyTacticalCardId);
                EquipEnemy(c,boss);
                c.enemyMaxHp+=EnemyEquipmentSum(g=>g.health);
                c.enemyEvasion = BaseEnemyEvasion();
            }
            // Difficulty is applied only to a newly spawned opponent, never to a restored battle.
            c.enemyMaxHp = WildlifeMechanics.ScaleHealth(RunModes.ScaleEnemyHealth(State, c.enemyMaxHp),c.mutantAnimal);
            c.enemyHp = c.enemyMaxHp;
            c.drawPile.AddRange(State.deck); Shuffle(c.drawPile);
            c.enemyDrawPile.AddRange(c.enemyDeck); Shuffle(c.enemyDrawPile);
            Heal(Modifier("battle_start_heal"));
            RecordCombatInformation(new CombatLogEntry { kind = "battle_start", title = "전투 시작", detail = c.enemyName + " LV." + c.enemyLevel + " · 상대 체력 " + c.enemyHp + "/" + c.enemyMaxHp });
            TraitEvent(false,"battle_start",null,true);TraitEvent(true,"battle_start",null,true);
            c.playerOpeningEnergy=c.energy;c.enemyOpeningEnergy=c.enemyAvailableEnergy;
            Say(c.enemyName + " Lv." + level + " 전투 시작");
            StartPlayerTurn(true);
        }

        private void EquipEnemy(CombatState combat,bool boss)
        {
            int count=boss?(combat.enemyLevel>=16?5:combat.enemyLevel>=10?3:2):combat.enemyLevel>=18?5:combat.enemyLevel>=14?3:combat.enemyLevel>=8?1:0;
            if(count==0)return;
            var weapons=GameDatabase.Gear.Where(x=>x.slot==GearSlot.Weapon && x.weaponClass==combat.enemyWeaponClass && (combat.enemyLevel>=17 || x.rarity!="초월")).ToList();
            if(weapons.Count>0)combat.enemyGear.Add(weapons[Next(weapons.Count)].id);
            var slots=new[]{GearSlot.Clothes,GearSlot.Head,GearSlot.Arm,GearSlot.Legs}.ToList();Shuffle(slots);
            foreach(var slot in slots.Take(Math.Max(0,count-combat.enemyGear.Count)))
            {
                var candidates=GameDatabase.Gear.Where(x=>x.slot==slot && (combat.enemyLevel>=17 || x.rarity!="초월")).ToList();
                combat.enemyGear.Add(candidates[Next(candidates.Count)].id);
            }
        }
        private int EnemyEquipmentSum(Func<GearDef,int> value)
        {
            return State.combat==null || State.combat.enemyGear==null?0:State.combat.enemyGear.Select(GameDatabase.Equipment).Where(x=>x!=null).Sum(value);
        }
        private int ControlEnergyPenalty(bool enemy,StatusActorState statuses)
        {
            int resistance=enemy?EnemyEquipmentSum(g=>g.controlResistance):EquipmentSum(g=>g.controlResistance);
            return Math.Max(0,StatusMechanics.EnergyPenalty(statuses)-Math.Min(1,resistance));
        }
        private int TakeHealthDamage(bool enemy,int damage)
        {
            var combat=State.combat;int hp=enemy?combat.enemyHp:State.hp;
            var companion=CompanionMechanics.Absorb(enemy?combat.enemySkills:combat.playerSkills,damage);
            damage=companion.remaining;
            if(companion.absorbed>0)
            {
                EmitCombatAction(new CombatAction { id=++actionSerial, cardId="chloe_r", kind="companion_damage", enemy=!enemy, companionAbsorbed=companion.absorbed });
                Say((enemy?combat.enemyName:"하나")+"의 니나가 피해 "+companion.absorbed+"을 대신 받았습니다."+(companion.destroyed?" 니나가 파괴되었습니다.":""));
            }
            int deferral=Math.Min(20,enemy?EnemyEquipmentSum(g=>g.damageDeferral):EquipmentSum(g=>g.damageDeferral));
            int delayed=Math.Max(0,damage)*deferral/100;
            if(delayed>0)(enemy?combat.enemyDeferredDamage:combat.playerDeferredDamage).Add(new DeferredDamage{amount=delayed,remaining=3});
            int immediate=Math.Min(hp,Math.Max(0,damage-delayed));
            if(enemy)combat.enemyHp-=immediate;else State.hp-=immediate;
            if (enemy) { BossMechanics.CheckPhase(combat); RecordBossProgress(BossMechanics.ObserveDamage(combat, immediate)); }
            return immediate;
        }
        private void ResolveDeferredDamage(bool enemy)
        {
            var combat=State.combat;var pending=enemy?combat.enemyDeferredDamage:combat.playerDeferredDamage;
            int total=0;
            foreach(var debt in pending){int amount=(debt.amount+debt.remaining-1)/Math.Max(1,debt.remaining);debt.amount-=amount;debt.remaining--;total+=amount;}
            pending.RemoveAll(x=>x.amount<=0 || x.remaining<=0);
            int damage=Math.Min(enemy?combat.enemyHp:State.hp,total);
            if(enemy)combat.enemyHp-=damage;else State.hp-=damage;
            if(damage<=0)return;
            EmitCombatAction(new CombatAction{id=++actionSerial,cardId="ao_dai",kind="deferred_damage",enemy=!enemy,damage=damage});
            Say((enemy?combat.enemyName:"하나")+" 유예 피해 "+damage);
            TraitEvent(enemy,"damaged",null,true,damage);TraitEvent(enemy,"low_health",null,true,damage);
        }

        private void StartPlayerTurn(bool first = false)
        {
            var c = State.combat;
            int retained = !first && !string.IsNullOrEmpty(c.retainedCardId) ? 1 : 0;
            // Restore the reserved copy before any turn-start draws, so those draws respect its hand slot.
            if (retained > 0) c.hand.Add(c.retainedCardId);
            if (!first) retained += RestoreChargedCards();
            c.retainedCardId = null; c.retainIndex = -1;
            c.turn++; c.energy = MaxEnergy + Modifier("turn_energy")+(first?c.playerOpeningEnergy:0); c.playerOpeningEnergy=0; c.attacksPlayed = 0;
            RecordCombatInformation(new CombatLogEntry { kind = "turn_start", title = c.turn + "턴 시작", detail = "코스트·필드·턴 시작 효과를 갱신합니다." });
            ResetAugmentTurn();
            PrepareBossNextTurn(); BossMechanics.BeginPlayerTurn(c);
            if (!first) SkillMechanics.StartTurn(c.playerSkills);
            if (!first) CompanionMechanics.StartTurn(c.playerSkills);
            TraitMechanics.StartTurn(c.playerTraits);
            c.enemyTurn = false; c.freeCasts.Clear(); c.freeCastSources.Clear();
            if (!first) c.block = 0;
            c.block += TotalBlock + Modifier("turn_block"); Heal(Modifier("turn_heal"));
            RecordBossProgress(BossMechanics.ObserveBlock(c, TotalBlock + Modifier("turn_block")));
            TraitEvent(false,"turn_start",null,true);
            ResolveDeferredDamage(false);
            c.energy=Math.Max(1,c.energy-ControlEnergyPenalty(false,c.playerStatuses));
            if (c.boss != null && c.boss.pendingDrain > 0) { c.energy = Math.Max(1, c.energy - Math.Min(1, c.boss.pendingDrain)); c.boss.pendingDrain = 0; }
            if (c.poison > 0)
            {
                int damage = TakeHealthDamage(false,c.poison);
                Say("하나 중독 피해 " + damage); c.poison--;
                EmitCombatAction(new CombatAction { id = ++actionSerial, cardId = "status_poison", enemy = true, damage = damage });
                if(damage>0){TraitEvent(false,"damaged",null,true,damage);TraitEvent(false,"low_health",null,true,damage);}
            }
            if (CheckBattleEnd()) return;
            AugmentTurnStart(first);
            DrawCards(Math.Max(0, 5 + Modifier("turn_draw") + AugmentOpeningDraw() - retained));
            FreeCastMechanics.ActivatePending(c.pendingFreeCasts, c.turn, State.deck, c.freeCasts, c.freeCastSources, c.hand, c.drawPile, c.discardPile);
            PlanEnemyTurn();
            Say(c.turn + "턴 · 코스트 " + c.energy + "/" + MaxEnergy);
        }

        private void PlanEnemyTurn()
        {
            var c = State.combat; c.enemyPlan.Clear(); c.enemyPlanCosts.Clear(); c.enemyPlanFreeCast.Clear(); c.enemyAdvancedActions.Clear(); c.enemyHand.Clear();
            c.enemyActionIndex = 0; c.enemyEnergy = EnergyForLevel(c.enemyLevel)+TraitMechanics.EnergyBonus(c.enemyTraits)+c.enemyOpeningEnergy;
            if (!string.IsNullOrEmpty(c.animal))
            {
                PlanWildlifeTurn();
            }
            else
            {
                foreach(var held in c.enemyChargedCards.Where(x=>x.readyTurn<=c.turn))c.enemyHand.Add(held.reference);
                DrawInto(c.enemyHand, c.enemyDrawPile, c.enemyDiscardPile, Math.Max(0,4 + c.enemyLevel / 8-c.enemyHand.Count));
                int available = c.enemyEnergy;
                var grants = new List<FreeCastGrant>(); var sources = new List<string>();
                FreeCastMechanics.ActivatePending(c.enemyPendingFreeCasts, c.turn, c.enemyDeck, grants, sources, c.enemyHand, c.enemyDrawPile, c.enemyDiscardPile, true, false);
                var history = c.enemyLastSkills.Select(x => new SkillHistory { owner = x.owner, cardId = x.cardId }).ToList();
                var skillPlan = SkillMechanics.Clone(c.enemySkills);
                SkillMechanics.StartTurn(skillPlan);
                CompanionMechanics.StartTurn(skillPlan);
                var traitPlan=TraitMechanics.Clone(c.enemyTraits);TraitMechanics.StartTurn(traitPlan);
                var planStatuses=StatusMechanics.Clone(c.enemyStatuses);
                int planStrength=c.enemyStrength,planWeak=c.enemyWeak,planHp=c.enemyHp,planTargetHp=State.hp,planTargetBlock=c.block,planBlock=EnemyEquipmentSum(g=>g.block);
                var planDefenderSkills=SkillMechanics.Clone(c.playerSkills);
                var turnContext=TraitContextFor(true,null,true,skillActor:skillPlan);
                var opening=TraitMechanics.Resolve(traitPlan,ActorTraits(true),"turn_start",turnContext);available+=opening.pulses.Where(x=>x.kind=="energy").Sum(x=>x.amount);
                foreach(var pulse in opening.pulses){if(pulse.kind=="strength")planStrength+=pulse.amount;if(pulse.kind=="heal")planHp=Math.Min(c.enemyMaxHp,planHp+pulse.amount*(100-TraitMechanics.HealingReduction(traitPlan))/100);if(pulse.kind=="cleanse")planWeak=0;}
                if(opening.pulses.Any(x=>x.kind=="cleanse"))planStatuses.effects.RemoveAll(x=>!x.pending);
                available=Math.Max(1,available-ControlEnergyPenalty(true,planStatuses));c.enemyEnergy=available;
                PrioritizeBossSignature(c, available);
                // Plan against real piles, including draws and target recalls, so the preview and actual turn agree.
                // Moving each planned card into a pile now preserves a finite deck even when a reset reuses a copy.
                while (c.enemyPlan.Count < 40)
                {
                    int index = c.enemyHand.FindIndex(id => StatusMechanics.CanUse(planStatuses,GameDatabase.Card(id)) && (AdvancedCardMechanics.IsX(GameDatabase.Card(id)) || HasUnplannedEnemyCharge(id) || (grants.Any(g => g.cardId == id && g.uses > 0) ? 0 : Math.Max(0, CardCost(id) + StatusMechanics.CostPenalty(planStatuses,GameDatabase.Card(id)) - Math.Max(SkillMechanics.Discount(skillPlan,id),SkillMechanics.Discount(traitPlan.skills,id)))) <= available));
                    if (index < 0) break;
                    string id = c.enemyHand[index]; var card = GameDatabase.Card(id);
                    if (card == null) { c.enemyHand.RemoveAt(index); continue; }
                    bool freeGranted = grants.Any(g => g.cardId == id && g.uses > 0);
                    bool garnetRecall = FreeCastMechanics.GarnetRecallBeforeExhaust(card, freeGranted, sources);
                    bool garnetExecution = FreeCastMechanics.GarnetExecutionReady(card, skillPlan, freeGranted, planTargetHp, State.maxHp);
                    int price = freeGranted ? 0 : Math.Max(0, CardCost(id) + StatusMechanics.CostPenalty(planStatuses,card) - Math.Max(SkillMechanics.Discount(skillPlan,id),SkillMechanics.Discount(traitPlan.skills,id)));
                    var advanced=PlanAdvancedEnemyAction(card,index,available);
                    if(advanced.charge>0)freeGranted=false;
                    price=AdvancedEnemyPrice(card,price,available,freeGranted,advanced);
                    advanced.x=AdvancedCardMechanics.IsX(card)?freeGranted?0:price:0;
                    index=c.enemyHand.IndexOf(id);
                    if(advanced.mode=="charge")
                    {
                        available-=price;c.enemyHand.RemoveAt(index);c.enemyPlan.Add(id);c.enemyPlanCosts.Add(price);c.enemyPlanFreeCast.Add(freeGranted);c.enemyAdvancedActions.Add(advanced);
                        ConsumeFreeCast(grants,id);SkillMechanics.ConsumeDiscount(skillPlan,id);SkillMechanics.ConsumeDiscount(traitPlan.skills,id);
                        planBlock+=AdvancedCardMechanics.ChargeGuard(card);continue;
                    }
                    card=AdvancedCardMechanics.PreparedCard(card,advanced.mode,advanced.charge,advanced.sacrificed,advanced.x);
                    if(AdvancedCardMechanics.IsEmptyX(card))
                    {
                        ConsumeFreeCast(grants,id);SkillMechanics.ConsumeDiscount(skillPlan,id);SkillMechanics.ConsumeDiscount(traitPlan.skills,id);
                        c.enemyHand.RemoveAt(index);c.enemyPlan.Add(id);c.enemyPlanCosts.Add(0);c.enemyPlanFreeCast.Add(freeGranted);c.enemyAdvancedActions.Add(advanced);
                        if(card.exhaust)c.enemyExhaustPile.Add(id);else c.enemyDiscardPile.Add(id);continue;
                    }
                    if(advanced.charge==0)
                    {
                        ConsumeFreeCast(grants, id);
                        SkillMechanics.ConsumeDiscount(skillPlan, id);
                        SkillMechanics.ConsumeDiscount(traitPlan.skills,id);
                    }
                    var before = SkillMechanics.Clone(skillPlan);
                    var traitBefore=SkillMechanics.Clone(traitPlan.skills);
                    var bonuses=AddAdvancedBonus(SkillMechanics.Bonuses(before,card),EnemyAdvancedBonus(card,advanced,planHp,planBlock));
                    var beforeTraits=ResolveBeforeTraits(traitPlan,true,card,card.damage>0 || bonuses.damage>0,before,planHp);
                    int planExtra=AdvancedCardMechanics.SuppressesAttack(card)?0:bonuses.damage;
                    if(card.damage>0 || planExtra>0)planExtra+=beforeTraits.bonus.damage+TraitMechanics.DamageBonus(traitPlan)+TraitMechanics.EmpowerBonus(traitPlan,card);
                    if(planWeak>0)planExtra=planExtra*3/4;if(c.vulnerable>1)planExtra=planExtra*3/2;
                    int perHit=CalculateEnemyDamage(card,planStrength,Math.Max(0,c.vulnerable-1),weak:planWeak);
                    if (SkillMechanics.BreaksBlock(before, card)) planTargetBlock = 0;
                    int actualDamage=garnetExecution?planTargetHp:SimulateSharedDirectDamage(planDefenderSkills,planTargetHp,ref planTargetBlock,perHit,planExtra,Math.Max(1,card.hits));planTargetHp-=actualDamage;
                    int healed=Math.Min(c.enemyMaxHp-planHp,(EnemyAuxiliaryValue(card,card.heal)+bonuses.heal)*(100-Math.Max(TraitMechanics.HealingReduction(traitPlan),StatusMechanics.HealingReduction(c.enemyStatuses)))/100);planHp+=healed;planStrength+=card.strength;
                    available+=beforeTraits.pulses.Where(x=>x.kind=="energy").Sum(x=>x.amount);
                    available -= price; available += card.energy;
                    c.enemyHand.RemoveAt(index); c.enemyPlan.Add(id); c.enemyPlanCosts.Add(price); c.enemyPlanFreeCast.Add(freeGranted);
                    c.enemyAdvancedActions.Add(advanced);planBlock+=EnemyAuxiliaryValue(card,card.block)+bonuses.block;
                    if(advanced.charge==0) { if (card.exhaust && !garnetRecall) c.enemyExhaustPile.Add(id); else c.enemyDiscardPile.Add(id); }
                    DrawInto(c.enemyHand, c.enemyDrawPile, c.enemyDiscardPile, card.draw);
                    RecordLastSkill(history, card);
                    SkillMechanics.AfterCard(skillPlan, before, card, true, paidCost: price);
                    CompanionMechanics.AfterCard(skillPlan,card);
                    var afterTraits=SimulateAfterTraits(traitPlan,true,card,before,actualDamage,EnemyAuxiliaryValue(card,card.block)+bonuses.block,healed,planHp,planTargetHp,planTargetBlock);
                    available+=afterTraits.pulses.Where(x=>x.kind=="energy").Sum(x=>x.amount);
                    foreach(var pulse in beforeTraits.pulses.Concat(afterTraits.pulses)){if(pulse.kind=="strength")planStrength+=pulse.amount;if(pulse.kind=="heal" || pulse.kind=="bonus_heal")planHp=Math.Min(c.enemyMaxHp,planHp+pulse.amount*(100-TraitMechanics.HealingReduction(traitPlan))/100);}
                    RecallSkillDiscounts(skillPlan, before, c.enemyDeck, c.enemyHand, c.enemyDrawPile, c.enemyDiscardPile);
                    RecallSkillDiscounts(traitPlan.skills,traitBefore,c.enemyDeck,c.enemyHand,c.enemyDrawPile,c.enemyDiscardPile);
                    GrantFreeCasts(card, c.enemyDeck, grants, sources, c.enemyHand, c.enemyDrawPile, c.enemyDiscardPile, true, LastSkill(history, card.owner), before, true);
                }
                c.enemyDiscardPile.AddRange(c.enemyHand); c.enemyHand.Clear();
            }
            RefreshEnemyIntent();
        }

        private int EnemyCardDamage(CardDef card)
        {
            return CalculateEnemyDamage(card, State.combat.enemyStrength, State.combat.vulnerable);
        }
        private int EnemyDirectHitTotal(int perHit, int bonus, int hits)
        {
            return RunModes.ScaleEnemyDirectDamage(State, perHit + bonus) + RunModes.ScaleEnemyDirectDamage(State, perHit) * Math.Max(0, hits - 1);
        }
        private int EnemyAuxiliaryValue(CardDef card,int value)
        {
            // D/F are available from the opening fight; their power grows with the enemy's level.
            return card!=null && (card.category=="weapon" || card.category=="tactical")?value*Math.Min(100,50+State.combat.enemyLevel*5)/100:value;
        }
        private int EnemyBonusDamage(CardDef card, SkillActorState actor, int vulnerable)
        {
            int damage = SkillMechanics.Bonuses(actor, card).damage;
            if (State.combat.enemyWeak > 0) damage = damage * 3 / 4;
            if (vulnerable > 0) damage = damage * 3 / 2;
            damage=damage*(100+TraitMechanics.Exposure(State.combat.playerTraits))/100;
            return StatusMechanics.Damage(State.combat.enemyStatuses,State.combat.playerStatuses,damage);
        }
        private int EnemyTotalBonusDamage(CardDef card,SkillActorState skillActor,TraitActorState traits,int vulnerable,int advanced=0)
        {
            if(AdvancedCardMechanics.SuppressesAttack(card))return 0;
            int bonus=SkillMechanics.Bonuses(skillActor,card).damage+advanced;
            if(card.damage<=0 && bonus<=0 && TraitMechanics.EmpowerBonus(traits,card)<=0)return 0;
            bonus+=ResolveBeforeTraits(TraitMechanics.Clone(traits),true,card,true,skillActor).bonus.damage+TraitMechanics.DamageBonus(traits)+TraitMechanics.EmpowerBonus(traits,card);
            if(State.combat.enemyWeak>0)bonus=bonus*3/4;if(vulnerable>0)bonus=bonus*3/2;
            return StatusMechanics.Damage(State.combat.enemyStatuses,State.combat.playerStatuses,bonus*(100+TraitMechanics.Exposure(State.combat.playerTraits))/100);
        }

        private int CalculateEnemyDamage(CardDef card, int strength, int vulnerable,int exposure=-1,int weak=-1,StatusActorState attackerStatuses=null,StatusActorState defenderStatuses=null)
        {
            if (card == null || card.damage <= 0) return 0;
            var c = State.combat;
            int value;
            if (!string.IsNullOrEmpty(c.animal))
            {
                value = WildlifeMechanics.BasicDamage(c.animal,c.enemyLevel);
                if(WildlifeMechanics.BelongsTo(card.id,c.animal))value+=card.damage;
                value=WildlifeMechanics.ScaleAttack(value,c.mutantAnimal);
            }
            else value = EnemyAuxiliaryValue(card,card.damage) + c.enemyLevel / 2 + strength + EnemyEquipmentSum(g=>g.attack);
            if ((weak>=0?weak:c.enemyWeak) > 0) value = value * 3 / 4;
            if (vulnerable > 0) value = value * 3 / 2;
            value=value*(100+(exposure>=0?exposure:TraitMechanics.Exposure(c.playerTraits)))/100;
            return StatusMechanics.Damage(attackerStatuses ?? c.enemyStatuses,defenderStatuses ?? c.playerStatuses,value);
        }

        private int BossSpecialDamageForecast()
        {
            var c = State.combat; if (!BossMechanics.NeedsSpecialAction(c, BossMechanics.PreviewAtTurnEnd(c))) return 0;
            var phase = BossMechanics.Phase(c); if (phase == null) return 0;
            int weak = c.enemyWeak, vulnerable = c.enemyTurn ? c.vulnerable : Math.Max(0, c.vulnerable - 1), actorHp = c.enemyHp, defenderHp = State.hp, defenderBlock = c.block;
            var attacker = StatusMechanics.Clone(c.enemyStatuses); var defender = StatusMechanics.Clone(c.playerStatuses);
            var traits = TraitMechanics.Clone(c.enemyTraits); var defenderTraits = TraitMechanics.Clone(c.playerTraits);
            if (!c.enemyTurn)
            {
                StatusMechanics.EndTurn(defender); TraitMechanics.EndTurn(defenderTraits);
                var skills = SkillMechanics.Clone(c.enemySkills); SkillMechanics.StartTurn(skills);
                TraitMechanics.StartTurn(traits);
                var opening = TraitMechanics.Resolve(traits, ActorTraits(true), "turn_start", TraitContextFor(true, null, true, skillActor: skills));
                if (opening.pulses.Any(pulse => pulse.kind == "cleanse")) { weak = 0; attacker.effects.RemoveAll(x => !x.pending); }
            }
            int unusedBlock;
            return PreviewBossSpecial(ref actorHp, ref defenderHp, ref defenderBlock, ref weak, ref vulnerable, traits, defenderTraits, attacker, defender, out unusedBlock);
        }

        private int PreviewBossDefenderTrigger(string trigger, CardDef card, int damage, ref int actorHp, ref int defenderHp, ref int defenderBlock,
            ref int weak, ref int vulnerable, TraitActorState attackerTraits, TraitActorState defenderTraits,
            SkillActorState defenderSkills, StatusActorState attackerStatuses, StatusActorState defenderStatuses)
        {
            var context = TraitContextFor(false, card, true, damage, skillActor: defenderSkills, hp: defenderHp);
            context.targetHp = actorHp;
            var result = TraitMechanics.Resolve(defenderTraits, ActorTraits(false), trigger, context);
            int bonusBlock = result.bonus.block, bonusHeal = result.bonus.heal;
            foreach (var pulse in result.pulses)
            {
                if (pulse.kind.StartsWith("status_", StringComparison.Ordinal)) StatusMechanics.Add(attackerStatuses, pulse.kind.Substring(7), pulse.sourceCard, pulse.duration);
                switch (pulse.kind)
                {
                    case "block": defenderBlock += pulse.amount; break;
                    case "bonus_block": int block = Math.Min(bonusBlock, pulse.amount); bonusBlock -= block; defenderBlock += block; break;
                    case "heal":
                        defenderHp = Math.Min(State.maxHp, defenderHp + pulse.amount * (100 - Math.Max(TraitMechanics.HealingReduction(defenderTraits), StatusMechanics.HealingReduction(defenderStatuses))) / 100); break;
                    case "bonus_heal":
                        int heal = Math.Min(bonusHeal, pulse.amount); bonusHeal -= heal;
                        defenderHp = Math.Min(State.maxHp, defenderHp + heal * (100 - Math.Max(TraitMechanics.HealingReduction(defenderTraits), StatusMechanics.HealingReduction(defenderStatuses))) / 100); break;
                    case "weak": weak = Math.Max(weak, pulse.amount); break;
                    case "cleanse":
                        vulnerable = 0; defenderStatuses.effects.RemoveAll(x => !x.pending);
                        defenderTraits.buffs.RemoveAll(x => x.kind == "heal_reduction" || x.kind == "exposure"); break;
                    case "heal_reduction": case "exposure":
                        var buff = attackerTraits.buffs.FirstOrDefault(x => x.kind == pulse.kind && x.sourceId == pulse.sourceId);
                        if (buff == null) { buff = new TraitBuff { kind = pulse.kind, sourceId = pulse.sourceId }; attackerTraits.buffs.Add(buff); }
                        buff.amount = Math.Max(buff.amount, pulse.amount); buff.remaining = Math.Max(buff.remaining, Math.Min(2, pulse.duration)); break;
                    case "execute": actorHp = 0; break;
                }
            }
            return result.damageReduction;
        }

        private int PreviewBossStrike(int amount, CardDef card, ref int actorHp, ref int defenderHp, ref int defenderBlock,
            ref int weak, ref int vulnerable, TraitActorState attackerTraits, TraitActorState defenderTraits,
            SkillActorState defenderSkills, StatusActorState attackerStatuses, StatusActorState defenderStatuses)
        {
            int reduction = PreviewBossDefenderTrigger("before_incoming", card, 0, ref actorHp, ref defenderHp, ref defenderBlock,
                ref weak, ref vulnerable, attackerTraits, defenderTraits, defenderSkills, attackerStatuses, defenderStatuses);
            int previousBlock = defenderBlock;
            int raw = Math.Max(0, BossStrikeDamage(amount, weak, vulnerable, attackerStatuses, defenderStatuses) - reduction);
            int absorbed = Math.Min(defenderBlock, raw); defenderBlock -= absorbed;
            int afterBlock = raw - absorbed, deferred = afterBlock * Math.Min(20, EquipmentSum(g => g.damageDeferral)) / 100;
            int damage = Math.Min(defenderHp, afterBlock - deferred); defenderHp -= damage;
            StatusMechanics.DamageTaken(defenderStatuses, damage);
            var triggers = new List<string> { "incoming_hit" };
            if (damage > 0) { triggers.Add("damaged"); triggers.Add("low_health"); }
            foreach (var trigger in triggers)
                PreviewBossDefenderTrigger(trigger, card, damage, ref actorHp, ref defenderHp, ref defenderBlock,
                    ref weak, ref vulnerable, attackerTraits, defenderTraits, defenderSkills, attackerStatuses, defenderStatuses);
            if (previousBlock > 0 && defenderBlock == 0)
                PreviewBossDefenderTrigger("block_break", card, damage, ref actorHp, ref defenderHp, ref defenderBlock,
                    ref weak, ref vulnerable, attackerTraits, defenderTraits, defenderSkills, attackerStatuses, defenderStatuses);
            return raw;
        }

        private int PreviewBossSpecial(ref int actorHp, ref int defenderHp, ref int defenderBlock, ref int weak, ref int vulnerable,
            TraitActorState attackerTraits, TraitActorState defenderTraits, StatusActorState attackerStatuses, StatusActorState defenderStatuses, out int addedBlock)
        {
            addedBlock = 0;
            var c = State.combat; var b = BossMechanics.PreviewAtTurnEnd(c); if (!BossMechanics.NeedsSpecialAction(c, b)) return 0;
            var phase = BossMechanics.Phase(c); if (phase == null) return 0;
            var effects = new List<KeyValuePair<BossEffect, int>>();
            bool hasDelayed = b.delayedDamage > 0 && c.turn >= b.delayedDueTurn;
            if (hasDelayed) effects.Add(new KeyValuePair<BossEffect, int>(BossEffect.Strike, b.delayedDamage));
            if (b.challengeActive && !b.interrupted)
            {
                effects.Add(new KeyValuePair<BossEffect, int>(phase.effect, phase.effectAmount));
                effects.Add(new KeyValuePair<BossEffect, int>(phase.secondaryEffect, phase.secondaryAmount));
            }
            int damageBudget = BossMechanics.MaxExtraDamage, supportBudget = BossMechanics.MaxSupport, totalDamage = 0;
            var defenderSkills = SkillMechanics.Clone(c.playerSkills);
            for (int index = 0; index < effects.Count && defenderHp > 0 && actorHp > 0; ++index)
            {
                var entry = effects[index];
                int amount = BossMechanics.Amount(entry.Key, entry.Value);
                switch (entry.Key)
                {
                    case BossEffect.Strike:
                        amount = Math.Min(damageBudget, amount); damageBudget -= amount;
                        string source = hasDelayed && index == 0 ? b.delayedSourceCard ?? phase.signatureCardId : phase.signatureCardId;
                        if (amount > 0) totalDamage += PreviewBossStrike(amount, GameDatabase.Card(source), ref actorHp, ref defenderHp, ref defenderBlock,
                            ref weak, ref vulnerable, attackerTraits, defenderTraits, defenderSkills, attackerStatuses, defenderStatuses); break;
                    case BossEffect.Shield:
                        amount = Math.Min(supportBudget, amount); supportBudget -= amount; addedBlock += amount; break;
                    case BossEffect.Heal:
                        amount = Math.Min(supportBudget, amount); supportBudget -= amount;
                        actorHp = Math.Min(c.enemyMaxHp, actorHp + amount * (100 - Math.Max(TraitMechanics.HealingReduction(attackerTraits), StatusMechanics.HealingReduction(attackerStatuses))) / 100); break;
                    case BossEffect.Status:
                        if (phase.statusKey == "vulnerable") vulnerable = Math.Max(vulnerable, 1);
                        else if (phase.statusKey != "weak" && BossMechanics.StatusKeys.Contains(phase.statusKey)) StatusMechanics.Add(defenderStatuses, phase.statusKey, phase.signatureCardId, 1);
                        break;
                    case BossEffect.Cleanse:
                        weak = 0; attackerStatuses.effects.RemoveAll(x => !x.pending);
                        attackerTraits.buffs.RemoveAll(x => x.kind == "heal_reduction" || x.kind == "exposure"); break;
                }
            }
            return totalDamage;
        }

        private void RefreshEnemyIntent()
        {
            var c = State.combat;
            if (c == null) return;
            int strength = c.enemyStrength,weak=c.enemyWeak,defenderHp=State.hp,defenderBlock=c.block;
            var skillPreview = SkillMechanics.Clone(c.enemySkills);
            var traitPreview=TraitMechanics.Clone(c.enemyTraits);int actorHp=c.enemyHp;
            var grants = c.enemyTurn ? c.enemyFreeCasts.Select(x => new FreeCastGrant { cardId=x.cardId, uses=x.uses }).ToList() : new List<FreeCastGrant>();
            var sources = c.enemyTurn ? c.enemyFreeCastSources.ToList() : new List<string>();
            if (!c.enemyTurn) FreeCastMechanics.ActivatePending(c.enemyPendingFreeCasts, c.turn, c.enemyDeck, grants, sources, null, null, null, false, false);
            var history = c.enemyLastSkills.Select(x => new SkillHistory { owner=x.owner, cardId=x.cardId }).ToList();
            var defenderTraits=TraitMechanics.Clone(c.playerTraits);
            var defenderSkills=SkillMechanics.Clone(c.playerSkills);
            var attackerStatuses=StatusMechanics.Clone(c.enemyStatuses);var defenderStatuses=StatusMechanics.Clone(c.playerStatuses);
            int available=c.enemyTurn?c.enemyAvailableEnergy:EnergyForLevel(c.enemyLevel)+TraitMechanics.EnergyBonus(c.enemyTraits)+c.enemyOpeningEnergy;
            c.intentDamage = c.intentBlock = 0;
            if (!c.enemyTurn)
            {
                SkillMechanics.StartTurn(skillPreview);TraitMechanics.StartTurn(traitPreview);TraitMechanics.EndTurn(defenderTraits);
                CompanionMechanics.StartTurn(skillPreview);
                StatusMechanics.EndTurn(defenderStatuses);
                var opening=TraitMechanics.Resolve(traitPreview,ActorTraits(true),"turn_start",TraitContextFor(true,null,true,skillActor:skillPreview));
                foreach(var pulse in opening.pulses){if(pulse.kind=="strength")strength+=pulse.amount;if(pulse.kind=="heal")actorHp=Math.Min(c.enemyMaxHp,actorHp+pulse.amount*(100-TraitMechanics.HealingReduction(traitPreview))/100);if(pulse.kind=="block")c.intentBlock+=pulse.amount;if(pulse.kind=="cleanse")weak=0;}
                if(opening.pulses.Any(x=>x.kind=="cleanse"))attackerStatuses.effects.RemoveAll(x=>!x.pending);
                c.intentBlock+=EnemyEquipmentSum(g=>g.block);
                available+=opening.pulses.Where(x=>x.kind=="energy").Sum(x=>x.amount);available=Math.Max(1,available-ControlEnergyPenalty(true,attackerStatuses));
            }
            // Existing player debuffs tick when this player turn ends, before the enemy acts.
            int vulnerable = c.enemyTurn ? c.vulnerable : Math.Max(0, c.vulnerable - 1);
            int specialBlock;
            c.intentDamage += PreviewBossSpecial(ref actorHp, ref defenderHp, ref defenderBlock, ref weak, ref vulnerable, traitPreview, defenderTraits, attackerStatuses, defenderStatuses, out specialBlock);
            c.intentBlock += specialBlock;
            for(int intentIndex=c.enemyTurn?c.enemyActionIndex:0;intentIndex<c.enemyPlan.Count;++intentIndex)
            {
                var id=c.enemyPlan[intentIndex];
                var animalSkill=WildlifeMechanics.Card(id);
                if(animalSkill!=null)
                {
                    if(WildlifeMechanics.BelongsTo(id,c.animal) && !WildlifeMechanics.Interrupted(id,attackerStatuses))
                    {
                        int rawWildlife=RunModes.ScaleEnemyDirectDamage(State,CalculateEnemyDamage(animalSkill,strength,vulnerable,TraitMechanics.Exposure(defenderTraits),weak,attackerStatuses,defenderStatuses));
                        c.intentDamage+=rawWildlife;
                    }
                    continue;
                }
                var card = GameDatabase.Card(id);
                if (card == null) continue;
                var advanced=EnemyAdvancedActionAt(intentIndex);
                if(!StatusMechanics.CanUse(attackerStatuses,card))continue;
                bool freeGranted=advanced.charge==0 && grants.Any(x=>x.cardId==id && x.uses>0);
                bool garnetExecution=FreeCastMechanics.GarnetExecutionReady(card,skillPreview,freeGranted,defenderHp,State.maxHp);
                bool plannedFree=intentIndex<c.enemyPlanFreeCast.Count && c.enemyPlanFreeCast[intentIndex];
                // Re-evaluate the plan's resets against this cloned state. A new player
                // action can remove their preparation without changing the saved plan.
                if(plannedFree && !freeGranted && advanced.charge==0)continue;
                int price=freeGranted?0:Math.Max(0,CardCost(id)+StatusMechanics.CostPenalty(attackerStatuses,card)-Math.Max(SkillMechanics.Discount(skillPreview,id),SkillMechanics.Discount(traitPreview.skills,id)));
                price=AdvancedEnemyPrice(card,price,available,freeGranted,advanced);
                if(intentIndex<c.enemyPlanCosts.Count && c.enemyPlanCosts[intentIndex]==0 && CardCost(id)>0 && price>0 && !AdvancedCardMechanics.IsX(card))continue;
                if(price>available)continue;available-=price;available+=card.energy;
                if(advanced.charge==0)
                {
                    ConsumeFreeCast(grants,id);
                    SkillMechanics.ConsumeDiscount(skillPreview,id);
                    SkillMechanics.ConsumeDiscount(traitPreview.skills,id);
                }
                if(advanced.mode=="charge"){c.intentBlock+=AdvancedCardMechanics.ChargeGuard(card);continue;}
                card=AdvancedCardMechanics.PreparedCard(card,advanced.mode,advanced.charge,advanced.sacrificed,AdvancedCardMechanics.IsX(card)?freeGranted?0:price:0);
                if(AdvancedCardMechanics.IsEmptyX(card))continue;
                var skillBefore=SkillMechanics.Clone(skillPreview);var bonuses=AddAdvancedBonus(SkillMechanics.Bonuses(skillBefore,card),EnemyAdvancedBonus(card,advanced,actorHp,c.intentBlock));
                var beforeTraits=ResolveBeforeTraits(traitPreview,true,card,card.damage>0 || bonuses.damage>0,skillBefore,actorHp);
                int extra=AdvancedCardMechanics.SuppressesAttack(card)?0:bonuses.damage;
                if(card.damage>0 || extra>0)extra+=beforeTraits.bonus.damage+TraitMechanics.DamageBonus(traitPreview)+TraitMechanics.EmpowerBonus(traitPreview,card);
                if(weak>0)extra=extra*3/4;if(vulnerable>0)extra=extra*3/2;extra=extra*(100+TraitMechanics.Exposure(defenderTraits))/100;
                int baseRaw=CalculateEnemyDamage(card,strength,vulnerable,TraitMechanics.Exposure(defenderTraits),weak,attackerStatuses,defenderStatuses);
                // CalculateEnemyDamage already includes the live status modifiers; this preview uses its cloned state.
                int raw=EnemyDirectHitTotal(baseRaw,StatusMechanics.Damage(attackerStatuses,defenderStatuses,extra),Math.Max(1,card.hits));
                c.intentDamage+=garnetExecution?defenderHp:raw;c.intentBlock+=EnemyAuxiliaryValue(card,card.block)+bonuses.block+beforeTraits.bonus.block;
                int previousDefenderBlock=defenderBlock;
                if (SkillMechanics.BreaksBlock(skillBefore, card)) defenderBlock = 0;
                int actualDamage=garnetExecution?defenderHp:SimulateSharedDirectDamage(defenderSkills,defenderHp,ref defenderBlock,baseRaw,StatusMechanics.Damage(attackerStatuses,defenderStatuses,extra),Math.Max(1,card.hits));defenderHp-=actualDamage;
                StatusMechanics.DamageTaken(defenderStatuses,actualDamage);
                int cardHeal=Math.Min(c.enemyMaxHp-actorHp,(EnemyAuxiliaryValue(card,card.heal)+bonuses.heal)*(100-Math.Max(TraitMechanics.HealingReduction(traitPreview),StatusMechanics.HealingReduction(attackerStatuses)))/100);actorHp+=cardHeal;
                strength += card.strength;
                SkillMechanics.AfterCard(skillPreview,skillBefore,card,true,paidCost:price);
                CompanionMechanics.AfterCard(skillPreview,card);
                RecordLastSkill(history,card);
                GrantFreeCasts(card,c.enemyDeck,grants,sources,null,null,null,false,LastSkill(history,card.owner),skillBefore,true);
                StatusMechanics.Apply(defenderStatuses,skillBefore,card,true);
                if(card.category=="basic" && card.key=="ATK")StatusMechanics.Activate(defenderStatuses,card.id,"next_basic");
                var after=SimulateAfterTraits(traitPreview,true,card,skillBefore,actualDamage,EnemyAuxiliaryValue(card,card.block)+bonuses.block,cardHeal,actorHp,defenderHp,defenderBlock);
                var pulses=beforeTraits.pulses.Concat(after.pulses).ToList();
                c.intentBlock+=pulses.Where(x=>x.kind=="block").Sum(x=>x.amount);
                foreach(var pulse in pulses)
                {
                    if(pulse.kind.StartsWith("status_",StringComparison.Ordinal))StatusMechanics.Add(defenderStatuses,pulse.kind.Substring(7),pulse.sourceCard,pulse.duration);
                    if(pulse.kind=="strength")strength+=pulse.amount;
                    if(pulse.kind=="cleanse"){weak=0;attackerStatuses.effects.RemoveAll(x=>!x.pending);}
                    if(pulse.kind=="vulnerable")vulnerable=Math.Max(vulnerable,pulse.amount);
                    if(pulse.kind=="heal" || pulse.kind=="bonus_heal")actorHp=Math.Min(c.enemyMaxHp,actorHp+pulse.amount*(100-TraitMechanics.HealingReduction(traitPreview))/100);
                    if(pulse.kind=="exposure")
                    {
                        var buff=defenderTraits.buffs.FirstOrDefault(x=>x.kind=="exposure" && x.sourceId==pulse.sourceId);
                        if(buff==null){buff=new TraitBuff{kind="exposure",sourceId=pulse.sourceId};defenderTraits.buffs.Add(buff);}buff.amount=Math.Max(buff.amount,pulse.amount);buff.remaining=Math.Max(buff.remaining,pulse.duration);
                    }
                    if(pulse.kind=="execute"){c.intentDamage+=pulse.amount;defenderHp=0;}
                }
                // A shield broken by this card may cleanse the new debuff before the next planned card.
                if(previousDefenderBlock>0 && defenderBlock==0)
                {
                    var broken=TraitMechanics.Resolve(defenderTraits,ActorTraits(false),"block_break",TraitContextFor(false,card,true,actualDamage,hp:defenderHp));
                    if(broken.pulses.Any(x=>x.kind=="cleanse"))
                    {
                        defenderStatuses.effects.RemoveAll(x=>!x.pending);vulnerable=0;
                        defenderTraits.buffs.RemoveAll(x=>x.kind=="heal_reduction" || x.kind=="exposure");
                    }
                }
                // Intent shows raw damage assuming hits land; blocks and evasion are resolved in combat.
                vulnerable = Math.Max(vulnerable, card.vulnerable);
                if (garnetExecution && defenderHp <= 0)
                {
                    var prevention = SkillMechanics.Revive(defenderSkills) ?? SkillMechanics.Revive(defenderTraits.skills);
                    if (prevention == null) return; // The battle ends here; its installed pressure never ticks.
                    defenderHp = Math.Min(State.maxHp, prevention.amount);
                }
            }
            // Scheduled effects fire before the next player turn and are part of this intent.
            int damageBudget=40,supportBudget=25;
            foreach (var pulse in SkillMechanics.Tick(skillPreview).Concat(SkillMechanics.Tick(traitPreview.skills)))
            {
                if (pulse.kind == "guard" || pulse.kind=="hot") {int amount=Math.Min(supportBudget,pulse.amount);supportBudget-=amount;if(pulse.kind=="guard")c.intentBlock+=amount;}
                else {int amount=Math.Min(damageBudget,pulse.amount);damageBudget-=amount;c.intentDamage+=amount;}
            }
        }

        private int BaseEnemyEvasion() { return string.IsNullOrEmpty(State.combat.animal) ? Math.Min(45, 3 + State.combat.enemyLevel / 2 + EnemyEquipmentSum(g=>g.evasion)) : 0; }
        private void RecallSkillDiscounts(SkillActorState actor, SkillActorState before, IEnumerable<string> owned, List<string> hand, List<string> draw, List<string> discard, bool playerUpgrade = false)
        {
            foreach (var discount in actor.discounts)
            {
                string id=discount.targetCard;
                if (SkillMechanics.Discount(before,id)>=discount.amount || !owned.Any(r=>CardInstances.SameType(r,id)) || hand.Any(r=>CardInstances.SameType(r,id)) || hand.Count>=12) continue;
                Func<string,bool> eligible=r=>CardInstances.SameType(r,id) && discount.amount >= (playerUpgrade ? PlayerCard(r)?.cost ?? 99 : CardCost(r));
                int index=discard.FindLastIndex(r=>eligible(r));
                if (index>=0) { string actual=discard[index];discard.RemoveAt(index);hand.Add(actual);continue; }
                index=draw.FindLastIndex(r=>eligible(r));
                if (index>=0) { string actual=draw[index];draw.RemoveAt(index);hand.Add(actual); }
            }
        }
        private void DrawCards(int amount)
        {
            var c=State.combat;
            int charging=c.chargedCards.Count(x=>!c.hand.Contains(x.reference));
            DrawInto(c.hand,c.drawPile,c.discardPile,Math.Max(0,Math.Min(amount,12-c.hand.Count-charging)));
        }
        private List<TraitSource> ActorTraits(bool enemy)
        {
            var result=new List<TraitSource>();
            IEnumerable<string> ids=State.passives.Distinct().Take(3);
            if(enemy)
            {
                var character=State.combat==null ? null : GameDatabase.Character(State.combat.enemyId);
                ids=character==null || !string.IsNullOrEmpty(State.combat.animal) ? new string[0] : new[]{character.passiveId};
            }
            foreach(var id in ids)
            {
                var passive=GameDatabase.Passive(id);
                if(passive?.mechanics!=null) result.Add(new TraitSource {id=id,name=passive.name,owner=passive.owner,profile=passive.mechanics});
            }
            if(!enemy) foreach(var id in OwnedRuneIds())
            {
                var rune=GameDatabase.Rune(id);
                if(rune?.mechanics!=null) result.Add(new TraitSource {id=id,name=rune.name,owner=rune.name,profile=rune.mechanics});
            }
            foreach(var gear in (enemy?State.combat.enemyGear:State.gear).Distinct().Select(GameDatabase.Equipment).Where(x=>x?.mechanics!=null))
                result.Add(new TraitSource{id="gear:"+gear.id,name=gear.name,owner=gear.name,profile=gear.mechanics});
            return result;
        }
        private TraitContext TraitContextFor(bool enemy,CardDef card,bool landed,int damage=0,int block=0,int heal=0,int avoided=0,SkillActorState skillActor=null,int hp=-1)
        {
            var c=State.combat;
            return new TraitContext {card=card,landed=landed,damage=damage,block=block,heal=heal,avoided=avoided,hp=hp>=0?hp:enemy?c.enemyHp:State.hp,maxHp=enemy?c.enemyMaxHp:State.maxHp,targetHp=enemy?State.hp:c.enemyHp,targetBlock=enemy?c.block:c.enemyBlock,wildlife=!string.IsNullOrEmpty(c.animal),skillActor=skillActor ?? (enemy?c.enemySkills:c.playerSkills)};
        }
        private TraitResult ResolveBeforeTraits(TraitActorState actor,bool enemy,CardDef card,bool landed,SkillActorState skillActor,int hp=-1)
        {
            var result=new TraitResult(); var context=TraitContextFor(enemy,card,landed,skillActor:skillActor,hp:hp);var sources=ActorTraits(enemy);
            var triggers=new List<string> {"before_card"};
            if(card.category=="basic" && card.key=="ATK") triggers.Add("before_basic");
            if(card.category=="skill") triggers.Add("before_skill");
            if(card.category=="skill" && card.key=="R") triggers.Add("before_ultimate");
            bool attack=card.damage>0 || SkillMechanics.Bonuses(skillActor,card).damage>0 || TraitMechanics.EmpowerBonus(actor,card)>0;
            if(attack) triggers.Add("before_attack");
            if(context.wildlife && !enemy) {triggers.Add("wildlife_before_card");if(attack) triggers.Add("wildlife_before_attack");}
            foreach(var trigger in triggers)
            {
                var current=TraitMechanics.Resolve(actor,sources,trigger,context);
                result.bonus.damage+=current.bonus.damage;result.bonus.block+=current.bonus.block;result.bonus.heal+=current.bonus.heal;result.pulses.AddRange(current.pulses);
            }
            result.bonus.damage=Math.Min(10,result.bonus.damage);result.bonus.block=Math.Min(10,result.bonus.block);result.bonus.heal=Math.Min(12,result.bonus.heal);
            return result;
        }
        private TraitResult PreviewBeforeTraits(bool enemy,CardDef card,SkillActorState skills)
        {
            return ResolveBeforeTraits(TraitMechanics.Clone(enemy?State.combat.enemyTraits:State.combat.playerTraits),enemy,card,true,skills);
        }
        private void PruneUnownedTraits()
        {
            var owned=new HashSet<string>(State.passives.Concat(OwnedRuneIds()).Where(x=>!string.IsNullOrEmpty(x)));
            foreach(var id in State.gear)owned.Add("gear:"+id);
            Action<TraitActorState> prune=actor=>
            {
                if(actor==null)return;TraitMechanics.Ensure(actor);
                actor.counters.RemoveAll(x=>!owned.Contains(x.sourceId));actor.buffs.RemoveAll(x=>!owned.Contains(x.sourceId));
                Func<string,bool> abandoned=owner=>owner!=null && owner.StartsWith("trait:",StringComparison.Ordinal) && !owned.Contains(owner.Substring(6));
                actor.skills.resources.RemoveAll(x=>abandoned(x.owner));actor.skills.effects.RemoveAll(x=>abandoned(x.owner));actor.skills.history.RemoveAll(x=>abandoned(x.owner));
                // Choices happen outside combat. No pending cast grant carries into another battle.
                actor.skills.discounts.Clear();actor.skills.discountSources.Clear();
            };
            prune(State.persistentTraits);if(State.combat!=null)prune(State.combat.playerTraits);
        }
        private void ApplyBeforeTraits(bool enemy,CardDef card,bool landed,SkillActorState skillBefore,int hp=-1)
        {
            var c=State.combat;var actor=enemy?c.enemyTraits:c.playerTraits;
            var before=SkillMechanics.Clone(actor.skills);
            ApplyTraitResult(enemy,ResolveBeforeTraits(actor,enemy,card,landed,skillBefore,hp));
            if(!enemy)RecallSkillDiscounts(actor.skills,before,State.deck,c.hand,c.drawPile,c.discardPile,true);
        }
        private TraitResult TraitEvent(bool enemy,string trigger,CardDef card,bool landed,int damage=0,int block=0,int heal=0,int avoided=0)
        {
            var result=new TraitResult();
            if(State.combat==null || State.stage!=RunStage.Combat || resolvingTraits) return result;
            var c=State.combat;
            result=TraitMechanics.Resolve(enemy?c.enemyTraits:c.playerTraits,ActorTraits(enemy),trigger,TraitContextFor(enemy,card,landed,damage,block,heal,avoided));
            ApplyTraitResult(enemy,result);return result;
        }
        private void AfterCardTraits(bool enemy,CardDef card,bool landed,int damage,int block,int heal,int avoided,bool attackAttempt)
        {
            var c=State.combat;
            var targetStatuses=enemy?c.playerStatuses:c.enemyStatuses;var previousControls=StatusMechanics.Snapshot(targetStatuses);
            bool hit=landed && attackAttempt;
            TraitEvent(enemy,"after_card",card,hit,damage,block,heal,avoided);
            if(card.category=="basic" && card.key=="ATK") {TraitEvent(enemy,"after_basic",card,hit,damage,block,heal,avoided);(enemy?c.enemyTraits:c.playerTraits).skills.effects.RemoveAll(x=>x.kind=="empower_basic");}
            if(card.category=="skill") TraitEvent(enemy,"after_skill",card,hit,damage,block,heal,avoided);
            if(card.movement)TraitEvent(enemy,"after_movement",card,hit,damage,block,heal,avoided);
            if(card.category=="skill" && card.key=="R") TraitEvent(enemy,"after_ultimate",card,hit,damage,block,heal,avoided);
            if(block>0) TraitEvent(enemy,"block",card,landed,damage,block,heal,avoided);
            if(heal>0) TraitEvent(enemy,"heal",card,landed,damage,block,heal,avoided);
            if(hit)
            {
                TraitEvent(enemy,"hit",card,true,damage,block,heal,avoided);
                if(card.category=="skill") TraitEvent(enemy,"skill_hit",card,true,damage,block,heal,avoided);
                if(!enemy && !string.IsNullOrEmpty(c.animal)) TraitEvent(false,"wildlife_hit",card,true,damage,block,heal,avoided);
                TraitEvent(!enemy,"incoming_hit",card,true,damage,block,heal,avoided);
            }
            if(landed && (card.weak>0 || card.vulnerable>0)) TraitEvent(enemy,"control",card,true,damage,block,heal,avoided);
            if(avoided>0) TraitEvent(!enemy,"evade",card,true,damage,block,heal,avoided);
            if(damage>0) {TraitEvent(!enemy,"damaged",card,true,damage,block,heal,avoided);TraitEvent(!enemy,"low_health",card,true,damage,block,heal,avoided);}
            if(StatusMechanics.Snapshot(targetStatuses).Any(x=>!previousControls.Any(p=>p.kind==x.kind) && x.kind!="status_armor_break" && x.kind!="status_heal_reduction" && x.kind!="status_attack_down"))TraitEvent(enemy,"control",card,true,damage);
            TraitMechanics.RecordCard(enemy?c.enemyTraits:c.playerTraits,ActorTraits(enemy),card);
        }
        private TraitResult SimulateAfterTraits(TraitActorState actor,bool enemy,CardDef card,SkillActorState skills,int damage,int block,int heal,int hp,int targetHp=-1,int targetBlock=-1)
        {
            var result=new TraitResult();bool attack=card.damage>0 || SkillMechanics.Bonuses(skills,card).damage>0 || TraitMechanics.EmpowerBonus(actor,card)>0;
            var context=TraitContextFor(enemy,card,attack,damage,block,heal,skillActor:skills,hp:hp);var sources=ActorTraits(enemy);
            if(targetHp>=0)context.targetHp=targetHp;if(targetBlock>=0)context.targetBlock=targetBlock;
            var triggers=new List<string>{"after_card"};
            if(card.category=="basic" && card.key=="ATK")triggers.Add("after_basic");
            if(card.category=="skill")triggers.Add("after_skill");if(card.category=="skill" && card.key=="R")triggers.Add("after_ultimate");
            if(card.movement)triggers.Add("after_movement");
            if(block>0)triggers.Add("block");if(heal>0)triggers.Add("heal");
            if(attack){triggers.Add("hit");if(card.category=="skill")triggers.Add("skill_hit");if(!enemy && context.wildlife)triggers.Add("wildlife_hit");}
            if(card.weak>0 || card.vulnerable>0)triggers.Add("control");
            foreach(var trigger in triggers)
            {
                context.landed=trigger=="control" || attack;
                var current=TraitMechanics.Resolve(actor,sources,trigger,context);result.pulses.AddRange(current.pulses);result.bonus.block+=current.bonus.block;result.bonus.heal+=current.bonus.heal;
            }
            if(card.category=="basic" && card.key=="ATK")actor.skills.effects.RemoveAll(x=>x.kind=="empower_basic");
            TraitMechanics.RecordCard(actor,sources,card);return result;
        }
        private void ApplyTraitResult(bool enemy,TraitResult result)
        {
            var c=State.combat;if(c==null) return;
            bool wasResolving=resolvingTraits;resolvingTraits=true;
            try
            {
                int bonusBlock=result.bonus.block,bonusHeal=result.bonus.heal;
                foreach(var pulse in result.pulses)
                {
                    int heal=0,block=0,damage=0;int before=enemy?c.enemyHp:State.hp;
                    if(pulse.kind.StartsWith("status_",StringComparison.Ordinal))StatusMechanics.Add(enemy?c.playerStatuses:c.enemyStatuses,pulse.kind.Substring(7),pulse.sourceCard,pulse.duration);
                    switch(pulse.kind)
                    {
                        case "heal":if(enemy)HealEnemy(pulse.amount);else Heal(pulse.amount);heal=(enemy?c.enemyHp:State.hp)-before;break;
                        case "bonus_heal":int addedHeal=Math.Min(bonusHeal,pulse.amount);bonusHeal-=addedHeal;if(enemy)HealEnemy(addedHeal);else Heal(addedHeal);heal=(enemy?c.enemyHp:State.hp)-before;break;
                        case "bonus_block":block=Math.Min(bonusBlock,pulse.amount);bonusBlock-=block;if(enemy)c.enemyBlock+=block;else {c.block+=block;RecordBossProgress(BossMechanics.ObserveBlock(c,block));}break;
                        case "block":block=pulse.amount;if(enemy)c.enemyBlock+=block;else {c.block+=block;RecordBossProgress(BossMechanics.ObserveBlock(c,block));}break;
                        case "energy":if(enemy)c.enemyAvailableEnergy+=pulse.amount;else c.energy+=pulse.amount;break;
                        case "draw":if(!enemy)DrawCards(pulse.amount);break;
                        case "poison":if(enemy)c.poison+=pulse.amount;else c.enemyPoison+=pulse.amount;break;
                        case "strength":if(enemy)c.enemyStrength=Math.Min(12,c.enemyStrength+pulse.amount);else c.strength=Math.Min(12,c.strength+pulse.amount);break;
                        case "evasion":if(enemy && string.IsNullOrEmpty(c.animal)){c.enemyEvasion=Math.Max(c.enemyEvasion,pulse.amount);c.enemyEvasionTurns=Math.Max(c.enemyEvasionTurns,2);}else if(!enemy){c.evasion=Math.Max(c.evasion,pulse.amount);c.evasionTurns=Math.Max(c.evasionTurns,2);}break;
                        case "weak":if(enemy)c.weak=Math.Max(c.weak,pulse.amount);else c.enemyWeak=Math.Max(c.enemyWeak,pulse.amount);break;
                        case "vulnerable":if(enemy)c.vulnerable=Math.Max(c.vulnerable,pulse.amount);else c.enemyVulnerable=Math.Max(c.enemyVulnerable,pulse.amount);break;
                        case "cleanse":if(enemy){c.enemyPoison=c.enemyWeak=c.enemyVulnerable=0;c.enemyStatuses.effects.RemoveAll(x=>!x.pending);c.enemyTraits.buffs.RemoveAll(x=>x.kind=="heal_reduction" || x.kind=="exposure");}else{c.poison=c.weak=c.vulnerable=0;c.playerStatuses.effects.RemoveAll(x=>!x.pending);c.playerTraits.buffs.RemoveAll(x=>x.kind=="heal_reduction" || x.kind=="exposure");}break;
                        case "heal_reduction":case "exposure":
                            var target=enemy?c.playerTraits:c.enemyTraits;var buff=target.buffs.FirstOrDefault(x=>x.sourceId==pulse.sourceId && x.kind==pulse.kind);
                            if(buff==null){buff=new TraitBuff {sourceId=pulse.sourceId,label=GameDatabase.Rune(pulse.sourceId)?.name ?? GameDatabase.Passive(pulse.sourceId)?.name ?? pulse.kind,kind=pulse.kind};target.buffs.Add(buff);}
                            buff.amount=Math.Max(buff.amount,pulse.amount);buff.remaining=Math.Max(buff.remaining,Math.Min(2,pulse.duration));break;
                        case "execute":damage=enemy?State.hp:c.enemyHp;if(enemy)State.hp=0;else c.enemyHp=0;break;
                    }
                    EmitCombatAction(new CombatAction {id=++actionSerial,cardId=pulse.sourceCard,traitId=pulse.sourceId,kind=pulse.kind.StartsWith("status_",StringComparison.Ordinal)?pulse.kind:"trait_"+pulse.kind,enemy=enemy,damage=damage,heal=heal,block=block});
                }
            }
            finally {resolvingTraits=wasResolving;}
        }
        private void DrawInto(List<string> hand, List<string> draw, List<string> discard, int amount)
        {
            for (int i = 0; i < amount && hand.Count < 12; ++i)
            {
                if (draw.Count == 0 && discard.Count > 0) { draw.AddRange(discard); discard.Clear(); Shuffle(draw); }
                if (draw.Count == 0) break;
                int last = draw.Count - 1; hand.Add(draw[last]); draw.RemoveAt(last);
            }
        }

        private bool CheckBattleEnd()
        {
            if (State.stage == RunStage.Combat && State.combat != null)
            {
                var c = State.combat;
                if (State.hp <= 0) ResolveRevive(false);
                if (c.enemyHp <= 0) ResolveRevive(true);
            }
            if (State.hp <= 0) { if (State.stage == RunStage.Combat) RecordCombatInformation(new CombatLogEntry { kind = "battle_end", title = "전투 패배", detail = "하나 체력 0" }); Lose(); return true; }
            if (State.stage == RunStage.Combat && State.combat.enemyHp <= 0) { WinBattle(); return true; }
            return State.stage != RunStage.Combat;
        }
        private void ResolveRevive(bool enemy)
        {
            var c = State.combat;
            var pulse = SkillMechanics.Revive(enemy ? c.enemySkills : c.playerSkills) ?? SkillMechanics.Revive((enemy?c.enemyTraits:c.playerTraits).skills);
            if (pulse == null) return;
            if (enemy) c.enemyHp = Math.Min(c.enemyMaxHp, pulse.amount); else State.hp = Math.Min(State.maxHp, pulse.amount);
            EmitCombatAction(new CombatAction { id=++actionSerial, cardId=pulse.sourceCard,traitId=pulse.traitId, kind="revive", enemy=enemy, heal=enemy ? c.enemyHp : State.hp });
            Say((enemy ? c.enemyName : "하나") + "의 치명적인 피해를 막고 회복했습니다.");
        }
        private void ResolveSkillTicks(bool enemy)
        {
            var c = State.combat;
            if (c == null || State.stage != RunStage.Combat) return;
            int damageBudget=40,supportBudget=25;
            foreach (var pulse in SkillMechanics.Tick(enemy ? c.enemySkills : c.playerSkills).Concat(SkillMechanics.Tick((enemy?c.enemyTraits:c.playerTraits).skills)))
            {
                if(pulse.kind=="hot" || pulse.kind=="guard"){pulse.amount=Math.Min(supportBudget,pulse.amount);supportBudget-=pulse.amount;}
                else {pulse.amount=Math.Min(damageBudget,pulse.amount);damageBudget-=pulse.amount;}
                if(pulse.amount<=0)continue;
                ResolveSkillPulse(pulse, enemy);
                if (CheckBattleEnd()) break;
            }
        }
        private void ResolveCounters(bool enemyDefender, int landedHits = 1)
        {
            var c = State.combat;
            if (enemyDefender && c.boss != null && !c.enemyTurn && c.enemyHp > 0 && c.boss.counterDamage > 0 && c.turn <= c.boss.counterUntilTurn)
            {
                int counter = c.boss.counterDamage; c.boss.counterDamage = 0;
                ResolveBossStrike(counter, "boss_counter", BossMechanics.Phase(c)?.signatureCardId, false);
                if (CheckBattleEnd()) return;
            }
            foreach (var pulse in SkillMechanics.Counters(enemyDefender ? c.enemySkills : c.playerSkills, landedHits).Concat(SkillMechanics.Counters((enemyDefender?c.enemyTraits:c.playerTraits).skills, landedHits)))
            {
                // Counter pulses do not trigger additional counters, so retaliation cannot recurse.
                ResolveSkillPulse(pulse, enemyDefender);
                if (CheckBattleEnd()) break;
            }
        }
        private bool ApplyCardStatuses(bool enemy,SkillActorState before,CardDef card,bool landed)
        {
            var target=enemy?State.combat.playerStatuses:State.combat.enemyStatuses;
            var previous=StatusMechanics.Snapshot(target);
            bool control=StatusMechanics.Apply(target,before,card,landed);
            if(card.category=="basic" && card.key=="ATK")
            {
                if(landed)control|=StatusMechanics.Activate(target,card.id,"next_basic");
                else target.effects.RemoveAll(x=>x.pending && x.timing=="next_basic");
            }
            RecordStatusChanges(enemy,target,previous);
            return control;
        }
        private void RecordStatusChanges(bool enemy,StatusActorState target,List<SkillMechanicToken> previous)
        {
            foreach(var status in StatusMechanics.Snapshot(target))
                if(!previous.Any(x=>x.kind==status.kind && x.remaining>=status.remaining))
                    EmitCombatAction(new CombatAction {id=++actionSerial,cardId=status.sourceCard,kind=status.kind,enemy=enemy});
        }
        private void ResolveSkillPulse(SkillPulse pulse, bool enemy)
        {
            var c = State.combat; int damage = 0, heal = 0, block = 0;
            if (pulse.kind == "guard")
            {
                block = pulse.amount;
                if (enemy) c.enemyBlock += block; else { c.block += block; RecordBossProgress(BossMechanics.ObserveBlock(c, block)); }
            }
            else if (pulse.kind == "hot")
            {
                int before = enemy ? c.enemyHp : State.hp;
                if (enemy) HealEnemy(pulse.amount); else Heal(pulse.amount);
                heal = (enemy ? c.enemyHp : State.hp) - before;
            }
            else
            {
                int amount = enemy && pulse.kind == "counter" ? RunModes.ScaleEnemyDirectDamage(State, pulse.amount) : pulse.amount;
                if (pulse.kind != "bleed" && pulse.kind != "burn")
                {
                    int absorbed = Math.Min(enemy ? c.block : c.enemyBlock, amount);
                    if (enemy) c.block -= absorbed; else c.enemyBlock -= absorbed;
                    if (!enemy) RecordBossProgress(BossMechanics.ObserveDamage(c, absorbed));
                    amount -= absorbed;
                }
                damage = TakeHealthDamage(!enemy,amount);
            }
            EmitCombatAction(new CombatAction { id=++actionSerial, cardId=pulse.sourceCard,traitId=pulse.traitId, kind=pulse.kind, enemy=enemy, damage=damage, heal=heal, block=block });
            var card = GameDatabase.Card(pulse.sourceCard);
            if(pulse.kind!="hot" && pulse.kind!="guard")
            {
                var target=enemy?c.playerStatuses:c.enemyStatuses;
                StatusMechanics.DamageTaken(target,damage);
                var beforeStatuses=StatusMechanics.Snapshot(target);
                bool control=StatusMechanics.Activate(target,pulse.sourceCard,pulse.key);
                RecordStatusChanges(enemy,target,beforeStatuses);
                if(control)TraitEvent(enemy,"control",card,true,damage);
            }
            Say((card == null ? "지속 효과" : card.name) + (block > 0 ? " · 방어 " + block : heal > 0 ? " · 회복 " + heal : " · 추가 피해 " + damage));
            if(string.IsNullOrEmpty(pulse.traitId))
            {
                if(damage>0)
                {
                    TraitEvent(enemy,"hit",card,true,damage);
                    if(pulse.kind=="counter" || pulse.kind=="delayed_damage" || pulse.kind=="summon")TraitEvent(enemy,"install_hit",card,true,damage);
                    TraitEvent(!enemy,"damaged",card,true,damage);TraitEvent(!enemy,"low_health",card,true,damage);
                }
                if(heal>0)TraitEvent(enemy,"heal",card,false,heal:heal);
                if(block>0)TraitEvent(enemy,"block",card,false,block:block);
            }
        }

        private void WinBattle()
        {
            RecordCombatInformation(new CombatLogEntry { kind = "battle_end", title = "전투 승리", detail = State.combat.enemyName + " 제압 · 하나 체력 " + State.hp + "/" + State.maxHp });
            var c = State.combat; var node = ActiveNode(); bool boss = node.kind == ZoneKind.Boss;
            int bossEnergyGained = 0;
            ResearchProgression.Victory(State, GameDatabase.Character(c.enemyId)?.id, boss);
            TraitEvent(false,"battle_win",null,true);
            int rewardEnergy=EnergyForLevel(State.level);
            var r = new RewardState { boss = boss, source = boss ? RewardSource.Boss : node.kind == ZoneKind.Subject ? RewardSource.Subject : RewardSource.Wildlife, xp = (boss ? 180 : node.kind == ZoneKind.Subject ? 90 : 50) + c.enemyLevel * (boss ? 14 : node.kind == ZoneKind.Subject ? 12 : 9), credits = (boss ? 185 : node.kind == ZoneKind.Subject ? 80 : 48) + c.enemyLevel * 5 + Next(25) + Modifier("reward_credit"), cardBudget = node.kind == ZoneKind.Wildlife ? rewardEnergy - 1 : 0 };
            if (node.kind == ZoneKind.Wildlife)
            {
                r.choices = CardOffer(RandomCardPool(), 3, ref State.rngState);
                if (c.animal == "wolf" && Roll(WildlifeMechanics.ObjectDropChance(c.animal,c.mutantAnimal))) r.objectId = RandomObject(2);
                if (c.animal == "bear" && Roll(WildlifeMechanics.ObjectDropChance(c.animal,c.mutantAnimal))) r.objectId = RandomObject(4);
                // Independent bonus rolls preserve all three ordinary skill choices.
                // The basic attack is optional and uses the same selection budget.
                if (Roll(WildlifeBasicAttackDropChance)) r.choices.Add("basic_attack");
                if (Roll(WildlifeMeatDropChance)) r.foodId = "meat";
            }
            else
            {
                var character = GameDatabase.Character(c.enemyId);
                r.choices = c.enemyDeck.Distinct().Where(id => GameDatabase.Card(id).category == "skill").ToList();
                if (boss)
                {
                    State.defeatedBosses.Add(c.enemyId);
                    State.bossVictories++;
                    if (State.mode != RunMode.Endless || State.bossVictories <= RunModes.EndlessBossEnergyLimit)
                    { State.maxEnergyBonus++; bossEnergyGained = 1; }
                    if (character != null && !State.passives.Contains(character.passiveId)) State.pendingPassive = character.passiveId;
                    r.objectId = RandomObject(4);
                }
                else if (IsNearKiosk(node) && Roll(25)) r.objectId = RandomObject(4);
            }
            State.rewards = r; State.stage = RunStage.Rewards; State.credits += r.credits;
            if (!string.IsNullOrEmpty(r.objectId)) State.objects.Add(r.objectId);
            if (!string.IsNullOrEmpty(r.foodId)) State.foods.Add(r.foodId);
            GainXp(r.xp); Heal(Modifier("kill_heal") + AugmentAmount("victory_heal"));
            Say("전투 승리 · 경험치 +" + r.xp + " · 크레딧 +" + r.credits + (boss ? bossEnergyGained > 0 ? " · 최대 코스트 +1 (" + PlayerBaseEnergy + ")" : " · 보스 최대 코스트 성장 완료 (+6)" : "") + (r.objectId == null ? "" : " · " + GameDatabase.Object(r.objectId).name) + (r.foodId == null ? "" : " · " + GameDatabase.Food(r.foodId).name + " 1개"));
        }

        private string RandomObject(int count) { return GameDatabase.Objects.Take(count).ElementAt(Next(Math.Min(count, GameDatabase.Objects.Count))).id; }
        private void GainXp(int amount)
        {
            State.xp += amount;
            while (State.level < 20 && State.xp >= NextLevelXp)
            {
                State.xp -= NextLevelXp; State.level++; RecalculateHealth(false); Heal(8);
                Say("레벨 업! Lv." + State.level + " · 최대 체력 " + State.maxHp);
            }
            if (State.level == 20) State.xp = 0;
        }

        private bool AcceptPassiveOrChoose()
        {
            if (State.passives.Contains(State.pendingPassive)) { State.pendingPassive = null; CompleteNode(); return true; }
            if (State.passives.Count < 3)
            {
                State.passives.Add(State.pendingPassive); SyncWeaponCards(); RecalculateHealth(false);
                Say(GameDatabase.Passive(State.pendingPassive).name + " 패시브 획득"); State.pendingPassive = null; CompleteNode();
            }
            else { State.stage = RunStage.PassiveChoice; Say("패시브는 최대 3개입니다. 기존 패시브를 교체하거나 새 패시브를 포기하세요."); }
            return true;
        }

        private void CompleteNode()
        {
            var node = ActiveNode(); if (node == null || node.visited) return;
            ResearchProgression.CompleteNode(State, node.id);
            node.visited = true; State.row = node.row; State.lane = node.lane;
            State.activeNodeId = -1; State.combat = null; State.stage = RunStage.Map;
            State.encounterOffers.Clear(); State.chosenEventId = null;
            State.cardUpgradePending = false;
            ClearRarePending();
            if (node.kind == ZoneKind.Boss)
            {
                if (State.act >= 3 && State.mode == RunMode.Endless) BeginAugmentChoice();
                else if (State.act >= 3) { State.stage = RunStage.Won; Say("니아: 출구는 바로 여기야. 하나는 VF 세계를 탈출해 아글라이아 연구소로 돌아왔다."); }
                else { State.act++; State.row = State.lane = -1; Say("제" + State.act + "구역으로 진입했습니다. 더 강한 실험체들이 기다립니다."); }
            }
        }

        private void BeginNextEndlessCycle()
        {
            // Archive only visited locations for journal validation. Node identities never repeat.
            State.archivedMapNodes.AddRange(State.map.Where(n => n.visited));
            State.map.Clear(); State.endlessCycle++; State.act = 1; State.row = State.lane = -1;
            State.stage = RunStage.Map;
            State.scoutedActs.Clear(); State.hyperloopActs.Clear(); State.revealedNodeIds.Clear();
            State.actBosses.Clear(); State.rewards = null;
            GenerateMap(); EnsureBossAssignments();
            Say("니아의 VF 세계가 다시 이어집니다. " + RunModes.CycleLabel(State) + " · 적 체력 " + RunModes.EnemyHealthPercent(State) + "% · 직접 공격 피해 " + RunModes.EnemyDamagePercent(State) + "%.");
        }

        public bool EndEndlessRun()
        {
            if (State.mode != RunMode.Endless || State.stage == RunStage.Preparation || State.stage == RunStage.Won || State.stage == RunStage.Lost) return false;
            State.endlessRetired = true; State.stage = RunStage.Lost;
            return Say("무한 원정을 종료했습니다. 지금까지의 연구 기록을 정산합니다.");
        }

        private void Lose() { State.hp = 0; State.stage = RunStage.Lost; Say("VF 연결이 끊어졌습니다. 니아의 손을 다시 잡고 탐사를 시작하세요."); }
        private void Heal(int amount)
        {
            int reduction=State.stage==RunStage.Combat && State.combat!=null ? Math.Max(TraitMechanics.HealingReduction(State.combat.playerTraits),StatusMechanics.HealingReduction(State.combat.playerStatuses)) : 0;
            State.hp = (int)Math.Min(State.maxHp, Math.Max(0L, (long)State.hp + (long)Math.Max(0, amount)*(100-reduction)/100));
        }
        private void HealEnemy(int amount)
        {
            var c=State.combat;
            c.enemyHp=Math.Min(c.enemyMaxHp,c.enemyHp+Math.Max(0,amount)*(100-Math.Max(TraitMechanics.HealingReduction(c.enemyTraits),StatusMechanics.HealingReduction(c.enemyStatuses)))/100);
        }
        private int EquipmentSum(Func<GearDef, int> value) { return State.gear.Select(GameDatabase.Equipment).Where(x => x != null).Sum(value); }
        private int Modifier(string trigger)
        {
            int sum = State.passives.Select(GameDatabase.Passive).Where(x => x != null && x.trigger == trigger && (x.mechanics==null || !x.mechanics.replaceLegacy)).Sum(x => x.amount);
            sum += OwnedRuneIds().Select(GameDatabase.Rune).Where(rune => rune.trigger == trigger && (rune.mechanics == null || !rune.mechanics.replaceLegacy)).Sum(rune => rune.amount);
            sum += State.gear.Select(GameDatabase.Equipment).Where(x => x != null && x.effect == trigger).Sum(x => x.amount);
            return sum;
        }
        private void RecalculateHealth(bool full)
        {
            int before = State.maxHp;
            State.maxHp = (int)Math.Min(int.MaxValue, Math.Max(1L, 80L + 4 * (State.level - 1) + State.maxHealthBonus + Modifier("max_health") + EquipmentSum(x => x.health)));
            State.hp = full ? State.maxHp : (int)Math.Min(State.maxHp, Math.Max(1L, (long)State.hp + Math.Max(0L, (long)State.maxHp - before)));
        }
        private void SyncWeaponCards()
        {
            if(State.weaponGrantedCards==null)State.weaponGrantedCards=new List<string>();
            // Old saves did not record provenance. Infer only the copies granted by their equipped weapons.
            // Without equipment, every previously acquired D card is preserved.
            if(State.weaponGrantVersion==0)
            {
                State.weaponGrantedCards.Clear();
                foreach(var id in WeaponIdentity.EquipmentCards(State.gear,false))
                    if(State.deck.Count(x=>x==id)>State.weaponGrantedCards.Count(x=>x==id))State.weaponGrantedCards.Add(id);
            }
            foreach(var id in State.weaponGrantedCards)
            {
                int index=State.deck.LastIndexOf(id);if(index>=0)State.deck.RemoveAt(index);
            }
            State.weaponGrantedCards=WeaponIdentity.EquipmentCards(State.gear,State.passives.Contains("alex_p"));
            State.deck.AddRange(State.weaponGrantedCards);State.weaponGrantVersion=1;
        }
        private List<string> Offer(IEnumerable<string> pool, int amount)
        {
            var list = pool.Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList(); Shuffle(list);
            return list.Take(amount).ToList();
        }
        private IEnumerable<string> RandomCardPool()
        {
            return GameDatabase.Cards.Where(x => x.category == "skill" || x.category == "tactical").Select(x => x.id);
        }
        private HashSet<string> PassiveCardOwners()
        {
            return new HashSet<string>((State.passives ?? new List<string>()).Select(GameDatabase.Passive)
                .Where(x => x != null && !string.IsNullOrEmpty(x.owner)).Select(x => x.owner), StringComparer.Ordinal);
        }
        private static bool FavoredSkill(CardDef card, HashSet<string> owners)
        {
            return card != null && card.category == "skill" && (card.key == "Q" || card.key == "W" || card.key == "E" || card.key == "R") && owners.Contains(card.owner);
        }
        public bool IsPassiveFavoredCard(string cardId)
        {
            return FavoredSkill(GameDatabase.Card(cardId), PassiveCardOwners());
        }
        private List<string> CardOffer(IEnumerable<string> pool, int amount, ref int randomState)
        {
            var candidates = pool.Where(x => !string.IsNullOrEmpty(x)).Distinct().Select(GameDatabase.Card).Where(x => x != null).ToList();
            var owners = PassiveCardOwners();
            var result = new List<string>();
            while (result.Count < amount && candidates.Count > 0)
            {
                // Integer weights 3:2 give an owner's Q/W/E/R a modest 1.5x preference.
                int total = candidates.Sum(card => FavoredSkill(card, owners) ? 3 : 2);
                int pick = Next(ref randomState, total), selected = 0;
                for (; selected < candidates.Count - 1; ++selected)
                {
                    pick -= FavoredSkill(candidates[selected], owners) ? 3 : 2;
                    if (pick < 0) break;
                }
                result.Add(candidates[selected].id); candidates.RemoveAt(selected);
            }
            return result;
        }
        private void RollStartingDraft()
        {
            State.draftSeed = Next(int.MaxValue) + 1;
            State.draftBiasPassive = null;
            int randomState = State.draftSeed;
            State.draftOffers = StartingOffers(ref randomState);
            State.draftBiasPassive = State.chosenPassive;
        }
        private List<string> StartingOffers(ref int randomState)
        {
            var locked=State.draftSelected.Where(id=>GameDatabase.Card(id)!=null).Distinct().Take(3).ToList();
            var slots=State.draftOffers==null?new List<string>():State.draftOffers.ToList();
            var pool=RandomCardPool().Where(id=>CardCost(id)<5).ToList();
            // Reweight the same full sample first, so switching passive away and back cannot farm a new roll.
            var fresh=CardOffer(pool,6,ref randomState).Where(id=>!locked.Contains(id)).Take(6-locked.Count).ToList();
            if(fresh.Count<6-locked.Count)fresh.AddRange(CardOffer(pool.Where(id=>!locked.Contains(id) && !fresh.Contains(id)),6-locked.Count-fresh.Count,ref randomState));
            // Preserve selected cards in their original visual positions, including older saved selections.
            var result=new List<string>();int next=0;
            for(int i=0;i<6;++i)
            {
                string previous=i<slots.Count?slots[i]:null;
                if(previous!=null && locked.Contains(previous) && !result.Contains(previous))result.Add(previous);
                else if(next<fresh.Count)result.Add(fresh[next++]);
            }
            foreach(var id in locked)if(!result.Contains(id))result.Add(id);
            return result.Take(6).ToList();
        }
        private void RefreshStartingDraft()
        {
            // Legacy saves retain their existing valid offers until the player explicitly rerolls.
            if (State.draftSeed == 0 || State.draftBiasPassive == State.chosenPassive) return;
            int randomState = State.draftSeed;
            State.draftOffers = StartingOffers(ref randomState);
            State.draftBiasPassive = State.chosenPassive;
            State.draftSelected.RemoveAll(id => !State.draftOffers.Contains(id));
        }
        private void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; --i) { int j = Next(i + 1); T value = list[i]; list[i] = list[j]; list[j] = value; }
        }
        private int Next(int max)
        {
            return Next(ref State.rngState, max);
        }
        private static int Next(ref int randomState, int max)
        {
            if (max <= 0) throw new InvalidOperationException("게임 데이터 선택지가 비어 있습니다.");
            uint x = unchecked((uint)randomState); x ^= x << 13; x ^= x >> 17; x ^= x << 5;
            randomState = unchecked((int)x); return (int)(x % (uint)max);
        }
        private bool TryDirectHit(CardDef card, bool enemy, out bool accuracyMiss, bool freeCast = false)
        {
            accuracyMiss = false;
            var actor = enemy ? State.combat.enemySkills : State.combat.playerSkills;
            if (AccuracyMechanics.GuaranteedHit(card, actor, freeCast)) return true;
            // At 100 accuracy no RNG is consumed, preserving ordinary save replays.
            if (Roll(100 - (enemy ? EnemyAccuracy : Accuracy))) { accuracyMiss = true; return false; }
            return !Roll(enemy ? Evasion : EnemyEvasion);
        }
        private bool Roll(int percent) { return percent > 0 && Next(100) < Math.Min(100, percent); }
        private bool Fail(string message) { State.message = message; return false; }
        private bool Say(string message)
        {
            if (State.stage == RunStage.Combat && State.combat != null && !string.IsNullOrEmpty(message)
                && (message.Contains("연계가 취소") || message.Contains("사용이 상태이상으로 막혔")))
                RecordCombatInformation(new CombatLogEntry { kind = "cancelled", enemy = true, title = "상대 행동 취소", detail = message });
            State.message = message; if (!string.IsNullOrEmpty(message)) State.log.Add(message);
            if (State.log.Count > 80) State.log.RemoveAt(0); return true;
        }
    }
}
