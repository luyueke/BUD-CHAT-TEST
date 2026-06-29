/**
 * @ Author: Jun Zhou
 * @ Create Time: 2023-07-19 15:50:26
 * @ Modified by: Jun Zhou
 * @ Modified time: 2023-09-12 18:21:28
 * @ Description: 基础模具
 */
using Game.Base;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using UnityEngine;

namespace Game.Props.PropsManagers
{
    [NodeBehaviourAttribute(typeof(SimpleShapeBehaviour))]
	public class SimpleShapeManager : BaseNodeManager
	{
		protected override void OnNotifyCreateInEdit(NodeBaseBehaviour nodeBehaviour)
		{
			nodeBehaviour.entity.AddComp<MaterialComponent>();
			RefreshNode(nodeBehaviour as SimpleShapeBehaviour);
            SetModelOnGround(nodeBehaviour);
        }

		protected override void OnNotifyCreateInClone(NodeBaseBehaviour oldBehaviour, NodeBaseBehaviour newBehaviour)
		{
			RefreshNode(newBehaviour as SimpleShapeBehaviour);
		}

		protected override void OnNotifyCreateInBuild(NodeBaseBehaviour nodeBehaviour)
		{
			if (nodeBehaviour.entity.HasComp<MaterialComponent>())
			{
				RefreshNode(nodeBehaviour as SimpleShapeBehaviour);
			}
		}

		void RefreshNode(SimpleShapeBehaviour behaviour)
		{
			var behav = behaviour;
			var materialComponent = behaviour.entity.GetComp<MaterialComponent>();
			behav.SetMaterial(materialComponent.matId);
			behav.SetMaterialTiling(materialComponent.tile);
			behav.SetColor(materialComponent.color);
		}

        private void SetModelOnGround(NodeBaseBehaviour nodeBaseBehaviour) {
            float offset = 0;
            var minY = float.MaxValue;
            var renderList = nodeBaseBehaviour.GetComponentsInChildren<Renderer>();
            foreach (var tmpRender in renderList) {
                if (tmpRender.bounds.min.y < minY) {
                    minY = tmpRender.bounds.min.y;
                }
            }
            var curPos = nodeBaseBehaviour.transform.position;
            offset = curPos.y - minY;
            if (curPos.y <= offset) {
                curPos.y = offset;
                nodeBaseBehaviour.transform.position = curPos;
            }

        }
    }
}
