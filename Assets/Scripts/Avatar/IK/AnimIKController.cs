using System;
using System.Collections.Generic;
using Es;
using GameData.BaseInfo;
using GameData.PgcData;
using JetBrains.Annotations;
using Newtonsoft.Json;
using RootMotion.FinalIK;
using UIAgent;
using UnityEngine;
using xasset;

namespace BUD.AnimPose
{
    public class AnimIKController : MonoBehaviour
    {
        public PoseDataManager dataManager { get; private set; } = new();
        private AnimBgmController _animBgmController;
        private List<BaseAnimIK> animIks = new List<BaseAnimIK>();
        private Dictionary<int,PropAnimIK> propIks = new Dictionary<int, PropAnimIK>();
        private bool canPlayAnim = false;
        private bool isLoop = false;
        private float currentAnimRuntime = 0;
        private ItemKeyFrameData defItemData;
        private Action complete;
        private int frameFrequency = 5;
        private bool isNeedSound = true;
        private bool isPlaySound = true;

        public void ChangeAnimResType(AnimResType resType)
        {
            var animtor = this.GetComponent<Animator>();
            if (animtor != null)
            {
                animtor.enabled = resType == AnimResType.PGC;
            }
            for (var i = 0; i < animIks.Count; i++)
            {
                var baseIk = animIks[i].GetComponents<IK>();
                for (var j = 0; j < baseIk.Length; j++)
                {
                    baseIk[j].enabled = resType == AnimResType.UGC;
                }
            }
            this.enabled = resType == AnimResType.UGC;
        }

        public void ChangeUgcToPgcAnim()
        {
            StopAnimAndResetJointNode();
            ChangeAnimResType(AnimResType.PGC);
            ResetDefaultPosition();
            RemoveOtherAnimIk();
        }

        public void ResetDefaultPosition()
        {
            foreach (var animIk in animIks)
            {
                animIk.ResetDefaultPosition();
            }
        }

        public void SetAnimFrameData(AnimFrameData data)
        {
            frameFrequency = data.framefrequency;
            dataManager.SetAnimFrameData(data);
        }

        public void AddAnimIK(BaseAnimIK ik)
        {
            if (!animIks.Contains(ik))
            {
                animIks.Add(ik);
            }
        }


        public void ChangeShotMaterial(bool isShot)
        {
            for (var i = 0; i < animIks.Count; i++)
            {
                if (isShot)
                {
                    animIks[i].SetWhiteMaterials();
                }
                else
                {
                    animIks[i].SetDefaultMaterials();
                }
            }
        }


        public void Insert(int index, BaseAnimIK ik)
        {
            if (!animIks.Contains(ik))
            {
                animIks.Insert(index, ik);
            }
        }


        public void RemoveAnimIK(BaseAnimIK ik)
        {
            if (animIks.Contains(ik))
            {
                animIks.Remove(ik);
            }
        }

        public void RemoveOtherAnimIk()
        {
            var selfAnimIk = this.transform.GetComponent<BaseAnimIK>();
            animIks.Clear();
            animIks.Add(selfAnimIk);
        }

        public void SetAnimIKs(List<BaseAnimIK> iks)
        {
            animIks = iks;
        }

        public List<BaseAnimIK> GetAnimIKs()
        {
            return animIks;
        }

        public void RemoveAt(int index)
        {
            animIks.RemoveAt(index);
        }

        public void AddPropIK(PropAnimIK ik)
        {
            propIks[ik.curIndex] = ik;
        }

        public void RemovePropIK(PropAnimIK ik)
        {
            propIks.Remove(ik.curIndex);
        }

        public Dictionary<int, Transform> GetBindNodes(UgcPoseSubType poseType)
        {
            Dictionary<int, Transform> bindNodes = new Dictionary<int, Transform>();
            var poseData = DataTables.GetPoseModeConfig((int) poseType);
            for (var i = 0; i < poseData.BindIndexs.Count; i++)
            {
                int bindIndex = i;
                var bind = GetBindNode(poseType, bindIndex);
                bindNodes.Add(bindIndex, bind);
            }
            return bindNodes;
        }

