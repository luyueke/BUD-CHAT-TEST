using Es;
using Game.Event;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AnniversaryStoreExchangeShopItem : MonoBehaviour
{
    // UI组件
    public Text title;
    public Slider slider;
    public Text targetNum;
    public Text num;
    public Button claimBtn;
    public Button goBtn;
    public GameObject overObj;

    public GameObject dailyImage;
    public GameObject weekImage;
    public GameObject ingImage;
    string _taskId;
    int _eventId;
    TaskConfig _config;
    void SetStatus(int statu)
    {
        switch (statu)
        {
            case (int)EventStatus.UnClaim:
                goBtn.gameObject.SetActive(true);
                claimBtn.gameObject.SetActive(false);
                overObj.gameObject.SetActive(false);
                break;
            case (int)EventStatus.Claim:
                goBtn.gameObject.SetActive(false);
                claimBtn.gameObject.SetActive(true);
                overObj.gameObject.SetActive(false);
                break;
            case (int)EventStatus.Finish:
                goBtn.gameObject.SetActive(false);
                claimBtn.gameObject.SetActive(false);
                overObj.gameObject.SetActive(true);
                break;
        }
    }

    public void SetData(TaskConfig config, TaskItemData serverData)
    {
        _config = config;
        title.text = config.title;
        slider.value = serverData.finishAmount / config.num;
        targetNum.text = $"{serverData.finishAmount}/{config.num}";
        num.text = config.rewardNum[0].Split(",")[1];
        _taskId = config.taskId;
        _eventId = config.eventId;
        if (config.taskId == "S11CelebrationStoreDailyTask")
        {
            dailyImage.SetActive(true);
            weekImage.SetActive(false);
        }
        else
        {
            dailyImage.SetActive(false);
            weekImage.SetActive(true);
        }
        SetStatus(serverData.eventStatus);
        goBtn.onClick.AddListener(() => {
            NewbieTaskSkipManager.Inst.HandleSkip(config.progress[0]);
        });
        claimBtn.onClick.AddListener(OnClaimBtnClick);
    }
    void OnClaimBtnClick()
    {
        
    }
}
