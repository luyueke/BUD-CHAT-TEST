using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BUD.AnimPose;
using Es;
using Game.Config;
using Game.KinematicCharacter;
using Game.Pet;
using GameData.PgcData;
using GameData.UGCData;
using Message;
using Pb.Base;
using RootMotion.FinalIK;
using UnityEngine;
using xasset;
using Debug = UnityEngine.Debug;

namespace Game.Avatar
{
    public class SpecialSkinHatsPartAdapter : HatsPartAdapter, SpecialSkinPart
    {
        private string special_hats_path { get; set; } = "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/Bip001 Neck/Bip001 Head/special_hat";
        private GameObject footEff;
        public SpecialSkinHatsPartAdapter(GameObject avatar) : base(avatar)
        {

        }

        public override void PutOn(string id, Action action)
        {
            base.PutOn(id, action);
            if (id == "10900505")
            {
                //特殊道具脚印
                if (footEff == null)
                {
                    var path = "Assets/Loadable/Avatar/DefaultSkin/Hats/hat_553_01/ani/eff/eff_hat_533_jiaoyin.prefab";
                    LoadRes<GameObject>(path, (isSuc, warpper) =>
                    {
                        if (isSuc && warpper != null && curPartId == id && avatar != null)
                        {
                            var hatPrefab = warpper.RetainAsset(avatar);
                            footEff = GameObject.Instantiate(hatPrefab.gameObject, avatar.transform);
                            lod?.SetLodMesh(footEff);
                        }
                    });
                }
            }
        }

        public override void TakeOff()
        {
            base.TakeOff();
            if (footEff != null)
            {
                GameObject.Destroy(footEff);
                footEff = null;
            }
        }

        protected override void GetHatBone()
        {
            hatBone = avatar.transform.Find(special_hats_path).gameObject;
        }

        public GameObject GetAniRoot()
        {
            return curHat;
        }
    }

    public class HatsPartAdapter : StandardPartAdapter
    {
        private const string hats_path = "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/Bip001 Neck/Bip001 Head/hat";
        private const string hats_parent_path = "hat_01_x";
        private GameObject hatParent;
        protected GameObject hatBone;
        protected GameObject curHat;
        private AssetWrapper<GameObject> cWarpper;
        private Renderer hatRenderer;
        //TODO:做UGC时候可去掉
        private List<GameObject> ugcHats;
        private bool isColor = false;
        public HatsPartAdapter(GameObject avatar) : base(avatar)
        {
            GetHatBone();
            hatParent = hatBone.transform.Find(hats_parent_path).gameObject;
            ugcHats = GetChildNodes(hatParent, "hat_");
            ugcHats.ForEach(x => x.gameObject.SetActive(false));
        }

        protected virtual void GetHatBone()
        {
            hatBone = avatar.transform.Find(hats_path).gameObject;
        }

        private List<GameObject> GetChildNodes(GameObject nodeParent, string compareStr)
        {
            List<GameObject> nodes = new List<GameObject>();
            for (int i = 0; i < nodeParent.transform.childCount; i++)
            {
                var node = nodeParent.transform.GetChild(i);
                if (node.name.Contains(compareStr))
                {
                    nodes.Add(node.gameObject);
                }
            }
            return nodes;
        }

        public override void Move(Vector3 pos)
        {
            hatBone.transform.localPosition = pos;
        }

        public override void Rotate(Vector3 rot)
        {
            hatParent.transform.localEulerAngles = rot;
        }

        public override void Scale(Vector3 sca)
        {
            hatBone.transform.localScale = sca;
        }

        public override void ChangeColor(Color col)
        {
            if (isColor && hatRenderer != null)
            {
                if (col != new Color(0.000f, 0.000f, 0.000f, 0.000f))
                {
                    hatRenderer.material.SetColor("_BaseColor", col);
                }
            }
        }

        public override void AddEffect()
        {
        }

        public override void RemoveEffect()
        {
        }

        public override void PutOn(string id, Action action)
        {
            curPartId = id;
            var bData = Es.DataTables.GetAvatarCommonData(id);
            if (bData == null)
            {
                action?.Invoke();
                return;
            }
            isColor = bData.setColor;
            LoadRes<GameObject>(bData.texDir + bData.prefabName + prefabExt, (isSuc, warpper) =>
            {
                if (isSuc && warpper != null && curPartId == id && avatar != null)
                {
                    TakeOff();
                    cWarpper = warpper;
                    var hatPrefab = warpper.RetainAsset(avatar);
                    var hatNode = hatPrefab.transform.Find(hats_path + "/" + hats_parent_path);
                    var meshNode = GetComponent<MeshRenderer>(hatNode.gameObject);
                    curHat = GameObject.Instantiate(meshNode.gameObject, hatParent.transform);
                    lod?.SetLodMesh(curHat);
                    hatRenderer = curHat.GetComponent<MeshRenderer>();
                    ChangeColor(curColor);
                    action?.Invoke();
                }
                else
                {
                    action?.Invoke();
                }
            });
        }


        public override void TakeOff()
        {
            if (curHat != null)
            {
                GameObject.Destroy(curHat);
                curHat = null;
            }
            if (cWarpper != null)
            {
                cWarpper = null;
            }
        }
    }

