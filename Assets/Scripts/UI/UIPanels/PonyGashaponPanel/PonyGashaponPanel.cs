using System;
using System.Collections;
using System.Collections.Generic;
using BUD.AnimPose;
using Es;
using Game.Avatar;
using Game.Database;
using Game.Vehicle.PGCVehicle;
using GameData.Gashapon;
using GameData.PgcData;
using GameData.Rewards;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UI.BaseWidgets;
using UI.Manager;
using UI.UIPanels.CreaterRewardPanel;
using UI.UIPanels.GashaponPanel;
using UI.UIPanels.RechargePanel;
using UnityEngine;
using UnityEngine.UI;

public class PonyGashaponPanel : BaseGashaponView
{

    [SerializeField] private CButton TwistBtn;
    [SerializeField] private CButton Twist10Btn;
    [SerializeField] private Text OncePrice;
    [SerializeField] private Text OnceBeforePrice;
    [SerializeField] private Text OnceDiscountText;
    [SerializeField] private Transform OnceDiscount;
    [SerializeField] private Text TenPrice;
    [SerializeField] private Text TenBeforePrice;
    [SerializeField] private Text TenDiscountText;
    [SerializeField] private Transform TenDiscount;
    [SerializeField] private CButton InfoBtn;
    [SerializeField] private CButton PreviewBtn;
    [SerializeField] private CButton SeasonBtn;
    [SerializeField] private CButton RewardShowBtn;
    [SerializeField] private Text tipsText;
    [SerializeField] private GameObject rotateMaskGo;

    // [SerializeField] private Text shuijingText;
    // [SerializeField] private Text suipianText;


    [SerializeField] private PonyGashaponStarLayout ponyGashaponStarLayout;
    [SerializeField] private PonyGashaponTurntable ponyGashaponTurntable;
    [SerializeField] internal Transform characterRoot;
    [SerializeField] internal AvatarCameraController avatarCameraController;
    //人物3d预览
    private int m_CurSelectType;
    string bundleViewBgColor = "#FF9B9B";

    internal PlayerAnimationCtrl animationCtrl;
    internal AnimIKController animationCtrlIK;
    internal PlayerHoldBehaviour playerHold;
    internal CharacterWrap otherCharacterWrap;
    internal BaseAvatarWrapper avatarWrapper;
    internal BaseAvatarData saveAvatarData;
    internal PlayerAnimationCtrl otherAnimationCtrl;
    internal AnimIKController otherAnimationCtrlIK;

    bool isRotating = false;

    int gachaRound = -1;//扭蛋时 当前的星星数

    //round:0~6  roundIndex:0~2
    protected CurrencyType exchangeCurrency = CurrencyType.PurpleDreamCoin;

    override public void OnCreate(string id)
    {
        base.OnCreate(id);
        PreviewBtn.onClick.AddListener(OnPriviewBtnClick);
        SeasonBtn.onClick.AddListener(OnSeasonBtnClick);
        InfoBtn.onClick.AddListener(OnInfoClick);
        TwistBtn.onClick.AddListener(OnTwistClick);
        Twist10Btn.onClick.AddListener(OnTenTwistClick);
        RewardShowBtn.onClick.AddListener(OnPriviewBtnClick);
        rotateMaskGo.SetActive(false);
        InitData();
        InitCharacterWrapper();
    }



    override public void OnShow()
    {
        base.OnShow();

        GetSeverRefresh();
        UpdateWidget();
        LoadVehicle();
    }

    private void InitCharacterWrapper()
    {
        var saveCharacterData = AccountDataManager.Inst.UserInfo.avatarInfo;

        var characterWrapper = AvatarController.Inst.CreateUIAvatarWithIKController(saveCharacterData, characterRoot);
        animationCtrl = characterWrapper.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
        animationCtrlIK = characterWrapper.Avatar.GetComponent<AnimIKController>();
        playerHold = characterWrapper.Avatar.GetComponentInChildren<PlayerHoldBehaviour>();
        avatarCameraController.RotateTarget = characterRoot;

        otherCharacterWrap = AvatarController.Inst.CreateUIAvatarWithIKController(AccountDataManager.Inst.UserInfo.otherAvatarInfo, characterRoot);
        otherAnimationCtrl = otherCharacterWrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
        otherAnimationCtrlIK = otherCharacterWrap.Avatar.GetComponent<AnimIKController>();
        avatarWrapper = characterWrapper;
        otherCharacterWrap.Avatar.gameObject.SetActive(true);
        saveAvatarData = saveCharacterData;
    }
    private void LoadVehicle()
    {
        int pgcId = 160200002;   //160200002
        var vehicleConfig = DataTables.GetPgcVehicleConfig(pgcId);
        PGCVehicleManager.Inst.CreateUIPGCVehicle(pgcId, characterRoot, new PlayerAnimationCtrl[] { animationCtrl, otherAnimationCtrl }, (vehicleController) =>
        {
            characterRoot.localEulerAngles = new Vector3(0, -150, 0);
            avatarCameraController.roleCamera.transform.localPosition = new Vector3(0, 0.6f, -15);
            avatarCameraController.roleCamera.orthographicSize = 3;
            // avatarCameraController.SetVehicleViewByConfig(vehicleConfig);
        });
    }
    override public void OnHide()
    {
        base.OnHide();
    }

