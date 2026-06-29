using System;
using Game.Avatar;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Pet {
    public class PetNormalPartAdapter : StandardPartAdapter {
        private static readonly int baseColor = Shader.PropertyToID("_BaseColor");


        protected virtual string PartBonePath => "";
        protected virtual string PartParentPath => "";
        protected Transform partBone;
        protected Transform partParent;
        protected bool isColor;
        protected AssetWrapper<GameObject> cWrapper;
        protected GameObject curPart;
        protected MeshRenderer partRenderer;

        public PetNormalPartAdapter(GameObject _avatar) : base(_avatar) {
        }

        public override void AddEffect() {
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
                    var partPrefab = wrapper.RetainAsset(avatar);
                    var hatNode = partPrefab.transform.Find(PartBonePath + "/" + PartParentPath);
                    var meshNode = GetComponent<MeshRenderer>(hatNode.gameObject);
                    curPart = Object.Instantiate(meshNode.gameObject, GetPartParent());
                    partRenderer = curPart.GetComponent<MeshRenderer>();
                    ChangeColor(curColor);
                    action?.Invoke();
                } else {
                    action?.Invoke();
                }
            });
        }

        public override void TakeOff() {
            if (curPart != null) {
                Object.Destroy(curPart);
                curPart = null;
            }
            cWrapper = null;
        }


        public override void Move(Vector3 pos) {
            GetPartBone().localPosition = pos;
        }

        public override void Rotate(Vector3 rot) {
            GetPartParent().localEulerAngles = rot;
        }

        public override void Scale(Vector3 sca) {
            GetPartBone().localScale = sca;
        }

        public override void ChangeColor(Color col) {
            if (isColor && partRenderer != null) {
                partRenderer.material.SetColor(baseColor, col);
            }
        }


        protected virtual Transform GetPartParent() {
            if (partParent == null) {
                partParent = GetPartBone().Find(PartParentPath);
            }
            return partParent;
        }

        protected virtual Transform GetPartBone() {
            if (partBone == null) {
                partBone = avatar.transform.Find(PartBonePath);
            }
            return partBone;
        }


    }
}
