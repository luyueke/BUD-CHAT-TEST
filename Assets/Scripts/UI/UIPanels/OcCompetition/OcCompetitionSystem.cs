using Game.COSXML.Utils;
using Game.Store;
using GameData;
using GameData.Base;
using GameData.BaseInfo;
using GameData.Manager;
using GameData.UGCData;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UI.UIPanels.FittingRoom;
using UnityEngine;

namespace GameUI
{
    public class OcCompetitionSystem : GlobalInstance<OcCompetitionSystem>
    {
        public OcCompetitionSystemData data = new OcCompetitionSystemData();


        public bool InSubmission()
        {
            return data.ContestInfo.status == (int)ContestStatus.Submission;
        }

        public bool Exist()
        {
            ContestInfo content = data.ContestInfo;
            if (content == null)
            {
                content = LobbyInfoManager.Inst.GetContestInfo(BUDContestType.OC);
            }

            if (content == null)
            {
                return false;
            }

            return content.status == (int)ContestStatus.Submission || content.status == (int)ContestStatus.InProgress;
        }


        #region UI
        public void OpenPanel(ContestInfo content = null)
        {
            data.RankListMsg = null;
            if (content == null)
            {
                content = LobbyInfoManager.Inst.GetContestInfo(BUDContestType.OC);
            }
            ContestDataManager.Inst.RefreshContestInfo(content.contestId, (bo, info) =>
            {
                if (bo && info != null)
                {
                    data.ContestInfo = info;

                    if (Exist())
                    {
                        var panel = UIManager.Inst.FindPanel<FittingRoomPanel>(PanelId.FittingRoomPanel);
                        if (panel != null)
                        {
                            panel.ShowAvatar(false);
                        }

                        UIManager.Inst.OpenPanel(PanelId.OcCompetitionPanel);
                    }
                }
            });

        }

        public void OpenPreRewardPanel()
        {
            UIManager.Inst.OpenPanel(PanelId.OcCptPreRewardPanel);
        }
        #endregion


        #region Web
        // 参赛
        public void Join(OcInfo ocData, PoseInfo poseId, string coverUrl, Action ac, int setType = 0, string oldCreation = "")
        {
            var req = new JoinContestReq()
            {
                contestIds = new List<string>() { data.ContestInfo.contestId },
                creationId = ocData.ocId,
                poseId = poseId.id,
                cover = coverUrl,
                setType = setType,
                oldCreation = oldCreation
            };
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ContestJoin, HttpMethod.POST, JsonConvert.SerializeObject(req), (response) =>
            {
                ac?.Invoke();
                MessageHelper.Broadcast(MessageName.OnRefreshTaskDataAfterBack);
            },
            (fail) =>
            {
                Debug.LogError("设子大赛参赛 contest/ocEntryList  " + fail);
            });
        }

