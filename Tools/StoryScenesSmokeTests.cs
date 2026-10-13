#if LUMIA_TEST_RUNNER
using System;
using System.Collections.Generic;
using System.Linq;
using Lumia;

public static partial class CoreSmokeTests
{
    private static void StorySceneCatalogAndText()
    {
        Check(StoryScenes.All.Length == 3 && StoryScenes.All.All(scene => scene.frames.Length == 4), "three requested story chapters have four separate frames each");
        var frames = StoryScenes.All.SelectMany(scene => scene.frames).ToArray();
        Check(frames.Length == 12 && frames.Select(frame => frame.image).Distinct().Count() == 12, "all twelve story images have unique predictable resource keys");
        foreach (var scene in StoryScenes.All)
        for (int index = 0; index < scene.frames.Length; index++)
        {
            Check(scene.frames[index].image == scene.id + "_" + (index + 1), "one requested line corresponds to one image frame: " + scene.id);
            Check(!string.IsNullOrEmpty(scene.frames[index].narration) || !string.IsNullOrEmpty(scene.frames[index].dialogue), "story frame has separate readable text: " + scene.frames[index].image);
        }
        var prologue = StoryScenes.Find(StoryScenes.Prologue);
        Check(prologue.frames[0].narration == "루미아 섬에서 PC게임 금단증상을 겪고 있는 니아.."
            && prologue.frames[1].narration == "슬더스를 하고 싶은 마음에 VF 능력이 폭주해 루미아 섬을 변형시킨다!"
            && prologue.frames[2].narration == "평소처럼 실험을 진행하다가 말려든 연구원 하나.."
            && prologue.frames[3].narration == "과연 하나는 끝까지 나아가 루미아 섬을 돌려놓을 수 있을 것인가?", "prologue preserves all four requested lines verbatim");
        var normal = StoryScenes.Find(StoryScenes.NormalEnding);
        Check(normal.frames[0].narration == "니아를 쓰러트리고 루미아 섬을 돌려놓은 하나..?"
            && normal.frames[1].dialogue == "여기서 끝이 아니다!"
            && normal.frames[2].narration == "" && normal.frames[2].dialogue == "방금 네가 쓰러트린 건 우리 니아들 중 최약체.."
            && normal.frames[3].narration == "과연 하나는 다시 루미아 섬을 돌려놓을 수 있을 것인가?", "normal ending preserves the fake ending and Nia's requested lines");
        var hard = StoryScenes.Find(StoryScenes.HardEnding);
        Check(hard.frames[1].narration == "라는 꿈을 꾼 니아.." && hard.frames[1].dialogue == "아, 망할 꿈."
            && hard.frames[2].narration == "재밌는 꿈을 꾼 니아가 다시 VF 능력을 사용한다!"
            && hard.frames[3].narration == "" && hard.frames[3].dialogue == "이번엔 ‘무한 모드’에 도전해보는 게 어때?", "hard ending retains the waking-dream reveal and endless-mode invitation");
        Check(frames.Where(frame => frame.dialogue.Length > 0).All(frame => frame.speaker == "니아")
            && frames.Where(frame => frame.dialogue.Length == 0).All(frame => frame.speaker == ""), "Nia's speech is separate from narration rather than baked into art");
        Check(StoryScenes.Find("missing") == null && StoryScenes.Pending(null) == null, "missing stories and missing run states are safe");
    }

    private static void StoryRunResumeAndLegacy()
    {
        var engine = new GameEngine(873445); var state = engine.State;
        state.story = StoryScenes.NewRun(state);
        int random = state.rngState, draft = state.draftSeed;
        string runId = state.research.runId;
        Check(StoryScenes.Pending(state)?.id == StoryScenes.Prologue && state.story.frameIndex == 0, "a newly initialized run opens its prologue before preparation");
        Check(!StoryScenes.Back(state) && StoryScenes.Advance(state) && StoryScenes.Advance(state) && state.story.frameIndex == 2,
            "prologue navigation has an explicit first-frame boundary");
        var restored = new GameEngine((RunState)Clone(state));
        Check(StoryScenes.Pending(restored.State)?.id == StoryScenes.Prologue && restored.State.story.frameIndex == 2
            && restored.State.story.runId == runId, "saved prologue resumes at its exact frame and run identity");
        Check(StoryScenes.Back(restored.State) && restored.State.story.frameIndex == 1 && state.story.frameIndex == 2,
            "restored story navigation is independent of its source save");
        Check(StoryScenes.Skip(restored.State) && restored.State.stage == RunStage.Preparation
            && restored.State.story.completedScenes.SequenceEqual(new[] { StoryScenes.Prologue }) && StoryScenes.Pending(restored.State) == null,
            "skip completes only the prologue and leaves normal preparation intact");
        restored = new GameEngine((RunState)Clone(restored.State));
        Check(StoryScenes.Pending(restored.State) == null && !StoryScenes.Advance(restored.State) && !StoryScenes.Skip(restored.State),
            "completed prologue cannot reopen or complete twice after save reload");
        Check(state.rngState == random && state.draftSeed == draft && restored.State.research.runId == runId,
            "story navigation consumes no gameplay or draft randomness and preserves research identity");

        var legacy = new GameEngine(873446).State; legacy.story = null;
        Check(StoryScenes.Pending(legacy) == null && legacy.story.completedScenes.Contains(StoryScenes.Prologue), "an existing legacy preparation continues without a forced prologue");
        legacy.stage = RunStage.Won; legacy.story = null;
        Check(!StoryScenes.QueueVictory(legacy) && legacy.story.completedScenes.Contains(StoryScenes.NormalEnding), "an already won legacy save cannot repeatedly create a new ending");
        var corrupt = new GameEngine(873447).State;
        corrupt.story = new StoryRunProgress { runId = corrupt.research.runId, activeSceneId = StoryScenes.Prologue, frameIndex = 99,
            completedScenes = new List<string> { "unknown", StoryScenes.NormalEnding, StoryScenes.NormalEnding } };
        Check(StoryScenes.Pending(corrupt)?.id == StoryScenes.Prologue && corrupt.story.frameIndex == 3
            && corrupt.story.completedScenes.SequenceEqual(new[] { StoryScenes.NormalEnding }), "saved story index and completed IDs are normalized safely");
        corrupt.story.runId = "another-run";
        Check(StoryScenes.Pending(corrupt) == null && corrupt.story.runId == corrupt.research.runId, "story progress from a different run cannot hijack this run");
        corrupt.story.activeSceneId = StoryScenes.HardEnding; corrupt.story.frameIndex = -50;
        Check(StoryScenes.Pending(corrupt) == null && corrupt.stage == RunStage.Preparation, "a malformed premature ending is cleared without changing gameplay stage");
    }

