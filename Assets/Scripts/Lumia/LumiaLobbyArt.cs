using UnityEngine;

namespace Lumia
{
    public sealed partial class LumiaGame
    {
        PixelLayerSprite lobbyNiaPixels;
        StoryCharacterGeometry lobbyNiaGeometry;
        bool lobbyArtOnly;

        PixelLayerSprite LobbyNia()
        {
            if (lobbyNiaPixels == null)
            {
                var source = Resources.Load<Texture2D>("Lumia/Lobby/Nia");
                var metadata = Resources.Load<TextAsset>("Lumia/Lobby/geometry");
                if (!source || !metadata) return null;
                lobbyNiaGeometry = JsonUtility.FromJson<StoryCharacterGeometry>(metadata.text);
                lobbyNiaPixels = CreatePixelLayer(source, lobbyNiaGeometry, StoryComposition.LogicalSpriteSize, "Lobby Nia pixels");
            }
            return EnsurePixelLayer(lobbyNiaPixels);
        }
        Rect LobbyNiaRect(PixelLayerSprite sprite)
        {
            const int scale = 7;
            return new Rect(934 - sprite.width * scale / 2f, 560 - sprite.height * scale, sprite.width * scale, sprite.height * scale);
        }
        void DrawLobbyArt()
        {
            var background = PixelArt.Background(); var sprite = LobbyNia();
            Tex(new Rect(0, 0, W, H), background, ScaleMode.ScaleAndCrop);
            if (sprite != null) GUI.DrawTextureWithTexCoords(LobbyNiaRect(sprite), sprite.texture, sprite.uv, true);
        }
        public void ShowLayeredLobbyVerification(bool artOnly)
        {
            GuardLayeredStoryVerification(); CloseStoryLayers(); CloseStoryReplay(); lobby = true; lobbyArtOnly = artOnly;
            lobbyDialogueIndex = 12;
        }
        public void VerifyLayeredLobbyArt()
        {
            GuardLayeredStoryVerification();
            var sprite = LobbyNia(); var background = Resources.Load<Texture2D>("Lumia/Lobby/Background");
            if (!background || sprite == null || lobbyNiaGeometry.width != sprite.source.width || lobbyNiaGeometry.height != sprite.source.height
                || background != PixelArt.Background() || background.filterMode != FilterMode.Point || sprite.texture.width != 80)
                throw new System.InvalidOperationException("The lobby needs its separate background, transparent Nia and pixel geometry.");
            Rect body = LobbyNiaRect(sprite);
            if (body.x < 540 || body.xMax > W || body.y < 5 || body.yMax > 670)
                throw new System.InvalidOperationException("Lobby Nia overlaps the menu or is clipped by the screen.");
            sprite.texture.Release();
            if (!LobbyNia().texture.IsCreated()) throw new System.InvalidOperationException("Lobby pixel target did not recover.");
            Debug.Log("BTS LOBBY LAYERS PASS / independentBackground=PASS / independentNia=PASS / pixelGrid=80 / lostTargetRecovery=PASS");
        }
    }
}
