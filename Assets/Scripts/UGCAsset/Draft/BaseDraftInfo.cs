// @Author: YangJie
// @Description:
// @Date:  2023/07/14
// @Modify:

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Basic.Utils;
using Game.COSXML;
using GameData.Base;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UIAgent;
using UnityEngine;
using Random = UnityEngine.Random;

namespace UGCAsset.Draft {
    public class DraftUploadInfo : IEquatable<DraftUploadInfo> {
        public string draftName;
        public string draftPath;
        public string remoteUrl;
        public string remoteFolder;
        public UploadStatus uploadStatus;


        public DraftUploadInfo() {
        }


        public DraftUploadInfo(string draftName, string draftPath, string remoteFolder) {
            this.draftName = draftName;
            this.draftPath = draftPath;
            this.remoteFolder = remoteFolder.Trim('\\', '/');
            uploadStatus = UploadStatus.NotUpload;
        }

        public void SetDraftPath(string path) {
            if (path == draftPath) {
                return;
            }

            uploadStatus = UploadStatus.NotUpload;
            draftPath = path;
        }

        public void SetRemoteUrl(string url) {
            remoteUrl = url;
            uploadStatus = UploadStatus.UploadSuccess;
        }


        public bool Equals(DraftUploadInfo other) {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return draftPath == other.draftPath;
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != this.GetType())
                return false;
            return Equals((DraftUploadInfo)obj);
        }

