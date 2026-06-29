using System;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.Util.IO;
using Game.Store;
using Com.TheFallenGames.OSA.DataHelpers;

// 演员卡商城列表适配器：结构与 MusicStoreListAdapter 一致。
public class ActorCardStoreListAdapter : GridAdapter<GridParams, ActorCardStoreListItemHolder>
{
    public UnityEngine.Events.UnityEvent OnItemsUpdatedAct;
    public Action<RecommendItemData> OnSelectItemAct;
    public Action EmptyDataAct;

    public SimpleDataHelper<RecommendItemData> Data { get; private set; }

    #region OSA implementation

    protected override void Start()
    {
        Data = new SimpleDataHelper<RecommendItemData>(this);
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
            var creationId = Data[i].ugcId;
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

    public void UpdateSingleItem(RecommendItemData draftsListItem)
    {
        if (Data.Count == 0)
            return;

        int index = 0;
        for (var i = 0; i < Data.Count; i++)
        {
            if (Data[i] == null)
                continue;
            var leftId = Data[i].ugcId;
            var rightId = draftsListItem.ugcId;
            if (!string.IsNullOrEmpty(rightId) && leftId == rightId)
            {
                index = i;
                break;
            }
        }

        Data.UpdateItem(index, draftsListItem);
        Refresh();
    }

    private string curSelectId = "";
    public void SetDataSelected(RecommendItemData data)
    {
        // 「去创作」占位项没有 UgcInfo，不参与选中
        if (data == null || data.UgcInfo == null)
        {
            return;
        }
        curSelectId = data.UgcInfo.id;
        OnSelectItemAct?.Invoke(data);
        Refresh();
    }

    private void OnSelectItem(RecommendItemData info)
    {
        var id = info?.UgcInfo?.id;
        if (!string.IsNullOrEmpty(id) && id != curSelectId)
        {
            curSelectId = id;
            Refresh();
        }

        OnSelectItemAct?.Invoke(info);
    }

    public override void Refresh(bool contentPanelEndEdgeStationary = false /*ignored*/, bool keepVelocity = false)
    {
        OnItemsUpdatedAct?.Invoke();
        base.Refresh(false, keepVelocity);
    }

    protected override void UpdateCellViewsHolder(ActorCardStoreListItemHolder newOrRecycled)
    {
        RecommendItemData data = Data[newOrRecycled.ItemIndex];
        newOrRecycled.UpdateViews(data, curSelectId, OnSelectItem);

        // 「去创作」占位项：不加载封面
        if (ActorCardStoreItemView.IsDesignEntry(data))
        {
            newOrRecycled.Rm_Cover.gameObject.SetActive(false);
            return;
        }

        var path = data?.UgcInfo?.cover;
        if (data != null && !string.IsNullOrEmpty(path))
        {
            newOrRecycled.Rm_Cover.gameObject.SetActive(false);
            var capturedUgcId = data.ugcId;
            newOrRecycled.Rm_Cover.Load(path, true,
                (from, success) =>
                {
                    // 回调触发时 holder 可能已被 OSA 复用给其他 item（包括 Design 占位项），
                    // 用 ItemIndex 查当前 holder 实际对应的数据，id 不一致则说明已被复用，不显示
                    var idx = newOrRecycled.ItemIndex;
                    if (idx < 0 || idx >= Data.Count) return;
                    var currentId = Data[idx]?.ugcId;
                    if (string.IsNullOrEmpty(capturedUgcId) || capturedUgcId != currentId) return;
                    newOrRecycled.Rm_Cover.gameObject.SetActive(true);
                });
        }
        else
        {
            newOrRecycled.Rm_Cover.gameObject.SetActive(false);
        }
    }

    #endregion
}

public class ActorCardStoreListItemHolder : CellViewsHolder
{
    public RemoteImageBehaviour Rm_Cover;
    public ActorCardStoreItemView draftsItem;

    public override void CollectViews()
    {
        base.CollectViews();
        Rm_Cover = GameObjectEx.FindChildByName(views, "RemoteIcon").GetComponent<RemoteImageBehaviour>();
        draftsItem = root.GetComponentInParent<ActorCardStoreItemView>();
    }

    public void UpdateViews(RecommendItemData model, string curSelectId, Action<RecommendItemData> onSelect)
    {
        if (draftsItem != null)
        {
            draftsItem.SetData(model, onSelect);
            // 「去创作」占位项 UgcInfo 为 null，不参与选中高亮
            var id = model?.UgcInfo?.id;
            draftsItem.SetSelectState(!string.IsNullOrEmpty(id) && curSelectId == id);
        }
    }
}
