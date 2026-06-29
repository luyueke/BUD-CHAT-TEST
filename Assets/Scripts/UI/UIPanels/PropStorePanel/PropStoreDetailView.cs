using Game.Base;
using Game.CommunityGame;
using Game.ECS;
using Game.Props.PropsManagers;
using GameData.MapData;
using Message;
using UI;
using UI.BaseWidgets;
using UI.UIWidgets;
using UnityEngine;

namespace Game.PropStore
{
    public class PropStoreDetailView : MonoBehaviour
    {
        public PreviewCameraHandler previewCameraHandler;
        public RenderTexture previewRenderTexture;
        public Camera previewCamera;
        public UserInfoView UserInfoView;
        public CButton Btn_UserHead;
        public PurchaseButton PurchaseButton;
        public CText Txt_ItemName;
        public PropStoreAdapter PropStoreAdapter;

        private GameObject coverObj = null;
        private string curPreviewMetaUrl;
        private RecommendItemData curData;

        public void Awake() {
            MessageHelper.AddListener<string>(MessageName.OnBuyUgcItemSuccess, OnBuyUgcItemSuccess);
        }

        private void OnDestroy()
        {
            DestroyPreviewObj();
            MessageHelper.RemoveListener<string>(MessageName.OnBuyUgcItemSuccess, OnBuyUgcItemSuccess);
        }

        private void DestroyPreviewObj()
        {
            if (coverObj != null) {
                AssetPropNodeManager.Inst.DestroyProp(coverObj);
                coverObj = null;
            }
        }

        public void RefreshUIByData(RecommendItemData data)
        {
            this.curData = data;
            DestroyPreviewObj();
            RefreshUIInfo(data);
            StartPreview(data);
        }

        private void RefreshUIInfo(RecommendItemData data)
        {
            var ugcId = data?.ugcInfo?.id;
            var consumed = data?.interactInfo?.consumed;
            var propName = data?.ugcInfo?.name;
            var paymentInfo = data?.ugcInfo?.paymentInfo;
            int ugcStyle = 0;
            if (data != null && data.ugcInfo != null)
            {
                ugcStyle = data.ugcInfo.ugcStyle;
            }
            AccountUserInfo accountUserInfo = data?.creatorInfo;
            UserInfoView.IsOpenProfilePanel = false;
            UserInfoView.SetData(accountUserInfo);
            Btn_UserHead.onClick.RemoveAllListeners();
            Btn_UserHead.onClick.AddListener(() =>
            {
                UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.Prop, ugcId, ugcStyle);
            });
            PurchaseButton.SetData(data.ugcInfo, consumed, paymentInfo);
            Txt_ItemName.text = propName;

            UserInfoView.gameObject.SetActive(true);
            Txt_ItemName.gameObject.SetActive(true);
        }

        private void StartPreview(RecommendItemData data)
        {
            var ugcId = data.ugcInfo.id;
            this.curPreviewMetaUrl = data.ugcInfo.metaDataUrl;
            var mUrl = this.curPreviewMetaUrl;
            coverObj = AssetPropNodeManager.Inst.CreateProp(ugcId, mUrl, tmpObj => {
                previewCameraHandler.SetFocus();
            });
            if (coverObj == null) {
                LoggerUtils.LogError("素材创建失败:" + mUrl + "," );
                return;
            }
            coverObj.transform.SetParent(previewCamera.transform);
            coverObj.Reset();
            previewCameraHandler.SetTarget(coverObj);
            previewCameraHandler.SetFocus();
        }

        private void OnBuyUgcItemSuccess(string ugcId){
            if (PropStoreAdapter != null)
            {
                PropStoreAdapter.OnBuySuccess(ugcId);
            }

            if (curData != null && curData.interactInfo != null)
            {
                curData.interactInfo.consumed = 1;
                RefreshUIInfo(curData);
            }
        }
    }
}
