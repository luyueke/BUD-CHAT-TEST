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
using Game.Event;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;
using UI.UIPanels.RechargePanel;

public class SleepLittleKoi : PackBaseView
{
    [SerializeField] internal Transform characterRoot;
    [SerializeField] private AvatarCameraController avatarCameraController;

    [SerializeField] private List<Text> endTimes;
    [SerializeField] private List<Image> fortures_pillows;
    [SerializeField] private Text youyouNum;
    [SerializeField] private CButton buyButton;
    [SerializeField] private GameObject notJoinRoot;
    [SerializeField] private GameObject joinedRoot;
    [SerializeField] private Text winnerText;
    [SerializeField] private CButton roleButton;
    [SerializeField] private Sprite bigReward;

    internal BaseAvatarWrapper avatarWrapper;

    private NewYearLuckyLottery sleepyKoiLuckyLottery;
    private string atlasPath = RechargePanel.RechargePanelAtlas;


    private void Awake()
    {
        var saveCharacterData = AccountDataManager.Inst.UserInfo.avatarInfo;

        var characterWrapper = AvatarController.Inst.CreateUIAvatarWithIKController(saveCharacterData, characterRoot);
        avatarWrapper = characterWrapper;
        avatarCameraController.RotateTarget = characterRoot;
        //瞌睡苏西拖鞋
        OnWearAvatar("11300298");
        //瞌睡苏西兔兔眼罩	
        OnWearAvatar("12100029");
        //瞌睡苏西睡帽
        OnWearAvatar("10900410");
        //瞌睡苏西玩具熊
        OnWearAvatar("11000195");
        //瞌睡苏西长发
        OnWearAvatar("10800104");
        //瞌睡苏西连衣裙
        OnWearAvatar("10400399");
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
        EventTracking.LoadEvent.ReportPopupStatus(RechargeId.SleepyCoi.ToString());
        InitClickListener();
        GetProductInfo();
        EventCenterDataManager.Inst.ReportTask(PostEventId.ViewSleepyKoi);

    }





    protected new void GetProductInfo()
    {
        day = DateTime.Now.Day;
        IAPDataManager.Inst.GetProductInfo(res =>
        {
            sleepyKoiLuckyLottery = res.sleepyKoiLuckyLottery;
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
        if (sleepyKoiLuckyLottery == null)
        {
            return;
        }

        if (IAPDataManager.Inst.IsOfficialChannel())
        {
            ProductInfo productInfo = sleepyKoiLuckyLottery.productInfo;
            ConfirmPaymentPanel panel =
                UIManager.Inst.OpenPanel<ConfirmPaymentPanel>(PanelId.ConfirmPaymentPanel, productInfo.price);
            panel.SetCallback(paymentType => { Purchase(paymentType, productInfo, productInfo.productName); });
            return;
        }

        Purchase(ConfirmPaymentPanel.PaymentType.Default, sleepyKoiLuckyLottery.productInfo, sleepyKoiLuckyLottery.productInfo.productName);
    }

    private void OnRoleBtnClick()
    {
        var rulePanel = UIManager.Inst.OpenPanel<GashaponRulePanel>(PanelId.GashaponRulePanel);
        rulePanel.SetPanelTitle("规则说明");
        rulePanel.AddDesc("");
        rulePanel.AddDesc("1. 活动期间花费1元参与活动，可获得1个优优币，并赠送三个任意颜色的瞌睡枕，若三个瞌睡枕颜色一致，则另外加赠5个优优币；");
        rulePanel.AddDesc("2. 已解锁当日瞌睡枕的玩家自动参与次日0点的全服抽奖，拥有从幸运奖池中获得随机奖励的机会，并可再次付费抽取次日瞌睡枕；");
        rulePanel.AddDesc("3. 活动结束后活动界面将关闭，未领取奖励将通过邮件发放；");
        rulePanel.AddDesc("4. 获得奖励的概率为：\n瞌睡苏西套装或社区商品币*10：17%\n优优币*1：16.6%\n幸运币*1：16.6%\n徽章*10：16.6%\nVIP体验卡*1天：16.6%\n创作者能量币*50：16.6%");
    }

    public void OnServerDataUpdate()
    {
        if (sleepyKoiLuckyLottery == null) return;
        RefreshView(sleepyKoiLuckyLottery);
    }

    protected override void OnBuySuccess(string orderId)
    {
        sleepyKoiLuckyLottery.isPaid = 1;
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
                for (int i = 0; i < data.lotteryList.Count; i++)
                {
                    var spName = "pillow_" + data.lotteryList[i];
                    var sp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath, spName, this.gameObject);
                    fortures_pillows[i].sprite = sp;
                }

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
            JsonConvert.SerializeObject(new JObject()
            {
                ["claimType"] = (int)type,
                ["luckyLotteryType"] = 1,
            }),
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
                        for (int i = 0; i < response.lotteryList.Count; i++)
                        {
                            var spName = "pillow_" + response.lotteryList[i];
                            var sp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath, spName, this.gameObject);
                            fortures_pillows[i].sprite = sp;
                        }
                        
                        youyouNum.text = response.lotteryList[0] == response.lotteryList[1] && response.lotteryList[1] == response.lotteryList[2] ? "x6" : "x1";
                    }
                }
                else
                {
                    sleepyKoiLuckyLottery.rewardStatus = (int)ClaimStatus.Claimed;
                    var panel = UIManager.Inst.OpenPanel<NewYearsFortuneRewardPanel>(PanelId.NewYearsFortuneRewardPanel);
                    var spName = "SleepLittleKoi_RwardBg";
                    var sp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath, spName, this.gameObject);
                    panel.SetRewardBg(sp);
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

