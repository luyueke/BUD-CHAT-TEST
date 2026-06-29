using System;
using System.Collections.Generic;
using Es;
using Game.Config;
using GameData.Manager;
using UnityEngine;

namespace xasset
{
    public class UGCPartLoader
    {
        public static void LoadUGCPartRemoteImageAsync(int ugcType,int textureCount,  string path,Action<bool,UGCTempPartRemoteWrapper> callback)
        {
            string originPath = path;
            var sizeInfo =  DataTables.GetUgcScreenGraphics(ugcType.ToString());
            int size = 128;
            if (sizeInfo != null)
            {
                size = GameDataManager.Inst.IsLowMobile() ? sizeInfo.LSize : sizeInfo.HSize;
            }
            string downloadPath = GetUGCPartDownloadUrl(path, size, textureCount);
           
            var request =  UGCTempPartRemoteRequest.Load(textureCount,size, downloadPath, originPath);
            if (request == null)
            {
                callback?.Invoke(false,null);
                return ;
            }
            request.completed += req =>
            {
                if (req.result == Request.Result.Failed)
                {
                    //if (req.error.Contains("destination host"))
                    {
                        LoadUGCPartRemoteImageAsyncByOriginUrl(textureCount,path,callback);
                    }
                }
                else
                {
                    var wrapper = new UGCTempPartRemoteWrapper(request);
                    callback?.Invoke(true,wrapper);
                }
            };
        }

        public static void LoadUGCPartRemoteImageAsyncByOriginUrl(int textureCount,string path,Action<bool,UGCTempPartRemoteWrapper> callback = null)
        {

            string downloadPath = GetUGCPartOriginDownloadUrl(path);
            int originSize = 256;
            var request =  UGCTempPartRemoteRequest.Load(textureCount,originSize, downloadPath, path);
            if (request == null)
            {
                callback?.Invoke(false,null);
                return;
            }
            request.completed += req =>
            {
                if (req.result == Request.Result.Failed)
                {
                    Debug.LogError("ugcpart texture origin download fail! msg:" + req.error);
                    if (req.error.Contains("destination host"))
                    {
                        Message.MessageHelper.Broadcast(Message.MessageName.ShowToast, "当前网络环境存在异常，可以试下切换其他的网络环境T^T");
                    }
                }
                var wrapper = new UGCTempPartRemoteWrapper(request);
                callback?.Invoke(req.result == Request.Result.Success,wrapper);
            };
        }


        public static string GetUGCPartDownloadUrl(string url,int size, int textureCount)
        {

            if (url.EndsWith(".png"))
            {
                Vector2Int textureSize = GetUGCPartTextureSize(textureCount, new Vector2Int(size, size));
                try
                {
                    Uri uri = new Uri(url);
                    var queryAndPath = uri.PathAndQuery;
                    //TODO:实例
                    //https://cdn.budapp.cn/UgcClothes/1816013150517530624/BF97E4B80315763A2ABF84EFFF445FFC.png?imageMogr2/thumbnail/500x500/UgcClothes/1816013150517530624/BF97E4B80315763A2ABF84EFFF445FFC.png
                    //https://cdn-global.joinbudapp.com/UgcClothes/1846151399680081920/184C1B055828C88CE3342D8FA73EBF2C.png?imageMogr2/thumbnail/512x512/UgcClothes/1846151399680081920/184C1B055828C88CE3342D8FA73EBF2C.png
                    url = GameConsts.BusinessCdnHost + queryAndPath + "?imageMogr2/thumbnail/" + textureSize.x + "x" + textureSize.y + queryAndPath;
                }
                catch (Exception e)
                {
                    LoggerUtils.LogError("LoadUGCPartRemoteImageAsync URL is invalid: " + url + ", " + e.Message);
                }
            }
            return url;
        }

        public static string GetUGCPartOriginDownloadUrl(string url)
        {
            if (url.EndsWith(".png"))
            {
                try
                {
                    Uri uri = new Uri(url);
                    var queryAndPath = uri.PathAndQuery;
                    url = GameConsts.BusinessBaseHost +  queryAndPath;
                    url = url.Replace(GameConsts.BusinessBaseHost, GameConsts.BusinessCdnUrl);
                }
                catch (Exception e)
                {
                    LoggerUtils.LogError("LoadUGCPartRemoteImageAsync Origin URL is invalid: " + url + ", " + e.Message);
                }
            }
            return url;
        }


        public static Vector2Int GetUGCPartTextureSize(int textureCount, Vector2Int baseSize)
        {
            int sizeX =  Mathf.NextPowerOfTwo(Mathf.CeilToInt(Mathf.Sqrt(textureCount)));
            int sizeY = sizeX;
            if (textureCount <= Mathf.Pow(sizeY, 2) / 2)
            {
                sizeY /= 2;
            }
            var size = new Vector2Int(sizeX * baseSize.x, sizeY*baseSize.y);
            return size;
        }

        public static int GetSmallSize(Vector2Int bigTextureSize, int smallTextureCount)
        {
            int sizeX = Mathf.NextPowerOfTwo(Mathf.CeilToInt(Mathf.Sqrt(smallTextureCount)));
            return bigTextureSize.x / sizeX;
        }
    }

    public class UGCTempPartRemoteWrapper
    {

        public RemoteImageRequest request;
        public Action<bool> completed;

        public UGCTempPartRemoteWrapper(UGCTempPartRemoteRequest _request)
        {
            request = _request;
            request.completed += OnCompleted;
        }

        private void OnCompleted(Request req)
        {
            try
            {
                if (req.result == Request.Result.Failed)
                {
                    Debug.LogError("ugcpart texture download fail! msg:" + req.error);
                    if (req.error.Contains("destination host"))
                    {
                        Message.MessageHelper.Broadcast(Message.MessageName.ShowToast, "当前网络环境存在异常，可以试下切换其他的网络环境T^T");
                    }
                }
                // 修复一下材质shader
                completed?.Invoke(req.result == Request.Result.Success);
            }
            catch (Exception e)
            {
                LoggerUtils.LogError("Error", e.StackTrace);
            }
            completed = null;
        }

        /// <summary>
        /// 取用资源
        /// </summary>
        /// <param name="user"> 资源的使用者 </param>
        /// <returns></returns>
        public Texture RetainAsset(GameObject user)
        {
            if (user == null) return null;
            if (request.asset == null) return null;
            var clone = Clone(request);
            AutoreleaseCache.Get(user).Add(clone);
            return request.asset;
        }

        public Dictionary<string, Texture> RetainAssets(GameObject user)
        {
            if (user == null) return null;
            if (request.assets == null) return null;
            var clone = Clone(request);
            AutoreleaseCache.Get(user).Add(clone);
            return request.assets;
        }

        ~UGCTempPartRemoteWrapper()
        {
            // 如果加载了没有用到 GC的时候释放request
            if (request != null) request.Release();
        }

        protected RemoteImageRequest Clone(RemoteImageRequest req) {
            var remoteRequest = req as UGCTempPartRemoteRequest;
            return UGCTempPartRemoteRequest.Load(remoteRequest.textureCount,remoteRequest.texSize, remoteRequest.path, remoteRequest.originPath);
        }

        public string GetPath()
        {
            var remoteRequest = request as UGCTempPartRemoteRequest;
            if (remoteRequest == null)
            {
                return null;
            }
            if (string.IsNullOrEmpty(remoteRequest.originPath))
            {
                return remoteRequest.path;
            }
            else
            {
                return remoteRequest.originPath;
            }
        }
    }
}
