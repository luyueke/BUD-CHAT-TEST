// @Author: YangJie
// @Description:
// @Date:  2023/08/31
// @Modify: 内购通用属性

using System;
using Game.Base;
using Game.Props.PropsComponents;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.GameEdit
{
    public class UniversalTransactionSubView : MonoBehaviour
    {
        
        
        NodeBaseBehaviour currentNodeBehv;
        TransactionComponent transactionComponent;
        private Toggle toggle;
        
        private void Awake()
        {
            toggle = GameObjectEx.FindComponentByName<Toggle>(transform, "Content/Scroll View/Viewport/Content/DefaultActive/Toggle");
            toggle.onValueChanged.AddListener(OnToggleValueChanged);
        }
        private void OnToggleValueChanged(bool arg0)
        {
            transactionComponent.isTransaction = arg0;
        }

        private void Start() 
        {
            var curObj = GizmoManager.Inst.CurGizmoCtrl.GetCurrentTarget();
            if (curObj == null) return;

            var nodeBehav = curObj.GetComponent<NodeBaseBehaviour>();
            currentNodeBehv = nodeBehav;
            transactionComponent = nodeBehav.entity.GetOrAddComp<TransactionComponent>();
            toggle.isOn = transactionComponent.isTransaction;
        }
    }
}