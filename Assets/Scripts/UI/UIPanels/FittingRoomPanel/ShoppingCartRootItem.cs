using System;
using Com.TheFallenGames.OSA.Util.IO;
using Game.Store;
using GameData;
using GameData.PgcData;
using UI.Manager;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class ShoppingCartItemData
{
    public string id;
    public int type;          // ResourceType
    public string iconPath;   // UGC cover URL；为空时用 id 当 PGC sprite name
    public int price;         // 月卡折后实付价
    public int originalPrice; // 月卡折前原价（与 price 相同表示无折扣）
    public string name;
    public int currencyType;  // CurrencyType
    public int subType;
    public int goodsType;     // GoodsType
    public bool isPet;        // 宠物试衣间加入的商品
}

public class ShoppingCartRootItem : MonoBehaviour
{

    Image icon;//物品图标
    RemoteImageBehaviour RemoteIcon;
    Image icon_buy;//购买消耗图标
    Image img_select;//选中框图标
    Text price;//购买消耗数量
    Toggle tog_select;//选中状态

    private ShoppingCartItemData _data;
    private bool _suppress;//为 true 时不向上抛 OnSelectChanged(全选等程序化设置时使用)

    /// <summary>选中状态变化时通知父级(由用户操作触发)</summary>
    public Action OnSelectChanged;

    /// <summary>点击 item 后通知父级，父级负责单选切换 img_select</summary>
    public Action<ShoppingCartRootItem> OnItemClick;

    public ShoppingCartItemData Data => _data;
    public bool IsSelected => tog_select != null && tog_select.isOn;

    void Awake()
    {
        icon = GameObjectEx.FindComponentByName<Image>(transform, "icon");
        RemoteIcon = GameObjectEx.FindComponentByName<RemoteImageBehaviour>(transform, "RemoteIcon");
        icon_buy = GameObjectEx.FindComponentByName<Image>(transform, "icon_buy");
        img_select = GameObjectEx.FindComponentByName<Image>(transform, "img_select");
        if (img_select != null) img_select.gameObject.SetActive(false);
        price = GameObjectEx.FindComponentByName<Text>(transform, "price");
        tog_select = GameObjectEx.FindComponentByName<Toggle>(transform, "tog_select");
        if (tog_select != null) tog_select.onValueChanged.AddListener(OnToggleSelect);
        transform.GetComponent<Button>()?.onClick.AddListener(() =>
        {
            if (_data == null || string.IsNullOrEmpty(_data.id)) return;
            // 剧场商品：与原商城 OnItemSelected 逻辑一致，打开 TheatreInfoPanel(Store)
            if ((ResourceType)_data.type == ResourceType.Theatre)
            {
                AssetsDataManager.GetTheatreInfo(_data.id, (ok, rsp) =>
                {
                    if (!ok) return;
                    var theatreInfo = rsp?.theatreInfo;
                    if (theatreInfo != null)
                        UIManager.Inst.OpenPanel(PanelId.TheatreInfoPanel, theatreInfo, (int)TheatreEnterType.Store);
                });
                return;
            }
            // 通知父级做单选切换（父级负责隐藏其他 item 的 img_select，显示本 item 的）
            OnItemClick?.Invoke(this);
            // 试衣间试穿
            var panel = UIManager.Inst.FindPanel<FittingRoomPanel>(PanelId.FittingRoomPanel);
            if (panel != null) panel.TryOnCartItem(_data.id, null);
        });
    }

    /// <summary>显示/隐藏「正穿着」标记 img_select</summary>
    public void SetTryOnShown(bool on)
    {
        if (img_select != null) img_select.gameObject.SetActive(on);
    }

    // 用户点击 tog_select 取消或者选中
    private void OnToggleSelect(bool isOn)
    {
        ApplyState(isOn);
        if (!_suppress) OnSelectChanged?.Invoke();
    }

    // 同步选中框显示与 ShoppingCartManager 的选中数据
    private void ApplyState(bool isOn)
    {
        if (_data == null) return;
        if (isOn) ShoppingCartManager.Inst.AddSelected(_data);
        else ShoppingCartManager.Inst.RemoveSelected(_data.id);
    }

