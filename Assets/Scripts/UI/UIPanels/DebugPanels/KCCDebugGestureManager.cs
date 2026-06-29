using UnityEngine;
using System.Collections;

/// <summary>
/// KCC调试手势管理器
/// 负责初始化画圈手势检测器
/// </summary>
public class KCCDebugGestureManager : MonoBehaviour
{
    private static KCCDebugGestureManager _instance;
    public static KCCDebugGestureManager Inst
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("KCCDebugGestureManager");
                _instance = go.AddComponent<KCCDebugGestureManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private CircleGestureDetector gestureDetector;
    private bool isInitialized = false;
    private Coroutine initCoroutine;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    public void Init()
    {
        if (isInitialized)
            return;

        // 在UIRoot上添加手势检测器
        if (UIManager.Inst != null && UIManager.Inst.UIRoot != null)
        {
            GameObject detectorObj = new GameObject("CircleGestureDetector");
            detectorObj.transform.SetParent(UIManager.Inst.UIRoot, false);
            gestureDetector = detectorObj.AddComponent<CircleGestureDetector>();
            isInitialized = true;
            LoggerUtils.Log("KCCDebugGestureManager: Circle gesture detector initialized");
        }
        else
        {
            LoggerUtils.LogError("KCCDebugGestureManager: UIManager or UIRoot not available, will retry later");
            // 延迟初始化
            if (initCoroutine == null)
            {
                initCoroutine = StartCoroutine(DelayedInit());
            }
        }
    }

    private IEnumerator DelayedInit()
    {
        yield return new WaitForSeconds(0.5f);
        Init();
        initCoroutine = null;
    }

    private void OnDestroy()
    {
        if (initCoroutine != null)
        {
            StopCoroutine(initCoroutine);
            initCoroutine = null;
        }
        if (gestureDetector != null)
        {
            Destroy(gestureDetector.gameObject);
            gestureDetector = null;
        }
        isInitialized = false;
        if (_instance == this)
        {
            _instance = null;
        }
    }
}

