using Es;
using Game.Base;
using Game.Config;
using Game.Props.PropsBehaviours;
using Game.Props.PropsManagers;
using UI.Manager;

/// <summary>
/// 通关条件：触碰到旗子
/// </summary>
public class ReachEndFlagCondition : WinConditionBase
{
    private WinReachEngFlagBehaviour _behaviour;
    private const string PropConfigId = "20100027";

    public ReachEndFlagCondition(WinCondition config) : base(config)
    {
    }

    //todo:fsc 进模板图后设置
    public static void SetInitialFlagPosition(UnityEngine.Vector3 pos)
    {
        // WinReachEngFlagBehaviour behav = null;
        // var nodes = SceneSystem.Inst.FilterNodeBehaviours<WinReachEngFlagBehaviour>(SceneBuilder.Inst.allControllerBehaviours);
        // if (nodes.Count > 0)
        // {
        //     behav = nodes[0];
        // }
        // if (behav)
        // {
        //     behav.transform.position = pos;
        // }
    }

    public override void OnEditAdd()
    {
        base.OnEditAdd();
        NodeBaseBehaviour nBehav;
        var opReason = GamePropNodeManager.Inst.TryCreateInEdit(PropConfigId, out nBehav);
        if (opReason == GameGlobalEnum.NodeOpReason.CreateSuccess)
        {
            InputHandlerManager.Inst.SelectEntity(nBehav.entity);
        }
    }

    public override void OnEditRemove()
    {
        base.OnEditRemove();
        InitBevFromSceneParse();

        if (_behaviour == null) return;
        var reason = GamePropNodeManager.Inst.TryDeleteInEdit(_behaviour.gameObject);
        if (reason == GameGlobalEnum.NodeOpReason.RemoveSuccess)
        {
            InputHandlerManager.Inst.UnSelectAll();
            GizmoManager.Inst.CurGizmoCtrl.DisableGizmo();
        }
    }

    private void InitBevFromSceneParse()
    {
        if (_behaviour != null) return;
        var mgr = GlobalNodeManager.Inst.Get<WinReachEngFlagManager>();
        _behaviour = mgr.GetReachEngFlagBehaviour();
    }

    public override void OnStart()
    {
        InitBevFromSceneParse();
        if (_behaviour == null) return;
        base.OnStart();
        _behaviour.OnReset();
        _behaviour.IsStartRunning = true;
    }

    public override void OnStop()
    {
        InitBevFromSceneParse();
        if (_behaviour == null) return;
        _behaviour.IsStartRunning = false;
        _behaviour.OnReset();
        base.OnStop();
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
        if (_behaviour != null && _behaviour.IsReached)
        {
            Status = WinConditionStatus.Succeed;
        }

        return Status;
    }
}