using System;
using System.Collections.Generic;
using System.Linq;
using Basic.Extensions;
using Basic.Utils;
using Es;
using Game.Config;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using Game.Scene.ModeController;
using Game.Utils;
using GameData;
using GameData.BaseInfo;
using GameData.UGCData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pb.Map;
using UGCAsset;
using UnityEngine;

namespace Game.Base
{
    public class GameUgcMatManager:GlobalInstance<GameUgcMatManager>
    {
        /// <summary>
        /// 游玩场景中的UgcMat数据
        /// </summary>
        public PGameUGCMatMapData GlobalUGCData { get; private set; }

        public PGameUGCMatData GetUGCMatData(string id) {
            if (GlobalUGCData != null && GlobalUGCData.MatDataList != null)
            {
                for (var i = 0; i < GlobalUGCData.MatDataList.Count; i++)
                {
                    var mataData = GlobalUGCData.MatDataList[i];
                    if (mataData.UmatId == id)
                    {
                        return mataData;
                    }
                }
            }
            return null;
        }

        public void LoadTextureAsync(string url,int ugcStyle,GameObject refNode, Action<int,Texture> callback)
        {
            if (string.IsNullOrEmpty(url))
            {
                callback?.Invoke(ugcStyle,null);
                return;
            }
            if(url.Contains(GameConsts.BusinessBaseUrl))
            {
                url = url.Replace(GameConsts.BusinessBaseUrl, GameConsts.BusinessCdnUrl);
            }
            else if(url.Contains(GameConsts.AccBusinessBaseUrl))
            {
                url = url.Replace(GameConsts.AccBusinessBaseUrl, GameConsts.BusinessCdnUrl);
            }
            var warpper = Loader.LoadRemoteImageAsync(url);
            if (warpper == null)
            {
                callback?.Invoke(ugcStyle,null);
                return;
            }

            warpper.completed += suc =>
            {
                if (suc)
                {
                    var tex = warpper.RetainAsset(refNode);
                    callback?.Invoke(ugcStyle,tex);
                }
                else
                {
                    callback?.Invoke(ugcStyle,null);
                }
            };
        }


        public void LoadUgcTextureById(string matId, GameObject refNode,Renderer[] renderers, Action<int,Texture> callback)
        {
            // //缓存中找不到，则在材质列表中找
            string materialUrl = string.Empty;
            var resInfo = MatResInfoList.Find(x => x.ugcInfo.id == matId);
            int ugcStyle = 0;
            if (resInfo != null)
            {
                materialUrl = resInfo.ugcInfo.materialUrl;
                ugcStyle = resInfo.ugcInfo.ugcStyle;
            }
// #if UNITY_EDITOR
//             if (string.IsNullOrEmpty(materialUrl))
//             {
//                 RepairMapData(matId, refNode, callback);
//             }
// #else
            ChangeRendererMaterial(ugcStyle,renderers);
            UpdateSaveData(matId,ugcStyle,materialUrl);
            LoadTextureAsync(materialUrl,ugcStyle,refNode,callback);
// #endif
        }
        
        
        private class UgcMatDetailRsp
        {
            public List<DraftListItem> materialList;
        }

      
        
        //TODO:修改地图中材质列表部分丢失问题，仅限内部人员使用
        private void RepairMapData(string matId, GameObject refNode,Action<int,Texture> callback)
        {
            JObject interactListReq = new JObject()
            {
                ["idList"] =  matId,
            };
            NetworkManager.Inst.SendHttpRequest("/ugc/material/batchInfo",
                HttpMethod.GET,
                JsonConvert.SerializeObject(interactListReq),
                onReceive: arg0 =>
                {
                    var curRspData = JsonConvert.DeserializeObject<UgcMatDetailRsp>(arg0);
                    if (curRspData != null && curRspData.materialList != null && curRspData.materialList.Count > 0)
                    {
                        string materialUrl = curRspData.materialList[0].materialInfo?.materialUrl;
                        if (!string.IsNullOrEmpty(materialUrl))
                        {
                            Debug.LogError("matId===="+matId + "  arg0="+arg0);
                            int ugcStyle = curRspData.materialList[0].materialInfo.ugcStyle;
                            UpdateSaveData(matId,ugcStyle,materialUrl);
                            LoadTextureAsync(materialUrl,ugcStyle,refNode,callback);
                            return;
                        }
                    }
                    Debug.LogError("matId=11==="+matId + "  arg0="+arg0);

                    callback?.Invoke(0,null);
                },
                onFail: arg0 =>
                {
                    callback?.Invoke(0,null);
                    LoggerUtils.LogError("refreshInteractList failed retryCount =", 3);
                    isRequest = false;
                }, null, 0, 3);

        }

