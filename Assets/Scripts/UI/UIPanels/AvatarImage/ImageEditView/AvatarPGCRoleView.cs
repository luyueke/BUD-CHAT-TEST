using System;
using System.Collections.Generic;
using System.Linq;
using Game.Avatar;
using Game.BagSystem;
using GameData.PgcData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UGCAsset;
using UI.UIPanels.AvatarImage.ColorPicker;
using UnityEngine;
using UnityEngine.UI;

public abstract class IAvatarRoleItem : MonoBehaviour
{
    public RoleColorView colorView;
    public PaletteColorView paletteColorView;
    public RoleAdjustView adjustViewWithColor;
    public HsvColorView hsvColorView;
    public GameObject listView;
    protected Transform ColorBar;

    protected AvatarMenuType currentMenuType = AvatarMenuType.PrefabImage;
    public AvatarMenuType MenuType => currentMenuType;

    protected List<AvatarRoleItemView> roleItems = new List<AvatarRoleItemView>();
    protected List<AvatarRoleColorItemView> colorItems = new List<AvatarRoleColorItemView>();

    protected bool _isSetColor = false;
    protected bool _isShowColorView = false;

    protected string currentSelectedId = "";

    protected Action<AvatarMenuType, AvatarSubType, RoleActionType, AvatarRoleItemProtocol> ClickAction;
    protected Action<AvatarMenuType, RoleColorAction, string> ClickColorAction;
    protected Action<string> onColorChangeAction;
    protected Action<bool> showPaltte;
    protected Action<bool> showHsv;

    protected virtual void Awake()
    {
        InitUIIfNeed();
    }

    protected abstract void InitUIIfNeed();

    public void SetData(AvatarMenuType MenuType)
    {
        currentMenuType = MenuType;
    }

    public virtual void SetDefaultTab(AvatarSubType part) { }
    protected abstract void OnClickItem(RoleActionType actionType, AvatarRoleItemProtocol itemData);

    public void ShowColorBar(bool isShow)
    {
        ColorBar.gameObject.SetActive(isShow);
        UpdateHeight();
    }

    protected void UpdateHeight()
    {
        foreach (var remainSpace in transform.GetComponentsInChildren<FillRemainSpace>())
        {
            remainSpace.UpdateHeight();
        }
    }

    public void SetColor(bool setColor)
    {
        _isSetColor = setColor;
    }

    /// <summary>
    /// 是否显示选颜色view
    /// </summary>
    /// <param name="isShowColorView"></param>
    public void ShowColorView(bool isShowColorView)
    {
        _isShowColorView = isShowColorView;
    }

    public void ReloadColorList(string allColorId, string commonColorId)
    {
        paletteColorView.InitPaletteView(AvatarColorManager.GetColorList(allColorId), OnSelectColor);
        colorView.Init(AvatarColorManager.GetColorList(commonColorId), OnSelectColor);
    }

    protected void OnSelectColor(string colorData)
    {
        onColorChangeAction?.Invoke(colorData);
    }

    /// <summary>
    /// 更新当前选中态id
    /// </summary>
    /// <param name="itemId"></param>
    public void UpdateSelectedItem(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            return;
        }

        currentSelectedId = itemId;
        roleItems.ForEach(element => { element.UpdateSelected(element.ItemData.itemId == itemId); });
        colorItems.ForEach(element => { element.UpdateSelected(element.ItemData.itemId == itemId); });
    }

    public virtual void UpdateCollect(AvatarRoleItemProtocol itemData, bool isCollect)
    {
        var itemView = roleItems.Find(element => element.ItemData.itemId == itemData.itemId);
        if (itemView != null)
        {
            itemView.UpdateCollect(isCollect);
        }
    }

    public void RegisterListener(Action<AvatarMenuType, AvatarSubType, RoleActionType, AvatarRoleItemProtocol> clickAction = null,
        Action<string> onColorChangeAction = null,
        Action<bool> showPaltteAction = null,
        Action<bool> showHsvAction = null, Action<AvatarMenuType, RoleColorAction, string> colorAction = null)
    {
        this.ClickAction = clickAction;
        this.onColorChangeAction = onColorChangeAction;
        this.showPaltte = showPaltteAction;
        this.showHsv = showHsvAction;
        this.ClickColorAction = colorAction;
    }

    public void ShowListView(bool isShow)
    {
        listView.gameObject.SetActive(isShow);
    }
}

public class AvatarPGCRoleView : IAvatarRoleItem
{
    protected AvatarSubType currentPart;

    private GridLayoutGroup content;
    private AvatarRoleItemView roleItem;

    protected override void Awake()
    {
        base.Awake();

        SetupUI(convertDatas());
    }

