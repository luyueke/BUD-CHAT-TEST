using System;
using System.Collections;
using System.Collections.Generic;
using BUD.AnimPose;
using Es;
using Game.Audio;
using Game.AvatarTool;
using Game.Config;
using GameData.BaseInfo;
using GameData.Manager;
using GameData.PgcData;
using GameData.UGCData;
using Message;
using Newtonsoft.Json;
using UnityEngine;
using static Game.KinematicCharacter.KinematicCharacterMotor;

namespace Game.Avatar
{
    public class CharacterWrap : BaseAvatarWrapper
    {


        //拾取道具拾起点
        private Transform pickPos;
        //拾取道具父节点
        private Transform pickNode;
        //拾取道具父节点
        private Transform foodNode;

        // 伙伴上车前的原始父节点：下车时还原到此（self-buddy=AIBuddyAvatarController，map-buddy=AIBuddyInMapBehaviour）。
        // 写死挂回 AIBuddyAvatarController 只对 self-buddy 正确，map-buddy 会脱离其放置点 behaviour，导致头顶交互图标的射线反查失效。
        private Transform _buddyVehicleOriginalParent;

        private CharacterData _chaData;
        public CharacterData ChaData
        {
            get
            {
                UpdateCharacterData();
                return _chaData;
            }
        }



        private Dictionary<int, CharacterPartData> avatarPartDatas;
        private Dictionary<int, PartAdapter> _adapters;
        private List<int> filterColors = new List<int>() { UniqueType.GetAvatar(AvatarSubType.Glasses), UniqueType.GetAvatar(AvatarSubType.Hats) };

        private bool isUIOrSelfPlayer;
        public CharacterWrap(GameObject go, bool isOtherPlayer = false)
        {
            Avatar = go;
            if (isOtherPlayer) Avatar.GetOrAddComponent<CharacterLOD>();
            isUIOrSelfPlayer = !isOtherPlayer;
            BindAvatar();
            InitFindNode();
            if (Avatar != null)
            {
                var ctrl = Avatar.GetComponentInChildren<PlayerAnimationCtrl>(true);
                if (ctrl != null)
                {
                    ctrl.OnUGCPartsPlayAnim = SetUGCBoneShow;
                    ctrl.OnUGCPartsResetAnim = ResetUGCBoneShow;
                }
            }
        }


        public override CharacterPartData GetPartData(int type)
        {
            foreach (var kv in avatarPartDatas)
            {
                if (kv.Key == type) return kv.Value;
            }
            return null;
        }

        public override T GetData<T>() where T : class
        {
            UpdateCharacterData();
            return _chaData as T;
        }

        //过滤眼镜、帽子中多余颜色值
        public void FilterExcessColors()
        {
            if (avatarPartDatas != null)
            {
                foreach (var partData in avatarPartDatas.Values)
                {
                    if (filterColors.Contains(partData.Type))
                    {
                        var esData = Es.DataTables.GetAvatarCommonData(partData.Id);
                        if (!esData.setColor)
                        {
                            partData.Cr = null;
                        }
                    }
                }
            }
        }

        private void UpdateCharacterData()
        {
            if (avatarPartDatas != null)
            {
                _chaData.partDatas.Clear();
                foreach (var partData in avatarPartDatas.Values)
                {
                    _chaData.partDatas.Add(partData);
                }
            }
        }

        private void InitFindNode()
        {
            pickNode = Avatar.transform.Find(BodyPath.PICK_HAND_PATH);
            foodNode = Avatar.transform.Find(BodyPath.PICK_FOOD_PATH);
        }

        /// <summary>
        /// 更新当前形象数据
        /// </summary>
        /// <param name="data"></param>
        public override void RefreshAvatar<T>(T data, Action complete = null)
        {
            var characterData = data as CharacterData;
            if (characterData == null)
            {
                complete?.Invoke();
                return;
            }
            SetCharacterData(characterData.Clone(), complete);
        }

        public void PutOnDefaultClothes(CharacterData avatarData)
        {
            if (avatarData == null)
            {
                return;
            }

            int resType = UniqueType.GetAvatar(AvatarSubType.Clothes);
            bool isNotPgcClothes = IsNotClothesData(resType, avatarData);

            int ugcResType = UniqueType.GetUgcAvatar(AvatarSubType.Clothes);
            bool isNotUgcClothes = IsNotClothesData(ugcResType, avatarData);

            if (isNotPgcClothes && isNotUgcClothes)
            {
                ChangePart(resType, "10400197");
            }
        }

        private bool IsNotClothesData(int resType, CharacterData avatarData)
        {
            var ugcPartData = avatarData.partDatas.Find(x => x.Type == resType);
            return ugcPartData == null || ugcPartData.Id.Equals("0");
        }

        public bool HasVehicle()
        {
            return _chaData.vehicleData != null;
        }



