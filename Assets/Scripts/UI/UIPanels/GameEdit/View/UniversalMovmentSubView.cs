/**
 * @ Author: Jun Zhou
 * @ Create Time: 2023-08-03 15:46:25
 * @ Modified by: Jun Zhou
 * @ Modified time: 2023-08-14 10:27:42
 * @ Description: 通用属性面板——动画
 */

using Game.Base;
using Game.Props.PropsComponents;
using Game.Props.PropsController;
using Game.Props.PropsManagers;
using UI.BaseWidgets;
using UI.Manager;
using UI.UIWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.GameEdit
{
    public class UniversalMovmentSubView : MonoBehaviour 
    {
        [SerializeField]private TabView moveMode;
        [SerializeField]private TabItem autoStartTab;
        [Header("移动速度")]
        [SerializeField]private TabView moveSpeedTabView;
        [Header("锚点移动")]
        [SerializeField]private GameObject pathMoveOptionGo; // 锚点移动的选项内容
        [SerializeField]private NumControlText numControlText;
        [SerializeField]private TabItem autoTurnAroundTab;
        [SerializeField] private PropertySwitchSubView switchView;
        [SerializeField] private PropertySensorBoxSubView sensorBoxView;

        NodeBaseBehaviour currentNodeBehv;
        
        private void Start() 
        {
            autoTurnAroundTab.Init();
            autoStartTab.Init();
            moveMode.AddItemSelectCallBack(OnMoveModeSelect);
            moveSpeedTabView.AddItemSelectCallBack(OnMoveSpeedSelect);
            autoStartTab.AddValueChangeCallListener(OnAutoStartSelect);
            autoTurnAroundTab.AddValueChangeCallListener(OnAutoTurnAroundSelect);
            numControlText.AddOnAddListener(OnAddBtnClick);
            numControlText.AddOnSubListener(OnSubBtnClick);

            var curObj = GizmoManager.Inst.CurGizmoCtrl.GetCurrentTarget();
            if (curObj == null) return;
            var nodeBehav = curObj.GetComponent<NodeBaseBehaviour>();
            currentNodeBehv = nodeBehav;
            switchView.SetEntity(nodeBehav.entity);
            sensorBoxView.SetEntity(nodeBehav.entity);

            InitSelect();
        }

        void InitSelect()
        {
            int index = 0;
            pathMoveOptionGo.SetActive(false);
            autoTurnAroundTab.SetIsSelectWithoutCallback(true);
            if (currentNodeBehv.entity.TryGetComp<MovementComponent>(out var movementComponent))
            {
                index = 2;
                pathMoveOptionGo.SetActive(true);
                numControlText.SetNum(movementComponent.PathPoints.Count);
                moveSpeedTabView.SelectWithoutCallback(movementComponent.SpeedLv);
                autoStartTab.SetIsSelectWithoutCallback(movementComponent.AutoPlay);
                autoTurnAroundTab.SetIsSelectWithoutCallback(movementComponent.TurnAround);
            }
            moveMode.SelectWithoutCallback(index); // 选择模式
            moveSpeedTabView.gameObject.SetActive(index != 0); // 移动速度
        }

        void OnMoveModeSelect(TabItem item, int index)
        {
            var isPathMoveMode = index == 2; // 锚点移动
            if (isPathMoveMode)
            {
                if (!currentNodeBehv.entity.HasComp<MovementComponent>())
                {
                    currentNodeBehv.entity.AddComp<MovementComponent>();
                    MovementController.Inst.AddNode(currentNodeBehv);
                }
                var movementComp = currentNodeBehv.entity.GetComp<MovementComponent>();
                var mpManager = GlobalNodeManager.Inst.Get<MovePointManager>();
                if (movementComp.PathPoints.Count > 0) // 初始化已有节点
                {
                    mpManager.InitNodes(currentNodeBehv);
                } else { // 初始化默认节点
                    var pathNode = mpManager.CreateNodeDefault(currentNodeBehv);
                    InputHandlerManager.Inst.SelectEntity(pathNode.entity);
                }

                numControlText.SetNum(movementComp.PathPoints.Count);
            } else {
                currentNodeBehv.entity.RemoveComp<MovementComponent>();
                MovementController.Inst.RemoveNode(currentNodeBehv);
                GlobalNodeManager.Inst.Get<MovePointManager>().ClearAll();
            }
            pathMoveOptionGo.SetActive(index == 2); // 锚点移动
            moveSpeedTabView.gameObject.SetActive(index != 0); // 移动速度
        }

        void OnMoveSpeedSelect(TabItem item, int index)
        {
            if(currentNodeBehv.entity.TryGetComp<MovementComponent>(out var movementComponent))
            {
                movementComponent.SpeedLv = index;
            }
        }

        void OnAutoStartSelect(bool isOn)
        {
            if(currentNodeBehv.entity.TryGetComp<MovementComponent>(out var movementComponent))
            {
                movementComponent.AutoPlay = isOn;
            }
        }

        void OnAutoTurnAroundSelect(bool isOn)
        {
            if(currentNodeBehv.entity.TryGetComp<MovementComponent>(out var movementComponent))
            {
                movementComponent.TurnAround = isOn;
            }
        }

        void OnAddBtnClick(int count)
        {
            var mpManager = GlobalNodeManager.Inst.Get<MovePointManager>();
            var pathNode = mpManager.CreateNodeDefault(currentNodeBehv);
            InputHandlerManager.Inst.SelectEntity(pathNode.entity);
        }

        void OnSubBtnClick(int count)
        {
            var mpManager = GlobalNodeManager.Inst.Get<MovePointManager>();
            mpManager.RemoveNode(currentNodeBehv);
        }
    }
}