using System;
using System.Collections.Generic;
using Game.Avatar;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Pet
{

    class PetPGCEarPartAdapter : StandardPartAdapter {
        private static readonly int baseColor = Shader.PropertyToID("_BaseColor");


        private const string ear_path = "Pet001 Root/Pet001 Spine/Pet001 Spine1/pet001 Head/pet_headdress_01";
        private const string ear_parent_path = "pet_headdress_02";


        private GameObject earParent;
        private GameObject earBone;
        private List<GameObject> ugcEars;
        private AssetWrapper<GameObject> cWrapper;

        private GameObject curEar;
        private Renderer earRenderer;
        private bool isColor = false;


        public PetPGCEarPartAdapter(GameObject _avatar) : base(_avatar) {
            earBone = avatar.transform.Find(ear_path).gameObject;
            earParent = earBone.transform.Find(ear_parent_path).gameObject;
            ugcEars = earParent.GetChildNodes("ear_");
            ugcEars.ForEach(x => x.gameObject.SetActive(false));
        }

        public override void RemoveEffect() {

        }

        public override void PutOn(string id, Action action) {
            curPartId = id;
            var bData = Es.DataTables.GetPetAvatarCommonData(id);
            if (bData == null) {
                action?.Invoke();
                return;
            }
            isColor = bData.setColor;
            LoadRes<GameObject>(bData.texDir + bData.prefabName + prefabExt, (isSuc, wrapper) => {
                if (isSuc && wrapper != null && curPartId == id && avatar != null) {
                    TakeOff();
                    cWrapper = wrapper;
                    var earPrefab = wrapper.RetainAsset(avatar);
                    var hatNode = earPrefab.transform.Find(ear_path + "/" + ear_parent_path);
                    var meshNode = GetComponent<MeshRenderer>(hatNode.gameObject);
                    curEar = Object.Instantiate(meshNode.gameObject, earParent.transform);
                    lod?.SetLodMesh(curEar);
                    earRenderer = curEar.GetComponent<MeshRenderer>();
                    ChangeColor(curColor);
                    action?.Invoke();
                } else {
                    action?.Invoke();
                }
            });
        }


        public override void TakeOff() {
            if (curEar != null) {
                Object.Destroy(curEar);
                curEar = null;
            }
            cWrapper = null;
        }

        public override void Move(Vector3 pos) {
            earBone.transform.localPosition = pos;
        }

        public override void Rotate(Vector3 rot) {
            earParent.transform.localEulerAngles = rot;
        }

        public override void Scale(Vector3 sca) {
            earBone.transform.localScale = sca;
        }

        public override void ChangeColor(Color col) {
            if (isColor && earRenderer != null) {
                earRenderer.material.SetColor(baseColor, col);
            }
        }

        public override void AddEffect() {

        }
    }
}