        public void SetCharacterData(CharacterData data, Action complete = null)
        {
            if (data != null && data.partDatas != null)
            {
                _chaData = data;
                avatarPartDatas = new();
                HashSet<AvatarSubType> set = new();
                int partCount = 0;
                Action callback = () =>
                {
                    partCount++;
                    if (partCount == data.partDatas.Count)
                    {
                        complete?.Invoke();
                    }
                };

                var shapCtrl = Avatar.GetComponentInChildren<CustomBodyTypeController>();
                if (shapCtrl != null)
                {
                    CustomBodyTypeController.BodyType type = data.bodyType == 0 ? CustomBodyTypeController.BodyType.None : (CustomBodyTypeController.BodyType)data.bodyType;
                    shapCtrl.ApplyBodyType(type);
                }
                foreach (var partData in data.partDatas)
                {
                    ResourceType resourceType = UniqueType.ResourceType(partData.Type);
                    AvatarSubType avatarSubType = UniqueType.AvatarSubType(partData.Type);
                    if(avatarSubType == AvatarSubType.SpecialSkin)
                    {
                        if (set.Contains(avatarSubType))
                        {
                            callback?.Invoke();
                            continue;
                        }
                        var isShowSpecialSkin = true;
                        foreach (var dataInfo in data.partDatas)
                        {
                            ResourceType dataType = UniqueType.ResourceType(dataInfo.Type);
                            if(dataType == ResourceType.UgcVehicle)
                            {
                                if(!string.IsNullOrEmpty(dataInfo.UId) || !string.IsNullOrEmpty(dataInfo.Id))
                                {
                                    isShowSpecialSkin = false;
                                }
                                break;
                            }
                            else{
                                isShowSpecialSkin = true;
                            }
                        }
                        if(!isShowSpecialSkin){
                            TakeOffSpecialSkin(partData.Type);
                            callback?.Invoke();
                            continue;
                        }
                        else{
                            avatarPartDatas[partData.Type] = partData;
                            ChangePart(partData.Type, partData.Id, () =>
                            {
                                callback?.Invoke();
                            });
                            if (!partData.IsNull()) set.Add(avatarSubType);
                            ChangeColor(partData.Type, partData.Cr);
                            Move(partData.Type, partData.Pos);
                            Rotate(partData.Type, partData.Rot);
                            Scale(partData.Type, partData.Sca);
                            SetLeftOrRight(partData.Type, partData.LRType);
                        }
                    }
                    else if (resourceType == ResourceType.Avatar)
                    {
                        if (set.Contains(avatarSubType))
                        {
                            callback?.Invoke();
                            continue;
                        }
                        avatarPartDatas[partData.Type] = partData;
                        ChangePart(partData.Type, partData.Id, () =>
                        {
                            callback?.Invoke();
                        });
                        if (!partData.IsNull()) set.Add(avatarSubType);
                        ChangeColor(partData.Type, partData.Cr);
                        Move(partData.Type, partData.Pos);
                        Rotate(partData.Type, partData.Rot);
                        Scale(partData.Type, partData.Sca);
                        SetLeftOrRight(partData.Type, partData.LRType);

                    }
                    else if (resourceType == ResourceType.UgcAvatar)
                    {
                        if (set.Contains(avatarSubType))
                        {
                            callback?.Invoke();
                            continue;
                        }
                        avatarPartDatas[partData.Type] = partData;
                        ChangeUGCPart(partData.Type, partData.Id, partData.UId, partData.Url, partData.UgcStyle, () =>
                        {
                            callback?.Invoke();
                        });
                        if (!partData.IsNull()) set.Add(avatarSubType);
                        ChangeColor(partData.Type, partData.Cr);
                        Move(partData.Type, partData.Pos);
                        Rotate(partData.Type, partData.Rot);
                        Scale(partData.Type, partData.Sca);
                        SetLeftOrRight(partData.Type, partData.LRType);
                        SetAnchor(partData.Type, partData.CAnchor);
                    }
                    else if(resourceType == ResourceType.Vehicle)
                    {
                        //if (set.Contains(avatarSubType))
                        //{
                        //    callback?.Invoke();
                        //    continue;
                        //}
                        avatarPartDatas[partData.Type] = partData;
                        ChangeUGCVehicle(partData.Type, partData.Id, partData.UId, partData.Url, () =>
                        {
                            GameObject rayUseObj = new GameObject("rayUseVehicleObj");
                            rayUseObj.transform.SetParent(Avatar.transform);
                            rayUseObj.transform.localPosition = new Vector3(0, .5f, 0);
                            rayUseObj.transform.localRotation = Quaternion.identity;
                            rayUseObj.transform.localScale = Vector3.one * 0.1f;
                            var boxCollider = rayUseObj.AddComponent<BoxCollider>();
                            boxCollider.isTrigger = true;
                            rayUseObj.layer = LayerMask.NameToLayer("Model");
                            callback?.Invoke();
                        });
                        if (!partData.IsNull()) set.Add(avatarSubType);
                        ChangeColor(partData.Type, partData.Cr);
                        Move(partData.Type, partData.Pos);
                        Rotate(partData.Type, partData.Rot);
                        Scale(partData.Type, partData.Sca);
                        SetLeftOrRight(partData.Type, partData.LRType);
                        SetAnchor(partData.Type, partData.CAnchor);
                        
                    }
                    else if(resourceType == ResourceType.UgcVehicle)
                    {
                        avatarPartDatas[partData.Type] = partData;
                        ChangeUGCVehicle(partData.Type, partData.Id, partData.UId, partData.Url, () =>
                        {
                            GameObject rayUseObj = new GameObject("rayUseVehicleObj");
                            rayUseObj.transform.SetParent(Avatar.transform);
                            rayUseObj.transform.localPosition = new Vector3(0, .5f, 0);
                            rayUseObj.transform.localRotation = Quaternion.identity;
                            rayUseObj.transform.localScale = Vector3.one * 0.1f;
                            var boxCollider = rayUseObj.AddComponent<BoxCollider>();
                            boxCollider.isTrigger = true;
                            rayUseObj.layer = LayerMask.NameToLayer("Model");
                            callback?.Invoke();
                        });
                        if (!partData.IsNull()) set.Add(avatarSubType);
                        ChangeColor(partData.Type, partData.Cr);
                        partData.Rot = partData.Rot - Avatar.transform?.parent?.localRotation.eulerAngles;
                        //Rotate(partData.Type, partData.Rot);
                        Move(partData.Type, partData.Pos);
                        Scale(partData.Type, partData.Sca);
                        SetLeftOrRight(partData.Type, partData.LRType);
                        SetAnchor(partData.Type, partData.CAnchor);
                    }
                }
            }
            else
            {
                complete?.Invoke();
            }
        }

