using System.IO;
using UnityEngine;
using Game.ECS;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using Game.Props.PropsManagers;
using Pb.Map;
using Game.Scene.ModeController;
using GameData.OfflineRender;
using HLOD;
using Newtonsoft.Json;

namespace Game.Base
{
    public class SceneBuilder : GameInstance<SceneBuilder>, IAutoInit
    {
        public EcsSceneWorld EcsSceneWorld { get;private set; }
        public Transform SceneParent{ get;private set; }
        public Transform StageParent{ get;private set; }
        public Transform EditStageParent{ get;private set; }  // 该节点不参与序列化，只有编辑模式才创建

        public void Init()
        {
            EcsSceneWorld = new EcsSceneWorld();
            GameMapSettingManager.Inst.Init(EcsSceneWorld);
            GamePropNodeManager.Inst.Init(EcsSceneWorld);
            InitSceneParent();
        }

        public void InitSceneParent()
        {
            SceneParent = new GameObject("Scene").transform;
            StageParent = new GameObject("Stage").transform;
            if (this.IsEdit())
            {
                EditStageParent = new GameObject("EditStage").transform;
                EditStageParent.SetParent(SceneParent);
            }
            StageParent.SetParent(SceneParent);
        }

        #region 解析地图

        /// <summary>
        /// 通关PMapData 还原地图
        /// </summary>
        /// <param name="mapData"></param>
        public void BuildMapByData(PMapData mapData)
        {
            GameUgcMatManager.Inst.InitUGCMatData(mapData.UgcmatData);
            GlobalNodeManager.Inst.Get<PropManager>().InitPropData(mapData.UgcItemData);
            GameMapSettingManager.Inst.InitSettingByData(mapData.SettingData);
            GamePropNodeManager.Inst.CreateSceneNodes(mapData.PropData.Pref);
#if UNITY_EDITOR
            //TODO：模版地图中创建道具使用
            // CreateDefaultNodes();
#endif
        }

        #endregion

#if UNITY_EDITOR
        public void CreateDefaultNodes()
        {
            CreateSpawnPoints();
        }

        public void CreateSpawnPoints()
        {
            for (int i = 0; i < 16; i++)
            {
                NodeBaseBehaviour behaviour;
                GamePropNodeManager.Inst.TryCreateInEdit("20100024",out behaviour);
                if (behaviour == null)
                {
                    continue;
                }
                behaviour.transform.localPosition = MatrixArrangement(i, Vector3.zero);
                var component = behaviour.entity.GetOrAddComp<SpawnPointComponent>();
                if (i == 0)
                {
                    component.SpawnDefault = 1;
                }
                component.SpawnIndex = i + 1;
                var sBehaviour = behaviour as SpawnPointBehaviour;
                sBehaviour.SetIndex(component.SpawnIndex);
                sBehaviour.SetDefault(component.SpawnDefault);
            }
        }

        public Vector3 MatrixArrangement(int index,Vector3 centerPos)
        {
              int matrixLength = 4;
              float interval = 1.5f;
              return new Vector3((index / matrixLength -matrixLength/2f) * interval + centerPos.x, 0, (index % matrixLength -matrixLength/2f) * interval+ centerPos.z);
        }
#endif


        #region 保存地图

        public PMapData SaveMapToData()
        {
            PMapData mapData = new PMapData();
            mapData.SettingData = GameMapSettingManager.Inst.SaveSettingData();
            mapData.PropData = GamePropNodeManager.Inst.SavePropData();
            mapData.UgcmatData = GameUgcMatManager.Inst.SaveUGCMatData();
            mapData.UgcItemData = GlobalNodeManager.Inst.Get<PropManager>().SavePropData(mapData.PropData);
            mapData.BasicData = GetBasicData();
            mapData.HlodData = HLODManager.Inst.SaveData();
            return mapData;
        }

        private PGameBasicData GetBasicData()
        {
            PGameBasicData basicData = new PGameBasicData();
            return basicData;
        }

        #endregion

    }
}

