using UnityEngine;

public class BMonoBehaviour<T>:MonoBehaviour where T: MonoBehaviour
{
    public static T Inst;
    protected virtual void Awake()
    {
        Inst = this as T;
    }

    protected virtual void OnDestroy()
    {
        Inst = null;
    }
}