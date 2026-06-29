using System;
using System.Collections.Generic;
using Game.Avatar;
using UnityEngine;

namespace Game.Pet
{

    class PetPGCTailPartAdapter : PetBonePartAdapter {
        protected override string PartPath => "Pet001 Spine/pet_waist_01/pet_waist_02";
        private Transform partBone;
        private string PartBonePath => "Pet001 Root/Pet001 Spine/pet_waist_01";
        private string PartParentPath => "pet_waist_02";
        public override void Move(Vector3 pos) {
            partBone.transform.localPosition = pos;
        }

        public override void Rotate(Vector3 rot) {
            partParent.localEulerAngles = rot;
        }

        public override void Scale(Vector3 sca) {
            partBone.transform.localScale = sca;
        }

        public PetPGCTailPartAdapter(GameObject _avatar, Dictionary<string, Transform> bones) : base(_avatar, bones) {
            partBone = _avatar.transform.Find(PartBonePath);
            partParent = partBone.transform.Find(PartParentPath);
        }

    }
}