    /// <summary>程序化设置选中状态(全选/取消全选),不会向上抛 OnSelectChanged</summary>
    public void SetSelected(bool isOn)
    {
        _suppress = true;
        if (tog_select == null || tog_select.isOn == isOn) ApplyState(isOn);
        else tog_select.isOn = isOn;//触发 OnToggleSelect -> ApplyState
        _suppress = false;
    }

    // 按 ResourceType 走对应接口拉封面（冷启动 iconPath 为空时使用）
    private void FetchCoverByType(string ugcId, ResourceType resType)
    {
        switch (resType)
        {
            case ResourceType.UgcPose:
                AssetsDataManager.GetPoseInfo(ugcId, (ok, rsp) =>
                {
                    if (!ok || RemoteIcon == null) return;
                    var cover = rsp?.UgcInfo?.cover;
                    if (!string.IsNullOrEmpty(cover)) RemoteIcon.Load(cover);
                });
                break;
            case ResourceType.UgcEmote:
                AssetsDataManager.GetUgcAnimInfo(ugcId, (ok, rsp) =>
                {
                    if (!ok || RemoteIcon == null) return;
                    var animInfo = rsp?.animInfo;
                    if (animInfo == null) return;
                    // cover 为空时用 animType 拼 CDN 模板图（与 UgcAnimEnterModelController 一致）
                    var cover = string.IsNullOrEmpty(animInfo.cover)
                        ? "https://cdn.budapp.cn/UgcAnimStudio/TemplateCover/UGCAnim_" + animInfo.animType + ".png"
                        : animInfo.cover;
                    RemoteIcon.Load(cover);
                });
                break;
            case ResourceType.MusicScore:
                AssetsDataManager.GetMusicScoreInfo(ugcId, (ok, rsp) =>
                {
                    if (!ok || RemoteIcon == null) return;
                    var cover = rsp?.UgcInfo?.cover;
                    if (!string.IsNullOrEmpty(cover)) RemoteIcon.Load(cover);
                });
                break;
            case ResourceType.AvatarCard:
                AssetsDataManager.GetActorInfo(ugcId, (ok, rsp) =>
                {
                    if (!ok || RemoteIcon == null) return;
                    var cover = rsp?.UgcInfo?.cover;
                    if (!string.IsNullOrEmpty(cover)) RemoteIcon.Load(cover);
                });
                break;
            case ResourceType.UgcVehicle:
                AssetsDataManager.GetUgcVehicleInfo(ugcId, (ok, rsp) =>
                {
                    if (!ok || RemoteIcon == null) return;
                    var cover = rsp?.UgcInfo?.cover;
                    if (!string.IsNullOrEmpty(cover)) RemoteIcon.Load(cover);
                });
                break;
            case ResourceType.Theatre:
                AssetsDataManager.GetTheatreInfo(ugcId, (ok, rsp) =>
                {
                    if (!ok || RemoteIcon == null) return;
                    var cover = rsp?.UgcInfo?.cover;
                    if (!string.IsNullOrEmpty(cover)) RemoteIcon.Load(cover);
                });
                break;
            case ResourceType.UgcAvatar:
            case ResourceType.UGCPetAvatar:
            default:
                // 皮肤/宠物皮肤及其他未覆盖类型走皮肤批量接口
                AssetsDataManager.GetUgcInfo(ugcId, server =>
                {
                    if (RemoteIcon == null) return;
                    var cover = server?.UgcInfo?.cover;
                    if (!string.IsNullOrEmpty(cover)) RemoteIcon.Load(cover);
                });
                break;
        }
    }

    public void SetData(ShoppingCartItemData data)
    {
        if (data == null) return;
        _data = data;
        SetSelected(ShoppingCartManager.Inst.IsSelected(data.id));//恢复选中状态
        if (price != null) price.text = data.price.ToString();
        if (icon_buy != null)
        {
            var sp = PgcUtils.LoadCurrencyIcon((CurrencyType)data.currencyType, gameObject);
            if (sp != null) icon_buy.sprite = sp;
        }
        if (RemoteIcon != null)
        {
            if (!string.IsNullOrEmpty(data.iconPath))
            {
                RemoteIcon.Load(data.iconPath);
            }
            else
            {
                // iconPath 为空（冷启动恢复）：按 ResourceType 走对应接口拉封面
                FetchCoverByType(data.id, (ResourceType)data.type);
            }
        }
    }
}
