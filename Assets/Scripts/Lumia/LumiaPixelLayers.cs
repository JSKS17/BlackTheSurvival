using UnityEngine;

namespace Lumia
{
    public sealed partial class LumiaGame
    {
        sealed class PixelLayerSprite
        {
            public Texture2D source;
            public RenderTexture texture;
            public Rect uv;
            public int width, height;
        }
        static PixelLayerSprite CreatePixelLayer(Texture2D source, StoryCharacterGeometry geometry, int size, string name)
        {
            int x0 = geometry == null ? 0 : Mathf.Clamp(Mathf.FloorToInt((float)geometry.left * size / geometry.width), 0, size - 1);
            int y0 = geometry == null ? 0 : Mathf.Clamp(Mathf.FloorToInt((float)geometry.top * size / geometry.height), 0, size - 1);
            int x1 = geometry == null ? size : Mathf.Clamp(Mathf.CeilToInt((float)geometry.right * size / geometry.width), x0 + 1, size);
            int y1 = geometry == null ? size : Mathf.Clamp(Mathf.CeilToInt((float)geometry.bottom * size / geometry.height), y0 + 1, size);
            source.filterMode = FilterMode.Point; source.wrapMode = TextureWrapMode.Clamp;
            var target = new RenderTexture(size, size, 0, RenderTextureFormat.ARGB32) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, useMipMap = false, antiAliasing = 1, name = name };
            RefreshPixelLayerTexture(source, target);
            return new PixelLayerSprite { source = source, texture = target, width = x1 - x0, height = y1 - y0,
                uv = new Rect((float)x0 / size, 1f - (float)y1 / size, (float)(x1 - x0) / size, (float)(y1 - y0) / size) };
        }
        static PixelLayerSprite EnsurePixelLayer(PixelLayerSprite sprite)
        {
            if (sprite != null && !sprite.texture.IsCreated()) RefreshPixelLayerTexture(sprite.source, sprite.texture);
            return sprite;
        }
        static void RefreshPixelLayerTexture(Texture2D source, RenderTexture target)
        {
            target.Create(); var previous = RenderTexture.active; bool srgb = GL.sRGBWrite;
            try { GL.sRGBWrite = QualitySettings.activeColorSpace == ColorSpace.Linear; Graphics.Blit(source, target); }
            finally { RenderTexture.active = previous; GL.sRGBWrite = srgb; }
        }
        void ReleasePixelLayer(PixelLayerSprite sprite)
        {
            if (sprite != null && sprite.texture) { sprite.texture.Release(); Destroy(sprite.texture); }
        }
    }
}
