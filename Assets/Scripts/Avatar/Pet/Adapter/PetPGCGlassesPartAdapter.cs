using System;
using System.Collections.Generic;
using Game.Avatar;
using UnityEngine;

namespace Game.Pet {
    public class PetPGCGlassesPartAdapter : PetNormalPartAdapter {


        protected override string PartBonePath => "Pet001 Root/Pet001 Spine/Pet001 Spine1/pet001 Head/pet_glasses_01";
        protected override string PartParentPath => "pet_glasses_02";

        public PetPGCGlassesPartAdapter(GameObject _avatar) : base(_avatar)
        {
        }
    }
}
