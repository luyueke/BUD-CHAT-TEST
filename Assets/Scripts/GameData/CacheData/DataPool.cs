using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GameData
{
    public class DataPool:GlobalInstance<DataPool>
    {
        public Dictionary<NetCacheKey, NetDataBlock> NetBusinessDatas;

        public DataPool()
        {
            NetBusinessDatas = new Dictionary<NetCacheKey, NetDataBlock>();
        }

        public NetDataBlock GetHttpData(NetCacheKey key)
        {
            if (NetBusinessDatas.ContainsKey(key))
            {
                var block = NetBusinessDatas[key];
                return block;
            }
            return null;
        }

        public void AddHttpData(NetCacheKey key,string content)
        {
            NetDataBlock block = new NetDataBlock()
            {
                content = content
            };
            NetBusinessDatas[key] = block;
        }
    }
}


