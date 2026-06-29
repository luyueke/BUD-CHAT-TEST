/// <summary>
/// 只供游玩场景内使用
/// 如全局配置等管理类，使用GlobalInstance
/// </summary>
/// <typeparam name="T"></typeparam>
public class GameInstance<T> : BaseInstance where T : BaseInstance, new()
{
    private static T _instance;
    private static object lockObj = new object();
    public static T Inst
    {
        get
        {
            _instance = GameInstanceManager.CreateInstance<T>(typeof(T).FullName);
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

public class BaseInstance
{
    public virtual void Release() { }
}