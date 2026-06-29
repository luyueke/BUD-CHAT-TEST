using System;
using UnityEngine;

namespace xasset
{
    public class UpdateInfo : ScriptableObject
    {
        public static readonly string Filename = $"{nameof(UpdateInfo).ToLower()}.json";
        public VersionsInfo versionsInfo = new VersionsInfo();
        public string downloadURL;
    }
    
    public class UpdateInfoConfig : ScriptableObject
    {
        public static readonly string Filename = $"{nameof(UpdateInfoConfig).ToLower()}.json";
        public string file = "updateinfo.json";
        public string hash;
        public ulong size;
        public long timestamp;
        public string downloadURL;
    }
    
    
    [Serializable]
    public class VersionsInfo
    {
        public int quality;  // 0:high 1:low
        public string file;
        public string hash;
        public ulong size;
        public long timestamp;
    }
}