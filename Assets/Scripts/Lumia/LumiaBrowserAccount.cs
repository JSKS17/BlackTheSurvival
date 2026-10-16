using System;
using System.IO;
using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
using UnityEngine.Scripting;
#endif

namespace Lumia
{
    public sealed partial class LumiaGame
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        string browserAccountId = "";
        bool browserAccountFrozen;

        [Serializable] sealed class BrowserCloudMetadata
        {
            public long revision;
            public string lastSyncedHash = "";
        }

        [Serializable] sealed class BrowserAccountRequest
        {
            public string action, requestId, bundleJson, gameVersion, runJson, researchJson, settingsJson, sha256;
            public bool freeze, hasRun, hasResearch, hasSettings, tutorialSeen;
            public int schemaVersion;
            public long revision;
            public BrowserPendingSave[] pending;
        }

        [Serializable] sealed class BrowserAccountResponse
        {
            public string kind, requestId, bundleJson, lastSyncedHash, error;
            public long revision;
            public bool ok;
        }

        [DllImport("__Internal")] static extern string BTS_AccountId();
        [DllImport("__Internal")] static extern void BTS_AccountEmit(string json);
#endif

        bool BrowserAccountFrozen
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return browserAccountFrozen;
#else
                return false;
#endif
            }
        }

        string BrowserSaveDirectory(bool verification)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            browserAccountId = BTS_AccountId() ?? "";
            Guid parsed;
            if (browserAccountId.Length != 0 && (!Guid.TryParseExact(browserAccountId, "D", out parsed)
                || parsed.ToString("D") != browserAccountId))
                throw new InvalidDataException("계정 저장 공간을 확인하지 못했습니다.");
            browserAccountFrozen = browserAccountId.Length != 0 && !verification;
            if (browserAccountFrozen) return Path.Combine(Application.persistentDataPath, "accounts", browserAccountId);
#endif
            return Application.persistentDataPath;
        }

        string BrowserTutorialPreference(string preference)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (browserAccountId.Length != 0) return preference + ".account." + browserAccountId;
