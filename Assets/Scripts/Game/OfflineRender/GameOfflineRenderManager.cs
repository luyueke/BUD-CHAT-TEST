// @Author: YangJie
// @Description:
// @Date:  2023/09/19
// @Modify:

using System;
using System.Collections.Generic;
using Basic.Extensions;
using Game.OfflineRender;
using Game.Props.PropsManagers;
using Game.Utils;
using GameData.BaseInfo;
using GameData.Manager;
using GameData.OfflineRender;
using Network;
using Network.Http;
using UnityEngine;

namespace Game.Base {
    public class GameOfflineRenderManager : GlobalInstance<GameOfflineRenderManager> {
        private Dictionary<string, OfflineRenderData> offlineRenderData = new Dictionary<string, OfflineRenderData>();


        public GameOfflineRenderManager() {
            OfflineSandboxStorage.Init();
        }

        public void AddOfflineRenderData(string id, OfflineRenderData renderData) {
            if (string.IsNullOrEmpty(id) || renderData == null || renderData.abConfig == null) {
                return;
            }

            offlineRenderData[id] = renderData;
        }

        public bool ContainsOfflineRenderData(string id) {
            if (string.IsNullOrEmpty(id)) {
                return false;
            }
            return offlineRenderData.ContainsKey(id);
        }

        public bool TryGetOfflineRenderData(string id, out OfflineRenderData renderData) {
            if (string.IsNullOrEmpty(id)) {
                renderData = null;
                return false;
            }
            return offlineRenderData.TryGetValue(id, out renderData);
        }



        public void InitAndLoadMapOfflineRenderData(Action callBack) {
            var mapInfo = GameDataManager.Inst.mapGlobalData.GetCurInfo<MapInfo>();
            InitAndLoadMapOfflineRenderData(mapInfo.propIds, callBack);
        }

        public void InitAndLoadMapOfflineRenderData(List<string> ids, Action callBack) {
            offlineRenderData.Clear();
            if (!ids.IsNullOrEmpty()) {
                GetRenderInfo(ids, (renderInfo) => {
                    if (renderInfo == null || renderInfo.propList.IsNullOrEmpty()) {
                        callBack?.Invoke();
                        return;
                    }
                    foreach (var renderData in renderInfo.propList) {
                        if (renderData.abConfig != null) {
                            offlineRenderData.TryAdd(renderData.id, renderData);
                        }
                    }

                    callBack?.Invoke();
                });
            } else {
                callBack?.Invoke();
            }
        }

        /// <summary>
        /// 获取离线渲染信息并创建物体
        /// </summary>
        /// <param name="id"></param>
        /// <param name="parent"></param>
        /// <param name="callBack"></param>

        public void LoadGameObj(string id, Transform parent, Action<GameObject> callBack) {
            if (string.IsNullOrEmpty(id)) {
                callBack?.Invoke(null);
                return;
            }
            if (offlineRenderData.TryGetValue(id, out var renderData) && !renderData.IsValid()) {
                callBack?.Invoke(null);
                return;
            }

            if (renderData == null) {
                GetRenderInfo(new List<string>() {id}, batchInfo => {
                    if (batchInfo == null || batchInfo.propList == null || batchInfo.propList.Count == 0) {
                        callBack?.Invoke(null);
                    } else {
                        foreach (var tmpData in batchInfo.propList) {
                            AddOfflineRenderData(tmpData.id, tmpData);
                        }
                        LoadHighGameObject(id, parent, callBack);
                    }
                });
            } else {
                LoadHighGameObject(id, parent, callBack);
            }
        }


        /// <summary>
        /// 加载高模
        /// </summary>
        /// <param name="id"></param>
        /// <param name="parent"></param>
        /// <param name="callBack"></param>
        public void LoadHighGameObject(string id, Transform parent, Action<GameObject> callBack) {
            if (string.IsNullOrEmpty(id)) {
                callBack?.Invoke(null);
                return;
            }

            if (!offlineRenderData.TryGetValue(id, out var renderData) || !renderData.IsValid()) {
                callBack?.Invoke(null);
                return;
            }

            LoadLowGameObject(id, parent, (lowObj) => {
                if (lowObj == null) {
                    callBack?.Invoke(null);
                    return;
                }
                if (!renderData.IsSample() && !renderData.IsCombined()) {
                    var highAssetWrapper = GetAssetWrapper(renderData, ModelLODType.HIGH);
                    if (highAssetWrapper == null) {
                        callBack?.Invoke(lowObj);
                        return;
                    }

                    highAssetWrapper.completed += isSuccess => {
                        if (!isSuccess) {
                            callBack?.Invoke(lowObj);
                            return;
                        }

                        if (lowObj == null)
                        {
                            callBack?.Invoke(null);
                            return;
                        }

                        var highObj = highAssetWrapper.RetainAsset(lowObj);
                        var meshFilters = highObj.GetComponentsInChildren<MeshFilter>();
                        foreach (var meshFilter in meshFilters) {
                            var childTrans = GameObjectEx.FindChildByName(lowObj, meshFilter.name);
                            if (childTrans == null) {
                                LoggerUtils.LogError("childTrans is null:" + meshFilter.name + "," + id);
                                continue;
                            }
                            var meshBehaviour = childTrans.GetComponent<MeshCombineBehaviour>();
                            if (meshBehaviour != null) {
                                meshBehaviour.SetMesh(meshFilter.sharedMesh);
                            } else {
                                LoggerUtils.LogError("meshBehaviour is null:" + meshFilter.name);
                            }
                        }
                        callBack?.Invoke(lowObj);
                    };
                } else {
                    callBack?.Invoke(lowObj);
                }
            });
        }

