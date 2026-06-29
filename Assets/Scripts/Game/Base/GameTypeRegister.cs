/**
 * @ Author: Jun Zhou
 * @ Create Time: 2023-07-17 15:56:24
 * @ Modified by: Jun Zhou
 * @ Modified time: 2023-07-18 15:46:52
 * @ Description: 游戏配置的注册器
 */
using Game.Generated;
using GameData.Config;
using System;
using System.Collections.Generic;
using Game.Scene.ModeController;
using UnityEngine;

namespace Game.Base
{
    
    public class GameTypeRegister: GameInstance<GameTypeRegister>
    {
        private EntityComponentRegister componentRegister;
        private EntityManagerRegister managerRegister;
        
        public Dictionary<Type,NodeComponentId> ComponentKeyDict = new Dictionary<Type,NodeComponentId>();
        public GameTypeRegister()
        {
            componentRegister = new EntityComponentRegister();
            managerRegister = new EntityManagerRegister();
            
            ComponentKeyDict.Clear();
            foreach (var compKT in componentRegister.Dict)
            {
                ComponentKeyDict.Add(compKT.Value,compKT.Key);
            }
        }

        public Type GetComponentType(NodeComponentId componentId)
        {
            if (componentRegister.Dict.TryGetValue(componentId, out var eType))
            {
                return eType;
            }

            return null;
        }

        public NodeComponentId GetComponentId(Type type)
        {
            if (ComponentKeyDict.TryGetValue(type, out var compId))
            {
                return compId;
            }
            return default;

   
        }

        public Type GetManagerType(NodeModelType nodeModelType)
        {
            if (managerRegister.Dict.TryGetValue(nodeModelType, out var eType))
            {
                return eType;
            }

            return null;
        }
    }
}