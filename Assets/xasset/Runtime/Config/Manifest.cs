using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace xasset
{
    public class Manifest : ScriptableObject, ISerializationCallbackReceiver
    {
        public string build;
        public int[] changes = Array.Empty<int>();
        public string[] dirs = Array.Empty<string>();
        public ManifestAsset[] assets = Array.Empty<ManifestAsset>();
        public ManifestBundle[] bundles = Array.Empty<ManifestBundle>();
        public AssetPack[] packs = Array.Empty<AssetPack>();
        private readonly Dictionary<string, List<int>> directoryWithAssets = new Dictionary<string, List<int>>();
        private readonly Dictionary<string, ManifestAsset> nameWithAssets = new Dictionary<string, ManifestAsset>();
        private readonly Dictionary<string, List<AssetPack>> nameWithPacks = new Dictionary<string, List<AssetPack>>();

        // 缓存常用的文件扩展名，避免重复的EndsWith调用
        private static readonly string UnityExtension = ".unity";
        private static readonly string PrefabExtension = ".prefab";
        private static readonly int UnityExtensionLength = UnityExtension.Length;
        private static readonly int PrefabExtensionLength = PrefabExtension.Length;

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            foreach (var item in dirs)
            {
                var dir = item;
                if (!directoryWithAssets.TryGetValue(dir, out _)) directoryWithAssets.Add(dir, new List<int>());

                int pos;
                while ((pos = dir.LastIndexOf('/')) != -1)
                {
                    dir = dir.Substring(0, pos);
                    if (!directoryWithAssets.TryGetValue(dir, out _)) directoryWithAssets.Add(dir, new List<int>());
                }
            }

            foreach (var pack in packs)
            {
                pack.manifest = this;
                pack.OnDeserialize();
                if (!nameWithPacks.ContainsKey(pack.name)) nameWithPacks[pack.name] = new List<AssetPack>();
                nameWithPacks[pack.name].Add(pack);
            }

            foreach (var bundle in bundles)
            {
                var extension = Path.GetExtension(bundle.name);
                var nameWithAppendHash = string.IsNullOrEmpty(extension)
                    ? $"{bundle.name}_{bundle.hash}"
                    : $"{bundle.name.Replace(extension, string.Empty)}_{bundle.hash}{extension}";
                bundle.nameWithAppendHash = nameWithAppendHash;
                bundle.manifest = this;

                // TODO
                if (build.StartsWith("wwise") || bundle.raw)
                {
                    // 如果是Raw资源, 名字不加哈希值，方便加载
                    bundle.nameWithAppendHash = $"{bundle.name}";
                }
            }

            foreach (var asset in assets)
            {
                var dir = dirs[asset.dir];
                var path = $"{dir}/{asset.name}";
                asset.path = path;
                asset.manifest = this;
                AddAsset(asset);
                if (directoryWithAssets.TryGetValue(dir, out var value)) value.Add(asset.id);
            }
        }

        public IEnumerable<string> GetChanges()
        {
            return changes.Length > 0
                ? Array.ConvertAll(changes, input => bundles[input].nameWithAppendHash)
                : Array.ConvertAll(bundles, input => input.nameWithAppendHash);
        }

        public int[] GetAssets(string dir, bool recursion)
        {
            if (!recursion)
                return directoryWithAssets.TryGetValue(dir, out var value)
                    ? value.ToArray()
                    : Array.Empty<int>();

            var keys = new List<string>();
            foreach (var item in directoryWithAssets.Keys)
                if (item.StartsWith(dir)
                    && (item.Length == dir.Length || (item.Length > dir.Length && item[dir.Length] == '/')))
                    keys.Add(item);

            if (keys.Count <= 0) return Array.Empty<int>();

            var get = new List<int>();
            foreach (var item in keys) get.AddRange(GetAssets(item, false));

            return get.ToArray();
        }

        /// <summary>
        /// 优化的文件扩展名检查方法，避免EndsWith的性能开销
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <returns>是否为.unity或.prefab文件</returns>
        private static bool IsUnityOrPrefabFile(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;

            var pathLength = path.Length;

            // 检查.prefab扩展名
            if (pathLength >= PrefabExtensionLength)
            {
                var prefabStartIndex = pathLength - PrefabExtensionLength;
                if (path[prefabStartIndex] == '.' &&
                    path[prefabStartIndex + 1] == 'p' &&
                    path[prefabStartIndex + 2] == 'r' &&
                    path[prefabStartIndex + 3] == 'e' &&
                    path[prefabStartIndex + 4] == 'f' &&
                    path[prefabStartIndex + 5] == 'a' &&
                    path[prefabStartIndex + 6] == 'b')
                {
                    return true;
                }
            }
            // 检查.unity扩展名
            if (pathLength >= UnityExtensionLength)
            {
                var unityStartIndex = pathLength - UnityExtensionLength;
                if (path[unityStartIndex] == '.' && 
                    path[unityStartIndex + 1] == 'u' && 
                    path[unityStartIndex + 2] == 'n' && 
                    path[unityStartIndex + 3] == 'i' && 
                    path[unityStartIndex + 4] == 't' && 
                    path[unityStartIndex + 5] == 'y')
                {
                    return true;
                }
            }
            
     
            
            return false;
        }

        private void AddAsset(ManifestAsset asset)
        {
            nameWithAssets[asset.path] = asset;
            // 场景和预设默认生成短链接
            if (asset.auto) return;
            if (!IsUnityOrPrefabFile(asset.path)) return;
            var alias = Path.GetFileNameWithoutExtension(asset.path);
            if (nameWithAssets.TryGetValue(alias, out var value))
                Logger.W($"{alias} already exist {value.path}");
            else
                nameWithAssets[alias] = asset;
        }

        public bool IsDirectory(string path)
        {
            return directoryWithAssets.ContainsKey(path);
        }

        public bool ContainsAsset(string path)
        {
            return nameWithAssets.ContainsKey(path);
        }

        public bool TryGetAsset(string path, out ManifestAsset asset)
        {
            return nameWithAssets.TryGetValue(path, out asset);
        }

        public ManifestBundle GetBundle(string assetPath)
        {
            return TryGetAsset(assetPath, out var value) ? bundles[value.bundle] : null;
        }

        public ManifestBundle[] GetDependencies(ManifestBundle bundle)
        {
            return bundle.deps == null
                ? Array.Empty<ManifestBundle>()
                : Array.ConvertAll(bundle.deps, input => bundles[input]);
        }

        public bool TryGetAssetPacks(string assetPack, out List<AssetPack> result)
        {
            return nameWithPacks.TryGetValue(assetPack, out result);
        }
    }
}