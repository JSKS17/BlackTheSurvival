using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Lumia
{
    public sealed partial class LumiaGame
    {
        bool storyReplayMenu, storyReplaying;
        string storyReplayId;
        int storyReplayFrame;
        readonly Dictionary<string, Texture2D> storyImages = new Dictionary<string, Texture2D>();
        GUIStyle storyNarrationStyle, storyDialogueStyle;
        readonly Dictionary<string, PixelLayerSprite> storyPixelSprites = new Dictionary<string, PixelLayerSprite>();
        StoryCharacterGeometryCatalog storyCharacterGeometry;

        public bool StoryVisible
        {
            get { return storyReplayMenu || storyReplaying || !lobby && Engine != null && StoryScenes.Pending(Engine.State) != null; }
        }

        void InitializeRunStory()
        {
            CloseStoryReplay();
            if (Engine != null) Engine.State.story = StoryScenes.NewRun(Engine.State);
        }
        void RestoreRunStory()
        {
            CloseStoryReplay();
            if (Engine != null) StoryScenes.Normalize(Engine.State);
        }
        void UpdateStoryFlow()
        {
            if (lobby || Engine == null || storyReplaying || storyReplayMenu) return;
            if (StoryScenes.QueueVictory(Engine.State)) WriteRunSave();
        }
        void OpenStoryReplayMenu()
        {
            CloseStoryLayers(); storyReplayMenu = true; storyReplaying = false; storyReplayId = null;
        }
        bool OpenStoryReplay(string id)
        {
            if (StoryScenes.Find(id) == null || !StoryScenes.ReplayAvailable(id, researchStore?.Profile)) return false;
            CloseStoryLayers(); storyReplayMenu = false; storyReplaying = true; storyReplayId = id; storyReplayFrame = 0;
            AudioCue("ui.select"); return true;
        }
        void CloseStoryReplay() { storyReplayMenu = storyReplaying = false; storyReplayId = null; storyReplayFrame = 0; }
        void CloseStoryLayers()
        {
            settings = catalog = inventory = help = fieldInfo = enemyLoadout = research = false;
            modeSelecting = runMenu = endlessConfirm = false;
            inspectCard = inspectTrait = inspectGear = inspectBoss = craftPending = mapIntel = "";
            toastUntil = 0;
        }

        void DrawStoryScene()
        {
            currentLayer = topLayer = 0; GUI.enabled = true;
            if (storyReplayMenu) { DrawStoryReplayMenu(); return; }
            var scene = storyReplaying ? StoryScenes.Find(storyReplayId) : StoryScenes.Pending(Engine?.State);
            if (scene == null) { CloseStoryReplay(); return; }
            int index = storyReplaying ? storyReplayFrame : Engine.State.story.frameIndex;
            index = Math.Max(0, Math.Min(scene.frames.Length - 1, index));
            var frame = scene.frames[index];
            Fill(new Rect(0, 0, W, H), Ink);
            Fill(new Rect(0, 0, W, 4), scene.id == StoryScenes.HardEnding ? Gold : Pink);
            Txt(new Rect(34, 22, 1070, 34), scene.title, 25, Text);
            Txt(new Rect(1100, 26, 145, 25), (index + 1) + " / " + scene.frames.Length, 17, Mint, true);
            Rect canvas = new Rect(32, 82, 1216, 514);
            Box(canvas, Panel, Line);
            // Load the complete set first. A cut never paints its background while actors are still loading.
            PrepareStoryComposition(frame.image);
            DrawStoryComposition(new Rect(canvas.x + 2, canvas.y + 2, canvas.width - 4, canvas.height - 4), frame.image);
            PrepareStoryStyles();
            Rect bubble = StoryBubbleRect(frame, canvas);
            if (!string.IsNullOrEmpty(frame.dialogue)) DrawStoryBubble(bubble);
            if (!string.IsNullOrEmpty(frame.narration))
            {
                float height = storyNarrationStyle.CalcHeight(new GUIContent(frame.narration), 1120) + 26;
                Rect narration = new Rect(62, 102, 1156, height);
                Fill(narration, new Color(.025f, .045f, .075f, .91f));
                GUI.Label(new Rect(narration.x + 18, narration.y + 12, narration.width - 36, narration.height - 24), frame.narration, storyNarrationStyle);
            }
            if (!string.IsNullOrEmpty(frame.dialogue)) DrawStoryDialogueText(frame, bubble);
            Txt(new Rect(34, 607, 1210, 21), storyReplaying ? "스토리 다시보기 · 원정 진행과 보상은 바뀌지 않습니다." : "이전 · 다음으로 컷을 넘길 수 있습니다. 건너뛰어도 원정과 탈출 보상은 그대로 이어집니다.", 12, Muted);
            if (Btn(new Rect(34, 647, 172, 42), storyReplaying ? "다시보기 종료" : "건너뛰기", false, Muted)) { FinishStoryDisplay(); return; }
            if (Btn(new Rect(222, 647, 172, 42), "<  이전", false, Mint, index > 0))
            {
                if (storyReplaying) storyReplayFrame--;
                else if (StoryScenes.Back(Engine.State)) WriteRunSave();
            }
            if (!storyReplaying && Btn(new Rect(410, 647, 230, 42), "저장하고 로비로", false, Muted))
            { WriteRunSave(); lobby = true; return; }
            if (Btn(new Rect(866, 647, 382, 42), index == scene.frames.Length - 1 ? storyReplaying ? "다시보기 완료" : scene.id == StoryScenes.Prologue ? "실험 준비로  >" : "탈출 기록 확인  >" : "다음  >", true, Mint))
            {
                if (storyReplaying)
                {
                    if (index < scene.frames.Length - 1) storyReplayFrame++;
                    else { CloseStoryReplay(); storyReplayMenu = true; }
                }
                else if (StoryScenes.Advance(Engine.State)) WriteRunSave();
            }
        }

        void FinishStoryDisplay()
        {
            if (storyReplaying) { CloseStoryReplay(); storyReplayMenu = true; }
            else if (Engine != null && StoryScenes.Skip(Engine.State)) WriteRunSave();
        }
        void DrawStoryReplayMenu()
        {
            Fill(new Rect(0, 0, W, H), Ink);
            Tex(new Rect(0, 0, W, H), PixelArt.Background(), ScaleMode.ScaleAndCrop);
            Fill(new Rect(0, 0, W, H), new Color(.02f, .04f, .07f, .82f));
            Txt(new Rect(60, 66, 1160, 48), "스토리 다시보기", 32, Pink);
            Txt(new Rect(62, 126, 1156, 24), "프롤로그와 완료한 모드의 탈출 이야기를 다시 볼 수 있습니다.", 16, Muted);
            for (int index = 0; index < StoryScenes.All.Length; index++)
            {
                var scene = StoryScenes.All[index]; bool unlocked = StoryScenes.ReplayAvailable(scene.id, researchStore?.Profile);
                Rect card = new Rect(62 + index * 390, 200, 376, 350);
                Box(card, Panel, unlocked ? Pink : Line);
                PrepareStoryComposition(scene.frames[0].image);
                DrawStoryComposition(new Rect(card.x + 12, card.y + 12, 352, 198), scene.frames[0].image, true);
                Para(new Rect(card.x + 18, card.y + 227, 340, 54), scene.title, 20, unlocked ? Text : Muted);
                Txt(new Rect(card.x + 18, card.y + 290, 340, 23), unlocked ? "4컷 · 다시보기" : scene.id == StoryScenes.NormalEnding ? "노말 모드 탈출 후 해금" : "하드 모드 탈출 후 해금", 13, unlocked ? Mint : Muted);
                if (Hit(card, unlocked)) { OpenStoryReplay(scene.id); return; }
            }
            if (Btn(new Rect(62, 610, 1156, 50), "로비로 돌아가기", true, Mint)) { CloseStoryReplay(); lobby = true; }
        }
        Texture2D StoryImage(string id)
        {
            Texture2D image;
            if (!storyImages.TryGetValue(id, out image))
            {
                image = Resources.Load<Texture2D>("Lumia/Story/" + id);
                if (image) { image.filterMode = FilterMode.Point; image.wrapMode = TextureWrapMode.Clamp; }
                storyImages[id] = image;
            }
            return image;
        }
        void PrepareStoryComposition(string cut)
        {
            var composition = StoryComposition.For(cut);
            if (composition == null) return;
            StoryImage("Backgrounds/" + composition.background);
            foreach (var actor in composition.actors) StoryCharacter(actor.sprite);
        }
        PixelLayerSprite StoryCharacter(string id)
        {
            PixelLayerSprite sprite;
            if (storyPixelSprites.TryGetValue(id, out sprite)) return EnsurePixelLayer(sprite);
            var source = StoryImage("Characters/" + id);
            if (!source) return null;
            if (storyCharacterGeometry == null)
            {
                var json = Resources.Load<TextAsset>("Lumia/Story/Characters/geometry");
                storyCharacterGeometry = json ? JsonUtility.FromJson<StoryCharacterGeometryCatalog>(json.text) : new StoryCharacterGeometryCatalog();
            }
            var geometry = storyCharacterGeometry.characters.FirstOrDefault(value => value.id == id);
            sprite = CreatePixelLayer(source, geometry, StoryComposition.LogicalSpriteSize, "Story pixels / " + id);
            storyPixelSprites[id] = sprite; return sprite;
        }
        void DrawStoryComposition(Rect rect, string cut, bool thumbnail = false)
        {
            var composition = StoryComposition.For(cut);
            if (composition == null) { Fill(rect, Panel); return; }
            var background = StoryImage("Backgrounds/" + composition.background);
            if (background) Tex(rect, background, ScaleMode.ScaleAndCrop); else Tex(rect, PixelArt.BattleBackground(), ScaleMode.ScaleAndCrop);
            GUI.BeginGroup(rect);
            Rect local = new Rect(0, 0, rect.width, rect.height);
            DrawStoryFieldEffect(local, composition.fieldEffect);
            foreach (var actor in composition.actors)
            {
                var sprite = StoryCharacter(actor.sprite); if (sprite == null) continue;
                float factor = thumbnail ? rect.height / 510f : 1;
                float width = sprite.width * actor.pixelScale * factor, height = sprite.height * actor.pixelScale * factor;
                Rect body = new Rect(Mathf.Round(rect.width * actor.centerX - width / 2), Mathf.Round(rect.height * actor.baseline - height), Mathf.Round(width), Mathf.Round(height));
                if (composition.fieldEffect != "dream") Fill(new Rect(body.center.x - width * .28f, body.yMax - 5 * factor, width * .56f, 7 * factor), new Color(.02f,.025f,.045f,.4f));
                Rect uv = sprite.uv; if (actor.flip) { uv.x += uv.width; uv.width = -uv.width; }
                Color old = GUI.color; GUI.color = new Color(1, 1, 1, actor.opacity);
                GUI.DrawTextureWithTexCoords(body, sprite.texture, uv, true); GUI.color = old;
            }
            GUI.EndGroup();
        }
        void DrawStoryFieldEffect(Rect rect, string effect)
        {
            if (effect == "none") return;
            if (effect == "dream") { Fill(rect, new Color(.82f,.72f,.94f,.13f)); return; }
            Color color = effect == "glitch" ? new Color(.43f,.9f,.81f,.65f) : new Color(.89f,.35f,.75f,.65f);
            for (int index = 0; index < 7; index++)
            {
                float x = rect.width * (.12f + index * .125f), y = rect.height * (.36f + (index % 3) * .16f);
                float size = rect.height * (effect == "portal" ? .04f : .025f);
                Fill(new Rect(Mathf.Round(x), Mathf.Round(y), Mathf.Round(size), Mathf.Round(size)), color);
                Fill(new Rect(Mathf.Round(x + size), Mathf.Round(y + size), Mathf.Round(size * 2), Mathf.Round(size * .35f)), color);
            }
        }
        void ReleaseStoryPixelTextures()
        {
            foreach (var sprite in storyPixelSprites.Values) ReleasePixelLayer(sprite);
            storyPixelSprites.Clear();
        }
        void PrepareStoryStyles()
        {
            if (storyNarrationStyle != null) return;
            storyNarrationStyle = new GUIStyle(wrapped) { fontSize = 23, alignment = TextAnchor.MiddleLeft, wordWrap = true };
            storyNarrationStyle.normal.textColor = Text;
            storyDialogueStyle = new GUIStyle(wrapped) { fontSize = 23, alignment = TextAnchor.MiddleLeft, wordWrap = true };
            storyDialogueStyle.normal.textColor = Ink;
        }
        Rect StoryBubbleRect(StoryFrame frame, Rect canvas)
        {
            string dialogue = StoryDialogueText(frame);
            float height = storyDialogueStyle.CalcHeight(new GUIContent(dialogue), 472) + 64;
            return new Rect(canvas.xMax - 546, canvas.yMax - height - 26, 520, height);
        }
        void DrawStoryBubble(Rect bubble)
        {
            Box(bubble, C("f4eff4"), Pink);
            // A pixel-stepped tail stays inside the image, separate from the navigation bar.
            Fill(new Rect(bubble.x + 40, bubble.yMax - 1, 24, 10), Pink);
            Fill(new Rect(bubble.x + 44, bubble.yMax - 1, 16, 8), C("f4eff4"));
            Fill(new Rect(bubble.x + 44, bubble.yMax + 9, 12, 6), Pink);
        }
        void DrawStoryDialogueText(StoryFrame frame, Rect bubble)
        {
            Txt(new Rect(bubble.x + 20, bubble.y + 13, bubble.width - 40, 24), frame.speaker, 15, C("88516f"));
            GUI.Label(new Rect(bubble.x + 24, bubble.y + 43, bubble.width - 48, bubble.height - 56), StoryDialogueText(frame), storyDialogueStyle);
        }
        static string StoryDialogueText(StoryFrame frame) => frame.image == "normal_3" ? frame.dialogue.Replace("건 우리", "건\n우리") : frame.dialogue;

        public int VerifyStoryLayout()
        {
            if (!initialized) throw new InvalidOperationException("Story layout audit needs initialized native GUI styles.");
            PrepareStoryStyles(); int measurements = 0, assets = 0;
            foreach (var scene in StoryScenes.All)
            foreach (var frame in scene.frames)
            {
                var composition = StoryComposition.For(frame.image);
                var image = StoryImage("Backgrounds/" + composition.background);
                if (!image) throw new InvalidOperationException("Story background is missing: " + composition.background);
                if (image.filterMode != FilterMode.Point) throw new InvalidOperationException("Story resource must use point filtering: " + frame.image);
                assets++;
                if (font) font.RequestCharactersInTexture(frame.narration + frame.dialogue, 23, FontStyle.Normal);
                float narration = storyNarrationStyle.CalcHeight(new GUIContent(frame.narration), 1120);
                float dialogue = storyDialogueStyle.CalcHeight(new GUIContent(StoryDialogueText(frame)), 472);
                if (narration > 120 || dialogue > 180) throw new InvalidOperationException("Story text does not fit its native image overlay: " + frame.image);
                measurements += 2;
            }
            Debug.Log("BTS STORY LAYOUT PASS / scenes=3 / frames=12 / assets=" + assets + " / measurements=" + measurements + " / nativeFont=PASS / pointFilter=PASS");
            return measurements;
        }

        public void ShowStoryVerification(string id, int frame)
        {
            if (!PresentationVerificationEnabled && !LayeredStoryVerificationEnabled) return;
            CloseStoryLayers(); CloseStoryReplay(); lobby = true;
            if (id == "menu") { storyReplayMenu = true; return; }
            var scene = StoryScenes.Find(id);
            if (scene == null || frame < 0 || frame >= scene.frames.Length) throw new ArgumentException("Unknown story verification frame: " + id + " / " + frame);
            storyReplaying = true; storyReplayId = id; storyReplayFrame = frame;
        }
    }
}
