using System.Collections.Generic;
using Game.Base;
using Game.Config;
using Game.ECS;
using Game.Props.PropsComponents;
using Game.Props.PropsManagers;


namespace Game.Props.PropsController
{
    /// <summary>
    /// 道具被开关控制的属性管理器
    /// </summary>
    public class SwitchCtrController : BasePropController<SwitchCtrController>, IAutoInit
    {
        private const string TAG = "SwitchCtrController";
        //被开关控制部分的实体，按照不同类型分类
        private Dictionary<GameGlobalEnum.PropControlType, Dictionary<uint, SceneEntity>> controlTypeDicts = new Dictionary<GameGlobalEnum.PropControlType, Dictionary<uint, SceneEntity>>();
        
        //当前控制的类型
        private readonly List<GameGlobalEnum.PropControlType> controlTypes = new List<GameGlobalEnum.PropControlType>
        {
            GameGlobalEnum.PropControlType.Visible,
            GameGlobalEnum.PropControlType.Movement,
            GameGlobalEnum.PropControlType.Anim,
            // GameGlobalEnum.PropControlType.Sound,
            // GameGlobalEnum.PropControlType.Firework
        };
        
        
        public void Init()
        {
            LoggerUtils.Log($"{TAG}.Init");
            foreach (var controlType in controlTypes)
            {
                controlTypeDicts[controlType] = new Dictionary<uint, SceneEntity>();
            }
        }
        
        protected override bool Filter(NodeBaseBehaviour nodeBehaviour)
        {
            return nodeBehaviour.entity.HasComp<SwitchCtrComponent>();
        }
        
        #region 绑定与解除绑定
        private void RemoveControlledEntity(SceneEntity entity)
        {
            if (!entity.HasComp<SwitchCtrComponent>()) return;
            GameObjectComponent goCmp = entity.GetComp<GameObjectComponent>();
            
            //从关联的开关中移除
            GlobalNodeManager.Inst.Get<SwitchBtnManager>().RemoveControlledId(goCmp.Uid);
            
            //从管理器移除
            foreach (var ctrDict in controlTypeDicts.Values)
            {
                ctrDict.Remove(goCmp.Uid);
            }
        }
        
        private Dictionary<uint, SceneEntity> GetSwitchCtrDict(GameGlobalEnum.PropControlType controlType)
        {
            if (controlTypeDicts.TryGetValue(controlType, out var ctrDict))
            {
                return ctrDict;
            }
            return null;
        }
        
        /// <summary>
        /// 根据controlType从SwitchCtrComponent获取开关列表
        /// </summary>
        /// <param name="ctrComp"></param>
        /// <param name="controlType"></param>
        /// <returns></returns>
        private List<uint> GetSwitchListFromCtrComp(SwitchCtrComponent ctrComp,GameGlobalEnum.PropControlType controlType)
        {
            if (ctrComp.SrcTypeDicts.ContainsKey(controlType))
            {
                return ctrComp.SrcTypeDicts[controlType];
            }

            return null;
        }

