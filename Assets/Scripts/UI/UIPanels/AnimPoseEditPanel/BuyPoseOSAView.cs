using System;
using System.Collections;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using Game.AnimationStudio;
using Game.Base;
using Game.Props.PropsManagers;
using Game.Store;
using GameData.BaseInfo;
using GameData.MapData;
using GameData.PgcData;
using UI.BaseWidgets;
using UI.UIPanels.FittingRoom;
using UnityEngine;

namespace BUD.AnimPose
{
    public class BuyPoseOSAView : BasePoseOSAView
    {
        [SerializeField] private FittingRoomAdapter assetsList;
        private UgcPoseSubType subType = UgcPoseSubType.Single;
        protected GoodsDataClassifyList assetsDatas = new();
        protected AvatarBagSceneHandler dataHandler;
        private string curSelectId;

        public bool isVehicle = false; //是载具中

        public override void OnStart(UgcPoseSubType subType)
        {
            this.subType = subType;
            dataHandler = AssetsDataManager.GetData<AvatarBagSceneHandler>();
            dataHandler.AddDataChange(this.gameObject, OnDataChange);
            var datas = dataHandler.GetGoodsData(UniqueType.Get(ResourceType.UgcPose, (int) subType));
            assetsDatas.SetData(datas, BuyPredicate);
            assetsList.Data = new LazyDataHelper<GoodsData>(assetsList, CreateNewModel);
            assetsList.Init();
            assetsList.OnItemSelected = OnItemSelected;
        }

        public override void OnUpdate()
        {
            assetsList.Data.ResetItems(assetsDatas.Count());
        }

        private bool BuyPredicate(GoodsData goodsData)
        {
            if (goodsData.ButtonType == ButtonType.Design)
            {
                goodsData.AddTips = "获得更多";
                return true;
            }
            if (goodsData.ButtonType == ButtonType.TakeOff) return false;
            if (goodsData.GoodsType != GoodsType.SinglePgc && goodsData.GoodsType != GoodsType.SingleUgc) return false;
            if (goodsData.Assets == null || goodsData.Assets.Count != 1) return false;
            if (!goodsData.IsOwned) return false;
            var asset = goodsData.Assets[0];
            if (!(asset is UgcPoseAssetsData)) return false;
            return asset.InventoryData.Tag == Network.Message.BackpackTag.ErrBackpackTag;
        }

        public GoodsData CreateNewModel(int index)
        {
            var assetsData = assetsDatas.Get(index);
            assetsData.Selected = curSelectId == assetsData.Id;
            return assetsData;
        }

        private void OnItemSelected(GoodsData data)
        {

            switch (data.ButtonType)
            {
                case ButtonType.Design:
                    // 跳转商城
                    switch (subType)
                    {
                        case UgcPoseSubType.Single:
                        case UgcPoseSubType.Double:
                            var poseStorePanel = UIManager.Inst.OpenPanel<PoseStorePanel>(PanelId.PoseStorePanel);
                            if (poseStorePanel != null)
                            {
                                poseStorePanel.OnCloseAction += () =>
                                {
                                    assetsList.Refresh();
                                };
                            }
                            break;

                        case UgcPoseSubType.PetSingle:
                        case UgcPoseSubType.PetWithPlayer:
                            var petFittingRoomPanel = UIManager.Inst.OpenPanel<FittingRoomPanel>(PanelId.FittingRoomPanel,true);
                            petFittingRoomPanel.JumpTo(MainTabs.Tab.Ugc, GameData.PgcData.UniqueType.Get(GameData.PgcData.ResourceType.UgcPose, (int)GameData.PgcData.UgcAnimSubType.PetAll));
                            petFittingRoomPanel.OnCloseAction += (data) =>
                            {
                                assetsList.Refresh();
                            };
                            break;
                    }
                    return;
            }
            if (isVehicle && CanUseVip("添加社区购买驾驶姿势") == false)
            {
                return;
            }
            var asset = data.GetFirstAsset<AssetsData>();
            var poseInfo = asset.UgcInfo.UgcInfo as PoseInfo;
            OnSelectPoseClick?.Invoke(poseInfo?.poseData);
            curSelectId = data.Id;
            assetsList.Data.ResetItems(assetsDatas.Count());
        }

        private void OnDataChange(AssetsData[] changes)
        {
            // 需要保持与 OnStart 一致的过滤/“获得更多”逻辑，否则刷新后列表会出现异常项/数量抖动
            assetsDatas.SetData(
                dataHandler.GetGoodsData(UniqueType.Get(ResourceType.UgcPose, (int)subType)),
                BuyPredicate
            );
            assetsList.Data.ResetItems(assetsDatas.Count());
        }

    }
}