    private List<AvatarRoleItemData> convertDatas()
    {
        List<AvatarRoleItemData> items = new List<AvatarRoleItemData>();
        if (MenuType == AvatarMenuType.Skin)
        {
            var paletter = new AvatarRoleItemData(AvatarColorManager.PaletteKey);
            items.Add(paletter);
            var list = AvatarColorManager.GetColorList(AvatarColorManager.SKIN_COM);
            foreach (var s in list)
            {
                var item = new AvatarRoleItemData(s);
                items.Add(item);
            }

            return items;
        }

        currentPart = AvatarConfigTool.PartType(MenuType);

        items = convertDatas(currentPart);

        return items;
    }

    private List<AvatarRoleItemData> convertDatas(AvatarSubType part)
    {
        List<AvatarRoleItemData> items = new List<AvatarRoleItemData>();

        var allBaseItems =
            UserBagSystem.Inst.GetAvatarBagDatasByType<BaseUserBagData>((int)part);
        var tmpItems = allBaseItems.Select(tmp => Es.DataTables.GetAvatarCommonData(tmp.id))
            .Where(tmp => tmp != null);
        items = tmpItems.Select(element => new AvatarRoleItemData(element)).ToList();

        return items;
    }

    private void SetupUI(List<AvatarRoleItemData> datas)
    {
        bool isShowColorBar = AvatarConfigTool.IsShowColorBar(MenuType);
        bool isSkin = currentMenuType == AvatarMenuType.Skin;
        ShowColorBar(isShowColorBar);

        if (isSkin)
        {
            content.cellSize = new Vector2(138, 138);
        }

        colorItems.ForEach(x => GameObject.Destroy(x.gameObject));
        colorItems.Clear();

        roleItems.ForEach(x => GameObject.Destroy(x.gameObject));
        roleItems.Clear();

        var colorItem = GameObjectEx.FindChildByName(transform, "ListView/AvatarRoleSkinItem")
            .GetComponent<AvatarRoleColorItemView>();
        for (int i = 0; i < datas.Count; i++)
        {
            var data = datas[i];
            if (isSkin)
            {
                var item = GameObject.Instantiate(colorItem, content.transform);
                item.gameObject.SetActive(true);
                item.SetData(data, OnClickColor);
                colorItems.Add(item);
                item.UpdateSelected(currentSelectedId == data.itemId);
            }
            else
            {
                var item = GameObject.Instantiate(roleItem, content.transform);
                item.gameObject.SetActive(true);
                item.SetData(data, OnClickItem);
                roleItems.Add(item);
                item.UpdateSelected(currentSelectedId == data.itemId);
                RedDotManager.Inst.CheckRedDot((int)currentMenuType, data.itemId, item.transform);
            }
        }

        if (roleItems.Count > 0)
        {
            List<string> idList = roleItems.Select(item => item.ItemData.itemId).ToList();

            var req = new CollectStatusRequest
            {
                idList = idList,
                statusType = 1
            };

            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.getCollect, Network.Http.HttpMethod.GET, JsonConvert.SerializeObject(req), (response) =>
            {
                PGCInfos pgcInfos = JsonConvert.DeserializeObject<PGCInfos>(response);

                foreach (var pgcItem in pgcInfos.pgcInfos)
                {
                    var item = roleItems.Find(item => item.ItemData.itemId == pgcItem.id);
                    if (item != null)
                    {
                        item.UpdateCollect("1".Equals(pgcItem.collectStatus));
                    }
                }
                
            }, (fail) =>
            {
                LoggerUtils.LogError("获取pgc收藏状态失败 ：" + fail);
            });
        }
    }

    protected override void InitUIIfNeed()
    {
        if (content == null)
        {
            ColorBar = GameObjectEx.FindChildByName(transform, "ColorBar");
            content = GameObjectEx.FindChildByName(transform, "ListView/Viewport/Content")
                .GetComponent<GridLayoutGroup>();
            roleItem = GameObjectEx.FindChildByName(transform, "ListView/AvatarRoleItem")
                .GetComponent<AvatarRoleItemView>();
        }

        hsvColorView.InitHsvView(OnSelectColor);

        adjustViewWithColor.showPaltte = showPaltte;
        adjustViewWithColor.showHsv = showHsv;
        adjustViewWithColor.showAdjust = boolean =>
        {
            if (!boolean && (_isSetColor || !_isShowColorView))
            {
                ShowColorBar(false);
            }
        };
    }

    protected override void OnClickItem(RoleActionType actionType, AvatarRoleItemProtocol itemData)
    {
        if (actionType == RoleActionType.Item)
        {
            roleItems.ForEach(element =>
            {
                bool isSelect = element.ItemData.itemId == itemData.itemId;
                element.UpdateSelected(isSelect);
            });
            currentSelectedId = itemData.itemId;
        }

        ClickAction?.Invoke(MenuType, currentPart, actionType, itemData);
    }

    private void OnClickColor(RoleColorAction action, string hex)
    {
        colorItems.ForEach(element =>
        {
            bool isSelect = element.ItemData.itemId == hex;
            element.UpdateSelected(isSelect);
        });
        currentSelectedId = hex;
        ClickColorAction?.Invoke(MenuType, action, hex);
    }
}