using System.Collections.Generic;
using Es;
using GameData.PgcData;
using UnityEngine;

namespace BUD.AnimPose
{
    public class PropAnimIK:MonoBehaviour
    {
        private Dictionary<int, Transform> bindNodes;
        private PoseModeConfig poseData;
        public int curIndex { get; private set; }//对应道具列表索引(最多5个)
        
        public string curUid{ get; private set; }
        public int bindIndex { get; private set; }//对应绑定骨骼索引
        public UgcPoseSubType poseType { get;private set; }

        public void InitPropNode(int index,int bIndex,string uid,UgcPoseSubType pType,Dictionary<int, Transform> nodes)
        {
            curUid = uid;
            curIndex = index;
            bindIndex = bIndex;
            poseType = pType;
            bindNodes = nodes;
            poseData = DataTables.GetPoseModeConfig((int) poseType);
        }

        public void UpdateBindNodes(Dictionary<int, Transform> nodes)
        {
            bindNodes = nodes;
        }

        public void SetKeyFrameData(ItemKeyFrameData keyFrameData)
        {
            SetBindIndex(keyFrameData.bindIndex);
            this.transform.localPosition = keyFrameData.pos;
            this.transform.localRotation = keyFrameData.rot;
            this.transform.localScale = keyFrameData.sca;
            this.gameObject.SetActive(keyFrameData.state == 0);
        }

        public void SetBindIndex(int bIndex)
        {
            bindIndex = bIndex;
            var parNode = GetBindNode(bIndex);
            this.transform.SetParent(parNode);
        }

        public void SaveKeyFrame(ItemKeyFrameData keyFrame)
        {
            keyFrame.pos = this.transform.localPosition;
            keyFrame.rot = this.transform.localRotation;
            keyFrame.sca = this.transform.localScale;
            keyFrame.state = this.gameObject.activeSelf ? 0 : 1;
            keyFrame.bindIndex = bindIndex;
            keyFrame.index = curIndex;
        }
        
        public void SetKeyFrameData(ItemKeyFrameData current, ItemKeyFrameData next, float offset)
        {
            var itemData = offset > 0 ? next : current;
            this.transform.gameObject.SetActive(itemData.state == 0);
            
            if (current.state != next.state || current.bindIndex != next.bindIndex)
            {
                SetKeyFrameData(itemData);
                return;
            }
           
            var curNode = GetBindNode(current.bindIndex);
            this.transform.SetParent(curNode);
            this.transform.localPosition = Vector3.Lerp(current.pos, next.pos, offset);
            this.transform.localRotation = Quaternion.Lerp(current.rot, next.rot, offset);
            this.transform.localScale = Vector3.Lerp(current.sca, next.sca, offset);
           
        }


        private Transform GetBindNode(int bindIndex)
        {
            if (bindNodes != null && bindNodes.ContainsKey(bindIndex))
            {
                return bindNodes[bindIndex];
            }
            Debug.LogError("无法获取BindNode节点，数据异常 "+bindIndex);
            return null;
        }
    }
}