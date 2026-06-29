using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BUD.AnimPose;
using Es;
using Game.Avatar;
using Game.Database;
using Game.Vehicle.PGCVehicle;
using GameData.Gashapon;
using GameData.PgcData;
using GameData.Rewards;
using UI.BaseWidgets;
using UI.Manager;
using UI.UIPanels.CreaterRewardPanel;
using UI.UIPanels.GashaponPanel;
using Newtonsoft.Json;
using Game.Store;

public class AirVehicleGashaponPanel : BaseGashaponView
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
    [SerializeField] private CButton PreVideoBtn;
    [SerializeField] private CButton SeasonBtn;
    [SerializeField] private CButton RewardShowBtn;
    [SerializeField] private Text tipsText;
    [SerializeField] private GameObject rotateMaskGo;

    [SerializeField] private PonyGashaponStarLayout ponyGashaponStarLayout;
    [SerializeField] private PonyGashaponTurntable ponyGashaponTurntable;
    [SerializeField] internal Transform characterRoot;
    [SerializeField] internal AvatarCameraController avatarCameraController;

    private int m_CurSelectType;
    private string bundleViewBgColor = "#FF9B9B";

    internal PlayerAnimationCtrl animationCtrl;
    internal AnimIKController animationCtrlIK;
    internal PlayerHoldBehaviour playerHold;
    internal CharacterWrap otherCharacterWrap;
    internal BaseAvatarWrapper avatarWrapper;
    internal BaseAvatarData saveAvatarData;
    internal PlayerAnimationCtrl otherAnimationCtrl;
    internal AnimIKController otherAnimationCtrlIK;

    private bool isRotating;
    private int gachaRound = -1;

    protected CurrencyType exchangeCurrency = CurrencyType.PurpleDreamCoin;

    private const string videoPath = "Assets/Loadable/Demand3D/ResVideo/air.mp4";

    public override void OnCreate(string id)
    {
        base.OnCreate(id);

        PreviewBtn?.onClick.AddListener(OnPriviewBtnClick);
        PreVideoBtn?.onClick.AddListener(OnPriviewVideoBtnClick);
        SeasonBtn?.onClick.AddListener(OnSeasonBtnClick);
        InfoBtn?.onClick.AddListener(OnInfoClick);
        TwistBtn?.onClick.AddListener(OnTwistClick);
        Twist10Btn?.onClick.AddListener(OnTenTwistClick);
        RewardShowBtn?.onClick.AddListener(OnPriviewBtnClick);

        if (rotateMaskGo != null) rotateMaskGo.SetActive(false);

        InitData();
        InitCharacterWrapper();
    }

    private void OnPriviewVideoBtnClick()
    {
        UIManager.Inst.OpenPanel<VideoPreviewPanel>(PanelId.VideoPreviewPanel, videoPath);
    }

    public override void OnShow()
    {
        base.OnShow();

        GetSeverRefresh();
        UpdateWidget();
        LoadVehicle();
    }

    public override void OnHide()
    {
        base.OnHide();
    }

    private void InitData(int type = 0)
    {
        m_CurSelectType = type;
        var baseId = GetBaseGashaponId();
        if (!string.IsNullOrEmpty(baseId))
        {
            gashaponData = GashaponDataManager.Inst.gashaponData(baseId);
        }
        ponyGashaponStarLayout?.SetStarCount(gashaponInfoRsp?.luckyProgressInfo?.round ?? 0);
    }

    private string GetBaseGashaponId()
    {
        // AirVehicle 扭蛋和 Pony 扭蛋逻辑一致，但不应硬编码具体 lotteryId
        return gashaponId;
    }

    private void InitCharacterWrapper()
    {
        if (characterRoot == null || avatarCameraController == null)
        {
            return;
        }

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

    private int TryGetPreviewVehiclePgcId()
    {
        // 尽量从奖池里找一个“载具”PGC 作为展示，避免硬编码 Pony 的 pgcId
        var data = gashaponData;
        if (data?.RewardList == null) return 0;

        for (int i = 0; i < data.RewardList.Count; i++)
        {
            var reward = data.RewardList[i];
            if (reward == null) continue;
            if (reward.RewardType != Product.RewardType.RewardPgcResource) continue;

            var pgcId = reward.Id;
            if (string.IsNullOrEmpty(pgcId)) continue;

            var resCfg = Es.DataTables.GetGameResData(pgcId);
            if (resCfg != null && resCfg.ResourceType == (int)ResourceType.Vehicle)
            {
                if (int.TryParse(pgcId, out var intId))
                {
                    return intId;
                }
            }
        }

        return 0;
    }

    private void LoadVehicle()
    {
        if (characterRoot == null || avatarCameraController == null)
        {
            return;
        }

        var pgcId = TryGetPreviewVehiclePgcId();
        if (pgcId <= 0)
        {
            // 兜底：沿用 Pony 的展示载具，确保不空场
            pgcId = 160200004;
        }

        PGCVehicleManager.Inst.CreateUIPGCVehicle(pgcId, characterRoot, new PlayerAnimationCtrl[] { animationCtrl, otherAnimationCtrl }, (vehicleController) =>
        {
            characterRoot.localEulerAngles = new Vector3(0, -150, 0);
            avatarCameraController.roleCamera.transform.localPosition = new Vector3(0, 0.6f, -15);
            avatarCameraController.roleCamera.orthographicSize = 5;
        });
    }

    protected override void OnGashaponInfoUpdate(GashaponInfoRsp infoRsp)
    {
        base.OnGashaponInfoUpdate(infoRsp);
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

        if (OnceBeforePrice != null) OnceBeforePrice.text = infoRsp.singleDrawPrice.ToString();
        if (OncePrice != null) OncePrice.text = infoRsp.singleDrawDiscountedPrice.ToString();
        if (OnceBeforePrice != null) OnceBeforePrice.gameObject.SetActive(infoRsp.singleDrawDiscountedPrice != infoRsp.singleDrawPrice);
        if (TenBeforePrice != null) TenBeforePrice.text = infoRsp.tenDrawPrice.ToString();
        if (TenPrice != null) TenPrice.text = infoRsp.tenDrawDiscountedPrice.ToString();
        if (TenBeforePrice != null) TenBeforePrice.gameObject.SetActive(infoRsp.singleDrawDiscountedPrice != infoRsp.singleDrawPrice);

        float discount = infoRsp.singleDrawPrice <= 0 ? 1f : infoRsp.singleDrawDiscountedPrice / (float)infoRsp.singleDrawPrice;
        if (TenDiscount != null) TenDiscount.gameObject.SetActive(discount != 1);
        if (OnceDiscount != null) OnceDiscount.gameObject.SetActive(discount != 1);
        if (OnceDiscountText != null) OnceDiscountText.text = string.Format("限时{0}折", discount * 10);
        if (TenDiscountText != null) TenDiscountText.text = string.Format("限时{0}折", discount * 10);

        if (infoRsp.singleDrawPrice == 0 && infoRsp.singleDrawDiscountedPrice == 0)
        {
            if (tipsText != null) tipsText.text = "恭喜！你已集齐当前奖池所有商品！";
            if (TwistBtn != null) TwistBtn.gameObject.SetActive(false);
            if (Twist10Btn != null) Twist10Btn.gameObject.SetActive(false);
        }
        else
        {
            if (tipsText != null) tipsText.text = string.Format("3次为1轮，再抽取{0}次必点亮星星", 3 - (infoRsp.luckyProgressInfo?.roundIndex ?? 0));
            if (TwistBtn != null) TwistBtn.gameObject.SetActive(true);
            if (Twist10Btn != null) Twist10Btn.gameObject.SetActive(true);
        }

        ponyGashaponStarLayout?.SetStarCount(infoRsp.luckyProgressInfo?.round ?? 0);
    }

    public override void OnGashaOnceRsp(GashaponRsp gashaponRsp)
    {
        // AirVehicle 的奖励展示与 Pony 不同：这里保留转盘旋转/遮罩，但不弹 Pony 专属点亮/大奖动画
        GashaponDataManager.Inst.RequestGashaponInfo(GetBaseGashaponId(), OnGashaponInfoUpdate);
        gashaponInfoRsp = gashaponRsp.lotteryInfo;

        int oldRound = gachaRound;
        int newRound = gashaponRsp.lotteryInfo.luckyProgressInfo?.round ?? 0;
        bool hitPrize = oldRound != newRound;

        if (rotateMaskGo != null) rotateMaskGo.SetActive(true);
        ponyGashaponTurntable?.StartRotation(hitPrize, (isHit) =>
        {
            rotateMaskGo.SetActive(false);
            isRotating = false;
            OnGashaponInfoUpdate(gashaponInfoRsp);
            if (hitPrize)
            {
                //点亮星星
                var newRound = gashaponRsp.lotteryInfo.luckyProgressInfo.round; //0:说明新转盘
                string lightUpStarText = "+1";
                if (oldRound != newRound)
                {
                    if (newRound > oldRound)
                    {
                        lightUpStarText = "+" + (newRound - oldRound);
                    }
                    else
                    {
                        lightUpStarText = "+" + (newRound + 7 - oldRound);
                    }
                }
                //惊喜大奖
                bool hadBigReward = oldRound > newRound && ContainsBigReward(gashaponRsp);//是否获得惊喜大奖
                var panel = UIManager.Inst.OpenPanel<PonyGashaponAnimPanel>(PanelId.PonyGashaponAnimPanel);
                panel.lightUpStar();
                panel.setLightUpStarText(lightUpStarText);
                panel.ShowYinfu();
                panel.SetTypeImage(2);
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
        });
    }

    bool ContainsBigReward(GashaponRsp gashaponRsp)
    {
        if (gashaponRsp == null || gashaponRsp.rewardList == null)
        {
            return false;
        }
        List<int> pgcIdList = new List<int> { 160200004 };
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
        List<int> pgcIdList = new List<int> { 160200004 };
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
        panel.ShowRewards(GetBaseGashaponId(), gashaponRsp);
        panel.onBackCallBack = callback;

        if (UIManager.Inst.TryFindPanel<CreatorRewardPanel>(WindowId.CreatorWindow, PanelId.CreatorCenterPanel,
                out var creatorRewardPanel) && gashaponData != null && gashaponData.CurrencyType == CurrencyType.GreenCoin)
        {
            creatorRewardPanel.Refresh();
        }
    }

    private void GetSeverRefresh()
    {
        var baseId = GetBaseGashaponId();
        if (string.IsNullOrEmpty(baseId))
        {
            return;
        }
        GashaponDataManager.Inst.RequestGashaponInfo(baseId, OnGashaponInfoUpdate);
    }

    private void OnPriviewBtnClick()
    {
        if (gashaponData == null)
        {
            Debug.LogError("gashaponData is null!");
            return;
        }
        // 多奖池预览：默认按 <baseId>_7 ... <baseId>_0 的命名方式组织
        var baseId = GetBaseGashaponId();
        var gashaponPonyPreviewItemDataList = new List<GashaponPonyPreviewItemData>();
        if (!string.IsNullOrEmpty(baseId))
        {
            gashaponPonyPreviewItemDataList.Add(new GashaponPonyPreviewItemData()
            {
                rewardList = GashaponDataManager.Inst.gashaponData(baseId + "_7")?.RewardList,
                title = "惊喜大奖"
            });

            for (int i = 6; i >= 1; i--)
            {
                gashaponPonyPreviewItemDataList.Add(new GashaponPonyPreviewItemData()
                {
                    rewardList = GashaponDataManager.Inst.gashaponData(baseId + "_" + i)?.RewardList,
                    title = $"奖池 (繁星x{i})"
                });
            }

            gashaponPonyPreviewItemDataList.Add(new GashaponPonyPreviewItemData()
            {
                rewardList = GashaponDataManager.Inst.gashaponData(baseId + "_0")?.RewardList,
                title = "基础奖池"
            });
        }

        var viewCfg = GashaponDataManager.Inst.GetGashaponView(baseId);
        var previewPanel = UIManager.Inst.OpenPanel<GashaponPreviewPanel>(PanelId.GashaponPreviewPanel, new GashaponPreviewParam
        {
            bgPath = viewCfg?.BgPath,
            title = GetGashaponName(),
            gashaponData = gashaponData,
            rewardCurrency = CurrencyType.PurpleDreamCoin,
            rulePath = viewCfg?.RulePath,
            onBackCallBack = ShowCloseInfo,
            isPonyPreview = true,
            gashaponPonyPreviewItemDataList = gashaponPonyPreviewItemDataList
        });

        previewPanel?.SetBundleViewBgClolr(bundleViewBgColor);
    }

    private void ShowCloseInfo()
    {
        LoadVehicle();
    }

    private string GetGashaponName()
    {
        var viewCfg = GashaponDataManager.Inst.GetGashaponView(GetBaseGashaponId());
        return viewCfg != null ? viewCfg.GashaName : string.Empty;
    }

    private void UpdateWidget()
    {
        // 预留：币种等展示（与 Pony 保持一致）
    }

    private void OnSeasonBtnClick()
    {
        var exchangeGroupList = new List<GashaponPonyExchangeItemData>();
        var idOrder = new List<string> { "160200004", "95_1", "190000026", "10500080", "180100005", "40400455" };
        gashaponData.ExchangeList.Sort((a, b) =>
        {
            int indexA = idOrder.IndexOf(a.Id);
            int indexB = idOrder.IndexOf(b.Id);
            if (indexA < 0) indexA = int.MaxValue;
            if (indexB < 0) indexB = int.MaxValue;
            return indexA.CompareTo(indexB);
        });
        exchangeGroupList.Add(new GashaponPonyExchangeItemData
        {
            exchangeList = gashaponData.ExchangeList,
            title = "云凝甜筒号"
        });
        var ponyExchangeList = dataHandler.GetGashaponData("lottery.ponyVehicle").ExchangeList;
        if(ponyExchangeList != null && ponyExchangeList.Count > 0)
        {
            // lottery.ponyVehicle 兑换列表中多出了水晶项，需隐藏
            ponyExchangeList = ponyExchangeList.FindAll(e => (int)e.RewardType != (int)BUDRewardType.RewardCrystal);
            exchangeGroupList.Add(new GashaponPonyExchangeItemData
            {
                exchangeList = ponyExchangeList,
                title = "铃阙巡礼"
            });
        }

        var viewCfg = GashaponDataManager.Inst.GetGashaponView(GetBaseGashaponId());
        var bgPath = viewCfg != null ? viewCfg.BgPath : null;
        var exchangePanel = UIManager.Inst.OpenPanel<GashaponExchangePanel>(PanelId.GashaponExchangePanel, new GashaponExchangeParam
        {
            bgPath = bgPath,
            title = GetGashaponName(),
            gashaponData = gashaponData,
            rewardCurrency = CurrencyType.CrystalShards,
            rewardCurrency1 = CurrencyType.Crystal,
            itemBgColor = bundleViewBgColor,
            clearTips = "",
            buyTips = "",
            isSpecialRoot = true,
            exchangeGroupList = exchangeGroupList,
        });

        exchangePanel.SetAnimPreviewBtnColor(new Color32(210, 73, 73, 255), new Color32(255, 192, 31, 255), new Color32(255, 86, 96, 255));
        exchangePanel.SetBundleViewBgColor(bundleViewBgColor);
        exchangePanel.onCloseCallback = () => { LoadVehicle(); };
    }

    private List<GashaponExchangeData> GetResListData(string id)
    {
        var baseId = GetBaseGashaponId();
        var list = GashaponDataManager.Inst.gashaponData(baseId + id)?.RewardList;
        List<GashaponExchangeData> datas = new List<GashaponExchangeData>();
        foreach(var item in list)
        {
            for(int i = 0; i < gashaponData.ExchangeList.Count; i++)
            {
                if(item.Id == gashaponData.ExchangeList[i].Id
                   || gashaponData.ExchangeList[i].Id == "100_1"
                   || gashaponData.ExchangeList[i].Id == "102_1")
                {
                    if(!datas.Contains(gashaponData.ExchangeList[i]))
                        datas.Add(gashaponData.ExchangeList[i]);
                    break;
                }
            }
        }
        return datas;
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

        var gId = GetBaseGashaponId();
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

        int num = AccountDataManager.Inst.BalanceInfo.GetAccountCount(CurrencyType.PurpleDreamCoin);
        if (gashaponInfoRsp.tenDrawDiscountedPrice > num)
        {
            GashaponDataManager.Inst.ShowCurrencyNoEnough((int)CurrencyType.PurpleDreamCoin, gashaponInfoRsp.tenDrawDiscountedPrice);
            return;
        }

        if (!GashaponUtils.CurrencyIsEnough((int)CurrencyType.PurpleDreamCoin, gashaponInfoRsp.singleDrawPrice))
        {
            var val = gashaponInfoRsp.singleDrawPrice;
            GashaponDataManager.Inst.ShowCurrencyNoEnough((int)CurrencyType.PurpleDreamCoin, val);
            return;
        }

        var gId = GetBaseGashaponId();
        gachaRound = gashaponInfoRsp.luckyProgressInfo?.round ?? 0;
        isRotating = true;
        // 与 Pony 保持一致：该扭蛋十连按钮实际请求 3 次（3次为1轮）
        GashaponDataManager.Inst.RequestGashapon(gId, 3, OnGashaOnceRsp);
    }

    private void OnInfoClick()
    {
        var viewCfg = GashaponDataManager.Inst.GetGashaponView(GetBaseGashaponId());
        var rulePath = viewCfg != null ? viewCfg.RulePath : null;
        if (string.IsNullOrEmpty(rulePath + GetBaseGashaponId()))
        {
            return;
        }
        UIManager.Inst.OpenPanel<GashaponRulePanel>(PanelId.GashaponRulePanel, rulePath);
    }
}
