using System;
using System.Collections.Generic;
using Es;
using Game.Avatar;
using Game.Store;
using GameData.Gashapon;
using GameData.PgcData;
using Newtonsoft.Json;
using UI.BaseWidgets;
using Product;
using UI.Manager;
using UI.UIPanels.GashaponPanel;
using Basic.Utils;
using Game.Audio;
using UnityEngine;
using UnityEngine.UI;

public class GashaponXiaXiaZaiPanel : BaseGashaponView
{
    private const string ConfigPath = "Assets/Loadable/UI/UIPanel/GashaponXiaXiaZaiPanel/GashaponXiaXiaZaiConfig.json";
    private const string AtlasPath = "Assets/Loadable/UI/UIPanel/GashaponXiaXiaZaiPanel/GashaponXiaXiaZaiPanel.spriteatlas";

    public List<Toggle> toggles;
    public Transform itemPanent;
    public GameObject item;
    public CButton PreviewBtn;
    public CButton ShowRewardBtn;
    public CButton TwistBtn;
    public CButton InfoBtn;

    public CButton BigRewardBtn;
    public Text BeforePrice;
    public Text Price;
    public Text Discount;
    public Text TipsText;
    public GameObject eff;
    public GameObject ShowReward;
    public CButton clossShowReward;
    public Image ShowReward_icon;
       public Image showrewardIconBg;
    public GameObject selectBg;
    public Text numText;
    [SerializeField] private CButton PreVideoBtn;

    private int _drawToken;

    private bool _twistClicked;
    private bool _isDrawing;

    [SerializeField] internal Transform CharacterRoot;
    [SerializeField] internal AvatarCameraController avatarCameraController;

    private List<XiaXiaZaiSuitConfig> suitConfigs;
    private readonly List<GameObject> characterShows = new();
    private readonly List<CharacterWrap> characterWrappers = new();
    private readonly List<GashaponXiaXiaZaiItem> items = new();
    [SerializeField] private List<SuitRewardItem> RewardItemList;
    private int _curToggleIndex;

    // 记录每张牌当前已显示的 rewardId，用于 RefreshItemsByRewardPool 做差分刷新，
    // 避免抽奖回调里 OnGashaponInfoUpdate 被触发时把已翻开格子的 icon 闪烁一次
    private readonly Dictionary<int, int> _displayedRewardByCard = new();

    private const string videoPath = "Assets/Loadable/Demand3D/ResVideo/xiaxia/huhuyu.mp4";

    private Dictionary<int, string> levelColor = new Dictionary<int, string>()
    {
        {1,"FFA95A"},
        {2,"9F72FF"},
        {3,"92BEFF"},
        {4, "FF785A"},
        {
            5,"7BED72"
        }
    };

    public override void OnCreate(string id)
    {
        base.OnCreate(id);
        LoadSuitConfig();
        InitPreviewPlayers();
        InitItems();
        InitToggles();
        TwistBtn.onClick.AddListener(OnTwistClick);
        PreviewBtn.onClick.AddListener(OnPreviewBtnClick);
        InfoBtn.onClick.AddListener(() =>
        {
            var viewCfg = GashaponDataManager.Inst.GetGashaponView(gashaponId);
            UIManager.Inst.OpenPanel<GashaponRulePanel>(PanelId.GashaponRulePanel, viewCfg.RulePath);
        });
        ShowReward.GetComponent<Button>().onClick.AddListener(() => { ShowReward.SetActive(false); });
        if (ShowRewardBtn != null)
            ShowRewardBtn.onClick.AddListener(() =>
                UIManager.Inst.OpenPanel<CollaborationLimitedGiftPackPanel>(PanelId.CollaborationLimitedGiftPackPanel));

        if (BigRewardBtn != null)
            BigRewardBtn.onClick.AddListener(OnBigRewardClick);

        if (ShowReward != null) ShowReward.SetActive(false);
        if (clossShowReward != null)
            clossShowReward.onClick.AddListener(() => { if (ShowReward != null) ShowReward.SetActive(false); });

        RefreshPriceUI();
        PreVideoBtn?.onClick.AddListener(OnPriviewVideoBtnClick);
    }

    private void OnPriviewVideoBtnClick()
    {
        UIManager.Inst.OpenPanel<VideoPreviewPanel>(PanelId.VideoPreviewPanel, videoPath);
    }

    public override void OnShow()
    {
        if (CharacterRoot != null)
            CharacterRoot.gameObject.SetActive(true);

        var pendingId = GashaponDataManager.Inst.pendingSubLotteryId;
        GashaponDataManager.Inst.pendingSubLotteryId = null;

        if (pendingId == "lottery.babyShrimp.huhu" || pendingId == "lottery.babyShrimpSuit")
        {
            toggles[0].isOn = true;
            _curToggleIndex = 0;
        }
        if (pendingId == "lottery.babyShrimp.wuwu")
        {
            toggles[1].isOn = true;
            _curToggleIndex = 1;
        }
        if (pendingId == "lottery.babyShrimp.rabbit")
        {
            toggles[2].isOn = true;
            _curToggleIndex = 2;
        }

        if (suitConfigs != null && _curToggleIndex < suitConfigs.Count)
            GashaponDataManager.Inst.RequestGashaponInfo(suitConfigs[_curToggleIndex].lotteryId, OnGashaponInfoUpdate);
    }

