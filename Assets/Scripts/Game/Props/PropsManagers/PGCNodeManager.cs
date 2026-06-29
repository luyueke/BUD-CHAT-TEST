
using Game.Base;
using Game.ECS;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;

namespace Game.Props.PropsManagers
{
    [NodeBehaviourAttribute(typeof(PGCNodeBehaviour))]
	public class PGCNodeManager : BaseNodeManager
	{
		protected override void OnNotifyCreateInEdit(NodeBaseBehaviour nodeBehaviour) 
		{
			nodeBehaviour.entity.AddComp<PGCNodeComponent>();
			PGCNodeBehaviour pgcBehaviour = nodeBehaviour as PGCNodeBehaviour;
			CreateAssetObj(pgcBehaviour);

		}

		protected override void OnNotifyCreateInBuild(NodeBaseBehaviour nodeBehaviour)
		{
			PGCNodeBehaviour pgcBehaviour = nodeBehaviour as PGCNodeBehaviour;
			CreateAssetObj(pgcBehaviour);
		}

		
		private void CreateAssetObj(PGCNodeBehaviour plantBehaviour)
		{
			var goComp =  plantBehaviour.entity.GetComp<GameObjectComponent>();
			ModelCachePool.Inst.GetAsync(goComp.PropId, (newAssetObj) =>
			{
				plantBehaviour.SetAssetObj(newAssetObj);
			});
		}
	}
}
        
