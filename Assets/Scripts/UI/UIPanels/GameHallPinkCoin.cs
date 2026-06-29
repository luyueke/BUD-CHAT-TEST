using EventTracking;
using Game.Event;
using System;
using UnityEngine;
using UnityEngine.UI;

public class GameHallPinkCoin : MonoBehaviour
{
    public Button rootBtn;
    public RedDotItem taskReddotItem;

    private void Start()
    {
        rootBtn.onClick.AddListener(RootBtnClick);
    }

    private void RootBtnClick()
    {
        UIManager.Inst.OpenPanel(PanelId.PinkCoinPanel , SetRedDot , SetRedDot);
        string key = "FirstOpenFittingRoomPanel" + AccountDataManager.Inst.UserInfo.uid;
            if (SignInPanel.isNewPlayer && !PlayerPrefs.HasKey(key) && !PlayerPrefs.HasKey("guide_ID_8"))
        {
            PlayerPrefs.SetInt("guide_ID_8", 1);
            PlayerPrefs.Save();
            LoadEvent.ReportPopupStatus("8", "guide_ID");
        }
    }

    public void OnInitCreate(TaskInfoData taskInfoData)
    {
        SetRedDot();
    }

    public void SetRedDot()
    {
        int pinkCoinTaskNum = ReddotManagerUtils.Inst.GetRedDotCount(ReddotType.PinkCoinTask);
        taskReddotItem.SetRedDotNum(pinkCoinTaskNum);
    }
}