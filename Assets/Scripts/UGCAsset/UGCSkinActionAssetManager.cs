using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Es;
using Game.Config;
using GameData.Base;
using GameData.BaseInfo;
using GameData.Managers;
using GameData.UGCData;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UGCAsset.Draft;
using UnityEngine;

namespace UGCAsset {

    public class UGCSkinActionAssetManager<TK> : GlobalInstance<TK>
        where TK : BaseGlobalInstance, new()
    {
        protected readonly Dictionary<string, SkinActionBaseDraftInfo> draftInfos;
        protected string GetAssetInfoUrl => HttpUrlDefine.GetClothesInfo;

        public UGCSkinActionAssetManager() {
            var infoPath = $"{Application.persistentDataPath}/DraftCache/{typeof(SkinActionBaseDraftInfo).Name}_{AccountDataManager.Inst.Uid}.json";
            draftInfos = File.Exists(infoPath)
                ? JsonConvert.DeserializeObject<Dictionary<string, SkinActionBaseDraftInfo>>(File.ReadAllText(infoPath))
                : new Dictionary<string, SkinActionBaseDraftInfo>();
            MessageHelper.AddListener<SkinActionBaseDraftInfo>(DraftMessage.DraftSaveStatus, OnDraftSaveCallBack);
        }

        private void OnDraftSaveCallBack(SkinActionBaseDraftInfo draftInfo) {
            if (string.IsNullOrEmpty(draftInfo.draftId)) {
                return;
            }
            if (draftInfo.GetUploadStatus() != UploadStatus.SavedDraft)
                return;
            if (!draftInfos.ContainsKey(draftInfo.draftId))
                return;
            draftInfos.Remove(draftInfo.draftId);
            Save();
        }

        public void DeleteDraftInfo(string draftId) {
            if (!draftInfos.TryGetValue(draftId, out var tmpMapDraftInfo)) return;
            tmpMapDraftInfo.Cancel();
            tmpMapDraftInfo.RemoveDraftFiles();
            draftInfos.Remove(draftId);
            Save();
        }

        public void SaveDraftInfo(SkinActionBaseDraftInfo draftInfo) {
            if (!draftInfo.IsValid()) {
                Debug.LogError("草稿信息无效");
                return;
            }

            if (draftInfos.TryGetValue(draftInfo.draftId, out var tmpMapDraftInfo)) {
                if (tmpMapDraftInfo.Equals(draftInfo)) {
                    Save();
                    return;
                }

                Debug.LogError("删除草稿");
                tmpMapDraftInfo.Cancel();
                draftInfos.Remove(draftInfo.draftId);
                tmpMapDraftInfo.RemoveDraftFiles();
            }

            draftInfos.Add(draftInfo.draftId, draftInfo);
            Save();
        }

        public void UploadDraftInfo(string draftId, Action<SkinActionBaseDraftInfo, bool> callBack = null) {
            if (!draftInfos.TryGetValue(draftId, out var draftInfo)) {
                Debug.LogError("草稿信息不存在");
                callBack?.Invoke(null, false);
                return;
            }

            UploadDraftInfo(draftInfo, callBack);
        }

        /// <summary>
        /// 上传草稿信息
        /// </summary>
        /// <param name="draftInfo"></param>
        /// <param name="callBack"></param>
        public void UploadDraftInfo(SkinActionBaseDraftInfo draftInfo, Action<SkinActionBaseDraftInfo, bool> callBack = null) {
            if (!draftInfo.IsValid()) {
                Debug.LogError("草稿信息无效");
                callBack?.Invoke(draftInfo, false);
                return;
            }

            if (draftInfos.TryGetValue(draftInfo.draftId, out var tmpMapDraftInfo)) {
                if (!tmpMapDraftInfo.Equals(draftInfo)) {
                    tmpMapDraftInfo.Cancel();
                    tmpMapDraftInfo.RemoveDraftFiles();
                    Debug.LogError("RemoveDraftFiles");
                    draftInfos.Remove(draftInfo.draftId);
                }
            } else {
                draftInfos.Add(draftInfo.draftId, draftInfo);
            }

            draftInfo.UploadAndSave(callBack);
            Save();
        }


        public SkinActionBaseDraftInfo GetDraftInfo(SkinInfo info, SkinActionInfo skinActionInfo) {
            draftInfos.TryGetValue(info.id, out var draftInfo);
            if (draftInfo != null) {
                if (draftInfo.draftVersion > info.draftVersion) {
                    draftInfo._skinInfo.CopyTo(info);
                    draftInfo._skinInfo = info;
                    draftInfo._skinActionInfo = skinActionInfo;
                    return draftInfo;
                } else {
                    // 远端草稿版本大于本地，删除本地草稿
                    draftInfos.Remove(info.id);
                    return null;
                }
            } else {
                return null;
            }
        }

        public SkinActionBaseDraftInfo GetOrCreateDraftInfo(SkinInfo info, SkinActionInfo skinActionInfo) {
            if (!string.IsNullOrEmpty(info.id) && draftInfos.TryGetValue(info.id, out SkinActionBaseDraftInfo draftInfo) && draftInfo != null) {
                if (draftInfo.draftVersion >= info.draftVersion) {
                    draftInfo._skinInfo.CopyTo(info);
                    draftInfo._skinInfo = info;

                    draftInfo._skinActionInfo = skinActionInfo;
                    return draftInfo;
                }

                // 远端草稿版本大于本地，删除本地草稿
                draftInfo = Activator.CreateInstance(typeof(SkinActionBaseDraftInfo), info, skinActionInfo) as SkinActionBaseDraftInfo;
                draftInfos[info.id] = draftInfo;
                return null;
            }
            else {
                draftInfo = Activator.CreateInstance(typeof(SkinActionBaseDraftInfo), info, skinActionInfo) as SkinActionBaseDraftInfo;
                if (!string.IsNullOrEmpty(info.id)) {
                    draftInfos.Add(info.id, draftInfo);
                }
                return draftInfo;
            }
        }

        public SkinActionBaseDraftInfo GetDraftInfo(string draftId) {
            if (string.IsNullOrEmpty(draftId)) {
                return null;
            }

            draftInfos.TryGetValue(draftId, out var draftInfo);
            return draftInfo;
        }

        public SkinActionBaseDraftInfo[] GetDraftInfos() {
            return draftInfos.Values.ToArray();
        }


        /// <summary>
        /// 获取UGC资源详情
        /// </summary>
        public void GetAssetInfo(string id, Action<SkinInfo> callBack){
            JObject req = new() { { "id", id } };
            NetworkManager.Inst.SendHttpRequest<SkinInfoGetResponse>(GetAssetInfoUrl, HttpMethod.GET, req, response => {
                callBack?.Invoke(response.GetUgcInfo());
            }, fail => {
                Debug.LogError($"获取UGC 信息失败 [{id}]:" + fail);
                callBack?.Invoke(null);
            });
        }


        public void CreateInServer(SkinInfo info, SkinActionInfo skinActionInfo, Action<byte[]> callBack = null) {
            if (string.IsNullOrEmpty(info.cover)) {
                info.cover = GetTemplateCover(info.templateId);
            }
            var draftInfo = GetOrCreateDraftInfo(info, skinActionInfo);
            draftInfo?.CreateDraftToServer((skinInfoRsp, skinActionInfoRsp, isSuccess) => {
                if (isSuccess) {
                    skinInfoRsp.CopyTo(info);
                    callBack?.Invoke(GetTemplateMetaData(skinInfoRsp.templateId));
                } else {
                    callBack?.Invoke(null);
                }
            });
        }

        public virtual byte[] GetTemplateMetaData(string templateId = null) {
            PropTemplate template = null;
            // if (string.IsNullOrEmpty(templateId)) {
            //     template = DataTables.GetPropTemplateList().FirstOrDefault();
            // } else {
            //     template = DataTables.GetPropTemplate(templateId);
            // }
            template = DataTables.GetPropTemplateList().FirstOrDefault();
            var metaDataUrl = template?.MetaDataUrl;
            if (string.IsNullOrEmpty(metaDataUrl)) return null;
            var metaDataRequest = xasset.Asset.Load(metaDataUrl, typeof(TextAsset));
            if (metaDataRequest == null) return null;
            var metaData = metaDataRequest.asset as TextAsset;
            if (metaData == null) return null;
            return metaData.bytes;
        }

        public virtual string GetTemplateCover(string templateId = null) {
            return null;
        }

        public override void Release() {
            base.Release();
            MessageHelper.RemoveListener<SkinActionBaseDraftInfo>(DraftMessage.DraftSaveStatus, OnDraftSaveCallBack);
            Save();
        }

        protected virtual void Save() {
            if (draftInfos != null) {
                var cacheDir = $"{Application.persistentDataPath}/DraftCache";
                var infoPath =
                    $"{Application.persistentDataPath}/DraftCache/{typeof(SkinActionBaseDraftInfo).Name}_{AccountDataManager.Inst.Uid}.json";
                if (!Directory.Exists($"{Application.persistentDataPath}/DraftCache")) {
                    Directory.CreateDirectory(cacheDir);
                }

                File.WriteAllText(infoPath, JsonConvert.SerializeObject(draftInfos));
#if UNITY_EDITOR
                Debug.Log(
                    $"UGCAssetManager save draft to:{typeof(SkinActionBaseDraftInfo).Name}_{AccountDataManager.Inst.Uid},  data:{JsonConvert.SerializeObject(draftInfos)}");
#endif
            }
        }
    }
}

