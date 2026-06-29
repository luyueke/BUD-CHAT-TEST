using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BUD.AnimPose;
using Game.Avatar;
using Game.Pet;
using Game.Store;
using GameData;
using GameData.BaseInfo;
using GameData.PgcData;
using UI.BaseWidgets;
using UI.UIPanels.FittingRoom;
using UI.UIPanels.RechargePanel;
using UI.UIWidgets;
using UnityEngine;
using UnityEngine.UI;

// 姿势商城详情视图：从 FittingRoomPanel 的 Ugc+姿势 流程迁出。
// 预览参照 AnimIkPreview + FittingRoomPanel TryOn 的 ResourceType.UgcPose 分支：
//   面板内自建带 IK 的预览角色，按 poseType（Single/Double/PetSingle/PetWithPlayer）把姿势应用到角色；
//   双人姿势用主角色+副角色，宠物姿势用宠物+玩家。角色只创建一次，切换姿势仅重新 Pose（不叠加）。
//   点赞(UgcLikeButton)；购买为直购，价格/折扣与购买流程与 OperationView 的 BuyBtn（FittingRoomPanel.Buy）一致。
public class PoseStoreDetailView : MonoBehaviour
{
    [Header("商品信息")]
    [SerializeField] private UserInfoView UserInfoView;
    [SerializeField] private CButton Btn_UserHead;
    [SerializeField] private CText Txt_ItemName;

    [Header("购买")]
    // 价格/折扣展示复用 OperationView 同款 BuyButton 组件（全限定名，避免与全局同名 BuyButton 冲突）
    [SerializeField] private UI.UIPanels.FittingRoom.BuyButton buyButton;
    [SerializeField] private GameObject Go_BuyRoot;
    [SerializeField] private GameObject Go_OwnedRoot;

    [Header("预览角色")]
    [SerializeField] private Transform characterRoot;
    [SerializeField] private AvatarCameraController dragUtil;

    [Header("点赞")]
    [SerializeField] private UgcLikeButton UgcLikeButton;
    [Header("搜索")]
    [SerializeField] private CButton SearchButton;
    [SerializeField] private SearchView SearchViewPanel;

       public CButton BtnChangeOc;

    private RecommendItemData curData;
    private PoseInfo curPoseInfo;

    public Action<RecommendItemData> DidPayAssetAction;
    // 搜索结果（列表数据 + 上拉加载下一页的 Action），由 PoseStorePanel 订阅后灌给列表
    public Action<List<RecommendItemData>, Action> SearchResultAction;
    // 取消搜索，由 PoseStorePanel 订阅后恢复当前栏目列表
    public Action SearchCancelAction;
    private string _searchKey;

    // 预览角色（懒创建一次后复用）
    private AnimIKController animationCtrlIK;
    private AnimIKController otherAnimationCtrlIK;
    private AnimIKController petAnimationCtrlIK;
    private bool _peopleCreated = false;
    private bool _petCreated = false;
    // 换设子时由 OcChangePanel 回调传入的新形象，避免 SyncAvatarData 异步未落盘时读到旧数据
    private BaseAvatarData _pendingAvatarData;

    public void Awake()
    {
        if (dragUtil != null && dragUtil.roleCamera != null)
            dragUtil.roleCamera.backgroundColor = new Color(1, 1, 1, 0);

        // 与原商城 OperationView 一致：BuyButton 组件只负责价格展示，点击由同节点上的 Button 接收
        if (buyButton != null)
        {
            var buyClickBtn = buyButton.GetComponent<Button>();
            if (buyClickBtn != null) buyClickBtn.onClick.AddListener(OnBuyBtnClick);
            else LoggerUtils.LogError("PoseStoreDetailView: buyButton 节点上缺少 Button 组件，购买无法点击");
        }
        if (UgcLikeButton != null) UgcLikeButton.ClickAction = () => UgcLikeButton.Like();
        if (SearchButton != null) SearchButton.onClick.AddListener(OpenSearchView);
    }

    #region 搜索（参考原商城 UGCScene 的 OnSearchClick/OnSearchAction 流程）

    public void OpenSearchView()
    {
        if (SearchViewPanel == null)
        {
            return;
        }
        SearchViewPanel.searchHandle = "搜索姿势设计码/作者ID/姿势名";
        SearchViewPanel.isFittingRoom = true;
        SearchViewPanel.gameObject.SetActive(true);
        SearchViewPanel.SetSearchAction(OnSearchWord, OnSearchClear, OnSearchCancel);
        SearchViewPanel.SetInitSearchAction(OpenSearchView);
        SearchViewPanel.Show();
    }

