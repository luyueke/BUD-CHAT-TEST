/**
 * @ Author: Jun Zhou
 * @ Create Time: 2023-08-02 17:44:17
 * @ Modified by: Jun Zhou
 * @ Modified time: 2023-09-27 17:02:35
 * @ Description: 道具的移动属性
 */

using Game.Base;
using Game.Props.PropsComponents;
using Game.Utils;
using DG.Tweening;
using UnityEngine;
using Game.Props.PropsManagers;
using Game.Props.PropsBehaviours;
using System.Collections.Generic;
using Game.ECS;
using GameData.BaseInfo;
using GameData.Manager;
using GameSync.Manager;

namespace Game.Props.PropsController
{
	public class MovementController : BasePropController<MovementController>, IAutoInit
	{
		bool IsPlayAnim = false;
		public void Init()
        {
            LoggerUtils.Log("MovementController.Init");
        }

		protected override bool Filter(NodeBaseBehaviour nodeBehaviour)
		{
			return nodeBehaviour.entity.HasComp<MovementComponent>();
		}

		public override void OnPlay()
        {
			LoggerUtils.Log("MovementController.OnPlay");
			ExecuteAnim();
        }

		public override void OnEdit()
        {
			StopAnim();
        }

		public override void OnGuest()
        {
			ExecuteAnim();
        }

		/// <summary>
		/// 由开关控制调用
		/// </summary>
		public void SwitchEntity(SceneEntity entity)
		{
			var behav = entity.GetNodeBaseBehaviour();
			if (nodeBaseBehaviours.Contains(behav))
			{
				var mtComponent = entity.GetComp<MovementComponent>();

				if (mtComponent.TmpAnimIsRun)
				{
					mtComponent.TmpAnimNode.DOPause();
				} else {
					if (mtComponent.TmpAnimNode == null)
					{
						RealExecuteAnim(behav);
						return;
					} else {
						mtComponent.TmpAnimNode.DOPlay();
					}
				}
				mtComponent.TmpAnimIsRun = !mtComponent.TmpAnimIsRun;
			}
		}

		void StopAnim()
		{
			if (!IsPlayAnim) return;

			IsPlayAnim =  false;
			for (int i = 0; i < nodeBaseBehaviours.Count; i++)
			{
				// 为节点添加一个动画父节点。 为了不影响原本节点的动画
				var node = nodeBaseBehaviours[i];
				var mtComponent = node.entity.GetComp<MovementComponent>();
				if (mtComponent.TmpAnimNode == null) continue;

				MovableNodeHelper.RevertMovableNode(node, mtComponent.TmpOriginPosition, mtComponent.TmpOriginRotation);

				mtComponent.TmpOriginPosition = Vector3.zero;
				mtComponent.TmpOriginRotation = Vector3.zero;
				mtComponent.TmpAnimIsRun = false;
				mtComponent.TmpAnimNode = null;
			}
		}

		void ExecuteAnim()
		{
			if (IsPlayAnim) return;

			for (int i = 0; i < nodeBaseBehaviours.Count; i++)
			{
				// 为节点添加一个动画父节点。 为了不影响原本节点的动画
				var node = nodeBaseBehaviours[i];
				var mtComponent = node.entity.GetComp<MovementComponent>();
				if (!mtComponent.AutoPlay) continue;

				RealExecuteAnim(node);
			}
		}

