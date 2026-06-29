/**
 * @ Author: Jun Zhou
 * @ Create Time: 2023-08-03 15:46:25
 * @ Modified by: Jun Zhou
 * @ Modified time: 2023-08-11 13:54:05
 * @ Description: 通用属性面板——动画
 */

using Game.Base;
using Game.Props.PropsComponents;
using Game.Props.PropsController;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.GameEdit
{
    public class UniversalAnimSubView : MonoBehaviour 
    {
        [SerializeField]private TabView rSpeedTabView;
        [SerializeField]private TabView rAxisTabView;
        [SerializeField]private TabView floatTabView;
        [SerializeField]private TabItem autoStartTab;
        [SerializeField] private PropertySwitchSubView switchView;
        [SerializeField] private PropertySensorBoxSubView sensorBoxView;

        NodeBaseBehaviour currentNodeBehv;
        int rSpeedTabSelect = 0;
        int rAxisTabSelect = 1;
        int rfloatTabSelect = 0;
        bool autoStart = true;

        private void Start() 
        {
            var curObj = GizmoManager.Inst.CurGizmoCtrl.GetCurrentTarget();
            if (curObj == null) return;

            var nodeBehav = curObj.GetComponent<NodeBaseBehaviour>();
            currentNodeBehv = nodeBehav;
            if (nodeBehav.entity.HasComp<RPAnimComponent>())
            {
                var rpAnimComponent = nodeBehav.entity.GetComp<RPAnimComponent>();
                rSpeedTabSelect = rpAnimComponent.RSpeed;
                rAxisTabSelect = rpAnimComponent.RAxis;
                rfloatTabSelect = rpAnimComponent.USpeed;
                autoStart = rpAnimComponent.AutoPlay;
            }
            rSpeedTabView.SelectWithoutCallback(rSpeedTabSelect);
            rAxisTabView.SelectWithoutCallback(rAxisTabSelect);
            floatTabView.SelectWithoutCallback(rfloatTabSelect);
            autoStartTab.SetIsSelectWithoutCallback(autoStart);

            // Event
            rSpeedTabView.AddItemSelectCallBack(OnRSpeedTabSelect);
            rAxisTabView.AddItemSelectCallBack(OnRAxisTabSelect); 
            floatTabView.AddItemSelectCallBack(OnFloatTabSelect);    
            autoStartTab.AddValueChangeCallListener(OnAutoStartSelect);
            
            switchView.SetEntity(nodeBehav.entity);
            sensorBoxView.SetEntity(nodeBehav.entity);
        }

        void NotifyChangeComponent()
        {
            if (!currentNodeBehv.entity.HasComp<RPAnimComponent>())
            {
                currentNodeBehv.entity.AddComp<RPAnimComponent>();
                RPAnimController.Inst.AddNode(currentNodeBehv);
            }

            var rpAnimComponent = currentNodeBehv.entity.GetComp<RPAnimComponent>();
            rpAnimComponent.RSpeed = rSpeedTabSelect;
            rpAnimComponent.RAxis = rAxisTabSelect;
            rpAnimComponent.USpeed = rfloatTabSelect;
            rpAnimComponent.AutoPlay = autoStart;

            if (rpAnimComponent.RSpeed == 0 && rpAnimComponent.USpeed == 0)
            {
                currentNodeBehv.entity.RemoveComp<RPAnimComponent>();
                RPAnimController.Inst.RemoveNode(currentNodeBehv);
            }
        }

        void OnRSpeedTabSelect(TabItem item, int index)
        {
            rSpeedTabSelect = index;
            NotifyChangeComponent();
        }

        void OnRAxisTabSelect(TabItem item, int index)
        {
            rAxisTabSelect = index;
            NotifyChangeComponent();
        }

        void OnFloatTabSelect(TabItem item, int index)
        {
            rfloatTabSelect = index;
            NotifyChangeComponent();
        }

        void OnAutoStartSelect(bool isOn)
        {
            autoStart = isOn;
            NotifyChangeComponent();
        }
    }
}