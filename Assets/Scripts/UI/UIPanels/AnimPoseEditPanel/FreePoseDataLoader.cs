using System;
using System.Collections.Generic;
using GameData.BaseInfo;
using GameData.PgcData;
using Newtonsoft.Json;
using UnityEngine;

namespace BUD.AnimPose
{
    public class FreePoseDataLoader:MonoBehaviour
    {
        private const string localOfficialConfig = "Assets/Arts/Config/CustomPoseConfig/officialPose.json";

        public void GetDatas(UgcPoseSubType subType,Action<List<QuickPoseData>> resultAction, GameObject bindNo)
        {
            var wrapper = Loader.Load<TextAsset>(localOfficialConfig);
            if (wrapper == null)
            {
                LoggerUtils.LogError("localOfficialConfig  read Error");
                return;
            }

            var officialData = wrapper.RetainAsset(bindNo);
            var poseDatas = JsonConvert.DeserializeObject<List<PoseInfo>>(officialData.text);
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

        public static void GetData(UgcPoseSubType subType, Action<List<QuickPoseData>> resultAction, GameObject bindNo) {
            var wrapper = Loader.Load<TextAsset>(localOfficialConfig);
            if (wrapper == null)
            {
                LoggerUtils.LogError("localOfficialConfig  read Error");
                return;
            }

            var officialData = wrapper.RetainAsset(bindNo);
            var poseDatas = JsonConvert.DeserializeObject<List<PoseInfo>>(officialData.text);
            var tempList = new List<QuickPoseData>();
            for (var i = 0; i < poseDatas.Count; i++)
            {
                if (poseDatas[i].poseType == (int)subType)
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