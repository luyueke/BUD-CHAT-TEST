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
    public abstract class BonePartAdapter : StandardPartAdapter
    {
        protected AssetWrapper<GameObject> cWarpper;

        public virtual GameObject curPart
        {
            get;
            set;
        }

        public BonePartAdapter(GameObject avatar, Dictionary<string, Transform> bones) : base(avatar, bones)
        {
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
            Stopwatch watch = new Stopwatch();
            watch.Start();
            LoadRes<GameObject>(bData.texDir + bData.prefabName + prefabExt, (isSuc, warpper) =>
            {
                watch.Stop();
                //避免频繁切换异常
                if (isSuc && warpper != null && id == curPartId && avatar != null)
                {
                    TakeOff();
                    cWarpper = warpper;
                    var partPrefab = warpper.RetainAsset(avatar);
                    var meshNode = GetComponent<SkinnedMeshRenderer>(partPrefab);
                    curPart = GameObject.Instantiate(meshNode.gameObject, avatar.transform);
                    lod?.SetLodMesh(curPart);
                    UpdateBones(curPart);
                    PutOnFinish();
                    ChangeColor(curColor);
                    action?.Invoke();
                }
                else
                {
                    action?.Invoke();
                }
            });
        }

        public virtual void PutOnFinish()
        {

        }

        //考虑资源释放
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
    }

    public class UGCClothesPartAdapter : BonePartAdapter
    {
        public GameObject underWear;
        public GameObject defCloth;

        private string curUrl = string.Empty;

        public UGCClothesPartAdapter(GameObject avatar, Dictionary<string, Transform> bones) : base(avatar, bones)
        {
            underWear = avatar.transform.Find("body_underwear")?.gameObject;
            defCloth = avatar.transform.Find("body_clothing")?.gameObject;
        }

        public override void PutOn(string id, Action action)
        {
            PutOn(id, suc => action?.Invoke());
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

            LoadRes<GameObject>(GameConsts.ClothesAssetDir + bData.prefabName + prefabExt,
                (isSuc, warpper) =>
                {
                    if (isSuc && warpper != null && id == curPartId && avatar != null)
                    {
                        TakeOff();
                        cWarpper = warpper;
                        var partPrefab = cWarpper.RetainAsset(avatar);
                        curPart = GameObject.Instantiate(partPrefab, avatar.transform);
                        curPart.name = GameConsts.clothMeshNodeName;
                        lod?.SetLodMesh(curPart);
                        UpdateBones(curPart);
                        callback?.Invoke(true);
                        defCloth.SetActive(false);
                    }
                    else
                    {
                        callback?.Invoke(false);
                    }
                });
        }

        public override void ResetCurrentID()
        {
            base.ResetCurrentID();
            curUrl = string.Empty;
        }

        public override void UGCPutOn(string id, string url, int ugcStyle, Action suc)
        {
            underWear.SetActive(true);
            if (curPart == null)
            {
                defCloth.SetActive(true);
            }

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
                            matTextureName = ugcStyle == (int)UgcShaderStyle.Anime ? "_BaseMap" : "_MainTex";
                            ugcType = int.Parse(nameData[0]);
                        }
                        else
                        {
                            ugcType = int.Parse(nameData[0]);
                            matTextureName = "_opacity_texmask";
                        }

                        UgcPartData ugcdata = UgcPartDataManager.Inst.GetUgcPartData(id, ugcType);
                        var partsGo = curPart.transform.Find(ugcdata.partsName);
                        var renderer = partsGo.GetComponent<SkinnedMeshRenderer>();
                        var partsMat = SetPartMaterial(renderer, ugcStyle);
                        partsMat.SetTexture(matTextureName, kValue.Value);
                    }

                    suc?.Invoke();
                };
                var textureCount = UgcPartDataManager.Inst.GetUgcTextureCount(id);
                UGCPartLoader.LoadUGCPartRemoteImageAsync((int)AvatarSubType.Clothes, textureCount, url, completed);
            };
            PutOn(id, callback);
        }
    }

    public class ClothesPartAdapter : BonePartAdapter
    {
        public GameObject underWear;
        public GameObject defCloth;
        public ClothesPartAdapter(GameObject avatar, Dictionary<string, Transform> bones) : base(avatar, bones)
        {
            underWear = avatar.transform.Find("body_underwear")?.gameObject;
            defCloth = avatar.transform.Find("body_clothing")?.gameObject;
        }

        public override void PutOn(string id, Action action)
        {
            if (curPart == null)
            {
                defCloth.SetActive(true);
            }
            base.PutOn(id, action);
        }

        public override void PutOnFinish()
        {
            underWear.SetActive(false);
            defCloth.SetActive(false);
        }

        public override void TakeOff()
        {
            base.TakeOff();
            if (underWear != null)
            {
                underWear.SetActive(true);
            }
        }
    }

    public class ShoePartAdapter : BonePartAdapter
    {
        protected GameObject part;
        private AssetWrapper<GameObject> warpper;
        private GameObject curShoes;
        public ShoePartAdapter(GameObject avatar, Dictionary<string, Transform> bones) : base(avatar, bones)
        {

        }
    }

    public class UGCShoePartAdapter : BonePartAdapter
    {
        private string curUrl = string.Empty;
        protected GameObject part;
        private AssetWrapper<GameObject> warpper;
        private GameObject curShoes;

        public UGCShoePartAdapter(GameObject avatar, Dictionary<string, Transform> bones) : base(avatar, bones)
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

            LoadRes<GameObject>(GameConsts.ClothesAssetDir + bData.prefabName + prefabExt,
                (isSuc, warpper) =>
                {
                    if (isSuc && warpper != null && id == curPartId && avatar != null)
                    {
                        TakeOff();
                        cWarpper = warpper;
                        var partPrefab = cWarpper.RetainAsset(avatar);
                        curPart = GameObject.Instantiate(partPrefab, avatar.transform);
                        curPart.name = GameConsts.shoeMeshNodeName;
                        lod?.SetLodMesh(curPart);
                        UpdateBones(curPart);
                        action?.Invoke(true);
                    }
                    else
                    {
                        action?.Invoke(false);
                    }
                });
        }

        public override void ResetCurrentID()
        {
            base.ResetCurrentID();
            curUrl = string.Empty;
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
                        var renderer = partsGo.GetComponent<SkinnedMeshRenderer>();
                        var partsMat = SetPartMaterial(renderer, ugcStyle);
                        partsMat.SetTexture(MatTextureName, kValue.Value);
                    }
                    suc?.Invoke();
                };
                var textureCount = UgcPartDataManager.Inst.GetUgcTextureCount(id);
                UGCPartLoader.LoadUGCPartRemoteImageAsync((int)AvatarSubType.Shoe, textureCount, url, completed);
            };
            PutOn(id, callback);

        }
    }

    public class HairPartAdapter : BonePartAdapter
    {
        public HairPartAdapter(GameObject avatar, Dictionary<string, Transform> bones) : base(avatar, bones)
        {
        }

        public override void ChangeColor(Color col)
        {
            base.ChangeColor(col);
            if (curPart != null)
            {
                var matComp = curPart.GetComponent<Renderer>().material;
                matComp.SetColor("_BaseColor", col);
            }
        }
    }

    public class UGCHairPartAdapter : BonePartAdapter, IUGCPartAdapter
    {

        private const string hair_path = "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/Bip001 Neck/Bip001 Head/ugc_hair";
        private const string hair_parent_path = "ugc_hair_x";

        public UGCHairPartAdapter(GameObject avatar, Dictionary<string, Transform> bones) : base(avatar, bones)
        {
            propSkinRoot = avatar.transform.Find($"{hair_path}/{hair_parent_path}")?.gameObject;
        }

        public override void Move(Vector3 pos)
        {
            base.Move(pos);
            propSkinRoot.transform.localPosition = pos;
        }

        public override void Rotate(Vector3 rot)
        {
            base.Rotate(rot);
            propSkinRoot.transform.localEulerAngles = rot;
        }

        public override void Scale(Vector3 sca)
        {
            propSkinRoot.transform.localScale = sca;
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

}