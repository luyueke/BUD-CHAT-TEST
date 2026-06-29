using System;
using System.Collections.Generic;
using Es;
using Game.Avatar;
using Game.KinematicCharacter;
using GameData.BaseInfo;
using GameData.PgcData;
using GameData.UGCData;
using UnityEngine;

namespace Game.Pet {


    public class PetWrap : BaseAvatarWrapper {

        private PetData _data;
        public PetData Data {
            get {
                UpdateData();
                return _data;
            }
        }

        public KinematicCharacterController kinematicCharacterController;

        private Dictionary<int, CharacterPartData> avatarPartDatas;
        private Dictionary<int, PartAdapter> _adapters;
        private const string PetSizeId = "79700001";
        public PetWrap(GameObject go,bool isUI = false) {
            Avatar = go;
            isUIOrOtherPet = isUI;
            BindAvatar();
            InitFindNode();
            if (Avatar != null) {
                // var ctrl = Avatar.GetComponentInChildren<PlayerAnimationCtrl>(true);
                // if (ctrl!=null)
                // {
                //     ctrl.OnUGCPartsPlayAnim = SetUGCBoneShow;
                //     ctrl.OnUGCPartsResetAnim = ResetUGCBoneShow;
                // }
            }
        }

        public override void SetParent(Transform par, bool isNormalization)
        {
            Avatar.transform.SetParent(par,false);
            if (isNormalization) {
                Avatar.transform.localEulerAngles = Vector3.zero;
                Avatar.transform.localPosition = Vector3.zero;
            }
        }


        public CharacterPartData AddDefaultSizeData(PetData data)
        {
            var subType = UniqueType.GetPGCPetAvatar(AvatarSubType.Size);
            var partData = data.GetPartData(subType);
            if (partData == null || partData.Id == "0") {
                partData = new CharacterPartData();
                partData.Type = subType;
                partData.Sca = new Vec3(1, 1, 1);
                data.partDatas.Add(partData);
            }
            partData.Id = PetSizeId;
            return partData;
        }

        public void SetData(PetData data, Action onComplete = null) {
            if (data != null && data.partDatas != null) {
                _data = data;
                avatarPartDatas = new();
                HashSet<AvatarSubType> set = new();
                int partCount = 0;
                Action callback = () => {
                    partCount++;
                    if (partCount == data.partDatas.Count) {
                        onComplete?.Invoke();
                    }
                };

                AddDefaultSizeData(data);
                
                foreach (var partData in data.partDatas) {
                    ResourceType resourceType = UniqueType.ResourceType(partData.Type);
                    AvatarSubType avatarSubType = UniqueType.AvatarSubType(partData.Type);
                    if (resourceType == ResourceType.PGCPetAvatar) {
                        if (set.Contains(avatarSubType)) {
                            callback?.Invoke();
                            continue;
                        }

                        avatarPartDatas[partData.Type] = partData;
                        ChangePart(partData.Type, partData.Id, () => {
                            callback?.Invoke();
                        });
                        if (!partData.IsNull())
                            set.Add(avatarSubType);
                        ChangeColor(partData.Type, partData.Cr);
                        Move(partData.Type, partData.Pos);
                        Rotate(partData.Type, partData.Rot);
                        Scale(partData.Type, partData.Sca);

                    } else if (resourceType == ResourceType.UGCPetAvatar) {
                        if (set.Contains(avatarSubType)) {
                            callback?.Invoke();
                            continue;
                        }
                        avatarPartDatas[partData.Type] = partData;
                        ChangeUGCPart(partData.Type, partData.Id, partData.UId, partData.Url,partData.UgcStyle, () => {
                            callback?.Invoke();
                        });
                        if (!partData.IsNull())
                            set.Add(avatarSubType);
                        ChangeColor(partData.Type, partData.Cr);
                        Move(partData.Type, partData.Pos);
                        Rotate(partData.Type, partData.Rot);
                        Scale(partData.Type, partData.Sca);
                        SetAnchor(partData.Type, partData.CAnchor);
                    }
                }
            } else {
                onComplete?.Invoke();
            }
        }


        private void UpdateData() {
            if (avatarPartDatas != null) {
                _data.partDatas.Clear();
                foreach (var partData in avatarPartDatas.Values) {
                    _data.partDatas.Add(partData);
                }
            }
        }
        private bool isUIOrOtherPet;

        public override T GetData<T>() where T: class  {
            return _data as T;
        }


