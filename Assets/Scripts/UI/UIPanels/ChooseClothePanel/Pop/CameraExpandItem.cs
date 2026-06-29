using System;
using System.Collections;
using System.Linq;
using Es;
using Game.Event;
using Message;
using Newtonsoft.Json;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class CameraExpandItem : MonoBehaviour
{
    public Text AddTxt;

    public Text Des;

    public Text Progress;

    public GameObject Lock;

    public CButton ClaimBtn;

    public CButton GoBtn;

    public GameObject Claimed;

    private Action<TaskItemData> _onClaim;
    private TaskItemData _data;
    private TaskConfig _taskConfig;
    private string _taskId = "AlbumExpansionTask";
    private void Awake()
    {
        ClaimBtn.onClick.AddListener(OnClaimBtn);
        GoBtn.onClick.AddListener(OnGoBtn);
    }
    void OnGoBtn()
    {
        if(_data.eventStatus == 1 && _taskConfig.progress.Count > 0)
        {
            NewbieTaskSkipManager.Inst.HandleSkip(_taskConfig.progress[0]);
        }
    }
    void OnClaimBtn() 
    {
        if(_data.eventStatus == 2)
        {
            OnClaimBtnClick();
        }
        //_onClaim?.Invoke(_data);
    }

    public void SetData(TaskItemData data, Action<TaskItemData> onClaim, int idx) 
    {
        var configs = Es.DataTables.GetTaskConfigList();
        var _taskConfigs = configs.Where(config => config.taskId == _taskId).ToList();
        _taskConfig = _taskConfigs.FirstOrDefault(config => config.eventId == data.eventId);
        if(_taskConfig == null)
        {
            LoggerUtils.LogError($"taskConfig is null, eventId: {data.eventId}");
            return;
        }

        _data = data;
        _onClaim = onClaim;

        AddTxt.text = "+" + _taskConfig.rewardNum[0].Split(",")[1];

        Progress.text = $"{data.finishAmount}/{data.targetAmount}";

        if (_taskConfig.title.Contains("{0}"))
        {
            Des.text = string.Format(_taskConfig.title, data.targetAmount);
        }
        else
        {
            Des.text = _taskConfig.title;
        }

        Lock.gameObject.SetActive(data.eventStatus == 1 && _taskConfig.progress.Count == 0);

        Claimed.gameObject.SetActive(data.eventStatus == 3);
        
        GoBtn.gameObject.SetActive(data.eventStatus == 1 && _taskConfig.progress.Count > 0);

        ClaimBtn.gameObject.SetActive(data.eventStatus == 2);
    }

    void OnClaimBtnClick()
    {
        EventCenterDataManager.Inst.CliamReward(_taskId, _data.eventId, 1, 0, (claimRspData) =>
        {
            GoBtn.gameObject.SetActive(false);
            ClaimBtn.gameObject.SetActive(false);
            Claimed.gameObject.SetActive(true);
            MessageHelper.Broadcast(MessageName.OnAlbumPhotoDataChanged, -1);
        });
    }
}