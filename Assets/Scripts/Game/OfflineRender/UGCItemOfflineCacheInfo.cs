// @Author: YangJie
// @Description:
// @Date:  2023/09/19
// @Modify:

using System.Collections.Generic;
using Game.OfflineRender;
using Newtonsoft.Json;

namespace Game.Base
{

    public class UGCItemOfflineCacheData
    {
        public string fileName;
        public long fileSize;
        public long lastWriteTime;
        [JsonIgnore]
        public UGCCombineData combineData;
    }

    public class UGCItemOfflineCacheInfo
    {
        public Dictionary<ModelLODType, UGCItemOfflineCacheData> lodDic = new Dictionary<ModelLODType, UGCItemOfflineCacheData>();
    }
}