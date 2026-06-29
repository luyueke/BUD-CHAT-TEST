using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ChatScrollRect : ScrollRect
{
    public class ChatItemProxy
    {
        public float y;
        public float height;
        public OfflineMessageItem data;
        public GameHallChatProxyItem target;
        public bool needUpdateHeight = false;

        public void AddChatContent(string value) {

            if (target != null) {
                target.AddChat(value);
                var rt = target.GetComponent<RectTransform>();
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
                height = rt.sizeDelta.y;
            } else {
                needUpdateHeight = true;
            }

            if (data != null) {
                data.data += value;
            }

        }
    }

    public GameHallChatProxyItem rt_prefab;

    // public RectTransform rt_top;
    public RectTransform rt_pool;

    private float topPadding = 40;
    private float bottonPadding = 35;
    private float contentHeight = 40;
    private List<GameHallChatProxyItem> allChatItems = new List<GameHallChatProxyItem>();

    // private bool triggerTop = false;

    Vector2 anchorMin = new Vector2(0, 1);
    Vector2 anchorMax = new Vector2(1, 1);
    Vector2 pivot = new Vector2(0.5f, 1);
    private bool isAtTop = false;

    private List<ChatItemProxy> proxyDatas = new List<ChatItemProxy>();
    private Queue<ChatItemProxy> dataPool = new Queue<ChatItemProxy>();
    private Action onScrollTopCallBack;


    protected override void Awake()
    {
        base.Awake();
        onValueChanged.AddListener(OnValueChange);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }

    private void AddToPool(ChatItemProxy info)
    {
        if (info.target)
        {
            info.target.transform.SetParent(rt_pool, false);
        }

        info.target = null;
    }

    private GameHallChatProxyItem GetFromPool()
    {
        if (rt_pool.childCount == 0)
        {
            var go = GameObject.Instantiate(rt_prefab);
            allChatItems.Add(go);
            return go;
        }
        else
        {
            return rt_pool.GetChild(0).GetComponent<GameHallChatProxyItem>();
        }
    }

    public void SetScrollTopCallBack(Action callBack) {
        onScrollTopCallBack = callBack;
    }

    private void OnValueChange(Vector2 vec)
    {
        if (verticalNormalizedPosition > 0.99f) {

            if (!isAtTop) {
                onScrollTopCallBack?.Invoke();
                isAtTop = true;
            }
        } else {
            if (isAtTop) {
                isAtTop = false;
            }
        }

        for (int i = 0; i < proxyDatas.Count; i++)
        {
            ChatItemProxy info = proxyDatas[i];
            if (info.target != null)
            {
                //检测是否不在区域了
                if (!IsInViewport(info))
                {
                    AddToPool(info);
                }
            }
            else
            {
                //检测是否在区域
                if (IsInViewport(info))
                {
                    AddToViewport(info);
                }
            }
        }
    }

    public override void OnDrag(PointerEventData eventData)
    {
        base.OnDrag(eventData);
        // if (triggerTop)
        // {
        //     if (content.anchoredPosition.y >= -rt_top.sizeDelta.y)
        //     {
        //         triggerTop = false;
        //         rt_top.gameObject.SetActive(false);
        //     }
        // }
        // else
        // {
        //     if (content.anchoredPosition.y < -rt_top.sizeDelta.y)
        //     {
        //         triggerTop = true;
        //         rt_top.gameObject.SetActive(true);
        //     }
        // }
    }

    private bool isLock = false;

    public override void OnEndDrag(PointerEventData eventData)
    {
        base.OnEndDrag(eventData);
    }

    /// <summary>
    /// 是否在视图中
    /// </summary>
    /// <param name="info"></param>
    /// <param name="offset">距离底部偏移，避免AI 。。。不弹出</param>
    /// <returns></returns>
    private bool IsInViewport(ChatItemProxy info, float offset = 0)
    {
        float topY = -content.anchoredPosition.y;
        float bottomY = -content.anchoredPosition.y - viewport.rect.height;

        if (info.y - info.height <= topY && info.y >= bottomY - offset)
            return true;
        return false;
    }

    public bool IsOverView()
    {
        return contentHeight > viewport.rect.height;
    }

    private GameHallChatProxyItem AddToViewport(ChatItemProxy info)
    {
        var go = GetFromPool();
        go.transform.SetParent(content, false);
        info.target = go;
        UpdateToViewport(go, info);
        return go;
    }

    private void UpdateToViewport(GameHallChatProxyItem go, ChatItemProxy info)
    {
        go.SetData(info.data);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        var oMin = rt.offsetMin;
        oMin.x = 0;
        rt.offsetMin = oMin;
        var oMax = rt.offsetMax;
        oMax.x = 0;
        rt.offsetMax = oMax;


        var pos = rt.anchoredPosition;
        pos.y = info.y;
        rt.anchoredPosition = pos;

        if (info.needUpdateHeight) {
            var offsetY = rt.sizeDelta.y - info.height;
            info.height = rt.sizeDelta.y;
            contentHeight += offsetY;
            info.needUpdateHeight = false;
            float realContentHeight = contentHeight + bottonPadding;
            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, realContentHeight);
        }
    }


    private ChatItemProxy GetDataFromPool()
    {
        if (dataPool.Count == 0)
            return new ChatItemProxy();
        else
        {
            return dataPool.Dequeue();
        }
    }

    private ChatItemProxy InsertData(OfflineMessageItem data) {
        rt_prefab.SetData(data);
        var rt = rt_prefab.GetComponent<RectTransform>();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        ChatItemProxy info = GetDataFromPool();
        info.data = data;
        info.y = -topPadding;
        info.height = rt.sizeDelta.y;

        proxyDatas.Insert(0, info);
        contentHeight += info.height;
        float realContentHeight = contentHeight + bottonPadding;
        content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, realContentHeight);
        foreach (var proxyData in proxyDatas) {
            if (proxyData == info) {
                continue;
            }
            proxyData.y -= info.height;
            if (proxyData.target != null) {
                UpdateToViewport(proxyData.target, proxyData);
            }
        }
        return info;
    }

    private ChatItemProxy AddData(OfflineMessageItem data)
    {
        rt_prefab.SetData(data);
        var rt = rt_prefab.GetComponent<RectTransform>();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        ChatItemProxy info = GetDataFromPool();
        info.data = data;
        info.y = proxyDatas.Count > 0 ? proxyDatas[^1].y - proxyDatas[^1].height : -topPadding;
        info.height = rt.sizeDelta.y;
        proxyDatas.Add(info);

        contentHeight += info.height;
        float realContentHeight = contentHeight + bottonPadding;
        content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, realContentHeight);

        return info;
    }

    public ChatItemProxy AddMsg(OfflineMessageItem msg)
    {
        ChatItemProxy lastInfo = proxyDatas.Count == 0 ? null : proxyDatas[^1];
        ChatItemProxy info = AddData(msg);
        if (lastInfo == null)
        {
            AddToViewport(info);
        }
        else
        {
            float bottomY = -content.anchoredPosition.y - viewport.rect.height;

            if (lastInfo.y >= bottomY || lastInfo.y - lastInfo.height >= bottomY)
            {
                AddToViewport(info);
            }
        }
        return info;
    }

    /// <summary>
    /// 头部插入数据
    /// </summary>
    /// <param name="msg"></param>
    /// <returns></returns>
    public ChatItemProxy InsertMsg(OfflineMessageItem msg) {
        ChatItemProxy info = InsertData(msg);
        OnValueChange(Vector2.zero);
        return info;
    }


    public void AddChatContent(ChatItemProxy itemProxy, string value) {
        if (itemProxy == null) {
            return;
        }
        var lastHeight = itemProxy.height;
        itemProxy.AddChatContent(value);
        if (!itemProxy.needUpdateHeight) {
            contentHeight += (itemProxy.height - lastHeight);
            float realContentHeight = contentHeight + bottonPadding;
            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, realContentHeight);
        }
    }


    public void AddMsgOnInit(List<OfflineMessageItem> contents)
    {
        contentHeight = 40;
        proxyDatas.Clear();
        dataPool.Clear();
        for (int i = 0; i < contents.Count; i++)
        {
            ChatItemProxy lastInfo = proxyDatas.Count == 0 ? null : proxyDatas[^1];
            ChatItemProxy info = AddData(contents[i]);
            if (lastInfo == null)
            {
                AddToViewport(info);
            }
            else
            {
                float bottomY = -content.anchoredPosition.y - viewport.rect.height;

                if (lastInfo.y >= bottomY || lastInfo.y - lastInfo.height >= bottomY)
                {
                    AddToViewport(info);
                }
            }
        }
    }

    private Coroutine cor_move;
    private WaitForEndOfFrame wait = new WaitForEndOfFrame();

    public void ProcMovement()
    {
        if (cor_move != null)
            StopCoroutine(cor_move);

        this.DOKill();
        cor_move = StartCoroutine(DelayMove());
    }

    private IEnumerator DelayMove()
    {
        yield return wait;
        this.DOVerticalNormalizedPos(0, 0.3f).SetEase(Ease.InOutSine);
    }
}
