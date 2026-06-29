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
    public static class BodyPath
    {
        //骨骼头部路径
        public const string BONE_PATH = "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/Bip001 Neck/Bip001 Head";

        //骨骼右手路径
        public const string RIGHT_HAND_PATH = "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/Bip001 Neck/Bip001 R Clavicle/Bip001 R UpperArm/Bip001 R Forearm/Bip001 R Hand/prop_r";
        //骨骼左手路径
        public const string LEFT_HAND_PATH = "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/Bip001 Neck/Bip001 L Clavicle/Bip001 L UpperArm/Bip001 L Forearm/Bip001 L Hand/prop_l";
        public const string LEFT_EFFECT_PATH = "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/Bip001 Neck/Bip001 L Clavicle/Bip001 L UpperArm/Bip001 L Forearm/Bip001 L Hand/effect_l";
        public const string RIGHT_EFFECT_PATH = "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/Bip001 Neck/Bip001 R Clavicle/Bip001 R UpperArm/Bip001 R Forearm/Bip001 R Hand/effect_r";

        //骨骼右手拾起取点路径
        public const string PICK_HAND_PATH = "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/Bip001 Neck/Bip001 R Clavicle/Bip001 R UpperArm/Bip001 R Forearm/Bip001 R Hand/prop_r/pickNode";
        //吃东西拾起点
        public const string PICK_FOOD_PATH = "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/Bip001 Neck/Bip001 R Clavicle/Bip001 R UpperArm/Bip001 R Forearm/Bip001 R Hand/prop_r/foodNode";

        //背部路径
        public const string BACK_PATH = "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/backNode";

        //背包路径
        public const string BAG_PATH = "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/back";
        public const string SPECIAL_BAG_PATH = "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/special_back";

        // 帽子路径
        public const string HAT_PATH = "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/Bip001 Neck/Bip001 Head/hat";
        public const string SPECIAL_HAT_PATH = "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/Bip001 Neck/Bip001 Head/special_hat";

        public const string SPECIAL_EFFECT_PATH = "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/Bip001 Neck/Bip001 Head/special_effect/effect_x";

    }

    public enum BodyNode
    {
        RightHand = 1,
        LeftHand = 2,
        PickNode = 3,
        PickPos = 4,
        FoodNode = 5,
        BackNode = 6,//降落伞背包节点位置
        LEffectNode = 7,
        BackDeckNode = 8,//背包节点位置
        REffectNode = 9,//右手特效、道具挂点-光剑（该节点不可K动画！！）
        HatNode = 10, // 帽子挂点
        SpecialBackDeckNode = 11,
        SpecialHatNode = 12,
        SpecialEffectNode = 13
    }
}