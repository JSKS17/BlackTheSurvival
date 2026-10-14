using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lumia
{
    [Serializable] public sealed class UltimateAnimationFrame
    {
        public int x, y, width, height, offsetX, offsetY;
    }
    [Serializable] public sealed class UltimateAnimationDef
    {
        public string id, name, resource;
        public int atlasWidth, atlasHeight, canvasWidth, canvasHeight, frameCount;
        public float framesPerSecond;
        public UltimateAnimationFrame[] frames;
        public float Duration { get { return frameCount / framesPerSecond; } }
    }
    [Serializable] public sealed class UltimateAnimationCatalog
    {
        public UltimateAnimationDef[] items;
    }

    /// <summary>The approved atlas pixels are displayed directly; rendering never changes combat state.</summary>
    public static class UltimateAnimations
    {
        // Leave the centre clear for the simultaneous card reveal. The animation has no UI panel.
        public static readonly Rect PlayerArea = new Rect(24, 81, 456, 356);
        public static readonly Rect EnemyArea = new Rect(800, 81, 456, 356);
        public const float ReducedSeconds = .30f;
        const int TextureCacheLimit = 3;
        static readonly Dictionary<string, UltimateAnimationDef> definitions = new Dictionary<string, UltimateAnimationDef>();
        static readonly Dictionary<string, string> cardOwners = new Dictionary<string, string>();
        static readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
        static readonly Queue<string> textureOrder = new Queue<string>();
        static bool prepared;

        public static IReadOnlyDictionary<string, UltimateAnimationDef> Catalog { get { Prepare(); return definitions; } }

        static void Prepare()
        {
            if (prepared) return;
            prepared = true;
            var asset = Resources.Load<TextAsset>("Lumia/UltimateAnimations/catalog");
            if (asset)
            {
                var catalog = JsonUtility.FromJson<UltimateAnimationCatalog>(asset.text);
                if (catalog != null && catalog.items != null)
                    foreach (var value in catalog.items)
                        if (Valid(value)) definitions[value.id] = value;
                Resources.UnloadAsset(asset);
            }
            foreach (var character in GameDatabase.Characters)
                if (character.cards != null)
                    foreach (string card in character.cards) cardOwners[card] = character.id;
        }

        public static UltimateAnimationDef For(CardDef card)
        {
            if (card == null || card.key != "R" || string.IsNullOrEmpty(card.id)) return null;
            Prepare(); string owner; UltimateAnimationDef value;
            return cardOwners.TryGetValue(card.id, out owner) && definitions.TryGetValue(owner, out value) ? value : null;
        }

        public static bool Valid(UltimateAnimationDef value)
        {
            if (value == null || string.IsNullOrEmpty(value.id) || string.IsNullOrEmpty(value.resource)
                || value.atlasWidth <= 0 || value.atlasHeight <= 0 || value.canvasWidth <= 0 || value.canvasHeight <= 0
                || value.framesPerSecond <= 0 || value.frameCount != 12 || value.frames == null || value.frames.Length != value.frameCount) return false;
            foreach (var frame in value.frames)
                if (frame == null || frame.x < 0 || frame.y < 0 || frame.width <= 0 || frame.height <= 0
                    || frame.x + frame.width > value.atlasWidth || frame.y + frame.height > value.atlasHeight
                    || frame.offsetX < 0 || frame.offsetY < 0 || frame.offsetX + frame.width > value.canvasWidth
                    || frame.offsetY + frame.height > value.canvasHeight) return false;
            return true;
        }

        public static Texture2D Texture(UltimateAnimationDef value)
        {
            if (value == null) return null;
            Texture2D texture;
            if (textures.TryGetValue(value.id, out texture)) return texture;
            texture = Resources.Load<Texture2D>(value.resource);
            if (!texture || texture.width != value.atlasWidth || texture.height != value.atlasHeight)
            {
                if (texture) Resources.UnloadAsset(texture);
                return null;
            }
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            while (textures.Count >= TextureCacheLimit && textureOrder.Count > 0)
            {
                string old = textureOrder.Dequeue(); Texture2D expired;
                if (textures.TryGetValue(old, out expired)) { textures.Remove(old); if (expired) Resources.UnloadAsset(expired); }
            }
            textures[value.id] = texture; textureOrder.Enqueue(value.id);
            return texture;
        }

        public static int FrameIndex(UltimateAnimationDef value, float elapsed, bool reduced)
        {
            return reduced ? Math.Min(7, value.frameCount - 1) : Math.Max(0, Math.Min(value.frameCount - 1, Mathf.FloorToInt(elapsed * value.framesPerSecond)));
        }
        public static Rect FrameUV(UltimateAnimationDef value, int index)
        {
            var frame = value.frames[index];
            return new Rect((float)frame.x / value.atlasWidth, 1f - (float)(frame.y + frame.height) / value.atlasHeight,
                (float)frame.width / value.atlasWidth, (float)frame.height / value.atlasHeight);
        }
        public static Rect Area(bool enemy) { return enemy ? EnemyArea : PlayerArea; }
        public static Rect DisplayBounds(UltimateAnimationDef value)
        {
            // Trim only the empty safety margin outside every approved frame. A fixed union preserves motion,
            // proportions and offsets; fitting each individual frame would make the character pump in size.
            float left = value.canvasWidth, top = value.canvasHeight, right = 0, bottom = 0;
            foreach (var frame in value.frames)
            {
                left = Math.Min(left, frame.offsetX); top = Math.Min(top, frame.offsetY);
                right = Math.Max(right, frame.offsetX + frame.width); bottom = Math.Max(bottom, frame.offsetY + frame.height);
            }
            return Rect.MinMaxRect(left, top, right, bottom);
        }
        public static Rect CanvasRect(UltimateAnimationDef value, bool enemy)
        {
            var area = Area(enemy); var bounds = DisplayBounds(value);
            float scale = Math.Min(area.width / bounds.width, area.height / bounds.height);
            return new Rect(Mathf.Round(area.center.x - bounds.width * scale / 2 - bounds.x * scale),
                Mathf.Round(area.center.y - bounds.height * scale / 2 - bounds.y * scale), value.canvasWidth * scale, value.canvasHeight * scale);
        }
        public static Rect FrameRect(UltimateAnimationDef value, int index, bool enemy = false)
        {
            var canvas = CanvasRect(value, enemy); var frame = value.frames[index];
            float scale = canvas.width / value.canvasWidth;
            return new Rect(canvas.x + frame.offsetX * scale, canvas.y + frame.offsetY * scale, frame.width * scale, frame.height * scale);
        }

        public static void Draw(UltimateAnimationDef value, CardDef card, bool enemy, float elapsed, bool reduced, GUIStyle font)
        {
            if (Event.current.type != EventType.Repaint) return;
            var texture = Texture(value); if (!texture) return;
            Matrix4x4 matrix = GUI.matrix; Color color = GUI.color; bool enabled = GUI.enabled;
            try
            {
                GUI.enabled = true;
                // Draw only approved transparent pixels, with no backdrop, title, border, tint or banner.
                int frame = FrameIndex(value, elapsed, reduced);
                Rect target = FrameRect(value, frame, enemy), area = Area(enemy);
                GUI.color = Color.white;
                GUI.BeginGroup(area);
                try
                {
                    target.x -= area.x; target.y -= area.y;
                    // Fresh atlas region every repaint: no previous frame is retained or blended.
                    GUI.DrawTextureWithTexCoords(target, texture, FrameUV(value, frame), true);
                }
                finally { GUI.EndGroup(); }
            }
            finally { GUI.matrix = matrix; GUI.color = color; GUI.enabled = enabled; }
        }

        public static void ReleaseTextures()
        {
            foreach (var texture in textures.Values) if (texture) Resources.UnloadAsset(texture);
            textures.Clear(); textureOrder.Clear();
        }
    }
}
