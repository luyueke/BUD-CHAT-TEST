using BUD.AnimPose;
using Com.TheFallenGames.OSA.Util.IO;
using Game.Store;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace GameUI
{
    public class OcPoseItemData 
    {
        public QuickPoseData quickData;
        public GoodsData goodsData;
        public OcPoseItemData() {

        }
        public OcPoseItemData(QuickPoseData _quickData) {
            quickData = _quickData;
            goodsData = null;
        }
        public OcPoseItemData(GoodsData _goodsData)
        {
            quickData = null;
            goodsData = _goodsData;
        }
    }
    public class OcPoseItem : MonoBehaviour
    {
        public GameObject buyRoot;
        public GameObject infoRoot;

        public RemoteImageBehaviour remoteIcon;
        public GameObject selectIcon;
        public Button selectBtn;
        //public GameObject Go_VipTag;
        private Action<OcPoseItemData> onItemSelected;
        private OcPoseItemData itemData;
        private int Idx;
        private void Awake()
        {
            selectBtn.onClick.AddListener(OnItemClick);
            selectIcon.gameObject.SetActive(false);
        }

        private void OnItemClick()
        {
            onItemSelected?.Invoke(itemData);
            if (itemData.quickData == null && itemData.goodsData == null)
            {

            }
            else
            {
                selectIcon.gameObject.SetActive(true);
            }
        }

        public void SetIcon(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                LoggerUtils.LogError($"GameIconLoadItem.LoadIcon error.(url is empty)");
                return;
            }
            // ShowLoading();
            remoteIcon.Load(url, true, null);
        }

        public void SetData(OcPoseItemData poseData, Action<OcPoseItemData> action, int idx) 
        {
            itemData = poseData;
            Idx = idx;
            onItemSelected = action;
            selectIcon.gameObject.SetActive(false);
            if (poseData.quickData == null && poseData.goodsData == null) 
            {
                buyRoot.gameObject.SetActive(true);
                infoRoot.gameObject.SetActive(false);
            }
            else if (poseData.quickData != null)
            {
                buyRoot.gameObject.SetActive(false);
                infoRoot.gameObject.SetActive(true);
                SetData(poseData.quickData);
            }
            else if (poseData.goodsData != null)
            {
                buyRoot.gameObject.SetActive(false);
                infoRoot.gameObject.SetActive(true);
                SetData(poseData.goodsData);
            }
        }

        public void SetData(QuickPoseData poseData)
        {
            //selectIcon.SetActive(poseData.Selected);
            remoteIcon.Load(poseData.poseInfo.textureUrl);
            //Go_VipTag.SetActive(poseData.poseInfo.isVip == 1);
        }

        // dataHandler = AssetsDataManager.GetData<AvatarBagSceneHandler>();
        //GoodsData  // class 110100
        public void SetData(GoodsData goodsData) {
            var assetsData = goodsData.Assets[0] as UgcPoseAssetsData;
            if (assetsData.UgcInfo != null)
            {
                remoteIcon.Load(assetsData.UgcInfo.UgcInfo.cover);
            }
            else
            {
                CheckUgcIsBan(assetsData);
            }
        }

        private void CheckUgcIsBan(UgcPoseAssetsData assetsData)
        {
            AssetsDataManager.GetPoseInfo(assetsData.Id, (isSuccess, serverData) =>
            {
                if (!isSuccess) return;

                assetsData.UgcInfo = serverData;
                assetsData.Name = serverData.poseInfo.name;
                if (this == null) return;

                if(itemData == null || itemData.goodsData == null) return;
                if (itemData.goodsData.Id != serverData.poseInfo.id) return;

                SetData(itemData.goodsData);
            });
        }
    }
}