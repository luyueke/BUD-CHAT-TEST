using System;
using Es;
using Game.Base;
using Game.Config;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using Game.Props.PropsManagers;
using UI.Manager;
using UnityEngine;

/// <summary>
/// 通关条件：触碰到旗子
/// </summary>
public class CollectStarCondition : WinConditionBase
{
    private CollectStarManager mgr;
    private const string PropConfigId = "20100028";
    private readonly Vector3 _cloneOffset = new Vector3(-0.2f, 0f, 0.0001f);


    public CollectStarManager Mgr
    {
        get
        {
            if (mgr == null)
            {
                mgr = GlobalNodeManager.Inst.Get<CollectStarManager>();
            }

            return mgr;
        }
    }

    public CollectStarCondition(WinCondition config) : base(config)
    {
    }

    public override void OnEditAdd()
    {
        base.OnEditAdd();
        CreateNewStar(default, (nBev) =>
        {
            InputHandlerManager.Inst.SelectEntity(nBev.entity);
            RefreshProgressUI();
        });
    }

    public void OnEditSetMultiStar(int num)
    {
        OnSetStarCount(num);
    }

    public override void OnEditIconSelect()
    {
        if (Mgr.GetCurStarCount() >= Mgr.MaxNum)
        {
            TipPanel.ShowToast(Mgr.MaxHint);
            return;
        }

        CreateNewStar(default, (nBev) =>
        {
            InputHandlerManager.Inst.SelectEntity(nBev.entity);
            RefreshProgressUI();
        });
    }

    public override void OnEditRemove()
    {
        base.OnEditRemove();
        RemoveAllStar(() =>
        {
            InputHandlerManager.Inst.UnSelectAll();
            GizmoManager.Inst.CurGizmoCtrl.DisableGizmo();
            RefreshProgressUI();
        });
    }

    public override bool IsCanDeleteProp(NodeBaseBehaviour bev)
    {
        if (bev is not CollectStarBehaviour)
            return true;

        if (Mgr.GetCurStarCount() > Mgr.MinNum)
        {
            RemoveStar(bev, () => { InputHandlerManager.Inst.UnSelectAll(); });
            return false;
        }

        return true;
    }

    public override void OnStart()
    {
        base.OnStart();
        Mgr.OnPassLevelStart();
        Mgr.ResetCollectProgress();
    }

    public override void OnStop()
    {
        base.OnStop();
        Mgr.OnPassLevelStop();
    }

    public override void OnSuccess()
    {
        base.OnSuccess();
    }

    public override void OnFailed()
    {
        base.OnFailed();
    }

    public override WinConditionStatus DoResultJudging()
    {
        var allStar = Mgr.GetCollectStarList();
        if (allStar is { Count: > 0 })
        {
            var winCount = 0;
            foreach (var star in allStar)
            {
                if (star != null && star.entity != null && star.entity.GetComp<CollectStarComponent>().IsCollect)
                {
                    winCount++;
                }
            }

            LoggerUtils.Log($"CollectAllStarManager DoStarCollected winCount:{winCount}");
            if (winCount == Mgr.GetCurStarCount())
            {
                return WinConditionStatus.Succeed;
            }
        }

        return WinConditionStatus.Running;
    }


    #region 接收编辑输入，创建/删除星星

    public void OnSetStarCount(int countInput)
    {
        var delta = countInput - Mgr.GetCurStarCount();
        LoggerUtils.Log($"OnSetStarCount--->{countInput}, delta:{delta}");
        switch (delta)
        {
            case > 0:
                BatchCreateNewStar(delta);
                break;
            case < 0:
                BatchRemoveStar(Math.Abs(delta));
                break;
        }
        
        RefreshProgressUI();
    }

    private void BatchCreateNewStar(int addCount)
    {
        var lastCreatePos = Vector3.zero;
        for (var i = 0; i < addCount; i++)
        {
            if (i != 0) lastCreatePos += _cloneOffset;
            CreateNewStar(lastCreatePos, (nBev) => { lastCreatePos = nBev.transform.position; });
        }
    }

    private void CreateNewStar(Vector3 createPos = default, Action<NodeBaseBehaviour> callback = null)
    {
        var opReason = GamePropNodeManager.Inst.TryCreateInEdit(PropConfigId, out var nBev, createPos);
        if (opReason == GameGlobalEnum.NodeOpReason.CreateSuccess)
        {
            InputHandlerManager.Inst.SelectEntity(nBev.entity);
            callback?.Invoke(nBev);
        }
    }

    private void BatchRemoveStar(int subCount)
    {
        var allStar = Mgr.GetCollectStarList();
        // 删倒数subCount个星星
        var deleteList = allStar.GetRange(allStar.Count - subCount, subCount);
        foreach (var bev in deleteList)
        {
            if (bev != null && bev.entity != null && bev.entity.GetComp<CollectStarComponent>().Id != 0)
            {
                RemoveStar(bev);
            }
        }
    }

    public void RemoveStar(NodeBaseBehaviour bev, Action callback = null)
    {
        if (bev == null) return;
        GamePropNodeManager.Inst.DestroyNodeToSecondCache(bev.gameObject);
        callback?.Invoke();
    }

    public void RemoveAllStar(Action callback = null)
    {
        for (int i = Mgr.GetCurStarCount() - 1; i >= 0; i--)
        {
            var bev = Mgr.GetCollectStarBev(i);
            if (bev)
            {
                RemoveStar(bev);
            }
        }

        callback?.Invoke();
    }

    #endregion

    #region UI控制

    private void RefreshProgressUI()
    {
        if (UIManager.Inst.TryFindPanel<GamePlayPanel>(WindowId.GuestWindow, PanelId.GamePlayPanel, out var panel)) 
        {
            panel.RefreshCollectStarProgress();
        }
    }

    #endregion
}