/**
 * @ Author: Jun Zhou
 * @ Create Time: 2023-07-18 10:52:44
 * @ Modified by: Jun Zhou
 * @ Modified time: 2023-07-18 11:02:24
 * @ Description: 全局的协程工具类
 */

using System;
using System.Collections;
using UnityEngine;

public class GlobalCoroutineUtils : GlobalInstance<GlobalCoroutineUtils>
{
    private GlobalCoroutineExecuter executer;

    public GlobalCoroutineUtils()
    {
        this.executer = new GameObject("GlobalCoroutineUtils").AddComponent<GlobalCoroutineExecuter>();
        this.executer.gameObject.DontDestroy();
    }

    public Coroutine StartCoroutine(IEnumerator routine)
    {
        return this.executer.StartCoroutine(routine);
    }

    public void StopCoroutine(IEnumerator routine)
    {
        this.executer.StopCoroutine(routine);
    }

    public void StopCoroutine(Coroutine routine)
    {
        this.executer.StopCoroutine(routine);
    }
    
    public void WaitForEndOfFrame(Action callback)
    {
        StartCoroutine(CoroutineCallBack(new WaitForEndOfFrame(), (_) =>
        {
            callback?.Invoke();
        }));
    }
    
    public void WaitForFrame(Action callback)
    {
        StartCoroutine(WaitFrame(callback));
    }
   

    public Coroutine CallBack<T>(T instruction, Action<T> callback) where T : YieldInstruction
    {
        return StartCoroutine(CoroutineCallBack(instruction, callback));
    }

    private IEnumerator CoroutineCallBack<T>(T enumerator, Action<T> callBack) where T : YieldInstruction
    {
        yield return enumerator;
        callBack?.Invoke(enumerator);
    }

    private IEnumerator WaitFrame(Action callBack)
    {
        yield return null;
        callBack?.Invoke();
    }
}