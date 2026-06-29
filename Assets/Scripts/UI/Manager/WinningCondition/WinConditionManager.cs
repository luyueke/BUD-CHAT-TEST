using System;
using System.Collections.Generic;
using System.Linq;
using Basic.UndoRedo;
using Es;
using Game.Base;
using Game.ECS;
using Game.MapSetting;
using Game.Props.PropsComponents;
using UndoSystem;
using UnityEngine;

/// <summary>
/// 通关条件设置manager
/// shaocheng
/// 2023-5-5 11:37:48
/// </summary>
public class WinConditionManager : GameInstance<WinConditionManager>, IPassLevelMgr
{
    private const string NoGoalTip = "No goals, just enjoy";
    private const string SameWIdError = "the wId is the same, skip it";
    private readonly string _configPath = "Configs/WinCondition";
    private Dictionary<int, WinConditionBase> _curConditions = new Dictionary<int, WinConditionBase>();

    public WinConditionManager()
    {
        _curConditions?.Clear();
    }

    public override void Release()
    {
        base.Release();
        _curConditions?.Clear();
    }

    private void ResetCurrentConditions()
    {
        _curConditions ??= new Dictionary<int, WinConditionBase>();
        _curConditions?.Clear();
    }

    private bool IsConditionContains(int id)
    {
        return _curConditions is { Count: > 0 } && _curConditions.ContainsKey(id);
    }

    public WinCondition GetConfigById(int wId)
    {
        return DataTables.GetWinCondition(wId);
    }

    private void ForLoopCurConditions(Action<WinConditionBase> cb)
    {
        if (_curConditions is not { Count: > 0 }) return;
        foreach (var c in _curConditions.Values)
        {
            cb.Invoke(c);
        }
    }

    private int GetWinIdByType(WinConditionType type)
    {
        // foreach (var c in _configs.Values)
        // {
        //     if (c.type == type)
        //     {
        //         return c.id;
        //     }
        // }

        return 0;
    }

    #region 外部调用

    /// <summary>
    /// 保存时获取数据塞到json
    /// </summary>
    /// <returns></returns>
    // public List<WinConditionData> GetMapJsonData()
    // {
    //     if (_curConditions is not { Count: > 0 }) return null;
    //     var data = new List<WinConditionData>();
    //     foreach (var c in _curConditions.Values)
    //     {
    //         data.Add(new WinConditionData()
    //         {
    //             id = c.Config.id
    //         });
    //     }
    //
    //     return data;
    // }
    public void SetDefaultGoalAndHintOnCreateMap()
    {
        // SceneBuilder.Inst.SetGameHint("");
        // SetWinCondition(1, (result, msg) =>
        // {
        //     if (result) ReachEndFlagCondition.SetInitialFlagPosition(new UnityEngine.Vector3(0.22f, 0, 5f));
        // });
    }

    public int GetCurrentConditionId()
    {
        if (_curConditions is not { Count: > 0 }) return 0;
        foreach (var c in _curConditions.Values)
        {
            //todo:fsc 目前只支持设置一个
            if (c.Config != null)
            {
                return c.Config.Id;
            }
        }

        return 0;
    }

    public WinCondition GetCurrentConditionConfig()
    {
        if (_curConditions is not { Count: > 0 }) return null;
        foreach (var c in _curConditions.Values)
        {
            //todo:fsc 目前只支持设置一个
            if (c.Config != null)
            {
                return c.Config;
            }
        }

        return null;
    }

    public T GetCurrentWinCondition<T>() where T : WinConditionBase 
    {
        if (_curConditions is not { Count: > 0 }) return null;
        return _curConditions.Values.First() as T;
    }
    
    public WinConditionBase GetCurrentWinCondition()
    {
        return GetCurrentWinCondition<WinConditionBase>();
    }

