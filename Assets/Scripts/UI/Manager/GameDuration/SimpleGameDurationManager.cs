using System;
using Basic.Utils;
using Message;
using UnityEngine;

/// <summary>
/// 游戏倒计时manager,支持切后台保持倒计时
/// </summary>
public class SimpleGameDurationManager : GlobalInstance<SimpleGameDurationManager>, IGameMono
{
    private GameDurationData _curGameDurationData; //地图配置的游玩时间
    private GameDurationData _playPassDurationData; //游玩过程实际使用的时间
    private GameDurationBehaviorBase _behavior;
    private DurationType _durationType = DurationType.Default;

    private bool _isStart = false;
    private long _startTimestamp;
    private long _lastFixedTimeStamp;

    public SimpleGameDurationManager()
    {
        MessageHelper.AddListener(MessageName.CountDownPauseAndRecord, OnCountDownPauseAndRecord);
        MessageHelper.AddListener(MessageName.CountDownContinue, OnCountDownContinue);
    }

    public override void Release()
    {
        MessageHelper.RemoveListener(MessageName.CountDownPauseAndRecord, OnCountDownPauseAndRecord);
        MessageHelper.RemoveListener(MessageName.CountDownContinue, OnCountDownContinue);
        CountDownStop();
        ResetAllData();
        base.Release();
    }

    public void ResetAllData()
    {
        _startTimestamp = 0;
        _lastFixedTimeStamp = 0;
        _isStart = false;
        _curGameDurationData = null;
        _behavior = null;
        _playPassDurationData = null;
    }

    private void OnCountDownPauseAndRecord()
    {
        CountDownPause();
    }

    private void OnCountDownContinue()
    {
        CountDownContinue();
    }

    /// <summary>
    /// 时间戳计算，以支持切后台的时间也包含在内
    /// </summary>
    public void FixedUpdate()
    {
        if (!_isStart || _curGameDurationData == null || _behavior == null)
            return;

        // Profiler.BeginSample("GameDurationOnFixedTimePassed");
        var curStamp = GameUtils.GetTimeStamp();
        var allCostTimeSecs = curStamp - _startTimestamp;


        _behavior.leftTime = _curGameDurationData.duration - allCostTimeSecs;
        // ring
        if (!_behavior.isRinged)
        {
            if (allCostTimeSecs >= _behavior.RingSecondsPassed)
            {
                _behavior.OnRing(allCostTimeSecs);
            }
        }
        //stop
        if (allCostTimeSecs > _curGameDurationData.duration)
        {
            CountDownPause();
            _behavior?.OnTimeout(allCostTimeSecs);
            return;
        }

        // tick
        if (_lastFixedTimeStamp == 0)
        {
            _lastFixedTimeStamp = curStamp;
        }

        // //LoggerUtils.Log($"OnFixedTimePassed-----curStamp:{curStamp}, lastFixedTimeStamp:{_lastFixedTimeStamp}, _startTimestamp:{_startTimestamp}");
        var timePassedSecs = curStamp - _lastFixedTimeStamp;
        if (timePassedSecs >= 1)
        {
            _behavior.OnTick(timePassedSecs);
            _lastFixedTimeStamp = curStamp;
        }


        // Profiler.EndSample();
    }


    public void RecordPlayTimeCost()
    {
        if (_startTimestamp <= 0)
        {
            return;
        }

        var curStamp = GameUtils.GetTimeStamp();
        var allCostTimeSecs = curStamp - _startTimestamp;
        _playPassDurationData ??= new GameDurationData();
        var timeCostInt = (int)allCostTimeSecs;
        _playPassDurationData.duration = timeCostInt;
    }

    private GameDurationBehaviorBase GetGameDurationBev(GameDurationData data)
    {
        //拓展其他类型以实现不同的倒计时效果
        switch (_durationType)
        {
            case DurationType.Default:
                return new GameDurationBasicBev(data);
            case DurationType.Park:
                return new GameDurationParkBev(data);
            default:
                return new GameDurationBasicBev(data);
                break;
        }
    }


