// @Author: YangJie
// @Description:
// @Date:  2023/07/26
// @Modify:

using Es;
using Game.Base;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using UnityEngine;

namespace Game.Utils {
    public static class GameSoundHelper {
        private static string GetMatSound(this NodeBaseBehaviour simpleShapeBehaviour) {
            var matComp = simpleShapeBehaviour.entity.GetComp<MaterialComponent>();
            return matComp.GetMatSound();
        }

        private static string GetMatSound(this MaterialComponent matComp) {
            if (matComp.matId.IsUGC) {
                return "Default";
            } else {
                var matConfig = DataTables.GetMatDataConfig(matComp.matId.MatId);
                if (matConfig == null) {
                    return "Default";
                }

                return matConfig.Footstep;
            }
        }


        private static string GetMatSound(this Mesh mesh) {
            return "Default";
        }


        public static string GetMatSound(this GameObject hitGameObject) {
            if (hitGameObject.TryGetComponentInParent<TerrainBehaviour>(out var terrainBehaviour)) {
                return terrainBehaviour.GetMatSound();
            } else if (hitGameObject.TryGetComponentInParent<SimpleShapeBehaviour>(out var simpleShapeBehaviour)) {
                return simpleShapeBehaviour.GetMatSound();
            } else if (hitGameObject.TryGetComponentInParent<MeshCombineBehaviour>(out var meshCombineBehaviour)) {
                return meshCombineBehaviour.GetMat().GetMatSound();
            } else if (hitGameObject.TryGetComponentInParent<ColliderCombineBehaviour>(
                           out var colliderCombineBehaviour)) {
                return colliderCombineBehaviour.GetMat().GetMatSound();
            }else if (hitGameObject.TryGetComponentInParent<AIYandereTerrainBehaviour>(out var aiTerrain))
            {
                return aiTerrain.GetMatSound();
            }

            return "Default";
        }
    }
}
