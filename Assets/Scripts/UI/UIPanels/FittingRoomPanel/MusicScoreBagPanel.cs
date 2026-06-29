using Com.TheFallenGames.OSA.DataHelpers;
using Game.Store;
using GameData.BaseInfo;
using GameData.PgcData;
using System.Collections;
using System.Collections.Generic;
using Game.Base;
using Game.MusicalInstrument;
using UI.Base;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.UI;

public class MusicScoreBagPanel : BasePanel<MusicScoreBagPanel>
{
    public class BagTuple
    {
        public BagTabs.Tab Item1;
        public UgcSource.Source Item2;
        public string Item3;
    }

    [SerializeField] internal UgcSource ugcSourceUI;
    [SerializeField] internal FittingRoomAdapter assetsList;
    [SerializeField] internal Text tipText;
    [SerializeField] internal Button closeButton;

    protected AvatarBagSceneHandler dataHandler;
    protected GoodsDataClassifyList assetsDatas = new();
    protected BagTuple bagTuple;

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        closeButton.onClick.AddListener(CloseSelf);

        assetsList.Data = new LazyDataHelper<GoodsData>(assetsList, CreateNewModel);
        assetsList.Init();

        ugcSourceUI.SetCallback(OnUgcSource);
        dataHandler = AssetsDataManager.GetData<AvatarBagSceneHandler>();
        dataHandler.AddDataChange(gameObject, OnDataChange);
        OnRedDotUpdate();

        bagTuple = new BagTuple();
        bagTuple.Item1 = BagTabs.Tab.Ugc;
        bagTuple.Item2 = UgcSource.Source.Buy;

        ugcSourceUI.DefualtOn(bagTuple.Item2);
    }

    internal void OnDataChange(AssetsData[] changes)
    {
        OnRedDotUpdate();

        switch (bagTuple.Item1)
        {
            case BagTabs.Tab.Bud:
                assetsDatas.SetData(dataHandler.GetGoodsData(GetSelectedClassType()), null);
                break;
            case BagTabs.Tab.Ugc:
                var datas = dataHandler.GetGoodsData(GetSelectedClassType());
                switch (bagTuple.Item2)
                {
                    case UgcSource.Source.Buy:
                        assetsDatas.SetData(datas, BuyPredicate);
                        break;
                    case UgcSource.Source.Create:
                        assetsDatas.SetData(datas, CreatePredicate);
                        break;
                }
                break;
        }
        assetsList.Data.ResetItems(assetsDatas.Count());
    }

    public void OnRedDotUpdate()
    {
        var ugcType = UniqueType.Get(ResourceType.MusicScore, (int)MusicScoreSubType.GeneralMusicScore);
        dataHandler.RedDotUgcCreator.TryGetValue(ugcType, out var count1);
        dataHandler.RedDotUgcBuy.TryGetValue(ugcType, out var count2);
        ugcSourceUI.SetRedDot(UgcSource.Source.Create, count1 > 0);
        ugcSourceUI.SetRedDot(UgcSource.Source.Buy, count2 > 0);
    }

    internal bool CreatePredicate(GoodsData goodsData)
    {
        if (goodsData.ButtonType == ButtonType.Design)
        {
            if (!GameController.IsInHallScene())
                return false;
            
            goodsData.AddTips = "去创作";
            return true;
        }
        if (goodsData.ButtonType == ButtonType.TakeOff) return true;
        if (goodsData.GoodsType != GoodsType.SinglePgc && goodsData.GoodsType != GoodsType.SingleUgc) return false;
        if (goodsData.Assets == null || goodsData.Assets.Count != 1) return false;
        if (!goodsData.IsOwned) return false;
        var asset = goodsData.Assets[0];
        if (!(asset is MusicScoreAssetsData)) return false;
        return asset.InventoryData.Tag == Network.Message.BackpackTag.Creator;
    }

    internal bool BuyPredicate(GoodsData goodsData)
    {
        if (goodsData.ButtonType == ButtonType.Design)
        {
            goodsData.AddTips = "获取乐谱";
            return true;
        }
        if (goodsData.ButtonType == ButtonType.TakeOff) return true;
        if (goodsData.GoodsType != GoodsType.SinglePgc && goodsData.GoodsType != GoodsType.SingleUgc) return false;
        if (goodsData.Assets == null || goodsData.Assets.Count != 1) return false;
        if (!goodsData.IsOwned) return false;
        var asset = goodsData.Assets[0];
        if (!(asset is MusicScoreAssetsData)) return false;
        return asset.InventoryData.Tag == Network.Message.BackpackTag.ErrBackpackTag;
    }

    internal int GetSelectedClassType()
    {
        return UniqueType.Get(ResourceType.MusicScore, (int)MusicScoreSubType.GeneralMusicScore);
    }

    internal void OnUgcSource(UgcSource.Source source)
    {
        bagTuple.Item2 = source;

        assetsList.gameObject.SetActive(true);
        assetsList.OnItemSelected = OnItemSelected;
        var datas = dataHandler.GetGoodsData(GetSelectedClassType());
        ShowTip("");
        switch (source)
        {
            case UgcSource.Source.Buy:
                assetsDatas.SetData(datas, BuyPredicate);
                if (assetsDatas.Count() == 0) ShowTip("暂无购买的商品");
                break;
            case UgcSource.Source.Create:
                assetsDatas.SetData(datas, CreatePredicate);
                break;
        }
        assetsList.Data.ResetItems(assetsDatas.Count());
    }

    public void ShowTip(string str)
    {
        tipText.gameObject.SetActive(true);
        tipText.text = str;
    }

    internal void OnItemSelected(GoodsData data)
    {
        switch (data.ButtonType)
        {
            case ButtonType.Design:
                // 跳转avatar绘制
                Design();
                return;
        }

        var assetData = data.GetFirstAsset<AssetsData>();
        if (assetData != null)
        {
            Message.MessageHelper.Broadcast(Message.MessageName.OnMusicScorePlay, assetData.UgcInfo.musicScoreInfo);
            AssetsDataManager.ClearNewTip(data);
            CloseSelf();
        }
        else
        {
            TipPanel.ShowToast("乐谱数据错误");
        }
    }

    internal void Design()
    {
        // 添加跳转
        switch (bagTuple.Item2)
        {
            case UgcSource.Source.Buy:
                //S2-A - 打开试衣间即退出演奏
                GuestInstrumentOPManager.Inst.ExitPlayMusicInstrumentState();
                
                // var fittingRoom = UIManager.Inst.SwapPanel(PanelId.FittingRoomPanel) as FittingRoomPanel;
                // fittingRoom?.JumpTo(MainTabs.Tab.Ugc, GetSelectedClassType());
                UIManager.Inst.OpenPanel(PanelId.MusicStorePanel);
                break;
            case UgcSource.Source.Create:
                UIManager.Inst.OpenPanel(PanelId.MusicScoreStudioPanel);
                break;
        }
    }

    public GoodsData CreateNewModel(int index)
    {
        var assetsData = assetsDatas.Get(index);
        assetsData.Selected = bagTuple.Item3 == assetsData.Id;
        return assetsData;
    }
}
