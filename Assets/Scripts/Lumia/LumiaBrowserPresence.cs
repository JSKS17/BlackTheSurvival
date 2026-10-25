using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Globalization;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;
#endif

namespace Lumia
{
    public sealed partial class LumiaGame
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        bool browserPresenceLobbyDrawn, browserPresenceSent, browserPresenceReady, browserPresenceLobbyVisible;
        long browserPresenceCount = -1;
        string browserPresenceLabel = "";
        Texture2D browserPresenceDot;
        static readonly Color BrowserPresenceGreen = C("4ade80");
        [DllImport("__Internal")] static extern void BTS_PresenceState(int ready, int lobbyVisible);
#endif

        void BeginBrowserPresenceFrame()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            browserPresenceLobbyDrawn = false;
#endif
        }

        void DrawBrowserPresence()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // DrawLobby can remain behind a modal; only the unobscured logo gets a badge.
            browserPresenceLobbyDrawn = topLayer == 0;
            if (!browserPresenceLobbyDrawn || BrowserAccountFrozen || browserPresenceCount < 0) return;
            if (!browserPresenceDot)
            {
                browserPresenceDot = new Texture2D(10, 10, TextureFormat.RGBA32, false)
                    { filterMode = FilterMode.Point, hideFlags = HideFlags.HideAndDontSave };
                for (int y = 0; y < 10; y++)
                    for (int x = 0; x < 10; x++)
                        browserPresenceDot.SetPixel(x, y, (x - 4.5f) * (x - 4.5f) + (y - 4.5f) * (y - 4.5f) <= 25
                            ? Color.white : Color.clear);
                browserPresenceDot.Apply();
            }
            Color previous = GUI.color;
            GUI.color = BrowserPresenceGreen;
            GUI.DrawTexture(new Rect(340, 51, 10, 10), browserPresenceDot);
            GUI.color = previous;
            Txt(new Rect(358, 43, 240, 28), browserPresenceLabel, 18, BrowserPresenceGreen);
#endif
        }

        void EndBrowserPresenceFrame(bool rendered)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (Event.current.type != EventType.Repaint) return;
            bool ready = rendered && initialized && !BrowserAccountFrozen;
            PublishBrowserPresence(ready, ready && browserPresenceLobbyDrawn);
#endif
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        // The web presence client sends only a validated server count, or -1 when unavailable.
        [Preserve]
        public void BrowserPresenceCount(string value)
        {
            long count;
            if (value == "-1") count = -1;
            else if (!long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out count)
                || count < 0 || count > 9007199254740991L) count = -1;
            browserPresenceCount = count;
            browserPresenceLabel = count < 0 ? "" : "현재 접속자 " + count.ToString(CultureInfo.InvariantCulture) + "명";
        }

        void PublishBrowserPresence(bool ready, bool lobbyVisible)
        {
            if (browserPresenceSent && browserPresenceReady == ready && browserPresenceLobbyVisible == lobbyVisible) return;
            browserPresenceSent = true;
            browserPresenceReady = ready;
            browserPresenceLobbyVisible = lobbyVisible;
            BTS_PresenceState(ready ? 1 : 0, lobbyVisible ? 1 : 0);
        }

        void OnDisable()
        {
            if (Instance != this) return;
            PublishBrowserPresence(false, false);
            BrowserPresenceCount("-1");
            if (browserPresenceDot) Destroy(browserPresenceDot);
            browserPresenceDot = null;
        }
#endif
    }
}
