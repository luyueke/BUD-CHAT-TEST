using System.Collections;
using System.Collections.Generic;
using Game.Base;
using Game.ECS;
using Game.Props.PropsComponents;
using UnityEngine;

public class LockHideManager : GameInstance<LockHideManager>, IModeManager,INodeLife
{
    private const string TAG = "LockHideManager";
    private List<SceneEntity> hideList = new List<SceneEntity>();
    private List<SceneEntity> lockList = new List<SceneEntity>();


    public bool GetLockState(SceneEntity entity)
    {
        if (entity == null)
        {
            return false;
        }
        return lockList.Contains(entity);
    }


    public void SetLockState(SceneEntity entity,bool value)
    {
        if(entity == null) return;
        if (value == true)
        {
            if (!lockList.Contains(entity))
            {
                lockList.Add(entity);
            }
        }
        else
        {
            if (lockList.Contains(entity))
            {
                lockList.Remove(entity);
            }
        }
    }

    public void HideNode(SceneEntity entity)
    {
        if(entity == null) return;
        SetNodeVisibility(entity,false);
    }

    public void SetNodesVisibility(List<SceneEntity> list,bool isShow)
    {
        if(list == null || list.Count == 0) return;
        foreach (var entity in list)
        {
            SetNodeVisibility(entity,isShow);
        }
    }

    public void SetNodeVisibility(SceneEntity entity, bool isShow)
    {
        if(entity == null) return;
        if (isShow)
        {
            if (hideList.Contains(entity))
            {
                hideList.Remove(entity);
            }
        }
        else
        {
            if (!hideList.Contains(entity))
            {
                hideList.Add(entity);

            }
        }
        
        var curGo = entity.GetViewGo();
        curGo.SetActive(isShow);
    }

    private void SetAllNodeVisibility(bool isShow)
    {
        foreach (var entity in hideList)
        {
            var nodeObj = entity.GetViewGo();
            if (nodeObj == null)
            {
                continue;
            }
            //在显示的时候，如果有ActiveCtrComponent，交由ActiveCtrComponent管理
            if (isShow == true && entity.HasComp<ActiveCtrComponent>())
            {
                continue;
            }
            
            //TODO @jaywill:判断是否特殊逻辑：如被开关控制的，或者游玩模式下不可见的

            nodeObj.SetActive(isShow);
        }
    }

    public void ClearHideList()
    {
        SetAllNodeVisibility(true);
        hideList.Clear();
    }
    
    public List<SceneEntity> GetHideList()
    {
        List<SceneEntity> result = new List<SceneEntity>();
        for (int i = 0; i < hideList.Count; i++)
        {
            result.Add(hideList[i]);
        }

        return result;
    }

    public void OnEdit()
    {
        LoggerUtils.Log($"{TAG} OnEdit");
        SetAllNodeVisibility(false);
    }

    public void OnPlay()
    {
        LoggerUtils.Log($"{TAG} OnPlay");
        SetAllNodeVisibility(true);
    }

    public void OnGuest()
    {
        
    }

    public void OnCloneNode(NodeBaseBehaviour oldBehaviour, NodeBaseBehaviour newBehaviour)
    {
        
    }

    public void OnCreateNode(NodeBaseBehaviour nodeBehaviour, NodeCreateType createType)
    {
        
    }

    public void OnRemoveNode(NodeBaseBehaviour nodeBehaviour)
    {
        var entity = nodeBehaviour.entity;
        if (entity == null) return;
        if (lockList.Contains(entity))
        {
            lockList.Remove(entity);
        }

        if (hideList.Contains(entity))
        {
            hideList.Remove(entity);
        }
    }

    public void OnRevertNode(NodeBaseBehaviour nodeBehaviour)
    {
        
    }
}
