using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Es;
using Game.Avatar;
using Game.Config;
using GameData.PgcData;
using GameData.UGCData;
using UnityEngine;
using xasset;

namespace Game.Pet {
    class PetUGCTailPartAdapter : BonePartAdapter,IUGCPartAdapter  {
        private Transform partBone;
        private string PartBonePath => "Pet001 Root/Pet001 Spine/pet_waist_01";
        private string PartParentPath => "pet_waist_02";
        private Transform parent;
        public PetUGCTailPartAdapter(GameObject _avatar, Dictionary<string, Transform> bones) : base(_avatar, bones)
        {
            partBone = _avatar.transform.Find(PartBonePath);
            parent = partBone.transform.Find(PartParentPath);
            propSkinRoot = parent.gameObject;
        }
        public override void Move(Vector3 pos) {
            partBone.transform.localPosition = pos;
        }

        public override void Rotate(Vector3 rot) {
            parent.transform.localEulerAngles = rot;
        }

        public override void Scale(Vector3 sca) {
            partBone.transform.localScale = sca;
        }
        public override void AddEffect() {
        }

        public override void PutOn(string id, Action action) {
            PutOn(id, suc=>action?.Invoke());
        }
        public void PutOn(string id, Action<bool> action)
        {
            curPartId = id;

            var callback = action;
            var bData = UgcPartDataManager.Inst.GetUgcPartDataOnFirst(id);
            if (bData == null)
            {
                action?.Invoke(false);
                return;
            }

            LoadRes<GameObject>(GameConsts.PetClothesAssetDir + bData.prefabName + prefabExt,
                (isSuc, warpper) =>
                {
                    if (isSuc && warpper != null && id == curPartId && avatar != null)
                    {
                        TakeOff();
                        cWarpper = warpper;
                        var partPrefab = cWarpper.RetainAsset(avatar);
                        curPart = GameObject.Instantiate(partPrefab, parent);
                        curPart.name = GameConsts.tailMeshNodeName;
                        lod?.SetLodMesh(curPart);
                        UpdateBones(curPart);
                        callback?.Invoke(true);
                    }
                    else
                    {
                        callback?.Invoke(false);
                    }
                });
        }
        public override void UGCPutOn(string id, string url,int ugcStyle, Action suc)
        {

            curPartId = id;
            curUrl = url;

            Action<bool> callback = result =>
            {
                if (!result || curPart == null || curPartId != id || curUrl != url)
                {
                    suc?.Invoke();
                    return;
                }
                if (string.IsNullOrEmpty(url))
                {
                    suc?.Invoke();
                    return;
                }
                var textureCount = UgcPartDataManager.Inst.GetUgcTextureCount(id);

                Action<bool,UGCTempPartRemoteWrapper> completed = (success,wrapper) =>
                {
                    if (!success ||curPartId != id || curUrl != url || curPart == null)
                    {
                        suc?.Invoke();
                        return;
                    }
                    var dic = wrapper.RetainAssets(curPart);

                    if (dic == null)
                    {
                        Debug.LogError(4);
                        suc?.Invoke();
                        return;
                    }

                    foreach (var kValue in dic)
                    {
                        string nameWithoutEx = Path.GetFileNameWithoutExtension(kValue.Key);
                        string[] nameData = nameWithoutEx.Split('_');
                        int ugcType; //前后左右
                        string matTextureName = string.Empty;
                        if (!nameData.Contains("alpha"))
                        {
                            matTextureName = ugcStyle == (int) UgcShaderStyle.Anime ? "_BaseMap" : "_MainTex";
                            ugcType = int.Parse(nameData[0]);
                        }
                        else
                        {
                            ugcType = int.Parse(nameData[0]);
                            matTextureName = "_opacity_texmask";
                        }

                        UgcPartData ugcdata = UgcPartDataManager.Inst.GetUgcPartData(id, ugcType);
                        var partsGo = GameObjectEx.FindChildByName(curPart.transform,ugcdata.partsName);
                        var renderer = partsGo.GetComponent<SkinnedMeshRenderer>();
                        var partsMat = SetPartMaterial(renderer, ugcStyle);
                        partsMat.SetTexture(matTextureName, kValue.Value);
                    }
                    suc?.Invoke();
                };

                UGCPartLoader.LoadUGCPartRemoteImageAsync((int)(AvatarSubType.Tail), textureCount, url, completed);
            };
            PutOn(id, callback);
        }
        public override void ResetCurrentID()
        {
            base.ResetCurrentID();
            curUrl = string.Empty;
        }
        public override void RemoveEffect() {
        }


        public string curUrl { get; set; }
        public GameObject propSkinRoot { get; set; }
    }
}
