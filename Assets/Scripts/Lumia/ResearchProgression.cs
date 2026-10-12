using System;
using System.Collections.Generic;
using System.Linq;

namespace Lumia
{
    [Serializable] public sealed class ResearchSubjectProgress
    {
        public string subjectId;
        public bool observed, uniqueCompleted;
        public List<string> usedSkills = new List<string>();
    }
    [Serializable] public sealed class ResearchRewardLine
    {
        public string title;
        public int count, unit, amount;
    }
    [Serializable] public sealed class ResearchSettlement
    {
        public string runId, finishedAt;
        public int seed, nodes, victories, bosses, interruptions, escapeBonus, newSubjects, newTasks, newBosses, newSynergies, earned;
        public bool escaped, alreadySettled, legacy;
        public List<ResearchRewardLine> lines = new List<ResearchRewardLine>();
        public List<string> completedTasks = new List<string>(), observedSubjects = new List<string>(), synergyKeys = new List<string>();
    }
    [Serializable] public sealed class ResearchProfile
    {
        public int version = 1, data, totalData, totalRuns, totalWins;
        public List<ResearchSubjectProgress> subjects = new List<ResearchSubjectProgress>();
        public List<string> completedReports = new List<string>(), settledRunIds = new List<string>(), defeatedBosses = new List<string>(), synergyKeys = new List<string>();
        // The permanent ledger is separate from the twenty retained recent summaries.
        public List<ResearchSettlement> settlements = new List<ResearchSettlement>(), history = new List<ResearchSettlement>();
    }
    [Serializable] public sealed class ResearchRunState
    {
        public string runId, startedAt;
        public bool begun, settled, legacyEnded;
        public List<int> completedNodeIds = new List<int>(), victoryNodeIds = new List<int>(), bossNodeIds = new List<int>();
        public List<string> observedSubjects = new List<string>(), usedSkillIds = new List<string>(), completedUniqueSubjects = new List<string>();
        public List<string> defeatedBossSubjects = new List<string>(), synergyKeys = new List<string>(), bossInterruptions = new List<string>();
        public List<string> unlockedReports = new List<string>(), completedResearchEvents = new List<string>();
        public ResearchSettlement settlement;
    }
    public sealed class ResearchTaskView
    {
        public string id, title, description;
        public int progress, target;
        public bool completed;
    }
    public sealed class ResearchSubjectDef
    {
        public string subjectId, title, reportText, unlockText;
        public ResearchTaskView[] tasks;
    }

    // Observation only: no game RNG, cards, resources, traits, combat values or route state
    // are changed by this module. Permanent progress is merged only after a terminal run.
    public static class ResearchProgression
    {
        public const int ReportCost = 12;
        sealed class Experiment
        {
            public string subjectId, kind, description;
            public CardDef card;
            public SkillRule rule;
        }
        static Dictionary<string, Experiment> experiments;
        static readonly string[] SkillKeys = { "Q", "W", "E", "R" };
        static string Stamp() => DateTime.UtcNow.ToString("o");
        static bool SubjectExists(string id) => !string.IsNullOrEmpty(id) && GameDatabase.Character(id) != null;
        static List<T> Present<T>(List<T> values) => values ?? new List<T>();
        static void Add<T>(List<T> values, T value) { if (!values.Contains(value)) values.Add(value); }
        static string SubjectOf(CardDef card) => card == null ? null : GameDatabase.Characters.FirstOrDefault(c => (c.cards ?? new string[0]).Contains(card.id))?.id;
        static string SubjectOfOwner(string owner) => GameDatabase.Characters.FirstOrDefault(c => c.id == owner || c.name == owner)?.id;
        static bool Active(RunState state) => state?.research != null && state.research.begun && !state.research.settled && !state.research.legacyEnded;

