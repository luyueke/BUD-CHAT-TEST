using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.COSXML.Auth;
using Game.COSXML.CosException;
using Game.COSXML.Transfer;
using UnityEngine;

namespace Game.COSXML
{
    public static class CosXmlUploadManager
    {
        public static string PackageType = "";
        #region 国服配置
        private const string HotUpdateBucket = "u3d-hotupdate-data-1318932159";
        private const string BusinessBucket = "u3d-business-data-1318932159";
        /// <summary>客户端本地日志上报桶（与业务/热更桶隔离）。</summary>
        public const string ClientLogBucket = "u3d-log-1318932159";
        // 腾讯云 SecretId
        private const string SecretId = "";
        // 腾讯云 SecretKey
        private const string SecretKey = "cGsgO3eFkAWmw0ztgO0VvFVhnokaL0a4";
        // 存储桶所在地域
        private const string Region = "ap-beijing";
        private const string AccRegion = "accelerate";
        #endregion
      
        
        #region 海外服配置
        private const string HotUpdateBucket_US = "us-hotupdate-1318932159";
        private const string BusinessBucket_US = "us-business-1318932159";
        // 腾讯云 SecretId
        private const string SecretId_US = "";
        // 腾讯云 SecretKey
        private const string SecretKey_US = "Xz0ujPvGPOCqJdmusfJn6uX0Kl3A7vdE";
        private const string AccRegion_US = "na-siliconvalley";
        #endregion
        
        private static CosXmlServer cosXml;
        private static TransferManager transferManager = null;
        /// <summary>未开全球加速的桶（如客户端日志桶）走地域域名，避免 BucketAccelerateNotEnabled。</summary>
        private static CosXmlServer cosXmlRegional;
        private static TransferManager transferManagerRegional;

        private static readonly List<COSXMLUploadInfo> UploadingInfos = new List<COSXMLUploadInfo>();
        private static readonly Queue<COSXMLUploadInfo> PreUploadedInfos = new Queue<COSXMLUploadInfo>();
        private static bool isSetupCalled;
        private static bool isQuitting;

        public delegate void OnUploadSuccess(string url, string err);

        public delegate void OnUploadProgress(float progress);

        private const int UploadMaxCount = 20;

        private static string GetHotUpdateBucket()
        {
            if (PackageType == "US")
            {
                Debug.LogError("###GetHotUpdateBucket 海外地址");
                return HotUpdateBucket_US;
            }
#if PACKAGE_TYPE_US
            Debug.LogError("###GetHotUpdateBucket 海外地址");
            return HotUpdateBucket_US;
#else
           // Debug.LogError("###GetHotUpdateBucket 国服地址");
            return HotUpdateBucket;
#endif
        }

        private static string GetBusinessBucket()
        {
            if (PackageType == "US")
            {
                Debug.LogError("###GetBusinessBucket 海外地址");
                return BusinessBucket_US;
            }
#if PACKAGE_TYPE_US
            Debug.LogError("###GetBusinessBucket 海外地址");
            return BusinessBucket_US;
#else
           // Debug.LogError("###GetBusinessBucket 国服地址");
            return BusinessBucket;
#endif
        }

        private static string GetSecretId()
        {
            if (PackageType == "US")
            {
                Debug.LogError("###GetSecretId 海外地址");
                return SecretId_US;
            }
#if PACKAGE_TYPE_US
            Debug.LogError("###GetSecretId 海外地址");
            return SecretId_US;
#else
        //    Debug.LogError("###GetSecretId 国服地址");
            return SecretId;
#endif
        }
        
        private static string GetSecretKey()
        {
            if (PackageType == "US")
            {
                Debug.LogError("###GetSecretKey 海外地址");
                return SecretKey_US;
            }
#if PACKAGE_TYPE_US
            Debug.LogError("###GetSecretKey 海外地址");
            return SecretKey_US;
#else
        //    Debug.LogError("###GetSecretKey 国服地址");
            return SecretKey;
#endif
        }


        public static string GetBusinessRootUrl()
        {
            string bucket = GetBusinessBucket();
            return $"https://{bucket}.cos.{AccRegion}.myqcloud.com";
        }


        public static string GetBucketRootUrl(string bucket)
        {
            if (bucket == ClientLogBucket)
                return $"https://{bucket}.cos.{Region}.myqcloud.com";
            string businessBucket = GetBusinessBucket();
            return bucket == businessBucket ? GetBusinessRootUrl() : $"https://{bucket}.cos.{AccRegion}.myqcloud.com";
        }

        private static bool UseRegionalEndpointForBucket(string bucket)
        {
            return bucket == ClientLogBucket;
        }


