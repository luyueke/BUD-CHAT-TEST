using UnityEditor;
using UnityEngine;

namespace AssetChecker {
    [CreateAssetMenu(fileName = "ModelChecker", menuName = "AssetChecker/ModelChecker")]
    public class ModelChecker : BaseAssetChecker {

        [Header("模型压缩格式")]
        public ModelImporterMeshCompression meshCompression = ModelImporterMeshCompression.Medium;
        public override bool Check(AssetImporter assetImporter) {
            var modelImporter = assetImporter as ModelImporter;
            if (modelImporter == null)
            {
                return true;
            }
            bool isNeedUpdate = false;
            if (modelImporter.meshCompression != meshCompression)
            {
                isNeedUpdate = true;
                modelImporter.meshCompression = meshCompression;
            }

            if (modelImporter.importAnimation && modelImporter.clipAnimations.Length > 0 && modelImporter.animationCompression != ModelImporterAnimationCompression.Optimal)
            {
                isNeedUpdate = true;
                modelImporter.animationCompression = ModelImporterAnimationCompression.Optimal;
            }
            if (isNeedUpdate)
            {
                modelImporter.SaveAndReimport();
            }
            return true;
        }
    }
}
