using System;
using System.Collections.Generic;
using GameData.BaseInfo;
using GameData.PgcData;
using Newtonsoft.Json;
using UnityEngine;

namespace BUD.AnimPose
{
    public class IncubationCabinDataLoader:MonoBehaviour
    {
        private const string cabinOfficialLoopPoseConfig = "Assets/Arts/Config/CabinPoseConfig/cabinOfficialLoopPose.json";
        private const string cabinOfficialNonLoopPoseConfig = "Assets/Arts/Config/CabinPoseConfig/cabinOfficialNonLoopPose.json";
        private const string cabinCommunityLoopPoseConfig = "Assets/Arts/Config/CabinPoseConfig/cabinCommunityLoopPose.json";
        private const string cabinCommunityNonLoopPoseConfig = "Assets/Arts/Config/CabinPoseConfig/cabinCommunityNonLoopPose.json";

        public void GetOfficialLoopDatas(UgcPoseSubType subType, Action<List<QuickPoseData>> resultAction, GameObject bindNo)
        {
            var wrapper = Loader.Load<TextAsset>(cabinOfficialLoopPoseConfig);
            if (wrapper == null)
            {
                LoggerUtils.LogError("cabinOfficialLoopPoseConfig  read Error");
                return;
            }

            var loopData = wrapper.RetainAsset(bindNo);
            var poseDatas = JsonConvert.DeserializeObject<List<PoseInfo>>(loopData.text);
            var tempList = new List<QuickPoseData>();
            for (var i = 0; i < poseDatas.Count; i++)
            {
                if (poseDatas[i].poseType == (int) subType)
                {
                    tempList.Add(new QuickPoseData()
                    {
                        poseInfo = poseDatas[i]
                    });
                }
            }
            resultAction?.Invoke(tempList);
        }


        public void GetOfficialNonLoopDatas(UgcPoseSubType subType, Action<List<QuickPoseData>> resultAction, GameObject bindNo)
        {
            var wrapper = Loader.Load<TextAsset>(cabinOfficialNonLoopPoseConfig);
            if (wrapper == null)
            {
                LoggerUtils.LogError("cabinOfficialNonLoopPoseConfig  read Error");
                return;
            }
            var nonLoopData = wrapper.RetainAsset(bindNo);
            var poseDatas = JsonConvert.DeserializeObject<List<PoseInfo>>(nonLoopData.text);
            var tempList = new List<QuickPoseData>();
            for (var i = 0; i < poseDatas.Count; i++)
            {
                if (poseDatas[i].poseType == (int) subType)
                {
                    tempList.Add(new QuickPoseData()
                    {
                        poseInfo = poseDatas[i]
                    });
                }
            }
            resultAction?.Invoke(tempList);
        }

        public void GetCommunityLoopDatas(UgcPoseSubType subType, Action<List<QuickPoseData>> resultAction, GameObject bindNo)
        {
            var wrapper = Loader.Load<TextAsset>(cabinCommunityLoopPoseConfig);
            if (wrapper == null)
            {
                LoggerUtils.LogError("cabinCommunityLoopPoseConfig  read Error");
                return;
            }
            var loopData = wrapper.RetainAsset(bindNo);
            var poseDatas = JsonConvert.DeserializeObject<List<PoseInfo>>(loopData.text);
            var tempList = new List<QuickPoseData>();
            for (var i = 0; i < poseDatas.Count; i++)
            {
                if (poseDatas[i].poseType == (int) subType)
                {
                    tempList.Add(new QuickPoseData()
                    {
                        poseInfo = poseDatas[i]
                    });
                }
            }
            resultAction?.Invoke(tempList);
        }

        public void GetCommunityNonLoopDatas(UgcPoseSubType subType, Action<List<QuickPoseData>> resultAction, GameObject bindNo)
        {
            var wrapper = Loader.Load<TextAsset>(cabinCommunityNonLoopPoseConfig);
            if (wrapper == null)
            {
                LoggerUtils.LogError("cabinCommunityNonLoopPoseConfig  read Error");
                return;
            }
            var nonLoopData = wrapper.RetainAsset(bindNo);
            var poseDatas = JsonConvert.DeserializeObject<List<PoseInfo>>(nonLoopData.text);
            var tempList = new List<QuickPoseData>();
            for (var i = 0; i < poseDatas.Count; i++)
            {
                if (poseDatas[i].poseType == (int) subType)
                {
                    tempList.Add(new QuickPoseData()
                    {
                        poseInfo = poseDatas[i]
                    });
                }
            }
            resultAction?.Invoke(tempList);
        }
    }
}