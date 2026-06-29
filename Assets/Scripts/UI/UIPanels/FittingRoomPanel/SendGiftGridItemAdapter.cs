using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using frame8.Logic.Misc.Other.Extensions;
using Game.Store;
using System;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.UI;


public class SendGiftGridItemAdapter : GridAdapter<SendGiftGridParams, SendGiftItemHolder>
{
    public PullToRefreshBehaviour PullToRefreshBehaviour;
    public LazyDataHelper<GoodsData> Data { get; set; }

    public Action<GoodsData> OnItemSelected;

    public Color BgColor;
    public Color SelectedColor;


    /// <summary>
    /// 太多地方用这个 Item了，需要识别是否是试衣间
    /// </summary>
    [HideInInspector] public bool IsFittingRoomPanel = false;

    public void ResetColor()
    {
        ColorUtility.TryParseHtmlString("#EDE8FF", out BgColor);
        ColorUtility.TryParseHtmlString("#FFCD19", out SelectedColor);
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
    }

    protected override void UpdateCellViewsHolder(SendGiftItemHolder viewsHolder)
    {
        var model = Data.GetOrCreate(viewsHolder.ItemIndex);
        viewsHolder.UpdateColor(BgColor, SelectedColor);
        viewsHolder.UpdateViews(model, IsFittingRoomPanel, OnItemSelected);
    }

    protected override void OnCellViewsHolderCreated(SendGiftItemHolder cellVH,
        CellGroupViewsHolder<SendGiftItemHolder> cellGroup)
    {
        base.OnCellViewsHolderCreated(cellVH, cellGroup);
    }

    protected override CellGroupViewsHolder<SendGiftItemHolder> GetNewCellGroupViewsHolder()
    {
        return new SendGridCellGroupViewsHolder();
    }

    protected override void UpdateViewsHolder(CellGroupViewsHolder<SendGiftItemHolder> newOrRecycled)
    {
        base.UpdateViewsHolder(newOrRecycled);

        if (newOrRecycled.NumActiveCells > 0)
        {
            var firstCellVH = newOrRecycled.ContainingCellViewsHolders[0];
            var itemData = Data.GetOrCreate(firstCellVH.ItemIndex);
            var newOrRecycledCasted = newOrRecycled as SendGridCellGroupViewsHolder;
            if (itemData.ButtonType == ButtonType.NoMoreTips)
                newOrRecycledCasted.ShowTips();
            else
                newOrRecycledCasted.HideTips();
        }

        ScheduleComputeVisibilityTwinPass();
    }
}

public class SendGiftItemHolder : CellViewsHolder
{
    public SendGiftItem item;
    public CanvasGroup canvasGroup;

    public override void CollectViews()
    {
        base.CollectViews();
        item = root.GetComponent<SendGiftItem>();
        canvasGroup = root.GetComponent<CanvasGroup>();
    }

    public void UpdateColor(Color color1, Color color2)
    {
        item.SetStyle(color1, color2);
    }

    public void UpdateViews(GoodsData data, bool isFittingRoom, Action<GoodsData> action)
    {
        if (data.ButtonType == ButtonType.NoMoreTips)
        {
            if (canvasGroup != null) canvasGroup.alpha = 0;
        }
        else
        {
            if (canvasGroup != null) canvasGroup.alpha = 1;
            item.UpdateViews(data, isFittingRoom, action);
        }
    }
}

[Serializable]
public class SendGiftGridParams : GridParams
{
    [SerializeField] GameObject CellGroupPrefab = null;

    protected override GameObject CreateCellGroupPrefabGameObject()
    {
        return CellGroupPrefab ? CellGroupPrefab : base.CreateCellGroupPrefabGameObject();
    }
}

public class SendGridCellGroupViewsHolder : CellGroupViewsHolder<SendGiftItemHolder>
{
    public ContentSizeFitter contentSizeFitterComponent;

    Transform tipPanel;

    public override void CollectViews()
    {
        base.CollectViews();

        contentSizeFitterComponent = root.gameObject.AddComponent<ContentSizeFitter>();
        contentSizeFitterComponent.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        contentSizeFitterComponent.enabled = true;

        root.GetComponentAtPath("NoMoreDataTips", out tipPanel);
    }

    public void ShowTips()
    {
        tipPanel?.gameObject.SetActive(true);
    }

    public void HideTips()
    {
        tipPanel?.gameObject.SetActive(false);
    }
}