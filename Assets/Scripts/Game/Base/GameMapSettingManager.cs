using System.Collections;
using System.Collections.Generic;
using Game.ECS;
using Game.MapSetting;
using GameData.Config;
using UnityEngine;
using Pb.Map;
using Google.Protobuf.Collections;

namespace Game.Base
{
    public class GameMapSettingManager : GameInstance<GameMapSettingManager>
    {
        private EcsSceneWorld sceneWorld;
        public SceneEntity settingEntity;
        public void Init(EcsSceneWorld wrold)
        {
            sceneWorld = wrold;
            settingEntity = sceneWorld.CreateEntity();
        }
        
        
        /// <summary>
        /// 从地图pb中解析地图设置，如灯光、音乐、环境音等
        /// </summary>
        /// <returns></returns>
        public void InitSettingByData(PGameSettingData settingData)
        {
            if (settingData.Attrs == null) return;
            for (int i = 0; i < settingData.Attrs.Count; i++)
            {
                var componentData = settingData.Attrs[i];
                NodeComponentId componentId = (NodeComponentId)componentData.CmpId;
                var componentType = GameTypeRegister.Inst.GetComponentType(componentId);
                if (typeof(IComponentSerializer).IsAssignableFrom(componentType)) // 是否是序列化类型的
                {
                    var component = settingEntity.AddComp(componentType);
                    component.SetId((uint)componentData.CmpId);
                    (component as IComponentSerializer).Read(componentData);
                }
            }
            
            OnCreateByData();

        }

        private void OnCreateByData()
        {
            var instances = GameInstanceManager.GetAllInstances();
            if (instances != null && instances.Count > 0)
            {
                foreach (var item in instances.Values)
                {
                    if (item is IMapSetting gameMono)
                    {
                        gameMono.OnCreateByData();
                    }
                }
            }
        }

        public PGameSettingData SaveSettingData()
        {
            PGameSettingData settingData = new PGameSettingData();
            RepeatedField<PComponentData> compDatas = settingData.Attrs;
            foreach (var comp in settingEntity.Components.Values)
            {
                if (comp is IComponentSerializer)
                {
                    comp.SetId((uint)GameTypeRegister.Inst.GetComponentId(comp.GetType()));
                    var serComp = comp as IComponentSerializer;
                    var componentData = serComp.Write();
                    if (componentData != null)
                    {
                        componentData.CmpId = comp.CmpId;
                        compDatas.Add(componentData);
                    }
                }
            }
            return settingData;
        }
    }
}
