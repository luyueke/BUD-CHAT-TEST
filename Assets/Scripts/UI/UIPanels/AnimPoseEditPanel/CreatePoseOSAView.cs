using System;
using System.Collections;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using Game.Base;
using Game.Props.PropsManagers;
using Game.Store;
using GameData.BaseInfo;
using GameData.MapData;
using GameData.PgcData;
using Product;
using UI.BaseWidgets;
using UI.UIPanels.FittingRoom;
using UnityEngine;

namespace BUD.AnimPose
{
    public class CreatePoseOSAView : BasePoseOSAView
    {
        [SerializeField] private FittingRoomAdapter assetsList;
        private UgcPoseSubType poseSubType = UgcPoseSubType.Single;
        protected GoodsDataClassifyList assetsDatas = new();
        protected AvatarBagSceneHandler dataHandler;
        public bool isVehicle = false; //是载具中

        public override void OnStart(UgcPoseSubType subType)
        {
            poseSubType = subType;
            dataHandler = AssetsDataManager.GetData<AvatarBagSceneHandler>();
            dataHandler.AddDataChange(this.gameObject, OnDataChange);
            var datas = dataHandler.GetGoodsData(UniqueType.Get(ResourceType.UgcPose, (int) poseSubType));
            assetsDatas.SetData(datas, CreatePredicate);
            assetsList.Data = new LazyDataHelper<GoodsData>(assetsList, CreateNewModel);
            assetsList.Init();
            assetsList.OnItemSelected = OnItemSelected;
        }
        
        public override void OnUpdate()
        {
            assetsList.Data.ResetItems(assetsDatas.Count());
        }

        private bool CreatePredicate(GoodsData goodsData)
        {
            if (goodsData.ButtonType == ButtonType.Design)  return false;
            if (goodsData.ButtonType == ButtonType.TakeOff) return false;
            if (goodsData.GoodsType != GoodsType.SinglePgc && goodsData.GoodsType != GoodsType.SingleUgc) return false;
            if (goodsData.Assets == null || goodsData.Assets.Count != 1) return false;
            if (!goodsData.IsOwned) return false;
            var asset = goodsData.Assets[0];
            if (!(asset is UgcPoseAssetsData)) return false;
            return asset.InventoryData.Tag == Network.Message.BackpackTag.Creator;
        }

        public GoodsData CreateNewModel(int index)
        {
            var assetsData = assetsDatas.Get(index);
            assetsData.Selected = CurSelectId == assetsData.Id;
            return assetsData;
        }

        private void OnItemSelected(GoodsData data)
        {

            switch (data.ButtonType)
            {
                case ButtonType.Design:
                    // 跳转商城
                    return;
            }
            if (isVehicle && CanUseVip("添加我创作的驾驶姿势") == false)
            {
                return;
            }
            var asset = data.GetFirstAsset<AssetsData>();
            var poseInfo = asset.UgcInfo.UgcInfo as PoseInfo;
            OnSelectPoseClick?.Invoke(poseInfo?.poseData);
            CurSelectId = data.Id;
            assetsList.Data.ResetItems(assetsDatas.Count());
        }
        
        private void OnDataChange(AssetsData[] changes)
        {
            // 需要保持与 OnStart 一致的过滤条件，否则数据刷新后可能把 ButtonType.Design(获得更多) 混进来
            assetsDatas.SetData(
                dataHandler.GetGoodsData(UniqueType.Get(ResourceType.UgcPose, (int)poseSubType)),
                CreatePredicate
            );
            assetsList.Data.ResetItems(assetsDatas.Count());
        }

    }
}
