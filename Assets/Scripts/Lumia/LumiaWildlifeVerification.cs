using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;

namespace Lumia
{
    /// <summary>Opt-in development-player coverage for wildlife art, previews and actual enemy-only skills.</summary>
    [DefaultExecutionOrder(10020)]
    public sealed class LumiaWildlifeVerification : MonoBehaviour
    {
        [Serializable] public sealed class Report
        {
            public string result = "RUNNING", version, failure;
            public bool isolatedSave, personalSavesUnchanged, animalSkillsNotPlayable, pointFilteredPortraits;
            public bool normalAndMutantStats, actualBoarCharge, actualBearSlam, interruptedCharge, readableSkillDetails;
            public bool stableMutationAfterReload, scoutedMutationName, renderDoesNotChangeState;
            public bool dedicatedMutantPortraits, distinctMutantLogicalPixels, mutantPortraitsUse80Grid;
            public bool stableMutationPortraitAfterReload, ordinaryPortraitIdentityPreserved;
            public int screenshots;
            public List<AnimalRecord> animals = new List<AnimalRecord>();
            public List<CaptureRecord> captures = new List<CaptureRecord>();
        }
        [Serializable] public sealed class AnimalRecord
        {
            public string animal, name, portraitResource;
            public bool mutant;
            public int act, level, health, intentDamage, objectDropChance, logicalWidth, logicalHeight;
            public int sourceWidth, sourceHeight, normalLogicalWidth, normalLogicalHeight, comparedLogicalPixels, differentLogicalPixels;
            public int[] alpha80Bounds;
            public int battleRectWidth = 218, battleRectHeight = 216;
        }
        [Serializable] public sealed class CaptureRecord
        {
            public string file;
            public int width, height, nonBlackPixels, colorVariants;
        }

        readonly Report report = new Report();
        string output;
        bool failed;
        Dictionary<string, string> personal;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (Debug.isDebugBuild && Array.IndexOf(Environment.GetCommandLineArgs(), "-bts-wildlife-verify") >= 0)
                new GameObject("BTS wildlife verification").AddComponent<LumiaWildlifeVerification>();
        }

