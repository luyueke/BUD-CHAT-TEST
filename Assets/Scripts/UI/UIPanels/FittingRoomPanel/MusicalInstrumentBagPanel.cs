using Com.TheFallenGames.OSA.DataHelpers;
using Game.Database;
using Game.Store;
using GameData.PgcData;
using System.Collections;
using System.Collections.Generic;
using UI.Base;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.UI;

public class MusicalInstrumentBagPanel : BasePanel<MusicalInstrumentBagPanel>
{
    public class BagTuple
    {
        public MISource.Source Item1;
        public string Item2;
        public bool Item3;
    }

    [SerializeField] internal MISource miSourceUI;
    [SerializeField] internal FittingRoomAdapter assetsList;
    [SerializeField] internal Text tipText;
    [SerializeField] internal Button closeButton;
    [SerializeField] internal Button sureButton;
    [SerializeField] internal Button defaultButton;
    [SerializeField] internal GameObject defaultSelect;

    protected AvatarBagSceneHandler dataHandler;
    protected GoodsDataClassifyList assetsDatas = new();
    protected BagTuple bagTuple;
    private string DefaultId = "12400014";

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        closeButton.onClick.AddListener(CloseSelf);

        defaultButton.onClick.AddListener(OnDefaultSelected);

        sureButton.onClick.AddListener(OnSureClick);

        bagTuple = new BagTuple();
        bagTuple.Item1 = MISource.Source.Buy;

        dataHandler = AssetsDataManager.GetData<AvatarBagSceneHandler>();
        dataHandler.AddDataChange(gameObject, OnDataChange);

        var list = dataHandler.GetGoodsData(UniqueType.GetAvatar(AvatarSubType.MusicalInstrument));
        var list1 = dataHandler.GetGoodsData(UniqueType.GetUgcAvatar(AvatarSubType.MusicalInstrument));
        if ((list == null || list.Count <= 2) && (list1 == null || list1.Count <= 2))
        {
            // 没有任何乐器
            DefualtOn(DefaultId);
            ShowTip("暂无购买的乐器\n可以去商城获取乐器哦");
            miSourceUI.gameObject.SetActive(false);
            assetsList.gameObject.SetActive(false);
            sureButton.gameObject.SetActive(false);
            return;
        }

        assetsList.Data = new LazyDataHelper<GoodsData>(assetsList, CreateNewModel);
        assetsList.Init();
        ColorUtility.TryParseHtmlString("#DAD0FF", out assetsList.BgColor);

        miSourceUI.SetCallback(OnUgcSource);

        OnRedDotUpdate();

        DefualtOn(args[0] as string);

