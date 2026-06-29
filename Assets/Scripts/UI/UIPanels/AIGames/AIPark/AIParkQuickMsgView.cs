using AIGame.Base;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UnityEngine;

public class AIParkQuickMsgView : MonoBehaviour
{
    private AIParkQuickEmotePanel _quickEmotePanel;

    public GameObject item;

    private bool bInited = false;   

    private List<string> _quickMsgList = new List<string>()
    {
        "你好，请问我要怎么做才可以离开这里？",
        "我觉得我的身体现在像初升的太阳般有朝气。",
        "你快理理我，我等的花儿都谢了！",
        "再不让我出去我就和这家医院同归于尽！",
        "我最喜欢在医院呆着了，谁都别让我离开这里！",
    };


    public void InitQuickMsgScrollView(AIParkQuickEmotePanel quickEmotePanel,bool inGuide = false)
    {
        _quickEmotePanel = quickEmotePanel;
        if (!bInited)
        {
            // 清理已有的子物体（除了预制体）
            for (int i = item.transform.parent.childCount - 1; i >= 0; i--)
            {
                var child = item.transform.parent.GetChild(i);
                if (child.gameObject != item)
                {
                    Destroy(child.gameObject);
                }
            }

            // 创建消息列表
            for (int i = 0; i < _quickMsgList.Count; i++)
            {
                var msgItem = Instantiate(item, item.transform.parent);
                var btn = msgItem.GetComponent<CButton>();
                if (btn != null)
                {
                    btn.SetLocalText(_quickMsgList[i]);
                    int index = i;
                    btn.onClick.AddListener(() => OnQuickMsgClick(index));
                }
                msgItem.SetActive(true);
            }

            // 隐藏预制体
            item.SetActive(false);
            bInited = true;
        }
    }

    private void OnQuickMsgClick(int index)
    {
        if (index >= 0 && index < _quickMsgList.Count)
        {
            string msg = _quickMsgList[index];
            // 发送消息到聊天面板
            var guestPanel = UIManager.Inst.FindPanel<AIParkGuestPanel>(PanelId.AIParkGuestPanel);
            if (guestPanel != null)
            {
                guestPanel.SendInput(msg);
                AIParkUtils.Inst.OnSendConversationReq();
            }
            // EventCenterDataManager.Inst.ReportTask(PostEventId.UseDoubleEmoteInMap);
            // 关闭当前面板
            if (_quickEmotePanel != null)
            {
                _quickEmotePanel.CloseSelf();
            }
        }
    }
}
