/**
 * @ Author: Jun Zhou
 * @ Create Time: 2023-08-08 14:46:44
 * @ Modified by: Jun Zhou
 * @ Modified time: 2023-08-22 13:36:40
 * @ Description: 可以移动物体的控制器
 */

using UnityEngine;
using Game.KinematicCharacter;

namespace Props.Mono
{
    public class MovingPlatformController : MonoBehaviour, IMoverController
    {
        PhysicsMover mover;
        Rigidbody rgBody;

        Vector3 _originalPosition;
        Quaternion _originalRotation;

        private void Awake() 
        {
            mover = this.gameObject.AddComponent<PhysicsMover>();
            // rgBody = GetComponent<Rigidbody>();
            // rgBody.useGravity = false;
            
            mover.MoverController = this;
            _originalPosition = this.transform.localPosition;
            _originalRotation = this.transform.localRotation;
            mover.SetPositionAndRotation(this.transform.position, this.transform.rotation);
        }

        public void UpdateMovement(out Vector3 goalPosition, out Quaternion goalRotation, float deltaTime)
        {
            goalPosition = this.transform.parent.TransformPoint(_originalPosition);
            goalRotation = this.transform.parent.rotation * _originalRotation;
        }

        public void StopPhysics()
        {
            if (mover != null)
            {
                mover.enabled = false;
            }
        }

        private void OnDestroy() 
        {
            Destroy(mover);
            Destroy(rgBody);
        }
    }
}