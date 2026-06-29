using System;
using System.Collections;
using System.Collections.Generic;
using BUD.AnimPose;
using Com.TheFallenGames.OSA.DataHelpers;
using Game.Avatar;
using Game.Pet;
using Game.Store;
using GameData;
using GameData.BaseInfo;
using GameData.PgcData;
using Newtonsoft.Json;
using Pb.Base;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.UI;
using xasset;
public class UgcGameVehicleView : MonoBehaviour
{
    public MISource resMISource;
    [SerializeField] private Text emptyTip;
    [SerializeField] private Button GoStoreBtn;

    public string CurSelectId;

    /// <summary>选中载具后的覆盖回调（设置后替代默认"仅召唤"逻辑，用于"召唤+伙伴上车"）。</summary>
    public Action<VehicleInfo> OnItemSelectedOverride;
    [SerializeField] private FittingRoomAdapter assetsList;
    [SerializeField] private VehicleSubTypeSelect subTypeSelect;
    private VehicleSubType vehicleSubType = VehicleSubType.SingleVehicle;
    // 载具来源：true=官方 PGC（ResourceType.Vehicle），false=社区 UGC。由顶层 animMISource(官方/社区) 驱动，与 resMISource(创作/购买) 解耦。
    private bool _sourcePgc;
    // 兼容旧用法（CameraModeNpcMenu）：开启后 resMISource 的「商城」分页加载 PGC 载具。新面板请改用 animMISource + SetSourcePgc。
    private bool _includePgc;
    // 当前列表是否处于 PGC 加载模式（供 CreatePredicate / OnItemSelected 区分 UGC/PGC）
    private bool _pgcMode;
    private GoodsDataClassifyList assetsDatas = new();
    private AvatarBagSceneHandler dataHandler;
    private Action closeSelf;
    private bool isCalling = false;
    public enum ResVehicleType
    {
        Create,
        Store
    }

    private ResVehicleType curResVehicleType = ResVehicleType.Create;
    
    
    public void OnStart(VehicleSubType subType,Action close)
    {
        vehicleSubType = subType;
        closeSelf = close;
        dataHandler = AssetsDataManager.GetData<AvatarBagSceneHandler>();
        dataHandler.AddDataChange(this.gameObject, OnDataChange);
        assetsList.Data = new LazyDataHelper<GoodsData>(assetsList, CreateNewModel);
        assetsList.Init();
        assetsList.OnItemSelected = OnItemSelected;
        resMISource.SetCallback(OnResValueChange);
        GoStoreBtn.onClick.AddListener(GotoAvatarStore);
        subTypeSelect.OnSelectedAction = (isSingle) => ChangeVehicleType(isSingle);
    }
    private void OnEnable()
    {
        resMISource.SetCallback(OnResValueChange);
    }

    private void GotoAvatarStore()
    {
        FittingRoomPanel panel = null;
        if (vehicleSubType == VehicleSubType.SingleVehicle)
        {
            panel = UIManager.Inst.OpenPanel<FittingRoomPanel>(PanelId.FittingRoomPanel);
            panel.JumpTo(MainTabs.Tab.Ugc, GameData.PgcData.UniqueType.Get(GameData.PgcData.ResourceType.UgcVehicle, (int)GameData.PgcData.VehicleSubType.SingleVehicle));
        }
        else
        {
            panel = UIManager.Inst.OpenPanel<FittingRoomPanel>(PanelId.FittingRoomPanel, true);
            panel.JumpTo(MainTabs.Tab.Ugc, GameData.PgcData.UniqueType.Get(GameData.PgcData.ResourceType.UgcVehicle, (int)GameData.PgcData.VehicleSubType.DoubleVehicle));
        }

     
        closeSelf?.Invoke();
    }


    
    public void SetDefaultMISource(UIEmoteType emoteType)
    {
        curResVehicleType = ResVehicleType.Store;
        resMISource.DefualtOn(MISource.Source.Create);
        //emptyTip.gameObject.SetActive(emoteType == UIEmoteType.Vehicle);
    }

    /// <summary>设置载具来源：true=官方 PGC，false=社区 UGC。由顶层 animMISource 驱动，与 resMISource(创作/购买) 解耦。</summary>
    public void SetSourcePgc(bool isPgc)
    {
        _sourcePgc = isPgc;
        if (isPgc)
        {
            // 官方 PGC：不经 resMISource，直接按来源加载
            UpdateAssetDatas();
        }
        else
        {
            // 社区 UGC：默认选中「创作」(Bud槽→Create)，由 DefualtOn 同步 resMISource 选中态、curResVehicleType 与列表数据，避免选中态与数据不一致
            resMISource.DefualtOn(MISource.Source.Bud);
        }
    }


    public void OnResValueChange(MISource.Source source)
    {
        curResVehicleType = source == MISource.Source.Bud ? ResVehicleType.Create : ResVehicleType.Store;
        UpdateAssetDatas();
    }

    public void ChangeVehicleType(bool isSingle)
    {
        vehicleSubType = isSingle ? VehicleSubType.SingleVehicle : VehicleSubType.DoubleVehicle;
        UpdateAssetDatas();
    }

    /// <summary>锁定为仅展示双人载具，并隐藏单/双切换（用于伙伴上车场景）。</summary>
    /// <param name="includePgc">兼容旧用法：true 时 resMISource「商城」分页加载 PGC。新面板请用 animMISource + SetSourcePgc，保持默认 false。</param>
    public void LockToDouble(bool includePgc = false)
    {
        _includePgc = includePgc;
        if (subTypeSelect != null) subTypeSelect.gameObject.SetActive(false);
        ChangeVehicleType(false);
    }

