using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MemoryUsage : InstMonoBehaviour<MemoryUsage>
{
    private const float MB = 1024 * 1024;

    private float gcTotalMemory, saveMemory, endMemory;
    private long gcTotalMemoryByte;
    
    private Rect windowRect = new Rect(100, 100, 500, 300);
    private GUIStyle style = new GUIStyle();

    private Dictionary<string, ValueTuple<float, float>> dic = new Dictionary<string, ValueTuple<float, float>>();
    private const string defaultKey = "default";
    private string key = defaultKey;
    private bool show = true;

    public void Show(bool _show = true)
    {
        show = _show;
    }

    public long GetMemoryUsageByte()
    {
        gcTotalMemoryByte = System.GC.GetTotalMemory(false);
        return gcTotalMemoryByte;
    }
    
    public float GetMemoryUsageMB()
    {
        gcTotalMemory = ((float)GetMemoryUsageByte()) / MB;
        return gcTotalMemory;
    }

    public float Save(string _key = defaultKey)
    {
        if (dic.ContainsKey(key))
        {
            dic.Remove(key);
        }
        if (dic.ContainsKey(_key))
        {
            Debug.LogWarning("当前追踪已存在");
        }
        dic[_key] = new ValueTuple<float, float>(GetMemoryUsageMB(), -1);
        return gcTotalMemory;
    }
    
    public float End(string _key = defaultKey)
    {
        if (!dic.ContainsKey(_key))
        {
            Debug.LogWarning($"当前{_key}追踪不存在");
            return 0;
        }

        var valueTuple = dic[_key];
        valueTuple.Item2 = GetMemoryUsageMB();
        dic[_key] = valueTuple;
        key = _key;
        return valueTuple.Item2 - valueTuple.Item1;
    }
    
    void OnGUI()
    {
        if (!show)
        {
            return;
        }
        windowRect = GUI.Window(0, windowRect, DoWindow, "内存情况");
    }
    
    void DoWindow(int windowID)
    {
        style = new GUIStyle();
        style.fontSize = 30;
        style.normal.textColor = Color.green;
        float y = 30, step = 30;
        GUI.Label(new Rect(10,y,500,200),$"当前内存{GetMemoryUsageMB()}MB", style);
        if (dic.ContainsKey(key))
        {
            GUI.Label(new Rect(10,y +=step,500,200),$"当前存档点{key}", style);
            if (dic[key].Item1 > -1)
            {
                GUI.Label(new Rect(10,y +=step,500,200),$"存档点内存：{dic[key].Item1}MB", style);
            }
            if (dic[key].Item2 > -1)
            {
                GUI.Label(new Rect(10,y +=step,500,200),$"结束内存：{dic[key].Item2}MB", style);
                GUI.Label(new Rect(10,y +=step,500,200),$"内存相差：{dic[key].Item2 - dic[key].Item1}MB", style);
            }
        }

        if (GUI.Button(new Rect(10,y +=step,200,50),"关闭",style))
        {
            Show(false);
        }
    }
}
