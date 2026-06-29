using UnityEngine;

namespace Game.KinematicCharacter
{
    public class PrefabLauncher : MonoBehaviour
    {
        public Rigidbody ToLaunch;
        public float Force;

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                Rigidbody inst = Instantiate(ToLaunch, transform.position, transform.rotation);
                inst.AddForce(transform.forward * Force, ForceMode.VelocityChange);
                Destroy(inst.gameObject, 8f);
            }
        }
    }
}