
using Game.Base;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;

namespace Game.Props.PropsManagers
{
    [NodeBehaviourAttribute(typeof(PointLightBehaviour))]
	public class PointLightManager : BaseNodeManager
	{
		protected override void OnNotifyCreateInEdit(NodeBaseBehaviour nodeBehaviour) 
		{
			nodeBehaviour.entity.AddComp<PointLightComponent>();
		}

		public override void OnPlay()
		{
			base.OnPlay();
			foreach (var behaviour in entities)
			{
				((PointLightBehaviour)behaviour).SetLightVisible(false);
			}
		}

		public override void OnGuest()
		{
			base.OnGuest();
			foreach (var behaviour in entities)
			{
				((PointLightBehaviour)behaviour).SetLightVisible(false);
			}
		}

		public override void OnEdit()
		{
			base.OnEdit();
			foreach (var behaviour in entities)
			{
				((PointLightBehaviour)behaviour).SetLightVisible(true);
			}
		}

	}
}
        