#endif
            return preference;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        BrowserCloudMetadata ReadBrowserCloudMetadata()
        {
            string path = Path.Combine(Path.GetDirectoryName(savePath), BrowserAccountStorage.MetadataFileName);
            if (!File.Exists(path)) return new BrowserCloudMetadata();
            var metadata = JsonUtility.FromJson<BrowserCloudMetadata>(File.ReadAllText(path));
            if (metadata == null || metadata.revision < 0
                || metadata.revision == 0 && !string.IsNullOrEmpty(metadata.lastSyncedHash)
                || metadata.revision > 0 && !BrowserAccountStorage.IsHash(metadata.lastSyncedHash))
                throw new InvalidDataException("브라우저의 동기화 기록이 올바르지 않습니다.");
            return metadata;
        }

        void WriteBrowserCloudMetadata(long revision, string hash)
        {
            var previous = ReadBrowserCloudMetadata();
            if (revision < previous.revision || revision < 1 || !BrowserAccountStorage.IsHash(hash)
                || revision == previous.revision && hash != previous.lastSyncedHash)
                throw new InvalidDataException("동기화 기록의 버전이 올바르지 않습니다.");
            string path = Path.Combine(Path.GetDirectoryName(savePath), BrowserAccountStorage.MetadataFileName);
            WriteAtomicRunFile(path, JsonUtility.ToJson(new BrowserCloudMetadata { revision = revision, lastSyncedHash = hash }), false);
        }

        void ValidateBrowserRun(string json) { new GameEngine(ParseRunSave(json)); }
        void ValidateBrowserResearch(string json) { researchStore.ValidateJson(json); }

        BrowserSaveBundle ReadBrowserBundle() => BrowserAccountStorage.Read(Path.GetDirectoryName(savePath), Path.GetFileName(savePath),
            PlayerPrefs.GetInt(TutorialPreference, 0) != 0, ValidateBrowserRun, ValidateBrowserResearch, ReadBrowserSettings());

        void EmitBrowserLocal(string requestId)
        {
            var metadata = ReadBrowserCloudMetadata();
            BTS_AccountEmit(JsonUtility.ToJson(new BrowserAccountResponse
            {
                kind = "local", requestId = requestId, ok = true,
                bundleJson = BrowserAccountStorage.Serialize(ReadBrowserBundle()),
                revision = metadata.revision, lastSyncedHash = metadata.lastSyncedHash
            }));
        }

        void ReopenBrowserResearch()
        {
            researchStore = new ResearchStore(Path.Combine(Path.GetDirectoryName(savePath), BrowserAccountStorage.ResearchFileName),
                profile => JsonUtility.ToJson(profile, true), json => JsonUtility.FromJson<ResearchProfile>(json));
        }

        void ResetBrowserRun()
        {
            Engine = null;
            lobby = true;
            tutorial = null; tutorialReturnEngine = null; tutorialOffer = tutorialQueued = false;
            CloseStoryReplay(); CloseStoryLayers(); CloseCombatInformation();
            effects.Clear(); displayedTurn = -1; nextEnemyActionAt = nextPlayerActionAt = 0;
            retainSelecting = hyperloopSelecting = false; inspectRareReward = false; rareRevealKey = "";
            upgradeSelected = -1; upgradeBranch = ""; prepStep = 0;
            researchSaveError = ""; pendingResearchRuns = 0;
            ResetBossPreviews();
        }

        // This entry point is called by name from JavaScript. Preserve is needed
        // because IL2CPP otherwise cannot see that SendMessage references it.
        [Preserve]
        public void BrowserAccountCommand(string json)
        {
            BrowserAccountRequest request = null;
            try
            {
                if (browserAccountId.Length == 0) throw new InvalidOperationException("로그인한 계정이 없습니다.");
                request = JsonUtility.FromJson<BrowserAccountRequest>(json);
                if (request == null || string.IsNullOrEmpty(request.requestId)) throw new InvalidDataException("잘못된 계정 요청입니다.");
                switch (request.action)
                {
                    case "export":
                        if (!browserAccountFrozen)
                        {
                            Save();
                            if (!TutorialActive && Engine != null && BrowserAccountStorage.ReadSlot(savePath, ValidateBrowserRun)
                                != JsonUtility.ToJson(Engine.State, true))
                                throw new IOException("현재 게임 진행을 저장하지 못했습니다. 페이지를 닫지 말고 다시 시도해 주세요.");
                        }
                        if (request.freeze) browserAccountFrozen = true;
                        EmitBrowserLocal(request.requestId);
                        break;
                    case "dirty":
                        EmitBrowserLocal(request.requestId);
                        break;
                    case "import":
                        if (!browserAccountFrozen) throw new InvalidOperationException("저장 기록을 적용하기 전에 게임을 일시 정지해야 합니다.");
                        if (request.bundleJson == null || BrowserAccountStorage.Hash(request.bundleJson) != request.sha256)
                            throw new InvalidDataException("서버 저장 기록의 확인 값이 일치하지 않습니다.");
                        var bundle = new BrowserSaveBundle
                        {
                            schemaVersion = request.schemaVersion, gameVersion = request.gameVersion,
                            run = request.hasRun ? request.runJson : null,
                            research = request.hasResearch ? request.researchJson : null,
                            pending = request.pending, tutorialSeen = request.tutorialSeen,
                            // A legacy cloud bundle has no preference information.
                            // Retain this account's local settings without using
                            // another account's global PlayerPrefs as a fallback.
                            settings = request.hasSettings ? JsonUtility.FromJson<BrowserGameSettings>(request.settingsJson) : ReadBrowserSettings()
                        };
                        if (request.hasSettings && bundle.settings == null)
                            throw new InvalidDataException("서버 환경 설정이 올바르지 않습니다.");
                        // Validate metadata before any slot changes as well.
                        var previous = ReadBrowserCloudMetadata();
                        if (request.revision < previous.revision || request.revision < 1 || !BrowserAccountStorage.IsHash(request.sha256)
                            || request.revision == previous.revision && request.sha256 != previous.lastSyncedHash)
                            throw new InvalidDataException("서버 저장 기록의 버전이 올바르지 않습니다.");
                        int previousTutorial = PlayerPrefs.GetInt(TutorialPreference, 0);
                        try
                        {
                            BrowserAccountStorage.Apply(Path.GetDirectoryName(savePath), Path.GetFileName(savePath), bundle,
                                ValidateBrowserRun, ValidateBrowserResearch, afterReplace: () =>
                                {
                                    PlayerPrefs.SetInt(TutorialPreference, bundle.tutorialSeen ? 1 : 0); PlayerPrefs.Save();
                                    WriteBrowserCloudMetadata(request.revision, request.sha256);
                                });
                        }
                        catch { PlayerPrefs.SetInt(TutorialPreference, previousTutorial); PlayerPrefs.Save(); BrowserPersistence.RequestSync(); throw; }
                        ApplyGameSettings(bundle.settings);
                        ResetBrowserRun(); ReopenBrowserResearch(); RetryPendingResearch();
                        BrowserPersistence.RequestSync();
                        EmitBrowserLocal(request.requestId);
                        break;
                    case "ack":
                        WriteBrowserCloudMetadata(request.revision, request.sha256);
                        EmitBrowserLocal(request.requestId);
                        break;
                    case "prepare-resume":
                        browserAccountFrozen = true;
                        RetryPendingResearch();
                        EmitBrowserLocal(request.requestId);
                        break;
                    case "resume":
                        // JavaScript only sends this after prepare-resume's
                        // complete file transaction has reached IndexedDB.
                        // Do not read/write any files after releasing the game.
                        browserAccountFrozen = false;
                        BTS_AccountEmit(JsonUtility.ToJson(new BrowserAccountResponse
                        {
                            kind = "result", requestId = request.requestId, ok = true
                        }));
                        break;
                    default: throw new InvalidDataException("지원하지 않는 계정 요청입니다.");
                }
            }
            catch (Exception error)
            {
                BTS_AccountEmit(JsonUtility.ToJson(new BrowserAccountResponse
                {
                    kind = "result", requestId = request?.requestId ?? "", ok = false,
                    error = "저장 기록을 처리하지 못했습니다. " + error.Message
                }));
            }
        }
#endif
    }
}
