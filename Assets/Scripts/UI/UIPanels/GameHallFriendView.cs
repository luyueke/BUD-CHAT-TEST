using Game.Audio;
using Game.GameHall.View;
using Message;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GameHallFriendView
{
    private GameObject defaultGo;
    private Transform m_view;
    private GameHallFriendEntry gameEntry;
    private CButton addFriendBtn;
    private CText reddotText;
    private Image redotObject;

    public GameHallFriendView Bind(GameObject rootUI)
    {
        m_view = GameObjectEx.FindChildByName(rootUI, "LeftView/FriendView");
        defaultGo = GameObjectEx.FindChildByName(m_view, "Default").gameObject;
        addFriendBtn = GameObjectEx.FindComponentByName<CButton>(m_view, "FriendRequest");
        reddotText = GameObjectEx.FindComponentByName<CText>(m_view, "redddotText");
        redotObject = GameObjectEx.FindComponentByName<Image>(m_view, "redot");
        defaultGo.GetComponent<Button>().onClick.AddListener(() =>
        {
            UIManager.Inst.OpenPanel(PanelId.IntimacySystemPanel);
        });

        gameEntry = m_view.GetComponentInChildren<GameHallFriendEntry>();
        gameEntry.AddClickListener(OnScrollViewClick);
        gameEntry.GetFirstPageFriendDatas(OnHasFriends);
        addFriendBtn.onClick.AddListener(() => { UIManager.Inst.OpenPanel(PanelId.IntimacySystemPanel); });
        MessageHelper.AddListener(MessageName.AddFriendSuccess, AddFriendSuccess);
        return this;
    }

    public void Refresh()
    {
        if (gameEntry != null)
        {
            gameEntry.GetFirstPageFriendDatas(OnHasFriends);
        }
    }
    
    private void AddFriendSuccess()
    {
        if (gameEntry != null)
        {
            gameEntry.GetFirstPageFriendDatas(OnHasFriends);
        }
    }


    private void OnHasFriends(bool hasFriends)
    {
        defaultGo.SetActive(!hasFriends);
    }

    private void OnScrollViewClick(PointerEventData data)
    {
        UIManager.Inst.OpenPanel(PanelId.IntimacySystemPanel);
        AkSoundManager.Inst.PlayUIEffectSound(UISoundType.UI_OpenPage_A5);
    }


    public void SetRedDotNum(int reddotNum)
    {
        if (redotObject == null || reddotText == null)
        {
            return;
        }

        redotObject.gameObject.SetActive(reddotNum > 0);
        string redotNumText = reddotNum > 99 ? "99+" : reddotNum.ToString();
        reddotText.text = redotNumText;
    }
}