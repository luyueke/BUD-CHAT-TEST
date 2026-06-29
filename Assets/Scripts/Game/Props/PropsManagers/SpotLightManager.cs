
using Game.Base;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;

namespace Game.Props.PropsManagers
{
    [NodeBehaviourAttribute(typeof(SpotLightBehaviour))]
	public class SpotLightManager : BaseNodeManager
	{
		protected override void OnNotifyCreateInEdit(NodeBaseBehaviour nodeBehaviour) 
		{
			nodeBehaviour.entity.AddComp<SpotLightComponent>();
		}


		public override void OnEdit()
		{
			base.OnEdit();
			foreach (var behaviour in entities)
			{
				((SpotLightBehaviour)behaviour).SetLightVisible(true);
			}
		}

		public override void OnGuest()
		{
			base.OnGuest();
			foreach (var behaviour in entities)
			{
				((SpotLightBehaviour)behaviour).SetLightVisible(false);
			}
		}

		public override void OnPlay()
		{
			base.OnPlay();
			foreach (var behaviour in entities)
			{
				((SpotLightBehaviour)behaviour).SetLightVisible(false);
			}
		}

	}
}
        
