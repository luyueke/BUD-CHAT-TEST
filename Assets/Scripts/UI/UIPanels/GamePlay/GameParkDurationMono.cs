using System;
using Game.Audio;
using Game.Avatar;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 倒计时panel，层级规则：3 2 1 倒计时 > 顶部数字 > 其他UIPanel @PM:Risa
/// </summary>
public class GameParkDurationMono : MonoBehaviour
{

    public Text timeText;

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

    protected void OnBecameVisible()
    {
        // this.transform.SetAsLastSibling();
    }

    protected void OnBecameInvisible()
    {
        // CancelInvoke();
        // InputReceiver.locked = false;
    }

    public void OnPause()
    {
        ResetTimer();
    }

    private void ResetTimer()
    {
        // if (AvatarController.Inst.SelfController != null)
        // {
        //     AkSoundManager.Inst.PostEvent(StopEndTimeCount, AvatarController.Inst.SelfController.gameObject);
        // }
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
        if (seconds <= 10)
        {
            timeText.text = $"<color=#FF0000>{curMin:00}:{curSecs:00}</color>";
        }
        else
        {
            timeText.text = $"{curMin:00}:{curSecs:00}";
        }
       
    }

    public void StartCountDownAnim(GameDurationData data, Action callback = null)
    {
        SetTimeText(data.duration);
        PlayCountdownSound(true);
        AkSoundManager.Inst.PostEvent(DuringCountDown, AvatarController.Inst.SelfController.gameObject);

        InputReceiver.locked = false;
        callback?.Invoke();
    }

    public void StartNoTimeAnim(Action callback = null)
    {

        PlayCountdownSound(false);

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
        //屏蔽倒计时声音
        // if (isBegin)
        // {
        //     AkSoundManager.Inst.PostEvent(StartCountDown, AvatarController.Inst.SelfController.gameObject);
        //     InvokeRepeating(nameof(DoPlayCountdownBeginSound), 0f, 1.0f);
        // }
        // else
        // {
        //     AkSoundManager.Inst.PostEvent(EndTimeCount, AvatarController.Inst.SelfController.gameObject);
        // }
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