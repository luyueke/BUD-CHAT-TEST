using System;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.DataHelpers;
using Game.Avatar;
using Game.MusicalInstrument;
using Game.Store;
using GameData.PgcData;
using Message;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.UI;

public class MusicalInstrumentEmoView : MonoBehaviour
{
    [SerializeField] private MISource innerMISource;            // 我购买 / 我创作（仅 UGC 模式显示）
    [SerializeField] private FittingRoomAdapter assetsList;
    [SerializeField] private Text emptyTip;
    [SerializeField] private Button goStoreBtn;
    [SerializeField] private Text goStoreBtnText;
    private AvatarBagSceneHandler dataHandler;
    private GoodsDataClassifyList assetsDatas = new();
    private MISource.Source outerSource = MISource.Source.Bud;


    public enum ResMIType
    {
        Create,   // 我创作
        Store     // 我购买
    }
    // 内层 MISource 2-toggle 约定（与 UgcEmoView / UgcGameVehicleView 一致）：
    //   Source.Bud (toggle[0])    -> 我创作
    //   Source.Create (toggle[1]) -> 我购买（Store）
    private ResMIType curResMIType = ResMIType.Store;

    private Action closeSelf;
    public string CurSelectId;

    private bool waitingEquip;
    private bool waitingRestore;

    public void OnStart(Action close)
    {
        closeSelf = close;
        waitingEquip = false;
        waitingRestore = false;

        dataHandler = AssetsDataManager.GetData<AvatarBagSceneHandler>();
        dataHandler.AddDataChange(this.gameObject, OnDataChange);

        assetsList.Data = new LazyDataHelper<GoodsData>(assetsList, CreateNewModel);
        assetsList.Init();
        assetsList.OnItemSelected = OnItemSelected;

        innerMISource.SetCallback(OnInnerSourceChange);
        // 防止多次 OnStart 时重复注册（UnityEvent 不去重）
        goStoreBtn.onClick.RemoveAllListeners();
        goStoreBtn.onClick.AddListener(GotoStore);

        // 初始化选中态为当前已装备乐器
        InitCurrentSelection();

        // listener 只在生命周期入口/出口处增删，避免在 Broadcast 期间改集合
        MessageHelper.AddListener<bool>(MessageName.OnGetPlayerHoldInstrument, OnInstrumentReady);
        MessageHelper.AddListener(MessageName.OnExitPlayInstrumentAnim, OnExitRestore);
    }

    private void InitCurrentSelection()
    {
        var avatar = AccountDataManager.Inst.UserInfo?.avatarInfo;
        if (avatar == null) return;
        int pgcType = UniqueType.GetAvatar(AvatarSubType.MusicalInstrument);
        int ugcType = UniqueType.GetUgcAvatar(AvatarSubType.MusicalInstrument);
        var part = avatar.partDatas?.Find(p => p.Type == pgcType || p.Type == ugcType);
        if (part == null) return;
        // PGC：part.Id = data.Id；UGC：ChangeSkinData 写入 part.UId = skinInfo.id = data.Id
        CurSelectId = part.Type == ugcType ? part.UId : part.Id;
    }

    private void OnEnable()
    {
        if (innerMISource != null) innerMISource.SetCallback(OnInnerSourceChange);
    }

    public void SetOuterSource(MISource.Source source)
    {
        outerSource = source;
        bool isUgc = source != MISource.Source.Bud;
        innerMISource.gameObject.SetActive(isUgc);
        if (isUgc)
        {
            // toggle[1] = 我购买（Store）作为默认
            curResMIType = ResMIType.Store;
            innerMISource.DefualtOn(MISource.Source.Create);
        }
        else
        {
            RefreshList();
        }
    }

    private void OnInnerSourceChange(MISource.Source source)
    {
        curResMIType = source == MISource.Source.Bud ? ResMIType.Create : ResMIType.Store;
        RefreshList();
    }

