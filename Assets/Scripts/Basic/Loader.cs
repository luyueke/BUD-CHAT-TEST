
using System;
using System.Collections;
using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using xasset;
using Object = UnityEngine.Object;

public class Loader
{
    public static AssetWrapper<T> Load<T>(string path) where T : Object
    {
        var request = Asset.Load(path, typeof(T));

        if (request == null) return null;

        return new AssetWrapper<T>(request);
    }

    public static T Load<T>(string path, GameObject obj) where T : Object
    {
        var wrapper = Load<T>(path);
        return wrapper?.RetainAsset(obj);
    }


    public static AssetWrapper<T> LoadAsync<T>(string path) where T : Object
    {
        var request = Asset.LoadAsync(path, typeof(T));

        if (request == null) return null;

        return new AssetWrapper<T>(request);
    }

    public static void LoadAsync<T>(string path,Action<bool,AssetWrapper<T>> callback) where T : Object
    {
        var request = Asset.LoadAsync(path,  typeof(T));
        if (request == null) {
            callback?.Invoke(false, null);
            return;
        }
        var wrapper = new AssetWrapper<T>(request);
        wrapper.completed = (isSuc)=>
        {
            callback(isSuc,wrapper);
        };
    }


    public static void LoadAsyncOrSync<T>(string path,Action<bool,AssetWrapper<T>> callback) where T : Object
    {
        if (Assets.IsDownloaded(path))
        {
            var request = Asset.Load(path,  typeof(T));
            if (request == null) {
                callback?.Invoke(false, null);
                return;
            }
            var wrapper = new AssetWrapper<T>(request);
            callback?.Invoke(request.result == Request.Result.Success, wrapper);
        }
        else
        {
            var request = Asset.LoadAsync(path,  typeof(T));
            if (request == null) {
                callback?.Invoke(false, null);
                return;
            }
            var wrapper = new AssetWrapper<T>(request);
            wrapper.completed = (isSuc)=>
            {
                callback(isSuc,wrapper);
            };
        }
    }
    
    public static void LoadAsyncOrSync<T>(string path, GameObject user, Action<T> callback) where T : Object
    {
        if (xasset.Assets.IsDownloaded(path))
        {
            var wrapper = Load<T>(path);
            if (wrapper != null)
            {
                var res = wrapper.RetainAsset(user);
                callback?.Invoke(res);
            }
            return;
        }
        var request = LoadAsync<T>(path);
        if (request == null)
        {
            LoggerUtils.LogError("request == null :", path);
            return;
        }
        request.completed = suc =>
        {
            if (suc)
            {
                var res = request.RetainAsset(user);
                callback?.Invoke(res);
            }
            else
            {
                Debug.LogError($"{path} load failed {request.request.error}");
                callback?.Invoke(null);
            }
        };
    }


    public static RemoteImageWrapper LoadRemoteImageAsync(string path)
    {
        var request = Asset.LoadRemoteImageAsync(path);

        if (request == null) return null;

        return new RemoteImageWrapper(request);
    }


    public static Texture LoadRemoteImage(string path, GameObject obj)
    {
        var request = Asset.LoadRemoteImage(path);

        if (request == null) return null;

        return new RemoteImageWrapper(request).RetainAsset(obj);
    }

    public static SceneRequest LoadAsync(string path, bool withAdditive = false)
    {
        return Scene.LoadAsync(path, withAdditive);
    }

    public static IEnumerator LoadWwiseAsync(List<string> paths, Action<bool> callback)
    {
        var request = xasset.Assets.LoadWwiseAsync(paths);
        yield return request;
        if (request.downloadSize != 0)
        {
            var result = request.DownloadAsync();
            yield return result;

            if (result.result == DownloadRequest.Result.Success)
            {
                callback?.Invoke(true);
            }
            else
            {
                callback?.Invoke(false);
            }
        }
        else
        {
            callback?.Invoke(true);
        }
    }


    public static void ClearCache(GameObject user)
    {
        AutoreleaseCache.Get(user).Clear();
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
}

public class AssetWrapper<T> where T : Object
{
    public AssetRequest request;
    public Action<bool> completed;
    private bool IsUsed = false;