        public static ResearchProfile Normalize(ResearchProfile profile)
        {
            if (profile == null) profile = new ResearchProfile();
            profile.subjects = Present(profile.subjects).Where(x => x != null && !string.IsNullOrEmpty(x.subjectId)).GroupBy(x => x.subjectId).Select(group => new ResearchSubjectProgress {
                subjectId = group.Key, observed = group.Any(x => x.observed), uniqueCompleted = group.Any(x => x.uniqueCompleted), usedSkills = group.SelectMany(x => Present(x.usedSkills)).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList()
            }).ToList();
            profile.completedReports = Present(profile.completedReports).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();
            profile.settledRunIds = Present(profile.settledRunIds).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();
            profile.defeatedBosses = Present(profile.defeatedBosses).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();
            profile.synergyKeys = Present(profile.synergyKeys).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();
            profile.settlements = Present(profile.settlements).Where(x => x != null && !string.IsNullOrEmpty(x.runId)).GroupBy(x => x.runId).Select(x => x.First()).ToList();
            profile.history = Present(profile.history).Where(x => x != null).Take(20).ToList();
            return profile;
        }
        public static ResearchRunState EnsureRun(RunState state, bool migrateExisting = false)
        {
            if (state == null) return null;
            // Unity can materialize a missing/null Serializable field as an empty
            // instance. An absent identity is therefore also a legacy journal.
            bool added = state.research == null || string.IsNullOrEmpty(state.research.runId);
            if (state.research == null) state.research = new ResearchRunState { runId = Guid.NewGuid().ToString("N"), startedAt = Stamp() };
            var run = state.research;
            if (string.IsNullOrEmpty(run.runId)) run.runId = Guid.NewGuid().ToString("N");
            if (string.IsNullOrEmpty(run.startedAt)) run.startedAt = Stamp();
            run.completedNodeIds = Present(run.completedNodeIds); run.victoryNodeIds = Present(run.victoryNodeIds); run.bossNodeIds = Present(run.bossNodeIds);
            run.observedSubjects = Present(run.observedSubjects); run.usedSkillIds = Present(run.usedSkillIds); run.completedUniqueSubjects = Present(run.completedUniqueSubjects);
            run.defeatedBossSubjects = Present(run.defeatedBossSubjects); run.synergyKeys = Present(run.synergyKeys); run.bossInterruptions = Present(run.bossInterruptions);
            run.unlockedReports = Present(run.unlockedReports); run.completedResearchEvents = Present(run.completedResearchEvents);
            if (added && migrateExisting)
            {
                run.begun = state.stage != RunStage.Preparation;
                if (state.stage == RunStage.Won || state.stage == RunStage.Lost)
                {
                    run.legacyEnded = run.settled = true;
                    run.settlement = new ResearchSettlement { runId = run.runId, seed = state.seed, escaped = state.stage == RunStage.Won, alreadySettled = true, legacy = true, finishedAt = Stamp() };
                }
            }
            return run;
        }
        public static void Begin(RunState state)
        {
            var run = EnsureRun(state);
            if (run != null && !run.settled && !run.legacyEnded) run.begun = true;
        }
        public static void ObserveSubject(RunState state, string subjectId)
        {
            if (Active(state) && SubjectExists(subjectId)) Add(state.research.observedSubjects, subjectId);
        }
        public static void CompleteNode(RunState state, int nodeId)
        {
            if (Active(state) && nodeId >= 0 && Present(state.map).Any(n => n != null && n.id == nodeId)) Add(state.research.completedNodeIds, nodeId);
        }
        public static void Victory(RunState state, string subjectId, bool boss = false)
        {
            if (!Active(state) || state.activeNodeId < 0) return;
            Add(state.research.victoryNodeIds, state.activeNodeId);
            CompleteNode(state, state.activeNodeId);
            if (SubjectExists(subjectId)) ObserveSubject(state, subjectId);
            if (boss)
            {
                Add(state.research.bossNodeIds, state.activeNodeId);
                if (SubjectExists(subjectId)) Add(state.research.defeatedBossSubjects, subjectId);
            }
        }
        public static void BossInterrupted(RunState state, int phase)
        {
            if (Active(state) && state.stage == RunStage.Combat && state.combat?.boss?.interrupted == true && phase >= 1 && phase <= (BossMechanics.Profile(state.combat.enemyId)?.phases.Length ?? 0))
                Add(state.research.bossInterruptions, state.activeNodeId + ":" + phase);
        }

