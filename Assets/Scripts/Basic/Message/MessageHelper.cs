// @Author: YangJie
// @Description:
// @Date:  2023/07/12
// @Modify:

using System;
using System.Collections.Generic;

namespace Message
{
    public delegate void MessageHandler();

    public delegate void MessageHandler<T>(T arg);

    public delegate void MessageHandler<T1, T2>(T1 arg1, T2 arg2);

    public delegate void MessageHandler<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3);

    public delegate void MessageHandler<T1, T2, T3, T4>(T1 arg1, T2 arg2, T3 arg3, T4 arg4);

    public static class MessageHelper
    {
        private static Dictionary<string, List<Delegate>> _messageTable = new Dictionary<string, List<Delegate>>();

        public static void AddListener(string message, MessageHandler handler)
        {
            AddListener(message, (Delegate)handler);
        }

        private static void AddListener(string message, Delegate handler)
        {
            if (PreListenerAdding(message, handler))
            {
                if (!_messageTable.TryGetValue(message, out var delegates))
                {
                    delegates = new List<Delegate>();
                    _messageTable.Add(message, delegates);
                }
                var dIndex = delegates.FindIndex(tmp => tmp.GetType() == handler.GetType());
                if (dIndex == -1)
                {
                    delegates.Add(handler);
                }
                else
                {
                    delegates[dIndex] = Delegate.Combine(delegates[dIndex], handler);
                }
            }
            
        }

        public static void AddListener<T>(string message, MessageHandler<T> handler)
        {
            AddListener(message, (Delegate)handler);
        }

        public static void AddListener<T1, T2>(string message, MessageHandler<T1, T2> handler)
        {
            AddListener(message, (Delegate)handler);
        }
        
        public static void AddListener<T1, T2, T3>(string message, MessageHandler<T1, T2, T3> handler)
        {
            AddListener(message, (Delegate)handler);
        }
        
        public static void AddListener<T1, T2, T3, T4>(string message, MessageHandler<T1, T2, T3, T4> handler)
        {
            AddListener(message, (Delegate)handler);
        }

        public static void Broadcast(string message)
        {
            if (!_messageTable.TryGetValue(message, out var delegates)) return;
            var snapshot = new List<Delegate>(delegates);
            foreach (var tmpDelegate in snapshot)
            {
                if (tmpDelegate is MessageHandler source)
                {
                    source();
                }
            }
        }

        public static void Broadcast<T>(string message, T arg)
        {
            if (!_messageTable.TryGetValue(message, out var delegates)) return;
            var snapshot = new List<Delegate>(delegates);
            foreach (var tmpDelegate in snapshot)
            {
                if (tmpDelegate is MessageHandler<T> source)
                {
                    source(arg);
                }
            }
        }

        public static void Broadcast<T1, T2>(string message, T1 arg1, T2 arg2)
        {
            if (!_messageTable.TryGetValue(message, out var delegates)) return;
            var snapshot = new List<Delegate>(delegates);
            foreach (var tmpDelegate in snapshot)
            {
                if (tmpDelegate is MessageHandler<T1, T2> source)
                {
                    source(arg1, arg2);
                }
            }
        }

        public static void Broadcast<T1, T2, T3>(string message, T1 arg1, T2 arg2, T3 arg3)
        {
            if (!_messageTable.TryGetValue(message, out var delegates)) return;
            var snapshot = new List<Delegate>(delegates);
            foreach (var tmpDelegate in snapshot)
            {
                if (tmpDelegate is MessageHandler<T1, T2, T3> source)
                {
                    source(arg1, arg2, arg3);
                }
            }
        }

        public static void Broadcast<T1, T2, T3, T4>(string message, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            if (!_messageTable.TryGetValue(message, out var delegates)) return;
            var snapshot = new List<Delegate>(delegates);
            foreach (var tmpDelegate in snapshot)
            {
                if (tmpDelegate is MessageHandler<T1, T2, T3, T4> source)
                {
                    source(arg1, arg2, arg3, arg4);
                }
            }
        }

        private static void PostListenerRemoving(string message)
        {
            if (_messageTable.TryGetValue(message, out var tmpDelegates))
            {
                if (tmpDelegates != null)
                {
                    for (var i = tmpDelegates.Count - 1; i >= 0; i--)
                    {
                        if (tmpDelegates[i] == null || tmpDelegates[i].GetInvocationList().Length <= 0)
                        {
                            tmpDelegates.RemoveAt(i);
                        }
                    }
                }
                if (tmpDelegates == null || tmpDelegates.Count <= 0)
                {
                    _messageTable.Remove(message);
                }
            }
        }

        private static bool PreBroadcasting(string message)
        {
            return _messageTable.ContainsKey(message);
        }

        private static bool PreListenerAdding(string message, Delegate listenerForAdding)
        {
            if (null == listenerForAdding)
            {
                return false;
            }
            if (_messageTable.TryGetValue(message, out var delegates))
            {
                foreach (var tmpDelegate in delegates)
                {
                    if (tmpDelegate.GetType() == listenerForAdding.GetType())
                    {
                        foreach (var delegateCur in tmpDelegate.GetInvocationList())
                        {
                            if (listenerForAdding == delegateCur)
                            {
                                //已添加过，无需重复添加
                                return false;
                            }
                        }
                    }
                }
            }
            return true;
        }

        private static bool PreListenerRemoving(string message, Delegate listenerForRemoving)
        {
            if (!_messageTable.TryGetValue(message, out var delegates)) return false;
            if (delegates == null)
            {
                return false;
            }
            foreach (var tmpDelegate in delegates)
            {
                if (tmpDelegate.GetType() == listenerForRemoving.GetType())
                {
                    foreach (var delegateCur in tmpDelegate.GetInvocationList())
                    {
                        if (listenerForRemoving == delegateCur)
                        {
                            //已添加过，无需重复添加
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        
        private static void RemoveListener(string message, Delegate handler)
        {
            if (PreListenerRemoving(message, handler))
            {
                var dFindIndex = _messageTable[message].FindIndex(tmp => tmp.GetType() == handler.GetType());
                _messageTable[message][dFindIndex] = Delegate.Remove(_messageTable[message][dFindIndex], handler);
            }
            PostListenerRemoving(message);
        }

        public static void RemoveListener(string message, MessageHandler handler)
        {
            RemoveListener(message, (Delegate)handler);
        }

        public static void RemoveListener<T>(string message, MessageHandler<T> handler)
        {
            RemoveListener(message, (Delegate)handler);
        }

        public static void RemoveListener<T1, T2>(string message, MessageHandler<T1, T2> handler)
        {
            RemoveListener(message, (Delegate)handler);
        }

        public static void RemoveListener<T1, T2, T3>(string message, MessageHandler<T1, T2, T3> handler)
        {
            RemoveListener(message, (Delegate)handler);
        }

        public static void RemoveListener<T1, T2, T3, T4>(string message, MessageHandler<T1, T2, T3, T4> handler)
        {
            RemoveListener(message, (Delegate)handler);
        }

        public static void Release()
        {
            _messageTable.Clear();
        }
    }
}