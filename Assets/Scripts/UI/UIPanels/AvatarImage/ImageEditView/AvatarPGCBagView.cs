using System;
using System.Collections.Generic;
using System.Linq;
using Game.Avatar;
using Game.BagSystem;
using GameData.PgcData;
using Network.Http;
using Network;
using Newtonsoft.Json;
using UGCAsset;
using UI.UIPanels.AvatarImage.ColorPicker;
using UnityEngine;
using UnityEngine.UI;

public class AvatarBagInfo
{
    public List<AvatarRoleItemView> items = new List<AvatarRoleItemView>();
    public string selectedId = "";
}

public class AvatarPGCBagView : MonoBehaviour
{
    public AvatarMenuType MenuType
    {
        get { return currentMenuType; }
    }

    private AvatarMenuType currentMenuType = AvatarMenuType.Bag;

    private AvatarRoleItemView roleItem;

    private Dictionary<AvatarSubType, AvatarBagInfo> _infos = new Dictionary<AvatarSubType, AvatarBagInfo>();
    private Dictionary<AvatarSubType, GameObject> bagViews = new Dictionary<AvatarSubType, GameObject>();
    private Dictionary<AvatarSubType, AvatarTabItem> tabViews = new Dictionary<AvatarSubType, AvatarTabItem>();
    private Action<AvatarSubType, RoleActionType, AvatarRoleItemProtocol> ClickAction;

    private List<AvatarSubType> bagTypes = new List<AvatarSubType>
    {
        AvatarSubType.Backpack, AvatarSubType.Crossbody, AvatarSubType.Cape
    };

    private AvatarSubType ActionPart = AvatarSubType.Backpack;
    
    public RoleAdjustView adjustViewWithColor;
    public RoleColorView colorView;
    public PaletteColorView paletteColorView;
    public GameObject bagView;
    protected bool _isSetColor = false;
    private GridLayoutGroup content;
    // protected Transform ColorBar;
    public HsvColorView hsvColorView;
    protected Action<string> onColorChangeAction;
    protected bool _isShowColorView = false;
    protected Action<AvatarMenuType, RoleColorAction, string> ClickColorAction;

    protected Action<bool> showHsv;



    public Action<bool> showPaltte;

    private void Awake()
    {
        InitDefaultData();
        
        InitUIIfNeed();

        SetupUI();

        onClickTab(ActionPart);
    }
    public void SetColor(bool setColor)
    {
        _isSetColor = setColor;
    }
    
