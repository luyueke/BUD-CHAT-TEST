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
    public class CapeBagPartAdapter : BonePartAdapter
    {
        protected string hanging_path { get; set; } = "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/cape";
        protected GameObject hangingBone;

        public CapeBagPartAdapter(GameObject avatar, Dictionary<string, Transform> bones) : base(avatar, bones)
        {
            hangingBone = avatar.transform.Find(hanging_path).gameObject;
        }

        public override void Move(Vector3 pos)
        {
            hangingBone.transform.localPosition = pos;
        }

        public override void Rotate(Vector3 rot)
        {
            hangingBone.transform.localRotation = Quaternion.Euler(rot);
            LoggerUtils.Log("Rotate:" + rot + "," + hangingBone.transform.localEulerAngles);
        }

        public override void Scale(Vector3 sca)
        {
            hangingBone.transform.localScale = sca;
        }
    }
}