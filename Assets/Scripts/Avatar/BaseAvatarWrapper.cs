using System;
using GameData.BaseInfo;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Avatar {
    public abstract class BaseAvatarWrapper {


        public GameObject Avatar { get; protected set; }

        public GameObject CustomAvatar { get;set;  }

        public virtual void SetParent(Transform par, bool isNormalization) {
            Avatar.transform.SetParent(par);
            if (isNormalization) {
                Avatar.transform.localScale = Vector3.one;
                Avatar.transform.localEulerAngles = Vector3.zero;
                Avatar.transform.localPosition = Vector3.zero;
            }
        }

        public virtual void ChangePart(int resType, string id, Action action = null)
        {
        }

        public virtual void ChangeShape(int resType, int id)
        {

        }

        public virtual void ChangeColor(int resType, string col)
        {
        }

        public virtual void Move(int resType, Vec3 pos)
        {

        }

        public virtual void Rotate(int resType, Vec3 rot)
        {
        }

        public virtual void Scale(int resType, Vec3 sca)
        {
        }

        public virtual void HVScale(int resType, Vec3 sca)
        {
        }

        public virtual void SetLeftOrRight(int resType, int leftOrRight)
        {

        }

        public virtual void SetAnchor(int resType, Vec3 anchor)
        {
        }

        public virtual void ChangeUGCPart(int resType, string id, string uid, string url, int ugcStyle = 0,
            Action action = null)
        {

        }

        public virtual void ChangeUGCPart(SkinInfo skinInfo, Action action = null) {

        }

        public virtual void ChangeUGCVehicle(int resType, string id, string uid, string url, Action action = null)
        {

        }

        public virtual void ChangeUGCVehicle(string uid, VehicleInfo vehicleInfo, Action action = null)
        {

        }

        public abstract int GetMutexType(int resType);

        public virtual void TakeOff(int resType)
        {
        }

        public virtual void TakeOffSpecialSkin()
        {

        }

        public virtual CharacterPartData GetPartData(int type)
        {
            return null;
        }

        /// <summary>
        /// 更新当前形象数据
        /// </summary>
        /// <param name="data"></param>
        public virtual void RefreshAvatar<T>(T data,Action complete = null)
        {
        }

        public virtual T GetData<T>() where T: class {
            return default;
        }


        public virtual Transform GetBandNode(int type)
        {
            return default;
        }

        public virtual void RsetFaceMat()
        {

        }
    }
}
