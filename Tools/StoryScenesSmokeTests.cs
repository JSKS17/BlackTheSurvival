#if LUMIA_TEST_RUNNER
using System;
using System.Collections.Generic;
using System.IO;
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

    private static void StoryAccountHistoryTransactions()
    {
        var profile = new ResearchProfile();
        Check(StoryScenes.RememberStories(profile, null) && profile.storyHistoryVersion == 1 && profile.seenStories.Count == 0,
            "fresh account initializes viewing history before its first clear without marking unseen stories");
        var first = new GameEngine(912001).State; first.story = StoryScenes.NewRun(first, profile);
        Check(StoryScenes.Pending(first)?.id == StoryScenes.Prologue, "fresh account sees its first prologue");
        Check(StoryScenes.Advance(first) && first.story.frameIndex == 1, "unfinished first prologue can advance");
        Check(!StoryScenes.RememberStories(profile, first) && profile.seenStories.Count == 0,
            "saving an unfinished cutscene does not mark it as viewed");
        var interrupted = (RunState)Clone(first);
        StoryScenes.ApplyRemembered(StoryScenes.Normalize(interrupted), profile.seenStories);
        Check(StoryScenes.Pending(interrupted)?.id == StoryScenes.Prologue && interrupted.story.frameIndex == 1,
            "unfinished cutscene retains its saved frame when applying account history");
        Check(StoryScenes.Skip(first) && StoryScenes.RememberStories(profile, first)
            && profile.seenStories.SequenceEqual(new[] { StoryScenes.Prologue }), "skipping records prologue viewing account-wide");
        var second = new GameEngine(912002).State; second.story = StoryScenes.NewRun(second, profile);
        Check(StoryScenes.Pending(second) == null && second.story.completedScenes.Contains(StoryScenes.Prologue),
            "subsequent expedition skips the viewed prologue and carries a recoverable history copy");
        profile.normalCleared = true; profile.totalRuns = profile.totalWins = 1;
        first.stage = RunStage.Won;
        Check(StoryScenes.QueueVictory(first, profile) && StoryScenes.Pending(first)?.id == StoryScenes.NormalEnding,
            "first normal ending still appears when research settlement has just recorded the first clear");
        while (StoryScenes.Pending(first) != null) StoryScenes.Advance(first);
        Check(StoryScenes.RememberStories(profile, first) && profile.seenStories.Contains(StoryScenes.NormalEnding),
            "completing every ending frame adds the ending to permanent history");
        second.stage = RunStage.Won;
        string gameBefore = StoryGameplaySnapshot(second);
        Check(!StoryScenes.QueueVictory(second, profile) && StoryScenes.Pending(second) == null
            && second.story.completedScenes.Contains(StoryScenes.NormalEnding), "later normal victories do not replay the ending");
        Check(gameBefore == StoryGameplaySnapshot(second), "viewed-ending suppression changes no gameplay, rewards or random state");
        var hard = new GameEngine(912003, RunMode.Hard).State; hard.story = StoryScenes.NewRun(hard, profile); hard.stage = RunStage.Won;
        Check(StoryScenes.QueueVictory(hard, profile) && StoryScenes.Pending(hard)?.id == StoryScenes.HardEnding,
            "viewing the normal ending does not suppress the first hard ending");
        StoryScenes.Skip(hard); StoryScenes.RememberStories(profile, hard);
        Check(StoryScenes.ReplayAvailable(StoryScenes.Prologue, profile)
            && StoryScenes.ReplayAvailable(StoryScenes.NormalEnding, profile)
            && StoryScenes.ReplayAvailable(StoryScenes.HardEnding, profile), "all viewed stories remain explicitly replayable");

        var replayFirst = new ResearchProfile();
        var unused = new GameEngine(912004).State; unused.story = StoryScenes.NewRun(unused);
        string unusedBefore = StoryGameplaySnapshot(unused);
        Check(StoryScenes.RememberStories(replayFirst, null, new[] { StoryScenes.Prologue })
            && StoryScenes.Pending(unused) != null,
            "explicit replay completion or early exit can remember prologue without advancing the active expedition");
        var afterReplay = new GameEngine(912006).State; afterReplay.story = StoryScenes.NewRun(afterReplay, replayFirst);
        Check(StoryScenes.Pending(afterReplay) == null && unusedBefore == StoryGameplaySnapshot(unused),
            "viewing prologue through replay first suppresses future automatic playback without awarding gameplay progress");

        var legacy = new ResearchProfile { totalRuns = 7, totalWins = 2, normalCleared = true, hardCleared = true };
        Check(StoryScenes.RememberStories(legacy, null) && StoryScenes.All.All(scene => legacy.seenStories.Contains(scene.id)),
            "old completed-mode profile migrates its stories exactly once");
        var legacyPending = new ResearchProfile { totalRuns = 1, totalWins = 1, normalCleared = true };
        var pending = new GameEngine(912007).State; pending.story = StoryScenes.NewRun(pending); StoryScenes.Skip(pending);
        pending.stage = RunStage.Won; StoryScenes.QueueVictory(pending); StoryScenes.Advance(pending);
        Check(StoryScenes.RememberStories(legacyPending, pending) && !legacyPending.seenStories.Contains(StoryScenes.NormalEnding),
            "legacy clear migration excludes a saved unfinished ending");
        StoryScenes.ApplyRemembered(StoryScenes.Normalize(pending), legacyPending.seenStories);
        Check(StoryScenes.Pending(pending)?.id == StoryScenes.NormalEnding && pending.story.frameIndex == 1,
            "legacy unfinished ending continues at its saved frame");
        foreach (RunMode mode in new[] { RunMode.Normal, RunMode.Hard })
        {
            var justCleared = new ResearchProfile { totalRuns = 1, totalWins = 1, normalCleared = true, hardCleared = mode == RunMode.Hard };
            var beforeQueue = new GameEngine(912011, mode).State;
            beforeQueue.story = StoryScenes.NewRun(beforeQueue); StoryScenes.Skip(beforeQueue); beforeQueue.stage = RunStage.Won;
            string firstEnding = StoryScenes.VictoryScene(beforeQueue);
            Check(StoryScenes.RememberStories(justCleared, beforeQueue) && !justCleared.seenStories.Contains(firstEnding),
                "legacy save immediately after clear settlement does not imply its unqueued ending was viewed: " + mode);
            Check(StoryScenes.QueueVictory(beforeQueue, justCleared) && StoryScenes.Pending(beforeQueue)?.id == firstEnding,
                "legacy terminal save still queues its unseen first ending after migration: " + mode);
        }
        var malformed = new ResearchProfile { seenStories = new List<string> { null, "missing", StoryScenes.Prologue, StoryScenes.Prologue } };
        ResearchProgression.Normalize(malformed);
        Check(malformed.seenStories.SequenceEqual(new[] { StoryScenes.Prologue }) && malformed.storyHistoryVersion == 0,
            "profile normalization removes invalid and duplicate story IDs without guessing first-clear history");

        string scratchRoot = Path.GetFullPath(Path.Combine("Temp", "StoryHistoryTests"));
        string scratch = Path.GetFullPath(Path.Combine(scratchRoot, Guid.NewGuid().ToString("N")));
        Check(scratch.StartsWith(scratchRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase), "story fixtures stay inside their private test folder");
        Directory.CreateDirectory(scratch);
        try
        {
            string path = Path.Combine(scratch, "research.bin"), error;
            var store = new ResearchStore(path, ResearchFixtureEncode, ResearchFixtureDecode);
            var stale = new ResearchStore(path, ResearchFixtureEncode, ResearchFixtureDecode);
            var finished = new GameEngine(912008).State; finished.story = StoryScenes.NewRun(finished); StoryScenes.Skip(finished);
            var withoutContinue = (RunState)Clone(finished);
            Check(store.TryRememberStories(withoutContinue, null, out error) && error == "",
                "completed prior save can be imported before creating a replacement expedition without Continue");
            var reloaded = new ResearchStore(path, ResearchFixtureEncode, ResearchFixtureDecode);
            Check(reloaded.Profile.seenStories.SequenceEqual(new[] { StoryScenes.Prologue }) && reloaded.Profile.totalRuns == 0
                && reloaded.Profile.data == 0 && File.Exists(path + ".bak"), "view history survives process reload without awarding research or runs");
            string committed = File.ReadAllText(path);
            Check(!reloaded.TryRememberStories(withoutContinue, null, out error) && error == "" && File.ReadAllText(path) == committed,
                "repeated view-history merge is idempotent and avoids rewriting the profile");
            Check(stale.TryRememberStories(null, new[] { StoryScenes.NormalEnding }, out error)
                && stale.Profile.seenStories.Count == 2 && stale.Profile.seenStories.Contains(StoryScenes.Prologue),
                "stale second writer reloads existing history before merging a new replay");
            ResearchSettlement settlement;
            Check(store.TrySettle(ResearchStoreRun(912009), out settlement, out error)
                && store.Profile.seenStories.Contains(StoryScenes.NormalEnding) && store.Profile.totalRuns == 1,
                "research settlement preserves stories written by a different store");
            int balance = store.Profile.data;
            Check(stale.TryRememberStories(null, new[] { StoryScenes.HardEnding }, out error)
                && stale.Profile.totalRuns == 1 && stale.Profile.data == balance && stale.Profile.seenStories.Count == 3,
                "story writes preserve research data and settlements committed by another writer");

            string faultPath = Path.Combine(scratch, "fault.bin"); int calls = 0;
            var fault = new ResearchStore(faultPath, value => { if (++calls == 2) throw new IOException("injected story persistence failure"); return ResearchFixtureEncode(value); }, ResearchFixtureDecode);
            Check(!fault.TryRememberStories(finished, null, out error) && error != "" && !File.Exists(faultPath)
                && fault.Profile.seenStories.Count == 0 && finished.story.completedScenes.Contains(StoryScenes.Prologue),
                "failed account write preserves the completed ID on its expedition and does not partially commit history");
            var replacement = new GameEngine(912010).State; replacement.story = StoryScenes.NewRun(replacement);
            StoryScenes.ApplyRemembered(replacement.story, finished.story.completedScenes);
            var replacementReloaded = (RunState)Clone(replacement);
            Check(StoryScenes.Pending(replacementReloaded) == null && fault.TryRememberStories(replacementReloaded, null, out error)
                && error == "" && fault.Profile.seenStories.Contains(StoryScenes.Prologue),
                "replacement expedition carries uncommitted viewing history and retries it after reload");
            using (var held = new FileStream(faultPath + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                string beforeLock = File.ReadAllText(faultPath);
                Check(!fault.TryRememberStories(null, new[] { StoryScenes.HardEnding }, out error) && error != ""
                    && File.ReadAllText(faultPath) == beforeLock, "concurrent account lock defers story writes without damaging the committed file");
            }
            File.WriteAllText(faultPath, ResearchFixtureEncode(new ResearchProfile { version = 2 }));
            string futureBytes = File.ReadAllText(faultPath);
            Check(!fault.TryRememberStories(finished, null, out error) && error != "" && File.ReadAllText(faultPath) == futureBytes,
                "future profile schema remains untouched by history migration");
        }
        finally
        {
            if (Path.GetFullPath(scratch).StartsWith(scratchRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) Directory.Delete(scratch, true);
        }
    }
}
#endif
