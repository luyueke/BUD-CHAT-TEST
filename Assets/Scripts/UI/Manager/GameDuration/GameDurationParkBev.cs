using AIGame.Base;
using Message;
using System;

public class GameDurationParkBev : GameDurationBehaviorBase
{
    private GameParkDurationMono _curDurationPanel;
    private AIParkGuestPanel _curAIParkGuestPanel;

    private float Event_Tips_30; // 事件剩余小于等于30s提示
    private float Event_Tips_0; // 事件结束提示
    public GameDurationParkBev(GameDurationData durationData, long ringSecondsPassed = 0) : base(durationData, ringSecondsPassed)
    {
        //倒计时剩20秒时触发OnRing
        this.RingSecondsPassed = durationData.duration - 20;
        isRinged = false;

        var sceneIdx = AIParkUtils.Inst.ParkCustomData.SceneIndex;
        var events = AIParkUtils.Inst.ParkGameData.events;
        if (events[sceneIdx - 1].EventType == 1) 
        {
            //选择事件
            Event_Tips_30 = durationData.duration - 30;
        }
        else
        {
            Event_Tips_30 = 0;
        }
  
        Event_Tips_0 = durationData.duration;
    }

    public override void OnStart(Action act)
    {
        isRinged = false;
        _curAIParkGuestPanel = UIManager.Inst.FindPanel<AIParkGuestPanel>(WindowId.GuestWindow, PanelId.AIParkGuestPanel);
        _curDurationPanel = _curAIParkGuestPanel.GetComponentInChildren<GameParkDurationMono>(true);
        _curDurationPanel?.StartCountDownAnim(DurationData,act);
    }

    public override void OnStop()
    {
        isRinged = false;
        if (_curDurationPanel != null)
        {
            _curDurationPanel.OnPause();
        }
        UIManager.Inst.ClosePanel(PanelId.GameDurationPanel);
        _curDurationPanel = null;
    }

    public override void OnPause()
    {
        if (_curDurationPanel == null)
        {
            _curAIParkGuestPanel = UIManager.Inst.FindPanel<AIParkGuestPanel>(WindowId.GuestWindow, PanelId.AIParkGuestPanel);
            _curDurationPanel = _curAIParkGuestPanel.GetComponentInChildren<GameParkDurationMono>();
        }

        if (_curDurationPanel != null)
        {
            _curDurationPanel.OnPause();
        }
    }

    public override void OnContinue()
    {
    }

    public override void OnTick(long passedSecs)
    {
        if (_curDurationPanel == null)
        {
            _curAIParkGuestPanel = UIManager.Inst.FindPanel<AIParkGuestPanel>(WindowId.GuestWindow, PanelId.AIParkGuestPanel);
            _curDurationPanel = _curAIParkGuestPanel.GetComponentInChildren<GameParkDurationMono>();
        }

        if (_curDurationPanel != null)
        {
            _curDurationPanel.OnTickSecond((int)passedSecs);
        }

        if (Event_Tips_30 > 0)
        {
            Event_Tips_30 -= passedSecs;
            if (Event_Tips_30 <= 0)
            {
                TipPanel.ShowToast("当前事件将在30s后结束，请在结束前完成选择");
            }
        }

        if (Event_Tips_0 > 0)
        {
            Event_Tips_0 -= passedSecs;
            if (Event_Tips_0 <= 0)
            {
                var sceneIdx = AIParkUtils.Inst.ParkCustomData.SceneIndex;
                var events = AIParkUtils.Inst.ParkGameData.events;
                TipPanel.ShowToast($"事件“{events[sceneIdx-1].EventOpts[0].EventName}”已结束");
            }
        }
    }

    public override void OnRing(long allPassedSecs)
    {
        isRinged = true;

        if (_curDurationPanel == null)
        {
            _curAIParkGuestPanel = UIManager.Inst.FindPanel<AIParkGuestPanel>(WindowId.GuestWindow, PanelId.AIParkGuestPanel);
            _curDurationPanel = _curAIParkGuestPanel.GetComponentInChildren<GameParkDurationMono>();
        }

        if (_curDurationPanel != null)
        {
            _curDurationPanel.StartNoTimeAnim();
        }
        MessageHelper.Broadcast(MessageName.Park_GetNextSceneData);
    }


    public override void OnTimeout(long timeCost)
    {
        MessageHelper.Broadcast(MessageName.Park_CountDownEnd);
    }
}