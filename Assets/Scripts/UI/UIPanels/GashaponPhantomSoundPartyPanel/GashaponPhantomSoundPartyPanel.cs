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
using UI.UIPanels.RechargePanel;
using Newtonsoft.Json;
using Game.Store;
using Product;
public class AirVehicleGashaponPanelInfo
{
    public string name;
    public string pgcId;
    public string iconPath;
}
public class GashaponPhantomSoundPartyPanel : BaseGashaponView
{
    List<AirVehicleGashaponPanelInfo> panelInfoList = new List<AirVehicleGashaponPanelInfo>()
    {
        new AirVehicleGashaponPanelInfo()
        {
            name = "霓虹歌台",
            pgcId = "160100009",
        },
        new AirVehicleGashaponPanelInfo()
        {
            name = "浮光印象",
            pgcId = "160100010",
        },
        new AirVehicleGashaponPanelInfo()
        {
            name = "节拍回廊",
            pgcId = "160100011",
        }
    };

    public static string GetVehicleName(int pgcId)
    {
        switch (pgcId)
        {
            case 160100009: return "霓虹歌台";
            case 160100010: return "浮光印象";
            case 160100011: return "节拍回廊";
            default: return null;
        }
    }

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
    [SerializeField] private CButton GiftBtn;

    [SerializeField] private CButton RewardShowBtn;
    [SerializeField] private Text tipsText;
    [SerializeField] private GameObject rotateMaskGo;
    // 兑换入口(SeasonBtn/ExchangeBtn)上的红点：拥有 panelInfoList 任一载具(打折态)时显示
    [SerializeField] private GameObject exchangeRedDot;

    [SerializeField] private PonyGashaponStarLayout ponyGashaponStarLayout;
    [SerializeField] private PonyGashaponTurntable ponyGashaponTurntable;
    [SerializeField] internal Transform characterRoot;
    [SerializeField] internal AvatarCameraController avatarCameraController;

    [SerializeField] internal Button btn_left;
    [SerializeField] internal Button btn_right;

    [SerializeField] private List<SuitRewardItem> RewardItemList;
    [SerializeField] private CButton BigRewardBtn;

    private List<int> _vehiclePgcIds = new List<int>();
    private int _vehicleIndex;

    private int m_CurSelectType;
    private string bundleViewBgColor = "#FF9B9B";

    internal PlayerAnimationCtrl animationCtrl;
    internal AnimIKController animationCtrlIK;
    internal PlayerHoldBehaviour playerHold;
    internal BaseAvatarWrapper avatarWrapper;
    internal BaseAvatarData saveAvatarData;

    private bool isRotating;
    private bool _isJustDrew;
    private int gachaRound = -1;
    private bool _isLoadingVehicle;

    protected CurrencyType exchangeCurrency = CurrencyType.ZZZCoin;

    private const string videoPath = "Assets/Loadable/Demand3D/ResVideo/phantomSound/phantomSound.mp4";

    // 兑换商店 SpecialItem 背景色按等级取色：服务端 ExchangeData 不下发 Level，按 Id 写死。
    // Level 取值对应 GashaponPriceItem.levelColor 的 key：1=FFA95A 2=9F72FF 3=92BEFF 4=FF785A 5=7BED72；
    // 填 0(或不在表内) 则命中默认色 levelColor[3]。TODO: 待策划确认各 item 等级后替换 0。
    private static readonly Dictionary<string, int> _exchangeItemLevelMap = new Dictionary<string, int>()
    {
        { "160100009", 1 }, // 霓虹歌台
        { "160100010", 1 }, // 浮光印象
        { "160100011", 1 }, // 节拍回廊
        { "34700001", 1 },
        { "10500085", 1 },
        { "190000033", 1 },
        { "180100010", 1 },
        { "40200568", 1 },
        { "11400092", 1 },
        { "40100578", 2 },
        { "40200566", 2 },
        { "40200565", 2 },
        { "40200567", 2 },
        { "40200560", 3 },
        { "40200561", 3 },
        { "40200562", 3 },
        { "40200563", 3 },
        { "31600001", 3 },
    };

