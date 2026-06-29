using System;
using System.Collections;
using System.Collections.Generic;
using Game.Avatar;
using GameData.BaseInfo;
using GameData.Config;
using GameData.UGCData;
using Newtonsoft.Json;
using UnityEngine;

namespace GameData.Manager
{

    /// <summary>
    /// 剧场数据管理器,负责剧本数据加载,剧本进度保存,剧本角色自定义等
    /// 包括缓存的加载
    /// </summary>
    public class OCTheatreDataManager : GameInstance<OCTheatreDataManager>{

        public const string OCTheatreSaveKey = "OCTheatreSave"; //剧本保存数据key
        public Dictionary<string, OCTheatreInfo> ocTheatreInfoDict = new Dictionary<string, OCTheatreInfo>();
        public Dictionary<string, OCTheatreAvatarInfo> ocCharacterInfoDict = new Dictionary<string, OCTheatreAvatarInfo>();
        private OCTheatreSave ocTheatreSave;
        private readonly OCTheatreResourcePreloadHelper theatreResourcePreloadHelper = new OCTheatreResourcePreloadHelper();

        #region 资源相关

        public void LoadTheatreInfo(OCTheatreInfo theatreInfo, Action<OCTDetailInfoRuntime> onLoaded){
            if (theatreInfo == null)
            {
                onLoaded?.Invoke(null);
                return;
            }

            // 当前实例已缓存过pb/runtime时，直接复用，避免重复加载与解析。
            if (theatreInfo.detailInfoPb != null)
            {
                var runtime = theatreInfo.GetDetailInfoRuntime();
                if (runtime != null)
                {
                    onLoaded?.Invoke(runtime);
                    return;
                }
            }

            if (TryGetCachedTheatreInfo(theatreInfo, out var cachedRuntime))
            {
                onLoaded?.Invoke(cachedRuntime);
                return;
            }

            var theatreDetailData = xasset.Asset.Load(theatreInfo.metaDataUrl, typeof(TextAsset));
            var theatreDetailText = theatreDetailData?.asset as TextAsset;
            var theatreDetailBytes = theatreDetailText?.bytes;
            theatreDetailData?.Release();
            var detailInfo = CacheLoadedTheatreInfo(theatreInfo, theatreDetailBytes);
            onLoaded?.Invoke(detailInfo);   
        }

        public bool TryGetCachedTheatreInfo(OCTheatreInfo theatreInfo, out OCTDetailInfoRuntime detailInfo)
        {
            detailInfo = null;
            if (theatreInfo == null || string.IsNullOrEmpty(theatreInfo.id)) return false;

            if (!ocTheatreInfoDict.TryGetValue(theatreInfo.id, out var cachedInfo)) return false;
            if (cachedInfo == null || cachedInfo.detailInfoPb == null) return false;

            detailInfo = cachedInfo.GetDetailInfoRuntime();
            return detailInfo != null;
        }

        /// <summary>
        /// theatreInfo 已携带 detailInfoPb（如 FakeData 在内存中构建的情况）时，
        /// 直接从 PB 生成 runtime 并注册到缓存，跳过 xasset 加载。
        /// </summary>
        public OCTDetailInfoRuntime RegisterFromExistingPb(OCTheatreInfo theatreInfo)
        {
            if (theatreInfo == null || theatreInfo.detailInfoPb == null) return null;
            var runtime = theatreInfo.GetDetailInfoRuntime();
            if (runtime == null) return null;
            if (!string.IsNullOrEmpty(theatreInfo.id))
                ocTheatreInfoDict[theatreInfo.id] = theatreInfo;
            return runtime;
        }

        public xasset.AssetRequest LoadTheatreInfoRequest(OCTheatreInfo theatreInfo)
        {
            if (theatreInfo == null || string.IsNullOrEmpty(theatreInfo.metaDataUrl)) return null;
            return xasset.Asset.LoadAsync(theatreInfo.metaDataUrl, typeof(TextAsset));
        }

        public xasset.RemoteAssetRequest LoadTheatreInfoRemoteRequest(OCTheatreInfo theatreInfo)
        {
            if (theatreInfo == null || string.IsNullOrEmpty(theatreInfo.metaDataUrl)) return null;
            return xasset.Asset.LoadRemoteAssetAsync(theatreInfo.metaDataUrl);
        }

        public OCTDetailInfoRuntime CacheLoadedTheatreInfo(OCTheatreInfo theatreInfo, byte[] theatreDetailBytes)
        {
            if (theatreInfo == null || string.IsNullOrEmpty(theatreInfo.id)) return null;
            if (theatreDetailBytes == null || theatreDetailBytes.Length == 0) return null;

            var cacheInfo = theatreInfo.Clone();
            var runtime = cacheInfo.LoadDetailInfo(theatreDetailBytes);
            if (runtime == null) return null;

            ocTheatreInfoDict[theatreInfo.id] = cacheInfo;
            theatreResourcePreloadHelper.PreloadAll(cacheInfo, runtime, ocCharacterInfoDict);
            return runtime;
        }