    private void RefreshPriceUI()
    {
        //if (Price != null) Price.SetText("--");
        if (BeforePrice != null) BeforePrice.gameObject.SetActive(false);
        if (Discount != null) Discount.transform.parent.gameObject.SetActive(false);
        if (TipsText != null) TipsText.SetText("每次抽取都不会重复奖励，9次之内必解锁大奖");
    }

    private Sprite LoadSprite(string spriteName)
    {
        return XAssetLoaderMgr.Inst.LoadSpriteInAltas(AtlasPath, spriteName, gameObject);
    }

    private void InitItems()
    {
        for (int j = 0; j < 9; j++)
        {
            var go = Instantiate(item, itemPanent);
            go.transform.SetSiblingIndex(j);
            var itemScript = go.GetComponent<GashaponXiaXiaZaiItem>();
            itemScript.Init(j, OnItemClick);
            items.Add(itemScript);
        }
    }

    private void RefreshItems(int index)
    {
        var sprite = LoadSprite($"item_bg{index}");
        var sprite_big = LoadSprite($"item_bg{index}_big");

        foreach (var it in items)
        {
            if (it.bgImage != null)
                it.bgImage.sprite = sprite;
            if(it.bgImage_big != null)
                it.bgImage_big.sprite = sprite_big;
            if(it.icon_bg_anim != null)
                it.icon_bg_anim.sprite = sprite;
            it.Reset();
        }
        // 切页签/重置背景后，所有 item 都已 Reset，差分缓存也需同步清空
        _displayedRewardByCard.Clear();
    }

