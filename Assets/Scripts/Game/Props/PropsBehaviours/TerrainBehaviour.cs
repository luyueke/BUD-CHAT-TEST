using Es;
using Game.Base;
using Game.Props.PropsComponents;
using Game.Utils;
using GameData;
using UnityEngine;

namespace Game.Props.PropsBehaviours {
    public class TerrainBehaviour : NodeBaseBehaviour {
        private static MaterialPropertyBlock mpb;
        private Renderer[] renderers;
        GameMaterialLoader matLoader;

        public override void OnInitByCreate() {
            if (mpb == null) {
                mpb = new MaterialPropertyBlock();
            }

            var rootFloor = this.transform.Find("Mesh1");
            renderers = rootFloor.GetComponentsInChildren<Renderer>();
            matLoader = new GameMaterialLoader(gameObject, renderers, mpb);
        }

        private void OnDestroy() {
            matLoader.Destroy();
        }

        public void SetColor(Color color) {
            matLoader.SetColor(color);
        }

        public void SetMaterial(MaterialUnionID matId) {
            matLoader.Load(matId);
            matLoader.SetMaterialTiling(Vector2.one *  entity.GetComp<TerrainComponent>().size / 5);
        }

        public void HideTerrain(bool isHide) {
            for (int i = 0; i < renderers.Length; i++) {
                renderers[i].enabled = !isHide;
            }
        }

        public void SetSize(float size) {
            for (int i = 0; i < transform.childCount; i++) {
                transform.GetChild(i).transform.localScale = new Vector3(size, 1, size);
            }
            // 默认是5
            matLoader.SetMaterialTiling(Vector2.one *  size / 5);


        }

    }
}