        /// <summary>
        /// 添加entity到本Manager
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="controlType"></param>
        private void AddControlEntityToManager(SceneEntity entity,GameGlobalEnum.PropControlType controlType)
        {
            if(!entity.HasComp<SwitchCtrComponent>()) return;
            uint uid = entity.GetComp<GameObjectComponent>().Uid;
            var ctrDict = GetSwitchCtrDict(controlType);
            if (ctrDict!=null && !ctrDict.ContainsKey(uid))
            {
                ctrDict.Add(uid, entity);
                LoggerUtils.Log($"[AddControlEntityToManager] add to {controlType} Dict ,uid=>{uid}");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="controlType"></param>
        private void RemoveControlEntityFromManager(SceneEntity entity,GameGlobalEnum.PropControlType controlType)
        {
            if(!entity.HasComp<SwitchCtrComponent>()) return;
            var ctrComp = entity.GetComp<SwitchCtrComponent>();
            uint uid = entity.GetComp<GameObjectComponent>().Uid;
            var ctrDict = GetSwitchCtrDict(controlType);
            if (ctrDict != null)
            {
                //当该物体，关于开关的某个属性已经为空，才从管理器移除，如Visible跟所有开关都无关联，则从管理器的Visible中移除
                if (ctrComp.SrcTypeDicts[controlType] == null || ctrComp.SrcTypeDicts[controlType].Count <= 0)
                {
                    ctrDict.Remove(uid);
                }
            }
        }
        
        
        //没有被控制时,去除 SwitchCtrComponent
        private void CheckRemoveCtrComp(SceneEntity curEntity)
        {
            if (curEntity.HasComp<SwitchCtrComponent>())
            {
                var sComp = curEntity.GetComp<SwitchCtrComponent>();
                int totalCount = 0;
                foreach (var srcUids in sComp.SrcTypeDicts.Values)
                {
                    if (srcUids != null && srcUids.Count > 0)
                    {
                        totalCount += srcUids.Count;
                    }
                }
                if (totalCount <= 0)
                {
                    curEntity.RemoveComp<SwitchCtrComponent>();
                    LoggerUtils.Log("not controll by Switch,remove SwitchCtrComponent");
                }
            }
        }
        
        private void BindSwitchIds(SceneEntity entity, GameGlobalEnum.PropControlType controlType)
        {
            SwitchCtrComponent ctrComp = entity.GetComp<SwitchCtrComponent>();
            uint ctrId = entity.GetComp<GameObjectComponent>().Uid;
    
            var switchIds = GetSwitchListFromCtrComp(ctrComp, controlType);
            if (switchIds != null)
            {
                for (int i = 0; i < switchIds.Count; i++)
                {
                    uint switchId = switchIds[i];
                    GlobalNodeManager.Inst.Get<SwitchBtnManager>().AddCtrIdToSwitchComp(ctrId, switchId, controlType);
                }
            }
        }
        
        /// <summary>
        /// 将SwitchCtrComponent节点绑定到对应开关中
        /// </summary>
        /// <param name="nodeBehaviour"></param>
        private void BindToSwitch(NodeBaseBehaviour nodeBehaviour)
        {
            SceneEntity ctrEntity = nodeBehaviour.entity;
            foreach (var controlType in controlTypes)
            {
                BindSwitchIds(ctrEntity, controlType);
            }
        }
      
        
        /// <summary>
        /// 将SwitchCtrComponent节点绑定到管理器中
        /// </summary>
        /// <param name="nodeBehaviour"></param>
        private void BindToManager(NodeBaseBehaviour nodeBehaviour)
        {
            SceneEntity ctrEntity = nodeBehaviour.entity;
            SwitchCtrComponent ctrComp = ctrEntity.GetComp<SwitchCtrComponent>();

            foreach (var controlType in controlTypes)
            {
                var srcUids = GetSwitchListFromCtrComp(ctrComp, controlType);
                if (srcUids !=null && srcUids.Count > 0)
                {
                    AddControlEntityToManager(ctrEntity, controlType);
                }
            }
        }
        
        /// <summary>
        /// 将开关id关联到被控制物体上
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="switchUid"></param>
        /// <param name="controlType"></param>
        private void AddSwitchIdToCtrComp(SceneEntity entity, uint switchUid,GameGlobalEnum.PropControlType controlType)
        {
            var ctrComp = entity.GetOrAddComp<SwitchCtrComponent>();

            var uidList = GetSwitchListFromCtrComp(ctrComp,controlType);
            if (uidList == null)
            {
                List<uint> srcUids = new List<uint>();
                ctrComp.SrcTypeDicts.Add(controlType,srcUids);
                uidList = ctrComp.SrcTypeDicts[controlType];
            }
            
            uidList.Add(switchUid);
            LoggerUtils.Log($"[AddMoveCtrEntity] add to {controlType} Dict ,uid=>{switchUid}");
            
        }
		
        /// <summary>
        /// 从控制物体上移除开关id
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="switchId"></param>
        /// <param name="controlType"></param>
        private void RemoveSwitchIdFromCtrComp(SceneEntity entity, uint switchId,GameGlobalEnum.PropControlType controlType)
        {
            if (!entity.HasComp<SwitchCtrComponent>())
            {
                return;
            }
            var ctrComp = entity.GetComp<SwitchCtrComponent>();
            var uidList = GetSwitchListFromCtrComp(ctrComp,controlType);
            if (uidList !=null && uidList.Contains(switchId))
            {
                uidList.Remove(switchId);
            }
        }
        
        
        public void BindEntity(SceneEntity entity, uint switchId,GameGlobalEnum.PropControlType controlType)
        {
            AddSwitchIdToCtrComp(entity,switchId,controlType);
            AddControlEntityToManager(entity,controlType);
        }

        public void UnbindEntity(SceneEntity entity, uint switchId,GameGlobalEnum.PropControlType controlType)
        {
            RemoveSwitchIdFromCtrComp(entity,switchId,controlType);
            RemoveControlEntityFromManager(entity,controlType);
            CheckRemoveCtrComp(entity);
        }

        public void UnbindEntityByUid(uint crtId, uint switchId,GameGlobalEnum.PropControlType controlType)
        {
            var ctrDict = GetSwitchCtrDict(controlType);
            if (ctrDict != null && ctrDict.ContainsKey(crtId))
            {
                var entity = ctrDict[crtId];
                UnbindEntity(entity,switchId,controlType);
            }
        }

        #endregion


        #region 点击开关的效果

        public void OnHandleControl(uint uid,GameGlobalEnum.PropControlType controlType)
        {
            var ctrDict = GetSwitchCtrDict(controlType);
            if (ctrDict != null && ctrDict.ContainsKey(uid))
            {
                var entity = ctrDict[uid];
                switch (controlType)
                {
                    case GameGlobalEnum.PropControlType.Visible:
                        OnHandleVisible(entity);
                        break;
                    case GameGlobalEnum.PropControlType.Movement:
                        OnHandleMove(entity);
                        break;
                    case GameGlobalEnum.PropControlType.Anim:
                        OnHandleAnim(entity);
                        break;
                    case GameGlobalEnum.PropControlType.Sound:
                        OnHandleSound(entity);
                        break;
                    case GameGlobalEnum.PropControlType.Firework:
                        OnHandleFirework(entity);
                        break;
                }
            }
        }

        public void OnHandleVisible(SceneEntity entity)
        {
            LoggerUtils.Log($"{TAG} OnHandleVisible uid:{entity.GetComp<GameObjectComponent>().Uid} name: {entity.GetViewGo().name}");
            ActiveCtrController.Inst.SwitchEntity(entity);
        }

        public void OnHandleMove(SceneEntity entity)
        {
            LoggerUtils.Log($"{TAG} OnHandleMove uid:{entity.GetComp<GameObjectComponent>().Uid} name: {entity.GetViewGo().name}");
            MovementController.Inst.SwitchEntity(entity);
        }

        public void OnHandleSound(SceneEntity entity)
        {
            LoggerUtils.Log($"{TAG} OnHandleSound uid:{entity.GetComp<GameObjectComponent>().Uid} name: {entity.GetViewGo().name}");
        }

        public void OnHandleAnim(SceneEntity entity)
        {
            LoggerUtils.Log($"{TAG} OnHandleAnim uid:{entity.GetComp<GameObjectComponent>().Uid} name: {entity.GetViewGo().name}");
            RPAnimController.Inst.SwitchEntity(entity);
        }

        public void OnHandleFirework(SceneEntity entity)
        {
            LoggerUtils.Log($"{TAG} OnHandleFirework uid:{entity.GetComp<GameObjectComponent>().Uid} name: {entity.GetViewGo().name}");
        }

        #endregion

        public override void OnCreateNode(NodeBaseBehaviour nodeBehaviour, NodeCreateType createType)
        {
            base.OnCreateNode(nodeBehaviour, createType);
            if (Filter(nodeBehaviour))
            {
                BindToManager(nodeBehaviour);
            }
        }

        public override void OnCloneNode(NodeBaseBehaviour oldBehaviour, NodeBaseBehaviour newBehaviour)
        {
            base.OnCloneNode(oldBehaviour, newBehaviour);
            if (Filter(newBehaviour))
            {
                LoggerUtils.Log("SwitchCtrController OnCloneNode entity.uid:"+newBehaviour.entity.GetComp<GameObjectComponent>().Uid);
                BindToSwitch(newBehaviour);
                BindToManager(newBehaviour);
            }
        }
        
        public override void OnRevertNode(NodeBaseBehaviour nodeBehaviour)
        {
            base.OnRevertNode(nodeBehaviour);
            if (Filter(nodeBehaviour))
            {
                BindToSwitch(nodeBehaviour);
                BindToManager(nodeBehaviour);
            }
        }
        
        public override void OnRemoveNode(NodeBaseBehaviour nodeBehaviour)
        {
            base.OnRemoveNode(nodeBehaviour);
            if (Filter(nodeBehaviour))
            {
                //将该道具的uid从关联的开关上移除
                RemoveControlledEntity(nodeBehaviour.entity);
            }
        }

        public override void OnEdit()
        {
            base.OnEdit();
            var visibleCtrDict= GetSwitchCtrDict(GameGlobalEnum.PropControlType.Visible);
            if (visibleCtrDict != null && visibleCtrDict.Count > 0)
            {
                foreach (var entity in visibleCtrDict.Values)
                {
                    ActiveCtrController.Inst.SetEditActive(entity,true);
                }
            }
        }
    }
}
