using System;
using System.Collections.Generic;

namespace xasset
{
    [Serializable]
    public class AssetPack
    {
        public const string Extension = ".assets";
        public string name;
        public string file;
        public string desc;
        public string hash;
        public ulong size;
        public bool packed;
        public List<AssetLocation> assets = new List<AssetLocation>();
        private Dictionary<string, AssetLocation> _assets = new Dictionary<string, AssetLocation>();
        public string nameWithAppendHash => file.Replace(Extension, $"_{hash}{Extension}");
        public Manifest manifest { get; set; }

        public void OnDeserialize()
        {
            foreach (var asset in assets)
                _assets[manifest.bundles[asset.id].name] = asset;
        }

        public bool TryGetAsset(string asset, out AssetLocation result)
        {
            return _assets.TryGetValue(asset, out result);
        }

        public bool Contains(string asset)
        {
            return _assets.ContainsKey(asset);
        }
    }
}