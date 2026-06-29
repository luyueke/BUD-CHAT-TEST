using System;
using Message;

public class GameDurationBasicBev : GameDurationBehaviorBase
{
    private GameDurationPanel _curDurationPanel;
    
    public GameDurationBasicBev(GameDurationData durationData, long ringSecondsPassed = 0) : base(durationData, ringSecondsPassed)
    {
        //倒计时剩5秒时触发OnRing
        this.RingSecondsPassed = durationData.duration - 5;
        isRinged = false;
    }

    public override void OnStart(Action act)
    {
        isRinged = false;
        _curDurationPanel = UIManager.Inst.OpenPanel<GameDurationPanel>(PanelId.GameDurationPanel);
        _curDurationPanel.StartCountDownAnim(DurationData, act);
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
            _curDurationPanel = UIManager.Inst.FindPanel<GameDurationPanel>(WindowId.GuestWindow, PanelId.GameDurationPanel);
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
            _curDurationPanel = UIManager.Inst.FindPanel<GameDurationPanel>(WindowId.GuestWindow, PanelId.GameDurationPanel);
        }
        
        if (_curDurationPanel != null)
        {
            _curDurationPanel.OnTickSecond((int)passedSecs);
        }
    }

    public override void OnRing(long allPassedSecs)
    {
        isRinged = true;
        
        if (_curDurationPanel == null)
        {
            _curDurationPanel = UIManager.Inst.FindPanel<GameDurationPanel>(WindowId.GuestWindow, PanelId.GameDurationPanel);
        }
        
        if (_curDurationPanel != null)
        {
            _curDurationPanel.StartNoTimeAnim();
        }
    }

    public override void OnTimeout(long timeCost)
    {
        MessageHelper.Broadcast(MessageName.PassLevelJudging);
    }
}