    public override void OnCreate(string id)
    {
        base.OnCreate(id);

        PreviewBtn?.onClick.AddListener(OnPriviewBtnClick);
        PreVideoBtn?.onClick.AddListener(OnPriviewVideoBtnClick);
        SeasonBtn?.onClick.AddListener(OnSeasonBtnClick);
        GiftBtn?.onClick.AddListener(OnGiftBtnClick);
        InfoBtn?.onClick.AddListener(OnInfoClick);
        TwistBtn?.onClick.AddListener(OnTwistClick);
        Twist10Btn?.onClick.AddListener(OnTenTwistClick);
        RewardShowBtn?.onClick.AddListener(OnPriviewBtnClick);
        btn_left?.onClick.AddListener(OnVehicleLeftClick);
        btn_right?.onClick.AddListener(OnVehicleRightClick);
        BigRewardBtn?.onClick.AddListener(OnBigRewardClick);

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

        // 先清理已有的角色与载具，保证 characterRoot 下始终只有一套模型。
        // 否则从 GashaponPreviewPanel / GashaponExchangePanel 返回后重建角色会叠加出多个 3D 模型。
        ClearCharacterWrapper();

        var saveCharacterData = AccountDataManager.Inst.UserInfo.avatarInfo;

        var characterWrapper = AvatarController.Inst.CreateUIAvatarWithIKController(saveCharacterData, characterRoot);
        animationCtrl = characterWrapper.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
        animationCtrlIK = characterWrapper.Avatar.GetComponent<AnimIKController>();
        playerHold = characterWrapper.Avatar.GetComponentInChildren<PlayerHoldBehaviour>();
        avatarCameraController.RotateTarget = characterRoot;
        avatarWrapper = characterWrapper;
        saveAvatarData = saveCharacterData;

        // 本扭蛋的载具（160100009/10/11）均为单人座，只展示主角一个角色。
        // 注意：不能用 AccountDataManager.Inst.UserInfo.otherAvatarInfo 判空来决定是否加载同伴——
        // 该属性在没有同伴时会回退成主角自己（avatarInfo），永远非 null，会导致重复创建出第二个角色。
    }

    /// <summary>
    /// 清理 characterRoot 下创建出来的角色模型与共享载具，避免重复 3D 模型残留。
    /// 按组件遍历销毁所有角色节点（含未被字段引用的残留节点），载具由 PGCVehicleManager 统一回收。
    /// </summary>
    private void ClearCharacterWrapper()
    {
        // 共享载具（FittingRoom 槽位）统一回收
        PGCVehicleManager.Inst.RemoveUIPGCVehicle();

        // 销毁 characterRoot 下所有角色模型节点（每个 AvatarNode 含一个 PlayerAnimationCtrl）
        if (characterRoot != null)
        {
            for (int i = characterRoot.childCount - 1; i >= 0; i--)
            {
                var child = characterRoot.GetChild(i);
                if (child != null && child.GetComponentInChildren<PlayerAnimationCtrl>(true) != null)
                {
                    GameObject.Destroy(child.gameObject);
                }
            }
        }

        animationCtrl = null;
        animationCtrlIK = null;
        playerHold = null;
        avatarWrapper = null;
    }

    private void CollectVehiclePgcIds()
    {
        // 本扭蛋固定展示 panelInfoList 中声明的 3 台载具（160100009/10/11）。
        // 不再从 ExchangeList 反查——若兑换列表里只命中了其中 2 台（或某台未被标记为 Vehicle），
        // 左右切换就只会在 2 台之间循环，丢失第 3 台。
        _vehiclePgcIds.Clear();
        foreach (var info in panelInfoList)
        {
            if (info == null || string.IsNullOrEmpty(info.pgcId)) continue;
            if (int.TryParse(info.pgcId, out var intId) && !_vehiclePgcIds.Contains(intId))
                _vehiclePgcIds.Add(intId);
        }
    }

