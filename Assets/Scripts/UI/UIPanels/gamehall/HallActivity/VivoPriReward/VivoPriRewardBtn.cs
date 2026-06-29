using System;
using Message;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// vivo 特权按钮
/// </summary>
public class VivoPriRewardPackBtn : MonoBehaviour
{
    BudTimer _timer;
    public CButton btn;


    private int _day = -1;

    bool isVivoSupport = false;

    public GameObject redDotGo;
    public void Awake()
    {
        isVivoSupport = VivoPriRewardPackMgr.Inst.IsVivoSupport();
        gameObject.SetActive(isVivoSupport);
        MessageHelper.AddListener(MessageName.UpdateVivoPriState, RefreshUI);
        btn.onClick.AddListener(OnClick);
      
        if (isVivoSupport)
        {
            RefreshUI();
            _timer = TimerManager.Inst.Run("VivoPriRewardPackBtn", 0, 1, () =>
            {
                CheckTime();
            });
        }
        else
        {
            Destroy(gameObject);
        }

    }

    void CheckTime()
    {
        DateTime now = TcpTimeSystem.Inst.ServerDataTime;
        if (now.Day != _day)
        {
            _day = now.Day;
            VivoPriRewardPackMgr.Inst.GetVivoPriRewardInfo();
        }
    }

    void RefreshUI()
    {
        //红点
        redDotGo.SetActive(VivoPriRewardPackMgr.Inst.CheckHasRedDot());
    }

    public void OnDestroy()
    {
        TimerManager.Inst.Stop(_timer);
        _timer = null;
        MessageHelper.RemoveListener(MessageName.UpdateVivoPriState, RefreshUI);
    }

    public void OnClick()
    {
        VivoPriRewardPackMgr.Inst.OpenVivoPriRewardWin();
    }

  
}

