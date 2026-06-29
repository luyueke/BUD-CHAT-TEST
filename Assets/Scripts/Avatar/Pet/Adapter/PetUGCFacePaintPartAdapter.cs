using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Es;
using Game.Avatar;
using Game.Config;
using GameData.PgcData;
using UnityEngine;
using xasset;
namespace Game.Pet {
    public class PetUGCFacePaintPartAdapter : StandardPartAdapter {
        private string curUrl = string.Empty;
        private GameObject ugc_face;
        private MeshRenderer skinRenderer;
        private readonly int baseMapName = Shader.PropertyToID("_patterns_tex");

        public PetUGCFacePaintPartAdapter(GameObject _avatar) : base(_avatar) {
            ugc_face = _avatar.transform.Find("Pet001 Root/Pet001 Spine/Pet001 Spine1/pet001 Head/pet_ugc_pattern").gameObject;
            skinRenderer = ugc_face.GetComponent<MeshRenderer>();
        }
        public override void UGCPutOn(string id, string url,int ugcStyle, Action suc)
        {
            curPartId = id;
            curUrl = url;

            if (string.IsNullOrEmpty(url))
            {
                suc?.Invoke();
                return;
            }
            var textureCount = UgcPartDataManager.Inst.GetUgcTextureCount(id);



            Action<bool,UGCTempPartRemoteWrapper> completed = (success,wrapper) =>
            {
                if (!success ||curPartId != id || curUrl != url || ugc_face == null)
                {
                    suc?.Invoke();
                    return;
                }

                var dic = wrapper.RetainAssets(ugc_face);

                if (dic == null)
                {
                    suc?.Invoke();
                    return;
                }
                PutOn(id, suc);
                foreach (var kValue in dic)
                {
                    string nameWithoutEx = Path.GetFileNameWithoutExtension(kValue.Key);
                    string[] nameData = nameWithoutEx.Split('_');
                    if (!nameData.Contains("alpha"))
                    {
                        string matTextureName = "_patterns_tex";
                        kValue.Value.wrapMode = TextureWrapMode.Clamp;
                        skinRenderer.material.SetTexture(matTextureName, kValue.Value);
                    }
                }
                suc?.Invoke();
            };
            UGCPartLoader.LoadUGCPartRemoteImageAsync((int) AvatarSubType.FacePaint, textureCount, url, completed);
        }
        public override void TakeOff()
        {
            if (ugc_face != null)
            {
                ugc_face.SetActive(false);
            }


        }
        public override void PutOn(string id, Action action)
        {
            if (ugc_face != null)
            {
                ugc_face.SetActive(true);
            }
            action?.Invoke();
        }

        public override void Move(Vector3 pos)
        {
            skinRenderer.material.SetTextureOffset(baseMapName, pos);
        }

        public override void Scale(Vector3 sca)
        {
            skinRenderer.material.SetFloat("_patterms_size", sca.x);
        }

        public override void AddEffect() {
        }



        public override void RemoveEffect() {
        }



    }
}
