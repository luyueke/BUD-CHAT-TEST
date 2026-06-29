using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace xasset
{
    public static class Assets
    {
        public const ulong WriteOffset = 33;
        public const string Bundles = "Bundles";
        public static readonly System.Version APIVersion = new System.Version(2022, 2, 2);
        public static string UpdateURL { get; set; }
        public static string HotUpdateVersion { get; set; }
        public static string DownloadURL { get; set; }
        public static Versions Versions { get; set; } = ScriptableObject.CreateInstance<Versions>();
        public static Versions OtherVerions { get; set; } = ScriptableObject.CreateInstance<Versions>();
        public static PlayerAssets PlayerAssets { get; set; } = ScriptableObject.CreateInstance<PlayerAssets>();
        public static bool SimulationMode { get; set; }
        public static bool FastVerifyMode { get; set; } = true;
        public static bool OfflineMode { get; set; } = true;
        public static Platform Platform { get; set; } = Utility.GetPlatform();
        public static bool IsWebGLPlatform => Platform == Platform.WebGL;
        public static string Protocol => Utility.GetProtocol();
        public static string Quality { get; set; } = "High";
        public static string PlayerDataPath { get; internal set; } = $"{Application.streamingAssetsPath}/{Bundles}";
        public static string DownloadDataPath { get; set; } = $"{Application.persistentDataPath}/{Bundles}";

        public static List<string> HotUpdateBuild = new List<string>()
        {
            "UgcAvatarPart",
            "UgcAvatarPartConfig",
            "Part_Accessoies_OnDemand",
            "Part_Bag_OnDemand",
            "Part_Cape_OnDemand",
            "Part_Clothes_OnDemand",
            "Part_Crossboby_OnDemand",
            "Part_Earrings_OnDemand",
            "Part_Effect_OnDemand",
            "Part_Eyes_OnDemand",
            "Part_Glasses_OnDemand",
            "Part_Glove_OnDemand",
            "Part_Hair_OnDemand",
            "Part_Hand_OnDemand",
            "Part_Hats_OnDemand",
            "Part_Mouse_OnDemand",
            "Part_Shoe_OnDemand",
            "Part_Special_OnDemand",
            "BaseTexture",
            "PGCRes",
            "Languages",
            "Configs"
        };

        public static void Dispose()
        {
            PlayerDataPath = $"{Application.streamingAssetsPath}/{Bundles}";
        }
        public static InitializeRequest InitializeAsync(Action<Request> completed = null)
        {
            var request = new InitializeRequest();
            request.SendRequest();
            request.completed = completed;
            return request;
        }


        public static VersionsRequest GetVersionsAsync(string url, string hash, ulong size)
        {
            var request = new VersionsRequest { url = GetDownloadURL(url), hash = hash, size = size };
            request.SendRequest();
            return request;
        }

        public static GetDownloadSizeRequest GetDownloadSizeAsync(Versions versions, params string[] assetPaths)
        {
            var request = new GetDownloadSizeRequest { assetPaths = assetPaths, versionsList = new List<Versions> { versions } };
            request.SendRequest();
            return request;
        }

        public static WwiseRequest LoadWwiseAsync(List<string> paths)
        {
            var request = new WwiseRequest() { wwisePaths = paths };
            request.SendRequest();
            return request;
        }

        public static GetDownloadSizeRequest GetDownloadSizeAsync(List<Versions> versionsList, params string[] assetPaths)
        {
            var request = new GetDownloadSizeRequest { assetPaths = assetPaths, versionsList = versionsList };
            request.SendRequest();
            return request;
        }

        public static RemoveRequest RemoveAsync(params string[] assetPaths)
        {
            var set = new HashSet<string>();
            foreach (var file in assetPaths)
                if (Versions.TryGetAssetPacks(file, out var packs))
                    foreach (var pack in packs)
                        if (pack.packed && IsDownloaded(pack))
                            set.Add(GetDownloadDataPath(pack.nameWithAppendHash));
                        else
                            foreach (var asset in pack.assets)
                                set.Add(GetDownloadDataPath(pack.manifest.bundles[asset.id].nameWithAppendHash));
                else if (Versions.TryGetAssets(file, out var assets))
                    foreach (var asset in assets)
                        set.Add(GetDownloadDataPath(asset.manifest.bundles[asset.bundle].nameWithAppendHash));
                else
                    Logger.W($"File not found {file}");

            var request = new RemoveRequest();
            request.files.AddRange(set);
            request.SendRequest();
            return request;
        }

        public static bool Contains(string path)
        {
#if UNITY_EDITOR
            if (Assets.SimulationMode)
            {
                return File.Exists(path);
            }
#endif
            return Versions.data.Exists(version => version.manifest.ContainsAsset(path));
        }

        public static bool IsDownloaded(string path)
        {
#if UNITY_EDITOR
            if (Assets.SimulationMode)
                return true;
#endif
            if (!Versions.TryGetAsset(path, out var asset)) return true;
            var bundle = asset.mainBundle;
            if (!IsDownloaded(bundle))
                return false;

            foreach (var dependency in asset.mainBundle.deps)
            {
                bundle = asset.manifest.bundles[dependency];
                if (!IsDownloaded(bundle)) return false;
            }

            return true;
        }

        public static bool IsDownloaded(ManifestBundle bundle)
        {
            if (IsPlayerAsset(bundle.nameWithAppendHash)) return true;
            var path = GetDownloadDataPath(bundle.nameWithAppendHash);
            var file = new FileInfo(path);
            if (!file.Exists || file.Length != (long)bundle.size) return false;
            if (FastVerifyMode) return true;
            bool isSameHash = Utility.ComputeHash(path) == bundle.hash;
            if(!isSameHash)
            {
                Debug.Log($"IsDownloaded Hash Not Same, file={bundle.name}");
            }
            return isSameHash;
          
        }

        public static bool IsDownloaded(AssetPack pack)
        {
            var file = new FileInfo(GetDownloadDataPath(pack.nameWithAppendHash));
            return (file.Exists &&
                    file.Length == (long)pack.size && FastVerifyMode) ||
                   Utility.ComputeHash(file.FullName) == pack.hash;
        }

        public static bool IsPlayerAsset(string key)
        {
            if (OfflineMode) return true;
            return  PlayerAssets != null && PlayerAssets.Contains(key);
        }

        public static bool TryGetPlayerAsset(string key, out PlayerAsset asset)
        {
            asset = null;
            return PlayerAssets != null && PlayerAssets.TryGetValue(key, out asset);
        }

        public static bool IsDownloaded(Version version)
        {
            var file = new FileInfo(GetDownloadDataPath($"{version.file}"));
            return (file.Exists &&
                    file.Length == (long)version.size && FastVerifyMode) ||
                   Utility.ComputeHash(file.FullName) == version.hash;
        }

        public static string GetDownloadURL(string filename)
        {
            return $"{DownloadURL}/{filename}";
        }

        public static string GetPlayerDataPath(string filename)
        {
            return $"{PlayerDataPath}/{filename}";
        }
        public static string GetTopPlayerDataPath(string filename)
        {
            return $"{PlayerDataPath}/{filename}";
        }

        public static string GetBundlePath(string filename)
        {
            return $"{PlayerDataPath}/{filename}";
        }

        public static string GetPlayerDataURl(string filename)
        {
            return $"{Protocol}{GetPlayerDataPath(filename)}";
        }

        public static string GetTopPlayerDataURl(string filename)
        {
            return $"{Protocol}{GetTopPlayerDataPath(filename)}";
        }

        public static string GetDownloadDataPath(string filename)
        {
            var path = $"{DownloadDataPath}/{filename}";
            Utility.CreateDirectoryIfNecessary(path);
            return path;
        }

        public static string GetTemporaryCachePath(string filename)
        {
            var path = $"{Application.temporaryCachePath}/{filename}";
            Utility.CreateDirectoryIfNecessary(path);
            return path;
        }

        public static bool TryGetAssetPack(ManifestBundle bundle, out AssetPack result)
        {
            result = null;
            var id = bundle.pack;
            var manifest = bundle.manifest;
            if (id < 0 || id >= manifest.packs.Length) return false;
            var pack = manifest.packs[id];
            if (!pack.Contains(bundle.name)) return false;
            result = pack;
            return pack.packed;
        }

        public static void HotUpdateVersions(Versions newVersion)
        {
            for (int i = 0; i < HotUpdateBuild.Count; i++)
            {
                if (Versions.TryGetVersion(HotUpdateBuild[i], out Version v1) && newVersion.TryGetVersion(HotUpdateBuild[i], out Version v2))
                {
                    v1.file = v2.file;
                    v1.size = v2.size;
                    v1.hash = v2.hash;
                    v1.ver = v2.ver;
                    v1.manifest = v2.manifest;
                }
            }
        }
    }
}