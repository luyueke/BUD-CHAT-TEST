using System.Collections.Generic;
using UnityEngine;

namespace BUD.AnimPose
{
    public abstract class BaseAnimIK : MonoBehaviour
    {
        public GameObject JointPrefab;
        public Transform jointNodeParent { get; set; }

        public Material ShotMaterial;
        
        public Dictionary<int, Transform> jointNodes = new Dictionary<int, Transform>();
        
        protected GameObject roleNode { get; set; }

        public abstract void SetJointNodeVisible(bool visible);
        public abstract void InitAnimIK(bool isEdit);
        public abstract void ResetJointNodes();
        public abstract void SaveKeyFrame(RoleKeyframeData keyFrame);
        public abstract void SetKeyFrameData(RoleKeyframeData keyFrameData);
        public abstract void SetKeyFrameData(RoleKeyframeData current, RoleKeyframeData next, float offset);

        public abstract void FixTransform();

        public abstract void ResetDefaultPosition();

        public abstract void SetWhiteMaterials();

        public abstract void SetDefaultMaterials();
        
        
        public void SetOptEntity(GameObject entity)
        {
            roleNode = entity;
        }
    }
}