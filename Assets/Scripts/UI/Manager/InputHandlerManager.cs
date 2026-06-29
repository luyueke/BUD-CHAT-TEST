using System;
using Es;
using Game.Base;
using Game.ECS;
using Game.Scene.ModeController;
using Game.Utils;
using GameData.BaseInfo;
using GameData.Manager;
using Message;
using UI.EditOperation;
using UIAgent;
using UnityEditor;
using UnityEngine;

namespace UI.Manager
{
    public class InputHandlerManager : GameInstance<InputHandlerManager>, IAutoInit
    {
        private float maxCamDist = 350;
        private InputHandler CurInputHandler;
        private Action OnUnSelectAll;
        private Action<SceneEntity> OnSelectedEntity;
        private Action<RaycastHit[]> OnSelectedNode;
        private Action<GameObject> OnSelectObj;
        private int banSelectCount = 0;

        private SceneEntity lastSelectEntity;

        public void Init()
        {
            InitInputHandler();
        }

        public override void Release()
        {
            base.Release();
            InputReceiver.Inst.SetHandle(null);
            lastSelectEntity = null;
            CurInputHandler = null;
            OnUnSelectAll = null;
            OnSelectedEntity = null;
            OnSelectObj = null;
            if (UIAgentManager.HasInstance)
            {
                UIAgentManager.Inst.SelectGoMethodEventHandler -= SelectGoByAgent; 
            }
        }

        private void InitInputHandler()
        {
            if (this.IsAnimPoseEdit())
            {
                var editInputHandler = new EditModeHandler();
                editInputHandler.InitCamera();
                editInputHandler.AddClickScreenCallback(OnSelectJointClick);
                CurInputHandler = editInputHandler;
                InputReceiver.Inst.SetHandle(CurInputHandler);
                return;
            }

            if (this.IsEdit())
            {
                var editInputHandler = new EditModeHandler();
                editInputHandler.InitCamera();
                editInputHandler.AddClickScreenCallback(OnClickScreen);
                CurInputHandler = editInputHandler;
                InputReceiver.Inst.SetHandle(CurInputHandler);

                UIAgentManager.Inst.SelectGoMethodEventHandler += SelectGoByAgent;
            }
            else if (this.IsPlay())
            {
                //todo:fsc PlayModeHandler 暂未接入，暂使用EditHandler处理手势操作
                var editInputHandler = new EditModeHandler();
                editInputHandler.SetCamera(GameCameraUtils.Inst.GetMainCamera(), GameCameraUtils.Inst.GetPlayVirtualCamera());
                CurInputHandler = editInputHandler;
                InputReceiver.Inst.SetHandle(CurInputHandler);
            }
            else if(this.IsGuest())
            {
                var editInputHandler = new PlayModeHandler();
                editInputHandler.InitCamera();
            }
            else if(this.IsAIGuest())
            {
                var editInputHandler = new PlayModeHandler();
                editInputHandler.InitCamera();
            }
        }

        public void SetIsCanSelect(bool value)
        {
            if (value == false)
            {
                banSelectCount++;
            }
            else
            {
                banSelectCount--;
                if (banSelectCount < 0)
                {
                    banSelectCount = 0;
                }
            }
        }

        public void OnSelectJointClick(Touch touch)
        {
            Ray ray = GameCameraUtils.Inst.GetMainCamera().ScreenPointToRay(touch.position); 
            var raycasts = Physics.RaycastAll(ray, 2 * maxCamDist, 1 << LayerMask.NameToLayer("Model") | 1 << LayerMask.NameToLayer("ShotExclude"));
            if (raycasts.Length != 0)
            {
                for (var i = 0; i < raycasts.Length; i++)
                {
                    OnSelectedNode?.Invoke(raycasts);
                }
            }
            else
            {
                OnUnSelectAll?.Invoke();
            }
        }
        
        
        public void OnClickScreen(Touch touch)
        {
            if (banSelectCount > 0)
            {
                return;
            }

            // LoggerUtils.Log($"{TAG}: OnClickScreen ");
            Ray ray = GameCameraUtils.Inst.GetMainCamera().ScreenPointToRay(touch.position);

            //"SpecialModel" pack model visible,camera cover invisble 
            bool isHit = Physics.Raycast(ray, out RaycastHit hit, 2 * maxCamDist,
                1 << LayerMask.NameToLayer("Model")
                | 1 << LayerMask.NameToLayer("ShotExclude")
                | 1 << LayerMask.NameToLayer("GameSurface")
                // | 1 << LayerMask.NameToLayer("TriggerModel")
                // | 1 << LayerMask.NameToLayer("Touch")
                // | 1 << LayerMask.NameToLayer("PVPArea")
                // | 1 << LayerMask.NameToLayer("WaterCube")
                // | 1 << LayerMask.NameToLayer("IceCube")
            ); //todo:层级清理

            if (!isHit)
            {
                UnSelectAll();
                return;
            }

            // 默认地面的Layer=GameSurface，Tag=DefaultGround
            var go = hit.collider.gameObject;
            if (go.CompareTag("DefaultGround"))
            {
                UnSelectAll();
                return;
            }
            
            var nodeBehav = go.GetComponentInParent<NodeBaseBehaviour>();
            if(this.IsVehicleEdit() && nodeBehav == null && go != null)
            {
                if(VehicleDataManager.Inst.IsSelectDrivePos)
                {
                    OnSelectObj?.Invoke(go);
                }
                else
                {
                    UnSelectAll();
                }
                return;
            }
            var entity = GamePropUtils.GetCanControllerNode(nodeBehav.gameObject);
            if (entity != null)
            {
                if (lastSelectEntity != null && lastSelectEntity != entity)
                {
                    OnPropUnSelected(lastSelectEntity);
                }
                lastSelectEntity = entity;
                SelectEntity(entity);
            }
        }

