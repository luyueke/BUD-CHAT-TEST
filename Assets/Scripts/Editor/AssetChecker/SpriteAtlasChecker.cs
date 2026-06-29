using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace AssetChecker {

    [CreateAssetMenu(fileName = "SpriteAtlasChecker", menuName = "AssetChecker/SpriteAtlasChecker")]
    public class SpriteAtlasChecker: BaseAssetChecker {
        public override bool Check(AssetImporter assetImporter) {
            SpriteAtlas spriteAtlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(assetImporter.assetPath);
            var packingSetting = spriteAtlas.GetPackingSettings();
            bool isNeedUpdate = false;
            if (packingSetting.enableRotation)
            {
                isNeedUpdate = true;
                packingSetting.enableRotation = false;
            }
            if (packingSetting.enableTightPacking)
            {
                isNeedUpdate = true;
                packingSetting.enableTightPacking = false;
            }

            if (isNeedUpdate)
            {
                LoggerUtils.LogError("Set SpriteAtlas PackingSettings: " + assetImporter.assetPath);
                spriteAtlas.SetPackingSettings(packingSetting);
                EditorUtility.SetDirty(spriteAtlas);
                AssetDatabase.SaveAssetIfDirty(spriteAtlas);
            }
            return true;
        }
    }
}
