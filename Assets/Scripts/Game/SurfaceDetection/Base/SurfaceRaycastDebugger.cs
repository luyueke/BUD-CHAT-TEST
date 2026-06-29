using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.SurfaceDetection.Base
{
    public class SurfaceRaycastDebugger : InstMonoBehaviour<SurfaceRaycastDebugger>
    {
        public Action OnDrawGizmoAct;
        public Action<float, float> OnDebugValueChanged;

        [OnValueChanged("OnSerializeDebugValueChanged")]
        public float raycastRadius;

        [OnValueChanged("OnSerializeDebugValueChanged")]
        public float rayCenterOffset;

        private void Awake()
        {
        }

        public void DestroySelf()
        {
            GameObject.Destroy(this.gameObject);
        }

        private void OnSerializeDebugValueChanged()
        {
            OnDebugValueChanged?.Invoke(rayCenterOffset, raycastRadius);
        }

        private void OnDrawGizmos()
        {
            OnDrawGizmoAct?.Invoke();
        }
    }
}