    // 单人座载具，只让主角上座。
    private PlayerAnimationCtrl[] BuildRiderCtrls()
    {
        return new PlayerAnimationCtrl[] { animationCtrl };
    }

    private void LoadVehicle()
    {
        if (characterRoot == null || avatarCameraController == null) return;

        CollectVehiclePgcIds();
        _vehicleIndex = Mathf.Clamp(_vehicleIndex, 0, _vehiclePgcIds.Count - 1);
        RefreshArrowButtons();

        PGCVehicleManager.Inst.CreateUIPGCVehicle(_vehiclePgcIds[_vehicleIndex], characterRoot, BuildRiderCtrls(), (vehicleController) =>
        {
            characterRoot.localEulerAngles = new Vector3(0, -150, 0);
            avatarCameraController.roleCamera.transform.localPosition = new Vector3(0, 0.6f, -15);
            avatarCameraController.roleCamera.orthographicSize = 5;
        });
    }

    private void RefreshArrowButtons()
    {
        if (btn_left != null) btn_left.gameObject.SetActive(_vehiclePgcIds.Count > 1);
        if (btn_right != null) btn_right.gameObject.SetActive(_vehiclePgcIds.Count > 1);
    }

    private void OnVehicleLeftClick()
    {
        if (_vehiclePgcIds.Count <= 1 || _isLoadingVehicle) return;
        _vehicleIndex = (_vehicleIndex - 1 + _vehiclePgcIds.Count) % _vehiclePgcIds.Count;
        LoadVehicleByIndex();
    }

    private void OnVehicleRightClick()
    {
        if (_vehiclePgcIds.Count <= 1 || _isLoadingVehicle) return;
        _vehicleIndex = (_vehicleIndex + 1) % _vehiclePgcIds.Count;
        LoadVehicleByIndex();
    }

