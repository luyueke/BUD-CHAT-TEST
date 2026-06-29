using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;




namespace xasset.editor
{
    [CustomEditor(typeof(AssetQualify))]
    public class AssetQualifyEditor : Editor
    {

        private TextureImporterFormat ignoreTextureImporterFormat = TextureImporterFormat.ASTC_8x8;
        private int ignoreTextureSize = 512;
        public BuildTarget buildTarget = BuildTarget.Android;
        // private pLAT
        private AssetQualify assetQualify;

        private void OnEnable()
        {
            assetQualify = target as AssetQualify;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (assetQualify == null || assetQualify.ignoreObjects.Count <= 0)
            {
                return;
            }
            
            GUILayout.Space(5);

            if (GUILayout.Button("设置忽略文件格式", EditorStyles.miniButton, GUILayout.Width(200)))
            {
                foreach (var assetQualifyIgnoreObject in assetQualify.ignoreObjects)
                {
                    if (assetQualifyIgnoreObject is Texture)
                    {
                        var assetPath = AssetDatabase.GetAssetPath(assetQualifyIgnoreObject);
                        var ai = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                        if (ai)
                        {
                            if (ai.textureType != TextureImporterType.Default && ai.textureType != TextureImporterType.NormalMap)
                            {
                                continue;
                            }
                            bool isChange = false;
                            var setting = ai.GetPlatformTextureSettings(buildTarget.ToString());
                            if (setting.overridden == false)
                            {
                                isChange = true;
                                setting = ai.GetDefaultPlatformTextureSettings();
                                setting.name = buildTarget.ToString();
                                setting.overridden = true;
                            }

                            var lastSize = setting.maxTextureSize;
                            if (setting.maxTextureSize != ignoreTextureSize)
                            {
                                setting.maxTextureSize = ignoreTextureSize;
                                isChange = true;
                            }

                            var lastFormat = setting.format;
                            if (setting.format != ignoreTextureImporterFormat)
                            {
                                setting.format = ignoreTextureImporterFormat;
                                isChange = true;
                            }
                            if (isChange)
                            {
                                Debug.LogWarning($"修改贴图格式 {assetQualifyIgnoreObject} : {lastSize} {lastFormat}" );
                                ai.SetPlatformTextureSettings(setting);
                                ai.SaveAndReimport();
                            }
                        }
                    }
                }
            }
            ignoreTextureImporterFormat = (TextureImporterFormat)EditorGUILayout.EnumPopup("忽略贴图文件格式", ignoreTextureImporterFormat);
            ignoreTextureSize = EditorGUILayout.IntPopup("忽略贴图文件尺寸", ignoreTextureSize, new[] {"128", "256", "512", "1024"}, new []{128, 256, 512, 1024});
            buildTarget = (BuildTarget)EditorGUILayout.EnumPopup("设置忽略文件平台", buildTarget);
            if (buildTarget != BuildTarget.Android && buildTarget != BuildTarget.iOS)
            {
                Debug.LogError("只能选择 Android 或 iOS 平台");
                buildTarget = EditorUserBuildSettings.activeBuildTarget;
            }
        }
    }
}