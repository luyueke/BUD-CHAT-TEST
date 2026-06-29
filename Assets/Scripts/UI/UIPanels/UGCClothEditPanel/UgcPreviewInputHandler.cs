using System;
using Basic;
using DG.Tweening;
using Es;
using Game.Config;
using Game.UGCEditor;
using GameData.BaseInfo;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// UGC衣服手势操作
/// - 放大缩小手势
/// - 单指y轴360度旋转
/// - 单指上下移动手势
/// </summary>
public class UgcPreviewInputHandler : InputHandler
{
    private new enum MultiGesture
    {
        None,
        TwoFingers,
        Span,
        Move,
        SingleClick,
        SingleFingerMove,
        SingleFingerRotate
    }

    public enum ClothRotType
    {
        Y_Axie = 0,
        YX_Axie = 1
    }
    
    private const float SingleMoveThreshHold = 0.1f;
    private GameObject _targetModel;
    private GameObject _clickArea;
    private MultiGesture _gesture;
    private float _lastTouchSpan;
    private Vector2 _lastCenter;
    private Vector3 editAngle;
    private UGCClothEditorConfig _config;
    private Camera _previewCamera;
    private Action<Transform> _simpleClickAct;
    private float speed = 0.2f;
    private bool _isAvatar = false;
    private ClothRotType rotType = ClothRotType.YX_Axie;
    private Canvas uiCanvas;

    /// <summary>自定义射线检测相机，null 时回退到默认 UICamera。</summary>
    private Camera _raycastCamera;

    /// <summary>自定义射线检测层级掩码，0 时回退到默认 "UI" 层。</summary>
    private int _raycastLayerMask;

    /// <summary>
    /// 旋转锁定开关。为 true 时禁止拖拽旋转（用于展示BOX预览模式）。
    /// </summary>
    public bool IsRotationLocked { get; set; }

    #region 初始化

    private int _skinType;
    
    public UgcPreviewInputHandler(int skinType)
    {
        _skinType = skinType;
    }
    /// <summary>
    /// 预览衣服/人物
    /// </summary>
    /// <param name="target">被控制的衣服模型/人物模型</param>
    /// <param name="partName">索引值，用于查找UGCClothGuestureConfig表</param>
    /// <param name="isAvatar">是否是人物模型预览，传True则partName前默认加上"avatar_"前缀</param>
    public void SetPreviewTarget(GameObject target, UgcPartData partData, bool isAvatar = false, bool isCameraReset = true)
    {
        SetTargetModel(target);
        if (partData==null)
        {
            return;
        }
        var key = isAvatar ? "avatar_" + partData.uId :  partData.uId;
        _isAvatar = isAvatar;
        SetConfigData(isAvatar,key,partData.rotAngle);
        uiCanvas = GameObject.Find("Canvas").GetComponent<Canvas>();
        if (isCameraReset)
        {
            ResetPreview();
        }
    }

    public void SetPreviewCamera(Camera camera)
    {
        _previewCamera = camera;
    }

    private void SetTargetModel(GameObject target)
    {
        _targetModel = target;
    }

    public void SetClickArea(GameObject clickArea)
    {
        _clickArea = clickArea;
    }

    private void SetConfigData(bool isAvatar,string partName, Vector3 angle)
    {
        _config = _skinType == (int)SkinType.Avatar?DataTables.GetUGCClothEditorConfig(partName):DataTables.GetPetUGCClothEditorConfig(partName);
        editAngle = isAvatar ? _config.DefalutModelRot : angle;

        if (_config == null)
        {
            LoggerUtils.LogError(
                $"UgcPreviewInputHandler SetConfigData failed, pls check excel config !!, key={partName}");
        }
    }

    #endregion

    #region InputHandler Override

    public override void OnTouchBegin(Touch touch)
    {
        base.OnTouchBegin(touch);
    }

    public override void OnShortTouchEnd(Touch touch)
    {
        OnSimpleClick(touch);
    }

    public override void OnTouchStay(Touch touch)
    {
    }

    public override void OnLongTouchEnd(Touch touch)
    {
        _gesture = MultiGesture.None;
    }

    public override void OnMovementTouchStay(Touch touch)
    {
        // Debug.Log("OnMovementTouchStay-----------" + CheckCanTouch());
        if (!CheckCanTouch())
            return;

        // var delta = touch.deltaPosition;
        // var x = delta.x;
        // var y = delta.y;
        // var absX = Mathf.Abs(x);
        // var absY = Mathf.Abs(y);
        OnDragAndRotate(touch);
    }

