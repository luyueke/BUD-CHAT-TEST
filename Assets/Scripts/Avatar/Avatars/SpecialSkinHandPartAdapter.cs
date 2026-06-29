using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BUD.AnimPose;
using Es;
using Game.Config;
using Game.KinematicCharacter;
using Game.Pet;
using GameData.PgcData;
using GameData.UGCData;
using Message;
using Pb.Base;
using RootMotion.FinalIK;
using UnityEngine;
using xasset;
using Debug = UnityEngine.Debug;

namespace Game.Avatar
{

    public class SpecialSkinHandPartAdapter : HandPartAdapter, SpecialSkinPart
    {

        protected override string hand_l_path => "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/Bip001 Neck/Bip001 L Clavicle/Bip001 L UpperArm/Bip001 L Forearm/arm_l/arm_l_x";
        protected override string hand_r_path => "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/Bip001 Neck/Bip001 R Clavicle/Bip001 R UpperArm/Bip001 R Forearm/arm_r/arm_r_x";
        protected override string glove_l_path => "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/Bip001 Neck/Bip001 L Clavicle/Bip001 L UpperArm/Bip001 L Forearm/Bip001 L Hand/glove_l/glove_l_x";
        protected override string glove_r_path => "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/Bip001 Neck/Bip001 R Clavicle/Bip001 R UpperArm/Bip001 R Forearm/Bip001 R Hand/glove_r/glove_r_x";


        public SpecialSkinHandPartAdapter(GameObject avatar, Dictionary<string, Transform> bones) : base(avatar, bones)
        {

        }

        public GameObject GetAniRoot()
        {
            return curPart;
        }
    }

}