using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Avatar
{
    public class UGCVehiclePartAdapter : HangingPartAdapter, IUGCPartAdapter
    {
        protected override string hanging_path { get; set; } = "dialogpos";

        public UGCVehiclePartAdapter(GameObject avatar, Dictionary<string, Transform> bones) : base(avatar, bones)
        {
            nodeParent = hangingBone;
            propSkinRoot = hangingBone;
        }

        public string curUrl { get; set; }
        public GameObject propSkinRoot { get; set; }
    }
}

