using System;
using Game.Avatar;
using UnityEngine;

namespace Game.Pet {
    public class PetPGCFacePaintPartAdapter : StandardPartAdapter {

        private Texture faceTex;
        private MeshRenderer faceRenderer;
        protected AssetWrapper<Texture> cWarpper;
        private Texture defaultTex;
        private readonly int baseMapName = Shader.PropertyToID("_BaseMap");
        private static readonly int pattermsSize = Shader.PropertyToID("_patterms_size");
        private static readonly int patternsTEX = Shader.PropertyToID("_patterns_tex");

        private const string face_path = "Pet001 Root/Pet001 Spine/Pet001 Spine1/pet001 Head/pet_ugc_pattern";

        public PetPGCFacePaintPartAdapter(GameObject _avatar) : base(_avatar) {
            faceRenderer = GameObjectEx.FindComponentByName<MeshRenderer>(_avatar.transform, face_path);
            if (faceRenderer != null) {
                defaultTex = faceRenderer.material.GetTexture(patternsTEX);
            }

        }


        public override void AddEffect() {
        }

        public override void PutOn(string id, Action action) {
            var bData = Es.DataTables.GetPetAvatarCommonData(id);
            if (bData == null)
            {
                action?.Invoke();
                return;
            }
            var wrapper = Loader.Load<Texture>(bData.texDir + bData.texName[0] + texExt);
            if (wrapper == null)
            {
                action?.Invoke();
                return;
            }

            curPartId = id;
            wrapper.completed = (isSuc) =>
            {
                if (isSuc && curPartId == id && avatar != null)
                {
                    TakeOff();
                    faceRenderer.gameObject.SetActive(true);
                    cWarpper = wrapper;
                    faceTex = wrapper.RetainAsset(avatar);
                    if (faceRenderer != null) {
                        faceRenderer.material.SetTexture(patternsTEX, faceTex);
                    }
                    action?.Invoke();
                }
                else
                {
                    action?.Invoke();
                }
            };
        }

        public override void RemoveEffect() {
        }

        public override void TakeOff()
        {
            if (faceRenderer != null) {
                faceRenderer.material.SetTexture(baseMapName, defaultTex);
            }

            cWarpper = null;
        }

        public override void Move(Vector3 pos) {
            if (faceRenderer != null) {
                faceRenderer.material.SetTextureOffset(patternsTEX, pos);
            }

        }

        public override void Scale(Vector3 sca)
        {
            if (faceRenderer != null) {
                faceRenderer.material.SetFloat(pattermsSize, sca.x);
            }

        }

    }
}
