/**
 * @ Author: Jun Zhou
 * @ Create Time: 2023-07-18 10:37:32
 * @ Modified by: Jun Zhou
 * @ Modified time: 2023-07-31 18:12:27
 * @ Description: 模型加载缓存类
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Game.Utils;
using GameData.Config;
using UnityEngine;
using Random = UnityEngine.Random;

public class ModelCachePool : InstMonoBehaviour<ModelCachePool>
{
    public int maxLength = 10000;
    private int curLength = 0;
    private Dictionary<string, List<GameObject>> noUserPool = new Dictionary<string, List<GameObject>>();

    public void Clear()
    {
        foreach (var kvp in noUserPool)
        {
            var lst = kvp.Value;
            for (int i = lst.Count - 1; i >= 0; --i)
            {
                var go = lst[i];
                Destroy(go);
            }
        }

        noUserPool.Clear();
        curLength = 0;
    }

    public GameObject Get(string id)
    {
        GameObject go = null;
        if (noUserPool.ContainsKey(id) && noUserPool[id].Count > 0)
        {
            curLength--;
            go = noUserPool[id].First();
            noUserPool[id].RemoveAt(0);
        }
        else
        {
            go = CreateNode(id);
        }
        go.SetActive(true);
        return go;
    }

    //
    public GameObject Get(string id,string prefabPath)
    {
        GameObject go = null;
        if (noUserPool.ContainsKey(id) && noUserPool[id].Count > 0)
        {
            curLength--;
            go = noUserPool[id].First();
            noUserPool[id].RemoveAt(0);
        }
        else
        {
            go = CreateNodeByPath(prefabPath);
        }
        go.SetActive(true);
        return go;
    }
    
    public void GetAsync(string id, Action<GameObject> callback)
    {
        GameObject go = null;
        if (noUserPool.ContainsKey(id) && noUserPool[id].Count > 0)
        {
            curLength--;
            go = noUserPool[id].First();
            noUserPool[id].RemoveAt(0);
            go.SetActive(true);
            callback?.Invoke(go);
        }
        else
        {
            CreateAsync(id, go =>
            {
#if UNITY_EDITOR
                // 电脑上延迟1s, 方便查看异步加载效果
                go.SetActive(false);
                TimerManager.Inst.RunOnce($"GetAsync_{Random.Range(0, 1000000)}", 1, () =>
                {
                    go.SetActive(true);
                    callback?.Invoke(go);
                });
                return;
#endif
                go.SetActive(true);
                callback?.Invoke(go);
            });
        }
    }

    private GameObject CreateNodeByPath(string path)
    {
        GameObject go = null;
        var wrapper = Loader.Load<GameObject>(path);
        go = wrapper.Instantiate();
        go.name = wrapper.GetAssetName();
        return go;
    }

    private GameObject CreateNode(string id)
    {
        var gamePropConfig = GamePropDataHelper.GetPropDataByID(id);
        GameObject go = null;
        string path = $"Assets/Loadable/Model3D/Editor_Props/{gamePropConfig.PrefabName}.prefab";
        var wrapper = Loader.Load<GameObject>(path);
#if UNITY_EDITOR
        if (wrapper == null)
        {
            LoggerUtils.LogError($"wapper 加载出错: 检查资源路径 {path}");
        }
        else
            LoggerUtils.Log($"wapper 加载资源完成 {path}");
#endif
        go = wrapper.Instantiate();
        go.name = wrapper.GetAssetName();
        return go;
    }

    private void CreateAsync(string id,Action<GameObject> callback)
    {
        var gamePropConfig = GamePropDataHelper.GetPropDataByID(id);
        GameObject go = null;
        string assetPath = $"Assets/Loadable/Demand3D/{gamePropConfig.PrefabName}.prefab";
        Loader.LoadAsyncOrSync<GameObject>(assetPath, (isSuccess, warpper) =>
        {
            if (isSuccess)
            {
                var go = warpper.Instantiate();
                go.name = warpper.GetAssetName();
                callback?.Invoke(go);
            }
        });
    }

    public void Release(string id, GameObject go)
    {

        if (curLength >= maxLength)
        {
            GameObject.Destroy(go);
            return;
        }

        // // 如果是UGC 资源 不做缓存
        if (id ==GamePropDataHelper.GetPropIdByNodeModelType(NodeModelType.Prop).Id)
        {
            GameObject.Destroy(go);
            return;
        }

        curLength++;
        go.transform.SetParent(this.transform);
        go.gameObject.SetActive(false);
        go.transform.localScale = Vector3.one;
        go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        if (!noUserPool.ContainsKey(id))
        {
            noUserPool.Add(id, new List<GameObject>());
        }
        noUserPool[id].Add(go);
    }

    private void OnDestroy()
    {
        inst = null;
    }
}

