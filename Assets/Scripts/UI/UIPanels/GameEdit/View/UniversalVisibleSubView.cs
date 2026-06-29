/**
 * @ Author: Jun Zhou
 * @ Create Time: 2023-08-03 15:46:25
 * @ Modified by: Jun Zhou
 * @ Modified time: 2023-08-08 17:25:59
 * @ Description: 通用属性面板——动画
 */

using Game.Base;
using Game.Props.PropsComponents;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.GameEdit
{
    public class UniversalVisibleSubView : MonoBehaviour 
    {
        [SerializeField]private TabView DefaultActiveView;
        [SerializeField] private PropertySwitchSubView switchView;
        [SerializeField] private PropertySensorBoxSubView sensorBoxView;

        NodeBaseBehaviour currentNodeBehv;
        int defaultActiveIndex = 0;
        
        private void Start() 
        {
            var curObj = GizmoManager.Inst.CurGizmoCtrl.GetCurrentTarget();
            if (curObj == null) return;

            var nodeBehav = curObj.GetComponent<NodeBaseBehaviour>();
            currentNodeBehv = nodeBehav;
            if (nodeBehav.entity.HasComp<ActiveCtrComponent>())
            {
                var activeCtrComponent = nodeBehav.entity.GetComp<ActiveCtrComponent>();
                if (activeCtrComponent.DefaultHide == 1)
                {
                    defaultActiveIndex = 1;
                }
            }
            DefaultActiveView.SelectWithoutCallback(defaultActiveIndex);

            // Event
            DefaultActiveView.AddItemSelectCallBack(OnActiveTabSelect);
            
            switchView.SetEntity(nodeBehav.entity);
            sensorBoxView.SetEntity(nodeBehav.entity);
        }

        void NotifyChangeComponent()
        {
            var activeCtrComponent = currentNodeBehv.entity.GetComp<ActiveCtrComponent>();
            if (defaultActiveIndex == 1)
            {
                if (activeCtrComponent == null)
                {
                    activeCtrComponent = currentNodeBehv.entity.AddComp<ActiveCtrComponent>();
                }

                activeCtrComponent.DefaultHide = 1;
                ActiveCtrController.Inst.AddNode(currentNodeBehv);
            }
            else
            {
                if (activeCtrComponent != null)
                {
                    activeCtrComponent.DefaultHide = 0;
                }
                currentNodeBehv.entity.RemoveComp<ActiveCtrComponent>();
                ActiveCtrController.Inst.RemoveNode(currentNodeBehv);
            }
        }

        void OnActiveTabSelect(TabItem item, int index)
        {
            defaultActiveIndex = index;
            NotifyChangeComponent();
        }
        
    }
}