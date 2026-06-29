// @Author: YangJie
// @Description:
// @Date:  2023/09/15
// @Modify:

using System;
using Game.Props.PropsComponents;
using UnityEngine;

namespace Game.Utils
{
    public class MeshCombineBehaviour : MonoBehaviour
    {

        protected static MaterialPropertyBlock mpb;

        protected MeshRenderer meshRenderer;

        protected MeshFilter meshFilter;
        protected MaterialComponent matComp;
        protected MeshCollider meshCollider;


        private GameMaterialLoader matLoader;

        private bool isInitialized = false;

        private void Init() {
            if (isInitialized) {
                return;
            }
            meshFilter = gameObject.GetOrAddComponent<MeshFilter>();
            meshRenderer = gameObject.GetOrAddComponent<MeshRenderer>();
            meshCollider = gameObject.GetOrAddComponent<MeshCollider>();
            isInitialized = true;
        }

        protected virtual void Awake()
        {
            Init();

        }


        public void SetMesh(Mesh mesh)
        {
            Init();
            meshFilter.sharedMesh = mesh;
            meshCollider.sharedMesh = mesh;
        }

        public virtual void SetMat(MaterialComponent materialComponent)
        {
            Init();
            mpb ??= new MaterialPropertyBlock();
            matComp = materialComponent;
            meshRenderer.enabled = true;
            matLoader = new GameMaterialLoader(gameObject, new Renderer[] {meshRenderer}, mpb);
            matLoader.Load(matComp.matId);
            matLoader.SetMaterialTiling(Vector2.one);
            matLoader.SetColor(matComp.color);

            if (meshCollider != null && !meshCollider.enabled) {
                meshCollider.enabled = true;
            }

            if (meshFilter.sharedMesh == null && meshCollider.sharedMesh != null) {
                meshFilter.sharedMesh = meshCollider.sharedMesh;
            }

        }

        public virtual MaterialComponent GetMat()
        {
            return matComp;
        }

        public void HighLight(bool isHigh)
        {
            if (isHigh)
            {
                matLoader.SetColor(GamePropUtils.GetHighlightColor(matComp.color));
            } else {
                matLoader.SetColor(matComp.color);
            }
        }


    }
}
