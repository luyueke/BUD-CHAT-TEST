using Es;
using UI.Manager;
using UnityEngine;

namespace UI.EditOperation.Rules
{
    /// <summary>
    /// 轴控制规则
    /// </summary>
    public abstract class BasicAxisRule : BasicRule
    {
        public enum ControlType
        {
            Entry,
            Axis
        }

        protected GameObject EntryUI;
        protected GameObject XAxisUI;
        protected GameObject YAxisUI;
        protected GameObject ZAxisUI;

        protected bool _isInit = false;

        protected virtual void InitUI()
        {
            if (!_isInit)
            {
                EntryUI = ControlledUIList[0];
                XAxisUI = ControlledUIList[1];
                YAxisUI = ControlledUIList[2];
                ZAxisUI = ControlledUIList[3];
                _isInit = true;
            }
        }

        protected virtual void ResetUI()
        {
            SetEntryUI(true);
            SetAxisUI(true, true, true);
        }

        protected void SetEntryUI(bool isShow)
        {
            if (EntryUI) EntryUI.SetActive(isShow);
        }

        protected void SetAxisUI(bool x, bool y, bool z)
        {
            if (XAxisUI) XAxisUI.SetActive(x);
            if (YAxisUI) YAxisUI.SetActive(y);
            if (ZAxisUI) ZAxisUI.SetActive(z);
        }

        protected void SetGizmoRotate(bool xEnable, bool yEnable, bool zEnable)
        {
            var gController = GizmoManager.Inst.CurGizmoCtrl;
            if (gController == null) return;
            gController.ShowRotateAxisVisible(0, xEnable);
            gController.ShowRotateAxisVisible(1, yEnable);
            gController.ShowRotateAxisVisible(2, zEnable);
        }

        protected void SetGizmoScale(bool xEnable, bool yEnable, bool zEnable)
        {
            var gController = GizmoManager.Inst.CurGizmoCtrl;
            if (gController == null) return;
            gController.ShowScaleAxisVisible(0, xEnable);
            gController.ShowScaleAxisVisible(1, yEnable);
            gController.ShowScaleAxisVisible(2, zEnable);
        }

        protected void SetRotGizmoAndAxisUI(bool xEnable, bool yEnable, bool zEnable)
        {
            SetGizmoRotate(xEnable, yEnable, zEnable);
            SetAxisUI(xEnable, yEnable, zEnable);
        }

        protected void SetScaleGizmoAndAxisUI(bool xEnable, bool yEnable, bool zEnable)
        {
            SetGizmoScale(xEnable, yEnable, zEnable);
            SetAxisUI(xEnable, yEnable, zEnable);
        }

        protected bool IsControlAxis(params object[] otherParams)
        {
            return otherParams is { Length: > 0 } && otherParams[0] is ControlType && (ControlType)otherParams[0] == ControlType.Axis;
        }

        public override void OnTrigger(GamePropEditOperation config, params object[] otherParams)
        {
            InitUI();
            ResetUI();
        }

        public override void CalculateCombineData(GamePropEditOperation subConfig)
        {
        }
    }
}