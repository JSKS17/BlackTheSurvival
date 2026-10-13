using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Lumia.Editor
{
    // Validate the data through the same Resources paths used in the player,
    // including Unity's actual imported dimensions and filtering settings.
    public static class UltimateAnimationAssetValidation
    {
        [Serializable] sealed class Catalog { public Animation[] items; }
        [Serializable] sealed class Animation
        {
            public string id, name, resource;
            public int atlasWidth, atlasHeight, canvasWidth, canvasHeight, frameCount, framesPerSecond;
            public Frame[] frames;
        }
        [Serializable] sealed class Frame { public int x, y, width, height, offsetX, offsetY; }
        [Serializable] sealed class Result
        {
            public bool passed;
            public int animations, frames;
            public string[] verifiedIds;
            public string checks;
        }

        [MenuItem("Black The Survival/Validate ultimate animation assets")]
        public static void Validate()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var text = Resources.Load<TextAsset>("Lumia/UltimateAnimations/catalog");
            Require(text != null, "Ultimate animation catalog not found through Resources");
            Catalog catalog = JsonUtility.FromJson<Catalog>(text.text);
            Require(catalog != null && catalog.items != null && catalog.items.Length == 91, "Expected 91 approved animations");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            int frameCount = 0;
            foreach (Animation item in catalog.items)
            {
                Require(!string.IsNullOrEmpty(item.id) && ids.Add(item.id), "Duplicate or empty animation id");
                Require(item.resource == "Lumia/UltimateAnimations/" + item.id, item.id + ": unexpected resource path");
                Require(item.frameCount == 12 && item.framesPerSecond == 8 && item.frames != null && item.frames.Length == item.frameCount, item.id + ": invalid timing");
                Require(item.canvasWidth > 0 && item.canvasHeight > 0, item.id + ": invalid canvas");
                string path = "Assets/Resources/" + item.resource + ".png";
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Require(importer != null, item.id + ": missing texture importer");
                Require(importer.textureType == TextureImporterType.Default && importer.filterMode == FilterMode.Point &&
                    !importer.mipmapEnabled && !importer.isReadable && importer.npotScale == TextureImporterNPOTScale.None &&
                    importer.textureCompression == TextureImporterCompression.Uncompressed && importer.wrapMode == TextureWrapMode.Clamp &&
                    importer.alphaIsTransparency && importer.alphaSource == TextureImporterAlphaSource.FromInput &&
                    importer.maxTextureSize >= Math.Max(item.atlasWidth, item.atlasHeight), item.id + ": incorrect pixel import settings");
                var texture = Resources.Load<Texture2D>(item.resource);
                Require(texture != null, item.id + ": texture not found through Resources");
                try
                {
                    Require(texture.width == item.atlasWidth && texture.height == item.atlasHeight,
                        item.id + ": atlas was resized during import");
                    Require(texture.mipmapCount == 1 && texture.filterMode == FilterMode.Point && texture.wrapMode == TextureWrapMode.Clamp && !texture.isReadable,
                        item.id + ": imported texture settings changed");
                    foreach (Frame frame in item.frames)
                    {
                        Require(frame.width > 0 && frame.height > 0 && frame.x >= 0 && frame.y >= 0 &&
                            frame.x + frame.width <= texture.width && frame.y + frame.height <= texture.height,
                            item.id + ": frame exceeds atlas bounds");
                        Require(frame.offsetX >= 0 && frame.offsetY >= 0 && frame.offsetX + frame.width <= item.canvasWidth && frame.offsetY + frame.height <= item.canvasHeight,
                            item.id + ": frame exceeds playback canvas");
                        ++frameCount;
                    }
                }
                finally { Resources.UnloadAsset(texture); }
            }
            var verifiedIds = new List<string>(ids);
            verifiedIds.Sort(StringComparer.Ordinal);
            var result = new Result
            {
                passed = true, animations = ids.Count, frames = frameCount, verifiedIds = verifiedIds.ToArray(),
                checks = "Resources catalog and 91 Texture2Ds load; JsonUtility frame schema; unique subject ids; original atlas dimensions retained; 1092 frames fit atlas and canvas; point filtering; uncompressed/non-readable; no mipmaps/NPOT scaling; clamp; input alpha. Textures unloaded after each validation."
            };
            Directory.CreateDirectory("docs");
            File.WriteAllText("docs/runtime-ultimate-import.json", JsonUtility.ToJson(result, true));
            Resources.UnloadAsset(text);
            Debug.Log("ULTIMATE ASSET VALIDATION PASS / animations=" + ids.Count + " frames=" + frameCount);
        }

        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidDataException(message);
        }
    }
}
