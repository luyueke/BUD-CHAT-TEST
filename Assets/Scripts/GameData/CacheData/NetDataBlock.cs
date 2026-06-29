using System;
using UnityEngine.Events;

public class NetDataBlock
{
    public string content;
}

public struct NetCacheKey
{
    public string httpUrl;
    public string filterStr;
}

public class NetCacheEvent
{
    public NetCacheEvent(UnityAction<string> act)
    {
        OnChange = act;
    }
    public UnityAction<string> OnChange;
}