    private void LoadVehicleByIndex()
    {
        if (characterRoot == null || avatarCameraController == null) return;

        _isLoadingVehicle = true;
        if (btn_left != null) btn_left.interactable = false;
        if (btn_right != null) btn_right.interactable = false;

        PGCVehicleManager.Inst.CreateUIPGCVehicle(_vehiclePgcIds[_vehicleIndex], characterRoot, BuildRiderCtrls(), (vehicleController) =>
        {
            characterRoot.localEulerAngles = new Vector3(0, -150, 0);
            avatarCameraController.roleCamera.transform.localPosition = new Vector3(0, 0.6f, -15);
            avatarCameraController.roleCamera.orthographicSize = 5;
            _isLoadingVehicle = false;
            if (btn_left != null) btn_left.interactable = true;
            if (btn_right != null) btn_right.interactable = true;
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
            if (tipsText != null) tipsText.text = string.Format("3次为1轮，再抽取{0}次必点亮音符灯", 3 - (infoRsp.luckyProgressInfo?.roundIndex ?? 0));
            if (TwistBtn != null) TwistBtn.gameObject.SetActive(true);
            if (Twist10Btn != null) Twist10Btn.gameObject.SetActive(true);
        }

        ponyGashaponStarLayout?.SetStarCount(infoRsp.luckyProgressInfo?.round ?? 0);

        if (!_isJustDrew && infoRsp.isHavingPgcOptionalBox == 1)
        {
            var animPanel = UIManager.Inst.OpenPanel<PhantomSoundPartyGashaponAnimPanel>(PanelId.PhantomSoundPartyGashaponAnimPanel);
            if (animPanel != null)
            {
                animPanel.SetLotteryId(gashaponId);
                animPanel.ShowPlayBigRewardSelect();
            }
        }

        // RewardItemList：与 GashaponXiaXiaZaiPanel 逻辑一致，taskList 按下标逐项 SetData 刷新状态
        if (infoRsp.taskList != null)
        {
            for (int i = 0; i < RewardItemList.Count; i++)
            {
                if (infoRsp.taskList.Count <= i) return;
                RewardItemList[i].SetData(infoRsp.taskList[i]);
            }
        }
    }

    public override void OnGashaOnceRsp(GashaponRsp gashaponRsp)
    {
        _isJustDrew = true;
        if (gashaponRsp?.rewardList != null)
        {
            foreach (var r in gashaponRsp.rewardList)
                Debug.Log($"[幻音派对抽奖] name={r.rewardName} pgcId={r.pgcId} rewardType={r.rewardType} amount={r.amount} level={r.level}");
        }
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
                bool hadBigReward = ContainsBigReward(gashaponRsp);
                var panel = UIManager.Inst.OpenPanel<PhantomSoundPartyGashaponAnimPanel>(PanelId.PhantomSoundPartyGashaponAnimPanel);
                panel.lightUpStar();
                panel.setLightUpStarText(lightUpStarText);
                panel.ShowYinfu();
                panel.SetTypeImage(2);
                panel.onBackCallBack = () =>
                {
                    _isJustDrew = false;
                    var bigReward = FliterBigReward(ref gashaponRsp);
                    OnGashaTwistAnimComplete(gashaponRsp, () =>
                    {
                        if (hadBigReward)
                        {
                            //显示惊喜大奖
                            //再显示新转盘
                            var panel = UIManager.Inst.OpenPanel<PhantomSoundPartyGashaponAnimPanel>(PanelId.PhantomSoundPartyGashaponAnimPanel);
                            panel.SetLotteryId(gashaponId);
                            panel.showBigReward(bigReward);
                        }
                    });

                };

            }
            else
            {
                _isJustDrew = false;
                var bigReward = FliterBigReward(ref gashaponRsp);
                OnGashaTwistAnimComplete(gashaponRsp, () =>
                {
                    if (bigReward != null)
                    {
                        var panel = UIManager.Inst.OpenPanel<PhantomSoundPartyGashaponAnimPanel>(PanelId.PhantomSoundPartyGashaponAnimPanel);
                        panel.SetLotteryId(gashaponId);
                        panel.showBigReward(bigReward);
                    }
                });
            }
            if (gashaponRsp.gachaTimes != 0 && gashaponRsp.gachaReturn != null)
            {
                TipPanel.ShowToast(string.Format("已抽取{0}次，返还{1}绒币", gashaponRsp.gachaTimes, gashaponRsp.gachaReturn.amount));
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
        List<int> pgcIdList = new List<int> { 160100009, 160100010, 160100011 };
        foreach (var reward in gashaponRsp.rewardList)
        {
            if (reward.rewardType == (int)BUDRewardType.RewardPgcOptionalBox)
                return true;
            if (int.TryParse(reward.pgcId, out var pid) && pgcIdList.Contains(pid))
                return true;
            if (reward.rewardType == (int)BUDRewardType.RewardTypeZZZPhantomCrystal && reward.amount >= 5) //5个炫彩水晶
                return true;
        }
        return false;
    }

    /// <summary>
    /// 过滤惊喜大奖
    /// </summary>
    RewardInfo FliterBigReward(ref GashaponRsp gashaponRsp)
    {
        if (gashaponRsp == null || gashaponRsp.rewardList == null)
        {
            return null;
        }
        List<int> pgcIdList = new List<int> { 160100009, 160100010, 160100011 };
        int index = -1;
        for (int i = 0; i < gashaponRsp.rewardList.Count; i++)
        {
            var reward = gashaponRsp.rewardList[i];
            if (reward.rewardType == (int)BUDRewardType.RewardPgcOptionalBox)
            {
                index = i;
                break;
            }
            if (int.TryParse(reward.pgcId, out var pid) && pgcIdList.Contains(pid))
            {
                index = i;
                break;
            }
            if (reward.rewardType == (int)BUDRewardType.RewardTypeZZZPhantomCrystal && reward.amount >= 5) //5个炫彩水晶
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
                    title = $"奖池 (音符灯x{i})"
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
            rewardCurrency = CurrencyType.ZZZCoin,
            rulePath = viewCfg?.RulePath,
            onBackCallBack = ShowCloseInfo,
            isPonyPreview = true,
            gashaponPonyPreviewItemDataList = gashaponPonyPreviewItemDataList
        });

        previewPanel?.SetBundleViewBgClolr(bundleViewBgColor);
        UIManager.Inst.OpenPanel<PhantomSoundPartyBigRewardShowPanel>(PanelId.PhantomSoundPartyBigRewardShowPanel);
    }

