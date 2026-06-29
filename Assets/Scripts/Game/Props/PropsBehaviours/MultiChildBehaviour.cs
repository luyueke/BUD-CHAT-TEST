using System.Collections;
using System.Collections.Generic;
using Game.Base;
using Game.Utils;
using Google.Protobuf.Collections;
using Pb.Map;
using UnityEngine;

namespace Game.Props.PropsBehaviours
{
    /// <summary>
    /// 父节点,承载节点,用以挂在多个NodeBaseBehaviour相关逻辑
    /// 如：组合CombineBehaviour、CoinPackBehaviour等
    /// </summary>
    public class MultiChildBehaviour : NodeBaseBehaviour
    {
        /// <summary>
        /// 子节点是否可以选中 
        /// </summary>
        public bool ChildSelectable = false;

        public virtual bool IsAutoCreatePrims { get; protected set; } = true;
        
        public virtual void CreateChildNodes(RepeatedField<PNodeData> nodes)
        {
            if (nodes != null && nodes.Count > 0)
            {
                GamePropNodeManager.Inst.CreateSceneNodes(nodes, transform);
            }
        }

        public override void HighLight(bool isHigh)
        {
            base.HighLight(isHigh);
            var combineBehaviours = GetComponentsInChildren<MeshCombineBehaviour>();
            foreach (var tmpCombineBehaviour in combineBehaviours)
            {
                tmpCombineBehaviour.HighLight(isHigh);
            }
        }


    }
}
