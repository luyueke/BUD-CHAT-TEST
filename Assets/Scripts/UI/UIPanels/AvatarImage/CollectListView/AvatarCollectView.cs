using Network.Http;
using Network;
using UI.BaseWidgets;
using UnityEngine;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Game.Avatar;
using System.Linq;
using GameData.PgcData;
using GameData.UGCData;

public class AvatarCollectView : IAvatarRoleItem
{
    [SerializeField] private AvatarCollectListEntry Entry;
    [SerializeField] private CText tipText;
    [SerializeField] private GameObject views;

    private AvatarRoleItemProtocol curSelectedItem;

    private string currentCookie = "";
    private bool isEnd = false;
    private bool isRequest = false;

    protected override void Awake()
    {
        InitUIIfNeed();

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

        UpdateHeight();
    }

    private void OnDisable()
    {
        Entry.adapter.HideSelected();
    }

    private void Start()
    {
        RefreshData();
    }

    public void RefreshData()
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
            ["interactType"] = (int)UGCInteractType.CollectSkin,
            ["cookie"] = cookie
        };

        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.UGCInteractList,
            HttpMethod.GET,
            JsonConvert.SerializeObject(jb),
            onReceive: arg0 =>
            {
                ResInfoList serverData = JsonConvert.DeserializeObject<ResInfoList>(arg0);
                isEnd = serverData.isEnd == 1;

                if (serverData.list != null)
                {
                    currentCookie = serverData.cookie;

                    var datas = serverData.list.Select((element) =>
                    {
                        if (element.isPgc == 1)
                        {
                            AvatarSubType part = (AvatarSubType)Es.DataTables.GetGameResData(element.pgcInfo.id).SubType;
                            var data = Es.DataTables.GetAvatarCommonData(element.pgcInfo.id);
                            if (data != null)
                            {
                                var itemData = new AvatarRoleItemData(data) as AvatarRoleItemProtocol;
                                itemData.isCollected = true;
                                return itemData;
                            }
                            else
                                return null;
                        }
                        else
                        {
                            return new AvatarRoleItemData(element.ugcInfo, element.interactInfo) as AvatarRoleItemProtocol;
                        }
                    }).Where(tmp => tmp != null).ToList();

                    tipText.gameObject.SetActive(false);
                    views.SetActive(true);

                    Entry.ReloadData(datas, forceRefresh);
                }
                else
                {
                    tipText.text = "无收藏";
                }

            }, onFail: arg0 =>
            {
                LoggerUtils.LogError("获取收藏列表失败 : " + arg0);
            });
    }
    protected override void InitUIIfNeed()
    {
        ColorBar = GameObjectEx.FindChildByName(transform, "ColorBar");
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
            curSelectedItem = itemData;

            Entry.adapter.OnSelect(curSelectedItem);
        }

        AvatarSubType part;
        if (itemData.isPGCItem)
            part = (AvatarSubType)Es.DataTables.GetGameResData(itemData.itemId).SubType;
        else
            part = (AvatarSubType)Es.DataTables.GetGameResData(itemData.templateId).SubType;

        var menuType = AvatarConfigTool.MenuType(part);

        ClickAction?.Invoke(AvatarMenuType.Collect, part, actionType, itemData);

        if (actionType == RoleActionType.Adjust)
        {
            ColorViewUtils.ReloadColorList(menuType, this);
        }
    }

    public override void UpdateCollect(AvatarRoleItemProtocol itemData, bool isCollect)
    {
        if (!Entry.HasData) return;

        if (isCollect)
        {
            Entry.AddData(itemData);
        }
        else
        {
            Entry.RemoveData(itemData);
        }

        Entry.adapter.Refresh();

        if (Entry.adapter.Data.Count == 0)
        {
            tipText.text = "无收藏";
            tipText.gameObject.SetActive(true);
        }
        else
        {
            tipText.gameObject.SetActive(false);
        }
    }
}