        public override int GetHashCode() {
            return (draftPath != null ? draftPath.GetHashCode() : 0);
        }
    }



    public abstract class BaseDraftInfo<T, K> : IEquatable<BaseDraftInfo<T, K>>
        where T : BaseDraftInfo<T, K> where K : UgcBaseInfo {
        public K baseInfo;

        public List<DraftUploadInfo> draftUploadInfos;
        public string draftId;
        public string draftName;

        public long updateTime;
        public int draftVersion;
        public int coverAutoSaved;
        public int editTime;

        public string uid;
        private bool isCancel = false;

        /// <summary>
        /// 是否保存到后端
        /// </summary>
        protected bool isSaveDraft = false;

        protected virtual string CoverKey => "cover";
        protected virtual string CoverRemoteFolder => $"UgcCover/{uid}";
        protected virtual string MetaDataKey => "metaData";
        protected virtual string MetaDataRemoteFolder => $"UgcMetadata/{uid}";

        protected virtual string SetUrl => "";

        public delegate void DraftSavedCallBack(T draftInfo);


        public BaseDraftInfo() {
        }

        public BaseDraftInfo(K ugcInfo) {
            baseInfo = ugcInfo;
            draftId = ugcInfo.id;
            draftVersion = ugcInfo.draftVersion;
            editTime = ugcInfo.editTime;
            uid = AccountDataManager.Inst.Uid;
            draftUploadInfos = new List<DraftUploadInfo>();
            updateTime = GameUtils.GetTimeStamp();
            SetUploadRemoteInfo(CoverKey, ugcInfo.cover, CoverRemoteFolder);
            SetUploadRemoteInfo(MetaDataKey, ugcInfo.metaDataUrl, MetaDataRemoteFolder);
        }


        protected string GetDraftCacheFolder() {
            var cacheFolder = Application.persistentDataPath + $"/DraftCache/{GetType().Name}/";
            if (!Directory.Exists(cacheFolder)) {
                Directory.CreateDirectory(cacheFolder);
            }

            return cacheFolder;
        }


        public UploadStatus GetUploadStatus() {
            if (isSaveDraft) {
                return UploadStatus.SavedDraft;
            }

            var uploadStatus = UploadStatus.NotUpload;
            if (draftUploadInfos == null || draftUploadInfos.Count == 0)
                return UploadStatus.UploadFail;
            if (draftUploadInfos.All(tmp => tmp.uploadStatus == UploadStatus.UploadSuccess)) {
                uploadStatus = UploadStatus.UploadSuccess;
            } else if (draftUploadInfos.Any(tmp => tmp.uploadStatus == UploadStatus.Uploading)) {
                uploadStatus = UploadStatus.Uploading;
            } else if (draftUploadInfos.Any(tmp => tmp.uploadStatus == UploadStatus.UploadFail)) {
                uploadStatus = UploadStatus.UploadFail;
            }

            return uploadStatus;
        }

        public string GetUploadMessage() {
            var uploadStatus = GetUploadStatus();
            if (uploadStatus == UploadStatus.NotUpload)
                return "未上传";
            if (uploadStatus == UploadStatus.UploadSuccess)
                return "上传成功";
            if (uploadStatus == UploadStatus.UploadFail)
                return "上传失败";
            if (uploadStatus == UploadStatus.Uploading)
                return "上传中";
            if (uploadStatus == UploadStatus.SavedDraft)
                return "已保存";
            return "未上传";
        }


        protected void SetUploadLocalInfo(string key, string localPath, string remoteFolder) {
            if (string.IsNullOrEmpty(localPath) || !File.Exists(localPath))
                return;
            var uploadInfo = draftUploadInfos.Find(info => info.draftName == key);
            if (uploadInfo != null) {
                uploadInfo.SetDraftPath(localPath);
            } else {
                draftUploadInfos.Add(new DraftUploadInfo(key, localPath, remoteFolder));
            }
        }

        protected void SetUploadRemoteInfo(string key, string remotePath, string remoteFolder) {
            if (string.IsNullOrEmpty(remotePath))
                return;
            var uploadInfo = draftUploadInfos.Find(info => info.draftName == key);
            if (uploadInfo != null) {
                uploadInfo.SetRemoteUrl(remotePath);
            } else {
                uploadInfo = new DraftUploadInfo(key, null, remoteFolder);
                uploadInfo.SetRemoteUrl(remotePath);
                draftUploadInfos.Add(uploadInfo);
            }
        }


        public void Upload(Action<T, bool> callBack) {
            if (draftUploadInfos == null || isCancel) {
                Debug.LogError("draftUploadInfos == null 或 isCancel == true");
                callBack?.Invoke(this as T, false);
                return;
            }

            if (GetUploadStatus() == UploadStatus.UploadSuccess || GetUploadStatus() == UploadStatus.SavedDraft) {
                callBack?.Invoke(this as T, true);
                return;
            }

            foreach (var draftUploadInfo in draftUploadInfos) {
                if (draftUploadInfo.uploadStatus == UploadStatus.NotUpload ||
                    draftUploadInfo.uploadStatus == UploadStatus.UploadFail) {
                    draftUploadInfo.uploadStatus = UploadStatus.Uploading;
                    var remotePath = draftUploadInfo.remoteFolder + "/" + Path.GetFileName(draftUploadInfo.draftPath);
                    string localPath = draftUploadInfo.draftPath;
                    CosXmlUploadManager.UploadFile(remotePath, localPath, (url, err) => {
                        if (localPath != draftUploadInfo.draftPath) {
                            LoggerUtils.LogError("本地文件已经修改");
                            return;
                        }

                        LoggerUtils.Log("DraftUploadInfo:" + $"上传草稿箱文件: 本地路径 ${draftUploadInfo.draftPath} 远程路径 {url}");
                        OnUploadCallBack(draftUploadInfo, url, err, () => {
                            if (GetUploadStatus() == UploadStatus.UploadSuccess) {
                                callBack?.Invoke(this as T, true);
                            } else {
                                callBack?.Invoke(this as T, false);
                            }
                        });
                    });
                }
            }
        }


        /// <summary>
        /// 上传资源到服务器并调用后端接口
        /// </summary>
        /// <param name="callBack"></param>
        public void UploadAndSave(Action<T, bool> callBack = null) {
            if (draftUploadInfos == null || isCancel) {
                Debug.LogError("draftUploadInfos == null 或 isCancel == true");
                callBack?.Invoke(this as T, false);
                return;
            }

            // 数据已经全部上传到 COS
            if (GetUploadStatus() == UploadStatus.UploadSuccess) {
                EditDraftToServer(callBack);
                return;
            }

            // 数据已经全部保存到后端
            if (GetUploadStatus() == UploadStatus.SavedDraft) {
                // 添加回调，因为第二次不会回调导致后续步骤卡住
                callBack?.Invoke(this as T, true);
                MessageHelper.Broadcast(DraftMessage.DraftSaveStatus, this as T);
                return;
            }

            // 开始上传到 COS
            foreach (var draftUploadInfo in draftUploadInfos) {
                if (draftUploadInfo.uploadStatus == UploadStatus.NotUpload || draftUploadInfo.uploadStatus == UploadStatus.UploadFail) {
                    draftUploadInfo.uploadStatus = UploadStatus.Uploading;
                    var remotePath = draftUploadInfo.remoteFolder + "/" + Path.GetFileName(draftUploadInfo.draftPath);
                    string localPath = draftUploadInfo.draftPath;
                    LoggerUtils.Log("DraftUploadInfo:" + $"开始上传: 本地路径 ${draftUploadInfo.draftPath}");
                    CosXmlUploadManager.UploadFile(remotePath, localPath, (url, err) => {
                        if (localPath != draftUploadInfo.draftPath) {
                            LoggerUtils.LogError("本地文件已经修改");
                            return;
                        }

                        LoggerUtils.Log("DraftUploadInfo:" + $"上传草稿箱文件成功: 本地路径 ${draftUploadInfo.draftPath} 远程路径 {url}");
                        OnUploadCallBack(draftUploadInfo, url, err, () => {
                            if (GetUploadStatus() == UploadStatus.UploadSuccess) {
                                Debug.Log("上传成功, 删除本地文件");
                                EditDraftToServer(callBack);
                            } else {
                                Debug.LogError("上传失败：" + GetUploadStatus());
                                callBack?.Invoke(this as T, false);
                            }
                        });
                    });
                }
            }

            MessageHelper.Broadcast(DraftMessage.DraftSaveStatus, this as T);
        }

        public virtual bool IsValid() {
            return !string.IsNullOrEmpty(draftId) && draftUploadInfos != null && draftUploadInfos.Count > 0;
        }

        public virtual void SetMetaData(byte[] metaData) {
            draftVersion++;
            updateTime = GameUtils.GetTimeStamp();
            isSaveDraft = false;
            var localMetaDataUrl = Path.Combine(GetDraftCacheFolder(), GetDraftFileName() + ".bytes");
            File.WriteAllBytes(localMetaDataUrl, metaData);
            SetUploadLocalInfo(MetaDataKey, localMetaDataUrl, MetaDataRemoteFolder);
        }

        public virtual void SetCover(byte[] coverData) {
            draftVersion++;
            updateTime = GameUtils.GetTimeStamp();
            var localCoverPath = Path.Combine(GetDraftCacheFolder(), GetDraftFileName() + ".png");
            File.WriteAllBytes(localCoverPath, coverData);
            SetUploadLocalInfo(CoverKey, localCoverPath, CoverRemoteFolder);
        }

        public virtual string GetMetadataRemoteUrl() {
            return draftUploadInfos.Find(info => info.draftName == MetaDataKey)?.remoteUrl;
        }

        public virtual string GetMetadataLocalUrl() {
            return draftUploadInfos.Find(info => info.draftName == MetaDataKey)?.draftPath;
        }


        /// <summary>
        /// 该接口合并了本地和远程的元数据，可能返回本地，也可能返回远端
        /// </summary>
        /// <returns></returns>
        public virtual string GetMetadataUrl() {
            var metaDataUrl = GetMetadataLocalUrl();
            if (string.IsNullOrEmpty(metaDataUrl) || !File.Exists(metaDataUrl)) {
                metaDataUrl = GetMetadataRemoteUrl();
            }

            return metaDataUrl;
        }

        public virtual string GetCoverRemoteUrl() {
            return draftUploadInfos.Find(info => info.draftName == CoverKey)?.remoteUrl;
        }

        public virtual string GetCoverLocalUrl() {
            return draftUploadInfos.Find(info => info.draftName == CoverKey)?.draftPath;
        }


        /// <summary>
        /// 该接口合并了本地和远程的封面图，可能返回本地，也可能返回远端
        /// </summary>
        /// <returns></returns>
        public virtual string GetCoverUrl() {
            var coverUrl = GetCoverLocalUrl();
            if (string.IsNullOrEmpty(coverUrl)) {
                coverUrl = GetCoverRemoteUrl();
            }
            if (string.IsNullOrEmpty(coverUrl)) {
                coverUrl = baseInfo.cover;
            }

            return coverUrl;
        }


        public void ResetUploadInfo() {
            foreach (var draftUploadInfo in draftUploadInfos) {
                if (draftUploadInfo.uploadStatus != UploadStatus.SavedDraft) {
                    draftUploadInfo.uploadStatus = UploadStatus.NotUpload;
                }
            }
        }


        protected virtual void OnUploadCallBack(DraftUploadInfo draftUploadInfo, string remoteUrl, string err,
            Action callBack) {
            if (isCancel) {
                CheckUploadAll(callBack);
                return;
            }

            if (!string.IsNullOrEmpty(err) || string.IsNullOrEmpty(remoteUrl)) {
                LoggerUtils.LogError(
                    $"上传草稿箱文件失败: 本地路径 {draftUploadInfo.draftPath} Err: {err} {draftUploadInfo.draftName}");
                draftUploadInfo.uploadStatus = UploadStatus.UploadFail;
            } else {
                draftUploadInfo.remoteUrl = remoteUrl;
                draftUploadInfo.uploadStatus = UploadStatus.UploadSuccess;
                RefreshUploadInfo(draftUploadInfo);
            }

            CheckUploadAll(callBack);
        }

        protected virtual void RefreshUploadInfo(DraftUploadInfo draftUploadInfo) {
            if (draftUploadInfo.draftName == MetaDataKey && baseInfo != null) {
                // 元数据不需要审核
                baseInfo.metaDataUrl = draftUploadInfo.remoteUrl;
            } else if (draftUploadInfo.draftName == CoverKey && baseInfo != null) {
                baseInfo.cover = draftUploadInfo.remoteUrl;
            }
        }




        public virtual void CreateDraftToServer(Action<K, bool> callBack) {
            var ugcInfo = ToUgcInfo();
            var req = new UGCSetRequest(ugcInfo, UGCOperationType.Add);
            NetworkManager.Inst.SendHttpRequest<DraftListItem>(SetUrl, HttpMethod.POST, req, draftListItem => {
                LoggerUtils.Log($"创建草稿信息成功:");
                callBack(draftListItem?.Get<K>(), true);
                isSaveDraft = true;
                MessageHelper.Broadcast(DraftMessage.DraftSaveStatus, this as T);
            }, fail => {
                LoggerUtils.LogError($"创建草稿信息失败:" + fail);
                callBack(null, false);
            });
        }

        public virtual void EditDraftToServer(Action<T, bool> callBack = null) {
            var ugcInfo = ToUgcInfo();
            var req = new UGCSetRequest(ugcInfo, UGCOperationType.Edit);
            NetworkManager.Inst.SendHttpRequest<DraftListItem>(SetUrl, HttpMethod.POST, req, draftListItem => {
                callBack?.Invoke(this as T, true);
                isSaveDraft = true;
                MessageHelper.Broadcast(DraftMessage.DraftSaveStatus, this as T);
            }, fail => {
                Debug.LogError($"保存草稿信息失败 [{ugcInfo.id}]:" + fail);
                callBack?.Invoke(this as T, false);
            });
        }

        public virtual void PublishDraftToServer(Action<K, bool> callBack) {
            var ugcInfo = ToUgcInfo();
            var req = new UGCSetRequest(ugcInfo, UGCOperationType.Publish);
            NetworkManager.Inst.SendHttpRequest<DraftListItem>(SetUrl, HttpMethod.POST, req, draftListItem => {
                callBack(draftListItem?.Get<K>(), true);
                isSaveDraft = true;
                MessageHelper.Broadcast(DraftMessage.DraftSaveStatus, this as T);
            }, rsp => {
                if (rsp.result == 501) {
                    // 审核失败
                    Action<bool> onAppealCallBack = (isAppeal) => {
                        // 提交申诉，视作发布成功
                        if (isAppeal) {
                            if (ugcInfo.auditInfo == null)
                            {
                                ugcInfo.auditInfo = new AuditStatus()
                                {
                                    auditResult = 4
                                };
                            }
                            else
                            {
                                ugcInfo.auditInfo.auditResult = 4;
                            }
                            callBack(ugcInfo, true);
                            isSaveDraft = true;
                            MessageHelper.Broadcast(DraftMessage.DraftSaveStatus, this as T);
                        } else {
                            // 放弃申诉，视作发布失败
                            callBack(null, false);
                        }
                    };
                    UIAgentManager.Inst.OpenPanel(PanelId.UGCAuditRejectedPanel, WindowId.None, req, rsp.rmsg, onAppealCallBack);
                } else {
                    HttpErrorCodeHandler errorCodeHandler = new Network.Http.HttpErrorCodeHandler();
                    errorCodeHandler.HandleErrorCodeResult(rsp);
                    LoggerUtils.LogError($"发布失败 [{ugcInfo.id}]:" + rsp.rmsg);
                    callBack(null, false);
                }
            }, null, 0, 0, true, false);
        }

        public virtual K ToUgcInfo() {
            if (baseInfo == null) {
                baseInfo = Activator.CreateInstance<K>();
            }

            baseInfo.draftVersion = draftVersion;
            baseInfo.updateTime = updateTime;
            baseInfo.editTime = editTime;
            var remoteMetadataUrl = GetMetadataRemoteUrl();
            if (!string.IsNullOrEmpty(remoteMetadataUrl)) {
                baseInfo.metaDataUrl = remoteMetadataUrl;
            }

            var coverRemoteUrl = GetCoverRemoteUrl();
            if (!string.IsNullOrEmpty(coverRemoteUrl)) {
                baseInfo.cover = coverRemoteUrl;
            }

            return baseInfo;
        }


        public void Cancel() {
            isCancel = true;
        }


        public bool Equals(BaseDraftInfo<T, K> other) {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return draftUploadInfos.SequenceEqual(other.draftUploadInfos) && draftId == other.draftId &&
                   draftVersion == other.draftVersion;
        }

        /// <summary>
        /// 获取唯一草稿文件名
        /// </summary>
        /// <returns></returns>
        public string GetDraftFileName() {
            return (uid + "_" + GameUtils.GetTimeStamp() + "_" + draftVersion + "_" + Random.Range(100, 999))
                .GetMd5Hash();
        }


        protected void CheckUploadAll(Action callBack) {
            bool isComplete = draftUploadInfos.All(tmp =>
                tmp.uploadStatus is UploadStatus.UploadSuccess || tmp.uploadStatus is UploadStatus.UploadFail);
            if (isComplete) {
                MessageHelper.Broadcast(DraftMessage.DraftSaveStatus, this as T);
                callBack?.Invoke();
            }
        }
    }
}
