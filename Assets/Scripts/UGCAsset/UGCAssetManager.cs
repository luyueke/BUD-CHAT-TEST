using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameData.Base;
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
    public abstract class UGCAssetManager<T, TD, TK> : GlobalInstance<TK> where T : BaseDraftInfo<T, TD>, new()
        where TK : BaseGlobalInstance, new()
        where TD : UgcBaseInfo {
        protected readonly Dictionary<string, T> draftInfos;

        private Dictionary<string, CacheUGCData<TD>> cacheUGCDatas;


        protected abstract string GetAssetInfoUrl {
            get;
        }


        public UGCAssetManager() {
            var infoPath =
                $"{Application.persistentDataPath}/DraftCache/{typeof(T).Name}_{AccountDataManager.Inst.Uid}.json";
            draftInfos = File.Exists(infoPath)
                ? JsonConvert.DeserializeObject<Dictionary<string, T>>(File.ReadAllText(infoPath))
                : new Dictionary<string, T>();
            cacheUGCDatas = new Dictionary<string, CacheUGCData<TD>>();
            MessageHelper.AddListener<T>(DraftMessage.DraftSaveStatus, OnDraftSaveCallBack);
        }


        private void OnDraftSaveCallBack(T draftInfo) {
            if (string.IsNullOrEmpty(draftInfo.draftId)) {
                return;
            }
            if (draftInfo.GetUploadStatus() != UploadStatus.SavedDraft) return;
            if (!draftInfos.ContainsKey(draftInfo.draftId)) return;
            draftInfos.Remove(draftInfo.draftId);
            Save();
        }

        public void DeleteDraftInfo(string draftId) {
            if (!draftInfos.TryGetValue(draftId, out var tmpMapDraftInfo)) return;
            tmpMapDraftInfo.Cancel();
            draftInfos.Remove(draftId);
            Save();
        }


        public void SaveDraftInfo(T draftInfo) {
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
            }

            draftInfos.Add(draftInfo.draftId, draftInfo);
            Save();
        }


        public void UploadDraftInfo(string draftId, Action<T, bool> callBack = null) {
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
        public void UploadDraftInfo(T draftInfo, Action<T, bool> callBack = null) {
            if (!draftInfo.IsValid()) {
                Debug.LogError("草稿信息无效");
                callBack?.Invoke(draftInfo, false);
                return;
            }

            if (draftInfos.TryGetValue(draftInfo.draftId, out var tmpMapDraftInfo)) {
                if (!tmpMapDraftInfo.Equals(draftInfo)) {
                    tmpMapDraftInfo.Cancel();
                    Debug.LogError("RemoveDraftFiles");
                    draftInfos.Remove(draftInfo.draftId);
                }
            } else {
                draftInfos.Add(draftInfo.draftId, draftInfo);
            }

            draftInfo.UploadAndSave(callBack);
            Save();
        }

        public T GetDraftInfo(TD info) {
            draftInfos.TryGetValue(info.id, out var draftInfo);
            if (draftInfo != null) {
                if (draftInfo.draftVersion > info.draftVersion) {
                    draftInfo.baseInfo.CopyTo(info);
                    draftInfo.baseInfo = info;
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

        public T GetOrCreateDraftInfo(TD info) {
            if (!string.IsNullOrEmpty(info.id) && draftInfos.TryGetValue(info.id, out T draftInfo) &&
                draftInfo != null) {
                if (draftInfo.draftVersion > info.draftVersion) {
                    draftInfo.baseInfo.CopyTo(info);
                    draftInfo.baseInfo = info;
                    return draftInfo;
                }

                // 远端草稿版本大于本地，删除本地草稿
                draftInfo = Activator.CreateInstance(typeof(T), info) as T;
                draftInfos[info.id] = draftInfo;
                return draftInfo;
            } else {
                draftInfo = Activator.CreateInstance(typeof(T), info) as T;
                if (!string.IsNullOrEmpty(info.id)) {
                    draftInfos.Add(info.id, draftInfo);
                }
                return draftInfo;
            }
        }


        public T GetDraftInfo(string draftId) {
            if (string.IsNullOrEmpty(draftId)) {
                return null;
            }

            draftInfos.TryGetValue(draftId, out var draftInfo);
            return draftInfo;
        }

        public T[] GetDraftInfos() {
            return draftInfos.Values.ToArray();
        }

        /// <summary>
        /// 只用于获取 已经发布的UGC资源详情, 带缓存(缓存记录在内存中, 退出app 才会清理)
        /// </summary>
        public void GetAssetInfo<TF>(string id, Action<TD> callBack) where TF : BaseUgcInfoResponse<TD>{
            if (string.IsNullOrEmpty(id)) {
                callBack?.Invoke(null);
                return;
            }
            if (cacheUGCDatas.TryGetValue(id, out var cacheUGCData)) {
                if (cacheUGCData.status == CacheStatus.Success) {
                    callBack?.Invoke(cacheUGCData.info);
                    return;
                } else if (cacheUGCData.status == CacheStatus.Requesting) {
                    cacheUGCData.AddCallback(callBack);
                    return;
                }
            } else {
                cacheUGCData = new CacheUGCData<TD>(callBack);
                cacheUGCDatas.Add(id, cacheUGCData);
            }
            cacheUGCData.status = CacheStatus.Requesting;
            JObject req = new() { { "id", id } };
            NetworkManager.Inst.SendHttpRequest<TF>(GetAssetInfoUrl, HttpMethod.GET, req, response => {
                cacheUGCData.SetInfo(response.GetUgcInfo());
                cacheUGCData.InvokeCallBack();
            }, fail => {
                LoggerUtils.Log($"获取UGC 信息失败 [{id}]:" , fail);
                cacheUGCData.SetInfo(null);
                cacheUGCData.InvokeCallBack();
            });
        }


        public void CreateInServer(TD info, Action<byte[]> callBack = null) {
            if (string.IsNullOrEmpty(info.cover)) {
                info.cover = GetTemplateCover(info.templateId);
            }

            var draftInfo = GetOrCreateDraftInfo(info);
            draftInfo?.CreateDraftToServer((tmpInfo, isSuccess) => {
                if (isSuccess) {
                    tmpInfo.CopyTo(info);
                    callBack?.Invoke(GetTemplateMetaData(tmpInfo.templateId));
                } else {
                    callBack?.Invoke(null);
                }
            });
        }

        public virtual byte[] GetTemplateMetaData(string templateId = null) {
            return null;
        }

        public virtual string GetTemplateCover(string templateId = null) {
            return null;
        }


        public override void Release() {
            base.Release();
            MessageHelper.RemoveListener<T>(DraftMessage.DraftSaveStatus, OnDraftSaveCallBack);
            Save();
        }

        protected virtual void Save() {
            if (draftInfos != null) {
                var cacheDir = $"{Application.persistentDataPath}/DraftCache";
                var infoPath =
                    $"{Application.persistentDataPath}/DraftCache/{typeof(T).Name}_{AccountDataManager.Inst.Uid}.json";
                if (!Directory.Exists($"{Application.persistentDataPath}/DraftCache")) {
                    Directory.CreateDirectory(cacheDir);
                }

                File.WriteAllText(infoPath, JsonConvert.SerializeObject(draftInfos));
#if UNITY_EDITOR
                Debug.Log(
                    $"UGCAssetManager save draft to:{typeof(T).Name}_{AccountDataManager.Inst.Uid},  data:{JsonConvert.SerializeObject(draftInfos)}");
#endif
            }
        }
    }
}