        public void SetNodeEntitySelect(NodeBaseBehaviour node)
        {
            var entity = GamePropUtils.GetCanControllerNode(node.gameObject);
            if (entity != null)
            {
                if (lastSelectEntity != null && lastSelectEntity != entity)
                {
                    OnPropUnSelected(lastSelectEntity);
                }
                lastSelectEntity = entity;
                SelectEntity(entity);
            }
        }

        #region Select & UnSelect

        void SelectGoByAgent(GameObject go)
        {
            var nodeBehav = go.GetComponent<NodeBaseBehaviour>();
            if (nodeBehav != null)
            {
                SelectEntity(nodeBehav.entity);
            }
        }

        public void SelectEntity(SceneEntity entity,bool isRefresh = false)
        {
#if UNITY_EDITOR
            var curBehav = entity.GetNodeBaseBehaviour();
            var bevObj = curBehav.gameObject;
            Selection.SetActiveObjectWithContext(bevObj, bevObj);
#endif
            if (entity == EditOperationManager.Inst.CurrentSelectEntity && isRefresh == false)
            {
                return;
            }

            //载具中点击人物要判断是否在驾驶位置选择中
            //if (GameDataManager.Inst.mapGlobalData?.GetCurInfo<VehicleInfo>() != null)
            //{
            //    GameObjectComponent objCmp = null;
            //    if (entity.TryGetComp(out objCmp))
            //    {
            //        if (objCmp.ModelType == GameData.Config.NodeModelType.PreviewModel && !VehicleDataManager.Inst.IsSelectDrivePos)
            //        {
            //            UnSelectAll();
            //            return;
            //        }
            //        else if (objCmp.ModelType != GameData.Config.NodeModelType.PreviewModel && VehicleDataManager.Inst.IsSelectDrivePos)
            //        {
            //            MessageHelper.Broadcast(MessageName.OnTouchForVehiclePerson);
            //            return;
            //        }
            //    }
            //}

            EditOperationManager.Inst.CurrentSelectEntity = entity;
            OnPropSelected(entity);
            OnSelectedEntity?.Invoke(entity);
        }

        public void UnSelectAll()
        {
            OnUnSelectAll?.Invoke();
            InterfaceNotifyUtil.OnUnSelectAll();
            if (lastSelectEntity != null)
            {
                OnPropUnSelected(lastSelectEntity);
            }

            EditOperationManager.Inst.CurrentSelectEntity = null;
        }

        public void AddSelectObjListener(Action<GameObject> callback)
        {
            OnSelectObj += callback;
        }

        public void RemoveSelectObjListener(Action<GameObject> callback)
        {
            OnSelectObj -= callback;
        }

        public void AddSelectEntityListener(Action<SceneEntity> callback)
        {
            OnSelectedEntity += callback;
        }

        public void RemoveSelectEntityListener(Action<SceneEntity> callback)
        {
            OnSelectedEntity -= callback;
        }

        public void AddSelectNodeListener(Action<RaycastHit[]> callback)
        {
            OnSelectedNode += callback;
        }

        public void RemoveSelectNodeListener(Action<RaycastHit[]> callback)
        {
            OnSelectedNode -= callback;
        }

        
        public void AddUnSelectAllListener(Action callback)
        {
            OnUnSelectAll += callback;
        }

        public void RemoveUnSelectAllListener(Action callback)
        {
            OnUnSelectAll -= callback;
        }

        #endregion

        #region 选中道具操作

        private void OnPropSelected(SceneEntity entity)
        {
            InterfaceNotifyUtil.OnSelectNode(entity.GetNodeBaseBehaviour());
            var nodeMgr = GlobalNodeManager.Inst.GetNodeEditManager(entity);
            if (nodeMgr == null) return;
            nodeMgr.OnSelectProp(entity);
        }

        private void OnPropUnSelected(SceneEntity entity)
        {
            InterfaceNotifyUtil.OnUnSelectNode(entity.GetNodeBaseBehaviour());
            var nodeMgr = GlobalNodeManager.Inst.GetNodeEditManager(entity);
            if (nodeMgr == null) return;
            nodeMgr.OnUnSelectProp(entity);
        }

        #endregion


        /// <summary>
        /// 场景相机视角，仅编辑模式下可用
        /// </summary>
        public void SetSceneGizmoRotation(Quaternion targetRotation)
        {
            if (CurInputHandler is EditModeHandler)
            {
                var editModeHandler = CurInputHandler as EditModeHandler;
                editModeHandler.OnSceneGizmoHandlePicked(targetRotation);
            }
        }
    }
}