    public class NosePartAdapter : FaceSinglePartAdapter
    {
        private Transform bone_child_Nose;
        public NosePartAdapter(GameObject head) : base(head)
        {
            face_Part = head.transform.Find("nose").gameObject;
            bone_child_Nose = face_Part.transform.Find("nose_x");
            partRenderer = bone_child_Nose.Find("body_nose").GetComponent<MeshRenderer>();
            orgMat = partRenderer.material;
            curMat = orgMat;
        }

        public override void Move(Vector3 pos)
        {
            face_Part.transform.localPosition = pos;
        }

        public override void HVScale(Vector3 sca)
        {
            bone_child_Nose.transform.localScale = sca;
        }

        public override void Scale(Vector3 sca)
        {
            face_Part.transform.localScale = sca;
        }
    }
    public class UGCMouthPartAdapter : FaceSinglePartAdapter
    {
        private string curUrl = string.Empty;
        public UGCMouthPartAdapter(GameObject head) : base(head)
        {
            face_Part = head.transform.Find("mouth").gameObject;
            pgcPart = face_Part.transform.Find("body_mouth").gameObject;
        }
        private Action putOnSuccess;
        public override void Move(Vector3 pos)
        {
            face_Part.transform.localPosition = pos;
        }

        public override void Rotate(Vector3 rot)
        {
            face_Part.transform.localEulerAngles = rot;
        }

        public override void Scale(Vector3 sca)
        {
            face_Part.transform.localScale = sca;
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
            LoadRes<GameObject>(GameConsts.ClothesAssetDir + bData.prefabName + prefabExt, (isSuc, warpper) =>
            {
                if (isSuc && warpper != null && id == curPartId && avatar != null)
                {
                    TakeOff();
                    var partPrefab = warpper.RetainAsset(avatar);
                    var partNode = GameObjectEx.FindChildByName(partPrefab, "mouth");
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

        public override void UGCPutOn(string id, string url, int ugcStyle, Action suc)
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

                Action<bool, UGCTempPartRemoteWrapper> completed = (success, wrapper) =>
                {
                    if (!success || curPartId != id || curUrl != url || curPart == null)
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
                UGCPartLoader.LoadUGCPartRemoteImageAsync((int)AvatarSubType.Mouth, textureCount, url, completed);
            };
            PutOn(id, callback);
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

        public override void Reset()
        {

        }
    }
    public class MousePartAdapter : FaceSinglePartAdapter
    {
        public MousePartAdapter(GameObject head) : base(head)
        {
            face_Part = head.transform.Find("mouth").gameObject;
            partRenderer = face_Part.transform.Find("body_mouth").gameObject.GetComponent<MeshRenderer>();
            orgMat = partRenderer.material;
            curMat = orgMat;
        }

        public override void Move(Vector3 pos)
        {
            face_Part.transform.localPosition = pos;
        }

        public override void Rotate(Vector3 rot)
        {
            face_Part.transform.localEulerAngles = rot;
        }

        public override void Scale(Vector3 sca)
        {
            face_Part.transform.localScale = sca;
        }
    }

    public class FaceSinglePartAdapter : StandardPartAdapter
    {
        protected Material orgMat;
        protected Material curMat;
        protected MeshRenderer partRenderer;
        protected GameObject face_Part;
        protected Texture partTex;
        protected AssetWrapper<Texture> cWarpper;

        protected GameObject curPart;
        protected GameObject pgcPart;

        public FaceSinglePartAdapter(GameObject _avatar) : base(_avatar)
        {

        }

        //用于控制嘴巴动画是否切换UGC
        public void SetUGCMouthBoneShow(string clipName)
        {
            if (curPart != null)
            {
                bool isSwitchUGC = !clipName.Contains("ugcmouthignore") && !clipName.Contains("ugcignore");
                pgcPart.SetActive(isSwitchUGC);
                curPart.SetActive(!isSwitchUGC);
            }
        }


        public override void AddEffect()
        {
        }

        public override void RemoveEffect()
        {
        }

        public override void PutOn(string id, Action action)
        {
            curPartId = id;
            var bData = Es.DataTables.GetAvatarCommonData(id);
            if (bData == null)
            {
                action?.Invoke();
                return;
            }
            LoadRes<Texture>(bData.texDir + bData.texName[0] + texExt, (isSuc, warpper) =>
            {
                if (isSuc && warpper != null && curPartId == id && avatar != null)
                {
                    TakeOff();
                    cWarpper = warpper;
                    partRenderer.gameObject.SetActive(true);
                    partTex = warpper.RetainAsset(avatar);
                    orgMat.SetTexture("_BaseMap", partTex);
                    action?.Invoke();
                }
                else
                {
                    action?.Invoke();
                }
            });
        }

        public override void TakeOff()
        {
            if (cWarpper != null)
            {
                cWarpper = null;
            }
            partRenderer.gameObject.SetActive(false);
        }

        public override void Reset()
        {
            base.Reset();
            partRenderer.material = orgMat;

            partRenderer.gameObject.SetActive(curPartId != "0" && !string.IsNullOrEmpty(curPartId));
        }
    }

}