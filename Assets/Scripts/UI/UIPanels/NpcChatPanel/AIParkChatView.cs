using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Game.Props.PropsManagers.AIGames.AIPark.FSM;
using Message;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Random = UnityEngine.Random;

public class AIParkChatView : MonoBehaviour
{
    public RectTransform Content;
    public AIParkChatItem otherItemPrefab;
    public AIParkChatItem selfItemPrefab;
    private AIParkChatItem currentItem;
    public Button hideBtn;
    public Text unReadCountText;

    public GameObject toNewestChatMsgGo;
    public Button toNewestChatMsgBtn;

    public Transform chatContent;

    public ScrollRect scrollRect;
    GameObject banUINode;
    // 对象池相关
    private Queue<AIParkChatItem> _otherItemPool = new Queue<AIParkChatItem>();
    private Queue<AIParkChatItem> _selfItemPool = new Queue<AIParkChatItem>();
    private List<AIParkChatItem> _activeItems = new List<AIParkChatItem>();
    private const int INITIAL_POOL_SIZE = 10;
    private const int MAX_POOL_SIZE = 50;


    // 滚动检测相关
    [Header("滚动检测设置")]
    [SerializeField] private bool usePreciseDetection = false; // 是否使用精确检测
    [SerializeField] private float bottomThreshold = 0.1f; // 底部阈值

    private readonly string[] UserNameColor = new[]
    {
        "<c=B5ABFF>", "<c=FF8989>", "<c=00ADFF>", "<c=02E880>"
    };

    int _showPosx = 0;
    int _hidePosx = 0;
    Tween _chatTween;

    public static bool isShowChatView = false;
    void Awake()
    {
        hideBtn.onClick.AddListener(OnHideBtnClick);
        toNewestChatMsgBtn.onClick.AddListener(OnToNewestChatMsgBtnClick);
        _showPosx = (int)transform.localPosition.x + 1108;
        _hidePosx = (int)transform.localPosition.x;

        // 添加拖拽事件监听器
        var scrollRectEventTrigger = scrollRect.gameObject.GetComponent<EventTrigger>();
        if (scrollRectEventTrigger == null)
        {
            scrollRectEventTrigger = scrollRect.gameObject.AddComponent<EventTrigger>();
        }

        // 添加开始拖拽事件
        var beginDragEntry = new EventTrigger.Entry();
        beginDragEntry.eventID = EventTriggerType.BeginDrag;
        beginDragEntry.callback.AddListener((data) => OnScrollRectBeginDrag((PointerEventData)data));
        scrollRectEventTrigger.triggers.Add(beginDragEntry);

        // 添加拖拽中事件
        var dragEntry = new EventTrigger.Entry();
        dragEntry.eventID = EventTriggerType.Drag;
        dragEntry.callback.AddListener((data) => OnScrollRectDrag((PointerEventData)data));
        scrollRectEventTrigger.triggers.Add(dragEntry);

        // 添加结束拖拽事件
        var endDragEntry = new EventTrigger.Entry();
        endDragEntry.eventID = EventTriggerType.EndDrag;
        endDragEntry.callback.AddListener((data) => OnScrollRectEndDrag((PointerEventData)data));
        scrollRectEventTrigger.triggers.Add(endDragEntry);

        toNewestChatMsgGo.SetActive(false);
        // 初始化对象池
        InitializeObjectPools();

        RefreshChatView();
    }

    public void SetBanUINode(GameObject banUINode)
    {
        this.banUINode = banUINode;
        banUINode.SetActive(false);
    }

    void OnHideBtnClick()
    {
        HideChatView();
    }

    public void QuickHideView()
    {
        if (!isShowChatView)
        {
            return;
        }
        if (_chatTween != null)
        {
            _chatTween.Kill();
        }
        banUINode.SetActive(false);
        MessageHelper.RemoveListener<AIParkChatInfo>(MessageName.S11ParkReceiveNewChat, OnReceiveNewChat);
        transform.localPosition = new Vector3(_hidePosx, transform.localPosition.y, transform.localPosition.z);
        AIParkChatMgr.Inst.SetLockUnReadState(false);
        isShowChatView = false;
    }

    public void ShowChatView()
    {
        if (isShowChatView)
        {
            return;
        }
        if (_chatTween != null)
        {
            _chatTween.Kill();
        }
        banUINode.SetActive(true);
        AIParkChatMgr.Inst.SetLockUnReadState(true);
        RefreshChatView();
        _chatTween = transform.DOLocalMoveX(_showPosx, 0.3f).SetEase(Ease.Linear);
        _chatTween.onComplete = () =>
        {
            MessageHelper.AddListener<AIParkChatInfo>(MessageName.S11ParkReceiveNewChat, OnReceiveNewChat);
        };
        isShowChatView = true;
    }

