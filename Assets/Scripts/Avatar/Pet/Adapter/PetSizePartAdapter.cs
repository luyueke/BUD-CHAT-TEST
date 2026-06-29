using System;
using Game.Avatar;
using UnityEngine;

namespace Game.Pet
{
    public class PetSizePartAdapter:PartAdapter
    {
        private GameObject avatar;
        public PetSizePartAdapter(GameObject _avatar)
        {
            avatar = _avatar;
        }

        public override void Move(Vector3 pos)
        {
        }

        public override void Rotate(Vector3 rot)
        {
        }

        public override void Scale(Vector3 sca)
        {
            avatar.transform.localScale = sca;
        }

        public override void HVScale(Vector3 sca)
        {
        }

        public override void ChangeColor(Color col)
        {
        }

        public override void AddEffect()
        {
        }

        public override void RemoveEffect()
        {
        }

        public override void PutOn(string id, Action action)
        {
        }

        public override void UGCPutOn(string id, string url, int ugcStyle = 0, Action suc = null)
        {
        }

        public override void PropSkinPutOn(string id, string url, Action suc)
        {
        }

        public override void SetAnchor(Vector3 anchor)
        {
        }

        public override void TakeOff()
        {
        }

        public override void ResetCurrentID()
        {
        }

        public override void Reset()
        {
        }

        public override void SetLeftOrRight(int leftOrRight)
        {
        }

        public override void IsUIOrSelfPlayer(bool flag)
        {
        }
    }
}