using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Lumia.Editor
{
    public static class TacticalIconAssetValidation
    {
        public static void Validate()
        {
            if (TacticalSkills.Definitions.Length != 26)
                throw new BuildFailedException("Expected 26 tactical skill definitions.");
            foreach (var skill in TacticalSkills.Definitions)
                Check("Assets/Resources/Lumia/SkillIcons/" + skill.id + ".png", 96);
            Check("Assets/Resources/Lumia/SystemIcons/tactical_module.png", 64);
            Debug.Log("LUMIA TACTICAL ICONS PASS / skills=26 / module=1 / pixel imports=27");
        }

        static void Check(string assetPath, int size)
        {
            string project = Path.GetDirectoryName(Application.dataPath);
            string filename = Path.Combine(project, assetPath);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (!File.Exists(filename) || new FileInfo(filename).Length == 0 || texture == null || importer == null
                || texture.width != size || texture.height != size || texture.filterMode != FilterMode.Point
                || importer.mipmapEnabled || importer.textureCompression != TextureImporterCompression.Uncompressed
                || importer.npotScale != TextureImporterNPOTScale.None || importer.wrapMode != TextureWrapMode.Clamp)
                throw new BuildFailedException("Missing or incorrect tactical pixel icon: " + assetPath);
        }
    }
}
