using System.IO;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace Lumia
{
    // Web file writes first change Emscripten's in-memory filesystem. Unity loads
    // IndexedDB before starting the player; syncing here only commits later writes.
    public static class BrowserPersistence
    {
        public static void RequestSync()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            JS_FileSystem_Sync();
            BTS_AccountDirty();
#endif
        }

        internal static void Flush(FileStream stream)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // fsync-to-disk has no browser implementation. RequestSync commits the
            // closed file to IndexedDB after the complete transaction below.
            stream.Flush();
#else
            stream.Flush(true);
#endif
        }

        internal static void ReplaceFile(string temporary, string destination, string backup)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (backup != null) File.Copy(destination, backup, true);
            // FS.rename atomically overwrites the destination in the virtual
            // filesystem. File.Replace relies on native APIs unavailable on Web.
            if (BTS_RenamePersistentFile(temporary, destination) == 0)
                throw new IOException("브라우저 저장 파일을 교체하지 못했습니다.");
#else
            File.Replace(temporary, destination, backup);
#endif
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        static extern void JS_FileSystem_Sync();

        [DllImport("__Internal")]
        static extern void BTS_AccountDirty();

        [DllImport("__Internal")]
        static extern int BTS_RenamePersistentFile(string source, string destination);
#endif
    }
}
