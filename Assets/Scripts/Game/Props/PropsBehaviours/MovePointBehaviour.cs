
using Game.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Props.PropsBehaviours
{
    public class MovePointBehaviour : NodeBaseBehaviour
    {
        private TextMeshPro numTxt;
        private GameObject lineGo;

        Transform tragetTF; // 路点链接的上一个对象

        private NodeBaseBehaviour ctrNode;//控制该Point的节点

        private void Awake() 
        {
            numTxt = this.transform.Find("Text").GetComponent<TextMeshPro>();
            lineGo = this.transform.Find("MovePointLine").gameObject;

            LoggerUtils.Log("MovePointBehaviour.Awake");
        }

        private void OnDisable() 
        {
            tragetTF = null;
        }

        public void SetControlNode(NodeBaseBehaviour node)
        {
            ctrNode = node;
        }

        public NodeBaseBehaviour GetControlNode()
        {
            return ctrNode;
        }

        public void SetTarget(Transform target)
        {
            tragetTF = target;
        }

        public void SetText(int num)
        {
            numTxt.text = num.ToString();
        }

        public void Clear()
        {
            tragetTF = null;
            ctrNode = null;
        }

        void Update()
        {
            if (tragetTF!=null && lineGo!=null)
            {
                lineGo.transform.LookAt(tragetTF);
                lineGo.transform.localScale =
                    new Vector3(1, 1,  0.5f*Vector3.Distance(tragetTF.position, transform.position));
            }
        }
    }
}
        
