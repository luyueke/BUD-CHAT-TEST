using System;
using System.Collections.Generic;
using UnityEngine;

public static class DontDestroyUtils
{
    private static List<GameObject> dontDestroys = new List<GameObject>();

    public static void DontDestroy(this GameObject obj)
    {
        if (!dontDestroys.Contains(obj))
        {
            dontDestroys.Add(obj);
        }

        if (Application.isPlaying)
        {
            GameObject.DontDestroyOnLoad(obj);
        }
    }


    public static void Dispose()
    {
        foreach (var obj in dontDestroys)
        {
            if (obj != null)
            {
                GameObject.Destroy(obj);
            }
        }

        dontDestroys.Clear();
    }

    public static bool IsContains(GameObject go)
    {
        return dontDestroys?.Contains(go) ?? false;
    }
}