    public (bool addResult, string msg) AddWinCondition(int wId, bool isFromSetting = false)
    {
        var config = GetConfigById(wId);
        if (config == null) return (false, $"id:{wId} config not found");
        if (_curConditions.ContainsKey(wId)) return (false, $"id:{wId} already contains condition");
        var newWCondition = WinConditionFactory.CreateNew(config);
        _curConditions.Add(wId, newWCondition);
        if (isFromSetting) newWCondition.OnEditAdd();
        return (true, $"add {wId} condition success");
    }

    #region Add/Remove通关条件 && Undo/Redo

    public static void AddWinConditionRecord(int oldWId, int newWId)
    {
        LoggerUtils.Log($"WinConditionManager AddWinConditionRecord : {oldWId}");

        var beginData = new WinConditionUndoData
        {
            wId = oldWId
        };

        var endData = new WinConditionUndoData
        {
            wId = newWId
        };

        var record = new UndoRecord(UndoHelperName.WinConditionUndoHelper)
        {
            BeginData = beginData,
            EndData = endData
        };
        UndoRecordPool.Inst.PushRecord(record);
    }

    
    //数据保存到Cmp
    private void SaveDataToCmp()
    {
        PassLevelDataManager.Inst.SetWinConditionData(GetCurrentConditionId());
    }


    public void SetWinConditionBySelectIcon(int wId, Action cb = null)
    {
        if (wId == 0)
        {
            return;
        }

        SetWinCondition(wId, (success, resultMsg) =>
        {
            if (!success && resultMsg == SameWIdError)
            {
                cb?.Invoke();
            }
        });
    }

    /// <summary>
    /// 设置界面调用，设置通关条件
    /// </summary>
    /// <param name="newWId"></param>
    /// <param name="setFinishCb">完成设置后的回调</param>
    public void SetWinCondition(int newWId, Action<bool, string> setFinishCb = null)
    {
        //添加通关条件
        var curWid = GetCurrentConditionId();
        if (newWId == curWid)
        {
            SaveDataToCmp();
            var curCondition = GetCurrentWinCondition();
            curCondition.OnEditIconSelect();
            setFinishCb?.Invoke(false, SameWIdError);
            return;
        }

        //变更通关条件时的二次确认弹窗 Condition1 -> None, Condition1 -> Condition2
        if (curWid != 0)
        {
            ShowChangeConditionConfirmPanel(newWId, setFinishCb);
            return;
        }

        // None->Condition1
        DoSetWinCondition(newWId, setFinishCb);
    }

    private void DoSetWinCondition(int newWId, Action<bool, string> setFinishCb = null)
    {
        var curConditionId = GetCurrentConditionId();
        if (curConditionId != 0)
        {
            RemoveWinCondition(curConditionId);
        }

        var r = AddWinCondition(newWId, true);

        //切换通关条件后，添加undo record
        if (newWId == 0 || r.addResult)
        {
            AddWinConditionRecord(curConditionId, newWId);
        }

        SaveDataToCmp();
        setFinishCb?.Invoke(r.addResult, r.msg);
    }

    private void ShowChangeConditionConfirmPanel(int newWid, Action<bool, string> setFinishCb = null)
    {
        void OnClickChange()
        {
            DoSetWinCondition(newWid, setFinishCb);
        }

        void OnClickCancel()
        {
            setFinishCb?.Invoke(false, "Change Win condition canceled");
        }

        var fromText = GetCurrentConditionConfig().SettingButtonText;
        var toText = newWid == 0 ? "无" : GetConfigById(newWid).SettingButtonText;
        UIManager.Inst.OpenPanel(PanelId.WinConditionConfirmPanel, fromText, toText, new Action(OnClickChange), new Action(OnClickCancel));
    }

    public void RemoveWinCondition(int wId)
    {
        //移除通关条件
        if (!IsConditionContains(wId))
        {
            return;
        }

        _curConditions[wId].OnEditRemove();
        _curConditions.Remove(wId);
    }

