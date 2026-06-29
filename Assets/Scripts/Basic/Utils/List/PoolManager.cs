using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Reflection;

namespace Fsbm.Runtime
{
    /// <summary>
    /// 对象池管理器
    /// </summary>
    public class PoolManager : MonoSingleton<PoolManager>
    {
        //*********** 属性 ********
        [HideInInspector][SerializeField][Tooltip("每个对象池最多存多少个对象")]
        private int _maxCount = 50;
        [HideInInspector][SerializeField][Tooltip("每过多少秒自动清除这段时间不活动的对象池")]
        private int _autoClearTime = 5 * 60;

        [HideInInspector]
        [SerializeField]
        private List<PoolSetData> _poolSetDatas=new List<PoolSetData>();
        [HideInInspector]
        [SerializeField]
        private List<PoolSetData> _gameObjectPoolSetDatas=new List<PoolSetData>();


        //********* 私有变量 ***********
        //存储普通对象
        private Dictionary<string, IPoolList> _poolDic = new Dictionary<string, IPoolList>();
        //存储GameObect对象
        private Dictionary<string, PoolList<GameObject>> _gameObjectPoolDic = new Dictionary<string, PoolList<GameObject>>();
        //记录要删除不用的对象池，提升效性不用每次都创建list，所以写成类成员;
        private List<string> _deleteKeyList = new List<string>();

        private float _lastCheckUpdateTime=0;
        private float _checkUpdateTime = 0;
    
        private Dictionary<string, PoolSetData> _poolSetDataDic = new Dictionary<string, PoolSetData>();
        private Dictionary<string, PoolSetData> _gameObjectpoolSetDataDic = new Dictionary<string, PoolSetData>();

