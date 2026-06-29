using System;

namespace xasset
{
    [Serializable]
    public class ManifestBundle
    {
        public int[] deps = Array.Empty<int>();
        public string hash;
        public string name;
        public bool raw;
        public ulong size;
        public int pack;
        public string nameWithAppendHash { get; set; }
        public Manifest manifest { get; set; }
    }
}