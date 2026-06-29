using System.Collections.Generic;
using UnityEngine;

namespace Game.Pet {
    public class PetPGCScarfPartAdapter : PetNormalPartAdapter {
        protected override string PartBonePath => "Pet001 Root/Pet001 Spine/Pet001 Spine1/pet_necklace_01";
        protected override string PartParentPath => "pet_necklace_02";

        public PetPGCScarfPartAdapter(GameObject _avatar) : base(_avatar) {
        }

    }
}
