using System;
using System.IO;

namespace Lumia
{
    // Account research is independent of the replaceable single-run save. The caller
    // supplies serialization so atomic persistence can also be tested without Unity.
    public sealed partial class ResearchStore
    {
        readonly string path;
        readonly Func<ResearchProfile, string> serialize;
        readonly Func<string, ResearchProfile> deserialize;
        public ResearchProfile Profile { get; private set; }
        public string LoadError { get; private set; }
        public bool IsReadOnly { get; private set; }
        public bool RecoveredFromBackup { get; private set; }
        bool primaryValid;

        public ResearchStore(string path, Func<ResearchProfile, string> serialize, Func<string, ResearchProfile> deserialize)
        {
            this.path = Path.GetFullPath(path);
            this.serialize = serialize ?? throw new ArgumentNullException(nameof(serialize));
            this.deserialize = deserialize ?? throw new ArgumentNullException(nameof(deserialize));
            Reload();
        }

        ResearchProfile Parse(string json)
        {
            var profile = deserialize(json);
            if (profile == null || profile.version != 1 || profile.data < 0 || profile.totalData < profile.data
                || profile.totalRuns < 0 || profile.totalWins < 0 || profile.totalWins > profile.totalRuns)
                throw new InvalidDataException("연구 기록의 형식이나 수치가 올바르지 않습니다.");
            ResearchProgression.Normalize(profile);
            return profile;
        }

        internal ResearchProfile ValidateJson(string json) => Parse(json);

        void Reload()
        {
            IsReadOnly = false; LoadError = ""; primaryValid = false; RecoveredFromBackup = false;
            if (!File.Exists(path) && !File.Exists(path + ".bak")) { Profile = new ResearchProfile(); return; }
            Exception failure = null;
            if (File.Exists(path))
            {
                try { Profile = Parse(File.ReadAllText(path)); primaryValid = true; return; }
                catch (Exception e) { failure = e; }
                // A future format belongs to a newer game. Never roll it back to an
                // older backup or overwrite it with this version's profile.
                try
                {
                    var future = deserialize(File.ReadAllText(path));
                    if (future != null && future.version > 1)
                    {
                        Profile = new ResearchProfile(); IsReadOnly = true;
                        LoadError = "더 새로운 버전의 연구 기록입니다. 해당 버전에서 열어주세요.";
                        return;
                    }
                }
                catch { }
            }
            if (File.Exists(path + ".bak"))
            {
                try
                {
                    Profile = Parse(File.ReadAllText(path + ".bak")); RecoveredFromBackup = true;
                    LoadError = "연구 기록을 백업에서 복원했습니다."; return;
                }
                catch (Exception e) { failure = e; }
            }
            Profile = new ResearchProfile(); IsReadOnly = true;
            LoadError = "연구 기록을 읽지 못해 정산을 보류합니다. 원본과 백업은 보존됩니다. " + failure?.Message;
        }

        FileStream Lock()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            return new FileStream(path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }

        void Persist(ResearchProfile profile)
        {
            string json = serialize(profile);
            Parse(json);
            string temporary = path + ".tmp-" + Guid.NewGuid().ToString("N");
            try
            {
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write(json); writer.Flush(); BrowserPersistence.Flush(stream);
                }
                if (File.Exists(path)) BrowserPersistence.ReplaceFile(temporary, path, primaryValid ? path + ".bak" : null);
                else File.Move(temporary, path);
                // Keep the last valid backup when the primary was damaged or missing.
                Profile = profile; primaryValid = true; IsReadOnly = false; LoadError = "";
                if (!File.Exists(path + ".bak"))
                {
                    try { File.Copy(path, path + ".bak", false); }
                    catch (IOException) { LoadError = "연구 기록은 저장했지만 백업을 만들지 못했습니다."; }
                    catch (UnauthorizedAccessException) { LoadError = "연구 기록은 저장했지만 백업을 만들지 못했습니다."; }
                }
            }
            finally
            {
                if (File.Exists(temporary)) File.Delete(temporary);
                BrowserPersistence.RequestSync();
            }
        }

        public ResearchSettlement GetSettlement(RunState state)
        {
            return state == null ? null : ResearchProgression.CalculateReport(Profile, state);
        }

        public bool TrySettle(RunState state, out ResearchSettlement report, out string error)
        {
            report = GetSettlement(state); error = "";
            if (state?.research == null || !state.research.begun || state.research.legacyEnded
                || (state.stage != RunStage.Won && state.stage != RunStage.Lost)) return false;
            bool settledBefore = state.research.settled;
            var reportBefore = state.research.settlement;
            try
            {
                using (Lock())
                {
                    Reload();
                    if (IsReadOnly) { error = LoadError; return false; }
                    bool duplicate = Profile.settledRunIds.Contains(state.research.runId);
                    var candidate = Parse(serialize(Profile));
                    report = ResearchProgression.Settle(candidate, state);
                    if (duplicate) return false;
                    Persist(candidate);
                    return true;
                }
            }
            catch (Exception e)
            {
                state.research.settled = settledBefore; state.research.settlement = reportBefore;
                report = GetSettlement(state); error = "연구 정산을 저장하지 못했습니다. 다시 시도할 수 있습니다. " + e.Message;
                return false;
            }
        }

        public bool TryCompleteReport(string subjectId, out string error)
        {
            error = "";
            try
            {
                using (Lock())
                {
                    Reload();
                    if (IsReadOnly) { error = LoadError; return false; }
                    var candidate = Parse(serialize(Profile));
                    error = ResearchProgression.ReportError(candidate, subjectId);
                    if (!string.IsNullOrEmpty(error) || !ResearchProgression.CompleteReport(candidate, subjectId)) return false;
                    Persist(candidate);
                    return true;
                }
            }
            catch (Exception e) { error = "연구 보고서를 저장하지 못했습니다. 데이터는 소모되지 않았습니다. " + e.Message; return false; }
        }

        public bool CanUseRareAppearance(string subjectId) => !string.IsNullOrEmpty(subjectId)
            && (subjectId == "lumi" || GameDatabase.Character(subjectId) != null) && Profile.completedReports.Contains(subjectId);

        public bool UsesRareAppearance(string subjectId) => CanUseRareAppearance(subjectId)
            && Profile.rareAppearanceSubjects.Contains(subjectId);

        public bool TrySetRareAppearance(string subjectId, bool enabled, out string error)
        {
            error = "";
            try
            {
                using (Lock())
                {
                    Reload();
                    if (IsReadOnly) { error = LoadError; return false; }
                    if (!CanUseRareAppearance(subjectId)) { error = "연구 보고서를 먼저 완성해야 합니다."; return false; }
                    if (UsesRareAppearance(subjectId) == enabled) return true;
                    var candidate = Parse(serialize(Profile));
                    if (enabled) candidate.rareAppearanceSubjects.Add(subjectId);
                    else candidate.rareAppearanceSubjects.Remove(subjectId);
                    Persist(candidate);
                    return true;
                }
            }
            catch (Exception e) { error = "레어 조우 외형 설정을 저장하지 못했습니다. " + e.Message; return false; }
        }
    }
}