    private void OnItemClick(int index)
    {
        if (!_twistClicked)
        {
            TipPanel.ShowToast("请先购买");
            return;
        }
        if (_isDrawing) return;
        _isDrawing = true;
        _twistClicked = false;
        if (TipsText != null) TipsText.SetText("每次抽取都不会重复奖励，9次之内必解锁大奖");
        if (ShowReward != null) ShowReward.SetActive(false);
        if (ShowReward_icon != null) ShowReward_icon.sprite = null;
        if (numText != null) numText.gameObject.SetActive(false);
        int myToken = ++_drawToken;

        string subLotteryId = suitConfigs != null && _curToggleIndex < suitConfigs.Count
            ? suitConfigs[_curToggleIndex].lotteryId
            : gashaponId;

        GashaponDataManager.Inst.RequestGashapon(subLotteryId, OneTimeGasha, rsp =>
        {
            if (rsp == null) { _isDrawing = false; return; }

            // 不走基类 OnGashaOnceRsp——基类会用主 gashaponId 刷新 Info，
            // 主 ID 的 taskList 可能返回 rewardStatus=3，污染 _taskStatus
            gashaponInfoRsp = rsp.lotteryInfo;
            // 立即用抽奖响应里最新的 lotteryInfo 刷一次 UI（RewardItemList、价格等），
            // 不等服务器二次请求，避免动画播完时 RewardItemList 状态还停留在旧值。
            // 注意：抽奖期间 _isDrawing == true，OnGashaponInfoUpdate 内的 RefreshItemsByRewardPool 会被跳过，
            // 整个 grid 不会被刷新，避免 icon 闪烁；grid 同步留到特效结束后再做
            if (rsp.lotteryInfo != null) OnGashaponInfoUpdate(rsp.lotteryInfo);
            GashaponDataManager.Inst.RequestGashaponInfo(subLotteryId, OnGashaponInfoUpdate);
            AccountDataManager.Inst.RefreshUserInfo();

            if (rsp.rewardList == null || rsp.rewardList.Count == 0)
            {
                _isDrawing = false;
                // 抽奖异常不进特效流程，需在这里补一次 grid 同步（抽奖期间被跳过了）
                if (gashaponInfoRsp != null) RefreshItemsByRewardPool(gashaponInfoRsp);
                return;
            }
            var reward = rsp.rewardList[0];

            var curSuit = suitConfigs != null && _curToggleIndex < suitConfigs.Count ? suitConfigs[_curToggleIndex] : null;
            int bgLevel = reward.level > 0 ? Math.Min(reward.level, 3) : (curSuit?.BgLevel ?? 1);
            var bgSprite = LoadSprite($"bg{bgLevel}");
            var capturedItem = items[index];
            var bgSprite_big = capturedItem.bgImage_big.sprite;

            void StartEffect(Sprite icon)
            {
                capturedItem.ShowEffect(() => PlayEff(bgSprite_big, bgSprite, icon, () =>
                {
                    _isDrawing = false;
                    capturedItem.ShowRewardIcon(icon, bgSprite);
                    if (GameUtils.ConvertRewardType(reward.rewardType) != CurrencyType.None)
                        capturedItem.SetCount(reward.amount);
                    capturedItem.EnableBtn();
                    // 把刚显示的牌写入差分缓存，避免接下来 RefreshItemsByRewardPool 再次 Reset+重设导致闪烁
                    if (gashaponInfoRsp?.rewardPool != null)
                    {
                        var d = gashaponInfoRsp.rewardPool.Find(x => x.cardId == index + 1);
                        if (d != null && d.rewardId != 0)
                            _displayedRewardByCard[index] = d.rewardId;
                    }
                    // 抽奖期间整个 grid 被跳过，此刻同步一次最新 rewardPool 状态（差分逻辑会确保不闪烁）
                    if (gashaponInfoRsp != null) RefreshItemsByRewardPool(gashaponInfoRsp);
                    if (selectBg != null) selectBg.SetActive(false);
                    bool allDrawn = gashaponInfoRsp != null && gashaponInfoRsp.singleDrawPrice == 0 && gashaponInfoRsp.singleDrawDiscountedPrice == 0;
                    TwistBtn.gameObject.SetActive(!allDrawn);
                    if (allDrawn && TipsText != null)
                    {
                        var showTxt = suitConfigs != null && _curToggleIndex < suitConfigs.Count ? suitConfigs[_curToggleIndex].showText : null;
                        if (!string.IsNullOrEmpty(showTxt)) TipsText.SetText(showTxt);
                    }
                    if (ShowReward != null && _drawToken == myToken)
                    {
                        ShowReward.SetActive(true);
                        AkSoundManager.Inst.PlayUIEffectSound("Play_UI_GetRewards_A3");
                        if (ShowReward_icon != null) ShowReward_icon.sprite = icon;
                        if (numText != null)
                        {
                            numText.gameObject.SetActive(reward.amount > 1);
                            if (reward.amount > 1) numText.SetText($"x{reward.amount}");
                        }
                        if (showrewardIconBg != null)
                        {
                            int colorLevel = reward.level > 0 ? reward.level : 1;
                            if (levelColor.TryGetValue(colorLevel, out string hex) && ColorUtility.TryParseHtmlString("#" + hex, out Color bgColor))
                                showrewardIconBg.color = bgColor;
                        }
                    }
                }));
            }

            var currencyType = GameUtils.ConvertRewardType(reward.rewardType);
            if (currencyType != CurrencyType.None)
            {
                // 与 GashaponXiaXiaZaiItem.ShowRewardIcon(GashaponRewardData) 货币分支保持一致：
                // 货币道具 icon 尺寸需为 108x108。特效结束 callback 调用的是 ShowRewardIcon(Sprite, Sprite) 重载，
                // 内部 SetIcon 不会改 sizeDelta，所以在这里提前设好，特效结束后会保留
                capturedItem.iconImage.rectTransform.sizeDelta = new Vector2(108, 108);
                PgcUtils.LoadCurrencyIconAsync(currencyType, capturedItem.gameObject, StartEffect);
                return;
            }

            // 优先用 subData.RewardList 里 GashaponRewardData 预解析好的 PgcDatas[0].ResourceType
            // （和 GashaponXiaXiaZaiItem 一致）；本地 DataTables 在某些 pgcId 缺配时 GetPgcConfigData 会返回 null
            // 导致 switch 整段跳过、最终走到通用 LoadRewardIcon 显示不出正确 icon
            GashaponData subDataForReward = GashaponDataManager.Inst.gashaponData(subLotteryId);
            GashaponRewardData matchedReward = null;
            if (subDataForReward?.RewardList != null)
            {
                if (!string.IsNullOrEmpty(reward.pgcId))
                    matchedReward = subDataForReward.RewardList.Find(r => r.Id == reward.pgcId);
                if (matchedReward == null && !string.IsNullOrEmpty(reward.bundleId))
                    matchedReward = subDataForReward.RewardList.Find(r => r.BundleId == reward.bundleId);
            }

            ResourceType? resType = null;
            if (matchedReward != null && GashaponUtils.HasPGCData(matchedReward))
                resType = matchedReward.PgcDatas[0].ResourceType;
            else if (!string.IsNullOrEmpty(reward.pgcId))
            {
                var pgcCfg = PgcUtils.GetPgcConfigData(reward.pgcId);
                if (pgcCfg != null) resType = (ResourceType)pgcCfg.ResourceType;
            }

            if (resType != null)
            {
                switch (resType.Value)
                {
                    case ResourceType.Vehicle:
                        StartEffect(PgcUtils.LoadVehicleIcon(reward.pgcId, capturedItem.gameObject));
                        return;
                    case ResourceType.Avatar:
                        if (!string.IsNullOrEmpty(reward.bundleId))
                            PgcUtils.LoadBundleIconAsync(reward.bundleId, capturedItem.gameObject, StartEffect);
                        else
                            PgcUtils.LoadAvatarIconAsync(reward.pgcId, capturedItem.gameObject, StartEffect);
                        return;
                    case ResourceType.PGCPetAvatar:
                        PgcUtils.LoadPetAvatarIconAsync(reward.pgcId, capturedItem.gameObject, StartEffect);
                        return;
                    case ResourceType.Emote:
                        PgcUtils.LoadEmoteIconAsync(reward.pgcId, capturedItem.gameObject, StartEffect);
                        return;
                }
            }

            var rewardType = reward.rewardType;
            if (rewardType == (int)RewardType.RewardAvatarFrame)
                UserUIWidgetManager.Inst.GetHeadCycleImgByPgcIdAsync(reward.pgcId, capturedItem.gameObject, StartEffect);
            else if (rewardType == (int)RewardType.RewardChatBubbles)
                UserUIWidgetManager.Inst.GetChatBubbleIconByPgcIdAsync(reward.pgcId, capturedItem.gameObject, StartEffect);
            else if (rewardType == (int)BUDRewardType.RewardUgcTemplateResource)
                PgcUtils.LoadPetUGCTemplateAsync(reward.pgcId, capturedItem.gameObject, StartEffect);
            else if (rewardType == (int)BUDRewardType.RewardHomepageSkin)
                StartEffect(ProfileThemeManager.Inst.LoadThemeIcon(reward.pgcId, capturedItem.gameObject));
            else if (rewardType == (int)BUDRewardType.RewardTypeNicknameFrame)
                UserUIWidgetManager.Inst.GetNicknameBgByPgcIdAsync(reward.pgcId, capturedItem.gameObject, StartEffect);
            else if (rewardType == (int)BUDRewardType.RewardTypeTitle)
                UserUIWidgetManager.Inst.GetTitleImgByPgcIdAsync(reward.pgcId, capturedItem.gameObject, StartEffect);
            else if (rewardType == (int)BUDRewardType.RewardPgcBundle && !string.IsNullOrEmpty(reward.bundleId))
                PgcUtils.LoadBundleIconAsync(reward.bundleId, capturedItem.gameObject, StartEffect);
            else
                StartEffect(PgcUtils.LoadRewardIcon((BUDRewardType)reward.rewardType, capturedItem.gameObject));
        }, cardId: index + 1);
    }