        private void BindAvatar()
        {
            _adapters = new Dictionary<int, PartAdapter>();
            Dictionary<string, Transform> bonesDic = InitBoneData(Avatar.transform);
            var head = Avatar.transform.Find(BodyPath.BONE_PATH).gameObject;
            // PGC
            _adapters.Add(UniqueType.GetAvatar(AvatarSubType.Brow), new BrowPartAdapter(head));
            _adapters.Add(UniqueType.GetAvatar(AvatarSubType.Eyes), new EyePartAdapter(Avatar, head));
            _adapters.Add(UniqueType.GetAvatar(AvatarSubType.Mouth), new MousePartAdapter(head));
            _adapters.Add(UniqueType.GetAvatar(AvatarSubType.Nose), new NosePartAdapter(head));
            _adapters.Add(UniqueType.GetAvatar(AvatarSubType.Blush), new BlushPartAdapter(head));
            _adapters.Add(UniqueType.GetAvatar(AvatarSubType.Hand), new HandPartAdapter(Avatar));
            _adapters.Add(UniqueType.GetAvatar(AvatarSubType.Glove), new GlovePartAdapter(Avatar, bonesDic));
            _adapters.Add(UniqueType.GetAvatar(AvatarSubType.Clothes), new ClothesPartAdapter(Avatar, bonesDic));
            _adapters.Add(UniqueType.GetAvatar(AvatarSubType.Shoe), new ShoePartAdapter(Avatar, bonesDic));
            _adapters.Add(UniqueType.GetAvatar(AvatarSubType.Hair), new HairPartAdapter(Avatar, bonesDic));
            _adapters.Add(UniqueType.GetAvatar(AvatarSubType.Hats), new HatsPartAdapter(Avatar));
            _adapters.Add(UniqueType.GetAvatar(AvatarSubType.Scarf), new ScarfPartAdapter(Avatar, bonesDic));
            _adapters.Add(UniqueType.GetAvatar(AvatarSubType.Backpack), new BackBagPartAdapter(Avatar, bonesDic));
            _adapters.Add(UniqueType.GetAvatar(AvatarSubType.Cape), new CapeBagPartAdapter(Avatar, bonesDic));
            _adapters.Add(UniqueType.GetAvatar(AvatarSubType.Crossbody), new CrossBagPartAdapter(Avatar, bonesDic));
            _adapters.Add(UniqueType.GetAvatar(AvatarSubType.Effect), new EffectPartAdapter(Avatar));
            _adapters.Add(UniqueType.GetAvatar(AvatarSubType.Belt), new BeltPartAdapter(Avatar, bonesDic));
            _adapters.Add(UniqueType.GetAvatar(AvatarSubType.Glasses), new GlassesPartAdapter(Avatar, bonesDic));
            _adapters.Add(UniqueType.GetAvatar(AvatarSubType.Visor), new VisorPartAdapter(Avatar, bonesDic));
            _adapters.Add(UniqueType.GetAvatar(AvatarSubType.FacePaint), new FacePaintPartAdapter(Avatar));
            _adapters.Add(UniqueType.GetAvatar(AvatarSubType.Earring), new EarringPartAdapter(Avatar));
            _adapters.Add(UniqueType.GetAvatar(AvatarSubType.Skin), new SkinPartAdapter(Avatar));
            _adapters.Add(UniqueType.GetAvatar(AvatarSubType.Head), new HeadPartAdapter(Avatar));
            _adapters.Add(UniqueType.GetAvatar(AvatarSubType.MusicalInstrument), new InstrumentPartAdapter(Avatar, bonesDic));
            _adapters.Add(UniqueType.GetAvatar(AvatarSubType.SpecialSkin), new SpecialSkinPartAdapter(Avatar, head, bonesDic));
            // UGC
            _adapters.Add(UniqueType.GetUgcAvatar(AvatarSubType.Clothes), new UGCClothesPartAdapter(Avatar, bonesDic));
            _adapters.Add(UniqueType.GetUgcAvatar(AvatarSubType.Backpack), new UGCBackBagPartAdapter(Avatar, bonesDic));
            _adapters.Add(UniqueType.GetUgcAvatar(AvatarSubType.Hand), new UGCHandPartAdapter(Avatar, bonesDic));
            _adapters.Add(UniqueType.GetUgcAvatar(AvatarSubType.Hats), new UGCHatsPartAdapter(Avatar));
            _adapters.Add(UniqueType.GetUgcAvatar(AvatarSubType.Shoe), new UGCShoePartAdapter(Avatar, bonesDic));
            _adapters.Add(UniqueType.GetUgcAvatar(AvatarSubType.Eyes), new UGCEyePartAdapter(Avatar, head));
            _adapters.Add(UniqueType.GetUgcAvatar(AvatarSubType.Glasses), new UGCGlassesPartAdapter(Avatar, bonesDic));
            _adapters.Add(UniqueType.GetUgcAvatar(AvatarSubType.Mouth), new UGCMouthPartAdapter(head));
            _adapters.Add(UniqueType.GetUgcAvatar(AvatarSubType.FacePaint), new UGCFacePaintPartAdapter(head));
            _adapters.Add(UniqueType.GetUgcAvatar(AvatarSubType.Hair), new UGCHairPartAdapter(Avatar, bonesDic));
            _adapters.Add(UniqueType.GetUgcAvatar(AvatarSubType.MusicalInstrument), new UGCInstrumentPartAdapter(Avatar, bonesDic));
            //载具
            _adapters.Add(UniqueType.GetUgcVehicle(VehicleSubType.SingleVehicle), new UGCVehiclePartAdapter(Avatar, bonesDic));
            _adapters.Add(UniqueType.GetUgcVehicle(VehicleSubType.DoubleVehicle), new UGCVehiclePartAdapter(Avatar, bonesDic));
            foreach (var adapter in _adapters) adapter.Value?.IsUIOrSelfPlayer(isUIOrSelfPlayer);
        }

        //当佩戴部分ugc部位时，播放表情需要控制ugc部分隐藏，显示k帧动画
        public void SetUGCBoneShow(string clipName)
        {
            if (_adapters != null && _adapters.ContainsKey(UniqueType.GetUgcAvatar(AvatarSubType.Eyes)))
            {
                var adapter = _adapters[UniqueType.GetUgcAvatar(AvatarSubType.Eyes)] as UGCEyePartAdapter;
                adapter.SetUGCEyeBoneShow(clipName);
            }
            if (_adapters != null && _adapters.ContainsKey(UniqueType.GetUgcAvatar(AvatarSubType.Mouth)))
            {
                var adapter = _adapters[UniqueType.GetUgcAvatar(AvatarSubType.Mouth)] as UGCMouthPartAdapter;
                adapter.SetUGCMouthBoneShow(clipName);
            }
        }

        public void ResetUGCBoneShow()
        {
            if (_adapters != null && _adapters.ContainsKey(UniqueType.GetUgcAvatar(AvatarSubType.Eyes)))
            {
                var adapter = _adapters[UniqueType.GetUgcAvatar(AvatarSubType.Eyes)] as UGCEyePartAdapter;
                adapter.ResetUGCEyeBoneShow();
            }
            if (_adapters != null && _adapters.ContainsKey(UniqueType.GetUgcAvatar(AvatarSubType.Mouth)))
            {
                var adapter = _adapters[UniqueType.GetUgcAvatar(AvatarSubType.Mouth)] as UGCMouthPartAdapter;
                adapter.ResetUGCMouthBoneShow();
            }
        }
        public PartAdapter GetPartAdapter(int id)
        {
            return _adapters.ContainsKey(id) ? _adapters[id] : null;
        }

