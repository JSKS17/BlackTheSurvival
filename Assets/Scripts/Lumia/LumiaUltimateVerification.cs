using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;

namespace Lumia
{
    /// <summary>Opt-in native-player regression for approved R atlases and actual combat routes.</summary>
    [DefaultExecutionOrder(10010)]
    public sealed class LumiaUltimateVerification : MonoBehaviour
    {
        [Serializable] public sealed class Report
        {
            public string version, result = "RUNNING", failure;
            public int subjects, frames, playerCasts, enemyCasts, layoutChecks, screenshots;
            public bool ownerFromCard, pointFiltered, nonOverlappingCards, victoryFinishesBeforeRewards;
            public bool defeatFinishesBeforeResults, reducedMotion, resetClears, noGameplayMutation, personalSavesUnchanged, pauseResumes;
            public bool garnetFreeRecast, midCastMotionSettingStable;
            public bool concurrentCardAndAnimation, transparentCasterOverlay, enlargedFixedFrameUnion;
            public bool readableOutlinedFeedback, feedbackDoesNotExtendActionLock;
            public bool rapidFeedbackHasDistinctLines;
            public bool combatFoodCostsAndDisabledState, pendingFollowupFieldText, criticalDamageCraftFilter;
            public List<string> captures = new List<string>();
            public List<CapturePixels> capturePixels = new List<CapturePixels>();
        }
        [Serializable] public sealed class CapturePixels
        {
            public string file;
            public int width, height, nonBlackPixels, colorVariants, stagePixels, stageBrightPixels, stageColorVariants;
        }
        readonly Report report = new Report();
        string output;
        bool failed;
        Dictionary<string, string> personal;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (Debug.isDebugBuild && Array.IndexOf(Environment.GetCommandLineArgs(), "-bts-ultimate-verify") >= 0)
                new GameObject("BTS ultimate verification").AddComponent<LumiaUltimateVerification>();
        }
        IEnumerator Start()
        {
            output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "UltimateVerification"));
            Directory.CreateDirectory(output); personal = PersonalFiles(); SaveReport();
            var runner = Verify();
            while (!failed)
            {
                object next = null; bool more = false;
                try { more = runner.MoveNext(); if (more) next = runner.Current; }
                catch (Exception exception) { failed = true; report.result = "FAIL"; report.failure = exception.ToString(); SaveReport(); Debug.LogError("BTS ULTIMATE FAIL " + exception); }
                if (failed || !more) break;
                yield return next;
            }
            if (failed) Application.Quit(1);
        }
        IEnumerator Verify()
        {
            Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
            yield return null; yield return new WaitForSecondsRealtime(.3f);
            var game = LumiaGame.Instance;
            Require(game && game.UltimateVerificationEnabled, "Native ultimate runner requires isolated development mode.");
            report.version = Application.version;
            var catalog = UltimateAnimations.Catalog;
            Require(catalog.Count == 91, "All 91 approved subjects must be exported.");
            report.subjects = catalog.Count;
            foreach (var character in GameDatabase.Characters)
            {
                var card = character.cards.Select(GameDatabase.Card).Single(value => value.key == "R");
                var definition = UltimateAnimations.For(card);
                Require(definition != null && definition.id == character.id, "R animation must follow the card owner: " + card.id);
                var texture = UltimateAnimations.Texture(definition);
                Require(texture && texture.filterMode == FilterMode.Point && texture.mipmapCount == 1, "Point filtered native atlas: " + character.id);
                Require(definition.framesPerSecond == 8 && definition.frameCount == 12 && Math.Abs(definition.Duration - 1.5f) < .001f, "Approved timing: " + character.id);
                for (int frame = 0; frame < definition.frameCount; frame++)
                {
                    var uv = UltimateAnimations.FrameUV(definition, frame);
                    foreach (bool enemy in new[] { false, true })
                    {
                        var area = UltimateAnimations.Area(enemy); var rect = UltimateAnimations.FrameRect(definition, frame, enemy);
                        Require(Contains(area, rect, 1) && Contains(new Rect(0, 0, 1, 1), uv, .00001f), "Frame source and display bounds: " + character.id + "/" + frame);
                        Require(!area.Overlaps(new Rect(24, 485, 1004, 229)) && !area.Overlaps(CombatCardCue.UltimateEnvelope)
                            && area.yMin > 80 && area.yMax < 441, "Transparent R must not overlap the card reveal, top HUD or hand cards.");
                        Require(enemy ? area.center.x > 800 : area.center.x < 480, "R must be anchored above the casting actor's side.");
                        report.layoutChecks++;
                    }
                    report.frames++; report.layoutChecks++;
                }
                foreach (bool enemy in new[] { false, true })
                {
                    game.PlayUltimateVerification(card.id, enemy, 7);
                    Require(game.UltimateVerificationSubject == character.id, "Actual cast must choose the skill owner for either actor: " + card.id);
                    for (int frame = 0; frame < 12; frame++)
                    {
                        game.FreezeUltimateVerification(frame);
                        Require(game.UltimateVerificationFrame == frame, "Atlas playback must expose the expected distinct frame: " + card.id + "/" + frame);
                        game.AssertEffectsAnimationUnchanged(); report.layoutChecks++;
                    }
                    if (enemy) report.enemyCasts++; else report.playerCasts++;
                }
            }
            report.ownerFromCard = report.pointFiltered = report.nonOverlappingCards = report.noGameplayMutation = true;
            report.transparentCasterOverlay = report.enlargedFixedFrameUnion = true;
            Require(UltimateAnimations.For(new CardDef { id = "future_r", key = "R" }) == null
                && UltimateAnimations.For(GameDatabase.Card("nia_q")) == null, "Generic or missing R definitions must safely use the existing effect fallback.");

            foreach (string item in new[] { "nia:player", "garnet:enemy", "martina:player", "darko:enemy", "nadine:player", "tia:enemy" })
            {
                string[] pieces = item.Split(':'); game.PlayUltimateVerification(pieces[0] + "_r", pieces[1] == "enemy", 7);
                yield return new WaitForSecondsRealtime(.12f);
                yield return Capture(pieces[0] + "_" + pieces[1] + "_frame08");
            }
            game.PlayUltimateVerification("nia_r", false, -1);
            game.VerifyConcurrentUltimateCue(); report.concurrentCardAndAnimation = true;
            yield return new WaitForSecondsRealtime(.12f); yield return Capture("nia_card_and_animation_together");
            game.PlayUltimateVerification("garnet_r", true, -1); game.VerifyConcurrentUltimateCue();
            yield return new WaitForSecondsRealtime(.12f); yield return Capture("garnet_enemy_card_and_animation_together");
            game.VerifyReadableFeedback(); report.readableOutlinedFeedback = report.feedbackDoesNotExtendActionLock = true;
            yield return new WaitForSecondsRealtime(.12f); yield return Capture("combat_outlined_feedback_after_particles");
            game.VerifyRapidFeedback(); report.rapidFeedbackHasDistinctLines = true;
            yield return new WaitForSecondsRealtime(.12f); yield return Capture("combat_rapid_feedback_distinct_lines");
            game.VerifyCombatFoodInventory(); report.combatFoodCostsAndDisabledState = true;
            yield return new WaitForSecondsRealtime(.12f); yield return Capture("food_soup2_normal1_insufficient_cost");
            game.VerifyPendingFollowupField(); report.pendingFollowupFieldText = true;
            yield return new WaitForSecondsRealtime(.12f); yield return Capture("vanya_next_turn_free_followup_field");
            game.VerifyCriticalDamageCraftFilter(); report.criticalDamageCraftFilter = true;
            yield return new WaitForSecondsRealtime(.12f); yield return Capture("campfire_critical_damage_weapon_filter");
            game.VerifyUltimateReducedMotion(); report.reducedMotion = true;
            yield return new WaitForSecondsRealtime(.12f); yield return Capture("nia_reduced_motion");
            game.VerifyUltimateMotionSettingChanges(); report.midCastMotionSettingStable = true;
            game.VerifyUltimateTerminal(false); report.victoryFinishesBeforeRewards = true;
            yield return new WaitForSecondsRealtime(.12f); yield return Capture("victory_r_before_reward_cards");
            game.VerifyUltimateTerminal(true); report.defeatFinishesBeforeResults = true;
            yield return new WaitForSecondsRealtime(.12f); yield return Capture("defeat_r_before_result");
            game.VerifyUltimateGarnetFirst();
            yield return new WaitForSecondsRealtime(.12f); yield return Capture("garnet_first_r_free_followup");
            game.VerifyUltimateGarnetRecast(); report.garnetFreeRecast = true;
            yield return new WaitForSecondsRealtime(.12f); yield return Capture("garnet_free_recast_execution");
            game.PlayUltimateVerification("nia_r", false, 7);
            Screen.SetResolution(1024, 768, FullScreenMode.Windowed);
            yield return new WaitForSecondsRealtime(.3f); yield return Capture("nia_letterbox_1024x768");
            Screen.SetResolution(1600, 900, FullScreenMode.Windowed);
            yield return new WaitForSecondsRealtime(.3f); yield return Capture("nia_scaled_1600x900");
            game.VerifyUltimatePauseStart();
            yield return new WaitForSecondsRealtime(.25f);
            game.VerifyUltimatePauseFinish(); report.pauseResumes = true;
            game.VerifyUltimateReset(); report.resetClears = true;
            Require(personal.OrderBy(value => value.Key).SequenceEqual(PersonalFiles().OrderBy(value => value.Key)), "Personal saves must remain unchanged.");
            report.personalSavesUnchanged = true; report.result = "PASS"; SaveReport();
            game.FinishEffectsVerification();
            Debug.Log("BTS ULTIMATE PASS / subjects=" + report.subjects + " / frames=" + report.frames + " / actualPlayer=" + report.playerCasts
                + " / actualEnemy=" + report.enemyCasts + " / layout=" + report.layoutChecks + " / screenshots=" + report.screenshots
                + " / cardsNonOverlap=PASS / terminal=PASS / reduced=PASS / reset=PASS / pause=PASS / personalSaves=PASS");
            Application.Quit(0);
        }
        IEnumerator Capture(string name)
        {
            string path = Path.Combine(output, name + ".png");
            if (File.Exists(path)) File.Delete(path);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(path);
            yield return new WaitForSecondsRealtime(.15f);
            Require(File.Exists(path), "Actual screen capture must exist: " + name);
            var image = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                Require(ImageConversion.LoadImage(image, File.ReadAllBytes(path)), "Native capture PNG must decode: " + name);
                var pixels = image.GetPixels32();
                var colors = new HashSet<int>(); var stageColors = new HashSet<int>();
                var evidence = new CapturePixels { file = name + ".png", width = image.width, height = image.height };
                float scale = Math.Min(image.width / 1280f, image.height / 720f);
                float ox = (image.width - 1280 * scale) / 2, oy = (image.height - 720 * scale) / 2;
                var stage = UltimateAnimations.Area(LumiaGame.Instance.UltimateVerificationEnemy);
                int left = Mathf.RoundToInt(ox + stage.xMin * scale), right = Mathf.RoundToInt(ox + stage.xMax * scale);
                int top = Mathf.RoundToInt(oy + stage.yMin * scale), bottom = Mathf.RoundToInt(oy + stage.yMax * scale);
                for (int y = 0; y < image.height; y++)
                    for (int x = 0; x < image.width; x++)
                    {
                        var pixel = pixels[y * image.width + x]; int topY = image.height - 1 - y;
                        int key = ((pixel.r >> 4) << 8) | ((pixel.g >> 4) << 4) | (pixel.b >> 4);
                        colors.Add(key);
                        if (pixel.r > 8 || pixel.g > 8 || pixel.b > 8) evidence.nonBlackPixels++;
                        if (x >= left && x < right && topY >= top && topY < bottom)
                        {
                            evidence.stagePixels++; stageColors.Add(key);
                            if (pixel.r > 40 || pixel.g > 40 || pixel.b > 40) evidence.stageBrightPixels++;
                        }
                    }
                evidence.colorVariants = colors.Count; evidence.stageColorVariants = stageColors.Count;
                report.capturePixels.Add(evidence);
                Require(evidence.nonBlackPixels > pixels.Length / 20 && evidence.colorVariants > 32,
                    "Native capture is black, blank, or unrendered: " + name + ". Run the verification player in a visible game window; a hidden Unity window may not repaint its backbuffer.");
                Require(evidence.stageColorVariants > 16 && evidence.stageBrightPixels > evidence.stagePixels / 200,
                    "Native R stage has no visible rendered content: " + name);
            }
            finally { Destroy(image); }
            report.captures.Add(name + ".png"); report.screenshots++;
        }
        static bool Contains(Rect outer, Rect inner, float tolerance)
        {
            return inner.xMin >= outer.xMin - tolerance && inner.yMin >= outer.yMin - tolerance
                && inner.xMax <= outer.xMax + tolerance && inner.yMax <= outer.yMax + tolerance;
        }
        void SaveReport() { File.WriteAllText(Path.Combine(output, "coverage.json"), JsonUtility.ToJson(report, true)); }
        static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
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
    }

    public sealed partial class LumiaGame
    {
        public bool UltimateVerificationEnabled { get { return Debug.isDebugBuild && Array.IndexOf(Environment.GetCommandLineArgs(), "-bts-ultimate-verify") >= 0; } }
        public string UltimateVerificationSubject { get { return effects.UltimateSubject; } }
        public bool UltimateVerificationEnemy { get { return effects.UltimateEnemy; } }
        public int UltimateVerificationFrame { get { return effects.UltimateFrame(reduceMotion); } }
        float ultimateVerificationDuration;
        int pausedVerificationFrame;
        public void PlayUltimateVerification(string id, bool enemy, int frame)
        {
            if (!UltimateVerificationEnabled) throw new InvalidOperationException("Ultimate verification requires its isolated development flag.");
            var card = GameDatabase.Card(id); var owner = UltimateAnimations.For(card);
            PrepareEffectsVerification(owner.id);
            var state = Engine.State; var combat = state.combat;
            state.maxHp = state.hp = combat.enemyHp = combat.enemyMaxHp = 10000;
            combat.energy = 50;
            if (enemy)
            {
                if (!Engine.BeginEndTurn()) throw new InvalidOperationException("Actual enemy R must begin an enemy turn.");
                combat.enemyPlan = new List<string> { id }; combat.enemyHand = new List<string> { id };
                combat.enemyPlanCosts = new List<int> { card.cost }; combat.enemyPlanFreeCast = new List<bool> { false };
                combat.enemyActionIndex = 0; combat.enemyAvailableEnergy = 50; Engine.CombatActions.Clear();
                if (!Engine.AdvanceEnemyAction()) throw new InvalidOperationException("Actual enemy R failed: " + id);
            }
            else
            {
                combat.hand[0] = id;
                if (!Engine.PlayCard(0)) throw new InvalidOperationException("Actual player R failed: " + id);
            }
            if (!Engine.CombatActions.Any(action => action.cardId == id && action.enemy == enemy && CombatCardCue.IsDirectCast(action, card)))
                throw new InvalidOperationException("Actual R route must emit its direct cast: " + id);
            ConsumeCombatActions(); ultimateVerificationDuration = owner.Duration;
            FreezeUltimateVerification(frame); CaptureEffectsVerificationState();
        }
        public void FreezeUltimateVerification(int frame)
        {
            float elapsed = frame < 0 ? .1f : (frame + .25f) / 8f;
            effects.FreezeForVerification(elapsed / ultimateVerificationDuration);
            if (frame >= 0 && !effects.Casting) throw new InvalidOperationException("R playback must serialize subsequent direct cards.");
            var card = GameDatabase.Card(effects.UltimateSubject + "_r");
            if (elapsed < CombatCardCue.RevealSeconds && !CombatCardCue.Visible(new CombatAction { cardId = card.id }, card, elapsed / ultimateVerificationDuration, ultimateVerificationDuration, false))
                throw new InvalidOperationException("R must begin simultaneously with its card cue.");
        }
        public void VerifyConcurrentUltimateCue()
        {
            var card = GameDatabase.Card(effects.UltimateSubject + "_r");
            effects.FreezeForVerification(.1f / ultimateVerificationDuration);
            if (effects.UltimateFrame(reduceMotion) != 0 || !effects.Casting
                || !CombatCardCue.Visible(new CombatAction { cardId = card.id }, card, .1f / ultimateVerificationDuration, ultimateVerificationDuration, false))
                throw new InvalidOperationException("The R atlas and enlarged card must both be visible at 0.10 seconds.");
            AssertEffectsAnimationUnchanged();
        }
        public void VerifyReadableFeedback()
        {
            PrepareEffectsVerification("nia"); effects.Clear();
            effects.Add(new CombatAction { id = 1, cardId = "nia_q", damage = 17, critical = true, block = 9, heal = 4 }, false);
            effects.Add(new CombatAction { id = 2, traitId = GameDatabase.Character("isaac").passiveId, kind = "trait_bonus_damage", damage = 3 }, false);
            effects.Add(new CombatAction { id = 3, kind = "status_stun" }, false);
            effects.FreezeForVerification(1.15f);
            if (effects.Busy || effects.Casting || !effects.ReadableFeedbackAlive || CombatEffects.ReadableTextSeconds < 1)
                throw new InvalidOperationException("Outlined text must remain readable after its particles expire without extending action locks.");
            CaptureEffectsVerificationState();
        }
        public void VerifyRapidFeedback()
        {
            PrepareEffectsVerification("nia"); effects.Clear();
            for (int i = 0; i < 4; i++)
            {
                effects.Add(new CombatAction { id = i + 1, cardId = "basic_attack", damage = 6 + i * 3, critical = i == 3 }, false);
                effects.Add(new CombatAction { id = i + 11, traitId = GameDatabase.Character("isaac").passiveId, kind = "trait_bonus_damage", damage = i + 1 }, false);
            }
            effects.Add(new CombatAction { id = 22, kind = "trait_heal", heal = 3 }, false);
            effects.Add(new CombatAction { id = 23, kind = "trait_block", block = 5 }, false);
            effects.Add(new CombatAction { id = 24, kind = "status_stun" }, false);
            effects.FreezeForVerification(.75f);
            if (!effects.FeedbackLanesDistinct()) throw new InvalidOperationException("Rapid cards and procs must reserve separate stable text lines for each actor.");
            CaptureEffectsVerificationState();
        }
        public void VerifyUltimateReducedMotion()
        {
            PrepareEffectsVerification("nia"); reduceMotion = true;
            var card = GameDatabase.Card("nia_r");
            effects.Add(EffectsAction(card, false), true); effects.FreezeForVerification(.7f);
            if (effects.UltimateFrame(true) != 7 || effects.Offset(false, true) != Vector2.zero)
                throw new InvalidOperationException("Reduced motion must show a stationary R impact frame.");
            CaptureEffectsVerificationState();
        }
        public void VerifyCombatFoodInventory()
        {
            PrepareEffectsVerification("nia"); var s = Engine.State; var c = s.combat;
            s.maxHp = 240; s.hp = 100; s.foods = new List<string> { "soup", "meat" }; c.energy = 1;
            if (Engine.FoodUseCost("soup") != 2 || Engine.FoodUseCost("meat") != 1 || Engine.CanUseFood(0) || !Engine.CanUseFood(1))
                throw new InvalidOperationException("At one energy, soup must be disabled and normal food available with costs two and one.");
            if (!Engine.UseFood(1) || c.energy != 0 || s.hp <= 100 || s.foods.Count != 1)
                throw new InvalidOperationException("The actual normal food route must consume one energy and one food.");
            c.energy = 1; s.foods.Add("meat"); inventory = true; inventoryTab = 2; inventoryScroll = Vector2.zero;
            CaptureEffectsVerificationState();
        }
        public void VerifyPendingFollowupField()
        {
            PrepareEffectsVerification("nia"); var s = Engine.State; var c = s.combat;
            s.deck = new List<string> { "vanya_q", "basic_attack" }; c.hand = new List<string> { "vanya_q" };
            c.drawPile.Clear(); c.discardPile.Clear(); c.exhaustPile.Clear(); c.energy = 20;
            if (!Engine.PlayCard(0) || c.freeCasts.Any(x => x.cardId == "vanya_q" && x.uses > 0)
                || !c.pendingFreeCasts.Any(x => x.cardId == "vanya_q" && x.uses == 1 && x.readyTurn == c.turn + 1))
                throw new InvalidOperationException("Actual Vanya Q must queue a next-turn follow-up instead of an immediate free cast.");
            var tokens = FieldTokens(false);
            var token = tokens.SingleOrDefault(x => x.kind == "pending_free_cast" && x.targetCard == "vanya_q");
            if (token == null || !TokenShort(token).Contains("다음 자기 턴 무료 1회"))
                throw new InvalidOperationException("The field must explain the queued free follow-up and its availability turn.");
            Engine.CombatActions.Clear(); effects.Clear(); fieldInfo = true; fieldEnemy = false;
            float pendingY = tokens.Take(tokens.IndexOf(token)).Sum(value => (value.kind ?? "").StartsWith("synergy_", StringComparison.Ordinal) ? 84 : 72);
            fieldScroll = new Vector2(0, pendingY);
            CaptureEffectsVerificationState();
        }
        public void VerifyCriticalDamageCraftFilter()
        {
            PrepareEffectsVerification("nia"); var s = Engine.State;
            s.combat = null; s.stage = RunStage.Map;
            var node = Engine.AvailableNodes().First(); node.kind = ZoneKind.Campfire;
            if (!Engine.EnterNode(node.lane) || !Engine.ChooseCamp(true))
                throw new InvalidOperationException("The native critical equipment fixture must enter and choose an actual crafting campfire.");
            s.objects.AddRange(GameDatabase.Objects.Select(value => value.id));
            craftSlot = GearSlot.Weapon; craftTags.Clear(); craftTags.Add("치명타 피해"); craftQuery = craftMaterial = ""; scroll = Vector2.zero;
            var gear = FilterCraftGear().ToArray();
            if (!gear.Any(value => value.id == "joyeuse") || gear.Any(value => value.critDamageBonus <= 0)
                || gear.Any(value => !DescriptionSummary.Gear(value).Contains("치명타 피해") || !PixelArt.SystemIcon(value.id)))
                throw new InvalidOperationException("The critical damage tag must show its new weapon and accurate bonus summary/icon.");
            effects.Clear(); Engine.CombatActions.Clear(); CaptureEffectsVerificationState();
        }
        public void VerifyUltimateTerminal(bool enemy)
        {
            PlayUltimateVerification("nia_r", enemy, 7);
            effects.Clear(); var c = Engine.State.combat;
            // Replay through the real route with a one-HP target, without introducing an artificial result stage.
            c.enemyTurn = false; c.enemyActionIndex = 0; c.enemyPlan.Clear(); Engine.CombatActions.Clear();
            c.energy = 50; c.hand = new List<string> { "nia_r" };
            if (enemy)
            {
                Engine.State.hp = 1; c.block = c.evasion = 0;
                Engine.BeginEndTurn(); c.enemyPlan = new List<string> { "nia_r" }; c.enemyHand = new List<string> { "nia_r" };
                c.enemyPlanCosts = new List<int> { GameDatabase.Card("nia_r").cost }; c.enemyPlanFreeCast = new List<bool> { false };
                c.enemyAvailableEnergy = 50; Engine.CombatActions.Clear(); Engine.AdvanceEnemyAction();
            }
            else { c.enemyHp = 1; c.enemyBlock = c.enemyEvasion = 0; Engine.PlayCard(0); }
            if (Engine.State.stage != (enemy ? RunStage.Lost : RunStage.Rewards)) throw new InvalidOperationException("Actual fatal R must settle its expected result.");
            ConsumeCombatActions(); FreezeUltimateVerification(7);
            if (!UltimateResultHold) throw new InvalidOperationException("Fatal R must finish before exposing result cards.");
            CaptureEffectsVerificationState();
        }
        public void VerifyUltimateMotionSettingChanges()
        {
            PlayUltimateVerification("nia_r", false, 7);
            reduceMotion = true;
            if (effects.UltimateFrame(true) != 7) throw new InvalidOperationException("Enabling reduced motion must not reinterpret an in-flight normal R timeline.");
            FreezeUltimateVerification(11);
            if (effects.UltimateFrame(true) != 11) throw new InvalidOperationException("The current normal R must retain its final frame after a settings change.");
            VerifyUltimateReducedMotion(); reduceMotion = false;
            if (effects.UltimateFrame(false) != 7) throw new InvalidOperationException("Disabling reduced motion must not truncate or reinterpret an in-flight static R timeline.");
            AssertEffectsAnimationUnchanged();
        }
        public void VerifyUltimatePauseStart()
        {
            effects.ResumeVerification(); settings = true; SyncCombatPresentation(); pausedVerificationFrame = effects.UltimateFrame(false);
        }
        public void VerifyUltimateGarnetFirst()
        {
            PrepareEffectsVerification("garnet"); var c = Engine.State.combat; var card = GameDatabase.Card("garnet_r");
            c.enemyHp = 60; c.enemyMaxHp = 100; c.energy = card.cost;
            c.hand = new List<string> { card.id }; c.drawPile.Clear(); c.discardPile.Clear(); c.exhaustPile.Clear();
            Engine.State.deck = new List<string> { card.id };
            if (!Engine.PlayCard(0) || c.energy != 0 || !c.hand.Contains(card.id)) throw new InvalidOperationException("Garnet first R must return a free follow-up at zero energy.");
            ConsumeCombatActions(); ultimateVerificationDuration = UltimateAnimations.For(card).Duration;
            FreezeUltimateVerification(7); CaptureEffectsVerificationState();
        }
        public void VerifyUltimateGarnetRecast()
        {
            effects.Clear(); var c = Engine.State.combat; c.enemyHp = 20;
            if (c.energy != 0 || !Engine.PlayCard(c.hand.IndexOf("garnet_r")) || Engine.State.stage != RunStage.Rewards)
                throw new InvalidOperationException("Garnet must execute with its real zero-energy follow-up.");
            ConsumeCombatActions(); FreezeUltimateVerification(7);
            if (effects.UltimateSubject != "garnet" || !UltimateResultHold) throw new InvalidOperationException("Garnet's free execution must replay its approved R animation before rewards.");
            CaptureEffectsVerificationState();
        }
        public void VerifyUltimatePauseFinish()
        {
            if (effects.UltimateFrame(false) != pausedVerificationFrame) throw new InvalidOperationException("A settings overlay must pause R playback.");
            settings = false; SyncCombatPresentation();
            if (effects.UltimateFrame(false) != pausedVerificationFrame) throw new InvalidOperationException("Resuming must preserve the paused frame.");
        }
        public void VerifyUltimateReset()
        {
            lobby = true; SyncCombatPresentation();
            if (effects.UltimateBusy || effects.Casting || effects.UltimateSubject != null) throw new InvalidOperationException("Returning to the lobby must clear R playback.");
        }
    }
}
