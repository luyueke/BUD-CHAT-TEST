using System;
using System.Collections.Generic;
using Game.Avatar;
using UnityEngine;

namespace Game.Pet
{

    class PetPGCBackpackPartAdapter : PetNormalPartAdapter {


        protected override string PartBonePath => "Pet001 Root/Pet001 Spine/Pet001 Spine1/pet_back_01";
        protected override string PartParentPath => "pet_back_02";




        public PetPGCBackpackPartAdapter(GameObject _avatar) : base(_avatar)
        {
        }
    }
}
