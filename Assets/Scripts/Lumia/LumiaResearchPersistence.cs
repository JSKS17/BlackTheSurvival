using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Lumia
{
    public sealed partial class LumiaGame
    {
        int pendingResearchRuns;

        static bool ResearchTerminal(RunState state) => state != null && (state.stage == RunStage.Won || state.stage == RunStage.Lost);

        static RunState ParseRunSave(string json)
        {
            var state = JsonUtility.FromJson<RunState>(json);
            if (state == null || state.level < 1 || state.level > 20 || state.deck == null || state.map == null
                || !Enum.IsDefined(typeof(RunStage), state.stage))
                throw new InvalidDataException("저장 기록 형식이 올바르지 않습니다.");
            return state;
        }

        RunState ReadRunSave(out bool recovered)
        {
            recovered = false; Exception failure = null;
            try { return ParseRunSave(File.ReadAllText(savePath)); }
            catch (Exception error) { failure = error; }
            try { var state = ParseRunSave(File.ReadAllText(savePath + ".bak")); recovered = true; return state; }
            catch (Exception error) { throw new InvalidDataException("실험 기록과 백업을 읽을 수 없습니다. " + failure?.Message + " / " + error.Message, error); }
        }

        static void WriteAtomicRunFile(string path, string json, bool backup)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temporary = path + ".tmp-" + Guid.NewGuid().ToString("N");
            try
            {
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                {
                    writer.Write(json); writer.Flush(); BrowserPersistence.Flush(stream);
                }
                if (File.Exists(path)) BrowserPersistence.ReplaceFile(temporary, path, backup ? path + ".bak" : null);
                else File.Move(temporary, path);
            }
            finally
            {
                if (File.Exists(temporary)) File.Delete(temporary);
                BrowserPersistence.RequestSync();
            }
        }

        bool ValidRunPrimary()
        {
            try { ParseRunSave(File.ReadAllText(savePath)); return true; }
            catch { return false; }
        }

        string[] PendingResearchFiles()
        {
            string directory = Path.GetDirectoryName(savePath);
            return Directory.Exists(directory)
                ? Directory.GetFiles(directory, Path.GetFileName(savePath) + ".research-pending-*.json", SearchOption.TopDirectoryOnly)
                : new string[0];
        }

        string PendingResearchPath(RunState state)
        {
            string id = state.research.runId;
            if (id.Length > 96 || id.Any(character => !(char.IsLetterOrDigit(character) || character == '-' || character == '_')))
            {
                using (var hash = SHA256.Create()) id = BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(id))).Replace("-", "").ToLowerInvariant();
            }
            return savePath + ".research-pending-" + id + ".json";
        }

        // A queued run remains durable until the permanent ledger confirms it. A crash
        // after the profile replacement but before deletion is harmless on the next retry.
        void RetryPendingResearch()
        {
            if (researchStore == null || string.IsNullOrEmpty(savePath)) return;
            try
            {
                foreach (string path in PendingResearchFiles())
                {
                    try
                    {
                        var state = new GameEngine(ParseRunSave(File.ReadAllText(path))).State;
                        if (!ResearchTerminal(state) || state.research == null || !state.research.begun || state.research.legacyEnded) continue;
                        ResearchSettlement report; string error;
                        bool changed = researchStore.TrySettle(state, out report, out error);
                        if (string.IsNullOrEmpty(error) && (changed || researchStore.Profile.settledRunIds.Contains(state.research.runId)))
                        {
                            File.Delete(path);
                            BrowserPersistence.RequestSync();
                        }
                    }
                    catch (Exception error) { Debug.LogWarning("보관한 연구 정산 재시도: " + error.Message); }
                }
                pendingResearchRuns = PendingResearchFiles().Length;
            }
            catch (Exception error) { Debug.LogWarning("보관한 연구 기록 확인: " + error.Message); }
        }

        bool PreserveResearchBeforeNewRun()
        {
            RetryPendingResearch();
            RunState previous = Engine?.State;
            if (previous == null && (File.Exists(savePath) || File.Exists(savePath + ".bak")))
            {
                try { bool recovered; previous = new GameEngine(ReadRunSave(out recovered)).State; }
                catch (Exception error)
                {
                    Notify("기존 실험 기록을 확인하지 못해 새 원정을 시작하지 않았습니다. " + error.Message);
                    return false;
                }
            }
            if (!ResearchTerminal(previous) || previous.research == null || !previous.research.begun || previous.research.legacyEnded) return true;
            ResearchSettlement report; string settlementError;
            researchStore.TrySettle(previous, out report, out settlementError);
            if (string.IsNullOrEmpty(settlementError)) return true;
            if (previous.research.settled && researchStore.Profile.settledRunIds.Contains(previous.research.runId)) return true;
            try
            {
                // Preserve the complete terminal run before replacing the single run slot.
                string json = JsonUtility.ToJson(previous, true);
                ParseRunSave(json);
                WriteAtomicRunFile(PendingResearchPath(previous), json, false);
                pendingResearchRuns = PendingResearchFiles().Length;
                Notify("미정산 연구 기록을 보관했습니다. 다음 실행 또는 연구 기록 열기 때 다시 정산합니다.");
                return true;
            }
            catch (Exception error)
            {
                researchSaveError = "미정산 연구 기록을 보관하지 못했습니다. " + error.Message;
                Notify("연구 기록을 보존할 수 없어 새 원정을 시작하지 않았습니다. 잠시 후 다시 시도해주세요.");
                return false;
            }
        }
    }
}
