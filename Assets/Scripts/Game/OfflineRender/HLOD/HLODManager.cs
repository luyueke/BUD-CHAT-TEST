// @Author: YangJie
// @Description:
// @Date:  2023/09/27
// @Modify:

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Basic.Utils;
using Game.Base;
using Game.ECS;
using Game.OfflineRender;
using Game.Props.PropsComponents;
using Game.Props.PropsController;
using Game.Props.PropsManagers;
using Game.Scene.ModeController;
using Game.Utils;
using GameData;
using GameData.Base;
using GameData.Config;
using GameData.MapData;
using Message;
using Pb.Map;
using UnityEngine;
using Random = UnityEngine.Random;

namespace HLOD {
    public class HLODManager : GameInstance<HLODManager>, INodeLife, IAutoInit, IGameMono {
        private Dictionary<uint, HLODGrid> hlodGridDic = new Dictionary<uint, HLODGrid>();
        private PMapData pMapData;
        private HLODGrid rootGrid;
        public bool isStarted = false;

        private const int MaxUpdateFrame = 10;
        private int curFrame = 0;

        private readonly Vector3 RootGridSize = new Vector3(500, 200, 500);
        private readonly Vector3 RootGridCenter = new Vector3(0, 90, 0);
        private readonly float LimitGridSize = 10;


        public List<int> lodNodeTypes = new List<int>() { };

        #region 分块配置

        public List<NodeModelType> lodNodeModels = new List<NodeModelType> {
            NodeModelType.SimpleShape,
            NodeModelType.PGCPlant,
            NodeModelType.PGCEffect,
            NodeModelType.PGCStone,
            NodeModelType.Prop,
        };

        /// <summary>
        /// 可移动属性
        /// </summary>
        public List<Tuple<NodeComponentId, string>> moveBehaviourComponents = new List<Tuple<NodeComponentId, string>>() {
        };

        #endregion


        public override void Release() {
            base.Release();
            pMapData = null;
            isStarted = false;
            hlodGridDic?.Clear();
            rootGrid?.Clear();
            MessageHelper.RemoveListener<EnterGameModel, UgcBaseInfo>(MessageName.OverEnterGame, OnOverBuildMap);
        }

        public void Update() {
        }

        public void FixedUpdate() {
            if (isStarted && curFrame++ > MaxUpdateFrame) {
                UpdateLODGrid();
            }
        }

        public void UpdateLODGrid() {
            curFrame = 0;
            var mainCamera = GameCameraUtils.Inst.GetMainCamera();
            var cameraParams = new HLODCameraParams(mainCamera);
            rootGrid?.Update(cameraParams);
        }


        public void Init() {
            MessageHelper.AddListener<EnterGameModel, UgcBaseInfo>(MessageName.OverBuildMap, OnOverBuildMap);
        }

        private void OnOverBuildMap(EnterGameModel enterGameModel, UgcBaseInfo ugcBaseInfo) {
            if (enterGameModel == EnterGameModel.GuestScene) {
                isStarted = true;
            }
        }

        public void Init(PMapData mapData) {
            if (mapData?.HlodData == null) {
                return;
            }
            rootGrid = new HLODGrid(RootGridCenter, RootGridSize);
            pMapData = mapData;
            foreach (var pGridData in pMapData.HlodData.GridData) {
                var tmpGrid = rootGrid.GetOrAddByKey(pGridData.Value);
                tmpGrid.AddNodeId(pGridData.Key);
                hlodGridDic.TryAdd(pGridData.Key, tmpGrid);
            }
        }



        public void OnCloneNode(NodeBaseBehaviour oldBehaviour, NodeBaseBehaviour newBehaviour) {

            if (newBehaviour is IHLODNode newHlodBehaviour && oldBehaviour is IHLODNode oldHlodBehaviour)
            {
                if (oldHlodBehaviour.assetObj != null && newHlodBehaviour.assetObj == null)
                {
                    var assetObjName = oldHlodBehaviour.assetObj.name;

                    var newAssetTrans = GameObjectEx.FindChildByName(newBehaviour.gameObject, assetObjName);
                    if (newAssetTrans != null)
                    {
                        newHlodBehaviour.RefreshAsset(newAssetTrans.gameObject);
                    }
                }
            }


        }