    private void InitData(int type = 0)
    {
        m_CurSelectType = type;
        gashaponData = GashaponDataManager.Inst.gashaponData(GetGashaponIdByType());
        ponyGashaponStarLayout.SetStarCount(gashaponInfoRsp?.luckyProgressInfo?.round ?? 0);
    }

    private string GetGashaponIdByType()
    {
        string typeId = "lottery.ponyVehicle";
        return typeId;
    }



    protected override void OnGashaponInfoUpdate(GashaponInfoRsp infoRsp)
    {
        base.OnGashaponInfoUpdate(infoRsp);
        // Debug.LogError("infoRsp: " + JsonConvert.SerializeObject(infoRsp));
        if (infoRsp == null)
        {
            return;
        }
        UpdateWidget();
        gashaponInfoRsp = infoRsp;
        if (isRotating)
        {
            return;
        }
        OnceBeforePrice.text = infoRsp.singleDrawPrice.ToString();
        OncePrice.text = infoRsp.singleDrawDiscountedPrice.ToString();
        OnceBeforePrice.gameObject.SetActive(infoRsp.singleDrawDiscountedPrice != infoRsp.singleDrawPrice);
        TenBeforePrice.text = infoRsp.tenDrawPrice.ToString();
        TenPrice.text = infoRsp.tenDrawDiscountedPrice.ToString();
        TenBeforePrice.gameObject.SetActive(infoRsp.singleDrawDiscountedPrice != infoRsp.singleDrawPrice);
        float discount = infoRsp.singleDrawDiscountedPrice / (float)infoRsp.singleDrawPrice;
        TenDiscount.gameObject.SetActive(discount != 1);
        OnceDiscount.gameObject.SetActive(discount != 1);
        OnceDiscountText.text = string.Format("限时{0}折", discount * 10);
        TenDiscountText.text = string.Format("限时{0}折", discount * 10);

        if (infoRsp.singleDrawPrice == 0 && infoRsp.singleDrawDiscountedPrice == 0)
        {
            tipsText.text = "恭喜！你已集齐当前奖池所有商品！";
            TwistBtn.gameObject.SetActive(false);
            Twist10Btn.gameObject.SetActive(false);
        }
        else
        {
            tipsText.text = string.Format("3次为1轮，再抽取{0}次必点亮爱心", 3 - (infoRsp.luckyProgressInfo?.roundIndex ?? 0));
            TwistBtn.gameObject.SetActive(true);
            Twist10Btn.gameObject.SetActive(true);
        }
        ponyGashaponStarLayout.SetStarCount(infoRsp.luckyProgressInfo?.round ?? 0);
    }