    private void PlayEff(Sprite bgImg, Sprite iconBgImg, Sprite iconImg, Action onComplete = null)
    {
        if (eff == null) { onComplete?.Invoke(); return; }
        eff.SetActive(true);
        AkSoundManager.Inst.PlayUIEffectSound("Play_UI_XiaxiaCard_Ani");
        var childAnimator = eff.GetComponentInChildren<Animator>();
        if (childAnimator == null)
        {
            TimerManager.Inst.RunOnce("XiaXiaZaiEffHide", 1f, () => { if (eff != null) eff.SetActive(false); onComplete?.Invoke(); });
            return;
        }
        var bgImage = GameObjectEx.FindComponentByName<Image>(childAnimator.transform, "Image_golden");
        if (bgImage != null) bgImage.sprite = bgImg;
        var iconbgImage = GameObjectEx.FindComponentByName<Image>(childAnimator.transform, "Image_golden_tex");
        if (iconbgImage != null) iconbgImage.sprite = iconBgImg;
        var icon = GameObjectEx.FindComponentByName<Image>(childAnimator.transform, "icon");
        if (icon != null) icon.sprite = iconImg;
        childAnimator.Rebind();
        childAnimator.Update(0f);
        float duration = 1f;
        if (childAnimator.runtimeAnimatorController != null)
        {
            var clips = childAnimator.runtimeAnimatorController.animationClips;
            if (clips != null && clips.Length > 0) duration = clips[0].length;
        }
        TimerManager.Inst.RunOnce("XiaXiaZaiEffHide", duration, () => { if (eff != null) eff.SetActive(false); onComplete?.Invoke(); });
    }



    private void OnTwistClick()
    {
        if (gashaponInfoRsp == null) return;

        int price = gashaponInfoRsp.singleDrawDiscountedPrice;
        int balance = AccountDataManager.Inst.BalanceInfo.GetAccountCount(CurrencyType.ShrimpYuan);
        if (price > balance)
        {
            GashaponDataManager.Inst.ShowCurrencyNoEnough((int)CurrencyType.ShrimpYuan, price);
            return;
        }
        _twistClicked = true;
        TwistBtn.gameObject.SetActive(false);
        if (TipsText != null) TipsText.SetText("请选择一张翻牌");
        selectBg.SetActive(true);
    }

