/**
 * @ Author: Jun Zhou
 * @ Create Time: 2023-07-18 16:54:15
 * @ Modified by: Jun Zhou
 * @ Modified time: 2023-08-07 11:48:26
 * @ Description: 道具管理器的全局管理类
 */
 
using System;
using System.Linq;
using System.Collections.Generic;
using Game.Avatar;
using Game.ECS;
using GameData.Config;
using UnityEngine;

namespace Game.Base
{
    public class GlobalNodeManager : GameInstance<GlobalNodeManager>
    {
        private static Dictionary<Type, INodeManager> managerInstDict = new Dictionary<Type, INodeManager>();

        public T Create<T>() where T : INodeManager
        {
            Type mType = typeof(T);
            return (T)Create(mType);
        }

        public INodeManager Create(Type mType)
        {

            if (managerInstDict.ContainsKey(mType))
            {
                return managerInstDict[mType];
            }
            var managerInst = Activator.CreateInstance(mType) as INodeManager;
            managerInstDict.Add(mType, managerInst);
            return managerInst;
        }

        public INodeManager Get(NodeModelType modelType)
        {
            var mType = GameTypeRegister.Inst.GetManagerType(modelType);
            if (mType == null)
            {
                Debug.Log($"[Game.Base] Get NodeManager Fail(NodeModelType = {modelType}).");
                return null;   
            }
            return Get(mType);
        }

        public INodeManager Get(Type mType)
        {
            if (managerInstDict.TryGetValue(mType, out var managerInst))
            {
                return managerInst;
            }

            return Create(mType);
        }

        public T Get<T>() where T : INodeManager, new ()
        {
            Type mType = typeof(T);
            return (T)Create(mType);
        }

        public bool Has<T>()
        {
            Type mType = typeof(T);
            return managerInstDict.ContainsKey(mType);
        }

        public List<INodeManager> GetAll()
        {
            return managerInstDict.Values.ToList();
        }

        public void OnNodeCombine(SceneEntity sceneEntity)
        {
            foreach (var manager in managerInstDict.Values)
            {
                if (manager is ICombine)
                {
                    var combineManager = manager as ICombine;
                    combineManager.OnCombine(sceneEntity);
                }
            }
        }

        public override void Release()
        {
            foreach (var manager in managerInstDict.Values)
            {
                if (manager is INodeManager nodeManager)
                {
                    nodeManager.Release();
                }
            }
            managerInstDict.Clear();
            base.Release();
        }

        #region 编辑器关联Manager

        public INodeEdit GetNodeEditManager(SceneEntity entity)
        {
            var comp = entity.GetComp<GameObjectComponent>();
            if (comp == null)
            {
                Debug.Log($"entity is has GameObjectComponent,{entity.Id}");
                return null;   
            }

            var modelType = entity.GetComp<GameObjectComponent>().ModelType;
            var mType = GameTypeRegister.Inst.GetManagerType(modelType);
            if (mType == null)
            {
                Debug.Log($"[Game.Base] GetNodeEditManager Fail(NodeModelType = {modelType}).");
                return null;   
            }
            
            if (managerInstDict.TryGetValue(mType, out var managerInst))
            {
                var inters = managerInst.GetType().GetInterfaces();
                if (inters.Contains(typeof(INodeEdit)))
                {
                    return (INodeEdit)managerInst;
                }
            }

            return null;
        }

        #endregion

        #region 注册Trigger

        public void RegisterAvatarTrigger()
        {
            var avtTrigger = AvatarController.Inst.SelfAvatarTrigger;
            avtTrigger.OnAvatarTrigEnter.RemoveAllListeners();
            avtTrigger.OnAvatarTrigExit.RemoveAllListeners();
            avtTrigger.OnAvatarColliderEnter.RemoveAllListeners();
            avtTrigger.OnAvatarTrigEnter.AddListener(OnAvatarTrigNodeEnter);
            avtTrigger.OnAvatarTrigExit.AddListener(OnAvatarTrigNodeExit);
            avtTrigger.OnAvatarColliderEnter.AddListener(OnAvatarColliderEnter);
        }

        private void OnAvatarTrigNodeEnter(Collider collider)
        {
            if (collider == null) return;
            var nodeBev = collider.GetComponentInParent<NodeBaseBehaviour>();
            if (nodeBev) nodeBev.OnTrigEnter();
        }
        
        private void OnAvatarTrigNodeExit(Collider collider)
        {
            if (collider == null) return;
            var nodeBev = collider.GetComponentInParent<NodeBaseBehaviour>();
            if (nodeBev) nodeBev.OnTrigExit();
        }

        private void OnAvatarColliderEnter(Collider collider)
        {
            if (collider == null) return;
            var nodeBev = collider.GetComponentInParent<NodeBaseBehaviour>();
            if (nodeBev) nodeBev.OnColliderEnter();
        }

        #endregion
    }
}