        private Dictionary<string, Transform> InitBoneData(Transform avatar)
        {
            Transform[] transforms = avatar.GetComponentsInChildren<Transform>();
            var avatarbones = new Dictionary<string, Transform>();
            foreach (var transform in transforms)
            {
                avatarbones[transform.name] = transform;
            }

            return avatarbones;
        }


        public override void ChangePart(int resType, string id, Action action = null)
        {
            var animationCtrl = Avatar.GetComponent<PlayerAnimationCtrl>();
            if (string.IsNullOrEmpty(id) || id.Equals("0"))
            {
                TakeOff(resType);
                TakeOff(GetMutexType(resType));
                if (animationCtrl != null)
                {
                    animationCtrl.RefreshSkinInfo(resType, id);
                }
                action?.Invoke();
                return;
            }

            //var specialConfig = DataTables.GetSpecialSkinConfig(id);
            //bool specialSkin = specialConfig != null;
            //// 特殊皮肤（走跑跳互次）
            //if (specialSkin) TakeOffSpecialSkin();

            TakeOff(GetMutexType(resType));

            if (avatarPartDatas.ContainsKey(resType))
            {
                avatarPartDatas[resType].Id = id;
            }
            else
            {
                CharacterPartData data = new CharacterPartData();
                data.Type = resType;
                data.Id = id;
                avatarPartDatas.Add(resType, data);
            }

            // 对乐器类型：用 PGC 配置值还原骨骼 transform，与试衣间处理方式一致
            // 防止 UGC 乐器遗留的 Scale/Move/Rotate 污染 PGC 乐器的显示（大小/位置不对）
            if ((AvatarSubType)UniqueType.AvatarSubType(resType) == AvatarSubType.MusicalInstrument)
            {
                var bData = Es.DataTables.GetAvatarCommonData(id);
                if (bData != null)
                {
                    Move(resType, bData.pDef);
                    Rotate(resType, bData.rDef);
                    Scale(resType, bData.sDef);
                }
                else
                {
                    // 无配置时仅重置 Scale 为默认（1,1,1），不改变骨骼位置/旋转
                    GetPartAdapter(resType)?.Scale(Vector3.one);
                }
            }

            if (animationCtrl != null)
            {
                animationCtrl.RefreshSkinInfo(resType, id);
            }
            GetPartAdapter(resType)?.PutOn(id, action);
        }

        /// <summary>
        /// 获取互斥的部位 pgc和ugc互斥
        /// </summary>
        /// <param name="resType"></param>
        /// <returns></returns>
        public override int GetMutexType(int resType)
        {
            if (UniqueType.ResourceType(resType) == ResourceType.Avatar)
            {
                return UniqueType.GetUgcAvatar((AvatarSubType)UniqueType.AvatarSubType(resType));
            }
            else
            {
                return UniqueType.GetAvatar((AvatarSubType)UniqueType.AvatarSubType(resType));
            }
        }

        public override void ChangeUGCPart(int resType, string id, string uid, string url, int ugcStyle = 0, Action action = null)
        {
            if (string.IsNullOrEmpty(id) || id.Equals("0"))
            {
                TakeOff(resType);
                TakeOff(GetMutexType(resType));
                action?.Invoke();
                return;
            }

            TakeOff(GetMutexType(resType));

            if (!avatarPartDatas.TryGetValue(resType, out var data))
            {
                data = new CharacterPartData()
                {
                    Type = resType
                };
                avatarPartDatas.Add(resType, data);
            }
            data.Id = id;
            data.UId = uid;
            data.Url = url;
            data.UgcStyle = ugcStyle;
            var templateData = Es.DataTables.GetClothesTemplate(id);

            if (templateData.IsProp)
            {
                GetPartAdapter(resType)?.PropSkinPutOn(uid, url, action);
            }
            else
            {
                GetPartAdapter(resType)?.UGCPutOn(id, url, ugcStyle, action);
            }


        }

        public override void ChangeUGCPart(SkinInfo skinInfo, Action action = null)
        {
            var resType = UniqueType.GetUgcAvatar((AvatarSubType)skinInfo.subType);
            var config = Es.DataTables.GetGameResData(skinInfo.templateId);
            if (config != null && config.SubType != skinInfo.subType)
            {
                resType = UniqueType.Get(config.ResourceType, config.SubType);
                LoggerUtils.Log("UGC穿上服务器返回的subtype 和 templateId不一致:", skinInfo.id + "|" + skinInfo.templateId + "|" + skinInfo.subType);
            }
            if (string.IsNullOrEmpty(skinInfo.templateId) || skinInfo.templateId.Equals("0"))
            {
                TakeOff(resType);
                TakeOff(GetMutexType(resType));
                action?.Invoke();
                return;
            }

            TakeOff(GetMutexType(resType));

            if (!avatarPartDatas.TryGetValue(resType, out var data))
            {
                data = new CharacterPartData()
                {
                    Type = resType
                };
                avatarPartDatas.Add(resType, data);

            }
            data.Id = skinInfo.templateId;
            data.UId = skinInfo.id;
            if (skinInfo.isProp)
            {
                data.Url = skinInfo.metaDataUrl;
            }
            else
            {
                data.Url = skinInfo.clothesUrl;
            }
            data.UgcStyle = skinInfo.ugcStyle;

            AvatarCommonData ugcConfig = AvatarCommonData.From(skinInfo);
            if (skinInfo.isProp)
            {
                GetPartAdapter(resType)?.PropSkinPutOn(data.UId, data.Url, action);

            }
            else
            {
                GetPartAdapter(resType)?.UGCPutOn(data.Id, data.Url, data.UgcStyle, action);
            }

            if (ugcConfig != null)
            {
                SetAnchor(resType, ugcConfig.anchor);
                Move(resType, ugcConfig.pDef);
                Rotate(resType, ugcConfig.rDef);
                Scale(resType, ugcConfig.sDef);
                SetLeftOrRight(resType, ugcConfig.leftRightType);
            }
        }

        public override void ChangeUGCVehicle(int resType, string id, string uid, string url, Action action = null)
        {
            base.ChangeUGCVehicle(resType, id, uid, url, action);
            TakeOff(resType);

            if (!avatarPartDatas.TryGetValue(resType, out var data))
            {
                data = new CharacterPartData()
                {
                    Type = resType
                };
                avatarPartDatas.Add(resType, data);
            }
            data.Id = id;
            data.UId = uid;
            data.Url = url;

            GetPartAdapter(resType)?.PropSkinPutOn(uid, url, action);
        }

