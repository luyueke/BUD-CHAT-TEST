using Es;
using Game.Base;

public enum WinConditionStatus
{
    NotStart,
    Running,
    Succeed,
    Failed,
}

public abstract class WinConditionBase
{
    public WinCondition Config;
    public WinConditionStatus Status = WinConditionStatus.NotStart;

    protected WinConditionBase(WinCondition config)
    {
        Config = config;
    }

    /// <summary>
    /// 编辑模式设置条件后
    /// </summary>
    public virtual void OnEditAdd()
    {
        Status = WinConditionStatus.NotStart;
    }

    /// <summary>
    /// 编辑模式取消条件设置
    /// </summary>
    public virtual void OnEditRemove()
    {
        OnStop();
    }
    
    /// <summary>
    /// 编辑模式选中Icon
    /// </summary>
    public virtual void OnEditIconSelect()
    {
    }

    /// <summary>
    /// 删除通关道具时，接管删除操作
    /// </summary>
    /// <returns></returns>
    public virtual bool IsCanDeleteProp(NodeBaseBehaviour bev)
    {
        return true;
    }

    /// <summary>
    /// 开始执行通关条件
    /// </summary>
    public virtual void OnStart()
    {
        Status = WinConditionStatus.Running;
    }

    /// <summary>
    /// 结束执行通关条件
    /// </summary>
    public virtual void OnStop()
    {
        Status = WinConditionStatus.NotStart;
    }
    
    /// <summary>
    /// 条件达成
    /// </summary>
    public virtual void OnSuccess()
    {
        Status = WinConditionStatus.Succeed;
    }
    
    /// <summary>
    /// 条件达成失败
    /// </summary>
    public virtual void OnFailed()
    {
        Status = WinConditionStatus.Failed;
    }

    /// <summary>
    /// 条件是否已经达成
    /// </summary>
    /// <returns></returns>
    public bool IsSuccess()
    {
        return Status == WinConditionStatus.Succeed;
    }
    
    /// <summary>
    /// 进行条件是否达成判断
    /// </summary>
    public abstract WinConditionStatus DoResultJudging();
}