        public void LoadCharacterInfo(
            OCTheatreGameController Controller, 
            OCTDetailInfoRuntime theatreInfo, 
            Action<bool> onLoaded
        ){
            if (Controller == null || theatreInfo == null)
            {
                onLoaded?.Invoke(false);
                return;
            }

            if (theatreInfo.allAvatars != null)
            {
                foreach(var avatar in theatreInfo.allAvatars){
                    if(ocCharacterInfoDict.TryGetValue(avatar.playerId, out var avatarInfo)){
                        var clothes = avatarInfo.avatarClothes?.Find(c => c.clothesIndex == avatar.clothesIndex)
                            ?? avatarInfo.avatarClothes?.Find(c => c.isDef == 1);
                        if (clothes == null && avatarInfo.avatarClothes?.Count > 0)
                            clothes = avatarInfo.avatarClothes[0];
                        if (clothes == null) continue;
                        Controller.LoadCharacter(avatar.playerId, clothes.clothesIndex, clothes.clothesJson);
                    }
                }
            }

            onLoaded?.Invoke(true);
        }

        public IEnumerator LoadCharacterInfoAsync(
            OCTheatreGameController controller,
            OCTDetailInfoRuntime theatreInfo,
            Action<float> onProgress,
            Action<bool> onLoaded)
        {
            if (controller == null || theatreInfo == null)
            {
                onLoaded?.Invoke(false);
                yield break;
            }

            var avatarList = theatreInfo.allAvatars;
            if (avatarList == null || avatarList.Count == 0)
            {
                onProgress?.Invoke(1f);
                onLoaded?.Invoke(true);
                yield break;
            }

            int total = avatarList.Count;
            for (int i = 0; i < total; i++)
            {
                var avatar = avatarList[i];
                if (avatar != null)
                {
                    if(ocCharacterInfoDict.TryGetValue(avatar.playerId, out var avatarInfo)){
                        var clothes = avatarInfo.avatarClothes?.Find(c => c.clothesIndex == avatar.clothesIndex)
                            ?? avatarInfo.avatarClothes?.Find(c => c.isDef == 1);
                        if (clothes == null && avatarInfo.avatarClothes?.Count > 0)
                            clothes = avatarInfo.avatarClothes[0];
                        if (clothes == null) continue;
                        controller.LoadCharacter(avatar.playerId, clothes.clothesIndex, clothes.clothesJson);
                    }
                }

                onProgress?.Invoke((i + 1f) / total);
                if ((i & 1) == 1) yield return null;
            }

            onLoaded?.Invoke(true);
        }

        public void ReleaseAll(){
            theatreResourcePreloadHelper.Clear();
            ocTheatreInfoDict.Clear();
            ocCharacterInfoDict.Clear();
            ocTheatreSave = null;
        }

        #endregion


        #region 本地自定义&存档相关

        public bool IsFirstTime(string theatreID){
            if(ocTheatreSave == null){
                LoadSaveData();
            }
            return !ocTheatreSave.theatreSave.ContainsKey(theatreID);
        }

        public void ClearProgress(string theatreID){
            if(ocTheatreSave == null){
                LoadSaveData();
            }
            ocTheatreSave.theatreSave.Remove(theatreID);
            ocTheatreSave.theatrePaths.Remove(theatreID);
            SaveSaveData();
        }

        /// <summary>
        /// 保存剧本游玩进度
        /// </summary>
        /// <param name="theatreID"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
        public bool SaveProgress(string theatreID, int progress){
            if(ocTheatreSave == null){
                LoadSaveData();
            }
            ocTheatreSave.theatreSave[theatreID] = progress;
            SaveSaveData();
            return true;
        }
        
        public bool SavePaths(string theatreID, List<int> paths){
            if(ocTheatreSave == null){
                LoadSaveData();
            }
            ocTheatreSave.theatrePaths[theatreID] = paths;
            SaveSaveData();
            return true;
        }

        public int GetProgress(string theatreID){
            if(ocTheatreSave == null) LoadSaveData();
            return ocTheatreSave.theatreSave.TryGetValue(theatreID, out var progress) ? progress : 0;
        }

        public List<int> GetPaths(string theatreID){
            if(ocTheatreSave == null) LoadSaveData();
            return ocTheatreSave.theatrePaths.TryGetValue(theatreID, out var paths) ? paths : new List<int>();
        }

        public void SaveBackground(string theatreID, string backgroundURL){
            if(ocTheatreSave == null) LoadSaveData();
            ocTheatreSave.theatreBackground[theatreID] = backgroundURL;
            SaveSaveData();
        }

        public string GetBackground(string theatreID){
            if(ocTheatreSave == null) LoadSaveData();
            return ocTheatreSave.theatreBackground.TryGetValue(theatreID, out var url) ? url : string.Empty;
        }

        /// <summary>
        /// 替换剧本中的角色
        /// </summary>
        /// <param name="theatreID"></param>
        /// <param name="avatarID"></param>
        /// <param name="avatarClothesIndex"></param>
        public void ReplaceAvatar(string theatreID, string avatarID, int avatarClothesIndex){
            if(ocTheatreSave == null){
                LoadSaveData();
            }
            ocTheatreSave.avatarReplaceDict[theatreID] = new OCAvatarReplace(){
                avatarID = avatarID,
                avatarClothesIndex = avatarClothesIndex
            };
        }

        public void LoadSaveData(){
            var saveString = PlayerPrefs.GetString(OCTheatreSaveKey); 
            if(string.IsNullOrEmpty(saveString)){
                ocTheatreSave = new OCTheatreSave();
            }else{
                ocTheatreSave = JsonConvert.DeserializeObject<OCTheatreSave>(saveString);
            }
        }

        public void SaveSaveData(){
            var saveString = JsonConvert.SerializeObject(ocTheatreSave);
            PlayerPrefs.SetString(OCTheatreSaveKey, saveString);
            PlayerPrefs.Save();
        }

        #endregion
    }
}