    private void ShowCloseInfo()
    {
        // 从预览页返回：重建角色（内部会先清理，保证只有一套模型）再加载载具
        InitCharacterWrapper();
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
        RefreshExchangeRedDot();
    }

    // panelInfoList 载具的 pgcId 列表(160100009/10/11)
    private List<string> GetPanelVehiclePgcIds()
    {
        var ids = new List<string>();
        foreach (var info in panelInfoList)
        {
            if (info != null && !string.IsNullOrEmpty(info.pgcId))
                ids.Add(info.pgcId);
        }
        return ids;
    }

    // panelInfoList 载具中已拥有的数量
    private int OwnedPanelVehicleCount()
    {
        int count = 0;
        foreach (var info in panelInfoList)
        {
            if (info != null && !string.IsNullOrEmpty(info.pgcId) && AssetsDataManager.IsOwned(info.pgcId))
                count++;
        }
        return count;
    }

    // 是否已拥有 panelInfoList 中任意一台载具(打折前提)
    private bool OwnsAnyPanelVehicle() => OwnedPanelVehicleCount() > 0;

    // 兑换 item "已点击(已读)"状态按载具 pgcId 分别持久化：点击过的载具永久不再显示红点
    private string ExchangeItemSeenKey(string pgcId) => "phantom_exchange_item_clicked_" + GetBaseGashaponId() + "_" + pgcId;

    // 某载具是否需要红点：处于折扣态(已拥有任一载具) 且 该载具未拥有 且 未被点击过
    private bool IsVehicleRedDotPending(string pgcId)
    {
        if (string.IsNullOrEmpty(pgcId)) return false;
        if (!OwnsAnyPanelVehicle()) return false;            // 折扣前提：已拥有任一载具
        if (AssetsDataManager.IsOwned(pgcId)) return false;  // 已拥有的载具不提示
        if (PlayerPrefs.GetInt(ExchangeItemSeenKey(pgcId), 0) == 1) return false; // 已点击过
        return true;
    }

    // 已点击过(已读)的载具 pgcId 集合，传给兑换面板控制各 item 红点
    private HashSet<string> GetExchangeSeenVehicleIds()
    {
        var set = new HashSet<string>();
        foreach (var info in panelInfoList)
        {
            if (info == null || string.IsNullOrEmpty(info.pgcId)) continue;
            if (PlayerPrefs.GetInt(ExchangeItemSeenKey(info.pgcId), 0) == 1)
                set.Add(info.pgcId);
        }
        return set;
    }

    // 入口红点：只要还存在任意一台"待点击"的折扣载具就显示
    private void RefreshExchangeRedDot()
    {
        if (exchangeRedDot == null) return;
        bool anyPending = false;
        foreach (var info in panelInfoList)
        {
            if (info != null && IsVehicleRedDotPending(info.pgcId)) { anyPending = true; break; }
        }
        exchangeRedDot.SetActive(anyPending);
    }
    private void OnGiftBtnClick()
    {
        //UIManager.Inst.OpenPanel(PanelId.PhantomSoundPartyLimitedGiftPackPanel);
         UIManager.Inst.OpenPanel(PanelId.RechargePanel, (int)RechargeId.zzzGiftPack);
    }