        public void LoadLowGameObject(string id, Transform parent, Action<GameObject> callBack) {

            if (string.IsNullOrEmpty(id)) {
                callBack?.Invoke(null);
                return;
            }

            if (!offlineRenderData.TryGetValue(id, out var renderData) || !renderData.IsValid()) {
                callBack?.Invoke(null);
                return;
            }

            if (renderData.IsCombined()) {
                LoadCombineGameObject(renderData, parent, callBack);
                return;
            }


            var lowAssetWrapper = GetAssetWrapper(renderData, ModelLODType.LOW);
            if (lowAssetWrapper == null) {
                callBack?.Invoke(null);
                return;
            }

            lowAssetWrapper.completed += isSuccess => {
                if (!isSuccess) {
                    callBack?.Invoke(null);
                    return;
                }

                GameObject originObj = new GameObject("CombineAsset");
                originObj.transform.SetParent(parent);
                originObj.Reset();
                var lowObj = lowAssetWrapper.Instantiate(originObj.transform);
                foreach (var matData in lowAssetWrapper.request.combineData.matDatas) {
                    var childTrans = lowObj.transform.Find(matData.nodeName);
                    if (childTrans == null) {
                        continue;
                    }
                    var meshBehaviour = childTrans.gameObject.GetOrAddComponent<MeshCombineBehaviour>();
                    meshBehaviour.SetMat(matData.GetMatComponent());
                }
                callBack?.Invoke(originObj);
            };
        }

        private void LoadCombineGameObject(OfflineRenderData renderData, Transform parent, Action<GameObject> callBack) {
            var combineAssetWrapper = GetAssetWrapper(renderData, ModelLODType.LOW);
            if (combineAssetWrapper == null) {
                callBack?.Invoke(null);
                return;
            }

            combineAssetWrapper.completed += isSuccess => {
                if (!isSuccess) {
                    callBack?.Invoke(null);
                    return;
                }

                GameObject originObj = new GameObject("CombineAsset");
                originObj.transform.SetParent(parent);
                originObj.Reset();
                var combineObj = combineAssetWrapper.Instantiate(originObj.transform);
                foreach (var matData in combineAssetWrapper.request.combineData.matDatas) {

                    if (matData.IsCombineMat()) {
                        // Find 可能返回 null（服务器数据中节点名与模型不匹配），需判空后再访问
                        var combineTrans = combineObj.transform.Find(matData.nodeName);
                        if (combineTrans == null)
                        {
                            LoggerUtils.LogError($"[GameOfflineRenderManager] LoadCombineGameObject 找不到 CombineMat 节点：{matData.nodeName}");
                            continue;
                        }
                        var meshObj = combineTrans.gameObject;
                        var meshBehaviour = meshObj.GetOrAddComponent<TextureCombineBehaviour>();
                        meshBehaviour.SetData(matData.GetTextureCombineData(), combineAssetWrapper.RetainTexture(matData.mainTex, combineObj), combineAssetWrapper.RetainTexture(matData.norTex, combineObj));
                    } else {
                        var matComp = matData.GetMatComponent();
                        // Find 可能返回 null，判空后再访问
                        var matTrans = combineObj.transform.Find(matData.nodeName);
                        if (matTrans == null)
                        {
                            LoggerUtils.LogError($"[GameOfflineRenderManager] LoadCombineGameObject 找不到 Mat 节点：{matData.nodeName}");
                            continue;
                        }
                        var meshObj = matTrans.gameObject;
                        if (matComp.matId.IsEmission || matComp.matId.IsTransparent) {
                            var meshBehaviour = meshObj.GetOrAddComponent<MeshCombineBehaviour>();
                            meshBehaviour.SetMat(matComp);
                        } else {
                            var colliderBehaviour = meshObj.GetOrAddComponent<ColliderCombineBehaviour>();
                            colliderBehaviour.SetMat(matComp);
                        }
                    }
                }

                var propManager = GlobalNodeManager.Inst.Get<PropManager>();
                if (propManager != null && propManager.TryGetPropData(renderData.id, out var pNodeData)) {
                    MeshCombineManager.Inst.FilterCombineNode(pNodeData, out _, out var unCombineList);
                    if (unCombineList != null && unCombineList.Count > 0) {
                        MeshCombineManager.Inst.CreateUnCombineMesh(originObj, unCombineList);
                    }
                }
                callBack?.Invoke(originObj);
            };


        }


        private void GetRenderInfo(List<string> ids, Action<RenderBatchInfo> callBack) {
            var req = new Dictionary<string, string>() {
                { "propIds", string.Join(',', ids) }
            };
            NetworkManager.Inst.SendHttpRequest<RenderBatchInfo>(HttpUrlDefine.RenderBatchInfo, HttpMethod.GET, req,
                (rsp) => {
                    callBack?.Invoke(rsp);
                }, err => {
                    callBack?.Invoke(null);
                });
        }

        private OfflineWrapper GetAssetWrapper(OfflineRenderData renderData, ModelLODType type) {
            var request = OfflineRequest.Load(renderData, type);
            if (request == null) {
                return null;
            }

            return new OfflineWrapper(request);
        }
    }
}
