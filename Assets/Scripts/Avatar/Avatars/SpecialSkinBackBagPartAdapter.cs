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
    public class SpecialSkinBackBagPartAdapter : BackBagPartAdapter, SpecialSkinPart
    {
        private string special_hanging_path { get; set; } = "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/special_back";
        public SpecialSkinBackBagPartAdapter(GameObject avatar, Dictionary<string, Transform> bones) : base(avatar, bones)
        {
            hangingBone = avatar.transform.Find(special_hanging_path).gameObject;
            nodeParent = hangingBone;
        }

        public GameObject GetAniRoot()
        {
            return curPart;
        }

        public override void PutOn(string id, Action action)
        {
            base.PutOn(id, () =>
            {
                action?.Invoke();
                MessageHelper.Broadcast(MessageName.OnAvatarPutOnSuccess, avatar.GetHashCode(), id);
            });
        }
    }

    public class SpecialSkinEffectPartAdapter : EffectPartAdapter, SpecialSkinPart
    {
        public SpecialSkinEffectPartAdapter(GameObject avatar) : base(avatar, false)
        {

            bone_Effect = new GameObject("special_effect");
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

        public GameObject GetAniRoot()
        {
            return bone_Effect_x;
        }
    }

    public class BackBagPartAdapter : HangingPartAdapter
    {
        private bool isColor = false;

        protected override string hanging_path { get; set; } = "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/back";
        public BackBagPartAdapter(GameObject avatar, Dictionary<string, Transform> bones) : base(avatar, bones)
        {
            nodeParent = hangingBone;
        }


        public override void ChangeColor(Color col)
        {
            curColor = col;
            if (isColor && curPart)
            {
                var matComp = curPart.GetComponent<Renderer>().material;
                matComp.SetColor("_BaseColor", col);
            }
        }

        public override void PutOn(string id, Action action)
        {
            var bData = Es.DataTables.GetAvatarCommonData(id);
            if (bData == null)
            {
                action?.Invoke();
                return;
            }
            isColor = bData.setColor;
            base.PutOn(id, action);
        }

        public override void AddEffect()
        {
        }

        public override void RemoveEffect()
        {
        }
    }
    public class UGCBackBagPartAdapter : HangingPartAdapter, IUGCPartAdapter
    {
        protected override string hanging_path { get; set; } = "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/back";
        public UGCBackBagPartAdapter(GameObject avatar, Dictionary<string, Transform> bones) : base(avatar, bones)
        {
            nodeParent = hangingBone;
            propSkinRoot = hangingBone;
        }
        public override void ResetCurrentID()
        {
            base.ResetCurrentID();
            curUrl = string.Empty;
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
            LoadRes<GameObject>(GameConsts.ClothesAssetDir + bData.prefabName + prefabExt,
                (isSuc, warpper) =>
                {
                    if (isSuc && warpper != null && id == curPartId && avatar != null)
                    {
                        base.TakeOff();
                        cWarpper = warpper;
                        var bagPrefab = warpper.RetainAsset(avatar);
                        var bagNode = GameObjectEx.FindChildByName(bagPrefab, "back");
                        curPart = new GameObject(GameConsts.bagMeshNodeName);
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
                                var partObj = GameObject.Instantiate(GameObjectEx.FindChildByName(bagNode, ugcdataList[i].partsName), curPart.transform);
                                partObj.name = ugcdataList[i].partsName;
                            }
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
                UGCPartLoader.LoadUGCPartRemoteImageAsync((int)AvatarSubType.Backpack, textureCount, url, completed);
            };
            PutOn(id, callback);
        }




        public override void AddEffect()
        {
        }

        public override void RemoveEffect()
        {
        }

        public string curUrl
        {
            get;
            set;
        } = string.Empty;


        public override GameObject curPart
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
    public class CrossBagPartAdapter : BonePartAdapter
    {
        public CrossBagPartAdapter(GameObject avatar, Dictionary<string, Transform> bones) : base(avatar, bones)
        {
        }

    }
    public class UGCFacePaintPartAdapter : StandardPartAdapter
    {
        private string curUrl = string.Empty;
        private GameObject ugc_face;
        private MeshRenderer skinRenderer;
        private readonly int baseMapName = Shader.PropertyToID("_patterns_tex");

        public UGCFacePaintPartAdapter(GameObject face) : base(face)
        {
            ugc_face = GameObjectEx.FindChildByName(face, "ugc_face").gameObject;
            skinRenderer = ugc_face.GetComponent<MeshRenderer>();
        }
        public override void AddEffect()
        {

        }

        public override void RemoveEffect()
        {

        }
        public override void Move(Vector3 pos)
        {
            skinRenderer.material.SetTextureOffset(baseMapName, pos);
        }

        public override void Scale(Vector3 sca)
        {
            skinRenderer.material.SetFloat("_patterms_size", sca.x);
        }
        public override void UGCPutOn(string id, string url, int ugcStyle, Action suc)
        {
            curPartId = id;
            curUrl = url;

            if (string.IsNullOrEmpty(url))
            {
                suc?.Invoke();
                return;
            }

            Action<bool, UGCTempPartRemoteWrapper> completed = (success, wrapper) =>
            {
                if (!success || curPartId != id || curUrl != url || ugc_face == null)
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

            var textureCount = UgcPartDataManager.Inst.GetUgcTextureCount(id);
            UGCPartLoader.LoadUGCPartRemoteImageAsync((int)AvatarSubType.FacePaint, textureCount, url, completed);
        }
        public override void PutOn(string id, Action action)
        {
            if (ugc_face != null)
            {
                ugc_face.SetActive(true);
            }
            action?.Invoke();
        }

        public override void TakeOff()
        {
            if (ugc_face != null)
            {
                ugc_face.SetActive(false);
            }


        }
    }
    public class FacePaintPartAdapter : StandardPartAdapter
    {
        private Texture faceTex;
        public SkinnedMeshRenderer skinRenderer;
        protected AssetWrapper<Texture> cWarpper;
        private Texture defaultTex;
        private readonly int baseMapName = Shader.PropertyToID("_BaseMap");

        public FacePaintPartAdapter(GameObject face) : base(face)
        {
            skinRenderer = face.transform.Find("body_face").GetComponent<SkinnedMeshRenderer>();
            defaultTex = skinRenderer.material.GetTexture(baseMapName);
        }

        public override void AddEffect()
        {

        }

        public override void RemoveEffect()
        {

        }
        public override void Move(Vector3 pos)
        {
            skinRenderer.material.SetTextureOffset(baseMapName, pos);
        }

        public override void Scale(Vector3 sca)
        {
            skinRenderer.material.SetFloat("_patterms_size", sca.x);
        }

        public override void PutOn(string id, Action action)
        {
            var bData = Es.DataTables.GetAvatarCommonData(id);
            if (bData == null)
            {
                action?.Invoke();
                return;
            }
            var warpper = Loader.Load<Texture>(bData.texDir + bData.texName[0] + texExt);
            if (warpper == null)
            {
                action?.Invoke();
                return;
            }

            curPartId = id;
            warpper.completed = (isSuc) =>
            {
                if (isSuc && curPartId == id && avatar != null)
                {
                    TakeOff();
                    cWarpper = warpper;
                    faceTex = warpper.RetainAsset(avatar);
                    skinRenderer.material.SetTexture(baseMapName, faceTex);
                    skinRenderer.material.SetColor("_patterns_color", Color.white);
                    action?.Invoke();
                }
                else
                {
                    action?.Invoke();
                }
            };
        }

        public override void TakeOff()
        {
            skinRenderer.material.SetTexture(baseMapName, defaultTex);
            if (cWarpper != null)
            {
                cWarpper = null;
            }
        }
    }
    public class VisorPartAdapter : GlassesPartAdapter
    {
        protected override string hanging_path { get; set; } = "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/Bip001 Neck/Bip001 Head/mask";
        public VisorPartAdapter(GameObject avatar, Dictionary<string, Transform> bones) : base(avatar, bones)
        {
        }
    }
}