        IEnumerator Start()
        {
            output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "WildlifeVerification"));
            Directory.CreateDirectory(output); personal = PersonalFiles(); SaveReport();
            var runners = new Stack<IEnumerator>(); runners.Push(Verify());
            while (!failed && runners.Count > 0)
            {
                object next = null; bool more = false;
                try
                {
                    var runner = runners.Peek(); more = runner.MoveNext();
                    if (more) next = runner.Current; else runners.Pop();
                }
                catch (Exception exception)
                {
                    failed = true; report.result = "FAIL"; report.failure = exception.ToString(); SaveReport();
                    Debug.LogError("BTS WILDLIFE FAIL " + exception);
                }
                if (failed) break;
                if (!more) continue;
                if (next is IEnumerator && !(next is CustomYieldInstruction)) { runners.Push((IEnumerator)next); continue; }
                yield return next;
            }
            if (failed) Application.Quit(1);
        }

        IEnumerator Verify()
        {
            Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
            yield return null; yield return new WaitForSecondsRealtime(.3f);
            var game = LumiaGame.Instance;
            Require(game && game.WildlifeVerificationEnabled, "Native wildlife runner requires an isolated development flag.");
            Require(Screen.width == 1280 && Screen.height == 720, "Native wildlife capture requires a 1280 x 720 game window.");
            game.GuardWildlifeVerification(); report.isolatedSave = true; report.version = Application.version;
            foreach (string id in new[] { WildlifeMechanics.BoarCharge, WildlifeMechanics.BearSlam })
            {
                Require(GameDatabase.Card(id) == null && !GameDatabase.Cards.Any(card => card.id == id), "Animal-only skills leaked into playable cards: " + id);
                Require(WildlifeMechanics.Card(id) != null, "Missing animal-only skill definition: " + id);
            }
            report.animalSkillsNotPlayable = true;
            foreach (string animal in new[] { "chicken", "wolf", "boar", "bear", "dog" })
            {
                int normalHealth = 0, normalDamage = 0;
                var ordinary = PixelArt.Portrait(animal);
                Require(ReferenceEquals(ordinary, PixelArt.WildlifePortrait(animal, false)), "Ordinary wildlife must keep its existing portrait: " + animal);
                var normalPixels = GridPixels(ordinary);
                foreach (bool mutant in new[] { false, true })
                {
                    game.PrepareWildlifeVerification(animal, mutant, 2, 1);
                    var c = game.Engine.State.combat;
                    var portrait = PixelArt.WildlifePortrait(c.animal, c.mutantAnimal);
                    Require(portrait && portrait.filterMode == FilterMode.Point && portrait.mipmapCount == 1, "Wildlife portrait must remain point filtered: " + animal);
                    var pixels = GridPixels(portrait);
                    string resource = "Lumia/Wildlife/" + (mutant ? "mutant_" : "") + animal;
                    var authored = Resources.Load<Texture2D>(resource);
                    int different = 0;
                    if (mutant)
                    {
                        Require(authored, "Authored mutant portrait missing: " + animal);
                        Require(authored.filterMode == FilterMode.Point && authored.mipmapCount == 1, "Mutant source must use Point without mipmaps: " + animal);
                        Require(!ReferenceEquals(portrait, ordinary), "Mutant portrait silently fell back to ordinary wildlife: " + animal);
                        Require(portrait.width == 80 && portrait.height == 80, "Mutant portrait must keep the 80 x 80 logical grid: " + animal);
                        if (animal != "dog") Require(portrait.width == ordinary.width && portrait.height == ordinary.height, "Wildlife variant changed its logical display dimensions: " + animal);
                        for (int pixel = 0; pixel < pixels.Length; pixel++)
                        {
                            var a = pixels[pixel]; var b = normalPixels[pixel];
                            if ((a.a >= 32 || b.a >= 32) && Math.Abs(a.r - b.r) + Math.Abs(a.g - b.g) + Math.Abs(a.b - b.b) + Math.Abs(a.a - b.a) > 32) different++;
                        }
                        Require(different > 64, "Mutation must remain visibly different on the actual coarse grid: " + animal);
                    }
                    else Require(ReferenceEquals(portrait, ordinary), "Normal wildlife rendering changed: " + animal);
                    if (animal != "dog")
                    {
                        Require(Resources.Load<Texture2D>("Lumia/Wildlife/" + animal), "Authored independent animal portrait missing: " + animal);
                        Require(portrait.height <= 80, "Wildlife must use the game's coarse pixel grid: " + animal);
                    }
                    string name = c.enemyName;
                    Require(name.Contains(WildlifeMechanics.Name(animal)) && name.StartsWith("변이 ") == mutant, "Animal and mutation name must match the actual spawn.");
                    int intent = c.intentDamage;
                    if (!mutant) { normalHealth = c.enemyMaxHp; normalDamage = intent; }
                    else
                    {
                        Require(c.enemyMaxHp == WildlifeMechanics.ScaleHealth(normalHealth, true), "Mutant health must increase exactly once: " + animal);
                        Require(intent > normalDamage, "Mutant intent must include its attack increase: " + animal);
                    }
                    var before = JsonUtility.ToJson(game.Engine.State);
                    var reload = new GameEngine(JsonUtility.FromJson<RunState>(before));
                    Require(reload.State.combat.mutantAnimal == mutant && reload.State.combat.enemyMaxHp == c.enemyMaxHp,
                        "Reload must preserve the spawned variant and scaled health.");
                    Require(reload.IsMutantWildlife(reload.State.map.Single(node => node.id == reload.State.activeNodeId)) == mutant,
                        "Saved map and combat variants must agree.");
                    var reloadedPortrait = PixelArt.WildlifePortrait(reload.State.combat.animal, reload.State.combat.mutantAnimal);
                    Require(ReferenceEquals(reloadedPortrait, portrait), "Reload selected the wrong animal variant texture: " + animal);
                    report.animals.Add(new AnimalRecord { animal = animal, name = name, mutant = mutant, act = 2,
                        level = c.enemyLevel, health = c.enemyMaxHp, intentDamage = intent,
                        objectDropChance = WildlifeMechanics.ObjectDropChance(animal, mutant), logicalWidth = portrait.width, logicalHeight = portrait.height,
                        portraitResource = authored ? resource : "Lumia/CharacterAtlas (unchanged ordinary dog)",
                        sourceWidth = authored ? authored.width : ordinary.width, sourceHeight = authored ? authored.height : ordinary.height,
                        normalLogicalWidth = ordinary.width, normalLogicalHeight = ordinary.height,
                        comparedLogicalPixels = pixels.Length, differentLogicalPixels = different, alpha80Bounds = AlphaBounds(pixels) });
                    yield return Capture((mutant ? "mutant_" : "normal_") + animal);
                    game.AssertWildlifeVerificationUnchanged();
                }
            }
            report.normalAndMutantStats = report.stableMutationAfterReload = report.pointFilteredPortraits = true;
            report.dedicatedMutantPortraits = report.distinctMutantLogicalPixels = report.mutantPortraitsUse80Grid = true;
            report.stableMutationPortraitAfterReload = report.ordinaryPortraitIdentityPreserved = true;

            foreach (string animal in new[] { "boar", "bear" })
            {
                int turn = animal == "boar" ? 2 : 3;
                string id = animal == "boar" ? WildlifeMechanics.BoarCharge : WildlifeMechanics.BearSlam;
                game.PrepareWildlifeVerification(animal, true, 3, turn);
                Require(game.Engine.State.combat.enemyPlan.Contains(id), "Actual animal planning must advertise its special turn: " + animal);
                yield return Capture(animal + "_special_preview");
                game.ShowWildlifeVerificationDetail(id);
                yield return Capture(animal + "_special_detail");
                game.CloseWildlifeVerificationDetail();
                game.PlayWildlifeVerificationSkill(false);
                Require(game.Engine.State.combat.enemyTurn && game.Engine.CombatActions.Count == 0, "The native presentation must consume the actual enemy action once.");
                yield return Capture(animal + "_special_cast");
                game.AssertWildlifeVerificationUnchanged();
                if (animal == "boar") report.actualBoarCharge = true; else report.actualBearSlam = true;
            }
            report.readableSkillDetails = true;

            game.PrepareWildlifeVerification("boar", true, 3, 2);
            game.PlayWildlifeVerificationSkill(true);
            yield return Capture("boar_interrupted");
            game.AssertWildlifeVerificationUnchanged(); report.interruptedCharge = true;

            game.PrepareWildlifeVerification("wolf", true, 3, 1);
            game.ShowWildlifeVerificationMap();
            yield return Capture("mutant_scouted_map");
            game.AssertWildlifeVerificationUnchanged(); report.scoutedMutationName = true;
            Require(personal.OrderBy(pair => pair.Key).SequenceEqual(PersonalFiles().OrderBy(pair => pair.Key)), "Personal saves or research were modified by wildlife verification.");
            report.personalSavesUnchanged = report.renderDoesNotChangeState = true;
            game.FinishWildlifeVerification(); report.result = "PASS"; SaveReport();
            Debug.Log("BTS WILDLIFE VERIFICATION PASS / animals=" + report.animals.Count + " / screenshots=" + report.screenshots);
            Application.Quit(0);
        }

        static Color32[] GridPixels(Texture2D source)
        {
            var target = RenderTexture.GetTemporary(80, 80, 0, RenderTextureFormat.ARGB32);
            var previous = RenderTexture.active;
            Texture2D copy = null;
            try
            {
                target.filterMode = FilterMode.Point;
                Graphics.Blit(source, target); RenderTexture.active = target;
                copy = new Texture2D(80, 80, TextureFormat.RGBA32, false);
                copy.ReadPixels(new Rect(0, 0, 80, 80), 0, 0, false); copy.Apply(false, false);
                return copy.GetPixels32();
            }
            finally
            {
                if (copy) Destroy(copy);
                RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target);
            }
        }

        static int[] AlphaBounds(Color32[] pixels)
        {
            int left = 80, right = 0, bottom = 80, top = 0;
            for (int y = 0; y < 80; y++) for (int x = 0; x < 80; x++)
            {
                if (pixels[y * 80 + x].a < 32) continue;
                left = Math.Min(left, x); right = Math.Max(right, x + 1);
                bottom = Math.Min(bottom, y); top = Math.Max(top, y + 1);
            }
            return new[] { left, bottom, right, top };
        }

        IEnumerator Capture(string name)
        {
            // Keep the game window visible: hidden Unity windows may never repaint their backbuffer.
            yield return null; yield return new WaitForSecondsRealtime(.12f); yield return new WaitForEndOfFrame();
            string path = Path.Combine(output, name + ".png");
            if (File.Exists(path)) File.Delete(path);
            ScreenCapture.CaptureScreenshot(path);
            yield return new WaitForSecondsRealtime(.16f);
            Require(File.Exists(path), "Missing native wildlife screenshot: " + name);
            var image = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                Require(ImageConversion.LoadImage(image, File.ReadAllBytes(path)), "Native screenshot did not decode: " + name);
                var pixels = image.GetPixels32(); var colors = new HashSet<int>();
                var record = new CaptureRecord { file = name + ".png", width = image.width, height = image.height };
                foreach (var pixel in pixels)
                {
                    colors.Add(((pixel.r >> 4) << 8) | ((pixel.g >> 4) << 4) | (pixel.b >> 4));
                    if (pixel.r > 8 || pixel.g > 8 || pixel.b > 8) record.nonBlackPixels++;
                }
                record.colorVariants = colors.Count;
                Require(record.nonBlackPixels > pixels.Length / 20 && record.colorVariants > 32, "Native screenshot was black or unrendered: " + name);
                report.captures.Add(record); report.screenshots++;
            }
            finally { Destroy(image); }
            SaveReport();
        }
        void SaveReport() { File.WriteAllText(Path.Combine(output, "coverage.json"), JsonUtility.ToJson(report, true)); }
        internal static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
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
        public bool WildlifeVerificationEnabled { get { return Debug.isDebugBuild && Array.IndexOf(Environment.GetCommandLineArgs(), "-bts-wildlife-verify") >= 0; } }
        string wildlifeVerificationState;

        public void GuardWildlifeVerification()
        {
            LumiaWildlifeVerification.Require(WildlifeVerificationEnabled && Path.GetFileName(savePath) == "lumia-verification.json", "Wildlife verification must use isolated save and research slots.");
        }
        public void PrepareWildlifeVerification(string animal, bool mutant, int act, int turn)
        {
            GuardWildlifeVerification(); CloseStoryReplay(); CloseCombatInformation();
            lobby = settings = catalog = inventory = help = fieldInfo = fieldEnemy = inspectEnemyCard = enemyLoadout = research = false;
            modeSelecting = runMenu = endlessConfirm = tutorialOffer = tutorialQueued = false;
            inspectCard = inspectTrait = inspectGear = craftPending = mapIntel = inspectBoss = "";
            sound = false; reduceMotion = false; toastUntil = 0; retainSelecting = hyperloopSelecting = false;
            handScroll = Vector2.zero; mapAct = -1; mapRow = -99;
            Engine = new GameEngine(102014);
            var main = GameDatabase.Runes.First(rune => rune.main);
            Engine.SetRunes(main.id, GameDatabase.Runes.First(rune => !rune.main && rune.tree == main.tree).id);
            Engine.SelectStartingPassive(Engine.State.passiveOffers[0]);
            foreach (string id in Engine.State.draftOffers.Take(3).ToArray()) Engine.ToggleDraft(id);
            LumiaWildlifeVerification.Require(Engine.BeginJourney(), "Native wildlife fixture must finish actual preparation.");
            var s = Engine.State;
            s.passives.Clear(); s.gear.Clear(); s.mainRune = s.supportRune = "";
            s.hp = s.maxHp = 240; s.act = act; s.row = s.lane = -1;
            var node = Engine.AvailableNodes().First();
            node.kind = ZoneKind.Wildlife; node.assignedAnimal = animal; node.mutantAnimal = mutant; node.wildlifeVariantVersion = 1;
            LumiaWildlifeVerification.Require(Engine.EnterNode(node.lane) && s.stage == RunStage.Combat, "Native fixture must enter a real wildlife node.");
            var c = s.combat;
            LumiaWildlifeVerification.Require(c.animal == animal && c.mutantAnimal == mutant, "Wildlife fixture spawned the wrong variant.");
            while (c.turn < turn)
            {
                LumiaWildlifeVerification.Require(Engine.BeginEndTurn(), "Fixture could not advance the player turn.");
                for (int i = 0; c.enemyTurn && i < 20; i++) Engine.AdvanceEnemyAction();
                LumiaWildlifeVerification.Require(!c.enemyTurn, "Fixture enemy turn did not finish.");
            }
            displayedTurn = c.turn; nextEnemyActionAt = float.MaxValue; nextPlayerActionAt = 0;
            SyncCombatPresentation(); effects.Clear(); Engine.CombatActions.Clear(); SnapshotWildlifeVerification();
        }
        void SnapshotWildlifeVerification()
        {
            string intent = Engine.EnemyIntent;
            wildlifeVerificationState = JsonUtility.ToJson(Engine.State);
        }
        public void AssertWildlifeVerificationUnchanged()
        {
            LumiaWildlifeVerification.Require(wildlifeVerificationState == JsonUtility.ToJson(Engine.State), "Native wildlife rendering changed gameplay state or randomness.");
        }
        public void ShowWildlifeVerificationDetail(string id)
        {
            GuardWildlifeVerification(); inspectCard = id; inspectEnemyCard = true; detailScroll = Vector2.zero;
        }
        public void CloseWildlifeVerificationDetail() { inspectCard = ""; inspectEnemyCard = false; }
        public void PlayWildlifeVerificationSkill(bool interrupt)
        {
            GuardWildlifeVerification(); var c = Engine.State.combat;
            string id = c.enemyPlan.Single(); int hp = Engine.State.hp;
            if (interrupt) StatusMechanics.Add(c.enemyStatuses, "root", id, 1);
            LumiaWildlifeVerification.Require(Engine.BeginEndTurn(), "The real enemy skill route must start at player turn end.");
            LumiaWildlifeVerification.Require(Engine.AdvanceEnemyAction(), "The real enemy skill route did not advance.");
            LumiaWildlifeVerification.Require(Engine.CombatActions.Any(action => action.cardId == id && action.kind == (interrupt ? "wildlife_interrupt" : "wildlife_skill")), "Missing actual enemy-only skill action.");
            if (interrupt) LumiaWildlifeVerification.Require(Engine.State.hp == hp && c.enemyVulnerable > 0, "Interrupted boar charge must cause no damage and enter despair.");
            else LumiaWildlifeVerification.Require(Engine.State.hp < hp, "The actual animal-only skill did not damage its target.");
            ConsumeCombatActions(); effects.FreezeForVerification(.58f); SnapshotWildlifeVerification();
        }
        public void ShowWildlifeVerificationMap()
        {
            GuardWildlifeVerification(); var s = Engine.State;
            var node = s.map.Single(value => value.id == s.activeNodeId);
            s.revealedNodeIds.Add(node.id); node.visited = false;
            LumiaWildlifeVerification.Require(Engine.NodeIntel(node).StartsWith("변이 "), "Security reveal must show the saved mutant variant.");
            s.stage = RunStage.Map; s.combat = null; s.row = s.lane = -1;
            effects.Clear(); mapScroll = Vector2.zero; SnapshotWildlifeVerification();
        }
        public void FinishWildlifeVerification()
        {
            GuardWildlifeVerification(); effects.Clear(); effects.ResumeVerification();
            nextEnemyActionAt = nextPlayerActionAt = 0; lobby = true; Engine = null;
        }
    }
}
