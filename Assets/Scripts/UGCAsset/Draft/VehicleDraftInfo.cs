using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Basic.Utils;
using Game.COSXML;
using GameData.Base;
using GameData.BaseInfo;
using GameData.PgcData;
using Message;
using Network;
using Network.Http;
using UIAgent;
//using UnityEditor.Media;
using UnityEngine;

namespace UGCAsset.Draft
{
    public class VehicleDraftInfo //: BaseDraftInfo<VehicleDraftInfo, VehicleInfo>
    {
        public VehicleInfo vehicleInfo;

        public List<DraftUploadInfo> draftUploadInfos;
        public string draftId;
        public string draftName;

        public long updateTime;
        public int draftVersion;
        public int coverAutoSaved;
        public int editTime;

        public string uid;

        private bool isCancel = false;

        protected string MapMetaKey => "MapMetaKey"; // 保存的地图元数据Key
        protected virtual string CoverKey => "cover";
        protected virtual string MetaDataKey => "metaData";
        protected virtual string CoverRemoteFolder => $"UgcCover/{uid}";
        protected virtual string MetaDataRemoteFolder => $"UgcMetadata/{uid}";
        private string VerifyKey => "VerifyKey"; //  3D 添加审核图
        protected virtual string SetUrl => HttpUrlDefine.SetVehicle;

        /// <summary>
        /// 是否保存到后端
        /// </summary>
        private bool isSaveDraft = false;

        public VehicleDraftInfo()
        {

        }

        public VehicleDraftInfo(VehicleInfo vehicleInfoValue)
        {
            this.vehicleInfo = vehicleInfoValue;
            draftId = this.vehicleInfo.id;
            draftVersion = this.vehicleInfo.draftVersion;
            editTime = this.vehicleInfo.editTime;
            uid = AccountDataManager.Inst.Uid;
            draftUploadInfos = new List<DraftUploadInfo>();
            updateTime = GameUtils.GetTimeStamp();
            SetUploadRemoteInfo(CoverKey, this.vehicleInfo.cover, CoverRemoteFolder);
            SetUploadRemoteInfo(MetaDataKey, this.vehicleInfo.metaDataUrl, MetaDataRemoteFolder);
        }

        public UploadStatus GetUploadStatus()
        {
            if (isSaveDraft)
            {
                return UploadStatus.SavedDraft;
            }

            var uploadStatus = UploadStatus.NotUpload;
            if (draftUploadInfos == null || draftUploadInfos.Count == 0)
                return UploadStatus.UploadFail;
            if (draftUploadInfos.All(tmp => tmp.uploadStatus == UploadStatus.UploadSuccess))
            {
                uploadStatus = UploadStatus.UploadSuccess;
            }
            else if (draftUploadInfos.Any(tmp => tmp.uploadStatus == UploadStatus.Uploading))
            {
                uploadStatus = UploadStatus.Uploading;
            }
            else if (draftUploadInfos.Any(tmp => tmp.uploadStatus == UploadStatus.UploadFail))
            {
                uploadStatus = UploadStatus.UploadFail;
            }

            return uploadStatus;
        }

