using Message;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 挂在 UI 遮罩/可点区域上：
/// - 点击时广播 MessageName.UICameraModeGlobalClose 用于收起相机菜单
/// - 不再转发/劫持拖拽事件，避免干扰底层镜头输入
/// </summary>
public class UICameraModeGlobalClosePassthrough : MonoBehaviour, IPointerDownHandler, IPointerClickHandler
{
    [Header("Broadcast")]
    [SerializeField] private bool broadcastOnPointerDown = false;
    [SerializeField] private bool broadcastOnPointerClick = true;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (broadcastOnPointerDown)
        {
            MessageHelper.Broadcast(MessageName.UICameraModeGlobalClose);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (broadcastOnPointerClick)
        {
            MessageHelper.Broadcast(MessageName.UICameraModeGlobalClose);
        }
    }
}

