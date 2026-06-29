using System;
using System.Collections.Specialized;
using UnityEngine;
using System.Collections.Generic;
using Game.Base;
using Game.ECS;


namespace Game.Utils
{ 
    public class CacheItem
    {
        public GameObject target;
        public Transform originParent;
        public bool isDestroy = false;
        public uint uid = 0;
    }

    /// <summary>
    /// Author:JayWill
    /// Description:Undo/Redo 系统所使用的二级缓存池，undo/redo 15步骤中
    /// 删除和创建会经过SecondCachePool，只到超出undo管理范畴会进入ModelCachePool
    /// </summary>

    public class SecondCachePool
    {
        private const string TAG = "SecondCachePool";
        private OrderedDictionary _itemOrderDict = new OrderedDictionary();
        private Transform cacheNode;
        private int maxCount = 1000;
        private Action<GameObject> _overMaxCallback;

        public void DestroyNode(GameObject gameObject)
        {
            if (gameObject == null)
            {
                LoggerUtils.Log($"{TAG} DestroyEntity gameObject is null !!!");
                return;
            }

            AddItem(gameObject);
        }
        
        public void AddItem(GameObject go)
        {
            LoggerUtils.Log($"{TAG} AddItem:" + go?.transform.name);
            
            if (!_itemOrderDict.Contains(go))
            {
                if (_itemOrderDict.Count >= maxCount)
                {

                    var removeItem = _itemOrderDict[0] as CacheItem;
                    RemoveItemAt(0);
                    if (removeItem.target)
                    {
                        // GamePropNodeManager.Inst.DestroyNode(removeItem.target);
                        LoggerUtils.Log($"{TAG} 超过最大限制个数,销毁:" + removeItem.target.GetHashCode());
                        _overMaxCallback?.Invoke(removeItem.target);
                    }
                }

                CacheItem item = new CacheItem();
                item.target = go;
                item.originParent = go.transform.parent;
                item.isDestroy = true;
                var nodeBehav = go.GetComponent<NodeBaseBehaviour>();
                if (nodeBehav != null && nodeBehav.entity != null)
                {
                    item.uid = nodeBehav.entity.GetComp<GameObjectComponent>().Uid;
                    LoggerUtils.Log($"{TAG} 删除道具:" + item.uid);
                }

                _itemOrderDict.Add(go, item);
                go.transform.parent = GetCacheNode();
                SetItemEnAble(go, false);
            }
        }

        public void RevertItem(GameObject go)
        {
            if (_itemOrderDict.Contains(go))
            {
                var item = _itemOrderDict[go] as CacheItem;
                LoggerUtils.Log($"{TAG} RevertItem:" + item.uid);
                item.isDestroy = false;
                _itemOrderDict.Remove(go);
                go.transform.parent = item.originParent;
                SetItemEnAble(go, true);
            }
        }

        public bool IsContains(GameObject go)
        {
            return _itemOrderDict.Contains(go);
        }

        public GameObject GetGameObjectByUid(uint uid)
        {
            if (uid <= 0)
            {
                return null;
            }

            foreach (var item in _itemOrderDict.Values)
            {
                CacheItem cacheItem = item as CacheItem;
                if (cacheItem.uid == uid)
                {
                    return cacheItem.target;
                }
            }

            return null;
        }

        public void RemoveItem(GameObject go)
        {
            if (go == null)
            {
                return;
            }
            
            if (_itemOrderDict.Contains(go))
            {
                _itemOrderDict.Remove(go);
                SetItemEnAble(go, true);
            }
        }

        public void RemoveItemAt(int index)
        {
            CacheItem item = _itemOrderDict[index] as CacheItem;
            if (item != null && item.target != null)
            {
                RemoveItem(item.target);
            }
        }

        public Transform GetCacheNode()
        {
            if (cacheNode == null)
            {
                cacheNode = new GameObject("SecondCacheNode").transform;
                cacheNode.gameObject.SetActive(false);
            }

            return cacheNode;
        }

        public int GetCount()
        {
            return _itemOrderDict.Count;
        }

        private void SetItemEnAble(GameObject go, bool able)
        {
            MeshRenderer[] meshRenders = go.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var mesh in meshRenders)
            {
                mesh.enabled = able;
            }

            NodeBaseBehaviour[] nbehaviours = go.GetComponentsInChildren<NodeBaseBehaviour>(true);
            foreach (var behaviour in nbehaviours)
            {
                behaviour.enabled = able;
            }
        }

        public  List<GameObject> ClearPool()
        {
            if (_itemOrderDict == null || _itemOrderDict.Count == 0)
            {
                return null;
            }

            List<GameObject> toDestroyList = new List<GameObject>();

            foreach (GameObject gameObject in _itemOrderDict.Keys)
            {
                if (gameObject != null)
                {
                    SetItemEnAble(gameObject, true);
                    toDestroyList.Add(gameObject);
                }
            }
            _itemOrderDict.Clear();
            return toDestroyList;
        }

        public void AddOverMaxListener(Action<GameObject> callback)
        {
            _overMaxCallback += callback;
        }

        public void ClearOverMaxListener()
        {
            _overMaxCallback = null;
        }

        public void Release()
        {
            _itemOrderDict.Clear();
            ClearOverMaxListener();
        }
    }
}

