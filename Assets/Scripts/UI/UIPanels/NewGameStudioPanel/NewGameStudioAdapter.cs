using System;
using Com.TheFallenGames.OSA.Core;
using Com.TheFallenGames.OSA.CustomParams;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.IO;
using Com.TheFallenGames.OSA.Util.IO.Pools;
using frame8.Logic.Misc.Other.Extensions;
using GameData.Base;
using UGCAsset.Draft;


public class NewGameStudioAdapter : OSA<BaseParamsWithPrefab, NewGameStudioItemHolder>
{
    public UnityEngine.Events.UnityEvent OnItemsUpdated;
    public Action<DraftListItem> dataAction;
    public Action<DraftListItem, NewGameStudioItem> uploadAction;
    public SimpleDataHelper<DraftListItem> Data { get; set; }
    private IPool texturePool;

    #region OSA implementation

    protected override void Start()
    {
        texturePool = new FIFOCachingPool(12, TextureDestoryer);
        Data = new SimpleDataHelper<DraftListItem>(this);
        base.Start();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        ClearPool();
    }

    public void ClearPool()
    {
        if (texturePool != null)
            texturePool.Clear();
    }

    /// <summary>
    /// 更新单个上传状态
    /// </summary>
    public void UpdateSingleUploadStatus(string draftId, UploadStatus uploadStatus)
    {
        Refresh();
    }

    public void RemoveSingleItem(string infoId)
    {
        if (Data.Count == 0)
        {
            return;
        }

        int index = 0;
        for (var i = 0; i < Data.Count; i++)
        {
            UgcBaseInfo ugcBaseInfo = GameStudioUtils.GetBaseInfo(Data[i]);
            if (ugcBaseInfo != null && ugcBaseInfo.id == infoId)
            {
                index = i;
                break;
            }
        }
        
        Data.RemoveOne(index);
        Refresh();
    }

    public void UpdateSingleItem(DraftListItem draftsListItem)
    {
        if (Data.Count == 0)
            return;
        int index = 0;
        for (var i = 0; i < Data.Count; i++)
        {
            UgcBaseInfo ugcBaseInfo = GameStudioUtils.GetBaseInfo(Data[i]);
            UgcBaseInfo draftBaseInfo = GameStudioUtils.GetBaseInfo(draftsListItem);
            if (ugcBaseInfo != null && ugcBaseInfo.id == draftBaseInfo.id)
            {
                index = i;
                break;
            }
        }

        Data.UpdateItem(index, draftsListItem);
        Refresh();
    }

    public void SetOnClickAct(Action<DraftListItem> act = null)
    {
        this.dataAction = act;
    }

    public void OnItemClick(DraftListItem info)
    {
        dataAction?.Invoke(info);
    }

    private void TextureDestoryer(object urlKey, object texture)
    {
        var asUnityObject = texture as UnityEngine.Object;
        if (asUnityObject != null)
            Destroy(asUnityObject);
    }


    public override void Refresh(bool contentPanelEndEdgeStationary = false /*ignored*/, bool keepVelocity = false)
    {
        OnItemsUpdated?.Invoke();
        base.Refresh(false, keepVelocity);
    }


    /// <inheritdoc/>
    protected override NewGameStudioItemHolder CreateViewsHolder(int itemIndex)
    {
        var instance = new NewGameStudioItemHolder();
        instance.Init(_Params.ItemPrefab, _Params.Content, itemIndex);
        instance.CoverRmBev.InitializeWithPool(texturePool);
        return instance;
    }


    protected override void UpdateViewsHolder(NewGameStudioItemHolder newOrRecycled)
    {
        DraftListItem data = Data[newOrRecycled.ItemIndex];
        newOrRecycled.UpdateViews(OnItemClick, data);

        if (data.mapInfo != null)
        {
            if (data.mapInfo.cover != null && !string.IsNullOrEmpty(data.mapInfo.cover))
            {
                newOrRecycled.CoverRmBev.Load(data.mapInfo.cover);
            }
        }
    }

    #endregion
}


public class NewGameStudioItemHolder : BaseItemViewsHolder
    {
        public RemoteImageBehaviour CoverRmBev;
        public NewGameStudioItem StudioItem;

        public override void CollectViews()
        {
            base.CollectViews();
            root.GetComponentAtPath("DraftsBtn/DraftsGameImage", out CoverRmBev);
            StudioItem = root.GetComponentInParent<NewGameStudioItem>();
        }

        public void UpdateViews(Action<DraftListItem> act, DraftListItem data)
        {
            if (StudioItem != null)
            {
                StudioItem.Init(act, data);
            }
        }
    }