    public void HideChatView()
    {
        if (!isShowChatView)
        {
            return;
        }
        if (_chatTween != null)
        {
            _chatTween.Kill();
        }
        banUINode.SetActive(false);
        MessageHelper.RemoveListener<AIParkChatInfo>(MessageName.S11ParkReceiveNewChat, OnReceiveNewChat);
        _chatTween = transform.DOLocalMoveX(_hidePosx, 0.3f).SetEase(Ease.Linear);
        AIParkChatMgr.Inst.SetLockUnReadState(false);
        isShowChatView = false;
    }

    void OnReceiveNewChat(AIParkChatInfo chatInfo)
    {
        AddNewMessage(chatInfo);
    }


    void OnScrollRectValueChanged(Vector2 value)
    {
        // 检查滚动方向，value.y 从 1 到 0 表示从上往下滚动
        // 当用户向下滚动时，检查进入视图的聊天项
        if (usePreciseDetection)
        {
            CheckVisibleChatItemsPrecise();
        }
        else
        {
            CheckVisibleChatItemsSimple();
        }
    }

    /// <summary>
    /// 开始拖拽时调用
    /// </summary>
    void OnScrollRectBeginDrag(PointerEventData eventData)
    {
        Debug.Log("开始拖拽聊天界面");
        // 可以在这里添加拖拽开始时的逻辑
        // 比如暂停自动滚动、显示拖拽指示器等
    }

    /// <summary>
    /// 拖拽过程中调用
    /// </summary>
    void OnScrollRectDrag(PointerEventData eventData)
    {
        // 可以在这里添加拖拽过程中的逻辑
        // 比如实时更新拖拽指示器、检测拖拽方向等
        // Debug.Log($"拖拽中: {eventData.delta}");
        if (usePreciseDetection)
        {
            CheckVisibleChatItemsPrecise();
        }
        else
        {
            CheckVisibleChatItemsSimple();
        }
    }

    /// <summary>
    /// 结束拖拽时调用
    /// </summary>
    void OnScrollRectEndDrag(PointerEventData eventData)
    {
        Debug.Log("结束拖拽聊天界面");
        // 可以在这里添加拖拽结束时的逻辑
        // 比如恢复自动滚动、隐藏拖拽指示器等

        // 检查是否需要标记消息为已读

    }

    /// <summary>
    /// 检查当前可见的聊天项并更新未读状态
    /// </summary>
    private void CheckVisibleChatItems()
    {
        if (scrollRect == null || chatContent == null) return;

        // 获取 ScrollRect 的视口矩形
        RectTransform viewport = scrollRect.viewport;
        if (viewport == null) return;

        // 计算视口的世界坐标边界
        Vector3[] viewportCorners = new Vector3[4];
        viewport.GetWorldCorners(viewportCorners);

        // 获取视口的边界
        float viewportTop = viewportCorners[1].y;    // 顶部
        float viewportBottom = viewportCorners[0].y;  // 底部

        // 遍历所有活跃的聊天项
        for (int i = 0; i < _activeItems.Count; i++)
        {
            var chatItem = _activeItems[i];
            if (chatItem == null || !chatItem.gameObject.activeInHierarchy) continue;

            // 获取聊天项的世界坐标边界
            RectTransform itemRect = chatItem.GetComponent<RectTransform>();
            if (itemRect == null) continue;

            Vector3[] itemCorners = new Vector3[4];
            itemRect.GetWorldCorners(itemCorners);

            // 检查聊天项是否在视口内
            bool isVisible = IsRectVisibleInViewport(itemCorners, viewportTop, viewportBottom);

            if (isVisible)
            {
                // 找到对应的聊天信息并标记为已读
                MarkChatAsRead(i);
            }
        }

        // 刷新未读数量显示
        RefreshTipBtn();
    }

    /// <summary>
    /// 简化的可见性检测方法
    /// </summary>
    private void CheckVisibleChatItemsSimple()
    {
        if (scrollRect == null || chatContent == null) return;

        // 获取当前滚动位置
        float scrollValue = scrollRect.verticalNormalizedPosition;

        // 当滚动到底部附近时，标记所有聊天为已读
        if (scrollValue <= bottomThreshold) // 接近底部
        {
            var chatList = AIParkChatMgr.Inst.GetChatList();
            bool hasUnread = false;

            for (int i = 0; i < chatList.Count; i++)
            {
                if (!chatList[i].isRead)
                {
                    chatList[i].isRead = true;
                    hasUnread = true;
                }
            }

            if (hasUnread)
            {
                Debug.Log("滚动到底部，标记所有聊天为已读");
                RefreshTipBtn();
            }
        }
    }