    /// <summary>
    /// 删除通关条件道具时，接管删除流程，这里提供一个基础实现，若有特殊的规则则由各自manager自行实现删除流程
    /// </summary>
    /// <returns></returns>
    public bool OnDestroyBtnClicked(GameObject curObj)
    {
        var baseBev = curObj.GetComponent<NodeBaseBehaviour>();
        if (baseBev == null) return false;
        var goCmp = baseBev.entity.GetComp<GameObjectComponent>();
        if (goCmp == null) return false;
        if (IsWinConditionProp((int)goCmp.ModelType, out var winCfg))
        {
            var curCondition = GetCurrentWinCondition();
            if (curCondition != null && !curCondition.IsCanDeleteProp(baseBev))
            {
                return true;
            }
            
            SetWinCondition(0);
            return true;
        }

        return false;
    }

    #endregion


    public bool IsHaveWinCondition()
    {
        return _curConditions is { Count: > 0 };
    }

    public bool IsWinConditionProp(int modelType, out WinCondition winConfig)
    {
        var winConfigs = DataTables.GetWinConditionList();
        foreach (var wConfig in winConfigs)
        {
            if (wConfig.PropModelType == modelType)
            {
                winConfig = wConfig;
                return true;
            }
        }

        winConfig = null;
        return false;
    }

    /// <summary>
    /// 获取右上角Goal文本
    /// </summary>
    /// <returns></returns>
    public string GetWinConditionGoalTip()
    {
        if (_curConditions is not { Count: > 0 }) return NoGoalTip;
        return _curConditions.First().Value.Config != null ? _curConditions.First().Value.Config.SettingButtonText : NoGoalTip;
    }

    /// <summary>
    /// 获取已经获胜的条件
    /// </summary>
    /// <returns></returns>
    public WinConditionBase GetFinalWinnerCondition()
    {
        if (_curConditions is not { Count: > 0 }) return null;
        foreach (var c in _curConditions.Values)
        {
            if (c.IsSuccess()) return c;
        }

        return null;
    }

    #endregion

    #region 结算实现
    
    public void OnParseComponentData(PassLevelComponent component)
    {
        if (component == null) return;
        LoggerUtils.Log($"WinCondition OnParseComponentData: data:{component.WinConditionId}");
        ResetCurrentConditions();
        AddWinCondition(component.WinConditionId);
    }

    public void OnPassLevelStart()
    {
        ForLoopCurConditions((c) => { c.OnStart(); });
    }

    public void OnPassLevelStop()
    {
        ForLoopCurConditions((c) => { c.OnStop(); });
    }

    public void OnPassLevelPause()
    {
    }

    public void OnPassLevelContinue()
    {
    }

    public PassLevelResult CountPassLevelResult()
    {
        if (_curConditions is not { Count: > 0 })
        {
            return PassLevelResult.NotPassed;
        }

        ForLoopCurConditions((c) =>
        {
            var cResult = c.DoResultJudging();
            switch (cResult)
            {
                case WinConditionStatus.Succeed:
                    c.OnSuccess();
                    break;
                case WinConditionStatus.Failed:
                    c.OnFailed();
                    break;
            }
        });

        // todo:fsc 目前只有一个通关条件胜利，这里有点多余
        var passedCount = 0;
        passedCount += _curConditions.Values.Count(c => c.Status == WinConditionStatus.Succeed);
        if (passedCount == _curConditions.Count)
        {
            return PassLevelResult.Passed;
        }

        var failedCount = 0;
        failedCount += _curConditions.Values.Count(c => c.Status == WinConditionStatus.Failed);
        if (failedCount > 0)
        {
            return PassLevelResult.Failed;
        }

        return PassLevelResult.NotPassed;
    }

    public void SavePassLevelData(ref PassLevelData passLevelData)
    {
        var curWinCon = GetFinalWinnerCondition();
        if (curWinCon?.Config != null)
        {
            passLevelData.PassedType = curWinCon.Config.Id;
        }
    }

    #endregion
}

public enum WinResult
{
    NotPassed = 0,
    Passed,
    Failed,
}