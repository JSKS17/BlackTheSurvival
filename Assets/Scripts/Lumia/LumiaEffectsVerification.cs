using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;

namespace Lumia
{
    /// <summary>Opt-in development-player audit using the real battle interface and renderer.</summary>
    [DefaultExecutionOrder(10000)]
    public sealed class LumiaEffectsVerification : MonoBehaviour
    {
        [Serializable] public sealed class ProfileRecord
        {
            public string card, motif, motion, primary, accent;
            public float duration, impactAt;
            public int projectiles;
            public bool movement, ultimate;
        }
        [Serializable] public sealed class ViewRecord
        {
            public string view, card, side, motif, motion;
            public float progress;
            public bool reducedMotion, directCast, cueVisible;
        }
        [Serializable] public sealed class Coverage
        {
            public string version, result = "RUNNING", failure;
            public int cards, skills, characters, motifs, motions, playerActions, enemyActions;
            public int screenshots, guiRestoreChecks, cueLayoutMeasurements, width = 1280, height = 720;
            public bool isolatedSave, personalSavesUnchanged, animationStateUnchanged, actualPlayerRoute, actualEnemyRoute;
            public bool reducedMotionStationary, secondaryActionsNoCardCue, independentActorMotion;
            public List<ProfileRecord> profiles = new List<ProfileRecord>();
            public List<ViewRecord> views = new List<ViewRecord>();
        }
        readonly Coverage coverage = new Coverage();
        Dictionary<string, string> personalFiles;
        string output;
        bool checkGui, failed;
        GUIStyle auditFont;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (Debug.isDebugBuild && Array.IndexOf(Environment.GetCommandLineArgs(), "-bts-effects-verify") >= 0)
                new GameObject("BTS effects verification").AddComponent<LumiaEffectsVerification>();
        }

