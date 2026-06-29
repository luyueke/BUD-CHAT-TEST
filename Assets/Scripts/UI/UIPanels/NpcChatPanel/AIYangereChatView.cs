using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

public class AIYangereChatView : MonoBehaviour
{
    public RectTransform Content;
    public AIYangereChatItem ItemPrefab;
    public List<ChatLineData> Lines = new();
    private AIYangereChatItem currentItem;
    
    private readonly string[] UserNameColor = new[]
    {
        "<c=B5ABFF>", "<c=FF8989>", "<c=00ADFF>", "<c=02E880>"
    };

    /// <summary>
    /// 获取userName的显示
    /// </summary>
    public string GetUserName(string name,string color, string stPlayerName = null, string stid = null)
    {
        string userName;
        string limitName = name;
        if (name.Length > 12)
        {
            limitName = name.Substring(0, 12) + "...";//限制userName长度
        }
        userName = SetNameColor(limitName,color);
        if (stPlayerName != null)
        {
            var limitstPlayerName = stPlayerName;
            if (stPlayerName.Length > 12)
            {
                limitstPlayerName = stPlayerName.Substring(0, 12) + "...";
            }
            userName = SetNameColor(limitstPlayerName,color) + "\u00A0" + "&" + "\u00A0" + userName;
        }
        return userName;
    }

    private string SetNameColor(string name,string color)
    {
        name =  $"<c={color}>" + "[" + name + "]: " + "</c>";
        return name;
    }

    public void SetRecChat(string userName,string color, string content,bool isFirst,bool needAni)
    {
        if (string.IsNullOrEmpty(content) || this == null)
        {
            return;
        }
        var tempName = GetUserName(userName,color);
        if (isFirst)
        {
            currentItem = GameObject.Instantiate(ItemPrefab, Content);
            currentItem.SetText(tempName,false);
        }
        SetMessage(tempName + content,needAni);
    }

    public void SetRecChatWithoutName(string content, bool isFirst, bool needAni)
    {
        if (string.IsNullOrEmpty(content) || this == null)
        {
            return;
        }
        if (isFirst)
        {
            currentItem = GameObject.Instantiate(ItemPrefab, Content);
        }
        SetMessage(content, needAni);
    }


    /// <summary>
    /// 添加新消息数据
    /// </summary>
    private void SetMessage(string msg,bool isAnim)
    {
        if (currentItem != null)
        {
            msg = msg.Replace(" ", "\u00A0");
            currentItem.SetText(msg, isAnim,UpdateLayout);
        }
    }

    public void UpdateLayout()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(Content);
        var offset = Content.rect.height - 220;
        if (offset > 0)
        {
            Content.anchoredPosition = new Vector2(0,offset);
        }
    }

    public void ClearAllMessage()
    {
        Lines.Clear();
    }

    private void OnDestroy()
    {
        ClearAllMessage();
    }
}

public class ChatLineData
{
    public string Text;
}