    public AssetWrapper(AssetRequest _request)
    {
        request = _request;
        request.completed += OnCompleted;
    }

    public T Instantiate(Transform parent = null, bool instantiateInWorldSpace = false)
    {
        if (request.asset == null) return default;
        var clone = Clone(request);
        var obj = (T)Object.Instantiate(clone.asset, parent, instantiateInWorldSpace);
#if UNITY_EDITOR
        FixMat(obj);
#endif
        AutoreleaseCache.Get(obj).Add(clone);
        return obj;
    }

    private void OnCompleted(Request req)
    {
        // 修复一下材质shader
        completed?.Invoke(req.result == Request.Result.Success);
        if (req.error != null && req.error.Contains("System out of memory"))
        {
            Message.MessageHelper.Broadcast(Message.MessageName.ShowToast, "手机内存已不足，请及时前往设置清理缓存。 这将有助于提升游戏的流畅度，确保您在游戏中的正常体验。");
        }
        completed = null;
    }

    private void FixMat(T asset)
    {
        if (typeof(T) == typeof(Material))
        {
            if (asset) RefreshMaterial(asset as Material);
        }
        if (typeof(T) == typeof(GameObject))
        {
            if (asset) RefreshMaterial(asset as GameObject);
        }
    }

    /// <summary>
    /// 取用资源
    /// </summary>
    /// <param name="user"> 资源的使用者 </param>
    /// <returns></returns>
    public T RetainAsset(GameObject user)
    {
        if (user == null) return null;
        if (request.asset == null) return null;
        var clone = Clone(request);
#if UNITY_EDITOR
        FixMat((T)request.asset);
#endif
        AutoreleaseCache.Get(user).Add(clone);
        return (T)request.asset;
    }
    /// <summary>
    ///
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    public T RetainAsset()
    {
        if (request.asset == null) return null;
        return (T)request.asset;
    }

    ~AssetWrapper()
    {
        // 如果加载了没有用到 GC的时候释放request
        if (request != null) request.Release();
    }

    private AssetRequest Clone(AssetRequest request)
    {
        return request.isAll ? Asset.LoadAll(request.path, request.type) : Asset.Load(request.path, request.type);
    }

    private HashSet<int> matHash = new HashSet<int>();
    private void RefreshMaterial(GameObject obj)
    {
        var comps = obj.GetComponentsInChildren<Renderer>(true);
        for (int i = 0, Li = comps.Length; i < Li; i++)
        {
            var comp = comps[i];
            var list = new List<Material>();
            comp.GetSharedMaterials(list);
            RefreshMaterial(list.ToArray());
        }
    }

    private void RefreshMaterial(Material mat)
    {
        var hashCode = mat.GetHashCode();
        if (!matHash.Contains(mat.GetHashCode()))
        {
            var useful = Shader.Find(mat.shader.name);
            if (useful)
            {
                if (useful != mat.shader) mat.shader = useful;
                matHash.Add(hashCode);
            }
        }
    }

    private void RefreshMaterial(Material[] mats)
    {
        for (int i = 0, L = mats.Length; i < L; i++)
        {
            if (mats[i]) RefreshMaterial(mats[i]);
        }
    }

    public string GetAssetName()
    {
        if (request != null && request.asset != null)
            return request.asset.name;
        return "";
    }
}

public class RemoteImageWrapper
{
    public RemoteImageRequest request;
    public Action<bool> completed;

    public RemoteImageWrapper(RemoteImageRequest _request)
    {
        request = _request;
        request.completed += OnCompleted;
    }

    private void OnCompleted(Request req)
    {
        try
        {
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

    ~RemoteImageWrapper()
    {
        // 如果加载了没有用到 GC的时候释放request
        if (request != null) request.Release();
    }

    protected virtual RemoteImageRequest Clone(RemoteImageRequest request)
    {
        return Asset.LoadRemoteImageAsync(request.path);
    }

    public virtual string GetPath()
    {
        return request.path;
    }
}
