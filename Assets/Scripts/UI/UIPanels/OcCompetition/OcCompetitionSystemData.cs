using BUD.AnimPose;
using Game.Store;
using GameData;
using GameData.PgcData;
using System;
using System.Collections.Generic;
using UI.UIPanels.FittingRoom;
using UnityEngine;

namespace GameUI
{
    public class RewardRsp
    {
        public List<RewardItemRsp> rewardList;
    }
    public class RewardItemRsp
    {
        public int RewardType;
        public int Amount;
    }
    public enum OcCompetitionListType { 
        AllWork = 4 , // 投票列表 继续评选
        MyWork = 2,   // 我的作品
        RankWork = 3, //排行前100
        ChangeWork = 1, //换一换
    }
    public class OcCompetitionSystemData
    {
        //活动信息
        public ContestInfo ContestInfo;
        // 我的列表
        public OcCptListMsg MyListMsg;
        // 投票列表排行
        public OcCptListMsg VoteListMsg;
        // 列表排行
        public OcCptListMsg RankListMsg;


        //设子 
        public OcListPageUseData OcListRsp;
        private List<OcServerData> _ocListItems = new List<OcServerData>();
        public List<OcServerData> OcListItems
        {
            get { return _ocListItems; }
            set
            {
                _ocListItems = value;
                if (_ocListItems == null)
                {
                    _ocListItems = new List<OcServerData>();
                }
            }
        }
        public void Clear() {
            OcListRsp = null;
            OcListItems.Clear();
        }

        // 官方姿势
        public List<QuickPoseData> _quickPoses;
        public List<QuickPoseData> QuickPoses
        {
            get
            {
                return _quickPoses;
            }
            set
            {
                _quickPoses = value;
                if (_quickPoses == null)
                {
                    _quickPoses = new List<QuickPoseData>();
                }
            }
        }

        public void GetQuickPose(Action<List<QuickPoseData>> resultAction, GameObject bindNo)
        {
            if (QuickPoses == null)
            {
                FreePoseDataLoader.GetData(GameData.PgcData.UgcPoseSubType.Single,
                (ls) =>
                {
                    QuickPoses = ls;
                    resultAction?.Invoke(ls);
                },
                bindNo);
            }
            else
            {
                resultAction.Invoke(QuickPoses);
            }
        }

        //我的创作
        public GoodsDataClassifyList MyGoodsDataPose;
        //购买
        public GoodsDataClassifyList BuyGoodsDataPose;

        public void InitDataPose(GameObject bindNo) {
            MyGoodsDataPose = new GoodsDataClassifyList();
            var dataHandler = AssetsDataManager.GetData<AvatarBagSceneHandler>();
            dataHandler.AddDataChange(bindNo, (changes) => 
            {
                MyGoodsDataPose.SetData(dataHandler.GetGoodsData(UniqueType.Get(ResourceType.UgcPose, (int)UgcPoseSubType.Single)));
            });
            var datas = dataHandler.GetGoodsData(UniqueType.Get(ResourceType.UgcPose, (int)UgcPoseSubType.Single));
            MyGoodsDataPose.SetData(datas, CreatePredicate);

            BuyGoodsDataPose = new GoodsDataClassifyList();
            dataHandler.AddDataChange(bindNo, (changes) =>
            {
                BuyGoodsDataPose.SetData(dataHandler.GetGoodsData(UniqueType.Get(ResourceType.UgcPose, (int)UgcPoseSubType.Single)));
            });
            var datas2 = dataHandler.GetGoodsData(UniqueType.Get(ResourceType.UgcPose, (int)UgcPoseSubType.Single));
            BuyGoodsDataPose.SetData(datas2, BuyPredicate);
        }

        private bool CreatePredicate(GoodsData goodsData)
        {
            if (goodsData.ButtonType == ButtonType.Design) return false;
            if (goodsData.ButtonType == ButtonType.TakeOff) return false;
            if (goodsData.GoodsType != GoodsType.SinglePgc && goodsData.GoodsType != GoodsType.SingleUgc) return false;
            if (goodsData.Assets == null || goodsData.Assets.Count != 1) return false;
            if (!goodsData.IsOwned) return false;
            var asset = goodsData.Assets[0];
            if (!(asset is UgcPoseAssetsData)) return false;
            return asset.InventoryData.Tag == Network.Message.BackpackTag.Creator;
        }

        private bool BuyPredicate(GoodsData goodsData)
        {
            //if (goodsData.ButtonType == ButtonType.Design)
            //{
            //    goodsData.AddTips = "获得更多";
            //    return true;
            //}
            if (goodsData.ButtonType == ButtonType.TakeOff) return false;
            if (goodsData.GoodsType != GoodsType.SinglePgc && goodsData.GoodsType != GoodsType.SingleUgc) return false;
            if (goodsData.Assets == null || goodsData.Assets.Count != 1) return false;
            if (!goodsData.IsOwned) return false;
            var asset = goodsData.Assets[0];
            if (!(asset is UgcPoseAssetsData)) return false;
            return asset.InventoryData.Tag == Network.Message.BackpackTag.ErrBackpackTag;
        }
    }
}