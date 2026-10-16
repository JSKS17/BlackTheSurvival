using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Lumia
{
    public sealed partial class LumiaGame
    {
        readonly HashSet<string> storyHistorySession = new HashSet<string>();
        ResearchStore storyHistoryStore;
        string storyHistorySavePath = "", storyHistoryError = "";
        bool storyHistoryLoaded;

        void EnsureStoryHistorySession()
        {
            // Account switches and verification fixtures reopen their own store.
            if (ReferenceEquals(storyHistoryStore, researchStore) && storyHistorySavePath == savePath) return;
            storyHistorySession.Clear(); storyHistoryError = ""; storyHistoryLoaded = false;
            storyHistoryStore = researchStore; storyHistorySavePath = savePath;
        }

        void RememberStoryHistory(RunState source, string sceneId = null)
        {
            EnsureStoryHistorySession();
            storyHistoryLoaded = true;
            foreach (string id in StoryScenes.RememberedScenes(researchStore?.Profile, source)) storyHistorySession.Add(id);
            if (StoryScenes.Find(sceneId) != null) storyHistorySession.Add(sceneId);
            if (researchStore == null || BrowserAccountFrozen) return;
            string error;
            researchStore.TryRememberStories(source, storyHistorySession, out error);
            if (!string.IsNullOrEmpty(error))
            {
                if (storyHistoryError != error) { Notify(error); Debug.LogWarning(error); }
                storyHistoryError = error;
            }
            else storyHistoryError = "";
        }

        void PreserveStoryBeforeNewRun()
        {
            RunState previous = Engine?.State;
            if (previous == null && (File.Exists(savePath) || File.Exists(savePath + ".bak")))
            {
                try { bool recovered; previous = ReadRunSave(out recovered); }
                catch (Exception error) { Debug.LogWarning("이전 스토리 감상 기록 확인: " + error.Message); }
            }
            RememberStoryHistory(previous);
        }

        void RememberFinishedStory()
        {
            // Save the completed ID on the expedition first. If the account write
            // fails, reloading (or replacing this run) can safely retry the merge.
            WriteRunSave();
            RememberStoryHistory(Engine?.State);
        }

        void RememberReplayStory()
        {
            RememberStoryHistory(Engine?.State, storyReplayId);
            if (Engine != null && !TutorialActive)
            {
                StoryScenes.ApplyRemembered(StoryScenes.Normalize(Engine.State), storyHistorySession);
                WriteRunSave();
            }
        }
    }
}