        public static void Setup()
        {
            if (isSetupCalled)
                return;
            isSetupCalled = true;
            isQuitting = false;
            CosXmlUpdateDelegator.CheckInstance();
            CosXmlConfig config = new CosXmlConfig.Builder()
                .SetEndpointSuffix("cos.accelerate.myqcloud.com")
                .SetDebugLog(true)
                .Build();
            long keyDurationSecond = 600;
            string secretId = GetSecretId();
            string secretKey = GetSecretKey();
            QCloudCredentialProvider qCloudCredentialProvider = new DefaultQCloudCredentialProvider(secretId, secretKey, keyDurationSecond);
            // service 初始化完成
            cosXml = new CosXmlServer(config, qCloudCredentialProvider);
            var transferConfig = new TransferConfig();
            transferManager = new TransferManager(cosXml, transferConfig);

            CosXmlConfig configRegional = new CosXmlConfig.Builder()
                .SetRegion(Region)
                .SetDebugLog(true)
                .Build();
            cosXmlRegional = new CosXmlServer(configRegional, qCloudCredentialProvider);
            transferManagerRegional = new TransferManager(cosXmlRegional, transferConfig);
        }

        public static void ResetSetup()
        {
            isSetupCalled = false;
            Shutdown();
            Debug.Log("CosXmlUploadManager:" + "Reset called!");
        }


        public static void UploadDirectory(string uploadUri, string srcPath, OnUploadSuccess callBack = null, OnUploadProgress progressCallBack = null)
        {
            string bucket = GetBusinessBucket();
            UploadDirectory(bucket, uploadUri, srcPath, callBack, progressCallBack);
        }


        public static void UploadDirectoryToHotUpdate(string uploadUri, string srcPath, OnUploadSuccess callBack = null, OnUploadProgress progressCallBack = null)
        {
            string bucket = GetHotUpdateBucket();
            UploadDirectory(bucket, uploadUri, srcPath, callBack, progressCallBack);
        }


