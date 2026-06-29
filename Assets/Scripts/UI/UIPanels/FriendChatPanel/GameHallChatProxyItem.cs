using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameHallChatProxyItem : MonoBehaviour
{
    public GameHallChatItem OtherItem;
    public GameHallChatItem SelfItem;
    private GameHallChatItem currentItem;
    public void SetData(OfflineMessageItem info)
    {
        bool isSelf = AccountDataManager.Inst.IsSelf(info.uid);

        OtherItem.gameObject.SetActive(!isSelf);
        SelfItem.gameObject.SetActive(isSelf);
        currentItem = isSelf ? SelfItem : OtherItem;
        Action<OfflineMessageItem> initAct = isSelf ? SelfItem.Init : OtherItem.Init;
        initAct(info);
        
        
        if (isSelf)
        {
            SelfItem.ClearBub();
            SelfItem.SetBubbleStyle(info.chatBubbles, true);
        }
        else
        {
            OtherItem.ClearBub();
            OtherItem.SetBubbleStyle(info.chatBubbles);
        }
    }

    public void AddChat(string value) {
        if (currentItem == null) {
            return;
        }
        currentItem.AddChatContent(value);
    }

}
