using Cinemachine;
using UnityEngine;

public struct CinemachineTouchParam 
{
    public Transform target;

    public Vector3 followOff;
    public float fov;

    public float rotateSpeed;
    public float zoomSpeed;

    public float mouseRotateSpeed;
    public float mouseZoomSpeed;

    public float minZoom;
    public float maxZoom;
}

/// <summary>
/// 使用标准的CinemachineVirtualCamera实现触摸和鼠标控制。
/// - 通过旋转父物体来实现镜头旋转。
/// - 通过修改Framing Transposer的CameraDistance来实现缩放。
/// </summary>
public class VirtualCameraTouchController : MonoBehaviour
{
    [Header("相机与控制目标")]
    [Tooltip("作为旋转支点的父物体")]
    public Transform cameraRig; // 我们手动创建的那个CameraRig
    [Tooltip("你的Cinemachine Virtual Camera")]
    public CinemachineVirtualCamera virtualCamera;

    [Header("灵敏度设置 - 触摸")]
    public float touchRotateSpeed = 1f;
    public float touchZoomSpeed = 0.1f;

    [Header("灵敏度设置 - 鼠标")]
    public float mouseRotateSpeed = 2f;
    public float mouseZoomSpeed = 1f;

    [Header("旋转与缩放限制")]
    [Tooltip("垂直旋转角度的最小值")]
    public float minPitch = -30f;
    [Tooltip("垂直旋转角度的最大值")]
    public float maxPitch = 80f;
    [Tooltip("相机距离目标的最小值")]
    public float minZoomDistance = 2f;
    [Tooltip("相机距离目标的最大值")]
    public float maxZoomDistance = 20f;

    private CinemachineFramingTransposer framingTransposer;
    private float currentYRotation; // 我们现在只需要存储Y轴的旋转
    public void SetData(CinemachineTouchParam param)
    {
        cameraRig = param.target;

        //touchRotateSpeed = param.rotateSpeed;
        //touchZoomSpeed = param.zoomSpeed;
        //
        //mouseRotateSpeed = param.mouseRotateSpeed;
        //mouseZoomSpeed = param.mouseZoomSpeed;
        //
        //minZoomDistance = param.minZoom;
        //maxZoomDistance = param.maxZoom;

        // 初始化Y轴旋转角度为当前CameraRig的角度
        if (cameraRig != null)
        {
            currentYRotation = cameraRig.eulerAngles.y;
        }
        framingTransposer = virtualCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
    }

    void Awake()
    {
        virtualCamera = transform.GetComponent<CinemachineVirtualCamera>();
    }

    void Update()
    {
        if (virtualCamera == null || cameraRig == null || framingTransposer == null)
        {
            return;
        }
        // 根据平台选择输入方式
        if (Input.touchCount > 0)
        {
            HandleTouchInput();
        }
        else
        {
            HandleMouseInput();
        }
    }
    private void HandleTouchInput()
    {
        // 单指旋转
        if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Moved)
        {
            Vector2 delta = Input.GetTouch(0).deltaPosition;
            // *** 修改点: 只使用X轴的输入来旋转 ***
            Rotate(delta.x * touchRotateSpeed * 0.1f);
        }
        // 双指缩放
        else if (Input.touchCount == 2)
        {
            Touch touchZero = Input.GetTouch(0);
            Touch touchOne = Input.GetTouch(1);

            Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
            Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

            float prevMagnitude = (touchZeroPrevPos - touchOnePrevPos).magnitude;
            float currentMagnitude = (touchZero.position - touchOne.position).magnitude;

            float difference = currentMagnitude - prevMagnitude;
            Zoom(difference * touchZoomSpeed);
        }
    }

    private void HandleMouseInput()
    {
        // 鼠标左键拖动旋转
        if (Input.GetMouseButton(0))
        {
            float deltaX = Input.GetAxis("Mouse X") * mouseRotateSpeed;
            // *** 修改点: 只使用X轴的输入来旋转 ***
            Rotate(deltaX);
        }

        // 鼠标滚轮缩放
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            Zoom(scroll * mouseZoomSpeed);
        }
    }

    // *** 修改点: Rotate函数现在只接受一个参数 deltaX ***
    private void Rotate(float deltaX)
    {
        // 更新Y轴旋转角度
        currentYRotation += deltaX;

        // 获取当前的垂直角度，保持不变
        float currentXRotation = cameraRig.rotation.eulerAngles.x;

        // 应用旋转，只改变Y轴
        cameraRig.rotation = Quaternion.Euler(currentXRotation, currentYRotation, 0);
    }

    private void Zoom(float zoomAmount)
    {
        framingTransposer.m_CameraDistance = Mathf.Clamp(
            framingTransposer.m_CameraDistance - zoomAmount,
            minZoomDistance,
            maxZoomDistance
        );
    }
}