        private void BindAvatar() {
            _adapters = new Dictionary<int, PartAdapter>();
            Dictionary<string, Transform> bonesDic = InitBoneData(Avatar.transform);

            // PGC
            _adapters.Add(UniqueType.GetPGCPetAvatar(AvatarSubType.Skin), new PetPGCSkinPartAdapter(Avatar));
            _adapters.Add(UniqueType.GetPGCPetAvatar(AvatarSubType.Clothes), new PetPGCClothesPartAdapter(Avatar, bonesDic));
            _adapters.Add(UniqueType.GetPGCPetAvatar(AvatarSubType.Ear), new PetPGCEarPartAdapter(Avatar));
            _adapters.Add(UniqueType.GetPGCPetAvatar(AvatarSubType.Hats), new PetPGCHatsPartAdapter(Avatar));
            _adapters.Add(UniqueType.GetPGCPetAvatar(AvatarSubType.Tail), new PetPGCTailPartAdapter(Avatar, bonesDic));
            _adapters.Add(UniqueType.GetPGCPetAvatar(AvatarSubType.Backpack), new PetPGCBackpackPartAdapter(Avatar));
            _adapters.Add(UniqueType.GetPGCPetAvatar(AvatarSubType.Shoe), new PetPGCShoePartAdapter(Avatar, bonesDic));
            _adapters.Add(UniqueType.GetPGCPetAvatar(AvatarSubType.Glasses), new PetPGCGlassesPartAdapter(Avatar));
            _adapters.Add(UniqueType.GetPGCPetAvatar(AvatarSubType.Eyes), new PetPGCEyesPartAdapter(Avatar));
            _adapters.Add(UniqueType.GetPGCPetAvatar(AvatarSubType.Mouth), new PetPGCMouthPartAdapter(Avatar));
            _adapters.Add(UniqueType.GetPGCPetAvatar(AvatarSubType.FacePaint), new PetPGCFacePaintPartAdapter(Avatar));
            _adapters.Add(UniqueType.GetPGCPetAvatar(AvatarSubType.Scarf), new PetPGCScarfPartAdapter(Avatar));
            _adapters.Add(UniqueType.GetPGCPetAvatar(AvatarSubType.Hair), new PetPGCHairPartAdapter(Avatar, bonesDic));
            // UGC
            _adapters.Add(UniqueType.GetUGCPetAvatar(AvatarSubType.Clothes), new PetUGCClothesPartAdapter(Avatar,bonesDic));
            _adapters.Add(UniqueType.GetUGCPetAvatar(AvatarSubType.Ear), new PetUGCEarPartAdapter(Avatar));
            _adapters.Add(UniqueType.GetUGCPetAvatar(AvatarSubType.Hats), new PetUGCHatsPartAdapter(Avatar));
            _adapters.Add(UniqueType.GetUGCPetAvatar(AvatarSubType.Tail), new PetUGCTailPartAdapter(Avatar,bonesDic));
            _adapters.Add(UniqueType.GetUGCPetAvatar(AvatarSubType.Backpack), new PetUGCBackpackPartAdapter(Avatar));
            _adapters.Add(UniqueType.GetUGCPetAvatar(AvatarSubType.Shoe), new PetUGCShoePartAdapter(Avatar,bonesDic));
            _adapters.Add(UniqueType.GetUGCPetAvatar(AvatarSubType.Glasses), new PetUGCGlassesPartAdapter(Avatar));
            _adapters.Add(UniqueType.GetUGCPetAvatar(AvatarSubType.Eyes), new PetUGCEyesPartAdapter(Avatar));
            _adapters.Add(UniqueType.GetUGCPetAvatar(AvatarSubType.Mouth), new PetUGCMouthPartAdapter(Avatar));
            _adapters.Add(UniqueType.GetUGCPetAvatar(AvatarSubType.FacePaint), new PetUGCFacePaintPartAdapter(Avatar));
            _adapters.Add(UniqueType.GetUGCPetAvatar(AvatarSubType.Skin), new PetUGCSkinPartAdapter(Avatar));
            _adapters.Add(UniqueType.GetUGCPetAvatar(AvatarSubType.Scarf), new PetUGCScarfPartAdapter(Avatar));
            _adapters.Add(UniqueType.GetUGCPetAvatar(AvatarSubType.Hair), new PetUGCHairPartAdapter(Avatar));
            _adapters.Add(UniqueType.GetPGCPetAvatar(AvatarSubType.Size), new PetSizePartAdapter(Avatar));
            foreach (var adapter in _adapters) adapter.Value?.IsUIOrSelfPlayer(isUIOrOtherPet);
        }