        public static void AfterCard(RunState state, CardDef card, SkillActorState before, bool landed, int actualDamage, IEnumerable<CombatAction> actions)
        {
            if (!Active(state) || state.stage != RunStage.Combat || state.combat == null || card == null || before == null) return;
            var ownActions = (actions ?? Enumerable.Empty<CombatAction>()).Where(x => x != null && !x.enemy).ToList();
            // A successful manual/free player card always has its real action record.
            // Forecasts and enemy-only subsets must never enter the research journal.
            if (!ownActions.Any(x => x.cardId == card.id && (string.IsNullOrEmpty(x.kind) || x.kind == "card" || x.kind == "attack" || x.kind == "skill" || x.kind == "basic" || x.kind == "play"))) return;
            var after = state.combat.playerSkills;
            string subjectId = SubjectOf(card);
            if (subjectId != null && card.category == "skill" && SkillKeys.Contains(card.key)) Add(state.research.usedSkillIds, card.id);
            if (subjectId == "nia" && card.id == "nia_w" && SkillMechanics.Resource(before, card.owner, "blocks") >= 3 && landed && actualDamage > 0 && SkillMechanics.Resource(after, card.owner, "blocks") < 3)
                Add(state.research.completedUniqueSubjects, "nia");
            // The source passive works with any owner's basic attack. Its third-hit
            // healing action proves a real trigger, not merely a simulated counter.
            if (card.category == "basic" && card.key == "ATK" && landed && actualDamage > 0 && ownActions.Any(x => x.traitId == "isaac_p" && x.kind == "trait_heal" && x.heal > 0))
                Add(state.research.completedUniqueSubjects, "isaac");
            var experiment = ExperimentFor(subjectId);
            if (experiment != null && subjectId != "nia" && subjectId != "isaac" && experiment.card?.id == card.id && ExperimentSucceeded(experiment, before, after, landed, actualDamage, ownActions))
                Add(state.research.completedUniqueSubjects, subjectId);
            RecordSynergies(state.research, before, after, card);
        }
        static void RecordSynergies(ResearchRunState run, SkillActorState before, SkillActorState after, CardDef card)
        {
            string consumer = SubjectOf(card);
            if (consumer == null) return;
            foreach (var use in after?.synergy?.uses ?? new List<SynergyUse>())
            {
                if (use == null || use.count <= (before.synergy?.uses ?? new List<SynergyUse>()).Where(x => x != null && x.family == use.family).Sum(x => x.count)) continue;
                // Preparations can remain after a delayed boost. Read the actual
                // activation's consumption rather than a card's potential tags.
                foreach (var charge in before.synergy?.charges ?? new List<SynergyCharge>())
                {
                    if (charge == null || charge.family != use.family || charge.owner == card.owner || charge.amount <= 0 || charge.remaining <= 0) continue;
                    int remaining = (after.synergy?.charges ?? new List<SynergyCharge>()).Where(x => x != null && x.family == charge.family && x.owner == charge.owner && x.sourceCard == charge.sourceCard).Sum(x => Math.Max(0, x.amount));
                    if (remaining >= charge.amount) continue;
                    string producer = SubjectOfOwner(charge.owner);
                    if (producer == null || producer == consumer) continue;
                    Add(run.synergyKeys, use.family + "|" + producer + "|" + consumer);
                }
            }
        }

