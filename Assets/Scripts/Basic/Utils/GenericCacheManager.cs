using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
/**
* @ Author: JayWill
* @ Create Time: 2024-07-11 21:32:56
* @ Description: 通用缓存器,用来保存数据到本地，maxSaveSize参数为最大保存到本地的数量，keySelector为选择id的规则委托
*/
namespace Basic.Utils
{
    public class GenericCacheManager<T> where T : class
    {
        private int maxSaveSize;
        private List<T> cacheList = new List<T>();
        private Dictionary<string, T> cacheDictionary = new Dictionary<string, T>();
        private Func<T, string> keySelector;

        public GenericCacheManager(int maxSaveSize, Func<T, string> keySelector)
        {
            this.maxSaveSize = maxSaveSize;
            this.keySelector = keySelector;
        }

        public void AddToCache(T value)
        {
            string key = keySelector(value);

            cacheList.Add(value);
            cacheDictionary[key] = value;
        }

        public void ResetCache(string dstKey, T value)
        {
            for (int i = 0; i < cacheList.Count; i++)
            {
                var cache = cacheList[i];
                string key = keySelector(cache);
                if (key == dstKey)
                {
                    cacheList[i] = value;
                    break;
                }
            }

            if (cacheDictionary.ContainsKey(dstKey))
            {
                cacheDictionary[dstKey] = value;
            }
        }

        public bool ContainsKey(string key)
        {
            return cacheDictionary.ContainsKey(key);
        }

        public void SaveCacheToFile(string filePath)
        {
            // 复制一份数据
            List<T> cacheListCopy = new List<T>(cacheList);

            // 移除超出数量限制的较早数据
            if (cacheListCopy.Count > maxSaveSize)
            {
                int excessCount = cacheListCopy.Count - maxSaveSize;
                cacheListCopy.RemoveRange(0, excessCount);
            }

            try
            {
                File.WriteAllText(filePath, JsonConvert.SerializeObject(cacheListCopy));
            }
            catch (Exception e)
            {
               LoggerUtils.LogError("GenericCacheManager SaveCacheToFile Error:"+e.StackTrace);
            }

        }

        public void LoadCacheFromFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    cacheList = JsonConvert.DeserializeObject<List<T>>(json);
                    // 重新构建字典
                    cacheDictionary.Clear();
                    foreach (var entry in cacheList)
                    {
                        cacheDictionary[keySelector(entry)] = entry;
                    }
                }
                else
                {
                    LoggerUtils.Log("GenericCacheManager File does not exist: " + filePath);
                    cacheList = new List<T>();
                    cacheDictionary.Clear();
                }
            }
            catch (Exception e)
            {
                LoggerUtils.LogError("GenericCacheManager Read File Error: " + e.StackTrace);
                cacheList = new List<T>();
                cacheDictionary.Clear();
            }
        }

        public T GetFromCache(string key)
        {
            if (cacheDictionary.TryGetValue(key, out T value))
            {
                return value;
            }
            return null;
        }

        public List<T> GetList()
        {
            return cacheList;
        }

        public int GetCount()
        {
            return cacheList.Count;
        }
    }
}