    /// <summary>
    /// 精确检测聊天项是否进入视图
    /// </summary>
    private void CheckVisibleChatItemsPrecise()
    {
        if (scrollRect == null || chatContent == null) return;

        // 获取 ScrollRect 的视口
        RectTransform viewport = scrollRect.viewport;
        if (viewport == null) return;

        // 获取视口的世界坐标边界
        Vector3[] viewportCorners = new Vector3[4];
        viewport.GetWorldCorners(viewportCorners);

        // 计算视口的边界
        float viewportTop = viewportCorners[1].y;    // 顶部
        float viewportBottom = viewportCorners[0].y;  // 底部

        // 遍历所有活跃的聊天项
        for (int i = 0; i < _activeItems.Count; i++)
        {
            var chatItem = _activeItems[i];
            if (chatItem == null || !chatItem.gameObject.activeInHierarchy) continue;
            if (CheckChatIsRead(i))
            {
                continue;
            }

            // 获取聊天项的世界坐标边界
            RectTransform itemRect = chatItem.GetComponent<RectTransform>();
            if (itemRect == null) continue;

            Vector3[] itemCorners = new Vector3[4];
            itemRect.GetWorldCorners(itemCorners);

            // 检查聊天项是否在视口内
            bool isVisible = IsRectVisibleInViewport(itemCorners, viewportTop, viewportBottom);

            if (isVisible)
            {
                // 找到对应的聊天信息并标记为已读
                MarkChatAsRead(i);
            }
        }

        // 刷新未读数量显示
        RefreshTipBtn();
    }

    /// <summary>
    /// 使用屏幕坐标检测聊天项是否可见（更准确的方法）
    /// </summary>
    private bool IsChatItemVisibleOnScreen(AIParkChatItem chatItem)
    {
        if (chatItem == null) return false;

        RectTransform itemRect = chatItem.GetComponent<RectTransform>();
        if (itemRect == null) return false;

        // 获取聊天项在屏幕上的位置
        Vector3[] corners = new Vector3[4];
        itemRect.GetWorldCorners(corners);

        // 转换为屏幕坐标
        Vector3 screenPos1 = Camera.main.WorldToScreenPoint(corners[0]); // 左下
        Vector3 screenPos2 = Camera.main.WorldToScreenPoint(corners[2]); // 右上

        // 检查是否在屏幕范围内
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        return screenPos1.x < screenWidth && screenPos2.x > 0 &&
               screenPos1.y < screenHeight && screenPos2.y > 0;
    }

    /// <summary>
    /// 检查矩形是否在视口内可见
    /// </summary>
    private bool IsRectVisibleInViewport(Vector3[] itemCorners, float viewportTop, float viewportBottom)
    {
        float itemTop = itemCorners[1].y;     // 聊天项顶部
        float itemBottom = itemCorners[0].y;   // 聊天项底部

        float checkTop = (itemTop - itemBottom) * 0.3f + itemBottom;

        // 检查是否有重叠 - 聊天项至少有一部分在视口内
        return itemBottom < viewportTop && checkTop > viewportBottom;
    }

    /// <summary>
    /// 标记指定索引的聊天为已读
    /// </summary>
    private void MarkChatAsRead(int itemIndex)
    {
        var chatList = AIParkChatMgr.Inst.GetChatList();
        if (itemIndex >= 0 && itemIndex < chatList.Count)
        {
            var chatInfo = chatList[itemIndex];
            if (!chatInfo.isRead)
            {
                chatInfo.isRead = true;
                Debug.Log($"标记聊天为已读: {chatInfo.content}");
            }
        }
    }

    bool CheckChatIsRead(int itemIndex)
    {
        var chatList = AIParkChatMgr.Inst.GetChatList();
        if (itemIndex >= 0 && itemIndex < chatList.Count)
        {
            return chatList[itemIndex].isRead;
        }
        return true;
    }

    /// <summary>
    /// 测试对象池功能（可在Inspector中调用）
    /// </summary>
    [ContextMenu("Test Object Pool")]
    public void TestObjectPool()
    {
        Debug.Log("=== 对象池测试 ===");
        Debug.Log($"初始状态: {GetPoolStatistics()}");

        // 添加一些测试消息
        for (int i = 0; i < 5; i++)
        {
            AddNewMessage(new AIParkChatInfo() { npcId = ((int)ParkNpcRoleType.self).ToString(), content = $"测试自己消息 {i}" });
            AddNewMessage(new AIParkChatInfo() { npcId = "other", content = $"测试其他消息 {i}" });
        }

        Debug.Log($"添加消息后: {GetPoolStatistics()}");

        // 清空消息
        ClearAllMessage();

        Debug.Log($"清空后: {GetPoolStatistics()}");
    }

