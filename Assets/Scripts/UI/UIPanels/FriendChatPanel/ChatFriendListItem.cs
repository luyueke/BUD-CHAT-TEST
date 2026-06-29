
using System;
using Basic.Utils;
using Game.Config;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class ChatFriendListItem : MonoBehaviour
{
    [SerializeField] private HeadViewWidget headViewWidget;
    [SerializeField] private CText userNameTxt;
    [SerializeField] private GameObject onlineImg;
    [SerializeField] private CButton rootButton;
    [SerializeField] private GameObject reddotObj;
    [SerializeField] private Text reddotText;
    [SerializeField] private GameObject selected;
    [SerializeField] private GameObject bg;
    [SerializeField] private Text buddyName;
    [SerializeField] private GameObject buddyTag;
    
    private ConversationListItem ConversationListItem;
    private Action<ConversationListItem> clickAction;

    private void Start()
    {
        rootButton.onClick.AddListener(() =>
        {
            if (clickAction != null)
            {
                clickAction.Invoke(ConversationListItem);
            }
            reddotObj.gameObject.SetActive(false);
        });
    }

    public void SetData(ConversationListItem info, Action<ConversationListItem> action)
    {

        clickAction = action;
        if (info == null)
        {
            return;
        }

        ConversationListItem = info;
        userNameTxt.text = GameUtils.SubStringByBytes(ConversationListItem.nickname, 12);
        buddyName.text = GameUtils.SubStringByBytes(ConversationListItem.nickname, 12);
        reddotObj.gameObject.SetActive(info.count > 0);
        reddotText.text = info.count.ToString();
        int isOnline = ConversationListItem.isOnline;
        onlineImg.gameObject.SetActive(isOnline > 0);
        selected.gameObject.SetActive(info.isSelect);
        bg.gameObject.SetActive(info.isSelect);
        
        if (info.uid == GameConsts.AIBuddyTag)
        {
            userNameTxt.gameObject.SetActive(false);
            buddyName.gameObject.SetActive(true);
            buddyTag.gameObject.SetActive(true);
        }
        else
        {
            userNameTxt.gameObject.SetActive(true);
            buddyName.gameObject.SetActive(false);
            buddyTag.gameObject.SetActive(false);
            headViewWidget.InitHeadCycle(info.userInfo);
        }
    }

}
