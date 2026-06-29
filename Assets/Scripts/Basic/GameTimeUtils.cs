using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameTimeUtils : InstMonoBehaviour<GameTimeUtils>
{
    private float mTotalTime;
    private Dictionary<string, float> startTimes;

    public void Init()
    {

    }
    private void Awake()
    {
        if (!DontDestroyUtils.IsContains(this.gameObject))
        {
            this.gameObject.DontDestroy();
        }
        
        mTotalTime = 0f;
        startTimes = new Dictionary<string, float>();
    }

    private float lastTime = 0;
    void Update()
    {
        if (Application.isFocused)
        {
            mTotalTime += Time.deltaTime;
        }
        
        
        if (mTotalTime - lastTime > 3)
        {
            // LoggerUtils.Log("###当前统计时长："+mTotalTime);
            lastTime = mTotalTime;
        }
        
    }
    
    /// <summary>
    /// 开始收集时间
    /// </summary>
    /// <param name="pageName"></param>
    public void StartCollect(string pageName)
    {
        if (startTimes.ContainsKey(pageName))
        {
            startTimes[pageName] = mTotalTime;
        }
        else
        {
            startTimes.Add(pageName, mTotalTime);
        }
    }
    
    /// <summary>
    /// 停止收集时间，并返回总时长，单位为秒
    /// </summary>
    /// <param name="pageName"></param>
    /// <returns></returns>
    public int StopCollect(string pageName)
    {
        if (startTimes.ContainsKey(pageName))
        {
            float startTime = startTimes[pageName];
            int elapsedTime = Mathf.FloorToInt(mTotalTime - startTime);
            startTimes.Remove(pageName);
            return elapsedTime;
        }
        else
        {
            LoggerUtils.LogError("找不到该页面的开始时间："+pageName);
            return 0;
        }
    }
    
    /// <summary>
    /// 获取当前时长，该操作并不会停止计时器
    /// </summary>
    /// <param name="pageName"></param>
    /// <returns></returns>
    public int GetCollectTime(string pageName)
    {
        if (startTimes.ContainsKey(pageName))
        {
            float startTime = startTimes[pageName];
            return Mathf.FloorToInt(mTotalTime - startTime);
        }
        else
        {
            LoggerUtils.LogError("找不到该页面的开始时间："+pageName);
            return 0;
        }
    }
    
    public int RestartCollect(string pageName)
    {
        int restartTime = 0;
        
        if (startTimes.ContainsKey(pageName))
        {
            float startTime = startTimes[pageName];
            restartTime = Mathf.FloorToInt(mTotalTime - startTime);
            startTimes[pageName] = mTotalTime; // 重置时间
        }
        else
        {
            LoggerUtils.LogError("找不到该页面的开始时间："+pageName);
        }
        
        return restartTime;
    }

 
}
