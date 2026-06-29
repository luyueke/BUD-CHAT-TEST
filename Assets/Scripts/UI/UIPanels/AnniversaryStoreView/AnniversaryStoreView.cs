using Es;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class AnniversaryStoreView : MonoBehaviour
{
    public List<Toggle> toggles;

    public List<GameObject> childViews;
    public AnniversaryStoreTaskView taskView;
    public AnniversaryStoreGiftView giftView;
    Dictionary<int, int> viewsRedNum;
    int reddotSum=0;
    public Text lastTime;
    // 活动固定时间范围
    private static readonly DateTime ACTIVITY_START_TIME = new DateTime(2025, 7, 28);
    private static readonly DateTime ACTIVITY_END_TIME = new DateTime(2025, 9, 1);
    private void Start()
    {
        InitView();
        InitTogles();
        UpdateLastTime();
    }
    void InitView()
    {

        giftView.Init((i) =>
        {   
            toggles[2].transform.Find("reddot").gameObject.SetActive(i > 0);
        });
        taskView.Init((i) =>
        {
            toggles[0].transform.Find("reddot").gameObject.SetActive(i > 0);
        });
        for (int i = 0; i < 3; i++)
        {
            toggles[i].transform.Find("reddot").gameObject.SetActive(AnniversaryStoreMgr.Inst.reddotNums[i] > 0);
        }

    }
    public void  InitTogles()
    {
        for (int i = 0; i < toggles.Count; i++)
        {
            int index = i;  // 创建局部变量
            toggles[i].onValueChanged.AddListener((bool isOn) =>
            {
                if (isOn)
                {
                    OnClickTogle(index);  // 使用局部变量
                    toggles[index].transform.Find("Label").GetComponent<Text>().color = new Color(0.816f, 0.361f, 0.114f, 1f);
                }
                else
                {
                    toggles[index].transform.Find("Label").GetComponent<Text>().color = new Color(1f, 1f, 1f, 1f);
                }
            });
        }
        OnClickTogle(0);
    }


    void OnClickTogle(int index)
    {
        for(int i=0;i<childViews.Count;i++)
        {
            childViews[i].gameObject.SetActive(i == index);
        }

    }
    private void UpdateLastTime()
    {
        if (lastTime == null) return;

        // 获取当前时间
        DateTime now = DateTime.Now;

        // 如果已经超过结束时间
        if (now >= ACTIVITY_END_TIME)
        {
            lastTime.text = "活动已结束";
            return;
        }

        // 如果还没到开始时间
        if (now < ACTIVITY_START_TIME)
        {
            lastTime.text = "活动未开始";
            return;
        }

        // 计算剩余时间
        TimeSpan remainingTime = ACTIVITY_END_TIME - now;

        // 格式化显示
        string timeText = string.Format("{0}天{1}小时",
            remainingTime.Days,
            remainingTime.Hours);

        lastTime.text = timeText;
    }
}