    protected override void OnGashaponInfoUpdate(GashaponInfoRsp infoRsp)
    {
        base.OnGashaponInfoUpdate(infoRsp);
        if (!this || infoRsp == null) return;

        bool hasDiscount = infoRsp.singleDrawDiscountedPrice != infoRsp.singleDrawPrice;

        if (Price != null)
            Price.SetText(infoRsp.singleDrawDiscountedPrice.ToString());

        if (BeforePrice != null)
        {
            BeforePrice.gameObject.SetActive(hasDiscount);
            if (hasDiscount)
                BeforePrice.SetText(infoRsp.singleDrawPrice.ToString());
        }

        if (Discount != null)
        {
            Discount.transform.parent.gameObject.SetActive(hasDiscount);
            if (hasDiscount)
            {
                float rate = (float)infoRsp.singleDrawDiscountedPrice / infoRsp.singleDrawPrice;
                string discountStr = rate > 0.6f ? "75折" : rate > 0.45f ? "5折" : "4折";
                Discount.SetText(discountStr);
            }
        }

        RefreshItemsByRewardPool(infoRsp);

        bool allDrawn = infoRsp.singleDrawPrice == 0 && infoRsp.singleDrawDiscountedPrice == 0;
        if (!_twistClicked)
            TwistBtn.gameObject.SetActive(!allDrawn);
        if (allDrawn && TipsText != null)
        {
            var showTxt = suitConfigs != null && _curToggleIndex < suitConfigs.Count ? suitConfigs[_curToggleIndex].showText : null;
            if (!string.IsNullOrEmpty(showTxt)) TipsText.SetText(showTxt);
        }

        if (infoRsp.taskList != null)
        {
            for (int i = 0; i < RewardItemList.Count; i++)
            {
                if (infoRsp.taskList.Count <= i) return;
                RewardItemList[i].SetData(infoRsp.taskList[i]);
            }
        }
    }

    private void OnBigRewardClick()
    {
        // —— 临时测试：直接打开通用奖励界面展示"虾虾崽聊天气泡"（PgcId 120100031），不走服务器请求 ——
        // 测试完恢复正式逻辑：删掉本测试块（return 之前），并把下方 /* */ 注释打开
        // var testRewards = new List<CommonRewardItemData>
        // {
        //     new()
        //     {
        //         rewardName = "虾虾崽聊天气泡",
        //         RewardAmount = 1,
        //         rewardType = (int)RewardType.RewardChatBubbles,
        //         pgcId = "120100031",
        //     },
        // };
        // var testPanel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        // testPanel.ShowRewards(testRewards, true);
        // return;

        
        GashaponDataManager.Inst.RequestClaimTaskReward("lottery.babyShrimp.huhu", 4, rsp =>
        {
            AccountDataManager.Inst.BalanceInfo.Refresh();
          
            if (rsp?.rewardList == null || rsp.rewardList.Count == 0) return;
            var rewardDatas = new List<CommonRewardItemData>{
            new()
            {
                rewardName = "虾虾崽聊天气泡",
                RewardAmount = 1,
                rewardType = (int)RewardType.RewardChatBubbles,
                pgcId = "120100031",
            },};
            var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
            panel.ShowRewards(rewardDatas, true);
        });
        
    }

