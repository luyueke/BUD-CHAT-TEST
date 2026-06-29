using Game.Event;
using Network;
using Network.Http;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using View.UI.PopupPanelSystem.Data;

public class ActivityHandle
{
    public ActivityInfo _info;
    public ActivityBaseView _view;
    public ActivityTabItem _tab;
    public Transform _viewParent;
    public ActivityCenterPanel mainPanel;

    public void ShowView()
    {
        if (_view == null)
        {
            CreateView();
        }
        _view.Show();
        RefreshViewData();
    }

    public void RefreshViewData()
    {
        _view.RefrashData(_info);
    }
    public void CreateView()
    {
        var obj = Loader
            .Load<GameObject>(_info.viewPrefabPath)
            .Instantiate(_viewParent);
        obj.transform.localScale = Vector3.one;
        obj.transform.localPosition = Vector3.zero;
        obj.SetActive(true);
        _view = obj.GetComponent<ActivityBaseView>();
        _view.SetMainPanel(mainPanel);
        _view.Init(_info);
        _view.updateRedDotAction = UpDateRedDot;
    }
    public void HideView()
    {
        if (_view != null)
        {
            _view.Hide();
        }
    }
    public bool IsViewShow()
    {
        return _view != null && _view.IsShow();
    }

    public void UpDateRedDot()
    {
        var reddot = CheckRedDot();
        _tab.UpdateRedDot(reddot);
        if (reddot == false && _info != null && _info.activityId == ActivityId.AlbumActivity.ToString()) //相机任务需要额外请求任务红点
        {
            GetTaskListReq getTaskListReq = new GetTaskListReq()
            {
                idList = new List<string> { TASK_ID.S15TheaterDaily.ToString(), TASK_ID.S15TheaterDailyGrandTotal.ToString() }
            };

            var reqParam = JsonConvert.SerializeObject(getTaskListReq);

            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.TaskList, HttpMethod.POST, reqParam, content =>
            {
                var taskListRsp = JsonConvert.DeserializeObject<TaskListRsp>(content);
                reddot = TaskCanClaim(taskListRsp);
                if(reddot)
                {
                    _tab.UpdateRedDot(true);
                }
            }, failMessage =>
            {
                Debug.LogError("RequestTaskData error =" + failMessage);
            });
        }

    }
    private bool TaskCanClaim(TaskListRsp rsp)
    {
        if (rsp == null || rsp.list?.Count == 0)
        {
            return false;
        }
        for (int i = 0; i < rsp.list.Count; i++)
        {
            var reward = rsp.list[i].eventList.Find(p => p.eventStatus == (int)TaskClaimState.Enable);
            if (reward != null) return true;
        }
        return false;
    }
    public bool CheckRedDot()
    {
        if (_info != null && (_info.activityId == ActivityId.TreePlantingDayWateringActivity.ToString() || _info.activityId == "PlantTree"))
        {
            var sys = PlantTreeSystem.Inst;
            // 日常任务
            var daily = sys != null ? sys.DailyInfo : null;
            if (daily?.list != null)
            {
                for (int i = 0; i < daily.list.Count; i++)
                {
                    var group = daily.list[i];
                    var events = group?.eventList;
                    if (events == null) continue;
                    for (int j = 0; j < events.Count; j++)
                    {
                        var e = events[j];
                        if (e == null) continue;
                        if ((ClaimStatus)e.eventStatus == ClaimStatus.Unlocked)
                        {
                            return true;
                        }
                    }
                }
            }

            // 每日充值
            var recharge = sys != null ? sys.RechargeInfo : null;
            if (recharge?.eventList != null)
            {
                for (int i = 0; i < recharge.eventList.Count; i++)
                {
                    var e = recharge.eventList[i];
                    if (e == null) continue;
                    if ((ClaimStatus)e.eventStatus == ClaimStatus.Unlocked)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        //S4-B 消费狂欢节特殊处理
        if (_info != null && _info.activityId == ActivityId.ConsumeCarnival.ToString())
        {
            return _info?.consumeCarnivalInfo?.reddot == 1;
        }

        // 新年福袋不需要红点
        if (_info != null && _info.activityId == ActivityId.NewYearsTurntable2026.ToString())
        {
            return false;
        }


        // 组队消费特殊逻辑
        if (_info != null && _info.activityId == ActivityId.LaborDayGroupConsume.ToString())
        {
            if (_info?.groupConsumeInfo?.redDot > 0)
            {

                return true;
            }
        }
        // 劳动节特殊逻辑
        if (_info != null && _info.activityId == ActivityId.NewYear2026Act.ToString())
        {
            if (_info?.newYearLoginInfo?.reddot != 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }


        if (_info != null && _info.eventList != null)
        {
            return _info.eventList.Find(x => x.eventStatus == (int)ClaimStatus.Unlocked) != null;
        }

        return false;
    }
    public void Release()
    {
        GameObject.Destroy(_view?.gameObject);
        GameObject.Destroy(_tab?.gameObject);
    }
}