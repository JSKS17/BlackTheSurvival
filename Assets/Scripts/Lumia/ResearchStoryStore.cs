using System;
using System.Collections.Generic;

namespace Lumia
{
    public sealed partial class ResearchStore
    {
        // Use the same reload/lock/atomic replacement as report and settlement writes,
        // so an older UI snapshot cannot discard research earned by another writer.
        public bool TryRememberStories(RunState previousRun, IEnumerable<string> additional, out string error)
        {
            error = "";
            try
            {
                using (Lock())
                {
                    Reload();
                    if (IsReadOnly) { error = LoadError; return false; }
                    var candidate = Parse(serialize(Profile));
                    if (!StoryScenes.RememberStories(candidate, previousRun, additional)) return false;
                    Persist(candidate);
                    return true;
                }
            }
            catch (Exception e)
            {
                error = "스토리 감상 기록을 저장하지 못했습니다. 원정 기록에서 다시 저장할 수 있습니다. " + e.Message;
                return false;
            }
        }
    }
}