    private void OnSearchWord(string str)
    {
        SearchLogicMgr.Inst.AddSearchHistoryWord(str);
        _searchKey = str;

        var dataHandler = AssetsDataManager.GetData<AvatarUgcSceneHandler>();
        var classType = UniqueType.Get(ResourceType.UgcPose, (int)UgcPoseSubType.PeopleAll);
        Action nextAction = null;
        nextAction = dataHandler.SearchGoodsData(classType, str, (searchKey, isEnd, goodsDatas) =>
        {
            if (this == null || searchKey != _searchKey)
            {
                return;
            }
            SearchViewPanel.isSearching = false;
            SearchViewPanel.HideSearchDiscoverAndHistoryView();

            // GoodsData 还原回列表用的 RecommendItemData（Assets[0].UgcInfo 即原始数据）
            var list = goodsDatas == null
                ? new List<RecommendItemData>()
                : goodsDatas
                    .Where(g => g?.Assets != null && g.Assets.Count > 0 && g.Assets[0].UgcInfo != null)
                    .Select(g => g.Assets[0].UgcInfo)
                    .ToList();

            if (isEnd && list.Count == 0)
            {
                TipPanel.ShowToast("没有找到相关内容");
            }
            SearchResultAction?.Invoke(list, nextAction);
        });

        // 7 位设计码：与原商城一致，额外走 SearchPanel 的设计码精确搜索
        if (!string.IsNullOrEmpty(str) && Regex.IsMatch(str, @"^[0-9A-Z]{7}$"))
        {
            SearchPanel.Search(str, (int)AvatarSubType.All, SearchPanel.SearchType.UgcPose, false);
        }
    }

    private void OnSearchClear()
    {
        _searchKey = null;
        SearchResultAction?.Invoke(new List<RecommendItemData>(), null);
    }

    private void OnSearchCancel()
    {
        _searchKey = null;
        SearchCancelAction?.Invoke();
    }

    #endregion

    public void RefreshUIByData(RecommendItemData data)
    {
        this.curData = data;
        RefreshUIInfo(data);
    }

    private void RefreshUIInfo(RecommendItemData data)
    {
        var poseInfo = data.UgcInfo as PoseInfo;
        if (poseInfo == null)
        {
            return;
        }
        curPoseInfo = poseInfo;

        var ugcId = poseInfo.id;
        AccountUserInfo accountUserInfo = data?.creatorInfo;
        UserInfoView.IsOpenProfilePanel = false;
        UserInfoView.SetData(accountUserInfo);
        Btn_UserHead.onClick.RemoveAllListeners();
        Btn_UserHead.onClick.AddListener(() =>
        {
            if (this == null)
            {
                return;
            }
            UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.UgcPose, ugcId);
        });

        UserInfoView.gameObject.SetActive(true);
        Txt_ItemName.gameObject.SetActive(true);
        Txt_ItemName.text = poseInfo.name;

        // 点赞按钮绑定当前姿势（点赞成功后会直接翻转 interactInfo.liked，先确保不为 null）
        if (UgcLikeButton != null)
        {
            if (data.interactInfo == null)
            {
                data.interactInfo = new GameData.Base.BaseInteractInfo();
            }
            UgcLikeButton.gameObject.SetActive(true);
            UgcLikeButton.SetBundleData(data);
        }

        if (BtnChangeOc != null)
        {
            bool showChangeOc = poseInfo.poseType == (int)UgcPoseSubType.Double ||
                                poseInfo.poseType == (int)UgcPoseSubType.PetWithPlayer;
            BtnChangeOc.gameObject.SetActive(showChangeOc);
        }