    private static void StoryVictoryAndReplayIsolation()
    {
        foreach (var mode in new[] { RunMode.Normal, RunMode.Hard })
        {
            var state = new GameEngine(662243, mode).State; state.story = StoryScenes.NewRun(state);
            Check(StoryScenes.Skip(state), "finite-mode victory fixture completes its prologue");
            state.stage = RunStage.Won;
            string before = StoryGameplaySnapshot(state);
            string ending = mode == RunMode.Hard ? StoryScenes.HardEnding : StoryScenes.NormalEnding;
            Check(StoryScenes.QueueVictory(state) && StoryScenes.Pending(state)?.id == ending, "only the corresponding finite victory ending is queued: " + mode);
            Check(!StoryScenes.QueueVictory(state), "an already pending ending is not queued twice: " + mode);
            Check(StoryScenes.Advance(state) && StoryScenes.Advance(state), "ending can advance before interruption: " + mode);
            var saved = (RunState)Clone(state); var restored = new GameEngine(saved).State;
            Check(StoryScenes.Pending(restored)?.id == ending && restored.story.frameIndex == 2, "interrupted ending resumes its saved frame: " + mode);
            Check(StoryScenes.Advance(restored) && StoryScenes.Advance(restored) && StoryScenes.Pending(restored) == null
                && restored.story.completedScenes.Contains(ending) && !StoryScenes.QueueVictory(restored), "finishing all ending frames marks this run once: " + mode);
            restored = new GameEngine((RunState)Clone(restored)).State;
            Check(!StoryScenes.QueueVictory(restored) && StoryScenes.Pending(restored) == null, "completed ending stays completed on repeated load: " + mode);
            Check(before == StoryGameplaySnapshot(state), "story playback never changes resources, mode, victory, RNG or research state: " + mode);
        }
        foreach (var mode in new[] { RunMode.Normal, RunMode.Hard, RunMode.Endless })
        {
            var state = new GameEngine(662244, mode).State; state.story = StoryScenes.NewRun(state); StoryScenes.Skip(state);
            state.stage = RunStage.Lost;
            Check(!StoryScenes.QueueVictory(state) && StoryScenes.VictoryScene(state) == null, "defeat has no escape ending: " + mode);
            if (mode == RunMode.Endless)
            {
                state.stage = RunStage.Won;
                Check(!StoryScenes.QueueVictory(state) && StoryScenes.VictoryScene(state) == null, "endless terminal state has no finite ending");
                state.stage = RunStage.Lost; state.endlessRetired = true;
                Check(!StoryScenes.QueueVictory(state), "voluntary endless settlement has no escape ending");
            }
        }
        var profile = new ResearchProfile();
        Check(StoryScenes.ReplayAvailable(StoryScenes.Prologue, profile) && !StoryScenes.ReplayAvailable(StoryScenes.NormalEnding, profile)
            && !StoryScenes.ReplayAvailable(StoryScenes.HardEnding, profile), "only prologue replay is available before any clear");
        profile.normalCleared = true;
        Check(StoryScenes.ReplayAvailable(StoryScenes.NormalEnding, profile) && !StoryScenes.ReplayAvailable(StoryScenes.HardEnding, profile), "normal clear unlocks only its own replay");
        profile.hardCleared = true;
        Check(StoryScenes.ReplayAvailable(StoryScenes.HardEnding, profile) && !StoryScenes.ReplayAvailable("missing", profile), "hard clear unlocks its replay without inventing unknown scenes");
        Check(profile.data == 0 && profile.totalWins == 0 && profile.totalRuns == 0 && profile.settledRunIds.Count == 0,
            "replay availability never awards research, wins or run settlements");
    }

    private static string StoryGameplaySnapshot(RunState state)
    {
        return string.Join("|", state.stage, state.mode, state.rngState, state.draftSeed, state.level, state.xp, state.hp,
            state.credits, state.bossVictories, string.Join(",", state.deck), string.Join(",", state.gear),
            state.research.runId, state.research.settled, state.research.legacyEnded, state.research.settlement?.earned ?? 0,
            state.research.completedNodeIds.Count, state.research.usedSkillIds.Count);
    }
}
#endif
