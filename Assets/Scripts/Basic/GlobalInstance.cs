/// <summary>
/// 单例：使用全局配置等管理类等
/// </summary>
/// <typeparam name="T"></typeparam>
public class GlobalInstance<T> : BaseGlobalInstance where T : BaseGlobalInstance, new()
{
    private static T _instance;
    //lockObj 保证多线程中只存在唯一实例
    private static readonly object lockObj = new object();
    public static T Inst
    {
        get
        {
            if(_instance == null)
            {
                lock(lockObj)
                {
                    if(_instance == null)
                    {
                        _instance = GlobalInstanceManager.CreateInstance<T>(typeof(T).FullName);
                        _instance.Initialize();
                        return _instance;
                    }
                    else
                    {
                        return _instance;
                    }
                }
            }
           
            return _instance;
        }
    }

    public override void Release()
    {
        base.Release();
        _instance = null;
    }

    public static bool HasInstance
    {
        get
        {
            return _instance != null;
        }
    }
}

public class BaseGlobalInstance
{
    public virtual void Initialize() { }
    public virtual void Release() { }
}