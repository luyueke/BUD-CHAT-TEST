using System;
using System.Collections.Generic;
using BUD.GameStudio;
using GameData.Base;
using GameData.BaseInfo;
using Message;
using Newtonsoft.Json;
using UGCAsset;
using UGCAsset.Draft;
using UI.BaseWidgets;
using UnityEngine;

namespace AIGame.Base
{
    public class AIHospitalUgcStudioMainView : MonoBehaviour
    {
        public GameStudioEntry gameEntry;
        public GameObject emptyText;
        public AIHospitalUgcGameDetailView detailView;

        private StudioSubType _studioType;

        private void OnEnable()
        {
            InitListener(true);
        }

        private void OnDisable()
        {
            InitListener(false);
        }

        private void InitListener(bool load)
        {
            if (load)
            {
                MessageHelper.AddListener(DraftMessage.RefreshDraft, OnMapRefreshCallback);
                MessageHelper.AddListener(MessageName.OnAssetDelete, OnMapRefreshCallback);
            }
            else
            {
                MessageHelper.RemoveListener(DraftMessage.RefreshDraft, OnMapRefreshCallback);
                MessageHelper.RemoveListener(MessageName.OnAssetDelete, OnMapRefreshCallback);
            }
        }


        public void InitUI(StudioSubType studioSubType)
        {
            this._studioType = studioSubType;
            gameEntry.SetActions(OnStudioItemClick, null, IsEmptyAction, _studioType, GameType.AIGame);
            RequestDataList();
        }

        public void RemoveSingleItem(string mapId)
        {
            if (gameEntry != null)
            {
                gameEntry.RemoveSingleItem(mapId);
            }
        }

        public void UpdateSingleItem(DraftListItem draftListItem)
        {
            if (gameEntry != null && draftListItem != null)
            {
                gameEntry.UpdateSingleItem(draftListItem);
                UpdateDetailView(draftListItem);
            }
        }

        private void OnMapRefreshCallback()
        {
            RequestDataList();
        }

        /// <summary>
        /// 重新请求数据
        /// </summary>
        private void RequestDataList()
        {
            gameEntry.GetFirstPageDatas(GetPageDatas);
        }

        private void GetPageDatas(List<DraftListItem> infos)
        {
            ShowEmptyTips(infos == null || infos.Count == 0);
        }

        private void IsEmptyAction()
        {
            ShowEmptyTips(true);
        }

        private void ShowEmptyTips(bool show)
        {
            emptyText.gameObject.SetActive(false);
            if (_studioType == StudioSubType.Published && show)
            {
                emptyText.gameObject.SetActive(true);
            }
        }

        private void OnStudioItemClick(DraftListItem item)
        {
            UpdateDetailView(item);
        }

        /// <summary>
        /// 显示有右边detail布局
        /// </summary>
        /// <param name="mapInfo"></param>
        private void UpdateDetailView(DraftListItem draftListItem)
        {
            if (draftListItem == null)
            {
                return;
            }

            switch (_studioType)
            {
                case StudioSubType.Drafts:
                    detailView.ShowPanel(draftListItem);
                    break;
                case StudioSubType.Published:
                    var mapId = draftListItem.mapInfo?.id;
                    var panel = UIManager.Inst.OpenPanel<AIHospitalUgcMapInfoPanel>(PanelId.AIHospitalUgcMapInfoPanel, mapId);
                    panel.SetUpdateAction(UpdateAction);
                    //LoggerUtils.LogError("Published");
                    break;
            }
        }

        private void UpdateAction(string msg,bool result)
        {
            if (result)
            {
                RequestDataList();
            }
        }
    }
}