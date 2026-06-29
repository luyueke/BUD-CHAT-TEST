using Es;
using Game.Avatar;
using GameData.PgcData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class NewYearsFortune : PackBaseView
{
    [SerializeField] internal Transform characterRoot;
    [SerializeField] private AvatarCameraController avatarCameraController;

    [SerializeField] private List<Text> endTimes;
    [SerializeField] private List<Text> fortures;
    [SerializeField] private Text youyouNum;
    [SerializeField] private CButton buyButton;
    [SerializeField] private GameObject notJoinRoot;
    [SerializeField] private GameObject joinedRoot;
    [SerializeField] private Text winnerText;
    [SerializeField] private CButton roleButton;
    [SerializeField] private Sprite bigReward;

    internal BaseAvatarWrapper avatarWrapper;

    private NewYearLuckyLottery newYearLuckyLottery;

    private void Awake()
    {
        var saveCharacterData = AccountDataManager.Inst.UserInfo.avatarInfo;

        var characterWrapper = AvatarController.Inst.CreateUIAvatarWithIKController(saveCharacterData, characterRoot);
        avatarWrapper = characterWrapper;
        avatarCameraController.RotateTarget = characterRoot;
        OnWearAvatar("10400427");
        OnWearAvatar("10900434");
        OnWearAvatar("10100098");
        OnWearAvatar("11300318");
        OnWearAvatar("11000214");
        OnWearAvatar("10500036");
    }

    internal void OnWearAvatar(string pgcId)
    {
        var config = DataTables.GetAvatarCommonData(pgcId);
        if (config == null) return;
        var classType = UniqueType.GetAvatar(pgcId);
        avatarWrapper.ChangePart(classType, pgcId);
        avatarWrapper.ChangeColor(classType, config.defaultColor);
        avatarWrapper.Move(classType, config.pDef);
        avatarWrapper.Rotate(classType, config.rDef);
        avatarWrapper.Scale(classType, config.sDef);
        avatarWrapper.HVScale(classType, config.vhSDef);
        avatarWrapper.SetLeftOrRight(classType, config.leftRightType);
    }

    private void Start()
    {
        InitClickListener();
        GetProductInfo();
    }

    protected new void GetProductInfo()
    {
        day = DateTime.Now.Day;
        IAPDataManager.Inst.GetProductInfo(res =>
        {
            newYearLuckyLottery = res.newYearLuckyLottery;
            OnServerDataUpdate();
        });
    }

    private void InitClickListener()
    {
        buyButton.onClick.AddListener(OnBuyBtnClick);
        roleButton.onClick.AddListener(OnRoleBtnClick);
    }

    private void OnBuyBtnClick()
    {
        if (newYearLuckyLottery == null)
        {
            return;
        }

        if (IAPDataManager.Inst.IsOfficialChannel())
        {
            ProductInfo productInfo = newYearLuckyLottery.productInfo;
            ConfirmPaymentPanel panel =
                UIManager.Inst.OpenPanel<ConfirmPaymentPanel>(PanelId.ConfirmPaymentPanel, productInfo.price);
            panel.SetCallback(paymentType => { Purchase(paymentType, productInfo, productInfo.productName); });
            return;
        }

        Purchase(ConfirmPaymentPanel.PaymentType.Default, newYearLuckyLottery.productInfo, newYearLuckyLottery.productInfo.productName);
    }

    private void OnRoleBtnClick()
    {
        var rulePanel = UIManager.Inst.OpenPanel<GashaponRulePanel>(PanelId.GashaponRulePanel);
        rulePanel.SetPanelTitle("规则说明");
        rulePanel.AddDesc("");
        rulePanel.AddDesc("1. 活动期间花费1元参与活动，可获得优优币*1，并赠送三个福运签，若三个福运签签名一致，则另外加赠优优币*5；");
        rulePanel.AddDesc("2. 已解锁当日福运签的玩家自动参与次日0点的全服抽奖，拥有从福运奖池中获得随机奖励的机会，并可再次付费抽取次日福运签；");
        rulePanel.AddDesc("3. 活动结束后活动界面将关闭，未领取奖励将通过邮件发放；");
        rulePanel.AddDesc("4. 获得奖励的概率为：\n福神露琪儿套装或社区商品币*10：17%\n优优币*1：16.6%\n幸运币*1：16.6%\n徽章*10：16.6%\nVIP体验卡*1天：16.6%\n创作者能量币*50：16.6%");
    }

    public void OnServerDataUpdate()
    {
        if (newYearLuckyLottery == null) return;
        RefreshView(newYearLuckyLottery);
    }

    protected override void OnBuySuccess(string orderId)
    {
        newYearLuckyLottery.isPaid = 1;
        ClaimReward(ClaimType.Buy);
    }

    public void RefreshView(NewYearLuckyLottery data)
    {
        if (data != null)
        {
            buyButton.gameObject.SetActive(data.isPaid != 1);
            joinedRoot.gameObject.SetActive(data.isPaid == 1);
            notJoinRoot.gameObject.SetActive(data.isPaid != 1);

            if (data.lotteryList != null && data.lotteryList.Count > 3)
            {
                var index = 0;
                fortures.ForEach(f => f.text = data.lotteryList[index++]);

                youyouNum.text = data.lotteryList[0] == data.lotteryList[1] && data.lotteryList[1] == data.lotteryList[2] ? "x6" : "x1";
            }
            else
            {
                youyouNum.text = "";
            }

            if (!string.IsNullOrEmpty(data.firstPrizeUserNick)) winnerText.text = $"昨日福运大奖得主：{data.firstPrizeUserNick}";

            if (data.rewardStatus == (int)ClaimStatus.Unlocked)
            {
                ClaimReward(ClaimType.BigReward);
            }
        }
        else
        {
            buyButton.gameObject.SetActive(false);
            joinedRoot.gameObject.SetActive(false);
            notJoinRoot.gameObject.SetActive(true);
        }
    }

    public enum ClaimType
    {
        Null,
        Buy,
        BigReward
    }

    public class TaskClaimRsp
    {
        public List<TaskClaimRewardData> rewardList;
        public List<string> lotteryList;
        public int isFirstPrize;
    }

    public class TaskClaimRewardData
    {
        public int amount;
        public int rewardType;
    }

    /// <summary>
    /// 领取奖励
    /// </summary>
    /// <param name="type"></param>
    private void ClaimReward(ClaimType type)
    {
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.claimNewYearLuckyLottery,
            HttpMethod.POST,
            JsonConvert.SerializeObject(new JObject() { ["claimType"] = (int)type }),
            onReceive: arg0 =>
            {
                TaskClaimRsp response = JsonConvert.DeserializeObject<TaskClaimRsp>(arg0);

                if (type == ClaimType.Buy)
                {
                    if (response.rewardList != null)
                    {
                        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
                        panel.ShowRewards(new List<CommonRewardItemData>()
                        {
                            new CommonRewardItemData()
                            {
                                rewardType = response.rewardList[0].rewardType,
                                RewardAmount = response.rewardList[0].amount,
                                rewardName = PgcUtils.GetRewardName((BUDRewardType)response.rewardList[0].rewardType)
                            }
                        });
                        AccountDataManager.Inst.BalanceInfo.Refresh();
                        ReddotManagerUtils.Inst.RefreshRedDot();
                    }


                    buyButton.gameObject.SetActive(false);
                    joinedRoot.gameObject.SetActive(true);
                    notJoinRoot.gameObject.SetActive(false);

                    if (response.lotteryList != null)
                    {
                        var index = 0;
                        fortures.ForEach(f => f.text = response.lotteryList[index++]);

                        youyouNum.text = response.lotteryList[0] == response.lotteryList[1] && response.lotteryList[1] == response.lotteryList[2] ? "x6" : "x1";
                    }
                }
                else
                {
                    newYearLuckyLottery.rewardStatus = (int)ClaimStatus.Claimed;
                    var panel = UIManager.Inst.OpenPanel<NewYearsFortuneRewardPanel>(PanelId.NewYearsFortuneRewardPanel);
                    if (response.isFirstPrize == 1)
                    {
                        panel.SetData(true, bigReward, $"", winnerText.text);
                    }
                    else
                    {
                        panel.SetData(false, PgcUtils.LoadRewardIcon((BUDRewardType)response.rewardList[0].rewardType, panel.gameObject), $"x{response.rewardList[0].amount}", winnerText.text);
                    }

                    AccountDataManager.Inst.BalanceInfo.Refresh();
                    ReddotManagerUtils.Inst.RefreshRedDot();
                }
            }, onFail: arg0 =>
            {

            }, retryCount: 3);
    }


    private int day = 0;
    private void LateUpdate()
    {
        var now = DateTime.Now;
        if (now.Minute > 0 && now.Second > 0)
        {
            endTimes[0].text = formatTime(23 - now.Hour);
        }
        else
        {
            endTimes[0].text = formatTime(24 - now.Hour);
        }

        if (now.Second > 0)
        {
            endTimes[1].text = formatTime(59 - DateTime.Now.Minute);
        }
        else
        {
            endTimes[1].text = formatTime(60 - DateTime.Now.Minute);
        }

        if (now.Second > 0)
        {
            endTimes[2].text = formatTime(60 - DateTime.Now.Second);
        }
        else
        {
            endTimes[2].text = formatTime(0);
        }

        if (day != now.Day)
        {
            GetProductInfo();
        }
    }

    private string formatTime(int num)
    {
        return num <= 9 ? "0 " + num : $"{num / 10} {num % 10}";
    }
}
