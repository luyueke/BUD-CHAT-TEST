using System;
using Game.OfflineRender;
using UnityEngine;

namespace Game.Utils {
    public class TextureCombineBehaviour : MonoBehaviour {
        protected MeshRenderer meshRenderer;
        protected MeshFilter meshFilter;
        [SerializeField]
        protected TextureCombineData combineData;

        [SerializeField]
        private Texture mainTexture;
        [SerializeField]
        private Texture norTexture;

        public static MaterialPropertyBlock defaultPropertyBlock;


        private bool isGlow = false;
        private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
        private static readonly int BumpMap = Shader.PropertyToID("_BumpMap");
        private static readonly int EmissionMap = Shader.PropertyToID("_EmissionMap");
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");


        private void Awake() {
            gameObject.layer = LayerMask.NameToLayer("Model");
            meshFilter = gameObject.GetOrAddComponent<MeshFilter>();
        }

        public Tuple<TextureCombineData, Texture, Texture> GetData() {
            return new Tuple<TextureCombineData, Texture, Texture>(combineData, mainTexture, norTexture);
        }


        public void SetData(TextureCombineData data, Texture mainTex, Texture norTex)
        {
            combineData = data;
            if (meshRenderer == null)
            {
                meshRenderer = gameObject.GetOrAddComponent<MeshRenderer>();
            }

            if (meshRenderer == null)
            {
                LoggerUtils.LogError("设置模型时 Mesh 为null");
                return;
            }


            if (defaultPropertyBlock == null)
            {
                defaultPropertyBlock = new MaterialPropertyBlock();
            }

            meshRenderer.enabled = true;
            meshRenderer.sharedMaterial = CustomMaterialLoaderUntils.Inst.GetDefaultMaterial();
            meshRenderer.GetPropertyBlock(defaultPropertyBlock);
            if (mainTex != null)
            {
                defaultPropertyBlock.SetTexture(BaseMap, mainTex);
            }
            if (isGlow && mainTex != null)
            {
                defaultPropertyBlock.SetTexture(EmissionMap, mainTex);
            }
            if (norTex != null) {
                defaultPropertyBlock.SetTexture(BumpMap, norTex);
            }
            meshRenderer.SetPropertyBlock(defaultPropertyBlock);
            mainTexture = mainTex;
            norTexture = norTex;
        }


        public void HighLight(bool isHigh)
        {
            if (meshRenderer == null || combineData == null)
            {
                return;
            }
            defaultPropertyBlock.Clear();
            meshRenderer.GetPropertyBlock(defaultPropertyBlock);
            var targetColor = Color.white;
            if (!isHigh)
            {
                targetColor = Color.clear;
            }
            else
            {
                var baseTex = defaultPropertyBlock.GetTexture(BaseMap);
                if (baseTex != null)
                {
                    defaultPropertyBlock.SetTexture(EmissionMap, baseTex);
                }
            }
            defaultPropertyBlock.SetColor(EmissionColor, targetColor);
            meshRenderer.SetPropertyBlock(defaultPropertyBlock);
        }



    }
}
