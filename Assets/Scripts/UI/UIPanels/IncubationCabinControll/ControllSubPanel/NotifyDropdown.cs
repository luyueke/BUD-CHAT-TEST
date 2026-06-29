using System;
using UnityEngine;

/// <summary>
/// 挂载到 Dropdown GameObject 上，利用 OnTransformChildrenChanged 检测
/// "Dropdown List" 子节点的创建与销毁，暴露展开/收起事件
/// </summary>
public class DropdownOpenCloseNotifier : MonoBehaviour
{
    public event Action onShown;   // Dropdown 展开时触发，由外部订阅
    public event Action onHidden;  // Dropdown 收起时触发，由外部订阅

    // 当子节点发生增减时，检测"Dropdown List"子节点是否存在，以判断展开/收起状态
    private void OnTransformChildrenChanged()
    {
        if (transform.Find("Dropdown List") != null)
            onShown?.Invoke();
        else
            onHidden?.Invoke();
    }
}