        private Dictionary<string, Transform> InitBoneData(Transform avatar) {
            Transform[] transforms = avatar.GetComponentsInChildren<Transform>();
            var avatarBones = new Dictionary<string, Transform>();
            foreach (var transform in transforms) {
                avatarBones[transform.name] = transform;
            }

            return avatarBones;
        }


        private void InitFindNode() {

        }

        public override void ChangePart(int resType, string id, Action action = null) {
            if (string.IsNullOrEmpty(id) || id.Equals("0")) {
                TakeOff(resType);
                TakeOff(GetMutexType(resType));
                action?.Invoke();
                return;
            }

            TakeOff(GetMutexType(resType));

            if (avatarPartDatas.ContainsKey(resType)) {
                avatarPartDatas[resType].Id = id;
            } else {
                CharacterPartData partData = new CharacterPartData {
                    Type = resType,
                    Id = id
                };
                avatarPartDatas.Add(resType, partData);
            }
            GetPartAdapter(resType)?.PutOn(id, action);
        }


        public override void ChangeUGCPart(int resType, string id, string uid, string url,int ugcStyle = 0, Action action = null) {
            if (string.IsNullOrEmpty(id) || id.Equals("0")) {
                TakeOff(resType);
                TakeOff(GetMutexType(resType));
                action?.Invoke();
                return;
            }

            TakeOff(GetMutexType(resType));

            if (!avatarPartDatas.TryGetValue(resType, out var partData)) {
                partData = new CharacterPartData() {
                    Type = resType
                };
                avatarPartDatas.Add(resType, partData);
            }
            partData.Id = id;
            partData.UId = uid;
            partData.Url = url;
            partData.UgcStyle = ugcStyle;
            var templateData = Es.DataTables.GetPetClothesTemplate(id);
            if (templateData == null) return;
            if (templateData.IsProp) {
                GetPartAdapter(resType)?.PropSkinPutOn(uid, url, action);
            } else
            {
                GetPartAdapter(resType)?.UGCPutOn(id, url, ugcStyle, action);
            }

        }

        public override void ChangeUGCPart(SkinInfo skinInfo, Action action = null) {
            var resType = UniqueType.GetUGCPetAvatar((AvatarSubType)skinInfo.subType);
            var config = Es.DataTables.GetGameResData(skinInfo.templateId);
            if (config != null && config.SubType != skinInfo.subType) {
                resType = UniqueType.Get(config.ResourceType, config.SubType);
                LoggerUtils.Log("UGC穿上服务器返回的subtype 和 templateId不一致:", skinInfo.id + "|" + skinInfo.templateId + "|" + skinInfo.subType);
            }
            if (string.IsNullOrEmpty(skinInfo.templateId) || skinInfo.templateId.Equals("0")) {
                TakeOff(resType);
                TakeOff(GetMutexType(resType));
                action?.Invoke();
                return;
            }

            TakeOff(GetMutexType(resType));

            if (!avatarPartDatas.TryGetValue(resType, out var partData)) {
                partData = new CharacterPartData() {
                    Type = resType
                };
                avatarPartDatas.Add(resType, partData);

            }
            partData.Id = skinInfo.templateId;
            partData.UId = skinInfo.id;
            if (skinInfo.isProp) {
                partData.Url = skinInfo.metaDataUrl;
            } else {
                partData.Url = skinInfo.clothesUrl;
            }
            partData.UgcStyle = skinInfo.ugcStyle;

            AvatarCommonData ugcConfig = AvatarCommonData.From(skinInfo);
            if (skinInfo.isProp) {
                GetPartAdapter(resType)?.PropSkinPutOn(partData.UId, partData.Url, action);

            } else {
                GetPartAdapter(resType)?.UGCPutOn(partData.Id, partData.Url,partData.UgcStyle, action);
            }

            if (ugcConfig != null) {
                SetAnchor(resType, ugcConfig.anchor);
                Move(resType, ugcConfig.pDef);
                Rotate(resType, ugcConfig.rDef);
                Scale(resType, ugcConfig.sDef);
            }
        }