    #region 外部调用

    public int GetDurationValue()
    {
        if (_curGameDurationData == null)
        {
            return 0;
        }

        return _curGameDurationData.duration;
    }

    public int GetPlayTestDurationValue()
    {
        return GetPlayPassDurationData().duration;
    }

    public GameDurationData GetCurDurationData()
    {
        return _curGameDurationData;
    }

    public void ParserJsonData(int secs)
    {
        //LoggerUtils.Log($"GameDuration ParserJsonData: secs:{secs}");
        ResetAllData();
        SetDuration(secs);
    }

    // 地图是否设置了倒计时
    public bool IsGameDurationMap()
    {
        var settingData = GetCurDurationData();
        return settingData is { duration: > 0 };
    }

    /// <summary>
    /// 对应编辑设置中GameDuration
    /// </summary>
    /// <param name="inputSeconds">设置的通关时间，单位(S)</param>
    public void SetDuration(int inputSeconds)
    {
        //LoggerUtils.Log($"GameDurationManager OnGameDurationSet:{inputSeconds}");
        SetDuration(new GameDurationData()
        {
            duration = inputSeconds,
        });
    }

    /// <summary>
    /// 解析地图Json和设置界面时进行设置时，设置duration
    /// </summary>
    /// <param name="durationData"></param>
    public void SetDuration(GameDurationData durationData,DurationType type = DurationType.Default)
    {
        _durationType = type;
        _curGameDurationData = durationData;
    }

    public GameDurationBehaviorBase GetDurationBehaviour()
    {
        return _behavior;
    }

    public bool IsHaveGameDuration()
    {
        return _curGameDurationData is { duration: > 0 };
    }

    //入口: 编辑点试玩，游玩进图关第一帧后，传送到新图且图里有倒计时后
    public void CountDownStart(Action callBack = null)
    {
        if (_isStart || _curGameDurationData is not { duration: > 0 })
        {
            return;
        }

        _playPassDurationData = null;

        _behavior = GetGameDurationBev(_curGameDurationData);
        if (_behavior != null)
        {
            _behavior.OnStart(() =>
            {
                _startTimestamp = GameUtils.GetTimeStamp();
                _isStart = true;
                callBack?.Invoke();
            });
        }
    }

    //出口：编辑模式点回编辑，游玩模式超时，传送到新图前停掉旧图计时
    private void CountDownStop()
    {
        _behavior?.OnStop();
        _startTimestamp = 0;
        _lastFixedTimeStamp = 0;
        _isStart = false;
    }

    public void CountDownPause(bool isRecordData = true)
    {
        if (!_isStart || !IsHaveGameDuration())
        {
            return;
        }

        _isStart = false;
        _behavior?.OnPause();
        if (isRecordData)
        {
            RecordPlayTimeCost();
        }
    }

    public void CountDownContinue()
    {
        if (_isStart || !IsHaveGameDuration())
        {
            return;
        }

        _isStart = true;
        _behavior?.OnContinue();
    }

    public GameDurationData GetPlayPassDurationData()
    {
        return _playPassDurationData ?? new GameDurationData();
    }

    public bool IsTimeOut()
    {
        var settingData = GetCurDurationData();
        var curPassTimeData = GetPlayPassDurationData();
        if (settingData != null && curPassTimeData != null)
        {
            return curPassTimeData.duration > settingData.duration;
        }

        return false;
    }

    #endregion

    #region 结算实现

    public void OnPassLevelStart()
    {
        CountDownStart();
    }

    public void OnPassLevelStop()
    {
        CountDownStop();
    }

    public void OnPassLevelPause()
    {
        CountDownPause(false);
    }

    public void OnPassLevelContinue()
    {
        CountDownContinue();
    }

    public void Update()
    {
    }
    #endregion
}

public enum DurationType
{
    Default = 0, //默认医院用的
    Park = 1,
}
