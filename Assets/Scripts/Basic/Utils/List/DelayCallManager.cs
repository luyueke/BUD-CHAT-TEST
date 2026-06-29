using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Fsbm.Runtime
{
    /// <summary>
    /// 延时调用。
    /// </summary>
    public static class DelayCall
    {
        public static void CallLate(Action fun, float time = 0, int repeat = 1)
        {
            DelayCallManager.Instance.AddData(fun, time, repeat);
        }
        public static void CancelCallLate(Action fun)
        {
            DelayCallManager.Instance.Remove(fun);
        }
        public static uint Invoke(this Action fun, float time, int repeat = 0)
        {
            return DelayCallManager.Instance.AddData(fun, time, repeat);
        }
    }
    /// <summary>
    /// 延时调用管理器
    /// </summary>
    public class DelayCallManager : MonoSingleton<DelayCallManager>
    {
        private List<DelayCallData> _dataList = new List<DelayCallData>();

        private List<Action> _dataListTemp = new List<Action>();

        public static uint seqId = 0;
        public uint AddData(Action fun, float time, int repeat = 0)
        {
            var data = GetDelayCallData(fun);
            if(data == null)
            {
                data = GetDelayCallData();
                data.time = Time.realtimeSinceStartup;
                _dataList.Add(data);
            }
            data.fun = fun;
            data.repeat = repeat;
            data.delayTime = time;
            return data.id;
        }

        public bool Remove(Action fun)
        {
            var data = GetDelayCallData(fun);
            if (data != null)
            {
                _dataList.Remove(data);
                //ReferencePool.Release(data);
                return true;
            }
            return false;
        }
        void Update()
        {
            if(_dataList.Count>0)
            {
                var t = Time.realtimeSinceStartup;
                _dataListTemp.Clear();
                for (int i = 0, n = _dataList.Count; i < n; i++)
                {
                    var obj = _dataList[i];

                    if (t - obj.time >= obj.delayTime)
                    {
                        obj.time = t;
                        var fun = obj.fun;
                        if (obj.repeat > 1)
                            obj.repeat--;
                        else if (obj.repeat <= 1)
                        {
                            _dataList.RemoveAt(i);
                            //ReferencePool.Release(obj);
                            --n; --i;
                        }
                        _dataListTemp.Add(fun);
                    }
                }
                if (_dataListTemp.Count != 0)
                {
                    for (var i = 0; i < _dataListTemp.Count; i++)
                    {
                        var fun = _dataListTemp[i];
                        fun.Invoke();
                    }
                    _dataListTemp.Clear();
                }
            }
        }

        private DelayCallData GetDelayCallData(Action fun)
        {
            for (int i = 0, n = _dataList.Count; i < n; i++)
            {
                var obj = _dataList[i];
                if (obj.fun == fun)
                {
                    return obj;
                }
            }
            return null;
        }

        private DelayCallData GetDelayCallData()
        {
            var data = new DelayCallData();
            data.id = ++seqId;
            return data;
        }

        private class DelayCallData : IReference
        {
            public uint id = 0;
            public Action fun;
            public int repeat = 0;
            public float delayTime;
            public float time;
            public void Clear()
            {
                fun = null;
                time = 0;
                repeat = 0;
                delayTime = 0;
                id = 0;
            }
        }
    }
}