    public void ReloadColorList(string allColorId, string commonColorId)
    {
        paletteColorView.InitPaletteView(AvatarColorManager.GetColorList(allColorId), OnSelectColor);
        colorView.Init(AvatarColorManager.GetColorList(commonColorId), OnSelectColor);
    }
    private void SetupUI()
    {
        InitTabView();

        List<string> idList = new List<string>();

        foreach (var avatarPartEnum in bagTypes)
        {
            var Page = _infos[avatarPartEnum];
            Page.items.ForEach(x => GameObject.Destroy(x.gameObject));
            
            List<AvatarRoleItemView> itemViews = new List<AvatarRoleItemView>();

            var allBaseItems = UserBagSystem.Inst.GetAvatarBagDatasByType<BaseUserBagData>((int)avatarPartEnum);

            var tmpItems = allBaseItems.Select(tmp => Es.DataTables.GetAvatarCommonData(tmp.id))
                .Where(tmp => tmp != null);
            // var fixedItems =
            //     JsonConvert.DeserializeObject<List<AvatarItemConvertData>>(
            //         JsonConvert.SerializeObject(tmpItems, settings));
            var datas = tmpItems.Select(element => new AvatarRoleItemData(element)).ToList();

            var aView = bagViews[avatarPartEnum];
            for (int i = 0; i < datas.Count; i++)
            {
                var data = datas[i];
                var item = GameObject.Instantiate(roleItem,
                    GameObjectEx.FindChildByName(aView, "Viewport/Content"));
                item.gameObject.SetActive(true);
                item.SetData(data, OnClickItem);
                itemViews.Add(item);
                item.UpdateSelected(Page.selectedId == data.itemId);
                RedDotManager.Inst.CheckRedDot((int)currentMenuType, data.itemId, item.transform);
            }

            Page.items = itemViews;
            _infos[avatarPartEnum] = Page;
            idList.AddRange(itemViews.Select(item => item.ItemData.itemId).ToList());
        }

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
                UpdateCollect(pgcItem.id, "1".Equals(pgcItem.collectStatus));
            }

        }, (fail) =>
        {
            LoggerUtils.LogError("获取pgc收藏状态失败 ：" + fail);
        });
    }

    private void InitUIIfNeed()
    {
        if (roleItem == null)
        {
            roleItem = GameObjectEx.FindChildByName(transform, "AvatarRoleItem").GetComponent<AvatarRoleItemView>();

            List<string> bagNames = new List<string>
            {
                "BagView", "CrossView", "CapeView"
            };

            for (int i = 0; i < bagTypes.Count; i++)
            {
                var name = bagNames[i];
                var view = GameObjectEx.FindChildByName(transform, name).gameObject;
                view.SetActive(false);
                var avatarPartEnum = bagTypes[i];
                bagViews[avatarPartEnum] = view;
            }
        }
        // if (content == null)
        // {
        //     ColorBar = GameObjectEx.FindChildByName(transform, "ColorBar");
        // }

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
    
    public void ShowColorBar(bool isShow)
    {
        // ColorBar.gameObject.SetActive(isShow);
        if (!isShow)
        {
            adjustViewWithColor.rolePaletteColorView.gameObject.SetActive(false);
        }
        UpdateHeight();
    }
    
    protected void UpdateHeight()
    {
        foreach (var remainSpace in transform.GetComponentsInChildren<FillRemainSpace>())
        {
            remainSpace.UpdateHeight();
        }
    }

    
    protected void OnSelectColor(string colorData)
    {
        onColorChangeAction?.Invoke(colorData);
    }


    private void InitTabView()
    {
        var bagTabView = GameObjectEx.FindChildByName(transform, "Tabar");
        var tabItem = GameObjectEx.FindChildByName(transform, "BagContent/Tabar/ItemImp").GetComponent<AvatarTabItem>();
        List<string> bagTitles = new List<string>
        {
            "背包", "斜挎包", "斗篷"
        };

        for (int i = 0; i < bagTypes.Count; i++)
        {
            var data = bagTitles[i];
            var item = GameObject.Instantiate(tabItem, bagTabView);
            item.gameObject.SetActive(true);
            item.SetData(data, bagTypes[i], onClickTab);
            tabViews[bagTypes[i]] = item;
        }
    }

    private void onClickTab(AvatarSubType itemId)
    {
        ActionPart = itemId;
        foreach (var avatarBagTabItem in tabViews)
        {
            bool isSelected = avatarBagTabItem.Key == itemId;
            avatarBagTabItem.Value.UpdateSelected(isSelected);
        }

        foreach (var element in bagViews)
        {
            bool isSelected = element.Key == itemId;
            element.Value.SetActive(isSelected);
        }
    }

    private void OnClickItem(RoleActionType actionType, AvatarRoleItemProtocol itemData)
    {
        var infos = _infos[ActionPart];
        if (infos == null)
        {
            return;
        }

        if (actionType == RoleActionType.Item)
        {
            infos.items.ForEach(element =>
            {
                bool isSelect = element.ItemData.itemId == itemData.itemId;
                element.UpdateSelected(isSelect);
            });
            infos.selectedId = itemData.itemId;
            _infos[ActionPart] = infos;
        }

        ClickAction?.Invoke(ActionPart, actionType, itemData);
    }

    public void UpdateCollect(string itemId, bool isCollect)
    {
        foreach (var info in _infos)
        {
            var item = info.Value.items.Find(element => element.ItemData.itemId == itemId);
            if (item != null)
            {
                item.UpdateCollect(isCollect);
                break;
            }
        }
    }

    private void InitDefaultData()
    {
        if (_infos.Count == 0)
        {
            foreach (var avatarPartEnum in bagTypes)
            {
                var Page = new AvatarBagInfo();
                _infos[avatarPartEnum] = Page;
            }
        }
    }

    /// <summary>
    /// 更新当前选中态id
    /// </summary>
    /// <param name="itemId"></param>
    public void UpdateSelectedItem(string bagId, string capeId, string crossId)
    {
        InitDefaultData();
        if (!string.IsNullOrEmpty(bagId) && _infos.ContainsKey(AvatarSubType.Backpack))
        {
            var infos = _infos[AvatarSubType.Backpack];
            if (infos != null)
            {
                infos.items.ForEach(element =>
                {
                    bool isSelect = element.ItemData.itemId == bagId;
                    element.UpdateSelected(isSelect);
                });
                infos.selectedId = bagId;
                _infos[AvatarSubType.Backpack] = infos;
            }
        }

        if (!string.IsNullOrEmpty(capeId) && _infos.ContainsKey(AvatarSubType.Cape))
        {
            var infos = _infos[AvatarSubType.Cape];
            if (infos != null)
            {
                infos.items.ForEach(element =>
                {
                    bool isSelect = element.ItemData.itemId == capeId;
                    element.UpdateSelected(isSelect);
                });
                infos.selectedId = bagId;
                _infos[AvatarSubType.Cape] = infos;
            }
        }


        if (!string.IsNullOrEmpty(crossId) && _infos.ContainsKey(AvatarSubType.Crossbody))
        {
            var infos = _infos[AvatarSubType.Crossbody];
            if (infos != null)
            {
                infos.items.ForEach(element =>
                {
                    bool isSelect = element.ItemData.itemId == crossId;
                    element.UpdateSelected(isSelect);
                });
                infos.selectedId = crossId;
                _infos[AvatarSubType.Crossbody] = infos;
            }
        }
    }

    public void RegisterListener(Action<AvatarSubType, RoleActionType, AvatarRoleItemProtocol> clickAction = null,
        Action<string> onColorChangeAction = null,
        Action<bool> showPaltteAction = null,
        Action<bool> showHsvAction = null, Action<AvatarMenuType, RoleColorAction, string> colorAction = null)
    {
        ClickAction = clickAction;
        this.onColorChangeAction = onColorChangeAction;
        // this.showPaltte = showPaltteAction;
        // this.showHsv = showHsvAction;
        // this.ClickColorAction = colorAction;
    }
    
    public void ShowListView(bool isShow)
    {
        bagView.gameObject.SetActive(isShow);
    }
    

}