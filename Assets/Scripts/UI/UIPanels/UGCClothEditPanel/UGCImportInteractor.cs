using System;
using System.Collections.Generic;
using Basic;
using Basic.UndoRedo;
using UndoSystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UGCEditor
{
    public enum InteractorType
    {
        none,
        move,
        rote,
        scale,
        detailScale,
    }

    public class UGCImportInteractor : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler,
        IPointerClickHandler, IPointerDownHandler
    {
        public List<UnityEngine.U2D.SpriteAtlas> SpriteAtlasList;
        public InteratorMode InteMode { get; set; } = InteratorMode.Normal;
        /// <summary>
        /// 需要操作的组件
        /// </summary>
        public GameObject targetGameObject;

        public RectTransform targetRecTrans;

        private BoxCollider targetCollider;


        /// <summary>
        /// 自己的recttransfrom
        /// </summary>
        public RectTransform recTrans;

        public BoxCollider clickArea;
        public InteractorType type;

        public bool isDrag = false;

        public Button copyBtn;

        public Button desBtn;

        Vector2 lastScaleTouchPos;

        ElementUndoData beginData;
        ElementUndoData endData;

        [SerializeField] private Image rightLine;
        [SerializeField] private Image leftLine;
        [SerializeField] private Image topLine;
        [SerializeField] private Image bottomLine;

        [SerializeField] private GameObject corner;
        [SerializeField] private GameObject detailTransform;
        [SerializeField] private GameObject LTCorner;
        [SerializeField] private GameObject RTCorner;
        [SerializeField] private GameObject LBCorner;
        [SerializeField] private GameObject RBCorner;

        private float startAnlge = 0;

        private Vector2 startDragPoint;

        // 吸附角度范围
        [SerializeField] private float adsorptionAngleRange = 3;

        // 吸附角度
        private float adsorptionAngle = 45;
        private float lineWidth;

        private readonly float photoMinSize = 100;
        private Vector2 detailScaleDir;
        private Vector2 lastVerticalPoint;
        private float lastDis;
        private float includedAngle;
        private float tanAngle;
        private float cotAngle;
        private float minAngle;
        private float maxAngle;
        private Vector2 extrudeDir;
        private GameObject curSelectCorner;

        public void Init()
        {
            copyBtn.onClick.AddListener(OnCopy);
            desBtn.onClick.AddListener(OnDesTarget);
        }

        #region 事件注册

        public Action SetUndoRedoAct;
        private Action OnTransformChange;
        public Action OnCopyAct;
        public Action OnDistroyAct;
        public Action OnSelectAct;
        public Action OnReplace;
        public Action OnEndDragAct;
        public Action OnTransUndoAct;
        public Action<ElementUndoData> CreatUndoDataAct;

        #endregion

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="target"></param>
        public void Show(RectTransform rectTrans, Action onChange, Action init)
        {
            OnTransformChange = onChange;
            targetGameObject = rectTrans.gameObject;
            targetRecTrans = rectTrans;
            targetCollider = targetGameObject.GetComponent<BoxCollider>();
            targetCollider.size = rectTrans.sizeDelta;
            clickArea.size = rectTrans.sizeDelta;
            recTrans = transform.GetComponent<RectTransform>();
            transform.position = targetGameObject.transform.position;
            recTrans.sizeDelta = rectTrans.sizeDelta;
            transform.rotation = targetGameObject.transform.rotation;
            includedAngle = Mathf.Atan2(recTrans.sizeDelta.x, recTrans.sizeDelta.y) * Mathf.Rad2Deg;
            tanAngle = targetRecTrans.sizeDelta.y / targetRecTrans.sizeDelta.x;
            cotAngle = targetRecTrans.sizeDelta.x / targetRecTrans.sizeDelta.y;
            init?.Invoke();
            gameObject.SetActive(true);
            
            if (InteMode == InteratorMode.Transform)
            {
                ShowBtn(false);
                SetLineColor(Color.gray);
                detailTransform.gameObject.SetActive(true);
            }
        }
        
        public void OnPointerDown(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(recTrans, eventData.position,
                eventData.pressEventCamera, out lastScaleTouchPos);
            beginData = CreateUndoData(UGCElementType.Trans, targetRecTrans);
            startAnlge = recTrans.localEulerAngles.z;
            var curCamera = GlobalCameraManager.Inst.UICamera;
            startDragPoint = eventData.position - (Vector2) curCamera.WorldToScreenPoint(recTrans.position);

            Ray ray = curCamera.ScreenPointToRay(eventData.position);
            if (Physics.Raycast(ray, out RaycastHit hit, 1000,
                    1 << LayerMask.NameToLayer("Anchors")))
            {
                var obj = hit.collider.gameObject;
                if (obj.name == "MFbtn")
                {
                    type = InteractorType.scale;
                }
                else if (obj.name == "Detailbtn")
                {
                    type = InteractorType.detailScale;
                    curSelectCorner = obj;
                }
                else if (obj.name == "RotateBtn")
                {
                    type = InteractorType.rote;
                }
                else
                {
                    type = InteractorType.move;
                }
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            isDrag = true;

            switch (type)
            {
                case InteractorType.scale:
                    StartRotateAround();
                    break;
                case InteractorType.detailScale:
                    StartDetailScale(eventData);
                    break;
            }
        }

        private Vector2 CalPos(float harfW, float harfH, float pivotX, float pivotY, float angle)
        {
            int xPNNum = pivotX == 0 ? -1 : 1;
            int yPNNum = pivotY == 0 ? -1 : 1;

            float xPos = xPNNum * harfW * Mathf.Cos(angle * Mathf.Deg2Rad) -
                         yPNNum * harfH * Mathf.Sin(angle * Mathf.Deg2Rad);
            angle = 90 - angle;
            float yPos = xPNNum * harfW * Mathf.Cos(angle * Mathf.Deg2Rad) +
                         yPNNum * harfH * Mathf.Sin(angle * Mathf.Deg2Rad);

            return new Vector2(xPos, yPos);
        }

        public void OnDrag(PointerEventData eventData)
        {
            switch (type)
            {
                case InteractorType.none:
                    break;
                case InteractorType.move:
                    MoveSelectedObject(eventData.delta);
                    break;
                case InteractorType.rote:
                    RotateAround(eventData, false);
                    break;
                case InteractorType.scale:
                    RotateAround(eventData);
                    ScaleSelectedObject(eventData.position, eventData.pressEventCamera);
                    break;
                case InteractorType.detailScale:
                    SetDeailScale(eventData);
                    break;
            }

            OnTransformChange?.Invoke();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            isDrag = false;
            if (targetGameObject)
            {
                OnEndDragAct?.Invoke();
            }

            endData = CreateUndoData(UGCElementType.Trans, targetRecTrans);
            AddRecord(beginData, endData);

            if (type == InteractorType.scale)
            {
                EndRotateAround();
            }
            else if (type == InteractorType.detailScale)
            {
                EndDeailScale();
            }

            type = InteractorType.none;
            beginData = null;
            endData = null;
            SetUndoRedoAct?.Invoke();
        }

        public void MoveSelectedObject(Vector2 translateAmount)
        {
            RestrictedArea();
            targetRecTrans.anchoredPosition += translateAmount;
            recTrans.anchoredPosition += translateAmount;
        }

        private void RestrictedArea()
        {
            if (targetRecTrans.localPosition.x > TransformInteractorController.RestrictX ||
                targetRecTrans.localPosition.x < -TransformInteractorController.RestrictX
                || targetRecTrans.localPosition.y > TransformInteractorController.RestrictY ||
                targetRecTrans.localPosition.y < -TransformInteractorController.RestrictY)
            {
                Vector2 maxSize = targetRecTrans.localPosition;
                if (targetRecTrans.localPosition.x > TransformInteractorController.RestrictX)
                {
                    maxSize = new Vector2(TransformInteractorController.RestrictX, targetRecTrans.localPosition.y);
                    return;
                }
                else if (targetRecTrans.localPosition.x < -TransformInteractorController.RestrictX)
                {
                    maxSize = new Vector2(-TransformInteractorController.RestrictX, targetRecTrans.localPosition.y);
                }
                else if (targetRecTrans.localPosition.y > TransformInteractorController.RestrictY)
                {
                    maxSize = new Vector2(targetRecTrans.localPosition.x, TransformInteractorController.RestrictY);
                }
                else if (targetRecTrans.localPosition.y < -TransformInteractorController.RestrictY)
                {
                    maxSize = new Vector2(targetRecTrans.localPosition.x, -TransformInteractorController.RestrictY);
                }

                targetRecTrans.localPosition = maxSize;
                recTrans.localPosition = maxSize;
            }
        }

        private void StartRotateAround()
        {
            ShowBtn(false);
            SetLineColor(Color.red);
        }

        private void EndRotateAround()
        {
            ShowBtn(true);
            SetLineColor(Color.black);
            SetLineWidth(4);
        }

        private void SetLineColor(Color color)
        {
            rightLine.color = color;
            leftLine.color = color;
            topLine.color = color;
            bottomLine.color = color;
        }

        private void SetLineWidth(float width)
        {
            if (lineWidth == width) return;

            lineWidth = width;

            rightLine.rectTransform.sizeDelta = new Vector2(width, width * 2);
            leftLine.rectTransform.sizeDelta = new Vector2(width, width * 2);
            topLine.rectTransform.sizeDelta = new Vector2(0, width);
            bottomLine.rectTransform.sizeDelta = new Vector2(0, width);

            rightLine.rectTransform.anchoredPosition = new Vector2(width * 0.5f, 0);
            leftLine.rectTransform.anchoredPosition = new Vector2(-width * 0.5f, 0);
            topLine.rectTransform.anchoredPosition = new Vector2(0, width * 0.5f);
            bottomLine.rectTransform.anchoredPosition = new Vector2(0, -width * 0.5f);
        }

        private void ShowBtn(bool isShow)
        {
            corner.SetActive(isShow);
            copyBtn.gameObject.SetActive(isShow);
            desBtn.gameObject.SetActive(isShow);
        }

        private float VectorAngle(Vector2 from, Vector2 to)
        {
            float angle;

            Vector3 cross = Vector3.Cross(from, to);
            angle = Vector2.Angle(from, to);
            return cross.z > 0 ? -angle : angle;
        }

        public void RotateAround(PointerEventData eventData, bool isAdsorption = true)
        {
            Vector2 curPoint = eventData.position -
                               (Vector2) GlobalCameraManager.Inst.UICamera.WorldToScreenPoint(recTrans.position);
            float moveAngle = VectorAngle(startDragPoint, curPoint);
            float desAngle = startAnlge - moveAngle;

            if (desAngle < 0)
            {
                desAngle += 360;
            }

            // 是否带吸附效果
            if (isAdsorption)
            {
                float remainder = desAngle % adsorptionAngle;

                if (remainder < adsorptionAngleRange)
                {
                    desAngle -= remainder;
                    SetLineWidth(10);
                }
                else
                {
                    remainder = adsorptionAngle - remainder;
                    if (remainder < adsorptionAngleRange)
                    {
                        desAngle += remainder;
                        SetLineWidth(10);
                    }
                    else
                    {
                        SetLineWidth(4);
                    }
                }
            }

            Vector3 temp = targetRecTrans.eulerAngles;
            temp.z = desAngle;

            targetRecTrans.eulerAngles = temp;
            recTrans.eulerAngles = temp;
        }

        private Vector2 GetVerticalPoint(Vector2 eventPos)
        {
            float seq = Mathf.Pow(detailScaleDir.x, 2) + Mathf.Pow(detailScaleDir.y, 2);
            float p = eventPos.x * detailScaleDir.x + eventPos.y * detailScaleDir.y;
            float x = p * detailScaleDir.x / seq;
            float y = p * detailScaleDir.y / seq;

            return new Vector2(x, y);
        }

        private void StartDetailScale(PointerEventData eventData)
        {
            Vector2 photoScreenPos = (Vector2) GlobalCameraManager.Inst.UICamera.WorldToScreenPoint(recTrans.position);
            Vector2 cornerScreenPos =
                (Vector2) GlobalCameraManager.Inst.UICamera.WorldToScreenPoint(curSelectCorner.transform.position);
            lastDis = (cornerScreenPos - photoScreenPos).magnitude;

            Vector2 lbN = (LBCorner.transform.position - recTrans.position).normalized;
            Vector2 rtN = (RTCorner.transform.position - recTrans.position).normalized;
            Vector2 rbN = (RBCorner.transform.position - recTrans.position).normalized;
            Vector2 ltN = (LTCorner.transform.position - recTrans.position).normalized;
            extrudeDir = (curSelectCorner.transform.position - recTrans.position).normalized;

            switch (curSelectCorner.transform.parent.name)
            {
                case "LTCorner":
                    recTrans.pivot = new Vector2(1, 0);
                    targetRecTrans.pivot = new Vector2(1, 0);

                    minAngle = VectorAngle(extrudeDir, lbN);
                    maxAngle = VectorAngle(extrudeDir, rtN);
                    break;
                case "RTCorner":
                    recTrans.pivot = new Vector2(0, 0);
                    targetRecTrans.pivot = new Vector2(0, 0);

                    minAngle = VectorAngle(extrudeDir, ltN);
                    maxAngle = VectorAngle(extrudeDir, rbN);
                    break;
                case "LBCorner":
                    recTrans.pivot = new Vector2(1, 1);
                    targetRecTrans.pivot = new Vector2(1, 1);

                    minAngle = VectorAngle(extrudeDir, rbN);
                    maxAngle = VectorAngle(extrudeDir, ltN);
                    break;
                case "RBCorner":
                    recTrans.pivot = new Vector2(0, 1);
                    targetRecTrans.pivot = new Vector2(0, 1);

                    minAngle = VectorAngle(extrudeDir, rtN);
                    maxAngle = VectorAngle(extrudeDir, lbN);
                    break;
            }

            float harfW = recTrans.sizeDelta.x * 0.5f;
            float harfH = recTrans.sizeDelta.y * 0.5f;
            float angle = recTrans.eulerAngles.z < 0 ? recTrans.eulerAngles.z + 180 : recTrans.eulerAngles.z;
            Vector2 offset = CalPos(harfW, harfH, recTrans.pivot.x, recTrans.pivot.y, angle);

            recTrans.anchoredPosition += offset;
            targetRecTrans.anchoredPosition += offset;

            detailScaleDir = (curSelectCorner.transform.position - recTrans.position).normalized;
            lastVerticalPoint = GetVerticalPoint(cornerScreenPos);
        }

        private void SetDeailScale(PointerEventData eventData)
        {
            Vector2 photoScreenPoint =
                (Vector2) GlobalCameraManager.Inst.UICamera.WorldToScreenPoint(recTrans.position);

            float angle = VectorAngle(extrudeDir, eventData.position - photoScreenPoint);
            bool isPositive = minAngle <= angle && angle <= maxAngle;

            Vector2 size = Vector2.zero;
            float curDis = 0;
            if (isPositive)
            {
                Vector2 curPoint = GetVerticalPoint(eventData.position);
                Vector2 deltaSize = curPoint - lastVerticalPoint;

                curDis = (curPoint - photoScreenPoint).magnitude;
                bool isBigScale = curDis > lastDis;

                lastVerticalPoint = curPoint;

                float sizeX = deltaSize.magnitude * Mathf.Sin(includedAngle * Mathf.Deg2Rad);
                float sizeY = deltaSize.magnitude * Mathf.Cos(includedAngle * Mathf.Deg2Rad);
                deltaSize = new Vector2(sizeX, sizeY) * (isBigScale ? 1 : -1);
                size = recTrans.sizeDelta + deltaSize;
            }

            if (size.x < photoMinSize || size.y < photoMinSize)
            {
                if (targetRecTrans.sizeDelta.x <= targetRecTrans.sizeDelta.y)
                {
                    size.x = photoMinSize;
                    size.y = photoMinSize * tanAngle;
                }
                else
                {
                    size.y = photoMinSize;
                    size.x = photoMinSize * cotAngle;
                }

                float minSize =
                    (GetVerticalPoint(
                        (Vector2) GlobalCameraManager.Inst.UICamera.WorldToScreenPoint(curSelectCorner.transform
                            .position)) - photoScreenPoint).magnitude;
                lastDis = curDis > minSize ? curDis : minSize;
            }
            else
            {
                lastDis = curDis;
            }

            recTrans.sizeDelta = size;
            targetRecTrans.sizeDelta = size;
            targetCollider.size = size;
            clickArea.size = size;
        }

        private void EndDeailScale()
        {
            float harfW = recTrans.sizeDelta.x * 0.5f;
            float harfH = recTrans.sizeDelta.y * 0.5f;

            float angle = recTrans.eulerAngles.z < 0 ? recTrans.eulerAngles.z + 180 : recTrans.eulerAngles.z;
            Vector2 pivot = Vector2.one - recTrans.pivot;

            Vector2 offset = CalPos(harfW, harfH, pivot.x, pivot.y, angle);

            recTrans.anchoredPosition += offset;
            targetRecTrans.anchoredPosition += offset;

            recTrans.pivot = new Vector2(0.5f, 0.5f);
            targetRecTrans.pivot = new Vector2(0.5f, 0.5f);
        }

        public void ScaleSelectedObject(Vector2 newPos, Camera pressEventCamera)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(recTrans, newPos, pressEventCamera,
                out Vector2 localTouchPos);
            Vector2 tempt = localTouchPos - lastScaleTouchPos;
            LimitMinSize(targetRecTrans);
            if (targetRecTrans.sizeDelta.x > targetRecTrans.sizeDelta.y)
            {
                var ratio = targetRecTrans.sizeDelta.x / targetRecTrans.sizeDelta.y;
                var newX = targetRecTrans.sizeDelta.x + tempt.x;
                targetRecTrans.sizeDelta = new Vector2(newX, newX / ratio);
            }
            else
            {
                var ratio = targetRecTrans.sizeDelta.y / targetRecTrans.sizeDelta.x;
                var newY = targetRecTrans.sizeDelta.y + tempt.x;
                targetRecTrans.sizeDelta = new Vector2(newY / ratio, newY);
            }

            recTrans.sizeDelta = targetRecTrans.sizeDelta;
            targetCollider.size = targetRecTrans.sizeDelta;
            clickArea.size = targetRecTrans.sizeDelta;
            lastScaleTouchPos = localTouchPos;
        }

        private void LimitMinSize(RectTransform rectTrans)
        {
            if (targetRecTrans.sizeDelta.x < targetRecTrans.sizeDelta.y && rectTrans.sizeDelta.y < 1)
            {
                if (targetRecTrans.sizeDelta.x == 0)
                {
                    return;
                }

                var ratio = targetRecTrans.sizeDelta.y / targetRecTrans.sizeDelta.x;
                rectTrans.sizeDelta = new Vector2(1 / ratio, 1);
            }

            if (targetRecTrans.sizeDelta.x > targetRecTrans.sizeDelta.y && rectTrans.sizeDelta.x < 1)
            {
                if (targetRecTrans.sizeDelta.y == 0)
                {
                    return;
                }

                var ratio = targetRecTrans.sizeDelta.x / targetRecTrans.sizeDelta.y;
                rectTrans.sizeDelta = new Vector2(1, 1 / ratio);
            }
        }
        

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!isDrag && targetGameObject != null)
            {
                OnSelectAct?.Invoke();
            }
        }

        public void OnCopy()
        {
            if (targetGameObject != null)
            {
                OnCopyAct?.Invoke();
            }
        }
        

        public void OnDesTarget()
        {
            if (targetGameObject != null)
            {
                OnDistroyAct?.Invoke();
            }
        }

        public void Hide()
        {
            ResetInfo();
            gameObject.SetActive(false);
        }

        public void RefreshTransfrom(RectTransform oriRectrans)
        {
            if (targetRecTrans == oriRectrans)
            {
                transform.position = targetRecTrans.position;
                recTrans.sizeDelta = targetRecTrans.sizeDelta;
                transform.rotation = targetRecTrans.rotation;

                includedAngle = Mathf.Atan2(recTrans.sizeDelta.x, recTrans.sizeDelta.y) * Mathf.Rad2Deg;
                tanAngle = targetRecTrans.sizeDelta.y / targetRecTrans.sizeDelta.x;
                cotAngle = targetRecTrans.sizeDelta.x / targetRecTrans.sizeDelta.y;
            }
        }

        public void ShowDetailTransform()
        {
            if (targetGameObject != null)
            {
                ShowBtn(false);
                SetLineColor(Color.gray);
                detailTransform.gameObject.SetActive(true);
            }
        }

        public void HideDetailTransform()
        {
            ShowBtn(true);
            SetLineColor(Color.black);
            detailTransform.gameObject.SetActive(false);
        }

        public void ResetInfo()
        {
            isDrag = false;
            targetCollider = null;
            targetGameObject = null;
            targetRecTrans = null;
            gameObject.SetActive(false);
            detailTransform.gameObject.SetActive(false);
            SetLineColor(Color.black);
            ShowBtn(true);
            ClearAction();
        }

        private void ClearAction()
        {
            OnCopyAct = null;
            OnDistroyAct = null;
            OnSelectAct = null;
            OnEndDragAct = null;
            OnTransUndoAct = null;
            CreatUndoDataAct = null;
        }

        #region Undo/Redo
        
        private ElementUndoData CreateUndoData(UGCElementType type, RectTransform rectTrans)
        {
            ElementUndoData data = new ElementUndoData();
            data.targetNode = rectTrans;
            data.postion = rectTrans.localPosition;
            data.eulerAngles = rectTrans.localEulerAngles;
            data.sizeDelta = rectTrans.sizeDelta;
            data.transformType = (int) type;
            CreatUndoDataAct?.Invoke(data);
            return data;
        }
        
        public void AddRecord(ElementUndoData beginData, ElementUndoData endData)
        {
            UndoRecord record = new UndoRecord(UndoHelperName.UGCClothElementUndoHelper);
            record.BeginData = beginData;
            record.EndData = endData;
            UndoRecordPool.Inst.PushRecord(record);
        }
        
        public void SetTransUndo(RectTransform targetTrans, Vector2 position, Vector3 rotate, Vector2 sizeDelta)
        {
            if (targetTrans == null)
            {
                return;
            }
        
            var behav = targetTrans.GetComponent<ElementBaseBehaviour>();
            targetTrans.localPosition = position;
            targetTrans.localEulerAngles = rotate;
            targetTrans.sizeDelta = sizeDelta;
            behav.OnTransformChange();
            OnTransUndoAct?.Invoke();
        }
        
        public void AddDestroyRecord(GameObject gameObject)
        {
            UGCClothesCreateDestroyUndoData beginData = new UGCClothesCreateDestroyUndoData();
            beginData.targetNode = gameObject;
            beginData.createUndoMode = (int) UndoRedoConfig.CreateUndoMode.Destroy;
            // beginData.type = type;
            UGCClothesCreateDestroyUndoData endData = new UGCClothesCreateDestroyUndoData();
            endData.targetNode = null;
            endData.createUndoMode = (int) UndoRedoConfig.CreateUndoMode.Destroy;
            // endData.type = type;
            UndoRecord record = new UndoRecord(UndoHelperName.UGCClothesCreateDestroyUndoHelper);
            record.BeginData = beginData;
            record.EndData = endData;
            UndoRecordPool.Inst.PushRecord(record);
        }

        #endregion
    }
}