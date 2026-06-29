using Es;
using Game.Avatar;
using Game.Config;
using GameData.PgcData;
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using xasset;

namespace Game.Pet
{
    public class PetUGCEyesPartAdapter : StandardPartAdapter
    {
        private string curUrl = string.Empty;
        protected GameObject part_l;
        protected GameObject part_r;
        private Transform bone_Rot_L;
        private Transform bone_Rot_R;

        public PetUGCEyesPartAdapter(GameObject _avatar) : base(_avatar)
        {
            var head = GameObjectEx.FindChildByName(_avatar, "Pet001 Root/Pet001 Spine/Pet001 Spine1/pet001 Head");
            part_l = head.transform.Find("pet_eyes_l_01").gameObject;
            part_r = head.transform.Find("pet_eyes_r_01").gameObject;
            bone_Rot_L = part_l.transform.Find("pet_eyes_l_02");
            bone_Rot_R = part_r.transform.Find("pet_eyes_r_02");
            pgcNode_l = bone_Rot_L.Find("pet_eyes_l").gameObject;
            pgcNode_r = bone_Rot_R.Find("pet_eyes_r").gameObject;
        }
        public override void Move(Vector3 offset)
        {
            part_l.transform.localPosition = offset;
            offset.z = -offset.z;
            part_r.transform.localPosition = offset;
        }

        public override void Rotate(Vector3 rot)
        {
            Vector3 rot_L = rot;
            Vector3 rot_R = rot_L;
            rot_R.y = -rot_R.y;
            bone_Rot_L.localEulerAngles = rot_L;
            bone_Rot_R.localEulerAngles = rot_R;

        }

        public override void Scale(Vector3 sca)
        {
            part_l.transform.localScale = sca;
            part_r.transform.localScale = sca;
        }

        public override void ResetCurrentID()
        {
            base.ResetCurrentID();
            curUrl = string.Empty;
        }
        public override void AddEffect()
        {
        }

        public override void PutOn(string id, Action action)
        {
            PutOn(id, suc => action?.Invoke());
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
                    var clip = warpper.RetainAsset(avatar);
                    var ugcdataList = UgcPartDataManager.Inst.GetUgcPartDataList(id);
                    if (ugcdataList != null)
                    {
                        ugcNode_l = GameObject.Instantiate(GameObjectEx.FindChildByName(clip, ugcdataList[0].partsName), bone_Rot_L.transform).gameObject;
                        ugcNode_l.name = ugcdataList[0].partsName;
                        lod?.SetLodMesh(ugcNode_l);
                        ugcNode_r = GameObject.Instantiate(GameObjectEx.FindChildByName(clip, ugcdataList[1].partsName), bone_Rot_R.transform).gameObject;
                        ugcNode_r.name = ugcdataList[1].partsName;
                        lod?.SetLodMesh(ugcNode_r);
                    }
                    action?.Invoke(true);
                }
                else
                {
                    action?.Invoke(false);
                }
            });
        }
        public override void UGCPutOn(string id, string url, int ugcStyle, Action suc)
        {
            curPartId = id;
            curUrl = url;

            Action<bool> callback = result =>
            {
                if (!result || ugcNode_l == null || curPartId != id || curUrl != url)
                {
                    suc?.Invoke();
                    return;
                }

                if (string.IsNullOrEmpty(url))
                {
                    suc?.Invoke();
                    return;
                }

                Action<bool, UGCTempPartRemoteWrapper> completed = (success, wrapper) =>
                {
                    if (!success || curPartId != id || curUrl != url || ugcNode_l == null)
                    {
                        suc?.Invoke();
                        return;
                    }

                    var dic = wrapper.RetainAssets(ugcNode_l);

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
                        ugcType = int.Parse(nameData[0]);
                        if (!nameData.Contains("alpha"))
                        {
                            UgcPartData ugcdata = UgcPartDataManager.Inst.GetUgcPartData(id, ugcType);
                            if (ugcNode_l.name == ugcdata.partsName)
                            {
                                var partsMat = ugcNode_l.GetComponent<MeshRenderer>().material;
                                partsMat.SetTexture("_BaseMap", kValue.Value);
                            }
                            else if (ugcNode_r.name == ugcdata.partsName)
                            {
                                var partsMat = ugcNode_r.GetComponent<MeshRenderer>().material;
                                partsMat.SetTexture("_BaseMap", kValue.Value);
                            }
                        }
                    }

                    suc?.Invoke();
                };
                var textureCount = UgcPartDataManager.Inst.GetUgcTextureCount(id);
                UGCPartLoader.LoadUGCPartRemoteImageAsync((int)AvatarSubType.Eyes, textureCount, url, completed);
            };
            PutOn(id, callback);
        }
        public void ResetUGCEyeBoneShow()
        {
            if (ugcNode_l != null && ugcNode_r != null)
            {
                pgcNode_l.SetActive(false);
                pgcNode_r.SetActive(false);
                ugcNode_l.SetActive(true);
                ugcNode_r.SetActive(true);
            }
        }
        //考虑资源释放
        public override void TakeOff()
        {
            if (ugcNode_l != null && ugcNode_r != null)
            {
                ugcNode_l.transform.SetParent(null);
                GameObject.Destroy(ugcNode_l);
                ugcNode_l = null;
                ugcNode_r.transform.SetParent(null);
                GameObject.Destroy(ugcNode_r);
                ugcNode_r = null;
            }
        }
        public override void RemoveEffect()
        {
        }
    }
}
