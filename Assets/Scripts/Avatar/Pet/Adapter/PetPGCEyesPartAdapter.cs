using System;
using System.Collections.Generic;
using Basic.Utils;
using Game.Avatar;
using UnityEngine;

namespace Game.Pet {
    public class PetPGCEyesPartAdapter: StandardPartAdapter {
        protected GameObject part_l;
        protected GameObject part_r;
        protected GameObject node_l;
        protected GameObject node_r;
        private Transform bone_Rot_L;
        private Transform bone_Rot_R;

        private MeshRenderer renderer_L;
        private MeshRenderer renderer_R;
        private readonly int baseMapName = Shader.PropertyToID("_BaseMap");
        private Dictionary<string, AssetWrapper<Texture>> wrappers;
        protected Texture partTex1;
        protected Texture partTex2;
        protected Material orgMat1;
        protected Material orgMat2;

        public PetPGCEyesPartAdapter(GameObject _avatar) : base(_avatar) {

           var head =  GameObjectEx.FindChildByName(_avatar, "Pet001 Root/Pet001 Spine/Pet001 Spine1/pet001 Head");
            part_l = head.transform.Find("pet_eyes_l_01").gameObject;
            part_r = head.transform.Find("pet_eyes_r_01").gameObject;
            bone_Rot_L = part_l.transform.Find("pet_eyes_l_02");
            bone_Rot_R = part_r.transform.Find("pet_eyes_r_02");
            node_l = bone_Rot_L.Find("pet_eyes_l").gameObject;
            node_r = bone_Rot_R.Find("pet_eyes_r").gameObject;
            renderer_L = node_l.GetComponent<MeshRenderer>();
            orgMat1 = renderer_L.material;
            renderer_R = node_r.GetComponent<MeshRenderer>();
            orgMat2 = renderer_R.material;
        }


        public override void Move(Vector3 offset)
        {
            part_l.transform.localPosition = offset;
            offset.z = -offset.z;
            part_r.transform.localPosition = offset;
        }

        public override void Rotate(Vector3 rot)
        {
            Vector3 rot_L = rot;
            Vector3 rot_R = rot_L;
            rot_R.y = -rot_R.y;
            bone_Rot_L.localEulerAngles = rot_L;
            bone_Rot_R.localEulerAngles = rot_R;

        }

        public override void Scale(Vector3 sca)
        {
            part_l.transform.localScale = sca;
            part_r.transform.localScale = sca;
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
            var bData = Es.DataTables.GetPetAvatarCommonData(id);
            if (bData == null)
            {
                action?.Invoke();
                return;
            }

            // bData.texDir + bData.texName[0] + texExt
            List<string> paths = new List<string>();
            foreach (string texName in bData.texName) {
                paths.Add(bData.texDir + texName + texExt);
            }
            LoadRes<Texture>(paths, (isSuccess, wrapperDic) => {
                if (isSuccess && curPartId == id && avatar != null)
                {
                    TakeOff();
                    node_l.SetActive(true);
                    node_r.SetActive(true);
                    wrappers = wrapperDic;
                    foreach (var wrapper in wrappers) {
                        bool isLeft = wrapper.Key.Contains("left");
                        if (isLeft) {
                            partTex1 = wrapper.Value.RetainAsset(avatar);
                            renderer_L.material = orgMat1;
                            renderer_L.material.SetTexture(baseMapName, partTex1);
                        } else {
                            partTex2 = wrapper.Value.RetainAsset(avatar);
                            renderer_R.material = orgMat2;
                            renderer_R.material.SetTexture(baseMapName, partTex2);
                        }
                    }
                    action?.Invoke();
                }
                else
                {
                    action?.Invoke();
                }
            });
        }

        //考虑资源释放
        public override void TakeOff()
        {
            if (node_l != null && node_r != null)
            {
                node_l.SetActive(false);
                node_r.SetActive(false);
            }

            wrappers = null;
        }

        public override void Reset()
        {
            base.Reset();
            renderer_L.material = orgMat1;
            renderer_L.material.SetTexture(baseMapName, partTex1);
            renderer_R.material = orgMat2;
            renderer_R.material.SetTexture(baseMapName, partTex2);
        }
    }
}