    private void OnSeasonBtnClick()
    {
        var exchangeGroupList = new List<GashaponPonyExchangeItemData>();
        // var idOrder = new List<string> { "160200004", "95_1", "190000026", "10500080", "180100005", "40400455" };
        // gashaponData.ExchangeList.Sort((a, b) =>
        // {
        //     int indexA = idOrder.IndexOf(a.Id);
        //     int indexB = idOrder.IndexOf(b.Id);
        //     if (indexA < 0) indexA = int.MaxValue;
        //     if (indexB < 0) indexB = int.MaxValue;
        //     return indexA.CompareTo(indexB);
        // });
        // exchangeGroupList.Add(new GashaponPonyExchangeItemData
        // {
        //     exchangeList = gashaponData.ExchangeList,
        //     title = "幻音派对"
        // });
        var ponyExchangeList = dataHandler.GetGashaponData("lottery.zzz.phantomParty").ExchangeList;
        if(ponyExchangeList != null && ponyExchangeList.Count > 0)
        {
            exchangeGroupList.Add(new GashaponPonyExchangeItemData
            {
                exchangeList = ponyExchangeList,
                title = "幻音派对"
            });
        }

        var viewCfg = GashaponDataManager.Inst.GetGashaponView(GetBaseGashaponId());
        var bgPath = viewCfg != null ? viewCfg.BgPath : null;
        var exchangePanel = UIManager.Inst.OpenPanel<GashaponExchangePanel>(PanelId.GashaponExchangePanel, new GashaponExchangeParam
        {
            bgPath = bgPath,
            title = GetGashaponName(),
            gashaponData = gashaponData,
            rewardCurrency = CurrencyType.ZZZPhantomCrystalShards,
            rewardCurrency1 = CurrencyType.ZZZPhantomCrystal,
            itemBgColor = bundleViewBgColor,
            clearTips = "",
            buyTips = "",
            isSpecialRoot = true,
            exchangeGroupList = exchangeGroupList,
            isPhantomSoundParty = true,
            phantomVehicleOwnedDiscount = OwnsAnyPanelVehicle(),
            phantomVehiclePgcIds = GetPanelVehiclePgcIds(),
            phantomExchangeSeenIds = GetExchangeSeenVehicleIds(),
            specialItemLevelMap = _exchangeItemLevelMap,
        });

        exchangePanel.SetAnimPreviewBtnColor(new Color32(210, 73, 73, 255), new Color32(255, 192, 31, 255), new Color32(255, 86, 96, 255));
        exchangePanel.SetBundleViewBgColor(bundleViewBgColor);
        // 从兑换页返回：重建角色（内部会先清理，保证只有一套模型）再加载载具
        // 兑换载具后可能改变了任务进度，重新拉取服务器数据刷新 RewardItemList
        exchangePanel.onCloseCallback = () => { InitCharacterWrapper(); LoadVehicle(); GetSeverRefresh(); };
        exchangePanel.onSpecialItemClicked = (clickedVehicleId) =>
        {
            // 玩家点击了某台有红点的载具：仅该载具永久标记已读，其余载具红点保留
            if (!string.IsNullOrEmpty(clickedVehicleId))
                PlayerPrefs.SetInt(ExchangeItemSeenKey(clickedVehicleId), 1);
            RefreshExchangeRedDot();
        };
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

        int num = AccountDataManager.Inst.BalanceInfo.GetAccountCount(CurrencyType.ZZZCoin);
        if (gashaponInfoRsp.singleDrawDiscountedPrice > num)
        {
            OpenZZZCoinGiftPack(gashaponInfoRsp.singleDrawDiscountedPrice - num);
            return;
        }

        if (!GashaponUtils.CurrencyIsEnough((int)CurrencyType.ZZZCoin, gashaponInfoRsp.singleDrawDiscountedPrice))
        {
            OpenZZZCoinGiftPack(gashaponInfoRsp.singleDrawDiscountedPrice - num);
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

        int num = AccountDataManager.Inst.BalanceInfo.GetAccountCount(CurrencyType.ZZZCoin);
        if (gashaponInfoRsp.tenDrawDiscountedPrice > num)
        {
            OpenZZZCoinGiftPack(gashaponInfoRsp.tenDrawDiscountedPrice - num);
            return;
        }

        if (!GashaponUtils.CurrencyIsEnough((int)CurrencyType.ZZZCoin, gashaponInfoRsp.singleDrawPrice))
        {
            OpenZZZCoinGiftPack(gashaponInfoRsp.tenDrawDiscountedPrice - num);
            return;
        }

        var gId = GetBaseGashaponId();
        gachaRound = gashaponInfoRsp.luckyProgressInfo?.round ?? 0;
        isRotating = true;
        // 与 Pony 保持一致：该扭蛋十连按钮实际请求 3 次（3次为1轮）
        GashaponDataManager.Inst.RequestGashapon(gId, 3, OnGashaOnceRsp);
    }

    // 绒币不足时跳转到充值面板的绒币礼包页签(PhantomSoundPartyGiftPackPanel)
    // needCount：本次抽取需要消耗的绒币数量 - 当前已拥有的绒币数量(即还差多少绒币)
    private void OpenZZZCoinGiftPack(int needCount)
    {
        var panel = UIManager.Inst.OpenPanel<PhantomSoundPartyGiftPackMiniPanel>(PanelId.PhantomSoundPartyGiftPackMiniPanel);
        if (panel != null) panel.SetDic(Mathf.Max(0, needCount));
    }

    private void OnBigRewardClick()
    {
        GashaponDataManager.Inst.RequestClaimTaskReward(GetBaseGashaponId(), 4, rsp =>
        {
            AccountDataManager.Inst.BalanceInfo.Refresh();
            // 领取任务奖励后重新拉取服务器数据，刷新 RewardItemList 的领取状态
            GetSeverRefresh();
            if (rsp?.rewardList == null || rsp.rewardList.Count == 0) return;
            var rewardDatas = new List<CommonRewardItemData>{
            new()
            {
                rewardName = "绒天使聊天气泡",
                RewardAmount = 1,
                rewardType = (int)RewardType.RewardChatBubbles,
                pgcId = "120100032",
            },};
            var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
            panel.ShowRewards(rewardDatas, true);
        });
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

// #if UNITY_EDITOR
//     [ContextMenu("模拟抽奖结果(绒天使水晶x6+碎片x30)")]
//     private void SimulateGashaponRsp()
//     {
//         var mockRsp = new GashaponRsp
//         {
//             rewardList = new List<RewardInfo>
//             {
//                 new() { level = 1, rewardType = 117, amount = 6,  pgcId = "0", rewardName = "绒天使水晶" },
//                 new() { level = 5, rewardType = 118, amount = 20, pgcId = "0", rewardName = "绒天使碎片" },
//                 new() { level = 5, rewardType = 118, amount = 10, pgcId = "0", rewardName = "绒天使碎片" },
//             },
//             wonFirstPrizeFrequency = 0,
//             gachaTimes = 0,
//             lotteryInfo = new GashaponInfoRsp
//             {
//                 restGachaNum = 0,
//                 singleDrawPrice = 68,
//                 singleDrawDiscountedPrice = 68,
//                 tenDrawPrice = 204,
//                 tenDrawDiscountedPrice = 204,
//                 luckyProgressInfo = new GashaponLuckyPregross
//                 {
//                     start = 0, end = 0, round = 0, roundIndex = 0
//                 },
//             },
//         };
//         // 令 gachaRound 与 newRound 相同，跳过星星点亮动画直接展示奖励面板
//         gachaRound = mockRsp.lotteryInfo.luckyProgressInfo?.round ?? 0;
//         isRotating = true;
//         OnGashaOnceRsp(mockRsp);
//     }
// #endif

}
