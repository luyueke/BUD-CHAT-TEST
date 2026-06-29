using Game.Avatar;
using UnityEngine;

namespace Game.SurfaceDetection.Base
{
    /// <summary>
    /// 检测地面射线统一管理
    /// </summary>
    public class SurfaceRaycastController
    {
        private Vector3 PlayerPos => AvatarController.Inst.GetSelfAvatarPosition();
        private readonly int _surfaceLayerMask = LayerMask.GetMask("GameSurface", "Model", "Default"); //禁止每帧调用GetMask，会产生40B GC

        private float _defaultRayRadius = 0.15f;
        private float _defaultRayCenterOffset = 0.12f;
        private Vector3 DefaultRayCenter => new Vector3(PlayerPos.x, PlayerPos.y + _defaultRayCenterOffset, PlayerPos.z);

        #region Unity Debug

        public SurfaceRaycastController()
        {
#if UNITY_EDITOR
            SurfaceRaycastDebugger.Inst.OnDrawGizmoAct = OnDrawGizmoDebug;
            SurfaceRaycastDebugger.Inst.OnDebugValueChanged = OnDebuggerValueChanged;
#endif
        }

        public void Release()
        {
#if UNITY_EDITOR
            if (SurfaceRaycastDebugger.HasInstance)
            {
                SurfaceRaycastDebugger.Inst.DestroySelf();
            }
#endif
        }

        private void OnDebuggerValueChanged(float centerOffset, float radius)
        {
            _defaultRayCenterOffset = centerOffset;
            _defaultRayRadius = radius;
        }

        private void OnDrawGizmoDebug()
        {
#if UNITY_EDITOR
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(DefaultRayCenter, _defaultRayRadius);
#endif
        }

        #endregion


        #region 正常检测

        public void OverlapSphereNonAlloc(ref Collider[] colliders)
        {
            // 水方块等Collider的Trigger=true. QueryTriggerInteraction不能使用Ignore
            Physics.OverlapSphereNonAlloc(DefaultRayCenter, _defaultRayRadius, colliders, _surfaceLayerMask, QueryTriggerInteraction.Collide);
        }

        #endregion

        #region todo:水方块检测

        #endregion
    }
}