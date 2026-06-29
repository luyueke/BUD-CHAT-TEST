using System.Reflection;
using RTG;

namespace Game {
    public static class GizmoExtensions {

        private static FieldInfo curGizmoFieldInfo;

        /// <summary>
        /// 适配屏幕分辨率
        /// </summary>
        /// <param name="gizmo"></param>
        public static void SetRit(this ObjectTransformGizmo gizmo) {

            if (curGizmoFieldInfo == null) {
                curGizmoFieldInfo = typeof(ObjectTransformGizmo).GetField("_curGizmo", BindingFlags.NonPublic | BindingFlags.Instance);
            }
            if (curGizmoFieldInfo != null)
            {
                var curGizmo = curGizmoFieldInfo.GetValue(gizmo);

                if (curGizmo is MoveGizmo mGizmo)
                {
                    mGizmo.LookAndFeel3D.SetScale(4.5f * GraphicsManager.Inst.GetCurResolutionRit());
                } else if (curGizmo is ScaleGizmo sGizmo) {
                    sGizmo.LookAndFeel3D.SetScale(4.5f * GraphicsManager.Inst.GetCurResolutionRit());
                } else if (curGizmo is RotationGizmo rGizmo) {
                    rGizmo.LookAndFeel3D.SetScale(4.5f * GraphicsManager.Inst.GetCurResolutionRit());
                }
            }
        }
    }
}
