// GameThreadQueue.cs
// Create by xiaojl Mar/20/2023
// 主线程任务队列

using System;
using System.Collections.Generic;

namespace Network.Tcp.Core
{
    internal class GameThreadQueue
    {
        private static List<Action> _runQueue;
        private static List<Action> _taskQueue;

        public static void Init()
        {
            _runQueue = new List<Action>();
            _taskQueue = new List<Action>();
        }

        public static void Destroy()
        {
            _runQueue.Clear();
            _taskQueue.Clear();

            _runQueue = null;
            _taskQueue = null;
        }

        public static void Update()
        {
            // 加锁
            lock (_taskQueue)
            {
                // 拷贝任务队列
                _runQueue.AddRange(_taskQueue);

                // 清除任务队列
                _taskQueue.Clear();
            }

            // 处理网络异步任务
            foreach (var task in _runQueue)
            {
                try
                {
                    task();
                }
                catch (Exception ex)
                {

                }
            }

            _runQueue.Clear();
        }

        public static void Dispatch(Action task)
        {
            try {
                lock (_taskQueue)
                {
                    _taskQueue.Add(task);
                }
            } catch (Exception e) {
                LoggerUtils.Log("Dispatch Error:" + e.Message + "," + e.StackTrace);
            }

        }
    }
}
