using System;
using System.Collections.Generic;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.BaseWidgets;
using UnityEngine;

public enum OcActionType
{
    Click = 0,
    Delete,
}

public class AvatarOcRoleView : MonoBehaviour
{
    [SerializeField] private CText tipText;
    public AvatarMenuType MenuType
    {
        get { return currentMenuType; }
    }

    private AvatarMenuType currentMenuType = AvatarMenuType.PrefabImage;
    public AvatarOcRoleEntry Entry;
    private Action<OcActionType, AvatarOcData> ClickAction;

    private string currentCookie = "";
    private bool isEnd = false;
    private bool isRequest = false;

    private void Awake()
    {
        Entry.pullAction = () =>
        {
            if (CanLoadMore())
            {
                LoadData(currentCookie);
            }
        };

        Entry.clickAction = data =>
        {
            ClickAction?.Invoke(OcActionType.Click, data);

            Entry.adapter.OnSelect(data);
        };

        Entry.longClickAction = data =>
        {
            ClickAction?.Invoke(OcActionType.Delete, data);
        };
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
            ["cookie"] = cookie
        };

        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ocList,
            HttpMethod.GET, 
            JsonConvert.SerializeObject(jb),
            onReceive: arg0 =>
            {
                AvatarOcListRes serverData = JsonConvert.DeserializeObject<AvatarOcListRes>(arg0);
                currentCookie = serverData.cookie;
                isEnd = serverData.isEnd == 1;
                if (serverData.list!=null&&serverData.list.Count>=serverData.limitedSlotCount&&serverData.limitedSlotCount>0)
                {
                    for (int i = 0; i < serverData.limitedSlotCount; i++)
                    {
                        serverData.list[i].isLimitedSlot=true;
                    }
                }
                Entry.ReloadData(serverData.list, forceRefresh);
                if (Entry.HasData)
                    tipText.gameObject.SetActive(false);
                else
                    tipText.text = "快去保存属于你的设子吧！";
            }, onFail: arg0 =>
            {
                Entry.ReloadData(new List<AvatarOcData>(), false);
            });
    }

    private void OnRecivedData(List<AvatarOcInfo> items)
    {
        
    }
    
    public void RegisterListener(Action<OcActionType, AvatarOcData> clickAction = null)
    {
        ClickAction = clickAction;
    }
    
}

