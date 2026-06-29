using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Pointone.Sound;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class XAssetAudioLoader
{
    private static readonly bool s_checked;
    private static readonly bool s_hasXAsset;
    private static readonly Type s_assetType;
    private static readonly MethodInfo s_assetLoadMethod;
    private static readonly MethodInfo s_assetLoadAsyncMethod;

    private static readonly Dictionary<string, AudioClip> s_clipCache = new Dictionary<string, AudioClip>();
    private static readonly Dictionary<string, object> s_pendingRequests = new Dictionary<string, object>();
    private static PropertyInfo s_isDoneProp;
    private static PropertyInfo s_assetProp;

    public static void ClearCache()
    {
        s_clipCache.Clear();
        s_pendingRequests.Clear();
    }

    public static void RemoveFromCache(string path)
    {
        if (string.IsNullOrEmpty(path)) return;
        s_clipCache.Remove(path);
        s_pendingRequests.Remove(path);
    }

    public static int CacheCount => s_clipCache.Count;

    static XAssetAudioLoader()
    {
        try
        {
            var asmList = AppDomain.CurrentDomain.GetAssemblies();
            s_assetType = asmList.Select(a => a.GetType("xasset.Asset", false)).FirstOrDefault(t => t != null);
            if (s_assetType != null)
            {
                s_assetLoadMethod = s_assetType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(m => m.Name == "Load" && 
                                        m.GetParameters().Length == 2 &&
                                        m.GetParameters()[0].ParameterType == typeof(string) &&
                                        m.GetParameters()[1].ParameterType == typeof(Type));

                s_assetLoadAsyncMethod = s_assetType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(m => m.Name == "LoadAsync" && 
                                        m.GetParameters().Length == 2 &&
                                        m.GetParameters()[0].ParameterType == typeof(string) &&
                                        m.GetParameters()[1].ParameterType == typeof(Type));

                if (s_assetLoadMethod != null || s_assetLoadAsyncMethod != null)
                {
                    s_hasXAsset = true;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[PointoneSoundManager] Failed to detect xasset: {ex.Message}");
            s_hasXAsset = false;
            s_assetType = null;
            s_assetLoadMethod = null;
        }
        finally
        {
            s_checked = true;
        }
    }

    public static AudioClip LoadClip(string path, GameObject refObj)
    {
        if (string.IsNullOrEmpty(path)) return null;

        P1AudioLogger.LogClipLoadStart(path);

        // 尝试从 XAsset 加载
        if (s_checked && s_hasXAsset && s_assetLoadMethod != null)
        {
            var clip = TryLoadFromXAsset(path);
            if (clip != null)
            {
                P1AudioLogger.LogClipLoadResult(path, true, "XAsset", clip.name);
                return clip;
            }

            // 模糊匹配：尝试去掉或添加扩展名
            // 1. 如果有扩展名，尝试去掉
            bool xassetInFlight = s_pendingRequests.ContainsKey(path);
            string ext = System.IO.Path.GetExtension(path);
            if (!string.IsNullOrEmpty(ext))
            {
                string noExtPath = path.Substring(0, path.Length - ext.Length);
                clip = TryLoadFromXAsset(noExtPath);
                if (clip != null)
                {
                    P1AudioLogger.LogClipLoadResult(path, true, "XAsset (no ext)", clip.name);
                    return clip;
                }
                xassetInFlight = xassetInFlight || s_pendingRequests.ContainsKey(noExtPath);
            }
            // 2. 如果没有扩展名，尝试添加常见扩展名
            else
            {
                foreach (var e in s_audioExtensions)
                {
                    clip = TryLoadFromXAsset(path + e);
                    if (clip != null)
                    {
                        P1AudioLogger.LogClipLoadResult(path, true, $"XAsset ({e})", clip.name);
                        return clip;
                    }
                    xassetInFlight = xassetInFlight || s_pendingRequests.ContainsKey(path + e);
                }
            }

            // xasset 异步加载中，不再尝试 Resources 回退，避免每帧同步调用 Resources.Load
            if (xassetInFlight) return null;
        }

        // 尝试从项目路径加载（编辑器下）
        var projectClip = TryLoadFromProjectPath(path);
        if (projectClip != null)
        {
            P1AudioLogger.LogClipLoadResult(path, true, "Project Path", projectClip.name);
            return projectClip;
        }

        // 尝试从 Resources 加载
        var resClip = TryLoadViaResources(path);
        if (resClip != null)
        {
            P1AudioLogger.LogClipLoadResult(path, true, "Resources", resClip.name);
            return resClip;
        }

        P1AudioLogger.LogClipLoadResult(path, false, "所有方法都失败");
        return null;
    }

    private static AudioClip TryExtractClip(object request)
    {
        if (request == null) return null;
        var requestType = request.GetType();
        if (s_isDoneProp == null || s_isDoneProp.DeclaringType != requestType)
            s_isDoneProp = requestType.GetProperty("isDone", BindingFlags.Public | BindingFlags.Instance);
        if (s_isDoneProp != null && !(bool)s_isDoneProp.GetValue(request))
            return null;
        if (s_assetProp == null || s_assetProp.DeclaringType != requestType)
            s_assetProp = requestType.GetProperty("asset", BindingFlags.Public | BindingFlags.Instance);
        return s_assetProp?.GetValue(request) as AudioClip;
    }

    private static AudioClip TryLoadFromXAsset(string path)
    {
        if (s_clipCache.TryGetValue(path, out var cached))
        {
            if (cached != null) return cached;
            s_clipCache.Remove(path);
        }

        if (s_pendingRequests.TryGetValue(path, out var pending))
        {
            var clip = TryExtractClip(pending);
            if (clip != null)
            {
                s_clipCache[path] = clip;
                s_pendingRequests.Remove(path);
                return clip;
            }
            return null;
        }

        try
        {
            if (s_assetLoadAsyncMethod != null)
            {
                var request = s_assetLoadAsyncMethod.Invoke(null, new object[] { path, typeof(AudioClip) });
                if (request == null) return null;
                var clip = TryExtractClip(request);
                if (clip != null)
                {
                    s_clipCache[path] = clip;
                    return clip;
                }
                s_pendingRequests[path] = request;
                return null;
            }

            if (s_assetLoadMethod != null)
            {
                var request = s_assetLoadMethod.Invoke(null, new object[] { path, typeof(AudioClip) });
                var clip = TryExtractClip(request);
                if (clip != null)
                {
                    s_clipCache[path] = clip;
                    return clip;
                }
            }
        }
        catch (Exception)
        {
        }
        return null;
    }


    // Unity 支持的音频格式扩展名列表
    private static readonly string[] s_audioExtensions = { ".ogg", ".mp3", ".wav" };

#if UNITY_EDITOR
    private static AudioClip TryLoadFromProjectPath(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;

        var normalizedPath = path.Replace('\\', '/');
        
        // 如果路径已经是 Assets/ 开头的完整路径
        if (normalizedPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
        {
            // 尝试直接加载（可能已包含扩展名）
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(normalizedPath);
            if (clip != null) return clip;

            // 尝试添加扩展名
            foreach (var ext in s_audioExtensions)
            {
                if (normalizedPath.EndsWith(ext, StringComparison.OrdinalIgnoreCase)) continue;
                var pathWithExt = normalizedPath + ext;
                clip = AssetDatabase.LoadAssetAtPath<AudioClip>(pathWithExt);
                if (clip != null) return clip;
            }
        }
        else
        {
            // 相对路径，尝试在 Assets 下查找
            var assetsPath = "Assets/" + normalizedPath;
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetsPath);
            if (clip != null) return clip;

            foreach (var ext in s_audioExtensions)
            {
                if (normalizedPath.EndsWith(ext, StringComparison.OrdinalIgnoreCase)) continue;
                var pathWithExt = assetsPath + ext;
                clip = AssetDatabase.LoadAssetAtPath<AudioClip>(pathWithExt);
                if (clip != null) return clip;
            }
        }

        return null;
    }
#else
    private static AudioClip TryLoadFromProjectPath(string path)
    {
        // 运行时无法使用 AssetDatabase，返回 null
        return null;
    }
#endif

    private static AudioClip TryLoadViaResources(string path)
    {
        var resPath = DeriveResourcesPath(path);
        if (string.IsNullOrEmpty(resPath)) return null;
        
        // 首先尝试直接加载（路径可能已包含扩展名或已去除扩展名）
        var clip = Resources.Load<AudioClip>(resPath);
        if (clip != null) return clip;

        // 如果失败，尝试添加常见的音频扩展名
        foreach (var ext in s_audioExtensions)
        {
            var pathWithExt = resPath + ext;
            clip = Resources.Load<AudioClip>(pathWithExt);
            if (clip != null) return clip;
        }

        return null;
    }

    private static string DeriveResourcesPath(string path)
    {
        var p = path.Replace('\\', '/');
        var idx = p.IndexOf("Resources/", StringComparison.OrdinalIgnoreCase);
        if (idx >= 0)
        {
            p = p.Substring(idx + "Resources/".Length);
        }
        var dot = p.LastIndexOf('.');
        if (dot > 0) p = p.Substring(0, dot);
        return p;
    }
}