        public override void ChangeUGCVehicle(string uid, VehicleInfo vehicleInfo, Action action = null)
        {
            base.ChangeUGCVehicle(uid, vehicleInfo, action);
            if(vehicleInfo == null)
            {
                return;
            }
            CreateVehicle(uid, vehicleInfo, action);
        }

        /// <summary>
        /// 创建载具
        /// </summary>
        /// <param name="vehicleInfo"></param>
        /// <param name="action"></param>
        public void CreateVehicle(string uid, VehicleInfo vehicleInfo, Action action = null)
        {
            _chaData.AddVehicleData(vehicleInfo);
            _chaData.vehicleData = vehicleInfo;
            RefreshAvatar(_chaData, ResetVehiclePos);
            var animationCtrl = Avatar.GetComponent<PlayerAnimationCtrl>();
            animationCtrl.PlayerChangeVehicleForUICharacer();
            if(uid == AccountDataManager.Inst.UserInfo.uid && AvatarController.Inst.SelfController != null)
            {
                AvatarController.Inst.SelfController.Motor.IsDriveVehicle = true;
                AvatarController.Inst.SelfController.Motor.CurUGCVehicleStatus = UGCVehicleStatus.Drive;
                AvatarController.Inst.SelfController.Motor.SetVehicleHeight(vehicleInfo.vehicleHeight, true);
                GameDataManager.Inst.mapGlobalData.vehicleInfo = vehicleInfo;
            }
            // var audio = vehicleInfo.vehicleAudio?.starWwise;
            // if (audio != null)
            // {
            //     AkSoundManager.Inst.PlaySound(audio.group, audio.switchs, audio.wwise3P, Avatar);
            // }
            action?.Invoke();
            var animator = Avatar.transform.parent.parent.GetComponent<Animator>();
            if(animator != null)
            {
                if(vehicleInfo.endAniType != 0)
                {
                    animator.enabled = true;
                    animator.Play(string.Format("ugc_car0{0}_idle", vehicleInfo.endAniType));
                }
                else
                {
                    animator.enabled = false;
                }
            }
            var driverStateCtrl = Avatar.gameObject.GetComponent<PlayerStateController>();
            if (driverStateCtrl != null && !driverStateCtrl.IsSelf)
            {
                driverStateCtrl.PlayerKCCtrl.Motor.IsDriveVehicle = true;
                driverStateCtrl.PlayerKCCtrl.Motor.CurUGCVehicleStatus = UGCVehicleStatus.Drive;
                driverStateCtrl.PlayerKCCtrl.Motor.SetVehicleHeight(vehicleInfo.vehicleHeight, true);
            }
        }

        public void ResetVehiclePos()
        {
            CoroutineManager.Inst.StartCoroutine(CoDelayCallSetAvatarActive());
        }
        private IEnumerator CoDelayCallSetAvatarActive()
        {
            VehicleInfo vehicleInfo = _chaData.vehicleData == null ? ChaData.vehicleData : _chaData.vehicleData;
            if(vehicleInfo == null)
            {
                yield break;
            }

            yield return new WaitForSeconds(0.5f);
            
            for (int i = 0; i < Avatar.transform.childCount; i++)
            {
               if (Avatar.transform.GetChild(i).name == "dialogpos" || Avatar.transform.GetChild(i).name == "rayUseVehicleObj" 
               || Avatar.transform.GetChild(i).name == "AvatarNode")
               {
                   continue;
               }
               if(Avatar.transform.GetChild(i).name == "FEAT_1P_rehandlingselect_SFX_PREFAB(Clone)")//这里暂时先写死跳过镜子节点，不进行处理。避免镜子被卸载载具时候显示出来
               {
                    continue;
               }
               Avatar.transform.GetChild(i).gameObject.SetActive(false);
            }

            CharacterData data = ChaData.Clone();
            data.RemoveVehicleData();

            var wrap = AvatarController.Inst.CreateGameAvatarWithIKController(data, Avatar.transform);

            var resType = UniqueType.GetAvatar(AvatarSubType.SpecialSkin);
            var adapter = wrap.GetPartAdapter(resType) as SpecialSkinPartAdapter;
            if(adapter != null)
            {
                adapter.GetCurrentLoadedObj()?.gameObject?.SetActive(false);
            }

            ApplyUgcPoseWithSeatPreserved(
                wrap,
                vehicleInfo.curPoseData,
                new Vector3(0, 0.5f, 0),
                vehicleInfo?.detailInfo?.rDef
            );

            //Avatar.transform.Find("vehiclepos").localScale = vehicleInfo.detailInfo.sDef;
        }

        private static void DisableAnimator(GameObject go)
        {
            if (go == null)
            {
                return;
            }
            var ani = go.GetComponent<Animator>();
            if (ani != null)
            {
                ani.enabled = false;
            }
        }

        /// <summary>
        /// 应用UGC姿势数据
        /// </summary>
        private void ApplyUgcPoseWithSeatPreserved(CharacterWrap wrap, string poseJson, Vector3 customAvatarBaseOffset, Vector3? seatEulerRotation)
        {
            if (wrap?.Avatar == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(poseJson))
            {
                return;
            }
            //DisableAnimator(wrap.Avatar);

            var optNode = wrap.Avatar.transform.parent;
            var optLocalPos = optNode != null ? optNode.localPosition : Vector3.zero;
            var optLocalScale = optNode != null ? optNode.localScale : Vector3.one;

            var ikController = wrap.Avatar.GetComponent<AnimIKController>();
            if (ikController != null)
            {
                ikController.ChangeAnimResType(AnimResType.UGC);
                var frameData = JsonConvert.DeserializeObject<KeyFrameData>(poseJson);
                ikController.SetKeyFrameData(UgcPoseSubType.Single, frameData);
            }

            var ani = wrap.Avatar.GetComponent<Animator>();
            if(ani != null)
            {
                ani.enabled = true;
            }

            // 恢复姿势设置前的座位坐标（避免被 SetKeyFrameData 覆盖）
            if (optNode != null)
            {
                optNode.localPosition = optLocalPos;
                optNode.localScale = optLocalScale;
            }

            if (seatEulerRotation.HasValue)
            {
                var rot = Quaternion.Euler(seatEulerRotation.Value);
                if (wrap.Avatar.transform.parent != null)
                {
                    wrap.Avatar.transform.parent.localRotation = rot;
                }
            }
        }


