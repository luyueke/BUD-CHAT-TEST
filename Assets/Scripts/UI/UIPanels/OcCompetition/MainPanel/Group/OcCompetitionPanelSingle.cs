using Game.Avatar;
using Game.COSXML;
using Game.Store;
using Game.Utils;
using GameData;
using GameData.BaseInfo;
using GameData.PgcData;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UI.UIPanels.CommonConfirm;
using UI.UIPanels.FittingRoom;
using UnityEngine;

namespace GameUI
{
    public class OcCompetitionPanelSingle : MonoBehaviour
    {
        public Transform ItemParent;

        public FittingRoomItem Item;

        public OcCptWorkItem WorkItem;

        OcCompetitionSystemData data => OcCompetitionSystem.Inst.data;

        [HideInInspector] public OcCompetitionPanelMy Root;

        [HideInInspector] public OcCptListMsgItem ItemData;

        [HideInInspector] public List<FittingRoomItem> ItemList = new List<FittingRoomItem>();

        [HideInInspector] public List<GoodsData> goodsDatas;

        private void Awake()
        {
            Item.gameObject.SetActive(false);
        }

        public void Init(OcCompetitionPanelMy root)
        {
            Root = root;
        }

        private void OnEnable()
        {
            var dataHandler = AssetsDataManager.GetData<OcContestHandler>();
            dataHandler.AddDataChange(gameObject, (assets) =>
            {
                goodsDatas = dataHandler.GetGoodsDataInOc(ItemData.creationInfo.ocInfo.skinInfos, false);
                RefreshItem();
            });

            goodsDatas = dataHandler.GetGoodsDataInOc(ItemData.creationInfo.ocInfo.skinInfos, false);

            WorkItem.SetData(ItemData);

            RefreshItem();

            ShowBtn();
        }

        void RefreshItem()
        {
            foreach (var item in ItemList)
            {
                item.gameObject.SetActive(false);
            }
            //var tem = ItemData.creationInfo.ocInfo.skinInfos;
            if (goodsDatas != null && goodsDatas.Count > 0)
            {
                for (int i = 0; i < goodsDatas.Count; i++)
                {
                    if (i >= ItemList.Count)
                    {
                        var obj = GameObject.Instantiate(Item, ItemParent).GetComponent<FittingRoomItem>();
                        ItemList.Add(obj);
                    }
                    ItemList[i].gameObject.SetActive(true);
                    ItemList[i].UpdateViews(goodsDatas[i],false, OnItemSelected, i);
                    //ItemList[i].SetData(goodsDatas[i], ItemData.creationInfo.ocInfo.skinInfos[i]);
                }
            }
        }

        public void ShowBtn()
        {
            WorkItem.EditBtn.gameObject.SetActive(false);
            WorkItem.HeadView.gameObject.SetActive(false);
            WorkItem.BuyBtn.gameObject.SetActive(false);
            WorkItem.PutBtn.gameObject.SetActive(false);
            WorkItem.DetailBtn.gameObject.SetActive(false);
            if (ItemData.creator.uid == AccountDataManager.Inst.Uid)
            {
                if (OcCompetitionSystem.Inst.InSubmission())
                {
                    //WorkItem.EditBtn.gameObject.SetActive(true);
                }
            }
            else
            {
                WorkItem.HeadView.gameObject.SetActive(true);
                float price = 0;
                foreach (var item in goodsDatas)
                {
                    if (item.GoodsType == GoodsType.SingleUgc && !item.IsOwned)
                    {
                        if (item.OriginalPrice != null && item.OriginalPrice.CurrencyType == CurrencyType.PinkCoin)
                        {
                            price += item.OriginalPrice.Value;
                        }
                    }
                }

                if (price > 0)
                {
                    WorkItem.BuyBtn.gameObject.SetActive(true);
                    WorkItem.PriceTxt.text = price.ToString();
                }
                else
                {
                    WorkItem.PutBtn.gameObject.SetActive(true);
                }
            }
        }

