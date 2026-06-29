using Game.Base;
using Game.ECS;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Props.PropsManagers
{
	[NodeBehaviourAttribute(typeof(AIBuddyInMapBehaviour))]
	public class AIBuddyInMapManager : BaseNodeManager
	{
		private Dictionary<uint, AIBuddyInMapBehaviour> allBehaviours = new();

		/// <summary>当前已注册的全部地图伙伴行为（供 UI 侧编排器初始化时兜底扫描装配）。</summary>
		public IEnumerable<AIBuddyInMapBehaviour> AllBehaviours => allBehaviours.Values;

		protected override void OnNotifyCreateInEdit(NodeBaseBehaviour nodeBehaviour)
		{
			nodeBehaviour.entity.AddComp<AIBuddyInMapComponent>();
		}

		public void RegisterBehaviour(AIBuddyInMapBehaviour behaviour)
		{
			uint uid = behaviour.entity.GetComp<GameObjectComponent>().Uid;
			if (!allBehaviours.ContainsKey(uid))
			{
				AIBuddyInMapBehaviour b = behaviour as AIBuddyInMapBehaviour;
				allBehaviours.Add(uid, b);
			}
		}

		public void UnregisterBehaviour(uint uid)
		{
			allBehaviours.Remove(uid);
		}

		/// <summary>
		/// 请求装配地图伙伴：Cabin 数据在 UI 程序集，Game 取不到，
		/// 故广播给 UI 侧编排器（AIBuddyInMapUIManager）拉取数据后回调装配。
		/// </summary>
		public void QueueAIBuddyRequest(AIBuddyInMapBehaviour behaviour)
		{
			if (behaviour != null && behaviour.aIBuddyInMapComponent != null &&
				!string.IsNullOrEmpty(behaviour.aIBuddyInMapComponent.AiBuddyID))
			{
				behaviour.RequestSetup();
			}
		}

		public override void OnEdit()
		{
			base.OnEdit();
			foreach (var behaviour in allBehaviours.Values)
			{
				if (behaviour != null )
				{
					behaviour.OnEdit();
					// 进编辑态时（AiBuddyID 已就绪）重新请求装配已配置的伙伴：
					// UIManager 跨场景持久化、兜底扫描只跑一次，重进场景须在此重新广播，否则原有伙伴不加载。
					QueueAIBuddyRequest(behaviour);
				}
			}
		}

		public override void OnPlay()
		{
			base.OnPlay();
			foreach (var behaviour in allBehaviours.Values)
			{
				if (behaviour != null )
				{
					behaviour.OnPlay();
					QueueAIBuddyRequest(behaviour);
				}
			}
        }
	}
}
        
