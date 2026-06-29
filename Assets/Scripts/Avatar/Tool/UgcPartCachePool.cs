using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace Game.AvatarTool
{

    public class UgcPartCachePool:MonoBehaviour
    {
        public class UgcPartDownloader
        {

            public enum UGCLoadState
            {
                None,
                Loading,
                Loaded
            }

            public string partZipUrl { get; set; }
            public Dictionary<string, Texture2D> avatarPartTextures;
            public UGCLoadState loadState = UGCLoadState.None;
            public UnityEvent<Dictionary<string, Texture2D>> OnComplete;
        }

        private static UgcPartCachePool _pool;

        public static bool HasInstance { get; private set; } = false;

        public static UgcPartCachePool Pool
        {
            get
            {
                if (_pool == null)
                {
                    var temp = new GameObject("UgcPartCachePool");
                    _pool = temp.AddComponent<UgcPartCachePool>();
                    DontDestroyOnLoad(temp);
                }
                return _pool;
            }
        }
        private int smallSize = 256;
        private Vector2Int texSize = new Vector2Int(256, 256);

        private Dictionary<string, UgcPartDownloader> ugcCache = new Dictionary<string, UgcPartDownloader>();
        //引用计数
        private Dictionary<string, List<GameObject>> refNodes = new Dictionary<string, List<GameObject>>();


        private void Awake()
        {
            HasInstance = true;
        }

        public void GetUgcPart(string id,string url,GameObject obj,UnityAction<Dictionary<string, Texture2D>> callback)
        {
            if (string.IsNullOrEmpty(url))
            {
                LoggerUtils.LogError($"url is null");
                callback?.Invoke(null);
            }

            if (!refNodes.ContainsKey(url))
            {
                refNodes.Add(url,new List<GameObject>());
            }
            refNodes[url].Add(obj);

            if (ugcCache.ContainsKey(url))
            {
                var loader = ugcCache[url];
                if (loader.loadState == UgcPartDownloader.UGCLoadState.Loading)
                {
                    loader.OnComplete.AddListener(callback);
                    return;
                }
                callback?.Invoke(loader.avatarPartTextures);
            }
            else
            {
                UgcPartDownloader loader = new UgcPartDownloader();
                loader.loadState = UgcPartDownloader.UGCLoadState.Loading;
                loader.OnComplete = new UnityEvent<Dictionary<string, Texture2D>>() {};
                loader.OnComplete.AddListener(callback);
                ugcCache.Add(url, loader);
                StartCoroutine(GetByte(id,url, loader));
            }
        }

        public void GetSkinPropPart(string id, string url, GameObject refNode, UnityAction<GameObject> callback) {
            if (string.IsNullOrEmpty(id)) {
                callback?.Invoke(null);
                return;
            }
            callback?.Invoke(GameObject.CreatePrimitive(PrimitiveType.Cube));
        }


        public void DisposeTexture(string url,GameObject obj)
        {
            if (!string.IsNullOrEmpty(url) && refNodes != null && refNodes.ContainsKey(url))
            {
                if (refNodes[url] != null && refNodes[url].Contains(obj))
                {
                    refNodes[url].RemoveAll(x => x == obj);
                    if (refNodes[url].Count == 0)
                    {
                        RemoveURL(url);
                    }
                }
            }
        }

        public void DisposeTexture(GameObject obj)
        {
            if (refNodes != null && refNodes.Count != 0)
            {
                foreach (var keyValue in refNodes)
                {
                    if (keyValue.Value != null && keyValue.Value.Count != 0)
                    {
                        DisposeTexture(keyValue.Key,obj);
                    }
                }
            }
        }

        private void RemoveURL(string url)
        {
            if (!string.IsNullOrEmpty(url) && ugcCache.ContainsKey(url))
            {
                var loader = ugcCache[url];
                loader.OnComplete.RemoveAllListeners();
                if (loader.avatarPartTextures != null&& loader.avatarPartTextures.Count != 0)
                {
                    foreach (var tex in loader.avatarPartTextures.Values)
                    {
                        if (tex != null)
                        {
                            UnityEngine.Object.Destroy(tex as UnityEngine.Object);
                        }
                    }
                }
                ugcCache.Remove(url);
            }
        }

        public IEnumerator GetByte(string id, string url, UgcPartDownloader loader)
        {
            if (string.IsNullOrEmpty(url))
            {
                yield break;
            }
            UnityWebRequest www = UnityWebRequest.Get(url);
            www.timeout = 45;
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
            {
                LoggerUtils.Log(www.error);
                loader.OnComplete?.Invoke(null);
                loader.OnComplete?.RemoveAllListeners();
            }
            else
            {
                var clothesTex = GetTextureByByte(www.downloadHandler.data);
                var clothesTexDic = SplitTextures(clothesTex,smallSize,UgcPartDataManager.Inst.GetUgcTextureCount(id));
                loader.avatarPartTextures = clothesTexDic;
                loader.loadState = UgcPartDownloader.UGCLoadState.Loaded;
                loader.OnComplete?.Invoke(clothesTexDic);
                loader.OnComplete?.RemoveAllListeners();
            }
        }


        private Dictionary<string,Texture2D> GetTextureByBytes(Dictionary<string,byte[]> texBytes)
        {
            Dictionary<string, Texture2D> texs = new Dictionary<string, Texture2D>();
            foreach (var kValue in texBytes)
            {
                Texture2D tex = null;
#if UNITY_ANDROID
                tex = new Texture2D(texSize.x, texSize.y, TextureFormat.ETC2_RGBA8, false);
#elif UNITY_IOS
                tex = new Texture2D(texSize.x, texSize.y, TextureFormat.PVRTC_RGBA4, false);
#endif
                tex.LoadImage(kValue.Value);
                tex.name = kValue.Key;
                texs.Add(kValue.Key, tex);
            }


            return texs;
        }
        private Texture2D GetTextureByByte(byte[] texByte)
        {

            Texture2D tex = null;
#if UNITY_ANDROID
            tex = new Texture2D(texSize.x, texSize.y, TextureFormat.ETC2_RGBA8, false);
#elif UNITY_IOS
                tex = new Texture2D(texSize.x, texSize.y, TextureFormat.PVRTC_RGBA4, false);
#endif
            tex.LoadImage(texByte);
            return tex;
        }
        public static Dictionary<string, Texture2D> SplitTextures(Texture2D texture, int smallSize, int texCount)
        {
            try
            {
                var textures = new Dictionary<string, Texture2D>();
                int numRows = texture.height / smallSize;
                int numCols = texture.width / smallSize;
                int index = 0;
                bool isBreak = false;
                for (int i = 0; i < numRows; i++)
                {
                    for (int j = 0; j < numCols; j++)
                    {
                        var smallTexture = new Texture2D(smallSize, smallSize , TextureFormat.RGBA32 ,false);
                        smallTexture.SetPixels(texture.GetPixels(j * smallSize, i * smallSize, smallSize, smallSize));
                        smallTexture.Apply();
                        smallTexture.name = $"{index / 2 + 1}{(index % 2 == 0 ? "" : "_alpha")}";
                        textures.Add($"{index / 2 + 1}{(index % 2 == 0 ? "" : "_alpha")}", smallTexture);
                        index++;
                        if (index >= texCount)
                        {
                            isBreak = true;
                            break;
                        }
                    }
                    if (isBreak)
                    {
                        break;
                    }
                }
                UnityEngine.Object.Destroy(texture);
                return textures;
            }
            catch (Exception msg)
            {
                Debug.LogError("SplitTextures:" + msg.Message);
                return null;
            }
        }

        private void OnDestroy()
        {
            HasInstance = false;
        }


    }
}
