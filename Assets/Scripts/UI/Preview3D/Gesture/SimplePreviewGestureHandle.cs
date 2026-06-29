using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI.Preview3D.Gesture
{
    public class SimplePreviewGestureHandle : MonoBehaviour, IDragHandler
    {

        public Transform modelRootTrans;
        public Transform clickArea;

        public bool enable = false;

        public void SetAvatarTransform(Transform aTrans)
        {
            this.modelRootTrans = aTrans;
        }
        
        public void SetClickAreaTransform(Transform clickAreaTrans)
        {
            this.clickArea = clickAreaTrans;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!enable)
            {
                return;
            }
            
            GameObject selectObj = EventSystem.current.currentSelectedGameObject;
            if (!selectObj || !clickArea) return;
            if (selectObj.name != clickArea.name) return;
            
            Vector2 move = 0.3f * Input.GetTouch(0).deltaPosition;
            modelRootTrans.Rotate(Vector3.up, -move.x, Space.World);
        }

        public void ResetRotation()
        {
            if (modelRootTrans)
            {
                modelRootTrans.DOLocalRotate(Vector3.zero, 0.2f);
            }
        }

        public void SetEnable(bool enablePara)
        {
            this.enable = enablePara;
        }
    }
}
