using System;
using System.Collections.Generic;
using Game.ECS;
using UnityEngine;

namespace Game.Base
{
    public abstract class INodeCreater<T> : INodeCreater
    {
        protected T createData;

        public virtual void BindData(T data)
        {
            createData = data;
        }
    }

    public abstract class INodeCreater
    {
        protected EcsSceneWorld sceneWorld;
        public abstract NodeBaseBehaviour CreateAssetBehaviour(Type behvT); // 创建Behaviour
        public abstract void RefreshAssetGo(GameObject assetGo, GameObject parentGo); // 根据父节点刷新一下节点
        public abstract SceneEntity CreateEntity(); // 创建Entity实体

        public void Init(EcsSceneWorld world)
        {
            sceneWorld = world;
        }
    }

    public class SceneNodeFactory
    {
        EcsSceneWorld sceneWorld;
        private Dictionary<string, INodeCreater> creaters = new Dictionary<string, INodeCreater>();

        public SceneNodeFactory(EcsSceneWorld world)
        {
            sceneWorld = world;
        }

        public INodeCreater<S> GetCreater<T, S>(S data) where T: INodeCreater<S>,new()
        {
            string className = typeof(T).Name;
            if (!creaters.ContainsKey(className))
            {
                T nCreater = new T();
                nCreater.Init(sceneWorld);
                creaters.Add(className, nCreater);
            }
            var creater = (T)creaters[className];
            creater.BindData(data);
            return creater;
        }
    }
}