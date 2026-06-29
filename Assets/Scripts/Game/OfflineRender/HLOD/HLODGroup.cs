// @Author: YangJie
// @Description:
// @Date:  2023/09/27
// @Modify:

using System;
using System.Collections.Generic;
using System.Linq;
using Game.Base;
using Game.ECS;
using Game.OfflineRender;
using Unity.Collections;
using UnityEngine;

namespace HLOD
{
    public class HLODGroup : MonoBehaviour
    {
        private Dictionary<string, Mesh> highLodMeshes;

        private Dictionary<string, Mesh> lowLodMeshes;

        [SerializeField] private MeshFilter[] meshFilters;
        [SerializeField] private MeshRenderer[] meshRenderers;

        [SerializeField]
        [ReadOnly]
        private ModelLODType curLODType = ModelLODType.CULL;
        public ModelLODType CurLODType
        {
            get => curLODType;
            set
            {
                if ((curLODType == ModelLODType.NONE || curLODType == ModelLODType.CULL) && (value != ModelLODType.CULL && value != ModelLODType.NONE) && !isAssetLoaded)
                {
                    if (hlodNode != null)
                    {
                        hlodNode.onAssetFirstLoaded?.Invoke();
                        hlodNode.onAssetFirstLoaded = null;
                    }
                    isAssetLoaded = true;
                }
                curLODType = value;
                loadingLODType = ModelLODType.NONE;
            }
        }
        [ReadOnly]
        public ModelLODType targetLODType = ModelLODType.CULL;

        [ReadOnly]
        public ModelLODType loadingLODType = ModelLODType.NONE;

        public bool isSettingHighMesh = false;
        private bool isInit = false;

        private Bounds? localBounds;
        public Bounds bounds;



        private IHLODNode hlodNode;

        public bool isAssetLoaded = false;


        public string gridKey;



        public void Init(IHLODNode node)
        {
            hlodNode = node;
        }

        public void OnReset()
        {
            curLODType = ModelLODType.CULL;
        }

        public void Refresh() {
            bounds = gameObject.GetBounds();
        }

        public void ChangeLODStatus(ModelLODType lodType)
        {
            targetLODType = lodType;
            if (curLODType == targetLODType || loadingLODType == targetLODType)
            {
                return;
            }

            if (hlodNode == null)
            {
                return;
            }
            loadingLODType = targetLODType;
            switch (targetLODType)
            {
                case ModelLODType.CULL:
                    hlodNode.ChangeToCull();
                    break;
                case ModelLODType.LOW:
                    hlodNode.ChangeToLow();
                    break;
                case ModelLODType.HIGH:
                    hlodNode.ChangeToHigh();
                    break;
            }
        }

        /// <summary>
        /// 判断是否可以分块
        /// </summary>
        /// <returns></returns>

        public bool IsValid()
        {
            if (!hlodNode.isEnabled)
            {
                return false;
            }

            var behaviour = GetComponent<NodeBaseBehaviour>();
            var comp = behaviour.entity.GetComp<GameObjectComponent>();
            if (comp == null) {
                return false;
            }
            return !IsMoving(behaviour) && IsNormalNode(behaviour);
        }


        /// <summary>
        /// 判断节点是否是移动节点, 仅位置移动
        /// </summary>
        /// <param name="behaviour"></param>
        /// <returns></returns>
        private bool IsMoving(NodeBaseBehaviour behaviour)
        {
            if (behaviour == null)
            {
                return false;
            }
            var isMoving = CheckMovingParent(behaviour) || CheckMovingSelf(behaviour);
            return isMoving;
        }


        // 是否是组合下节点或单节点
        private bool IsNormalNode(NodeBaseBehaviour behaviour)
        {

            var gameObjectComponent = behaviour.entity.GetComp<GameObjectComponent>();

            if (!HLODManager.Inst.lodNodeModels.Contains(gameObjectComponent.ModelType))
            {
                return false;
            }

            if (behaviour.transform.parent == null)
            {
                return true;
            }
            var parentBehaviour = behaviour.transform.parent.GetComponentInParent<NodeBaseBehaviour>();
            if (parentBehaviour == null)
            {
                // 单节点
                return true;
            }

            return false;
        }

        private bool CheckMovingSelf(NodeBaseBehaviour behaviour)
        {
            return behaviour.entity.Components.Any(tmp => tmp.Value is IMovedComponent moveComponent && moveComponent.IsMoved());
        }

        private bool CheckMovingParent(NodeBaseBehaviour behaviour)
        {
            if (behaviour == null || behaviour.transform.parent == null)
            {
                return false;
            }
            var parentBehaviour = behaviour.transform.parent.GetComponentInParent<NodeBaseBehaviour>();
            return parentBehaviour != null && (IsMoving(parentBehaviour));
        }

        public Bounds GetBounds()
        {
            if (bounds == default)
            {
                Refresh();
            }
            return bounds;
        }
    }
}