    public override void OnMultipleTouchesBegin(Touch[] touches)
    {
        //Debug.Log("OnMultipleTouchesBegin-----------" + CheckCanTouch());
        if (!CheckCanTouch())
            return;

        _lastTouchSpan = GetTouchSpan(touches);
        _lastCenter = GetCenter(touches);
        if (touches.Length == 2) {
            _gesture = MultiGesture.TwoFingers;
        }
    }

    public override void OnMultipleTouchesStay(Touch[] touches)
    {
        //Debug.Log("OnMultipleTouchesStay-----------" + CheckCanTouch());
        if (!CheckCanTouch())
            return;

        // zoom       
        if (_gesture == MultiGesture.TwoFingers)
        {
            float angle = DeltaAngle(touches[0], touches[1]);
            if (angle == 0) {
                return;
            }
            if (angle >= 90f) {
                _gesture = MultiGesture.Span;
            } else {
                _gesture = MultiGesture.Move;
        }
    }

        if (_gesture == MultiGesture.Span) {
            float newTouchSpan = GetTouchSpan(touches);
            OnZoom(newTouchSpan);

        } else if (_gesture == MultiGesture.Move) {
            var newCenter = GetCenter(touches);
            OnMove(newCenter);
        }

    }

    private float DeltaAngle(Touch one, Touch other)
    {
        return Mathf.Abs(Vector2.SignedAngle(one.deltaPosition, other.deltaPosition));
    }
    
    public override bool OnDragJoyStick(Touch touche)
    {
        return false;
    }

    public override void OnMouseScrollWheel_Unity()
    {
        if (_config == null) return;

        _gesture = MultiGesture.TwoFingers;
        var dis = (Input.GetAxis("Mouse ScrollWheel") > 0 ? 1f : -1f) * _config.ZoomSpeed * GameConsts.TimeScale;
        DoZoom(dis);
    }

    #endregion

    #region 缩放