        public Transform GetBindNode(UgcPoseSubType poseType,int bIndex)
        {
            var poseData = DataTables.GetPoseModeConfig((int) poseType);
            int bindIndex = poseData.BindIndexs[bIndex];
            if (bindIndex == 0)
            {
                return animIks[0].transform.parent.parent;
            }
            int separateIndex = 6;
            Transform bind = null;
            switch (poseType)
            {
                case UgcPoseSubType.Single:
                case UgcPoseSubType.PetSingle:
                    string path = PoseDataManager.bindNodePaths[(BindNodePart)bindIndex];
                    bind = animIks[0].transform.Find(path);
                    break;
                case UgcPoseSubType.Double:
                case UgcPoseSubType.PetWithPlayer:
                    string path1 = PoseDataManager.bindNodePaths[(BindNodePart)bindIndex];
                    int animIndex = bIndex < separateIndex ? 0 : 1;
                    bind = animIks[animIndex].transform.Find(path1);
                    break;
            }
            return bind;
        }

        /// <summary>
        /// 必须先创建人物，在创建道具
        /// </summary>
        /// <param name="poseType"></param>
        /// <param name="iks"></param>
        public void CreatePropIKs(UgcPoseSubType poseType, List<AnimPropData> propList,bool collider = true,string layerName = "Model")
        {
            if (propList == null || propList.Count == 0)
            {
                return;
            }

            if (propList.Count > 5)
            {
                LoggerUtils.LogError("propList.Count Count is over 5", propList.Count);
            }

            //TODO:目前propList存在重复数据，临时策略
            Dictionary<int, AnimPropData> propDir = new Dictionary<int, AnimPropData>();
            for (var i = 0; i < propList.Count; i++)
            {
                var propData = propList[i];
                propDir[propData.index] = propData;
            }

            if (propDir.Count != 0)
            {
                foreach (var keyValue in propDir)
                {
                    var propData = keyValue.Value;
                    if (!string.IsNullOrEmpty(propData.id) && !string.IsNullOrEmpty(propData.metaDataUrl))
                    {
                        CreatePropIK(poseType, propData,collider,layerName);
                    }
                }
            }
        }

        public PropAnimIK CreatePropIK(UgcPoseSubType poseType,AnimPropData data,bool colliderVisible = true,string layerName = "Model")
        {
            var propIk = CreatePropIKByData(poseType,data);
            CreateOfflineProp(propIk, data, layerName,colliderVisible);
            return propIk;
        }

        private void CreateOfflineProp(PropAnimIK propIk, AnimPropData info,string layerName,bool colliderVisible)
        {
            var node = GameAgentManager.Inst.CreatePropWithOffline(info.id, info.metaDataUrl,
                (node) => { if (node != null) ChangeNodeLayer(node.transform,layerName,colliderVisible); });
            node.transform.SetParent(propIk.transform);
            node.transform.localPosition = Vector3.zero;
            node.transform.localEulerAngles = Vector3.zero;
            node.transform.localScale = Vector3.one;
        }


        private void ChangeNodeLayer(Transform node,string layerName,bool colliderVisible)
        {
            var colliders = node.GetComponentsInChildren<Collider>(true);
            for (var i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = colliderVisible;
            }
            node.gameObject.layer = LayerMask.NameToLayer(layerName);
            for (int i = 0; i < node.childCount; i++)
            {
                ChangeNodeLayer(node.GetChild(i),layerName,colliderVisible);
            }
        }

        public PropAnimIK CreatePropIKByData(UgcPoseSubType poseType,AnimPropData data)
        {
            var bindNodes = GetBindNodes(poseType);
            var node = new GameObject("PropOptNode");
            var propIk = node.AddComponent<PropAnimIK>();
            propIk.InitPropNode(data.index,data.bindIndex,data.id,poseType, bindNodes);
            AddPropIK(propIk);
            return propIk;
        }

        public Dictionary<int,PropAnimIK> GetPropIKs()
        {
            return propIks;
        }

