using UnityEngine;

namespace Fsbm.Runtime
{
    /// <summary>
    /// Mono单例
    /// </summary>
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        private static T _instance = null;

        private static Transform _transform;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    if (_transform == null)
                    {
                        _transform = BootTool.GetRootChild("Singletons");
                    }
                    var type = typeof(T);
                    var instanceName = type.FullName;
                    var instanceTf = _transform.Find(instanceName);
                    if (instanceTf == null)
                    {
                        var go = new GameObject(instanceName);
                        instanceTf = go.transform;
                        instanceTf.SetParent(_transform);
                    }

                    _instance = instanceTf.GetComponent<T>(); 
                    if (_instance == null)
                        _instance = instanceTf.gameObject.AddComponent<T>();

                }
                return _instance;
            }
        }

        public static bool HasInstance()
        {
            return _instance != null;
        }

        protected virtual void Awake()
        {
            if (_instance != null && _instance.gameObject != gameObject)
            {
                if (Application.isPlaying)
                    Destroy(gameObject);
                else
                    DestroyImmediate(gameObject);
            }
            if (_instance == null)
                _instance = this as T;
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

    }
}
