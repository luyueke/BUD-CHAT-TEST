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

    public class HandPartAdapter : StandardPartAdapter
    {
        public enum HandBipType
        {
            Arm = 0,
            Glove = 1,
        }

        public enum HandLRType
        {
            Default = 0,
            Left = 1,
            Right = 2,
            Both = 3
        }
        protected virtual string hand_l_path => "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/Bip001 Neck/Bip001 L Clavicle/Bip001 L UpperArm/Bip001 L Forearm/arm_l/arm_l_x";
        protected virtual string hand_r_path => "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/Bip001 Neck/Bip001 R Clavicle/Bip001 R UpperArm/Bip001 R Forearm/arm_r/arm_r_x";
        protected virtual string glove_l_path => "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/Bip001 Neck/Bip001 L Clavicle/Bip001 L UpperArm/Bip001 L Forearm/Bip001 L Hand/glove_l/glove_l_x";
        protected virtual string glove_r_path => "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/Bip001 Neck/Bip001 R Clavicle/Bip001 R UpperArm/Bip001 R Forearm/Bip001 R Hand/glove_r/glove_r_x";

        private List<GameObject> handNodes;
        private List<GameObject> gloveNodes;
        private HandBipType handBip;
        protected GameObject curPart;
        //添加手部道具时额外绑定一个父节点
        public HandPartAdapter(GameObject avatar) : base(avatar)
        {
            Init(avatar);
        }

        public HandPartAdapter(GameObject avatar, Dictionary<string, Transform> bones) : base(avatar, bones)
        {
            Init(avatar);
        }

        protected void Init(GameObject avatar)
        {
            handNodes = new List<GameObject>();
            gloveNodes = new List<GameObject>();

            var hand_l = avatar.transform.Find(hand_l_path).gameObject;
            var hand_r = avatar.transform.Find(hand_r_path).gameObject;
            var lNode = InstantiateParentNode(hand_l.name, hand_l.transform);
            var rNode = InstantiateParentNode(hand_r.name, hand_r.transform);
            handNodes.Add(lNode);
            handNodes.Add(rNode);

            var glove_l = avatar.transform.Find(glove_l_path).gameObject;
            var glove_r = avatar.transform.Find(glove_r_path).gameObject;
            var lgNode = InstantiateParentNode(glove_l.name, glove_l.transform);
            var rgNode = InstantiateParentNode(glove_r.name, glove_r.transform);
            gloveNodes.Add(lgNode);
            gloveNodes.Add(rgNode);
        }

        public override void Move(Vector3 pos)
        {
            List<GameObject> nodes = handBip == HandBipType.Arm ? handNodes : gloveNodes;
            nodes[0].transform.localPosition = pos;
            nodes[1].transform.localPosition = pos;
        }

        public override void Rotate(Vector3 rot)
        {
            List<GameObject> nodes = handBip == HandBipType.Arm ? handNodes : gloveNodes;
            nodes[0].transform.localEulerAngles = rot;
            nodes[1].transform.localEulerAngles = rot;
        }

        public override void Scale(Vector3 sca)
        {
            List<GameObject> nodes = handBip == HandBipType.Arm ? handNodes : gloveNodes;
            nodes[0].transform.localScale = sca;
            nodes[1].transform.localScale = sca;
        }

        public override void AddEffect()
        {
        }

        public override void RemoveEffect()
        {
        }

        public override void PutOn(string id, Action action)
        {
            curPartId = id;
            var bData = Es.DataTables.GetAvatarCommonData(id);
            if (bData == null)
            {
                action?.Invoke();
                return;
            }
            handBip = (HandBipType)bData.handBipType;
            LoadRes<GameObject>(bData.texDir + bData.prefabName + prefabExt, (isSuc, wrapper) =>
            {
                if (isSuc && wrapper != null && curPartId == id && avatar != null)
                {
                    TakeOff();
                    var partPrefab = wrapper.RetainAsset(avatar);
                    string l_path = bData.handBipType == (int)HandBipType.Arm ? hand_l_path : glove_l_path;
                    string r_path = bData.handBipType == (int)HandBipType.Arm ? hand_r_path : glove_r_path;
                    List<GameObject> nodes = bData.handBipType == (int)HandBipType.Arm ? handNodes : gloveNodes;
                    ChangeHandOrGlove(partPrefab, l_path, r_path, (HandLRType)bData.leftRightType, nodes);
                    lod?.SetLodMesh(handNodes, gloveNodes);
                    action?.Invoke();
                }
                else
                {
                    action?.Invoke();
                }
            });
        }

        public override void SetLeftOrRight(int leftOrRight)
        {
            base.SetLeftOrRight(leftOrRight);
            var handLR = (HandLRType)leftOrRight;
            if (curPart != null && handLR != HandLRType.Both)
            {
                var bData = Es.DataTables.GetAvatarCommonData(curPartId);
                List<GameObject> nodes = handBip == HandBipType.Arm ? handNodes : gloveNodes;
                if (handLR == HandLRType.Left && curPart.transform.parent != nodes[0].transform)
                {
                    var pos = curPart.transform.localPosition;
                    curPart.transform.SetParent(nodes[0].transform);
                    curPart.transform.localPosition = new Vector3(-pos.x, pos.y, pos.z);
                    curPart.transform.localRotation = bData.leftRightType == (int)handLR ? Quaternion.identity : Quaternion.Euler(new Vector3(0f, 0f, 180f));
                }
                else if (handLR == HandLRType.Right && curPart.transform.parent != nodes[1].transform)
                {
                    var pos = curPart.transform.localPosition;
                    curPart.transform.SetParent(nodes[1].transform);
                    curPart.transform.localPosition = new Vector3(-pos.x, pos.y, pos.z);
                    curPart.transform.localRotation = bData.leftRightType == (int)handLR ? Quaternion.identity : Quaternion.Euler(new Vector3(0f, 0f, 180f));
                }
            }
        }


        protected virtual void ChangeHandOrGlove(GameObject partPrefab, string lPath, string rPath, HandLRType handLR, List<GameObject> nodes)
        {
            switch (handLR)
            {
                case HandLRType.Left:
                    var node_l = partPrefab.transform.Find(lPath);
                    var part_l = GetPartNode(node_l);
                    if (part_l == null)
                    {
                        LoggerUtils.LogError($"{partPrefab.name}---lPath = {lPath}");
                        return;
                    }
                    curPart = InstantiateHand(part_l, nodes[0].transform);
                    break;
                case HandLRType.Right:
                    var node_r = partPrefab.transform.Find(rPath);
                    var part_r = GetPartNode(node_r);
                    if (part_r == null)
                    {
                        LoggerUtils.LogError($"{partPrefab.name}---rPath = {rPath}");
                        return;
                    }
                    curPart = InstantiateHand(part_r, nodes[1].transform);
                    break;
                case HandLRType.Both:
                    var node_bl = partPrefab.transform.Find(lPath);
                    var node_br = partPrefab.transform.Find(rPath);
                    var part_bl = GetPartNode(node_bl);
                    var part_br = GetPartNode(node_br);
                    if (part_bl == null || part_br == null)
                    {
                        LoggerUtils.LogError($"{partPrefab.name}---lPath = {lPath}--rPath={rPath}");
                        return;
                    }
                    InstantiateHand(part_bl, nodes[0].transform);
                    InstantiateHand(part_br, nodes[1].transform);
                    break;
            }
        }



        private GameObject InstantiateParentNode(string nodeName, Transform parent)
        {
            GameObject tempParent = new GameObject(nodeName);
            tempParent.transform.SetParent(parent);

            tempParent.transform.localPosition = Vector3.zero;
            tempParent.transform.localRotation = Quaternion.identity;
            tempParent.transform.localScale = Vector3.one;
            return tempParent;
        }

        private GameObject InstantiateHand(GameObject src, Transform par)
        {
            var node = GameObject.Instantiate(src, par);
            UpdateBones(node);
            return node;
        }

        public override void TakeOff()
        {
            handNodes.ForEach(x => ClearChildNode(x.transform));
            gloveNodes.ForEach(x => ClearChildNode(x.transform));
        }

    }

}