using GameData;
using System;
using System.Collections.Generic;

public delegate void StateAction();
public delegate void StateAction<T1>(T1 arg1);
public delegate void StateAction<T1, T2>(T1 arg1, T2 arg2);
public delegate void StateAction<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3);

public class StateEventManager : GameInstance<StateEventManager>
{
    private Dictionary<string, Dictionary<StateEvent, Delegate>> playerStateEventDic;

    public StateEventManager()
    {
        playerStateEventDic = new Dictionary<string, Dictionary<StateEvent, Delegate>>();
    }

    private void RegisterPlayerEvent(string playerId)
    {
        playerStateEventDic.Add(playerId, new Dictionary<StateEvent, Delegate>());
    }

    public void UnRegisterPlayerEvent(string playerId)
    {
        if (playerStateEventDic.ContainsKey(playerId))
        {
            playerStateEventDic.Remove(playerId);
        }
    }

    public void RegisterStateEvent(string playerId, StateEvent key, StateAction actoin)
    {
        if (!playerStateEventDic.ContainsKey(playerId))
        {
            RegisterPlayerEvent(playerId);
        }

        CombineDelegate(playerId, key, actoin);
    }

    public void RegisterStateEvent<T1>(string playerId, StateEvent key, StateAction<T1> actoin)
    {
        if (!playerStateEventDic.ContainsKey(playerId))
        {
            RegisterPlayerEvent(playerId);
        }

        CombineDelegate(playerId, key, actoin);
    }

    public void RegisterStateEvent<T1, T2>(string playerId, StateEvent key, StateAction<T1, T2> actoin)
    {
        if (!playerStateEventDic.ContainsKey(playerId))
        {
            RegisterPlayerEvent(playerId);
        }

        CombineDelegate(playerId, key, actoin);
    }

    public void RegisterStateEvent<T1, T2, T3>(string playerId, StateEvent key, StateAction<T1, T2, T3> actoin)
    {
        if (!playerStateEventDic.ContainsKey(playerId))
        {
            RegisterPlayerEvent(playerId);
        }

        CombineDelegate(playerId, key, actoin);
    }

    public void UnRegisterStateEvent(string playerId, StateEvent key, StateAction action)
    {
        RemoveDelegate(playerId, key, action);
    }

    public void UnRegisterStateEvent<T1>(string playerId, StateEvent key, StateAction<T1> action)
    {
        RemoveDelegate(playerId, key, action);
    }

    public void UnRegisterStateEvent<T1, T2>(string playerId, StateEvent key, StateAction<T1, T2> action)
    {
        RemoveDelegate(playerId, key, action);
    }

    public void UnRegisterStateEvent<T1, T2, T3>(string playerId, StateEvent key, StateAction<T1, T2, T3> action)
    {
        RemoveDelegate(playerId, key, action);
    }

    public bool TriggerStateEvent(string playerId, StateEvent key)
    {
        if (string.IsNullOrEmpty(playerId)) return false;

        if (playerStateEventDic.TryGetValue(playerId, out Dictionary<StateEvent, Delegate> stateEventDic))
        {
            Delegate action;

            if (stateEventDic.TryGetValue(key, out action) && action != null)
            {
                StateAction stateAction = action as StateAction;
                if (stateAction != null)
                {
                    stateAction();

                    return true;
                }
            }
        }

        return false;
    }

    public bool TriggerStateEvent<T1>(string playerId, StateEvent key, T1 arg1)
    {
        if (string.IsNullOrEmpty(playerId)) return false;

        if (playerStateEventDic.TryGetValue(playerId, out Dictionary<StateEvent, Delegate> stateEventDic))
        {
            Delegate action;

            if (stateEventDic.TryGetValue(key, out action) && action != null)
            {
                StateAction<T1> stateAction = action as StateAction<T1>;
                if (stateAction != null)
                {
                    stateAction(arg1);

                    return true;
                }
            }
        }

        return false;
    }

    public bool TriggerStateEvent<T1, T2>(string playerId, StateEvent key, T1 arg1, T2 arg2)
    {
        if (string.IsNullOrEmpty(playerId)) return false;

        if (playerStateEventDic.TryGetValue(playerId, out Dictionary<StateEvent, Delegate> stateEventDic))
        {
            Delegate action;

            if (stateEventDic.TryGetValue(key, out action) && action != null)
            {
                StateAction<T1, T2> stateAction = action as StateAction<T1, T2>;

                if (stateAction != null)
                {
                    stateAction(arg1, arg2);

                    return true;
                }
            }
        }

        return false;
    }

    public bool TriggerStateEvent<T1, T2, T3>(string playerId, StateEvent key, T1 arg1, T2 arg2, T3 arg3)
    {
        if (string.IsNullOrEmpty(playerId)) return false;

        if (playerStateEventDic.TryGetValue(playerId, out Dictionary<StateEvent, Delegate> stateEventDic))
        {
            Delegate action;

            if (stateEventDic.TryGetValue(key, out action) && action != null)
            {
                StateAction<T1, T2, T3> stateAction = action as StateAction<T1, T2, T3>;

                if (stateAction != null)
                {
                    stateAction(arg1, arg2, arg3);

                    return true;
                }
            }
        }

        return false;
    }

    private void CombineDelegate(string playerId, StateEvent key, Delegate action)
    {
        if (string.IsNullOrEmpty(playerId)) return;

        if (playerStateEventDic.TryGetValue(playerId, out Dictionary<StateEvent, Delegate> stateEventDic))
        {
            if (PreListenerAdding(key, action, stateEventDic))
            {
                stateEventDic[key] = Delegate.Combine(stateEventDic[key], action);
            }
        }
    }

    private void RemoveDelegate(string playerId, StateEvent key, Delegate action)
    {
        if (string.IsNullOrEmpty(playerId)) return;

        if (playerStateEventDic.TryGetValue(playerId, out Dictionary<StateEvent, Delegate> stateEventDic))
        {
            if (stateEventDic.ContainsKey(key))
            {
                stateEventDic[key] = Delegate.Remove(stateEventDic[key], action);
            }
        }
    }

    private bool PreListenerAdding(StateEvent type, Delegate listenerForAdding, Dictionary<StateEvent, Delegate> stateEventDic)
    {
        if (null == listenerForAdding)
        {
            return false;
        }

        bool flag = true;
        if (!stateEventDic.ContainsKey(type))
        {
            stateEventDic.Add(type, null);
        }
        Delegate delegate2 = stateEventDic[type];
        if ((delegate2 != null) && (delegate2.GetType() != listenerForAdding.GetType()))
        {
            flag = false;
        }

        if (null != delegate2)
        {
            foreach (Delegate delegateCur in delegate2.GetInvocationList())
            {
                if (listenerForAdding == delegateCur)
                {
                    //已添加过，无需重复添加
                    return false;
                }
            }
        }

        return flag;
    }
}
