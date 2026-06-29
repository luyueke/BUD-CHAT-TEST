using System;
using System.Collections.Generic;
using Basic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UGCEditor
{
    public class InteractorCopyEdit : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler,
        IPointerClickHandler, IPointerDownHandler
    {
        public List<UnityEngine.U2D.SpriteAtlas> SpriteAtlasList;

        /// <summary>
        /// ��Ҫ���������
        /// </summary>
        public GameObject targetGameObject;

        public RectTransform targetRecTrans;

        BoxCollider targetCollider;

        private enum ConerType
        {
            LT,
            RT,
            LD,
            RD
        }


        /// <summary>
        /// �Լ���recttransfrom
        /// </summary>
        public RectTransform recTrans;

        public BoxCollider clickArea;
        public InteractorType type;

        public bool isDrag = false;

        Vector2 lastScaleTouchPos;

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

        private float lineWidth;

        private float photoMinSize = 100;
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
        private bool isDetailTransform;
        public GameObject rotateBtn;
        public GameObject rotateTopLine;

        #region �¼�ע��

        public Action SetUndoRedoAct;
        private Action OnTransformChange;

        public Action OnSelectAct;
        public Action OnReplace;
        public Action OnEndDragAct;
        public Action<ElementUndoData> CreatUndoDataAct;

        #endregion

        private float _offset;
        public RectTransform point1;
        public RectTransform point2;

        /// <summary>
        /// ��ʼ��
        /// </summary>
        /// <param name="target"></param>
        public void Settup(RectTransform rectTrans, Action onChange, Action init, float offset)
        {
            OnTransformChange = onChange;
            _offset = offset;
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
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(recTrans, eventData.position,
                eventData.pressEventCamera, out lastScaleTouchPos);

            startAnlge = recTrans.localEulerAngles.z;
            startDragPoint = eventData.position -
                             (Vector2) GlobalCameraManager.Inst.UICamera.WorldToScreenPoint(recTrans.position);

            var curCamera = GlobalCameraManager.Inst.UICamera;
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
            curTranslateAmount = Vector2.zero;
            curDeltaSize = Vector2.zero;
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
                    RotateAround(eventData);
                    break;
                case InteractorType.scale:
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

            if (type == InteractorType.scale)
            {
                EndRotateAround();
            }
            else if (type == InteractorType.detailScale)
            {
                EndDeailScale();
            }

            type = InteractorType.none;

            SetUndoRedoAct?.Invoke();
            curTranslateAmount = Vector2.zero;
            curDeltaSize = Vector2.zero;
        }

        private Vector2 curTranslateAmount;

        public void MoveSelectedObject(Vector2 translateAmount)
        {
            curTranslateAmount += translateAmount;
            var v2 = GetOffsetMovePos(ref curTranslateAmount);
            Vector2 lastPos = new Vector2(targetRecTrans.anchoredPosition.x, targetRecTrans.anchoredPosition.y);
            targetRecTrans.anchoredPosition += v2;
            recTrans.anchoredPosition += v2;
            if (!CheckIsInMask())
            {
                targetRecTrans.anchoredPosition = lastPos;
                recTrans.anchoredPosition = lastPos;
            }
        }

        //控制移动单位
        public Vector2 GetOffsetMovePos(ref Vector2 translate)
        {

            var v2 = Vector2.zero;
            v2.x = (int) (translate.x / _offset) * _offset;
            v2.y = (int) (translate.y / _offset) * _offset;
            translate.x %= _offset;
            translate.y %= _offset;
            return v2;
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
        }

        private float VectorAngle(Vector2 from, Vector2 to)
        {
            float angle;

            Vector3 cross = Vector3.Cross(from, to);
            angle = Vector2.Angle(from, to);
            return cross.z > 0 ? -angle : angle;
        }

        public void RotateAround(PointerEventData eventData)
        {
            Vector2 curPoint = eventData.position -
                               (Vector2) GlobalCameraManager.Inst.UICamera.WorldToScreenPoint(recTrans.position);
            float moveAngle = VectorAngle(startDragPoint, curPoint);
            float desAngle = startAnlge - moveAngle;

            if (desAngle < 0)
            {
                desAngle += 360;
            }

            int x = (int) (desAngle / 90);
            if (desAngle % 90 > 25)
            {
                x++;
            }

            desAngle = x * 90;
            Vector3 temp = targetRecTrans.eulerAngles;
            Vector3 lastAngle = new Vector3(temp.x, temp.y, temp.z);
            float turn = temp.z - desAngle;
            int deviation = 10;
            if ((turn > deviation || turn < -deviation) && (turn + 360 > deviation || turn + 360 < -deviation))
            {
                CheckBeforeRotate(turn);
            }

            temp.z = desAngle;
            targetRecTrans.eulerAngles = temp;
            recTrans.eulerAngles = temp;
            if (!CheckIsInMask())
            {
                targetRecTrans.eulerAngles = lastAngle;
                recTrans.eulerAngles = lastAngle;
            }

        }

        private void CheckBeforeRotate(float turn)
        {
            if (((Mathf.Abs(recTrans.sizeDelta.x) / _offset) % 2) != ((Mathf.Abs(recTrans.sizeDelta.y) / _offset) % 2))
            {
                float offset = _offset / 2 * (turn > 0 ? 1 : -1);
                targetRecTrans.anchoredPosition -= new Vector2(offset, offset);
                recTrans.anchoredPosition -= new Vector2(offset, offset);
            }


        }

        public bool ignoreCheck;

        private bool CheckIsInMask()
        {
            if (ignoreCheck)
            {
                return true;
            }

            Vector3[] corners = new Vector3[4];
            recTrans.GetWorldCorners(corners);
            float deviation = 0.0001f;
            for (int i = 0; i < corners.Length; i++)
            {
                if (corners[i].x < point1.position.x - deviation || corners[i].x > point2.position.x + deviation ||
                    corners[i].y > point1.position.y + deviation || corners[i].y < point2.position.y - deviation)
                {
                    return false;
                }
            }

            return true;
        }


        private ConerType curConer;

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
                    curConer = ConerType.LT;
                    minAngle = VectorAngle(extrudeDir, lbN);
                    maxAngle = VectorAngle(extrudeDir, rtN);
                    break;
                case "RTCorner":
                    recTrans.pivot = new Vector2(0, 0);
                    targetRecTrans.pivot = new Vector2(0, 0);
                    curConer = ConerType.RT;
                    minAngle = VectorAngle(extrudeDir, ltN);
                    maxAngle = VectorAngle(extrudeDir, rbN);
                    break;
                case "LBCorner":
                    recTrans.pivot = new Vector2(1, 1);
                    targetRecTrans.pivot = new Vector2(1, 1);
                    curConer = ConerType.LD;
                    minAngle = VectorAngle(extrudeDir, rbN);
                    maxAngle = VectorAngle(extrudeDir, ltN);
                    break;
                case "RBCorner":
                    recTrans.pivot = new Vector2(0, 1);
                    targetRecTrans.pivot = new Vector2(0, 1);
                    curConer = ConerType.RD;
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
            lastVerticalPoint = cornerScreenPos;
        }

        private Vector2 curDeltaSize;

        private void SetDeailScale(PointerEventData eventData)
        {
            Vector2 size = Vector2.zero;
            Vector2 deltaSize = Vector2.zero;
            switch (curConer)
            {
                case ConerType.LT:
                    deltaSize.x = lastVerticalPoint.x - eventData.position.x;
                    deltaSize.y = eventData.position.y - lastVerticalPoint.y;
                    break;
                case ConerType.RT:
                    deltaSize = eventData.position - lastVerticalPoint;
                    break;
                case ConerType.LD:
                    deltaSize = lastVerticalPoint - eventData.position;
                    break;
                case ConerType.RD:
                    deltaSize.x = eventData.position.x - lastVerticalPoint.x;
                    deltaSize.y = lastVerticalPoint.y - eventData.position.y;
                    break;
            }

            lastVerticalPoint = eventData.position;
            curDeltaSize += deltaSize;
            var vec2 = GetOffsetMovePos(ref curDeltaSize);
            size = recTrans.sizeDelta + vec2;
            photoMinSize = _offset * 3;
            if (size.x < photoMinSize)
            {
                size.x = photoMinSize;
            }

            if (size.y < photoMinSize)
            {
                size.y = photoMinSize;
            }

            Vector2 lastSize = new Vector3(recTrans.sizeDelta.x, recTrans.sizeDelta.y);
            recTrans.sizeDelta = size;
            targetRecTrans.sizeDelta = size;
            targetCollider.size = size;
            clickArea.size = size;
            if (!CheckIsInMask())
            {
                recTrans.sizeDelta = lastSize;
                targetRecTrans.sizeDelta = lastSize;
                targetCollider.size = lastSize;
                clickArea.size = lastSize;
            }

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

        public void OnClickReplaceBtn()
        {
            OnReplace?.Invoke();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!isDrag && targetGameObject != null)
            {
                OnSelectAct?.Invoke();
            }
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
                isDetailTransform = true;
                ShowBtn(false);
                SetLineColor(Color.gray);
                detailTransform.gameObject.SetActive(true);
            }
        }

        public void HideDetailTransform()
        {
            isDetailTransform = false;
            ShowBtn(true);
            SetLineColor(Color.black);
            detailTransform.gameObject.SetActive(false);
        }

        public void ResetInfo()
        {
            if (isDetailTransform) return;

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
            OnSelectAct = null;
            OnEndDragAct = null;
            CreatUndoDataAct = null;
        }

        public Vector3[] GetPointPos()
        {
            Vector3[] coners = new Vector3[4];
            recTrans.GetWorldCorners(coners);
            return GetLeftTopPointAndRightDownPoint(coners);

        }

        //获取左上角以及右下角的点
        private Vector3[] GetLeftTopPointAndRightDownPoint(Vector3[] coners)
        {
            switch ((int) recTrans.eulerAngles.z)
            {
                case 0:
                case 360:
                default:
                    return new[] {coners[1], coners[3]};
                case 90:
                    return new[] {coners[2], coners[0]};
                case 180:
                    return new[] {coners[3], coners[1]};
                case 270:
                    return new[] {coners[0], coners[2]};
            }

        }

        public void SetConersShow(bool isShow)
        {
            LTCorner.SetActive(isShow);
            RTCorner.SetActive(isShow);
            LBCorner.SetActive(isShow);
            RBCorner.SetActive(isShow);
        }

        public void SetRotateShow(bool isShow)
        {
            rotateTopLine.SetActive(isShow);
            rotateBtn.SetActive(isShow);
        }
    }
}