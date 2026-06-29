using Game.Props.PropsComponents;
using UnityEngine;

namespace Game.Utils {
    public class MeshSimpleBehaviour : MonoBehaviour {


        protected static MaterialPropertyBlock mpb;
        protected MaterialComponent matComp;


        public virtual void SetMat(MaterialComponent materialComponent)
        {
            // Init();
            mpb ??= new MaterialPropertyBlock();
            matComp = materialComponent;


            var matLoader = new GameMaterialLoader(gameObject, mpb);
            matLoader.Load(materialComponent.matId);
            matLoader.SetMaterialTiling(materialComponent.tile);
            matLoader.SetColor(materialComponent.color);
        }

        public virtual MaterialComponent GetMat()
        {
            return matComp;
        }


    }
}
