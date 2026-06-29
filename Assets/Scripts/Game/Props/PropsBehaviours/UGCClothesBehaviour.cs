using System;
using System.IO;
using System.Linq;
using Es;
using Game.AvatarTool;
using Game.Base;
using Game.Config;
using Game.Props.PropsComponents;
using GameData.Base;
using GameData.BaseInfo;
using UGCAsset;
using UIAgent;
using UnityEngine;

namespace Game.Props.PropsBehaviours
{
    public class UGCClothesBehaviour : NodeBaseBehaviour
    {
        private GameObject defaultObj;
        private GameObject clothesObj;
        private UGCClothesComponent clothesComp;


        public override void OnInitByCreate()
        {
            base.OnInitByCreate();
            defaultObj = transform.Find("default").gameObject;
            clothesComp = entity.GetOrAddComp<UGCClothesComponent>();
            RefreshClothes();
        }
        public void SetClothesInfo(SkinInfo clothesInfo)
        {
            clothesComp.id = clothesInfo.id;
            clothesComp.cover = clothesInfo.cover;
            clothesComp.templateId = clothesInfo.templateId;
            clothesComp.metaDataUrl = clothesInfo.metaDataUrl;
            clothesComp.clothesUrl = clothesInfo.clothesUrl;
            RefreshClothes();
        }

        public override void OnTouchClick()
        {
            base.OnTouchClick();
        }

        private void RefreshClothes()
        {
            if (string.IsNullOrEmpty(clothesComp.id))
            {
                defaultObj.SetActive(true);
                if (clothesObj != null)
                {
                    Destroy(clothesObj);
                    clothesObj = null;
                }
                return;
            }
            defaultObj.SetActive(false);
            if (clothesObj != null)
            {
                Destroy(clothesObj);
            }
            if (!clothesComp.isPgc)
            {
                RefreshUGCClothes();
            }
        }

        private void RefreshUGCClothes()
        {
            var templateCfg = Es.DataTables.GetClothesTemplate(clothesComp.templateId);
            var model = Loader.Load<GameObject>(GameConsts.ClothesAssetDir +templateCfg.ClothModelPath);
            clothesObj = model.Instantiate(transform);
            clothesObj.name = "UGCClothModel";
            clothesObj.transform.localPosition = Vector3.up * -0.5f;
            clothesObj.transform.localScale = Vector3.one * 2.0f;
            clothesObj.transform.localRotation = Quaternion.Euler(0, 180, 0);

            var curPart = clothesObj.transform.Find("UGCclothing");

            UgcPartCachePool.Pool.GetUgcPart(clothesComp.templateId,clothesComp.clothesUrl, gameObject, (dic) =>
            {
                foreach (var kValue in dic)
                {
                    string nameWithoutEx = Path.GetFileNameWithoutExtension(kValue.Key);
                    string[] nameData = nameWithoutEx.Split('_');
                    int ugcType; //前后左右
                    string matTextureName;
                    if (!nameData.Contains("alpha"))
                    {
                        matTextureName = "_MainTex";
                        ugcType = int.Parse(nameData[0]);
                    }
                    else
                    {
                        ugcType = int.Parse(nameData[0]);
                        matTextureName = "_opacity_texmask";
                    }
                    var ugcData = UgcPartDataManager.Inst.GetUgcPartData(clothesComp.templateId, ugcType);
                    var partsGo = curPart.transform.Find(ugcData.partsName);
                    var partsMat = partsGo.GetComponent<SkinnedMeshRenderer>().material;
                    partsMat.SetTexture(matTextureName, kValue.Value);
                }
            });
        }

        private void OnDestroy()
        {
            if (UgcPartCachePool.HasInstance)
            {
                UgcPartCachePool.Pool.DisposeTexture(gameObject);
            }
            
        }
    }
}