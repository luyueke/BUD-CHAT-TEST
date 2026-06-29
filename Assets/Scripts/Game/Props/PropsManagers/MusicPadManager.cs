
using Game.Base;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;

namespace Game.Props.PropsManagers
{
    [NodeBehaviourAttribute(typeof(MusicPadBehaviour))]
	public class MusicPadManager : BaseNodeManager
	{
		protected override void OnNotifyCreateInEdit(NodeBaseBehaviour nodeBehaviour) 
		{
			nodeBehaviour.entity.AddComp<MusicPadComponent>();
		}

		protected override void OnNotifyCreateInClone(NodeBaseBehaviour oldBehaviour, NodeBaseBehaviour newBehaviour)
		{
			RefreshNode(newBehaviour as MusicPadBehaviour);
		}

		protected override void OnNotifyCreateInBuild(NodeBaseBehaviour nodeBehaviour) 
		{

			RefreshNode(nodeBehaviour as MusicPadBehaviour);
		}

		void RefreshNode(MusicPadBehaviour behaviour)
		{
			var behav = behaviour;
			var musicPadComponent = behaviour.entity.GetComp<MusicPadComponent>();
			for (int i = 0; i < musicPadComponent.KeyIds.Count; i++)
			{
				behav.SetColor(i, musicPadComponent.KeyIds[i]);
			}
		}
	}
}
        
