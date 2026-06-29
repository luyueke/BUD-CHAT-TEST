using Game;
using Game.Base;
using Game.Scene.ModeController;
using Game.Utils;
using RTG;
using UnityEngine;

namespace UI.Manager
{
    public class GizmoManager : GlobalInstance<GizmoManager>, IAutoInit
    {
        public const string RtgPrefabPath = "Assets/Arts/Prefabs/RTGApp.prefab";

        public GizmoController CurGizmoCtrl { private set; get; }


        public void Init()
        {
            InitGizmo();
        }

        public override void Release()
        {
            base.Release();
        }

        public void DestoryGizmo()
        {
            var rtg = RTGApp.Get;
            if (rtg != null)
            {
                Object.Destroy(rtg.gameObject);
            }
        }

        private void InitGizmo()
        {
            // if (!this.IsEdit()) return; //only create gizmo in edit mode

            var rtg = RTGApp.Get;
            if (rtg == null) {
                var rtgAppWrapper = Loader.Load<GameObject>(RtgPrefabPath);
                var rtgObj = rtgAppWrapper.Instantiate();
                var rtgCameraScript = rtgObj.GetComponentInChildren<RTFocusCamera>();
                rtgCameraScript.SetTargetCamera(GameCameraUtils.Inst.GetMainCamera());
                rtgObj.gameObject.SetActive(true);
            }

            if (CurGizmoCtrl == null) {
                CurGizmoCtrl = new GizmoController();
            }
            CurGizmoCtrl.Clear();
        }


    }
}