        public void GetOutSelfVehicle(string uid)
        {
            MessageHelper.Broadcast(MessageName.OnVehicleMovingSoundStop, uid);//下车时候触发一下移动声音停止，防止载具移动时候下车没有监听到停止声音事件

            _chaData.RemoveVehicleData();
            var type = UniqueType.GetUgcVehicle(VehicleSubType.SingleVehicle);
            TakeOff(type);
            var resType = UniqueType.GetUgcVehicle(VehicleSubType.DoubleVehicle);
            TakeOff(resType);
            RefreshAvatar(_chaData);
            var animator = Avatar.transform.parent.parent.GetComponent<Animator>();
            if (animator != null)
            {
                animator.enabled = false;
                animator.gameObject.transform.localPosition = Vector3.zero;
            }
            Avatar.GetComponent<AnimIKController>().ChangeAnimResType(AnimResType.PGC);

            if (uid == AccountDataManager.Inst.UserInfo.uid && AvatarController.Inst.SelfController != null)
            {
                AvatarController.Inst.SelfController.Motor.IsDriveVehicle = false;
                AvatarController.Inst.SelfController.Motor.CurUGCVehicleStatus = UGCVehicleStatus.None;
                GameDataManager.Inst.mapGlobalData.vehicleInfo = null;
            }
            Avatar.transform.parent.localRotation = Quaternion.identity;
            Avatar.transform.parent.localPosition = new Vector3(0, 0.5f, 0);
            string name = "";
            for (int i = 0; i < Avatar.transform.childCount; i++)
            {
                name = Avatar.transform.GetChild(i).name;
                Avatar.transform.GetChild(i).gameObject.SetActive(true);
                if (name == "AvatarNode")
                {
                    GameObject.Destroy(Avatar.transform.GetChild(i).gameObject);
                }
                else if(name == "body_clothing" || name == "body_underwear" || name == "JointNodeParent")
                {
                    Avatar.transform.GetChild(i).gameObject.SetActive(false);
                }
            }
            var driverStateCtrl = Avatar.gameObject.GetComponent<PlayerStateController>();
            if(driverStateCtrl != null)
            {
                driverStateCtrl.PlayerKCCtrl.Motor.IsDriveVehicle = false;
                driverStateCtrl.PlayerKCCtrl.Motor.CurUGCVehicleStatus = UGCVehicleStatus.None;
                //driverStateCtrl.ExitState(PlayerState.ChangeClothes, false);
            }
        }

        public void GetInVehicle(CharacterWrap character, VehicleInfo vehicleInfo, string uid)
        {
            _chaData.vehicleData = vehicleInfo;
            if (uid == AccountDataManager.Inst.UserInfo.uid && AvatarController.Inst.SelfController != null)
            {
                AvatarController.Inst.SelfController.Motor.IsDriveVehicle = true;
                AvatarController.Inst.SelfController.Motor.CurUGCVehicleStatus = UGCVehicleStatus.TakeCar;
            }
            var stateCtrl = character.Avatar.gameObject.GetComponent<PlayerStateController>();
            if(stateCtrl == null)
            {
                return;
            }

            Transform playerRoot = stateCtrl.transform; 
            if (stateCtrl.PlayerKCCtrl != null)
            {
                playerRoot = stateCtrl.PlayerKCCtrl.transform; 
            }
            playerRoot.SetParent(Avatar.transform.parent, true);
            stateCtrl.PlayerKCCtrl.Motor.SetCapsuleCollisionsActivation(false);

            stateCtrl.PlayerKCCtrl.Motor.enabled = false;

            playerRoot.localPosition = new Vector3(0, -0.5f, 0);
            playerRoot.localRotation = Quaternion.identity;

            if (stateCtrl.Wrap.Avatar.transform.parent != null && vehicleInfo.doubleUserDetail != null)
            {
                stateCtrl.Wrap.Avatar.transform.parent.localPosition = Avatar.transform.parent.localPosition - vehicleInfo.doubleUserDetail.pDef;
            }

            ApplyUgcPoseWithSeatPreserved(
                stateCtrl.Wrap,
                vehicleInfo.doublePoseData,
                new Vector3(0, 0.5f, 0),
                vehicleInfo?.doubleUserDetail?.rDef
            );

            if(uid == AccountDataManager.Inst.UserInfo.uid)
            {
                var driverStateCtrl = Avatar.gameObject.GetComponent<PlayerStateController>();
                if(driverStateCtrl != null)
                {
                    driverStateCtrl.PlayerKCCtrl.CameraTarget = GameObject.Find("CameraTarget").transform;
                }
            }

        }

        public void GetOutVehicle(CharacterWrap character,string uid, bool isDiscardVehicle = false)
        {
            // 注意：这里是“乘客下车”逻辑，方法调用者通常是司机的Wrap（见GameVehicleManager.GetOutVehicle），不能在此停止司机的载具音效
            if (uid == AccountDataManager.Inst.UserInfo.uid && AvatarController.Inst.SelfController != null)
            {
                AvatarController.Inst.SelfController.Motor.IsDriveVehicle = false;
                AvatarController.Inst.SelfController.Motor.CurUGCVehicleStatus = UGCVehicleStatus.None;
            }
            var stateCtrl = character.Avatar.gameObject.GetComponent<PlayerStateController>();
            if(stateCtrl == null)
            {
                return;
            }
            stateCtrl.PlayerKCCtrl.Motor.gameObject.transform.SetParent(AvatarController.Inst.gameObject.transform);
            stateCtrl.PlayerKCCtrl.Motor.SetCapsuleCollisionsActivation(true);
            stateCtrl.PlayerKCCtrl.Motor.enabled = true;

            if(uid == AccountDataManager.Inst.UserInfo.uid)
            {
                AvatarController.Inst.SelfController.Motor.SetPositionAndRotation(Avatar.transform.position, Avatar.transform.rotation);
                AvatarController.Inst.SelfController.CameraTarget = GameObject.Find("CameraTarget").transform;
                var wrap = AvatarController.Inst.SelfWrap;
                if(wrap != null)
                {
                    wrap.Avatar.transform.parent.localPosition = new Vector3(0, 0.5f, 0);
                    wrap.Avatar.transform.parent.localRotation = Quaternion.identity;
                }
            }

            var animator = character.Avatar.transform.parent.parent.GetComponent<Animator>();
            if (animator != null)
            {
                animator.enabled = false;
                animator.gameObject.transform.localPosition = Vector3.zero;
            }
            character.Avatar.GetComponent<AnimIKController>().ChangeAnimResType(AnimResType.PGC);
            character.Avatar.transform.parent.localRotation = Quaternion.identity;
            character.Avatar.transform.parent.localPosition = new Vector3(0, 0.5f, 0);
            character.Avatar.transform.Find("Bip001/Bip001 Pelvis").localPosition = Vector3.zero;//这里重置下骨骼位置，防止下车玩家骨骼被载具姿势影响
            if(isDiscardVehicle)
            {
                var driverStateCtrl = Avatar.gameObject.GetComponent<PlayerStateController>();
                if(driverStateCtrl != null)
                {
                    driverStateCtrl.PlayerKCCtrl.Motor.IsDriveVehicle = false;
                    driverStateCtrl.PlayerKCCtrl.Motor.CurUGCVehicleStatus = UGCVehicleStatus.None;
                }
            }

        }

