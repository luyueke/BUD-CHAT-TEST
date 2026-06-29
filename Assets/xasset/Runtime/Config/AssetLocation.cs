using System;

namespace xasset
{
    [Serializable]
    public class AssetLocation
    {
        public int id;
        public ulong size;
        public ulong offset;
    }
}