#if LUMIA_TEST_RUNNER
using System;
using System.Collections.Generic;
using System.Linq;
using Lumia;

public static partial class CoreSmokeTests
{
    private static RunState ResearchRun(int seed = 61)
    {
        var state = new RunState { seed = seed, rngState = seed * 7919, stage = RunStage.Combat, activeNodeId = 0,
            map = new List<MapNode> { new MapNode { id = 0, kind = ZoneKind.Subject, visited = true } }, combat = new CombatState() };
        ResearchProgression.Begin(state); return state;
    }
    private static void ResearchResource(SkillActorState actor, CardDef card, string key, int value, int cap = 8)
    {
        if (string.IsNullOrEmpty(key)) return;
        var existing = actor.resources.FirstOrDefault(r => r.owner == card.owner && r.key == key);
        if (existing == null) { existing = new SkillResource { owner = card.owner, key = key, cap = cap }; actor.resources.Add(existing); }
        existing.cap = cap; existing.amount = Math.Min(value, cap);
    }
    private static SkillActorState ResearchRulePreparation(CardDef card, SkillRule rule)
    {
        var before = new SkillActorState();
        if (rule == null) return before;
        if (rule.op == "consume") ResearchResource(before, card, rule.key, Math.Max(1, rule.amount));
        if (rule.op == "state_off" || rule.op == "state_toggle") ResearchResource(before, card, rule.key, 1, 1);
        if (rule.op == "gain") ResearchResource(before, card, rule.key, Math.Max(0, rule.cap - rule.amount), rule.cap);
        if (!string.IsNullOrEmpty(rule.scaleKey)) ResearchResource(before, card, rule.scaleKey, 1);
        if (!string.IsNullOrEmpty(rule.conditionKey)) ResearchResource(before, card, rule.conditionKey, rule.conditionAmount);
        if (!string.IsNullOrEmpty(rule.conditionKey2)) ResearchResource(before, card, rule.conditionKey2, rule.conditionAmount2);
        if (!string.IsNullOrEmpty(rule.conditionPrevious))
        {
            var previous = GameDatabase.Card(rule.conditionPrevious) ?? GameDatabase.Cards.FirstOrDefault(c => c.owner == card.owner && c.key == rule.conditionPrevious);
            if (previous != null) before.history.Add(new SkillOwnerHistory { owner = card.owner, cardId = previous.id });
        }
        return before;
    }
    private static bool ResearchNativeWitness(string subjectId, CardDef card, SkillActorState before)
    {
        var state = ResearchRun(); var after = SkillMechanics.Clone(before);
        var bonus = SkillMechanics.Bonuses(before, card, true);
        SkillMechanics.AfterCard(after, before, card, true, paidCost: card.cost);
        state.combat.playerSkills = after;
        var own = new CombatAction { cardId = card.id, damage = card.damage + bonus.damage, block = card.block + bonus.block, heal = card.heal + bonus.heal };
        int rng = state.rngState, hp = state.hp, energy = state.combat.energy;
        string resources = SkillMechanics.Summary(after);
        ResearchProgression.AfterCard(state, card, before, true, own.damage, new[] { own });
        Check(state.rngState == rng && state.hp == hp && state.combat.energy == energy && SkillMechanics.Summary(after) == resources, "research witnesses do not change combat or gameplay RNG: " + subjectId);
        bool success = state.research.completedUniqueSubjects.Contains(subjectId);
        var enemyOnly = ResearchRun(); enemyOnly.combat.playerSkills = after;
        ResearchProgression.AfterCard(enemyOnly, card, before, true, own.damage, new[] { new CombatAction { cardId = card.id, enemy = true, damage = own.damage } });
        Check(enemyOnly.research.usedSkillIds.Count == 0 && enemyOnly.research.completedUniqueSubjects.Count == 0, "enemy-only action is never player research: " + subjectId);
        var preview = ResearchRun(); preview.combat.playerSkills = after;
        ResearchProgression.AfterCard(preview, card, before, true, own.damage, new CombatAction[0]);
        Check(preview.research.usedSkillIds.Count == 0 && preview.research.completedUniqueSubjects.Count == 0, "preview without an executed action is never research: " + subjectId);
        return success;
    }
    private static void ResearchCoverageAndExperiments()
    {
        var profile = new ResearchProfile(); int witnesses = 0;
        foreach (var character in GameDatabase.Characters)
        {
            var definition = ResearchProgression.ForSubject(character.id);
            var tasks = ResearchProgression.Tasks(profile, character.id);
            Check(definition != null && definition.subjectId == character.id && tasks.Length == 3 && tasks.Select(t => t.id).Distinct().Count() == 3, "every subject has a unique three-task report: " + character.id);
            Check(tasks.All(t => !string.IsNullOrEmpty(t.description) && t.progress == 0 && !t.completed) && tasks[1].target == 4, "new research has readable requirements and all four native skills: " + character.id);
            Check(!string.IsNullOrEmpty(definition.reportText) && !string.IsNullOrEmpty(definition.unlockText), "every report describes its research and encounter unlock: " + character.id);
            if (character.id == "isaac") { ++witnesses; continue; } // Engine integration below proves the actual third-basic passive.
            bool completed = false;
            foreach (var card in character.cards.Select(GameDatabase.Card).Where(c => c != null))
            {
                if (character.id == "nia" && card.id == "nia_w")
                {
                    var preparation = new SkillActorState(); ResearchResource(preparation, card, "blocks", 3, 3);
                    completed |= ResearchNativeWitness(character.id, card, preparation);
                }
                foreach (var rule in card.mechanics?.rules ?? new SkillRule[0])
                {
                    completed |= ResearchNativeWitness(character.id, card, ResearchRulePreparation(card, rule));
                    if (completed) break;
                }
                if (completed) break;
            }
            Check(completed, "the subject's native skill rules can actually satisfy the displayed unique task: " + character.id + " / " + tasks[2].description);
            ++witnesses;
        }
        Check(witnesses == GameDatabase.Characters.Count, "all subject experiments have an executable native witness");
        Check(ResearchProgression.ForSubject("missing") == null && ResearchProgression.Tasks(profile, "missing").Length == 0, "invalid subject is harmless");
        Check(!ResearchProgression.CompleteReport(profile, "nia") && profile.data == 0 && profile.completedReports.Count == 0, "unmet report never spends or unlocks");
        Console.WriteLine("RESEARCH NATIVE EXPERIMENT COVERAGE PASS / subjects=" + witnesses);
    }
    private static void ResearchRunSettlement()
    {
        var profile = new ResearchProfile(); var state = ResearchRun(); var r = state.research;
        int rng = state.rngState;
        ResearchProgression.ObserveSubject(state, "nia"); ResearchProgression.ObserveSubject(state, "nia"); ResearchProgression.ObserveSubject(state, "invalid");
        ResearchProgression.CompleteNode(state, 0); ResearchProgression.CompleteNode(state, 0); ResearchProgression.CompleteNode(state, 999);
        ResearchProgression.Victory(state, "nia", true); ResearchProgression.Victory(state, "nia", true);
        state.map[0].kind = ZoneKind.Boss; state.map[0].assignedOpponent = "nia"; state.combat.enemyId = "nia"; state.combat.boss = new BossBattleState { interrupted = true };
        ResearchProgression.BossInterrupted(state, 1); ResearchProgression.BossInterrupted(state, 1); ResearchProgression.BossInterrupted(state, 2); ResearchProgression.BossInterrupted(state, 3); ResearchProgression.BossInterrupted(state, 3); ResearchProgression.BossInterrupted(state, 99);
        r.usedSkillIds.AddRange(GameDatabase.Character("nia").cards); r.completedUniqueSubjects.Add("nia"); r.synergyKeys.Add("bomb|isol|celine"); r.synergyKeys.Add("bomb|isol|isol");
        Check(ResearchProgression.CalculateReport(profile, state).earned == 0 && profile.totalRuns == 0 && !r.settled, "ongoing runs cannot claim research");
        state.stage = RunStage.Lost;
        var before = (ResearchProfile)Clone(profile); var report = ResearchProgression.CalculateReport(profile, state);
        Check(report.nodes == 1 && report.victories == 1 && report.bosses == 1 && report.interruptions == 3 && report.newSubjects == 1 && report.newTasks == 3 && report.newBosses == 1 && report.newSynergies == 1 && report.earned == 28 && report.lines.Count == 9, "terminal research includes all three Nia phases once and exact deduplicated exploration, boss, task and cross-family discoveries");
        Check(profile.data == before.data && profile.subjects.Count == 0 && !r.settled && r.settlement == null && state.rngState == rng, "settlement preview is read-only");
        var savedBeforeSettlement = (RunState)Clone(state);
        var committed = ResearchProgression.Settle(profile, state);
        Check(profile.data == 28 && profile.totalData == 28 && profile.totalRuns == 1 && profile.totalWins == 0 && r.settled && r.settlement.earned == 28, "failed run preserves all earned research exactly once");
        Check(profile.subjects.Single().observed && profile.subjects.Single().uniqueCompleted && profile.subjects.Single().usedSkills.Count == 4 && ResearchProgression.Tasks(profile, "nia").All(t => t.completed), "terminal merge includes all three completed tasks");
        var duplicate = ResearchProgression.Settle(profile, savedBeforeSettlement);
        Check(duplicate.alreadySettled && duplicate.earned == 28 && savedBeforeSettlement.research.settled && profile.data == 28 && profile.totalRuns == 1, "reloading the terminal pre-settlement save returns original receipt without crediting twice");
        savedBeforeSettlement.research.settlement.earned = 9999;
        var authority = ResearchProgression.CalculateReport(profile, savedBeforeSettlement);
        Check(authority.earned == 28, "persistent ledger is authoritative over a changed run receipt");
        Check(ResearchProgression.ReportCost == 500 && !ResearchProgression.CompleteReport(profile, "nia") && profile.data == 28, "completed tasks alone cannot pay the five hundred data report cost");
        profile.data += ResearchProgression.ReportCost; profile.totalData += ResearchProgression.ReportCost;
        Check(ResearchProgression.CompleteReport(profile, "nia") && profile.data == 28 && profile.totalData == ResearchProgression.ReportCost + 28 && profile.completedReports.SequenceEqual(new[] { "nia" }), "report completion spends exactly five hundred and unlocks only its subject");
        Check(!ResearchProgression.CompleteReport(profile, "nia") && profile.data == 28, "completed reports cannot spend twice");
        var sameSeed = ResearchRun(state.seed); sameSeed.stage = RunStage.Won; ResearchProgression.CompleteNode(sameSeed, 0); ResearchProgression.ObserveSubject(sameSeed, "nia");
        Check(sameSeed.research.runId != state.research.runId, "a new expedition with the same gameplay seed has a fresh settlement identity");
        var repeated = ResearchProgression.Settle(profile, sameSeed);
        Check(repeated.earned == 13 && repeated.newSubjects == 0 && repeated.newTasks == 0 && profile.totalRuns == 2 && profile.totalWins == 1, "same-seed replay earns new exploration and escape but not permanent discovery bonuses again");
        var empty = ResearchRun(); empty.stage = RunStage.Lost;
        Check(ResearchProgression.Settle(profile, empty).earned == 0, "starting and immediately dying gives no fixed data reward");
        var preparation = new RunState(); ResearchProgression.EnsureRun(preparation, true); preparation.stage = RunStage.Lost;
        int beforeRuns = profile.totalRuns;
        Check(ResearchProgression.Settle(profile, preparation).earned == 0 && profile.totalRuns == beforeRuns, "unstarted preparation is not an expedition settlement");
        var legacyTerminal = new RunState { stage = RunStage.Won, seed = 111, defeatedBosses = new List<string> { "nia" } };
        ResearchProgression.EnsureRun(legacyTerminal, true);
        Check(legacyTerminal.research.legacyEnded && legacyTerminal.research.settled && ResearchProgression.Settle(profile, legacyTerminal).earned == 0 && profile.totalRuns == beforeRuns, "already-finished pre-research saves do not retroactively pay a run");
        var legacyActive = new RunState { stage = RunStage.Map, seed = 9, map = new List<MapNode> { new MapNode { id = 0, visited = true } } };
        ResearchProgression.EnsureRun(legacyActive, true); legacyActive.stage = RunStage.Lost;
        Check(legacyActive.research.begun && ResearchProgression.Settle(profile, legacyActive).earned == 0, "active legacy run records only observations after migration");
        var emptyJournalTerminal = new RunState { stage = RunStage.Won, seed = 19, research = new ResearchRunState() };
        var emptyJournalProfile = new ResearchProfile(); ResearchProgression.EnsureRun(emptyJournalTerminal, true);
        Check(emptyJournalTerminal.research.legacyEnded && emptyJournalTerminal.research.settled && !string.IsNullOrEmpty(emptyJournalTerminal.research.runId)
            && ResearchProgression.Settle(emptyJournalProfile, emptyJournalTerminal).earned == 0 && emptyJournalProfile.totalRuns == 0, "an empty deserialized legacy journal is recognized as an already-finished old run");
        var emptyJournalActive = new RunState { stage = RunStage.Map, seed = 21, research = new ResearchRunState(), map = new List<MapNode> { new MapNode { id = 0 } } };
        ResearchProgression.EnsureRun(emptyJournalActive, true); string migratedId = emptyJournalActive.research.runId;
        ResearchProgression.ObserveSubject(emptyJournalActive, "nia"); ResearchProgression.CompleteNode(emptyJournalActive, 0);
        ResearchProgression.EnsureRun(emptyJournalActive, true); emptyJournalActive.stage = RunStage.Lost;
        Check(emptyJournalActive.research.begun && !emptyJournalActive.research.legacyEnded && emptyJournalActive.research.runId == migratedId
            && ResearchProgression.Settle(emptyJournalProfile, emptyJournalActive).earned == 6 && emptyJournalProfile.totalRuns == 1,
            "an empty deserialized active legacy journal begins fresh research, preserves its identity on reload, and pays only subsequent real observations");
        var emptyJournalPreparation = new RunState { stage = RunStage.Preparation, research = new ResearchRunState() };
        ResearchProgression.EnsureRun(emptyJournalPreparation, true);
        Check(!emptyJournalPreparation.research.begun && !emptyJournalPreparation.research.legacyEnded, "an empty legacy preparation journal remains unstarted");
        // Completing distinct Q/W/E/R is cumulative, rather than requiring a lucky four-skill draft in one run.
        var cumulative = new ResearchProfile(); var one = ResearchRun(); one.research.usedSkillIds.AddRange(GameDatabase.Character("jackie").cards.Take(3)); one.stage = RunStage.Lost;
        ResearchProgression.Settle(cumulative, one);
        Check(ResearchProgression.Tasks(cumulative, "jackie")[1].progress == 3 && !ResearchProgression.Tasks(cumulative, "jackie")[1].completed, "partial skill research persists across expeditions");
        var two = ResearchRun(); two.research.usedSkillIds.Add(GameDatabase.Character("jackie").cards.Last()); two.stage = RunStage.Lost;
        Check(ResearchProgression.Settle(cumulative, two).newTasks == 1 && ResearchProgression.Tasks(cumulative, "jackie")[1].completed, "fourth distinct skill completes the cumulative task once");
        // History display retention must never reduce deduplication protection.
        for (int i = 0; i < 23; ++i) { var later = ResearchRun(i); later.stage = RunStage.Lost; ResearchProgression.Settle(profile, later); }
        Check(profile.history.Count == 20 && profile.settledRunIds.Count > 20 && profile.settlements.Count == profile.settledRunIds.Count, "permanent settlement ledger survives the twenty-row display limit");
        int beforeExpiredReplay = profile.data;
        Check(ResearchProgression.Settle(profile, (RunState)Clone(state)).earned == 28 && profile.data == beforeExpiredReplay && profile.data == 41, "old receipt remains recoverable and cannot credit again after history expiry");
        Check(state.rngState == rng, "settlement and report purchase never advance game RNG");
    }
    private static GameEngine ResearchCombat(int seed, string passive = null)
    {
        var engine = Ready(seed); engine.State.passives.Clear(); if (passive != null) engine.State.passives.Add(passive);
        Enter(engine, ZoneKind.Wildlife); engine.State.combat.enemyHp = engine.State.combat.enemyMaxHp = 5000; engine.State.combat.enemyEvasion = 0;
        return engine;
    }
    private static void ResearchPlay(GameEngine engine, string id)
    {
        engine.State.combat.hand.Add(id); engine.State.combat.energy = 99;
        Check(engine.PlayCard(engine.State.combat.hand.Count - 1), "research fixture executes actual card " + id);
    }
    private static void ResearchArmLethalCounter(GameEngine engine)
    {
        var combat = engine.State.combat;
        engine.State.hp = 1; engine.State.mainRune = engine.State.supportRune = null;
        engine.State.passives.Clear(); engine.State.gear.Clear(); combat.block = 0; combat.playerTraits = new TraitActorState();
        combat.enemySkills = new SkillActorState();
        var counterCard = GameDatabase.Card("hyunwoo_w"); var before = SkillMechanics.Clone(combat.enemySkills);
        SkillMechanics.AfterCard(combat.enemySkills, before, counterCard, true, recordHistory: false);
        Check(combat.enemySkills.effects.Any(effect => effect.kind == "counter" && effect.amount > 1), "lethal retaliation uses a real native counter effect");
    }
    private static void ResearchActualCombos()
    {
        var nia = ResearchCombat(803); ResearchPlay(nia, "nia_w");
        Check(!nia.State.research.completedUniqueSubjects.Contains("nia"), "Nia W without maximum blocks never completes her experiment");
        for (int i = 0; i < 2; ++i) ResearchPlay(nia, "nia_q"); ResearchPlay(nia, "nia_w");
        Check(!nia.State.research.completedUniqueSubjects.Contains("nia"), "Nia partial-block W is insufficient");
        for (int i = 0; i < 3; ++i) ResearchPlay(nia, "nia_q");
        ResearchPlay(nia, "nia_w");
        Check(nia.State.research.completedUniqueSubjects.Contains("nia"), "Nia maximum-block W qualifies through real native damage and resource recovery");
        var miss = ResearchRun(); var w = GameDatabase.Card("nia_w"); var full = new SkillActorState(); ResearchResource(full, w, "blocks", 3, 3);
        SkillMechanics.AfterCard(miss.combat.playerSkills, full, w, false);
        ResearchProgression.AfterCard(miss, w, full, false, 0, new[] { new CombatAction { cardId = w.id, avoided = 1 } });
        Check(!miss.research.completedUniqueSubjects.Contains("nia"), "a missed maximum-block W cannot complete Nia research");
        var isaac = ResearchCombat(805, "isaac_p"); isaac.State.hp = isaac.State.maxHp - 30;
        ResearchPlay(isaac, "basic_attack"); ResearchPlay(isaac, "basic_attack");
        Check(!isaac.State.research.completedUniqueSubjects.Contains("isaac"), "Isaac ordinary first and second basics do not count as the third-hit passive");
        int beforeHp = isaac.State.hp; ResearchPlay(isaac, "basic_attack");
        Check(isaac.State.hp > beforeHp && isaac.State.research.completedUniqueSubjects.Contains("isaac"), "Isaac third universal basic actually heals and completes his experiment");
        var fullHp = ResearchCombat(807, "isaac_p"); for (int i = 0; i < 3; ++i) ResearchPlay(fullHp, "basic_attack");
        Check(!fullHp.State.research.completedUniqueSubjects.Contains("isaac"), "a zero-heal full-health passive is not falsely reported as the required healing experiment");
        var bomb = ResearchCombat(809); ResearchPlay(bomb, "isol_q");
        Check(bomb.State.research.synergyKeys.Count == 0, "preparing a bomb alone is not a cross-subject activation");
        ResearchPlay(bomb, "celine_q");
        Check(bomb.State.research.synergyKeys.Contains("bomb|isol|celine"), "actual Isol-to-Celine bomb preparation consumption is recorded");
        var chargeBefore = new SkillActorState(); var consumer = GameDatabase.Card("celine_q");
        chargeBefore.synergy.charges.Add(new SynergyCharge { family = "bomb", owner = GameDatabase.Card("isol_q").owner, sourceCard = "isol_q" });
        chargeBefore.synergy.charges.Add(new SynergyCharge { family = "bomb", owner = GameDatabase.Card("rozzi_r").owner, sourceCard = "rozzi_r" });
        var chargeAfter = SkillMechanics.Clone(chargeBefore); SkillMechanics.AfterCard(chargeAfter, chargeBefore, consumer, true);
        var actual = ResearchRun(); actual.combat.playerSkills = chargeAfter;
        ResearchProgression.AfterCard(actual, consumer, chargeBefore, true, consumer.damage, new[] { new CombatAction { cardId = consumer.id, damage = consumer.damage } });
        Check(actual.research.synergyKeys.SequenceEqual(new[] { "bomb|isol|celine" }), "multiple foreign preparations only credit the producer whose charge was actually spent");
        var forecast = new GameEngine((RunState)Clone(bomb.State)); int observed = forecast.State.research.usedSkillIds.Count, connections = forecast.State.research.synergyKeys.Count;
        foreach (var id in GameDatabase.Character("nia").cards) { forecast.CardTotalDamage(id); forecast.CardDamage(id); }
        Check(forecast.State.research.usedSkillIds.Count == observed && forecast.State.research.synergyKeys.Count == connections, "engine damage previews do not progress player research");
        var reloaded = new GameEngine((RunState)Clone(nia.State));
        Check(reloaded.State.research.runId == nia.State.research.runId && reloaded.State.research.completedUniqueSubjects.Contains("nia"), "run research identity and actual experiment survive save reload");
        var fatalQ = ResearchCombat(811); ResearchArmLethalCounter(fatalQ); ResearchPlay(fatalQ, "nia_q");
        Check(fatalQ.State.stage == RunStage.Lost && fatalQ.State.research.usedSkillIds.Contains("nia_q")
            && fatalQ.CombatActions.Any(action => action.enemy && action.kind == "counter" && action.damage > 0), "the final actually-used Q is recorded before its lethal retaliation changes the run stage");
        var fatalW = ResearchCombat(813); for (int i = 0; i < 3; ++i) ResearchPlay(fatalW, "nia_q");
        Check(!fatalW.State.research.completedUniqueSubjects.Contains("nia") && !fatalW.State.research.usedSkillIds.Contains("nia_w"), "maximum-block experiment is still incomplete before the final W");
        ResearchArmLethalCounter(fatalW); ResearchPlay(fatalW, "nia_w");
        Check(fatalW.State.stage == RunStage.Lost && fatalW.State.research.usedSkillIds.Contains("nia_w") && fatalW.State.research.completedUniqueSubjects.Contains("nia")
            && fatalW.CombatActions.Any(action => action.enemy && action.kind == "counter" && action.damage > 0), "maximum-block W's actual experiment is preserved even when its retaliation immediately kills the player");
        var fatalProfile = new ResearchProfile(); ResearchProgression.Settle(fatalProfile, fatalW.State);
        Check(ResearchProgression.Tasks(fatalProfile, "nia")[2].completed && fatalProfile.totalRuns == 1 && fatalProfile.totalWins == 0, "lethal-retaliation experiment is included in the failed expedition's settlement");
        Console.WriteLine("RESEARCH ACTUAL COMBOS PASS / Nia=PASS / Isaac=PASS / bomb=PASS / previews=PASS / save=PASS / lethalRetaliation=PASS");
    }
}
#endif