        public override void TakeOff(int resType) {
            if (avatarPartDatas!=null&&avatarPartDatas.ContainsKey(resType)) {
                avatarPartDatas[resType].Id = "0";
                avatarPartDatas[resType].Url = null;
                avatarPartDatas[resType].UgcStyle = 0;
            }

            GetPartAdapter(resType)?.ResetCurrentID();
            GetPartAdapter(resType)?.TakeOff();
        }


        /// <summary>
        /// 获取互斥的部位 pgc和ugc互斥
        /// </summary>
        /// <param name="resType"></param>
        /// <returns></returns>
        public override int GetMutexType(int resType) {
            if (UniqueType.ResourceType(resType) == ResourceType.PGCPetAvatar) {
                return UniqueType.GetUGCPetAvatar(UniqueType.AvatarSubType(resType));
            } else {
                return UniqueType.GetPGCPetAvatar(UniqueType.AvatarSubType(resType));
            }
        }




        public override void ChangeColor(int resType, string col) {
            if (avatarPartDatas.ContainsKey(resType) && !string.IsNullOrEmpty(col)) {
                avatarPartDatas[resType].Cr = col;
                Color tempColor = FormatUtils.StringToColorByHex(col);
                GetPartAdapter(resType)?.ChangeColor(tempColor);
            }
        }

        public override void Move(int resType, Vec3 pos) {
            if (avatarPartDatas.ContainsKey(resType) && pos != null) {
                avatarPartDatas[resType].Pos = pos;
                GetPartAdapter(resType)?.Move(pos);
            }
        }

        public override void Rotate(int resType, Vec3 rot) {
            if (avatarPartDatas.ContainsKey(resType) && rot != null) {
                avatarPartDatas[resType].Rot = rot;
                GetPartAdapter(resType)?.Rotate(rot);
            }
        }

        public override void Scale(int resType, Vec3 sca) {

            if (avatarPartDatas.ContainsKey(resType) && sca != null) {
                avatarPartDatas[resType].Sca = sca;
                GetPartAdapter(resType)?.Scale(sca);
            }
        }

        public override void HVScale(int resType, Vec3 sca) {
            if (avatarPartDatas.ContainsKey(resType) && sca != null) {
                avatarPartDatas[resType].CSca = sca;
                GetPartAdapter(resType)?.HVScale(sca);
            }
        }

        public override void SetAnchor(int resType, Vec3 anchor) {
            if (avatarPartDatas.ContainsKey(resType) && anchor != null) {
                avatarPartDatas[resType].CAnchor = anchor;
                GetPartAdapter(resType)?.SetAnchor(anchor);
            }
        }

        public PartAdapter GetPartAdapter(int id) {
            return _adapters.ContainsKey(id) ? _adapters[id] : null;
        }

        public override CharacterPartData GetPartData(int type) {
            foreach (var kv in avatarPartDatas)
            {
                if (kv.Key == type) return kv.Value;
            }
            return null;
        }

        public override void RefreshAvatar<T>(T data, Action complete = null) {
            var petData = data as PetData;
            if (petData == null)
            {
                complete?.Invoke();
                return;
            }
            SetData(petData.Clone(),complete);
        }

        public override void RsetFaceMat()
        {
            _adapters[UniqueType.GetPGCPetAvatar(AvatarSubType.Eyes)].Reset();
            _adapters[UniqueType.GetPGCPetAvatar(AvatarSubType.Mouth)].Reset();
        }

        public override Transform GetBandNode(int type)
        {

            switch (type)
            {
                case (int)BodyNode.RightHand:
                    return Avatar.transform.Find(BodyPath.RIGHT_HAND_PATH);
                case (int)BodyNode.LeftHand:
                    return Avatar.transform.Find(BodyPath.LEFT_HAND_PATH);
                case (int)BodyNode.BackNode:
                    return Avatar.transform.Find(BodyPath.BACK_PATH);
                case (int)BodyNode.LEffectNode:
                    return Avatar.transform.Find(BodyPath.LEFT_EFFECT_PATH);
                case (int)BodyNode.REffectNode:
                    return Avatar.transform.Find(BodyPath.RIGHT_EFFECT_PATH);
                case (int)BodyNode.BackDeckNode:
                    return Avatar.transform.Find(BodyPath.BAG_PATH);
            }
            return null;
        }

        public void DestorySelf()
        {
            if (Avatar != null)
            {
                GameObject.Destroy(Avatar);
            }
        }
    }


}
