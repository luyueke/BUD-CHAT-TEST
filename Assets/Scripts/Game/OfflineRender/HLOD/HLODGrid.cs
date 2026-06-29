// @Author: YangJie
// @Description:
// @Date:  2023/10/09
// @Modify:

using System.Collections.Generic;
using System.Linq;
using Game.Base;
using Game.ECS;
using Game.OfflineRender;
using UnityEngine;

namespace HLOD {
    public class HLODGrid {
        public static float LOD_DISTANCE_LOW = 0.3f;
        public static float LOD_DISTANCE_CULL = 0.1f;
        public static float LIMIT_GRID_SIZE = 10f;

        private HashSet<uint> hlodNodeIds = new HashSet<uint>();
        private List<HLODGroup> hlodGroups = new List<HLODGroup>();
        private readonly List<HLODGrid> childGrids = new List<HLODGrid>();

        private HLODGrid parentGrid;

        public Bounds bounds;

        private ModelLODType curLODType = ModelLODType.NONE;


        private bool isEmpty = true;

        private bool IsEmpty {
            get => isEmpty;
            set {
                isEmpty = value;
                if (parentGrid != null) {
                    parentGrid.IsEmpty =
                        !isEmpty ? value : parentGrid.childGrids.All(tmpChildGrid => tmpChildGrid.IsEmpty);
                }
            }
        }


#if UNITY_EDITOR
        public void DrawGizmos() {
            if (curLODType == ModelLODType.NONE) {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(bounds.center, bounds.size);
                return;
            }

            if (curLODType == ModelLODType.LOW) {
                if (IsAllLow()) {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireCube(bounds.center, bounds.size);
                } else {
                    foreach (var tmpChild in childGrids) {
                        tmpChild.DrawGizmos();
                    }
                }
            } else if (curLODType == ModelLODType.HIGH) {
                if (IsAllHigh()) {
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireCube(bounds.center, bounds.size);
                } else {
                    foreach (var tmpChild in childGrids) {
                        tmpChild.DrawGizmos();
                    }
                }
            }
        }
#endif

        public string Key {
            get;
            private set;
        }


        public HLODGrid(Vector3 center, Vector3 size) {
            bounds.center = center;
            bounds.size = size;
            Key = "root";
            curLODType = ModelLODType.NONE;
        }

        public void SetParent(HLODGrid parent) {
            parentGrid = parent;
            Key = parentGrid == null ? "root" : parentGrid.Key + "_" + parentGrid.childGrids.IndexOf(this);
        }

        public HLODGrid(Vector3 center, Vector3 size, float limitSize, HLODGrid parent = null) {
            bounds.center = center;
            bounds.size = size;
            parentGrid = parent;
            Key = parentGrid == null ? "root" : parentGrid.Key + "_" + parentGrid.childGrids.Count;
#if UNITY_EDITOR
            if (parent == null) {
                var tmpObj = new GameObject(Key);
                tmpObj.transform.localPosition = center;
                var drawer = tmpObj.AddComponent<HLODGridDrawer>();
                drawer.SetGrid(this);
            }
#endif
            SplitChildGrid();
        }


        public void Update(HLODCameraParams cameraParams) {
            ModelLODType tmpLodType = ModelLODType.NONE;
            if (parentGrid == null) {
                tmpLodType = ModelLODType.HIGH;
            } else {
                tmpLodType = CalculateModelLOD(cameraParams, ref bounds);
            }

            SetLODType(cameraParams, tmpLodType);
            switch (tmpLodType) {
                case ModelLODType.CULL:
                    break;
                case ModelLODType.LOW:
                case ModelLODType.HIGH:
                    foreach (var tmpGrid in childGrids) {
                        tmpGrid?.Update(cameraParams);
                    }
                    break;
            }
        }


