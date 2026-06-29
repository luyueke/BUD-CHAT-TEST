using System.Collections.Generic;
using System.IO;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace AssetChecker {
    [CreateAssetMenu(fileName = "ShaderChecker", menuName = "AssetChecker/ShaderChecker")]
    public class ShaderChecker : BaseAssetChecker {


        [Header("不符合要求资源")]
        [ReadOnly]
        [GUIColor("#FFFF00")]
        public List<ErrShader> errShaders;


        public override void Init() {
            var count = errShaders.RemoveAll(tmp => string.IsNullOrEmpty(tmp.path) || !File.Exists(tmp.path));
            if (count > 0) {
                EditorUtility.SetDirty(this);
            }
        }


        public override bool Check(AssetImporter assetImporter) {
            var shaderImporter = assetImporter as ShaderImporter;
            if (shaderImporter == null) {
                return true;
            }

            var shaderTxt = File.ReadAllText(assetImporter.assetPath);
            if (!string.IsNullOrEmpty(shaderTxt) && shaderTxt.Contains("only_renderers"))
            {
                if (!errShaders.Exists(tmp => assetImporter.assetPath == tmp.path)) {
                    errShaders.Add(new ErrShader() { path = assetImporter.assetPath});
                    EditorUtility.SetDirty(this);
                }
                Debug.LogError($"Shader检测到指定的渲染平台《only_renderers》!!请检查Shader:{assetImporter.assetPath}");
            } else {
                if (errShaders.Exists(tmp => assetImporter.assetPath == tmp.path)) {
                    errShaders.RemoveAll(tmp => assetImporter.assetPath == tmp.path);
                    EditorUtility.SetDirty(this);
                }
            }
            return true;
        }
    }



    [System.Serializable]
    public class ErrShader {

        [ReadOnly]
        public string path;

        [ShowInInspector]
        public Shader Asset => AssetDatabase.LoadAssetAtPath(path, typeof(Shader)) as Shader;

    }

}
