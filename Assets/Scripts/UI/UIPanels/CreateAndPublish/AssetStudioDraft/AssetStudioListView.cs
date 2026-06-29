using System;
using System.Collections.Generic;
using GameData.Base;
using GameData.BaseInfo;
using Message;
using UGCAsset;
using UGCAsset.Draft;
using UI.BaseWidgets;
using UnityEngine;

public class AssetStudioListView : MonoBehaviour
{
    public AssetStudioEntry gameEntry;
    public GameObject emptyText;

    private StudioSubType _studioType;

    private Action<DraftListItem> selectAction;
    public void InitUI(StudioSubType studioSubType, MainViewType viewType, Action<DraftListItem> selectAction, Action createAction = null)
    {
        this._studioType = studioSubType;
        this.selectAction = selectAction;
        gameEntry.SetActions(studioSubType, viewType, selectAction, GameStudioDraftsUploadClick, IsEmptyAction, createAction);
    }

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

    }

    private void InitListener(bool load)
    {
        if (load)
        {
            MessageHelper.AddListener<PropDraftInfo>(DraftMessage.DraftSaveStatus, OnPropDraftSaveCallback);
            MessageHelper.AddListener<MaterialDraftInfo>(DraftMessage.DraftSaveStatus, OnMaterialDraftSaveCallback);
            MessageHelper.AddListener(DraftMessage.RefreshDraft, OnMapRefreshCallback);
        }
        else
        {
            MessageHelper.RemoveListener<PropDraftInfo>(DraftMessage.DraftSaveStatus, OnPropDraftSaveCallback);
            MessageHelper.RemoveListener<MaterialDraftInfo>(DraftMessage.DraftSaveStatus, OnMaterialDraftSaveCallback);
            MessageHelper.RemoveListener(DraftMessage.RefreshDraft, OnMapRefreshCallback);
        }
    }

    public void EditInfo(DraftListItem draftListItem)
    {
        if (GameStudioUtils.GetBaseInfo(draftListItem) != null)
        {
            UpdateSingleItem(draftListItem);
        }
    }

    public void DeleteGame(DraftListItem draftListItem)
    {
        UgcBaseInfo ugcBaseInfo = GameStudioUtils.GetBaseInfo(draftListItem);
        if (ugcBaseInfo == null)
        {
            return;
        }

        if (ugcBaseInfo.isLocal)
        {
            //删除本地文件
            MainViewType mainViewType = GameStudioUtils.GetMainViewType(draftListItem);
            if (mainViewType == MainViewType.Prop)
            {
                PropAssetManager.Inst.DeleteDraftInfo(ugcBaseInfo.id);
            }
            else if (mainViewType == MainViewType.Material)
            {
                MaterialAssetManager.Inst.DeleteDraftInfo(ugcBaseInfo.id);
            }
        }

        RemoveSingleItem(ugcBaseInfo.id);
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
        } else if (mainViewType == MainViewType.Prop)
        {
            PropInfo mapInfo = draftListItem.propInfo;

            var draftInfo = PropAssetManager.Inst.GetDraftInfo(mapInfo.id);
            if (draftInfo != null)
            {
                PropAssetManager.Inst.UploadDraftInfo(draftInfo);
            }
        }
        else if (mainViewType == MainViewType.Material)
        {
            MaterialInfo mapInfo = draftListItem.materialInfo;

            var draftInfo = MaterialAssetManager.Inst.GetOrCreateDraftInfo(mapInfo);
            if (draftInfo != null)
            {
                MaterialAssetManager.Inst.UploadDraftInfo(draftInfo);
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
            selectAction?.Invoke(draftListItem);
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
    public void RequestDataList()
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
    private void GameStudioDraftsUploadClick(DraftListItem item, AvatarStudioBaseItem gameStudioItem)
    {
    }

    private void IsEmptyAction()
    {
        ShowEmptyTips(true);
    }

    private void ShowEmptyTips(bool show)
    {
        emptyText?.gameObject.SetActive(false);
        if (_studioType == StudioSubType.Published && show)
        {
            emptyText?.gameObject.SetActive(true);
        }
    }

}