        private static void UploadDirectory(string bucket, string uploadUri, string srcPath, OnUploadSuccess callBack = null, OnUploadProgress progressCallBack = null)
        {
            if (!Directory.Exists(srcPath))
            {
                callBack?.Invoke(Path.Combine(GetBucketRootUrl(bucket), uploadUri), "文件夹不存在");
                return;
            }
            if (!isSetupCalled)
            {
                Setup();
            }

            if (isQuitting)
            {
                return;
            }

            var files = GetFiles(srcPath);
            if (files.Count == 0) {
                callBack?.Invoke(Path.Combine(GetBucketRootUrl(bucket), uploadUri), "文件夹内容为空");
                return;
            }
            var tmpUploadInfos = new List<COSXMLUploadInfo>();
            uploadUri = uploadUri.TrimEnd('\\', '/');
            var uri = uploadUri;
            foreach (var tmpFile in files)
            {
                var targetUri = uploadUri + '/' + tmpFile.Replace(srcPath, "").TrimStart('\\', '/');
                targetUri = targetUri.Replace('\\', '/');
                var tmpUploadInfo = UploadFile(bucket, targetUri, tmpFile, (remoteUrl, err) =>
                {
                    var completeCount = tmpUploadInfos.Count(tmp => tmp == null || tmp.Progress >= 1);
                    if (completeCount >= files.Count)
                    {
                        var errorUploadInfos = tmpUploadInfos.Where(tmp => tmp != null && !string.IsNullOrEmpty(tmp.Error)).Select(tmp =>
                            new Tuple<string, string>(tmp.SrcPath, tmp.Error));

                        string errorJson = null;
                        if (errorUploadInfos.Any())
                        {
                            var parts = errorUploadInfos.Select(e =>
                                $"[\"{e.Item1.Replace("\"", "\\\"")}\",\"{e.Item2.Replace("\"", "\\\"")}\"]");
                            errorJson = "[" + string.Join(",", parts) + "]";
                        }
                        callBack?.Invoke(Path.Combine(GetBucketRootUrl(bucket), uri), errorJson);
                    }
                }, (progress) =>
                {
                    var allProgress = tmpUploadInfos.Sum(tmp => tmp.Progress);
                    progressCallBack?.Invoke(allProgress / files.Count);
                });
                tmpUploadInfos.Add(tmpUploadInfo);
            }

        }


        private static COSXMLUploadInfo UploadFile(string bucket, string uploadUri, string srcPath, OnUploadSuccess callBack = null,
            OnUploadProgress progressCallBack = null)
        {
            if (!File.Exists(srcPath))
            {
                callBack?.Invoke(null, "文件不存在:" + srcPath);
                return null;
            }

            if (!isSetupCalled)
            {
                Setup();
            }

            if (isQuitting)
            {
                callBack?.Invoke(null, "已经退出应用");
                return null;
            }

            COSXMLUploadInfo uploadInfo = null;

            uploadInfo = UploadingInfos.Find(tmp => tmp.SrcPath == srcPath) ?? PreUploadedInfos.FirstOrDefault(tmp => tmp.SrcPath == srcPath);

            if (uploadInfo != null)
            {
                uploadInfo.CompleteCallBack += callBack;
                uploadInfo.ProgressCallBack += progressCallBack;
                return uploadInfo;
            }
            uploadInfo = new COSXMLUploadInfo
            {
                SrcPath = srcPath,
                UploadUri = uploadUri,
                CompleteCallBack = callBack,
                ProgressCallBack = progressCallBack,
                Bucket = bucket
            };

            var uploadTask = new COSXMLUploadTask(bucket, uploadUri);
            uploadTask.SetSrcPath(srcPath);
            uploadTask.failCallback = delegate(CosClientException exception, CosServerException serverException)
            {
                if (exception != null && !string.IsNullOrEmpty(exception.Message)) {
                    uploadInfo.Error = exception.Message;
                } else if (serverException != null && !string.IsNullOrEmpty(serverException.errorMessage)) {
                    Debug.LogError("UploadFile Fail:" + serverException.GetInfo());
                    uploadInfo.Error = serverException.Message;
                } else {
                    uploadInfo.Error = "上传失败";
                }

            };
            uploadTask.progressCallback = delegate(long completed, long total)
            {
                uploadInfo.Progress = (float)completed / total;
            };
            uploadInfo.Task = uploadTask;
            PreUploadedInfos.Enqueue(uploadInfo);
            return uploadInfo;
        }

        /**
         * 上传文件
         */
        public static COSXMLUploadInfo UploadFile(string uploadUri, string srcPath, OnUploadSuccess callBack = null, OnUploadProgress progressCallBack = null)
        {
            string bucket = GetBusinessBucket();
            return UploadFile(bucket, uploadUri, srcPath, callBack, progressCallBack);
        }

        
        public static COSXMLUploadInfo UploadFileOnEditor(string uploadUri, string srcPath, OnUploadSuccess callBack = null, OnUploadProgress progressCallBack = null)
        {
            string bucket = GetHotUpdateBucket();
            return UploadFile(bucket, uploadUri, srcPath, callBack, progressCallBack);
        }

        /// <summary>上传至客户端日志桶 <see cref="ClientLogBucket"/>，uploadUri 为 COS 对象键（如 client-log/{uid}/{ts}/xxx.log）。</summary>
        public static COSXMLUploadInfo UploadFileToClientLogBucket(string uploadUri, string srcPath,
            OnUploadSuccess callBack = null, OnUploadProgress progressCallBack = null)
        {
            return UploadFile(ClientLogBucket, uploadUri, srcPath, callBack, progressCallBack);
        }

        internal static void OnUpdate()
        {
            for (int i = UploadingInfos.Count - 1; i >= 0; i--)
            {
                var uploadInfo = UploadingInfos[i];
                if (uploadInfo.Task.State() == TaskState.Completed)
                {
                    UploadingInfos.RemoveAt(i);
                    uploadInfo.CompleteCallBack?.Invoke(Path.Combine(GetBucketRootUrl(uploadInfo.Bucket), uploadInfo.UploadUri).Replace('\\', '/'), null);
                }
                else if (uploadInfo.Task.State() == TaskState.Failed || uploadInfo.Task.State() == TaskState.Cancel)
                {
                    UploadingInfos.RemoveAt(i);
                    uploadInfo.CompleteCallBack?.Invoke(null, uploadInfo.Error);
                }
                else
                {
                    uploadInfo.ProgressCallBack?.Invoke(uploadInfo.Progress);
                }
            }

            while (UploadingInfos.Count <= UploadMaxCount && PreUploadedInfos.Count > 0)
            {
                var tmpUploadInfo = PreUploadedInfos.Dequeue();
                try
                {
                    var tm = UseRegionalEndpointForBucket(tmpUploadInfo.Bucket)
                        ? transferManagerRegional
                        : transferManager;
                    tm.Upload(tmpUploadInfo.Task);
                }
                catch (Exception e)
                {
                    tmpUploadInfo.Task.Cancel();
                    tmpUploadInfo.Progress = 1;
                    tmpUploadInfo.Error = "用户取消";
                    Debug.LogError("CosException:" + e);
                }
                UploadingInfos.Add(tmpUploadInfo);
            }

        }
        internal static void Shutdown()
        {
            foreach (var uploadInfo in UploadingInfos)
            {
                uploadInfo.Task.Cancel();
            }
            UploadingInfos.Clear();
            cosXml = null;
            cosXmlRegional = null;
            transferManagerRegional = null;
        }

        internal static List<string> GetFiles(string dir)
        {
            var files = new List<string>();
            if (!Directory.Exists(dir))
            {
                return files;
            }
            var dirInfo = new DirectoryInfo(dir);
            var fileInfos = dirInfo.GetFiles();
            foreach (var fileInfo in fileInfos)
            {
                files.Add(Path.Combine(dir, fileInfo.Name));
            }
            var dirInfos = dirInfo.GetDirectories();
            foreach (var subDirInfo in dirInfos)
            {
                files.AddRange(GetFiles(Path.Combine(dir, subDirInfo.Name)));
            }
            return files;
        }



    }
}