        private void SetLODType(HLODCameraParams cameraParams, ModelLODType lodType) {
            if (curLODType == lodType) {
                return;
            }

            curLODType = lodType;
            if (lodType == ModelLODType.CULL) {
                if (childGrids != null) {
                    foreach (var tmpGrid in childGrids) {
                        tmpGrid?.SetLODType(cameraParams, lodType);
                    }
                }

                if (hlodGroups != null) {
                    foreach (var hlodGroup in hlodGroups) {
                        hlodGroup.ChangeLODStatus(lodType);
                    }
                }
            }

            if (hlodGroups != null) {
                foreach (var hlodGroup in hlodGroups) {
                    hlodGroup.ChangeLODStatus(lodType);
                }
            }
        }


        public void AddGroup(HLODGroup hlodGroup) {
            if (hlodGroups == null) {
                hlodGroups = new List<HLODGroup>();
            }

            if (!hlodGroups.Contains(hlodGroup)) {
                hlodGroup.gridKey = Key;
                hlodGroups.Add(hlodGroup);
                if (curLODType != ModelLODType.NONE) {
                    hlodGroup.ChangeLODStatus(curLODType);
                }
                IsEmpty = false;
            }
        }

        public void AddNodeId(uint nodeId) {
            hlodNodeIds.Add(nodeId);
        }

        public HashSet<uint> GetNodeIds() {
            return hlodNodeIds;
        }

        public HLODGrid CheckAndAddGroup(HLODGroup hlodGroup, Bounds hlodBounds) {
            if (hlodBounds.size == Vector3.zero) {
                return null;
            }

            HLODGrid tmpGrid = null;
            float centerDis = float.MaxValue;
            if (childGrids.Count == 0 &&
                (bounds.size.x > LIMIT_GRID_SIZE || bounds.size.y > LIMIT_GRID_SIZE ||
                 bounds.size.z > LIMIT_GRID_SIZE)) {
                SplitChildGrid();
            }

            foreach (var tmpChild in childGrids) {
                if (tmpChild != null && tmpChild.IsContains(hlodBounds)) {
                    var tmpDis = (tmpChild.bounds.center - hlodBounds.center).sqrMagnitude;
                    if (tmpDis < centerDis) {
                        centerDis = tmpDis;
                        tmpGrid = tmpChild;
                    }
                }
            }

            if (tmpGrid != null) {
                return tmpGrid.CheckAndAddGroup(hlodGroup, hlodBounds);
            }

            return this;
        }

        private bool IsContains(Bounds targetBounds) {
            var tmpBounds = bounds;
            tmpBounds.size *= 1.2f;
            if (tmpBounds.Contains(targetBounds.min) && tmpBounds.Contains(targetBounds.max)) {
                return true;
            } else {
                return false;
            }
        }

        public HLODGrid GetOrAddByKey(string key) {
            if (string.IsNullOrEmpty(key)) {
                return null;
            }

            HLODGrid targetGrid = null;
            if (Key == key) {
                targetGrid = this;
            } else {
                var tmpKeys = key.Replace(Key, "").Split("_").Where(tmp => !string.IsNullOrEmpty(tmp)).ToArray();
                if (!tmpKeys.Any()) {
                    return null;
                }

                if (!int.TryParse(tmpKeys.First(), out var childGridIndex)) {
                    return null;
                }

                HLODGrid childGrid = null;
                if (childGridIndex >= childGrids.Count || childGrids[childGridIndex] == null) {
                    childGrid = CreateChildGrid(childGridIndex);
                } else {
                    childGrid = childGrids[childGridIndex];
                }

                if (childGrid != null) {
                    targetGrid = childGrid.GetOrAddByKey(key);
                }
            }

            return targetGrid;
        }


        public bool IsAllHigh() {
            var isHigh = curLODType == ModelLODType.HIGH;

            if (!isHigh) return false;
            foreach (var tmpChildGrid in childGrids) {
                isHigh = tmpChildGrid.IsAllHigh();
                if (!isHigh) {
                    break;
                }
            }

            return isHigh;
        }