    private void GotoStore()
    {
        if (curResMIType == ResMIType.Create)
        {
            // 跳音乐工作室（与 Hall 入口一致）
            UIManager.Inst.OpenPanel(PanelId.InstrumentStudioCategoryPanel);
        }
        else
        {
            // 跳社区商城的乐器分类
            var panel = UIManager.Inst.OpenPanel<FittingRoomPanel>(PanelId.FittingRoomPanel);
            panel.JumpTo(MainTabs.Tab.Ugc, UniqueType.GetUgcAvatar(AvatarSubType.MusicalInstrument));
        }
        closeSelf?.Invoke();
    }

    private int GetCurrentClassType()
    {
        return outerSource == MISource.Source.Bud
            ? UniqueType.GetAvatar(AvatarSubType.MusicalInstrument)        // 10024
            : UniqueType.GetUgcAvatar(AvatarSubType.MusicalInstrument);    // 50024
    }

    private void RefreshList()
    {
        assetsList.gameObject.SetActive(true);
        assetsList.OnItemSelected = OnItemSelected;
        var datas = dataHandler.GetGoodsData(GetCurrentClassType());
        assetsDatas.SetData(datas, GetPredicate());

        bool isEmpty = assetsDatas.Count() <= 0;
        emptyTip.gameObject.SetActive(isEmpty);
        bool showStoreBtn = isEmpty && outerSource != MISource.Source.Bud;
        goStoreBtn.gameObject.SetActive(showStoreBtn);
        if (isEmpty) emptyTip.SetLocalText(GetEmptyTipText());
        if (showStoreBtn && goStoreBtnText != null)
        {
            goStoreBtnText.SetLocalText(curResMIType == ResMIType.Create ? "音乐工作室" : "社区商城");
        }
        assetsList.Data.ResetItems(assetsDatas.Count());
    }

    private string GetEmptyTipText()
    {
        if (outerSource == MISource.Source.Bud) return "暂无购买的乐器，可前往商城获取";
        return curResMIType == ResMIType.Create
            ? "暂无创作的乐器，可前往音乐工作室创作"
            : "暂无购买的乐器，可前往商城获取";
    }

    private Func<GoodsData, bool> GetPredicate()
    {
        if (outerSource == MISource.Source.Bud) return BudPredicate;
        return curResMIType == ResMIType.Create ? CreatePredicate : BuyPredicate;
    }

    private static bool BudPredicate(GoodsData goodsData)
    {
        if (goodsData.ButtonType == ButtonType.Design) return false;
        if (goodsData.ButtonType == ButtonType.TakeOff) return false;
        if (goodsData.GoodsType != GoodsType.SinglePgc) return false;
        if (goodsData.Assets == null || goodsData.Assets.Count != 1) return false;
        return goodsData.IsOwned;
    }

    private static bool CreatePredicate(GoodsData goodsData)
    {
        if (goodsData.ButtonType == ButtonType.Design) return false;
        if (goodsData.ButtonType == ButtonType.TakeOff) return false;
        if (goodsData.GoodsType != GoodsType.SingleUgc) return false;
        if (goodsData.Assets == null || goodsData.Assets.Count != 1) return false;
        if (!goodsData.IsOwned) return false;
        var asset = goodsData.Assets[0];
        if (!(asset is UGCAssetsData)) return false;
        return asset.InventoryData.Tag == Network.Message.BackpackTag.Creator;
    }

    private static bool BuyPredicate(GoodsData goodsData)
    {
        if (goodsData.ButtonType == ButtonType.Design) return false;
        if (goodsData.ButtonType == ButtonType.TakeOff) return false;
        if (goodsData.GoodsType != GoodsType.SingleUgc) return false;
        if (goodsData.Assets == null || goodsData.Assets.Count != 1) return false;
        if (!goodsData.IsOwned) return false;
        var asset = goodsData.Assets[0];
        if (!(asset is UGCAssetsData)) return false;
        return asset.InventoryData.Tag == Network.Message.BackpackTag.ErrBackpackTag;
    }

    public GoodsData CreateNewModel(int index)
    {
        var data = assetsDatas.Get(index);
        data.Selected = CurSelectId == data.Id;
        return data;
    }