    public override void OnGashaOnceRsp(GashaponRsp gashaponRsp)
    {
        // base.OnGashaOnceRsp(gashaponRsp);
        GashaponDataManager.Inst.RequestGashaponInfo(gashaponId, OnGashaponInfoUpdate);
        // var panel = UIManager.Inst.OpenPanel<GashaponTwistAnimPanel>(PanelId.GashaponTwistAnimPanel,
        // new GashaponTwistAnimParam()
        // {
        //     gashaponId = gashaponData.Id
        // });
        gashaponInfoRsp = gashaponRsp.lotteryInfo;
        int oldRound = gachaRound;
        int newRound = gashaponRsp.lotteryInfo.luckyProgressInfo?.round ?? 0;
        bool hitPrize = oldRound != newRound;

        // Debug.LogError("hitPrize: " + hitPrize + " oldRound: " + oldRound + " newRound: " + newRound);
        rotateMaskGo.SetActive(true);
        ponyGashaponTurntable.StartRotation(hitPrize, (hitPrize) =>
        {
            rotateMaskGo.SetActive(false);
            isRotating = false;
            OnGashaponInfoUpdate(gashaponInfoRsp);
            if (hitPrize)
            {
                //点亮星星
                var newRound = gashaponRsp.lotteryInfo.luckyProgressInfo.round; //0:说明新转盘
                //惊喜大奖
                bool hadBigReward = oldRound > newRound && ContainsBigReward(gashaponRsp);//是否获得惊喜大奖
                var panel = UIManager.Inst.OpenPanel<PonyGashaponAnimPanel>(PanelId.PonyGashaponAnimPanel);
                panel.lightUpStar();
                panel.ShowLove();
                panel.onBackCallBack = () =>
                {
                    var bigReward = FliterBigReward(ref gashaponRsp);
                    OnGashaTwistAnimComplete(gashaponRsp, () =>
                    {
                        if (hadBigReward)
                        {
                            //显示惊喜大奖
                            //再显示新转盘
                            var panel = UIManager.Inst.OpenPanel<PonyGashaponAnimPanel>(PanelId.PonyGashaponAnimPanel);
                            panel.showBigReward(bigReward);
                        }
                    });

                };
            }
            else
            {
                OnGashaTwistAnimComplete(gashaponRsp);
            }
            if (gashaponRsp.gachaTimes != 0 && gashaponRsp.gachaReturn != null)
            {
                TipPanel.ShowToast(string.Format("已抽取{0}次，返还{1}紫梦币", gashaponRsp.gachaTimes, gashaponRsp.gachaReturn.amount));
                var panel = UIManager.Inst.FindPanel(WindowId.CommonWindow, PanelId.TipPanel);
                if (panel != null)
                {
                    var canvas = panel.gameObject.AddComponent<Canvas>();
                    canvas.overrideSorting = true;
                    canvas.sortingOrder = 1000;
                }
            }
            //先恭喜点亮 再弹通用奖励
        });
    }

