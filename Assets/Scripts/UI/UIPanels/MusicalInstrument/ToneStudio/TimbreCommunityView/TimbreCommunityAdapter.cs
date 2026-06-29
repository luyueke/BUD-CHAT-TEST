
using System;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.IO;
using GameData.BaseInfo;

public class ToneFixedInfo
{
    public enum ToneFixedStyle
    {
        Normal = 0,
        Store = 1,
        Publish = 2,
    }
    
    public ToneInfo toneInfo;
    public ToneFixedStyle style;

    static public ToneFixedInfo Init(ToneInfo toneInfo, ToneFixedStyle style = ToneFixedStyle.Normal)
    {
        var fixedInfo = new ToneFixedInfo();
        fixedInfo.toneInfo = toneInfo;
        fixedInfo.style = style;
        return fixedInfo;
    }
}

public class TimbreCommunityAdapter : GridAdapter<GridParams, TimbreCommunityeItemHolder>
{
    public UnityEngine.Events.UnityEvent OnItemsUpdatedAct;
    public Action<ToneFixedInfo> OnSelectItemAct;
    public Action EmptyDataAct;

    // Helper that stores data and notifies the adapter when items count changes
    // Can be iterated and can also have its elements accessed by the [] operator
    public SimpleDataHelper<ToneFixedInfo> Data { get; private set; }
    #region OSA implementation

    protected override void Start()
    {
        Data = new SimpleDataHelper<ToneFixedInfo>(this);
        base.Start();
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
            if (Data[i] == null)
                continue;
            var creationId = Data[i].toneInfo.id;
            if (!string.IsNullOrEmpty(creationId) && creationId == infoId)
            {
                index = i;
                break;
            }
        }

        Data.RemoveOne(index);
        Refresh();
        if (Data.Count == 0)
        {
            EmptyDataAct.Invoke();
        }
    }

    public void UpdateSingleItem(ToneFixedInfo draftsListItem)
    {
        if (Data.Count == 0)
            return;

        int index = 0;
        for (var i = 0; i < Data.Count; i++)
        {
            if (Data[i] == null)
                continue;
            var leftId = Data[i].toneInfo.id;
            var rightId = draftsListItem.toneInfo.id;
            if (!string.IsNullOrEmpty(rightId) && leftId == rightId)
            {
                index = i;
                break;
            }
        }

        Data.UpdateItem(index, draftsListItem);
        Refresh();
    }

    private void OnSelectItem(ToneFixedInfo info)
    {
        OnSelectItemAct?.Invoke(info);
    }
    
    public override void Refresh(bool contentPanelEndEdgeStationary = false /*ignored*/, bool keepVelocity = false)
    {
        OnItemsUpdatedAct?.Invoke();
        base.Refresh(false, keepVelocity);
    }
    
    protected override void UpdateCellViewsHolder(TimbreCommunityeItemHolder newOrRecycled)
    {
        ToneFixedInfo data = Data[newOrRecycled.ItemIndex];

        newOrRecycled.UpdateViews(data, OnSelectItem);

        var path = data?.toneInfo?.cover;
        if (data != null && !string.IsNullOrEmpty(path))
        {
            newOrRecycled.Rm_Cover.gameObject.SetActive(false);
            newOrRecycled.Rm_Cover.Load(path, true,
                (from, success) => { newOrRecycled.Rm_Cover.gameObject.SetActive(true); });
        }
    }

    #endregion
}

public class TimbreCommunityeItemHolder : CellViewsHolder
{
    public RemoteImageBehaviour Rm_Cover;
    public TimbreCommunityItemView draftsItem;

    public override void CollectViews()
    {
        base.CollectViews();
        Rm_Cover = GameObjectEx.FindChildByName(views, "RemoteIcon").GetComponent<RemoteImageBehaviour>();
        draftsItem = root.GetComponentInParent<TimbreCommunityItemView>();
    }

    public void UpdateViews(ToneFixedInfo model, Action<ToneFixedInfo> onSelect)
    {
        if (draftsItem != null)
        {
            draftsItem.UpdateViews(model, onSelect);
        }
    }
}
