using System;
using System.Collections.Generic;
using Game.Event;
using UnityEngine;
using UnityEngine.UI;

public class NewbieV3TaskItem : MonoBehaviour
{
    public Button rootBtn;
    public Text title;

    public GameObject activityBg;
    public GameObject defaultBg;
    public GameObject lockImage;
    public GameObject selectBg;

    public TaskItemData _taskItemData;
    public int _curCount;
    private Action<int> _clickAction;


    private void Start()
    {
        rootBtn.onClick.AddListener(RootBtnClick);
    }

    private void RootBtnClick()
    {
        _clickAction?.Invoke(_curCount);
    }
    public void SetSelect(bool isSelect)
    {
        activityBg.SetActive(!isSelect);
        selectBg.SetActive(isSelect);
        if (isSelect)
        {
            title.rectTransform.eulerAngles = new Vector3(0, 0, 4.88f);
        }
        else
        {
            title.rectTransform.eulerAngles = Vector3.zero;
        }
        if (_taskItemData.eventStatus == (int)EventStatus.UnClaim) {
            defaultBg.SetActive(!isSelect);
        }
    }
    public void SetData(TaskItemData taskItemData, Action<int> clickAction , int curCount)
    {
        _taskItemData = taskItemData;
        _curCount = curCount;
        this._clickAction = clickAction;
        if (taskItemData == null)
        {
            return;
        }
        title.text = $"第{curCount}天";
        SetStatus(taskItemData.eventStatus);
    }

    void SetStatus(int statu)
    {   
        if(statu == (int)EventStatus.UnClaim)
        {
            defaultBg.SetActive(true);
            activityBg.SetActive(false);
            selectBg.SetActive(false);
            lockImage.SetActive(true);
        }
        else
        {
            defaultBg.SetActive(false);
            lockImage.SetActive(false);
        }
    }

}
