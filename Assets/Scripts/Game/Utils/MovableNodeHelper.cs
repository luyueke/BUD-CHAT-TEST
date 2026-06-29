
using UnityEngine;
using Game.Base;
using Game.Props.PropsBehaviours;
using Props.Mono;
using DG.Tweening;
/**
* @ Author: Jun Zhou
* @ Create Time: 2023-08-08 15:31:34
* @ Modified by: Jun Zhou
* @ Modified time: 2023-08-08 15:33:06
* @ Description: 可移动道具的帮助类
*/
namespace Game.Utils
{
    public class MovableNodeHelper
    {
        public static void AddMovingPlatformMono(NodeBaseBehaviour behaviour)
        {
            if (behaviour is MultiChildBehaviour)
            {
                // TODO 组合节点只挂一个
                var children = behaviour.GetComponentsInChildren<NodeBaseBehaviour>();
                for (int i = 0; i < children.Length; i++)
                {
                    var child = children[i].gameObject;
                    if (!child.TryGetComponent<MovingPlatformController>(out var movingController))
                    {
                        child.AddComponent<MovingPlatformController>();
                    }
                }
            } else {
                var child = behaviour.gameObject;
                if (!child.TryGetComponent<MovingPlatformController>(out var movingController))
                {
                    child.AddComponent<MovingPlatformController>();
                }
            }
        }

        public static void RemoveMovingPlatformMono(NodeBaseBehaviour behaviour)
        {
            if (behaviour is MultiChildBehaviour)
            {
                var children = behaviour.GetComponentsInChildren<NodeBaseBehaviour>();
                for (int i = 0; i < children.Length; i++)
                {
                    if (children[i].gameObject.TryGetComponent<MovingPlatformController>(out var movingController))
                    {
                        movingController.StopPhysics();
                        movingController.enabled = false;
                        GameObject.Destroy(movingController);
                    }
                }
            } else {
                if (behaviour.TryGetComponent<MovingPlatformController>(out var movingController))
                {
                    movingController.StopPhysics();
                    movingController.enabled = false;
                    GameObject.Destroy(movingController);
                }
            }
        }

        /// <summary>
        /// 为节点添加一个动画父节点。 为了不影响原本节点的动画 
        /// 替换成移动节点
        /// </summary>
        public static GameObject ReplaceMovableNode(string tag, NodeBaseBehaviour originNode)
        {
            var originNodeTF = originNode.transform;
            var animNode = MotionNodePool.Inst.Get(tag);
            animNode.transform.SetParent(originNodeTF.parent);
            animNode.transform.position = originNodeTF.position;
			animNode.transform.eulerAngles = originNodeTF.eulerAngles;
            originNodeTF.SetParent(animNode.transform);
            MovableNodeHelper.AddMovingPlatformMono(originNode);

            return animNode;
        }

        /// <summary>
        /// 还原移动节点
        /// </summary>
        public static void RevertMovableNode(NodeBaseBehaviour originNode, Vector3 originPosition, Vector3 originRotation)
        {
            var originNodeTF = originNode.transform;
            var animNode = originNodeTF.parent;
            animNode.DOKill();
            MovableNodeHelper.RemoveMovingPlatformMono(originNode);
            
            originNode.transform.SetParent(animNode.parent);
            originNode.transform.position = originPosition;
		    originNode.transform.eulerAngles = originRotation;
      
            MotionNodePool.Inst.Release(animNode.gameObject);

        
        }
    }
}