    private void RefreshItemsByRewardPool(GashaponInfoRsp infoRsp)
    {
        if (infoRsp.rewardPool == null || items.Count == 0) return;
        // 抽奖进行中（点击翻牌后、特效结束前）整个 grid 不刷新，避免 icon/icon_bg 闪烁。
        // grid 状态会在特效结束后由抽奖回调显式调用 RefreshItemsByRewardPool 同步
        if (_isDrawing) return;

        GashaponData subData = suitConfigs != null && _curToggleIndex < suitConfigs.Count
            ? GashaponDataManager.Inst.gashaponData(suitConfigs[_curToggleIndex].lotteryId)
            : null;
        var curSuitConfig = suitConfigs != null && _curToggleIndex < suitConfigs.Count
            ? suitConfigs[_curToggleIndex] : null;

        // 差分刷新：rewardId 与上次一致的格子完全不动，避免无谓的 Reset 导致 icon 闪烁。
        // rewardId == 0 视为"未抽中状态"，rewardId == -1 视为"兜底渲染"（无明确 rewardId 时占位）。
        var seenCards = new HashSet<int>();

        foreach (var drawn in infoRsp.rewardPool)
        {
            int itemIndex = drawn.cardId - 1;
            if (itemIndex < 0 || itemIndex >= items.Count) continue;
            seenCards.Add(itemIndex);

            if (drawn.everDrawn != 1 || drawn.rewardId == 0)
            {
                if (_displayedRewardByCard.ContainsKey(itemIndex))
                {
                    items[itemIndex].Reset();
                    _displayedRewardByCard.Remove(itemIndex);
                }
                continue;
            }

            // 与已显示的一致则跳过，避免闪烁
            if (_displayedRewardByCard.TryGetValue(itemIndex, out int prev) && prev == drawn.rewardId)
                continue;

            // 原逻辑：按 RewardId 从 RewardList 查单品
            var rewardData = subData?.RewardList?.Find(r => r.RewardId == drawn.rewardId);
            if (rewardData != null)
            {
                var bgSprite = LoadSprite($"bg{Math.Min((int)rewardData.Level, 3)}");
                items[itemIndex].Reset();
                items[itemIndex].ShowRewardIcon(rewardData, bgSprite);
                _displayedRewardByCard[itemIndex] = drawn.rewardId;
                continue;
            }

            // pgcId 直接显示
            if (!string.IsNullOrEmpty(drawn.pgcId))
            {
                var bgSprite = LoadSprite($"bg{Math.Max(1, Math.Min(drawn.level, 3))}");
                items[itemIndex].Reset();
                items[itemIndex].ShowRewardIcon(drawn.pgcId, bgSprite);
                _displayedRewardByCard[itemIndex] = drawn.rewardId;
                continue;
            }

            // 兜底：pgcId 为空且 RewardList 无匹配
            if (curSuitConfig == null) continue;
            var fallbackBg = LoadSprite($"bg{curSuitConfig.BgLevel}");
            if (curSuitConfig.IsSuit)
            {
                var captured = itemIndex;
                items[captured].Reset();
                PgcUtils.LoadBundleIconAsync(curSuitConfig.bundleId, items[captured].gameObject,
                    sp => items[captured].ShowRewardIcon(sp, fallbackBg));
                _displayedRewardByCard[itemIndex] = drawn.rewardId;
            }
            else if (!string.IsNullOrEmpty(curSuitConfig.SinglePgcId))
            {
                var captured = itemIndex;
                items[captured].Reset();
                items[captured].ShowRewardIcon(curSuitConfig.SinglePgcId, fallbackBg);
                _displayedRewardByCard[itemIndex] = drawn.rewardId;
            }
        }

        // 本次 rewardPool 未出现、但之前显示过的格子，需要 Reset 还原
        if (_displayedRewardByCard.Count > 0)
        {
            List<int> toRemove = null;
            foreach (var kv in _displayedRewardByCard)
            {
                if (seenCards.Contains(kv.Key)) continue;
                if (kv.Key >= 0 && kv.Key < items.Count) items[kv.Key].Reset();
                (toRemove ??= new List<int>()).Add(kv.Key);
            }
            if (toRemove != null)
                foreach (var k in toRemove) _displayedRewardByCard.Remove(k);
        }
    }

    private void OnPreviewBtnClick()
    {
        string subLotteryId = suitConfigs != null && _curToggleIndex < suitConfigs.Count
            ? suitConfigs[_curToggleIndex].lotteryId : gashaponId;
        var subGashaponData = GashaponDataManager.Inst.gashaponData(subLotteryId);
        if (subGashaponData == null)
        {
            Debug.LogError("gashaponData is null!");
            return;
        }
        var viewCfg = GashaponDataManager.Inst.GetGashaponView(gashaponId);
        var panel = UIManager.Inst.OpenPanel<GashaponPreviewPanel>(PanelId.GashaponPreviewPanel, new GashaponPreviewParam
        {
            bgPath = viewCfg.BgPath,
            title = suitConfigs != null && _curToggleIndex < suitConfigs.Count ? suitConfigs[_curToggleIndex].suit : string.Empty,
            gashaponData = subGashaponData,
            gashaponInfoRsp = gashaponInfoRsp,
            rewardCurrency = CurrencyType.ShrimpYuan,
            rulePath = viewCfg.RulePath,
            onBackCallBack = () =>
            {
                if (CharacterRoot != null)
                    CharacterRoot.gameObject.SetActive(true);
                // 返回时角色重新激活，特效 Animator 会 rebind 回 base，需重新驱动 preview（本体靠 AOC override 持久不受影响）
                if (_curToggleIndex < characterWrappers.Count)
                    characterWrappers[_curToggleIndex].Avatar.GetComponent<PlayerAnimationCtrl>()?.DriveSpecialEffectPreviewIdle();
            },
            
        });
        panel.SetBundleViewBgClolr("#CCEAAB");
        // huhu/wuwu 套装预览走通用流程，不要 IdleSync 路径，避免显示云和 exhibit idle 覆盖。
        // 仅当前选中的 suit 配置里含特殊皮肤（目前仅 huhuyun 兔云之座 10500084）时才开启
        bool hasSpecialSkin = false;
        if (suitConfigs != null && _curToggleIndex < suitConfigs.Count)
        {
            foreach (var pgcId in suitConfigs[_curToggleIndex].idList)
            {
                if (DataTables.GetSpecialSkinConfig(pgcId.ToString()) != null)
                {
                    hasSpecialSkin = true;
                    break;
                }
            }
        }
        if (hasSpecialSkin)
        {
            panel.EnableIdleSync();
        }
        else
        {
            // huhu/wuwu 套装预览：玩家可能正装备 huhuyun 等特殊皮肤，进预览时云会跟着加载出来；走通用流程不需要云，先卸掉
            panel.TakeOffSpecialSkin();
        }
        CharacterRoot.gameObject.SetActive(false);
    }

