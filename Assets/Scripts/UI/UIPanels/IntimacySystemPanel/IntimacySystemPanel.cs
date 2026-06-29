using System;
using System.Collections.Generic;
using Game.Audio;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class IntimacySystemPanel : BasePanel<IntimacySystemPanel>
{
    public FriendsView friendsView;
    public FollowView followingView;
    public FriendRequestView friendRequestView;
    public CButton hideBtn;

    public Transform tableContent;
    public Toggle table;
    private Dictionary<string, Tuple<Toggle, Text>> tableTTI = new ();
    private string select;
    private List<string> taskIdList = new List<string>()
    {
        "Friends",
        "Request",
        "Follow"
    };

    private Color normalColor = new Color(0.6862745f, 0.6862745f, 0.6862745f, 1.0f);

    private bool firstOpen = true;
    private Image reddotObj;
    private CText reddotNum;


    public override void OnCreate()
    {
        base.OnCreate();

        hideBtn.onClick.AddListener(() =>
        {
            CloseSelf();
        });

        foreach (var v in taskIdList)
        {
            var item = Instantiate(table, tableContent);
            item.onValueChanged.AddListener(arg => TableValueChange(v, arg));
            item.gameObject.SetActive(true);
            var trs = item.transform;
            var txt = trs.GetComponentInChildren<Text>();


            if (v.Equals("Friends"))
            {
                txt.SetLocalText("好友");
            } else if (v.Equals("Request"))
            {
                txt.SetLocalText("好友请求");
                reddotObj = GameObjectEx.FindComponentByName<Image>(trs,"redot");
                reddotNum = GameObjectEx.FindComponentByName<CText>(trs,"redddotText");
                SetFriendRequestReddot();
            } else if (v.Equals("Follow"))
            {
                txt.SetLocalText("关注");
            }
            tableTTI.Add(v, new Tuple<Toggle, Text>(item, txt));
        }

        tableTTI["Friends"].Item1.isOn = true;
        ShowFriendView();
    }

    private void SetFriendRequestReddot()
    {
        if (reddotObj == null || reddotNum == null)
        {
            return;
        }
        int applyFriendNum = ReddotManagerUtils.Inst.GetRedDotCount(ReddotType.ApplyingFriend);
        reddotObj.gameObject.SetActive(applyFriendNum > 0);
        string redotNumText = applyFriendNum > 99 ? "99+" : applyFriendNum.ToString();
        reddotNum.text = redotNumText;
    }

    private void TableValueChange(string id, bool arg)
    {
        tableTTI[id].Item2.color = arg ? Color.white : normalColor;
        if (arg)
        {
            select = id;
        }

        if (id.Equals("Friends"))
        {
            if (arg)
            {
                ShowFriendView();
                AkSoundManager.Inst.PlayUIEffectSound(UISoundType.UI_ShiftTab_B1);
            }
        }
        else if (id.Equals("Request"))
        {
            if (arg)
            {
                ShowFriendRequestView();
                AkSoundManager.Inst.PlayUIEffectSound(UISoundType.UI_ShiftTab_B1);
            }
        }
        else if (id.Equals("Follow"))
        {
            if (arg)
            {
                ShowFollowView();
                AkSoundManager.Inst.PlayUIEffectSound(UISoundType.UI_ShiftTab_B1);
            }
        }

        firstOpen = false;
    }

    private void ShowFriendView()
    {
        friendsView.gameObject.SetActive(true);
        friendRequestView.gameObject.SetActive(false);
        followingView.gameObject.SetActive(false);
        friendsView.OnInitCreated();
    }

    private void ShowFriendRequestView()
    {
        friendsView.gameObject.SetActive(false);
        friendRequestView.gameObject.SetActive(true);
        followingView.gameObject.SetActive(false);
        friendRequestView.OnInitCreated();

        ReddotManagerUtils.Inst.CleanRedDotNum(ReddotType.ApplyingFriend);
        SetFriendRequestReddot();
    }

    private void ShowFollowView()
    {
        friendsView.gameObject.SetActive(false);
        friendRequestView.gameObject.SetActive(false);
        followingView.gameObject.SetActive(true);
        followingView.OnInitCreated();
    }



}
