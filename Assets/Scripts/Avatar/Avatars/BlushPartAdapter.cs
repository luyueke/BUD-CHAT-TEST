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
    public class BlushPartAdapter : FaceRelativePartAdapter
    {
        public BlushPartAdapter(GameObject head) : base(head)
        {
            part_l = head.transform.Find("blusher_l").gameObject;
            part_r = head.transform.Find("blusher_r").gameObject;
            var node_L = part_l.transform.Find("body_blusher_l").gameObject;
            var node_R = part_r.transform.Find("body_blusher_r").gameObject;
            render_L = node_L.GetComponent<MeshRenderer>();
            render_R = node_R.GetComponent<MeshRenderer>();
            mat_L = render_L.material;
            mat_R = render_R.material;
        }
    }

    public class BrowPartAdapter : FaceRelativePartAdapter
    {
        private Transform bone_Rot_L;
        private Transform bone_Rot_R;
        public BrowPartAdapter(GameObject head) : base(head)
        {
            part_l = head.transform.Find("brow_l").gameObject;
            part_r = head.transform.Find("brow_r").gameObject;
            bone_Rot_L = part_l.transform.Find("brow_l_01");
            bone_Rot_R = part_r.transform.Find("brow_r_01");
            var node_L = bone_Rot_L.Find("body_brow_l").gameObject;
            var node_R = bone_Rot_R.Find("body_brow_r").gameObject;
            render_L = node_L.GetComponent<MeshRenderer>();
            render_R = node_R.GetComponent<MeshRenderer>();
            mat_L = render_L.material;
            mat_R = render_R.material;
        }

        public override void Rotate(Vector3 rot)
        {
            Vector3 rot_L = rot;
            Vector3 rot_R = rot_L;
            rot_R.y = -rot_R.y;
            bone_Rot_L.localEulerAngles = rot_L;
            bone_Rot_R.localEulerAngles = rot_R;

        }
    }

    public abstract class FaceRelativePartAdapter : StandardPartAdapter
    {
        protected GameObject part_l;
        protected GameObject part_r;

        protected Material mat_L;
        protected Material mat_R;

        private Texture lTex;
        private Texture rTex;

        protected MeshRenderer render_L;
        protected MeshRenderer render_R;

        private AssetWrapper<Texture> warpper_L;
        private AssetWrapper<Texture> warpper_R;

        public FaceRelativePartAdapter(GameObject head) : base(head)
        {
        }


        public override void Move(Vector3 offset)
        {
            part_l.transform.localPosition = offset;
            offset.z = -offset.z;
            part_r.transform.localPosition = offset;
        }


        public override void Scale(Vector3 sca)
        {
            part_l.transform.localScale = sca;
            part_r.transform.localScale = sca;
        }

        public override void ChangeColor(Color col)
        {
            base.ChangeColor(col);
            if (mat_L != null)
            {
                mat_L.SetColor("_BaseColor", col);
            }

            if (mat_R != null)
            {
                mat_R.SetColor("_BaseColor", col);
            }
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
            LoadRes<Texture>(bData.texDir + bData.texName[0] + texExt, (isSuc, warpper) =>
            {
                if (isSuc && warpper != null && curPartId == id && part_l != null)
                {
                    warpper_L = warpper;
                    lTex = warpper.RetainAsset(part_l);
                    if (lTex == null)
                    {
                        Debug.LogError($"id {id} lTex is not exist");
                        action?.Invoke();
                        return;
                    }
                    part_l.SetActive(true);
                    if (mat_L != null)
                    {
                        mat_L.SetTexture("_BaseMap", lTex);
                    }
                    ChangeColor(curColor);
                    action?.Invoke();
                }
                else
                {
                    action?.Invoke();
                }
            });

            LoadRes<Texture>(bData.texDir + bData.texName[1] + texExt, (isSuc, warpper) =>
            {
                if (isSuc && warpper != null && curPartId == id && part_r != null)
                {
                    warpper_R = warpper;
                    rTex = warpper.RetainAsset(part_r);
                    if (rTex == null)
                    {
                        Debug.LogError($"id {id} lTex is not exist");
                        return;
                    }
                    part_r.SetActive(true);
                    if (mat_R != null)
                    {
                        mat_R.SetTexture("_BaseMap", rTex);
                    }
                    ChangeColor(curColor);
                }
            });
        }

        //考虑资源释放
        public override void TakeOff()
        {
            part_l.SetActive(false);
            part_r.SetActive(false);
            if (warpper_L != null)
            {
                warpper_L = null;
            }

            if (warpper_R != null)
            {
                warpper_R = null;
            }
        }

        public override void Reset()
        {
            base.Reset();

            render_L.material = mat_L;
            render_R.material = mat_R;

            var active = curPartId != "0" && !string.IsNullOrEmpty(curPartId);
            part_l.SetActive(active);
            part_l.SetActive(active);
        }
    }
    public class EarringPartAdapter : StandardPartAdapter
    {
        public enum EarLRType
        {
            Default = 0,
            Left = 1,
            Right = 2,
            Both = 3
        }
        private const string ear_l_path = "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/Bip001 Neck/Bip001 Head/earring_l/earring_l_01";
        private const string ear_r_path = "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/Bip001 Neck/Bip001 Head/earring_r/earring_r_01";
        private List<GameObject> earNodes;
        private List<MeshRenderer> earRenderers;
        public EarringPartAdapter(GameObject avatar) : base(avatar)
        {
            earNodes = new List<GameObject>();
            earRenderers = new List<MeshRenderer>();
            var ear_l = avatar.transform.Find(ear_l_path).gameObject;
            var ear_r = avatar.transform.Find(ear_r_path).gameObject;
            earNodes.Add(ear_l);
            earNodes.Add(ear_r);
        }

        public override void Move(Vector3 pos)
        {
            earNodes[0].transform.localPosition = pos;
            earNodes[1].transform.localPosition = pos;
        }

        //TODO：考虑是否子节点旋转
        public override void Rotate(Vector3 rot)
        {
            earNodes[0].transform.localEulerAngles = rot;
            earNodes[1].transform.localEulerAngles = rot;
        }

        public override void Scale(Vector3 sca)
        {
            earNodes[0].transform.localScale = sca;
            earNodes[1].transform.localScale = sca;
        }

        public override void ChangeColor(Color col)
        {
            if (earRenderers.Count != 0)
            {
                for (var i = 0; i < earRenderers.Count; i++)
                {
                    earRenderers[i].material.SetColor("_BaseColor", col);
                }
            }
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
            LoadRes<GameObject>(bData.texDir + bData.prefabName + prefabExt, (isSuc, wrapper) =>
            {
                if (isSuc && wrapper != null && curPartId == id && avatar != null)
                {
                    TakeOff();
                    var partPrefab = wrapper.RetainAsset(avatar);
                    ChangeEar(partPrefab, (EarLRType)bData.leftRightType);
                    lod?.SetLodMesh(earNodes);
                    action?.Invoke();
                }
                else
                {
                    action?.Invoke();
                }
            });
        }


        private void ChangeEar(GameObject partPrefab, EarLRType leftRightType)
        {
            earRenderers.Clear();
            switch (leftRightType)
            {
                case EarLRType.Left:
                    var node_l = partPrefab.transform.Find(ear_l_path);
                    var part_l = GetPartNode(node_l);
                    var node = InstantiateHand(part_l, earNodes[0].transform);
                    earRenderers.Add(node.GetComponent<MeshRenderer>());
                    break;
                case EarLRType.Right:
                    var node_r = partPrefab.transform.Find(ear_r_path);
                    var part_r = GetPartNode(node_r);
                    node = InstantiateHand(part_r, earNodes[1].transform);
                    earRenderers.Add(node.GetComponent<MeshRenderer>());
                    break;
                case EarLRType.Both:
                    var node_bl = partPrefab.transform.Find(ear_l_path);
                    var node_br = partPrefab.transform.Find(ear_r_path);
                    var part_bl = GetPartNode(node_bl);
                    var part_br = GetPartNode(node_br);
                    node = InstantiateHand(part_bl, earNodes[0].transform);
                    var node1 = InstantiateHand(part_br, earNodes[1].transform);
                    earRenderers.Add(node.GetComponent<MeshRenderer>());
                    earRenderers.Add(node1.GetComponent<MeshRenderer>());
                    break;
            }
        }

        private GameObject InstantiateHand(GameObject src, Transform par)
        {
            return GameObject.Instantiate(src, par);
        }

        public override void TakeOff()
        {
            earNodes.ForEach(x => ClearChildNode(x.transform));
        }
    }


}