    private Vector2 GetCenter(Touch[] touches)
    {
        Vector2 mid = Vector2.zero;
        for (int i = 0; i < touches.Length; ++i)
        {
            var touchPos = touches[i].position;
            Vector2 outVec;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(uiCanvas.transform as RectTransform, touchPos, uiCanvas.worldCamera, out outVec))
            {
                mid += outVec;
            }
        }
        return mid / touches.Length;
    }

    private float GetTouchSpan(Touch[] touches)
    {
        Vector2 mid = GetCenter(touches);
        float dist = 0f;
        for (int i = 0; i < touches.Length; ++i)
        {
            var touchPos = touches[i].position;
            Vector2 outVec;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(uiCanvas.transform as RectTransform, touchPos, uiCanvas.worldCamera, out outVec))
            {
                dist += Vector2.Distance(mid, outVec);
            }
        }
        return dist / touches.Length;
    }

    //缩放
    private void OnZoom(float newTouchSpan)
    {
        var scaleDir = newTouchSpan - _lastTouchSpan;

        if (Mathf.Abs(scaleDir) > 5f) {
            scaleDir = scaleDir > 0 ? 1 : -1;
        } else {
            return;
        }
        var offset = _config.ZoomSpeed *  GameConsts.TimeScale * scaleDir; //固定缩放速度
        DoZoom(offset);
        _lastTouchSpan = newTouchSpan;

    }

    //改成不控制相机，直接缩放GameObject
    private void DoZoom(float offset)
    {
        if (_gesture != MultiGesture.Span) return;

        if (_targetModel == null || _config == null) return;

        var trans = _targetModel.transform;
        var currentOffset = trans.localScale.x;
        var newOffset = currentOffset + offset;
        newOffset = Mathf.Clamp(newOffset, _config.ZoomScaleMin, _config.ZoomScaleMax);
        // Debug.Log($"DoZoom--------offset:{offset}--newOffset:{newOffset}");
        trans.localScale = new Vector3(newOffset, newOffset, newOffset);

        // //缩放后需要检查一次当前位置
        // var curPos = _targetModel.transform.localPosition;
        // var limitedy = LimitedPosition(curPos.y);
        // var limPos = new Vector3(curPos.x, limitedy, curPos.z);
        // DOTween.To(() => _targetModel.transform.localPosition, x => _targetModel.transform.localPosition = x, limPos,
        //     0.1f);
    }

    #endregion

    #region Y轴旋转

    private void DoYRotate(float deltaX)
    {
        if (_targetModel == null) return;

        if (IsRotationLocked) return;

        _targetModel.transform.Rotate(Vector3.up, _config.YRotateSpeed * deltaX * -1);

        // Debug.Log($"DoYRotate-----------{_config.YRotateSpeed * deltaX}");
    }

    private void OnDragAndRotate(Touch touch)
    {
        if (_targetModel == null)
            return;

        if (IsRotationLocked)
            return;

        rotType =_isAvatar? ClothRotType.Y_Axie:ClothRotType.YX_Axie;

        switch (rotType)
        {
            case ClothRotType.Y_Axie:
                _targetModel.transform.Rotate(Vector3.up, -speed * touch.deltaPosition.x);
                break;
            case ClothRotType.YX_Axie:
                float x = _targetModel.transform.localEulerAngles.x - speed * touch.deltaPosition.y;
                if (x > 90&& x < 270)
                {
                    if (touch.deltaPosition.y<0)//避免在边界值旋转卡顿
                    {
                        x = 90;
                    }
                    else if(touch.deltaPosition.y>0)
                    {
                        x = 270;
                    }
                    else
                    {
                        x = _targetModel.transform.localEulerAngles.x;
                    }
                }
                float y = _targetModel.transform.localEulerAngles.y - speed * touch.deltaPosition.x;
                _targetModel.transform.localEulerAngles = new Vector3(x, y, 0);
                break;
        }
        // Vector2 move = 0.5f * touch.deltaPosition;
        // _targetModel.transform.Rotate(Vector3.up, -move.x, Space.World);
    }

    #endregion

    #region 上下移动

    void OnMove(Vector2 newCenter) {
        var moveDir = (newCenter - _lastCenter).y;
        if (Mathf.Abs(moveDir) > 5f) {
            moveDir = moveDir > 0 ? 1 : -1;
        } else {
            return;
        }
        var offset = _config.UpDownMoveSpeed * GameConsts.TimeScale * moveDir; //固定缩放速度
        DoMove(offset);
        _lastCenter = newCenter;
    }

    void DoMove(float offset) {

        if (_targetModel == null || _config == null) return;
        if (_gesture != MultiGesture.Move) return;
        // if (!IsCanUpDownMove()) {
        //     var pos = _targetModel.transform.localPosition;
        //     _targetModel.transform.localPosition = new Vector3(pos.x, _config.DefalutModelPos.y, pos.z);
        //     return;
        // }

        var oriPos = _targetModel.transform.localPosition;
        float posY = oriPos.y + offset;
        var curScale = _targetModel.transform.localScale.x;
        posY = Mathf.Clamp(posY, _config.DefalutModelPos.y + _config.UpDownMoveMin,
            _config.DefalutModelPos.y + _config.UpDownMoveMax);
        _targetModel.transform.localPosition = new Vector3(oriPos.x, posY, oriPos.z);
    }

    void OnDragAndVerticalMove(Touch touch)
    {
        if (_targetModel == null || _config == null) return;
        if (!IsCanUpDownMove())
        {
            var pos = _targetModel.transform.localPosition;
            _targetModel.transform.localPosition = new Vector3(pos.x,_config.DefalutModelPos.y,pos.z);
            return;
        }
        var oriPos = _targetModel.transform.localPosition;
        Vector3 screenPosition = new Vector3(touch.position.x, touch.position.y, oriPos.z);
        Vector3 curPos = _previewCamera.ScreenToWorldPoint(screenPosition);
        Vector3 lastScreenPosition = new Vector3(touch.position.x - touch.deltaPosition.x, touch.position.y - touch.deltaPosition.y, oriPos.z);
        Vector3 lastPos = _previewCamera.ScreenToWorldPoint(lastScreenPosition);
        var offset =curPos.y - lastPos.y;
        float posY = oriPos.y + offset;
        var curScale = _targetModel.transform.localScale.x;
        posY = Mathf.Clamp(posY,_config.DefalutModelPos.y + _config.UpDownMoveMin*curScale,_config.DefalutModelPos.y+ _config.UpDownMoveMax*curScale);
        _targetModel.transform.localPosition = new Vector3(oriPos.x,posY,oriPos.z);
    }
    
    private bool IsCanUpDownMove()
    {
        //缩放到最小则不允许上下滑动
        if (_targetModel.transform.localScale.x <= _config.ZoomScaleMin)
        {
            return false;
        }

        return true;
    }

    //线性缩放 与 坐标的直线表达式
    private float GetCurMaxY(float curScale)
    {
        if (_isAvatar)
        {
            return 0.36f * curScale - 1.13f;
        }
        else
        {
            return 0.1f * curScale - 0.8f;
        }
    }

    private float GetCurMinY(float curScale)
    {
        if (_isAvatar)
        {
            return -1.167f * curScale + 0.7f;
        }
        else
        {
            return -0.575f * curScale + 0.55f;
        }
    }

    #endregion

    #region 重置预览

    private const float ResetPreviewAnimTime = 0.3f;

    public void ResetPreview()
    {
        if (_config == null) return;
        SetPreviewPosition(new UgcClothPreviewParas(editAngle,_config), ResetPreviewAnimTime);
    }

    public UgcClothPreviewParas GetPreviewPosition()
    {
        return new UgcClothPreviewParas()
        {
            LocalPos = _targetModel.transform.localPosition,
            LocalRot = _targetModel.transform.localRotation.eulerAngles,
            LocalSca = _targetModel.transform.localScale
        };
    }

    public void SetPreviewPosition(UgcClothPreviewParas previewParas = null)
    {
        previewParas ??= new UgcClothPreviewParas(editAngle,_config);
        _targetModel.transform.localPosition = previewParas.LocalPos;
        _targetModel.transform.localRotation = Quaternion.Euler(previewParas.LocalRot);
        _targetModel.transform.localScale = previewParas.LocalSca;
    }

    private void SetPreviewPosition(UgcClothPreviewParas previewParas = null, float animTime = 0f)
    {
        if (_config == null || _targetModel == null || previewParas == null) return;
        _targetModel.transform.DOLocalMove(previewParas.LocalPos, animTime).SetEase(Ease.Linear);
        _targetModel.transform.DOLocalRotate(previewParas.LocalRot, animTime).SetEase(Ease.Linear);
        _targetModel.transform.DOScale(previewParas.LocalSca, animTime).SetEase(Ease.Linear);
    }

    public class UgcClothPreviewParas
    {
        public Vector3 LocalPos;
        public Vector3 LocalRot;
        public Vector3 LocalSca;

        public UgcClothPreviewParas()
        {
        }

        public UgcClothPreviewParas(Vector3 rot,UGCClothEditorConfig cfg)
        {
            if (cfg == null) return;
            LocalPos = cfg.DefalutModelPos;
            LocalRot = rot;
            LocalSca = cfg.DefalutModelScale;
        }
    }

    #endregion

    #region 点击切换部件

    private RectTransform dragRect;
    public void SetDragRect(RectTransform rect)
    {
        dragRect = rect;
    }


    private void OnSimpleClick(Touch touch)
    {
        var uiCamera = GlobalCameraManager.Inst.UICamera;
        if (dragRect == null || uiCamera == null) return;

        // DragRect 属于 UI Canvas，始终用 UICamera 做区域包含判断
        if (RectTransformUtility.RectangleContainsScreenPoint(dragRect, touch.position,
                uiCamera))
        {
            // 射线发射相机和层级支持自定义（如盒子场景使用 AvatarCamera + Terrain 层）
            var rayCamera = _raycastCamera != null ? _raycastCamera : uiCamera;
            var layerMask = _raycastLayerMask != 0 ? _raycastLayerMask : 1 << LayerMask.NameToLayer("UI");
            Ray ray = rayCamera.ScreenPointToRay(touch.position);
            bool isHit = Physics.Raycast(ray, out RaycastHit hit, 1000f, layerMask);
            Debug.DrawRay(ray.origin, ray.direction, Color.red, 10);
            if (isHit)
            {
                _simpleClickAct?.Invoke(hit.transform);
            }
        }
    }

    public void SetSimpleClickAction(Action<Transform> action)
    {
        _simpleClickAct = action;
    }

    /// <summary>
    /// 设置自定义射线检测相机和层级掩码。未调用时保持默认行为（UICamera + "UI" 层），不影响其他使用方。
    /// </summary>
    /// <param name="camera">执行 Raycast 的相机（应与模型渲染相机一致）。</param>
    /// <param name="layerMask">射线检测的层级掩码。</param>
    public void SetRaycastConfig(Camera camera, int layerMask)
    {
        _raycastCamera = camera;
        _raycastLayerMask = layerMask;
    }

    #endregion

    #region 范围检查

    private bool CheckCanTouch()
    {
        GameObject selectObj = EventSystem.current.currentSelectedGameObject;
        if (selectObj == null || selectObj != _clickArea)
        {
            return false;
        }

        return true;
    }

    #endregion
}