        public string GetUploadMessage()
        {
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

        protected void SetUploadRemoteInfo(string key, string remotePath, string remoteFolder)
        {
            if (string.IsNullOrEmpty(remotePath))
                return;
            var uploadInfo = draftUploadInfos.Find(info => info.draftName == key);
            if (uploadInfo != null)
            {
                uploadInfo.SetRemoteUrl(remotePath);
            }
            else
            {
                uploadInfo = new DraftUploadInfo(key, null, remoteFolder);
                uploadInfo.SetRemoteUrl(remotePath);
                draftUploadInfos.Add(uploadInfo);
            }
        }

        public virtual void CreateDraftToServer(Action<VehicleInfo, bool> callBack)
        {
            //SyncDraftInfo();
            //var req = new UGCVehicleSetRequest(this.vehicleInfo, UGCOperationType.Add);
            //vehicleInfo.id = vehicleInfo.templateId;
            //callBack(vehicleInfo, true);
            //isSaveDraft = true;
            //MessageHelper.Broadcast(DraftMessage.DraftSaveStatus, this);
            //Debug.LogError("服务器还没做，先用假数据处理草稿请求");
            //return;
            SyncDraftInfo();
            var req = new UGCVehicleSetRequest(this.vehicleInfo, UGCOperationType.Add);
            //vehicleInfo.id = vehicleInfo.templateId;
            NetworkManager.Inst.SendHttpRequest<DraftListItem>(SetUrl, HttpMethod.POST, req, draftListItem =>
            {
                LoggerUtils.Log($"创建草稿信息成功:");
                callBack(draftListItem?.vehicleInfo, true);
                isSaveDraft = true;
                MessageHelper.Broadcast(DraftMessage.DraftSaveStatus, this);
            }, fail =>
            {
                LoggerUtils.LogError($"创建草稿信息失败:" + fail);
                callBack(null, false);
            });
        }

        public void Upload(Action<VehicleDraftInfo, bool> callBack)
        {
            if (draftUploadInfos == null || isCancel)
            {
                Debug.LogError("draftUploadInfos == null 或 isCancel == true");
                callBack?.Invoke(this, false);
                return;
            }

            if (GetUploadStatus() == UploadStatus.UploadSuccess || GetUploadStatus() == UploadStatus.SavedDraft)
            {
                callBack?.Invoke(this, true);
                return;
            }

            foreach (var draftUploadInfo in draftUploadInfos)
            {
                if (draftUploadInfo.uploadStatus == UploadStatus.NotUpload ||
                    draftUploadInfo.uploadStatus == UploadStatus.UploadFail)
                {
                    draftUploadInfo.uploadStatus = UploadStatus.Uploading;
                    var remotePath = draftUploadInfo.remoteFolder + "/" + Path.GetFileName(draftUploadInfo.draftPath);
                    string localPath = draftUploadInfo.draftPath;
                    CosXmlUploadManager.UploadFile(remotePath, localPath, (url, err) => {
                        if (localPath != draftUploadInfo.draftPath)
                        {
                            if (!string.IsNullOrEmpty(localPath) && File.Exists(localPath))
                            {
                                File.Delete(localPath);
                            }

                            LoggerUtils.LogError("本地文件已经修改");
                            return;
                        }

                        LoggerUtils.Log("DraftUploadInfo:" + $"上传草稿箱文件: 本地路径 ${draftUploadInfo.draftPath} 远程路径 {url}");
                        OnUploadCallBack(draftUploadInfo, url, err, () => {
                            if (GetUploadStatus() == UploadStatus.UploadSuccess)
                            {
                                callBack?.Invoke(this, true);
                            }
                            else
                            {
                                callBack?.Invoke(this, false);
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
        public void UploadAndSave(Action<VehicleDraftInfo, bool> callBack = null)
        {
            if (draftUploadInfos == null || isCancel)
            {
                Debug.LogError("draftUploadInfos == null 或 isCancel == true");
                callBack?.Invoke(this, false);
                return;
            }

            // 数据已经全部上传到 COS
            if (GetUploadStatus() == UploadStatus.UploadSuccess)
            {
                EditDraftToServer(callBack);
                return;
            }

            // 数据已经全部保存到后端
            if (GetUploadStatus() == UploadStatus.SavedDraft)
            {
                MessageHelper.Broadcast(DraftMessage.DraftSaveStatus, this);
                return;
            }

            // 开始上传到 COS
            foreach (var draftUploadInfo in draftUploadInfos)
            {
                if (draftUploadInfo.uploadStatus == UploadStatus.NotUpload ||
                    draftUploadInfo.uploadStatus == UploadStatus.UploadFail)
                {
                    draftUploadInfo.uploadStatus = UploadStatus.Uploading;
                    var remotePath = draftUploadInfo.remoteFolder + "/" + Path.GetFileName(draftUploadInfo.draftPath);
                    string localPath = draftUploadInfo.draftPath;
                    LoggerUtils.Log("DraftUploadInfo:" + $"开始上传: 本地路径 ${draftUploadInfo.draftPath}");
                    CosXmlUploadManager.UploadFile(remotePath, localPath, (url, err) =>
                    {
                        if (localPath != draftUploadInfo.draftPath)
                        {
                            if (!string.IsNullOrEmpty(localPath) && File.Exists(localPath))
                            {
                                File.Delete(localPath);
                            }

                            LoggerUtils.LogError("本地文件已经修改");
                            return;
                        }

                        LoggerUtils.Log("DraftUploadInfo:" +
                                        $"上传草稿箱文件成功: 本地路径 ${draftUploadInfo.draftPath} 远程路径 {url}");
                        OnUploadCallBack(draftUploadInfo, url, err, () =>
                        {
                            if (GetUploadStatus() == UploadStatus.UploadSuccess)
                            {
                                Debug.Log("上传成功, 删除本地文件");
                                RemoveDraftFiles();
                                EditDraftToServer(callBack);
                            }
                            else
                            {
                                Debug.LogError("上传失败：" + GetUploadStatus());
                                callBack?.Invoke(this, false);
                            }
                        });
                    });
                }
            }

            MessageHelper.Broadcast(DraftMessage.DraftSaveStatus, this);
        }

        public virtual bool IsValid()
        {
            return !string.IsNullOrEmpty(draftId) && draftUploadInfos != null && draftUploadInfos.Count > 0;
        }

        private void SyncDraftInfo()
        {
            if (this.vehicleInfo == null)
            {
                this.vehicleInfo = new VehicleInfo();
                this.vehicleInfo.templateId = "80000001";
                this.vehicleInfo.vehicleType = (int)VehicleSubType.SingleVehicle;
            }

            this.vehicleInfo.draftVersion = draftVersion;
            this.vehicleInfo.updateTime = updateTime;
            this.vehicleInfo.editTime = editTime;
            var remoteMetadataUrl = GetMetadataRemoteUrl();
            if (!string.IsNullOrEmpty(remoteMetadataUrl))
            {
                this.vehicleInfo.metaDataUrl = remoteMetadataUrl;
            }

            var coverRemoteUrl = GetCoverRemoteUrl();
            if (!string.IsNullOrEmpty(coverRemoteUrl))
            {
                this.vehicleInfo.cover = coverRemoteUrl;
            }
        }

        public virtual string GetMetadataRemoteUrl()
        {
            return draftUploadInfos.Find(info => info.draftName == MetaDataKey)?.remoteUrl;
        }

        public virtual string GetMetadataLocalUrl()
        {
            return draftUploadInfos.Find(info => info.draftName == MetaDataKey)?.draftPath;
        }

        /// <summary>
        /// 该接口合并了本地和远程的元数据，可能返回本地，也可能返回远端
        /// </summary>
        /// <returns></returns>
        public virtual string GetMetadataUrl()
        {
            var metaDataUrl = GetMetadataLocalUrl();
            if (string.IsNullOrEmpty(metaDataUrl) || !File.Exists(metaDataUrl))
            {
                metaDataUrl = GetMetadataRemoteUrl();
            }

            return metaDataUrl;
        }

        public virtual void SetCover(byte[] coverData)
        {
            draftVersion++;
            updateTime = GameUtils.GetTimeStamp();
            var localCoverPath = Path.Combine(GetDraftCacheFolder(), GetDraftFileName() + ".png");
            File.WriteAllBytes(localCoverPath, coverData);
            SetUploadLocalInfo(CoverKey, localCoverPath, CoverRemoteFolder);
        }

        public virtual string GetCoverRemoteUrl()
        {
            return draftUploadInfos.Find(info => info.draftName == CoverKey)?.remoteUrl;
        }

        public virtual string GetCoverLocalUrl()
        {
            return draftUploadInfos.Find(info => info.draftName == CoverKey)?.draftPath;
        }

        public virtual void SetMetaData(byte[] metaData)
        {
            draftVersion++;
            updateTime = GameUtils.GetTimeStamp();
            var localMetaDataUrl = Path.Combine(GetDraftCacheFolder(), GetDraftFileName() + ".bytes");
            File.WriteAllBytes(localMetaDataUrl, metaData);
            SetUploadLocalInfo(MetaDataKey, localMetaDataUrl, MetaDataRemoteFolder);
        }

        public void SetMapMetaData(byte[] metaData)
        {
            var localMetaDataUrl = Path.Combine(GetDraftCacheFolder(), GetDraftFileName() + ".bytes");
            File.WriteAllBytes(localMetaDataUrl, metaData);
            SetUploadLocalInfo(MapMetaKey, localMetaDataUrl, MetaDataRemoteFolder);
        }

        protected string GetDraftCacheFolder()
        {
            var cacheFolder = Application.persistentDataPath + $"/DraftCache/{GetType().Name}/";
            if (!Directory.Exists(cacheFolder))
            {
                Directory.CreateDirectory(cacheFolder);
            }

            return cacheFolder;
        }


        public string GetMapUrl()
        {
            var uploadInfo = draftUploadInfos.Find(info => info.draftName == MapMetaKey);
            return uploadInfo?.draftPath ?? uploadInfo?.remoteUrl;
        }

        public void SetVerify(byte[] verifyData)
        {
            if (verifyData == null)
            {
                LoggerUtils.LogError("SetVerify Error: verifyData == null");
                return;
            }

            draftVersion++;
            updateTime = GameUtils.GetTimeStamp();
            var localVerifyPath = Path.Combine(GetDraftCacheFolder(), GetDraftFileName() + ".png");
            if (File.Exists(localVerifyPath))
            {
                return;
            }
            File.WriteAllBytes(localVerifyPath, verifyData);
            SetUploadLocalInfo(VerifyKey, localVerifyPath, CoverRemoteFolder);
        }

        /// <summary>
        /// 获取唯一草稿文件名
        /// </summary>
        /// <returns></returns>
        public string GetDraftFileName()
        {
            return (uid + "_" + GameUtils.GetTimeStamp() + "_" + draftVersion + "_" + UnityEngine.Random.Range(100, 999))
                .GetMd5Hash();
        }

        protected void SetUploadLocalInfo(string key, string localPath, string remoteFolder)
        {
            if (string.IsNullOrEmpty(localPath) || !File.Exists(localPath))
                return;
            var uploadInfo = draftUploadInfos.Find(info => info.draftName == key);
            if (uploadInfo != null)
            {
                uploadInfo.SetDraftPath(localPath);
            }
            else
            {
                draftUploadInfos.Add(new DraftUploadInfo(key, localPath, remoteFolder));
            }
        }

        public void Cancel()
        {
            isCancel = true;
        }

        public virtual void EditDraftToServer(Action<VehicleDraftInfo, bool> callBack = null)
        {
            SyncDraftInfo();
            var req = new UGCSetRequest(this.vehicleInfo,  UGCOperationType.Edit);
            NetworkManager.Inst.SendHttpRequest<DraftListItem>(SetUrl, HttpMethod.POST, req, draftListItem =>
            {
                callBack?.Invoke(this, true);
                isSaveDraft = true;
                MessageHelper.Broadcast(DraftMessage.DraftSaveStatus, this);
            }, fail =>
            {
                Debug.LogError($"保存草稿信息失败 [{this.vehicleInfo.id}]:" + fail);
                callBack?.Invoke(this, false);
            });
        }

        /// <summary>
        /// 该接口合并了本地和远程的封面图，可能返回本地，也可能返回远端
        /// </summary>
        /// <returns></returns>
        public virtual string GetCoverUrl()
        {
            var coverUrl = GetCoverLocalUrl();
            if (string.IsNullOrEmpty(coverUrl))
            {
                coverUrl = GetCoverRemoteUrl();
            }

            if (string.IsNullOrEmpty(coverUrl))
            {
                coverUrl = this.vehicleInfo.cover;
            }

            return coverUrl;
        }

        public void ResetUploadInfo()
        {
            foreach (var draftUploadInfo in draftUploadInfos)
            {
                if (draftUploadInfo.uploadStatus != UploadStatus.SavedDraft)
                {
                    draftUploadInfo.uploadStatus = UploadStatus.NotUpload;
                }
            }
        }


        protected virtual void OnUploadCallBack(DraftUploadInfo draftUploadInfo, string remoteUrl, string err,
            Action callBack)
        {
            if (isCancel)
            {
                CheckUploadAll(callBack);
                return;
            }

            if (!string.IsNullOrEmpty(err) || string.IsNullOrEmpty(remoteUrl))
            {
                LoggerUtils.LogError(
                    $"上传草稿箱文件失败: 本地路径 {draftUploadInfo.draftPath} Err: {err} {draftUploadInfo.draftName}");
                draftUploadInfo.uploadStatus = UploadStatus.UploadFail;
            }
            else
            {
                draftUploadInfo.remoteUrl = remoteUrl;
                draftUploadInfo.uploadStatus = UploadStatus.UploadSuccess;
                RefreshUploadInfo(draftUploadInfo);
            }

            CheckUploadAll(callBack);
        }

        protected virtual void RefreshUploadInfo(DraftUploadInfo draftUploadInfo)
        {
            if (draftUploadInfo.draftName == MetaDataKey && this.vehicleInfo != null)
            {
                // 元数据不需要审核
                this.vehicleInfo.metaDataUrl = draftUploadInfo.remoteUrl;
            }
            else if (draftUploadInfo.draftName == CoverKey && this.vehicleInfo != null)
            {
                this.vehicleInfo.cover = draftUploadInfo.remoteUrl;
            }

            if (draftUploadInfo.draftName == MapMetaKey && this.vehicleInfo != null)
            {
                this.vehicleInfo.mapUrl = draftUploadInfo.remoteUrl;
            }
            else if (draftUploadInfo.draftName == VerifyKey && this.vehicleInfo != null)
            {
                this.vehicleInfo.verifyUrl = draftUploadInfo.remoteUrl;
            }

        }

        protected void CheckUploadAll(Action callBack)
        {
            bool isComplete = draftUploadInfos.All(tmp =>
                tmp.uploadStatus is UploadStatus.UploadSuccess || tmp.uploadStatus is UploadStatus.UploadFail);
            if (isComplete)
            {
                MessageHelper.Broadcast(DraftMessage.DraftSaveStatus, this);
                callBack?.Invoke();
            }
        }
        public virtual void PublishDraftToServer(Action<VehicleInfo, bool> callBack)
        {
            SyncDraftInfo();
            var ugcInfo = vehicleInfo;
            var req = new UGCVehicleSetRequest(this.vehicleInfo, UGCOperationType.Publish);
            NetworkManager.Inst.SendHttpRequest<DraftListItem>(SetUrl, HttpMethod.POST, req, draftListItem => {
                callBack(draftListItem?.vehicleInfo, true);
                isSaveDraft = true;
                MessageHelper.Broadcast(DraftMessage.DraftSaveStatus, this);
            }, rsp => {
                if (rsp.result == 501)
                {
                    // 审核失败
                    Action<bool> onAppealCallBack = (isAppeal) => {
                        // 提交申诉，视作发布成功
                        if (isAppeal)
                        {
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
                            MessageHelper.Broadcast(DraftMessage.DraftSaveStatus, this);
                        }
                        else
                        {
                            // 放弃申诉，视作发布失败
                            callBack(null, false);
                        }
                    };
                    UIAgentManager.Inst.OpenPanel(PanelId.UGCAuditRejectedPanel, WindowId.None, req, rsp.rmsg, onAppealCallBack);
                }
                else
                {
                    HttpErrorCodeHandler errorCodeHandler = new Network.Http.HttpErrorCodeHandler();
                    errorCodeHandler.HandleErrorCodeResult(rsp);
                    LoggerUtils.LogError($"发布失败 [{vehicleInfo.id}]:" + rsp.rmsg);
                    callBack(null, false);
                }
            }, null, 0, 0, true, false);



            // NetworkManager.Inst.SendHttpRequest<DraftListItem>(SetUrl, HttpMethod.POST, req, draftListItem => {
            //     callBack(draftListItem?.Get<K>(), true);
            //     isSaveDraft = true;
            //     MessageHelper.Broadcast(DraftMessage.DraftSaveStatus, this as T);
            // }, rsp => {
            //     if (rsp.result == 501) {
            //         // 审核失败
            //         Action<bool> onAppealCallBack = (isAppeal) => {
            //             // 提交申诉，视作发布成功
            //             if (isAppeal) {
            //                 callBack(ugcInfo, true);
            //                 isSaveDraft = true;
            //                 MessageHelper.Broadcast(DraftMessage.DraftSaveStatus, this as T);
            //             } else {
            //                 // 放弃申诉，视作发布失败
            //                 callBack(null, false);
            //             }
            //         };
            //         UIAgentManager.Inst.OpenPanel(PanelId.UGCAuditRejectedPanel, WindowId.None, ugcInfo, rsp.rmsg, onAppealCallBack);
            //     } else {
            //         HttpErrorCodeHandler errorCodeHandler = new Network.Http.HttpErrorCodeHandler();
            //         errorCodeHandler.HandleErrorCodeResult(rsp);
            //         LoggerUtils.LogError($"发布失败 [{ugcInfo.id}]:" + rsp.rmsg);
            //         callBack(null, false);
            //     }
            // }, null, 0, 0, true, false);


        }

        public virtual void RemoveDraftFiles()
        {
            foreach (var tmpDraftUploadInfo in draftUploadInfos)
            {
                if (!string.IsNullOrEmpty(tmpDraftUploadInfo.draftPath) && File.Exists(tmpDraftUploadInfo.draftPath))
                {
                    // 删除本地文件
                    File.Delete(tmpDraftUploadInfo.draftPath);
                }
            }
        }

        //public virtual void PublishDraftToServer(Action<SkinInfo, SkinActionInfo, bool> callBack)
        //{
        //    SyncDraftInfo();
        //    var ugcInfo = vehicleInfo;
        //    var req = new UGCSkinActionSetRequest(this.vehicleInfo, this._skinActionInfo, UGCOperationType.Publish);
        //    NetworkManager.Inst.SendHttpRequest<DraftListItem>(SetUrl, HttpMethod.POST, req, draftListItem => {
        //        callBack(draftListItem?.skinInfo, draftListItem?.skinActionInfo, true);
        //        isSaveDraft = true;
        //        MessageHelper.Broadcast(DraftMessage.DraftSaveStatus, this);
        //    }, rsp => {
        //        if (rsp.result == 501)
        //        {
        //            // 审核失败
        //            Action<bool> onAppealCallBack = (isAppeal) => {
        //                // 提交申诉，视作发布成功
        //                if (isAppeal)
        //                {
        //                    if (ugcInfo.auditInfo == null)
        //                    {
        //                        ugcInfo.auditInfo = new AuditStatus()
        //                        {
        //                            auditResult = 4
        //                        };
        //                    }
        //                    else
        //                    {
        //                        ugcInfo.auditInfo.auditResult = 4;
        //                    }
        //                    callBack(ugcInfo, this._skinActionInfo, true);
        //                    isSaveDraft = true;
        //                    MessageHelper.Broadcast(DraftMessage.DraftSaveStatus, this);
        //                }
        //                else
        //                {
        //                    // 放弃申诉，视作发布失败
        //                    callBack(null, null, false);
        //                }
        //            };
        //            UIAgentManager.Inst.OpenPanel(PanelId.UGCAuditRejectedPanel, WindowId.None, req, rsp.rmsg, onAppealCallBack);
        //        }
        //        else
        //        {
        //            HttpErrorCodeHandler errorCodeHandler = new Network.Http.HttpErrorCodeHandler();
        //            errorCodeHandler.HandleErrorCodeResult(rsp);
        //            LoggerUtils.LogError($"发布失败 [{_skinInfo.id}]:" + rsp.rmsg);
        //            callBack(null, null, false);
        //        }
        //    }, null, 0, 0, true, false);
        //}
    }
}


