// @Author: YangJie
// @Description:
// @Date:  2023/07/18
// @Modify:

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Game.Base;
using GameData;
using GameData.Base;
using GameData.MapData;
using GameData.UGCData;
using Message;
using UnityEngine;
using UnityEngine.Networking;

namespace Game.Base
{
    public abstract class BaseEnterModelController
    {

        protected bool isInitialized = false;
        protected virtual void Init()
        {
            AutoInitManager();
            isInitialized = true;
        }

        public virtual void Release()
        {
            isInitialized = false;
            GameInstanceManager.Release();
        }

        public virtual void Start(UgcBaseInfo baseInfo)
        {
            if (!isInitialized)
            {
                Init();
            }
        }


        /// <summary>
        /// 加载成功元数据后的回调
        /// </summary>
        /// <param name="metaDataBytes"></param>
        protected virtual bool OnLoadMetaData(byte[] metaDataBytes)
        {
            return true;
        }

        /// <summary>
        /// 加载远端地图元数据
        /// </summary>
        /// <param name="metaDataUrl"></param>
        protected virtual void LoadRemoteMetaData(string metaDataUrl)
        {
            Debug.Log("LoadRemoteMapMetaData:" + metaDataUrl);
            MessageHelper.Broadcast(MessageName.StartLoadRemoteMetadata);
            new AssetLoader().DownloadAsset(metaDataUrl.Replace('\\', '/'), (byte[] metaDataBytes) =>
            {
                MessageHelper.Broadcast(MessageName.OverLoadRemoteMetadata);
                OnLoadMetaData(metaDataBytes);
            }, errorCode =>
            {
                MessageHelper.Broadcast(MessageName.OverLoadRemoteMetadata);
                LoggerUtils.LogError("远端数据加载失败:" , errorCode);
                GameController.ExitGame("远端数据加载失败:" + errorCode);
            });
        }


        protected virtual void AutoInitManager()
        {
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes().Where(t => typeof(IAutoInit).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract))
                .ToArray();
            foreach (var tmpType in types)
            {
                var inst = tmpType.BaseType?.GetMethod("get_Inst", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy)?.Invoke(null, null);
                var initMethod = tmpType.GetMethods(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault(tmp => tmp.Name == "Init" && tmp.GetParameters().Length == 0);
                if (initMethod != null && inst != null)
                {
                    initMethod.Invoke(inst, null);
                }
            }
        }
        
        protected void PreloadPhoto(List<UGCPartData> partDatas, Action<int> callback)
        {

            int downCount = 0;
            int failCount = 0;
            Action<int> complete = callback;
            List<string> photoUrls = new List<string>();
            if (partDatas != null)
            {
                for (var i = 0; i < partDatas.Count; i++)
                {
                    var partData = partDatas[i];
                    if (partData.photos != null)
                    {
                        var urls = partData.photos.Select(x => x.photoUrl).Where(x => !string.IsNullOrEmpty(x));
                        photoUrls.AddRange(urls);
                    }
                }
            }

            if (photoUrls.Count == 0)
            {
                complete?.Invoke(failCount);
                return;
            }

            for (var i = 0; i < photoUrls.Count; i++)
            {
                var warpper = Loader.LoadRemoteImageAsync(photoUrls[i]);
                warpper.completed = success =>
                {
                    downCount++;
                    if (!success)
                    {
                        failCount++;
                    }

                    if (downCount == photoUrls.Count)
                    {
                        complete?.Invoke(failCount);
                    }
                };
            }
        }

    }
}
