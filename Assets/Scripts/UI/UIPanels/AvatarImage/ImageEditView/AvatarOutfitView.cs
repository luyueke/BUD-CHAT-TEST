using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Network;
using Network.Http;
using UI.BaseWidgets;
using Game.Avatar;
using GameData.PgcData;
using GameData.UGCData;

public class AvatarOutfitView : AvatarPGCRoleView
{
    [SerializeField] private AvatarUGCListEntry Entry;
    [SerializeField] private CText tipText;
    [SerializeField] private CButton openUIBtn;

    private Dictionary<AvatarSubType, AvatarTabItem> tabViews = new Dictionary<AvatarSubType, AvatarTabItem>();
    private Dictionary<AvatarSubType, GameObject> outfitViews = new Dictionary<AvatarSubType, GameObject>();

    private AvatarRoleItemView UGCItem;

    private string currentCookie = "";
    private bool isEnd = false;
    private bool isRequest = false;
    private bool isNewUser = false;
    private Transform bagTabView;
    private List<AvatarSubType> partType = new List<AvatarSubType>
    {
        AvatarSubType.Clothes, AvatarSubType.Clothes
    };

    protected override void Awake()
    {
        base.Awake();

        InitUGCUIIfNeed();
        InitTabView();

        currentPart = AvatarSubType.Clothes;
        onClickTab(currentPart);

        InitUGCOutfitList();
    }

    public override void SetDefaultTab(AvatarSubType part = AvatarSubType.Clothes)
    {
        base.SetDefaultTab(part);

        currentPart = part;
        onClickTab(currentPart);
    }

    private void InitUGCOutfitList()
    {
        Entry.adapter.partType = AvatarSubType.Clothes;
        Entry.pullAction = () =>
        {
            if (CanLoadMore())
            {
                LoadData(currentCookie);
            }
        };

        Entry.clickAction = (type, itemId) =>
        {
            OnClickItem(type, itemId);
        };
    }

    private void InitUGCUIIfNeed()
    {
        if (UGCItem == null)
        {
            openUIBtn.onClick.AddListener(OnOpenUIBtnClick);

            UGCItem = GameObjectEx.FindChildByName(transform, "UGCOutfitView/AvatarRoleItem").GetComponent<AvatarRoleItemView>();

            List<string> viewNames = new List<string>
            {
                "ListView", "UGCOutfitView"
            };

            for (int i = 0; i < partType.Count; i++)
            {
                var name = viewNames[i];
                var view = GameObjectEx.FindChildByName(transform, name).gameObject;
                view.SetActive(false);
                var avatarPartEnum = partType[i];
                outfitViews[avatarPartEnum] = view;
            }
        }

    }

    private void InitTabView()
    {
        bagTabView = GameObjectEx.FindChildByName(transform, "Tabar");
        var tabItem = GameObjectEx.FindChildByName(transform, "Tabar/ItemImp").GetComponent<AvatarTabItem>();
        List<string> bagTitles = new List<string>
        {
            "官方衣服", "社区衣服"
        };

        for (int i = 0; i < partType.Count; i++)
        {
            var data = bagTitles[i];
            var item = GameObject.Instantiate(tabItem, bagTabView);
            item.gameObject.SetActive(true);
            item.SetData(data, partType[i], onClickTab);
            tabViews[partType[i]] = item;
        }
        bagTabView.gameObject.SetActive(!isNewUser);
    }

    public void SetNewUser(bool isNew)
    {
        isNewUser = isNew;
    }

    private void onClickTab(AvatarSubType part)
    {
        currentPart = part;
        foreach (var avatarBagTabItem in tabViews)
        {
            bool isSelected = avatarBagTabItem.Key == part;
            avatarBagTabItem.Value.UpdateSelected(isSelected);
        }

        foreach (var element in outfitViews)
        {
            bool isSelected = element.Key == part;
            element.Value.SetActive(isSelected);
        }

        if (part == AvatarSubType.Clothes)
            RefreshData();

        UpdateHeight();
    }

    private void OnOpenUIBtnClick()
    {
    }

    protected override void OnClickItem(RoleActionType actionType, AvatarRoleItemProtocol itemData)
    {
        if (actionType == RoleActionType.Item)
        {
            currentSelectedId = itemData.itemId;

            var clothesInfo = Entry.adapter.OnSelect(itemData.itemId);
            roleItems.ForEach(element =>
            {
                bool isSelect = element.ItemData.itemId == itemData.itemId;
                element.UpdateSelected(isSelect);
            });
        }

        ClickAction?.Invoke(MenuType, currentPart, actionType, itemData);
    }

    public override void UpdateCollect(AvatarRoleItemProtocol itemData, bool isCollect)
    {
        if (itemData.isPGCItem)
        {
            base.UpdateCollect(itemData, isCollect);
        }
        else
        {
            Entry.adapter.UpdateCollect(itemData.itemId, isCollect);
        }

    }

    private void RefreshData()
    {
        currentCookie = "";
        LoadData(currentCookie, true);
    }

    private bool CanLoadMore()
    {
        if (isRequest)
        {
            return false;
        }
        return !isEnd;
    }

    private void LoadData(string cookie, bool forceRefresh = false)
    {
        // 调用后端接口
        var jb = new JObject
        {
            ["interactType"] = (int)UGCInteractType.BuySkin,
            ["cookie"] = cookie
        };

        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.UGCInteractList,
            HttpMethod.GET,
            JsonConvert.SerializeObject(jb),
            onReceive: arg0 =>
            {
                ResInfoList serverData = JsonConvert.DeserializeObject<ResInfoList>(arg0);
                isEnd = serverData.isEnd == 1;

                if (serverData.list == null || serverData.list.Count == 0)
                {
                    tipText.text = "点击下方查看更多社区衣服";
                    openUIBtn.gameObject.SetActive(true);

                    return;
                }

                currentCookie = serverData.cookie;
                var datas = serverData.list.Select(element => new AvatarRoleItemData(element.ugcInfo, element.interactInfo)).ToList();
                Entry.ReloadData(datas, forceRefresh);
                Entry.adapter.OnSelect(currentSelectedId);

                tipText.gameObject.SetActive(false);
                openUIBtn.gameObject.SetActive(false);

            }, onFail: arg0 =>
            {
                LoggerUtils.LogError("获取UGC购买衣服列表失败 : " + arg0);
            });
    }
}