        //游玩、编辑、人物Avatar中设置UgcMat
        public void LoadGameUgcTextureById(string matId,GameObject refNode,Renderer[] renderers, Action<int,Texture> callback)
        {
            string materialUrl = string.Empty;
            int ugcStyle = 0;
            if (GlobalUGCData != null && GlobalUGCData.MatDataList != null && GlobalUGCData.MatDataList.Count > 0)
            {
                var tmpMatData = GlobalUGCData.MatDataList.FirstOrDefault(tmp => tmp.UmatId == matId);
                if (tmpMatData != null) {
                    materialUrl = tmpMatData.UmatUrl;
                    ugcStyle = tmpMatData.UmatStyle;
                }
            }

            if (saveCache.ContainsKey(matId))
            {
                materialUrl = saveCache[matId].UmatUrl;
                ugcStyle = saveCache[matId].UmatStyle;
            }

            if (!string.IsNullOrEmpty(materialUrl))
            {
                ChangeRendererMaterial(ugcStyle,renderers);
                UpdateSaveData(matId,ugcStyle,materialUrl);
                LoadTextureAsync(materialUrl,ugcStyle,refNode,callback);
                return;
            }
            LoadUgcTextureById(matId,refNode,renderers,callback);
        }

        private void ChangeRendererMaterial(int ugcStyle,Renderer[] renderers)
        {
            var mat = CustomMaterialLoaderUntils.Inst.GetDefaultMaterial();
            var animeMat =  Loader.Load<Material>("Assets/Arts/Game/BaseMatMaterial/AnimeStyleMatte.mat").Instantiate();
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].material = ugcStyle == (int) UgcShaderStyle.Normal ? mat : animeMat;
            }
        }


        private void UpdateSaveData(string matId,int ugcStyle,string materialUrl)
        {
            if (!string.IsNullOrEmpty(materialUrl))
            {
                var pbData = new PGameUGCMatData();
                pbData.UmatId = matId;
                pbData.UmatUrl = materialUrl;
                pbData.UmatStyle = ugcStyle;
                saveCache[matId] = pbData;
            }
        }



        Dictionary<string, PGameUGCMatData> saveCache = new Dictionary<string, PGameUGCMatData>(); // 保存时的缓存

        public void WriteUgcMat(MaterialUnionID id) {
            if (id.IsUGC) {
                if (!saveCache.ContainsKey(id.UGCId)) {
                    var matMata = GetUGCMatData(id.UGCId);
                    if (matMata != null)
                    {
                        var pbData = new PGameUGCMatData();
                        pbData.UmatId = id.UGCId;
                        pbData.UmatUrl = matMata.UmatUrl;
                        saveCache.Add(id.UGCId, pbData);
                    }
                }
            }
        }


        public UgcShaderStyle GetPGCAnimMatStyle()
        {
            // 当前使用 PGC 材质
            var baseShapes = GamePropNodeManager.Inst.GetBehaviours<SimpleShapeBehaviour>();
            if (!baseShapes.IsNullOrEmpty())
            {
                for (var i = 0; i < baseShapes.Count; i++)
                {
                    var tmp = baseShapes[i];
                    if (tmp != null && !tmp.entity.GetComp<MaterialComponent>().matId.IsUGC)
                    {
                        var unionID = tmp.entity.GetComp<MaterialComponent>().matId;
                        if (!unionID.IsUGC)
                        {
                            var matId = unionID.MatId;
                            var matData = DataTables.GetMatDataConfig(matId);
                            if (matData != null && matData.MatGroupType == (int) MatGroupTypeEnum.Anime)
                            {
                                return UgcShaderStyle.Anime;
                            }
                        }
                    }
                }
            }
            return UgcShaderStyle.Normal;
        }


        public PGameUGCMatMapData SaveUGCMatData() {


            var uMatIds = new HashSet<string>();
            // 当前使用 UGC 材质
            var baseShapes = GamePropNodeManager.Inst.GetBehaviours<SimpleShapeBehaviour>();
            if (!baseShapes.IsNullOrEmpty()) {
                baseShapes.ForEach(tmp => {
                    if (tmp == null) {
                        return;
                    }
                    if (tmp.entity.GetComp<MaterialComponent>().matId.IsUGC) {
                        var uMatId = tmp.entity.GetComp<MaterialComponent>().matId.UGCId;
                        uMatIds.Add(uMatId);
                    }
                });
            }

            var props = GamePropNodeManager.Inst.GetBehaviours<PropBehaviour>();
            if (!props.IsNullOrEmpty()) {
                props.ForEach(tmp => {
                    if (tmp == null) {
                        return;
                    }
                    var combineBehaviours = tmp.GetComponentsInChildren<MeshCombineBehaviour>();
                    combineBehaviours.ForEach(tmpMeshBehaviour => {
                        var matComp = tmpMeshBehaviour.GetMat();
                        if (matComp.matId.IsUGC) {
                            var uMatId = matComp.matId.UGCId;
                            uMatIds.Add(uMatId);
                        }
                    });
                });
            }

            var terrainBehaviour = GamePropNodeManager.Inst.GetBehaviour<TerrainBehaviour>();
            if (terrainBehaviour != null) {
                if (terrainBehaviour.entity.GetComp<MaterialComponent>().matId.IsUGC) {
                    var uMatId = terrainBehaviour.entity.GetComp<MaterialComponent>().matId.UGCId;
                    uMatIds.Add(uMatId);
                }
            }
            return SaveMapUGCMatData(uMatIds.ToList());
        }



        public PGameUGCMatMapData SaveMapUGCMatData(List<string> ids) {
            var matMapData = new PGameUGCMatMapData() {
                MatDataList = {  }
            };
            foreach (var tmpId in ids)
            {
                if (saveCache.TryGetValue(tmpId, out var matData))
                {
                    matMapData.MatDataList.Add(matData);
                }
            }
            return matMapData;
        }

        public PGameUGCMatMapData SaveUGCMatData(List<string> ids)
        {
            var ugcMatData = new PGameUGCMatMapData();
            foreach (var tmpId in ids)
            {
                if (saveCache.TryGetValue(tmpId, out var matData))
                {
                    ugcMatData.MatDataList.Add(matData);
                }

            }
            return ugcMatData;

        }

        public void AddUGCMatData(PGameUGCMatMapData data) {
            if (data == null) return;
            foreach (var tmpUgcMatData in data.MatDataList)
            {
                saveCache.TryAdd(tmpUgcMatData.UmatId, tmpUgcMatData);
            }
        }

        public void InitUGCMatData(PGameUGCMatMapData data) {
            if (data == null) {
                GlobalUGCData = new PGameUGCMatMapData();
            } else {
                GlobalUGCData = data;
            }
            ResetCookie();
        }

        private void ResetCookie()
        {
            isEnd = false;
            isRequest = false;
            currentCookie = "";
            MatResInfoList.Clear();
        }


        #region 已拥有的材质请求

        private string currentCookie = "";
        private bool isEnd = false;
        private bool isRequest = false;

        private JObject interactListReq = new JObject() {
            ["interactType"] = (int)UgcInteractType.PurchasedMaterial,
            ["cookie"] = "",
            ["ugcType"] = (int)UgcType.Material,
        };

        public List<ResInfo<MaterialInfo>> MatResInfoList = new List<ResInfo<MaterialInfo>>();

        private Action<List<ResInfo<MaterialInfo>>> onRefreshInteractCallback;
        private Action onRestInteractCallback;

        public void AddRefreshInteractListener(Action<List<ResInfo<MaterialInfo>>> action) {
            onRefreshInteractCallback += action;
        }

        public void RemoveRefreshInteractListener(Action<List<ResInfo<MaterialInfo>>> action) {
            onRefreshInteractCallback -= action;
        }

        public void AddRestInteractListener(Action action) {
            onRestInteractCallback += action;
        }

        public void RemoveRestInteractListener(Action action) {
            onRestInteractCallback -= action;
        }

        /// <summary>
        /// 退出商店刷新
        /// </summary>
        public void ForceRefreshInteractList()
        {
            ResetCookie();
            onRestInteractCallback?.Invoke();
            RefreshInteractList(3);
        }

        /// <summary>
        /// 刷新一下已经拥有的材质数据
        /// </summary>
        public void RefreshInteractList(int retryCount = 0) {
            if (isEnd || isRequest) return;

            isRequest = true;
            // 调用后端接口
            interactListReq["cookie"] = currentCookie;
            interactListReq["targetUid"] = AccountDataManager.Inst.Uid;

            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.UGCInteractList,
                HttpMethod.GET,
                JsonConvert.SerializeObject(interactListReq),
                onReceive: arg0 => {
                    ResInfoList<MaterialInfo> serverData =
                        JsonConvert.DeserializeObject<ResInfoList<MaterialInfo>>(arg0);
                    currentCookie = serverData.cookie;
                    isEnd = serverData.isEnd == 1;
                    isRequest = false;

                    if (serverData.list != null) {
                        onRefreshInteractCallback?.Invoke(serverData.list);
                        MatResInfoList.AddRange(serverData.list);
                    }
                },
                onFail: arg0 =>
                {
                    LoggerUtils.LogError("refreshInteractList failed retryCount =",retryCount);
                    isRequest = false;
                },null,0,retryCount);
        }

        #endregion
    }
}
