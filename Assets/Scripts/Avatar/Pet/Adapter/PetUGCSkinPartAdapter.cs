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

    /// <summary>
    /// PGC肤色适配器
    /// </summary>
    class PetUGCSkinPartAdapter : StandardPartAdapter {
        private static readonly int baseColor = Shader.PropertyToID("_BaseColor");
        private string matPath = "Assets/Loadable/Pet/PGCPart/Skins/UGCMat/";
        private string curUrl = string.Empty;
        private GameObject ugc_body;
        private readonly string baseMapName = "_MainTex";
        private readonly string baseAlphaMapName = "_opacity_texmask";
        private List<string> bodyPartNames = new List<string>() {"pet_ugcbody_1_1", "pet_ugcbody_1_2", "pet_ugcface_1_1", "pet_ugcface_1_2", "pet_ugchand_1_1", "pet_ugchand_1_2", "pet_ugcleg_1_1", "pet_ugcleg_1_2"};
        public PetUGCSkinPartAdapter(GameObject _avatar) : base(_avatar) {
            ugc_body = GameObjectEx.FindChildByName(_avatar, "UgcBody").gameObject;
           
         
        }

        public override void AddEffect() {

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

            Action<bool, UGCTempPartRemoteWrapper> completed = (success, wrapper) => {
                if (!success ||curPartId != id || curUrl != url || ugc_body == null)
                {
                    suc?.Invoke();
                    return;
                }

                var dic = wrapper.RetainAssets(ugc_body);

                if (dic == null)
                {
                    suc?.Invoke();
                    return;
                }
                PutOn(id, () =>
                {
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
                            matTextureName = baseAlphaMapName;
                        }
                        UgcPartData ugcdata = UgcPartDataManager.Inst.GetUgcPartData(id, ugcType);
                        var partsGo = GameObjectEx.FindChildByName(ugc_body,ugcdata.partsName);
                        var renderer = partsGo.GetComponent<SkinnedMeshRenderer>();
                        var partsMat = SetPartMaterial(renderer, ugcStyle);
                        partsMat.SetTexture(matTextureName, kValue.Value);
                    }
                    suc?.Invoke();
                });
            };
            var textureCount = UgcPartDataManager.Inst.GetUgcTextureCount(id);
            UGCPartLoader.LoadUGCPartRemoteImageAsync((int)(AvatarSubType.Skin), textureCount, url, completed);
        }
        public override void PutOn(string id, Action action) {
            List<string> paths = new List<string>();
            foreach (string texName in bodyPartNames) {
                paths.Add(matPath + texName + ".mat");
            }
            LoadRes<Material>(paths, (isSuccess, wrapperDic) => {
                if (isSuccess)
                {
                    var w  = wrapperDic;
                    foreach (var wrapper in  w) {
                        var fileName = Path.GetFileNameWithoutExtension(wrapper.Key);
                        var partsGo = GameObjectEx.FindChildByName(ugc_body,fileName);
                        partsGo.GetComponent<SkinnedMeshRenderer>().material = wrapper.Value.RetainAsset(avatar);
                    }
                }
                action?.Invoke();
            });
        }

        public override void RemoveEffect() {
        }
       
        public override void TakeOff() {
            
            // var ugcdatas = UgcPartDataManager.Inst.GetUgcPartDataList(curPartId);
            // if (ugcdatas!=null)
            // {
            //     for (int i = 0; i < ugcdatas.Count; i++)
            //     {
            //         var partsGo = GameObjectEx.FindChildByName(ugc_body,ugcdatas[i].partsName);
            //         var partsMat = partsGo.GetComponent<SkinnedMeshRenderer>().material;
            //         partsMat.SetTexture(baseMapName, null);
            //         partsMat.SetTexture(baseAlphaMapName, null);
            //     }
            // }
            
        }
    }
}


