using System;
using Game.Props.PropsBehaviours;
using UnityEngine;
using UnityEngine.Events;

namespace AIGame.Prop
{
    public class NpcAvatarTrigger : MonoBehaviour
    {
        private float height = 1.8f;
        private float radius = 0.55f;

        private void FixedUpdate()
        {
            var colliders = Physics.OverlapCapsule(transform.position - height * 0.5f * Vector3.up,
                    transform.position + height * 0.5f * Vector3.up,
                    radius, LayerMask.GetMask("Model"));
            if (colliders != null)
            {
                for (var i = 0; i < colliders.Length; i++)
                {
                    var behaviour = colliders[i].GetComponentInParent<AIPropBaseBehaviour>();
                    if (behaviour != null)
                    {
                        behaviour.OnNpcColliderHit();
                    }
                }
            }
        }
    }
}