        public void SetPropIks(Dictionary<int,PropAnimIK> iks)
        {
            this.propIks = iks;
        }



        public void PlayOnceAnimOnHall(Action callback = null)
        {
            ResetJointNodes();
            complete = callback;
            currentAnimRuntime = 0;
            canPlayAnim = true;
            isPlaySound = true;
            GetAnimBgmCtr().StartPlay();
        }


        public void PlayOnceAnim(Action callback = null,bool isPlaySound = true)
        {
            ResetJointNodes();
            complete = callback;
            currentAnimRuntime = 0;
            canPlayAnim = true;
            this.isPlaySound = isPlaySound;
            isLoop = false;
            GetComponent<PlayerAnimationCtrl>().SetSpecialActivity(false);
            if (isPlaySound)
            {
                GetAnimBgmCtr().StartPlay();
            }
        }

        /// <summary>
        /// 循环播放
        /// </summary>
        /// <param name="callback">单次回调</param>
        public void PlayLoopAnim(Action callback = null,bool isPlaySound = true)
        {
            ResetJointNodes();
            complete = callback;
            currentAnimRuntime = 0;
            canPlayAnim = true;
            this.isPlaySound = isPlaySound;
            isLoop = true;
            GetComponent<PlayerAnimationCtrl>().SetSpecialActivity(false);
            if (isPlaySound)
            {
                GetAnimBgmCtr().StartPlay();
            }
           
        }


        private void ResetJointNodes()
        {
            foreach (var animIK in animIks)
            {
                if (animIK != null)
                {
                    animIK.ResetJointNodes();
                }
            }
        }

        public void StopAnim()
        {
            currentAnimRuntime = 0;
            canPlayAnim = false;
            isPlaySound = true;
            RemovePropIks();
            _animBgmController?.StopPlay();
        }

        public void OnDestroy()
        {
            _animBgmController?.StopPlay();
        }

        public void StopAnimAndResetJointNode()
        {
            StopAnim();
            ResetJointNodes();
        }

        public void RemovePropIks()
        {
            foreach (var keyValue in propIks)
            {
                if (keyValue.Value != null)
                {
                    GameObject.Destroy(keyValue.Value.gameObject);
                }
            }
            propIks.Clear();
        }

        private void Update()
        {
            if (canPlayAnim)
            {
                currentAnimRuntime += Time.deltaTime;
                SetTimeStamp(currentAnimRuntime, frameFrequency, true);
            }
        }

        public void SaveKeyFrameData(KeyFrameData frameData)
        {
            for (var i = 0; i < animIks.Count; i++)
            {
                animIks[i].SetOptEntity(animIks[i].gameObject);
                animIks[i].SaveKeyFrame(frameData.keyFrame[i]);
            }

            frameData.items = new List<ItemKeyFrameData>();
            if (propIks != null && propIks.Count != 0)
            {
                foreach (var keyValue in propIks)
                {
                    ItemKeyFrameData itemData = new ItemKeyFrameData();
                    keyValue.Value.SaveKeyFrame(itemData);
                    frameData.items.Add(itemData);
                }
            }
        }

        public void SetJointNodeVisible(bool visible)
        {
            for (var i = 0; i < animIks.Count; i++)
            {
                animIks[i].SetJointNodeVisible(visible);
            }
        }

