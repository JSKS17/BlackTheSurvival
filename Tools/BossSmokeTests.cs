#if LUMIA_TEST_RUNNER
using System;
using System.Collections.Generic;
using System.Linq;
using Lumia;

public static partial class CoreSmokeTests
{
    // Enter a real saved map node before simplifying unrelated traits for numeric tests.
    private static GameEngine BossFixture(string id = "nia", int turn = 1)
    {
        var e = Ready(1801); e.State.passives.Clear(); e.State.mainRune = e.State.supportRune = null;
        e.State.gear.Clear(); e.State.actBosses[0] = id; e.State.hp = e.State.maxHp = 999;
        e.State.row = 10; Enter(e, ZoneKind.Boss); var c = e.State.combat;
        c.enemyHp = c.enemyMaxHp = 999; c.enemyBlock = c.enemyStrength = c.enemyEvasion = c.enemyWeak = c.enemyVulnerable = 0;
        c.enemyTraits = new TraitActorState(); c.playerTraits = new TraitActorState();
        e.State.persistentTraits = c.playerTraits;
        c.enemySkills = new SkillActorState(); c.playerSkills = new SkillActorState();
        c.enemyStatuses = new StatusActorState(); c.playerStatuses = new StatusActorState();
        c.enemyOpeningEnergy = 0; c.enemyGear.Clear();
        c.enemyDeck.Clear(); c.enemyDrawPile.Clear(); c.enemyDiscardPile.Clear(); c.enemyExhaustPile.Clear();
        c.enemyHand.Clear(); c.enemyPlan.Clear(); c.enemyPlanCosts.Clear(); c.enemyPlanFreeCast.Clear();
        c.hand.Clear(); c.drawPile.Clear(); c.discardPile.Clear(); c.exhaustPile.Clear();
        c.energy = 99; c.block = c.strength = c.evasion = c.evasionTurns = 0; c.turn = turn;
        c.boss = new BossBattleState(); BossMechanics.BeginPlayerTurn(c); e.CombatActions.Clear();
        return e;
    }
    private static bool BossPlay(GameEngine e, string id)
    {
        e.State.combat.hand.Add(id); e.State.combat.energy = 99;
        return e.PlayCard(e.State.combat.hand.Count - 1);
    }
    private static void BossFinishEnemy(GameEngine e)
    {
        int guard = 0;
        while (e.State.stage == RunStage.Combat && e.State.combat.enemyTurn && guard++ < 80)
            Check(e.AdvanceEnemyAction(), "paced boss enemy action progresses");
        Check(guard < 80, "boss paced turn is finite");
    }
    private static void BossProfileCoverage()
    {
        var ids = new HashSet<string>(); int phaseCount = 0;
        var goals = new HashSet<BossGoal>(); var effects = new HashSet<BossEffect>();
        foreach (var character in GameDatabase.Characters)
        {
            var p = BossProfiles.Get(character.id); Check(p != null && p.subjectId == character.id && ids.Add(p.subjectId), "unique authored boss: " + character.id);
            Check(!string.IsNullOrEmpty(p.title) && !string.IsNullOrEmpty(p.overview), "boss has a character-specific premise: " + character.id);
            Check(p.phases.Length == (character.id == "nia" ? 3 : 2), "all bosses have phases, Nia has three: " + character.id);
            Check(p.phases.Select(x => x.name).Distinct().Count() == p.phases.Length, "phase names change: " + character.id);
            foreach (var phase in p.phases)
            {
                phaseCount++; goals.Add(phase.goal); effects.Add(phase.effect); effects.Add(phase.secondaryEffect);
                Check(!string.IsNullOrEmpty(phase.tell) && !string.IsNullOrEmpty(phase.response), "phase explains telegraph and response: " + character.id);
                Check(character.cards.Contains(phase.signatureCardId), "signature is the boss's actual QWER: " + phase.signatureCardId);
                Check(phase.goalAmount > 0 && phase.interval >= 2 && phase.interval <= 3, "finite attainable goal and telegraph interval: " + character.id);
                Check(phase.fallbackAmount > 0 && (phase.fallbackGoal == BossGoal.Damage || phase.fallbackGoal == BossGoal.Block), "every deck can use damage or block fallback: " + character.id);
                Check(phase.effect != BossEffect.None && phase.effectAmount > 0, "phase has an actual failure effect: " + character.id);
                if (phase.effect == BossEffect.Status || phase.secondaryEffect == BossEffect.Status)
                    Check(BossMechanics.StatusKeys.Contains(phase.statusKey), "boss status preserves an existing soft-control identity: " + character.id);
                if (phase.effect == BossEffect.ResourcePrepare || phase.secondaryEffect == BossEffect.ResourcePrepare)
                {
                    Check(!string.IsNullOrEmpty(phase.resourceKey) && !string.IsNullOrEmpty(phase.resourceLabel) && phase.resourceCap > 0, "resource preparation names a bounded skill resource: " + character.id);
                    Check(character.cards.Select(GameDatabase.Card).Where(x => x.mechanics != null).SelectMany(x => x.mechanics.rules).Any(x => x.key == phase.resourceKey && new[] { "gain", "set", "state_on", "state_toggle" }.Contains(x.op)), "prepared resource is really produced by this subject's skills: " + character.id + "/" + phase.resourceKey);
                }
                string text = BossMechanics.EffectsText(phase);
                Check(!string.IsNullOrEmpty(text) && !text.Contains("null"), "failure preview resolves names and numbers: " + character.id);
            }
        }
        Check(ids.Count == 91 && phaseCount == 183, "complete 91-subject, 183-phase coverage");
        Check(goals.Count == 7 && effects.Where(x => x != BossEffect.None).Distinct().Count() == 11, "boss profiles use all seven responses and eleven special effect types");
        Check(BossProfiles.Get("missing") == null, "unknown subject has no invented profile");
    }
    private static void BossEntryAndReadOnlyGuides()
    {
        foreach (var character in GameDatabase.Characters)
        {
            var e = BossFixture(character.id); var c = e.State.combat;
            Check(e.IsBossCombat && c.boss.phase == 1 && !c.boss.challengeActive && !BossMechanics.NeedsSpecialAction(c), "actual boss entry starts with one safe full player turn: " + character.id);
            string initialIntent = e.EnemyIntent;
            string before = FieldSnapshot(e.State); int rng = e.State.rngState;
            for (int i = 0; i < 3; ++i)
            {
                var guide = e.BossGuideFor(character.id); var view = e.CurrentBossView; string intent = e.EnemyIntent;
                Check(guide != null && view != null && guide.title.Contains(character.name) && !string.IsNullOrEmpty(guide.counterplay), "guide and live phase are readable: " + character.id);
            }
            Check(FieldSnapshot(e.State) == before && e.State.rngState == rng, "boss UI getters do not mutate state or RNG: " + character.id);
        }
        var ordinary = MechanicsFixture(null, "nia");
        Check(!ordinary.IsBossCombat && ordinary.CurrentBossView == null && ordinary.State.combat.boss == null, "ordinary subject and wildlife do not acquire boss rules");
    }
    private static void BossChallengeGoals()
    {
        var profile = BossProfiles.Get("nia"); var saved = profile.phases[0];
        try
        {
            foreach (BossGoal goal in Enum.GetValues(typeof(BossGoal)))
            {
                profile.phases[0] = new BossPhaseDef { name = "goal fixture", tell = "test", response = "test", signatureCardId = "nia_q", goal = goal, goalAmount = goal == BossGoal.Damage ? 10 : goal == BossGoal.Block ? 10 : 2, fallbackGoal = BossGoal.Damage, fallbackAmount = 9999, effect = BossEffect.Strike, effectAmount = 10 };
                var e = BossFixture("nia", 2); var c = e.State.combat;
                string first = goal == BossGoal.Block ? "basic_guard" : goal == BossGoal.Skill ? "nia_q" : goal == BossGoal.Movement ? GameDatabase.Cards.First(x => x.movement && x.category == "skill").id : "basic_attack";
                Check(BossPlay(e, first), "first qualifying goal card: " + goal);
                if (goal == BossGoal.MixedKeys)
                {
                    Check(BossPlay(e, first) && c.boss.progress == 1 && !c.boss.interrupted, "same card key does not inflate distinct-key challenge");
                    Check(BossPlay(e, "basic_guard"), "second distinct key");
                }
                else Check(BossPlay(e, first), "second qualifying goal card: " + goal);
                Check(c.boss.interrupted && c.boss.rewardUntilTurn == 3 && BossMechanics.RewardVulnerable(c), "response interrupts and rewards exactly once: " + goal);
                int reward = c.boss.rewardUntilTurn; Check(BossPlay(e, "basic_guard") && c.boss.rewardUntilTurn == reward, "extra cards cannot farm or extend reward");
            }
            profile.phases[0] = new BossPhaseDef { name = "fallback fixture", tell = "test", response = "test", signatureCardId = "nia_q", goal = BossGoal.Movement, goalAmount = 99, fallbackGoal = BossGoal.Block, fallbackAmount = 5, effect = BossEffect.Strike, effectAmount = 10 };
            var fallback = BossFixture("nia", 2); Check(BossPlay(fallback, "basic_guard") && fallback.State.combat.boss.interrupted, "block fallback works without any movement skill");
            Check(fallback.EndTurn() && fallback.State.combat.turn == 3 && BossMechanics.RewardVulnerable(fallback.State.combat), "late defensive interruption remains useful for the next player turn");
            Check(fallback.EndTurn() && !BossMechanics.RewardVulnerable(fallback.State.combat), "interruption reward expires after one full next player turn");

            profile.phases[0].goal = BossGoal.Damage; profile.phases[0].goalAmount = 6; profile.phases[0].fallbackAmount = 9999;
            var blocked = BossFixture("nia", 2); blocked.State.combat.enemyBlock = 30;
            Check(BossPlay(blocked, "basic_attack") && blocked.State.combat.enemyHp == 999 && blocked.State.combat.boss.interrupted, "damage absorbed by boss shield counts as offensive counterplay");
            var missed = BossFixture("nia", 2); missed.State.combat.enemyEvasion = 100;
            Check(BossPlay(missed, "basic_attack") && missed.State.combat.boss.progress == 0 && !missed.State.combat.boss.interrupted, "dodged attacks do not count as damage");
        }
        finally { profile.phases[0] = saved; }
    }
    private static void BossPhaseTransitions()
    {
        var e = BossFixture(); var c = e.State.combat;
        c.enemyHp = 501; c.enemyMaxHp = 1000;
        Check(BossPlay(e, "basic_attack") && c.boss.phase == 1 && c.boss.pendingPhase == 2, "real hit queues 50-percent phase without an immediate extra turn");
        int health = c.enemyHp; Check(e.CurrentBossView.pendingPhase == 2, "pending phase is exposed immediately");
        Check(e.EndTurn() && c.boss.phase == 2 && c.boss.pendingPhase == 0 && c.enemyHp == health && c.turn == 2, "phase applies next player turn without healing or bonus action");
        c.enemyHp = 900; Check(e.EndTurn() && c.boss.phase == 2, "boss healing cannot regress a phase");
        c.enemyHp = 251; Check(BossPlay(e, "basic_attack") && c.boss.pendingPhase == 3 && c.boss.phase == 2, "Nia queues final game-world phase at 25 percent");
        Check(e.EndTurn() && c.boss.phase == 3 && c.turn == 4, "third phase still gives a complete response turn");
        var other = BossFixture("aya"); other.State.combat.enemyHp = 100;
        Check(other.EndTurn() && other.State.combat.boss.phase == 2 && other.State.combat.boss.pendingPhase == 0, "other subjects stop at their authored second phase");
        var lethal = BossFixture(); lethal.State.combat.enemyHp = 1;
        Check(BossPlay(lethal, "basic_attack") && lethal.State.stage == RunStage.Rewards, "phase thresholds never grant an invulnerability gate against lethal damage");
    }
    private static void BossFailureEffects()
    {
        var profile = BossProfiles.Get("nia"); var saved = profile.phases[0];
        try
        {
            foreach (BossEffect effect in Enum.GetValues(typeof(BossEffect)))
            {
                if (effect == BossEffect.None) continue;
                profile.phases[0] = new BossPhaseDef { name = "failure fixture", tell = "test", response = "test", signatureCardId = "nia_q", goal = BossGoal.Block, goalAmount = 9999, fallbackGoal = BossGoal.Damage, fallbackAmount = 9999, effect = effect, effectAmount = 999, resourceKey = "blocks", resourceLabel = "아케이드 블록", resourceCap = 3, statusKey = "blind" };
                var e = BossFixture("nia", 2); var c = e.State.combat; c.enemyHp = 990;
                if (effect == BossEffect.Cleanse) { c.enemyPoison = c.enemyWeak = c.enemyVulnerable = 2; StatusMechanics.Add(c.enemyStatuses, "root", "nia_q", 2); }
                int hp = e.State.hp, energyMax = e.MaxEnergy;
                Check(e.BeginEndTurn() && e.AdvanceEnemyAction() && c.boss.specialResolved && c.enemyActionIndex == 0, "failed special is one separate paced action: " + effect);
                Check(e.CombatActions.Count(x => x.enemy && (x.kind ?? "").StartsWith("boss_")) == 1, "failure emits one source-backed effect for animation: " + effect);
                switch (effect)
                {
                    case BossEffect.Strike: Check(hp - e.State.hp == 20, "direct strike is capped at twenty before modifiers"); break;
                    case BossEffect.Shield: Check(c.enemyBlock == 18, "special shield is capped at eighteen"); break;
                    case BossEffect.Heal: Check(c.enemyHp == 999, "special heal cannot exceed maximum health"); break;
                    case BossEffect.DelayedStrike: Check(e.State.hp == hp && c.boss.delayedDamage == 16 && c.boss.delayedDueTurn == 3, "delayed damage grants a full additional response turn"); break;
                    case BossEffect.Prepare: Check(c.boss.preparedEnergy == 1, "prepared enemy energy is capped at one"); break;
                    case BossEffect.Status: Check(StatusMechanics.Has(c.playerStatuses, "blind"), "soft debuff is actually present"); break;
                    case BossEffect.Counter: Check(c.boss.counterDamage == 6 && c.boss.counterUntilTurn == 3, "counter is bounded and reserved for the next player turn"); break;
                    case BossEffect.Drain: Check(c.boss.pendingDrain == 1, "drain affects only next turn"); break;
                    case BossEffect.Evasion: Check(c.enemyEvasion > 0 && c.enemyEvasion <= 65 && c.enemyEvasionTurns == 1, "evasion remains bounded and expires after next player turn"); break;
                    case BossEffect.ResourcePrepare: Check(c.boss.preparedResources.Count == 1 && SkillMechanics.Resource(c.enemySkills, "니아", "blocks") == 0, "resource is queued, never secretly added after planning"); break;
                    case BossEffect.Cleanse: Check(c.enemyPoison == 0 && c.enemyWeak == 0 && c.enemyVulnerable == 0 && !StatusMechanics.Has(c.enemyStatuses, "root"), "cleanse removes the promised harmful conditions"); break;
                }
                BossFinishEnemy(e); Check(c.turn == 3 && e.MaxEnergy == energyMax, "special effects never permanently change max energy");
                int after = e.State.hp;
                if (effect == BossEffect.Counter)
                {
                    Check(BossPlay(e, "basic_attack") && after - e.State.hp == 6 && c.boss.counterDamage == 0, "next attack triggers the counter once");
                    after = e.State.hp; Check(BossPlay(e, "basic_attack") && e.State.hp == after, "second attack cannot replay the counter");
                }
                if (effect == BossEffect.Drain) Check(c.energy == Math.Max(1, energyMax - 1), "temporary drain leaves at least one usable cost");
                if (effect == BossEffect.Prepare) Check(c.enemyOpeningEnergy == 1 && c.boss.preparedEnergy == 0, "prepared energy transfers once before the next enemy plan");
                if (effect == BossEffect.ResourcePrepare) Check(SkillMechanics.Resource(c.enemySkills, "니아", "blocks") == 2 && c.boss.preparedResources.Count == 0, "real skill resource applies once before next planning");
                if (effect == BossEffect.Evasion)
                {
                    Check(c.enemyEvasionTurns == 1, "evasion is still active during the response turn");
                    Check(e.EndTurn() && c.enemyEvasionTurns == 0, "evasion expires when that player turn ends");
                }
                if (effect == BossEffect.Status) Check(StatusMechanics.Has(c.playerStatuses, "blind") && e.EndTurn() && !StatusMechanics.Has(c.playerStatuses, "blind"), "debuff lasts the next player turn and then expires");
                if (effect == BossEffect.DelayedStrike) Check(e.EndTurn() && after - e.State.hp == 16 && c.boss.delayedDamage == 0, "queued explosion occurs only after a full response turn");
            }
            profile.phases[0].effect = BossEffect.Shield; profile.phases[0].effectAmount = 999; profile.phases[0].secondaryEffect = BossEffect.Heal; profile.phases[0].secondaryAmount = 999;
            var support = BossFixture("nia", 2); support.State.combat.enemyHp = 800; support.BeginEndTurn(); support.AdvanceEnemyAction();
            Check(support.State.combat.enemyBlock + support.State.combat.enemyHp - 800 == 24, "combined shield and healing share the twenty-four support cap");
            profile.phases[0].effect = profile.phases[0].secondaryEffect = BossEffect.Strike;
            var attacks = BossFixture("nia", 2); int before = attacks.State.hp; attacks.BeginEndTurn(); attacks.AdvanceEnemyAction();
            Check(before - attacks.State.hp == 20, "multiple immediate strikes share a single twenty-damage budget");
        }
        finally { profile.phases[0] = saved; }
        foreach (var character in GameDatabase.Characters)
        {
            var phases = BossProfiles.Get(character.id).phases;
            for (int index = 0; index < phases.Length; ++index)
            {
                var phase = phases[index]; if (phase.effect != BossEffect.ResourcePrepare && phase.secondaryEffect != BossEffect.ResourcePrepare) continue;
                var e = BossFixture(character.id, 2); var c = e.State.combat; c.boss.phase = index + 1; c.boss.challengeActive = true;
                var rules = character.cards.Select(GameDatabase.Card).SelectMany(x => x.mechanics.rules).Where(x => x.key == phase.resourceKey && new[] { "gain", "set", "state_on", "state_toggle" }.Contains(x.op)).ToArray();
                bool isState = SkillMechanics.IsState(character.name, phase.resourceKey) || rules.Any(x => x.isState);
                int cap = isState ? 1 : rules.Max(x => x.cap);
                c.enemySkills.resources.Add(new SkillResource { owner = character.name, key = phase.resourceKey, label = phase.resourceLabel, cap = cap, amount = cap, isState = isState });
                Check(e.BeginEndTurn() && e.AdvanceEnemyAction() && c.boss.preparedResources.Count > 0, "every authored resource special really queues a skill resource: " + character.id);
                BossFinishEnemy(e);
                var resource = c.enemySkills.resources.First(x => x.owner == character.name && x.key == phase.resourceKey);
                Check(resource.amount == cap && resource.cap == cap && resource.isState == isState, "resource preparation preserves an existing full source cap and binary state: " + character.id + "/" + phase.resourceKey);
            }
        }
    }
    private static void BossIntentAndSignature()
    {
        var planner = typeof(GameEngine).GetMethod("PlanEnemyTurn", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        foreach (var character in GameDatabase.Characters)
        {
            var e = BossFixture(character.id, 2); var c = e.State.combat;
            c.enemyPlan.AddRange(new[] { "basic_attack", "basic_guard", "basic_attack" });
            c.enemyPlanCosts.AddRange(new[] { 1, 1, 1 }); c.enemyPlanFreeCast.AddRange(new[] { false, false, false });
            string intent = e.EnemyIntent; int expected = c.intentDamage, rng = e.State.rngState;
            string before = FieldSnapshot(e.State);
            for (int i = 0; i < 5; ++i) { string again = e.EnemyIntent; var live = e.CurrentBossView; }
            Check(FieldSnapshot(e.State) == before && e.State.rngState == rng, "repeated failure forecast is deterministic and read only: " + character.id);
            int hp = e.State.hp; Check(e.EndTurn(), "resolve special and ordinary card forecast: " + character.id);
            Check(hp - e.State.hp == expected, "boss special plus ordinary-card damage matches raw no-dodge forecast: " + character.id);

            e = BossFixture(character.id, 2); c = e.State.combat;
            c.enemyDeck.AddRange(character.cards); c.enemyDeck.AddRange(new[] { "basic_attack", "basic_guard" }); c.enemyDrawPile.AddRange(c.enemyDeck);
            planner.Invoke(e, null); string signature = BossProfiles.Get(character.id).phases[0].signatureCardId;
            if (GameDatabase.Card(signature).cost <= c.enemyEnergy)
                Check(c.enemyPlan.Count > 0 && c.enemyPlan[0] == signature, "available signature is prioritized inside ordinary energy budget: " + character.id);
            Check(c.enemyPlan.Count <= 40 && c.enemyPlanCosts.Count == c.enemyPlan.Count && c.enemyPlanFreeCast.Count == c.enemyPlan.Count, "boss plan remains finite and tracks each cost: " + character.id);
            Check(c.enemyPlanCosts.Sum() <= c.enemyEnergy + c.enemyPlan.Select(GameDatabase.Card).Sum(x => x.energy), "signature cannot create an unbudgeted extra card: " + character.id);
        }
        var profile = BossProfiles.Get("nia"); var saved = profile.phases[0];
        try
        {
            profile.phases[0] = new BossPhaseDef { name = "incoming fixture", tell = "test", response = "test", signatureCardId = "nia_q", goal = BossGoal.Block, goalAmount = 999, fallbackGoal = BossGoal.Damage, fallbackAmount = 999, effect = BossEffect.Strike, effectAmount = 20 };
            var aya = BossFixture("nia", 2); aya.State.passives.Add("aya_p"); int hp = aya.State.hp;
            string intent = aya.EnemyIntent;
            Check(aya.State.combat.intentDamage == 20 && aya.EndTurn() && hp - aya.State.hp == 15, "Aya incoming protection absorbs five from a special strike and forecast remains raw damage");
            Check(aya.CombatActions.Any(x => x.traitId == "aya_p" && x.block == 5), "special protection emits its normal passive animation");
            var vigilance = BossFixture("nia", 2); vigilance.State.supportRune = "vigilance"; vigilance.State.hp = 700; hp = vigilance.State.hp; intent = vigilance.EnemyIntent;
            Check(vigilance.EndTurn() && hp - vigilance.State.hp == 18 && vigilance.CombatActions.Any(x => x.traitId == "vigilance" && x.block == 2), "vigilance's incoming shield also applies to special strikes");
            var guardian = BossFixture("nia", 2); var c = guardian.State.combat; guardian.State.mainRune = "guardian"; c.block = 1;
            StatusMechanics.Add(c.playerStatuses, "armor_break", "nia_q", 2);
            c.enemyPlan.Add("basic_attack"); c.enemyPlanCosts.Add(1); c.enemyPlanFreeCast.Add(false);
            intent = guardian.EnemyIntent; int expected = c.intentDamage; hp = guardian.State.hp;
            Check(guardian.EndTurn() && hp - guardian.State.hp + 1 == expected && !StatusMechanics.Has(c.playerStatuses, "armor_break"), "special shield-break cleanse protects the following ordinary card in both forecast and actual turn");
            profile.phases[0].effect = BossEffect.Counter; profile.phases[0].effectAmount = 6;
            var counter = BossFixture("nia", 2); counter.State.passives.Add("aya_p"); Check(counter.EndTurn(), "reserve counter with defensive passive"); hp = counter.State.hp;
            Check(BossPlay(counter, "basic_attack") && hp - counter.State.hp == 1, "incoming protection applies equally to the once-only boss counter");
        }
        finally { profile.phases[0] = saved; }
    }
    private static void BossPersistenceAndMigration()
    {
        var p = BossProfiles.Get("nia"); var saved = p.phases[0];
        try
        {
            p.phases[0] = new BossPhaseDef { name = "save fixture", tell = "test", response = "test", signatureCardId = "nia_q", goal = BossGoal.MixedKeys, goalAmount = 3, fallbackGoal = BossGoal.Damage, fallbackAmount = 999, effect = BossEffect.DelayedStrike, effectAmount = 10, secondaryEffect = BossEffect.ResourcePrepare, secondaryAmount = 2, resourceKey = "blocks", resourceLabel = "아케이드 블록", resourceCap = 3 };
            var e = BossFixture("nia", 2); Check(BossPlay(e, "basic_attack"), "save fixture makes genuine partial progress");
            string initial = e.EnemyIntent; var loaded = new GameEngine((RunState)Clone(e.State));
            Check(loaded.State.combat.boss.progress == 1 && loaded.State.combat.boss.playedKeys.SequenceEqual(new[] { "ATK" }) && loaded.State.rngState == e.State.rngState, "partial mixed-key progress and RNG survive loading");
            Check(e.BeginEndTurn() && loaded.BeginEndTurn() && e.AdvanceEnemyAction() && loaded.AdvanceEnemyAction(), "both saves resolve the same first paced special");
            Check(FieldSnapshot(e.State) == FieldSnapshot(loaded.State), "first paced special is deterministic across a save");
            loaded = new GameEngine((RunState)Clone(e.State));
            Check(loaded.State.combat.boss.specialResolved && loaded.State.combat.boss.delayedDamage == 10 && loaded.State.combat.boss.preparedResources.Count == 1, "resolved action, queued explosion and prepared resource are persisted");
            BossFinishEnemy(e); BossFinishEnemy(loaded);
            Check(FieldSnapshot(e.State) == FieldSnapshot(loaded.State) && SkillMechanics.Resource(loaded.State.combat.enemySkills, "니아", "blocks") == 2, "resuming does not repeat the special or resource grant");
            int hp = loaded.State.hp; var again = new GameEngine((RunState)Clone(loaded.State));
            Check(loaded.EndTurn() && again.EndTurn() && hp - again.State.hp == 10 && FieldSnapshot(loaded.State) == FieldSnapshot(again.State), "scheduled explosion resolves once after reload at its promised turn");

            var phase = BossFixture(); phase.State.combat.enemyHp = 501; phase.State.combat.enemyMaxHp = 1000; BossPlay(phase, "basic_attack");
            var phaseLoaded = new GameEngine((RunState)Clone(phase.State));
            Check(phaseLoaded.State.combat.boss.pendingPhase == 2 && phaseLoaded.State.combat.boss.phase == 1 && phaseLoaded.EndTurn() && phaseLoaded.State.combat.boss.phase == 2, "queued phase persists and is applied at the correct future turn");
            var old = BossFixture("aya", 5); old.State.combat.boss = null; int oldHp = old.State.hp, oldEnemyHp = old.State.combat.enemyHp, oldRng = old.State.rngState;
            var migrated = new GameEngine((RunState)Clone(old.State));
            Check(migrated.IsBossCombat && !migrated.State.combat.boss.challengeActive && migrated.State.combat.boss.nextMechanicTurn >= 6, "legacy boss combat gets a future telegraph instead of retroactive failure");
            Check(migrated.State.hp == oldHp && migrated.State.combat.enemyHp == oldEnemyHp && migrated.State.rngState == oldRng, "migration preserves health and gameplay RNG");
            old.State.combat.enemyTurn = true; migrated = new GameEngine((RunState)Clone(old.State));
            Check(migrated.State.combat.boss.specialResolved && !BossMechanics.NeedsSpecialAction(migrated.State.combat), "legacy save in an enemy turn cannot receive an untelegraphed special");
        }
        finally { p.phases[0] = saved; }
    }
}
#endif
