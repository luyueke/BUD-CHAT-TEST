using System;
using System.Collections.Generic;
using Game.Base;
using Game.Props.PropsComponents;
using Game.Props.PropsManagers;
using Game.Utils;
using GameData.Config;
using GameData.MapData;
using Pb.Map;
using UIAgent;
using UnityEngine;
using xasset;
using Object = UnityEngine.Object;

/// <summary>
///脱离三角套的 道具加载流程，供Avatar 3d衣服、UI预览 等使用
/// </summary>
public class AssetPropNodeManager : GlobalInstance<AssetPropNodeManager>
{
    private bool isInited = false;
    public void Init()
    {
        if (isInited)
        {
            return;
        }

        RegisGameAgent();
        isInited = true;
    }

    private void RegisGameAgent()
    {
        GameAgentManager.Inst.ClearHandler();
        GameAgentManager.Inst.CreatePropEventHandler += CreateProp;
        GameAgentManager.Inst.CreatePropOfflineEventHandler += CreatePropWithOffline;
        GameAgentManager.Inst.IsInHallSceneHandler += IsInHallScene;
    }

    public bool IsInHallScene() {
        return GameController.IsInHallScene();
    }

    public GameObject CreatePropWithOffline(string id, string url, Action<GameObject> callBack) {
        var propNode = new GameObject($"HallPropNode_{id}");
        var anchorRoot = new GameObject("AnchorRoot");
        anchorRoot.transform.SetParent(propNode.transform);
        anchorRoot.Reset();
        var combineAsset = new GameObject("CombineAsset");
        combineAsset.transform.SetParent(anchorRoot.transform);
        combineAsset.Reset();

        if (GlobalNodeManager.HasInstance) {
            var propManager = GlobalNodeManager.Inst.Get<PropManager>();
            if (propManager != null && propManager.TryGetPropData(id, out var pNodeData)) {
                CreatePropAssetAsync(id, pNodeData, combineAsset.transform, propSkinObj => {
                    if (propNode == null) {
                        if (propSkinObj != null) {
                            Object.Destroy(propSkinObj);
                        }

                        return;
                    }

                    callBack?.Invoke(propSkinObj);
                });
                return propNode;
            }
        }

        if (!string.IsNullOrEmpty(url) && !string.IsNullOrEmpty(id)) {
            var assetRequest = Asset.LoadRemoteAssetAsync(url);
            if (assetRequest != null) {
                assetRequest.completed += (_) => {
                    if (propNode == null) {
                        LoggerUtils.Log("资源已经释放");
                        return;
                    }

                    if (assetRequest.result == Request.Result.Success) {
                        CreatePropAssetAsync(id, assetRequest.asset, combineAsset.transform, propSkinObj => {
                            if (propNode == null) {
                                if (propSkinObj != null) {
                                    Object.Destroy(propSkinObj);
                                }

                                return;
                            }
                            callBack?.Invoke(propSkinObj);
                        });
                    } else {
                        LoggerUtils.Log("下载失败:", assetRequest.error);
                        callBack?.Invoke(null);
                    }
                };
            }
        }


        return propNode;
    }


    public GameObject CreateProp(string id, string url, Action<GameObject> callBack) {
        var propNode = new GameObject($"HallPropNode_{id}");
        var anchorRoot = new GameObject("AnchorRoot");
        anchorRoot.transform.SetParent(propNode.transform);
        anchorRoot.Reset();
        var combineAsset = new GameObject("CombineAsset");
        combineAsset.transform.SetParent(anchorRoot.transform);
        combineAsset.Reset();
        if (GlobalNodeManager.HasInstance) {
            var propManager = GlobalNodeManager.Inst.Get<PropManager>();
            if (propManager != null && propManager.TryGetPropData(id, out var pNodeData)) {
                var propSkinObj = CreatePropAssetSync(id, pNodeData, combineAsset.transform);
                callBack?.Invoke(propSkinObj);
                return propNode;
            }
        }

        if (!string.IsNullOrEmpty(url) && !string.IsNullOrEmpty(id)) {
            var assetRequest = Asset.LoadRemoteAssetAsync(url);
            if (assetRequest != null) {
                assetRequest.completed += (_) => {
                    if (propNode == null) {
                        LoggerUtils.Log("资源已经释放");
                        return;
                    }

                    if (assetRequest.result == Request.Result.Success) {
                        var propSkinObj = CreatePropAssetSync(id, assetRequest.asset, combineAsset.transform);
                        callBack?.Invoke(propSkinObj);
                    } else {
                        LoggerUtils.Log("下载失败:", assetRequest.error);
                        callBack?.Invoke(null);
                    }
                };
            }
        }

        return propNode;
    }

    /// <summary>
    /// 仅仅创建展示作用素材，未包含任何素材逻辑
    /// </summary>
    /// <param name="id"></param>
    /// <param name="metaDataBytes"></param>
    /// <returns></returns>
    public GameObject CreatePropSync(string id, byte[] metaDataBytes) {
        PNodeData pNodeData = GetNodeData(id, metaDataBytes);
        if (pNodeData == null) {
            LoggerUtils.Log("CreateProp itemPb == null:" + id);
            return new GameObject("EmptyObj");
        }

        return CreatePropSync(id, pNodeData);
    }

