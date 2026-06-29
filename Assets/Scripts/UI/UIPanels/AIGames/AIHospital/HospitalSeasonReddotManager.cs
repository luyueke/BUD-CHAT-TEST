using System;
using System.Collections.Generic;
using UnityEngine;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Message;
using Basic.Utils;

namespace UI.UIPanels.AIGames.AIHospital
{
    public enum HospitalSeasonRedDotType
    {
        GamePlay = 1,
        Activity = 2,
        SeasonPass = 3,
        Store = 4,
    }
    /// <summary>
    /// 医院场景季节面板红点管理器
    /// </summary>
    public class HospitalSeasonReddotManager: MonoBehaviour
    {
        #region 计算活动红点
        // 活动数据缓存
        private Dictionary<string, ActivityInfo> _activityInfoCache = new Dictionary<string, ActivityInfo>();
        // 是否正在请求数据
        private bool _isRequesting = false;
        /// <summary>
        /// 检查指定活动ID是否需要显示红点
        /// </summary>
        /// <param name="activityId">活动ID</param>
        /// <returns>是否显示红点</returns>
        public bool CheckActivityRedDot(ActivityId activityId)
        {
            string activityIdStr = activityId.ToString();
            
            // 如果缓存中有数据，直接计算红点状态
            if (_activityInfoCache.TryGetValue(activityIdStr, out ActivityInfo info))
            {
                return CalculateRedDotState(activityIdStr, info);
            }
            
            // 如果没有缓存数据且未在请求中，尝试获取活动数据
            if (!_isRequesting)
            {
                RefreshActivityData();
            }
            
            // 默认不显示红点
            return false;
        }

        /// <summary>
        /// 刷新活动数据
        /// </summary>
        private void RefreshActivityData()
        {
            if (_isRequesting)
            {
                return;
            }

            _isRequesting = true;

            // 构建请求的活动ID列表
            List<string> activityIdList = new List<string>
            {
                ActivityId.AbandonedHospitalEscape.ToString(),
                // 这里可以添加其他需要监控的活动ID
            };

            // 创建请求对象
            ActivityCenterInfoReq req = new ActivityCenterInfoReq
            {
                idList = activityIdList
            };

            // 发送请求
            NetworkManager.Inst.SendHttpRequest(
                HttpUrlDefine.ActivityList, 
                HttpMethod.POST, 
                JsonConvert.SerializeObject(req), 
                OnGetActivityDataSuccess,
                OnGetActivityDataFailed
            );
        }

        /// <summary>
        /// 获取活动数据成功回调
        /// </summary>
        private void OnGetActivityDataSuccess(string content)
        {
            _isRequesting = false;
            
            // 解析响应数据
            ActivityResponse response = JsonConvert.DeserializeObject<ActivityResponse>(content);
            
            if (response?.list == null || response.list.Count == 0)
            {
                return;
            }
            
            // 更新缓存
            foreach (var activityInfo in response.list)
            {
                if (string.IsNullOrEmpty(activityInfo.activityId))
                {
                    continue;
                }
                
                if (_activityInfoCache.ContainsKey(activityInfo.activityId))
                {
                    _activityInfoCache[activityInfo.activityId] = activityInfo;
                }
                else
                {
                    _activityInfoCache.Add(activityInfo.activityId, activityInfo);
                }
            }
            
            // 通知红点状态更新
            MessageHelper.Broadcast(MessageName.OnHospitalSeasonRedDotStateChanged,  HospitalSeasonRedDotType.Activity);
        }

        /// <summary>
        /// 获取活动数据失败回调
        /// </summary>
        private void OnGetActivityDataFailed(string error)
        {
            _isRequesting = false;
            LoggerUtils.LogError($"获取活动数据失败: {error}");
        }

        /// <summary>
        /// 计算红点状态
        /// </summary>
        private bool CalculateRedDotState(string activityId, ActivityInfo info)
        {
            if (info == null)
            {
                return false;
            }

            // 不同活动类型的红点逻辑
            switch (activityId)
            {
                // 其他活动通用逻辑
                default:
                    return HasUnclaimedRewards(info);
            }
        }

        /// <summary>
        /// 检查活动是否有未领取的奖励
        /// </summary>
        private bool HasUnclaimedRewards(ActivityInfo info)
        {
            // 检查事件列表中是否有未领取的奖励
            if (info.eventList != null && info.eventList.Count > 0)
            {
                return info.eventList.Find(x => x.eventStatus == (int)ClaimStatus.Unlocked) != null;
            }
            
            return false;
        }
        #endregion

        #region 计算SeasonPass红点

        private bool IsSeasonPassHasRedDot = false;
        public void GetSeasonPassReddot(Action<bool> hasRedot)
        {
            SeasonPassDataManager.Inst.GetSeasonPassList(SeasonPassType.S9AbandonedHospitalSeasonPass, (isSuccess, rsp) => {
                if (this == null || gameObject == null) {
                    return;
                }
                if (isSuccess) {
                    if (rsp.rewardList == null)
                    {
                        hasRedot?.Invoke(false);
                    }
                    else
                    {
                        IsSeasonPassHasRedDot = rsp.rewardList.Exists(tmp => tmp.BudRewardStatus == BudRewardStatus.Unlocked) || rsp.paidRewardList.Exists(tmp => tmp.BudRewardStatus == BudRewardStatus.Unlocked);
                        hasRedot?.Invoke(IsSeasonPassHasRedDot);
                    }
                }
                else
                {
                    hasRedot?.Invoke(false);
                }
            });
        }

        #endregion
    }
} 