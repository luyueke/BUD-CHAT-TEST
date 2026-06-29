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
    public class UGCEyePartAdapter : StandardPartAdapter
    {
        private string curUrl = string.Empty;
        protected GameObject part_l;
        protected GameObject part_r;
        private Transform bone_Rot_L;
        private Transform bone_Rot_R;
        public UGCEyePartAdapter(GameObject Avatar, GameObject head) : base(head)
        {
            part_l = head.transform.Find("eye_l").gameObject;
            part_r = head.transform.Find("eye_r").gameObject;
            bone_Rot_L = part_l.transform.Find("eye_l_01");
            bone_Rot_R = part_r.transform.Find("eye_r_01");
            pgcNode_l = bone_Rot_L.Find("body_eye_l").gameObject;
            pgcNode_r = bone_Rot_R.Find("body_eye_r").gameObject;

        }
        public override void ResetCurrentID()
        {
            base.ResetCurrentID();
            curUrl = string.Empty;
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

        public override void AddEffect()
        {
        }

        public override void RemoveEffect()
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
            LoadRes<GameObject>(GameConsts.ClothesAssetDir + bData.prefabName + prefabExt, (isSuc, warpper) =>
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
    }

    public class EyePartAdapter : StandardPartAdapter
    {
        protected GameObject part_l;
        protected GameObject part_r;
        protected GameObject node_l;
        protected GameObject node_r;
        private Transform bone_Rot_L;
        private Transform bone_Rot_R;

        private MeshRenderer renderer_L;
        private MeshRenderer renderer_R;
        // 记录原始材质球，用于 Reset 时还原
        private Material orgMat_L;
        private Material orgMat_R;
        private Texture l_Tex;
        private Texture r_Tex;
        private PlayerAnimationCtrl playerAnimationCtrl;

        public EyePartAdapter(GameObject Avatar, GameObject head) : base(head)
        {
            part_l = head.transform.Find("eye_l").gameObject;
            part_r = head.transform.Find("eye_r").gameObject;
            bone_Rot_L = part_l.transform.Find("eye_l_01");
            bone_Rot_R = part_r.transform.Find("eye_r_01");
            node_l = bone_Rot_L.Find("body_eye_l").gameObject;
            node_r = bone_Rot_R.Find("body_eye_r").gameObject;
            renderer_L = node_l.GetComponent<MeshRenderer>();
            renderer_R = node_r.GetComponent<MeshRenderer>();
            // 在构造阶段记录原始材质球，供 Reset 还原时使用
            orgMat_L = renderer_L.material;
            orgMat_R = renderer_R.material;
            playerAnimationCtrl = Avatar.GetComponent<PlayerAnimationCtrl>();
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
            LoadRes<Texture>(bData.aniPath + "_1_left.png", (isSuc, lWarpper) =>
            {
                if (isSuc && lWarpper != null && curPartId == id && part_l != null)
                {
                    l_Tex = lWarpper.RetainAsset(part_l);
                    if (l_Tex == null)
                    {
                        Debug.LogError($"id {id} lTex is not exist");
                        action?.Invoke();
                        return;
                    }
                    renderer_L.material.SetTexture("_BaseMap", l_Tex);
                    action?.Invoke();
                }
                else
                {
                    action?.Invoke();
                }
            });
            LoadRes<Texture>(bData.aniPath + "_1_right.png", (isSuc, rWarpper) =>
            {
                if (isSuc && rWarpper != null && curPartId == id && part_r != null)
                {
                    r_Tex = rWarpper.RetainAsset(part_r);
                    if (r_Tex == null)
                    {
                        Debug.LogError($"id {id} lTex is not exist");
                        return;
                    }
                    renderer_R.material.SetTexture("_BaseMap", r_Tex);
                }
            });

            LoadRes<AnimationClip>(bData.aniPath + ".anim", (isSuc, wrapper) =>
            {
                if (isSuc && wrapper != null && curPartId == id && avatar != null)
                {
                    node_l?.SetActive(true);
                    node_r?.SetActive(true);
                    var clip = wrapper.RetainAsset(avatar);
                    if (clip != null && playerAnimationCtrl != null)
                    {
                        AnimationClip clipA = AnimationClip.Instantiate(clip);
                        playerAnimationCtrl.LoadPlay(clipA, 1);
                    }
                    action?.Invoke();
                }
                else
                {
                    action?.Invoke();
                }
            });
        }

        //考虑资源释放
        public override void TakeOff()
        {
            if (node_l != null && node_r != null)
            {
                node_l.SetActive(false);
                node_r.SetActive(false);
            }
        }

        public override void Reset()
        {
            base.Reset();
            // 还原左右眼原始材质球，防止切换动作后眼睛贴图残留
            renderer_L.material = orgMat_L;
            renderer_R.material = orgMat_R;
            var active = curPartId != "0" && !string.IsNullOrEmpty(curPartId);
            node_l.SetActive(active);
            node_r.SetActive(active);
        }
    }


}