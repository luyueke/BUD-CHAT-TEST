using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//默认优先执行生命函数
[DefaultExecutionOrder(-110)]
public class GameMonoManager : InstMonoBehaviour<GameMonoManager>
{
    private List<IGameMono> tempList = new List<IGameMono>();

    private void Awake()
    {
        if (!DontDestroyUtils.IsContains(this.gameObject))
        {
            this.gameObject.DontDestroy();
        }
    }

    public void Init()
    {

    }

    private void UpdateGameMono(IGameMono iGamemono)
    {
        if (iGamemono == null) return;
        try
        {
            iGamemono.Update();
        }
        catch (Exception e)
        {
            LoggerUtils.LogError(e.ToString());
        }
    }

    private void FixUpdateGameMono(IGameMono iGamemono)
    {
        if (iGamemono == null) return;
        try
        {
            iGamemono.FixedUpdate();
        }
        catch (Exception e)
        {
            LoggerUtils.LogError(e.ToString());
        }
    }

    private void Update()
    {
        tempList.Clear();
        var cInstancs = GameInstanceManager.GetAllInstances();
        if (cInstancs != null && cInstancs.Count > 0)
        {
            foreach (var item in cInstancs.Values)
            {
                if (item is IGameMono)
                {
                    tempList.Add((IGameMono)item);
                }
            }
        }
        var gInstancs = GlobalInstanceManager.GetAllInstances();
        if (gInstancs != null && gInstancs.Count > 0)
        {
            foreach (var item in gInstancs.Values)
            {
                if (item is IGameMono)
                {
                    tempList.Add((IGameMono)item);
                }
            }
        }



        for (int i = 0; i < tempList.Count; i++)
        {
            UpdateGameMono(tempList[i]);
        }
    }
    private void FixedUpdate()
    {
        tempList.Clear();
        var cInstancs = GameInstanceManager.GetAllInstances();
        if (cInstancs != null && cInstancs.Count > 0)
        {
            foreach (var item in cInstancs.Values)
            {
                if (item is IGameMono)
                {
                    tempList.Add((IGameMono)item);
                }
            }
        }

        var gInstancs = GlobalInstanceManager.GetAllInstances();
        if (gInstancs != null && gInstancs.Count > 0)
        {
            foreach (var item in gInstancs.Values)
            {
                if (item is IGameMono)
                {
                    tempList.Add((IGameMono)item);
                }
            }
        }

        //var gInstancs = GlobalInstanceManager.GetAllInstances().Values.ToList();
        //if (gInstancs != null && gInstancs.Count > 0)
        //{
        //    foreach (var item in gInstancs)
        //    {
        //        if (item is IGameMono)
        //        {
        //            tempList.Add((IGameMono)item);
        //        }
        //    }
        //}

        for (int i = 0; i < tempList.Count; i++)
        {
            FixUpdateGameMono(tempList[i]);
        }
    }

    
    private void OnDestroy()
    {
        GameInstanceManager.Release();
        GlobalInstanceManager.ReleaseWithIgnore();
    }

}
