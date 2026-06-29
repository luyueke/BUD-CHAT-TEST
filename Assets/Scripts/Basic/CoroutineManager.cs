using System;
using System.Collections;
using UnityEngine;

public class CoroutineManager : GlobalInstance<CoroutineManager>
{
    private CoroutineExecuter executer;

    public CoroutineManager()
    {
        this.executer = new GameObject("CoroutineManager").AddComponent<CoroutineExecuter>();
        this.executer.gameObject.DontDestroy();
    }

    public Coroutine StartCoroutine(IEnumerator routine)
    {
        if (executer == null)
        {
            return null;
        }
        return this.executer.StartCoroutine(routine);
    }

    public void StopCoroutine(IEnumerator routine)
    {
        if (executer == null)
        {
            return;
        }
        this.executer.StopCoroutine(routine);
    }

    public void StopCoroutine(Coroutine routine)
    {
        if (executer == null)
        {
            return;
        }
        this.executer.StopCoroutine(routine);
    }

    public Coroutine CallBack<T>(T instruction, Action<T> callback) where T : YieldInstruction
    {
        return StartCoroutine(CoroutineCallBack(instruction as YieldInstruction, () =>
        {
            callback?.Invoke(instruction);
        }));
    }

    public Coroutine CallBack(IEnumerator enumerator, Action<IEnumerator> callBack)
    {
        return StartCoroutine(CoroutineCallBack(enumerator, () =>
        {
            callBack?.Invoke(enumerator);
        }));
    }

    private IEnumerator CoroutineCallBack(YieldInstruction enumerator, Action callBack)
    {
        yield return enumerator;
        callBack?.Invoke();
    }
    
    private IEnumerator CoroutineCallBack(IEnumerator enumerator, Action callBack)
    {
        yield return enumerator;
        callBack?.Invoke();
    }
}