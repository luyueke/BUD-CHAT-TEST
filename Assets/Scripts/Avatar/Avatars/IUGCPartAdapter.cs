using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Basic.Utils;
using BUD.AnimPose;
using Es;
using Game.AvatarTool;
using Game.Config;
using Game.KinematicCharacter;
using Game.Pet;
using GameData.BaseInfo;
using GameData.MapData;
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
    public interface IUGCPartAdapter
    {


        public string curUrl
        {
            get;
            set;
        }

        public GameObject curPart
        {
            get;
            set;
        }

        public GameObject propSkinRoot
        {
            get;
            set;
        }
    }
}