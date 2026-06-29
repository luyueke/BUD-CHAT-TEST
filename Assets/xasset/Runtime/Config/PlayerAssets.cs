using System.Collections.Generic;
using UnityEngine;

namespace xasset
{
    public class PlayerAssets : ScriptableObject, ISerializationCallbackReceiver
    {
        public static readonly string Filename = $"{nameof(PlayerAssets).ToLower()}.json";
        public List<PlayerAsset> data = new List<PlayerAsset>();
        private readonly Dictionary<string, PlayerAsset> _data = new Dictionary<string, PlayerAsset>();

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            foreach (var asset in data) _data[asset.key] = asset;
        }

        public bool Contains(string key)
        {
            return _data.ContainsKey(key);
        }

        public bool TryGetValue(string key, out PlayerAsset value)
        {
            return _data.TryGetValue(key, out value);
        }
    }
}