using System;
using Game.Audio;
using Game.Avatar;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 倒计时panel，层级规则：3 2 1 倒计时 > 顶部数字 > 其他UIPanel @PM:Risa
/// </summary>
public class GameDurationPanel : BasePanel<GameDurationPanel>
{
    public Animator startAnimator;
    public Animator noTimeAnimator;

    public Text timeText;
    private BudTimer startTimer;
    private BudTimer endTimer;

    private int curMin;
    private int curSecs;

    private long lastEffectTime = 0;

    #region WWISENAME


    private const string StartCountDown = "Play_Challenge_Start_Countdown";
    private const string DuringCountDown = "Play_Challenge_Start";
    private const string NoTimeTips = "Play_YE_Countdown_Timesup";
    private const string EndTimeCount = "Play_YE_Countdown_Number";
    private const string StopEndTimeCount = "Stop_YE_Countdown_Number";
    #endregion

    protected override void OnBecameVisible()
    {
        base.OnBecameVisible();
        this.transform.SetAsLastSibling();
    }

    protected override void OnBecameInvisible()
    {
        base.OnBecameInvisible();
        CancelInvoke();
        InputReceiver.locked = false;
        TimerManager.Inst.Stop(startTimer);
        TimerManager.Inst.Stop(endTimer);
    }

    public void OnPause()
    {
        ResetTimer();
        startAnimator.gameObject.SetActive(false);
        noTimeAnimator.gameObject.SetActive(false);
    }

    private void ResetTimer()
    {
        TimerManager.Inst.Stop(startTimer);
        if (endTimer != null)
        {
            if (AvatarController.Inst.SelfController != null)
            {
                AkSoundManager.Inst.PostEvent(StopEndTimeCount, AvatarController.Inst.SelfController.gameObject);
            }
            TimerManager.Inst.Stop(endTimer);
        }
    }

    public void SetLastEffect(long lastTime)
    {
        lastEffectTime = lastTime;
    }

    private void SetTimeText(int seconds)
    {
        curMin = seconds / 60;
        curSecs = seconds % 60;
        if (lastEffectTime > 0 && seconds == lastEffectTime)
        {
            
        }

        // 这里每1s会有一次boxing和gc
        timeText.text = $"{curMin:00}:{curSecs:00}";
    }

    public void StartCountDownAnim(GameDurationData data, Action callback = null)
    {
        SetTimeText(data.duration);
        startAnimator.gameObject.SetActive(true);
        InputReceiver.locked = true;
        TimerManager.Inst.Stop(startTimer);
        PlayCountdownSound(true);
        startTimer = TimerManager.Inst.RunOnce(nameof(StartCountDownAnim), 3.0f, () =>
        {
            startAnimator.gameObject.SetActive(false);
            AkSoundManager.Inst.PostEvent(DuringCountDown, AvatarController.Inst.SelfController.gameObject);

            InputReceiver.locked = false;
            callback?.Invoke();
        });
    }

    public void StartNoTimeAnim(Action callback = null)
    {
        noTimeAnimator.enabled = true;
        TimerManager.Inst.Stop(endTimer);
        PlayCountdownSound(false);
        endTimer = TimerManager.Inst.RunOnce(nameof(StartNoTimeAnim), 5.0f, () =>
        {
            AkSoundManager.Inst.PostEvent(NoTimeTips, AvatarController.Inst.SelfController.gameObject); 
            noTimeAnimator.enabled = false;
            callback?.Invoke();
        });
    }

    public void OnTickSecond(int passedSeconds)
    {
        var curAllSecs = curMin * 60 + curSecs;
        var lastSeconds = curAllSecs - passedSeconds;
        SetTimeText(lastSeconds);
    }

    private int CountDownCount = 0;

    private void PlayCountdownSound(bool isBegin)
    {
        CountDownCount = 0;

        if (isBegin)
        {
            AkSoundManager.Inst.PostEvent(StartCountDown, AvatarController.Inst.SelfController.gameObject);
            InvokeRepeating(nameof(DoPlayCountdownBeginSound), 0f, 1.0f);
        }
        else
        {
            AkSoundManager.Inst.PostEvent(EndTimeCount, AvatarController.Inst.SelfController.gameObject);
        }
    }

    private void DoPlayCountdownBeginSound()
    {
        if (CountDownCount < 3)
        {
            CountDownCount++;
            AkSoundManager.Inst.PostEvent(StartCountDown, AvatarController.Inst.SelfController.gameObject);
        }
        else
        {
            CancelInvoke(nameof(DoPlayCountdownBeginSound));
            CountDownCount = 0;
        }
    }

    // private void DoPlayCountdownEndSound()
    // {
    //     if (CountDownCount < 3)
    //     {
    //         CountDownCount++;
    //         AkSoundManager.Inst.PostEvent(EndTimeCount, AvatarController.Inst.SelfController.gameObject);
    //     }
    //     else
    //     {
    //         CancelInvoke(nameof(DoPlayCountdownEndSound));
    //         CountDownCount = 0;
    //     }
    // }
}