        /// <summary>
        /// 让 AI 伙伴作为乘客上车（this = 驾驶者玩家的 Wrap，buddyWrap = 伙伴的 Wrap）。
        /// 复用双人乘客的挂载与姿势（doublePoseData），但不走玩家 uid 分支——
        /// 伙伴 uid 与玩家相同，且伙伴无需相机/驾驶接管，不能误操作玩家自己的 Controller。
        /// </summary>
        public void GetInVehicleForBuddy(CharacterWrap buddyWrap, VehicleInfo vehicleInfo)
        {
            if (buddyWrap == null || vehicleInfo == null) return;
            var stateCtrl = buddyWrap.Avatar.gameObject.GetComponent<PlayerStateController>();
            if (stateCtrl == null || stateCtrl.PlayerKCCtrl == null) return;

            Transform buddyRoot = stateCtrl.PlayerKCCtrl.transform;
            // 记录上车前的父节点，下车时原样还原（区分 self-buddy / map-buddy 的归属）
            buddyWrap._buddyVehicleOriginalParent = buddyRoot.parent;
            buddyRoot.SetParent(Avatar.transform.parent, true);
            stateCtrl.PlayerKCCtrl.Motor.SetCapsuleCollisionsActivation(false);
            stateCtrl.PlayerKCCtrl.Motor.enabled = false;

            buddyRoot.localPosition = new Vector3(0, -0.5f, 0);
            buddyRoot.localRotation = Quaternion.identity;

            if (buddyWrap.Avatar.transform.parent != null && vehicleInfo.doubleUserDetail != null)
            {
                buddyWrap.Avatar.transform.parent.localPosition = Avatar.transform.parent.localPosition - vehicleInfo.doubleUserDetail.pDef;
            }

            ApplyUgcPoseWithSeatPreserved(
                buddyWrap,
                vehicleInfo.doublePoseData,
                new Vector3(0, 0.5f, 0),
                vehicleInfo?.doubleUserDetail?.rDef
            );
        }

        /// <summary>
        /// AI 伙伴下车（载具结束）：解绑、恢复 Motor，停在当前位置（与 GetOutVehicle 一致，去掉玩家 uid 分支）。
        /// </summary>
        public void GetOutVehicleForBuddy(CharacterWrap buddyWrap)
        {
            if (buddyWrap == null) return;
            var stateCtrl = buddyWrap.Avatar.gameObject.GetComponent<PlayerStateController>();
            if (stateCtrl == null || stateCtrl.PlayerKCCtrl == null) return;

            Vector3 currentPos = stateCtrl.transform.position;
            Quaternion currentRot = stateCtrl.transform.rotation;

            // 还原到上车前的父节点；记录缺失或父节点已销毁时回退到 AIBuddyAvatarController（兼容 self-buddy 旧行为）
            Transform restoreParent = buddyWrap._buddyVehicleOriginalParent != null
                ? buddyWrap._buddyVehicleOriginalParent
                : AIBuddyAvatarController.Inst.gameObject.transform;
            buddyWrap._buddyVehicleOriginalParent = null;
            stateCtrl.PlayerKCCtrl.Motor.gameObject.transform.SetParent(restoreParent);
            stateCtrl.PlayerKCCtrl.Motor.SetCapsuleCollisionsActivation(true);
            stateCtrl.PlayerKCCtrl.Motor.enabled = true;
            stateCtrl.PlayerKCCtrl.Motor.SetPositionAndRotation(currentPos, currentRot);

            var animator = buddyWrap.Avatar.transform.parent.parent.GetComponent<Animator>();
            if (animator != null)
            {
                animator.enabled = false;
                animator.gameObject.transform.localPosition = Vector3.zero;
            }
            buddyWrap.Avatar.GetComponent<AnimIKController>().ChangeAnimResType(AnimResType.PGC);
            buddyWrap.Avatar.transform.parent.localRotation = Quaternion.identity;
            buddyWrap.Avatar.transform.parent.localPosition = new Vector3(0, 0.5f, 0);
            buddyWrap.Avatar.transform.Find("Bip001/Bip001 Pelvis").localPosition = Vector3.zero;
        }

        public void ChangeLeftOrRight(int resType)
        {
            var lrHand = avatarPartDatas[resType].LRType;
            var curLR = (HandPartAdapter.HandLRType)lrHand;
            if (curLR == HandPartAdapter.HandLRType.Left)
            {
                lrHand = (int)HandPartAdapter.HandLRType.Right;
                GetPartAdapter(resType)?.SetLeftOrRight(lrHand);
            }
            else if (curLR == HandPartAdapter.HandLRType.Right)
            {
                lrHand = (int)HandPartAdapter.HandLRType.Left;
                GetPartAdapter(resType)?.SetLeftOrRight(lrHand);
            }
            avatarPartDatas[resType].LRType = lrHand;
        }


        public override void SetLeftOrRight(int resType, int leftOrRight)
        {
            if (leftOrRight != (int)HandPartAdapter.HandLRType.Left && leftOrRight != (int)HandPartAdapter.HandLRType.Right) return;
            if (avatarPartDatas.ContainsKey(resType))
            {
                avatarPartDatas[resType].LRType = leftOrRight;
                GetPartAdapter(resType)?.SetLeftOrRight(leftOrRight);
                GetPartAdapter(resType)?.Move(avatarPartDatas[resType].Pos);
                GetPartAdapter(resType)?.Rotate(avatarPartDatas[resType].Rot);
                GetPartAdapter(resType)?.Scale(avatarPartDatas[resType].Sca);
            }

        }

