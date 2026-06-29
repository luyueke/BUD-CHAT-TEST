using System;
using System.Collections.Generic;
using GameData.Base;
using GameData.BaseInfo;
using Message;
using UGCAsset;
using UGCAsset.Draft;
using UI.BaseWidgets;
using UnityEngine;

public enum MainViewType
{
    Game = 0,
    Prop = 1,
    Material = 2,
    Cloth = 3
}

/// <summary>
/// 草稿箱主界面组件
/// @stanley
/// </summary>

namespace BUD.GameStudio
{
    public class GameStudioMainView : MonoBehaviour
    {
        public GameDetailView gameDetailView;
        public GameStudioEntry gameEntry;
        public GameObject emptyText;

        private StudioSubType _studioType;

        private void OnEnable()
        {
            InitListener(true);
        }

        private void OnDisable()
        {
            InitListener(false);
        }

        private void Start()
        {
            InitGameDetailView();
        }

        private void InitListener(bool load)
        {
            if (load)
            {
                MessageHelper.AddListener<MapDraftInfo>(DraftMessage.DraftSaveStatus, OnMapDraftSaveCallBack);
                MessageHelper.AddListener<PropDraftInfo>(DraftMessage.DraftSaveStatus, OnPropDraftSaveCallback);
                MessageHelper.AddListener(DraftMessage.RefreshDraft, OnMapRefreshCallback);
            }
            else
            {
                MessageHelper.RemoveListener<MapDraftInfo>(DraftMessage.DraftSaveStatus, OnMapDraftSaveCallBack);
                MessageHelper.RemoveListener<PropDraftInfo>(DraftMessage.DraftSaveStatus, OnPropDraftSaveCallback);
                MessageHelper.RemoveListener(DraftMessage.RefreshDraft, OnMapRefreshCallback);
            }
        }


        public void InitUI(StudioSubType studioSubType)
        {
            this._studioType = studioSubType;
            gameEntry.SetActions(OnStudioItemClick, GameStudioDraftsUploadClick, IsEmptyAction, _studioType);
            RequestDataList();
        }

        private void InitGameDetailView()
        {
            gameDetailView.InitUI();
            gameDetailView.SetAction(EditInfo, CopyGame, DeleteGame, PublishGame, UpdateGame, ReuploadGame);
        }

        /// <summary>
        /// 更新地图
        /// </summary>
        /// <param name="isSuccess"></param>
        private void UpdateGame(bool isSuccess)
        {
            if (isSuccess)
            {
                RequestDataList();
            }
        }


        private void EditGame(DraftListItem draftListItem)
        {
        }

        private void EditInfo(DraftListItem draftListItem)
        {
            if (GameStudioUtils.GetBaseInfo(draftListItem) != null)
            {
                UpdateSingleItem(draftListItem);
            }
        }

        private void CopyGame(DraftListItem draftListItem)
        {
            RequestDataList();
        }

        private void DeleteGame(DraftListItem draftListItem)
        {
            UgcBaseInfo ugcBaseInfo = GameStudioUtils.GetBaseInfo(draftListItem);
            if (ugcBaseInfo != null)
            {
                if (ugcBaseInfo.isLocal)
                {
                    //删除本地文件
                    MainViewType mainViewType = GameStudioUtils.GetMainViewType(draftListItem);
                    if (mainViewType == MainViewType.Game)
                    {
                        MapAssetManager.Inst.DeleteDraftInfo(ugcBaseInfo.id);
                    }
                }

                RemoveSingleItem(ugcBaseInfo.id);
            }
        }

        public void PublishGame(bool isSuccess)
        {
            //发布成功切换到已发布tab
            if (isSuccess)
            {
                // InitViewByType(StudioSubType.Published);
            }
        }

        /// <summary>
        /// 重新上传
        /// </summary>
        /// <param name="draftListItem"></param>
        private void ReuploadGame(DraftListItem draftListItem)
        {
            if (draftListItem == null)
            {
                return;
            }

            if (GameStudioUtils.GetBaseInfo(draftListItem) == null)
            {
                return;
            }

            MainViewType mainViewType = GameStudioUtils.GetMainViewType(draftListItem);
            if (mainViewType == MainViewType.Game)
            {
                MapInfo mapInfo = draftListItem.mapInfo;

                var mapDraftInfo = MapAssetManager.Inst.GetDraftInfo(mapInfo.id);
                if (mapDraftInfo != null)
                {
                    MapAssetManager.Inst.UploadDraftInfo(mapDraftInfo);
                }
            }
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

        private void OnMapDraftSaveCallBack(MapDraftInfo mapDraftInfo)
        {
            RequestDataList();
        }

        private void OnPropDraftSaveCallback(PropDraftInfo ugcItemDraftInfo)
        {
            RequestDataList();
        }

        private void OnMaterialDraftSaveCallback(MaterialDraftInfo materialDraftInfo)
        {
            RequestDataList();
        }

        private void OnClothDraftSaveCallback(SkinDraftInfo skinDraftInfo)
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


        /// <summary>
        /// 草稿上传操作
        /// </summary>
        /// <param name="item"></param>
        /// <param name="gameStudioItem"></param>
        private void GameStudioDraftsUploadClick(DraftListItem item, DraftsItem gameStudioItem)
        {
        }

        private void IsEmptyAction()
        {
            ShowEmptyTips(true);
        }

        private void ShowEmptyTips(bool show)
        {
            gameDetailView.gameObject.SetActive(false);
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
            if (draftListItem != null)
            {
                //调用接口获取草稿状态
                gameDetailView.UpdateInfo(draftListItem, _studioType);
            }
        }

        public void DestroyUI()
        {
            gameDetailView.DestroyUI();
        }
    }
}
