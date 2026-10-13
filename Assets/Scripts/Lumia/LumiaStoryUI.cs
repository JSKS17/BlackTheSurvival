using System;
using System.Collections.Generic;
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
            var image = StoryImage(frame.image);
            if (image) Tex(new Rect(canvas.x + 2, canvas.y + 2, canvas.width - 4, canvas.height - 4), image, ScaleMode.ScaleToFit);
            else DrawStoryFallback(canvas, scene.id, index);
            PrepareStoryStyles();
            if (!string.IsNullOrEmpty(frame.narration))
            {
                float height = storyNarrationStyle.CalcHeight(new GUIContent(frame.narration), 1120) + 26;
                Rect narration = new Rect(62, 102, 1156, height);
                Fill(narration, new Color(.025f, .045f, .075f, .91f));
                GUI.Label(new Rect(narration.x + 18, narration.y + 12, narration.width - 36, narration.height - 24), frame.narration, storyNarrationStyle);
            }
            if (!string.IsNullOrEmpty(frame.dialogue)) DrawStoryDialogue(frame, canvas);
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
                var preview = StoryImage(scene.frames[0].image);
                if (preview) Tex(new Rect(card.x + 12, card.y + 12, 352, 198), preview, ScaleMode.ScaleToFit);
                else Tex(new Rect(card.x + 12, card.y + 12, 352, 198), PixelArt.Background(), ScaleMode.ScaleAndCrop);
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
        void DrawStoryFallback(Rect rect, string scene, int index)
        {
            Tex(new Rect(rect.x + 2, rect.y + 2, rect.width - 4, rect.height - 4), PixelArt.Background(), ScaleMode.ScaleAndCrop);
            Fill(new Rect(rect.x + 2, rect.y + 2, rect.width - 4, rect.height - 4), new Color(.02f, .035f, .06f, .32f));
            if (scene == StoryScenes.Prologue || index == 0)
                Tex(new Rect(230, 218, 275, 320), PixelArt.Portrait("hana"), ScaleMode.ScaleToFit);
            Tex(new Rect(scene == StoryScenes.HardEnding ? 540 : 805, 208, 275, 330), PixelArt.Portrait("nia"), ScaleMode.ScaleToFit);
        }
        void PrepareStoryStyles()
        {
            if (storyNarrationStyle != null) return;
            storyNarrationStyle = new GUIStyle(wrapped) { fontSize = 23, alignment = TextAnchor.MiddleLeft, wordWrap = true };
            storyNarrationStyle.normal.textColor = Text;
            storyDialogueStyle = new GUIStyle(wrapped) { fontSize = 23, alignment = TextAnchor.MiddleLeft, wordWrap = true };
            storyDialogueStyle.normal.textColor = Ink;
        }
        void DrawStoryDialogue(StoryFrame frame, Rect canvas)
        {
            string dialogue = StoryDialogueText(frame);
            float height = storyDialogueStyle.CalcHeight(new GUIContent(dialogue), 472) + 64;
            Rect bubble = new Rect(canvas.xMax - 546, canvas.yMax - height - 26, 520, height);
            Box(bubble, C("f4eff4"), Pink);
            Txt(new Rect(bubble.x + 20, bubble.y + 13, bubble.width - 40, 24), frame.speaker, 15, C("88516f"));
            GUI.Label(new Rect(bubble.x + 24, bubble.y + 43, bubble.width - 48, height - 56), dialogue, storyDialogueStyle);
            // A pixel-stepped tail stays inside the image, separate from the navigation bar.
            Fill(new Rect(bubble.x + 40, bubble.yMax - 1, 24, 10), Pink);
            Fill(new Rect(bubble.x + 44, bubble.yMax - 1, 16, 8), C("f4eff4"));
            Fill(new Rect(bubble.x + 44, bubble.yMax + 9, 12, 6), Pink);
        }
        static string StoryDialogueText(StoryFrame frame) => frame.image == "normal_3" ? frame.dialogue.Replace("건 우리", "건\n우리") : frame.dialogue;

        public int VerifyStoryLayout()
        {
            if (!initialized) throw new InvalidOperationException("Story layout audit needs initialized native GUI styles.");
            PrepareStoryStyles(); int measurements = 0, assets = 0;
            foreach (var scene in StoryScenes.All)
            foreach (var frame in scene.frames)
            {
                var image = StoryImage(frame.image);
                if (!image) throw new InvalidOperationException("Story resource is missing: Lumia/Story/" + frame.image);
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
            if (!PresentationVerificationEnabled) return;
            CloseStoryLayers(); CloseStoryReplay(); lobby = true;
            if (id == "menu") { storyReplayMenu = true; return; }
            var scene = StoryScenes.Find(id);
            if (scene == null || frame < 0 || frame >= scene.frames.Length) throw new ArgumentException("Unknown story verification frame: " + id + " / " + frame);
            storyReplaying = true; storyReplayId = id; storyReplayFrame = frame;
        }
    }
}