    private void LoadSuitConfig()
    {
        var wrapper = Loader.Load<TextAsset>(ConfigPath);
        if (wrapper == null)
        {
            Debug.LogError("[GashaponXiaXiaZaiPanel] 配置加载失败: " + ConfigPath);
            return;
        }
        var textAsset = wrapper.RetainAsset(gameObject);
        suitConfigs = JsonConvert.DeserializeObject<List<XiaXiaZaiSuitConfig>>(textAsset.text);
    }

    // 可被套装替换/卸下的部位类型（保留 Skin/Head/Body/Shape 等体型结构）
    private static readonly AvatarSubType[] RemovableSubTypes =
    {
        AvatarSubType.Scarf, AvatarSubType.Belt, AvatarSubType.Brow, AvatarSubType.Clothes,
        AvatarSubType.Effect, AvatarSubType.Eyes, AvatarSubType.Glasses, AvatarSubType.Hair,
        AvatarSubType.Hats, AvatarSubType.Hand, AvatarSubType.Mouth, AvatarSubType.FacePaint,
        AvatarSubType.Shoe, AvatarSubType.Backpack, AvatarSubType.Cape, AvatarSubType.Crossbody,
        AvatarSubType.Earring, AvatarSubType.Nose, AvatarSubType.Blush, AvatarSubType.Glove,
        AvatarSubType.Visor, AvatarSubType.MusicalInstrument, AvatarSubType.Ear, AvatarSubType.Tail,
        AvatarSubType.SpecialSkin,
    };

    private void InitPreviewPlayers()
    {
        if (suitConfigs == null) return;

        avatarCameraController.RotateTarget = CharacterRoot;
        avatarCameraController.ZoomWholePosY = 0;
        avatarCameraController.ZoomWholeCameraSize = 1f;
        var saveCharacterData = AccountDataManager.Inst.UserInfo.avatarInfo;

        foreach (var config in suitConfigs)
        {
            var characterWrapper = AvatarController.Inst.CreateUIAvatar(saveCharacterData);
            characterWrapper.SetParent(CharacterRoot, true);
            var animationCtrl = characterWrapper.Avatar.GetComponent<PlayerAnimationCtrl>();
            int pendingEffects = 0;

            // 收集套装覆盖的部位类型
            var coveredTypes = new HashSet<int>();
            foreach (var pgcId in config.idList)
            {
                var pgcCfg = PgcUtils.GetPgcConfigData(pgcId.ToString());
                if (pgcCfg != null)
                    coveredTypes.Add(UniqueType.GetAvatar((AvatarSubType)pgcCfg.SubType));
            }

            // 卸下套装未覆盖的装饰部位，只保留体型结构
            if (config.IsSuit)
            {
                foreach (var subType in RemovableSubTypes)
                {
                    var uniqueType = UniqueType.GetAvatar(subType);
                    if (!coveredTypes.Contains(uniqueType))
                        characterWrapper.TakeOff(uniqueType);
                }
            }

            foreach (var pgcId in config.idList)
            {
                var pgcIdStr = pgcId.ToString();
                var pgcConfig = PgcUtils.GetPgcConfigData(pgcIdStr);
                if (pgcConfig == null) continue;
                var specialConfig = DataTables.GetSpecialSkinConfig(pgcIdStr);
                bool isSpecialSkin = specialConfig != null;
                var classType = isSpecialSkin
                    ? UniqueType.GetAvatar(AvatarSubType.SpecialSkin)
                    : UniqueType.GetAvatar((AvatarSubType)pgcConfig.SubType);
                if (isSpecialSkin)
                {
                    characterWrapper.ChangePart(classType, pgcIdStr, () =>
                    {
                        if (animationCtrl != null)
                            animationCtrl.CheckAndOverrideSpecialAnim();
                    });
                }
                else if ((AvatarSubType)pgcConfig.SubType == AvatarSubType.Effect)
                {
                    pendingEffects++;
                    characterWrapper.ChangePart(classType, pgcIdStr, () =>
                    {
                        pendingEffects--;
                        if (pendingEffects == 0 && animationCtrl != null)
                            animationCtrl.ForceIdleForUICharacter();
                    });
                }
                else
                {
                    if ((AvatarSubType)pgcConfig.SubType == AvatarSubType.Eyes) continue;
                    characterWrapper.ChangePart(classType, pgcIdStr);
                }
                var avatarConfig = DataTables.GetAvatarCommonData(pgcIdStr);
                if (avatarConfig == null) continue;
                characterWrapper.ChangeColor(classType, avatarConfig.defaultColor);
                characterWrapper.Move(classType, avatarConfig.pDef);
                characterWrapper.Rotate(classType, avatarConfig.rDef);
                characterWrapper.Scale(classType, avatarConfig.sDef);
                characterWrapper.HVScale(classType, avatarConfig.vhSDef);
                characterWrapper.SetLeftOrRight(classType, avatarConfig.leftRightType);
            }
            characterWrappers.Add(characterWrapper);
            characterShows.Add(characterWrapper.Avatar);
            characterWrapper.Avatar.SetActive(false);
        }
    }