    private GameObject CreatePropSync(string id, PNodeData pNodeData) {
        var propNode = new GameObject("HallPropNode");
        var anchorRoot = new GameObject("AnchorRoot");
        anchorRoot.transform.SetParent(propNode.transform);
        anchorRoot.Reset();
        var combineAsset = new GameObject("CombineAsset");
        combineAsset.transform.SetParent(anchorRoot.transform);
        combineAsset.Reset();
        CreatePropAssetSync(id, pNodeData, combineAsset.transform);
        return propNode;
    }

    private GameObject CreatePropAssetSync(string id, byte[] metaDataBytes, Transform parent) {
        PNodeData pNodeData = GetNodeData(id, metaDataBytes);
        if (pNodeData == null) {
            LoggerUtils.Log("CreateProp itemPb == null:" + id);
            var emptyObj = new GameObject("EmptyObj");
            emptyObj.transform.SetParent(parent);
            emptyObj.Reset();
            return emptyObj;
        }
        return CreatePropAssetSync(id, pNodeData, parent);
    }

    private GameObject CreatePropAssetSync(string id, PNodeData pNodeData, Transform parent) {
        if (pNodeData == null) {
            var emptyObj = new GameObject("EmptyObj");
            emptyObj.transform.SetParent(parent);
            emptyObj.Reset();
            return emptyObj;
        }

        return MeshCombineManager.Inst.GetCombineObj(id, pNodeData, parent);
    }

    private void CreatePropAssetAsync(string id, byte[] metaDataBytes, Transform parent, Action<GameObject> callBack) {
        PNodeData pNodeData = GetNodeData(id, metaDataBytes);
        if (pNodeData == null) {
            LoggerUtils.Log("CreateProp itemPb == null:" + id);
            var emptyObj = new GameObject("EmptyObj");
            emptyObj.transform.SetParent(parent);
            emptyObj.Reset();
            callBack?.Invoke(emptyObj);
            return;
        }
        CreatePropAssetAsync(id, pNodeData, parent, callBack);
    }

    private PNodeData GetNodeData(string id, byte[] metaDataBytes) {
        PNodeData pNodeData = null;
        if (GlobalNodeManager.HasInstance) {
            var propManager = GlobalNodeManager.Inst.Get<PropManager>();
            propManager?.TryGetPropData(id, out pNodeData);
        }

        if (pNodeData == null) {
            var itemPb = MapPbDataTool.ParsePropPb(metaDataBytes);
            if (itemPb == null) {
                return null;
            }

            if (itemPb.UgcmatData != null) {
                GameUgcMatManager.Inst.AddUGCMatData(itemPb.UgcmatData);
            }

            pNodeData = GetEmptyNodeData(id);
            pNodeData.Prims.AddRange(itemPb.NodeData.Prims);
            if (GlobalNodeManager.HasInstance) {
                var propManager = GlobalNodeManager.Inst.Get<PropManager>();
                propManager?.AddPropData(id, pNodeData);
            }
        }
        return pNodeData;
    }

    private void CreatePropAssetAsync(string id, PNodeData pNodeData, Transform parent, Action<GameObject> callBack) {
        if (pNodeData == null) {
            var emptyObj = new GameObject("EmptyObj");
            emptyObj.transform.SetParent(parent);
            emptyObj.Reset();
            callBack?.Invoke(emptyObj);
            return;
        }

        GameOfflineRenderManager.Inst.LoadGameObj(id, parent, tmpObj => {
            if (tmpObj == null) {
                callBack?.Invoke(MeshCombineManager.Inst.GetCombineObj(id, pNodeData, parent));
            } else {
                MeshCombineManager.Inst.FilterCombineNode(pNodeData, out _, out var unCombineList);
                if (unCombineList != null && unCombineList.Count > 0) {
                    MeshCombineManager.Inst.CreateUnCombineMesh(tmpObj, unCombineList);
                }
                callBack?.Invoke(tmpObj);
            }
        });
    }

    public PNodeData GetEmptyNodeData(string id) {
        var comp = new PropComponent {
            uItemId = id,
            scale = Vector3.one,
            anchor = Vector3.zero
        };
        var componentData = comp.Write();
        var pNodeData = new PNodeData {
            PropId = GamePropDataHelper.GetPropIdByNodeModelType(NodeModelType.Prop).Id,
            Pos = Vector3.zero.ToPB(),
            Scale = Vector3.one.ToPB(),
            Rotation = Vector3.zero.ToPB(),
            Attrs = { componentData }
        };
        return pNodeData;
    }


    public void DestroyProp(GameObject target)
    {
        if (target == null) return;
        // LoggerUtils.Log("####AssetPropNodeManager DestroyProp:"+target.transform.GetHashCode());
        GameObject.Destroy(target);
    }

}