        public override void SetAnchor(int resType, Vec3 anchor)
        {
            if (avatarPartDatas.ContainsKey(resType) && anchor != null)
            {
                avatarPartDatas[resType].CAnchor = anchor;
                GetPartAdapter(resType)?.SetAnchor(anchor);
            }
        }

        public override void TakeOff(int resType)
        {
            if (avatarPartDatas.ContainsKey(resType))
            {
                avatarPartDatas[resType].Id = "0";
                avatarPartDatas[resType].Url = null;
                avatarPartDatas[resType].UgcStyle = 0;
            }

            var animationCtrl = Avatar.GetComponent<PlayerAnimationCtrl>();
            if (animationCtrl != null)
            {
                animationCtrl.RefreshSkinInfo(resType, "0");
            }

            GetPartAdapter(resType)?.ResetCurrentID();
            GetPartAdapter(resType)?.TakeOff();
        }

        public void TakeOffSpecialSkin(int resType)
        {
            var adapter = GetPartAdapter(resType) as SpecialSkinPartAdapter;
            if(adapter != null)
            {
                adapter.ChangeSpecialSkinStatus();
            }
        }

        /// <summary>
        /// 脱下特殊走跑跳皮肤
        /// </summary>
        public override void TakeOffSpecialSkin()
        {
            foreach (var avatarPartData in avatarPartDatas)
            {
                if (DataTables.GetSpecialSkinConfig(avatarPartData.Value.Id) != null)
                {
                    var resType = avatarPartData.Key;
                    if (avatarPartDatas.ContainsKey(resType))
                    {
                        avatarPartDatas[resType].Id = "0";
                        avatarPartDatas[resType].Url = null;
                        avatarPartDatas[resType].UgcStyle = 0;
                    }

                    var animationCtrl = Avatar.GetComponent<PlayerAnimationCtrl>();
                    if (animationCtrl != null)
                    {
                        animationCtrl.RefreshSkinInfo(resType, "0");
                    }

                    GetPartAdapter(resType)?.ResetCurrentID();
                    GetPartAdapter(resType)?.TakeOff();
                }
            }
        }

        public override void ChangeShape(int resType, int id)
        {
            var shapeCtrl = Avatar.GetComponentInChildren<CustomBodyTypeController>();
            shapeCtrl.ApplyBodyType((CustomBodyTypeController.BodyType)id);
            ChaData.bodyType = id;
        }

        public override void ChangeColor(int resType, string col)
        {
            if (avatarPartDatas.ContainsKey(resType) && !string.IsNullOrEmpty(col))
            {
                avatarPartDatas[resType].Cr = col;
                Color tempColor = FormatUtils.StringToColorByHex(col);
                GetPartAdapter(resType)?.ChangeColor(tempColor);
            }
        }

        public override void Move(int resType, Vec3 pos)
        {
            if (avatarPartDatas.ContainsKey(resType) && pos != null)
            {
                avatarPartDatas[resType].Pos = pos;
                GetPartAdapter(resType)?.Move(pos);
            }
        }

        public override void Rotate(int resType, Vec3 rot)
        {
            if (avatarPartDatas.ContainsKey(resType) && rot != null)
            {
                avatarPartDatas[resType].Rot = rot;
                GetPartAdapter(resType)?.Rotate(rot);
            }
        }

        public override void Scale(int resType, Vec3 sca)
        {

            if (avatarPartDatas.ContainsKey(resType) && sca != null)
            {
                avatarPartDatas[resType].Sca = sca;
                GetPartAdapter(resType)?.Scale(sca);
            }
        }

        public override void HVScale(int resType, Vec3 sca)
        {
            if (avatarPartDatas.ContainsKey(resType) && sca != null)
            {
                avatarPartDatas[resType].CSca = sca;
                GetPartAdapter(resType)?.HVScale(sca);
            }
        }

        public override Transform GetBandNode(int type)
        {

            switch (type)
            {
                case (int)BodyNode.RightHand:
                    return Avatar.transform.Find(BodyPath.RIGHT_HAND_PATH);
                case (int)BodyNode.LeftHand:
                    return Avatar.transform.Find(BodyPath.LEFT_HAND_PATH);
                case (int)BodyNode.PickNode:
                    if (pickNode == null)
                    {
                        pickNode = Avatar.transform.Find(BodyPath.PICK_HAND_PATH);
                    }
                    return pickNode;
                case (int)BodyNode.PickPos:
                    if (pickPos == null)
                    {
                        pickPos = Avatar.transform.Find("pickPos");
                    }
                    return pickPos;
                case (int)BodyNode.FoodNode:
                    if (foodNode == null)
                    {
                        foodNode = Avatar.transform.Find(BodyPath.PICK_FOOD_PATH);
                    }
                    return foodNode;
                case (int)BodyNode.BackNode:
                    return Avatar.transform.Find(BodyPath.BACK_PATH);
                case (int)BodyNode.LEffectNode:
                    return Avatar.transform.Find(BodyPath.LEFT_EFFECT_PATH);
                case (int)BodyNode.REffectNode:
                    return Avatar.transform.Find(BodyPath.RIGHT_EFFECT_PATH);
                case (int)BodyNode.BackDeckNode:
                    return Avatar.transform.Find(BodyPath.BAG_PATH);
                case (int)BodyNode.HatNode:
                    return Avatar.transform.Find(BodyPath.HAT_PATH);
                case (int)BodyNode.SpecialBackDeckNode:
                    return Avatar.transform.Find(BodyPath.SPECIAL_BAG_PATH);
                case (int)BodyNode.SpecialHatNode:
                    return Avatar.transform.Find(BodyPath.SPECIAL_HAT_PATH);
                case (int)BodyNode.SpecialEffectNode:
                    return Avatar.transform.Find(BodyPath.SPECIAL_EFFECT_PATH);
            }
            return null;
        }

        public override void RsetFaceMat()
        {
            _adapters[UniqueType.GetAvatar(AvatarSubType.Eyes)].Reset();
            _adapters[UniqueType.GetAvatar(AvatarSubType.Brow)].Reset();
            _adapters[UniqueType.GetAvatar(AvatarSubType.Mouth)].Reset();
        }

        public void Reset(int resType)
        {
            GetPartAdapter(resType)?.Reset();
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