    /// <summary>
    /// 测试滚动检测功能（可在Inspector中调用）
    /// </summary>
    [ContextMenu("Test Scroll Detection")]
    public void TestScrollDetection()
    {
        Debug.Log("=== 滚动检测测试 ===");
        Debug.Log($"当前滚动位置: {scrollRect?.verticalNormalizedPosition}");
        Debug.Log($"使用精确检测: {usePreciseDetection}");
        Debug.Log($"底部阈值: {bottomThreshold}");
        Debug.Log($"未读消息数量: {AIParkChatMgr.Inst.GetUnReadCount()}");

        // 手动触发检测
        if (usePreciseDetection)
        {
            CheckVisibleChatItemsPrecise();
        }
        else
        {
            CheckVisibleChatItemsSimple();
        }
    }

    /// <summary>
    /// 初始化对象池
    /// </summary>
    private void InitializeObjectPools()
    {
        // 预创建一些对象放入池中
        for (int i = 0; i < INITIAL_POOL_SIZE; i++)
        {
            var otherItem = GameObject.Instantiate(otherItemPrefab, chatContent);
            otherItem.gameObject.SetActive(false);
            _otherItemPool.Enqueue(otherItem);

            var selfItem = GameObject.Instantiate(selfItemPrefab, chatContent);
            selfItem.gameObject.SetActive(false);
            _selfItemPool.Enqueue(selfItem);
        }
    }

    /// <summary>
    /// 从对象池获取聊天项
    /// </summary>
    private AIParkChatItem GetItemFromPool(bool isSelf)
    {
        Queue<AIParkChatItem> pool = isSelf ? _selfItemPool : _otherItemPool;
        AIParkChatItem item;

        if (pool.Count > 0)
        {
            item = pool.Dequeue();
        }
        else
        {
            // 池中没有可用对象，创建新的
            var prefab = isSelf ? selfItemPrefab : otherItemPrefab;
            item = GameObject.Instantiate(prefab, chatContent);
        }
        item.transform.SetAsLastSibling();

        item.gameObject.SetActive(true);
        _activeItems.Add(item);
        return item;
    }

    /// <summary>
    /// 将聊天项归还到对象池
    /// </summary>
    private void ReturnItemToPool(AIParkChatItem item, bool isSelf)
    {
        if (item == null) return;

        item.gameObject.SetActive(false);
        _activeItems.Remove(item);

        Queue<AIParkChatItem> pool = isSelf ? _selfItemPool : _otherItemPool;
        if (pool.Count < MAX_POOL_SIZE)
        {
            pool.Enqueue(item);
        }
        else
        {
            // 池已满，销毁对象
            Destroy(item.gameObject);
        }
    }

    /// <summary>
    /// 清空所有活跃的聊天项
    /// </summary>
    private void ClearAllActiveItems()
    {
        // 创建临时列表来避免在遍历时修改集合
        var itemsToRemove = new List<AIParkChatItem>(_activeItems);

        foreach (var item in itemsToRemove)
        {
            if (item != null)
            {
                bool isSelf = item is AIParkChatSelfItem;
                ReturnItemToPool(item, isSelf);
            }
        }
        _activeItems.Clear();
    }

    void RefreshChatView()
    {
        // 先清空现有的聊天项
        ClearAllActiveItems();

        var chatList = AIParkChatMgr.Inst.GetChatList();
        if(chatList == null){
            return;
        }
        foreach (var chat in chatList)
        {
            bool isSelf = chat.npcId == ((int)ParkNpcRoleType.self).ToString();
            var item = GetItemFromPool(isSelf);
            item.SetData(chat);
        }
        Content.GetComponent<ContentSizeFitter>().SetLayoutVertical();
        // 刷新后滚动到底部
        StartCoroutine(ScrollToBottomAfterLayout());
    }

