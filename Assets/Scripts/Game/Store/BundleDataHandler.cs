using Game.Database;
using Game.Store;
using GameData.PgcData;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Store
{
    public class BundleDataHandler : AssetsDataHandler
    {
        private Dictionary<string, BundleData> bundleDict;
       
        public override void InitData()
        {
            dict = new();
            bundleDict = new();

            var bundleList = AssetsDataManager.StoreData.BundleList;
            // 商城资源
            for (int i = 0, C = bundleList.Count; i < C; i++)
            {
                var serverData = bundleList[i];
                var gashaponData = GetBundleData(serverData.Id);
                gashaponData.Name = serverData.Name;
            }
        }

        public List<BundleData> GetBundleList()
        {
            return bundleDict.Values.ToList();
        }

        public BundleData GetBundleData(string id)
        {
            if (bundleDict.ContainsKey(id)) return bundleDict[id];

            BundleData data = new BundleData();
            data.Id = id;
            bundleDict.Add(id, data);
            return data;
        }
    }

    public class BundleData
    {
        public string Id;
        public string Name;

        //客户端业务字段：因数据不足，暂时用不上
        // public List<AssetsData> PgcDatas; // 如果奖励是pgc 这里就有值
    }
}