        RefreshOwnedStatus();
        PreviewPose(poseInfo);
    }

    private void RefreshOwnedStatus()
    {
        bool owned = (curData?.interactInfo?.consumed == 1) ||
                     (!string.IsNullOrEmpty(curPoseInfo?.id) && AssetsDataManager.IsOwned(curPoseInfo.id)) ||
                     curData?.creatorInfo?.uid == AccountDataManager.Inst.Uid;
        if (Go_BuyRoot != null)
        {
            Go_BuyRoot.SetActive(!owned);
        }
        if (Go_OwnedRoot != null)
        {
            Go_OwnedRoot.SetActive(owned);
        }
        if (buyButton != null)
        {
            // 已拥有隐藏购买按钮
            buyButton.gameObject.SetActive(!owned);
            if (!owned)
            {
                var goodsData = BuildGoodsData();
                if (goodsData != null)
                {
                    buyButton.SetTarget(goodsData);
                }
            }
        }
    }

    #region 预览角色 + 姿势

    private void PreviewPose(PoseInfo poseInfo)
    {
        if (poseInfo == null)
        {
            return;
        }

        var subType = (UgcPoseSubType)poseInfo.poseType;
        if (dragUtil != null)
        {
            dragUtil.SetEmoteView((UgcAnimSubType)poseInfo.poseType);
        }

        switch (subType)
        {
            case UgcPoseSubType.Single:
            case UgcPoseSubType.Double:
                EnsurePeopleCharacters();
                if (animationCtrlIK != null && otherAnimationCtrlIK != null)
                {
                    animationCtrlIK.Pose(poseInfo, otherAnimationCtrlIK);
                }
                break;
            case UgcPoseSubType.PetSingle:
            case UgcPoseSubType.PetWithPlayer:
                EnsurePetCharacters();
                if (petAnimationCtrlIK != null && otherAnimationCtrlIK != null)
                {
                    petAnimationCtrlIK.Pose(poseInfo, otherAnimationCtrlIK);
                }
                break;
        }
    }

    // 人物姿势：主角色 + 副角色（副角色默认隐藏，双人姿势由 Pose() 内部激活）
    private void EnsurePeopleCharacters()
    {
        if (_peopleCreated)
        {
            return;
        }
        var saveCharacterData = (_pendingAvatarData as CharacterData) ?? AccountDataManager.Inst.UserInfo.avatarInfo;
        var characterWrapper = AvatarController.Inst.CreateUIAvatarWithIKController(saveCharacterData, characterRoot);
        animationCtrlIK = characterWrapper.Avatar.GetComponent<AnimIKController>();

        var otherCharacterWrap = AvatarController.Inst.CreateUIAvatarWithIKController(AccountDataManager.Inst.UserInfo.otherAvatarInfo, characterRoot);
        otherAnimationCtrlIK = otherCharacterWrap.Avatar.GetComponent<AnimIKController>();
        otherCharacterWrap.Avatar.gameObject.SetActive(false);

        if (dragUtil != null)
        {
            dragUtil.RotateTarget = characterRoot;
        }
        _peopleCreated = true;
    }

    // 宠物姿势：宠物 + 玩家角色（玩家默认隐藏）
    private void EnsurePetCharacters()
    {
        if (_petCreated)
        {
            return;
        }
        var savePetData = (_pendingAvatarData as PetData) ?? AccountDataManager.Inst.PetInfo.avatarInfo;
        var petWrapper = PetAvatarController.Inst.CreateUIAvatarWithIKController(savePetData, characterRoot);
        petAnimationCtrlIK = petWrapper.Avatar.GetComponent<AnimIKController>();

        var otherCharacterWrap = AvatarController.Inst.CreateUIAvatarWithIKController(AccountDataManager.Inst.UserInfo.avatarInfo, characterRoot);
        otherAnimationCtrlIK = otherCharacterWrap.Avatar.GetComponent<AnimIKController>();
        otherCharacterWrap.Avatar.gameObject.SetActive(false);

        if (dragUtil != null)
        {
            dragUtil.RotateTarget = characterRoot;
        }
        _petCreated = true;
    }

    // 换设子后重建预览角色并重新应用当前姿势（供 PoseStorePanel.BtnChangeOc 回调调用）
    // avatarData：OcChangePanel 回调传入的新形象；SyncAvatarData 是异步的，
    // UserInfo.avatarInfo 此时尚未更新，直接使用回调数据避免仍显示旧设子。
    public void ResetAndRePreview(BaseAvatarData avatarData = null)
    {
        _pendingAvatarData = avatarData;
        foreach (Transform child in characterRoot)
            Destroy(child.gameObject);
        animationCtrlIK = null;
        otherAnimationCtrlIK = null;
        petAnimationCtrlIK = null;
        _peopleCreated = false;
        _petCreated = false;
        if (curPoseInfo != null) PreviewPose(curPoseInfo);
        _pendingAvatarData = null;
    }

    #endregion

    // 照 FittingRoom 原商城的方式构造姿势 GoodsData（subType=1008 姿势）：
    // GoodsData.Update() 按原商城同一套逻辑折算 Price；BuyButton.SetTarget 据此展示折扣等。
    // 姿势 subType=1008 命中「姿势不能用券」，不显示票券，与原商城一致。
    // InventoryData 的 set 是 internal 这里不赋值：未拥有时价格计算不受影响，已拥有判断由 RefreshOwnedStatus 兜底。
    private GoodsData BuildGoodsData()
    {
        if (curData == null || curPoseInfo == null)
        {
            return null;
        }

        var assetsData = new UgcPoseAssetsData
        {
            Id = curPoseInfo.id,
            Name = curPoseInfo.name,
            ResourceType = ResourceType.UgcPose,
            UgcInfo = curData,
            Value = curPoseInfo.paymentInfo != null
                ? new CurrencyData
                {
                    CurrencyType = curPoseInfo.paymentInfo.currencyType,
                    Value = curPoseInfo.paymentInfo.price
                }
                : new CurrencyData
                {
                    CurrencyType = CurrencyType.None,
                    Value = 0
                }
        };

        var goodsData = new GoodsData
        {
            Id = curPoseInfo.id,
            Name = curPoseInfo.name,
            GoodsType = GoodsType.SingleUgc,
            subType = 1000 + (int)UgcType.Pose,
            Assets = new List<AssetsData> { assetsData },
        };
        goodsData.Update();
        return goodsData;
    }

    // 直购流程与 FittingRoomPanel.Buy（OperationView 的 BuyBtn）一致。
    private bool _isBuying = false;
    private void OnBuyBtnClick()
    {
        if (curPoseInfo == null) return;
        if (_isBuying) return;

        var data = BuildGoodsData();
        if (data == null || data.Price == null) return;

        var tmpGoodsData = new GoodsData()
        {
            ButtonType = data.ButtonType,
            Id = data.Id,
            GoodsType = data.GoodsType,
            subType = data.subType,
            Price = new CurrencyData()
            {
                CurrencyType = data.Price.CurrencyType,
                Value = data.Price.Value,
            }
        };

        // 当前拥有打折卡且当前商品支持打折卡
        if (DiscountCardUtils.IsOwnedDiscountCard() && DiscountCardUtils.IsSupportDiscountCard(data))
        {
            tmpGoodsData.Price.Value = Mathf.RoundToInt(tmpGoodsData.Price.Value * DiscountCardUtils.GetDiscount());
        }

        _isBuying = true;
        AssetsDataManager.BuyGoods(tmpGoodsData, (success, reason, needNum) =>
        {
            if (this == null)
            {
                return;
            }
            _isBuying = false;
            if (!success)
            {
                if (reason.Equals("余额不足"))
                {
                    switch (data.Price.CurrencyType)
                    {
                        case CurrencyType.Coin:
                            ExchangeCoinPanel exchangeCoinPanel = UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel);
                            exchangeCoinPanel.SetData(CurrencyType.Coin, CurrencyType.Gem, needNum);
                            break;
                        case CurrencyType.Badge:
                            ExchangeCoinPanel badgePanel = UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel);
                            badgePanel.SetData(CurrencyType.Badge, CurrencyType.Gem, needNum);
                            break;
                        case CurrencyType.PinkCoin:
                            if (ExchangeCoinPanel.JudgePinkCoin(needNum))
                            {
                                ExchangeCoinPanel pinkCoinPanel = UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel);
                                pinkCoinPanel.SetData(CurrencyType.PinkCoin, CurrencyType.Gem, needNum);
                            }
                            break;
                        case CurrencyType.Gem:
                            UIManager.Inst.OpenPanel(PanelId.GetMoreGemsPanel, needNum);
                            break;
                    }
                }
            }
            else
            {
                var panel = UIManager.Inst.OpenPanel<BuySuccessTipPanel>(PanelId.BuySuccessTipPanel);
                if (panel != null)
                {
                    panel.InitData(data, "购买成功！");
                }
                if (curData != null)
                {
                    if (curData.interactInfo == null)
                    {
                        curData.interactInfo = new GameData.Base.BaseInteractInfo();
                    }
                    curData.interactInfo.consumed = 1;
                    DidPayAssetAction?.Invoke(curData);
                }
                RefreshOwnedStatus();
            }
            data.IsPayingRequest = false;
        });
    }
}
