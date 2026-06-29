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
    public class GlassesPartAdapter : HangingPartAdapter
    {
        protected override string hanging_path { get; set; } = "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/Bip001 Neck/Bip001 Head/glasses";
        private bool isColor = false;
        public GlassesPartAdapter(GameObject avatar, Dictionary<string, Transform> bones) : base(avatar, bones)
        {
            nodeParent = hangingBone;
        }

        public override void ChangeColor(Color col)
        {
            if (isColor && curPart)
            {
                var matComp = curPart.GetComponent<Renderer>().material;
                matComp.SetColor("_BaseColor", col);
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
            var bData = Es.DataTables.GetAvatarCommonData(id);
            if (bData == null) return;
            isColor = bData.setColor;
            base.PutOn(id, action);
        }
    }

    public class UGCGlassesPartAdapter : HangingPartAdapter, IUGCPartAdapter
    {
        protected override string hanging_path { get; set; } = "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/Bip001 Neck/Bip001 Head/glasses";
        private bool isColor = false;
        public UGCGlassesPartAdapter(GameObject avatar, Dictionary<string, Transform> bones) : base(avatar, bones)
        {
            nodeParent = hangingBone;
            propSkinRoot = hangingBone;
        }
        private Action putOnSuccess;
        public override void ResetCurrentID()
        {
            base.ResetCurrentID();
            curUrl = string.Empty;
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
                    cWarpper = warpper;
                    var glassesPrefab = warpper.RetainAsset(avatar);
                    var glassesNode = GameObjectEx.FindChildByName(glassesPrefab, hanging_path);
                    curPart = new GameObject(GameConsts.glassesMeshNodeName);
                    curPart.transform.SetParent(nodeParent.transform);
                    curPart.transform.localPosition = Vector3.zero;
                    curPart.transform.localRotation = Quaternion.identity;
                    curPart.transform.localScale = Vector3.one;
                    lod?.SetLodMesh(curPart);
                    var ugcdataList = UgcPartDataManager.Inst.GetUgcPartDataList(id);
                    if (ugcdataList != null)
                    {
                        for (int i = 0; i < ugcdataList.Count; i++)
                        {
                            var partObj = GameObject.Instantiate(GameObjectEx.FindChildByName(glassesNode, ugcdataList[i].partsName), curPart.transform);
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
                        string MatTextureName = string.Empty;
                        if (!nameData.Contains("alpha"))
                        {
                            MatTextureName = ugcStyle == (int)UgcShaderStyle.Anime ? "_BaseMap" : "_MainTex";
                            ugcType = int.Parse(nameData[0]);
                        }
                        else
                        {
                            ugcType = int.Parse(nameData[0]);
                            MatTextureName = "_opacity_texmask";
                        }

                        UgcPartData ugcdata = UgcPartDataManager.Inst.GetUgcPartData(id, ugcType);
                        var partsGo = curPart.transform.Find(ugcdata.partsName);
                        var renderer = partsGo.GetComponent<MeshRenderer>();
                        var partsMat = SetPartMaterial(renderer, ugcStyle);
                        partsMat.SetTexture(MatTextureName, kValue.Value);
                    }

                    suc?.Invoke();
                };
                var textureCount = UgcPartDataManager.Inst.GetUgcTextureCount(id);
                UGCPartLoader.LoadUGCPartRemoteImageAsync((int)AvatarSubType.Glasses, textureCount, url, completed);
            };
            PutOn(id, callback);
        }

        public string curUrl
        {
            get;
            set;
        }

        public GameObject propSkinRoot
        {
            get;
            set;
        }
    }

    public class BeltPartAdapter : HangingPartAdapter
    {
        protected override string hanging_path { get; set; } = "Bip001/Bip001 Pelvis/Bip001 Spine/waist/waist_x";
        public BeltPartAdapter(GameObject avatar, Dictionary<string, Transform> bones) : base(avatar, bones)
        {
            nodeParent = hangingBone;
        }


        public override void AddEffect()
        {
        }

        public override void RemoveEffect()
        {
        }


        public override void Move(Vector3 pos)
        {
            hangingBone.transform.parent.localPosition = pos;
        }

        public override void Scale(Vector3 sca)
        {
            hangingBone.transform.parent.localScale = sca;
        }

        public override void PutOn(string id, Action action)
        {
            base.PutOn(id, action);
            //UpdateBones(curPart);
        }
    }

    public class EffectPartAdapter : StandardPartAdapter
    {
        private const string effect_path = "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/Bip001 Neck/Bip001 Head";
        protected GameObject bone_Effect;
        protected GameObject hangingBone;
        protected GameObject curHanging;
        protected GameObject bone_Effect_x;
        protected GameObject meshNodeGo;
        protected AssetWrapper<GameObject> cWarpper;

        public EffectPartAdapter(GameObject avatar, bool isCreateNode = true) : base(avatar)
        {
            hangingBone = avatar.transform.Find(effect_path).gameObject;
            if (!isCreateNode)
            {
                return;
            }
            //pos、scale handle
            bone_Effect = new GameObject("effect");
            bone_Effect.transform.SetParent(hangingBone.transform);
            bone_Effect.transform.localPosition = Vector3.zero;
            bone_Effect.transform.localScale = Vector3.one;
            bone_Effect.transform.localEulerAngles = Vector3.zero;

            bone_Effect_x = new GameObject("effect_x");
            bone_Effect_x.transform.SetParent(bone_Effect.transform);
            bone_Effect_x.transform.localPosition = Vector3.zero;
            bone_Effect_x.transform.localScale = Vector3.one;
            bone_Effect_x.transform.localEulerAngles = Vector3.zero;

        }

        public override void Move(Vector3 pos)
        {
            bone_Effect.transform.localPosition = pos;
        }

        public override void Rotate(Vector3 rot)
        {
            bone_Effect_x.transform.localEulerAngles = rot;
        }

        public override void Scale(Vector3 sca)
        {
            bone_Effect.transform.localScale = sca;
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
            LoadRes<GameObject>(bData.texDir + bData.prefabName + prefabExt, (isSuc, warpper) =>
            {
                if (isSuc && warpper != null && curPartId == id && avatar != null)
                {
                    TakeOff();
                    cWarpper = warpper;
                    var bagPrefab = warpper.RetainAsset(avatar);
                    var bagNode = bagPrefab.transform.Find(effect_path + "/effect");
                    meshNodeGo = GetComponent<Transform>(bagNode.gameObject).gameObject;
                    curHanging = GameObject.Instantiate(meshNodeGo.transform.parent.gameObject, bone_Effect_x.transform);
                    lod?.SetLodMesh(curHanging);
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
            if (curHanging != null)
            {
                GameObject.Destroy(curHanging);
                curHanging = null;
            }
            if (cWarpper != null)
            {
                cWarpper = null;
            }
        }
    }
    public class UGCHatsPartAdapter : StandardPartAdapter, IUGCPartAdapter
    {
        private const string hats_path = "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/Bip001 Neck/Bip001 Head/hat";
        private const string hats_parent_path = "hat_01_x";
        private GameObject hatParent;
        private GameObject hatBone;
        private AssetWrapper<GameObject> cWarpper;
        private bool isColor = false;
        private const string specialName = "ugchat_2004_3";
        public UGCHatsPartAdapter(GameObject avatar) : base(avatar)
        {
            hatBone = avatar.transform.Find(hats_path).gameObject;
            hatParent = hatBone.transform.Find(hats_parent_path).gameObject;
            propSkinRoot = hatParent;
        }
        public override void Move(Vector3 pos)
        {
            hatBone.transform.localPosition = pos;
        }
        private Action putOnSuccess;
        public override void Rotate(Vector3 rot)
        {
            hatParent.transform.localEulerAngles = rot;
        }

        public override void Scale(Vector3 sca)
        {
            hatBone.transform.localScale = sca;
        }
        public override void AddEffect()
        {
        }
        public override void ResetCurrentID()
        {
            base.ResetCurrentID();
            curUrl = string.Empty;
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
                    cWarpper = warpper;
                    var hatPrefab = warpper.RetainAsset(avatar);
                    var hatNode = hatPrefab.transform.Find(hats_path + "/" + hats_parent_path);
                    curPart = new GameObject(GameConsts.hatMeshNodeName);
                    curPart.transform.SetParent(hatParent.transform);
                    curPart.transform.localPosition = Vector3.zero;
                    curPart.transform.localRotation = Quaternion.identity;
                    curPart.transform.localScale = Vector3.one;
                    lod?.SetLodMesh(curPart);
                    var ugcdataList = UgcPartDataManager.Inst.GetUgcPartDataList(id);
                    if (ugcdataList != null)
                    {
                        for (int i = 0; i < ugcdataList.Count; i++)
                        {
                            var partObj = GameObject.Instantiate(GameObjectEx.FindChildByName(hatNode, ugcdataList[i].partsName), curPart.transform);
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
                        string MatTextureName = string.Empty;
                        if (!nameData.Contains("alpha"))
                        {
                            MatTextureName = ugcStyle == (int)UgcShaderStyle.Anime ? "_BaseMap" : "_MainTex";
                            ugcType = int.Parse(nameData[0]);
                        }
                        else
                        {
                            ugcType = int.Parse(nameData[0]);
                            MatTextureName = "_opacity_texmask";
                        }

                        UgcPartData ugcdata = UgcPartDataManager.Inst.GetUgcPartData(id, ugcType);
                        var partsGo = curPart.transform.Find(ugcdata.partsName);
                        var renderer = partsGo.GetComponent<MeshRenderer>();

                        if (renderer.name.Contains(specialName) && MatTextureName.Equals("_BaseMap"))
                        {
                            MatTextureName = "_MainTex";
                        }
                        var partsMat = SetPartMaterial(renderer, ugcStyle);

                        partsMat.SetTexture(MatTextureName, kValue.Value);
                    }

                    suc?.Invoke();
                };
                var textureCount = UgcPartDataManager.Inst.GetUgcTextureCount(id);
                UGCPartLoader.LoadUGCPartRemoteImageAsync((int)AvatarSubType.Hats, textureCount, url, completed);
            };
            PutOn(id, callback);
        }

        protected override Material SetPartMaterial(Renderer renderer, int ugcStyle)
        {
            if (ugcStyle == (int)UgcShaderStyle.Anime && !renderer.name.Contains(specialName))
            {
                if (!renderer.material.name.Contains("UgcAnimeStyle"))
                {
                    var animeWrapper = Loader.Load<Material>("Assets/Arts/Game/BaseMatMaterial/UgcAnimeStyle.mat");
                    renderer.material = animeWrapper.Instantiate();
                }
            }
            return renderer.material;
        }

        public override void TakeOff()
        {
            if (curPart != null)
            {
                curPart.transform.SetParent(null);
                GameObject.Destroy(curPart);
                curPart = null;
            }
            if (cWarpper != null)
            {
                cWarpper = null;
            }
        }

        public string curUrl
        {
            get;
            set;
        }

        public GameObject curPart
        {
            get;
            set;
        }

        public GameObject propSkinRoot
        {
            get;
            set;
        }
    }

    public interface SpecialSkinPart
    {
        public abstract GameObject GetAniRoot();
    }

}