
using Game.Base;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using Game.Scene.ModeController;
using Game.Utils;
using Google.Protobuf.Collections;
using Pb.Map;

namespace Game.Props.PropsManagers
{
    [NodeBehaviourAttribute(typeof(CombineBehaviour))]
	public class CombineManager : BaseNodeManager
	{
		protected override void OnNotifyCreateInEdit(NodeBaseBehaviour nodeBehaviour)
		{

		}

		public void BuildChildNodes(CombineBehaviour combineBehaviour, RepeatedField<PNodeData> nodes)
		{
			if (combineBehaviour == null || nodes == null || nodes.Count <= 0) {
				return;
			}

			if (GlobalNodeManager.Inst.IsGuest()) {
				var pNodeData = new PNodeData() {
					Prims = { nodes }
				};
				MeshCombineManager.Inst.GetCombineObj(null, pNodeData, combineBehaviour.transform);
			} else {
				GamePropNodeManager.Inst.CreateSceneNodes(nodes, combineBehaviour.transform);
			}
		}


	}
}