        public  PoolManager()
        {
           
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        //********* 处理Mono事件 *********



        //public void OnBeforeSerialize()
        //{
        //    if (_poolSetDatas.Count != _poolSetDataDic.Count)
        //    {
        //        _poolSetDatas.Clear();
        //        foreach (string key in _poolSetDataDic.Keys)
        //        {
        //            _poolSetDatas.Add(_poolSetDataDic[key]);
        //        }
        //    }
        //    if (_gameObjectPoolSetDatas.Count != _gameObjectpoolSetDataDic.Count)
        //    {
        //        _gameObjectPoolSetDatas.Clear();
        //        foreach (string key in _gameObjectpoolSetDataDic.Keys)
        //        {
        //            _gameObjectPoolSetDatas.Add(_gameObjectpoolSetDataDic[key]);
        //        }
        //    }
        //}

        //// List -> Dictionary
        //public void OnAfterDeserialize()
        //{
        //    _poolSetDataDic.Clear();
        //    foreach (PoolSetData entry in _poolSetDatas)
        //    {
        //        _poolSetDataDic[entry.name] = entry;
        //    }
        //    _gameObjectpoolSetDataDic.Clear();
        //    foreach (PoolSetData entry in _gameObjectPoolSetDatas)
        //    {
        //        _gameObjectpoolSetDataDic[entry.name] = entry;
        //    }
        //}

        //public int nextClearTime
        //{
        //    get
        //    {
        //        float tempT = Time.time - _lastCheckUpdateTime;
        //        tempT= _checkUpdateTime - tempT;
        //        if (tempT < 0)
        //            tempT = 0;
        //        return Mathf.CeilToInt(tempT);
        //    }
        //}
        //********* 可配置参数的公共方法 ***********

        /// <summary>
        /// 每个对象池最多存多少个对象,默认50个,
        /// </summary>
        public int maxCount
        {
            get { return _maxCount; }
            set
            {
                if (_maxCount == value)
                    return;
                _maxCount = value;
                CheckLimit();
            }
        }
        /// <summary>
        /// 每过多少秒自动清除这段时间不活动的对象池 默认5分钟
        /// </summary>
        //public int autoClearTime
        //{
        //    get{ return _autoClearTime;}
        //    set
        //    {
        //        if (_autoClearTime == value)
        //            return;
        //        _autoClearTime = value;
        //        StartAutoClear(_autoClearTime);
        //    }
        //}

    
   
        /// <summary>
        /// 设置指定的普通对象池，每个对象池最多存多少个对象,
        /// </summary>
        public void SetMaxCount<T>(int value) where T : class, new()
        {
            SetMaxCount(typeof(T),value);
        }
        public void SetMaxCount(Type type,int value) 
        {
            PoolSetData setData = GetPoolSetData(type,true);
            setData.maxCount = value;
        }
        /// <summary>
        /// 设置指定的GameObject对象池，每个对象池最多存多少个对象,
        /// </summary>
        public void SetMaxCount(string perfabsName, int value)
        {
            PoolSetData setData = GetGameObjectPoolSetData(perfabsName, true);
            setData.maxCount = value;
        }
        public int GetMaxCount<T>() where T : class, new()
        {
            return GetMaxCount(typeof(T));
        }
        public int GetMaxCount(Type type) 
        {
            PoolSetData setData = GetPoolSetData(type,false);
            return setData != null ? setData.maxCount : _maxCount;
        }
        public int GetMaxCount(string perfabsName)
        {
            PoolSetData setData = GetGameObjectPoolSetData(perfabsName, false);
            return setData != null ? setData.maxCount : _maxCount;
        }

        /// <summary>
        /// 设置指定的普通对象池，每过多少秒自动清除这段时间不活动的对象池
        /// </summary>
        //public void SetAutoClearTime<T>(int value) where T : class, new()
        //{
        //    SetAutoClearTime(typeof(T), value);
        //}
        //public void SetAutoClearTime(Type type,int value) 
        //{
        //    PoolSetData setData = GetPoolSetData(type,true);
        //    setData.autoClearTime = value;
        //    if (_poolDic.ContainsKey(type.FullName))
        //        StartAutoClear(value);
        //}
        ///// <summary>
        ///// 设置指定的GameObject对象池，每过多少秒自动清除这段时间不活动的对象池
        ///// </summary>
        //public void SetAutoClearTime(string perfabsName, int value)
        //{
        //    PoolSetData setData = GetGameObjectPoolSetData(perfabsName, true);
        //    setData.autoClearTime = value;
        //    if (_gameObjectPoolDic.ContainsKey(perfabsName))
        //        StartAutoClear(value);
        //}
        public int GetAutoClearTime<T>() where T : class, new()
        {
            return GetAutoClearTime(typeof(T));
        }
        public int GetAutoClearTime(Type type) 
        {
            PoolSetData setData = GetPoolSetData(type,false);
            return setData != null ? setData.autoClearTime : _autoClearTime;

        }
        public int GetAutoClearTime(string perfabsName)
        {
            PoolSetData setData = GetGameObjectPoolSetData(perfabsName, false);
            return setData != null ? setData.autoClearTime : _autoClearTime;
        }





        //*********  公共方法 ***********


        /// <summary>
        /// 普通对象池数量
        /// </summary>
        public int poolCount
        {
            get  {  return _poolDic.Count; }
        }
        /// <summary>
        ///  普通对象池集，只提供给Editor用，项目中别用
        /// </summary>
        public Dictionary<string, IPoolList> poolDic
        {
            get  { return _poolDic;  }
        }

        /// <summary>
        /// GameObject对象池数量
        /// </summary>
        public int gameObjectPoolCount
        {
            get {  return _gameObjectPoolDic.Count;  }
        }
        /// <summary>
        ///  GameObject对象池集，只提供给Editor用，项目中别用
        /// </summary>
        public Dictionary<string, PoolList<GameObject>> gameObjectPoolDic
        {
            get  {  return _gameObjectPoolDic;  }
        }
        /// <summary>
        /// 获取一个普通对象
        /// 使用反射进行创建，效率慢10倍，建意判断为null在外面创建
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="autoCreate"></param>
        /// <returns></returns>
        public T GetObject<T>(bool autoCreate=true,Func<T> newFun=null) where T : class, new()
        {
            string key = typeof(T).FullName;
            IPoolList poolList = null;
            if (_poolDic.TryGetValue(key,out poolList))
            {
                return ((PoolList<T>)poolList).GetObject(autoCreate);
            }
            else if(autoCreate)
            {
                if(newFun!=null)
                    return newFun();
                return System.Activator.CreateInstance<T>();
            }
            return null;
        }


        /// <summary>
        /// 获取普通对象池
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="autoCreate">没有是否创建</param>
        /// <returns></returns>
        public PoolList<T> GetPoolList<T>(bool autoCreate = true) where T : class, new()
        {
            string key = typeof(T).FullName;
            IPoolList poolList = null;
            if (_poolDic.TryGetValue(key,out poolList)==false&& autoCreate)
            {
                poolList = new PoolList<T>(key);
                _poolDic[key] = poolList;
                PoolSetData setData = null;
                float t = _autoClearTime;
                if (_poolSetDataDic.TryGetValue(key,out setData))
                {
                    poolList.setData = setData;
                    t = setData.autoClearTime;
                }
                //StartAutoClear(t);
            }
            return poolList as PoolList<T>;
        }

        /// <summary>
        /// 把一个不用的对象放入对象池
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        public void PushObject<T>(T obj) where T : class, new()
        {
    
            PoolList<T> poolList = GetPoolList<T>(true);
            poolList.PushObject(obj);
        }

        public void Clear<T>(bool inactivity=false) where T : class, new()
        {
            PoolList<T> poolList = GetPoolList<T>(false);
            if(poolList!=null)
                poolList.Clear(inactivity);
        }
        public void Clear(GameObject prefabs, bool inactivity = false)
        {
            var poolList = GetGameObjectPoolList(prefabs,false);
            if (poolList != null)
                poolList.Clear(inactivity);
        }
        /// <summary>
        /// 通过预设 获取GameObject对象,并放在指定父级一下
        /// </summary>
        /// <param name="prefabs">预设 </param>
        /// <param name="parent">获取后放在父级下</param>
        /// <returns></returns>
        public GameObject GetGameObject(GameObject prefabs, Transform parent)
        {
            if (prefabs == null)
                return null;
            PoolList<GameObject> poolList = GetGameObjectPoolList(prefabs, false);
            if (poolList != null)
            {
                if (poolList.prefabs == null)
                    poolList.prefabs = prefabs;
                return poolList.GetGameObject(parent);
            }
            else
            {
                GameObject obj = BootTool.CreateChild( prefabs, parent);
                return obj;
            }
           
        }

        
        public GameObject GetGameObjectByKey(string key, Transform parent)
        {
            PoolList<GameObject> poolList = GetGameObjectPoolListByKey(key, false);
            if (poolList != null)
            {
                return poolList.GetGameObject(parent);
            }
            else
            {
                return null;
            }
        }
        public GameObject GetGameObject(string name, Transform parent)
        {
              return GetGameObjectByKey(name,parent);
        }
        public T GetGameObject<T>(GameObject prefabs, Transform parent) where T : MonoBehaviour
        {
            GameObject go = GetGameObject(prefabs, parent);
            if (go != null)
                return go.GetComponent<T>();
            return null;
        }
        public T GetGameObject<T>(string name, Transform parent) where T:MonoBehaviour
        {
            GameObject go = GetGameObject(name, parent);
            if (go != null)
                return go.GetComponent<T>();
            return null;
        }
        public bool IsInPool(GameObject go)
        {
            PoolList<GameObject> poolList = GetGameObjectPoolListByKey(go.name, false);
            if (poolList != null)
            {
                return poolList.IsInPool(go);
            }
     
             return false;
           
        }
        public bool IsInPool<T>(T go) where T : class, new()
        {
            PoolList<T> poolList = GetPoolList<T>(false);
            if (poolList != null)
            {
                return poolList.IsInPool(go);
            }

            return false;

        }
        /// <summary>
        /// 获取指定预设的GameObject对象池
        /// </summary>
        /// <param name="prefabs">预设 对象</param>
        /// <param name="autoCreate">不存是否创建</param>
        /// <returns></returns>
        public PoolList<GameObject> GetGameObjectPoolList(GameObject prefabs, bool autoCreate = true)
        {
            return _GetGameObjectPoolList(prefabs.name,prefabs, autoCreate);
        }
        public PoolList<GameObject> GetGameObjectPoolListByKey(string key, bool autoCreate = true)
        {
            return _GetGameObjectPoolList(key, null, autoCreate);
        }
        /// <summary>
        /// 获取指定预设0 的GameObject对象池
        /// </summary>
        /// <param name="prefabs">预设 对象资源的路径</param>
        /// <param name="autoCreate">不存是否创建</param>
        /// <returns></returns>
        public PoolList<GameObject> GetGameObjectPoolList(string name, bool autoCreate = true)
        {
            return _GetGameObjectPoolList(name, null, false);
        }


        //public void ClearOnce()
        //{
        //    AutoClear();
        //}

        //*******************************************
        
        //private void StartAutoClear(float t)
        //{
        //    if (IsInvoking("AutoClear") == false)
        //    {
        //        _lastCheckUpdateTime = Time.time;
        //        _checkUpdateTime = t;
        //        Invoke("AutoClear", t);
        //    }
        //    else
        //    {
        //        if (t < _checkUpdateTime)
        //        {
        //            float tempT = Time.time - _lastCheckUpdateTime;
        //            if (t<tempT)
        //            {
        //                AutoClear();
        //            }
        //            else
        //            {
        //                t = t - tempT;
        //                _lastCheckUpdateTime = Time.time;
        //                _checkUpdateTime = t;
        //                CancelInvoke("AutoClear");
        //                Invoke("AutoClear", t);
        //            }
        //        }
        //    }
        //}


        private PoolList<GameObject> _GetGameObjectPoolList(string key,GameObject prefabs, bool autoCreate = true)
        {
            PoolList<GameObject> poolList = null;
            if (_gameObjectPoolDic.TryGetValue(key,out poolList)==false&& autoCreate)
            {
                poolList = new PoolList<GameObject>(key,prefabs);
                _gameObjectPoolDic[key] = poolList;
                PoolSetData setData = null;
                float t = _autoClearTime;
                if (_gameObjectpoolSetDataDic.TryGetValue(key, out setData))
                {
                    poolList.setData = setData;
                    t = setData.autoClearTime;
                }
                //StartAutoClear(t);
            }
            return poolList;
        }


        /// <summary>
        /// 把GameObject缓存到对象池
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="perfabs"></param>
        public void PushGameObject(GameObject obj, GameObject perfabs)
        {
     
            PoolList<GameObject> poolList = GetGameObjectPoolList(perfabs, true);
            poolList.PushObject(obj);
        }

        public void PushGameObjectByKey(GameObject obj, string key)
        {
      
            PoolList<GameObject> poolList = GetGameObjectPoolListByKey(key);
            poolList.PushObject(obj);
        }
        public void PushGameObject(GameObject obj)
        {

            PoolList<GameObject> poolList = GetGameObjectPoolListByKey(obj.name, true);
            poolList.PushObject(obj);
        }

        public void PushGameObject(MonoBehaviour mono)
        {
            PushGameObject(mono.gameObject);
        }
        //************** 私方法 ***************
        /// <summary>
        /// 检查要请理的对象池
        /// </summary>
      
        //private void AutoClear()
        //{
        //    float t = _autoClearTime;
        //    float timeNow = Time.time;
        //    _deleteKeyList.Clear();
        //       CancelInvoke("AutoClear");
        //    if (_gameObjectPoolDic.Count > 0)
        //    {
        //        foreach (KeyValuePair<string, PoolList<GameObject>> kv in _gameObjectPoolDic)
        //        {
        //            PoolList<GameObject> poolList = kv.Value;

        //            float atime = poolList.setData != null ? poolList.setData.autoClearTime : _autoClearTime;
        //            float tt = atime-(timeNow - poolList.lastClearTime);
        //            if (tt <= 0.1f)
        //            {
        //                poolList.Clear(true);
        //                tt = atime;
        //            }
        //            if (poolList.count == 0)
        //            {
        //                _deleteKeyList.Add(kv.Key);
        //            }
        //            else if(tt>0)
        //                t = Mathf.Min(t, tt);
        //        }
        //        for (int i = _deleteKeyList.Count - 1; i >= 0; i--)
        //        {
        //            _gameObjectPoolDic.Remove(_deleteKeyList[i]);
        //            Transform tf= transform.Find(_deleteKeyList[i]);
        //            if (tf != null)
        //                Destroy(tf.gameObject);
        //        }
        //        _deleteKeyList.Clear();
        //    }
        //    if (_poolDic.Count > 0)
        //    {
        //        foreach (KeyValuePair<string, IPoolList> kv in _poolDic)
        //        {
        //            IPoolList poolList = kv.Value;

        //            float atime = poolList.setData != null ? poolList.setData.autoClearTime : _autoClearTime;
        //            float tt = atime - (timeNow - poolList.lastClearTime);
        //            if (tt <= 0.1f)
        //            {
        //                poolList.Clear(true);
        //                tt = atime;
        //            }

        //            if (poolList.count == 0)
        //            {
        //                _deleteKeyList.Add(kv.Key);
        //            }
        //            else if(tt>0)
        //                t = Mathf.Min(t, tt);
        //        }
        //        for (int i = _deleteKeyList.Count - 1; i >= 0; i--)
        //        {
        //            _poolDic.Remove(_deleteKeyList[i]);
        //        }
        //        _deleteKeyList.Clear();
        //    }
        //    //if (t <= 0)
        //    //    t = _autoClearTime;
        //    //if (t > 0&&( _poolDic.Count>0||_gameObjectPoolDic.Count>0))
        //    //    StartAutoClear(t);
        //}

        /// <summary>
        /// 检查最大数量上限
        /// </summary>
        private void CheckLimit()
        {
            foreach (KeyValuePair<string, PoolList<GameObject>> kv in _gameObjectPoolDic)
            {
                PoolList<GameObject> poolList = kv.Value;
                if (poolList.setData == null)
                {
                    poolList.CheckLimit();
                }

            }

            foreach (KeyValuePair<string, IPoolList> kv in _poolDic)
            {
                IPoolList poolList = kv.Value;
                if (poolList.setData == null)
                {
                    poolList.CheckLimit();
                }
            }
        }


        private PoolSetData GetPoolSetData<T>(bool autoCreate=false) where T : class, new()
        {
            Type type = typeof(T);
            return GetPoolSetData(type, autoCreate);
        }
        private PoolSetData GetPoolSetData(Type type,bool autoCreate = false) 
        {
            string key = type.FullName;
            PoolSetData setData = null;
            if (_poolSetDataDic.TryGetValue(key, out setData) == false)
            {
                if (autoCreate)
                {
                    setData = new PoolSetData();
                    setData.name = key;
                    //setData.autoClearTime = autoClearTime;
                    _poolSetDataDic[key] = setData;
                }
            }
            return setData;
        }
        private PoolSetData GetGameObjectPoolSetData(string key,bool autoCreate = false) 
        {
            PoolSetData setData = null;
            if (_gameObjectpoolSetDataDic.TryGetValue(key, out setData) == false)
            {
                if (autoCreate)
                {
                    setData = new PoolSetData();
                    setData.name = key;
                    //setData.autoClearTime = autoClearTime;
                    _gameObjectpoolSetDataDic[key] = setData;
                }
            }
            return setData;
        }
    }






