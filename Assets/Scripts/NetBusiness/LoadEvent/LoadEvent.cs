using UnityEngine;
using Network;
using Network.Http;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Basic.Utils;
using System.Collections.Generic;

namespace EventTracking
{   
    public class TaskUVInfo
    {
        public string taskId;
        public int type; //0：打开界面，1：展示任务，2：点击任务
        public int groupId; //第几天，展示任务需要上传
        public List<int> memberIds; //第几个任务，点击类型时需要上传
    }
    public class LoadEvent : MonoBehaviour
    {   
        //上报字典
        public static JObject reportData = new JObject
        {
            ["showPopupIds"] = new JArray(),
            ["clickedPopupIds"] = new JArray()
        };

        // API路径
        public const string API_POPUP_UPLOAD = "/other/upload/popup";

        private static bool isSending = false;


        //添加到缓存字典
        public static void AddReportData(int popupId, int popClickId)
        {
            
            var key1 = AccountDataManager.Inst.Uid + popupId + "show";
            var key2 = AccountDataManager.Inst.Uid + popClickId + "click";

            if (popupId != -1)  
            {
                
                    ((JArray)reportData["showPopupIds"]).Add(popupId);
                
            }
            
            if (popClickId != -1)
            {

                    ((JArray)reportData["clickedPopupIds"]).Add(popClickId);;

            }

            
        }
        //发送缓存数据
        public static bool pushreportData()
        {


            var showIds = ((JArray)reportData["showPopupIds"]).ToObject<List<int>>();
            var clickIds = ((JArray)reportData["clickedPopupIds"]).ToObject<List<int>>();

            if (showIds.Count == 0 && clickIds.Count == 0)
            {
                LoggerUtils.Log("没有需要上报的数据");
                return false;
            }

            var paramStr = JsonConvert.SerializeObject(reportData);

            NetworkManager.Inst.SendHttpRequest(
                API_POPUP_UPLOAD,
                HttpMethod.POST,
                paramStr,
                (response) => 
                {
                    LoggerUtils.Log($"弹窗埋点上报成功: {response}");

                },
                (error) => 
                {
                    LoggerUtils.LogError($"弹窗埋点上报失败: {error}");

                }
            );
            LoggerUtils.Log($"当前埋点数据: {reportData}");
            ((JArray)reportData["showPopupIds"]).Clear();
            ((JArray)reportData["clickedPopupIds"]).Clear();

            return true;
        }
 
        //活动用
        public static void ReportPopupStatus(string activityId , string eventName = "LogEvent_ActivityCenter")
        {
            LoggerUtils.Log("EventName: " + eventName + " EventType: " + activityId);
            
            // 针对GameHall事件，每天只上报一次
            if (eventName == "GameHall")
            {
                if (AccountDataManager.Inst.UserInfo.isNewUser != 1)
                {
                    return;
                }
                
                // 检查今天是否已经上报过GameHall事件
                string today = System.DateTime.Now.ToString("yyyy-MM-dd");
                string lastReportDate = PlayerPrefs.GetString("GameHall_LastReportDate", "");
                
                if (lastReportDate == today)
                {
                   // LoggerUtils.Log("GameHall事件今天已经上报过，跳过");
                    return;
                }
                
                // 记录今天的上报日期
                PlayerPrefs.SetString("GameHall_LastReportDate", today);
                PlayerPrefs.Save();
            }
            
            AnalyticsManager.Inst.Track(
            eventName,
            new Dictionary<string, object>
            {
                ["uid"] = AccountDataManager.Inst.Uid,
                ["EventType"] = activityId
            }
            );
       
        }
        public static void RemoveNewPlayerStatus() {
            if (PlayerPrefs.HasKey("guide_ID_4"))
            {
                PlayerPrefs.DeleteKey("guide_ID_4");
            }
            if (PlayerPrefs.HasKey("guide_ID_5"))
            {
                PlayerPrefs.DeleteKey("guide_ID_5");
            }
            if (PlayerPrefs.HasKey("guide_ID_6"))
            {
                PlayerPrefs.DeleteKey("guide_ID_6");
            }
            if (PlayerPrefs.HasKey("guide_ID_8"))
            {
                PlayerPrefs.DeleteKey("guide_ID_8");
            }
            if (PlayerPrefs.HasKey("FirstOpenAccurateRecommendation"))
            {
                PlayerPrefs.DeleteKey("FirstOpenAccurateRecommendation");
            }
            
            if (PlayerPrefs.HasKey("FirstOpenFittingRoomPanel"))
            {
                PlayerPrefs.DeleteKey("FirstOpenFittingRoomPanel");
            }
        }
        public static void ReportTask(int eventId ,int value ,string mapid = "")
        {
            JObject jObject = new JObject()
            {
                ["eventId"] = eventId,
                ["value"] = value
            };
            if(!string.IsNullOrEmpty(mapid))
            {
                jObject["bizId"] = mapid;
            }
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.PostEvent,
                HttpMethod.POST,
                JsonConvert.SerializeObject(jObject),
                (content) =>
                {
                    LoggerUtils.Log("事件上报成功");
                },
                (error) =>
                {
                    LoggerUtils.Log("事件上报失败");
                });
        }

        public static void ReportTaskUV(TaskUVInfo taskUVInfo) 
        {
            LoggerUtils.Log($"上报UV事件:{taskUVInfo}");
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.TaskUvEvent,
                HttpMethod.POST,
                JsonConvert.SerializeObject(taskUVInfo),
                (content) =>
                {
                    LoggerUtils.Log("事件上报成功");
                },
                (error) =>
                {
                    LoggerUtils.Log("事件上报失败");
                });
        }

        //设置公用属性
        //public static void SetSuperProperties(Dictionary<string, object> superProperties)
        //{
        //    AnalyticsManager.Inst.SetSuperProperties(superProperties);
        //   // Debug.LogError("上报！");
        //}
    }
}