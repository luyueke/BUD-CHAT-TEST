using Game.BagSystem;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using static ReddotManagerUtils;

public class RedDot : MonoBehaviour, IRedDot
{
    [SerializeField] private List<ReddotType> customType;
    [SerializeField] private List<LocalRedDotData> redDotDataList;
    private EventTrigger eventTrigger;
    private Action<IRedDot> OnRelease;

    public int InstanceID => gameObject.GetInstanceID();

    void Awake() 
    {
        if (customType != null && customType.Count > 0)
        {
            MessageHelper.AddListener(MessageName.ReddotNotice, RefreshState);
        }
    }

    void OnEnable()
    {
        RefreshState();
    }

    void OnDestroy()
    {
        if (customType != null && customType.Count > 0)
        {
            MessageHelper.RemoveListener(MessageName.ReddotNotice, RefreshState);
        }
        OnRelease?.Invoke(this);
    }

    void RefreshState() {
        foreach (var item in customType)
        {
            var count = ReddotManagerUtils.Inst.GetRedDotCount(item);
            if (count > 0)
            {
                gameObject.SetActive(true);
                return;
            }
        }
        gameObject.SetActive(false);
    }

    public void AddClickAction()
    {
        UnityAction<BaseEventData> click = new UnityAction<BaseEventData>(OnPointerClick);
        EventTrigger.Entry myclick = new EventTrigger.Entry();
        myclick.eventID = EventTriggerType.PointerClick;
        myclick.callback.AddListener(click);

        eventTrigger = transform.parent.gameObject.AddComponent<EventTrigger>();
        eventTrigger.triggers.Add(myclick);
    }

    public void ClearRedDot()
    {
        Destroy(eventTrigger);
        Destroy(gameObject);
    }

    public void SetData(LocalRedDotData data, Action<IRedDot> onRelease)
    {
        redDotDataList = new List<LocalRedDotData>();

        redDotDataList.Add(data);
        this.OnRelease = onRelease;
    }

    public void SetRelease(Action<IRedDot> onRelease)
    {
        this.OnRelease = onRelease;
    }

    public void AddData(LocalRedDotData data)
    {
        if (!redDotDataList.Contains(data))
        {
            redDotDataList.Add(data);
        }
    }

    private void OnPointerClick(BaseEventData data)
    {
        return;
        if (redDotDataList == null || redDotDataList.Count == 0) return;

        var redDotData = redDotDataList[0];

        var pgcRedDot = new PGCRedDotData();
        var ugcRedDot = new UGCRedDotData();
        if (redDotData.isPGC)
        {
            pgcRedDot.idList = new List<string>() { redDotData.id };
        }
        else
        {
            ugcRedDot = new UGCRedDotData()
            {
                list = new List<LocalRedDotData>()
                {
                    new LocalRedDotData()
                    {
                        id = redDotData.id,
                        type = redDotData.type,
                        redDotNum = redDotData.redDotNum,
                    }
                }
            };
        }

        var req = new RedDotData()
        {
            pgcReddot = pgcRedDot,
            ugcReddot = ugcRedDot
        };

        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.delRedDot, HttpMethod.POST, JsonConvert.SerializeObject(req), response =>
        {
            RemoveRedDotComplete(redDotData);
        }, fail =>
        {
            LoggerUtils.LogError($"触发红点失败 : {fail}");
        });
    }

    private void RemoveRedDotComplete(LocalRedDotData redDotData)
    {
        if (redDotData.isPGC)
            UserBagSystem.Inst.RemoveDataById<BaseUserBagData>(redDotData.id);
        else
            UserBagSystem.Inst.RemoveUGCDataById<BaseUserBagData>(redDotData.id, redDotData.type);

        RedDotManager.Inst.TriggerRedDot(redDotData.type, redDotData.id);
    }

    public bool RemoveRedDotData(LocalRedDotData data)
    {
        var redDotData = redDotDataList.Find(redDotData => redDotData.type == data.type && redDotData.id == data.id);

        if (redDotData == null) return false;

        redDotDataList.Remove(redDotData);
        if (redDotDataList.Count == 0)
        {
            ClearRedDot();
            return true;
        }

        return false;
    }
}
