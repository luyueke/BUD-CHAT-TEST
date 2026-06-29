using System.Collections.Generic;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class SVipRewardView : MonoBehaviour
{
    [SerializeField] private CButton rootBtn;
    [SerializeField] private Text endTxt;
    [SerializeField] private GameObject tomorrowObj;
    [SerializeField] private Image acceptRewardBtn;


    private SubscribeStatusResponse _subscribeStatusResponse;

    public void SetData(SubscribeStatusResponse subscribeStatusResponse)
    {
        this._subscribeStatusResponse = subscribeStatusResponse;
        endTxt.SetLocalText("SVIP年卡有效倒计时：{0}" , subscribeStatusResponse.remainTime);
        int dailyrewardStatus = subscribeStatusResponse.dailyRewardStatus;
        tomorrowObj.gameObject.SetActive(dailyrewardStatus == 1);
        acceptRewardBtn.gameObject.SetActive(dailyrewardStatus == 0);
    }

    private void Start()
    {
        rootBtn.onClick.AddListener(() =>
        {
            IAPDataManager.Inst.GetSubScribeReward(isSuccess =>
            {
                if (isSuccess)
                {
                    var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
                    panel.ShowRewards(new List<TaskRewardData>()
                    {
                        new TaskRewardData()
                        {
                            num = 350,
                            rewardType = (int)BUDRewardType.RewardCoin,
                        },
                        new TaskRewardData()
                        {
                            num = 10,
                            rewardType = (int)BUDRewardType.RewardBadge,
                        }
                    });
                    acceptRewardBtn.gameObject.SetActive(false);
                    tomorrowObj.gameObject.SetActive(true);
                    
                    AccountDataManager.Inst.BalanceInfo.Refresh();

                }
            });
        });
    }
}