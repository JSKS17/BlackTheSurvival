using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lumia
{
    /// <summary>A brief card reveal inside the battlefield. Never captures input.</summary>
    public static class CombatCardCue
    {
        public const float RevealSeconds = .32f, ReducedRevealSeconds = .15f;
        public static readonly Rect UltimateEnvelope = new Rect(482, 125, 316, 286);
        static GUIStyle heading, title, caption;
        static Font preparedFont;
        static readonly Dictionary<string, Color> colors = new Dictionary<string, Color>();

        public static bool IsDirectCast(CombatAction action, CardDef card)
        {
            return action != null && card != null && string.IsNullOrEmpty(action.kind) && string.IsNullOrEmpty(action.traitId);
        }
        public static bool Visible(CombatAction action, CardDef card, float progress, float duration, bool reduced)
        {
            return IsDirectCast(action, card) && progress >= 0 && progress * duration < (reduced ? ReducedRevealSeconds : RevealSeconds);
        }
        public static void Draw(CombatAction action, CardDef card, CombatVisualProfile profile,
            float normalizedProgress, float duration, GUIStyle font, bool reduced)
        {
            if (!Visible(action, card, normalizedProgress, duration, reduced)) return;
            PrepareStyles(font);
            float elapsed = normalizedProgress * duration;
            Color primary = Parse(profile.primary), accent = Parse(profile.accent);
            Color side = action.enemy ? new Color(.96f, .59f, .72f) : new Color(.60f, .91f, .78f);
            float alpha = reduced ? 1 : Mathf.Min(Mathf.Clamp01(elapsed / .035f), Mathf.Clamp01((RevealSeconds - elapsed) / .085f));
            Matrix4x4 previousMatrix = GUI.matrix; Color previousColor = GUI.color; bool previousEnabled = GUI.enabled;
            try
            {
                GUI.enabled = true; GUI.color = Color.white;
                if (reduced)
                {
                    Rect ribbon = new Rect(476, 226, 328, 78); Panel(ribbon, primary, .98f);
                    Texture(new Rect(ribbon.x + 12, ribbon.y + 13, 50, 50), PixelArt.SkillIcon(card.id), 1);
                    Text(new Rect(ribbon.x + 75, ribbon.y + 8, 240, 22), action.enemy ? "상대의 카드 사용" : "하나의 카드 사용", caption, 11, side);
                    Text(new Rect(ribbon.x + 75, ribbon.y + 31, 240, 39), card.name + (string.IsNullOrEmpty(action.upgradeLabel) ? "" : " +"), title, 17, Color.white);
                    return;
                }
                float arrive = EaseOut(Mathf.Clamp01(elapsed / .075f)), settle = EaseOut(Mathf.Clamp01((elapsed - .075f) / .075f));
                float scale = elapsed < .075f ? Mathf.Lerp(.72f, 1.10f, arrive) : Mathf.Lerp(1.10f, 1f, settle);
                // R starts with its atlas immediately. Keep its card in the centre instead of entering
                // through the caster's overlay; non-R cards retain their directional entrance.
                Vector2 center = new Vector2(Snap(640 + (card.key == "R" ? 0 : (action.enemy ? 1 : -1) * 130 * (1 - arrive))), 266);
                Rect rect = new Rect(Snap(center.x - 89 * scale), Snap(center.y - 116 * scale), Snap(178 * scale), Snap(232 * scale));
                Fill(new Rect(rect.x + 7, rect.y + 7, rect.width, rect.height), new Color(.015f, .025f, .04f, alpha * .65f));
                Panel(rect, primary, alpha);
                Fill(new Rect(rect.x + 3, rect.y + 3, rect.width - 6, 31 * scale), Alpha(primary * .35f, alpha));
                string key = card.key == "ATK" ? "기본 공격" : card.key == "DEF" ? "방어" : card.key;
                Text(new Rect(rect.x + 10 * scale, rect.y + 6 * scale, rect.width - 20 * scale, 23 * scale),
                    (card.owner ?? "") + " / " + key, heading, Mathf.RoundToInt(12 * scale), Alpha(Readable(accent), alpha));
                float size = Snap(90 * scale);
                Rect icon = new Rect(Snap(center.x - size / 2), Snap(rect.y + 45 * scale), size, size);
                Frame(new Rect(icon.x - 5, icon.y - 5, icon.width + 10, icon.height + 10), Alpha(primary, alpha), 2);
                Texture(icon, PixelArt.SkillIcon(card.id), alpha);
                Text(new Rect(rect.x + 10 * scale, rect.y + 146 * scale, rect.width - 20 * scale, 43 * scale),
                    card.name + (string.IsNullOrEmpty(action.upgradeLabel) ? "" : " +"), title, Mathf.RoundToInt(17 * scale), new Color(.92f, .96f, .97f, alpha));
                Fill(new Rect(rect.x + 10 * scale, rect.yMax - 32 * scale, rect.width - 20 * scale, 2), Alpha(primary, alpha * .8f));
                Text(new Rect(rect.x + 10 * scale, rect.yMax - 27 * scale, rect.width - 20 * scale, 20 * scale),
                    action.enemy ? "상대  ·  카드 사용" : string.IsNullOrEmpty(action.upgradeLabel) ? "하나  ·  카드 사용" : "강화  ·  "+action.upgradeLabel, caption, Mathf.RoundToInt(11 * scale), Alpha(side, alpha));
                float distance = 106 * scale + (1 - settle) * 24;
                for (int sign = -1; sign <= 1; sign += 2)
                {
                    float x = Snap(center.x + sign * distance);
                    Fill(new Rect(x - (sign < 0 ? 0 : 20), center.y - 15, 20, 3), Alpha(accent, alpha * .8f));
                    Fill(new Rect(x, center.y - 15, 3, 30), Alpha(accent, alpha * .8f));
                }
            }
            finally { GUI.matrix = previousMatrix; GUI.color = previousColor; GUI.enabled = previousEnabled; }
        }
        static void PrepareStyles(GUIStyle source)
        {
            if (heading != null && preparedFont == source.font) return;
            preparedFont = source.font;
            heading = new GUIStyle(source) { alignment = TextAnchor.MiddleCenter, wordWrap = false, clipping = TextClipping.Clip };
            title = new GUIStyle(source) { alignment = TextAnchor.MiddleCenter, wordWrap = true, clipping = TextClipping.Clip };
            caption = new GUIStyle(heading);
        }
        static void Text(Rect rect, string text, GUIStyle style, int size, Color color)
        {
            Fit(style, text, size, rect.width, rect.height);
            style.normal.textColor = color; GUI.Label(rect, text, style);
        }
        static void Fit(GUIStyle style, string text, int size, float width, float height)
        {
            style.fontSize = Math.Max(8, size);
            while (style.fontSize > 8 && (style.wordWrap ? style.CalcHeight(new GUIContent(text), width) > height
                : style.CalcSize(new GUIContent(text)).x > width)) style.fontSize--;
        }
        public static int VerifyLayout(GUIStyle source)
        {
            PrepareStyles(source); int measurements = 0;
            foreach (var card in GameDatabase.Cards)
            {
                string key = card.key == "ATK" ? "기본 공격" : card.key == "DEF" ? "방어" : card.key;
                string owner = (card.owner ?? "") + " / " + key;
                foreach (float scale in new[] { .72f, 1f, 1.10f })
                {
                    if (source.font) source.font.RequestCharactersInTexture(owner + card.name, Mathf.RoundToInt(17 * scale), FontStyle.Normal);
                    Fit(heading, owner, Mathf.RoundToInt(12 * scale), 158 * scale, 23 * scale);
                    Fit(title, card.name, Mathf.RoundToInt(17 * scale), 158 * scale, 43 * scale);
                    if (heading.CalcSize(new GUIContent(owner)).x > 158 * scale + .1f
                        || title.CalcHeight(new GUIContent(card.name), 158 * scale) > 43 * scale + .1f)
                        throw new InvalidOperationException("Card reveal text exceeds native layout: " + card.id);
                    measurements += 2;
                }
            }
            return measurements;
        }
        static Color Parse(string value)
        {
            value = value ?? "91dfbd"; Color color;
            if (colors.TryGetValue(value, out color)) return color;
            if (!ColorUtility.TryParseHtmlString("#" + value.TrimStart('#'), out color)) color = new Color(.57f, .87f, .74f);
            colors[value] = color; return color;
        }
        static Color Alpha(Color value, float alpha) { value.a = alpha; return value; }
        static Color Readable(Color value)
        {
            float brightness = value.r * .2126f + value.g * .7152f + value.b * .0722f;
            return brightness < .65f ? Color.Lerp(value, Color.white, (.72f - brightness) / (1 - brightness)) : value;
        }
        static float EaseOut(float p) { float remaining = 1 - p; return 1 - remaining * remaining * remaining; }
        static float Snap(float value) { return Mathf.Round(value / 2) * 2; }
        static void Panel(Rect rect, Color color, float alpha)
        {
            Fill(rect, new Color(.045f, .08f, .115f, .98f * alpha)); Frame(rect, Alpha(color, alpha), 2);
            for (int corner = 0; corner < 4; corner++)
                Fill(new Rect(corner % 2 == 0 ? rect.x + 5 : rect.xMax - 8, corner < 2 ? rect.y + 5 : rect.yMax - 8, 3, 3), Alpha(color, alpha));
        }
        static void Frame(Rect rect, Color color, float width)
        {
            Fill(new Rect(rect.x, rect.y, rect.width, width), color); Fill(new Rect(rect.x, rect.yMax - width, rect.width, width), color);
            Fill(new Rect(rect.x, rect.y, width, rect.height), color); Fill(new Rect(rect.xMax - width, rect.y, width, rect.height), color);
        }
        static void Fill(Rect rect, Color color)
        {
            Color previous = GUI.color; GUI.color = color;
            GUI.DrawTexture(new Rect(Snap(rect.x), Snap(rect.y), Math.Max(2, Snap(rect.width)), Math.Max(2, Snap(rect.height))), Texture2D.whiteTexture);
            GUI.color = previous;
        }
        static void Texture(Rect rect, Texture2D texture, float alpha)
        {
            Color previous = GUI.color; GUI.color = new Color(1, 1, 1, alpha);
            GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit, true); GUI.color = previous;
        }
    }
}
