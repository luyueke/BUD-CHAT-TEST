
using UnityEngine;
using System.Collections.Generic;

/**
* @ Author: Jun Zhou
* @ Create Time: 2023-08-03 10:43:51
* @ Modified by: Jun Zhou
* @ Modified time: 2023-08-03 10:45:38
* @ Description: 可移动节点的缓存池
*/
namespace Game.Utils
{
    public class MotionNodePool : InstMonoBehaviour<MotionNodePool>
    {
        private List<GameObject> nodePool = new List<GameObject>();

        public GameObject Get(string name)
        {
            GameObject node = null;
            if (nodePool.Count > 0)
            {
                node = nodePool[0];
                nodePool.RemoveAt(0);
            }
            else
            {
                node = new GameObject();
                node.AddComponent<DoTweenBehaviour>();
            }
            node.name = name;
            return node;
        }

        public void Release(GameObject node)
        {
            node.transform.SetParent(this.transform);
            nodePool.Add(node);
        }
    }
}