    /// <summary>
    /// 在布局更新后滚动到底部
    /// </summary>
    private IEnumerator ScrollToBottomAfterLayout()
    {
        AIParkChatMgr.Inst.SetAllChatRead();
        RefreshTipBtn();
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    /// <summary>
    /// 添加新消息到聊天界面
    /// </summary>
    public void AddNewMessage(AIParkChatInfo chatInfo)
    {
        bool isSelf = chatInfo.npcId == ((int)ParkNpcRoleType.self).ToString();
        var item = GetItemFromPool(isSelf);
        item.SetData(chatInfo);

        RefreshTipBtn();
        // 添加新消息后滚动到底部
        // StartCoroutine(ScrollToBottomAfterLayout());
    }

    public void RefreshTipBtn()
    {
        int unReadCount = AIParkChatMgr.Inst.GetUnReadCount();
        if (unReadCount > 0)
        {
            toNewestChatMsgGo.SetActive(true);
        }
        else
        {
            toNewestChatMsgGo.SetActive(false);
        }
        unReadCountText.text = string.Format("{0}条未读信息", unReadCount);
    }

    /// <summary>
    /// 获取对象池统计信息（用于调试）
    /// </summary>
    public string GetPoolStatistics()
    {
        return $"其他消息池: {_otherItemPool.Count}, 自己消息池: {_selfItemPool.Count}, 活跃项: {_activeItems.Count}";
    }

    /// <summary>
    /// 优化对象池，清理过多的对象
    /// </summary>
    public void OptimizeObjectPools()
    {
        // 如果池中对象过多，清理一些
        while (_otherItemPool.Count > MAX_POOL_SIZE / 2)
        {
            var item = _otherItemPool.Dequeue();
            if (item != null)
            {
                Destroy(item.gameObject);
            }
        }

        while (_selfItemPool.Count > MAX_POOL_SIZE / 2)
        {
            var item = _selfItemPool.Dequeue();
            if (item != null)
            {
                Destroy(item.gameObject);
            }
        }

        Debug.Log($"优化后的对象池: {GetPoolStatistics()}");
    }

    /// <summary>
    /// 滚动到content最底部
    /// </summary>
    void OnToNewestChatMsgBtnClick()
    {
        if (scrollRect != null)
        {
            // 使用 CanvasUpdateRegistry 确保在下一帧更新布局
            Canvas.ForceUpdateCanvases();

            // 设置垂直滚动位置为1（最底部）
            scrollRect.verticalNormalizedPosition = 0f;

            AIParkChatMgr.Inst.SetAllChatRead();
            RefreshTipBtn();

            // 或者使用以下方式也可以滚动到底部
            // scrollRect.normalizedPosition = new Vector2(scrollRect.normalizedPosition.x, 0f);
        }
    }
    /// <summary>
    /// 获取userName的显示
    /// </summary>
    public string GetUserName(string name, string color, string stPlayerName = null, string stid = null)
    {
        string userName;
        string limitName = name;
        if (name.Length > 12)
        {
            limitName = name.Substring(0, 12) + "...";//限制userName长度
        }
        userName = SetNameColor(limitName, color);
        if (stPlayerName != null)
        {
            var limitstPlayerName = stPlayerName;
            if (stPlayerName.Length > 12)
            {
                limitstPlayerName = stPlayerName.Substring(0, 12) + "...";
            }
            userName = SetNameColor(limitstPlayerName, color) + "\u00A0" + "&" + "\u00A0" + userName;
        }
        return userName;
    }

    private string SetNameColor(string name, string color)
    {
        name = $"<c={color}>" + "[" + name + "]: " + "</c>";
        return name;
    }

    /// <summary>
    /// 添加新消息数据
    /// </summary>
    private void SetMessage(string msg, bool isAnim)
    {
        if (currentItem != null)
        {
            msg = msg.Replace(" ", "\u00A0");
            // currentItem.SetText(msg, isAnim,UpdateLayout);
            currentItem.SetText(msg, false);
        }
    }

    public void UpdateLayout()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(Content);
        var offset = Content.rect.height - 220;
        if (offset > 0)
        {
            Content.anchoredPosition = new Vector2(0, offset);
        }
    }

    public void ClearAllMessage()
    {
        ClearAllActiveItems();
    }

    private void OnDestroy()
    {
        isShowChatView = false;
        ClearAllMessage();

        // 清理对象池中的所有对象
        while (_otherItemPool.Count > 0)
        {
            var item = _otherItemPool.Dequeue();
            if (item != null)
            {
                Destroy(item.gameObject);
            }
        }

        while (_selfItemPool.Count > 0)
        {
            var item = _selfItemPool.Dequeue();
            if (item != null)
            {
                Destroy(item.gameObject);
            }
        }

        _activeItems.Clear();
    }

    [Button("测试添加聊天")]
    public void TestAddChat()
    {
        AIParkChatMgr.Inst.TestAddChat();
    }
}



