/**
 * @ Author: Jun Zhou
 * @ Create Time: 2023-08-02 17:44:17
 * @ Modified by: Jun Zhou
 * @ Modified time: 2023-09-27 17:06:11
 * @ Description: 道具的动画
 */

using Game.Base;
using Game.Props.PropsComponents;
using Game.Utils;
using DG.Tweening;
using UnityEngine;
using Game.ECS;
using GameSync.Manager;

namespace Game.Props.PropsController
{
	public class RPAnimController : BasePropController<RPAnimController>, IAutoInit
	{
		bool IsPlayAnim = false;
		public void Init()
        {
            LoggerUtils.Log("RPAnimController.Init");
        }
		
		protected override bool Filter(NodeBaseBehaviour nodeBehaviour)
		{
			return nodeBehaviour.entity.HasComp<RPAnimComponent>();
		}

		public override void OnPlay()
        {
			LoggerUtils.Log("RPAnimController.OnPlay");
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

		public override void OnCombine(SceneEntity entity)
		{
			base.OnCombine(entity);

			// TODO UNDO REDO
			entity.RemoveComp<RPAnimComponent>();
			RemoveNode(entity.GetNodeBaseBehaviour());
		}

		/// <summary>
		/// 由开关控制调用
		/// </summary>
		public void SwitchEntity(SceneEntity entity)
		{
			var behav = entity.GetNodeBaseBehaviour();
			if (nodeBaseBehaviours.Contains(behav))
			{
				var rpComponent = entity.GetComp<RPAnimComponent>();

				if (rpComponent.TmpAnimIsRun)
				{
					rpComponent.TmpAnimNode.DOPause();
				} else {
					if (rpComponent.TmpAnimNode == null)
					{
						RealExecuteAnim(behav);
						return;
					} else {
						rpComponent.TmpAnimNode.DOPlay();
					}
				}
				rpComponent.TmpAnimIsRun = !rpComponent.TmpAnimIsRun;
			}
		}

		void StopAnim()
		{
			if (!IsPlayAnim) return;

			IsPlayAnim =  false;
			for (int i = 0; i < nodeBaseBehaviours.Count; i++)
			{
				var node = nodeBaseBehaviours[i];
				var rpComponent = node.entity.GetComp<RPAnimComponent>();
				if (rpComponent.TmpAnimNode == null) continue;

				MovableNodeHelper.RevertMovableNode(node, rpComponent.TmpOriginPosition, rpComponent.TmpOriginRotation);

				rpComponent.TmpOriginPosition = Vector3.zero;
				rpComponent.TmpOriginRotation = Vector3.zero;
				rpComponent.TmpAnimIsRun = false;
				rpComponent.TmpAnimNode = null;
			}
		}

		void ExecuteAnim()
		{
			if (IsPlayAnim) return;
			
			for (int i = 0; i < nodeBaseBehaviours.Count; i++)
			{
				var node = nodeBaseBehaviours[i];
				var rpComponent = node.entity.GetComp<RPAnimComponent>();
				if (!rpComponent.AutoPlay) continue;

				RealExecuteAnim(node);
			}
		}

		void RealExecuteAnim(NodeBaseBehaviour node)
		{
			IsPlayAnim = true;
			var rpComponent = node.entity.GetComp<RPAnimComponent>();
			rpComponent.TmpOriginPosition = node.transform.position;
			rpComponent.TmpOriginRotation = node.transform.eulerAngles;
			rpComponent.TmpAnimIsRun = true;

			var animNode = MovableNodeHelper.ReplaceMovableNode("RPAnim", node);
			rpComponent.TmpAnimNode = animNode.transform;
			
			// 旋转动画
			var rotSpeed = RPAnimConfig.RotSpeeds[rpComponent.RSpeed];
			Tween t = null;
			if (rotSpeed > 0)
			{
				var axis = Vector3.forward;
				if (rpComponent.RAxis == 0)
				{
					axis = Vector3.right;
				} else if (rpComponent.RAxis == 1)
				{
					axis = Vector3.up;
				}

				t = animNode.transform
						.DORotate(axis * 360, rotSpeed, RotateMode.FastBeyond360)
						.SetEase(Ease.Linear)
						.SetUpdate(UpdateType.Fixed)
						.SetLoops(-1, LoopType.Incremental);
				DotweenNetManager.Inst.CollectionTween(t);						
			}

			// 上下动画
			var upDuration = RPAnimConfig.UpdownDurations[rpComponent.USpeed];
			if (upDuration > 0)
			{
				var upEase = RPAnimConfig.UpEases[rpComponent.USpeed];
				var downEase = RPAnimConfig.DownEases[rpComponent.USpeed];
				var yPosition = animNode.transform.localPosition.y;
				t = animNode.transform
					.DOLocalMoveY(yPosition + 0.3f, upDuration)
					.SetEase(upEase)
					.SetUpdate(UpdateType.Fixed)
					.SetLoops(-1, LoopType.Yoyo);
				DotweenNetManager.Inst.CollectionTween(t);
			}
		}
	}
}