        public void SetKeyFrameData(UgcPoseSubType animType,KeyFrameData frameData)
        {
            if (frameData == null)
            {
                return;
            }

            if (frameData.keyFrame == null || (frameData.keyFrame != null && frameData.keyFrame.Count == 0))
            {
                frameData.keyFrame = new List<RoleKeyframeData>();
                RoleKeyframeData keyFrame1 = new RoleKeyframeData();
                frameData.keyFrame.Add(keyFrame1);
                switch (animType)
                {
                    case UgcPoseSubType.Single:
                        keyFrame1.roleType = RoleType.Avatar;
                        break;
                    case UgcPoseSubType.PetSingle:
                        keyFrame1.roleType = RoleType.Pet;
                        break;
                    case UgcPoseSubType.Double:
                        RoleKeyframeData keyFrame2 = new RoleKeyframeData();
                        frameData.keyFrame.Add(keyFrame2);
                        keyFrame1.roleType = RoleType.Avatar;
                        keyFrame2.roleType = RoleType.Avatar;
                        break;
                    case UgcPoseSubType.PetWithPlayer:
                        keyFrame2 = new RoleKeyframeData();
                        frameData.keyFrame.Add(keyFrame2);
                        keyFrame1.roleType = RoleType.Avatar;
                        keyFrame2.roleType = RoleType.Pet;
                        break;
                }
                for (var i = 0; i < animIks.Count; i++)
                {
                    animIks[i].SetOptEntity(animIks[i].gameObject);
                    animIks[i].SaveKeyFrame(frameData.keyFrame[i]);
                }
            }

            else
            {
                for (var i = 0; i < animIks.Count; i++)
                {
                    animIks[i].SetOptEntity(animIks[i].gameObject);
                    animIks[i].SetKeyFrameData(frameData.keyFrame[i]);
                }

                if (propIks != null && propIks.Count != 0)
                {
                    foreach (var keyValue in propIks)
                    {
                        var propIk = keyValue.Value;
                        var itemData = frameData.items.Find(x => x.index.Equals(propIk.curIndex));
                        if (itemData != null)
                        {
                            propIk.SetKeyFrameData(itemData);
                        }
                    }
                }
            }
        }

        public void SetKeyFrameNumber(int frame)
        {
            var keyFrames = dataManager.GetKeyFrameList();
            if (frame > keyFrames.Count - 1)
            {
                Debug.LogError("frame is Over keyFrames.Count");
                return;
            }
            var curKeyFrame = keyFrames[frame];
            for (var i = 0; i < animIks.Count; i++)
            {
                animIks[i].SetOptEntity(animIks[i].gameObject);
                animIks[i].SetKeyFrameData(curKeyFrame.keyFrame[i]);
            }
        }

        public void SetTimeStamp(float curTime, int frequency,bool isPlayAnim = false)
        {
            float curFrameCount = curTime * frequency;
            int curFrameIndex = (int) Math.Floor(curFrameCount);

            var keyFrames = dataManager.GetKeyFrameList();
            if (isPlayAnim && curFrameIndex >= keyFrames.Count - 1)
            {
                if (isLoop)
                {
                    canPlayAnim = true;
                    currentAnimRuntime = 0;
                    if (isPlaySound)
                    {
                        GetAnimBgmCtr().StartPlay();
                    }
                }
                else
                {
                    canPlayAnim = false;
                    GetAnimBgmCtr().StopPlay();
                }
                complete?.Invoke();
                return;
            }

            var curFrameNumber = dataManager.GetCurValidFrameNumber(curFrameIndex);
            var nextFrameNumber = dataManager.GetNextValidFrameNumber(curFrameIndex);

            nextFrameNumber = nextFrameNumber == 0 ? curFrameNumber : nextFrameNumber;

            var curKeyFrame = keyFrames[curFrameNumber];
            var nextKeyFrame = keyFrames[nextFrameNumber];

            if (curKeyFrame.keyFrame.Count == 0 || nextKeyFrame.keyFrame.Count == 0)
            {
                canPlayAnim = false;
                GetAnimBgmCtr().StopPlay();
                return;
            }
            float offset = 0;
            if (nextFrameNumber > curFrameNumber)
            {
                offset = (curFrameCount - curFrameNumber) / (nextFrameNumber - curFrameNumber);
            }
            for (var i = 0; i < animIks.Count; i++)
            {
                animIks[i]?.SetOptEntity(animIks[i].gameObject);
                animIks[i]?.SetKeyFrameData(curKeyFrame.keyFrame[i], nextKeyFrame.keyFrame[i], offset);
            }

            GetOrCreateDefItemData();

            foreach (var keyValue in propIks)
            {
                ItemKeyFrameData curItemFrame = GetItemData(keyValue.Key, curKeyFrame);
                ItemKeyFrameData nextItemFrame = GetItemData(keyValue.Key, nextKeyFrame);

                keyValue.Value.SetKeyFrameData(curItemFrame, nextItemFrame, offset);
            }

            PlayAnimBgmTrackByTime(curTime);
        }

