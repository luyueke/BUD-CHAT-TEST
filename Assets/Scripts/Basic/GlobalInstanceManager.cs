using System.Collections.Generic;
using System.Linq;
using System;
public class GlobalInstanceManager
{
    private static Dictionary<string, BaseGlobalInstance> allInstances = new Dictionary<string, BaseGlobalInstance>();

    public static Dictionary<string, BaseGlobalInstance> GetAllInstances()
    {
        return allInstances;
    }
    private static List<string> ignoreTypes = new List<string>()
    {
        "Game.Audio.AkSoundManager"
    };

    public static T CreateInstance<T>(string typeName) where T : BaseGlobalInstance, new()
    {
        if (!allInstances.ContainsKey(typeName) || allInstances[typeName] == null)
        {
            var instance = new T();
            allInstances.Add(typeName, instance);
        }
        return allInstances[typeName] as T;
    }

    public static void Release()
    {
        if (allInstances.Count > 0)
        {
            foreach (var ins in allInstances.Values)
            {
                ins?.Release();
            }
        }
        allInstances.Clear();
    }

    //ignoreTypes中注册的不进行清理
    public static void ReleaseWithIgnore()
    {
        if (allInstances.Count > 0)
        {
            var allKeys = allInstances.Keys.ToList();
            for (int i = allInstances.Keys.Count - 1; i >= 0; i--)
            {
                var key = allKeys[i];
                if (ignoreTypes.Contains(key))
                {
                    continue;
                }
                // if (allInstances.TryGetValue(key, out var ins) && !ignoreTypes.Contains(ins.GetType()))
                if (allInstances.TryGetValue(key, out var ins))
                {
                    ins?.Release();
                    allInstances.Remove(key);
                }
            }
        }
    }
}