using System;
using System.Collections.Generic;
using UnityEngine;

namespace Basic.Utils
{
    public class TaskRunner
    {
        private object result;
        private Action<object> msgResp;

        public TaskRunner(object val, Action<object> resp)
        {
            result = val;
            msgResp = resp;
        }

        public void Invoke()
        {
            msgResp?.Invoke(result);
            result = null;
        }
    }
    
    //子线程派发数据到主线程
    public class MainThreadDispatcher : MonoBehaviour
    {
        private readonly List<TaskRunner> executionQueue = new List<TaskRunner>();
        private readonly List<Action> executionActionQueue = new List<Action>();
        private static bool initialized = false;

        private static MainThreadDispatcher _current;

        //private int _count;
        public static MainThreadDispatcher Current
        {
            get
            {
                Init();
                return _current;
            }
        }


        public static void Init()
        {
            if (!initialized)
            {
                if (!Application.isPlaying)
                    return;
                initialized = true;
                var g = new GameObject("MainThreadDispatcher");
                _current = g.AddComponent<MainThreadDispatcher>();
            }
        }

        
        public static bool Exists()
        {
            return _current != null;
        }


        void Awake()
        {
            _current = this;
            initialized = true;
            this.gameObject.DontDestroy();
        }
        
        List<Action> _currentActions = new List<Action>();
        List<TaskRunner> _currentTasks = new List<TaskRunner>();

        // Update is called once per frame
        void Update()
        {
            lock (executionActionQueue)
            {
                _currentActions.Clear();
                _currentActions.AddRange(executionActionQueue);
                executionActionQueue.Clear();
            }

            foreach (var act in _currentActions)
            {
                act?.Invoke();
            }

            lock (executionQueue)
            {
                _currentTasks.Clear();
                _currentTasks.AddRange(executionQueue);
                executionQueue.Clear();
            }

            foreach (var act in _currentTasks)
            {
                act?.Invoke();
            }
        }

        public static void Enqueue(TaskRunner task)
        {
            var current = _current;
            if (current != null)
            {
                lock (current.executionQueue)
                {
                    if (current)
                    {
                        current.executionQueue.Add(task);
                    }
                }
            }
        }

        public static void Enqueue(Action action)
        {
            var current = _current;
            if (current != null)
            {
                lock (current.executionActionQueue)
                {
                    if (current)
                    {
                        current.executionActionQueue.Add(action);
                    }
                }
            }
        }

        public static void Shutdown()
        {
            if (_current == null)
            {
                return;
            }

            Destroy(_current.gameObject);
            _current = null;
            initialized = false;
        }

        void OnDestroy()
        {
            executionQueue.Clear();
            executionActionQueue.Clear();
            _current = null;
            initialized = false;
        }
    }
}