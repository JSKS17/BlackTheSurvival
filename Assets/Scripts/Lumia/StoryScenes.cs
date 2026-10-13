using System;
using System.Collections.Generic;
using System.Linq;

namespace Lumia
{
    public sealed class StoryFrame
    {
        public readonly string image, narration, speaker, dialogue;
        public StoryFrame(string image, string narration, string dialogue = "", string speaker = "니아")
        {
            this.image = image; this.narration = narration ?? "";
            this.dialogue = dialogue ?? ""; this.speaker = string.IsNullOrEmpty(dialogue) ? "" : speaker;
        }
    }

    public sealed class StorySceneDef
    {
        public readonly string id, title;
        public readonly StoryFrame[] frames;
        public StorySceneDef(string id, string title, params StoryFrame[] frames)
        { this.id = id; this.title = title; this.frames = frames; }
    }

    /// <summary>Run-local progress. Replays are transient UI and never write to this record.</summary>
    [Serializable] public sealed class StoryRunProgress
    {
        public int version = 1;
        public string runId, activeSceneId;
        public int frameIndex;
        public List<string> completedScenes = new List<string>();
    }

    /// <summary>Story presentation only: no gameplay RNG, rewards, research settlement or mode unlocks.</summary>
    public static class StoryScenes
    {
        public const string Prologue = "prologue", NormalEnding = "normal", HardEnding = "hard";
        public static readonly StorySceneDef[] All =
        {
            new StorySceneDef(Prologue, "프롤로그 / 니아의 게임",
                new StoryFrame("prologue_1", "루미아 섬에서 PC게임 금단증상을 겪고 있는 니아.."),
                new StoryFrame("prologue_2", "슬더스를 하고 싶은 마음에 VF 능력이 폭주해 루미아 섬을 변형시킨다!"),
                new StoryFrame("prologue_3", "평소처럼 실험을 진행하다가 말려든 연구원 하나.."),
                new StoryFrame("prologue_4", "과연 하나는 끝까지 나아가 루미아 섬을 돌려놓을 수 있을 것인가?")),
            new StorySceneDef(NormalEnding, "노말 탈출 / 여기서 끝이 아니다!",
                new StoryFrame("normal_1", "니아를 쓰러트리고 루미아 섬을 돌려놓은 하나..?"),
                new StoryFrame("normal_2", "그런데 그 때, 니아가 나타났다!", "여기서 끝이 아니다!"),
                new StoryFrame("normal_3", "", "방금 네가 쓰러트린 건 우리 니아들 중 최약체.."),
                new StoryFrame("normal_4", "과연 하나는 다시 루미아 섬을 돌려놓을 수 있을 것인가?")),
            new StorySceneDef(HardEnding, "하드 탈출 / 니아의 꿈",
                new StoryFrame("hard_1", "이번에야말로 니아를 쓰러트리고 루미아 섬을 돌려놓은 하나.."),
                new StoryFrame("hard_2", "라는 꿈을 꾼 니아..", "아, 망할 꿈."),
                new StoryFrame("hard_3", "재밌는 꿈을 꾼 니아가 다시 VF 능력을 사용한다!"),
                new StoryFrame("hard_4", "", "이번엔 ‘무한 모드’에 도전해보는 게 어때?"))
        };

        public static StorySceneDef Find(string id) { return All.FirstOrDefault(scene => scene.id == id); }
        public static string RunIdentity(RunState state)
        {
            return state == null ? "" : !string.IsNullOrEmpty(state.research?.runId)
                ? state.research.runId : "seed:" + state.seed;
        }
        public static string VictoryScene(RunState state)
        {
            if (state == null || state.stage != RunStage.Won || state.endlessRetired || RunModes.ModeFor(state) == RunMode.Endless) return null;
            return RunModes.ModeFor(state) == RunMode.Hard ? HardEnding : NormalEnding;
        }
        public static StoryRunProgress NewRun(RunState state)
        {
            return new StoryRunProgress { runId = RunIdentity(state), activeSceneId = Prologue, frameIndex = 0 };
        }

        // Legacy saves continue at their existing game state. An already finished legacy run
        // is not treated as a new victory, and opening its save cannot replay an ending forever.
        public static StoryRunProgress Normalize(RunState state)
        {
            if (state == null) return null;
            string identity = RunIdentity(state);
            var progress = state.story;
            if (progress == null || !string.IsNullOrEmpty(progress.runId) && progress.runId != identity)
            {
                progress = new StoryRunProgress { runId = identity };
                progress.completedScenes.Add(Prologue);
                string ending = VictoryScene(state);
                if (ending != null) progress.completedScenes.Add(ending);
                state.story = progress;
            }
            progress.version = 1; progress.runId = identity;
            progress.completedScenes = (progress.completedScenes ?? new List<string>()).Where(id => Find(id) != null).Distinct().ToList();
            var active = Find(progress.activeSceneId);
            if (active == null || progress.completedScenes.Contains(active.id)) ClearActive(progress);
            else if (active.id == Prologue && state.stage != RunStage.Preparation)
            {
                Complete(progress, active.id);
            }
            else if (active.id != Prologue && VictoryScene(state) != active.id) ClearActive(progress);
            else progress.frameIndex = Math.Max(0, Math.Min(active.frames.Length - 1, progress.frameIndex));
            return progress;
        }

        public static StorySceneDef Pending(RunState state)
        {
            var progress = Normalize(state);
            return progress == null ? null : Find(progress.activeSceneId);
        }
        public static bool QueueVictory(RunState state)
        {
            var progress = Normalize(state); string ending = VictoryScene(state);
            if (progress == null || ending == null || !string.IsNullOrEmpty(progress.activeSceneId) || progress.completedScenes.Contains(ending)) return false;
            progress.activeSceneId = ending; progress.frameIndex = 0; return true;
        }
        public static bool Back(RunState state)
        {
            var progress = Normalize(state);
            if (progress == null || Find(progress.activeSceneId) == null || progress.frameIndex <= 0) return false;
            progress.frameIndex--; return true;
        }
        public static bool Advance(RunState state)
        {
            var progress = Normalize(state); var active = progress == null ? null : Find(progress.activeSceneId);
            if (active == null) return false;
            if (progress.frameIndex < active.frames.Length - 1) progress.frameIndex++;
            else Complete(progress, active.id);
            return true;
        }
        public static bool Skip(RunState state)
        {
            var progress = Normalize(state); var active = progress == null ? null : Find(progress.activeSceneId);
            if (active == null) return false;
            Complete(progress, active.id); return true;
        }
        static void Complete(StoryRunProgress progress, string id)
        {
            if (!progress.completedScenes.Contains(id)) progress.completedScenes.Add(id);
            ClearActive(progress);
        }
        static void ClearActive(StoryRunProgress progress) { progress.activeSceneId = null; progress.frameIndex = 0; }
        public static bool ReplayAvailable(string sceneId, ResearchProfile profile)
        {
            if (sceneId == Prologue) return true;
            if (sceneId == NormalEnding) return profile != null && (profile.normalCleared || profile.hardCleared);
            return sceneId == HardEnding && profile != null && profile.hardCleared;
        }
    }
}
