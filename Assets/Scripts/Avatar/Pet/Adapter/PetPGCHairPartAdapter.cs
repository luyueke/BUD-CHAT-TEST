using System.Collections.Generic;
using UnityEngine;

namespace Game.Pet {
    public class PetPGCHairPartAdapter : PetBonePartAdapter  {
        public PetPGCHairPartAdapter(GameObject avatar, Dictionary<string, Transform> bones) : base(avatar, bones)
        {
        }

        public override void ChangeColor(Color col) {

        }
    }
}