    public void ChangeAnimType(VehicleSubType subType)
    {
        vehicleSubType = subType;
        UpdateAssetDatas();
    }

    private void UpdateAssetDatas()
    {
        assetsList.gameObject.SetActive(true);
        assetsList.OnItemSelected = OnItemSelected;
        // PGC 来源加载官方载具（ResourceType.Vehicle），否则加载 UGC 载具。_sourcePgc=新两级模型；_includePgc=旧用法兼容（CameraModeNpcMenu）
        _pgcMode = _sourcePgc || (_includePgc && curResVehicleType == ResVehicleType.Store);
        var resType = _pgcMode ? ResourceType.Vehicle : ResourceType.UgcVehicle;
        var datas = dataHandler.GetGoodsData(UniqueType.Get(resType, (int) vehicleSubType));
        assetsDatas.SetData(datas, CreatePredicate);
        emptyTip.gameObject.SetActive(assetsDatas.Count() <= 0);
        GoStoreBtn.gameObject.SetActive(false);
        if (assetsDatas.Count() <= 0)
        {
            string text;
            if (_pgcMode)
            {
                text = "暂无官方双人载具";
            }
            else if (curResVehicleType == ResVehicleType.Store)
            {
                text = "可前往商城获取";
                GoStoreBtn.gameObject.SetActive(true);
            }
            else
            {
                text = "还没有创作过载具，可前往载具编辑器创作";
            }
            emptyTip.SetLocalText(text);
        }
        assetsList.Data.ResetItems(assetsDatas.Count());
    }

    private bool CreatePredicate(GoodsData goodsData)
    {
        if (goodsData.ButtonType == ButtonType.Design) return false;
        if (goodsData.ButtonType == ButtonType.UgcEmoteIdle) return false;
        if (goodsData.ButtonType == ButtonType.TakeOff) return false;
        if (goodsData.GoodsType != GoodsType.SinglePgc && goodsData.GoodsType != GoodsType.SingleUgc) return false;
        if (goodsData.Assets == null || goodsData.Assets.Count != 1) return false;
        if (!goodsData.IsOwned) return false;
        var asset = goodsData.Assets[0];
        // PGC 模式：只接受 PGC 载具（VehicleAssetsData），无创作者/购买标签区分
        if (_pgcMode)
            return asset is VehicleAssetsData;
        if (!(asset is UgcVehicleAssetsData)) return false;
        var ugcAsset = asset as UgcVehicleAssetsData;
        var tag = curResVehicleType == ResVehicleType.Create
            ? Network.Message.BackpackTag.Creator
            : Network.Message.BackpackTag.ErrBackpackTag;
        if (asset.InventoryData.Tag != tag) return false;
        return true;
    }

    public GoodsData CreateNewModel(int index)
    {
        var assetsData = assetsDatas.Get(index);
        assetsData.Selected = CurSelectId == assetsData.Id;
        return assetsData;
    }

    private void OnItemSelected(GoodsData data)
    {
        var asset = data.GetFirstAsset<AssetsData>();
        VehicleInfo vehicleInfo;
        if (asset is VehicleAssetsData)
        {
            // PGC 载具：背包无 UgcInfo，按配置 ID 构造 VehicleInfo（id/templateId 用于召唤端识别 PGC）
            vehicleInfo = new VehicleInfo
            {
                id = asset.Id,
                templateId = asset.Id,
                vehicleType = (int)VehicleType.Double,
            };
        }
        else
        {
            vehicleInfo = asset.UgcInfo.UgcInfo as VehicleInfo;
        }
        if (OnItemSelectedOverride != null)
        {
            OnItemSelectedOverride(vehicleInfo);
        }
        else
        {
            EnterVehicle(vehicleInfo);
        }
        closeSelf?.Invoke();
    }

     private void EnterVehicle(VehicleInfo vehicleInfo)
     {
        if(isCalling){
            TipPanel.ShowToast("正在呼叫载具");
            return;
        }

        if(AvatarController.Inst.SelfWrap.HasVehicle()
        && AvatarController.Inst.SelfWrap.ChaData.vehicleData.id == vehicleInfo.id){
            TipPanel.ShowToast("你已经在当前载具上了");
            return;
        }

        //if (BuddyLinkEmoteManager.Inst.IsInBuddyLinkState(AccountDataManager.Inst.Uid))
        //{
        //    TipPanel.ShowToast("双人动作/双人牵手时，不能召唤载具");
        //    return;
        //}

        if (AvatarController.Inst.SelfStateController != null &&
            !AvatarController.Inst.SelfStateController.CanCallVehicle(out var toastReason))
        {
            TipPanel.ShowToast(toastReason);
            return;
        }


        if (vehicleInfo != null){
            isCalling = true;
            Vector3 position = AvatarController.Inst.SelfWrap.Avatar.transform.position;
            Quaternion rotation = AvatarController.Inst.SelfWrap.Avatar.transform.rotation;
            GameVehicleManager.Inst.SendCreateVehicle(
                AccountDataManager.Inst.Uid, 
                vehicleInfo.id, 
                JsonConvert.SerializeObject(vehicleInfo), 
                position, 
                rotation,
                (isSuccess)=>{
                    if(isSuccess){
                        TipPanel.ShowToast("载具召唤中");
                    }else{
                        TipPanel.ShowToast("请先下车再召唤载具");
                    }
                });
        }
     }
     

    private void OnDataChange(AssetsData[] changes)
    {
        var resType = _pgcMode ? ResourceType.Vehicle : ResourceType.UgcVehicle;
        assetsDatas.SetData(dataHandler.GetGoodsData(UniqueType.Get(resType, (int) vehicleSubType)));
        assetsList.Data.ResetItems(assetsDatas.Count());
    }

}
