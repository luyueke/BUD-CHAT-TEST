using Game.Database;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Store
{
    /// <summary>
    /// 背包应用数据句柄
    /// 筛选和组装对应场景需要的数据
    /// 实现数据获取、数据改变回调、数据处理（排序，预值计算等）
    /// </summary>
    public class AssetsDataHandler
    {
        internal Dictionary<string, AssetsData> dict;
        private Action<AssetsData[]> OnDataChange;
        private Dictionary<Action<AssetsData[]>, GameObject> actions = new();
        public AssetsDataHandler()
        {
            SafeInitData();
        }

        public void SafeInitData()
        {
            try
            {
                InitData();
                BeforeChangeInvoke(null);
                AfterChangeInvoke(null);
            }
            catch (Exception e)
            {
                LoggerUtils.LogError("Load AssetsData Fail :" + e.Message + e.StackTrace);
            }
        }

        public virtual void InitData()
        {

        }

        public AssetsData GetAssetsData(string id) {
            if (!string.IsNullOrEmpty(id) && dict != null && dict.TryGetValue(id, out AssetsData data)) {
                return data;
            } else {
                return null;
            }
        }

        public void AddDataChange(GameObject go, Action<AssetsData[]> action)
        {
            if (actions.ContainsKey(action))
            {
                actions.Remove(action);
                OnDataChange -= action;
            }
            actions.Add(action, go);
            OnDataChange += action;
        }

        public void RemoveDataChange(Action<AssetsData[]> action)
        {
            if (actions.ContainsKey(action))
            {
                actions.Remove(action);
                OnDataChange -= action;
            }
        }

        public void RemoveAllDataChange(GameObject go)
        {
            List<Action<AssetsData[]>> remove = new();
            foreach (var kv in actions)
            {
                if (kv.Value == go)
                {
                    OnDataChange -= kv.Key;
                    remove.Add(kv.Key);
                }
            }
            foreach (var action in remove) actions.Remove(action);
        }

        internal void OnDataChangeInvoke(AssetsData[] data)
        {
            List<Action<AssetsData[]>> remove = new();
            foreach (var kv in actions)
            {
                if (kv.Value == null)
                {
                    OnDataChange -= kv.Key;
                    remove.Add(kv.Key);
                }
            }
            try
            {
                OnDataChange?.Invoke(data);
            }
            catch { }
            foreach (var action in remove) actions.Remove(action);
        }

        internal void UpdateData(InventoryData[] datas)
        {
            if (dict == null) return;
            List<AssetsData> changed = new();
            List<InventoryData> news = new();
            for (int i = 0, L = datas.Length; i < L; i++)
            {
                var data = datas[i];
                var key = data.Id;
                if (dict.ContainsKey(key))
                {
                    if (dict[key].InventoryData == null || !dict[key].InventoryData.Equals(data)) changed.Add(dict[key]);
                    if (dict[key].InventoryData == null)
                        dict[key].InventoryData = data.Clone();
                    else
                        dict[key].InventoryData.ValueCopy(data);
                }
                else
                {
                    news.Add(data);
                }
            }

            AddDataInvoke(news);
            news.ForEach(n =>
            {
                if (dict.ContainsKey(n.Id))
                {
                    changed.Add(dict[n.Id]);
                }
            });

            if (changed.Count > 0)
            {
                BeforeChangeInvoke(changed);
                OnDataChangeInvoke(changed.ToArray());
                AfterChangeInvoke(changed);
            }
        }

        protected virtual void AddDataInvoke(List<InventoryData> news)
        {

        }

        protected virtual void BeforeChangeInvoke(List<AssetsData> changed)
        {

        }

        protected virtual void AfterChangeInvoke(List<AssetsData> changed)
        {

        }
    }
}