    private void OnDataChange(AssetsData[] changes)
    {
        if (!gameObject.activeInHierarchy) return;
        RefreshList();
    }

    private void OnItemSelected(GoodsData data)
    {
        if (waitingEquip || waitingRestore) return;
        CurSelectId = data.Id;

        var newAvatar = AccountDataManager.Inst.UserInfo.avatarInfo;
        if (newAvatar == null)
        {
            TipPanel.ShowToast("装备失败");
            return;
        }

        // 点击已装备的乐器时，直接进入演奏状态，无需重新同步
        if (IsInstrumentAlreadyEquipped(newAvatar, data))
        {
            waitingRestore = true;
            GuestInstrumentOPManager.Inst.EnterPlayMusicInstrumentState();
            closeSelf?.Invoke();
            return;
        }

        if (!ApplyInstrumentPart(newAvatar, data))
            return;

        waitingEquip = true;
        AccountDataManager.Inst.SyncAvatarData(newAvatar, success =>
        {
            if (success) return;
            waitingEquip = false;
            TipPanel.ShowToast("装备失败");
        });
    }

    private bool IsInstrumentAlreadyEquipped(CharacterData avatar, GoodsData data)
    {
        int pgcType = UniqueType.GetAvatar(AvatarSubType.MusicalInstrument);
        int ugcType = UniqueType.GetUgcAvatar(AvatarSubType.MusicalInstrument);
        return avatar.partDatas?.Exists(p =>
        {
            if (p.Type == pgcType) return p.Id == data.Id;
            if (p.Type == ugcType) return p.UId == data.Id;
            return false;
        }) == true;
    }

    private bool ApplyInstrumentPart(CharacterData avatar, GoodsData data)
    {
        int pgcType = UniqueType.GetAvatar(AvatarSubType.MusicalInstrument);
        int ugcType = UniqueType.GetUgcAvatar(AvatarSubType.MusicalInstrument);
        // 先移除已有的乐器部件，避免 PGC / UGC 同时存在
        avatar.partDatas?.RemoveAll(p => p.Type == pgcType || p.Type == ugcType);

        if (data.GoodsType == GoodsType.SinglePgc)
        {
            avatar.partDatas?.Add(new CharacterPartData
            {
                Type = pgcType,
                Id = data.Id,
            });
            return true;
        }

        var ugcAsset = data.Assets[0] as UGCAssetsData;
        var skinInfo = ugcAsset?.UgcInfo?.skinInfo;
        if (skinInfo == null)
        {
            TipPanel.ShowToast("装备失败");
            return false;
        }
        // ChangeSkinData 负责写入所有渲染字段（包括 Id=templateId, UId=skinInfo.id）
        avatar.ChangeSkinData(skinInfo);
        return true;
    }

    private void OnInstrumentReady(bool ok)
    {
        if (!waitingEquip) return;
        waitingEquip = false;
        if (!ok) return;

        // 面板已关闭（加载期间用户关闭）时，不进入演奏状态，避免移动异常
        if (!gameObject.activeInHierarchy) return;

        waitingRestore = true;
        GuestInstrumentOPManager.Inst.EnterPlayMusicInstrumentState();
        closeSelf?.Invoke();
    }

    private void OnDisable()
    {
        // waitingEquip 兜底：加载期间面板被隐藏时清理，防止状态永久卡死
        if (waitingEquip)
            waitingEquip = false;
    }

    private void OnExitRestore()
    {
        if (!waitingRestore) return;
        waitingRestore = false;
        // MoveInstrumentToBack 已将模型放回 instrument_back 挂点
        // PGC 和 UGC 均无需额外操作，模型自然留在背部
    }

    private void OnDestroy()
    {
        MessageHelper.RemoveListener<bool>(MessageName.OnGetPlayerHoldInstrument, OnInstrumentReady);
        MessageHelper.RemoveListener(MessageName.OnExitPlayInstrumentAnim, OnExitRestore);
        dataHandler?.RemoveDataChange(OnDataChange);
    }
}
