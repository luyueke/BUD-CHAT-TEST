// NetworkDispatcher.cs
// Create by xiaojl Mar/15/2023
// 网络事件/消息分发器

using System;
using System.Collections.Generic;
using Google.Protobuf;
using UnityEngine;

namespace Network.Tcp.Core
{
    //消息监听委托
    public delegate void MsgListener(byte[] data);

    internal partial class TcpNetworkMgr
    {
        // 消息函数列表
        private Dictionary<NetMsg, MsgListener> _hooks;

        // 事件监听列表
        private List<Action<NetworkStatus>> _eventHooks;

        // 初始化
        private void InitDispatcher()
        {
            _hooks = new Dictionary<NetMsg, MsgListener>();
            _eventHooks = new List<Action<NetworkStatus>>();
        }

        // 析构
        private void DestroyDispatcher()
        {
            _hooks.Clear();
            _hooks = null;
        }

        private bool IsHookContains(MsgListener hook, MsgListener func)
        {
            foreach (MsgListener d in hook.GetInvocationList())
            {
                if (d != null && d == func)
                {
                    return true;
                }
            }

            return false;
        }

        // 添加消息函数
        public void AddHook(NetMsg msg, MsgListener func)
        {
            if (_hooks.TryGetValue(msg, out var hook))
            {
                if (!IsHookContains(hook, func))
                {
                    hook = (MsgListener)Delegate.Combine(hook, func);
                    _hooks[msg] = hook;
                }
                else
                {
                    LoggerUtils.LogError($"Network add hook error : repeat MsgListener:{func.Method.Name}");
                }
            }
            else
            {
                _hooks.Add(msg, func);
            }
        }

        // 移除消息函数
        public void RemoveHook(NetMsg msg, MsgListener func)
        {
            if (_hooks == null)
            {
                return;
            }
            if (_hooks.TryGetValue(msg, out var hook))
            {
                if (IsHookContains(hook, func))
                {
                    hook = (MsgListener)Delegate.Remove(hook, func);

                    if (hook == null)
                    {
                        _hooks.Remove(msg);
                    }
                    else
                    {
                        _hooks[msg] = hook;
                    }
                }
                else
                {
                    LoggerUtils.LogError($"Network remove hook error : not found MsgListener:{func.Method.Name}");
                }
            }
            else
            {
                LoggerUtils.LogError($"Network remove hook error : not found MsgListener:{func.Method.Name}");
            }
        }


        // 添加事件监听
        public void AddEventHook(Action<NetworkStatus> func)
        {
            if (!_eventHooks.Contains(func))
                _eventHooks.Add(func);
        }

        // 移除事件监听
        public void RemoveEventHook(Action<NetworkStatus> func)
        {
            if (_eventHooks.Contains(func))
                _eventHooks.Remove(func);
        }

        // 消息分发
        private void DispatchMessage(NetMsg msg, byte[] data)
        {
            if (_hooks.TryGetValue(msg, out var hook))
            {
                hook?.Invoke(data);
            }
        }

        // 事件分发
        private void DispatchEvent(NetworkStatus status)
        {
            for (int i = 0; i < _eventHooks.Count; ++i)
                _eventHooks[i]?.Invoke(status);
        }

        public void SendCustomCmd(NetCmd cmd, IMessage data){
            Send(cmd, data);
        }
    }
}