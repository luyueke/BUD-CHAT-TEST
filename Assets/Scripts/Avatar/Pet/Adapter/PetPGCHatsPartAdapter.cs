using System;
using System.Collections.Generic;
using Game.Avatar;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Pet {

    class PetPGCHatsPartAdapter : PetNormalPartAdapter {
        protected override string PartBonePath => "Pet001 Root/Pet001 Spine/Pet001 Spine1/pet001 Head/pet_hat_01";
        protected override string PartParentPath => "pet_hat_02";

        public PetPGCHatsPartAdapter(GameObject _avatar) : base(_avatar) {
        }


    }
}

