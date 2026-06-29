using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using xasset.editor;

namespace xasset
{
    public class TextureQuality
    {
        [MenuItem("xasset/切换低模贴图", false, 100)]
        public static void ChangeLowQualityTexture()
        {
            Builder.Quality = 1;
            PreprocessBuildBundles(null, null);
        }

        [MenuItem("xasset/切换高模贴图", false, 100)]
        public static void ChangeHighQualityTexture()
        {
            Builder.Quality = 0;
            PreprocessBuildBundles(null, null);
        }

        public static void PreprocessBuildBundles(Build[] builds, Settings settings)
        {
            var qualitys = AssetDatabase.LoadAssetAtPath<TextureQualityData>("Assets/Editor/TextureQuality/Quality.asset");
            if (qualitys == null) return;

            for (int i = 0, C = qualitys.qualitySettings.Count; i < C; i++)
            {
                var setting = qualitys.qualitySettings[i];
                setting.specialSettings.Clear();
                var assets = AssetDatabase.FindAssets("t:Texture", new string[] { setting.path });
                if (assets != null && assets.Length > 0)
                {
                    for (int j = 0, L = assets.Length; j < L; j++)
                    {
                        var importerPath = AssetDatabase.GUIDToAssetPath(assets[j]);
                        if (Builder.Quality == 0) High(importerPath, setting);
                        else Low(importerPath, setting);
                    }
                }
            }

            AssetDatabase.Refresh();
        }

        public static void High(string path, QualitySetting setting)
        {
            var importer = AssetImporter.GetAtPath(path);
            if (importer is TextureImporter)
            {
                var textureImporter = importer as TextureImporter;
                if (textureImporter.textureType == TextureImporterType.Default)
                {
                    if ((int)setting.highSize == 0) return;
                    var psetting = textureImporter.GetPlatformTextureSettings(EditorUserBuildSettings.activeBuildTarget.ToString());
                    if (!psetting.overridden)
                    {
                        psetting = textureImporter.GetDefaultPlatformTextureSettings();
                    }
                    
                    if (psetting.maxTextureSize != (int)setting.highSize)
                    {
                        psetting.maxTextureSize = (int)setting.highSize;
                        textureImporter.SetPlatformTextureSettings(psetting);
                        textureImporter.SaveAndReimport();
                    }
                }
            }
        }

        public static void Low(string path, QualitySetting setting)
        {
            var importer = AssetImporter.GetAtPath(path);
            if (importer is TextureImporter)
            {
                var textureImporter = importer as TextureImporter;
                if (textureImporter.textureType == TextureImporterType.Default)
                {
                    var csetting = textureImporter.GetPlatformTextureSettings("Custom");
                    var lowSize = csetting.overridden ? csetting.maxTextureSize : (int)setting.lowSize;
                    if (lowSize == 0) return;
                    var psetting = textureImporter.GetPlatformTextureSettings(EditorUserBuildSettings.activeBuildTarget.ToString());
                    if (!psetting.overridden)
                    {
                        psetting = textureImporter.GetDefaultPlatformTextureSettings();
                    }
                    
                    if (psetting.maxTextureSize != lowSize)
                    {
                        psetting.maxTextureSize = lowSize;
                        textureImporter.SetPlatformTextureSettings(psetting);
                        textureImporter.SaveAndReimport();
                    }
                }
            }
        }
    }
}