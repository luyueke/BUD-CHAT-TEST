using System;
using System;
using Game.Avatar;
using UnityEngine;


namespace Game.Pet {
    class PetUGCHairPartAdapter : PetNormalPartAdapter , IUGCPartAdapter{
        protected override string PartBonePath => "Pet001 Root/Pet001 Spine/Pet001 Spine1/pet001 Head/pet_ugc_hair_01";
        protected override string PartParentPath => "pet_ugc_hair_02";
        public PetUGCHairPartAdapter(GameObject _avatar) : base(_avatar) {
            propSkinRoot = _avatar.transform.Find("Pet001 Root/Pet001 Spine/Pet001 Spine1/pet001 Head/pet_ugc_hair_01/pet_ugc_hair_02").gameObject;
        }

        public override void AddEffect() {
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