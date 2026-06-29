using System;
using Game.Base;
using UIAgent;

public enum OxygenState
{
    open,
    close
}

public struct OxygenBarConfig
{
    public float oxygenRatio;
    public int ProgressBarItemCount;//增益条个数
}

public class OxygenManager : GameInstance<OxygenManager>,IModeManager
{
    BudTimer oxyTimer;
    BudTimer hideTimer;
    BudTimer recoverTimer;
    BudTimer hypoxiaTimer;//缺氧定时器
    public float timeConsuming = 0.1f; //氧气消耗速度
    public float consumptionPerSecond = 0.1f; //氧气消耗量
    public static float maxOxygen = 30; //最大氧气值
    public static float maxHypoxiaTime = 15;//最大缺氧时间
    public float curHypoxiaTime = maxHypoxiaTime;
    public float curOxygen = maxOxygen;
    string LogHead = "--OxygenManager:";
    public bool isWearMask;
    public bool isPlayingSound;
    public bool isOpenOxygen;

    private Action<OxygenBarConfig> _oxygenUpdateCallback;
    

    public OxygenManager()
    {
       
    }
    
    public void OpenOxygen()
    {
        CloseAllTimer();
        UIAgentManager.Inst.OpenPanel(PanelId.OxygenPanel);
        curHypoxiaTime = maxHypoxiaTime;
        isOpenOxygen = true;
        UpdateOxygenValue();
        oxyTimer = TimerManager.Inst.Run("OxyTimer", 0, timeConsuming, () =>
        {
            UpdateOxygenValue();
        });
    }
    
    //恢复氧气
    public void RecoverOxygen()
    {
        CloseAllTimer();
        isOpenOxygen = false;
        recoverTimer = TimerManager.Inst.Run("RecoverOxygen", 0f, 0.02f, () =>
        {
            curOxygen += 0.3f;
            if (curOxygen >= maxOxygen)
            {
                CloseOxygen();
            }
            if (curOxygen >= maxOxygen)
            {
                curOxygen = maxOxygen;
            }
            UpdateOxygenValue();
        });
    }

    public void CloseOxygen()
    {
        curOxygen = maxOxygen;
        UpdateOxygenValue();
        CloseAllTimer();
        isOpenOxygen = false;
        hideTimer = TimerManager.Inst.RunOnce("CloseOxygen", 1f, () =>
        {
            CloseOxygenPanel();
        });
    }

    private void CloseOxygenPanel()
    {
        UIAgentManager.Inst.ClosePanel(WindowId.GuestWindow,PanelId.OxygenPanel);
    }

    private void Hypoxia()
    {
        CloseAllTimer();
        curHypoxiaTime = maxHypoxiaTime;
        hypoxiaTimer = TimerManager.Inst.Run("Hypoxia", 0f, 1f, () =>
           {
               curHypoxiaTime -= 1;
               if (curHypoxiaTime <= 0)
               {
                   curHypoxiaTime = maxHypoxiaTime;
               }
           });
    }

    public void CloseAllTimer()
    {
        if (oxyTimer != null)
        {
            TimerManager.Inst.Stop(oxyTimer);
            oxyTimer = null;
        }
        if (hideTimer != null)
        {
            TimerManager.Inst.Stop(hideTimer);
            hideTimer = null;
        }
        if (recoverTimer != null)
        {
            TimerManager.Inst.Stop(recoverTimer);
            recoverTimer = null;
        }
        if (hypoxiaTimer != null)
        {
            TimerManager.Inst.Stop(hypoxiaTimer);
            hypoxiaTimer = null;
        }
    }
    
    private void UpdateOxygenValue()
    {
        curOxygen -= consumptionPerSecond;
        curOxygen = curOxygen <= 0 ? 0 : curOxygen;
        OxygenBarConfig config = new OxygenBarConfig()
        {
            oxygenRatio = curOxygen / maxOxygen,
            ProgressBarItemCount = 0
        };
        
        _oxygenUpdateCallback?.Invoke(config);
    }
    

    public void ForceCloseOxygen()
    {
        curOxygen = maxOxygen;
        UpdateOxygenValue();
        CloseAllTimer();
        isOpenOxygen = false;
        CloseOxygenPanel();
    }
    
    public override void Release()
    {
        base.Release();
        if (TimerManager.HasInstance)
        {
            CloseAllTimer();
        }
    }
    
    public float GetRemainingTime()
    {
        float time = 0;
        return time;
    }

    public float GetRatio(float value)
    {
        float maxTime = 0;
        float curTime = 0;
        return 1-((curTime)/(maxTime-value));
    }


    public void AddOxygenUpdateListener(Action<OxygenBarConfig> callback)
    {
        _oxygenUpdateCallback += callback;
    }
    
    public void RemoveOxygenUpdateListener(Action<OxygenBarConfig> callback)
    {
        _oxygenUpdateCallback -= callback;
    }

    public void ClearOxygenUpdateListener()
    {
        _oxygenUpdateCallback = null;
    }

    public void OnEdit()
    {
        curOxygen = maxOxygen;
        CloseAllTimer();
        CloseOxygenPanel();
    }

    public void OnPlay()
    {
        
    }

    public void OnGuest()
    {
        
    }
}
