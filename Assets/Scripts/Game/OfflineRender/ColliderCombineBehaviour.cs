using Game.Props.PropsComponents;
using UnityEngine;

namespace Game.Base {
    public class ColliderCombineBehaviour :  MonoBehaviour {
        [SerializeField]
        protected MaterialComponent matComp;


        protected MeshCollider meshCollider;

        protected virtual void Awake() {
            gameObject.layer = LayerMask.NameToLayer("Model");
        }

        public virtual void SetMat(MaterialComponent materialComponent)
        {
            matComp = materialComponent;
        }

        public virtual MaterialComponent GetMat()
        {
            return matComp;
        }


        public void SetCollider(bool isEnable)
        {
            if (meshCollider != null)
            {
                meshCollider.enabled = isEnable;
            }
        }
    }
}
