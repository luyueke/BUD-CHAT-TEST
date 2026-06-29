using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Fsbm.Runtime
{

 
    [Serializable]
    /// <summary>
    ///  对UnityEenet的一次封装, 为了放心使用AddListener，不让重复添加,
    ///  同时培加事件派发来源UEvent.currentTarget，方法知道这个事件是从哪里派发出来的
    /// </summary>
    public class UEvent : UnityEvent
    {
        public static UnityEventBase currentEvent
        {
            get; internal set;
        }


        private Dictionary<UnityAction, bool> _callbackDic ;
        private List<UnityAction> _tempList;

  

        public void AddListenerOnce(UnityAction call)
        {
            AddListener(call, true);
        }

        public new void AddListener(UnityAction call)
        {
            AddListener(call, false);
        }
        private void AddListener(UnityAction call, bool isInvokeRemove)
        {
            if (call == null)
                return;
            if (_callbackDic == null)
                _callbackDic = new Dictionary<UnityAction, bool>();
            if (_callbackDic.ContainsKey(call) == false)
            {
                _callbackDic.Add(call,isInvokeRemove);
                base.AddListener(call);
            }
            else
            {
                _callbackDic[call] = isInvokeRemove;
            }
        }
        

        public new void Invoke()
        {
            UnityEventBase tempEvent = UEvent.currentEvent;
            UEvent.currentEvent = this;
  
 
            if (_callbackDic != null)
            {
                if (_tempList == null)
                    _tempList = new List<UnityAction>();
                _tempList.Clear();
                foreach (KeyValuePair<UnityAction, bool> keyvalue in _callbackDic)
                {
                    if (keyvalue.Value)
                        _tempList.Add(keyvalue.Key);
                }
                for (int i = 0; i < _tempList.Count; i++)
                {
                    UnityAction call = _tempList[i];
                    _callbackDic.Remove(call);
                }
            }
            base.Invoke();
            if (_callbackDic != null)
            {
                for (int i = 0; i < _tempList.Count; i++)
                {
                    UnityAction call = _tempList[i];
                    if (_callbackDic.ContainsKey(call) == false)
                    {
                        base.RemoveListener(call);
                    }
                }
                _tempList.Clear();
            }
            UEvent.currentEvent = tempEvent;
        }
        
        public new void RemoveListener(UnityAction call)
        {
            if (call == null)
                return;
            if (_callbackDic != null && _callbackDic.ContainsKey(call))
            {
                base.RemoveListener(call);
                _callbackDic.Remove(call);
            }
        }
        public new void RemoveAllListeners()
        {
            base.RemoveAllListeners();
            if(_callbackDic != null )
                _callbackDic.Clear();
        }
        public bool HasListener(UnityAction call)
        {
            if (_callbackDic != null && _callbackDic.ContainsKey(call) )
            {
                return true;
            }
            return false;
        }
        public bool HasListener()
        {
            if (_callbackDic!=null&&_callbackDic.Count >=0)
                return true;
            else if (GetPersistentEventCount() > 0)
                return true;
            return false;
        }

        public static UEvent operator + (UEvent lhs,UnityAction rhs)
        {
            lhs.AddListener(rhs);
            return lhs;
        }
        public static UEvent operator -(UEvent lhs, UnityAction rhs)
        {
            lhs.RemoveListener(rhs);
            return lhs;
        }

    }

    [Serializable]
    public class UEvent<T0> : UnityEvent<T0>
    {
        private Dictionary<UnityAction<T0>, bool> _callbackDic ;
        private List<UnityAction<T0>> _tempList;
        public void AddListenerOnce(UnityAction<T0> call)
        {
            AddListener(call, true);
        } 

        public new void AddListener(UnityAction<T0> call)
        {
            AddListener(call, false);
        }
        private void AddListener(UnityAction<T0> call, bool isInvokeRemove)
        {
            if (call == null)
                return;
            if (_callbackDic == null)
                _callbackDic = new Dictionary<UnityAction<T0>, bool>();
            if (_callbackDic.ContainsKey(call) == false)
            {
                _callbackDic.Add(call, isInvokeRemove);
                base.AddListener(call);
            }
            else
            {
                _callbackDic[call] = isInvokeRemove;
            }
        }


        public new void Invoke(T0 arg0)
        {
            UnityEventBase tempEvent = UEvent.currentEvent;
            UEvent.currentEvent = this;
    
            if (_callbackDic != null)
            {
                if (_tempList == null)
                    _tempList = new List<UnityAction<T0>>();
                _tempList.Clear();

                foreach (KeyValuePair<UnityAction<T0>, bool> keyvalue in _callbackDic)
                {
                    if (keyvalue.Value)
                        _tempList.Add(keyvalue.Key);
                }
                for (int i = 0; i < _tempList.Count; i++)
                {
                    UnityAction<T0> call = _tempList[i];
                    _callbackDic.Remove(call);
                }
            }

            base.Invoke(arg0);
            if (_callbackDic != null)
            {
                for (int i = 0; i < _tempList.Count; i++)
                {
                    UnityAction<T0> call = _tempList[i];
                    if (_callbackDic.ContainsKey(call) == false)
                    {
                        base.RemoveListener(call);
                    }
                }
                _tempList.Clear();
            }
            UEvent.currentEvent = tempEvent;
        }

        public new void RemoveListener(UnityAction<T0> call)
        {
            if (call == null)
                return;
            if (_callbackDic!=null&&_callbackDic.ContainsKey(call))
            {
                base.RemoveListener(call);
                _callbackDic.Remove(call);
            }
        }
        public new void RemoveAllListeners()
        {
            base.RemoveAllListeners();
            if(_callbackDic != null)
                _callbackDic.Clear();
        }
        public bool HasListener(UnityAction<T0> call)
        {
            if (_callbackDic != null&&_callbackDic.ContainsKey(call))
            {
                return true;
            }
            return false;
        }
        public bool HasListener()
        {
            if (_callbackDic != null&&_callbackDic.Count >= 0)
                return true;
            else if (GetPersistentEventCount() > 0)
                return true;
            return false;
        }
        public static UEvent<T0> operator +(UEvent<T0> lhs, UnityAction<T0> rhs)
        {
            lhs.AddListener(rhs);
            return lhs;
        }
        public static UEvent<T0> operator -(UEvent<T0> lhs, UnityAction<T0> rhs)
        {
            lhs.RemoveListener(rhs);
            return lhs;
        }

    }




    [Serializable]
    public class UEvent<T0,T1> : UnityEvent<T0, T1>
    {
        private Dictionary<UnityAction<T0, T1>, bool> _callbackDic;
        private List<UnityAction<T0, T1>> _tempList;



        public void AddListenerOnce(UnityAction<T0, T1> call)
        {
            AddListener(call, true);
        }

        public new void AddListener(UnityAction<T0, T1> call)
        {
            AddListener(call, false);
        }
        private void AddListener(UnityAction<T0, T1> call, bool isInvokeRemove)
        {
            if (call == null)
                return;
            if (_callbackDic == null)
                _callbackDic = new Dictionary<UnityAction<T0, T1>, bool>();
            if (_callbackDic.ContainsKey(call) == false)
            {
                _callbackDic.Add(call, isInvokeRemove);
                base.AddListener(call);
            }
            else
            {
                _callbackDic[call] = isInvokeRemove;
            }
        }


        public new void Invoke(T0 arg0, T1 arg1)
        {
            UnityEventBase tempEvent = UEvent.currentEvent;
            UEvent.currentEvent = this;


            if (_callbackDic != null)
            {
                if (_tempList == null)
                    _tempList = new List<UnityAction<T0, T1>>();
                _tempList.Clear();

                foreach (KeyValuePair<UnityAction<T0, T1>, bool> keyvalue in _callbackDic)
                {
                    if (keyvalue.Value)
                        _tempList.Add(keyvalue.Key);
                }
                for (int i = 0; i < _tempList.Count; i++)
                {
                    UnityAction<T0, T1> call = _tempList[i];
                    _callbackDic.Remove(call);
                }
            }

            base.Invoke(arg0,arg1);
            if (_callbackDic != null)
            {
                for (int i = 0; i < _tempList.Count; i++)
                {
                    UnityAction<T0, T1> call = _tempList[i];
                    if (_callbackDic.ContainsKey(call) == false)
                    {
                        base.RemoveListener(call);
                    }
                }
                _tempList.Clear();
            }
            UEvent.currentEvent = tempEvent;
        }

        public new void RemoveListener(UnityAction<T0, T1> call)
        {
            if (call == null)
                return;
            if (_callbackDic!=null&&_callbackDic.ContainsKey(call))
            {
                base.RemoveListener(call);
                _callbackDic.Remove(call);
            }
        }
        public new void RemoveAllListeners()
        {
            base.RemoveAllListeners();
            if(_callbackDic != null)
                _callbackDic.Clear();
        }
        public bool HasListener(UnityAction<T0, T1> call)
        {
            if (_callbackDic != null && _callbackDic.ContainsKey(call))
            {
                return true;
            }
            return false;
        }
        public bool HasListener()
        {
            if (_callbackDic != null && _callbackDic.Count >= 0)
                return true;
            else if (GetPersistentEventCount() > 0)
                return true;
            return false;
        }
        public static UEvent<T0,T1> operator +(UEvent<T0, T1> lhs, UnityAction<T0, T1> rhs)
        {
            lhs.AddListener(rhs);
            return lhs;
        }
        public static UEvent<T0, T1> operator -(UEvent<T0, T1> lhs, UnityAction<T0, T1> rhs)
        {
            lhs.RemoveListener(rhs);
            return lhs;
        }
    }

    [Serializable]
    public class UEvent<T0, T1,T2> : UnityEvent<T0, T1, T2>
    {
        private Dictionary<UnityAction<T0, T1, T2>, bool> _callbackDic ;
        private List<UnityAction<T0, T1, T2>> _tempList;



        public void AddListenerOnce(UnityAction<T0, T1, T2> call)
        {
            AddListener(call, true);
        }

        public new void AddListener(UnityAction<T0, T1, T2> call)
        {
            AddListener(call, false);
        }
        private void AddListener(UnityAction<T0, T1, T2> call, bool isInvokeRemove)
        {
            if (call == null)
                return;
            if (_callbackDic == null)
                _callbackDic = new Dictionary<UnityAction<T0, T1, T2>, bool>();
            if (_callbackDic.ContainsKey(call) == false)
            {
                _callbackDic.Add(call, isInvokeRemove);
                base.AddListener(call);
            }
            else
            {
                _callbackDic[call] = isInvokeRemove;
            }
        }


        public new void Invoke(T0 arg0, T1 arg1, T2 arg2)
        {
            UnityEventBase tempEvent = UEvent.currentEvent;
            UEvent.currentEvent = this;

            if (_callbackDic != null)
            {
                if(_tempList==null)
                    _tempList = new List<UnityAction<T0, T1, T2>>();
                _tempList.Clear();
                foreach (KeyValuePair<UnityAction<T0, T1, T2>, bool> keyvalue in _callbackDic)
                {
                    if (keyvalue.Value)
                        _tempList.Add(keyvalue.Key);
                }
                for (int i = 0; i < _tempList.Count; i++)
                {
                    UnityAction<T0, T1, T2> call = _tempList[i];
                    _callbackDic.Remove(call);
                }
            }

            base.Invoke(arg0, arg1,arg2);
            if (_callbackDic != null)
            {
                for (int i = 0; i < _tempList.Count; i++)
                {
                    UnityAction<T0, T1, T2> call = _tempList[i];
                    if (_callbackDic.ContainsKey(call) == false)
                    {
                        base.RemoveListener(call);
                    }
                }
                _tempList.Clear();
            }
            UEvent.currentEvent = tempEvent;
        }

        public new void RemoveListener(UnityAction<T0, T1, T2> call)
        {
            if (call == null)
                return;
            if (_callbackDic!=null&&_callbackDic.ContainsKey(call))
            {
                base.RemoveListener(call);
                _callbackDic.Remove(call);
            }
        }
        public new void RemoveAllListeners()
        {
            base.RemoveAllListeners();
            if(_callbackDic!=null)
                _callbackDic.Clear();
        }
        public bool HasListener(UnityAction<T0, T1, T2> call)
        {
            if (_callbackDic!=null&&_callbackDic.ContainsKey(call))
            {
                return true;
            }
            return false;
        }
        public bool HasListener()
        {
            if (_callbackDic!=null&&_callbackDic.Count >= 0)
                return true;
            else if (GetPersistentEventCount() > 0)
                return true;
            return false;
        }
        public static UEvent<T0, T1,T2> operator +(UEvent<T0, T1, T2> lhs, UnityAction<T0, T1, T2> rhs)
        {
            lhs.AddListener(rhs);
            return lhs;
        }
        public static UEvent<T0, T1, T2> operator -(UEvent<T0, T1, T2> lhs, UnityAction<T0, T1, T2> rhs)
        {
            lhs.RemoveListener(rhs);
            return lhs;
        }
    }


    [Serializable]
    public class UEvent<T0, T1, T2, T3> : UnityEvent<T0, T1, T2, T3>
    {
        private Dictionary<UnityAction<T0, T1, T2, T3>, bool> _callbackDic ;

        private List<UnityAction<T0, T1, T2, T3>> _tempList;

        public UEvent()
        {
           
        }


        public void AddListenerOnce(UnityAction<T0, T1, T2, T3> call)
        {
            AddListener(call, true);
        }

        public new void AddListener(UnityAction<T0, T1, T2, T3> call)
        {
            AddListener(call, false);
        }
        private void AddListener(UnityAction<T0, T1, T2, T3> call, bool isInvokeRemove)
        {
            if (call == null)
                return;
            if (_callbackDic == null)
                _callbackDic = new Dictionary<UnityAction<T0, T1, T2, T3>, bool>();
            if (_callbackDic.ContainsKey(call) == false)
            {
                _callbackDic.Add(call, isInvokeRemove);
                base.AddListener(call);
            }
            else
            {
                _callbackDic[call] = isInvokeRemove;
            }
        }


        public new void Invoke(T0 arg0, T1 arg1, T2 arg2, T3 arg3)
        {
            UnityEventBase tempEvent = UEvent.currentEvent;
            UEvent.currentEvent = this;

            if (_callbackDic != null)
            {
                if(_tempList==null)
                    _tempList = new List<UnityAction<T0, T1, T2, T3>>();
                _tempList.Clear();
                foreach (KeyValuePair<UnityAction<T0, T1, T2, T3>, bool> keyvalue in _callbackDic)
                {
                    if (keyvalue.Value)
                        _tempList.Add(keyvalue.Key);
                }
                for (int i = 0; i < _tempList.Count; i++)
                {
                    UnityAction<T0, T1, T2, T3> call = _tempList[i];
                    _callbackDic.Remove(call);
                }
            }
            base.Invoke(arg0, arg1, arg2,arg3);
            if (_callbackDic != null)
            {
                for (int i = 0; i < _tempList.Count; i++)
                {
                    UnityAction<T0, T1, T2, T3> call = _tempList[i];
                    if (_callbackDic.ContainsKey(call) == false)
                    {
                        base.RemoveListener(call);
                    }
                }
                _tempList.Clear();
            }
            UEvent.currentEvent = tempEvent;
        }

        public new void RemoveListener(UnityAction<T0, T1, T2, T3> call)
        {
            if (call == null)
                return;
            if (_callbackDic != null&&_callbackDic.ContainsKey(call))
            {
                base.RemoveListener(call);
                _callbackDic.Remove(call);
            }
        }
        public new void RemoveAllListeners()
        {
            base.RemoveAllListeners();
            if(_callbackDic != null)
                _callbackDic.Clear();
        }
        public bool HasListener(UnityAction<T0, T1, T2, T3> call)
        {
            if (_callbackDic.ContainsKey(call))
            {
                return true;
            }
            return false;
        }
        public bool HasListener()
        {
            if (_callbackDic != null&&_callbackDic.Count >= 0)
                return true;
            else if (GetPersistentEventCount() > 0)
                return true;
            return false;
        }
        public static UEvent<T0, T1, T2,T3> operator +(UEvent<T0, T1, T2, T3> lhs, UnityAction<T0, T1, T2, T3> rhs)
        {
            lhs.AddListener(rhs);
            return lhs;
        }
        public static UEvent<T0, T1, T2, T3> operator -(UEvent<T0, T1, T2, T3> lhs, UnityAction<T0, T1, T2, T3> rhs)
        {
            lhs.RemoveListener(rhs);
            return lhs;
        }
    }
}
