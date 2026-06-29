using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class AIParkGuestChatNode : MonoBehaviour
{
    public RectTransform Content;
    public AIParkGuestChatItem ItemPrefab;
    public List<ChatLineData> Lines = new();
    private AIParkGuestChatItem currentItem;

    public AIParkChatItem[] chatItems;

    // 聊天项管理
    private List<ChatItemData> _activeChatItems = new List<ChatItemData>();
    private const int MAX_VISIBLE_ITEMS = 2;
    private const float ITEM_LIFETIME = 10f; // 10秒生命周期

    // 动画设置
    private const float MOVE_DURATION = 0.3f;
    private const float FADE_DURATION = 0.5f;
    private const float MOVE_DISTANCE = 80f; // 向上移动的距离

    public int curShowCount = 0;

    public Transform Viewport;

    bool _isMoving = false;

    Queue<string> _chatQueue = new Queue<string>();

    List<string> _curPlayingChatList = new List<string>();
    BudTimer _timer1;
    BudTimer _timer2;

    float spaceY = 20;
    float viewportHeight = 0; //窗口高度
    public string testStr = "1234567890";

    void Awake()
    {
        // 初始化聊天项
        InitializeChatItems();

        viewportHeight = this.GetComponent<RectTransform>().sizeDelta.y;
    }

    /// <summary>
    /// 初始化聊天项
    /// </summary>
    private void InitializeChatItems()
    {
        for (int i = 0; i < chatItems.Length; i++)
        {
            var chatItem = chatItems[i];
            if (chatItem != null)
            {
                var rectTransform = chatItem.GetComponent<RectTransform>();
                chatItem.gameObject.SetActive(false);

                _activeChatItems.Add(new ChatItemData
                {
                    chatItem = chatItem,
                    isActive = false,
                    startTime = 0f,
                    targetPosition = rectTransform.anchoredPosition
                });
            }
        }
    }

    /// <summary>
    /// 产品要求 当内容不超出框时,内容需要对齐顶部
    /// 当内容超出框时,内容需要对齐底部
    /// </summary>
    /// <returns></returns>
    bool CheckIsAlignTop()
    {
        float totalHeight = 0;
        int count = 0;
        for (int i = 0; i < _curPlayingChatList.Count; i++)
        {
            if (_activeChatItems[i].chatItem.gameObject.activeInHierarchy)
            {
                totalHeight += _activeChatItems[i].chatItem.GetComponent<RectTransform>().sizeDelta.y;
                count++;
            }
        }
        totalHeight += spaceY * (count - 1);
        if (totalHeight < viewportHeight)
        {
            return true;
        }
        return false;
    }


    void ResetChatItemsPosAndData()
    { 
        bool lastIsAlignTop = CheckIsAlignTop();
        //当前的对齐是上或下 
        for (int i = 0; i < _curPlayingChatList.Count; i++)
        {
            _activeChatItems[i].chatItem.gameObject.SetActive(true);
            _activeChatItems[i].chatItem.SetText(_curPlayingChatList[i], false);
            _activeChatItems[i].chatItem.GetComponent<CanvasGroup>().alpha = 1;
            // 强制刷新布局以确保sizeDelta是最新的
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_activeChatItems[i].chatItem.GetComponent<RectTransform>());
        }
        for (int i = _curPlayingChatList.Count; i < _activeChatItems.Count; i++)
        {
            _activeChatItems[i].chatItem.gameObject.SetActive(false);
        }
        if (lastIsAlignTop && curShowCount < 3)
        {
            //对齐顶部
            _activeChatItems[0].chatItem.GetComponent<RectTransform>().anchoredPosition = new Vector2(_activeChatItems[0].chatItem.GetComponent<RectTransform>().anchoredPosition.x, 0);

            for (int i = 1; i < curShowCount; i++)
            {
                float y =
                    _activeChatItems[i - 1].chatItem.GetComponent<RectTransform>().anchoredPosition.y - _activeChatItems[i - 1].chatItem.GetComponent<RectTransform>().sizeDelta.y - spaceY;
                _activeChatItems[i].chatItem.GetComponent<RectTransform>().anchoredPosition = new Vector2(_activeChatItems[i].chatItem.GetComponent<RectTransform>().anchoredPosition.x, y);
            }
        }
        else
        {
            float y = -viewportHeight - spaceY;

            _activeChatItems[curShowCount - 1].chatItem.GetComponent<RectTransform>().anchoredPosition =
                new Vector2(_activeChatItems[curShowCount - 1].chatItem.GetComponent<RectTransform>().anchoredPosition.x, y);
            //对齐底部
            if (curShowCount > 1)
            {
                y =
                    -(viewportHeight - _activeChatItems[curShowCount - 2].chatItem.GetComponent<RectTransform>().sizeDelta.y);
                _activeChatItems[curShowCount - 2].chatItem.GetComponent<RectTransform>().anchoredPosition = new Vector2(_activeChatItems[curShowCount - 2].chatItem.GetComponent<RectTransform>().anchoredPosition.x, y);
            }
            if (curShowCount > 2)
            {
                y =
                    _activeChatItems[curShowCount - 2].chatItem.GetComponent<RectTransform>().anchoredPosition.y + _activeChatItems[curShowCount - 3].chatItem.GetComponent<RectTransform>().sizeDelta.y + spaceY;

                _activeChatItems[curShowCount - 3].chatItem.GetComponent<RectTransform>().anchoredPosition = new Vector2(_activeChatItems[curShowCount - 3].chatItem.GetComponent<RectTransform>().anchoredPosition.x, y);
            }
        }
        // Debug.LogError("curShowCount:" + curShowCount + " lastIsAlignTop:" + lastIsAlignTop);
        // for (int i = 0; i < _activeChatItems.Count; i++)
        // {
        //     Debug.LogError("i:" + i + " name:" + _activeChatItems[i].chatItem.gameObject.name + " pos:" + _activeChatItems[i].chatItem.GetComponent<RectTransform>().anchoredPosition.y);
        // }



    }

    /// <summary>
    /// 插入聊天信息
    /// </summary>
    public void InsertChat(string content)
    {
        if (curShowCount == 3 || _isMoving)
        {
            _chatQueue.Enqueue(content);
            return;
        }
        _curPlayingChatList.Add(content);
        // _activeChatItems[curShowCount].chatItem.gameObject.SetActive(true);
        // _activeChatItems[curShowCount].isActive = true;

        curShowCount++;
        ResetChatItemsPosAndData();
        bool isAlignTop = CheckIsAlignTop();

        TimerManager.Inst.Stop(_timer1);
        if (curShowCount > 2 && !isAlignTop)
        {
            MoveExistingItemsUp();
        }
        else
        {
            _timer1 = TimerManager.Inst.RunOnce("MoveExistingItemsUp1", ITEM_LIFETIME, () =>
            {
                MoveExistingItemsUp();
            });
        }


    }

    /// <summary>
    /// 移动现有聊天项向上
    /// </summary>
    private void MoveExistingItemsUp()
    {
        _isMoving = true;
        float movePosY = 0;
        bool isAlignTop = CheckIsAlignTop();
        Transform target = null;
        if (isAlignTop)
        {
            for (int i = 0; i < _activeChatItems.Count; i++)
            {
                if (_activeChatItems[i].chatItem.gameObject.activeInHierarchy)
                {
                    float targetY = spaceY + _activeChatItems[i].chatItem.GetComponent<RectTransform>().sizeDelta.y;
                    movePosY = targetY - _activeChatItems[i].chatItem.GetComponent<RectTransform>().anchoredPosition.y;
                    target = _activeChatItems[i].chatItem.transform;
                    break;
                }
            }
        }
        else
        {
            for (int i = _activeChatItems.Count - 1; i >= 0; i--)
            {
                if (_activeChatItems[i].chatItem.gameObject.activeInHierarchy)
                {
                    float targetY = -(viewportHeight - _activeChatItems[i].chatItem.GetComponent<RectTransform>().sizeDelta.y);
                    movePosY = targetY - _activeChatItems[i].chatItem.GetComponent<RectTransform>().anchoredPosition.y;
                    target = _activeChatItems[i].chatItem.transform;
                    break;
                }

            }
        }
        if (target != null)
        {
            target.GetComponent<CanvasGroup>().alpha = 1;
        }
        // Debug.LogError("moveY=" + movePosY + " curShowCount=" + curShowCount + " isAlignTop=" + isAlignTop + " target=" + target.gameObject.name);


        for (int i = 0; i < _activeChatItems.Count; i++)
        {
            var itemData = _activeChatItems[i];
            if (itemData.chatItem.gameObject.activeInHierarchy)
            {
                var rectTransform = itemData.chatItem.GetComponent<RectTransform>();
                Vector2 newPosition = rectTransform.anchoredPosition + Vector2.up * movePosY;

                // 执行移动动画
                rectTransform.DOAnchorPos(newPosition, MOVE_DURATION).SetEase(Ease.OutQuad);
                itemData.targetPosition = newPosition;
            }
        }

        if (curShowCount == 3)
        {
            _activeChatItems[0].chatItem.GetComponent<CanvasGroup>().DOFade(0, MOVE_DURATION).SetEase(Ease.Linear);
        }

        TimerManager.Inst.Stop(_timer2);
        _timer2 = TimerManager.Inst.RunOnce("MoveExistingItemsUp2", 0.35f, () =>
        {
            _isMoving = false;
            if (curShowCount == 0)
            {
                return;
            }
            if (curShowCount == 3)
            {
                _activeChatItems[0].chatItem.gameObject.SetActive(false); //只保留2个
            }
            curShowCount--;
            if (target != null)
            {
                if (isAlignTop)
                {
                    target.gameObject.SetActive(false);
                }
                else
                {
                    target.GetComponent<CanvasGroup>().alpha = 1;
                }
            }
            _curPlayingChatList.RemoveAt(0);
            // ResetChatItemsPosAndData();
            if (_chatQueue.Count > 0)
            {
                InsertChat(_chatQueue.Dequeue());
            }
            else
            {
                if (curShowCount > 0)
                {
                    TimerManager.Inst.Stop(_timer1);
                    _timer1 = TimerManager.Inst.RunOnce("MoveExistingItemsUp1", ITEM_LIFETIME, () =>
                    {
                        MoveExistingItemsUp();
                    });
                }
            }
        });
    }


    /// <summary>
    /// 淡出并向上移动
    /// </summary>
    // private IEnumerator FadeOutAndMoveUp(ChatItemData itemData)
    // {
    //     var chatItem = itemData.chatItem;
    //     var rectTransform = chatItem.GetComponent<RectTransform>();
    //     var canvasGroup = chatItem.GetComponent<CanvasGroup>();

    //     // 如果没有CanvasGroup组件，添加一个
    //     if (canvasGroup == null)
    //     {
    //         canvasGroup = chatItem.gameObject.AddComponent<CanvasGroup>();
    //     }

    //     // 计算需要移动的距离，确保完全移出Viewport
    //     float moveDistance = CalculateMoveDistance(rectTransform);
    //     Vector2 targetPos = rectTransform.anchoredPosition + Vector2.up * moveDistance;

    //     // 同时执行移动和淡出动画
    //     rectTransform.DOAnchorPos(targetPos, FADE_DURATION).SetEase(Ease.InQuad);
    //     canvasGroup.DOFade(0f, FADE_DURATION).SetEase(Ease.InQuad);

    //     yield return new WaitForSeconds(FADE_DURATION);

    //     // 重置状态
    //     itemData.isActive = false;
    //     itemData.startTime = 0f;
    //     chatItem.gameObject.SetActive(false);
    //     canvasGroup.alpha = 1f;

    //     // 重置位置
    //     float resetDistance = CalculateInitialDistance(rectTransform, 0);
    //     rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, -resetDistance);
    // }



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

    public void SetRecChat(string userName, string color, string content, bool isFirst, bool needAni)
    {
        if (string.IsNullOrEmpty(content) || this == null)
        {
            return;
        }
        var tempName = GetUserName(userName, color);
        if (isFirst)
        {
            // currentItem = GameObject.Instantiate(ItemPrefab, Content);
            // currentItem.SetText(tempName, false);
        }
        AIParkChatMgr.Inst.InsertChat(userName, content);
        InsertChat(tempName + content);
        // SetMessage(tempName + content, needAni);
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
        Lines.Clear();
    }

    private void OnDestroy()
    {
        TimerManager.Inst.Stop(_timer1);
        TimerManager.Inst.Stop(_timer2);
        ClearAllMessage();
    }

    int testIdx = 0;
    /// <summary>
    /// 测试添加聊天消息
    /// </summary>
    [Button("测试添加聊天")]
    public void TestAddChat()
    {
        string[] testMessages = {
            "这是一条测试消息这是一条测试消息这是一条测试消息1",
            "这是第二条测试消息这是第二条测试消息这是第二条测试消息",
            "第三条测试消息来了第三条测试消息来了第三条测试消息来了",
            "第四条测试消息第四条测试消息第四条测试消息",
            "第五条测试消息第五条测试消息第五条测试消息"
        };
        // UnityEditor.EditorApplication.isPaused = true;
        InsertChat(testMessages[testIdx++]);
        if (testIdx >= testMessages.Length)
        {
            testIdx = 0;
        }
    }

    [Button("测试")]
    public void Test()
    {
        Debug.LogError("测试");
        Debug.LogError(_activeChatItems[1].chatItem.GetComponent<RectTransform>().sizeDelta.y + "name:" + _activeChatItems[1].chatItem.gameObject.name);
        Debug.LogError(_activeChatItems[2].chatItem.GetComponent<RectTransform>().sizeDelta.y + "name:" + _activeChatItems[2].chatItem.gameObject.name);
    }

    /// <summary>
    /// 聊天项数据结构
    /// </summary>
    [System.Serializable]
    private class ChatItemData
    {
        public AIParkChatItem chatItem;
        public bool isActive;
        public float startTime;
        public Vector2 targetPosition;
    }
}



