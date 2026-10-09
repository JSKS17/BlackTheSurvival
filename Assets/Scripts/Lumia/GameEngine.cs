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
    }

    [Serializable] public class FreeCastGrant
    {
        public string cardId;
        public int uses;
    }

    [Serializable] public class CombatAction
    {
        public int id;
        public string cardId;
        public string kind;
        public string traitId;
        public bool enemy;
        public int damage, block, heal, avoided;
        public bool critical;
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
        public int enemyLevel, enemyHp, enemyMaxHp, enemyBlock, enemyEnergy;
        public int turn, energy, block, strength, evasion, evasionTurns, poison, vulnerable, weak;
        public int enemyStrength, enemyEvasion, enemyEvasionTurns, enemyPoison, enemyVulnerable, enemyWeak;
        public int intentDamage, intentBlock, attacksPlayed, enemyActionIndex, enemyAvailableEnergy;
        public int playerOpeningEnergy, enemyOpeningEnergy;
        public bool enemyTurn;
        public string enemyWeaponClass, enemyWeaponCardId, enemyTacticalCardId, enemyPassiveId;
        public List<string> enemyGear = new List<string>();
        public List<DeferredDamage> playerDeferredDamage = new List<DeferredDamage>(), enemyDeferredDamage = new List<DeferredDamage>();
        public List<FreeCastGrant> freeCasts = new List<FreeCastGrant>(), enemyFreeCasts = new List<FreeCastGrant>();
        public List<string> freeCastSources = new List<string>(), enemyFreeCastSources = new List<string>();
        public List<int> enemyPlanCosts = new List<int>();
        public List<bool> enemyPlanFreeCast = new List<bool>();
        public SkillActorState playerSkills = new SkillActorState(), enemySkills = new SkillActorState();
        public TraitActorState playerTraits = new TraitActorState(), enemyTraits = new TraitActorState();
        public StatusActorState playerStatuses = new StatusActorState(), enemyStatuses = new StatusActorState();
        public List<SkillHistory> lastSkills = new List<SkillHistory>(), enemyLastSkills = new List<SkillHistory>();
        public List<string> hand = new List<string>(), drawPile = new List<string>(), discardPile = new List<string>(), exhaustPile = new List<string>();
        public List<string> enemyDeck = new List<string>(), enemyDrawPile = new List<string>(), enemyDiscardPile = new List<string>(), enemyExhaustPile = new List<string>(), enemyHand = new List<string>(), enemyPlan = new List<string>();
    }

    [Serializable] public class RewardPrice
    {
        public string cardId;
        public int cost;
    }

    [Serializable] public class RewardState
    {
        public int xp, credits, cardBudget;
        public string objectId, foodId;
        public bool boss, cardsCommitted;
        public List<string> choices = new List<string>(), taken = new List<string>();
        public List<RewardPrice> legacyPrices = new List<RewardPrice>();
    }

    [Serializable] public class RunState
    {
        public int version = 2, mapRows = 12, seed, rngState, act = 1, row = -1, lane = -1, activeNodeId = -1;
        public RunStage stage = RunStage.Preparation;
        public int level = 1, xp, hp = 80, maxHp = 80, credits = 120, maxHealthBonus;
        public string mainRune, supportRune, chosenPassive, chosenEventId, pendingPassive, message;
        public int passiveRerolls = 2, draftRerolls = 2, campChoice, campActions;
        // One saved seed per paid draft roll: changing the passive reweights this same roll.
        // Zero marks an older saved draft whose displayed offers must remain intact.
        public int draftSeed;
        public int weaponGrantVersion;
        public int energyGrowthVersion, maxEnergyBonus, bossVictories;
        public string draftBiasPassive;
        public bool runeChangePending;
        public List<string> deck = new List<string>(), passives = new List<string>(), gear = new List<string>(), foods = new List<string>(), objects = new List<string>();
        public List<string> weaponGrantedCards = new List<string>();
        public List<string> passiveOffers = new List<string>(), draftOffers = new List<string>(), draftSelected = new List<string>(), upgrades = new List<string>();
        public List<string> encounterOffers = new List<string>(), defeatedBosses = new List<string>(), log = new List<string>();
        public List<MapNode> map = new List<MapNode>();
        public CombatState combat;
        public RewardState rewards;
        public TraitActorState persistentTraits = new TraitActorState();
    }

    /// <summary>Serializable, deterministic game rules. UI and persistence are deliberately external.</summary>
    public sealed class GameEngine
    {
        public const int RowsPerAct = 12;
        public const float PassiveSkillOfferMultiplier = 1.5f;
        public const int StartingMaxEnergy = 5;
        public const int CriticalChanceCap = 30;
        public const int WildlifeBasicAttackDropChance = 3;
        public const int WildlifeMeatDropChance = 50;
        public RunState State { get; private set; }
        public readonly List<CombatAction> CombatActions = new List<CombatAction>();
        private int actionSerial;
        private bool resolvingTraits;
        public int RewardRemainingBudget { get { return State.rewards == null ? 0 : Math.Max(0, State.rewards.cardBudget - State.rewards.taken.Sum(RewardCardPrice)); } }
        public int PlayerBaseEnergy { get { return StartingMaxEnergy + Math.Max(0, State.maxEnergyBonus); } }
        public int MaxEnergy { get { return PlayerBaseEnergy + (State.stage==RunStage.Combat && State.combat!=null ? TraitMechanics.EnergyBonus(State.combat.playerTraits) : 0); } }
        public int DefeatedBossCount { get { return State.bossVictories; } }
        public int CritChance { get { return Math.Min(CriticalChanceCap, Math.Max(0, EquipmentSum(g => g.critChance))); } }
        public int EnemyCritChance { get { return State.combat == null || !string.IsNullOrEmpty(State.combat.animal) ? 0 : Math.Min(CriticalChanceCap, Math.Max(0, EnemyEquipmentSum(g => g.critChance))); } }
        public int NextLevelXp { get { return 50 + State.level * 12; } }
        public int TotalAttack { get { return EquipmentSum(g => g.attack); } }
        public int TotalBlock { get { return EquipmentSum(g => g.block); } }
        public int Evasion { get { return Math.Min(90, Math.Min(65, EquipmentSum(g => g.evasion) + Modifier("evade_bonus") + (State.combat == null ? 0 : State.combat.evasion + Modifier("battle_start_evasion") + TraitMechanics.EvasionBonus(State.combat.playerTraits)))+(State.combat==null?0:StatusMechanics.AccuracyPenalty(State.combat.enemyStatuses))); } }
        public int EnemyEvasion { get { var c=State.combat;return c==null?0:c.enemyEvasion>=100?100:Math.Min(90,Math.Min(65,c.enemyEvasion+TraitMechanics.EvasionBonus(c.enemyTraits))+StatusMechanics.AccuracyPenalty(c.playerStatuses)); } }
        public string EnemyIntent
        {
            get
            {
                var c = State.combat;
                if (c == null) return "";
                RefreshEnemyIntent();
                var names = c.enemyPlan.Select(id => GameDatabase.Card(id)).Where(x => x != null).Select(x => x.name);
                return string.Join(" · ", names) + "  / 피해 " + c.intentDamage + " · 방어 " + c.intentBlock + " · 코스트 " + c.enemyPlanCosts.Sum() + "/" + EnergyForLevel(c.enemyLevel) + (c.enemyPlan.Any(id => GameDatabase.Card(id) != null && GameDatabase.Card(id).freeCastOnHit) ? " · 적중 시 연계 포함" : "");
            }
        }

        public GameEngine(int seed)
        {
            State = new RunState { seed = seed, rngState = seed == 0 ? 1831565813 : seed, weaponGrantVersion=1, energyGrowthVersion=1 };
            GenerateMap();
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
            State.persistentTraits=TraitMechanics.Ensure(State.persistentTraits);
            if (State.rngState == 0) State.rngState = 1831565813;
            if (State.level < 1 || State.level > 20 || State.map == null || State.deck == null) throw new ArgumentException("손상된 저장 데이터입니다.");
            if (State.defeatedBosses == null) State.defeatedBosses = new List<string>();
            if (State.energyGrowthVersion == 0)
            {
                // Existing victories grant the new permanent boss bonus once; levels grant no energy.
                State.bossVictories = Math.Max(State.defeatedBosses.Count, Math.Max(0, State.act - 1));
                State.maxEnergyBonus = State.bossVictories;
                State.energyGrowthVersion = 1;
            }
            RefreshMapProximity();
            State.mapRows = State.map.Count > 0 ? State.map.Max(n => n.row) + 1 : RowsPerAct;
            if (State.version == 1)
            {
                // Old saves committed rewards on click. Preserve the choice, but undo only those appended copies.
                if (State.stage == RunStage.Rewards && State.rewards != null)
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
            if (State.combat != null)
            {
                var c = State.combat;
                if (c.lastSkills == null) c.lastSkills = new List<SkillHistory>();
                if (c.enemyLastSkills == null) c.enemyLastSkills = new List<SkillHistory>();
                if (c.freeCasts == null) c.freeCasts = new List<FreeCastGrant>();
                if (c.freeCastSources == null) c.freeCastSources = new List<string>();
                if (c.enemyFreeCasts == null) c.enemyFreeCasts = new List<FreeCastGrant>();
                if (c.enemyFreeCastSources == null) c.enemyFreeCastSources = new List<string>();
                if (c.enemyPlanCosts == null) c.enemyPlanCosts = new List<int>();
                if (c.enemyPlanCosts.Count != c.enemyPlan.Count) c.enemyPlanCosts = c.enemyPlan.Select(CardCost).ToList();
                if (c.enemyPlanFreeCast == null || c.enemyPlanFreeCast.Count != c.enemyPlan.Count)
                    c.enemyPlanFreeCast = c.enemyPlan.Select((id, i) => c.enemyPlanCosts[i] == 0 && CardCost(id) > 0).ToList();
                c.playerSkills = SkillMechanics.Ensure(c.playerSkills); c.enemySkills = SkillMechanics.Ensure(c.enemySkills);
                c.playerTraits=TraitMechanics.Ensure(c.playerTraits);c.enemyTraits=TraitMechanics.Ensure(c.enemyTraits);
                c.playerStatuses=StatusMechanics.Ensure(c.playerStatuses);c.enemyStatuses=StatusMechanics.Ensure(c.enemyStatuses);
                if(c.enemyGear==null)c.enemyGear=new List<string>();
                if(c.playerDeferredDamage==null)c.playerDeferredDamage=new List<DeferredDamage>();
                if(c.enemyDeferredDamage==null)c.enemyDeferredDamage=new List<DeferredDamage>();
                State.persistentTraits=c.playerTraits;
            }
            // Do not rewrite an in-progress battle's piles; equipment grants synchronize between nodes.
            if(State.stage!=RunStage.Combat)SyncWeaponCards();
        }

        public static int EnergyForLevel(int level) { return level >= 18 ? 8 : level >= 12 ? 7 : level >= 6 ? 6 : 5; }

        public bool SetRunes(string main, string support)
        {
            if (State.stage != RunStage.Preparation && !State.runeChangePending) return Fail("룬은 조우 이벤트에서만 변경할 수 있습니다.");
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
            return Say("하나: 연구소의 출구를 찾아야 해. 니아, 길을 보여 줘.");
        }

        public List<MapNode> AvailableNodes()
        {
            if (State.stage != RunStage.Map) return new List<MapNode>();
            return State.map.Where(n => n.act == State.act && n.row == State.row + 1 && (State.lane < 0 || Math.Abs(n.lane - State.lane) <= 1)).ToList();
        }

        public bool EnterNode(int lane)
        {
            RefreshMapProximity();
            var node = AvailableNodes().FirstOrDefault(n => n.lane == lane);
            if (node == null) return Fail("연결된 다음 구역만 이동할 수 있습니다.");
            State.activeNodeId = node.id; State.combat = null; State.rewards = null;
            State.pendingPassive = null; State.runeChangePending = false; State.chosenEventId = null;
            switch (node.kind)
            {
                case ZoneKind.Wildlife: StartBattle(true, false); break;
                case ZoneKind.Subject: StartBattle(false, false); break;
                case ZoneKind.Boss: StartBattle(false, true); break;
                case ZoneKind.Kiosk: State.stage = RunStage.Kiosk; Say("키오스크에 도착했습니다. 크레딧으로 오브젝트와 음식을 구매하세요."); break;
                case ZoneKind.Campfire:
                    State.hp = State.maxHp; State.stage = RunStage.Campfire; State.campChoice = 0; State.campActions = 3;
                    Say("모닥불에서 체력을 모두 회복했습니다. 만년 스프를 받거나 제작·요리 3회를 선택하세요."); break;
                case ZoneKind.Encounter:
                    State.stage = RunStage.Encounter; State.encounterOffers = Offer(GameDatabase.Events.Select(x => x.id), 3);
                    Say("세 실험체의 흔적을 발견했습니다. 하나의 조우를 선택하세요."); break;
            }
            return true;
        }

        public int CardCost(string id) { var c = GameDatabase.Card(id); return c == null ? 99 : Math.Max(0, c.cost); }
        public int EffectiveCardCost(string id)
        {
            var c = State.combat;
            return c != null && c.freeCasts.Any(g => g.cardId == id && g.uses > 0) ? 0 : Math.Max(0, CardCost(id) + (c==null?0:StatusMechanics.CostPenalty(c.playerStatuses,GameDatabase.Card(id))) - (c == null ? 0 : Math.Max(SkillMechanics.Discount(c.playerSkills,id),SkillMechanics.Discount(c.playerTraits.skills,id))));
        }
        public int EnemyEffectiveCardCost(string id)
        {
            var c = State.combat;
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
            return State.stage == RunStage.Combat && c != null && !c.enemyTurn && handIndex >= 0 && handIndex < c.hand.Count
                && GameDatabase.Card(c.hand[handIndex]) != null && StatusMechanics.CanUse(c.playerStatuses,GameDatabase.Card(c.hand[handIndex])) && EffectiveCardCost(c.hand[handIndex]) <= c.energy;
        }

        public int CardDamage(string id)
        {
            var card = GameDatabase.Card(id);
            if (card == null || card.damage <= 0) return 0;
            int value = card.damage + State.level / 3 + TotalAttack + UpgradeBonus(id, 3) + (State.combat == null ? 0 : State.combat.strength);
            value += Modifier(card.category == "basic" ? "attack_bonus" : "skill_bonus");
            if (State.combat != null && State.combat.weak > 0) value = value * 3 / 4;
            if (State.combat != null && State.combat.enemyVulnerable > 0) value = value * 3 / 2;
            if (State.combat != null) value=value*(100+TraitMechanics.Exposure(State.combat.enemyTraits))/100;
            if(State.combat!=null)value=StatusMechanics.Damage(State.combat.playerStatuses,State.combat.enemyStatuses,value);
            return Math.Max(0, value);
        }
        private int PlayerBonusDamage(string id)
        {
            var c = State.combat; var card = GameDatabase.Card(id);
            if (c == null || card == null) return 0;
            int skillDamage=SkillMechanics.Bonuses(c.playerSkills,card).damage,empower=TraitMechanics.EmpowerBonus(c.playerTraits,card);
            if(card.damage<=0 && skillDamage<=0 && empower<=0) return 0;
            int damage = skillDamage + PreviewBeforeTraits(false,card,c.playerSkills).bonus.damage + TraitMechanics.DamageBonus(c.playerTraits) + empower;
            if (c.weak > 0) damage = damage * 3 / 4;
            if (c.enemyVulnerable > 0) damage = damage * 3 / 2;
            damage=damage*(100+TraitMechanics.Exposure(c.enemyTraits))/100;
            return StatusMechanics.Damage(c.playerStatuses,c.enemyStatuses,damage);
        }
        public int CardTotalDamage(string id)
        {
            var card = GameDatabase.Card(id);
            return card == null ? 0 : CardDamage(id) * Math.Max(1, card.hits) + PlayerBonusDamage(id);
        }

        public bool PlayCard(int handIndex)
        {
            var c = State.combat;
            if (!CanPlayCard(handIndex)) return Fail(c != null && c.enemyTurn ? "적의 행동이 끝나기를 기다리세요." : "카드를 사용할 코스트가 부족하거나 사용할 수 없는 손패입니다.");
            var id = c.hand[handIndex]; var card = GameDatabase.Card(id);
            int price = EffectiveCardCost(id);
            ConsumeFreeCast(c.freeCasts, id);
            SkillMechanics.ConsumeDiscount(c.playerSkills, id);
            SkillMechanics.ConsumeDiscount(c.playerTraits.skills,id);
            var skillBefore = SkillMechanics.Clone(c.playerSkills);
            int traitHpBefore=State.hp;
            c.energy -= price; c.hand.RemoveAt(handIndex);
            if (card.exhaust) c.exhaustPile.Add(id); else c.discardPile.Add(id);
            int hits = Math.Max(1, card.hits), dealt = 0, avoided = 0, criticalHits = 0;
            int bonusDamage = PlayerBonusDamage(id);
            var incoming=card.damage>0 || bonusDamage>0 ? TraitEvent(true,"before_incoming",card,true) : new TraitResult();int incomingReduction=incoming.damageReduction,defenderBlock=c.enemyBlock;
            bool landed = card.damage == 0 && bonusDamage == 0, bonusUsed = false;
            for (int i = 0; i < hits && c.enemyHp > 0; ++i)
            {
                if (card.damage <= 0 && bonusDamage <= 0) break;
                if (Roll(EnemyEvasion)) { avoided++; Say(c.enemyName + "이(가) 공격을 회피했습니다."); continue; }
                landed = true;
                int baseDamage = CardDamage(id);
                // Only the universal basic attack's own hit is multiplied. Skill/passive bonus pulses remain separate.
                if (id == "basic_attack" && baseDamage > 0 && Roll(CritChance)) { baseDamage = CriticalBaseDamage(baseDamage); criticalHits++; }
                int damage = Math.Max(0,baseDamage + (bonusUsed ? 0 : bonusDamage) - (bonusUsed?0:incomingReduction)), absorbed = Math.Min(c.enemyBlock, damage);
                bonusUsed = true;
                c.enemyBlock -= absorbed; damage -= absorbed; dealt += TakeHealthDamage(true,damage);
            }
            if (card.damage > 0 || bonusDamage > 0) c.attacksPlayed++;
            var bonuses = SkillMechanics.Bonuses(skillBefore, card, landed);
            int block = (card.block > 0 ? card.block + UpgradeBonus(id, 3) : 0) + bonuses.block, beforeHp = State.hp;
            c.block += block;
            if (card.heal > 0 || bonuses.heal > 0) Heal((card.heal > 0 ? card.heal + UpgradeBonus(id, 2) : 0) + bonuses.heal);
            c.strength += card.strength;
            if (landed) { c.enemyPoison += card.poison; c.enemyVulnerable = Math.Max(c.enemyVulnerable, card.vulnerable); c.enemyWeak = Math.Max(c.enemyWeak, card.weak); }
            if (card.evasion > 0) { c.evasion = Math.Max(c.evasion, card.evasion); c.evasionTurns = Math.Max(c.evasionTurns, card.duration); }
            c.energy += card.energy;
            DrawCards(card.draw);
            SkillMechanics.AfterCard(c.playerSkills, skillBefore, card, landed);
            StatusMechanics.CancelMissingInstallations(c.enemyStatuses,c.playerSkills);
            StatusMechanics.DamageTaken(c.enemyStatuses,dealt);
            bool statusControl=ApplyCardStatuses(false,skillBefore,card,landed);
            RecallSkillDiscounts(c.playerSkills, skillBefore, State.deck, c.hand, c.drawPile, c.discardPile);
            if (RecordLastSkill(c.lastSkills, card)) c.lastSkillCard = id;
            if (!card.freeCastOnHit || landed) GrantFreeCasts(card, State.deck, c.freeCasts, c.freeCastSources, c.hand, c.drawPile, c.discardPile, true, LastSkill(c.lastSkills, card.owner));
            RefreshEnemyIntent();
            int cardHeal=State.hp-beforeHp;
            CombatActions.Add(new CombatAction { id = ++actionSerial, cardId = id, enemy = false, damage = dealt, block = block, heal = cardHeal, avoided = avoided, critical = criticalHits > 0, criticalHits = criticalHits });
            bool attackAttempt=card.damage>0 || bonusDamage>0;
            ApplyBeforeTraits(false,card,landed && attackAttempt,skillBefore,traitHpBefore);
            AfterCardTraits(false,card,landed,dealt,block,cardHeal,avoided,attackAttempt);
            if(statusControl)TraitEvent(false,"control",card,true,dealt);
            if(defenderBlock>0 && c.enemyBlock==0) TraitEvent(true,"block_break",card,landed,dealt);
            Say(card.name + " 사용" + (criticalHits > 0 ? " · 치명타!" : "") + (card.damage > 0 ? " · 피해 " + dealt : "") + (block > 0 ? " · 방어 +" + block : ""));
            if (landed && (card.damage > 0 || bonusDamage > 0)) ResolveCounters(true);
            CheckBattleEnd();
            return true;
        }

        public bool BeginEndTurn()
        {
            var c = State.combat;
            if (State.stage != RunStage.Combat || c == null || c.enemyTurn) return Fail("현재 플레이어의 턴이 아닙니다.");
            c.discardPile.AddRange(c.hand); c.hand.Clear();
            c.freeCasts.Clear(); c.freeCastSources.Clear();
            if (c.weak > 0) c.weak--;
            if (c.vulnerable > 0) c.vulnerable--;
            if (c.enemyPoison > 0)
            {
                int damage = TakeHealthDamage(true,c.enemyPoison);
                Say(c.enemyName + " 중독 피해 " + damage); c.enemyPoison--;
                CombatActions.Add(new CombatAction { id = ++actionSerial, cardId = "status_poison", enemy = false, damage = damage });
                if(damage>0){TraitEvent(true,"damaged",null,true,damage);TraitEvent(true,"low_health",null,true,damage);}
            }
            ResolveSkillTicks(false);
            TraitEvent(false,"turn_end",null,true);
            TraitMechanics.EndTurn(c.playerTraits);
            StatusMechanics.EndTurn(c.playerStatuses);
            if (CheckBattleEnd()) return true;
            c.enemyBlock = 0;
            if (c.enemyEvasionTurns > 0 && --c.enemyEvasionTurns == 0) c.enemyEvasion = BaseEnemyEvasion();
            c.enemyTurn = true; c.enemyActionIndex = 0; c.enemyFreeCasts.Clear(); c.enemyFreeCastSources.Clear();
            c.enemyAvailableEnergy = EnergyForLevel(c.enemyLevel)+TraitMechanics.EnergyBonus(c.enemyTraits)+c.enemyOpeningEnergy;c.enemyOpeningEnergy=0;
            SkillMechanics.StartTurn(c.enemySkills);
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
            if (c.enemyActionIndex >= c.enemyPlan.Count)
            {
                c.enemyTurn = false; c.enemyPlan.Clear(); c.enemyPlanCosts.Clear(); c.enemyPlanFreeCast.Clear(); c.enemyHand.Clear();
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
            var id = c.enemyPlan[index]; var card = GameDatabase.Card(id);
            if (card == null) return true;
            if(!StatusMechanics.CanUse(c.enemyStatuses,card)){Say(c.enemyName+"의 "+card.name+" 사용이 상태이상으로 막혔습니다.");return true;}
            bool freeGranted = c.enemyFreeCasts.Any(g => g.cardId == id && g.uses > 0);
            bool plannedFree = index < c.enemyPlanFreeCast.Count && c.enemyPlanFreeCast[index];
            // A predicted reset may have recalled this exact copy. A missed source must not turn it into a second paid cast.
            if (plannedFree && !freeGranted) { Say(c.enemyName + "의 " + card.name + " 연계가 취소되었습니다."); return true; }
            int price = EnemyEffectiveCardCost(id);
            if(index<c.enemyPlanCosts.Count && c.enemyPlanCosts[index]==0 && CardCost(id)>0 && price>0)
            {Say(c.enemyName+"의 "+card.name+" 할인 연계가 취소되었습니다.");return true;}
            if (price > c.enemyAvailableEnergy) { Say(c.enemyName + "의 " + card.name + " 연계가 취소되었습니다."); return true; }
            ConsumeFreeCast(c.enemyFreeCasts, id);
            SkillMechanics.ConsumeDiscount(c.enemySkills, id);
            SkillMechanics.ConsumeDiscount(c.enemyTraits.skills,id);
            var skillBefore = SkillMechanics.Clone(c.enemySkills);
            int traitHpBefore=c.enemyHp;
            c.enemyAvailableEnergy -= price; c.enemyAvailableEnergy += card.energy;
            int damagePerHit = EnemyCardDamage(card), bonusDamage = EnemyTotalBonusDamage(card,c.enemySkills,c.enemyTraits,c.vulnerable), hits = Math.Max(1, card.hits), dealt = 0, avoided = 0, criticalHits = 0;
            var incoming=damagePerHit>0 || bonusDamage>0 ? TraitEvent(false,"before_incoming",card,true) : new TraitResult();int incomingReduction=incoming.damageReduction,defenderBlock=c.block;
            bool landed = damagePerHit == 0 && bonusDamage == 0, bonusUsed = false;
            for (int i = 0; i < hits && State.hp > 0; ++i)
            {
                if (damagePerHit == 0 && bonusDamage == 0) break;
                if (Roll(Evasion)) { avoided++; Say("하나가 " + card.name + "을(를) 회피했습니다."); continue; }
                landed = true;
                int baseDamage = damagePerHit;
                if (id == "basic_attack" && baseDamage > 0 && Roll(EnemyCritChance)) { baseDamage = CriticalBaseDamage(baseDamage); criticalHits++; }
                int damage = Math.Max(0,baseDamage + (bonusUsed ? 0 : bonusDamage) - (bonusUsed?0:incomingReduction)), absorbed = Math.Min(c.block, damage); c.block -= absorbed;
                bonusUsed = true;
                dealt += TakeHealthDamage(false,damage-absorbed);
            }
            var bonuses = SkillMechanics.Bonuses(skillBefore, card, landed);
            int addedBlock = EnemyAuxiliaryValue(card,card.block) + bonuses.block;
            c.enemyBlock += addedBlock;
            int beforeHp = c.enemyHp;
            HealEnemy(EnemyAuxiliaryValue(card,card.heal)+bonuses.heal);
            c.enemyStrength += card.strength;
            if (landed) { c.poison += card.poison; c.vulnerable = Math.Max(c.vulnerable, card.vulnerable); c.weak = Math.Max(c.weak, card.weak); }
            if (card.evasion > 0 && string.IsNullOrEmpty(c.animal)) { c.enemyEvasion = Math.Max(BaseEnemyEvasion(), card.evasion); c.enemyEvasionTurns = Math.Max(c.enemyEvasionTurns, card.duration); }
            SkillMechanics.AfterCard(c.enemySkills, skillBefore, card, landed);
            StatusMechanics.CancelMissingInstallations(c.playerStatuses,c.enemySkills);
            StatusMechanics.DamageTaken(c.playerStatuses,dealt);
            bool statusControl=ApplyCardStatuses(true,skillBefore,card,landed);
            RecordLastSkill(c.enemyLastSkills, card);
            if (!card.freeCastOnHit || landed) GrantFreeCasts(card, c.enemyDeck, c.enemyFreeCasts, c.enemyFreeCastSources, c.enemyHand, c.enemyDrawPile, c.enemyDiscardPile, false, LastSkill(c.enemyLastSkills, card.owner));
            int cardHeal=c.enemyHp-beforeHp;
            CombatActions.Add(new CombatAction { id = ++actionSerial, cardId = id, enemy = true, damage = dealt, block = addedBlock, heal = cardHeal, avoided = avoided, critical = criticalHits > 0, criticalHits = criticalHits });
            bool attackAttempt=damagePerHit>0 || bonusDamage>0;
            ApplyBeforeTraits(true,card,landed && attackAttempt,skillBefore,traitHpBefore);
            AfterCardTraits(true,card,landed,dealt,addedBlock,cardHeal,avoided,attackAttempt);
            if(statusControl)TraitEvent(true,"control",card,true,dealt);
            if(defenderBlock>0 && c.block==0) TraitEvent(false,"block_break",card,landed,dealt);
            Say(c.enemyName + " · " + card.name + (criticalHits > 0 ? " · 치명타!" : "") + (card.damage > 0 ? " · 피해 " + dealt : ""));
            if (landed && (damagePerHit > 0 || bonusDamage > 0)) ResolveCounters(false);
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
            var grant = grants.FirstOrDefault(g => g.cardId == id && g.uses > 0);
            if (grant != null) grant.uses--;
        }

        private static void GrantFreeCasts(CardDef card, IEnumerable<string> owned, List<FreeCastGrant> grants, List<string> sources, List<string> hand, List<string> draw, List<string> discard, bool recall = true, string lastSkill = null)
        {
            if (card.freeCastTargets == null || card.freeCastCount <= 0) return;
            var targets = card.freeCastTargets.Distinct().Where(id => !card.freeCastLastSkill || id == lastSkill);
            foreach (var target in targets)
            {
                string source = card.id + "|" + target;
                if (!owned.Contains(target) || GameDatabase.Card(target) == null || sources.Contains(source)) continue;
                sources.Add(source);
                var grant = grants.FirstOrDefault(g => g.cardId == target);
                if (grant == null) { grant = new FreeCastGrant { cardId = target }; grants.Add(grant); }
                grant.uses += card.freeCastCount;
                // Recall an actual owned copy. Exhausted cards stay exhausted and no new cards are created.
                if (!recall || hand.Contains(target) || hand.Count >= 12) continue;
                int index = discard.LastIndexOf(target);
                if (index >= 0) { discard.RemoveAt(index); hand.Add(target); continue; }
                index = draw.LastIndexOf(target);
                if (index >= 0) { draw.RemoveAt(index); hand.Add(target); }
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

        public bool ClaimCard(string id)
        {
            var r = State.rewards;
            if (State.stage != RunStage.Rewards || r == null || r.cardsCommitted || !r.choices.Contains(id)) return Fail("현재 보상에 있는 카드를 선택하세요.");
            if (r.taken.Remove(id)) return Say(GameDatabase.Card(id).name + " 카드 선택을 취소했습니다.");
            if (RewardCardPrice(id) > RewardRemainingBudget) return Fail("보상 카드 선택 코스트가 부족합니다. 선택한 카드를 눌러 취소할 수 있습니다.");
            r.taken.Add(id);
            return Say(GameDatabase.Card(id).name + " 카드를 선택했습니다. 확정 전까지 변경할 수 있습니다.");
        }

        public bool FinishRewards()
        {
            if (State.stage != RunStage.Rewards || State.rewards == null) return Fail("현재 받을 보상이 없습니다.");
            var r = State.rewards;
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
            if (craft) return Say("제작과 요리를 합쳐 최대 3번 할 수 있습니다.");
            State.foods.Add("soup"); CompleteNode();
            return Say("만년 스프를 받았습니다. 사용하면 체력을 모두 회복합니다.");
        }

        public bool Craft(string id, int replaceIndex = -1)
        {
            var item = GameDatabase.Equipment(id);
            if (State.stage != RunStage.Campfire || State.campChoice != 2 || State.campActions <= 0 || item == null) return Fail("제작은 모닥불에서 최대 3번 가능합니다.");
            if (!State.objects.Contains(item.objectId)) return Fail("제작에 필요한 오브젝트가 없습니다.");
            int count = State.gear.Count(g => GameDatabase.Equipment(g) != null && GameDatabase.Equipment(g).slot == item.slot);
            if (replaceIndex >= 0 && (replaceIndex >= State.gear.Count || GameDatabase.Equipment(State.gear[replaceIndex]).slot != item.slot)) return Fail("같은 부위의 장비를 교체하세요.");
            if (count >= 2 && replaceIndex < 0) return Fail("각 부위는 2개까지만 착용합니다. 교체할 장비를 선택하세요.");
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
            CompleteNode(); return true;
        }

        public bool SelectEncounter(string id)
        {
            if (State.stage != RunStage.Encounter || !State.encounterOffers.Contains(id) || !string.IsNullOrEmpty(State.chosenEventId)) return Fail("제시된 조우 3개 중 하나를 선택하세요.");
            State.chosenEventId = id;
            return Say(GameDatabase.Event(id).story);
        }

        public bool CanChooseEventOption(int index)
        {
            var ev=GameDatabase.Event(State.chosenEventId);
            if(State.stage!=RunStage.Encounter || ev==null || State.runeChangePending || index<0 || index>=ev.options.Length)return false;
            var option=ev.options[index];
            if(option.effect=="trade_card" || option.effect=="trade_object") return State.credits>=Math.Max(0,option.amount);
            if(option.effect=="risky_card" || option.effect=="risky_object") return State.hp>Math.Max(0,option.amount);
            if(option.effect=="remove_card")return State.deck.Count>5 && State.deck.Any(id=>GameDatabase.Card(id)!=null && GameDatabase.Card(id).category!="weapon");
            if(option.effect=="upgrade_card")return State.deck.Any(id=>GameDatabase.Card(id)!=null && GameDatabase.Card(id).category!="weapon" && !State.upgrades.Contains(id));
            return true;
        }
        public string EventOptionUnavailableReason(int index)
        {
            if(CanChooseEventOption(index))return "";
            var ev=GameDatabase.Event(State.chosenEventId);
            if(ev==null || index<0 || index>=ev.options.Length)return "선택할 수 없는 행동입니다.";
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
            if (State.stage != RunStage.Encounter || e == null || State.runeChangePending || index < 0 || index >= e.options.Length) return Fail("조우의 행동을 선택하세요.");
            if(!CanChooseEventOption(index)) return Fail(EventOptionUnavailableReason(index));
            var o = e.options[index]; bool pending = false;
            switch (o.effect)
            {
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
                    var upgradable = State.deck.Where(id => GameDatabase.Card(id) != null && GameDatabase.Card(id).category != "weapon" && !State.upgrades.Contains(id)).Distinct().ToList();
                    if (upgradable.Count > 0) { var upgrade = upgradable[Next(upgradable.Count)]; State.upgrades.Add(upgrade); Say(GameDatabase.Card(upgrade).name + " 강화 · 피해/방어 +3, 회복 +2"); }
                    break;
            }
            Say(o.description);
            if (State.hp <= 0) { Lose(); return true; }
            if (pending) return AcceptPassiveOrChoose();
            CompleteNode(); return true;
        }

        public bool FinishRuneChange()
        {
            if (State.stage != RunStage.Encounter || !State.runeChangePending || string.IsNullOrEmpty(State.mainRune) || string.IsNullOrEmpty(State.supportRune)) return false;
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

        public bool UseFood(int index)
        {
            if (State.stage == RunStage.Preparation || State.stage == RunStage.Won || State.stage == RunStage.Lost || (State.combat != null && State.combat.enemyTurn) || index < 0 || index >= State.foods.Count) return Fail("사용할 음식을 선택하세요.");
            var food = GameDatabase.Food(State.foods[index]); if (food == null) return false;
            if (State.hp >= State.maxHp) return Fail("체력이 이미 가득합니다.");
            State.foods.RemoveAt(index); int before = State.hp;
            if (food.fullHeal) State.hp = State.maxHp; else Heal(food.heal);
            int recovered=State.hp-before;
            if(State.stage==RunStage.Combat && recovered>0) TraitEvent(false,"heal",null,false,heal:recovered);
            return Say(food.name + " 사용 · 체력 +" + recovered);
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
                    for (int lane = 0; lane < 3; ++lane) State.map.Add(new MapNode { id = State.map.Count, act = act, row = row, lane = lane, kind = kinds[lane] });
                }
            }
            RefreshMapProximity();
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

        private void StartBattle(bool wildlife, bool boss)
        {
            SyncWeaponCards();
            var node = ActiveNode();
            int progress = (State.act - 1) * State.mapRows + node.row;
            int level = Math.Min(20, 1 + progress * 19 / Math.Max(1, State.mapRows * 3 - 1));
            var c = new CombatState { enemyLevel = level, enemyEnergy = EnergyForLevel(level), strength = Modifier("battle_start_strength"), block = Modifier("battle_start_block") };
            State.combat = c; State.stage = RunStage.Combat;
            c.playerTraits=TraitMechanics.NewBattle(State.persistentTraits);State.persistentTraits=c.playerTraits;
            if (wildlife)
            {
                string[] animals = { "chicken", "dog", "boar", "wolf", "bear" };
                string[] names = { "닭", "들개", "멧돼지", "늑대", "곰" };
                int[] health = { 20, 28, 38, 44, 62 };
                int pick = Next(animals.Length); c.animal = c.enemyId = animals[pick]; c.enemyName = names[pick];
                c.enemyMaxHp = (int)Math.Ceiling((health[pick] + level * 3) * (1.15f + (State.act-1)*.12f)); c.enemyDeck.Add("basic_attack");
            }
            else
            {
                var combatRoster = GameDatabase.Characters.Where(x => x.cards != null && x.cards.Length > 0 && (!boss || GameDatabase.Passive(x.passiveId) != null)).ToList();
                var candidates = combatRoster.Where(x => !boss || (x.id != "nia" && !State.defeatedBosses.Contains(x.id))).ToList();
                if (candidates.Count == 0) candidates = combatRoster;
                var character = boss && State.act == 3 ? GameDatabase.Character("nia") : candidates[Next(candidates.Count)];
                c.enemyId = character.id; c.enemyName = (boss ? "보스 · " : "") + character.name;
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
            c.enemyHp = c.enemyMaxHp;
            c.drawPile.AddRange(State.deck); Shuffle(c.drawPile);
            c.enemyDrawPile.AddRange(c.enemyDeck); Shuffle(c.enemyDrawPile);
            Heal(Modifier("battle_start_heal"));
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
            int deferral=Math.Min(20,enemy?EnemyEquipmentSum(g=>g.damageDeferral):EquipmentSum(g=>g.damageDeferral));
            int delayed=Math.Max(0,damage)*deferral/100;
            if(delayed>0)(enemy?combat.enemyDeferredDamage:combat.playerDeferredDamage).Add(new DeferredDamage{amount=delayed,remaining=3});
            int immediate=Math.Min(hp,Math.Max(0,damage-delayed));
            if(enemy)combat.enemyHp-=immediate;else State.hp-=immediate;
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
            CombatActions.Add(new CombatAction{id=++actionSerial,cardId="ao_dai",kind="deferred_damage",enemy=!enemy,damage=damage});
            Say((enemy?combat.enemyName:"하나")+" 유예 피해 "+damage);
            TraitEvent(enemy,"damaged",null,true,damage);TraitEvent(enemy,"low_health",null,true,damage);
        }

        private void StartPlayerTurn(bool first = false)
        {
            var c = State.combat; c.turn++; c.energy = MaxEnergy + Modifier("turn_energy")+(first?c.playerOpeningEnergy:0); c.playerOpeningEnergy=0; c.attacksPlayed = 0;
            if (!first) SkillMechanics.StartTurn(c.playerSkills);
            TraitMechanics.StartTurn(c.playerTraits);
            c.enemyTurn = false; c.freeCasts.Clear(); c.freeCastSources.Clear();
            if (!first) c.block = 0;
            c.block += TotalBlock + Modifier("turn_block"); Heal(Modifier("turn_heal"));
            TraitEvent(false,"turn_start",null,true);
            ResolveDeferredDamage(false);
            c.energy=Math.Max(1,c.energy-ControlEnergyPenalty(false,c.playerStatuses));
            if (c.poison > 0)
            {
                int damage = TakeHealthDamage(false,c.poison);
                Say("하나 중독 피해 " + damage); c.poison--;
                CombatActions.Add(new CombatAction { id = ++actionSerial, cardId = "status_poison", enemy = true, damage = damage });
                if(damage>0){TraitEvent(false,"damaged",null,true,damage);TraitEvent(false,"low_health",null,true,damage);}
            }
            if (CheckBattleEnd()) return;
            DrawCards(5 + Modifier("turn_draw")); PlanEnemyTurn();
            Say(c.turn + "턴 · 코스트 " + c.energy + "/" + MaxEnergy);
        }

        private void PlanEnemyTurn()
        {
            var c = State.combat; c.enemyPlan.Clear(); c.enemyPlanCosts.Clear(); c.enemyPlanFreeCast.Clear(); c.enemyHand.Clear();
            c.enemyActionIndex = 0; c.enemyEnergy = EnergyForLevel(c.enemyLevel)+TraitMechanics.EnergyBonus(c.enemyTraits)+c.enemyOpeningEnergy;
            if (!string.IsNullOrEmpty(c.animal))
            {
                c.enemyPlan.Add("basic_attack"); c.enemyPlanCosts.Add(CardCost("basic_attack")); c.enemyPlanFreeCast.Add(false);
                if (c.animal == "wolf" || (c.animal == "dog" && c.enemyLevel >= 8)) { c.enemyPlan.Add("basic_attack"); c.enemyPlanCosts.Add(CardCost("basic_attack")); c.enemyPlanFreeCast.Add(false); }
            }
            else
            {
                DrawInto(c.enemyHand, c.enemyDrawPile, c.enemyDiscardPile, 4 + c.enemyLevel / 8);
                int available = c.enemyEnergy;
                var grants = new List<FreeCastGrant>(); var sources = new List<string>();
                var history = c.enemyLastSkills.Select(x => new SkillHistory { owner = x.owner, cardId = x.cardId }).ToList();
                var skillPlan = SkillMechanics.Clone(c.enemySkills);
                SkillMechanics.StartTurn(skillPlan);
                var traitPlan=TraitMechanics.Clone(c.enemyTraits);TraitMechanics.StartTurn(traitPlan);
                var planStatuses=StatusMechanics.Clone(c.enemyStatuses);
                int planStrength=c.enemyStrength,planWeak=c.enemyWeak,planHp=c.enemyHp,planTargetHp=State.hp,planTargetBlock=c.block;
                var turnContext=TraitContextFor(true,null,true,skillActor:skillPlan);
                var opening=TraitMechanics.Resolve(traitPlan,ActorTraits(true),"turn_start",turnContext);available+=opening.pulses.Where(x=>x.kind=="energy").Sum(x=>x.amount);
                foreach(var pulse in opening.pulses){if(pulse.kind=="strength")planStrength+=pulse.amount;if(pulse.kind=="heal")planHp=Math.Min(c.enemyMaxHp,planHp+pulse.amount*(100-TraitMechanics.HealingReduction(traitPlan))/100);if(pulse.kind=="cleanse")planWeak=0;}
                if(opening.pulses.Any(x=>x.kind=="cleanse"))planStatuses.effects.RemoveAll(x=>!x.pending);
                available=Math.Max(1,available-ControlEnergyPenalty(true,planStatuses));c.enemyEnergy=available;
                // Plan against real piles, including draws and target recalls, so the preview and actual turn agree.
                // Moving each planned card into a pile now preserves a finite deck even when a reset reuses a copy.
                while (c.enemyPlan.Count < 40)
                {
                    int index = c.enemyHand.FindIndex(id => StatusMechanics.CanUse(planStatuses,GameDatabase.Card(id)) && (grants.Any(g => g.cardId == id && g.uses > 0) ? 0 : Math.Max(0, CardCost(id) + StatusMechanics.CostPenalty(planStatuses,GameDatabase.Card(id)) - Math.Max(SkillMechanics.Discount(skillPlan,id),SkillMechanics.Discount(traitPlan.skills,id)))) <= available);
                    if (index < 0) break;
                    string id = c.enemyHand[index]; var card = GameDatabase.Card(id);
                    if (card == null) { c.enemyHand.RemoveAt(index); continue; }
                    bool freeGranted = grants.Any(g => g.cardId == id && g.uses > 0);
                    int price = freeGranted ? 0 : Math.Max(0, CardCost(id) + StatusMechanics.CostPenalty(planStatuses,card) - Math.Max(SkillMechanics.Discount(skillPlan,id),SkillMechanics.Discount(traitPlan.skills,id)));
                    ConsumeFreeCast(grants, id);
                    SkillMechanics.ConsumeDiscount(skillPlan, id);
                    SkillMechanics.ConsumeDiscount(traitPlan.skills,id);
                    var before = SkillMechanics.Clone(skillPlan);
                    var traitBefore=SkillMechanics.Clone(traitPlan.skills);
                    var bonuses=SkillMechanics.Bonuses(before,card);
                    var beforeTraits=ResolveBeforeTraits(traitPlan,true,card,card.damage>0 || bonuses.damage>0,before,planHp);
                    int planExtra=bonuses.damage;
                    if(card.damage>0 || planExtra>0)planExtra+=beforeTraits.bonus.damage+TraitMechanics.DamageBonus(traitPlan)+TraitMechanics.EmpowerBonus(traitPlan,card);
                    if(planWeak>0)planExtra=planExtra*3/4;if(c.vulnerable>1)planExtra=planExtra*3/2;
                    int raw=CalculateEnemyDamage(card,planStrength,Math.Max(0,c.vulnerable-1),weak:planWeak)*Math.Max(1,card.hits)+planExtra;
                    int absorbed=Math.Min(planTargetBlock,raw);planTargetBlock-=absorbed;int actualDamage=Math.Min(planTargetHp,raw-absorbed);planTargetHp-=actualDamage;
                    int healed=Math.Min(c.enemyMaxHp-planHp,(EnemyAuxiliaryValue(card,card.heal)+bonuses.heal)*(100-Math.Max(TraitMechanics.HealingReduction(traitPlan),StatusMechanics.HealingReduction(c.enemyStatuses)))/100);planHp+=healed;planStrength+=card.strength;
                    available+=beforeTraits.pulses.Where(x=>x.kind=="energy").Sum(x=>x.amount);
                    available -= price; available += card.energy;
                    c.enemyHand.RemoveAt(index); c.enemyPlan.Add(id); c.enemyPlanCosts.Add(price); c.enemyPlanFreeCast.Add(freeGranted);
                    if (card.exhaust) c.enemyExhaustPile.Add(id); else c.enemyDiscardPile.Add(id);
                    DrawInto(c.enemyHand, c.enemyDrawPile, c.enemyDiscardPile, card.draw);
                    RecordLastSkill(history, card);
                    SkillMechanics.AfterCard(skillPlan, before, card, true);
                    var afterTraits=SimulateAfterTraits(traitPlan,true,card,before,actualDamage,EnemyAuxiliaryValue(card,card.block)+bonuses.block,healed,planHp,planTargetHp,planTargetBlock);
                    available+=afterTraits.pulses.Where(x=>x.kind=="energy").Sum(x=>x.amount);
                    foreach(var pulse in beforeTraits.pulses.Concat(afterTraits.pulses)){if(pulse.kind=="strength")planStrength+=pulse.amount;if(pulse.kind=="heal" || pulse.kind=="bonus_heal")planHp=Math.Min(c.enemyMaxHp,planHp+pulse.amount*(100-TraitMechanics.HealingReduction(traitPlan))/100);}
                    RecallSkillDiscounts(skillPlan, before, c.enemyDeck, c.enemyHand, c.enemyDrawPile, c.enemyDiscardPile);
                    RecallSkillDiscounts(traitPlan.skills,traitBefore,c.enemyDeck,c.enemyHand,c.enemyDrawPile,c.enemyDiscardPile);
                    GrantFreeCasts(card, c.enemyDeck, grants, sources, c.enemyHand, c.enemyDrawPile, c.enemyDiscardPile, true, LastSkill(history, card.owner));
                }
                c.enemyDiscardPile.AddRange(c.enemyHand); c.enemyHand.Clear();
            }
            RefreshEnemyIntent();
        }

        private int EnemyCardDamage(CardDef card)
        {
            return CalculateEnemyDamage(card, State.combat.enemyStrength, State.combat.vulnerable);
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
        private int EnemyTotalBonusDamage(CardDef card,SkillActorState skillActor,TraitActorState traits,int vulnerable)
        {
            int bonus=SkillMechanics.Bonuses(skillActor,card).damage;
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
                int basic = c.animal == "bear" ? 11 : c.animal == "boar" ? 8 : c.animal == "wolf" ? 7 : c.animal == "dog" ? 6 : 5;
                value = basic + c.enemyLevel / 2;
            }
            else value = EnemyAuxiliaryValue(card,card.damage) + c.enemyLevel / 2 + strength + EnemyEquipmentSum(g=>g.attack);
            if ((weak>=0?weak:c.enemyWeak) > 0) value = value * 3 / 4;
            if (vulnerable > 0) value = value * 3 / 2;
            value=value*(100+(exposure>=0?exposure:TraitMechanics.Exposure(c.playerTraits)))/100;
            return StatusMechanics.Damage(attackerStatuses ?? c.enemyStatuses,defenderStatuses ?? c.playerStatuses,value);
        }

        private void RefreshEnemyIntent()
        {
            var c = State.combat;
            if (c == null) return;
            int strength = c.enemyStrength,weak=c.enemyWeak,defenderHp=State.hp,defenderBlock=c.block;
            var skillPreview = SkillMechanics.Clone(c.enemySkills);
            var traitPreview=TraitMechanics.Clone(c.enemyTraits);int actorHp=c.enemyHp;
            var defenderTraits=TraitMechanics.Clone(c.playerTraits);
            var attackerStatuses=StatusMechanics.Clone(c.enemyStatuses);var defenderStatuses=StatusMechanics.Clone(c.playerStatuses);
            int available=c.enemyTurn?c.enemyAvailableEnergy:EnergyForLevel(c.enemyLevel)+TraitMechanics.EnergyBonus(c.enemyTraits)+c.enemyOpeningEnergy;
            c.intentDamage = c.intentBlock = 0;
            if (!c.enemyTurn)
            {
                SkillMechanics.StartTurn(skillPreview);TraitMechanics.StartTurn(traitPreview);TraitMechanics.EndTurn(defenderTraits);
                StatusMechanics.EndTurn(defenderStatuses);
                var opening=TraitMechanics.Resolve(traitPreview,ActorTraits(true),"turn_start",TraitContextFor(true,null,true,skillActor:skillPreview));
                foreach(var pulse in opening.pulses){if(pulse.kind=="strength")strength+=pulse.amount;if(pulse.kind=="heal")actorHp=Math.Min(c.enemyMaxHp,actorHp+pulse.amount*(100-TraitMechanics.HealingReduction(traitPreview))/100);if(pulse.kind=="block")c.intentBlock+=pulse.amount;if(pulse.kind=="cleanse")weak=0;}
                if(opening.pulses.Any(x=>x.kind=="cleanse"))attackerStatuses.effects.RemoveAll(x=>!x.pending);
                c.intentBlock+=EnemyEquipmentSum(g=>g.block);
                available+=opening.pulses.Where(x=>x.kind=="energy").Sum(x=>x.amount);available=Math.Max(1,available-ControlEnergyPenalty(true,attackerStatuses));
            }
            // Existing player debuffs tick when this player turn ends, before the enemy acts.
            int vulnerable = c.enemyTurn ? c.vulnerable : Math.Max(0, c.vulnerable - 1);
            for(int intentIndex=c.enemyTurn?c.enemyActionIndex:0;intentIndex<c.enemyPlan.Count;++intentIndex)
            {
                var id=c.enemyPlan[intentIndex];
                var card = GameDatabase.Card(id);
                if (card == null) continue;
                if(!StatusMechanics.CanUse(attackerStatuses,card))continue;
                int price=c.enemyFreeCasts.Any(x=>x.cardId==id && x.uses>0)?0:Math.Max(0,CardCost(id)+StatusMechanics.CostPenalty(attackerStatuses,card)-Math.Max(SkillMechanics.Discount(skillPreview,id),SkillMechanics.Discount(traitPreview.skills,id)));
                // Earlier on-hit resets are represented in the saved plan; no new RNG or pile changes occur here.
                if(intentIndex<c.enemyPlanCosts.Count && c.enemyPlanCosts[intentIndex]==0 && CardCost(id)>0)price=0;
                if(price>available)continue;available-=price;available+=card.energy;
                var skillBefore=SkillMechanics.Clone(skillPreview);var bonuses=SkillMechanics.Bonuses(skillBefore,card);
                var beforeTraits=ResolveBeforeTraits(traitPreview,true,card,card.damage>0 || bonuses.damage>0,skillBefore,actorHp);
                int extra=bonuses.damage;
                if(card.damage>0 || extra>0)extra+=beforeTraits.bonus.damage+TraitMechanics.DamageBonus(traitPreview)+TraitMechanics.EmpowerBonus(traitPreview,card);
                if(weak>0)extra=extra*3/4;if(vulnerable>0)extra=extra*3/2;extra=extra*(100+TraitMechanics.Exposure(defenderTraits))/100;
                int baseRaw=CalculateEnemyDamage(card,strength,vulnerable,TraitMechanics.Exposure(defenderTraits),weak,attackerStatuses,defenderStatuses);
                // CalculateEnemyDamage already includes the live status modifiers; this preview uses its cloned state.
                int raw=baseRaw*Math.Max(1,card.hits)+StatusMechanics.Damage(attackerStatuses,defenderStatuses,extra);
                c.intentDamage+=raw;c.intentBlock+=EnemyAuxiliaryValue(card,card.block)+bonuses.block+beforeTraits.bonus.block;
                int previousDefenderBlock=defenderBlock;
                int absorbed=Math.Min(defenderBlock,raw);defenderBlock-=absorbed;int actualDamage=Math.Min(defenderHp,raw-absorbed);defenderHp-=actualDamage;
                StatusMechanics.DamageTaken(defenderStatuses,actualDamage);
                int cardHeal=Math.Min(c.enemyMaxHp-actorHp,(EnemyAuxiliaryValue(card,card.heal)+bonuses.heal)*(100-Math.Max(TraitMechanics.HealingReduction(traitPreview),StatusMechanics.HealingReduction(attackerStatuses)))/100);actorHp+=cardHeal;
                strength += card.strength;
                SkillMechanics.ConsumeDiscount(skillPreview, id);
                SkillMechanics.AfterCard(skillPreview,skillBefore,card,true);
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
        private void RecallSkillDiscounts(SkillActorState actor, SkillActorState before, IEnumerable<string> owned, List<string> hand, List<string> draw, List<string> discard)
        {
            foreach (var discount in actor.discounts)
            {
                string id=discount.targetCard;
                if (discount.amount<CardCost(id) || SkillMechanics.Discount(before,id)>=discount.amount || !owned.Contains(id) || hand.Contains(id) || hand.Count>=12) continue;
                int index=discard.LastIndexOf(id);
                if (index>=0) { discard.RemoveAt(index);hand.Add(id);continue; }
                index=draw.LastIndexOf(id);
                if (index>=0) { draw.RemoveAt(index);hand.Add(id); }
            }
        }
        private void DrawCards(int amount) { DrawInto(State.combat.hand, State.combat.drawPile, State.combat.discardPile, Math.Max(0, amount)); }
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
            if(!enemy) foreach(var id in new[]{State.mainRune,State.supportRune}.Distinct())
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
            var owned=new HashSet<string>(State.passives.Concat(new[]{State.mainRune,State.supportRune}).Where(x=>!string.IsNullOrEmpty(x)));
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
            if(!enemy)RecallSkillDiscounts(actor.skills,before,State.deck,c.hand,c.drawPile,c.discardPile);
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
                        case "bonus_block":block=Math.Min(bonusBlock,pulse.amount);bonusBlock-=block;if(enemy)c.enemyBlock+=block;else c.block+=block;break;
                        case "block":block=pulse.amount;if(enemy)c.enemyBlock+=block;else c.block+=block;break;
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
                    CombatActions.Add(new CombatAction {id=++actionSerial,cardId=pulse.sourceCard,traitId=pulse.sourceId,kind=pulse.kind.StartsWith("status_",StringComparison.Ordinal)?pulse.kind:"trait_"+pulse.kind,enemy=enemy,damage=damage,heal=heal,block=block});
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
            if (State.hp <= 0) { Lose(); return true; }
            if (State.stage == RunStage.Combat && State.combat.enemyHp <= 0) { WinBattle(); return true; }
            return State.stage != RunStage.Combat;
        }
        private void ResolveRevive(bool enemy)
        {
            var c = State.combat;
            var pulse = SkillMechanics.Revive(enemy ? c.enemySkills : c.playerSkills) ?? SkillMechanics.Revive((enemy?c.enemyTraits:c.playerTraits).skills);
            if (pulse == null) return;
            if (enemy) c.enemyHp = Math.Min(c.enemyMaxHp, pulse.amount); else State.hp = Math.Min(State.maxHp, pulse.amount);
            CombatActions.Add(new CombatAction { id=++actionSerial, cardId=pulse.sourceCard,traitId=pulse.traitId, kind="revive", enemy=enemy, heal=enemy ? c.enemyHp : State.hp });
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
        private void ResolveCounters(bool enemyDefender)
        {
            var c = State.combat;
            foreach (var pulse in SkillMechanics.Counters(enemyDefender ? c.enemySkills : c.playerSkills).Concat(SkillMechanics.Counters((enemyDefender?c.enemyTraits:c.playerTraits).skills)))
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
                    CombatActions.Add(new CombatAction {id=++actionSerial,cardId=status.sourceCard,kind=status.kind,enemy=enemy});
        }
        private void ResolveSkillPulse(SkillPulse pulse, bool enemy)
        {
            var c = State.combat; int damage = 0, heal = 0, block = 0;
            if (pulse.kind == "guard")
            {
                block = pulse.amount;
                if (enemy) c.enemyBlock += block; else c.block += block;
            }
            else if (pulse.kind == "hot")
            {
                int before = enemy ? c.enemyHp : State.hp;
                if (enemy) HealEnemy(pulse.amount); else Heal(pulse.amount);
                heal = (enemy ? c.enemyHp : State.hp) - before;
            }
            else
            {
                int amount = pulse.amount;
                if (pulse.kind != "bleed" && pulse.kind != "burn")
                {
                    int absorbed = Math.Min(enemy ? c.block : c.enemyBlock, amount);
                    if (enemy) c.block -= absorbed; else c.enemyBlock -= absorbed;
                    amount -= absorbed;
                }
                damage = TakeHealthDamage(!enemy,amount);
            }
            CombatActions.Add(new CombatAction { id=++actionSerial, cardId=pulse.sourceCard,traitId=pulse.traitId, kind=pulse.kind, enemy=enemy, damage=damage, heal=heal, block=block });
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
            var c = State.combat; var node = ActiveNode(); bool boss = node.kind == ZoneKind.Boss;
            TraitEvent(false,"battle_win",null,true);
            int rewardEnergy=EnergyForLevel(State.level);
            var r = new RewardState { boss = boss, xp = (boss ? 180 : node.kind == ZoneKind.Subject ? 90 : 50) + c.enemyLevel * (boss ? 14 : node.kind == ZoneKind.Subject ? 12 : 9), credits = (boss ? 185 : node.kind == ZoneKind.Subject ? 80 : 48) + c.enemyLevel * 5 + Next(25) + Modifier("reward_credit"), cardBudget = boss ? rewardEnergy + 2 : node.kind == ZoneKind.Subject ? rewardEnergy : rewardEnergy - 1 };
            if (node.kind == ZoneKind.Wildlife)
            {
                r.choices = CardOffer(RandomCardPool(), 3, ref State.rngState);
                if (c.animal == "wolf" && Roll(25)) r.objectId = RandomObject(2);
                if (c.animal == "bear" && Roll(35)) r.objectId = RandomObject(4);
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
                    State.maxEnergyBonus++;
                    if (character != null && !State.passives.Contains(character.passiveId)) State.pendingPassive = character.passiveId;
                    r.objectId = RandomObject(4);
                }
                else if (IsNearKiosk(node) && Roll(25)) r.objectId = RandomObject(4);
            }
            State.rewards = r; State.stage = RunStage.Rewards; State.credits += r.credits;
            if (!string.IsNullOrEmpty(r.objectId)) State.objects.Add(r.objectId);
            if (!string.IsNullOrEmpty(r.foodId)) State.foods.Add(r.foodId);
            GainXp(r.xp); Heal(Modifier("kill_heal"));
            Say("전투 승리 · 경험치 +" + r.xp + " · 크레딧 +" + r.credits + (boss ? " · 최대 코스트 +1 (" + PlayerBaseEnergy + ")" : "") + (r.objectId == null ? "" : " · " + GameDatabase.Object(r.objectId).name) + (r.foodId == null ? "" : " · " + GameDatabase.Food(r.foodId).name + " 1개"));
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
            node.visited = true; State.row = node.row; State.lane = node.lane;
            State.activeNodeId = -1; State.combat = null; State.stage = RunStage.Map;
            State.encounterOffers.Clear(); State.chosenEventId = null;
            if (node.kind == ZoneKind.Boss)
            {
                if (State.act >= 3) { State.stage = RunStage.Won; Say("니아: 출구는 바로 여기야. 하나는 VF 세계를 탈출해 아글라이아 연구소로 돌아왔다."); }
                else { State.act++; State.row = State.lane = -1; Say("제" + State.act + "구역으로 진입했습니다. 더 강한 실험체들이 기다립니다."); }
            }
        }

        private void Lose() { State.hp = 0; State.stage = RunStage.Lost; Say("VF 연결이 끊어졌습니다. 니아의 손을 다시 잡고 탐사를 시작하세요."); }
        private void Heal(int amount)
        {
            int reduction=State.stage==RunStage.Combat && State.combat!=null ? Math.Max(TraitMechanics.HealingReduction(State.combat.playerTraits),StatusMechanics.HealingReduction(State.combat.playerStatuses)) : 0;
            State.hp = Math.Min(State.maxHp, Math.Max(0, State.hp + Math.Max(0, amount)*(100-reduction)/100));
        }
        private void HealEnemy(int amount)
        {
            var c=State.combat;
            c.enemyHp=Math.Min(c.enemyMaxHp,c.enemyHp+Math.Max(0,amount)*(100-Math.Max(TraitMechanics.HealingReduction(c.enemyTraits),StatusMechanics.HealingReduction(c.enemyStatuses)))/100);
        }
        private int UpgradeBonus(string id, int amount) { return State.upgrades.Contains(id) ? amount : 0; }
        private int EquipmentSum(Func<GearDef, int> value) { return State.gear.Select(GameDatabase.Equipment).Where(x => x != null).Sum(value); }
        private int Modifier(string trigger)
        {
            int sum = State.passives.Select(GameDatabase.Passive).Where(x => x != null && x.trigger == trigger && (x.mechanics==null || !x.mechanics.replaceLegacy)).Sum(x => x.amount);
            var main = GameDatabase.Rune(State.mainRune); var support = GameDatabase.Rune(State.supportRune);
            if (main != null && main.trigger == trigger && (main.mechanics==null || !main.mechanics.replaceLegacy)) sum += main.amount;
            if (support != null && support.trigger == trigger && (support.mechanics==null || !support.mechanics.replaceLegacy)) sum += support.amount;
            sum += State.gear.Select(GameDatabase.Equipment).Where(x => x != null && x.effect == trigger).Sum(x => x.amount);
            return sum;
        }
        private void RecalculateHealth(bool full)
        {
            int before = State.maxHp;
            State.maxHp = Math.Max(1, 80 + 4 * (State.level - 1) + State.maxHealthBonus + Modifier("max_health") + EquipmentSum(x => x.health));
            State.hp = full ? State.maxHp : Math.Min(State.maxHp, Math.Max(1, State.hp + Math.Max(0, State.maxHp - before)));
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
        private bool Roll(int percent) { return percent > 0 && Next(100) < Math.Min(100, percent); }
        private bool Fail(string message) { State.message = message; return false; }
        private bool Say(string message)
        {
            State.message = message; if (!string.IsNullOrEmpty(message)) State.log.Add(message);
            if (State.log.Count > 80) State.log.RemoveAt(0); return true;
        }
    }
}
