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

    public class UGCHandPartAdapter : StandardPartAdapter, IUGCPartAdapter
    {
        public enum HandLRType
        {
            Default = 0,
            Left = 1,
            Right = 2,
            Both = 3
        }
        private const string hand_l_path = "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/Bip001 Neck/Bip001 L Clavicle/Bip001 L UpperArm/Bip001 L Forearm/arm_l/arm_l_x";
        private const string hand_r_path = "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/Bip001 Neck/Bip001 R Clavicle/Bip001 R UpperArm/Bip001 R Forearm/arm_r/arm_r_x";
        protected virtual string glove_l_path => "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/Bip001 Neck/Bip001 L Clavicle/Bip001 L UpperArm/Bip001 L Forearm/Bip001 L Hand/glove_l/glove_l_x";
        protected virtual string glove_r_path => "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/Bip001 Neck/Bip001 R Clavicle/Bip001 R UpperArm/Bip001 R Forearm/Bip001 R Hand/glove_r/glove_r_x";
        private List<GameObject> handNodes;
        private List<GameObject> gloveNodes;
        //添加手部道具时额外绑定一个父节点
        public UGCHandPartAdapter(GameObject avatar, Dictionary<string, Transform> bones) : base(avatar, bones)
        {
            Init(avatar);
        }

        public override void ResetCurrentID()
        {
            base.ResetCurrentID();
            curUrl = string.Empty;
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

            propSkinRoot = lgNode;
        }
        public override void Move(Vector3 pos)
        {
            List<GameObject> nodes = gloveNodes;
            nodes[0].transform.localPosition = pos;
            nodes[1].transform.localPosition = pos;
        }

        public override void Rotate(Vector3 rot)
        {
            List<GameObject> nodes = gloveNodes;
            nodes[0].transform.localEulerAngles = rot;
            nodes[1].transform.localEulerAngles = rot;
        }

        public override void Scale(Vector3 sca)
        {
            List<GameObject> nodes = gloveNodes;
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
            PutOn(id, suc => action?.Invoke());
        }


        public void PutOn(string id, Action<bool> action)
        {
            curPartId = id;
            var bData = UgcPartDataManager.Inst.GetUgcPartDataOnFirst(id);
            if (bData == null)
            {
                action?.Invoke(false);
                return;
            }
            LoadRes<GameObject>(GameConsts.ClothesAssetDir + bData.prefabName + prefabExt, (isSuc, warpper) =>
            {
                if (isSuc && warpper != null && id == curPartId && avatar != null)
                {
                    TakeOff();
                    var partPrefab = warpper.RetainAsset(avatar);
                    curPart = new GameObject(GameConsts.handMeshNodeName);
                    curPart.transform.SetParent(gloveNodes[0].transform);
                    curPart.transform.localPosition = Vector3.zero;
                    curPart.transform.localRotation = Quaternion.identity;
                    curPart.transform.localScale = Vector3.one;
                    var node_l = partPrefab.transform.Find(glove_l_path);
                    var ugcdataList = UgcPartDataManager.Inst.GetUgcPartDataList(id);
                    if (ugcdataList != null)
                    {
                        for (int i = 0; i < ugcdataList.Count; i++)
                        {
                            var part_l = GameObject.Instantiate(GameObjectEx.FindChildByName(node_l, ugcdataList[i].partsName), curPart.transform);
                            part_l.name = ugcdataList[i].partsName;
                            UpdateBones(part_l.gameObject);
                        }
                    }
                    action?.Invoke(true);
                }
                else
                {
                    action?.Invoke(false);
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
                List<GameObject> nodes = gloveNodes;
                if (handLR == HandLRType.Left && curPart.transform.parent != nodes[0].transform)
                {
                    var pos = curPart.transform.localPosition;
                    curPart.transform.SetParent(nodes[0].transform);
                    curPart.transform.localPosition = new Vector3(-pos.x, pos.y, pos.z);
                    curPart.transform.localRotation *= Quaternion.Euler(new Vector3(0f, 0f, 180f));
                }
                else if (handLR == HandLRType.Right && curPart.transform.parent != nodes[1].transform)
                {
                    var pos = curPart.transform.localPosition;
                    curPart.transform.SetParent(nodes[1].transform);
                    curPart.transform.localPosition = new Vector3(-pos.x, pos.y, pos.z);
                    curPart.transform.localRotation *= Quaternion.Euler(new Vector3(0f, 0f, 180f));
                }
            }
        }

        public override void UGCPutOn(string id, string url, int ugcStyle, Action suc)
        {
            curPartId = id;
            curUrl = url;

            Action<bool> callback = result =>
            {
                if (!result || curPart == null || curPartId != id || curUrl != url)
                {
                    suc?.Invoke();
                    return;
                }

                if (string.IsNullOrEmpty(url))
                {
                    suc?.Invoke();
                    return;
                }

                Action<bool, UGCTempPartRemoteWrapper> completed = (success, wrapper) =>
                {
                    if (!success || curPartId != id || curUrl != url || curPart == null)
                    {
                        suc?.Invoke();
                        return;
                    }

                    var dic = wrapper.RetainAssets(curPart);

                    if (dic == null)
                    {
                        suc?.Invoke();
                        return;
                    }

                    foreach (var kValue in dic)
                    {
                        string nameWithoutEx = Path.GetFileNameWithoutExtension(kValue.Key);
                        string[] nameData = nameWithoutEx.Split('_');
                        int ugcType; //前后左右
                        string MatTextureName = string.Empty;
                        if (!nameData.Contains("alpha"))
                        {
                            MatTextureName = ugcStyle == (int)UgcShaderStyle.Anime ? "_BaseMap" : "_MainTex";
                            ugcType = int.Parse(nameData[0]);
                        }
                        else
                        {
                            ugcType = int.Parse(nameData[0]);
                            MatTextureName = "_opacity_texmask";
                        }

                        UgcPartData ugcdata = UgcPartDataManager.Inst.GetUgcPartData(id, ugcType);
                        var partsGo = curPart.transform.Find(ugcdata.partsName);
                        var renderer = partsGo.GetComponent<MeshRenderer>();
                        var partsMat = SetPartMaterial(renderer, ugcStyle);
                        partsMat.SetTexture(MatTextureName, kValue.Value);
                    }

                    suc?.Invoke();
                };
                var textureCount = UgcPartDataManager.Inst.GetUgcTextureCount(id);
                UGCPartLoader.LoadUGCPartRemoteImageAsync((int)AvatarSubType.Hand, textureCount, url, completed);
            };
            PutOn(id, callback);
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

        public override void TakeOff()
        {
            if (handNodes != null)
            {
                handNodes.ForEach(x => ClearNode(x.transform));
            }

            if (gloveNodes != null)
            {
                gloveNodes.ForEach(x => ClearNode(x.transform));
            }
        }

        private void ClearNode(Transform node)
        {
            for (int i = node.childCount - 1; i >= 0; i--)
            {
                var childNode = node.GetChild(i);
                if (childNode != null)
                {
                    childNode.SetParent(null);
                    GameObject.Destroy(childNode.gameObject);
                }
            }
        }

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