        public void OnCreateNode(NodeBaseBehaviour nodeBehaviour, NodeCreateType createType) {

            if (!nodeBehaviour.TryGetComponent(out HLODGroup lodGroup)) {
                return;
            }

            if (createType != NodeCreateType.SceneBuild) {
                lodGroup.ChangeLODStatus(ModelLODType.HIGH);
                return;
            }

            if (!this.IsGuest()) {
                lodGroup.ChangeLODStatus(ModelLODType.HIGH);
                return;
            }
            // 场景中的节点才有分块
            if (!nodeBehaviour.gameObject.IsChildOf(SceneBuilder.Inst.StageParent)) {
                lodGroup.ChangeLODStatus(ModelLODType.HIGH);
                return;
            }
            if (nodeBehaviour.entity.TryGetComp(out GameObjectComponent gameObjectComponent)
                && hlodGridDic.TryGetValue(gameObjectComponent.Uid, out var hlodGrid)) {
                hlodGrid.AddGroup(lodGroup);
            } else {
                lodGroup.ChangeLODStatus(ModelLODType.HIGH);
            }
        }

        public void OnRemoveNode(NodeBaseBehaviour nodeBehaviour) {
        }

        public void OnRevertNode(NodeBaseBehaviour nodeBehaviour) {
        }


        public List<string> GetHighItemList() {
            if (rootGrid == null) {
                return pMapData?.UgcItemData?.ItemDataList?.Select(tmp => tmp.UItemId).ToList();
            }

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            // 获取地图中所有出生点
            var spawnNodeDataList = pMapData.PropData.Pref.Where(tmp => tmp.TryGetComponent(out SpawnPointComponent _))
                .ToList();
            var spawnId = Random.Range(0, spawnNodeDataList.Count - 1);
            var spawnNodeData = spawnNodeDataList[spawnId];
            GlobalNodeManager.Inst.Get<SpawnPointManager>().SetSpawnPoint(spawnId);
            var pos = spawnNodeData.Pos.ToVector3();
            HLODCameraParams cameraParams = new HLODCameraParams(pos);
            var ugcItemIds = new HashSet<string>();
            stopwatch.Stop();
            LoggerUtils.Log("获取出生点:" + stopwatch.ElapsedMilliseconds);
            stopwatch.Restart();
            foreach (var tmpHlodGrid in hlodGridDic.Values.Distinct()) {
                var modelLODType = tmpHlodGrid.CalculateModelLOD(cameraParams, ref tmpHlodGrid.bounds);
                if (modelLODType == ModelLODType.HIGH) {
                    var itemIds = tmpHlodGrid.GetNodeIds();
                    foreach (var tmpItemId in itemIds) {
                        var tmpNodeData = pMapData.PropData.GetNodeData(tmpItemId);
                        if (tmpNodeData.TryGetComponent(out PropComponent itemComponent)) {
                            ugcItemIds.Add(itemComponent.uItemId);
                        }
                    }
                }
            }

            stopwatch.Stop();
            LoggerUtils.Log("获取高模耗时:" + stopwatch.ElapsedMilliseconds);
            return ugcItemIds.ToList();
        }


        public PGameHLODData SaveData() {
            var sw = new Stopwatch();
            sw.Start();
            rootGrid ??= new HLODGrid(RootGridCenter, RootGridSize, LimitGridSize);
            rootGrid.Clear();
            var parent = SceneBuilder.Inst.StageParent;
            foreach (var tmpGroup in parent.GetComponentsInChildren<HLODGroup>()) {
                tmpGroup.Refresh();
                if (!tmpGroup.IsValid()) {
                    continue;
                }
                var tmpGrid = rootGrid.CheckAndAddGroup(tmpGroup, tmpGroup.GetBounds());
                if (tmpGrid != null && tmpGrid != rootGrid) {
                    tmpGrid.AddGroup(tmpGroup);
                }
            }

            var gridItemInfoDic = new Dictionary<uint, string>();
            rootGrid.Export(gridItemInfoDic);

            sw.Stop();
            LoggerUtils.Log("地图分块耗时:" + sw.ElapsedMilliseconds);

            var hlodData = new PGameHLODData();
            hlodData.GridData.Add(gridItemInfoDic);
            return hlodData;
        }


    }
}
