using System.Collections.Generic;
using System.IO;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace AssetChecker {

    [CreateAssetMenu(fileName = "TextureChecker", menuName = "AssetChecker/TextureChecker")]
    public class TextureChecker: BaseAssetChecker {


        [Header("贴图大小限制")]
        public int sizeLimit = 512;

        [Header("是否自动自行资源压缩")]
        [Tooltip("压缩格式为ASTC 8*8")]
        public bool autoFixCompress;

        [Header("不符合要求资源")]
        [ReadOnly]
        [GUIColor("#FFFF00")]
        public List<ErrTexture> errTextures;



        public override void Init() {
            var count = errTextures.RemoveAll(tmp => string.IsNullOrEmpty(tmp.path) || !File.Exists(tmp.path));
            if (count > 0) {
                EditorUtility.SetDirty(this);
            }
        }

        public override bool Check(AssetImporter assetImporter) {
            var textureImporter = assetImporter as TextureImporter;
            if (textureImporter == null) {
                return true;
            }
            if (textureImporter.textureType != TextureImporterType.Default && textureImporter.textureType != TextureImporterType.NormalMap) {
                return true;
            }

            bool isDirty = false;


            textureImporter.GetSourceTextureWidthAndHeight(out int width, out int height);
            if (width > sizeLimit || height > sizeLimit) {
                if (!errTextures.Exists(tmp => assetImporter.assetPath == tmp.path)) {
                    errTextures.Add(new ErrTexture() { path = assetImporter.assetPath});
                    isDirty = true;
                }
            } else {
                if (errTextures.Exists(tmp => assetImporter.assetPath == tmp.path)) {
                    errTextures.RemoveAll(tmp => assetImporter.assetPath == tmp.path);
                    isDirty = true;
                }
            }

            if (autoFixCompress) {
                textureImporter.mipmapEnabled = false;
                var platformList = new List<string>() { BuildTarget.Android.ToString(), BuildTarget.iOS.ToString()};

                foreach (var platform in platformList) {
                    var platformSetting = textureImporter.GetPlatformTextureSettings(platform);
                    if (platformSetting != null && (platformSetting.format!= TextureImporterFormat.ASTC_8x8 || platformSetting.maxTextureSize > sizeLimit)) {
                        if (autoFixCompress) {
                            platformSetting.overridden = true;
                            platformSetting.format = TextureImporterFormat.ASTC_8x8;
                            platformSetting.maxTextureSize = Mathf.Min(sizeLimit, platformSetting.maxTextureSize);
                            textureImporter.SetPlatformTextureSettings(platformSetting);
                            textureImporter.SaveAndReimport();
                        }
                    }
                }


            }

            if (isDirty) {
                EditorUtility.SetDirty(this);
            }





            return true;
        }
    }



    [System.Serializable]
    public class ErrTexture {

        [ReadOnly]
        public string path;

        [ShowInInspector]
        public Texture Asset => AssetDatabase.LoadAssetAtPath(path, typeof(Texture)) as Texture;

    }

}
