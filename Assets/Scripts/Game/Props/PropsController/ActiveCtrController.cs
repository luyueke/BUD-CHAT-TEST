using System.Collections;
using System.Collections.Generic;
using Game.Base;
using Game.ECS;
using Game.Props.PropsComponents;
using Game.Props.PropsController;
using UnityEngine;

public class ActiveCtrController : BasePropController<ActiveCtrController>, IAutoInit
{
    protected override bool Filter(NodeBaseBehaviour nodeBehaviour)
    {
        return nodeBehaviour.entity.HasComp<ActiveCtrComponent>();
    }

    public void Init()
    {
        
    }
    
    
    /// <summary>
    /// 由开关等控制调用
    /// </summary>
    public void SwitchEntity(SceneEntity entity)
    {
        //TODO:后续可以其他过滤条件，可以在这里添加
        var targetGo = entity.GetViewGo();
        targetGo.SetActive(!targetGo.activeSelf);
    }
    
    
    //被组合时，清除该属性
    public override void OnCombine(SceneEntity entity)
    {
        if (entity != null && entity.HasComp<ActiveCtrComponent>())
        {
            var uid = entity.GetComp<GameObjectComponent>().Uid;
            var nodeBehav = entity.GetViewGo().GetComponent<NodeBaseBehaviour>();
            if (nodeBaseBehaviours.Contains(nodeBehav))
            {
                nodeBaseBehaviours.Remove(nodeBehav);
            }
            entity.RemoveComp<ActiveCtrComponent>();
        }
        
    }

    public override void OnEdit()
    {
        base.OnEdit();
        EnterEditMode();
    }

    public override void OnPlay()
    {
        base.OnPlay();
        EnterPlayMode();
    }

    public override void OnGuest()
    {
        base.OnGuest();
        EnterPlayMode();
    }

    private void EnterEditMode()
    {
        for (int i = 0; i < nodeBaseBehaviours.Count; i++)
        {
            var nodeBehaviour = nodeBaseBehaviours[i];
            SetEditActive(nodeBehaviour,true);
        }
    }

    public void SetEditActive(SceneEntity entity,bool isActive)
    {
        if (isActive == true)
        {
            if (LockHideManager.Inst.GetLockState(entity))
            {
                return;
            }
        }
        entity.GetViewGo().SetActive(isActive);
    }

    public void SetEditActive(NodeBaseBehaviour nodeBehaviour, bool isActive)
    {
        SetEditActive(nodeBehaviour.entity, isActive);
    }

    private void EnterPlayMode()
    {
        for (int i = 0; i < nodeBaseBehaviours.Count; i++)
        {
            var nodeBehaviour = nodeBaseBehaviours[i];
            ActiveCtrComponent activeComp = nodeBehaviour.entity.GetComp<ActiveCtrComponent>();
            if (activeComp == null)
            {
                continue;
            }

            GameObject bindGo = nodeBehaviour.entity.GetViewGo();
            if (activeComp.DefaultHide == 1)
            {
                bindGo.SetActive(false);
            }
            else
            {
                bindGo.SetActive(true);
            }
        }
        
    }
}