        static Experiment ExperimentFor(string id)
        {
            if (id == null) return null;
            if (experiments == null)
            {
                experiments = new Dictionary<string, Experiment>(StringComparer.Ordinal);
                foreach (var character in GameDatabase.Characters) experiments[character.id] = BuildExperiment(character);
            }
            Experiment value; return experiments.TryGetValue(id, out value) ? value : null;
        }
        static int RuleScore(SkillRule r)
        {
            if (r == null) return -1;
            bool condition = !string.IsNullOrEmpty(r.conditionKey) || !string.IsNullOrEmpty(r.conditionKey2) || !string.IsNullOrEmpty(r.conditionPrevious);
            if (condition && new[] { "bonus_damage", "bonus_block", "bonus_heal", "consume", "state_off", "discount", "delayed_damage", "bleed", "burn", "empower_basic" }.Contains(r.op)) return 100 + Math.Max(r.conditionAmount, r.conditionAmount2);
            if (r.op == "bonus_damage" && !string.IsNullOrEmpty(r.scaleKey)) return 90;
            if (r.op == "consume") return 80;
            if (r.op == "state_toggle") return 75;
            if (r.op == "state_off") return 70;
            if (r.op == "gain" && r.cap > r.amount && r.amount > 0) return 60;
            if (r.op == "bonus_block" && !string.IsNullOrEmpty(r.scaleKey)) return 55;
            if (r.op == "state_on") return 40;
            if (r.op == "delayed_damage" || r.op == "summon" || r.op == "counter" || r.op == "empower_basic") return 30;
            if (r.op == "set" && r.amount > 0) return 20;
            return -1;
        }
        static Experiment BuildExperiment(CharacterDef character)
        {
            if (character.id == "nia") return new Experiment { subjectId = "nia", kind = "nia", card = GameDatabase.Card("nia_w"), description = "아케이드 블록이 3일 때 W를 적중시켜 블록을 회수하고 추가 피해를 줍니다." };
            if (character.id == "isaac") return new Experiment { subjectId = "isaac", kind = "isaac", description = "아이작의 패시브를 보유한 상태에서 세 번째 기본 공격을 적중시키고, 착취의 추가 피해와 체력 회복을 실제로 발동합니다. 어느 실험체의 기본 공격이든 인정됩니다." };
            var candidate = (character.cards ?? new string[0]).Select(GameDatabase.Card).Where(c => c?.mechanics != null)
                .SelectMany(c => (c.mechanics.rules ?? new SkillRule[0]).Select(r => new { card = c, rule = r, score = RuleScore(r) }))
                .Where(x => x.score >= 0).OrderByDescending(x => x.score).ThenBy(x => x.card.id, StringComparer.Ordinal).FirstOrDefault();
            if (candidate == null)
            {
                // All current subjects have source mechanics; this safe fallback
                // also keeps a future roster addition visible until it gains one.
                var card = (character.cards ?? new string[0]).Select(GameDatabase.Card).FirstOrDefault(c => c != null && c.damage > 0);
                return new Experiment { subjectId = character.id, card = card, kind = "hit", description = (card?.name ?? "기술") + "을 적중시켜 실제 피해를 줍니다." };
            }
            var result = new Experiment { subjectId = character.id, card = candidate.card, rule = candidate.rule, kind = candidate.rule.op };
            result.description = ExperimentDescription(result);
            return result;
        }
        static string ConditionText(CardDef card, SkillRule rule)
        {
            var conditions = new List<string>();
            if (!string.IsNullOrEmpty(rule.conditionKey)) conditions.Add(SkillMechanics.StateCondition(card, rule.conditionKey, rule.conditionAmount, rule.conditionExact));
            if (!string.IsNullOrEmpty(rule.conditionKey2)) conditions.Add(SkillMechanics.StateCondition(card, rule.conditionKey2, rule.conditionAmount2, rule.conditionExact2));
            if (!string.IsNullOrEmpty(rule.conditionPrevious)) conditions.Add("직전에 " + (GameDatabase.Card(rule.conditionPrevious)?.name ?? rule.conditionPrevious) + " 사용");
            if (!string.IsNullOrEmpty(rule.scaleKey)) conditions.Add(SkillMechanics.ResourceName(card, rule.scaleKey) + " 1 이상");
            return conditions.Count == 0 ? "" : "조건: " + string.Join(" · ", conditions) + ". ";
        }
        static string ExperimentDescription(Experiment e)
        {
            var rule = e.rule; var card = e.card;
            string label = SkillMechanics.ResourceName(card, rule.key ?? rule.op);
            string action;
            if (rule.op == "consume") action = label + " 수치를 실제로 소모합니다";
            else if (rule.op == "gain") action = label + " 수치를 " + rule.cap + "까지 높입니다";
            else if (rule.op == "state_toggle") action = SkillMechanics.StateName(card, rule.key) + "를 실제로 전환합니다";
            else if (rule.op == "state_on") action = SkillMechanics.StateName(card, rule.key) + "를 새로 활성화합니다";
            else if (rule.op == "state_off") action = SkillMechanics.StateName(card, rule.key) + "를 실제로 해제합니다";
            else if (rule.op == "bonus_damage") action = "조건부 추가 피해를 실제로 발동합니다";
            else if (rule.op == "bonus_block") action = "조건부 추가 방어도를 실제로 얻습니다";
            else if (rule.op == "bonus_heal") action = "조건부 체력 회복을 실제로 받습니다";
            else if (rule.op == "discount") action = (GameDatabase.Card(rule.targetCard)?.name ?? "연계 기술") + "의 코스트 감소를 새로 얻습니다";
            else if (rule.op == "set") action = label + " 수치를 " + rule.amount + "로 새로 만듭니다";
            else action = (rule.label ?? label) + " 효과를 필드에 새로 남깁니다";
            return ConditionText(card, rule) + card.name + "(" + card.key + ")을 사용해 " + action + "." + (rule.onHit ? " 공격이 적중해야 합니다." : "");
        }
        static bool Eligible(SkillRule r, SkillActorState before, CardDef card, bool landed)
        {
            if (r.onHit && !landed) return false;
            if (!string.IsNullOrEmpty(r.conditionKey))
            {
                int value = SkillMechanics.Resource(before, card.owner, r.conditionKey);
                if (r.conditionExact ? value != r.conditionAmount : value < r.conditionAmount) return false;
            }
            if (!string.IsNullOrEmpty(r.conditionKey2))
            {
                int value = SkillMechanics.Resource(before, card.owner, r.conditionKey2);
                if (r.conditionExact2 ? value != r.conditionAmount2 : value < r.conditionAmount2) return false;
            }
            if (!string.IsNullOrEmpty(r.conditionPrevious))
            {
                var history = (before.history ?? new List<SkillOwnerHistory>()).FirstOrDefault(x => x.owner == card.owner);
                var previous = history == null ? null : GameDatabase.Card(history.cardId);
                if (previous == null || (previous.id != r.conditionPrevious && previous.key != r.conditionPrevious)) return false;
            }
            return string.IsNullOrEmpty(r.scaleKey) || SkillMechanics.Resource(before, card.owner, r.scaleKey) > 0;
        }
        static bool ExperimentSucceeded(Experiment e, SkillActorState before, SkillActorState after, bool landed, int actualDamage, List<CombatAction> actions)
        {
            if (e.kind == "hit") return landed && actualDamage > 0;
            var r = e.rule; var card = e.card;
            if (r == null || after == null || !Eligible(r, before, card, landed)) return false;
            int old = SkillMechanics.Resource(before, card.owner, r.key), current = SkillMechanics.Resource(after, card.owner, r.key);
            if (r.op == "consume" || r.op == "state_off") return old > 0 && current < old;
            if (r.op == "state_toggle") return old != current;
            if (r.op == "state_on") return old == 0 && current == 1;
            if (r.op == "gain") return current >= r.cap && current > old;
            if (r.op == "set") return current == r.amount && current != old;
            if (r.op == "bonus_damage") return landed && actualDamage > 0 && r.amount > 0;
            if (r.op == "bonus_block") return r.amount > 0 && actions.Any(x => x.cardId == card.id && x.block > 0);
            if (r.op == "bonus_heal") return r.amount > 0 && actions.Any(x => x.cardId == card.id && x.heal > 0);
            if (r.op == "discount") return SkillMechanics.Discount(after, r.targetCard) > SkillMechanics.Discount(before, r.targetCard);
            return (after.effects ?? new List<SkillTimedEffect>()).Any(x => x.owner == card.owner && x.sourceCard == card.id && x.key == (r.key ?? r.op) && x.kind == r.op && x.amount > 0 && x.remaining > 0
                && !(before.effects ?? new List<SkillTimedEffect>()).Any(y => y.owner == x.owner && y.key == x.key && y.kind == x.kind && y.remaining >= x.remaining && y.amount >= x.amount));
        }

