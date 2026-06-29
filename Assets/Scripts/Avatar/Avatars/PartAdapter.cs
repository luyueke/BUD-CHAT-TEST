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

namespace Game.Avatar
{
    public abstract class PartAdapter
    {
        public abstract void Move(Vector3 pos);
        public abstract void Rotate(Vector3 rot);
        public abstract void Scale(Vector3 sca);

        public abstract void HVScale(Vector3 sca);

        public abstract void ChangeColor(Color col);

        public abstract void AddEffect();

        public abstract void RemoveEffect();

        public abstract void PutOn(string id, Action action);

        public abstract void UGCPutOn(string id, string url, int ugcStyle = 0, Action suc = null);

        /// <summary>
        /// 3D 衣服
        /// </summary>
        /// <param name="id"></param>
        /// <param name="url"></param>
        /// <param name="suc"></param>
        public abstract void PropSkinPutOn(string id, string url, Action suc);

        /// <summary>
        /// 设置锚点偏移
        /// </summary>
        public abstract void SetAnchor(Vector3 anchor);

        public abstract void TakeOff();

        public abstract void ResetCurrentID();

        public abstract void Reset();

        public abstract void SetLeftOrRight(int leftOrRight);

        public abstract void IsUIOrSelfPlayer(bool flag);
    }

}