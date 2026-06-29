using System.Collections.Generic;
using UnityEngine;


namespace Game.KinematicCharacter
{
    public class PlanetManager : MonoBehaviour, IMoverController
    {
        public PhysicsMover PlanetMover;
        public SphereCollider GravityField;
        public float GravityStrength = 10;
        public Vector3 OrbitAxis = Vector3.forward;
        public float OrbitSpeed = 10;

        public Teleporter OnPlaygroundTeleportingZone;
        public Teleporter OnPlanetTeleportingZone;

        private List<KinematicCharacterController> _characterControllersOnPlanet = new List<KinematicCharacterController>();
        private Vector3 _savedGravity;
        private Quaternion _lastRotation;

        private void Start()
        {
            OnPlaygroundTeleportingZone.OnCharacterTeleport -= ControlGravity;
            OnPlaygroundTeleportingZone.OnCharacterTeleport += ControlGravity;

            OnPlanetTeleportingZone.OnCharacterTeleport -= UnControlGravity;
            OnPlanetTeleportingZone.OnCharacterTeleport += UnControlGravity;

            _lastRotation = PlanetMover.transform.rotation;

            PlanetMover.MoverController = this;
        }

        public void UpdateMovement(out Vector3 goalPosition, out Quaternion goalRotation, float deltaTime)
        {
            if (PlanetMover.RigidbodyMode)
            {
                goalPosition = PlanetMover.Rigidbody.position;
            }
            else
            {
                goalPosition = PlanetMover.Transform.position;
            }

            // Rotate
            Quaternion targetRotation = Quaternion.Euler(OrbitAxis * OrbitSpeed * deltaTime) * _lastRotation;
            goalRotation = targetRotation;
            _lastRotation = targetRotation;

            // Apply gravity to characters
            foreach (KinematicCharacterController cc in _characterControllersOnPlanet)
            {
                cc.CurIKCController._gravity = (PlanetMover.transform.position - cc.transform.position).normalized * GravityStrength;
            }
        }

        void ControlGravity(KinematicCharacterController cc)
        {
            _savedGravity = cc.CurIKCController._gravity;
            _characterControllersOnPlanet.Add(cc);
        }

        void UnControlGravity(KinematicCharacterController cc)
        {
            cc.CurIKCController._gravity = _savedGravity;
            _characterControllersOnPlanet.Remove(cc);
        }
    }
}