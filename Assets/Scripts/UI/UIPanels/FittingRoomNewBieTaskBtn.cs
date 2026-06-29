using EventTracking;
using Game.Event;
using Message;
using System;
using UnityEngine;
using UnityEngine.UI;

public class FittingRoomNewBieTaskBtn : MonoBehaviour
{
    public Button rootBtn;
    public RedDotItem taskReddotItem;
    public Animator animatior;
    private void Start()
    {
        rootBtn.onClick.AddListener(RootBtnClick);
    }

    private void RootBtnClick()
    {
        UIManager.Inst.OpenPanelTakeAni(PanelId.NewBieSevenDayV2TaskPanel , SetRedDot , SetRedDot);
    }
    public void PlayNewBieTaskAnimation()
    {
        animatior.Play("animation");
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