        // 作品列表
        // 1 all entries 换一换列表 2 my entry 3 top100 entries
        public void OcWorks(OcCompetitionListType listType, Action ac, bool isChange = false)
        {
            JObject req = null;
            var contestId = data.ContestInfo.contestId;
            if (listType == OcCompetitionListType.RankWork && InSubmission())
            {
                contestId = data.ContestInfo.lastOCContestId;
            }
            switch (listType)
            {
                case OcCompetitionListType.AllWork:
                case OcCompetitionListType.ChangeWork:
                    req = new JObject
                    {
                        ["contestId"] = contestId,
                        ["listType"] = (int)listType,
                        ["cookie"] = data.VoteListMsg?.cookie,
                    };
                    break;
                case OcCompetitionListType.MyWork:
                    req = new JObject
                    {
                        ["contestId"] = contestId,
                        ["listType"] = (int)listType,
                    };
                    break;
                case OcCompetitionListType.RankWork:
                    if (data.RankListMsg != null && data.RankListMsg.isEnd == 1)
                    {
                        return;
                    }
                    req = new JObject
                    {
                        ["contestId"] = contestId,
                        ["listType"] = (int)listType,
                        ["cookie"] = data.RankListMsg?.cookie,
                    };
                    break;
            }

            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.OcEntryList, HttpMethod.GET, JsonConvert.SerializeObject(req), (response) =>
            {
                var msg = JsonConvert.DeserializeObject<OcCptListMsg>(response);
                switch (listType)
                {
                    case OcCompetitionListType.AllWork:
                    case OcCompetitionListType.ChangeWork:
                        data.VoteListMsg = msg;
                        //换一换成功客户端主动减一 无需请求活动大量信息
                        if (isChange)
                        {
                            data.ContestInfo.changeTimes -= 1;
                        }
                        if (msg.list == null || msg.list.Count < 2)
                        {
                            TipPanel.ShowToast("已无更多参赛作品");
                        }
                        break;
                    case OcCompetitionListType.MyWork:
                        data.MyListMsg = msg;
                        break;
                    case OcCompetitionListType.RankWork:
                        if (data.RankListMsg == null)
                        {
                            data.RankListMsg = msg;
                        }
                        else
                        {
                            data.RankListMsg.list.AddRange(msg.list);
                            data.RankListMsg.cookie = msg.cookie;
                            data.RankListMsg.userRankData = msg.userRankData;
                        }
                        break;
                }

                ac?.Invoke();
                MessageHelper.Broadcast(MessageName.RefreshOcTimes);
            },
            (fail) =>
            {
                Debug.LogError("设子大赛列表 contest/ocEntryList  " + fail);
            });
        }

        // 作品投票
        public void OcVote(string contestId, string creationId)
        {
            var req = new JObject
            {
                ["contestId"] = contestId,
                ["creationId"] = creationId,
            };
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.OcVote, HttpMethod.POST, JsonConvert.SerializeObject(req), (response) =>
            {
                var msg = JsonConvert.DeserializeObject<RewardRsp>(response);

                // 打开通用奖励展示面板
                var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);

                var rewards = msg.rewardList;

                // 创建用于显示的奖励数据列表
                var taskRewardDatas = new List<CommonRewardItemData>();
                foreach (var reward in rewards)
                {
                    var item = new CommonRewardItemData();
                    item.rewardType = reward.RewardType;
                    item.RewardAmount = reward.Amount;
                    //item.rewardName = ;
                    //item.pgcId = reward.pgcIdList[0];
                    taskRewardDatas.Add(item);
                }

                //投票成功客户端主动减一 无需请求活动大量信息
                data.ContestInfo.voteTimes -= 1;

                AccountDataManager.Inst.BalanceInfo.Refresh();

                // 显示所有奖励
                panel.ShowRewards(taskRewardDatas);
                if (data.ContestInfo.voteTimes > 0)
                {
                    panel.ShowCommonBtn("继续评选", () =>
                    {
                        var root = UIManager.Inst.FindPanel<OcCompetitionPanel>(PanelId.OcCompetitionPanel);
                        root.MyGroup.VoteGroup.GetVote();
                    });
                }
                else
                {
                    panel.ShowCommonBtn("确定", () =>
                    {
                        var root = UIManager.Inst.FindPanel<OcCompetitionPanel>(PanelId.OcCompetitionPanel);
                        root.MyGroup.SetStep(OcCompetitionStep.All);
                    });
                }

                MessageHelper.Broadcast(MessageName.RefreshOcTimes);
            },
            (fail) =>
            {
                Debug.LogError("设子大赛投票 contest/vote  " + fail);
            });
        }


        //设子信息
        public void OcInfoListReq(Action<List<OcServerData>> resultAction)
        {
            if (data.OcListRsp != null && data.OcListRsp.IsEnd == 1)
            {
                resultAction?.Invoke(data.OcListRsp.list);
                return;
            }

            JObject jb = new JObject
            {
                ["skinType"] = (int)SkinType.Avatar,  // 0 设字  1宠物
                ["cookie"] = data.OcListRsp?.cookie,
            };
            var reqParam = JsonConvert.SerializeObject(jb);
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ocList, HttpMethod.GET, reqParam,
            onReceive: content =>
            {
                data.OcListRsp = JsonConvert.DeserializeObject<OcListPageUseData>(content);

                if (data.OcListRsp == null)
                {
                    return;
                }
                if (data.OcListRsp.list != null)
                {
                    data.OcListItems.AddRange(data.OcListRsp.list);
                }
                resultAction?.Invoke(data.OcListRsp.list);
            },
            onFail: error => { LoggerUtils.LogError("OcInfoListReq error = " + error); });
        }

        public void OcInfoReq(string ocId, Action<ContestOCInfoRsp> ac)
        {
            var req = new JObject()
            {
                ["ocId"] = ocId,
                ["contestId"] = data.ContestInfo.contestId,
            };
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ContestOcInfo, HttpMethod.GET, JsonConvert.SerializeObject(req), (response) =>
            {
                var ocInfo = JsonConvert.DeserializeObject<ContestOCInfoRsp>(response);
                ac?.Invoke(ocInfo);

            },
            (error) =>
            {
                LoggerUtils.LogError("OcInfoReq error = " + error);
            });
        }

        //保存设子
        public void SetOcReq(string ocCover, string avatarJson, SkinType skinType, Action<bool> ac)
        {

            OcInfo info = new OcInfo()
            {
                ocCover = ocCover,
                avatarJson = avatarJson,
                skinType = (int)skinType
            };

            OcSetReq req = new OcSetReq
            {
                setType = 0,
                ocInfo = info
            };
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.setOc, HttpMethod.POST, JsonConvert.SerializeObject(req),
                onReceive: arg0 =>
                {
                    ac?.Invoke(true);
                },
                onFail: arg0 =>
                {
                    ac?.Invoke(false);
                }
            );
        }
        #endregion
    }
}