using Game.Base;
using Game.Props.PropsComponents;
using UnityEngine;

namespace Game.Props.PropsBehaviours {
    public class PreviewModelBehaviour : NodeBaseBehaviour {
        public void RefreshColor() {
            var comp = entity.GetComp<PreviewModelComponent>();
            if (comp != null) {
                var meshRenderers = GetComponentsInChildren<MeshRenderer>(true);
                foreach (var meshRenderer in meshRenderers) {
                    meshRenderer.material.color = comp.color;
                }
                var skinMeshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
                foreach (var meshRenderer in skinMeshRenderers) {
                    meshRenderer.material.color = comp.color;
                }
            }
        }
    }
}