        public bool IsAllLow() {
            var isLow = curLODType == ModelLODType.LOW;

            if (!isLow) return false;
            foreach (var tmpChildGrid in childGrids) {
                isLow = tmpChildGrid.IsAllLow();
                if (!isLow) {
                    break;
                }
            }

            return isLow;
        }




        public void Clear() {
            hlodGroups.Clear();
            curLODType = ModelLODType.NONE;
            foreach (var tmpChildGrid in childGrids) {
                if (tmpChildGrid != null) {
                    tmpChildGrid.Clear();
                }
            }
        }

        public void Export(Dictionary<uint, string> hlodItemDic) {
            foreach (var tmpHLODGroup in hlodGroups) {
                var behaviour = tmpHLODGroup.GetComponent<NodeBaseBehaviour>();
                if (behaviour != null) {
                    if (behaviour.entity.TryGetComp(out GameObjectComponent objComp)) {
                        hlodItemDic.TryAdd(objComp.Uid, Key);
                    }
                }
            }

            foreach (var childGrid in childGrids) {
                childGrid.Export(hlodItemDic);
            }
        }

        public ModelLODType CalculateModelLOD(HLODCameraParams cameraParams, ref Bounds tmpBounds,
            bool testPlanes = true) {
            if (testPlanes) {
                var isCull = Vector3.Distance(tmpBounds.ClosestPoint(cameraParams.pos), cameraParams.pos) >
                             cameraParams.farClipPlane;
                if (isCull) {
                    return ModelLODType.CULL;
                }
            }

            var distance = Vector3.Distance(cameraParams.pos, tmpBounds.center);
            var relativeWidth = tmpBounds.size.x * 0.5f / (distance * cameraParams.relative);
            if (relativeWidth > LOD_DISTANCE_LOW) {
                return ModelLODType.HIGH;
            } else if (relativeWidth > LOD_DISTANCE_CULL) {
                return ModelLODType.LOW;
            }

            return ModelLODType.CULL;
        }

        private void SplitChildGrid() {
            for (int i = 0; i < 8; i++) {
                CreateChildGrid(i);
            }
        }

        private HLODGrid CreateChildGrid(int index) {
            if (index >= 8) {
                return null;
            }

            if (bounds.size.x > LIMIT_GRID_SIZE || bounds.size.y > LIMIT_GRID_SIZE || bounds.size.z > LIMIT_GRID_SIZE) {
                while (childGrids.Count <= index) {
                    childGrids.Add(null);
                }

                if (childGrids[index] != null) {
                    return childGrids[index];
                }

                var halfSize = bounds.size * 0.5f;
                var offset = halfSize * 0.5f;
                var center = bounds.center;
                Vector3 childCenter = new Vector3();
                switch (index) {
                    case 0:
                        childCenter = new Vector3(center.x - offset.x, center.y - offset.y, center.z - offset.z);
                        break;
                    case 1:
                        childCenter = new Vector3(center.x + offset.x, center.y - offset.y, center.z - offset.z);
                        break;
                    case 2:
                        childCenter = new Vector3(center.x - offset.x, center.y - offset.y, center.z + offset.z);
                        break;
                    case 3:
                        childCenter = new Vector3(center.x + offset.x, center.y - offset.y, center.z + offset.z);
                        break;
                    case 4:
                        childCenter = new Vector3(center.x - offset.x, center.y + offset.y, center.z - offset.z);
                        break;
                    case 5:
                        childCenter = new Vector3(center.x + offset.x, center.y + offset.y, center.z - offset.z);
                        break;
                    case 6:
                        childCenter = new Vector3(center.x - offset.x, center.y + offset.y, center.z + offset.z);
                        break;
                    case 7:
                        childCenter = new Vector3(center.x + offset.x, center.y + offset.y, center.z + offset.z);
                        break;
                }

                childGrids[index] = new HLODGrid(childCenter, halfSize);
                childGrids[index].SetParent(this);
                return childGrids[index];
            } else {
                return null;
            }
        }
    }
}
