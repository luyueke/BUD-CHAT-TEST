using System;
using System.Collections.Generic;
using Cinemachine;
using GameData.Manager;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections;
using Basic.Utils;
using Game.Avatar;
using Game.Config;
using Game.KinematicCharacter;
using GameData;
using Message;
using UI.UIPanels;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CameraModeHandler : InputHandler
{
    private Transform camFollow;
    private CinemachineVirtualCamera VirtualCam;
    private CinemachineTransposer camTransposer;
    public MobileJoystick joyStick;
    float moveTouchId = -1;
    int rotTouchId = -1;
    private float maxWidth = 500;//地图边界
    private float maxHeight =  500;//地图边界
    private float rotateSpeed = 2;
    private float moveSpeed = 0.18f;
    private float maxCamDist = CameraModeConotroller.CAMERA_ZOOM_MAX_DISTANCE;//拉远最远距离
    private float zoomSpeed = 0.6f;//相机模式缩放速度
    private float selZoomSpeed = 3f;//自拍缩放速度
    private float minCamDist = CameraModeConotroller.CAMERA_ZOOM_MIN_DISTANCE;//拉近最近距离
    private float lastTouchSpan;
    private Vector2 lastCenter;
    private Camera eCamera;
    private CameraModeType cameraMode;

    public bool DisableMultiTouch { get; set; }
    /// <summary>
    /// Follow 模式：允许双指缩放但走 FOV 而非 Transposer offset，禁止双指平移。
    /// </summary>
    public bool PinchZoomFOVOnly { get; set; }

    private bool IsJoystickActive => joyStick != null && joyStick.pressed;
    private static readonly List<RaycastResult> _multiTouchRaycastResults = new List<RaycastResult>(16);

    public CameraModeType CurrentMode => cameraMode;
    
    public Action<Touch> OnSelectTarget;

    public Action<Touch> OnClickTarget;

    public Vector3 orginPos;//进入相机模式时的初始位置
    public Vector3 orginEuler;//进入相机模式时的初始角度
    public Vector3 orginForward;//进入相机模式时的初始朝向
    private Transform mCtrCamera;

    private const float MOVE_LIMIT = 1000;
    private float H_CAMERA_LIMIT = 75;//第一人称横向滑动限制（±75，总摆幅150°）
    private const float V_SELFIE_ROTATE_MIN = 0;//自拍模式纵向限制min
    private const float V_SELFIE_ROTATE_MAX = 30;//自拍模式纵向限制max
    private const float V_CAMERA_ROTATE_MIN = -60; //相机模式纵向限制min
    private const float V_CAMERA_ROTATE_MAX = 85;//相机模式纵向限制max
    private const float MOVE_THRESHOLD = 5;//移动启动阈值，超过则裁判的为开始移动
    private const float FIELD_OF_VIEW_MIN = CameraModeConotroller.CAMERA_ZOOM_FOV_MIN;
    private const float FILED_OF_VIEW_MAX = CameraModeConotroller.CAMERA_ZOOM_FOV_MAX;

    protected new enum MultiGesture
    {
        MoreFingers,
        TwoFingers,
        Span,
        Move
    }
    protected MultiGesture gesture;

    public void SetCamera(Camera cam, CinemachineVirtualCamera vCam)
    {
        eCamera = cam;
        RebindVirtualCamera(vCam);
        OnEnter();
    }

    public void RebindVirtualCamera(CinemachineVirtualCamera vCam)
    {
        VirtualCam = vCam;
        camFollow = vCam != null ? vCam.Follow : null;
        camTransposer = VirtualCam != null ? VirtualCam.GetCinemachineComponent<CinemachineTransposer>() : null;
        mCtrCamera = camFollow;
    }

    public void OnEnter()
    {
        var playVirCamera = CameraModeConotroller.Inst.GetNormalVirCamera();
        if (playVirCamera != null && playVirCamera.Follow != null && camFollow != null)
        {
            camFollow.SetPositionAndRotation(playVirCamera.Follow.position, playVirCamera.Follow.rotation);
        }

        // 从自拍模式切回时，VirtualCam 的 Body 可能是 HardLock，此时 Transposer 为空。
        // 这里先强制切回 Transposer，再重新抓一次 camTransposer，避免空引用。
        if (VirtualCam != null)
        {
            CameraModeConotroller.Inst.SetVirCameraBodyType(VirtualCam, CameraBodyType.Transposer);
            camTransposer = VirtualCam.GetCinemachineComponent<CinemachineTransposer>();
            if (camTransposer != null)
            {
                camTransposer.m_FollowOffset = new Vector3(0, 0, CameraModeConotroller.DEFAULT_FOLLOW_Z); //默认跟随距离
            }
            VirtualCam.m_Lens.FieldOfView = CameraModeConotroller.CAMERA_FIELD_OF_VIEW;
        }

        OnCameraMove();
    }


    public void OnCameraMove()
    {
        orginPos = eCamera.transform.position;
        orginEuler = eCamera.transform.localEulerAngles;
        orginForward = eCamera.transform.forward;
        camTransposer = VirtualCam.GetCinemachineComponent<CinemachineTransposer>();
        CoroutineManager.Inst.StartCoroutine(DelaySetCameraPos());

    }

    public void ResetJoyStick()
    {
        if(joyStick)
        {
            joyStick.OnResetJoystick();
            moveTouchId = -1;
        }
    }

    public IEnumerator DelaySetCameraPos()
    {
        yield return 0;
        orginPos = eCamera.transform.position;
        orginEuler = eCamera.transform.localEulerAngles;
        orginForward = eCamera.transform.forward;
    }

    public override void OnShortTouchEnd(Touch touch)
    {
        OnSelectTarget?.Invoke(touch);
    }

    public void SwitchCameraMode(CameraModeType mode)
    {
        cameraMode = mode;
    }

    private void Rotate(float angleVertical, float angleHorizontal)
    {
        //自拍模式则按第一人称方式旋转人物
        switch (cameraMode)
        {
            case CameraModeType.SelfieMode:
                //人物不能动的话，不允许旋转
                var player = AvatarController.Inst.SelfController;
                player.Motor.transform.Rotate(Vector3.up, angleHorizontal * 2, Space.World);
                var curRotation = player.Motor.transform.rotation;
                player.Motor.SetRotation(curRotation);
                // player.Motor.transform.Rotate(Vector3.up, angleHorizontal, Space.World);
                RotateVertical(angleVertical, V_SELFIE_ROTATE_MIN, V_SELFIE_ROTATE_MAX);
                break;
            case CameraModeType.FixedViewNode:
                RotateH(angleHorizontal);
                RotateVertical(angleVertical);
                break;
            case CameraModeType.FirstView:
                RotateFirstView(angleVertical, angleHorizontal);
                break;
            case CameraModeType.Lens:
                // Lens 模式下需要保留“拖拽屏幕转向”
                RotateH(angleHorizontal);
                RotateVertical(angleVertical);
                break;
        }
    }

    /// <summary>
    /// Lens 模式：用左摇杆平移相机节点（前后左右）
    /// </summary>
    public void MoveByJoystick(Vector2 axis, float speed)
    {
        if (mCtrCamera == null) return;
        if (axis.sqrMagnitude <= 0.0001f) return;

        // 以当前镜头朝向为参考，在 XZ 平面移动
        var forward = mCtrCamera.forward;
        var right = mCtrCamera.right;
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        var delta = (forward * axis.y + right * axis.x) * (speed * GameConsts.TimeScale * Time.deltaTime);
        var targetPos = mCtrCamera.position + delta;
        targetPos = LimitedCameraPosition(targetPos);
        mCtrCamera.position = targetPos;
    }

    /// <summary>
    /// Lens 模式：上下按钮调整高度
    /// </summary>
    public void MoveVertical(float deltaY, float minY, float maxY)
    {
        if (mCtrCamera == null) return;
        var p = mCtrCamera.position;
        p.y = Mathf.Clamp(p.y + deltaY, minY, maxY);
        mCtrCamera.position = p;
    }

    /// <summary>
    /// Lens 模式：左右按钮沿相机本地 right 轴平移
    /// </summary>
    public void MoveHorizontal(float deltaX)
    {
        if (mCtrCamera == null) return;
        var right = mCtrCamera.right;
        right.y = 0;
        right.Normalize();
        var p = mCtrCamera.position + right * deltaX;
        mCtrCamera.position = LimitedCameraPosition(p);
    }

    //横向旋转无限制
    private void RotateH(float angleHorizontal)
    {
        mCtrCamera.Rotate(Vector3.up, angleHorizontal, Space.World);
    }

    // 第一人称：水平旋转驱动玩家本体，镜头节点只做俯仰
    private void RotateFirstView(float angleVertical, float angleHorizontal)
    {
        var player = AvatarController.Inst != null ? AvatarController.Inst.SelfController : null;
        if (player != null && player.Motor != null && Mathf.Abs(angleHorizontal) > 0.0001f)
        {
            player.Motor.transform.Rotate(Vector3.up, angleHorizontal, Space.World);
            var curRotation = player.Motor.transform.rotation;
            player.Motor.SetRotation(curRotation);
        }

        // 第一人称保持上下俯仰能力
        RotateVertical(angleVertical, -90, 90);
    }

    //横向旋转及限制
    private void RotateHorizontal(float angleHorizontal)
    {
        float diffAngle = Vector3.Angle(mCtrCamera.forward, orginForward);
        Vector3 normal = Vector3.Cross(orginForward, mCtrCamera.forward);//计算法线向量
        diffAngle *= Mathf.Sign(Vector3.Dot(normal,Vector3.up));
        float targetAngle = diffAngle + angleHorizontal;
        targetAngle = Mathf.Clamp(targetAngle, -H_CAMERA_LIMIT, H_CAMERA_LIMIT);
        float angleToRotate = targetAngle - diffAngle;
        mCtrCamera.Rotate(Vector3.up, angleToRotate, Space.World);
    }

    //纵向旋转及限制
    private void RotateVertical(float angle,float rotMin = V_CAMERA_ROTATE_MIN, float rotMax = V_CAMERA_ROTATE_MAX)
    {
        Vector3 currentAngle = mCtrCamera.localRotation.eulerAngles;
        float curAngleX = currentAngle.x > 180f ? currentAngle.x - 360f : currentAngle.x;
        float targetAngle = curAngleX + angle;
        targetAngle = Mathf.Clamp(targetAngle, rotMin, rotMax);
        float angleToRotate = targetAngle - curAngleX;
        mCtrCamera.Rotate(mCtrCamera.right, angleToRotate, Space.World);
    }

    public override void OnMultipleTouchesBegin(Touch[] touches)
    {
        if (DisableMultiTouch) return;
        if (IsJoystickActive)
        {
            return;
        }
        if (!TryGetFirstTwoNonUiTouches(touches, out var touchA, out var touchB, out var validCount))
        {
            gesture = MultiGesture.MoreFingers;
            return;
        }

        if (validCount < 2)
        {
            gesture = MultiGesture.MoreFingers;
            return;
        }

        lastCenter = (touchA.position + touchB.position) * 0.5f;
        lastTouchSpan = Vector2.Distance(touchA.position, touchB.position) * 0.5f;
        gesture = MultiGesture.TwoFingers;
    }

    public override void OnMultipleTouchesStay(Touch[] touches)
    {
        if (DisableMultiTouch) return;
        if (IsJoystickActive)
        {
            return;
        }
        if (!TryGetFirstTwoNonUiTouches(touches, out var touchA, out var touchB, out var validCount) || validCount < 2)
        {
            gesture = MultiGesture.MoreFingers;
            return;
        }

        if (gesture == MultiGesture.TwoFingers)
        {
            float newTouchSpan = Vector2.Distance(touchA.position, touchB.position) * 0.5f;
            float spanDelta = Mathf.Abs(newTouchSpan - lastTouchSpan);
            Vector2 newCenter = (touchA.position + touchB.position) * 0.5f;
            float centerDelta = Vector2.Distance(newCenter, lastCenter);
            float angle = DeltaAngle(touchA, touchB);

            // 优先按“跨度变化”识别缩放，避免首帧夹角不稳定导致误判 Move。
            if (spanDelta > centerDelta * 0.5f + 0.01f)
            {
                gesture = MultiGesture.Span;
            }
            else
            {
                if (angle == 0f && centerDelta <= 0.01f && spanDelta <= 0.01f) return;
                gesture = angle < 90f ? MultiGesture.Move : MultiGesture.Span;
            }
        }
        rotTouchId = -1;

        if (gesture == MultiGesture.Span)
        {
            float newTouchSpan = GetTouchSpan(touches);
            OnPinch(newTouchSpan);
        }
        else if (gesture == MultiGesture.Move)
        {
            Vector2 newCenter = GetCenter(touches);
            OnMove(newCenter);
        }
    }

    //双指缩放
    void OnPinch(float newTouchSpan)
    {
        if (VirtualCam == null) return;
        float offset = 0;
        float currentOffset = 0;
        float newOffset = 0;
        switch (cameraMode)
        {
            case CameraModeType.SelfieMode:
                offset = (newTouchSpan - lastTouchSpan) * selZoomSpeed * GameConsts.TimeScale;
                currentOffset = VirtualCam.m_Lens.FieldOfView;
                newOffset = currentOffset - offset;
                newOffset = Mathf.Clamp(newOffset, FIELD_OF_VIEW_MIN, FILED_OF_VIEW_MAX);

                VirtualCam.m_Lens.FieldOfView = newOffset;
                lastTouchSpan = newTouchSpan;
                break;
            case CameraModeType.FixedViewNode:
                if (PinchZoomFOVOnly)
                {
                    // Follow 模式：双指缩放控制 FOV
                    offset = (newTouchSpan - lastTouchSpan) * selZoomSpeed * GameConsts.TimeScale;
                    currentOffset = VirtualCam.m_Lens.FieldOfView;
                    newOffset = currentOffset - offset;
                    newOffset = Mathf.Clamp(newOffset, FIELD_OF_VIEW_MIN, FILED_OF_VIEW_MAX);
                    VirtualCam.m_Lens.FieldOfView = newOffset;
                    lastTouchSpan = newTouchSpan;
                    MessageHelper.Broadcast(MessageName.UICameraModeFovChanged, newOffset);
                }
                else
                {
                    // OC 模式：双指缩放控制 Transposer offset
                    if (camTransposer == null) return;
                    offset = (newTouchSpan - lastTouchSpan) * ZoomSpeed * GameConsts.TimeScale;
                    currentOffset = camTransposer.m_FollowOffset.z;
                    newOffset = currentOffset + offset;
                    newOffset = Mathf.Clamp(newOffset, -maxCamDist, -minCamDist);
                    camTransposer.m_FollowOffset = new Vector3(0, 0, newOffset);
                    lastTouchSpan = newTouchSpan;
                }
                break;
            case CameraModeType.FirstView:
                // 第一人称不允许平移，但允许通过 FOV “拉近/拉远”
                offset = (newTouchSpan - lastTouchSpan) * selZoomSpeed * GameConsts.TimeScale;
                currentOffset = VirtualCam.m_Lens.FieldOfView;
                newOffset = currentOffset - offset;
                newOffset = Mathf.Clamp(newOffset, FIELD_OF_VIEW_MIN, FILED_OF_VIEW_MAX);
                VirtualCam.m_Lens.FieldOfView = newOffset;
                lastTouchSpan = newTouchSpan;
                break;
        }
    }

    public float ZoomSpeed
    {
        get { return zoomSpeed; }
        set
        {
            zoomSpeed = value;
        }
    }
    //双指平移
    void OnMove(Vector2 newCenter)
    {
        if(cameraMode == CameraModeType.SelfieMode || cameraMode == CameraModeType.FirstView || cameraMode == CameraModeType.Lens || PinchZoomFOVOnly)
        {
            //自拍/第一人称/Lens/Follow 模式不能双指平移
            return;
        }
        Vector3 camForward = mCtrCamera.forward;
        Vector3 camRight = mCtrCamera.right;
        camForward.y = 0;
        camRight.y = 0;
        float dirx = lastCenter.x - newCenter.x;
        float diry = lastCenter.y - newCenter.y;
        camForward = moveSpeed * diry * GameConsts.TimeScale * camForward.normalized;
        camRight = moveSpeed * dirx * GameConsts.TimeScale * camRight.normalized;
        var movetoPos = LimitedCameraPosition(mCtrCamera.position + camForward + camRight);
        mCtrCamera.position = movetoPos;
        lastCenter = newCenter;
    }

    Vector2 GetCenter(Touch[] touches)
    {
        Vector2 mid = Vector2.zero;
        for (int i = 0; i < touches.Length; ++i)
        {
            mid += touches[i].position;
        }
        return mid / touches.Length;
    }

    float GetTouchSpan(Touch[] touches)
    {
        Vector2 mid = GetCenter(touches);

        float dist = 0f;

        for (int i = 0; i < touches.Length; ++i)
        {
            dist += Vector2.Distance(mid, touches[i].position);
        }
        return dist / touches.Length;
    }

    private float DeltaAngle(Touch one, Touch other)
    {
        return Mathf.Abs(Vector2.SignedAngle(one.deltaPosition, other.deltaPosition));
    }

    private static bool TryGetFirstTwoNonUiTouches(Touch[] touches, out Touch first, out Touch second, out int validCount)
    {
        first = default;
        second = default;
        validCount = 0;
        if (touches == null || touches.Length == 0) return false;

        for (int i = 0; i < touches.Length; i++)
        {
            var t = touches[i];
            if (IsTouchOnBlockingUI(t)) continue;

            if (validCount == 0) first = t;
            else if (validCount == 1) second = t;
            validCount++;
        }

        return validCount > 0;
    }

    /// <summary>
    /// 判断触摸是否在"真正阻塞"的 UI 上（Selectable / IDragHandler）。
    /// Passthrough 层（UICameraModeGlobalClosePassthrough）不视为阻塞，
    /// 以免全屏遮罩导致所有双指手势被过滤。
    /// </summary>
    private static bool IsTouchOnBlockingUI(Touch touch)
    {
        if (EventSystem.current == null) return false;
        if (!EventSystem.current.IsPointerOverGameObject(touch.fingerId)) return false;

        _multiTouchRaycastResults.Clear();
        var ped = new PointerEventData(EventSystem.current)
        {
            position = touch.position,
            pointerId = touch.fingerId
        };
        EventSystem.current.RaycastAll(ped, _multiTouchRaycastResults);

        for (int i = 0; i < _multiTouchRaycastResults.Count; i++)
        {
            var go = _multiTouchRaycastResults[i].gameObject;
            if (go == null) continue;

            if (IsPassthroughUI(go)) return false;
            if (go.GetComponent<Selectable>() != null) return true;
            if (go.GetComponent<IDragHandler>() != null) return true;
        }

        return false;
    }

    private static bool IsPassthroughUI(GameObject go)
    {
        for (var t = go != null ? go.transform : null; t != null; t = t.parent)
        {
            if (t.GetComponent("UICameraModeGlobalClosePassthrough") != null) return true;
        }
        return false;
    }

    private Vector3 LimitedWroldPosition(Vector3 currentPosition)
    {
        Vector3 limited = currentPosition;
        if (currentPosition.x > maxWidth / 2)
        {
            limited.x = maxWidth / 2;
        }
        else if (currentPosition.x < -maxWidth / 2)
        {
            limited.x = -maxWidth / 2;
        }

        if (currentPosition.z > maxHeight / 2)
        {
            limited.z = maxHeight / 2;
        }
        else if (currentPosition.z < -maxHeight / 2)
        {
            limited.z = -maxHeight / 2;
        }
        return limited;
    }

    private Vector3 LimitedCameraPosition(Vector3 currentPosition)
    {
        //TODO:Downtown地图轴心不在中心点位置，后续需要特使处理
        Vector3 movetoPos = LimitedWroldPosition(currentPosition);
        if (movetoPos.x > (orginPos.x + MOVE_LIMIT))
        {
            movetoPos.x = orginPos.x + MOVE_LIMIT;
        }
        else if (movetoPos.x < (orginPos.x - MOVE_LIMIT))
        {
            movetoPos.x = orginPos.x - MOVE_LIMIT;
        }

        if (movetoPos.z > orginPos.z + MOVE_LIMIT)
        {
            movetoPos.z = orginPos.z + MOVE_LIMIT;
        }
        else if (movetoPos.z < orginPos.z - MOVE_LIMIT)
        {
            movetoPos.z = orginPos.z - MOVE_LIMIT;
        }
        return movetoPos;
    }

    public override void OnTouchBegin(Touch touch)
    {
        rotTouchId = touch.fingerId;
    }

    public Vector3 Vec3Rotate(Vector3 source,float angle)
    {
        Quaternion q = Quaternion.AngleAxis(angle,Vector3.forward);
        return q * source;
    }

    public override void OnTouchStay(Touch touch)
    {
        if (touch.fingerId == rotTouchId)
        {
            if (touch.phase == TouchPhase.Ended)
            {
                rotTouchId = -1;
            }
            else if (touch.phase == TouchPhase.Moved)
            {
                float speed;
                if (PinchZoomFOVOnly)
                {
                    // Follow 模式：使用与游玩模式相同的灵敏度
                    speed = GameDataManager.Inst?.globalSettingData?.cameraPanSensitivity ?? 0.31f;
                }
                else
                {
                    speed = rotateSpeed * GameConsts.TimeScale;
                }
                Vector3 offset = speed * touch.deltaPosition;
                Rotate(-offset.y, offset.x);
            }
        }
    }
}