        miSourceUI.DefualtOn(bagTuple.Item1);
    }

    internal void DefualtOn(string id)
    {
        bagTuple.Item2 = id;
        if (id == DefaultId) defaultSelect.SetActive(true);
    }

    internal void OnDataChange(AssetsData[] changes)
    {
        OnRedDotUpdate();

        var datas = dataHandler.GetGoodsData(GetSelectedClassType());
        switch (bagTuple.Item1)
        {
            case MISource.Source.Bud:
                assetsDatas.SetData(datas, BudPredicate);
                if (assetsDatas.Count() == 0) ShowTip("暂无购买的乐器\n可以去商城获取乐器哦");
                break;
            case MISource.Source.Buy:
                assetsDatas.SetData(datas, BuyPredicate);
                if (assetsDatas.Count() == 0) ShowTip("暂无购买的乐器\n可以去商城获取乐器哦");
                break;
            case MISource.Source.Create:
                assetsDatas.SetData(datas, CreatePredicate);
                if (assetsDatas.Count() == 0) ShowTip("暂无创作的乐器\n可以去音乐工作室创作乐器哦");
                break;
        }
        assetsList.Data.ResetItems(assetsDatas.Count());
    }

    public void OnRedDotUpdate()
    {
        dataHandler.RedDotPgc.TryGetValue(UniqueType.GetAvatar(AvatarSubType.MusicalInstrument), out var count);
        miSourceUI.SetRedDot(MISource.Source.Bud, count > 0);
        var ugcType = UniqueType.GetUgcAvatar(AvatarSubType.MusicalInstrument);
        dataHandler.RedDotUgcCreator.TryGetValue(ugcType, out var count1);
        dataHandler.RedDotUgcBuy.TryGetValue(ugcType, out var count2);
        miSourceUI.SetRedDot(MISource.Source.Create, count1 > 0);
        miSourceUI.SetRedDot(MISource.Source.Buy, count2 > 0);
    }

    internal bool BudPredicate(GoodsData goodsData)
    {
        if (goodsData.ButtonType == ButtonType.Design) return false;
        if (goodsData.ButtonType == ButtonType.TakeOff) return false;
        if (goodsData.GoodsType != GoodsType.SinglePgc && goodsData.GoodsType != GoodsType.SingleUgc) return false;
        if (goodsData.Assets == null || goodsData.Assets.Count != 1) return false;
        if (!goodsData.IsOwned) return false;
        return true;
    }

    internal bool CreatePredicate(GoodsData goodsData)
    {
        if (goodsData.ButtonType == ButtonType.Design) return false;
        if (goodsData.ButtonType == ButtonType.TakeOff) return false;
        if (goodsData.GoodsType != GoodsType.SinglePgc && goodsData.GoodsType != GoodsType.SingleUgc) return false;
        if (goodsData.Assets == null || goodsData.Assets.Count != 1) return false;
        if (!goodsData.IsOwned) return false;
        var asset = goodsData.Assets[0];
        if (!(asset is UGCAssetsData)) return false;
        return asset.InventoryData.Tag == Network.Message.BackpackTag.Creator;
    }

    internal bool BuyPredicate(GoodsData goodsData)
    {
        if (goodsData.ButtonType == ButtonType.Design) return false;
        if (goodsData.ButtonType == ButtonType.TakeOff) return false;
        if (goodsData.GoodsType != GoodsType.SinglePgc && goodsData.GoodsType != GoodsType.SingleUgc) return false;
        if (goodsData.Assets == null || goodsData.Assets.Count != 1) return false;
        if (!goodsData.IsOwned) return false;
        var asset = goodsData.Assets[0];
        if (!(asset is UGCAssetsData)) return false;
        return asset.InventoryData.Tag == Network.Message.BackpackTag.ErrBackpackTag;
    }

    internal int GetSelectedClassType()
    {
        return UniqueType.Get(bagTuple.Item1 == MISource.Source.Bud ? ResourceType.Avatar : ResourceType.UgcAvatar, (int)AvatarSubType.MusicalInstrument);
    }

    internal void OnUgcSource(MISource.Source source)
    {
        bagTuple.Item1 = source;

        assetsList.gameObject.SetActive(true);
        assetsList.OnItemSelected = OnItemSelected;
        var datas = dataHandler.GetGoodsData(GetSelectedClassType());
        ShowTip("");
        switch (source)
        {
            case MISource.Source.Bud:
                assetsDatas.SetData(datas, BudPredicate);
                if (assetsDatas.Count() == 0) ShowTip("暂无购买的乐器\n可以去商城获取乐器哦");
                break;
            case MISource.Source.Buy:
                assetsDatas.SetData(datas, BuyPredicate);
                if (assetsDatas.Count() == 0) ShowTip("暂无购买的乐器\n可以去商城获取乐器哦");
                break;
            case MISource.Source.Create:
                assetsDatas.SetData(datas, CreatePredicate);
                if (assetsDatas.Count() == 0) ShowTip("暂无创作的乐器\n可以去音乐工作室创作乐器哦");
                break;
        }
        assetsList.Data.ResetItems(assetsDatas.Count());
    }

    public void ShowTip(string str)
    {
        tipText.gameObject.SetActive(true);
        if (string.IsNullOrEmpty(str))
        {
            tipText.SetText("");
        }
        else
        {
            tipText.SetLocalText(str);
        }
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

        bagTuple.Item2 = data.Id;
        bagTuple.Item3 = data.GoodsType == GoodsType.SinglePgc ? true : false;
        defaultSelect.SetActive(false);

        assetsList.Data.ResetItems(assetsDatas.Count());
    }

    internal void OnDefaultSelected()
    {
        bagTuple.Item2 = DefaultId;
        bagTuple.Item3 = true;
        defaultSelect.SetActive(true);
        assetsList.Data?.ResetItems(assetsDatas.Count());
    }

    internal void Design()
    {
        // 添加跳转
        switch (bagTuple.Item1)
        {
            case MISource.Source.Bud:
                break;
            case MISource.Source.Buy:
                break;
            case MISource.Source.Create:
                break;
        }
    }

    public GoodsData CreateNewModel(int index)
    {
        var assetsData = assetsDatas.Get(index);
        assetsData.Selected = bagTuple.Item2 == assetsData.Id;
        return assetsData;
    }

    internal void OnSureClick()
    {
        var miData = AccountDataManager.Inst.TryListenMIData;
        if (miData.isPgc != bagTuple.Item3 || miData.id != bagTuple.Item2)
        {
            AccountDataManager.Inst.TryListenMIData = new MIData()
            {
                id = bagTuple.Item2,
                isPgc = bagTuple.Item3
            };
            Message.MessageHelper.Broadcast(Message.MessageName.OnTryListenMIChange);
        }
        CloseSelf();
    }
}
