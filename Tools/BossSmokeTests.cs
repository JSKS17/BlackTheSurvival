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
        e.State.row = 10; e.AvailableNodes().First(n => n.lane == 1).assignedOpponent = id;
        Enter(e, ZoneKind.Boss); var c = e.State.combat;
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
                Check(BossMechanics.IsRestriction(phase.goal) ? phase.fallbackAmount == 0
                    : phase.fallbackAmount > 0 && (phase.fallbackGoal == BossGoal.Damage || phase.fallbackGoal == BossGoal.Block), "restrictions have no bypass; affirmative goals retain a damage or block alternative: " + character.id);
                Check(phase.effect != BossEffect.None && phase.effectAmount > 0, "phase has an actual failure effect: " + character.id);
                if (phase.effect == BossEffect.Status || phase.secondaryEffect == BossEffect.Status)
                    Check(BossMechanics.StatusKeys.Contains(phase.statusKey), "boss status preserves an existing soft-control identity: " + character.id);
                if (phase.effect == BossEffect.ResourcePrepare || phase.secondaryEffect == BossEffect.ResourcePrepare)
                {
                    Check(!string.IsNullOrEmpty(phase.resourceKey) && !string.IsNullOrEmpty(phase.resourceLabel) && phase.resourceCap > 0, "resource preparation names a bounded skill resource: " + character.id);
                    Check(character.cards.Select(GameDatabase.Card).Where(x => x.mechanics != null).SelectMany(x => x.mechanics.rules).Any(x => x.key == phase.resourceKey && new[] { "gain", "set", "state_on", "state_toggle", "forge_sword" }.Contains(x.op)), "prepared resource is really produced by this subject's skills: " + character.id + "/" + phase.resourceKey);
                }
                string text = BossMechanics.EffectsText(phase);
                Check(!string.IsNullOrEmpty(text) && !text.Contains("null"), "failure preview resolves names and numbers: " + character.id);
            }
        }
        Check(ids.Count == 91 && phaseCount == 183, "complete 91-subject, 183-phase coverage");
        Check(goals.Count == 11 && effects.Where(x => x != BossEffect.None).Distinct().Count() == 11, "boss profiles use eleven response conditions and eleven special effect types");
        Check(BossProfiles.All.Sum(p => p.phases.Count(phase => BossMechanics.IsRestriction(phase.goal) || phase.goal == BossGoal.DebuffSkill)) == 16,
            "sixteen authored phases introduce debuff use and three types of restriction");
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
            foreach (BossGoal goal in Enum.GetValues(typeof(BossGoal)).Cast<BossGoal>().Where(goal => !BossMechanics.IsRestriction(goal)))
            {
                profile.phases[0] = new BossPhaseDef { name = "goal fixture", tell = "test", response = "test", signatureCardId = "nia_q", goal = goal, goalAmount = goal == BossGoal.Damage ? 10 : goal == BossGoal.Block ? 10 : 2, fallbackGoal = BossGoal.Damage, fallbackAmount = 9999, effect = BossEffect.Strike, effectAmount = 10 };
                var e = BossFixture("nia", 2); var c = e.State.combat;
                string first = goal == BossGoal.Block ? "basic_guard" : goal == BossGoal.DebuffSkill ? "hyunwoo_q" : goal == BossGoal.Skill ? "nia_q" : goal == BossGoal.Movement ? GameDatabase.Cards.First(x => x.movement && x.category == "skill").id : "basic_attack";
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
                var rules = character.cards.Select(GameDatabase.Card).SelectMany(x => x.mechanics.rules).Where(x => x.key == phase.resourceKey && new[] { "gain", "set", "state_on", "state_toggle", "forge_sword" }.Contains(x.op)).ToArray();
                bool isState = SkillMechanics.IsState(character.name, phase.resourceKey) || rules.Any(x => x.isState);
                int cap = isState ? 1 : rules.Max(x => x.cap);
                c.enemySkills.resources.Add(new SkillResource { owner = character.name, key = phase.resourceKey, label = phase.resourceLabel, cap = cap, amount = cap, isState = isState });
                Check(e.BeginEndTurn() && e.AdvanceEnemyAction() && c.boss.preparedResources.Count > 0, "every authored resource special really queues a skill resource: " + character.id);
                var pending = c.boss.preparedResources.Single(x => x.key == phase.resourceKey);
                int pendingAmount = pending.amount;
                Check(pending.cap == cap && pending.isState == isState && pending.amount <= cap, "queued resource preserves its actual producer cap and state: " + character.id + "/" + phase.resourceKey);
                // Isolate preparation from the intervening passive turn-end effects.
                // Shared blood can legitimately be spent before the queued refill.
                var preparationOnly = new GameEngine((RunState)Clone(e.State));
                typeof(GameEngine).GetMethod("PrepareBossNextTurn", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(preparationOnly, null);
                var prepared = preparationOnly.State.combat.enemySkills.resources.Single(x => x.owner == character.name && x.key == phase.resourceKey);
                Check(prepared.amount == cap && prepared.cap == cap && prepared.isState == isState && preparationOnly.State.combat.boss.preparedResources.Count == 0,
                    "preparation alone preserves an existing full source cap and binary state: " + character.id + "/" + phase.resourceKey);
                BossFinishEnemy(e);
                var resource = c.enemySkills.resources.First(x => x.owner == character.name && x.key == phase.resourceKey);
                if (character.id == "bianca" && phase.resourceKey == "blood")
                {
                    int spendIndex = Array.FindIndex(GameDatabase.Passive("bianca_p").mechanics.rules, rule => rule.trigger == "turn_end" && rule.op == "consume" && rule.key == "blood");
                    Check(pendingAmount == 1 && c.enemyTraits.counters.Any(x => x.sourceId == "bianca_p" && x.ruleIndex == spendIndex && x.battleUses == 1),
                        "Bianca actually consumes the shared blood at turn end before the authored one-blood refill");
                    Check(resource.amount == 1 && resource.cap == 4 && !resource.isState,
                        "Bianca's real cycle spends four blood then receives one queued blood under the original cap four");
                }
                else Check(resource.amount == cap && resource.cap == cap && resource.isState == isState, "the real resource cycle preserves a full value when passive consumption is absent or replenished: " + character.id + "/" + phase.resourceKey);
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
    private static GameEngine RestrictionFixture(BossGoal goal, int threshold = 24)
    {
        BossProfiles.Get("nia").phases[0] = new BossPhaseDef { name = "restriction fixture", tell = "test", response = "test", signatureCardId = "nia_q",
            goal = goal, goalAmount = goal == BossGoal.NoHeavyHit ? threshold : 2, fallbackGoal = BossGoal.Damage,
            // Deliberately invalid authoring tests that the engine cannot bypass a restriction.
            fallbackAmount = 1, effect = BossEffect.Strike, effectAmount = 10 };
        return BossFixture("nia", 2);
    }
    private static void BossRestrictionRules()
    {
        var profile = BossProfiles.Get("nia"); var original = profile.phases[0];
        try
        {
            Check(BossMechanics.RestrictionMinimumCards == 2, "restrictions require two manual card uses, not an idle turn");
            Check(BossMechanics.IsDebuffSkill(GameDatabase.Card("hyunwoo_q")) && BossMechanics.IsDebuffSkill(GameDatabase.Card("jackie_q"))
                && BossMechanics.IsDebuffSkill(GameDatabase.Card("sua_e")) && BossMechanics.IsDebuffSkill(GameDatabase.Card("weapon_bat")),
                "direct control, bleed, conditional control and weapon control are debuff skills");
            Check(!BossMechanics.IsDebuffSkill(GameDatabase.Card("basic_attack")) && !BossMechanics.IsDebuffSkill(GameDatabase.Card("basic_guard"))
                && !BossMechanics.IsDebuffSkill(GameDatabase.Card("tactical_wind")) && !BossMechanics.IsDebuffSkill(null),
                "basics, pure healing and invalid cards are not debuff skills");
            Check(BossMechanics.IsDebuffSkill(new CardDef { id = "tactical_test", category = "tactical", key = "F", statuses = new[] { new CardStatusRule { key = "slow", duration = 1 } } }),
                "the same classification includes a tactical card that actually defines a harmful status");
            foreach (var goal in new[] { BossGoal.NoDebuffSkill, BossGoal.NoHeavyHit, BossGoal.NoMovement })
            {
                foreach (int cards in new[] { 0, 1, 2 })
                {
                    var e = RestrictionFixture(goal); var c = e.State.combat;
                    for (int i = 0; i < cards; ++i) Check(BossPlay(e, "basic_guard"), "safe manual restriction card: " + goal);
                    Check(!c.boss.interrupted && e.CurrentBossView.needsTurnEnd, "compliance remains pending until turn end: " + goal + "/" + cards);
                    string before = FieldSnapshot(e.State); var preview = BossMechanics.PreviewAtTurnEnd(c);
                    Check(preview != c.boss && preview.interrupted == (cards >= 2) && FieldSnapshot(e.State) == before,
                        "turn-end preview predicts minimum card count without mutating the live challenge: " + goal + "/" + cards);
                    Check(e.BeginEndTurn() && c.boss.interrupted == (cards >= 2), "only two compliant cards can finalize the restriction: " + goal + "/" + cards);
                    Check(BossMechanics.NeedsSpecialAction(c) == (cards < 2), "minimum-card failure keeps the normal special action: " + goal + "/" + cards);
                    if (cards >= 2)
                        Check(e.CombatActions.Count(a => a.kind == "boss_interrupt") == 1 && c.boss.rewardUntilTurn == 3 && !e.BeginEndTurn(),
                            "turn-end success grants exactly one interruption and one-turn attack opportunity");
                    BossFinishEnemy(e);
                }

                var broken = RestrictionFixture(goal); var b = broken.State.combat;
                string forbidden = goal == BossGoal.NoMovement ? "aya_e" : goal == BossGoal.NoDebuffSkill ? "hyunwoo_q" : "basic_attack";
                if (goal == BossGoal.NoHeavyHit) b.strength = 24 - broken.CardDamage("basic_attack");
                b.hand.Add(forbidden); Check(broken.CanPlayCard(b.hand.Count - 1), "a forbidden tactic remains a legal combat card");
                Check(broken.PlayCard(b.hand.Count - 1) && b.boss.violated && b.boss.violationCardId == forbidden, "using a forbidden card records an irreversible violation: " + goal);
                Check(BossPlay(broken, "basic_guard") && BossPlay(broken, "basic_attack") && b.boss.violated && !b.boss.interrupted,
                    "later safe cards and a damage fallback cannot repair the violation: " + goal);
                string snapshot = FieldSnapshot(broken.State); var view = broken.CurrentBossView; string intent = broken.EnemyIntent;
                Check(view.restricted && view.violated && !string.IsNullOrEmpty(view.restrictionText) && view.violationCardId == forbidden && view.nextActionDetails.Contains(GameDatabase.Card(forbidden).name)
                    && FieldSnapshot(broken.State) == snapshot, "read-only UI preserves and explains the exact forbidden card: " + goal);
                Check(broken.BeginEndTurn() && !b.boss.interrupted && BossMechanics.NeedsSpecialAction(b), "violated restriction resolves its advertised failure: " + goal);
            }

            var passive = RestrictionFixture(BossGoal.NoDebuffSkill); passive.State.passives.Add("jackie_p");
            Check(BossPlay(passive, "basic_attack") && passive.State.combat.playerTraits.skills.effects.Any(x => x.kind == "bleed"), "a normal attack really triggers a separate passive bleed");
            Check(BossPlay(passive, "basic_guard") && !passive.State.combat.boss.violated && passive.BeginEndTurn() && passive.State.combat.boss.interrupted,
                "passive debuffs do not turn basic attacks into forbidden skills");

            profile.phases[0] = new BossPhaseDef { name = "debuff use fixture", tell = "test", response = "test", signatureCardId = "nia_q", goal = BossGoal.DebuffSkill,
                goalAmount = 1, fallbackGoal = BossGoal.Damage, fallbackAmount = 9999, effect = BossEffect.Strike, effectAmount = 10 };
            var missed = BossFixture("nia", 2); missed.State.combat.enemyEvasion = 100;
            Check(BossPlay(missed, "hyunwoo_q") && missed.State.combat.boss.interrupted && !StatusMechanics.Has(missed.State.combat.enemyStatuses, "slow"),
                "debuff-use goal consistently counts the declared card use even when its hit misses");
        }
        finally { profile.phases[0] = original; }
    }
    private static void BossHeavyAttackBoundary()
    {
        var profile = BossProfiles.Get("nia"); var original = profile.phases[0];
        try
        {
            foreach (int damage in new[] { 23, 24 })
            {
                var e = RestrictionFixture(BossGoal.NoHeavyHit); var c = e.State.combat;
                c.strength = damage - e.CardDamage("basic_attack");
                Check(BossPlay(e, "basic_attack") && c.boss.violated == (damage >= 24), "direct attack threshold is inclusive, not off by one: " + damage);
                if (damage >= 24) Check(c.boss.violationDamage == 24, "violation records the card's actual direct damage");
            }
            var shield = RestrictionFixture(BossGoal.NoHeavyHit); var s = shield.State.combat;
            s.strength = 24 - shield.CardDamage("basic_attack"); s.enemyBlock = 999; int hp = s.enemyHp;
            Check(BossPlay(shield, "basic_attack") && s.enemyHp == hp && s.boss.violated && s.boss.violationDamage == 24,
                "damage absorbed entirely by the boss shield still counts as direct impact");
            var dodged = RestrictionFixture(BossGoal.NoHeavyHit); var d = dodged.State.combat;
            d.strength = 24 - dodged.CardDamage("basic_attack"); d.enemyEvasion = 100;
            Check(BossPlay(dodged, "basic_attack") && !d.boss.violated && BossPlay(dodged, "basic_guard") && dodged.BeginEndTurn() && d.boss.interrupted,
                "a fully evaded attack has zero direct damage but still counts as a manual card use");
            var multiple = RestrictionFixture(BossGoal.NoHeavyHit, 10); var m = multiple.State.combat;
            Check(GameDatabase.Card("aya_q").hits == 2 && multiple.CardDamage("aya_q") < 10 && BossPlay(multiple, "aya_q") && m.boss.violated && m.boss.violationDamage >= 10,
                "all direct hits from one card are combined before the heavy-attack decision");
            var boosted = RestrictionFixture(BossGoal.NoHeavyHit, 8); boosted.State.passives.Add("isaac_p");
            Check(boosted.CardDamage("basic_attack") == 6 && BossPlay(boosted, "basic_attack") && boosted.State.combat.boss.violated
                && boosted.State.combat.boss.violationDamage == 8, "bonuses added to the card's own hit count toward its direct attack threshold");
            var automatic = RestrictionFixture(BossGoal.NoHeavyHit); var a = automatic.State.combat;
            a.enemyPoison = 30; a.playerSkills.effects.Add(new SkillTimedEffect { owner = "test", sourceCard = "jackie_q", key = "automatic", kind = "burn", amount = 40, remaining = 1 });
            hp = a.enemyHp;
            Check(BossPlay(automatic, "basic_guard") && BossPlay(automatic, "basic_guard") && automatic.BeginEndTurn()
                && hp - a.enemyHp >= 40 && !a.boss.violated && a.boss.interrupted, "automatic poison and timed damage cannot violate a manual heavy-attack restriction");
        }
        finally { profile.phases[0] = original; }
    }
    private static void BossRestrictionPersistence()
    {
        var profile = BossProfiles.Get("nia"); var original = profile.phases[0];
        try
        {
            foreach (var goal in new[] { BossGoal.NoDebuffSkill, BossGoal.NoMovement, BossGoal.NoHeavyHit })
            {
                var e = RestrictionFixture(goal); var c = e.State.combat;
                Check(BossPlay(e, "basic_guard"), "persist one compliant manual card");
                var loaded = new GameEngine((RunState)Clone(e.State));
                Check(loaded.State.combat.boss.version >= 2 && loaded.State.combat.boss.progress == 1 && !loaded.State.combat.boss.violated,
                    "new-format save preserves a partial restriction instead of restarting its challenge");
                Check(BossPlay(e, "basic_guard") && BossPlay(loaded, "basic_guard"), "complete both saved and live restrictions");
                string before = FieldSnapshot(e.State); int rng = e.State.rngState;
                for (int read = 0; read < 5; ++read) { var view = e.CurrentBossView; string intent = e.EnemyIntent; }
                Check(FieldSnapshot(e.State) == before && e.State.rngState == rng && !c.boss.interrupted, "successful end-now forecasts never commit the live challenge or use RNG");
                Check(e.BeginEndTurn() && loaded.BeginEndTurn() && c.boss.interrupted && FieldSnapshot(e.State) == FieldSnapshot(loaded.State),
                    "live and restored restrictions finalize identically before the enemy turn");
                var finished = new GameEngine((RunState)Clone(loaded.State));
                BossFinishEnemy(e); BossFinishEnemy(finished);
                Check(FieldSnapshot(e.State) == FieldSnapshot(finished.State), "saving after finalization does not replay an interruption or reward");

                var broken = RestrictionFixture(goal); var b = broken.State.combat;
                string forbidden = goal == BossGoal.NoMovement ? "aya_e" : goal == BossGoal.NoDebuffSkill ? "hyunwoo_q" : "basic_attack";
                if (goal == BossGoal.NoHeavyHit) b.strength = 24 - broken.CardDamage("basic_attack");
                Check(BossPlay(broken, forbidden), "make a real violation before saving");
                var restored = new GameEngine((RunState)Clone(broken.State));
                Check(restored.State.combat.boss.violated && restored.State.combat.boss.violationCardId == forbidden
                    && restored.State.combat.boss.violationDamage == b.boss.violationDamage, "save preserves violation identity and direct damage");
                Check(BossPlay(restored, "basic_guard") && restored.BeginEndTurn() && !restored.State.combat.boss.interrupted, "load cannot erase or repair a violation");
                BossFinishEnemy(restored); var r = restored.State.combat;
                Check(!restored.CurrentBossView.violated && string.IsNullOrEmpty(restored.CurrentBossView.violationCardId),
                    "completed restriction does not show the previous violation on a waiting turn");
                r.turn = r.boss.nextMechanicTurn; BossMechanics.BeginPlayerTurn(r);
                Check(r.boss.challengeActive && !r.boss.violated && r.boss.progress == 0 && string.IsNullOrEmpty(r.boss.violationCardId),
                    "a fresh telegraph resets prior violations and manual-card progress");

                var old = (RunState)Clone(broken.State); old.combat.boss.version = 1;
                old.combat.boss.delayedDamage = 7; old.combat.boss.delayedDueTurn = 2; old.combat.boss.rewardUntilTurn = 3;
                old.combat.boss.counterDamage = 4; old.combat.boss.counterUntilTurn = 3;
                int oldHp = old.hp, oldEnemy = old.combat.enemyHp, oldRng = old.rngState;
                var migrated = new GameEngine(old); var migratedBoss = migrated.State.combat.boss;
                Check(migratedBoss.version >= 2 && !migratedBoss.challengeActive && migratedBoss.nextMechanicTurn > migrated.State.combat.turn,
                    "an old active changed condition is deferred instead of being judged retroactively");
                Check(migratedBoss.delayedDamage == 7 && migratedBoss.delayedDueTurn == 2 && migratedBoss.rewardUntilTurn == 3
                    && migratedBoss.counterDamage == 4 && migrated.State.hp == oldHp && migrated.State.combat.enemyHp == oldEnemy && migrated.State.rngState == oldRng,
                    "restriction migration preserves reserved effects, existing vulnerability, health and RNG");
                Check(migrated.BeginEndTurn() && BossMechanics.NeedsSpecialAction(migrated.State.combat) && migrated.AdvanceEnemyAction()
                    && migrated.State.combat.boss.delayedDamage == 0, "deferring the changed challenge never cancels a previously scheduled explosion");
            }
        }
        finally { profile.phases[0] = original; }
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
