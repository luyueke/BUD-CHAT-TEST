// @Author: YangJie
// @Description:
// @Date:  2023/09/15
// @Modify:

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Es;
using Game.Base;
using Game.Config;
using Game.Props.PropsComponents;
using GameData.Config;
using Google.Protobuf.Collections;
using Newtonsoft.Json;
using Pb.Map;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Game.Utils {
    public class MeshCombineManager : GlobalInstance<MeshCombineManager>, IAutoInit {
        private Dictionary<string, GameObject> combinePoolDic = new Dictionary<string, GameObject>();
        private Dictionary<string, Queue<GameObject>> combineQueueDic = new Dictionary<string, Queue<GameObject>>();
        private GameObject combineMeshRoot = null;
        private Dictionary<string, Mesh> meshDic = new Dictionary<string, Mesh>();
        private CultureInfo ci;


        const string CombineObjName = "CombineObjRoot";
        const string UnCombineObjName = "UnCombineObj";
        private bool isInitialized = false;


        public MeshCombineManager() {
            Init();
        }

        public GameObject GetCombineObj(string nodeMD5, PNodeData pNodeData, Transform parent = null) {

            if (string.IsNullOrEmpty(nodeMD5) || nodeMD5 == GameConsts.ScenePropDraftId)
            {
                nodeMD5 = pNodeData.GetMD5();
            }

            GameObject originObj = new GameObject("CombineAsset");
            GameObject tmpObj = null;
            FilterCombineNode(pNodeData, out var combineList, out var unCombineList);

            ///从缓存池中获取
            if (combineQueueDic.TryGetValue(nodeMD5, out var queue) && queue.Count > 0) {
                tmpObj = queue.Dequeue();
                if (tmpObj != null) {
                    tmpObj.transform.SetParent(originObj.transform);
                    tmpObj.Reset();
                    originObj.transform.SetParent(parent);
                    originObj.Reset();
                    return originObj;
                }
            }

            /// 从原始节点中创建
            if (combinePoolDic.TryGetValue(nodeMD5, out var tmpOriginObj) && tmpOriginObj != null) {
                tmpObj = Object.Instantiate(tmpOriginObj, originObj.transform);
                var nMeshBehaviours = tmpObj.GetComponentsInChildren<MeshCombineBehaviour>();
                var oMeshBehaviours = tmpOriginObj.GetComponentsInChildren<MeshCombineBehaviour>();
                for (var i = 0; i < nMeshBehaviours.Length; i++) {
                    nMeshBehaviours[i].SetMat(oMeshBehaviours[i].GetMat());
                }

                tmpObj.Reset();
                tmpObj.name = CombineObjName;
            }

            /// 动态合并
            if (tmpObj == null) {
                tmpObj = CreateCombineMesh(originObj, combineList);
                combinePoolDic[nodeMD5] = tmpObj;
            }

            // 创建非合并节点
            CreateUnCombineMesh(originObj, unCombineList);
            originObj.transform.SetParent(parent);
            originObj.Reset();
            return originObj;
        }

        private GameObject CreateCombineMesh(GameObject rootObj, List<PNodeData> combineList) {
            if (combineList.Count == 0) {
                return null;
            }

            var combineObjRoot = new GameObject(CombineObjName);
            combineObjRoot.transform.SetParent(rootObj.transform);


            var combineDataDic = new Dictionary<string, Tuple<MaterialComponent, List<PNodeData>>>();
            foreach (var tmpData in combineList) {
                if (!tmpData.TryGetComponent<MaterialComponent>(out var comp)) {
                    continue;
                }

                var key = $"{comp.color}_{comp.matId}";
                if (!combineDataDic.ContainsKey(key)) {
                    combineDataDic.Add(key, new Tuple<MaterialComponent, List<PNodeData>>(comp, new List<PNodeData>()));
                }

                combineDataDic[key].Item2.Add(tmpData);
            }

            foreach (var combineData in combineDataDic) {
                var tmpObj = new GameObject(combineData.Key) {
                    layer = LayerMask.NameToLayer("Model")
                };
                tmpObj.transform.SetParent(combineObjRoot.transform);
                Mesh tmpMesh;
                if (combineData.Value.Item2.Count == 1) {
                    var tmpData = combineData.Value.Item2[0];
                    var newMesh = GetMesh(tmpData.PropId);
                    if (newMesh == null) {
                        LoggerUtils.LogError("CreateCombineMesh tmpMesh == null");
                        continue;
                    }

                    tmpMesh = SetMeshTilling(newMesh, combineData.Value.Item1.tile);
                    var rot = tmpData.Rotation.ToVector3();
                    var sca = tmpData.Scale.ToVector3();
                    var pos = tmpData.Pos.ToVector3();

                    tmpObj.transform.localPosition = pos;
                    tmpObj.transform.localRotation = Quaternion.Euler(rot);
                    tmpObj.transform.localScale = sca;
                } else {
                    tmpMesh = new Mesh() {
                        name = combineData.Key
                    };

                    var combineInstances = new List<CombineInstance>();
                    var verticesCount = 0;
                    foreach (var tmpData in combineData.Value.Item2) {
                        var newMesh = GetMesh(tmpData.PropId);
                        if (newMesh == null) {
                            LoggerUtils.LogError("CreateCombineMesh tmpMesh == null");
                            continue;
                        }

                        if (!tmpData.TryGetComponent<MaterialComponent>(out var comp)) {
                            continue;
                        }

                        newMesh = SetMeshTilling(newMesh, comp.tile);
                        var rot = tmpData.Rotation.ToVector3();
                        var sca = tmpData.Scale.ToVector3();
                        var pos = tmpData.Pos.ToVector3();
                        var combineInstance = new CombineInstance() {
                            mesh = newMesh,
                            transform = Matrix4x4.TRS(pos, Quaternion.Euler(rot), sca)
                        };
                        verticesCount += newMesh.vertexCount;
                        combineInstances.Add(combineInstance);
                    }

                    if (verticesCount >= 65535) {
                        tmpMesh.indexFormat = IndexFormat.UInt32;
                    }

                    tmpMesh.CombineMeshes(combineInstances.ToArray()); //将combineInstances数组传入函数
                    tmpObj.Reset();
                }

                var meshCombineBehaviour = tmpObj.AddComponent<MeshCombineBehaviour>();
                meshCombineBehaviour.SetMesh(tmpMesh);
                meshCombineBehaviour.SetMat(combineData.Value.Item1);
            }

            return combineObjRoot;
        }

        public void CreateUnCombineMesh(GameObject rootObj, IReadOnlyCollection<PNodeData> unCombineList) {
            if (unCombineList.Count > 0) {
                var unCombineObjRoot = new GameObject(UnCombineObjName);
                unCombineObjRoot.transform.SetParent(rootObj.transform);
                unCombineObjRoot.Reset();
                foreach (var pNodeData in unCombineList) {
                    var propData = GamePropDataHelper.GetPropDataByID(pNodeData.PropId);
                    if (propData.ModelType == (int)NodeModelType.SimpleShape) {
                        CreateSimpleNode(unCombineObjRoot, propData, pNodeData);
                    } else if (propData.ModelType == (int)NodeModelType.DText) {
                        CreateDText(unCombineObjRoot, propData, pNodeData);
                    } else if (propData.ModelType == (int)NodeModelType.Combine) {
                        var combineNode = GetCombineObj(null, pNodeData, unCombineObjRoot.transform);
                        combineNode.transform.localPosition = pNodeData.Pos.ToVector3();
                        combineNode.transform.localEulerAngles = pNodeData.Rotation.ToVector3();
                        combineNode.transform.localScale = pNodeData.Scale.ToVector3();
                    } else {
                        GamePropNodeManager.Inst.CreateSceneNodeByData(pNodeData, unCombineObjRoot.transform);
                    }
                }
            }
        }

        public void CreateCombineNode(GameObject rootObj, PNodeData combineNodeData) {
            var subCombineRoot = new GameObject("subCombineRoot");
            subCombineRoot.transform.SetParent(rootObj.transform);
            subCombineRoot.transform.localPosition = combineNodeData.Pos.ToVector3();
            subCombineRoot.transform.localEulerAngles = combineNodeData.Rotation.ToVector3();
            subCombineRoot.transform.localScale = combineNodeData.Scale.ToVector3();

            foreach (var nodeData in combineNodeData.Prims) {
                var propData = GamePropDataHelper.GetPropDataByID(nodeData.PropId);
                if (propData.ModelType == (int)NodeModelType.SimpleShape) {
                    CreateSimpleNode(subCombineRoot, propData, nodeData);
                } else if (propData.ModelType == (int)NodeModelType.DText) {
                    CreateDText(subCombineRoot, propData, nodeData);
                } else if (propData.ModelType == (int)NodeModelType.Combine) {
                    CreateCombineNode(subCombineRoot, nodeData);
                }
            }
        }

        public void CreateDText(GameObject rootObj, GamePropData propData, PNodeData pNodeData) {
            if (pNodeData.TryGetComponent<DTextComponent>(out var dTextComponent)) {
                var wrapper =
                    Loader.Load<GameObject>($"Assets/Loadable/Model3D/Editor_Props/{propData.PrefabName}.prefab");
                GameObject go = wrapper.Instantiate(rootObj.transform);
                go.name = wrapper.GetAssetName();
                go.transform.localPosition = pNodeData.Pos.ToVector3();
                go.transform.localRotation = Quaternion.Euler(pNodeData.Rotation.ToVector3());
                go.transform.localScale = pNodeData.Scale.ToVector3();
                go.layer = LayerMask.NameToLayer("Model");
                var textPro = go.GetComponentInChildren<SuperTextMesh>();
#if PACKAGE_TYPE_US
                textPro.font = Loader.Load<Font>("Assets/Arts/Font/Sarabun-ExtraBold.ttf", go);
#endif

                textPro.SetContent(dTextComponent.Content);
                textPro.SetColor(dTextComponent.TextColor);
            }
        }

        public void CreateSimpleNode(GameObject rootObj, GamePropData propData, PNodeData pNodeData) {
            if (pNodeData.TryGetComponent<MaterialComponent>(out var materialComponent)) {
                var wrapper =
                    Loader.Load<GameObject>($"Assets/Loadable/Model3D/Editor_Props/{propData.PrefabName}.prefab");
                GameObject go = wrapper.Instantiate(rootObj.transform);
                go.name = wrapper.GetAssetName();
                go.transform.localPosition = pNodeData.Pos.ToVector3();
                go.transform.localRotation = Quaternion.Euler(pNodeData.Rotation.ToVector3());
                go.transform.localScale = pNodeData.Scale.ToVector3();
                go.layer = LayerMask.NameToLayer("Model");
                var simpleBehaviour = go.AddComponent<MeshSimpleBehaviour>();
                simpleBehaviour.SetMat(materialComponent);
            }
        }


        private Mesh GetMesh(string id) {
            var tmpMesh = GetOriginMeshInfo(id);
            if (tmpMesh != null) {
                return Object.Instantiate(tmpMesh);
            } else {
                return null;
            }
        }

        public Mesh GetOriginMeshInfo(string id) {
            var propData = GamePropDataHelper.GetPropDataByID(id);
            if (propData.ModelType != (int)NodeModelType.SimpleShape) {
                LoggerUtils.LogError("id 为 非 3D 模型");
                return null;
            }

            if (meshDic.TryGetValue(id, out var tmpMesh) && tmpMesh != null)
                return tmpMesh;
            var prefabName = propData.PrefabName.Replace("BaseShape", "BaseShapeMesh");
            tmpMesh = Loader.Load<Mesh>($"Assets/Loadable/Model3D/Editor_Props/{prefabName}.mesh", combineMeshRoot);
            meshDic[id] = tmpMesh;
            return tmpMesh;
        }

        private Mesh SetMeshTilling(Mesh mesh, Vector2 tilling) {
            if (tilling == Vector2.one) {
                return mesh;
            }
            var uv = mesh.uv;
            for (int i = 0; i < uv.Length; i++) {
                uv[i] = new Vector2(uv[i].x * tilling.x, uv[i].y * tilling.y);
            }

            mesh.uv = uv;
            mesh.RecalculateUVDistributionMetrics();
            return mesh;
        }

        public void FilterCombineNode(PNodeData pNodeData, out List<PNodeData> combineList,
            out List<PNodeData> unCombineList) {
            combineList = new List<PNodeData>();
            unCombineList = new List<PNodeData>();
            foreach (var tmp in pNodeData.Prims) {
                var propData = GamePropDataHelper.GetPropDataByID(tmp.PropId);
                if (propData.ModelType != (int)NodeModelType.SimpleShape) {
                    unCombineList.Add(tmp);
                } else {
                    if (tmp.TryGetComponent<MaterialComponent>(out var comp) && !comp.matId.IsTransparent) {
                        combineList.Add(tmp);
                    } else {
                        unCombineList.Add(tmp);
                    }
                }
            }
        }

        public void Init() {
            if (isInitialized) {
                return;
            }

            isInitialized = true;
            ci = (CultureInfo)CultureInfo.CurrentCulture.Clone();
            ci.NumberFormat.CurrencyDecimalSeparator = ".";
            if (combineMeshRoot == null) {
                combineMeshRoot = GameObject.Find("CombineMeshRoot");
            }

            if (combineMeshRoot == null) {
                combineMeshRoot = new GameObject("CombineMeshRoot");
                combineMeshRoot.DontDestroy();
            }
        }

        public override void Release() {
            base.Release();
            meshDic?.Clear();
            isInitialized = false;
        }
    }
}