        IEnumerator Start()
        {
            output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "EffectsVerification"));
            Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output, "coverage.json"), JsonUtility.ToJson(coverage, true));
            var runner = Verify();
            while (!failed)
            {
                object next = null; bool more = false;
                try { more = runner.MoveNext(); if (more) next = runner.Current; }
                catch (Exception exception) { Fail(exception); }
                if (failed || !more) break;
                yield return next;
            }
            if (failed) Application.Quit(1);
        }

        IEnumerator Verify()
        {
            Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
            personalFiles = PersonalFiles();
            yield return null; yield return new WaitForSecondsRealtime(.3f);
            var game = LumiaGame.Instance;
            Require(game != null, "The normal game bootstrap must be present.");
            game.GuardEffectsVerification(); coverage.isolatedSave = true;
            Require(Screen.width == 1280 && Screen.height == 720, "Native verification requires 1280 x 720.");
            coverage.version = Application.version;
            coverage.cards = GameDatabase.Cards.Count;
            coverage.skills = GameDatabase.Cards.Count(card => card.category == "skill");
            coverage.characters = GameDatabase.Characters.Count;
            Require(coverage.cards == 392 && coverage.skills == 364 && coverage.characters == 91,
                "The complete production card roster must be loaded.");

            game.PrepareEffectsVerification("nia");
            foreach (var card in GameDatabase.Cards)
            {
                var profile = CombatVisualProfiles.For(card);
                Require(profile != null && !string.IsNullOrEmpty(profile.motif) && !string.IsNullOrEmpty(profile.motion), card.id + ": missing profile.");
                Require(profile.duration > .3f && profile.duration <= 1.5f && profile.impactAt > 0 && profile.impactAt < 1,
                    card.id + ": invalid animation timing.");
                Color color;
                Require(ColorUtility.TryParseHtmlString("#" + profile.primary, out color) && ColorUtility.TryParseHtmlString("#" + profile.accent, out color),
                    card.id + ": invalid palette.");
                Require(ReferenceEquals(profile, CombatVisualProfiles.For(card)), card.id + ": profiles must be cached without gameplay randomness.");
                coverage.profiles.Add(new ProfileRecord { card = card.id, motif = profile.motif, motion = profile.motion,
                    primary = profile.primary, accent = profile.accent, duration = profile.duration, impactAt = profile.impactAt,
                    projectiles = profile.projectiles, movement = card.movement, ultimate = profile.ultimate });
                foreach (bool enemy in new[] { false, true })
                {
                    game.VerifyEffectsCardMotion(card, enemy);
                    if (enemy) coverage.enemyActions++; else coverage.playerActions++;
                }
            }
            coverage.motifs = coverage.profiles.Select(profile => profile.motif).Distinct().Count();
            coverage.motions = coverage.profiles.Select(profile => profile.motion).Distinct().Count();
            game.VerifyEffectsSecondaryActions(); coverage.secondaryActionsNoCardCue = true;
            game.VerifyEffectsIndependentActors(); coverage.independentActorMotion = true;
            coverage.reducedMotionStationary = true;

            // One native battle view for every visual family; sides alternate rather than reusing a player-only demo.
            int ordinal = 0;
            foreach (var group in GameDatabase.Cards.GroupBy(card => CombatVisualProfiles.For(card).motif).OrderBy(group => group.Key))
            {
                var card = group.OrderByDescending(value => value.damage > 0).ThenBy(value => value.key == "Q" ? 0 : 1).ThenBy(value => value.id).First();
                bool enemy = ordinal++ % 2 == 1;
                float progress = Mathf.Min(.78f, CombatVisualProfiles.For(card).impactAt + .12f);
                game.ShowEffectsVerification(card.id, enemy, progress);
                var record = Record("motif_" + group.Key + (enemy ? "_enemy" : "_player"), card, enemy, progress, false, true);
                yield return new WaitForSecondsRealtime(.12f);
                game.AssertEffectsAnimationUnchanged();
                Capture(record);
                yield return new WaitForSecondsRealtime(.12f);
                Require(File.Exists(Path.Combine(output, record.view + ".png")), "Native capture missing: " + record.view);
                Debug.Log("BTS EFFECTS CAPTURE " + record.view);
            }

            var movement = GameDatabase.Cards.First(card => card.movement && CombatVisualProfiles.For(card).motion == "dash");
            string[] names = { "actual_player_cast_intro", "actual_enemy_cast_intro", "cast_fade", "movement_depart", "movement_contact", "movement_return",
                "reduced_player", "reduced_enemy", "secondary_no_card_popup", "rapid_both_sides" };
            foreach (string name in names)
            {
                string id = name.StartsWith("movement_", StringComparison.Ordinal) || name.StartsWith("reduced_", StringComparison.Ordinal) || name == "rapid_both_sides"
                    ? movement.id : "nia_q";
                var card = GameDatabase.Card(id);
                bool enemy = name == "actual_enemy_cast_intro" || name == "reduced_enemy";
                bool reduced = name.StartsWith("reduced_", StringComparison.Ordinal);
                bool direct = name != "secondary_no_card_popup";
                float progress = name.Contains("cast_intro") ? .14f : name == "cast_fade" ? .9f : name == "movement_depart" ? .18f
                    : name == "movement_return" ? .86f : reduced ? .3f : .52f;
                if (name.StartsWith("actual_", StringComparison.Ordinal))
                {
                    game.PlayEffectsVerificationCard(id, enemy, progress);
                    if (enemy) coverage.actualEnemyRoute = true; else coverage.actualPlayerRoute = true;
                }
                else game.ShowEffectsVerification(id, enemy, progress, reduced, name == "secondary_no_card_popup", name == "rapid_both_sides");
                var record = Record(name, card, enemy, progress, reduced, direct);
                yield return new WaitForSecondsRealtime(.12f);
                checkGui = true;
                yield return null;
                checkGui = false;
                Require(!failed, "GUI restoration failed.");
                game.AssertEffectsAnimationUnchanged();
                Capture(record);
                yield return new WaitForSecondsRealtime(.12f);
                Require(File.Exists(Path.Combine(output, record.view + ".png")), "Native capture missing: " + record.view);
                Debug.Log("BTS EFFECTS CAPTURE " + record.view);
            }
            foreach (string motion in new[] { "blink", "leap", "retreat", "orbit" })
            {
                var card = GameDatabase.Cards.First(value => value.movement && CombatVisualProfiles.For(value).motion == motion);
                bool enemy = motion == "leap" || motion == "orbit";
                game.ShowEffectsVerification(card.id, enemy, .45f);
                var record = Record("movement_" + motion + "_contact", card, enemy, .45f, false, true);
                yield return new WaitForSecondsRealtime(.12f);
                game.AssertEffectsAnimationUnchanged();
                Capture(record);
                yield return new WaitForSecondsRealtime(.12f);
                Require(File.Exists(Path.Combine(output, record.view + ".png")), "Native capture missing: " + record.view);
                Debug.Log("BTS EFFECTS CAPTURE " + record.view);
            }
            Require(coverage.views.Count >= 24 && coverage.views.Count <= 48, "The native capture set must remain reviewable (24 to 48 views).");
            Require(coverage.guiRestoreChecks > 0, "The native renderer must restore GUI state after rendering.");
            Require(coverage.cueLayoutMeasurements == coverage.cards * 3 * 2, "Every native card reveal title and owner must fit all three zoom scales.");
            Require(personalFiles.OrderBy(pair => pair.Key).SequenceEqual(PersonalFiles().OrderBy(pair => pair.Key)), "Personal run/research files changed during verification.");
            coverage.personalSavesUnchanged = coverage.animationStateUnchanged = true;
            coverage.screenshots = coverage.views.Count;
            coverage.result = "PASS";
            File.WriteAllText(Path.Combine(output, "coverage.json"), JsonUtility.ToJson(coverage, true));
            game.FinishEffectsVerification();
            Debug.Log("BTS EFFECTS VERIFICATION PASS / cards=" + coverage.cards + " / profiles=" + coverage.profiles.Count + " / motifs=" + coverage.motifs
                + " / motions=" + coverage.motions + " / playerActions=" + coverage.playerActions + " / enemyActions=" + coverage.enemyActions
                + " / views=" + coverage.screenshots + " / GUIrestore=" + coverage.guiRestoreChecks + " / actualPlayerRoute=PASS / actualEnemyRoute=PASS"
                + " / cueLayoutMeasurements=" + coverage.cueLayoutMeasurements
                + " / secondaryNoCue=PASS / reducedMotion=PASS / independentSides=PASS / stateUnchanged=PASS / personalSaves=PASS");
            Application.Quit(0);
        }

        ViewRecord Record(string name, CardDef card, bool enemy, float progress, bool reduced, bool direct)
        {
            var profile = CombatVisualProfiles.For(card);
            var action = new CombatAction { cardId = card.id, enemy = enemy, kind = direct ? null : "trait_bonus_damage", traitId = direct ? null : "isaac_p" };
            var record = new ViewRecord { view = name, card = card.id, side = enemy ? "enemy" : "player", motif = profile.motif, motion = profile.motion,
                progress = progress, reducedMotion = reduced, directCast = direct,
                cueVisible = CombatCardCue.Visible(action, card, progress, reduced ? .24f : profile.duration, reduced) };
            coverage.views.Add(record);
            return record;
        }
        void Capture(ViewRecord record)
        {
            string path = Path.Combine(output, record.view + ".png");
            if (File.Exists(path)) File.Delete(path);
            ScreenCapture.CaptureScreenshot(path);
        }

        void OnGUI()
        {
            if (!checkGui || failed || Event.current.type != EventType.Repaint || !LumiaGame.Instance) return;
            try
            {
                if (auditFont == null) auditFont = new GUIStyle(GUI.skin.label) { font = Resources.Load<Font>("Lumia/Galmuri11"), fontSize = 13 };
                if (coverage.cueLayoutMeasurements == 0) coverage.cueLayoutMeasurements = CombatCardCue.VerifyLayout(auditFont);
                LumiaGame.Instance.VerifyEffectsGuiRestore(auditFont);
                coverage.guiRestoreChecks++;
            }
            catch (Exception exception) { Fail(exception); }
        }
        void Fail(Exception exception)
        {
            failed = true; coverage.result = "FAIL"; coverage.failure = exception.Message;
            Debug.LogError("BTS EFFECTS VERIFICATION FAIL " + exception);
            if (!string.IsNullOrEmpty(output)) File.WriteAllText(Path.Combine(output, "coverage.json"), JsonUtility.ToJson(coverage, true));
        }
        static Dictionary<string, string> PersonalFiles()
        {
            var result = new Dictionary<string, string>();
            foreach (string name in new[] { GameIdentity.SaveFileName, GameIdentity.SaveFileName + ".bak", "bts-research-v1.json", "bts-research-v1.json.bak" })
            {
                string path = Path.Combine(Application.persistentDataPath, name);
                using (var sha = SHA256.Create()) result[name] = File.Exists(path) ? BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "") : "MISSING";
            }
            return result;
        }
        internal static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }

    public sealed partial class LumiaGame
    {
        string effectsVerificationState;
        bool EffectsVerificationEnabled => Debug.isDebugBuild && Array.IndexOf(Environment.GetCommandLineArgs(), "-bts-effects-verify") >= 0;

        public void GuardEffectsVerification()
        {
            LumiaEffectsVerification.Require(EffectsVerificationEnabled && Path.GetFileName(savePath) == "lumia-verification.json",
                "Effects verification requires an isolated development run and research slot.");
        }
        public void PrepareEffectsVerification(string opponent)
        {
            GuardEffectsVerification();
            lobby = settings = catalog = inventory = help = fieldInfo = fieldEnemy = inspectEnemyCard = enemyLoadout = research = false;
            modeSelecting = runMenu = endlessConfirm = false;
            inspectCard = inspectTrait = inspectGear = craftPending = mapIntel = inspectBoss = "";
            sound = false; reduceMotion = false; toastUntil = 0;
            retainSelecting = hyperloopSelecting = false; handScroll = Vector2.zero;
            Engine = new GameEngine(90410);
            var main = GameDatabase.Runes.First(rune => rune.main);
            Engine.SetRunes(main.id, GameDatabase.Runes.First(rune => !rune.main && rune.tree == main.tree).id);
            Engine.SelectStartingPassive(Engine.State.passiveOffers[0]);
            foreach (string id in Engine.State.draftOffers.Take(3).ToArray()) Engine.ToggleDraft(id);
            LumiaEffectsVerification.Require(Engine.BeginJourney(), "Effects fixture must complete normal preparation.");
            var state = Engine.State;
            state.passives.Clear(); state.gear.Clear(); state.mainRune = state.supportRune = "";
            state.hp = state.maxHp = 240;
            var node = Engine.AvailableNodes().First(); node.kind = ZoneKind.Subject; node.assignedOpponent = opponent;
            LumiaEffectsVerification.Require(Engine.EnterNode(node.lane) && state.stage == RunStage.Combat, "Effects fixture must enter an actual subject battle.");
            var combat = state.combat;
            combat.enemyHp = combat.enemyMaxHp = 360; combat.enemyGear.Clear(); combat.enemyPassiveId = "";
            combat.enemyTraits = new TraitActorState(); combat.enemySkills = new SkillActorState(); combat.enemyStatuses = new StatusActorState();
            combat.playerTraits = state.persistentTraits = new TraitActorState(); combat.playerSkills = new SkillActorState(); combat.playerStatuses = new StatusActorState();
            combat.enemyEvasion = combat.evasion = combat.enemyBlock = combat.block = 0;
            combat.energy = Engine.MaxEnergy;
            combat.hand = new List<string> { "nia_q", "nia_w", "aya_e", "basic_attack", "basic_guard" };
            displayedTurn = combat.turn;
            nextEnemyActionAt = float.MaxValue; nextPlayerActionAt = 0;
            effects.Clear(); Engine.CombatActions.Clear();
            CaptureEffectsVerificationState();
        }
        void CaptureEffectsVerificationState()
        {
            // Refresh the read-only derived intent once before comparing the serialized gameplay state.
            string intent = Engine.EnemyIntent;
            effectsVerificationState = JsonUtility.ToJson(Engine.State);
        }
        public void AssertEffectsAnimationUnchanged()
        {
            LumiaEffectsVerification.Require(effectsVerificationState == JsonUtility.ToJson(Engine.State),
                "Rendering changed HP, energy, skill/status state, deck, or gameplay randomness.");
        }
        static CombatAction EffectsAction(CardDef card, bool enemy, int serial = 1)
        {
            return new CombatAction { id = serial, cardId = card.id, enemy = enemy,
                damage = card.damage > 0 ? Math.Max(1, card.damage) : 0, block = Math.Max(0, card.block), heal = Math.Max(0, card.heal) };
        }
        public void VerifyEffectsCardMotion(CardDef card, bool enemy)
        {
            GuardEffectsVerification(); effects.Clear();
            var action = EffectsAction(card, enemy);
            LumiaEffectsVerification.Require(CombatCardCue.IsDirectCast(action, card), card.id + ": actual card action must reveal its card.");
            float duration = effects.Add(action, false);
            LumiaEffectsVerification.Require(duration >= CombatVisualProfiles.For(card).duration, card.id + ": direct use must retain the complete visual timeline.");
            foreach (float progress in new[] { 0f, .15f, .35f, .55f, .8f, 1f })
            {
                effects.FreezeForVerification(progress);
                Vector2 offset = effects.Offset(enemy, false);
                LumiaEffectsVerification.Require(!float.IsNaN(offset.x) && !float.IsNaN(offset.y) && Mathf.Abs(offset.x) < 700 && Mathf.Abs(offset.y) < 250,
                    card.id + ": invalid actor position.");
                LumiaEffectsVerification.Require(effects.Offset(enemy, true) == Vector2.zero, card.id + ": reduced motion must remain stationary.");
                if (progress == 0 || progress == 1)
                    LumiaEffectsVerification.Require(offset == Vector2.zero, card.id + ": an actor must start and finish at its original position.");
                if (progress == .15f)
                    LumiaEffectsVerification.Require(effects.Casting, card.id + ": direct use must retain its brief card reveal.");
                if (progress >= .8f)
                    LumiaEffectsVerification.Require(!effects.Casting, card.id + ": the card reveal must release input before the effect finishes.");
            }
            effects.Clear(); effects.Add(action, true); effects.FreezeForVerification(.3f);
            LumiaEffectsVerification.Require(effects.Offset(enemy, true) == Vector2.zero, card.id + ": reduced use must not move the sprite.");
            effects.Clear(); AssertEffectsAnimationUnchanged();
        }
        public void VerifyEffectsSecondaryActions()
        {
            var card = GameDatabase.Card("nia_q");
            foreach (string kind in new[] { "trait_bonus_damage", "status_stun", "delayed_damage", "summon", "bleed", "burn", "counter", "boss_strike", "augment", "revive" })
            {
                effects.Clear(); var action = new CombatAction { cardId = card.id, kind = kind, damage = 3 };
                LumiaEffectsVerification.Require(!CombatCardCue.IsDirectCast(action, card), kind + ": secondary effects must not reveal a new card.");
                effects.Add(action, false); effects.FreezeForVerification(.2f);
                LumiaEffectsVerification.Require(!effects.Casting, kind + ": secondary effects must not lock card input as a new cast.");
            }
            var trait = new CombatAction { cardId = card.id, traitId = "isaac_p", damage = 3 };
            LumiaEffectsVerification.Require(!CombatCardCue.IsDirectCast(trait, card), "Trait source identifiers must suppress card reveals even without kind.");
            effects.Clear(); effects.Add(trait, false); effects.FreezeForVerification(.2f);
            LumiaEffectsVerification.Require(!effects.Casting, "A passive pulse must not become a direct cast.");
            foreach (var action in new[]
            {
                new CombatAction { cardId = "status_poison", damage = 3 },
                new CombatAction { cardId = "ao_dai", kind = "deferred_damage", damage = 3 },
                new CombatAction { cardId = card.id, traitId = "lightning", kind = "trait_bonus_damage", damage = 3 },
                new CombatAction { cardId = card.id, traitId = "gear:ao_dai", kind = "trait_block", block = 3 }
            })
            {
                effects.Clear(); effects.Add(action, false); effects.FreezeForVerification(.2f);
                LumiaEffectsVerification.Require(!CombatCardCue.IsDirectCast(action, GameDatabase.Card(action.cardId)) && !effects.Casting,
                    "Poison, rune, equipment, and deferred-damage routes must remain secondary effects.");
            }
            effects.Clear(); AssertEffectsAnimationUnchanged();
        }
        public void VerifyEffectsIndependentActors()
        {
            var card = GameDatabase.Cards.First(value => value.movement && CombatVisualProfiles.For(value).motion == "dash");
            effects.Clear(); effects.Add(EffectsAction(card, false), false); effects.Add(EffectsAction(card, true, 2), false);
            bool playerMoved = false, enemyMoved = false;
            foreach (float progress in new[] { .25f, .4f, .55f, .7f })
            {
                effects.FreezeForVerification(progress);
                playerMoved |= effects.Offset(false, false) != Vector2.zero;
                enemyMoved |= effects.Offset(true, false) != Vector2.zero;
            }
            LumiaEffectsVerification.Require(playerMoved && enemyMoved, "Overlapping uses must retain independent motion for both actors.");
            effects.Clear(); AssertEffectsAnimationUnchanged();
        }
        public void ShowEffectsVerification(string id, bool enemy, float progress, bool reduced = false, bool secondary = false, bool rapid = false)
        {
            var card = GameDatabase.Card(id);
            var owner = GameDatabase.Characters.FirstOrDefault(character => character.name == card.owner);
            PrepareEffectsVerification(owner?.id ?? "nia");
            reduceMotion = reduced;
            var action = EffectsAction(card, enemy);
            if (secondary) { action.kind = "trait_bonus_damage"; action.traitId = "isaac_p"; action.damage = 4; }
            effects.Add(action, reduced);
            if (rapid) effects.Add(EffectsAction(card, !enemy, 2), reduced);
            effects.FreezeForVerification(progress);
            CaptureEffectsVerificationState();
        }
        public void PlayEffectsVerificationCard(string id, bool enemy, float progress)
        {
            var card = GameDatabase.Card(id);
            var owner = GameDatabase.Characters.FirstOrDefault(character => character.name == card.owner);
            PrepareEffectsVerification(owner?.id ?? "nia");
            var combat = Engine.State.combat;
            if (enemy)
            {
                LumiaEffectsVerification.Require(Engine.BeginEndTurn(), "Actual enemy route must begin the player's end turn.");
                combat.enemyPlan = new List<string> { id }; combat.enemyHand = new List<string> { id };
                combat.enemyPlanCosts = new List<int> { card.cost }; combat.enemyPlanFreeCast = new List<bool> { false };
                combat.enemyActionIndex = 0; combat.enemyAvailableEnergy = Math.Max(Engine.MaxEnergy, card.cost);
                Engine.CombatActions.Clear();
                LumiaEffectsVerification.Require(Engine.AdvanceEnemyAction(), "Actual enemy route must advance a valid planned card.");
            }
            else
            {
                combat.hand[0] = id;
                LumiaEffectsVerification.Require(Engine.CanPlayCard(0) && Engine.PlayCard(0), "Actual player route must play a valid hand card.");
            }
            LumiaEffectsVerification.Require(Engine.CombatActions.Any(action => action.enemy == enemy && action.cardId == id && CombatCardCue.IsDirectCast(action, card)),
                "The real engine route must emit a direct card action for the correct side.");
            ConsumeCombatActions(); effects.FreezeForVerification(progress);
            LumiaEffectsVerification.Require(effects.Casting, "The real card route must display its short card reveal.");
            CaptureEffectsVerificationState();
        }
        public void VerifyEffectsGuiRestore(GUIStyle style)
        {
            Matrix4x4 oldMatrix = GUI.matrix; Color oldColor = GUI.color; bool oldEnabled = GUI.enabled; int oldSize = style.fontSize;
            try
            {
                Matrix4x4 matrix = Matrix4x4.TRS(new Vector3(-10000, -10000, 0), Quaternion.identity, new Vector3(.8f, .8f, 1));
                Color color = new Color(.7f, .8f, .9f, .6f);
                GUI.matrix = matrix; GUI.color = color; GUI.enabled = false;
                effects.Draw(style, reduceMotion);
                LumiaEffectsVerification.Require(GUI.matrix == matrix && GUI.color == color && !GUI.enabled && style.fontSize == oldSize,
                    "Effects must restore GUI transform, color, enabled state, and the supplied font size.");
                effects.DrawActor(new Rect(129, 169, 214, 215), PixelArt.Portrait("hana"), false, reduceMotion);
                effects.DrawActor(new Rect(913, 169, 218, 216), PixelArt.Portrait(Engine.State.combat.enemyId), true, reduceMotion);
                LumiaEffectsVerification.Require(GUI.matrix == matrix && GUI.color == color && !GUI.enabled && style.fontSize == oldSize,
                    "Actor movement must restore the surrounding GUI state.");
            }
            finally { GUI.matrix = oldMatrix; GUI.color = oldColor; GUI.enabled = oldEnabled; style.fontSize = oldSize; }
        }
        public void FinishEffectsVerification()
        {
            GuardEffectsVerification(); effects.Clear(); effects.ResumeVerification();
            nextEnemyActionAt = nextPlayerActionAt = 0;
            // A verification shutdown never advances or settles the player's expedition.
            lobby = true; Engine = null;
        }
    }
}
