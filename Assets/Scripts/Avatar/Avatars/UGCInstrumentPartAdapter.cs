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
    public class UGCInstrumentPartAdapter : HangingPartAdapter, IUGCPartAdapter
    {
        protected override string hanging_path { get; set; } = "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/instrument_back";
        public UGCInstrumentPartAdapter(GameObject avatar, Dictionary<string, Transform> bones) : base(avatar, bones)
        {
            nodeParent = hangingBone;
            propSkinRoot = hangingBone;
        }

        public override void AddEffect()
        {
        }

        public override void RemoveEffect()
        {
        }

        public string curUrl { get; set; }
        public GameObject propSkinRoot { get; set; }
        public override void PropSkinPutOn(string id, string url, Action suc)
        {
            base.PropSkinPutOn(id, url, suc);
            var bev = avatar.GetComponentInChildren<PlayerHoldBehaviour>();
            if (bev != null)
            {
                if (bev.curInstrumentState == MusicalHoldState.Play)
                {
                    bev.FouceSetToHand();
                }
            }
        }
    }
}