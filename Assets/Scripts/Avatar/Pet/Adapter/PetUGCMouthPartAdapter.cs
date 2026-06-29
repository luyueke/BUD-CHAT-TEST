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
    public class PetUGCMouthPartAdapter : FaceSinglePartAdapter {
        private string curUrl = string.Empty;
        private Action putOnSuccess;
        private Transform mouthBone;
        public PetUGCMouthPartAdapter(GameObject _avatar) : base(_avatar) {
            var head =  GameObjectEx.FindChildByName(_avatar, "Pet001 Root/Pet001 Spine/Pet001 Spine1/pet001 Head");
            face_Part = GameObjectEx.FindChildByName(head, "pet_mouth_01/pet_mouth_02").gameObject;
            pgcPart = face_Part.transform.Find("pet_mouth").gameObject;
            mouthBone = GameObjectEx.FindChildByName(head, "pet_mouth_01");
        }

        public override void Move(Vector3 pos)
        {
            mouthBone.localPosition = pos;
        }
        public override void Rotate(Vector3 rot)
        {
            face_Part.transform.localEulerAngles = rot;
        }
        public override void Scale(Vector3 sca)
        {
            mouthBone.localScale = sca;
        }

        public override void AddEffect() {
        }

        public override void PutOn(string id, Action action)
        {
            PutOn(id, suc=>action?.Invoke());
        }

        public void PutOn(string id, Action<bool> action)
        {
            curPartId = id;
            var bData = UgcPartDataManager.Inst.GetUgcPartDataOnFirst(id);
            if (bData == null)
            {
                action?.Invoke(false);
                return;
            }
            LoadRes<GameObject>(GameConsts.PetClothesAssetDir + bData.prefabName + prefabExt, (isSuc, warpper) =>
            {
                if (isSuc && warpper != null && id == curPartId && avatar != null)
                {
                    TakeOff();
                    var partPrefab = warpper.RetainAsset(avatar);
                    var partNode = GameObjectEx.FindChildByName(partPrefab, "pet_mouth_01");
                    curPart = new GameObject(GameConsts.mouthMeshNodeName);
                    curPart.transform.SetParent(face_Part.transform);
                    curPart.transform.localPosition = Vector3.zero;
                    curPart.transform.localRotation = Quaternion.identity;
                    curPart.transform.localScale = Vector3.one;
                    lod?.SetLodMesh(curPart);
                    var ugcdataList = UgcPartDataManager.Inst.GetUgcPartDataList(id);
                    if (ugcdataList != null)
                    {
                        for (int i = 0; i < ugcdataList.Count; i++)
                        {
                            var partObj = GameObject.Instantiate(GameObjectEx.FindChildByName(partNode, ugcdataList[i].partsName), curPart.transform);
                            partObj.name = ugcdataList[i].partsName;
                        }
                    }
                    putOnSuccess?.Invoke();
                    action?.Invoke(true);
                }
                else
                {
                    action?.Invoke(false);
                }
                putOnSuccess = null;
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

                Action<bool, UGCTempPartRemoteWrapper> completed = (success, wrapper) => {
                    if (!success || curPartId != id || curUrl != url)
                    {
                        suc?.Invoke();
                        return;
                    }

                    var dic = wrapper.RetainAssets(curPart);

                    if (dic == null)
                    {
                        suc?.Invoke();
                        return;
                    }
                    foreach (var kValue in dic)
                    {
                        string nameWithoutEx = Path.GetFileNameWithoutExtension(kValue.Key);
                        string[] nameData = nameWithoutEx.Split('_');
                        int ugcType; //前后左右
                        if (!nameData.Contains("alpha"))
                        {
                            ugcType = int.Parse(nameData[0]);
                            UgcPartData ugcdata = UgcPartDataManager.Inst.GetUgcPartData(id, ugcType);
                            var partsGo = curPart.transform.Find(ugcdata.partsName);
                            var partsMat = partsGo.GetComponent<MeshRenderer>().material;
                            partsMat.SetTexture("_BaseMap", kValue.Value);
                        }
                    }
                    suc?.Invoke();
                };
                var textureCount = UgcPartDataManager.Inst.GetUgcTextureCount(id);
                UGCPartLoader.LoadUGCPartRemoteImageAsync((int) AvatarSubType.Mouth, textureCount, url, completed);
            };
            PutOn(id, callback);
        }

        public override void RemoveEffect() {
        }

        public void ResetUGCMouthBoneShow()
        {
            if (curPart != null)
            {
                pgcPart.SetActive(false);
                curPart.SetActive(true);
            }
        }

        public override void TakeOff()
        {
            if (cWarpper != null)
            {
                cWarpper = null;
            }
            if (curPart != null)
            {
                curPart.transform.SetParent(null);
                GameObject.Destroy(curPart);
                curPart = null;
            }
        }

    }
}