        public void PutOn()
        {
            var gameHallPanel = UIManager.Inst.FindPanel<GameHallPanel>(WindowId.GameHallWindow, PanelId.GameHallPanel);
            if (gameHallPanel == null) 
            {
                return;
            }
            foreach (var item in goodsDatas)
            {
                var asset = item.GetFirstAsset<AssetsData>();
                if (item.IsOwned)
                {
                    if (asset.ResourceType == ResourceType.Avatar)
                    {
                        var pgcAssets = asset as PGCAssetsData;
                        var subType = UniqueType.GetAvatar(pgcAssets.AvatarSubType);
                        //var specialConfig = DataTables.GetSpecialSkinConfig(pgcAssets.Id);
                        //var config = DataTables.GetAvatarCommonData(item.GetPgcId());
                        gameHallPanel.CharacterWrap.ChangePart(subType, pgcAssets.Id, null);
                    }
                    else if (asset.ResourceType == ResourceType.UgcAvatar)
                    {
                        if (asset != null && asset.UgcInfo != null && asset.UgcInfo.skinInfo != null)
                        {
                            gameHallPanel.CharacterWrap.ChangeUGCPart(asset.UgcInfo.skinInfo, null);
                        }
                    }
                }
            }
            //var avatarData = AvatarDataManager.Inst.SelfCharacterData;
            //foreach (var item in ls)
            //{
            //    avatarData.ChangeSkinData(item);
            //}
            AccountDataManager.Inst.SyncAvatarData(gameHallPanel.CharacterWrap.ChaData, isSuc =>
            {
                if (isSuc)
                {
                    AvatarDataManager.Inst.SelfCharacterData = gameHallPanel.CharacterWrap.ChaData;
                    gameHallPanel?.PlayChangeOcAni();
                }
            });

            OcCompetitionSystem.Inst.OcInfoListReq((assets) =>
            {
                if (data.OcListRsp.totalSlotCount == 0 || data.OcListRsp.totalCount < data.OcListRsp.totalSlotCount)
                {
                    OcCompetitionSystem.Inst.SetOcReq(ItemData.creationInfo.ocInfo.baseInfo.ocCover,
                        CharacterData.SerializeObject(gameHallPanel.CharacterWrap.ChaData),
                        SkinType.Avatar,
                        (succ) =>
                        {
                            if (succ) TipPanel.ShowToast("成功保存到设子卡位");
                            else TipPanel.ShowToast("卡位不足，无法保存设子");
                        });
                }
                else
                {
                    UIManager.Inst.OpenPanel<BuyOcPanel>(PanelId.BuyOcPanel, false);
                }
            });

            //var photoCamera = Loader.Load<GameObject>("Assets/Arts/Prefabs/CharacterUICamera.prefab").Instantiate(gameHallPanel.CharacterWrap.Avatar.transform).GetComponent<Camera>();
            //photoCamera.transform.localPosition = new Vector3(0, true ? 0.5f : 0.35f, 1);
            //photoCamera.transform.localEulerAngles = new Vector3(0, 180, 0);
            //photoCamera.orthographicSize = photoCamera.orthographicSize * ResolutionAutoFit.CameraScale *
            //                               gameHallPanel.CharacterWrap.Avatar.transform.localScale.x;
            //StartCoroutine(TakeMatchPhoto(photoCamera, (url) => {
            //    OcCompetitionSystem.Inst.OcInfoListReq((assets) =>
            //    {
            //        if (data.OcListRsp.totalSlotCount == 0 || data.OcListRsp.totalCount < data.OcListRsp.totalSlotCount)
            //        {
            //            OcCompetitionSystem.Inst.SetOcReq(url,
            //                CharacterData.SerializeObject(gameHallPanel.CharacterWrap.ChaData),
            //                SkinType.Avatar,
            //                (succ) => { 
            //                    if (succ) TipPanel.ShowToast("成功保存到设子卡位");
            //                    else TipPanel.ShowToast("卡位不足，无法保存设子");
            //                });
            //        }
            //        else
            //        {
            //            UIManager.Inst.OpenPanel<BuyOcPanel>(PanelId.BuyOcPanel, false);
            //        }
            //    });
            //}));
        }

        private IEnumerator TakeMatchPhoto(Camera camera,Action<string> ac)
        { 
            yield return new WaitForEndOfFrame();

            try
            {
                Rect rect = new Rect(0, 0, camera.pixelWidth, camera.pixelHeight);

                byte[] imgBytes = ScreenShotUtils.TakeShotGamma(camera, rect);

                Destroy(camera.gameObject);

                string fileName = LocalDataUtils.Inst.SaveImgRes(imgBytes);

                var uri = $"FittingRoom/characterInfo/{AccountDataManager.Inst.Uid}/{Path.GetFileName(fileName)}";
                CosXmlUploadManager.UploadFile(uri, fileName, (url, err) =>
                {
                    File.Delete(fileName);

                    if (!string.IsNullOrEmpty(err))
                    {
                        LoggerUtils.LogError($"Upload Character Image Fail!!! Err : {err}");
                        return;
                    }
                    else
                    {
                        LoggerUtils.Log("Upload Img Success url: " + url);

                        ac?.Invoke(url);
                    }
                });
            }
            catch (Exception e)
            {
                LoggerUtils.LogError(e.Message);
            }
        }

        public void BuyAll()
        {
            var ls = new List<AssetsBuyParam>();
            foreach (var item in goodsDatas)
            {
                if (item.GoodsType == GoodsType.SingleUgc && !item.IsOwned)
                {
                    if (item.OriginalPrice != null && item.OriginalPrice.CurrencyType == CurrencyType.PinkCoin)
                    {
                        var t = new AssetsBuyParam();
                        t.ugcInfo = item.GetFirstAsset<AssetsData>().UgcInfo.UgcInfo;
                        t.currencyType = item.OriginalPrice.CurrencyType;
                        t.subType = AssetsDataManager.GetUgcStyle(item);
                        t.price = (int)(Mathf.Ceil(item.OriginalPrice.Value));
                        ls.Add(t);
                    }
                }
            }
            AssetsBuyUtils.BuyUgcItem(ls, (succ) =>
            {
                if (succ)
                {
                    WorkItem.BuyBtn.gameObject.SetActive(false);
                    WorkItem.PutBtn.gameObject.SetActive(true);
                }
            });
        }

        private void OnItemSelected(GoodsData data)
        {
            switch (data.GoodsType)
            {
                case GoodsType.SingleUgc:
                    int ugcStyle = AssetsDataManager.GetUgcStyle(data);
                    UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.Skin, data.Id, ugcStyle);
                    break;
                case GoodsType.SinglePgc:
                    //UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.Skin, data.Id);
                    break;
            }
        }
    }
}