    bool ContainsBigReward(GashaponRsp gashaponRsp)
    {
        if (gashaponRsp == null || gashaponRsp.rewardList == null)
        {
            return false;
        }
        List<int> pgcIdList = new List<int> { 160200002, 160100002, 33400001 };
        foreach (var reward in gashaponRsp.rewardList)
        {
            if (pgcIdList.Contains(int.Parse(reward.pgcId)))
            {
                return true;
            }
            if (reward.rewardType == (int)BUDRewardType.RewardCrystal && reward.amount >= 5) //5个炫彩水晶
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 过滤惊喜大奖
    /// </summary>
    /// <param name="gashaponRsp"></param>
    /// <returns></returns>
    RewardInfo FliterBigReward(ref GashaponRsp gashaponRsp)
    {
        if (gashaponRsp == null || gashaponRsp.rewardList == null)
        {
            return null;
        }
        List<int> pgcIdList = new List<int> { 160200002, 160100002, 33400001 };
        int index = -1;
        for (int i = 0; i < gashaponRsp.rewardList.Count; i++)
        {
            var reward = gashaponRsp.rewardList[i];
            if (pgcIdList.Contains(int.Parse(reward.pgcId)))
            {
                index = i;
                break;
            }
            if (reward.rewardType == (int)BUDRewardType.RewardCrystal && reward.amount >= 5) //5个炫彩水晶
            {
                index = i;
                break;
            }
        }
        RewardInfo rewardInfo = null;
        if (index != -1)
        {
            rewardInfo = gashaponRsp.rewardList[index];
            gashaponRsp.rewardList.RemoveAt(index);
        }
        return rewardInfo;
    }



    private void OnGashaTwistAnimComplete(GashaponRsp gashaponRsp, Action callback = null)
    {
        UIManager.Inst.ClosePanel(PanelId.GashaponTwistAnimPanel);
        if (gashaponRsp == null || gashaponRsp.rewardList == null)
        {
            return;
        }

        if (gashaponRsp.rewardList.Count == 0)
        {
            callback?.Invoke();
            return;
        }

        var panel = UIManager.Inst.OpenPanel<GashaponRewardPanel>(PanelId.GashaponRewardPanel);
        panel.ShowRewards(gashaponId, gashaponRsp);
        panel.onBackCallBack = callback;
        if (UIManager.Inst.TryFindPanel<CreatorRewardPanel>(WindowId.CreatorWindow, PanelId.CreatorCenterPanel,
                out var creatorRewardPanel) && gashaponData.CurrencyType == CurrencyType.GreenCoin)
        {
            creatorRewardPanel.Refresh();
        }
    }

    private void GetSeverRefresh()
    {
        string typeId = GetGashaponIdByType();
        if (string.IsNullOrEmpty(typeId))
        {
            return;
        }
        GashaponDataManager.Inst.RequestGashaponInfo(typeId, OnGashaponInfoUpdate);
    }
    private void OnPriviewBtnClick()
    {
        if (gashaponData == null)
        {
            Debug.LogError("gashaponData is null!");
            return;
        }

        int starCount = gashaponInfoRsp?.luckyProgressInfo?.round ?? 0;
        // starCount = 7;//test   2
        List<GashaponPonyPreviewItemData> gashaponPonyPreviewItemDataList = new List<GashaponPonyPreviewItemData>(){
            new GashaponPonyPreviewItemData() {
                rewardList = GashaponDataManager.Inst.gashaponData("lottery.ponyVehicle_7").RewardList,
                title = "惊喜大奖"
            },
            new GashaponPonyPreviewItemData() {
                rewardList = GashaponDataManager.Inst.gashaponData("lottery.ponyVehicle_6").RewardList,
                title = "奖池 (爱心x6)"
            },
            new GashaponPonyPreviewItemData() {
                rewardList = GashaponDataManager.Inst.gashaponData("lottery.ponyVehicle_5").RewardList,
                title = "奖池 (爱心x5)"
            },
            new GashaponPonyPreviewItemData() {
                rewardList = GashaponDataManager.Inst.gashaponData("lottery.ponyVehicle_4").RewardList,
                title = "奖池 (爱心x4)"
            },
            new GashaponPonyPreviewItemData() {
                rewardList = GashaponDataManager.Inst.gashaponData("lottery.ponyVehicle_3").RewardList,
                title = "奖池 (爱心x3)"
            },
            new GashaponPonyPreviewItemData() {
                rewardList = GashaponDataManager.Inst.gashaponData("lottery.ponyVehicle_2").RewardList,
                title = "奖池 (爱心x2)"
            },
            new GashaponPonyPreviewItemData() {
                rewardList = GashaponDataManager.Inst.gashaponData("lottery.ponyVehicle_1").RewardList,
                title = "奖池 (爱心x1)"
            },
            new GashaponPonyPreviewItemData() {
                rewardList = GashaponDataManager.Inst.gashaponData("lottery.ponyVehicle_0").RewardList,
                title = "基础奖池"
            },
        };
        // var gashaponData1 = GashaponDataManager.Inst.gashaponData("lottery.ponyVehicle_" + starCount);
        // Debug.LogError("gashaponData1: " + gashaponData1.RewardList[0]);
        // gashaponData.RewardList = gashaponData1.RewardList;

        var viewCfg = GashaponDataManager.Inst.GetGashaponView(GetGashaponIdByType());
        var previewPanel = UIManager.Inst.OpenPanel<GashaponPreviewPanel>(PanelId.GashaponPreviewPanel, new GashaponPreviewParam
        {
            bgPath = viewCfg.BgPath,
            title = GetGashaponNameByType(),
            gashaponData = gashaponData,
            rewardCurrency = CurrencyType.PurpleDreamCoin,
            rulePath = viewCfg.RulePath,
            onBackCallBack = ShowCloseInfo,
            isPonyPreview = true,
            gashaponPonyPreviewItemDataList = gashaponPonyPreviewItemDataList
        });
        if (previewPanel != null)
            previewPanel.SetBundleViewBgClolr(bundleViewBgColor);

    }

    private void ShowCloseInfo()
    {
        LoadVehicle();
    }

    private string GetGashaponNameByType()
    {
        string name = "铃阙巡礼";
        return name;

    }

    void UpdateWidget()
    {
        // var pdcId = BagDatabase.Inst.Select("33400001"); //水晶
        // if ((pdcId != null && pdcId.OwnedNum > 0))
        // {
        //     shuijingText.text = pdcId.OwnedNum.ToString();
        // }
        // else
        // {
        //     shuijingText.text = "0";
        // }

        // pdcId = BagDatabase.Inst.Select("33500001"); //碎片
        // if ((pdcId != null && pdcId.OwnedNum > 0))
        // {
        //     suipianText.text = pdcId.OwnedNum.ToString();
        // }
        // else
        // {
        //     suipianText.text = "0";
        // }
    }

    private void OnSeasonBtnClick()
    {
        var viewCfg = GashaponDataManager.Inst.GetGashaponView(gashaponId);
        var viewPath = GashaponUtils.ViewBasePath + "PonyGashapon/preview_bg.png";
        var exchangePanel = UIManager.Inst.OpenPanel<GashaponExchangePanel>(PanelId.GashaponExchangePanel, new GashaponExchangeParam
        {
            bgPath = viewPath,
            title = "兑换商店",
            gashaponData = gashaponData,
            // rewardCurrency = exchangeCurrency == CurrencyType.None ? gashaponData.CurrencyType : exchangeCurrency,
            rewardCurrency = CurrencyType.CrystalShards,
            rewardCurrency1 = CurrencyType.Crystal,
            itemBgColor = bundleViewBgColor,
            clearTips = "",
            buyTips = ""
        });
        exchangePanel.SetAnimPreviewBtnColor(new Color32(210, 73, 73, 255), new Color32(255, 192, 31, 255), new Color32(255, 86, 96, 255));
        exchangePanel.SetBundleViewBgColor(bundleViewBgColor);
        exchangePanel.onCloseCallback = () =>
        {
            LoadVehicle();
        };
    }

    private void OnTwistClick()
    {
        if (!GlobalFuncExtensions.CheckCanClick())
        {
            return;
        }
        if (gashaponInfoRsp == null)
        {
            return;
        }
        int num = AccountDataManager.Inst.BalanceInfo.GetAccountCount(CurrencyType.PurpleDreamCoin);
        if (gashaponInfoRsp.singleDrawDiscountedPrice > num)
        {
            GashaponDataManager.Inst.ShowCurrencyNoEnough((int)CurrencyType.PurpleDreamCoin, gashaponInfoRsp.singleDrawDiscountedPrice);
            return;
        }

        if (!GashaponUtils.CurrencyIsEnough((int)CurrencyType.PurpleDreamCoin, gashaponInfoRsp.singleDrawDiscountedPrice))
        {
            var val = gashaponInfoRsp.singleDrawDiscountedPrice;
            GashaponDataManager.Inst.ShowCurrencyNoEnough((int)CurrencyType.PurpleDreamCoin, val);
            return;
        }

        var gId = GetGashaponIdByType();
        gachaRound = gashaponInfoRsp.luckyProgressInfo?.round ?? 0;
        isRotating = true;
        GashaponDataManager.Inst.RequestGashapon(gId, OneTimeGasha, OnGashaOnceRsp);
    }

    private void OnTenTwistClick()
    {
        if (!GlobalFuncExtensions.CheckCanClick())
        {
            return;
        }
        if (gashaponInfoRsp == null)
        {
            return;
        }
        //判断当前喵币是否大于十抽金额
        int num = AccountDataManager.Inst.BalanceInfo.GetAccountCount(CurrencyType.PurpleDreamCoin);
        if (gashaponInfoRsp.tenDrawDiscountedPrice > num)
        {
            int lessNum = gashaponInfoRsp.tenDrawDiscountedPrice - num;
            GashaponDataManager.Inst.ShowCurrencyNoEnough((int)CurrencyType.PurpleDreamCoin, gashaponInfoRsp.tenDrawDiscountedPrice);
            return;
        }

        if (!GashaponUtils.CurrencyIsEnough((int)CurrencyType.PurpleDreamCoin, gashaponInfoRsp.singleDrawPrice))
        {
            var val = gashaponInfoRsp.singleDrawPrice;
            GashaponDataManager.Inst.ShowCurrencyNoEnough((int)CurrencyType.PurpleDreamCoin, val);
            return;
        }

        var gId = GetGashaponIdByType();
        gachaRound = gashaponInfoRsp.luckyProgressInfo?.round ?? 0;
        isRotating = true;
        GashaponDataManager.Inst.RequestGashapon(gId, 3, OnGashaOnceRsp);
    }

    private void OnInfoClick()
    {
        var rulePath = "Assets/Loadable/UI/UIPanel/PonyGashaponPanel/Rule.json";

        UIManager.Inst.OpenPanel<GashaponRulePanel>(PanelId.GashaponRulePanel, rulePath);
    }

    [Button("测试旋转")]
    private void TestTwist()
    {
        ponyGashaponTurntable.TestNotHitPrize();
    }

    [Button("测试提前大奖")]
    private void TestGachaReturnTwist()
    {
        ponyGashaponTurntable.TestHitPrize();
    }
}