    private void InitToggles()
    {
        for (int i = 0; i < toggles.Count; i++)
        {
            int index = i;
            toggles[i].onValueChanged.AddListener(isOn =>
            {
                if (isOn) OnToggleSelected(index);
            });
        }

        // 初始化默认选中第三个 tog（设 isOn 触发 listener → OnToggleSelected；若已是 true 则手动调一次）
        bool alreadyOn = toggles[2].isOn;
        toggles[2].isOn = true;
        if (alreadyOn) OnToggleSelected(2);
    }

    private void OnToggleSelected(int index)
    {
        _curToggleIndex = index;
        _twistClicked = false;
        if (TipsText != null) TipsText.SetText("每次抽取都不会重复奖励，9次之内必解锁大奖");

        for (int i = 0; i < characterShows.Count; i++)
            characterShows[i].SetActive(i == index);

        if (index < characterWrappers.Count)
        {
            var ctrl = characterWrappers[index].Avatar.GetComponent<PlayerAnimationCtrl>();
            ctrl?.ForceIdleForUICharacter();
            // 角色是 SetActive(false) 创建、切 toggle 才激活，特效 Animator 激活时会 rebind 回默认，
            // 故在显示这一刻重新驱动特效 preview（DriveSpecialEffectPreviewIdle 对非特殊皮肤 specialAnimRoot 为 null 即 no-op）
            ctrl?.DriveSpecialEffectPreviewIdle();
        }

        if (index < characterShows.Count && suitConfigs != null && index < suitConfigs.Count)
        {
            var cfg = suitConfigs[index];
            var t = characterShows[index].transform;
            float s = cfg.characterScale > 0f ? cfg.characterScale : 1f;
            t.localScale = Vector3.one * s;
        }

        ApplyCharacterEye(index);
        RefreshItems(index);
        avatarCameraController.SetCameraZoom(ViewType.ZoomWholeBody);
        RefreshPriceUI();

        if (suitConfigs != null && index < suitConfigs.Count)
            GashaponDataManager.Inst.RequestGashaponInfo(suitConfigs[index].lotteryId, OnGashaponInfoUpdate);
    }

    private void ApplyCharacterEye(int index)
    {
        if (index >= characterWrappers.Count || suitConfigs == null || index >= suitConfigs.Count) return;
        foreach (var id in suitConfigs[index].idList)
        {
            var pgcCfg = PgcUtils.GetPgcConfigData(id.ToString());
            if (pgcCfg == null || (AvatarSubType)pgcCfg.SubType != AvatarSubType.Eyes) continue;
            var wrapper = characterWrappers[index];
            var pgcIdStr = id.ToString();
            var classType = UniqueType.GetAvatar(AvatarSubType.Eyes);
            wrapper.TakeOff(classType);
            wrapper.TakeOff(UniqueType.GetUgcAvatar(AvatarSubType.Eyes));
            wrapper.ChangePart(classType, pgcIdStr);

            // 用眼睛 pgc 自身的默认配置覆盖玩家角色的眼睛位置/缩放/间距，避免被玩家自己的眼睛设置影响
            var avatarConfig = DataTables.GetAvatarCommonData(pgcIdStr);
            if (avatarConfig != null)
            {
                wrapper.ChangeColor(classType, avatarConfig.defaultColor);
                wrapper.Move(classType, avatarConfig.pDef);
                wrapper.Rotate(classType, avatarConfig.rDef);
                wrapper.Scale(classType, avatarConfig.sDef);
                wrapper.HVScale(classType, avatarConfig.vhSDef);
                wrapper.SetLeftOrRight(classType, avatarConfig.leftRightType);
            }
            return;
        }
        // config 里没有眼睛，保留角色创建时的默认眼睛，不做任何操作
    }
}

[Serializable]
public class XiaXiaZaiSuitConfig
{
    public string suit;
    public string level;
    public string cardLevel;
    public string lotteryId;
    public string bundleId;
    public List<int> idList;

    public string showText;
    public float characterOffsetY;
    public float characterScale;
    public bool IsSuit => !string.IsNullOrEmpty(bundleId);
    public string SinglePgcId => idList != null && idList.Count > 0 ? idList[0].ToString() : null;

    public int BgLevel {
        get {
            if (int.TryParse(level, out int n)) return Mathf.Clamp(n, 1, 3);
            if (level != null && level.StartsWith("SS")) return 1;
            if (level != null && level.StartsWith("S"))  return 1;
            return 1;
        }
    }
}