        private ItemKeyFrameData GetItemData(int index,KeyFrameData keyFrame)
        {
            ItemKeyFrameData itemFrame = null;
            if (keyFrame.items != null)
            {
                itemFrame = keyFrame.items.Find(x => x.index.Equals(index));
            }

            if (itemFrame == null)
            {
                itemFrame = defItemData;
                itemFrame.index = index;
            }
            return itemFrame;
        }

        private void GetOrCreateDefItemData()
        {
            if (defItemData != null)
            {
                return;
            }
            defItemData = new ItemKeyFrameData()
            {
                bindIndex = 0,
                state = 1,
                pos = Vector3.zero,
                rot = Quaternion.identity,
                sca = Vector3.one
            };
        }

        public void SetBgmEnable(bool needSound = true)
        {
            this.isNeedSound = needSound;
        }

        public AnimBgmController GetAnimBgmCtr()
        {
            if (_animBgmController == null)
            {
                _animBgmController = this.gameObject.AddComponent<AnimBgmController>();
            }
            return _animBgmController;
        }

        public void SetUIPreviewMode(bool isUIPreview)
        {
            GetAnimBgmCtr().SetUIPreviewMode(isUIPreview);
        }

        private AnimInfo curAnimInfo;
        public void Play(AnimInfo info, AnimIKController other, Action onceCallback = null)
        {
            if (other != null) {
                ResetOtherIK((UgcAnimSubType)info.animType, other.GetComponent<BaseAnimIK>());
            }

            StopAnimAndResetJointNode();
            var id = info.id;
            curAnimInfo = info;
            ChangeAnimResType(AnimResType.UGC);
            other?.ChangeAnimResType(AnimResType.UGC);
            CreatePropIKs((UgcPoseSubType)info.animType, info.propList);
            var assetRequest = Asset.LoadRemoteAssetAsync(info.metaDataUrl);
            if (assetRequest != null)
            {
                assetRequest.completed += (_) =>
                {
                    if (curAnimInfo.id != id) return;
                    if (assetRequest.result == Request.Result.Success)
                    {
                        var content = System.Text.Encoding.UTF8.GetString(assetRequest.asset);
                        var frameData = JsonConvert.DeserializeObject<AnimFrameData>(content);
                        SetAnimFrameData(frameData);
                        if (info.loop == 1) PlayLoopAnim();
                        else PlayOnceAnim(onceCallback);
                    }
                };
            }
        }

        private void ResetOtherIK(UgcAnimSubType type, BaseAnimIK other)
        {
            other.gameObject.SetActive(false);
            RemoveAnimIK(other);

            switch (type)
            {
                case UgcAnimSubType.Double:
                    AddAnimIK(other);
                    other.gameObject.SetActive(true);
                    break;
                case UgcAnimSubType.PetWithPlayer:
                    Insert(0, other);
                    other.gameObject.SetActive(true);
                    break;
            }
        }

        public void Pose(PoseInfo info, AnimIKController other)
        {
            ResetOtherIK((UgcAnimSubType)info.poseType, other.GetComponent<BaseAnimIK>());

            ChangeAnimResType(AnimResType.UGC);
            other.ChangeAnimResType(AnimResType.UGC);
            var keyFrameData = JsonConvert.DeserializeObject<KeyFrameData>(info.poseData);
            SetKeyFrameData((UgcPoseSubType)info.poseType, keyFrameData);
        }

        private void PlayAnimBgmTrackByTime(float time)
        {
            if(!isNeedSound)
                return;

            var infos = dataManager.GetBgmTrackInfo();
            if(infos == null || infos.Count == 0)
                return;

            var bgmCtr = GetAnimBgmCtr();
            bgmCtr.PlayAnimBgmTrackByTime(infos, time);
        }
    }
}
