/**
 * @ Author: Jun Zhou
 * @ Create Time: 2023-07-19 10:42:27
 * @ Modified by: Jun Zhou
 * @ Modified time: 2023-08-31 15:00:20
 * @ Description: SceneEntity 扩展
 */

using System.Collections.Generic;
using Es;
using Game.Base;
using Game.Utils;
using UnityEngine;

namespace Game.ECS
{
    public static class SceneEntityEx
    {
        public static GameObject GetViewGo(this SceneEntity entity)
        {
            var gameObjectComponent = entity.GetComp<GameObjectComponent>();
            if (gameObjectComponent == null) return null;
            return gameObjectComponent.BindGo;
        }

        public static void SetViewGo(this SceneEntity entity, GameObject go)
        {
            var gameObjectComponent = entity.GetComp<GameObjectComponent>();
            gameObjectComponent.BindGo = go;
        }

        public static bool HasViewGo(this SceneEntity entity)
        {
            return entity.GetViewGo() != null;
        }

        public static NodeBaseBehaviour GetNodeBaseBehaviour(this SceneEntity entity)
        {
            var bindGo = GetViewGo(entity);
            if (bindGo == null) return null;
            var bev = bindGo.GetComponent<NodeBaseBehaviour>();
            return bev;
        }

        public static T GetBehaviour<T>(this SceneEntity entity) where T : NodeBaseBehaviour
        {
            var bev = GetViewGo(entity).GetComponent<T>();
            return bev;
        }

        public static GameObjectComponent GetGameObjectComponent(this SceneEntity entity)
        {
            return entity.GetComp<GameObjectComponent>();
        }

        public static void ClearUid(this SceneEntity entity)
        {
            var goComponent =entity.GetGameObjectComponent();
            goComponent.Uid = 0;
        }

        /// <summary>
        /// 读取道具操作配置表
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static GamePropEditOperation GetEditOperationConfig(this SceneEntity entity)
        {
            if (entity == null) return null;
            var gCmp = entity.GetGameObjectComponent();
            var config = GamePropDataHelper.GetPropDataByID(gCmp.PropId);
            if (config == null) return null;
            var propEditCfg = Es.DataTables.GetGamePropEditOperation(config.HandleType);
            return propEditCfg;
        }

        /// <summary>
        /// 读取道具配置表
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static GamePropData GetPropConfig(this SceneEntity entity)
        {
            if (entity == null) return null;
            var gCmp = entity.GetGameObjectComponent();
            var propCfg = Es.DataTables.GetGamePropData(gCmp.PropId);
            return propCfg;
        }

        public static Bounds GetBounds(this SceneEntity entity, bool isReset = false) {
            return entity.GetViewGo().GetBounds(isReset);
        }

        public static Bounds GetBounds(this List<SceneEntity> entities) {
            var center = Vector3.zero;

            foreach (var childEntity in entities) {
                center += childEntity.GetBounds().center;
            }
            center /= entities.Count;
            var bounds = new Bounds(center, Vector3.zero);
            foreach (var childEntity in entities) {
                bounds.Encapsulate(childEntity.GetBounds());
            }
            return bounds;
        }


    }
}