        public static ResearchSubjectDef ForSubject(string subjectId)
        {
            var character = GameDatabase.Character(subjectId); if (character == null) return null;
            return new ResearchSubjectDef { subjectId = subjectId, title = character.name + " 연구 보고서", reportText = ResearchEncounters.ReportText(subjectId) + "\n관측과 기술 활용, 고유 실험을 기록했습니다. " + ExperimentFor(subjectId).description,
                unlockText = character.name + "의 추가 실험체 조우가 새 원정의 후보에 포함됩니다. 기존 카드와 실험체는 그대로 사용할 수 있습니다.", tasks = Tasks(null, subjectId) };
        }
        public static ResearchTaskView[] Tasks(ResearchProfile profile, string subjectId)
        {
            var character = GameDatabase.Character(subjectId); if (character == null) return new ResearchTaskView[0];
            var entry = Present(profile?.subjects).FirstOrDefault(x => x != null && x.subjectId == subjectId);
            int skills = (character.cards ?? new string[0]).Select(GameDatabase.Card).Count(c => c != null && SkillKeys.Contains(c.key) && Present(entry?.usedSkills).Contains(c.id));
            return new[] {
                TaskView(subjectId + ":observe", "실험체 관측", "실제 전투에서 만나거나, 조우 후보 중 이 실험체를 선택합니다. 미리보기와 제안 목록은 포함하지 않습니다.", entry?.observed == true ? 1 : 0, 1),
                TaskView(subjectId + ":skills", "기술 연구", character.name + "의 Q·W·E·R 네 기술을 직접 사용합니다. 여러 원정에 걸쳐 누적되며, 무료 연계 사용도 포함됩니다.", skills, 4),
                TaskView(subjectId + ":unique", "고유 실험", ExperimentFor(subjectId).description, entry?.uniqueCompleted == true ? 1 : 0, 1)
            };
        }
        static ResearchTaskView TaskView(string id, string title, string description, int progress, int target) => new ResearchTaskView { id = id, title = title, description = description, progress = Math.Min(progress, target), target = target, completed = progress >= target };
        public static string ReportText(ResearchProfile profile, string subjectId)
        {
            var definition = ForSubject(subjectId); if (definition == null) return "연구 대상이 없습니다.";
            return definition.reportText + "\n" + (Present(profile?.completedReports).Contains(subjectId) ? "보고서 완성. " : "세 과제를 완료하고 관측 데이터 " + ReportCost + "로 보고서를 완성할 수 있습니다. ") + definition.unlockText;
        }
        public static string ReportError(ResearchProfile profile, string subjectId)
        {
            if (!SubjectExists(subjectId)) return "연구 대상이 없습니다.";
            if (Present(profile?.completedReports).Contains(subjectId)) return "이미 완성한 보고서입니다.";
            if (Tasks(profile, subjectId).Any(t => !t.completed)) return "세 연구 과제를 먼저 완료해야 합니다.";
            if ((profile?.data ?? 0) < ReportCost) return "관측 데이터가 " + ReportCost + " 필요합니다.";
            return "";
        }
        public static bool CompleteReport(ResearchProfile profile, string subjectId)
        {
            if (profile == null || ReportError(profile, subjectId) != "") return false;
            Normalize(profile); profile.data -= ReportCost; Add(profile.completedReports, subjectId); return true;
        }
        static ResearchSubjectProgress Combined(ResearchSubjectProgress original, ResearchRunState run, string id)
        {
            return new ResearchSubjectProgress { subjectId = id, observed = original?.observed == true || Present(run?.observedSubjects).Contains(id), uniqueCompleted = original?.uniqueCompleted == true || Present(run?.completedUniqueSubjects).Contains(id),
                usedSkills = Present(original?.usedSkills).Concat(Present(run?.usedSkillIds).Where(skill => SubjectOf(GameDatabase.Card(skill)) == id)).Distinct().ToList() };
        }
        public static ResearchSettlement CalculateReport(ResearchProfile profile, RunState state)
        {
            var run = state?.research;
            if (run == null || string.IsNullOrEmpty(run.runId)) return new ResearchSettlement { seed = state?.seed ?? 0 };
            if (Present(profile?.settledRunIds).Contains(run.runId))
            {
                var saved = Present(profile?.settlements).Concat(Present(profile?.history)).FirstOrDefault(x => x != null && x.runId == run.runId) ?? run.settlement;
                return CopySettlement(saved ?? new ResearchSettlement { runId = run.runId, seed = state.seed }, true);
            }
            if (run.legacyEnded || !run.begun || (state.stage != RunStage.Won && state.stage != RunStage.Lost))
                return new ResearchSettlement { runId = run.runId, seed = state.seed, escaped = state.stage == RunStage.Won, alreadySettled = run.legacyEnded, legacy = run.legacyEnded };
            var report = new ResearchSettlement { runId = run.runId, seed = state.seed, escaped = state.stage == RunStage.Won, nodes = Present(run.completedNodeIds).Distinct().Count(), victories = Present(run.victoryNodeIds).Distinct().Count(), bosses = Present(run.bossNodeIds).Distinct().Count(), interruptions = Present(run.bossInterruptions).Where(key => ValidInterruption(state, key)).Distinct().Count(), escapeBonus = state.stage == RunStage.Won ? 12 : 0 };
            report.observedSubjects = Present(run.observedSubjects).Where(SubjectExists).Distinct().ToList();
            report.newSubjects = report.observedSubjects.Count(id => !Present(profile?.subjects).Any(x => x != null && x.subjectId == id && x.observed));
            foreach (var character in GameDatabase.Characters)
            {
                var original = Present(profile?.subjects).FirstOrDefault(x => x != null && x.subjectId == character.id);
                var originalTasks = Tasks(profile, character.id);
                var merged = Combined(original, run, character.id);
                var combinedProfile = new ResearchProfile { subjects = new List<ResearchSubjectProgress> { merged } };
                var mergedTasks = Tasks(combinedProfile, character.id);
                for (int i = 0; i < mergedTasks.Length; ++i) if (mergedTasks[i].completed && !originalTasks[i].completed) report.completedTasks.Add(mergedTasks[i].id);
            }
            report.newTasks = report.completedTasks.Count;
            report.newBosses = Present(run.defeatedBossSubjects).Where(SubjectExists).Distinct().Count(id => !Present(profile?.defeatedBosses).Contains(id));
            report.synergyKeys = Present(run.synergyKeys).Where(ValidSynergy).Distinct().ToList();
            report.newSynergies = report.synergyKeys.Count(key => !Present(profile?.synergyKeys).Contains(key));
            Line(report, "구역 완료", report.nodes, 1); Line(report, "전투 승리", report.victories, 1); Line(report, "보스 처치", report.bosses, 6); Line(report, "보스 공략 성공", report.interruptions, 1); Line(report, "탈출 성공", report.escaped ? 1 : 0, 12);
            Line(report, "첫 실험체 관측", report.newSubjects, 2); Line(report, "새 연구 과제 완료", report.newTasks, 3); Line(report, "첫 보스 연구", report.newBosses, 3); Line(report, "새 실험체 교차 연계", report.newSynergies, 3);
            report.earned = report.lines.Sum(x => x.amount); return report;
        }
        static bool ValidSynergy(string key)
        {
            var parts = (key ?? "").Split('|'); return parts.Length == 3 && CrossSubjectSynergies.Families.Contains(parts[0]) && parts[1] != parts[2] && SubjectExists(parts[1]) && SubjectExists(parts[2]);
        }
        static bool ValidInterruption(RunState state, string key)
        {
            var parts = (key ?? "").Split(':'); int nodeId, phase;
            if (parts.Length != 2 || !int.TryParse(parts[0], out nodeId) || !int.TryParse(parts[1], out phase) || phase < 1) return false;
            var node = Present(state.map).FirstOrDefault(n => n != null && n.id == nodeId && n.kind == ZoneKind.Boss);
            if (node == null) return false;
            var profile = BossMechanics.Profile(node.assignedOpponent);
            if (profile == null && nodeId == state.activeNodeId) profile = BossMechanics.Profile(state.combat?.enemyId);
            return phase <= (profile?.phases.Length ?? 0);
        }
        static void Line(ResearchSettlement report, string title, int count, int unit) => report.lines.Add(new ResearchRewardLine { title = title, count = count, unit = unit, amount = count * unit });
        static ResearchSettlement CopySettlement(ResearchSettlement value, bool alreadySettled)
        {
            return new ResearchSettlement { runId = value.runId, finishedAt = value.finishedAt, seed = value.seed, nodes = value.nodes, victories = value.victories, bosses = value.bosses, interruptions = value.interruptions, escapeBonus = value.escapeBonus, newSubjects = value.newSubjects, newTasks = value.newTasks, newBosses = value.newBosses, newSynergies = value.newSynergies, earned = value.earned, escaped = value.escaped, alreadySettled = alreadySettled, legacy = value.legacy,
                lines = Present(value.lines).Where(x => x != null).Select(x => new ResearchRewardLine { title = x.title, count = x.count, unit = x.unit, amount = x.amount }).ToList(), completedTasks = Present(value.completedTasks).ToList(), observedSubjects = Present(value.observedSubjects).ToList(), synergyKeys = Present(value.synergyKeys).ToList() };
        }
        public static ResearchSettlement Settle(ResearchProfile profile, RunState state)
        {
            if (profile == null || state?.research == null) return CalculateReport(profile, state);
            var run = state.research; var report = CalculateReport(profile, state);
            bool terminal = state.stage == RunStage.Won || state.stage == RunStage.Lost;
            if (!terminal || !run.begun || run.legacyEnded) return report;
            if (report.alreadySettled)
            {
                run.settled = true; run.settlement = CopySettlement(report, true); return report;
            }
            Normalize(profile);
            foreach (var character in GameDatabase.Characters)
            {
                var old = profile.subjects.FirstOrDefault(x => x.subjectId == character.id);
                var merged = Combined(old, run, character.id);
                if (!merged.observed && !merged.uniqueCompleted && merged.usedSkills.Count == 0) continue;
                if (old != null) profile.subjects.Remove(old);
                profile.subjects.Add(merged);
            }
            foreach (var boss in run.defeatedBossSubjects.Where(SubjectExists)) Add(profile.defeatedBosses, boss);
            foreach (var key in report.synergyKeys) Add(profile.synergyKeys, key);
            profile.data += report.earned; profile.totalData += report.earned; profile.totalRuns++; if (report.escaped) profile.totalWins++;
            Add(profile.settledRunIds, run.runId); report.finishedAt = Stamp();
            profile.settlements.Add(CopySettlement(report, false)); profile.history.Insert(0, CopySettlement(report, false)); if (profile.history.Count > 20) profile.history.RemoveRange(20, profile.history.Count - 20);
            run.settled = true; run.settlement = CopySettlement(report, false); return report;
        }
    }
}
