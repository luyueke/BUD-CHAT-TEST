using System;
using UnityEngine;
using UnityEngine.Events;

public class AvatarTrigger : MonoBehaviour
{
    public UnityEvent<Collider> OnAvatarTrigEnter = new UnityEvent<Collider>();
    public UnityEvent<Collider> OnAvatarTrigExit = new UnityEvent<Collider>();
    public UnityEvent<Collider> OnAvatarColliderEnter = new UnityEvent<Collider>();
    public void Awake()
    {
    }

    private bool IsCanTrigger(Collider other)
    {
        return other.gameObject.layer == LayerMask.NameToLayer("Model") ||
               other.gameObject.layer == LayerMask.NameToLayer("Prop");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsCanTrigger(other))
        {
            OnAvatarTrigEnter?.Invoke(other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsCanTrigger(other))
        {
            OnAvatarTrigExit?.Invoke(other);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (IsCanTrigger(collision.collider))
        {
            OnAvatarColliderEnter?.Invoke(collision.collider);
        }
    }
}