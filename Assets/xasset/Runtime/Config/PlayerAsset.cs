using System;

namespace xasset
{
    [Serializable]
    public class PlayerAsset
    {
        public string key;
        public ulong offset;

        public static PlayerAsset Create(ManifestBundle bundle, ulong offset = 0)
        {
            return new PlayerAsset {key = bundle.nameWithAppendHash, offset = offset };
        }
    }
}