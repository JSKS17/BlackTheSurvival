using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Lumia
{
    public sealed partial class LumiaGame
    {
        bool mobileTypography;
        int typographyWidth = -1, typographyHeight = -1, typographyMinimum;
        readonly Dictionary<MobileFontFitKey, int> mobileFontFits = new Dictionary<MobileFontFitKey, int>();
        int mobileFontFitRevision = -1;
        struct MobileFontFitKey : IEquatable<MobileFontFitKey>
        {
            public string text; public int size, requested; public float width, height; public bool wrap;
            public bool Equals(MobileFontFitKey other) => text == other.text && size == other.size && requested == other.requested && width == other.width && height == other.height && wrap == other.wrap;
            public override bool Equals(object other) => other is MobileFontFitKey && Equals((MobileFontFitKey)other);
            public override int GetHashCode() { unchecked { return ((((text.GetHashCode() * 397 ^ size) * 397 ^ requested) * 397 ^ width.GetHashCode()) * 397 ^ height.GetHashCode()) * 397 ^ wrap.GetHashCode(); } }
        }
        [DllImport("__Internal")] static extern int BTS_IsMobilePresentation();

        void UpdateMobileTypography()
        {
            if (typographyWidth == Screen.width && typographyHeight == Screen.height) return;
            bool mobile = Application.isMobilePlatform || SystemInfo.deviceType == DeviceType.Handheld;
#if UNITY_WEBGL && !UNITY_EDITOR
            mobile |= BTS_IsMobilePresentation() != 0;
#endif
            float scale = Mathf.Max(.25f, Mathf.Min(Screen.width / W, Screen.height / H));
            int minimum = mobile ? Mathf.Clamp(Mathf.CeilToInt(12 / scale), 18, 34) : 0;
            typographyWidth = Screen.width; typographyHeight = Screen.height;
            if (mobileTypography == mobile && typographyMinimum == minimum) return;
            mobileTypography = mobile; typographyMinimum = minimum;
            craftingFontRevision++;
            summaryScroll.Clear(); textMeasures.Clear(); summaryLayouts.Clear(); craftingLayouts.Clear();
            mobileFontFits.Clear();
            storyNarrationStyle = storyDialogueStyle = null;
            CancelNativeTouchScroll(true);
        }

        int UIFontSize(int requested) => !mobileTypography ? requested : Math.Max(typographyMinimum, requested + 8);

        int BodyUIFontSize(int requested, float height)
        {
            int size = UIFontSize(requested);
            if (!mobileTypography) return size;
            // A very short caption must still show an entire line while its
            // remaining text scrolls. Taller descriptions retain the full size.
            while (size > requested)
            {
                wrapped.fontSize = size;
                if (wrapped.lineHeight <= height - 2) break;
                size--;
            }
            return size;
        }

        int FittedUIFontSize(GUIStyle style, string text, Rect rect, int requested, bool wrap)
        {
            int size = UIFontSize(requested);
            if (!mobileTypography) return size;
            style.wordWrap = wrap;
            if (mobileFontFitRevision != craftingFontRevision) { mobileFontFits.Clear(); mobileFontFitRevision = craftingFontRevision; }
            var key = new MobileFontFitKey { text = text ?? "", size = size, requested = requested, width = rect.width, height = rect.height, wrap = wrap };
            int fitted;
            if (mobileFontFits.TryGetValue(key, out fitted)) return fitted;
            var content = new GUIContent(text ?? "");
            // Compact badges and buttons keep their complete labels inside their
            // hit areas; descriptions use the larger scrollable body size instead.
            int floor = Math.Max(9, Math.Min(requested, size));
            while (size > floor)
            {
                style.fontSize = size;
                if (style.CalcHeight(content, rect.width) <= rect.height
                    && (wrap || style.CalcSize(content).x <= rect.width)) break;
                size--;
            }
            if (mobileFontFits.Count >= 1024) mobileFontFits.Clear();
            mobileFontFits[key] = size; return size;
        }
    }
}
