using System.Collections.Generic;
using System.Linq;

public class GameInstanceManager
{
    private static Dictionary<string, BaseInstance> allInstances = new Dictionary<string, BaseInstance>();
    public static T CreateInstance<T>(string typeName) where T : BaseInstance, new()
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
            foreach (var ins in allInstances.Values.ToList())
            {
                ins?.Release();
            }
        }
        allInstances.Clear();
    }
    public static Dictionary<string, BaseInstance> GetAllInstances()
    {
        return allInstances;
    }
}