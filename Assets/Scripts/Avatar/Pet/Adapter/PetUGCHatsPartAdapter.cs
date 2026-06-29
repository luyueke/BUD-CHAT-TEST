using System;
using Game.Avatar;
using UnityEngine;

namespace Game.Pet
{

    class PetUGCHatsPartAdapter : StandardPartAdapter , IUGCPartAdapter{
        protected Transform partBone;
        protected Transform partParent;
        private const string part_path = "Pet001 Root/Pet001 Spine/Pet001 Spine1/pet001 Head/pet_hat_01";
        private const string part_parent_path = "pet_hat_02";
        public PetUGCHatsPartAdapter(GameObject _avatar) : base(_avatar) {

            propSkinRoot = _avatar.transform.Find(part_path+"/"+part_parent_path).gameObject;
            partBone = avatar.transform.Find(part_path);
            partParent = partBone.transform.Find(part_parent_path);
        }
        public override void AddEffect() {

        }
        public override void Move(Vector3 pos) {
            partBone.transform.localPosition = pos;
        }

        public override void Rotate(Vector3 rot) {
            partParent.transform.localEulerAngles = rot;
        }

        public override void Scale(Vector3 sca) {
            partBone.transform.localScale = sca;
        }

        public override void PutOn(string id, Action action) {

        }

        public override void RemoveEffect() {

        }


        public string curUrl { get; set; }
        public GameObject curPart { get; set; }
        public GameObject propSkinRoot { get; set; }
        public override void TakeOff()
        {
            if (curPart != null)
            {
                curPart.transform.SetParent(null);
                GameObject.Destroy(curPart);
                curPart = null;
            }
        }
    }
}

