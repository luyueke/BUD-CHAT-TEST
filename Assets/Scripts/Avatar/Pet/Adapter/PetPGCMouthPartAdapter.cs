using System;
using Game.Avatar;
using UnityEngine;

namespace Game.Pet {
    public class PetPGCMouthPartAdapter : FaceSinglePartAdapter {


        private Transform mouthBone;
        private Transform mouthParent;

        public PetPGCMouthPartAdapter(GameObject _avatar) : base(_avatar) {

            var head =  GameObjectEx.FindChildByName(_avatar, "Pet001 Root/Pet001 Spine/Pet001 Spine1/pet001 Head");

            mouthBone = GameObjectEx.FindChildByName(head, "pet_mouth_01");
            mouthParent = GameObjectEx.FindChildByName(mouthBone, "pet_mouth_02");
            partRenderer = mouthParent.Find("pet_mouth").gameObject.GetComponent<MeshRenderer>();
            orgMat = partRenderer.material;
            curMat = orgMat;
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
            LoadRes<Texture>(bData.texDir + bData.texName[0] + texExt, (isSuc, warpper) =>
            {
                if (isSuc && warpper != null && curPartId == id && avatar != null)
                {
                    TakeOff();
                    cWarpper = warpper;
                    partRenderer.gameObject.SetActive(true);
                    partTex = warpper.RetainAsset(avatar);
                    orgMat.SetTexture("_BaseMap", partTex);
                    action?.Invoke();
                }
                else
                {
                    action?.Invoke();
                }
            });
        }


        public override void Move(Vector3 pos)
        {
            mouthBone.localPosition = pos;
        }

        public override void Rotate(Vector3 rot)
        {
            mouthParent.localEulerAngles = rot;
        }

        public override void Scale(Vector3 sca)
        {
            mouthBone.localScale = sca;
        }

        public override void Reset()
        {
            base.Reset();
            orgMat.SetTexture("_BaseMap", partTex);
        }
    }
}
