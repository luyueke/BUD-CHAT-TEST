using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using AIGame.Base;
using Message;
using Newtonsoft.Json;
using UnityEngine;

public class YandereDataManager : GlobalInstance<YandereDataManager>
{
    private AIResultRespose _curAiResultData = new AIResultRespose();
    public int FinalResult { get; set; }
    public bool IsFirstReply { get; set; }
    public float TextAnimDuration = 2;
    public bool IsTryAgain = false;
    public void Init()
    {
        ClearData();
    }

    public void SetNpcOpData(int npcLocation,int moodOption)
    {
        _curAiResultData.npcLocation = npcLocation;
        _curAiResultData.moodOption = moodOption;
    }

    public void SetFollowPlayer(int followPlayer)
    {
        _curAiResultData.followPlayer = followPlayer;
    }

    public void SetResult(int result)
    {
        _curAiResultData.result = result;
    }

    public int GetResult(YandereStep curStep)
    {
        switch (curStep)
        {
            case YandereStep.BadEnd_1:
                return 1;
            case YandereStep.BadEnd_2:
                return 2;
            case YandereStep.BadEnd_3:
                return 3;
            case YandereStep.GoodEnd_1:
                return 4;
            case YandereStep.GoodEnd_2:
                return 5;
        }
        return 0;
    }
    

    public string GetStateContent(int currencyState)
    {
        switch (currencyState)
        {
            case 1:
                return "优优妹心情变好了～";
            case 2:
                return "优优妹开始生气了！";
            case 3:
                return "优优妹有点难过QAQ";
        }
        return "";
    }

    public int GetCurrencyState()
    {
        return _curAiResultData.currencyState;
    }

    //获取结局
    public int GetResult()
    {
        return _curAiResultData.result;
    }
    
    public bool CanNpcFollowPlayer()
    {
        var canFollowBySever = _curAiResultData.followPlayer == 1;
        var aiGame = AIGameController.Inst.GetCurAIGame<AIYandereGame>();
        //逃杀模式必须跟随
        if (aiGame.IsOpenDoor())
        {
            canFollowBySever = true;
        }
        var isGameStart = aiGame.CheckStartGame(false);
        return canFollowBySever && isGameStart;
    }

    public AIResultRespose GetCurData()
    {
        return _curAiResultData;
    }
    
    public void ClearData()
    {
        FinalResult = 0;
        IsFirstReply = false;
        _curAiResultData?.ClearData();
    }
    
    public override void Release()
    {
        base.Release();
        ClearData();
    }
}