    ///////////////////////////////////////////////////////////////////////////
    ///      额外类定义
    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// 对象池设置信息
    /// </summary>
    [Serializable]
    public class PoolSetData
    {
        public string name;
        public int maxCount = -1;
        public int autoClearTime = 5*300 ;
    }

    ///////////////////////////////////////////////////////////////////////////


    /// <summary>
    /// 单个对象池
    /// author Oscar
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PoolList<T> :IPoolList where T : class, new()
    {
        private Queue<T> _pools = new Queue<T>();
        private bool _isGameObjectPool;
        private Transform _poolParent;

        /// <summary>
        /// 最近活动个数
        /// </summary>
        private int _lastActiveCount;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="prefabs"> 如果是GameObject时，这里必需要有预投</param>
        /// <param name="prefabsPath"> 如果是通过Resource加载的预置，这里必需要有路径</param>
        internal PoolList(string name,GameObject prefabs = null)
        {
            this.name = name;
            this.prefabs = prefabs;
            this.poolType = typeof(T);
            _isGameObjectPool = poolType == typeof(GameObject);
            lastClearTime = Time.time;
          //  if (_isGameObjectPool && prefabs == null)
              //  throw new Exception("GameObject对象池，参数prefabs不参为空");
        }

        public PoolSetData setData { get; set; }
        /// <summary>
        /// 最后一次活动时间，
        /// </summary>
        public float lastClearTime { get;private set; }

        /// <summary>
        /// 对象池名字
        /// </summary>
        public string name {  get;private set; }

        /// <summary>
        /// 当前对象数
        /// </summary>
        public int count
        {
            get { return _pools.Count; }
        }

        /// <summary>
        /// 存储的对象类型
        /// </summary>
        public Type poolType  { get;private set;}

        /// <summary>
        /// 存储的对象的预设
        /// </summary>
        public GameObject prefabs  {  get; internal set; }

        
        public bool IsInPool(T obj)
        {
            if (_pools.Count > 0)
            {
                return _pools.Contains(obj);
            }
            return false;
        }
        /// <summary>
        /// 获取一个对象 
        /// 使用反射进行创建，效率慢10倍，建意判断为null在外面创建
        /// </summary>
        /// <param name="autoCreate">不存在是否创建</param>
        /// <returns></returns>
        public T GetObject(bool autoCreate = true)
        {
            T obj = null;
            if (_pools.Count > 0)
            {
                while (_pools.Count > 0)
                {
                    obj = _pools.Dequeue();
                    _lastActiveCount--;
                    if (obj != null)
                    {
                        if (_isGameObjectPool)
                        {
                            if (IsAlive(obj as GameObject))
                                break;
                            else
                                obj = null;
                        }
                        else
                            break;
                    }
                }
                if (_lastActiveCount <= 0 && _pools.Count > 0)
                    _lastActiveCount = 1;//留一个
            }
            if (obj == null && autoCreate)
            {
                if (_isGameObjectPool)
                {
                    if (prefabs != null)
                    {
                        GameObject go = GameObject.Instantiate(prefabs) as GameObject;
                        go.name = prefabs.name;
                       // if (go.activeSelf == false)
                        //    go.SetActive(true);
                        obj = go as T;
                    }
                    else
                        return null;
                }
                else
                    obj = System.Activator.CreateInstance<T>();
            }
            if (obj != null && _isGameObjectPool)
            {
                GameObject go = obj as GameObject;
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                //go.transform.localScale = Vector3.one;
                if (prefabs!=null&& prefabs.activeSelf)
                    go.SetActive(true);
            }
        
            return obj;
        }

        /// <summary>
        /// 获取一个GameObject对象
        /// </summary>
        /// <param name="parent">放在什么父级下</param>
        /// <param name="autoCreate"></param>
        /// <returns></returns>
        public T GetGameObject(Transform parent, bool autoCreate = true)
        {
            T obj = GetObject(autoCreate) ;
            if (obj is GameObject && _isGameObjectPool)
            {
                GameObject go = obj as GameObject;
                BootTool.SetChildParent(go, parent);
            }
            return obj;
        }

        /// <summary>
        /// 缓存一个对象
        /// </summary>
        /// <param name="obj"></param>
        public void PushObject(T obj)
        {
            if (_pools.Contains(obj))
                return;
            if(_isGameObjectPool)
            {
                if (IsAlive(obj as GameObject) == false)
                    return;
            }
            int tempCount = setData!=null ? setData.maxCount : PoolManager.Instance.maxCount;


            if (_pools.Count < tempCount)
            {
                if (_isGameObjectPool)
                {
                    GameObject go = obj as GameObject;
                    if (_poolParent == null)
                    {
                        Transform tf = PoolManager.Instance.transform.Find(name);
                        if (tf != null)
                            _poolParent = tf;
                        else
                        {
                            _poolParent = new GameObject(name).transform;
                            _poolParent.gameObject.SetActive(false);
                            _poolParent.parent = PoolManager.Instance.transform;
                        }
                    }
                    IReference[] gbs = go.GetComponents<IReference>();
                    if (gbs != null)
                    {
                        for (int i = 0; i < gbs.Length; i++)
                        {
                            gbs[i].Clear();
                        }
                    }
                    go.transform.SetParent(_poolParent, false);
                    go.SetActive(false);
                }
                else if (obj is IReference)
                    ((IReference)obj).Clear();

                _pools.Enqueue(obj);

                _lastActiveCount++;
            }
            else if (obj is UnityEngine.Object)
            {
                GameObject.Destroy(obj as UnityEngine.Object);
            }
        }

        /// <summary>
        /// 清除多少个对象
        /// </summary>
        /// <param name="count"></param>
        public void Clear(bool inactivity = false)
        {
            if (_lastActiveCount < 0|| inactivity==false)
                _lastActiveCount = 0;
            int num = _pools.Count - _lastActiveCount; //算出不活跃的
            _lastActiveCount = 0;
            if (_pools.Count > 0 && num > 0)
            {
                for (int i = 0; i < num; i++)
                {
                    if (_pools.Count != 0)
                    {
                        object obj = _pools.Dequeue();
                        if (obj is UnityEngine.Object)
                            GameObject.Destroy(obj as UnityEngine.Object);
                    }
                }
            }
            lastClearTime = Time.time;
        }

        /// <summary>
        /// 上限检查
        /// </summary>
        public void CheckLimit()
        {
            int tempCount = setData != null ? setData.maxCount : PoolManager.Instance.maxCount;
            if (_pools.Count > tempCount)
            {
                int n = _pools.Count - tempCount;
                for (int i = 0; i < n; i++)
                {
                    object obj = _pools.Dequeue();
                    if (obj is UnityEngine.Object)
                        GameObject.Destroy(obj as UnityEngine.Object);
                }
            }
        }

        private bool IsAlive(GameObject go )
        {
            try
            {
                if (go!=null&&go.name != null)
                    return true;
            }
            catch 
            {
                //Debug.LogError(ee);
                return false;
            }
            return false;
        }
    }


    /// <summary>
    /// 
    /// 由于unity3d只支持.net 2.0，不支持4.0的协变成逆变特性，所以只有定义一接口来做转变
    /// author Oscar
    /// </summary>
    public interface IPoolList
    {
        //对象池名字
        string name{get;}

        PoolSetData setData { get; set; }
        /// <summary>
        /// 最后一次活动时间，
        /// </summary>
        float lastClearTime  { get; }



        /// <summary>
        /// 当前对象数
        /// </summary>
        int count { get; }

        /// <summary>
        /// 存储的对象类型
        /// </summary>
        Type poolType{  get; }

        GameObject prefabs  { get; }

        void Clear(bool inactivity = true);
        void CheckLimit();
    }
}
