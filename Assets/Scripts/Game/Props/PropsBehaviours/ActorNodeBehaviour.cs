using System.Collections;
using System.Collections.Generic;
using Game.Base;
using Game.ECS;
using UnityEngine;

namespace Game.Props.PropsBehaviours
{
    /// <summary>
    /// 模型承载节点,用以挂载NodeBaseBehaviour相关逻辑
    /// 模型置于子层级
    /// 如：滑梯SlidePipeBehaviour、SlideItemBehaviour
    /// </summary>
    public class ActorNodeBehaviour : NodeBaseBehaviour
    {
        public GameObject assetObj;//模型节点

        public virtual void SetAssetObj(GameObject gameObject)
        {
            gameObject.transform.SetParent(transform);
            gameObject.transform.localPosition = Vector3.zero;
            gameObject.transform.localEulerAngles = Vector3.zero;
            gameObject.transform.localScale = Vector3.one;
            assetObj = gameObject;
        }
        
        //自定义模型Id，可以是内部Id，供仅有一个PropId，但拥有子Id的道具
        public virtual string GetAssetId()
        {
            GameObjectComponent comp = entity.GetComp<GameObjectComponent>();
            return comp.PropId;
        }
        
    }
}