		void RealExecuteAnim(NodeBaseBehaviour node,bool isSyncOnline = true)
		{
			IsPlayAnim = true;
			var mtComponent = node.entity.GetComp<MovementComponent>();
			var speed = MovmentConfig.MoveSpeed[mtComponent.SpeedLv];
			float turnTime = MovmentConfig.TurnAroundTime[mtComponent.SpeedLv];
			bool isTurnAround = mtComponent.TurnAround;
			// 设置了选择，掉头功能将会失效
			if (node.entity.TryGetComp<RPAnimComponent>(out var rPAnimComponent))
			{
				isTurnAround = rPAnimComponent.RSpeed == 0;
			}
			var pathPoints = new List<Vector3>(mtComponent.PathPoints);
			pathPoints.Insert(0, node.transform.position);
			var duration = CalculateMoveTime(pathPoints, speed);

			var animNode = MovableNodeHelper.ReplaceMovableNode("MovementAnim", node);
			mtComponent.TmpOriginPosition = node.transform.position;
			mtComponent.TmpOriginRotation = node.transform.eulerAngles;
			mtComponent.TmpAnimIsRun = true;
			mtComponent.TmpAnimNode = animNode.transform;

			MovableNodeHelper.AddMovingPlatformMono(node);
			int increaseStep = 1;
			Tween t = animNode.transform.DOPath(mtComponent.PathPoints.ToArray(), duration)
				.SetEase(Ease.Linear)
				.SetLoops(-1, LoopType.Yoyo)
				.SetUpdate(UpdateType.Fixed)
				.OnWaypointChange((index) =>
				{
					if (isTurnAround)
					{
						if ((index + increaseStep) >= pathPoints.Count)
						{
							increaseStep = -1;
						} else if ((index + increaseStep) < 0){
							increaseStep = 1;
						}
						var nextIndex = index + increaseStep;
						// LoggerUtils.Log($"OnWaypointChange {index} {nextIndex}");
						animNode.transform.DOLookAt(pathPoints[nextIndex], turnTime);
					}
				});
			if (isSyncOnline)
			{
				DotweenNetManager.Inst.CollectionTween(t);
			}
		}

		float CalculateMoveTime(List<Vector3> allPoints,float speed)
		{
			float distance = 0;
			for (int i = 1; i < allPoints.Count; i++)
			{
				distance += (allPoints[i] - allPoints[i - 1]).magnitude;
			}

			return distance / speed;
		}

		public override void OnSelectNode(NodeBaseBehaviour nodeBehaviour)
		{
			base.OnSelectNode(nodeBehaviour);

			if (Filter(nodeBehaviour))
			{
				var mpManager = GlobalNodeManager.Inst.Get<MovePointManager>();
				mpManager.CloseAndSave();
				mpManager.InitNodes(nodeBehaviour);
			} else if (!(nodeBehaviour is MovePointBehaviour))
			{
				var mpManager = GlobalNodeManager.Inst.Get<MovePointManager>();
				mpManager.CloseAndSave();
			}
		}

        public override void OnCreateNode(NodeBaseBehaviour nodeBehaviour, NodeCreateType createType) {
            base.OnCreateNode(nodeBehaviour, createType);
            var curMapInfo = GameDataManager.Inst.mapGlobalData.GetCurInfo<MapInfo>();
            if (nodeBehaviour.entity.TryGetComp<MovementComponent>(out var movementComponent)) {
                if (createType == NodeCreateType.SceneBuild && curMapInfo.migrateData == 1 && !movementComponent.isMigrated) {
                    // 兼容海外数据
                    movementComponent.SpeedLv -= 1;
                }
                movementComponent.isMigrated = true;
            }

        }

        public override void OnUnSelectAll()
		{
			base.OnUnSelectAll();

			var mpManager = GlobalNodeManager.Inst.Get<MovePointManager>();
			mpManager.CloseAndSave();
		}

		public override void OnRemoveNode(NodeBaseBehaviour nodeBehaviour)
		{
			base.OnRemoveNode(nodeBehaviour);

			if (Filter(nodeBehaviour))
			{
				var mpManager = GlobalNodeManager.Inst.Get<MovePointManager>();
				mpManager.CloseAndSave();
			}
		}


        public override void OnCombine(SceneEntity entity)
		{
			base.OnCombine(entity);

			// TODO UNDO REDO
			entity.RemoveComp<MovementComponent>();
			RemoveNode(entity.GetNodeBaseBehaviour());
		}

		public void OnDragMoveEnd(GameObject target)
		{
			var targetBehv = target.GetComponent<NodeBaseBehaviour>();
			if (targetBehv != null && (targetBehv is MovePointBehaviour)) // 移动的是一个路点
			{
				SavePathData();
			}
		}

		// 保存一下当前的锚点位置
		void SavePathData()
		{
			var mpManager = GlobalNodeManager.Inst.Get<MovePointManager>();
			mpManager.Save();
		}
	}
}
