using System;
using Game.Avatar;
using UnityEngine;

namespace Game.Pet
{
    class PetUGCEarPartAdapter : StandardPartAdapter , IUGCPartAdapter{
        private GameObject earBone;
        private const string ear_path = "Pet001 Root/Pet001 Spine/Pet001 Spine1/pet001 Head/pet_headdress_01";
        private const string ear_parent_path = "pet_headdress_02";
        private GameObject earParent;
        public PetUGCEarPartAdapter(GameObject _avatar) : base(_avatar) {
            propSkinRoot = _avatar.transform.Find(ear_path+"/"+ear_parent_path).gameObject;
            earBone = avatar.transform.Find(ear_path).gameObject;
            earParent = earBone.transform.Find(ear_parent_path).gameObject;
        }
        public override void AddEffect() {

        }
        public override void Move(Vector3 pos) {
            earBone.transform.localPosition = pos;
        }

        public override void Rotate(Vector3 rot) {
            earParent.transform.localEulerAngles = rot;
        }

        public override void Scale(Vector3 sca) {
            earBone.transform.localScale = sca;
        }

        public override void PutOn(string id, Action action) {

        }

        public override void RemoveEffect() {

        }
        public override void TakeOff()
        {
            if (curPart != null)
            {
                curPart.transform.SetParent(null);
                GameObject.Destroy(curPart);
                curPart = null;
            }
        }
        public string curUrl { get; set; }
        public GameObject curPart { get; set; }
        public GameObject propSkinRoot { get; set; }
    }
}

