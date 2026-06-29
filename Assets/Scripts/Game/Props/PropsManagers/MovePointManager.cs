/**
 * @ Author: Jun Zhou
 * @ Create Time: 2023-08-04 11:44:57
 * @ Modified by: Jun Zhou
 * @ Modified time: 2023-08-07 18:22:40
 * @ Description: 道具的可编辑路点的辅助道具
 */


using System.Collections.Generic;
using Game.Base;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using Game.Utils;
using GameData.Config;
using System.Linq;
using UnityEngine;

namespace Game.Props.PropsManagers
{
    [NodeBehaviourAttribute(typeof(MovePointBehaviour))]
	public class MovePointManager : BaseNodeManager
	{
		Es.GamePropData movePointGameData;
		// 当前设置路点的节点对象
		NodeBaseBehaviour currentTargetNode;
		// 用来缓存节点。 这个辅助道具不会真正删除。防止重复创建
		protected List<MovePointBehaviour> cache = new List<MovePointBehaviour>(); 
		public MovePointManager()
		{
			movePointGameData = GamePropDataHelper.GetPropIdByNodeModelType(NodeModelType.MovePoint);
			if (movePointGameData == null)
			{
				LoggerUtils.LogError("MovePointManager Not Find Move Point Config.");
			}
		}

		protected override void OnNotifyCreateInClone(NodeBaseBehaviour oldBehaviour, NodeBaseBehaviour newBehaviour)
		{
			base.OnNotifyCreateInClone(oldBehaviour, newBehaviour);
			var movePointBehav = newBehaviour as MovePointBehaviour;
			if (movePointBehav != null)
			{
				var oldPointBehav = oldBehaviour as MovePointBehaviour;
				var ctrNode = currentTargetNode;
				if (oldPointBehav != null && oldPointBehav.GetControlNode() != null)
				{
					ctrNode = oldPointBehav.GetControlNode();
				}
				AddPointControlNode(ctrNode,movePointBehav);
			}
		}
		
		//因为属性内没有做undo redo,所以暂不处理 创建和删除的undo / redo ，否则会出现不一致的问题
		// protected override void OnNotifyRemove(NodeBaseBehaviour nodeBehaviour)
		// {
		// 	base.OnNotifyRemove(nodeBehaviour);
		// 	var movePointBehav = nodeBehaviour as MovePointBehaviour;
		// 	if (movePointBehav != null && movePointBehav.GetControlNode() != null)
		// 	{
		// 		RemoveNode(movePointBehav.GetControlNode());
		// 	}
		// }
		//
		// protected override void OnNotifyRevert(NodeBaseBehaviour nodeBehaviour)
		// {
		// 	base.OnNotifyRevert(nodeBehaviour);
		// 	var movePointBehav = nodeBehaviour as MovePointBehaviour;
		// 	if (movePointBehav != null && movePointBehav.GetControlNode() != null)
		// 	{
		// 		AddPointControlNode(movePointBehav.GetControlNode(),movePointBehav);
		// 	}
		// }
		

		/// <summary>
		/// 初始化Node节点的所有路点
		/// </summary>
		public void InitNodes(NodeBaseBehaviour node)
		{
			currentTargetNode = node;
			var lastTarget = node.transform;
			var movementComp = node.entity.GetOrAddComp<MovementComponent>();
			// 初始化已有节点
			for (int i = 0; i < movementComp.PathPoints.Count; i++)
			{
				var pathNode = CreateNode();
				pathNode.transform.position = movementComp.PathPoints[i];
				pathNode.SetControlNode(node);
				pathNode.SetText(i + 1);
				pathNode.SetTarget(lastTarget);
				movementComp.PathPointTFCache.Add(pathNode.transform);
				lastTarget = pathNode.transform;
			}
		}

		/// <summary>
		/// 创建一个默认节点 
		/// </summary>
		public MovePointBehaviour CreateNodeDefault(NodeBaseBehaviour node)
		{
			var movementComp = node.entity.GetOrAddComp<MovementComponent>();
			var defaultPosition = node.transform.position;
			var pathNode = CreateNode();
			pathNode.transform.position = defaultPosition;
			AddPointControlNode(node,pathNode);
			return pathNode;
		}

		private void AddPointControlNode(NodeBaseBehaviour node,MovePointBehaviour pathNode)
		{
			var movementComp = node.entity.GetOrAddComp<MovementComponent>();
			pathNode.SetControlNode(node);
			pathNode.SetText(movementComp.PathPoints.Count + 1);
			if (movementComp.PathPointTFCache.Count == 0) // 第一个路点
			{
				currentTargetNode = node;
				pathNode.SetTarget(node.transform);
			} else {
				pathNode.SetTarget(movementComp.PathPointTFCache.Last());
			}
			movementComp.PathPointTFCache.Add(pathNode.transform);
			Save();
		}

		/// <summary>
		/// 创建一个节点
		/// </summary>
		MovePointBehaviour CreateNode()
		{
			MovePointBehaviour behaviour;
			if (cache.Count > 0)
			{
				behaviour = cache[0];
				behaviour.gameObject.SetActive(true);
				entities.Add(behaviour);
				cache.Remove(behaviour);
			} else {
				NodeBaseBehaviour newNode;
				GamePropNodeManager.Inst.TryCreateInEdit(movePointGameData.Id, out newNode);
				newNode.transform.SetParent(SceneBuilder.Inst.EditStageParent);
				behaviour = newNode as MovePointBehaviour;
			}

			return behaviour;
		}

		/// <summary>
		/// 默认移除之后一个
		/// </summary>
		public void RemoveNode(NodeBaseBehaviour node)
		{
			var movementComp = node.entity.GetOrAddComp<MovementComponent>();
			if (movementComp.PathPointTFCache.Count > 0)
			{
				var lastTarget = movementComp.PathPointTFCache.Last();
				var pathBehv = lastTarget.GetComponent<NodeBaseBehaviour>();
				var index = entities.IndexOf(pathBehv);
				
				if (index >= 0)
				{
					entities[index].gameObject.SetActive(false);
					var pointBehav = pathBehv as MovePointBehaviour;
					cache.Add(pointBehav);
					entities.RemoveAt(index);
				}
				movementComp.PathPointTFCache.Remove(lastTarget);

				Save();
			}
		}

		public void CloseAndSave()
		{
			Save();
			ClearAll();
			currentTargetNode = null;
		}

		public void ClearAll()
		{
			if (currentTargetNode != null)
			{
				currentTargetNode.entity.GetComp<MovementComponent>()?.PathPointTFCache.Clear();
			}

			entities.ForEach(x=>{
				if (x is MovePointBehaviour behaviour)
				{
					behaviour.Clear();
					behaviour.gameObject.SetActive(false);
					cache.Add(behaviour);
				}
			});

			entities.Clear();
		}

		public void Save()
		{
			if (currentTargetNode != null && currentTargetNode.entity != null && currentTargetNode.entity.HasComp<MovementComponent>())
			{
				var movementComponent = currentTargetNode.entity.GetComp<MovementComponent>();
				movementComponent.PathPoints.Clear();
				movementComponent.PathPoints.AddRange(movementComponent.PathPointTFCache.Select(x=>x.transform.position));
			}
		}
	}
}
        
